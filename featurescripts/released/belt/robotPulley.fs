FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "onshape/std/chamfer.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "6b7d5995c565ae73c7740b0b");

export import(path : "484d2d590d4a2ab919981b0e", version : "7137aa56702a1f5e39558ef4");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "7ff3897ddcba9a81bae27310");

annotation {
        "Feature Type Name" : "Robot pulley",
        "Manipulator Change Function" : "robotPulleyManipulatorChange",
        "Feature Type Description" : "Create GT2, HTD, and RT25 pulleys." ~
        "<br>See also the Robot belt FeatureScript, which works directly with this feature." ~ CREDIT,
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotPulley = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        pulleyPredicate(definition);
    }
    {
        doRobotPulley(context, id, definition);
    });

function doRobotPulley(context is Context, id is Id, definition is map)
{
    var pulleyDefinitions;
    if (definition.creationMethod == CreationMethod.MANUAL)
    {
        const pointDistances = getPointDistances(definition);
        // Manual mode will only ever create a single pulley
        pulleyDefinitions = [getManualPulleyDefinition(context, definition, pointDistances)];
        addManualManipulators(context, id, definition, pulleyDefinitions[0], pointDistances);
    }
    else
    {
        pulleyDefinitions = getBeltPulleyDefinitions(context, definition);
    }

    // Try to add manipulators as early as possible
    if (definition.addFlanges && definition.addText)
    {
        addTextPositionManipulator(context, id, definition, pulleyDefinitions[0]);
    }

    const pulleys = createPulleys(context, id + "pulley", definition, pulleyDefinitions);

    if (offsetProfile(definition))
    {
        addProfileManipulator(context, id, definition, pulleyDefinitions[0].plane, pulleys[0]);
    }

    for (var i, pulleyDefinition in pulleyDefinitions)
    {
        setPulleyProperties(context, pulleyDefinition, pulleys[i]);
    }

    if (definition.addFlanges)
    {
        addFlanges(context, id + "flange", definition, pulleyDefinitions, pulleys);
    }

    // A two belt pulley's belts are added to its mate connectors
    if (definition.addMateConnectors || definition.twoBelts)
    {
        addMateConnectors(context, id + "mateConnector", definition, pulleyDefinitions, pulleys);
    }

    if (definition.addBore)
    {
        addBores(context, id + "bore", definition, pulleyDefinitions, pulleys);
    }

    // Add last to allow handling bore overlap error
    if (definition.addFlanges && definition.addText)
    {
        addText(context, id + "text", definition, pulleyDefinitions, pulleys);
    }
}

function addManualManipulators(context is Context, id is Id, definition is map, pulleyDefinition is PulleyDefinition, pointDistances is array)
{
    const plane = pulleyDefinition.plane;

    var startPlane = plane;
    startPlane.origin += startPlane.normal * getPointDistance(definition, pointDistances);
    addStartOffsetManipulator(context, id, definition, startPlane);

    const points = mapArray(pointDistances, function(distance)
        {
            return plane.origin + plane.normal * distance;
        });
    addPointManipulator(context, id, definition, points);
}

function addProfileManipulator(context is Context, id is Id, definition is map, plane is Plane, pulley is Query)
{
    const profileAxis = line(plane.origin, -plane.x);
    const pulleyFaces = qOwnedByBody(pulley, EntityType.FACE);
    const outsideFaces = pulleyFaces->qSubtraction(pulleyFaces->qParallelPlanes(plane));
    addProfileOffsetManipulator(context, id, PROFILE_OFFSET_MANIPULATOR, profileAxis, outsideFaces, definition.profileOffsetOppositeDirection);
}

/**
 * Computes the radius of a flange.
 */
function getFlangeRadius(definition is map, beltType is BeltType, teeth is number) returns ValueWithUnits
{
    const pulleyRadius = getPulleyRadius(getBeltPitch(beltType), teeth) + getProfileOffset(definition);
    return pulleyRadius + getFlangeRadiusOffset(beltType, definition.flangeSize, definition.unitSystem);
}

/**
 * Returns the width of just the toothed portion of a pulley.
 */
function getPulleyTeethWidth(definition is map, beltType is BeltType, beltWidth is ValueWithUnits) returns ValueWithUnits
{
    var baseWidth = getBasePulleyTeethWidth(beltType, beltWidth, definition.unitSystem);
    baseWidth += definition.twoBelts ? getTwoBeltPulleyExtraWidth(beltType, beltWidth, definition.unitSystem) : 0 * meter;
    return baseWidth;
}

/**
 * Returns the total width of a pulley, including flanges.
 */
function getPulleyWidth(definition is map, beltType is BeltType, beltWidth is ValueWithUnits) returns ValueWithUnits
{
    var teethWidth = getPulleyTeethWidth(definition, beltType, beltWidth);
    if (definition.addFlanges)
    {
        return teethWidth + getFlangeWidth(beltType, definition.flangeSize, definition.unitSystem) * 2;
    }
    return teethWidth;
}

/**
 * Returns an array of distances each point in the point manipulator should be relative to the center of the pulley.
 */
function getPointDistances(definition is map) returns array
{
    const beltValue = getBeltTypeValue(definition);
    const pulleyWidth = getPulleyWidth(definition, beltValue.beltType, beltValue.beltWidth);
    if (!definition.twoBelts)
    {
        return vector([0 * meter, pulleyWidth, -pulleyWidth]) / 2;
    }
    const extraWidth = getTwoBeltPulleyExtraWidth(beltValue.beltType, beltValue.beltWidth, definition.unitSystem);
    return vector([extraWidth, pulleyWidth, -pulleyWidth, -extraWidth]) / 2;
}

/**
 * A collection of information specific to each pulley used for modeling.
 * @type {{
 *      @field plane : A plane at the center of the pulley.
 *      @field width : The width of the pulley face.
 * }}
 */
type PulleyDefinition typecheck canBePulleyDefinition;

export predicate canBePulleyDefinition(value)
{
    value is map;
    value.identity is Query;
    value.plane is Plane;
    value.beltType is BeltType;
    value.beltWidth is ValueWithUnits;
    value.teeth is number;
    // value.width is ValueWithUnits;
}

function getManualPulleyDefinition(context is Context, definition is map, pointDistances is array) returns PulleyDefinition
{
    const basePlane = getLocationPlane(context, definition);

    var plane = applyStartOffset(context, definition, basePlane);
    plane = applyPointManipulator(definition, plane, pointDistances);

    const beltValue = getBeltTypeValue(definition);

    return {
                "identity" : undefined, // Don't use identity to improve robustness
                "plane" : plane,
                "beltType" : beltValue.beltType,
                "beltWidth" : beltValue.beltWidth,
                "teeth" : definition.pulleyTeeth,
            } as PulleyDefinition;
}

function getBeltPulleyDefinitions(context is Context, definition is map) returns array
{
    const beltSelections = verifyNonemptyQuery(context, definition, "beltSelections", "Select curved belt faces or belt mate connectors to use.");
    // Deduplicate belts by location to prevent duplicate pulley creation
    // Also handles case where belt teeth are added to selected face, resulting in duplicate pulleys
    var usedLocations = {};
    var pulleyDefinitions = [];
    for (var selection in beltSelections)
    {
        const attribute = getBeltPulleyFaceAttribute(context, selection);
        if (attribute == undefined)
        {
            throw regenError("Selected face is not a valid pulley face belonging to a robot belt.", ["beltSelections"], selection);
        }
        else if (attribute.pulleyType == PulleyType.IDLER)
        {
            throw regenError("Cannot add pulley to idler location.", ["beltSelections"], selection);
        }

        var pulleyPlane; // Avoid shadowing the plane function
        if (isMateConnector(context, selection))
        {
            pulleyPlane = evVertexCoordSystem(context, { "vertex" : selection })->plane();
        }
        else
        {
            pulleyPlane = evSurfaceDefinition(context, { "face" : selection }).coordSystem->plane();
            pulleyPlane.x = pulleyPlane->yAxis(); // Rotate plane to be consistent with belts
            // The cylinder's origin is anywhere on its axis: the belt's middle is the face's
            const centroid = evApproximateCentroid(context, { "entities" : selection });
            pulleyPlane.origin += pulleyPlane.normal * dot(centroid - pulleyPlane.origin, pulleyPlane.normal);
        }
        if (definition.twoBelts)
        {
            // The selected belt runs on the pulley's first side (see addMateConnectors), or its second, flipped
            const extraWidth = getTwoBeltPulleyExtraWidth(attribute.beltType, attribute.beltWidth, definition.unitSystem);
            pulleyPlane.origin += pulleyPlane.normal * extraWidth / 2 * (definition.flipTwoBeltSide ? -1 : 1);
        }

        if (usedLocations[pulleyPlane.origin] != undefined)
        {
            continue;
        }
        usedLocations[pulleyPlane.origin] = true;

        const pulleyDefinition = {
                    "identity" : selection,
                    "plane" : pulleyPlane,
                    "beltType" : attribute.beltType,
                    "beltWidth" : attribute.beltWidth,
                    "teeth" : attribute.pulleyTeeth,
                } as PulleyDefinition;
        pulleyDefinitions = append(pulleyDefinitions, pulleyDefinition);
    }
    return pulleyDefinitions;
}

function getBeltPulleyFaceAttribute(context is Context, selection is Query)
{
    return getAttribute(context, {
                "entity" : isMateConnector(context, selection) ? selection->qOwnerBody() : selection,
                "name" : BELT_PULLEY_FACE_ATTRIBUTE
            });
}

function createPulleys(context is Context, id is Id, definition is map, pulleyDefinitions is array) returns array
{
    const profileOffset = getProfileOffset(definition);
    var pulleys = [];
    for (var i, pulleyDefinition in pulleyDefinitions)
    {
        const pulleyId = id + unstableIdComponent(i);
        if (pulleyDefinition.identity != undefined)
        {
            setExternalDisambiguation(context, pulleyId, pulleyDefinition.identity);
        }
        const plane = pulleyDefinition.plane;
        const profile = sketchPulleyProfile(context, pulleyId + "profile", plane, pulleyDefinition.beltType, pulleyDefinition.teeth, profileOffset);
        const teethWidth = getPulleyTeethWidth(definition, pulleyDefinition.beltType, pulleyDefinition.beltWidth);
        opExtrude(context, pulleyId + "extrude", {
                    "entities" : profile,
                    "direction" : plane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : teethWidth / 2,
                    "startBound" : BoundingType.BLIND,
                    "startDepth" : teethWidth / 2
                });
        const pulley = qCreatedBy(pulleyId + "extrude", EntityType.BODY);
        setAttribute(context, {
                    "entities" : pulley,
                    "name" : PULLEY_ATTRIBUTE,
                    "attribute" : pulleyAttribute(definition, pulleyDefinition, "body")
                });
        pulleys = append(pulleys, pulley);
    }
    cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
    return pulleys;
}

function addMateConnectors(context is Context, id is Id, definition is map, pulleyDefinitions is array, pulleys is array)
{
    for (var i, pulleyDefinition in pulleyDefinitions)
    {
        const mateConnectorId = id + unstableIdComponent(i);
        if (pulleyDefinition.identity != undefined)
        {
            setExternalDisambiguation(context, mateConnectorId, pulleyDefinition.identity);
        }
        const extraWidth = definition.twoBelts ? getTwoBeltPulleyExtraWidth(pulleyDefinition.beltType, pulleyDefinition.beltWidth, definition.unitSystem) : 0 * meter;

        var firstDefinition = pulleyDefinition;
        firstDefinition.plane.origin -= firstDefinition.plane.normal * extraWidth / 2;

        opMateConnector(context, mateConnectorId + "first", {
                    "coordSystem" : firstDefinition.plane->coordSystem(),
                    "owner" : pulleys[i]
                });

        setAttribute(context, {
                    "entities" : qCreatedBy(mateConnectorId + "first", EntityType.BODY),
                    "name" : PULLEY_ATTRIBUTE,
                    "attribute" : pulleyAttribute(definition, firstDefinition, "firstMateConnector")
                });

        if (definition.twoBelts)
        {
            var secondDefinition = pulleyDefinition;
            secondDefinition.plane.origin += secondDefinition.plane.normal * extraWidth / 2;

            opMateConnector(context, mateConnectorId + "second", {
                        "coordSystem" : secondDefinition.plane->coordSystem(),
                        "owner" : pulleys[i]
                    });
            setAttribute(context, {
                        "entities" : qCreatedBy(mateConnectorId + "second", EntityType.BODY),
                        "name" : PULLEY_ATTRIBUTE,
                        "attribute" : pulleyAttribute(definition, secondDefinition, "secondMateConnector")
                    });
        }
    }
}

/**
 * A constructor for a pulley attribute.
 */
function pulleyAttribute(definition is map, pulleyDefinition is PulleyDefinition, coordSystemId is string) returns PulleyAttribute
{
    return {
                "beltType" : pulleyDefinition.beltType,
                "pulleyTeeth" : pulleyDefinition.teeth,
                "twoBelts" : definition.twoBelts,
                "coordSystem" : persistentCoordSystem(pulleyDefinition.plane->coordSystem(), coordSystemId, true)
            } as PulleyAttribute;
}

function addFlanges(context is Context, id is Id, definition is map, pulleyDefinitions is array, pulleys is array)
{
    for (var i, pulleyDefinition in pulleyDefinitions)
    {
        const beltType = pulleyDefinition.beltType;
        const pulleyPlane = pulleyDefinition.plane;
        const pulleyTeethWidth = getPulleyTeethWidth(definition, beltType, pulleyDefinition.beltWidth);

        const flangeRadiusOffset = getFlangeRadiusOffset(beltType, definition.flangeSize, definition.unitSystem);
        const flangeRadius = getFlangeRadius(definition, beltType, pulleyDefinition.teeth);
        const radius = flangeRadius - flangeRadiusOffset;

        const flangeWidth = getFlangeWidth(beltType, definition.flangeSize, definition.unitSystem);

        const flangeId = id + unstableIdComponent(i);
        if (pulleyDefinition.identity != undefined)
        {
            setExternalDisambiguation(context, flangeId, pulleyDefinition.identity);
        }

        // Create a plane perpendicular to the pulley
        // origin is the center of the pulley face, x is away from the pulley
        const flangePlane = plane(pulleyPlane.origin + pulleyPlane.normal * pulleyTeethWidth / 2, pulleyPlane.x, pulleyPlane.normal);
        const sketchId = flangeId + "sketch";
        const sketch = newSketchOnPlane(context, sketchId, { "sketchPlane" : flangePlane });

        skLineSegment(sketch, "inside", {
                    "start" : zeroVector(2) * meter,
                    "end" : vector(0 * meter, radius)
                });

        skLineSegment(sketch, "outside", {
                    "start" : vector(flangeWidth, 0 * meter),
                    "end" : vector(flangeWidth, flangeRadius)
                });

        skLineSegment(sketch, "bottom", {
                    "start" : zeroVector(2) * meter,
                    "end" : vector(flangeWidth, 0 * meter)
                });

        const topPoint = vector(flangeRadiusOffset, flangeRadius);
        skLineSegment(sketch, "top", {
                    "start" : topPoint,
                    "end" : vector(flangeWidth, flangeRadius)
                });
        skLineSegment(sketch, "chamfer", {
                    "start" : vector(0 * meter, radius),
                    "end" : topPoint
                });

        skSolve(sketch);

        opRevolve(context, flangeId + "revolve", {
                    "entities" : qCreatedBy(sketchId, EntityType.FACE),
                    "axis" : line(pulleyPlane.origin, pulleyPlane.normal),
                    "angleForward" : 0 * radian
                });

        opPattern(context, flangeId + "mirror", {
                    "entities" : qCreatedBy(flangeId + "revolve", EntityType.BODY),
                    "transforms" : [mirrorAcross(pulleyPlane)],
                    "instanceNames" : ["flangeCopy"]
                });

        opBoolean(context, flangeId + "boolean", {
                    "tools" : qUnion(pulleys[i], qCreatedBy(flangeId, EntityType.BODY)->qBodyType(BodyType.SOLID)),
                    "operationType" : BooleanOperationType.UNION
                });
    }

    cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
}


function setPulleyProperties(context is Context, pulleyDefinition is PulleyDefinition, pulley is Query)
{
    setProperty(context, {
                "entities" : pulley,
                "propertyType" : PropertyType.NAME,
                // 24T RT25 Pulley
                "value" : pulleyDefinition.teeth ~ "T " ~ getBeltTypeName(pulleyDefinition.beltType) ~ " Pulley"
            });

    setProperty(context, {
                "entities" : pulley,
                "propertyType" : PropertyType.MATERIAL,
                "value" : PLASTIC
            });

    setProperty(context, {
                "entities" : pulley,
                "propertyType" : PropertyType.APPEARANCE,
                "value" : PRINTED_GREEN
            });
}

function addBores(context is Context, id is Id, definition is map, pulleyDefinitions is array, pulleys is array)
{
    // We first generate the bores, then compute the manipulators before cutting the bores
    // This guarantees that we'll be able to find a suitable bore face to attach the manipulator to
    const boreResult = createBores(context, id + "bore", definition, pulleyDefinitions, pulleys);
    const bores = boreResult.bores;

    const insideEdges = startTracking(context, qNonCapEntity(id + "bore", EntityType.EDGE));

    try
    {
        opBoolean(context, id + "cutBore", {
                    "tools" : qUnion(bores),
                    "targets" : qUnion(pulleys),
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
    catch
    {
        addBoreDebugEntities(context, id, definition, pulleyDefinitions);
        throw regenError("Failed to add bore. Check input.", ["hexWidth", "boreDiameter", "fit", "fitClearance"]);
    }

    if (size(evaluateQuery(context, qUnion(pulleys))) != size(pulleys))
    {
        addBoreDebugEntities(context, id, definition, pulleyDefinitions);
        throw regenError("Failed to add bore. Check input.", ["hexWidth", "boreDiameter", "fit", "fitClearance"]);
    }

    if (definition.entranceChamfer)
    {
        const boreEdges = qCreatedBy(id + "cutBore", EntityType.EDGE)->qSubtraction(insideEdges);

        try
        {
            opChamfer(context, id + "entranceChamfer", {
                        "entities" : boreEdges,
                        "chamferType" : ChamferType.EQUAL_OFFSETS,
                        "width" : definition.chamferDistance
                    });
        }
        catch
        {
            addBoreDebugEntities(context, id, definition, pulleyDefinitions);
            throw regenError("Failed to chamfer bore. Check input.", ["hexWidth", "boreDiameter", "fit", "fitClearance", "chamferDistance"]);
        }
    }
}

function createBores(context is Context, id is Id, definition is map, pulleyDefinitions is array, pulleys is array) returns map
{
    const extrudeDistance = evBox3d(context, {
                    "topology" : qEverything(EntityType.BODY),
                    "tight" : false
                })->box3dDiagonalLength();

    var bores = [];
    for (var i, pulleyDefinition in pulleyDefinitions)
    {
        const boreId = id + unstableIdComponent(i);
        if (pulleyDefinition.identity != undefined)
        {
            setExternalDisambiguation(context, boreId, pulleyDefinition.identity);
        }
        const bore = createBore(context, boreId, definition, pulleyDefinition.plane, extrudeDistance);
        bores = append(bores, bore);
    }
    cleanup(context, id + "delete", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));

    return { "bores" : bores };
}

function createBore(context is Context, id is Id, definition is map, plane is Plane, extrudeDistance is ValueWithUnits) returns Query
{
    sketchBoreProfile(context, id + "profile", definition, plane);
    opExtrude(context, id + "extrude", {
                "entities" : qCreatedBy(id + "profile", EntityType.FACE),
                "direction" : plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : extrudeDistance / 2,
                "startBound" : BoundingType.BLIND,
                "startDepth" : extrudeDistance / 2
            });
    if (definition.boreType == BoreType.SPLINE)
    {
        // A spline's sketched at its nominal size, so its fit is added here (half on each side)
        const clearance = fitClearance(definition, splineDiameter(definition.splineType));
        if (!tolerantEqualsZero(clearance))
        {
            opOffsetFace(context, id + "offsetFace", {
                        "moveFaces" : qNonCapEntity(id + "extrude", EntityType.FACE),
                        "offsetDistance" : clearance / 2
                    });
        }
    }
    return qCreatedBy(id + "extrude", EntityType.BODY);
}

function sketchBoreProfile(context is Context, id is Id, definition is map, plane is Plane)
{
    const sketch = newSketchOnPlane(context, id + "profile", { "sketchPlane" : plane });

    if (definition.boreType == BoreType.HEX)
    {
        const width = definition.hexWidth + fitClearance(definition, definition.hexWidth);
        const hexRadius = (width / 2) / cos(30 * degree);
        skRegularPolygon(sketch, "hex", {
                    "center" : zeroVector(2) * meter,
                    "firstVertex" : vector(hexRadius, 0 * meter),
                    "sides" : 6
                });
    }
    else if (definition.boreType == BoreType.HOLE)
    {
        skCircle(sketch, "circle", {
                    "center" : zeroVector(2) * meter,
                    "radius" : (definition.boreDiameter + fitClearance(definition, definition.boreDiameter)) / 2
                });
    }
    else if (definition.boreType == BoreType.SPLINE)
    {
        skSplineProfile(sketch, "spline", {
                    "splineType" : definition.splineType,
                    "location" : zeroVector(2) * meter
                });
    }

    skSolve(sketch);
}

function addBoreDebugEntities(context is Context, id is Id, definition is map, pulleyDefinitions is array)
{
    try
    {
        const errorId = id + "temp";
        const pulleys = createPulleys(context, errorId + "pulley", definition, pulleyDefinitions);
        if (definition.addFlanges)
        {
            addFlanges(context, errorId + "flange", definition, pulleyDefinitions, pulleys);
        }
        addDebugEntities(context, qUnion(pulleys), DebugColor.BLUE);

        const boreResult = createBores(context, errorId + "bore", definition, pulleyDefinitions, pulleys);
        addDebugEntities(context, qUnion(boreResult.bores));
    }
}

function addText(context is Context, id is Id, definition is map, pulleyDefinitions is array, pulleys is array)
{
    const textId = id + "text";
    createAllText(context, textId, definition, pulleyDefinitions);

    const textFaces = startTracking(context, qCreatedBy(textId, EntityType.FACE));
    try
    {
        opBoolean(context, id + "cutText", {
                    "tools" : qCreatedBy(textId, EntityType.BODY)->qBodyType(BodyType.SOLID),
                    "targets" : qUnion(pulleys),
                    "operationType" : BooleanOperationType.SUBTRACTION,
                    "targetsAndToolsNeedGrouping" : true
                });
    }
    catch
    {
        throwTextError(context, id + "error", definition, pulleyDefinitions);
    }

    if (isQueryEmpty(context, textFaces))
    {
        throwTextError(context, id + "error", definition, pulleyDefinitions);
    }
}

function createAllText(context is Context, id is Id, definition is map, pulleyDefinitions is array)
{
    for (var i, pulleyDefinition in pulleyDefinitions)
    {
        const textId = id + unstableIdComponent(i);
        if (pulleyDefinition.identity != undefined)
        {
            setExternalDisambiguation(context, textId, pulleyDefinition.identity);
        }
        createText(context, textId, definition, pulleyDefinition);
    }
    cleanup(context, id + "delete", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
}

function createText(context is Context, id is Id, definition is map, pulleyDefinition is PulleyDefinition)
{
    var text = pulleyDefinition.teeth ~ "T";

    const plane = pulleyDefinition.plane;

    const pulleyWidth = getPulleyWidth(definition, pulleyDefinition.beltType, pulleyDefinition.beltWidth);
    var textPlane = plane;
    textPlane.origin += textPlane.normal * pulleyWidth / 2;

    const flangeRadius = getFlangeRadius(definition, pulleyDefinition.beltType, pulleyDefinition.teeth);
    const textPosition = flangeRadius * definition.textPosition;

    textPlane.origin += textPlane->yAxis() * textPosition;
    const sketchRegions = opText(context, id + "text", {
                "text" : text,
                "bold" : definition.boldText,
                "height" : getTextHeight(definition),
                "plane" : textPlane
            });

    opExtrude(context, id + "extrudeText", {
                "entities" : sketchRegions,
                "direction" : -textPlane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : getTextDepth(definition)
            });

    if (definition.engraveBothSides)
    {
        var oppositeTextPlane = plane;
        oppositeTextPlane.origin -= oppositeTextPlane.normal * pulleyWidth / 2;
        oppositeTextPlane.origin += oppositeTextPlane->yAxis() * textPosition;

        const sketchRegions = opText(context, id + "oppositeText", {
                    "text" : text,
                    "height" : getTextHeight(definition),
                    "plane" : oppositeTextPlane,
                    "mirrorHorizontal" : true
                });

        opExtrude(context, id + "extrudeOppositeText", {
                    "entities" : sketchRegions,
                    "direction" : textPlane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : getTextDepth(definition)
                });
    }

    opPattern(context, id + "copyText", {
                "entities" : qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID),
                "transforms" : [rotationAround(line(plane.origin, plane.normal), 180 * degree)],
                "instanceNames" : ["copy"]
            });
}

function throwTextError(context is Context, id is Id, definition is map, pulleyDefinitions is array)
{
    try
    {
        const pulleys = createPulleys(context, id + "pulley", definition, pulleyDefinitions);
        addFlanges(context, id + "flange", definition, pulleyDefinitions, pulleys);

        if (definition.addBore)
        {
            addBores(context, id, definition, pulleyDefinitions, pulleys);
        }
        addDebugEntities(context, qUnion(pulleys), DebugColor.BLUE);

        createAllText(context, id + "text", definition, pulleyDefinitions);
        addDebugEntities(context, qCreatedBy(id + "text", EntityType.BODY));
    }
    throw regenError("Failed to engrave text. Check input.");
}


const TEXT_POSITION_MANIPULATOR = "textPositionManipulator";

function addTextPositionManipulator(context is Context, id is Id, definition is map, pulleyDefinition is PulleyDefinition)
{
    const pulleyWidth = getPulleyWidth(definition, pulleyDefinition.beltType, pulleyDefinition.beltWidth);
    var textPlane = pulleyDefinition.plane;
    textPlane.origin += textPlane.normal * pulleyWidth / 2;

    const flangeRadius = getFlangeRadius(definition, pulleyDefinition.beltType, pulleyDefinition.teeth);

    addManipulators(context, id, {
                (TEXT_POSITION_MANIPULATOR) : linearManipulator({
                        "base" : textPlane.origin,
                        "direction" : textPlane->yAxis(),
                        "offset" : flangeRadius * definition.textPosition,
                        "minValue" : 0 * meter,
                        "maxValue" : flangeRadius,
                        "style" : ManipulatorStyleEnum.TANGENTIAL,
                        "primaryParameterId" : "textPosition"
                    })
            });
}

function textPositionManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    const manipulator = newManipulators[TEXT_POSITION_MANIPULATOR];
    if (manipulator == undefined)
    {
        return definition;
    }

    var pulleyTeeth;
    var beltType;
    if (definition.creationMethod == CreationMethod.MANUAL)
    {
        beltType = getBeltTypeValue(definition).beltType;
        pulleyTeeth = definition.pulleyTeeth;
    }
    else
    {
        const attribute = getBeltPulleyFaceAttribute(context, definition.beltSelections->qNthElement(0));
        if (attribute == undefined || attribute.pulleyType == PulleyType.IDLER)
        {
            return definition;
        }
        beltType = attribute.beltType;
        pulleyTeeth = attribute.pulleyTeeth;
    }

    definition.textPosition = roundToPrecision(manipulator.offset / getFlangeRadius(definition, beltType, pulleyTeeth), 2);
    return definition;
}

export function robotPulleyManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    definition = profileOffsetManipulatorChange(definition, newManipulators[PROFILE_OFFSET_MANIPULATOR], PROFILE_OFFSET_FLIP);
    definition = startOffsetManipulatorChange(definition, newManipulators);
    definition = pointManipulatorChange(definition, newManipulators);
    definition = textPositionManipulatorChange(context, definition, newManipulators);
    return definition;
}
