FeatureScript 2960;
/**
 * Manipulators for features which place something on each of several edges, such as edge derive and robot nut strip.
 * Each edge gets a points manipulator (e.g. choosing a mate connector or a nine point position), a flip manipulator,
 * and a manipulator choosing one of four secondary axes, i.e. rotating in 90 degree increments.
 *
 * Each edge's choices are stored in the hidden `parameters` array, which `edgeManipulatorsEditLogic` keeps with its
 * edge as edges are added, removed, or reordered. To use:
 * - Name the edges parameter `edges`, and add `edgeManipulatorsPredicate` after it.
 * - Call `edgeManipulatorsChange` from the manipulator change function and `edgeManipulatorsEditLogic` from the
 *   editing logic function.
 * - For each edge, read its choices with `edgeParameters`, and call `addEdgeManipulators`.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "onshape/std/mateconnectoraxistype.gen.fs", version : "2960.0");

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

/** An edge's choices before its manipulators are used. `index` is left out so features can choose its default. */
const DEFAULT_EDGE_PARAMETERS = { "flipPrimaryAxis" : false, "secondaryAxisIndex" : 0 };

/**
 * The hidden parameters the manipulators set. @seealso [edgeParameters]
 */
export predicate edgeManipulatorsPredicate(definition is map)
{
    annotation { "Name" : "Parameters", "UIHint" : ["ALWAYS_HIDDEN"] }
    isAnything(definition.parameters);

    annotation { "Name" : "Edge to use", "Filter" : EntityType.EDGE, "UIHint" : ["ALWAYS_HIDDEN"] }
    definition.edgeQuery is Query;
}

/**
 * The choices for the first `count` edges, filling in defaults for edges which don't have any yet: maps of
 * `flipPrimaryAxis`, `secondaryAxisIndex`, and `index` (the points manipulator's, which may be `undefined`).
 */
export function edgeParameters(definition is map, count is number) returns array
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
 * The secondary axis type an edge's choices select.
 */
export function secondaryAxisType(parameters is map) returns MateConnectorAxisType
{
    return SECONDARY_AXIS_TYPES[parameters.secondaryAxisIndex];
}

/**
 * The coordinate system an edge's choices select, from `location` (see `edgeCoordSystem`): its Z axis flipped if
 * chosen, and its X axis the chosen secondary axis.
 */
export function applyEdgeParameters(location is CoordSystem, parameters is map) returns CoordSystem
{
    const xAxis = [location.xAxis, yAxis(location), -location.xAxis, -yAxis(location)][parameters.secondaryAxisIndex];
    return coordSystem(location.origin, xAxis, parameters.flipPrimaryAxis ? -location.zAxis : location.zAxis);
}

/**
 * A coordinate system at the middle of a line edge, with Z along the edge and X along its sketch's normal (or, for edges
 * not in a sketch, an arbitrary perpendicular direction).
 */
export function edgeCoordSystem(context is Context, edge is Query) returns CoordSystem
{
    const line = evEdgeTangentLine(context, { "edge" : edge, "parameter" : 0.5 });
    const xAxis = isQueryEmpty(context, edge->qSketchFilter(SketchObject.YES)) ?
        perpendicularVector(line.direction) :
        evOwnerSketchPlane(context, { "entity" : edge }).normal;
    return coordSystem(line.origin, xAxis, line.direction);
}

/**
 * Adds edge `i`'s manipulators: choosing one of `points` (skipped if there are none), flipping, and choosing the
 * secondary axis (shown around a circle with arrows, to suggest the rotation it controls).
 *
 * @param location : Where to show the flip and secondary axis manipulators, before the edge's choices are applied.
 * @param index : The point currently chosen.
 */
export function addEdgeManipulators(context is Context, id is Id, i is number, location is CoordSystem, parameters is map,
    points is array, index is number, radius is ValueWithUnits)
{
    if (points != [])
    {
        addManipulators(context, id, {
                    (POINT_MANIPULATOR ~ i) : pointsManipulator({ "points" : points, "index" : index })
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
 * Applies the manipulators' changes to `definition`. Call from the feature's manipulator change function.
 */
export function edgeManipulatorsChange(definition is map, newManipulators is map) returns map
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
 * Keeps each edge's choices with it when edges are added, removed, or reordered. Call from the feature's editing logic
 * function.
 */
export function edgeManipulatorsEditLogic(context is Context, oldDefinition is map, definition is map) returns map
{
    if (oldDefinition.edges != definition.edges)
    {
        definition = updateEdgeParameters(context, definition);
    }
    return definition;
}

/**
 * Matches `edges` against `edgeQuery` (the edges `parameters` was last updated for). Based on loft.fs in the Onshape
 * standard library.
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
