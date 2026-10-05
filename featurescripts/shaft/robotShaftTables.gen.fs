FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

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
