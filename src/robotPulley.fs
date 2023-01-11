FeatureScript 1930;
import(path : "onshape/std/common.fs", version : "1930.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "804646d519131ab5d52e2baf");

export import(path : "00b10ef1fb1a7418097fc0af", version : "81a5f0dafb1063cd569c35e6");

export import(path : "e269bd2b7266145c47eaf374", version : "b54f77829d3c3e4e5f1b9278");

annotation { "Feature Type Name" : "Robot pulley" }
export const robotPulley = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Belt faces", "Filter" : GeometryType.CYLINDER && EntityType.FACE }
        definition.beltFaces is Query;

        annotation { "Name" : "Width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.width, PULLEY_WIDTH_BOUNDS);

        annotation { "Name" : "Bore type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
        definition.boreType is BoreType;

        if (definition.boreType == BoreType.HEX || definition.boreType == BoreType.CIRCULAR)
        {
            annotation { "Name" : "Bore diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.boreDiameter, PULLEY_BORE_BOUNDS);
        }
        else if (definition.boreType == BoreType.GEAR)
        {
            annotation { "Name" : "Gear teeth", "UIHint" : "REMEMBER_PREVIOUS_VALUE" }
            isInteger(definition.gearTeeth, PULLEY_GEAR_TEETH_BOUNDS);
        }
        else if (definition.boreType == BoreType.FALCON_SPLINE || definition.boreType == BoreType.ADAPTER)
        {
            annotation { "Name" : "Bore fit", "UIHint" : "REMEMBER_PREVIOUS_VALUE" }
            isLength(definition.boreFit, LENGTH_BOUNDS);

            annotation { "Name" : "Opposite direction", "UIHint" : "OPPOSITE_DIRECTION" }
            definition.oppositeDirection is boolean;
        }

        if (definition.boreType != BoreType.NONE)
        {
            annotation { "Name" : "Chamfer bore edges", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.chamferEdges is boolean;
        }

        annotation { "Name" : "Has flanges", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.hasFlanges is boolean;

        if (definition.hasFlanges)
        {
            annotation { "Name" : "Flange width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.flangeWidth, LENGTH_BOUNDS);
        }

        annotation { "Name" : "Engrave tooth count", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.engraveToothCount is boolean;
    }
    {
        doRobotPulley(context, id, definition);
    });

function doRobotPulley(context is Context, id is Id, definition is map)
{
    const instantiator = newInstantiator(id + "feature");
    // addPulleyInstance(context, instantiator);
}

// function getPulleyWidth(pulleyConfiguration is map) returns ValueWithUnits
// {
//     return pulleyConfiguration.width + (pulleyConfiguration.hasFlanges ? pulleyConfiguration.flangeWidth * 2 : 0 * meter);
// }

// function addPulleyInstance(context is Context, instantiator is Instantiator, pulleyConfiguration is map)
// {
//     addInstance(instantiator, Pulley::build, {
//                 "configuration" : pulleyConfiguration,
//                 "transform" : toWorld(location),
//                 "identity" : identity
//             });

//     if (pulleyConfiguration.boreType != BoreType.NONE)
//     {
//         addInstance(instantiator, Bore::build, {
//                     "configuration" : pulleyConfiguration,
//                     "transform" : toWorld(location),
//                     "identity" : identity
//                 });
//     }
// }
