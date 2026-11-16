FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
import(path : "0195d390c3944cd4fab21ce0", version : "82243acdb77d04d0ca39cd49");
import(path : "ea127c07807644fb48d3a1ae", version : "71504a904ad4d8a579c5b4da");
export import(path : "948c83c1b1ac83de4ccf921b", version : "5bd77bd9ef3230343bfbc217");
export import(path : "b82468283e5ec09720bad185", version : "142c41c73cc2f8336c28931e");
import(path : "70d403fe3ae377ef6f571c77", version : "00896ab2792d3e568f3faadf");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "ee89dca8b88ae07af559109e");
// Exports ChainType and SprocketType, parameter types
export import(path : "93af3f24abb0f9f345268c81", version : "66e6f12bd269691031eca3e1");

/**
 * Whether the chain goes around two sprockets, set as far apart as its links make them, or around any sprockets and
 * idlers, where they're selected.
 */
export enum ChainMode
{
    annotation { "Name" : "Simple" }
    SIMPLE,
    annotation { "Name" : "Complex" }
    COMPLEX
}

export predicate isSimpleChain(definition is map)
{
    definition.chainMode == ChainMode.SIMPLE;
}

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
        "Feature Type Description" : "Create #25, #25H, #35, and 8mm roller chain around sprockets and idlers, and check its length." ~
        "<br>See also the Robot sprocket and Robot tensioner FeatureScripts, which work with this feature directly." ~ CREDIT,
        "Manipulator Change Function" : "robotChainManipulatorChange",
        "Editing Logic Function" : "robotChainEditLogic",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotChain = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        unitSystemPredicate(definition);

        annotation { "Name" : "Chain mode", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.chainMode is ChainMode;

        if (isSimpleChain(definition))
        {
            annotation {
                        "Name" : "Sprocket one location",
                        "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR || (EntityType.EDGE && GeometryType.CIRCLE) ||
                            (EntityType.FACE && GeometryType.CYLINDER) || (EntityType.BODY && BodyType.SOLID),
                        "MaxNumberOfPicks" : 1,
                        "Description" : "Its center (a sketch point, mate connector, or a circle or cylinder around it, like a sprocket's bore), its pitch circle, or a Robot sprocket."
                    }
            definition.sprocketOneLocation is Query;

            annotation { "Name" : "Location type", "UIHint" : ["SHOW_LABEL"] }
            definition.sprocketOneSelectionType is SprocketSelectionType;

            if (definition.sprocketOneSelectionType == SprocketSelectionType.CENTER)
            {
                annotation { "Name" : "Sprocket one teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isInteger(definition.sprocketOneTeeth, SPROCKET_TEETH_BOUNDS);
            }

            annotation {
                        "Name" : "Sprocket two location",
                        "Filter" : (EntityType.VERTEX && SketchObject.YES) || BodyType.MATE_CONNECTOR || (EntityType.EDGE && GeometryType.CIRCLE) ||
                            (EntityType.FACE && GeometryType.CYLINDER) || (EntityType.BODY && BodyType.SOLID),
                        "MaxNumberOfPicks" : 1,
                        "Description" : "Its center (a sketch point, mate connector, or a circle or cylinder around it, like a sprocket's bore), its pitch circle, or a Robot sprocket."
                    }
            definition.sprocketTwoLocation is Query;

            annotation { "Name" : "Location type", "UIHint" : ["SHOW_LABEL"] }
            definition.sprocketTwoSelectionType is SprocketSelectionType;

            if (definition.sprocketTwoSelectionType == SprocketSelectionType.CENTER)
            {
                annotation { "Name" : "Sprocket two teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isInteger(definition.sprocketTwoTeeth, SPROCKET_TEETH_BOUNDS);
            }
        }
        else
        {
            annotation {
                        "Name" : "Sprockets",
                        "Item name" : "Sprocket",
                        "Item label template" : "#chainSide #sprocketType",
                        "Driven query" : "location"
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

                annotation { "Name" : "Location type", "UIHint" : ["SHOW_LABEL"] }
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

                annotation { "Name" : "Chain side", "UIHint" : ["SHOW_LABEL"] }
                sprocket.chainSide is ChainSide;
            }
        }

        startOffsetPredicate(definition);

        annotation { "Group Name" : "Chain", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Chain type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.chainType is ChainType;

            annotation { "Name" : "Links", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isInteger(definition.links, LINKS_BOUNDS);

            annotation { "Name" : "Select closest links" }
            isButton(definition.selectClosestLinks);
        }

        annotation { "Group Name" : "Other options", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Add mate connectors", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.addMateConnectors is boolean;

            if (isSimpleChain(definition))
            {
                annotation { "Name" : "Center to center adjustment", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.centerToCenterAdjustment, ZERO_DEFAULT_LENGTH_BOUNDS);
            }
            else
            {
                annotation { "Name" : "Chain fit adjustment", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                            "Description" : "How much longer the path around the sprockets should be than the chain: positive for a looser chain." }
                isLength(definition.fitAdjustment, ZERO_DEFAULT_LENGTH_BOUNDS);
            }
        }
    }
    {
        const info = getChainInfo(definition.chainType);
        const sprockets = isSimpleChain(definition) ? getSimpleSprockets(context, id, definition, info) : getSprockets(context, definition, info);
        reportSprocketWarnings(context, id, sprockets);
        const chainPlane = sprockets.plane;
        const circles = sprockets.circles;
        addStartOffsetManipulator(context, id, definition, chainPlane);

        const counterClockwise = loopCounterClockwise(circles);
        var sketch;
        try
        {
            sketch = sketchLoop(context, id + "sketch", chainPlane, circles, counterClockwise);
        }
        catch (error)
        {
            // There's no path around them (like one inside another): show them
            throw regenError(error.message, sketchCircles(context, id + "errorCircles", chainPlane, circles));
        }
        if (isSimpleChain(definition))
        {
            reportSimpleFit(context, id, definition, sprockets);
        }
        else
        {
            addSideFlipManipulators(context, id, chainPlane, sketch.arcs, counterClockwise);
            reportLength(context, id, definition, info, loopLength(circles, counterClockwise));
        }

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
                    "attribute" : loopAttribute(name, chainPlane, sprockets.length, loopCircles, counterClockwise)
                });
        setChainProperties(context, chain, definition);

        if (definition.addMateConnectors)
        {
            addMateConnectors(context, id + "mateConnector", chainPlane, circles, sprockets.faceAttributes, chain);
        }
        cleanup(context, id + "delete", qCreatedBy(id + "sketch", EntityType.BODY));
    });

/**
 * What's chosen for each sprocket or idler: a complex chain's `sprockets`' items, or a simple chain's two sprockets, as
 * maps of their `location`, `selectionType`, `sprocketType`, `teeth`, `idlerDiameter`, and `chainSide`, and the
 * `parameterName` of the location, and the `identity` to disambiguate what's made around it (a simple chain's
 * sprockets have none, so changing what's selected doesn't change what the chain's faces are).
 */
function sprocketChoices(definition is map) returns array
{
    if (isSimpleChain(definition))
    {
        return mapArray(["sprocketOne", "sprocketTwo"], function(name)
            {
                return {
                        "location" : definition[name ~ "Location"],
                        "selectionType" : definition[name ~ "SelectionType"],
                        "sprocketType" : SprocketType.SPROCKET,
                        "teeth" : definition[name ~ "Teeth"],
                        "chainSide" : ChainSide.INSIDE,
                        "parameterName" : name ~ "Location"
                    };
            });
    }
    var choices = [];
    for (var i, sprocket in definition.sprockets)
    {
        sprocket.parameterName = arrayParameterId("sprockets", i, "location");
        sprocket.identity = sprocket.location;
        choices = append(choices, sprocket);
    }
    return choices;
}

/**
 * Where each sprocket or idler is, and what it is (see `sprocketChoices`): a Robot sprocket's from its attribute, a
 * center's with its teeth, or a pitch circle's, from its size.
 *
 * @returns {{
 *      @field planes {array} : Each one's plane, at its center.
 *      @field faceAttributes {array} : Each one's `ChainFaceAttribute`.
 *      @field fractional {array} : The parameters of pitch circles which aren't a whole number of teeth.
 *      @field otherChain {array} : The parameters of Robot sprockets for another chain.
 * }}
 */
function readSprockets(context is Context, definition is map, info is map, choices is array) returns map
{
    var planes = [];
    var faceAttributes = [];
    var fractional = [];
    var otherChain = [];
    for (var sprocket in choices)
    {
        const parameterName = sprocket.parameterName;
        var faceAttribute = { "chainType" : definition.chainType, "sprocketType" : sprocket.sprocketType };
        if (sprocket.selectionType == SprocketSelectionType.ROBOT_SPROCKET)
        {
            const attribute = getAttribute(context, { "entity" : sprocket.location, "name" : SPROCKET_ATTRIBUTE });
            if (attribute == undefined || !canBeSprocketAttribute(attribute) || attribute.coordSystem.coordSystem == undefined)
            {
                throw regenError("Select a sprocket made by Robot sprocket, or one of its mate connectors.", [parameterName], sprocket.location);
            }
            if (!sameSprockets(attribute.chainType, definition.chainType))
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
                // Exactly a whole number of teeth's pitch circle, to within std's length tolerance
                if (!tolerantEquals(getSprocketRadius(info.pitch, round(pitchTeeth)), evCurveDefinition(context, { "edge" : sprocket.location }).radius))
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
    return {
            "planes" : planes,
            "faceAttributes" : faceAttributes,
            "fractional" : fractional,
            "otherChain" : otherChain
        };
}

/**
 * The circle the chain's path goes around for a sprocket or idler (see `readSprockets`), at `location` in the chain's
 * plane: a sprocket's pitch circle, or an idler, with the chain's rollers on it (with its `idlerRadius`).
 */
function sprocketCircle(info is map, sprocket is map, faceAttribute is ChainFaceAttribute, location is Vector) returns BoundaryCircle
{
    var circle = {
        "identity" : sprocket.identity,
        "location" : location,
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
    return circle as BoundaryCircle;
}

/**
 * A complex chain's sprockets and idlers, where they're selected.
 *
 * @returns {{
 *      @field plane {Plane} : The chain's plane: the first sprocket's, with the start offset.
 *      @field circles {array} : The `BoundaryCircle`s its path goes around (see `sprocketCircle`).
 *      @field faceAttributes {array} : Each one's `ChainFaceAttribute`.
 *      @field length {ValueWithUnits} : The length the path should be: the chain's, with the fit adjustment.
 *      @field fractional {array} : The parameters of pitch circles which aren't a whole number of teeth.
 *      @field otherChain {array} : The parameters of Robot sprockets for another chain.
 *      @field misaligned {array} : The parameters of sprockets and idlers not in the first one's plane.
 * }}
 */
function getSprockets(context is Context, definition is map, info is map) returns map
{
    verifyNonemptyArray(context, definition, "sprockets", "Add the sprockets and idlers the chain goes around.");
    if (size(definition.sprockets) < 2)
    {
        throw regenError("Add at least two sprockets.", ["sprockets"]);
    }
    const choices = sprocketChoices(definition);
    for (var sprocket in choices)
    {
        verifyNonemptyArrayQuery(context, definition, sprocket.parameterName, "Select the sprocket's or idler's center, pitch circle, or Robot sprocket.");
    }
    var result = readSprockets(context, definition, info, choices);
    const planes = result.planes;

    // Each is projected onto the chain's plane: those which aren't in the first one's are warned about
    result.misaligned = [];
    for (var i, sprocketPlane in planes)
    {
        if (!coplanarPlanes(sprocketPlane, planes[0]))
        {
            result.misaligned = append(result.misaligned, choices[i].parameterName);
        }
    }
    result.plane = applyStartOffset(context, definition, planes[0]);
    result.circles = [];
    for (var i, sprocket in choices)
    {
        result.circles = append(result.circles, sprocketCircle(info, sprocket, result.faceAttributes[i], worldToPlane(result.plane, planes[i].origin)));
    }
    result.length = info.pitch * definition.links + definition.fitAdjustment;
    return result;
}

/**
 * A simple chain's two sprockets: sprocket one's where it's selected, and sprocket two toward its selection, as far
 * as the chain's links make it (with the center to center adjustment), unless it's selected about that far already
 * (to the display's precision), when it's where it's selected, so other parts line up with it.
 *
 * @returns {{
 *      @field measured {ValueWithUnits} : How far apart the sprockets are selected.
 *      @field ideal {ValueWithUnits} : How far apart the chain makes them, with the adjustment.
 *      @field fits {boolean} : Whether they're selected that far apart (so they're where they're selected).
 * }}
 *      and the rest as `getSprockets` returns.
 */
function getSimpleSprockets(context is Context, id is Id, definition is map, info is map) returns map
{
    verifyNonemptyQuery(context, definition, "sprocketOneLocation", "Select sprocket one's center, pitch circle, or Robot sprocket.");
    verifyNonemptyQuery(context, definition, "sprocketTwoLocation", "Select sprocket two's center, pitch circle, or Robot sprocket.");
    const choices = sprocketChoices(definition);
    var result = readSprockets(context, definition, info, choices);
    const planes = result.planes;
    const plane = applyStartOffset(context, definition, planes[0]);
    const toSecond = project(plane, planes[1].origin) - plane.origin;
    if (tolerantEqualsZero(norm(toSecond)))
    {
        throw regenError("The selected sprockets are on the same axis.", ["sprocketOneLocation", "sprocketTwoLocation"],
            qUnion(definition.sprocketOneLocation, definition.sprocketTwoLocation));
    }
    result.misaligned = coplanarPlanes(planes[1], planes[0]) ? [] : ["sprocketTwoLocation"];

    const radii = mapArray(result.faceAttributes, function(faceAttribute)
        {
            return getSprocketRadius(info.pitch, faceAttribute.teeth);
        });
    const measured = norm(toSecond);
    // The sprockets' pitch circles where they're selected, to show what an error's about
    const selected = function()
        {
            return sketchCircles(context, id + "errorCircles", plane, [
                        { "location" : zeroVector(2) * meter, "radius" : radii[0] },
                        { "location" : worldToPlane(plane, plane.origin + toSecond), "radius" : radii[1] }
                    ]);
        };
    const chainDistance = twoCircleDistance(radii, info.pitch * definition.links, measured);
    if (chainDistance == undefined)
    {
        throw regenError("The " ~ definition.links ~ " link chain is too short for its sprockets.", ["links"], selected());
    }
    result.ideal = chainDistance + definition.centerToCenterAdjustment;
    result.measured = measured;
    result.fits = withinDisplayPrecision(definition, measured, result.ideal);
    const distance = result.fits ? measured : result.ideal;

    result.plane = plane;
    result.circles = [
            sprocketCircle(info, choices[0], result.faceAttributes[0], zeroVector(2) * meter),
            sprocketCircle(info, choices[1], result.faceAttributes[1], worldToPlane(plane, plane.origin + normalize(toSecond) * distance))
        ];
    // The path at the chain's distance (with the adjustment) is what Robot tensioner tensions it to
    result.length = tryTwoCircleLength(radii, result.ideal);
    if (result.length == undefined)
    {
        throw regenError("Center to center adjustment is too large, decrease the value.", ["centerToCenterAdjustment"], selected());
    }
    return result;
}

/**
 * Reports whether a simple chain's sprockets are selected as far apart as its links make them: info if they are, and
 * if they aren't, a warning of how far apart they should be.
 */
function reportSimpleFit(context is Context, id is Id, definition is map, sprockets is map)
{
    const oddNote = definition.links % 2 == 1 ? " An odd number of links needs an offset link." : "";
    if (sprockets.fits)
    {
        reportFeatureInfo(context, id, "The " ~ definition.links ~ " link chain fits the sprockets." ~ oddNote);
        return;
    }
    // Doesn't end with a period, to make copying the distance easier, as Robot belt's
    reportFeatureWarning(context, id, "To use this chain, update the part studio so the distance between the sprockets is " ~
            makeValueString(definition.unitSystem, sprockets.ideal) ~ oddNote, ["sprocketTwoLocation"]);
}

/**
 * Reports a warning for each problem with the sprockets: pitch circles of a fraction of teeth, Robot sprockets for
 * another chain, and sprockets not in the first one's plane. Only the first status reported shows.
 */
function reportSprocketWarnings(context is Context, id is Id, sprockets is map)
{
    if (sprockets.fractional != [])
    {
        reportFeatureWarning(context, id, "A selected pitch circle isn't the size of a sprocket with a whole number of teeth: it's used as the nearest one.",
            sprockets.fractional);
    }
    if (sprockets.otherChain != [])
    {
        reportFeatureWarning(context, id, "A selected Robot sprocket is for another type of chain.", sprockets.otherChain);
    }
    if (sprockets.misaligned != [])
    {
        reportFeatureWarning(context, id, "A selected sprocket or idler isn't lined up with the first: the chain goes around where it is in the first one's plane.",
            sprockets.misaligned);
    }
}

/**
 * The plane of a sketch point, mate connector, circle, or cylinder, at its center (a cylinder's, in its middle).
 */
function getLocationPlane(context is Context, selection is Query, parameterName is string) returns Plane
{
    const result = selectionPlane(context, selection);
    if (result != undefined)
    {
        return result;
    }
    throw regenError("Select a sketch point, mate connector, circle, or cylinder (or set the location type to Robot sprocket).", [parameterName], selection);
}

/**
 * Reports whether the chain's length (with the fit adjustment) is the length of the path around the sprockets: info
 * if it is, and if it isn't, a warning of how many links are closest.
 */
function reportLength(context is Context, id is Id, definition is map, info is map, pathLength is ValueWithUnits)
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
 * Select closest links sets Links to the even number nearest the path's length, where the sprockets are selected
 * (less the fit or center to center adjustment).
 */
export function robotChainEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query, clickedButton is string) returns map
{
    const pitch = getChainInfo(definition.chainType).pitch;
    const kindOf = function(selection is Query)
        {
            const kind = locationKind(context, selection, SPROCKET_ATTRIBUTE, function(radius)
                {
                    return pitchCircleTeeth(pitch, radius);
                });
            if (kind == undefined)
            {
                return undefined;
            }
            return switch (kind) {
                        "part" : SprocketSelectionType.ROBOT_SPROCKET,
                        "pitchCircle" : SprocketSelectionType.PITCH_CIRCLE,
                        "center" : SprocketSelectionType.CENTER
                    };
        };
    if (isSimpleChain(definition))
    {
        for (var name in ["sprocketOne", "sprocketTwo"])
        {
            const selection = definition[name ~ "Location"];
            const old = oldDefinition[name ~ "Location"];
            if (old != undefined && areQueriesEquivalent(context, old, selection))
            {
                continue;
            }
            const selectionType = kindOf(selection);
            if (selectionType != undefined)
            {
                definition[name ~ "SelectionType"] = selectionType;
            }
        }
    }
    else
    {
        for (var i, sprocket in definition.sprockets)
        {
            if (!selectionChanged(context, oldDefinition.sprockets, i, "location", sprocket.location))
            {
                continue;
            }
            const selectionType = kindOf(sprocket.location);
            if (selectionType != undefined)
            {
                definition.sprockets[i].selectionType = selectionType;
            }
        }
    }

    if (clickedButton == "selectClosestLinks")
    {
        // A guard: if the sprockets aren't all selected (or are wrong), there's no closest chain
        try silent
        {
            definition.links = closestLinks(getChainInfo(definition.chainType), selectedPathLength(context, definition));
        }
    }
    return definition;
}

/**
 * The length the chain should be for the sprockets where they're selected: the path's around them, less the fit
 * adjustment, or a simple chain's, less the center to center adjustment.
 */
function selectedPathLength(context is Context, definition is map) returns ValueWithUnits
{
    const info = getChainInfo(definition.chainType);
    if (!isSimpleChain(definition))
    {
        const circles = getSprockets(context, definition, info).circles;
        return loopLength(circles, loopCounterClockwise(circles)) - definition.fitAdjustment;
    }
    verifyNonemptyQuery(context, definition, "sprocketOneLocation", "Select sprocket one's center, pitch circle, or Robot sprocket.");
    verifyNonemptyQuery(context, definition, "sprocketTwoLocation", "Select sprocket two's center, pitch circle, or Robot sprocket.");
    const sprockets = readSprockets(context, definition, info, sprocketChoices(definition));
    const plane = sprockets.planes[0];
    const radii = mapArray(sprockets.faceAttributes, function(faceAttribute)
        {
            return getSprocketRadius(info.pitch, faceAttribute.teeth);
        });
    // Its start offset moves the chain along the sprockets' axis, not their distance apart
    const distance = norm(project(plane, sprockets.planes[1].origin) - plane.origin) - definition.centerToCenterAdjustment;
    const length = tryTwoCircleLength(radii, distance);
    if (length == undefined)
    {
        throw regenError("There's no path around the sprockets.");
    }
    return length;
}
