FeatureScript 2909;
// git commit 'Add FeatureScript language support extension'

import(path : "onshape/std/geometry.fs", version : "2909.0");

export operator*(x is Vector, y is number)
{
    return vector(x[0] * y, x[1] * y, x[2] * y);
}

const doubled = (val) => val * 2;
const zero = function() { return 0; };
var invalidCounter = 0;
invalidCounter++;
--invalidCounter;

