FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
import(path : "ea127c07807644fb48d3a1ae", version : "3c1ddfaf5ff0b3d5897422d0");
export import(path : "948c83c1b1ac83de4ccf921b", version : "e4ee8d8fa0d9ee2f7a34dd9f");
export import(path : "b82468283e5ec09720bad185", version : "f035c6827196f0268491d7a0");
import(path : "70d403fe3ae377ef6f571c77", version : "a2f868f07eddf15bac1228f8");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "4f09b23b6e418ecb226e90c1");
// Exports ChainType and SprocketType, parameter types
export import(path : "93af3f24abb0f9f345268c81", version : "96e431e9857c4c335cdb3eb1");

export enum ChainSide
{
    annotation { "Name" : "Inside" }
    INSIDE,
    annotation { "Name" : "Outside" }
    OUTSIDE
}

/**
 * What a sprocket's location is: its center (with its teeth given), its pitch circle (which gives its teeth), or a
 * Robot sprocket (which has its own). Editing logic sets it from what's selected.
 */
export enum SprocketSelectionType
{
    annotation { "Name" : "Center" }
    CENTER,
    annotation { "Name" : "Pitch circle" }
    PITCH_CIRCLE,
    annotation { "Name" : "Robot sprocket" }
    ROBOT_SPROCKET
}

const LINKS_BOUNDS = { (unitless) : [4, 100, 1e5] } as IntegerBoundSpec;
const SPROCKET_TEETH_BOUNDS = { (unitless) : [5, 16, 500] } as IntegerBoundSpec;
const IDLER_DIAMETER_BOUNDS = { (meter) : [1e-4, 0.0254, 500], (inch) : 1, (millimeter) : 25 } as LengthBoundSpec;

annotation {
        "Feature Type Name" : "Robot chain",
        "Feature Type Description" : "Create #25, #35, and 8mm roller chain around sprockets and idlers, and check its length." ~
        "<br>See also the Robot sprocket and Robot tensioner FeatureScripts, which work with this feature directly." ~ CREDIT,
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
                        "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR || (EntityType.EDGE && GeometryType.CIRCLE) ||
                            (EntityType.FACE && GeometryType.CYLINDER) || (EntityType.BODY && BodyType.SOLID),
                        "MaxNumberOfPicks" : 1,
                        "Description" : "Its center (a sketch point, mate connector, or a circle or cylinder around it, like a sprocket's bore), its pitch circle, or a Robot sprocket."
                    }
            sprocket.location is Query;

            annotation { "Name" : "Location type", "UIHint" : ["SHOW_LABEL"],
                        "Description" : "What the location is. Set from what's selected: a circle the size of a pitch circle is one." }
            sprocket.selectionType is SprocketSelectionType;

            if (sprocket.selectionType != SprocketSelectionType.ROBOT_SPROCKET)
            {
                annotation { "Name" : "Type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                sprocket.sprocketType is SprocketType;

                if (sprocket.sprocketType == SprocketType.SPROCKET && sprocket.selectionType == SprocketSelectionType.CENTER)
                {
                    annotation { "Name" : "Teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isInteger(sprocket.teeth, SPROCKET_TEETH_BOUNDS);
                }
                else if (sprocket.sprocketType == SprocketType.IDLER)
                {
                    annotation { "Name" : "Idler diameter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "The diameter the chain's rollers run on." }
                    isLength(sprocket.idlerDiameter, IDLER_DIAMETER_BOUNDS);
                }
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
        reportSprocketWarnings(context, id, sprockets);
        const chainPlane = sprockets.plane;
        const circles = sprockets.circles;
        addStartOffsetManipulator(context, id, definition, chainPlane);

        const counterClockwise = loopCounterClockwise(circles);
        const sketch = sketchLoop(context, id + "sketch", chainPlane, circles, counterClockwise);
        addSideFlipManipulators(context, id, chainPlane, sketch.arcs, counterClockwise);
        validateLength(context, id, definition, info, loopLength(circles, counterClockwise));

        const chain = createChain(context, id + "chain", definition, info, chainPlane, sketch, sprockets.faceAttributes);
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
            addMateConnectors(context, id + "mateConnector", chainPlane, circles, sprockets.faceAttributes, chain);
        }
        cleanup(context, id + "delete", qCreatedBy(id + "sketch", EntityType.BODY));
    });

/**
 * The chain's sprockets and idlers, as selected.
 *
 * @returns {{
 *      @field plane {Plane} : The chain's plane: the first sprocket's, with the start offset.
 *      @field circles {array} : The `BoundaryCircle`s its path goes around: a sprocket's pitch circle, or an idler,
 *              with the chain's rollers on it (with its `idlerRadius`).
 *      @field faceAttributes {array} : Each one's `ChainFaceAttribute`.
 *      @field fractional {array} : The parameters of pitch circles which aren't a whole number of teeth.
 *      @field otherChain {array} : The parameters of Robot sprockets for another chain.
 * }}
 */
function getSprockets(context is Context, definition is map, info is map) returns map
{
    verifyNonemptyArray(context, definition, "sprockets", "Add the sprockets and idlers the chain goes around.");
    if (size(definition.sprockets) < 2)
    {
        throw regenError("Add at least two sprockets.", ["sprockets"]);
    }

    var planes = [];
    var faceAttributes = [];
    var fractional = [];
    var otherChain = [];
    for (var i, sprocket in definition.sprockets)
    {
        const parameterName = arrayParameterId("sprockets", i, "location");
        verifyNonemptyArrayQuery(context, definition, parameterName, "Select the sprocket's or idler's center, pitch circle, or Robot sprocket.");
        var faceAttribute = { "chainType" : definition.chainType, "sprocketType" : sprocket.sprocketType };
        if (sprocket.selectionType == SprocketSelectionType.ROBOT_SPROCKET)
        {
            const attribute = getAttribute(context, { "entity" : sprocket.location, "name" : SPROCKET_ATTRIBUTE });
            if (attribute == undefined || !canBeSprocketAttribute(attribute) || attribute.coordSystem.coordSystem == undefined)
            {
                throw regenError("Select a sprocket made by Robot sprocket, or one of its mate connectors.", [parameterName], sprocket.location);
            }
            if (attribute.chainType != definition.chainType)
            {
                otherChain = append(otherChain, parameterName);
            }
            planes = append(planes, attribute.coordSystem.coordSystem->plane());
            faceAttribute.sprocketType = SprocketType.SPROCKET;
            faceAttribute.teeth = attribute.teeth;
        }
        else
        {
            planes = append(planes, getLocationPlane(context, sprocket.location, parameterName));
            if (sprocket.sprocketType == SprocketType.IDLER)
            {
                faceAttribute.idlerRadius = sprocket.idlerDiameter / 2;
            }
            else if (sprocket.selectionType == SprocketSelectionType.PITCH_CIRCLE)
            {
                if (!isCircle(context, sprocket.location))
                {
                    throw regenError("Select a circle the size of the sprocket's pitch circle.", [parameterName], sprocket.location);
                }
                const pitchTeeth = pitchCircleTeeth(info.pitch, evCurveDefinition(context, { "edge" : sprocket.location }).radius);
                if (pitchTeeth == undefined)
                {
                    throw regenError("The selected pitch circle is too small for the chain.", [parameterName], sprocket.location);
                }
                if (!tolerantEquals(pitchTeeth, round(pitchTeeth)))
                {
                    fractional = append(fractional, parameterName);
                }
                faceAttribute.teeth = round(pitchTeeth);
            }
            else
            {
                faceAttribute.teeth = sprocket.teeth;
            }
        }
        faceAttributes = append(faceAttributes, faceAttribute as ChainFaceAttribute);
    }

    const chainPlane = applyStartOffset(context, definition, planes[0]);
    var circles = [];
    for (var i, sprocket in definition.sprockets)
    {
        const faceAttribute = faceAttributes[i];
        var circle = {
            "identity" : sprocket.location,
            // Projected onto the chain's plane
            "location" : worldToPlane(chainPlane, planes[i].origin),
            "flipped" : sprocket.chainSide == ChainSide.OUTSIDE
        };
        if (faceAttribute.sprocketType == SprocketType.SPROCKET)
        {
            circle.radius = getSprocketRadius(info.pitch, faceAttribute.teeth);
        }
        else
        {
            circle.idlerRadius = faceAttribute.idlerRadius;
            circle.radius = circle.idlerRadius + info.rollerDiameter / 2;
        }
        circles = append(circles, circle as BoundaryCircle);
    }
    return {
            "plane" : chainPlane,
            "circles" : circles,
            "faceAttributes" : faceAttributes,
            "fractional" : fractional,
            "otherChain" : otherChain
        };
}

function reportSprocketWarnings(context is Context, id is Id, sprockets is map)
{
    if (sprockets.fractional != [])
    {
        reportFeatureWarning(context, id, "A selected pitch circle isn't the size of a sprocket with a whole number of teeth: it's used as the nearest one.", sprockets.fractional);
    }
    if (sprockets.otherChain != [])
    {
        reportFeatureWarning(context, id, "A selected Robot sprocket is for another type of chain.", sprockets.otherChain);
    }
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
    const faces = selection->qEntityFilter(EntityType.FACE)->qGeometry(GeometryType.CYLINDER);
    if (!isQueryEmpty(context, faces))
    {
        var result = evSurfaceDefinition(context, { "face" : faces }).coordSystem->plane();
        // The cylinder's origin is anywhere on its axis
        const centroid = evApproximateCentroid(context, { "entities" : faces });
        result.origin += result.normal * dot(centroid - result.origin, result.normal);
        return result;
    }
    throw regenError("Select a sketch point, mate connector, circle, or cylinder (or set the location type to Robot sprocket).", [parameterName], selection);
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
 * The chain's body: the path, thickened by its plates' height, and as wide as the chain. Its curved faces around each
 * sprocket get its `ChainFaceAttribute`, so Robot sprocket can make sprockets on them.
 */
function createChain(context is Context, id is Id, definition is map, info is map, chainPlane is Plane, sketch is map, faceAttributes is array) returns Query
{
    const arcTracking = mapArray(sketch.arcs, function(arc)
        {
            return startTracking(context, arc);
        });
    opOffsetWire(context, id + "offset", {
                "edges" : sketch.path,
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
    for (var i, tracking in arcTracking)
    {
        const faces = tracking->qEntityFilter(EntityType.FACE)->qGeometry(GeometryType.CYLINDER);
        if (!isQueryEmpty(context, faces))
        {
            setAttribute(context, {
                        "entities" : faces,
                        "name" : CHAIN_SPROCKET_FACE_ATTRIBUTE,
                        "attribute" : faceAttributes[i]
                    });
        }
    }
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

function addMateConnectors(context is Context, id is Id, chainPlane is Plane, circles is array, faceAttributes is array, chain is Query)
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
        setAttribute(context, {
                    "entities" : qCreatedBy(mateConnectorId, EntityType.BODY),
                    "name" : CHAIN_SPROCKET_FACE_ATTRIBUTE,
                    "attribute" : faceAttributes[i]
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
 * Sets each sprocket's location type from what's selected, when its selection changes (see `locationKind`); and
 * Select closest links sets Links to the even number nearest the path's length (less the fit adjustment).
 */
export function robotChainEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query, clickedButton is string) returns map
{
    const pitch = getChainInfo(definition.chainType).pitch;
    for (var i, sprocket in definition.sprockets)
    {
        if (!selectionChanged(context, oldDefinition.sprockets, i, "location", sprocket.location))
        {
            continue;
        }
        const kind = locationKind(context, sprocket.location, SPROCKET_ATTRIBUTE, function(radius)
            {
                return pitchCircleTeeth(pitch, radius);
            });
        if (kind != undefined)
        {
            definition.sprockets[i].selectionType = switch (kind) {
                        "part" : SprocketSelectionType.ROBOT_SPROCKET,
                        "pitchCircle" : SprocketSelectionType.PITCH_CIRCLE,
                        "center" : SprocketSelectionType.CENTER
                    };
        }
    }

    if (clickedButton == "selectClosestLinks")
    {
        // A guard: if the sprockets aren't all selected (or are wrong), there's no closest chain
        try silent
        {
            const info = getChainInfo(definition.chainType);
            const circles = getSprockets(context, definition, info).circles;
            const length = loopLength(circles, loopCounterClockwise(circles));
            definition.links = closestLinks(info, length - definition.fitAdjustment);
        }
    }
    return definition;
}
