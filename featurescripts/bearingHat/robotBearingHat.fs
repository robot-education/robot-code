FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "8b8c46128a5dbc2594925f4a", version : "2073caea5ae472033c5090d9");
import(path : "01402b7c9eebd8bf0b5d3e52", version : "76161d325e4bc689a054d495");
import(path : "452d43a015d17145ad7775e4", version : "e68e283095fa8403f8fa0213");
import(path : "6c65805103086c85362ee4b7", version : "06268198ef2566cb246b9f56");
import(path : "0794d10863d10d98a88c2ab4", version : "90bbee184f6552271649afea");
export import(path : "58d66340f7b70cfc86606676", version : "45bbe2db54d801bb072f1cc1");

export import(path : "cd2c6499801ec51a7947274e", version : "15e8c2a1378004d7660f8dec");


export enum HatType
{
    annotation { "Name" : "COTS" }
    COTS,
    annotation { "Name" : "Standard hat" }
    STANDARD,
    annotation { "Name" : "Custom hat" }
    CUSTOM
}

predicate isCotsHat(definition is map)
{
    definition.hatType == HatType.COTS;
}

predicate isCustomHat(definition is map)
{
    definition.hatType == HatType.CUSTOM;
}

predicate useSingleHoleTable(definition is map)
{
    definition.hatType == HatType.COTS;
}

predicate useSingleClearanceHoleTable(definition is map)
{
    definition.hatType != HatType.COTS && !definition.cutMountingHoles;
}


annotation { "Feature Type Name" : "Robot bearing hat",
        "Feature Type Description" : "Cut holes for mounting bearing hats." ~ CREDIT,
        "Manipulator Change Function" : "robotBearingHatManipulatorChange",
        "Editing Logic Function" : "robotBearingHatEditLogic",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotBearingHat = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Has clearance", "UIHint" : "ALWAYS_HIDDEN" }
        definition.hasClearance is boolean;

        annotation { "Name" : "HatType", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "HORIZONTAL_ENUM"] }
        definition.hatType is HatType;

        annotation { "Group Name" : "Selections", "Collapsed By Default" : false }
        {
            annotation {
                        "Name" : "Bearing edge to place hat",
                        "Filter" : GeometryType.CIRCLE && SketchObject.NO && EdgeTopology.TWO_SIDED && ConstructionObject.NO && ModifiableEntityOnly.YES && AllowMeshGeometry.NO && ActiveSheetMetal.NO,
                        "MaxNumberOfPicks" : 1
                    }
            definition.bearingEdge is Query;

            axisOrientationPredicate(definition);

            if (!isCustomHat(definition))
            {
                angleReferencePredicate(definition);
                angleOffsetPredicate(definition);
            }
        }

        annotation { "Group Name" : "Bearing hat", "Collapsed By Default" : false }
        {
            if (isCustomHat(definition))
            {
                annotation {
                            "Name" : "First mounting location",
                            "Filter" : ((EntityType.VERTEX || GeometryType.CIRCLE) && ModifiableEntityOnly.YES) || BodyType.MATE_CONNECTOR,
                            "MaxNumberOfPicks" : 1
                        }
                definition.firstMountingLocation is Query;

                annotation {
                            "Name" : "Second mounting location",
                            "Filter" : ((EntityType.VERTEX || GeometryType.CIRCLE) && ModifiableEntityOnly.YES) || BodyType.MATE_CONNECTOR,
                            "MaxNumberOfPicks" : 1
                        }
                definition.secondMountingLocation is Query;

                annotation { "Name" : "Connect mounting locations" }
                definition.connectMountingLocations is boolean;
            }

            if (!isCotsHat(definition))
            {
                annotation { "Name" : "Cut mounting holes", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.cutMountingHoles is boolean;
            }

            if (isCustomHat(definition))
            {
                annotation { "Name" : "Bore table", "Lookup Table" : boreTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.borePath is LookupTablePath;
            }
            else
            {
                annotation { "Name" : "Bearing hat table", "Lookup Table" : bearingHatTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.bearingHatPath is LookupTablePath;
            }

            if (useSingleHoleTable(definition))
            {
                annotation { "Name" : "Hole table", "Lookup Table" : singleHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.singleHolePath is LookupTablePath;
            }
            else if (useSingleClearanceHoleTable(definition))
            {
                annotation { "Name" : "Hole table", "Lookup Table" : singleClearanceHoleTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.singleClearanceHolePath is LookupTablePath;
            }
            else
            {
                annotation { "Name" : "Hole table", "Lookup Table" : holeTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.holePath is LookupTablePath;
            }

            holeDiameterPredicate(definition);

            if (definition.hasClearance)
            {
                annotation { "Name" : "Tap drill diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_EXPRESSION"] }
                isLength(definition.tapDrillDiameter, HOLE_DIAMETER_BOUNDS);
            }
        }
    }
    {
        const result = verifyBearingEdge(context, definition);
        var circlePlane = result.circlePlane;
        const scope = result.scope;

        verifyHoleDiameter(context, id, definition);

        if (!isCustomHat(definition))
        {
            circlePlane = applyAngleReference(context, definition, circlePlane, "bearingEdge");
        }
        circlePlane = applyAxisOrientation(definition, circlePlane);

        var mountingLocations;
        var boreValues;
        if (isCustomHat(definition))
        {
            const table = getLookupTable(boreTable, definition.borePath);
            boreValues = getBoreValues(table.boreDiameter);
            mountingLocations = getCustomMountingLocations(context, definition, circlePlane);
        }
        else
        {
            const table = getLookupTable(bearingHatTable, definition.bearingHatPath);
            boreValues = getBoreValues(table.boreDiameter);
            const width = table.width;
            mountingLocations = [vector(-width / 2, 0 * meter), vector(width / 2, 0 * meter)];

            addAngleOffsetManipulator(context, id, definition, circlePlane, width);
            circlePlane = applyAngleOffset(definition, circlePlane);
        }

        if (!isCotsHat(definition))
        {
            createBearingHat(context, id + "bearingHat", definition, circlePlane, boreValues, mountingLocations);
        }
        else
        {
            opMateConnector(context, id + "mateConnector", {
                        "coordSystem" : coordSystem(circlePlane),
                        "owner" : scope
                    });
        }

        if (isCotsHat(definition) || definition.cutMountingHoles)
        {
            cutMountingHoles(context, id, definition, circlePlane, mountingLocations, scope);
        }

        cleanup(context, id + "delete", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
    });

const BEARING_HAT_ATTRIBUTE = "robotBearingHatAttribute";

function verifyBearingEdge(context is Context, definition is map) returns map
{
    verifyNonemptyQuery(context, definition, "bearingEdge", "Select a bearing edge to use.");

    const scope = qOwnerBody(definition.bearingEdge)->qBodyType(BodyType.SOLID);
    if (isQueryEmpty(context, scope))
    {
        throw regenError("The bearing edge must belong to a solid part.", ["bearingEdge"], definition.bearingEdge);
    }

    const holeFace = qAdjacent(definition.bearingEdge, AdjacencyType.EDGE, EntityType.FACE)->qGeometry(GeometryType.CYLINDER);
    if (evaluateQueryCount(context, holeFace) != 1)
    {
        throw regenError("The bearing edge must be adjacent to a single hole.", ["bearingEdge"], definition.bearingEdge);
    }

    const circlePlane = getEdgeDirectionInfo(context, holeFace, definition.bearingEdge)->plane();
    return {
            "circlePlane" : circlePlane,
            "scope" : scope
        };
}

/**
 * Verifies the hole diameter specified in the lookup table matches the parameter.
 * Note we don't need to do this for tapped holes since the std hole feature will always check that for us.
 */
function verifyHoleDiameter(context is Context, id is Id, definition is map)
{
    const lookupHoleDiameter = getHolePathAndValue(definition).value.holeDiameter->lookupTableEvaluate();
    if (!tolerantEquals(definition.holeDiameter, lookupHoleDiameter))
    {
        reportFeatureInfo(context, id, ErrorStringEnum.HOLE_PARAMS_OVERRIDDEN_INFO);
    }
}

function getBoreValues(boreDiameter is ValueWithUnits) returns map
{
    return {
            "boreDiameter" : boreDiameter,
            "outerDiameter" : boreDiameter + 0.25 * inch,
            "cBoreDiameter" : boreDiameter + 0.125 * inch,
        };
}

function getCustomMountingLocations(context is Context, definition is map, circlePlane is Plane) returns array
{
    verifyNonemptyQuery(context, definition, "firstMountingLocation", "Select a location for the first mounting hole.");
    verifyNonemptyQuery(context, definition, "secondMountingLocation", "Select a location for the second mounting hole.");

    return mapArray([definition.firstMountingLocation, definition.secondMountingLocation], function(location is Query)
        {
            if (!isQueryEmpty(context, location->qGeometry(GeometryType.CIRCLE)))
            {
                const circle = evCurveDefinition(context, { "edge" : location });
                return worldToPlane(circlePlane, circle.coordSystem.origin);
            }
            return worldToPlane(circlePlane, evVertexPoint(context, { "vertex" : location }));
        });
}

function sketchMountingHoleFaces(context is Context, id is Id, circlePlane is Plane, mountingLocations is array, holeDiameter is ValueWithUnits) returns Query
{
    const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : circlePlane });

    skCircle(sketch, "firsMountingHole", {
                "center" : mountingLocations[0],
                "radius" : holeDiameter / 2
            });

    skCircle(sketch, "secondMountingHole", {
                "center" : mountingLocations[1],
                "radius" : holeDiameter / 2
            });

    skSolve(sketch);
    return qSketchRegion(id);
}


const BEARING_HAT_DEPTH = 0.125 * inch;

function createBearingHat(context is Context, id is Id, definition is map, circlePlane is Plane, boreValues is map, mountingLocations is array)
{
    const bearingHat = createBearingHatBase(context, id + "bearingHat", definition, circlePlane, boreValues, mountingLocations);

    const mountingHoleFaces = sketchMountingHoleFaces(context, id + "mountingHoleFaces", circlePlane, mountingLocations, definition.holeDiameter);
    opExtrude(context, id + "extrude", {
                "entities" : mountingHoleFaces,
                "direction" : circlePlane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : BEARING_HAT_DEPTH
            });

    opBoolean(context, id + "cutHoles", {
                "tools" : qCreatedBy(id + "extrude", EntityType.BODY),
                "targets" : bearingHat,
                "operationType" : BooleanOperationType.SUBTRACTION
            });

    const holeDefinition = holeDefinition([
                holeProfile(HolePositionReference.AXIS_POINT, 0 * inch, boreValues.cBoreDiameter / 2),
                holeProfile(HolePositionReference.AXIS_POINT, 0.0625 * inch, boreValues.cBoreDiameter / 2),
                holeProfile(HolePositionReference.AXIS_POINT, 0.0625 * inch, boreValues.boreDiameter / 2),
                holeProfile(HolePositionReference.AXIS_POINT, 0.125 * inch, boreValues.boreDiameter / 2),
                // hole definitions must have a tip
                holeProfile(HolePositionReference.AXIS_POINT, 3 / 16 * inch, 0 * meter),
            ]);
    opHole(context, id + "bearingHole", {
                "holeDefinition" : holeDefinition,
                "axes" : [line(circlePlane.origin, circlePlane.normal)],
                "targets" : bearingHat
            });

    if (isCustomHat(definition))
    {
        filletConcaveEdges(context, id + "fillet", bearingHat, circlePlane.normal);
    }

    setBearingHatProperties(context, bearingHat);
}


function createBearingHatBase(context is Context, id is Id, definition is map, circlePlane is Plane, boreValues is map, mountingLocations is array) returns Query
{
    const centerCircle = {
                "location" : zeroVector(2) * meter,
                "radius" : boreValues.outerDiameter / 2,
                "flipped" : false
            } as BoundaryCircle;

    var mountingCircles = [];
    for (var i, mountingLocation in mountingLocations)
    {
        const circle = {
                    "location" : mountingLocation,
                    "radius" : 0.25 * inch,
                    "flipped" : false
                } as BoundaryCircle;

        mountingCircles = append(mountingCircles, circle);
        if (isCustomHat(definition) && definition.connectMountingLocations && i == 0)
        {
            // Skip adding circle on first iteration:
            // mounting location -> mounting location -> center
            continue;
        }
        mountingCircles = append(mountingCircles, centerCircle);
    }

    const locations = extractFromArrayOfMaps(mountingCircles, "location");
    const counterClockwise = isCounterClockwise(locations);

    var connectingPoints = [];
    for (var i, curr in mountingCircles)
    {
        const prev = getPrevious(mountingCircles, i);
        connectingPoints = append(connectingPoints, circleToCircle(prev, curr, counterClockwise));
    }

    sketchConnectingLines(context, id + "connectingLines", mountingCircles, circlePlane, connectingPoints);
    sketchConnectingArcs(context, id + "connectingArcs", mountingCircles, circlePlane, connectingPoints, counterClockwise);

    const face = extractFaces(context, id + "faces", qCreatedBy(id, EntityType.EDGE), circlePlane);
    opExtrude(context, id + "bearingHatExtrude", {
                "entities" : face,
                "direction" : circlePlane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : BEARING_HAT_DEPTH
            });

    const bearingHat = qCreatedBy(id + "bearingHatExtrude", EntityType.BODY);
    cleanup(context, id + "delete", qCreatedBy(id, EntityType.BODY)->qSubtraction(bearingHat));
    return bearingHat;
}

function filletConcaveEdges(context is Context, id is Id, body is Query, direction is Vector)
{
    const edges = qOwnedByBody(body, EntityType.EDGE)->qParallelEdges(direction);
    var concaveEdges = [];
    for (var edge in evaluateQuery(context, edges))
    {
        const convexity = evEdgeConvexity(context, { "edge" : edge });
        if (convexity == EdgeConvexityType.CONCAVE)
        {
            concaveEdges = append(concaveEdges, edge);
        }
    }

    if (concaveEdges != [])
    {
        try
        {
            opFillet(context, id + "fillet", {
                        "entities" : qUnion(concaveEdges),
                        "radius" : 0.375 * inch
                    });
        }
    }
}

function setBearingHatProperties(context is Context, bearingHat is Query)
{
    setAttribute(context, {
                "entities" : bearingHat,
                "name" : BEARING_HAT_ATTRIBUTE,
                "attribute" : {}
            });
    const numBearingHats = evaluateQueryCount(context, qHasAttribute(BEARING_HAT_ATTRIBUTE));
    setProperty(context, {
                "entities" : bearingHat,
                "propertyType" : PropertyType.NAME,
                "value" : "Bearing Hat " ~ numBearingHats
            });

    setProperty(context, {
                "entities" : bearingHat,
                "propertyType" : PropertyType.APPEARANCE,
                "value" : BLACK
            });

    setProperty(context, {
                "entities" : bearingHat,
                "propertyType" : PropertyType.MATERIAL,
                "value" : PLASTIC
            });
}

function cutMountingHoles(context is Context, id is Id, definition is map, circlePlane is Plane, mountingLocations is array, scope is Query)
precondition
{
    isTopLevelId(id);
}
{
    const locations = sketchHoleLocations(context, id + "locations", circlePlane, mountingLocations);

    const holeDiameter = definition.hasClearance ? definition.tapDrillDiameter : definition.holeDiameter;
    var path = getHoleTableAndPath(definition).path;

    const isTappedHole = useSingleClearanceHoleTable(definition) ? false : path.holeType == "Tapped";
    if (isTappedHole)
    {
        path.size = "#10";
        path["type"] = "Straight tap";
        path.fit = "None";
    }
    else
    {
        path["type"] = "Clearance";
    }

    const holeDefinition = {
            "isV2" : true,
            "unitsSystem" : UnitsSystem.INCH,
            "endStyleV2" : HoleEndStyleV2.THROUGH,
            "locations" : locations,
            "isTappedThrough" : true,
            // Not strictly required
            //"ansiHoleTableEx" : lookupTablePath({ "fit" : "None", "holeType" : "Tapped", "pitch" : "24 tpi (UNC)", "size" : "#10", "type" : "Straight tap" }),
            "ansiHoleTableEx" : path,
            "holeDiameterV2" : holeDiameter,
            // tapDrillDiameter used when tapping via a single hole
            "majorDiameter" : 0.190 * inch, // Appears on the attribute, feature crashes without it
            "holeDiameterV2Precision" : PrecisionType.DEFAULT,
            "holeDiameterV2ToleranceType" : ToleranceTypeExtended.NONE,
            "scope" : scope
        };

    callSubfeatureAndProcessStatus(id, hole, context, id + "cutHoles", holeDefinition, {
                "propagateErrorDisplay" : true,
                "featureParameterMap" : { "scope" : "bearingEdge" }
            });
}

export function robotBearingHatManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return angleOffsetManipulatorChange(definition, newManipulators);
}

export function robotBearingHatEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    isCreating is boolean, specifiedParameters is map) returns map
{
    if (getActivePath(oldDefinition) != getActivePath(definition))
    {
        const pathAndValue = getHolePathAndValue(definition);
        if (useSingleHoleTable(definition))
        {
            definition.hasClearance = false;
        }
        else
        {
            definition.hasClearance = pathAndValue.path.holeType == "Tapped";
        }
        definition.holeDiameter = lookupTableEvaluate(pathAndValue.value.holeDiameter);
        if (pathAndValue.value.tapDrillDiameter != undefined)
        {
            definition.tapDrillDiameter = lookupTableEvaluate(pathAndValue.value.tapDrillDiameter);
        }
    }
    return definition;
}

function getHoleTableAndPath(definition is map) returns map
{
    var table;
    var path;
    if (useSingleHoleTable(definition))
    {
        path = definition.singleHolePath;
        table = singleHoleTable;
    }
    else if (useSingleClearanceHoleTable(definition))
    {
        path = definition.singleClearanceHolePath;
        table = singleClearanceHoleTable;
    }
    else
    {
        path = definition.holePath;
        table = holeTable;
    }
    return { "table" : table, "path" : path };
}

function getHolePathAndValue(definition is map) returns map
{
    var table;
    var path;
    if (useSingleHoleTable(definition))
    {
        path = definition.singleHolePath;
        table = singleHoleTable;
    }
    else if (useSingleClearanceHoleTable(definition))
    {
        path = definition.singleClearanceHolePath;
        table = singleClearanceHoleTable;
    }
    else
    {
        path = definition.holePath;
        table = holeTable;
    }

    return {
            "path" : path,
            "value" : getLookupTable(table, path)
        };
}

function getActivePath(definition is map)
{
    if (definition != {})
    {
        return getHoleTableAndPath(definition).path;
    }
    return undefined;
}

/**
 * Find an selected edge's coordinate system and whether the direction to cut needs to be inverted.
 * Borrowed from Onshape's externalThread feature.
 */
function getEdgeDirectionInfo(context is Context, holeFace is Query, endEdge is Query) returns CoordSystem
{
    var coordSys = evCurveDefinition(context, { "edge" : endEdge, "returnBSplinesAsOther" : true }).coordSystem;
    const cylinderBox = calculateCylinderBoundingBox(context, holeFace, coordSys);
    const needsFlip = (cylinderBox.maxCorner[2] > TOLERANCE.zeroLength * meter);
    if (needsFlip)
    {
        coordSys.zAxis *= -1;
    }
    return coordSys;
    // const length = abs(cylinderBox.maxCorner[2]) > abs(cylinderBox.minCorner[2]) ? abs(cylinderBox.maxCorner[2]) : abs(cylinderBox.minCorner[2]);
    // return { "needsFlip" : needsFlip, "edgeCoordSys" : coordSys };
}

/**
 * Calculate cylinder bounding box.
 * Borrowed from Onshape's externalThread feature.
 */
function calculateCylinderBoundingBox(context is Context, cylinderEntities is Query, coordSys is CoordSystem) returns Box3d
{
    return evBox3d(context, {
                "topology" : cylinderEntities,
                "cSys" : coordSystem(coordSys.origin, perpendicularVector(coordSys.zAxis), coordSys.zAxis),
                "tight" : true
            });
}
