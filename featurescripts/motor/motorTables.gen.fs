FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/* Generated from motorTables.py by `fs gen` -- DO NOT EDIT */

export const motorTable = {
        "name" : "motor",
        "displayName" : "Motor",
        "entries" : {
            "Kraken X60" : { "bodyDiameter" : 60 * millimeter, "bodyLength" : 75 * millimeter, "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 30 * degree, 60 * degree, 90 * degree, 120 * degree, 150 * degree, 180 * degree, 210 * degree, 240 * degree, 300 * degree, 330 * degree], "partName" : "Kraken X60", "pilotDiameter" : 0.75 * inch, "pilotHeight" : 0.1875 * inch, "screw" : "#10", "shaftDiameter" : 8 * millimeter, "shaftLength" : 1.25 * inch },
            "Falcon 500" : { "bodyDiameter" : 60 * millimeter, "bodyLength" : 81 * millimeter, "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 60 * degree, 120 * degree, 180 * degree, 240 * degree, 300 * degree], "partName" : "Falcon 500", "pilotDiameter" : 0.75 * inch, "pilotHeight" : 0.1875 * inch, "screw" : "#10", "shaftDiameter" : 8 * millimeter, "shaftLength" : 1.38 * inch },
            "NEO" : { "bodyDiameter" : 60 * millimeter, "bodyLength" : 58.3 * millimeter, "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 90 * degree, 180 * degree, 270 * degree], "partName" : "NEO", "pilotDiameter" : 19.1 * millimeter, "pilotHeight" : 3.5 * millimeter, "screw" : "#10", "shaftDiameter" : 8 * millimeter, "shaftLength" : 35 * millimeter },
            "Kraken X44" : { "bodyDiameter" : 44 * millimeter, "bodyLength" : 75 * millimeter, "boltCircleDiameter" : 1.375 * inch, "holeAngles" : [0 * degree, 30 * degree, 60 * degree, 90 * degree, 120 * degree, 150 * degree, 180 * degree, 210 * degree, 240 * degree, 300 * degree, 330 * degree], "partName" : "Kraken X44", "pilotDiameter" : 0.75 * inch, "pilotHeight" : 0.1875 * inch, "screw" : "#10", "shaftDiameter" : 8 * millimeter, "shaftLength" : 1.25 * inch },
            "NEO Vortex" : { "bodyDiameter" : 60 * millimeter, "bodyFlats" : 2 * inch, "bodyLength" : 79.7 * millimeter, "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 45 * degree, 135 * degree, 180 * degree, 225 * degree, 315 * degree], "partName" : "NEO Vortex", "pilotDiameter" : 1.25 * inch, "screw" : "#10" },
            "NEO 550" : { "bodyDiameter" : 35 * millimeter, "bodyLength" : 44.5 * millimeter, "boltCircleDiameter" : 25 * millimeter, "holeAngles" : [90 * degree, 270 * degree], "partName" : "NEO 550", "pilotDiameter" : 13 * millimeter, "pilotHeight" : 1.5 * millimeter, "screw" : "M3", "shaftDiameter" : 3.175 * millimeter, "shaftLength" : 8.5 * millimeter },
            "NEO 2.0" : { "bodyDiameter" : 60 * millimeter, "bodyFlats" : 2 * inch, "bodyLength" : 48 * millimeter, "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 45 * degree, 135 * degree, 180 * degree, 225 * degree, 315 * degree], "partName" : "NEO 2.0", "pilotDiameter" : 19 * millimeter, "pilotHeight" : 3.5 * millimeter, "screw" : "#10", "shaftDiameter" : 8 * millimeter, "shaftLength" : 35 * millimeter },
            "CIM" : { "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 180 * degree], "partName" : "CIM", "pilotDiameter" : 0.75 * inch, "screw" : "#10" },
            "Mini CIM" : { "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 180 * degree], "partName" : "Mini CIM", "pilotDiameter" : 0.75 * inch, "screw" : "#10" },
            "RS-775" : { "boltCircleDiameter" : 29 * millimeter, "holeAngles" : [0 * degree, 180 * degree], "partName" : "RS-775", "pilotDiameter" : 17.5 * millimeter, "screw" : "M4" },
        },
    };

export const gearboxTable = {
        "name" : "gearbox",
        "displayName" : "Gearbox",
        "entries" : {
            "MAXPlanetary" : { "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 135 * degree, 180 * degree, 315 * degree], "partName" : "MAXPlanetary", "pilotDiameter" : 1.25 * inch, "screw" : "#10" },
            "PlanetaryX" : { "boltCircleDiameter" : 2 * inch, "holeAngles" : [0 * degree, 45 * degree, 135 * degree, 180 * degree, 225 * degree, 315 * degree], "partName" : "PlanetaryX", "pilotDiameter" : 1.5 * inch, "screw" : "#10" },
            "UltraPlanetary" : { "boltCircleDiameter" : 32 * millimeter, "holeAngles" : [30 * degree, 90 * degree, 150 * degree, 210 * degree, 270 * degree, 330 * degree], "partName" : "UltraPlanetary", "pilotDiameter" : 22 * millimeter, "screw" : "M3" },
            "VersaPlanetary" : { "boltCircleDiameter" : 2 * inch, "holeAngles" : [45 * degree, 135 * degree, 225 * degree, 315 * degree], "partName" : "VersaPlanetary", "pilotDiameter" : 0.75 * inch, "screw" : "#10" },
            "Sport" : { "boltCircleDiameter" : 2 * inch, "holeAngles" : [45 * degree, 135 * degree, 225 * degree, 315 * degree], "partName" : "Sport", "pilotDiameter" : 1.5 * inch, "screw" : "#10" },
        },
    };
