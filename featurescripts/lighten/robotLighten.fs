FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");

const WALL_BOUNDS = { (meter) : [1e-5, 0.003175, 500], (inch) : 0.125, (millimeter) : 3 } as LengthBoundSpec;
const RIB_BOUNDS = { (meter) : [1e-5, 0.003175, 500], (inch) : 0.125, (millimeter) : 3 } as LengthBoundSpec;
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
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotLighten = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Face to lighten", "MaxNumberOfPicks" : 1,
                    "Filter" : EntityType.FACE && GeometryType.PLANE && BodyType.SOLID && SketchObject.NO && ModifiableEntityOnly.YES }
        definition.face is Query;

        annotation { "Name" : "Ribs to use", "Filter" : EntityType.EDGE && SketchObject.YES }
        definition.ribEdges is Query;

        annotation { "Name" : "Exclude construction lines", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.excludeConstruction is boolean;

        annotation { "Name" : "Wall thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.wallThickness, WALL_BOUNDS);

        annotation { "Name" : "Rib thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.ribThickness, RIB_BOUNDS);

        annotation { "Name" : "End type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"] }
        definition.endType is LightenEndType;

        if (isBlind(definition))
        {
            annotation { "Name" : "Depth", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.depth, DEPTH_BOUNDS);
        }

        annotation { "Name" : "Fillet corners", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.filletCorners is boolean;

        if (definition.filletCorners)
        {
            annotation { "Name" : "Fillet radius", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.filletRadius, FILLET_RADIUS_BOUNDS);
        }

        annotation { "Group Name" : "Ignored faces", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Faces to ignore",
                        "Filter" : EntityType.FACE && ((BodyType.SOLID && ModifiableEntityOnly.YES) || (SketchObject.YES && ConstructionObject.NO)) }
            definition.ignoredFaces is Query;
        }
    }
    {
        const face = getFace(context, definition);
        // Its normal points out of its part, so pockets go against it
        const facePlane = evPlane(context, { "face" : face });
        const part = qUnion(evaluateQuery(context, qOwnerBody(face)));
        const ribEdges = getRibEdges(context, definition);
        verifyParallel(context, face, facePlane, ribPlane(context, ribEdges));
        // To round the pockets' corners, they're made with walls and ribs this much thicker, then grown back by it (see
        // `roundPockets`)
        const radius = definition.filletCorners ? definition.filletRadius : 0 * meter;

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
        const ends = qUnion(evaluateQuery(context, qCapEntity(id + "pocket", CapType.EITHER, EntityType.FACE)));
        // The pockets' ends, through the walls, ribs, and rounding (see `pocketEnds`)
        const trackedEnds = startTracking(context, ends);

        // Sketch regions to ignore are left solid: cut from the pocket, and walled like its edges
        const ignoredRegions = qSketchFilter(definition.ignoredFaces, SketchObject.YES);
        excludeRegions(context, id + "excludeRegions", facePlane, pocketBodies(context, id), ignoredRegions);

        // The walls: along the face's edges, but those it shares with the part's faces to ignore, and around the regions
        // to ignore
        const ignoredEdges = qIntersection([qLoopEdges(face), qLoopEdges(qSketchFilter(definition.ignoredFaces, SketchObject.NO))]);
        cutWalls(context, id + "walls", facePlane, pocketBodies(context, id), qUnion([qSubtraction(qLoopEdges(face), ignoredEdges), qLoopEdges(ignoredRegions)]),
            ignoredEdges, definition.wallThickness + radius);

        // The ribs, cut from what's left
        const inset = pocketBodies(context, id);
        const ribs = buildRibs(context, id + "ribs", bandExtent(context, facePlane, qUnion([inset, ribEdges])), ribEdges,
            definition.ribThickness / 2 + radius);
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
            throw regenError("Failed to cut ribs.", ["ribEdges", "ribThickness"], failing);
        }
        // The pockets left between them, which the walls and ribs split into pieces
        const pockets = pocketBodies(context, id);

        if (definition.filletCorners)
        {
            roundPockets(context, id, pocketEnds(pockets, facePlane, trackedEnds), pockets, radius);
        }

        if (isQueryEmpty(context, pockets))
        {
            reportFeatureWarning(context, id, "There's no room for pockets between the walls and ribs.",
                ["wallThickness", "ribThickness", "filletRadius"]);
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
                reportFeatureWarning(context, id, "Some ribs touch no wall or other rib, so they're left as loose parts.", ["ribEdges"]);
                setErrorEntities(context, id, { "entities" : loose });
            }
            else
            {
                // Only without the warning, which this would replace
                const lightened = 1 - evVolume(context, { "entities" : part }) / volume;
                reportFeatureInfo(context, id, "Lightened the part by " ~ roundToPrecision(lightened * 100, 1) ~ "%.");
            }
        }

        opDeleteBodies(context, id + "cleanup", {
                    "entities" : qUnion([pockets, qCreatedBy(id, EntityType.BODY)->qSketchFilter(SketchObject.YES), qCreatedBy(id + "ribs", EntityType.BODY)])
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
 * Throws unless the face to lighten is parallel to the ribs' sketches, so the ribs go straight into it.
 */
function verifyParallel(context is Context, face is Query, facePlane is Plane, ribs is Plane)
{
    if (!parallelVectors(facePlane.normal, ribs.normal))
    {
        throw regenError("The face to lighten must be parallel to the ribs.", ["face", "ribEdges"], face);
    }
}

/**
 * The pockets so far: the solids made under `id`, the pocket and the pieces walls and ribs split it into (their tools,
 * the walls' and ribs' bodies, are used up by the booleans which cut them).
 */
function pocketBodies(context is Context, id is Id) returns Query
{
    return qUnion(evaluateQuery(context, qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID)));
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
        edges = getRibEdges(context, definition);
        plane = ribPlane(context, edges);
    }
    if (plane != undefined)
    {
        const faces = evaluateQuery(context, facesUnder(context, plane, edges, hiddenBodies));
        if (size(faces) == 1)
        {
            definition.face = faces[0];
        }
    }
    return definition;
}

/**
 * Cuts walls `width` thick along `edges` from `pockets`, under `id`: a capsule around each edge (`width` to each side,
 * with round ends), all in one sketch on `facePlane`, extruded through the pockets and cut from them. Unlike hollowing
 * the pockets (which can't split one into several), this leaves however many pockets there's room for: a hole near
 * the edge, or two near each other, just leave no pocket between them. Ends at `ignoredEdges` (the sides along faces
 * to ignore) are left square, so the walls stop there.
 *
 * The capsules' outlines split the sketch into regions, some of them outside every capsule (like the face's middle);
 * the walls are the regions closer to the edges than `width` (see `isWallRegion`).
 */
function cutWalls(context is Context, id is Id, facePlane is Plane, pockets is Query, edges is Query, ignoredEdges is Query, width is ValueWithUnits)
{
    if (isQueryEmpty(context, edges) || isQueryEmpty(context, pockets))
    {
        return;
    }
    const extent = bandExtent(context, facePlane, qUnion([pockets, edges]));
    const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : extent.plane });
    const squareEnds = mapArray(evaluateQuery(context, qAdjacent(ignoredEdges, AdjacencyType.VERTEX, EntityType.VERTEX)), function(vertex)
        {
            return worldToPlane(extent.plane, evVertexPoint(context, { "vertex" : vertex }));
        });
    sketchWalls(sketch, wallCurves(context, edges, extent.plane, width), width, squareEnds);
    skSolve(sketch);

    const centerlines = qCreatedBy(id + "sketch", EntityType.EDGE)->qConstructionFilter(ConstructionObject.YES);
    const regions = filter(evaluateQuery(context, qSketchRegion(id + "sketch")), function(region)
        {
            return isWallRegion(context, region, centerlines, width);
        });
    if (regions == [])
    {
        return;
    }
    try
    {
        opExtrude(context, id + "extrude", {
                    "entities" : qUnion(regions),
                    "direction" : extent.plane.normal,
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : extent.depth,
                    "startBound" : BoundingType.BLIND,
                    "startDepth" : extent.depth
                });
    }
    catch
    {
        throw regenError("Failed to extrude walls.", ["wallThickness"], qUnion(regions));
    }
    const walls = qCreatedBy(id + "extrude", EntityType.BODY);
    try
    {
        opBoolean(context, id + "cut", {
                    "targets" : pockets,
                    "tools" : walls,
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    }
    catch
    {
        // A failed boolean changes nothing, so the walls are still there to show
        throw regenError("Failed to cut walls.", ["wallThickness"], walls);
    }
}

/**
 * Whether a region of the walls' sketch is wall: closer to the edges (their `centerlines`, drawn in the sketch) than
 * `width`. A region inside a capsule has points closer than that; one outside every capsule (like the face's middle,
 * or a hole's) is at least `width` from all of them, touching the capsules' outlines.
 */
function isWallRegion(context is Context, region is Query, centerlines is Query, width is ValueWithUnits) returns boolean
{
    const distance = evDistance(context, { "side0" : region, "side1" : centerlines }).distance;
    return distance < width - 1e-6 * meter;
}

/**
 * The walls' edges, as curves in `plane`'s coordinates, for `sketchWalls`: lines (`"points"`, their ends), arcs
 * (`"center"`, `"radius"`, and `"points"`, their start, middle, and end), full circles (`"center"` and `"radius"`), and
 * anything else as polylines (`"points"`), through enough points to be within a few percent of `width` of the curve.
 */
function wallCurves(context is Context, edges is Query, plane is Plane, width is ValueWithUnits) returns array
{
    return mapArray(evaluateQuery(context, edges), function(edge)
        {
            const curve = evCurveDefinition(context, { "edge" : edge });
            const ends = mapArray(evEdgeTangentLines(context, { "edge" : edge, "parameters" : [0, 0.5, 1] }), function(line)
                {
                    return worldToPlane(plane, line.origin);
                });
            if (curve is Line)
            {
                return { "kind" : "line", "points" : [ends[0], ends[2]] };
            }
            if (curve is Circle)
            {
                const center = worldToPlane(plane, curve.coordSystem.origin);
                if (tolerantEquals(ends[0], ends[2]))
                {
                    return { "kind" : "circle", "center" : center, "radius" : curve.radius };
                }
                return { "kind" : "arc", "center" : center, "radius" : curve.radius, "points" : ends };
            }
            const count = min(max(ceil(evLength(context, { "entities" : edge }) / (width / 2)), 8), 128);
            const parameters = mapArray(range(0, count), i => i / count);
            return { "kind" : "polyline", "points" : mapArray(evEdgeTangentLines(context, { "edge" : edge, "parameters" : parameters }), function(line)
                        {
                            return worldToPlane(plane, line.origin);
                        }) };
        });
}

/**
 * Sketches the walls' capsules around `curves` (see `wallCurves`), `width` to each side: each curve's sides (lines'
 * offsets, arcs' and circles' concentric arcs, or for those no bigger than `width`, wedges to their centers), round
 * ends (a circle at each end, but those at `squareEnds`, which get straight ends), and the curve itself, as
 * construction, to tell the walls' regions by (see `isWallRegion`).
 */
export function sketchWalls(sketch is Sketch, curves is array, width is ValueWithUnits, squareEnds is array)
{
    var ends = [];
    for (var i, curve in curves)
    {
        const tag = "wall" ~ i;
        if (curve.kind == "circle")
        {
            skCircle(sketch, tag ~ "center", { "center" : curve.center, "radius" : curve.radius, "construction" : true });
            skCircle(sketch, tag ~ "outer", { "center" : curve.center, "radius" : curve.radius + width });
            if (curve.radius > width)
            {
                skCircle(sketch, tag ~ "inner", { "center" : curve.center, "radius" : curve.radius - width });
            }
            continue;
        }
        if (curve.kind == "arc")
        {
            sketchArcWall(sketch, tag, curve, width, squareEnds);
        }
        else
        {
            const points = curve.points;
            for (var j = 0; j < size(points) - 1; j += 1)
            {
                skLineSegment(sketch, tag ~ "center" ~ j, { "start" : points[j], "end" : points[j + 1], "construction" : true });
                const side = perpendicular(points[j + 1] - points[j]) * width;
                skLineSegment(sketch, tag ~ "left" ~ j, { "start" : points[j] + side, "end" : points[j + 1] + side });
                skLineSegment(sketch, tag ~ "right" ~ j, { "start" : points[j] - side, "end" : points[j + 1] - side });
                // A polyline's bends are rounded too
                if (j > 0)
                {
                    ends = append(ends, points[j]);
                }
            }
            for (var end in [[points[0], points[1]], [points[size(points) - 1], points[size(points) - 2]]])
            {
                if (isAt(end[0], squareEnds))
                {
                    const side = perpendicular(end[1] - end[0]) * width;
                    skLineSegment(sketch, tag ~ "square" ~ size(ends), { "start" : end[0] + side, "end" : end[0] - side });
                }
                ends = append(ends, end[0]);
            }
        }
        if (curve.kind == "arc")
        {
            ends = concatenateArrays([ends, [curve.points[0], curve.points[2]]]);
        }
    }

    // A circle at each end (once, where curves meet), but the square ones
    var circled = [];
    for (var end in ends)
    {
        if (!isAt(end, squareEnds) && !isAt(end, circled))
        {
            skCircle(sketch, "end" ~ size(circled), { "center" : end, "radius" : width });
            circled = append(circled, end);
        }
    }
}

/**
 * An arc's capsule: arcs `width` outside and inside it (or for an arc no bigger than `width`, lines from its center to
 * the outer arc's ends), and straight ends at `squareEnds`.
 */
function sketchArcWall(sketch is Sketch, tag is string, curve is map, width is ValueWithUnits, squareEnds is array)
{
    const scaled = function(radius is ValueWithUnits) returns array
        {
            return mapArray(curve.points, point => curve.center + (point - curve.center) * (radius / curve.radius));
        };
    skArc(sketch, tag ~ "center", { "start" : curve.points[0], "mid" : curve.points[1], "end" : curve.points[2], "construction" : true });
    const outer = scaled(curve.radius + width);
    skArc(sketch, tag ~ "outer", { "start" : outer[0], "mid" : outer[1], "end" : outer[2] });
    const small = curve.radius <= width;
    const inner = small ? [curve.center, curve.center, curve.center] : scaled(curve.radius - width);
    if (!small)
    {
        skArc(sketch, tag ~ "inner", { "start" : inner[0], "mid" : inner[1], "end" : inner[2] });
    }
    for (var k in [0, 2])
    {
        // A small arc's wedge has its sides; others have them only at square ends (round ones are inside the circle)
        if (small || isAt(curve.points[k], squareEnds))
        {
            skLineSegment(sketch, tag ~ "side" ~ k, { "start" : inner[k], "end" : outer[k] });
        }
    }
}

/**
 * The unit vector a quarter turn counterclockwise from `direction`.
 */
function perpendicular(direction is Vector) returns Vector
{
    const unit = normalize(direction);
    return vector(-unit[1], unit[0]);
}

/**
 * Whether `point` is at any of `points`.
 */
function isAt(point is Vector, points is array) returns boolean
{
    return any(points, other => tolerantEquals(point, other));
}

/**
 * The ribs' sketch edges (their construction edges too, unless they're excluded).
 */
function getRibEdges(context is Context, definition is map) returns Query
{
    var edges = qEntityFilter(definition.ribEdges, EntityType.EDGE);
    if (definition.excludeConstruction)
    {
        edges = qConstructionFilter(edges, ConstructionObject.NO);
    }
    if (isQueryEmpty(context, edges))
    {
        throw regenError("Select ribs to use.", ["ribEdges"]);
    }
    return edges;
}

/**
 * The plane of the first rib's sketch. The ribs can be from any sketches, as long as they're parallel: each is
 * extruded through everything along their normal.
 */
function ribPlane(context is Context, edges is Query) returns Plane
{
    const evaluated = evaluateQuery(context, edges);
    const plane = evOwnerSketchPlane(context, { "entity" : evaluated[0] });
    const skewed = filter(evaluated, function(edge)
        {
            return !parallelVectors(evOwnerSketchPlane(context, { "entity" : edge }).normal, plane.normal);
        });
    if (skewed != [])
    {
        throw regenError("The ribs must be in parallel sketches.", ["ribEdges"], qUnion(skewed));
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
 * The ribs along `edges`, `halfWidth` to each side, through `extent` (see `bandExtent`), each made on its own: edges
 * extruded together make one sheet, creased where they meet, which can't be thickened. Each is its edge's sheet,
 * thickened; but an arc or circle hardly bigger than `halfWidth` can't be thickened toward its center, so its rib is a
 * cylinder around its center, `halfWidth` bigger than it, instead (a little more than its rib, near its center). A rib
 * which fails highlights its edge.
 */
function buildRibs(context is Context, id is Id, extent is map, edges is Query, halfWidth is ValueWithUnits) returns Query
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
                throw regenError("Failed to extrude rib.", ["ribEdges", "ribThickness"], edge);
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
            throw regenError("Failed to extrude rib.", ["ribEdges", "ribThickness"], edge);
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
            throw regenError("Failed to thicken rib.", ["ribEdges", "ribThickness"], edge);
        }
    }
    return qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID);
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
