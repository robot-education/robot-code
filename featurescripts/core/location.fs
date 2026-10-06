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

    if (isCircle(context, location))
    {
        const circle = evCurveDefinition(context, { "edge" : location });
        return plane(circle.coordSystem);
    }
    return evVertexCoordSystem(context, { "vertex" : location })->plane();
}
