FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "onshape/std/chamfer.fs", version : "2960.0");

// Exports Fit and SplineType, parameter types
export import(path : "926d933eb33b11a3452660fd", version : "9f460f5afe4b1aa32d2f1898");
export import(path : "6e24956e9977116c79280620", version : "0ec5da0acf56336b68065e37");
import(path : "a4248fe48b63da8d1971e19a", version : "84a8da5dce4e619110893727");
import(path : "8b8c46128a5dbc2594925f4a", version : "6b7d5995c565ae73c7740b0b");

/**
 * Bores through parts on a shaft (sprockets, gears, and the like): a hex, round, or spline bore, with a fit, and
 * optionally an entrance chamfer.
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
 * Add bore, and the bore's shape, size, fit, and entrance chamfer.
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
                annotation { "Name" : "Width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "Across the hex's flats." }
                isLength(definition.hexWidth, HEX_WIDTH_BOUNDS);
            }
            else if (definition.boreShape == BoreShape.ROUND)
            {
                annotation { "Name" : "Diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "The diameter of the shaft it goes on." }
                isLength(definition.boreDiameter, ROUND_DIAMETER_BOUNDS);
            }
            else
            {
                annotation { "Name" : "Spline type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                definition.splineType is SplineType;
            }

            // Of the bore on its shaft
            fitPredicate(definition);

            entranceChamferPredicate(definition);
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
 *      @field chamfer {ValueWithUnits} : @optional Its entrance chamfer's distance, if it has one.
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
    value.chamfer == undefined || isLength(value.chamfer);
}

/**
 * The bore `borePredicate` declares, or `undefined` without one.
 */
export function definitionBore(definition is map)
{
    if (!definition.addBore)
    {
        return undefined;
    }
    const across = switch (definition.boreShape) {
                BoreShape.HEX : definition.hexWidth,
                BoreShape.ROUND : definition.boreDiameter,
                BoreShape.SPLINE : splineDiameter(definition.splineType)
            };
    return {
                "shape" : definition.boreShape,
                "across" : across,
                "splineType" : definition.splineType,
                "clearance" : fitClearance(definition, across),
                "chamfer" : definition.entranceChamfer ? definition.chamferDistance : undefined
            } as Bore;
}

/**
 * Cuts `bore` through each of `targets` (the parts), centered on its plane in `planes` (along its normal), with
 * `identities` (queries, or undefined) to disambiguate them. `featureId` is the feature's id, for its errors (see
 * `runStep`).
 */
export function cutBores(context is Context, featureId is Id, id is Id, bore is Bore, planes is array, identities is array, targets is array)
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
    const bodies = qUnion(tools);
    cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));

    // The bore's edges inside the parts, to tell its entrance edges from
    const insideEdges = startTracking(context, qNonCapEntity(id, EntityType.EDGE));
    runStep(context, featureId, id + "cut", opBoolean, {
                "tools" : bodies,
                "targets" : qUnion(targets),
                "operationType" : BooleanOperationType.SUBTRACTION
            }, {
                "message" : "Couldn't cut the bore.",
                "faultyParameters" : ["boreShape", "hexWidth", "boreDiameter", "splineType", "fit", "fitClearance"],
                "entities" : qUnion([bodies, qUnion(targets)])
            });

    if (bore.chamfer != undefined)
    {
        const entranceEdges = qSubtraction(qCreatedBy(id + "cut", EntityType.EDGE), insideEdges);
        runStep(context, featureId, id + "chamfer", opChamfer, {
                    "entities" : entranceEdges,
                    "chamferType" : ChamferType.EQUAL_OFFSETS,
                    "width" : bore.chamfer
                }, {
                    "message" : "Couldn't chamfer the bore's entrances.",
                    "faultyParameters" : ["chamferDistance"],
                    "entities" : entranceEdges
                });
    }
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
    if (bore.shape == BoreShape.SPLINE && !tolerantEqualsZero(bore.clearance))
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
 * Chamfers a bore's entrances: the edges of `boreFaces` (the bore's sides, in the part) which meet the part's other
 * faces, rather than each other. `featureId` is the feature's id, for its errors (see `runStep`).
 */
export function chamferBoreEntrances(context is Context, featureId is Id, id is Id, boreFaces is Query, distance is ValueWithUnits)
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
    runStep(context, featureId, id, opChamfer, {
                "entities" : qUnion(entrances),
                "chamferType" : ChamferType.EQUAL_OFFSETS,
                "width" : distance
            }, {
                "message" : "Couldn't chamfer the bore's entrances.",
                "faultyParameters" : ["chamferDistance"],
                "entities" : qUnion(entrances)
            });
}
