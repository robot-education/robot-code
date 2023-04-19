FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");

export import(path : "onshape/std/fillet.fs", version : "2014.0");

import(path : "2a1fbdd680ed055fe57e372f", version : "b09d630978b71b04194fc4a8");

export enum RobotFilletType
{
    annotation { "Name" : "Plate" }
    PLATE,
    annotation { "Name" : "Dog bone" }
    DOG_BONE
}

annotation { "Feature Type Name" : "Robot fillet" }
export const robotFillet = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Robot fillet type", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "HORIZONTAL_ENUM"] }
        definition.robotFilletType is RobotFilletType;

        if (definition.robotFilletType == RobotFilletType.PLATE)
        {
            annotation { "Name" : "Plates to fillet", "Filter" :
                        EntityType.BODY && BodyType.SOLID &&
                        ModifiableEntityOnly.YES && ActiveSheetMetal.NO }
            definition.plates is Query;

            annotation { "Name" : "Edges to exclude", "Filter" : EntityType.EDGE && EdgeTopology.TWO_SIDED &&
                        ModifiableEntityOnly.YES && SketchObject.NO &&
                        ConstructionObject.NO &&
                        GeometryType.LINE }
            definition.edgesToExclude is Query;

            annotation { "Name" : "Radius" }
            isLength(definition.plateRadius, LENGTH_BOUNDS);
        }
        else
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

            annotation { "Name" : "Custom secondary radius", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
            definition.customSecondaryRadius is boolean;

            if (definition.customSecondaryRadius)
            {
                annotation { "Name" : "Secondary radius", "UIHint" : UIHint.REMEMBER_PREVIOUS_VALUE }
                isLength(definition.secondaryRadius, BLEND_BOUNDS);
            }
        }
    }
    {
        if (definition.robotFilletType == RobotFilletType.PLATE)
        {
            doPlateFillet(context, id, definition);
        }
        else
        {
            doDogBoneFillet(context, id, definition);
        }
    });

function doPlateFillet(context is Context, id is Id, definition is map)
{
    definition.plates = verifyNonemptyQuery(context, definition, "plates", "Select plates to fillet.");
    
    for (var i, plate in definition.plates)
    {
        if (!isPlate(context, plate))
        {
            throw regenError("Selected entity is not a valid plate.", ["plates"], plate);
        }

        const filletId = id + "fillet" + unstableIdComponent(i);
        setExternalDisambiguation(context, filletId, plate);

        const plateAttribute = getPlateAttribute(context, plate);
        const edges = qOwnedByBody(plate, EntityType.EDGE)->qParallelEdges(plateAttribute.platePlane.normal)->qSubtraction(definition.edgesToExclude);

        callSubfeatureAndProcessStatus(id, opFillet, context, filletId,
            { "entities" : edges, "radius" : definition.plateRadius });
    }
}

function doDogBoneFillet(context is Context, id is Id, definition is map)
{
    if (definition.secondaryFillet && !definition.customSecondaryRadius)
    {
        definition.secondaryRadius = (definition.blendControlType == BlendControlType.RADIUS) ? definition.radius : definition.width;
    }
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
        throw regenError("Selected edge must be concave.", ["entities"], edge);
    }

    const adjacentFaces = qAdjacent(edge, AdjacencyType.EDGE, EntityType.FACE);

    const nonPlanarFaces = adjacentFaces->qSubtraction(adjacentFaces->qGeometry(GeometryType.PLANE));
    if (!isQueryEmpty(context, nonPlanarFaces))
    {
        throw regenError("Selected edge is not adjacent to a planar face.", ["entities"], qUnion(nonPlanarFaces, edge));
    }

    const adjacentDirections = mapArray(evaluateQuery(context, adjacentFaces), function(face is Query) returns Vector
        {
            return evPlane(context, { "face" : face }).normal;
        });

    var cutterRadius;
    if (definition.blendControlType == BlendControlType.RADIUS)
    {
        cutterRadius = definition.radius;
    }
    else
    {
        const angle = angleBetween(adjacentDirections[0], adjacentDirections[1]);
        // const innerAngle = (180 * degree - angle) / 2;
        cutterRadius = definition.width / 2 / sin(angle);
    }

    const edgeTangents = evEdgeTangentLines(context, {
                "edge" : edge,
                "parameters" : [0, 1]
            });
    const offset = normalize((adjacentDirections[0] + adjacentDirections[1]) / 2) * cutterRadius;
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

// manipulators ahh

