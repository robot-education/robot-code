FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "2073caea5ae472033c5090d9");

import(path : "5bee4cc7b6b0575cdb535750", version : "9faaf2e96493e60886247f9d");
import(path : "ea127c07807644fb48d3a1ae", version : "72fbd92d548c811d10a5d2f3");
import(path : "0794d10863d10d98a88c2ab4", version : "90bbee184f6552271649afea");
import(path : "70d403fe3ae377ef6f571c77", version : "b2c24635006801842ffbe0f7");

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
        "<br>See also the Robot pulley and Robot tensioner FeatureScripts, which work with this feature directly." ~ CREDIT
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
    if (isOpenBelt(definition))
    {
        doOpenBelt(context, id, definition);
        return;
    }
    var beltDefinition;
    if (isStandaloneBelt(definition))
    {
        beltDefinition = getStandaloneBeltDefinition(context, id, definition);
    }
    else if (isSimpleBelt(definition))
    {
        beltDefinition = getSimpleBeltDefinition(context, id, definition);
        addStartOffsetManipulator(context, id, definition, beltDefinition.beltPlane);
    }
    else
    {
        beltDefinition = getComplexBeltDefinition(context, id, definition);
        addStartOffsetManipulator(context, id, definition, beltDefinition.beltPlane);
    }

    const circles = getBoundaryCircles(beltDefinition);
    const counterClockwise = loopCounterClockwise(circles);
    const sketch = sketchLoop(context, id + "sketch", beltDefinition.beltPlane, circles, counterClockwise);

    if (isComplexBelt(definition))
    {
        addBeltFlipManipulators(context, id, beltDefinition, sketch.arcs, counterClockwise);
        validateComplexBeltLength(context, id, definition, beltDefinition, loopLength(circles, counterClockwise));
    }

    const belt = createBelt(context, id + "belt", definition, beltDefinition, sketch, counterClockwise);
    setAttribute(context, {
                "entities" : belt,
                "name" : LOOP_ATTRIBUTE,
                "attribute" : beltLoopAttribute(definition, beltDefinition, circles, counterClockwise)
            });

    if (definition.addMateConnectors)
    {
        createMateConnectors(context, id + "mateConnectors", beltDefinition, belt);
    }
    cleanup(context, id + "delete", qCreatedBy(id + "sketch", EntityType.BODY));
}

/**
 * A belt, and the pulleys and idlers it goes around.
 * @type {{
 *      @field beltPlane {Plane} : The plane of the belt's middle. Its `x` axis is arbitrary.
 *      @field pulleyDefinitions {array} : Its `PulleyDefinition`s, in order around it.
 * }}
 */
type BeltDefinition typecheck canBeBeltDefinition;

predicate canBeBeltDefinition(value)
{
    value is map;
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
 * A pulley or idler of a belt.
 *
 * @type {{
 *      @field identity {Query} : @optional What it was selected with, to disambiguate what's made around it.
 *      @field location {Vector} : Its center, in the belt's plane.
 * }}
 */
type PulleyDefinition typecheck canBePulleyDefinition;

predicate canBePulleyDefinition(value)
{
    value is map;
    value.identity is Query || value.identity is undefined;
    is2dPoint(value.location);
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

/**
 * An open belt: from its start, around its pulleys and idlers, to its end. Each is on the inside of the belt's turn
 * around it, so a pulley's on its teeth's side, and an idler's on its back's: an open belt's single sided, so its
 * pulleys must all be on one side, and its idlers on the other.
 */
function doOpenBelt(context is Context, id is Id, definition is map)
{
    const beltDefinition = getComplexBeltDefinition(context, id, definition);
    const plane = beltDefinition.beltPlane;
    addStartOffsetManipulator(context, id, definition, plane);
    verifyNonemptyQuery(context, definition, "endPoint", "Select where the belt ends.");
    const start = worldToPlane(plane, evVertexCoordSystem(context, { "vertex" : definition.startPoint }).origin);
    const end = worldToPlane(plane, evVertexCoordSystem(context, { "vertex" : definition.endPoint }).origin);

    var circles = getBoundaryCircles(beltDefinition);
    var teethLeft = undefined;
    for (var i, circle in circles)
    {
        const before = i == 0 ? start : circles[i - 1].location;
        const after = i == size(circles) - 1 ? end : circles[i + 1].location;
        const into = circle.location - before;
        const out = after - circle.location;
        // The belt turns left around it: it's on the belt's left
        const left = into[0] * out[1] - into[1] * out[0] >= 0 * meter ^ 2;
        circles[i].flipped = !left;
        const teethOnLeft = isPulley(beltDefinition.pulleyDefinitions[i].pulleyType) ? left : !left;
        if (teethLeft == undefined)
        {
            teethLeft = teethOnLeft;
        }
        else if (teethLeft != teethOnLeft)
        {
            throw regenError("An open belt's teeth face its pulleys, and its back its idlers, so its pulleys must all be on one side of it, and its idlers on the other.",
                ["pulleys"]);
        }
    }

    const path = openPath(start, circles, end);
    const beltInfo = getBeltModelInfo(beltDefinition.beltType);
    const teethSide = getBeltModelInsideThickness(beltInfo, false);
    const profile = sketchOpenPathProfile(context, id + "sketch", plane, path, teethLeft ? teethSide : beltInfo.outsideThickness,
        teethLeft ? beltInfo.outsideThickness : teethSide);
    const arcTracking = mapArray(profile.arcs, function(arc)
        {
            return startTracking(context, arc);
        });
    opExtrude(context, id + "belt", {
                "entities" : profile.faces,
                "direction" : plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : beltDefinition.beltWidth / 2,
                "startBound" : BoundingType.BLIND,
                "startDepth" : beltDefinition.beltWidth / 2
            });
    const belt = qCreatedBy(id + "belt", EntityType.BODY);
    for (var i, tracking in arcTracking)
    {
        const faces = tracking->qEntityFilter(EntityType.FACE)->qGeometry(GeometryType.CYLINDER);
        if (!isQueryEmpty(context, faces))
        {
            setAttribute(context, {
                        "entities" : faces,
                        "name" : BELT_PULLEY_FACE_ATTRIBUTE,
                        "attribute" : beltFaceAttribute(beltDefinition, beltDefinition.pulleyDefinitions[i])
                    });
        }
    }

    const lengthString = makeValueString(definition.unitSystem, path.length);
    setBeltProperties(context, belt, getBeltTypeName(beltDefinition.beltType) ~ " Belt (" ~ lengthString ~ ")");
    // The belt is 34.567 in long: 175 teeth.
    reportFeatureInfo(context, id, "The belt is " ~ lengthString ~ " long: " ~ floor(path.length / getBeltPitch(beltDefinition.beltType)) ~ " teeth.");

    if (definition.addMateConnectors)
    {
        createMateConnectors(context, id + "mateConnectors", beltDefinition, belt);
    }
    cleanup(context, id + "delete", qCreatedBy(id + "sketch", EntityType.BODY));
}

function getStandaloneBeltDefinition(context is Context, id is Id, definition is map) returns BeltDefinition
{
    const pulleyTeeth = [definition.pulleyOneTeeth, definition.pulleyTwoTeeth];
    const beltValue = getBeltValue(getBeltTableAndPath(definition));
    const centerToCenter = computeBeltCenterToCenter(beltValue.beltType, definition.beltTeeth, pulleyTeeth, 0 * meter);

    // The selected 100T belt has a 3.45 in center to center distance.
    reportFeatureInfo(context, id, "The selected belt has a " ~ makeValueString(definition.unitSystem, centerToCenter) ~ " center to center distance.");

    return {
                "beltPlane" : YZ_PLANE,
                "beltType" : beltValue.beltType,
                "beltWidth" : beltValue.beltWidth,
                "beltTeeth" : definition.beltTeeth,
                "pulleyDefinitions" : [
                        twoPulleyDefinition(zeroVector(2) * meter, pulleyTeeth[0]),
                        twoPulleyDefinition(vector(centerToCenter, 0 * meter), pulleyTeeth[1])
                    ]
            } as BeltDefinition;
}

/**
 * One of a simple belt's two pulleys. Its selection isn't its identity, so changing what's selected doesn't change what
 * the belt's faces are.
 */
function twoPulleyDefinition(location is Vector, pulleyTeeth is number) returns PulleyDefinition
{
    return {
                "location" : location,
                "pulleyType" : PulleyType.INSIDE_PULLEY,
                "pulleyTeeth" : pulleyTeeth
            } as PulleyDefinition;
}

/**
 * A simple belt's pulleys: its plane (pulley one's, with the start offset), pulley two's center on it, and their
 * teeth.
 */
function getSimpleBeltPulleys(context is Context, definition is map, beltType is BeltType) returns map
{
    verifyNonemptyQuery(context, definition, "pulleyOneSelection", "Select a sketch point, circle, mate connector, or Robot pulley to use for pulley one.");
    verifyNonemptyQuery(context, definition, "pulleyTwoSelection", "Select a sketch point, circle, mate connector, or Robot pulley to use for pulley two.");
    const one = getPulleySelection(context, definition, beltType, definition.pulleyOneSelectionType, "pulleyOneSelection", definition.pulleyOneTeeth);
    const two = getPulleySelection(context, definition, beltType, definition.pulleyTwoSelectionType, "pulleyTwoSelection", definition.pulleyTwoTeeth);
    for (var selection in [[one, "pulleyOneSelection"], [two, "pulleyTwoSelection"]])
    {
        if (selection[0].idler)
        {
            throw regenError("A simple belt's pulleys are both inside it: select a pulley, not an idler.", [selection[1]], definition[selection[1]]);
        }
    }
    const beltPlane = applyStartOffset(context, definition, one.plane);
    const secondPoint = project(beltPlane, two.plane.origin);
    if (tolerantEquals(secondPoint, beltPlane.origin))
    {
        throw regenError("The selected pulleys are on the same axis.", ["pulleyOneSelection", "pulleyTwoSelection"],
            qUnion(definition.pulleyOneSelection, definition.pulleyTwoSelection));
    }
    return {
            "beltPlane" : beltPlane,
            "secondPoint" : secondPoint,
            "selections" : [one, two]
        };
}

/**
 * A simple belt's definition: its pulleys are as far apart as its belt makes them, unless they're selected about that
 * far apart already (as `validateCenterToCenter` says), when they're where they're selected, so other parts line up
 * with them.
 */
function getSimpleBeltDefinition(context is Context, id is Id, definition is map) returns BeltDefinition
{
    const beltValue = getBeltValue(getBeltTableAndPath(definition));
    const pulleys = getSimpleBeltPulleys(context, definition, beltValue.beltType);
    reportSelectionWarnings(context, id, definition, pulleys.selections, ["pulleyOneSelection", "pulleyTwoSelection"]);
    if (pulleys.selections[1].selectionType != SelectionType.GEOMETRY && !isPointOnPlane(pulleys.selections[1].plane.origin, pulleys.beltPlane))
    {
        reportFeatureWarning(context, id, "The selected pulleys aren't lined up with each other.", ["pulleyTwoSelection"]);
    }

    const pulleyTeeth = [pulleys.selections[0].pulleyTeeth, pulleys.selections[1].pulleyTeeth];
    const toSecond = pulleys.secondPoint - pulleys.beltPlane.origin;
    const measuredCenterToCenter = norm(toSecond);
    const idealCenterToCenter = computeBeltCenterToCenter(beltValue.beltType, definition.beltTeeth, pulleyTeeth, measuredCenterToCenter) + definition.centerToCenterAdjustment;
    // If the measured center to center is way off, fall back to the ideal center to center
    const centerToCenter = validateCenterToCenter(context, id, definition, measuredCenterToCenter, idealCenterToCenter) ? measuredCenterToCenter : idealCenterToCenter;

    return {
                "beltPlane" : pulleys.beltPlane,
                "beltType" : beltValue.beltType,
                "beltWidth" : beltValue.beltWidth,
                "beltTeeth" : definition.beltTeeth,
                "pulleyDefinitions" : [
                        twoPulleyDefinition(zeroVector(2) * meter, pulleyTeeth[0]),
                        twoPulleyDefinition(worldToPlane(pulleys.beltPlane, pulleys.beltPlane.origin + normalize(toSecond) * centerToCenter), pulleyTeeth[1])
                    ]
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

/**
 * A complex belt's pulleys and idlers, as selected: their selections, and the belt's plane (the first's, with the
 * start offset).
 */
function getComplexBeltPulleys(context is Context, definition is map, beltType is BeltType) returns map
{
    verifyNonemptyArray(context, definition, "pulleys", "Add pulley locations.");
    if (size(definition.pulleys) < 2 && !isOpenBelt(definition))
    {
        throw regenError("Add at least two pulleys.", ["pulleys"]);
    }
    var selections = [];
    for (var i, pulley in definition.pulleys)
    {
        const parameterName = arrayParameterId("pulleys", i, "pulleySelection");
        verifyNonemptyArrayQuery(context, definition, parameterName, "Select a pulley location.");
        selections = append(selections, getPulleySelection(context, definition, beltType, pulley.selectionType, parameterName, pulley.pulleyTeeth));
    }
    // An open belt's plane is its start's
    var basePlane = selections[0].plane;
    if (isOpenBelt(definition))
    {
        verifyNonemptyQuery(context, definition, "startPoint", "Select where the belt starts.");
        basePlane = evVertexCoordSystem(context, { "vertex" : definition.startPoint })->plane();
    }
    return {
            "beltPlane" : applyStartOffset(context, definition, basePlane),
            "selections" : selections
        };
}

function getComplexBeltDefinition(context is Context, id is Id, definition is map) returns BeltDefinition
{
    const beltValue = getBeltValue(getBeltTableAndPath(definition));
    const pulleys = getComplexBeltPulleys(context, definition, beltValue.beltType);
    const beltPlane = pulleys.beltPlane;
    var selections = pulleys.selections;

    var pulleyDefinitions = [];
    var parameterNames = [];
    for (var i, pulley in definition.pulleys)
    {
        const selection = selections[i];
        const parameterName = arrayParameterId("pulleys", i, "pulleySelection");
        parameterNames = append(parameterNames, parameterName);
        var pulleyDefinition = {
            "identity" : pulley.pulleySelection,
            // Projected onto the belt's plane
            "location" : worldToPlane(beltPlane, selection.plane.origin),
            "pulleyType" : getPulleyType(definition, pulley)
        };

        if (selection.selectionType == SelectionType.ROBOT_PULLEY)
        {
            if (!isPointOnPlane(selection.plane.origin, beltPlane))
            {
                reportFeatureWarning(context, id, "A selected Robot pulley isn't lined up with the belt.", [parameterName]);
            }
            if (pulleyDefinition.pulleyType == PulleyType.IDLER && !selection.idler)
            {
                reportFeatureInfo(context, id, "A Robot pulley is being used as an idler.");
            }
        }

        if (isPulley(pulleyDefinition.pulleyType))
        {
            if (selection.idler)
            {
                throw regenError("The selected Robot pulley is an idler, which only goes on a single sided belt's outside.", [parameterName], pulley.pulleySelection);
            }
            pulleyDefinition.pulleyTeeth = selection.pulleyTeeth;
        }
        else
        {
            pulleyDefinition.idlerRadius = getIdlerRadius(pulley, selection);
            // An idler's teeth don't matter
            selections[i].fractionalTeeth = false;
        }
        pulleyDefinitions = append(pulleyDefinitions, pulleyDefinition as PulleyDefinition);
    }
    reportSelectionWarnings(context, id, definition, selections, parameterNames);

    return {
                "beltPlane" : beltPlane,
                "beltType" : beltValue.beltType,
                "beltWidth" : beltValue.beltWidth,
                "beltTeeth" : definition.beltTeeth,
                "pulleyDefinitions" : pulleyDefinitions
            } as BeltDefinition;
}

/**
 * An idler's radius: a Robot pulley's (see `getPulleySelection`), or its Idler diameter's.
 */
function getIdlerRadius(pulley is map, selection is map) returns ValueWithUnits
{
    return selection.selectionType == SelectionType.ROBOT_PULLEY ? selection.idlerRadius : pulley.idlerDiameter / 2;
}

/**
 * Where a pulley is, and its teeth, from its selection of `selectionType` (the parameter `parameterName`): a sketch
 * point, circle, or mate connector (geometry, with `teeth`), a Robot pulley or one of its mate connectors, or a
 * pitch circle (whose size gives its teeth).
 *
 * @returns {{
 *      @field selectionType {SelectionType} :
 *      @field plane {Plane} : Its plane, at its center.
 *      @field pulleyTeeth {number} : `undefined` for a Robot pulley idler.
 *      @field idler {boolean} : It's a Robot pulley idler.
 *      @field idlerRadius {ValueWithUnits} : A Robot pulley's, as an idler: an idler's radius, or a pulley's teeth's
 *              tips'.
 *      @field fractionalTeeth {boolean} : A pitch circle's teeth aren't a whole number (`pulleyTeeth` is rounded).
 *      @field otherBeltType {boolean} : A Robot pulley's for another type of belt.
 * }}
 */
function getPulleySelection(context is Context, definition is map, beltType is BeltType, selectionType is SelectionType, parameterName is string, teeth) returns map
{
    const selection = getParameter(definition, parameterName);
    var result = { "selectionType" : selectionType, "idler" : false, "fractionalTeeth" : false, "otherBeltType" : false };
    if (selectionType == SelectionType.ROBOT_PULLEY)
    {
        // A two belt pulley's mate connectors have attributes of their own, for each belt
        const attribute = getAttribute(context, {
                    "entity" : selection,
                    "name" : PULLEY_ATTRIBUTE
                });
        if (attribute == undefined || !canBePulleyAttribute(attribute) || attribute.coordSystem.coordSystem == undefined)
        {
            throw regenError("Select a pulley made by Robot pulley, or one of its mate connectors.", [parameterName], selection);
        }
        if (attribute.twoBelts && !isMateConnector(context, selection))
        {
            throw regenError("A pulley for two belts has a mate connector for each: select one.", [parameterName], selection);
        }
        result.plane = attribute.coordSystem.coordSystem->plane();
        result.idler = attribute.idlerRadius != undefined;
        result.pulleyTeeth = attribute.pulleyTeeth;
        // A pulley used as an idler has the belt's back on its teeth's tips
        result.idlerRadius = result.idler ? attribute.idlerRadius :
            getPulleyRadius(getBeltPitch(attribute.beltType), attribute.pulleyTeeth) - getBeltModelInfo(attribute.beltType).insideThickness;
        result.otherBeltType = attribute.beltType != beltType;
    }
    else if (selectionType == SelectionType.PITCH_CIRCLE)
    {
        if (!isCircle(context, selection))
        {
            throw regenError("Select a circle the size of the pulley's pitch circle.", [parameterName], selection);
        }
        const circle = evCurveDefinition(context, { "edge" : selection });
        const pitchTeeth = computeTargetPulleyTeeth(getBeltPitch(beltType), circle.radius);
        result.plane = circle.coordSystem->plane();
        result.pulleyTeeth = max(round(pitchTeeth), 1);
        result.fractionalTeeth = !tolerantEquals(pitchTeeth, round(pitchTeeth));
    }
    else
    {
        result.plane = getGeometryPlane(context, selection, parameterName);
        result.pulleyTeeth = teeth;
    }
    return result;
}

/**
 * Reports the warnings `getPulleySelection`'s results call for.
 */
function reportSelectionWarnings(context is Context, id is Id, definition is map, selections is array, parameterNames is array)
{
    var fractional = [];
    var otherBeltType = [];
    for (var i, selection in selections)
    {
        if (selection.fractionalTeeth)
        {
            fractional = append(fractional, parameterNames[i]);
        }
        if (selection.otherBeltType)
        {
            otherBeltType = append(otherBeltType, parameterNames[i]);
        }
    }
    if (fractional != [])
    {
        reportFeatureWarning(context, id, "A selected pitch circle isn't the size of a pulley with a whole number of teeth: it's used as the nearest one.", fractional);
    }
    if (otherBeltType != [])
    {
        reportFeatureWarning(context, id, "A selected Robot pulley is for another type of belt.", otherBeltType);
    }
}

/**
 * The plane of a sketch point, circle, or mate connector, at its center.
 */
function getGeometryPlane(context is Context, selection is Query, parameterName is string) returns Plane
{
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
 * Reports a warning if the measuredCenterToCenter does not closely match the idealCenterToCenter.
 */
function validateCenterToCenter(context is Context, id is Id, definition is map, measuredCenterToCenter is ValueWithUnits, idealCenterToCenter is ValueWithUnits) returns boolean
{
    if (definition.disableBeltValidation || withinDisplayPrecision(definition, measuredCenterToCenter, idealCenterToCenter))
    {
        return true;
    }

    // Doesn't end with a period, to make copying the distance easier:
    // To use this belt, update the part studio so the distance between your selections is 4.5 in
    const centerToCenterString = makeValueString(definition.unitSystem, idealCenterToCenter);
    reportFeatureWarning(context, id, "To use this belt, update the part studio so the distance between your selections is " ~ centerToCenterString);
    return false;
}

function validateComplexBeltLength(context is Context, id is Id, definition is map, beltDefinition is BeltDefinition, actualLength is ValueWithUnits)
{
    if (definition.disableBeltValidation)
    {
        return;
    }
    const idealLength = getBeltPitch(beltDefinition.beltType) * beltDefinition.beltTeeth + definition.beltFitAdjustment;

    if (withinDisplayPrecision(definition, actualLength, idealLength))
    {
        reportFeatureInfo(context, id, "Your selections match the selected " ~ beltDefinition.beltTeeth ~ "T belt. Note you may still want to design an adjustable tensioner to ensure proper fit.");
        return;
    }

    // The belt is longer than the path, so its path should be bigger, or the belt smaller
    const tooLong = idealLength > actualLength;
    // The selected belt is 0.25 in too long. To use this belt, get the distance close by selecting a smaller belt
    // and/or increasing the total perimeter, then use Robot tensioner to compute an exact solution.
    reportFeatureWarning(context, id, "The selected belt is " ~ makeValueString(definition.unitSystem, abs(idealLength - actualLength)) ~
            " too " ~ (tooLong ? "long" : "short") ~ ". To use this belt, get the distance close by selecting a " ~
            (tooLong ? "smaller" : "larger") ~ " belt and/or " ~ (tooLong ? "increasing" : "decreasing") ~
            " the total perimeter, then use Robot tensioner to compute an exact solution.");
}

/**
 * The number of teeth a pulley whose pitch circle has a radius of `pitchRadius` has: not a whole number, unless it's
 * just right.
 */
function computeTargetPulleyTeeth(pitch is ValueWithUnits, pitchRadius is ValueWithUnits) returns number
{
    return 2 * PI * pitchRadius / pitch;
}

/**
 * The center to center distance of a belt with `teeth` teeth on two pulleys with `pulleyTeethArray` teeth: exactly,
 * nearest `nearDistance` (from the usual formula's, if it's zero). Belts' center to center formulas only approximate
 * the path around their pulleys, while Robot tensioner measures it exactly.
 */
function computeBeltCenterToCenter(beltType is BeltType, teeth is number, pulleyTeethArray is array, nearDistance is ValueWithUnits) returns ValueWithUnits
{
    const pitch = getBeltPitch(beltType);
    const radii = mapArray(pulleyTeethArray, function(pulleyTeeth)
        {
            return getPulleyRadius(pitch, pulleyTeeth);
        });
    const length = pitch * teeth;
    var estimate = nearDistance;
    if (tolerantEqualsZero(estimate))
    {
        const term = (length - PI * (radii[0] + radii[1])) / 4;
        const discriminant = term ^ 2 - (2 * (radii[1] - radii[0])) ^ 2 / 8;
        if (term <= 0 * meter || discriminant < 0 * meter ^ 2)
        {
            // The selected belt is way too small, e.g., it's smaller than the circumference of one of the pulleys
            throw regenError("The selected belt is too small for its pulleys.", ["pulleyOneTeeth", "pulleyTwoTeeth", "beltTeeth"]);
        }
        estimate = term + sqrt(discriminant);
    }
    const offset = nearestRoot(function(x)
        {
            const pathLength = tryTwoPulleyLength(radii, estimate + x);
            return pathLength == undefined ? undefined : pathLength - length;
        }, 0.1 * millimeter, TOLERANCE.zeroLength * meter);
    if (offset == undefined)
    {
        throw regenError("The selected belt is too small for its pulleys.", ["pulleyOneTeeth", "pulleyTwoTeeth", "beltTeeth"]);
    }
    return estimate + offset;
}

/**
 * The length of the path around two pulleys of `radii`, `distance` apart, or `undefined` if there's none.
 */
function tryTwoPulleyLength(radii is array, distance is ValueWithUnits)
{
    return tryLoopLength([
                { "location" : zeroVector(2) * meter, "radius" : radii[0], "flipped" : false } as BoundaryCircle,
                { "location" : vector(distance, 0 * meter), "radius" : radii[1], "flipped" : false } as BoundaryCircle
            ], true);
}

/**
 * Creates the belt, with its attributes and properties, from the sketch of its path (see `sketchLoop`).
 */
function createBelt(context is Context, id is Id, definition is map, beltDefinition is BeltDefinition, sketch is map, counterClockwise is boolean) returns Query
{
    const arcTracking = mapArray(sketch.arcs, function(arc)
        {
            return startTracking(context, arc);
        });

    const beltModel = {
                "modelBeltTeeth" : definition.modelBeltTeeth,
                "isDoubleSidedBelt" : isDoubleSidedBelt(definition),
                "beltType" : beltDefinition.beltType,
                "beltTeeth" : beltDefinition.beltTeeth,
                "beltWidth" : beltDefinition.beltWidth
            } as BeltModel;

    const belt = extrudeBelt(context, id, beltModel, beltDefinition.beltPlane, sketch.path, counterClockwise).belt;

    for (var i, tracking in arcTracking)
    {
        // Both of its curved faces, but a double sided belt's around many teeth has none
        const faces = tracking->qEntityFilter(EntityType.FACE)->qGeometry(GeometryType.CYLINDER);
        if (!isQueryEmpty(context, faces))
        {
            setAttribute(context, {
                        "entities" : faces,
                        "name" : BELT_PULLEY_FACE_ATTRIBUTE,
                        "attribute" : beltFaceAttribute(beltDefinition, beltDefinition.pulleyDefinitions[i])
                    });
        }
    }

    setBeltProperties(context, belt, getBeltName(beltModel.beltTeeth, beltModel.isDoubleSidedBelt, beltModel.beltType));
    return belt;
}

function beltFaceAttribute(beltDefinition is BeltDefinition, pulleyDefinition is PulleyDefinition) returns BeltFaceAttribute
{
    return {
                "beltType" : beltDefinition.beltType,
                "beltWidth" : beltDefinition.beltWidth,
                "pulleyType" : pulleyDefinition.pulleyType,
                "pulleyTeeth" : pulleyDefinition.pulleyTeeth,
                "idlerRadius" : pulleyDefinition.idlerRadius
            } as BeltFaceAttribute;
}

function beltLoopAttribute(definition is map, beltDefinition is BeltDefinition, circles is array, counterClockwise is boolean) returns LoopAttribute
{
    var length = getBeltPitch(beltDefinition.beltType) * beltDefinition.beltTeeth;
    if (isComplexBelt(definition))
    {
        length += definition.beltFitAdjustment;
    }
    var loopCircles = [];
    for (var i, circle in circles)
    {
        circle.identity = undefined;
        circle.idlerRadius = beltDefinition.pulleyDefinitions[i].idlerRadius;
        loopCircles = append(loopCircles, circle);
    }
    // Like "100T double sided 5mm HTD belt"
    const name = beltDefinition.beltTeeth ~ "T " ~ (isDoubleSidedBelt(definition) ? "double sided " : "") ~ getBeltTypeName(beltDefinition.beltType) ~ " belt";
    return loopAttribute(name, beltDefinition.beltPlane, length, loopCircles, counterClockwise);
}

/**
 * The circles a belt's path goes around: a pulley's pitch circle, or an idler, with the belt's back over it.
 */
function getBoundaryCircles(beltDefinition is BeltDefinition) returns array
{
    return mapArray(beltDefinition.pulleyDefinitions, function(pulleyDefinition)
        {
            var radius;
            if (isPulley(pulleyDefinition.pulleyType))
            {
                radius = getPulleyRadius(getBeltPitch(beltDefinition.beltType), pulleyDefinition.pulleyTeeth);
            }
            else
            {
                radius = pulleyDefinition.idlerRadius + getBeltOutsideThickness(beltDefinition.beltType);
            }
            return {
                        "location" : pulleyDefinition.location,
                        "identity" : pulleyDefinition.identity,
                        "radius" : radius,
                        "flipped" : isOutside(pulleyDefinition.pulleyType)
                    } as BoundaryCircle;
        });
}

function setBeltProperties(context is Context, belt is Query, name is string)
{
    setProperty(context, {
                "entities" : belt,
                "propertyType" : PropertyType.MATERIAL,
                "value" : material("Viton Rubber", 1827 * kilogram / meter ^ 3) // default belt material and density
            });
    setProperty(context, {
                "entities" : belt,
                "propertyType" : PropertyType.APPEARANCE,
                "value" : BLACK
            });
    setProperty(context, {
                "entities" : belt,
                "propertyType" : PropertyType.NAME,
                "value" : name
            });
}

/**
 * Like `100T Double Sided 5mm HTD Belt`.
 */
function getBeltName(beltTeeth is number, isDoubleSidedBelt is boolean, beltType is BeltType) returns string
{
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

        coordSystem.origin = planeToWorld(beltDefinition.beltPlane, pulleyDefinition.location);
        opMateConnector(context, mateConnectorId, {
                    "coordSystem" : coordSystem,
                    "owner" : belt
                });
        setAttribute(context, {
                    "entities" : qCreatedBy(mateConnectorId, EntityType.BODY),
                    "name" : BELT_PULLEY_FACE_ATTRIBUTE,
                    "attribute" : beltFaceAttribute(beltDefinition, pulleyDefinition)
                });
    }
}

const BELT_SIDE_FLIP_MANIPULATOR = "beltSideFlipManipulator";

function addBeltFlipManipulators(context is Context, id is Id, beltDefinition is BeltDefinition, arcs is array, counterClockwise is boolean)
{
    var manipulators = {};
    for (var i, pulleyDefinition in beltDefinition.pulleyDefinitions)
    {
        const tangentLine = evEdgeTangentLine(context, {
                    "edge" : arcs[i],
                    "parameter" : 0.5
                });
        var base = tangentLine.origin;
        // Points outwards from the belt
        const direction = cross(beltDefinition.beltPlane.normal, tangentLine.direction) * (counterClockwise ? 1 : -1);

        if (pulleyDefinition.pulleyType == PulleyType.IDLER)
        {
            // On the idler, inside the belt's back
            base += direction * getBeltOutsideThickness(beltDefinition.beltType);
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
        const parsed = match(key, BELT_SIDE_FLIP_MANIPULATOR ~ "\\.(\\d+)");
        if (parsed.hasMatch && manipulator.flipped)
        {
            const index = stringToNumber(parsed.captures[1]);
            const currentSide = definition.pulleys[index].beltSide;
            // Swap the side each time the manipulator is clicked
            definition.pulleys[index].beltSide = currentSide == BeltSide.INSIDE ? BeltSide.OUTSIDE : BeltSide.INSIDE;
        }
    }
    definition = startOffsetManipulatorChange(definition, newManipulators);
    return definition;
}

/**
 * Sets each pulley's location type from what's selected, when its selection changes (see `locationKind`); Belt teeth
 * to the belt chosen, unless its supplier's Custom; and the belt to the one which fits the selections best when
 * Select closest belt's clicked, or (while the feature's being created) the selections change and no belt's been
 * chosen.
 */
export function robotBeltEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query, clickedButton is string) returns map
{
    const selections = setSelectionTypes(context, oldDefinition, definition);
    definition = selections.definition;

    if (oldDefinition != {} && beltChanged(oldDefinition, definition))
    {
        const tableAndPath = getBeltTableAndPath(definition);
        if (!isOpenBelt(definition) && !hasCustomTeeth(tableAndPath.path))
        {
            definition.beltTeeth = getBeltValue(tableAndPath).beltTeeth;
        }
    }

    const beltChosen = (specifiedParameters.beltPath ?? false) || (specifiedParameters.doubleBeltPath ?? false) || (specifiedParameters.beltTeeth ?? false);
    const autoChoose = isCreating && selections.changed && !beltChosen;
    if (!isOpenBelt(definition) && !isStandaloneBelt(definition) && (clickedButton == "selectClosestBelt" || autoChoose))
    {
        // A guard: if the selections aren't all there (or are wrong), there's no closest belt
        try silent
        {
            definition = setBeltTeeth(definition, closestBeltTeeth(context, definition));
        }
    }
    return definition;
}

/**
 * Sets the location type of each pulley whose selection changed, from what it is (see `locationKind`).
 *
 * @returns {{
 *      @field definition {map} :
 *      @field changed {boolean} : Whether any selection changed.
 * }}
 */
function setSelectionTypes(context is Context, oldDefinition is map, definition is map) returns map
{
    const pitch = getBeltPitch(getBeltValue(getBeltTableAndPath(definition)).beltType);
    const kindOf = function(selection is Query)
        {
            const kind = locationKind(context, selection, PULLEY_ATTRIBUTE, function(radius)
                {
                    return computeTargetPulleyTeeth(pitch, radius);
                });
            if (kind == undefined)
            {
                return undefined;
            }
            return switch (kind) {
                        "part" : SelectionType.ROBOT_PULLEY,
                        "pitchCircle" : SelectionType.PITCH_CIRCLE,
                        "center" : SelectionType.GEOMETRY
                    };
        };
    var changed = false;
    if (isSimpleBelt(definition))
    {
        if (!isStandaloneBelt(definition))
        {
            for (var name in ["pulleyOne", "pulleyTwo"])
            {
                const selection = definition[name ~ "Selection"];
                const old = oldDefinition[name ~ "Selection"];
                if (old != undefined && areQueriesEquivalent(context, old, selection))
                {
                    continue;
                }
                const selectionType = kindOf(selection);
                if (selectionType != undefined)
                {
                    definition[name ~ "SelectionType"] = selectionType;
                    changed = true;
                }
            }
        }
    }
    else
    {
        for (var i, pulley in definition.pulleys)
        {
            if (!selectionChanged(context, oldDefinition.pulleys, i, "pulleySelection", pulley.pulleySelection))
            {
                continue;
            }
            const selectionType = kindOf(pulley.pulleySelection);
            if (selectionType != undefined)
            {
                definition.pulleys[i].selectionType = selectionType;
                changed = true;
            }
        }
    }
    return { "definition" : definition, "changed" : changed };
}

function beltChanged(oldDefinition is map, definition is map) returns boolean
{
    return getBeltTableAndPath(oldDefinition).path != getBeltTableAndPath(definition).path;
}

/**
 * The teeth of the belt (of the type chosen) which fits the selections best: of those its supplier sells, or any.
 */
function closestBeltTeeth(context is Context, definition is map) returns number
{
    const tableAndPath = getBeltTableAndPath(definition);
    const beltValue = getBeltValue(tableAndPath);
    const pitch = getBeltPitch(beltValue.beltType);

    var length;
    if (isSimpleBelt(definition))
    {
        const pulleys = getSimpleBeltPulleys(context, definition, beltValue.beltType);
        const radii = mapArray(pulleys.selections, function(selection)
            {
                return getPulleyRadius(pitch, selection.pulleyTeeth);
            });
        const distance = norm(pulleys.secondPoint - pulleys.beltPlane.origin) - definition.centerToCenterAdjustment;
        length = tryTwoPulleyLength(radii, distance);
    }
    else
    {
        const pulleys = getComplexBeltPulleys(context, definition, beltValue.beltType);
        var circles = [];
        for (var i, pulley in definition.pulleys)
        {
            const pulleyType = getPulleyType(definition, pulley);
            circles = append(circles, {
                            "location" : worldToPlane(pulleys.beltPlane, pulleys.selections[i].plane.origin),
                            "radius" : isPulley(pulleyType) ? getPulleyRadius(pitch, pulleys.selections[i].pulleyTeeth) :
                                getIdlerRadius(pulley, pulleys.selections[i]) + getBeltOutsideThickness(beltValue.beltType),
                            "flipped" : isOutside(pulleyType)
                        } as BoundaryCircle);
        }
        length = tryLoopLength(circles, loopCounterClockwise(circles));
        if (length != undefined)
        {
            length -= definition.beltFitAdjustment;
        }
    }
    if (length == undefined)
    {
        throw "There's no path around the selections.";
    }

    const teeth = length / pitch;
    const options = getCurrentBeltOptionsArray(tableAndPath);
    if (options == undefined)
    {
        return max(round(teeth), 1);
    }
    var closest = options[0];
    for (var option in options)
    {
        if (abs(option - teeth) < abs(closest - teeth))
        {
            closest = option;
        }
    }
    return closest;
}

/**
 * Sets the belt, and Belt teeth, to `teeth`.
 */
function setBeltTeeth(definition is map, teeth is number) returns map
{
    const pathName = isDoubleSidedBelt(definition) ? "doubleBeltPath" : "beltPath";
    if (!hasCustomTeeth(definition[pathName]))
    {
        definition[pathName].teeth = teeth ~ "T";
    }
    definition.beltTeeth = teeth;
    return definition;
}
