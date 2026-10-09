FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
export import(path : "onshape/std/persistentCoordSystem.fs", version : "2960.0");

/**
 * What Robot chain and Robot sprocket share: chain sizes, sprockets' geometry, and the attributes each reads of the
 * other's.
 */

/**
 * Roller chain sizes. Stored in documents: never rename or remove one.
 */
export enum ChainType
{
    annotation { "Name" : "#25" }
    ANSI_25,
    annotation { "Name" : "#35" }
    ANSI_35,
    annotation { "Name" : "8mm (05B)" }
    ISO_05B
}

/**
 * A chain's dimensions (ANSI B29.1, and ISO 606 for 05B, which goBILDA's 8mm chain is).
 *
 * @returns {{
 *      @field pitch {ValueWithUnits} : The distance between its pins.
 *      @field rollerDiameter {ValueWithUnits} :
 *      @field innerWidth {ValueWithUnits} : Between its inner plates: the width of its rollers.
 *      @field plateHeight {ValueWithUnits} : How tall its plates are, across the chain.
 *      @field width {ValueWithUnits} : How wide it is, over its pins.
 * }}
 */
export function getChainInfo(chainType is ChainType) returns map
{
    return switch (chainType) {
                ChainType.ANSI_25 : {
                        "pitch" : 0.25 * inch,
                        "rollerDiameter" : 0.13 * inch,
                        "innerWidth" : 0.125 * inch,
                        "plateHeight" : 0.237 * inch,
                        "width" : 0.31 * inch
                    },
                ChainType.ANSI_35 : {
                        "pitch" : 0.375 * inch,
                        "rollerDiameter" : 0.2 * inch,
                        "innerWidth" : 0.1875 * inch,
                        "plateHeight" : 0.356 * inch,
                        "width" : 0.47 * inch
                    },
                ChainType.ISO_05B : {
                        "pitch" : 8 * millimeter,
                        "rollerDiameter" : 5 * millimeter,
                        "innerWidth" : 3 * millimeter,
                        "plateHeight" : 7.1 * millimeter,
                        "width" : 11.1 * millimeter
                    }
            };
}

export function getChainTypeName(chainType is ChainType) returns string
{
    return switch (chainType) {
                ChainType.ANSI_25 : "#25",
                ChainType.ANSI_35 : "#35",
                ChainType.ISO_05B : "8mm"
            };
}

/**
 * The radius of a sprocket's pitch circle, which its chain's pins are on.
 */
export function getSprocketRadius(pitch is ValueWithUnits, teeth is number) returns ValueWithUnits
{
    return pitch / (2 * sin(180 * degree / teeth));
}

/**
 * The teeth of a sprocket whose pitch circle has a radius of `radius`: not a whole number unless it's just right, and
 * `undefined` if no sprocket's that small.
 */
export function pitchCircleTeeth(pitch is ValueWithUnits, radius is ValueWithUnits)
{
    if (radius <= pitch / 2)
    {
        return undefined;
    }
    return 180 * degree / asin(pitch / (2 * radius));
}

/**
 * The width of a sprocket's teeth: ISO 606's, 0.93 of the chain's inner width.
 */
export function getSprocketToothWidth(chainType is ChainType) returns ValueWithUnits
{
    return 0.93 * getChainInfo(chainType).innerWidth;
}

/**
 * A sprocket's tooth form (ISO 606, with the most room for the chain): each gap's seating curve is an arc around
 * its roller's center (on the pitch circle), `seatingAngle` around, and each tooth's flanks are arcs tangent to the
 * seating curves out to the tip circle.
 *
 * @param profileOffset : How far the profile is offset outward (inward, if negative), like Robot pulley's.
 * @returns {{
 *      @field pitchRadius {ValueWithUnits} :
 *      @field seatingRadius {ValueWithUnits} : The seating curve's radius.
 *      @field seatingAngle {ValueWithUnits} : How far around the seating curve goes.
 *      @field flankRadius {ValueWithUnits} :
 *      @field tipRadius {ValueWithUnits} :
 *      @field rootRadius {ValueWithUnits} : The bottom of the gaps.
 * }}
 */
export function getSprocketToothForm(chainType is ChainType, teeth is number, profileOffset is ValueWithUnits) returns map
{
    const info = getChainInfo(chainType);
    // ISO 606's formulas are in millimeters
    const d1 = info.rollerDiameter / millimeter;
    const pitchRadius = getSprocketRadius(info.pitch, teeth);
    const seatingRadius = (0.505 * d1 + 0.069 * d1 ^ (1 / 3)) * millimeter - profileOffset;
    const flankRadius = 0.008 * d1 * (teeth ^ 2 + 180) * millimeter + profileOffset;
    // Halfway between the largest and smallest tips ISO 606 allows
    const maxTipDiameter = 2 * pitchRadius + 1.25 * info.pitch - info.rollerDiameter;
    const minTipDiameter = 2 * pitchRadius + info.pitch * (1 - 1.6 / teeth) - info.rollerDiameter;
    return {
            "pitchRadius" : pitchRadius,
            "seatingRadius" : seatingRadius,
            "seatingAngle" : 140 * degree - 90 * degree / teeth,
            "flankRadius" : flankRadius,
            "tipRadius" : (maxTipDiameter + minTipDiameter) / 4 + profileOffset,
            "rootRadius" : pitchRadius - seatingRadius
        };
}

/**
 * Sketches a sprocket's toothed profile on `plane` (centered on it) and returns its face: per tooth, a seating curve,
 * two flanks, and a tip (see `getSprocketToothForm`). A tooth whose flanks would cross below the tip circle is cut
 * off a little below where they cross, so it has a tip.
 */
export function sketchSprocketProfile(context is Context, id is Id, plane is Plane, chainType is ChainType, teeth is number,
    profileOffset is ValueWithUnits) returns Query
{
    const form = getSprocketToothForm(chainType, teeth, profileOffset);
    if (form.seatingRadius <= 0 * meter)
    {
        throw regenError("The profile offset is too large for this chain.", ["profileOffsetDistance"]);
    }
    const toothAngle = 360 * degree / teeth;
    const at = function(radius is ValueWithUnits, angle is ValueWithUnits) returns Vector
        {
            return vector(cos(angle), sin(angle)) * radius;
        };

    // The first gap's (at angle 0) counter clockwise flank, from the seating curve's end out; the rest are rotated
    // copies, and clockwise ones mirrored
    const roller = at(form.pitchRadius, 0 * degree);
    const seatingEnd = roller + at(form.seatingRadius, 180 * degree - form.seatingAngle / 2);
    // Tangent to the seating curve, curving the other way: its center's past the roller's, from the seating end
    const flankCenter = seatingEnd + (roller - seatingEnd) / form.seatingRadius * form.flankRadius;
    // A tooth whose flanks would cross before the tip circle is cut off a little below where they cross: its tip's
    // radius is found by bisection, leaving its tip a tenth of the tooth wide
    var tipRadius = form.tipRadius;
    const pastCenter = function(radius is ValueWithUnits) returns boolean
        {
            const end = flankTip(flankCenter, form.flankRadius, radius, seatingEnd);
            return end == undefined || atan2(end[1], end[0]) > 0.45 * toothAngle;
        };
    if (pastCenter(tipRadius))
    {
        var low = norm(seatingEnd);
        var high = tipRadius;
        for (var i = 0; i < 50; i += 1)
        {
            const middle = (low + high) / 2;
            if (pastCenter(middle))
            {
                high = middle;
            }
            else
            {
                low = middle;
            }
        }
        tipRadius = low;
    }
    const flankEnd = flankTip(flankCenter, form.flankRadius, tipRadius, seatingEnd);
    if (flankEnd == undefined || flankEnd[1] < seatingEnd[1] || tipRadius <= form.pitchRadius)
    {
        throw regenError("A " ~ teeth ~ " tooth sprocket's teeth don't fit this chain.", ["teeth"]);
    }
    const flankMiddle = flankCenter + normalize(normalize(seatingEnd - flankCenter) + normalize(flankEnd - flankCenter)) * form.flankRadius;
    const tipEndAngle = atan2(flankEnd[1], flankEnd[0]);

    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    for (var tooth = 0; tooth < teeth; tooth += 1)
    {
        const angle = tooth * toothAngle;
        const rotate = function(point is Vector) returns Vector
            {
                return vector(point[0] * cos(angle) - point[1] * sin(angle), point[0] * sin(angle) + point[1] * cos(angle));
            };
        const mirror = function(point is Vector) returns Vector
            {
                return vector(point[0], -point[1]);
            };
        skArc(sketch, "flankIn" ~ tooth, {
                    "start" : rotate(mirror(flankEnd)),
                    "mid" : rotate(mirror(flankMiddle)),
                    "end" : rotate(mirror(seatingEnd))
                });
        skArc(sketch, "seating" ~ tooth, {
                    "start" : rotate(mirror(seatingEnd)),
                    "mid" : rotate(roller + at(form.seatingRadius, 180 * degree)),
                    "end" : rotate(seatingEnd)
                });
        skArc(sketch, "flankOut" ~ tooth, {
                    "start" : rotate(seatingEnd),
                    "mid" : rotate(flankMiddle),
                    "end" : rotate(flankEnd)
                });
        skArc(sketch, "tip" ~ tooth, {
                    "start" : rotate(flankEnd),
                    "mid" : at(tipRadius, angle + toothAngle / 2),
                    "end" : at(tipRadius, angle + toothAngle - tipEndAngle)
                });
    }
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}

/**
 * Where a flank (a circle around `center`) reaches the tip circle, going out from `start`: the crossing nearer it.
 */
function flankTip(center is Vector, radius is ValueWithUnits, tipRadius is ValueWithUnits, start is Vector)
{
    const distance = norm(center);
    // The crossings of two circles: along the line between their centers, and off it
    const along = (distance ^ 2 + tipRadius ^ 2 - radius ^ 2) / (2 * distance);
    const offSquared = tipRadius ^ 2 - along ^ 2;
    if (offSquared < 0 * meter ^ 2)
    {
        return undefined;
    }
    const direction = center / distance;
    const normal = vector(-direction[1], direction[0]);
    const first = direction * along + normal * sqrt(offSquared);
    const second = direction * along - normal * sqrt(offSquared);
    return squaredNorm(first - start) < squaredNorm(second - start) ? first : second;
}

/**
 * Set on Robot sprockets (and their mate connectors), as a `SprocketAttribute`: Robot chain goes around them.
 */
export const SPROCKET_ATTRIBUTE = "robotSprocket";

export type SprocketAttribute typecheck canBeSprocketAttribute;

export predicate canBeSprocketAttribute(value)
{
    value is map;
    value.chainType is ChainType;
    value.teeth is number;
    value.coordSystem is PersistentCoordSystem;
}

/**
 * A sprocket or idler of a chain.
 */
export enum SprocketType
{
    annotation { "Name" : "Sprocket" }
    SPROCKET,
    annotation { "Name" : "Idler" }
    IDLER
}

/**
 * Set on a chain's curved faces around each sprocket or idler, and its mate connectors, as a `ChainFaceAttribute`:
 * Robot sprocket makes sprockets on them.
 */
export const CHAIN_SPROCKET_FACE_ATTRIBUTE = "robotChainSprocketFace";

export type ChainFaceAttribute typecheck canBeChainFaceAttribute;

export predicate canBeChainFaceAttribute(value)
{
    value is map;
    value.chainType is ChainType;
    value.sprocketType is SprocketType;
    if (value.sprocketType == SprocketType.SPROCKET)
    {
        value.teeth is number;
    }
    else
    {
        isLength(value.idlerRadius);
    }
}
