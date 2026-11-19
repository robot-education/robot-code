FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

// Exports Fit and SplineType, parameter types
export import(path : "926d933eb33b11a3452660fd", version : "d69c937bc00dba371714fef9");
export import(path : "6e24956e9977116c79280620", version : "4bf61c29c6573723844585f5");
import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
import(path : "eb11a2948f8123134339137f", version : "2209aff42808fb5a7c367b91");
// SplineXS bores at each standard fit
import(path : "35acc10879cd067431ae1122", version : "d6247ba8d64267eb0851d9fc");

/**
 * Bores through parts on a shaft (sprockets, gears, and the like): a hex, round, or spline bore, and optionally an
 * entrance chamfer. A standard hex's, or a spline's, has a fit on its shaft; a round bore (a hole) and a custom hex are
 * the size given.
 */
export enum BoreShape
{
    annotation { "Name" : "Hex" }
    HEX,
    annotation { "Name" : "Round" }
    ROUND,
    annotation { "Name" : "Spline" }
    SPLINE
}

/**
 * A hex bore's size: a standard hex shaft's (1/2 in. or 3/8 in. across its flats), with a fit, or a custom width.
 */
export enum BoreHexSize
{
    annotation { "Name" : "1/2 in." }
    _1_2_IN,
    annotation { "Name" : "3/8 in." }
    _3_8_IN,
    annotation { "Name" : "Custom" }
    CUSTOM
}

const BORE_HEX_WIDTHS = { (BoreHexSize._1_2_IN) : 0.5 * inch, (BoreHexSize._3_8_IN) : 0.375 * inch };

const HEX_WIDTH_BOUNDS = { (meter) : [1e-4, 0.0127, 500], (inch) : 0.5, (millimeter) : 12 } as LengthBoundSpec;
const ROUND_DIAMETER_BOUNDS = { (meter) : [1e-4, 0.00635, 500], (inch) : 0.25, (millimeter) : 8 } as LengthBoundSpec;
export const ENTRANCE_CHAMFER_BOUNDS = { (meter) : [1e-5, 0.0005, 500], (inch) : 1 / 64, (millimeter) : 0.5 } as LengthBoundSpec;

/**
 * Entrance chamfer, and its distance: a chamfer around a bore's ends, to guide a shaft in.
 */
export predicate entranceChamferPredicate(definition is map)
{
    annotation { "Name" : "Entrance chamfer", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.entranceChamfer is boolean;

    if (definition.entranceChamfer)
    {
        annotation { "Group Name" : "Entrance chamfer", "Collapsed By Default" : false, "Driving Parameter" : "entranceChamfer" }
        {
            annotation { "Name" : "Chamfer distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.chamferDistance, ENTRANCE_CHAMFER_BOUNDS);
        }
    }
}

/**
 * Whether a bore has a fit on its shaft: a standard hex or a spline does; a round bore is a hole of its diameter, and a
 * custom hex's width is the hole's.
 */
export predicate boreHasFit(definition is map)
{
    definition.boreShape != BoreShape.ROUND;
    definition.boreShape != BoreShape.HEX || definition.boreHexSize != BoreHexSize.CUSTOM;
}

/**
 * Add bore, and the bore's shape, size, fit (see `boreHasFit`), and entrance chamfer.
 */
export predicate borePredicate(definition is map)
{
    annotation { "Name" : "Add bore", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.addBore is boolean;

    if (definition.addBore)
    {
        annotation { "Group Name" : "Add bore", "Collapsed By Default" : false, "Driving Parameter" : "addBore" }
        {
            annotation { "Name" : "Bore", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.boreShape is BoreShape;

            if (definition.boreShape == BoreShape.HEX)
            {
                annotation { "Name" : "Hex size", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                definition.boreHexSize is BoreHexSize;

                if (definition.boreHexSize == BoreHexSize.CUSTOM)
                {
                    annotation { "Name" : "Width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Across the hex's flats." }
                    isLength(definition.hexWidth, HEX_WIDTH_BOUNDS);
                }
            }
            else if (definition.boreShape == BoreShape.ROUND)
            {
                annotation { "Name" : "Diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.boreDiameter, ROUND_DIAMETER_BOUNDS);
            }
            else
            {
                annotation { "Name" : "Spline type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                definition.splineType is SplineType;
            }

            // Of the bore on its shaft
            if (boreHasFit(definition))
            {
                fitPredicate(definition);
            }

            entranceChamferPredicate(definition);
        }
    }
}

/**
 * Whether the mating gear's own bore (`mateBorePredicate`) has a fit, as `boreHasFit`.
 */
export predicate mateBoreHasFit(definition is map)
{
    definition.mateBoreShape != BoreShape.ROUND;
    definition.mateBoreShape != BoreShape.HEX || definition.mateBoreHexSize != BoreHexSize.CUSTOM;
}

/**
 * A second bore, as `borePredicate`'s, for a feature's second part: a gear pair's mating gear, when it has its own.
 * Its parameters are `borePredicate`'s, with `mate` before them (`mateAddBore`, `mateBoreShape`, ...), its fit's too
 * (`mateBoreFitPredicate`).
 */
export predicate mateBorePredicate(definition is map)
{
    annotation { "Name" : "Add bore", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.mateAddBore is boolean;

    if (definition.mateAddBore)
    {
        annotation { "Group Name" : "Add bore", "Collapsed By Default" : false, "Driving Parameter" : "mateAddBore" }
        {
            annotation { "Name" : "Bore", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.mateBoreShape is BoreShape;

            if (definition.mateBoreShape == BoreShape.HEX)
            {
                annotation { "Name" : "Hex size", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                definition.mateBoreHexSize is BoreHexSize;

                if (definition.mateBoreHexSize == BoreHexSize.CUSTOM)
                {
                    annotation { "Name" : "Width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Across the hex's flats." }
                    isLength(definition.mateHexWidth, HEX_WIDTH_BOUNDS);
                }
            }
            else if (definition.mateBoreShape == BoreShape.ROUND)
            {
                annotation { "Name" : "Diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.mateBoreDiameter, ROUND_DIAMETER_BOUNDS);
            }
            else
            {
                annotation { "Name" : "Spline type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                definition.mateSplineType is SplineType;
            }

            if (mateBoreHasFit(definition))
            {
                mateBoreFitPredicate(definition);
            }

            annotation { "Name" : "Entrance chamfer", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.mateEntranceChamfer is boolean;

            if (definition.mateEntranceChamfer)
            {
                annotation { "Group Name" : "Entrance chamfer", "Collapsed By Default" : false, "Driving Parameter" : "mateEntranceChamfer" }
                {
                    annotation { "Name" : "Chamfer distance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(definition.mateChamferDistance, ENTRANCE_CHAMFER_BOUNDS);
                }
            }
        }
    }
}

/**
 * A bore's shape and size: what `cutBores` cuts.
 *
 * @type {{
 *      @field shape {BoreShape} :
 *      @field across {ValueWithUnits} : A hex's width across its flats, or a round bore's diameter (for a spline, its
 *              spline's diameter): what its fit is for.
 *      @field splineType {SplineType} : @optional A spline bore's.
 *      @field clearance {ValueWithUnits} : How much bigger than the shaft it is, across.
 *      @field fit {Fit} : @optional The fit its clearance is for: a SplineXS bore at a standard fit is sketched with it
 *              (`SPLINE_XS_BORES`), rather than offset.
 *      @field chamfer {ValueWithUnits} : @optional Its entrance chamfer's distance, if it has one.
 *      @field parameters {array} : The parameters which set it, for errors: its shape's, size's, and fit's.
 *      @field chamferParameters {array} : Its entrance chamfer's.
 * }}
 */
export type Bore typecheck canBeBore;

export predicate canBeBore(value)
{
    value is map;
    value.shape is BoreShape;
    isLength(value.across);
    value.shape != BoreShape.SPLINE || value.splineType is SplineType;
    isLength(value.clearance);
    value.fit == undefined || value.fit is Fit;
    value.chamfer == undefined || isLength(value.chamfer);
    value.parameters is array;
    value.chamferParameters is array;
}

/**
 * The bore `borePredicate` declares, or `undefined` without one: with its fit's clearance (see `boreHasFit`), or none.
 */
export function definitionBore(definition is map)
{
    if (!definition.addBore)
    {
        return undefined;
    }
    const across = switch (definition.boreShape) {
                BoreShape.HEX : definition.boreHexSize == BoreHexSize.CUSTOM ? definition.hexWidth : BORE_HEX_WIDTHS[definition.boreHexSize],
                BoreShape.ROUND : definition.boreDiameter,
                BoreShape.SPLINE : splineDiameter(definition.splineType)
            };
    const hasFit = boreHasFit(definition);
    return {
                "shape" : definition.boreShape,
                "across" : across,
                "splineType" : definition.splineType,
                "clearance" : hasFit ? fitClearance(definition, across) : 0 * meter,
                "fit" : hasFit ? definition.fit : Fit.NONE,
                "chamfer" : definition.entranceChamfer ? definition.chamferDistance : undefined,
                "parameters" : BORE_PARAMETERS,
                "chamferParameters" : ["chamferDistance"]
            } as Bore;
}

export const BORE_PARAMETERS = ["boreShape", "boreHexSize", "hexWidth", "boreDiameter", "splineType", "fit", "fitClearance"];

/**
 * The parameters of the mating gear's own bore (`mateBorePredicate`), as `BORE_PARAMETERS`.
 */
export const MATE_BORE_PARAMETERS = ["mateBoreShape", "mateBoreHexSize", "mateHexWidth", "mateBoreDiameter", "mateSplineType", "mateFit", "mateFitClearance"];

/**
 * The bore `mateBorePredicate` declares, or `undefined` without one, as `definitionBore`.
 */
export function definitionMateBore(definition is map)
{
    var bore = definitionBore({
                "addBore" : definition.mateAddBore,
                "boreShape" : definition.mateBoreShape,
                "boreHexSize" : definition.mateBoreHexSize,
                "hexWidth" : definition.mateHexWidth,
                "boreDiameter" : definition.mateBoreDiameter,
                "splineType" : definition.mateSplineType,
                "fit" : definition.mateFit,
                "fitClearance" : definition.mateFitClearance,
                "entranceChamfer" : definition.mateEntranceChamfer,
                "chamferDistance" : definition.mateChamferDistance
            });
    if (bore != undefined)
    {
        bore.parameters = MATE_BORE_PARAMETERS;
        bore.chamferParameters = ["mateChamferDistance"];
    }
    return bore;
}

/**
 * Cuts `bore` through each of `targets` (the parts), centered on its plane in `planes` (along its normal), with
 * `identities` (queries, or undefined) to disambiguate them. If the cut or its chamfer fails, the parts are shown in
 * blue, with what failed in red. If the bore's bigger than a part, the cut uses it up, so `reconstructOp` (a function
 * of an `Id`) builds the parts again under the id it's given, to show in blue.
 */
export function cutBores(context is Context, id is Id, bore is Bore, planes is array, identities is array, targets is array, reconstructOp is function)
{
    const tools = boreTools(context, id, bore, planes, identities);
    // The bore's edges inside the parts, to tell its entrance edges from
    const insideEdges = startTracking(context, qNonCapEntity(id, EntityType.EDGE));
    try
    {
        opBoolean(context, id + "cut", {
                    "tools" : tools,
                    "targets" : qUnion(targets),
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
    catch
    {
        // A failed boolean changes nothing, so the bores and the parts are still there to show
        addDebugEntities(context, qUnion(targets), DebugColor.BLUE);
        throw regenError("Failed to cut bore.", bore.parameters, tools);
    }
    for (var target in targets)
    {
        if (isQueryEmpty(context, target))
        {
            // The bore's bigger than the part: the cut used it up, with the bores, so they're made again to show
            reconstructOp(id + "errorParts");
            addDebugEntities(context, qCreatedBy(id + "errorParts", EntityType.BODY)->qBodyType(BodyType.SOLID), DebugColor.BLUE);
            throw regenError("The bore is bigger than the part.", bore.parameters, boreTools(context, id + "error", bore, planes, identities));
        }
    }

    if (bore.chamfer != undefined)
    {
        const entranceEdges = qSubtraction(qCreatedBy(id + "cut", EntityType.EDGE), insideEdges);
        try
        {
            opChamfer(context, id + "chamfer", {
                        "entities" : entranceEdges,
                        "chamferType" : ChamferType.EQUAL_OFFSETS,
                        "width" : bore.chamfer
                    });
        }
        catch
        {
            // The parts, bored
            addDebugEntities(context, qUnion(targets), DebugColor.BLUE);
            throw regenError("Failed to chamfer bore.", bore.chamferParameters, entranceEdges);
        }
    }
}

/**
 * The bores' tool bodies (see `cutBores`), through everything both ways from each of `planes`.
 */
function boreTools(context is Context, id is Id, bore is Bore, planes is array, identities is array) returns Query
{
    var tools = [];
    for (var i, plane in planes)
    {
        const boreId = id + unstableIdComponent(i);
        if (identities[i] != undefined)
        {
            setExternalDisambiguation(context, boreId, identities[i]);
        }
        tools = append(tools, boreTool(context, boreId, bore, plane));
    }
    cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
    return qUnion(tools);
}

/**
 * A bore's tool body, through everything both ways from `plane`.
 */
function boreTool(context is Context, id is Id, bore is Bore, plane is Plane) returns Query
{
    const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : plane });
    if (bore.shape == BoreShape.HEX)
    {
        const width = bore.across + bore.clearance;
        skRegularPolygon(sketch, "hex", {
                    "center" : vector(0, 0) * meter,
                    "firstVertex" : vector(width / 2 / cos(30 * degree), 0 * meter),
                    "sides" : 6
                });
    }
    else if (bore.shape == BoreShape.ROUND)
    {
        skCircle(sketch, "circle", {
                    "center" : vector(0, 0) * meter,
                    "radius" : (bore.across + bore.clearance) / 2
                });
    }
    else if (isPresketchedSpline(bore))
    {
        skDataArray(sketch, "spline", {
                    "sketchDataArray" : SPLINE_XS_BORES[bore.fit],
                    "location" : vector(0, 0) * meter
                });
    }
    else
    {
        skSplineProfile(sketch, "spline", {
                    "splineType" : bore.splineType,
                    "location" : vector(0, 0) * meter
                });
    }
    skSolve(sketch);
    opExtrude(context, id + "extrude", {
                "entities" : qCreatedBy(id + "sketch", EntityType.FACE),
                "direction" : plane.normal,
                "endBound" : BoundingType.THROUGH_ALL,
                "startBound" : BoundingType.THROUGH_ALL
            });
    if (bore.shape == BoreShape.SPLINE && !isPresketchedSpline(bore) && !tolerantEqualsZero(bore.clearance))
    {
        // A spline's sketched at its nominal size, so its fit is added here (half on each side)
        opOffsetFace(context, id + "offsetFace", {
                    "moveFaces" : qNonCapEntity(id + "extrude", EntityType.FACE),
                    "offsetDistance" : bore.clearance / 2
                });
    }
    return qCreatedBy(id + "extrude", EntityType.BODY);
}

/**
 * Whether `bore` is a SplineXS bore at a standard fit, whose profile is sketched with its fit (`SPLINE_XS_BORES`):
 * offsetting its involute flanks' faces is slow.
 */
function isPresketchedSpline(bore is Bore) returns boolean
{
    return bore.shape == BoreShape.SPLINE && bore.splineType == SplineType.SPLINE_XS && bore.fit != undefined && SPLINE_XS_BORES[bore.fit] != undefined;
}

/**
 * Chamfers a bore's entrances: the edges of `boreFaces` (the bore's sides, in the part) which meet the part's other
 * faces, rather than each other.
 */
export function chamferBoreEntrances(context is Context, id is Id, boreFaces is Query, distance is ValueWithUnits)
{
    var entrances = [];
    for (var edge in evaluateQuery(context, qAdjacent(boreFaces, AdjacencyType.EDGE, EntityType.EDGE)))
    {
        if (!isQueryEmpty(context, qSubtraction(qAdjacent(edge, AdjacencyType.EDGE, EntityType.FACE), boreFaces)))
        {
            entrances = append(entrances, edge);
        }
    }
    if (entrances == [])
    {
        return;
    }
    try
    {
        opChamfer(context, id, {
                    "entities" : qUnion(entrances),
                    "chamferType" : ChamferType.EQUAL_OFFSETS,
                    "width" : distance
                });
    }
    catch
    {
        throw regenError("Failed to chamfer bore.", ["chamferDistance"], qUnion(entrances));
    }
}
