FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "8b8c46128a5dbc2594925f4a", version : "0a4039e144d8b21589cb8d49");
export import(path : "948c83c1b1ac83de4ccf921b", version : "5bd77bd9ef3230343bfbc217");
export import(path : "b82468283e5ec09720bad185", version : "142c41c73cc2f8336c28931e");
import(path : "0195d390c3944cd4fab21ce0", version : "82243acdb77d04d0ca39cd49");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "0794d10863d10d98a88c2ab4", version : "ee89dca8b88ae07af559109e");
import(path : "0103ad63394d7713fbf44448", version : "5f7d67b62160df309da69ec3");
// Exports BoreShape, Fit, and SplineType, parameter types
export import(path : "01f0c5634015659514b83da1", version : "bdc467e5999f2319f4645191");
// Exports ChainType, a parameter type
export import(path : "93af3f24abb0f9f345268c81", version : "66e6f12bd269691031eca3e1");
export import(path : "core/simplify.fs", version : "");
export import(path : "core/autoAssembly.fs", version : "");
// Exports sprocketProductTable and SPROCKET_PRODUCTS, FRCDesignLib's sprockets
export import(path : "chain/sprocketProducts.gen.fs", version : "");

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
        "Auto Assembly" : true,
        "Simplification" : true,
        "Feature Type Description" : "Create #25, #25H, #35, and 8mm sprockets." ~
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
                        "UIHint" : UIHint.PREVENT_CREATING_NEW_MATE_CONNECTORS }
            definition.chainSelections is Query;
        }

        annotation { "Name" : "Library sprocket", "Default" : false, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                    "Description" : "Builds a stand-in for one of FRCDesignLib's sprockets, which FRCDesign's Auto Assembly swaps in." }
        definition.librarySprocket is boolean;

        if (definition.librarySprocket)
        {
            annotation { "Group Name" : "Library sprocket", "Collapsed By Default" : false, "Driving Parameter" : "librarySprocket" }
            {
                annotation { "Name" : "Sprocket", "Lookup Table" : sprocketProductTable, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.sprocketProduct is LookupTablePath;
            }
        }
        else
        {
            // A stand-in has none of a printed sprocket's options
            borePredicate(definition);

            annotation { "Group Name" : "Other options", "Collapsed By Default" : true }
            {
                profileOffsetPredicate(definition);

                annotation { "Name" : "Add mate connectors", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.addMateConnectors is boolean;

                simplificationPredicate(definition);
            }
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

        if (definition.librarySprocket)
        {
            // Hidden, they keep their values
            definition = mergeMaps(definition, { "addProfileOffset" : false, "allowSimplification" : false, "addMateConnectors" : false });
            sprockets = mapArray(sprockets, function(sprocket)
                {
                    return withLibraryProduct(definition, sprocket);
                });
        }

        const bodies = createSprockets(context, id + "sprocket", definition, sprockets);
        if (definition.librarySprocket)
        {
            reportMissingProducts(definition, sprockets, bodies);
            return;
        }
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
            cutBores(context, id + "bore", bore,
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

        const sprocketPlane = selectionPlane(context, selection);
        // A chain's two faces around a sprocket (and its mate connector) make one sprocket: their centers are the same,
        // within std's length tolerance (found from each, they differ by rounding)
        if (any(sprockets, sprocket => tolerantEquals(sprocket.plane.origin, sprocketPlane.origin)))
        {
            continue;
        }
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
    // Simplified: its pitch circle, without teeth
    const simplified = isSimplified(context, definition);
    var bodies = [];
    for (var i, sprocket in sprockets)
    {
        const sprocketId = id + unstableIdComponent(i);
        if (sprocket.identity != undefined)
        {
            setExternalDisambiguation(context, sprocketId, sprocket.identity);
        }
        if (sprocket.product != undefined)
        {
            bodies = append(bodies, addSprocketStandIn(context, sprocketId, sprocket));
            continue;
        }
        var profile;
        if (simplified || sprocket.missing != undefined)
        {
            // Its pitch circle, without teeth (or as a missing library sprocket's stand-in would be, to show)
            const sketch = newSketchOnPlane(context, sprocketId + "profile", { "sketchPlane" : sprocket.plane });
            skCircle(sketch, "pitchCircle", {
                        "center" : vector(0, 0) * meter,
                        "radius" : getSprocketRadius(getChainInfo(sprocket.chainType).pitch, sprocket.teeth) + profileOffset
                    });
            skSolve(sketch);
            profile = qCreatedBy(sprocketId + "profile", EntityType.FACE);
        }
        else
        {
            profile = sketchSprocketProfile(context, sprocketId + "profile", sprocket.plane, sprocket.chainType, sprocket.teeth, profileOffset);
        }
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

/**
 * `sprocket` with the library sprocket it's built as (`product`, its vendor's and bore's first for its chain, of its
 * teeth), or `missing` if its vendor has none.
 */
function withLibraryProduct(definition is map, sprocket is map) returns map
{
    const choice = getLookupTable(sprocketProductTable, definition.sprocketProduct);
    const product = findProduct(SPROCKET_PRODUCTS[choice.vendor][choice.bore], function(product)
        {
            return sameSprockets(product.chainType, sprocket.chainType) && product.teeth == sprocket.teeth;
        }, undefined);
    if (product == undefined)
    {
        sprocket.missing = true;
    }
    else
    {
        sprocket.product = product;
    }
    return sprocket;
}

/**
 * A library sprocket's stand-in (see `addStandIn`), as wide as its teeth, with its sprocket attributes, on its body and
 * its mate connectors, so a chain goes around it as around a sprocket.
 */
function addSprocketStandIn(context is Context, id is Id, sprocket is map) returns Query
{
    const product = sprocket.product;
    const standIn = addStandIn(context, id + "standIn", sprocket.plane, getSprocketRadius(getChainInfo(sprocket.chainType).pitch, sprocket.teeth),
        getSprocketToothWidth(sprocket.chainType), product.boreDiameter, AutoAssemblyKind.SPROCKET, product as Product, {
                "chainType" : sprocket.chainType,
                "teeth" : sprocket.teeth
            });
    setAttribute(context, {
                "entities" : standIn.body,
                "name" : SPROCKET_ATTRIBUTE,
                "attribute" : sprocketAttribute(sprocket, "body")
            });
    setAttribute(context, {
                "entities" : standIn.connectors,
                "name" : SPROCKET_ATTRIBUTE,
                "attribute" : sprocketAttribute(sprocket, "standInMateConnector")
            });
    return standIn.body;
}

/**
 * Errors if a sprocket's vendor has none for its chain with its teeth, with the stand-ins of those (each as one would
 * be), and the teeth it has.
 */
function reportMissingProducts(definition is map, sprockets is array, bodies is array)
{
    var missing = [];
    for (var i, sprocket in sprockets)
    {
        if (sprocket.missing != undefined)
        {
            missing = append(missing, bodies[i]);
        }
    }
    if (missing == [])
    {
        return;
    }
    const sprocket = sprockets[indexOf(mapArray(sprockets, function(sprocket) { return sprocket.missing != undefined; }), true)];
    const path = definition.sprocketProduct;
    const choice = getLookupTable(sprocketProductTable, path);
    const chainName = getChainTypeName(sprocket.chainType);
    var teeth = [];
    for (var product in SPROCKET_PRODUCTS[choice.vendor][choice.bore])
    {
        if (sameSprockets(product.chainType, sprocket.chainType))
        {
            teeth = append(teeth, product.teeth ~ "T");
        }
    }
    const what = path.vendor ~ " has no " ~ sprocket.teeth ~ "T " ~ chainName ~ " sprocket with a " ~ path.bore ~ " bore";
    const has = teeth == [] ? "it has no " ~ chainName ~ " sprockets with that bore." : "its " ~ chainName ~ " ones have " ~ join(teeth, ", ") ~ ".";
    const parameters = definition.creationMethod == SprocketCreationMethod.MANUAL ? ["sprocketProduct", "teeth"] : ["sprocketProduct"];
    throw regenError(what ~ ": " ~ has, parameters, qUnion(missing));
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
