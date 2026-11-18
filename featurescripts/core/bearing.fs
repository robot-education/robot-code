FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
export import(path : "0195d390c3944cd4fab21ce0", version : "82243acdb77d04d0ca39cd49");
// Exports startOffsetPredicate's types
export import(path : "b82468283e5ec09720bad185", version : "142c41c73cc2f8336c28931e");
// Exports MateConnectorAxisType, a parameter type
export import(path : "58d66340f7b70cfc86606676", version : "da525a155390a101429f7f9a");
// Exports Fit, a parameter type
export import(path : "926d933eb33b11a3452660fd", version : "d69c937bc00dba371714fef9");
export import(path : "01402b7c9eebd8bf0b5d3e52", version : "bb7c494edc43a307e631af8d");
export import(path : "core/autoAssembly.fs", version : "");

/**
 * Placing a bearing or bushing, for Robot bearing and Robot bushing: a bore cut at a location for it, and it declared
 * there for Auto Assembly (see `docs/auto-assembly.md`), with a stand-in.
 */

/**
 * A bearing or bushing, from `bearingTables.gen.fs`'s tables.
 */
export type BearingPart typecheck canBeBearingPart;

/**
 * @field outerDiameter {ValueWithUnits} : What the bore is cut to (a flanged part's body, not its flange).
 * @field width {ValueWithUnits} : How far it goes into the bore.
 * @field innerDiameter {ValueWithUnits} : Its stand-in's hole: its inner diameter, or a hex's or square's nominal size.
 * @field product {map} : The library's part (`Product`).
 */
export predicate canBeBearingPart(value)
{
    value is map;
    isLength(value.outerDiameter);
    isLength(value.width);
    isLength(value.innerDiameter);
    canBeProduct(value.product);
}

/**
 * Where it goes: the location, and its orientation, angle, and starting offset, as Robot motor's.
 */
export predicate bearingPositionPredicate(definition is map, name is string)
{
    locationPredicate(definition, name);

    axisOrientationPredicate(definition);

    angleReferencePredicate(definition);

    angleOffsetPredicate(definition);

    startOffsetPredicate(definition);
}

/**
 * The bore: its fit, and the parts it's cut through.
 */
export predicate bearingBorePredicate(definition is map)
{
    boreFitPredicate(definition);

    holeMergeScopePredicate(definition);
}

/**
 * The plane it sits on: the location's, offset, turned to the angle reference and Angle, and flipped. Its normal points
 * out of the face it sits on, away from the parts; its x axis is the angle reference's. Adds the offset's and angle's
 * manipulators.
 */
export function bearingPlane(context is Context, id is Id, definition is map, part is BearingPart) returns Plane
{
    var plane = applyStartOffset(context, definition, getLocationPlane(context, definition));
    addStartOffsetManipulator(context, id, definition, plane);
    plane = applyAngleReference(context, definition, plane);
    plane = applyAxisOrientation(definition, plane);
    addAngleOffsetManipulator(context, id, definition, plane, part.outerDiameter);
    return applyAngleOffset(definition, plane);
}

/**
 * The `face` connector of a bearing's or bushing's convention: the bore's center, on the face it sits on, its z axis
 * out of the bore, and its x axis the plane's.
 */
export function bearingConnector(plane is Plane) returns CoordSystem
{
    return coordSystem(plane.origin, plane.x, plane.normal);
}

/**
 * Cuts the bore, through the merge scope behind `plane`, at the part's outside diameter plus Bore fit's clearance;
 * declares the part on the part it's in; and with Stand-in, builds its stand-in. Returns the part the bore is in.
 */
export function placeBearing(context is Context, id is Id, definition is map, plane is Plane, part is BearingPart, kind is AutoAssemblyKind) returns Query
{
    if (isQueryEmpty(context, definition.scope))
    {
        throw regenError(ErrorStringEnum.HOLE_EMPTY_SCOPE, ["scope"]);
    }
    const bounds = evBox3d(context, { "topology" : definition.scope, "cSys" : coordSystem(plane), "tight" : false });
    const depth = -bounds.minCorner[2];
    if (!tolerantGreaterThan(depth, 0 * meter))
    {
        throw regenError("The parts to cut aren't behind the sketch point. Flip the primary axis.", ["oppositeDirection", "scope"], definition.scope);
    }
    // Before the cut, which can split it
    const host = partBehind(context, definition.scope, plane);

    const sketchId = id + "boreSketch";
    const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : plane });
    const boreDiameter = part.outerDiameter + boreFitClearance(definition, part.outerDiameter);
    skCircle(sketch, "bore", { "center" : vector(0, 0) * meter, "radius" : boreDiameter / 2 });
    skSolve(sketch);
    opExtrude(context, id + "boreTool", {
                "entities" : qSketchRegion(sketchId),
                "direction" : -plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : depth
            });
    const tool = qCreatedBy(id + "boreTool", EntityType.BODY);
    try
    {
        opBoolean(context, id + "bore", {
                    "tools" : tool,
                    "targets" : definition.scope,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
    catch
    {
        // A failed boolean changes nothing, so the tool is still there to show
        throw regenError("Failed to cut the bore.", ["scope"], tool);
    }

    const declaration = autoAssemblyDeclaration(kind, part.product as Product, {
                "outerDiameter" : part.outerDiameter,
                "width" : part.width
            });
    declarePart(context, id + "face", {
                "owner" : host,
                "coordSystem" : bearingConnector(plane),
                "connector" : "face",
                "declaration" : declaration
            });

    if (definition.standIn)
    {
        buildBearingStandIn(context, id + "standIn", plane, part, declaration);
    }
    cleanup(context, id + "deleteSketches", qCreatedBy(sketchId, EntityType.BODY));
    return host;
}

/**
 * A ring at the part's outside and inner diameters, as wide as it, from `plane` into the bore.
 */
function buildBearingStandIn(context is Context, id is Id, plane is Plane, part is BearingPart, declaration is map)
{
    const sketchId = id + "sketch";
    const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : plane });
    skCircle(sketch, "outer", { "center" : vector(0, 0) * meter, "radius" : part.outerDiameter / 2 });
    skCircle(sketch, "inner", { "center" : vector(0, 0) * meter, "radius" : part.innerDiameter / 2 });
    skSolve(sketch);
    opExtrude(context, id + "extrude", {
                "entities" : qSketchRegion(sketchId, true),
                "direction" : -plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : part.width
            });
    makeStandIn(context, id + "declare", {
                "standIn" : qCreatedBy(id + "extrude", EntityType.BODY),
                "connectors" : { "face" : bearingConnector(plane) },
                "declaration" : declaration
            });
    cleanup(context, id + "deleteSketch", qCreatedBy(sketchId, EntityType.BODY));
}

/**
 * Dragging the starting offset or the angle.
 */
export function bearingManipulatorChange(definition is map, newManipulators is map) returns map
{
    return angleOffsetManipulatorChange(startOffsetManipulatorChange(definition, newManipulators), newManipulators);
}
