FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");


export import(path : "6e24956e9977116c79280620", version : "1cdcfd6334c53e51fef6f5f5");

export enum ShaftType
{
    annotation { "Name" : "Hex" }
    HEX,
    annotation { "Name" : "Spline" }
    SPLINE
}

export const SHAFT_ATTRIBUTE = "robotShaftAttribute";

export type ShaftAttribute typecheck canBeShaftAttribute;

export predicate canBeShaftAttribute(value)
{
    value is map;
    value.shaftType is ShaftType;

    if (value.shaftType == ShaftType.HEX)
    {
        value.hexType is HexType;
        value.hexSize is HexSize;
    }
    else
    {
        value.splineType is SplineType;
    }
}

export enum HexType
{
    annotation { "Name" : "Stock" }
    STOCK,
    annotation { "Name" : "Rounded hex" }
    ROUNDED_HEX,
    annotation { "Name" : "UltraHex" }
    ULTRA_HEX,
    annotation { "Name" : "Churro" }
    CHURRO,
    annotation { "Name" : "Hex Lite" }
    HEX_LITE
}

export enum HexSize
{
    annotation { "Name" : "1/2 in." }
    _1_2_IN,
    annotation { "Name" : "3/8 in." }
    _3_8_IN
}

export function getHexSize(definition is map) returns HexSize
{
    if (definition.hexType == HexType.ULTRA_HEX)
    {
        return HexSize._1_2_IN;
    }
    return definition.hexSize;
}

/**
 * Returns the nominal width of a hex shaft.
 */
export function getHexWidth(definition is map) returns ValueWithUnits
{
    return getHexSize(definition) == HexSize._1_2_IN ? 0.5 * inch : 0.375 * inch;
}

/**
 * Given the nominal width of a hex across the flats, returns the radius of a circle which circumscribes the hex.
 */
export function getHexCircleRadius(flatWidth is ValueWithUnits) returns ValueWithUnits
{
    return (flatWidth / 2) / cos(30 * degree);
}