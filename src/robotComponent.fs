FeatureScript 1948;
import(path : "onshape/std/common.fs", version : "1948.0");

import(path : "4f42869c6ffe2c26fe065d56", version : "f2dcacc6e35597334f1895e9");

export enum ComponentType
{
    annotation { "Name" : "Motor" }
    MOTOR,
    annotation { "Name" : "Gearbox" }
    GEARBOX,
    annotation { "Name" : "Required" }
    REQUIRED,
    annotation { "Name" : "Motor controller" }
    MOTOR_CONTROLLER,
    annotation { "Name" : "Pnuematic" }
    PNEUMATIC,
    annotation { "Name" : "Sensor" }
    SENSOR,
    annotation { "Name" : "Other" }
    OTHER
}

export enum MotorType
{
    annotation { "Name" : "Falcon 500" }
    FALCON_500,
    annotation { "Name" : "NEO" }
    NEO,
    annotation { "Name" : "CIM" }
    CIM,
    annotation { "Name" : "NEO 550" }
    NEO_550,
    annotation { "Name" : "775pro" }
    _775_PRO,
    annotation { "Name" : "BAG" }
    BAG,
    annotation { "Name" : "BaneBots 550" }
    _550
}

export enum GearboxType
{
    annotation { "Name" : "MAXPlanetary" }
    MAX_PLANETARY,
    annotation { "Name" : "UltraPlanetary" }
    ULTRA_PLANETARY,
    annotation { "Name" : "VersaPlanetary" }
    VERSA_PLANETARY,
    annotation { "Name" : "Sport" }
    SPORT
}

export enum MotorControllerType
{
    annotation { "Name" : "Spark max" }
    SPARK_MAX,
    annotation { "Name" : "Victor SPX" }
    VICTOR_SPX,
    annotation { "Name" : "Talon SRX" }
    TALON_SRX,
    annotation { "Name" : "Spark" }
    SPARK
}

export enum RequiredComponentType
{
    annotation { "Name" : "roboRIO 2.0" }
    ROBORIO,
    annotation { "Name" : "Power distribution" }
    POWER_DISTRIBUTION,
    annotation { "Name" : "120 amp main breaker" }
    MAIN_BREAKER,
    // annotation { "Name" : "Battery" }
    // BATTERY,
    annotation { "Name" : "Robot signal light (RSL)" }
    ROBOT_SIGNAL_LIGHT,
    annotation { "Name" : "Robot radio" }
    ROBOT_RADIO
}

export enum PowerDistrbutionType
{
    annotation { "Name" : "REV power distribution hub" }
    REV_PDH,
    annotation { "Name" : "CTRE power distribution panel" }
    CTRE_PDP
}

export enum MainBreakerType
{
    annotation { "Name" : "Standard" }
    STANDARD,
    annotation { "Name" : "Eaton bussman" }
    EATON_BUSSMAN
}

export enum PneumaticType
{
    annotation { "Name" : "Manifold" }
    MANIFOLD,
    annotation { "Name" : "Compressor" }
    COMPRESSOR
}


export enum ManifoldSize
{
    THREE,
    FOUR,
    FIVE,
    SIX
}

export enum SensorType
{
    annotation { "Name" : "Limelight camera" }
    LIME_LIGHT,
}

export enum OtherComponentType
{
    annotation { "Name" : "" }
    CAMERA,
}

// export predicate motorPredicate(definition is map)
// {
//     annotation { "Name" : "Motor type" }
//     definition.motorType is MotorType;
// }

// export predicate gearboxPredicate(definition is map)
// {
//     annotation { "Name" : "Gearbox type" }
//     definition.gearboxType is GearboxType;
// }

export predicate requiredPredicate(definition is map)
{

}

export predicate motorControllerPredicate(definition is map)
{

}

export predicate pneumaticPredicate(definition is map)
{

}

export predicate sensorPredicate(definition is map)
{

}

export predicate otherPredicate(definition is map)
{

}


/**
 * Create various FRC components. Allows the creation of mounting patterns (either by tagging or booleaning directly) as well as a surface representation of the actual component.
 */
annotation { "Feature Type Name" : "Robot component", "Editing Logic Function" : "autoSelectParts",
        "Feature Type Description" : "Creates a variety of motor mounting patterns for common FRC motors and gearboxes.<br>" ~
        "FeatureScript by Alex Kempen."
    }
export const robotComponent = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Component type", "UIHint" : ["ALWAYS_HIDDEN"] }
        definition.componentType is ComponentType;

        // outputTypePredicate(definition);

        annotation { "Name" : "Component", "Lookup Table" : componentTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "UNCONFIGURABLE"] }
        definition.component is LookupTablePath;

        // if (definition.componentType == ComponentType.MOTOR)
        // {

        // }
        // else if (definition.componentType == ComponentType.GEARBOX)
        // {

        // }
        // else if (definition.componentType == ComponentType.REQUIRED)
        // {
        //     requiredPredicate(definition);
        // }
        // else if (definition.componentType == ComponentType.MOTOR_CONTROLLER)
        // {
        //     motorControllerPredicate(definition);
        // }
        // else if (definition.componentType == ComponentType.PNEUMATIC)
        // {
        //     pneumaticPredicate(definition);
        // }
        // else if (definition.componentType == ComponentType.SENSOR)
        // {
        //     sensorPredicate(definition);
        // }
        // else if (definition.componentType == ComponentType.OTHER)
        // {
        //     otherPredicate(definition);
        // }

        // annotation { "Name" : "Motor locations", "Filter" : QueryFilterCompound.ALLOWS_VERTEX }
        // definition.motorLocations is Query;

        // annotation { "Name" : "Motor type", "UIHint" : UIHint.SHOW_LABEL }
        // definition.motorType is MotorType;

        // annotation { "Name" : "All mounting holes" }
        // definition.allHoles is boolean;

        // if (definition.motorType == MotorType._775 || definition.motorType == MotorType._550)
        // {
        //     annotation { "Name" : "Vent Holes", "Default" : false }
        //     definition.ventHoles is boolean;
        // }

        // annotation { "Name" : "Clock angle" }
        // isAngle(definition.clockAngle, ANGLE_360_ZERO_DEFAULT_BOUNDS);

        // annotation { "Name" : "Reorient motor", "UIHint" : UIHint.OPPOSITE_DIRECTION_CIRCULAR }
        // definition.rotateMotor is boolean;

        // annotation { "Name" : "Angle reference", "Filter" : (QueryFilterCompound.ALLOWS_VERTEX || QueryFilterCompound.ALLOWS_DIRECTION), "MaxNumberOfPicks" : 1 }
        // definition.direction is Query;

        // // ventHoles is off or motorType isn't 775 or 550
        // if (definition.ventHoles == false || !(definition.motorType == MotorType._775 || definition.motorType == MotorType._550))
        // {
        //     annotation { "Name" : "Additional mounting holes", "Item name" : "Mounting hole set" }
        //     definition.clockedHolesArray is array;
        //     for (var clockedHole in definition.clockedHolesArray)
        //     {
        //         annotation { "Name" : "Angle offset" }
        //         isAngle(clockedHole.angleOffset, ANGLE_360_ZERO_DEFAULT_BOUNDS);
        //     }
        // }

        // annotation { "Name" : "Merge scope", "Filter" : EntityType.BODY && BodyType.SOLID }
        // definition.mergeScope is Query;

        // annotation { "Name" : "Add mate connector", "Default" : true }
        // definition.addMateConnector is boolean;

        // annotation { "Name" : "Export motors", "UIHint" : UIHint.ALWAYS_HIDDEN, "Default" : false }
        // definition.exportMotor is boolean;

        // annotation { "Name" : "Plate plane", "UIHint" : UIHint.ALWAYS_HIDDEN }
        // isAnything(definition.platePlane);
    }
    {
        return;
    }); // end of feature definition

function makePatternDefinition(boltCircle is ValueWithUnits, holeDia is ValueWithUnits, bossSize is ValueWithUnits, numHoles is number, bodyDia is ValueWithUnits, circularBody is boolean)
{
    return { "boltCircle" : boltCircle, "holeDia" : holeDia, "bossSize" : bossSize, "numHoles" : numHoles, "bodyDia" : bodyDia, "circularBody" : circularBody };
}

function drawMountingHoles(sketch is Sketch, i is number, angleOffset is ValueWithUnits, mountingRadius is ValueWithUnits, holeRadius is ValueWithUnits, numHoles is number, allHoles is boolean)
{
    // All motors get at least two mounting holes
    skCircle(sketch, "leftHole" ~ i, {
                "center" : mountingRadius * vector(cos(angleOffset), sin(angleOffset)),
                "radius" : holeRadius
            });
    skCircle(sketch, "rightHole" ~ i, {
                "center" : mountingRadius * vector(cos(angleOffset + PI * radian), sin(angleOffset + PI * radian)),
                "radius" : holeRadius
            });

    if (numHoles == 4 && allHoles)
    {
        skCircle(sketch, "topHole" ~ i, {
                    "center" : mountingRadius * vector(cos(angleOffset + PI / 2 * radian), sin(angleOffset + PI / 2 * radian)),
                    "radius" : holeRadius
                });
        skCircle(sketch, "botomHole" ~ i, {
                    "center" : mountingRadius * vector(cos(angleOffset + 3 * PI / 2 * radian), sin(angleOffset + 3 * PI / 2 * radian)),
                    "radius" : holeRadius
                });
    }
    else if (numHoles == 6 && allHoles)
    {
        var angleVector1 is Vector = vector([cos(PI / 3 * radian + angleOffset), sin(PI / 3 * radian + angleOffset)]);
        var angleVector2 is Vector = vector([cos(2 * PI / 3 * radian + angleOffset), sin(2 * PI / 3 * radian + angleOffset)]);

        skCircle(sketch, "upperRightHole" ~ i, {
                    "center" : angleVector1 * mountingRadius,
                    "radius" : holeRadius
                });
        skCircle(sketch, "upperLeftHole" ~ i, {
                    "center" : angleVector2 * mountingRadius,
                    "radius" : holeRadius
                });
        skCircle(sketch, "lowerRightHole" ~ i, {
                    "center" : -angleVector1 * mountingRadius,
                    "radius" : holeRadius
                });
        skCircle(sketch, "lowerleftHole" ~ i, {
                    "center" : -angleVector2 * mountingRadius,
                    "radius" : holeRadius
                });
    }
}

function createSlot(slotAngle is ValueWithUnits, angularSize is ValueWithUnits, slotRadius is ValueWithUnits, slotWidth is ValueWithUnits) returns map
{
    var startAngle = (slotAngle + angularSize / 2);
    var endAngle = (slotAngle - angularSize / 2);

    var startVector is Vector = vector([cos(startAngle), sin(startAngle)]);
    var midVector is Vector = vector([cos(slotAngle), sin(slotAngle)]);
    var endVector is Vector = vector([cos(endAngle), sin(endAngle)]);
    var outerRadius is ValueWithUnits = slotRadius + slotWidth;
    var innerRadius is ValueWithUnits = slotRadius - slotWidth;

    var slotMap is map = {
        "circle1" : { "center" : startVector * slotRadius, "radius" : slotWidth },
        "circle2" : { "center" : endVector * slotRadius, "radius" : slotWidth },
        "arc1" : { "start" : startVector * outerRadius, "end" : endVector * outerRadius, "mid" : midVector * outerRadius },
        "arc2" : { "start" : startVector * innerRadius, "end" : endVector * innerRadius, "mid" : midVector * innerRadius }
    };

    return slotMap;
}

// The following code is from the Lighten Featurescript, written by Ilya Baran (Onshape Inc.) and Morgan Bartlett
export function autoSelectParts(context is Context, id is Id, oldDefinition is map, definition is map,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    if (definition.motorLocations == oldDefinition.motorLocations || specifiedParameters.mergeScope)
        return definition;

    // Look at all the non-hidden bodies
    const targetQueries = qSubtraction(qBodyType(qEverything(EntityType.BODY), BodyType.SOLID), hiddenBodies);
    // Pick the ones that collide with the selected points
    const collisions = try(evCollision(context, { tools : definition.motorLocations, targets : targetQueries }));
    if (collisions == undefined)
    {
        definition.mergeScope = qUnion([]);
        return definition;
    }
    var collidingBodies = [];
    for (var collision in collisions)
    {
        collidingBodies = append(collidingBodies, collision.targetBody);
    }

    definition.mergeScope = qUnion(collidingBodies);

    return definition;
}
