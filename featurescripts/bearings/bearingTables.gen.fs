FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "core/robotProperties.fs", version : ""); // core/robotProperties.fs

/* Generated from bearingTables.py by `fs gen` -- DO NOT EDIT */

export enum BearingVendor
{
    annotation { "Name" : "Generic" }
    GENERIC,
    annotation { "Name" : "WCP" }
    WCP,
    annotation { "Name" : "AndyMark" }
    ANDYMARK,
    annotation { "Name" : "REV" }
    REV,
    annotation { "Name" : "ThriftyBot" }
    THRIFTYBOT,
    annotation { "Name" : "Swyft" }
    SWYFT,
    annotation { "Name" : "SDS" }
    SDS,
    annotation { "Name" : "McMaster-Carr" }
    MCMASTER,
    annotation { "Name" : "goBILDA" }
    GOBILDA
}

export enum BearingType
{
    annotation { "Name" : "Flanged" }
    FLANGED,
    annotation { "Name" : "Radial" }
    RADIAL,
    annotation { "Name" : "Bushing" }
    BUSHING
}

export enum BearingBore
{
    annotation { "Name" : "Hex" }
    HEX,
    annotation { "Name" : "Rounded hex" }
    ROUNDED_HEX,
    annotation { "Name" : "Round" }
    ROUND,
    annotation { "Name" : "REX" }
    REX
}

export enum BearingSize
{
    annotation { "Name" : "1/2 in" }
    IN_1_2,
    annotation { "Name" : "3/8 in" }
    IN_3_8,
    annotation { "Name" : "3/16 in" }
    IN_3_16,
    annotation { "Name" : "#10 (0.19 in)" }
    NO_10,
    annotation { "Name" : "1/4 in" }
    IN_1_4,
    annotation { "Name" : "5/8 in" }
    IN_5_8,
    annotation { "Name" : "3/4 in" }
    IN_3_4,
    annotation { "Name" : "4 mm" }
    MM_4,
    annotation { "Name" : "5 mm" }
    MM_5,
    annotation { "Name" : "6 mm" }
    MM_6,
    annotation { "Name" : "7 mm" }
    MM_7,
    annotation { "Name" : "8 mm" }
    MM_8,
    annotation { "Name" : "9 mm" }
    MM_9,
    annotation { "Name" : "10 mm" }
    MM_10,
    annotation { "Name" : "12 mm" }
    MM_12,
    annotation { "Name" : "15 mm" }
    MM_15,
    annotation { "Name" : "17 mm" }
    MM_17,
    annotation { "Name" : "25 mm" }
    MM_25,
    annotation { "Name" : "30 mm" }
    MM_30,
    annotation { "Name" : "32 mm" }
    MM_32,
    annotation { "Name" : "35 mm" }
    MM_35,
    annotation { "Name" : "80 mm" }
    MM_80,
    annotation { "Name" : "120 mm" }
    MM_120
}

export enum BearingDimensions
{
    annotation { "Name" : "0.375 in OD x 0.125 in" }
    OD_0_375_X_0_125,
    annotation { "Name" : "0.375 in OD x 0.156 in (extended inner race)" }
    OD_0_375_X_0_156_EXTENDED,
    annotation { "Name" : "0.5 in OD x 0.156 in" }
    OD_0_5_X_0_156,
    annotation { "Name" : "0.5 in OD x 0.188 in" }
    OD_0_5_X_0_188,
    annotation { "Name" : "0.5 in OD x 0.196 in" }
    OD_0_5_X_0_196,
    annotation { "Name" : "0.625 in OD x 0.196 in" }
    OD_0_625_X_0_196,
    annotation { "Name" : "0.75 in OD x 0.281 in" }
    OD_0_75_X_0_281,
    annotation { "Name" : "0.875 in OD x 0.219 in" }
    OD_0_875_X_0_219,
    annotation { "Name" : "0.875 in OD x 0.281 in" }
    OD_0_875_X_0_281,
    annotation { "Name" : "1 in OD x 0.281 in" }
    OD_1_X_0_281,
    annotation { "Name" : "1.125 in OD x 0.25 in" }
    OD_1_125_X_0_25,
    annotation { "Name" : "1.125 in OD x 0.313 in" }
    OD_1_125_X_0_313,
    annotation { "Name" : "1.125 in OD x 0.375 in" }
    OD_1_125_X_0_375,
    annotation { "Name" : "1.375 in OD x 0.438 in" }
    OD_1_375_X_0_438,
    annotation { "Name" : "8 mm OD x 2 mm" }
    MM_8_X_2,
    annotation { "Name" : "8 mm OD x 2.5 mm" }
    MM_8_X_2_5,
    annotation { "Name" : "9 mm OD x 2.5 mm" }
    MM_9_X_2_5,
    annotation { "Name" : "9 mm OD x 3 mm" }
    MM_9_X_3,
    annotation { "Name" : "9 mm OD x 5 mm" }
    MM_9_X_5,
    annotation { "Name" : "9 mm OD x 11 mm" }
    MM_9_X_11,
    annotation { "Name" : "10 mm OD x 2.5 mm" }
    MM_10_X_2_5,
    annotation { "Name" : "10 mm OD x 3 mm" }
    MM_10_X_3,
    annotation { "Name" : "10 mm OD x 4 mm" }
    MM_10_X_4,
    annotation { "Name" : "11 mm OD x 3 mm" }
    MM_11_X_3,
    annotation { "Name" : "11 mm OD x 4 mm" }
    MM_11_X_4,
    annotation { "Name" : "11 mm OD x 5 mm" }
    MM_11_X_5,
    annotation { "Name" : "12 mm OD x 3.5 mm" }
    MM_12_X_3_5,
    annotation { "Name" : "12 mm OD x 4 mm" }
    MM_12_X_4,
    annotation { "Name" : "13 mm OD x 3.5 mm" }
    MM_13_X_3_5,
    annotation { "Name" : "13 mm OD x 4 mm" }
    MM_13_X_4,
    annotation { "Name" : "13 mm OD x 5 mm" }
    MM_13_X_5,
    annotation { "Name" : "14 mm OD x 4 mm" }
    MM_14_X_4,
    annotation { "Name" : "14 mm OD x 5 mm" }
    MM_14_X_5,
    annotation { "Name" : "15 mm OD x 5 mm" }
    MM_15_X_5,
    annotation { "Name" : "16 mm OD x 4 mm" }
    MM_16_X_4,
    annotation { "Name" : "16 mm OD x 5 mm" }
    MM_16_X_5,
    annotation { "Name" : "16 mm OD x 6.3 mm" }
    MM_16_X_6_3,
    annotation { "Name" : "17 mm OD x 5 mm" }
    MM_17_X_5,
    annotation { "Name" : "17 mm OD x 6 mm" }
    MM_17_X_6,
    annotation { "Name" : "19 mm OD x 6 mm" }
    MM_19_X_6,
    annotation { "Name" : "21 mm OD x 4 mm" }
    MM_21_X_4,
    annotation { "Name" : "21 mm OD x 5 mm" }
    MM_21_X_5,
    annotation { "Name" : "22 mm OD x 7 mm" }
    MM_22_X_7,
    annotation { "Name" : "23 mm OD x 4 mm" }
    MM_23_X_4,
    annotation { "Name" : "24 mm OD x 5 mm" }
    MM_24_X_5,
    annotation { "Name" : "24 mm OD x 6 mm" }
    MM_24_X_6,
    annotation { "Name" : "26 mm OD x 5 mm" }
    MM_26_X_5,
    annotation { "Name" : "26 mm OD x 8 mm" }
    MM_26_X_8,
    annotation { "Name" : "28 mm OD x 7 mm" }
    MM_28_X_7,
    annotation { "Name" : "28 mm OD x 8 mm" }
    MM_28_X_8,
    annotation { "Name" : "32 mm OD x 10 mm" }
    MM_32_X_10,
    annotation { "Name" : "37 mm OD x 7 mm" }
    MM_37_X_7,
    annotation { "Name" : "39 mm OD x 5 mm" }
    MM_39_X_5,
    annotation { "Name" : "42 mm OD x 7 mm" }
    MM_42_X_7,
    annotation { "Name" : "47 mm OD x 7 mm" }
    MM_47_X_7,
    annotation { "Name" : "100 mm OD x 10 mm" }
    MM_100_X_10,
    annotation { "Name" : "136 mm OD x 8 mm" }
    MM_136_X_8
}

export enum BearingSeal
{
    annotation { "Name" : "Shielded" }
    SHIELDED,
    annotation { "Name" : "Sealed" }
    SEALED,
    annotation { "Name" : "Open" }
    OPEN,
    annotation { "Name" : "None" }
    NONE
}

/** Each bore's shape: a round bore's `diameter`, or a hex's `acrossFlats`, with its corners rounded to `diameter` where they are. */
export const BEARING_BORES = {
    BearingBore.HEX : {
        BearingSize.IN_1_2 : { "acrossFlats" : 0.5 * inch },
        BearingSize.IN_3_8 : { "acrossFlats" : 0.375 * inch },
        BearingSize.MM_5 : { "acrossFlats" : 5 * millimeter }
    },
    BearingBore.ROUNDED_HEX : {
        BearingSize.IN_1_2 : { "acrossFlats" : 0.5 * inch, "diameter" : 13.75 * millimeter },
        BearingSize.IN_3_8 : { "acrossFlats" : 0.375 * inch, "diameter" : 10.25 * millimeter }
    },
    BearingBore.ROUND : {
        BearingSize.IN_1_2 : { "diameter" : 0.5 * inch },
        BearingSize.IN_3_8 : { "diameter" : 0.375 * inch },
        BearingSize.IN_3_16 : { "diameter" : 0.1875 * inch },
        BearingSize.NO_10 : { "diameter" : 0.19 * inch },
        BearingSize.IN_1_4 : { "diameter" : 0.25 * inch },
        BearingSize.IN_5_8 : { "diameter" : 0.625 * inch },
        BearingSize.MM_4 : { "diameter" : 4 * millimeter },
        BearingSize.MM_5 : { "diameter" : 5 * millimeter },
        BearingSize.MM_6 : { "diameter" : 6 * millimeter },
        BearingSize.MM_7 : { "diameter" : 7 * millimeter },
        BearingSize.MM_8 : { "diameter" : 8 * millimeter },
        BearingSize.MM_9 : { "diameter" : 9 * millimeter },
        BearingSize.MM_10 : { "diameter" : 10 * millimeter },
        BearingSize.MM_12 : { "diameter" : 12 * millimeter },
        BearingSize.MM_15 : { "diameter" : 15 * millimeter },
        BearingSize.MM_17 : { "diameter" : 17 * millimeter },
        BearingSize.MM_25 : { "diameter" : 25 * millimeter },
        BearingSize.MM_30 : { "diameter" : 30 * millimeter },
        BearingSize.MM_32 : { "diameter" : 32 * millimeter },
        BearingSize.MM_35 : { "diameter" : 35 * millimeter },
        BearingSize.MM_80 : { "diameter" : 80 * millimeter },
        BearingSize.MM_120 : { "diameter" : 120 * millimeter }
    },
    BearingBore.REX : {
        BearingSize.MM_8 : { "acrossFlats" : 7 * millimeter }
    }
};

/** What each vendor makes, by vendor, type, bore, size, dimensions, and seal: each bearing's dimensions, a flanged one's flange, and its properties. */
export const BEARINGS = {
    BearingVendor.GENERIC : {
        BearingType.FLANGED : {
            BearingBore.HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "partNumber" : "FR8ZZ-HexHD", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Hex Flanged Bearing (0.875 in OD x 0.281 in, Sealed)", "partNumber" : "FR62RS", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/8 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "partNumber" : "FR6ZZL-Hex", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            },
            BearingBore.ROUNDED_HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1 * inch, "width" : 0.281 * inch, "flangeDiameter" : 1.06 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1 in OD x 0.281 in)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "partNumber" : "FR8ZZ", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.SEALED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in, Sealed)", "partNumber" : "FR82RS", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_219 : {
                        BearingSeal.OPEN : { "outerDiameter" : 0.875 * inch, "width" : 0.219 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.062 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.219 in, Open)", "partNumber" : "FR6", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)", "partNumber" : "FR6ZZ", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in, Sealed)", "partNumber" : "FR62RS", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1 * inch, "width" : 0.281 * inch, "flangeDiameter" : 1.06 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (1 in OD x 0.281 in)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/8 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "partNumber" : "FR6ZZ-L", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_16 : {
                    BearingDimensions.OD_0_5_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.196 * inch, "flangeDiameter" : 0.565 * inch, "flangeThickness" : 0.042 * inch, "name" : "3/16 in Round Flanged Bearing (0.5 in OD x 0.196 in)", "partNumber" : "FR3ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.NO_10 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "0.19 in Round Flanged Bearing (0.875 in OD x 0.281 in)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_5_X_0_188 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.188 * inch, "flangeDiameter" : 0.565 * inch, "flangeThickness" : 0.042 * inch, "name" : "1/4 in Round Flanged Bearing (0.5 in OD x 0.188 in)", "partNumber" : "FR188ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_625_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.625 * inch, "width" : 0.196 * inch, "flangeDiameter" : 0.69 * inch, "flangeThickness" : 0.042 * inch, "name" : "1/4 in Round Flanged Bearing (0.625 in OD x 0.196 in)", "partNumber" : "FR4ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_5 : {
                    BearingDimensions.MM_8_X_2 : {
                        BearingSeal.OPEN : { "outerDiameter" : 8 * millimeter, "width" : 2 * millimeter, "flangeDiameter" : 9.2 * millimeter, "flangeThickness" : 0.6 * millimeter, "name" : "5 mm Round Flanged Bearing (8 mm OD x 2 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_9_X_2_5 : {
                        BearingSeal.OPEN : { "outerDiameter" : 9 * millimeter, "width" : 2.5 * millimeter, "flangeDiameter" : 10.2 * millimeter, "flangeThickness" : 0.6 * millimeter, "name" : "5 mm Round Flanged Bearing (9 mm OD x 2.5 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_9_X_3 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 9 * millimeter, "width" : 3 * millimeter, "flangeDiameter" : 10.2 * millimeter, "flangeThickness" : 0.6 * millimeter, "name" : "5 mm Round Flanged Bearing (9 mm OD x 3 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_10_X_3 : {
                        BearingSeal.OPEN : { "outerDiameter" : 10 * millimeter, "width" : 3 * millimeter, "flangeDiameter" : 11.2 * millimeter, "flangeThickness" : 0.6 * millimeter, "name" : "5 mm Round Flanged Bearing (10 mm OD x 3 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_10_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 10 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 11.6 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "5 mm Round Flanged Bearing (10 mm OD x 4 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_11_X_3 : {
                        BearingSeal.OPEN : { "outerDiameter" : 11 * millimeter, "width" : 3 * millimeter, "flangeDiameter" : 12.5 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "5 mm Round Flanged Bearing (11 mm OD x 3 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_11_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 11 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 12.5 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "5 mm Round Flanged Bearing (11 mm OD x 4 mm)", "partNumber" : "MF115ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_11_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 11 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 12.5 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (11 mm OD x 5 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_13_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 13 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 15 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (13 mm OD x 4 mm)", "partNumber" : "F695ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 16 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (14 mm OD x 5 mm)", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 16 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (14 mm OD x 5 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_16_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (16 mm OD x 5 mm)", "partNumber" : "F625ZZ", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (16 mm OD x 5 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "5 mm Round Flanged Bearing (19 mm OD x 6 mm)", "partNumber" : "F1950ZZ", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "5 mm Round Flanged Bearing (19 mm OD x 6 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_6 : {
                    BearingDimensions.MM_10_X_2_5 : {
                        BearingSeal.OPEN : { "outerDiameter" : 10 * millimeter, "width" : 2.5 * millimeter, "flangeDiameter" : 11.2 * millimeter, "flangeThickness" : 0.6 * millimeter, "name" : "6 mm Round Flanged Bearing (10 mm OD x 2.5 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_10_X_3 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 10 * millimeter, "width" : 3 * millimeter, "flangeDiameter" : 11.2 * millimeter, "flangeThickness" : 0.6 * millimeter, "name" : "6 mm Round Flanged Bearing (10 mm OD x 3 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_12_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 12 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 13.5 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "6 mm Round Flanged Bearing (12 mm OD x 4 mm)", "partNumber" : "F126ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_13_X_3_5 : {
                        BearingSeal.OPEN : { "outerDiameter" : 13 * millimeter, "width" : 3.5 * millimeter, "flangeDiameter" : 15 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "6 mm Round Flanged Bearing (13 mm OD x 3.5 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_13_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 13 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 15 * millimeter, "flangeThickness" : 1.1 * millimeter, "name" : "6 mm Round Flanged Bearing (13 mm OD x 5 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_15_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 15 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 17 * millimeter, "flangeThickness" : 1.2 * millimeter, "name" : "6 mm Round Flanged Bearing (15 mm OD x 5 mm)", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 15 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 17 * millimeter, "flangeThickness" : 1.2 * millimeter, "name" : "6 mm Round Flanged Bearing (15 mm OD x 5 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_17_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 17 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 19 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "6 mm Round Flanged Bearing (17 mm OD x 6 mm)", "partNumber" : "F606ZZ", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 17 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 19 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "6 mm Round Flanged Bearing (17 mm OD x 6 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "6 mm Round Flanged Bearing (19 mm OD x 6 mm)", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "6 mm Round Flanged Bearing (19 mm OD x 6 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_14_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 15.6 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "8 mm Round Flanged Bearing (14 mm OD x 4 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 15 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "8 mm Round Flanged Bearing (14 mm OD x 5 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_16_X_4 : {
                        BearingSeal.OPEN : { "outerDiameter" : 16 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "8 mm Round Flanged Bearing (16 mm OD x 4 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_16_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1.1 * millimeter, "name" : "8 mm Round Flanged Bearing (16 mm OD x 5 mm)", "partNumber" : "W688AZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_16_X_6_3 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 6.3 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1.3 * millimeter, "name" : "8 mm Round Flanged Bearing (16 mm OD x 6.3 mm)", "partNumber" : "F688Z", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "8 mm Round Flanged Bearing (19 mm OD x 6 mm)", "partNumber" : "F698ZZ", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "8 mm Round Flanged Bearing (19 mm OD x 6 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_22_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 25 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "8 mm Round Flanged Bearing (22 mm OD x 7 mm)", "partNumber" : "F608ZZ", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 25 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "8 mm Round Flanged Bearing (22 mm OD x 7 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "8 mm Round Flanged Bearing (0.875 in OD x 0.281 in)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_9 : {
                    BearingDimensions.MM_17_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 17 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 19 * millimeter, "flangeThickness" : 1.1 * millimeter, "name" : "9 mm Round Flanged Bearing (17 mm OD x 5 mm)", "partNumber" : "F689ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_12 : {
                    BearingDimensions.MM_24_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 24 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 26.5 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "12 mm Round Flanged Bearing (24 mm OD x 6 mm)", "partNumber" : "F6901ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_15 : {
                    BearingDimensions.MM_21_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 21 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 22.5 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "15 mm Round Flanged Bearing (21 mm OD x 4 mm)", "partNumber" : "F6702ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_25 : {
                    BearingDimensions.MM_37_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 37 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 40 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "25 mm Round Flanged Bearing (37 mm OD x 7 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_30 : {
                    BearingDimensions.MM_42_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 42 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 45 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "30 mm Round Flanged Bearing (42 mm OD x 7 mm)", "partNumber" : "F6806ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_35 : {
                    BearingDimensions.MM_47_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 50 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm)", "partNumber" : "F6807ZZ", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.SEALED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 50 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm, Sealed)", "partNumber" : "F68072RS", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SEALED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Hex Radial Bearing (1.125 in OD x 0.313 in, Sealed)", "partNumber" : "R82RS-Hex", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Hex Radial Bearing (0.875 in OD x 0.281 in, Sealed)", "partNumber" : "R62RS-Hex", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "1/2 in Round Radial Bearing (0.875 in OD x 0.281 in)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_125_X_0_25 : {
                        BearingSeal.OPEN : { "outerDiameter" : 1.125 * inch, "width" : 0.25 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.25 in, Open)", "partNumber" : "R8", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)", "partNumber" : "R8ZZ", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.SEALED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in, Sealed)", "partNumber" : "R82RS", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_375_X_0_438 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.375 * inch, "width" : 0.438 * inch, "name" : "1/2 in Round Radial Bearing (1.375 in OD x 0.438 in)", "partNumber" : "1621ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_219 : {
                        BearingSeal.OPEN : { "outerDiameter" : 0.875 * inch, "width" : 0.219 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.219 in, Open)", "partNumber" : "R6", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)", "partNumber" : "R6ZZ", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in, Sealed)", "partNumber" : "R62RS", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_125_X_0_375 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.375 * inch, "name" : "3/8 in Round Radial Bearing (1.125 in OD x 0.375 in)", "partNumber" : "1614ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_16 : {
                    BearingDimensions.OD_0_5_X_0_156 : {
                        BearingSeal.OPEN : { "outerDiameter" : 0.5 * inch, "width" : 0.156 * inch, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.156 in, Open)", "partNumber" : "R3", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_5_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.196 * inch, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.196 in)", "partNumber" : "R3ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.NO_10 : {
                    BearingDimensions.OD_0_375_X_0_156_EXTENDED : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.375 * inch, "width" : 0.156 * inch, "outerWidth" : 0.125 * inch, "name" : "0.19 in Round Radial Bearing (0.375 in OD x 0.156 in (extended inner race))", "partNumber" : "R166ZZEE", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_375_X_0_125 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.375 * inch, "width" : 0.125 * inch, "name" : "1/4 in Round Radial Bearing (0.375 in OD x 0.125 in)", "partNumber" : "R168ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_5_X_0_188 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.188 * inch, "name" : "1/4 in Round Radial Bearing (0.5 in OD x 0.188 in)", "partNumber" : "R188ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_625_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.625 * inch, "width" : 0.196 * inch, "name" : "1/4 in Round Radial Bearing (0.625 in OD x 0.196 in)", "partNumber" : "R4ZZ", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 0.625 * inch, "width" : 0.196 * inch, "name" : "1/4 in Round Radial Bearing (0.625 in OD x 0.196 in, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_75_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.75 * inch, "width" : 0.281 * inch, "name" : "1/4 in Round Radial Bearing (0.75 in OD x 0.281 in)", "partNumber" : "RA4ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_5_8 : {
                    BearingDimensions.OD_1_375_X_0_438 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.375 * inch, "width" : 0.438 * inch, "name" : "5/8 in Round Radial Bearing (1.375 in OD x 0.438 in)", "partNumber" : "1623ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_5 : {
                    BearingDimensions.MM_8_X_2_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 8 * millimeter, "width" : 2.5 * millimeter, "name" : "5 mm Round Radial Bearing (8 mm OD x 2.5 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_9_X_3 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 9 * millimeter, "width" : 3 * millimeter, "name" : "5 mm Round Radial Bearing (9 mm OD x 3 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_10_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 10 * millimeter, "width" : 4 * millimeter, "name" : "5 mm Round Radial Bearing (10 mm OD x 4 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_11_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 11 * millimeter, "width" : 4 * millimeter, "name" : "5 mm Round Radial Bearing (11 mm OD x 4 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_13_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 13 * millimeter, "width" : 4 * millimeter, "name" : "5 mm Round Radial Bearing (13 mm OD x 4 mm)", "partNumber" : "695ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "name" : "5 mm Round Radial Bearing (14 mm OD x 5 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_16_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "name" : "5 mm Round Radial Bearing (16 mm OD x 5 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "name" : "5 mm Round Radial Bearing (19 mm OD x 6 mm)", "partNumber" : "635ZZ", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "name" : "5 mm Round Radial Bearing (19 mm OD x 6 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_6 : {
                    BearingDimensions.MM_12_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 12 * millimeter, "width" : 4 * millimeter, "name" : "6 mm Round Radial Bearing (12 mm OD x 4 mm)", "partNumber" : "126ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_17_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 17 * millimeter, "width" : 6 * millimeter, "name" : "6 mm Round Radial Bearing (17 mm OD x 6 mm)", "partNumber" : "606ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_7 : {
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "name" : "7 mm Round Radial Bearing (14 mm OD x 5 mm)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "name" : "7 mm Round Radial Bearing (19 mm OD x 6 mm)", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.SEALED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "name" : "7 mm Round Radial Bearing (19 mm OD x 6 mm, Sealed)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_22_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "name" : "7 mm Round Radial Bearing (22 mm OD x 7 mm)", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.SEALED : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "name" : "7 mm Round Radial Bearing (22 mm OD x 7 mm, Sealed)", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "name" : "7 mm Round Radial Bearing (22 mm OD x 7 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_16_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "name" : "8 mm Round Radial Bearing (16 mm OD x 5 mm)", "partNumber" : "688ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "name" : "8 mm Round Radial Bearing (19 mm OD x 6 mm)", "partNumber" : "R1980ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_22_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "name" : "8 mm Round Radial Bearing (22 mm OD x 7 mm)", "partNumber" : "608ZZ", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "name" : "8 mm Round Radial Bearing (22 mm OD x 7 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_10 : {
                    BearingDimensions.MM_26_X_8 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 26 * millimeter, "width" : 8 * millimeter, "name" : "10 mm Round Radial Bearing (26 mm OD x 8 mm)", "partNumber" : "6000ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_12 : {
                    BearingDimensions.MM_21_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 21 * millimeter, "width" : 5 * millimeter, "name" : "12 mm Round Radial Bearing (21 mm OD x 5 mm)", "partNumber" : "6801ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_28_X_8 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 28 * millimeter, "width" : 8 * millimeter, "name" : "12 mm Round Radial Bearing (28 mm OD x 8 mm)", "partNumber" : "6001ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_15 : {
                    BearingDimensions.MM_21_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 21 * millimeter, "width" : 4 * millimeter, "name" : "15 mm Round Radial Bearing (21 mm OD x 4 mm)", "partNumber" : "6702ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_24_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 24 * millimeter, "width" : 5 * millimeter, "name" : "15 mm Round Radial Bearing (24 mm OD x 5 mm)", "partNumber" : "6802ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_28_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 28 * millimeter, "width" : 7 * millimeter, "name" : "15 mm Round Radial Bearing (28 mm OD x 7 mm)", "partNumber" : "6902ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_17 : {
                    BearingDimensions.MM_23_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 23 * millimeter, "width" : 4 * millimeter, "name" : "17 mm Round Radial Bearing (23 mm OD x 4 mm)", "partNumber" : "6703ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_26_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 26 * millimeter, "width" : 5 * millimeter, "name" : "17 mm Round Radial Bearing (26 mm OD x 5 mm)", "partNumber" : "6803ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_25 : {
                    BearingDimensions.MM_37_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 37 * millimeter, "width" : 7 * millimeter, "name" : "25 mm Round Radial Bearing (37 mm OD x 7 mm)", "partNumber" : "6805ZZ", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 37 * millimeter, "width" : 7 * millimeter, "name" : "25 mm Round Radial Bearing (37 mm OD x 7 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_30 : {
                    BearingDimensions.MM_42_X_7 : {
                        BearingSeal.OPEN : { "outerDiameter" : 42 * millimeter, "width" : 7 * millimeter, "name" : "30 mm Round Radial Bearing (42 mm OD x 7 mm, Open)", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_35 : {
                    BearingDimensions.MM_47_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "name" : "35 mm Round Radial Bearing (47 mm OD x 7 mm)", "partNumber" : "6807ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_80 : {
                    BearingDimensions.MM_100_X_10 : {
                        BearingSeal.SEALED : { "outerDiameter" : 100 * millimeter, "width" : 10 * millimeter, "name" : "80 mm Round Radial Bearing (100 mm OD x 10 mm, Sealed)", "partNumber" : "68162RS", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        }
    },
    BearingVendor.WCP : {
        BearingType.FLANGED : {
            BearingBore.HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "WCP", "partNumber" : "WCP-0783", "url" : "https://wcproducts.com/products/wcp-0783", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/8 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "WCP", "partNumber" : "WCP-1507", "url" : "https://wcproducts.com/products/wcp-1507", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            },
            BearingBore.ROUNDED_HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-2057", "url" : "https://wcproducts.com/products/wcp-2057", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1 * inch, "width" : 0.281 * inch, "flangeDiameter" : 1.06 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-2058", "url" : "https://wcproducts.com/products/wcp-2058", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "WCP", "partNumber" : "WCP-0785", "url" : "https://wcproducts.com/products/wcp-0785", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-0784", "url" : "https://wcproducts.com/products/wcp-0784", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "WCP", "partNumber" : "WCP-0781", "url" : "https://wcproducts.com/products/wcp-0781", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-0779", "url" : "https://wcproducts.com/products/wcp-0779", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1 * inch, "width" : 0.281 * inch, "flangeDiameter" : 1.06 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (1 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-2060", "url" : "https://wcproducts.com/products/wcp-2060", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/8 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "WCP", "partNumber" : "WCP-0780", "url" : "https://wcproducts.com/products/wcp-0780", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_5_X_0_188 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.188 * inch, "flangeDiameter" : 0.565 * inch, "flangeThickness" : 0.042 * inch, "name" : "1/4 in Round Flanged Bearing (0.5 in OD x 0.188 in)", "vendor" : "WCP", "partNumber" : "WCP-0072", "url" : "https://wcproducts.com/products/wcp-0072", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_625_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.625 * inch, "width" : 0.196 * inch, "flangeDiameter" : 0.69 * inch, "flangeThickness" : 0.042 * inch, "name" : "1/4 in Round Flanged Bearing (0.625 in OD x 0.196 in)", "vendor" : "WCP", "partNumber" : "WCP-0042", "url" : "https://wcproducts.com/products/wcp-0042", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_16_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1.1 * millimeter, "name" : "8 mm Round Flanged Bearing (16 mm OD x 5 mm)", "vendor" : "WCP", "partNumber" : "WCP-1508", "url" : "https://wcproducts.com/products/wcp-1508", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "8 mm Round Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-2054", "url" : "https://wcproducts.com/products/wcp-2054", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_12 : {
                    BearingDimensions.MM_24_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 24 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 26.5 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "12 mm Round Flanged Bearing (24 mm OD x 6 mm)", "vendor" : "WCP", "partNumber" : "WCP-1734", "url" : "https://wcproducts.com/products/wcp-1734", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_30 : {
                    BearingDimensions.MM_42_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 42 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 45 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "30 mm Round Flanged Bearing (42 mm OD x 7 mm)", "vendor" : "WCP", "partNumber" : "WCP-0299", "url" : "https://wcproducts.com/products/wcp-0299", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_35 : {
                    BearingDimensions.MM_47_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 50 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm)", "vendor" : "WCP", "partNumber" : "WCP-1471", "url" : "https://wcproducts.com/products/wcp-1471", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "1/2 in Round Radial Bearing (0.875 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-0777", "url" : "https://wcproducts.com/products/wcp-0777", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)", "vendor" : "WCP", "partNumber" : "WCP-0778", "url" : "https://wcproducts.com/products/wcp-0778", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-0776", "url" : "https://wcproducts.com/products/wcp-0776", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_16 : {
                    BearingDimensions.OD_0_5_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.196 * inch, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.196 in)", "vendor" : "WCP", "partNumber" : "WCP-0773", "url" : "https://wcproducts.com/products/wcp-0773", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_375_X_0_125 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.375 * inch, "width" : 0.125 * inch, "name" : "1/4 in Round Radial Bearing (0.375 in OD x 0.125 in)", "vendor" : "WCP", "partNumber" : "WCP-0774", "url" : "https://wcproducts.com/products/wcp-0774", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_5_X_0_188 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.188 * inch, "name" : "1/4 in Round Radial Bearing (0.5 in OD x 0.188 in)", "vendor" : "WCP", "partNumber" : "WCP-0775", "url" : "https://wcproducts.com/products/wcp-0775", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_625_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.625 * inch, "width" : 0.196 * inch, "name" : "1/4 in Round Radial Bearing (0.625 in OD x 0.196 in)", "vendor" : "WCP", "partNumber" : "WCP-0041", "url" : "https://wcproducts.com/products/wcp-0041", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_75_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.75 * inch, "width" : 0.281 * inch, "name" : "1/4 in Round Radial Bearing (0.75 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-0212", "url" : "https://wcproducts.com/products/wcp-0212", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_6 : {
                    BearingDimensions.MM_12_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 12 * millimeter, "width" : 4 * millimeter, "name" : "6 mm Round Radial Bearing (12 mm OD x 4 mm)", "vendor" : "WCP", "partNumber" : "WCP-0498", "url" : "https://wcproducts.com/products/wcp-0498", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_12 : {
                    BearingDimensions.MM_21_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 21 * millimeter, "width" : 5 * millimeter, "name" : "12 mm Round Radial Bearing (21 mm OD x 5 mm)", "vendor" : "WCP", "partNumber" : "WCP-1756", "url" : "https://wcproducts.com/products/wcp-1756", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_15 : {
                    BearingDimensions.MM_24_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 24 * millimeter, "width" : 5 * millimeter, "name" : "15 mm Round Radial Bearing (24 mm OD x 5 mm)", "vendor" : "WCP", "partNumber" : "WCP-0078", "url" : "https://wcproducts.com/products/wcp-0078", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_28_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 28 * millimeter, "width" : 7 * millimeter, "name" : "15 mm Round Radial Bearing (28 mm OD x 7 mm)", "vendor" : "WCP", "partNumber" : "WCP-0840", "url" : "https://wcproducts.com/products/wcp-0840", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_17 : {
                    BearingDimensions.MM_23_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 23 * millimeter, "width" : 4 * millimeter, "name" : "17 mm Round Radial Bearing (23 mm OD x 4 mm)", "vendor" : "WCP", "partNumber" : "WCP-1660", "url" : "https://wcproducts.com/products/wcp-1660", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_26_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 26 * millimeter, "width" : 5 * millimeter, "name" : "17 mm Round Radial Bearing (26 mm OD x 5 mm)", "vendor" : "WCP", "partNumber" : "WCP-0841", "url" : "https://wcproducts.com/products/wcp-0841", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_25 : {
                    BearingDimensions.MM_37_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 37 * millimeter, "width" : 7 * millimeter, "name" : "25 mm Round Radial Bearing (37 mm OD x 7 mm)", "vendor" : "WCP", "partNumber" : "WCP-0027", "url" : "https://wcproducts.com/products/wcp-0027", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_80 : {
                    BearingDimensions.MM_100_X_10 : {
                        BearingSeal.SEALED : { "outerDiameter" : 100 * millimeter, "width" : 10 * millimeter, "name" : "80 mm Round Radial Bearing (100 mm OD x 10 mm, Sealed)", "vendor" : "WCP", "partNumber" : "WCP-0036", "url" : "https://wcproducts.com/products/wcp-0036", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        }
    },
    BearingVendor.ANDYMARK : {
        BearingType.FLANGED : {
            BearingBore.HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "AndyMark", "partNumber" : "am-2986", "url" : "https://andymark.com/am-2986", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.975 * inch, "flangeThickness" : 0.062 * inch, "name" : "3/8 in Hex Flanged Bearing (0.875 in OD x 0.281 in, Sealed)", "vendor" : "AndyMark", "partNumber" : "am-4489", "url" : "https://andymark.com/am-4489", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "AndyMark", "partNumber" : "am-0692", "url" : "https://andymark.com/am-0692", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "AndyMark", "partNumber" : "am-0030", "url" : "https://andymark.com/am-0030", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.SEALED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in, Sealed)", "vendor" : "AndyMark", "partNumber" : "am-4465", "url" : "https://andymark.com/am-4465", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.975 * inch, "flangeThickness" : 0.062 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "AndyMark", "partNumber" : "am-0028", "url" : "https://andymark.com/am-0028", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.975 * inch, "flangeThickness" : 0.062 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in, Sealed)", "vendor" : "AndyMark", "partNumber" : "am-4463", "url" : "https://andymark.com/am-4463", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "AndyMark", "partNumber" : "am-0573", "url" : "https://andymark.com/am-0573", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_5 : {
                    BearingDimensions.MM_11_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 11 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 12.5 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "5 mm Round Flanged Bearing (11 mm OD x 4 mm)", "vendor" : "AndyMark", "partNumber" : "am-5606", "url" : "https://andymark.com/products/5-mm-id-11-mm-od-shielded-flanged-bearing-mf115zz", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_13_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 13 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 15 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (13 mm OD x 4 mm)", "vendor" : "AndyMark", "partNumber" : "am-5469", "url" : "https://andymark.com/am-5469", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_6 : {
                    BearingDimensions.MM_12_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 12 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 13.6 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "6 mm Round Flanged Bearing (12 mm OD x 4 mm)", "vendor" : "AndyMark", "partNumber" : "am-3377", "url" : "https://andymark.com/products/6-mm-id-12-mm-od-shielded-flanged-bearing-f126zz", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22.1 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "8 mm Round Flanged Bearing (19 mm OD x 6 mm)", "vendor" : "AndyMark", "partNumber" : "am-0931", "url" : "https://andymark.com/am-0931", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_22_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 25 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "8 mm Round Flanged Bearing (22 mm OD x 7 mm)", "vendor" : "AndyMark", "partNumber" : "am-4457", "url" : "https://andymark.com/am-4457", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_15 : {
                    BearingDimensions.MM_21_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 21 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 22.5 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "15 mm Round Flanged Bearing (21 mm OD x 4 mm)", "vendor" : "AndyMark", "partNumber" : "am-5536", "url" : "https://andymark.com/products/15-mm-id-21-mm-od-shielded-flanged-bearing-f6702zz", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SEALED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Hex Radial Bearing (1.125 in OD x 0.313 in, Sealed)", "vendor" : "AndyMark", "partNumber" : "am-4703", "url" : "https://andymark.com/am-4703", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Hex Radial Bearing (0.875 in OD x 0.281 in, Sealed)", "vendor" : "AndyMark", "partNumber" : "am-4702", "url" : "https://andymark.com/am-4702", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)", "vendor" : "AndyMark", "partNumber" : "am-0181", "url" : "https://andymark.com/am-0181", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)", "vendor" : "AndyMark", "partNumber" : "am-0516", "url" : "https://andymark.com/am-0516", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_125_X_0_375 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.375 * inch, "name" : "3/8 in Round Radial Bearing (1.125 in OD x 0.375 in)", "vendor" : "AndyMark", "partNumber" : "am-0209", "url" : "https://andymark.com/am-0209", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_16 : {
                    BearingDimensions.OD_0_5_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.196 * inch, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.196 in)", "vendor" : "AndyMark", "partNumber" : "am-4458", "url" : "https://andymark.com/am-4458", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.NO_10 : {
                    BearingDimensions.OD_0_375_X_0_156_EXTENDED : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.375 * inch, "width" : 0.156 * inch, "outerWidth" : 0.125 * inch, "name" : "0.19 in Round Radial Bearing (0.375 in OD x 0.156 in (extended inner race))", "vendor" : "AndyMark", "partNumber" : "am-3681", "url" : "https://andymark.com/am-3681", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_625_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.625 * inch, "width" : 0.196 * inch, "name" : "1/4 in Round Radial Bearing (0.625 in OD x 0.196 in)", "vendor" : "AndyMark", "partNumber" : "am-4460", "url" : "https://andymark.com/am-4460", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_5 : {
                    BearingDimensions.MM_13_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 13 * millimeter, "width" : 4 * millimeter, "name" : "5 mm Round Radial Bearing (13 mm OD x 4 mm)", "vendor" : "AndyMark", "partNumber" : "am-4724", "url" : "https://andymark.com/products/5-mm-id-13-mm-od-shielded-bearing-695zz", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_6 : {
                    BearingDimensions.MM_17_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 17 * millimeter, "width" : 6 * millimeter, "name" : "6 mm Round Radial Bearing (17 mm OD x 6 mm)", "vendor" : "AndyMark", "partNumber" : "am-4454", "url" : "https://andymark.com/am-4454", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_22_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "name" : "8 mm Round Radial Bearing (22 mm OD x 7 mm)", "vendor" : "AndyMark", "partNumber" : "am-4456", "url" : "https://andymark.com/am-4456", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        }
    },
    BearingVendor.REV : {
        BearingType.FLANGED : {
            BearingBore.HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "REV", "partNumber" : "REV-21-1915", "url" : "https://www.revrobotics.com/search.php?search_query=REV-21-1915&section=product", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            },
            BearingBore.ROUNDED_HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1 * inch, "width" : 0.281 * inch, "flangeDiameter" : 1.0625 * inch, "flangeThickness" : 0.062 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1 in OD x 0.281 in)", "vendor" : "REV", "partNumber" : "REV-21-3197", "url" : "https://www.revrobotics.com/search.php?search_query=REV-21-3197&section=product", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "REV", "partNumber" : "REV-21-1916", "url" : "https://www.revrobotics.com/search.php?search_query=REV-21-1916&section=product", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.NO_10 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.969 * inch, "flangeThickness" : 0.0625 * inch, "name" : "0.19 in Round Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "REV", "partNumber" : "REV-21-3199", "url" : "https://www.revrobotics.com/search.php?search_query=REV-21-3199&section=product", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_35 : {
                    BearingDimensions.MM_47_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 50 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm)", "vendor" : "REV", "partNumber" : "REV-29-3295", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-3295&section=product", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_12_X_3_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 12 * millimeter, "width" : 3.5 * millimeter, "flangeDiameter" : 13.6 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "8 mm Round Flanged Bearing (12 mm OD x 3.5 mm)", "vendor" : "REV", "partNumber" : "REV-49-1559-PK10", "url" : "https://www.revrobotics.com/rev-49-1559-pk10/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.ROUND : {
                BearingSize.MM_35 : {
                    BearingDimensions.MM_47_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "name" : "35 mm Round Radial Bearing (47 mm OD x 7 mm)", "vendor" : "REV", "partNumber" : "REV-29-3296", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-3296&section=product", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        },
        BearingType.BUSHING : {
            BearingBore.HEX : {
                BearingSize.MM_5 : {
                    BearingDimensions.MM_9_X_5 : {
                        BearingSeal.NONE : { "outerDiameter" : 9 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 13 * millimeter, "flangeThickness" : 2 * millimeter, "name" : "5 mm Hex Flanged Bushing (9 mm OD x 5 mm)", "vendor" : "REV", "partNumber" : "REV-41-1326", "url" : "https://www.revrobotics.com/Through-Bore-Bearings/", "material" : ACETAL, "appearance" : BLACK }
                    },
                    BearingDimensions.MM_9_X_11 : {
                        BearingSeal.NONE : { "outerDiameter" : 9 * millimeter, "width" : 11 * millimeter, "flangeDiameter" : 13 * millimeter, "flangeThickness" : 2 * millimeter, "name" : "5 mm Hex Flanged Bushing (9 mm OD x 11 mm)", "vendor" : "REV", "partNumber" : "REV-41-1329", "url" : "https://www.revrobotics.com/Through-Bore-Bearings/", "material" : ACETAL, "appearance" : BLACK }
                    }
                }
            }
        }
    },
    BearingVendor.THRIFTYBOT : {
        BearingType.FLANGED : {
            BearingBore.HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0001", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0001", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            },
            BearingBore.ROUNDED_HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0014", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0014", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0028", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0028", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0025", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0025", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0008", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0008", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_16_X_6_3 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 6.3 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1.3 * millimeter, "name" : "8 mm Round Flanged Bearing (16 mm OD x 6.3 mm)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0006", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0006", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_35 : {
                    BearingDimensions.MM_47_X_7 : {
                        BearingSeal.SEALED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 50 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm, Sealed)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0065", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0065", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0024", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0024", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0008", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0008", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_1_125_X_0_375 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.375 * inch, "name" : "3/8 in Round Radial Bearing (1.125 in OD x 0.375 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0022", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0022", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_375_X_0_125 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.375 * inch, "width" : 0.125 * inch, "name" : "1/4 in Round Radial Bearing (0.375 in OD x 0.125 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0021", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0021", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_5_X_0_188 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.188 * inch, "name" : "1/4 in Round Radial Bearing (0.5 in OD x 0.188 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0027", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0027", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_75_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.75 * inch, "width" : 0.281 * inch, "name" : "1/4 in Round Radial Bearing (0.75 in OD x 0.281 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0038", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0038", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_35 : {
                    BearingDimensions.MM_47_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "name" : "35 mm Round Radial Bearing (47 mm OD x 7 mm)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0064", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0064", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        }
    },
    BearingVendor.SWYFT : {
        BearingType.FLANGED : {
            BearingBore.HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "Swyft", "partNumber" : "BRG-1001", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "Swyft", "partNumber" : "BRG-1002", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 15 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "8 mm Round Flanged Bearing (14 mm OD x 5 mm)", "vendor" : "Swyft", "partNumber" : "BRG-8mm-14mm-01", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        }
    },
    BearingVendor.SDS : {
        BearingType.FLANGED : {
            BearingBore.ROUND : {
                BearingSize.MM_9 : {
                    BearingDimensions.MM_17_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 17 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 19 * millimeter, "flangeThickness" : 1.1 * millimeter, "name" : "9 mm Round Flanged Bearing (17 mm OD x 5 mm)", "vendor" : "SDS", "partNumber" : "F689ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)", "vendor" : "SDS", "partNumber" : "R8ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)", "vendor" : "SDS", "partNumber" : "R6ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_5_X_0_188 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.188 * inch, "name" : "1/4 in Round Radial Bearing (0.5 in OD x 0.188 in)", "vendor" : "SDS", "partNumber" : "R188ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.OD_0_75_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.75 * inch, "width" : 0.281 * inch, "name" : "1/4 in Round Radial Bearing (0.75 in OD x 0.281 in)", "vendor" : "SDS", "partNumber" : "RA4ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_16_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "name" : "8 mm Round Radial Bearing (16 mm OD x 5 mm)", "vendor" : "SDS", "partNumber" : "688ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_15 : {
                    BearingDimensions.MM_24_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 24 * millimeter, "width" : 5 * millimeter, "name" : "15 mm Round Radial Bearing (24 mm OD x 5 mm)", "vendor" : "SDS", "partNumber" : "6802ZZ", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        }
    },
    BearingVendor.MCMASTER : {
        BearingType.FLANGED : {
            BearingBore.ROUND : {
                BearingSize.MM_5 : {
                    BearingDimensions.MM_8_X_2 : {
                        BearingSeal.OPEN : { "outerDiameter" : 8 * millimeter, "width" : 2 * millimeter, "flangeDiameter" : 9.2 * millimeter, "flangeThickness" : 0.6 * millimeter, "name" : "5 mm Round Flanged Bearing (8 mm OD x 2 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K455", "url" : "https://www.mcmaster.com/57155K455/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_9_X_2_5 : {
                        BearingSeal.OPEN : { "outerDiameter" : 9 * millimeter, "width" : 2.5 * millimeter, "flangeDiameter" : 10.2 * millimeter, "flangeThickness" : 0.6 * millimeter, "name" : "5 mm Round Flanged Bearing (9 mm OD x 2.5 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K457", "url" : "https://www.mcmaster.com/57155K457/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_9_X_3 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 9 * millimeter, "width" : 3 * millimeter, "flangeDiameter" : 10.2 * millimeter, "flangeThickness" : 0.6 * millimeter, "name" : "5 mm Round Flanged Bearing (9 mm OD x 3 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K643", "url" : "https://www.mcmaster.com/57155K643/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_10_X_3 : {
                        BearingSeal.OPEN : { "outerDiameter" : 10 * millimeter, "width" : 3 * millimeter, "flangeDiameter" : 11.2 * millimeter, "flangeThickness" : 0.6 * millimeter, "name" : "5 mm Round Flanged Bearing (10 mm OD x 3 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K459", "url" : "https://www.mcmaster.com/57155K459/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_10_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 10 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 11.6 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "5 mm Round Flanged Bearing (10 mm OD x 4 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K568", "url" : "https://www.mcmaster.com/57155K568/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_11_X_3 : {
                        BearingSeal.OPEN : { "outerDiameter" : 11 * millimeter, "width" : 3 * millimeter, "flangeDiameter" : 12.5 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "5 mm Round Flanged Bearing (11 mm OD x 3 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K462", "url" : "https://www.mcmaster.com/57155K462/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_11_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 11 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 12.6 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "5 mm Round Flanged Bearing (11 mm OD x 4 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K571", "url" : "https://www.mcmaster.com/57155K571/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_11_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 11 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 12.5 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (11 mm OD x 5 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K573", "url" : "https://www.mcmaster.com/57155K573/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_13_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 13 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 15 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (13 mm OD x 4 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K575", "url" : "https://www.mcmaster.com/57155K575/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 16 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (14 mm OD x 5 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K577", "url" : "https://www.mcmaster.com/57155K577/", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 16 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (14 mm OD x 5 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K466", "url" : "https://www.mcmaster.com/57155K466/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_16_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (16 mm OD x 5 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K579", "url" : "https://www.mcmaster.com/57155K579/", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (16 mm OD x 5 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K468", "url" : "https://www.mcmaster.com/57155K468/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.OPEN : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "5 mm Round Flanged Bearing (19 mm OD x 6 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K471", "url" : "https://www.mcmaster.com/57155K471/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_6 : {
                    BearingDimensions.MM_10_X_2_5 : {
                        BearingSeal.OPEN : { "outerDiameter" : 10 * millimeter, "width" : 2.5 * millimeter, "flangeDiameter" : 11.2 * millimeter, "flangeThickness" : 0.6 * millimeter, "name" : "6 mm Round Flanged Bearing (10 mm OD x 2.5 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K472", "url" : "https://www.mcmaster.com/57155K472/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_10_X_3 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 10 * millimeter, "width" : 3 * millimeter, "flangeDiameter" : 11.2 * millimeter, "flangeThickness" : 0.6 * millimeter, "name" : "6 mm Round Flanged Bearing (10 mm OD x 3 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K584", "url" : "https://www.mcmaster.com/57155K584/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_13_X_3_5 : {
                        BearingSeal.OPEN : { "outerDiameter" : 13 * millimeter, "width" : 3.5 * millimeter, "flangeDiameter" : 15 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "6 mm Round Flanged Bearing (13 mm OD x 3.5 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K476", "url" : "https://www.mcmaster.com/57155K476/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_13_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 13 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 15 * millimeter, "flangeThickness" : 1.1 * millimeter, "name" : "6 mm Round Flanged Bearing (13 mm OD x 5 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K588", "url" : "https://www.mcmaster.com/57155K588/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_15_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 15 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 17 * millimeter, "flangeThickness" : 1.2 * millimeter, "name" : "6 mm Round Flanged Bearing (15 mm OD x 5 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K591", "url" : "https://www.mcmaster.com/57155K591/", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 15 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 17 * millimeter, "flangeThickness" : 1.2 * millimeter, "name" : "6 mm Round Flanged Bearing (15 mm OD x 5 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K478", "url" : "https://www.mcmaster.com/57155K478/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_17_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 17 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 19 * millimeter, "flangeThickness" : 1.2 * millimeter, "name" : "6 mm Round Flanged Bearing (17 mm OD x 6 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K593", "url" : "https://www.mcmaster.com/57155K593/", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 17 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 19 * millimeter, "flangeThickness" : 1.2 * millimeter, "name" : "6 mm Round Flanged Bearing (17 mm OD x 6 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K481", "url" : "https://www.mcmaster.com/57155K481/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "6 mm Round Flanged Bearing (19 mm OD x 6 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K595", "url" : "https://www.mcmaster.com/57155K595/", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "6 mm Round Flanged Bearing (19 mm OD x 6 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K483", "url" : "https://www.mcmaster.com/57155K483/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_14_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 15.6 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "8 mm Round Flanged Bearing (14 mm OD x 4 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K823", "url" : "https://www.mcmaster.com/57155K823/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_16_X_4 : {
                        BearingSeal.OPEN : { "outerDiameter" : 16 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "8 mm Round Flanged Bearing (16 mm OD x 4 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K498", "url" : "https://www.mcmaster.com/57155K498/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_16_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1.1 * millimeter, "name" : "8 mm Round Flanged Bearing (16 mm OD x 5 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K825", "url" : "https://www.mcmaster.com/57155K825/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "8 mm Round Flanged Bearing (19 mm OD x 6 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K627", "url" : "https://www.mcmaster.com/57155K627/", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "8 mm Round Flanged Bearing (19 mm OD x 6 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K511", "url" : "https://www.mcmaster.com/57155K511/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_22_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 25 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "8 mm Round Flanged Bearing (22 mm OD x 7 mm)", "vendor" : "McMaster-Carr", "partNumber" : "57155K629", "url" : "https://www.mcmaster.com/57155K629/", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 25 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "8 mm Round Flanged Bearing (22 mm OD x 7 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "57155K513", "url" : "https://www.mcmaster.com/57155K513/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_25 : {
                    BearingDimensions.MM_37_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 37 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 40 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "25 mm Round Flanged Bearing (37 mm OD x 7 mm)", "vendor" : "McMaster-Carr", "partNumber" : "4390N178", "url" : "https://www.mcmaster.com/4390N178/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_30 : {
                    BearingDimensions.MM_42_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 42 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 45 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "30 mm Round Flanged Bearing (42 mm OD x 7 mm)", "vendor" : "McMaster-Carr", "partNumber" : "4390N181", "url" : "https://www.mcmaster.com/4390N181/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_25 : {
                        BearingSeal.OPEN : { "outerDiameter" : 1.125 * inch, "width" : 0.25 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.25 in, Open)", "vendor" : "McMaster-Carr", "partNumber" : "60355K173", "url" : "https://www.mcmaster.com/60355K173/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_219 : {
                        BearingSeal.OPEN : { "outerDiameter" : 0.875 * inch, "width" : 0.219 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.219 in, Open)", "vendor" : "McMaster-Carr", "partNumber" : "60355K165", "url" : "https://www.mcmaster.com/60355K165/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_3_16 : {
                    BearingDimensions.OD_0_5_X_0_156 : {
                        BearingSeal.OPEN : { "outerDiameter" : 0.5 * inch, "width" : 0.156 * inch, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.156 in, Open)", "vendor" : "McMaster-Carr", "partNumber" : "60355K132", "url" : "https://www.mcmaster.com/60355K132/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_625_X_0_196 : {
                        BearingSeal.OPEN : { "outerDiameter" : 0.625 * inch, "width" : 0.196 * inch, "name" : "1/4 in Round Radial Bearing (0.625 in OD x 0.196 in, Open)", "vendor" : "McMaster-Carr", "partNumber" : "60355K151", "url" : "https://www.mcmaster.com/60355K151/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_5 : {
                    BearingDimensions.MM_8_X_2_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 8 * millimeter, "width" : 2.5 * millimeter, "name" : "5 mm Round Radial Bearing (8 mm OD x 2.5 mm)", "vendor" : "McMaster-Carr", "partNumber" : "7804K104", "url" : "https://www.mcmaster.com/7804K104/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_9_X_3 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 9 * millimeter, "width" : 3 * millimeter, "name" : "5 mm Round Radial Bearing (9 mm OD x 3 mm)", "vendor" : "McMaster-Carr", "partNumber" : "7804K105", "url" : "https://www.mcmaster.com/7804K105/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_10_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 10 * millimeter, "width" : 4 * millimeter, "name" : "5 mm Round Radial Bearing (10 mm OD x 4 mm)", "vendor" : "McMaster-Carr", "partNumber" : "7804K106", "url" : "https://www.mcmaster.com/7804K106/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_11_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 11 * millimeter, "width" : 4 * millimeter, "name" : "5 mm Round Radial Bearing (11 mm OD x 4 mm)", "vendor" : "McMaster-Carr", "partNumber" : "7804K107", "url" : "https://www.mcmaster.com/7804K107/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_13_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 13 * millimeter, "width" : 4 * millimeter, "name" : "5 mm Round Radial Bearing (13 mm OD x 4 mm)", "vendor" : "McMaster-Carr", "partNumber" : "7804K108", "url" : "https://www.mcmaster.com/7804K108/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "name" : "5 mm Round Radial Bearing (14 mm OD x 5 mm)", "vendor" : "McMaster-Carr", "partNumber" : "7804K109", "url" : "https://www.mcmaster.com/7804K109/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_16_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "name" : "5 mm Round Radial Bearing (16 mm OD x 5 mm)", "vendor" : "McMaster-Carr", "partNumber" : "6153K15", "url" : "https://www.mcmaster.com/6153K15/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "name" : "5 mm Round Radial Bearing (19 mm OD x 6 mm)", "vendor" : "McMaster-Carr", "partNumber" : "5972K263", "url" : "https://www.mcmaster.com/5972K263/", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "name" : "5 mm Round Radial Bearing (19 mm OD x 6 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "5972K197", "url" : "https://www.mcmaster.com/5972K197/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_7 : {
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "name" : "7 mm Round Radial Bearing (14 mm OD x 5 mm)", "vendor" : "McMaster-Carr", "partNumber" : "7804K114", "url" : "https://www.mcmaster.com/7804K114/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "name" : "7 mm Round Radial Bearing (19 mm OD x 6 mm)", "vendor" : "McMaster-Carr", "partNumber" : "4481N11", "url" : "https://www.mcmaster.com/4481N11/", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.SEALED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "name" : "7 mm Round Radial Bearing (19 mm OD x 6 mm, Sealed)", "vendor" : "McMaster-Carr", "partNumber" : "5972K225", "url" : "https://www.mcmaster.com/5972K225/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_22_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "name" : "7 mm Round Radial Bearing (22 mm OD x 7 mm)", "vendor" : "McMaster-Carr", "partNumber" : "6153K27", "url" : "https://www.mcmaster.com/6153K27/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.SEALED : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "name" : "7 mm Round Radial Bearing (22 mm OD x 7 mm, Sealed)", "vendor" : "McMaster-Carr", "partNumber" : "5972K502", "url" : "https://www.mcmaster.com/5972K502/", "material" : STEEL, "appearance" : STEEL_GRAY },
                        BearingSeal.OPEN : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "name" : "7 mm Round Radial Bearing (22 mm OD x 7 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "6153K114", "url" : "https://www.mcmaster.com/6153K114/", "material" : STAINLESS_STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_22_X_7 : {
                        BearingSeal.OPEN : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "name" : "8 mm Round Radial Bearing (22 mm OD x 7 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "5972K91", "url" : "https://www.mcmaster.com/5972K91/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_25 : {
                    BearingDimensions.MM_37_X_7 : {
                        BearingSeal.OPEN : { "outerDiameter" : 37 * millimeter, "width" : 7 * millimeter, "name" : "25 mm Round Radial Bearing (37 mm OD x 7 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "5972K216", "url" : "https://www.mcmaster.com/5972K216/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_30 : {
                    BearingDimensions.MM_42_X_7 : {
                        BearingSeal.OPEN : { "outerDiameter" : 42 * millimeter, "width" : 7 * millimeter, "name" : "30 mm Round Radial Bearing (42 mm OD x 7 mm, Open)", "vendor" : "McMaster-Carr", "partNumber" : "5972K217", "url" : "https://www.mcmaster.com/5972K217/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        }
    },
    BearingVendor.GOBILDA : {
        BearingType.FLANGED : {
            BearingBore.ROUND : {
                BearingSize.MM_4 : {
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 16 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "4 mm Round Flanged Bearing (14 mm OD x 5 mm)", "vendor" : "goBILDA", "partNumber" : "1601-0014-0004", "url" : "https://www.gobilda.com/4mm-id-flanged-ball-bearing-14mm-od-5mm-thickness-2-pack/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_5 : {
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 16 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (14 mm OD x 5 mm)", "vendor" : "goBILDA", "partNumber" : "1601-0014-0005", "url" : "https://www.gobilda.com/5mm-id-flanged-ball-bearing-14mm-od-5mm-thickness-2-pack/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_6 : {
                    BearingDimensions.MM_12_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 12 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 13.5 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "6 mm Round Flanged Bearing (12 mm OD x 4 mm)", "vendor" : "goBILDA", "partNumber" : "1601-0412-0006", "url" : "https://www.gobilda.com/6mm-id-flanged-ball-bearing-12mm-od-4mm-thickness-2-pack/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 15 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "6 mm Round Flanged Bearing (14 mm OD x 5 mm)", "vendor" : "goBILDA", "partNumber" : "1611-0514-0006", "url" : "https://www.gobilda.com/1611-series-flanged-ball-bearing-6mm-id-x-14mm-od-5mm-thickness-2-pack/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 15 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "8 mm Round Flanged Bearing (14 mm OD x 5 mm)", "vendor" : "goBILDA", "partNumber" : "1611-0514-0008", "url" : "https://www.gobilda.com/1611-series-flanged-ball-bearing-8mm-id-x-14mm-od-5mm-thickness-2-pack/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_12 : {
                    BearingDimensions.MM_32_X_10 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 32 * millimeter, "width" : 10 * millimeter, "flangeDiameter" : 34 * millimeter, "flangeThickness" : 2.3 * millimeter, "name" : "12 mm Round Flanged Bearing (32 mm OD x 10 mm)", "vendor" : "goBILDA", "partNumber" : "1601-1032-0012", "url" : "https://www.gobilda.com/12mm-id-flanged-ball-bearing-32mm-od-10mm-thickness/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_32 : {
                    BearingDimensions.MM_39_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 39 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 41 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "32 mm Round Flanged Bearing (39 mm OD x 5 mm)", "vendor" : "goBILDA", "partNumber" : "1601-0039-0032", "url" : "https://www.gobilda.com/32mm-id-flanged-ball-bearing-39mm-od-5mm-thickness/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            },
            BearingBore.REX : {
                BearingSize.MM_8 : {
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 15 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "8 mm REX Flanged Bearing (14 mm OD x 5 mm)", "vendor" : "goBILDA", "partNumber" : "1611-0514-4008", "url" : "https://www.gobilda.com/1611-series-flanged-ball-bearing-8mm-rex-id-x-14mm-od-5mm-thickness-2-pack/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.ROUND : {
                BearingSize.MM_4 : {
                    BearingDimensions.MM_10_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 10 * millimeter, "width" : 4 * millimeter, "name" : "4 mm Round Radial Bearing (10 mm OD x 4 mm)", "vendor" : "goBILDA", "partNumber" : "1600-0410-0004", "url" : "https://www.gobilda.com/4mm-id-non-flanged-ball-bearing-10mm-od-4mm-thickness-2-pack/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    },
                    BearingDimensions.MM_16_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 4 * millimeter, "name" : "4 mm Round Radial Bearing (16 mm OD x 4 mm)", "vendor" : "goBILDA", "partNumber" : "1600-0416-0004", "url" : "https://www.gobilda.com/4mm-id-non-flanged-ball-bearing-16mm-od-4mm-thickness-2-pack/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_6 : {
                    BearingDimensions.MM_16_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 4 * millimeter, "name" : "6 mm Round Radial Bearing (16 mm OD x 4 mm)", "vendor" : "goBILDA", "partNumber" : "1600-0416-0006", "url" : "https://www.gobilda.com/6mm-id-non-flanged-ball-bearing-16mm-od-4mm-thickness-2-pack/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_22_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "name" : "8 mm Round Radial Bearing (22 mm OD x 7 mm)", "vendor" : "goBILDA", "partNumber" : "1600-0722-0008", "url" : "https://www.gobilda.com/1600-series-non-flanged-ball-bearing-8mm-id-x-22mm-od-7mm-thickness-2-pack/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_12 : {
                    BearingDimensions.MM_32_X_10 : {
                        BearingSeal.SEALED : { "outerDiameter" : 32 * millimeter, "width" : 10 * millimeter, "name" : "12 mm Round Radial Bearing (32 mm OD x 10 mm, Sealed)", "vendor" : "goBILDA", "partNumber" : "1626-1032-0012", "url" : "https://www.gobilda.com/12mm-id-non-flanged-ball-bearing-sealed-32mm-od-10mm-thickness-2-pack/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                },
                BearingSize.MM_120 : {
                    BearingDimensions.MM_136_X_8 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 136 * millimeter, "width" : 8 * millimeter, "name" : "120 mm Round Radial Bearing (136 mm OD x 8 mm)", "vendor" : "goBILDA", "partNumber" : "1600-0001-0120", "url" : "https://www.gobilda.com/120mm-non-flanged-ball-bearing-136mm-od-8mm-thickness/", "material" : STEEL, "appearance" : STEEL_GRAY }
                    }
                }
            }
        }
    }
};
