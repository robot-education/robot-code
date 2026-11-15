FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
export import(path : "onshape/std/persistentCoordSystem.fs", version : "2960.0");

import(path : "00b10ef1fb1a7418097fc0af", version : "2994750994597bce75afd4a7");
import(path : "948c83c1b1ac83de4ccf921b", version : "5bd77bd9ef3230343bfbc217");

export const PULLEY_TEETH_BOUNDS = { (unitless) : [2, 24, 1e50] } as IntegerBoundSpec;

export const PULLEY_ATTRIBUTE = "robotPulley";

/**
 * Set on pulleys and idlers (and their mate connectors), for Robot belt to put belts on them.
 * @type {{
 *      @field pulleyTeeth {number} : A pulley's teeth. An idler has none, but an `idlerRadius`.
 *      @field idlerRadius {ValueWithUnits} : An idler's radius.
 * }}
 */
export type PulleyAttribute typecheck canBePulleyAttribute;

export predicate canBePulleyAttribute(value)
{
    value is map;
    value.beltType is BeltType;
    value.pulleyTeeth is number || isLength(value.idlerRadius);
    value.twoBelts is boolean;
    value.coordSystem is PersistentCoordSystem;
}

export const IDLER_DIAMETER_BOUNDS = { (meter) : [1e-4, 0.0254, 500], (inch) : 1, (millimeter) : 25 } as LengthBoundSpec;

export enum FlangeSize
{
    annotation { "Name" : "Small" }
    SMALL,
    annotation { "Name" : "Medium" }
    MEDIUM,
    annotation { "Name" : "Large" }
    LARGE
}

export function getPulleyRadius(pitch is ValueWithUnits, pulleyTeeth is number)
{
    return getPulleyDiameter(pitch, pulleyTeeth) / 2;
}

export function getPulleyDiameter(pitch is ValueWithUnits, pulleyTeeth is number)
{
    return pulleyTeeth * pitch / PI;
}

/**
 * Returns the amount to add to the pulley's pitch diameter.
 */
export function getFlangeRadiusOffset(beltType is BeltType, flangeSize is FlangeSize, unitSystem is UnitSystem) returns ValueWithUnits
{
    // Use two standard pulley sets, one for "small" belts and one for "large" ones
    if (beltType == BeltType._2_MM_GT2 || beltType == BeltType._3_MM_GT2 || beltType == BeltType._3_MM_HTD)
    {
        return switch (flangeSize) {
                    FlangeSize.SMALL : unitSystemValue(unitSystem, 3 / 64 * inch, 1.5 * millimeter),
                    FlangeSize.MEDIUM : unitSystemValue(unitSystem, 3 / 32 * inch, 2.5 * millimeter),
                    FlangeSize.LARGE : unitSystemValue(unitSystem, 5 / 32 * inch, 4 * millimeter)
                };
    }
    return switch (flangeSize) {
                FlangeSize.SMALL : unitSystemValue(unitSystem, 3 / 32 * inch, 2.5 * millimeter),
                FlangeSize.MEDIUM : unitSystemValue(unitSystem, 7 / 32 * inch, 6 * millimeter),
                FlangeSize.LARGE : unitSystemValue(unitSystem, 11 / 32 * inch, 9 * millimeter)
            };
}

/**
 * Returns the width of a pully flange.
 */
export function getFlangeWidth(beltType is BeltType, flangeSize is FlangeSize, unitSystem is UnitSystem) returns ValueWithUnits
{
    if (beltType == BeltType._2_MM_GT2 || beltType == BeltType._3_MM_GT2 || beltType == BeltType._3_MM_HTD)
    {
        return switch (flangeSize) {
                    FlangeSize.SMALL : unitSystemValue(unitSystem, 0.0625 * inch, 2 * millimeter),
                    FlangeSize.MEDIUM : unitSystemValue(unitSystem, 0.125 * inch, 3 * millimeter),
                    FlangeSize.LARGE : unitSystemValue(unitSystem, 0.1875 * inch, 5 * millimeter)
                };
    }
    return switch (flangeSize) {
                FlangeSize.SMALL : unitSystemValue(unitSystem, 0.125 * inch, 3 * millimeter),
                FlangeSize.MEDIUM : unitSystemValue(unitSystem, 0.25 * inch, 7 * millimeter),
                FlangeSize.LARGE : unitSystemValue(unitSystem, 0.375 * inch, 10 * millimeter)
            };
}

/**
 * Returns the width a pulley should have.
 */
export function getBasePulleyTeethWidth(beltType is BeltType, beltWidth is ValueWithUnits, unitSystem is UnitSystem) returns ValueWithUnits
{
    // ISO Belt widths:
    // BeltSize.GT2 : 10.16 * millimeter,
    // BeltSize.HTD_9_MM : 10.4 * millimeter,
    // BeltSize.HTD_15_MM : 16.51 * millimeter,
    // BeltSize.RT25 : 0.5625 * inch

    // Fixed values to convert to the appropriate unit system and get close to ISO
    // Assume all belts except RT25 have a metric width
    // Note beltWidth is always in the unit system of the belt, so we need to convert unit systems as well
    if (beltType == BeltType.RT25)
    {
        // Okay to hard code this conversion since RT25 belts are always the same width
        // Comes out to 16 mm, or 0.63 inches
        return unitSystemValue(unitSystem, 0.0625 * inch, ceil(beltWidth, 1 * millimeter) + 3 * millimeter);
    }
    // This applies to belts of various widths, but assume a 15 mm HTD belt as an example
    return unitSystemValue(unitSystem, ceil(beltWidth, 0.0625 * inch) + 0.0625 * inch, beltWidth + 2 * millimeter);
}

/**
 * Returns the extra width a two belt pulley face should have compared to a standard pulley.
 * Note these sizes are either the belt width or the belt width rounded up to the next whole unit.
 */
export function getTwoBeltPulleyExtraWidth(beltType is BeltType, beltWidth is ValueWithUnits, unitSystem is UnitSystem) returns ValueWithUnits
{
    // const _9mmSize = unitSystemValue(unitSystem, 0.375 * inch, getBeltWidth(beltType));
    // return switch (beltType) {
    //             BeltType.GT2 : _9mmSize,
    //             BeltType.HTD_9_MM : _9mmSize,
    //             BeltType.HTD_15_MM : unitSystemValue(unitSystem, 0.625 * inch, getBeltWidth(beltType)),
    //             BeltType.RT25 : unitSystemValue(unitSystem, getBeltWidth(beltType), 13 * millimeter)
    //         };

    // We just round up the beltWidth to the nearest nice unit system value
    return ceil(beltWidth, unitSystemValue(unitSystem, 1 / 16 * inch, 1 * millimeter));
}

/**
 * A pulley's groove for a belt of `beltType`, as vendors' pulleys have it (`vendor/pulley-grooves.md`), before
 * `GROOVE_CLEARANCE`. Its bottom is an arc on each side of its axis (one arc, if they're the same), joined by a line.
 * Its sides are flanks (a GT2's or RT25's: arcs around centers on the lands' circle across the groove, which the
 * bottom's arc on its side is inside, tangent), straight walls (5mm HTD's: lines tangent to the bottom's arcs), or the
 * bottom's arcs going on (3mm HTD's); and the lands' corners are rounded.
 *
 * @returns {{
 *      @field tipRadius {ValueWithUnits} : The bottom's arcs' radius.
 *      @field tipDepth {ValueWithUnits} : How far inside the lands' circle their centers are.
 *      @field tipSpread {ValueWithUnits} : How far across the groove's axis each one's center is (zero for one arc).
 *      @field flankRadius {ValueWithUnits} : @optional The flanks' radius.
 *      @field wallAngle {ValueWithUnits} : @optional The walls' angle to the groove's axis, opening out.
 *      @field filletRadius {ValueWithUnits} : The lands' corners' radius.
 * }}
 */
export function getPulleyGroove(beltType is BeltType) returns map
{
    return switch (beltType) {
                // goBILDA's: Gates' GT2 tooth (R3 0.555 mm, 0.75 mm tall, R2 1 mm flanks, R1 0.15 mm), its arcs 0.075 mm bigger
                BeltType._2_MM_GT2 : { "tipRadius" : 0.63 * millimeter, "tipDepth" : 0.195 * millimeter, "tipSpread" : 0 * meter,
                        "flankRadius" : 1.075 * millimeter, "filletRadius" : 0.15 * millimeter },
                // Gates' 3mm GT2 tooth (R3 0.85 mm, 1.14 mm tall, R2 1.52 mm flanks, R1 0.25 mm), its arcs 0.075 mm
                // bigger, as goBILDA's 2mm grooves are: within 0.05 mm of WCP's grooves (which have straight sides)
                BeltType._3_MM_GT2 : { "tipRadius" : 0.925 * millimeter, "tipDepth" : 0.29 * millimeter, "tipSpread" : 0 * meter,
                        "flankRadius" : 1.595 * millimeter, "filletRadius" : 0.25 * millimeter },
                // goBILDA's
                BeltType._3_MM_HTD : { "tipRadius" : 0.8636 * millimeter, "tipDepth" : 0.4572 * millimeter, "tipSpread" : 0.0305 * millimeter,
                        "filletRadius" : 0.2694 * millimeter },
                // goBILDA's
                BeltType._5_MM_HTD : { "tipRadius" : 1.1938 * millimeter, "tipDepth" : 0.8097 * millimeter, "tipSpread" : 0.3302 * millimeter,
                        "wallAngle" : 9.794 * degree, "filletRadius" : 0.4699 * millimeter },
                // REV's (its aluminum pulleys' fillets; its plastic ones' are 0.8865 mm)
                BeltType.RT25 : { "tipRadius" : 1.8017 * millimeter, "tipDepth" : 0.6113 * millimeter, "tipSpread" : 0 * meter,
                        "flankRadius" : 3.2199 * millimeter, "filletRadius" : 0.5266 * millimeter }
            };
}

/**
 * How much bigger a printed pulley's grooves are than vendors' (`getPulleyGroove`), all around (their bottoms' and
 * flanks' radii, around the same centers), as printed grooves come out a little small: 0.2 mm wider and 0.1 mm deeper,
 * as droftarts' Parametric Pulley's 0.2 mm additional tooth width. Its lands, which the belt's pitch line rides on,
 * aren't moved.
 */
export const GROOVE_CLEARANCE = 0.1 * millimeter;

/**
 * Sketches the toothed profile of a pulley for a belt of `beltType` with `teeth` teeth, centered on `plane`, and returns
 * its face: grooves as vendors' pulleys have them (`getPulleyGroove`), `GROOVE_CLEARANCE` bigger, between lands at the
 * belt's inside (its `insideThickness` inside the pitch circle).
 *
 * @param profileOffset : How far the profile is offset outward (inward, if negative): to make the pulley a little
 *          bigger or smaller, for a tighter or looser belt. Every arc keeps its center: convex arcs grow, and concave
 *          ones shrink.
 */
export function sketchPulleyProfile(context is Context, id is Id, plane is Plane, beltType is BeltType, teeth is number,
    profileOffset is ValueWithUnits) returns Query
{
    return sketchPulleyGrooves(context, id, plane, beltType, teeth, profileOffset, GROOVE_CLEARANCE);
}

/**
 * `sketchPulleyProfile`, with grooves `clearance` bigger than vendors'.
 */
export function sketchPulleyGrooves(context is Context, id is Id, plane is Plane, beltType is BeltType, teeth is number,
    profileOffset is ValueWithUnits, clearance is ValueWithUnits) returns Query
{
    const groove = getPulleyGroove(beltType);
    const pitchRadius = getPulleyRadius(getBeltPitch(beltType), teeth);
    const toothAngle = 360 * degree / teeth;
    const origin = zeroVector(2) * meter;
    const tooFew = "A " ~ getBeltTypeName(beltType) ~ " pulley needs more teeth than " ~ teeth ~ ".";

    // The first groove's, on the x axis: the rest are rotated copies. Its counter clockwise side's (+y) curves are
    // found, and its clockwise side's are their mirror images.
    const landRadius = pitchRadius - getBeltModelInfo(beltType).insideThickness;
    const tipCenterRadius = landRadius - groove.tipDepth;
    const tipCenter = vector(sqrt(tipCenterRadius ^ 2 - groove.tipSpread ^ 2), groove.tipSpread);
    const tipRadius = groove.tipRadius + clearance - profileOffset;
    const filletRadius = groove.filletRadius + profileOffset;
    // The groove's side: its flank, or the bottom's arc itself
    var sideCenter = tipCenter;
    var sideRadius = tipRadius;
    const hasFlanks = groove.flankRadius != undefined;
    if (hasFlanks)
    {
        // On the lands' circle, across the groove, as far from the bottom's arc's center as it's inside the flank
        const centers = circleIntersections(origin, landRadius, tipCenter, groove.flankRadius - groove.tipRadius);
        if (centers == [])
        {
            throw regenError(tooFew, ["pulleyTeeth"]);
        }
        sideCenter = centers[0][1] < centers[1][1] ? centers[0] : centers[1];
        sideRadius = groove.flankRadius + clearance - profileOffset;
    }
    if (tipRadius <= 0 * meter || sideRadius <= 0 * meter || filletRadius <= 0 * meter)
    {
        throw regenError("The profile offset is too large for this belt's teeth.", ["profileOffsetDistance"]);
    }

    // The fillet's center is inside the lands' circle (the same, offset or not), and outside the side, tangent to both
    const filletCenterRadius = landRadius - groove.filletRadius;
    const hasWalls = groove.wallAngle != undefined;
    var filletCenter;
    var onSide;
    var onTip;
    if (hasWalls)
    {
        // The wall's tangent to the bottom's arc where the arc's square to it, and goes out from there, so the fillet's
        // center is a fillet's radius off the wall, out along it as far as the lands' circle less the fillet's radius
        const along = vector(cos(groove.wallAngle), sin(groove.wallAngle));
        const across = vector(-sin(groove.wallAngle), cos(groove.wallAngle));
        onTip = tipCenter + across * tipRadius;
        const offWall = onTip + across * filletRadius;
        const toCenter = dot(offWall, along);
        const discriminant = toCenter ^ 2 - squaredNorm(offWall) + filletCenterRadius ^ 2;
        if (discriminant < 0 * meter ^ 2)
        {
            throw regenError(tooFew, ["pulleyTeeth"]);
        }
        const out = -toCenter + sqrt(discriminant);
        if (out < 0 * meter)
        {
            throw regenError(tooFew, ["pulleyTeeth"]);
        }
        filletCenter = offWall + along * out;
        onSide = onTip + along * out;
    }
    else
    {
        const filletCenters = circleIntersections(origin, filletCenterRadius, sideCenter, sideRadius + filletRadius);
        if (filletCenters == [])
        {
            throw regenError(tooFew, ["pulleyTeeth"]);
        }
        filletCenter = filletCenters[0][1] > filletCenters[1][1] ? filletCenters[0] : filletCenters[1];
        onSide = sideCenter + normalize(filletCenter - sideCenter) * sideRadius;
        // Where a flank meets the bottom's arc: on the line from the flank's center through the arc's
        onTip = hasFlanks ? sideCenter + normalize(tipCenter - sideCenter) * sideRadius : onSide;
    }
    const filletAngle = atan2(filletCenter[1], filletCenter[0]);
    const onLand = filletCenter / norm(filletCenter) * (landRadius + profileOffset);
    // The bottom's arc's innermost point, square to the groove's axis, where the line across the bottom meets it
    const bottom = tipCenter - vector(tipRadius, 0 * meter);
    const hasFlat = !tolerantEqualsZero(groove.tipSpread);
    // A groove's fillets can't reach past the middle of its lands, and its sides must go out from its bottom
    if (filletAngle >= toothAngle / 2 || onTip[1] <= bottom[1] || (hasFlanks && norm(onSide) <= norm(onTip)))
    {
        throw regenError(tooFew, ["pulleyTeeth"]);
    }
    const middle = function(center is Vector, radius is ValueWithUnits, start is Vector, end is Vector) returns Vector
        {
            return center + normalize(normalize(start - center) + normalize(end - center)) * radius;
        };
    const filletMiddle = middle(filletCenter, filletRadius, onLand, onSide);
    const flankMiddle = middle(sideCenter, sideRadius, onTip, onSide);
    const tipMiddle = middle(tipCenter, tipRadius, bottom, onTip);
    const mirror = function(point is Vector) returns Vector
        {
            return vector(point[0], -point[1]);
        };

    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    for (var tooth = 0; tooth < teeth; tooth += 1)
    {
        const angle = tooth * toothAngle;
        const rotate = function(point is Vector) returns Vector
            {
                return vector(point[0] * cos(angle) - point[1] * sin(angle), point[0] * sin(angle) + point[1] * cos(angle));
            };
        skArc(sketch, "fillet" ~ tooth ~ "_0", { "start" : rotate(mirror(onLand)), "mid" : rotate(mirror(filletMiddle)), "end" : rotate(mirror(onSide)) });
        if (hasFlanks)
        {
            skArc(sketch, "flank" ~ tooth ~ "_0", { "start" : rotate(mirror(onSide)), "mid" : rotate(mirror(flankMiddle)), "end" : rotate(mirror(onTip)) });
        }
        else if (hasWalls)
        {
            skLineSegment(sketch, "wall" ~ tooth ~ "_0", { "start" : rotate(mirror(onSide)), "end" : rotate(mirror(onTip)) });
        }
        // The groove's bottom
        if (hasFlat)
        {
            skArc(sketch, "groove" ~ tooth ~ "_0", { "start" : rotate(mirror(onTip)), "mid" : rotate(mirror(tipMiddle)), "end" : rotate(mirror(bottom)) });
            skLineSegment(sketch, "groove" ~ tooth ~ "_1", { "start" : rotate(mirror(bottom)), "end" : rotate(bottom) });
            skArc(sketch, "groove" ~ tooth ~ "_2", { "start" : rotate(bottom), "mid" : rotate(tipMiddle), "end" : rotate(onTip) });
        }
        else
        {
            skArc(sketch, "groove" ~ tooth, { "start" : rotate(mirror(onTip)), "mid" : rotate(bottom), "end" : rotate(onTip) });
        }
        if (hasFlanks)
        {
            skArc(sketch, "flank" ~ tooth ~ "_2", { "start" : rotate(onTip), "mid" : rotate(flankMiddle), "end" : rotate(onSide) });
        }
        else if (hasWalls)
        {
            skLineSegment(sketch, "wall" ~ tooth ~ "_2", { "start" : rotate(onTip), "end" : rotate(onSide) });
        }
        skArc(sketch, "fillet" ~ tooth ~ "_2", { "start" : rotate(onSide), "mid" : rotate(filletMiddle), "end" : rotate(onLand) });
        // The land to the next groove
        skArc(sketch, "land" ~ tooth, {
                    "start" : rotate(onLand),
                    "mid" : vector(cos(angle + toothAngle / 2), sin(angle + toothAngle / 2)) * (landRadius + profileOffset),
                    "end" : vector(cos(angle + toothAngle - filletAngle), sin(angle + toothAngle - filletAngle)) * (landRadius + profileOffset)
                });
    }
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}

/**
 * Where two circles cross: two points (one, twice, where they touch), or none.
 */
function circleIntersections(center1 is Vector, radius1 is ValueWithUnits, center2 is Vector, radius2 is ValueWithUnits) returns array
{
    const distance = norm(center2 - center1);
    if (tolerantEqualsZero(distance))
    {
        return [];
    }
    // Along the line between their centers, and off it
    const along = (distance ^ 2 + radius1 ^ 2 - radius2 ^ 2) / (2 * distance);
    const offSquared = radius1 ^ 2 - along ^ 2;
    if (offSquared < 0 * meter ^ 2)
    {
        return [];
    }
    const direction = (center2 - center1) / distance;
    const normal = vector(-direction[1], direction[0]);
    const base = center1 + direction * along;
    return [base + normal * sqrt(offSquared), base - normal * sqrt(offSquared)];
}
