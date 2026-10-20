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
        "Feature Type Description" : "Quickly sketch ribs for use in part lightening." ~ CREDIT,
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

            if (definition.quickRibType == QuickRibType.CHAIN)
            {
                annotation { "Name" : "Points", "Filter" : EntityType.VERTEX || (EntityType.EDGE && GeometryType.CIRCLE) || (EntityType.EDGE && GeometryType.ARC) || BodyType.MATE_CONNECTOR }
                definition.quickPoints is Query;

                annotation { "Name" : "Closed" }
                definition.quickClosed is boolean;
            }
            else
            {
                annotation { "Name" : "Hub", "MaxNumberOfPicks" : 1, "Filter" : EntityType.VERTEX || (EntityType.EDGE && GeometryType.CIRCLE) || (EntityType.EDGE && GeometryType.ARC) || BodyType.MATE_CONNECTOR }
                definition.quickHub is Query;

                annotation { "Name" : "Spokes", "Filter" : EntityType.VERTEX || (EntityType.EDGE && GeometryType.CIRCLE) || (EntityType.EDGE && GeometryType.ARC) || BodyType.MATE_CONNECTOR }
                definition.quickSpokes is Query;

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

        // Whether ribs are being chosen to delete, and those chosen so far (indices into `getQuickRibs`'s ribs): set by
        // editing logic and the toggle points
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
        sketchQuickRibs(context, id + "ribSketch", plane, quickRibs.ribs);
        if (quickAdd != [])
        {
            reportFeatureInfo(context, id, "Click add ribs to add the highlighted ribs.");
        }
        if (definition.deletingRibs)
        {
            const toDelete = filter(ribIndices(definition.ribsToDelete), index => index < size(quickRibs.ribs));
            addDeleteRibsManipulator(context, id, plane, quickRibs.ribs, toDelete);
            // Those toggled, in red
            if (toDelete != [])
            {
                addDebugEntities(context, qUnion(mapArray(toDelete, index => sketchEntityQuery(id + "ribSketch", EntityType.EDGE, "rib" ~ index))),
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
 * Add ribs adds Quick add's chain or hub and spokes (`addQuickRibs`), when it makes ribs (`quickAddRibs`). Clear invalid selections
 * drops invalid selections (`clearInvalidSelections`). Start deleting ribs starts choosing ribs to delete; Finish
 * deleting ribs deletes those chosen, by deleting what makes them (`deleteRibs`). Fills in Face to use, while
 * it's empty and hasn't been set: the face under the first selection (`firstFaceUnder`).
 */
export function quickRibEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query, clickedButton is string) returns map
{
    if (clickedButton == "addRibs" && quickAddRibs(context, definition) != [])
    {
        definition = addQuickRibs(definition);
    }
    else if (clickedButton == "clearInvalid")
    {
        definition = clearInvalidSelections(context, definition);
    }
    else if (clickedButton == "startDeleting")
    {
        definition.deletingRibs = true;
        definition.ribsToDelete = [];
    }
    else if (clickedButton == "finishDeleting")
    {
        definition = deleteRibs(context, definition, ribIndices(definition.ribsToDelete));
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
 * Add ribs (a button): Quick add's Points and Closed become a chain, or its Hub, Spokes, and Add circle a hub and
 * spokes; then its selections are cleared, for the next.
 */
export function addQuickRibs(definition is map) returns map
{
    if (definition.quickRibType == QuickRibType.CHAIN)
    {
        definition.chains = append(definition.chains, { "chainPoints" : definition.quickPoints, "closed" : definition.quickClosed });
        definition.quickPoints = qNothing();
    }
    else
    {
        definition.hubs = append(definition.hubs, { "hub" : definition.quickHub, "spokes" : definition.quickSpokes,
                    "addCircle" : definition.quickAddCircle });
        definition.quickHub = qNothing();
        definition.quickSpokes = qNothing();
    }
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
 *      @field ribs {array} : Each a map of its `line` (`[from, to]`) or `circle` (its `center`, and the `through`
 *              point it goes through), and its `sources`: what makes it, for deleting it (see `deleteRibSources`).
 *      @field skipped {number} : How many chains, hubs, and edge ribs were skipped.
 * }}
 */
export function getQuickRibs(context is Context, definition is map) returns map
{
    var parts = [];
    var skipped = 0;
    for (var i, chain in definition.chains)
    {
        const points = pointsOf(context, chain.chainPoints);
        if (size(points) < 2)
        {
            skipped += 1;
            continue;
        }
        parts = concatenateArrays([parts, chainParts(points, chain.closed, { "chain" : i })]);
    }
    for (var i, hub in definition.hubs)
    {
        const center = pointsOf(context, hub.hub);
        const spokes = pointsOf(context, hub.spokes);
        if (center == [] || spokes == [])
        {
            skipped += 1;
            continue;
        }
        parts = concatenateArrays([parts, hubParts(center[0], spokes, hub.addCircle, { "hub" : i })]);
    }
    for (var i, edgeRib in definition.edgeRibs)
    {
        const segments = edgeRibSegments(context, edgeRib);
        if (segments == [])
        {
            skipped += 1;
            continue;
        }
        for (var j, segment in segments)
        {
            parts = append(parts, { "line" : segment, "source" : { "edgeRib" : i, "point" : j } });
        }
    }
    return { "ribs" : ribsOf(parts), "skipped" : skipped };
}

/**
 * A chain's lines through `points` (see `chainSegments`), each from `source` (which chain) and its `segment`: its
 * index, the closing one last.
 */
export function chainParts(points is array, closed is boolean, source is map) returns array
{
    var parts = [];
    for (var j, segment in chainSegments(points, closed))
    {
        parts = append(parts, { "line" : segment, "source" : mergeMaps(source, { "segment" : j }) });
    }
    return parts;
}

/**
 * A hub and spokes' lines (see `hubSegments`), each from `source` (which hub) and its `spoke`'s index, and with
 * `addCircle`, its circle around the hub through the first spoke, from `source` and `circle`.
 */
export function hubParts(hub is Vector, spokes is array, addCircle is boolean, source is map) returns array
{
    var parts = [];
    for (var j, segment in hubSegments(hub, spokes))
    {
        parts = append(parts, { "line" : segment, "source" : mergeMaps(source, { "spoke" : j }) });
    }
    if (addCircle)
    {
        parts = append(parts, { "circle" : { "center" : hub, "through" : spokes[0] }, "source" : mergeMaps(source, { "circle" : true }) });
    }
    return parts;
}

/**
 * Ribs (see `getQuickRibs`) from parts (each a `line` or a `circle`, and its `source`), lines first: the same rib
 * (`sameRib`) is one, with each's source, and lines with no length are dropped.
 */
export function ribsOf(parts is array) returns array
{
    var ribs = [];
    for (var part in concatenateArrays([filter(parts, part => part.line != undefined), filter(parts, part => part.circle != undefined)]))
    {
        if (part.line != undefined && tolerantEquals(part.line[0], part.line[1]))
        {
            continue;
        }
        var found = false;
        for (var i, rib in ribs)
        {
            if (sameRib(rib, part))
            {
                ribs[i].sources = append(rib.sources, part.source);
                found = true;
                break;
            }
        }
        if (!found)
        {
            ribs = append(ribs, part.line != undefined ? { "line" : part.line, "sources" : [part.source] } :
                    { "circle" : part.circle, "sources" : [part.source] });
        }
    }
    return ribs;
}

/**
 * Whether two ribs (or parts, see `ribsOf`) are the same: lines with the same ends, either way round, or circles with
 * the same center and radius.
 */
function sameRib(a is map, b is map) returns boolean
{
    if (a.line != undefined && b.line != undefined)
    {
        return (tolerantEquals(a.line[0], b.line[0]) && tolerantEquals(a.line[1], b.line[1])) ||
            (tolerantEquals(a.line[0], b.line[1]) && tolerantEquals(a.line[1], b.line[0]));
    }
    if (a.circle != undefined && b.circle != undefined)
    {
        return tolerantEquals(a.circle.center, b.circle.center) &&
            tolerantEquals(norm(a.circle.through - a.circle.center), norm(b.circle.through - b.circle.center));
    }
    return false;
}

/**
 * The ribs Quick add would add (see `addQuickRibs`), from its selections as they are (see `getQuickRibs`): its chain's
 * (`chainRibs`), or its hub and spokes' (`hubRibs`).
 */
export function quickAddRibs(context is Context, definition is map) returns array
{
    if (definition.quickRibType == QuickRibType.CHAIN)
    {
        return chainRibs(pointsOf(context, definition.quickPoints), definition.quickClosed);
    }
    return hubRibs(pointsOf(context, definition.quickHub), pointsOf(context, definition.quickSpokes), definition.quickAddCircle);
}

/**
 * A chain's ribs (see `getQuickRibs`) through `points`: none with fewer than two.
 */
export function chainRibs(points is array, closed is boolean) returns array
{
    return ribsOf(chainParts(points, closed, {}));
}

/**
 * A hub and spokes' ribs (see `getQuickRibs`): from `hub` (the hub's point, or none) to each of `spokes`, and with
 * `addCircle`, a circle around it through the first. None without a hub or spokes.
 */
export function hubRibs(hub is array, spokes is array, addCircle is boolean) returns array
{
    if (hub == [] || spokes == [])
    {
        return [];
    }
    return ribsOf(hubParts(hub[0], spokes, addCircle, {}));
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
 * The rib indices in Ribs to delete: none until it's set, when it's a number (an `isAnything` parameter's unset value),
 * not undefined.
 */
export function ribIndices(value) returns array
{
    return value is array ? value : [];
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
 * A toggle point on each of `ribs` (see `ribTogglePoint`), selected for those at `toDelete` (indices), as std's
 * patterns' Skip instances do.
 */
export function addDeleteRibsManipulator(context is Context, id is Id, plane is Plane, ribs is array, toDelete is array)
{
    addManipulators(context, id, {
                (DELETE_RIBS_MANIPULATOR) : togglePointsManipulator({
                        "points" : mapArray(ribs, rib => ribTogglePoint(plane, rib)),
                        "selectedIndices" : toDelete,
                        "suppressedIndices" : []
                    })
            });
}

/**
 * Clicking ribs' toggle points (see `addDeleteRibsManipulator`) sets which are to be deleted (`ribsToDelete`), by index.
 */
export function deleteRibsManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    const points = newManipulators[DELETE_RIBS_MANIPULATOR];
    if (points == undefined)
    {
        return definition;
    }
    definition.ribsToDelete = points.selectedIndices;
    return definition;
}

/**
 * Finish deleting ribs (a button): deletes the ribs at `indices` (into `getQuickRibs`'s) by deleting what makes them
 * (see `deleteRibSources`). If the ribs have changed so an index is past them, nothing is deleted.
 */
export function deleteRibs(context is Context, definition is map, indices is array) returns map
{
    const ribs = getQuickRibs(context, definition).ribs;
    if (any(indices, index => index >= size(ribs)))
    {
        return definition;
    }
    const sources = concatenateArrays(mapArray(indices, index => ribs[index].sources));
    // The selections with points, as `getQuickRibs` finds them: sources' indices are into these
    return deleteRibSources(definition, {
                "chains" : mapArray(definition.chains, chain => pointEntitiesOf(context, chain.chainPoints)),
                "spokes" : mapArray(definition.hubs, hub => pointEntitiesOf(context, hub.spokes)),
                "edgeRibs" : mapArray(definition.edgeRibs, edgeRib => pointEntitiesOf(context, edgeRib.ribPoints))
            }, sources);
}

/**
 * Deletes what makes ribs (`sources`, see `getQuickRibs`), given `entities`: each chain's, hub's spokes', and edge
 * rib's selections with points (`chains`, `spokes`, `edgeRibs`). A chain is split where its segments are (see
 * `chainPieces`); a hub loses its spokes, and its circle (Add circle); an edge rib loses its points. Those left with no
 * ribs (a chain with fewer than two points, a hub without spokes, or an edge rib without points) are removed.
 */
export function deleteRibSources(definition is map, entities is map, sources is array) returns map
{
    var chains = [];
    for (var i, chain in definition.chains)
    {
        const segments = mapArray(filter(sources, source => source.chain == i), source => source.segment);
        if (segments == [])
        {
            chains = append(chains, chain);
            continue;
        }
        const points = entities.chains[i];
        for (var piece in chainPieces(size(points), chain.closed, segments))
        {
            chain.chainPoints = qUnion(mapArray(piece.indices, index => points[index]));
            chain.closed = piece.closed;
            chains = append(chains, chain);
        }
    }
    var hubs = [];
    for (var i, hub in definition.hubs)
    {
        const deleted = filter(sources, source => source.hub == i);
        const spokes = withoutIndices(entities.spokes[i], mapArray(filter(deleted, source => source.spoke != undefined), source => source.spoke));
        if (deleted == [])
        {
            hubs = append(hubs, hub);
        }
        else if (spokes != [])
        {
            hub.spokes = qUnion(spokes);
            hub.addCircle = hub.addCircle && !any(deleted, source => source.circle != undefined);
            hubs = append(hubs, hub);
        }
    }
    var edgeRibs = [];
    for (var i, edgeRib in definition.edgeRibs)
    {
        const deleted = mapArray(filter(sources, source => source.edgeRib == i), source => source.point);
        const points = withoutIndices(entities.edgeRibs[i], deleted);
        if (deleted == [])
        {
            edgeRibs = append(edgeRibs, edgeRib);
        }
        else if (points != [])
        {
            edgeRib.ribPoints = qUnion(points);
            edgeRibs = append(edgeRibs, edgeRib);
        }
    }
    definition.chains = chains;
    definition.hubs = hubs;
    definition.edgeRibs = edgeRibs;
    return definition;
}

/**
 * What's left of a chain of `count` points when its `segments` (indices, see `chainParts`) are deleted: the runs of
 * points between them, each its `indices` and whether it's `closed`. A closed chain opens at its last deleted segment,
 * running on around from it; runs of one point make no rib, and are dropped.
 */
export function chainPieces(count is number, closed is boolean, segments is array) returns array
{
    // An open chain has no closing segment: as if it were deleted
    const deleted = closed && count > 2 ? segments : append(segments, count - 1);
    if (!any(range(0, count - 1), segment => isIn(segment, deleted)))
    {
        return [{ "indices" : range(0, count - 1), "closed" : true }];
    }
    // Start after the last deleted segment, so no run wraps past it
    var last = 0;
    for (var segment in deleted)
    {
        last = max(last, segment);
    }
    var pieces = [];
    var run = [];
    for (var k = 0; k < count; k += 1)
    {
        const index = (last + 1 + k) % count;
        // The segment from the point before it is deleted
        if (run != [] && isIn((index + count - 1) % count, deleted))
        {
            pieces = append(pieces, run);
            run = [];
        }
        run = append(run, index);
    }
    pieces = append(pieces, run);
    return mapArray(filter(pieces, piece => size(piece) >= 2), piece => { "indices" : piece, "closed" : false });
}

/**
 * `items` without those at `indices`.
 */
function withoutIndices(items is array, indices is array) returns array
{
    var kept = [];
    for (var i, item in items)
    {
        if (!isIn(i, indices))
        {
            kept = append(kept, item);
        }
    }
    return kept;
}

/**
 * The points of a selection, in order (see `selectedPoints`).
 */
function pointsOf(context is Context, selection is Query) returns array
{
    return mapArray(selectedPoints(context, selection), found => found.point);
}

/**
 * The entities of a selection which have points, in order (see `selectedPoints`).
 */
function pointEntitiesOf(context is Context, selection is Query) returns array
{
    return mapArray(selectedPoints(context, selection), found => found.entity);
}

/**
 * A selection's entities and their points, in order, each its `entity` and `point`: a mate connector's origin, a
 * vertex's point, or a circle's or arc's center. What isn't there any more is left out.
 */
function selectedPoints(context is Context, selection is Query) returns array
{
    var found = [];
    for (var entity in evaluateQuery(context, selection))
    {
        try silent
        {
            found = append(found, { "entity" : entity, "point" : pointOf(context, entity) });
        }
    }
    return found;
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
 * The face under the first quick rib selection which has one (see `faceUnder`), in order: Quick add's Points, Hub, and
 * Spokes, the chains', the hubs' and their spokes', and the edge ribs' points and edges. Undefined if none do.
 */
export function firstFaceUnder(context is Context, definition is map, hiddenBodies is Query)
{
    const chains = definition.chains;
    const hubs = definition.hubs;
    const edgeRibs = definition.edgeRibs;
    const selections = concatenateArrays([[definition.quickPoints, definition.quickHub, definition.quickSpokes], mapArray(chains, chain => chain.chainPoints), mapArray(hubs, hub => hub.hub),
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
    definition.quickPoints = qUnion(evaluateQuery(context, definition.quickPoints));
    definition.quickHub = qUnion(evaluateQuery(context, definition.quickHub));
    definition.quickSpokes = qUnion(evaluateQuery(context, definition.quickSpokes));
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
