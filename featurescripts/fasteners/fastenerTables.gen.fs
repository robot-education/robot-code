FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/* Generated from fastenerTables.py by `fs gen` -- DO NOT EDIT */

export enum FastenerVendor
{
    annotation { "Name" : "Generic" }
    GENERIC,
    annotation { "Name" : "McMaster-Carr" }
    MCMASTER,
    annotation { "Name" : "REV" }
    REV,
    annotation { "Name" : "WCP" }
    WCP,
    annotation { "Name" : "AndyMark" }
    ANDYMARK
}

export enum FastenerSize
{
    annotation { "Name" : "#4-40" }
    NO_4_40,
    annotation { "Name" : "#6-32" }
    NO_6_32,
    annotation { "Name" : "#8-32" }
    NO_8_32,
    annotation { "Name" : "#10-32" }
    NO_10_32,
    annotation { "Name" : "1/4-20" }
    QUARTER_20
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

export enum ScrewLength
{
    annotation { "Name" : "3/32 in." }
    IN_0_09375,
    annotation { "Name" : "1/8 in." }
    IN_0_125,
    annotation { "Name" : "5/32 in." }
    IN_0_15625,
    annotation { "Name" : "3/16 in." }
    IN_0_1875,
    annotation { "Name" : "1/4 in." }
    IN_0_25,
    annotation { "Name" : "5/16 in." }
    IN_0_3125,
    annotation { "Name" : "3/8 in." }
    IN_0_375,
    annotation { "Name" : "7/16 in." }
    IN_0_4375,
    annotation { "Name" : "1/2 in." }
    IN_0_5,
    annotation { "Name" : "9/16 in." }
    IN_0_5625,
    annotation { "Name" : "5/8 in." }
    IN_0_625,
    annotation { "Name" : "11/16 in." }
    IN_0_6875,
    annotation { "Name" : "3/4 in." }
    IN_0_75,
    annotation { "Name" : "7/8 in." }
    IN_0_875,
    annotation { "Name" : "1 in." }
    IN_1,
    annotation { "Name" : "1-1/8 in." }
    IN_1_125,
    annotation { "Name" : "1-1/4 in." }
    IN_1_25,
    annotation { "Name" : "1-3/8 in." }
    IN_1_375,
    annotation { "Name" : "1-1/2 in." }
    IN_1_5,
    annotation { "Name" : "1-5/8 in." }
    IN_1_625,
    annotation { "Name" : "1-3/4 in." }
    IN_1_75,
    annotation { "Name" : "1-7/8 in." }
    IN_1_875,
    annotation { "Name" : "2 in." }
    IN_2,
    annotation { "Name" : "2-1/8 in." }
    IN_2_125,
    annotation { "Name" : "2-1/4 in." }
    IN_2_25,
    annotation { "Name" : "2-3/8 in." }
    IN_2_375,
    annotation { "Name" : "2-1/2 in." }
    IN_2_5,
    annotation { "Name" : "2-5/8 in." }
    IN_2_625,
    annotation { "Name" : "2-3/4 in." }
    IN_2_75,
    annotation { "Name" : "3 in." }
    IN_3,
    annotation { "Name" : "3-1/4 in." }
    IN_3_25,
    annotation { "Name" : "3-1/2 in." }
    IN_3_5,
    annotation { "Name" : "3-3/4 in." }
    IN_3_75,
    annotation { "Name" : "4 in." }
    IN_4,
    annotation { "Name" : "4-1/4 in." }
    IN_4_25,
    annotation { "Name" : "4-1/2 in." }
    IN_4_5,
    annotation { "Name" : "4-3/4 in." }
    IN_4_75,
    annotation { "Name" : "5 in." }
    IN_5,
    annotation { "Name" : "5-1/4 in." }
    IN_5_25,
    annotation { "Name" : "5-1/2 in." }
    IN_5_5,
    annotation { "Name" : "5-3/4 in." }
    IN_5_75,
    annotation { "Name" : "6 in." }
    IN_6,
    annotation { "Name" : "6-1/4 in." }
    IN_6_25,
    annotation { "Name" : "6-1/2 in." }
    IN_6_5,
    annotation { "Name" : "7 in." }
    IN_7,
    annotation { "Name" : "7-1/2 in." }
    IN_7_5,
    annotation { "Name" : "8 in." }
    IN_8,
    annotation { "Name" : "8-1/2 in." }
    IN_8_5,
    annotation { "Name" : "9 in." }
    IN_9,
    annotation { "Name" : "10 in." }
    IN_10
}

export enum NutType
{
    annotation { "Name" : "Nylock" }
    NYLOCK,
    annotation { "Name" : "Thin nylock" }
    THIN_NYLOCK,
    annotation { "Name" : "Hex" }
    HEX,
    annotation { "Name" : "Thin hex" }
    THIN_HEX,
    annotation { "Name" : "Square" }
    SQUARE
}

export enum WasherType
{
    annotation { "Name" : "5/16 in. OD x 0.03 in." }
    OD_5_16_X_0_03,
    annotation { "Name" : "5/16 in. OD x 0.032 in." }
    OD_5_16_X_0_032,
    annotation { "Name" : "3/8 in. OD x 0.012 in." }
    OD_3_8_X_0_012,
    annotation { "Name" : "3/8 in. OD x 0.03 in." }
    OD_3_8_X_0_03,
    annotation { "Name" : "3/8 in. OD x 0.031 in." }
    OD_3_8_X_0_031,
    annotation { "Name" : "3/8 in. OD x 0.049 in." }
    OD_3_8_X_0_049,
    annotation { "Name" : "7/16 in. OD x 0.03 in." }
    OD_7_16_X_0_03,
    annotation { "Name" : "7/16 in. OD x 0.049 in." }
    OD_7_16_X_0_049,
    annotation { "Name" : "1/2 in. OD x 0.04 in." }
    OD_1_2_X_0_04,
    annotation { "Name" : "1/2 in. OD x 0.049 in." }
    OD_1_2_X_0_049,
    annotation { "Name" : "5/8 in. OD x 0.031 in." }
    OD_5_8_X_0_031,
    annotation { "Name" : "5/8 in. OD x 0.05 in." }
    OD_5_8_X_0_05,
    annotation { "Name" : "5/8 in. OD x 0.065 in." }
    OD_5_8_X_0_065
}

export const FASTENER_SIZES = {
            FastenerSize.NO_4_40 : { "name" : "#4-40", "size" : "#4", "pitch" : "40 tpi", "majorDiameter" : 0.112 * inch, "tapDrillDiameter" : 0.089 * inch, "closeClearanceDiameter" : 0.116 * inch, "freeClearanceDiameter" : 0.1285 * inch },
            FastenerSize.NO_6_32 : { "name" : "#6-32", "size" : "#6", "pitch" : "32 tpi", "majorDiameter" : 0.138 * inch, "tapDrillDiameter" : 0.1065 * inch, "closeClearanceDiameter" : 0.144 * inch, "freeClearanceDiameter" : 0.1495 * inch },
            FastenerSize.NO_8_32 : { "name" : "#8-32", "size" : "#8", "pitch" : "32 tpi", "majorDiameter" : 0.164 * inch, "tapDrillDiameter" : 0.136 * inch, "closeClearanceDiameter" : 0.1695 * inch, "freeClearanceDiameter" : 0.177 * inch },
            FastenerSize.NO_10_32 : { "name" : "#10-32", "size" : "#10", "pitch" : "32 tpi", "majorDiameter" : 0.19 * inch, "tapDrillDiameter" : 0.159 * inch, "closeClearanceDiameter" : 0.196 * inch, "freeClearanceDiameter" : 0.201 * inch },
            FastenerSize.QUARTER_20 : { "name" : "1/4-20", "size" : "1/4", "pitch" : "20 tpi", "majorDiameter" : 0.25 * inch, "tapDrillDiameter" : 0.201 * inch, "closeClearanceDiameter" : 0.257 * inch, "freeClearanceDiameter" : 0.266 * inch }
        };

export const SCREW_HEADS = {
            FastenerSize.NO_4_40 : {
                ScrewHead.SOCKET : { "headDiameter" : 0.183 * inch, "headHeight" : 0.112 * inch, "socketSize" : 0.09375 * inch },
                ScrewHead.BUTTON : { "headDiameter" : 0.213 * inch, "headHeight" : 0.059 * inch, "socketSize" : 0.078125 * inch },
                ScrewHead.FLAT : { "headDiameter" : 0.255 * inch, "headHeight" : 0.083 * inch, "socketSize" : 0.0625 * inch, "countersinkAngle" : 82 * degree }
            },
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
            }
        };

export const SCREW_LENGTHS = {
            ScrewLength.IN_0_09375 : { "length" : 0.09375 * inch, "name" : "3/32 in." },
            ScrewLength.IN_0_125 : { "length" : 0.125 * inch, "name" : "1/8 in." },
            ScrewLength.IN_0_15625 : { "length" : 0.15625 * inch, "name" : "5/32 in." },
            ScrewLength.IN_0_1875 : { "length" : 0.1875 * inch, "name" : "3/16 in." },
            ScrewLength.IN_0_25 : { "length" : 0.25 * inch, "name" : "1/4 in." },
            ScrewLength.IN_0_3125 : { "length" : 0.3125 * inch, "name" : "5/16 in." },
            ScrewLength.IN_0_375 : { "length" : 0.375 * inch, "name" : "3/8 in." },
            ScrewLength.IN_0_4375 : { "length" : 0.4375 * inch, "name" : "7/16 in." },
            ScrewLength.IN_0_5 : { "length" : 0.5 * inch, "name" : "1/2 in." },
            ScrewLength.IN_0_5625 : { "length" : 0.5625 * inch, "name" : "9/16 in." },
            ScrewLength.IN_0_625 : { "length" : 0.625 * inch, "name" : "5/8 in." },
            ScrewLength.IN_0_6875 : { "length" : 0.6875 * inch, "name" : "11/16 in." },
            ScrewLength.IN_0_75 : { "length" : 0.75 * inch, "name" : "3/4 in." },
            ScrewLength.IN_0_875 : { "length" : 0.875 * inch, "name" : "7/8 in." },
            ScrewLength.IN_1 : { "length" : 1 * inch, "name" : "1 in." },
            ScrewLength.IN_1_125 : { "length" : 1.125 * inch, "name" : "1-1/8 in." },
            ScrewLength.IN_1_25 : { "length" : 1.25 * inch, "name" : "1-1/4 in." },
            ScrewLength.IN_1_375 : { "length" : 1.375 * inch, "name" : "1-3/8 in." },
            ScrewLength.IN_1_5 : { "length" : 1.5 * inch, "name" : "1-1/2 in." },
            ScrewLength.IN_1_625 : { "length" : 1.625 * inch, "name" : "1-5/8 in." },
            ScrewLength.IN_1_75 : { "length" : 1.75 * inch, "name" : "1-3/4 in." },
            ScrewLength.IN_1_875 : { "length" : 1.875 * inch, "name" : "1-7/8 in." },
            ScrewLength.IN_2 : { "length" : 2 * inch, "name" : "2 in." },
            ScrewLength.IN_2_125 : { "length" : 2.125 * inch, "name" : "2-1/8 in." },
            ScrewLength.IN_2_25 : { "length" : 2.25 * inch, "name" : "2-1/4 in." },
            ScrewLength.IN_2_375 : { "length" : 2.375 * inch, "name" : "2-3/8 in." },
            ScrewLength.IN_2_5 : { "length" : 2.5 * inch, "name" : "2-1/2 in." },
            ScrewLength.IN_2_625 : { "length" : 2.625 * inch, "name" : "2-5/8 in." },
            ScrewLength.IN_2_75 : { "length" : 2.75 * inch, "name" : "2-3/4 in." },
            ScrewLength.IN_3 : { "length" : 3 * inch, "name" : "3 in." },
            ScrewLength.IN_3_25 : { "length" : 3.25 * inch, "name" : "3-1/4 in." },
            ScrewLength.IN_3_5 : { "length" : 3.5 * inch, "name" : "3-1/2 in." },
            ScrewLength.IN_3_75 : { "length" : 3.75 * inch, "name" : "3-3/4 in." },
            ScrewLength.IN_4 : { "length" : 4 * inch, "name" : "4 in." },
            ScrewLength.IN_4_25 : { "length" : 4.25 * inch, "name" : "4-1/4 in." },
            ScrewLength.IN_4_5 : { "length" : 4.5 * inch, "name" : "4-1/2 in." },
            ScrewLength.IN_4_75 : { "length" : 4.75 * inch, "name" : "4-3/4 in." },
            ScrewLength.IN_5 : { "length" : 5 * inch, "name" : "5 in." },
            ScrewLength.IN_5_25 : { "length" : 5.25 * inch, "name" : "5-1/4 in." },
            ScrewLength.IN_5_5 : { "length" : 5.5 * inch, "name" : "5-1/2 in." },
            ScrewLength.IN_5_75 : { "length" : 5.75 * inch, "name" : "5-3/4 in." },
            ScrewLength.IN_6 : { "length" : 6 * inch, "name" : "6 in." },
            ScrewLength.IN_6_25 : { "length" : 6.25 * inch, "name" : "6-1/4 in." },
            ScrewLength.IN_6_5 : { "length" : 6.5 * inch, "name" : "6-1/2 in." },
            ScrewLength.IN_7 : { "length" : 7 * inch, "name" : "7 in." },
            ScrewLength.IN_7_5 : { "length" : 7.5 * inch, "name" : "7-1/2 in." },
            ScrewLength.IN_8 : { "length" : 8 * inch, "name" : "8 in." },
            ScrewLength.IN_8_5 : { "length" : 8.5 * inch, "name" : "8-1/2 in." },
            ScrewLength.IN_9 : { "length" : 9 * inch, "name" : "9 in." },
            ScrewLength.IN_10 : { "length" : 10 * inch, "name" : "10 in." }
        };

/** What each vendor makes, by vendor, size, head, and length: each screw's part number and url, where it has them. */
export const SCREWS = {
        FastenerVendor.GENERIC : {
            FastenerSize.NO_4_40 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_09375 : {},
                    ScrewLength.IN_0_125 : {},
                    ScrewLength.IN_0_15625 : {},
                    ScrewLength.IN_0_1875 : {},
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_3125 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_125 : {},
                    ScrewLength.IN_1_25 : {},
                    ScrewLength.IN_1_375 : {},
                    ScrewLength.IN_1_5 : {},
                    ScrewLength.IN_1_625 : {},
                    ScrewLength.IN_1_75 : {},
                    ScrewLength.IN_2 : {},
                    ScrewLength.IN_2_25 : {},
                    ScrewLength.IN_2_5 : {},
                    ScrewLength.IN_2_75 : {},
                    ScrewLength.IN_3 : {},
                    ScrewLength.IN_3_5 : {},
                    ScrewLength.IN_4 : {}
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_125 : {},
                    ScrewLength.IN_0_1875 : {},
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_3125 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_25 : {},
                    ScrewLength.IN_1_5 : {},
                    ScrewLength.IN_2 : {}
                },
                ScrewHead.FLAT : {
                    ScrewLength.IN_0_125 : {},
                    ScrewLength.IN_0_15625 : {},
                    ScrewLength.IN_0_1875 : {},
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_3125 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_125 : {},
                    ScrewLength.IN_1_25 : {},
                    ScrewLength.IN_1_5 : {}
                }
            },
            FastenerSize.NO_6_32 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_09375 : {},
                    ScrewLength.IN_0_125 : {},
                    ScrewLength.IN_0_15625 : {},
                    ScrewLength.IN_0_1875 : {},
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_3125 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_125 : {},
                    ScrewLength.IN_1_25 : {},
                    ScrewLength.IN_1_375 : {},
                    ScrewLength.IN_1_5 : {},
                    ScrewLength.IN_1_625 : {},
                    ScrewLength.IN_1_75 : {},
                    ScrewLength.IN_1_875 : {},
                    ScrewLength.IN_2 : {},
                    ScrewLength.IN_2_125 : {},
                    ScrewLength.IN_2_25 : {},
                    ScrewLength.IN_2_5 : {},
                    ScrewLength.IN_2_75 : {},
                    ScrewLength.IN_3 : {},
                    ScrewLength.IN_3_5 : {}
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_125 : {},
                    ScrewLength.IN_0_1875 : {},
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_3125 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_125 : {},
                    ScrewLength.IN_1_25 : {}
                },
                ScrewHead.FLAT : {
                    ScrewLength.IN_0_125 : {},
                    ScrewLength.IN_0_15625 : {},
                    ScrewLength.IN_0_1875 : {},
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_3125 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_25 : {},
                    ScrewLength.IN_1_5 : {},
                    ScrewLength.IN_1_75 : {}
                }
            },
            FastenerSize.NO_8_32 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_09375 : {},
                    ScrewLength.IN_0_125 : {},
                    ScrewLength.IN_0_15625 : {},
                    ScrewLength.IN_0_1875 : {},
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_3125 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_6875 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_125 : {},
                    ScrewLength.IN_1_25 : {},
                    ScrewLength.IN_1_375 : {},
                    ScrewLength.IN_1_5 : {},
                    ScrewLength.IN_1_625 : {},
                    ScrewLength.IN_1_75 : {},
                    ScrewLength.IN_1_875 : {},
                    ScrewLength.IN_2 : {},
                    ScrewLength.IN_2_25 : {},
                    ScrewLength.IN_2_5 : {},
                    ScrewLength.IN_2_75 : {},
                    ScrewLength.IN_3 : {},
                    ScrewLength.IN_3_25 : {},
                    ScrewLength.IN_3_5 : {},
                    ScrewLength.IN_3_75 : {},
                    ScrewLength.IN_4 : {},
                    ScrewLength.IN_4_5 : {},
                    ScrewLength.IN_5 : {}
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_125 : {},
                    ScrewLength.IN_0_15625 : {},
                    ScrewLength.IN_0_1875 : {},
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_3125 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_125 : {},
                    ScrewLength.IN_1_25 : {},
                    ScrewLength.IN_1_375 : {},
                    ScrewLength.IN_1_5 : {},
                    ScrewLength.IN_1_75 : {},
                    ScrewLength.IN_2 : {},
                    ScrewLength.IN_2_25 : {},
                    ScrewLength.IN_2_5 : {},
                    ScrewLength.IN_3 : {}
                },
                ScrewHead.FLAT : {
                    ScrewLength.IN_0_15625 : {},
                    ScrewLength.IN_0_1875 : {},
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_3125 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_125 : {},
                    ScrewLength.IN_1_25 : {},
                    ScrewLength.IN_1_375 : {},
                    ScrewLength.IN_1_5 : {},
                    ScrewLength.IN_1_625 : {},
                    ScrewLength.IN_1_75 : {},
                    ScrewLength.IN_2 : {},
                    ScrewLength.IN_2_25 : {},
                    ScrewLength.IN_2_5 : {},
                    ScrewLength.IN_3 : {}
                }
            },
            FastenerSize.NO_10_32 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_15625 : {},
                    ScrewLength.IN_0_1875 : {},
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_3125 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_6875 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_125 : {},
                    ScrewLength.IN_1_25 : {},
                    ScrewLength.IN_1_375 : {},
                    ScrewLength.IN_1_5 : {},
                    ScrewLength.IN_1_625 : {},
                    ScrewLength.IN_1_75 : {},
                    ScrewLength.IN_1_875 : {},
                    ScrewLength.IN_2 : {},
                    ScrewLength.IN_2_25 : {},
                    ScrewLength.IN_2_5 : {},
                    ScrewLength.IN_2_75 : {},
                    ScrewLength.IN_3 : {},
                    ScrewLength.IN_3_25 : {},
                    ScrewLength.IN_3_5 : {},
                    ScrewLength.IN_3_75 : {},
                    ScrewLength.IN_4 : {},
                    ScrewLength.IN_4_5 : {},
                    ScrewLength.IN_5 : {},
                    ScrewLength.IN_5_5 : {},
                    ScrewLength.IN_6 : {},
                    ScrewLength.IN_6_5 : {},
                    ScrewLength.IN_7 : {}
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_125 : {},
                    ScrewLength.IN_0_1875 : {},
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_3125 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_125 : {},
                    ScrewLength.IN_1_25 : {},
                    ScrewLength.IN_1_375 : {},
                    ScrewLength.IN_1_5 : {},
                    ScrewLength.IN_1_625 : {},
                    ScrewLength.IN_1_75 : {},
                    ScrewLength.IN_2 : {},
                    ScrewLength.IN_2_25 : {},
                    ScrewLength.IN_2_5 : {},
                    ScrewLength.IN_2_75 : {},
                    ScrewLength.IN_3 : {},
                    ScrewLength.IN_4_75 : {}
                },
                ScrewHead.FLAT : {
                    ScrewLength.IN_0_1875 : {},
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_3125 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_125 : {},
                    ScrewLength.IN_1_25 : {},
                    ScrewLength.IN_1_375 : {},
                    ScrewLength.IN_1_5 : {},
                    ScrewLength.IN_1_625 : {},
                    ScrewLength.IN_1_75 : {},
                    ScrewLength.IN_2 : {},
                    ScrewLength.IN_2_25 : {},
                    ScrewLength.IN_2_5 : {},
                    ScrewLength.IN_3 : {},
                    ScrewLength.IN_3_5 : {},
                    ScrewLength.IN_4 : {}
                }
            },
            FastenerSize.QUARTER_20 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_3125 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_6875 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_125 : {},
                    ScrewLength.IN_1_25 : {},
                    ScrewLength.IN_1_375 : {},
                    ScrewLength.IN_1_5 : {},
                    ScrewLength.IN_1_625 : {},
                    ScrewLength.IN_1_75 : {},
                    ScrewLength.IN_1_875 : {},
                    ScrewLength.IN_2 : {},
                    ScrewLength.IN_2_25 : {},
                    ScrewLength.IN_2_375 : {},
                    ScrewLength.IN_2_5 : {},
                    ScrewLength.IN_2_625 : {},
                    ScrewLength.IN_2_75 : {},
                    ScrewLength.IN_3 : {},
                    ScrewLength.IN_3_25 : {},
                    ScrewLength.IN_3_5 : {},
                    ScrewLength.IN_3_75 : {},
                    ScrewLength.IN_4 : {},
                    ScrewLength.IN_4_25 : {},
                    ScrewLength.IN_4_5 : {},
                    ScrewLength.IN_5 : {},
                    ScrewLength.IN_5_25 : {},
                    ScrewLength.IN_5_5 : {},
                    ScrewLength.IN_5_75 : {},
                    ScrewLength.IN_6 : {},
                    ScrewLength.IN_6_25 : {},
                    ScrewLength.IN_6_5 : {},
                    ScrewLength.IN_7 : {},
                    ScrewLength.IN_7_5 : {},
                    ScrewLength.IN_8 : {},
                    ScrewLength.IN_8_5 : {},
                    ScrewLength.IN_9 : {},
                    ScrewLength.IN_10 : {}
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_1875 : {},
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_3125 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_125 : {},
                    ScrewLength.IN_1_25 : {},
                    ScrewLength.IN_1_375 : {},
                    ScrewLength.IN_1_5 : {},
                    ScrewLength.IN_1_625 : {},
                    ScrewLength.IN_1_75 : {},
                    ScrewLength.IN_2 : {},
                    ScrewLength.IN_2_25 : {},
                    ScrewLength.IN_2_5 : {},
                    ScrewLength.IN_2_75 : {},
                    ScrewLength.IN_3 : {},
                    ScrewLength.IN_3_25 : {},
                    ScrewLength.IN_3_5 : {},
                    ScrewLength.IN_4 : {}
                },
                ScrewHead.FLAT : {
                    ScrewLength.IN_0_25 : {},
                    ScrewLength.IN_0_375 : {},
                    ScrewLength.IN_0_4375 : {},
                    ScrewLength.IN_0_5 : {},
                    ScrewLength.IN_0_5625 : {},
                    ScrewLength.IN_0_625 : {},
                    ScrewLength.IN_0_75 : {},
                    ScrewLength.IN_0_875 : {},
                    ScrewLength.IN_1 : {},
                    ScrewLength.IN_1_125 : {},
                    ScrewLength.IN_1_25 : {},
                    ScrewLength.IN_1_375 : {},
                    ScrewLength.IN_1_5 : {},
                    ScrewLength.IN_1_625 : {},
                    ScrewLength.IN_1_75 : {},
                    ScrewLength.IN_2 : {},
                    ScrewLength.IN_2_25 : {},
                    ScrewLength.IN_2_5 : {},
                    ScrewLength.IN_2_75 : {},
                    ScrewLength.IN_3 : {},
                    ScrewLength.IN_3_25 : {},
                    ScrewLength.IN_3_5 : {},
                    ScrewLength.IN_4 : {},
                    ScrewLength.IN_5 : {},
                    ScrewLength.IN_6 : {}
                }
            }
        },
        FastenerVendor.MCMASTER : {
            FastenerSize.NO_4_40 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_09375 : { "partNumber" : "91251A231", "url" : "https://www.mcmaster.com/91251A231/" },
                    ScrewLength.IN_0_125 : { "partNumber" : "91864A018", "url" : "https://www.mcmaster.com/91864A018/" },
                    ScrewLength.IN_0_15625 : { "partNumber" : "91251A232", "url" : "https://www.mcmaster.com/91251A232/" },
                    ScrewLength.IN_0_1875 : { "partNumber" : "91251A105", "url" : "https://www.mcmaster.com/91251A105/" },
                    ScrewLength.IN_0_25 : { "partNumber" : "91251A106", "url" : "https://www.mcmaster.com/91251A106/" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "91251A107", "url" : "https://www.mcmaster.com/91251A107/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91251A108", "url" : "https://www.mcmaster.com/91251A108/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "91864A081", "url" : "https://www.mcmaster.com/91864A081/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91251A110", "url" : "https://www.mcmaster.com/91251A110/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "91864A028", "url" : "https://www.mcmaster.com/91864A028/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91251A112", "url" : "https://www.mcmaster.com/91251A112/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91251A113", "url" : "https://www.mcmaster.com/91251A113/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "91251A114", "url" : "https://www.mcmaster.com/91251A114/" },
                    ScrewLength.IN_1 : { "partNumber" : "91251A115", "url" : "https://www.mcmaster.com/91251A115/" },
                    ScrewLength.IN_1_125 : { "partNumber" : "91864A029", "url" : "https://www.mcmaster.com/91864A029/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "91251A078", "url" : "https://www.mcmaster.com/91251A078/" },
                    ScrewLength.IN_1_375 : { "partNumber" : "91251A210", "url" : "https://www.mcmaster.com/91251A210/" },
                    ScrewLength.IN_1_5 : { "partNumber" : "91251A149", "url" : "https://www.mcmaster.com/91251A149/" },
                    ScrewLength.IN_1_625 : { "partNumber" : "91251A233", "url" : "https://www.mcmaster.com/91251A233/" },
                    ScrewLength.IN_1_75 : { "partNumber" : "91251A160", "url" : "https://www.mcmaster.com/91251A160/" },
                    ScrewLength.IN_2 : { "partNumber" : "91251A162", "url" : "https://www.mcmaster.com/91251A162/" },
                    ScrewLength.IN_2_25 : { "partNumber" : "91251A211", "url" : "https://www.mcmaster.com/91251A211/" },
                    ScrewLength.IN_2_5 : { "partNumber" : "91251A834", "url" : "https://www.mcmaster.com/91251A834/" },
                    ScrewLength.IN_2_75 : { "partNumber" : "91251A513", "url" : "https://www.mcmaster.com/91251A513/" },
                    ScrewLength.IN_3 : { "partNumber" : "91251A212", "url" : "https://www.mcmaster.com/91251A212/" },
                    ScrewLength.IN_3_5 : { "partNumber" : "90044A291", "url" : "https://www.mcmaster.com/90044A291/" },
                    ScrewLength.IN_4 : { "partNumber" : "90044A292", "url" : "https://www.mcmaster.com/90044A292/" }
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_125 : { "partNumber" : "91255A103", "url" : "https://www.mcmaster.com/91255A103/" },
                    ScrewLength.IN_0_1875 : { "partNumber" : "91255A105", "url" : "https://www.mcmaster.com/91255A105/" },
                    ScrewLength.IN_0_25 : { "partNumber" : "91255A106", "url" : "https://www.mcmaster.com/91255A106/" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "91255A107", "url" : "https://www.mcmaster.com/91255A107/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91255A108", "url" : "https://www.mcmaster.com/91255A108/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "91255A021", "url" : "https://www.mcmaster.com/91255A021/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91255A110", "url" : "https://www.mcmaster.com/91255A110/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "91255A022", "url" : "https://www.mcmaster.com/91255A022/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91255A112", "url" : "https://www.mcmaster.com/91255A112/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91255A113", "url" : "https://www.mcmaster.com/91255A113/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "91255A115", "url" : "https://www.mcmaster.com/91255A115/" },
                    ScrewLength.IN_1 : { "partNumber" : "91255A116", "url" : "https://www.mcmaster.com/91255A116/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "91255A117", "url" : "https://www.mcmaster.com/91255A117/" },
                    ScrewLength.IN_1_5 : { "partNumber" : "91255A023", "url" : "https://www.mcmaster.com/91255A023/" },
                    ScrewLength.IN_2 : { "partNumber" : "91255A118", "url" : "https://www.mcmaster.com/91255A118/" }
                },
                ScrewHead.FLAT : {
                    ScrewLength.IN_0_125 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_15625 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_1875 : { "partNumber" : "91263A375", "url" : "https://www.mcmaster.com/91263A375/" },
                    ScrewLength.IN_0_25 : { "partNumber" : "91263A376", "url" : "https://www.mcmaster.com/91263A376/" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "91263A136", "url" : "https://www.mcmaster.com/91263A136/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91263A501", "url" : "https://www.mcmaster.com/91263A501/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91263A503", "url" : "https://www.mcmaster.com/91263A503/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91263A961", "url" : "https://www.mcmaster.com/91263A961/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91263A962", "url" : "https://www.mcmaster.com/91263A962/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1_125 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1_5 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" }
                }
            },
            FastenerSize.NO_6_32 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_09375 : { "partNumber" : "91251A268", "url" : "https://www.mcmaster.com/91251A268/" },
                    ScrewLength.IN_0_125 : { "partNumber" : "91864A084", "url" : "https://www.mcmaster.com/91864A084/" },
                    ScrewLength.IN_0_15625 : { "partNumber" : "91251A269", "url" : "https://www.mcmaster.com/91251A269/" },
                    ScrewLength.IN_0_1875 : { "partNumber" : "91864A085", "url" : "https://www.mcmaster.com/91864A085/" },
                    ScrewLength.IN_0_25 : { "partNumber" : "91251A144", "url" : "https://www.mcmaster.com/91251A144/" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "91864A019", "url" : "https://www.mcmaster.com/91864A019/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91251A146", "url" : "https://www.mcmaster.com/91251A146/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "91864A023", "url" : "https://www.mcmaster.com/91864A023/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91251A148", "url" : "https://www.mcmaster.com/91251A148/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "91864A086", "url" : "https://www.mcmaster.com/91864A086/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91251A150", "url" : "https://www.mcmaster.com/91251A150/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91251A151", "url" : "https://www.mcmaster.com/91251A151/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "91251A152", "url" : "https://www.mcmaster.com/91251A152/" },
                    ScrewLength.IN_1 : { "partNumber" : "91251A153", "url" : "https://www.mcmaster.com/91251A153/" },
                    ScrewLength.IN_1_125 : { "partNumber" : "91251A870", "url" : "https://www.mcmaster.com/91251A870/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "91251A155", "url" : "https://www.mcmaster.com/91251A155/" },
                    ScrewLength.IN_1_375 : { "partNumber" : "91251A874", "url" : "https://www.mcmaster.com/91251A874/" },
                    ScrewLength.IN_1_5 : { "partNumber" : "91251A157", "url" : "https://www.mcmaster.com/91251A157/" },
                    ScrewLength.IN_1_625 : { "partNumber" : "91251A875", "url" : "https://www.mcmaster.com/91251A875/" },
                    ScrewLength.IN_1_75 : { "partNumber" : "91251A158", "url" : "https://www.mcmaster.com/91251A158/" },
                    ScrewLength.IN_1_875 : { "partNumber" : "91251A270", "url" : "https://www.mcmaster.com/91251A270/" },
                    ScrewLength.IN_2 : { "partNumber" : "91251A159", "url" : "https://www.mcmaster.com/91251A159/" },
                    ScrewLength.IN_2_25 : { "partNumber" : "91251A876", "url" : "https://www.mcmaster.com/91251A876/" },
                    ScrewLength.IN_2_5 : { "partNumber" : "91251A871", "url" : "https://www.mcmaster.com/91251A871/" },
                    ScrewLength.IN_2_75 : { "partNumber" : "91251A872", "url" : "https://www.mcmaster.com/91251A872/" },
                    ScrewLength.IN_3 : { "partNumber" : "91251A873", "url" : "https://www.mcmaster.com/91251A873/" },
                    ScrewLength.IN_3_5 : { "partNumber" : "90044A295", "url" : "https://www.mcmaster.com/90044A295/" }
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_125 : { "partNumber" : "91255A140", "url" : "https://www.mcmaster.com/91255A140/" },
                    ScrewLength.IN_0_1875 : { "partNumber" : "91255A142", "url" : "https://www.mcmaster.com/91255A142/" },
                    ScrewLength.IN_0_25 : { "partNumber" : "91255A144", "url" : "https://www.mcmaster.com/91255A144/" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "91255A145", "url" : "https://www.mcmaster.com/91255A145/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91255A146", "url" : "https://www.mcmaster.com/91255A146/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "91255A133", "url" : "https://www.mcmaster.com/91255A133/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91255A148", "url" : "https://www.mcmaster.com/91255A148/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "91255A134", "url" : "https://www.mcmaster.com/91255A134/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91255A150", "url" : "https://www.mcmaster.com/91255A150/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91255A151", "url" : "https://www.mcmaster.com/91255A151/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "91255A152", "url" : "https://www.mcmaster.com/91255A152/" },
                    ScrewLength.IN_1 : { "partNumber" : "91255A153", "url" : "https://www.mcmaster.com/91255A153/" },
                    ScrewLength.IN_1_125 : { "partNumber" : "91255A135", "url" : "https://www.mcmaster.com/91255A135/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "91255A155", "url" : "https://www.mcmaster.com/91255A155/" }
                },
                ScrewHead.FLAT : {
                    ScrewLength.IN_0_125 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_15625 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_1875 : { "partNumber" : "91263A505", "url" : "https://www.mcmaster.com/91263A505/" },
                    ScrewLength.IN_0_25 : { "partNumber" : "91263A512", "url" : "https://www.mcmaster.com/91263A512/" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "91263A506", "url" : "https://www.mcmaster.com/91263A506/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91263A514", "url" : "https://www.mcmaster.com/91263A514/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91263A516", "url" : "https://www.mcmaster.com/91263A516/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91263A507", "url" : "https://www.mcmaster.com/91263A507/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91263A518", "url" : "https://www.mcmaster.com/91263A518/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1 : { "partNumber" : "91263A520", "url" : "https://www.mcmaster.com/91263A520/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1_5 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1_75 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" }
                }
            },
            FastenerSize.NO_8_32 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_09375 : { "partNumber" : "91251A271", "url" : "https://www.mcmaster.com/91251A271/" },
                    ScrewLength.IN_0_125 : { "partNumber" : "91251A179", "url" : "https://www.mcmaster.com/91251A179/" },
                    ScrewLength.IN_0_15625 : { "partNumber" : "91251A272", "url" : "https://www.mcmaster.com/91251A272/" },
                    ScrewLength.IN_0_1875 : { "partNumber" : "91251A180", "url" : "https://www.mcmaster.com/91251A180/" },
                    ScrewLength.IN_0_25 : { "partNumber" : "91251A190", "url" : "https://www.mcmaster.com/91251A190/" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "91251A191", "url" : "https://www.mcmaster.com/91251A191/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91251A192", "url" : "https://www.mcmaster.com/91251A192/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "91864A024", "url" : "https://www.mcmaster.com/91864A024/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91251A194", "url" : "https://www.mcmaster.com/91251A194/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "91864A033", "url" : "https://www.mcmaster.com/91864A033/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91251A196", "url" : "https://www.mcmaster.com/91251A196/" },
                    ScrewLength.IN_0_6875 : { "partNumber" : "90044A421", "url" : "https://www.mcmaster.com/90044A421/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91251A197", "url" : "https://www.mcmaster.com/91251A197/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "91251A198", "url" : "https://www.mcmaster.com/91251A198/" },
                    ScrewLength.IN_1 : { "partNumber" : "91251A199", "url" : "https://www.mcmaster.com/91251A199/" },
                    ScrewLength.IN_1_125 : { "partNumber" : "91864A025", "url" : "https://www.mcmaster.com/91864A025/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "91251A201", "url" : "https://www.mcmaster.com/91251A201/" },
                    ScrewLength.IN_1_375 : { "partNumber" : "91864A026", "url" : "https://www.mcmaster.com/91864A026/" },
                    ScrewLength.IN_1_5 : { "partNumber" : "91251A203", "url" : "https://www.mcmaster.com/91251A203/" },
                    ScrewLength.IN_1_625 : { "partNumber" : "91864A027", "url" : "https://www.mcmaster.com/91864A027/" },
                    ScrewLength.IN_1_75 : { "partNumber" : "91251A082", "url" : "https://www.mcmaster.com/91251A082/" },
                    ScrewLength.IN_1_875 : { "partNumber" : "91251A188", "url" : "https://www.mcmaster.com/91251A188/" },
                    ScrewLength.IN_2 : { "partNumber" : "91251A205", "url" : "https://www.mcmaster.com/91251A205/" },
                    ScrewLength.IN_2_25 : { "partNumber" : "91251A184", "url" : "https://www.mcmaster.com/91251A184/" },
                    ScrewLength.IN_2_5 : { "partNumber" : "91251A207", "url" : "https://www.mcmaster.com/91251A207/" },
                    ScrewLength.IN_2_75 : { "partNumber" : "91251A827", "url" : "https://www.mcmaster.com/91251A827/" },
                    ScrewLength.IN_3 : { "partNumber" : "91251A086", "url" : "https://www.mcmaster.com/91251A086/" },
                    ScrewLength.IN_3_25 : { "partNumber" : "91251A189", "url" : "https://www.mcmaster.com/91251A189/" },
                    ScrewLength.IN_3_5 : { "partNumber" : "91251A186", "url" : "https://www.mcmaster.com/91251A186/" },
                    ScrewLength.IN_3_75 : { "partNumber" : "91251A285", "url" : "https://www.mcmaster.com/91251A285/" },
                    ScrewLength.IN_4 : { "partNumber" : "91251A193", "url" : "https://www.mcmaster.com/91251A193/" },
                    ScrewLength.IN_4_5 : { "partNumber" : "90044A297", "url" : "https://www.mcmaster.com/90044A297/" },
                    ScrewLength.IN_5 : { "partNumber" : "90044A298", "url" : "https://www.mcmaster.com/90044A298/" }
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_125 : { "partNumber" : "91255A162", "url" : "https://www.mcmaster.com/91255A162/" },
                    ScrewLength.IN_0_15625 : { "partNumber" : "91255A163", "url" : "https://www.mcmaster.com/91255A163/" },
                    ScrewLength.IN_0_1875 : { "partNumber" : "91255A189", "url" : "https://www.mcmaster.com/91255A189/" },
                    ScrewLength.IN_0_25 : { "partNumber" : "91255A190", "url" : "https://www.mcmaster.com/91255A190/" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "91255A191", "url" : "https://www.mcmaster.com/91255A191/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91255A192", "url" : "https://www.mcmaster.com/91255A192/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "91255A164", "url" : "https://www.mcmaster.com/91255A164/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91255A194", "url" : "https://www.mcmaster.com/91255A194/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "91255A165", "url" : "https://www.mcmaster.com/91255A165/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91255A196", "url" : "https://www.mcmaster.com/91255A196/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91255A197", "url" : "https://www.mcmaster.com/91255A197/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "91255A166", "url" : "https://www.mcmaster.com/91255A166/" },
                    ScrewLength.IN_1 : { "partNumber" : "91255A199", "url" : "https://www.mcmaster.com/91255A199/" },
                    ScrewLength.IN_1_125 : { "partNumber" : "91255A167", "url" : "https://www.mcmaster.com/91255A167/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "91255A201", "url" : "https://www.mcmaster.com/91255A201/" },
                    ScrewLength.IN_1_375 : { "partNumber" : "91255A168", "url" : "https://www.mcmaster.com/91255A168/" },
                    ScrewLength.IN_1_5 : { "partNumber" : "91255A203", "url" : "https://www.mcmaster.com/91255A203/" },
                    ScrewLength.IN_1_75 : { "partNumber" : "91255A169", "url" : "https://www.mcmaster.com/91255A169/" },
                    ScrewLength.IN_2 : { "partNumber" : "91255A205", "url" : "https://www.mcmaster.com/91255A205/" },
                    ScrewLength.IN_2_25 : { "partNumber" : "91255A206", "url" : "https://www.mcmaster.com/91255A206/" },
                    ScrewLength.IN_2_5 : { "partNumber" : "91255A811", "url" : "https://www.mcmaster.com/91255A811/" },
                    ScrewLength.IN_3 : { "partNumber" : "91255A812", "url" : "https://www.mcmaster.com/91255A812/" }
                },
                ScrewHead.FLAT : {
                    ScrewLength.IN_0_15625 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_1875 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_25 : { "partNumber" : "91263A521", "url" : "https://www.mcmaster.com/91263A521/" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "91263A523", "url" : "https://www.mcmaster.com/91263A523/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91263A522", "url" : "https://www.mcmaster.com/91263A522/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91263A524", "url" : "https://www.mcmaster.com/91263A524/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91263A525", "url" : "https://www.mcmaster.com/91263A525/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91263A526", "url" : "https://www.mcmaster.com/91263A526/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "91263A527", "url" : "https://www.mcmaster.com/91263A527/" },
                    ScrewLength.IN_1 : { "partNumber" : "91263A528", "url" : "https://www.mcmaster.com/91263A528/" },
                    ScrewLength.IN_1_125 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "91263A142", "url" : "https://www.mcmaster.com/91263A142/" },
                    ScrewLength.IN_1_375 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1_5 : { "partNumber" : "91263A145", "url" : "https://www.mcmaster.com/91263A145/" },
                    ScrewLength.IN_1_625 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1_75 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_2 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_2_25 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_2_5 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_3 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" }
                }
            },
            FastenerSize.NO_10_32 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_15625 : { "partNumber" : "91251A518", "url" : "https://www.mcmaster.com/91251A518/" },
                    ScrewLength.IN_0_1875 : { "partNumber" : "91251A901", "url" : "https://www.mcmaster.com/91251A901/" },
                    ScrewLength.IN_0_25 : { "partNumber" : "91251A338", "url" : "https://www.mcmaster.com/91251A338/" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "91251A337", "url" : "https://www.mcmaster.com/91251A337/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91251A340", "url" : "https://www.mcmaster.com/91251A340/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "91251A350", "url" : "https://www.mcmaster.com/91251A350/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91251A342", "url" : "https://www.mcmaster.com/91251A342/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "91251A356", "url" : "https://www.mcmaster.com/91251A356/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91251A344", "url" : "https://www.mcmaster.com/91251A344/" },
                    ScrewLength.IN_0_6875 : { "partNumber" : "90044A201", "url" : "https://www.mcmaster.com/90044A201/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91251A345", "url" : "https://www.mcmaster.com/91251A345/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "91251A346", "url" : "https://www.mcmaster.com/91251A346/" },
                    ScrewLength.IN_1 : { "partNumber" : "91251A347", "url" : "https://www.mcmaster.com/91251A347/" },
                    ScrewLength.IN_1_125 : { "partNumber" : "91251A348", "url" : "https://www.mcmaster.com/91251A348/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "91251A349", "url" : "https://www.mcmaster.com/91251A349/" },
                    ScrewLength.IN_1_375 : { "partNumber" : "90044A202", "url" : "https://www.mcmaster.com/90044A202/" },
                    ScrewLength.IN_1_5 : { "partNumber" : "91251A351", "url" : "https://www.mcmaster.com/91251A351/" },
                    ScrewLength.IN_1_625 : { "partNumber" : "91251A287", "url" : "https://www.mcmaster.com/91251A287/" },
                    ScrewLength.IN_1_75 : { "partNumber" : "91251A352", "url" : "https://www.mcmaster.com/91251A352/" },
                    ScrewLength.IN_1_875 : { "partNumber" : "91251A288", "url" : "https://www.mcmaster.com/91251A288/" },
                    ScrewLength.IN_2 : { "partNumber" : "91251A353", "url" : "https://www.mcmaster.com/91251A353/" },
                    ScrewLength.IN_2_25 : { "partNumber" : "91251A354", "url" : "https://www.mcmaster.com/91251A354/" },
                    ScrewLength.IN_2_5 : { "partNumber" : "91251A355", "url" : "https://www.mcmaster.com/91251A355/" },
                    ScrewLength.IN_2_75 : { "partNumber" : "91251A359", "url" : "https://www.mcmaster.com/91251A359/" },
                    ScrewLength.IN_3 : { "partNumber" : "91251A360", "url" : "https://www.mcmaster.com/91251A360/" },
                    ScrewLength.IN_3_5 : { "partNumber" : "91251A361", "url" : "https://www.mcmaster.com/91251A361/" },
                    ScrewLength.IN_3_75 : { "partNumber" : "91251A289", "url" : "https://www.mcmaster.com/91251A289/" },
                    ScrewLength.IN_4 : { "partNumber" : "91251A364", "url" : "https://www.mcmaster.com/91251A364/" },
                    ScrewLength.IN_4_5 : { "partNumber" : "91251A357", "url" : "https://www.mcmaster.com/91251A357/" },
                    ScrewLength.IN_5 : { "partNumber" : "91251A358", "url" : "https://www.mcmaster.com/91251A358/" },
                    ScrewLength.IN_5_5 : { "partNumber" : "91251A906", "url" : "https://www.mcmaster.com/91251A906/" },
                    ScrewLength.IN_6 : { "partNumber" : "91251A907", "url" : "https://www.mcmaster.com/91251A907/" },
                    ScrewLength.IN_6_5 : { "partNumber" : "90044A262", "url" : "https://www.mcmaster.com/90044A262/" },
                    ScrewLength.IN_7 : { "partNumber" : "90044A263", "url" : "https://www.mcmaster.com/90044A263/" }
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_125 : { "partNumber" : "91255A024", "url" : "https://www.mcmaster.com/91255A024/" },
                    ScrewLength.IN_0_1875 : { "partNumber" : "91255A025", "url" : "https://www.mcmaster.com/91255A025/" },
                    ScrewLength.IN_0_25 : { "partNumber" : "91255A261", "url" : "https://www.mcmaster.com/91255A261/" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "91255A262", "url" : "https://www.mcmaster.com/91255A262/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91255A263", "url" : "https://www.mcmaster.com/91255A263/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "91255A026", "url" : "https://www.mcmaster.com/91255A026/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91255A265", "url" : "https://www.mcmaster.com/91255A265/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "91255A027", "url" : "https://www.mcmaster.com/91255A027/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91255A267", "url" : "https://www.mcmaster.com/91255A267/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91255A269", "url" : "https://www.mcmaster.com/91255A269/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "91255A271", "url" : "https://www.mcmaster.com/91255A271/" },
                    ScrewLength.IN_1 : { "partNumber" : "91255A273", "url" : "https://www.mcmaster.com/91255A273/" },
                    ScrewLength.IN_1_125 : { "partNumber" : "91255A028", "url" : "https://www.mcmaster.com/91255A028/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "91255A274", "url" : "https://www.mcmaster.com/91255A274/" },
                    ScrewLength.IN_1_375 : { "partNumber" : "91255A272", "url" : "https://www.mcmaster.com/91255A272/" },
                    ScrewLength.IN_1_5 : { "partNumber" : "91255A275", "url" : "https://www.mcmaster.com/91255A275/" },
                    ScrewLength.IN_1_625 : { "partNumber" : "91255A277", "url" : "https://www.mcmaster.com/91255A277/" },
                    ScrewLength.IN_1_75 : { "partNumber" : "91255A276", "url" : "https://www.mcmaster.com/91255A276/" },
                    ScrewLength.IN_2 : { "partNumber" : "91255A278", "url" : "https://www.mcmaster.com/91255A278/" },
                    ScrewLength.IN_2_25 : { "partNumber" : "91255A815", "url" : "https://www.mcmaster.com/91255A815/" },
                    ScrewLength.IN_2_5 : { "partNumber" : "91255A029", "url" : "https://www.mcmaster.com/91255A029/" },
                    ScrewLength.IN_3 : { "partNumber" : "91255A030", "url" : "https://www.mcmaster.com/91255A030/" }
                },
                ScrewHead.FLAT : {
                    ScrewLength.IN_0_1875 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_25 : { "partNumber" : "91263A540", "url" : "https://www.mcmaster.com/91263A540/" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "91263A157", "url" : "https://www.mcmaster.com/91263A157/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91263A542", "url" : "https://www.mcmaster.com/91263A542/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91263A544", "url" : "https://www.mcmaster.com/91263A544/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91263A543", "url" : "https://www.mcmaster.com/91263A543/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91263A546", "url" : "https://www.mcmaster.com/91263A546/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "91263A547", "url" : "https://www.mcmaster.com/91263A547/" },
                    ScrewLength.IN_1 : { "partNumber" : "91263A548", "url" : "https://www.mcmaster.com/91263A548/" },
                    ScrewLength.IN_1_125 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "91263A160", "url" : "https://www.mcmaster.com/91263A160/" },
                    ScrewLength.IN_1_375 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1_5 : { "partNumber" : "91263A550", "url" : "https://www.mcmaster.com/91263A550/" },
                    ScrewLength.IN_1_625 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1_75 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_2 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_2_25 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_2_5 : { "partNumber" : "91263A163", "url" : "https://www.mcmaster.com/91263A163/" },
                    ScrewLength.IN_3 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_3_5 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_4 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" }
                }
            },
            FastenerSize.QUARTER_20 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_25 : { "partNumber" : "91251A533", "url" : "https://www.mcmaster.com/91251A533/" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "91864A061", "url" : "https://www.mcmaster.com/91864A061/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91251A535", "url" : "https://www.mcmaster.com/91251A535/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "91864A062", "url" : "https://www.mcmaster.com/91864A062/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91251A537", "url" : "https://www.mcmaster.com/91251A537/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "91251A538", "url" : "https://www.mcmaster.com/91251A538/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91251A539", "url" : "https://www.mcmaster.com/91251A539/" },
                    ScrewLength.IN_0_6875 : { "partNumber" : "90044A423", "url" : "https://www.mcmaster.com/90044A423/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91251A540", "url" : "https://www.mcmaster.com/91251A540/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "91251A541", "url" : "https://www.mcmaster.com/91251A541/" },
                    ScrewLength.IN_1 : { "partNumber" : "91251A542", "url" : "https://www.mcmaster.com/91251A542/" },
                    ScrewLength.IN_1_125 : { "partNumber" : "91251A560", "url" : "https://www.mcmaster.com/91251A560/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "91251A544", "url" : "https://www.mcmaster.com/91251A544/" },
                    ScrewLength.IN_1_375 : { "partNumber" : "91251A543", "url" : "https://www.mcmaster.com/91251A543/" },
                    ScrewLength.IN_1_5 : { "partNumber" : "90044A123", "url" : "https://www.mcmaster.com/90044A123/" },
                    ScrewLength.IN_1_625 : { "partNumber" : "91251A527", "url" : "https://www.mcmaster.com/91251A527/" },
                    ScrewLength.IN_1_75 : { "partNumber" : "90044A124", "url" : "https://www.mcmaster.com/90044A124/" },
                    ScrewLength.IN_1_875 : { "partNumber" : "91251A548", "url" : "https://www.mcmaster.com/91251A548/" },
                    ScrewLength.IN_2 : { "partNumber" : "90044A125", "url" : "https://www.mcmaster.com/90044A125/" },
                    ScrewLength.IN_2_25 : { "partNumber" : "90044A126", "url" : "https://www.mcmaster.com/90044A126/" },
                    ScrewLength.IN_2_375 : { "partNumber" : "90044A425", "url" : "https://www.mcmaster.com/90044A425/" },
                    ScrewLength.IN_2_5 : { "partNumber" : "90044A127", "url" : "https://www.mcmaster.com/90044A127/" },
                    ScrewLength.IN_2_625 : { "partNumber" : "90044A426", "url" : "https://www.mcmaster.com/90044A426/" },
                    ScrewLength.IN_2_75 : { "partNumber" : "90044A128", "url" : "https://www.mcmaster.com/90044A128/" },
                    ScrewLength.IN_3 : { "partNumber" : "90044A129", "url" : "https://www.mcmaster.com/90044A129/" },
                    ScrewLength.IN_3_25 : { "partNumber" : "90044A526", "url" : "https://www.mcmaster.com/90044A526/" },
                    ScrewLength.IN_3_5 : { "partNumber" : "90044A316", "url" : "https://www.mcmaster.com/90044A316/" },
                    ScrewLength.IN_3_75 : { "partNumber" : "90044A527", "url" : "https://www.mcmaster.com/90044A527/" },
                    ScrewLength.IN_4 : { "partNumber" : "90044A131", "url" : "https://www.mcmaster.com/90044A131/" },
                    ScrewLength.IN_4_25 : { "partNumber" : "91251A559", "url" : "https://www.mcmaster.com/91251A559/" },
                    ScrewLength.IN_4_5 : { "partNumber" : "91251A089", "url" : "https://www.mcmaster.com/91251A089/" },
                    ScrewLength.IN_5 : { "partNumber" : "91251A090", "url" : "https://www.mcmaster.com/91251A090/" },
                    ScrewLength.IN_5_25 : { "partNumber" : "91251A293", "url" : "https://www.mcmaster.com/91251A293/" },
                    ScrewLength.IN_5_5 : { "partNumber" : "91251A093", "url" : "https://www.mcmaster.com/91251A093/" },
                    ScrewLength.IN_5_75 : { "partNumber" : "91251A294", "url" : "https://www.mcmaster.com/91251A294/" },
                    ScrewLength.IN_6 : { "partNumber" : "91251A095", "url" : "https://www.mcmaster.com/91251A095/" },
                    ScrewLength.IN_6_25 : { "partNumber" : "91251A562", "url" : "https://www.mcmaster.com/91251A562/" },
                    ScrewLength.IN_6_5 : { "partNumber" : "91251A563", "url" : "https://www.mcmaster.com/91251A563/" },
                    ScrewLength.IN_7 : { "partNumber" : "91251A908", "url" : "https://www.mcmaster.com/91251A908/" },
                    ScrewLength.IN_7_5 : { "partNumber" : "91251A923", "url" : "https://www.mcmaster.com/91251A923/" },
                    ScrewLength.IN_8 : { "partNumber" : "91251A566", "url" : "https://www.mcmaster.com/91251A566/" },
                    ScrewLength.IN_8_5 : { "partNumber" : "90044A299", "url" : "https://www.mcmaster.com/90044A299/" },
                    ScrewLength.IN_9 : { "partNumber" : "90044A311", "url" : "https://www.mcmaster.com/90044A311/" },
                    ScrewLength.IN_10 : { "partNumber" : "90044A312", "url" : "https://www.mcmaster.com/90044A312/" }
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_1875 : { "partNumber" : "91255A521", "url" : "https://www.mcmaster.com/91255A521/" },
                    ScrewLength.IN_0_25 : { "partNumber" : "91255A533", "url" : "https://www.mcmaster.com/91255A533/" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "91255A522", "url" : "https://www.mcmaster.com/91255A522/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91255A535", "url" : "https://www.mcmaster.com/91255A535/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "91255A523", "url" : "https://www.mcmaster.com/91255A523/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91255A537", "url" : "https://www.mcmaster.com/91255A537/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "91255A524", "url" : "https://www.mcmaster.com/91255A524/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91255A539", "url" : "https://www.mcmaster.com/91255A539/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91255A540", "url" : "https://www.mcmaster.com/91255A540/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "91255A541", "url" : "https://www.mcmaster.com/91255A541/" },
                    ScrewLength.IN_1 : { "partNumber" : "91255A542", "url" : "https://www.mcmaster.com/91255A542/" },
                    ScrewLength.IN_1_125 : { "partNumber" : "91255A525", "url" : "https://www.mcmaster.com/91255A525/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "91255A544", "url" : "https://www.mcmaster.com/91255A544/" },
                    ScrewLength.IN_1_375 : { "partNumber" : "91255A526", "url" : "https://www.mcmaster.com/91255A526/" },
                    ScrewLength.IN_1_5 : { "partNumber" : "91255A546", "url" : "https://www.mcmaster.com/91255A546/" },
                    ScrewLength.IN_1_625 : { "partNumber" : "91255A527", "url" : "https://www.mcmaster.com/91255A527/" },
                    ScrewLength.IN_1_75 : { "partNumber" : "91255A548", "url" : "https://www.mcmaster.com/91255A548/" },
                    ScrewLength.IN_2 : { "partNumber" : "91255A545", "url" : "https://www.mcmaster.com/91255A545/" },
                    ScrewLength.IN_2_25 : { "partNumber" : "91255A528", "url" : "https://www.mcmaster.com/91255A528/" },
                    ScrewLength.IN_2_5 : { "partNumber" : "91255A547", "url" : "https://www.mcmaster.com/91255A547/" },
                    ScrewLength.IN_2_75 : { "partNumber" : "91255A529", "url" : "https://www.mcmaster.com/91255A529/" },
                    ScrewLength.IN_3 : { "partNumber" : "91255A549", "url" : "https://www.mcmaster.com/91255A549/" },
                    ScrewLength.IN_3_25 : { "partNumber" : "91255A819", "url" : "https://www.mcmaster.com/91255A819/" },
                    ScrewLength.IN_3_5 : { "partNumber" : "91255A531", "url" : "https://www.mcmaster.com/91255A531/" },
                    ScrewLength.IN_4 : { "partNumber" : "91255A532", "url" : "https://www.mcmaster.com/91255A532/" }
                },
                ScrewHead.FLAT : {
                    ScrewLength.IN_0_25 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_375 : { "partNumber" : "91263A551", "url" : "https://www.mcmaster.com/91263A551/" },
                    ScrewLength.IN_0_4375 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_5 : { "partNumber" : "91263A553", "url" : "https://www.mcmaster.com/91263A553/" },
                    ScrewLength.IN_0_5625 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_0_625 : { "partNumber" : "91263A556", "url" : "https://www.mcmaster.com/91263A556/" },
                    ScrewLength.IN_0_75 : { "partNumber" : "91263A559", "url" : "https://www.mcmaster.com/91263A559/" },
                    ScrewLength.IN_0_875 : { "partNumber" : "91263A560", "url" : "https://www.mcmaster.com/91263A560/" },
                    ScrewLength.IN_1 : { "partNumber" : "91263A562", "url" : "https://www.mcmaster.com/91263A562/" },
                    ScrewLength.IN_1_125 : { "partNumber" : "91263A166", "url" : "https://www.mcmaster.com/91263A166/" },
                    ScrewLength.IN_1_25 : { "partNumber" : "91263A563", "url" : "https://www.mcmaster.com/91263A563/" },
                    ScrewLength.IN_1_375 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1_5 : { "partNumber" : "91263A564", "url" : "https://www.mcmaster.com/91263A564/" },
                    ScrewLength.IN_1_625 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_1_75 : { "partNumber" : "91263A565", "url" : "https://www.mcmaster.com/91263A565/" },
                    ScrewLength.IN_2 : { "partNumber" : "91263A566", "url" : "https://www.mcmaster.com/91263A566/" },
                    ScrewLength.IN_2_25 : { "partNumber" : "91263A567", "url" : "https://www.mcmaster.com/91263A567/" },
                    ScrewLength.IN_2_5 : { "partNumber" : "91263A568", "url" : "https://www.mcmaster.com/91263A568/" },
                    ScrewLength.IN_2_75 : { "partNumber" : "91263A569", "url" : "https://www.mcmaster.com/91263A569/" },
                    ScrewLength.IN_3 : { "partNumber" : "91263A570", "url" : "https://www.mcmaster.com/91263A570/" },
                    ScrewLength.IN_3_25 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_3_5 : { "partNumber" : "91263A169", "url" : "https://www.mcmaster.com/91263A169/" },
                    ScrewLength.IN_4 : { "partNumber" : "91263A172", "url" : "https://www.mcmaster.com/91263A172/" },
                    ScrewLength.IN_5 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" },
                    ScrewLength.IN_6 : { "partNumber" : "null", "url" : "https://www.mcmaster.com/null/" }
                }
            }
        },
        FastenerVendor.REV : {
            FastenerSize.NO_10_32 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_25 : { "partNumber" : "REV-29-2910-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2910-PK50&section=product" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "REV-29-2911-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2911-PK50&section=product" },
                    ScrewLength.IN_0_375 : { "partNumber" : "REV-29-2912-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2912-PK50&section=product" },
                    ScrewLength.IN_0_5 : { "partNumber" : "REV-29-2914-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2914-PK50&section=product" },
                    ScrewLength.IN_0_625 : { "partNumber" : "REV-29-2916-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2916-PK50&section=product" },
                    ScrewLength.IN_0_75 : { "partNumber" : "REV-29-2918-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2918-PK50&section=product" },
                    ScrewLength.IN_1 : { "partNumber" : "REV-29-2922-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2922-PK50&section=product" },
                    ScrewLength.IN_1_25 : { "partNumber" : "REV-29-2924-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2924-PK50&section=product" },
                    ScrewLength.IN_1_5 : { "partNumber" : "REV-29-2926-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2926-PK50&section=product" },
                    ScrewLength.IN_1_75 : { "partNumber" : "REV-29-2928-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2928-PK25&section=product" },
                    ScrewLength.IN_2 : { "partNumber" : "REV-29-2930-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2930-PK25&section=product" },
                    ScrewLength.IN_2_25 : { "partNumber" : "REV-29-2931-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2931-PK25&section=product" },
                    ScrewLength.IN_2_5 : { "partNumber" : "REV-29-2932-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2932-PK25&section=product" },
                    ScrewLength.IN_2_75 : { "partNumber" : "REV-29-2933-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2933-PK25&section=product" },
                    ScrewLength.IN_3 : { "partNumber" : "REV-29-2934-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2934-PK25&section=product" },
                    ScrewLength.IN_3_5 : { "partNumber" : "REV-29-2936-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2936-PK25&section=product" },
                    ScrewLength.IN_4 : { "partNumber" : "REV-29-2938-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2938-PK25&section=product" }
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_25 : { "partNumber" : "REV-29-2939-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2939-PK50&section=product" },
                    ScrewLength.IN_0_3125 : { "partNumber" : "REV-29-2940-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2940-PK50&section=product" },
                    ScrewLength.IN_0_375 : { "partNumber" : "REV-29-2941-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2941-PK50&section=product" },
                    ScrewLength.IN_0_5 : { "partNumber" : "REV-29-2943-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2943-PK50&section=product" },
                    ScrewLength.IN_0_625 : { "partNumber" : "REV-29-2945-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2945-PK50&section=product" },
                    ScrewLength.IN_0_75 : { "partNumber" : "REV-29-2947-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2947-PK50&section=product" },
                    ScrewLength.IN_1 : { "partNumber" : "REV-29-2951-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2951-PK50&section=product" },
                    ScrewLength.IN_1_25 : { "partNumber" : "REV-29-2953-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2953-PK50&section=product" },
                    ScrewLength.IN_1_5 : { "partNumber" : "REV-29-2955-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2955-PK50&section=product" },
                    ScrewLength.IN_1_625 : { "partNumber" : "REV-29-2956-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2956-PK25&section=product" },
                    ScrewLength.IN_1_75 : { "partNumber" : "REV-29-2957-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2957-PK25&section=product" },
                    ScrewLength.IN_2 : { "partNumber" : "REV-29-2959-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2959-PK25&section=product" },
                    ScrewLength.IN_2_25 : { "partNumber" : "REV-29-2960-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2960-PK25&section=product" },
                    ScrewLength.IN_2_5 : { "partNumber" : "REV-29-2961-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2961-PK25&section=product" },
                    ScrewLength.IN_2_75 : { "partNumber" : "REV-29-2962-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2962-PK25&section=product" },
                    ScrewLength.IN_3 : { "partNumber" : "REV-29-2963-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2963-PK25&section=product" }
                }
            }
        },
        FastenerVendor.WCP : {
            FastenerSize.NO_4_40 : {
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_25 : { "partNumber" : "WCP-2354", "url" : "https://wcproducts.com/products/wcp-2354" },
                    ScrewLength.IN_0_375 : { "partNumber" : "WCP-2355", "url" : "https://wcproducts.com/products/wcp-2355" },
                    ScrewLength.IN_0_75 : { "partNumber" : "WCP-2356", "url" : "https://wcproducts.com/products/wcp-2356" }
                }
            },
            FastenerSize.NO_6_32 : {
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_25 : { "partNumber" : "WCP-2353", "url" : "https://wcproducts.com/products/wcp-2353" }
                }
            },
            FastenerSize.NO_8_32 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_1 : { "partNumber" : "WCP-2249", "url" : "https://wcproducts.com/products/wcp-2249" }
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_25 : { "partNumber" : "WCP-1548", "url" : "https://wcproducts.com/products/wcp-1548" },
                    ScrewLength.IN_0_5 : { "partNumber" : "WCP-0250", "url" : "https://wcproducts.com/products/wcp-0250" }
                }
            },
            FastenerSize.NO_10_32 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_25 : { "partNumber" : "WCP-0264", "url" : "https://wcproducts.com/products/wcp-0264" },
                    ScrewLength.IN_0_375 : { "partNumber" : "WCP-0265", "url" : "https://wcproducts.com/products/wcp-0265" },
                    ScrewLength.IN_0_5 : { "partNumber" : "WCP-0266", "url" : "https://wcproducts.com/products/wcp-0266" },
                    ScrewLength.IN_1 : { "partNumber" : "WCP-0267", "url" : "https://wcproducts.com/products/wcp-0267" },
                    ScrewLength.IN_1_5 : { "partNumber" : "WCP-0268", "url" : "https://wcproducts.com/products/wcp-0268" },
                    ScrewLength.IN_1_75 : { "partNumber" : "WCP-1549", "url" : "https://wcproducts.com/products/wcp-1549" },
                    ScrewLength.IN_2 : { "partNumber" : "WCP-0269", "url" : "https://wcproducts.com/products/wcp-0269" },
                    ScrewLength.IN_2_5 : { "partNumber" : "WCP-0270", "url" : "https://wcproducts.com/products/wcp-0270" },
                    ScrewLength.IN_3 : { "partNumber" : "WCP-0271", "url" : "https://wcproducts.com/products/wcp-0271" },
                    ScrewLength.IN_3_5 : { "partNumber" : "WCP-0272", "url" : "https://wcproducts.com/products/wcp-0272" },
                    ScrewLength.IN_4 : { "partNumber" : "WCP-0273", "url" : "https://wcproducts.com/products/wcp-0273" },
                    ScrewLength.IN_4_5 : { "partNumber" : "WCP-0274", "url" : "https://wcproducts.com/products/wcp-0274" },
                    ScrewLength.IN_5 : { "partNumber" : "WCP-0275", "url" : "https://wcproducts.com/products/wcp-0275" }
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_25 : { "partNumber" : "WCP-0251", "url" : "https://wcproducts.com/products/wcp-0251" },
                    ScrewLength.IN_0_375 : { "partNumber" : "WCP-0252", "url" : "https://wcproducts.com/products/wcp-0252" },
                    ScrewLength.IN_0_5 : { "partNumber" : "WCP-0253", "url" : "https://wcproducts.com/products/wcp-0253" },
                    ScrewLength.IN_0_75 : { "partNumber" : "WCP-0135", "url" : "https://wcproducts.com/products/wcp-0135" },
                    ScrewLength.IN_0_875 : { "partNumber" : "WCP-1463", "url" : "https://wcproducts.com/products/wcp-1463" },
                    ScrewLength.IN_1 : { "partNumber" : "WCP-0255", "url" : "https://wcproducts.com/products/wcp-0255" },
                    ScrewLength.IN_1_25 : { "partNumber" : "WCP-0256", "url" : "https://wcproducts.com/products/wcp-0256" },
                    ScrewLength.IN_1_5 : { "partNumber" : "WCP-0257", "url" : "https://wcproducts.com/products/wcp-0257" },
                    ScrewLength.IN_1_75 : { "partNumber" : "WCP-0258", "url" : "https://wcproducts.com/products/wcp-0258" },
                    ScrewLength.IN_2 : { "partNumber" : "WCP-0259", "url" : "https://wcproducts.com/products/wcp-0259" },
                    ScrewLength.IN_2_25 : { "partNumber" : "WCP-0260", "url" : "https://wcproducts.com/products/wcp-0260" },
                    ScrewLength.IN_2_5 : { "partNumber" : "WCP-0261", "url" : "https://wcproducts.com/products/wcp-0261" },
                    ScrewLength.IN_2_75 : { "partNumber" : "WCP-0262", "url" : "https://wcproducts.com/products/wcp-0262" },
                    ScrewLength.IN_3 : { "partNumber" : "WCP-0263", "url" : "https://wcproducts.com/products/wcp-0263" },
                    ScrewLength.IN_4_75 : { "partNumber" : "WCP-1656", "url" : "https://wcproducts.com/products/wcp-1656" }
                }
            }
        },
        FastenerVendor.ANDYMARK : {
            FastenerSize.NO_4_40 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_375 : { "partNumber" : "am-1032", "url" : "https://andymark.com/products/4-40-x-0-375-in-socket-head-cap-screw-1" },
                    ScrewLength.IN_0_5 : { "partNumber" : "am-1136", "url" : "https://andymark.com/products/4-40-x-0-5-in-socket-head-cap-screw" }
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_25 : { "partNumber" : "am-1679", "url" : "https://andymark.com/products/4-40-x-0-25-in-button-head-cap-screw-with-nylon-patch" }
                },
                ScrewHead.FLAT : {
                    ScrewLength.IN_0_375 : { "partNumber" : "am-1586", "url" : "https://andymark.com/products/4-40-x-0-375-in-flat-head-cap-screw-with-nylon-patch" }
                }
            },
            FastenerSize.NO_6_32 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_2_125 : { "partNumber" : "am-1657", "url" : "https://andymark.com/products/6-32-x-2-125-in-socket-head-cap-screw" }
                },
                ScrewHead.FLAT : {
                    ScrewLength.IN_0_625 : { "partNumber" : "am-1570", "url" : "https://andymark.com/products/6-32-x-0-625-in-flat-head-socket-cap-screw" }
                }
            },
            FastenerSize.NO_8_32 : {
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_5 : { "partNumber" : "am-1691", "url" : "https://andymark.com/products/8-32-x-0-5-in-button-head-cap-screw-with-nylon-patch" }
                }
            },
            FastenerSize.NO_10_32 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_25 : { "partNumber" : "am-1771", "url" : "https://andymark.com/products/10-32-x-250-in-socket-head-cap-screw-with-nylon-thread-lock-patch" },
                    ScrewLength.IN_0_375 : { "partNumber" : "am-1359", "url" : "https://andymark.com/products/10-32-x-0-375-in-socket-head-cap-screw-1" },
                    ScrewLength.IN_0_5 : { "partNumber" : "am-1002", "url" : "https://andymark.com/products/10-32-x-0-5-in-socket-head-cap-screw-1" },
                    ScrewLength.IN_0_625 : { "partNumber" : "am-1007", "url" : "https://andymark.com/products/10-32-x-0-625-in-socket-head-cap-screw-1" },
                    ScrewLength.IN_0_75 : { "partNumber" : "am-1047", "url" : "https://andymark.com/products/10-32-x-0-75-in-socket-head-cap-screw" },
                    ScrewLength.IN_0_875 : { "partNumber" : "am-1753", "url" : "https://andymark.com/products/10-32-x-0-875-in-socket-head-cap-screw" },
                    ScrewLength.IN_1 : { "partNumber" : "am-1056", "url" : "https://andymark.com/products/10-32-x-1-in-socket-head-cap-screw-1" },
                    ScrewLength.IN_1_125 : { "partNumber" : "am-1013", "url" : "https://andymark.com/products/10-32-x-1-125-in-socket-head-cap-screw-1" },
                    ScrewLength.IN_1_25 : { "partNumber" : "am-1041", "url" : "https://andymark.com/products/10-32-x-1-25-in-socket-head-cap-screw-1" },
                    ScrewLength.IN_1_375 : { "partNumber" : "am-1154", "url" : "https://andymark.com/products/10-32-x-1-375-in-socket-head-cap-screw" },
                    ScrewLength.IN_1_5 : { "partNumber" : "am-1014", "url" : "https://andymark.com/products/10-32-x-1-5-in-socket-head-cap-screw" },
                    ScrewLength.IN_1_75 : { "partNumber" : "am-1048", "url" : "https://andymark.com/products/10-32-x-1-75-in-socket-head-cap-screw-1" },
                    ScrewLength.IN_2 : { "partNumber" : "am-1049", "url" : "https://andymark.com/products/10-32-x-2-in-socket-head-cap-screw-1" },
                    ScrewLength.IN_2_25 : { "partNumber" : "am-1156", "url" : "https://andymark.com/products/10-32-x-2-25-in-socket-head-cap-screw-2" },
                    ScrewLength.IN_2_5 : { "partNumber" : "am-1024", "url" : "https://andymark.com/products/10-32-x-2-5-in-socket-head-cap-screw-1" },
                    ScrewLength.IN_2_75 : { "partNumber" : "am-1397", "url" : "https://andymark.com/products/10-32-x-2-75-in-socket-head-cap-screw" },
                    ScrewLength.IN_3 : { "partNumber" : "am-1522", "url" : "https://andymark.com/products/10-32-x-3-in-socket-head-cap-screw" },
                    ScrewLength.IN_3_25 : { "partNumber" : "am-1258", "url" : "https://andymark.com/products/10-32-x-3-25-in-socket-head-cap-screw-1" },
                    ScrewLength.IN_3_5 : { "partNumber" : "am-1402", "url" : "https://andymark.com/products/10-32-x-3-5-in-socket-head-cap-screw" }
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_375 : { "partNumber" : "am-1588", "url" : "https://andymark.com/products/10-32-x-3-8-in-button-head-cap-screw-with-nylon-patch" },
                    ScrewLength.IN_0_5 : { "partNumber" : "am-1512", "url" : "https://andymark.com/products/10-32-x-0-5-in-button-head-cap-screw" },
                    ScrewLength.IN_0_625 : { "partNumber" : "am-1549", "url" : "https://andymark.com/products/10-32-x-0-625-in-button-head-cap-screw-with-nylon-patch" },
                    ScrewLength.IN_0_75 : { "partNumber" : "am-1524", "url" : "https://andymark.com/products/10-32-x-0-75-in-hex-drive-button-head-cap-screw" },
                    ScrewLength.IN_1 : { "partNumber" : "am-1396", "url" : "https://andymark.com/products/10-32-x-1-in-button-head-cap-screw-1" },
                    ScrewLength.IN_1_25 : { "partNumber" : "am-1260", "url" : "https://andymark.com/products/10-32-x-1-25-in-button-head-cap-screw" },
                    ScrewLength.IN_1_5 : { "partNumber" : "am-1730", "url" : "https://andymark.com/products/10-32-x-1-5-in-button-head-cap-screw" },
                    ScrewLength.IN_1_75 : { "partNumber" : "am-1584", "url" : "https://andymark.com/products/10-32-x-1-75-in-button-head-cap-screw" }
                },
                ScrewHead.FLAT : {
                    ScrewLength.IN_0_5 : { "partNumber" : "am-1404", "url" : "https://andymark.com/products/10-32-x-0-5-in-flat-head-cap-screw" },
                    ScrewLength.IN_0_625 : { "partNumber" : "am-1252", "url" : "https://andymark.com/products/10-32-x-0-625-in-flat-head-cap-screw" },
                    ScrewLength.IN_0_75 : { "partNumber" : "am-1170", "url" : "https://andymark.com/products/10-32-x-0-75-in-flat-head-cap-screw" },
                    ScrewLength.IN_1 : { "partNumber" : "am-1324", "url" : "https://andymark.com/products/10-32-x-1-in-flat-head-cap-screw-1" },
                    ScrewLength.IN_1_5 : { "partNumber" : "am-1756", "url" : "https://andymark.com/products/10-32-x-1-500-in-flat-head-cap-screw" },
                    ScrewLength.IN_2 : { "partNumber" : "am-1383", "url" : "https://andymark.com/products/10-32-x-2-in-flat-head-cap-screw-black-oxide" }
                }
            },
            FastenerSize.QUARTER_20 : {
                ScrewHead.SOCKET : {
                    ScrewLength.IN_0_625 : { "partNumber" : "am-1053", "url" : "https://andymark.com/products/1-4-20-x-0-625-in-hex-drive-socket-head-cap-screw" },
                    ScrewLength.IN_1 : { "partNumber" : "am-1341", "url" : "https://andymark.com/products/1-4-20-x-1-in-socket-head-cap-screw" },
                    ScrewLength.IN_1_5 : { "partNumber" : "am-1131", "url" : "https://andymark.com/products/1-4-20-x-1-5-in-socket-head-cap-screw" },
                    ScrewLength.IN_1_75 : { "partNumber" : "am-1058", "url" : "https://andymark.com/products/1-4-20-x-1-75-in-hex-drive-socket-head-cap-screw" },
                    ScrewLength.IN_2 : { "partNumber" : "am-1012", "url" : "https://andymark.com/products/1-4-20-x-2-in-socket-head-cap-screw" },
                    ScrewLength.IN_2_5 : { "partNumber" : "am-1031", "url" : "https://andymark.com/products/1-4-20-x-2-5-in-hex-drive-socket-head-cap-screw" },
                    ScrewLength.IN_3 : { "partNumber" : "am-1030", "url" : "https://andymark.com/products/1-4-20-x-3-in-socket-head-cap-screw-1" },
                    ScrewLength.IN_3_25 : { "partNumber" : "am-3338", "url" : "https://andymark.com/products/1-4-20-x-3-25-in-socket-head-cap-screw" }
                },
                ScrewHead.BUTTON : {
                    ScrewLength.IN_0_375 : { "partNumber" : "am-1520", "url" : "https://andymark.com/products/1-4-20-x-0-375-in-button-head-cap-screw" },
                    ScrewLength.IN_0_5 : { "partNumber" : "am-1039", "url" : "https://andymark.com/products/1-4-20-x-0-5-in-button-head-cap-screw" },
                    ScrewLength.IN_0_625 : { "partNumber" : "am-1681", "url" : "https://andymark.com/products/1-4-20-x-0-625-in-button-head-cap-screw" },
                    ScrewLength.IN_0_75 : { "partNumber" : "am-1754", "url" : "https://andymark.com/products/screw-bhcs-1-4-20-x-750-zinc" },
                    ScrewLength.IN_1 : { "partNumber" : "am-1525", "url" : "https://andymark.com/products/1-4-20-x-1-in-button-head-cap-screw" },
                    ScrewLength.IN_1_25 : { "partNumber" : "am-1183", "url" : "https://andymark.com/products/1-4-20-x-1-25-in-button-head-cap-screw" },
                    ScrewLength.IN_1_5 : { "partNumber" : "am-1118", "url" : "https://andymark.com/products/1-4-20-x-1-5-in-button-head-cap-screw" },
                    ScrewLength.IN_1_75 : { "partNumber" : "am-1420", "url" : "https://andymark.com/products/1-4-20-x-1-75-in-button-head-cap-screw" },
                    ScrewLength.IN_2 : { "partNumber" : "am-1655", "url" : "https://andymark.com/products/1-4-20-x-2-in-button-head-cap-screw" },
                    ScrewLength.IN_2_5 : { "partNumber" : "am-1695", "url" : "https://andymark.com/products/1-4-20-x-2-5-in-button-head-cap-screw" }
                },
                ScrewHead.FLAT : {
                    ScrewLength.IN_0_75 : { "partNumber" : "am-1672", "url" : "https://andymark.com/products/1-4-20-x-0-75-in-flat-head-cap-screw" },
                    ScrewLength.IN_2 : { "partNumber" : "am-1707", "url" : "https://andymark.com/products/1-4-20-x-2-in-flat-head-cap-screw" }
                }
            }
        }
    };

export const NUTS = {
            FastenerVendor.GENERIC : {
                FastenerSize.NO_4_40 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.25 * inch, "height" : 0.141 * inch },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.25 * inch, "height" : 0.109 * inch },
                    NutType.HEX : { "acrossFlats" : 0.25 * inch, "height" : 0.094 * inch },
                    NutType.THIN_HEX : { "acrossFlats" : 0.25 * inch, "height" : 0.063 * inch }
                },
                FastenerSize.NO_6_32 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.3125 * inch, "height" : 0.172 * inch },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.3125 * inch, "height" : 0.125 * inch },
                    NutType.HEX : { "acrossFlats" : 0.3125 * inch, "height" : 0.109 * inch },
                    NutType.THIN_HEX : { "acrossFlats" : 0.3125 * inch, "height" : 0.078 * inch }
                },
                FastenerSize.NO_8_32 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.34375 * inch, "height" : 0.203 * inch },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.34375 * inch, "height" : 0.141 * inch },
                    NutType.HEX : { "acrossFlats" : 0.34375 * inch, "height" : 0.125 * inch },
                    NutType.SQUARE : { "acrossFlats" : 0.34375 * inch, "height" : 0.125 * inch }
                },
                FastenerSize.NO_10_32 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.375 * inch, "height" : 0.236 * inch },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.375 * inch, "height" : 0.156 * inch },
                    NutType.HEX : { "acrossFlats" : 0.375 * inch, "height" : 0.125 * inch },
                    NutType.SQUARE : { "acrossFlats" : 0.375 * inch, "height" : 0.125 * inch }
                },
                FastenerSize.QUARTER_20 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.4375 * inch, "height" : 0.312 * inch },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.4375 * inch, "height" : 0.203 * inch },
                    NutType.HEX : { "acrossFlats" : 0.4375 * inch, "height" : 0.219 * inch },
                    NutType.SQUARE : { "acrossFlats" : 0.4375 * inch, "height" : 0.219 * inch }
                }
            },
            FastenerVendor.MCMASTER : {
                FastenerSize.NO_4_40 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.25 * inch, "height" : 0.141 * inch, "partNumber" : "90631A005", "url" : "https://www.mcmaster.com/90631A005/" },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.25 * inch, "height" : 0.109 * inch, "partNumber" : "90633A005", "url" : "https://www.mcmaster.com/90633A005/" },
                    NutType.HEX : { "acrossFlats" : 0.25 * inch, "height" : 0.094 * inch, "partNumber" : "90480A005", "url" : "https://www.mcmaster.com/90480A005/" },
                    NutType.THIN_HEX : { "acrossFlats" : 0.25 * inch, "height" : 0.063 * inch, "partNumber" : "90760A005", "url" : "https://www.mcmaster.com/90760A005/" }
                },
                FastenerSize.NO_6_32 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.3125 * inch, "height" : 0.172 * inch, "partNumber" : "90631A007" },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.3125 * inch, "height" : 0.125 * inch, "partNumber" : "90633A007" },
                    NutType.HEX : { "acrossFlats" : 0.3125 * inch, "height" : 0.109 * inch, "partNumber" : "90480A007" },
                    NutType.THIN_HEX : { "acrossFlats" : 0.3125 * inch, "height" : 0.078 * inch, "partNumber" : "90760A007" }
                },
                FastenerSize.NO_8_32 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.34375 * inch, "height" : 0.203 * inch, "partNumber" : "90631A009", "url" : "https://www.mcmaster.com/90631A009/" },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.34375 * inch, "height" : 0.141 * inch, "partNumber" : "90633A009", "url" : "https://www.mcmaster.com/90633A009/" },
                    NutType.HEX : { "acrossFlats" : 0.34375 * inch, "height" : 0.125 * inch, "partNumber" : "90480A009", "url" : "https://www.mcmaster.com/90480A009/" },
                    NutType.SQUARE : { "acrossFlats" : 0.34375 * inch, "height" : 0.125 * inch, "partNumber" : "94855A216", "url" : "https://www.mcmaster.com/94855A216/" }
                },
                FastenerSize.NO_10_32 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.375 * inch, "height" : 0.236 * inch, "partNumber" : "90631A411", "url" : "https://www.mcmaster.com/90631A411/" },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.375 * inch, "height" : 0.156 * inch, "partNumber" : "90633A411", "url" : "https://www.mcmaster.com/90633A411/" },
                    NutType.HEX : { "acrossFlats" : 0.375 * inch, "height" : 0.125 * inch, "partNumber" : "90480A195", "url" : "https://www.mcmaster.com/90480A195/" },
                    NutType.SQUARE : { "acrossFlats" : 0.375 * inch, "height" : 0.125 * inch, "partNumber" : "94855A283", "url" : "https://www.mcmaster.com/94855A283/" }
                },
                FastenerSize.QUARTER_20 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.4375 * inch, "height" : 0.312 * inch, "partNumber" : "90640A129", "url" : "https://www.mcmaster.com/90640A129/" },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.4375 * inch, "height" : 0.203 * inch, "partNumber" : "90566A029", "url" : "https://www.mcmaster.com/90566A029/" },
                    NutType.HEX : { "acrossFlats" : 0.4375 * inch, "height" : 0.219 * inch, "partNumber" : "90494A029", "url" : "https://www.mcmaster.com/90494A029/" },
                    NutType.SQUARE : { "acrossFlats" : 0.4375 * inch, "height" : 0.219 * inch, "partNumber" : "90043A029", "url" : "https://www.mcmaster.com/90043A029/" }
                }
            }
        };

export const WASHERS = {
            FastenerVendor.GENERIC : {
                FastenerSize.NO_4_40 : {
                    WasherType.OD_5_16_X_0_032 : { "innerDiameter" : 0.125 * inch, "outerDiameter" : 0.3125 * inch, "thickness" : 0.032 * inch }
                },
                FastenerSize.NO_6_32 : {
                    WasherType.OD_3_8_X_0_049 : { "innerDiameter" : 0.156 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.049 * inch }
                },
                FastenerSize.NO_8_32 : {
                    WasherType.OD_7_16_X_0_049 : { "innerDiameter" : 0.188 * inch, "outerDiameter" : 0.4375 * inch, "thickness" : 0.049 * inch }
                },
                FastenerSize.NO_10_32 : {
                    WasherType.OD_1_2_X_0_049 : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.5 * inch, "thickness" : 0.049 * inch }
                },
                FastenerSize.QUARTER_20 : {
                    WasherType.OD_5_8_X_0_065 : { "innerDiameter" : 0.281 * inch, "outerDiameter" : 0.625 * inch, "thickness" : 0.065 * inch }
                }
            },
            FastenerVendor.MCMASTER : {
                FastenerSize.NO_4_40 : {
                    WasherType.OD_5_16_X_0_03 : { "innerDiameter" : 0.125 * inch, "outerDiameter" : 0.3125 * inch, "thickness" : 0.03 * inch, "partNumber" : "96765A110", "url" : "https://www.mcmaster.com/96765A110/" }
                },
                FastenerSize.NO_6_32 : {
                    WasherType.OD_3_8_X_0_03 : { "innerDiameter" : 0.156 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.03 * inch, "partNumber" : "96765A115", "url" : "https://www.mcmaster.com/96765A115/" }
                },
                FastenerSize.NO_8_32 : {
                    WasherType.OD_3_8_X_0_03 : { "innerDiameter" : 0.188 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.03 * inch, "partNumber" : "96765A120", "url" : "https://www.mcmaster.com/96765A120/" }
                },
                FastenerSize.NO_10_32 : {
                    WasherType.OD_7_16_X_0_03 : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.4375 * inch, "thickness" : 0.03 * inch, "partNumber" : "96765A125", "url" : "https://www.mcmaster.com/96765A125/" },
                    WasherType.OD_1_2_X_0_04 : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.5 * inch, "thickness" : 0.04 * inch, "partNumber" : "90377A123", "url" : "https://www.mcmaster.com/90377A123/" }
                },
                FastenerSize.QUARTER_20 : {
                    WasherType.OD_5_8_X_0_05 : { "innerDiameter" : 0.281 * inch, "outerDiameter" : 0.625 * inch, "thickness" : 0.05 * inch, "partNumber" : "96765A140", "url" : "https://www.mcmaster.com/96765A140/" }
                }
            },
            FastenerVendor.WCP : {
                FastenerSize.NO_10_32 : {
                    WasherType.OD_3_8_X_0_012 : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.012 * inch, "partNumber" : "WCP-0038", "url" : "https://wcproducts.com/products/wcp-0038" },
                    WasherType.OD_3_8_X_0_031 : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.031 * inch, "partNumber" : "WCP-0197", "url" : "https://wcproducts.com/products/wcp-0197" },
                    WasherType.OD_5_8_X_0_031 : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.625 * inch, "thickness" : 0.031 * inch, "partNumber" : "WCP-0523", "url" : "https://wcproducts.com/products/wcp-0523" }
                },
                FastenerSize.QUARTER_20 : {
                    WasherType.OD_3_8_X_0_012 : { "innerDiameter" : 0.281 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.012 * inch, "partNumber" : "WCP-0039", "url" : "https://wcproducts.com/products/wcp-0039" },
                    WasherType.OD_3_8_X_0_031 : { "innerDiameter" : 0.281 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.031 * inch, "partNumber" : "WCP-0198", "url" : "https://wcproducts.com/products/wcp-0198" }
                }
            }
        };

export const WASHER_TYPE_NAMES = {
            WasherType.OD_5_16_X_0_03 : "5/16 in. OD x 0.03 in.",
            WasherType.OD_5_16_X_0_032 : "5/16 in. OD x 0.032 in.",
            WasherType.OD_3_8_X_0_012 : "3/8 in. OD x 0.012 in.",
            WasherType.OD_3_8_X_0_03 : "3/8 in. OD x 0.03 in.",
            WasherType.OD_3_8_X_0_031 : "3/8 in. OD x 0.031 in.",
            WasherType.OD_3_8_X_0_049 : "3/8 in. OD x 0.049 in.",
            WasherType.OD_7_16_X_0_03 : "7/16 in. OD x 0.03 in.",
            WasherType.OD_7_16_X_0_049 : "7/16 in. OD x 0.049 in.",
            WasherType.OD_1_2_X_0_04 : "1/2 in. OD x 0.04 in.",
            WasherType.OD_1_2_X_0_049 : "1/2 in. OD x 0.049 in.",
            WasherType.OD_5_8_X_0_031 : "5/8 in. OD x 0.031 in.",
            WasherType.OD_5_8_X_0_05 : "5/8 in. OD x 0.05 in.",
            WasherType.OD_5_8_X_0_065 : "5/8 in. OD x 0.065 in."
        };
