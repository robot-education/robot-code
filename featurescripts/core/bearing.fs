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
 * Placing bearings or bushings, for Robot bearing and Robot bushing: a bore cut at each location for one, and it declared
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
 * Where they go: the locations, one bearing or bushing at each, the parts they're cut into, and their orientation,
 * angle, and starting offset, as Robot motor's.
 * @param name {string} : What's placed, plural ("bearings").
 */
export predicate bearingPositionPredicate(definition is map, name is string)
{
    annotation { "Name" : "Sketch points to place " ~ name,
                "Filter" : (EntityType.VERTEX && SketchObject.YES && ModifiableEntityOnly.YES) || GeometryType.CIRCLE || BodyType.MATE_CONNECTOR,
                "UIHint" : UIHint.INITIAL_FOCUS }
    definition.locations is Query;

    holeMergeScopePredicate(definition);

    axisOrientationPredicate(definition);

    angleReferencePredicate(definition);

    angleOffsetPredicate(definition);

    startOffsetPredicate(definition);
}

const LOCATION_ERROR = "Select a sketch point, circle, or mate connector to use.";

/**
 * The plane each one sits on: its location's, offset, turned to the angle reference and Angle, and flipped. Its normal
 * points out of the face it sits on, away from the parts; its x axis is the angle reference's. Adds the offset's and
 * angle's manipulators, on the first.
 */
export function bearingPlanes(context is Context, id is Id, definition is map, part is BearingPart) returns array
{
    const locations = evaluateQuery(context, definition.locations);
    if (locations == [])
    {
        throw regenError(LOCATION_ERROR, ["locations"]);
    }
    var planes = [];
    for (var i, location in locations)
    {
        const locationPlane = selectionPlane(context, location);
        if (locationPlane == undefined)
        {
            throw regenError(LOCATION_ERROR, ["locations"], location);
        }
        var plane = applyStartOffset(context, definition, locationPlane);
        if (i == 0)
        {
            addStartOffsetManipulator(context, id, definition, plane);
        }
        plane = applyAngleReference(context, definition, plane, "locations");
        plane = applyAxisOrientation(definition, plane);
        if (i == 0)
        {
            addAngleOffsetManipulator(context, id, definition, plane, part.outerDiameter);
        }
        planes = append(planes, applyAngleOffset(definition, plane));
    }
    return planes;
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
 * Cuts a bore at each plane, through the merge scope behind it, at the part's outside diameter plus `clearance` (Robot
 * bearing's Bore fit; none for a pressed-in bushing), in one boolean; declares a part in each, on the part it's in; and
 * with Auto Assembly body, builds each one's Auto Assembly body.
 */
export function placeBearings(context is Context, id is Id, definition is map, planes is array, part is BearingPart, clearance is ValueWithUnits, kind is AutoAssemblyKind)
{
    if (isQueryEmpty(context, definition.scope))
    {
        throw regenError(ErrorStringEnum.HOLE_EMPTY_SCOPE, ["scope"]);
    }
    var hosts = [];
    for (var i, plane in planes)
    {
        const bounds = evBox3d(context, { "topology" : definition.scope, "cSys" : coordSystem(plane), "tight" : false });
        const depth = -bounds.minCorner[2];
        if (!tolerantGreaterThan(depth, 0 * meter))
        {
            throw regenError("The parts to cut aren't behind the sketch point. Flip the primary axis.", ["oppositeDirection", "scope"], definition.scope);
        }
        // Before the cut, which can split it
        hosts = append(hosts, partBehind(context, definition.scope, plane));

        const bearingId = id + bearingIdComponent(i);
        const sketch = newSketchOnPlane(context, bearingId + "boreSketch", { "sketchPlane" : plane });
        skCircle(sketch, "bore", { "center" : vector(0, 0) * meter, "radius" : (part.outerDiameter + clearance) / 2 });
        skSolve(sketch);
        opExtrude(context, bearingId + "boreTool", {
                    "entities" : qSketchRegion(bearingId + "boreSketch"),
                    "direction" : -plane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : depth
                });
    }
    const tools = qUnion(mapArray(range(0, size(planes) - 1), i => qCreatedBy(id + bearingIdComponent(i) + "boreTool", EntityType.BODY)));
    try
    {
        opBoolean(context, id + "bore", {
                    "tools" : tools,
                    "targets" : definition.scope,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
    catch
    {
        // A failed boolean changes nothing, so the tools are still there to show
        throw regenError("Failed to cut the bore.", ["scope"], tools);
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
    for (var i, plane in planes)
    {
        const bearingId = id + bearingIdComponent(i);
        if (declaration != undefined)
        {
            declarePart(context, bearingId + "mateConnector", {
                        "owner" : hosts[i],
                        "coordSystem" : bearingConnector(plane),
                        "connector" : "mateConnector",
                        "declaration" : declaration
                    });
        }
        if (definition.autoAssemblyBody)
        {
            buildBearingBody(context, bearingId + "autoAssemblyBody", plane, part, declaration);
        }
    }
    cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
}

/**
 * The id under which the `i`th location's bore, connector and Auto Assembly body are made: `bearing1`, `bearing2`.
 */
function bearingIdComponent(i is number) returns string
{
    return "bearing" ~ (i + 1);
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

/**
 * Std's hole heuristics, as `mountingEditLogic`'s, at every location: they set the merge scope to the parts there, and
 * Flip primary axis so the bores go into them, unless they've been set.
 */
export function bearingEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, specifiedParameters is map, hiddenBodies is Query) returns map
{
    const holeDefinition = {
            "locations" : locationPoints(context, id + "new", definition),
            "scope" : definition.scope,
            "oppositeDirection" : definition.oppositeDirection,
            "startStyle" : HoleStartStyle.SKETCH,
            "endStyle" : HoleEndStyle.THROUGH
        };
    const oldHoleDefinition = oldDefinition == {} ? {} : {
                "locations" : locationPoints(context, id + "old", oldDefinition),
                "scope" : oldDefinition.scope,
                "oppositeDirection" : oldDefinition.oppositeDirection,
                "startStyle" : HoleStartStyle.SKETCH,
                "endStyle" : HoleEndStyle.THROUGH
            };
    const newHoleDefinition = holeScopeFlipHeuristicsCall(context, oldHoleDefinition, holeDefinition, specifiedParameters, hiddenBodies);
    definition.scope = newHoleDefinition.scope;
    definition.oppositeDirection = newHoleDefinition.oppositeDirection;
    return definition;
}

/**
 * A sketch point at each location that has a plane, for std's hole heuristics.
 */
function locationPoints(context is Context, id is Id, definition is map) returns Query
{
    var points = [];
    for (var i, location in evaluateQuery(context, definition.locations))
    {
        const plane = selectionPlane(context, location);
        if (plane != undefined)
        {
            points = append(points, sketchHoleLocation(context, id + ("location" ~ i), plane));
        }
    }
    return qUnion(points);
}
