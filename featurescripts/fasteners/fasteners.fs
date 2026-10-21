FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "core/robotFeature.fs", version : "");
import(path : "core/robotProperties.fs", version : "");
import(path : "core/coreUtils.fs", version : "");
// Also exports the enums used as parameter types
export import(path : "fasteners/fastenerTables.gen.fs", version : "");

/**
 * FRCDesign's library fasteners: its screws, washers, and nuts, generated for FRCDesignApp. Each is made in a Part
 * Studio of its own, whose configuration sets every parameter and offers only what's made (see fastenerTables.py),
 * so these dialogs are rarely seen: each is a flat list of enums, with no conditions, which the configuration filters.
 *
 * Each part is built along the Z axis, with one mate connector at the origin pointing out of the hole: where it seats
 * against the part or washer it sits on.
 */

// How deep a socket is, of its head's height
const SOCKET_DEPTH = 0.6;

annotation { "Feature Type Name" : "Screw",
        "Feature Type Description" : "A standard screw from FRCDesign's library, seated at the origin." ~ CREDIT }
export const fastenerScrew = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        vendorPredicate(definition);

        sizePredicate(definition);

        annotation { "Name" : "Head", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.head is ScrewHead;

        annotation { "Name" : "Length", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.length is ScrewLength;
    }
    {
        const stock = SCREWS[definition.vendor][definition.size][definition.head][definition.length];
        if (stock == undefined)
        {
            throw regenError("Selected vendor does not make this screw, change the length.", ["length"]);
        }
        const size = FASTENER_SIZES[definition.size];
        const head = SCREW_HEADS[definition.size][definition.head];
        const length = SCREW_LENGTHS[definition.length];
        const isFlat = definition.head == ScrewHead.FLAT;

        // 1. Revolve the screw's profile: its head above the origin, or a flat head's below it, and its shank down
        const sketchId = id + "profile";
        const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : PROFILE_PLANE });
        skPolyline(sketch, "profile", { "points" : screwProfile(size, head, definition.head, length.length) });
        if (definition.head == ScrewHead.BUTTON)
        {
            skArc(sketch, "dome", buttonDome(head));
        }
        skSolve(sketch);
        opRevolve(context, id + "revolve", {
                    "entities" : qSketchRegion(sketchId),
                    "axis" : line(WORLD_ORIGIN, Z_DIRECTION),
                    "angleForward" : 2 * PI * radian
                });
        const screw = qCreatedBy(id + "revolve", EntityType.BODY);

        // 2. Cut its hex socket down from the top of its head
        const top = isFlat ? 0 * meter : head.headHeight;
        const socketId = id + "socket";
        const socketSketch = newSketchOnPlane(context, socketId + "sketch", {
                    "sketchPlane" : plane(vector(0 * meter, 0 * meter, top), Z_DIRECTION)
                });
        skRegularPolygon(socketSketch, "hex", {
                    "center" : vector(0, 0) * meter,
                    "firstVertex" : vector(head.socketSize / sqrt(3), 0 * meter),
                    "sides" : 6
                });
        skSolve(socketSketch);
        opExtrude(context, socketId + "extrude", {
                    "entities" : qSketchRegion(socketId + "sketch"),
                    "direction" : -Z_DIRECTION,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : SOCKET_DEPTH * (isFlat ? flatHeadHeight(size, head) : head.headHeight)
                });
        opBoolean(context, socketId + "cut", {
                    "tools" : qCreatedBy(socketId + "extrude", EntityType.BODY),
                    "targets" : screw,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });

        // 3. Seat it, under its head
        addSeatMateConnector(context, id + "mateConnector", screw);

        cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
        setFastenerProperties(context, screw, stock);
    });

/**
 * A washer, from its bottom face at the origin up.
 */
annotation { "Feature Type Name" : "Washer",
        "Feature Type Description" : "A washer from FRCDesign's library, from the origin up." ~ CREDIT }
export const fastenerWasher = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        vendorPredicate(definition);

        sizePredicate(definition);

        annotation { "Name" : "Type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.washerType is WasherType;
    }
    {
        const washer = WASHERS[definition.vendor][definition.size][definition.washerType];
        if (washer == undefined)
        {
            throw regenError("Selected vendor does not make this washer, change the type.", ["washerType"]);
        }

        const sketchId = id + "sketch";
        const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : XY_PLANE });
        skCircle(sketch, "outer", { "center" : vector(0, 0) * meter, "radius" : washer.outerDiameter / 2 });
        skCircle(sketch, "inner", { "center" : vector(0, 0) * meter, "radius" : washer.innerDiameter / 2 });
        skSolve(sketch);
        opExtrude(context, id + "extrude", {
                    "entities" : qSketchRegion(sketchId, true),
                    "direction" : Z_DIRECTION,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : washer.thickness
                });
        const body = qCreatedBy(id + "extrude", EntityType.BODY);

        opMateConnector(context, id + "mateConnector", {
                    "coordSystem" : coordSystem(WORLD_ORIGIN, X_DIRECTION, Z_DIRECTION),
                    "owner" : body
                });

        cleanup(context, id + "deleteSketches", qCreatedBy(sketchId, EntityType.BODY));
        setFastenerProperties(context, body, washer);
    });

/**
 * A nut, from its bearing face at the origin up.
 */
annotation { "Feature Type Name" : "Nut",
        "Feature Type Description" : "A nut from FRCDesign's library, seated at the origin." ~ CREDIT }
export const fastenerNut = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        vendorPredicate(definition);

        sizePredicate(definition);

        annotation { "Name" : "Type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.nutType is NutType;
    }
    {
        const nut = NUTS[definition.vendor][definition.size][definition.nutType];
        if (nut == undefined)
        {
            throw regenError("Selected vendor does not make this nut, change the type.", ["nutType"]);
        }
        const size = FASTENER_SIZES[definition.size];

        const sketchId = id + "sketch";
        const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : XY_PLANE });
        if (definition.nutType == NutType.SQUARE)
        {
            skRectangle(sketch, "square", {
                        "firstCorner" : vector(-nut.acrossFlats / 2, -nut.acrossFlats / 2),
                        "secondCorner" : vector(nut.acrossFlats / 2, nut.acrossFlats / 2)
                    });
        }
        else
        {
            skRegularPolygon(sketch, "hex", {
                        "center" : vector(0, 0) * meter,
                        "firstVertex" : vector(nut.acrossFlats / sqrt(3), 0 * meter),
                        "sides" : 6
                    });
        }
        skCircle(sketch, "hole", { "center" : vector(0, 0) * meter, "radius" : size.majorDiameter / 2 });
        skSolve(sketch);
        opExtrude(context, id + "extrude", {
                    "entities" : qSketchRegion(sketchId, true),
                    "direction" : Z_DIRECTION,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : nut.height
                });
        const body = qCreatedBy(id + "extrude", EntityType.BODY);

        addSeatMateConnector(context, id + "mateConnector", body);

        cleanup(context, id + "deleteSketches", qCreatedBy(sketchId, EntityType.BODY));
        setFastenerProperties(context, body, nut);
    });

predicate vendorPredicate(definition is map)
{
    annotation { "Name" : "Vendor", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.vendor is FastenerVendor;
}

predicate sizePredicate(definition is map)
{
    annotation { "Name" : "Size", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.size is FastenerSize;
}

// The XZ plane, with the sketch's X along the radius and its Y along the axis
const PROFILE_PLANE = plane(WORLD_ORIGIN, -Y_DIRECTION, X_DIRECTION);


/**
 * A mate connector at the origin, pointing up: fastened to an edge, the part sits on it.
 */
function addSeatMateConnector(context is Context, id is Id, owner is Query)
{
    opMateConnector(context, id, {
                "coordSystem" : coordSystem(WORLD_ORIGIN, X_DIRECTION, Z_DIRECTION),
                "owner" : owner
            });
}

/**
 * How far a flat head goes into its countersink: from its diameter down to the shank's at its countersink angle.
 */
export function flatHeadHeight(size is map, head is map) returns ValueWithUnits
{
    return (head.headDiameter - size.majorDiameter) / 2 / tan(head.countersinkAngle / 2);
}

/**
 * A screw's profile in `PROFILE_PLANE`, closed along its axis: (radius, height) points from the top of its head down to
 * the end of its shank. A button head's dome is `buttonDome`, which closes its profile from the axis to its edge.
 */
export function screwProfile(size is map, head is map, headType is ScrewHead, length is ValueWithUnits) returns array
{
    const radius = size.majorDiameter / 2;
    const headRadius = head.headDiameter / 2;
    const zero = 0 * meter;
    if (headType == ScrewHead.FLAT)
    {
        // A flat head's length is from its top, which sits flush with the part
        return [
                vector(zero, zero),
                vector(headRadius, zero),
                vector(radius, -flatHeadHeight(size, head)),
                vector(radius, -length),
                vector(zero, -length),
                vector(zero, zero)
            ];
    }
    const shank = [vector(headRadius, zero), vector(radius, zero), vector(radius, -length), vector(zero, -length), vector(zero, head.headHeight)];
    if (headType == ScrewHead.BUTTON)
    {
        return concatenateArrays([[vector(headRadius, buttonEdgeHeight(head))], shank]);
    }
    return concatenateArrays([[vector(zero, head.headHeight), vector(headRadius, head.headHeight)], shank]);
}

function buttonEdgeHeight(head is map) returns ValueWithUnits
{
    return head.headHeight / 4;
}

/**
 * A button head's dome: an arc from the top of its axis to its edge, a quarter of its height above its underside.
 */
export function buttonDome(head is map) returns map
{
    const edge = vector(head.headDiameter / 2, buttonEdgeHeight(head));
    const apex = vector(0 * meter, head.headHeight);
    // The circle through the apex and the edge, centered on the axis
    const rise = apex[1] - edge[1];
    const radius = (edge[0] ^ 2 + rise ^ 2) / (2 * rise);
    const center = vector(0 * meter, apex[1] - radius);
    const middle = center + radius * normalize(normalize(edge - center) + normalize(apex - center));
    return { "start" : edge, "mid" : middle, "end" : apex };
}

/**
 * Sets a part's properties from its table entry (see fastenerTables.py's `Record`): its name, its vendor and part
 * number, and a link to buy it where it has them, and its material and appearance. FRCDesignApp indexes the same
 * records, so these are the only properties set.
 */
function setFastenerProperties(context is Context, body is Query, entry is map)
{
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.NAME, "value" : entry.name });
    if (entry.vendor != undefined)
    {
        setProperty(context, { "entities" : body, "propertyType" : PropertyType.VENDOR, "value" : entry.vendor });
    }
    if (entry.partNumber != undefined)
    {
        setProperty(context, { "entities" : body, "propertyType" : PropertyType.PART_NUMBER, "value" : entry.partNumber });
    }
    if (entry.url != undefined)
    {
        setProperty(context, { "entities" : body, "propertyType" : PropertyType.DESCRIPTION, "value" : entry.url });
    }
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.MATERIAL, "value" : STEEL });
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.APPEARANCE, "value" : BLACK });
}
