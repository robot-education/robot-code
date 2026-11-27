FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "bearings/bearingTables.gen.fs", version : ""); // bearings/bearingTables.gen.fs
import(path : "bushings/bushingTables.gen.fs", version : ""); // bushings/bushingTables.gen.fs

/* Generated from robotBearingTables.py by `fs gen` -- DO NOT EDIT */

export const robotBearingTable = {
        "name" : "vendor",
        "displayName" : "Vendor",
        "entries" : {
            "Generic" : {
                "name" : "type",
                "displayName" : "Type",
                "entries" : {
                    "Flanged" : {
                        "name" : "bore",
                        "displayName" : "Bore",
                        "default" : "Rounded hex",
                        "entries" : {
                            "Hex" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "1/2 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "HEX", "size" : "IN_1_2" }, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "3/8 in" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "default" : "1.125 in OD x 0.313 in",
                                        "entries" : {
                                            "0.875 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Sealed" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "HEX", "size" : "IN_3_8", "dimensions" : "OD_0_875_X_0_281" }, "name" : "3/8 in Hex Flanged Bearing (0.875 in OD x 0.281 in, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "1.125 in OD x 0.313 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "HEX", "size" : "IN_3_8", "dimensions" : "OD_1_125_X_0_313" }, "name" : "3/8 in Hex Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                            "Rounded hex" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "1/2 in" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "default" : "1.125 in OD x 0.313 in",
                                        "entries" : {
                                            "0.875 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUNDED_HEX, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUNDED_HEX", "size" : "IN_1_2", "dimensions" : "OD_0_875_X_0_281" }, "name" : "1/2 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "1 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUNDED_HEX, "dimensions" : BearingDimensions.OD_1_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUNDED_HEX", "size" : "IN_1_2", "dimensions" : "OD_1_X_0_281" }, "name" : "1/2 in Rounded Hex Flanged Bearing (1 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "1.125 in OD x 0.313 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUNDED_HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUNDED_HEX", "size" : "IN_1_2", "dimensions" : "OD_1_125_X_0_313" }, "name" : "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "3/8 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUNDED_HEX, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUNDED_HEX", "size" : "IN_3_8" }, "name" : "3/8 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                },
                            },
                            "Round" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "1/2 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_1_2", "seal" : "SHIELDED" }, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.GENERIC },
                                            "Sealed" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_1_2", "seal" : "SEALED" }, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "3/8 in" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "default" : "1.125 in OD x 0.313 in",
                                        "entries" : {
                                            "0.875 in OD x 0.219 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_219, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "IN_3_8", "dimensions" : "OD_0_875_X_0_219" }, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.219 in, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "0.875 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_3_8", "dimensions" : "OD_0_875_X_0_281", "seal" : "SHIELDED" }, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.GENERIC },
                                                    "Sealed" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_3_8", "dimensions" : "OD_0_875_X_0_281", "seal" : "SEALED" }, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "1 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "IN_3_8", "dimensions" : "OD_1_X_0_281" }, "name" : "3/8 in Round Flanged Bearing (1 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "1.125 in OD x 0.313 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_3_8", "dimensions" : "OD_1_125_X_0_313" }, "name" : "3/8 in Round Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "3/16 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_5_X_0_196, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_3_16" }, "name" : "3/16 in Round Flanged Bearing (0.5 in OD x 0.196 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_16, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "#10 (0.19 in)" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "NO_10" }, "name" : "0.19 in Round Flanged Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.NO_10, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "1/4 in" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "0.5 in OD x 0.188 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_5_X_0_188, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_1_4", "dimensions" : "OD_0_5_X_0_188" }, "name" : "1/4 in Round Flanged Bearing (0.5 in OD x 0.188 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "0.625 in OD x 0.196 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_625_X_0_196, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_1_4", "dimensions" : "OD_0_625_X_0_196" }, "name" : "1/4 in Round Flanged Bearing (0.625 in OD x 0.196 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "5 mm" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "8 mm OD x 2 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_8_X_2, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_8_X_2" }, "name" : "5 mm Round Flanged Bearing (8 mm OD x 2 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "9 mm OD x 2.5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_9_X_2_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_9_X_2_5" }, "name" : "5 mm Round Flanged Bearing (9 mm OD x 2.5 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "9 mm OD x 3 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_9_X_3, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_9_X_3" }, "name" : "5 mm Round Flanged Bearing (9 mm OD x 3 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "10 mm OD x 3 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_10_X_3, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_10_X_3" }, "name" : "5 mm Round Flanged Bearing (10 mm OD x 3 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "10 mm OD x 4 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_10_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_10_X_4" }, "name" : "5 mm Round Flanged Bearing (10 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "11 mm OD x 3 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_11_X_3, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_11_X_3" }, "name" : "5 mm Round Flanged Bearing (11 mm OD x 3 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "11 mm OD x 4 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_11_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "MM_5", "dimensions" : "MM_11_X_4" }, "name" : "5 mm Round Flanged Bearing (11 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "11 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_11_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_11_X_5" }, "name" : "5 mm Round Flanged Bearing (11 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "13 mm OD x 4 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_13_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "MM_5", "dimensions" : "MM_13_X_4" }, "name" : "5 mm Round Flanged Bearing (13 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "14 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_14_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_14_X_5", "seal" : "SHIELDED" }, "name" : "5 mm Round Flanged Bearing (14 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_14_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_14_X_5", "seal" : "OPEN" }, "name" : "5 mm Round Flanged Bearing (14 mm OD x 5 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "16 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_16_X_5", "seal" : "SHIELDED" }, "name" : "5 mm Round Flanged Bearing (16 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_16_X_5", "seal" : "OPEN" }, "name" : "5 mm Round Flanged Bearing (16 mm OD x 5 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "19 mm OD x 6 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_19_X_6", "seal" : "SHIELDED" }, "name" : "5 mm Round Flanged Bearing (19 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_19_X_6", "seal" : "OPEN" }, "name" : "5 mm Round Flanged Bearing (19 mm OD x 6 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "6 mm" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "10 mm OD x 2.5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_10_X_2_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_10_X_2_5" }, "name" : "6 mm Round Flanged Bearing (10 mm OD x 2.5 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_6, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "10 mm OD x 3 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_10_X_3, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_10_X_3" }, "name" : "6 mm Round Flanged Bearing (10 mm OD x 3 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "12 mm OD x 4 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_12_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_12_X_4" }, "name" : "6 mm Round Flanged Bearing (12 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "13 mm OD x 3.5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_13_X_3_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_13_X_3_5" }, "name" : "6 mm Round Flanged Bearing (13 mm OD x 3.5 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_6, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "13 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_13_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_13_X_5" }, "name" : "6 mm Round Flanged Bearing (13 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "15 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_15_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_15_X_5", "seal" : "SHIELDED" }, "name" : "6 mm Round Flanged Bearing (15 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.GENERIC },
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_15_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_15_X_5", "seal" : "OPEN" }, "name" : "6 mm Round Flanged Bearing (15 mm OD x 5 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_6, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "17 mm OD x 6 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_17_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_17_X_6", "seal" : "SHIELDED" }, "name" : "6 mm Round Flanged Bearing (17 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.GENERIC },
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_17_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_17_X_6", "seal" : "OPEN" }, "name" : "6 mm Round Flanged Bearing (17 mm OD x 6 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_6, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "19 mm OD x 6 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_19_X_6", "seal" : "SHIELDED" }, "name" : "6 mm Round Flanged Bearing (19 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.GENERIC },
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_19_X_6", "seal" : "OPEN" }, "name" : "6 mm Round Flanged Bearing (19 mm OD x 6 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_6, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "8 mm" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "0.875 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "OD_0_875_X_0_281" }, "name" : "8 mm Round Flanged Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "14 mm OD x 4 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_14_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_14_X_4" }, "name" : "8 mm Round Flanged Bearing (14 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "14 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_14_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "MM_8", "dimensions" : "MM_14_X_5" }, "name" : "8 mm Round Flanged Bearing (14 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "16 mm OD x 4 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_16_X_4" }, "name" : "8 mm Round Flanged Bearing (16 mm OD x 4 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "16 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "MM_8", "dimensions" : "MM_16_X_5" }, "name" : "8 mm Round Flanged Bearing (16 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "16 mm OD x 6.3 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_6_3, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_16_X_6_3" }, "name" : "8 mm Round Flanged Bearing (16 mm OD x 6.3 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "19 mm OD x 6 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_19_X_6", "seal" : "SHIELDED" }, "name" : "8 mm Round Flanged Bearing (19 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.GENERIC },
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_19_X_6", "seal" : "OPEN" }, "name" : "8 mm Round Flanged Bearing (19 mm OD x 6 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "22 mm OD x 7 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_22_X_7", "seal" : "SHIELDED" }, "name" : "8 mm Round Flanged Bearing (22 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.GENERIC },
                                                    "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_22_X_7", "seal" : "OPEN" }, "name" : "8 mm Round Flanged Bearing (22 mm OD x 7 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "9 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_17_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_9" }, "name" : "9 mm Round Flanged Bearing (17 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_9, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "12 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_24_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_12" }, "name" : "12 mm Round Flanged Bearing (24 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_12, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "15 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_21_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_15" }, "name" : "15 mm Round Flanged Bearing (21 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_15, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "30 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_42_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "size" : "MM_30" }, "name" : "30 mm Round Flanged Bearing (42 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_30, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "35 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_47_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "MM_35", "seal" : "SHIELDED" }, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_35, "vendor" : BearingVendor.GENERIC },
                                            "Sealed" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_47_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "FLANGED", "bore" : "ROUND", "size" : "MM_35", "seal" : "SEALED" }, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.MM_35, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                },
                            },
                        },
                    },
                    "Radial" : {
                        "name" : "bore",
                        "displayName" : "Bore",
                        "entries" : {
                            "Hex" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "1/2 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Sealed" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "HEX", "size" : "IN_1_2" }, "name" : "1/2 in Hex Radial Bearing (1.125 in OD x 0.313 in, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "3/8 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Sealed" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "HEX", "size" : "IN_3_8" }, "name" : "3/8 in Hex Radial Bearing (0.875 in OD x 0.281 in, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                },
                            },
                            "Round" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "1/2 in" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "default" : "1.125 in OD x 0.313 in",
                                        "entries" : {
                                            "0.875 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_1_2", "dimensions" : "OD_0_875_X_0_281" }, "name" : "1/2 in Round Radial Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "1.125 in OD x 0.25 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_25, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "IN_1_2", "dimensions" : "OD_1_125_X_0_25" }, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.25 in, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "1.125 in OD x 0.313 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_1_2", "dimensions" : "OD_1_125_X_0_313", "seal" : "SHIELDED" }, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.GENERIC },
                                                    "Sealed" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_1_2", "dimensions" : "OD_1_125_X_0_313", "seal" : "SEALED" }, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "1.375 in OD x 0.438 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_375_X_0_438, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "IN_1_2", "dimensions" : "OD_1_375_X_0_438" }, "name" : "1/2 in Round Radial Bearing (1.375 in OD x 0.438 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "3/8 in" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "0.875 in OD x 0.219 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_219, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "IN_3_8" }, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.219 in, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "0.875 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_3_8", "dimensions" : "OD_0_875_X_0_281", "seal" : "SHIELDED" }, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.GENERIC },
                                                    "Sealed" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_3_8", "dimensions" : "OD_0_875_X_0_281", "seal" : "SEALED" }, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "1.125 in OD x 0.375 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_375, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_3_8", "dimensions" : "OD_1_125_X_0_375" }, "name" : "3/8 in Round Radial Bearing (1.125 in OD x 0.375 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "3/16 in" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "0.5 in OD x 0.156 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_5_X_0_156, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "IN_3_16" }, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.156 in, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.IN_3_16, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "0.5 in OD x 0.196 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_5_X_0_196, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_3_16" }, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.196 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_16, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "#10 (0.19 in)" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_375_X_0_156_EXTENDED, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "NO_10" }, "name" : "0.19 in Round Radial Bearing (0.375 in OD x 0.156 in (extended inner race))" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.NO_10, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "1/4 in" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "0.375 in OD x 0.125 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_375_X_0_125, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_1_4", "dimensions" : "OD_0_375_X_0_125" }, "name" : "1/4 in Round Radial Bearing (0.375 in OD x 0.125 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "0.5 in OD x 0.188 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_5_X_0_188, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_1_4", "dimensions" : "OD_0_5_X_0_188" }, "name" : "1/4 in Round Radial Bearing (0.5 in OD x 0.188 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "0.625 in OD x 0.196 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_625_X_0_196, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_1_4", "dimensions" : "OD_0_625_X_0_196", "seal" : "SHIELDED" }, "name" : "1/4 in Round Radial Bearing (0.625 in OD x 0.196 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.GENERIC },
                                                    "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_625_X_0_196, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_1_4", "dimensions" : "OD_0_625_X_0_196", "seal" : "OPEN" }, "name" : "1/4 in Round Radial Bearing (0.625 in OD x 0.196 in, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "0.75 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_75_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_1_4", "dimensions" : "OD_0_75_X_0_281" }, "name" : "1/4 in Round Radial Bearing (0.75 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "5/8 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_375_X_0_438, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "IN_5_8" }, "name" : "5/8 in Round Radial Bearing (1.375 in OD x 0.438 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_5_8, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "5 mm" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "8 mm OD x 2.5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_8_X_2_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_8_X_2_5" }, "name" : "5 mm Round Radial Bearing (8 mm OD x 2.5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "9 mm OD x 3 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_9_X_3, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_9_X_3" }, "name" : "5 mm Round Radial Bearing (9 mm OD x 3 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "10 mm OD x 4 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_10_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_10_X_4" }, "name" : "5 mm Round Radial Bearing (10 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "11 mm OD x 4 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_11_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_11_X_4" }, "name" : "5 mm Round Radial Bearing (11 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "13 mm OD x 4 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_13_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "MM_5" }, "name" : "5 mm Round Radial Bearing (13 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "14 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_14_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_14_X_5" }, "name" : "5 mm Round Radial Bearing (14 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "16 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_16_X_5" }, "name" : "5 mm Round Radial Bearing (16 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "19 mm OD x 6 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_19_X_6", "seal" : "SHIELDED" }, "name" : "5 mm Round Radial Bearing (19 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                    "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_19_X_6", "seal" : "OPEN" }, "name" : "5 mm Round Radial Bearing (19 mm OD x 6 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "6 mm" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "12 mm OD x 4 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_12_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_6", "dimensions" : "MM_12_X_4" }, "name" : "6 mm Round Radial Bearing (12 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "17 mm OD x 6 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_17_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_6", "dimensions" : "MM_17_X_6" }, "name" : "6 mm Round Radial Bearing (17 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "7 mm" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "14 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_14_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_7", "dimensions" : "MM_14_X_5" }, "name" : "7 mm Round Radial Bearing (14 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_7, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "19 mm OD x 6 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_7", "dimensions" : "MM_19_X_6" }, "name" : "7 mm Round Radial Bearing (19 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_7, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "22 mm OD x 7 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_7", "dimensions" : "MM_22_X_7", "seal" : "SHIELDED" }, "name" : "7 mm Round Radial Bearing (22 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_7, "vendor" : BearingVendor.GENERIC },
                                                    "Sealed" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_7", "dimensions" : "MM_22_X_7", "seal" : "SEALED" }, "name" : "7 mm Round Radial Bearing (22 mm OD x 7 mm, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.MM_7, "vendor" : BearingVendor.GENERIC },
                                                    "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_7", "dimensions" : "MM_22_X_7", "seal" : "OPEN" }, "name" : "7 mm Round Radial Bearing (22 mm OD x 7 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_7, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "8 mm" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "16 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "MM_8", "dimensions" : "MM_16_X_5" }, "name" : "8 mm Round Radial Bearing (16 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "19 mm OD x 6 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_8" }, "name" : "8 mm Round Radial Bearing (19 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "22 mm OD x 7 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "MM_8", "dimensions" : "MM_22_X_7", "seal" : "SHIELDED" }, "name" : "8 mm Round Radial Bearing (22 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.GENERIC },
                                                    "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "MM_8", "dimensions" : "MM_22_X_7", "seal" : "OPEN" }, "name" : "8 mm Round Radial Bearing (22 mm OD x 7 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_8, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "10 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_26_X_8, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_10" }, "name" : "10 mm Round Radial Bearing (26 mm OD x 8 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_10, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "12 mm" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "21 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_21_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_12", "dimensions" : "MM_21_X_5" }, "name" : "12 mm Round Radial Bearing (21 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_12, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "28 mm OD x 8 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_28_X_8, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_12", "dimensions" : "MM_28_X_8" }, "name" : "12 mm Round Radial Bearing (28 mm OD x 8 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_12, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "15 mm" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "21 mm OD x 4 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_21_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_15", "dimensions" : "MM_21_X_4" }, "name" : "15 mm Round Radial Bearing (21 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_15, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "24 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_24_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_15", "dimensions" : "MM_24_X_5" }, "name" : "15 mm Round Radial Bearing (24 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_15, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "28 mm OD x 7 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_28_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_15", "dimensions" : "MM_28_X_7" }, "name" : "15 mm Round Radial Bearing (28 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_15, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "17 mm" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "23 mm OD x 4 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_23_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_17", "dimensions" : "MM_23_X_4" }, "name" : "17 mm Round Radial Bearing (23 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_17, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                            "26 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_26_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_17", "dimensions" : "MM_26_X_5" }, "name" : "17 mm Round Radial Bearing (26 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_17, "vendor" : BearingVendor.GENERIC },
                                                },
                                            },
                                        },
                                    },
                                    "25 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_37_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_25", "seal" : "SHIELDED" }, "name" : "25 mm Round Radial Bearing (37 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_25, "vendor" : BearingVendor.GENERIC },
                                            "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_37_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_25", "seal" : "OPEN" }, "name" : "25 mm Round Radial Bearing (37 mm OD x 7 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_25, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "30 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_42_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_30", "seal" : "SHIELDED" }, "name" : "30 mm Round Radial Bearing (42 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_30, "vendor" : BearingVendor.GENERIC },
                                            "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_42_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_30", "seal" : "OPEN" }, "name" : "30 mm Round Radial Bearing (42 mm OD x 7 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_30, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "35 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_47_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "bore" : "ROUND", "size" : "MM_35" }, "name" : "35 mm Round Radial Bearing (47 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_35, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                    "80 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Sealed" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_100_X_10, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "type" : "RADIAL", "size" : "MM_80" }, "name" : "80 mm Round Radial Bearing (100 mm OD x 10 mm, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.MM_80, "vendor" : BearingVendor.GENERIC },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            },
            "WCP" : {
                "name" : "type",
                "displayName" : "Type",
                "entries" : {
                    "Flanged" : {
                        "name" : "bore",
                        "displayName" : "Bore",
                        "default" : "Rounded hex",
                        "entries" : {
                            "Hex" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "1/2 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "bore" : "HEX", "size" : "IN_1_2" }, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                    "3/8 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "bore" : "HEX", "size" : "IN_3_8" }, "name" : "3/8 in Hex Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                },
                            },
                            "Rounded hex" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "1/2 in" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "default" : "1.125 in OD x 0.313 in",
                                        "entries" : {
                                            "0.875 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUNDED_HEX, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "bore" : "ROUNDED_HEX", "size" : "IN_1_2", "dimensions" : "OD_0_875_X_0_281" }, "name" : "1/2 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.WCP },
                                                },
                                            },
                                            "1 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUNDED_HEX, "dimensions" : BearingDimensions.OD_1_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "bore" : "ROUNDED_HEX", "size" : "IN_1_2", "dimensions" : "OD_1_X_0_281" }, "name" : "1/2 in Rounded Hex Flanged Bearing (1 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.WCP },
                                                },
                                            },
                                            "1.125 in OD x 0.313 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUNDED_HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "bore" : "ROUNDED_HEX", "size" : "IN_1_2", "dimensions" : "OD_1_125_X_0_313" }, "name" : "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.WCP },
                                                },
                                            },
                                        },
                                    },
                                    "3/8 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUNDED_HEX, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "bore" : "ROUNDED_HEX", "size" : "IN_3_8" }, "name" : "3/8 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                },
                            },
                            "Round" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "1/2 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_1_2" }, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                    "3/8 in" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "default" : "1.125 in OD x 0.313 in",
                                        "entries" : {
                                            "0.875 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_3_8", "dimensions" : "OD_0_875_X_0_281" }, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.WCP },
                                                },
                                            },
                                            "1 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "size" : "IN_3_8" }, "name" : "3/8 in Round Flanged Bearing (1 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.WCP },
                                                },
                                            },
                                            "1.125 in OD x 0.313 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_3_8", "dimensions" : "OD_1_125_X_0_313" }, "name" : "3/8 in Round Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.WCP },
                                                },
                                            },
                                        },
                                    },
                                    "1/4 in" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "0.5 in OD x 0.188 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_5_X_0_188, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_1_4", "dimensions" : "OD_0_5_X_0_188" }, "name" : "1/4 in Round Flanged Bearing (0.5 in OD x 0.188 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.WCP },
                                                },
                                            },
                                            "0.625 in OD x 0.196 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_625_X_0_196, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_1_4", "dimensions" : "OD_0_625_X_0_196" }, "name" : "1/4 in Round Flanged Bearing (0.625 in OD x 0.196 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.WCP },
                                                },
                                            },
                                        },
                                    },
                                    "8 mm" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "0.875 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "size" : "MM_8" }, "name" : "8 mm Round Flanged Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.WCP },
                                                },
                                            },
                                            "16 mm OD x 5 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "bore" : "ROUND", "size" : "MM_8" }, "name" : "8 mm Round Flanged Bearing (16 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.WCP },
                                                },
                                            },
                                        },
                                    },
                                    "12 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_24_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "size" : "MM_12" }, "name" : "12 mm Round Flanged Bearing (24 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_12, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                    "30 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_42_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "size" : "MM_30" }, "name" : "30 mm Round Flanged Bearing (42 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_30, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                    "35 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_47_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "FLANGED", "bore" : "ROUND", "size" : "MM_35" }, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_35, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                },
                            },
                        },
                    },
                    "Radial" : {
                        "name" : "size",
                        "displayName" : "Size",
                        "entries" : {
                            "1/2 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "default" : "1.125 in OD x 0.313 in",
                                "entries" : {
                                    "0.875 in OD x 0.281 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "IN_1_2", "dimensions" : "OD_0_875_X_0_281" }, "name" : "1/2 in Round Radial Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                    "1.125 in OD x 0.313 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "IN_1_2", "dimensions" : "OD_1_125_X_0_313" }, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                },
                            },
                            "3/8 in" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "IN_3_8" }, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.WCP },
                                },
                            },
                            "3/16 in" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_5_X_0_196, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "IN_3_16" }, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.196 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_16, "vendor" : BearingVendor.WCP },
                                },
                            },
                            "1/4 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.375 in OD x 0.125 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_375_X_0_125, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "IN_1_4", "dimensions" : "OD_0_375_X_0_125" }, "name" : "1/4 in Round Radial Bearing (0.375 in OD x 0.125 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                    "0.5 in OD x 0.188 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_5_X_0_188, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "IN_1_4", "dimensions" : "OD_0_5_X_0_188" }, "name" : "1/4 in Round Radial Bearing (0.5 in OD x 0.188 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                    "0.625 in OD x 0.196 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_625_X_0_196, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "IN_1_4", "dimensions" : "OD_0_625_X_0_196" }, "name" : "1/4 in Round Radial Bearing (0.625 in OD x 0.196 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                    "0.75 in OD x 0.281 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_75_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "IN_1_4", "dimensions" : "OD_0_75_X_0_281" }, "name" : "1/4 in Round Radial Bearing (0.75 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                },
                            },
                            "6 mm" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_12_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "MM_6" }, "name" : "6 mm Round Radial Bearing (12 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.WCP },
                                },
                            },
                            "12 mm" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_21_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "MM_12" }, "name" : "12 mm Round Radial Bearing (21 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_12, "vendor" : BearingVendor.WCP },
                                },
                            },
                            "15 mm" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "24 mm OD x 5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_24_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "MM_15", "dimensions" : "MM_24_X_5" }, "name" : "15 mm Round Radial Bearing (24 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_15, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                    "28 mm OD x 7 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_28_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "MM_15", "dimensions" : "MM_28_X_7" }, "name" : "15 mm Round Radial Bearing (28 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_15, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                },
                            },
                            "17 mm" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "23 mm OD x 4 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_23_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "MM_17", "dimensions" : "MM_23_X_4" }, "name" : "17 mm Round Radial Bearing (23 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_17, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                    "26 mm OD x 5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_26_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "MM_17", "dimensions" : "MM_26_X_5" }, "name" : "17 mm Round Radial Bearing (26 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_17, "vendor" : BearingVendor.WCP },
                                        },
                                    },
                                },
                            },
                            "25 mm" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_37_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "MM_25" }, "name" : "25 mm Round Radial Bearing (37 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_25, "vendor" : BearingVendor.WCP },
                                },
                            },
                            "80 mm" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Sealed" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_100_X_10, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "type" : "RADIAL", "size" : "MM_80" }, "name" : "80 mm Round Radial Bearing (100 mm OD x 10 mm, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.MM_80, "vendor" : BearingVendor.WCP },
                                },
                            },
                        },
                    },
                },
            },
            "AndyMark" : {
                "name" : "type",
                "displayName" : "Type",
                "entries" : {
                    "Flanged" : {
                        "name" : "bore",
                        "displayName" : "Bore",
                        "entries" : {
                            "Hex" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "1/2 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "FLANGED", "bore" : "HEX", "size" : "IN_1_2" }, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.ANDYMARK },
                                        },
                                    },
                                    "3/8 in" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "default" : "1.125 in OD x 0.313 in",
                                        "entries" : {
                                            "0.875 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Sealed" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "FLANGED", "bore" : "HEX", "size" : "IN_3_8", "dimensions" : "OD_0_875_X_0_281" }, "name" : "3/8 in Hex Flanged Bearing (0.875 in OD x 0.281 in, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.ANDYMARK },
                                                },
                                            },
                                            "1.125 in OD x 0.313 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "FLANGED", "bore" : "HEX", "size" : "IN_3_8", "dimensions" : "OD_1_125_X_0_313" }, "name" : "3/8 in Hex Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.ANDYMARK },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                            "Round" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "1/2 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_1_2", "seal" : "SHIELDED" }, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.ANDYMARK },
                                            "Sealed" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_1_2", "seal" : "SEALED" }, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.ANDYMARK },
                                        },
                                    },
                                    "3/8 in" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "default" : "1.125 in OD x 0.313 in",
                                        "entries" : {
                                            "0.875 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_3_8", "dimensions" : "OD_0_875_X_0_281", "seal" : "SHIELDED" }, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.ANDYMARK },
                                                    "Sealed" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_3_8", "dimensions" : "OD_0_875_X_0_281", "seal" : "SEALED" }, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.ANDYMARK },
                                                },
                                            },
                                            "1.125 in OD x 0.313 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_3_8", "dimensions" : "OD_1_125_X_0_313" }, "name" : "3/8 in Round Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.ANDYMARK },
                                                },
                                            },
                                        },
                                    },
                                    "5 mm" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "11 mm OD x 4 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_11_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "FLANGED", "bore" : "ROUND", "size" : "MM_5", "dimensions" : "MM_11_X_4" }, "name" : "5 mm Round Flanged Bearing (11 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.ANDYMARK },
                                                },
                                            },
                                            "13 mm OD x 4 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_13_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "FLANGED", "bore" : "ROUND", "size" : "MM_5", "dimensions" : "MM_13_X_4" }, "name" : "5 mm Round Flanged Bearing (13 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.ANDYMARK },
                                                },
                                            },
                                        },
                                    },
                                    "6 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_12_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "FLANGED", "size" : "MM_6" }, "name" : "6 mm Round Flanged Bearing (12 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.ANDYMARK },
                                        },
                                    },
                                    "8 mm" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "19 mm OD x 6 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_19_X_6" }, "name" : "8 mm Round Flanged Bearing (19 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.ANDYMARK },
                                                },
                                            },
                                            "22 mm OD x 7 mm" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_22_X_7" }, "name" : "8 mm Round Flanged Bearing (22 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.ANDYMARK },
                                                },
                                            },
                                        },
                                    },
                                    "15 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_21_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "FLANGED", "size" : "MM_15" }, "name" : "15 mm Round Flanged Bearing (21 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_15, "vendor" : BearingVendor.ANDYMARK },
                                        },
                                    },
                                },
                            },
                        },
                    },
                    "Radial" : {
                        "name" : "bore",
                        "displayName" : "Bore",
                        "entries" : {
                            "Hex" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "1/2 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Sealed" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "RADIAL", "bore" : "HEX", "size" : "IN_1_2" }, "name" : "1/2 in Hex Radial Bearing (1.125 in OD x 0.313 in, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.ANDYMARK },
                                        },
                                    },
                                    "3/8 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Sealed" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "RADIAL", "bore" : "HEX", "size" : "IN_3_8" }, "name" : "3/8 in Hex Radial Bearing (0.875 in OD x 0.281 in, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.ANDYMARK },
                                        },
                                    },
                                },
                            },
                            "Round" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "1/2 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_1_2" }, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.ANDYMARK },
                                        },
                                    },
                                    "3/8 in" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "0.875 in OD x 0.281 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_3_8", "dimensions" : "OD_0_875_X_0_281" }, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.ANDYMARK },
                                                },
                                            },
                                            "1.125 in OD x 0.375 in" : {
                                                "name" : "seal",
                                                "displayName" : "Seal",
                                                "entries" : {
                                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_375, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_3_8", "dimensions" : "OD_1_125_X_0_375" }, "name" : "3/8 in Round Radial Bearing (1.125 in OD x 0.375 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.ANDYMARK },
                                                },
                                            },
                                        },
                                    },
                                    "3/16 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_5_X_0_196, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_3_16" }, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.196 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_16, "vendor" : BearingVendor.ANDYMARK },
                                        },
                                    },
                                    "#10 (0.19 in)" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_375_X_0_156_EXTENDED, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "RADIAL", "bore" : "ROUND", "size" : "NO_10" }, "name" : "0.19 in Round Radial Bearing (0.375 in OD x 0.156 in (extended inner race))" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.NO_10, "vendor" : BearingVendor.ANDYMARK },
                                        },
                                    },
                                    "1/4 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_625_X_0_196, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "RADIAL", "bore" : "ROUND", "size" : "IN_1_4" }, "name" : "1/4 in Round Radial Bearing (0.625 in OD x 0.196 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.ANDYMARK },
                                        },
                                    },
                                    "5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_13_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "RADIAL", "bore" : "ROUND", "size" : "MM_5" }, "name" : "5 mm Round Radial Bearing (13 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.ANDYMARK },
                                        },
                                    },
                                    "6 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_17_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "RADIAL" }, "name" : "6 mm Round Radial Bearing (17 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.ANDYMARK },
                                        },
                                    },
                                    "8 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "ANDYMARK", "type" : "RADIAL", "bore" : "ROUND", "size" : "MM_8" }, "name" : "8 mm Round Radial Bearing (22 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.ANDYMARK },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            },
            "REV" : {
                "name" : "type",
                "displayName" : "Type",
                "entries" : {
                    "Flanged" : {
                        "name" : "bore",
                        "displayName" : "Bore",
                        "default" : "Rounded hex",
                        "entries" : {
                            "Hex" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "REV", "type" : "FLANGED", "bore" : "HEX" }, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.REV },
                                },
                            },
                            "Rounded hex" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "default" : "1.125 in OD x 0.313 in",
                                "entries" : {
                                    "1 in OD x 0.281 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUNDED_HEX, "dimensions" : BearingDimensions.OD_1_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "REV", "type" : "FLANGED", "bore" : "ROUNDED_HEX", "dimensions" : "OD_1_X_0_281" }, "name" : "1/2 in Rounded Hex Flanged Bearing (1 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.REV },
                                        },
                                    },
                                    "1.125 in OD x 0.313 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUNDED_HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "REV", "type" : "FLANGED", "bore" : "ROUNDED_HEX", "dimensions" : "OD_1_125_X_0_313" }, "name" : "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.REV },
                                        },
                                    },
                                },
                            },
                            "Round" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "#10 (0.19 in)" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "REV", "type" : "FLANGED", "bore" : "ROUND", "size" : "NO_10" }, "name" : "0.19 in Round Flanged Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.NO_10, "vendor" : BearingVendor.REV },
                                        },
                                    },
                                    "35 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_47_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "REV", "type" : "FLANGED", "bore" : "ROUND", "size" : "MM_35" }, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_35, "vendor" : BearingVendor.REV },
                                        },
                                    },
                                },
                            },
                        },
                    },
                    "Radial" : {
                        "name" : "seal",
                        "displayName" : "Seal",
                        "entries" : {
                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_47_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "REV", "type" : "RADIAL" }, "name" : "35 mm Round Radial Bearing (47 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_35, "vendor" : BearingVendor.REV },
                        },
                    },
                },
            },
            "ThriftyBot" : {
                "name" : "type",
                "displayName" : "Type",
                "entries" : {
                    "Flanged" : {
                        "name" : "bore",
                        "displayName" : "Bore",
                        "default" : "Rounded hex",
                        "entries" : {
                            "Hex" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "type" : "FLANGED", "bore" : "HEX" }, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.THRIFTYBOT },
                                },
                            },
                            "Rounded hex" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "1/2 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUNDED_HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "type" : "FLANGED", "bore" : "ROUNDED_HEX", "size" : "IN_1_2" }, "name" : "1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.THRIFTYBOT },
                                        },
                                    },
                                    "3/8 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUNDED_HEX, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "type" : "FLANGED", "bore" : "ROUNDED_HEX", "size" : "IN_3_8" }, "name" : "3/8 in Rounded Hex Flanged Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.THRIFTYBOT },
                                        },
                                    },
                                },
                            },
                            "Round" : {
                                "name" : "size",
                                "displayName" : "Size",
                                "entries" : {
                                    "1/2 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_1_2" }, "name" : "1/2 in Round Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.THRIFTYBOT },
                                        },
                                    },
                                    "3/8 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "type" : "FLANGED", "bore" : "ROUND", "size" : "IN_3_8" }, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.THRIFTYBOT },
                                        },
                                    },
                                    "8 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_6_3, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT" }, "name" : "8 mm Round Flanged Bearing (16 mm OD x 6.3 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.THRIFTYBOT },
                                        },
                                    },
                                    "35 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Sealed" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_47_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "type" : "FLANGED", "bore" : "ROUND", "size" : "MM_35" }, "name" : "35 mm Round Flanged Bearing (47 mm OD x 7 mm, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.MM_35, "vendor" : BearingVendor.THRIFTYBOT },
                                        },
                                    },
                                },
                            },
                        },
                    },
                    "Radial" : {
                        "name" : "size",
                        "displayName" : "Size",
                        "entries" : {
                            "1/2 in" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "type" : "RADIAL", "size" : "IN_1_2" }, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.THRIFTYBOT },
                                },
                            },
                            "3/8 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.875 in OD x 0.281 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "type" : "RADIAL", "size" : "IN_3_8", "dimensions" : "OD_0_875_X_0_281" }, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.THRIFTYBOT },
                                        },
                                    },
                                    "1.125 in OD x 0.375 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_375, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "type" : "RADIAL", "size" : "IN_3_8", "dimensions" : "OD_1_125_X_0_375" }, "name" : "3/8 in Round Radial Bearing (1.125 in OD x 0.375 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.THRIFTYBOT },
                                        },
                                    },
                                },
                            },
                            "1/4 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.375 in OD x 0.125 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_375_X_0_125, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "type" : "RADIAL", "size" : "IN_1_4", "dimensions" : "OD_0_375_X_0_125" }, "name" : "1/4 in Round Radial Bearing (0.375 in OD x 0.125 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.THRIFTYBOT },
                                        },
                                    },
                                    "0.5 in OD x 0.188 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_5_X_0_188, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "type" : "RADIAL", "size" : "IN_1_4", "dimensions" : "OD_0_5_X_0_188" }, "name" : "1/4 in Round Radial Bearing (0.5 in OD x 0.188 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.THRIFTYBOT },
                                        },
                                    },
                                    "0.75 in OD x 0.281 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_75_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "type" : "RADIAL", "size" : "IN_1_4", "dimensions" : "OD_0_75_X_0_281" }, "name" : "1/4 in Round Radial Bearing (0.75 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.THRIFTYBOT },
                                        },
                                    },
                                },
                            },
                            "35 mm" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_47_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "type" : "RADIAL", "size" : "MM_35" }, "name" : "35 mm Round Radial Bearing (47 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_35, "vendor" : BearingVendor.THRIFTYBOT },
                                },
                            },
                        },
                    },
                },
            },
            "Swyft" : {
                "name" : "bore",
                "displayName" : "Bore",
                "entries" : {
                    "Hex" : {
                        "name" : "seal",
                        "displayName" : "Seal",
                        "entries" : {
                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.HEX, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "SWYFT", "bore" : "HEX" }, "name" : "1/2 in Hex Flanged Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.SWYFT },
                        },
                    },
                    "Round" : {
                        "name" : "size",
                        "displayName" : "Size",
                        "entries" : {
                            "3/8 in" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "SWYFT", "bore" : "ROUND", "size" : "IN_3_8" }, "name" : "3/8 in Round Flanged Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.SWYFT },
                                },
                            },
                            "8 mm" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_14_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "SWYFT", "bore" : "ROUND", "size" : "MM_8" }, "name" : "8 mm Round Flanged Bearing (14 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.SWYFT },
                                },
                            },
                        },
                    },
                },
            },
            "SDS" : {
                "name" : "type",
                "displayName" : "Type",
                "entries" : {
                    "Flanged" : {
                        "name" : "seal",
                        "displayName" : "Seal",
                        "entries" : {
                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_17_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "SDS", "type" : "FLANGED" }, "name" : "9 mm Round Flanged Bearing (17 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_9, "vendor" : BearingVendor.SDS },
                        },
                    },
                    "Radial" : {
                        "name" : "size",
                        "displayName" : "Size",
                        "entries" : {
                            "1/2 in" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "SDS", "size" : "IN_1_2" }, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.SDS },
                                },
                            },
                            "3/8 in" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "SDS", "size" : "IN_3_8" }, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.SDS },
                                },
                            },
                            "1/4 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.5 in OD x 0.188 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_5_X_0_188, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "SDS", "size" : "IN_1_4", "dimensions" : "OD_0_5_X_0_188" }, "name" : "1/4 in Round Radial Bearing (0.5 in OD x 0.188 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.SDS },
                                        },
                                    },
                                    "0.75 in OD x 0.281 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_75_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "SDS", "size" : "IN_1_4", "dimensions" : "OD_0_75_X_0_281" }, "name" : "1/4 in Round Radial Bearing (0.75 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.SDS },
                                        },
                                    },
                                },
                            },
                            "8 mm" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "SDS", "size" : "MM_8" }, "name" : "8 mm Round Radial Bearing (16 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.SDS },
                                },
                            },
                            "15 mm" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_24_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "SDS", "type" : "RADIAL" }, "name" : "15 mm Round Radial Bearing (24 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_15, "vendor" : BearingVendor.SDS },
                                },
                            },
                        },
                    },
                },
            },
            "McMaster-Carr" : {
                "name" : "type",
                "displayName" : "Type",
                "entries" : {
                    "Flanged" : {
                        "name" : "size",
                        "displayName" : "Size",
                        "entries" : {
                            "5 mm" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "8 mm OD x 2 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_8_X_2, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_8_X_2" }, "name" : "5 mm Round Flanged Bearing (8 mm OD x 2 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "9 mm OD x 2.5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_9_X_2_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_9_X_2_5" }, "name" : "5 mm Round Flanged Bearing (9 mm OD x 2.5 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "9 mm OD x 3 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_9_X_3, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_9_X_3" }, "name" : "5 mm Round Flanged Bearing (9 mm OD x 3 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "10 mm OD x 3 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_10_X_3, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_10_X_3" }, "name" : "5 mm Round Flanged Bearing (10 mm OD x 3 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "10 mm OD x 4 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_10_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_10_X_4" }, "name" : "5 mm Round Flanged Bearing (10 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "11 mm OD x 3 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_11_X_3, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_11_X_3" }, "name" : "5 mm Round Flanged Bearing (11 mm OD x 3 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "11 mm OD x 4 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_11_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_11_X_4" }, "name" : "5 mm Round Flanged Bearing (11 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "11 mm OD x 5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_11_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_11_X_5" }, "name" : "5 mm Round Flanged Bearing (11 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "13 mm OD x 4 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_13_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_13_X_4" }, "name" : "5 mm Round Flanged Bearing (13 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "14 mm OD x 5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_14_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_14_X_5", "seal" : "SHIELDED" }, "name" : "5 mm Round Flanged Bearing (14 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_14_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_14_X_5", "seal" : "OPEN" }, "name" : "5 mm Round Flanged Bearing (14 mm OD x 5 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "16 mm OD x 5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_16_X_5", "seal" : "SHIELDED" }, "name" : "5 mm Round Flanged Bearing (16 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_16_X_5", "seal" : "OPEN" }, "name" : "5 mm Round Flanged Bearing (16 mm OD x 5 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "19 mm OD x 6 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_5", "dimensions" : "MM_19_X_6" }, "name" : "5 mm Round Flanged Bearing (19 mm OD x 6 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                },
                            },
                            "6 mm" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "10 mm OD x 2.5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_10_X_2_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_10_X_2_5" }, "name" : "6 mm Round Flanged Bearing (10 mm OD x 2.5 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_6, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "10 mm OD x 3 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_10_X_3, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_10_X_3" }, "name" : "6 mm Round Flanged Bearing (10 mm OD x 3 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "13 mm OD x 3.5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_13_X_3_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_13_X_3_5" }, "name" : "6 mm Round Flanged Bearing (13 mm OD x 3.5 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_6, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "13 mm OD x 5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_13_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_13_X_5" }, "name" : "6 mm Round Flanged Bearing (13 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "15 mm OD x 5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_15_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_15_X_5", "seal" : "SHIELDED" }, "name" : "6 mm Round Flanged Bearing (15 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.MCMASTER },
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_15_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_15_X_5", "seal" : "OPEN" }, "name" : "6 mm Round Flanged Bearing (15 mm OD x 5 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_6, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "17 mm OD x 6 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_17_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_17_X_6", "seal" : "SHIELDED" }, "name" : "6 mm Round Flanged Bearing (17 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.MCMASTER },
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_17_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_17_X_6", "seal" : "OPEN" }, "name" : "6 mm Round Flanged Bearing (17 mm OD x 6 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_6, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "19 mm OD x 6 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_19_X_6", "seal" : "SHIELDED" }, "name" : "6 mm Round Flanged Bearing (19 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_6, "vendor" : BearingVendor.MCMASTER },
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_6", "dimensions" : "MM_19_X_6", "seal" : "OPEN" }, "name" : "6 mm Round Flanged Bearing (19 mm OD x 6 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_6, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                },
                            },
                            "8 mm" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "14 mm OD x 4 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_14_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_14_X_4" }, "name" : "8 mm Round Flanged Bearing (14 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "16 mm OD x 4 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_16_X_4" }, "name" : "8 mm Round Flanged Bearing (16 mm OD x 4 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_8, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "16 mm OD x 5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_8" }, "name" : "8 mm Round Flanged Bearing (16 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "19 mm OD x 6 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_19_X_6", "seal" : "SHIELDED" }, "name" : "8 mm Round Flanged Bearing (19 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.MCMASTER },
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_19_X_6", "seal" : "OPEN" }, "name" : "8 mm Round Flanged Bearing (19 mm OD x 6 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_8, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "22 mm OD x 7 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_22_X_7", "seal" : "SHIELDED" }, "name" : "8 mm Round Flanged Bearing (22 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.MCMASTER },
                                            "Open" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_8", "dimensions" : "MM_22_X_7", "seal" : "OPEN" }, "name" : "8 mm Round Flanged Bearing (22 mm OD x 7 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_8, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                },
                            },
                            "30 mm" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.FLANGED, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_42_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "FLANGED", "size" : "MM_30" }, "name" : "30 mm Round Flanged Bearing (42 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_30, "vendor" : BearingVendor.MCMASTER },
                                },
                            },
                        },
                    },
                    "Radial" : {
                        "name" : "size",
                        "displayName" : "Size",
                        "entries" : {
                            "1/2 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "default" : "1.125 in OD x 0.313 in",
                                "entries" : {
                                    "1.125 in OD x 0.25 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_25, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "IN_1_2" }, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.25 in, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "1.125 in OD x 0.313 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_1_125_X_0_313, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "IN_1_2" }, "name" : "1/2 in Round Radial Bearing (1.125 in OD x 0.313 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_2, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                },
                            },
                            "3/8 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.875 in OD x 0.219 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_219, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "IN_3_8" }, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.219 in, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "0.875 in OD x 0.281 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_875_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "IN_3_8" }, "name" : "3/8 in Round Radial Bearing (0.875 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_8, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                },
                            },
                            "3/16 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.5 in OD x 0.156 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_5_X_0_156, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "IN_3_16" }, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.156 in, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.IN_3_16, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "0.5 in OD x 0.196 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_5_X_0_196, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "IN_3_16" }, "name" : "3/16 in Round Radial Bearing (0.5 in OD x 0.196 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_3_16, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                },
                            },
                            "1/4 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.625 in OD x 0.196 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_625_X_0_196, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "IN_1_4", "dimensions" : "OD_0_625_X_0_196", "seal" : "SHIELDED" }, "name" : "1/4 in Round Radial Bearing (0.625 in OD x 0.196 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.MCMASTER },
                                            "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_625_X_0_196, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "IN_1_4", "dimensions" : "OD_0_625_X_0_196", "seal" : "OPEN" }, "name" : "1/4 in Round Radial Bearing (0.625 in OD x 0.196 in, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "0.75 in OD x 0.281 in" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.OD_0_75_X_0_281, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "IN_1_4", "dimensions" : "OD_0_75_X_0_281" }, "name" : "1/4 in Round Radial Bearing (0.75 in OD x 0.281 in)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.IN_1_4, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                },
                            },
                            "5 mm" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "8 mm OD x 2.5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_8_X_2_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_8_X_2_5" }, "name" : "5 mm Round Radial Bearing (8 mm OD x 2.5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "9 mm OD x 3 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_9_X_3, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_9_X_3" }, "name" : "5 mm Round Radial Bearing (9 mm OD x 3 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "10 mm OD x 4 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_10_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_10_X_4" }, "name" : "5 mm Round Radial Bearing (10 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "11 mm OD x 4 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_11_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_11_X_4" }, "name" : "5 mm Round Radial Bearing (11 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "13 mm OD x 4 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_13_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_5" }, "name" : "5 mm Round Radial Bearing (13 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "14 mm OD x 5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_14_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_14_X_5" }, "name" : "5 mm Round Radial Bearing (14 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "16 mm OD x 5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_16_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_16_X_5" }, "name" : "5 mm Round Radial Bearing (16 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "19 mm OD x 6 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_19_X_6", "seal" : "SHIELDED" }, "name" : "5 mm Round Radial Bearing (19 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                            "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_5", "dimensions" : "MM_19_X_6", "seal" : "OPEN" }, "name" : "5 mm Round Radial Bearing (19 mm OD x 6 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_5, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                },
                            },
                            "7 mm" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "14 mm OD x 5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_14_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_7", "dimensions" : "MM_14_X_5" }, "name" : "7 mm Round Radial Bearing (14 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_7, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "19 mm OD x 6 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_19_X_6, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_7", "dimensions" : "MM_19_X_6" }, "name" : "7 mm Round Radial Bearing (19 mm OD x 6 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_7, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "22 mm OD x 7 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_7", "dimensions" : "MM_22_X_7", "seal" : "SHIELDED" }, "name" : "7 mm Round Radial Bearing (22 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_7, "vendor" : BearingVendor.MCMASTER },
                                            "Sealed" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_7", "dimensions" : "MM_22_X_7", "seal" : "SEALED" }, "name" : "7 mm Round Radial Bearing (22 mm OD x 7 mm, Sealed)" }, "seal" : BearingSeal.SEALED, "size" : BearingSize.MM_7, "vendor" : BearingVendor.MCMASTER },
                                            "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_7", "dimensions" : "MM_22_X_7", "seal" : "OPEN" }, "name" : "7 mm Round Radial Bearing (22 mm OD x 7 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_7, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                },
                            },
                            "8 mm" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_8", "seal" : "SHIELDED" }, "name" : "8 mm Round Radial Bearing (22 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_8, "vendor" : BearingVendor.MCMASTER },
                                    "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_22_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_8", "seal" : "OPEN" }, "name" : "8 mm Round Radial Bearing (22 mm OD x 7 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_8, "vendor" : BearingVendor.MCMASTER },
                                },
                            },
                            "10 mm" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_26_X_8, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_10" }, "name" : "10 mm Round Radial Bearing (26 mm OD x 8 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_10, "vendor" : BearingVendor.MCMASTER },
                                },
                            },
                            "12 mm" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "21 mm OD x 5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_21_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_12", "dimensions" : "MM_21_X_5" }, "name" : "12 mm Round Radial Bearing (21 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_12, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "28 mm OD x 8 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_28_X_8, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_12", "dimensions" : "MM_28_X_8" }, "name" : "12 mm Round Radial Bearing (28 mm OD x 8 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_12, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                },
                            },
                            "15 mm" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "21 mm OD x 4 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_21_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_15", "dimensions" : "MM_21_X_4" }, "name" : "15 mm Round Radial Bearing (21 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_15, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "24 mm OD x 5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_24_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_15", "dimensions" : "MM_24_X_5" }, "name" : "15 mm Round Radial Bearing (24 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_15, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "28 mm OD x 7 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_28_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_15", "dimensions" : "MM_28_X_7" }, "name" : "15 mm Round Radial Bearing (28 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_15, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                },
                            },
                            "17 mm" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "23 mm OD x 4 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_23_X_4, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_17", "dimensions" : "MM_23_X_4" }, "name" : "17 mm Round Radial Bearing (23 mm OD x 4 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_17, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                    "26 mm OD x 5 mm" : {
                                        "name" : "seal",
                                        "displayName" : "Seal",
                                        "entries" : {
                                            "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_26_X_5, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_17", "dimensions" : "MM_26_X_5" }, "name" : "17 mm Round Radial Bearing (26 mm OD x 5 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_17, "vendor" : BearingVendor.MCMASTER },
                                        },
                                    },
                                },
                            },
                            "25 mm" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_37_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_25", "seal" : "SHIELDED" }, "name" : "25 mm Round Radial Bearing (37 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_25, "vendor" : BearingVendor.MCMASTER },
                                    "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_37_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_25", "seal" : "OPEN" }, "name" : "25 mm Round Radial Bearing (37 mm OD x 7 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_25, "vendor" : BearingVendor.MCMASTER },
                                },
                            },
                            "30 mm" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_42_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_30", "seal" : "SHIELDED" }, "name" : "30 mm Round Radial Bearing (42 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_30, "vendor" : BearingVendor.MCMASTER },
                                    "Open" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_42_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_30", "seal" : "OPEN" }, "name" : "30 mm Round Radial Bearing (42 mm OD x 7 mm, Open)" }, "seal" : BearingSeal.OPEN, "size" : BearingSize.MM_30, "vendor" : BearingVendor.MCMASTER },
                                },
                            },
                            "35 mm" : {
                                "name" : "seal",
                                "displayName" : "Seal",
                                "entries" : {
                                    "Shielded" : { "bearingType" : BearingType.RADIAL, "bore" : BearingBore.ROUND, "dimensions" : BearingDimensions.MM_47_X_7, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "MCMASTER", "type" : "RADIAL", "size" : "MM_35" }, "name" : "35 mm Round Radial Bearing (47 mm OD x 7 mm)" }, "seal" : BearingSeal.SHIELDED, "size" : BearingSize.MM_35, "vendor" : BearingVendor.MCMASTER },
                                },
                            },
                        },
                    },
                },
            },
        },
    };

export const bushingTable = {
        "name" : "vendor",
        "displayName" : "Vendor",
        "entries" : {
            "Generic" : {
                "name" : "bore",
                "displayName" : "Bore",
                "entries" : {
                    "Rounded hex" : {
                        "name" : "size",
                        "displayName" : "Size",
                        "entries" : {
                            "1/2 in" : {
                                "name" : "material",
                                "displayName" : "Material",
                                "entries" : {
                                    "Bronze" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "0.75 in OD x 0.313 in" : { "bore" : BushingBore.ROUNDED_HEX, "dimensions" : BushingDimensions.OD_0_75_X_0_313, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "bore" : "ROUNDED_HEX", "size" : "IN_1_2", "material" : "BRONZE", "dimensions" : "OD_0_75_X_0_313" }, "name" : "1/2 in Rounded Hex Flanged Bronze Bushing (0.75 in OD x 0.313 in)" }, "size" : BushingSize.IN_1_2, "vendor" : BushingVendor.GENERIC },
                                            "0.75 in OD x 0.333 in" : { "bore" : BushingBore.ROUNDED_HEX, "dimensions" : BushingDimensions.OD_0_75_X_0_333, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "bore" : "ROUNDED_HEX", "size" : "IN_1_2", "material" : "BRONZE", "dimensions" : "OD_0_75_X_0_333" }, "name" : "1/2 in Rounded Hex Flanged Bronze Bushing (0.75 in OD x 0.333 in)" }, "size" : BushingSize.IN_1_2, "vendor" : BushingVendor.GENERIC },
                                        },
                                    },
                                    "Nylon" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "0.75 in OD x 0.375 in" : { "bore" : BushingBore.ROUNDED_HEX, "dimensions" : BushingDimensions.OD_0_75_X_0_375, "material" : BushingMaterial.NYLON, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "bore" : "ROUNDED_HEX", "size" : "IN_1_2", "material" : "NYLON" }, "name" : "1/2 in Rounded Hex Flanged Nylon Bushing (0.75 in OD x 0.375 in)" }, "size" : BushingSize.IN_1_2, "vendor" : BushingVendor.GENERIC },
                                        },
                                    },
                                },
                            },
                            "3/8 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.625 in OD x 0.313 in" : { "bore" : BushingBore.ROUNDED_HEX, "dimensions" : BushingDimensions.OD_0_625_X_0_313, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "bore" : "ROUNDED_HEX", "size" : "IN_3_8", "dimensions" : "OD_0_625_X_0_313" }, "name" : "3/8 in Rounded Hex Flanged Bronze Bushing (0.625 in OD x 0.313 in)" }, "size" : BushingSize.IN_3_8, "vendor" : BushingVendor.GENERIC },
                                    "0.625 in OD x 0.375 in" : { "bore" : BushingBore.ROUNDED_HEX, "dimensions" : BushingDimensions.OD_0_625_X_0_375, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "bore" : "ROUNDED_HEX", "size" : "IN_3_8", "dimensions" : "OD_0_625_X_0_375" }, "name" : "3/8 in Rounded Hex Flanged Bronze Bushing (0.625 in OD x 0.375 in)" }, "size" : BushingSize.IN_3_8, "vendor" : BushingVendor.GENERIC },
                                },
                            },
                        },
                    },
                    "Round" : {
                        "name" : "size",
                        "displayName" : "Size",
                        "entries" : {
                            "1/2 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.75 in OD x 0.313 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_0_75_X_0_313, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "bore" : "ROUND", "size" : "IN_1_2" }, "name" : "1/2 in Round Flanged Bronze Bushing (0.75 in OD x 0.313 in)" }, "size" : BushingSize.IN_1_2, "vendor" : BushingVendor.GENERIC },
                                },
                            },
                            "3/8 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.625 in OD x 0.313 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_0_625_X_0_313, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "bore" : "ROUND", "size" : "IN_3_8" }, "name" : "3/8 in Round Flanged Bronze Bushing (0.625 in OD x 0.313 in)" }, "size" : BushingSize.IN_3_8, "vendor" : BushingVendor.GENERIC },
                                },
                            },
                            "1/4 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.5 in OD x 0.313 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_0_5_X_0_313, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "bore" : "ROUND", "size" : "IN_1_4" }, "name" : "1/4 in Round Flanged Bronze Bushing (0.5 in OD x 0.313 in)" }, "size" : BushingSize.IN_1_4, "vendor" : BushingVendor.GENERIC },
                                },
                            },
                            "3/4 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.875 in OD x 0.313 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_0_875_X_0_313, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "bore" : "ROUND", "size" : "IN_3_4", "dimensions" : "OD_0_875_X_0_313" }, "name" : "3/4 in Round Flanged Bronze Bushing (0.875 in OD x 0.313 in)" }, "size" : BushingSize.IN_3_4, "vendor" : BushingVendor.GENERIC },
                                    "1.125 in OD x 0.333 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_1_125_X_0_333, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "bore" : "ROUND", "size" : "IN_3_4", "dimensions" : "OD_1_125_X_0_333" }, "name" : "3/4 in Round Flanged Bronze Bushing (1.125 in OD x 0.333 in)" }, "size" : BushingSize.IN_3_4, "vendor" : BushingVendor.GENERIC },
                                },
                            },
                            "7/8 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "1.125 in OD x 0.333 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_1_125_X_0_333, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "bore" : "ROUND", "size" : "IN_7_8" }, "name" : "7/8 in Round Flanged Bronze Bushing (1.125 in OD x 0.333 in)" }, "size" : BushingSize.IN_7_8, "vendor" : BushingVendor.GENERIC },
                                },
                            },
                            "1 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "1.125 in OD x 0.313 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_1_125_X_0_313, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "bore" : "ROUND", "size" : "IN_1", "dimensions" : "OD_1_125_X_0_313" }, "name" : "1 in Round Flanged Bronze Bushing (1.125 in OD x 0.313 in)" }, "size" : BushingSize.IN_1, "vendor" : BushingVendor.GENERIC },
                                    "1.125 in OD x 0.333 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_1_125_X_0_333, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "GENERIC", "bore" : "ROUND", "size" : "IN_1", "dimensions" : "OD_1_125_X_0_333" }, "name" : "1 in Round Flanged Bronze Bushing (1.125 in OD x 0.333 in)" }, "size" : BushingSize.IN_1, "vendor" : BushingVendor.GENERIC },
                                },
                            },
                        },
                    },
                },
            },
            "WCP" : {
                "name" : "bore",
                "displayName" : "Bore",
                "entries" : {
                    "Rounded hex" : {
                        "name" : "size",
                        "displayName" : "Size",
                        "entries" : {
                            "1/2 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.75 in OD x 0.313 in" : { "bore" : BushingBore.ROUNDED_HEX, "dimensions" : BushingDimensions.OD_0_75_X_0_313, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "bore" : "ROUNDED_HEX", "size" : "IN_1_2" }, "name" : "1/2 in Rounded Hex Flanged Bronze Bushing (0.75 in OD x 0.313 in)" }, "size" : BushingSize.IN_1_2, "vendor" : BushingVendor.WCP },
                                },
                            },
                            "3/8 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.625 in OD x 0.313 in" : { "bore" : BushingBore.ROUNDED_HEX, "dimensions" : BushingDimensions.OD_0_625_X_0_313, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "bore" : "ROUNDED_HEX", "size" : "IN_3_8" }, "name" : "3/8 in Rounded Hex Flanged Bronze Bushing (0.625 in OD x 0.313 in)" }, "size" : BushingSize.IN_3_8, "vendor" : BushingVendor.WCP },
                                },
                            },
                        },
                    },
                    "Round" : {
                        "name" : "size",
                        "displayName" : "Size",
                        "entries" : {
                            "1/2 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.75 in OD x 0.313 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_0_75_X_0_313, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "bore" : "ROUND", "size" : "IN_1_2" }, "name" : "1/2 in Round Flanged Bronze Bushing (0.75 in OD x 0.313 in)" }, "size" : BushingSize.IN_1_2, "vendor" : BushingVendor.WCP },
                                },
                            },
                            "3/8 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.625 in OD x 0.313 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_0_625_X_0_313, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "bore" : "ROUND", "size" : "IN_3_8" }, "name" : "3/8 in Round Flanged Bronze Bushing (0.625 in OD x 0.313 in)" }, "size" : BushingSize.IN_3_8, "vendor" : BushingVendor.WCP },
                                },
                            },
                            "1/4 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.5 in OD x 0.313 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_0_5_X_0_313, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "bore" : "ROUND", "size" : "IN_1_4" }, "name" : "1/4 in Round Flanged Bronze Bushing (0.5 in OD x 0.313 in)" }, "size" : BushingSize.IN_1_4, "vendor" : BushingVendor.WCP },
                                },
                            },
                            "3/4 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.875 in OD x 0.313 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_0_875_X_0_313, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "bore" : "ROUND", "size" : "IN_3_4" }, "name" : "3/4 in Round Flanged Bronze Bushing (0.875 in OD x 0.313 in)" }, "size" : BushingSize.IN_3_4, "vendor" : BushingVendor.WCP },
                                },
                            },
                            "1 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "1.125 in OD x 0.313 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_1_125_X_0_313, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "WCP", "bore" : "ROUND", "size" : "IN_1" }, "name" : "1 in Round Flanged Bronze Bushing (1.125 in OD x 0.313 in)" }, "size" : BushingSize.IN_1, "vendor" : BushingVendor.WCP },
                                },
                            },
                        },
                    },
                },
            },
            "ThriftyBot" : {
                "name" : "bore",
                "displayName" : "Bore",
                "entries" : {
                    "Rounded hex" : {
                        "name" : "size",
                        "displayName" : "Size",
                        "entries" : {
                            "1/2 in" : {
                                "name" : "material",
                                "displayName" : "Material",
                                "entries" : {
                                    "Bronze" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "0.75 in OD x 0.333 in" : { "bore" : BushingBore.ROUNDED_HEX, "dimensions" : BushingDimensions.OD_0_75_X_0_333, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "bore" : "ROUNDED_HEX", "size" : "IN_1_2", "material" : "BRONZE" }, "name" : "1/2 in Rounded Hex Flanged Bronze Bushing (0.75 in OD x 0.333 in)" }, "size" : BushingSize.IN_1_2, "vendor" : BushingVendor.THRIFTYBOT },
                                        },
                                    },
                                    "Nylon" : {
                                        "name" : "dimensions",
                                        "displayName" : "Dimensions",
                                        "entries" : {
                                            "0.75 in OD x 0.375 in" : { "bore" : BushingBore.ROUNDED_HEX, "dimensions" : BushingDimensions.OD_0_75_X_0_375, "material" : BushingMaterial.NYLON, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "bore" : "ROUNDED_HEX", "size" : "IN_1_2", "material" : "NYLON" }, "name" : "1/2 in Rounded Hex Flanged Nylon Bushing (0.75 in OD x 0.375 in)" }, "size" : BushingSize.IN_1_2, "vendor" : BushingVendor.THRIFTYBOT },
                                        },
                                    },
                                },
                            },
                            "3/8 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "0.625 in OD x 0.375 in" : { "bore" : BushingBore.ROUNDED_HEX, "dimensions" : BushingDimensions.OD_0_625_X_0_375, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "bore" : "ROUNDED_HEX", "size" : "IN_3_8" }, "name" : "3/8 in Rounded Hex Flanged Bronze Bushing (0.625 in OD x 0.375 in)" }, "size" : BushingSize.IN_3_8, "vendor" : BushingVendor.THRIFTYBOT },
                                },
                            },
                        },
                    },
                    "Round" : {
                        "name" : "size",
                        "displayName" : "Size",
                        "entries" : {
                            "3/4 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "1.125 in OD x 0.333 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_1_125_X_0_333, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "bore" : "ROUND", "size" : "IN_3_4" }, "name" : "3/4 in Round Flanged Bronze Bushing (1.125 in OD x 0.333 in)" }, "size" : BushingSize.IN_3_4, "vendor" : BushingVendor.THRIFTYBOT },
                                },
                            },
                            "7/8 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "1.125 in OD x 0.333 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_1_125_X_0_333, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "bore" : "ROUND", "size" : "IN_7_8" }, "name" : "7/8 in Round Flanged Bronze Bushing (1.125 in OD x 0.333 in)" }, "size" : BushingSize.IN_7_8, "vendor" : BushingVendor.THRIFTYBOT },
                                },
                            },
                            "1 in" : {
                                "name" : "dimensions",
                                "displayName" : "Dimensions",
                                "entries" : {
                                    "1.125 in OD x 0.333 in" : { "bore" : BushingBore.ROUND, "dimensions" : BushingDimensions.OD_1_125_X_0_333, "material" : BushingMaterial.BRONZE, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "vendor" : "THRIFTYBOT", "bore" : "ROUND", "size" : "IN_1" }, "name" : "1 in Round Flanged Bronze Bushing (1.125 in OD x 0.333 in)" }, "size" : BushingSize.IN_1, "vendor" : BushingVendor.THRIFTYBOT },
                                },
                            },
                        },
                    },
                },
            },
        },
    };

export const hatTable = {
        "name" : "bore",
        "displayName" : "Bore",
        "entries" : {
            "0.875 in" : {
                "name" : "centers",
                "displayName" : "Centers",
                "entries" : {
                    "1.25 in" : { "holeDiameter" : 0.201 * inch, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "size" : "OD_0_875", "centers" : "IN_1_25" }, "name" : "0.875\" OD Bearing Retention Plate (1.25\" C-C)" }, "thickness" : 0.09 * inch },
                    "2 in" : { "holeDiameter" : 0.201 * inch, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "size" : "OD_0_875", "centers" : "IN_2" }, "name" : "0.875\" OD Bearing Retention Plate (2\" C-C)" }, "thickness" : 0.09 * inch },
                },
            },
            "1.125 in" : {
                "name" : "centers",
                "displayName" : "Centers",
                "entries" : {
                    "1.5 in" : { "holeDiameter" : 0.201 * inch, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "size" : "OD_1_125", "centers" : "IN_1_5" }, "name" : "1.125\" OD Bearing Retention Plate (1.5\" C-C)" }, "thickness" : 0.09 * inch },
                    "2 in" : { "holeDiameter" : 0.201 * inch, "product" : { "documentId" : "", "elementId" : "", "configuration" : { "size" : "OD_1_125", "centers" : "IN_2" }, "name" : "1.125\" OD Bearing Retention Plate (2\" C-C)" }, "thickness" : 0.09 * inch },
                },
            },
        },
    };

export const screwStudioTable = {
        "name" : "studio",
        "displayName" : "Studio",
        "entries" : {
            "Screw" : { "documentId" : "77461d97ef7c9d816f0c7351", "elementId" : "33a58862fedc0f265077f7dd" },
            "Nut" : { "documentId" : "77461d97ef7c9d816f0c7351", "elementId" : "7ea1af35e52d2ba86db5870a" },
        },
    };
