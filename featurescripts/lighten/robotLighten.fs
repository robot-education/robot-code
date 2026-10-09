FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "21762d39019c8b2289e2fbb8", version : "80768fbb394ad68f2a15753b");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");

const WALL_BOUNDS = { (meter) : [1e-5, 0.003175, 500], (inch) : 0.125, (millimeter) : 3 } as LengthBoundSpec;
const RIB_BOUNDS = { (meter) : [1e-5, 0.003175, 500], (inch) : 0.125, (millimeter) : 3 } as LengthBoundSpec;
// A 1/8 in. router bit's
const FILLET_RADIUS_BOUNDS = { (meter) : [1e-5, 0.0015875, 500], (inch) : 0.0625, (millimeter) : 1.5 } as LengthBoundSpec;

/**
 * Lightens parts with pockets: everything within the extrude of the faces to lighten (into their parts, as the end type
 * says) is cut away, but for walls along the faces' edges (their parts' sides and holes) and ribs along the selected
 * sketch edges, with the pockets' corners rounded as a router bit leaves them.
 */
annotation { "Feature Type Name" : "Robot lighten",
        "Feature Type Description" : "Lighten parts with pockets, leaving walls around their edges and holes, and ribs along a sketch." ~ CREDIT,
        "Manipulator Change Function" : "robotLightenManipulatorChange",
        "Editing Logic Function" : "robotLightenEditLogic",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotLighten = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Faces to lighten",
                    "Filter" : EntityType.FACE && GeometryType.PLANE && BodyType.SOLID && SketchObject.NO && ModifiableEntityOnly.YES }
        definition.faces is Query;

        annotation { "Name" : "Ribs to use", "Filter" : EntityType.EDGE && SketchObject.YES }
        definition.ribEdges is Query;

        annotation { "Name" : "Exclude construction lines", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.excludeConstruction is boolean;

        annotation { "Name" : "Wall thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.wallThickness, WALL_BOUNDS);

        annotation { "Name" : "Rib thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.ribThickness, RIB_BOUNDS);

        extrudePredicate(definition);

        annotation { "Name" : "Fillet corners", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.filletCorners is boolean;

        if (definition.filletCorners)
        {
            annotation { "Name" : "Fillet radius", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            isLength(definition.filletRadius, FILLET_RADIUS_BOUNDS);
        }

        annotation { "Group Name" : "Ignored faces", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Faces to ignore", "Filter" : EntityType.FACE && BodyType.SOLID && ModifiableEntityOnly.YES }
            definition.ignoredFaces is Query;
        }
    }
    {
        const faces = getFaces(context, definition);
        const ribEdges = getRibEdges(context, definition);
        const plane = ribPlane(context, ribEdges);
        verifyParallel(context, faces, plane);
        // To round the pockets' corners, they're cut with walls and ribs this much thicker on each side, then grown back
        // by it (see `roundPockets`)
        const radius = definition.filletCorners ? definition.filletRadius : 0 * meter;

        // The pockets: the extrude of the faces, as the end type says. Std's extrude, at the top level id, so its
        // manipulators are the feature's
        buildPockets(context, id, definition, faces);
        const extruded = qUnion(evaluateQuery(context, qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID)));

        // The walls, along the faces' edges (but those of the ignored faces), and the ribs, cut from the pockets
        const wallEdges = qSubtraction(qLoopEdges(faces), qLoopEdges(definition.ignoredFaces));
        const extent = bandExtent(context, plane, qUnion([extruded, wallEdges, ribEdges]));
        const walls = buildBands(context, id + "walls", extent, wallEdges, definition.wallThickness + radius, "walls", ["wallThickness"]);
        const ribs = buildBands(context, id + "ribs", extent, ribEdges, definition.ribThickness / 2 + radius, "ribs", ["ribEdges", "ribThickness"]);

        const bands = qUnion([walls, ribs]);
        try
        {
            opBoolean(context, id + "cutWallsAndRibs", {
                        "targets" : extruded,
                        "tools" : bands,
                        "operationType" : BooleanOperationType.SUBTRACTION
                    });
        }
        catch
        {
            // Each wall and rib, cut alone from a copy of the extrude
            const failing = failingBodies(context, id + "error", bands, function(errorId is Id, band is Query)
                {
                    opBoolean(context, errorId + "cut", {
                                "targets" : copyBodies(context, errorId + "copy", extruded),
                                "tools" : band,
                                "operationType" : BooleanOperationType.SUBTRACTION,
                                "keepTools" : true
                            });
                });
            throw regenError("Failed to cut walls and ribs.", ["wallThickness", "ribThickness"], failing);
        }
        // The pockets left between them, which the walls and ribs (used up) split into pieces
        const pockets = qUnion(evaluateQuery(context, qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID)));

        if (definition.filletCorners)
        {
            roundPockets(context, id, plane, pockets, radius);
        }

        if (isQueryEmpty(context, pockets))
        {
            reportFeatureWarning(context, id, "There's no room for pockets between the walls and ribs.",
                ["wallThickness", "ribThickness", "filletRadius"]);
        }
        else
        {
            const parts = qOwnerBody(faces);
            try
            {
                opBoolean(context, id + "cut", {
                            "targets" : parts,
                            "tools" : pockets,
                            "operationType" : BooleanOperationType.SUBTRACTION
                        });
            }
            catch
            {
                // Each pocket, cut alone from a copy of the parts
                const failing = failingBodies(context, id + "error", pockets, function(errorId is Id, pocket is Query)
                    {
                        opBoolean(context, errorId + "cut", {
                                    "targets" : copyBodies(context, errorId + "copy", parts),
                                    "tools" : pocket,
                                    "operationType" : BooleanOperationType.SUBTRACTION,
                                    "keepTools" : true
                                });
                    });
                throw regenError("Failed to cut pockets.", ["faces"], failing);
            }
            // Pieces of ribs which touch no wall or other rib are cut free, as parts of their own
            const loose = qCreatedBy(id + "cut", EntityType.BODY);
            if (!isQueryEmpty(context, loose))
            {
                reportFeatureWarning(context, id, "Some ribs touch no wall or other rib, so they're left as loose parts.", ["ribEdges"]);
                setErrorEntities(context, id, { "entities" : loose });
            }
        }

        opDeleteBodies(context, id + "cleanup", {
                    "entities" : qUnion([pockets, qCreatedBy(id + "walls", EntityType.BODY), qCreatedBy(id + "ribs", EntityType.BODY)])
                });
    });

/**
 * The faces to lighten.
 */
function getFaces(context is Context, definition is map) returns Query
{
    const faces = qEntityFilter(definition.faces, EntityType.FACE);
    if (isQueryEmpty(context, faces))
    {
        throw regenError("Select the faces to lighten.", ["faces"]);
    }
    return faces;
}

/**
 * Throws unless every face to lighten is parallel to the ribs' sketches, so the ribs go straight into it.
 */
function verifyParallel(context is Context, faces is Query, plane is Plane)
{
    const skewed = qSubtraction(faces, qParallelPlanes(faces, plane.normal, true));
    if (!isQueryEmpty(context, skewed))
    {
        throw regenError("The faces to lighten must be parallel to the ribs.", ["faces", "ribEdges"], skewed);
    }
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
 * Extrudes `faces` with std's extrude at `extrudeId`, as the definition's end type says.
 */
function buildPockets(context is Context, extrudeId is Id, definition is map, faces is Query)
{
    var extrudeDefinition = definition;
    extrudeDefinition.entities = faces;
    extrudeDefinition.operationType = NewBodyOperationType.NEW;
    extrude(context, extrudeId, extrudeDefinition);
}

/**
 * Where walls and ribs go, along `plane`'s normal: through `bounds` (the pockets and the edges along which they go),
 * and a little past it.
 *
 * @returns {{
 *      @field plane {Plane} : `plane`, moved halfway through them.
 *      @field depth {ValueWithUnits} : How far each band goes each way from its edge: through all of them, from any edge.
 *      @field halfDepth {ValueWithUnits} : How far a band goes each way from `plane`.
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
 * The bands along `edges` (walls or ribs, as `name` says), `halfWidth` to each side, through `extent` (see
 * `bandExtent`). Each is an edge's sheet, thickened; but an arc or circle hardly bigger than `halfWidth` can't be
 * thickened toward its center, so its band is a cylinder around its center, `halfWidth` bigger than it. That's its
 * band exactly for a circle (a hole's), and a little more (nearer its center than `halfWidth`) for an arc. Failures
 * highlight `faultyParameters`, and the edges which failed.
 */
function buildBands(context is Context, id is Id, extent is map, edges is Query, halfWidth is ValueWithUnits, name is string,
    faultyParameters is array) returns Query
{
    var swept = [];
    var arcs = [];
    var arcEdges = [];
    for (var edge in evaluateQuery(context, edges))
    {
        const curve = evCurveDefinition(context, { "edge" : edge });
        if (!(curve is Circle) || curve.radius > halfWidth * 1.05)
        {
            swept = append(swept, edge);
            continue;
        }
        arcEdges = append(arcEdges, edge);
        // A circle split into arcs needs just one cylinder
        if (!any(arcs, function(arc)
                {
                    return tolerantEquals(arc.coordSystem.origin, curve.coordSystem.origin) && tolerantEquals(arc.radius, curve.radius);
                }))
        {
            arcs = append(arcs, curve);
        }
    }

    if (swept != [])
    {
        thickenEdges(context, id + "thicken", extent, qUnion(swept), halfWidth, name, faultyParameters);
    }
    if (arcs != [])
    {
        const sketch = newSketchOnPlane(context, id + "cylinderSketch", { "sketchPlane" : extent.plane });
        for (var i, arc in arcs)
        {
            skCircle(sketch, "circle" ~ i, {
                        "center" : worldToPlane(extent.plane, arc.coordSystem.origin),
                        "radius" : arc.radius + halfWidth
                    });
        }
        skSolve(sketch);
        try
        {
            opExtrude(context, id + "cylinders", {
                        "entities" : qCreatedBy(id + "cylinderSketch", EntityType.FACE),
                        "direction" : extent.plane.normal,
                        "endBound" : BoundingType.BLIND,
                        "endDepth" : extent.halfDepth,
                        "startBound" : BoundingType.BLIND,
                        "startDepth" : extent.halfDepth
                    });
        }
        catch
        {
            throw regenError("Failed to extrude " ~ name ~ ".", faultyParameters, qUnion(arcEdges));
        }
        opDeleteBodies(context, id + "deleteCylinderSketch", { "entities" : qCreatedBy(id + "cylinderSketch", EntityType.BODY) });
    }
    return qUnion([qCreatedBy(id + "thicken" + "bands", EntityType.BODY), qCreatedBy(id + "cylinders", EntityType.BODY)]);
}

/**
 * Bands along `edges` (see `buildBands`): each extruded as a sheet, under `id + "sheets"`, and thickened to each side,
 * under `id + "bands"`.
 */
function thickenEdges(context is Context, id is Id, extent is map, edges is Query, halfWidth is ValueWithUnits, name is string,
    faultyParameters is array)
{
    try
    {
        extrudeEdges(context, id + "sheets", extent, edges);
    }
    catch
    {
        throw regenError("Failed to extrude " ~ name ~ ".", faultyParameters, edges);
    }
    try
    {
        opThicken(context, id + "bands", {
                    "entities" : qCreatedBy(id + "sheets", EntityType.BODY),
                    "thickness1" : halfWidth,
                    "thickness2" : halfWidth
                });
    }
    catch
    {
        // Each edge's band made alone, to show which fail
        var failing = [];
        for (var i, edge in evaluateQuery(context, edges))
        {
            const edgeId = id + "error" + unstableIdComponent(i);
            try silent
            {
                extrudeEdges(context, edgeId + "sheet", extent, edge);
                opThicken(context, edgeId + "band", {
                            "entities" : qCreatedBy(edgeId + "sheet", EntityType.BODY),
                            "thickness1" : halfWidth,
                            "thickness2" : halfWidth
                        });
            }
            catch
            {
                failing = append(failing, edge);
            }
        }
        throw regenError("Failed to thicken " ~ name ~ ".", faultyParameters, failing == [] ? edges : qUnion(failing));
    }
}

/**
 * Extrudes `edges` as sheets, each way from them through `extent` (see `bandExtent`).
 */
function extrudeEdges(context is Context, id is Id, extent is map, edges is Query)
{
    opExtrude(context, id, {
                "entities" : edges,
                "direction" : extent.plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : extent.depth,
                "startBound" : BoundingType.BLIND,
                "startDepth" : extent.depth
            });
}

/**
 * Rounds the pockets' corners, as a router bit of `radius` leaves them: they were cut with walls and ribs `radius`
 * thicker on each side, so a pocket narrower than the bit is gone, and the rest are grown back by `radius` and their
 * corners filleted. Every corner then has room for its fillet, and a pocket which narrows (between ribs meeting at a
 * sharp angle) ends in one round, rather than failing to fit a fillet into each side.
 */
function roundPockets(context is Context, id is Id, plane is Plane, pockets is Query, radius is ValueWithUnits)
{
    if (isQueryEmpty(context, pockets))
    {
        return;
    }
    try
    {
        opOffsetFace(context, id + "grow", {
                    "moveFaces" : pocketSides(pockets, plane),
                    "offsetDistance" : radius
                });
    }
    catch
    {
        // Each pocket, grown alone (they're separate bodies, so one's try doesn't change another's)
        const failing = failingBodies(context, id + "error", pockets, function(errorId is Id, pocket is Query)
            {
                opOffsetFace(context, errorId, {
                            "moveFaces" : pocketSides(pocket, plane),
                            "offsetDistance" : radius
                        });
            });
        throw regenError("Failed to grow pockets back to round their corners.", ["filletRadius"], failing);
    }
    const corners = pocketCorners(context, pockets, plane);
    if (isQueryEmpty(context, corners))
    {
        return;
    }
    try
    {
        opFillet(context, id + "fillet", {
                    "entities" : corners,
                    "radius" : radius
                });
    }
    catch
    {
        // Each pocket's corners, filleted alone
        const failing = failingBodies(context, id + "error", pockets, function(errorId is Id, pocket is Query)
            {
                opFillet(context, errorId, {
                            "entities" : pocketCorners(context, pocket, plane),
                            "radius" : radius
                        });
            });
        throw regenError("Failed to fillet pocket corners.", ["filletRadius"], failing);
    }
}

/**
 * The pockets' sides (not their ends, in the sketch's plane and parallel to it).
 */
function pocketSides(pockets is Query, plane is Plane) returns Query
{
    const faces = qOwnedByBody(pockets, EntityType.FACE);
    return qSubtraction(faces, qParallelPlanes(faces, plane.normal, true));
}

/**
 * The pockets' corners: their convex edges along the sketch's normal.
 */
function pocketCorners(context is Context, pockets is Query, plane is Plane) returns Query
{
    return qUnion(filter(evaluateQuery(context, qParallelEdges(qOwnedByBody(pockets, EntityType.EDGE), plane.normal)), function(edge)
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

export function robotLightenManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return extrudeManipulatorChange(context, definition, newManipulators);
}

/**
 * Fills in the faces to lighten, unless they've been set: the faces in the first rib's sketch plane which the ribs are
 * over. Points
 * the pockets into the faces' parts (against their normals), unless Opposite direction has been set.
 */
export function robotLightenEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    if (!(specifiedParameters.oppositeDirection ?? false))
    {
        // An extrude of a part's face goes out of it, along its normal
        definition.oppositeDirection = true;
    }
    if (specifiedParameters.faces ?? false)
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
        definition.faces = facesUnder(context, plane, edges, hiddenBodies);
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
