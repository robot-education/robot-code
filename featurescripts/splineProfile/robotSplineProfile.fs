FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "2073caea5ae472033c5090d9");

export import(path : "21762d39019c8b2289e2fbb8", version : "06bafd6cfddc3fbe92c47892");
export import(path : "0195d390c3944cd4fab21ce0", version : "eb719b1576c924e1ac6e1ffa");
export import(path : "b75434df23d86ba9542f761e", version : "ba222d9a7c55b13cef9c1c62");
export import(path : "6e24956e9977116c79280620", version : "1cdcfd6334c53e51fef6f5f5");
import(path : "0103ad63394d7713fbf44448", version : "d9ead1a79bded860ba8f3ddf");
import(path : "6c65805103086c85362ee4b7", version : "06268198ef2566cb246b9f56");

annotation { "Feature Type Name" : "Robot spline profile",
        "Feature Type Description" : "Create MAXSpline and SplineXL profiles." ~ CREDIT,
        "Manipulator Change Function" : "robotSplineProfileManipulatorChange",
        "Editing Logic Function" : "robotSplineProfileEditLogic",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotSplineProfile = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        booleanStepTypePredicate(definition);

        annotation { "Name" : "Spline type", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
        definition.splineType is SplineType;

        profileSidePredicate(definition);

        locationPredicate(definition, "spline");

        profileOffsetPredicate(definition);

        extrudePredicate(definition);

        booleanStepScopePredicate(definition);
    }
    {
        definition.entities = createEntities(context, id + "profile", definition);

        // Capture the boolean operation so we can intercept the created tools and add manipulators
        const booleanOpType = definition.operationType;
        definition.operationType = NewBodyOperationType.NEW;
        callSubfeatureAndProcessStatus(id, extrude, context, id, definition, { "featureParameterMap" : { "entities" : "location" } });

        // Sometimes offset is needed even if offsetProfile is false
        const profileOffset = getSplineProfileOffset(definition);
        if (!tolerantEqualsZero(profileOffset))
        {
            const outsideFaces = qNonCapEntity(id, EntityType.FACE);
            try
            {
                opOffsetFace(context, id + "offsetFaces", {
                            "moveFaces" : outsideFaces,
                            "offsetDistance" : profileOffset
                        });
            }
            catch
            {
                throw regenError("Failed to apply profile offset.", ["profileOffsetDistance"]);
            }
        }

        addSplineProfileOffsetManipulator(context, id, definition);

        // Boolean after extruding so we can add manipulators in the right spots
        const reconstructOp = function(id)
            {
                var tempDefinition = definition;
                tempDefinition.operationType = NewBodyOperationType.NEW;
                extrude(context, id, tempDefinition);

                if (!tolerantEqualsZero(profileOffset))
                {
                    const outsideFaces = qNonCapEntity(id, EntityType.FACE);
                    opOffsetFace(context, id + "offsetFaces", {
                                "moveFaces" : outsideFaces,
                                "offsetDistance" : profileOffset
                            });
                }
            };

        definition.operationType = booleanOpType;
        processNewBodyIfNeeded(context, id, definition, reconstructOp);

        // Cleanup after boolean to avoid deleting profile faces to early
        cleanup(context, id + "deleteProfiles", qCreatedBy(id + "profile", EntityType.BODY));
    });

function createEntities(context is Context, id is Id, definition is map) returns Query
{
    const plane = getLocationPlane(context, definition);
    const sketch = newSketchOnPlane(context, id + "sketch", { "sketchPlane" : plane });
    // Always use outside profile to maximize robustness
    skSplineProfile(sketch, "spline", { "splineType" : definition.splineType });
    skSolve(sketch);

    return qCreatedBy(id, EntityType.FACE);
}

function addSplineProfileOffsetManipulator(context is Context, id is Id, definition is map)
{
    if (!offsetProfile(definition))
    {
        return;
    }
    // A collection of heuristics to find the middle of the extrude
    const firstProfile = qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID)->qNthElement(0);
    const firstProfileEdge = qNonCapEntity(id, EntityType.EDGE)->qSketchFilter(SketchObject.NO)->qNthElement(0);

    const extrudeDirection = evEdgeTangentLine(context, {
                    "edge" : firstProfileEdge,
                    "parameter" : 0.5
                }).direction;

    const boundingBox = evBox3d(context, {
                "topology" : firstProfile,
                "tight" : false
            });
    const center = box3dCenter(boundingBox);
    const profileAxis = line(center, perpendicularVector(extrudeDirection));

    const flipped = definition.profileOffsetOppositeDirection;
    const flipDirection = definition.profileSide == ProfileSide.INSIDE;
    addProfileOffsetManipulator(context, id, PROFILE_OFFSET_MANIPULATOR, profileAxis, qNonCapEntity(id, EntityType.FACE), flipped, flipDirection);
}

function getSplineProfileOffset(definition is map) returns ValueWithUnits
{
    var profileOffset = getProfileOffset(definition);
    if (definition.profileSide == ProfileSide.INSIDE)
    {
        profileOffset += 0.0625 * inch;
        // Positive values point inwards
        profileOffset *= -1;
    }
    return profileOffset;
}

export function robotSplineProfileManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    definition = extrudeManipulatorChange(context, definition, newManipulators);
    definition = profileOffsetManipulatorChange(definition, newManipulators[PROFILE_OFFSET_MANIPULATOR], PROFILE_OFFSET_FLIP);
    return definition;
}

export function robotSplineProfileEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, specifiedParameters is map, hiddenBodies is Query) returns map
{
    // Create entities to trigger merge scope logic
    if (!isQueryEmpty(context, definition.locations))
    {
        definition.entities = try silent(createEntities(context, id, definition));
    }
    return stdExtrudeEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
}
