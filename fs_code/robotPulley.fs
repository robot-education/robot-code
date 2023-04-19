FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "c470cc508bab81129ff0d110");

export import(path : "00b10ef1fb1a7418097fc0af", version : "1c5780f138c5c63e16f34d6b");

export import(path : "e269bd2b7266145c47eaf374", version : "feb99fe3056e40ea2665c716");

export enum EndType
{
    annotation { "Name" : "Blind" }
    BLIND,
    annotation { "Name" : "Up to  entity" }
    ENTITY
}

predicate bossPredicate(definition is map)
{
    annotation { "Name" : "Boss" }
    definition.hasBoss is boolean;

    if (definition.hasBoss)
    {
        annotation { "Group Name" : "Boss", "Collapsed By Default" : false, "Driving Parameter" : "hasBoss" }
        {
            annotation { "Name" : "End type" }
            definition.endType is EndType;

            if (definition.endType == EndType.BLIND)
            {
                annotation { "Name" : "Depth" }
                isLength(definition.bossDepth, LENGTH_BOUNDS);
            }
            else
            {
                annotation { "Name" : "Entity", "Filter" : EntityType.FACE && GeometryType.PLANE || EntityType.VERTEX, "MaxNumberOfPicks" : 1 }
                definition.bossEndEntity is Query;
            }

            annotation { "Name" : "Flip boss", "UIHint" : ["OPPOSITE_DIRECTION"] }
            definition.flipBoss is boolean;

            if (definition.endType == EndType.ENTITY)
            {
                annotation { "Name" : "Offset distance", "UIHint" : ["DISPLAY_SHORT"] }
                definition.offsetBossEnd is boolean;

                if (definition.offsetBossEnd)
                {
                    annotation { "Name" : "Offset distance", "UIHint" : ["DISPLAY_SHORT"] }
                    isLength(definition.bossEndOffset, LENGTH_BOUNDS);

                    annotation { "Name" : "Opposite direction", "UIHint" : ["OPPOSITE_DIRECTION"] }
                    definition.flipBossEndOffset is boolean;
                }
            }

            annotation { "Name" : "Diameter" }
            isLength(definition.bossDiameter, LENGTH_BOUNDS);

            annotation { "Name" : "Symmetric" }
            definition.symmetricBoss is boolean;

            if (!definition.symmetricBoss)
            {
                annotation { "Name" : "Second boss" }
                definition.secondBoss is boolean;

                if (definition.secondBoss)
                {
                    annotation { "Name" : "End type" }
                    definition.secondBossEndType is EndType;

                    if (definition.secondBossEndType == EndType.BLIND)
                    {
                        annotation { "Name" : "Depth" }
                        isLength(definition.secondBossDepth, LENGTH_BOUNDS);
                    }
                    else if (definition.secondBossEndType == EndType.ENTITY)
                    {
                        annotation { "Name" : "Entity", "Filter" : EntityType.FACE && GeometryType.PLANE || EntityType.VERTEX, "MaxNumberOfPicks" : 1 }
                        definition.secondBossEndEntity is Query;

                        annotation { "Name" : "Offset distance", "UIHint" : ["DISPLAY_SHORT"] }
                        definition.offsetSecondBossEnd is boolean;

                        if (definition.offsetSecondBossEnd)
                        {
                            annotation { "Name" : "Offset distance", "UIHint" : ["DISPLAY_SHORT"] }
                            isLength(definition.secondBossEndOffset, LENGTH_BOUNDS);

                            annotation { "Name" : "Opposite direction", "UIHint" : ["OPPOSITE_DIRECTION"] }
                            definition.flipSecondBossEndOffset is boolean;
                        }
                    }

                    annotation { "Name" : "Diameter" }
                    isLength(definition.secondBossDiameter, LENGTH_BOUNDS);
                }
            }
        }
    }
}

annotation { "Feature Type Name" : "Robot pulley" }
export const robotPulley = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Belt faces", "Filter" : GeometryType.CYLINDER && EntityType.FACE }
        definition.beltFaces is Query;

        annotation { "Name" : "Width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.width, PULLEY_WIDTH_BOUNDS);

        annotation { "Name" : "Flanges", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.hasFlanges is boolean;

        if (definition.hasFlanges)
        {
            annotation { "Group Name" : "Flanges", "Collapsed By Default" : false, "Driving Parameter" : "hasFlanges" }
            {
                annotation { "Name" : "Flange width", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.flangeWidth, LENGTH_BOUNDS);

                annotation { "Name" : "Flip flange", "UIHint" : "OPPOSITE_DIRECTION" }
                definition.flipFlange is boolean;

                annotation { "Name" : "Flange diameter" }
                isLength(definition.flangeDiameter, LENGTH_BOUNDS);

                annotation { "Name" : "Both sides", "Default" : true }
                definition.bothSides is boolean;
            }
        }

        annotation { "Name" : "Bore" }
        definition.hasBore is boolean;

        if (definition.hasBore)
        {
            annotation { "Group Name" : "Bore", "Collapsed By Default" : false, "Driving Parameter" : "hasBore" }
            {
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

                annotation { "Name" : "Tolerance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.boreTolerance, LENGTH_BOUNDS);

                annotation { "Name" : "Opposite direction", "UIHint" : "OPPOSITE_DIRECTION" }
                definition.oppositeDirection is boolean;

                annotation { "Name" : "Chamfer bore edges", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.chamferEdges is boolean;
            }
        }

        bossPredicate(definition);

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
