FeatureScript 1930;
import(path : "onshape/std/common.fs", version : "1930.0");

export const componentTable = {
        "name" : "componentType",
        "displayName" : "Component type",
        "default" : "Motor",
        "entries" : {
            "Motor" : motorTable,
            "Gearbox" : gearboxTable,
            "Required" : requiredTable,
            "Motor controller" : motorControllerTable
        }
    };

const motorTable = {
        "name" : "motor",
        "displayName" : "Motor",
        "default" : "Falcon 500",
        "entries" : {
            "Falcon 500" : {},
            "NEO" : {},
            "NEO 550" : {},
            "775pro" : {},
            "CIM" : {},
            "BAG" : {}
        }
    };

const gearboxTable = {
        "name" : "gearbox",
        "displayName" : "Gearbox",
        "default" : "MAXPlanetary",
        "entries" : {
            "MAXPlanetary" : {},
            "UltraPlanetary" : {},
            "VersaPlanetary" : {},
            "Sport" : {}
        }
    };

const requiredTable = {
        "name" : "required",
        "displayName" : "Required",
        "default" : "roboRIO 2.0",
        "entries" : {
            "roboRIO 2.0" : {},
            "Power distribution" : {
                "name" : "powerDistributionType",
                "displayName" : "Type",
                "default" : "REV PDH",
                "entries" : {
                    "REV PDH" : {},
                    "CTRE PDP" : {}
                }
            },
            "Main breaker" : {
                "name" : "type",
                "displayName" : "Type",
                "default" : "Standard",
                "entries" : {
                    "Standard" : {},
                    "Eaton bussman" : {}
                }
            },
            "Robot signal light" : {},
            "Robot radio" : {}
        }
    };

const motorControllerTable = {
        "name" : "motorController",
        "displayName" : "Motor controller",
        "default" : "Spark MAX",
        "entries" : {
            "Spark MAX" : {},
            "Victor SPX" : {},
            "Talon SRX" : {},
            "Spark" : {}
        }
    };

const pneumaticTable = {
        "name" : "pneumatic",
        "displayName" : "Pneumatic",
        "default" : "Compressor",
        "entries" : {
            "Manifold" : {

            },
            "Compressor" : {

            }
        }
    };

const sensorTable = {
        "name" : "sensor",
        "displayName" : "Sensor",
        "default" : "Limelight",
        "entries" : {
            "Limelight" : {}
        }
    };

const otherTable = 2;
