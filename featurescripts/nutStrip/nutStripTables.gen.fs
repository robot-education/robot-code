FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/* Generated from nutStripTables.py by `fs gen` -- DO NOT EDIT */

export const frcNutStripTable = {
        "name" : "vendor",
        "displayName" : "Vendor",
        "entries" : {
            "WCP" : {
                "name" : "size",
                "displayName" : "Size",
                "entries" : {
                    "1/2 in." : {
                        "name" : "thread",
                        "displayName" : "Thread",
                        "entries" : {
                            "#10-32" : { "height" : 0.5 * inch, "majorDiameter" : 0.19 * inch, "pitch" : "32 tpi", "size" : "#10", "sizeName" : "1/2 in.", "spacing" : 0.5 * inch, "tapDrillDiameter" : 0.159 * inch, "threadName" : "#10-32", "vendor" : "WCP", "width" : 0.5 * inch, "xHoleStart" : 0.25 * inch, "yHoleStart" : 0.5 * inch },
                            "#8-32" : { "height" : 0.5 * inch, "majorDiameter" : 0.164 * inch, "pitch" : "32 tpi", "size" : "#8", "sizeName" : "1/2 in.", "spacing" : 0.5 * inch, "tapDrillDiameter" : 0.136 * inch, "threadName" : "#8-32", "vendor" : "WCP", "width" : 0.5 * inch, "xHoleStart" : 0.25 * inch, "yHoleStart" : 0.5 * inch },
                        },
                    },
                    "3/8 in." : {
                        "name" : "thread",
                        "displayName" : "Thread",
                        "entries" : {
                            "#10-32" : { "height" : 0.375 * inch, "majorDiameter" : 0.19 * inch, "pitch" : "32 tpi", "size" : "#10", "sizeName" : "3/8 in.", "spacing" : 0.5 * inch, "tapDrillDiameter" : 0.159 * inch, "threadName" : "#10-32", "vendor" : "WCP", "width" : 0.375 * inch, "xHoleStart" : 0.25 * inch, "yHoleStart" : 0.5 * inch },
                        },
                    },
                },
            },
            "REV" : {
                "name" : "size",
                "displayName" : "Size",
                "entries" : {
                    "3/8 in." : {
                        "name" : "thread",
                        "displayName" : "Thread",
                        "entries" : {
                            "#10-32" : { "appearance" : color(0.3, 0.3, 0.3), "height" : 0.375 * inch, "majorDiameter" : 0.19 * inch, "pitch" : "32 tpi", "size" : "#10", "sizeName" : "3/8 in.", "spacing" : 0.5 * inch, "tapDrillDiameter" : 0.159 * inch, "threadName" : "#10-32", "vendor" : "REV", "width" : 0.375 * inch, "xHoleStart" : 0.24 * inch, "yHoleStart" : 0.24 * inch },
                        },
                    },
                },
            },
        },
    };

export const ftcNutStripTable = {
        "name" : "vendor",
        "displayName" : "Vendor",
        "entries" : {
            "REV" : {
                "name" : "size",
                "displayName" : "Size",
                "entries" : {
                    "8 mm" : {
                        "name" : "thread",
                        "displayName" : "Thread",
                        "entries" : {
                            "M3 x 0.5" : { "height" : 8 * millimeter, "majorDiameter" : 3 * millimeter, "pitch" : "0.5 mm", "size" : "M3", "sizeName" : "8 mm", "spacing" : 16 * millimeter, "tapDrillDiameter" : 2.5 * millimeter, "threadName" : "M3 x 0.5", "vendor" : "REV", "width" : 8 * millimeter, "xHoleStart" : 8 * millimeter, "yHoleStart" : 16 * millimeter },
                        },
                    },
                },
            },
        },
    };
