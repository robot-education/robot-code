FeatureScript 2909;
// git commit 'Add FeatureScript language support extension'

import(path : "onshape/std/geometry.fs", version : "2909.0");
foo::import(path : "1234567890abcdef", version : "abcdef1234567890");

export enum MyOption
{
    annotation { "Name" : "Option One" }
    ONE,
    annotation { "Name" : "Option Two" }
    TWO
}

export type Person typecheck canBePerson;

export predicate canBePerson(value)
{
    value is map;
    value.age is number;
}

function helper(context is Context, query is Query) returns Query
{
    return qCreatedBy(id + "made", EntityType.EDGE);
}

annotation { "Feature Type Name" : "Semantic Slot" }
export const slot = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Width", "Default" : 1 * inch }
        isLength(definition.width, LENGTH_BOUNDS);
        annotation { "Name" : "Mode", "Default" : MyOption.ONE }
        definition.mode is MyOption;
    }
    {
        const ownerPlane = evOwnerSketchPlane(context, { "entity" : definition.slotPath });
        opExtrude(context, id + "extrude1", {
            "entities" : definition.slotPath,
            "endBound" : BoundingType.THROUGH_ALL,
            unquotedKey : ownerPlane
        });
        definition.width = definition.width + meter * 0;
    });

