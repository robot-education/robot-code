FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
// The ribs: chains, hubs and spokes, and edge ribs. Also exports EdgeRibEnd, a parameter type
export import(path : "ec576ccf1c20e5355392c3a6", version : "cb8cfff00100795f4d5be726");
QuickRibIcon::import(path : "f9fda209a03cf7514da2a818", version : "ec13dad85d83811c1b909de6");

/**
 * Sketches ribs (lines, and circles) between points (circles' and arcs' centers, vertices, and mate connectors), and
 * from points to edges, quickly: for Robot lighten's Ribs to use. Chains run through their points in order (closed or
 * not; two points make one rib); hubs have spokes to their hub, and a circle around it if asked; edge ribs run from
 * points to an edge, meeting it square, or where a manipulator puts them. See `quickRibs.fs`.
 */
annotation { "Feature Type Name" : "Quick rib",
        "Feature Type Description" : "Sketch ribs between circles and points, as chains or hubs and spokes, or to edges, for Robot lighten." ~ CREDIT,
        "Editing Logic Function" : "quickRibEditLogic",
        "Manipulator Change Function" : "quickRibManipulatorChange",
        "Icon" : QuickRibIcon::BLOB_DATA
    }
export const quickRib = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Face to use", "MaxNumberOfPicks" : 1, "Filter" : EntityType.FACE && GeometryType.PLANE && SketchObject.NO,
                    "Description" : "Where the ribs are sketched (points are projected onto it). Filled in from what's chosen first." }
        definition.face is Query;

        annotation { "Group Name" : "Chains", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Chains", "Item name" : "chain", "Item label template" : "#chainPoints", "Driven query" : "chainPoints",
                        "UIHint" : ["FOCUS_INNER_QUERY"] }
            definition.chains is array;

            for (var chain in definition.chains)
            {
                annotation { "Name" : "Points",
                            "Filter" : EntityType.VERTEX || (EntityType.EDGE && GeometryType.CIRCLE) || (EntityType.EDGE && GeometryType.ARC) || BodyType.MATE_CONNECTOR }
                chain.chainPoints is Query;

                annotation { "Name" : "Closed" }
                chain.closed is boolean;
            }
        }

        annotation { "Group Name" : "Hub and spokes", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Hub and spokes", "Item name" : "hub", "Item label template" : "#hub", "Driven query" : "hub",
                        "UIHint" : ["FOCUS_INNER_QUERY"] }
            definition.hubs is array;

            for (var hub in definition.hubs)
            {
                annotation { "Name" : "Hub", "MaxNumberOfPicks" : 1,
                            "Filter" : EntityType.VERTEX || (EntityType.EDGE && GeometryType.CIRCLE) || (EntityType.EDGE && GeometryType.ARC) || BodyType.MATE_CONNECTOR }
                hub.hub is Query;

                annotation { "Name" : "Spokes",
                            "Filter" : EntityType.VERTEX || (EntityType.EDGE && GeometryType.CIRCLE) || (EntityType.EDGE && GeometryType.ARC) || BodyType.MATE_CONNECTOR }
                hub.spokes is Query;

                annotation { "Name" : "Add circle", "Description" : "Add a circle through the first selected spoke." }
                hub.addCircle is boolean;
            }
        }

        annotation { "Group Name" : "Edge ribs", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Edge ribs", "Item name" : "edge rib", "Item label template" : "#ribEnd", "UIHint" : ["FOCUS_INNER_QUERY"] }
            definition.edgeRibs is array;

            for (var edgeRib in definition.edgeRibs)
            {
                annotation { "Name" : "End", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
                edgeRib.ribEnd is EdgeRibEnd;

                annotation { "Name" : "Edge", "MaxNumberOfPicks" : 1, "Filter" : EntityType.EDGE }
                edgeRib.ribEdge is Query;

                annotation { "Name" : "Points",
                            "Filter" : EntityType.VERTEX || (EntityType.EDGE && GeometryType.CIRCLE) || (EntityType.EDGE && GeometryType.ARC) || BodyType.MATE_CONNECTOR }
                edgeRib.ribPoints is Query;

                if (edgeRib.ribEnd == EdgeRibEnd.MANIPULATOR)
                {
                    annotation { "Name" : "Location" }
                    isReal(edgeRib.edgeLocation, EDGE_RIB_LOCATION_BOUNDS);
                }
            }
        }

        annotation { "Name" : "Clear invalid selections" }
        isButton(definition.clearInvalid);
    }
    {
        if (isQueryEmpty(context, definition.face))
        {
            throw regenError("Select a face to use.", ["face"]);
        }
        const plane = evPlane(context, { "face" : definition.face });
        const ribs = sketchQuickRibs(context, id + "ribSketch", definition, plane);
        addEdgeRibManipulators(context, id, definition);
        if (isQueryEmpty(context, ribs.edges))
        {
            throw regenError("Add chains, hubs, or edge ribs to sketch ribs.", ["chains", "hubs", "edgeRibs"]);
        }
        if (ribs.skipped > 0)
        {
            reportFeatureWarning(context, id, "Some quick ribs are skipped because they're incomplete.", ["chains", "hubs", "edgeRibs"]);
        }
    });

/**
 * Dragging an edge rib's end sets its Location (`edgeRibManipulatorChange`).
 */
export function quickRibManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return edgeRibManipulatorChange(context, definition, newManipulators);
}

/**
 * When Clear invalid selections is clicked, drops invalid selections (`clearInvalidSelections`). Fills in Face to use,
 * while it's empty and hasn't been set: the face under the first selection (`firstFaceUnder`).
 */
export function quickRibEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query, clickedButton is string) returns map
{
    if (clickedButton == "clearInvalid")
    {
        definition = clearInvalidSelections(context, definition);
    }
    if (!(specifiedParameters.face ?? false) && isQueryEmpty(context, definition.face))
    {
        const face = firstFaceUnder(context, definition, hiddenBodies);
        if (face != undefined)
        {
            definition.face = face;
        }
    }
    return definition;
}
