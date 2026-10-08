FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/* Generated from bearingHatTables.py by `fs gen` -- DO NOT EDIT */

export const bearingHatTable = {
        "name" : "bore",
        "displayName" : "Bore",
        "default" : "1.125 in",
        "entries" : {
            "0.875 in" : {
                "name" : "width",
                "displayName" : "Width",
                "default" : "2 in",
                "entries" : {
                    "1.25 in" : { "boreDiameter" : 0.875 * inch, "width" : 1.25 * inch },
                    "2 in" : { "boreDiameter" : 0.875 * inch, "width" : 2 * inch },
                },
            },
            "1.125 in" : {
                "name" : "width",
                "displayName" : "Width",
                "default" : "2 in",
                "entries" : {
                    "1.5 in" : { "boreDiameter" : 1.125 * inch, "width" : 1.5 * inch },
                    "2 in" : { "boreDiameter" : 1.125 * inch, "width" : 2 * inch },
                },
            },
        },
    };

export const boreTable = {
        "name" : "bore",
        "displayName" : "Bore",
        "default" : "1.125 in",
        "entries" : {
            "0.875 in" : { "boreDiameter" : 0.875 * inch },
            "1.125 in" : { "boreDiameter" : 1.125 * inch },
        },
    };

export const holeTable = {
        "name" : "holeType",
        "displayName" : "Hole type",
        "entries" : {
            "Clearance" : {
                "name" : "fit",
                "displayName" : "Fastener fit",
                "entries" : {
                    "Close" : { "holeDiameter" : "0.196 in" },
                    "Free" : { "holeDiameter" : "0.201 in" },
                },
            },
            "Tapped" : {
                "name" : "pitch",
                "displayName" : "Threads/inch",
                "default" : "32 tpi (UNF)",
                "entries" : {
                    "24 tpi (UNC)" : {
                        "name" : "fit",
                        "displayName" : "Fastener fit",
                        "entries" : {
                            "Close" : { "holeDiameter" : "0.196 in", "tapDrillDiameter" : "0.1495 in" },
                            "Free" : { "holeDiameter" : "0.201 in", "tapDrillDiameter" : "0.1495 in" },
                        },
                    },
                    "32 tpi (UNF)" : {
                        "name" : "fit",
                        "displayName" : "Fastener fit",
                        "entries" : {
                            "Close" : { "holeDiameter" : "0.196 in", "tapDrillDiameter" : "0.1590 in" },
                            "Free" : { "holeDiameter" : "0.201 in", "tapDrillDiameter" : "0.1590 in" },
                        },
                    },
                },
            },
        },
    };

export const singleClearanceHoleTable = {
        "name" : "fit",
        "displayName" : "Fastener fit",
        "entries" : {
            "Close" : { "holeDiameter" : "0.196 in" },
            "Free" : { "holeDiameter" : "0.201 in" },
        },
    };

export const singleHoleTable = {
        "name" : "holeType",
        "displayName" : "Hole type",
        "entries" : {
            "Clearance" : {
                "name" : "fit",
                "displayName" : "Fastener fit",
                "entries" : {
                    "Close" : { "holeDiameter" : "0.196 in" },
                    "Free" : { "holeDiameter" : "0.201 in" },
                },
            },
            "Tapped" : {
                "name" : "pitch",
                "displayName" : "Threads/inch",
                "default" : "32 tpi (UNF)",
                "entries" : {
                    "24 tpi (UNC)" : { "holeDiameter" : "0.1495 in" },
                    "32 tpi (UNF)" : { "holeDiameter" : "0.1590 in" },
                },
            },
        },
    };
