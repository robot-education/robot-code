FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
QuickRibIcon::import(path : "f9fda209a03cf7514da2a818", version : "ec13dad85d83811c1b909de6");

/**
 * Sketches ribs (lines, and circles) between points (circles' and arcs' centers, vertices, and mate connectors), and
 * from points to edges, quickly: for Robot lighten's Ribs to use. Chains run through their points in order (closed or
 * not; two points make one rib); hubs have spokes to their hub, and a circle around it if asked; edge ribs run from
 * points to an edge, meeting it square, or where a manipulator puts them.
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
        annotation { "Name" : "Face to use", "MaxNumberOfPicks" : 1, "Filter" : EntityType.FACE && GeometryType.PLANE && SketchObject.NO }
        definition.face is Query;

        annotation { "Group Name" : "Quick add", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Type", "UIHint" : ["SHOW_LABEL", "REMEMBER_PREVIOUS_VALUE"] }
            definition.quickRibType is QuickRibType;

            annotation { "Name" : "Selections", "Filter" : EntityType.VERTEX || (EntityType.EDGE && GeometryType.CIRCLE) || (EntityType.EDGE && GeometryType.ARC) || BodyType.MATE_CONNECTOR }
            definition.quickSelections is Query;

            if (definition.quickRibType == QuickRibType.CHAIN)
            {
                annotation { "Name" : "Closed" }
                definition.quickClosed is boolean;
            }
            else
            {
                annotation { "Name" : "Add circle" }
                definition.quickAddCircle is boolean;
            }

            annotation { "Name" : "Add ribs" }
            isButton(definition.addRibs);
        }

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

                annotation { "Name" : "Add circle" }
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

        // Ribs deleted (by key, see `lineKey`), whether they're being chosen, and those chosen so far: set by editing
        // logic and the toggle points
        annotation { "Name" : "Deleted ribs", "UIHint" : ["ALWAYS_HIDDEN"] }
        isAnything(definition.deletedRibs);

        annotation { "Name" : "Deleting ribs", "UIHint" : ["ALWAYS_HIDDEN"] }
        definition.deletingRibs is boolean;

        annotation { "Name" : "Ribs to delete", "UIHint" : ["ALWAYS_HIDDEN"] }
        isAnything(definition.ribsToDelete);

        annotation { "Group Name" : "Delete ribs", "Collapsed By Default" : false }
        {
            if (!definition.deletingRibs)
            {
                annotation { "Name" : "Start deleting ribs" }
                isButton(definition.startDeleting);
            }
            else
            {
                annotation { "Name" : "Finish deleting ribs" }
                isButton(definition.finishDeleting);
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
        const quickRibs = getQuickRibs(context, definition);
        const quickAdd = quickAddRibs(context, definition);
        addEdgeRibManipulators(context, id, definition);
        if (quickRibs.ribs == [] && quickAdd == [])
        {
            throw regenError("Add chains, hubs, or edge ribs to sketch ribs.", ["chains", "hubs", "edgeRibs"]);
        }
        // What Quick add would add, in blue
        showRibs(context, id + "quickAdd", plane, quickAdd, DebugColor.BLUE);
        // While deleting, every rib is sketched, with a toggle point to delete it (deleted ones toggled already)
        const deleted = definition.deletingRibs ? [] : definition.deletedRibs ?? [];
        const ribs = filter(quickRibs.ribs, rib => !isIn(rib.key, deleted));
        if (ribs == [] && quickRibs.ribs != [])
        {
            throw regenError("All ribs are deleted.");
        }
        sketchQuickRibs(context, id + "ribSketch", plane, ribs);
        if (quickAdd != [])
        {
            reportFeatureInfo(context, id, "Click add ribs to add the highlighted ribs.");
        }
        if (definition.deletingRibs)
        {
            const toDelete = definition.ribsToDelete ?? [];
            addDeleteRibsManipulator(context, id, plane, quickRibs.ribs, toDelete);
            // Those toggled, in red
            const toggled = toggledRibs(quickRibs.ribs, toDelete);
            if (toggled != [])
            {
                addDebugEntities(context, qUnion(mapArray(toggled, index => sketchEntityQuery(id + "ribSketch", EntityType.EDGE, "rib" ~ index))),
                    DebugColor.RED);
            }
            reportFeatureInfo(context, id, "Select ribs to delete.");
        }
        if (quickRibs.skipped > 0)
        {
            reportFeatureWarning(context, id, "Some quick ribs are skipped because they're incomplete.", ["chains", "hubs", "edgeRibs"]);
        }
    });

/**
 * Dragging an edge rib's end sets its Location (`edgeRibManipulatorChange`); clicking a rib's toggle point, while
 * deleting ribs, toggles whether it's to be deleted (`deleteRibsManipulatorChange`).
 */
export function quickRibManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    definition = deleteRibsManipulatorChange(context, definition, newManipulators);
    return edgeRibManipulatorChange(context, definition, newManipulators);
}

/**
 * Add ribs turns Quick add's selections into a chain or a hub and spokes (`addQuickRibs`). Clear invalid selections
 * drops invalid selections (`clearInvalidSelections`). Start deleting ribs starts choosing ribs to delete, from those
 * deleted already (so they can be put back); Finish deleting ribs deletes those chosen. Fills in Face to use, while
 * it's empty and hasn't been set: the face under the first selection (`firstFaceUnder`).
 */
export function quickRibEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query, clickedButton is string) returns map
{
    if (clickedButton == "addRibs")
    {
        definition = addQuickRibs(definition, evaluateQuery(context, definition.quickSelections));
    }
    else if (clickedButton == "clearInvalid")
    {
        definition = clearInvalidSelections(context, definition);
    }
    else if (clickedButton == "startDeleting")
    {
        definition.deletingRibs = true;
        definition.ribsToDelete = definition.deletedRibs ?? [];
    }
    else if (clickedButton == "finishDeleting")
    {
        definition.deletedRibs = definition.ribsToDelete ?? [];
        definition.deletingRibs = false;
        definition.ribsToDelete = [];
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

// The ribs

/**
 * The bounds of an edge rib's Location: how far along its edge, from its start to its end, as std's fillet bounds its
 * points on edges (`EDGE_INTERIOR_PARAMETER_BOUNDS`).
 */
const EDGE_RIB_LOCATION_RANGE = [0.001, 0.5, 0.999];
export const EDGE_RIB_LOCATION_BOUNDS = { (unitless) : EDGE_RIB_LOCATION_RANGE } as RealBoundSpec;
const EDGE_RIB_MANIPULATOR = "edgeRib";

/**
 * What Quick add's selections are added as.
 */
export enum QuickRibType
{
    annotation { "Name" : "Chain" }
    CHAIN,
    annotation { "Name" : "Hub and spokes" }
    HUB
}

/**
 * Add ribs (a button): Quick add's `selections` (evaluated, in the order chosen) become a chain, with its Closed, or a
 * hub and spokes, the first the hub and the rest its spokes, with its Add circle; then they're cleared. Fewer than two
 * are left as they are: they make no rib.
 */
export function addQuickRibs(definition is map, selections is array) returns map
{
    if (size(selections) < 2)
    {
        return definition;
    }
    if (definition.quickRibType == QuickRibType.CHAIN)
    {
        definition.chains = append(definition.chains, { "chainPoints" : qUnion(selections), "closed" : definition.quickClosed });
    }
    else
    {
        definition.hubs = append(definition.hubs, { "hub" : selections[0], "spokes" : qUnion(subArray(selections, 1)),
                    "addCircle" : definition.quickAddCircle });
    }
    definition.quickSelections = qNothing();
    return definition;
}

/**
 * Where an edge rib meets its edge.
 */
export enum EdgeRibEnd
{
    annotation { "Name" : "Perpendicular to edge" }
    PERPENDICULAR,
    annotation { "Name" : "Manipulator" }
    MANIPULATOR
}

/**
 * The quick ribs (from Chains, Hub and spokes, and Edge ribs), lines first, without repeats. Incomplete chains, hubs,
 * and edge ribs, and selections which aren't there, are skipped.
 *
 * @returns {{
 *      @field ribs {array} : Each a map of its `key` (see `lineKey` and `circleKey`), and its `line` (`[from, to]`) or
 *              `circle` (its `center`, and the `through` point it goes through).
 *      @field skipped {number} : How many chains, hubs, and edge ribs were skipped.
 * }}
 */
export function getQuickRibs(context is Context, definition is map) returns map
{
    var segments = [];
    var circles = [];
    var skipped = 0;
    for (var chain in definition.chains)
    {
        const points = pointsOf(context, chain.chainPoints);
        if (size(points) < 2)
        {
            skipped += 1;
            continue;
        }
        segments = concatenateArrays([segments, chainSegments(points, chain.closed)]);
    }
    for (var hub in definition.hubs)
    {
        const center = pointsOf(context, hub.hub);
        const spokes = pointsOf(context, hub.spokes);
        if (center == [] || spokes == [])
        {
            skipped += 1;
            continue;
        }
        segments = concatenateArrays([segments, hubSegments(center[0], spokes)]);
        if (hub.addCircle)
        {
            circles = append(circles, { "center" : center[0], "through" : spokes[0] });
        }
    }
    for (var edgeRib in definition.edgeRibs)
    {
        const ribs = edgeRibSegments(context, edgeRib);
        if (ribs == [])
        {
            skipped += 1;
            continue;
        }
        segments = concatenateArrays([segments, ribs]);
    }
    return { "ribs" : ribsOf(segments, circles), "skipped" : skipped };
}

/**
 * Ribs (see `getQuickRibs`) from lines (each `[from, to]`, without repeats or lines with no length) and circles (each
 * its `center` and the point it goes `through`).
 */
function ribsOf(segments is array, circles is array) returns array
{
    const lines = mapArray(uniqueSegments(segments), segment => { "key" : lineKey(segment), "line" : segment });
    const rounds = mapArray(circles, circle => { "key" : circleKey(circle.center, circle.through), "circle" : circle });
    return concatenateArrays([lines, rounds]);
}

/**
 * The ribs Quick add would add (see `addQuickRibs`), from its selections as they are (see `getQuickRibs`): none with
 * fewer than two points.
 */
export function quickAddRibs(context is Context, definition is map) returns array
{
    return quickAddRibsOf(definition, pointsOf(context, definition.quickSelections));
}

/**
 * The ribs Quick add would add through `points` (see `quickAddRibs`): a chain through them, or a hub (the first) and
 * its spokes.
 */
export function quickAddRibsOf(definition is map, points is array) returns array
{
    if (size(points) < 2)
    {
        return [];
    }
    if (definition.quickRibType == QuickRibType.CHAIN)
    {
        return ribsOf(chainSegments(points, definition.quickClosed), []);
    }
    const circles = definition.quickAddCircle ? [{ "center" : points[0], "through" : points[1] }] : [];
    return ribsOf(hubSegments(points[0], subArray(points, 1)), circles);
}

/**
 * Shows `ribs` (see `getQuickRibs`) on `plane` in `color` while the feature's edited, adding nothing: as std's
 * `addDebugLine` does, they're sketched in a subfeature under `id`, shown, and rolled back.
 */
function showRibs(context is Context, id is Id, plane is Plane, ribs is array, color is DebugColor)
{
    if (ribs == [])
    {
        return;
    }
    startFeature(context, id, {});
    try silent
    {
        addDebugEntities(context, sketchQuickRibs(context, id + "sketch", plane, ribs), color);
    }
    abortFeature(context, id);
}

/**
 * Sketches `ribs` (see `getQuickRibs`) on `plane` under `id`, points projected onto it.
 * @returns {Query} : The sketch's edges (nothing, without any ribs).
 */
export function sketchQuickRibs(context is Context, id is Id, plane is Plane, ribs is array) returns Query
{
    if (ribs == [])
    {
        return qNothing();
    }
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    for (var i, rib in ribs)
    {
        if (rib.line != undefined)
        {
            skLineSegment(sketch, "rib" ~ i, { "start" : worldToPlane(plane, rib.line[0]), "end" : worldToPlane(plane, rib.line[1]) });
        }
        else
        {
            skCircle(sketch, "rib" ~ i, { "center" : worldToPlane(plane, rib.circle.center),
                        "radius" : hubCircleRadius(plane, rib.circle.center, rib.circle.through) });
        }
    }
    skSolve(sketch);
    return qCreatedBy(id, EntityType.EDGE);
}

/**
 * A line rib's key, which stays the same while its ends do (to a micrometer), either way round: for remembering which
 * are deleted.
 */
export function lineKey(segment is array) returns string
{
    const first = pointKey(segment[0]);
    const second = pointKey(segment[1]);
    // Either way round: the ends in a set order
    return precedes(segment[0], segment[1]) ? "line " ~ first ~ " " ~ second : "line " ~ second ~ " " ~ first;
}

/**
 * A hub circle's key (see `lineKey`): its center and radius.
 */
export function circleKey(center is Vector, through is Vector) returns string
{
    return "circle " ~ pointKey(center) ~ " " ~ roundToPrecision(norm(through - center) / meter, 6);
}

function pointKey(point is Vector) returns string
{
    return "(" ~ roundToPrecision(point[0] / meter, 6) ~ ", " ~ roundToPrecision(point[1] / meter, 6) ~ ", " ~ roundToPrecision(point[2] / meter, 6) ~ ")";
}

/**
 * Whether `a` comes before `b`, by x, then y, then z (to a micrometer, as `pointKey` rounds them).
 */
function precedes(a is Vector, b is Vector) returns boolean
{
    for (var i in [0, 1, 2])
    {
        const difference = roundToPrecision(a[i] / meter, 6) - roundToPrecision(b[i] / meter, 6);
        if (difference != 0)
        {
            return difference < 0;
        }
    }
    return false;
}

/**
 * Where a rib's toggle point is, to delete it (see `addDeleteRibsManipulator`): a line's middle, or the point on a
 * circle across from its first spoke, on `plane`.
 */
export function ribTogglePoint(plane is Plane, rib is map) returns Vector
{
    if (rib.line != undefined)
    {
        return project(plane, (rib.line[0] + rib.line[1]) / 2);
    }
    const center = project(plane, rib.circle.center);
    const radius = hubCircleRadius(plane, rib.circle.center, rib.circle.through);
    const away = center - project(plane, rib.circle.through);
    const direction = tolerantEquals(norm(away), 0 * meter) ? plane.x : normalize(away);
    return center + direction * radius;
}

const DELETE_RIBS_MANIPULATOR = "deleteRibs";

/**
 * The indices of `ribs` whose keys are in `toDelete`: those toggled to be deleted.
 */
export function toggledRibs(ribs is array, toDelete is array) returns array
{
    var toggled = [];
    for (var i, rib in ribs)
    {
        if (isIn(rib.key, toDelete))
        {
            toggled = append(toggled, i);
        }
    }
    return toggled;
}

/**
 * A toggle point on each of `ribs` (see `ribTogglePoint`), selected for those whose keys are in `toDelete`, as std's
 * patterns' Skip instances do.
 */
export function addDeleteRibsManipulator(context is Context, id is Id, plane is Plane, ribs is array, toDelete is array)
{
    addManipulators(context, id, {
                (DELETE_RIBS_MANIPULATOR) : togglePointsManipulator({
                        "points" : mapArray(ribs, rib => ribTogglePoint(plane, rib)),
                        "selectedIndices" : toggledRibs(ribs, toDelete),
                        "suppressedIndices" : []
                    })
            });
}

/**
 * Clicking ribs' toggle points (see `addDeleteRibsManipulator`) sets which are to be deleted (`ribsToDelete`), by key.
 */
export function deleteRibsManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    const points = newManipulators[DELETE_RIBS_MANIPULATOR];
    if (points == undefined)
    {
        return definition;
    }
    const ribs = getQuickRibs(context, definition).ribs;
    definition.ribsToDelete = mapArray(filter(points.selectedIndices, index => index < size(ribs)), index => ribs[index].key);
    return definition;
}

/**
 * The points of a selection, in order: a mate connector's origin, a vertex's point, or a circle's or arc's center. What
 * isn't there any more is left out.
 */
function pointsOf(context is Context, selection is Query) returns array
{
    var points = [];
    for (var entity in evaluateQuery(context, selection))
    {
        try silent
        {
            points = append(points, pointOf(context, entity));
        }
    }
    return points;
}

function pointOf(context is Context, entity is Query) returns Vector
{
    if (!isQueryEmpty(context, entity->qBodyType(BodyType.MATE_CONNECTOR)))
    {
        return evMateConnector(context, { "mateConnector" : entity }).origin;
    }
    if (!isQueryEmpty(context, entity->qEntityFilter(EntityType.VERTEX)))
    {
        return evVertexPoint(context, { "vertex" : entity });
    }
    return evCurveDefinition(context, { "edge" : entity }).coordSystem.origin;
}

/**
 * An edge rib's lines (each `[from, to]`): from each of its points to its edge, where the edge's nearest to it
 * (Perpendicular to edge), or all to its location along it (Manipulator). None if its edge or points aren't chosen (or
 * there).
 */
function edgeRibSegments(context is Context, edgeRib is map) returns array
{
    const starts = pointsOf(context, edgeRib.ribPoints);
    if (starts == [] || isQueryEmpty(context, edgeRib.ribEdge))
    {
        return [];
    }
    try silent
    {
        if (edgeRib.ribEnd == EdgeRibEnd.PERPENDICULAR)
        {
            return mapArray(starts, start => [start, evDistance(context, { "side0" : start, "side1" : edgeRib.ribEdge }).sides[1].point]);
        }
        const end = evEdgeTangentLine(context, { "edge" : edgeRib.ribEdge, "parameter" : edgeRib.edgeLocation, "arcLengthParameterization" : true }).origin;
        return mapArray(starts, start => [start, end]);
    }
    return [];
}

/**
 * The lines (each `[from, to]`) along a chain's points, back to the start if it's closed (and has more than two).
 */
export function chainSegments(points is array, closed is boolean) returns array
{
    var segments = [];
    const count = size(points);
    for (var i = 0; i < count - 1; i += 1)
    {
        segments = append(segments, [points[i], points[i + 1]]);
    }
    if (closed && count > 2)
    {
        segments = append(segments, [points[count - 1], points[0]]);
    }
    return segments;
}

/**
 * The lines (each `[from, to]`) from a hub to each of its spokes.
 */
export function hubSegments(hub is Vector, spokes is array) returns array
{
    return mapArray(spokes, spoke => [hub, spoke]);
}

/**
 * The radius of a hub's circle (Add circle), around the hub through its first spoke: how far apart they are in the
 * sketch's plane (projected onto it, as the sketch is).
 */
export function hubCircleRadius(plane is Plane, hub is Vector, firstSpoke is Vector) returns ValueWithUnits
{
    return norm(worldToPlane(plane, firstSpoke) - worldToPlane(plane, hub));
}

/**
 * Segments without repeats (either way round) or zero length ones.
 */
export function uniqueSegments(segments is array) returns array
{
    var unique = [];
    for (var segment in segments)
    {
        if (tolerantEquals(segment[0], segment[1]))
        {
            continue;
        }
        if (!any(unique, other => (tolerantEquals(other[0], segment[0]) && tolerantEquals(other[1], segment[1])) ||
                        (tolerantEquals(other[0], segment[1]) && tolerantEquals(other[1], segment[0]))))
        {
            unique = append(unique, segment);
        }
    }
    return unique;
}

/**
 * A manipulator on the end of each edge rib with Manipulator, to drag it along its edge (as std's fillet does its
 * points on edges).
 */
export function addEdgeRibManipulators(context is Context, id is Id, definition is map)
{
    for (var i, edgeRib in definition.edgeRibs)
    {
        if (edgeRib.ribEnd != EdgeRibEnd.MANIPULATOR)
        {
            continue;
        }
        try silent
        {
            const lines = evEdgeTangentLines(context, { "edge" : edgeRib.ribEdge, "parameters" : [0, edgeRib.edgeLocation, 1],
                        "arcLengthParameterization" : true });
            addManipulators(context, id, { (EDGE_RIB_MANIPULATOR ~ "." ~ i) : linearManipulator({
                                "base" : lines[1].origin,
                                "direction" : lines[1].direction,
                                "offset" : 0 * meter,
                                "minValue" : -norm(lines[0].origin - lines[1].origin),
                                "maxValue" : norm(lines[2].origin - lines[1].origin),
                                "style" : ManipulatorStyleEnum.TANGENTIAL
                            }) });
        }
    }
}

/**
 * Dragging an edge rib's end along its edge sets its location: the edge's nearest point to where it's dragged.
 */
export function edgeRibManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    for (var key, manipulator in newManipulators)
    {
        const index = edgeRibManipulatorIndex(key);
        if (index == undefined || index >= size(definition.edgeRibs))
        {
            continue;
        }
        const position = manipulator.base + manipulator.direction * manipulator.offset;
        const nearest = evDistance(context, { "side0" : position, "side1" : definition.edgeRibs[index].ribEdge, "arcLengthParameterization" : true });
        definition.edgeRibs[index].edgeLocation = clamp(nearest.sides[1].parameter, EDGE_RIB_LOCATION_RANGE[0], EDGE_RIB_LOCATION_RANGE[2]);
    }
    return definition;
}

/**
 * The edge rib a manipulator's key is for (`edgeRib.<index>`), or undefined.
 */
export function edgeRibManipulatorIndex(key is string)
{
    const found = match(key, EDGE_RIB_MANIPULATOR ~ "\\.(\\d+)");
    return found.hasMatch ? stringToNumber(found.captures[1]) : undefined;
}

/**
 * The face under the first quick rib selection which has one (see `faceUnder`), in order: Quick add's selections, the
 * chains', the hubs' and their spokes', and the edge ribs' points and edges. Undefined if none do.
 */
export function firstFaceUnder(context is Context, definition is map, hiddenBodies is Query)
{
    const chains = definition.chains;
    const hubs = definition.hubs;
    const edgeRibs = definition.edgeRibs;
    const selections = concatenateArrays([[definition.quickSelections], mapArray(chains, chain => chain.chainPoints), mapArray(hubs, hub => hub.hub),
                mapArray(hubs, hub => hub.spokes), mapArray(edgeRibs, edgeRib => edgeRib.ribPoints), mapArray(edgeRibs, edgeRib => edgeRib.ribEdge)]);
    for (var selection in selections)
    {
        for (var entity in evaluateQuery(context, selection))
        {
            // Editing logic mustn't throw: what isn't there any more is passed over
            try silent
            {
                const face = faceUnder(context, entity, hiddenBodies);
                if (face != undefined)
                {
                    return face;
                }
            }
        }
    }
    return undefined;
}

/**
 * The planar face (of a part which isn't hidden) in an entity's plane which is nearest its point, or undefined. Its
 * plane: a sketch entity's sketch plane, a circle's or arc's plane, or a mate connector's XY plane.
 */
function faceUnder(context is Context, entity is Query, hiddenBodies is Query)
{
    var sketchPlane;
    if (!isQueryEmpty(context, entity->qBodyType(BodyType.MATE_CONNECTOR)))
    {
        const coordSystem = evMateConnector(context, { "mateConnector" : entity });
        sketchPlane = plane(coordSystem.origin, coordSystem.zAxis);
    }
    else if (!isQueryEmpty(context, entity->qSketchFilter(SketchObject.YES)))
    {
        sketchPlane = evOwnerSketchPlane(context, { "entity" : entity });
    }
    else if (!isQueryEmpty(context, entity->qEntityFilter(EntityType.EDGE)))
    {
        const curve = evCurveDefinition(context, { "edge" : entity });
        if (!(curve is Circle))
        {
            return undefined;
        }
        sketchPlane = plane(curve.coordSystem.origin, curve.coordSystem.zAxis);
    }
    else
    {
        return undefined;
    }
    const faces = qOwnedByBody(qSubtraction(qAllModifiableSolidBodiesNoMesh(), hiddenBodies), EntityType.FACE)->qGeometry(GeometryType.PLANE);
    const nearest = evaluateQuery(context, qClosestTo(qCoincidesWithPlane(faces, sketchPlane), pointOf(context, entity)));
    return nearest == [] ? undefined : nearest[0];
}

/**
 * Clear invalid selections (a button): drops what's chosen (Quick add's selections too), but isn't there any more;
 * chains of fewer than two points; hubs without a hub; and edge ribs without an edge or points.
 */
export function clearInvalidSelections(context is Context, definition is map) returns map
{
    // Evaluated, to keep only what's there
    definition.quickSelections = qUnion(evaluateQuery(context, definition.quickSelections));
    var chains = [];
    for (var chain in definition.chains)
    {
        chain.chainPoints = qUnion(evaluateQuery(context, chain.chainPoints));
        if (evaluateQueryCount(context, chain.chainPoints) >= 2)
        {
            chains = append(chains, chain);
        }
    }
    var hubs = [];
    for (var hub in definition.hubs)
    {
        hub.hub = qUnion(evaluateQuery(context, hub.hub));
        hub.spokes = qUnion(evaluateQuery(context, hub.spokes));
        if (!isQueryEmpty(context, hub.hub))
        {
            hubs = append(hubs, hub);
        }
    }
    definition.chains = chains;
    definition.hubs = hubs;
    var edgeRibs = [];
    for (var edgeRib in definition.edgeRibs)
    {
        edgeRib.ribPoints = qUnion(evaluateQuery(context, edgeRib.ribPoints));
        if (!isQueryEmpty(context, edgeRib.ribPoints) && !isQueryEmpty(context, edgeRib.ribEdge))
        {
            edgeRibs = append(edgeRibs, edgeRib);
        }
    }
    definition.edgeRibs = edgeRibs;
    return definition;
}
