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
            annotation { "Name" : "Faces to ignore",
                        "Filter" : EntityType.FACE && ((BodyType.SOLID && ModifiableEntityOnly.YES) || (SketchObject.YES && ConstructionObject.NO)) }
            definition.ignoredFaces is Query;
        }
    }
    {
        const faces = getFaces(context, definition);
        const ribEdges = getRibEdges(context, definition);
        const plane = ribPlane(context, ribEdges);
        verifyParallel(context, faces, plane);
        // To round the pockets' corners, they're made with walls and ribs this much thicker, then grown back by it (see
        // `roundPockets`)
        const radius = definition.filletCorners ? definition.filletRadius : 0 * meter;

        // The edges the faces to lighten share with the parts' faces to ignore, which the extrude sweeps into its sides
        // along them
        const ignoredEdges = qIntersection([qLoopEdges(faces), qLoopEdges(qSketchFilter(definition.ignoredFaces, SketchObject.NO))]);
        const ignoredSides = startTracking(context, ignoredEdges);
        // The pockets: the extrude of the faces, as the end type says. Std's extrude, at the top level id, so its
        // manipulators are the feature's
        buildPockets(context, id, definition, faces);
        // Sketch regions to ignore are left solid: cut from the pockets, so walls go around them too
        excludeRegions(context, id + "excludeRegions", plane, qUnion(evaluateQuery(context, qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID))),
            qSketchFilter(definition.ignoredFaces, SketchObject.YES));
        const extruded = qUnion(evaluateQuery(context, qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID)));
        const ends = qUnion(evaluateQuery(context, qCapEntity(id, CapType.EITHER, EntityType.FACE)));
        // The pockets' ends, through the walls, ribs, and rounding (see `pocketEnds`)
        const trackedEnds = startTracking(context, ends);

        // The walls: the pockets, inset by them, but along the parts' faces to ignore
        insetPockets(context, id + "walls", extruded, ends, qIntersection([qOwnedByBody(extruded, EntityType.FACE), ignoredSides]),
            definition.wallThickness + radius);

        // The ribs, cut from what's left
        const inset = qUnion(evaluateQuery(context, qCreatedBy(id + "walls", EntityType.BODY)->qBodyType(BodyType.SOLID)));
        const ribs = buildRibs(context, id + "ribs", bandExtent(context, plane, qUnion([inset, ribEdges])), ribEdges,
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
        // The pockets left between them, which the ribs (used up) split into pieces
        const pockets = qUnion(evaluateQuery(context, qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID)));

        if (definition.filletCorners)
        {
            roundPockets(context, id, pocketEnds(pockets, plane, trackedEnds), pockets, radius);
        }

        if (isQueryEmpty(context, pockets))
        {
            reportFeatureWarning(context, id, "There's no room for pockets between the walls and ribs.",
                ["wallThickness", "ribThickness", "filletRadius"]);
        }
        else
        {
            // Evaluated, to measure the same parts after the cut
            const parts = qUnion(evaluateQuery(context, qOwnerBody(faces)));
            const volume = evVolume(context, { "entities" : parts });
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
            else
            {
                // Only without the warning, which this would replace
                const lightened = 1 - evVolume(context, { "entities" : parts }) / volume;
                reportFeatureInfo(context, id, "Lightened the parts by " ~ roundToPrecision(lightened * 100, 1) ~ "%.");
            }
        }

        opDeleteBodies(context, id + "cleanup", {
                    "entities" : qUnion([pockets, qCreatedBy(id + "ribs", EntityType.BODY)])
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
 * Insets `pockets` by `distance` (the walls), under `id`, but at `ends` (their caps) and `ignoredSides`, which stay
 * where they are. As Ilya Baran and Morgan Bartlett's Lighten does: those are moved out by `distance`, and the pockets
 * hollowed by it, which moves every face in by it at once, holes of any size and their corners too, so it doesn't
 * fail where a wall along one edge would. What's inside (enclosed) is kept, and the rest deleted. Their concave edges
 * are rounded a hair (std's boolean tolerance, 0.01 mm) first, so they're rounded to `distance` (and the hair) as
 * they're moved, as the walls' inside corners should be. (Lighten then sets those to `distance` exactly, which isn't
 * worth an operation which can fail.)
 */
function insetPockets(context is Context, id is Id, pockets is Query, ends is Query, ignoredSides is Query, distance is ValueWithUnits)
{
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
    }
    catch
    {
        // Each pocket, hollowed alone (they're separate bodies, so one's try doesn't change another's)
        const failing = failingBodies(context, id + "error", pockets, function(errorId is Id, pocket is Query)
            {
                opShell(context, errorId, {
                            "entities" : pocket,
                            "thickness" : -distance
                        });
            });
        throw regenError("Failed to make walls.", ["wallThickness"], failing);
    }
    try
    {
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
