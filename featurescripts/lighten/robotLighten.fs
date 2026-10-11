FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

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
        if (quickRibs.skipped > 0)
        {
            reportFeatureWarning(context, id, "Some quick ribs are skipped because they're incomplete.", ["chains", "hubs", "edgeRibs"]);
        }
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

        // The pocket: the face's extrude into its part
        const depth = pocketDepth(context, definition, part, facePlane);
        if (isBlind(definition))
        {
            addDepthManipulator(context, id, face, facePlane, depth);
        }
        // How errors show what's built and what's at fault: out of the part, on its face (see `showOut`)
        const errors = {
                "featureId" : id,
                "id" : id + "errorDisplay",
                "transform" : transform(facePlane.normal * depth),
                "count" : new box(0),
                "shown" : new box([])
            };
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
            showOperationError(context, errors, id + "pocket");
            throw regenError("Failed to create pockets.", ["face", "depth"], face);
        }
        const ends = qCapEntity(id + "pocket", CapType.EITHER, EntityType.FACE);
        // The pockets' ends, through the walls, ribs, and rounding (see `pocketEnds`)
        const trackedEnds = startTracking(context, ends);

        // Sketch regions to ignore are left solid: cut from the pocket, so walls go around them too
        showBuiltOnFailure(context, errors, qCreatedBy(id + "pocket", EntityType.BODY), function()
            {
                excludeRegions(context, errors, id + "excludeRegions", facePlane, qCreatedBy(id + "pocket", EntityType.BODY), qSketchFilter(ignoredFaces, SketchObject.YES));
            });

        // The walls: the pocket (and the pieces regions to ignore split it into), inset by them, but along the part's
        // faces to ignore
        const extruded = qUnion([qCreatedBy(id + "pocket", EntityType.BODY), qCreatedBy(id + "excludeRegions" + "cut", EntityType.BODY)]);
        const pocketFaces = qOwnedByBody(extruded, EntityType.FACE);
        showBuiltOnFailure(context, errors, extruded, function()
            {
                insetPockets(context, errors, id + "walls", id + "wallTrials", extruded, ends, qIntersection([pocketFaces, ignoredSides]),
                    mapArray(overriddenSides, wallOverride => mergeMaps(wallOverride, { "sides" : qIntersection([pocketFaces, wallOverride.sides]) })),
                    definition.wallThickness, radius);
            });

        // The ribs, cut from what's left: what the walls enclose
        const inset = qCreatedBy(id + "walls", EntityType.BODY);
        const ribs = showBuiltOnFailure(context, errors, inset, function()
            {
                return buildRibs(context, errors, id + "ribs", bandExtent(context, facePlane, qUnion([inset, ribEdges])), ribGroups, radius);
            });
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
            showOperationError(context, errors, id + "cutRibs");
            // Each rib, cut alone from a copy of the pockets
            const failing = failingBodies(context, id + "error", ribs, function(errorId is Id, rib is Query)
                {
                    opBoolean(context, errorId + "cut", {
                                "targets" : copyBodies(context, errorId + "copy", inset),
                                "tools" : rib,
                                "operationType" : BooleanOperationType.SUBTRACTION,
                                "keepTools" : true
                            });
                });
            showOut(context, errors, failing, DebugColor.RED);
            showOut(context, errors, inset, DebugColor.BLUE);
            throw regenError("Failed to create rib.", failingRibParameters(context, id + "ribs", ribGroups, failing));
        }
        // The pockets left between them, which the walls and ribs split into pieces
        const pockets = qUnion([inset, qCreatedBy(id + "cutRibs", EntityType.BODY)]);

        if (definition.filletCorners)
        {
            showBuiltOnFailure(context, errors, pockets, function()
                {
                    roundPockets(context, errors, id, facePlane, pocketEnds(pockets, facePlane, trackedEnds), pockets, radius);
                });
        }

        if (isQueryEmpty(context, pockets))
        {
            reportFeatureWarning(context, id, "There's no room for pockets between the walls and ribs.",
                concatenateArrays([["wallThickness", "filletRadius"], overridesWalls(definition) ? ["wallOverrides"] : [], ribParameters(definition)]));
        }
        else
        {
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
                showOperationError(context, errors, id + "cut");
                // Each pocket, cut alone from a copy of the part
                const failing = failingBodies(context, id + "error", pockets, function(errorId is Id, pocket is Query)
                    {
                        opBoolean(context, errorId + "cut", {
                                    "targets" : copyBodies(context, errorId + "copy", part),
                                    "tools" : pocket,
                                    "operationType" : BooleanOperationType.SUBTRACTION,
                                    "keepTools" : true
                                });
                    });
                showOut(context, errors, failing, DebugColor.RED);
                showOut(context, errors, qSubtraction(pockets, failing), DebugColor.BLUE);
                throw regenError("Failed to cut the pockets shown.", ["face"]);
            }
            // Pieces of ribs which touch no wall or other rib are cut free, as parts of their own
            const loose = qCreatedBy(id + "cut", EntityType.BODY);
            if (!isQueryEmpty(context, loose))
            {
                reportFeatureWarning(context, id, "Some ribs touch no wall or other rib, so they're left as loose parts.", ribParameters(definition));
                setErrorEntities(context, id, { "entities" : loose });
            }
            else if (quickRibs.skipped == 0)
            {
                // Only without a warning, which this would replace
                const lightened = 1 - evVolume(context, { "entities" : part }) / volume;
                reportFeatureInfo(context, id, "Weight reduced by " ~ roundToPrecision(lightened * 100, 1) ~ "%.");
            }
        }

        // The pockets are used up by the cut (or there are none)
        opDeleteBodies(context, id + "cleanup", {
                    "entities" : qUnion([qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES), qCreatedBy(id + "ribs", EntityType.BODY)])
                });
    });

/**
 * Runs `step`, and if it fails, shows `built` (what's been built so far) in blue (see `showOut`), around what its error
 * shows in red, before letting the error through.
 */
function showBuiltOnFailure(context is Context, errors is map, built is Query, step is function)
{
    try silent
    {
        return step();
    }
    catch (error)
    {
        showOut(context, errors, qSubtraction(built, qUnion(errors.shown[])), DebugColor.BLUE);
        throw error;
    }
}

/**
 * Shows pocket geometry (`entities`: bodies, faces, or edges) in `color` while the dialog's open, out of the part: moved
 * along the face's normal by the pocket's depth (`errors.transform`), so it stands on the face, where the part doesn't
 * hide it. It shows copies (under `errors.id`), with std's `addDebugEntities`, which outlasts the error's rollback.
 * What's shown red is remembered (`errors.shown`), so blue around it leaves it out. Only for an error about to be
 * thrown: if the copies fail, it's shown less, but the error's still thrown.
 */
function showOut(context is Context, errors is map, entities is Query, color is DebugColor)
{
    const showId = errors.id + unstableIdComponent(errors.count[]);
    errors.count[] += 1;
    if (color == DebugColor.RED)
    {
        errors.shown[] = append(errors.shown[], entities);
    }
    try silent
    {
        const bodies = qEntityFilter(entities, EntityType.BODY);
        if (!isQueryEmpty(context, bodies))
        {
            copyBodies(context, showId + "bodies", bodies);
        }
        const faces = qEntityFilter(entities, EntityType.FACE);
        if (!isQueryEmpty(context, faces))
        {
            opExtractSurface(context, showId + "faces", { "faces" : faces });
        }
        for (var i, edge in evaluateQuery(context, qEntityFilter(entities, EntityType.EDGE)))
        {
            opExtractWires(context, showId + "edges" + unstableIdComponent(i), { "edges" : edge });
        }
        const shown = qCreatedBy(showId, EntityType.BODY);
        opTransform(context, showId + "move", {
                    "bodies" : shown,
                    "transform" : errors.transform
                });
        addDebugEntities(context, shown, color);
    }
}

/**
 * Shows Onshape's own error display for the failed operation `operationId` (what it found wrong, in red, as its own
 * features show it) on the feature (std's `processSubfeatureStatus`). Before the operation's feature, if it's in one,
 * is rolled back.
 */
function showOperationError(context is Context, errors is map, operationId is Id)
{
    processSubfeatureStatus(context, errors.featureId, { "subfeatureId" : operationId, "propagateErrorDisplay" : true });
}

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
 * Insets `pockets` by `wall` and `radius` (the walls, thicker by the radius their corners are rounded to later), under
 * `id`, but at `ends` (their caps) and `ignoredSides`, which stay where they are, and at each of `overrides`' `sides`,
 * inset by its `thickness` instead (see `buildWalls`).
 *
 * They're made in a feature of their own, so when they fail, what was done is rolled back (after Onshape's error
 * display for the operation which failed is shown), and `wallFailure` works out why (trying them again, a different way
 * each time, under `trialId`).
 */
function insetPockets(context is Context, errors is map, id is Id, trialId is Id, pockets is Query, ends is Query, ignoredSides is Query,
    overrides is array, wall is ValueWithUnits, radius is ValueWithUnits)
{
    startFeature(context, id, {});
    var failure;
    try silent
    {
        buildWalls(context, id, pockets, ends, ignoredSides, overrides, wall, radius);
    }
    catch (error)
    {
        failure = error;
    }
    if (failure == undefined)
    {
        endFeature(context, id);
        return;
    }
    if (failure is map && failure.operation != undefined)
    {
        showOperationError(context, errors, failure.operation);
    }
    abortFeature(context, id);
    throw wallFailure(context, errors, trialId, pockets, ends, ignoredSides, overrides, wall, radius, failure);
}

/**
 * The walls, as the previous Onshape Lighten feature makes them: `ends` and `ignoredSides` are moved out by the
 * distance (`wall` and `radius`), and each of `overrides`' sides by `wall` less its thickness (in, for a thicker wall);
 * then the pockets are hollowed by the distance, which moves every face in by it at once, holes of any size and their
 * corners too, so it doesn't fail where a wall along one edge would. What's inside (enclosed) is kept, and the rest
 * deleted. Their concave edges are rounded a hair (std's boolean tolerance, 0.01 mm) first, so they're rounded as
 * they're moved, as the walls' inside corners should be; those rounds are then set to exactly the distance
 * (`opModifyFillet`, as Vent does). Only the faces of `pockets` are moved, so it can be tried on some of them.
 *
 * Before any of that, the part's rounded corners tighter than the distance (`tightCorners`) are made sharp (deleted,
 * and their sides extended to meet): hollowing can't move a convex face in further than its radius, and the walls
 * inside a sharp corner are the same as inside a rounded one that tight.
 *
 * Throws which step failed: a map of its `stage` ("sharpen", "extend", "override", "round" (either rounding), or
 * "shell"), the `operation` which failed, and for "sharpen", the `corners`, or for an override, its `override`.
 */
function buildWalls(context is Context, id is Id, pockets is Query, ends is Query, ignoredSides is Query, overrides is array,
    wall is ValueWithUnits, radius is ValueWithUnits)
{
    const distance = wall + radius;
    const faces = qOwnedByBody(pockets, EntityType.FACE);
    const corners = tightCorners(context, pockets, qUnion([ends, ignoredSides, qUnion(mapArray(overrides, wallOverride => wallOverride.sides))]), distance);
    if (!isQueryEmpty(context, corners))
    {
        try silent
        {
            opDeleteFace(context, id + "sharpen", {
                        "deleteFaces" : corners,
                        "includeFillet" : false,
                        "capVoid" : false
                    });
        }
        catch
        {
            throw { "stage" : "sharpen", "operation" : id + "sharpen", "corners" : corners };
        }
    }
    try silent
    {
        opOffsetFace(context, id + "extend", {
                    "moveFaces" : qIntersection([faces, qUnion([ends, ignoredSides])]),
                    "offsetDistance" : distance
                });
    }
    catch
    {
        throw { "stage" : "extend", "operation" : id + "extend" };
    }
    for (var i, wallOverride in overrides)
    {
        const offset = wall - wallOverride.thickness;
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
            throw { "stage" : "override", "operation" : overrideId, "override" : wallOverride };
        }
    }

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
            throw { "stage" : "round", "operation" : id + "roundConcave" };
        }
    }
    // Hollowing grows them by the distance; they're set to exactly it after
    const rounds = startTracking(context, qCreatedBy(id + "roundConcave", EntityType.FACE));

    try silent
    {
        opShell(context, id + "shell", {
                    "entities" : pockets,
                    "thickness" : -distance
                });
    }
    catch
    {
        throw { "stage" : "shell", "operation" : id + "shell" };
    }
    // One at a time, so the pockets don't need a boolean
    for (var i, pocket in evaluateQuery(context, pockets))
    {
        const encloseId = id + "enclose" + unstableIdComponent(i);
        try silent
        {
            opEnclose(context, encloseId, { "entities" : pocket });
        }
        catch
        {
            throw { "stage" : "shell", "operation" : encloseId };
        }
    }
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
            throw { "stage" : "round", "operation" : id + "exactCorners" };
        }
    }
    opDeleteBodies(context, id + "deleteShells", { "entities" : pockets });
}

/**
 * Why the walls failed, as an error to throw, with what's at fault shown in red: only on the way to it (it tries them
 * again, a few ways, each rolled back), so it costs nothing when they don't. In order:
 *
 * - With wall overrides, if the walls work without them, the overrides which fail alone (or all of them).
 * - Otherwise blame is laid as if there were no overrides. With Fillet corners, if the walls work at Wall thickness
 *   alone, Fillet radius is to blame (the walls are built thicker by it, then the pockets grown back), and the pockets
 *   which fail alone are shown.
 * - Otherwise a generic error. It shows the part's tight corners which fail alone, if they couldn't be made sharp, or
 *   the pockets which fail alone.
 *
 * @param failure : What `buildWalls` threw.
 */
function wallFailure(context is Context, errors is map, id is Id, pockets is Query, ends is Query, ignoredSides is Query, overrides is array,
    wall is ValueWithUnits, radius is ValueWithUnits, failure) returns map
{
    const trials = new box(0);
    // Whether `operation(trialId)` works, tried in a feature of its own, which is then rolled back
    const tryAlone = function(operation is function) returns boolean
        {
            const trialId = id + unstableIdComponent(trials[]);
            trials[] += 1;
            startFeature(context, trialId, {});
            var worked = false;
            try silent
            {
                operation(trialId);
                worked = true;
            }
            abortFeature(context, trialId);
            return worked;
        };
    const works = function(trialOverrides is array, trialRadius is ValueWithUnits, trialPockets is Query) returns boolean
        {
            return tryAlone(trialId => buildWalls(context, trialId, trialPockets, ends, ignoredSides, trialOverrides, wall, trialRadius));
        };
    const radiusParameters = radius > 0 * meter ? ["wallThickness", "filletRadius"] : ["wallThickness"];

    if (overrides != [] && works([], radius, pockets))
    {
        var culprits = filter(overrides, wallOverride => !works([wallOverride], radius, pockets));
        if (culprits == [])
        {
            culprits = overrides;
        }
        return regenError("Selected wall overrides are invalid.",
            concatenateArrays(mapArray(culprits, wallOverride => wallOverride.parameters)),
            qUnion(mapArray(culprits, wallOverride => wallOverride.faces)));
    }

    if (radius > 0 * meter && works([], 0 * meter, pockets))
    {
        showOut(context, errors, failingPockets(context, pockets, pocket => works([], radius, pocket)), DebugColor.RED);
        return regenError("Fillet radius is too large for the pockets shown.", ["filletRadius"]);
    }

    // What failed is shown; which step failed (an operation of Onshape's own, in its error display) isn't worth saying
    if (failure is map && failure.stage == "sharpen")
    {
        const culprits = filter(evaluateQuery(context, failure.corners), function(corner)
            {
                return !tryAlone(trialId => opDeleteFace(context, trialId, { "deleteFaces" : corner, "includeFillet" : false, "capVoid" : false }));
            });
        showOut(context, errors, cornerEdges(culprits == [] ? failure.corners : qUnion(culprits), ends), DebugColor.RED);
    }
    else
    {
        showOut(context, errors, failingPockets(context, pockets, pocket => works([], radius, pocket)), DebugColor.RED);
    }
    return regenError("Failed to create walls.", radiusParameters);
}

/**
 * The pockets `works` fails for, tried alone: all of them, if none fails alone.
 */
function failingPockets(context is Context, pockets is Query, works is function) returns Query
{
    const failing = filter(evaluateQuery(context, pockets), pocket => !works(pocket));
    return failing == [] ? pockets : qUnion(failing);
}

/**
 * The edges where `corners` (sides of the pockets) meet their `ends`: on the face to lighten and its far side.
 */
function cornerEdges(corners is Query, ends is Query) returns Query
{
    return qIntersection([qAdjacent(corners, AdjacencyType.EDGE, EntityType.EDGE), qAdjacent(ends, AdjacencyType.EDGE, EntityType.EDGE)]);
}

/**
 * The pockets' convex rounded sides (the part's rounded outside corners, which hollowing moves in), other than
 * `excluded`, whose radius is less than `distance`: hollowing can't move a convex side in further than its radius.
 */
function tightCorners(context is Context, pockets is Query, excluded is Query, distance is ValueWithUnits) returns Query
{
    const sides = qSubtraction(qOwnedByBody(pockets, EntityType.FACE)->qGeometry(GeometryType.CYLINDER), excluded);
    return qUnion(filter(evaluateQuery(context, sides), function(face)
            {
                const cylinder = evSurfaceDefinition(context, { "face" : face });
                if (!(cylinder is Cylinder) || cylinder.radius >= distance)
                {
                    return false;
                }
                // Convex: its normal (out of the pocket) points away from its axis
                const tangent = evFaceTangentPlane(context, { "face" : face, "parameter" : vector(0.5, 0.5) });
                const axis = line(cylinder.coordSystem.origin, cylinder.coordSystem.zAxis);
                return dot(tangent.normal, tangent.origin - project(axis, tangent.origin)) > 0;
            }));
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
 * The parameters of the rib groups (Ribs to use and Rib thickness, or an override's) whose ribs (built under `id`, see
 * `buildRibs`) are among `failing`.
 */
function failingRibParameters(context is Context, id is Id, groups is array, failing is Query) returns array
{
    var parameters = [];
    for (var j, group in groups)
    {
        if (!isQueryEmpty(context, qIntersection([failing, qCreatedBy(id + unstableIdComponent(j), EntityType.BODY)])))
        {
            parameters = concatenateArrays([parameters, group.parameters]);
        }
    }
    return parameters;
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
function excludeRegions(context is Context, errors is map, id is Id, plane is Plane, pockets is Query, regions is Query)
{
    if (isQueryEmpty(context, regions))
    {
        return;
    }
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
        showOperationError(context, errors, id + "extrude");
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
        showOperationError(context, errors, id + "cut");
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
function buildRibs(context is Context, errors is map, id is Id, extent is map, groups is array, radius is ValueWithUnits) returns Query
{
    for (var j, group in groups)
    {
        buildRibGroup(context, errors, id + unstableIdComponent(j), extent, group.edges, group.thickness / 2 + radius, group.parameters);
    }
    return qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID);
}

/**
 * One group's ribs, `halfWidth` to each side (see `buildRibs`).
 */
function buildRibGroup(context is Context, errors is map, id is Id, extent is map, edges is Query, halfWidth is ValueWithUnits, parameters is array)
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
                showOperationError(context, errors, ribId + "cylinder");
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
            showOperationError(context, errors, ribId + "sheet");
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
            showOperationError(context, errors, ribId + "rib");
            throw regenError("Failed to create rib.", parameters, edge);
        }
    }
}

/**
 * Rounds the pockets' corners, as a router bit of `radius` leaves them: they were made with walls and ribs `radius`
 * thicker, so a pocket narrower than the bit is gone, and the rest are grown back by `radius`. Their corners are
 * rounded a hair (0.01 mm) first, as the previous Onshape Lighten feature does, so growing them rounds them, rather
 * than filleting them after, which can fail where a pocket narrows; those rounds are then set to exactly `radius`
 * (`opModifyFillet`, as Vent does). Their inside corners (the walls' and ribs' outside ones) stay sharp.
 *
 * Pockets smaller than `radius` both ways (`smallPockets`) are left solid first: slivers between walls and ribs, not
 * worth cutting, whose corners can be too sharp to round.
 *
 * Their narrow ends (`narrowEnds`), where the rounds on either side would meet, as at the tip of a pocket
 * between ribs, are given one full round across them instead (std's `opFullRoundFillet`), which growing makes a round
 * end as wide as the pocket.
 */
function roundPockets(context is Context, errors is map, id is Id, plane is Plane, ends is Query, pockets is Query, radius is ValueWithUnits)
{
    const small = smallPockets(context, plane, pockets, radius);
    if (small != [])
    {
        opDeleteBodies(context, id + "dropSmall", { "entities" : qUnion(small) });
    }
    if (isQueryEmpty(context, pockets))
    {
        return;
    }
    for (var i, narrowEnd in narrowEnds(context, pockets, ends))
    {
        const fullRoundId = id + "fullRound" + unstableIdComponent(i);
        try silent
        {
            opFullRoundFillet(context, fullRoundId, {
                        "side1Face" : narrowEnd.sides[0],
                        "side2Face" : narrowEnd.sides[1],
                        "centerFaces" : narrowEnd.face,
                        "tangentPropagation" : false
                    });
        }
        catch
        {
            showOperationError(context, errors, fullRoundId);
            showOut(context, errors, narrowEnd.face, DebugColor.RED);
            throw regenError("Failed to fillet the specified corners.", ["filletRadius"]);
        }
    }
    const corners = pocketCorners(context, pockets, ends);
    if (!isQueryEmpty(context, corners))
    {
        try silent
        {
            opFillet(context, id + "roundCorners", {
                        "entities" : corners,
                        "radius" : TOLERANCE.booleanDefaultTolerance * meter
                    });
        }
        catch
        {
            showOperationError(context, errors, id + "roundCorners");
            showOut(context, errors, failingCorners(context, id + "error", pockets, corners), DebugColor.RED);
            throw regenError("Failed to fillet the specified corners.", ["filletRadius"]);
        }
    }
    try silent
    {
        opOffsetFace(context, id + "grow", {
                    "moveFaces" : qSubtraction(qOwnedByBody(pockets, EntityType.FACE), ends),
                    "offsetDistance" : radius
                });
    }
    catch
    {
        showOperationError(context, errors, id + "grow");
        // Each pocket, grown alone (they're separate bodies, so one's try doesn't change another's)
        const failing = failingBodies(context, id + "error", pockets, function(errorId is Id, pocket is Query)
            {
                opOffsetFace(context, errorId, {
                            "moveFaces" : qSubtraction(qOwnedByBody(pocket, EntityType.FACE), ends),
                            "offsetDistance" : radius
                        });
            });
        showOut(context, errors, failing, DebugColor.RED);
        throw regenError("Failed to fillet the specified corners.", ["filletRadius"]);
    }
    // Growing made them the radius and the hair; they're set to exactly the radius
    const rounds = qCreatedBy(id + "roundCorners", EntityType.FACE);
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
            showOperationError(context, errors, id + "exactCorners");
            const failing = failingAlone(context, id + "error", rounds, function(trialId is Id, round is Query)
                {
                    opModifyFillet(context, trialId, {
                                "faces" : round,
                                "modifyFilletType" : ModifyFilletType.CHANGE_RADIUS,
                                "reFillet" : false,
                                "radius" : radius
                            });
                });
            showOut(context, errors, failing == [] ? rounds : qUnion(failing), DebugColor.RED);
            throw regenError("Failed to fillet the specified corners.", ["filletRadius"]);
        }
    }
}

/**
 * The pockets (before they're grown back) whose bounding box, tight, in `plane` (the face's), is less than `radius`
 * across both ways. Its sides are along the plane's x and y axes, so a pocket's measured as it's turned on the face.
 */
function smallPockets(context is Context, plane is Plane, pockets is Query, radius is ValueWithUnits) returns array
{
    return filter(evaluateQuery(context, pockets), function(pocket)
        {
            const bounds = evBox3d(context, { "topology" : pocket, "cSys" : coordSystem(plane), "tight" : true });
            const size = bounds.maxCorner - bounds.minCorner;
            return size[0] < radius && size[1] < radius;
        });
}

/**
 * Which of `corners` (of `pockets`) make their hair fillet fail: those which fail alone, or without any, the corners of
 * the pockets whose corners fail together, or without any of those, all of them. Only for the catch of the failed
 * fillet, as the feature's about to throw.
 */
function failingCorners(context is Context, id is Id, pockets is Query, corners is Query) returns Query
{
    const hair = TOLERANCE.booleanDefaultTolerance * meter;
    const fillet = function(trialId is Id, edges is Query)
        {
            opFillet(context, trialId, { "entities" : edges, "radius" : hair });
        };
    const alone = failingAlone(context, id + "corners", corners, fillet);
    if (alone != [])
    {
        return qUnion(alone);
    }
    const pocketsFailing = failingAlone(context, id + "pockets", pockets, function(trialId is Id, pocket is Query)
        {
            fillet(trialId, qIntersection([corners, qOwnedByBody(pocket, EntityType.EDGE)]));
        });
    return pocketsFailing == [] ? corners : qIntersection([corners, qOwnedByBody(qUnion(pocketsFailing), EntityType.EDGE)]);
}

/**
 * Which of `entities` `operation(id, entity)` fails for, each tried alone in a feature of its own (under `id`), rolled
 * back after, so each try sees what the failed operation was given. Only for the catch of a failed operation, as the
 * feature's about to throw.
 */
function failingAlone(context is Context, id is Id, entities is Query, operation is function) returns array
{
    var failing = [];
    for (var i, entity in evaluateQuery(context, entities))
    {
        const trialId = id + unstableIdComponent(i);
        startFeature(context, trialId, {});
        try silent
        {
            operation(trialId + "operation", entity);
        }
        catch
        {
            failing = append(failing, entity);
        }
        abortFeature(context, trialId);
    }
    return failing;
}

/**
 * The pockets' narrow ends: their sides (not `ends`) between two convex corners which the hair rounds on those corners
 * would use up, leaving less of it than std's boolean tolerance (so the rounds meet). A round of radius `h` on a
 * corner whose faces' normals turn by `angle` takes `h * tan(angle / 2)` of each face.
 *
 * @returns {array} : Each as a map of its `face`, and the two `sides` it's between.
 */
function narrowEnds(context is Context, pockets is Query, ends is Query) returns array
{
    const hair = TOLERANCE.booleanDefaultTolerance * meter;
    const endEdges = qAdjacent(ends, AdjacencyType.EDGE, EntityType.EDGE);
    var found = [];
    for (var face in evaluateQuery(context, qSubtraction(qOwnedByBody(pockets, EntityType.FACE), ends)))
    {
        const edges = qAdjacent(face, AdjacencyType.EDGE, EntityType.EDGE);
        const corners = evaluateQuery(context, qSubtraction(edges, endEdges));
        if (size(corners) != 2 || !all(corners, corner => evEdgeConvexity(context, { "edge" : corner }) == EdgeConvexityType.CONVEX))
        {
            continue;
        }
        const sides = mapArray(corners, corner => qSubtraction(qAdjacent(corner, AdjacencyType.EDGE, EntityType.FACE), face));
        var used = 0 * meter;
        for (var i, corner in corners)
        {
            const normals = mapArray([face, sides[i]], f => evFaceNormalAtEdge(context, { "edge" : corner, "face" : f, "parameter" : 0.5 }));
            used += hair * tan(angleBetween(normals[0], normals[1]) / 2);
        }
        // Its width: its edges along the ends (on the face to lighten and its far side) are each as long as it's wide
        const width = evLength(context, { "entities" : qIntersection([edges, endEdges]) }) / 2;
        if (width - used < hair)
        {
            found = append(found, { "face" : face, "sides" : sides });
        }
    }
    return found;
}

/**
 * The pockets' ends (`trackedEnds`, the extrude's, tracked through the operations since), and their faces parallel to
 * `plane`, which are ends too: so their flat ends are found even if tracking misses them.
 */
function pocketEnds(pockets is Query, plane is Plane, trackedEnds is Query) returns Query
{
    const faces = qOwnedByBody(pockets, EntityType.FACE);
    return qUnion([qIntersection([faces, trackedEnds]), qParallelPlanes(faces, plane.normal, true)]);
}

/**
 * The pockets' corners: their convex edges between sides (not along `ends`).
 */
function pocketCorners(context is Context, pockets is Query, ends is Query) returns Query
{
    const edges = qSubtraction(qOwnedByBody(pockets, EntityType.EDGE), qAdjacent(ends, AdjacencyType.EDGE, EntityType.EDGE));
    return qUnion(filter(evaluateQuery(context, edges), function(edge)
            {
                return evEdgeConvexity(context, { "edge" : edge }) == EdgeConvexityType.CONVEX;
            }));
}

/**
 * Which of `bodies` `operation(id, body)` fails for, tried on each alone (under an id of its own in `id`), to show
 * what made an operation on them all fail: all of them, if none fails alone. Only for the catch of a failed operation,
 * as the feature's about to throw: it runs an operation per body, and the error rolls everything it builds back.
 */
function failingBodies(context is Context, id is Id, bodies is Query, operation is function) returns Query
{
    var failing = [];
    for (var i, body in evaluateQuery(context, bodies))
    {
        try silent
        {
            operation(id + unstableIdComponent(i), body);
        }
        catch
        {
            failing = append(failing, body);
        }
    }
    return failing == [] ? bodies : qUnion(failing);
}

/**
 * Copies `bodies` (under `id`), and returns the copies, to try an operation without changing them.
 */
function copyBodies(context is Context, id is Id, bodies is Query) returns Query
{
    opPattern(context, id, {
                "entities" : bodies,
                "transforms" : [identityTransform()],
                "instanceNames" : ["copy"]
            });
    return qCreatedBy(id, EntityType.BODY);
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
