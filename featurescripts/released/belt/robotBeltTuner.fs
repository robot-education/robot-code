FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "2073caea5ae472033c5090d9");
import(path : "452d43a015d17145ad7775e4", version : "e68e283095fa8403f8fa0213");
import(path : "ea127c07807644fb48d3a1ae", version : "72fbd92d548c811d10a5d2f3");
export import(path : "948c83c1b1ac83de4ccf921b", version : "4aff58a1ab26d9f7aa7abfbb");

import(path : "e269bd2b7266145c47eaf374", version : "6c8b8d8077dcf88085165ded");
import(path : "00b10ef1fb1a7418097fc0af", version : "3ba879cf97235b1a292f0dbc");
import(path : "5bee4cc7b6b0575cdb535750", version : "9faaf2e96493e60886247f9d");

import(path : "6c65805103086c85362ee4b7", version : "06268198ef2566cb246b9f56");

export enum AdjustmentType
{
    annotation { "Name" : "Move" }
    MOVE,
    annotation { "Name" : "Resize" }
    RESIZE
}

annotation {
        "Feature Type Name" : "Robot belt tuner",
        "Feature Type Description" : "Compute solutions for Complex belts created with the Robot belt FeatureScript." ~ CREDIT,
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotBeltTuner = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        unitSystemPredicate(definition);

        annotation { "Name" : "Adjustment type", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.adjustmentType is AdjustmentType;

        annotation { "Name" : "Robot belt", "Filter" : EntityType.BODY, "MaxNumberOfPicks" : 1 }
        definition.belt is Query;

        annotation { "Name" : "Curved belt face to adjust", "Filter" : EntityType.FACE && GeometryType.CYLINDER, "MaxNumberOfPicks" : 1 }
        definition.beltFace is Query;

        if (definition.adjustmentType == AdjustmentType.MOVE)
        {
            annotation { "Name" : "Adjustment axis", "Filter" : GeometryType.LINE || QueryFilterCompound.ALLOWS_VERTEX, "MaxNumberOfPicks" : 1 }
            definition.adjustmentDirection is Query;
        }
    }
    {
        verifyNonemptyQuery(context, definition, "belt", "Select a belt to tune.");
        const beltAttribute = getAttribute(context, {
                    "entity" : definition.belt,
                    "name" : BELT_ATTRIBUTE
                });
        if (beltAttribute == undefined)
        {
            throw regenError("The selected belt is not a valid robot belt.", ["belt"], definition.belt);
        }
        if (beltAttribute.beltMode == BeltMode.SIMPLE)
        {
            throw regenError("Simple belts cannot be tuned.", ["belt"], definition.belt);
        }

        verifyNonemptyQuery(context, definition, "beltFace", "Select a curved belt face to adjust.");
        const adjustAttribute = getAttribute(context, {
                    "entity" : definition.beltFace,
                    "name" : BELT_PULLEY_FACE_ATTRIBUTE
                });

        if (adjustAttribute == undefined)
        {
            throw regenError("The selected face to adjust does not correspond to a valid idler or pulley face.", ["beltFace"], definition.beltFace);
        }
        else if (isQueryEmpty(context, definition.beltFace->qIntersection(qOwnedByBody(definition.belt, EntityType.FACE))))
        {
            throw regenError("The selected belt face does not belong to the selected belt.", ["belt", "beltFace"], qUnion(definition.belt, definition.beltFace));
        }
        else if (definition.adjustmentType == AdjustmentType.RESIZE && !isIdler(adjustAttribute.pulleyType))
        {
            throw regenError("Pulleys cannot be resized automatically. Select an idler instead.", ["beltFace"], definition.beltFace);
        }

        if (definition.adjustmentType == AdjustmentType.MOVE)
        {
            verifyNonemptyQuery(context, definition, "adjustmentDirection", "Select an axis to adjust the pulley along.");
        }

        const beltStartFace = qOwnedByBody(definition.belt, EntityType.FACE)->qHasAttribute(BELT_START_FACE_ATTRIBUTE);
        var beltPlane = evFaceTangentPlane(context, {
                "face" : beltStartFace,
                "parameter" : vector(0.5, 0.5)
            });
        beltPlane.origin -= beltPlane.normal * beltAttribute.beltWidth / 2;

        const adjustFaceCylinder = evSurfaceDefinition(context, { "face" : definition.beltFace });
        const adjustLocation = worldToPlane(beltPlane, adjustFaceCylinder.coordSystem.origin);

        const pulleyFaces = qPrimaryPulleyFaces(definition.belt);
        var circles = [];
        var adjustIndex = undefined;
        for (var i, face in evaluateQuery(context, pulleyFaces))
        {
            const attribute = getAttribute(context, {
                        "entity" : face,
                        "name" : BELT_PULLEY_FACE_ATTRIBUTE
                    });
            const cylinder = evSurfaceDefinition(context, { "face" : face });

            const location = worldToPlane(beltPlane, cylinder.coordSystem.origin);
            debug(context, location);
            if (adjustIndex == undefined && tolerantEquals(location, adjustLocation))
            {
                adjustIndex = i;
            }

            var radius;
            if (isPulley(attribute.pulleyType))
            {
                radius = getPulleyRadius(getBeltPitch(attribute.beltSize), attribute.pulleyTeeth);
            }
            else
            {
                radius = attribute.idlerRadius + getBeltOusideThickness(attribute.beltSize);
            }
            circles = append(circles, {
                            "location" : location,
                            "radius" : radius,
                            "flipped" : isOutside(attribute.pulleyType)
                        } as BoundaryCircle);
        }

        if (adjustIndex == undefined)
        {
            throw regenError("Unexpectedly failed to find pulley to adjust. Check input.", ["belt", "beltFace"], definition.beltFace);
        }

        const locations = extractFromArrayOfMaps(circles, "location");
        const counterClockwise = isCounterClockwise(locations);

        const targetLength = getBeltPitch(beltAttribute.beltSize) * beltAttribute.beltTeeth;
        const beltLength = computeBeltLength(circles, counterClockwise);

        if (withinDisplayPrecision(definition, beltLength, targetLength))
        {
            addDebugEntities(context, definition.belt, DebugColor.GREEN);
            reportFeatureInfo(context, id, "The selected belt fits the model correctly, so this feature can be safely deleted.");
            return;
        }

        var circleFunction;
        if (definition.adjustmentType == AdjustmentType.MOVE)
        {
            const moveDirection = extractDirection(context, definition.adjustmentDirection);
            circleFunction = getPulleyMoveFunction(moveDirection, beltPlane, circles, adjustIndex);
        }
        else
        {
            circleFunction = getPulleyResizeFunction(circles, adjustIndex);
        }

        const valueFunction = function(value is ValueWithUnits)
            {
                try
                {
                    const tempCircles = circleFunction(value);
                    const beltLength = computeBeltLength(tempCircles, counterClockwise);
                    return abs(targetLength - beltLength);
                }
                catch
                {
                    return undefined;
                }
            };

        const result = gradientDescent(valueFunction, 0 * meter, 5 * millimeter, TOLERANCE.zeroLength * meter * 1000);
        if (result.status != GradientStatus.SUCCESS)
        {
            try
            {
                circles = circleFunction(result.value);
                const belt = createBelt(context, id + "belt", beltPlane, beltAttribute.beltSize, circles, counterClockwise);
                setErrorEntities(context, id, { "entities" : belt });
            }
            if (result.status == GradientStatus.OUT_OF_BOUNDS || result.status == GradientStatus.EXCEEDED_ITERATIONS)
            {
                var message = "Failed to find a configuration with a valid belt length.";
                if (result.value != undefined)
                {
                    message ~= " The closest value results in a belt which is " ~ makeValueString(definition.unitSystem, valueFunction(result.value)) ~ " away from the correct length.";
                }
                throw regenError(message);
            }
            throw regenError("Unexpectedly failed to find a valid value. Check input. If the problem persists, contact Alex.");
        }

        try
        {
            const updatedCircles = circleFunction(result.value);
            const belt = createBelt(context, id + "belt", beltAttribute, beltPlane, updatedCircles, counterClockwise);
            addDebugEntities(context, belt, DebugColor.BLUE);

            var displayValue;
            if (definition.adjustmentType == AdjustmentType.MOVE)
            {
                displayValue = abs(result.value);
            }
            else
            {
                // Subtract beltThickness once again
                displayValue = (circles[adjustIndex].radius + result.value) - getBeltOusideThickness(beltAttribute.beltSize);
            }
            const displayString = makeValueString(definition.unitSystem, displayValue, true);

            var message;
            if (definition.adjustmentType == AdjustmentType.MOVE)
            {
                const startPoint = planeToWorld(beltPlane, circles[adjustIndex].location);
                const endPoint = planeToWorld(beltPlane, updatedCircles[adjustIndex].location);
                const length = norm(startPoint - endPoint);
                addDebugArrow(context, startPoint, endPoint, length * 0.2, DebugColor.GREEN);

                message = "Successfully computed a valid value for the belt. To use it, move the selected pulley " ~ displayString ~ " as indicated.";
            }
            else
            {
                const verb = (result.value > 0 * meter) ? "increasing" : "decreasing";
                message = "Successfully computed a valid value for the belt. To use it, change the diameter of the idler to " ~ displayString;
            }

            reportFeatureInfo(context, id, message);
        }
        catch
        {
            // Typically means a valid solution was found, but the solution has a loop in the belt
            processSubfeatureStatus(context, id, {
                        "subfeatureId" : id + "belt",
                        "propagateErrorDisplay" : true
                    });
            throw regenError("Failed to find a configuration with a valid belt length.");
        }

        cleanup(context, id + "delete", qCreatedBy(id, EntityType.BODY));
    });



function getPulleyResizeFunction(circles is array, adjustIndex is number) returns function
{
    return function(offset is ValueWithUnits)
        {
            var tempCircles = circles;
            tempCircles[adjustIndex].radius += offset;
            return tempCircles;
        };
}

function getPulleyMoveFunction(direction is Vector, beltPlane is Plane, circles is array, adjustIndex is number) returns function
{
    const direction2d = worldToPlane(beltPlane, beltPlane.origin + direction * meter)->normalize();
    return function(offset is ValueWithUnits)
        {
            var tempCircles = circles;
            tempCircles[adjustIndex].location += direction2d * offset;
            return tempCircles;
        };
}

function createBelt(context is Context, id is Id, beltAttribute is BeltAttribute, beltPlane is Plane, circles is array, counterClockwise is boolean) returns Query
{
    // Sketch the belt with no regard for robustness/query stabilization
    const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : beltPlane });
    const connectingPointsArray = getConnectingPointsArray(circles, counterClockwise);
    for (var i, points in connectingPointsArray)
    {
        skLineSegment(sketch, "line" ~ i, {
                    "start" : points[0],
                    "end" : points[1]
                });
        const nextPoints = getNext(connectingPointsArray, i);
        addArc(sketch, "arc" ~ i, concatenateArrays([points, nextPoints]), circles[i], counterClockwise);
    }
    skSolve(sketch);

    const beltLoop = qCreatedBy(id + "sketch", EntityType.EDGE)->qBodyType(BodyType.WIRE);
    beltAttribute.modelBeltTeeth = false; // Don't model belt teeth to avoid hurting performance
    const result = extrudeBelt(context, id + "belt", beltAttribute, beltPlane, beltLoop, counterClockwise);
    cleanup(context, id + "delete", qCreatedBy(id + "sketch", EntityType.BODY));
    return result.belt;
}

export enum GradientStatus
{
    SUCCESS,
    INVALID_START,
    EXCEEDED_ITERATIONS,
    OUT_OF_BOUNDS,
    ZERO_GRADIENT,
}

const MAX_ITERATIONS = 250;

/**
 * A relatively crude implementation of gradient descent.
 * @param f {function} : Maps an argument `ValueWithUnits` to the closeness of the belt to the target belt.
 * @param start : The starting value of the algorithm.
 * @param stepSize : The starting step size of the algorithm. This gets reduced automatically as the algorithm converges to zero.
 *
 * @returns {{
 *      @field status {GradientStatus} :
 *      @field value {ValueWithUnits} : The closest paramter for which `f` is defined, or `undefined` if none exists.
 * }}
 */
function gradientDescent(f is function, start is ValueWithUnits, stepSize is ValueWithUnits, precision is ValueWithUnits) returns map
{
    var curr = start;
    var currResult = f(curr);
    var iterations = 0;

    if (currResult == undefined)
    {
        return { "status" : GradientStatus.INVALID_START };
    }

    var prevDirection = undefined;
    while (abs(currResult) >= precision)
    {
        iterations += 1;
        if (iterations >= MAX_ITERATIONS)
        {
            return { "status" : GradientStatus.EXCEEDED_ITERATIONS, "value" : curr };
        }

        const gradient = gradient(f, curr);
        if (gradient == undefined)
        {
            return { "status" : GradientStatus.OUT_OF_BOUNDS, "value" : curr };
        }
        else if (tolerantEqualsZero(gradient))
        {
            return { "status" : GradientStatus.ZERO_GRADIENT, "value" : curr };
        }

        const currDirection = gradient > 0;
        // If direction changes, cut stepSize
        if (prevDirection != undefined && currDirection != prevDirection)
        {
            stepSize /= 2;
        }
        else
        {
            const prevCurr = curr;
            curr += stepSize * gradient;
            currResult = f(curr);
            // Ensure curr is never undefined
            if (currResult == undefined)
            {
                return { "status" : GradientStatus.OUT_OF_BOUNDS, "value" : prevCurr };
            }
        }
        prevDirection = currDirection;
    }
    return { "status" : GradientStatus.SUCCESS, "value" : curr };
}

/**
 * An arbitrarily small h used to compute the derivative.
 */
const H = 0.01 * millimeter;

/**
 * Computes the gradient of f at curr using an arbitrarily small value for `h`.
 * Note `f(curr)` is assumed to be defined.
 */
function gradient(f is function, curr is ValueWithUnits)
{
    const currValue = f(curr);
    const offsetValue = f(curr + H);
    if (offsetValue == undefined)
    {
        return undefined;
    }
    // Rise over run
    return (currValue - offsetValue) / H;
}
