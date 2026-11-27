FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/* Generated from hatTables.py by `fs gen` -- DO NOT EDIT */

export enum BearingHatVendor
{
    annotation { "Name" : "WCP" }
    WCP
}

export enum BearingHatSize
{
    annotation { "Name" : "1.125 in" }
    OD_1_125,
    annotation { "Name" : "0.875 in" }
    OD_0_875
}

export enum BearingHatCenters
{
    annotation { "Name" : "2 in" }
    IN_2,
    annotation { "Name" : "1.5 in" }
    IN_1_5,
    annotation { "Name" : "1.25 in" }
    IN_1_25
}

/** What each vendor makes, by vendor, size and centers: each plate's sizes and properties. The sizes are estimates (see hatTables.py). */
export const BEARING_HATS = {
    BearingHatVendor.WCP : {
        BearingHatSize.OD_0_875 : {
            BearingHatCenters.IN_1_25 : { "outerDiameter" : 0.875 * inch, "centers" : 1.25 * inch, "thickness" : 0.09 * inch, "rim" : 0.125 * inch, "lobeRadius" : 0.25 * inch, "holeDiameter" : 0.201 * inch, "innerDiameter" : 0.75 * inch, "name" : "0.875\" OD Bearing Retention Plate (1.25\" C-C)", "vendor" : "West Coast Products", "partNumber" : "WCP-0487" },
            BearingHatCenters.IN_2 : { "outerDiameter" : 0.875 * inch, "centers" : 2 * inch, "thickness" : 0.09 * inch, "rim" : 0.125 * inch, "lobeRadius" : 0.25 * inch, "holeDiameter" : 0.201 * inch, "innerDiameter" : 0.75 * inch, "name" : "0.875\" OD Bearing Retention Plate (2\" C-C)", "vendor" : "West Coast Products", "partNumber" : "WCP-0327" }
        },
        BearingHatSize.OD_1_125 : {
            BearingHatCenters.IN_1_5 : { "outerDiameter" : 1.125 * inch, "centers" : 1.5 * inch, "thickness" : 0.09 * inch, "rim" : 0.125 * inch, "lobeRadius" : 0.25 * inch, "holeDiameter" : 0.201 * inch, "innerDiameter" : 1 * inch, "name" : "1.125\" OD Bearing Retention Plate (1.5\" C-C)", "vendor" : "West Coast Products", "partNumber" : "WCP-0488" },
            BearingHatCenters.IN_2 : { "outerDiameter" : 1.125 * inch, "centers" : 2 * inch, "thickness" : 0.09 * inch, "rim" : 0.125 * inch, "lobeRadius" : 0.25 * inch, "holeDiameter" : 0.201 * inch, "innerDiameter" : 1 * inch, "name" : "1.125\" OD Bearing Retention Plate (2\" C-C)", "vendor" : "West Coast Products", "partNumber" : "WCP-0328" }
        }
    }
};
