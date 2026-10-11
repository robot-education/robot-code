FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "8b8c46128a5dbc2594925f4a", version : "6b7d5995c565ae73c7740b0b");

/**
 * @param name {string} : A string used for the name. Appended to the string `"Sketch point to place "`.
 */
export predicate locationPredicate(definition is map, name is string)
{
    annotation { "Name" : "Sketch point to place " ~ name,
                "Filter" : (EntityType.VERTEX && SketchObject.YES && ModifiableEntityOnly.YES) || GeometryType.CIRCLE || BodyType.MATE_CONNECTOR,
                "UIHint" : UIHint.INITIAL_FOCUS,
                "MaxNumberOfPicks" : 1
            }
    definition.location is Query;
}

export function verifyNonemptyLocation(context is Context, definition is map) returns Query
{
    return verifyNonemptyQuery(context, definition, "location", "Select a sketch point, circle, or mate connector to use.")[0];
}

/**
 * Verifies location is valid and returns the plane corresponding to the user's selection.
 */
export function getLocationPlane(context is Context, definition is map) returns Plane
{
    const location = verifyNonemptyLocation(context, definition);
    const result = selectionPlane(context, location);
    if (result == undefined)
    {
        throw regenError("Select a sketch point, circle, or mate connector to use.", ["location"], location);
    }
    return result;
}

/**
 * The plane of a sketch point, mate connector, circle, or cylindrical face, at its center (a cylinder's, in its
 * middle, see `cylinderMiddlePlane`), or `undefined` for anything else, for the caller's own error.
 */
export function selectionPlane(context is Context, selection is Query)
{
    if (isCircle(context, selection))
    {
        return evCurveDefinition(context, { "edge" : selection }).coordSystem->plane();
    }
    if (isVertex(context, selection) || isMateConnector(context, selection))
    {
        return evVertexCoordSystem(context, { "vertex" : selection })->plane();
    }
    const faces = selection->qEntityFilter(EntityType.FACE)->qGeometry(GeometryType.CYLINDER);
    if (!isQueryEmpty(context, faces))
    {
        return cylinderMiddlePlane(context, faces);
    }
    return undefined;
}

/**
 * The plane of a cylindrical face's axis, through its middle: its cylinder's origin is anywhere on its axis, so it's
 * moved to the face's centroid, along the axis (for a chain's or belt's face, its middle).
 */
export function cylinderMiddlePlane(context is Context, face is Query) returns Plane
{
    var result = evSurfaceDefinition(context, { "face" : face }).coordSystem->plane();
    const centroid = evApproximateCentroid(context, { "entities" : face });
    result.origin += result.normal * dot(centroid - result.origin, result.normal);
    return result;
}
