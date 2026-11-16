FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

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
    annotation { "Name" : "VEX" }
    VEX,
    annotation { "Name" : "ThriftyBot" }
    THRIFTYBOT,
    annotation { "Name" : "Swyft" }
    SWYFT,
    annotation { "Name" : "SDS" }
    SDS
}

export enum BearingType
{
    annotation { "Name" : "Flanged" }
    FLANGED,
    annotation { "Name" : "Radial" }
    RADIAL
}

export enum BearingBore
{
    annotation { "Name" : "Hex" }
    HEX,
    annotation { "Name" : "Rounded hex" }
    ROUNDED_HEX,
    annotation { "Name" : "Round" }
    ROUND,
    annotation { "Name" : "ThunderSquare" }
    THUNDERSQUARE
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
    annotation { "Name" : "5 mm" }
    MM_5,
    annotation { "Name" : "6 mm" }
    MM_6,
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
    annotation { "Name" : "35 mm" }
    MM_35,
    annotation { "Name" : "80 mm" }
    MM_80
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
    annotation { "Name" : "11 mm OD x 4 mm" }
    MM_11_X_4,
    annotation { "Name" : "12 mm OD x 4 mm" }
    MM_12_X_4,
    annotation { "Name" : "13 mm OD x 4 mm" }
    MM_13_X_4,
    annotation { "Name" : "14 mm OD x 5 mm" }
    MM_14_X_5,
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
    annotation { "Name" : "37 mm OD x 7 mm" }
    MM_37_X_7,
    annotation { "Name" : "42 mm OD x 7 mm" }
    MM_42_X_7,
    annotation { "Name" : "47 mm OD x 7 mm" }
    MM_47_X_7,
    annotation { "Name" : "100 mm OD x 10 mm" }
    MM_100_X_10
}

export enum BearingSeal
{
    annotation { "Name" : "Shielded" }
    SHIELDED,
    annotation { "Name" : "Sealed" }
    SEALED,
    annotation { "Name" : "Open" }
    OPEN
}

/** Each bore's shape: a round bore's `diameter`, or a hex's or square's `sides` and `acrossFlats`, with its corners rounded to `diameter` where they are. */
export const BEARING_BORES = {
    BearingBore.HEX : {
        BearingSize.IN_1_2 : { "acrossFlats" : 0.5 * inch, "sides" : 6 },
        BearingSize.IN_3_8 : { "acrossFlats" : 0.375 * inch, "sides" : 6 }
    },
    BearingBore.ROUNDED_HEX : {
        BearingSize.IN_1_2 : { "acrossFlats" : 0.5 * inch, "sides" : 6, "diameter" : 13.75 * millimeter },
        BearingSize.IN_3_8 : { "acrossFlats" : 0.375 * inch, "sides" : 6, "diameter" : 10.25 * millimeter }
    },
    BearingBore.ROUND : {
        BearingSize.IN_1_2 : { "diameter" : 0.5 * inch },
        BearingSize.IN_3_8 : { "diameter" : 0.375 * inch },
        BearingSize.IN_3_16 : { "diameter" : 0.1875 * inch },
        BearingSize.NO_10 : { "diameter" : 0.19 * inch },
        BearingSize.IN_1_4 : { "diameter" : 0.25 * inch },
        BearingSize.IN_5_8 : { "diameter" : 0.625 * inch },
        BearingSize.IN_3_4 : { "diameter" : 0.75 * inch },
        BearingSize.MM_5 : { "diameter" : 5 * millimeter },
        BearingSize.MM_6 : { "diameter" : 6 * millimeter },
        BearingSize.MM_8 : { "diameter" : 8 * millimeter },
        BearingSize.MM_9 : { "diameter" : 9 * millimeter },
        BearingSize.MM_10 : { "diameter" : 10 * millimeter },
        BearingSize.MM_12 : { "diameter" : 12 * millimeter },
        BearingSize.MM_15 : { "diameter" : 15 * millimeter },
        BearingSize.MM_17 : { "diameter" : 17 * millimeter },
        BearingSize.MM_25 : { "diameter" : 25 * millimeter },
        BearingSize.MM_30 : { "diameter" : 30 * millimeter },
        BearingSize.MM_35 : { "diameter" : 35 * millimeter },
        BearingSize.MM_80 : { "diameter" : 80 * millimeter }
    },
    BearingBore.THUNDERSQUARE : {
        BearingSize.IN_1_4 : { "acrossFlats" : 0.25 * inch, "sides" : 4, "diameter" : 0.302 * inch }
    }
};

/** What each vendor makes, by vendor, type, bore, size, dimensions, and seal: each bearing's dimensions, a flanged one's flange, and its properties. */
export const BEARINGS = {
    BearingVendor.GENERIC : {
        BearingType.FLANGED : {
            BearingBore.HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "partNumber" : "FR8ZZ-HexHD" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Hex Flanged Bearing (0.875 in OD x 0.281 in, Sealed)", "partNumber" : "FR62RS" }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/8 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "partNumber" : "FR6ZZL-Hex" }
                    }
                }
            },
            BearingBore.ROUNDED_HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)" }
                    },
                    BearingDimensions.OD_1_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1 * inch, "width" : 0.281 * inch, "flangeDiameter" : 1.06 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1 in OD x 0.281 in)" }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)" }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "partNumber" : "FR8ZZ" },
                        BearingSeal.SEALED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in, Sealed)", "partNumber" : "FR82RS" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_219 : {
                        BearingSeal.OPEN : { "outerDiameter" : 0.875 * inch, "width" : 0.219 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.062 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.219 in, Open)", "partNumber" : "FR6" }
                    },
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)", "partNumber" : "FR6ZZ" },
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in, Sealed)", "partNumber" : "FR62RS" }
                    },
                    BearingDimensions.OD_1_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1 * inch, "width" : 0.281 * inch, "flangeDiameter" : 1.06 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (1 in OD x 0.281 in)" }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/8 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "partNumber" : "FR6ZZ-L" }
                    }
                },
                BearingSize.IN_3_16 : {
                    BearingDimensions.OD_0_5_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.196 * inch, "flangeDiameter" : 0.565 * inch, "flangeThickness" : 0.042 * inch, "name" : "3/16 in Round Flanged Bearing (0.5 in OD x 0.196 in)", "partNumber" : "FR3ZZ" }
                    }
                },
                BearingSize.NO_10 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "0.19 in Round Flanged Bearing (0.875 in OD x 0.281 in)" }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_5_X_0_188 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.188 * inch, "flangeDiameter" : 0.565 * inch, "flangeThickness" : 0.042 * inch, "name" : "1/4 in Round Flanged Bearing (0.5 in OD x 0.188 in)", "partNumber" : "FR188ZZ" }
                    },
                    BearingDimensions.OD_0_625_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.625 * inch, "width" : 0.196 * inch, "flangeDiameter" : 0.69 * inch, "flangeThickness" : 0.042 * inch, "name" : "1/4 in Round Flanged Bearing (0.625 in OD x 0.196 in)", "partNumber" : "FR4ZZ" }
                    }
                },
                BearingSize.IN_3_4 : {
                    BearingDimensions.OD_1_375_X_0_438 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.375 * inch, "width" : 0.438 * inch, "flangeDiameter" : 1.5 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/4 in Round Flanged Bearing (1.375 in OD x 0.438 in)" }
                    }
                },
                BearingSize.MM_5 : {
                    BearingDimensions.MM_11_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 11 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 12.5 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "5 mm Round Flanged Bearing (11 mm OD x 4 mm)", "partNumber" : "MF115ZZ" }
                    },
                    BearingDimensions.MM_13_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 13 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 15 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (13 mm OD x 4 mm)", "partNumber" : "F695ZZ" }
                    },
                    BearingDimensions.MM_16_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (16 mm OD x 5 mm)", "partNumber" : "F625ZZ" }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "5 mm Round Flanged Bearing (19 mm OD x 6 mm)", "partNumber" : "F1950ZZ" }
                    }
                },
                BearingSize.MM_6 : {
                    BearingDimensions.MM_12_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 12 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 13.6 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "6 mm Round Flanged Bearing (12 mm OD x 4 mm)", "partNumber" : "F126ZZ" }
                    },
                    BearingDimensions.MM_17_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 17 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 19 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "6 mm Round Flanged Bearing (17 mm OD x 6 mm)", "partNumber" : "F606ZZ" }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 15.6 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "8 mm Round Flanged Bearing (14 mm OD x 5 mm)" }
                    },
                    BearingDimensions.MM_16_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1.1 * millimeter, "name" : "8 mm Round Flanged Bearing (16 mm OD x 5 mm)", "partNumber" : "W688AZZ" }
                    },
                    BearingDimensions.MM_16_X_6_3 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 6.3 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1.1 * millimeter, "name" : "8 mm Round Flanged Bearing (16 mm OD x 6.3 mm)", "partNumber" : "F688Z" }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "8 mm Round Flanged Bearing (19 mm OD x 6 mm)", "partNumber" : "F698ZZ" }
                    },
                    BearingDimensions.MM_22_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 25 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "8 mm Round Flanged Bearing (22 mm OD x 7 mm)", "partNumber" : "F608ZZ" }
                    },
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "8 mm Round Flanged Bearing (0.875 in OD x 0.281 in)" }
                    }
                },
                BearingSize.MM_9 : {
                    BearingDimensions.MM_17_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 17 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 19 * millimeter, "flangeThickness" : 1.1 * millimeter, "name" : "9 mm Round Flanged Bearing (17 mm OD x 5 mm)", "partNumber" : "F689ZZ" }
                    }
                },
                BearingSize.MM_12 : {
                    BearingDimensions.MM_24_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 24 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 26.5 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "12 mm Round Flanged Bearing (24 mm OD x 6 mm)", "partNumber" : "F6901ZZ" }
                    }
                },
                BearingSize.MM_15 : {
                    BearingDimensions.MM_21_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 21 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 22.5 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "15 mm Round Flanged Bearing (21 mm OD x 4 mm)", "partNumber" : "F6702ZZ" }
                    }
                },
                BearingSize.MM_30 : {
                    BearingDimensions.MM_42_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 42 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 45 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "30 mm Round Flanged Bearing (42 mm OD x 7 mm)", "partNumber" : "F6806ZZ" }
                    }
                },
                BearingSize.MM_35 : {
                    BearingDimensions.MM_47_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 50 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm)", "partNumber" : "F6807ZZ" },
                        BearingSeal.SEALED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 50 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm, Sealed)", "partNumber" : "F68072RS" }
                    }
                }
            },
            BearingBore.THUNDERSQUARE : {
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/4 in ThunderSquare Flanged Bearing (0.875 in OD x 0.281 in)" }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SEALED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Hex Radial Bearing (1.125 in OD x 0.313 in, Sealed)", "partNumber" : "R82RS-Hex" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Hex Radial Bearing (0.875 in OD x 0.281 in, Sealed)", "partNumber" : "R62RS-Hex" }
                    }
                }
            },
            BearingBore.ROUNDED_HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Rounded Hex Radial Bearing (1.125 in OD x 0.313 in)" }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "1/2 in Round Radial Bearing (0.875 in OD x 0.281 in)" }
                    },
                    BearingDimensions.OD_1_125_X_0_25 : {
                        BearingSeal.OPEN : { "outerDiameter" : 1.125 * inch, "width" : 0.25 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.25 in, Open)", "partNumber" : "R8" }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)", "partNumber" : "R8ZZ" },
                        BearingSeal.SEALED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in, Sealed)", "partNumber" : "R82RS" }
                    },
                    BearingDimensions.OD_1_375_X_0_438 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.375 * inch, "width" : 0.438 * inch, "name" : "1/2 in Round Radial Bearing (1.375 in OD x 0.438 in)", "partNumber" : "1621ZZ" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_219 : {
                        BearingSeal.OPEN : { "outerDiameter" : 0.875 * inch, "width" : 0.219 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.219 in, Open)", "partNumber" : "R6" }
                    },
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)", "partNumber" : "R6ZZ" },
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in, Sealed)", "partNumber" : "R62RS" }
                    },
                    BearingDimensions.OD_1_125_X_0_375 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.375 * inch, "name" : "3/8 in Round Radial Bearing (1.125 in OD x 0.375 in)", "partNumber" : "1614ZZ" }
                    }
                },
                BearingSize.IN_3_16 : {
                    BearingDimensions.OD_0_5_X_0_156 : {
                        BearingSeal.OPEN : { "outerDiameter" : 0.5 * inch, "width" : 0.156 * inch, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.156 in, Open)", "partNumber" : "R3" }
                    },
                    BearingDimensions.OD_0_5_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.196 * inch, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.196 in)", "partNumber" : "R3ZZ" }
                    }
                },
                BearingSize.NO_10 : {
                    BearingDimensions.OD_0_375_X_0_156_EXTENDED : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.375 * inch, "width" : 0.156 * inch, "outerWidth" : 0.125 * inch, "name" : "0.19 in Round Radial Bearing (0.375 in OD x 0.156 in (extended inner race))", "partNumber" : "R166ZZEE" }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_375_X_0_125 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.375 * inch, "width" : 0.125 * inch, "name" : "1/4 in Round Radial Bearing (0.375 in OD x 0.125 in)", "partNumber" : "R168ZZ" }
                    },
                    BearingDimensions.OD_0_5_X_0_188 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.188 * inch, "name" : "1/4 in Round Radial Bearing (0.5 in OD x 0.188 in)", "partNumber" : "R188ZZ" }
                    },
                    BearingDimensions.OD_0_625_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.625 * inch, "width" : 0.196 * inch, "name" : "1/4 in Round Radial Bearing (0.625 in OD x 0.196 in)", "partNumber" : "R4ZZ" }
                    },
                    BearingDimensions.OD_0_75_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.75 * inch, "width" : 0.281 * inch, "name" : "1/4 in Round Radial Bearing (0.75 in OD x 0.281 in)", "partNumber" : "RA4ZZ" }
                    }
                },
                BearingSize.IN_5_8 : {
                    BearingDimensions.OD_1_375_X_0_438 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.375 * inch, "width" : 0.438 * inch, "name" : "5/8 in Round Radial Bearing (1.375 in OD x 0.438 in)", "partNumber" : "1623ZZ" }
                    }
                },
                BearingSize.MM_5 : {
                    BearingDimensions.MM_13_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 13 * millimeter, "width" : 4 * millimeter, "name" : "5 mm Round Radial Bearing (13 mm OD x 4 mm)", "partNumber" : "695ZZ" }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "name" : "5 mm Round Radial Bearing (19 mm OD x 6 mm)", "partNumber" : "635ZZ" }
                    }
                },
                BearingSize.MM_6 : {
                    BearingDimensions.MM_12_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 12 * millimeter, "width" : 4 * millimeter, "name" : "6 mm Round Radial Bearing (12 mm OD x 4 mm)", "partNumber" : "126ZZ" }
                    },
                    BearingDimensions.MM_17_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 17 * millimeter, "width" : 6 * millimeter, "name" : "6 mm Round Radial Bearing (17 mm OD x 6 mm)", "partNumber" : "606ZZ" }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_16_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "name" : "8 mm Round Radial Bearing (16 mm OD x 5 mm)", "partNumber" : "688ZZ" }
                    },
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "name" : "8 mm Round Radial Bearing (19 mm OD x 6 mm)", "partNumber" : "R1980ZZ" }
                    },
                    BearingDimensions.MM_22_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "name" : "8 mm Round Radial Bearing (22 mm OD x 7 mm)", "partNumber" : "608ZZ" }
                    }
                },
                BearingSize.MM_10 : {
                    BearingDimensions.MM_26_X_8 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 26 * millimeter, "width" : 8 * millimeter, "name" : "10 mm Round Radial Bearing (26 mm OD x 8 mm)", "partNumber" : "6000ZZ" }
                    }
                },
                BearingSize.MM_12 : {
                    BearingDimensions.MM_21_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 21 * millimeter, "width" : 5 * millimeter, "name" : "12 mm Round Radial Bearing (21 mm OD x 5 mm)", "partNumber" : "6801ZZ" }
                    },
                    BearingDimensions.MM_28_X_8 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 28 * millimeter, "width" : 8 * millimeter, "name" : "12 mm Round Radial Bearing (28 mm OD x 8 mm)", "partNumber" : "6001ZZ" }
                    }
                },
                BearingSize.MM_15 : {
                    BearingDimensions.MM_21_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 21 * millimeter, "width" : 4 * millimeter, "name" : "15 mm Round Radial Bearing (21 mm OD x 4 mm)", "partNumber" : "6702ZZ" }
                    },
                    BearingDimensions.MM_24_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 24 * millimeter, "width" : 5 * millimeter, "name" : "15 mm Round Radial Bearing (24 mm OD x 5 mm)", "partNumber" : "6802ZZ" }
                    },
                    BearingDimensions.MM_28_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 28 * millimeter, "width" : 7 * millimeter, "name" : "15 mm Round Radial Bearing (28 mm OD x 7 mm)", "partNumber" : "6902ZZ" }
                    }
                },
                BearingSize.MM_17 : {
                    BearingDimensions.MM_23_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 23 * millimeter, "width" : 4 * millimeter, "name" : "17 mm Round Radial Bearing (23 mm OD x 4 mm)", "partNumber" : "6703ZZ" }
                    },
                    BearingDimensions.MM_26_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 26 * millimeter, "width" : 5 * millimeter, "name" : "17 mm Round Radial Bearing (26 mm OD x 5 mm)", "partNumber" : "6803ZZ" }
                    }
                },
                BearingSize.MM_25 : {
                    BearingDimensions.MM_37_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 37 * millimeter, "width" : 7 * millimeter, "name" : "25 mm Round Radial Bearing (37 mm OD x 7 mm)", "partNumber" : "6805ZZ" }
                    }
                },
                BearingSize.MM_35 : {
                    BearingDimensions.MM_47_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "name" : "35 mm Round Radial Bearing (47 mm OD x 7 mm)", "partNumber" : "6807ZZ" }
                    }
                },
                BearingSize.MM_80 : {
                    BearingDimensions.MM_100_X_10 : {
                        BearingSeal.SEALED : { "outerDiameter" : 100 * millimeter, "width" : 10 * millimeter, "name" : "80 mm Round Radial Bearing (100 mm OD x 10 mm, Sealed)", "partNumber" : "68162RS" }
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
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "WCP", "partNumber" : "WCP-0783", "url" : "https://wcproducts.com/products/wcp-0783" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/8 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "WCP", "partNumber" : "WCP-1507", "url" : "https://wcproducts.com/products/wcp-1507" }
                    }
                }
            },
            BearingBore.ROUNDED_HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-2057", "url" : "https://wcproducts.com/products/wcp-2057" }
                    },
                    BearingDimensions.OD_1_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1 * inch, "width" : 0.281 * inch, "flangeDiameter" : 1.06 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-2058", "url" : "https://wcproducts.com/products/wcp-2058" }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "WCP", "partNumber" : "WCP-0785", "url" : "https://wcproducts.com/products/wcp-0785" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-0784", "url" : "https://wcproducts.com/products/wcp-0784" }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "WCP", "partNumber" : "WCP-0781", "url" : "https://wcproducts.com/products/wcp-0781" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-0779", "url" : "https://wcproducts.com/products/wcp-0779" }
                    },
                    BearingDimensions.OD_1_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1 * inch, "width" : 0.281 * inch, "flangeDiameter" : 1.06 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (1 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-2060", "url" : "https://wcproducts.com/products/wcp-2060" }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/8 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "WCP", "partNumber" : "WCP-0780", "url" : "https://wcproducts.com/products/wcp-0780" }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_5_X_0_188 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.188 * inch, "flangeDiameter" : 0.565 * inch, "flangeThickness" : 0.042 * inch, "name" : "1/4 in Round Flanged Bearing (0.5 in OD x 0.188 in)", "vendor" : "WCP", "partNumber" : "WCP-0072", "url" : "https://wcproducts.com/products/wcp-0072" }
                    },
                    BearingDimensions.OD_0_625_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.625 * inch, "width" : 0.196 * inch, "flangeDiameter" : 0.69 * inch, "flangeThickness" : 0.042 * inch, "name" : "1/4 in Round Flanged Bearing (0.625 in OD x 0.196 in)", "vendor" : "WCP", "partNumber" : "WCP-0042", "url" : "https://wcproducts.com/products/wcp-0042" }
                    }
                },
                BearingSize.IN_3_4 : {
                    BearingDimensions.OD_1_375_X_0_438 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.375 * inch, "width" : 0.438 * inch, "flangeDiameter" : 1.5 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/4 in Round Flanged Bearing (1.375 in OD x 0.438 in)", "vendor" : "WCP", "partNumber" : "WCP-0782", "url" : "https://wcproducts.com/products/wcp-0782" }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_16_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1.1 * millimeter, "name" : "8 mm Round Flanged Bearing (16 mm OD x 5 mm)", "vendor" : "WCP", "partNumber" : "WCP-1508", "url" : "https://wcproducts.com/products/wcp-1508" }
                    },
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "8 mm Round Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-2054", "url" : "https://wcproducts.com/products/wcp-2054" }
                    }
                },
                BearingSize.MM_12 : {
                    BearingDimensions.MM_24_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 24 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 26.5 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "12 mm Round Flanged Bearing (24 mm OD x 6 mm)", "vendor" : "WCP", "partNumber" : "WCP-1734", "url" : "https://wcproducts.com/products/wcp-1734" }
                    }
                },
                BearingSize.MM_30 : {
                    BearingDimensions.MM_42_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 42 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 45 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "30 mm Round Flanged Bearing (42 mm OD x 7 mm)", "vendor" : "WCP", "partNumber" : "WCP-0299", "url" : "https://wcproducts.com/products/wcp-0299" }
                    }
                },
                BearingSize.MM_35 : {
                    BearingDimensions.MM_47_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 50 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm)", "vendor" : "WCP", "partNumber" : "WCP-1471", "url" : "https://wcproducts.com/products/wcp-1471" }
                    }
                }
            },
            BearingBore.THUNDERSQUARE : {
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/4 in ThunderSquare Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "217-5830", "url" : "https://wcproducts.com/products/217-5830" }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "1/2 in Round Radial Bearing (0.875 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-0777", "url" : "https://wcproducts.com/products/wcp-0777" }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)", "vendor" : "WCP", "partNumber" : "WCP-0778", "url" : "https://wcproducts.com/products/wcp-0778" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-0776", "url" : "https://wcproducts.com/products/wcp-0776" }
                    }
                },
                BearingSize.IN_3_16 : {
                    BearingDimensions.OD_0_5_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.196 * inch, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.196 in)", "vendor" : "WCP", "partNumber" : "WCP-0773", "url" : "https://wcproducts.com/products/wcp-0773" }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_375_X_0_125 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.375 * inch, "width" : 0.125 * inch, "name" : "1/4 in Round Radial Bearing (0.375 in OD x 0.125 in)", "vendor" : "WCP", "partNumber" : "WCP-0774", "url" : "https://wcproducts.com/products/wcp-0774" }
                    },
                    BearingDimensions.OD_0_5_X_0_188 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.188 * inch, "name" : "1/4 in Round Radial Bearing (0.5 in OD x 0.188 in)", "vendor" : "WCP", "partNumber" : "WCP-0775", "url" : "https://wcproducts.com/products/wcp-0775" }
                    },
                    BearingDimensions.OD_0_625_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.625 * inch, "width" : 0.196 * inch, "name" : "1/4 in Round Radial Bearing (0.625 in OD x 0.196 in)", "vendor" : "WCP", "partNumber" : "WCP-0041", "url" : "https://wcproducts.com/products/wcp-0041" }
                    },
                    BearingDimensions.OD_0_75_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.75 * inch, "width" : 0.281 * inch, "name" : "1/4 in Round Radial Bearing (0.75 in OD x 0.281 in)", "vendor" : "WCP", "partNumber" : "WCP-0212", "url" : "https://wcproducts.com/products/wcp-0212" }
                    }
                },
                BearingSize.MM_6 : {
                    BearingDimensions.MM_12_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 12 * millimeter, "width" : 4 * millimeter, "name" : "6 mm Round Radial Bearing (12 mm OD x 4 mm)", "vendor" : "WCP", "partNumber" : "WCP-0498", "url" : "https://wcproducts.com/products/wcp-0498" }
                    }
                },
                BearingSize.MM_12 : {
                    BearingDimensions.MM_21_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 21 * millimeter, "width" : 5 * millimeter, "name" : "12 mm Round Radial Bearing (21 mm OD x 5 mm)", "vendor" : "WCP", "partNumber" : "WCP-1756", "url" : "https://wcproducts.com/products/wcp-1756" }
                    }
                },
                BearingSize.MM_15 : {
                    BearingDimensions.MM_24_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 24 * millimeter, "width" : 5 * millimeter, "name" : "15 mm Round Radial Bearing (24 mm OD x 5 mm)", "vendor" : "WCP", "partNumber" : "WCP-0078", "url" : "https://wcproducts.com/products/wcp-0078" }
                    },
                    BearingDimensions.MM_28_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 28 * millimeter, "width" : 7 * millimeter, "name" : "15 mm Round Radial Bearing (28 mm OD x 7 mm)", "vendor" : "WCP", "partNumber" : "WCP-0840", "url" : "https://wcproducts.com/products/wcp-0840" }
                    }
                },
                BearingSize.MM_17 : {
                    BearingDimensions.MM_23_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 23 * millimeter, "width" : 4 * millimeter, "name" : "17 mm Round Radial Bearing (23 mm OD x 4 mm)", "vendor" : "WCP", "partNumber" : "WCP-1660", "url" : "https://wcproducts.com/products/wcp-1660" }
                    },
                    BearingDimensions.MM_26_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 26 * millimeter, "width" : 5 * millimeter, "name" : "17 mm Round Radial Bearing (26 mm OD x 5 mm)", "vendor" : "WCP", "partNumber" : "WCP-0841", "url" : "https://wcproducts.com/products/wcp-0841" }
                    }
                },
                BearingSize.MM_25 : {
                    BearingDimensions.MM_37_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 37 * millimeter, "width" : 7 * millimeter, "name" : "25 mm Round Radial Bearing (37 mm OD x 7 mm)", "vendor" : "WCP", "partNumber" : "WCP-0027", "url" : "https://wcproducts.com/products/wcp-0027" }
                    }
                },
                BearingSize.MM_80 : {
                    BearingDimensions.MM_100_X_10 : {
                        BearingSeal.SEALED : { "outerDiameter" : 100 * millimeter, "width" : 10 * millimeter, "name" : "80 mm Round Radial Bearing (100 mm OD x 10 mm, Sealed)", "vendor" : "WCP", "partNumber" : "WCP-0036", "url" : "https://wcproducts.com/products/wcp-0036" }
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
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "AndyMark", "partNumber" : "am-2986", "url" : "https://andymark.com/am-2986" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Hex Flanged Bearing (0.875 in OD x 0.281 in, Sealed)", "vendor" : "AndyMark", "partNumber" : "am-4489", "url" : "https://andymark.com/am-4489" }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/8 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "AndyMark", "partNumber" : "am-0692", "url" : "https://andymark.com/am-0692" }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "AndyMark", "partNumber" : "am-0030", "url" : "https://andymark.com/am-0030" },
                        BearingSeal.SEALED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in, Sealed)", "vendor" : "AndyMark", "partNumber" : "am-4465", "url" : "https://andymark.com/am-4465" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_219 : {
                        BearingSeal.OPEN : { "outerDiameter" : 0.875 * inch, "width" : 0.219 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.062 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.219 in, Open)", "vendor" : "AndyMark", "partNumber" : "am-0027", "url" : "https://andymark.com/am-0027" }
                    },
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "AndyMark", "partNumber" : "am-0028", "url" : "https://andymark.com/am-0028" },
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in, Sealed)", "vendor" : "AndyMark", "partNumber" : "am-4463", "url" : "https://andymark.com/am-4463" }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/8 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "AndyMark", "partNumber" : "am-0573", "url" : "https://andymark.com/am-0573" }
                    }
                },
                BearingSize.IN_3_16 : {
                    BearingDimensions.OD_0_5_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.196 * inch, "flangeDiameter" : 0.565 * inch, "flangeThickness" : 0.042 * inch, "name" : "3/16 in Round Flanged Bearing (0.5 in OD x 0.196 in)", "vendor" : "AndyMark", "partNumber" : "am-4459", "url" : "https://andymark.com/am-4459" }
                    }
                },
                BearingSize.MM_5 : {
                    BearingDimensions.MM_11_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 11 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 12.5 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "5 mm Round Flanged Bearing (11 mm OD x 4 mm)", "vendor" : "AndyMark", "partNumber" : "am-5606", "url" : "https://andymark.com/am-5606" }
                    },
                    BearingDimensions.MM_13_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 13 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 15 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "5 mm Round Flanged Bearing (13 mm OD x 4 mm)", "vendor" : "AndyMark", "partNumber" : "am-5469", "url" : "https://andymark.com/am-5469" }
                    }
                },
                BearingSize.MM_6 : {
                    BearingDimensions.MM_12_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 12 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 13.6 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "6 mm Round Flanged Bearing (12 mm OD x 4 mm)", "vendor" : "AndyMark", "partNumber" : "am-3377", "url" : "https://andymark.com/am-3377" }
                    },
                    BearingDimensions.MM_17_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 17 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 19 * millimeter, "flangeThickness" : 1 * millimeter, "name" : "6 mm Round Flanged Bearing (17 mm OD x 6 mm)", "vendor" : "AndyMark", "partNumber" : "am-4455", "url" : "https://andymark.com/am-4455" }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "flangeDiameter" : 22 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "8 mm Round Flanged Bearing (19 mm OD x 6 mm)", "vendor" : "AndyMark", "partNumber" : "am-0931", "url" : "https://andymark.com/am-0931" }
                    },
                    BearingDimensions.MM_22_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 25 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "8 mm Round Flanged Bearing (22 mm OD x 7 mm)", "vendor" : "AndyMark", "partNumber" : "am-4457", "url" : "https://andymark.com/am-4457" }
                    }
                },
                BearingSize.MM_15 : {
                    BearingDimensions.MM_21_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 21 * millimeter, "width" : 4 * millimeter, "flangeDiameter" : 22.5 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "15 mm Round Flanged Bearing (21 mm OD x 4 mm)", "vendor" : "AndyMark", "partNumber" : "am-5536", "url" : "https://andymark.com/am-5536" }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SEALED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Hex Radial Bearing (1.125 in OD x 0.313 in, Sealed)", "vendor" : "AndyMark", "partNumber" : "am-4703", "url" : "https://andymark.com/am-4703" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Hex Radial Bearing (0.875 in OD x 0.281 in, Sealed)", "vendor" : "AndyMark", "partNumber" : "am-4702", "url" : "https://andymark.com/am-4702" }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_25 : {
                        BearingSeal.OPEN : { "outerDiameter" : 1.125 * inch, "width" : 0.25 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.25 in, Open)", "vendor" : "AndyMark", "partNumber" : "am-0029", "url" : "https://andymark.com/am-0029" }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)", "vendor" : "AndyMark", "partNumber" : "am-0181", "url" : "https://andymark.com/am-0181" },
                        BearingSeal.SEALED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in, Sealed)", "vendor" : "AndyMark", "partNumber" : "am-4464", "url" : "https://andymark.com/am-4464" }
                    },
                    BearingDimensions.OD_1_375_X_0_438 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.375 * inch, "width" : 0.438 * inch, "name" : "1/2 in Round Radial Bearing (1.375 in OD x 0.438 in)", "vendor" : "AndyMark", "partNumber" : "am-4466", "url" : "https://andymark.com/am-4466" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_219 : {
                        BearingSeal.OPEN : { "outerDiameter" : 0.875 * inch, "width" : 0.219 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.219 in, Open)", "vendor" : "AndyMark", "partNumber" : "am-0936", "url" : "https://andymark.com/am-0936" }
                    },
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)", "vendor" : "AndyMark", "partNumber" : "am-0516", "url" : "https://andymark.com/am-0516" },
                        BearingSeal.SEALED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in, Sealed)", "vendor" : "AndyMark", "partNumber" : "am-4462", "url" : "https://andymark.com/am-4462" }
                    },
                    BearingDimensions.OD_1_125_X_0_375 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.375 * inch, "name" : "3/8 in Round Radial Bearing (1.125 in OD x 0.375 in)", "vendor" : "AndyMark", "partNumber" : "am-0209", "url" : "https://andymark.com/am-0209" }
                    }
                },
                BearingSize.IN_3_16 : {
                    BearingDimensions.OD_0_5_X_0_156 : {
                        BearingSeal.OPEN : { "outerDiameter" : 0.5 * inch, "width" : 0.156 * inch, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.156 in, Open)", "vendor" : "AndyMark", "partNumber" : "am-0031", "url" : "https://andymark.com/am-0031" }
                    },
                    BearingDimensions.OD_0_5_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.196 * inch, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.196 in)", "vendor" : "AndyMark", "partNumber" : "am-4458", "url" : "https://andymark.com/am-4458" }
                    }
                },
                BearingSize.NO_10 : {
                    BearingDimensions.OD_0_375_X_0_156_EXTENDED : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.375 * inch, "width" : 0.156 * inch, "outerWidth" : 0.125 * inch, "name" : "0.19 in Round Radial Bearing (0.375 in OD x 0.156 in (extended inner race))", "vendor" : "AndyMark", "partNumber" : "am-3681", "url" : "https://andymark.com/am-3681" }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_625_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.625 * inch, "width" : 0.196 * inch, "name" : "1/4 in Round Radial Bearing (0.625 in OD x 0.196 in)", "vendor" : "AndyMark", "partNumber" : "am-4460", "url" : "https://andymark.com/am-4460" }
                    }
                },
                BearingSize.IN_5_8 : {
                    BearingDimensions.OD_1_375_X_0_438 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.375 * inch, "width" : 0.438 * inch, "name" : "5/8 in Round Radial Bearing (1.375 in OD x 0.438 in)", "vendor" : "AndyMark", "partNumber" : "am-4467", "url" : "https://andymark.com/am-4467" }
                    }
                },
                BearingSize.MM_5 : {
                    BearingDimensions.MM_13_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 13 * millimeter, "width" : 4 * millimeter, "name" : "5 mm Round Radial Bearing (13 mm OD x 4 mm)", "vendor" : "AndyMark", "partNumber" : "am-4724", "url" : "https://andymark.com/am-4724" }
                    }
                },
                BearingSize.MM_6 : {
                    BearingDimensions.MM_12_X_4 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 12 * millimeter, "width" : 4 * millimeter, "name" : "6 mm Round Radial Bearing (12 mm OD x 4 mm)", "vendor" : "AndyMark", "partNumber" : "am-4149", "url" : "https://andymark.com/am-4149" }
                    },
                    BearingDimensions.MM_17_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 17 * millimeter, "width" : 6 * millimeter, "name" : "6 mm Round Radial Bearing (17 mm OD x 6 mm)", "vendor" : "AndyMark", "partNumber" : "am-4454", "url" : "https://andymark.com/am-4454" }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_19_X_6 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 19 * millimeter, "width" : 6 * millimeter, "name" : "8 mm Round Radial Bearing (19 mm OD x 6 mm)", "vendor" : "AndyMark", "partNumber" : "am-2095", "url" : "https://andymark.com/am-2095" }
                    },
                    BearingDimensions.MM_22_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 22 * millimeter, "width" : 7 * millimeter, "name" : "8 mm Round Radial Bearing (22 mm OD x 7 mm)", "vendor" : "AndyMark", "partNumber" : "am-4456", "url" : "https://andymark.com/am-4456" }
                    }
                },
                BearingSize.MM_10 : {
                    BearingDimensions.MM_26_X_8 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 26 * millimeter, "width" : 8 * millimeter, "name" : "10 mm Round Radial Bearing (26 mm OD x 8 mm)", "vendor" : "AndyMark", "partNumber" : "am-4468", "url" : "https://andymark.com/am-4468" }
                    }
                },
                BearingSize.MM_12 : {
                    BearingDimensions.MM_28_X_8 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 28 * millimeter, "width" : 8 * millimeter, "name" : "12 mm Round Radial Bearing (28 mm OD x 8 mm)", "vendor" : "AndyMark", "partNumber" : "am-4469", "url" : "https://andymark.com/am-4469" }
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
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "REV", "partNumber" : "REV-21-1915", "url" : "https://www.revrobotics.com/search.php?search_query=REV-21-1915&section=product" }
                    }
                }
            },
            BearingBore.ROUNDED_HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1 * inch, "width" : 0.281 * inch, "flangeDiameter" : 1.06 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1 in OD x 0.281 in)", "vendor" : "REV", "partNumber" : "REV-21-3197", "url" : "https://www.revrobotics.com/search.php?search_query=REV-21-3197&section=product" }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "REV", "partNumber" : "REV-21-1916", "url" : "https://www.revrobotics.com/search.php?search_query=REV-21-1916&section=product" }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.NO_10 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "0.19 in Round Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "REV", "partNumber" : "REV-21-3199", "url" : "https://www.revrobotics.com/search.php?search_query=REV-21-3199&section=product" }
                    }
                },
                BearingSize.MM_35 : {
                    BearingDimensions.MM_47_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 50 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm)", "vendor" : "REV", "partNumber" : "REV-29-3295", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-3295&section=product" }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.ROUND : {
                BearingSize.MM_35 : {
                    BearingDimensions.MM_47_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "name" : "35 mm Round Radial Bearing (47 mm OD x 7 mm)", "vendor" : "REV", "partNumber" : "REV-29-3296", "url" : "https://www.revrobotics.com/search.php?search_query=REV-29-3296&section=product" }
                    }
                }
            }
        }
    },
    BearingVendor.VEX : {
        BearingType.FLANGED : {
            BearingBore.HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "VEX", "partNumber" : "217-3875" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/8 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "VEX", "partNumber" : "217-2735" }
                    }
                }
            },
            BearingBore.ROUNDED_HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "VEX", "partNumber" : "217-4006" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "VEX", "partNumber" : "217-5829" }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "VEX", "partNumber" : "217-2731" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "VEX", "partNumber" : "217-2733" }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/8 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "VEX", "partNumber" : "217-2732" }
                    }
                },
                BearingSize.IN_3_4 : {
                    BearingDimensions.OD_1_375_X_0_438 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.375 * inch, "width" : 0.438 * inch, "flangeDiameter" : 1.5 * inch, "flangeThickness" : 0.063 * inch, "name" : "3/4 in Round Flanged Bearing (1.375 in OD x 0.438 in)", "vendor" : "VEX", "partNumber" : "217-2789" }
                    }
                }
            },
            BearingBore.THUNDERSQUARE : {
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "1/4 in ThunderSquare Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "VEX", "partNumber" : "217-5830" }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.ROUNDED_HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Rounded Hex Radial Bearing (1.125 in OD x 0.313 in)", "vendor" : "VEX", "partNumber" : "217-4270" }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "1/2 in Round Radial Bearing (0.875 in OD x 0.281 in)", "vendor" : "VEX", "partNumber" : "217-3337" }
                    },
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)", "vendor" : "VEX", "partNumber" : "217-3109" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)", "vendor" : "VEX", "partNumber" : "217-3239" }
                    }
                },
                BearingSize.IN_3_16 : {
                    BearingDimensions.OD_0_5_X_0_196 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.196 * inch, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.196 in)", "vendor" : "VEX", "partNumber" : "217-3246" }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_375_X_0_125 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.375 * inch, "width" : 0.125 * inch, "name" : "1/4 in Round Radial Bearing (0.375 in OD x 0.125 in)", "vendor" : "VEX", "partNumber" : "217-2788" }
                    },
                    BearingDimensions.OD_0_5_X_0_188 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.188 * inch, "name" : "1/4 in Round Radial Bearing (0.5 in OD x 0.188 in)", "vendor" : "VEX", "partNumber" : "217-3489" }
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
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0001", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0001" }
                    }
                }
            },
            BearingBore.ROUNDED_HEX : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0014", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0014" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0028", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0028" }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0025", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0025" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0008", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0008" }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_16_X_6_3 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 6.3 * millimeter, "flangeDiameter" : 18 * millimeter, "flangeThickness" : 1.1 * millimeter, "name" : "8 mm Round Flanged Bearing (16 mm OD x 6.3 mm)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0006", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0006" }
                    }
                },
                BearingSize.MM_35 : {
                    BearingDimensions.MM_47_X_7 : {
                        BearingSeal.SEALED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "flangeDiameter" : 50 * millimeter, "flangeThickness" : 1.5 * millimeter, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm, Sealed)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0065", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0065" }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0024", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0024" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0008", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0008" }
                    },
                    BearingDimensions.OD_1_125_X_0_375 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.375 * inch, "name" : "3/8 in Round Radial Bearing (1.125 in OD x 0.375 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0022", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0022" }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_375_X_0_125 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.375 * inch, "width" : 0.125 * inch, "name" : "1/4 in Round Radial Bearing (0.375 in OD x 0.125 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0021", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0021" }
                    },
                    BearingDimensions.OD_0_5_X_0_188 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.188 * inch, "name" : "1/4 in Round Radial Bearing (0.5 in OD x 0.188 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0027", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0027" }
                    },
                    BearingDimensions.OD_0_75_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.75 * inch, "width" : 0.281 * inch, "name" : "1/4 in Round Radial Bearing (0.75 in OD x 0.281 in)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0038", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0038" }
                    }
                },
                BearingSize.MM_35 : {
                    BearingDimensions.MM_47_X_7 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 47 * millimeter, "width" : 7 * millimeter, "name" : "35 mm Round Radial Bearing (47 mm OD x 7 mm)", "vendor" : "ThriftyBot", "partNumber" : "TTB-0064", "url" : "https://www.thethriftybot.com/search?type=product&q=TTB-0064" }
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
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.063 * inch, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)", "vendor" : "Swyft", "partNumber" : "BRG-1001" }
                    }
                }
            },
            BearingBore.ROUND : {
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "flangeDiameter" : 0.97 * inch, "flangeThickness" : 0.06 * inch, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)", "vendor" : "Swyft", "partNumber" : "BRG-1002" }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_14_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 14 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 15.6 * millimeter, "flangeThickness" : 0.8 * millimeter, "name" : "8 mm Round Flanged Bearing (14 mm OD x 5 mm)", "vendor" : "Swyft", "partNumber" : "BRG-8mm-14mm-01" }
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
                        BearingSeal.SHIELDED : { "outerDiameter" : 17 * millimeter, "width" : 5 * millimeter, "flangeDiameter" : 19 * millimeter, "flangeThickness" : 1.1 * millimeter, "name" : "9 mm Round Flanged Bearing (17 mm OD x 5 mm)", "vendor" : "SDS", "partNumber" : "F689ZZ" }
                    }
                }
            }
        },
        BearingType.RADIAL : {
            BearingBore.ROUND : {
                BearingSize.IN_1_2 : {
                    BearingDimensions.OD_1_125_X_0_313 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)", "vendor" : "SDS", "partNumber" : "R8ZZ" }
                    }
                },
                BearingSize.IN_3_8 : {
                    BearingDimensions.OD_0_875_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.875 * inch, "width" : 0.281 * inch, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)", "vendor" : "SDS", "partNumber" : "R6ZZ" }
                    }
                },
                BearingSize.IN_1_4 : {
                    BearingDimensions.OD_0_5_X_0_188 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.5 * inch, "width" : 0.188 * inch, "name" : "1/4 in Round Radial Bearing (0.5 in OD x 0.188 in)", "vendor" : "SDS", "partNumber" : "R188ZZ" }
                    },
                    BearingDimensions.OD_0_75_X_0_281 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 0.75 * inch, "width" : 0.281 * inch, "name" : "1/4 in Round Radial Bearing (0.75 in OD x 0.281 in)", "vendor" : "SDS", "partNumber" : "RA4ZZ" }
                    }
                },
                BearingSize.MM_8 : {
                    BearingDimensions.MM_16_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 16 * millimeter, "width" : 5 * millimeter, "name" : "8 mm Round Radial Bearing (16 mm OD x 5 mm)", "vendor" : "SDS", "partNumber" : "688ZZ" }
                    }
                },
                BearingSize.MM_15 : {
                    BearingDimensions.MM_24_X_5 : {
                        BearingSeal.SHIELDED : { "outerDiameter" : 24 * millimeter, "width" : 5 * millimeter, "name" : "15 mm Round Radial Bearing (24 mm OD x 5 mm)", "vendor" : "SDS", "partNumber" : "6802ZZ" }
                    }
                }
            }
        }
    }
};
