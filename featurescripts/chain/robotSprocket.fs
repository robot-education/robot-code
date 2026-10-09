FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "core/coreUtils.fs", version : "");
export import(path : "core/unitSystem.fs", version : "");
export import(path : "core/startOffset.fs", version : "");
import(path : "core/location.fs", version : "");
import(path : "core/robotFeature.fs", version : "");
import(path : "core/robotProperties.fs", version : "");
import(path : "core/profileOffset.fs", version : "");
// Exports BoreShape, Fit, and SplineType, parameter types
export import(path : "core/bore.fs", version : "");
// Exports ChainType, a parameter type
export import(path : "chain/chainCommon.fs", version : "");

export enum SprocketCreationMethod
{
    annotation { "Name" : "Manual" }
    MANUAL,
    annotation { "Name" : "Chain" }
    CHAIN
}

const SPROCKET_TEETH_BOUNDS = { (unitless) : [5, 16, 500] } as IntegerBoundSpec;

/**
 * Makes sprockets (ISO 606 teeth, as wide as its chain's), where a sketch point is, or where a Robot chain goes around
 * one, with a bore.
 */
annotation {
        "Feature Type Name" : "Robot sprocket",
        "Feature Type Description" : "Create #25, #35, and 8mm sprockets." ~
        "<br>See also the Robot chain FeatureScript, which works with this feature directly." ~ CREDIT,
        "Manipulator Change Function" : "robotSprocketManipulatorChange",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotSprocket = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        unitSystemPredicate(definition);

        annotation { "Name" : "Creation method", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.creationMethod is SprocketCreationMethod;

        if (definition.creationMethod == SprocketCreationMethod.MANUAL)
        {
            locationPredicate(definition, "sprocket");

            annotation { "Group Name" : "Sprocket", "Collapsed By Default" : false }
            {
                annotation { "Name" : "Chain type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                definition.chainType is ChainType;

                annotation { "Name" : "Teeth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isInteger(definition.teeth, SPROCKET_TEETH_BOUNDS);
            }

            startOffsetPredicate(definition);
        }
        else
        {
            annotation { "Name" : "Curved chain faces or mate connectors", "Filter" : (EntityType.FACE && GeometryType.CYLINDER) || BodyType.MATE_CONNECTOR,
                        "UIHint" : UIHint.PREVENT_CREATING_NEW_MATE_CONNECTORS,
                        "Description" : "A Robot chain's faces around its sprockets, or its mate connectors: a sprocket is made for each." }
            definition.chainSelections is Query;
        }

        borePredicate(definition);

        annotation { "Group Name" : "Other options", "Collapsed By Default" : true }
        {
            profileOffsetPredicate(definition);

            annotation { "Name" : "Add mate connectors", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.addMateConnectors is boolean;
        }
    }
    {
        var sprockets;
        if (definition.creationMethod == SprocketCreationMethod.MANUAL)
        {
            var sprocketPlane = applyStartOffset(context, definition, getLocationPlane(context, definition));
            addStartOffsetManipulator(context, id, definition, sprocketPlane);
            sprockets = [{ "plane" : sprocketPlane, "chainType" : definition.chainType, "teeth" : definition.teeth }];
        }
        else
        {
            sprockets = getChainSprockets(context, definition);
        }

        const bodies = createSprockets(context, id + "sprocket", definition, sprockets);
        if (offsetProfile(definition))
        {
            const profileAxis = line(sprockets[0].plane.origin, -sprockets[0].plane.x);
            const faces = qOwnedByBody(bodies[0], EntityType.FACE);
            addProfileOffsetManipulator(context, id, PROFILE_OFFSET_MANIPULATOR, profileAxis, qSubtraction(faces, qParallelPlanes(faces, sprockets[0].plane)),
                definition.profileOffsetOppositeDirection);
        }

        if (definition.addMateConnectors)
        {
            addMateConnectors(context, id + "mateConnector", sprockets, bodies);
        }

        const bore = definitionBore(definition);
        if (bore != undefined)
        {
            cutBores(context, id, id + "bore", bore,
                mapArray(sprockets, function(sprocket) { return sprocket.plane; }),
                mapArray(sprockets, function(sprocket) { return sprocket.identity; }),
                bodies);
        }
    });

/**
 * A sprocket at each selected face or mate connector of a Robot chain (but its idlers), once per place.
 */
function getChainSprockets(context is Context, definition is map) returns array
{
    const selections = verifyNonemptyQuery(context, definition, "chainSelections", "Select a Robot chain's curved faces or mate connectors.");
    var sprockets = [];
    var seen = {};
    for (var selection in selections)
    {
        const attribute = getAttribute(context, { "entity" : selection, "name" : CHAIN_SPROCKET_FACE_ATTRIBUTE });
        if (attribute == undefined || !canBeChainFaceAttribute(attribute))
        {
            throw regenError("Select a curved face or mate connector of a chain made by Robot chain.", ["chainSelections"], selection);
        }
        if (attribute.sprocketType == SprocketType.IDLER)
        {
            throw regenError("The selection is around one of the chain's idlers, not a sprocket.", ["chainSelections"], selection);
        }

        var sprocketPlane;
        if (isMateConnector(context, selection))
        {
            sprocketPlane = evVertexCoordSystem(context, { "vertex" : selection })->plane();
        }
        else
        {
            sprocketPlane = evSurfaceDefinition(context, { "face" : selection }).coordSystem->plane();
            // The cylinder's origin is anywhere on its axis: the chain's middle is the face's
            const centroid = evApproximateCentroid(context, { "entities" : selection });
            sprocketPlane.origin += sprocketPlane.normal * dot(centroid - sprocketPlane.origin, sprocketPlane.normal);
        }
        // A chain's two faces around a sprocket (and its mate connector) make one sprocket
        if (seen[sprocketPlane.origin] != undefined)
        {
            continue;
        }
        seen[sprocketPlane.origin] = true;
        sprockets = append(sprockets, {
                        "identity" : selection,
                        "plane" : sprocketPlane,
                        "chainType" : attribute.chainType,
                        "teeth" : attribute.teeth
                    });
    }
    return sprockets;
}

function createSprockets(context is Context, id is Id, definition is map, sprockets is array) returns array
{
    const profileOffset = getProfileOffset(definition);
    var bodies = [];
    for (var i, sprocket in sprockets)
    {
        const sprocketId = id + unstableIdComponent(i);
        if (sprocket.identity != undefined)
        {
            setExternalDisambiguation(context, sprocketId, sprocket.identity);
        }
        const profile = sketchSprocketProfile(context, sprocketId + "profile", sprocket.plane, sprocket.chainType, sprocket.teeth, profileOffset);
        const width = getSprocketToothWidth(sprocket.chainType);
        opExtrude(context, sprocketId + "extrude", {
                    "entities" : profile,
                    "direction" : sprocket.plane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : width / 2,
                    "startBound" : BoundingType.BLIND,
                    "startDepth" : width / 2
                });
        const body = qCreatedBy(sprocketId + "extrude", EntityType.BODY);
        setAttribute(context, {
                    "entities" : body,
                    "name" : SPROCKET_ATTRIBUTE,
                    "attribute" : sprocketAttribute(sprocket, "body")
                });
        setProperty(context, {
                    "entities" : body,
                    "propertyType" : PropertyType.NAME,
                    // 16T #25 Sprocket
                    "value" : sprocket.teeth ~ "T " ~ getChainTypeName(sprocket.chainType) ~ " Sprocket"
                });
        setProperty(context, {
                    "entities" : body,
                    "propertyType" : PropertyType.MATERIAL,
                    "value" : PLASTIC
                });
        setProperty(context, {
                    "entities" : body,
                    "propertyType" : PropertyType.APPEARANCE,
                    "value" : PRINTED_GREEN
                });
        bodies = append(bodies, body);
    }
    cleanup(context, id + "deleteSketches", qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES));
    return bodies;
}

function sprocketAttribute(sprocket is map, coordSystemId is string) returns SprocketAttribute
{
    return {
                "chainType" : sprocket.chainType,
                "teeth" : sprocket.teeth,
                "coordSystem" : persistentCoordSystem(coordSystem(sprocket.plane), coordSystemId, true)
            } as SprocketAttribute;
}

function addMateConnectors(context is Context, id is Id, sprockets is array, bodies is array)
{
    for (var i, sprocket in sprockets)
    {
        const mateConnectorId = id + unstableIdComponent(i);
        if (sprocket.identity != undefined)
        {
            setExternalDisambiguation(context, mateConnectorId, sprocket.identity);
        }
        opMateConnector(context, mateConnectorId, {
                    "coordSystem" : coordSystem(sprocket.plane),
                    "owner" : bodies[i]
                });
        setAttribute(context, {
                    "entities" : qCreatedBy(mateConnectorId, EntityType.BODY),
                    "name" : SPROCKET_ATTRIBUTE,
                    "attribute" : sprocketAttribute(sprocket, "mateConnector")
                });
    }
}

export function robotSprocketManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    definition = profileOffsetManipulatorChange(definition, newManipulators[PROFILE_OFFSET_MANIPULATOR], PROFILE_OFFSET_FLIP);
    return startOffsetManipulatorChange(definition, newManipulators);
}
