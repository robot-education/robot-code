FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "core/robotFeature.fs", version : "");
import(path : "core/tappedHole.fs", version : "");
import(path : "nutStrip/nutStripTables.gen.fs", version : "");
// Also exports the enums used as parameter types
export import(path : "core/linearStock.fs", version : "");

/** The default number of holes tied to the end of a nut strip. */
const TIED_HOLE_COUNT_BOUNDS = { (unitless) : [1, 1, 1e3] } as IntegerBoundSpec;

/**
 * Places nut strips along edges, or extrudes one from a point.
 */
annotation { "Feature Type Name" : "Robot nut strip",
        "Feature Type Description" : "Add nut strips along edges, such as the inside edges of tube, or extrude one from a point." ~ CREDIT,
        "Manipulator Change Function" : "robotNutStripManipulatorChange",
        "Editing Logic Function" : "robotNutStripEditLogic"
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

        if (isEdgePlacement(definition))
        {
            stockEdgePredicate(definition);
        }
        else
        {
            stockPointPredicate(definition, "nut strip");
        }

        // Forked from robotFrame, with its own defaults
        annotation { "Name" : "Tie holes to end", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.tieHoles is boolean;

        if (definition.tieHoles)
        {
            annotation { "Group Name" : "Tie holes to end", "Collapsed By Default" : false, "Driving Parameter" : "tieHoles" }
            {
                annotation { "Name" : "Tied holes", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isInteger(definition.tiedHoleCount, TIED_HOLE_COUNT_BOUNDS);

                annotation { "Name" : "Show tied holes", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.showTiedHoles is boolean;
            }
        }
    }
    {
        const nutStrip = getNutStrip(definition);
        placeStock(context, id, definition, nutStrip, function(context is Context, id is Id, location is CoordSystem, length is ValueWithUnits)
            {
                return buildNutStrip(context, id, definition, nutStrip, location, length);
            });
    }, mergeMaps(STOCK_DEFAULTS, { "tiedHoleCount" : 1 }));

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
function getNutStripTie(definition is map, nutStrip is map)
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
 * The strip and its Y row of holes are one extrude of a sketch; its X row of holes (and center hole, if it has one)
 * are cut with one more.
 */
function buildNutStrip(context is Context, id is Id, definition is map, nutStrip is map, location is CoordSystem,
    length is ValueWithUnits) returns map
{
    const tie = getNutStripTie(definition, nutStrip);
    const radius = nutStrip.tapDrillDiameter / 2;

    // Sketched on the strip's bottom with X along its length, so its sketch Y is the strip's X
    const yHoles = holePositions(nutStrip.yHoleStart, nutStrip.spacing, radius, length, tie);
    const stripPlane = plane(toWorld(location, vector(0 * meter, -nutStrip.height / 2, 0 * meter)), yAxis(location), location.zAxis);
    const stripSketch = newSketchOnPlane(context, id + "stripSketch", { "sketchPlane" : stripPlane });
    skRectangle(stripSketch, "outline", {
                "firstCorner" : vector(0 * meter, -nutStrip.width / 2),
                "secondCorner" : vector(length, nutStrip.width / 2)
            });
    sketchHoles(stripSketch, yHoles, radius);
    skSolve(stripSketch);
    opExtrude(context, id + "strip", {
                "entities" : qSketchRegion(id + "stripSketch", true),
                "direction" : stripPlane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : nutStrip.height
            });
    const strip = qCreatedBy(id + "strip", EntityType.BODY);

    // Sketched on the strip's side with X along its length, so its sketch Y is the strip's -Y
    const xHoles = holePositions(nutStrip.xHoleStart, nutStrip.spacing, radius, length, tie);
    const holePlane = plane(toWorld(location, vector(-nutStrip.width / 2, 0 * meter, 0 * meter)), location.xAxis, location.zAxis);
    if (xHoles != [])
    {
        const holeSketch = newSketchOnPlane(context, id + "holeSketch", { "sketchPlane" : holePlane });
        sketchHoles(holeSketch, xHoles, radius);
        skSolve(holeSketch);
        opExtrude(context, id + "holeTools", {
                    "entities" : qSketchRegion(id + "holeSketch"),
                    "direction" : holePlane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : nutStrip.width
                });
    }
    const hasCenterHole = nutStrip.centerHole ?? false;
    if (hasCenterHole)
    {
        const centerHoleSketch = newSketchOnPlane(context, id + "centerHoleSketch", {
                    "sketchPlane" : plane(location.origin, location.zAxis, location.xAxis)
                });
        skCircle(centerHoleSketch, "hole", { "center" : vector(0, 0) * meter, "radius" : radius });
        skSolve(centerHoleSketch);
        opExtrude(context, id + "centerHoleTool", {
                    "entities" : qSketchRegion(id + "centerHoleSketch"),
                    "direction" : location.zAxis,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : length
                });
    }
    if (xHoles != [] || hasCenterHole)
    {
        opBoolean(context, id + "cutHoles", {
                    "tools" : qUnion([qCreatedBy(id + "holeTools", EntityType.BODY), qCreatedBy(id + "centerHoleTool", EntityType.BODY)]),
                    "targets" : strip,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }

    // Found once the holes are cut, since holes which cross are split
    const rows = concatenateArrays([
                holesInRow(context, id + "strip", stripPlane, yHoles),
                holesInRow(context, id + "holeTools", holePlane, xHoles)
            ]);
    var holes = mapArray(rows, function(hole)
        {
            return { "faces" : hole.faces, "coordSystem" : hole.coordSystem };
        });
    if (hasCenterHole)
    {
        holes = append(holes, {
                    "faces" : qCreatedBy(id + "centerHoleTool", EntityType.FACE)->qGeometry(GeometryType.CYLINDER),
                    "coordSystem" : coordSystem(location.origin, location.xAxis, location.zAxis)
                });
    }
    opDeleteBodies(context, id + "deleteSketches", {
                "entities" : qUnion([
                        qCreatedBy(id + "stripSketch", EntityType.BODY),
                        qCreatedBy(id + "holeSketch", EntityType.BODY),
                        qCreatedBy(id + "centerHoleSketch", EntityType.BODY)
                    ])
            });

    setTappedThroughHoles(context, id, holes, nutStrip);
    // e.g. 6 in. Nut Strip (WCP 1/2 in., #10-32)
    setStockProperties(context, strip, definition, nutStrip,
        "Nut Strip (" ~ nutStrip.vendor ~ " " ~ nutStrip.sizeName ~ ", " ~ nutStrip.threadName ~ ")", length);

    var tiedHoles = [];
    for (var hole in rows)
    {
        if (hole.tied)
        {
            tiedHoles = append(tiedHoles, hole.faces);
        }
    }
    return {
            "endFace" : qOwnedByBody(strip, EntityType.FACE)->qGeometry(GeometryType.PLANE)->qContainsPoint(location.origin + location.zAxis * length),
            "tiedHoles" : qUnion(tiedHoles),
            "irregular" : !isRegularLength(length, endMargin(nutStrip), tieUnit(nutStrip))
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
 * The holes created by extruding the circles of a row from `holePlane`: maps of their `faces`, `coordSystem` (for
 * `setTappedThroughHoles`), and whether they're `tied`. Faces are matched to holes by where they are along the strip,
 * since a hole crossing another one is split.
 */
function holesInRow(context is Context, extrudeId is Id, holePlane is Plane, holes is array) returns array
{
    var faces = makeArray(size(holes), []);
    for (var face in evaluateQuery(context, qCreatedBy(extrudeId, EntityType.FACE)->qGeometry(GeometryType.CYLINDER)))
    {
        const position = dot(evSurfaceDefinition(context, { "face" : face }).coordSystem.origin - holePlane.origin, holePlane.x);
        var closest = 0;
        for (var i, hole in holes)
        {
            if (abs(hole.position - position) < abs(holes[closest].position - position))
            {
                closest = i;
            }
        }
        faces[closest] = append(faces[closest], face);
    }
    return mapArray(range(0, size(holes) - 1), function(i)
        {
            const origin = holePlane.origin + holePlane.x * holes[i].position;
            return {
                    "faces" : qUnion(faces[i]),
                    "coordSystem" : coordSystem(origin, holePlane.x, holePlane.normal),
                    "tied" : holes[i].tied
                };
        });
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
