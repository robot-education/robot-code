FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "onshape/std/chamfertype.gen.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "2073caea5ae472033c5090d9");

import(path : "452d43a015d17145ad7775e4", version : "e68e283095fa8403f8fa0213");
import(path : "5bee4cc7b6b0575cdb535750", version : "9faaf2e96493e60886247f9d");
import(path : "ea127c07807644fb48d3a1ae", version : "72fbd92d548c811d10a5d2f3");
import(path : "0794d10863d10d98a88c2ab4", version : "90bbee184f6552271649afea");

export import(path : "4d2d3f0157d54e1b6a06420a", version : "b17a9f4837591274d709d92b");

import(path : "6c65805103086c85362ee4b7", version : "06268198ef2566cb246b9f56");

// Custom icon by Eliza Barnett of Team 1745
Icon::import(path : "f7208b20c64f74041af4afa2", version : "3326cee850e70c8a170b912d");

annotation {
        "Feature Type Name" : "Robot belt",
        "Editing Logic Function" : "robotBeltEditLogic",
        "Manipulator Change Function" : "robotBeltManipulatorChange",
        "Icon" : Icon::BLOB_DATA,
        "Feature Type Description" : "Create GT2, HTD, and RT25 belts and position and size them automatically based on model geometry." ~
        "<br>See also the Robot pulley and Robot belt tuner FeatureScripts, which work with this feature directly." ~ CREDIT
    }
export const frcBeltCalculator = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        robotBeltPredicate(definition);
    }
    {
        doRobotBelt(context, id, definition);
    });

function doRobotBelt(context is Context, id is Id, definition is map)
{
    var beltDefinition;
    if (isStandaloneBelt(definition))
    {
        beltDefinition = getStandaloneBeltDefinition(context, id, definition);
    }
    else if (isSimpleBelt(definition))
    {
        beltDefinition = getSimpleBeltDefinition(context, id, definition);
    }
    else
    {
        beltDefinition = getComplexBeltDefinition(context, id, definition);
    }

    addStartOffsetManipulator(context, id, definition, beltDefinition.beltPlane);

    const sketchBeltResult = sketchBelt(context, id + "sketch", beltDefinition);
    const counterClockwise = sketchBeltResult.counterClockwise;
    const beltLoop = sketchBeltResult.beltLoop;
    const arcQueries = sketchBeltResult.arcQueries;

    if (isComplexBelt(definition))
    {
        addBeltFlipManipulators(context, id, beltDefinition, arcQueries, counterClockwise);
        validateComplexBeltLength(context, id, definition, beltLoop, beltDefinition);
    }

    createBelt(context, id + "belt", definition, beltDefinition, beltLoop, counterClockwise, arcQueries);
    const belt = qCreatedBy(id + "belt", EntityType.BODY);

    if (definition.addMateConnectors)
    {
        createMateConnectors(context, id + "mateConnectors", beltDefinition, belt);
    }
    cleanup(context, id + "delete", qCreatedBy(id + "sketch", EntityType.BODY));
}

/**
 * A type defining belt information used for modeling.
 * @type {{
 *      @field beltPlane {Plane} : A plane defining the position of the belt.
 *              Its `x` axis should be arbitrary in order to be consistent with other belts.
 * }}
 */
type BeltDefinition typecheck canBeBeltDefinition;

export predicate canBeBeltDefinition(value)
{
    value.beltPlane is Plane;
    value.beltType is BeltType;
    value.beltWidth is ValueWithUnits;
    value.beltTeeth is number;

    value.pulleyDefinitions is array;
    for (var pulleyDefinition in value.pulleyDefinitions)
    {
        pulleyDefinition is PulleyDefinition;
    }
}

/**
 * A type defining pulley information used for modeling.
 *
 * @type {{
 *      @field location {Vector} : A 2D point representing the location of the pulley relative to the belt plane.
 * }}
 */
type PulleyDefinition typecheck canBePulleyDefinition;

export predicate canBePulleyDefinition(value)
{
    value.identity is Query || value.identity is undefined;
    value.location is Vector;
    value.pulleyType is PulleyType;
    if (isPulley(value.pulleyType))
    {
        value.pulleyTeeth is number;
    }
    else
    {
        value.idlerRadius is ValueWithUnits;
    }
}

function getStandaloneBeltDefinition(context is Context, id is Id, definition is map) returns BeltDefinition
{
    const pulleyTeeth = [definition.pulleyOneTeeth, definition.pulleyTwoTeeth];
    const beltValue = getBeltValue(getBeltTableAndPath(definition));
    const centerToCenter = computeBeltCenterToCenter(getBeltPitch(beltValue.beltType), definition.beltTeeth, pulleyTeeth);

    /**
     * The selected 100T belt has a 3.45 in center to center distance.
     */
    reportFeatureInfo(context, id, "The selected belt has a " ~ makeValueString(definition.unitSystem, centerToCenter) ~ " center to center distance.");

    const pulleyDefinitions = [
            {
                    "location" : zeroVector(2) * meter,
                    "pulleyType" : PulleyType.INSIDE_PULLEY,
                    "pulleyTeeth" : pulleyTeeth[0]
                } as PulleyDefinition,
            {
                    "location" : vector(centerToCenter, 0 * meter),
                    "pulleyType" : PulleyType.INSIDE_PULLEY,
                    "pulleyTeeth" : pulleyTeeth[1]
                } as PulleyDefinition
        ];

    return {
                "beltPlane" : YZ_PLANE,
                "beltType" : beltValue.beltType,
                "beltWidth" : beltValue.beltWidth,
                "beltTeeth" : definition.beltTeeth,
                "pulleyDefinitions" : pulleyDefinitions
            } as BeltDefinition;
}


function getPulleyType(definition is map, pulley is map) returns PulleyType
{
    if (pulley.beltSide == BeltSide.INSIDE)
    {
        return PulleyType.INSIDE_PULLEY;
    }
    return isDoubleSidedBelt(definition) ? PulleyType.OUTSIDE_PULLEY : PulleyType.IDLER;
}

function getComplexBeltDefinition(context is Context, id is Id, definition is map) returns BeltDefinition
{
    verifyNonemptyArray(context, definition, "pulleys", "Add pulley locations.");
    if (size(definition.pulleys) < 2)
    {
        throw regenError("You must have at least two pulleys.", ["pulleys"]);
    }

    const beltValue = getBeltValue(getBeltTableAndPath(definition));

    var pulleyLocations = [];
    var pulleyTeeth = [];
    for (var i, pulley in definition.pulleys)
    {
        verifyNonemptyArrayQuery(context, definition, arrayParameterId("pulleys", i, "pulleySelection"), "Select a pulley location.");

        if (pulley.selectionType == SelectionType.GEOMETRY)
        {
            const point = evVertexPoint(context, { "vertex" : pulley.pulleyLocation });
            pulleyLocations = append(pulleyLocations, point);
            pulleyTeeth = append(pulleyTeeth, pulley.pulleyTeeth);
        }
        else if (pulley.selectionType == SelectionType.ROBOT_PULLEY)
        {
            const pulleyInfo = getRobotPulleySelection(context, id, definition, arrayParameterId("pulleys", i, "pulleySelection"), beltValue);
            pulleyLocations = append(pulleyLocations, pulleyInfo.coordSystem.origin);
            pulleyTeeth = append(pulleyTeeth, pulleyInfo.pulleyTeeth);
        }
        else
        {
            const pulleyInfo = getPitchCircleSelection(context, id, definition, arrayParameterId("pulleys", i, "pulleySelection"), beltValue);
            pulleyLocations = append(pulleyLocations, pulleyInfo.coordSystem.origin);
            pulleyTeeth = append(pulleyTeeth, pulleyInfo.pulleyTeeth);
        }
    }

    const beltPlane = getComplexBeltPlane(context, definition);
    var pulleyDefinitions = [];
    for (var pulley, i in definition.pulleys)
    {
        const point = pulleyLocations[i];

        var pulleyDefinition = {
            // worldToPlane projects the point onto the plane
            "identity" : pulley.pulleyLocation,
            "location" : worldToPlane(beltPlane, point),
            "pulleyType" : getPulleyType(definition, pulley)
        };

        if (pulley.selectionType == SelectionType.ROBOT_PULLEY)
        {
            if (!isPointOnPlane(point, beltPlane))
            {
                reportFeatureWarning(context, id, "A selected pulley is not properly aligned with the belt.");
            }

            // We could maybe handle this via the UI, but it's much easier to do it here
            if (pulleyDefinition.pulleyType == PulleyType.IDLER)
            {
                reportFeatureInfo(context, id, "A Robot pulley is being used as an idler.");
            }
        }

        if (isPulley(pulleyDefinition.pulleyType))
        {
            pulleyDefinition.pulleyTeeth = pulleyTeeth[i];
        }
        else
        {
            pulleyDefinition.idlerRadius = pulley.idlerDiameter / 2;
        }
        pulleyDefinitions = append(pulleyDefinitions, pulleyDefinition as PulleyDefinition);
    }

    return {
                "beltPlane" : beltPlane,
                "beltType" : beltValue.beltType,
                "beltWidth" : beltValue.beltWidth,
                "beltTeeth" : definition.beltTeeth,
                "pulleyDefinitions" : pulleyDefinitions
            } as BeltDefinition;
}

function getComplexBeltPlane(context is Context, definition is map) returns Plane
{
    const pulley = definition.pulleys[0];
    var basePlane;
    if (pulley.selectionType == SelectionType.GEOMETRY)
    {
        // verifyNonemptyArrayQuery(context, definition, arrayParameterId("pulleys", i, "pulleyLocation"), "Select a pulley location.");
        basePlane = evVertexCoordSystem(context, { "vertex" : pulley.pulleyLocation })->plane();
    }
    else
    {
        // const pulleyInfo = verifyRobotPulleyQuery(context, id, definition, arrayParmeterId("pulleys", 0, "pulleyLocation"), beltValue);
        basePlane = pulley.pulleyInfo.coordSystem->plane();
    }
    return applyStartOffset(context, definition, basePlane);
}

/**
 * Computes the belt definition.
 *
 * Note error handling is performed in the following order:
 * 1. Selection errors.
 * 2. Center to center distance errors.
 */
function getSimpleBeltDefinition(context is Context, id is Id, definition is map) returns BeltDefinition
precondition
{
    isTopLevelId(id);
}
{
    const beltValue = getBeltValue(getBeltTableAndPath(definition));

    var pulleyTeeth = [];
    var basePlane;
    verifyNonemptyQuery(context, definition, "pulleyOneSelection", "Select a sketch point, circle, mate connector, or Robot pulley to use for pulley one.");
    if (definition.pulleyOneSelectionType == SelectionType.GEOMETRY)
    {
        basePlane = getGeometrySelection(context, id, definition, "pulleyOneSelection");
        pulleyTeeth = append(pulleyTeeth, definition[pulleyString(Pulley.ONE) ~ "Teeth"]);
    }
    else if (definition.pulleyOneSelectionType == SelectionType.ROBOT_PULLEY)
    {
        const pulleyInfo = getRobotPulleySelection(context, id, definition, "pulleyOneSelection", beltValue);
        pulleyTeeth = append(pulleyTeeth, pulleyInfo.teeth);
        basePlane = pulleyInfo.basePlane;
    }
    else
    {
        const pulleyInfo = getPitchCircleSelection(context, id, definition, "pulleyOneSelection", beltValue);
        pulleyTeeth = append(pulleyTeeth, pulleyInfo.pulleyTeeth);
        basePlane = pulleyInfo.basePlane;
    }

    const beltPlane = applyStartOffset(context, definition, basePlane);

    var secondPoint;
    verifyNonemptyQuery(context, definition, "pulleyTwoSelection", "Select a sketch point, circle, mate connector, or Robot pulley to use for pulley two.");
    if (definition.pulleyTwoSelectionType == SelectionType.GEOMETRY)
    {
        pulleyTeeth = append(pulleyTeeth, definition[pulleyString(Pulley.TWO) ~ "Teeth"]);
        secondPoint = computeGeometrySecondPoint(context, id, definition, beltPlane);
    }
    else if (definition.pulleyTwoSelectionType == SelectionType.ROBOT_PULLEY)
    {
        const pulleyInfo = getRobotPulleySelection(context, id, definition, "pulleyTwoSelection", beltValue);
        pulleyTeeth = append(pulleyTeeth, pulleyInfo.teeth);
        secondPoint = computePulleySecondPoint(context, id, definition, beltPlane, pulleyInfo.basePlane.origin);
    }
    else
    {
        const pulleyInfo = getPitchCircleSelection(context, id, definition, "pulleyTwoSelection", beltValue);
        pulleyTeeth = append(pulleyTeeth, pulleyInfo.teeth);
        secondPoint = computePulleySecondPoint(context, id, definition, beltPlane, pulleyInfo.basePlane.origin);
    }

    const secondPointVector = secondPoint - beltPlane.origin;

    const measuredCenterToCenter = norm(secondPointVector);
    const idealCenterToCenter = computeBeltCenterToCenter(getBeltPitch(beltValue.beltType), definition.beltTeeth, pulleyTeeth) + definition.centerToCenterAdjustment;
    var modelCenterToCenter;
    if (validateCenterToCenter(context, id, definition, measuredCenterToCenter, idealCenterToCenter))
    {
        // Use the measured center to center as the model center to center so assemblies and things line up properly
        modelCenterToCenter = measuredCenterToCenter;
    }
    else
    {
        // If the measured center to center is way off, fall back to the ideal center to center
        modelCenterToCenter = idealCenterToCenter;
    }

    // Note: don't use identities in the simple case to improve robustness
    const pulleyDefinitions = [
            {
                    "location" : zeroVector(2) * meter,
                    "pulleyType" : PulleyType.INSIDE_PULLEY,
                    "pulleyTeeth" : pulleyTeeth[0],
                } as PulleyDefinition,
            {
                    // Remake the second point so it's properly on the beltPlane and accounts for differing center to center
                    "location" : worldToPlane(beltPlane, beltPlane.origin + normalize(secondPointVector) * modelCenterToCenter),
                    "pulleyType" : PulleyType.INSIDE_PULLEY,
                    "pulleyTeeth" : pulleyTeeth[1],
                } as PulleyDefinition
        ];

    return {
                "beltPlane" : beltPlane,
                "beltMode" : BeltMode.SIMPLE,
                "beltType" : beltValue.beltType,
                "beltWidth" : beltValue.beltWidth,
                "beltTeeth" : definition.beltTeeth,
                "pulleyDefinitions" : pulleyDefinitions
            } as BeltDefinition;
}

/**
 * Evaluates a geometry selection.
 */
function getGeometrySelection(context is Context, id is Id, definition is map, parameterName is string) returns Plane
{
    // This function takes id but doesn't use it, we leave it in case we need to report a feature warning in the future
    const selection = getParameter(definition, parameterName);
    if (isCircle(context, selection))
    {
        return evCurveDefinition(context, { "edge" : selection }).coordSystem->plane();
    }
    else if (isVertex(context, selection) || isMateConnector(context, selection))
    {
        return evVertexCoordSystem(context, { "vertex" : selection })->plane();
    }
    throw regenError("Select a sketch point, circle, or mate connector.", [parameterName], selection);
}

/**
 * Evaluates a Robot Pulley selection.
 */
function getRobotPulleySelection(context is Context, id is Id, definition is map, parameterName is string, beltValue is map) returns map
{
    var pulley = getParameter(definition, parameterName);
    if (isMateConnector(context, pulley))
    {
        pulley = qOwnerBody(pulley);
    }

    const pulleyAttribute = getAttribute(context, {
                "entity" : pulley,
                "name" : PULLEY_ATTRIBUTE
            });
    if (pulleyAttribute == undefined || !canBePulleyAttribute(pulleyAttribute))
    {
        throw regenError("Selection was not created by an up-to-date Robot pulley FeatureScript.", [parameterName], pulley);
    }

    if (!isMateConnector(context, pulley) && pulleyAttribute.twoBelts)
    {
        throw regenError("Cannot add a belt to a two belt pulley directly. Select one of its mate connectors instead.", [parameterName], pulley);
    }

    if (pulleyAttribute.beltType != beltValue.beltType)
    {
        reportFeatureWarning(context, id, "The selected pulley is for a different type of belt.");
    }

    // First coordSystem is a PersistentCoordSystem
    const coordSystem = pulleyAttribute.coordSystem.coordSystem;
    return {
            "basePlane" : coordSystem->plane(),
            "pulleyTeeth" : pulleyAttribute.pulleyTeeth
        };
}

/**
 * Evaluates a pitch circle selection.
 */
function getPitchCircleSelection(context is Context, id is Id, definition is map, parameterName is string, beltValue is map)
{
    const selection = getParameter(definition, parameterName);
    const isCircle = isCircle(context, selection);
    if (!isCircle)
    {
        throw regenError("Selection is not a valid pitch circle.", [parameterName], selection);
    }
    const circle = evCurveDefinition(context, { "edge" : definition.pulleyOneSelection });

    const targetPulleyTeeth = computeTargetPulleyTeeth(getBeltPitch(beltValue.beltType), circle.radius);
    if (!isPositiveInteger(targetPulleyTeeth))
    {
        setErrorEntities(context, id, { "entities" : definition.pulleyOneSelection });
        reportFeatureWarning(context, id, "The selected pitch circle defines a pulley with a fractional number of teeth.");
    }

    return {
            "basePlane" : circle.coordSystem->plane(),
            "pulleyTeeth" : round(targetPulleyTeeth)
        };
}

// function getSimpleBeltPlane(context is Context, definition is map) returns Plane
// {
//     var basePlane;
//     if (definition.pulleyOneSelectionType == SelectionType.GEOMETRY)
//     {
//         basePlane = evVertexCoordSystem(context, { "vertex" : definition.pulleyOneSelection })->plane();
//     }
//     else
//     {
//         basePlane = definition.pulleyOneInfo.coordSystem->plane();
//     }
//     return applyStartOffset(context, definition, basePlane);
// }

function computePulleySecondPoint(context is Context, id is Id, definition is map, beltPlane is Plane, basePoint is Vector) returns Vector
{
    if (!isPointOnPlane(basePoint, beltPlane))
    {
        reportFeatureWarning(context, id, "The selected pulley is not properly aligned with the belt.");
    }
    const secondPoint = project(beltPlane, basePoint);

    if (tolerantEquals(secondPoint, beltPlane.origin))
    {
        throw regenError("The selected pulley locations overlap each other.",
            ["pulleyOneSelection", "pulleyTwoSelection"], qUnion(definition.pulleyOneSelection, definition.pulleyTwoSelection));
    }
    return secondPoint;
}

/**
 * Computes the intersection of the second point with the belt plane.
 */
function computeGeometrySecondPoint(context is Context, id is Id, definition is map, beltPlane is Plane) returns Vector
{
    var secondPoint;
    if (isVertex(context, definition.pulleyTwoSelection) || isMateConnector(context, definition.pulleyTwoSelection))
    {
        const basePoint = evVertexCoordSystem(context, { "vertex" : definition.pulleyTwoSelection }).origin;
        secondPoint = project(beltPlane, basePoint);
    }
    else
    {
        const axis = evAxis(context, { "axis" : definition.pulleyTwoSelection });
        // Required to prevent issues with offsets causing the pulley to move
        if (!perpendicularVectors(axis.direction, beltPlane.normal))
        {
            throw regenError("The selected axis must be perpendicular to the belt plane.", ["pulleyOneSelection", "pulleyTwoSelection"], qUnion(definition.pulleyOneSelection, definition.pulleyTwoSelection));
        }
        const axisIntersection = intersection(beltPlane, axis);
        if (axisIntersection.dim == 1)
        {
            throw regenError("The selected axis is collinear with the belt plane.",
                ["pulleyOneSelection", "pulleyTwoSelection"], qUnion(definition.pulleyOneSelection, definition.pulleyTwoSelection));
        }
        else if (axisIntersection.dim == -1)
        {
            throw regenError("The selected axis does not intersect the belt plane.",
                ["pulleyOneSelection", "pulleyTwoSelection"], qUnion(definition.pulleyOneSelection, definition.pulleyTwoSelection));
        }
        secondPoint = axisIntersection.intersection;
    }

    if (tolerantEquals(secondPoint, beltPlane.origin))
    {
        throw regenError("The selected pulley locations cannot be coincident.",
            ["pulleyOneSelection", "pulleyTwoSelection"], qUnion(definition.pulleyOneSelection, definition.pulleyTwoSelection));
    }
    return secondPoint;
}

/**
 * Reports a warning if the measuredCenterToCenter does not closely match the idealCenterToCenter.
 */
function validateCenterToCenter(context is Context, id is Id, definition is map, measuredCenterToCenter is ValueWithUnits, idealCenterToCenter is ValueWithUnits) returns boolean
{
    // Strict matching
    if (definition.disableBeltValidation || withinDisplayPrecision(definition, measuredCenterToCenter, idealCenterToCenter))
    {
        // reportFeatureInfo(context, id, "The selected " ~ teeth ~ "T belt matches the distance between selections.");
        return true;
    }

    // Note: the following strings don't end with a period to make copy-pasting easier
    const centerToCenterString = makeValueString(definition.unitSystem, idealCenterToCenter);
    /**
     * The distance between your selections does not match the center to center distance of the selected belt.
     * Update the part studio so the distance between selections is 4.5 in
     */
    reportFeatureWarning(context, id, "To use this belt, update the part studio so the distance between your selections is " ~ centerToCenterString);
    return false;
}

function validateComplexBeltLength(context is Context, id is Id, definition is map, beltLoop is Query, beltDefinition is BeltDefinition)
{
    if (definition.disableBeltValidation)
    {
        return;
    }
    const actualLength = evLength(context, { "entities" : beltLoop });
    const idealLength = (getBeltPitch(beltDefinition.beltType) * beltDefinition.beltTeeth) + definition.beltFitAdjustment;

    if (withinDisplayPrecision(definition, actualLength, idealLength))
    {
        reportFeatureInfo(context, id, "Your selections match the selected " ~ beltDefinition.beltTeeth ~ "T belt. Note you may still want to design an adjustable tensioner to ensure proper fit.");
        return;
    }

    const difference = idealLength - actualLength;

    var verbs;
    const tooLong = difference > 0 * meter;
    if (tooLong)
    {
        verbs = {
                "longOrShort" : "long",
                "smallerOrLarger" : "smaller",
                "increaseOrDecrease" : "increasing"
            };
    }
    else
    {
        verbs = {
                "longOrShort" : "short",
                "smallerOrLarger" : "larger",
                "increaseOrDecrease" : "decreasing"
            };
    }


    /**
     * The closest belt is 100T and is 4.5 in too long./The selected belt is 4.5 in too long.
     * To use this belt, get the distance close by selecting a smaller belt and/or increasing the total perimeter, then use the Robot belt tuner FeatureScript to compute an exact solution.
     */
    var message = "The selected belt is ";
    message ~= makeValueString(definition.unitSystem, abs(difference)) ~ " too " ~ verbs.longOrShort;
    message ~= ". ";
    message ~= "To use this belt, get the distance close by selecting a " ~ verbs.smallerOrLarger ~ " belt and/or " ~ verbs.increaseOrDecrease ~ " the total perimeter, then use the Robot belt tuner FeatureScript to compute an exact solution.";
    reportFeatureWarning(context, id, message);
}

function getPulleyDiameters(pitch is ValueWithUnits, pulleyTeethArray is array)
{
    return mapArray(pulleyTeethArray, function(pulleyTeeth)
        {
            return getPulleyDiameter(pitch, pulleyTeeth);
        });
}

/**
 * Returns the target number of teeth a pulley with the specified pitchRadius would have.
 * Note the number of teeth may be a fraction.
 */
function computeTargetPulleyTeeth(pitch is ValueWithUnits, pulleyRadius is ValueWithUnits) returns number
{
    return pulleyRadius * PI / pitch;
}

/**
 * Returns the center to center distance of an arbitrary two pulley belt.
 */
function computeBeltCenterToCenter(pitch is ValueWithUnits, teeth is number, pulleyTeethArray is array) returns ValueWithUnits
precondition
{
    size(pulleyTeethArray) == 2;
}
{
    const pulleyDiameters = getPulleyDiameters(pitch, pulleyTeethArray);
    const largePulleyDiameter = max(pulleyDiameters);
    const smallPulleyDiameter = min(pulleyDiameters);

    try
    {
        const term = (teeth * pitch - (PI / 2 * (largePulleyDiameter + smallPulleyDiameter))) / 4;
        return term + sqrt(term ^ 2 - ((largePulleyDiameter - smallPulleyDiameter) ^ 2) / 8);
    }
    catch
    {
        // The selected belt is way too small, e.g., it's smaller than the circumference of one of the pulleys
        throw regenError("The selected belt is too small.", ["pulleyOneTeeth", "pulleyTwoTeeth", "beltTeeth"]);
    }
}

// Unused: sizing a belt to a center-to-center distance, kept for when it's wanted again
// /**
//  * Computes the number of teeth required to achieve the specified `targetCenterToCenter` distance.
//  * Note the number of teeth may be a fraction.
//  */
// function computeTargetBeltTeeth(pitch is ValueWithUnits, pulleyTeethArray is array, targetCenterToCenter is ValueWithUnits) returns number
// precondition
// {
//     size(pulleyTeethArray) == 2;
// }
// {
//     const pulleyDiameters = getPulleyDiameters(pitch, pulleyTeethArray);
//     const largePulleyDiameter = max(pulleyDiameters);
//     const smallPulleyDiameter = min(pulleyDiameters);
//
//     const term = -largePulleyDiameter * smallPulleyDiameter +
//         4 * targetCenterToCenter ^ 2 +
//         largePulleyDiameter * targetCenterToCenter * PI +
//         smallPulleyDiameter * targetCenterToCenter * PI;
//     const numerator = (largePulleyDiameter ^ 2 + smallPulleyDiameter ^ 2 + 2 * term);
//     return numerator / (4 * targetCenterToCenter * pitch);
// }

/**
 * Creates the belt.
 * @param id : @autocomplete `id + "belt"`
 */
function createBelt(context is Context, id is Id, definition is map, beltDefinition is BeltDefinition, beltLoop is Query, counterClockwise is boolean, arcQueries is array)
{
    const pulleyDefinitions = beltDefinition.pulleyDefinitions;
    const arcTrackingQueries = mapArray(arcQueries, function(arcQuery)
        {
            return startTracking(context, arcQuery);
        });

    const beltAttribute = {
                "beltMode" : definition.beltMode,
                "modelBeltTeeth" : definition.modelBeltTeeth,
                "isDoubleSidedBelt" : isDoubleSidedBelt(definition),
                "beltType" : beltDefinition.beltType,
                "beltTeeth" : beltDefinition.beltTeeth,
                "beltWidth" : beltDefinition.beltWidth
            } as BeltAttribute;

    const result = extrudeBelt(context, id + "belt", beltAttribute, beltDefinition.beltPlane, beltLoop, counterClockwise);

    setAttribute(context, {
                "entities" : result.startFace,
                "name" : BELT_START_FACE_ATTRIBUTE,
                "attribute" : {}
            });

    setAttribute(context, {
                "entities" : result.belt,
                "name" : BELT_ATTRIBUTE,
                "attribute" : beltAttribute
            });

    for (var i, trackedArcQuery in arcTrackingQueries)
    {
        const pulleyDefinition = beltDefinition.pulleyDefinitions[i];

        const arcFaces = trackedArcQuery->qEntityFilter(EntityType.FACE);
        var closestFace = qClosestTo(arcFaces, planeToWorld(beltDefinition.beltPlane, pulleyDefinition.location));
        var furthestFace = arcFaces->qSubtraction(closestFace);
        // Faces can not exist on double sided belts with too many teeth
        if (!isQueryEmpty(context, closestFace))
        {
            setAttribute(context, {
                        "entities" : closestFace,
                        "name" : BELT_PULLEY_FACE_ATTRIBUTE,
                        "attribute" : {
                                "beltSize" : beltDefinition.beltSize,
                                "pulleyType" : pulleyDefinition.pulleyType,
                                "pulleyTeeth" : pulleyDefinition.pulleyTeeth,
                                "idlerRadius" : pulleyDefinition.idlerRadius,
                                "beltSide" : pulleyDefinition.beltSide,
                            } as BeltFaceAttribute
                    });
        }

        if (!isQueryEmpty(context, furthestFace))
        {
            setAttribute(context, {
                        "entities" : furthestFace,
                        "name" : BELT_PULLEY_FACE_ATTRIBUTE,
                        "attribute" : {
                                "beltSize" : beltDefinition.beltSize,
                                "pulleyType" : pulleyDefinition.pulleyType,
                                "pulleyTeeth" : pulleyDefinition.pulleyTeeth,
                                "idlerRadius" : pulleyDefinition.idlerRadius,
                                "beltSide" : pulleyDefinition.beltSide,
                            } as BeltFaceAttribute
                    });

            setAttribute(context, {
                        "entities" : furthestFace->qNthElement(0),
                        "name" : BELT_PRIMARY_PULLEY_FACE_ATTRIBUTE,
                        "attribute" : {}
                    });
        }
    }

    setBeltProperties(context, result.belt, beltAttribute);
}


/**
 * Sketches the main interior loop of a belt. Returns a query for the loop and an ordered array of queries for each arc.
 */
function sketchBelt(context is Context, id is Id, beltDefinition is BeltDefinition) returns map
{
    const beltPlane = beltDefinition.beltPlane;

    var circles = getBoundaryCircles(beltDefinition.pulleyDefinitions, beltDefinition);
    const locations = extractFromArrayOfMaps(circles, "location");
    const counterClockwise = isCounterClockwise(locations);
    const connectingPointsArray = getPulleyConnectingPointsArray(circles, counterClockwise);
    // robustness requirements are still high, even for a belt
    const arcQueries = sketchConnectingArcs(context, id + "arcs", circles, beltPlane, connectingPointsArray, counterClockwise);

    sketchConnectingLines(context, id + "lines", circles, beltPlane, connectingPointsArray);

    return {
            "beltLoop" : qCreatedBy(id, EntityType.EDGE)->qSketchFilter(SketchObject.YES),
            "arcQueries" : arcQueries,
            "counterClockwise" : counterClockwise
        };
}

/**
 * Extracts an array of circle definitions which can be consumed by boundary sketching utilities.
 */
function getBoundaryCircles(pulleyDefinitions is array, beltDefinition is BeltDefinition) returns array
{
    return mapArray(pulleyDefinitions, function(pulleyDefinition)
        {
            return getBoundaryCircle(pulleyDefinition, beltDefinition);
        });
}

function getBoundaryCircle(pulleyDefinition is PulleyDefinition, beltDefinition is BeltDefinition) returns BoundaryCircle
{
    var radius;
    if (isPulley(pulleyDefinition.pulleyType))
    {
        radius = getPulleyRadius(getBeltPitch(beltDefinition.beltType), pulleyDefinition.pulleyTeeth);
    }
    else
    {
        radius = pulleyDefinition.idlerRadius + getBeltOusideThickness(beltDefinition.beltType);
    }
    return {
                "location" : pulleyDefinition.location,
                "identity" : pulleyDefinition.identity,
                "radius" : radius,
                "flipped" : isOutside(pulleyDefinition.pulleyType)
            } as BoundaryCircle;
}

function getPulleyConnectingPointsArray(circles is array, counterClockwise is boolean) returns array
{
    return mapArrayIndices(circles, function(i)
        {
            const prev = getPrevious(circles, i);
            const curr = circles[i];
            return circleToCircle(prev, curr, counterClockwise);
        });
}

function setBeltProperties(context is Context, beltQuery is Query, beltAttribute is BeltAttribute)
{
    setProperty(context, {
                "entities" : beltQuery,
                "propertyType" : PropertyType.MATERIAL,
                "value" : material("Viton Rubber", 1827 * kilogram / meter ^ 3) // default belt material and density
            });
    setProperty(context, {
                "entities" : beltQuery,
                "propertyType" : PropertyType.APPEARANCE,
                "value" : BLACK
            });

    const beltName = getBeltName(beltAttribute.beltTeeth, beltAttribute.isDoubleSidedBelt, beltAttribute.beltType);
    setProperty(context, {
                "entities" : beltQuery,
                "propertyType" : PropertyType.NAME,
                "value" : beltName
            });
}

function getBeltName(beltTeeth is number, isDoubleSidedBelt is boolean, beltType is BeltType) returns string
{
    // 24T Double Sided GT2 Belt

    return beltTeeth ~ "T " ~ (isDoubleSidedBelt ? "Double Sided " : "") ~ getBeltTypeName(beltType) ~ " Belt";
}


function createMateConnectors(context is Context, id is Id, beltDefinition is BeltDefinition, belt is Query)
{
    var coordSystem = beltDefinition.beltPlane->coordSystem();
    for (var i, pulleyDefinition in beltDefinition.pulleyDefinitions)
    {
        const mateConnectorId = id + unstableIdComponent(i);
        if (pulleyDefinition.identity != undefined)
        {
            setExternalDisambiguation(context, mateConnectorId, pulleyDefinition.identity);
        }

        const location = planeToWorld(beltDefinition.beltPlane, pulleyDefinition.location);
        coordSystem.origin = location;
        opMateConnector(context, mateConnectorId, {
                    "coordSystem" : coordSystem,
                    "owner" : belt
                });
        setAttribute(context, {
                    "entities" : qCreatedBy(mateConnectorId, EntityType.BODY),
                    "name" : BELT_PULLEY_FACE_ATTRIBUTE,
                    "attribute" : {
                            "pulleyType" : pulleyDefinition.pulleyType,
                            "beltSize" : beltDefinition.beltSize,
                            "pulleyTeeth" : pulleyDefinition.pulleyTeeth,
                            "idlerRadius" : pulleyDefinition.idlerRadius,
                            "outerFace" : false
                        } as BeltFaceAttribute
                });
    }
}

const BELT_SIDE_FLIP_MANIPULATOR = "beltSideFlipManipulator";

function addBeltFlipManipulators(context is Context, id is Id, beltDefinition is BeltDefinition, arcQueries is array, counterClockwise is boolean)
{
    var manipulators = {};
    for (var i, pulleyDefinition in beltDefinition.pulleyDefinitions)
    {
        const pulleyType = pulleyDefinition.pulleyType;
        const beltSide = pulleyDefinition.beltSide;
        const arcQuery = arcQueries[i];

        const tangentLine = evEdgeTangentLine(context, {
                    "edge" : arcQuery,
                    "parameter" : 0.5
                });
        var base = tangentLine.origin;
        // Points outwards from the belt
        var direction = cross(beltDefinition.beltPlane.normal, tangentLine.direction) * (counterClockwise ? 1 : -1);

        // If idler, adjust base by belt thickness
        if (pulleyDefinition.pulleyType == PulleyType.IDLER)
        {
            // Always draw on inside of belt face
            base += direction * getBeltOusideThickness(beltDefinition.beltSize) * (counterClockwise ? 1 : -1);
        }
        manipulators[BELT_SIDE_FLIP_MANIPULATOR ~ "." ~ i] = flipManipulator({
                    "base" : base,
                    "direction" : direction,
                    "flipped" : false
                });
    }
    addManipulators(context, id, manipulators);
}

export function robotBeltManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    for (var key, manipulator in newManipulators)
    {
        const parsed = match(key, BELT_SIDE_FLIP_MANIPULATOR ~ ".(\\d+)");
        if (parsed.hasMatch && manipulator.flipped)
        {
            const index = stringToNumber(parsed.captures[1]);
            const currentSide = definition.pulleys[index].beltSide;
            // Swap the pulley type each time the manipulator is clicked
            definition.pulleys[index].beltSide = currentSide == BeltSide.INSIDE ? BeltSide.OUTSIDE : BeltSide.INSIDE;
        }
    }
    definition = startOffsetManipulatorChange(definition, newManipulators);
    return definition;
}

export function robotBeltEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean, clickedButton is string) returns map
{
    if (oldDefinition == {})
    {
        return definition;
    }

    // if (isSimpleBelt(definition))
    // {
    //     definition = fillBeltParameters(context, oldDefinition, definition, Pulley.ONE);
    //     definition = fillBeltParameters(context, oldDefinition, definition, Pulley.TWO);
    // }

    if (clickedButton == "selectClosestBelt" || beltChanged(oldDefinition, definition))
    {
        definition = setClosestBelt(context, id, definition);
    }

    if (beltChanged(oldDefinition, definition))
    {
        definition = syncBeltParameters(definition);
    }

    return definition;
}

function beltChanged(oldDefinition is map, definition is map) returns boolean
{
    const oldPath = getBeltTableAndPath(oldDefinition).path;
    const newPath = getBeltTableAndPath(definition).path;
    return oldPath != newPath;
}

function syncBeltParameters(definition is map) returns map
{
    const tableAndPath = getBeltTableAndPath(definition);
    if (hasCustomTeeth(tableAndPath.path))
    {
        return definition;
    }
    const beltValue = getBeltValue(tableAndPath);
    definition.beltTeeth = beltValue.beltTeeth;
    return definition;
}



// /**
//  * Attempts to set the belt size whenever a pulley mate connector is selected.
//  */
// function fillBeltParameters(context is Context, oldDefinition is map, definition is map, pulley is Pulley) returns map
// {
//     const locationKey = pulleyString(pulley) ~ "Location";
//     if ((oldDefinition == {} || !areQueriesEquivalent(context, oldDefinition[locationKey], definition[locationKey])) && isMateConnector(context, definition[locationKey]))
//     {
//         const attribute = getAttribute(context, {
//                     // Mate connector query parameters need qOwnerBody to get the explicit mate connector
//                     "entity" : definition[locationKey]->qOwnerBody(),
//                     "name" : PULLEY_ATTRIBUTE
//                 });
//         if (attribute == undefined)
//         {
//             return definition;
//         }
//         definition = mergeMaps(definition, beltSizeToBeltParameters(attribute.beltSize));
//         definition[pulleyString(pulley) ~ "Teeth"] = attribute.pulleyTeeth;
//     }
//     return definition;
// }

function setClosestBelt(context is Context, id is Id, definition is map) returns map
{
    if (isSimpleBelt(definition))
    {
        if (isQueryEmpty(context, definition.pulleyOneSelection) || isQueryEmpty(context, definition.pulleyTwoSelection))
        {
            return definition;
        }

        try silent
        {
            // const beltPlane = getSimpleBeltPlane(context, definition);
            // const secondPoint = computeSecondPoint(context, id, definition, beltPlane);
            // const secondPointVector = secondPoint - beltPlane.origin;

            // const targetCenterToCenter = norm(secondPointVector);

            // const tableAndPath = getBeltTableAndPath(definition);
            // const pitch = getBeltPitch(getBeltValue(tableAndPath).beltType);

            // const pulleyTeeth = getSimpleBeltPulleyTeeth(definition);
            // const targetTeeth = computeTargetBeltTeeth(pitch, pulleyTeeth, targetCenterToCenter);

            // const beltTeeth = getClosestBeltTeeth(tableAndPath, targetTeeth);

            // if (!hasCustomTeeth(tableAndPath.path))
            // {
            //     definition.beltPath.teeth = toString(beltTeeth) ~ "T";
            // }
            // definition.beltTeeth = beltTeeth;
        }
    }
    else
    {
        // if (autoBelt(definition))
        // {
        //     const circles = getBoundaryCircles(pulleyDefinitions, beltSize);
        //     const locations = extractFromArrayOfMaps(circles, "location");
        //     const beltLength = computeBeltLength(circles, isCounterClockwise(locations));
        //     const targetTeeth = beltLength / getBeltPitch(beltSize);
        //     beltTeeth = getClosestBeltTeeth(definition, targetTeeth);
        // }
    }

    return definition;
}

// Unused: sizing a belt to a center-to-center distance, kept for when it's wanted again
// /**
//  * Returns the number of teeth the belt should use based on the options currently available in the lookup table.
//  */
// function getClosestBeltTeeth(tableAndPath is map, targetTeeth is number) returns number
// {
//     if (hasCustomTeeth(tableAndPath.path))
//     {
//         return round(targetTeeth);
//     }
//     const beltArray = getCurrentBeltOptionsArray(tableAndPath);
//     var bestBelt = beltArray[0];
//     for (var belt in beltArray)
//     {
//         if (belt <= targetTeeth)
//         {
//             bestBelt = belt;
//         }
//         else // belt is larger; maybe terminate search
//         {
//             // smaller belt, larger belt
//             if (abs(bestBelt - targetTeeth) >= abs(belt - targetTeeth))
//             {
//                 bestBelt = belt;
//             }
//             break;
//         }
//     }
//     return bestBelt;
// }
