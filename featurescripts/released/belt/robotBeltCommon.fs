FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "20659432897c109a97ad647f", version : "f6a71215a0d0b909e1c19f06");
export import(path : "d82c5bf9082d0054f8f0b419", version : "85f89bc09b57a2add3d22385");

/**
 * What a belt's body is modeled as (see `extrudeBelt`).
 */
export type BeltModel typecheck canBeBeltModel;

export predicate canBeBeltModel(value)
{
    value is map;
    value.beltType is BeltType;
    value.beltTeeth is number;
    value.beltWidth is ValueWithUnits;
    value.modelBeltTeeth is boolean;
    value.isDoubleSidedBelt is boolean;
    value.outsideThickness is ValueWithUnits;
}

/**
 * Set on each of a belt's curved faces around a pulley or idler, and on its mate connectors, as a `BeltFaceAttribute`:
 * Robot pulley makes pulleys on them.
 */
export const BELT_PULLEY_FACE_ATTRIBUTE = "robotBeltPulleyFace";

export type BeltFaceAttribute typecheck canBeBeltFaceAttribute;

export predicate canBeBeltFaceAttribute(value)
{
    value is map;
    value.beltType is BeltType;
    value.beltWidth is ValueWithUnits;

    value.pulleyType is PulleyType;
    if (isPulley(value.pulleyType))
    {
        value.pulleyTeeth is number;
    }
    else
    {
        value.idlerRadius is ValueWithUnits;
    }
}

/**
 * Whether the belt is a simple two-pulley belt, a complex belt with multiple pulleys and idlers, or an open belt with
 * ends (clamped, like a linear slide's).
 */
export enum BeltMode
{
    annotation { "Name" : "Simple" }
    SIMPLE,
    annotation { "Name" : "Complex" }
    COMPLEX,
    annotation { "Name" : "Open" }
    OPEN
}

export enum PulleyType
{
    INSIDE_PULLEY,
    OUTSIDE_PULLEY,
    IDLER
}

export predicate isPulley(pulleyType is PulleyType)
{
    pulleyType == PulleyType.INSIDE_PULLEY || pulleyType == PulleyType.OUTSIDE_PULLEY;
}

export predicate isIdler(pulleyType is PulleyType)
{
    pulleyType == PulleyType.IDLER;
}

export predicate isOutside(pulleyType is PulleyType)
{
    pulleyType == PulleyType.OUTSIDE_PULLEY || pulleyType == PulleyType.IDLER;
}


export predicate beltTypePredicate(definition is map)
{
    annotation { "Name" : "Belt type table", "Lookup Table" : beltTypeTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
    definition.beltTypePath is LookupTablePath;
}

export function getBeltPitch(beltType is BeltType) returns ValueWithUnits
{
    return switch (beltType) {
                BeltType._2_MM_GT2 : 2 * millimeter,
                BeltType._3_MM_GT2 : 3 * millimeter,
                BeltType._3_MM_HTD : 3 * millimeter,
                BeltType._5_MM_HTD : 5 * millimeter,
                BeltType.RT25 : 0.25 * inch
            };
}

export function getBeltTypeName(beltType is BeltType) returns string
{
    return switch (beltType) {
                BeltType._2_MM_GT2 : "2mm GT2",
                BeltType._3_MM_GT2 : "3mm GT2",
                BeltType._3_MM_HTD : "3mm HTD",
                BeltType._5_MM_HTD : "5mm HTD",
                BeltType.RT25 : "RT25"
            };
}

/**
 * The thickness of a belt's back, outside its pitch line: an idler's path is this much bigger than it. A belt table's
 * value's `backThickness` (a rubber coated belt's), or its type's.
 */
export function getBeltOutsideThickness(beltValue is map) returns ValueWithUnits
{
    return beltValue.backThickness ?? getBeltModelInfo(beltValue.beltType).outsideThickness;
}

/**
 * Returns the thickness of the inside of the belt, as measured from the pitch circle when modeling the belt.
 */
export function getBeltModelInsideThickness(beltInfo is map, modelBeltTeeth is boolean)
{
    return modelBeltTeeth ? beltInfo.insideThickness : beltInfo.toothOffset + beltInfo.toothRadius;
}

/**
 * Returns information used specifically for modeling a belt.
 *
 * @returns {{
 *      @field outsideThickness : The thickness of the back of the belt measured from the pitch circle.
 *              Does not apply to double sided belts, which simply use the insideThickness again.
 *      @field insideThickness : The thickness of the inside of the belt measured from the pitch circle.
 *      @field toothOffset : The distance from the center of each tooth to the pitch circle.
 *      @field toothRadius : The radius of each tooth.
 *      @field toothFilletRadius : The radius of the fillet applied to the teeth.
 *      @field flankRadius : @optional A GT2 belt's teeth's flanks' radius: they're arcs around centers on the belt's
 *              inside, across the tooth, which its tip's arc is inside, tangent. Gates' GT2 profiles (R2, with the tip's
 *              R3, the fillet's R1, and the tooth's height h). Other belts' teeth are all one arc, their tip's.
 *              Robot belt models teeth as their tip's arc alone (a circle), which fits in the groove.
 * }}
 */
export function getBeltModelInfo(beltType is BeltType)
{
    if (beltType == BeltType._2_MM_GT2)
    {
        // h 0.75, R1 0.15, R2 1.00, R3 0.555, and its pitch line 0.254 outside its inside
        return {
                "outsideThickness" : (0.63 - 0.254) * millimeter,
                "insideThickness" : 0.254 * millimeter,
                "toothOffset" : (0.254 + 0.75 - 0.555) * millimeter,
                "toothRadius" : 0.555 * millimeter,
                "toothFilletRadius" : 0.15 * millimeter,
                "flankRadius" : 1 * millimeter
            };
    }
    else if (beltType == BeltType._3_MM_GT2)
    {
        // h 1.14, R1 0.25, R2 1.52, R3 0.85, and its pitch line 0.381 outside its inside
        return {
                "outsideThickness" : 0.81 * millimeter,
                "insideThickness" : 0.381 * millimeter,
                "toothOffset" : (0.381 + 1.14 - 0.85) * millimeter,
                "toothRadius" : 0.85 * millimeter,
                "toothFilletRadius" : 0.25 * millimeter,
                "flankRadius" : 1.52 * millimeter
            };
    }
    else if (beltType == BeltType._3_MM_HTD)
    {
        // 5mm HTD's, scaled by the pitch: HTD teeth are the same shape at every pitch, and this gives the standard
        // 0.381mm pitch line differential
        return {
                "outsideThickness" : 1.16 * 0.6 * millimeter,
                "insideThickness" : 0.381 * millimeter,
                "toothOffset" : (0.5715 + 2.06 - 1.49) * 0.6 * millimeter,
                "toothRadius" : 1.49 * 0.6 * millimeter,
                "toothFilletRadius" : 0.42 * 0.6 * millimeter,
            };
    }
    else if (beltType == BeltType._5_MM_HTD)
    {
        // Its teeth 2.06 mm tall, and its pitch line 0.5715 mm outside its inside
        return {
                "outsideThickness" : 1.16 * millimeter,
                "insideThickness" : 0.5715 * millimeter,
                "toothOffset" : (0.5715 + 2.06 - 1.49) * millimeter,
                "toothRadius" : 1.49 * millimeter,
                "toothFilletRadius" : 0.42 * millimeter,
            };
    }
    else if (beltType == BeltType.RT25)
    {
        return {
                "outsideThickness" : 0.7112 * millimeter,
                "insideThickness" : 0.5588 * millimeter,
                "toothOffset" : 1.09 * millimeter,
                "toothRadius" : 1.8 * millimeter,
                "toothFilletRadius" : 0.529082 * millimeter,
            };
    }
}
