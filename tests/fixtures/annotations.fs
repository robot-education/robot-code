FeatureScript 2909;
// git commit 'Add FeatureScript language support extension'

import(path : "onshape/std/common.fs", version : "2909.0");

annotation { "Feature Type Name" : "Annotated Demo", "UIHint" : UIHint.NO_PREVIEW_PROVIDED }
export const annotatedDemo = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Group", "Collapsed By Default" : false }
        {
            annotation { "Name" : "Length", "Default" : 1 * inch }
            isLength(definition.length, LENGTH_BOUNDS);
        }
    }
    {
        const localMap = {
            label : "plain map key",
            escaped : "line\n\u0041",
            "quoted" : definition.length,
            nested : { child : true }
        };
    });
