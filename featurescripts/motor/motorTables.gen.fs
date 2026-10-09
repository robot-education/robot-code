FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/* Generated from motorTables.py by `fs gen` -- DO NOT EDIT */

export const frcMotorTable = {
        "name" : "motor",
        "displayName" : "Motor",
        "entries" : {
            "Kraken X60" : {
                "name" : "pattern",
                "displayName" : "Hole pattern",
                "entries" : {
                    "All" : { "blockAngle" : -90 * degree, "blockMotor" : "Kraken_X60", "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 30 * degree, 60 * degree, 90 * degree, 120 * degree, 150 * degree, 180 * degree, 210 * degree, 240 * degree, 300 * degree, 330 * degree], "partName" : "Kraken X60", "pilotDiameter" : 0.75 * inch, "screw" : "#10" },
                    "Falcon 500" : { "blockAngle" : -90 * degree, "blockMotor" : "Kraken_X60", "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 60 * degree, 120 * degree, 180 * degree, 240 * degree, 300 * degree], "partName" : "Kraken X60", "pilotDiameter" : 0.75 * inch, "screw" : "#10" },
                    "CIM" : { "blockAngle" : -90 * degree, "blockMotor" : "Kraken_X60", "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 180 * degree], "partName" : "Kraken X60", "pilotDiameter" : 0.75 * inch, "screw" : "#10" },
                },
            },
            "Falcon 500" : {
                "name" : "version",
                "displayName" : "Version",
                "entries" : {
                    "V1/2" : { "blockAngle" : 0 * degree, "blockMotor" : "KrakenX60", "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 60 * degree, 120 * degree, 180 * degree, 240 * degree, 300 * degree], "partName" : "Falcon 500", "pilotDiameter" : 0.75 * inch, "screw" : "#10" },
                    "V3" : { "blockAngle" : 0 * degree, "blockMotor" : "Falcon_500_V3", "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 60 * degree, 120 * degree, 180 * degree, 240 * degree, 300 * degree], "partName" : "Falcon 500", "pilotDiameter" : 0.75 * inch, "screw" : "#10" },
                },
            },
            "Kraken X44" : { "blockAngle" : -90 * degree, "blockMotor" : "Kraken_X44", "boltCircleDiameter" : 1.375 * inch, "holeAngles" : [0 * degree, 30 * degree, 60 * degree, 90 * degree, 120 * degree, 150 * degree, 180 * degree, 210 * degree, 240 * degree, 300 * degree, 330 * degree], "partName" : "Kraken X44", "pilotDiameter" : 0.75 * inch, "screw" : "#10" },
            "NEO" : {
                "name" : "version",
                "displayName" : "Version",
                "entries" : {
                    "V1.1" : { "blockAngle" : 0 * degree, "blockMotor" : "NEO_V1_1", "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 90 * degree, 180 * degree, 270 * degree], "partName" : "NEO", "pilotDiameter" : 19.1 * millimeter, "screw" : "#10" },
                    "V2.0" : { "blockAngle" : 0 * degree, "blockMotor" : "Copy_of_NEO_Vortex", "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 45 * degree, 135 * degree, 180 * degree, 225 * degree, 315 * degree], "partName" : "NEO 2.0", "pilotDiameter" : 19 * millimeter, "screw" : "#10" },
                    "V1.0" : { "blockAngle" : 0 * degree, "blockMotor" : "NEO_V1_0", "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 90 * degree, 180 * degree, 270 * degree], "partName" : "NEO", "pilotDiameter" : 19.1 * millimeter, "screw" : "#10" },
                },
            },
            "NEO Vortex" : { "blockAngle" : 0 * degree, "blockMotor" : "NEO_Vortex", "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 45 * degree, 135 * degree, 180 * degree, 225 * degree, 315 * degree], "partName" : "NEO Vortex", "pilotDiameter" : 1.25 * inch, "screw" : "#10" },
            "NEO 550" : { "blockAngle" : 0 * degree, "blockMotor" : "NEO_550", "boltCircleDiameter" : 25 * millimeter, "holeAngles" : [90 * degree, 270 * degree], "partName" : "NEO 550", "pilotDiameter" : 13 * millimeter, "screw" : "M3" },
            "CIM" : { "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 180 * degree], "partName" : "CIM", "pilotDiameter" : 0.75 * inch, "screw" : "#10" },
            "RS-775" : { "boltCircleDiameter" : 29 * millimeter, "holeAngles" : [0 * degree, 180 * degree], "partName" : "RS-775", "pilotDiameter" : 17.5 * millimeter, "screw" : "M4" },
            "Minion" : {
                "name" : "pattern",
                "displayName" : "Hole pattern",
                "entries" : {
                    "#10-32" : { "bodyDiameter" : 38 * millimeter, "bodyLength" : 49.5 * millimeter, "boltCircleDiameter" : 1 * inch, "holeAngles" : [30 * degree, 150 * degree, 210 * degree, 270 * degree, 330 * degree], "partName" : "Minion", "pilotDiameter" : 18.8 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "#10", "shaftDiameter" : 7.94 * millimeter, "shaftLength" : 23 * millimeter },
                    "550" : { "bodyDiameter" : 38 * millimeter, "bodyLength" : 49.5 * millimeter, "boltCircleDiameter" : 25 * millimeter, "holeAngles" : [0 * degree, 180 * degree], "partName" : "Minion", "pilotDiameter" : 18.8 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M3", "shaftDiameter" : 7.94 * millimeter, "shaftLength" : 23 * millimeter },
                    "775" : { "bodyDiameter" : 38 * millimeter, "bodyLength" : 49.5 * millimeter, "boltCircleDiameter" : 29 * millimeter, "holeAngles" : [53.5 * degree, 233.5 * degree], "partName" : "Minion", "pilotDiameter" : 18.8 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 7.94 * millimeter, "shaftLength" : 23 * millimeter },
                },
            },
            "Mini CIM" : { "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 180 * degree], "partName" : "Mini CIM", "pilotDiameter" : 0.75 * inch, "screw" : "#10" },
            "Thrifty Pulsar" : {
                "name" : "pattern",
                "displayName" : "Hole pattern",
                "entries" : {
                    "#10-32" : { "blockAngle" : 0 * degree, "blockMotor" : "Copy_of_NEO_V1_1", "boltCircleDiameter" : 1.375 * inch, "holeAngles" : [45 * degree, 135 * degree, 225 * degree, 315 * degree], "partName" : "Thrifty Pulsar", "pilotDiameter" : 19 * millimeter, "screw" : "#10" },
                    "775" : { "blockAngle" : 0 * degree, "blockMotor" : "Copy_of_NEO_V1_1", "boltCircleDiameter" : 29 * millimeter, "holeAngles" : [0 * degree, 90 * degree, 180 * degree, 270 * degree], "partName" : "Thrifty Pulsar", "pilotDiameter" : 19 * millimeter, "screw" : "M4" },
                },
            },
        },
    };

export const frcGearboxTable = {
        "name" : "gearbox",
        "displayName" : "Gearbox",
        "entries" : {
            "MAXPlanetary" : { "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 135 * degree, 180 * degree, 315 * degree], "partName" : "MAXPlanetary", "pilotDiameter" : 1.25 * inch, "screw" : "#10" },
            "PlanetaryX" : { "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 45 * degree, 135 * degree, 180 * degree, 225 * degree, 315 * degree], "partName" : "PlanetaryX", "pilotDiameter" : 1.5 * inch, "screw" : "#10" },
            "UltraPlanetary" : { "boltCircleDiameter" : 32 * millimeter, "holeAngles" : [30 * degree, 90 * degree, 150 * degree, 210 * degree, 270 * degree, 330 * degree], "partName" : "UltraPlanetary", "pilotDiameter" : 22 * millimeter, "screw" : "M3" },
            "VersaPlanetary" : { "boltCircleDiameter" : 2 * inch, "holeAngles" : [45 * degree, 135 * degree, 225 * degree, 315 * degree], "partName" : "VersaPlanetary", "pilotDiameter" : 0.75 * inch, "screw" : "#10" },
        },
    };

export const ftcMotorTable = {
        "name" : "motor",
        "displayName" : "Motor",
        "entries" : {
            "Yellow Jacket" : {
                "name" : "speed",
                "displayName" : "Speed",
                "default" : "435 RPM",
                "entries" : {
                    "6000 RPM" : {
                        "name" : "pattern",
                        "displayName" : "Hole pattern",
                        "entries" : {
                            "16 mm square" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 84 * millimeter, "holePositions" : [vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (1:1, 6000 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                            "All" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 84 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (1:1, 6000 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                        },
                    },
                    "1620 RPM" : {
                        "name" : "pattern",
                        "displayName" : "Hole pattern",
                        "entries" : {
                            "16 mm square" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 84 * millimeter, "holePositions" : [vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (3.7:1, 1620 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                            "All" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 84 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (3.7:1, 1620 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                        },
                    },
                    "1150 RPM" : {
                        "name" : "pattern",
                        "displayName" : "Hole pattern",
                        "entries" : {
                            "16 mm square" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 84 * millimeter, "holePositions" : [vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (5.2:1, 1150 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                            "All" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 84 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (5.2:1, 1150 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                        },
                    },
                    "435 RPM" : {
                        "name" : "pattern",
                        "displayName" : "Hole pattern",
                        "entries" : {
                            "16 mm square" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 92.9 * millimeter, "holePositions" : [vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (13.7:1, 435 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                            "All" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 92.9 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (13.7:1, 435 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                        },
                    },
                    "312 RPM" : {
                        "name" : "pattern",
                        "displayName" : "Hole pattern",
                        "entries" : {
                            "16 mm square" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 92.9 * millimeter, "holePositions" : [vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (19.2:1, 312 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                            "All" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 92.9 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (19.2:1, 312 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                        },
                    },
                    "223 RPM" : {
                        "name" : "pattern",
                        "displayName" : "Hole pattern",
                        "entries" : {
                            "16 mm square" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 92.9 * millimeter, "holePositions" : [vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (26.9:1, 223 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                            "All" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 92.9 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (26.9:1, 223 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                        },
                    },
                    "117 RPM" : {
                        "name" : "pattern",
                        "displayName" : "Hole pattern",
                        "entries" : {
                            "16 mm square" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 101.6 * millimeter, "holePositions" : [vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (50.9:1, 117 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                            "All" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 101.6 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (50.9:1, 117 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                        },
                    },
                    "84 RPM" : {
                        "name" : "pattern",
                        "displayName" : "Hole pattern",
                        "entries" : {
                            "16 mm square" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 101.6 * millimeter, "holePositions" : [vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (71.2:1, 84 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                            "All" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 101.6 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (71.2:1, 84 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                        },
                    },
                    "60 RPM" : {
                        "name" : "pattern",
                        "displayName" : "Hole pattern",
                        "entries" : {
                            "16 mm square" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 101.6 * millimeter, "holePositions" : [vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (99.5:1, 60 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                            "All" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 101.6 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (99.5:1, 60 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                        },
                    },
                    "43 RPM" : {
                        "name" : "pattern",
                        "displayName" : "Hole pattern",
                        "entries" : {
                            "16 mm square" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 101.6 * millimeter, "holePositions" : [vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (139:1, 43 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                            "All" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 101.6 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (139:1, 43 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                        },
                    },
                    "30 RPM" : {
                        "name" : "pattern",
                        "displayName" : "Hole pattern",
                        "entries" : {
                            "16 mm square" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 110.5 * millimeter, "holePositions" : [vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (188:1, 30 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                            "All" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 110.5 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (188:1, 30 RPM)", "pilotDiameter" : 14 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                        },
                    },
                },
            },
        },
    };

export const ftcGearboxTable = {
        "name" : "gearbox",
        "displayName" : "Gearbox",
        "entries" : {
            "goBILDA planetary" : {
                "name" : "pattern",
                "displayName" : "Hole pattern",
                "entries" : {
                    "16 mm square" : { "holePositions" : [vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5103 Series Planetary Gearbox", "pilotDiameter" : 14 * millimeter, "screw" : "M4" },
                    "All" : { "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5103 Series Planetary Gearbox", "pilotDiameter" : 14 * millimeter, "screw" : "M4" },
                },
            },
            "UltraPlanetary" : { "boltCircleDiameter" : 32 * millimeter, "holeAngles" : [30 * degree, 90 * degree, 150 * degree, 210 * degree, 270 * degree, 330 * degree], "partName" : "UltraPlanetary", "pilotDiameter" : 22 * millimeter, "screw" : "M3" },
        },
    };
