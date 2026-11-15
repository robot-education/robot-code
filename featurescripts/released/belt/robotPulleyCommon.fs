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
 * Sketches the toothed profile of a pulley for a belt of `beltType` with `teeth` teeth, centered on `plane`, and returns
 * its face. Each groove is the belt's tooth (see `getBeltModelInfo`): its tip, an arc of `toothRadius` around a center
 * `toothOffset` inside the pitch circle, and for a GT2 belt, its flanks, arcs of `flankRadius` around centers on the
 * lands' circle across the groove, which the tip's arc is inside, tangent. Between the grooves are lands at the belt's
 * inside (`insideThickness` inside the pitch circle), their corners rounded by the belt's tooth fillet.
 *
 * @param profileOffset : How far the profile is offset outward (inward, if negative): to make the pulley a little
 *          bigger or smaller, for a tighter or looser belt. Every arc keeps its center: convex arcs grow, and concave
 *          ones shrink.
 */
export function sketchPulleyProfile(context is Context, id is Id, plane is Plane, beltType is BeltType, teeth is number,
    profileOffset is ValueWithUnits) returns Query
{
    const belt = getBeltModelInfo(beltType);
    const pitchRadius = getPulleyRadius(getBeltPitch(beltType), teeth);
    const toothAngle = 360 * degree / teeth;
    const origin = zeroVector(2) * meter;
    const tooFew = "A " ~ getBeltTypeName(beltType) ~ " pulley needs more teeth than " ~ teeth ~ ".";

    // The first groove's, on the x axis: the rest are rotated copies. Its counter clockwise side's (+y) curves are
    // found, and its clockwise side's are their mirror images.
    const landRadius = pitchRadius - belt.insideThickness;
    const tipCenter = vector(pitchRadius - belt.toothOffset, 0 * meter);
    const tipRadius = belt.toothRadius - profileOffset;
    const filletRadius = belt.toothFilletRadius + profileOffset;
    // The groove's side: a GT2 belt's flank, or the tip's arc itself
    var sideCenter = tipCenter;
    var sideRadius = tipRadius;
    const hasFlanks = belt.flankRadius != undefined;
    if (hasFlanks)
    {
        // On the lands' circle, across the groove, as far from the tip's center as the tip's arc is inside the flank
        const centers = circleIntersections(origin, landRadius, tipCenter, belt.flankRadius - belt.toothRadius);
        if (centers == [])
        {
            throw regenError(tooFew, ["pulleyTeeth"]);
        }
        sideCenter = centers[0][1] < centers[1][1] ? centers[0] : centers[1];
        sideRadius = belt.flankRadius - profileOffset;
    }
    if (tipRadius <= 0 * meter || sideRadius <= 0 * meter || filletRadius <= 0 * meter)
    {
        throw regenError("The profile offset is too large for this belt's teeth.", ["profileOffsetDistance"]);
    }

    // The fillet's center is inside the lands' circle and outside the side's, tangent to both (the same, offset or not)
    const filletCenters = circleIntersections(origin, landRadius - belt.toothFilletRadius, sideCenter, sideRadius + filletRadius);
    if (filletCenters == [])
    {
        throw regenError(tooFew, ["pulleyTeeth"]);
    }
    const filletCenter = filletCenters[0][1] > filletCenters[1][1] ? filletCenters[0] : filletCenters[1];
    const filletAngle = atan2(filletCenter[1], filletCenter[0]);
    const onLand = filletCenter / norm(filletCenter) * (landRadius + profileOffset);
    const onSide = sideCenter + normalize(filletCenter - sideCenter) * sideRadius;
    // Where a flank meets the tip: on the line from the flank's center through the tip's
    const onTip = hasFlanks ? sideCenter + normalize(tipCenter - sideCenter) * sideRadius : onSide;
    // A groove's fillets can't reach past the middle of its lands, and its flanks must go out from its tip
    if (filletAngle >= toothAngle / 2 || onTip[1] <= 0 * meter || (hasFlanks && norm(onSide) <= norm(onTip)))
    {
        throw regenError(tooFew, ["pulleyTeeth"]);
    }
    const middle = function(center is Vector, radius is ValueWithUnits, start is Vector, end is Vector) returns Vector
        {
            return center + normalize(normalize(start - center) + normalize(end - center)) * radius;
        };
    const filletMiddle = middle(filletCenter, filletRadius, onLand, onSide);
    const flankMiddle = middle(sideCenter, sideRadius, onTip, onSide);
    const bottom = tipCenter - vector(tipRadius, 0 * meter);
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
        // The groove's bottom: the tooth's tip
        skArc(sketch, "groove" ~ tooth, { "start" : rotate(mirror(onTip)), "mid" : rotate(bottom), "end" : rotate(onTip) });
        if (hasFlanks)
        {
            skArc(sketch, "flank" ~ tooth ~ "_2", { "start" : rotate(onTip), "mid" : rotate(flankMiddle), "end" : rotate(onSide) });
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
