FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
export import(path : "onshape/std/persistentCoordSystem.fs", version : "2960.0");

import(path : "00b10ef1fb1a7418097fc0af", version : "3ba879cf97235b1a292f0dbc");
import(path : "948c83c1b1ac83de4ccf921b", version : "4aff58a1ab26d9f7aa7abfbb");

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
 * its face. Each groove is the belt's tooth (as Robot belt models it: a circle `toothOffset` inside the pitch circle,
 * of `toothRadius`), between lands at the belt's inside (`insideThickness` inside the pitch circle), with the lands'
 * corners rounded by the belt's tooth fillet, so the belt's modeled teeth sit in the grooves.
 *
 * @param profileOffset : How far the profile is offset outward (inward, if negative): to make the pulley a little
 *          bigger or smaller, for a tighter or looser belt.
 */
export function sketchPulleyProfile(context is Context, id is Id, plane is Plane, beltType is BeltType, teeth is number,
    profileOffset is ValueWithUnits) returns Query
{
    const belt = getBeltModelInfo(beltType);
    const pitchRadius = getPulleyRadius(getBeltPitch(beltType), teeth);
    // The lands' circle, and each groove's and fillet's, offset: convex arcs grow, and concave ones shrink
    const landRadius = pitchRadius - belt.insideThickness + profileOffset;
    const grooveCenterRadius = pitchRadius - belt.toothOffset;
    const grooveRadius = belt.toothRadius - profileOffset;
    const filletRadius = belt.toothFilletRadius + profileOffset;
    if (grooveRadius <= 0 * meter || filletRadius <= 0 * meter)
    {
        throw regenError("The profile offset is too large for this belt's teeth.", ["profileOffsetDistance"]);
    }

    // Each fillet's center is tangent inside the lands' circle and outside the groove: the angle between it and the
    // groove's center, about the pulley's
    const toFillet = landRadius - filletRadius;
    const cosine = (grooveCenterRadius ^ 2 + toFillet ^ 2 - (grooveRadius + filletRadius) ^ 2) / (2 * grooveCenterRadius * toFillet);
    const toothAngle = 360 * degree / teeth;
    if (abs(cosine) > 1 || acos(cosine) >= toothAngle / 2)
    {
        throw regenError("A " ~ getBeltTypeName(beltType) ~ " pulley needs more teeth than " ~ teeth ~ ".", ["pulleyTeeth"]);
    }
    const filletAngle = acos(cosine);

    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    const at = function(radius is ValueWithUnits, angle is ValueWithUnits) returns Vector
        {
            return vector(cos(angle), sin(angle)) * radius;
        };
    for (var tooth = 0; tooth < teeth; tooth += 1)
    {
        const angle = tooth * toothAngle;
        const grooveCenter = at(grooveCenterRadius, angle);
        var landEnds = [];
        var grooveEnds = [];
        for (var side in [-1, 1])
        {
            const filletCenter = at(toFillet, angle + side * filletAngle);
            const onLand = at(landRadius, angle + side * filletAngle);
            const onGroove = grooveCenter + normalize(filletCenter - grooveCenter) * grooveRadius;
            const middle = filletCenter + normalize(normalize(onLand - filletCenter) + normalize(onGroove - filletCenter)) * filletRadius;
            skArc(sketch, "fillet" ~ tooth ~ "_" ~ (side + 1), { "start" : onLand, "mid" : middle, "end" : onGroove });
            landEnds = append(landEnds, onLand);
            grooveEnds = append(grooveEnds, onGroove);
        }
        // The groove, through its bottom
        skArc(sketch, "groove" ~ tooth, {
                    "start" : grooveEnds[0],
                    "mid" : grooveCenter - at(grooveRadius, angle),
                    "end" : grooveEnds[1]
                });
        // The land to the next groove
        skArc(sketch, "land" ~ tooth, {
                    "start" : landEnds[1],
                    "mid" : at(landRadius, angle + toothAngle / 2),
                    "end" : at(landRadius, angle + toothAngle - filletAngle)
                });
    }
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}
