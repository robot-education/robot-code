FeatureScript 2909;
// git commit 'Add FeatureScript language support extension'

import(path : "onshape/std/common.fs", version : "2909.0");
export import(path : "onshape/std/geometry.fs", version : "2909.0");
foo::import(path : "1234567890abcdef", version : "abcdef1234567890");

const importedValue = foo::bar;

