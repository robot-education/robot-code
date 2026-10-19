FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
// With a variable #logging of true, it logs each step
import(path : "f4b16cafee30b43d5a717afd", version : "6df25fd6f38ff159a61d61db");
RobotLightenIcon::import(path : "bfffc466263212064267fd69", version : "218975d3bf949876b8d9948a");

const WALL_BOUNDS = { (meter) : [1e-5, 0.003175, 500], (inch) : 0.125, (millimeter) : 3 } as LengthBoundSpec;
const RIB_BOUNDS = { (meter) : [1e-5, 0.003175, 500], (inch) : 0.125, (millimeter) : 3 } as LengthBoundSpec;
const WALL_OVERRIDE_BOUNDS = { (meter) : [1e-5, 0.0015875, 500], (inch) : 0.0625, (millimeter) : 1.5 } as LengthBoundSpec;
const RIB_OVERRIDE_BOUNDS = { (meter) : [1e-5, 0.00635, 500], (inch) : 0.25, (millimeter) : 6 } as LengthBoundSpec;
const DEPTH_BOUNDS = { (meter) : [1e-5, 0.003175, 500], (inch) : 0.125, (millimeter) : 3 } as LengthBoundSpec;
// A 1/8 in. router bit's
const FILLET_RADIUS_BOUNDS = { (meter) : [1e-5, 0.0015875, 500], (inch) : 0.0625, (millimeter) : 1.5 } as LengthBoundSpec;

// Without Exact pockets, walls and ribs are made this much thicker than they're to be: with Fillet corners, the pockets
// are grown back by as much, rounding their corners this much more than Fillet radius; without, the walls and ribs are
// left this much thicker. Walls which would exactly meet, or a pocket exactly the bit, at the sizes given (as round
// ones are) meet a little past them, rather than leaving pockets touching along an edge, or slivers, which the cuts
// and growing back fail on. More than std's boolean tolerance, so it's never taken as touching.
const TIE_OFFSET = 1e-4 * meter;

// Errors which exact sizes can cause end with this
const TRY_EXACT_POCKETS = " Try toggling the exact pockets option.";

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
 * corners rounded as a router bit leaves them. Ribs run along the selected sketch edges (a sketch's, or Quick rib's).
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

                    annotation { "Name" : "Add walls around ignored faces", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    definition.wallIgnoredFaces is boolean;
                }
            }

            annotation { "Name" : "Ribs to use", "Filter" : EntityType.EDGE && SketchObject.YES }
            definition.ribEdges is Query;

            annotation { "Name" : "Exclude construction lines", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.excludeConstruction is boolean;
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
                // Later overrides take precedence over earlier ones (and all of them over Wall thickness and Faces to
                // ignore)
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

        annotation { "Group Name" : "Options", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Exact pockets", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                        "Description" : "If false, created pockets will be adjusted a slight amount in order to prevent issues with non-manifold geometry." }
            definition.exactPockets is boolean;
        }
    }
    {
        const face = getFace(context, definition);
        // Its normal points out of its part, so pockets go against it
        const facePlane = evPlane(context, { "face" : face });
        const part = qOwnerBody(face);
        // Mirrored or patterned onto another part (a feature mirror or pattern without the part): everything's built
        // where the face is, and the pockets moved by this onto that part
        const patternTransform = getRemainderPatternTransform(context, { "references" : face });
        const ignoredFaces = getIgnoredFaces(context, definition);
        const wallOverrides = getWallOverrides(context, definition, face);
        var ribGroups = getRibGroups(context, definition);
        var regions = qSketchFilter(ignoredFaces, SketchObject.YES);
        // How the ribs and regions to ignore move relative to the face then (see `byFace`), found before any operation,
        // as std asks of pattern transforms
        const ribTransform = relativeTransform(context, patternTransform, qUnion(mapArray(ribGroups, group => group.edges)));
        const regionTransform = relativeTransform(context, patternTransform, regions);
        verifyParallel(context, definition, face, facePlane, ribGroups, ribTransform);
        const ribEdgesByFace = byFace(context, id + "ribsByFace", mapArray(ribGroups, group => group.edges), ribTransform);
        for (var i, edges in ribEdgesByFace)
        {
            ribGroups[i].edges = edges;
        }
        const ribEdges = qUnion(ribEdgesByFace);
        regions = byFace(context, id + "regionsByFace", [regions], regionTransform)[0];
        // To round the pockets' corners as a router bit of the radius would, they're made with walls and ribs thicker,
        // their corners filleted a hair, and grown back (see `roundPockets`): by the radius less the hair, so the hair
        // and the growth make the radius. Without Exact pockets, the radius is TIE_OFFSET more than Fillet radius, so
        // walls and ribs are exactly as thick as asked, but meet a little past where they would; without Fillet
        // corners, they're just TIE_OFFSET thicker, and nothing's grown back
        const tieOffset = definition.exactPockets ? 0 * meter : TIE_OFFSET;
        const radius = definition.filletCorners ? definition.filletRadius + tieOffset : 0 * meter;
        const grow = definition.filletCorners ? radius - TOLERANCE.booleanDefaultTolerance * meter : tieOffset;
        logLine(context, "Robot lighten: wall " ~ lengthText(definition.wallThickness) ~ ", fillet radius " ~ lengthText(radius) ~ ", grown by " ~
                lengthText(grow));

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
            throw regenError("Failed to extrude face to lighten, check input.", ["face", "depth"], face);
        }
        // Its ends, on the face and its far side: every other face is a side
        const ends = qCapEntity(id + "pocket", CapType.EITHER, EntityType.FACE);

        // Sketch regions to ignore are left solid: with walls around them, cut from the pocket now, so walls go around
        // them too; without, cut from the pockets once they're rounded (below)
        if (definition.wallIgnoredFaces)
        {
            excludeRegions(context, id, facePlane, qCreatedBy(id + "pocket", EntityType.BODY), regions);
        }

        // The walls: the pocket (and the pieces regions to ignore split it into), inset by them, but along the part's
        // faces to ignore
        const extruded = qUnion([qCreatedBy(id + "pocket", EntityType.BODY), qCreatedBy(id + "excludeRegions" + "cut", EntityType.BODY)]);
        if (isQueryEmpty(context, extruded))
        {
            throw regenError("Regions to ignore cover the whole face to lighten.", ["ignoredFaces"], qUnion([face, regions]));
        }
        const pocketFaces = qOwnedByBody(extruded, EntityType.FACE);
        // The pockets' ends from here on, tracked from the extrude's caps (see `buildWalls`)
        const pocketEnds = buildWalls(context, id, definition, face, extruded, ends, qIntersection([pocketFaces, ignoredSides]),
            mapArray(overriddenSides, wallOverride => mergeMaps(wallOverride, { "sides" : qIntersection([pocketFaces, wallOverride.sides]) })), grow);

        // The ribs, cut from what's left: what the walls enclose
        const inset = qCreatedBy(id + "walls", EntityType.BODY);
        if (ribGroups != [])
        {
            const ribs = buildRibs(context, id, bandExtent(context, facePlane, qUnion([inset, ribEdges])), ribGroups, grow);
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
                throw regenError("Failed to cut ribs from pockets." ~ TRY_EXACT_POCKETS, append(ribParameters(definition), "exactPockets"), ribs);
            }
        }
        // The pockets left between them, which the walls and ribs split into pieces
        const pockets = qUnion([inset, qCreatedBy(id + "cutRibs", EntityType.BODY), qCreatedBy(id + "excludeRegions" + "cut", EntityType.BODY)]);
        logEntities(context, "Pockets between the walls and ribs", pockets, DebugColor.GREEN);
        if (isQueryEmpty(context, pockets))
        {
            // The cut used up the walls' insides and the ribs, so the selections are what's shown
            throw regenError("Walls and ribs do not leave room for pockets.",
                concatenateArrays([["wallThickness", "filletRadius"], overridesWalls(definition) ? ["wallOverrides"] : [], ribParameters(definition)]),
                qUnion([face, ribEdges]));
        }

        if (definition.filletCorners)
        {
            roundPockets(context, id, pockets, pocketEnds, grow);
        }
        if (!definition.wallIgnoredFaces)
        {
            excludeRegions(context, id, facePlane, pockets, regions);
            if (isQueryEmpty(context, pockets))
            {
                throw regenError("Regions to ignore cover the whole face to lighten.", ["ignoredFaces"], qUnion([face, regions]));
            }
        }

        // Mirrored or patterned onto another part: the pockets moved there, to cut that part
        var target = part;
        if (!tolerantEquals(patternTransform, identityTransform()))
        {
            target = patternedPart(context, id, face, facePlane, patternTransform);
            opTransform(context, id + "patterned", {
                        "bodies" : pockets,
                        "transform" : patternTransform
                    });
        }

        const volume = evVolume(context, { "entities" : target });
        try silent
        {
            opBoolean(context, id + "cut", {
                        "targets" : target,
                        "tools" : pockets,
                        "operationType" : BooleanOperationType.SUBTRACTION
                    });
        }
        catch
        {
            processSubfeatureStatus(context, id, { "subfeatureId" : id + "cut", "propagateErrorDisplay" : true });
            throw regenError("Failed to cut pockets from part." ~ TRY_EXACT_POCKETS, ["face", "exactPockets"], pockets);
        }
        const lightened = 1 - evVolume(context, { "entities" : target }) / volume;
        reportFeatureInfo(context, id, "Weight reduced by " ~ roundToPrecision(lightened * 100, 1) ~ "%.");
        // Pieces the cut left free (ribs which touch no wall or other rib) are parts of their own: shown, as info,
        // which the weight's gives way to
        const loose = qCreatedBy(id + "cut", EntityType.BODY);
        if (!isQueryEmpty(context, loose))
        {
            reportFeatureInfo(context, id, "Some parts are loose.");
            addDebugEntities(context, loose, DebugColor.BLUE);
        }

        // The pockets are used up by the cut, and the sketches' copies aren't needed
        opDeleteBodies(context, id + "cleanup", {
                    "entities" : qUnion([qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES), qCreatedBy(id + "ribs", EntityType.BODY),
                            qCreatedBy(id + "ribsByFace", EntityType.BODY), qCreatedBy(id + "regionsByFace", EntityType.BODY)])
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
        throw regenError("Select a face to lighten.", ["face"]);
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
 * along them are swept from. Each must border the face to lighten; those with no faces (selected, or left by later
 * ones) are left out.
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
            continue;
        }
        if (isQueryEmpty(context, qIntersection([qLoopEdges(face), qLoopEdges(wallOverride.selected)])))
        {
            throw regenError("Override faces must be adjacent to the face to lighten.", [wallOverride.parameters[0]],
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
 * Whether Wall overrides apply: with Override wall thickness.
 */
function overridesWalls(definition is map) returns boolean
{
    return definition.overrideWallThickness;
}

/**
 * The ribs, in groups of one thickness: Ribs to use, at Rib thickness, then with
 * Override rib thickness, each of Rib overrides; each less what a later one has, so later ones take precedence (a
 * whole sketch, then a few of its ribs at another thickness). An override's ribs needn't be among Ribs to use. Each
 * group's edges are its sketch edges (and their construction edges, unless they're excluded); groups with none (none
 * selected, or left by later ones) are left out, so there may be no ribs at all: just walls.
 *
 * @returns {array} : Each group, as a map of `edges` (a query), `thickness`, and `parameters` (its parameters, for
 *          errors about its ribs).
 */
function getRibGroups(context is Context, definition is map) returns array
{
    return filter(ribGroups(definition), function(group)
        {
            return !isQueryEmpty(context, group.edges);
        });
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
 * The parameters which set the ribs, for errors about them.
 */
function ribParameters(definition is map) returns array
{
    return concatenateArrays([["ribEdges", "ribThickness"], overridesRibs(definition) ? ["ribOverrides"] : []]);
}

/**
 * Whether Rib overrides apply: with Override rib thickness.
 */
function overridesRibs(definition is map) returns boolean
{
    return definition.overrideRibThickness;
}

/**
 * Throws unless the ribs' sketches (of `groups`, see `getRibGroups`), as they'll be relative to the face to lighten
 * (moved by `transform`, see `byFace`), are parallel to it (`facePlane`), so the ribs go straight into it.
 */
function verifyParallel(context is Context, definition is map, face is Query, facePlane is Plane, groups is array, transform is Transform)
{
    const skewed = filter(evaluateQuery(context, qUnion(mapArray(groups, group => group.edges))), function(edge)
        {
            return !parallelVectors(transform.linear * evOwnerSketchPlane(context, { "entity" : edge }).normal, facePlane.normal);
        });
    if (skewed != [])
    {
        throw regenError("Face to lighten must be parallel to the ribs.", concatenateArrays([["face"], ribParameters(definition)]),
            qUnion([face, qUnion(skewed)]));
    }
}

/**
 * How sketch entities (`references`) move relative to the face to lighten when the feature's mirrored or patterned (the
 * face's `patternTransform`, see the body): the face's transform, undone, then theirs. Nothing (the identity) when
 * they're patterned with the face, or nothing is; the face's, undone, when they're patterned but it isn't.
 */
function relativeTransform(context is Context, patternTransform is Transform, references is Query) returns Transform
{
    return inverse(patternTransform) * getRemainderPatternTransform(context, { "references" : references });
}

/**
 * Sketch entities (each of `queries`) where they'd be relative to the face to lighten when the feature's mirrored or
 * patterned: their sketches copied by `transform` (see `relativeTransform`), under `id`. Or as they are, if that's no
 * move.
 *
 * @returns {array} : Each of `queries`, as its entities, or their copies.
 */
function byFace(context is Context, id is Id, queries is array, transform is Transform) returns array
{
    const entities = qUnion(queries);
    if (tolerantEquals(transform, identityTransform()) || isQueryEmpty(context, entities))
    {
        return queries;
    }
    const copies = mapArray(queries, query => startTracking(context, query));
    opPattern(context, id, {
                "entities" : qOwnerBody(entities),
                "transforms" : [transform],
                "instanceNames" : ["1"]
            });
    return mapArray(copies, query => qUnion(evaluateQuery(context, query)));
}

/**
 * The part the feature lightens when it's mirrored or patterned onto another part: the one with a face where `face` (in
 * `plane`) goes by `transform`, through the middle of one of its edges.
 */
function patternedPart(context is Context, featureId is Id, face is Query, plane is Plane, transform is Transform) returns Query
{
    const edge = qNthElement(qAdjacent(face, AdjacencyType.EDGE, EntityType.EDGE), 0);
    const point = transform * evEdgeTangentLine(context, { "edge" : edge, "parameter" : 0.5 }).origin;
    const parts = qSubtraction(qAllModifiableSolidBodiesNoMesh(), qCreatedBy(featureId, EntityType.BODY));
    const faces = qContainsPoint(qCoincidesWithPlane(qOwnedByBody(parts, EntityType.FACE), transform * plane), point);
    if (isQueryEmpty(context, faces))
    {
        throw regenError("Cannot find a part to lighten where the feature is mirrored or patterned.", ["face"], face);
    }
    return qOwnerBody(qNthElement(faces, 0));
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
 * Dragging the depth manipulator sets Depth.
 */
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
    const edges = qUnion(mapArray(getRibGroups(context, definition), group => group.edges));
    if (!isQueryEmpty(context, edges))
    {
        const plane = evOwnerSketchPlane(context, { "entity" : qNthElement(edges, 0) });
        const faces = facesUnder(context, plane, edges, hiddenBodies);
        if (evaluateQueryCount(context, faces) == 1)
        {
            definition.face = faces;
        }
    }
    return definition;
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
        throw regenError("Failed to ignore selected regions, check input.", ["ignoredFaces"], regions);
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
        throw regenError("Failed to ignore selected regions, check input.", ["ignoredFaces"], regions);
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
 * The walls, as the previous Onshape Lighten feature and Vent make them: `pockets`, inset by Wall thickness and
 * `grow` (the walls are thicker by what the pockets are grown back by later, see the body), under
 * `featureId + "walls"`; but at `ends` (their caps) and `ignoredSides`, which stay where they are, and at each of
 * `overrides`' `sides`, inset by its `thickness` instead. Hollowing the pockets with their holes fails, or leaves out pieces of them, where the
 * offsets of holes and the outline meet (a hole in a rounded corner), so holes are cut separately: what's at least
 * the inset from the outline, less each hole grown by the inset, is what's at least the inset from both. In steps:
 *
 * 1. `ends` and `ignoredSides` are moved out by the inset, so hollowing brings them back.
 * 2. Each override's sides are moved out by Wall thickness less its thickness (in, for a thicker wall).
 * 3. The holes are split off (`holeRings`): copies of them are kept, and they're filled in the pockets
 *    (`opDeleteFace`). They're split off after steps 1 and 2, so their ends are moved out too, and their sides along
 *    faces to ignore and overrides are moved as the pockets' are.
 * 4. Concave edges of the pockets (now only their outlines'), and the holes' corners, get a hair fillet (std's boolean
 *    tolerance), which hollowing and growing grow into the walls' rounded inside corners.
 * 5. The pockets are hollowed by the inset (`opShell`), which moves their outlines' faces in at once, and what's inside
 *    is kept (`opEnclose`, a pocket at a time).
 * 6. The holes are grown by the inset (`opOffsetFace`), and cut from what's inside.
 *
 * The walls' inside corners are rounded the inset and the hair: a hair (0.01 mm) more than Wall thickness, once the
 * pockets are grown back. Setting them to exactly it (`opModifyFillet`, as Vent does) cost a step for that hair.
 *
 * A failed step fails the feature, with its own error display, highlighting what sets it. Hollowing and growing fail
 * where the walls are too thick for the outline or a hole, so their errors say so, showing the face, or the holes
 * which fail to grow (`holesFailingGrowth`).
 *
 * @returns {Query} : The inset pockets' ends: `ends`, tracked from after step 1 through hollowing, and the cuts and
 *          rounding after it. Tracking from before step 1 would take in the pocket's extrude (the last operation
 *          before it), whose sides are derived from its caps too.
 */
function buildWalls(context is Context, featureId is Id, definition is map, face is Query, pockets is Query, ends is Query, ignoredSides is Query,
    overrides is array, grow is ValueWithUnits) returns Query
{
    const id = featureId + "walls";
    const distance = definition.wallThickness + grow;
    const parameters = concatenateArrays([["wallThickness", "filletRadius"], overridesWalls(definition) ? ["wallOverrides"] : []]);
    // Which parameters to decrease, after std's "Selected offset is too large, decrease the value."
    const thickness = definition.filletCorners ? "Wall thickness plus fillet radius" : "Wall thickness";
    const decrease = definition.filletCorners ? "decrease them." : "decrease the value.";
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
        throw regenError("Failed to create walls along highlighted faces.", parameters, qIntersection([faces, qUnion([ends, ignoredSides])]));
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
            throw regenError("Could not offset override faces as requested.", wallOverride.parameters, wallOverride.faces);
        }
    }

    // 3. Holes split off: the pockets copied, their holes filled, copied again, and the first copy cut from the second
    const holes = qUnion(holeRings(context, pockets, ends));
    const holeBodies = qUnion([qCreatedBy(id + "holes", EntityType.BODY), qCreatedBy(id + "splitHoles", EntityType.BODY)]);
    if (!isQueryEmpty(context, holes))
    {
        try silent
        {
            opPattern(context, id + "unfilled", {
                        "entities" : pockets,
                        "transforms" : [identityTransform()],
                        "instanceNames" : ["1"]
                    });
            opDeleteFace(context, id + "fill", {
                        "deleteFaces" : holes,
                        "includeFillet" : false,
                        "capVoid" : false,
                        "leaveOpen" : false
                    });
            opPattern(context, id + "holes", {
                        "entities" : pockets,
                        "transforms" : [identityTransform()],
                        "instanceNames" : ["1"]
                    });
            opBoolean(context, id + "splitHoles", {
                        "targets" : qCreatedBy(id + "holes", EntityType.BODY),
                        "tools" : qCreatedBy(id + "unfilled", EntityType.BODY),
                        "operationType" : BooleanOperationType.SUBTRACTION
                    });
        }
        catch
        {
            // The holes are filled once their copies are made, so it's the face that's shown if they're gone
            throw regenError("Failed to create walls around highlighted holes.", parameters,
                isQueryEmpty(context, holes) ? face : partFaces(context, face, holes));
        }
    }
    logEntities(context, "Holes, cut separately", holeBodies, DebugColor.BLUE);
    // Their ends are parallel to the face; every other face is a side
    const holeFaces = qOwnedByBody(holeBodies, EntityType.FACE);
    const holeEnds = qParallelPlanes(holeFaces, evPlane(context, { "face" : face }).normal, true);
    const holeSides = qSubtraction(holeFaces, holeEnds);

    // 4. Concave edges, and the holes' corners, a hair
    const concave = qUnion(filter(evaluateQuery(context, qOwnedByBody(pockets, EntityType.EDGE)), function(edge)
            {
                return evEdgeConvexity(context, { "edge" : edge }) == EdgeConvexityType.CONCAVE;
            }));
    const holeEdges = qSubtraction(qOwnedByBody(holeBodies, EntityType.EDGE), qAdjacent(holeEnds, AdjacencyType.EDGE, EntityType.EDGE));
    const holeCorners = qUnion(filter(evaluateQuery(context, holeEdges), function(edge)
            {
                return evEdgeConvexity(context, { "edge" : edge }) == EdgeConvexityType.CONVEX;
            }));
    if (!isQueryEmpty(context, qUnion([concave, holeCorners])))
    {
        try silent
        {
            opFillet(context, id + "roundConcave", {
                        "entities" : qUnion([concave, holeCorners]),
                        "radius" : TOLERANCE.booleanDefaultTolerance * meter
                    });
        }
        catch
        {
            processSubfeatureStatus(context, featureId, { "subfeatureId" : id + "roundConcave", "propagateErrorDisplay" : true });
            throw regenError("Failed to fillet inside corners of walls.", parameters, qUnion([concave, holeCorners]));
        }
    }
    // 5. Hollowed, and what's inside kept
    try silent
    {
        opShell(context, id + "shell", {
                    "entities" : pockets,
                    "thickness" : -distance
                });
    }
    catch
    {
        // Not the shell's own display: it shows the pockets, sticking out of the part with their ends moved out
        throw regenError(thickness ~ " is too large, " ~ decrease, parameters, face);
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
            throw regenError("Failed to create pocket inside highlighted walls.", parameters, pocket);
        }
    }

    // 6. Holes grown, and cut from what's inside
    const inside = qUnion([qCreatedBy(id + "enclose", EntityType.BODY), qCreatedBy(id + "cutHoles", EntityType.BODY)]);
    if (!isQueryEmpty(context, holeBodies))
    {
        try silent
        {
            opOffsetFace(context, id + "growHoles", {
                        "moveFaces" : holeSides,
                        "offsetDistance" : distance
                    });
        }
        catch
        {
            const failing = holesFailingGrowth(context, id + "findHoles", holeBodies, holeSides, distance);
            throw regenError(thickness ~ " is too large for highlighted holes, " ~ decrease, parameters,
                partFaces(context, face, qIntersection([holeSides, qOwnedByBody(qUnion(failing), EntityType.FACE)])));
        }
        if (isQueryEmpty(context, qCreatedBy(id + "enclose", EntityType.BODY)))
        {
            opDeleteBodies(context, id + "deleteHoles", { "entities" : holeBodies });
        }
        else
        {
            try silent
            {
                opBoolean(context, id + "cutHoles", {
                            "targets" : qCreatedBy(id + "enclose", EntityType.BODY),
                            "tools" : holeBodies,
                            "operationType" : BooleanOperationType.SUBTRACTION
                        });
            }
            catch
            {
                // Where the walls around a hole exactly meet another hole's, or the outline's, the pockets either side
                // would touch along an edge, which the cut can't leave
                const meeting = holesFailingCut(context, id + "findMeeting", qCreatedBy(id + "enclose", EntityType.BODY), holeBodies);
                if (meeting == [])
                {
                    throw regenError("Failed to cut walls around holes from pockets.", parameters, face);
                }
                throw regenError("Walls around highlighted holes would make a non-manifold part." ~ TRY_EXACT_POCKETS,
                    append(parameters, "exactPockets"),
                    grownHolesOnPart(context, face, qUnion(meeting), holeSides, distance));
            }
        }
    }
    // A pocket narrower than twice the inset encloses nothing; none left is an error, not an empty result
    logEntities(context, "Pockets inside the walls", inside, DebugColor.GREEN);
    if (isQueryEmpty(context, inside))
    {
        throw regenError("Walls do not enclose a region for pockets, " ~ decrease, parameters, pockets);
    }

    opDeleteBodies(context, id + "deleteShells", { "entities" : pockets });
    return trackedEnds;
}

/**
 * The holes in `pockets`: each pocket's sides are rings of faces between its `ends`, its outline and each of its holes
 * (`qFaceOrEdgeBoundedFaces`). Its outline is the ring spanning the most, as each hole is inside it.
 *
 * @returns {array} : Each hole's ring of faces (a query).
 */
function holeRings(context is Context, pockets is Query, ends is Query) returns array
{
    var holes = [];
    for (var pocket in evaluateQuery(context, pockets))
    {
        var rings = [];
        var left = evaluateQuery(context, qSubtraction(qOwnedByBody(pocket, EntityType.FACE), ends));
        while (left != [])
        {
            const ring = qFaceOrEdgeBoundedFaces(qUnion([left[0], ends]));
            rings = append(rings, ring);
            left = evaluateQuery(context, qSubtraction(qUnion(left), ring));
        }
        var outline = 0;
        var widest = 0 * meter;
        for (var i, ring in rings)
        {
            const bounds = evBox3d(context, { "topology" : ring });
            if (norm(bounds.maxCorner - bounds.minCorner) > widest)
            {
                widest = norm(bounds.maxCorner - bounds.minCorner);
                outline = i;
            }
        }
        for (var i, ring in rings)
        {
            if (i != outline)
            {
                holes = append(holes, ring);
            }
        }
    }
    return holes;
}

/**
 * The holes (bodies of `holes`) which fail to grow by `distance`: each one's `sides` grown on its own, in a feature of
 * its own which is always rolled back, so this changes nothing. Run only once growing them all has failed.
 *
 * @returns {array} : The holes (queries) which fail.
 */
function holesFailingGrowth(context is Context, id is Id, holes is Query, sides is Query, distance is ValueWithUnits) returns array
{
    var failing = [];
    for (var i, hole in evaluateQuery(context, holes))
    {
        const tryId = id + unstableIdComponent(i);
        startFeature(context, tryId, {});
        try silent
        {
            opOffsetFace(context, tryId + "grow", {
                        "moveFaces" : qIntersection([sides, qOwnedByBody(hole, EntityType.FACE)]),
                        "offsetDistance" : distance
                    });
        }
        catch
        {
            failing = append(failing, hole);
        }
        abortFeature(context, tryId);
    }
    logLine(context, "Growing the holes by " ~ lengthText(distance) ~ " fails; " ~ size(failing) ~ " of them fail on their own");
    return failing;
}

/**
 * The holes (bodies of `holes`, grown) whose walls exactly meet another hole's, or the outline's (`inside`'s): cutting
 * them leaves pockets which touch along an edge, which a cut can't. The holes are cut from `inside` in turn, each
 * skipped if its cut fails; each which fails is shown with the hole it meets, the one cut before it whose cut,
 * followed by its own, fails (or none, if its own fails alone: it meets the outline's walls). Each try is in a feature
 * of its own which is always rolled back, so this changes nothing. Run only once cutting them all has failed.
 *
 * @returns {array} : The holes (queries) which fail, and those they meet.
 */
function holesFailingCut(context is Context, id is Id, inside is Query, holes is Query) returns array
{
    const all = evaluateQuery(context, holes);
    // In turn, skipping those which fail
    var failing = [];
    const turnId = id + "inTurn";
    startFeature(context, turnId, {});
    for (var i, hole in all)
    {
        try silent
        {
            opBoolean(context, turnId + unstableIdComponent(i), {
                        "targets" : qUnion([inside, qCreatedBy(turnId, EntityType.BODY)]),
                        "tools" : hole,
                        "operationType" : BooleanOperationType.SUBTRACTION,
                        "keepTools" : true
                    });
        }
        catch
        {
            failing = append(failing, hole);
        }
    }
    abortFeature(context, turnId);

    // Each which fails: alone, then after each other hole
    var meeting = failing;
    for (var j, hole in failing)
    {
        for (var k, other in concatenateArrays([[undefined], all]))
        {
            if (other == hole || isIn(other, meeting))
            {
                continue;
            }
            const pairId = id + "pair" + unstableIdComponent(j) + unstableIdComponent(k);
            startFeature(context, pairId, {});
            var fails = false;
            try silent
            {
                if (other != undefined)
                {
                    opBoolean(context, pairId + "other", {
                                "targets" : inside,
                                "tools" : other,
                                "operationType" : BooleanOperationType.SUBTRACTION,
                                "keepTools" : true
                            });
                }
                opBoolean(context, pairId + "hole", {
                            "targets" : qUnion([inside, qCreatedBy(pairId, EntityType.BODY)]),
                            "tools" : hole,
                            "operationType" : BooleanOperationType.SUBTRACTION,
                            "keepTools" : true
                        });
            }
            catch
            {
                fails = true;
            }
            abortFeature(context, pairId);
            if (fails)
            {
                // Alone, it meets the outline's walls; after another hole, that hole's
                if (other != undefined)
                {
                    meeting = append(meeting, other);
                }
                break;
            }
        }
    }
    logLine(context, "Cutting the grown holes fails; " ~ size(failing) ~ " fail in turn, meeting " ~ (size(meeting) - size(failing)) ~
            " others");
    return meeting;
}

/**
 * The part's faces and `face`'s edges where `holes` (bodies, their `sides` grown by `distance`) were before they were
 * grown: the edges of `face` whose midpoints are `distance` from their sides. Or `face`, if there are none.
 */
function grownHolesOnPart(context is Context, face is Query, holes is Query, sides is Query, distance is ValueWithUnits) returns Query
{
    const holeSides = qIntersection([sides, qOwnedByBody(holes, EntityType.FACE)]);
    const edges = filter(evaluateQuery(context, qAdjacent(face, AdjacencyType.EDGE, EntityType.EDGE)), function(edge)
        {
            const midpoint = evEdgeTangentLine(context, { "edge" : edge, "parameter" : 0.5 }).origin;
            return abs(evDistance(context, { "side0" : holeSides, "side1" : midpoint }).distance - distance) < TOLERANCE.booleanDefaultTolerance * meter;
        });
    if (edges == [])
    {
        return face;
    }
    return qUnion([qUnion(edges), qSubtraction(qAdjacent(qUnion(edges), AdjacencyType.EDGE, EntityType.FACE), face)]);
}

/**
 * The part's faces where `sides` (sides of the pockets, extruded from `face`'s edges) are, and their edges on `face`:
 * the edges of `face` whose midpoints are on them, and the faces along them. The pockets' ends were moved out, so their
 * sides stick out of the part; its own faces show where they are, and the edges show them seen along the face's
 * normal, where the faces are edge on. Or `sides`, if they're along none of its edges (like a region to ignore's).
 */
function partFaces(context is Context, face is Query, sides is Query) returns Query
{
    const edges = filter(evaluateQuery(context, qAdjacent(face, AdjacencyType.EDGE, EntityType.EDGE)), function(edge)
        {
            const midpoint = evEdgeTangentLine(context, { "edge" : edge, "parameter" : 0.5 }).origin;
            return !isQueryEmpty(context, qContainsPoint(sides, midpoint));
        });
    if (edges == [])
    {
        return sides;
    }
    return qUnion([qUnion(edges), qSubtraction(qAdjacent(qUnion(edges), AdjacencyType.EDGE, EntityType.FACE), face)]);
}

/**
 * The ribs of `groups` (see `getRibGroups`), each half its group's thickness to each side (and `grow` more), through
 * `extent` (see `bandExtent`). Each is made on its own, as edges extruded together make one sheet, creased where they
 * meet, which can't be thickened. Each is its edge's sheet, thickened. An arc or circle whose radius is within std's
 * boolean tolerance of that half (or under it) can't be thickened toward its center. Its rib is a solid cylinder
 * around its center instead. A rib which fails highlights its edge, and its group's parameters.
 */
function buildRibs(context is Context, featureId is Id, extent is map, groups is array, grow is ValueWithUnits) returns Query
{
    const id = featureId + "ribs";
    for (var j, group in groups)
    {
        buildRibGroup(context, featureId, id + unstableIdComponent(j), extent, group.edges, group.thickness / 2 + grow, group.parameters);
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
                throw regenError("Failed to create a rib from highlighted edge.", parameters, edge);
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
            throw regenError("Failed to create a rib from highlighted edge.", parameters, edge);
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
            throw regenError("Failed to create a rib from highlighted edge.", parameters, edge);
        }
    }
}

/**
 * Rounds the pockets' corners as a router bit leaves them, as Vent (its `expandWithFillet`) and the previous Onshape
 * Lighten feature do: the walls and ribs were made `grow` thicker, so a pocket narrower than the bit is gone, and the
 * rest are grown back by `grow`. In steps, a pocket at a time:
 *
 * 1. Each corner gets a hair fillet (std's boolean tolerance) on its own, in turn, each kept if it works. Growing rounds
 *    them to the hair and `grow`: Fillet radius (and `TIE_OFFSET`, without Exact pockets) exactly (`grow` is that
 *    less the hair), so their
 *    radius needn't be set after. A corner fails where its side is too short for its fillet and its neighbor's.
 * 2. Each corner which failed gets a full round across its narrow side (`narrowSide`: the side next to it whose other
 *    corner, failed too or filleted, is nearest it), between the sides either side of it (`opFullRoundFillet`). If the
 *    other corner was filleted, its fillet is deleted first (`opDeleteFace`, which heals it back to a corner). Both are
 *    in a feature of their own, so if the round fails, the fillet's put back.
 * 3. The pockets are grown back by `grow` (`opOffsetFace`). Full rounds grow to half their side's width (under a hair)
 *    and `grow`: a bit's two rounds with less than a hair between them are a round that wide, to within it.
 *
 * Nothing here fails the feature: a corner with no narrow side, a full round which fails, or the growth failing, is a
 * warning, showing what's left sharp (or the pockets, ungrown). A failed operation's error, copied for its error
 * display, would fail the feature, so it's cleared (keeping the display) before the warning's reported.
 */
function roundPockets(context is Context, featureId is Id, pockets is Query, ends is Query, grow is ValueWithUnits)
{
    const id = featureId + "round";
    for (var i, pocket in evaluateQuery(context, pockets))
    {
        const pocketId = id + unstableIdComponent(i);
        setExternalDisambiguation(context, pocketId, pocket);
        const filletsId = pocketId + "fillet";

        // 1. Each corner, a hair, on its own
        var failed = [];
        for (var j, corner in evaluateQuery(context, pocketCorners(context, pocket, ends)))
        {
            try silent
            {
                opFillet(context, filletsId + unstableIdComponent(j), {
                            "entities" : corner,
                            "radius" : TOLERANCE.booleanDefaultTolerance * meter
                        });
            }
            catch
            {
                failed = append(failed, corner);
            }
        }

        // 2. Each failed corner, a full round across its narrow side
        for (var j, corner in failed)
        {
            // Rounded already, with a neighbor
            if (isQueryEmpty(context, corner))
            {
                continue;
            }
            const narrow = narrowSide(context, corner, qUnion(failed), qCreatedBy(filletsId, EntityType.FACE), ends);
            if (narrow == undefined)
            {
                reportFeatureWarning(context, featureId, "Failed to fillet highlighted pocket corner.", ["filletRadius"]);
                setErrorEntities(context, featureId, { "entities" : corner });
                continue;
            }
            const roundId = pocketId + "fullRound" + unstableIdComponent(j);
            startFeature(context, roundId, {});
            var rounded = true;
            try silent
            {
                if (narrow.fillet != undefined)
                {
                    opDeleteFace(context, roundId + "deleteFillet", {
                                "deleteFaces" : narrow.fillet,
                                "includeFillet" : false,
                                "capVoid" : false
                            });
                }
                // The sides either side of it (its others are the pocket's ends)
                const sides = evaluateQuery(context, qSubtraction(qAdjacent(narrow.side, AdjacencyType.EDGE, EntityType.FACE), ends));
                if (size(sides) != 2)
                {
                    throw regenError("A narrow side needs a side either side of it.");
                }
                opFullRoundFillet(context, roundId + "fullRound", {
                            "side1Face" : sides[0],
                            "side2Face" : sides[1],
                            "centerFaces" : narrow.side,
                            "tangentPropagation" : false
                        });
            }
            catch
            {
                rounded = false;
            }
            if (rounded)
            {
                endFeature(context, roundId);
                continue;
            }
            // Puts back the fillet it deleted
            abortFeature(context, roundId);
            reportFeatureWarning(context, featureId, "Failed to fillet highlighted pocket.", ["filletRadius"]);
            setErrorEntities(context, featureId, { "entities" : qUnion([corner, narrow.side]) });
        }
    }

    // 3. Grown back
    try silent
    {
        opOffsetFace(context, id + "grow", {
                    "moveFaces" : qSubtraction(qOwnedByBody(pockets, EntityType.FACE), ends),
                    "offsetDistance" : grow
                });
    }
    catch
    {
        // Its error display, but not its error, which would fail the feature
        processSubfeatureStatus(context, featureId, { "subfeatureId" : id + "grow", "propagateErrorDisplay" : true });
        clearFeatureStatus(context, featureId, { "withDisplayData" : false });
        reportFeatureWarning(context, featureId, "Could not offset pockets to fillet radius." ~ TRY_EXACT_POCKETS, ["filletRadius", "exactPockets"]);
        setErrorEntities(context, featureId, { "entities" : pockets });
    }
}

/**
 * A failed corner's narrow side: of the sides next to it, the one whose other corner is nearest it, either one which
 * failed too (among `failed`), or one filleted (a face of `fillets`). The nearest is what made it fail, where its side
 * was too short for both fillets; it's a guess where both sides are short.
 *
 * @returns {{
 *      @field side {Query} : The narrow side.
 *      @field fillet : The other corner's fillet, to delete before rounding across the side, or `undefined` if it
 *              failed too.
 * }} : Or `undefined`, if neither side has another corner.
 */
function narrowSide(context is Context, corner is Query, failed is Query, fillets is Query, ends is Query)
{
    var nearest = undefined;
    for (var side in evaluateQuery(context, qSubtraction(qAdjacent(corner, AdjacencyType.EDGE, EntityType.FACE), ends)))
    {
        const otherCorners = evaluateQuery(context, qSubtraction(qIntersection([qLoopEdges(side), failed]), corner));
        const otherFillets = evaluateQuery(context, qIntersection([qAdjacent(side, AdjacencyType.EDGE, EntityType.FACE), fillets]));
        for (var other in concatenateArrays([otherCorners, otherFillets]))
        {
            const distance = evDistance(context, { "side0" : corner, "side1" : other }).distance;
            if (nearest == undefined || distance < nearest.distance)
            {
                nearest = { "side" : side, "fillet" : isIn(other, otherFillets) ? other : undefined, "distance" : distance };
            }
        }
    }
    return nearest;
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
