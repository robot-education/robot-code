FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "6b7d5995c565ae73c7740b0b");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");

import(path : "0195d390c3944cd4fab21ce0", version : "71278ebc72b57aad713b49aa");
export import(path : "01402b7c9eebd8bf0b5d3e52", version : "afd3970cf2628429b3763f68");
import(path : "0103ad63394d7713fbf44448", version : "93809a6b0922842a07809b6f");
export import(path : "58d66340f7b70cfc86606676", version : "88f7f55d3e4918ee4144e69e");
export import(path : "aff3918ff64d6eafb99fddcf", version : "dbbeeaaffabb7f49349dbec3");
import(path : "eb11a2948f8123134339137f", version : "2209aff42808fb5a7c367b91");
// Exports the adapter enums, which Onshape requires since they're parameter types
export import(path : "6451a02d1f9f40630984864b", version : "bc00aa3012cf45b0d092305a");
import(path : "aa47f3d3eb754118903deeec", version : "cdbb3e4ffa802ae7b7ce0f89");

/**
 * The bores SplineXS adapters can cut.
 */
export enum PrintBoreType
{
    annotation { "Name" : "Circle" }
    CIRCLE,
    annotation { "Name" : "SplineXS" }
    SPLINE_XS
}

annotation {
        "Feature Type Name" : "Robot print adapter",
        "Feature Type Description" : "Cuts mounting holes for 3D print adapters." ~ CREDIT,
        "Manipulator Change Function" : "robotPrintAdapterManipulatorChange",
        "Editing Logic Function" : "robotPrintAdapterEditLogic",
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotPrintAdapter = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Group Name" : "Selections", "Collapsed By Default" : false }
        {
            printAdapterSelectionPredicate(definition);

            if (printAdapterHasBoss(definition))
            {
                annotation { "Name" : "Use boss", "Default" : true, "Description" : "Leave room for the adapter's boss above the print, instead of sinking the whole adapter in.", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                definition.useBoss is boolean;
            }

            locationPredicate(definition, "print adapter");

            annotation { "Name" : "Opposite direction", "UIHint" : ["OPPOSITE_DIRECTION", "FIRST_IN_ROW"] }
            definition.oppositeDirection is boolean;

            profileOffsetPredicate(definition);

            holeMergeScopePredicate(definition);
        }

        annotation { "Name" : "Add bore" }
        definition.addBore is boolean;

        if (definition.addBore)
        {
            annotation { "Group Name" : "Add bore", "Collapsed By Default" : false, "Driving Parameter" : "addBore" }
            {
                if (printAdapterHasSplineXsBore(definition))
                {
                    annotation { "Name" : "Bore type", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
                    definition.boreType is PrintBoreType;
                }

                boreProfileOffsetPredicate(definition);

                simpleExtrudePredicate(definition);
            }
        }
    }
    {
        var locationPlane = getLocationPlane(context, definition);
        // By default, holes go down but extrudes go up, so flip the plane so everything is flipped correctly
        locationPlane.normal *= -1;

        if (definition.oppositeDirection)
        {
            locationPlane.normal *= -1;
        }

        createPrintAdapter(context, id, definition, locationPlane);
        if (definition.addBore)
        {
            createPrintBore(context, id, definition, locationPlane);
        }

        // Boolean after extruding so we can add manipulators in the right spots
        const reconstructOp = function(errorId)
            {
                createPrintAdapter(context, errorId, definition, locationPlane);
                if (definition.addBore)
                {
                    createPrintBore(context, errorId, definition, locationPlane);
                }
            };

        // Hold off on this so we have error bodies
        if (isQueryEmpty(context, definition.scope))
        {
            try
            {
                const errorId = id + "error";
                reconstructOp(errorId);
                setErrorEntities(context, id, { "entities" : qCreatedBy(errorId, EntityType.BODY) });
            }
            throw regenError(ErrorStringEnum.HOLE_EMPTY_SCOPE, ["scope"]);
        }

        processNewBodyIfNeeded(context, id, {
                    "operationType" : NewBodyOperationType.REMOVE,
                    "defaultScope" : false,
                    "booleanScope" : definition.scope
                }, reconstructOp);
    },
    {
            // For features made before these parameters
            "useBoss" : true,
            "boreType" : PrintBoreType.CIRCLE
        });

function createPrintAdapter(context is Context, id is Id, definition is map, plane is Plane)
{
    const adapterId = id + "adapter";
    const sketchId = adapterId + "sketchAdapter";
    
    createSketchDataArray(context, sketchId, {
                "plane" : plane,
                "sketchDataArray" : getPrintAdapter(definition).profile
            });

    opExtrude(context, adapterId + "extrude", {
                "entities" : qCreatedBy(sketchId, EntityType.FACE),
                "direction" : plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : getPocketDepth(definition)
            });
    const outsideFaces = qNonCapEntity(adapterId + "extrude", EntityType.FACE);

    cleanup(context, adapterId + "delete", qCreatedBy(sketchId, EntityType.BODY));

    const profileOffset = getProfileOffset(definition);
    if (!tolerantEqualsZero(profileOffset))
    {

        addProfileOffsetManipulator(context, id, PROFILE_OFFSET_MANIPULATOR, line(plane.origin, plane.x), outsideFaces, definition[PROFILE_OFFSET_FLIP]);

        try
        {
            opOffsetFace(context, adapterId + "offset", {
                        "moveFaces" : outsideFaces,
                        "offsetDistance" : profileOffset
                    });
        }
        catch
        {
            throw regenError("Failed to offset print adapter. Is the offset too large?", ["profileOffsetDistance"], qCreatedBy(id, EntityType.BODY));
        }
    }
}

function createPrintBore(context is Context, id is Id, definition is map, plane is Plane)
{
    // We need to use top level id for the extrude here, so don't make a specific boreId
    const sketchId = id + "sketch";
    
    plane.normal *= definition.oppositeDirection ? -1 : 1;
    const boreProfile = sketchBoreProfile(context, sketchId, definition, plane);

    const extrudeDefinition = transformDefintionForSimpleExtrude(definition, boreProfile);

    const extrudeId = id;
    callSubfeatureAndProcessStatus(id, extrude, context, extrudeId, extrudeDefinition, { "featureParameterMap" : { "entities" : "location" } });
    const outsideFaces = qNonCapEntity(extrudeId, EntityType.FACE);

    cleanup(context, id + "deleteBore", qCreatedBy(sketchId, EntityType.BODY));

    const boreOffset = getBoreProfileOffset(definition);
    if (!tolerantEqualsZero(boreOffset))
    {
        addProfileOffsetManipulator(context, id, BORE_PROFILE_OFFSET_MANIPULATOR, line(plane.origin + plane.normal * getPocketDepth(definition), plane.x), outsideFaces, definition[BORE_PROFILE_OFFSET_FLIP]);

        try
        {
            opOffsetFace(context, id + "offsetBoreFaces", {
                        "moveFaces" : outsideFaces,
                        "offsetDistance" : boreOffset
                    });
        }
        catch
        {
            throw regenError("Failed to offset bore. Is the offset too large?", ["boreProfileOffsetDistance"], qCreatedBy(id, EntityType.BODY));
        }
    }
}

function sketchBoreProfile(context is Context, id is Id, definition is map, plane is Plane) returns Query
{
    const sketch = newSketchOnPlane(context, id, { "sketchPlane" : plane });
    const bore = getPrintAdapter(definition).bore;
    if (printAdapterHasSplineXsBore(definition) && definition.boreType == PrintBoreType.SPLINE_XS)
    {
        skDataArray(sketch, "splineXs", { "sketchDataArray" : SPLINE_XS_HOLE });
    }
    else if (bore.hexSize != undefined)
    {
        const hexRadius = (bore.hexSize / 2) / cos(30 * degree);
        skRegularPolygon(sketch, "polygon1", {
                    "center" : zeroVector(2) * meter,
                    "firstVertex" : vector(hexRadius, 0 * meter),
                    "sides" : 6
                });
    }
    else
    {
        skCircle(sketch, "circle", {
                    "center" : zeroVector(2) * meter,
                    "radius" : bore.diameter / 2
                });
    }

    skSolve(sketch);
    return qSketchRegion(id);
}

export function robotPrintAdapterManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    definition = profileOffsetManipulatorChange(definition, newManipulators[PROFILE_OFFSET_MANIPULATOR], PROFILE_OFFSET_FLIP);
    if (definition.addBore)
    {
        definition = profileOffsetManipulatorChange(definition, newManipulators[BORE_PROFILE_OFFSET_MANIPULATOR], BORE_PROFILE_OFFSET_FLIP);

        var extrudeDefinition = definition;
        extrudeDefinition.hasSecondDirection = false;
        extrudeDefinition.symmetric = false;
        definition = extrudeManipulatorChange(context, extrudeDefinition, newManipulators);
        definition.hasSecondDirection = undefined;
        definition.symmetric = undefined;
    }
    return definition;
}

export function robotPrintAdapterEditLogic(context is Context, id is Id, oldDefinition is map, definition is map,
    isCreating is boolean, specifiedParameters is map, hiddenBodies is Query) returns map
{
    return mountingEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
}

/**
 * The depth of the pocket for the adapter: all of it, or all but its boss.
 */
function getPocketDepth(definition is map) returns ValueWithUnits
{
    const adapter = getPrintAdapter(definition);
    if (printAdapterHasBoss(definition) && definition.useBoss)
    {
        return adapter.depth - adapter.boss;
    }
    return adapter.depth;
}
