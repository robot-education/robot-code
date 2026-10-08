FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");


export import(path : "6e24956e9977116c79280620", version : "4deaa9512490955da06665c4");

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
    _3_8_IN,
    annotation { "Name" : "7 mm (8mm REX)" }
    _7_MM,
    annotation { "Name" : "11 mm (12mm REX)" }
    _11_MM
}

/**
 * Whether a hex size is metric, like goBILDA's REX (see `getRoundedDiameter`).
 */
export predicate isMetricHex(hexSize is HexSize)
{
    hexSize == HexSize._7_MM || hexSize == HexSize._11_MM;
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
    return switch (getHexSize(definition)) {
                HexSize._1_2_IN : 0.5 * inch,
                HexSize._3_8_IN : 0.375 * inch,
                HexSize._7_MM : 7 * millimeter,
                HexSize._11_MM : 11 * millimeter
            };
}

/**
 * The diameter of the round a rounded hex of a size is cut to: 13.75 mm for 1/2 in. (WCP, REV, VEX ThunderHex, ...),
 * 10.25 mm for 3/8 in., and 8 mm and 12 mm for goBILDA's 8mm and 12mm REX.
 */
export function getRoundedDiameter(hexSize is HexSize) returns ValueWithUnits
{
    return switch (hexSize) {
                HexSize._1_2_IN : 13.75 * millimeter,
                HexSize._3_8_IN : 10.25 * millimeter,
                HexSize._7_MM : 8 * millimeter,
                HexSize._11_MM : 12 * millimeter
            };
}

/**
 * Given the nominal width of a hex across the flats, returns the radius of a circle which circumscribes the hex.
 */
export function getHexCircleRadius(flatWidth is ValueWithUnits) returns ValueWithUnits
{
    return (flatWidth / 2) / cos(30 * degree);
}
