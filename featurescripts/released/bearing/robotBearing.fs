FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
import(path : "452d43a015d17145ad7775e4", version : "5632c018fec45fab4e533392");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "ee89dca8b88ae07af559109e");
export import(path : "core/bearing.fs", version : "");
export import(path : "cd2c6499801ec51a7947274e", version : "6a0eab11cdac6776966381cd");
export import(path : "released/bearing/robotBearingTables.gen.fs", version : "");
// FRCDesign's bearings part set: its bearings' sizes and bores
import(path : "bearings/bearings.fs", version : "");
// The fasteners' enums: the screws a retention declares
export import(path : "fasteners/fastenerTables.gen.fs", version : "");

export enum RetentionType
{
    annotation { "Name" : "Hat" }
    HAT,
    annotation { "Name" : "Screws" }
    SCREWS
}

export enum HatType
{
    annotation { "Name" : "COTS" }
    COTS,
    annotation { "Name" : "Standard hat" }
    STANDARD,
    annotation { "Name" : "Custom hat" }
    CUSTOM
}

/** A standard or COTS hat's mounting holes' centers, as WCP makes them. */
export enum HatCenters
{
    annotation { "Name" : "1.25 in" }
    _1_25_IN,
    annotation { "Name" : "1.5 in" }
    _1_5_IN,
    annotation { "Name" : "2 in" }
    _2_IN
}

export enum ScrewCount
{
    annotation { "Name" : "2" }
    TWO,
    annotation { "Name" : "3" }
    THREE
}

predicate isCustomHat(definition is map)
{
    definition.retention;
    definition.retentionType == RetentionType.HAT;
    definition.hatType == HatType.CUSTOM;
}

predicate useSingleHoleTable(definition is map)
{
    definition.retention;
    definition.retentionType == RetentionType.HAT;
    definition.hatType == HatType.COTS;
}

predicate useSingleClearanceHoleTable(definition is map)
{
    definition.retention;
    definition.retentionType == RetentionType.HAT;
    definition.hatType != HatType.COTS;
    !definition.cutMountingHoles;
}

predicate declaresScrews(definition is map)
{
    definition.retention;
    definition.retentionType == RetentionType.SCREWS || definition.hatType != HatType.COTS;
}

/**
 * The screws a retention declares for Auto Assembly: FRCDesign's fasteners', in the holes' size.
 */
predicate screwPredicate(definition is map)
{
    annotation { "Name" : "Declare screws", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                "Description" : "Declare a screw in each hole, for FRCDesign's Auto Assembly to insert." }
    definition.declareScrews is boolean;

    if (definition.declareScrews)
    {
        annotation { "Name" : "Screw vendor", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.screwVendor is FastenerVendor;

        annotation { "Name" : "Screw head", "Default" : ScrewHead.BUTTON, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.screwHead is ScrewHead;

        annotation { "Name" : "Screw finish", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.screwFinish is ScrewFinish;

        annotation { "Name" : "Automatic length", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                    "Description" : "Let Auto Assembly choose each screw's length from what its hole goes through." }
        definition.automaticScrewLength is boolean;

        if (!definition.automaticScrewLength)
        {
            annotation { "Name" : "Screw length", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.screwLength is ScrewLength;
        }
    }
}

const HOLE_CIRCLE_BOUNDS = { (meter) : [1e-5, 0.0254 * 1.5, 500], (inch) : 1.5, (millimeter) : 38 } as LengthBoundSpec;

annotation { "Feature Type Name" : "Robot bearing",
        "Feature Type Description" : "Cut a bore for a bearing, and retain it with a hat or screws." ~ CREDIT,
        "Manipulator Change Function" : "robotBearingHatManipulatorChange",
        "Editing Logic Function" : "robotBearingHatEditLogic",
        "Auto Assembly" : true,
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotBearingHat = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        // Set by editing logic, as conditions can't read lookup tables
        annotation { "Name" : "Has clearance", "UIHint" : ["ALWAYS_HIDDEN"], "Set By" : "hasTappedHoles" }
        definition.hasClearance is boolean;

        annotation { "Group Name" : "Bearing", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Bearing", "Lookup Table" : robotBearingTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.bearing is LookupTablePath;

            boreFitPredicate(definition);
        }

        annotation { "Group Name" : "Position", "Collapsed By Default" : false }
        {
            bearingPositionPredicate(definition, "bearing");
        }

        annotation { "Name" : "Retention", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.retention is boolean;

        annotation { "Group Name" : "Retention", "Driving Parameter" : "retention", "Collapsed By Default" : false }
        {
            if (definition.retention)
            {
                annotation { "Name" : "Retention type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.retentionType is RetentionType;

                if (definition.retentionType == RetentionType.HAT)
                {
                    annotation { "Name" : "Hat type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    definition.hatType is HatType;

                    if (definition.hatType == HatType.CUSTOM)
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
                    else
                    {
                        annotation { "Name" : "Hat centers", "Default" : HatCenters._2_IN, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        definition.hatCenters is HatCenters;
                    }

                    if (definition.hatType != HatType.COTS)
                    {
                        annotation { "Name" : "Cut mounting holes", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                        definition.cutMountingHoles is boolean;
                    }
                }
                else
                {
                    annotation { "Name" : "Screw count", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    definition.screwCount is ScrewCount;

                    annotation { "Name" : "Override hole circle", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    definition.overrideHoleCircle is boolean;

                    if (definition.overrideHoleCircle)
                    {
                        annotation { "Name" : "Hole circle diameter" }
                        isLength(definition.holeCircleDiameter, HOLE_CIRCLE_BOUNDS);
                    }
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

                if (declaresScrews(definition))
                {
                    screwPredicate(definition);
                }
            }
        }

        annotation { "Group Name" : "Other options", "Collapsed By Default" : true }
        {
            autoAssemblyBodyPredicate(definition);
        }
    }
    {
        const part = libraryBearing(definition);
        const plane = bearingPlane(context, id, definition, part);
        const host = placeBearing(context, id + "place", definition, plane, part, AutoAssemblyKind.BEARING);

        if (definition.retention)
        {
            verifyHoleDiameter(context, id, definition);
            if (definition.retentionType == RetentionType.HAT)
            {
                retainWithHat(context, id, definition, plane, part, host);
            }
            else
            {
                retainWithScrews(context, id, definition, plane, part, host);
            }
        }

        cleanup(context, id + "delete", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
    });

/**
 * The bearing chosen, as FRCDesign's bearings part set makes it: its sizes and flange from the set's `BEARINGS`, and its
 * body's hole at its bore's corners (`boreRadius`).
 */
function libraryBearing(definition is map) returns BearingPart
{
    const entry = getLookupTable(robotBearingTable, definition.bearing);
    if (entry.product == undefined)
    {
        throw regenError("Choose a different bearing.", ["bearing"]);
    }
    const made = BEARINGS[entry.vendor][entry.bearingType][entry.bore][entry.size][entry.dimensions][entry.seal];
    var part = {
        "outerDiameter" : made.outerDiameter,
        "width" : made.width,
        "innerDiameter" : 2 * boreRadius(BEARING_BORES[entry.bore][entry.size]),
        "product" : entry.product
    };
    if (entry.bearingType != BearingType.RADIAL)
    {
        part.flangeDiameter = made.flangeDiameter;
        part.flangeThickness = made.flangeThickness;
    }
    return part as BearingPart;
}

function retainWithHat(context is Context, id is Id, definition is map, plane is Plane, part is BearingPart, host is Query)
{
    const boreValues = getBoreValues(part.outerDiameter);
    var mountingLocations;
    if (definition.hatType == HatType.CUSTOM)
    {
        mountingLocations = getCustomMountingLocations(context, definition, plane);
    }
    else
    {
        const width = hatCenters(definition.hatCenters);
        mountingLocations = [vector(-width / 2, 0 * meter), vector(width / 2, 0 * meter)];
    }

    if (definition.hatType == HatType.COTS)
    {
        declarePart(context, id + "hat", {
                    "owner" : host,
                    "coordSystem" : coordSystem(plane.origin, plane.x, plane.normal),
                    "connector" : "face",
                    "declaration" : autoAssemblyDeclaration(AutoAssemblyKind.BEARING_HAT, wcpHat(part, definition.hatCenters), {
                                "outerDiameter" : part.outerDiameter
                            })
                });
    }
    else
    {
        const hat = createBearingHat(context, id + "bearingHat", definition, plane, boreValues, mountingLocations);
        if (definition.declareScrews)
        {
            // Seated on the hat's top
            var top = plane;
            top.origin += plane.normal * BEARING_HAT_DEPTH;
            declareScrews(context, id + "screws", definition, top, mountingLocations, hat);
        }
    }

    if (definition.hatType == HatType.COTS || definition.cutMountingHoles)
    {
        cutMountingHoles(context, id, definition, plane, mountingLocations, definition.scope);
    }
}

/**
 * WCP's hat for a bearing at a hat's centers.
 */
function wcpHat(part is BearingPart, centers is HatCenters) returns Product
{
    const bore = roundToPrecision(part.outerDiameter / inch, 3) ~ " in";
    const byBore = wcpHatTable.entries[bore];
    const hat = byBore == undefined ? undefined : byBore.entries[hatCentersName(centers)];
    if (hat == undefined)
    {
        throw regenError("WCP makes no hat for this bearing at these centers. Use a standard or custom hat, or screws.", ["hatCenters", "bearing"]);
    }
    return hat.product as Product;
}

function hatCenters(centers is HatCenters) returns ValueWithUnits
{
    return {
                HatCenters._1_25_IN : 1.25 * inch,
                HatCenters._1_5_IN : 1.5 * inch,
                HatCenters._2_IN : 2 * inch
            }[centers];
}

function hatCentersName(centers is HatCenters) returns string
{
    return {
                HatCenters._1_25_IN : "1.25 in",
                HatCenters._1_5_IN : "1.5 in",
                HatCenters._2_IN : "2 in"
            }[centers];
}

/** How far a screw retention's holes clear the bore. */
const SCREW_WALL = 0.03 * inch;

/**
 * Where a screw retention's holes go: evenly around the bore from the plane's x axis, on a circle clearing the bore by
 * a wall, so a screw head over each overlaps the bearing, unless overridden.
 */
export function screwPositions(definition is map, part is BearingPart) returns array
{
    const count = definition.screwCount == ScrewCount.TWO ? 2 : 3;
    const diameter = definition.overrideHoleCircle ? definition.holeCircleDiameter :
        part.outerDiameter + 2 * SCREW_WALL + definition.holeDiameter;
    return mapArray(range(0, count - 1), i => vector(cos(i * 360 * degree / count), sin(i * 360 * degree / count)) * diameter / 2);
}

function retainWithScrews(context is Context, id is Id, definition is map, plane is Plane, part is BearingPart, host is Query)
{
    const positions = screwPositions(definition, part);
    cutMountingHoles(context, id, definition, plane, positions, definition.scope);
    if (definition.declareScrews)
    {
        declareScrews(context, id + "screws", definition, plane, positions, host);
    }
}

/**
 * Declares a screw at each position, seated on `plane` (its z axis out of the hole), on `owner`: FRCDesign's fasteners'
 * #10-32, as the holes are. Without a length, Auto Assembly picks each from what its hole goes through.
 */
function declareScrews(context is Context, id is Id, definition is map, plane is Plane, positions is array, owner is Query)
{
    const path = getHoleTableAndPath(definition).path;
    if (path.holeType == "Tapped" && path.pitch != "32 tpi (UNF)")
    {
        reportFeatureInfo(context, id, "FRCDesign's fasteners have no #10-24 screws, so the screws aren't declared.");
        return;
    }
    var configuration = {
        "vendor" : toString(definition.screwVendor),
        "size" : toString(FastenerSize.NO_10_32),
        "head" : toString(definition.screwHead),
        "finish" : toString(definition.screwFinish)
    };
    if (!definition.automaticScrewLength)
    {
        configuration.length = toString(definition.screwLength);
    }
    const studio = getLookupTable(screwStudioTable, lookupTablePath({ "studio" : "Screw" }));
    const product = {
            "documentId" : studio.documentId,
            "elementId" : studio.elementId,
            "configuration" : configuration,
            "name" : "#10-32 Screw"
        } as Product;
    const declaration = autoAssemblyDeclaration(AutoAssemblyKind.SCREW, product, { "holeType" : path.holeType });
    for (var i, position in positions)
    {
        declarePart(context, id + ("screw" ~ (i + 1)), {
                    "owner" : owner,
                    "coordSystem" : coordSystem(planeToWorld(plane, position), plane.x, plane.normal),
                    "connector" : "mateConnector",
                    "declaration" : declaration
                });
    }
}

const BEARING_HAT_ATTRIBUTE = "robotBearingHatAttribute";

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

function createBearingHat(context is Context, id is Id, definition is map, circlePlane is Plane, boreValues is map, mountingLocations is array) returns Query
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
    return bearingHat;
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
                "value" : PRINTED_GREEN
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
                "featureParameterMap" : { "scope" : "scope" }
            });
}

export function robotBearingHatManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return bearingManipulatorChange(definition, newManipulators);
}

/**
 * Has clearance: whether the hat's holes are tapped (from its hole table), which shows Tap drill diameter.
 */
function hasTappedHoles(definition is map) returns boolean
{
    return !useSingleHoleTable(definition) && getHolePathAndValue(definition).path.holeType == "Tapped";
}

export function robotBearingHatEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    isCreating is boolean, specifiedParameters is map, hiddenBodies is Query) returns map
{
    definition.hasClearance = hasTappedHoles(definition);
    if (getActivePath(oldDefinition) != getActivePath(definition))
    {
        const pathAndValue = getHolePathAndValue(definition);
        definition.holeDiameter = lookupTableEvaluate(pathAndValue.value.holeDiameter);
        if (pathAndValue.value.tapDrillDiameter != undefined)
        {
            definition.tapDrillDiameter = lookupTableEvaluate(pathAndValue.value.tapDrillDiameter);
        }
    }
    return mountingEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
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
