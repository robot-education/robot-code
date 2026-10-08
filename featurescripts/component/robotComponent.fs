FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "6b7d5995c565ae73c7740b0b");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0195d390c3944cd4fab21ce0", version : "2087a92c024fe3ea73f587fa");
export import(path : "58d66340f7b70cfc86606676", version : "c66f2cde90ee0c14ff94cd63");

import(path : "01402b7c9eebd8bf0b5d3e52", version : "afd3970cf2628429b3763f68");

export enum ComponentType
{
    annotation { "Name" : "Power" }
    POWER,
    annotation { "Name" : "Control" }
    CONTROL,
    annotation { "Name" : "Motor controller" }
    MOTOR_CONTROLLER,
    // annotation { "Name" : "Pnuematic" }
    // PNEUMATIC,
    // annotation { "Name" : "Sensor" }
    // SENSOR,
    annotation { "Name" : "Deprecated" }
    DEPRECATED
}

// export enum ComponentMountingPattern
// {
//     annotation { "Name" : "Talon SRX/Victor SPX" }
//     CTRE,
//     annotation { "Name" : "SPARK MAX (Ziptie)" }
//     SPARKMAX,
//     annotation { "Name" : "PCM" }
//     PCM,
//     annotation { "Name" : "VRM" }
//     VRM,
//     annotation { "Name" : "PDP" }
//     PDP,
//     annotation { "Name" : "RoboRIO" }
//     ROBORIO,
//     annotation { "Name" : "Radio OM5P" }
//     OM5P,
//     annotation { "Name" : "PDH" }
//     PDH,
//     annotation { "Name" : "Pneumatics Hub" }
//     PHUB,
//     annotation { "Name" : "Mini Power Module" }
//     MPM,
// }

export enum MotorController
{
    annotation { "Name" : "SPARK MAX" }
    SPARK_MAX,
    annotation { "Name" : "Victor SPX" }
    VICTOR_SPX,
    annotation { "Name" : "Talon SRX" }
    TALON_SRX
}

export enum PowerComponent
{
    annotation { "Name" : "Power distribution hub (PDH)" }
    POWER_DISTRIBUTION_HUB,
    annotation { "Name" : "Power distribution panel (PDP)" }
    POWER_DISTRIBUTION_PANEL,
    annotation { "Name" : "120 amp main breaker" }
    MAIN_BREAKER,
    annotation { "Name" : "Robot signal light (RSL)" }
    ROBOT_SIGNAL_LIGHT,

}

export enum ControlComponent
{
    annotation { "Name" : "roboRIO 2.0" }
    ROBORIO,
    annotation { "Name" : "Power distribution hub (PDH)" }
    POWER_DISTRIBUTION_HUB,
    annotation { "Name" : "Power distribution panel (PDP)" }
    POWER_DISTRIBUTION_PANEL,
    annotation { "Name" : "Main breaker" }
    MAIN_BREAKER,
    annotation { "Name" : "VH-109 radio" }
    ROBOT_RADIO,
    annotation { "Name" : "Robot signal light (RSL)" }
    ROBOT_SIGNAL_LIGHT,
}

export enum PowerDistrbutionType
{
    annotation { "Name" : "REV power distribution hub" }
    REV_PDH,
    annotation { "Name" : "CTRE power distribution panel" }
    CTRE_PDP
}


annotation {
        "Feature Type Name" : "Robot component",
        "Feature Type Description" : "Cuts mounting holes for robotics components." ~ CREDIT,
        "Manipulator Change Function" : "robotComponentManipulatorChange",
        "Editing Logic Function" : "robotComponentEditLogic",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotComponent = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {

        annotation { "Group Name" : "Selections", "Collapsed By Default" : false }
        {
            locationPredicate(definition, "component");

            axisOrientationPredicate(definition);

            angleReferencePredicate(definition);

            angleOffsetPredicate(definition);

            holeMergeScopePredicate(definition);
        }

        annotation { "Group Name" : "Component", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Component type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.componentType is ComponentType;

            if (definition.componentType == ComponentType.POWER)
            {
                annotation { "Name" : "Power component", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.powerComponent is PowerComponent;
            }
            else if (definition.componentType == ComponentType.CONTROL)
            {
                annotation { "Name" : "Control component", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.controlComponent is ControlComponent;
            }

            holeDiameterPredicate(definition);
        }
    }
    {
        var plane = getLocationPlane(context, definition);
        plane = applyAngleReference(context, definition, plane);
        plane = applyAxisOrientation(definition, plane);
        addAngleOffsetManipulator(context, id, definition, plane, 3 * inch);
        plane = applyAngleOffset(definition, plane);

        const sketchId = id + "mountingPattern";
        const faces = sketchMountingPattern(context, sketchId, definition, plane);

        opExtrude(context, id + "extrude", {
                    "entities" : faces,
                    "direction" : -plane.normal,
                    "endBound" : BoundingType.THROUGH_ALL
                });

        try
        {
            opBoolean(context, id + "boolean", {
                        "tools" : qCreatedBy(id + "extrude", EntityType.BODY),
                        "targets" : definition.scope,
                        "operationType" : BooleanOperationType.SUBTRACTION
                    });
        }
        catch
        {
            const errorId = id + "error";
            const faces = sketchMountingPattern(context, errorId + "sketch", definition, plane);
            opExtrude(context, errorId + "extrude", {
                        "entities" : faces,
                        "direction" : -plane.normal,
                        "endBound" : BoundingType.THROUGH_ALL
                    });
            throw regenError(ErrorStringEnum.HOLE_NO_HITS, ["scope"], qCreatedBy(errorId + "extrude", EntityType.BODY));
        }
        cleanup(context, id + "delete", qCreatedBy(sketchId, EntityType.BODY));
    });

function sketchMountingPattern(context is Context, id is Id, definition is map, plane is Plane) returns Query
{
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    skCircle(sketch, "circle1", {
                "center" : vector(1, 1) * inch,
                "radius" : definition.holeDiameter
            });

    skCircle(sketch, "circle2", {
                "center" : vector(-1, -1) * inch,
                "radius" : definition.holeDiameter
            });

    skSolve(sketch);
    return qSketchRegion(id);
}

export function robotComponentManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return angleOffsetManipulatorChange(definition, newManipulators);
}

export function robotComponentEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    isCreating is boolean, specifiedParameters is map, hiddenBodies is Query) returns map
{
    definition = mountingEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);

    return definition;
}
