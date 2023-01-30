FeatureScript 1948;
import(path : "onshape/std/common.fs", version : "1948.0");

export const presetTappedOrClearanceHoleTable = {
        "name" : "type",
        "displayName" : "Hole type",
        "default" : "Clearance",
        "entries" : {
            "Clearance" : clearanceHoleTable,
            "Tapped" : tappedHoleTable
        }
    };

export const presetBlindInLastHoleTable = {
        "name" : "size",
        "displayName" : "Size",
        "default" : "#10",
        "entries" : {
            "#8" : {
                "name" : "pitch",
                "displayName" : "Threads/inch",
                "default" : "32 tpi",
                "entries" : {
                    "32 tpi" : {
                        "name" : "fit",
                        "displayName" : "Fit",
                        "default" : "Free",
                        "entries" : {
                            "Close" : {
                                "name" : "engagement",
                                "displayName" : "% diametric engagement",
                                "default" : "75%",
                                "entries" : {
                                    "50%" : { "tapDrillDiameter" : "0.144 in", "holeDiameter" : "0.1695 in", "cBoreDiameter" : "5/16 in", "cBoreDepth" : "0.164 in", "cSinkDiameter" : "0.359 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.164 in" },
                                    "75%" : { "tapDrillDiameter" : "0.136 in", "holeDiameter" : "0.1695 in", "cBoreDiameter" : "5/16 in", "cBoreDepth" : "0.164 in", "cSinkDiameter" : "0.359 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.164 in" }
                                }
                            },
                            "Free" : {
                                "name" : "engagement",
                                "displayName" : "% diametric engagement",
                                "default" : "75%",
                                "entries" : {
                                    "50%" : { "tapDrillDiameter" : "0.144 in", "holeDiameter" : "0.177 in", "cBoreDiameter" : "5/16 in", "cBoreDepth" : "0.164 in", "cSinkDiameter" : "0.359 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.164 in" },
                                    "75%" : { "tapDrillDiameter" : "0.136 in", "holeDiameter" : "0.177 in", "cBoreDiameter" : "5/16 in", "cBoreDepth" : "0.164 in", "cSinkDiameter" : "0.359 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.164 in" }
                                }
                            }
                        }
                    }
                }
            },
            "#10" : {
                "name" : "pitch",
                "displayName" : "Threads/inch",
                "default" : "32 tpi",
                "entries" : {
                    "32 tpi" : {
                        "name" : "fit",
                        "displayName" : "Fit",
                        "default" : "Free",
                        "entries" : {
                            "Close" : {
                                "name" : "engagement",
                                "displayName" : "% diametric engagement",
                                "default" : "75%",
                                "entries" : {
                                    "50%" : { "tapDrillDiameter" : "0.1695 in", "holeDiameter" : "0.196 in", "cBoreDiameter" : "3/8 in", "cBoreDepth" : "0.19 in", "cSinkDiameter" : "0.411 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.19 in" },
                                    "75%" : { "tapDrillDiameter" : "0.159 in", "holeDiameter" : "0.196 in", "cBoreDiameter" : "3/8 in", "cBoreDepth" : "0.19 in", "cSinkDiameter" : "0.411 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.19 in" }
                                }
                            },
                            "Free" : {
                                "name" : "engagement",
                                "displayName" : "% diametric engagement",
                                "default" : "75%",
                                "entries" : {
                                    "50%" : { "tapDrillDiameter" : "0.1695 in", "holeDiameter" : "0.201 in", "cBoreDiameter" : "3/8 in", "cBoreDepth" : "0.19 in", "cSinkDiameter" : "0.411 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.19 in" },
                                    "75%" : { "tapDrillDiameter" : "0.159 in", "holeDiameter" : "0.201 in", "cBoreDiameter" : "3/8 in", "cBoreDepth" : "0.19 in", "cSinkDiameter" : "0.411 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.19 in" }
                                }
                            }
                        }
                    }
                }
            },
            "1/4" : {
                "name" : "pitch",
                "displayName" : "Threads/inch",
                "default" : "20 tpi",
                "entries" : {
                    "20 tpi" : {
                        "name" : "fit",
                        "displayName" : "Fit",
                        "default" : "Free",
                        "entries" : {
                            "Close" : {
                                "name" : "engagement",
                                "displayName" : "% diametric engagement",
                                "default" : "75%",
                                "entries" : {
                                    "50%" : { "tapDrillDiameter" : "0.2188 in", "holeDiameter" : "0.257 in", "cBoreDiameter" : "7/16 in", "cBoreDepth" : "0.25 in", "cSinkDiameter" : "0.531 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.25 in" },
                                    "75%" : { "tapDrillDiameter" : "0.201 in", "holeDiameter" : "0.257 in", "cBoreDiameter" : "7/16 in", "cBoreDepth" : "0.25 in", "cSinkDiameter" : "0.531 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.25 in" }
                                }
                            },
                            "Free" : {
                                "name" : "engagement",
                                "displayName" : "% diametric engagement",
                                "default" : "75%",
                                "entries" : {
                                    "50%" : { "tapDrillDiameter" : "0.2188 in", "holeDiameter" : "0.266 in", "cBoreDiameter" : "7/16 in", "cBoreDepth" : "0.25 in", "cSinkDiameter" : "0.531 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.25 in" },
                                    "75%" : { "tapDrillDiameter" : "0.201 in", "holeDiameter" : "0.266 in", "cBoreDiameter" : "7/16 in", "cBoreDepth" : "0.25 in", "cSinkDiameter" : "0.531 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.25 in" }
                                }
                            }
                        }
                    },
                    "28 tpi" : {
                        "name" : "fit",
                        "displayName" : "Fit",
                        "default" : "Free",
                        "entries" : {
                            "Close" : {
                                "name" : "engagement",
                                "displayName" : "% diametric engagement",
                                "default" : "75%",
                                "entries" : {
                                    "50%" : { "tapDrillDiameter" : "0.228 in", "holeDiameter" : "0.257 in", "cBoreDiameter" : "7/16 in", "cBoreDepth" : "0.25 in", "cSinkDiameter" : "0.531 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.25 in" },
                                    "75%" : { "tapDrillDiameter" : "0.213 in", "holeDiameter" : "0.257 in", "cBoreDiameter" : "7/16 in", "cBoreDepth" : "0.25 in", "cSinkDiameter" : "0.531 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.25 in" }
                                }
                            },
                            "Free" : {
                                "name" : "engagement",
                                "displayName" : "% diametric engagement",
                                "default" : "75%",
                                "entries" : {
                                    "50%" : { "tapDrillDiameter" : "0.228 in", "holeDiameter" : "0.266 in", "cBoreDiameter" : "7/16 in", "cBoreDepth" : "0.25 in", "cSinkDiameter" : "0.531 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.25 in" },
                                    "75%" : { "tapDrillDiameter" : "0.213 in", "holeDiameter" : "0.266 in", "cBoreDiameter" : "7/16 in", "cBoreDepth" : "0.25 in", "cSinkDiameter" : "0.531 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.25 in" }
                                }
                            }
                        }
                    }
                }
            }
        }
    };

const clearanceHoleTable = {
        "name" : "size",
        "displayName" : "Size",
        "default" : "#10",
        "entries" : {
            "#8" : {
                "name" : "fit",
                "displayName" : "Fit",
                "default" : "Free",
                "entries" : {
                    "Close" : { "holeDiameter" : "0.1695 in", "cBoreDiameter" : "5/16 in", "cBoreDepth" : "0.164 in", "cSinkDiameter" : "0.359 in", "cSinkAngle" : "82 degree" },
                    "Free" : { "holeDiameter" : "0.177 in", "cBoreDiameter" : "5/16 in", "cBoreDepth" : "0.164 in", "cSinkDiameter" : "0.359 in", "cSinkAngle" : "82 degree" }
                }
            },
            "#10" : {
                "name" : "fit",
                "displayName" : "Fit",
                "default" : "Free",
                "entries" : {
                    "Close" : { "holeDiameter" : "0.196 in", "cBoreDiameter" : "3/8 in", "cBoreDepth" : "0.19 in", "cSinkDiameter" : "0.411 in", "cSinkAngle" : "82 degree" },
                    "Free" : { "holeDiameter" : "0.201 in", "cBoreDiameter" : "3/8 in", "cBoreDepth" : "0.19 in", "cSinkDiameter" : "0.411 in", "cSinkAngle" : "82 degree" }
                }
            },
            "1/4" : {
                "name" : "fit",
                "displayName" : "Fit",
                "default" : "Free",
                "entries" : {
                    "Close" : { "holeDiameter" : "0.257 in", "cBoreDiameter" : "7/16 in", "cBoreDepth" : "0.25 in", "cSinkDiameter" : "0.531 in", "cSinkAngle" : "82 degree" },
                    "Free" : { "holeDiameter" : "0.266 in", "cBoreDiameter" : "7/16 in", "cBoreDepth" : "0.25 in", "cSinkDiameter" : "0.531 in", "cSinkAngle" : "82 degree" }
                }
            }
        }
    };

const tappedHoleTable = {
        "name" : "size",
        "displayName" : "Size",
        "default" : "#10",
        "entries" : {
            "#8" : {
                "name" : "pitch",
                "displayName" : "Threads/inch",
                "default" : "32 tpi",
                "entries" : {
                    "32 tpi" : {
                        "name" : "engagement",
                        "displayName" : "% diametric engagement",
                        "default" : "75%",
                        "entries" : {
                            "50%" : { "holeDiameter" : "0.144 in", "tapDrillDiameter" : "0.144 in", "cBoreDiameter" : "5/16 in", "cBoreDepth" : "0.164 in", "cSinkDiameter" : "0.359 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.164 in" },
                            "75%" : { "holeDiameter" : "0.136 in", "tapDrillDiameter" : "0.136 in", "cBoreDiameter" : "5/16 in", "cBoreDepth" : "0.164 in", "cSinkDiameter" : "0.359 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.164 in" }
                        }
                    }
                }
            },
            "#10" : {
                "name" : "pitch",
                "displayName" : "Threads/inch",
                "default" : "32 tpi",
                "entries" : {
                    "32 tpi" : {
                        "name" : "engagement",
                        "displayName" : "% diametric engagement",
                        "default" : "75%",
                        "entries" : {
                            "50%" : { "holeDiameter" : "0.1695 in", "tapDrillDiameter" : "0.1695 in", "cBoreDiameter" : "3/8 in", "cBoreDepth" : "0.19 in", "cSinkDiameter" : "0.411 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.19 in" },
                            "75%" : { "holeDiameter" : "0.159 in", "tapDrillDiameter" : "0.159 in", "cBoreDiameter" : "3/8 in", "cBoreDepth" : "0.19 in", "cSinkDiameter" : "0.411 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.19 in" }
                        }
                    }
                }
            },
            "1/4" : {
                "name" : "pitch",
                "displayName" : "Threads/inch",
                "default" : "20 tpi",
                "entries" : {
                    "20 tpi" : {
                        "name" : "engagement",
                        "displayName" : "% diametric engagement",
                        "default" : "75%",
                        "entries" : {
                            "50%" : { "holeDiameter" : "0.2188 in", "tapDrillDiameter" : "0.2188 in", "cBoreDiameter" : "7/16 in", "cBoreDepth" : "0.25 in", "cSinkDiameter" : "0.531 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.25 in" },
                            "75%" : { "holeDiameter" : "0.201 in", "tapDrillDiameter" : "0.201 in", "cBoreDiameter" : "7/16 in", "cBoreDepth" : "0.25 in", "cSinkDiameter" : "0.531 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.25 in" }
                        }
                    },
                    "28 tpi" : {
                        "name" : "engagement",
                        "displayName" : "% diametric engagement",
                        "default" : "75%",
                        "entries" : {
                            "50%" : { "holeDiameter" : "0.228 in", "tapDrillDiameter" : "0.228 in", "cBoreDiameter" : "7/16 in", "cBoreDepth" : "0.25 in", "cSinkDiameter" : "0.531 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.25 in" },
                            "75%" : { "holeDiameter" : "0.213 in", "tapDrillDiameter" : "0.213 in", "cBoreDiameter" : "7/16 in", "cBoreDepth" : "0.25 in", "cSinkDiameter" : "0.531 in", "cSinkAngle" : "82 degree", "majorDiameter" : "0.25 in" }
                        }
                    }
                }
            }
        }
    };


