FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "shaft/robotShaftCommon.fs", version : ""); // shaft/robotShaftCommon.fs
import(path : "core/robotProperties.fs", version : ""); // core/robotProperties.fs

/* Generated from robotShaftTables.py by `fs gen` -- DO NOT EDIT */

export const tappedHoleTable = {
        "name" : "size",
        "displayName" : "Size",
        "default" : "#10",
        "entries" : {
            "#8" : {
                "name" : "pitch",
                "displayName" : "Threads/inch",
                "entries" : {
                    "32 tpi (UNC)" : { "majorDiameter" : 0.164 * inch, "tapDrillDiameter" : "0.1360 in" },
                    "36 tpi (UNF)" : { "majorDiameter" : 0.164 * inch, "tapDrillDiameter" : "0.1360 in" },
                },
            },
            "#10" : {
                "name" : "pitch",
                "displayName" : "Threads/inch",
                "default" : "32 tpi (UNF)",
                "entries" : {
                    "24 tpi (UNC)" : { "majorDiameter" : 0.19 * inch, "tapDrillDiameter" : "0.1495 in" },
                    "32 tpi (UNF)" : { "majorDiameter" : 0.19 * inch, "tapDrillDiameter" : "0.1590 in" },
                },
            },
            "1/4" : {
                "name" : "pitch",
                "displayName" : "Threads/inch",
                "entries" : {
                    "20 tpi (UNC)" : { "majorDiameter" : 0.25 * inch, "tapDrillDiameter" : "0.2010 in" },
                    "28 tpi (UNF)" : { "majorDiameter" : 0.25 * inch, "tapDrillDiameter" : "0.2130 in" },
                    "32 tpi (UNEF)" : { "majorDiameter" : 0.25 * inch, "tapDrillDiameter" : "0.2187 in" },
                },
            },
            "5/16" : {
                "name" : "pitch",
                "displayName" : "Threads/inch",
                "entries" : {
                    "18 tpi (UNC)" : { "majorDiameter" : 0.3125 * inch, "tapDrillDiameter" : "0.2570 in" },
                    "24 tpi (UNF)" : { "majorDiameter" : 0.3125 * inch, "tapDrillDiameter" : "0.2720 in" },
                    "32 tpi (UNEF)" : { "majorDiameter" : 0.3125 * inch, "tapDrillDiameter" : "0.2812 in" },
                },
            },
        },
    };

export const clearanceHoleTable = {
        "name" : "size",
        "displayName" : "Size",
        "default" : "#10",
        "entries" : {
            "#8" : {
                "name" : "fit",
                "displayName" : "Fastener fit",
                "entries" : {
                    "Close" : { "holeDiameter" : "0.1695 in" },
                    "Free" : { "holeDiameter" : "0.177 in" },
                },
            },
            "#10" : {
                "name" : "fit",
                "displayName" : "Fastener fit",
                "entries" : {
                    "Close" : { "holeDiameter" : "0.196 in" },
                    "Free" : { "holeDiameter" : "0.201 in" },
                },
            },
            "1/4" : {
                "name" : "fit",
                "displayName" : "Fastener fit",
                "entries" : {
                    "Close" : { "holeDiameter" : "0.257 in" },
                    "Free" : { "holeDiameter" : "0.266 in" },
                },
            },
        },
    };

export const frcShaftTable = {
        "name" : "vendor",
        "displayName" : "Vendor",
        "entries" : {
            "WCP" : {
                "name" : "shaft",
                "displayName" : "Shaft",
                "entries" : {
                    "1/2 in. Rounded Hex" : { "appearance" : BLACK, "hexSize" : HexSize._1_2_IN, "hexType" : HexType.ROUNDED_HEX, "material" : ALUMINUM, "partName" : "Rounded Hex Shaft (WCP 1/2 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "WCP-0914", "url" : "https://wcproducts.com/products/wcp-0914" }], "vendor" : "WCP" },
                    "3/8 in. Rounded Hex" : { "appearance" : BLACK, "hexSize" : HexSize._3_8_IN, "hexType" : HexType.ROUNDED_HEX, "material" : ALUMINUM, "partName" : "Rounded Hex Shaft (WCP 3/8 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "WCP-0911", "url" : "https://wcproducts.com/products/wcp-0911" }], "vendor" : "WCP" },
                    "1/2 in. Hex" : { "appearance" : BLACK, "hexSize" : HexSize._1_2_IN, "hexType" : HexType.STOCK, "material" : ALUMINUM, "partName" : "Hex Shaft (WCP 1/2 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "WCP-0915", "url" : "https://wcproducts.com/products/wcp-0915" }], "vendor" : "WCP" },
                    "3/8 in. Hex" : { "appearance" : BLACK, "hexSize" : HexSize._3_8_IN, "hexType" : HexType.STOCK, "material" : ALUMINUM, "partName" : "Hex Shaft (WCP 3/8 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "WCP-0912", "url" : "https://wcproducts.com/products/wcp-0912" }], "vendor" : "WCP" },
                    "1/2 in. Hex Lite" : { "appearance" : BLACK, "hexSize" : HexSize._1_2_IN, "hexType" : HexType.HEX_LITE, "material" : ALUMINUM, "partName" : "Hex Lite Shaft (WCP 1/2 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "WCP-0917", "url" : "https://wcproducts.com/products/wcp-0917" }], "vendor" : "WCP" },
                    "3/8 in. Hex Lite" : { "appearance" : BLACK, "hexSize" : HexSize._3_8_IN, "hexType" : HexType.HEX_LITE, "material" : ALUMINUM, "partName" : "Hex Lite Shaft (WCP 3/8 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "WCP-1418", "url" : "https://wcproducts.com/products/wcp-1418" }], "vendor" : "WCP" },
                    "SplineXL" : { "appearance" : BLACK, "material" : ALUMINUM, "partName" : "SplineXL Shaft (WCP)", "shaftType" : ShaftType.SPLINE, "splineType" : SplineType.SPLINE_XL, "stock" : [{ "length" : 47 * inch, "partNumber" : "WCP-0918", "url" : "https://wcproducts.com/products/wcp-0918" }], "vendor" : "WCP" },
                },
            },
            "REV" : {
                "name" : "shaft",
                "displayName" : "Shaft",
                "entries" : {
                    "1/2 in. Rounded Hex" : { "appearance" : BLACK, "hexSize" : HexSize._1_2_IN, "hexType" : HexType.ROUNDED_HEX, "material" : ALUMINUM, "partName" : "Rounded Hex Shaft (REV 1/2 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "REV-21-1135", "url" : "https://www.revrobotics.com/search.php?search_query=REV-21-1135&section=product" }], "vendor" : "REV" },
                    "1/2 in. UltraHex" : { "appearance" : WHITE, "hexSize" : HexSize._1_2_IN, "hexType" : HexType.ULTRA_HEX, "material" : ALUMINUM, "partName" : "UltraHex Shaft (REV 1/2 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 72 * inch, "partNumber" : "REV-41-3205", "url" : "https://www.revrobotics.com/rev-41-3205/" }], "vendor" : "REV" },
                    "MAXSpline" : { "appearance" : WHITE, "material" : ALUMINUM, "partName" : "MAXSpline Shaft (REV)", "shaftType" : ShaftType.SPLINE, "splineType" : SplineType.MAX_SPLINE, "stock" : [{ "length" : 47 * inch, "partNumber" : "REV-21-2520", "url" : "https://www.revrobotics.com/search.php?search_query=REV-21-2520&section=product" }], "vendor" : "REV" },
                },
            },
            "AndyMark" : {
                "name" : "shaft",
                "displayName" : "Shaft",
                "entries" : {
                    "1/2 in. Churro" : { "appearance" : WHITE, "hexSize" : HexSize._1_2_IN, "hexType" : HexType.CHURRO, "material" : ALUMINUM, "partName" : "Churro Shaft (AndyMark 1/2 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 2.48 * inch, "partNumber" : "am-3399", "url" : "https://andymark.com/products/1-2-in-churro-different-lengths" }, { "length" : 3.375 * inch, "partNumber" : "am-2569", "url" : "https://andymark.com/products/1-2-in-churro-different-lengths" }, { "length" : 3.875 * inch, "partNumber" : "am-3087", "url" : "https://andymark.com/products/1-2-in-churro-different-lengths" }, { "length" : 6.25 * inch, "partNumber" : "am-5724", "url" : "https://andymark.com/products/1-2-in-churro-different-lengths" }, { "length" : 11.25 * inch, "partNumber" : "am-3398", "url" : "https://andymark.com/products/1-2-in-churro-different-lengths" }, { "length" : 12 * inch, "partNumber" : "am-3101-1", "url" : "https://andymark.com/products/1-2-in-churro-different-lengths" }, { "length" : 17.313 * inch, "partNumber" : "am-5218", "url" : "https://andymark.com/products/1-2-in-churro-different-lengths" }, { "length" : 17.8 * inch, "partNumber" : "am-3101-1780", "url" : "https://andymark.com/products/1-2-in-churro-different-lengths" }, { "length" : 24 * inch, "partNumber" : "am-3101-2", "url" : "https://andymark.com/products/1-2-in-churro-different-lengths" }, { "length" : 36 * inch, "partNumber" : "am-3101-3", "url" : "https://andymark.com/products/1-2-in-churro-different-lengths" }, { "length" : 47 * inch, "partNumber" : "am-3101-4700", "url" : "https://andymark.com/products/1-2-in-churro-different-lengths" }], "vendor" : "AndyMark" },
                    "1/2 in. Hex (7075)" : { "appearance" : WHITE, "hexSize" : HexSize._1_2_IN, "hexType" : HexType.STOCK, "material" : ALUMINUM_7075, "partName" : "Hex Shaft (AndyMark 1/2 in., 7075)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 12 * inch, "partNumber" : "am-2291-1", "url" : "https://andymark.com/products/0-5-in-7075-aluminum-hex-shaft-stock" }, { "length" : 47 * inch, "partNumber" : "am-2291-4700", "url" : "https://andymark.com/products/0-5-in-7075-aluminum-hex-shaft-stock" }], "vendor" : "AndyMark" },
                    "3/8 in. Churro Lite" : { "appearance" : WHITE, "hexSize" : HexSize._3_8_IN, "hexType" : HexType.CHURRO, "material" : ALUMINUM, "partName" : "Churro Lite Shaft (AndyMark 3/8 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 10.5 * inch, "partNumber" : "am-5867", "url" : "https://andymark.com/products/3-8-in-churro-lite-different-lengths" }, { "length" : 36 * inch, "partNumber" : "am-3666-3", "url" : "https://andymark.com/products/3-8-in-churro-lite-different-lengths" }, { "length" : 47 * inch, "partNumber" : "am-3666-4700", "url" : "https://andymark.com/products/3-8-in-churro-lite-different-lengths" }], "vendor" : "AndyMark" },
                    "3/8 in. Hex (steel)" : { "appearance" : DARK_GRAY, "hexSize" : HexSize._3_8_IN, "hexType" : HexType.STOCK, "material" : STEEL, "partName" : "Hex Shaft (AndyMark 3/8 in., steel)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 1.85 * inch, "partNumber" : "am-2356", "url" : "https://andymark.com/products/3-8-in-steel-hex-shaft-stock" }, { "length" : 12 * inch, "partNumber" : "am-2356-1", "url" : "https://andymark.com/products/3-8-in-steel-hex-shaft-stock" }, { "length" : 36 * inch, "partNumber" : "am-2356-3", "url" : "https://andymark.com/products/3-8-in-steel-hex-shaft-stock" }, { "length" : 47 * inch, "partNumber" : "am-2356-4700", "url" : "https://andymark.com/products/3-8-in-steel-hex-shaft-stock" }], "vendor" : "AndyMark" },
                },
            },
            "Swyft" : {
                "name" : "shaft",
                "displayName" : "Shaft",
                "entries" : {
                    "1/2 in. Rounded Hex (7075)" : { "appearance" : BLACK, "hexSize" : HexSize._1_2_IN, "hexType" : HexType.ROUNDED_HEX, "material" : ALUMINUM_7075, "partName" : "Rounded Hex Shaft (Swyft 1/2 in., 7075)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "SR-AXLE-HEX-0.5in-36in-AL7075", "url" : "https://swyftrobotics.com/structure/swyft-axles" }], "vendor" : "Swyft" },
                    "1/2 in. Rounded Hex (6061)" : { "appearance" : BLACK, "hexSize" : HexSize._1_2_IN, "hexType" : HexType.ROUNDED_HEX, "material" : ALUMINUM, "partName" : "Rounded Hex Shaft (Swyft 1/2 in., 6061)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "SR-AXLE-HEXtoSPLINE-0.5in-36in-AL6061", "url" : "https://swyftrobotics.com/structure/swyft-axles" }], "vendor" : "Swyft" },
                },
            },
            "VEX" : {
                "name" : "shaft",
                "displayName" : "Shaft",
                "entries" : {
                    "1/2 in. ThunderHex" : { "appearance" : BLACK, "hexSize" : HexSize._1_2_IN, "hexType" : HexType.ROUNDED_HEX, "material" : ALUMINUM, "partName" : "ThunderHex Shaft (VEX 1/2 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "217-8631", "url" : "https://www.vexrobotics.com/217-8631.html" }], "vendor" : "VEX" },
                    "3/8 in. ThunderHex" : { "appearance" : BLACK, "hexSize" : HexSize._3_8_IN, "hexType" : HexType.ROUNDED_HEX, "material" : ALUMINUM, "partName" : "ThunderHex Shaft (VEX 3/8 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "217-5837", "url" : "https://www.vexrobotics.com/217-5837.html" }], "vendor" : "VEX" },
                    "1/2 in. Hex" : { "appearance" : WHITE, "hexSize" : HexSize._1_2_IN, "hexType" : HexType.STOCK, "material" : ALUMINUM, "partName" : "Hex Shaft (VEX 1/2 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "217-2753", "url" : "https://www.vexrobotics.com/217-2753.html" }], "vendor" : "VEX" },
                    "3/8 in. Hex" : { "appearance" : WHITE, "hexSize" : HexSize._3_8_IN, "hexType" : HexType.STOCK, "material" : ALUMINUM, "partName" : "Hex Shaft (VEX 3/8 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "217-2754", "url" : "https://www.vexrobotics.com/217-2754.html" }], "vendor" : "VEX" },
                },
            },
            "ThriftyBot" : {
                "name" : "shaft",
                "displayName" : "Shaft",
                "entries" : {
                    "1/2 in. Rounded Hex (7075)" : { "appearance" : BLACK, "hexSize" : HexSize._1_2_IN, "hexType" : HexType.ROUNDED_HEX, "material" : ALUMINUM_7075, "partName" : "Rounded Hex Shaft (ThriftyBot 1/2 in., 7075)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "TTB-0069", "url" : "https://www.thethriftybot.com/products/copy-of-qty-1-36-inch-long-1-2-rounded-hex-shaft-7075-aluminum" }], "vendor" : "ThriftyBot" },
                    "1/2 in. Rounded Hex (6061)" : { "appearance" : BLACK, "hexSize" : HexSize._1_2_IN, "hexType" : HexType.ROUNDED_HEX, "material" : ALUMINUM, "partName" : "Rounded Hex Shaft (ThriftyBot 1/2 in., 6061)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "TTB-0068", "url" : "https://www.thethriftybot.com/products/qty-1-36-inch-long-1-2-rounded-hex-shaft-6061-aluminum" }], "vendor" : "ThriftyBot" },
                    "3/8 in. Rounded Hex" : { "appearance" : BLACK, "hexSize" : HexSize._3_8_IN, "hexType" : HexType.ROUNDED_HEX, "material" : ALUMINUM, "partName" : "Rounded Hex Shaft (ThriftyBot 3/8 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 36 * inch, "partNumber" : "TTB-0265", "url" : "https://www.thethriftybot.com/products/3-8-rounded-hex-shaft-stock-36-long" }], "vendor" : "ThriftyBot" },
                },
            },
        },
    };

export const ftcShaftTable = {
        "name" : "vendor",
        "displayName" : "Vendor",
        "entries" : {
            "goBILDA" : {
                "name" : "shaft",
                "displayName" : "Shaft",
                "entries" : {
                    "8mm REX (stainless steel)" : { "appearance" : WHITE, "hexSize" : HexSize._7_MM, "hexType" : HexType.ROUNDED_HEX, "material" : STAINLESS_STEEL, "partName" : "8mm REX Shaft (goBILDA, stainless steel)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 24 * millimeter, "partNumber" : "2106-4008-0240", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-24mm-length/" }, { "length" : 32 * millimeter, "partNumber" : "2106-4008-0320", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-32mm-length/" }, { "length" : 40 * millimeter, "partNumber" : "2106-4008-0400", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-40mm-length/" }, { "length" : 43 * millimeter, "partNumber" : "2106-4008-0430", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-43mm-length/" }, { "length" : 48 * millimeter, "partNumber" : "2106-4008-0480", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-48mm-length/" }, { "length" : 52 * millimeter, "partNumber" : "2106-4008-0520", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-52mm-length/" }, { "length" : 54 * millimeter, "partNumber" : "2106-4008-0540", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-54mm-length/" }, { "length" : 56 * millimeter, "partNumber" : "2106-4008-0560", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-56mm-length/" }, { "length" : 64 * millimeter, "partNumber" : "2106-4008-0640", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-64mm-length/" }, { "length" : 72 * millimeter, "partNumber" : "2106-4008-0720", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-72mm-length/" }, { "length" : 80 * millimeter, "partNumber" : "2106-4008-0800", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-80mm-length/" }, { "length" : 88 * millimeter, "partNumber" : "2106-4008-0880", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-88mm-length/" }, { "length" : 96 * millimeter, "partNumber" : "2106-4008-0960", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-96mm-length/" }, { "length" : 104 * millimeter, "partNumber" : "2106-4008-1040", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-104mm-length/" }, { "length" : 112 * millimeter, "partNumber" : "2106-4008-1120", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-112mm-length/" }, { "length" : 120 * millimeter, "partNumber" : "2106-4008-1200", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-120mm-length/" }, { "length" : 144 * millimeter, "partNumber" : "2106-4008-1440", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-144mm-length/" }, { "length" : 168 * millimeter, "partNumber" : "2106-4008-1680", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-168mm-length/" }, { "length" : 192 * millimeter, "partNumber" : "2106-4008-1920", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-192mm-length/" }, { "length" : 216 * millimeter, "partNumber" : "2106-4008-2160", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-216mm-length/" }, { "length" : 240 * millimeter, "partNumber" : "2106-4008-2400", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-240mm-length/" }, { "length" : 264 * millimeter, "partNumber" : "2106-4008-2640", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-264mm-length/" }, { "length" : 288 * millimeter, "partNumber" : "2106-4008-2880", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-288mm-length/" }, { "length" : 312 * millimeter, "partNumber" : "2106-4008-3120", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-312mm-length/" }, { "length" : 336 * millimeter, "partNumber" : "2106-4008-3360", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-336mm-length/" }, { "length" : 384 * millimeter, "partNumber" : "2106-4008-3840", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-384mm-length/" }, { "length" : 432 * millimeter, "partNumber" : "2106-4008-4320", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-432mm-length/" }, { "length" : 624 * millimeter, "partNumber" : "2106-4008-6240", "url" : "https://www.gobilda.com/8mm-rex-shaft-with-e-clip-stainless-steel-624mm-length/" }], "vendor" : "goBILDA" },
                    "12mm REX (stainless steel)" : { "appearance" : WHITE, "hexSize" : HexSize._11_MM, "hexType" : HexType.ROUNDED_HEX, "material" : STAINLESS_STEEL, "partName" : "12mm REX Shaft (goBILDA, stainless steel)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 32 * millimeter, "partNumber" : "2109-4012-0320", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-32mm-length/" }, { "length" : 40 * millimeter, "partNumber" : "2109-4012-0400", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-40mm-length/" }, { "length" : 48 * millimeter, "partNumber" : "2109-4012-0480", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-48mm-length/" }, { "length" : 56 * millimeter, "partNumber" : "2109-4012-0560", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-56mm-length/" }, { "length" : 64 * millimeter, "partNumber" : "2109-4012-0640", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-64mm-length/" }, { "length" : 72 * millimeter, "partNumber" : "2109-4012-0720", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-72mm-length/" }, { "length" : 80 * millimeter, "partNumber" : "2109-4012-0800", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-80mm-length/" }, { "length" : 88 * millimeter, "partNumber" : "2109-4012-0880", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-88mm-length/" }, { "length" : 96 * millimeter, "partNumber" : "2109-4012-0960", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-96mm-length/" }, { "length" : 104 * millimeter, "partNumber" : "2109-4012-1040", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-104mm-length/" }, { "length" : 112 * millimeter, "partNumber" : "2109-4012-1120", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-112mm-length/" }, { "length" : 120 * millimeter, "partNumber" : "2109-4012-1200", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-120mm-length/" }, { "length" : 144 * millimeter, "partNumber" : "2109-4012-1440", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-144mm-length/" }, { "length" : 168 * millimeter, "partNumber" : "2109-4012-1680", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-168mm-length/" }, { "length" : 192 * millimeter, "partNumber" : "2109-4012-1920", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-192mm-length/" }, { "length" : 216 * millimeter, "partNumber" : "2109-4012-2160", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-216mm-length/" }, { "length" : 240 * millimeter, "partNumber" : "2109-4012-2400", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-240mm-length/" }, { "length" : 264 * millimeter, "partNumber" : "2109-4012-2640", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-264mm-length/" }, { "length" : 288 * millimeter, "partNumber" : "2109-4012-2880", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-288mm-length/" }, { "length" : 312 * millimeter, "partNumber" : "2109-4012-3120", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-312mm-length/" }, { "length" : 336 * millimeter, "partNumber" : "2109-4012-3360", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-336mm-length/" }, { "length" : 384 * millimeter, "partNumber" : "2109-4012-3840", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-384mm-length/" }, { "length" : 432 * millimeter, "partNumber" : "2109-4012-4320", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-432mm-length/" }, { "length" : 528 * millimeter, "partNumber" : "2109-4012-5280", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-528mm-length/" }, { "length" : 624 * millimeter, "partNumber" : "2109-4012-6240", "url" : "https://www.gobilda.com/12mm-rex-shaft-with-e-clip-stainless-steel-624mm-length/" }], "vendor" : "goBILDA" },
                    "12mm REX (aluminum)" : { "appearance" : WHITE, "hexSize" : HexSize._11_MM, "hexType" : HexType.ROUNDED_HEX, "material" : ALUMINUM, "partName" : "12mm REX Shaft (goBILDA, aluminum)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 43 * millimeter, "partNumber" : "2104-0012-0043", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-43mm-length/" }, { "length" : 48 * millimeter, "partNumber" : "2104-0012-0048", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-48mm-length/" }, { "length" : 56 * millimeter, "partNumber" : "2104-0012-0056", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-56mm-length/" }, { "length" : 64 * millimeter, "partNumber" : "2104-0012-0064", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-64mm-length/" }, { "length" : 72 * millimeter, "partNumber" : "2104-0012-0072", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-72mm-length/" }, { "length" : 80 * millimeter, "partNumber" : "2104-0012-0080", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-80mm-length/" }, { "length" : 88 * millimeter, "partNumber" : "2104-0012-0088", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-88mm-length/" }, { "length" : 96 * millimeter, "partNumber" : "2104-0012-0096", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-96mm-length/" }, { "length" : 104 * millimeter, "partNumber" : "2104-0012-0104", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-104mm-length/" }, { "length" : 112 * millimeter, "partNumber" : "2104-0012-0112", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-112mm-length/" }, { "length" : 120 * millimeter, "partNumber" : "2104-0012-0120", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-120mm-length/" }, { "length" : 144 * millimeter, "partNumber" : "2104-0012-0144", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-144mm-length/" }, { "length" : 192 * millimeter, "partNumber" : "2104-0012-0192", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-192mm-length/" }, { "length" : 240 * millimeter, "partNumber" : "2104-0012-0240", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-240mm-length/" }, { "length" : 288 * millimeter, "partNumber" : "2104-0012-0288", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-288mm-length/" }, { "length" : 336 * millimeter, "partNumber" : "2104-0012-0336", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-336mm-length/" }, { "length" : 384 * millimeter, "partNumber" : "2104-0012-0384", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-384mm-length/" }, { "length" : 432 * millimeter, "partNumber" : "2104-0012-0432", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-432mm-length/" }, { "length" : 528 * millimeter, "partNumber" : "2104-0012-0528", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-528mm-length/" }, { "length" : 624 * millimeter, "partNumber" : "2104-0012-0624", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-624mm-length/" }, { "length" : 1200 * millimeter, "partNumber" : "2104-0012-1200", "url" : "https://www.gobilda.com/12mm-rex-shaft-aluminum-1200mm-length/" }], "vendor" : "goBILDA" },
                },
            },
            "AndyMark" : {
                "name" : "shaft",
                "displayName" : "Shaft",
                "entries" : {
                    "Robits 3/8 in. Hex" : { "appearance" : DARK_GRAY, "hexSize" : HexSize._3_8_IN, "hexType" : HexType.STOCK, "material" : STEEL, "partName" : "Robits Hex Shaft (AndyMark 3/8 in.)", "shaftType" : ShaftType.HEX, "stock" : [{ "length" : 2 * inch, "partNumber" : "am-5003-0200", "url" : "https://andymark.com/products/robits-hex-shafts" }, { "length" : 3 * inch, "partNumber" : "am-5003-0300", "url" : "https://andymark.com/products/robits-hex-shafts" }, { "length" : 4 * inch, "partNumber" : "am-5003-0400", "url" : "https://andymark.com/products/robits-hex-shafts" }, { "length" : 6 * inch, "partNumber" : "am-5003-0600", "url" : "https://andymark.com/products/robits-hex-shafts" }, { "length" : 8 * inch, "partNumber" : "am-5003-0800", "url" : "https://andymark.com/products/robits-hex-shafts" }, { "length" : 10 * inch, "partNumber" : "am-5003-1000", "url" : "https://andymark.com/products/robits-hex-shafts" }, { "length" : 12 * inch, "partNumber" : "am-5003-1200", "url" : "https://andymark.com/products/robits-hex-shafts" }], "vendor" : "AndyMark" },
                },
            },
        },
    };
