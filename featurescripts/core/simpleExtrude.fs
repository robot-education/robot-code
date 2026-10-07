FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export import(path : "21762d39019c8b2289e2fbb8", version : "8f82cf693e7833130ba80201");

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