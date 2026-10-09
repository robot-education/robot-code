FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "6b7d5995c565ae73c7740b0b");
import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");

import(path : "0195d390c3944cd4fab21ce0", version : "2087a92c024fe3ea73f587fa");
export import(path : "01402b7c9eebd8bf0b5d3e52", version : "afd3970cf2628429b3763f68");
import(path : "6e24956e9977116c79280620", version : "0ec5da0acf56336b68065e37");
export import(path : "58d66340f7b70cfc86606676", version : "c66f2cde90ee0c14ff94cd63");
export import(path : "aff3918ff64d6eafb99fddcf", version : "4661373950000ab2acd87f53");
import(path : "eb11a2948f8123134339137f", version : "2209aff42808fb5a7c367b91");
// Exports the adapter enums, which Onshape requires since they're parameter types
export import(path : "6451a02d1f9f40630984864b", version : "ef6cb5b6d8c95ab1a3e79148");
import(path : "aa47f3d3eb754118903deeec", version : "812299f393e144ff2d6711d6");
// Exports Fit, a parameter type
export import(path : "926d933eb33b11a3452660fd", version : "9f460f5afe4b1aa32d2f1898");
import(path : "01f0c5634015659514b83da1", version : "5054ae3d069649e06068d82b");

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
        printAdapterSelectionPredicate(definition);

        if (printAdapterHasBoss(definition))
        {
            annotation { "Name" : "Use boss", "Default" : true, "Description" : "Leave room for the adapter's boss above the print, instead of sinking the whole adapter in.", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.useBoss is boolean;
        }

        locationPredicate(definition, "print adapter");

        annotation { "Name" : "Opposite direction", "UIHint" : ["OPPOSITE_DIRECTION", "FIRST_IN_ROW"] }
        definition.oppositeDirection is boolean;

        // Of the adapter in its pocket
        fitPredicate(definition);

        holeMergeScopePredicate(definition);

        annotation { "Name" : "Add bore" }
        definition.addBore is boolean;

        if (definition.addBore)
        {
            annotation { "Group Name" : "Add bore", "Collapsed By Default" : false, "Driving Parameter" : "addBore" }
            {
                if (printAdapterHasSplineXsBore(definition))
                {
                    annotation { "Name" : "Bore type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
                    definition.boreType is PrintBoreType;
                }

                // Of the bore on its shaft
                boreFitPredicate(definition);

                simpleExtrudePredicate(definition);

                entranceChamferPredicate(definition);
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
        var boreFaces;
        if (definition.addBore)
        {
            boreFaces = createPrintBore(context, id, definition, locationPlane);
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

        if (definition.addBore && definition.entranceChamfer)
        {
            // The bore's sides, followed through the boolean into the parts
            chamferBoreEntrances(context, id + "entranceChamfer", boreFaces, definition.chamferDistance);
        }
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
    // The fit's for the adapter's size across its outline
    const clearance = fitClearance(definition, profileAcross(context, qCreatedBy(sketchId, EntityType.FACE), plane));

    cleanup(context, adapterId + "delete", qCreatedBy(sketchId, EntityType.BODY));

    if (!tolerantEqualsZero(clearance))
    {
        // The pocket's a tool, so growing it (half the clearance on each side) grows the pocket
        try
        {
            opOffsetFace(context, adapterId + "offset", {
                        "moveFaces" : outsideFaces,
                        "offsetDistance" : clearance / 2
                    });
        }
        catch
        {
            throw regenError("Failed to fit the print adapter's pocket. Is its clearance too large?", ["fit", "fitClearance"], qCreatedBy(id, EntityType.BODY));
        }
    }
}

/**
 * Makes the bore's tool, and returns its sides, tracked (so they're the bore's sides in the parts, once it's cut).
 */
function createPrintBore(context is Context, id is Id, definition is map, plane is Plane) returns Query
{
    // We need to use top level id for the extrude here, so don't make a specific boreId
    const sketchId = id + "sketch";

    plane.normal *= definition.oppositeDirection ? -1 : 1;
    const boreProfile = sketchBoreProfile(context, sketchId, definition, plane);

    const extrudeDefinition = transformDefintionForSimpleExtrude(definition, boreProfile);

    const extrudeId = id;
    callSubfeatureAndProcessStatus(id, extrude, context, extrudeId, extrudeDefinition, { "featureParameterMap" : { "entities" : "location" } });
    // The bore's sides: what's made under the feature's id is the adapter's pocket too
    const outsideFaces = qSubtraction(qNonCapEntity(extrudeId, EntityType.FACE), qCreatedBy(id + "adapter", EntityType.FACE));

    cleanup(context, id + "deleteBore", qCreatedBy(sketchId, EntityType.BODY));

    const clearance = boreFitClearance(definition, boreAcross(definition));
    if (!tolerantEqualsZero(clearance))
    {
        // The bore's a tool, so growing it (half the clearance on each side) grows the bore
        try
        {
            opOffsetFace(context, id + "offsetBoreFaces", {
                        "moveFaces" : outsideFaces,
                        "offsetDistance" : clearance / 2
                    });
        }
        catch
        {
            throw regenError("Failed to fit the bore. Is its clearance too large?", ["boreFit", "boreFitClearance"], qCreatedBy(id, EntityType.BODY));
        }
    }
    return startTracking(context, outsideFaces);
}

/**
 * The size across the shaft the bore fits: a SplineXS shaft's major diameter, a hex's width across its flats, or the
 * clearance circle's diameter.
 */
function boreAcross(definition is map) returns ValueWithUnits
{
    const bore = getPrintAdapter(definition).bore;
    if (printAdapterHasSplineXsBore(definition) && definition.boreType == PrintBoreType.SPLINE_XS)
    {
        return splineDiameter(SplineType.SPLINE_XS);
    }
    return bore.hexSize ?? bore.diameter;
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
    if (definition.addBore)
    {
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
