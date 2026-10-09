FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "21762d39019c8b2289e2fbb8", version : "80768fbb394ad68f2a15753b");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");
import(path : "a4248fe48b63da8d1971e19a", version : "84a8da5dce4e619110893727");

const WALL_BOUNDS = { (meter) : [1e-5, 0.003175, 500], (inch) : 0.125, (millimeter) : 3 } as LengthBoundSpec;
const RIB_BOUNDS = { (meter) : [1e-5, 0.003175, 500], (inch) : 0.125, (millimeter) : 3 } as LengthBoundSpec;
// A 1/8 in. router bit's
const CORNER_RADIUS_BOUNDS = { (meter) : [1e-5, 0.0015875, 500], (inch) : 0.0625, (millimeter) : 1.5 } as LengthBoundSpec;

/**
 * Lightens parts with pockets: everything within the extrude of the faces to lighten (into their parts, as the end type
 * says) is cut away, but for walls along the faces' edges (their parts' sides and holes) and ribs along the rib
 * sketch's edges, with the pockets' corners rounded as a router bit leaves them.
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
                    "Filter" : EntityType.FACE && GeometryType.PLANE && BodyType.SOLID && SketchObject.NO && ModifiableEntityOnly.YES,
                    "Description" : "The flat faces pockets are cut into, parallel to the rib sketch." }
        definition.faces is Query;

        annotation { "Name" : "Rib sketch", "Filter" : EntityType.EDGE && SketchObject.YES,
                    "Description" : "The sketch whose edges (lines, arcs, circles, or splines) ribs are left along." }
        definition.ribEdges is Query;

        annotation { "Name" : "Exclude construction lines", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.excludeConstruction is boolean;

        annotation { "Name" : "Wall thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                    "Description" : "How thick the walls left along the parts' sides and around their holes are." }
        isLength(definition.wallThickness, WALL_BOUNDS);

        annotation { "Name" : "Rib thickness", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        isLength(definition.ribThickness, RIB_BOUNDS);

        extrudePredicate(definition);

        annotation { "Name" : "Fillet corners", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"],
                    "Description" : "Round the pockets' corners, as a router bit leaves them." }
        definition.filletCorners is boolean;

        if (definition.filletCorners)
        {
            annotation { "Group Name" : "Fillet corners", "Collapsed By Default" : false, "Driving Parameter" : "filletCorners" }
            {
                annotation { "Name" : "Radius", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                isLength(definition.cornerRadius, CORNER_RADIUS_BOUNDS);
            }
        }

        annotation { "Group Name" : "Ignored faces", "Collapsed By Default" : true }
        {
            annotation { "Name" : "Faces to ignore", "Filter" : EntityType.FACE && BodyType.SOLID && ModifiableEntityOnly.YES,
                        "Description" : "Faces no wall is left along, so pockets run out through them." }
            definition.ignoredFaces is Query;
        }
    }
    {
        const faces = getFaces(context, definition);
        const ribEdges = getRibEdges(context, definition);
        const plane = ribPlane(context, ribEdges);
        verifyParallel(context, faces, ribEdges, plane);
        // To round the pockets' corners, they're cut with walls and ribs this much thicker on each side, then grown back
        // by it (see `roundPockets`)
        const radius = definition.filletCorners ? definition.cornerRadius : 0 * meter;

        // The pockets: the extrude of the faces, as the end type says. Std's extrude, at the top level id, so its
        // manipulators are the feature's
        runStep(context, id, id, function(context is Context, extrudeId is Id, extrudeDefinition is map)
            {
                buildPockets(context, extrudeId, definition, faces);
            }, {}, {
                    "message" : "Couldn't extrude the faces to lighten.",
                    "featureParameterMappingFunction" : function(parameter) { return parameter; },
                    "entities" : faces
                });
        const extruded = qUnion(evaluateQuery(context, qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID)));

        // The walls, along the faces' edges (but those of the ignored faces), and the ribs, cut from the pockets
        const wallEdges = qSubtraction(qLoopEdges(faces), qLoopEdges(definition.ignoredFaces));
        const walls = buildBands(context, id, id + "walls", plane, wallEdges, definition.wallThickness + radius, {
                    "message" : "Couldn't make the walls along the faces' edges.",
                    "faultyParameters" : ["faces", "wallThickness", "ignoredFaces"]
                });
        const ribs = buildBands(context, id, id + "ribs", plane, ribEdges, definition.ribThickness / 2 + radius, {
                    "message" : "Couldn't make the ribs along the rib sketch's edges.",
                    "faultyParameters" : ["ribEdges", "ribThickness"]
                });
        runStep(context, id, id + "cutWallsAndRibs", opBoolean, {
                    "targets" : extruded,
                    "tools" : qUnion([walls, ribs]),
                    "operationType" : BooleanOperationType.SUBTRACTION
                }, {
                    "message" : "Couldn't cut the walls and ribs from the pockets.",
                    "faultyParameters" : ["wallThickness", "ribThickness"],
                    // A failed operation changes nothing, so they're still there to show
                    "entities" : qUnion([extruded, walls, ribs]),
                    "diagnose" : function(diagnosisId)
                        {
                            // Each wall and rib, cut alone from a copy of the extrude
                            return showFailing(failingItems(context, diagnosisId, evaluateQuery(context, qUnion([walls, ribs])), function(context is Context, trialId is Id, band is Query)
                                    {
                                        opBoolean(context, trialId + "cut", {
                                                    "targets" : copyBodies(context, trialId + "copy", extruded),
                                                    "tools" : band,
                                                    "operationType" : BooleanOperationType.SUBTRACTION,
                                                    "keepTools" : true
                                                });
                                    }), "The walls or ribs shown can't be cut on their own: look for one which nearly lines up with a part's side or another rib, or meets one at a tangent.");
                        }
                });
        // The pockets left between them, which the walls and ribs (used up) split into pieces
        const pockets = qUnion(evaluateQuery(context, qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID)));

        if (definition.filletCorners)
        {
            roundPockets(context, id, plane, pockets, radius);
        }

        if (isQueryEmpty(context, pockets))
        {
            reportFeatureWarning(context, id, "There's no room for pockets between the walls and ribs.",
                ["wallThickness", "ribThickness", "cornerRadius"]);
        }
        else
        {
            const parts = qOwnerBody(faces);
            runStep(context, id, id + "cut", opBoolean, {
                        "targets" : parts,
                        "tools" : pockets,
                        "operationType" : BooleanOperationType.SUBTRACTION
                    }, {
                        "message" : "Couldn't cut the pockets from the parts.",
                        "faultyParameters" : ["faces"],
                        "entities" : qUnion([pockets, faces]),
                        "diagnose" : function(diagnosisId)
                            {
                                // Each pocket, cut alone from a copy of the parts
                                return showFailing(failingItems(context, diagnosisId, evaluateQuery(context, pockets), function(context is Context, trialId is Id, pocket is Query)
                                        {
                                            opBoolean(context, trialId + "cut", {
                                                        "targets" : copyBodies(context, trialId + "copy", parts),
                                                        "tools" : pocket,
                                                        "operationType" : BooleanOperationType.SUBTRACTION,
                                                        "keepTools" : true
                                                    });
                                        }), "The pockets shown can't be cut on their own: look for one which nearly lines up with a part's face or edge.");
                            }
                    });
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
 * Throws unless every face to lighten is parallel to the rib sketch, so its ribs go straight into it.
 */
function verifyParallel(context is Context, faces is Query, ribEdges is Query, plane is Plane)
{
    const skewed = qSubtraction(faces, qParallelPlanes(faces, plane.normal, true));
    if (!isQueryEmpty(context, skewed))
    {
        throw regenError("The faces to lighten must be parallel to the rib sketch.", ["faces", "ribEdges"], skewed);
    }
}

/**
 * The rib sketch's edges (its construction edges too, unless they're excluded).
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
        throw regenError("Select the edges of a sketch to leave ribs along.", ["ribEdges"]);
    }
    return edges;
}

/**
 * The plane of the rib sketch, which every one of its edges must be in.
 */
function ribPlane(context is Context, edges is Query) returns Plane
{
    const evaluated = evaluateQuery(context, edges);
    const plane = evOwnerSketchPlane(context, { "entity" : evaluated[0] });
    for (var edge in evaluated)
    {
        if (!coplanarPlanes(evOwnerSketchPlane(context, { "entity" : edge }), plane))
        {
            throw regenError("The rib sketch's edges must all be in one plane.", ["ribEdges"], edges);
        }
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
 * Bands along `edges` (under `bandsId`), `halfWidth` to each side: their extrudes along the sketch's normal through
 * everything, as sheets (a circle's is a tube), thickened. Returns them. `id` is the feature's; `failure` says what
 * failed if they can't be made (see `runStep`).
 */
function buildBands(context is Context, id is Id, bandsId is Id, plane is Plane, edges is Query, halfWidth is ValueWithUnits,
    failure is map) returns Query
{
    failure.entities = edges;
    runStep(context, id, bandsId + "sheets", opExtrude, {
                "entities" : edges,
                "direction" : plane.normal,
                "startBound" : BoundingType.THROUGH_ALL,
                "endBound" : BoundingType.THROUGH_ALL
            }, failure);
    const sheets = qCreatedBy(bandsId + "sheets", EntityType.BODY);
    failure.diagnose = function(diagnosisId)
        {
            // Each sheet (along an edge, through everything), thickened alone
            return showFailing(failingItems(context, diagnosisId, evaluateQuery(context, sheets), function(context is Context, trialId is Id, sheet is Query)
                    {
                        opThicken(context, trialId + "thicken", {
                                    "entities" : sheet,
                                    "thickness1" : halfWidth,
                                    "thickness2" : halfWidth
                                });
                    }), "The ones along the sheets shown can't be made on their own.");
        };
    runStep(context, id, bandsId + "thicken", opThicken, {
                "entities" : sheets,
                "thickness1" : halfWidth,
                "thickness2" : halfWidth
            }, failure);
    return qCreatedBy(bandsId + "thicken", EntityType.BODY);
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
    runStep(context, id, id + "grow", opOffsetFace, {
                "moveFaces" : pocketSides(pockets, plane),
                "offsetDistance" : radius
            }, {
                "message" : "Couldn't grow the pockets back to round their corners.",
                "faultyParameters" : ["cornerRadius"],
                "entities" : pockets,
                "diagnose" : function(diagnosisId)
                    {
                        // Each pocket, grown alone (a copy of it)
                        return showFailing(failingItems(context, diagnosisId, evaluateQuery(context, pockets), function(context is Context, trialId is Id, pocket is Query)
                                {
                                    opOffsetFace(context, trialId + "grow", {
                                                "moveFaces" : pocketSides(copyBodies(context, trialId + "copy", pocket), plane),
                                                "offsetDistance" : radius
                                            });
                                }), "The pockets shown can't be grown back on their own.");
                    }
            });
    const corners = pocketCorners(context, pockets, plane);
    if (isQueryEmpty(context, corners))
    {
        return;
    }
    runStep(context, id, id + "fillet", opFillet, {
                "entities" : corners,
                "radius" : radius
            }, {
                "message" : "Couldn't fillet the pockets' corners.",
                "faultyParameters" : ["cornerRadius"],
                "entities" : corners,
                "diagnose" : function(diagnosisId)
                    {
                        // Each pocket's corners, filleted alone (on a copy of it)
                        return showFailing(failingItems(context, diagnosisId, evaluateQuery(context, pockets), function(context is Context, trialId is Id, pocket is Query)
                                {
                                    opFillet(context, trialId + "fillet", {
                                                "entities" : pocketCorners(context, copyBodies(context, trialId + "copy", pocket), plane),
                                                "radius" : radius
                                            });
                                }), "The corners of the pockets shown can't be filleted on their own.");
                    }
            });
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
 * A diagnosis (see `runStep`'s `failure.diagnose`) showing `failing`, with `message`, or `undefined` if nothing failed
 * on its own.
 */
function showFailing(failing is array, message is string)
{
    if (failing == [])
    {
        return undefined;
    }
    return { "entities" : qUnion(failing), "message" : message };
}

export function robotLightenManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return extrudeManipulatorChange(context, definition, newManipulators);
}

/**
 * Fills in the faces to lighten, unless they've been set: the faces in the rib sketch's plane which it's over. Points
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
    // A guard: editing logic mustn't throw while the dialog's being filled in, so without a rib sketch yet, it's left
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
 * The faces in `plane` (of parts which aren't hidden) which overlap `footprint` (the rib sketch's edges), seen along
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
