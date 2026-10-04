FeatureScript 2909;
// git commit 'Add FeatureScript language support extension'

import(path : "onshape/std/common.fs", version : "2909.0");

export type Person typecheck canBePerson;

export predicate canBePerson(value)
{
    value is map;
    value.age is number;
}

predicate canBePositiveLength(value)
{
    value is ValueWithUnits;
}

