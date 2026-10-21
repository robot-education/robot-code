FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/* Generated from quickFastenerTables.py by `fs gen` -- DO NOT EDIT */

export enum FastenerSize
{
    annotation { "Name" : "#6-32" }
    NO_6_32,
    annotation { "Name" : "#8-32" }
    NO_8_32,
    annotation { "Name" : "#10-32" }
    NO_10_32,
    annotation { "Name" : "1/4-20" }
    QUARTER_20,
    annotation { "Name" : "M3" }
    M3,
    annotation { "Name" : "M4" }
    M4
}

export enum ScrewHead
{
    annotation { "Name" : "Socket head" }
    SOCKET,
    annotation { "Name" : "Button head" }
    BUTTON,
    annotation { "Name" : "Flat head" }
    FLAT
}

export enum WasherType
{
    annotation { "Name" : "Flat" }
    FLAT,
    annotation { "Name" : "Wide" }
    WIDE
}

export enum NutType
{
    annotation { "Name" : "Nylock" }
    NYLOCK,
    annotation { "Name" : "Hex" }
    HEX
}

export enum SeatWasher
{
    annotation { "Name" : "None" }
    NONE,
    annotation { "Name" : "Flat washer" }
    FLAT,
    annotation { "Name" : "Wide washer" }
    WIDE
}

export const FASTENER_SIZES = {
            FastenerSize.NO_6_32 : { "name" : "#6-32", "metric" : false, "size" : "#6", "pitch" : "32 tpi", "majorDiameter" : 0.138 * inch, "tapDrillDiameter" : 0.1065 * inch, "closeClearanceDiameter" : 0.144 * inch, "freeClearanceDiameter" : 0.1495 * inch, "lengths" : [0.25 * inch, 0.375 * inch, 0.5 * inch, 0.625 * inch, 0.75 * inch, 1 * inch, 1.25 * inch, 1.5 * inch] },
            FastenerSize.NO_8_32 : { "name" : "#8-32", "metric" : false, "size" : "#8", "pitch" : "32 tpi", "majorDiameter" : 0.164 * inch, "tapDrillDiameter" : 0.136 * inch, "closeClearanceDiameter" : 0.1695 * inch, "freeClearanceDiameter" : 0.177 * inch, "lengths" : [0.25 * inch, 0.375 * inch, 0.5 * inch, 0.625 * inch, 0.75 * inch, 1 * inch, 1.25 * inch, 1.5 * inch, 1.75 * inch, 2 * inch] },
            FastenerSize.NO_10_32 : { "name" : "#10-32", "metric" : false, "size" : "#10", "pitch" : "32 tpi", "majorDiameter" : 0.19 * inch, "tapDrillDiameter" : 0.159 * inch, "closeClearanceDiameter" : 0.196 * inch, "freeClearanceDiameter" : 0.201 * inch, "lengths" : [0.25 * inch, 0.375 * inch, 0.5 * inch, 0.625 * inch, 0.75 * inch, 0.875 * inch, 1 * inch, 1.25 * inch, 1.5 * inch, 1.75 * inch, 2 * inch, 2.25 * inch, 2.5 * inch, 2.75 * inch, 3 * inch, 3.5 * inch, 4 * inch] },
            FastenerSize.QUARTER_20 : { "name" : "1/4-20", "metric" : false, "size" : "1/4", "pitch" : "20 tpi", "majorDiameter" : 0.25 * inch, "tapDrillDiameter" : 0.201 * inch, "closeClearanceDiameter" : 0.257 * inch, "freeClearanceDiameter" : 0.266 * inch, "lengths" : [0.375 * inch, 0.5 * inch, 0.625 * inch, 0.75 * inch, 0.875 * inch, 1 * inch, 1.25 * inch, 1.5 * inch, 1.75 * inch, 2 * inch, 2.25 * inch, 2.5 * inch, 2.75 * inch, 3 * inch, 3.5 * inch, 4 * inch] },
            FastenerSize.M3 : { "name" : "M3", "metric" : true, "size" : "M3", "pitch" : "0.5 mm", "majorDiameter" : 3 * millimeter, "tapDrillDiameter" : 2.5 * millimeter, "closeClearanceDiameter" : 3.2 * millimeter, "freeClearanceDiameter" : 3.4 * millimeter, "lengths" : [4 * millimeter, 5 * millimeter, 6 * millimeter, 8 * millimeter, 10 * millimeter, 12 * millimeter, 14 * millimeter, 16 * millimeter, 18 * millimeter, 20 * millimeter, 22 * millimeter, 25 * millimeter, 30 * millimeter, 35 * millimeter, 40 * millimeter] },
            FastenerSize.M4 : { "name" : "M4", "metric" : true, "size" : "M4", "pitch" : "0.7 mm", "majorDiameter" : 4 * millimeter, "tapDrillDiameter" : 3.3 * millimeter, "closeClearanceDiameter" : 4.3 * millimeter, "freeClearanceDiameter" : 4.5 * millimeter, "lengths" : [6 * millimeter, 8 * millimeter, 10 * millimeter, 12 * millimeter, 14 * millimeter, 16 * millimeter, 18 * millimeter, 20 * millimeter, 25 * millimeter, 30 * millimeter, 35 * millimeter, 40 * millimeter, 45 * millimeter, 50 * millimeter] }
        };

export const SCREW_HEADS = {
            FastenerSize.NO_6_32 : {
                ScrewHead.SOCKET : { "headDiameter" : 0.226 * inch, "headHeight" : 0.138 * inch, "socketSize" : 0.109375 * inch },
                ScrewHead.BUTTON : { "headDiameter" : 0.262 * inch, "headHeight" : 0.074 * inch, "socketSize" : 0.078125 * inch },
                ScrewHead.FLAT : { "headDiameter" : 0.307 * inch, "headHeight" : 0.097 * inch, "socketSize" : 0.078125 * inch, "countersinkAngle" : 82 * degree }
            },
            FastenerSize.NO_8_32 : {
                ScrewHead.SOCKET : { "headDiameter" : 0.27 * inch, "headHeight" : 0.164 * inch, "socketSize" : 0.140625 * inch },
                ScrewHead.BUTTON : { "headDiameter" : 0.312 * inch, "headHeight" : 0.087 * inch, "socketSize" : 0.09375 * inch },
                ScrewHead.FLAT : { "headDiameter" : 0.359 * inch, "headHeight" : 0.115 * inch, "socketSize" : 0.09375 * inch, "countersinkAngle" : 82 * degree }
            },
            FastenerSize.NO_10_32 : {
                ScrewHead.SOCKET : { "headDiameter" : 0.312 * inch, "headHeight" : 0.19 * inch, "socketSize" : 0.15625 * inch },
                ScrewHead.BUTTON : { "headDiameter" : 0.375 * inch, "headHeight" : 0.102 * inch, "socketSize" : 0.125 * inch },
                ScrewHead.FLAT : { "headDiameter" : 0.411 * inch, "headHeight" : 0.132 * inch, "socketSize" : 0.125 * inch, "countersinkAngle" : 82 * degree }
            },
            FastenerSize.QUARTER_20 : {
                ScrewHead.SOCKET : { "headDiameter" : 0.375 * inch, "headHeight" : 0.25 * inch, "socketSize" : 0.1875 * inch },
                ScrewHead.BUTTON : { "headDiameter" : 0.437 * inch, "headHeight" : 0.132 * inch, "socketSize" : 0.15625 * inch },
                ScrewHead.FLAT : { "headDiameter" : 0.531 * inch, "headHeight" : 0.153 * inch, "socketSize" : 0.15625 * inch, "countersinkAngle" : 82 * degree }
            },
            FastenerSize.M3 : {
                ScrewHead.SOCKET : { "headDiameter" : 5.5 * millimeter, "headHeight" : 3 * millimeter, "socketSize" : 2.5 * millimeter },
                ScrewHead.BUTTON : { "headDiameter" : 5.7 * millimeter, "headHeight" : 1.65 * millimeter, "socketSize" : 2 * millimeter },
                ScrewHead.FLAT : { "headDiameter" : 6.72 * millimeter, "headHeight" : 1.86 * millimeter, "socketSize" : 2 * millimeter, "countersinkAngle" : 90 * degree }
            },
            FastenerSize.M4 : {
                ScrewHead.SOCKET : { "headDiameter" : 7 * millimeter, "headHeight" : 4 * millimeter, "socketSize" : 3 * millimeter },
                ScrewHead.BUTTON : { "headDiameter" : 7.6 * millimeter, "headHeight" : 2.2 * millimeter, "socketSize" : 2.5 * millimeter },
                ScrewHead.FLAT : { "headDiameter" : 8.96 * millimeter, "headHeight" : 2.48 * millimeter, "socketSize" : 2.5 * millimeter, "countersinkAngle" : 90 * degree }
            }
        };

export const WASHERS = {
            FastenerSize.NO_6_32 : {
                WasherType.FLAT : { "innerDiameter" : 0.156 * inch, "outerDiameter" : 0.312 * inch, "thickness" : 0.032 * inch },
                WasherType.WIDE : { "innerDiameter" : 0.156 * inch, "outerDiameter" : 0.5 * inch, "thickness" : 0.04 * inch }
            },
            FastenerSize.NO_8_32 : {
                WasherType.FLAT : { "innerDiameter" : 0.188 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.049 * inch },
                WasherType.WIDE : { "innerDiameter" : 0.188 * inch, "outerDiameter" : 0.625 * inch, "thickness" : 0.04 * inch }
            },
            FastenerSize.NO_10_32 : {
                WasherType.FLAT : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.5 * inch, "thickness" : 0.049 * inch },
                WasherType.WIDE : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.75 * inch, "thickness" : 0.05 * inch }
            },
            FastenerSize.QUARTER_20 : {
                WasherType.FLAT : { "innerDiameter" : 0.281 * inch, "outerDiameter" : 0.625 * inch, "thickness" : 0.065 * inch },
                WasherType.WIDE : { "innerDiameter" : 0.281 * inch, "outerDiameter" : 1 * inch, "thickness" : 0.063 * inch }
            },
            FastenerSize.M3 : {
                WasherType.FLAT : { "innerDiameter" : 3.2 * millimeter, "outerDiameter" : 7 * millimeter, "thickness" : 0.5 * millimeter },
                WasherType.WIDE : { "innerDiameter" : 3.2 * millimeter, "outerDiameter" : 9 * millimeter, "thickness" : 0.8 * millimeter }
            },
            FastenerSize.M4 : {
                WasherType.FLAT : { "innerDiameter" : 4.3 * millimeter, "outerDiameter" : 9 * millimeter, "thickness" : 0.8 * millimeter },
                WasherType.WIDE : { "innerDiameter" : 4.3 * millimeter, "outerDiameter" : 12 * millimeter, "thickness" : 1 * millimeter }
            }
        };

export const NUTS = {
            FastenerSize.NO_6_32 : {
                NutType.NYLOCK : { "acrossFlats" : 0.3125 * inch, "height" : 0.172 * inch },
                NutType.HEX : { "acrossFlats" : 0.3125 * inch, "height" : 0.109 * inch }
            },
            FastenerSize.NO_8_32 : {
                NutType.NYLOCK : { "acrossFlats" : 0.34375 * inch, "height" : 0.203 * inch },
                NutType.HEX : { "acrossFlats" : 0.34375 * inch, "height" : 0.125 * inch }
            },
            FastenerSize.NO_10_32 : {
                NutType.NYLOCK : { "acrossFlats" : 0.375 * inch, "height" : 0.236 * inch },
                NutType.HEX : { "acrossFlats" : 0.375 * inch, "height" : 0.125 * inch }
            },
            FastenerSize.QUARTER_20 : {
                NutType.NYLOCK : { "acrossFlats" : 0.4375 * inch, "height" : 0.312 * inch },
                NutType.HEX : { "acrossFlats" : 0.4375 * inch, "height" : 0.219 * inch }
            },
            FastenerSize.M3 : {
                NutType.NYLOCK : { "acrossFlats" : 5.5 * millimeter, "height" : 4 * millimeter },
                NutType.HEX : { "acrossFlats" : 5.5 * millimeter, "height" : 2.4 * millimeter }
            },
            FastenerSize.M4 : {
                NutType.NYLOCK : { "acrossFlats" : 7 * millimeter, "height" : 5 * millimeter },
                NutType.HEX : { "acrossFlats" : 7 * millimeter, "height" : 3.2 * millimeter }
            }
        };
