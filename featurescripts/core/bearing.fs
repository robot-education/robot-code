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
 * there for Auto Assembly (see `docs/auto-assembly.md`), with an Auto Assembly body.
 */

/**
 * A bearing or bushing, from `bearingTables.gen.fs`'s tables.
 */
export type BearingPart typecheck canBeBearingPart;

/**
 * @field outerDiameter {ValueWithUnits} : What the bore is cut to (a flanged part's body, not its flange).
 * @field width {ValueWithUnits} : Its whole width, a flange's included.
 * @field flangeDiameter {ValueWithUnits} : @optional A flanged part's flange, which sits on the face, outside the bore,
 * @field flangeThickness {ValueWithUnits} : @requiredif {`flangeDiameter` is given.} this thick.
 * @field innerDiameter {ValueWithUnits} : Its Auto Assembly body's hole: its inner diameter, or a hex's or square's nominal size.
 * @field product {map} : The library's part (`Product`).
 */
export predicate canBeBearingPart(value)
{
    value is map;
    isLength(value.outerDiameter);
    isLength(value.width);
    isLength(value.innerDiameter);
    value.flangeDiameter == undefined || isLength(value.flangeDiameter);
    value.flangeDiameter == undefined || isLength(value.flangeThickness);
    canBeProduct(value.product);
}

/**
 * Where it goes: the location, the parts it's cut into, and its orientation, angle, and starting offset, as Robot
 * motor's.
 */
export predicate bearingPositionPredicate(definition is map, name is string)
{
    locationPredicate(definition, name);

    holeMergeScopePredicate(definition);

    axisOrientationPredicate(definition);

    angleReferencePredicate(definition);

    angleOffsetPredicate(definition);

    startOffsetPredicate(definition);
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
 * Where a bearing or bushing seats, as FRCDesign's bearings part set's `mateConnector` is: the bore's center, on the
 * face it sits on (a flange's underside), its z axis out of the bore, and its x axis the plane's.
 */
export function bearingConnector(plane is Plane) returns CoordSystem
{
    return coordSystem(plane.origin, plane.x, plane.normal);
}

/**
 * Cuts the bore, through the merge scope behind `plane`, at the part's outside diameter plus `clearance` (Robot bearing's Bore fit; none for a pressed-in bushing);
 * declares the part on the part it's in; and with Auto Assembly body, builds its Auto Assembly body. Returns the part the bore is in.
 */
export function placeBearing(context is Context, id is Id, definition is map, plane is Plane, part is BearingPart, clearance is ValueWithUnits, kind is AutoAssemblyKind) returns Query
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
    const boreDiameter = part.outerDiameter + clearance;
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

    // A part set's part has no document until its set is released
    const declaration = part.product.documentId == "" ? undefined : autoAssemblyDeclaration(kind, part.product as Product, {
                "outerDiameter" : part.outerDiameter,
                "width" : part.width
            });
    if (declaration == undefined)
    {
        reportFeatureInfo(context, id, "FRCDesign's library doesn't have this part yet, so it isn't declared for Auto Assembly.");
    }
    else
    {
        declarePart(context, id + "mateConnector", {
                    "owner" : host,
                    "coordSystem" : bearingConnector(plane),
                    "connector" : "mateConnector",
                    "declaration" : declaration
                });
    }

    if (definition.autoAssemblyBody)
    {
        buildBearingBody(context, id + "autoAssemblyBody", plane, part, declaration);
    }
    cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
    return host;
}

/**
 * A ring at the part's outside and inner diameters into the bore from `plane`, and a flanged part's flange on it: one
 * extrude each, unioned.
 */
function buildBearingBody(context is Context, id is Id, plane is Plane, part is BearingPart, declaration)
{
    const flanged = part.flangeDiameter != undefined;
    const below = flanged ? part.width - part.flangeThickness : part.width;
    extrudeRing(context, id + "body", plane, part.outerDiameter, part.innerDiameter, -plane.normal, below);
    if (flanged)
    {
        extrudeRing(context, id + "flange", plane, part.flangeDiameter, part.innerDiameter, plane.normal, part.flangeThickness);
    }
    if (flanged)
    {
        opBoolean(context, id + "union", {
                    "tools" : qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID),
                    "operationType" : BooleanOperationType.UNION
                });
    }
    makeAutoAssemblyBody(context, id + "declare", {
                "body" : qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID),
                "connectors" : { "mateConnector" : bearingConnector(plane) },
                "declaration" : declaration,
                "name" : part.product.name
            });
    cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
}

/**
 * A ring from `plane`, `depth` along `direction`.
 */
function extrudeRing(context is Context, id is Id, plane is Plane, outerDiameter is ValueWithUnits, innerDiameter is ValueWithUnits, direction is Vector, depth is ValueWithUnits)
{
    const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : plane });
    skCircle(sketch, "outer", { "center" : vector(0, 0) * meter, "radius" : outerDiameter / 2 });
    skCircle(sketch, "inner", { "center" : vector(0, 0) * meter, "radius" : innerDiameter / 2 });
    skSolve(sketch);
    opExtrude(context, id + "extrude", {
                "entities" : qSketchRegion(id + "sketch", true),
                "direction" : direction,
                "endBound" : BoundingType.BLIND,
                "endDepth" : depth
            });
}

/**
 * Dragging the starting offset or the angle.
 */
export function bearingManipulatorChange(definition is map, newManipulators is map) returns map
{
    return angleOffsetManipulatorChange(startOffsetManipulatorChange(definition, newManipulators), newManipulators);
}
