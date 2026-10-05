FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "core/edgeManipulators.fs", version : "");
import(path : "core/location.fs", version : "");
import(path : "core/mounting.fs", version : "");
import(path : "core/pointManipulator.fs", version : "");
import(path : "core/robotFeature.fs", version : "");
import(path : "core/robotProperties.fs", version : "");
import(path : "core/tappedHole.fs", version : "");
import(path : "nutStrip/nutStripTables.gen.fs", version : "");
// Also exports the enums used as parameter types
export import(path : "core/program.fs", version : "");
export import(path : "core/stdExtrude.fs", version : "");
export import(path : "onshape/std/mateconnectoraxistype.gen.fs", version : "2960.0");

/**
 * How nut strips are placed.
 */
export enum NutStripPlacement
{
    annotation { "Name" : "Edge" }
    EDGE,
    annotation { "Name" : "Point" }
    POINT
}

export predicate isEdgePlacement(definition is map)
{
    definition.placement == NutStripPlacement.EDGE;
}

/**
 * Places nut strips along edges, or extrudes one from a point.
 *
 * Nut strips are drawn from their start, which matters since their holes are spaced from it (and alternate). Edges'
 * flip manipulators and the extrude's opposite direction choose which end that is.
 */
annotation { "Feature Type Name" : "Robot nut strip",
        "Feature Type Description" : "Add nut strips along edges, such as the inside edges of tube, or extrude one from a point." ~ CREDIT,
        "Manipulator Change Function" : "robotNutStripManipulatorChange",
        "Editing Logic Function" : "robotNutStripEditLogic"
    }
export const robotNutStrip = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        programPredicate(definition);

        annotation { "Name" : "Placement", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.placement is NutStripPlacement;

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
            annotation { "Name" : "Edges", "Filter" : EntityType.EDGE && GeometryType.LINE, "UIHint" : ["UNCONFIGURABLE"] }
            definition.edges is Query;

            edgeManipulatorsPredicate(definition);

            annotation { "Name" : "Start offset", "Column Name" : "Has start offset",
                        "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW", "REMEMBER_PREVIOUS_VALUE"] }
            definition.hasStartOffset is boolean;
            if (definition.hasStartOffset)
            {
                annotation { "Name" : "Start offset", "UIHint" : ["DISPLAY_SHORT", "REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.startOffset, NUT_STRIP_OFFSET_BOUNDS);
            }

            annotation { "Name" : "End offset", "Column Name" : "Has end offset",
                        "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW", "REMEMBER_PREVIOUS_VALUE"] }
            definition.hasEndOffset is boolean;
            if (definition.hasEndOffset)
            {
                annotation { "Name" : "End offset", "UIHint" : ["DISPLAY_SHORT", "REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.endOffset, NUT_STRIP_OFFSET_BOUNDS);
            }
        }
        else
        {
            locationPredicate(definition, "nut strip");

            ninePointManipulatorPredicate(definition);

            annotation { "Group Name" : "Extrude", "Collapsed By Default" : false }
            {
                // The opposite direction button flips the direction the nut strip is drawn in, so the rotate button
                // goes next to it, as in the std Transform feature
                newExtrudeEndTypePredicate(definition);
                secondaryAxisPredicate(definition);
                newExtrudeBoundsPredicate(definition);
            }
        }
    }
    {
        const nutStrip = getNutStrip(definition);
        if (isEdgePlacement(definition))
        {
            nutStripsOnEdges(context, id, definition, nutStrip);
        }
        else
        {
            extrudeNutStrip(context, id, definition, nutStrip);
        }
    }, {
            "placement" : NutStripPlacement.EDGE,
            "hasStartOffset" : false,
            "hasEndOffset" : false,
            "parameters" : [],
            "edgeQuery" : qNothing(),
            "oppositeDirection" : false,
            "secondaryAxisType" : MateConnectorAxisType.PLUS_X,
            "index" : NINE_POINT_CENTER_INDEX
        });

/**
 * The selected nut strip's entry in its lookup table (see nutStripTables.py).
 */
function getNutStrip(definition is map) returns map
{
    return isFrc(definition) ?
        getLookupTable(frcNutStripTable, definition.frcNutStrip) :
        getLookupTable(ftcNutStripTable, definition.ftcNutStrip);
}

const START_OFFSET_MANIPULATOR = "startOffsetManipulator";
const END_OFFSET_MANIPULATOR = "endOffsetManipulator";

/**
 * The default end offset: the distance from the end of a WCP nut strip to its closest hole. Editing logic sets the
 * offset for other nut strips (see `endMargin`).
 */
const NUT_STRIP_OFFSET_BOUNDS = {
            (meter) : [-500, 0.00635, 500],
            (centimeter) : 0.635,
            (millimeter) : 6.35,
            (inch) : 0.25,
            (foot) : 0.25 / 12,
            (yard) : 0.25 / 36
        } as LengthBoundSpec;

/**
 * Places a nut strip along each edge, spanning it apart from `startOffset` and `endOffset` at its ends (which are
 * inset when positive), if they're on. The offsets' manipulators are shown on the first edge, when they're on.
 */
function nutStripsOnEdges(context is Context, id is Id, definition is map, nutStrip is map)
{
    const edges = verifyNonemptyQuery(context, definition, "edges", "Select one or more edges to add nut strips to.");
    const parameters = edgeParameters(definition, size(edges));
    const startOffset = definition.hasStartOffset ? definition.startOffset : 0 * meter;
    const endOffset = definition.hasEndOffset ? definition.endOffset : 0 * meter;
    const radius = norm(vector(nutStrip.width, nutStrip.height));
    var longest = 0 * meter;
    for (var i, edge in edges)
    {
        const stripId = id + unstableIdComponent(i);
        setExternalDisambiguation(context, stripId, edge);

        const middle = edgeCoordSystem(context, edge);
        const edgeLength = evLength(context, { "entities" : edge });
        // At the start of the edge, in the direction the strip is drawn
        var location = applyEdgeParameters(middle, parameters[i]);
        location.origin -= location.zAxis * edgeLength / 2;
        const edgeStart = location.origin;
        location.origin += location.zAxis * startOffset;

        const length = edgeLength - startOffset - endOffset;
        if (length < TOLERANCE.zeroLength * meter)
        {
            throw regenError("The offsets leave no room for a nut strip.", ["startOffset", "endOffset"], edge);
        }

        const index = parameters[i].index ?? NINE_POINT_CENTER_INDEX;
        const points = buildNutStrip(context, stripId, definition, nutStrip, location, length, index);
        addEdgeManipulators(context, id, i, middle, parameters[i], points, index, radius);
        longest = max(longest, length);
        if (i == 0 && definition.hasStartOffset)
        {
            addManipulators(context, id, {
                        (START_OFFSET_MANIPULATOR) : linearManipulator({
                                "base" : edgeStart,
                                "direction" : location.zAxis,
                                "offset" : startOffset,
                                "primaryParameterId" : "startOffset"
                            })
                    });
        }
        if (i == 0 && definition.hasEndOffset)
        {
            addManipulators(context, id, {
                        (END_OFFSET_MANIPULATOR) : linearManipulator({
                                "base" : edgeStart + location.zAxis * edgeLength,
                                "direction" : -location.zAxis,
                                "offset" : endOffset,
                                "primaryParameterId" : "endOffset"
                            })
                    });
        }
    }
    warnIfTooLong(context, id, definition, nutStrip, longest, ["edges"]);
}

/**
 * Warns if a nut strip `length` long is longer than the nut strip is sold in.
 */
function warnIfTooLong(context is Context, id is Id, definition is map, nutStrip is map, length is ValueWithUnits, parameters is array)
{
    if (stockFor(nutStrip, length) == undefined)
    {
        const longest = nutStrip.stock[size(nutStrip.stock) - 1].length;
        reportFeatureWarning(context, id, "This nut strip is only sold up to " ~ lengthString(definition, longest) ~ " long.", parameters);
    }
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
 * Extrudes a nut strip from the selected location. Its length and direction come from the extrude options, and it's
 * rotated by `secondaryAxisType`. The nine point manipulator chooses which point of its start lines up with the
 * location.
 */
function extrudeNutStrip(context is Context, id is Id, definition is map, nutStrip is map)
{
    const plane = getLocationPlane(context, definition);

    // Extrude a small face with the extrude options and measure it, so they (and their manipulators) work as usual.
    // Use the top level id to get extrude's manipulators.
    const extrudeDefinition = transformDefintionForNewExtrude(definition, sketchLengthFace(context, id + "lengthFace", plane));
    callSubfeatureAndProcessStatus(id, extrude, context, id, extrudeDefinition, {
                "featureParameterMap" : { "entities" : "location" }
            });
    // Z along the extrude's direction (its opposite direction flip), and X reoriented by secondaryAxisType
    const drawPlane = applyAxisOrientation(definition, plane);
    const extent = evBox3d(context, {
                "topology" : qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID),
                "cSys" : coordSystem(drawPlane),
                "tight" : true
            });
    opDeleteBodies(context, id + "deleteLength", { "entities" : qCreatedBy(id, EntityType.BODY) });

    var location = coordSystem(drawPlane);
    location.origin += drawPlane.normal * extent.minCorner[2];
    const length = extent.maxCorner[2] - extent.minCorner[2];

    const index = getPointIndex(definition, 9);
    addPointManipulator(context, id, definition, buildNutStrip(context, id + "nutStrip", definition, nutStrip, location, length, index));
    warnIfTooLong(context, id, definition, nutStrip, length, ["depth"]);
}

/**
 * Sketches a small circle on `plane` for extrudeNutStrip to extrude, and returns its face.
 */
function sketchLengthFace(context is Context, id is Id, plane is Plane) returns Query
{
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    skCircle(sketch, "circle", { "center" : vector(0, 0) * meter, "radius" : 1 * millimeter });
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}

/**
 * Builds a nut strip `length` long, starting at `location` and running along its Z axis, with its width along X.
 * Which of the nine points of its start (see `ninePointOffsets`) is at `location` is chosen by `index`.
 *
 * The strip and its Y row of holes are one extrude of a sketch; its X row of holes (and center hole, if it has one)
 * are cut with one more.
 *
 * @returns {array} : Where each of the nine points of its start is.
 */
function buildNutStrip(context is Context, id is Id, definition is map, nutStrip is map, location is CoordSystem,
    length is ValueWithUnits, index is number) returns array
{
    if (length < TOLERANCE.zeroLength * meter)
    {
        throw regenError("Nut strips must have a length.");
    }

    const offsets = ninePointOffsets(nutStrip.width, nutStrip.height);
    const center = -offsets[index];

    // Sketched on the strip's bottom with X along its length, so its sketch Y is the strip's X
    const yHoles = holePositions(nutStrip.yHoleStart, nutStrip, length);
    const stripPlane = plane(toWorld(location, center - vector(0 * meter, nutStrip.height / 2, 0 * meter)), yAxis(location), location.zAxis);
    const stripSketch = newSketchOnPlane(context, id + "stripSketch", { "sketchPlane" : stripPlane });
    skRectangle(stripSketch, "outline", {
                "firstCorner" : vector(0 * meter, -nutStrip.width / 2),
                "secondCorner" : vector(length, nutStrip.width / 2)
            });
    sketchHoles(stripSketch, yHoles, nutStrip.tapDrillDiameter);
    skSolve(stripSketch);
    opExtrude(context, id + "strip", {
                "entities" : qSketchRegion(id + "stripSketch", true),
                "direction" : stripPlane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : nutStrip.height
            });
    const strip = qCreatedBy(id + "strip", EntityType.BODY);

    // Sketched on the strip's side with X along its length, so its sketch Y is the strip's -Y
    const xHoles = holePositions(nutStrip.xHoleStart, nutStrip, length);
    const holePlane = plane(toWorld(location, center - vector(nutStrip.width / 2, 0 * meter, 0 * meter)), location.xAxis, location.zAxis);
    if (xHoles != [])
    {
        const holeSketch = newSketchOnPlane(context, id + "holeSketch", { "sketchPlane" : holePlane });
        sketchHoles(holeSketch, xHoles, nutStrip.tapDrillDiameter);
        skSolve(holeSketch);
        opExtrude(context, id + "holeTools", {
                    "entities" : qSketchRegion(id + "holeSketch"),
                    "direction" : holePlane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : nutStrip.width
                });
    }
    const hasCenterHole = nutStrip.centerHole ?? false;
    const centerHole = toWorld(location, center);
    if (hasCenterHole)
    {
        const centerHoleSketch = newSketchOnPlane(context, id + "centerHoleSketch", {
                    "sketchPlane" : plane(centerHole, location.zAxis, location.xAxis)
                });
        skCircle(centerHoleSketch, "hole", { "center" : vector(0, 0) * meter, "radius" : nutStrip.tapDrillDiameter / 2 });
        skSolve(centerHoleSketch);
        opExtrude(context, id + "centerHoleTool", {
                    "entities" : qSketchRegion(id + "centerHoleSketch"),
                    "direction" : location.zAxis,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : length
                });
    }
    const tools = qUnion([qCreatedBy(id + "holeTools", EntityType.BODY), qCreatedBy(id + "centerHoleTool", EntityType.BODY)]);
    if (xHoles != [] || hasCenterHole)
    {
        opBoolean(context, id + "cutHoles", {
                    "tools" : tools,
                    "targets" : strip,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }

    // Found once the holes are cut, since holes which cross are split
    var holes = concatenateArrays([
            holesInRow(context, id + "strip", stripPlane, nutStrip, yHoles),
            holesInRow(context, id + "holeTools", holePlane, nutStrip, xHoles)
        ]);
    if (hasCenterHole)
    {
        holes = append(holes, {
                    "faces" : qCreatedBy(id + "centerHoleTool", EntityType.FACE)->qGeometry(GeometryType.CYLINDER),
                    "coordSystem" : coordSystem(centerHole, location.xAxis, location.zAxis)
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
    setNutStripProperties(context, strip, definition, nutStrip, length);

    return mapArray(offsets, function(offset)
        {
            return toWorld(location, center + offset);
        });
}

/**
 * Where a row of holes goes along a strip `length` long: `spacing` apart from `start`, while they fit whole. So a
 * strip cut from a longer one has the holes it would keep, and stock lengths have the holes they're sold with.
 */
function holePositions(start is ValueWithUnits, nutStrip is map, length is ValueWithUnits) returns array
{
    var positions = [];
    const last = length - nutStrip.tapDrillDiameter / 2 + TOLERANCE.zeroLength * meter;
    for (var position = start; position <= last; position += nutStrip.spacing)
    {
        positions = append(positions, position);
    }
    return positions;
}

function sketchHoles(sketch is Sketch, positions is array, diameter is ValueWithUnits)
{
    for (var i, position in positions)
    {
        skCircle(sketch, "hole" ~ i, { "center" : vector(position, 0 * meter), "radius" : diameter / 2 });
    }
}

/**
 * The holes created by extruding the circles of a row from `holePlane`, for `setTappedThroughHoles`. Faces are matched
 * to holes by where they are along the strip, since a hole crossing another one is split.
 */
function holesInRow(context is Context, extrudeId is Id, holePlane is Plane, nutStrip is map, positions is array) returns array
{
    if (positions == [])
    {
        return [];
    }
    var faces = makeArray(size(positions), []);
    for (var face in evaluateQuery(context, qCreatedBy(extrudeId, EntityType.FACE)->qGeometry(GeometryType.CYLINDER)))
    {
        const axisPoint = evSurfaceDefinition(context, { "face" : face }).coordSystem.origin;
        const i = round((dot(axisPoint - holePlane.origin, holePlane.x) - positions[0]) / nutStrip.spacing);
        faces[i] = append(faces[i], face);
    }
    return mapArray(range(0, size(positions) - 1), function(i)
        {
            const origin = holePlane.origin + holePlane.x * positions[i];
            return { "faces" : qUnion(faces[i]), "coordSystem" : coordSystem(origin, holePlane.x, holePlane.normal) };
        });
}

/**
 * A length in inches for FRC or millimeters for FTC, e.g. `6 in.` or `136 mm`.
 */
function lengthString(definition is map, length is ValueWithUnits) returns string
{
    return isFrc(definition) ?
        roundToPrecision(length / inch, 3) ~ " in." :
        roundToPrecision(length / millimeter, 1) ~ " mm";
}

function setNutStripProperties(context is Context, strip is Query, definition is map, nutStrip is map, length is ValueWithUnits)
{
    // e.g. 6 in. Nut Strip (WCP 1/2 in., #10-32)
    setProperty(context, {
                "entities" : strip,
                "propertyType" : PropertyType.NAME,
                "value" : lengthString(definition, length) ~ " Nut Strip (" ~ nutStrip.vendor ~ " " ~ nutStrip.sizeName ~ ", " ~ nutStrip.threadName ~ ")"
            });
    // The part number of the stock it's cut from, and a link to buy it
    const stock = stockFor(nutStrip, length);
    if (stock != undefined)
    {
        setProperty(context, {
                    "entities" : strip,
                    "propertyType" : PropertyType.PART_NUMBER,
                    "value" : stock.partNumber
                });
    }
    setProperty(context, {
                "entities" : strip,
                "propertyType" : PropertyType.DESCRIPTION,
                "value" : stock == undefined ? nutStrip.url : stock.url
            });
    setProperty(context, {
                "entities" : strip,
                "propertyType" : PropertyType.MATERIAL,
                "value" : ALUMINUM
            });
    setProperty(context, {
                "entities" : strip,
                "propertyType" : PropertyType.APPEARANCE,
                "value" : nutStrip.appearance
            });
}

/**
 * The stock a nut strip `length` long is cut from: the shortest length it's sold in that's long enough, or `undefined`
 * if it's longer than all of them.
 */
function stockFor(nutStrip is map, length is ValueWithUnits)
{
    for (var stock in nutStrip.stock)
    {
        // Lengths are only shown to 3 decimal places, so a strip which looks like stock length is
        if (length <= stock.length + 0.001 * inch)
        {
            return stock;
        }
    }
    return undefined;
}

/**
 * @internal
 * The manipulator change function for robot nut strip.
 */
export function robotNutStripManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    if (isEdgePlacement(definition))
    {
        if (newManipulators[START_OFFSET_MANIPULATOR] != undefined)
        {
            definition.startOffset = newManipulators[START_OFFSET_MANIPULATOR].offset;
        }
        if (newManipulators[END_OFFSET_MANIPULATOR] != undefined)
        {
            definition.endOffset = newManipulators[END_OFFSET_MANIPULATOR].offset;
        }
        return edgeManipulatorsChange(definition, newManipulators);
    }
    definition = pointManipulatorChange(definition, newManipulators);
    return extrudeManipulatorChange(context, definition, newManipulators);
}

/**
 * @internal
 * The editing logic function for robot nut strip.
 */
export function robotNutStripEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    if (isEdgePlacement(definition))
    {
        // When an offset is turned on, or the nut strip changes, offset that end by its distance to the closest hole,
        // unless the offset has been set
        const changed = nutStripChanged(oldDefinition, definition);
        for (var offset in { "startOffset" : "hasStartOffset", "endOffset" : "hasEndOffset" })
        {
            const turnedOn = definition[offset.value] && !(oldDefinition[offset.value] ?? false);
            if ((turnedOn || changed) && !(specifiedParameters[offset.key] ?? false))
            {
                definition[offset.key] = endMargin(getNutStrip(definition));
            }
        }
        return edgeManipulatorsEditLogic(context, oldDefinition, definition);
    }
    definition.entities = qNothing();
    if (!isQueryEmpty(context, definition.location))
    {
        try silent
        {
            definition.entities = sketchLengthFace(context, id + "lengthFace", getLocationPlane(context, definition));
        }
    }
    return stdNewExtrudeEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
}

function nutStripChanged(oldDefinition is map, definition is map) returns boolean
{
    return oldDefinition.program != definition.program ||
        oldDefinition.frcNutStrip != definition.frcNutStrip ||
        oldDefinition.ftcNutStrip != definition.ftcNutStrip;
}
