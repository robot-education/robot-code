FeatureScript 2960;
/**
 * Places entities derived from a Part Studio at locations, aligned by one of their mate connectors.
 *
 * Instantiate the entities with `instantiatePointDerivePartStudio`, then place them with
 * `opPointPattern` (a copy at each of several locations) or `opPointTransform` (one location).
 * Both return the mate connectors' positions, which a points manipulator (see
 * core/pointManipulator.fs) can use to choose which one is aligned.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "onshape/std/mateconnectoraxistype.gen.fs", version : "2960.0");
export import(path : "onshape/std/rotationtype.gen.fs", version : "2960.0");

/**
 * Throws a [regenError] and marks the specified Part Studio reference parameter as faulty if no entities have been selected.
 *
 * @param parameterName :
 *          The name of the part studio reference parameter to check.
 *          @autocomplete `"myPartStudio"`
 * @param errorToReport {string} :
 *          The error to report.
 *          @autocomplete `ErrorStringEnum.IMPORT_DERIVED_NO_PARTS`
 */
export function verifyNonemptyStudioReference(context is Context, definition is map, parameterName is string, errorToReport is string)
{
    if (definition[parameterName].buildFunction == undefined)
    {
        throw regenError(errorToReport, [parameterName]);
    }
}

/**
 * Strips construction planes and sketch geometry from `partStudioData`.
 */
export function removePlanesAndSketches(partStudioData is PartStudioData) returns PartStudioData
{
    partStudioData.partQuery = partStudioData.partQuery->qSubtraction(
        qUnion([
                    partStudioData.partQuery->qSketchFilter(SketchObject.YES),
                    // qGeometry(GeometryType.PLANE) filters out construction planes for some reason (bug?)
                    partStudioData.partQuery->qConstructionFilter(ConstructionObject.YES)->qBodyType(BodyType.SHEET)
                ]));
    return partStudioData;
}

/**
 * Instantiates the entities of a Part Studio, along with their mate connectors, for `opPointPattern` or `opPointTransform`.
 *
 * @returns {Query} : A `Query` for the instantiated entities.
 */
export function instantiatePointDerivePartStudio(context is Context, id is Id, partStudioData is PartStudioData) returns Query
{
    partStudioData.partQuery = qUnion([
                partStudioData.partQuery,
                qMateConnectorsOfParts(partStudioData.partQuery)
            ]);
    try
    {
        const instantiator = newInstantiator(id + "import");
        // The instantiator's transform can't be used since the mate connectors need to be found first
        const entities = addInstance(instantiator, partStudioData);
        instantiate(context, instantiator);
        return entities;
    }
    throw regenError("Failed to import any entities. Ensure the selected part studio is not empty.");
}

/**
 * Derives entities at one or more locations.
 *
 * @param id : @autocomplete `id + "pointDerive"`
 * @param definition {{
 *          @field entities {Query} :
 *                  A [Query] for entities to pattern.
 *          @field keepTools {boolean} : @optional
 *                  Whether to keep `entities` after patterning them. Defaults to `false`.
 *          @field locations {array} :
 *                  An array of [CoordSystem]s to derive entities to. A copy of the entities is placed at each.
 *          @field identities {array} : @optional
 *                  An array of queries the same size as `locations` to disambiguate each copy with.
 *                  @autocomplete `identities`
 *          @field index {number} :
 *                  The index of the mate connector to align with each location. It's clamped to the number of
 *                  mate connectors. @autocomplete `definition.index`
 *          @field flipPrimaryAxis {boolean} : @optional
 *                  Whether to flip the primary axis of derived parts. Defaults to `false`.
 *          @field secondaryAxisType {MateConnectorAxisType} : @optional
 *                  The secondary axis to use when orienting derived parts. Defaults to `MateConnectorAxisType.PLUS_X`.
 *          @field transform {boolean} : @optional
 *                  Whether to move the entities by `translationX`, `translationY`, and `translationZ`, and rotate
 *                  them by `rotation` about the axis given by `rotationType`. Defaults to `false`.
 *          @field absoluteToWorld {boolean} : @optional
 *                  Whether the translation is in world coordinates rather than each location's. Defaults to `false`.
 * }}
 *
 * @returns {{
 *      @field points {array} :
 *              The final position of each mate connector of the first copy, or `[]` if there are no mate connectors
 *              (and no points manipulator should be added).
 *      @field index {number} :
 *              The index of the mate connector used, after clamping.
 *      @field firstLocation {CoordSystem} :
 *              The first location, after any translation.
 * }}
 */
export const opPointPattern = function(context is Context, id is Id, definition is map) returns map
    precondition
    {
        definition.entities is Query;
        definition.keepTools is boolean || definition.keepTools is undefined;
        definition.index is number;
        definition.locations is array;
        for (var location in definition.locations)
        {
            location is CoordSystem;
        }
        definition.identities is array || definition.identities is undefined;
        if (definition.identities != undefined)
        {
            size(definition.identities) == size(definition.locations);
        }
        definition.flipPrimaryAxis is boolean || definition.flipPrimaryAxis is undefined;
        definition.secondaryAxisType is MateConnectorAxisType || definition.secondaryAxisType is undefined;
    }
    {
        definition = mergeMaps({ "keepTools" : false, "identities" : makeArray(size(definition.locations), qNothing()) }, definition);
        const placement = computePlacement(context, definition, definition.locations);
        for (var i, locationTransform in placement.transforms)
        {
            const patternId = id + "pattern" + unstableIdComponent(i);
            setExternalDisambiguation(context, patternId, definition.identities[i]);
            opPattern(context, patternId, {
                        "entities" : definition.entities,
                        "transforms" : [locationTransform],
                        "instanceNames" : ["pattern"]
                    });
        }
        if (!definition.keepTools)
        {
            opDeleteBodies(context, id + "deleteImport", { "entities" : definition.entities });
        }
        return { "points" : placement.points, "index" : placement.index, "firstLocation" : placement.locations[0] };
    };

/**
 * Moves entities to a location. Takes the same options as `opPointPattern`, but with a single `location`.
 *
 * @param id : @autocomplete `id + "pointTransform"`
 * @param definition {{
 *          @field entities {Query} :
 *                  A [Query] for entities to transform.
 *          @field location {CoordSystem} :
 *                  The [CoordSystem] to derive entities to.
 *          @field index {number} : @autocomplete `definition.index`
 * }}
 *
 * @returns {{
 *      @field points {array} :
 *              The final position of each mate connector, or `[]` if there are none.
 *      @field index {number} :
 *              The index of the mate connector used, after clamping.
 *      @field transform {Transform} :
 *              The transform applied to the entities.
 * }}
 */
export const opPointTransform = function(context is Context, id is Id, definition is map) returns map
    precondition
    {
        definition.entities is Query;
        definition.index is number;
        definition.location is CoordSystem;
        definition.flipPrimaryAxis is boolean || definition.flipPrimaryAxis is undefined;
        definition.secondaryAxisType is MateConnectorAxisType || definition.secondaryAxisType is undefined;
    }
    {
        const placement = computePlacement(context, definition, [definition.location]);
        opTransform(context, id, { "bodies" : definition.entities, "transform" : placement.transforms[0] });
        return { "points" : placement.points, "index" : placement.index, "transform" : placement.transforms[0] };
    };

/**
 * Computes the transform placing `definition.entities` at each location, aligned by the mate connector at
 * `definition.index`, and where each mate connector ends up for the first location.
 */
function computePlacement(context is Context, definition is map, locations is array) returns map
{
    definition = mergeMaps({
                "transform" : false,
                "absoluteToWorld" : false,
                "rotationType" : RotationType.ABOUT_Z,
                "rotation" : 0 * degree,
                "flipPrimaryAxis" : false,
                "secondaryAxisType" : MateConnectorAxisType.PLUS_X
            }, definition);

    var base = axisTransform(definition);
    if (definition.transform)
    {
        const translation = vector([definition.translationX, definition.translationY, definition.translationZ]);
        const rotation = tolerantEquals(definition.rotation, 0 * degree) ? identityTransform() :
            rotationAround(ROTATION_AXES[definition.rotationType], definition.rotation);
        if (definition.absoluteToWorld)
        {
            base = rotation * base;
            locations = mapArray(locations, function(location)
                {
                    location.origin += translation;
                    return location;
                });
        }
        else
        {
            base = transform(translation) * rotation * base;
        }
    }

    var index = definition.index;
    var points = [];
    var alignment = identityTransform();
    const mateConnectors = evaluateQuery(context, definition.entities->qMateConnectorsOfParts());
    if (mateConnectors != [])
    {
        const coordSystems = mapArray(mateConnectors, function(mateConnector)
            {
                return evMateConnector(context, { "mateConnector" : mateConnector });
            });
        index = clamp(index, 0, size(coordSystems) - 1);
        alignment = fromWorld(coordSystems[index]);
        points = mapArray(coordSystems, function(coordSystem)
            {
                return toWorld(locations[0]) * base * alignment * coordSystem.origin;
            });
    }

    return {
            "transforms" : mapArray(locations, function(location)
                {
                    return toWorld(location) * base * alignment;
                }),
            "points" : points,
            "index" : index,
            "locations" : locations
        };
}

const ROTATION_AXES = {
        RotationType.ABOUT_X : X_AXIS,
        RotationType.ABOUT_Y : Y_AXIS,
        RotationType.ABOUT_Z : Z_AXIS
    };

// Rotations of the x axis about the z axis, as in the std transform feature
const SECONDARY_AXES = {
        MateConnectorAxisType.PLUS_X : X_DIRECTION,
        MateConnectorAxisType.PLUS_Y : Y_DIRECTION,
        MateConnectorAxisType.MINUS_X : -X_DIRECTION,
        MateConnectorAxisType.MINUS_Y : -Y_DIRECTION
    };

/**
 * The transform applying `flipPrimaryAxis` and `secondaryAxisType`.
 */
function axisTransform(definition is map) returns Transform
{
    const zAxis = definition.flipPrimaryAxis ? -Z_DIRECTION : Z_DIRECTION;
    return toWorld(coordSystem(WORLD_ORIGIN, SECONDARY_AXES[definition.secondaryAxisType], zAxis));
}

/**
 * A constructor for `PartStudioData`. Can be used to convert a FeatureScript imported part studio into
 * `PartStudioData` which can be passed into custom features as the value of a Part Studio reference parameter.
 *
 * @eg ```
 * MyStudio::import(path : ..., version : ...); // top level namespace import
 * const partStudioData = partStudioData(MyStudio::build, { "length" : 0.5 * meter }); // function call
 * ```
 * @param buildFunction {function} : @autocomplete `MyStudio::build`
 *          The build function of an imported part studio.
 * @param configuration {map} : @optional
 *          The configuration to generate the part studio with.
 * @param partQuery {Query} : @optional
 *          A `Query` for entities in the imported part studio which should be imported.
 *          Defaults to `qEverything(EntityType.BODY)`.
 *          @ex `qEverything(EntityType.BODY)->qBodyType(BodyType.SOLID)` to import only solid parts
 */
export function partStudioData(buildFunction is function, configuration is map, partQuery is Query) returns PartStudioData
{
    return { "buildFunction" : buildFunction, "configuration" : configuration, "partQuery" : partQuery } as PartStudioData;
}

export function partStudioData(buildFunction is function, configuration is map) returns PartStudioData
{
    return partStudioData(buildFunction, configuration, qEverything(EntityType.BODY));
}

export function partStudioData(buildFunction is function, partQuery is Query) returns PartStudioData
{
    return partStudioData(buildFunction, {}, partQuery);
}

export function partStudioData(buildFunction is function) returns PartStudioData
{
    return partStudioData(buildFunction, {}, qEverything(EntityType.BODY));
}
