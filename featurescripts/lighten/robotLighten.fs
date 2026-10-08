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
 * Lightens parts with pockets: everything within the extrude of the rib sketch's plane (as its end type says) is cut
 * away, but for walls along the parts' sides and around their holes (a shell of them), and ribs along the sketch's
 * edges, with the pockets' corners filleted.
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
        annotation { "Name" : "Rib sketch", "Filter" : EntityType.EDGE && SketchObject.YES,
                    "Description" : "The sketch whose edges ribs are left along. Its plane is where the pockets start." }
        definition.ribEdges is Query;

        annotation { "Name" : "Exclude construction", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
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

        annotation { "Name" : "Merge scope", "Filter" : EntityType.BODY && BodyType.SOLID && ModifiableEntityOnly.YES,
                    "Description" : "The parts to lighten." }
        definition.booleanScope is Query;
    }
    {
        const ribEdges = getRibEdges(context, definition);
        const plane = ribPlane(context, ribEdges);
        const parts = evaluateQuery(context, qEntityFilter(definition.booleanScope, EntityType.BODY)->qBodyType(BodyType.SOLID));
        if (parts == [])
        {
            throw regenError("Select the parts to lighten.", ["booleanScope"]);
        }
        const partsQuery = qUnion(parts);

        // The pockets: the extrude of a face covering the parts, as the end type says. Std's extrude, at the top level
        // id, so its manipulators are the feature's
        runStep(context, id, id, function(context is Context, extrudeId is Id, extrudeDefinition is map)
            {
                buildPockets(context, id + "region", extrudeId, definition, plane, partsQuery);
            }, {}, {
                    "message" : "Couldn't extrude the pockets.",
                    "featureParameterMappingFunction" : function(parameter) { return parameter; },
                    "entities" : partsQuery
                });
        const pockets = qUnion(evaluateQuery(context, qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID)));

        // Ribs, cut from the pockets so they're left
        const ribs = buildRibs(context, id, id + "ribs", definition, plane, ribEdges);
        runStep(context, id, id + "cutRibs", opBoolean, {
                    "targets" : pockets,
                    "tools" : ribs,
                    "operationType" : BooleanOperationType.SUBTRACTION
                }, {
                    "message" : "Couldn't cut the ribs from the pockets.",
                    "faultyParameters" : ["ribEdges", "ribThickness"],
                    "reconstruct" : function(errorId is Id)
                        {
                            buildPockets(context, errorId + "region", errorId + "pockets", definition, plane, partsQuery);
                            buildRibs(context, id, errorId + "ribs", definition, plane, ribEdges);
                        }
                });

        for (var i, part in parts)
        {
            const partId = id + unstableIdComponent(i);
            setExternalDisambiguation(context, partId, part);
            lightenPart(context, id, partId, definition, plane, part, pockets);
        }

        opDeleteBodies(context, id + "cleanup", {
                    "entities" : qUnion([pockets, qCreatedBy(id + "region", EntityType.BODY), qCreatedBy(id + "ribs", EntityType.BODY)])
                });
    });

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
 * Extrudes a face on `plane` covering `parts` (sketched under `sketchId`), with std's extrude at `extrudeId`, as the
 * definition's end type says.
 */
function buildPockets(context is Context, sketchId is Id, extrudeId is Id, definition is map, plane is Plane, parts is Query)
{
    var extrudeDefinition = definition;
    extrudeDefinition.entities = sketchCovering(context, sketchId, plane, parts);
    extrudeDefinition.operationType = NewBodyOperationType.NEW;
    extrude(context, extrudeId, extrudeDefinition);
}

/**
 * A rectangle on `plane` covering `bodies` (as they'd be projected onto it), with room to spare, so an extrude of it
 * reaches past their sides. Returns its face.
 */
function sketchCovering(context is Context, id is Id, plane is Plane, bodies is Query) returns Query
{
    const bounds = evBox3d(context, { "topology" : bodies, "cSys" : coordSystem(plane), "tight" : false });
    const margin = norm(bounds.maxCorner - bounds.minCorner) * 0.1;
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    skRectangle(sketch, "rectangle", {
                "firstCorner" : vector(bounds.minCorner[0] - margin, bounds.minCorner[1] - margin),
                "secondCorner" : vector(bounds.maxCorner[0] + margin, bounds.maxCorner[1] + margin)
            });
    skSolve(sketch);
    return qCreatedBy(id, EntityType.FACE);
}

/**
 * Ribs along `ribEdges` (under `ribsId`): their extrudes along the sketch's normal through everything, as sheets,
 * thickened to the rib thickness, half on each side. Returns them. `id` is the feature's.
 */
function buildRibs(context is Context, id is Id, ribsId is Id, definition is map, plane is Plane, ribEdges is Query) returns Query
{
    runStep(context, id, ribsId + "sheets", opExtrude, {
                "entities" : ribEdges,
                "direction" : plane.normal,
                "startBound" : BoundingType.THROUGH_ALL,
                "endBound" : BoundingType.THROUGH_ALL
            }, {
                "message" : "Couldn't extrude the rib sketch's edges.",
                "faultyParameters" : ["ribEdges"],
                "entities" : ribEdges
            });
    runStep(context, id, ribsId + "thicken", opThicken, {
                "entities" : qCreatedBy(ribsId + "sheets", EntityType.BODY),
                "thickness1" : definition.ribThickness / 2,
                "thickness2" : definition.ribThickness / 2
            }, {
                "message" : "Couldn't thicken the ribs.",
                "faultyParameters" : ["ribEdges", "ribThickness"],
                "entities" : ribEdges
            });
    return qCreatedBy(ribsId + "thicken", EntityType.BODY);
}

/**
 * Lightens `part` (with steps under `partId`): a copy of it has the pockets (less the ribs) cut from it, leaving what's
 * outside them and the ribs; the part is shelled, removing its faces parallel to the sketch (and the ignored ones), to
 * leave walls; the two are joined, and the pockets' corners are filleted.
 */
function lightenPart(context is Context, id is Id, partId is Id, definition is map, plane is Plane, part is Query, pockets is Query)
{
    const faces = qOwnedByBody(part, EntityType.FACE);
    const openFaces = qUnion([qParallelPlanes(faces, plane.normal, true), qIntersection([definition.ignoredFaces, faces])]);
    if (isQueryEmpty(context, qParallelPlanes(faces, plane.normal, true)))
    {
        throw regenError("A part has no faces parallel to the rib sketch, which pockets are cut into.", ["booleanScope"], part);
    }

    // What's left of a copy of the part outside the pockets, and its ribs
    runStep(context, id, partId + "copy", opPattern, {
                "entities" : part,
                "transforms" : [identityTransform()],
                "instanceNames" : ["copy"]
            }, { "message" : "Couldn't copy a part to lighten.", "entities" : part });
    const kept = qCreatedBy(partId + "copy", EntityType.BODY);
    runStep(context, id, partId + "cutPockets", opBoolean, {
                "targets" : kept,
                "tools" : pockets,
                "operationType" : BooleanOperationType.SUBTRACTION,
                "keepTools" : true
            }, {
                "message" : "Couldn't cut the pockets from a part.",
                "faultyParameters" : ["booleanScope"],
                "entities" : part,
                "reconstruct" : function(errorId is Id)
                    {
                        reconstructPockets(context, id, errorId, definition, plane, part);
                    }
            });

    // The walls
    runStep(context, id, partId + "shell", opShell, {
                "entities" : openFaces,
                // Inward
                "thickness" : -definition.wallThickness
            }, {
                "message" : "Couldn't shell a part to leave its walls. Are they too thick?",
                "faultyParameters" : ["wallThickness", "ignoredFaces"],
                "entities" : qUnion([part, openFaces])
            });

    // Pockets through all with no ribs across the part leave nothing of the copy
    if (!isQueryEmpty(context, kept))
    {
        runStep(context, id, partId + "join", opBoolean, {
                    "targets" : part,
                    "tools" : kept,
                    "operationType" : BooleanOperationType.UNION,
                    "targetsAndToolsNeedGrouping" : true
                }, {
                    "message" : "Couldn't join a part's walls to its ribs and what's outside its pockets.",
                    "faultyParameters" : ["booleanScope"],
                    "entities" : part,
                    "reconstruct" : function(errorId is Id)
                        {
                            reconstructPockets(context, id, errorId, definition, plane, part);
                        }
                });
    }

    if (definition.filletCorners)
    {
        filletCorners(context, id, partId + "fillet", definition, plane, part);
    }
}

/**
 * Rebuilds the pockets (less the ribs) under `errorId`, to show where they'd cut `part` when cutting or joining them
 * fails (what the feature built is rolled back with it).
 */
function reconstructPockets(context is Context, id is Id, errorId is Id, definition is map, plane is Plane, part is Query)
{
    buildPockets(context, errorId + "region", errorId + "pockets", definition, plane, part);
    const ribs = buildRibs(context, id, errorId + "ribs", definition, plane, getRibEdges(context, definition));
    opBoolean(context, errorId + "cutRibs", {
                "targets" : qCreatedBy(errorId + "pockets", EntityType.BODY)->qBodyType(BodyType.SOLID),
                "tools" : ribs,
                "operationType" : BooleanOperationType.SUBTRACTION
            });
    opDeleteBodies(context, errorId + "deleteRegion", { "entities" : qCreatedBy(errorId + "region", EntityType.BODY) });
}

/**
 * Fillets the pockets' corners in `part`: the concave edges along the sketch's normal which the feature made.
 */
function filletCorners(context is Context, id is Id, filletId is Id, definition is map, plane is Plane, part is Query)
{
    const candidates = qParallelEdges(qIntersection([qOwnedByBody(part, EntityType.EDGE), qCreatedBy(id, EntityType.EDGE)]), plane.normal);
    const corners = filter(evaluateQuery(context, candidates), function(edge)
        {
            return evEdgeConvexity(context, { "edge" : edge }) == EdgeConvexityType.CONCAVE;
        });
    if (corners == [])
    {
        return;
    }
    runStep(context, id, filletId, opFillet, {
                "entities" : qUnion(corners),
                "radius" : definition.cornerRadius
            }, {
                "message" : "Couldn't fillet the pockets' corners. Is the radius too large?",
                "faultyParameters" : ["cornerRadius"],
                "entities" : qUnion(corners)
            });
}

export function robotLightenManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return extrudeManipulatorChange(context, definition, newManipulators);
}

/**
 * Fills in the merge scope, unless it's been set: the parts the rib sketch is on (with a face in its plane) among
 * those it's over or under, or else all of those. Points the pockets into them, unless Opposite direction has been set.
 */
export function robotLightenEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean,
    specifiedParameters is map, hiddenBodies is Query) returns map
{
    // A guard: editing logic mustn't throw while the dialog's being filled in, so without a rib sketch yet, it's left
    var plane;
    var footprint;
    try silent
    {
        const edges = getRibEdges(context, definition);
        plane = ribPlane(context, edges);
        footprint = edges;
    }
    if (plane == undefined)
    {
        return definition;
    }
    if (!(specifiedParameters.booleanScope ?? false))
    {
        definition.booleanScope = partsUnder(context, id + "heuristics", plane, footprint, hiddenBodies);
    }
    if (!(specifiedParameters.oppositeDirection ?? false) && !isQueryEmpty(context, definition.booleanScope))
    {
        // Into the parts: the extrude goes along the sketch's normal unless it's flipped
        const center = box3dCenter(evBox3d(context, { "topology" : definition.booleanScope, "tight" : false }));
        definition.oppositeDirection = dot(center - plane.origin, plane.normal) < 0;
    }
    return definition;
}

/**
 * The parts `footprint` (the rib sketch's edges) is over or under, through everything along `plane`'s normal: those
 * with a face in `plane`, if any, or else all of them. Works it out in a feature it aborts (under `id`), as std's
 * boolean heuristics do, so nothing's left in the Part Studio.
 */
function partsUnder(context is Context, id is Id, plane is Plane, footprint is Query, hiddenBodies is Query) returns Query
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
    const hit = qUnion(parts);
    const onPlane = qOwnerBody(qCoincidesWithPlane(qOwnedByBody(hit, EntityType.FACE), plane));
    return isQueryEmpty(context, onPlane) ? hit : onPlane;
}
