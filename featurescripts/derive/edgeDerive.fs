FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "derive/opPointTransform.fs", version : "");

const POINT_MANIPULATOR = "edgePointManipulator";
const FLIP_MANIPULATOR = "edgeFlipManipulator";
const SECONDARY_AXIS_MANIPULATOR = "edgeSecondaryAxisManipulator";

/**
 * The manipulators added for each edge (with the edge's index appended to the key), and the field
 * of the edge's parameters each one sets.
 */
const MANIPULATOR_FIELDS = {
        (POINT_MANIPULATOR) : "index",
        (FLIP_MANIPULATOR) : "flipPrimaryAxis",
        (SECONDARY_AXIS_MANIPULATOR) : "secondaryAxisIndex"
    };

/** The secondary axes the secondary axis manipulator chooses between, by index. */
const SECONDARY_AXIS_TYPES = [
        MateConnectorAxisType.PLUS_X,
        MateConnectorAxisType.PLUS_Y,
        MateConnectorAxisType.MINUS_X,
        MateConnectorAxisType.MINUS_Y
    ];

const DEFAULT_EDGE_PARAMETERS = { "index" : 0, "flipPrimaryAxis" : false, "secondaryAxisIndex" : 0 };

/**
 * A feature which derives a Part Studio onto each of several edges (see derive/opPointTransform.fs).
 * The Part Studio is regenerated for each edge with its `length` configuration input set to the edge's length.
 *
 * @param definition {{
 *          @field partStudioData {PartStudioData} :
 *                  The entities to derive. Should have a configuration input named `length`.
 *          @field edges {Query} :
 *                  The edges to derive entities onto.
 *          @field parameters {array} : @optional
 *                  For each of `edgeQuery`, a map of its `index`, `flipPrimaryAxis`, and `secondaryAxisIndex`,
 *                  set by manipulators. @seealso [updateEdgeParameters]
 *          @field edgeQuery {Query} : @optional
 *                  The edges `parameters` corresponds to. @seealso [updateEdgeParameters]
 *          @field transform {boolean} : @optional
 *                  Whether to move the result by `translationX`, `translationY`, and `translationZ`, and rotate it
 *                  by `rotation` about its Z axis. Defaults to `false`.
 *          @field oppositeDirection {boolean} : @optional
 *                  Whether to reverse `rotation`. Defaults to `false`.
 *          @field deletePlanesAndSketches {boolean} : @optional
 *                  Whether to leave out the Part Studio's planes and sketches. Defaults to `true`.
 * }}
 */
annotation { "Feature Type Name" : "Edge derive",
        "Manipulator Change Function" : "edgeDeriveManipulatorChange",
        "Editing Logic Function" : "edgeDeriveEditLogic",
        "Feature Type Description" : "Derive parts to specific edges in part studios.<br>" ~
        "For full documentation, visit: <br>" ~
        "alexkempen.github.io/alex-featurescript-docs<br>" ~
        "FeatureScript by Alex Kempen."
    }
export const edgeDerive = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Entities to import", "ComputedConfigurationInputs" : ["length"] }
        definition.partStudioData is PartStudioData;

        annotation { "Name" : "Edges", "Filter" : EntityType.EDGE, "UIHint" : ["UNCONFIGURABLE"] }
        definition.edges is Query;

        annotation { "Name" : "Parameters", "UIHint" : ["ALWAYS_HIDDEN"] }
        isAnything(definition.parameters);

        annotation { "Name" : "Edge to use", "Filter" : EntityType.EDGE, "UIHint" : ["ALWAYS_HIDDEN"] }
        definition.edgeQuery is Query;

        annotation { "Name" : "Move" }
        definition.transform is boolean;

        if (definition.transform)
        {
            annotation { "Name" : "X translation" }
            isLength(definition.translationX, ZERO_DEFAULT_LENGTH_BOUNDS);

            annotation { "Name" : "Y translation" }
            isLength(definition.translationY, ZERO_DEFAULT_LENGTH_BOUNDS);

            annotation { "Name" : "Z translation" }
            isLength(definition.translationZ, ZERO_DEFAULT_LENGTH_BOUNDS);

            annotation { "Name" : "Rotation angle" }
            isAngle(definition.rotation, ANGLE_360_ZERO_DEFAULT_BOUNDS);

            annotation { "Name" : "Opposite direction", "UIHint" : ["OPPOSITE_DIRECTION_CIRCULAR"] }
            definition.oppositeDirection is boolean;
        }

        annotation { "Name" : "Delete planes and sketches", "Default" : true, "UIHint" : ["REMEMBER_PREVIOUS_VALUE"] }
        definition.deletePlanesAndSketches is boolean;
    }
    {
        verifyNonemptyStudioReference(context, definition, "partStudioData", ErrorStringEnum.IMPORT_DERIVED_NO_PARTS);
        const edges = verifyNonemptyQuery(context, definition, "edges", "Select one or more edges to use.");
        const remainingTransform = getRemainderPatternTransform(context, { "references" : definition.edges });

        var partStudioData = definition.partStudioData;
        if (definition.deletePlanesAndSketches)
        {
            partStudioData = removePlanesAndSketches(partStudioData);
        }
        const parameters = edgeParameters(definition, size(edges));
        const rotation = definition.transform ? adjustAngle(context, definition.rotation) * (definition.oppositeDirection ? -1 : 1) : 0 * degree;

        var manipulatorRadius;
        for (var i, edge in edges)
        {
            const deriveId = id + "location" + unstableIdComponent(i);
            setExternalDisambiguation(context, deriveId, edge);

            var edgeStudio = partStudioData;
            edgeStudio.configuration = mergeMaps(edgeStudio.configuration ?? {}, { "length" : evLength(context, { "entities" : edge }) });
            const entities = instantiatePointDerivePartStudio(context, deriveId, edgeStudio);

            if (manipulatorRadius == undefined)
            {
                const box = evBox3d(context, { "topology" : entities, "tight" : false });
                manipulatorRadius = norm(project(XY_PLANE, box.minCorner) - project(XY_PLANE, box.maxCorner)) * 1.25;
            }

            const result = callSubfeatureAndProcessStatus(id, opPointPattern, context, deriveId, {
                        "entities" : entities,
                        "locations" : [edgeLocation(context, edge)],
                        "identities" : [edge],
                        "index" : parameters[i].index,
                        "flipPrimaryAxis" : parameters[i].flipPrimaryAxis,
                        "secondaryAxisType" : SECONDARY_AXIS_TYPES[parameters[i].secondaryAxisIndex],
                        "transform" : definition.transform,
                        "absoluteToWorld" : true,
                        "translationX" : definition.translationX,
                        "translationY" : definition.translationY,
                        "translationZ" : definition.translationZ,
                        "rotation" : rotation
                    }, { "propagateErrorDisplay" : true });

            addEdgeManipulators(context, id, i, result, parameters[i], manipulatorRadius);
        }

        const mateConnectors = qCreatedBy(id, EntityType.BODY)->qBodyType(BodyType.MATE_CONNECTOR);
        if (!isQueryEmpty(context, mateConnectors))
        {
            opDeleteBodies(context, id + "deleteMateConnectors", { "entities" : mateConnectors });
        }

        transformResultIfNecessary(context, id, remainingTransform);
    },
    {
            "parameters" : [],
            "edgeQuery" : qNothing(),
            "transform" : false,
            "oppositeDirection" : false,
            "deletePlanesAndSketches" : true
        });

/**
 * The parameters of the first `count` edges, filling in defaults for edges which don't have any yet.
 */
function edgeParameters(definition is map, count is number) returns array
{
    const parameters = definition.parameters is array ? definition.parameters : [];
    var result = [];
    for (var i = 0; i < count; i += 1)
    {
        result = append(result, i < size(parameters) ? parameters[i] : DEFAULT_EDGE_PARAMETERS);
    }
    return result;
}

/**
 * Where entities are derived to on an edge: its midpoint, with Z along the edge and X along its sketch's normal
 * (or, for edges not in a sketch, an arbitrary perpendicular direction).
 */
function edgeLocation(context is Context, edge is Query) returns CoordSystem
{
    const line = evEdgeTangentLine(context, { "edge" : edge, "parameter" : 0.5 });
    const xAxis = isQueryEmpty(context, edge->qSketchFilter(SketchObject.YES)) ?
        perpendicularVector(line.direction) :
        evOwnerSketchPlane(context, { "entity" : edge }).normal;
    return coordSystem(line.origin, xAxis, line.direction);
}

/**
 * Adds an edge's manipulators: choosing the mate connector to use, flipping, and choosing the secondary axis
 * (shown around a circle with arrows, to suggest the rotation it controls).
 */
function addEdgeManipulators(context is Context, id is Id, i is number, result is map, parameters is map, radius is ValueWithUnits)
{
    const location = result.firstLocation;
    if (result.points != [])
    {
        addManipulators(context, id, {
                    (POINT_MANIPULATOR ~ i) : pointsManipulator({ "points" : result.points, "index" : result.index })
                });
    }
    addManipulators(context, id, {
                (FLIP_MANIPULATOR ~ i) : flipManipulator({
                            "base" : location.origin,
                            "direction" : location.zAxis,
                            "flipped" : parameters.flipPrimaryAxis
                        }),
                (SECONDARY_AXIS_MANIPULATOR ~ i) : pointsManipulator({
                            "points" : mapArray([location.xAxis, yAxis(location), -location.xAxis, -yAxis(location)], function(direction)
                                {
                                    return location.origin + direction * radius;
                                }),
                            "index" : parameters.secondaryAxisIndex
                        })
            });
    addRotationCircle(context, id + ("rotationCircle" ~ i), location, radius);
}

/**
 * Shows a blue circle with an arrowhead in each quadrant around `location`.
 */
function addRotationCircle(context is Context, id is Id, location is CoordSystem, radius is ValueWithUnits)
{
    const sketch = newSketchOnPlane(context, id + "rotationSketch", { "sketchPlane" : plane(location) });
    skCircle(sketch, "circle", { "center" : zeroVector(2) * meter, "radius" : radius });

    const arrowLength = radius * 0.15;
    for (var i, quadrant in [[1, 1], [-1, 1], [-1, -1], [1, -1]])
    {
        // The arrows point counterclockwise
        const point = vector(quadrant) * radius * sqrt(2) / 2;
        skLineSegment(sketch, "line" ~ i, { "start" : point + vector(0 * meter, quadrant[0] * arrowLength), "end" : point });
        skLineSegment(sketch, "secondLine" ~ i, { "start" : point + vector(-quadrant[1] * arrowLength, 0 * meter), "end" : point });
    }
    skSolve(sketch);

    addDebugEntities(context, qCreatedBy(id + "rotationSketch", EntityType.EDGE), DebugColor.BLUE);
    opDeleteBodies(context, id + "deleteSketch", { "entities" : qCreatedBy(id + "rotationSketch", EntityType.BODY) });
}

/**
 * @internal
 * The manipulator change function for edge derive.
 */
export function edgeDeriveManipulatorChange(context is Context, definition is map, newManipulators is map) returns map
{
    for (var key, manipulator in newManipulators)
    {
        for (var prefix, field in MANIPULATOR_FIELDS)
        {
            const parsed = match(key, prefix ~ "(\\d+)");
            if (!parsed.hasMatch)
            {
                continue;
            }
            const i = stringToNumber(parsed.captures[1]);
            definition.parameters = edgeParameters(definition, max(i + 1, size(definition.parameters ?? [])));
            definition.parameters[i][field] = field == "flipPrimaryAxis" ? manipulator.flipped : manipulator.index;
        }
    }
    return definition;
}

/**
 * @internal
 * The editing logic function for edge derive.
 */
export function edgeDeriveEditLogic(context is Context, id is Id, oldDefinition is map, definition is map, isCreating is boolean) returns map
{
    if (oldDefinition.edges != definition.edges)
    {
        definition = updateEdgeParameters(context, definition);
    }
    return definition;
}

/**
 * Keeps each edge's parameters with it when edges are added, removed, or reordered, by matching `edges` against
 * `edgeQuery` (the edges `parameters` was last updated for). Based on loft.fs in the Onshape standard library.
 */
function updateEdgeParameters(context is Context, definition is map) returns map
{
    const oldEdges = evaluateQuery(context, definition.edgeQuery ?? qNothing());
    const parameters = edgeParameters(definition, size(oldEdges));
    var oldParameters = {};
    for (var i, edge in oldEdges)
    {
        oldParameters[edge] = parameters[i];
    }

    const edges = evaluateQuery(context, definition.edges);
    definition.parameters = mapArray(edges, function(edge)
        {
            return oldParameters[edge] ?? DEFAULT_EDGE_PARAMETERS;
        });
    definition.edgeQuery = qUnion(edges);
    return definition;
}
