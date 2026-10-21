FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "core/robotFeature.fs", version : "");
import(path : "core/robotProperties.fs", version : "");
import(path : "core/coreUtils.fs", version : "");
// Also exports the enums used as parameter types
export import(path : "quickFastener/quickFastenerTables.gen.fs", version : "");

/**
 * The screws, washers, and nuts FRCDesign's Quick fastener inserts. Each is made in a Part Studio of its own, whose
 * configuration sets every parameter (see quickFastenerTables.py), so these dialogs are rarely seen.
 *
 * Each part is built along the Z axis, with one mate connector at the origin pointing out of the hole: where it seats
 * against the part it fastens, or against the washer it sits on.
 */

const SCREW_LENGTH_BOUNDS = { (inch) : [0.0625, 0.5, 12] } as LengthBoundSpec;

// How deep a socket is, of its head's height
const SOCKET_DEPTH = 0.6;

annotation { "Feature Type Name" : "Quick fastener screw",
        "Feature Type Description" : "A screw for FRCDesign's Quick fastener, seated at the origin." ~ CREDIT }
export const quickFastenerScrew = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Size", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.size is FastenerSize;

        annotation { "Name" : "Head", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.head is ScrewHead;

        annotation { "Name" : "Length", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.length, SCREW_LENGTH_BOUNDS);

        annotation { "Name" : "Washer", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.washer is SeatWasher;
    }
    {
        const size = FASTENER_SIZES[definition.size];
        const head = SCREW_HEADS[definition.size][definition.head];
        const isFlat = definition.head == ScrewHead.FLAT;
        if (isFlat && !tolerantGreaterThan(definition.length, flatHeadHeight(size, head)))
        {
            throw regenError("Length must be longer than the head, increase the value.", ["length"]);
        }

        // 1. Revolve the screw's profile: its head above the origin, or a flat head's below it, and its shank down
        const sketchId = id + "profile";
        const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : PROFILE_PLANE });
        skPolyline(sketch, "profile", { "points" : screwProfile(size, head, definition.head, definition.length) });
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

        // 3. Seat it: on the hole's edge, or on the washer under its head
        opMateConnector(context, id + "mateConnector", {
                    "coordSystem" : coordSystem(vector(0 * meter, 0 * meter, -washerThickness(definition.size, definition.washer)), X_DIRECTION, Z_DIRECTION),
                    "owner" : screw
                });

        cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
        setFastenerProperties(context, screw, size.name ~ " x " ~ lengthName(size, definition.length) ~ " " ~ HEAD_NAMES[definition.head] ~ " Screw");
    });

/**
 * A washer, from its bottom face at the origin up.
 */
annotation { "Feature Type Name" : "Quick fastener washer",
        "Feature Type Description" : "A washer for FRCDesign's Quick fastener, from the origin up." ~ CREDIT }
export const quickFastenerWasher = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Size", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.size is FastenerSize;

        annotation { "Name" : "Type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.washerType is WasherType;
    }
    {
        const size = FASTENER_SIZES[definition.size];
        const washer = WASHERS[definition.size][definition.washerType];

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
        setFastenerProperties(context, body, size.name ~ " " ~ WASHER_NAMES[definition.washerType] ~ " Washer");
    });

/**
 * A nut, from its bearing face at the origin up.
 */
annotation { "Feature Type Name" : "Quick fastener nut",
        "Feature Type Description" : "A nut for FRCDesign's Quick fastener, seated at the origin." ~ CREDIT }
export const quickFastenerNut = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Size", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.size is FastenerSize;

        annotation { "Name" : "Type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.nutType is NutType;

        annotation { "Name" : "Washer", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.washer is SeatWasher;
    }
    {
        const size = FASTENER_SIZES[definition.size];
        const nut = NUTS[definition.size][definition.nutType];

        const sketchId = id + "sketch";
        const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : XY_PLANE });
        skRegularPolygon(sketch, "hex", {
                    "center" : vector(0, 0) * meter,
                    "firstVertex" : vector(nut.acrossFlats / sqrt(3), 0 * meter),
                    "sides" : 6
                });
        skCircle(sketch, "hole", { "center" : vector(0, 0) * meter, "radius" : size.majorDiameter / 2 });
        skSolve(sketch);
        opExtrude(context, id + "extrude", {
                    "entities" : qSketchRegion(sketchId, true),
                    "direction" : Z_DIRECTION,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : nut.height
                });
        const body = qCreatedBy(id + "extrude", EntityType.BODY);

        opMateConnector(context, id + "mateConnector", {
                    "coordSystem" : coordSystem(vector(0 * meter, 0 * meter, -washerThickness(definition.size, definition.washer)), X_DIRECTION, Z_DIRECTION),
                    "owner" : body
                });

        cleanup(context, id + "deleteSketches", qCreatedBy(sketchId, EntityType.BODY));
        setFastenerProperties(context, body, size.name ~ " " ~ NUT_NAMES[definition.nutType] ~ " Nut");
    });

// The XZ plane, with the sketch's X along the radius and its Y along the axis
const PROFILE_PLANE = plane(WORLD_ORIGIN, -Y_DIRECTION, X_DIRECTION);

const HEAD_NAMES = { ScrewHead.SOCKET : "Socket Head", ScrewHead.BUTTON : "Button Head", ScrewHead.FLAT : "Flat Head" };
const WASHER_NAMES = { WasherType.FLAT : "Flat", WasherType.WIDE : "Wide" };
const NUT_NAMES = { NutType.NYLOCK : "Nylock", NutType.HEX : "Hex" };

/**
 * How far a flat head goes into its countersink: from its diameter down to the shank's at its countersink angle.
 */
export function flatHeadHeight(size is map, head is map) returns ValueWithUnits
{
    return (head.headDiameter - size.majorDiameter) / 2 / tan(head.countersinkAngle / 2);
}

/**
 * How far the washer a screw or nut sits on raises it.
 */
export function washerThickness(size is FastenerSize, washer is SeatWasher) returns ValueWithUnits
{
    if (washer == SeatWasher.NONE)
    {
        return 0 * meter;
    }
    return WASHERS[size][washer == SeatWasher.FLAT ? WasherType.FLAT : WasherType.WIDE].thickness;
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
 * A screw's length as its size names it, e.g. `1/2 in.` or `10 mm`.
 */
export function lengthName(size is map, length is ValueWithUnits) returns string
{
    if (size.metric)
    {
        return roundToPrecision(length / millimeter, 1) ~ " mm";
    }
    return roundToPrecision(length / inch, 4) ~ " in.";
}

function setFastenerProperties(context is Context, body is Query, name is string)
{
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.NAME, "value" : name });
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.MATERIAL, "value" : STEEL });
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.APPEARANCE, "value" : BLACK });
}
