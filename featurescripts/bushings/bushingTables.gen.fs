FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "core/robotProperties.fs", version : ""); // core/robotProperties.fs

/* Generated from bushingTables.py by `fs gen` -- DO NOT EDIT */

export enum BushingVendor
{
    annotation { "Name" : "Generic" }
    GENERIC,
    annotation { "Name" : "REV" }
    REV,
    annotation { "Name" : "ThriftyBot" }
    THRIFTYBOT
}

export enum BushingMaterial
{
    annotation { "Name" : "Bronze" }
    BRONZE,
    annotation { "Name" : "Nylon" }
    NYLON
}

export enum BushingBore
{
    annotation { "Name" : "Rounded hex" }
    ROUNDED_HEX,
    annotation { "Name" : "Round" }
    ROUND
}

export enum BushingSize
{
    annotation { "Name" : "1/2 in" }
    IN_1_2,
    annotation { "Name" : "3/8 in" }
    IN_3_8,
    annotation { "Name" : "3/4 in" }
    IN_3_4,
    annotation { "Name" : "7/8 in" }
    IN_7_8,
    annotation { "Name" : "1 in" }
    IN_1
}

export enum BushingDimensions
{
    annotation { "Name" : "0.625 in OD x 0.375 in" }
    OD_0_625_X_0_375,
    annotation { "Name" : "0.75 in OD x 0.333 in" }
    OD_0_75_X_0_333,
    annotation { "Name" : "0.75 in OD x 0.375 in" }
    OD_0_75_X_0_375,
    annotation { "Name" : "1 in OD x 0.563 in (1.125 in step)" }
    OD_1_X_0_563_STEP,
    annotation { "Name" : "1.125 in OD x 0.333 in" }
    OD_1_125_X_0_333
}

/** Each bore's shape: a round bore's `diameter`, or a rounded hex's `acrossFlats` and the `diameter` its corners are rounded to. */
export const BUSHING_BORES = {
    BushingBore.ROUNDED_HEX : {
        BushingSize.IN_1_2 : { "acrossFlats" : 0.5 * inch, "diameter" : 13.75 * millimeter },
        BushingSize.IN_3_8 : { "acrossFlats" : 0.375 * inch, "diameter" : 10.25 * millimeter }
    },
    BushingBore.ROUND : {
        BushingSize.IN_3_4 : { "diameter" : 0.75 * inch },
        BushingSize.IN_7_8 : { "diameter" : 0.875 * inch },
        BushingSize.IN_1 : { "diameter" : 1 * inch }
    }
};

/** What each vendor makes, by vendor, material, bore, size, and dimensions: each bushing's sizes, its flange (and step), and its properties. */
export const BUSHINGS = {
    BushingVendor.GENERIC : {
        BushingMaterial.BRONZE : {
            BushingBore.ROUNDED_HEX : {
                BushingSize.IN_1_2 : {
                    BushingDimensions.OD_0_75_X_0_333 : { "outerDiameter" : 0.75 * inch, "width" : 0.333 * inch, "flangeDiameter" : 0.875 * inch, "flangeThickness" : 0.083 * inch, "name" : "1/2 in Rounded Hex Flanged Bronze Bushing (0.75 in OD x 0.333 in)", "material" : BRONZE, "appearance" : BRASS_YELLOW }
                },
                BushingSize.IN_3_8 : {
                    BushingDimensions.OD_0_625_X_0_375 : { "outerDiameter" : 0.625 * inch, "width" : 0.375 * inch, "flangeDiameter" : 0.75 * inch, "flangeThickness" : 0.0825 * inch, "name" : "3/8 in Rounded Hex Flanged Bronze Bushing (0.625 in OD x 0.375 in)", "material" : BRONZE, "appearance" : BRASS_YELLOW }
                }
            },
            BushingBore.ROUND : {
                BushingSize.IN_3_4 : {
                    BushingDimensions.OD_1_X_0_563_STEP : { "outerDiameter" : 1 * inch, "width" : 0.563 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.0625 * inch, "stepDiameter" : 1.125 * inch, "stepHeight" : 0.125 * inch, "name" : "3/4 in Round Flanged Bronze Bushing (1 in OD x 0.563 in (1.125 in step))", "material" : BRONZE, "appearance" : BRASS_YELLOW },
                    BushingDimensions.OD_1_125_X_0_333 : { "outerDiameter" : 1.125 * inch, "width" : 0.333 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.083 * inch, "name" : "3/4 in Round Flanged Bronze Bushing (1.125 in OD x 0.333 in)", "material" : BRONZE, "appearance" : BRASS_YELLOW }
                },
                BushingSize.IN_7_8 : {
                    BushingDimensions.OD_1_125_X_0_333 : { "outerDiameter" : 1.125 * inch, "width" : 0.333 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.083 * inch, "name" : "7/8 in Round Flanged Bronze Bushing (1.125 in OD x 0.333 in)", "material" : BRONZE, "appearance" : BRASS_YELLOW }
                },
                BushingSize.IN_1 : {
                    BushingDimensions.OD_1_125_X_0_333 : { "outerDiameter" : 1.125 * inch, "width" : 0.333 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.083 * inch, "name" : "1 in Round Flanged Bronze Bushing (1.125 in OD x 0.333 in)", "material" : BRONZE, "appearance" : BRASS_YELLOW }
                }
            }
        },
        BushingMaterial.NYLON : {
            BushingBore.ROUNDED_HEX : {
                BushingSize.IN_1_2 : {
                    BushingDimensions.OD_0_75_X_0_375 : { "outerDiameter" : 0.75 * inch, "width" : 0.375 * inch, "flangeDiameter" : 0.875 * inch, "flangeThickness" : 0.125 * inch, "name" : "1/2 in Rounded Hex Flanged Nylon Bushing (0.75 in OD x 0.375 in)", "material" : NYLON, "appearance" : BLACK }
                }
            }
        }
    },
    BushingVendor.REV : {
        BushingMaterial.BRONZE : {
            BushingBore.ROUND : {
                BushingSize.IN_3_4 : {
                    BushingDimensions.OD_1_X_0_563_STEP : { "outerDiameter" : 1 * inch, "width" : 0.563 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.0625 * inch, "stepDiameter" : 1.125 * inch, "stepHeight" : 0.125 * inch, "name" : "3/4 in Round Flanged Bronze Bushing (1 in OD x 0.563 in (1.125 in step))", "vendor" : "REV Robotics", "partNumber" : "REV-21-2396", "material" : BRONZE, "appearance" : BRASS_YELLOW }
                }
            }
        }
    },
    BushingVendor.THRIFTYBOT : {
        BushingMaterial.BRONZE : {
            BushingBore.ROUNDED_HEX : {
                BushingSize.IN_1_2 : {
                    BushingDimensions.OD_0_75_X_0_333 : { "outerDiameter" : 0.75 * inch, "width" : 0.333 * inch, "flangeDiameter" : 0.875 * inch, "flangeThickness" : 0.083 * inch, "name" : "1/2 in Rounded Hex Flanged Bronze Bushing (0.75 in OD x 0.333 in)", "vendor" : "The Thrifty Bot", "partNumber" : "TTB-0267", "material" : BRONZE, "appearance" : BRASS_YELLOW }
                },
                BushingSize.IN_3_8 : {
                    BushingDimensions.OD_0_625_X_0_375 : { "outerDiameter" : 0.625 * inch, "width" : 0.375 * inch, "flangeDiameter" : 0.75 * inch, "flangeThickness" : 0.0825 * inch, "name" : "3/8 in Rounded Hex Flanged Bronze Bushing (0.625 in OD x 0.375 in)", "vendor" : "The Thrifty Bot", "partNumber" : "TTB-0266", "material" : BRONZE, "appearance" : BRASS_YELLOW }
                }
            },
            BushingBore.ROUND : {
                BushingSize.IN_3_4 : {
                    BushingDimensions.OD_1_125_X_0_333 : { "outerDiameter" : 1.125 * inch, "width" : 0.333 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.083 * inch, "name" : "3/4 in Round Flanged Bronze Bushing (1.125 in OD x 0.333 in)", "vendor" : "The Thrifty Bot", "partNumber" : "TTB-0268", "material" : BRONZE, "appearance" : BRASS_YELLOW }
                },
                BushingSize.IN_7_8 : {
                    BushingDimensions.OD_1_125_X_0_333 : { "outerDiameter" : 1.125 * inch, "width" : 0.333 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.083 * inch, "name" : "7/8 in Round Flanged Bronze Bushing (1.125 in OD x 0.333 in)", "vendor" : "The Thrifty Bot", "partNumber" : "TTB-0269", "material" : BRONZE, "appearance" : BRASS_YELLOW }
                },
                BushingSize.IN_1 : {
                    BushingDimensions.OD_1_125_X_0_333 : { "outerDiameter" : 1.125 * inch, "width" : 0.333 * inch, "flangeDiameter" : 1.225 * inch, "flangeThickness" : 0.083 * inch, "name" : "1 in Round Flanged Bronze Bushing (1.125 in OD x 0.333 in)", "vendor" : "The Thrifty Bot", "partNumber" : "TTB-0270", "material" : BRONZE, "appearance" : BRASS_YELLOW }
                }
            }
        },
        BushingMaterial.NYLON : {
            BushingBore.ROUNDED_HEX : {
                BushingSize.IN_1_2 : {
                    BushingDimensions.OD_0_75_X_0_375 : { "outerDiameter" : 0.75 * inch, "width" : 0.375 * inch, "flangeDiameter" : 0.875 * inch, "flangeThickness" : 0.125 * inch, "name" : "1/2 in Rounded Hex Flanged Nylon Bushing (0.75 in OD x 0.375 in)", "vendor" : "The Thrifty Bot", "partNumber" : "TTB-0004", "material" : NYLON, "appearance" : BLACK }
                }
            }
        }
    }
};
