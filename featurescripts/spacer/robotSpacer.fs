FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "2073caea5ae472033c5090d9");

export import(path : "afa4f72a120e4ce160eb6972", version : "4b7461ab215effc4f5e19cfd");
import(path : "6e24956e9977116c79280620", version : "1cdcfd6334c53e51fef6f5f5");
import(path : "6c65805103086c85362ee4b7", version : "06268198ef2566cb246b9f56");
import(path : "0794d10863d10d98a88c2ab4", version : "90bbee184f6552271649afea");
import(path : "ea127c07807644fb48d3a1ae", version : "72fbd92d548c811d10a5d2f3");

annotation {
        "Feature Type Name" : "Robot spacer",
        "Feature Type Description" : "Generate spacers." ~ CREDIT,
        "Manipulator Change Function" : "robotSpacerManipulatorChange",
        "Editing Logic Function" : "robotSpacerEditLogic",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotSpacer = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        robotSpacerPredicate(definition);
    }
    {
        doRobotSpacer(context, id, definition);
    });

export function doRobotSpacer(context is Context, id is Id, definition is map)
{
    const plane = getLocationPlane(context, definition);

    const innerDiameter = getInnerDiameter(definition);
    const outerDiameter = getOuterDiameter(definition, innerDiameter);

    const spacerFaceId = id + "spacerFace";
    const outerFace = sketchOuterFace(context, spacerFaceId + "outerFace", plane, outerDiameter);
    const innerFace = sketchInnerFace(context, spacerFaceId + "innerFace", plane, definition);

    definition = transformDefintionForNewExtrude(definition, outerFace);
    // use top level id as extrude id for manipulators
    const extrudeId = id;
    callSubfeatureAndProcessStatus(id, extrude, context, extrudeId, definition, {
                "featureParameterMap" : { "entities" : "location" }
            });
    const spacerBody = makeRobustQuery(context, qCreatedBy(extrudeId, EntityType.BODY)->qBodyType(BodyType.SOLID));

    // Extrude inner bore seperately for error display
    // const innerFaces = qUnion(spacerProfile.innerFace, spacerProfile.snapOnFace ?? qNothing());
    opExtrude(context, id + "spacerBore", {
                "entities" : innerFace,
                "direction" : plane.normal,
                "endBound" : BoundingType.THROUGH_ALL,
                "startBound" : BoundingType.THROUGH_ALL
            });
    var spacerBore = qCreatedBy(id + "spacerBore", EntityType.BODY);

    if (canHaveProfileOffset(definition) && offsetProfile(definition))
    {
        offsetFaces(context, id, qNonCapEntity(id + "spacerBore", EntityType.FACE), getProfileOffset(definition));

        // A collection of heuristics to find the middle of the extrude
        const firstProfile = qCreatedBy(extrudeId, EntityType.BODY)->qBodyType(BodyType.SOLID)->qNthElement(0);
        const firstProfileEdge = qNonCapEntity(extrudeId, EntityType.EDGE)->qSketchFilter(SketchObject.NO)->qNthElement(0);

        const planeNormal = evEdgeTangentLine(context, {
                        "edge" : firstProfileEdge,
                        "parameter" : 0.5
                    }).direction;

        const boundingBox = evBox3d(context, {
                    "topology" : firstProfile,
                    "tight" : false
                });
        const center = box3dCenter(boundingBox);
        const profileAxis = line(center, perpendicularVector(plane.normal));

        const flipped = definition.profileOffsetOppositeDirection;
        const flipDirection = definition.profileSide == ProfileSide.INSIDE;
        addProfileOffsetManipulator(context, id, PROFILE_OFFSET_MANIPULATOR, profileAxis, qNonCapEntity(extrudeId, EntityType.FACE), flipped, flipDirection);
    }


    if (isSnapOnSpacer(definition))
    {
        const snapOnFace = sketchSnapOnFace(context, id + "snapOnFace", plane, definition.hexSize, outerDiameter);
        opExtrude(context, id + "snapOnBore", {
                    "entities" : snapOnFace,
                    "direction" : plane.normal,
                    "endBound" : BoundingType.THROUGH_ALL,
                    "startBound" : BoundingType.THROUGH_ALL
                });
        spacerBore = qUnion(spacerBore, qCreatedBy(id + "snapOnBore", EntityType.BODY));
        cleanup(context, id + "deleteSnapOnFace", qCreatedBy(id + "snapOnFace", EntityType.BODY));
    }

    if (tolerantGreaterThanOrEqual(innerDiameter, outerDiameter))
    {
        addDebugEntities(context, spacerBody, DebugColor.BLUE);
        throw regenError("The inner diameter must be less than the outer diameter.", ["innerDiameter", "outerDiameter", "offset", "wallThickness"], spacerBore);
    }

    try
    {
        opBoolean(context, id + "innerBoolean", {
                    "tools" : spacerBore,
                    "targets" : spacerBody,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
    catch
    {
        processSubfeatureStatus(context, id, {
                    "subfeatureId" : id + "innerBoolean",
                    "propagateErrorDisplay" : true
                });
        throw regenError("Failed to create spacer.", spacerBody);
    }

    if (isSnapOnSpacer(definition))
    {
        filletSnapOnEdges(context, id, spacerBody->qOwnedByBody(EntityType.EDGE)->qParallelEdges(plane.normal));
    }
    setSpacerProperties(context, definition, spacerBody, extrudeId);
    cleanup(context, id + "cleanup", qCreatedBy(spacerFaceId, EntityType.BODY));
}

function getSnapOnGap(hexSize is HexSize) returns ValueWithUnits
{
    return (hexSize == HexSize._1_2_IN ? 0.375 * inch : 0.25 * inch);
}

function sketchSnapOnFace(context is Context, id is Id, plane is Plane, hexSize is HexSize, outerDiameter is ValueWithUnits) returns Query
{
    const snapOnGap = getSnapOnGap(hexSize);
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    skRectangle(sketch, "snapOnRectangle", {
                "firstCorner" : vector(-outerDiameter / 2, snapOnGap / 2),
                "secondCorner" : vector(0 * meter, -snapOnGap / 2)
            });
    skSolve(sketch);
    return qSketchRegion(id);
}

function sketchInnerFace(context is Context, id is Id, plane is Plane, definition is map) returns Query
{
    const innerSketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    const innerDiameter = getInnerDiameter(definition);
    if (hexSpacer(definition))
    {
        const innerRadius = circumscribedRadius(innerDiameter / 2, 6);
        skRegularPolygon(innerSketch, "hexagon", {
                    "center" : zeroVector(2) * meter,
                    "firstVertex" : vector(innerRadius, 0 * meter),
                    "sides" : 6
                });
    }
    else if (roundSpacer(definition))
    {
        skCircle(innerSketch, "circle", { "radius" : innerDiameter / 2 });
    }
    else if (splineSpacer(definition))
    {
        skSplineProfile(innerSketch, "spline", { "splineType" : definition.splineType });
    }
    skSolve(innerSketch);
    return qSketchRegion(id);
}

function sketchOuterFace(context is Context, id is Id, plane is Plane, outerDiameter is ValueWithUnits) returns Query
{
    const outerSketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    skCircle(outerSketch, "outerCircle", { "radius" : outerDiameter / 2 });
    skSolve(outerSketch);
    return qSketchRegion(id);
}

/**
 * Given the inscribed radius for a polygon, returns the circumscribed radius.
 */
function circumscribedRadius(inscribedRadius is ValueWithUnits, sides is number)
{
    return inscribedRadius / cos(360 * degree / sides / 2);
}

function offsetFaces(context is Context, id is Id, facesToOffset is Query, distance is ValueWithUnits)
precondition
{
    isTopLevelId(id);
}
{
    try
    {
        opOffsetFace(context, id + "offsetFace", {
                    "moveFaces" : facesToOffset,
                    "offsetDistance" : distance
                });
    }
    catch
    {
        throw regenError("Failed to offset spacer faces.", ["offset"]);
    }
}


function filletSnapOnEdges(context is Context, id is Id, edges is Query)
precondition
{
    // for error handling
    isTopLevelId(id);
}
{
    const concaveEdges = filter(evaluateQuery(context, edges), function(edge)
        {
            return evEdgeConvexity(context, { "edge" : edge }) == EdgeConvexityType.CONVEX;
        });
    try
    {
        opFillet(context, id + "fillet", {
                    "entities" : concaveEdges->qUnion(),
                    "radius" : 1 / 32 * inch
                });
    }
    catch
    {
        processSubfeatureStatus(context, id, {
                    "subfeatureId" : id + "fillet",
                    "propagateErrorDisplay" : true,
                });
        throw regenError("Failed to fillet snap on edges.", ["snapOn"]);
    }
}

function setSpacerProperties(context is Context, definition is map, spacer is Query, extrudeId is Id)
{
    var name;
    if (definition.spacerType == SpacerType.SPLINE)
    {
        name = splineName(definition.splineType);
    }
    else
    {
        name = hexSpacer(definition) ? "Hex" : "Round";
    }

    var length;
    try
    {
        const spacerFaces = qCapEntity(extrudeId, CapType.EITHER, EntityType.FACE);
        // This can fail if, e.g., one of the shaft cap faces is removed entirely by an overzealous hole
        // In a perfect world we'd measure maximum distance but measureDistance is weird and buggy
        length = measureDistance(context, { "entities" : spacerFaces }).distance;
    }
    catch
    {
        try
        {
            // Fallback to measuring the edge length directly
            // Not ideal since, e.g., the faces don't have to be parallel, but fine most of the time
            const spacerEdge = qNonCapEntity(extrudeId, EntityType.EDGE)->qLargest();
            length = evLength(context, { "entities" : spacerEdge });
        }
    }

    var computedName = name ~ " Spacer";
    if (length != undefined)
    {
        const valueString = makeValueString(definition.unitSystem, length);
        computedName = valueString ~ ". " ~ name ~ " Spacer";
    }
    setProperty(context, {
                "entities" : spacer,
                "propertyType" : PropertyType.NAME,
                /* 3 in. Hex Spacer */
                "value" : computedName
            });

    setProperty(context, {
                "entities" : spacer,
                "propertyType" : PropertyType.APPEARANCE,
                "value" : BLACK
            });

    setProperty(context, {
                "entities" : spacer,
                "propertyType" : PropertyType.MATERIAL,
                "value" : PLASTIC
            });
}

export function robotSpacerManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    definition = profileOffsetManipulatorChange(definition, newManipulators[PROFILE_OFFSET_MANIPULATOR], PROFILE_OFFSET_FLIP);
    return extrudeManipulatorChange(context, definition, newManipulators);
}

export function robotSpacerEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean, specifiedParameters is map, hiddenBodies is Query) returns map
{
    if (oldDefinition != {})
    {
        definition = wallEditLogic(oldDefinition, definition, specifiedParameters, getInnerDiameter(definition), getInnerDiameter(oldDefinition));
    }

    if (activePathChanged(oldDefinition, definition))
    {
        definition = applyTableDefinition(definition);
    }

    if (!isQueryEmpty(context, definition.location))
    {
        try silent
        {
            const plane = getLocationPlane(context, definition);

            const innerDiameter = getInnerDiameter(definition);
            const outerDiameter = getOuterDiameter(definition, innerDiameter);

            definition.entities = sketchOuterFace(context, id + "outerFace", plane, outerDiameter);
        }
    }
    return stdNewExtrudeEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
}

function activePathChanged(oldDefinition is map, definition is map) returns boolean
{
    if (oldDefinition == {})
    {
        return true;
    }
    // Either change in table or path requires update
    return getTableAndPath(oldDefinition) != getTableAndPath(definition);
}

function applyTableDefinition(definition is map) returns map
{
    const tableAndPath = getTableAndPath(definition);
    definition.holeDiameter = getLookupTable(tableAndPath.table, tableAndPath.path).holeDiameter;
    return definition;
}

