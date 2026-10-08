FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "core/stdExtrude.fs", version : "");
import(path : "core/robotFeature.fs", version : "");
import(path : "core/steps.fs", version : "");

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
                    "entities" : qUnion([extruded, walls, ribs])
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
            runStep(context, id, id + "cut", opBoolean, {
                        "targets" : qOwnerBody(faces),
                        "tools" : pockets,
                        "operationType" : BooleanOperationType.SUBTRACTION
                    }, {
                        "message" : "Couldn't cut the pockets from the parts.",
                        "faultyParameters" : ["faces"],
                        "entities" : qUnion([pockets, faces])
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
    runStep(context, id, bandsId + "thicken", opThicken, {
                "entities" : qCreatedBy(bandsId + "sheets", EntityType.BODY),
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
    const faces = qOwnedByBody(pockets, EntityType.FACE);
    runStep(context, id, id + "grow", opOffsetFace, {
                // Their sides, not their ends
                "moveFaces" : qSubtraction(faces, qParallelPlanes(faces, plane.normal, true)),
                "offsetDistance" : radius
            }, {
                "message" : "Couldn't grow the pockets back to round their corners.",
                "faultyParameters" : ["cornerRadius"],
                "entities" : pockets
            });
    const corners = filter(evaluateQuery(context, qParallelEdges(qOwnedByBody(pockets, EntityType.EDGE), plane.normal)), function(edge)
        {
            return evEdgeConvexity(context, { "edge" : edge }) == EdgeConvexityType.CONVEX;
        });
    if (corners == [])
    {
        return;
    }
    runStep(context, id, id + "fillet", opFillet, {
                "entities" : qUnion(corners),
                "radius" : radius
            }, {
                "message" : "Couldn't fillet the pockets' corners.",
                "faultyParameters" : ["cornerRadius"],
                "entities" : qUnion(corners)
            });
}

export function robotLightenManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return extrudeManipulatorChange(context, definition, newManipulators);
}

/**
 * Fills in the faces to lighten, unless they've been set: the faces in the rib sketch's plane of the parts it's over or
 * under. Points the pockets into the faces' parts (against their normals), unless Opposite direction has been set.
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
        definition.faces = facesUnder(context, id + "heuristics", plane, edges, hiddenBodies);
    }
    return definition;
}

/**
 * The faces in `plane` of the parts `footprint` (the rib sketch's edges) is over or under, through everything along
 * the plane's normal. Works it out in a feature it aborts (under `id`), as std's boolean heuristics do, so nothing's left
 * in the Part Studio.
 */
function facesUnder(context is Context, id is Id, plane is Plane, footprint is Query, hiddenBodies is Query) returns Query
{
    const candidates = qSubtraction(qAllModifiableSolidBodiesNoMesh(), hiddenBodies);
    var parts = [];
    startFeature(context, id);
    // A guard: if the footprint can't be extruded, no parts are found
    try silent
    {
        const bounds = evBox3d(context, { "topology" : footprint, "cSys" : coordSystem(plane), "tight" : false });
        // A little bigger, so a sketch of one line still has an area
        const margin = 1 * millimeter;
        const sketch = newSketchOnPlane(context, id + "footprint", { "sketchPlane" : plane });
        skRectangle(sketch, "rectangle", {
                    "firstCorner" : vector(bounds.minCorner[0] - margin, bounds.minCorner[1] - margin),
                    "secondCorner" : vector(bounds.maxCorner[0] + margin, bounds.maxCorner[1] + margin)
                });
        skSolve(sketch);
        opExtrude(context, id + "extrude", {
                    "entities" : qCreatedBy(id + "footprint", EntityType.FACE),
                    "direction" : plane.normal,
                    "startBound" : BoundingType.THROUGH_ALL,
                    "endBound" : BoundingType.THROUGH_ALL
                });
        for (var clash in evCollision(context, { "tools" : qCreatedBy(id + "extrude", EntityType.BODY), "targets" : candidates }))
        {
            parts = append(parts, clash.targetBody);
        }
    }
    abortFeature(context, id);
    return qCoincidesWithPlane(qOwnedByBody(qUnion(parts), EntityType.FACE), plane);
}
