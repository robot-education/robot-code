FeatureScript 2522;
import(path : "onshape/std/geometry.fs", version : "2522.0");

/**
 * Parts of this FS and the calculations involved in it were created by Ilya Baran
 */

const DEPTH_MANIPULATOR = "depthManipulator";

export enum VentBoundingType
{
    annotation { "Name" : "Blind" }
    BLIND,
    // annotation { "Name" : "Up to next" } // This doesn't work :(
    // UP_TO_NEXT,
    annotation { "Name" : "Up to face" }
    UP_TO_SURFACE,
    annotation { "Name" : "Up to part" }
    UP_TO_BODY,
    annotation { "Name" : "Up to vertex", "Hidden" : true } // Not implemented yet
    UP_TO_VERTEX,
    annotation { "Name" : "Through all" }
    THROUGH_ALL
}



/**
 * A `LengthBoundSpec` for fillets and chamfers, with smaller defaults than `NONNEGATIVE_LENGTH_BOUNDS`
 * (`0.2 * inch`, etc.), also allowing zero.
 */
export const BLEND_BOUNDS_WITH_ZERO =
{
            (meter) : [0, 0.005, 500],
            (centimeter) : 0.5,
            (millimeter) : 5.0,
            (inch) : 0.2,
            (foot) : 0.015,
            (yard) : 0.005
        } as LengthBoundSpec;

annotation { "Feature Type Name" : "Vent", "Manipulator Change Function" : "makeVentManipulatorChange" }
export const makeVent = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Faces to remove", "Filter" : EntityType.FACE && GeometryType.PLANE }
        definition.faces is Query;

        annotation { "Name" : "Parts to lighten", "Filter" : BodyType.SOLID && EntityType.BODY }
        definition.bodies is Query;

        annotation { "Name" : "Trim faces with part", "Default" : true, "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
        definition.trimFaces is boolean;

        annotation { "Name" : "Thickness of walls", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
        isLength(definition.thickness, BLEND_BOUNDS);

        annotation { "Name" : "Tool radius", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
        isLength(definition.radius, BLEND_BOUNDS_WITH_ZERO);

        annotation { "Name" : "Sharp corners", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
        definition.sharpCorners is boolean;

        annotation { "Name" : "End type", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
        definition.endBound is VentBoundingType;

        if (definition.endBound == VentBoundingType.BLIND)
        {
            annotation { "Name" : "Cut depth", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.depth, LENGTH_BOUNDS);
        }
        else if (definition.endBound == VentBoundingType.UP_TO_SURFACE)
        {
            annotation { "Name" : "Up to face",
                        "Filter" : EntityType.FACE && SketchObject.NO,
                        "MaxNumberOfPicks" : 1 }
            definition.endBoundEntityFace is Query;
        }
        else if (definition.endBound == VentBoundingType.UP_TO_BODY)
        {
            annotation { "Name" : "Up to surface or part",
                        "Filter" : EntityType.BODY && (BodyType.SOLID || BodyType.SHEET) && SketchObject.NO,
                        "MaxNumberOfPicks" : 1 }
            definition.endBoundEntityBody is Query;
        }
        else if (definition.endBound == VentBoundingType.UP_TO_VERTEX) // Not implemented
        {
            annotation { "Name" : "Up to vertex or mate connector",
                        "Filter" : EntityType.VERTEX || BodyType.MATE_CONNECTOR,
                        "MaxNumberOfPicks" : 1 }
            definition.endBoundEntityVertex is Query;
        }
    }
    {
        var faces = evaluateQuery(context, definition.faces);

        if (faces == [])
            throw regenError(ErrorStringEnum.CANNOT_RESOLVE_ENTITIES, ["faces"]);

        var toDelete = new box([]);
        var remainingTransform = getRemainderPatternTransform(context, { "references" : qUnion(faces) });
        if (remainingTransform != identityTransform())
        {
            var newFaces = startTracking(context, qUnion(faces));
            opPattern(context, id + "patternFaces", {
                        "entities" : qOwnerBody(qUnion(faces)),
                        "transforms" : [remainingTransform],
                        "instanceNames" : ["1"]
                    });
            faces = evaluateQuery(context, newFaces);
            toDelete[] = append(toDelete[], qOwnerBody(qUnion(faces)));
        }

        var facePlane = evPlane(context, {
                "face" : faces[0]
            });

        var bodies = evaluateQuery(context, definition.bodies) == [] ? evaluateQuery(context, getAdjacentBodies(context, qUnion(faces))) : evaluateQuery(context, definition.bodies);

        if (bodies == []) // We throw this only if we cannot compute any bodies for use
            throw regenError(ErrorStringEnum.CANNOT_RESOLVE_ENTITIES, ["bodies"]);

        var wallThickness = definition.thickness;

        var toolRadius = definition.radius;

        // Check that all the faces are coplanar
        for (var face in faces)
            if (!coplanarPlanes(evPlane(context, { "face" : face }), facePlane))
                throw regenError("Faces must be coplanar", ["faces"], qUnion([faces[0], face]));


        var partCoplanarFacesToUse = [];

        // Planar faces that are coplanar with faces[0]
        var partCoplanarFaces = evaluateQuery(context, qIntersectsPlane(
            qParallelPlanes(
                qGeometry(qOwnedByBody(qUnion(bodies), EntityType.FACE), GeometryType.PLANE),
                facePlane,
                true
                ),
            facePlane
            ));

        var direction;
        for (var face in partCoplanarFaces)
            if (evCollision(context, {
                            "tools" : qUnion(faces),
                            "targets" : face
                        }) != [])
            {
                var partCoplanarFacePlane = evPlane(context, {
                        "face" : face
                    });

                if (direction is Vector && !tolerantEquals(partCoplanarFacePlane.normal, direction))
                    throw regenError("Cannot determine direction: coplanar faces are pointing in different directions");
                else
                    direction = partCoplanarFacePlane.normal;

                partCoplanarFacesToUse = append(partCoplanarFacesToUse, face);
            }

        if (partCoplanarFacesToUse == [])
            throw regenError("Selected faces to remove are not on faces of parts");

        var depth = switch (definition.endBound) {
                VentBoundingType.THROUGH_ALL : evBox3d(context, {
                            "topology" : qUnion(bodies),
                            "tight" : true,
                            "cSys" : coordSystem(facePlane.origin, perpendicularVector(direction), -direction)
                        }).maxCorner[2],
                VentBoundingType.BLIND : definition.depth
            };

        if (definition.endBound == VentBoundingType.BLIND)
            addManipulators(context, id, {
                        (DEPTH_MANIPULATOR) : linearManipulator({
                                "base" : evFaceTangentPlane(context, {
                                            "face" : faces[0],
                                            "parameter" : vector(0.5, 0.5)
                                        }).origin,
                                "direction" : -direction,
                                "offset" : definition.depth,
                                "sources" : qUnion(faces)
                            })
                    });

        partCoplanarFacesToUse = qUnion(partCoplanarFacesToUse);

        //Extrude the selected faces by `depth + thickness/2 + radius` in `-direction` and `thickness/2 + radius` in `direction`

        var capSelectedMap = opExtrudeSeparate(context, id + "selectedFaces" + "extrude", {
                "entities" : qUnion(faces),
                "direction" : -direction,
                "endBound" : depth != undefined ? BoundingType.BLIND : definition.endBound as BoundingType,
                "endDepth" : try silent(depth + wallThickness / 2 + toolRadius),
                "endBoundEntity" : getEndBoundEntity(context, definition),
                "startBound" : BoundingType.BLIND,
                "startDepth" : wallThickness / 2 + toolRadius
            });

        var capEntities = qUnion([capSelectedMap[CapType.EITHER], startTracking(context, capSelectedMap[CapType.EITHER])]);

        // If this is up to something, move the end faces because the `shellAndEncloseWithConcaveFillet` offsets it in
        if (depth == undefined)
        {
            opOffsetFace(context, id + "selectedFaces" + "offsetEndFaces", {
                        "moveFaces" : capSelectedMap[CapType.END],
                        "offsetDistance" : wallThickness / 2 + toolRadius
                    });
        }

        shellAndEncloseWithConcaveFillet(context, id + "selectedFaces" + "shell", {
                    "entities" : qCreatedBy(id + "selectedFaces" + "extrude", EntityType.BODY),
                    "offset" : wallThickness / 2 + toolRadius,
                    "sharpCorners" : definition.sharpCorners,
                }, toDelete);

        //Extrude the part faces by depth + thickness + radius in -direction and thickness + radius in the other

        if (definition.trimFaces)
            try
            {
                opExtrude(context, id + "partFaces" + "extrude", {
                            "entities" : partCoplanarFacesToUse,
                            "direction" : -direction,
                            "endBound" : depth != undefined ? BoundingType.BLIND : definition.endBound as BoundingType,
                            "endDepth" : try silent(depth + wallThickness + toolRadius),
                            "endBoundEntity" : getEndBoundEntity(context, definition),
                            "startBound" : BoundingType.BLIND,
                            "startDepth" : wallThickness + toolRadius
                        });

                capEntities = qUnion([capEntities, startTracking(context, qCapEntity(id + "partFaces" + "extrude", CapType.EITHER, EntityType.FACE))]);

                // If this is up to something, move the end faces because the `shellAndEncloseWithConcaveFillet` offsets it in
                if (depth == undefined)
                {
                    opOffsetFace(context, id + "partFaces" + "offsetEndFaces", {
                                "moveFaces" : qCapEntity(id + "partFaces" + "extrude", CapType.END, EntityType.FACE),
                                "offsetDistance" : wallThickness + toolRadius
                            });
                }

                shellAndEncloseWithConcaveFillet(context, id + "partFaces" + "shell", {
                            "entities" : qCreatedBy(id + "partFaces" + "extrude", EntityType.BODY),
                            "offset" : wallThickness + toolRadius,
                            "sharpCorners" : definition.sharpCorners,
                        }, toDelete);

                // Boolean the two extrusions together using SUBTRACT_COMPLEMENT

                opBoolean(context, id + "combineExtrudes", {
                            "tools" : qSubtraction(qCreatedBy(id + "partFaces", EntityType.BODY), qUnion(toDelete[])),
                            "targets" : qSubtraction(qCreatedBy(id + "selectedFaces", EntityType.BODY), qUnion(toDelete[])),
                            "operationType" : BooleanOperationType.SUBTRACT_COMPLEMENT,
                            "keepTools" : false
                        });

                toDelete[] = append(toDelete[], qCreatedBy(id + "partFaces", EntityType.BODY));
            }
            catch
            {
                reportFeatureWarning(context, id, "Trimming with part faces failed. Feature may be incorrect");
            }

        opDeleteBodies(context, id + "delete", {
                    "entities" : qUnion(toDelete[])
                });

        capEntities = qUnion(evaluateQuery(context, capEntities));

        if (toolRadius > TOLERANCE.zeroLength * meter)
            // Expand the body by toolRadius
            expandWithFillet(context, id + "expandCombined", {
                        "entities" : qCreatedBy(id + "selectedFaces", EntityType.BODY),
                        "noEditFaces" : qUnion([capEntities, startTracking(context, capEntities)]),
                        "offset" : toolRadius
                    });

        // For sheet metal capability, use booleanBodies
        booleanBodies(context, id + "booleanFromPart", {
                    "tools" : qCreatedBy(id + "selectedFaces", EntityType.BODY),
                    "targets" : qUnion(bodies),
                    "operationType" : BooleanOperationType.SUBTRACTION
                });
    });

function getEndBoundEntity(context is Context, definition is map)
{
    if (definition.endBound == VentBoundingType.UP_TO_SURFACE)
    {
        verifyNonemptyQuery(context, definition, "endBoundEntityFace", ErrorStringEnum.EXTRUDE_SELECT_TERMINATING_SURFACE);
        return definition.endBoundEntityFace;
    }
    else if (definition.endBound == VentBoundingType.UP_TO_BODY)
    {
        verifyNonemptyQuery(context, definition, "endBoundEntityBody", ErrorStringEnum.EXTRUDE_SELECT_TERMINATING_BODY);
        return definition.endBoundEntityBody;
    }
    return undefined;
}

export function makeVentManipulatorChange(context is Context, definition is map, manipulators is map) returns map
{
    if (manipulators[DEPTH_MANIPULATOR] != undefined)
        definition.depth = abs(manipulators[DEPTH_MANIPULATOR].offset);
    return definition;
}

function shellAndEncloseWithConcaveFillet(context is Context, id is Id, definition is map, toDelete is box)
{
    // Fillet concave edges in the parts

    var edgesToFillet = [];

    if (!definition.sharpCorners)
    {
        var edges = evaluateQuery(context, qOwnedByBody(definition.entities, EntityType.EDGE));
        for (var edge in edges)
            if (evEdgeConvexity(context, { "edge" : edge }) == EdgeConvexityType.CONCAVE)
                edgesToFillet = append(edgesToFillet, edge);
    }

    if (edgesToFillet != [])
        opFillet(context, id + "fillet", {
                    "entities" : qUnion(edgesToFillet),
                    "radius" : TOLERANCE.booleanDefaultTolerance * meter
                });

    var filletsToEdit = startTracking(context, qCreatedBy(id + "fillet", EntityType.FACE));

    toDelete[] = append(toDelete[], definition.entities);

    // Shell the parts

    opShell(context, id + "shell", {
                "entities" : definition.entities,
                "thickness" : -definition.offset
            });

    // Enclose the parts

    opEnclose(context, id + "enclose", {
                "entities" : definition.entities
            });

    // Modify the fillet to the correct radius

    if (edgesToFillet != [])
        try
        {
            opModifyFillet(context, id + "modifyFillet", {
                        "faces" : qIntersection([filletsToEdit, qCreatedBy(id + "enclose")]),
                        "modifyFilletType" : ModifyFilletType.CHANGE_RADIUS,
                        "radius" : definition.offset
                    });
        }
}

function expandWithFillet(context is Context, id is Id, definition is map)
{
    // Fillet convex edges in the parts

    var edgesToFillet = [];
    var edges = evaluateQuery(context, qSubtraction(qOwnedByBody(definition.entities, EntityType.EDGE), qAdjacent(definition.noEditFaces, AdjacencyType.EDGE, EntityType.EDGE)));
    for (var edge in edges)
        if (evEdgeConvexity(context, { "edge" : edge }) == EdgeConvexityType.CONVEX)
            edgesToFillet = append(edgesToFillet, edge);

    if (edgesToFillet != [])
        opFillet(context, id + "fillet", {
                    "entities" : qUnion(edgesToFillet),
                    "radius" : TOLERANCE.booleanDefaultTolerance * meter
                });

    var filletsToEdit = qCreatedBy(id + "fillet", EntityType.FACE);

    // Move the part faces outwards by offset

    opOffsetFace(context, id + "offsetFace", {
                "moveFaces" : qSubtraction(qOwnedByBody(definition.entities, EntityType.FACE), definition.noEditFaces),
                "offsetDistance" : definition.offset
            });

    // Modify the fillet to the correct radius

    if (edgesToFillet != [])
        opModifyFillet(context, id + "modifyFillet", {
                    "faces" : filletsToEdit,
                    "modifyFilletType" : ModifyFilletType.CHANGE_RADIUS,
                    "radius" : definition.offset
                });
}

// Returns a map from capType to the faces
function opExtrudeSeparate(context is Context, id is Id, definition is map)
{
    var out = { CapType.EITHER : [], CapType.START : [], CapType.END : [] };
    var entities = evaluateQuery(context, definition.entities);
    var numSuccess = 0;
    for (var i = 0; i < size(entities); i += 1)
    {
        var extrudeDef = definition;
        extrudeDef.entities = entities[i];
        setExternalDisambiguation(context, id + unstableIdComponent(i), entities[i]);
        try
        {
            opExtrude(context, id + unstableIdComponent(i), extrudeDef);
            numSuccess += 1;
        }
        for (var capType in keys(out))
            out[capType] = append(out[capType], qCapEntity(id + unstableIdComponent(i), capType, EntityType.FACE));
    }
    if (numSuccess == 0)
        throw regenError("Extrude failed");
    for (var capType in keys(out))
    {
        out[capType] = qUnion(out[capType]);
    }
    return out;
}

function getAdjacentBodies(context is Context, faces is Query)
{
    var hasSolidFaces = evaluateQuery(context, qBodyType(faces, BodyType.SOLID)) != [];

    if (hasSolidFaces)
        return qOwnerBody(faces);
    else
    {
        var bodies = [];
        var collisions = evCollision(context, {
                "tools" : faces,
                "targets" : qBodyType(qEverything(EntityType.BODY), BodyType.SOLID)
            });
        for (var collision in collisions)
            bodies = append(bodies, collision.targetBody);
        return qUnion(bodies);
    }
}
