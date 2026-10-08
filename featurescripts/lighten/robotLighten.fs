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
 * says) is cut away, but for walls along the parts' sides and around their holes (a shell of them), and ribs along the
 * rib sketch's edges, with the pockets' corners filleted.
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
                    "Filter" : (EntityType.FACE && GeometryType.PLANE && BodyType.SOLID && ModifiableEntityOnly.YES) || (EntityType.FACE && SketchObject.YES),
                    "Description" : "The flat faces pockets are cut into, parallel to the rib sketch. A sketch region on a face selects the face." }
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
                    // A failed operation changes nothing, so they're still there to show
                    "entities" : qUnion([pockets, ribs])
                });

        var loose = [];
        for (var i, part in evaluateQuery(context, qOwnerBody(faces)))
        {
            const partId = id + unstableIdComponent(i);
            setExternalDisambiguation(context, partId, part);
            loose = concatenateArrays([loose, lightenPart(context, id, partId, definition, plane, part, faces, pockets)]);
        }

        opDeleteBodies(context, id + "cleanup", { "entities" : qUnion([pockets, qCreatedBy(id + "ribs", EntityType.BODY)]) });

        // Last, so no step's status hides it
        if (loose != [])
        {
            reportFeatureWarning(context, id, "Some ribs touch no wall or other rib, so they're left as loose parts.", ["ribEdges"]);
            setErrorEntities(context, id, { "entities" : qUnion(loose) });
        }
    });

/**
 * The faces to lighten: those selected, and the parts' faces under the sketch regions selected (as a sketch on a face
 * covers it, making the face itself hard to click).
 */
function getFaces(context is Context, definition is map) returns Query
{
    const selected = qEntityFilter(definition.faces, EntityType.FACE);
    if (isQueryEmpty(context, selected))
    {
        throw regenError("Select the faces to lighten.", ["faces"]);
    }
    var faces = [qSketchFilter(selected, SketchObject.NO)];
    for (var region in evaluateQuery(context, qSketchFilter(selected, SketchObject.YES)))
    {
        const under = partFacesUnder(context, region);
        if (under == [])
        {
            throw regenError("A sketch region selected to lighten isn't on a part's face.", ["faces"], region);
        }
        faces = append(faces, qUnion(under));
    }
    return qUnion(faces);
}

/**
 * The flat faces of modifiable parts which a sketch region lies on: in its plane, and touching it.
 */
function partFacesUnder(context is Context, region is Query) returns array
{
    const plane = evPlane(context, { "face" : region });
    const candidates = qCoincidesWithPlane(qOwnedByBody(qAllModifiableSolidBodiesNoMesh(), EntityType.FACE), plane);
    return filter(evaluateQuery(context, candidates), function(face)
        {
            return tolerantEqualsZero(evDistance(context, { "side0" : region, "side1" : face }).distance);
        });
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
 * outside them and the ribs; the part is shelled, removing its faces parallel to the faces to lighten (and the ignored
 * ones), to leave walls; the two are joined, and the pockets' corners are filleted. Returns the pieces of the copy which
 * didn't join the part (ribs touching no wall or other rib), which are left as parts of their own.
 */
function lightenPart(context is Context, id is Id, partId is Id, definition is map, plane is Plane, part is Query, faces is Query,
    pockets is Query) returns array
{
    const partFaces = qOwnedByBody(part, EntityType.FACE);
    const openFaces = qUnion([qParallelPlanes(partFaces, plane.normal, true), qIntersection([definition.ignoredFaces, partFaces])]);
    const lightened = qIntersection([faces, partFaces]);

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
                "faultyParameters" : ["faces"],
                "entities" : qUnion([lightened, pockets])
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
                    "faultyParameters" : ["faces"],
                    "entities" : qUnion([part, kept])
                });
    }

    if (definition.filletCorners)
    {
        filletCorners(context, id, partId + "fillet", definition, plane, part);
    }
    // Pieces of the copy touching the part's walls were joined to it
    return evaluateQuery(context, qSubtraction(kept, part));
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
