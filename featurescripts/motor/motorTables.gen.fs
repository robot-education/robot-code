FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/* Generated from motorTables.py by `fs gen` -- DO NOT EDIT */

export const frcMotorTable = {
        "name" : "motor",
        "displayName" : "Motor",
        "entries" : {
            "Kraken X60" : { "bodyDiameter" : 60 * millimeter, "bodyLength" : 75 * millimeter, "boltCircleDiameter" : 2 * inch, "bumpDistance" : 33.5 * millimeter, "bumpWidth" : 15.5 * millimeter, "holeAngles" : [0 * degree, 30 * degree, 60 * degree, 90 * degree, 120 * degree, 150 * degree, 180 * degree, 210 * degree, 240 * degree, 300 * degree, 330 * degree], "partName" : "Kraken X60", "pilotDiameter" : 0.75 * inch, "pilotHeight" : 0.1875 * inch, "screw" : "#10", "shaftDiameter" : 8 * millimeter, "shaftLength" : 1.25 * inch },
            "Falcon 500" : { "bodyDiameter" : 60 * millimeter, "bodyLength" : 81 * millimeter, "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 60 * degree, 120 * degree, 180 * degree, 240 * degree, 300 * degree], "partName" : "Falcon 500", "pilotDiameter" : 0.75 * inch, "pilotHeight" : 0.1875 * inch, "screw" : "#10", "shaftDiameter" : 8 * millimeter, "shaftLength" : 1.38 * inch },
            "Kraken X44" : { "bodyDiameter" : 44 * millimeter, "bodyLength" : 75 * millimeter, "boltCircleDiameter" : 1.375 * inch, "bumpDistance" : 25.4 * millimeter, "bumpWidth" : 11 * millimeter, "holeAngles" : [0 * degree, 30 * degree, 60 * degree, 90 * degree, 120 * degree, 150 * degree, 180 * degree, 210 * degree, 240 * degree, 300 * degree, 330 * degree], "partName" : "Kraken X44", "pilotDiameter" : 0.75 * inch, "pilotHeight" : 0.1875 * inch, "screw" : "#10", "shaftDiameter" : 8 * millimeter, "shaftLength" : 1.25 * inch },
            "NEO" : { "bodyDiameter" : 60 * millimeter, "bodyLength" : 58.3 * millimeter, "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 90 * degree, 180 * degree, 270 * degree], "partName" : "NEO", "pilotDiameter" : 19.1 * millimeter, "pilotHeight" : 3.5 * millimeter, "screw" : "#10", "shaftDiameter" : 8 * millimeter, "shaftLength" : 35 * millimeter },
            "NEO Vortex" : { "bodyDiameter" : 60 * millimeter, "bodyFlats" : 2 * inch, "bodyLength" : 79.7 * millimeter, "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 45 * degree, 135 * degree, 180 * degree, 225 * degree, 315 * degree], "partName" : "NEO Vortex", "pilotDiameter" : 1.25 * inch, "screw" : "#10" },
            "NEO 550" : { "bodyDiameter" : 35 * millimeter, "bodyLength" : 44.5 * millimeter, "boltCircleDiameter" : 25 * millimeter, "holeAngles" : [90 * degree, 270 * degree], "partName" : "NEO 550", "pilotDiameter" : 13 * millimeter, "pilotHeight" : 1.5 * millimeter, "screw" : "M3", "shaftDiameter" : 3.175 * millimeter, "shaftLength" : 8.5 * millimeter },
            "NEO 2.0" : { "bodyDiameter" : 60 * millimeter, "bodyFlats" : 2 * inch, "bodyLength" : 48 * millimeter, "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 45 * degree, 135 * degree, 180 * degree, 225 * degree, 315 * degree], "partName" : "NEO 2.0", "pilotDiameter" : 19 * millimeter, "pilotHeight" : 3.5 * millimeter, "screw" : "#10", "shaftDiameter" : 8 * millimeter, "shaftLength" : 35 * millimeter },
            "CIM" : { "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 180 * degree], "partName" : "CIM", "pilotDiameter" : 0.75 * inch, "screw" : "#10" },
            "RS-775" : { "boltCircleDiameter" : 29 * millimeter, "holeAngles" : [0 * degree, 180 * degree], "partName" : "RS-775", "pilotDiameter" : 17.5 * millimeter, "screw" : "M4" },
            "Minion" : {
                "name" : "pattern",
                "displayName" : "Hole pattern",
                "entries" : {
                    "#10-32" : { "bodyDiameter" : 38 * millimeter, "bodyLength" : 49.5 * millimeter, "boltCircleDiameter" : 1 * inch, "holeAngles" : [30 * degree, 150 * degree, 210 * degree, 270 * degree, 330 * degree], "partName" : "Minion", "pilotDiameter" : 18.8 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "#10", "shaftDiameter" : 7.94 * millimeter, "shaftLength" : 23 * millimeter },
                    "550 (M3)" : { "bodyDiameter" : 38 * millimeter, "bodyLength" : 49.5 * millimeter, "boltCircleDiameter" : 25 * millimeter, "holeAngles" : [0 * degree, 180 * degree], "partName" : "Minion", "pilotDiameter" : 18.8 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M3", "shaftDiameter" : 7.94 * millimeter, "shaftLength" : 23 * millimeter },
                    "775 (M4)" : { "bodyDiameter" : 38 * millimeter, "bodyLength" : 49.5 * millimeter, "boltCircleDiameter" : 29 * millimeter, "holeAngles" : [53.5 * degree, 233.5 * degree], "partName" : "Minion", "pilotDiameter" : 18.8 * millimeter, "pilotHeight" : 2 * millimeter, "screw" : "M4", "shaftDiameter" : 7.94 * millimeter, "shaftLength" : 23 * millimeter },
                },
            },
            "Mini CIM" : { "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 180 * degree], "partName" : "Mini CIM", "pilotDiameter" : 0.75 * inch, "screw" : "#10" },
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
                    "6000 RPM" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 84 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (1:1, 6000 RPM)", "pilotDiameter" : 14 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                    "1620 RPM" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 84 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (3.7:1, 1620 RPM)", "pilotDiameter" : 14 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                    "1150 RPM" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 84 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (5.2:1, 1150 RPM)", "pilotDiameter" : 14 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                    "435 RPM" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 92.9 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (13.7:1, 435 RPM)", "pilotDiameter" : 14 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                    "312 RPM" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 92.9 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (19.2:1, 312 RPM)", "pilotDiameter" : 14 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                    "223 RPM" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 92.9 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (26.9:1, 223 RPM)", "pilotDiameter" : 14 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                    "117 RPM" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 101.6 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (50.9:1, 117 RPM)", "pilotDiameter" : 14 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                    "84 RPM" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 101.6 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (71.2:1, 84 RPM)", "pilotDiameter" : 14 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                    "60 RPM" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 101.6 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (99.5:1, 60 RPM)", "pilotDiameter" : 14 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                    "43 RPM" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 101.6 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (139:1, 43 RPM)", "pilotDiameter" : 14 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                    "30 RPM" : { "bodyDiameter" : 37.5 * millimeter, "bodyLength" : 110.5 * millimeter, "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5203 Series Yellow Jacket (188:1, 30 RPM)", "pilotDiameter" : 14 * millimeter, "screw" : "M4", "shaftDiameter" : 8 * millimeter, "shaftLength" : 23.5 * millimeter },
                },
            },
        },
    };

export const ftcGearboxTable = {
        "name" : "gearbox",
        "displayName" : "Gearbox",
        "entries" : {
            "goBILDA planetary" : { "holePositions" : [vector(12, 0) * millimeter, vector(8, 8) * millimeter, vector(-8, 8) * millimeter, vector(-12, 0) * millimeter, vector(-8, -8) * millimeter, vector(8, -8) * millimeter], "partName" : "5103 Series Planetary Gearbox", "pilotDiameter" : 14 * millimeter, "screw" : "M4" },
            "UltraPlanetary" : { "boltCircleDiameter" : 32 * millimeter, "holeAngles" : [30 * degree, 90 * degree, 150 * degree, 210 * degree, 270 * degree, 330 * degree], "partName" : "UltraPlanetary", "pilotDiameter" : 22 * millimeter, "screw" : "M3" },
        },
    };
