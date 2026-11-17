FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "20659432897c109a97ad647f", version : "4322ada0b7998fa395666667");
export import(path : "d82c5bf9082d0054f8f0b419", version : "512461979234ba53a425b7f4");
export import(path : "3651d7ff6d8577f322b85723", version : "e98af2e09fb061040ac8dc07");

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
 * Whether the belt is a simple two-pulley belt, or a complex belt with multiple pulleys and idlers (a loop, or an open
 * belt with ends, clamped like a linear slide's).
 */
export enum BeltMode
{
    annotation { "Name" : "Simple" }
    SIMPLE,
    annotation { "Name" : "Complex" }
    COMPLEX
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


/**
 * A belt's type and width, without a supplier (an open belt's, cut to length, or a pulley's): FRC's or FTC's.
 */
export predicate beltTypePredicate(definition is map)
{
    if (isFrc(definition))
    {
        annotation { "Name" : "FRC belt type", "Lookup Table" : frcBeltTypeTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.frcBeltTypePath is LookupTablePath;
    }
    else
    {
        annotation { "Name" : "FTC belt type", "Lookup Table" : ftcBeltTypeTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.ftcBeltTypePath is LookupTablePath;
    }
}

/**
 * The lookup table a belt's type and width are chosen from (see `beltTypePredicate`), and its path in it, and its
 * Belt width if its width is Custom (see `getBeltValue`).
 */
export function getBeltTypeTableAndPath(definition is map) returns map
{
    return {
            "table" : isFrc(definition) ? frcBeltTypeTable : ftcBeltTypeTable,
            "path" : isFrc(definition) ? definition.frcBeltTypePath : definition.ftcBeltTypePath,
            "customWidth" : definition.customBeltWidth
        };
}

/**
 * The belt type chosen (see `beltTypePredicate`). Used by Robot pulley.
 *
 * @returns {{
 *      @field beltType {BeltType} :
 *      @field beltWidth {ValueWithUnits} :
 * }}
 */
export function getBeltTypeValue(definition is map) returns map
{
    return getBeltValue(getBeltTypeTableAndPath(definition));
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
 * Returns information used specifically for modeling a belt. Teeth are modeled as their tips' arcs (circles): a GT2
 * tooth's flanks are left out. Robot pulley's grooves are its own (`getPulleyGroove`), which the modeled teeth fit in.
 *
 * @returns {{
 *      @field outsideThickness : The thickness of the back of the belt measured from the pitch circle.
 *              Does not apply to double sided belts, which simply use the insideThickness again.
 *      @field insideThickness : The thickness of the inside of the belt measured from the pitch circle.
 *      @field toothOffset : The distance from the center of each tooth to the pitch circle.
 *      @field toothRadius : The radius of each tooth.
 *      @field toothFilletRadius : The radius of the fillet applied to the teeth.
 * }}
 */
export function getBeltModelInfo(beltType is BeltType)
{
    if (beltType == BeltType._2_MM_GT2)
    {
        // Gates' 2mm GT2: 0.75 mm tall, R3 0.555 mm, R1 0.15 mm, and its pitch line 0.254 mm outside its inside
        return {
                "outsideThickness" : (0.63 - 0.254) * millimeter,
                "insideThickness" : 0.254 * millimeter,
                "toothOffset" : (0.254 + 0.75 - 0.555) * millimeter,
                "toothRadius" : 0.555 * millimeter,
                "toothFilletRadius" : 0.15 * millimeter
            };
    }
    else if (beltType == BeltType._3_MM_GT2)
    {
        // Gates' 3mm GT2: 1.14 mm tall, R3 0.85 mm, R1 0.25 mm, and its pitch line 0.381 mm outside its inside
        return {
                "outsideThickness" : 0.81 * millimeter,
                "insideThickness" : 0.381 * millimeter,
                "toothOffset" : (0.381 + 1.14 - 0.85) * millimeter,
                "toothRadius" : 0.85 * millimeter,
                "toothFilletRadius" : 0.25 * millimeter
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
