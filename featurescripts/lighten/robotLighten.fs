FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
// ModifyFilletType, for opModifyFillet (common.fs doesn't export it)
import(path : "onshape/std/modifyFillet.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
// Ribs it sketches itself, from Chains, Hub and spokes, and Edge ribs
export import(path : "ec576ccf1c20e5355392c3a6", version : "d3a4f10647d95d5cecb59295");
RobotLightenIcon::import(path : "bfffc466263212064267fd69", version : "f76010d67ed6821a73cbf7e0");

const WALL_BOUNDS = { (meter) : [1e-5, 0.003175, 500], (inch) : 0.125, (millimeter) : 3 } as LengthBoundSpec;
const RIB_BOUNDS = { (meter) : [1e-5, 0.003175, 500], (inch) : 0.125, (millimeter) : 3 } as LengthBoundSpec;
const WALL_OVERRIDE_BOUNDS = { (meter) : [1e-5, 0.0015875, 500], (inch) : 0.0625, (millimeter) : 1.5 } as LengthBoundSpec;
const RIB_OVERRIDE_BOUNDS = { (meter) : [1e-5, 0.00635, 500], (inch) : 0.25, (millimeter) : 6 } as LengthBoundSpec;
const DEPTH_BOUNDS = { (meter) : [1e-5, 0.003175, 500], (inch) : 0.125, (millimeter) : 3 } as LengthBoundSpec;
// A 1/8 in. router bit's
const FILLET_RADIUS_BOUNDS = { (meter) : [1e-5, 0.0015875, 500], (inch) : 0.0625, (millimeter) : 1.5 } as LengthBoundSpec;

/**
 * How deep pockets go into the part.
 */
export enum LightenEndType
{
    annotation { "Name" : "Through all" }
    THROUGH_ALL,
    annotation { "Name" : "Blind" }
    BLIND
}

predicate isBlind(definition is map)
{
    definition.endType == LightenEndType.BLIND;
}

/**
 * Lightens a part with pockets: everything within the extrude of the face to lighten (into its part, through it or to
 * a depth) is cut away, but for walls along the face's edges (the part's sides and holes) and ribs, with the pockets'
 * corners rounded as a router bit leaves them. Ribs run along the selected sketch edges, and between points it's given
 * (see `quickRibs.fs`): chains, hubs and spokes, and from points to edges.
 */
annotation { "Feature Type Name" : "Robot lighten",
        "Feature Type Description" : "Lighten a part with pockets, leaving walls around its edges and holes, and ribs along a sketch or between holes." ~ CREDIT,
        "Manipulator Change Function" : "robotLightenManipulatorChange",
        "Editing Logic Function" : "robotLightenEditLogic",
        "Icon" : RobotLightenIcon::BLOB_DATA
    }
export const robotLighten = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Group Name" : "Selections", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Face to lighten", "MaxNumberOfPicks" : 1,
                        "Filter" : EntityType.FACE && GeometryType.PLANE && BodyType.SOLID && SketchObject.NO && ModifiableEntityOnly.YES }
            definition.face is Query;

            annotation { "Name" : "Ignore faces" }
            definition.ignoreFaces is boolean;

            annotation { "Group Name" : "Ignore faces", "Driving Parameter" : "ignoreFaces", "Collapsed By Default" : false }
            {
                if (definition.ignoreFaces)
                {
                    annotation { "Name" : "Faces to ignore", "UIHint" : UIHint.INITIAL_FOCUS,
                                "Filter" : EntityType.FACE && ((BodyType.SOLID && ModifiableEntityOnly.YES) || (SketchObject.YES && ConstructionObject.NO)) }
                    definition.ignoredFaces is Query;
                }
            }

            annotation { "Name" : "Ribs to use", "Filter" : EntityType.EDGE && SketchObject.YES }
            definition.ribEdges is Query;

            annotation { "Name" : "Exclude construction lines", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.excludeConstruction is boolean;
        }

        // Ribs it sketches itself (see `quickRibs.fs`)
        annotation { "Group Name" : "Quick ribs", "Collapsed By Default" : false }
        {
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

        annotation { "Name" : "Wall thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.wallThickness, WALL_BOUNDS);

        annotation { "Name" : "Rib thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.ribThickness, RIB_BOUNDS);

        annotation { "Name" : "Fillet corners", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.filletCorners is boolean;

        if (definition.filletCorners)
        {
            annotation { "Name" : "Fillet radius", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.filletRadius, FILLET_RADIUS_BOUNDS);
        }

        annotation { "Name" : "Override rib thickness" }
        definition.overrideRibThickness is boolean;

        annotation { "Group Name" : "Override rib thickness", "Driving Parameter" : "overrideRibThickness", "Collapsed By Default" : false }
        {
            if (definition.overrideRibThickness)
            {
                // Later overrides take precedence over earlier ones (and all of them over Rib thickness)
                annotation { "Name" : "Rib overrides", "Item name" : "override", "Item label template" : "#overrideThickness ribs" }
                definition.ribOverrides is array;

                for (var ribOverride in definition.ribOverrides)
                {
                    annotation { "Name" : "Ribs", "Filter" : EntityType.EDGE && SketchObject.YES }
                    ribOverride.overrideEdges is Query;

                    annotation { "Name" : "Rib thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(ribOverride.overrideThickness, RIB_OVERRIDE_BOUNDS);
                }
            }
        }

        annotation { "Name" : "Override wall thickness" }
        definition.overrideWallThickness is boolean;

        annotation { "Group Name" : "Override wall thickness", "Driving Parameter" : "overrideWallThickness", "Collapsed By Default" : false }
        {
            if (definition.overrideWallThickness)
            {
                // Later overrides take precedence over earlier ones (and all of them over Wall thickness and Faces to ignore)
                annotation { "Name" : "Wall overrides", "Item name" : "override", "Item label template" : "#overrideWall walls" }
                definition.wallOverrides is array;

                for (var wallOverride in definition.wallOverrides)
                {
                    annotation { "Name" : "Faces", "Filter" : EntityType.FACE && BodyType.SOLID && SketchObject.NO && ModifiableEntityOnly.YES }
                    wallOverride.overrideFaces is Query;

                    annotation { "Name" : "Wall thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    isLength(wallOverride.overrideWall, WALL_OVERRIDE_BOUNDS);
                }
            }
        }

        annotation { "Name" : "End type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
        definition.endType is LightenEndType;

        if (isBlind(definition))
        {
            annotation { "Name" : "Depth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.depth, DEPTH_BOUNDS);
        }
    }
    {
        const face = getFace(context, definition);
        // Its normal points out of its part, so pockets go against it
        const facePlane = evPlane(context, { "face" : face });
        const part = qOwnerBody(face);
        const ignoredFaces = getIgnoredFaces(context, definition);
        const wallOverrides = getWallOverrides(context, definition, face);
        // Quick ribs, sketched on the face (its sketch is deleted with the rest)
        const quickRibs = sketchQuickRibs(context, id + "quickRibs", definition, facePlane);
        addEdgeRibManipulators(context, id, definition);
        const ribGroups = getRibGroups(context, definition, quickRibs.edges);
        const ribEdges = qUnion(mapArray(ribGroups, group => group.edges));
        verifyParallel(context, face, facePlane, ribPlane(context, ribEdges, definition), definition);
        // To round the pockets' corners, they're made with walls and ribs this much thicker, then grown back by it (see
        // `roundPockets`)
        const radius = definition.filletCorners ? definition.filletRadius : 0 * meter;

        // The edges the face shares with the part's faces to ignore (but those with walls overridden), and with each
        // override's faces, which the pocket's extrude sweeps into its sides along them
        const overriddenFaces = qUnion(mapArray(wallOverrides, wallOverride => wallOverride.faces));
        const ignoredEdges = qIntersection([qLoopEdges(face), qLoopEdges(qSubtraction(qSketchFilter(ignoredFaces, SketchObject.NO), overriddenFaces))]);
        const ignoredSides = startTracking(context, ignoredEdges);
        var overriddenSides = [];
        for (var wallOverride in wallOverrides)
        {
            overriddenSides = append(overriddenSides, mergeMaps(wallOverride, { "sides" : startTracking(context, wallOverride.edges) }));
        }

        if (quickRibs.skipped > 0)
        {
            reportFeatureWarning(context, id, "Some quick ribs are skipped because they're incomplete.", ["chains", "hubs", "edgeRibs"]);
        }

        // The pocket: the face's extrude into its part
        const depth = pocketDepth(context, definition, part, facePlane);
        if (isBlind(definition))
        {
            addDepthManipulator(context, id, face, facePlane, depth);
        }
        try silent
        {
            opExtrude(context, id + "pocket", {
                        "entities" : face,
                        "direction" : -facePlane.normal,
                        "endBound" : BoundingType.BLIND,
                        "endDepth" : depth
                    });
        }
        catch
        {
            processSubfeatureStatus(context, id, { "subfeatureId" : id + "pocket", "propagateErrorDisplay" : true });
            throw regenError("Failed to create pockets.", ["face", "depth"], face);
        }
        // Its ends, on the face and its far side: every other face is a side
        const ends = qCapEntity(id + "pocket", CapType.EITHER, EntityType.FACE);

        // Sketch regions to ignore are left solid: cut from the pocket, so walls go around them too
        excludeRegions(context, id, facePlane, qCreatedBy(id + "pocket", EntityType.BODY), qSketchFilter(ignoredFaces, SketchObject.YES));

        // The walls: the pocket (and the pieces regions to ignore split it into), inset by them, but along the part's
        // faces to ignore
        const extruded = qUnion([qCreatedBy(id + "pocket", EntityType.BODY), qCreatedBy(id + "excludeRegions" + "cut", EntityType.BODY)]);
        if (isQueryEmpty(context, extruded))
        {
            throw regenError("The regions to ignore cover the whole face.", ["ignoredFaces"], qUnion([face, qSketchFilter(ignoredFaces, SketchObject.YES)]));
        }
        const pocketFaces = qOwnedByBody(extruded, EntityType.FACE);
        // The pockets' ends from here on, tracked from the extrude's caps (see `buildWalls`)
        const pocketEnds = buildWalls(context, id, definition, extruded, ends, qIntersection([pocketFaces, ignoredSides]),
            mapArray(overriddenSides, wallOverride => mergeMaps(wallOverride, { "sides" : qIntersection([pocketFaces, wallOverride.sides]) })), radius);

        // The ribs, cut from what's left: what the walls enclose
        const inset = qCreatedBy(id + "walls", EntityType.BODY);
        const ribs = buildRibs(context, id, bandExtent(context, facePlane, qUnion([inset, ribEdges])), ribGroups, radius);
        try silent
        {
            opBoolean(context, id + "cutRibs", {
                        "targets" : inset,
                        "tools" : ribs,
                        "operationType" : BooleanOperationType.SUBTRACTION
                    });
        }
        catch
        {
            processSubfeatureStatus(context, id, { "subfeatureId" : id + "cutRibs", "propagateErrorDisplay" : true });
            // A failed boolean changes nothing: the ribs are still there to show
            throw regenError("Failed to cut the ribs from the pockets.", ribParameters(definition), ribs);
        }
        // The pockets left between them, which the walls and ribs split into pieces
        const pockets = qUnion([inset, qCreatedBy(id + "cutRibs", EntityType.BODY)]);
        if (isQueryEmpty(context, pockets))
        {
            // The cut used up the walls' insides and the ribs, so the selections are what's shown
            throw regenError("There's no room for pockets between the walls and ribs.",
                concatenateArrays([["wallThickness", "filletRadius"], overridesWalls(definition) ? ["wallOverrides"] : [], ribParameters(definition)]),
                qUnion([face, ribEdges]));
        }

        // Pockets too small to be worth cutting are left solid
        const small = smallPockets(context, pockets, pocketEnds, radius, definition.wallThickness);
        if (size(small) == size(evaluateQuery(context, pockets)))
        {
            throw regenError("The pockets are all too small to cut.", ["wallThickness", "filletRadius"], qUnion(small));
        }
        if (small != [])
        {
            opDeleteBodies(context, id + "dropSmall", { "entities" : qUnion(small) });
        }

        if (definition.filletCorners)
        {
            roundPockets(context, id, pockets, pocketEnds, radius);
        }

        const volume = evVolume(context, { "entities" : part });
        try silent
        {
            opBoolean(context, id + "cut", {
                        "targets" : part,
                        "tools" : pockets,
                        "operationType" : BooleanOperationType.SUBTRACTION
                    });
        }
        catch
        {
            processSubfeatureStatus(context, id, { "subfeatureId" : id + "cut", "propagateErrorDisplay" : true });
            throw regenError("Failed to cut the pockets from the part.", ["face"], pockets);
        }
        // Pieces of ribs which touch no wall or other rib are cut free, as parts of their own
        const loose = qCreatedBy(id + "cut", EntityType.BODY);
        if (!isQueryEmpty(context, loose))
        {
            reportFeatureWarning(context, id, "Some ribs touch no wall or other rib, so they're left as loose parts.", ribParameters(definition));
            setErrorEntities(context, id, { "entities" : loose });
        }
        const lightened = 1 - evVolume(context, { "entities" : part }) / volume;
        reportFeatureInfo(context, id, "Weight reduced by " ~ roundToPrecision(lightened * 100, 1) ~ "%.");

        // The pockets are used up by the cut
        opDeleteBodies(context, id + "cleanup", {
                    "entities" : qUnion([qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES), qCreatedBy(id + "ribs", EntityType.BODY)])
                });
    });

/**
 * The face to lighten.
 */
function getFace(context is Context, definition is map) returns Query
{
    const face = qEntityFilter(definition.face, EntityType.FACE);
    if (isQueryEmpty(context, face))
    {
        throw regenError("Select the face to lighten.", ["face"]);
    }
    return face;
}

/**
 * The faces to ignore: none, unless Ignore faces is checked, when there must be some.
 */
function getIgnoredFaces(context is Context, definition is map) returns Query
{
    if (!definition.ignoreFaces)
    {
        return qNothing();
    }
    const faces = qEntityFilter(definition.ignoredFaces, EntityType.FACE);
    if (isQueryEmpty(context, faces))
    {
        throw regenError("Select faces to ignore.", ["ignoredFaces"]);
    }
    return faces;
}

/**
 * The wall overrides, with Override wall thickness: each of Wall overrides, its faces less those later ones have (so
 * later ones take precedence), and the edges `face` (the face to lighten) shares with them, which the pocket's sides
 * along them are swept from. Each must have faces, and border the face to lighten; those left with no faces are left
 * out.
 *
 * @returns {array} : Each override, as a map of `faces`, `edges`, `thickness`, and `parameters` (its parameters, for
 *          errors about it).
 */
function getWallOverrides(context is Context, definition is map, face is Query) returns array
{
    if (!overridesWalls(definition))
    {
        return [];
    }
    const overrides = wallOverrideGroups(definition);
    var found = [];
    for (var wallOverride in overrides)
    {
        if (isQueryEmpty(context, wallOverride.selected))
        {
            throw regenError("Select faces to override.", [wallOverride.parameters[0]]);
        }
        if (isQueryEmpty(context, qIntersection([qLoopEdges(face), qLoopEdges(wallOverride.selected)])))
        {
            throw regenError("Face overrides must be adjacent to the lightened face.", [wallOverride.parameters[0]],
                wallOverride.selected);
        }
        if (!isQueryEmpty(context, wallOverride.faces))
        {
            found = append(found, mergeMaps(wallOverride, { "edges" : qIntersection([qLoopEdges(face), qLoopEdges(wallOverride.faces)]) }));
        }
    }
    return found;
}

/**
 * `getWallOverrides`' overrides, before checking them: each with its `selected` faces, and its `faces`, less those later
 * ones select.
 */
function wallOverrideGroups(definition is map) returns array
{
    var overrides = [];
    for (var i, wallOverride in definition.wallOverrides)
    {
        overrides = append(overrides, { "selected" : qEntityFilter(wallOverride.overrideFaces, EntityType.FACE),
                    "thickness" : wallOverride.overrideWall,
                    "parameters" : [faultyArrayParameterId("wallOverrides", i, "overrideFaces"), faultyArrayParameterId("wallOverrides", i, "overrideWall")] });
    }
    for (var i, wallOverride in overrides)
    {
        const later = mapArray(subArray(overrides, i + 1), other => other.selected);
        overrides[i].faces = qSubtraction(wallOverride.selected, qUnion(later));
    }
    return overrides;
}

/**
 * Throws unless the face to lighten is parallel to the ribs' sketches, so the ribs go straight into it.
 */
function verifyParallel(context is Context, face is Query, facePlane is Plane, ribs is Plane, definition is map)
{
    if (!parallelVectors(facePlane.normal, ribs.normal))
    {
        throw regenError("The face to lighten must be parallel to the ribs.", concatenateArrays([["face"], ribParameters(definition)]), face);
    }
}

/**
 * How deep the pocket goes: Depth, or for Through all, to the far side of the part.
 */
function pocketDepth(context is Context, definition is map, part is Query, facePlane is Plane) returns ValueWithUnits
{
    if (isBlind(definition))
    {
        return definition.depth;
    }
    // To the part's far side, exactly: pockets ending flush with it cut through, as std's booleans treat faces within
    // their tolerance as the same
    return -evBox3d(context, { "topology" : part, "cSys" : coordSystem(facePlane), "tight" : true }).minCorner[2];
}

const DEPTH_MANIPULATOR = "depthManipulator";

function addDepthManipulator(context is Context, id is Id, face is Query, facePlane is Plane, depth is ValueWithUnits)
{
    addManipulators(context, id, {
                (DEPTH_MANIPULATOR) : linearManipulator({
                        "base" : project(facePlane, evApproximateCentroid(context, { "entities" : face })),
                        "direction" : -facePlane.normal,
                        "offset" : depth,
                        "primaryParameterId" : "depth"
                    })
            });
}

/**
 * Dragging the depth manipulator sets Depth; dragging an edge rib's end sets its Location (`edgeRibManipulatorChange`).
 */
export function robotLightenManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    const manipulator = newManipulators[DEPTH_MANIPULATOR];
    if (manipulator != undefined)
    {
        definition.depth = abs(manipulator.offset);
    }
    return edgeRibManipulatorChange(context, definition, newManipulators);
}

/**
 * When Clear invalid selections is clicked, drops the quick ribs' invalid selections (`clearInvalidSelections`). Fills
 * in the face to lighten, unless it's been set: the one face in the first rib's sketch plane which the ribs are over,
 * if there's just one; or without ribs to use, while it's empty, the face under the first quick rib selection
 * (`firstFaceUnder`).
 */
export function robotLightenEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query, clickedButton is string) returns map
{
    if (clickedButton == "clearInvalid")
    {
        definition = clearInvalidSelections(context, definition);
    }
    if (specifiedParameters.face ?? false)
    {
        return definition;
    }
    // A guard: editing logic mustn't throw while the dialog's being filled in, so without ribs yet, it's left
    var plane;
    var edges;
    try silent
    {
        edges = qUnion(mapArray(getRibGroups(context, definition, qNothing()), group => group.edges));
        plane = ribPlane(context, edges, definition);
    }
    if (plane != undefined)
    {
        const faces = facesUnder(context, plane, edges, hiddenBodies);
        if (evaluateQueryCount(context, faces) == 1)
        {
            definition.face = faces;
        }
    }
    else if (isQueryEmpty(context, definition.face))
    {
        const face = firstFaceUnder(context, definition, hiddenBodies);
        if (face != undefined)
        {
            definition.face = face;
        }
    }
    return definition;
}

/**
 * The walls, as the previous Onshape Lighten feature and Vent make them: `pockets`, inset by Wall thickness and
 * `radius` (the walls are thicker by the radius their corners are rounded to later), under `featureId + "walls"`; but
 * at `ends` (their caps) and `ignoredSides`, which stay where they are, and at each of `overrides`' `sides`, inset by
 * its `thickness` instead. In steps:
 *
 * 1. `ends` and `ignoredSides` are moved out by the inset, so hollowing brings them back.
 * 2. Each override's sides are moved out by Wall thickness less its thickness (in, for a thicker wall).
 * 3. Concave edges get a hair fillet (std's boolean tolerance), which hollowing grows into the walls' rounded inside
 *    corners.
 * 4. The pockets are hollowed by the inset (`opShell`), which moves every face in at once, holes of any size and their
 *    corners too, and what's inside is kept (`opEnclose`, a pocket at a time).
 * 5. The inside corners' rounds are set to exactly the inset (`opModifyFillet`, as Vent does); if they can't be, they're
 *    a hair bigger, and it's said as info.
 *
 * A failed step fails the feature, with its own error display, highlighting what sets it.
 *
 * @returns {Query} : The inset pockets' ends: `ends`, tracked from after step 1 through hollowing, and the cuts and
 *          rounding after it. Tracking from before step 1 would take in the pocket's extrude (the last operation
 *          before it), whose sides are derived from its caps too.
 */
function buildWalls(context is Context, featureId is Id, definition is map, pockets is Query, ends is Query, ignoredSides is Query,
    overrides is array, radius is ValueWithUnits) returns Query
{
    const id = featureId + "walls";
    const distance = definition.wallThickness + radius;
    const parameters = concatenateArrays([["wallThickness", "filletRadius"], overridesWalls(definition) ? ["wallOverrides"] : []]);
    const faces = qOwnedByBody(pockets, EntityType.FACE);

    // 1. Ends and ignored sides out
    try silent
    {
        opOffsetFace(context, id + "extend", {
                    "moveFaces" : qIntersection([faces, qUnion([ends, ignoredSides])]),
                    "offsetDistance" : distance
                });
    }
    catch
    {
        processSubfeatureStatus(context, featureId, { "subfeatureId" : id + "extend", "propagateErrorDisplay" : true });
        throw regenError("Failed to create the walls along the faces shown.", parameters, qIntersection([faces, qUnion([ends, ignoredSides])]));
    }
    const trackedEnds = startTracking(context, qIntersection([faces, ends]));

    // 2. Overridden sides
    for (var i, wallOverride in overrides)
    {
        const offset = definition.wallThickness - wallOverride.thickness;
        const sides = qIntersection([faces, wallOverride.sides]);
        if (tolerantEqualsZero(offset) || isQueryEmpty(context, sides))
        {
            continue;
        }
        const overrideId = id + "override" + unstableIdComponent(i);
        try silent
        {
            opOffsetFace(context, overrideId, {
                        "moveFaces" : sides,
                        "offsetDistance" : offset
                    });
        }
        catch
        {
            processSubfeatureStatus(context, featureId, { "subfeatureId" : overrideId, "propagateErrorDisplay" : true });
            throw regenError("Selected wall overrides are invalid.", wallOverride.parameters, wallOverride.faces);
        }
    }

    // 3. Concave edges, a hair
    const concave = qUnion(filter(evaluateQuery(context, qOwnedByBody(pockets, EntityType.EDGE)), function(edge)
            {
                return evEdgeConvexity(context, { "edge" : edge }) == EdgeConvexityType.CONCAVE;
            }));
    if (!isQueryEmpty(context, concave))
    {
        try silent
        {
            opFillet(context, id + "roundConcave", {
                        "entities" : concave,
                        "radius" : TOLERANCE.booleanDefaultTolerance * meter
                    });
        }
        catch
        {
            processSubfeatureStatus(context, featureId, { "subfeatureId" : id + "roundConcave", "propagateErrorDisplay" : true });
            throw regenError("Failed to round the walls' inside corners.", parameters, concave);
        }
    }
    // Hollowing changes them, so they're tracked through it, to be set to exactly the inset after
    const rounds = startTracking(context, qCreatedBy(id + "roundConcave", EntityType.FACE));

    // 4. Hollowed, and what's inside kept
    try silent
    {
        opShell(context, id + "shell", {
                    "entities" : pockets,
                    "thickness" : -distance
                });
    }
    catch
    {
        processSubfeatureStatus(context, featureId, { "subfeatureId" : id + "shell", "propagateErrorDisplay" : true });
        throw regenError("Failed to inset the pockets by the wall thickness.", parameters, pockets);
    }
    for (var i, pocket in evaluateQuery(context, pockets))
    {
        const encloseId = id + "enclose" + unstableIdComponent(i);
        try silent
        {
            opEnclose(context, encloseId, { "entities" : pocket });
        }
        catch
        {
            processSubfeatureStatus(context, featureId, { "subfeatureId" : encloseId, "propagateErrorDisplay" : true });
            throw regenError("Failed to inset the pocket shown.", parameters, pocket);
        }
    }
    // A pocket narrower than twice the inset encloses nothing; none left is an error, not an empty result
    if (isQueryEmpty(context, qCreatedBy(id + "enclose", EntityType.BODY)))
    {
        throw regenError("There's no room for pockets inside the walls.", parameters, pockets);
    }

    // 5. Inside corners, exactly the inset
    const insideCorners = qIntersection([rounds, qCreatedBy(id + "enclose", EntityType.FACE)]);
    if (!isQueryEmpty(context, insideCorners))
    {
        try silent
        {
            opModifyFillet(context, id + "exactCorners", {
                        "faces" : insideCorners,
                        "modifyFilletType" : ModifyFilletType.CHANGE_RADIUS,
                        "reFillet" : false,
                        "radius" : distance
                    });
        }
        catch
        {
            reportFeatureInfo(context, featureId, "The walls' inside corners are rounded 0.01 mm larger than the wall thickness.");
        }
    }
    opDeleteBodies(context, id + "deleteShells", { "entities" : pockets });
    return trackedEnds;
}

/**
 * The ribs, in groups of one thickness: Ribs to use and the quick ribs (`quickRibEdges`), at Rib thickness, then with
 * Override rib thickness, each of Rib overrides; each less what a later one has, so later ones take precedence (a
 * whole sketch, then a few of its ribs at another thickness). An override's ribs needn't be among Ribs to use. Each
 * group's edges are its sketch edges (and their construction edges, unless they're excluded); groups left with none
 * are left out.
 *
 * @returns {array} : Each group, as a map of `edges` (a query), `thickness`, and `parameters` (its parameters, for
 *          errors about its ribs).
 */
function getRibGroups(context is Context, definition is map, quickRibEdges is Query) returns array
{
    var groups = ribGroups(definition);
    // Overrides can't select them, so they're only ever at Rib thickness
    groups[0].selected = qUnion([groups[0].selected, quickRibEdges]);
    groups[0].edges = qUnion([groups[0].edges, quickRibEdges]);
    groups[0].parameters = concatenateArrays([groups[0].parameters, quickRibParameters(definition)]);
    if (isQueryEmpty(context, groups[0].selected))
    {
        throw regenError("Select ribs to use.", ["ribEdges"]);
    }
    var found = [];
    for (var i, group in groups)
    {
        if (i > 0 && isQueryEmpty(context, group.selected))
        {
            throw regenError("Select ribs to override.", [faultyArrayParameterId("ribOverrides", i - 1, "overrideEdges")]);
        }
        if (!isQueryEmpty(context, group.edges))
        {
            found = append(found, group);
        }
    }
    return found;
}

/**
 * `getRibGroups`'s groups, before checking them: each with its `selected` edges too (less construction edges, if
 * they're excluded), and its `edges`, less those later groups select.
 */
function ribGroups(definition is map) returns array
{
    var groups = [{ "edges" : definition.ribEdges, "thickness" : definition.ribThickness, "parameters" : ["ribEdges", "ribThickness"] }];
    if (overridesRibs(definition))
    {
        for (var i, ribOverride in definition.ribOverrides)
        {
            groups = append(groups, { "edges" : ribOverride.overrideEdges, "thickness" : ribOverride.overrideThickness,
                        "parameters" : [faultyArrayParameterId("ribOverrides", i, "overrideEdges"),
                                faultyArrayParameterId("ribOverrides", i, "overrideThickness")] });
        }
    }
    for (var i, group in groups)
    {
        var selected = qEntityFilter(group.edges, EntityType.EDGE);
        if (definition.excludeConstruction)
        {
            selected = qConstructionFilter(selected, ConstructionObject.NO);
        }
        groups[i].selected = selected;
    }
    for (var i, group in groups)
    {
        const later = mapArray(subArray(groups, i + 1), other => other.selected);
        groups[i].edges = qSubtraction(group.selected, qUnion(later));
    }
    return groups;
}

/**
 * The quick ribs' parameters which have any (Chains, Hub and spokes, and Edge ribs), for errors about them.
 */
function quickRibParameters(definition is map) returns array
{
    return filter(["chains", "hubs", "edgeRibs"], name => definition[name] != []);
}

/**
 * The parameters which set the ribs, for errors about them.
 */
function ribParameters(definition is map) returns array
{
    return concatenateArrays([["ribEdges", "ribThickness"], quickRibParameters(definition), overridesRibs(definition) ? ["ribOverrides"] : []]);
}

/**
 * Whether Rib overrides apply: with Override rib thickness.
 */
function overridesRibs(definition is map) returns boolean
{
    return definition.overrideRibThickness;
}

/**
 * Whether Wall overrides apply: with Override wall thickness.
 */
function overridesWalls(definition is map) returns boolean
{
    return definition.overrideWallThickness;
}

/**
 * The plane of the first rib's sketch. The ribs can be from any sketches, as long as they're parallel: each is
 * extruded through everything along their normal.
 */
function ribPlane(context is Context, edges is Query, definition is map) returns Plane
{
    const evaluated = evaluateQuery(context, edges);
    const plane = evOwnerSketchPlane(context, { "entity" : evaluated[0] });
    const skewed = filter(evaluated, function(edge)
        {
            return !parallelVectors(evOwnerSketchPlane(context, { "entity" : edge }).normal, plane.normal);
        });
    if (skewed != [])
    {
        throw regenError("The ribs must be in parallel sketches.", ribParameters(definition), qUnion(skewed));
    }
    return plane;
}

/**
 * Cuts `regions` (sketch regions), extruded along `plane`'s normal through `pockets`, from them, under `id`.
 */
function excludeRegions(context is Context, featureId is Id, plane is Plane, pockets is Query, regions is Query)
{
    if (isQueryEmpty(context, regions))
    {
        return;
    }
    const id = featureId + "excludeRegions";
    const extent = bandExtent(context, plane, qUnion([pockets, regions]));
    try silent
    {
        opExtrude(context, id + "extrude", {
                    "entities" : regions,
                    "direction" : plane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : extent.depth,
                    "startBound" : BoundingType.BLIND,
                    "startDepth" : extent.depth
                });
    }
    catch
    {
        processSubfeatureStatus(context, featureId, { "subfeatureId" : id + "extrude", "propagateErrorDisplay" : true });
        throw regenError("Failed to ignore the selected regions.", ["ignoredFaces"], regions);
    }
    try silent
    {
        opBoolean(context, id + "cut", {
                    "targets" : pockets,
                    "tools" : qCreatedBy(id + "extrude", EntityType.BODY),
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
    catch
    {
        processSubfeatureStatus(context, featureId, { "subfeatureId" : id + "cut", "propagateErrorDisplay" : true });
        throw regenError("Failed to ignore the selected regions.", ["ignoredFaces"], regions);
    }
}

/**
 * Where ribs (or regions to ignore) go, along `plane`'s normal: through `bounds` (the pockets, and the ribs' edges or
 * the regions), exactly (their bounding box, tight).
 *
 * @returns {{
 *      @field plane {Plane} : `plane`, moved halfway through them.
 *      @field depth {ValueWithUnits} : How far each goes each way from its edge or region: through all of them, from
 *              any one.
 *      @field halfDepth {ValueWithUnits} : How far each goes each way from `plane`.
 * }}
 */
function bandExtent(context is Context, plane is Plane, bounds is Query) returns map
{
    const boundingBox = evBox3d(context, { "topology" : bounds, "cSys" : coordSystem(plane), "tight" : true });
    const height = boundingBox.maxCorner[2] - boundingBox.minCorner[2];
    var middle = plane;
    middle.origin += plane.normal * (boundingBox.minCorner[2] + boundingBox.maxCorner[2]) / 2;
    return { "plane" : middle, "depth" : height, "halfDepth" : height / 2 };
}

/**
 * The ribs of `groups` (see `getRibGroups`), each half its group's thickness to each side (and `radius` more), through
 * `extent` (see `bandExtent`). Each is made on its own, as edges extruded together make one sheet, creased where they
 * meet, which can't be thickened. Each is its edge's sheet, thickened. An arc or circle whose radius is within std's
 * boolean tolerance of that half (or under it) can't be thickened toward its center. Its rib is a solid cylinder
 * around its center instead. A rib which fails highlights its edge, and its group's parameters.
 */
function buildRibs(context is Context, featureId is Id, extent is map, groups is array, radius is ValueWithUnits) returns Query
{
    const id = featureId + "ribs";
    for (var j, group in groups)
    {
        buildRibGroup(context, featureId, id + unstableIdComponent(j), extent, group.edges, group.thickness / 2 + radius, group.parameters);
    }
    return qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID);
}

/**
 * One group's ribs, `halfWidth` to each side (see `buildRibs`).
 */
function buildRibGroup(context is Context, featureId is Id, id is Id, extent is map, edges is Query, halfWidth is ValueWithUnits, parameters is array)
{
    // The circles given cylinders: a circle split into arcs needs just one
    var circles = [];
    for (var i, edge in evaluateQuery(context, edges))
    {
        const ribId = id + unstableIdComponent(i);
        setExternalDisambiguation(context, ribId, edge);
        const curve = evCurveDefinition(context, { "edge" : edge });
        // Thickening it toward its center would leave a hole under std's boolean tolerance, or none
        if (curve is Circle && curve.radius - halfWidth < TOLERANCE.booleanDefaultTolerance * meter)
        {
            if (any(circles, function(circle)
                    {
                        return tolerantEquals(circle.coordSystem.origin, curve.coordSystem.origin) && tolerantEquals(circle.radius, curve.radius);
                    }))
            {
                continue;
            }
            circles = append(circles, curve);
            const center = project(extent.plane, curve.coordSystem.origin);
            try silent
            {
                fCylinder(context, ribId + "cylinder", {
                            "bottomCenter" : center - extent.plane.normal * extent.halfDepth,
                            "topCenter" : center + extent.plane.normal * extent.halfDepth,
                            "radius" : curve.radius + halfWidth
                        });
            }
            catch
            {
                processSubfeatureStatus(context, featureId, { "subfeatureId" : ribId + "cylinder", "propagateErrorDisplay" : true });
                throw regenError("Failed to create rib.", parameters, edge);
            }
            continue;
        }

        try silent
        {
            opExtrude(context, ribId + "sheet", {
                        "entities" : edge,
                        "direction" : extent.plane.normal,
                        "endBound" : BoundingType.BLIND,
                        "endDepth" : extent.depth,
                        "startBound" : BoundingType.BLIND,
                        "startDepth" : extent.depth
                    });
        }
        catch
        {
            processSubfeatureStatus(context, featureId, { "subfeatureId" : ribId + "sheet", "propagateErrorDisplay" : true });
            throw regenError("Failed to create rib.", parameters, edge);
        }
        try silent
        {
            opThicken(context, ribId + "rib", {
                        "entities" : qCreatedBy(ribId + "sheet", EntityType.BODY),
                        "thickness1" : halfWidth,
                        "thickness2" : halfWidth
                    });
        }
        catch
        {
            processSubfeatureStatus(context, featureId, { "subfeatureId" : ribId + "rib", "propagateErrorDisplay" : true });
            throw regenError("Failed to create rib.", parameters, edge);
        }
    }
}

/**
 * Rounds the pockets' corners as a router bit of `radius` leaves them, as Vent (its `expandWithFillet`) and the previous
 * Onshape Lighten feature do: the walls and ribs were made `radius` thicker, so a pocket narrower than the bit is gone,
 * and the rest are grown back by `radius`. In steps:
 *
 * 1. Each pocket's corners get a hair fillet (std's boolean tolerance), which growing rounds to the bit's radius.
 * 2. If a pocket's fillet fails, its sides too narrow for both their corners' fillets (`narrowSides`) each get a full
 *    round across them instead (`opFullRoundFillet`), and its corners are filleted again. If a full round fails, or the
 *    fillet still does, that's a warning, with the operation's error display, and the pocket's left as it is: smaller,
 *    as its walls and ribs are thicker.
 * 3. The rounded pockets are grown back by `radius` (`opOffsetFace`).
 * 4. Their corners' rounds (tracked through the growth) are set to exactly `radius` (`opModifyFillet`); if they can't
 *    be, they're a hair bigger, and it's said as info. Full rounds are left as grown: a bit's two rounds with less than
 *    a hair between them are a round that wide, to within it.
 *
 * Only the first status reported shows, so a warning is reported before the failed operation's status is copied (for
 * its error display).
 */
function roundPockets(context is Context, featureId is Id, pockets is Query, ends is Query, radius is ValueWithUnits)
{
    const id = featureId + "round";
    var rounded = [];
    var filletIds = [];
    for (var i, pocket in evaluateQuery(context, pockets))
    {
        const pocketId = id + unstableIdComponent(i);
        setExternalDisambiguation(context, pocketId, pocket);

        // 1. Its corners, a hair
        if (hairFillet(context, pocketId + "fillet", pocketCorners(context, pocket, ends)))
        {
            filletIds = append(filletIds, pocketId + "fillet");
            rounded = append(rounded, pocket);
            continue;
        }

        // 2. Its narrow sides, a full round across each, then its corners again
        const narrowEnds = narrowSides(context, pocketId + "trial", pocket, ends);
        if (narrowEnds == [])
        {
            reportFeatureWarning(context, featureId, "Failed to fillet pocket.", ["filletRadius"]);
            processSubfeatureStatus(context, featureId, { "subfeatureId" : pocketId + "fillet", "propagateErrorDisplay" : true });
            setErrorEntities(context, featureId, { "entities" : pocketCorners(context, pocket, ends) });
            continue;
        }
        var fullRounds = true;
        for (var j, narrowEnd in narrowEnds)
        {
            const fullRoundId = pocketId + "fullRound" + unstableIdComponent(j);
            try silent
            {
                opFullRoundFillet(context, fullRoundId, {
                            "side1Face" : narrowEnd.sides[0],
                            "side2Face" : narrowEnd.sides[1],
                            "centerFaces" : narrowEnd.faces,
                            "tangentPropagation" : false
                        });
            }
            catch
            {
                reportFeatureWarning(context, featureId, "Failed to round a narrow pocket end.", ["filletRadius"]);
                processSubfeatureStatus(context, featureId, { "subfeatureId" : fullRoundId, "propagateErrorDisplay" : true });
                setErrorEntities(context, featureId, { "entities" : narrowEnd.faces });
                fullRounds = false;
            }
        }
        if (!fullRounds)
        {
            continue;
        }
        // A full round's edges are smooth, so they aren't corners
        if (!hairFillet(context, pocketId + "refillet", pocketCorners(context, pocket, ends)))
        {
            reportFeatureWarning(context, featureId, "Failed to fillet pocket.", ["filletRadius"]);
            processSubfeatureStatus(context, featureId, { "subfeatureId" : pocketId + "refillet", "propagateErrorDisplay" : true });
            setErrorEntities(context, featureId, { "entities" : pocketCorners(context, pocket, ends) });
            continue;
        }
        filletIds = append(filletIds, pocketId + "refillet");
        rounded = append(rounded, pocket);
    }
    if (rounded == [])
    {
        return;
    }

    // 3. Grown back by the radius, with the corners' rounds tracked through it
    const rounds = startTracking(context, qUnion(mapArray(filletIds, filletId => qCreatedBy(filletId, EntityType.FACE))));
    try silent
    {
        opOffsetFace(context, id + "grow", {
                    "moveFaces" : qSubtraction(qOwnedByBody(qUnion(rounded), EntityType.FACE), ends),
                    "offsetDistance" : radius
                });
    }
    catch
    {
        processSubfeatureStatus(context, featureId, { "subfeatureId" : id + "grow", "propagateErrorDisplay" : true });
        throw regenError("Failed to grow the pockets back by the fillet radius.", ["filletRadius"], qUnion(rounded));
    }

    // 4. The corners' rounds, exactly the radius
    if (!isQueryEmpty(context, rounds))
    {
        try silent
        {
            opModifyFillet(context, id + "exactCorners", {
                        "faces" : rounds,
                        "modifyFilletType" : ModifyFilletType.CHANGE_RADIUS,
                        "reFillet" : false,
                        "radius" : radius
                    });
        }
        catch
        {
            reportFeatureInfo(context, featureId, "The pockets' corners are rounded 0.01 mm larger than the fillet radius.");
        }
    }
}

/**
 * Fillets `corners` a hair (std's boolean tolerance), under `id`: whether it worked (or there are none to fillet).
 */
function hairFillet(context is Context, id is Id, corners is Query) returns boolean
{
    if (isQueryEmpty(context, corners))
    {
        return true;
    }
    try silent
    {
        opFillet(context, id, {
                    "entities" : corners,
                    "radius" : TOLERANCE.booleanDefaultTolerance * meter
                });
        return true;
    }
    return false;
}

/**
 * A pocket's narrow sides: those between two convex corners which can't both be filleted a hair. Each side's two
 * corners are tried alone (`hairFillet`), in a feature of their own under `id`, rolled back, so it's only done when
 * the pocket's fillet has failed. Narrow sides next to each other are one end, rounded between the sides either side
 * of them; an end without two (a pocket narrow all the way around) is left out.
 *
 * @returns {array} : Each end, as a map of its `faces`, and the two `sides` it's between.
 */
function narrowSides(context is Context, id is Id, pocket is Query, ends is Query) returns array
{
    const corners = pocketCorners(context, pocket, ends);
    var narrow = [];
    for (var i, face in evaluateQuery(context, qAdjacent(corners, AdjacencyType.EDGE, EntityType.FACE)))
    {
        const faceCorners = qIntersection([qLoopEdges(face), corners]);
        if (size(evaluateQuery(context, faceCorners)) != 2)
        {
            continue;
        }
        const trialId = id + unstableIdComponent(i);
        startFeature(context, trialId, {});
        const filleted = hairFillet(context, trialId + "fillet", faceCorners);
        abortFeature(context, trialId);
        if (!filleted)
        {
            narrow = append(narrow, face);
        }
    }

    // The faces next to `faces` across their corners
    const beside = function(faces is Query) returns Query
        {
            return qSubtraction(qAdjacent(qIntersection([qLoopEdges(faces), corners]), AdjacencyType.EDGE, EntityType.FACE), faces);
        };
    var found = [];
    while (narrow != [])
    {
        var run = [narrow[0]];
        narrow = subArray(narrow, 1);
        while (true)
        {
            const next = evaluateQuery(context, qIntersection([beside(qUnion(run)), qUnion(narrow)]));
            if (next == [])
            {
                break;
            }
            run = concatenateArrays([run, next]);
            narrow = filter(narrow, face => !isIn(face, next));
        }
        const sides = evaluateQuery(context, beside(qUnion(run)));
        if (size(sides) == 2)
        {
            found = append(found, { "faces" : qUnion(run), "sides" : sides });
        }
    }
    return found;
}

/**
 * The pockets (before they're grown back by `radius`) too small to be worth cutting: a heuristic. Those with less area,
 * as cut, than a circle twice as wide as the bit (`2 * radius`) or `wall`, whichever's wider; they're mostly slivers
 * between walls and ribs, whose corners can be too sharp to round. Their area as cut is their area, plus their
 * outline's length times `radius`, plus the bit's (Steiner's formula: exact for convex pockets, and a little more than
 * it for others).
 */
function smallPockets(context is Context, pockets is Query, ends is Query, radius is ValueWithUnits, wall is ValueWithUnits) returns array
{
    return filter(evaluateQuery(context, pockets), function(pocket)
        {
            // Both its ends (on the face and its far side) are its outline
            const pocketEnds = qIntersection([ends, qOwnedByBody(pocket, EntityType.FACE)]);
            const area = evArea(context, { "entities" : pocketEnds }) / 2;
            const outline = evLength(context, { "entities" : qAdjacent(pocketEnds, AdjacencyType.EDGE, EntityType.EDGE) }) / 2;
            return isSmallPocket(area, outline, radius, wall);
        });
}

/**
 * Whether a pocket of `area`, whose outline is `outline` long, is too small to cut once it's grown back by `radius`
 * (see `smallPockets`).
 */
export function isSmallPocket(area is ValueWithUnits, outline is ValueWithUnits, radius is ValueWithUnits, wall is ValueWithUnits) returns boolean
{
    return area + outline * radius + PI * radius ^ 2 < PI * max(2 * radius, wall) ^ 2;
}

/**
 * A pocket's corners: its convex edges which aren't on its `ends` (see the body). The pockets are the face's outline
 * extruded, so each is between two of its sides, along the face's normal.
 */
function pocketCorners(context is Context, pocket is Query, ends is Query) returns Query
{
    // Every edge of its ends, holes' loops too (`qLoopEdges` is only the outer boundary)
    const endEdges = qAdjacent(qIntersection([ends, qOwnedByBody(pocket, EntityType.FACE)]), AdjacencyType.EDGE, EntityType.EDGE);
    const sides = qSubtraction(qOwnedByBody(pocket, EntityType.EDGE), endEdges);
    return qUnion(filter(evaluateQuery(context, sides), function(edge)
            {
                return evEdgeConvexity(context, { "edge" : edge }) == EdgeConvexityType.CONVEX;
            }));
}

/**
 * The faces in `plane` (of parts which aren't hidden) which overlap `footprint` (the ribs), seen along
 * the plane's normal: their bounding boxes in it overlap. Only evaluates, as editing logic should: running operations
 * to see what they'd do (between `startFeature` and `abortFeature`) can crash the Part Studio.
 */
function facesUnder(context is Context, plane is Plane, footprint is Query, hiddenBodies is Query) returns Query
{
    const cSys = coordSystem(plane);
    const area = evBox3d(context, { "topology" : footprint, "cSys" : cSys, "tight" : false });
    const parts = qSubtraction(qAllModifiableSolidBodiesNoMesh(), hiddenBodies);
    return qUnion(filter(evaluateQuery(context, qCoincidesWithPlane(qOwnedByBody(parts, EntityType.FACE), plane)), function(face)
            {
                const bounds = evBox3d(context, { "topology" : face, "cSys" : cSys, "tight" : false });
                return bounds.minCorner[0] <= area.maxCorner[0] && area.minCorner[0] <= bounds.maxCorner[0] &&
                    bounds.minCorner[1] <= area.maxCorner[1] && area.minCorner[1] <= bounds.maxCorner[1];
            }));
}
