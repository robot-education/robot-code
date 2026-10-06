FeatureScript 2960;
/**
 * Places linear stock, like nut strips and tube: along edges, or extruded from a point.
 *
 * A feature using this module:
 * - Adds `stockPlacementPredicate` at the top (or `programPredicate` and `placementPredicate` around its own
 *   horizontal enums), then its part's parameters, then `stockEdgePredicate` or
 *   `stockPointPredicate` (see `isEdgePlacement`), then its own tie holes parameters (see `getTie`).
 * - Describes its chosen part with a map of `width` and `height` (its profile's size), `stock` (see `stockFor`),
 *   `appearance`, and `url`, and builds the part with a function (see `placeStock`).
 * - Calls `stockManipulatorChange` from its manipulator change function and `stockEditLogic` from its editing logic.
 *
 * Stock is drawn from its start, which matters since holes are spaced from it: an edge's flip manipulator, or the
 * extrude's opposite direction, chooses which end that is.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "0794d10863d10d98a88c2ab4", version : "599a218f6ba935dcd664345c");
// Exported since features' preconditions use their predicates (and enums) through this module's
export import(path : "0fb7aae3e8fad817927ae062", version : "22c5f762329d1b71c9e2a020");
export import(path : "0195d390c3944cd4fab21ce0", version : "71278ebc72b57aad713b49aa");
export import(path : "58d66340f7b70cfc86606676", version : "88f7f55d3e4918ee4144e69e");
export import(path : "554542fc345271814c4463b0", version : "7b51d4ee67ff00292b209c0a");
export import(path : "3651d7ff6d8577f322b85723", version : "bd1f8dd236054c189347634f");
export import(path : "21762d39019c8b2289e2fbb8", version : "647a15cef06a2b54daee84a1");
export import(path : "onshape/std/mateconnectoraxistype.gen.fs", version : "2960.0");

/**
 * How stock is placed.
 */
export enum StockPlacement
{
    annotation { "Name" : "Edge" }
    EDGE,
    annotation { "Name" : "Point" }
    POINT
}

export predicate isEdgePlacement(definition is map)
{
    definition.placement == StockPlacement.EDGE;
}

/**
 * The program and placement, which go at the top of the feature.
 */
export predicate stockPlacementPredicate(definition is map)
{
    programPredicate(definition);
    placementPredicate(definition);
}

/**
 * Edge or point, for features with more horizontal enums at the top (see `stockPlacementPredicate`).
 */
export predicate placementPredicate(definition is map)
{
    annotation { "Name" : "Placement", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
    definition.placement is StockPlacement;
}

/**
 * The default offset of each end of stock on an edge, when it's turned on. Editing logic may set a better one (see
 * `stockEditLogic`).
 */
const OFFSET_BOUNDS = {
            (meter) : [-500, 0.00635, 500],
            (centimeter) : 0.635,
            (millimeter) : 6.35,
            (inch) : 0.25,
            (foot) : 0.25 / 12,
            (yard) : 0.25 / 36
        } as LengthBoundSpec;

/**
 * The edges to place stock along, and the offsets of its ends.
 */
export predicate stockEdgePredicate(definition is map)
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
        isLength(definition.edgeStartOffset, OFFSET_BOUNDS);
    }

    annotation { "Name" : "End offset", "Column Name" : "Has end offset",
                "UIHint" : ["DISPLAY_SHORT", "FIRST_IN_ROW", "REMEMBER_PREVIOUS_VALUE"] }
    definition.hasEndOffset is boolean;
    if (definition.hasEndOffset)
    {
        annotation { "Name" : "End offset", "UIHint" : ["DISPLAY_SHORT", "REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.edgeEndOffset, OFFSET_BOUNDS);
    }
}

/**
 * The point to extrude stock from, and the extrude's options.
 *
 * @param name : What's placed, e.g. `"nut strip"`.
 */
export predicate stockPointPredicate(definition is map, name is string)
{
    locationPredicate(definition, name);

    ninePointManipulatorPredicate(definition);

    // The opposite direction button flips the direction the stock is drawn in, so the rotate button goes next to it,
    // as in the std Transform feature
    newExtrudeEndTypePredicate(definition);
    secondaryAxisPredicate(definition);
    newExtrudeBoundsPredicate(definition);
}

/**
 * Defaults for the parameters of this module, for a feature's defaults.
 */
export const STOCK_DEFAULTS = {
        "placement" : StockPlacement.EDGE,
        "hasStartOffset" : false,
        "hasEndOffset" : false,
        "parameters" : [],
        "edgeQuery" : qNothing(),
        "oppositeDirection" : false,
        "secondaryAxisType" : MateConnectorAxisType.PLUS_X,
        "index" : NINE_POINT_CENTER_INDEX,
        "tieHoles" : false,
        "showTiedHoles" : false
    };

// Tying holes to the end

/**
 * How holes are spaced along stock, and how many near its end are tied to it, from a feature's `tieHoles` and
 * `tiedHoleCount` parameters.
 *
 * Holes are spaced from the start of stock, so its pattern is the same either way. Tying holes to the end gives the
 * last `count` holes that count (and the holes around them) identities from the end instead: they're made from the
 * end, so they stay put relative to it as the stock's length changes.
 *
 * @param start : How far the first hole that counts is from the start of the stock.
 * @param unit : How far apart the holes that count are.
 * @returns {{
 *      @field count {number} : How many holes are tied to the end, or 0.
 *      @field start {ValueWithUnits}
 *      @field unit {ValueWithUnits}
 * }}
 */
export function getTie(definition is map, start is ValueWithUnits, unit is ValueWithUnits) returns map
{
    return { "count" : definition.tieHoles ? definition.tiedHoleCount : 0, "start" : start, "unit" : unit };
}

/**
 * How many times stock `length` long fits the `unit` between its first and last holes that count: its regular part
 * is `2 * tie.start + spacings * tie.unit` long, and the rest is at its end.
 */
function regularSpacings(length is ValueWithUnits, tie is map) returns number
{
    return floor((length - 2 * tie.start + TOLERANCE.zeroLength * meter) / tie.unit);
}

/**
 * Where a row of holes (or groups of them) goes along stock `length` long: maps of `position`, and whether it's `tied`
 * to the end (see `getTie`), in order from the start. Only holes which fit whole in its regular part are kept, so if
 * it isn't a whole number of spacings long (see `isRegularLength`), the extra length at its end has none.
 *
 * @param start : The position of the row's first hole.
 * @param pitch : How far apart the row's holes are.
 * @param extent : How far each hole reaches along the stock from its position, e.g. its radius.
 * @param tie : @see `getTie`
 */
export function holePositions(start is ValueWithUnits, pitch is ValueWithUnits, extent is ValueWithUnits, length is ValueWithUnits,
    tie is map) returns array
{
    const tolerance = TOLERANCE.zeroLength * meter;
    const spacings = regularSpacings(length, tie);
    const end = 2 * tie.start + spacings * tie.unit;
    // Holes are tied if they're around the last holes that count
    const firstTied = spacings - tie.count + 1;
    var positions = [];
    for (var position = start; spacings >= 0 && position + extent <= end + tolerance; position += pitch)
    {
        const spacing = floor((position - tie.start + tie.unit / 2 + tolerance) / tie.unit);
        positions = append(positions, { "position" : position, "tied" : tie.count > 0 && spacing >= firstTied });
    }
    return positions;
}

/**
 * Whether stock `length` long is a whole number of hole spacings long (see `getTie`), so the margin after its last hole
 * that counts is the same as the one before its first. Otherwise, the extra length is at its end.
 */
export function isRegularLength(length is ValueWithUnits, tie is map) returns boolean
{
    return length - 2 * tie.start - max(regularSpacings(length, tie), 0) * tie.unit < TOLERANCE.zeroLength * meter;
}

// Placing

const START_OFFSET_MANIPULATOR = "startOffsetManipulator";
const END_OFFSET_MANIPULATOR = "endOffsetManipulator";

/**
 * Places stock along the selected edges, or extrudes it from the selected point.
 *
 * @param part {{
 *      @field width {ValueWithUnits} : The width of its profile, along X.
 *      @field height {ValueWithUnits} : The height of its profile, along Y.
 *      @field stock {array} : @seealso [stockFor]
 * }}
 * @param build {function} : Builds the stock: `build(context is Context, id is Id, location is CoordSystem, length is
 *          ValueWithUnits) returns map`. `location` is the middle of its start, with Z along its length and its width
 *          along X. It returns a map of `endFace` (the face at its end) and `tiedHoles` (the faces of holes tied to
 *          it), which are highlighted when `showTiedHoles` is on, and whether it's `irregular` (see
 *          `isRegularLength`), which is warned about.
 */
export function placeStock(context is Context, id is Id, definition is map, part is map, build is function)
{
    var longest = 0 * meter;
    var highlighted = [];
    var irregular = false;
    if (isEdgePlacement(definition))
    {
        const edges = verifyNonemptyQuery(context, definition, "edges", "Select one or more edges to place on.");
        const parameters = edgeParameters(definition, size(edges));
        const startOffset = definition.hasStartOffset ? definition.edgeStartOffset : 0 * meter;
        const endOffset = definition.hasEndOffset ? definition.edgeEndOffset : 0 * meter;
        const radius = norm(vector(part.width, part.height));
        for (var i, edge in edges)
        {
            const stockId = id + unstableIdComponent(i);
            setExternalDisambiguation(context, stockId, edge);

            const middle = edgeCoordSystem(context, edge);
            const edgeLength = evLength(context, { "entities" : edge });
            // At the start of the edge, in the direction the stock is drawn
            var location = applyEdgeParameters(middle, parameters[i]);
            location.origin -= location.zAxis * edgeLength / 2;
            const edgeStart = location.origin;
            location.origin += location.zAxis * startOffset;

            const length = edgeLength - startOffset - endOffset;
            if (length < TOLERANCE.zeroLength * meter)
            {
                throw regenError("The offsets leave no room.", ["edgeStartOffset", "edgeEndOffset"], edge);
            }

            const index = parameters[i].index ?? NINE_POINT_CENTER_INDEX;
            const built = buildAtPoint(context, stockId, part, location, length, index, build);
            highlighted = append(highlighted, built.highlighted);
            irregular = irregular || built.irregular;
            addEdgeManipulators(context, id, i, middle, parameters[i], built.points, index, radius);
            longest = max(longest, length);
            if (i == 0)
            {
                addOffsetManipulators(context, id, definition, edgeStart, location.zAxis, edgeLength, startOffset, endOffset);
            }
        }
    }
    else
    {
        const extruded = extrudeLength(context, id, definition);
        const built = buildAtPoint(context, id + "stock", part, extruded.location, extruded.length, getPointIndex(definition, 9), build);
        highlighted = append(highlighted, built.highlighted);
        irregular = built.irregular;
        addPointManipulator(context, id, definition, built.points);
        longest = extruded.length;
    }

    if (definition.tieHoles && definition.showTiedHoles)
    {
        addDebugEntities(context, qUnion(highlighted), DebugColor.BLUE);
    }
    var warnings = [];
    if (part.stock != [] && stockFor(part, longest) == undefined)
    {
        warnings = append(warnings, "This is only sold up to " ~ lengthString(definition, part.stock[size(part.stock) - 1].length) ~ " long.");
    }
    if (irregular)
    {
        warnings = append(warnings, "This isn't a whole number of hole spacings long, so the extra length is at the end.");
    }
    if (warnings != [])
    {
        reportFeatureWarning(context, id, join(warnings, " "), [isEdgePlacement(definition) ? "edges" : "depth"]);
    }
}

/**
 * Builds stock with the point `index` of the nine points of its start at `location`.
 */
function buildAtPoint(context is Context, id is Id, part is map, location is CoordSystem, length is ValueWithUnits, index is number,
    build is function) returns map
{
    const offsets = ninePointOffsets(part.width, part.height);
    var center = location;
    center.origin = toWorld(location, -offsets[index]);
    const built = build(context, id, center, length);
    return {
            "points" : mapArray(offsets, function(offset)
                {
                    return toWorld(center, offset);
                }),
            "highlighted" : qUnion([built.endFace ?? qNothing(), built.tiedHoles ?? qNothing()]),
            "irregular" : built.irregular ?? false
        };
}

function addOffsetManipulators(context is Context, id is Id, definition is map, edgeStart is Vector, direction is Vector,
    edgeLength is ValueWithUnits, startOffset is ValueWithUnits, endOffset is ValueWithUnits)
{
    if (definition.hasStartOffset)
    {
        addManipulators(context, id, {
                    (START_OFFSET_MANIPULATOR) : linearManipulator({
                            "base" : edgeStart,
                            "direction" : direction,
                            "offset" : startOffset,
                            "primaryParameterId" : "edgeStartOffset"
                        })
                });
    }
    if (definition.hasEndOffset)
    {
        addManipulators(context, id, {
                    (END_OFFSET_MANIPULATOR) : linearManipulator({
                            "base" : edgeStart + direction * edgeLength,
                            "direction" : -direction,
                            "offset" : endOffset,
                            "primaryParameterId" : "edgeEndOffset"
                        })
                });
    }
}

/**
 * Extrudes a small face from the selected point with the extrude options and measures it, so they (and their
 * manipulators) work as usual. Returns the `location` stock starts at (with Z along the extrude's direction, and X
 * reoriented by `secondaryAxisType`) and its `length`.
 */
function extrudeLength(context is Context, id is Id, definition is map) returns map
{
    const plane = getLocationPlane(context, definition);
    // Use the top level id to get extrude's manipulators
    const extrudeDefinition = transformDefintionForNewExtrude(definition, sketchLengthFace(context, id + "lengthFace", plane));
    callSubfeatureAndProcessStatus(id, extrude, context, id, extrudeDefinition, {
                "featureParameterMap" : { "entities" : "location" }
            });
    const drawPlane = applyAxisOrientation(definition, plane);
    const extent = evBox3d(context, {
                "topology" : qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID),
                "cSys" : coordSystem(drawPlane),
                "tight" : true
            });
    opDeleteBodies(context, id + "deleteLength", { "entities" : qCreatedBy(id, EntityType.BODY) });

    var location = coordSystem(drawPlane);
    location.origin += drawPlane.normal * extent.minCorner[2];
    return { "location" : location, "length" : extent.maxCorner[2] - extent.minCorner[2] };
}

function sketchLengthFace(context is Context, id is Id, plane is Plane) returns Query
{
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    skCircle(sketch, "circle", { "center" : vector(0, 0) * meter, "radius" : 1 * millimeter });
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}

// Properties

/**
 * The stock a part `length` long is cut from: the shortest length it's sold in that's long enough, or `undefined` if
 * it's longer than all of them.
 *
 * @param part : Has a `stock` array of the lengths it's sold in, shortest first: maps of `length`, `partNumber`, and
 *          `url`.
 */
export function stockFor(part is map, length is ValueWithUnits)
{
    for (var stock in part.stock)
    {
        // Lengths are only shown to 3 decimal places, so a part which looks like stock length is
        if (length <= stock.length + 0.001 * inch)
        {
            return stock;
        }
    }
    return undefined;
}

/**
 * A length in inches for FRC or millimeters for FTC, e.g. `6 in.` or `136 mm`.
 */
export function lengthString(definition is map, length is ValueWithUnits) returns string
{
    return isFrc(definition) ?
        roundToPrecision(length / inch, 3) ~ " in." :
        roundToPrecision(length / millimeter, 1) ~ " mm";
}

/**
 * Names stock (its length, then `name`), gives it the part number of the stock it's cut from and a link to buy it,
 * and sets its material and appearance.
 */
export function setStockProperties(context is Context, body is Query, definition is map, part is map, name is string, length is ValueWithUnits)
{
    setProperty(context, {
                "entities" : body,
                "propertyType" : PropertyType.NAME,
                "value" : lengthString(definition, length) ~ " " ~ name
            });
    const stock = stockFor(part, length);
    if (stock != undefined)
    {
        setProperty(context, {
                    "entities" : body,
                    "propertyType" : PropertyType.PART_NUMBER,
                    "value" : stock.partNumber
                });
    }
    const url = stock == undefined ? part.url : stock.url;
    if (url != "")
    {
        setProperty(context, {
                    "entities" : body,
                    "propertyType" : PropertyType.DESCRIPTION,
                    "value" : url
                });
    }
    setProperty(context, {
                "entities" : body,
                "propertyType" : PropertyType.MATERIAL,
                "value" : ALUMINUM
            });
    setProperty(context, {
                "entities" : body,
                "propertyType" : PropertyType.APPEARANCE,
                "value" : part.appearance
            });
}

// Manipulators and editing logic

/**
 * Applies the manipulators' changes. Call from the feature's manipulator change function.
 */
export function stockManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    if (isEdgePlacement(definition))
    {
        if (newManipulators[START_OFFSET_MANIPULATOR] != undefined)
        {
            definition.edgeStartOffset = newManipulators[START_OFFSET_MANIPULATOR].offset;
        }
        if (newManipulators[END_OFFSET_MANIPULATOR] != undefined)
        {
            definition.edgeEndOffset = newManipulators[END_OFFSET_MANIPULATOR].offset;
        }
        return edgeManipulatorsChange(definition, newManipulators);
    }
    definition = pointManipulatorChange(definition, newManipulators);
    return extrudeManipulatorChange(context, definition, newManipulators);
}

/**
 * Call from the feature's editing logic function.
 *
 * @param defaultOffset : What each end's offset is set to when it's turned on (or when `partChanged`), unless it's been
 *          set; `undefined` to keep the bounds' default.
 * @param partChanged : Whether the part has changed.
 */
export function stockEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, specifiedParameters is map,
    hiddenBodies is Query, defaultOffset, partChanged is boolean) returns map
{
    if (isEdgePlacement(definition))
    {
        if (defaultOffset != undefined)
        {
            for (var offset in { "edgeStartOffset" : "hasStartOffset", "edgeEndOffset" : "hasEndOffset" })
            {
                const turnedOn = definition[offset.value] && !(oldDefinition[offset.value] ?? false);
                if ((turnedOn || partChanged) && !(specifiedParameters[offset.key] ?? false))
                {
                    definition[offset.key] = defaultOffset;
                }
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
