FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "93af3f24abb0f9f345268c81", version : "66e6f12bd269691031eca3e1"); // chain/chainCommon.fs

/* Generated from sprocketProducts.py by `fs gen` -- DO NOT EDIT */

export const librarySprocketTable = {
        "name" : "vendor",
        "displayName" : "Vendor",
        "default" : "WCP",
        "entries" : {
            "WCP" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : {
                        "name" : "chainType",
                        "displayName" : "Chain type",
                        "default" : "#25",
                        "entries" : {
                            "#25" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "12T",
                                "entries" : {
                                    "12T" : { "bore" : "1/2\" Hex", "index" : 0, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "14T" : { "bore" : "1/2\" Hex", "index" : 1, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "16T" : { "bore" : "1/2\" Hex", "index" : 2, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "17T" : { "bore" : "1/2\" Hex", "index" : 3, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "18T" : { "bore" : "1/2\" Hex", "index" : 4, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "20T" : { "bore" : "1/2\" Hex", "index" : 5, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "22T" : { "bore" : "1/2\" Hex", "index" : 6, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "24T" : { "bore" : "1/2\" Hex", "index" : 7, "vendor" : "WCP", "vendorName" : "WCP" },
                                },
                            },
                            "#35" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "9T",
                                "entries" : {
                                    "9T" : { "bore" : "1/2\" Hex", "index" : 8, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "12T" : { "bore" : "1/2\" Hex", "index" : 9, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "15T" : { "bore" : "1/2\" Hex", "index" : 10, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "18T" : { "bore" : "1/2\" Hex", "index" : 11, "vendor" : "WCP", "vendorName" : "WCP" },
                                },
                            },
                        },
                    },
                    "8mm SplineXS" : {
                        "name" : "teeth",
                        "displayName" : "Teeth",
                        "default" : "10T",
                        "entries" : {
                            "10T" : { "bore" : "8mm SplineXS", "index" : 0, "vendor" : "WCP", "vendorName" : "WCP" },
                            "12T" : { "bore" : "8mm SplineXS", "index" : 1, "vendor" : "WCP", "vendorName" : "WCP" },
                        },
                    },
                    "Falcon Spline" : {
                        "name" : "teeth",
                        "displayName" : "Teeth",
                        "default" : "10T",
                        "entries" : {
                            "10T" : { "bore" : "Falcon Spline", "index" : 0, "vendor" : "WCP", "vendorName" : "WCP" },
                        },
                    },
                    "8mm CIM/NEO" : {
                        "name" : "teeth",
                        "displayName" : "Teeth",
                        "default" : "10T",
                        "entries" : {
                            "10T" : { "bore" : "8mm CIM/NEO", "index" : 0, "vendor" : "WCP", "vendorName" : "WCP" },
                        },
                    },
                    "SplineXL" : {
                        "name" : "chainType",
                        "displayName" : "Chain type",
                        "default" : "#25",
                        "entries" : {
                            "#25" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "24T",
                                "entries" : {
                                    "24T" : { "bore" : "SplineXL", "index" : 0, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "30T" : { "bore" : "SplineXL", "index" : 1, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "36T" : { "bore" : "SplineXL", "index" : 2, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "42T" : { "bore" : "SplineXL", "index" : 3, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "48T" : { "bore" : "SplineXL", "index" : 4, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "54T" : { "bore" : "SplineXL", "index" : 5, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "60T" : { "bore" : "SplineXL", "index" : 6, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "66T" : { "bore" : "SplineXL", "index" : 7, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "72T" : { "bore" : "SplineXL", "index" : 8, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "84T" : { "bore" : "SplineXL", "index" : 9, "vendor" : "WCP", "vendorName" : "WCP" },
                                },
                            },
                            "#35" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "24T",
                                "entries" : {
                                    "18T" : { "bore" : "SplineXL", "index" : 11, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "24T" : { "bore" : "SplineXL", "index" : 10, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "30T" : { "bore" : "SplineXL", "index" : 12, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "36T" : { "bore" : "SplineXL", "index" : 13, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "42T" : { "bore" : "SplineXL", "index" : 14, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "48T" : { "bore" : "SplineXL", "index" : 15, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "54T" : { "bore" : "SplineXL", "index" : 16, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "60T" : { "bore" : "SplineXL", "index" : 17, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "66T" : { "bore" : "SplineXL", "index" : 18, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "72T" : { "bore" : "SplineXL", "index" : 19, "vendor" : "WCP", "vendorName" : "WCP" },
                                    "84T" : { "bore" : "SplineXL", "index" : 20, "vendor" : "WCP", "vendorName" : "WCP" },
                                },
                            },
                        },
                    },
                },
            },
            "The Thrifty Bot" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : {
                        "name" : "chainType",
                        "displayName" : "Chain type",
                        "default" : "#25",
                        "entries" : {
                            "#25" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "14T",
                                "entries" : {
                                    "14T" : { "bore" : "1/2\" Hex", "index" : 0, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                    "22T" : { "bore" : "1/2\" Hex", "index" : 1, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                },
                            },
                            "#35" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "10T",
                                "entries" : {
                                    "10T" : { "bore" : "1/2\" Hex", "index" : 2, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                },
                            },
                        },
                    },
                    "8mm CIM/NEO" : {
                        "name" : "chainType",
                        "displayName" : "Chain type",
                        "default" : "#25",
                        "entries" : {
                            "#25" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "9T",
                                "entries" : {
                                    "9T" : { "bore" : "8mm CIM/NEO", "index" : 0, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                },
                            },
                            "#35" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "7T",
                                "entries" : {
                                    "7T" : { "bore" : "8mm CIM/NEO", "index" : 1, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                },
                            },
                        },
                    },
                    "3/8\" Hex" : {
                        "name" : "chainType",
                        "displayName" : "Chain type",
                        "default" : "#25",
                        "entries" : {
                            "#25" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "9T",
                                "entries" : {
                                    "9T" : { "bore" : "3/8\" Hex", "index" : 0, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                    "12T" : { "bore" : "3/8\" Hex", "index" : 1, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                },
                            },
                            "#35" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "9T",
                                "entries" : {
                                    "9T" : { "bore" : "3/8\" Hex", "index" : 2, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                },
                            },
                        },
                    },
                    "1.125\" Round" : {
                        "name" : "chainType",
                        "displayName" : "Chain type",
                        "default" : "#25",
                        "entries" : {
                            "#25" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "36T",
                                "entries" : {
                                    "36T" : { "bore" : "1.125\" Round", "index" : 0, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                    "48T" : { "bore" : "1.125\" Round", "index" : 1, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                    "60T" : { "bore" : "1.125\" Round", "index" : 2, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                    "72T" : { "bore" : "1.125\" Round", "index" : 3, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                    "84T" : { "bore" : "1.125\" Round", "index" : 4, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                },
                            },
                            "#35" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "36T",
                                "entries" : {
                                    "24T" : { "bore" : "1.125\" Round", "index" : 6, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                    "36T" : { "bore" : "1.125\" Round", "index" : 5, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                    "48T" : { "bore" : "1.125\" Round", "index" : 7, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                    "60T" : { "bore" : "1.125\" Round", "index" : 8, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                    "72T" : { "bore" : "1.125\" Round", "index" : 9, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                    "84T" : { "bore" : "1.125\" Round", "index" : 10, "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                                },
                            },
                        },
                    },
                },
            },
            "REV" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : {
                        "name" : "chainType",
                        "displayName" : "Chain type",
                        "default" : "#25",
                        "entries" : {
                            "#25" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "16T",
                                "entries" : {
                                    "12T" : { "bore" : "1/2\" Hex", "index" : 1, "vendor" : "REV", "vendorName" : "REV" },
                                    "16T" : { "bore" : "1/2\" Hex", "index" : 0, "vendor" : "REV", "vendorName" : "REV" },
                                    "24T" : { "bore" : "1/2\" Hex", "index" : 2, "vendor" : "REV", "vendorName" : "REV" },
                                    "32T" : { "bore" : "1/2\" Hex", "index" : 3, "vendor" : "REV", "vendorName" : "REV" },
                                },
                            },
                            "#35" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "9T",
                                "entries" : {
                                    "9T" : { "bore" : "1/2\" Hex", "index" : 4, "vendor" : "REV", "vendorName" : "REV" },
                                    "10T" : { "bore" : "1/2\" Hex", "index" : 5, "vendor" : "REV", "vendorName" : "REV" },
                                    "11T" : { "bore" : "1/2\" Hex", "index" : 6, "vendor" : "REV", "vendorName" : "REV" },
                                    "12T" : { "bore" : "1/2\" Hex", "index" : 7, "vendor" : "REV", "vendorName" : "REV" },
                                    "16T" : { "bore" : "1/2\" Hex", "index" : 8, "vendor" : "REV", "vendorName" : "REV" },
                                    "18T" : { "bore" : "1/2\" Hex", "index" : 9, "vendor" : "REV", "vendorName" : "REV" },
                                    "20T" : { "bore" : "1/2\" Hex", "index" : 10, "vendor" : "REV", "vendorName" : "REV" },
                                    "24T" : { "bore" : "1/2\" Hex", "index" : 11, "vendor" : "REV", "vendorName" : "REV" },
                                },
                            },
                        },
                    },
                    "MAXSpline" : {
                        "name" : "chainType",
                        "displayName" : "Chain type",
                        "default" : "#25",
                        "entries" : {
                            "#25" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "24T",
                                "entries" : {
                                    "24T" : { "bore" : "MAXSpline", "index" : 0, "vendor" : "REV", "vendorName" : "REV" },
                                    "32T" : { "bore" : "MAXSpline", "index" : 1, "vendor" : "REV", "vendorName" : "REV" },
                                    "40T" : { "bore" : "MAXSpline", "index" : 2, "vendor" : "REV", "vendorName" : "REV" },
                                    "48T" : { "bore" : "MAXSpline", "index" : 3, "vendor" : "REV", "vendorName" : "REV" },
                                    "56T" : { "bore" : "MAXSpline", "index" : 4, "vendor" : "REV", "vendorName" : "REV" },
                                    "64T" : { "bore" : "MAXSpline", "index" : 5, "vendor" : "REV", "vendorName" : "REV" },
                                    "72T" : { "bore" : "MAXSpline", "index" : 6, "vendor" : "REV", "vendorName" : "REV" },
                                },
                            },
                            "#35" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "16T",
                                "entries" : {
                                    "16T" : { "bore" : "MAXSpline", "index" : 7, "vendor" : "REV", "vendorName" : "REV" },
                                    "18T" : { "bore" : "MAXSpline", "index" : 8, "vendor" : "REV", "vendorName" : "REV" },
                                    "20T" : { "bore" : "MAXSpline", "index" : 9, "vendor" : "REV", "vendorName" : "REV" },
                                    "24T" : { "bore" : "MAXSpline", "index" : 10, "vendor" : "REV", "vendorName" : "REV" },
                                    "28T" : { "bore" : "MAXSpline", "index" : 11, "vendor" : "REV", "vendorName" : "REV" },
                                    "32T" : { "bore" : "MAXSpline", "index" : 12, "vendor" : "REV", "vendorName" : "REV" },
                                    "36T" : { "bore" : "MAXSpline", "index" : 13, "vendor" : "REV", "vendorName" : "REV" },
                                    "40T" : { "bore" : "MAXSpline", "index" : 14, "vendor" : "REV", "vendorName" : "REV" },
                                    "44T" : { "bore" : "MAXSpline", "index" : 15, "vendor" : "REV", "vendorName" : "REV" },
                                    "48T" : { "bore" : "MAXSpline", "index" : 16, "vendor" : "REV", "vendorName" : "REV" },
                                    "52T" : { "bore" : "MAXSpline", "index" : 17, "vendor" : "REV", "vendorName" : "REV" },
                                    "56T" : { "bore" : "MAXSpline", "index" : 18, "vendor" : "REV", "vendorName" : "REV" },
                                    "60T" : { "bore" : "MAXSpline", "index" : 19, "vendor" : "REV", "vendorName" : "REV" },
                                    "64T" : { "bore" : "MAXSpline", "index" : 20, "vendor" : "REV", "vendorName" : "REV" },
                                    "68T" : { "bore" : "MAXSpline", "index" : 21, "vendor" : "REV", "vendorName" : "REV" },
                                    "72T" : { "bore" : "MAXSpline", "index" : 22, "vendor" : "REV", "vendorName" : "REV" },
                                    "76T" : { "bore" : "MAXSpline", "index" : 23, "vendor" : "REV", "vendorName" : "REV" },
                                    "80T" : { "bore" : "MAXSpline", "index" : 24, "vendor" : "REV", "vendorName" : "REV" },
                                },
                            },
                        },
                    },
                },
            },
            "AndyMark" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : {
                        "name" : "chainType",
                        "displayName" : "Chain type",
                        "default" : "#25",
                        "entries" : {
                            "#25" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "14T",
                                "entries" : {
                                    "14T" : { "bore" : "1/2\" Hex", "index" : 0, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "18T" : { "bore" : "1/2\" Hex", "index" : 1, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "22T" : { "bore" : "1/2\" Hex", "index" : 2, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "26T" : { "bore" : "1/2\" Hex", "index" : 3, "vendor" : "AM", "vendorName" : "AndyMark" },
                                },
                            },
                            "#35" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "10T",
                                "entries" : {
                                    "10T" : { "bore" : "1/2\" Hex", "index" : 4, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "14T" : { "bore" : "1/2\" Hex", "index" : 5, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "18T" : { "bore" : "1/2\" Hex", "index" : 6, "vendor" : "AM", "vendorName" : "AndyMark" },
                                },
                            },
                        },
                    },
                    "8mm CIM/NEO" : {
                        "name" : "teeth",
                        "displayName" : "Teeth",
                        "default" : "10T",
                        "entries" : {
                            "10T" : { "bore" : "8mm CIM/NEO", "index" : 0, "vendor" : "AM", "vendorName" : "AndyMark" },
                        },
                    },
                    "3/8\" Hex" : {
                        "name" : "teeth",
                        "displayName" : "Teeth",
                        "default" : "18T",
                        "entries" : {
                            "14T" : { "bore" : "3/8\" Hex", "index" : 1, "vendor" : "AM", "vendorName" : "AndyMark" },
                            "18T" : { "bore" : "3/8\" Hex", "index" : 0, "vendor" : "AM", "vendorName" : "AndyMark" },
                            "22T" : { "bore" : "3/8\" Hex", "index" : 2, "vendor" : "AM", "vendorName" : "AndyMark" },
                            "24T" : { "bore" : "3/8\" Hex", "index" : 3, "vendor" : "AM", "vendorName" : "AndyMark" },
                            "26T" : { "bore" : "3/8\" Hex", "index" : 4, "vendor" : "AM", "vendorName" : "AndyMark" },
                        },
                    },
                    "1.125\" Round" : {
                        "name" : "chainType",
                        "displayName" : "Chain type",
                        "default" : "#25",
                        "entries" : {
                            "#25" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "38T",
                                "entries" : {
                                    "32T" : { "bore" : "1.125\" Round", "index" : 1, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "38T" : { "bore" : "1.125\" Round", "index" : 0, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "44T" : { "bore" : "1.125\" Round", "index" : 2, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "50T" : { "bore" : "1.125\" Round", "index" : 3, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "56T" : { "bore" : "1.125\" Round", "index" : 4, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "62T" : { "bore" : "1.125\" Round", "index" : 5, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "66T" : { "bore" : "1.125\" Round", "index" : 6, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "68T" : { "bore" : "1.125\" Round", "index" : 7, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "74T" : { "bore" : "1.125\" Round", "index" : 8, "vendor" : "AM", "vendorName" : "AndyMark" },
                                },
                            },
                            "#35" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "28T",
                                "entries" : {
                                    "22T" : { "bore" : "1.125\" Round", "index" : 10, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "24T" : { "bore" : "1.125\" Round", "index" : 11, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "28T" : { "bore" : "1.125\" Round", "index" : 9, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "34T" : { "bore" : "1.125\" Round", "index" : 12, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "40T" : { "bore" : "1.125\" Round", "index" : 13, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "52T" : { "bore" : "1.125\" Round", "index" : 15, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "56T" : { "bore" : "1.125\" Round", "index" : 14, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "58T" : { "bore" : "1.125\" Round", "index" : 16, "vendor" : "AM", "vendorName" : "AndyMark" },
                                    "64T" : { "bore" : "1.125\" Round", "index" : 17, "vendor" : "AM", "vendorName" : "AndyMark" },
                                },
                            },
                        },
                    },
                },
            },
            "VEX" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : {
                        "name" : "chainType",
                        "displayName" : "Chain type",
                        "default" : "#25",
                        "entries" : {
                            "#25" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "16T",
                                "entries" : {
                                    "16T" : { "bore" : "1/2\" Hex", "index" : 0, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "18T" : { "bore" : "1/2\" Hex", "index" : 1, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "22T" : { "bore" : "1/2\" Hex", "index" : 2, "vendor" : "VEX", "vendorName" : "VEX" },
                                },
                            },
                            "#35" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "12T",
                                "entries" : {
                                    "12T" : { "bore" : "1/2\" Hex", "index" : 3, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "15T" : { "bore" : "1/2\" Hex", "index" : 4, "vendor" : "VEX", "vendorName" : "VEX" },
                                },
                            },
                        },
                    },
                    "3/8\" Hex" : {
                        "name" : "teeth",
                        "displayName" : "Teeth",
                        "default" : "16T",
                        "entries" : {
                            "16T" : { "bore" : "3/8\" Hex", "index" : 0, "vendor" : "VEX", "vendorName" : "VEX" },
                            "22T" : { "bore" : "3/8\" Hex", "index" : 1, "vendor" : "VEX", "vendorName" : "VEX" },
                        },
                    },
                    "1.125\" Round" : {
                        "name" : "chainType",
                        "displayName" : "Chain type",
                        "default" : "#25",
                        "entries" : {
                            "#25" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "32T",
                                "entries" : {
                                    "32T" : { "bore" : "1.125\" Round", "index" : 0, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "34T" : { "bore" : "1.125\" Round", "index" : 1, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "36T" : { "bore" : "1.125\" Round", "index" : 2, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "38T" : { "bore" : "1.125\" Round", "index" : 3, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "40T" : { "bore" : "1.125\" Round", "index" : 4, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "42T" : { "bore" : "1.125\" Round", "index" : 5, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "44T" : { "bore" : "1.125\" Round", "index" : 6, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "48T" : { "bore" : "1.125\" Round", "index" : 7, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "54T" : { "bore" : "1.125\" Round", "index" : 8, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "58T" : { "bore" : "1.125\" Round", "index" : 9, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "60T" : { "bore" : "1.125\" Round", "index" : 10, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "64T" : { "bore" : "1.125\" Round", "index" : 11, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "72T" : { "bore" : "1.125\" Round", "index" : 12, "vendor" : "VEX", "vendorName" : "VEX" },
                                },
                            },
                            "#35" : {
                                "name" : "teeth",
                                "displayName" : "Teeth",
                                "default" : "28T",
                                "entries" : {
                                    "22T" : { "bore" : "1.125\" Round", "index" : 14, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "24T" : { "bore" : "1.125\" Round", "index" : 15, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "26T" : { "bore" : "1.125\" Round", "index" : 16, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "28T" : { "bore" : "1.125\" Round", "index" : 13, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "30T" : { "bore" : "1.125\" Round", "index" : 17, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "32T" : { "bore" : "1.125\" Round", "index" : 18, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "36T" : { "bore" : "1.125\" Round", "index" : 19, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "42T" : { "bore" : "1.125\" Round", "index" : 20, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "44T" : { "bore" : "1.125\" Round", "index" : 21, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "54T" : { "bore" : "1.125\" Round", "index" : 22, "vendor" : "VEX", "vendorName" : "VEX" },
                                    "60T" : { "bore" : "1.125\" Round", "index" : 23, "vendor" : "VEX", "vendorName" : "VEX" },
                                },
                            },
                        },
                    },
                },
            },
        },
    };

export const sprocketProductTable = {
        "name" : "vendor",
        "displayName" : "Vendor",
        "default" : "WCP",
        "entries" : {
            "WCP" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : { "bore" : "1/2\" Hex", "vendor" : "WCP", "vendorName" : "WCP" },
                    "8mm SplineXS" : { "bore" : "8mm SplineXS", "vendor" : "WCP", "vendorName" : "WCP" },
                    "Falcon Spline" : { "bore" : "Falcon Spline", "vendor" : "WCP", "vendorName" : "WCP" },
                    "8mm CIM/NEO" : { "bore" : "8mm CIM/NEO", "vendor" : "WCP", "vendorName" : "WCP" },
                    "SplineXL" : { "bore" : "SplineXL", "vendor" : "WCP", "vendorName" : "WCP" },
                },
            },
            "The Thrifty Bot" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : { "bore" : "1/2\" Hex", "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                    "8mm CIM/NEO" : { "bore" : "8mm CIM/NEO", "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                    "3/8\" Hex" : { "bore" : "3/8\" Hex", "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                    "1.125\" Round" : { "bore" : "1.125\" Round", "vendor" : "TTB", "vendorName" : "The Thrifty Bot" },
                },
            },
            "REV" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : { "bore" : "1/2\" Hex", "vendor" : "REV", "vendorName" : "REV" },
                    "MAXSpline" : { "bore" : "MAXSpline", "vendor" : "REV", "vendorName" : "REV" },
                },
            },
            "AndyMark" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : { "bore" : "1/2\" Hex", "vendor" : "AM", "vendorName" : "AndyMark" },
                    "8mm CIM/NEO" : { "bore" : "8mm CIM/NEO", "vendor" : "AM", "vendorName" : "AndyMark" },
                    "3/8\" Hex" : { "bore" : "3/8\" Hex", "vendor" : "AM", "vendorName" : "AndyMark" },
                    "1.125\" Round" : { "bore" : "1.125\" Round", "vendor" : "AM", "vendorName" : "AndyMark" },
                },
            },
            "VEX" : {
                "name" : "bore",
                "displayName" : "Bore",
                "default" : "1/2\" Hex",
                "entries" : {
                    "1/2\" Hex" : { "bore" : "1/2\" Hex", "vendor" : "VEX", "vendorName" : "VEX" },
                    "3/8\" Hex" : { "bore" : "3/8\" Hex", "vendor" : "VEX", "vendorName" : "VEX" },
                    "1.125\" Round" : { "bore" : "1.125\" Round", "vendor" : "VEX", "vendorName" : "VEX" },
                },
            },
        },
    };

export const SPROCKET_PRODUCTS = {
        "WCP" : {
            "1/2\" Hex" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 12, "name" : "12t Steel Sprocket (#25, 1/2\" Rounded Hex Bore, Double Hub)", "partNumber" : "WCP-0576", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "69c145cf4f87758547276957", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 14, "name" : "14t Steel Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "WCP-0577", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "69c145cf4f87758547276957", "configuration" : { "List_miHamtgOhdzyeP" : "_14t_Steel" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 16, "name" : "16t Aluminum Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "WCP-0578", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "69c145cf4f87758547276957", "configuration" : { "List_miHamtgOhdzyeP" : "_16t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 17, "name" : "17t Aluminum Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "WCP-0579", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "69c145cf4f87758547276957", "configuration" : { "List_miHamtgOhdzyeP" : "_17t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 18, "name" : "18t Aluminum Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "WCP-0559", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "69c145cf4f87758547276957", "configuration" : { "List_miHamtgOhdzyeP" : "_18t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 20, "name" : "20t Aluminum Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "WCP-0580", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "69c145cf4f87758547276957", "configuration" : { "List_miHamtgOhdzyeP" : "_20t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 22, "name" : "22t Aluminum Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "WCP-0560", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "69c145cf4f87758547276957", "configuration" : { "List_miHamtgOhdzyeP" : "_22t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 24, "name" : "24t Aluminum Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "WCP-0581", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "69c145cf4f87758547276957", "configuration" : { "List_miHamtgOhdzyeP" : "_24t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 9, "name" : "9t Steel Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "WCP-0976", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "766515b89adb4dfc82d0291a", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 12, "name" : "12t Steel Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "WCP-0977", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "766515b89adb4dfc82d0291a", "configuration" : { "List_miHamtgOhdzyeP" : "_14t_Steel" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 15, "name" : "15t Steel Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "WCP-0978", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "766515b89adb4dfc82d0291a", "configuration" : { "List_miHamtgOhdzyeP" : "_16t_Aluminum_Double" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 18, "name" : "18t Steel Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "WCP-0979", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "766515b89adb4dfc82d0291a", "configuration" : { "List_miHamtgOhdzyeP" : "_17t_Aluminum" }, "boreDiameter" : 0.5774 * inch }
            ],
            "8mm SplineXS" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 10, "name" : "10t Steel Sprocket (#25, 8mm SplineXS Bore, Double Hub)", "partNumber" : "WCP-1019", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "86132887aae7ab052abddfd6", "configuration" : { "List_35EuQ04lYuL02s" : "Default" }, "boreDiameter" : 8 * millimeter },
                { "chainType" : ChainType.ANSI_25, "teeth" : 12, "name" : "12t Steel Sprocket (#25, 8mm SplineXS Bore, Double Hub)", "partNumber" : "WCP-1020", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "86132887aae7ab052abddfd6", "configuration" : { "List_35EuQ04lYuL02s" : "_12T_Steel__Double_Hub_" }, "boreDiameter" : 8 * millimeter }
            ],
            "Falcon Spline" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 10, "name" : "10t Steel Sprocket (#25, Falcon Spline Bore, Double Hub)", "partNumber" : "WCP-0582", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "90dc911bf44692d26b6a036a", "configuration" : { "List_35EuQ04lYuL02s" : "Default" }, "boreDiameter" : 8 * millimeter }
            ],
            "8mm CIM/NEO" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 10, "name" : "10t Steel Sprocket (#25, 8mm CIM/NEO Bore, Double Hub)", "partNumber" : "WCP-1419", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "61d8ecb9d30637ff923f392e", "configuration" : { "List_35EuQ04lYuL02s" : "Default" }, "boreDiameter" : 8 * millimeter }
            ],
            "SplineXL" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 24, "name" : "24t Aluminum Sprocket (#25, SplineXL Bore)", "partNumber" : "WCP-0964", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "530ab1b8ab63a10aa494e3ab", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 30, "name" : "30t Aluminum Sprocket (#25, SplineXL Bore)", "partNumber" : "WCP-0965", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "530ab1b8ab63a10aa494e3ab", "configuration" : { "List_miHamtgOhdzyeP" : "_14t_Steel" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 36, "name" : "36t Aluminum Sprocket (#25, SplineXL Bore)", "partNumber" : "WCP-0966", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "530ab1b8ab63a10aa494e3ab", "configuration" : { "List_miHamtgOhdzyeP" : "_16t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 42, "name" : "42t Aluminum Sprocket (#25, SplineXL Bore)", "partNumber" : "WCP-0967", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "530ab1b8ab63a10aa494e3ab", "configuration" : { "List_miHamtgOhdzyeP" : "_42t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 48, "name" : "48t Aluminum Sprocket (#25, SplineXL Bore)", "partNumber" : "WCP-0968", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "530ab1b8ab63a10aa494e3ab", "configuration" : { "List_miHamtgOhdzyeP" : "_48t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 54, "name" : "54t Aluminum Sprocket (#25, SplineXL Bore)", "partNumber" : "WCP-0969", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "530ab1b8ab63a10aa494e3ab", "configuration" : { "List_miHamtgOhdzyeP" : "_54t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 60, "name" : "60t Aluminum Sprocket (#25, SplineXL Bore)", "partNumber" : "WCP-0970", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "530ab1b8ab63a10aa494e3ab", "configuration" : { "List_miHamtgOhdzyeP" : "_60t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 66, "name" : "66t Aluminum Sprocket (#25, SplineXL Bore)", "partNumber" : "WCP-0971", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "530ab1b8ab63a10aa494e3ab", "configuration" : { "List_miHamtgOhdzyeP" : "_66t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 72, "name" : "72t Aluminum Sprocket (#25, SplineXL Bore)", "partNumber" : "WCP-0972", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "530ab1b8ab63a10aa494e3ab", "configuration" : { "List_miHamtgOhdzyeP" : "_72t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 84, "name" : "84t Aluminum Sprocket (#25, SplineXL Bore)", "partNumber" : "WCP-1376", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "530ab1b8ab63a10aa494e3ab", "configuration" : { "List_miHamtgOhdzyeP" : "_84t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 24, "name" : "24t Aluminum Sprocket (#35, SplineXL Bore)", "partNumber" : "WCP-0981", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "de502fd70fec3871c45417ce", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 18, "name" : "18t Aluminum Sprocket (#35, SplineXL Bore)", "partNumber" : "WCP-0980", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "de502fd70fec3871c45417ce", "configuration" : { "List_miHamtgOhdzyeP" : "Copy_of_24t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 30, "name" : "30t Aluminum Sprocket (#35, SplineXL Bore)", "partNumber" : "WCP-0982", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "de502fd70fec3871c45417ce", "configuration" : { "List_miHamtgOhdzyeP" : "_14t_Steel" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 36, "name" : "36t Aluminum Sprocket (#35, SplineXL Bore)", "partNumber" : "WCP-0983", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "de502fd70fec3871c45417ce", "configuration" : { "List_miHamtgOhdzyeP" : "_16t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 42, "name" : "42t Aluminum Sprocket (#35, SplineXL Bore)", "partNumber" : "WCP-0984", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "de502fd70fec3871c45417ce", "configuration" : { "List_miHamtgOhdzyeP" : "_42t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 48, "name" : "48t Aluminum Sprocket (#35, SplineXL Bore)", "partNumber" : "WCP-0985", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "de502fd70fec3871c45417ce", "configuration" : { "List_miHamtgOhdzyeP" : "_48t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 54, "name" : "54t Aluminum Sprocket (#35, SplineXL Bore)", "partNumber" : "WCP-0986", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "de502fd70fec3871c45417ce", "configuration" : { "List_miHamtgOhdzyeP" : "_54t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 60, "name" : "60t Aluminum Sprocket (#35, SplineXL Bore)", "partNumber" : "WCP-0987", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "de502fd70fec3871c45417ce", "configuration" : { "List_miHamtgOhdzyeP" : "_60t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 66, "name" : "66t Aluminum Sprocket (#35, SplineXL Bore)", "partNumber" : "WCP-0988", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "de502fd70fec3871c45417ce", "configuration" : { "List_miHamtgOhdzyeP" : "_66t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 72, "name" : "72t Aluminum Sprocket (#35, SplineXL Bore)", "partNumber" : "WCP-0989", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "de502fd70fec3871c45417ce", "configuration" : { "List_miHamtgOhdzyeP" : "_72t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 84, "name" : "84t Aluminum Sprocket (#35, SplineXL Bore)", "partNumber" : "WCP-1376", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "de502fd70fec3871c45417ce", "configuration" : { "List_miHamtgOhdzyeP" : "_84t_Aluminum" }, "boreDiameter" : 1.375 * inch }
            ]
        },
        "TTB" : {
            "1/2\" Hex" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 14, "name" : "14t Steel Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "TTB-0253", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "ba1f2f46eaf2ee8879a0c3e6", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 22, "name" : "22t Aluminum Sprocket (#25, 1/2\" Hex Bore, Single Hub)", "partNumber" : "ELEV-DRIVE-003", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "ba1f2f46eaf2ee8879a0c3e6", "configuration" : { "List_miHamtgOhdzyeP" : "_20t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 10, "name" : "10t Steel Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "TTB-0257", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "dcc120db65e6d812533added", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 0.5774 * inch }
            ],
            "8mm CIM/NEO" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 9, "name" : "9t Steel Sprocket (#25, 8mm CIM/NEO Bore, Double Hub)", "partNumber" : "TTB-0251", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "61d8ecb9d30637ff923f392e", "configuration" : { "List_35EuQ04lYuL02s" : "_9t_Steel__TTB_" }, "boreDiameter" : 8 * millimeter },
                { "chainType" : ChainType.ANSI_35, "teeth" : 7, "name" : "7t Steel Sprocket (#35, 8mm CIM/NEO Bore, Double Hub)", "partNumber" : "TTB-0256", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "eef943cb2aee872efed32ebd", "configuration" : { "List_35EuQ04lYuL02s" : "_9t_Steel__TTB_" }, "boreDiameter" : 8 * millimeter }
            ],
            "3/8\" Hex" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 9, "name" : "9t Steel Sprocket (#25, 3/8\" Hex Bore, Double Hub)", "partNumber" : "TTB-0250", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "0bc142783619d42368199384", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 0.433 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 12, "name" : "12t Steel Sprocket (#25, 3/8\" Hex Bore, Double Hub)", "partNumber" : "TTB-0252", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "0bc142783619d42368199384", "configuration" : { "List_miHamtgOhdzyeP" : "_18t_Aluminum_Double" }, "boreDiameter" : 0.433 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 9, "name" : "9t Steel Sprocket (#35, 3/8\" Hex Bore, Double Hub)", "partNumber" : "TTB-0255", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "f27a8ad485020bbb3e6148de", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 0.433 * inch }
            ],
            "1.125\" Round" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 36, "name" : "36t Aluminum Billet Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "TTB-0248", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "fef9a4f4d997523086cbad2e", "configuration" : { "List_p0IV4hyyRmgk0X" : "Default" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 48, "name" : "48t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "TTB-0275", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "fef9a4f4d997523086cbad2e", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_36t_Aluminum__Plate_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 60, "name" : "60t Aluminum Billet Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "TTB-0249", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "fef9a4f4d997523086cbad2e", "configuration" : { "List_p0IV4hyyRmgk0X" : "_36t_Aluminum__Billet_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 72, "name" : "72t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "TTB-0277", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "fef9a4f4d997523086cbad2e", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_60t_Aluminum__Plate_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 84, "name" : "84t Aluminum Billet Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "TTB-0285", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "fef9a4f4d997523086cbad2e", "configuration" : { "List_p0IV4hyyRmgk0X" : "_36t_Aluminum__Billet_0" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 36, "name" : "36t Aluminum Billet Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "TTB-0245", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "3601466dbcf87aba1b0ae7b3", "configuration" : { "List_p0IV4hyyRmgk0X" : "Default" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 24, "name" : "24t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "TTB-0279", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "3601466dbcf87aba1b0ae7b3", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_36t_Aluminum_Billet" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 48, "name" : "48t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "TTB-0281", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "3601466dbcf87aba1b0ae7b3", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_36t_Aluminum__Plate_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 60, "name" : "60t Aluminum Billet Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "TTB-0246", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "3601466dbcf87aba1b0ae7b3", "configuration" : { "List_p0IV4hyyRmgk0X" : "_36t_Aluminum__Billet_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 72, "name" : "72t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "TTB-0282", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "3601466dbcf87aba1b0ae7b3", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_60t_Aluminum__Plate_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 84, "name" : "84t Aluminum Billet Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "TTB-0247", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "3601466dbcf87aba1b0ae7b3", "configuration" : { "List_p0IV4hyyRmgk0X" : "_36t_Aluminum__Billet_0" }, "boreDiameter" : 1.125 * inch }
            ]
        },
        "REV" : {
            "1/2\" Hex" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 16, "name" : "16t Steel Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "REV-21-2016", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "b11af2af7ab3ecd04a9adb1a", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 12, "name" : "12t Steel Sprocket (#25, 1/2\" Rounded Hex Bore, Double Hub)", "partNumber" : "REV-21-2014", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "b11af2af7ab3ecd04a9adb1a", "configuration" : { "List_miHamtgOhdzyeP" : "_12t" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 24, "name" : "24t Aluminum Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "REV-21-2017", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "b11af2af7ab3ecd04a9adb1a", "configuration" : { "List_miHamtgOhdzyeP" : "_16t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 32, "name" : "32t Aluminum Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "REV-21-2018", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "b11af2af7ab3ecd04a9adb1a", "configuration" : { "List_miHamtgOhdzyeP" : "_16t_Aluminum_Double" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 9, "name" : "9t Steel Sprocket (#35, 1/2\" Rounded Hex Bore, Double Hub)", "partNumber" : "REV-21-3706", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "fb97615572be6b67d1e1eaa3", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 10, "name" : "10t Steel Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "REV-21-3707", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "fb97615572be6b67d1e1eaa3", "configuration" : { "List_miHamtgOhdzyeP" : "_14t_Steel" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 11, "name" : "11t Steel Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "REV-21-3708", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "fb97615572be6b67d1e1eaa3", "configuration" : { "List_miHamtgOhdzyeP" : "_16t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 12, "name" : "12t Steel Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "REV-21-3709", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "fb97615572be6b67d1e1eaa3", "configuration" : { "List_miHamtgOhdzyeP" : "_16t_Aluminum_Double" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 16, "name" : "16t Aluminum Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "REV-21-3710", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "fb97615572be6b67d1e1eaa3", "configuration" : { "List_miHamtgOhdzyeP" : "_16t_Steel" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 18, "name" : "18t Aluminum Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "REV-21-3711", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "fb97615572be6b67d1e1eaa3", "configuration" : { "List_miHamtgOhdzyeP" : "_17t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 20, "name" : "20t Aluminum Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "REV-21-3712", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "fb97615572be6b67d1e1eaa3", "configuration" : { "List_miHamtgOhdzyeP" : "_18t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 24, "name" : "24t Aluminum Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "REV-21-3713", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "fb97615572be6b67d1e1eaa3", "configuration" : { "List_miHamtgOhdzyeP" : "_24t_Aluminum" }, "boreDiameter" : 0.5774 * inch }
            ],
            "MAXSpline" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 24, "name" : "24t Aluminum Sprocket (#25, MAXSpline Bore, Double Hub)", "partNumber" : "REV-21-2015", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "7598650e2d59d03c0911badd", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 32, "name" : "32t Aluminum Sprocket (#25, MAXSpline Bore, Double Hub)", "partNumber" : "REV-21-2019", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "7598650e2d59d03c0911badd", "configuration" : { "List_miHamtgOhdzyeP" : "_14t_Steel" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 40, "name" : "40t Aluminum Billet Sprocket (#25, MAXSpline Bore)", "partNumber" : "REV-21-3370", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "7598650e2d59d03c0911badd", "configuration" : { "List_miHamtgOhdzyeP" : "_48t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 48, "name" : "48t Aluminum Plate Sprocket (#25, MAXSpline Bore)", "partNumber" : "REV-21-1964", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "7598650e2d59d03c0911badd", "configuration" : { "List_miHamtgOhdzyeP" : "_16t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 56, "name" : "56t Aluminum Billet Sprocket (#25, MAXSpline Bore)", "partNumber" : "REV-21-3378", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "7598650e2d59d03c0911badd", "configuration" : { "List_miHamtgOhdzyeP" : "_60t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 64, "name" : "64t Aluminum Plate Sprocket (#25, MAXSpline Bore)", "partNumber" : "REV-21-1972", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "7598650e2d59d03c0911badd", "configuration" : { "List_miHamtgOhdzyeP" : "_42t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 72, "name" : "72t Aluminum Billet Sprocket (#25, MAXSpline Bore)", "partNumber" : "REV-21-3386", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "7598650e2d59d03c0911badd", "configuration" : { "List_miHamtgOhdzyeP" : "_72t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 16, "name" : "16t Aluminum Sprocket (#35, MAXSpline Bore, Double Hub)", "partNumber" : "REV-21-3714", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 18, "name" : "18t Aluminum Sprocket (#35, MAXSpline Bore, Double Hub)", "partNumber" : "REV-21-3715", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_14t_Steel" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 20, "name" : "20t Aluminum Sprocket (#35, MAXSpline Bore, Double Hub)", "partNumber" : "REV-21-3716", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_16t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 24, "name" : "24t Aluminum Sprocket (#35, MAXSpline Bore, Double Hub)", "partNumber" : "REV-21-3717", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_42t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 28, "name" : "28t Aluminum Billet Sprocket (#35, MAXSpline Bore)", "partNumber" : "REV-21-3718", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_48t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 32, "name" : "32t Aluminum Billet Sprocket (#35, MAXSpline Bore)", "partNumber" : "REV-21-3719", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_54t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 36, "name" : "36t Aluminum Billet Sprocket (#35, MAXSpline Bore)", "partNumber" : "REV-21-3720", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_60t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 40, "name" : "40t Aluminum Billet Sprocket (#35, MAXSpline Bore)", "partNumber" : "REV-21-3721", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_66t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 44, "name" : "44t Aluminum Billet Sprocket (#35, MAXSpline Bore)", "partNumber" : "REV-21-3722", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_72t_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 48, "name" : "48t Aluminum Billet Sprocket (#35, MAXSpline Bore)", "partNumber" : "REV-21-3723", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_48t_Billet_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 52, "name" : "52t Aluminum Billet Sprocket (#35, MAXSpline Bore)", "partNumber" : "REV-21-3724", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_52t_Billet_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 56, "name" : "56t Aluminum Billet Sprocket (#35, MAXSpline Bore)", "partNumber" : "REV-21-3725", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_56t_Billet_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 60, "name" : "60t Aluminum Billet Sprocket (#35, MAXSpline Bore)", "partNumber" : "REV-21-3726", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_60t_Billet_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 64, "name" : "64t Aluminum Billet Sprocket (#35, MAXSpline Bore)", "partNumber" : "REV-21-3727", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_64t_Billet_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 68, "name" : "68t Aluminum Billet Sprocket (#35, MAXSpline Bore)", "partNumber" : "REV-21-3728", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_68t_Billet_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 72, "name" : "72t Aluminum Billet Sprocket (#35, MAXSpline Bore)", "partNumber" : "REV-21-3729", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_72t_Billet_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 76, "name" : "76t Aluminum Billet Sprocket (#35, MAXSpline Bore)", "partNumber" : "REV-21-3730", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_76t_Billet_Aluminum" }, "boreDiameter" : 1.375 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 80, "name" : "80t Aluminum Billet Sprocket (#35, MAXSpline Bore)", "partNumber" : "REV-21-3731", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "072494e1435e9ebbb4e52ce3", "configuration" : { "List_miHamtgOhdzyeP" : "_80t_Billet_Aluminum" }, "boreDiameter" : 1.375 * inch }
            ]
        },
        "AM" : {
            "1/2\" Hex" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 14, "name" : "14t Aluminum Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "am-4773", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "895bb150a7a0f64099c4b545", "configuration" : { "List_sbUIFfzvk3H7Y3" : "Default" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 18, "name" : "18t Aluminum Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "am-4775", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "895bb150a7a0f64099c4b545", "configuration" : { "List_sbUIFfzvk3H7Y3" : "_18t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 22, "name" : "22t Aluminum Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "am-4777", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "895bb150a7a0f64099c4b545", "configuration" : { "List_sbUIFfzvk3H7Y3" : "_22t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 26, "name" : "26t Aluminum Sprocket (#25, 1/2\" Hex Bore, Double Hub)", "partNumber" : "am-4779", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "895bb150a7a0f64099c4b545", "configuration" : { "List_sbUIFfzvk3H7Y3" : "_26t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 10, "name" : "10t Steel Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "am-4789", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "594d1877c7d37b8d55a2e156", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 14, "name" : "14t Steel Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "am-4790", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "594d1877c7d37b8d55a2e156", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel_Double" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 18, "name" : "18t Steel Sprocket (#35, 1/2\" Hex Bore, Double Hub)", "partNumber" : "am-4791", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "594d1877c7d37b8d55a2e156", "configuration" : { "List_miHamtgOhdzyeP" : "_18t_Steel" }, "boreDiameter" : 0.5774 * inch }
            ],
            "8mm CIM/NEO" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 10, "name" : "10t Aluminum Sprocket (#25, 8mm CIM/NEO Bore, Double Hub)", "partNumber" : "am-4772", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "61d8ecb9d30637ff923f392e", "configuration" : { "List_35EuQ04lYuL02s" : "_10t_Aluminum__Double_Hub___AM_" }, "boreDiameter" : 8 * millimeter }
            ],
            "3/8\" Hex" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 18, "name" : "18t Aluminum Sprocket (#25, 3/8\" Hex Bore, Double Hub)", "partNumber" : "am-4776", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "1778fec36561d38156871838", "configuration" : { "List_sbUIFfzvk3H7Y3" : "_18t_Aluminum" }, "boreDiameter" : 0.433 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 14, "name" : "14t Aluminum Sprocket (#25, 3/8\" Hex Bore, Double Hub)", "partNumber" : "am-4774", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "1778fec36561d38156871838", "configuration" : { "List_sbUIFfzvk3H7Y3" : "Default" }, "boreDiameter" : 0.433 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 22, "name" : "22t Aluminum Sprocket (#25, 3/8\" Hex Bore, Double Hub)", "partNumber" : "am-4778", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "1778fec36561d38156871838", "configuration" : { "List_sbUIFfzvk3H7Y3" : "_22t_Aluminum" }, "boreDiameter" : 0.433 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 24, "name" : "24t Aluminum Sprocket (#25, 3/8\" Hex Bore, Single Hub)", "partNumber" : "am-3630", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "1778fec36561d38156871838", "configuration" : { "List_sbUIFfzvk3H7Y3" : "_24t_Aluminum__Single_Hub_" }, "boreDiameter" : 0.433 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 26, "name" : "26t Aluminum Sprocket (#25, 3/8\" Hex Bore, Double Hub)", "partNumber" : "am-4780", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "1778fec36561d38156871838", "configuration" : { "List_sbUIFfzvk3H7Y3" : "_26t_Aluminum" }, "boreDiameter" : 0.433 * inch }
            ],
            "1.125\" Round" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 38, "name" : "38t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "am-4782", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "57d128844fdaaed1019d9824", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_36t_Aluminum__Billet_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 32, "name" : "32t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "am-4781", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "57d128844fdaaed1019d9824", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_32t_Aluminum_Plate__Legacy_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 44, "name" : "44t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "am-4783", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "57d128844fdaaed1019d9824", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_36t_Aluminum__Plate_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 50, "name" : "50t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "am-4784", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "57d128844fdaaed1019d9824", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_60t_Aluminum__Billet_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 56, "name" : "56t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "am-4785", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "57d128844fdaaed1019d9824", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_60t_Aluminum__Plate_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 62, "name" : "62t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "am-4786", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "57d128844fdaaed1019d9824", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_84t_Aluminum__Billet_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 66, "name" : "66t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "am-0238", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "57d128844fdaaed1019d9824", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_62t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 68, "name" : "68t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "am-4787", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "57d128844fdaaed1019d9824", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_Copy_of_62t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 74, "name" : "74t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "am-4788", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "57d128844fdaaed1019d9824", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_68t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 28, "name" : "28t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "am-4793", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "ffa08cfb2b0209bb4b843d82", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_36t_Aluminum__Billet_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 22, "name" : "22t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "am-4792", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "ffa08cfb2b0209bb4b843d82", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_32t_Aluminum_Plate__Legacy_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 24, "name" : "24t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "am-0217", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "ffa08cfb2b0209bb4b843d82", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_38t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 34, "name" : "34t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "am-4794", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "ffa08cfb2b0209bb4b843d82", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_36t_Aluminum__Plate_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 40, "name" : "40t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "am-4795", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "ffa08cfb2b0209bb4b843d82", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_60t_Aluminum__Billet_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 56, "name" : "56t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "am-4796", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "ffa08cfb2b0209bb4b843d82", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_84t_Aluminum__Billet_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 52, "name" : "52t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "am-4797", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "ffa08cfb2b0209bb4b843d82", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_62t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 58, "name" : "58t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "am-4798", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "ffa08cfb2b0209bb4b843d82", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_Copy_of_62t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 64, "name" : "64t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "am-4799", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "ffa08cfb2b0209bb4b843d82", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_68t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch }
            ]
        },
        "VEX" : {
            "1/2\" Hex" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 16, "name" : "16t Aluminum Sprocket (#25, 1/2\" Hex Bore, Single Hub)", "partNumber" : "217-2642 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "bdf0ad4d225446fcf5e8cf83", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 18, "name" : "18t Aluminum Sprocket (#25, 1/2\" Hex Bore, Single Hub)", "partNumber" : "217-3488 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "bdf0ad4d225446fcf5e8cf83", "configuration" : { "List_miHamtgOhdzyeP" : "_16t_Aluminum_Double" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 22, "name" : "22t Aluminum Sprocket (#25, 1/2\" Hex Bore, Single Hub)", "partNumber" : "217-2640 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "bdf0ad4d225446fcf5e8cf83", "configuration" : { "List_miHamtgOhdzyeP" : "_17t_Aluminum" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 12, "name" : "12t Aluminum Sprocket (#35, 1/2\" Hex Bore, Single Hub)", "partNumber" : "217-2662 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "f2f7a9798bccab05e02168cf", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 0.5774 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 15, "name" : "15t Aluminum Sprocket (#35, 1/2\" Hex Bore, Single Hub)", "partNumber" : "217-2660 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "f2f7a9798bccab05e02168cf", "configuration" : { "List_miHamtgOhdzyeP" : "_16t_Steel" }, "boreDiameter" : 0.5774 * inch }
            ],
            "3/8\" Hex" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 16, "name" : "16t Aluminum Sprocket (#25, 3/8\" Hex Bore, Single Hub)", "partNumber" : "217-5833 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "38ec6ebd93364b0ef6c6cc8b", "configuration" : { "List_miHamtgOhdzyeP" : "_12t_Steel" }, "boreDiameter" : 0.433 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 22, "name" : "22t Aluminum Sprocket (#25, 3/8\" Hex Bore, Single Hub)", "partNumber" : "217-5835 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "38ec6ebd93364b0ef6c6cc8b", "configuration" : { "List_miHamtgOhdzyeP" : "_17t_Aluminum" }, "boreDiameter" : 0.433 * inch }
            ],
            "1.125\" Round" : [
                { "chainType" : ChainType.ANSI_25, "teeth" : 32, "name" : "32t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "217-2690 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "e3678c5d4f5e133a4af5b614", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_32t_Aluminum_Plate__Legacy_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 34, "name" : "34t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "217-2664 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "e3678c5d4f5e133a4af5b614", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_38t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 36, "name" : "36t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "217-2639 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "e3678c5d4f5e133a4af5b614", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_24t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 38, "name" : "38t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "217-2638 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "e3678c5d4f5e133a4af5b614", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_36t_Aluminum__Billet_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 40, "name" : "40t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "217-2637 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "e3678c5d4f5e133a4af5b614", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_28t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 42, "name" : "42t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "217-2636 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "e3678c5d4f5e133a4af5b614", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_30t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 44, "name" : "44t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "217-2635 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "e3678c5d4f5e133a4af5b614", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_36t_Aluminum__Plate_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 48, "name" : "48t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "217-2634 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "e3678c5d4f5e133a4af5b614", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_60t_Aluminum__Plate_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 54, "name" : "54t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "217-2633 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "e3678c5d4f5e133a4af5b614", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_84t_Aluminum__Billet_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 58, "name" : "58t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "217-2632 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "e3678c5d4f5e133a4af5b614", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_62t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 60, "name" : "60t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "217-2631 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "e3678c5d4f5e133a4af5b614", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_Copy_of_62t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 64, "name" : "64t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "217-2629 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "e3678c5d4f5e133a4af5b614", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_60t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_25, "teeth" : 72, "name" : "72t Aluminum Plate Sprocket (#25, 1.125\" Round Bore)", "partNumber" : "217-2627 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "e3678c5d4f5e133a4af5b614", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_64t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 28, "name" : "28t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "217-2653 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "18090f93cf941fea743de808", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_36t_Aluminum__Billet_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 22, "name" : "22t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "217-2688 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "18090f93cf941fea743de808", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_32t_Aluminum_Plate__Legacy_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 24, "name" : "24t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "217-2655 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "18090f93cf941fea743de808", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_38t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 26, "name" : "26t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "217-2654 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "18090f93cf941fea743de808", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_24t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 30, "name" : "30t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "217-2652 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "18090f93cf941fea743de808", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_28t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 32, "name" : "32t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "217-2651 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "18090f93cf941fea743de808", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_30t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 36, "name" : "36t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "217-2649 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "18090f93cf941fea743de808", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_36t_Aluminum__Plate_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 42, "name" : "42t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "217-2648 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "18090f93cf941fea743de808", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_60t_Aluminum__Plate_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 44, "name" : "44t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "217-2647 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "18090f93cf941fea743de808", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_84t_Aluminum__Billet_" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 54, "name" : "54t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "217-2645 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "18090f93cf941fea743de808", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_62t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch },
                { "chainType" : ChainType.ANSI_35, "teeth" : 60, "name" : "60t Aluminum Plate Sprocket (#35, 1.125\" Round Bore)", "partNumber" : "217-2644 (Discontinued)", "documentId" : "a7548e2dd12e2a920f212fae", "elementId" : "18090f93cf941fea743de808", "configuration" : { "List_p0IV4hyyRmgk0X" : "Copy_of_Copy_of_62t_Aluminum_Plate" }, "boreDiameter" : 1.125 * inch }
            ]
        }
    };
