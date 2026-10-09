FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "onshape/std/fillet.fs", version : "2960.0");

import(path : "6c65805103086c85362ee4b7", version : "c8ae72bd99ee1f581e10e759");

annotation {
        "Feature Type Name" : "Robot dogbone fillet",
        "Manipulator Change Function" : "robotFilletManipulatorChange",
        "Feature Type Description" : "Quickly cut dog ear corners into plates." ~ CREDIT,
        "Icon" : RobotIcon::BLOB_DATA
    }
export const robotFillet = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Entities to fillet", "Filter" :
                    ActiveSheetMetal.NO && EntityType.EDGE &&
                    GeometryType.LINE &&
                    EdgeTopology.TWO_SIDED && ConstructionObject.NO &&
                    SketchObject.NO && ModifiableEntityOnly.YES
                }
        definition.entities is Query;

        annotation { "Name" : "Measurement", "UIHint" : [UIHint.SHOW_LABEL, UIHint.REMEMBER_PREVIOUS_VALUE] }
        definition.blendControlType is BlendControlType;

        if (definition.blendControlType == BlendControlType.RADIUS)
        {
            annotation { "Name" : "Radius", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.radius, BLEND_BOUNDS);
        }
        else
        {
            annotation { "Name" : "Width", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            isLength(definition.width, BLEND_BOUNDS);
        }
        annotation { "Name" : "Secondary fillet", "Default" : true, "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
        definition.secondaryFillet is boolean;

        if (definition.secondaryFillet)
        {
            annotation { "Group Name" : "Secondary fillet", "Collapsed By Default" : false, "Driving Parameter" : "secondaryFillet" }
            {
                annotation { "Name" : "Custom radius", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "DISPLAY_SHORT"] }
                definition.customSecondaryRadius is boolean;

                if (definition.customSecondaryRadius)
                {
                    annotation { "Name" : "Secondary radius", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "DISPLAY_SHORT"] }
                    isLength(definition.secondaryRadius, BLEND_BOUNDS);
                }
            }
        }
    }
    {
        doDogBoneFillet(context, id, definition);
    });

function doDogBoneFillet(context is Context, id is Id, definition is map)
{
    if (definition.secondaryFillet && !definition.customSecondaryRadius)
    {
        definition.secondaryRadius = (definition.blendControlType == BlendControlType.RADIUS) ? definition.radius : definition.width;
    }
    addFilletManipulator(context, id, definition);
    opDogBoneFillet(context, id, definition);
}

/**
 * @param definition {{
 *      @field entities {Query} : Edges to fillet.
 *      @field blendControlType {BlendControlType} :
 *      @field radius {ValueWithUnits} :
 *      @field width {ValueWithUnits} :
 *      @field secondaryFillet {boolean} :
 *      @field secondaryRadius {ValueWithUnits} :
 * }}
 */
const opDogBoneFillet = function(context is Context, id is Id, definition is map)
    precondition
    {
        definition.entities is Query;
        definition.blendControlType is BlendControlType;
        if (definition.blendControlType == BlendControlType.RADIUS)
        {
            isLength(definition.radius);
        }
        else
        {
            isLength(definition.width);
        }
    }
    {
        forEachEntity(context, id, definition.entities, function(edge is Query, id is Id)
            {
                doOneDogBoneFillet(context, id, definition, edge);
            });
    };

function doOneDogBoneFillet(context is Context, id is Id, definition is map, edge is Query)
{
    const convexity = evEdgeConvexity(context, { "edge" : edge });
    if (convexity != EdgeConvexityType.CONCAVE)
    {
        throw regenError("Selected edges must be concave.", ["entities"], edge);
    }

    const origin = evEdgeTangentLine(context, { "edge" : edge, "parameter" : 0.5 }).origin;
    const normals = findSurfaceNormalsAtEdge(context, edge, origin);

    var cutterRadius;
    if (definition.blendControlType == BlendControlType.RADIUS)
    {
        cutterRadius = definition.radius;
    }
    else
    {
        const normals = findSurfaceNormalsAtEdge(context, edge, origin);
        const angle = angleBetween(normals[0], normals[1]);
        cutterRadius = definition.width / 2 / sin(angle);
    }

    const edgeTangents = evEdgeTangentLines(context, { "edge" : edge, "parameters" : [0, 1] });

    // Offset to the center of the circle
    const offset = normalize((normals[0] + normals[1]) / 2) * cutterRadius;

    fCylinder(context, id + "cylinder", {
                "topCenter" : edgeTangents[0].origin + offset,
                "bottomCenter" : edgeTangents[1].origin + offset,
                "radius" : cutterRadius
            });

    const ownerBody = qOwnerBody(edge);
    opBoolean(context, id + "boolean", {
                "tools" : qCreatedBy(id + "cylinder", EntityType.BODY),
                "targets" : ownerBody,
                "operationType" : BooleanOperationType.SUBTRACTION
            });

    if (definition.secondaryFillet)
    {
        opFillet(context, id + "secondaryFillet", {
                    "entities" : qCreatedBy(id + "boolean", EntityType.EDGE)->qParallelEdges(edgeTangents[0].direction),
                    "radius" : definition.secondaryRadius
                });
    }
}

function getManipulatorId(definition is map) returns string
{
    return definition.blendControlType == BlendControlType.RADIUS ? FILLET_RADIUS_MANIPULATOR : FILLET_WIDTH_MANIPULATOR;
}

const FILLET_RADIUS_MANIPULATOR = "filletRadiusManipulator";
const FILLET_WIDTH_MANIPULATOR = "filletWidthManipulator";

/*
 * Create a linear manipulator for the fillet
 */
function addFilletManipulator(context is Context, id is Id, definition is map)
{
    // get last last edge (or arbitrary edge of the last face) from the qlv
    const operativeEntity = try(definition.entities->qNthElement(0));
    if (operativeEntity != undefined)
    {
        // convert given radius and edge topology into origin, direction, and offset
        const origin = evEdgeTangentLine(context, { "edge" : operativeEntity, "parameter" : 0.5 }).origin;
        const normals = try(findSurfaceNormalsAtEdge(context, operativeEntity, origin));
        if (normals != undefined && !parallelVectors(normals[0], normals[1]))
        {
            const direction = normalize(normals[0] + normals[1]);

            var convexity = 1.0;
            const bounds = boundsRange(BLEND_BOUNDS);
            var minDragValue = bounds[0];
            var maxDragValue = bounds[1];
            if (isEdgeConvex(context, operativeEntity))
            {
                convexity = -1.0;
                const tempMin = minDragValue;
                minDragValue = -maxDragValue;
                maxDragValue = -tempMin;
            }

            var offset;
            if (definition.blendControlType == BlendControlType.RADIUS)
            {
                offset = convexity * definition.radius * findRadiusToOffsetRatio(normals);
            }
            else
            {
                offset = convexity * definition.width * findRadiusToOffsetRatio(normals) / (normals[0] - normals[1])->norm();
            }

            const primaryParameterId = definition.blendControlType == BlendControlType.RADIUS ? "radius" : "width";
            // The undo stack entry is dependent on the manipulator id, so alter it based on the quantity being edited.
            const manipulatorId = getManipulatorId(definition);
            addManipulators(context, id, {
                        (manipulatorId) : linearManipulator({
                                "base" : origin,
                                "direction" : direction,
                                "offset" : offset,
                                "minValue" : minDragValue,
                                "maxValue" : maxDragValue,
                                "primaryParameterId" : primaryParameterId
                            })
                    });
        }
    }
}

/*
 * Find surface normals at the point closest to edgePoint on the two faces attached to the given edge.
 * Returns undefined if the edge does not have two faces adjacent to it.
 */
function findSurfaceNormalsAtEdge(context is Context, edge is Query, edgePoint is Vector)
{
    const faces = evaluateQuery(context, qAdjacent(edge, AdjacencyType.EDGE, EntityType.FACE));
    if (size(faces) < 2)
        return undefined;

    var normals = makeArray(2);
    for (var i = 0; i < 2; i += 1)
    {
        const param = evDistance(context, { "side0" : faces[i], "side1" : edgePoint }).sides[0].parameter;
        const plane = evFaceTangentPlane(context, {
                    "face" : faces[i],
                    "parameter" : param
                });

        normals[i] = plane.normal;
    }
    return normals;
}

/**
 * @internal
 * Manipulator change function for `fillet`.
 */
export function robotFilletManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    try
    {
        const manipulatorId = getManipulatorId(definition);
        if (newManipulators[manipulatorId] is map)
        {
            // convert given offset and edge topology into new radius
            const edge = definition.entities->qNthElement(0);
            const origin = evEdgeTangentLine(context, { "edge" : edge, "parameter" : 0.5 }).origin;
            const normals = findSurfaceNormalsAtEdge(context, edge, origin);
            const convexity = isEdgeConvex(context, edge) ? -1.0 : 1.0;
            const radius = convexity * newManipulators[manipulatorId].offset / findRadiusToOffsetRatio(normals);
            if (definition.blendControlType == BlendControlType.RADIUS)
            {
                definition.radius = radius;
            }
            else
            {
                definition.width = radius * (normals[0] - normals[1])->norm();
            }
        }
    }

    return definition;
}

function isEdgeConvex(context is Context, edge is Query) returns boolean
{
    return evEdgeConvexity(context, { "edge" : edge }) == EdgeConvexityType.CONVEX;
}

/*
 * The distance from the center of a corner-inscribed circle to the corner itself is:
 * radius / cos(0.5 * angle between surface normals)
 * Therefore, the distance between the outer ege of the circle and the corner (the offset of the manipulator) is:
 * (radius / cos(0.5 * angle between surface normals)) - radius
 * So:
 * offset = radius * ((1.0 / cos(0.5 * angle between surface normals)) - 1.0)
 */
function findRadiusToOffsetRatio(normalArray is array) returns number
{
    return (1.0 / cos(0.5 * angleBetween(normalArray[0], normalArray[1]))) - 1.0;
}
