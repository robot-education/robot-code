FeatureScript 1948;
import(path : "onshape/std/common.fs", version : "1948.0");

import(path : "472bc4c291e1d2d6f9b98937", version : "515ac497eaa4f72b8c5f1a57");
import(path : "9cb13882ac97598c2be31cc1", version : "2650327f9f114cdb36c8acd2");

/**
 * A type defining a belt.
 * @type {{
 *      @field beltPlane {Plane} : A plane defining the position of the belt.
 *              Its `x` axis should be arbitrary in order to be consistent with other belts.
 * }}
 */
export type BeltDefinition typecheck canBeBeltDefinition;

export predicate canBeBeltDefinition(value)
{
    value.beltPlane is Plane;
    value.pulleyDefinitions is array;
    for (var pulleyDefinition in value.pulleyDefinitions)
    {
        pulleyDefinition is PulleyDefinition;
    }

    value.pitch is ValueWithUnits;
    value.width is ValueWithUnits;
}

export type PulleyDefinition typecheck canBePulleyDefinition;

export predicate canBePulleyDefinition(value)
{
    is2dPoint(value.location);
    value.radius is ValueWithUnits;
    value.flipped is boolean;
    value.identity is Query || value.identity is undefined;
}

/**
 * @param id : @autocomplete `id + "belt"`
 * @param definition {{
 *      @field beltDefinition {BeltDefinition} :
 *      @field createBeltTeeth {boolean} :
 * }}
 *
 * @returns {{
 *      @field beltLength {ValueWithUnits} :
 *              The length of the belt.
 * }}
 */
export const opBelt = function(context is Context, id is Id, definition is map)
    precondition
    {
        definition.beltDefinition is BeltDefinition;
        definition.createBeltTeeth is boolean;
    }
    {
        const sketchId = id + "sketch";
        sketchBelt(context, sketchId, definition.beltDefinition);
        createBelt(context, id, qCreatedBy(sketchId, EntityType.EDGE)->qBodyType(BodyType.WIRE), definition.beltDefinition, definition.createBeltTeeth);

        cleanup(context, id + "delete", qCreatedBy(id)->qSubtraction(qCreatedBy(id)->qBodyType(BodyType.SOLID)));
    };

function beltThickness(beltDefinition is BeltDefinition)
{
    return beltDefinition.pitch / 3;
}

function beltToothRadius(beltDefinition is BeltDefinition)
{
    return beltDefinition.pitch / 4;
}

function sketchBelt(context is Context, id is Id, beltDefinition is BeltDefinition)
{
    const pulleyDefinitions = adjustPulleyDefinitions(beltDefinition);
    println(pulleyDefinitions);
    const locations = extractFromArrayOfMaps(pulleyDefinitions, "location");
    const centroid = computeCentroid(locations);
    const counterClockwise = isCounterClockwise(locations, centroid);
    const connectingPointsArray = getPulleyconnectingPointsArray(pulleyDefinitions, counterClockwise);
    // robustness requirements are still high, even for a belt
    sketchArcs(context, id + "arcs", pulleyDefinitions, beltDefinition.beltPlane, connectingPointsArray, counterClockwise);
    sketchConnectingLines(context, id + "auto", extractFromArrayOfMaps(pulleyDefinitions, "identity"), beltDefinition.beltPlane, connectingPointsArray);
}

/**
 * @return {array} : An array of `Vector`s representing the midpoints of each arc.
 */
function sketchArcs(context is Context, id is Id, pulleyDefinitions is array, platePlane is Plane, connectingPointsArray is array, counterClockwise is boolean) returns array
{
    var midPoints = [];
    for (var i, curr in pulleyDefinitions)
    {
        const nextIndex = getNext(size(pulleyDefinitions), i);
        const next = pulleyDefinitions[nextIndex];

        if (curr.identity != undefined)
        {
            setExternalDisambiguation(context, id + unstableIdComponent(i), curr.identity);
        }
        const autoSketch = newSketchOnPlane(context, id + unstableIdComponent(i), { "sketchPlane" : platePlane });
        const point = addArc(autoSketch, "arc",
            concatenateArrays([connectingPointsArray[i], connectingPointsArray[nextIndex]]), curr.location, curr.radius, counterClockwise);
        midPoints = append(midPoints, point);
        skSolve(autoSketch);
    }
    return midPoints;
}

function adjustPulleyDefinitions(beltDefinition is BeltDefinition) returns array
{
    return mapArray(beltDefinition.pulleyDefinitions, function(pulley)
        {
            pulley.radius + (pulley.flipped ? beltThickness(beltDefinition) : 0 * meter);
            return pulley;
        });
}

function getPulleyconnectingPointsArray(pulleyDefinitions is array, counterClockwise is boolean) returns array
{
    var connectingPointsArray = [];
    for (var i, curr in pulleyDefinitions)
    {
        const next = getNext(pulleyDefinitions, i);
        connectingPointsArray = append(connectingPointsArray, connectPulleys(curr, next, counterClockwise));
    }
    return connectingPointsArray;
}

/**
 * Returns the endpoints of a line connecting two pulleys.
 */
export function connectPulleys(pulley1 is PulleyDefinition, pulley2 is PulleyDefinition, counterClockwise is boolean) returns array
{
    const radius1 = pulley1.radius * (pulley1.flipped ? 1 : -1) * (counterClockwise ? -1 : 1);
    const radius2 = pulley2.radius * (pulley2.flipped ? 1 : -1) * (counterClockwise ? -1 : 1);
    const delta = normalize(pulley2.location - pulley1.location);
    const cross = vector(-delta[1], delta[0]);
    const alpha = (radius1 - radius2) / norm(pulley2.location - pulley1.location);
    const beta = sqrt(1 - alpha * alpha);

    const point1 = pulley1.location + (alpha * delta + beta * cross) * radius1;
    const point2 = pulley2.location + (alpha * delta + beta * cross) * radius2;
    return [point2, point1];
}

function createBelt(context is Context, id is Id, sketchEdges is Query, beltDefinition is BeltDefinition, createBeltTeeth is boolean)
{
    // create a surface of the belt profile
    opExtrude(context, id + "beltSurface", {
                "entities" : sketchEdges,
                "direction" : beltDefinition.beltPlane.normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : beltDefinition.width / 2,
                "startBound" : BoundingType.BLIND,
                "startDepth" : beltDefinition.width / 2,
            });

    // create the outer belt profile by thickening the surface of the belt profile
    opThicken(context, id + "belt", {
                "entities" : qBodyType(qCreatedBy(id + "beltSurface", EntityType.BODY), BodyType.SHEET),
                "thickness1" : beltThickness(beltDefinition),
                "thickness2" : 0 * millimeter
            });

    // ---create belt teeth---
    if (createBeltTeeth)
    {
        const startPoint = evEdgeTangentLine(context, {
                        "edge" : sketchEdges->qNthElement(0),
                        "parameter" : 0.5
                    }).origin;
        const widthVector = beltDefinition.beltPlane.normal * beltDefinition.width / 2;
        fCylinder(context, id + "tooth", {
                    "topCenter" : startPoint + widthVector,
                    "bottomCenter" : startPoint - widthVector,
                    "radius" : beltToothRadius(beltDefinition)
                });

        // pattern the extrusion of the tooth circle
        const patternDefinition = getClosedPathPatternDefinition(
            context,
            sketchEdges->qBodyType(BodyType.WIRE),
            qCreatedBy(id + "tooth", EntityType.BODY),
            beltDefinition.teeth);
        opPattern(context, id + "toothPattern", patternDefinition);

        opBoolean(context, id + "toothBoolean", {
                    "tools" : qUnion([qCreatedBy(id + "belt", EntityType.BODY),
                            qCreatedBy(id + "toothPattern", EntityType.BODY),
                            qCreatedBy(id + "tooth", EntityType.BODY)]),
                    "operationType" : BooleanOperationType.UNION
                });
    }
    setBeltProperties(context, beltDefinition, qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.SOLID));
}

function setBeltProperties(context is Context, beltDefinition is BeltDefinition, beltQuery is Query)
{
    // set belt properties
    setProperty(context, {
                "entities" : beltQuery,
                "propertyType" : PropertyType.MATERIAL,
                "value" : material("Viton Rubber", 1827 * kilogram / meter ^ 3) // default belt material and density
            });
    setProperty(context, {
                "entities" : beltQuery,
                "propertyType" : PropertyType.APPEARANCE,
                "value" : color(0.53, 0.53, 0.53) // default belt color (RGB value, x/255)
            });
    setProperty(context, {
                "entities" : beltQuery,
                "propertyType" : PropertyType.NAME,
                "value" : beltDefinition.teeth ~ "T " ~ beltDefinition.beltType ~ " Belt"
            });
}

// // Adapted from the Onshape STD
function getClosedPathPatternDefinition(context is Context, edges is Query, entities is Query, instanceCount is number) returns map
{
    const path = constructPath(context, edges, {}).path;
    var transforms = [];
    var names = [];

    if (instanceCount > 1)
    {
        // If the path is open, the parameters are {0.0, 1 / (instanceCount - 1), ..., (instanceCount - 2) / (instanceCount - 1), 1.0}
        // If the path is closed, the parameters are {0.0, 1 / (instanceCount), ..., (instanceCount - 2) / (instanceCount), (instanceCount - 1) / (instanceCount)}
        var parameters = [];
        for (var i = 0; i < instanceCount; i += 1)
        {
            parameters = append(parameters, i / instanceCount);
        }

        // Get tangent planes or lines from computePatternTangents
        // var tangents = computePatternTangents(context, path, parameters, referenceEntities);
        var tangents = mapArray(evPathTangentLines(context, path, parameters, entities).tangentLines, function(tangent is Line)
        {
            return tangent.origin;
        });

        // transform(..., ...) works with planes or lines
        // transform 0 is left out since original tool is kept
        transforms = [transform(tangents[1] - tangents[0])];
        names = ["1"];
        for (var i = 2; i < instanceCount; i += 1)
        {
            // transforms = append(transforms, transform(tangents[i - 1] - tangents[i]) * transforms[i - 2]);
            transforms = append(transforms, transform(tangents[i] - tangents[0]));
            names = append(names, "" ~ i);
        }
    }
    return { "transforms" : transforms, "instanceNames" : names, "entities" : entities };
}
