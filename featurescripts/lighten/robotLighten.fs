FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
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
 * a depth) is cut away, but for walls along the face's edges (the part's sides and holes) and ribs along the selected
 * sketch edges, with the pockets' corners rounded as a router bit leaves them.
 */
annotation { "Feature Type Name" : "Robot lighten",
        "Feature Type Description" : "Lighten a part with pockets, leaving walls around its edges and holes, and ribs along a sketch." ~ CREDIT,
        "Manipulator Change Function" : "robotLightenManipulatorChange",
        "Editing Logic Function" : "robotLightenEditLogic",
        "Icon" : RobotLightenIcon::BLOB_DATA
    }
export const robotLighten = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
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
        const ribGroups = getRibGroups(context, definition);
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
        try
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
            throw regenError("Failed to extrude the pocket.", ["face", "depth"], face);
        }
        const ends = qCapEntity(id + "pocket", CapType.EITHER, EntityType.FACE);
        // The pockets' ends, through the walls, ribs, and rounding (see `pocketEnds`)
        const trackedEnds = startTracking(context, ends);

        // Sketch regions to ignore are left solid: cut from the pocket, so walls go around them too
        excludeRegions(context, id + "excludeRegions", facePlane, qCreatedBy(id + "pocket", EntityType.BODY), qSketchFilter(ignoredFaces, SketchObject.YES));

        // The walls: the pocket (and the pieces regions to ignore split it into), inset by them, but along the part's
        // faces to ignore
        const extruded = qUnion([qCreatedBy(id + "pocket", EntityType.BODY), qCreatedBy(id + "excludeRegions" + "cut", EntityType.BODY)]);
        const pocketFaces = qOwnedByBody(extruded, EntityType.FACE);
        insetPockets(context, id + "walls", extruded, ends, qIntersection([pocketFaces, ignoredSides]),
            mapArray(overriddenSides, wallOverride => mergeMaps(wallOverride, { "sides" : qIntersection([pocketFaces, wallOverride.sides]) })),
            definition.wallThickness, radius);

        // The ribs, cut from what's left: what the walls enclose
        const inset = qCreatedBy(id + "walls", EntityType.BODY);
        const ribs = buildRibs(context, id + "ribs", bandExtent(context, facePlane, qUnion([inset, ribEdges])), ribGroups, radius);
        try
        {
            opBoolean(context, id + "cutRibs", {
                        "targets" : inset,
                        "tools" : ribs,
                        "operationType" : BooleanOperationType.SUBTRACTION
                    });
        }
        catch
        {
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
            throw regenError("Failed to cut ribs.", ribParameters(definition), failing);
        }
        // The pockets left between them, which the walls and ribs split into pieces
        const pockets = qUnion([inset, qCreatedBy(id + "cutRibs", EntityType.BODY)]);

        if (definition.filletCorners)
        {
            roundPockets(context, id, pocketEnds(pockets, facePlane, trackedEnds), pockets, radius);
        }

        if (isQueryEmpty(context, pockets))
        {
            reportFeatureWarning(context, id, "There's no room for pockets between the walls and ribs.",
                concatenateArrays([["wallThickness", "filletRadius"], ribParameters(definition)]));
        }
        else
        {
            const volume = evVolume(context, { "entities" : part });
            try
            {
                opBoolean(context, id + "cut", {
                            "targets" : part,
                            "tools" : pockets,
                            "operationType" : BooleanOperationType.SUBTRACTION
                        });
            }
            catch
            {
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
                throw regenError("Failed to cut pockets.", ["face"], failing);
            }
            // Pieces of ribs which touch no wall or other rib are cut free, as parts of their own
            const loose = qCreatedBy(id + "cut", EntityType.BODY);
            if (!isQueryEmpty(context, loose))
            {
                reportFeatureWarning(context, id, "Some ribs touch no wall or other rib, so they're left as loose parts.", ribParameters(definition));
                setErrorEntities(context, id, { "entities" : loose });
            }
            else
            {
                // Only without the warning, which this would replace
                const lightened = 1 - evVolume(context, { "entities" : part }) / volume;
                reportFeatureInfo(context, id, "Lightened the part by " ~ roundToPrecision(lightened * 100, 1) ~ "%.");
            }
        }

        // The pockets are used up by the cut (or there are none)
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
    if (!definition.overrideWallThickness)
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
            throw regenError("These faces don't border the face to lighten, so they have no walls.", [wallOverride.parameters[0]],
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
    var later = qNothing();
    for (var i = size(overrides) - 1; i >= 0; i -= 1)
    {
        overrides[i].faces = qSubtraction(overrides[i].selected, later);
        later = qUnion([later, overrides[i].selected]);
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
 * How deep the pocket goes: Depth, or for Through all, a little past the far side of the part.
 */
function pocketDepth(context is Context, definition is map, part is Query, facePlane is Plane) returns ValueWithUnits
{
    if (isBlind(definition))
    {
        return definition.depth;
    }
    const bounds = evBox3d(context, { "topology" : part, "cSys" : coordSystem(facePlane), "tight" : false });
    const through = -bounds.minCorner[2];
    return through + max(through * 0.05, 0.1 * millimeter);
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

export function robotLightenManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    const manipulator = newManipulators[DEPTH_MANIPULATOR];
    if (manipulator != undefined)
    {
        definition.depth = abs(manipulator.offset);
    }
    return definition;
}

/**
 * Fills in the face to lighten, unless it's been set: the one face in the first rib's sketch plane which the ribs are
 * over, if there's just one.
 */
export function robotLightenEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    if (specifiedParameters.face ?? false)
    {
        return definition;
    }
    // A guard: editing logic mustn't throw while the dialog's being filled in, so without ribs yet, it's left
    var plane;
    var edges;
    try silent
    {
        edges = qUnion(mapArray(getRibGroups(context, definition), group => group.edges));
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
    return definition;
}

/**
 * Insets `pockets` by `wall` and `radius` (the walls, thicker by the radius their corners are rounded to later), under
 * `id`, but at `ends` (their caps) and `ignoredSides`, which stay where they are, and at each of `overrides`' `sides`,
 * inset by its `thickness` instead. As Ilya Baran and Morgan Bartlett's Lighten does: those are moved out by the
 * distance (overridden sides, by `wall` less their thickness: in, for a thicker wall), and the pockets hollowed by it,
 * which moves every face in by it at once, holes of any size and their corners too, so it doesn't fail where a wall
 * along one edge would. What's inside (enclosed) is kept, and the rest deleted. Their concave edges are rounded a hair
 * (std's boolean tolerance, 0.01 mm) first, so they're rounded to the distance (and the hair) as they're moved, as the
 * walls' inside corners should be.
 */
function insetPockets(context is Context, id is Id, pockets is Query, ends is Query, ignoredSides is Query, overrides is array,
    wall is ValueWithUnits, radius is ValueWithUnits)
{
    const distance = wall + radius;
    try
    {
        opOffsetFace(context, id + "extend", {
                    "moveFaces" : qUnion([ends, ignoredSides]),
                    "offsetDistance" : distance
                });
    }
    catch
    {
        throw regenError("Failed to extend pockets past their ends.", ["wallThickness"], qUnion([ends, ignoredSides]));
    }
    for (var i, wallOverride in overrides)
    {
        const offset = wall - wallOverride.thickness;
        if (tolerantEqualsZero(offset) || isQueryEmpty(context, wallOverride.sides))
        {
            continue;
        }
        try
        {
            opOffsetFace(context, id + "override" + unstableIdComponent(i), {
                        "moveFaces" : wallOverride.sides,
                        "offsetDistance" : offset
                    });
        }
        catch
        {
            throw regenError("Failed to override walls.", wallOverride.parameters, wallOverride.sides);
        }
    }

    const concave = qUnion(filter(evaluateQuery(context, qOwnedByBody(pockets, EntityType.EDGE)), function(edge)
            {
                return evEdgeConvexity(context, { "edge" : edge }) == EdgeConvexityType.CONCAVE;
            }));
    if (!isQueryEmpty(context, concave))
    {
        try
        {
            opFillet(context, id + "roundConcave", {
                        "entities" : concave,
                        "radius" : TOLERANCE.booleanDefaultTolerance * meter
                    });
        }
        catch
        {
            throw regenError("Failed to round walls' inside corners.", ["wallThickness"], concave);
        }
    }

    try
    {
        opShell(context, id + "shell", {
                    "entities" : pockets,
                    "thickness" : -distance
                });
        // One at a time, so the pockets don't need a boolean
        for (var i, pocket in evaluateQuery(context, pockets))
        {
            opEnclose(context, id + "enclose" + unstableIdComponent(i), { "entities" : pocket });
        }
    }
    catch
    {
        throw regenError("Failed to make walls.", ["wallThickness"], pockets);
    }
    opDeleteBodies(context, id + "deleteShells", { "entities" : pockets });
}

/**
 * The ribs, in groups of one thickness: Ribs to use, at Rib thickness, then with Override rib thickness, each of Rib
 * overrides; each less what a later one has, so later ones take precedence (a whole sketch, then a few of its ribs at
 * another thickness). An override's ribs needn't be among Ribs to use. Each group's edges are its sketch edges (and
 * their construction edges, unless they're excluded); groups left with none are left out.
 *
 * @returns {array} : Each group, as a map of `edges` (a query), `thickness`, and `parameters` (its parameters, for
 *          errors about its ribs).
 */
function getRibGroups(context is Context, definition is map) returns array
{
    const groups = ribGroups(definition);
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
    if (definition.overrideRibThickness)
    {
        for (var i, ribOverride in definition.ribOverrides)
        {
            groups = append(groups, { "edges" : ribOverride.overrideEdges, "thickness" : ribOverride.overrideThickness,
                        "parameters" : [faultyArrayParameterId("ribOverrides", i, "overrideEdges"),
                                faultyArrayParameterId("ribOverrides", i, "overrideThickness")] });
        }
    }
    var later = qNothing();
    for (var i = size(groups) - 1; i >= 0; i -= 1)
    {
        var selected = qEntityFilter(groups[i].edges, EntityType.EDGE);
        if (definition.excludeConstruction)
        {
            selected = qConstructionFilter(selected, ConstructionObject.NO);
        }
        groups[i].selected = selected;
        groups[i].edges = qSubtraction(selected, later);
        later = qUnion([later, selected]);
    }
    return groups;
}

/**
 * The parameters which set the ribs, for errors about them.
 */
function ribParameters(definition is map) returns array
{
    return definition.overrideRibThickness ? ["ribEdges", "ribThickness", "ribOverrides"] : ["ribEdges", "ribThickness"];
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
function excludeRegions(context is Context, id is Id, plane is Plane, pockets is Query, regions is Query)
{
    if (isQueryEmpty(context, regions))
    {
        return;
    }
    const extent = bandExtent(context, plane, qUnion([pockets, regions]));
    try
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
        throw regenError("Failed to extrude regions to ignore.", ["ignoredFaces"], regions);
    }
    try
    {
        opBoolean(context, id + "cut", {
                    "targets" : pockets,
                    "tools" : qCreatedBy(id + "extrude", EntityType.BODY),
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
    catch
    {
        throw regenError("Failed to cut regions to ignore from pockets.", ["ignoredFaces"], regions);
    }
}

/**
 * Where ribs (or regions to ignore) go, along `plane`'s normal: through `bounds` (the pockets, and the ribs' edges or
 * the regions), and a little past it.
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
    const boundingBox = evBox3d(context, { "topology" : bounds, "cSys" : coordSystem(plane), "tight" : false });
    const height = boundingBox.maxCorner[2] - boundingBox.minCorner[2];
    const margin = max(height * 0.05, 0.1 * millimeter);
    var middle = plane;
    middle.origin += plane.normal * (boundingBox.minCorner[2] + boundingBox.maxCorner[2]) / 2;
    return { "plane" : middle, "depth" : height + margin, "halfDepth" : height / 2 + margin };
}

/**
 * The ribs of `groups` (see `getRibGroups`), each half its group's thickness to each side (and `radius` more), through `extent` (see `bandExtent`), each made on its own: edges
 * extruded together make one sheet, creased where they meet, which can't be thickened. Each is its edge's sheet,
 * thickened; but an arc or circle hardly bigger than that can't be thickened toward its center, so its rib is a
 * cylinder around its center, that much bigger than it, instead (a little more than its rib, near its center). A rib
 * which fails highlights its edge, and its group's parameters.
 */
function buildRibs(context is Context, id is Id, extent is map, groups is array, radius is ValueWithUnits) returns Query
{
    for (var j, group in groups)
    {
        buildRibGroup(context, id + unstableIdComponent(j), extent, group.edges, group.thickness / 2 + radius, group.parameters);
    }
    return qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID);
}

/**
 * One group's ribs, `halfWidth` to each side (see `buildRibs`).
 */
function buildRibGroup(context is Context, id is Id, extent is map, edges is Query, halfWidth is ValueWithUnits, parameters is array)
{
    // The circles given cylinders: a circle split into arcs needs just one
    var circles = [];
    for (var i, edge in evaluateQuery(context, edges))
    {
        const ribId = id + unstableIdComponent(i);
        setExternalDisambiguation(context, ribId, edge);
        const curve = evCurveDefinition(context, { "edge" : edge });
        if (curve is Circle && curve.radius <= halfWidth * 1.05)
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
            try
            {
                fCylinder(context, ribId + "cylinder", {
                            "bottomCenter" : center - extent.plane.normal * extent.halfDepth,
                            "topCenter" : center + extent.plane.normal * extent.halfDepth,
                            "radius" : curve.radius + halfWidth
                        });
            }
            catch
            {
                throw regenError("Failed to extrude rib.", parameters, edge);
            }
            continue;
        }

        try
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
            throw regenError("Failed to extrude rib.", parameters, edge);
        }
        try
        {
            opThicken(context, ribId + "rib", {
                        "entities" : qCreatedBy(ribId + "sheet", EntityType.BODY),
                        "thickness1" : halfWidth,
                        "thickness2" : halfWidth
                    });
        }
        catch
        {
            throw regenError("Failed to thicken rib.", parameters, edge);
        }
    }
}

/**
 * Rounds the pockets' corners, as a router bit of `radius` leaves them: they were made with walls and ribs `radius`
 * thicker, so a pocket narrower than the bit is gone, and the rest are grown back by `radius`. Their corners are
 * rounded a hair (0.01 mm) first, as Lighten does, so growing them rounds them to `radius` (and the hair), rather
 * than filleting them after, which can fail where a pocket narrows. Their inside corners (the walls' and ribs'
 * outside ones) stay sharp.
 */
function roundPockets(context is Context, id is Id, ends is Query, pockets is Query, radius is ValueWithUnits)
{
    if (isQueryEmpty(context, pockets))
    {
        return;
    }
    const corners = pocketCorners(context, pockets, ends);
    if (!isQueryEmpty(context, corners))
    {
        try
        {
            opFillet(context, id + "roundCorners", {
                        "entities" : corners,
                        "radius" : TOLERANCE.booleanDefaultTolerance * meter
                    });
        }
        catch
        {
            throw regenError("Failed to round pocket corners.", ["filletRadius"], corners);
        }
    }
    try
    {
        opOffsetFace(context, id + "grow", {
                    "moveFaces" : qSubtraction(qOwnedByBody(pockets, EntityType.FACE), ends),
                    "offsetDistance" : radius
                });
    }
    catch
    {
        // Each pocket, grown alone (they're separate bodies, so one's try doesn't change another's)
        const failing = failingBodies(context, id + "error", pockets, function(errorId is Id, pocket is Query)
            {
                opOffsetFace(context, errorId, {
                            "moveFaces" : qSubtraction(qOwnedByBody(pocket, EntityType.FACE), ends),
                            "offsetDistance" : radius
                        });
            });
        throw regenError("Failed to grow pockets back to round their corners.", ["filletRadius"], failing);
    }
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
