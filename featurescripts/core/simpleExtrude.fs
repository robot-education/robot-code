FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "21762d39019c8b2289e2fbb8", version : "7f7ad6a5f1bbd76302f920d1");

/**
 * Parameters for configuring an extrude along a predefined direction.
 */
export predicate simpleExtrudePredicate(definition is map)
{
    annotation { "Name" : "End type" }
    definition.endBound is BoundingType;

    extrudeBoundsPredicate(definition);


}

/**
 * For a simple extrude, sets `definition` parameters so `extrude` works as expected.
 */
export function transformDefintionForSimpleExtrude(definition is map, entities is Query) returns map
{
    definition.endBound = definition.endBound as BoundingType;
    definition.entities = entities;
    return definition;
}
