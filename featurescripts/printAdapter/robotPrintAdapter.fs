FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "2073caea5ae472033c5090d9");
import(path : "6c65805103086c85362ee4b7", version : "06268198ef2566cb246b9f56");

import(path : "0195d390c3944cd4fab21ce0", version : "eb719b1576c924e1ac6e1ffa");
export import(path : "01402b7c9eebd8bf0b5d3e52", version : "76161d325e4bc689a054d495");
import(path : "0103ad63394d7713fbf44448", version : "d9ead1a79bded860ba8f3ddf");
export import(path : "58d66340f7b70cfc86606676", version : "45bbe2db54d801bb072f1cc1");
export import(path : "aff3918ff64d6eafb99fddcf", version : "998f2649991756ad5a38110f");
import(path : "eb11a2948f8123134339137f", version : "aa3b93f58a282fb8286a97ca");
import(path : "printAdapter/printAdapterProfiles.gen.fs", version : "");
import(path : "splineProfile/splineProfiles.gen.fs", version : "");

// Sorted by vendor, then part number
export enum PrintAdapter
{
    annotation { "Name" : "AndyMark 1/2\" Hex Insert (am-5654)" }
    ANDYMARK_HEX_INSERT,
    annotation { "Name" : "AndyMark 3/8\" Hex Insert (am-5655)" }
    ANDYMARK_3_8_HEX_INSERT,
    annotation { "Name" : "AndyMark 8mm Keyed Insert (am-5656)" }
    ANDYMARK_8MM_KEYED_INSERT,
    annotation { "Name" : "AndyMark Kraken Spline Insert (am-5657)" }
    ANDYMARK_KRAKEN_INSERT,
    annotation { "Name" : "Swyft 1/2\" Hex Adapter (SR-HEXto3DPRINT-01)" }
    SWYFT_HEX_ADAPTER,
    annotation { "Name" : "TTB 1/2\" Hex Insert (TTB-0034)" }
    TTB_HEX_INSERT,
    annotation { "Name" : "TTB SplineXS Insert (TTB-0356)" }
    TTB_SPLINE_INSERT,
    annotation { "Name" : "WCP SplineXS Adapter (WCP-1021)" }
    WCP_SPLINE_ADAPTER,
    annotation { "Name" : "WCP 1/2\" Hex Adapter (WCP-1121)" }
    WCP_HEX_ADAPTER
}

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
            annotation { "Name" : "Adapter", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
            definition.printAdapter is PrintAdapter;

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
                if (PRINT_ADAPTERS[definition.printAdapter].bore.splineXs == true)
                {
                    annotation { "Name" : "Bore type", "Description" : "A clearance circle, or the SplineXS profile itself.", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
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
    });

function createPrintAdapter(context is Context, id is Id, definition is map, plane is Plane)
{
    const adapterId = id + "adapter";
    const sketchId = adapterId + "sketchAdapter";
    
    createSketchDataArray(context, sketchId, {
                "plane" : plane,
                "sketchDataArray" : PRINT_ADAPTERS[definition.printAdapter].profile
            });

    opExtrude(context, adapterId + "extrude", {
                "entities" : qCreatedBy(sketchId, EntityType.FACE),
                "direction" : plane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : PRINT_ADAPTERS[definition.printAdapter].depth
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
        addProfileOffsetManipulator(context, id, BORE_PROFILE_OFFSET_MANIPULATOR, line(plane.origin + plane.normal * PRINT_ADAPTERS[definition.printAdapter].depth, plane.x), outsideFaces, definition[BORE_PROFILE_OFFSET_FLIP]);

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
    const bore = PRINT_ADAPTERS[definition.printAdapter].bore;
    if (bore.splineXs == true && definition.boreType == PrintBoreType.SPLINE_XS)
    {
        skDataArray(sketch, "splineXs", { "sketchDataArray" : SPLINE_XS_HOLE });
    }
    else if (bore.hexSize != undefined)
    {
        const hexRadius = (bore.hexSize / 2) / cos(30 * degree);
        skRegularPolygon(sketch, "polygon1", {
                    "center" : zeroVector(2) * meter,
                    "firstVertex" : vector(cos(bore.vertexAngle), sin(bore.vertexAngle)) * hexRadius,
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
    definition = mountingEditLogic(context, id, oldDefinition, definition, specifiedParameters, hiddenBodies);
    return definition;
}


// SplineXS shafts are 8mm across
const SPLINE_XS_BORE = { "diameter" : 8.5 * millimeter, "splineXs" : true };

/**
 * Each adapter's `profile`, the `depth` of the pocket for it, and the `bore` cut through the print for its shaft.
 *
 * A bore is a hex (`hexSize` across flats, with a corner at `vertexAngle`, timed to match the adapter), or a
 * clearance circle (`diameter`), which SplineXS adapters (`splineXs`) can replace with a SplineXS profile.
 */
const PRINT_ADAPTERS = {
        // AndyMark's inserts are 0.25 thick with a boss on one side; the toothed part is 0.22 thick
        PrintAdapter.ANDYMARK_HEX_INSERT : {
                "profile" : ANDYMARK_HEX_INSERT_PROFILE,
                "depth" : 0.22 * inch,
                "bore" : { "hexSize" : 0.5 * inch, "vertexAngle" : 90 * degree }
            },
        PrintAdapter.ANDYMARK_3_8_HEX_INSERT : {
                "profile" : ANDYMARK_SMALL_INSERT_PROFILE,
                "depth" : 0.22 * inch,
                "bore" : { "hexSize" : 0.375 * inch, "vertexAngle" : 90 * degree }
            },
        PrintAdapter.ANDYMARK_8MM_KEYED_INSERT : {
                "profile" : ANDYMARK_SMALL_INSERT_PROFILE,
                "depth" : 0.22 * inch,
                // Clears the key, which reaches 4.9mm from the center
                "bore" : { "diameter" : 10 * millimeter }
            },
        PrintAdapter.ANDYMARK_KRAKEN_INSERT : {
                "profile" : ANDYMARK_SMALL_INSERT_PROFILE,
                "depth" : 0.22 * inch,
                "bore" : SPLINE_XS_BORE
            },
        PrintAdapter.SWYFT_HEX_ADAPTER : {
                "profile" : SWYFT_HEX_ADAPTER_PROFILE,
                "depth" : 0.25 * inch,
                "bore" : { "hexSize" : 0.5 * inch, "vertexAngle" : 90 * degree }
            },
        PrintAdapter.TTB_HEX_INSERT : {
                "profile" : TTB_HEX_INSERT_PROFILE,
                "depth" : 0.25 * inch,
                "bore" : { "hexSize" : 0.5 * inch, "vertexAngle" : 0 * degree }
            },
        // The same shape as WCP's SplineXS adapter
        PrintAdapter.TTB_SPLINE_INSERT : {
                "profile" : WCP_SPLINE_ADAPTER_PROFILE,
                "depth" : 0.25 * inch,
                "bore" : SPLINE_XS_BORE
            },
        PrintAdapter.WCP_SPLINE_ADAPTER : {
                "profile" : WCP_SPLINE_ADAPTER_PROFILE,
                "depth" : 0.25 * inch,
                "bore" : SPLINE_XS_BORE
            },
        PrintAdapter.WCP_HEX_ADAPTER : {
                "profile" : WCP_HEX_ADAPTER_PROFILE,
                "depth" : 0.25 * inch,
                "bore" : { "hexSize" : 0.5 * inch, "vertexAngle" : 0 * degree }
            }
    };
