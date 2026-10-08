FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "core/coreUtils.fs", version : "");
import(path : "core/unitSystemDisplay.fs", version : "");
export import(path : "core/unitSystem.fs", version : "");
export import(path : "core/startOffset.fs", version : "");
import(path : "core/loop.fs", version : "");
import(path : "core/robotFeature.fs", version : "");
import(path : "core/robotProperties.fs", version : "");

/**
 * Roller chain sizes. Stored in documents: never rename or remove one.
 */
export enum ChainType
{
    annotation { "Name" : "#25" }
    ANSI_25,
    annotation { "Name" : "#35" }
    ANSI_35
}

export enum SprocketType
{
    annotation { "Name" : "Sprocket" }
    SPROCKET,
    annotation { "Name" : "Idler" }
    IDLER
}

export enum ChainSide
{
    annotation { "Name" : "Inside" }
    INSIDE,
    annotation { "Name" : "Outside" }
    OUTSIDE
}

const LINKS_BOUNDS = { (unitless) : [4, 100, 1e5] } as IntegerBoundSpec;
const SPROCKET_TEETH_BOUNDS = { (unitless) : [5, 16, 500] } as IntegerBoundSpec;
const IDLER_DIAMETER_BOUNDS = { (meter) : [1e-4, 0.0254, 500], (inch) : 1, (millimeter) : 25 } as LengthBoundSpec;

/**
 * A chain's dimensions (ANSI B29.1).
 *
 * @returns {{
 *      @field pitch {ValueWithUnits} : The distance between its pins.
 *      @field rollerDiameter {ValueWithUnits} :
 *      @field plateHeight {ValueWithUnits} : How tall its plates are, across the chain.
 *      @field width {ValueWithUnits} : How wide it is, over its pins.
 * }}
 */
export function getChainInfo(chainType is ChainType) returns map
{
    return switch (chainType) {
                ChainType.ANSI_25 : {
                        "pitch" : 0.25 * inch,
                        "rollerDiameter" : 0.13 * inch,
                        "plateHeight" : 0.237 * inch,
                        "width" : 0.31 * inch
                    },
                ChainType.ANSI_35 : {
                        "pitch" : 0.375 * inch,
                        "rollerDiameter" : 0.2 * inch,
                        "plateHeight" : 0.356 * inch,
                        "width" : 0.47 * inch
                    }
            };
}

export function getChainTypeName(chainType is ChainType) returns string
{
    return switch (chainType) {
                ChainType.ANSI_25 : "#25",
                ChainType.ANSI_35 : "#35"
            };
}

/**
 * The radius of a sprocket's pitch circle, which its chain's pins are on.
 */
export function getSprocketRadius(pitch is ValueWithUnits, teeth is number) returns ValueWithUnits
{
    return pitch / (2 * sin(180 * degree / teeth));
}

annotation {
        "Feature Type Name" : "Robot chain",
        "Feature Type Description" : "Create #25 and #35 roller chain around sprockets and idlers, and check its length." ~
        "<br>See also the Robot tensioner FeatureScript, which finds where to put a sprocket or idler for a chain to fit." ~ CREDIT,
        "Manipulator Change Function" : "robotChainManipulatorChange",
        "Editing Logic Function" : "robotChainEditLogic",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotChain = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        unitSystemPredicate(definition);

        annotation {
                    "Name" : "Sprockets",
                    "Item name" : "Sprocket",
                    "Item label template" : "#chainSide #sprocketType",
                    "UIHint" : [UIHint.FOCUS_INNER_QUERY],
                    "Driving query" : "location"
                }
        definition.sprockets is array;
        for (var sprocket in definition.sprockets)
        {
            annotation {
                        "Name" : "Location",
                        "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR || (EntityType.EDGE && GeometryType.CIRCLE) || (EntityType.FACE && GeometryType.CYLINDER),
                        "MaxNumberOfPicks" : 1,
                        "Description" : "A sketch point, mate connector, circle, or cylinder at its center: like a sprocket's bore, or its teeth's circle."
                    }
            sprocket.location is Query;

            annotation { "Name" : "Type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            sprocket.sprocketType is SprocketType;

            if (sprocket.sprocketType == SprocketType.SPROCKET)
            {
                annotation { "Name" : "Pitch circle", "Description" : "The selected circle is the sprocket's pitch circle (its pins' circle), which gives its teeth." }
                sprocket.pitchCircle is boolean;

                if (!sprocket.pitchCircle)
                {
                    annotation { "Name" : "Teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isInteger(sprocket.teeth, SPROCKET_TEETH_BOUNDS);
                }
            }
            else
            {
                annotation { "Name" : "Idler diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "The diameter the chain's rollers run on." }
                isLength(sprocket.idlerDiameter, IDLER_DIAMETER_BOUNDS);
            }

            annotation { "Name" : "Chain side", "UIHint" : ["SHOW_LABEL"], "Description" : "Which side of the chain it's on: inside its loop, or outside it." }
            sprocket.chainSide is ChainSide;
        }

        startOffsetPredicate(definition);

        annotation { "Group Name" : "Chain", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Chain type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.chainType is ChainType;

            annotation { "Name" : "Links", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isInteger(definition.links, LINKS_BOUNDS);

            annotation { "Name" : "Select closest links", "Description" : "Sets Links to the even number of links nearest the length of the path around the sprockets." }
            isButton(definition.selectClosestLinks);
        }

        annotation { "Group Name" : "Other options", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Add mate connectors", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.addMateConnectors is boolean;

            annotation { "Name" : "Chain fit adjustment", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "How much longer the path around the sprockets should be than the chain: positive for a looser chain." }
            isLength(definition.fitAdjustment, ZERO_DEFAULT_LENGTH_BOUNDS);
        }
    }
    {
        const info = getChainInfo(definition.chainType);
        const sprockets = getSprockets(context, definition, info);
        if (sprockets.fractional != [])
        {
            reportFeatureWarning(context, id, "A selected pitch circle isn't the size of a sprocket with a whole number of teeth: it's used as the nearest one.", sprockets.fractional);
        }
        const chainPlane = sprockets.plane;
        const circles = sprockets.circles;
        addStartOffsetManipulator(context, id, definition, chainPlane);

        const counterClockwise = loopCounterClockwise(circles);
        const sketch = sketchLoop(context, id + "sketch", chainPlane, circles, counterClockwise);
        addSideFlipManipulators(context, id, chainPlane, sketch.arcs, counterClockwise);
        validateLength(context, id, definition, info, loopLength(circles, counterClockwise));

        const chain = createChain(context, id + "chain", info, chainPlane, sketch.path);
        const name = definition.links ~ " link " ~ getChainTypeName(definition.chainType) ~ " chain";
        var loopCircles = [];
        for (var circle in circles)
        {
            circle.identity = undefined;
            loopCircles = append(loopCircles, circle);
        }
        setAttribute(context, {
                    "entities" : chain,
                    "name" : LOOP_ATTRIBUTE,
                    "attribute" : loopAttribute(name, chainPlane, info.pitch * definition.links + definition.fitAdjustment, loopCircles, counterClockwise)
                });
        setChainProperties(context, chain, definition);

        if (definition.addMateConnectors)
        {
            addMateConnectors(context, id + "mateConnector", chainPlane, circles, chain);
        }
        cleanup(context, id + "delete", qCreatedBy(id + "sketch", EntityType.BODY));
    });

/**
 * The chain's plane (the first sprocket's, with the start offset), the circles its path goes around (a sprocket's pitch
 * circle, or an idler, with the chain's rollers on it, with an idler's `idlerRadius`), and the parameters of pitch
 * circles which aren't a whole number of teeth (`fractional`).
 */
function getSprockets(context is Context, definition is map, info is map) returns map
{
    verifyNonemptyArray(context, definition, "sprockets", "Add the sprockets and idlers the chain goes around.");
    if (size(definition.sprockets) < 2)
    {
        throw regenError("Add at least two sprockets.", ["sprockets"]);
    }

    var planes = [];
    var fractional = [];
    var teeth = [];
    for (var i, sprocket in definition.sprockets)
    {
        const parameterName = arrayParameterId("sprockets", i, "location");
        verifyNonemptyArrayQuery(context, definition, parameterName, "Select the sprocket's or idler's center.");
        planes = append(planes, getLocationPlane(context, sprocket.location, parameterName));

        if (sprocket.sprocketType == SprocketType.SPROCKET && sprocket.pitchCircle)
        {
            if (!isCircle(context, sprocket.location))
            {
                throw regenError("Select a circle the size of the sprocket's pitch circle.", [parameterName], sprocket.location);
            }
            const radius = evCurveDefinition(context, { "edge" : sprocket.location }).radius;
            if (radius <= info.pitch / 2)
            {
                throw regenError("The selected pitch circle is too small for the chain.", [parameterName], sprocket.location);
            }
            // Inverts getSprocketRadius
            const pitchTeeth = 180 * degree / asin(info.pitch / (2 * radius));
            if (!tolerantEquals(pitchTeeth, round(pitchTeeth)))
            {
                fractional = append(fractional, parameterName);
            }
            teeth = append(teeth, round(pitchTeeth));
        }
        else
        {
            teeth = append(teeth, sprocket.teeth);
        }
    }

    const chainPlane = applyStartOffset(context, definition, planes[0]);
    var circles = [];
    for (var i, sprocket in definition.sprockets)
    {
        var circle = {
            "identity" : sprocket.location,
            // Projected onto the chain's plane
            "location" : worldToPlane(chainPlane, planes[i].origin),
            "flipped" : sprocket.chainSide == ChainSide.OUTSIDE
        };
        if (sprocket.sprocketType == SprocketType.SPROCKET)
        {
            circle.radius = getSprocketRadius(info.pitch, teeth[i]);
        }
        else
        {
            circle.idlerRadius = sprocket.idlerDiameter / 2;
            circle.radius = circle.idlerRadius + info.rollerDiameter / 2;
        }
        circles = append(circles, circle as BoundaryCircle);
    }
    return { "plane" : chainPlane, "circles" : circles, "fractional" : fractional };
}

/**
 * The plane of a sketch point, mate connector, circle, or cylinder, at its center (a cylinder's, in its middle).
 */
function getLocationPlane(context is Context, selection is Query, parameterName is string) returns Plane
{
    if (isCircle(context, selection))
    {
        return evCurveDefinition(context, { "edge" : selection }).coordSystem->plane();
    }
    if (isVertex(context, selection) || isMateConnector(context, selection))
    {
        return evVertexCoordSystem(context, { "vertex" : selection })->plane();
    }
    const cylinder = evSurfaceDefinition(context, { "face" : selection });
    if (cylinder is Cylinder)
    {
        var result = cylinder.coordSystem->plane();
        // The cylinder's origin is anywhere on its axis
        const centroid = evApproximateCentroid(context, { "entities" : selection });
        result.origin += result.normal * dot(centroid - result.origin, result.normal);
        return result;
    }
    throw regenError("Select a sketch point, mate connector, circle, or cylinder.", [parameterName], selection);
}

/**
 * Reports whether the chain's length (with the fit adjustment) is the length of the path around the sprockets, and how
 * many links are, if it isn't.
 */
function validateLength(context is Context, id is Id, definition is map, info is map, pathLength is ValueWithUnits)
{
    const chainLength = info.pitch * definition.links + definition.fitAdjustment;
    const oddNote = definition.links % 2 == 1 ? " An odd number of links needs an offset link." : "";
    if (withinDisplayPrecision(definition, pathLength, chainLength))
    {
        reportFeatureInfo(context, id, "The " ~ definition.links ~ " link chain fits the sprockets." ~ oddNote);
        return;
    }
    const tooLong = chainLength > pathLength;
    // The 100 link chain is 0.25 in too long for the sprockets: 98 links are closest. Get the distance close, then
    // use Robot tensioner to make it exact.
    reportFeatureWarning(context, id, "The " ~ definition.links ~ " link chain is " ~ makeValueString(definition.unitSystem, abs(chainLength - pathLength)) ~
            " too " ~ (tooLong ? "long" : "short") ~ " for the sprockets: " ~ closestLinks(info, pathLength - definition.fitAdjustment) ~
            " links are closest. Get the distance close, then use Robot tensioner to make it exact." ~ oddNote, ["links"]);
}

/**
 * The even number of links (as chain is joined with a master link) nearest `length`.
 */
function closestLinks(info is map, length is ValueWithUnits) returns number
{
    return max(2 * round(length / info.pitch / 2), 2);
}

/**
 * The chain's body: the path, thickened by its plates' height, and as wide as the chain.
 */
function createChain(context is Context, id is Id, info is map, chainPlane is Plane, path is Query) returns Query
{
    opOffsetWire(context, id + "offset", {
                "edges" : path,
                "normal" : chainPlane.normal,
                "offset1" : info.plateHeight / 2,
                "offset2" : info.plateHeight / 2,
                "makeRegions" : true
            });
    opExtrude(context, id + "extrude", {
                "entities" : qCreatedBy(id + "offset", EntityType.FACE),
                "direction" : chainPlane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : info.width / 2,
                "startBound" : BoundingType.BLIND,
                "startDepth" : info.width / 2
            });
    cleanup(context, id + "delete", qCreatedBy(id + "offset", EntityType.BODY));
    return qCreatedBy(id + "extrude", EntityType.BODY);
}

function setChainProperties(context is Context, chain is Query, definition is map)
{
    setProperty(context, {
                "entities" : chain,
                "propertyType" : PropertyType.NAME,
                // 100 Link #25 Chain
                "value" : definition.links ~ " Link " ~ getChainTypeName(definition.chainType) ~ " Chain"
            });
    setProperty(context, {
                "entities" : chain,
                "propertyType" : PropertyType.MATERIAL,
                "value" : STEEL
            });
    setProperty(context, {
                "entities" : chain,
                "propertyType" : PropertyType.APPEARANCE,
                "value" : STEEL_GRAY
            });
}

function addMateConnectors(context is Context, id is Id, chainPlane is Plane, circles is array, chain is Query)
{
    var mateConnectorSystem = coordSystem(chainPlane);
    for (var i, circle in circles)
    {
        const mateConnectorId = id + unstableIdComponent(i);
        setExternalDisambiguation(context, mateConnectorId, circle.identity);
        mateConnectorSystem.origin = planeToWorld(chainPlane, circle.location);
        opMateConnector(context, mateConnectorId, {
                    "coordSystem" : mateConnectorSystem,
                    "owner" : chain
                });
    }
}

const CHAIN_SIDE_FLIP_MANIPULATOR = "chainSideFlipManipulator";

/**
 * A flip manipulator on each arc, which moves the chain to the sprocket's other side.
 */
function addSideFlipManipulators(context is Context, id is Id, chainPlane is Plane, arcs is array, counterClockwise is boolean)
{
    var manipulators = {};
    for (var i, arc in arcs)
    {
        const tangentLine = evEdgeTangentLine(context, {
                    "edge" : arc,
                    "parameter" : 0.5
                });
        manipulators[CHAIN_SIDE_FLIP_MANIPULATOR ~ "." ~ i] = flipManipulator({
                    "base" : tangentLine.origin,
                    // Out of the chain's loop
                    "direction" : cross(chainPlane.normal, tangentLine.direction) * (counterClockwise ? 1 : -1),
                    "flipped" : false
                });
    }
    addManipulators(context, id, manipulators);
}

export function robotChainManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    for (var key, manipulator in newManipulators)
    {
        const parsed = match(key, CHAIN_SIDE_FLIP_MANIPULATOR ~ "\\.(\\d+)");
        if (parsed.hasMatch && manipulator.flipped)
        {
            const index = stringToNumber(parsed.captures[1]);
            const side = definition.sprockets[index].chainSide;
            definition.sprockets[index].chainSide = side == ChainSide.INSIDE ? ChainSide.OUTSIDE : ChainSide.INSIDE;
        }
    }
    return startOffsetManipulatorChange(definition, newManipulators);
}

/**
 * Select closest links sets Links to the even number nearest the path's length (less the fit adjustment).
 */
export function robotChainEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean, clickedButton is string) returns map
{
    if (clickedButton != "selectClosestLinks")
    {
        return definition;
    }
    // A guard: if the sprockets aren't all selected (or are wrong), there's no closest chain
    try silent
    {
        const info = getChainInfo(definition.chainType);
        const circles = getSprockets(context, definition, info).circles;
        const length = loopLength(circles, loopCounterClockwise(circles));
        definition.links = closestLinks(info, length - definition.fitAdjustment);
    }
    return definition;
}
