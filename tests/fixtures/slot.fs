FeatureScript 2909;
// git commit 'Add FeatureScript language support extension'

import(path : "onshape/std/geometry.fs", version : "2909.0");

annotation { "Feature Type Name" : "Slot" }
export const slot = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Slot path", "Filter" : EntityType.EDGE }
        definition.slotPath is Query;

        annotation { "Name" : "Width" }
        isLength(definition.width, LENGTH_BOUNDS);
    }
    {
        opExtrude(context, id + "extrude1", {
            "entities" : definition.slotPath,
            "endBound" : BoundingType.THROUGH_ALL
        });
    });

