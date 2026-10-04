FeatureScript 2909;
// git commit 'Add FeatureScript language support extension'

import(path : "onshape/std/geometry.fs", version : "2909.0");

const unitDefaults = {
    (millimeter) : [0, 1, 100],
    (inch) : 0.04,
    unquoted : {
        nested : true
    },
    "quoted" : PI
} as LengthBoundSpec;

function choose(value is map) returns map
{
    value.width is number;
    if (value.enabled)
    {
        return {
            result : value.width,
            safeMember : value?.missing,
            safeIndex : value.options?[0]
        };
    }
    else
    {
        var b = new box(0);
        b[] = b[] + 1;
        const safeBox = b?[];
        return { result : safeBox };
    }
}
