FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "onshape/std/cosmeticThreadUtils.fs", version : "2960.0");
import(path : "onshape/std/externalThread.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
// Also exports the enums used as parameter types
export import(path : "86033ffc9510cee8dd457b7f", version : "41e4e704429c4965ab6949e1");

/**
 * FRCDesign's library fasteners: its screws, washers, and nuts, generated for FRCDesignApp. Each is made in a Part
 * Studio of its own, whose configuration sets every parameter and offers only what's made (see fastenerTables.py),
 * so these dialogs are rarely seen: each is a flat list of enums, with no conditions, which the configuration filters.
 *
 * Each part is built along the Z axis, with one mate connector at the origin pointing out of the hole: where it seats
 * against the part or washer it sits on. Screws and nuts are threaded as std's External thread and Hole features thread
 * them: cosmetically, with the same hole attributes.
 */

// Proportions of McMaster-Carr's models, of the head's or shank's diameter
const TIP_CHAMFER = 0.07;
const SOCKET_HEAD_CHAMFER = 0.021;
const BUTTON_HEAD_SIDE = 0.041;
const FLAT_HEAD_BAND = 0.006;
// A button head's flat top, of its socket's size
const BUTTON_HEAD_TOP = 1.4;
// A standard socket head screw's threads run 2D + 1/2 in from its tip (ASTM A574)
const SOCKET_HEAD_THREAD_ALLOWANCE = 0.5 * inch;
// Nuts' chamfers, from the circle across their flats, as McMaster-Carr models them
const NUT_CHAMFER_ANGLE = 30 * degree;
const NYLOCK_CHAMFER_ANGLE = 25 * degree;
// How far a nylock's nylon insert sits below the top of its dome
const NYLON_RECESS = 0.009 * inch;
const NYLON_COLOR = color(1, 1, 1);

annotation { "Feature Type Name" : "Screw",
        "Feature Type Description" : "A standard screw from FRCDesign's library, seated at the origin." ~ CREDIT }
export const fastenerScrew = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        vendorPredicate(definition);

        sizePredicate(definition);

        annotation { "Name" : "Head", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.head is ScrewHead;

        annotation { "Name" : "Finish", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.finish is ScrewFinish;

        annotation { "Name" : "Length", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.length is ScrewLength;

        annotation { "Name" : "Fully threaded", "Default" : false, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.fullyThreaded is boolean;
    }
    {
        const finishes = SCREWS[definition.vendor][definition.size][definition.head];
        if (finishes == undefined || finishes[definition.finish] == undefined)
        {
            throw regenError("Selected vendor does not make this screw in this finish, change the finish.", ["finish"]);
        }
        const threads = finishes[definition.finish][definition.length];
        if (threads == undefined)
        {
            throw regenError("Selected vendor does not make this screw, change the length.", ["length"]);
        }
        // The configuration hides Fully threaded where a length is sold one way, and a hidden input keeps its last value
        const fullThread = definition.fullyThreaded && threads.fullThread != undefined;
        const stock = fullThread ? threads.fullThread : threads.standard;
        const size = FASTENER_SIZES[definition.size];
        const head = SCREW_HEADS[definition.size][definition.head];
        const length = SCREW_LENGTHS[definition.length];
        const isFlat = definition.head == ScrewHead.FLAT;

        // 1. Revolve the screw's profile: its head above the origin, or a flat head's below it, and its shank down to its
        // chamfered tip
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
                    "endDepth" : socketDepth(size, head, definition.head)
                });
        opBoolean(context, socketId + "cut", {
                    "tools" : qCreatedBy(socketId + "extrude", EntityType.BODY),
                    "targets" : screw,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });

        // 3. Thread its shank up from its tip
        addScrewThread(context, id + "thread", screw, size, head, definition.head, length.length, fullThread);

        // 4. Seat it, under its head
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

        annotation { "Name" : "Finish", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.finish is WasherFinish;
    }
    {
        const finishes = WASHERS[definition.vendor][definition.size][definition.washerType];
        if (finishes == undefined)
        {
            throw regenError("Selected vendor does not make this washer, change the type.", ["washerType"]);
        }
        const washer = made(finishes, definition.finish, WasherFinish.BLACK);
        if (washer == undefined)
        {
            throw regenError("Selected vendor does not make this washer in this finish, change the finish.", ["finish"]);
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
        const isNylock = isNylockType(definition.nutType);

        // 1. Extrude its hex or square
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
        skSolve(sketch);
        opExtrude(context, id + "extrude", {
                    "entities" : qSketchRegion(sketchId),
                    "direction" : Z_DIRECTION,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : nut.height
                });

        // 2. Keep what's inside its profile, revolved: its bore, chamfers, and a nylock's dome and insert
        const profileId = id + "profile";
        const profileSketch = newSketchOnPlane(context, profileId, { "sketchPlane" : PROFILE_PLANE });
        skPolyline(profileSketch, "profile", { "points" : nutProfile(size, nut, definition.nutType) });
        if (isNylock)
        {
            skArc(profileSketch, "dome", nylockDome(nut));
        }
        skSolve(profileSketch);
        opRevolve(context, id + "revolve", {
                    "entities" : qSketchRegion(profileId),
                    "axis" : line(WORLD_ORIGIN, Z_DIRECTION),
                    "angleForward" : 2 * PI * radian
                });
        opBoolean(context, id + "intersect", {
                    "tools" : qUnion([qCreatedBy(id + "extrude", EntityType.BODY), qCreatedBy(id + "revolve", EntityType.BODY)]),
                    "operationType" : BooleanOperationType.INTERSECTION
                });
        // The intersection is a new body, as std's own intersections find it
        const body = qCreatedBy(id + "intersect", EntityType.BODY);

        // 3. Tap its bore, up to a nylock's insert, and seat it
        addNutThread(context, id + "thread", body, size, isNylock ? nut.height - NYLON_RECESS : nut.height);
        addSeatMateConnector(context, id + "mateConnector", body);

        cleanup(context, id + "deleteSketches", qUnion([qCreatedBy(sketchId, EntityType.BODY), qCreatedBy(profileId, EntityType.BODY)]));
        setFastenerProperties(context, body, nut);
        if (isNylock)
        {
            const nylon = qOwnedByBody(body, EntityType.FACE)->qGeometry(GeometryType.PLANE)
                ->qContainsPoint(vector((size.tapDrillDiameter + nut.insertDiameter) / 4, 0 * meter, nut.height - NYLON_RECESS));
            setProperty(context, { "entities" : nylon, "propertyType" : PropertyType.APPEARANCE, "value" : NYLON_COLOR });
        }
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

/**
 * What's made in the value chosen, or else in the standard one. The configuration hides an input where only the
 * standard is made, and a hidden input keeps whatever it was last set to.
 */
function made(entries is map, value, standard)
{
    const entry = entries[value];
    return entry != undefined ? entry : entries[standard];
}

// The XZ plane, with the sketch's X along the radius and its Y along the axis
const PROFILE_PLANE = plane(WORLD_ORIGIN, -Y_DIRECTION, X_DIRECTION);


function isNylockType(nutType is NutType) returns boolean
{
    return nutType == NutType.NYLOCK || nutType == NutType.THIN_NYLOCK;
}

/**
 * A nut's profile, which it's the revolve of within its hex or square: up its bore from its bearing face, and back down
 * its chamfers, both of a hex's ends, a square's top only. A nylock's runs from its dome's top (see `nylockDome`) down
 * into its insert's recess and back to the dome's start, which the dome closes; any other's closes on itself.
 */
export function nutProfile(size is map, nut is map, nutType is NutType) returns array
{
    const zero = 0 * meter;
    const bore = size.tapDrillDiameter / 2;
    const flat = nut.acrossFlats / 2;
    const isSquare = nutType == NutType.SQUARE;
    const isNylock = isNylockType(nutType);
    // Past the corners, where a chamfer's line leaves the nut
    const outer = 1.05 * flat * (isSquare ? sqrt(2) : 2 / sqrt(3));
    const rise = (outer - flat) * tan(isNylock ? NYLOCK_CHAMFER_ANGLE : NUT_CHAMFER_ANGLE);
    const bottom = isSquare ? [vector(bore, zero), vector(outer, zero)] : [vector(bore, zero), vector(flat, zero), vector(outer, rise)];
    if (!isNylock)
    {
        return concatenateArrays([
                    bottom,
                    [vector(outer, nut.height - rise), vector(flat, nut.height), vector(bore, nut.height), vector(bore, zero)]
                ]);
    }
    const top = nut.height - NYLON_RECESS;
    return concatenateArrays([
                [vector(flat - (nut.height - nut.hexHeight), nut.height), vector(nut.insertDiameter / 2, nut.height),
                    vector(nut.insertDiameter / 2, top), vector(bore, top)],
                bottom,
                [vector(outer, nut.hexHeight - rise), vector(flat, nut.hexHeight)]
            ]);
}

/**
 * A nylock's dome, from the top of its hex across its flats up to its flat top: a quarter circle as tall as the nut is
 * above its hex.
 */
export function nylockDome(nut is map) returns map
{
    const flat = nut.acrossFlats / 2;
    const radius = nut.height - nut.hexHeight;
    const center = vector(flat - radius, nut.hexHeight);
    return {
            "start" : vector(flat, nut.hexHeight),
            "mid" : center + radius * vector(cos(45 * degree), sin(45 * degree)),
            "end" : vector(flat - radius, nut.height)
        };
}

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
 * How far a flat head's cone goes into its countersink: from its diameter down to the shank's at its countersink angle.
 */
export function flatHeadHeight(size is map, head is map) returns ValueWithUnits
{
    return (head.headDiameter - size.majorDiameter) / 2 / tan(head.countersinkAngle / 2);
}

/**
 * How far a flat head goes below its top: its band, then its cone.
 */
export function flatHeadDepth(size is map, head is map) returns ValueWithUnits
{
    return FLAT_HEAD_BAND * head.headDiameter + flatHeadHeight(size, head);
}

export function tipChamfer(size is map) returns ValueWithUnits
{
    return TIP_CHAMFER * size.majorDiameter;
}

export function socketDepth(size is map, head is map, headType is ScrewHead) returns ValueWithUnits
{
    if (headType == ScrewHead.FLAT)
    {
        return 0.46 * flatHeadHeight(size, head);
    }
    return (headType == ScrewHead.BUTTON ? 0.45 : 0.5) * head.headHeight;
}

/**
 * How far a screw's threads run up from its tip: to its head, but for a standard socket head's, which stop 2D + 1/2 in
 * up.
 */
export function threadLength(size is map, head is map, headType is ScrewHead, length is ValueWithUnits, fullThread is boolean) returns ValueWithUnits
{
    const shank = headType == ScrewHead.FLAT ? length - flatHeadDepth(size, head) : length;
    if (headType != ScrewHead.SOCKET || fullThread)
    {
        return shank;
    }
    return min(shank, 2 * size.majorDiameter + SOCKET_HEAD_THREAD_ALLOWANCE);
}

/**
 * A screw's profile in `PROFILE_PLANE`: (radius, height) points around it, through its axis. A socket or flat head's
 * closes on itself; a button head's runs from the top of its side to the edge of its flat top, which `buttonDome`
 * joins.
 */
export function screwProfile(size is map, head is map, headType is ScrewHead, length is ValueWithUnits) returns array
{
    const radius = size.majorDiameter / 2;
    const headRadius = head.headDiameter / 2;
    const tip = tipChamfer(size);
    const zero = 0 * meter;
    const shank = [vector(radius, -length + tip), vector(radius - tip, -length), vector(zero, -length)];
    if (headType == ScrewHead.FLAT)
    {
        // A flat head's length is from its top, which sits flush with the part
        const band = FLAT_HEAD_BAND * head.headDiameter;
        return concatenateArrays([
                    [vector(zero, zero), vector(headRadius, zero), vector(headRadius, -band), vector(radius, -flatHeadDepth(size, head))],
                    shank,
                    [vector(zero, zero)]
                ]);
    }
    const underHead = [vector(headRadius, zero), vector(radius, zero)];
    if (headType == ScrewHead.BUTTON)
    {
        return concatenateArrays([
                    [vector(headRadius, buttonSideHeight(head))],
                    underHead,
                    shank,
                    [vector(zero, head.headHeight), vector(buttonTopRadius(head), head.headHeight)]
                ]);
    }
    const chamfer = SOCKET_HEAD_CHAMFER * head.headDiameter;
    return concatenateArrays([
                [vector(zero, head.headHeight), vector(headRadius - chamfer, head.headHeight), vector(headRadius, head.headHeight - chamfer)],
                underHead,
                shank,
                [vector(zero, head.headHeight)]
            ]);
}

function buttonSideHeight(head is map) returns ValueWithUnits
{
    return BUTTON_HEAD_SIDE * head.headDiameter;
}

function buttonTopRadius(head is map) returns ValueWithUnits
{
    return BUTTON_HEAD_TOP * head.socketSize / 2;
}

/**
 * A button head's dome: an arc centered on the axis, from the top of the head's side to the edge of its flat top.
 */
export function buttonDome(head is map) returns map
{
    const side = vector(head.headDiameter / 2, buttonSideHeight(head));
    const top = vector(buttonTopRadius(head), head.headHeight);
    // The center's height, equally far from both
    const height = (squaredNorm(top) - squaredNorm(side)) / (2 * (top[1] - side[1]));
    const center = vector(0 * meter, height);
    const radius = norm(side - center);
    const middle = center + radius * normalize(normalize(side - center) + normalize(top - center));
    return { "start" : side, "mid" : middle, "end" : top };
}

/**
 * Threads a screw's shank from the edge above its tip chamfer, as std's External thread does: its hole attribute on the
 * shank and that edge, and on the chamfer as its chamfer, and a cosmetic thread on the shank.
 */
function addScrewThread(context is Context, id is Id, screw is Query, size is map, head is map, headType is ScrewHead,
    length is ValueWithUnits, fullThread is boolean)
{
    const radius = size.majorDiameter / 2;
    const tip = tipChamfer(size);
    const start = vector(0 * meter, 0 * meter, -length + tip);
    const shaft = threadLength(size, head, headType, length, true) - tip;
    const threaded = threadLength(size, head, headType, length, fullThread) - tip;
    const faces = qOwnedByBody(screw, EntityType.FACE);
    const shank = faces->qGeometry(GeometryType.CYLINDER)->qContainsPoint(start + vector(radius, 0 * meter, shaft / 2));
    const edge = qOwnedByBody(screw, EntityType.EDGE)->qGeometry(GeometryType.CIRCLE)->qContainsPoint(start + vector(radius, 0 * meter, 0 * meter));
    const chamfer = faces->qGeometry(GeometryType.CONE)->qContainsPoint(start + vector(radius - tip / 2, 0 * meter, -tip / 2));

    // Up the shank, from the tip
    const cylinder = evSurfaceDefinition(context, { "face" : shank });
    const aligned = dot(cylinder.coordSystem.zAxis, Z_DIRECTION) > 0;
    const isBlind = threaded < shaft - TOLERANCE.zeroLength * meter;

    var attribute = makeHoleAttribute(true, toAttributeId(id), HoleStyle.SIMPLE);
    attribute.isTappedHole = true;
    // fs check: ignore keyword-key (std's hole attributes name it "type")
    attribute.sectionFace = { "type" : HoleSectionFaceType.THROUGH_FACE };
    attribute.isExternalThread = true;
    attribute.minorDiameter = { "value" : size.minorDiameter };
    attribute.majorDiameter = { "value" : size.majorDiameter };
    attribute.tappedDepth = { "value" : threaded };
    attribute.holeDepth = { "value" : shaft };
    attribute.holeDiameter = { "value" : size.majorDiameter };
    attribute.endType = isBlind ? HoleEndStyle.BLIND : HoleEndStyle.THROUGH;
    attribute.tapSize = size.name;
    attribute.isTappedThrough = !isBlind;
    attribute.isTaperedPipeTapHole = false;
    attribute.isStraightPipeTapHole = false;
    attribute.tapClearance = 0.0;
    attribute.cylinderAlignedWithThreadDirection = aligned;
    attribute.threadFaceModificationType = ThreadFaceModificationType.NONE;
    setAttribute(context, { "entities" : qUnion([shank, edge]), "attribute" : attribute });
    // fs check: ignore keyword-key (std's hole attributes name it "type")
    attribute.sectionFace = { "type" : HoleSectionFaceType.EXTERNAL_THREAD_CHAMFER_FACE };
    setAttribute(context, { "entities" : chamfer, "attribute" : attribute });

    addCosmeticThreadAttribute(context, shank, createCosmeticThreadDataFromEntity(
                coordSystem(start, aligned ? cylinder.coordSystem.xAxis : -cylinder.coordSystem.xAxis, Z_DIRECTION),
                threaded / meter, size.threadPitch / meter));
}

/**
 * Taps a nut's bore through, as std's Hole feature does: its hole attribute, and a cosmetic thread up from its bearing
 * face.
 */
function addNutThread(context is Context, id is Id, nut is Query, size is map, height is ValueWithUnits)
{
    const bore = qOwnedByBody(nut, EntityType.FACE)->qGeometry(GeometryType.CYLINDER)
        ->qContainsPoint(vector(size.tapDrillDiameter / 2, 0 * meter, height / 2));

    var attribute = makeHoleAttribute(true, toAttributeId(id), HoleStyle.SIMPLE);
    attribute.endType = HoleEndStyle.THROUGH;
    // fs check: ignore keyword-key (std's hole attributes name it "type")
    attribute.sectionFace = { "type" : HoleSectionFaceType.THROUGH_FACE };
    attribute.holeDiameter = size.tapDrillDiameter;
    attribute.majorDiameter = size.majorDiameter;
    attribute.isTappedHole = true;
    attribute.isTaperedPipeTapHole = false;
    attribute.isStraightPipeTapHole = false;
    attribute.tappedAngle = 0.0;
    attribute.tapSize = size.name;
    // As std's Hole sets a hole tapped through
    attribute.isTappedThrough = true;
    attribute.tappedDepth = 0 * meter;
    attribute.threadPitch = size.threadPitch;
    setAttribute(context, { "entities" : bore, "attribute" : attribute });

    addCosmeticThreadAttribute(context, bore, createCosmeticThreadDataFromEntity(
                coordSystem(WORLD_ORIGIN, X_DIRECTION, Z_DIRECTION), height / meter, size.threadPitch / meter));
}

/**
 * Sets a part's properties from its table entry (see fastenerTables.py's `Record`): its name, its vendor and part
 * number, and a link to buy it where it has them, and its material and its finish's appearance. FRCDesignApp indexes
 * the same records, so these are the only properties set.
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
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.MATERIAL, "value" : entry.material });
    setProperty(context, { "entities" : body, "propertyType" : PropertyType.APPEARANCE, "value" : entry.appearance });
}
