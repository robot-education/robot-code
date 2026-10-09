FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "2073caea5ae472033c5090d9");
import(path : "ea127c07807644fb48d3a1ae", version : "72fbd92d548c811d10a5d2f3");
export import(path : "948c83c1b1ac83de4ccf921b", version : "4aff58a1ab26d9f7aa7abfbb");
import(path : "core/loop.fs", version : "");
import(path : "e269bd2b7266145c47eaf374", version : "6c8b8d8077dcf88085165ded");
import(path : "chain/chainCommon.fs", version : "");

import(path : "6c65805103086c85362ee4b7", version : "06268198ef2566cb246b9f56");

export enum AdjustmentType
{
    annotation { "Name" : "Move" }
    MOVE,
    annotation { "Name" : "Resize" }
    RESIZE
}

/**
 * Finds how far to move a pulley, sprocket, or idler, along a direction, or how big to make an idler, for a belt or
 * chain (made by Robot belt or Robot chain, with their `LOOP_ATTRIBUTE`) to be just the length it should be, and
 * shows the belt's or chain's path then.
 */
annotation {
        "Feature Type Name" : "Robot tensioner",
        "Feature Type Description" : "Find how far to move a pulley, sprocket, or idler, or how big to make an idler, for a belt or chain to fit exactly." ~
        "<br>Works with belts and chains made by the Robot belt and Robot chain FeatureScripts." ~ CREDIT,
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotBeltTuner = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        unitSystemPredicate(definition);

        annotation { "Name" : "Adjustment type", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.adjustmentType is AdjustmentType;

        annotation { "Name" : "Belt or chain", "Filter" : EntityType.BODY && BodyType.SOLID, "MaxNumberOfPicks" : 1 }
        definition.loop is Query;

        annotation { "Name" : "Pulley, sprocket, or idler", "Filter" : (EntityType.FACE && GeometryType.CYLINDER) || BodyType.MATE_CONNECTOR || (EntityType.BODY && BodyType.SOLID),
                    "MaxNumberOfPicks" : 1,
                    "Description" : "The one to adjust: the belt's or chain's curved face around it, its mate connector, or a Robot pulley or sprocket." }
        definition.adjust is Query;

        if (definition.adjustmentType == AdjustmentType.MOVE)
        {
            annotation { "Name" : "Direction", "Filter" : QueryFilterCompound.ALLOWS_DIRECTION, "MaxNumberOfPicks" : 1,
                        "Description" : "The direction to move it in, like along a slot." }
            definition.direction is Query;
        }
    }
    {
        const attribute = getLoop(context, definition);
        const loopPlane = plane(attribute.coordSystem.coordSystem);
        const circles = attribute.circles;
        const counterClockwise = attribute.counterClockwise;
        const index = getAdjustIndex(context, definition, loopPlane, circles);

        var adjusted;
        if (definition.adjustmentType == AdjustmentType.MOVE)
        {
            const direction = getDirection(context, definition, loopPlane);
            adjusted = function(offset is ValueWithUnits) returns array
                {
                    var result = circles;
                    result[index].location += direction * offset;
                    return result;
                };
        }
        else
        {
            if (circles[index].idlerRadius == undefined)
            {
                throw regenError("Only an idler can be resized: select one, or move this instead.", ["adjust", "adjustmentType"], definition.adjust);
            }
            adjusted = function(offset is ValueWithUnits) returns array
                {
                    var result = circles;
                    result[index].radius += offset;
                    return result;
                };
        }
        // How much longer the path is than the belt or chain, adjusted by `offset`
        const tooShortBy = function(offset is ValueWithUnits)
            {
                const length = tryLoopLength(adjusted(offset), counterClockwise);
                return length == undefined ? undefined : length - attribute.length;
            };

        if (withinDisplayPrecision(definition, attribute.length + tooShortBy(0 * meter), attribute.length, true))
        {
            showLoop(context, id + "loop", loopPlane, circles, counterClockwise, DebugColor.GREEN);
            reportFeatureInfo(context, id, "The " ~ attribute.name ~ " fits already, so this feature can be deleted.");
            return;
        }

        const offset = nearestRoot(tooShortBy, 0.1 * millimeter, TOLERANCE.zeroLength * meter);
        if (offset == undefined)
        {
            if (definition.adjustmentType == AdjustmentType.MOVE)
            {
                throw regenError("Moving it that way doesn't make the " ~ attribute.name ~ " fit: try another direction.", ["direction"], definition.direction);
            }
            throw regenError("No size of idler makes the " ~ attribute.name ~ " fit: try moving it instead.", ["adjustmentType"]);
        }
        const solved = adjusted(offset);
        showLoop(context, id + "loop", loopPlane, solved, counterClockwise, DebugColor.BLUE);

        if (definition.adjustmentType == AdjustmentType.MOVE)
        {
            const start = planeToWorld(loopPlane, circles[index].location);
            const end = planeToWorld(loopPlane, solved[index].location);
            addDebugArrow(context, start, end, norm(end - start) * 0.2, DebugColor.GREEN);
            // Move it 0.1234 in as shown (along the direction) to fit the 100T 5mm HTD belt.
            reportFeatureInfo(context, id, "Move it " ~ makeValueString(definition.unitSystem, abs(offset), true) ~ " as shown (" ~
                    (offset > 0 * meter ? "along" : "against") ~ " the direction) to fit the " ~ attribute.name ~ ".");
        }
        else
        {
            const diameter = 2 * solved[index].idlerRadius + 2 * offset;
            reportFeatureInfo(context, id, "Make the idler " ~ makeValueString(definition.unitSystem, diameter, true) ~ " across to fit the " ~ attribute.name ~ ".");
        }
    });

/**
 * The selected belt's or chain's `LoopAttribute`.
 */
function getLoop(context is Context, definition is map) returns LoopAttribute
{
    verifyNonemptyQuery(context, definition, "loop", "Select a belt or chain to tension.");
    const attribute = getAttribute(context, {
                "entity" : definition.loop,
                "name" : LOOP_ATTRIBUTE
            });
    if (attribute == undefined || !canBeLoopAttribute(attribute))
    {
        throw regenError("Select a belt or chain made by Robot belt or Robot chain (one made by an older Robot belt needs updating first).", ["loop"], definition.loop);
    }
    if (attribute.coordSystem.coordSystem == undefined)
    {
        throw regenError("A mirrored belt or chain can't be tensioned: tension the one it mirrors.", ["loop"], definition.loop);
    }
    return attribute;
}

/**
 * Which of the loop's `circles` the selected face (around it) or mate connector (at its center) is.
 */
function getAdjustIndex(context is Context, definition is map, loopPlane is Plane, circles is array) returns number
{
    verifyNonemptyQuery(context, definition, "adjust", "Select the pulley, sprocket, or idler to adjust: the belt's or chain's curved face around it, or its mate connector.");
    var center;
    // A Robot pulley's or sprocket's center is its attribute's
    var part;
    for (var name in [PULLEY_ATTRIBUTE, SPROCKET_ATTRIBUTE])
    {
        part = part ?? getAttribute(context, { "entity" : definition.adjust, "name" : name });
    }
    if (part != undefined && part.coordSystem is PersistentCoordSystem && part.coordSystem.coordSystem != undefined)
    {
        center = part.coordSystem.coordSystem.origin;
    }
    else if (isMateConnector(context, definition.adjust))
    {
        center = evMateConnector(context, { "mateConnector" : definition.adjust }).origin;
    }
    else if (!isQueryEmpty(context, definition.adjust->qEntityFilter(EntityType.BODY)))
    {
        throw regenError("Select a Robot pulley or sprocket, or the belt's or chain's face around one, or its mate connector.", ["adjust"], definition.adjust);
    }
    else
    {
        if (isQueryEmpty(context, definition.adjust->qIntersection(qOwnedByBody(definition.loop, EntityType.FACE))))
        {
            throw regenError("Select a face of the selected belt or chain.", ["adjust"], definition.adjust);
        }
        center = evSurfaceDefinition(context, { "face" : definition.adjust }).coordSystem.origin;
    }
    const location = worldToPlane(loopPlane, center);
    for (var i, circle in circles)
    {
        if (norm(location - circle.location) < 1e-6 * meter)
        {
            return i;
        }
    }
    throw regenError("The selection isn't around one of the belt's or chain's pulleys, sprockets, or idlers.", ["adjust"], definition.adjust);
}

/**
 * The selected direction, in the loop's plane (as a 2D unit vector).
 */
function getDirection(context is Context, definition is map, loopPlane is Plane) returns Vector
{
    verifyNonemptyQuery(context, definition, "direction", "Select a direction to move it in.");
    const direction = extractDirection(context, definition.direction);
    if (direction == undefined)
    {
        throw regenError("Select an edge, axis, or face to move it along (or a face to move it out of).", ["direction"], definition.direction);
    }
    const inPlane = worldToPlane(loopPlane, loopPlane.origin + direction * meter) / meter;
    if (tolerantEqualsZero(norm(inPlane)))
    {
        throw regenError("The direction can't be across the belt or chain: it has to move in its plane.", ["direction"], definition.direction);
    }
    return normalize(inPlane);
}

/**
 * Shows the loop's path around `circles`, in `color`.
 */
function showLoop(context is Context, id is Id, loopPlane is Plane, circles is array, counterClockwise is boolean, color is DebugColor)
{
    // A guard: the path is only shown
    try silent
    {
        addDebugEntities(context, sketchLoop(context, id, loopPlane, circles, counterClockwise).path, color);
    }
    const sketches = qCreatedBy(id, EntityType.BODY);
    if (!isQueryEmpty(context, sketches))
    {
        opDeleteBodies(context, id + "delete", { "entities" : sketches });
    }
}
