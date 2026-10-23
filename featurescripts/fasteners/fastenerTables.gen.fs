FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "0794d10863d10d98a88c2ab4", version : "4f09b23b6e418ecb226e90c1"); // core/robotProperties.fs

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
    annotation { "Name" : "3/32 in" }
    IN_0_09375,
    annotation { "Name" : "1/8 in" }
    IN_0_125,
    annotation { "Name" : "5/32 in" }
    IN_0_15625,
    annotation { "Name" : "3/16 in" }
    IN_0_1875,
    annotation { "Name" : "1/4 in" }
    IN_0_25,
    annotation { "Name" : "5/16 in" }
    IN_0_3125,
    annotation { "Name" : "3/8 in" }
    IN_0_375,
    annotation { "Name" : "7/16 in" }
    IN_0_4375,
    annotation { "Name" : "1/2 in" }
    IN_0_5,
    annotation { "Name" : "9/16 in" }
    IN_0_5625,
    annotation { "Name" : "5/8 in" }
    IN_0_625,
    annotation { "Name" : "11/16 in" }
    IN_0_6875,
    annotation { "Name" : "3/4 in" }
    IN_0_75,
    annotation { "Name" : "7/8 in" }
    IN_0_875,
    annotation { "Name" : "1 in" }
    IN_1,
    annotation { "Name" : "1-1/8 in" }
    IN_1_125,
    annotation { "Name" : "1-1/4 in" }
    IN_1_25,
    annotation { "Name" : "1-3/8 in" }
    IN_1_375,
    annotation { "Name" : "1-1/2 in" }
    IN_1_5,
    annotation { "Name" : "1-5/8 in" }
    IN_1_625,
    annotation { "Name" : "1-3/4 in" }
    IN_1_75,
    annotation { "Name" : "1-7/8 in" }
    IN_1_875,
    annotation { "Name" : "2 in" }
    IN_2,
    annotation { "Name" : "2-1/8 in" }
    IN_2_125,
    annotation { "Name" : "2-1/4 in" }
    IN_2_25,
    annotation { "Name" : "2-3/8 in" }
    IN_2_375,
    annotation { "Name" : "2-1/2 in" }
    IN_2_5,
    annotation { "Name" : "2-5/8 in" }
    IN_2_625,
    annotation { "Name" : "2-3/4 in" }
    IN_2_75,
    annotation { "Name" : "3 in" }
    IN_3,
    annotation { "Name" : "3-1/4 in" }
    IN_3_25,
    annotation { "Name" : "3-1/2 in" }
    IN_3_5,
    annotation { "Name" : "3-3/4 in" }
    IN_3_75,
    annotation { "Name" : "4 in" }
    IN_4,
    annotation { "Name" : "4-1/4 in" }
    IN_4_25,
    annotation { "Name" : "4-1/2 in" }
    IN_4_5,
    annotation { "Name" : "4-3/4 in" }
    IN_4_75,
    annotation { "Name" : "5 in" }
    IN_5,
    annotation { "Name" : "5-1/4 in" }
    IN_5_25,
    annotation { "Name" : "5-1/2 in" }
    IN_5_5,
    annotation { "Name" : "5-3/4 in" }
    IN_5_75,
    annotation { "Name" : "6 in" }
    IN_6,
    annotation { "Name" : "6-1/4 in" }
    IN_6_25,
    annotation { "Name" : "6-1/2 in" }
    IN_6_5,
    annotation { "Name" : "7 in" }
    IN_7,
    annotation { "Name" : "7-1/2 in" }
    IN_7_5,
    annotation { "Name" : "8 in" }
    IN_8,
    annotation { "Name" : "8-1/2 in" }
    IN_8_5,
    annotation { "Name" : "9 in" }
    IN_9,
    annotation { "Name" : "10 in" }
    IN_10
}

export enum ScrewFinish
{
    annotation { "Name" : "Black" }
    BLACK,
    annotation { "Name" : "Zinc plated" }
    ZINC_PLATED,
    annotation { "Name" : "Aluminum" }
    ALUMINUM
}

export enum WasherFinish
{
    annotation { "Name" : "Black" }
    BLACK,
    annotation { "Name" : "Zinc plated" }
    ZINC_PLATED,
    annotation { "Name" : "Stainless steel" }
    STAINLESS
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
    annotation { "Name" : "5/16 in OD x 0.03 in" }
    OD_5_16_X_0_03,
    annotation { "Name" : "5/16 in OD x 0.032 in" }
    OD_5_16_X_0_032,
    annotation { "Name" : "3/8 in OD x 0.012 in" }
    OD_3_8_X_0_012,
    annotation { "Name" : "3/8 in OD x 0.03 in" }
    OD_3_8_X_0_03,
    annotation { "Name" : "3/8 in OD x 0.031 in" }
    OD_3_8_X_0_031,
    annotation { "Name" : "3/8 in OD x 0.049 in" }
    OD_3_8_X_0_049,
    annotation { "Name" : "7/16 in OD x 0.03 in" }
    OD_7_16_X_0_03,
    annotation { "Name" : "7/16 in OD x 0.049 in" }
    OD_7_16_X_0_049,
    annotation { "Name" : "1/2 in OD x 0.04 in" }
    OD_1_2_X_0_04,
    annotation { "Name" : "1/2 in OD x 0.049 in" }
    OD_1_2_X_0_049,
    annotation { "Name" : "5/8 in OD x 0.031 in" }
    OD_5_8_X_0_031,
    annotation { "Name" : "5/8 in OD x 0.05 in" }
    OD_5_8_X_0_05,
    annotation { "Name" : "5/8 in OD x 0.063 in" }
    OD_5_8_X_0_063,
    annotation { "Name" : "5/8 in OD x 0.065 in" }
    OD_5_8_X_0_065,
    annotation { "Name" : "3/4 in OD x 0.062 in" }
    OD_3_4_X_0_062,
    annotation { "Name" : "1 in OD x 0.062 in" }
    OD_1_X_0_062,
    annotation { "Name" : "3 in OD x 0.06 in" }
    OD_3_X_0_06
}

export const FASTENER_SIZES = {
            FastenerSize.NO_4_40 : { "name" : "#4-40", "size" : "#4", "pitch" : "40 tpi", "threadPitch" : inch / 40, "majorDiameter" : 0.112 * inch, "minorDiameter" : 0.0814 * inch, "tapDrillDiameter" : 0.089 * inch, "closeClearanceDiameter" : 0.116 * inch, "freeClearanceDiameter" : 0.1285 * inch },
            FastenerSize.NO_6_32 : { "name" : "#6-32", "size" : "#6", "pitch" : "32 tpi", "threadPitch" : inch / 32, "majorDiameter" : 0.138 * inch, "minorDiameter" : 0.1 * inch, "tapDrillDiameter" : 0.1065 * inch, "closeClearanceDiameter" : 0.144 * inch, "freeClearanceDiameter" : 0.1495 * inch },
            FastenerSize.NO_8_32 : { "name" : "#8-32", "size" : "#8", "pitch" : "32 tpi", "threadPitch" : inch / 32, "majorDiameter" : 0.164 * inch, "minorDiameter" : 0.1259 * inch, "tapDrillDiameter" : 0.136 * inch, "closeClearanceDiameter" : 0.1695 * inch, "freeClearanceDiameter" : 0.177 * inch },
            FastenerSize.NO_10_32 : { "name" : "#10-32", "size" : "#10", "pitch" : "32 tpi", "threadPitch" : inch / 32, "majorDiameter" : 0.19 * inch, "minorDiameter" : 0.1528 * inch, "tapDrillDiameter" : 0.159 * inch, "closeClearanceDiameter" : 0.196 * inch, "freeClearanceDiameter" : 0.201 * inch },
            FastenerSize.QUARTER_20 : { "name" : "1/4-20", "size" : "1/4", "pitch" : "20 tpi", "threadPitch" : inch / 20, "majorDiameter" : 0.25 * inch, "minorDiameter" : 0.1894 * inch, "tapDrillDiameter" : 0.201 * inch, "closeClearanceDiameter" : 0.257 * inch, "freeClearanceDiameter" : 0.266 * inch }
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
            ScrewLength.IN_0_09375 : { "length" : 0.09375 * inch, "name" : "3/32 in" },
            ScrewLength.IN_0_125 : { "length" : 0.125 * inch, "name" : "1/8 in" },
            ScrewLength.IN_0_15625 : { "length" : 0.15625 * inch, "name" : "5/32 in" },
            ScrewLength.IN_0_1875 : { "length" : 0.1875 * inch, "name" : "3/16 in" },
            ScrewLength.IN_0_25 : { "length" : 0.25 * inch, "name" : "1/4 in" },
            ScrewLength.IN_0_3125 : { "length" : 0.3125 * inch, "name" : "5/16 in" },
            ScrewLength.IN_0_375 : { "length" : 0.375 * inch, "name" : "3/8 in" },
            ScrewLength.IN_0_4375 : { "length" : 0.4375 * inch, "name" : "7/16 in" },
            ScrewLength.IN_0_5 : { "length" : 0.5 * inch, "name" : "1/2 in" },
            ScrewLength.IN_0_5625 : { "length" : 0.5625 * inch, "name" : "9/16 in" },
            ScrewLength.IN_0_625 : { "length" : 0.625 * inch, "name" : "5/8 in" },
            ScrewLength.IN_0_6875 : { "length" : 0.6875 * inch, "name" : "11/16 in" },
            ScrewLength.IN_0_75 : { "length" : 0.75 * inch, "name" : "3/4 in" },
            ScrewLength.IN_0_875 : { "length" : 0.875 * inch, "name" : "7/8 in" },
            ScrewLength.IN_1 : { "length" : 1 * inch, "name" : "1 in" },
            ScrewLength.IN_1_125 : { "length" : 1.125 * inch, "name" : "1-1/8 in" },
            ScrewLength.IN_1_25 : { "length" : 1.25 * inch, "name" : "1-1/4 in" },
            ScrewLength.IN_1_375 : { "length" : 1.375 * inch, "name" : "1-3/8 in" },
            ScrewLength.IN_1_5 : { "length" : 1.5 * inch, "name" : "1-1/2 in" },
            ScrewLength.IN_1_625 : { "length" : 1.625 * inch, "name" : "1-5/8 in" },
            ScrewLength.IN_1_75 : { "length" : 1.75 * inch, "name" : "1-3/4 in" },
            ScrewLength.IN_1_875 : { "length" : 1.875 * inch, "name" : "1-7/8 in" },
            ScrewLength.IN_2 : { "length" : 2 * inch, "name" : "2 in" },
            ScrewLength.IN_2_125 : { "length" : 2.125 * inch, "name" : "2-1/8 in" },
            ScrewLength.IN_2_25 : { "length" : 2.25 * inch, "name" : "2-1/4 in" },
            ScrewLength.IN_2_375 : { "length" : 2.375 * inch, "name" : "2-3/8 in" },
            ScrewLength.IN_2_5 : { "length" : 2.5 * inch, "name" : "2-1/2 in" },
            ScrewLength.IN_2_625 : { "length" : 2.625 * inch, "name" : "2-5/8 in" },
            ScrewLength.IN_2_75 : { "length" : 2.75 * inch, "name" : "2-3/4 in" },
            ScrewLength.IN_3 : { "length" : 3 * inch, "name" : "3 in" },
            ScrewLength.IN_3_25 : { "length" : 3.25 * inch, "name" : "3-1/4 in" },
            ScrewLength.IN_3_5 : { "length" : 3.5 * inch, "name" : "3-1/2 in" },
            ScrewLength.IN_3_75 : { "length" : 3.75 * inch, "name" : "3-3/4 in" },
            ScrewLength.IN_4 : { "length" : 4 * inch, "name" : "4 in" },
            ScrewLength.IN_4_25 : { "length" : 4.25 * inch, "name" : "4-1/4 in" },
            ScrewLength.IN_4_5 : { "length" : 4.5 * inch, "name" : "4-1/2 in" },
            ScrewLength.IN_4_75 : { "length" : 4.75 * inch, "name" : "4-3/4 in" },
            ScrewLength.IN_5 : { "length" : 5 * inch, "name" : "5 in" },
            ScrewLength.IN_5_25 : { "length" : 5.25 * inch, "name" : "5-1/4 in" },
            ScrewLength.IN_5_5 : { "length" : 5.5 * inch, "name" : "5-1/2 in" },
            ScrewLength.IN_5_75 : { "length" : 5.75 * inch, "name" : "5-3/4 in" },
            ScrewLength.IN_6 : { "length" : 6 * inch, "name" : "6 in" },
            ScrewLength.IN_6_25 : { "length" : 6.25 * inch, "name" : "6-1/4 in" },
            ScrewLength.IN_6_5 : { "length" : 6.5 * inch, "name" : "6-1/2 in" },
            ScrewLength.IN_7 : { "length" : 7 * inch, "name" : "7 in" },
            ScrewLength.IN_7_5 : { "length" : 7.5 * inch, "name" : "7-1/2 in" },
            ScrewLength.IN_8 : { "length" : 8 * inch, "name" : "8 in" },
            ScrewLength.IN_8_5 : { "length" : 8.5 * inch, "name" : "8-1/2 in" },
            ScrewLength.IN_9 : { "length" : 9 * inch, "name" : "9 in" },
            ScrewLength.IN_10 : { "length" : 10 * inch, "name" : "10 in" }
        };

/** What each vendor makes, by vendor, size, head, finish, and length: each screw's properties, partly threaded (`standard`) and, where it's sold so too, fully threaded (`fullThread`). */
export const SCREWS = {
        FastenerVendor.GENERIC : {
            FastenerSize.NO_4_40 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_09375 : { "standard" : { "name" : "#4-40 x 3/32 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#4-40 x 1/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#4-40 x 5/32 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#4-40 x 3/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#4-40 x 1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#4-40 x 5/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#4-40 x 3/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#4-40 x 7/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#4-40 x 1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#4-40 x 9/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#4-40 x 5/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#4-40 x 3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#4-40 x 7/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#4-40 x 1 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#4-40 x 1-1/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#4-40 x 1-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#4-40 x 1-3/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#4-40 x 1-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#4-40 x 1-5/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#4-40 x 1-3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#4-40 x 2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#4-40 x 2-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#4-40 x 2-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "#4-40 x 2-3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#4-40 x 3 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "#4-40 x 3-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "#4-40 x 4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#4-40 x 1/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#4-40 x 3/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#4-40 x 1/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#4-40 x 5/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#4-40 x 3/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#4-40 x 7/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#4-40 x 1/2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#4-40 x 9/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#4-40 x 5/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#4-40 x 3/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#4-40 x 7/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#4-40 x 1 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#4-40 x 1-1/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#4-40 x 1-1/2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#4-40 x 2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#4-40 x 1/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#4-40 x 5/32 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#4-40 x 3/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#4-40 x 1/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#4-40 x 5/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#4-40 x 3/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#4-40 x 7/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#4-40 x 1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#4-40 x 9/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#4-40 x 5/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#4-40 x 3/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#4-40 x 7/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#4-40 x 1 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#4-40 x 1-1/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#4-40 x 1-1/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#4-40 x 1-1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                }
            },
            FastenerSize.NO_6_32 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_09375 : { "standard" : { "name" : "#6-32 x 3/32 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#6-32 x 1/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#6-32 x 5/32 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#6-32 x 3/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#6-32 x 1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#6-32 x 5/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#6-32 x 3/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#6-32 x 7/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#6-32 x 1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#6-32 x 9/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#6-32 x 5/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#6-32 x 3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#6-32 x 7/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#6-32 x 1 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#6-32 x 1-1/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#6-32 x 1-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#6-32 x 1-3/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#6-32 x 1-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#6-32 x 1-5/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#6-32 x 1-3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_875 : { "standard" : { "name" : "#6-32 x 1-7/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#6-32 x 2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_125 : { "standard" : { "name" : "#6-32 x 2-1/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#6-32 x 2-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#6-32 x 2-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "#6-32 x 2-3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#6-32 x 3 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "#6-32 x 3-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#6-32 x 1/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#6-32 x 3/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#6-32 x 1/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#6-32 x 5/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#6-32 x 3/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#6-32 x 7/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#6-32 x 1/2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#6-32 x 9/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#6-32 x 5/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#6-32 x 3/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#6-32 x 7/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#6-32 x 1 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#6-32 x 1-1/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#6-32 x 1-1/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#6-32 x 1/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#6-32 x 5/32 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#6-32 x 3/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#6-32 x 1/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#6-32 x 5/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#6-32 x 3/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#6-32 x 7/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#6-32 x 1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#6-32 x 9/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#6-32 x 5/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#6-32 x 3/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#6-32 x 7/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#6-32 x 1 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#6-32 x 1-1/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#6-32 x 1-1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#6-32 x 1-3/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                }
            },
            FastenerSize.NO_8_32 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_09375 : { "standard" : { "name" : "#8-32 x 3/32 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#8-32 x 1/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#8-32 x 5/32 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#8-32 x 3/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#8-32 x 1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#8-32 x 5/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#8-32 x 3/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#8-32 x 7/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#8-32 x 1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#8-32 x 9/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#8-32 x 5/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_6875 : { "standard" : { "name" : "#8-32 x 11/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#8-32 x 3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#8-32 x 7/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#8-32 x 1 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#8-32 x 1-1/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#8-32 x 1-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#8-32 x 1-3/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#8-32 x 1-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#8-32 x 1-5/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#8-32 x 1-3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_875 : { "standard" : { "name" : "#8-32 x 1-7/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#8-32 x 2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#8-32 x 2-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#8-32 x 2-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "#8-32 x 2-3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#8-32 x 3 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_25 : { "standard" : { "name" : "#8-32 x 3-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "#8-32 x 3-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_75 : { "standard" : { "name" : "#8-32 x 3-3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "#8-32 x 4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4_5 : { "standard" : { "name" : "#8-32 x 4-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5 : { "standard" : { "name" : "#8-32 x 5 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#8-32 x 1/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#8-32 x 5/32 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#8-32 x 3/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#8-32 x 1/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#8-32 x 5/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#8-32 x 3/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#8-32 x 7/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#8-32 x 1/2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#8-32 x 9/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#8-32 x 5/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#8-32 x 3/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#8-32 x 7/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#8-32 x 1 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#8-32 x 1-1/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#8-32 x 1-1/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#8-32 x 1-3/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#8-32 x 1-1/2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#8-32 x 1-3/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#8-32 x 2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#8-32 x 2-1/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#8-32 x 2-1/2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#8-32 x 3 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#8-32 x 5/32 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#8-32 x 3/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#8-32 x 1/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#8-32 x 5/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#8-32 x 3/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#8-32 x 7/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#8-32 x 1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#8-32 x 9/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#8-32 x 5/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#8-32 x 3/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#8-32 x 7/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#8-32 x 1 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#8-32 x 1-1/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#8-32 x 1-1/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#8-32 x 1-3/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#8-32 x 1-1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#8-32 x 1-5/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#8-32 x 1-3/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#8-32 x 2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#8-32 x 2-1/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#8-32 x 2-1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#8-32 x 3 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                }
            },
            FastenerSize.NO_10_32 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#10-32 x 5/32 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#10-32 x 3/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#10-32 x 5/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#10-32 x 7/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#10-32 x 9/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#10-32 x 5/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_6875 : { "standard" : { "name" : "#10-32 x 11/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#10-32 x 7/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#10-32 x 1-1/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#10-32 x 1-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#10-32 x 1-3/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#10-32 x 1-5/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#10-32 x 1-3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_875 : { "standard" : { "name" : "#10-32 x 1-7/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#10-32 x 2-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#10-32 x 2-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "#10-32 x 2-3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#10-32 x 3 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_25 : { "standard" : { "name" : "#10-32 x 3-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "#10-32 x 3-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_75 : { "standard" : { "name" : "#10-32 x 3-3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "#10-32 x 4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4_5 : { "standard" : { "name" : "#10-32 x 4-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5 : { "standard" : { "name" : "#10-32 x 5 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5_5 : { "standard" : { "name" : "#10-32 x 5-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_6 : { "standard" : { "name" : "#10-32 x 6 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_6_5 : { "standard" : { "name" : "#10-32 x 6-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_7 : { "standard" : { "name" : "#10-32 x 7 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#10-32 x 1/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#10-32 x 3/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#10-32 x 5/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#10-32 x 7/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#10-32 x 9/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#10-32 x 5/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#10-32 x 7/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#10-32 x 1-1/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#10-32 x 1-1/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#10-32 x 1-3/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#10-32 x 1-5/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#10-32 x 1-3/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#10-32 x 2-1/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#10-32 x 2-1/2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "#10-32 x 2-3/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#10-32 x 3 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4_75 : { "standard" : { "name" : "#10-32 x 4-3/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#10-32 x 3/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#10-32 x 5/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#10-32 x 7/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#10-32 x 9/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#10-32 x 5/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#10-32 x 7/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#10-32 x 1-1/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#10-32 x 1-1/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#10-32 x 1-3/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#10-32 x 1-5/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#10-32 x 1-3/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#10-32 x 2-1/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#10-32 x 2-1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#10-32 x 3 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "#10-32 x 3-1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "#10-32 x 4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                }
            },
            FastenerSize.QUARTER_20 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "1/4-20 x 1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "1/4-20 x 5/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "1/4-20 x 3/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "1/4-20 x 7/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "1/4-20 x 1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "1/4-20 x 9/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "1/4-20 x 5/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_6875 : { "standard" : { "name" : "1/4-20 x 11/16 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "1/4-20 x 3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "1/4-20 x 7/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "1/4-20 x 1 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "1/4-20 x 1-1/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "1/4-20 x 1-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "1/4-20 x 1-3/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "1/4-20 x 1-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "1/4-20 x 1-5/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "1/4-20 x 1-3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_875 : { "standard" : { "name" : "1/4-20 x 1-7/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "1/4-20 x 2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "1/4-20 x 2-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_375 : { "standard" : { "name" : "1/4-20 x 2-3/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "1/4-20 x 2-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_625 : { "standard" : { "name" : "1/4-20 x 2-5/8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "1/4-20 x 2-3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "1/4-20 x 3 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_25 : { "standard" : { "name" : "1/4-20 x 3-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "1/4-20 x 3-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_75 : { "standard" : { "name" : "1/4-20 x 3-3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "1/4-20 x 4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4_25 : { "standard" : { "name" : "1/4-20 x 4-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4_5 : { "standard" : { "name" : "1/4-20 x 4-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5 : { "standard" : { "name" : "1/4-20 x 5 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5_25 : { "standard" : { "name" : "1/4-20 x 5-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5_5 : { "standard" : { "name" : "1/4-20 x 5-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5_75 : { "standard" : { "name" : "1/4-20 x 5-3/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_6 : { "standard" : { "name" : "1/4-20 x 6 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_6_25 : { "standard" : { "name" : "1/4-20 x 6-1/4 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_6_5 : { "standard" : { "name" : "1/4-20 x 6-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_7 : { "standard" : { "name" : "1/4-20 x 7 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_7_5 : { "standard" : { "name" : "1/4-20 x 7-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_8 : { "standard" : { "name" : "1/4-20 x 8 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_8_5 : { "standard" : { "name" : "1/4-20 x 8-1/2 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_9 : { "standard" : { "name" : "1/4-20 x 9 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_10 : { "standard" : { "name" : "1/4-20 x 10 in Socket Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "1/4-20 x 3/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "1/4-20 x 1/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "1/4-20 x 5/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "1/4-20 x 3/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "1/4-20 x 7/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "1/4-20 x 1/2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "1/4-20 x 9/16 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "1/4-20 x 5/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "1/4-20 x 3/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "1/4-20 x 7/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "1/4-20 x 1 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "1/4-20 x 1-1/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "1/4-20 x 1-1/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "1/4-20 x 1-3/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "1/4-20 x 1-1/2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "1/4-20 x 1-5/8 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "1/4-20 x 1-3/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "1/4-20 x 2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "1/4-20 x 2-1/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "1/4-20 x 2-1/2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "1/4-20 x 2-3/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "1/4-20 x 3 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_25 : { "standard" : { "name" : "1/4-20 x 3-1/4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "1/4-20 x 3-1/2 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "1/4-20 x 4 in Button Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "1/4-20 x 1/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "1/4-20 x 3/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "1/4-20 x 7/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "1/4-20 x 1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "1/4-20 x 9/16 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "1/4-20 x 5/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "1/4-20 x 3/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "1/4-20 x 7/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "1/4-20 x 1 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "1/4-20 x 1-1/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "1/4-20 x 1-1/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "1/4-20 x 1-3/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "1/4-20 x 1-1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "1/4-20 x 1-5/8 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "1/4-20 x 1-3/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "1/4-20 x 2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "1/4-20 x 2-1/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "1/4-20 x 2-1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "1/4-20 x 2-3/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "1/4-20 x 3 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_25 : { "standard" : { "name" : "1/4-20 x 3-1/4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "1/4-20 x 3-1/2 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "1/4-20 x 4 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5 : { "standard" : { "name" : "1/4-20 x 5 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_6 : { "standard" : { "name" : "1/4-20 x 6 in Flat Head Screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                }
            }
        },
        FastenerVendor.MCMASTER : {
            FastenerSize.NO_4_40 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_09375 : { "standard" : { "name" : "#4-40 x 3/32 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A231", "url" : "https://www.mcmaster.com/91251A231/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#4-40 x 1/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A018", "url" : "https://www.mcmaster.com/91864A018/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#4-40 x 5/32 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A232", "url" : "https://www.mcmaster.com/91251A232/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#4-40 x 3/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A105", "url" : "https://www.mcmaster.com/91251A105/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#4-40 x 1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A106", "url" : "https://www.mcmaster.com/91251A106/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#4-40 x 5/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A107", "url" : "https://www.mcmaster.com/91251A107/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#4-40 x 3/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A108", "url" : "https://www.mcmaster.com/91251A108/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#4-40 x 7/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A081", "url" : "https://www.mcmaster.com/91864A081/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#4-40 x 1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A110", "url" : "https://www.mcmaster.com/91251A110/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#4-40 x 9/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A028", "url" : "https://www.mcmaster.com/91864A028/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#4-40 x 5/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A112", "url" : "https://www.mcmaster.com/91251A112/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#4-40 x 3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A113", "url" : "https://www.mcmaster.com/91251A113/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#4-40 x 7/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A114", "url" : "https://www.mcmaster.com/91251A114/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#4-40 x 1 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A115", "url" : "https://www.mcmaster.com/91251A115/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#4-40 x 1 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A111", "url" : "https://www.mcmaster.com/90044A111/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#4-40 x 1-1/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A029", "url" : "https://www.mcmaster.com/91864A029/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#4-40 x 1-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A078", "url" : "https://www.mcmaster.com/91251A078/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#4-40 x 1-3/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A210", "url" : "https://www.mcmaster.com/91251A210/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#4-40 x 1-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A149", "url" : "https://www.mcmaster.com/91251A149/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#4-40 x 1-5/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A233", "url" : "https://www.mcmaster.com/91251A233/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#4-40 x 1-3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A160", "url" : "https://www.mcmaster.com/91251A160/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#4-40 x 2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A162", "url" : "https://www.mcmaster.com/91251A162/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#4-40 x 2-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A211", "url" : "https://www.mcmaster.com/91251A211/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#4-40 x 2-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A834", "url" : "https://www.mcmaster.com/91251A834/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "#4-40 x 2-3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A513", "url" : "https://www.mcmaster.com/91251A513/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#4-40 x 3 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A212", "url" : "https://www.mcmaster.com/91251A212/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "#4-40 x 3-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A291", "url" : "https://www.mcmaster.com/90044A291/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "#4-40 x 4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A292", "url" : "https://www.mcmaster.com/90044A292/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#4-40 x 1/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A102", "url" : "https://www.mcmaster.com/90128A102/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#4-40 x 3/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A105", "url" : "https://www.mcmaster.com/90128A105/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#4-40 x 1/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A106", "url" : "https://www.mcmaster.com/90128A106/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#4-40 x 5/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A107", "url" : "https://www.mcmaster.com/90128A107/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#4-40 x 3/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A108", "url" : "https://www.mcmaster.com/90128A108/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#4-40 x 7/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A311", "url" : "https://www.mcmaster.com/90128A311/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#4-40 x 1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A110", "url" : "https://www.mcmaster.com/90128A110/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#4-40 x 5/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A112", "url" : "https://www.mcmaster.com/90128A112/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#4-40 x 3/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A113", "url" : "https://www.mcmaster.com/90128A113/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#4-40 x 7/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A114", "url" : "https://www.mcmaster.com/90128A114/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#4-40 x 1 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A115", "url" : "https://www.mcmaster.com/90128A115/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#4-40 x 1-1/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A119", "url" : "https://www.mcmaster.com/90128A119/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#4-40 x 1-1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A123", "url" : "https://www.mcmaster.com/90128A123/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#4-40 x 2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A103", "url" : "https://www.mcmaster.com/90128A103/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#4-40 x 1/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A103", "url" : "https://www.mcmaster.com/91255A103/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#4-40 x 3/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A105", "url" : "https://www.mcmaster.com/91255A105/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#4-40 x 1/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A106", "url" : "https://www.mcmaster.com/91255A106/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#4-40 x 5/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A107", "url" : "https://www.mcmaster.com/91255A107/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#4-40 x 3/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A108", "url" : "https://www.mcmaster.com/91255A108/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#4-40 x 7/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A021", "url" : "https://www.mcmaster.com/91255A021/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#4-40 x 1/2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A110", "url" : "https://www.mcmaster.com/91255A110/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#4-40 x 9/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A022", "url" : "https://www.mcmaster.com/91255A022/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#4-40 x 5/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A112", "url" : "https://www.mcmaster.com/91255A112/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#4-40 x 3/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A113", "url" : "https://www.mcmaster.com/91255A113/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#4-40 x 7/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A115", "url" : "https://www.mcmaster.com/91255A115/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#4-40 x 1 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A116", "url" : "https://www.mcmaster.com/91255A116/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#4-40 x 1-1/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A117", "url" : "https://www.mcmaster.com/91255A117/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#4-40 x 1-1/2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A023", "url" : "https://www.mcmaster.com/91255A023/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#4-40 x 2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A118", "url" : "https://www.mcmaster.com/91255A118/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#4-40 x 1/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A253", "url" : "https://www.mcmaster.com/91306A253/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#4-40 x 3/16 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A711", "url" : "https://www.mcmaster.com/91306A711/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#4-40 x 1/4 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A311", "url" : "https://www.mcmaster.com/91306A311/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#4-40 x 3/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A315", "url" : "https://www.mcmaster.com/91306A315/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#4-40 x 7/16 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#4-40 x 1/2 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A318", "url" : "https://www.mcmaster.com/91306A318/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#4-40 x 5/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A254", "url" : "https://www.mcmaster.com/91306A254/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#4-40 x 3/4 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A255", "url" : "https://www.mcmaster.com/91306A255/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#4-40 x 1/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#4-40 x 5/32 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#4-40 x 3/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A375", "url" : "https://www.mcmaster.com/91263A375/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#4-40 x 1/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A376", "url" : "https://www.mcmaster.com/91263A376/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#4-40 x 5/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A136", "url" : "https://www.mcmaster.com/91263A136/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#4-40 x 3/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A501", "url" : "https://www.mcmaster.com/91263A501/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#4-40 x 7/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#4-40 x 1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A503", "url" : "https://www.mcmaster.com/91263A503/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#4-40 x 9/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#4-40 x 5/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A961", "url" : "https://www.mcmaster.com/91263A961/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#4-40 x 3/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A962", "url" : "https://www.mcmaster.com/91263A962/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#4-40 x 7/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#4-40 x 1 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#4-40 x 1-1/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#4-40 x 1-1/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#4-40 x 1-1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#4-40 x 3/16 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A375", "url" : "https://www.mcmaster.com/91263A375/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#4-40 x 1/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A376", "url" : "https://www.mcmaster.com/91263A376/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#4-40 x 5/16 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A136", "url" : "https://www.mcmaster.com/91263A136/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#4-40 x 3/8 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A501", "url" : "https://www.mcmaster.com/91263A501/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#4-40 x 1/2 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A503", "url" : "https://www.mcmaster.com/91263A503/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#4-40 x 5/8 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A961", "url" : "https://www.mcmaster.com/91263A961/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#4-40 x 3/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A962", "url" : "https://www.mcmaster.com/91263A962/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                }
            },
            FastenerSize.NO_6_32 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_09375 : { "standard" : { "name" : "#6-32 x 3/32 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A268", "url" : "https://www.mcmaster.com/91251A268/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#6-32 x 1/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A084", "url" : "https://www.mcmaster.com/91864A084/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#6-32 x 5/32 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A269", "url" : "https://www.mcmaster.com/91251A269/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#6-32 x 3/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A085", "url" : "https://www.mcmaster.com/91864A085/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#6-32 x 1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A144", "url" : "https://www.mcmaster.com/91251A144/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#6-32 x 5/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A019", "url" : "https://www.mcmaster.com/91864A019/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#6-32 x 3/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A146", "url" : "https://www.mcmaster.com/91251A146/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#6-32 x 7/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A023", "url" : "https://www.mcmaster.com/91864A023/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#6-32 x 1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A148", "url" : "https://www.mcmaster.com/91251A148/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#6-32 x 9/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A086", "url" : "https://www.mcmaster.com/91864A086/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#6-32 x 5/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A150", "url" : "https://www.mcmaster.com/91251A150/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#6-32 x 3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A151", "url" : "https://www.mcmaster.com/91251A151/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#6-32 x 7/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A152", "url" : "https://www.mcmaster.com/91251A152/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#6-32 x 1 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A153", "url" : "https://www.mcmaster.com/91251A153/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#6-32 x 1-1/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A870", "url" : "https://www.mcmaster.com/91251A870/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#6-32 x 1-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A155", "url" : "https://www.mcmaster.com/91251A155/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#6-32 x 1-1/4 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A112", "url" : "https://www.mcmaster.com/90044A112/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#6-32 x 1-3/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A874", "url" : "https://www.mcmaster.com/91251A874/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#6-32 x 1-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A157", "url" : "https://www.mcmaster.com/91251A157/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#6-32 x 1-5/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A875", "url" : "https://www.mcmaster.com/91251A875/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#6-32 x 1-3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A158", "url" : "https://www.mcmaster.com/91251A158/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_875 : { "standard" : { "name" : "#6-32 x 1-7/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A270", "url" : "https://www.mcmaster.com/91251A270/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#6-32 x 2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A159", "url" : "https://www.mcmaster.com/91251A159/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#6-32 x 2-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A876", "url" : "https://www.mcmaster.com/91251A876/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#6-32 x 2-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A871", "url" : "https://www.mcmaster.com/91251A871/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "#6-32 x 2-3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A872", "url" : "https://www.mcmaster.com/91251A872/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#6-32 x 3 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A873", "url" : "https://www.mcmaster.com/91251A873/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "#6-32 x 3-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A295", "url" : "https://www.mcmaster.com/90044A295/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#6-32 x 1/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A118", "url" : "https://www.mcmaster.com/90128A118/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#6-32 x 3/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A121", "url" : "https://www.mcmaster.com/90128A121/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#6-32 x 1/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A144", "url" : "https://www.mcmaster.com/90128A144/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#6-32 x 5/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A145", "url" : "https://www.mcmaster.com/90128A145/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#6-32 x 3/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A146", "url" : "https://www.mcmaster.com/90128A146/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#6-32 x 7/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A312", "url" : "https://www.mcmaster.com/90128A312/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#6-32 x 1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A148", "url" : "https://www.mcmaster.com/90128A148/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#6-32 x 5/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A150", "url" : "https://www.mcmaster.com/90128A150/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#6-32 x 3/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A151", "url" : "https://www.mcmaster.com/90128A151/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#6-32 x 7/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A152", "url" : "https://www.mcmaster.com/90128A152/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#6-32 x 1-1/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#6-32 x 1-1/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A160", "url" : "https://www.mcmaster.com/90128A160/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#6-32 x 1-1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A164", "url" : "https://www.mcmaster.com/90128A164/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_875 : { "standard" : { "name" : "#6-32 x 1-7/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#6-32 x 1/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A140", "url" : "https://www.mcmaster.com/91255A140/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#6-32 x 3/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A142", "url" : "https://www.mcmaster.com/91255A142/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#6-32 x 1/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A144", "url" : "https://www.mcmaster.com/91255A144/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#6-32 x 5/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A145", "url" : "https://www.mcmaster.com/91255A145/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#6-32 x 3/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A146", "url" : "https://www.mcmaster.com/91255A146/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#6-32 x 7/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A133", "url" : "https://www.mcmaster.com/91255A133/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#6-32 x 1/2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A148", "url" : "https://www.mcmaster.com/91255A148/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#6-32 x 9/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A134", "url" : "https://www.mcmaster.com/91255A134/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#6-32 x 5/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A150", "url" : "https://www.mcmaster.com/91255A150/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#6-32 x 3/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A151", "url" : "https://www.mcmaster.com/91255A151/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#6-32 x 7/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A152", "url" : "https://www.mcmaster.com/91255A152/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#6-32 x 1 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A153", "url" : "https://www.mcmaster.com/91255A153/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#6-32 x 1 in Button Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#6-32 x 1-1/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A135", "url" : "https://www.mcmaster.com/91255A135/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#6-32 x 1-1/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A155", "url" : "https://www.mcmaster.com/91255A155/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#6-32 x 1/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A259", "url" : "https://www.mcmaster.com/91306A259/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#6-32 x 3/16 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A260", "url" : "https://www.mcmaster.com/91306A260/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#6-32 x 1/4 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A320", "url" : "https://www.mcmaster.com/91306A320/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#6-32 x 3/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A322", "url" : "https://www.mcmaster.com/91306A322/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#6-32 x 7/16 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A262", "url" : "https://www.mcmaster.com/91306A262/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#6-32 x 1/2 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A324", "url" : "https://www.mcmaster.com/91306A324/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#6-32 x 5/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A327", "url" : "https://www.mcmaster.com/91306A327/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#6-32 x 3/4 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A263", "url" : "https://www.mcmaster.com/91306A263/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#6-32 x 1/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#6-32 x 5/32 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#6-32 x 3/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A505", "url" : "https://www.mcmaster.com/91263A505/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#6-32 x 1/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A512", "url" : "https://www.mcmaster.com/91263A512/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#6-32 x 5/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A506", "url" : "https://www.mcmaster.com/91263A506/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#6-32 x 3/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A514", "url" : "https://www.mcmaster.com/91263A514/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#6-32 x 7/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#6-32 x 1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A516", "url" : "https://www.mcmaster.com/91263A516/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#6-32 x 9/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#6-32 x 5/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A507", "url" : "https://www.mcmaster.com/91263A507/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#6-32 x 3/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A518", "url" : "https://www.mcmaster.com/91263A518/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#6-32 x 7/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#6-32 x 1 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A520", "url" : "https://www.mcmaster.com/91263A520/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#6-32 x 1-1/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#6-32 x 1-1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#6-32 x 1-3/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#6-32 x 3/16 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A505", "url" : "https://www.mcmaster.com/91263A505/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#6-32 x 1/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A512", "url" : "https://www.mcmaster.com/91263A512/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#6-32 x 5/16 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A506", "url" : "https://www.mcmaster.com/91263A506/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#6-32 x 3/8 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A514", "url" : "https://www.mcmaster.com/91263A514/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#6-32 x 1/2 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A516", "url" : "https://www.mcmaster.com/91263A516/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#6-32 x 5/8 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A507", "url" : "https://www.mcmaster.com/91263A507/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#6-32 x 3/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A518", "url" : "https://www.mcmaster.com/91263A518/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                }
            },
            FastenerSize.NO_8_32 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_09375 : { "standard" : { "name" : "#8-32 x 3/32 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A271", "url" : "https://www.mcmaster.com/91251A271/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#8-32 x 1/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A179", "url" : "https://www.mcmaster.com/91251A179/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#8-32 x 5/32 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A272", "url" : "https://www.mcmaster.com/91251A272/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#8-32 x 3/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A180", "url" : "https://www.mcmaster.com/91251A180/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#8-32 x 1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A190", "url" : "https://www.mcmaster.com/91251A190/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#8-32 x 5/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A191", "url" : "https://www.mcmaster.com/91251A191/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#8-32 x 3/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A192", "url" : "https://www.mcmaster.com/91251A192/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#8-32 x 7/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A024", "url" : "https://www.mcmaster.com/91864A024/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#8-32 x 1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A194", "url" : "https://www.mcmaster.com/91251A194/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#8-32 x 9/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A033", "url" : "https://www.mcmaster.com/91864A033/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#8-32 x 5/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A196", "url" : "https://www.mcmaster.com/91251A196/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_6875 : { "standard" : { "name" : "#8-32 x 11/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A421", "url" : "https://www.mcmaster.com/90044A421/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#8-32 x 3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A197", "url" : "https://www.mcmaster.com/91251A197/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#8-32 x 7/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A198", "url" : "https://www.mcmaster.com/91251A198/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#8-32 x 1 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A199", "url" : "https://www.mcmaster.com/91251A199/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#8-32 x 1-1/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A025", "url" : "https://www.mcmaster.com/91864A025/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#8-32 x 1-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A201", "url" : "https://www.mcmaster.com/91251A201/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#8-32 x 1-3/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A026", "url" : "https://www.mcmaster.com/91864A026/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#8-32 x 1-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A203", "url" : "https://www.mcmaster.com/91251A203/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#8-32 x 1-1/2 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A511", "url" : "https://www.mcmaster.com/90044A511/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#8-32 x 1-5/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A027", "url" : "https://www.mcmaster.com/91864A027/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#8-32 x 1-3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A082", "url" : "https://www.mcmaster.com/91251A082/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#8-32 x 1-3/4 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A512", "url" : "https://www.mcmaster.com/90044A512/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_875 : { "standard" : { "name" : "#8-32 x 1-7/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A188", "url" : "https://www.mcmaster.com/91251A188/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#8-32 x 2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A205", "url" : "https://www.mcmaster.com/91251A205/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#8-32 x 2 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A113", "url" : "https://www.mcmaster.com/90044A113/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#8-32 x 2-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A184", "url" : "https://www.mcmaster.com/91251A184/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#8-32 x 2-1/4 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A513", "url" : "https://www.mcmaster.com/90044A513/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#8-32 x 2-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A207", "url" : "https://www.mcmaster.com/91251A207/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#8-32 x 2-1/2 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A514", "url" : "https://www.mcmaster.com/90044A514/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "#8-32 x 2-3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A827", "url" : "https://www.mcmaster.com/91251A827/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#8-32 x 2-3/4 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A515", "url" : "https://www.mcmaster.com/90044A515/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#8-32 x 3 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A086", "url" : "https://www.mcmaster.com/91251A086/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#8-32 x 3 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A516", "url" : "https://www.mcmaster.com/90044A516/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_25 : { "standard" : { "name" : "#8-32 x 3-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A189", "url" : "https://www.mcmaster.com/91251A189/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "#8-32 x 3-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A186", "url" : "https://www.mcmaster.com/91251A186/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_75 : { "standard" : { "name" : "#8-32 x 3-3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A285", "url" : "https://www.mcmaster.com/91251A285/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "#8-32 x 4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A193", "url" : "https://www.mcmaster.com/91251A193/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4_5 : { "standard" : { "name" : "#8-32 x 4-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A297", "url" : "https://www.mcmaster.com/90044A297/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5 : { "standard" : { "name" : "#8-32 x 5 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A298", "url" : "https://www.mcmaster.com/90044A298/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#8-32 x 1/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A122", "url" : "https://www.mcmaster.com/90128A122/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#8-32 x 3/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A124", "url" : "https://www.mcmaster.com/90128A124/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#8-32 x 1/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A191", "url" : "https://www.mcmaster.com/90128A191/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#8-32 x 5/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A125", "url" : "https://www.mcmaster.com/90128A125/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#8-32 x 3/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A192", "url" : "https://www.mcmaster.com/90128A192/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#8-32 x 7/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A313", "url" : "https://www.mcmaster.com/90128A313/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#8-32 x 1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A194", "url" : "https://www.mcmaster.com/90128A194/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#8-32 x 9/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A314", "url" : "https://www.mcmaster.com/90128A314/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#8-32 x 5/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A196", "url" : "https://www.mcmaster.com/90128A196/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#8-32 x 3/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A197", "url" : "https://www.mcmaster.com/90128A197/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#8-32 x 7/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A198", "url" : "https://www.mcmaster.com/90128A198/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#8-32 x 1 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A199", "url" : "https://www.mcmaster.com/90128A199/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#8-32 x 1-1/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A126", "url" : "https://www.mcmaster.com/90128A126/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#8-32 x 1-1/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A201", "url" : "https://www.mcmaster.com/90128A201/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#8-32 x 1-1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A203", "url" : "https://www.mcmaster.com/90128A203/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#8-32 x 1-3/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A206", "url" : "https://www.mcmaster.com/90128A206/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#8-32 x 2-1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A213", "url" : "https://www.mcmaster.com/90128A213/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#8-32 x 1/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A162", "url" : "https://www.mcmaster.com/91255A162/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#8-32 x 5/32 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A163", "url" : "https://www.mcmaster.com/91255A163/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#8-32 x 3/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A189", "url" : "https://www.mcmaster.com/91255A189/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#8-32 x 1/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A190", "url" : "https://www.mcmaster.com/91255A190/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#8-32 x 5/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A191", "url" : "https://www.mcmaster.com/91255A191/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#8-32 x 3/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A192", "url" : "https://www.mcmaster.com/91255A192/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#8-32 x 7/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A164", "url" : "https://www.mcmaster.com/91255A164/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#8-32 x 1/2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A194", "url" : "https://www.mcmaster.com/91255A194/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#8-32 x 9/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A165", "url" : "https://www.mcmaster.com/91255A165/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#8-32 x 5/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A196", "url" : "https://www.mcmaster.com/91255A196/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#8-32 x 3/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A197", "url" : "https://www.mcmaster.com/91255A197/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#8-32 x 7/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A166", "url" : "https://www.mcmaster.com/91255A166/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#8-32 x 1 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A199", "url" : "https://www.mcmaster.com/91255A199/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#8-32 x 1-1/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A167", "url" : "https://www.mcmaster.com/91255A167/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#8-32 x 1-1/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A201", "url" : "https://www.mcmaster.com/91255A201/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#8-32 x 1-3/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A168", "url" : "https://www.mcmaster.com/91255A168/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#8-32 x 1-1/2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A203", "url" : "https://www.mcmaster.com/91255A203/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#8-32 x 1-3/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A169", "url" : "https://www.mcmaster.com/91255A169/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#8-32 x 2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A205", "url" : "https://www.mcmaster.com/91255A205/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#8-32 x 2-1/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A206", "url" : "https://www.mcmaster.com/91255A206/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#8-32 x 2-1/2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A811", "url" : "https://www.mcmaster.com/91255A811/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#8-32 x 3 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A812", "url" : "https://www.mcmaster.com/91255A812/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#8-32 x 3/16 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A264", "url" : "https://www.mcmaster.com/91306A264/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#8-32 x 1/4 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A333", "url" : "https://www.mcmaster.com/91306A333/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#8-32 x 5/16 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A265", "url" : "https://www.mcmaster.com/91306A265/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#8-32 x 3/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A335", "url" : "https://www.mcmaster.com/91306A335/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#8-32 x 7/16 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A266", "url" : "https://www.mcmaster.com/91306A266/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#8-32 x 1/2 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A337", "url" : "https://www.mcmaster.com/91306A337/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#8-32 x 5/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A338", "url" : "https://www.mcmaster.com/91306A338/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#8-32 x 3/4 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A339", "url" : "https://www.mcmaster.com/91306A339/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#8-32 x 7/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A267", "url" : "https://www.mcmaster.com/91306A267/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#8-32 x 1 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A340", "url" : "https://www.mcmaster.com/91306A340/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#8-32 x 1-1/2 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A268", "url" : "https://www.mcmaster.com/91306A268/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#8-32 x 5/32 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#8-32 x 3/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#8-32 x 1/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A521", "url" : "https://www.mcmaster.com/91263A521/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#8-32 x 5/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A523", "url" : "https://www.mcmaster.com/91263A523/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#8-32 x 3/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A522", "url" : "https://www.mcmaster.com/91263A522/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#8-32 x 7/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#8-32 x 1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A524", "url" : "https://www.mcmaster.com/91263A524/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#8-32 x 9/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#8-32 x 5/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A525", "url" : "https://www.mcmaster.com/91263A525/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#8-32 x 3/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A526", "url" : "https://www.mcmaster.com/91263A526/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#8-32 x 7/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A527", "url" : "https://www.mcmaster.com/91263A527/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#8-32 x 1 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A528", "url" : "https://www.mcmaster.com/91263A528/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#8-32 x 1-1/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#8-32 x 1-1/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A142", "url" : "https://www.mcmaster.com/91263A142/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#8-32 x 1-3/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#8-32 x 1-1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A145", "url" : "https://www.mcmaster.com/91263A145/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#8-32 x 1-5/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#8-32 x 1-3/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#8-32 x 2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#8-32 x 2-1/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#8-32 x 2-1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#8-32 x 3 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#8-32 x 1/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A521", "url" : "https://www.mcmaster.com/91263A521/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#8-32 x 5/16 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A523", "url" : "https://www.mcmaster.com/91263A523/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#8-32 x 3/8 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A522", "url" : "https://www.mcmaster.com/91263A522/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#8-32 x 1/2 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A524", "url" : "https://www.mcmaster.com/91263A524/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#8-32 x 5/8 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A525", "url" : "https://www.mcmaster.com/91263A525/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#8-32 x 3/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A526", "url" : "https://www.mcmaster.com/91263A526/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#8-32 x 7/8 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A527", "url" : "https://www.mcmaster.com/91263A527/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#8-32 x 1 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A528", "url" : "https://www.mcmaster.com/91263A528/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#8-32 x 1-1/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A142", "url" : "https://www.mcmaster.com/91263A142/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#8-32 x 1-1/2 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A145", "url" : "https://www.mcmaster.com/91263A145/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                }
            },
            FastenerSize.NO_10_32 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_15625 : { "standard" : { "name" : "#10-32 x 5/32 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A518", "url" : "https://www.mcmaster.com/91251A518/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#10-32 x 3/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A901", "url" : "https://www.mcmaster.com/91251A901/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A338", "url" : "https://www.mcmaster.com/91251A338/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#10-32 x 5/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A337", "url" : "https://www.mcmaster.com/91251A337/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A340", "url" : "https://www.mcmaster.com/91251A340/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#10-32 x 7/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A350", "url" : "https://www.mcmaster.com/91251A350/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A342", "url" : "https://www.mcmaster.com/91251A342/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#10-32 x 9/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A356", "url" : "https://www.mcmaster.com/91251A356/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#10-32 x 5/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A344", "url" : "https://www.mcmaster.com/91251A344/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_6875 : { "standard" : { "name" : "#10-32 x 11/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A201", "url" : "https://www.mcmaster.com/90044A201/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A345", "url" : "https://www.mcmaster.com/91251A345/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#10-32 x 7/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A346", "url" : "https://www.mcmaster.com/91251A346/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A347", "url" : "https://www.mcmaster.com/91251A347/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#10-32 x 1-1/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A348", "url" : "https://www.mcmaster.com/91251A348/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#10-32 x 1-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A349", "url" : "https://www.mcmaster.com/91251A349/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#10-32 x 1-3/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A202", "url" : "https://www.mcmaster.com/90044A202/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A351", "url" : "https://www.mcmaster.com/91251A351/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#10-32 x 1-1/2 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A118", "url" : "https://www.mcmaster.com/90044A118/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#10-32 x 1-5/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A287", "url" : "https://www.mcmaster.com/91251A287/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#10-32 x 1-3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A352", "url" : "https://www.mcmaster.com/91251A352/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#10-32 x 1-3/4 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A119", "url" : "https://www.mcmaster.com/90044A119/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_875 : { "standard" : { "name" : "#10-32 x 1-7/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A288", "url" : "https://www.mcmaster.com/91251A288/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A353", "url" : "https://www.mcmaster.com/91251A353/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#10-32 x 2 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A120", "url" : "https://www.mcmaster.com/90044A120/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#10-32 x 2-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A354", "url" : "https://www.mcmaster.com/91251A354/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#10-32 x 2-1/4 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A523", "url" : "https://www.mcmaster.com/90044A523/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#10-32 x 2-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A355", "url" : "https://www.mcmaster.com/91251A355/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#10-32 x 2-1/2 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A121", "url" : "https://www.mcmaster.com/90044A121/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "#10-32 x 2-3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A359", "url" : "https://www.mcmaster.com/91251A359/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#10-32 x 2-3/4 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A524", "url" : "https://www.mcmaster.com/90044A524/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#10-32 x 3 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A360", "url" : "https://www.mcmaster.com/91251A360/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#10-32 x 3 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A122", "url" : "https://www.mcmaster.com/90044A122/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "#10-32 x 3-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A361", "url" : "https://www.mcmaster.com/91251A361/", "material" : STEEL, "appearance" : BLACK }, "fullThread" : { "name" : "#10-32 x 3-1/2 in Socket Head Screw (Fully threaded)", "vendor" : "McMaster-Carr", "partNumber" : "90044A525", "url" : "https://www.mcmaster.com/90044A525/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_75 : { "standard" : { "name" : "#10-32 x 3-3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A289", "url" : "https://www.mcmaster.com/91251A289/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "#10-32 x 4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A364", "url" : "https://www.mcmaster.com/91251A364/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4_5 : { "standard" : { "name" : "#10-32 x 4-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A357", "url" : "https://www.mcmaster.com/91251A357/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5 : { "standard" : { "name" : "#10-32 x 5 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A358", "url" : "https://www.mcmaster.com/91251A358/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5_5 : { "standard" : { "name" : "#10-32 x 5-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A906", "url" : "https://www.mcmaster.com/91251A906/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_6 : { "standard" : { "name" : "#10-32 x 6 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A907", "url" : "https://www.mcmaster.com/91251A907/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_6_5 : { "standard" : { "name" : "#10-32 x 6-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A262", "url" : "https://www.mcmaster.com/90044A262/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_7 : { "standard" : { "name" : "#10-32 x 7 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A263", "url" : "https://www.mcmaster.com/90044A263/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#10-32 x 3/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A129", "url" : "https://www.mcmaster.com/90128A129/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A932", "url" : "https://www.mcmaster.com/90128A932/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#10-32 x 5/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A131", "url" : "https://www.mcmaster.com/90128A131/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A940", "url" : "https://www.mcmaster.com/90128A940/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#10-32 x 7/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A315", "url" : "https://www.mcmaster.com/90128A315/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A942", "url" : "https://www.mcmaster.com/90128A942/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#10-32 x 9/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A316", "url" : "https://www.mcmaster.com/90128A316/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#10-32 x 5/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A944", "url" : "https://www.mcmaster.com/90128A944/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A945", "url" : "https://www.mcmaster.com/90128A945/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#10-32 x 7/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A946", "url" : "https://www.mcmaster.com/90128A946/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A947", "url" : "https://www.mcmaster.com/90128A947/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#10-32 x 1-1/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A948", "url" : "https://www.mcmaster.com/90128A948/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#10-32 x 1-3/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A132", "url" : "https://www.mcmaster.com/90128A132/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A951", "url" : "https://www.mcmaster.com/90128A951/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#10-32 x 1-3/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A953", "url" : "https://www.mcmaster.com/90128A953/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A955", "url" : "https://www.mcmaster.com/90128A955/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#10-32 x 2-1/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A957", "url" : "https://www.mcmaster.com/90128A957/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#10-32 x 2-1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A961", "url" : "https://www.mcmaster.com/90128A961/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "#10-32 x 2-3/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A965", "url" : "https://www.mcmaster.com/90128A965/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#10-32 x 3 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A969", "url" : "https://www.mcmaster.com/90128A969/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "#10-32 x 3-1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A973", "url" : "https://www.mcmaster.com/90128A973/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "#10-32 x 4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A977", "url" : "https://www.mcmaster.com/90128A977/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_125 : { "standard" : { "name" : "#10-32 x 1/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A024", "url" : "https://www.mcmaster.com/91255A024/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#10-32 x 3/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A025", "url" : "https://www.mcmaster.com/91255A025/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A261", "url" : "https://www.mcmaster.com/91255A261/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#10-32 x 5/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A262", "url" : "https://www.mcmaster.com/91255A262/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A263", "url" : "https://www.mcmaster.com/91255A263/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#10-32 x 7/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A026", "url" : "https://www.mcmaster.com/91255A026/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A265", "url" : "https://www.mcmaster.com/91255A265/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#10-32 x 9/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A027", "url" : "https://www.mcmaster.com/91255A027/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#10-32 x 5/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A267", "url" : "https://www.mcmaster.com/91255A267/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A269", "url" : "https://www.mcmaster.com/91255A269/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#10-32 x 7/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A271", "url" : "https://www.mcmaster.com/91255A271/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A273", "url" : "https://www.mcmaster.com/91255A273/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#10-32 x 1-1/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A028", "url" : "https://www.mcmaster.com/91255A028/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#10-32 x 1-1/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A274", "url" : "https://www.mcmaster.com/91255A274/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#10-32 x 1-3/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A272", "url" : "https://www.mcmaster.com/91255A272/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A275", "url" : "https://www.mcmaster.com/91255A275/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#10-32 x 1-5/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A277", "url" : "https://www.mcmaster.com/91255A277/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#10-32 x 1-3/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A276", "url" : "https://www.mcmaster.com/91255A276/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A278", "url" : "https://www.mcmaster.com/91255A278/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#10-32 x 2-1/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A815", "url" : "https://www.mcmaster.com/91255A815/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#10-32 x 2-1/2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A029", "url" : "https://www.mcmaster.com/91255A029/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#10-32 x 3 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A030", "url" : "https://www.mcmaster.com/91255A030/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A352", "url" : "https://www.mcmaster.com/91306A352/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#10-32 x 5/16 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A272", "url" : "https://www.mcmaster.com/91306A272/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A354", "url" : "https://www.mcmaster.com/91306A354/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#10-32 x 7/16 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A273", "url" : "https://www.mcmaster.com/91306A273/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A356", "url" : "https://www.mcmaster.com/91306A356/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#10-32 x 5/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A274", "url" : "https://www.mcmaster.com/91306A274/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A358", "url" : "https://www.mcmaster.com/91306A358/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#10-32 x 7/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A275", "url" : "https://www.mcmaster.com/91306A275/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A276", "url" : "https://www.mcmaster.com/91306A276/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#10-32 x 1-1/4 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A714", "url" : "https://www.mcmaster.com/91306A714/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A277", "url" : "https://www.mcmaster.com/91306A277/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A715", "url" : "https://www.mcmaster.com/91306A715/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "#10-32 x 3/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A540", "url" : "https://www.mcmaster.com/91263A540/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#10-32 x 5/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A157", "url" : "https://www.mcmaster.com/91263A157/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A542", "url" : "https://www.mcmaster.com/91263A542/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "#10-32 x 7/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A544", "url" : "https://www.mcmaster.com/91263A544/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "#10-32 x 9/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#10-32 x 5/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A543", "url" : "https://www.mcmaster.com/91263A543/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A546", "url" : "https://www.mcmaster.com/91263A546/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#10-32 x 7/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A547", "url" : "https://www.mcmaster.com/91263A547/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A548", "url" : "https://www.mcmaster.com/91263A548/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#10-32 x 1-1/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#10-32 x 1-1/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A160", "url" : "https://www.mcmaster.com/91263A160/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#10-32 x 1-3/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A550", "url" : "https://www.mcmaster.com/91263A550/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#10-32 x 1-5/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#10-32 x 1-3/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#10-32 x 2-1/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#10-32 x 2-1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A163", "url" : "https://www.mcmaster.com/91263A163/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#10-32 x 3 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "#10-32 x 3-1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "#10-32 x 4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A540", "url" : "https://www.mcmaster.com/91263A540/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#10-32 x 5/16 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A157", "url" : "https://www.mcmaster.com/91263A157/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A542", "url" : "https://www.mcmaster.com/91263A542/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A544", "url" : "https://www.mcmaster.com/91263A544/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#10-32 x 5/8 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A543", "url" : "https://www.mcmaster.com/91263A543/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A546", "url" : "https://www.mcmaster.com/91263A546/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#10-32 x 7/8 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A547", "url" : "https://www.mcmaster.com/91263A547/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A548", "url" : "https://www.mcmaster.com/91263A548/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#10-32 x 1-1/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A160", "url" : "https://www.mcmaster.com/91263A160/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A550", "url" : "https://www.mcmaster.com/91263A550/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                }
            },
            FastenerSize.QUARTER_20 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "1/4-20 x 1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A533", "url" : "https://www.mcmaster.com/91251A533/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "1/4-20 x 5/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A061", "url" : "https://www.mcmaster.com/91864A061/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "1/4-20 x 3/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A535", "url" : "https://www.mcmaster.com/91251A535/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "1/4-20 x 7/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91864A062", "url" : "https://www.mcmaster.com/91864A062/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "1/4-20 x 1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A537", "url" : "https://www.mcmaster.com/91251A537/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "1/4-20 x 9/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A538", "url" : "https://www.mcmaster.com/91251A538/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "1/4-20 x 5/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A539", "url" : "https://www.mcmaster.com/91251A539/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_6875 : { "standard" : { "name" : "1/4-20 x 11/16 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A423", "url" : "https://www.mcmaster.com/90044A423/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "1/4-20 x 3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A540", "url" : "https://www.mcmaster.com/91251A540/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "1/4-20 x 7/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A541", "url" : "https://www.mcmaster.com/91251A541/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "1/4-20 x 1 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A542", "url" : "https://www.mcmaster.com/91251A542/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "1/4-20 x 1-1/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A560", "url" : "https://www.mcmaster.com/91251A560/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "1/4-20 x 1-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A544", "url" : "https://www.mcmaster.com/91251A544/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "1/4-20 x 1-3/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A543", "url" : "https://www.mcmaster.com/91251A543/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "1/4-20 x 1-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A123", "url" : "https://www.mcmaster.com/90044A123/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "1/4-20 x 1-5/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A527", "url" : "https://www.mcmaster.com/91251A527/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "1/4-20 x 1-3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A124", "url" : "https://www.mcmaster.com/90044A124/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_875 : { "standard" : { "name" : "1/4-20 x 1-7/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A548", "url" : "https://www.mcmaster.com/91251A548/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "1/4-20 x 2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A125", "url" : "https://www.mcmaster.com/90044A125/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "1/4-20 x 2-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A126", "url" : "https://www.mcmaster.com/90044A126/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_375 : { "standard" : { "name" : "1/4-20 x 2-3/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A425", "url" : "https://www.mcmaster.com/90044A425/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "1/4-20 x 2-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A127", "url" : "https://www.mcmaster.com/90044A127/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_625 : { "standard" : { "name" : "1/4-20 x 2-5/8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A426", "url" : "https://www.mcmaster.com/90044A426/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "1/4-20 x 2-3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A128", "url" : "https://www.mcmaster.com/90044A128/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "1/4-20 x 3 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A129", "url" : "https://www.mcmaster.com/90044A129/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_25 : { "standard" : { "name" : "1/4-20 x 3-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A526", "url" : "https://www.mcmaster.com/90044A526/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "1/4-20 x 3-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A316", "url" : "https://www.mcmaster.com/90044A316/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_75 : { "standard" : { "name" : "1/4-20 x 3-3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A527", "url" : "https://www.mcmaster.com/90044A527/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "1/4-20 x 4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A131", "url" : "https://www.mcmaster.com/90044A131/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4_25 : { "standard" : { "name" : "1/4-20 x 4-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A559", "url" : "https://www.mcmaster.com/91251A559/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4_5 : { "standard" : { "name" : "1/4-20 x 4-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A089", "url" : "https://www.mcmaster.com/91251A089/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5 : { "standard" : { "name" : "1/4-20 x 5 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A090", "url" : "https://www.mcmaster.com/91251A090/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5_25 : { "standard" : { "name" : "1/4-20 x 5-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A293", "url" : "https://www.mcmaster.com/91251A293/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5_5 : { "standard" : { "name" : "1/4-20 x 5-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A093", "url" : "https://www.mcmaster.com/91251A093/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5_75 : { "standard" : { "name" : "1/4-20 x 5-3/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A294", "url" : "https://www.mcmaster.com/91251A294/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_6 : { "standard" : { "name" : "1/4-20 x 6 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A095", "url" : "https://www.mcmaster.com/91251A095/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_6_25 : { "standard" : { "name" : "1/4-20 x 6-1/4 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A562", "url" : "https://www.mcmaster.com/91251A562/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_6_5 : { "standard" : { "name" : "1/4-20 x 6-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A563", "url" : "https://www.mcmaster.com/91251A563/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_7 : { "standard" : { "name" : "1/4-20 x 7 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A908", "url" : "https://www.mcmaster.com/91251A908/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_7_5 : { "standard" : { "name" : "1/4-20 x 7-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A923", "url" : "https://www.mcmaster.com/91251A923/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_8 : { "standard" : { "name" : "1/4-20 x 8 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91251A566", "url" : "https://www.mcmaster.com/91251A566/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_8_5 : { "standard" : { "name" : "1/4-20 x 8-1/2 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A299", "url" : "https://www.mcmaster.com/90044A299/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_9 : { "standard" : { "name" : "1/4-20 x 9 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A311", "url" : "https://www.mcmaster.com/90044A311/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_10 : { "standard" : { "name" : "1/4-20 x 10 in Socket Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "90044A312", "url" : "https://www.mcmaster.com/90044A312/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "1/4-20 x 1/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A239", "url" : "https://www.mcmaster.com/90128A239/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "1/4-20 x 3/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A241", "url" : "https://www.mcmaster.com/90128A241/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "1/4-20 x 7/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A317", "url" : "https://www.mcmaster.com/90128A317/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "1/4-20 x 1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A242", "url" : "https://www.mcmaster.com/90128A242/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "1/4-20 x 9/16 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A318", "url" : "https://www.mcmaster.com/90128A318/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "1/4-20 x 5/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A243", "url" : "https://www.mcmaster.com/90128A243/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "1/4-20 x 3/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A245", "url" : "https://www.mcmaster.com/90128A245/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "1/4-20 x 7/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A246", "url" : "https://www.mcmaster.com/90128A246/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "1/4-20 x 1 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A247", "url" : "https://www.mcmaster.com/90128A247/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "1/4-20 x 1-1/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A133", "url" : "https://www.mcmaster.com/90128A133/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "1/4-20 x 1-1/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A250", "url" : "https://www.mcmaster.com/90128A250/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "1/4-20 x 1-3/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A301", "url" : "https://www.mcmaster.com/90128A301/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "1/4-20 x 1-1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A251", "url" : "https://www.mcmaster.com/90128A251/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "1/4-20 x 1-5/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A319", "url" : "https://www.mcmaster.com/90128A319/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "1/4-20 x 1-3/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A252", "url" : "https://www.mcmaster.com/90128A252/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_875 : { "standard" : { "name" : "1/4-20 x 1-7/8 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90044A424", "url" : "https://www.mcmaster.com/90044A424/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "1/4-20 x 2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A253", "url" : "https://www.mcmaster.com/90128A253/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "1/4-20 x 2-1/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A254", "url" : "https://www.mcmaster.com/90128A254/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "1/4-20 x 2-1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A255", "url" : "https://www.mcmaster.com/90128A255/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "1/4-20 x 2-3/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A259", "url" : "https://www.mcmaster.com/90128A259/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "1/4-20 x 3 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A554", "url" : "https://www.mcmaster.com/90128A554/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_3_25 : { "standard" : { "name" : "1/4-20 x 3-1/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A557", "url" : "https://www.mcmaster.com/90128A557/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "1/4-20 x 3-1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A560", "url" : "https://www.mcmaster.com/90128A560/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_3_75 : { "standard" : { "name" : "1/4-20 x 3-3/4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A563", "url" : "https://www.mcmaster.com/90128A563/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "1/4-20 x 4 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A566", "url" : "https://www.mcmaster.com/90128A566/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_4_5 : { "standard" : { "name" : "1/4-20 x 4-1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A569", "url" : "https://www.mcmaster.com/90128A569/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_5 : { "standard" : { "name" : "1/4-20 x 5 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A572", "url" : "https://www.mcmaster.com/90128A572/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_5_5 : { "standard" : { "name" : "1/4-20 x 5-1/2 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A575", "url" : "https://www.mcmaster.com/90128A575/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_6 : { "standard" : { "name" : "1/4-20 x 6 in Socket Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "90128A577", "url" : "https://www.mcmaster.com/90128A577/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_1875 : { "standard" : { "name" : "1/4-20 x 3/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A521", "url" : "https://www.mcmaster.com/91255A521/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "1/4-20 x 1/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A533", "url" : "https://www.mcmaster.com/91255A533/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "1/4-20 x 5/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A522", "url" : "https://www.mcmaster.com/91255A522/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "1/4-20 x 3/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A535", "url" : "https://www.mcmaster.com/91255A535/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "1/4-20 x 7/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A523", "url" : "https://www.mcmaster.com/91255A523/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "1/4-20 x 1/2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A537", "url" : "https://www.mcmaster.com/91255A537/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "1/4-20 x 9/16 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A524", "url" : "https://www.mcmaster.com/91255A524/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "1/4-20 x 5/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A539", "url" : "https://www.mcmaster.com/91255A539/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "1/4-20 x 3/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A540", "url" : "https://www.mcmaster.com/91255A540/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "1/4-20 x 7/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A541", "url" : "https://www.mcmaster.com/91255A541/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "1/4-20 x 1 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A542", "url" : "https://www.mcmaster.com/91255A542/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "1/4-20 x 1-1/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A525", "url" : "https://www.mcmaster.com/91255A525/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "1/4-20 x 1-1/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A544", "url" : "https://www.mcmaster.com/91255A544/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "1/4-20 x 1-3/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A526", "url" : "https://www.mcmaster.com/91255A526/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "1/4-20 x 1-1/2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A546", "url" : "https://www.mcmaster.com/91255A546/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "1/4-20 x 1-5/8 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A527", "url" : "https://www.mcmaster.com/91255A527/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "1/4-20 x 1-3/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A548", "url" : "https://www.mcmaster.com/91255A548/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "1/4-20 x 2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A545", "url" : "https://www.mcmaster.com/91255A545/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "1/4-20 x 2-1/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A528", "url" : "https://www.mcmaster.com/91255A528/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "1/4-20 x 2-1/2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A547", "url" : "https://www.mcmaster.com/91255A547/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "1/4-20 x 2-3/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A529", "url" : "https://www.mcmaster.com/91255A529/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "1/4-20 x 3 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A549", "url" : "https://www.mcmaster.com/91255A549/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_25 : { "standard" : { "name" : "1/4-20 x 3-1/4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A819", "url" : "https://www.mcmaster.com/91255A819/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "1/4-20 x 3-1/2 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A531", "url" : "https://www.mcmaster.com/91255A531/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "1/4-20 x 4 in Button Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91255A532", "url" : "https://www.mcmaster.com/91255A532/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "1/4-20 x 1/4 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A371", "url" : "https://www.mcmaster.com/91306A371/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "1/4-20 x 5/16 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A278", "url" : "https://www.mcmaster.com/91306A278/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "1/4-20 x 3/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A373", "url" : "https://www.mcmaster.com/91306A373/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "1/4-20 x 1/2 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A375", "url" : "https://www.mcmaster.com/91306A375/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "1/4-20 x 5/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A377", "url" : "https://www.mcmaster.com/91306A377/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "1/4-20 x 3/4 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A379", "url" : "https://www.mcmaster.com/91306A379/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "1/4-20 x 7/8 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A279", "url" : "https://www.mcmaster.com/91306A279/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "1/4-20 x 1 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A383", "url" : "https://www.mcmaster.com/91306A383/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "1/4-20 x 1-1/4 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A280", "url" : "https://www.mcmaster.com/91306A280/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "1/4-20 x 1-1/2 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A384", "url" : "https://www.mcmaster.com/91306A384/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "1/4-20 x 1-3/4 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A281", "url" : "https://www.mcmaster.com/91306A281/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "1/4-20 x 2 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A282", "url" : "https://www.mcmaster.com/91306A282/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "1/4-20 x 2-1/2 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A716", "url" : "https://www.mcmaster.com/91306A716/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "1/4-20 x 3 in Button Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91306A717", "url" : "https://www.mcmaster.com/91306A717/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "1/4-20 x 1/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "1/4-20 x 3/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A551", "url" : "https://www.mcmaster.com/91263A551/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_4375 : { "standard" : { "name" : "1/4-20 x 7/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "1/4-20 x 1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A553", "url" : "https://www.mcmaster.com/91263A553/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5625 : { "standard" : { "name" : "1/4-20 x 9/16 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "1/4-20 x 5/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A556", "url" : "https://www.mcmaster.com/91263A556/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "1/4-20 x 3/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A559", "url" : "https://www.mcmaster.com/91263A559/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "1/4-20 x 7/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A560", "url" : "https://www.mcmaster.com/91263A560/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "1/4-20 x 1 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A562", "url" : "https://www.mcmaster.com/91263A562/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "1/4-20 x 1-1/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A166", "url" : "https://www.mcmaster.com/91263A166/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "1/4-20 x 1-1/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A563", "url" : "https://www.mcmaster.com/91263A563/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "1/4-20 x 1-3/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "1/4-20 x 1-1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A564", "url" : "https://www.mcmaster.com/91263A564/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "1/4-20 x 1-5/8 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "1/4-20 x 1-3/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A565", "url" : "https://www.mcmaster.com/91263A565/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "1/4-20 x 2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A566", "url" : "https://www.mcmaster.com/91263A566/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "1/4-20 x 2-1/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A567", "url" : "https://www.mcmaster.com/91263A567/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "1/4-20 x 2-1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A568", "url" : "https://www.mcmaster.com/91263A568/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "1/4-20 x 2-3/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A569", "url" : "https://www.mcmaster.com/91263A569/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "1/4-20 x 3 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A570", "url" : "https://www.mcmaster.com/91263A570/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_25 : { "standard" : { "name" : "1/4-20 x 3-1/4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "1/4-20 x 3-1/2 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A169", "url" : "https://www.mcmaster.com/91263A169/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "1/4-20 x 4 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "91263A172", "url" : "https://www.mcmaster.com/91263A172/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5 : { "standard" : { "name" : "1/4-20 x 5 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_6 : { "standard" : { "name" : "1/4-20 x 6 in Flat Head Screw", "vendor" : "McMaster-Carr", "partNumber" : "null", "url" : "https://www.mcmaster.com/null/", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "1/4-20 x 3/8 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A551", "url" : "https://www.mcmaster.com/91263A551/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "1/4-20 x 1/2 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A553", "url" : "https://www.mcmaster.com/91263A553/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "1/4-20 x 5/8 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A556", "url" : "https://www.mcmaster.com/91263A556/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "1/4-20 x 3/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A559", "url" : "https://www.mcmaster.com/91263A559/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "1/4-20 x 7/8 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A560", "url" : "https://www.mcmaster.com/91263A560/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "1/4-20 x 1 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A562", "url" : "https://www.mcmaster.com/91263A562/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "1/4-20 x 1-1/8 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A166", "url" : "https://www.mcmaster.com/91263A166/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "1/4-20 x 1-1/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A563", "url" : "https://www.mcmaster.com/91263A563/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "1/4-20 x 1-1/2 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A564", "url" : "https://www.mcmaster.com/91263A564/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "1/4-20 x 1-3/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A565", "url" : "https://www.mcmaster.com/91263A565/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "1/4-20 x 2 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A566", "url" : "https://www.mcmaster.com/91263A566/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "1/4-20 x 2-1/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A567", "url" : "https://www.mcmaster.com/91263A567/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "1/4-20 x 2-1/2 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A568", "url" : "https://www.mcmaster.com/91263A568/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "1/4-20 x 2-3/4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A569", "url" : "https://www.mcmaster.com/91263A569/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "1/4-20 x 3 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A570", "url" : "https://www.mcmaster.com/91263A570/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "1/4-20 x 3-1/2 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A169", "url" : "https://www.mcmaster.com/91263A169/", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "1/4-20 x 4 in Flat Head Screw (Zinc plated)", "vendor" : "McMaster-Carr", "partNumber" : "91263A172", "url" : "https://www.mcmaster.com/91263A172/", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                }
            }
        },
        FastenerVendor.REV : {
            FastenerSize.NO_10_32 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2910-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2910-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#10-32 x 5/16 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2911-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2911-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2912-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2912-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2914-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2914-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#10-32 x 5/8 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2916-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2916-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2918-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2918-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2922-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2922-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#10-32 x 1-1/4 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2924-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2924-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2926-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2926-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#10-32 x 1-3/4 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2928-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2928-PK25&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2930-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2930-PK25&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#10-32 x 2-1/4 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2931-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2931-PK25&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#10-32 x 2-1/2 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2932-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2932-PK25&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "#10-32 x 2-3/4 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2933-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2933-PK25&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#10-32 x 3 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2934-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2934-PK25&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "#10-32 x 3-1/2 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2936-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2936-PK25&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "#10-32 x 4 in Socket Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2938-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2938-PK25&section=product", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2939-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2939-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_3125 : { "standard" : { "name" : "#10-32 x 5/16 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2940-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2940-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2941-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2941-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2943-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2943-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#10-32 x 5/8 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2945-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2945-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2947-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2947-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2951-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2951-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#10-32 x 1-1/4 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2953-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2953-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2955-PK50", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2955-PK50&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_625 : { "standard" : { "name" : "#10-32 x 1-5/8 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2956-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2956-PK25&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#10-32 x 1-3/4 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2957-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2957-PK25&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2959-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2959-PK25&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#10-32 x 2-1/4 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2960-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2960-PK25&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#10-32 x 2-1/2 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2961-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2961-PK25&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "#10-32 x 2-3/4 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2962-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2962-PK25&section=product", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#10-32 x 3 in Button Head Screw", "vendor" : "REV", "partNumber" : "REV-29-2963-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-2963-PK25&section=product", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Button Head Screw (Zinc plated)", "vendor" : "REV", "partNumber" : "REV-29-1221-PK100", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-1221-PK100&section=product", "material" : STEEL, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Button Head Screw (Zinc plated)", "vendor" : "REV", "partNumber" : "REV-29-1222-PK100", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-1222-PK100&section=product", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Flat Head Screw (Zinc plated)", "vendor" : "REV", "partNumber" : "REV-29-1223-PK25", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-1223-PK25&section=product", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                }
            }
        },
        FastenerVendor.WCP : {
            FastenerSize.NO_8_32 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_1 : { "standard" : { "name" : "#8-32 x 1 in Socket Head Screw", "vendor" : "WCP", "partNumber" : "WCP-2249", "url" : "https://wcproducts.com/products/wcp-2249", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#8-32 x 1/4 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-1548", "url" : "https://wcproducts.com/products/wcp-1548", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#8-32 x 1/2 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0250", "url" : "https://wcproducts.com/products/wcp-0250", "material" : STEEL, "appearance" : BLACK } }
                    }
                }
            },
            FastenerSize.NO_10_32 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Socket Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0264", "url" : "https://wcproducts.com/products/wcp-0264", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Socket Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0265", "url" : "https://wcproducts.com/products/wcp-0265", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Socket Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0266", "url" : "https://wcproducts.com/products/wcp-0266", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Socket Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0267", "url" : "https://wcproducts.com/products/wcp-0267", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Socket Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0268", "url" : "https://wcproducts.com/products/wcp-0268", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#10-32 x 1-3/4 in Socket Head Screw", "vendor" : "WCP", "partNumber" : "WCP-1549", "url" : "https://wcproducts.com/products/wcp-1549", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Socket Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0269", "url" : "https://wcproducts.com/products/wcp-0269", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#10-32 x 2-1/2 in Socket Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0270", "url" : "https://wcproducts.com/products/wcp-0270", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#10-32 x 3 in Socket Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0271", "url" : "https://wcproducts.com/products/wcp-0271", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "#10-32 x 3-1/2 in Socket Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0272", "url" : "https://wcproducts.com/products/wcp-0272", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4 : { "standard" : { "name" : "#10-32 x 4 in Socket Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0273", "url" : "https://wcproducts.com/products/wcp-0273", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4_5 : { "standard" : { "name" : "#10-32 x 4-1/2 in Socket Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0274", "url" : "https://wcproducts.com/products/wcp-0274", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_5 : { "standard" : { "name" : "#10-32 x 5 in Socket Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0275", "url" : "https://wcproducts.com/products/wcp-0275", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0251", "url" : "https://wcproducts.com/products/wcp-0251", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0252", "url" : "https://wcproducts.com/products/wcp-0252", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0253", "url" : "https://wcproducts.com/products/wcp-0253", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0135", "url" : "https://wcproducts.com/products/wcp-0135", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#10-32 x 7/8 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-1463", "url" : "https://wcproducts.com/products/wcp-1463", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0255", "url" : "https://wcproducts.com/products/wcp-0255", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#10-32 x 1-1/4 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0256", "url" : "https://wcproducts.com/products/wcp-0256", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0257", "url" : "https://wcproducts.com/products/wcp-0257", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#10-32 x 1-3/4 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0258", "url" : "https://wcproducts.com/products/wcp-0258", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0259", "url" : "https://wcproducts.com/products/wcp-0259", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#10-32 x 2-1/4 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0260", "url" : "https://wcproducts.com/products/wcp-0260", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#10-32 x 2-1/2 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0261", "url" : "https://wcproducts.com/products/wcp-0261", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "#10-32 x 2-3/4 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0262", "url" : "https://wcproducts.com/products/wcp-0262", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#10-32 x 3 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-0263", "url" : "https://wcproducts.com/products/wcp-0263", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_4_75 : { "standard" : { "name" : "#10-32 x 4-3/4 in Button Head Screw", "vendor" : "WCP", "partNumber" : "WCP-1656", "url" : "https://wcproducts.com/products/wcp-1656", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ALUMINUM : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Button Head Screw (Aluminum)", "vendor" : "WCP", "partNumber" : "WCP-2110", "url" : "https://wcproducts.com/products/wcp-2110", "material" : ALUMINUM, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Button Head Screw (Aluminum)", "vendor" : "WCP", "partNumber" : "WCP-2111", "url" : "https://wcproducts.com/products/wcp-2111", "material" : ALUMINUM, "appearance" : MEDIUM_GRAY } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Button Head Screw (Aluminum)", "vendor" : "WCP", "partNumber" : "WCP-2112", "url" : "https://wcproducts.com/products/wcp-2112", "material" : ALUMINUM, "appearance" : MEDIUM_GRAY } }
                    }
                }
            }
        },
        FastenerVendor.ANDYMARK : {
            FastenerSize.NO_4_40 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#4-40 x 3/8 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1032", "url" : "https://andymark.com/products/4-40-x-0-375-in-socket-head-cap-screw-1", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#4-40 x 1/2 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1136", "url" : "https://andymark.com/products/4-40-x-0-5-in-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#4-40 x 1/4 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1679", "url" : "https://andymark.com/products/4-40-x-0-25-in-button-head-cap-screw-with-nylon-patch", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#4-40 x 3/8 in Flat Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1586", "url" : "https://andymark.com/products/4-40-x-0-375-in-flat-head-cap-screw-with-nylon-patch", "material" : STEEL, "appearance" : BLACK } }
                    }
                }
            },
            FastenerSize.NO_6_32 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_2_125 : { "standard" : { "name" : "#6-32 x 2-1/8 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1657", "url" : "https://andymark.com/products/6-32-x-2-125-in-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#6-32 x 5/8 in Flat Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1570", "url" : "https://andymark.com/products/6-32-x-0-625-in-flat-head-socket-cap-screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                }
            },
            FastenerSize.NO_8_32 : {
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#8-32 x 1/2 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1691", "url" : "https://andymark.com/products/8-32-x-0-5-in-button-head-cap-screw-with-nylon-patch", "material" : STEEL, "appearance" : BLACK } }
                    }
                }
            },
            FastenerSize.NO_10_32 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_25 : { "standard" : { "name" : "#10-32 x 1/4 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1771", "url" : "https://andymark.com/products/10-32-x-250-in-socket-head-cap-screw-with-nylon-thread-lock-patch", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1359", "url" : "https://andymark.com/products/10-32-x-0-375-in-socket-head-cap-screw-1", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1002", "url" : "https://andymark.com/products/10-32-x-0-5-in-socket-head-cap-screw-1", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#10-32 x 5/8 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1007", "url" : "https://andymark.com/products/10-32-x-0-625-in-socket-head-cap-screw-1", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1047", "url" : "https://andymark.com/products/10-32-x-0-75-in-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_875 : { "standard" : { "name" : "#10-32 x 7/8 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1753", "url" : "https://andymark.com/products/10-32-x-0-875-in-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1056", "url" : "https://andymark.com/products/10-32-x-1-in-socket-head-cap-screw-1", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_125 : { "standard" : { "name" : "#10-32 x 1-1/8 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1013", "url" : "https://andymark.com/products/10-32-x-1-125-in-socket-head-cap-screw-1", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#10-32 x 1-1/4 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1041", "url" : "https://andymark.com/products/10-32-x-1-25-in-socket-head-cap-screw-1", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_375 : { "standard" : { "name" : "#10-32 x 1-3/8 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1154", "url" : "https://andymark.com/products/10-32-x-1-375-in-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1014", "url" : "https://andymark.com/products/10-32-x-1-5-in-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#10-32 x 1-3/4 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1048", "url" : "https://andymark.com/products/10-32-x-1-75-in-socket-head-cap-screw-1", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1049", "url" : "https://andymark.com/products/10-32-x-2-in-socket-head-cap-screw-1", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_25 : { "standard" : { "name" : "#10-32 x 2-1/4 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1156", "url" : "https://andymark.com/products/10-32-x-2-25-in-socket-head-cap-screw-2", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "#10-32 x 2-1/2 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1024", "url" : "https://andymark.com/products/10-32-x-2-5-in-socket-head-cap-screw-1", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_75 : { "standard" : { "name" : "#10-32 x 2-3/4 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1397", "url" : "https://andymark.com/products/10-32-x-2-75-in-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "#10-32 x 3 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1522", "url" : "https://andymark.com/products/10-32-x-3-in-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_25 : { "standard" : { "name" : "#10-32 x 3-1/4 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1258", "url" : "https://andymark.com/products/10-32-x-3-25-in-socket-head-cap-screw-1", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_5 : { "standard" : { "name" : "#10-32 x 3-1/2 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1402", "url" : "https://andymark.com/products/10-32-x-3-5-in-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "#10-32 x 3/8 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1588", "url" : "https://andymark.com/products/10-32-x-3-8-in-button-head-cap-screw-with-nylon-patch", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1512", "url" : "https://andymark.com/products/10-32-x-0-5-in-button-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#10-32 x 5/8 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1549", "url" : "https://andymark.com/products/10-32-x-0-625-in-button-head-cap-screw-with-nylon-patch", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1524", "url" : "https://andymark.com/products/10-32-x-0-75-in-hex-drive-button-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1396", "url" : "https://andymark.com/products/10-32-x-1-in-button-head-cap-screw-1", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "#10-32 x 1-1/4 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1260", "url" : "https://andymark.com/products/10-32-x-1-25-in-button-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1730", "url" : "https://andymark.com/products/10-32-x-1-5-in-button-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "#10-32 x 1-3/4 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1584", "url" : "https://andymark.com/products/10-32-x-1-75-in-button-head-cap-screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "#10-32 x 1/2 in Flat Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1404", "url" : "https://andymark.com/products/10-32-x-0-5-in-flat-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "#10-32 x 5/8 in Flat Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1252", "url" : "https://andymark.com/products/10-32-x-0-625-in-flat-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "#10-32 x 3/4 in Flat Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1170", "url" : "https://andymark.com/products/10-32-x-0-75-in-flat-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "#10-32 x 1 in Flat Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1324", "url" : "https://andymark.com/products/10-32-x-1-in-flat-head-cap-screw-1", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "#10-32 x 1-1/2 in Flat Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1756", "url" : "https://andymark.com/products/10-32-x-1-500-in-flat-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "#10-32 x 2 in Flat Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1383", "url" : "https://andymark.com/products/10-32-x-2-in-flat-head-cap-screw-black-oxide", "material" : STEEL, "appearance" : BLACK } }
                    }
                }
            },
            FastenerSize.QUARTER_20 : {
                ScrewHead.SOCKET : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "1/4-20 x 5/8 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1053", "url" : "https://andymark.com/products/1-4-20-x-0-625-in-hex-drive-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "1/4-20 x 1 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1341", "url" : "https://andymark.com/products/1-4-20-x-1-in-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "1/4-20 x 1-1/2 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1131", "url" : "https://andymark.com/products/1-4-20-x-1-5-in-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "1/4-20 x 1-3/4 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1058", "url" : "https://andymark.com/products/1-4-20-x-1-75-in-hex-drive-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "1/4-20 x 2 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1012", "url" : "https://andymark.com/products/1-4-20-x-2-in-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "1/4-20 x 2-1/2 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1031", "url" : "https://andymark.com/products/1-4-20-x-2-5-in-hex-drive-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3 : { "standard" : { "name" : "1/4-20 x 3 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1030", "url" : "https://andymark.com/products/1-4-20-x-3-in-socket-head-cap-screw-1", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_3_25 : { "standard" : { "name" : "1/4-20 x 3-1/4 in Socket Head Screw", "vendor" : "AndyMark", "partNumber" : "am-3338", "url" : "https://andymark.com/products/1-4-20-x-3-25-in-socket-head-cap-screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                },
                ScrewHead.BUTTON : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_375 : { "standard" : { "name" : "1/4-20 x 3/8 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1520", "url" : "https://andymark.com/products/1-4-20-x-0-375-in-button-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_5 : { "standard" : { "name" : "1/4-20 x 1/2 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1039", "url" : "https://andymark.com/products/1-4-20-x-0-5-in-button-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_0_625 : { "standard" : { "name" : "1/4-20 x 5/8 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1681", "url" : "https://andymark.com/products/1-4-20-x-0-625-in-button-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1 : { "standard" : { "name" : "1/4-20 x 1 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1525", "url" : "https://andymark.com/products/1-4-20-x-1-in-button-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_25 : { "standard" : { "name" : "1/4-20 x 1-1/4 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1183", "url" : "https://andymark.com/products/1-4-20-x-1-25-in-button-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_5 : { "standard" : { "name" : "1/4-20 x 1-1/2 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1118", "url" : "https://andymark.com/products/1-4-20-x-1-5-in-button-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_1_75 : { "standard" : { "name" : "1/4-20 x 1-3/4 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1420", "url" : "https://andymark.com/products/1-4-20-x-1-75-in-button-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "1/4-20 x 2 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1655", "url" : "https://andymark.com/products/1-4-20-x-2-in-button-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2_5 : { "standard" : { "name" : "1/4-20 x 2-1/2 in Button Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1695", "url" : "https://andymark.com/products/1-4-20-x-2-5-in-button-head-cap-screw", "material" : STEEL, "appearance" : BLACK } }
                    },
                    ScrewFinish.ZINC_PLATED : {
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "1/4-20 x 3/4 in Button Head Screw (Zinc plated)", "vendor" : "AndyMark", "partNumber" : "am-1754", "url" : "https://andymark.com/products/screw-bhcs-1-4-20-x-750-zinc", "material" : STEEL, "appearance" : MEDIUM_GRAY } }
                    }
                },
                ScrewHead.FLAT : {
                    ScrewFinish.BLACK : {
                        ScrewLength.IN_0_75 : { "standard" : { "name" : "1/4-20 x 3/4 in Flat Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1672", "url" : "https://andymark.com/products/1-4-20-x-0-75-in-flat-head-cap-screw", "material" : STEEL, "appearance" : BLACK } },
                        ScrewLength.IN_2 : { "standard" : { "name" : "1/4-20 x 2 in Flat Head Screw", "vendor" : "AndyMark", "partNumber" : "am-1707", "url" : "https://andymark.com/products/1-4-20-x-2-in-flat-head-cap-screw", "material" : STEEL, "appearance" : BLACK } }
                    }
                }
            }
        }
    };

export const NUTS = {
            FastenerVendor.MCMASTER : {
                FastenerSize.NO_4_40 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.25 * inch, "height" : 0.141 * inch, "hexHeight" : 0.1175 * inch, "insertDiameter" : 0.1525 * inch, "name" : "#4-40 Nylock Nut", "vendor" : "McMaster-Carr", "partNumber" : "90631A005", "url" : "https://www.mcmaster.com/90631A005/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.25 * inch, "height" : 0.109 * inch, "hexHeight" : 0.090833 * inch, "insertDiameter" : 0.1525 * inch, "name" : "#4-40 Thin Nylock Nut", "vendor" : "McMaster-Carr", "partNumber" : "90633A005", "url" : "https://www.mcmaster.com/90633A005/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.HEX : { "acrossFlats" : 0.25 * inch, "height" : 0.094 * inch, "name" : "#4-40 Hex Nut", "vendor" : "McMaster-Carr", "partNumber" : "90480A005", "url" : "https://www.mcmaster.com/90480A005/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.THIN_HEX : { "acrossFlats" : 0.25 * inch, "height" : 0.063 * inch, "name" : "#4-40 Thin Hex Nut", "vendor" : "McMaster-Carr", "partNumber" : "90760A005", "url" : "https://www.mcmaster.com/90760A005/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL }
                },
                FastenerSize.NO_6_32 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.3125 * inch, "height" : 0.171875 * inch, "hexHeight" : 0.143229 * inch, "insertDiameter" : 0.183127 * inch, "name" : "#6-32 Nylock Nut", "vendor" : "McMaster-Carr", "partNumber" : "90631A007", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.3125 * inch, "height" : 0.125 * inch, "hexHeight" : 0.104167 * inch, "insertDiameter" : 0.190939 * inch, "name" : "#6-32 Thin Nylock Nut", "vendor" : "McMaster-Carr", "partNumber" : "90633A007", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.HEX : { "acrossFlats" : 0.3125 * inch, "height" : 0.109375 * inch, "name" : "#6-32 Hex Nut", "vendor" : "McMaster-Carr", "partNumber" : "90480A007", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.THIN_HEX : { "acrossFlats" : 0.25 * inch, "height" : 0.09375 * inch, "name" : "#6-32 Thin Hex Nut", "vendor" : "McMaster-Carr", "partNumber" : "90760A007", "material" : STEEL, "appearance" : ZINC_COATED_STEEL }
                },
                FastenerSize.NO_8_32 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.34375 * inch, "height" : 0.203 * inch, "hexHeight" : 0.169167 * inch, "insertDiameter" : 0.209687 * inch, "name" : "#8-32 Nylock Nut", "vendor" : "McMaster-Carr", "partNumber" : "90631A009", "url" : "https://www.mcmaster.com/90631A009/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.34375 * inch, "height" : 0.141 * inch, "hexHeight" : 0.1175 * inch, "insertDiameter" : 0.209687 * inch, "name" : "#8-32 Thin Nylock Nut", "vendor" : "McMaster-Carr", "partNumber" : "90633A009", "url" : "https://www.mcmaster.com/90633A009/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.HEX : { "acrossFlats" : 0.34375 * inch, "height" : 0.125 * inch, "name" : "#8-32 Hex Nut", "vendor" : "McMaster-Carr", "partNumber" : "90480A009", "url" : "https://www.mcmaster.com/90480A009/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.SQUARE : { "acrossFlats" : 0.34375 * inch, "height" : 0.125 * inch, "name" : "#8-32 Square Nut", "vendor" : "McMaster-Carr", "partNumber" : "94855A216", "url" : "https://www.mcmaster.com/94855A216/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL }
                },
                FastenerSize.NO_10_32 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.375 * inch, "height" : 0.234375 * inch, "hexHeight" : 0.195312 * inch, "insertDiameter" : 0.228966 * inch, "name" : "#10-32 Nylock Nut", "vendor" : "McMaster-Carr", "partNumber" : "90631A411", "url" : "https://www.mcmaster.com/90631A411/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.375 * inch, "height" : 0.171875 * inch, "hexHeight" : 0.132812 * inch, "insertDiameter" : 0.228966 * inch, "name" : "#10-32 Thin Nylock Nut", "vendor" : "McMaster-Carr", "partNumber" : "90633A411", "url" : "https://www.mcmaster.com/90633A411/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.HEX : { "acrossFlats" : 0.375 * inch, "height" : 0.125 * inch, "name" : "#10-32 Hex Nut", "vendor" : "McMaster-Carr", "partNumber" : "90480A195", "url" : "https://www.mcmaster.com/90480A195/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.SQUARE : { "acrossFlats" : 0.375 * inch, "height" : 0.125 * inch, "name" : "#10-32 Square Nut", "vendor" : "McMaster-Carr", "partNumber" : "94855A283", "url" : "https://www.mcmaster.com/94855A283/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL }
                },
                FastenerSize.QUARTER_20 : {
                    NutType.NYLOCK : { "acrossFlats" : 0.4375 * inch, "height" : 0.312 * inch, "hexHeight" : 0.26 * inch, "insertDiameter" : 0.266875 * inch, "name" : "1/4-20 Nylock Nut", "vendor" : "McMaster-Carr", "partNumber" : "90640A129", "url" : "https://www.mcmaster.com/90640A129/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.THIN_NYLOCK : { "acrossFlats" : 0.4375 * inch, "height" : 0.203 * inch, "hexHeight" : 0.169167 * inch, "insertDiameter" : 0.266875 * inch, "name" : "1/4-20 Thin Nylock Nut", "vendor" : "McMaster-Carr", "partNumber" : "90566A029", "url" : "https://www.mcmaster.com/90566A029/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.HEX : { "acrossFlats" : 0.4375 * inch, "height" : 0.219 * inch, "name" : "1/4-20 Hex Nut", "vendor" : "McMaster-Carr", "partNumber" : "90494A029", "url" : "https://www.mcmaster.com/90494A029/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL },
                    NutType.SQUARE : { "acrossFlats" : 0.4375 * inch, "height" : 0.21875 * inch, "name" : "1/4-20 Square Nut", "vendor" : "McMaster-Carr", "partNumber" : "90043A029", "url" : "https://www.mcmaster.com/90043A029/", "material" : STEEL, "appearance" : ZINC_COATED_STEEL }
                }
            }
        };

export const WASHERS = {
            FastenerVendor.GENERIC : {
                FastenerSize.NO_4_40 : {
                    WasherType.OD_5_16_X_0_032 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.125 * inch, "outerDiameter" : 0.3125 * inch, "thickness" : 0.032 * inch, "name" : "#4 Washer (5/16 in OD x 0.032 in)", "material" : STEEL, "appearance" : BLACK }
                    }
                },
                FastenerSize.NO_6_32 : {
                    WasherType.OD_3_8_X_0_049 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.156 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.049 * inch, "name" : "#6 Washer (3/8 in OD x 0.049 in)", "material" : STEEL, "appearance" : BLACK }
                    }
                },
                FastenerSize.NO_8_32 : {
                    WasherType.OD_7_16_X_0_049 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.188 * inch, "outerDiameter" : 0.4375 * inch, "thickness" : 0.049 * inch, "name" : "#8 Washer (7/16 in OD x 0.049 in)", "material" : STEEL, "appearance" : BLACK }
                    }
                },
                FastenerSize.NO_10_32 : {
                    WasherType.OD_1_2_X_0_049 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.5 * inch, "thickness" : 0.049 * inch, "name" : "#10 Washer (1/2 in OD x 0.049 in)", "material" : STEEL, "appearance" : BLACK }
                    }
                },
                FastenerSize.QUARTER_20 : {
                    WasherType.OD_5_8_X_0_065 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.281 * inch, "outerDiameter" : 0.625 * inch, "thickness" : 0.065 * inch, "name" : "1/4 in Washer (5/8 in OD x 0.065 in)", "material" : STEEL, "appearance" : BLACK }
                    }
                }
            },
            FastenerVendor.MCMASTER : {
                FastenerSize.NO_4_40 : {
                    WasherType.OD_5_16_X_0_03 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.125 * inch, "outerDiameter" : 0.3125 * inch, "thickness" : 0.03 * inch, "name" : "#4 Washer (5/16 in OD x 0.03 in)", "vendor" : "McMaster-Carr", "partNumber" : "96765A110", "url" : "https://www.mcmaster.com/96765A110/", "material" : STAINLESS_STEEL, "appearance" : BLACK },
                        WasherFinish.STAINLESS : { "innerDiameter" : 0.125 * inch, "outerDiameter" : 0.3125 * inch, "thickness" : 0.03 * inch, "name" : "#4 Washer (5/16 in OD x 0.03 in, Stainless steel)", "vendor" : "McMaster-Carr", "partNumber" : "92141A005", "url" : "https://www.mcmaster.com/92141A005/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                FastenerSize.NO_6_32 : {
                    WasherType.OD_5_16_X_0_03 : {
                        WasherFinish.STAINLESS : { "innerDiameter" : 0.156 * inch, "outerDiameter" : 0.3125 * inch, "thickness" : 0.03 * inch, "name" : "#6 Washer (5/16 in OD x 0.03 in, Stainless steel)", "vendor" : "McMaster-Carr", "partNumber" : "92141A008", "url" : "https://www.mcmaster.com/92141A008/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    },
                    WasherType.OD_3_8_X_0_03 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.156 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.03 * inch, "name" : "#6 Washer (3/8 in OD x 0.03 in)", "vendor" : "McMaster-Carr", "partNumber" : "96765A115", "url" : "https://www.mcmaster.com/96765A115/", "material" : STAINLESS_STEEL, "appearance" : BLACK }
                    }
                },
                FastenerSize.NO_8_32 : {
                    WasherType.OD_3_8_X_0_03 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.188 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.03 * inch, "name" : "#8 Washer (3/8 in OD x 0.03 in)", "vendor" : "McMaster-Carr", "partNumber" : "96765A120", "url" : "https://www.mcmaster.com/96765A120/", "material" : STAINLESS_STEEL, "appearance" : BLACK },
                        WasherFinish.STAINLESS : { "innerDiameter" : 0.188 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.03 * inch, "name" : "#8 Washer (3/8 in OD x 0.03 in, Stainless steel)", "vendor" : "McMaster-Carr", "partNumber" : "92141A009", "url" : "https://www.mcmaster.com/92141A009/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                FastenerSize.NO_10_32 : {
                    WasherType.OD_7_16_X_0_03 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.4375 * inch, "thickness" : 0.03 * inch, "name" : "#10 Washer (7/16 in OD x 0.03 in)", "vendor" : "McMaster-Carr", "partNumber" : "96765A125", "url" : "https://www.mcmaster.com/96765A125/", "material" : STAINLESS_STEEL, "appearance" : BLACK },
                        WasherFinish.STAINLESS : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.4375 * inch, "thickness" : 0.03 * inch, "name" : "#10 Washer (7/16 in OD x 0.03 in, Stainless steel)", "vendor" : "McMaster-Carr", "partNumber" : "92141A011", "url" : "https://www.mcmaster.com/92141A011/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    },
                    WasherType.OD_1_2_X_0_04 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.5 * inch, "thickness" : 0.04 * inch, "name" : "#10 Washer (1/2 in OD x 0.04 in)", "vendor" : "McMaster-Carr", "partNumber" : "90377A123", "url" : "https://www.mcmaster.com/90377A123/", "material" : STAINLESS_STEEL, "appearance" : BLACK }
                    }
                },
                FastenerSize.QUARTER_20 : {
                    WasherType.OD_5_8_X_0_05 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.281 * inch, "outerDiameter" : 0.625 * inch, "thickness" : 0.05 * inch, "name" : "1/4 in Washer (5/8 in OD x 0.05 in)", "vendor" : "McMaster-Carr", "partNumber" : "96765A140", "url" : "https://www.mcmaster.com/96765A140/", "material" : STAINLESS_STEEL, "appearance" : BLACK },
                        WasherFinish.STAINLESS : { "innerDiameter" : 0.281 * inch, "outerDiameter" : 0.625 * inch, "thickness" : 0.05 * inch, "name" : "1/4 in Washer (5/8 in OD x 0.05 in, Stainless steel)", "vendor" : "McMaster-Carr", "partNumber" : "92141A029", "url" : "https://www.mcmaster.com/92141A029/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            },
            FastenerVendor.WCP : {
                FastenerSize.NO_10_32 : {
                    WasherType.OD_3_8_X_0_012 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.012 * inch, "name" : "#10 Washer (3/8 in OD x 0.012 in)", "vendor" : "WCP", "partNumber" : "WCP-0038", "url" : "https://wcproducts.com/products/wcp-0038", "material" : STEEL, "appearance" : BLACK }
                    },
                    WasherType.OD_3_8_X_0_031 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.031 * inch, "name" : "#10 Washer (3/8 in OD x 0.031 in)", "vendor" : "WCP", "partNumber" : "WCP-0197", "url" : "https://wcproducts.com/products/wcp-0197", "material" : STEEL, "appearance" : BLACK }
                    },
                    WasherType.OD_5_8_X_0_031 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.625 * inch, "thickness" : 0.031 * inch, "name" : "#10 Washer (5/8 in OD x 0.031 in)", "vendor" : "WCP", "partNumber" : "WCP-0523", "url" : "https://wcproducts.com/products/wcp-0523", "material" : STEEL, "appearance" : BLACK }
                    }
                },
                FastenerSize.QUARTER_20 : {
                    WasherType.OD_3_8_X_0_012 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.281 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.012 * inch, "name" : "1/4 in Washer (3/8 in OD x 0.012 in)", "vendor" : "WCP", "partNumber" : "WCP-0039", "url" : "https://wcproducts.com/products/wcp-0039", "material" : STEEL, "appearance" : BLACK }
                    },
                    WasherType.OD_3_8_X_0_031 : {
                        WasherFinish.BLACK : { "innerDiameter" : 0.281 * inch, "outerDiameter" : 0.375 * inch, "thickness" : 0.031 * inch, "name" : "1/4 in Washer (3/8 in OD x 0.031 in)", "vendor" : "WCP", "partNumber" : "WCP-0198", "url" : "https://wcproducts.com/products/wcp-0198", "material" : STEEL, "appearance" : BLACK }
                    }
                }
            },
            FastenerVendor.ANDYMARK : {
                FastenerSize.NO_6_32 : {
                    WasherType.OD_5_8_X_0_031 : {
                        WasherFinish.STAINLESS : { "innerDiameter" : 0.156 * inch, "outerDiameter" : 0.625 * inch, "thickness" : 0.031 * inch, "name" : "#6 Washer (5/8 in OD x 0.031 in, Stainless steel)", "vendor" : "AndyMark", "partNumber" : "am-1658", "url" : "https://andymark.com/products/6-x-0-625-in-od-fender-washer", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                FastenerSize.NO_10_32 : {
                    WasherType.OD_3_4_X_0_062 : {
                        WasherFinish.ZINC_PLATED : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 0.75 * inch, "thickness" : 0.062 * inch, "name" : "#10 Washer (3/4 in OD x 0.062 in, Zinc plated)", "vendor" : "AndyMark", "partNumber" : "am-1523", "url" : "https://andymark.com/products/10-x-0-75-in-fender-washer", "material" : STEEL, "appearance" : MEDIUM_GRAY }
                    },
                    WasherType.OD_1_X_0_062 : {
                        WasherFinish.ZINC_PLATED : { "innerDiameter" : 0.219 * inch, "outerDiameter" : 1 * inch, "thickness" : 0.062 * inch, "name" : "#10 Washer (1 in OD x 0.062 in, Zinc plated)", "vendor" : "AndyMark", "partNumber" : "am-1575", "url" : "https://andymark.com/products/10-x-1-in-fender-washer", "material" : STEEL, "appearance" : MEDIUM_GRAY }
                    }
                },
                FastenerSize.QUARTER_20 : {
                    WasherType.OD_5_8_X_0_063 : {
                        WasherFinish.ZINC_PLATED : { "innerDiameter" : 0.281 * inch, "outerDiameter" : 0.625 * inch, "thickness" : 0.063 * inch, "name" : "1/4 in Washer (5/8 in OD x 0.063 in, Zinc plated)", "vendor" : "AndyMark", "partNumber" : "am-1027", "url" : "https://andymark.com/products/1-4-in-flat-washer", "material" : STEEL, "appearance" : MEDIUM_GRAY }
                    },
                    WasherType.OD_1_X_0_062 : {
                        WasherFinish.ZINC_PLATED : { "innerDiameter" : 0.281 * inch, "outerDiameter" : 1 * inch, "thickness" : 0.062 * inch, "name" : "1/4 in Washer (1 in OD x 0.062 in, Zinc plated)", "vendor" : "AndyMark", "partNumber" : "am-1421", "url" : "https://andymark.com/products/1-4-in-x-1-in-fender-washer", "material" : STEEL, "appearance" : MEDIUM_GRAY }
                    },
                    WasherType.OD_3_X_0_06 : {
                        WasherFinish.ZINC_PLATED : { "innerDiameter" : 0.252 * inch, "outerDiameter" : 3 * inch, "thickness" : 0.06 * inch, "name" : "1/4 in Washer (3 in OD x 0.06 in, Zinc plated)", "vendor" : "AndyMark", "partNumber" : "am-1628", "url" : "https://andymark.com/products/1-4-in-id-3-in-od-fender-washer", "material" : STEEL, "appearance" : MEDIUM_GRAY }
                    }
                }
            }
        };
