FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "onshape/std/hole.fs", version : "2960.0");
export import(path : "onshape/std/mateconnectoraxistype.gen.fs", version : "2960.0");

import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
// Exports Fit, a parameter type
export import(path : "926d933eb33b11a3452660fd", version : "734a856ee6a464616f05e7e4");

export enum ComponentType
{
    annotation { "Name" : "Motor" }
    MOTOR,
    annotation { "Name" : "Gearbox" }
    GEARBOX
}

export enum MotorType
{
    annotation { "Name" : "Falcon 500" }
    FALCON_500,
    annotation { "Name" : "NEO" }
    NEO,
    annotation { "Name" : "NEO 550" }
    NEO_550,
    annotation { "Name" : "775pro" }
    _775_PRO,
    annotation { "Name" : "CIM" }
    CIM,
    annotation { "Name" : "Bag" }
    BAG
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

// export enum MotorControllerType
// {
//     annotation { "Name" : "Spark MAX" }
//     SPARK_MAX,
//     annotation { "Name" : "Victor SPX" }
//     VICTOR_SPX,
//     annotation { "Name" : "Talon SRX" }
//     TALON_SRX,
//     annotation { "Name" : "Spark" }
//     SPARK,
// }

predicate isMotorSquare(definition is map)
{
    (definition.componentType == ComponentType.GEARBOX &&
            definition.gearboxType != GearboxType.MAX_PLANETARY &&
            definition.gearboxType != GearboxType.ULTRA_PLANETARY);
}

annotation {
        "Feature Type Name" : "Robot motor",
        "Editing Logic Function" : "robotMotorEditLogic",
        "Manipulator Change Function" : "robotMotorManipulatorChange",
        "Feature Type Description" : "Creates a variety of motor mounting patterns for motors and gearboxes.<br>" ~
        "FeatureScript by Alex Kempen."
    }
export const robotMotor = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Component type", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.componentType is ComponentType;

        if (definition.componentType == ComponentType.MOTOR)
        {
            annotation { "Name" : "Motor type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.motorType is MotorType;
        }
        else if (definition.componentType == ComponentType.GEARBOX)
        {
            annotation { "Name" : "Gearbox type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.gearboxType is GearboxType;
        }

        // Of the mounting holes' screws (#10, or M3 or M4 for metric motors)
        fitPredicate(definition);

        // Of the hole for the motor's boss
        boreFitPredicate(definition);

        annotation { "Name" : "All mounting holes", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.allHoles is boolean;

        if (definition.motorType == MotorType._775_PRO)
        {
            annotation { "Name" : "Vent holes", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.ventHoles is boolean;
        }

        annotation { "Name" : "Sketch points to place motors", "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR }
        definition.locations is Query;

        annotation { "Name" : "Flip primary axis", "UIHint" : ["OPPOSITE_DIRECTION", "FIRST_IN_ROW"] }
        definition.flipPrimary is boolean;

        annotation { "Name" : "Reorient secondary axis", "UIHint" : ["MATE_CONNECTOR_AXIS_TYPE"] }
        definition.secondaryAxisType is MateConnectorAxisType;

        annotation { "Name" : "Angle" }
        isAngle(definition.angle, ANGLE_360_ZERO_DEFAULT_BOUNDS);

        annotation { "Name" : "Opposite direction", "UIHint" : UIHint.OPPOSITE_DIRECTION_CIRCULAR }
        definition.oppositeDirection is boolean;

        annotation { "Name" : "Reference direction", "Filter" : (QueryFilterCompound.ALLOWS_VERTEX || QueryFilterCompound.ALLOWS_DIRECTION), "MaxNumberOfPicks" : 1 }
        definition.direction is Query;

        annotation { "Name" : "Merge scope", "Filter" : EntityType.BODY && BodyType.SOLID && ModifiableEntityOnly.YES && ActiveSheetMetal.NO }
        definition.booleanScope is Query;
    }
    {
        doRobotMotor(context, id, definition);
    });

function getMotorPattern(definition is map) returns Pattern
{
    return pattern(definition, (definition.componentType == ComponentType.MOTOR) ?
            MOTOR_PATTERNS[definition.motorType] :
            GEARBOX_PATTERNS[definition.gearboxType]);
}

function adjustPlanePosition(context is Context, definition is map, plane is Plane) returns Plane
{
    if (isQueryEmpty(context, definition.direction))
    {
        return plane;
    }

    if (!isQueryEmpty(context, definition.direction->qEntityFilter(EntityType.VERTEX)) || !isQueryEmpty(context, definition.direction->qBodyType(BodyType.MATE_CONNECTOR)))
    {
        const point = project(plane, evVertexPoint(context, { "vertex" : definition.direction }));
        plane.x = normalize(point - plane.origin);
    }
    else
    {
        plane.x = worldToPlane3D(plane, extractDirection(context, definition.direction) * meter)->normalize();
    }
    return plane;
}

function getStartAngle(definition is map) returns ValueWithUnits
{
    var angle = (isMotorSquare(definition) ? 45 : 0) * degree + 90 * degree;
    if (definition.secondaryAxisType == MateConnectorAxisType.PLUS_Y)
    {
        angle += 90 * degree;
    }
    else if (definition.secondaryAxisType == MateConnectorAxisType.MINUS_X)
    {
        angle += 180 * degree;
    }
    else if (definition.secondaryAxisType == MateConnectorAxisType.MINUS_Y)
    {
        angle += 270 * degree;
    }
    return angle;
}

function getClockAngle(definition is map) returns ValueWithUnits
{
    return definition.angle * (definition.oppositeDirection ? -1 : 1);
}

function doRobotMotor(context is Context, id is Id, definition is map)
{
    var remainingTransform = getRemainderPatternTransform(context, { "references" : definition.locations });
    definition.locations = verifyNonemptyQuery(context, definition, "location", "Select motor locations.");
    verifyNonemptyQuery(context, definition, "booleanScope", ErrorStringEnum.HOLE_EMPTY_SCOPE);

    var motorPattern = getMotorPattern(definition);

    const diagonalLength = boundingBoxLength(context, definition.booleanScope);

    for (var i, location in definition.locations)
    {
        const motorId = id + "motor" + unstableIdComponent(i);
        setExternalDisambiguation(context, motorId, location);

        var plane = evVertexCoordSystem(context, { "vertex" : location })->plane();
        plane = adjustPlanePosition(context, definition, plane);
        const startAngle = getStartAngle(definition);
        const clockAngle = getClockAngle(definition);

        if (i == 0)
        {
            addAngularManipulator(context, id, plane, startAngle, clockAngle, motorPattern.bodyDiameter / 2);
        }

        plane = rotationAround(line(plane.origin, plane.normal), startAngle + clockAngle) * plane;

        const centerVertex = createMotorSketch(context, motorId + "sketch", definition, motorPattern, plane);

        opExtrude(context, motorId + "extrude", {
                    "entities" : qCreatedBy(motorId + "sketch", EntityType.FACE),
                    "direction" : plane.normal * (definition.flipPrimary ? 1 : -1),
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : diagonalLength
                });

        callSubfeatureAndProcessStatus(id, hole, context, motorId + "hole", {
                    "locations" : qCreatedBy(motorId + "sketch", EntityType.VERTEX)->qSubtraction(centerVertex),
                    "holeDiameter" : motorPattern.holeDiameter,
                    "endStyle" : HoleEndStyle.THROUGH,
                    "scope" : definition.booleanScope,
                    "oppositeDirection" : definition.flipPrimary
                },
            {
                    "featureParameterMap" : { "scope" : "booleanScope" },
                    "additionalErrorEntities" : qCreatedBy(motorId + "extrude", EntityType.BODY),
                    "propagateErrorDisplay" : true
                });
    }

    transformResultIfNecessary(context, id, remainingTransform);
    opBoolean(context, id + "boolean", {
                "tools" : qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID),
                "targets" : definition.booleanScope,
                "operationType" : BooleanOperationType.SUBTRACTION
            });

    opDeleteBodies(context, id + "cleanup", { "entities" : qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES) });
}

/**
 * @return {Query} : A query for the center point of the sketch.
 */
function createMotorSketch(context is Context, id is Id, definition is map, motorPattern is Pattern, plane is Plane) returns Query
{
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    const bossHole = motorPattern.bossDiameter + boreFitClearance(definition, motorPattern.bossDiameter);
    const centerId = skCircle(sketch, "circle", { "radius" : bossHole / 2 }).centerId;
    drawMountingHoles(sketch, motorPattern, definition.allHoles);
    skSolve(sketch);
    return sketchEntityQuery(id, EntityType.VERTEX, centerId);
}

const ANGULAR_MANIPULATOR = "angularManipulator";

function addAngularManipulator(context is Context, id is Id, plane is Plane, startAngle is ValueWithUnits, clockAngle is ValueWithUnits, rotationRadius is ValueWithUnits)
{
    const rotatedPlane = rotationAround(line(plane.origin, plane.normal), startAngle) * plane;
    addManipulators(context, id, { (ANGULAR_MANIPULATOR) : angularManipulator({
                        "axisOrigin" : plane.origin,
                        "axisDirection" : plane.normal,
                        "rotationOrigin" : plane.origin + rotatedPlane.x * rotationRadius,
                        "angle" : clockAngle,
                        "minValue" : -2 * PI * radian,
                        "maxValue" : 2 * PI * radian,
                        "primaryParameterId" : "clockAngle"
                    })
            });
}

type Pattern typecheck canBePattern;

predicate canBePattern(value)
{
    value is map;
    value.holeType is HoleType;
    value.bodyType is MotorBodyType;

    value.bodyDiameter is ValueWithUnits;
    value.boltCircleDiameter is ValueWithUnits;
    value.bossDiameter is ValueWithUnits;

    value.numberOfHoles is number;
    value.holeDiameter is ValueWithUnits;

    if (value.numberOfHoles == 4)
    {
        value.verticalAngle is ValueWithUnits;
    }
}

/**
 * @param value {{
 *      @field holeType {HoleType} :
 *      @field bodyType {MotorBodyType} : @optional
 *              Defaults to `MotorBodyType.CIRCLE`.
 *      @field bodyDiameter {ValueWithUnits} :
 *      @field boltCircleDiameter {ValueWithUnits} :
 *      @field bossDiameter {ValueWithUnits} :
 *      @field numberOfHoles {number} :
 *      @field verticalAngle {ValueWithUnits} : @optional
 *              Defaults to `90 * degree`.
 * }}
 */
function pattern(definition is map, value is map) returns Pattern
{
    value.holeDiameter = getHoleDiameter(definition, value.holeType);
    return mergeMaps({ "verticalAngle" : 90 * degree, "bodyType" : MotorBodyType.CIRCLE }, value) as Pattern;
}

function getHoleDiameter(definition is map, holeType is HoleType) returns ValueWithUnits
{
    return fastenerHoleDiameter(definition, switch (holeType) {
                HoleType.NUMBER_10 : "#10",
                HoleType.M3 : "M3",
                HoleType.M4 : "M4"
            });
}

function drawMountingHoles(sketch is Sketch, motorPattern is Pattern, createAllHoles is boolean)
{
    // always create two holes
    motorPoints(sketch, "horizontal", 0 * degree, motorPattern.boltCircleDiameter);

    if (motorPattern.numberOfHoles == 4 && createAllHoles)
    {
        motorPoints(sketch, "vertical", motorPattern.verticalAngle, motorPattern.boltCircleDiameter);
    }
    else if (motorPattern.numberOfHoles == 6 && createAllHoles)
    {
        motorPoints(sketch, "upperRight", 60 * degree, motorPattern.boltCircleDiameter);
        motorPoints(sketch, "upperLeft", 120 * degree, motorPattern.boltCircleDiameter);
    }
}

/**
 * Adds a pair of points to `sketch`. The points are defined by `angle` and `diameter`.
 */
function motorPoints(sketch is Sketch, sketchId is string, angle is ValueWithUnits, diameter is ValueWithUnits)
{
    const vector = vector(cos(angle), sin(angle)) * (diameter / 2);
    skPoint(sketch, sketchId ~ "first", { "position" : vector });
    skPoint(sketch, sketchId ~ "second", { "position" : -vector });
}

export function robotMotorManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    if (newManipulators[ANGULAR_MANIPULATOR] is Manipulator)
    {
        const newAngle = newManipulators[ANGULAR_MANIPULATOR].angle;
        definition.angle = abs(newAngle);
        definition.oppositeDirection = newAngle < 0 * degree;
    }
    return definition;
}


enum HoleType
{
    NUMBER_10,
    M3,
    M4
}

enum MotorBodyType
{
    CIRCLE,
    SQUARE
}

const CIM_DEFAULTS = {
        "boltCircleDiameter" : 2 * inch,
        "bodyDiameter" : 60 * millimeter,
        "bossDiameter" : 0.75 * inch,
        "bodyType" : MotorBodyType.CIRCLE,
        "numberOfHoles" : 4,
        "holeType" : HoleType.NUMBER_10
    };

const MOTOR_PATTERNS = {
        MotorType.FALCON_500 : mergeMaps(CIM_DEFAULTS, { "numberOfHoles" : 6 }),
        MotorType.NEO : CIM_DEFAULTS,
        MotorType.CIM : CIM_DEFAULTS,
        MotorType.NEO_550 : {
            "boltCircleDiameter" : 25 * millimeter,
            "bodyDiameter" : 35 * millimeter,
            "bossDiameter" : 13 * millimeter,
            "numberOfHoles" : 4,
            "holeType" : HoleType.M3,
            "bodyType" : MotorBodyType.CIRCLE
        },
        MotorType._775_PRO : {
            "boltCircleDiameter" : 29 * millimeter,
            "bodyDiameter" : 44.3 * millimeter,
            "bossDiameter" : 17.5 * millimeter,
            "numberOfHoles" : 4,
            "holeType" : HoleType.M4,
            "bodyType" : MotorBodyType.CIRCLE
        },
    };

const GEARBOX_PATTERNS = {
        GearboxType.MAX_PLANETARY : mergeMaps(CIM_DEFAULTS, { "verticalAngle" : 45 * degree }),
        GearboxType.ULTRA_PLANETARY : {
            "boltCircleDiameter" : 32 * millimeter,
            "bodyDiameter" : 1.732 * inch,
            "bossDiameter" : 22 * millimeter,
            "numberOfHoles" : 6,
            "holeType" : HoleType.M3,
            "bodyType" : MotorBodyType.CIRCLE
        },
        GearboxType.VERSA_PLANETARY : {
            "boltCircleDiameter" : 2 * inch,
            "bodyDiameter" : 1.75 * inch,
            "bossDiameter" : .75 * inch,
            "numberOfHoles" : 4,
            "holeType" : HoleType.NUMBER_10,
            "bodyType" : MotorBodyType.SQUARE
        },
        GearboxType.SPORT : {
            "boltCircleDiameter" : 2 * inch,
            "bodyDiameter" : 1.75 * inch,
            "bossDiameter" : 1.5 * inch,
            "numberOfHoles" : 4,
            "holeType" : HoleType.NUMBER_10,
            "bodyType" : MotorBodyType.SQUARE
        },
    };


export function robotMotorEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, specifiedParameters is map, hiddenBodies is Query) returns map
{
    definition = autoSelectParts(context, oldDefinition, definition, specifiedParameters, hiddenBodies);
    return definition;
}


// The following code is adapted from the Lighten Featurescript
function autoSelectParts(context is Context, oldDefinition is map, definition is map,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    if (definition.motorLocations == oldDefinition.motorLocations || specifiedParameters.booleanScope)
    {
        return definition;
    }

    const targetQueries = qAllModifiableSolidBodiesNoMesh()->qSubtraction(hiddenBodies);
    const collisions = try(evCollision(context, { "tools" : definition.motorLocations, "targets" : targetQueries }));
    return mergeMaps(definition, { "booleanScope" : collisions == undefined ?
                qNothing() : extractFromArrayOfMaps(collisions, "targetBody")->qUnion()
            });
}
