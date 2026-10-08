FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "6b7d5995c565ae73c7740b0b");

export import(path : "21762d39019c8b2289e2fbb8", version : "8f82cf693e7833130ba80201");
export import(path : "0195d390c3944cd4fab21ce0", version : "2087a92c024fe3ea73f587fa");
export import(path : "b75434df23d86ba9542f761e", version : "410f29dc5fa8b0fe88f1e9c5");
export import(path : "6e24956e9977116c79280620", version : "0ec5da0acf56336b68065e37");
// Exports Fit, a parameter type
export import(path : "core/fit.fs", version : "");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");

annotation { "Feature Type Name" : "Robot spline profile",
        "Feature Type Description" : "Create MAXSpline, SplineXL, and SplineXS profiles." ~ CREDIT,
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

        // Of what goes on the shaft (an outside profile) or in it (an inside profile)
        fitPredicate(definition);

        extrudePredicate(definition);

        booleanStepScopePredicate(definition);
    }
    {
        definition.entities = createEntities(context, id + "profile", definition);

        // Capture the boolean operation so we can intercept the created tools and add manipulators
        const booleanOpType = definition.operationType;
        definition.operationType = NewBodyOperationType.NEW;
        callSubfeatureAndProcessStatus(id, extrude, context, id, definition, { "featureParameterMap" : { "entities" : "location" } });

        // An inside profile is always offset, from the outside profile it's sketched as
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
                throw regenError("Failed to fit the spline profile.", ["fit", "fitClearance"]);
            }
        }

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

/**
 * How far to offset the outside profile the spline's sketched as: for an outside profile, out by half its fit's
 * clearance (as what goes on the shaft is bigger than it); for an inside profile, in by the tube's wall (1/16 in.), and
 * half its fit's clearance more (as what goes in the tube is smaller than it).
 */
function getSplineProfileOffset(definition is map) returns ValueWithUnits
{
    // The fit's for the shaft's size, inside it or out (its range is the same)
    const clearance = fitClearance(definition, splineDiameter(definition.splineType));
    if (definition.profileSide == ProfileSide.INSIDE)
    {
        return -(0.0625 * inch + clearance / 2);
    }
    return clearance / 2;
}

export function robotSplineProfileManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    return extrudeManipulatorChange(context, definition, newManipulators);
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
