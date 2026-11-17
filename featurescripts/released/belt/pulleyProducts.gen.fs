FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "d82c5bf9082d0054f8f0b419", version : "abd856d63642589f61cbad83"); // released/belt/robotBeltTables.gen.fs

/* Generated from pulleyProducts.py by `fs gen` -- DO NOT EDIT */

export const pulleyProductTable = {
        "name" : "vendor",
        "displayName" : "Vendor",
        "default" : "WCP",
        "entries" : {
            "WCP" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : { "bore" : "1/2\" Hex", "vendor" : "WCP" },
                    "8mm SplineXS" : { "bore" : "8mm SplineXS", "vendor" : "WCP" },
                    "Falcon Spline" : { "bore" : "Falcon Spline", "vendor" : "WCP" },
                    "8mm CIM/NEO" : { "bore" : "8mm CIM/NEO", "vendor" : "WCP" },
                    "550" : { "bore" : "550", "vendor" : "WCP" },
                    "775" : { "bore" : "775", "vendor" : "WCP" },
                },
            },
            "The Thrifty Bot" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : { "bore" : "1/2\" Hex", "vendor" : "TTB" },
                    "8mm SplineXS" : { "bore" : "8mm SplineXS", "vendor" : "TTB" },
                    "Falcon Spline" : { "bore" : "Falcon Spline", "vendor" : "TTB" },
                    "8mm CIM/NEO" : { "bore" : "8mm CIM/NEO", "vendor" : "TTB" },
                    "1.125\" Round" : { "bore" : "1.125\" Round", "vendor" : "TTB" },
                },
            },
            "REV" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : { "bore" : "1/2\" Hex", "vendor" : "REV" },
                    "8mm CIM/NEO" : { "bore" : "8mm CIM/NEO", "vendor" : "REV" },
                    "MAXSpline" : { "bore" : "MAXSpline", "vendor" : "REV" },
                    "550" : { "bore" : "550", "vendor" : "REV" },
                },
            },
            "AndyMark" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : { "bore" : "1/2\" Hex", "vendor" : "AM" },
                    "3/8\" Hex" : { "bore" : "3/8\" Hex", "vendor" : "AM" },
                    "1.125\" Round" : { "bore" : "1.125\" Round", "vendor" : "AM" },
                    "0.958\" Round" : { "bore" : "0.958\" Round", "vendor" : "AM" },
                },
            },
            "VEX" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : { "bore" : "1/2\" Hex", "vendor" : "VEX" },
                    "Falcon Spline" : { "bore" : "Falcon Spline", "vendor" : "VEX" },
                    "1.125\" Round" : { "bore" : "1.125\" Round", "vendor" : "VEX" },
                    "550" : { "bore" : "550", "vendor" : "VEX" },
                    "775" : { "bore" : "775", "vendor" : "VEX" },
                },
            },
            "LAI" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : { "bore" : "1/2\" Hex", "vendor" : "LAI" },
                },
            },
        },
    };

export const PULLEY_PRODUCTS = {
        "WCP" : {
            "1/2\" Hex" : [
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 24, "width" : 9 * millimeter, "name" : "24t Aluminum Pulley (GT2 3mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-0662", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "20e0bbb1b40c631ffc67166e", "configuration" : { "List_diSBXaQ9V17QLz" : "Default" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 36, "width" : 9 * millimeter, "name" : "36t Aluminum Pulley (GT2 3mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-0663", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "20e0bbb1b40c631ffc67166e", "configuration" : { "List_diSBXaQ9V17QLz" : "_36t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 48, "width" : 9 * millimeter, "name" : "48t Aluminum Pulley (GT2 3mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-0664", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "20e0bbb1b40c631ffc67166e", "configuration" : { "List_diSBXaQ9V17QLz" : "_48t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 60, "width" : 9 * millimeter, "name" : "60t Aluminum Pulley (GT2 3mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-0665", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "20e0bbb1b40c631ffc67166e", "configuration" : { "List_diSBXaQ9V17QLz" : "_60t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 15, "width" : 9 * millimeter, "name" : "15t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-1420", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b568cf62768f549c2f067e7c", "configuration" : { "List_b49DPDa7oomn2X" : "Default" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 15, "width" : 15 * millimeter, "name" : "15t Aluminum Pulley (HTD 5mm, 15mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-0562", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b568cf62768f549c2f067e7c", "configuration" : { "List_b49DPDa7oomn2X" : "_12t_x_15mm_Wide" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 18, "width" : 9 * millimeter, "name" : "18t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-0563", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b568cf62768f549c2f067e7c", "configuration" : { "List_b49DPDa7oomn2X" : "_18t_x_9mm_Wide" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 18, "width" : 15 * millimeter, "name" : "18t Aluminum Pulley (HTD 5mm, 15mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-0991", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b568cf62768f549c2f067e7c", "configuration" : { "List_b49DPDa7oomn2X" : "_18t_x_9mm_Wide0" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 20, "width" : 9 * millimeter, "name" : "20t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-1754", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b568cf62768f549c2f067e7c", "configuration" : { "List_b49DPDa7oomn2X" : "_20t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 20, "width" : 15 * millimeter, "name" : "20t Aluminum Pulley (HTD 5mm, 15mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-1755", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b568cf62768f549c2f067e7c", "configuration" : { "List_b49DPDa7oomn2X" : "_20t_x_15mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 24, "width" : 9 * millimeter, "name" : "24t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-0564", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b568cf62768f549c2f067e7c", "configuration" : { "List_b49DPDa7oomn2X" : "_24t_x" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 24, "width" : 15 * millimeter, "name" : "24t Aluminum Pulley (HTD 5mm, 15mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-0992", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b568cf62768f549c2f067e7c", "configuration" : { "List_b49DPDa7oomn2X" : "_24t_x_15mm_Width" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 30, "width" : 9 * millimeter, "name" : "30t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-0565", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b568cf62768f549c2f067e7c", "configuration" : { "List_b49DPDa7oomn2X" : "_30t_x_9mm_Width" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 30, "width" : 15 * millimeter, "name" : "30t Aluminum Pulley (HTD 5mm, 15mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-0993", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b568cf62768f549c2f067e7c", "configuration" : { "List_b49DPDa7oomn2X" : "_30t_x_15mm_Width" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 36, "width" : 9 * millimeter, "name" : "36t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-0990", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b568cf62768f549c2f067e7c", "configuration" : { "List_b49DPDa7oomn2X" : "_36t_x_9mm_Width" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 36, "width" : 15 * millimeter, "name" : "36t Aluminum Pulley (HTD 5mm, 15mm Wide, 1/2\" Hex Bore)", "partNumber" : "WCP-0994", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b568cf62768f549c2f067e7c", "configuration" : { "List_b49DPDa7oomn2X" : "_36t_x_15mm_Width" }, "boreDiameter" : 0.5774 * inch }
            ],
            "8mm SplineXS" : [
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 16, "width" : 9 * millimeter, "name" : "16t Aluminum Pulley (GT2 3mm, 9mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-1178", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "096f4cd30a1e062da4489310", "configuration" : { "List_X6MBcLsuyfMsDH" : "_16t_x_9mm__GT2_3mm___WCP_" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 16, "width" : 15 * millimeter, "name" : "16t Aluminum Pulley (GT2 3mm, 15mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-1179", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "096f4cd30a1e062da4489310", "configuration" : { "List_X6MBcLsuyfMsDH" : "_16t_x_15mm__GT2_3mm___WCP_" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 12, "width" : 9 * millimeter, "name" : "12t Aluminum Pulley (HTD 5mm, 9mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-2576", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "Copy_of_12t_x_9mm__Legacy_" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 12, "width" : 15 * millimeter, "name" : "12t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm SplineXS Bore, Legacy)", "partNumber" : "WCP-1018", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "Copy_of_12t_x_15mm" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 14, "width" : 9 * millimeter, "name" : "14t Aluminum Pulley (HTD 5mm, 9mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-1801", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "_14t_x_9mm__HTD_5mm_" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 14, "width" : 15 * millimeter, "name" : "14t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-1803", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "_14t_x_15mm__HTD_5mm_" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 15, "width" : 9 * millimeter, "name" : "15t Aluminum Pulley (HTD 5mm, 9mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-2253", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "Copy_of_14t_x_15mm" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 15, "width" : 15 * millimeter, "name" : "15t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-2258", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "Copy_of_Copy_of_14t_x_15mm" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 16, "width" : 9 * millimeter, "name" : "16t Aluminum Pulley (HTD 5mm, 9mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-1802", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "_16t_x_9mm__HTD_5mm_" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 16, "width" : 15 * millimeter, "name" : "16t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-1804", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "_14t_x_15mm__HTD_5mm_0" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 18, "width" : 9 * millimeter, "name" : "18t Aluminum Pulley (HTD 5mm, 9mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-1634", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "_14t_x_9mm__HTD_5mm_0" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 18, "width" : 15 * millimeter, "name" : "18t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-1805", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "_18t_x_15mm__HTD_5mm_" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 20, "width" : 9 * millimeter, "name" : "20t Aluminum Pulley (HTD 5mm, 9mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-2254", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "_20t_x_9mm" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 20, "width" : 15 * millimeter, "name" : "20t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-2259", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "_20t_x_15mm" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 24, "width" : 9 * millimeter, "name" : "24t Aluminum Pulley (HTD 5mm, 9mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-2255", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "_24t_x_9mm" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 24, "width" : 15 * millimeter, "name" : "24t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-2260", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "_24t_x_15mm" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 30, "width" : 9 * millimeter, "name" : "30t Aluminum Pulley (HTD 5mm, 9mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-2256", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "_30t_x_9mm" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 30, "width" : 15 * millimeter, "name" : "30t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-2261", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "_30t_x_15mm" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 36, "width" : 9 * millimeter, "name" : "36t Aluminum Pulley (HTD 5mm, 9mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-2257", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "_36t_x_9mm" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 36, "width" : 15 * millimeter, "name" : "36t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm SplineXS Bore)", "partNumber" : "WCP-2262", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "93606c837542e027d88bd777", "configuration" : { "List_X6MBcLsuyfMsDH" : "_36t_x_15mm" }, "boreDiameter" : 8 * millimeter }
            ],
            "Falcon Spline" : [
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 12, "width" : 9 * millimeter, "name" : "12t Aluminum Pulley (HTD 5mm, 9mm Wide, Falcon Spline Bore)", "partNumber" : "WCP-0454", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "3fbf8e485e773b8bfa867e15", "configuration" : { "List_Wu9Tk6ElZc2oKP" : "_12t_x_9mm_HTD_5mm" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 16, "width" : 9 * millimeter, "name" : "16t Aluminum Pulley (GT2 3mm, 9mm Wide, Falcon Spline Bore)", "partNumber" : "WCP-0603", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "3fbf8e485e773b8bfa867e15", "configuration" : { "List_Wu9Tk6ElZc2oKP" : "Default" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 16, "width" : 15 * millimeter, "name" : "16t Aluminum Pulley (GT2 3mm, 15mm Wide, Falcon Spline Bore)", "partNumber" : "WCP-0318", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "3fbf8e485e773b8bfa867e15", "configuration" : { "List_Wu9Tk6ElZc2oKP" : "_16t_x_15mm_Width" }, "boreDiameter" : 8 * millimeter }
            ],
            "8mm CIM/NEO" : [
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 12, "width" : 9 * millimeter, "name" : "12t Aluminum Pulley (HTD 5mm, 9mm Wide, 8mm Keyed Bore)", "partNumber" : "WCP-0076", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "0b7e71624500a351e63d4c2b", "configuration" : { "List_YWji3ZOXT2GsiV" : "_12t_x_9mm__HTD_5mm___WCP_" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 12, "width" : 15 * millimeter, "name" : "12t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm Keyed Bore)", "partNumber" : "WCP-0077", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "0b7e71624500a351e63d4c2b", "configuration" : { "List_YWji3ZOXT2GsiV" : "_12t_x_18mm__HTD_5mm_" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 18, "width" : 9 * millimeter, "name" : "18t Aluminum Pulley (HTD 5mm, 9mm Wide, 8mm Keyed Bore)", "partNumber" : "WCP-1659", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "0b7e71624500a351e63d4c2b", "configuration" : { "List_YWji3ZOXT2GsiV" : "_18t_x_9mm__HTD_5mm_" }, "boreDiameter" : 8 * millimeter }
            ],
            "550" : [
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 12, "width" : 9 * millimeter, "name" : "12t Aluminum Pulley (GT2 3mm, 9mm Wide, 550 Bore)", "partNumber" : "WCP-0614", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b1a5445f24f8c62c0f09e66d", "configuration" : { "List_8m6tHp0cNDeSzI" : "_12t_x_9mm__GT2_3mm___WCP_" } }
            ],
            "775" : [
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 12, "width" : 9 * millimeter, "name" : "12t Aluminum Pulley (GT2 3mm, 9mm Wide, 775 Bore)", "partNumber" : "WCP-0616", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "efaae733303a688ff125f0b2", "configuration" : { "List_n2M4pCTF3zUFMO" : "Default" } }
            ]
        },
        "TTB" : {
            "1/2\" Hex" : [
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 15, "width" : 9.5 * millimeter, "name" : "15t Aluminum Pulley (HTD 5mm, 9.5mm Wide, 1/2\" Hex Bore)", "partNumber" : "TTB-0435", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "81ac4d707b69ba4fb7a8b60b", "configuration" : { "List_33MiSyP9U9NQ1D" : "_15t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 15, "width" : 18.5 * millimeter, "name" : "15t Aluminum Pulley (HTD 5mm, 18.5mm Wide, 1/2\" Hex Bore)", "partNumber" : "TTB-0058", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "81ac4d707b69ba4fb7a8b60b", "configuration" : { "List_33MiSyP9U9NQ1D" : "Copy_of_18t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 18, "width" : 9.5 * millimeter, "name" : "18t Aluminum Pulley (HTD 5mm, 9.5mm Wide, 1/2\" Hex Bore)", "partNumber" : "TTB-0346", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "81ac4d707b69ba4fb7a8b60b", "configuration" : { "List_33MiSyP9U9NQ1D" : "_18t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 18, "width" : 18.5 * millimeter, "name" : "18t Aluminum Pulley (HTD 5mm, 18.5mm Wide, 1/2\" Hex Bore)", "partNumber" : "TTB-0347", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "81ac4d707b69ba4fb7a8b60b", "configuration" : { "List_33MiSyP9U9NQ1D" : "_18t_x_18mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 24, "width" : 9.5 * millimeter, "name" : "24t Aluminum Pulley (HTD 5mm, 9.5mm Wide, 1/2\" Hex Bore)", "partNumber" : "TTB-0348", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "81ac4d707b69ba4fb7a8b60b", "configuration" : { "List_33MiSyP9U9NQ1D" : "_24t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 24, "width" : 18.5 * millimeter, "name" : "24t Aluminum Pulley (HTD 5mm, 18.5mm Wide, 1/2\" Hex Bore)", "partNumber" : "TTB-0126", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "81ac4d707b69ba4fb7a8b60b", "configuration" : { "List_33MiSyP9U9NQ1D" : "Copy_of_24t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 36, "width" : 9.5 * millimeter, "name" : "36t Aluminum Pulley (HTD 5mm, 9.5mm Wide, 1/2\" Hex Bore)", "partNumber" : "TTB-0446", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "81ac4d707b69ba4fb7a8b60b", "configuration" : { "List_33MiSyP9U9NQ1D" : "_36t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 36, "width" : 18.5 * millimeter, "name" : "36t Aluminum Pulley (HTD 5mm, 18.5mm Wide, 1/2\" Hex Bore, Lightened)", "partNumber" : "TTB-0349", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "81ac4d707b69ba4fb7a8b60b", "configuration" : { "List_33MiSyP9U9NQ1D" : "Copy_of_36t_x_9mm" }, "boreDiameter" : 0.5774 * inch }
            ],
            "8mm SplineXS" : [
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 11, "width" : 15 * millimeter, "name" : "11t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm SplineXS Bore)", "partNumber" : "TTB-0125", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "91fde2019e187d8ccd573c49", "configuration" : { "List_X6MBcLsuyfMsDH" : "_12t_x_9mm__HTD_5mm___WCP_" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 12, "width" : 9 * millimeter, "name" : "12t Aluminum Pulley (HTD 5mm, 9mm Wide, 8mm SplineXS Bore)", "partNumber" : "TTB-0339", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "91fde2019e187d8ccd573c49", "configuration" : { "List_X6MBcLsuyfMsDH" : "Default" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 12, "width" : 15 * millimeter, "name" : "12t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm SplineXS Bore)", "partNumber" : "TTB-0340", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "91fde2019e187d8ccd573c49", "configuration" : { "List_X6MBcLsuyfMsDH" : "_12t_x_15mm__HTD_5mm___WCP_" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 14, "width" : 9 * millimeter, "name" : "14t Aluminum Pulley (HTD 5mm, 9mm Wide, 8mm SplineXS Bore)", "partNumber" : "TTB-0341", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "91fde2019e187d8ccd573c49", "configuration" : { "List_X6MBcLsuyfMsDH" : "_14t_x_9mm__HTD_5mm_" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 14, "width" : 15 * millimeter, "name" : "14t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm SplineXS Bore)", "partNumber" : "TTB-0342", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "91fde2019e187d8ccd573c49", "configuration" : { "List_X6MBcLsuyfMsDH" : "_14t_x_15mm__HTD_5mm_" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 16, "width" : 9 * millimeter, "name" : "16t Aluminum Pulley (HTD 5mm, 9mm Wide, 8mm SplineXS Bore)", "partNumber" : "TTB-0343", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "91fde2019e187d8ccd573c49", "configuration" : { "List_X6MBcLsuyfMsDH" : "_16t_x_9mm__HTD_5mm_" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 16, "width" : 15 * millimeter, "name" : "16t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm SplineXS Bore)", "partNumber" : "TTB-0344", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "91fde2019e187d8ccd573c49", "configuration" : { "List_X6MBcLsuyfMsDH" : "_14t_x_15mm__HTD_5mm_0" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 16, "width" : 9 * millimeter, "name" : "16t Aluminum Pulley (GT2 3mm, 9mm Wide, 8mm SplineXS Bore)", "partNumber" : "TTB-0345", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "91fde2019e187d8ccd573c49", "configuration" : { "List_X6MBcLsuyfMsDH" : "_16t_x_9mm__GT2_3mm___WCP_" }, "boreDiameter" : 8 * millimeter }
            ],
            "Falcon Spline" : [
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 11, "width" : 18.5 * millimeter, "name" : "11t Aluminum Pulley (HTD 5mm, 18.5mm Wide, Falcon Spline Bore)", "partNumber" : "TTB-0062", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "3fbf8e485e773b8bfa867e15", "configuration" : { "List_Wu9Tk6ElZc2oKP" : "_11t_x_18_5mm_HTD_5mm__TTB_" }, "boreDiameter" : 8 * millimeter }
            ],
            "8mm CIM/NEO" : [
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 11, "width" : 15 * millimeter, "name" : "11t Aluminum Pulley (HTD 5mm, 15mm Wide, 8mm Keyed Bore)", "partNumber" : "TTB-0061", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "0b7e71624500a351e63d4c2b", "configuration" : { "List_YWji3ZOXT2GsiV" : "_11t_18_5mm__HTD_5mm___TTB_" }, "boreDiameter" : 8 * millimeter }
            ],
            "1.125\" Round" : [
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 48, "width" : 18.5 * millimeter, "name" : "48t Aluminum Pulley (HTD 5mm, 18.5mm Wide, 1.125\" Round Bore)", "partNumber" : "TTB-0128", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "d148072de8ea8b794663e7a0", "configuration" : { "List_aet5kgYH6v1GDj" : "Default" }, "boreDiameter" : 1.125 * inch }
            ]
        },
        "REV" : {
            "1/2\" Hex" : [
                { "beltType" : BeltType.RT25, "teeth" : 16, "width" : 0.5 * inch, "name" : "16t Aluminum Pulley (RT25, 1/2\" Wide, 1/2\" Hex Bore)", "partNumber" : "REV-21-2205", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b2b2c7e65105d8b88f38a7f0", "configuration" : { "List_kLhzNGEJGs4UMj" : "Default" }, "boreDiameter" : 0.5774 * inch }
            ],
            "8mm CIM/NEO" : [
                { "beltType" : BeltType.RT25, "teeth" : 12, "width" : 0.5 * inch, "name" : "12t Aluminum Pulley (RT25, 1/2\" Wide, 8mm Keyed Bore)", "partNumber" : "REV-21-2200", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "0b7e71624500a351e63d4c2b", "configuration" : { "List_YWji3ZOXT2GsiV" : "_12t" }, "boreDiameter" : 8 * millimeter },
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 16, "width" : 15 * millimeter, "name" : "16t Aluminum Pulley (GT2 3mm, 15mm Wide, 8mm Keyed Bore)", "partNumber" : "REV-21-1909", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "0b7e71624500a351e63d4c2b", "configuration" : { "List_YWji3ZOXT2GsiV" : "Default" }, "boreDiameter" : 8 * millimeter }
            ],
            "MAXSpline" : [
                { "beltType" : BeltType.RT25, "teeth" : 40, "width" : 0.5 * inch, "name" : "40t Plastic Pulley (RT25, 1/2\" Wide, MAXSpline Bore)", "partNumber" : "REV-25-2248", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e1e749203c17965a05557e98", "configuration" : { "List_KOc9hmQqjnTpbV" : "_40t", "List_aNwPoODfdqNaXN" : "Default" }, "boreDiameter" : 1.375 * inch },
                { "beltType" : BeltType.RT25, "teeth" : 24, "width" : 0.5 * inch, "name" : "24t Plastic Pulley (RT25, 1/2\" Wide, MAXSpline Bore)", "partNumber" : "REV-25-2224", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e1e749203c17965a05557e98", "configuration" : { "List_KOc9hmQqjnTpbV" : "Default", "List_aNwPoODfdqNaXN" : "Default" }, "boreDiameter" : 1.375 * inch },
                { "beltType" : BeltType.RT25, "teeth" : 24, "width" : 1 * inch, "name" : "24t Plastic Pulley (RT25, 1\" Wide, MAXSpline Bore)", "partNumber" : "REV-25-2224", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e1e749203c17965a05557e98", "configuration" : { "List_KOc9hmQqjnTpbV" : "Default", "List_aNwPoODfdqNaXN" : "Double" }, "boreDiameter" : 1.375 * inch },
                { "beltType" : BeltType.RT25, "teeth" : 32, "width" : 0.5 * inch, "name" : "32t Plastic Pulley (RT25, 1/2\" Wide, MAXSpline Bore)", "partNumber" : "REV-25-2236", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e1e749203c17965a05557e98", "configuration" : { "List_KOc9hmQqjnTpbV" : "_32t", "List_aNwPoODfdqNaXN" : "Default" }, "boreDiameter" : 1.375 * inch },
                { "beltType" : BeltType.RT25, "teeth" : 32, "width" : 1 * inch, "name" : "32t Plastic Pulley (RT25, 1\" Wide, MAXSpline Bore)", "partNumber" : "REV-25-2236", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e1e749203c17965a05557e98", "configuration" : { "List_KOc9hmQqjnTpbV" : "_32t", "List_aNwPoODfdqNaXN" : "Double" }, "boreDiameter" : 1.375 * inch },
                { "beltType" : BeltType.RT25, "teeth" : 40, "width" : 1 * inch, "name" : "40t Plastic Pulley (RT25, 1\" Wide, MAXSpline Bore)", "partNumber" : "REV-25-2248", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e1e749203c17965a05557e98", "configuration" : { "List_KOc9hmQqjnTpbV" : "_40t", "List_aNwPoODfdqNaXN" : "Double" }, "boreDiameter" : 1.375 * inch },
                { "beltType" : BeltType.RT25, "teeth" : 48, "width" : 0.5 * inch, "name" : "48t Plastic Pulley (RT25, 1/2\" Wide, MAXSpline Bore)", "partNumber" : "REV-25-2260", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e1e749203c17965a05557e98", "configuration" : { "List_KOc9hmQqjnTpbV" : "_48t", "List_aNwPoODfdqNaXN" : "Default" }, "boreDiameter" : 1.375 * inch },
                { "beltType" : BeltType.RT25, "teeth" : 48, "width" : 1 * inch, "name" : "48t Plastic Pulley (RT25, 1\" Wide, MAXSpline Bore)", "partNumber" : "REV-25-2260", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e1e749203c17965a05557e98", "configuration" : { "List_KOc9hmQqjnTpbV" : "_48t", "List_aNwPoODfdqNaXN" : "Double" }, "boreDiameter" : 1.375 * inch },
                { "beltType" : BeltType.RT25, "teeth" : 56, "width" : 0.5 * inch, "name" : "56t Plastic Pulley (RT25, 1/2\" Wide, MAXSpline Bore)", "partNumber" : "REV-25-2272", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e1e749203c17965a05557e98", "configuration" : { "List_KOc9hmQqjnTpbV" : "_56t", "List_aNwPoODfdqNaXN" : "Default" }, "boreDiameter" : 1.375 * inch },
                { "beltType" : BeltType.RT25, "teeth" : 56, "width" : 1 * inch, "name" : "56t Plastic Pulley (RT25, 1\" Wide, MAXSpline Bore)", "partNumber" : "REV-25-2272", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e1e749203c17965a05557e98", "configuration" : { "List_KOc9hmQqjnTpbV" : "_56t", "List_aNwPoODfdqNaXN" : "Double" }, "boreDiameter" : 1.375 * inch },
                { "beltType" : BeltType.RT25, "teeth" : 64, "width" : 0.5 * inch, "name" : "64t Plastic Pulley (RT25, 1/2\" Wide, MAXSpline Bore)", "partNumber" : "REV-25-2284", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e1e749203c17965a05557e98", "configuration" : { "List_KOc9hmQqjnTpbV" : "_64t", "List_aNwPoODfdqNaXN" : "Default" }, "boreDiameter" : 1.375 * inch },
                { "beltType" : BeltType.RT25, "teeth" : 64, "width" : 1 * inch, "name" : "64t Plastic Pulley (RT25, 1\" Wide, MAXSpline Bore)", "partNumber" : "REV-25-2284", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e1e749203c17965a05557e98", "configuration" : { "List_KOc9hmQqjnTpbV" : "_64t", "List_aNwPoODfdqNaXN" : "Double" }, "boreDiameter" : 1.375 * inch }
            ],
            "550" : [
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 12, "width" : 9 * millimeter, "name" : "12t Aluminum Pulley (GT2 3mm, 9mm Wide, 550 Bore)", "partNumber" : "REV-21-1908", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b1a5445f24f8c62c0f09e66d", "configuration" : { "List_8m6tHp0cNDeSzI" : "Default" } }
            ]
        },
        "AM" : {
            "1/2\" Hex" : [
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 24, "width" : 9 * millimeter, "name" : "24t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "am-4625", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "fbfc78aa11c89b54018c4082", "configuration" : { "List_k3bqnpHu4pTlNv" : "Default" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 24, "width" : 18 * millimeter, "name" : "24t Aluminum Pulley (HTD 5mm, 18mm Wide, 1/2\" Hex Bore)", "partNumber" : "am-4626", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "fbfc78aa11c89b54018c4082", "configuration" : { "List_k3bqnpHu4pTlNv" : "_24t_x_15mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 24, "width" : 15 * millimeter, "name" : "24t Plastic Pulley (HTD 5mm, 15mm Wide, 1/2\" Hex Bore)", "partNumber" : "am-3403 + am-3404", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "6e8ff1d6ea010e9b62e9d882", "configuration" : { "List_yPAFb8HrjPe66e" : "Default", "List_WD2b0FPIlYUDRR" : "Double", "List_w0N6RtDAfDOlDd" : "Default", "Simplified_Model" : "false" }, "boreDiameter" : 0.5774 * inch }
            ],
            "3/8\" Hex" : [
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 14, "width" : 9 * millimeter, "name" : "14t Plastic Pulley (HTD 5mm, 9mm Wide, 3/8\" Hex Bore)", "partNumber" : "am-4960", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "6e8ff1d6ea010e9b62e9d882", "configuration" : { "List_yPAFb8HrjPe66e" : "_14", "List_WD2b0FPIlYUDRR" : "Default", "List_w0N6RtDAfDOlDd" : "Default", "Simplified_Model" : "false" }, "boreDiameter" : 0.433 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 24, "width" : 9 * millimeter, "name" : "24t Plastic Pulley (HTD 5mm, 9mm Wide, 3/8\" Hex Bore)", "partNumber" : "am-3402", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "6e8ff1d6ea010e9b62e9d882", "configuration" : { "List_yPAFb8HrjPe66e" : "Default", "List_WD2b0FPIlYUDRR" : "Default", "List_w0N6RtDAfDOlDd" : "_3_8__Hex", "Simplified_Model" : "false" }, "boreDiameter" : 0.433 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 24, "width" : 15 * millimeter, "name" : "24t Plastic Pulley (HTD 5mm, 15mm Wide, 3/8\" Hex Bore)", "partNumber" : "am-3402 + am-3404", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "6e8ff1d6ea010e9b62e9d882", "configuration" : { "List_yPAFb8HrjPe66e" : "Default", "List_WD2b0FPIlYUDRR" : "Double", "List_w0N6RtDAfDOlDd" : "_3_8__Hex", "Simplified_Model" : "false" }, "boreDiameter" : 0.433 * inch }
            ],
            "1.125\" Round" : [
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 42, "width" : 15 * millimeter, "name" : "42t Plastic Pulley (HTD 5mm, 15mm Wide, 1.125\" Bearing Bore)", "partNumber" : "am-2234a", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "6e8ff1d6ea010e9b62e9d882", "configuration" : { "List_yPAFb8HrjPe66e" : "_42", "List_WD2b0FPIlYUDRR" : "Default", "List_w0N6RtDAfDOlDd" : "Default", "Simplified_Model" : "false" }, "boreDiameter" : 1.125 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 42, "width" : 30 * millimeter, "name" : "42t Plastic Pulley (HTD 5mm, 30mm Wide, 1.125\" Bearing Bore)", "partNumber" : "am-2234a + am-2813", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "6e8ff1d6ea010e9b62e9d882", "configuration" : { "List_yPAFb8HrjPe66e" : "_42", "List_WD2b0FPIlYUDRR" : "Double", "List_w0N6RtDAfDOlDd" : "Default", "Simplified_Model" : "false" }, "boreDiameter" : 1.125 * inch }
            ],
            "0.958\" Round" : [
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 39, "width" : 30 * millimeter, "name" : "39t Plastic Pulley (HTD 5mm, 30mm Wide, 0.958\" Round Bore)", "partNumber" : "am-2361a", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "6e8ff1d6ea010e9b62e9d882", "configuration" : { "List_yPAFb8HrjPe66e" : "_39", "List_WD2b0FPIlYUDRR" : "Default", "List_w0N6RtDAfDOlDd" : "Default", "Simplified_Model" : "false" }, "boreDiameter" : 0.958 * inch }
            ]
        },
        "VEX" : {
            "1/2\" Hex" : [
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 24, "width" : 9 * millimeter, "name" : "24t Aluminum Pulley (GT2 3mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "217-5875", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e0b78b7fc5296db321f026d1", "configuration" : { "List_daRtfpZmE8sXxM" : "Default" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 36, "width" : 9 * millimeter, "name" : "36t Aluminum Pulley (GT2 3mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "217-5873", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e0b78b7fc5296db321f026d1", "configuration" : { "List_daRtfpZmE8sXxM" : "_36t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 48, "width" : 9 * millimeter, "name" : "48t Aluminum Pulley (GT2 3mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "217-5874", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e0b78b7fc5296db321f026d1", "configuration" : { "List_daRtfpZmE8sXxM" : "_48t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 60, "width" : 9 * millimeter, "name" : "60t Aluminum Pulley (GT2 3mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "217-5876", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "e0b78b7fc5296db321f026d1", "configuration" : { "List_daRtfpZmE8sXxM" : "_60t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 18, "width" : 9 * millimeter, "name" : "18t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "217-3225", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "ff7d6124b9b99e0006351a34", "configuration" : { "List_33MiSyP9U9NQ1D" : "Default" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 18, "width" : 15 * millimeter, "name" : "18t Aluminum Pulley (HTD 5mm, 15mm Wide, 1/2\" Hex Bore)", "partNumber" : "217-4100", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "ff7d6124b9b99e0006351a34", "configuration" : { "List_33MiSyP9U9NQ1D" : "Copy_of_18t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 24, "width" : 9 * millimeter, "name" : "24t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "217-3227", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "ff7d6124b9b99e0006351a34", "configuration" : { "List_33MiSyP9U9NQ1D" : "_24t_Regular" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 24, "width" : 15 * millimeter, "name" : "24t Aluminum Pulley (HTD 5mm, 15mm Wide, 1/2\" Hex Bore)", "partNumber" : "217-4101", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "ff7d6124b9b99e0006351a34", "configuration" : { "List_33MiSyP9U9NQ1D" : "Copy_of_24t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 30, "width" : 9 * millimeter, "name" : "30t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "217-3229", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "ff7d6124b9b99e0006351a34", "configuration" : { "List_33MiSyP9U9NQ1D" : "_30t_Regular" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 30, "width" : 15 * millimeter, "name" : "30t Aluminum Pulley (HTD 5mm, 15mm Wide, 1/2\" Hex Bore)", "partNumber" : "217-4102", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "ff7d6124b9b99e0006351a34", "configuration" : { "List_33MiSyP9U9NQ1D" : "Copy_of_30t_x_9mm" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 36, "width" : 9 * millimeter, "name" : "36t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "217-3231", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "ff7d6124b9b99e0006351a34", "configuration" : { "List_33MiSyP9U9NQ1D" : "_36t_Regular" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 36, "width" : 15 * millimeter, "name" : "36t Aluminum Pulley (HTD 5mm, 15mm Wide, 1/2\" Hex Bore)", "partNumber" : "217-4103", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "ff7d6124b9b99e0006351a34", "configuration" : { "List_33MiSyP9U9NQ1D" : "Copy_of_36t_x_9mm" }, "boreDiameter" : 0.5774 * inch }
            ],
            "Falcon Spline" : [
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 16, "width" : 9 * millimeter, "name" : "16t Aluminum Pulley (GT2 3mm, 9mm Wide, Falcon Spline Bore)", "partNumber" : "217-6924", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "3fbf8e485e773b8bfa867e15", "configuration" : { "List_Wu9Tk6ElZc2oKP" : "_16t_x_9mm_GT2_3mm__VEX_" }, "boreDiameter" : 8 * millimeter }
            ],
            "1.125\" Round" : [
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 42, "width" : 9 * millimeter, "name" : "42t Plastic VersaPulley (HTD 5mm, 9mm Wide, 1.125\" Round Bore)", "partNumber" : "217-3199", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "079fa13fd1a76926aa28d7de", "configuration" : { "List_37juCscApTb4b7" : "Default", "Simplified_Model" : "false" }, "boreDiameter" : 1.125 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 42, "width" : 18 * millimeter, "name" : "42t Plastic VersaPulley (HTD 5mm, 18mm Wide, 1.125\" Round Bore)", "partNumber" : "217-3199", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "079fa13fd1a76926aa28d7de", "configuration" : { "List_37juCscApTb4b7" : "_42t_x_18mm", "Simplified_Model" : "false" }, "boreDiameter" : 1.125 * inch }
            ],
            "550" : [
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 12, "width" : 9 * millimeter, "name" : "12t Aluminum Pulley (GT2 3mm, 9mm Wide, 550 Bore)", "partNumber" : "217-5870", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "b1a5445f24f8c62c0f09e66d", "configuration" : { "List_8m6tHp0cNDeSzI" : "_12t_x_9mm__GT2_3mm___VEX_" } }
            ],
            "775" : [
                { "beltType" : BeltType._3_MM_GT2, "teeth" : 12, "width" : 9 * millimeter, "name" : "12t Aluminum Pulley (GT2 3mm, 9mm Wide, 775 Bore)", "partNumber" : "217-5871", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "efaae733303a688ff125f0b2", "configuration" : { "List_n2M4pCTF3zUFMO" : "_12t_x_9mm__VEX_" } }
            ]
        },
        "LAI" : {
            "1/2\" Hex" : [
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 18, "width" : 9 * millimeter, "name" : "18t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "240107", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "99f9d1ab489e9b9e9e66ea92", "configuration" : { "List_33MiSyP9U9NQ1D" : "Default" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 24, "width" : 9 * millimeter, "name" : "24t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "240108", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "99f9d1ab489e9b9e9e66ea92", "configuration" : { "List_33MiSyP9U9NQ1D" : "_24t_Regular" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 30, "width" : 9 * millimeter, "name" : "30t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "240109", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "99f9d1ab489e9b9e9e66ea92", "configuration" : { "List_33MiSyP9U9NQ1D" : "_30t_Regular" }, "boreDiameter" : 0.5774 * inch },
                { "beltType" : BeltType._5_MM_HTD, "teeth" : 36, "width" : 9 * millimeter, "name" : "36t Aluminum Pulley (HTD 5mm, 9mm Wide, 1/2\" Hex Bore)", "partNumber" : "240110", "documentId" : "7348c911d6b36d3b222c1dfe", "elementId" : "99f9d1ab489e9b9e9e66ea92", "configuration" : { "List_33MiSyP9U9NQ1D" : "_36t_Regular" }, "boreDiameter" : 0.5774 * inch }
            ]
        }
    };
