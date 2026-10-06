FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
RobotNutStripIcon::import(path : "nutStrip/robotNutStripIcon.svg", version : "");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "f58a965fe7005e2e3c9a67c7", version : "b5ccca2891edfadb45a47aac");
import(path : "e316d3a31f8726cbc70fe081", version : "f8720e7e64d2b639cd3f03d8");
// Also exports the enums used as parameter types
export import(path : "9fc889bb93a3c29feb4f9ae5", version : "70ced71f4553cf02ff68a84b");

/** The default number of holes tied to the end of a nut strip. */
const TIED_HOLE_COUNT_BOUNDS = { (unitless) : [1, 3, 1e3] } as IntegerBoundSpec;

/**
 * Places a nut strip along an edge, or extrudes one from a point.
 */
annotation { "Feature Type Name" : "Robot nut strip",
        "Feature Type Description" : "Add a nut strip along an edge, such as an inside edge of tube, or extrude one from a point." ~ CREDIT,
        "Manipulator Change Function" : "robotNutStripManipulatorChange",
        "Editing Logic Function" : "robotNutStripEditLogic",
        "Icon" : RobotNutStripIcon::BLOB_DATA
    }
export const robotNutStrip = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        stockPlacementPredicate(definition);

        if (isFrc(definition))
        {
            annotation { "Name" : "Nut strip", "Lookup Table" : frcNutStripTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.frcNutStrip is LookupTablePath;
        }
        else
        {
            annotation { "Name" : "Nut strip", "Lookup Table" : ftcNutStripTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.ftcNutStrip is LookupTablePath;
        }

        stockLocationPredicate(definition, "nut strip");

        // Forked from robotFrame, with its own defaults
        annotation { "Name" : "Tie holes to end", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.tieHoles is boolean;

        if (definition.tieHoles)
        {
            annotation { "Group Name" : "Tie holes to end", "Collapsed By Default" : false, "Driving Parameter" : "tieHoles" }
            {
                annotation { "Name" : "Tied holes", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isInteger(definition.tiedHoleCount, TIED_HOLE_COUNT_BOUNDS);

                annotation { "Name" : "Show tied holes", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.showTiedHoles is boolean;
            }
        }
    }
    {
        const nutStrip = getNutStrip(definition);
        placeStock(context, id, definition, nutStrip, "nut strip", function(context is Context, id is Id, location is CoordSystem, length is ValueWithUnits)
            {
                return buildNutStrip(context, id, definition, nutStrip, location, length);
            });
    }, mergeMaps(STOCK_DEFAULTS, { "tiedHoleCount" : 3 }));

/**
 * The selected nut strip's entry in its lookup table (see nutStripTables.py).
 */
function getNutStrip(definition is map) returns map
{
    return isFrc(definition) ?
        getLookupTable(frcNutStripTable, definition.frcNutStrip) :
        getLookupTable(ftcNutStripTable, definition.ftcNutStrip);
}

/**
 * How far the closest hole of a nut strip is from its start: the offset each end of a strip on an edge gets when it's
 * turned on.
 */
function endMargin(nutStrip is map) returns ValueWithUnits
{
    return min(nutStrip.xHoleStart, nutStrip.yHoleStart);
}

/**
 * The holes which count for tying holes to the end: each hole, in either row.
 */
function getNutStripTie(definition is map, nutStrip is map) returns map
{
    return getTie(definition, endMargin(nutStrip), tieUnit(nutStrip));
}

/**
 * How far apart the holes which count for tying holes to the end are.
 */
function tieUnit(nutStrip is map) returns ValueWithUnits
{
    return tolerantEquals(nutStrip.xHoleStart, nutStrip.yHoleStart) ? nutStrip.spacing : abs(nutStrip.xHoleStart - nutStrip.yHoleStart);
}

/**
 * Builds a nut strip `length` long, from `location` along its Z axis, with its width along X.
 *
 * As in robot frame, the strip is its profile extruded along its length, and its holes are cut with tools: its X and
 * Y rows of holes, and its center hole if it has one.
 */
function buildNutStrip(context is Context, id is Id, definition is map, nutStrip is map, location is CoordSystem,
    length is ValueWithUnits) returns map
{
    const tie = getNutStripTie(definition, nutStrip);
    const radius = nutStrip.tapDrillDiameter / 2;

    const profileSketch = newSketchOnPlane(context, id + "profileSketch", { "sketchPlane" : plane(location) });
    skRectangle(profileSketch, "outline", {
                "firstCorner" : vector(-nutStrip.width / 2, -nutStrip.height / 2),
                "secondCorner" : vector(nutStrip.width / 2, nutStrip.height / 2)
            });
    skSolve(profileSketch);
    opExtrude(context, id + "strip", {
                "entities" : qSketchRegion(id + "profileSketch"),
                "direction" : location.zAxis,
                "endBound" : BoundingType.BLIND,
                "endDepth" : length
            });
    const strip = qCreatedBy(id + "strip", EntityType.BODY);

    // Each row is cut from the side its holes go in from: the Y row from the strip's bottom, and the X row from its
    // side. Their sketches have X along the strip's length.
    const rows = [
            {
                "holes" : holePositions(nutStrip.yHoleStart, nutStrip.spacing, radius, length, tie),
                "plane" : plane(toWorld(location, vector(0 * meter, -nutStrip.height / 2, 0 * meter)), yAxis(location), location.zAxis),
                "depth" : nutStrip.height
            },
            {
                "holes" : holePositions(nutStrip.xHoleStart, nutStrip.spacing, radius, length, tie),
                "plane" : plane(toWorld(location, vector(-nutStrip.width / 2, 0 * meter, 0 * meter)), location.xAxis, location.zAxis),
                "depth" : nutStrip.width
            }
        ];
    var tools = [];
    var sketches = [qCreatedBy(id + "profileSketch", EntityType.BODY)];
    for (var i, row in rows)
    {
        if (row.holes == [])
        {
            continue;
        }
        const sketchId = id + ("rowSketch" ~ i);
        const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : row.plane });
        sketchHoles(sketch, row.holes, radius);
        skSolve(sketch);
        opExtrude(context, id + ("rowTools" ~ i), {
                    "entities" : qSketchRegion(sketchId),
                    "direction" : row.plane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : row.depth
                });
        tools = append(tools, qCreatedBy(id + ("rowTools" ~ i), EntityType.BODY));
        sketches = append(sketches, qCreatedBy(sketchId, EntityType.BODY));
    }
    if (nutStrip.centerHole ?? false)
    {
        const centerHoleSketch = newSketchOnPlane(context, id + "centerHoleSketch", { "sketchPlane" : plane(location) });
        skCircle(centerHoleSketch, "hole", { "center" : vector(0, 0) * meter, "radius" : radius });
        skSolve(centerHoleSketch);
        opExtrude(context, id + "centerHoleTool", {
                    "entities" : qSketchRegion(id + "centerHoleSketch"),
                    "direction" : location.zAxis,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : length
                });
        tools = append(tools, qCreatedBy(id + "centerHoleTool", EntityType.BODY));
        sketches = append(sketches, qCreatedBy(id + "centerHoleSketch", EntityType.BODY));
    }
    if (tools != [])
    {
        opBoolean(context, id + "cutHoles", {
                    "tools" : qUnion(tools),
                    "targets" : strip,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
    opDeleteBodies(context, id + "deleteSketches", { "entities" : qUnion(sketches) });

    const holes = findHoles(context, strip, location, rows);
    setTappedThroughHoles(context, id, holes, nutStrip);
    // e.g. 6 in. Nut Strip (WCP 1/2 in., #10-32)
    setStockProperties(context, strip, definition, nutStrip,
        "Nut Strip (" ~ nutStrip.vendor ~ " " ~ nutStrip.sizeName ~ ", " ~ nutStrip.threadName ~ ")", length);

    var tiedHoles = [];
    for (var hole in holes)
    {
        if (hole.tied)
        {
            tiedHoles = append(tiedHoles, hole.faces);
        }
    }
    return {
            "endFace" : qCapEntity(id + "strip", CapType.END, EntityType.FACE),
            "tiedHoles" : qUnion(tiedHoles),
            "tie" : tie
        };
}

function sketchHoles(sketch is Sketch, holes is array, radius is ValueWithUnits)
{
    for (var i, hole in holes)
    {
        skCircle(sketch, "hole" ~ i, { "center" : vector(hole.position, 0 * meter), "radius" : radius });
    }
}

/**
 * The holes in a cut nut strip: maps of their `faces`, `coordSystem` (for `setTappedThroughHoles`), and whether
 * they're `tied`. Each cylindrical face of the strip is matched to a hole by its axis (which says which row it's in, or
 * whether it's the center hole) and where it is along the strip, since a hole crossing another one is split. Holes
 * with no faces are left out.
 */
function findHoles(context is Context, strip is Query, location is CoordSystem, rows is array) returns array
{
    var faces = mapArray(rows, function(row)
        {
            return makeArray(size(row.holes), []);
        });
    var centerFaces = [];
    for (var face in evaluateQuery(context, qOwnedByBody(strip, EntityType.FACE)->qGeometry(GeometryType.CYLINDER)))
    {
        const axis = evSurfaceDefinition(context, { "face" : face }).coordSystem;
        if (parallelVectors(axis.zAxis, location.zAxis))
        {
            centerFaces = append(centerFaces, face);
            continue;
        }
        for (var r, row in rows)
        {
            if (row.holes == [] || !parallelVectors(axis.zAxis, row.plane.normal))
            {
                continue;
            }
            const position = dot(axis.origin - row.plane.origin, row.plane.x);
            var closest = 0;
            for (var i, hole in row.holes)
            {
                if (abs(hole.position - position) < abs(row.holes[closest].position - position))
                {
                    closest = i;
                }
            }
            faces[r][closest] = append(faces[r][closest], face);
        }
    }

    var holes = [];
    for (var r, row in rows)
    {
        for (var i, hole in row.holes)
        {
            if (faces[r][i] != [])
            {
                holes = append(holes, {
                            "faces" : qUnion(faces[r][i]),
                            "coordSystem" : coordSystem(row.plane.origin + row.plane.x * hole.position, row.plane.x, row.plane.normal),
                            "tied" : hole.tied
                        });
            }
        }
    }
    if (centerFaces != [])
    {
        holes = append(holes, {
                    "faces" : qUnion(centerFaces),
                    "coordSystem" : coordSystem(location.origin, location.xAxis, location.zAxis),
                    "tied" : false
                });
    }
    return holes;
}

/**
 * @internal
 * The manipulator change function for robot nut strip.
 */
export function robotNutStripManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return stockManipulatorChange(context, definition, newManipulators);
}

/**
 * @internal
 * The editing logic function for robot nut strip. Each end's offset defaults to its distance to the closest hole.
 */
export function robotNutStripEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    const changed = oldDefinition.program != definition.program ||
        oldDefinition.frcNutStrip != definition.frcNutStrip ||
        oldDefinition.ftcNutStrip != definition.ftcNutStrip;
    return stockEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies,
        endMargin(getNutStrip(definition)), changed);
}
