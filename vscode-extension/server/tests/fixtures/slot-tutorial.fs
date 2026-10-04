FeatureScript 2909;
// git commit 'Add FeatureScript language support extension'

import(path : "onshape/std/geometry.fs", version : "2909.0");

annotation { "Feature Type Name" : "Slot Tutorial Fixture" }
export const slotTutorialFixture = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Slot path", "Filter" : EntityType.EDGE, "MaxNumberOfPicks" : 1 }
        definition.slotPath is Query;
        annotation { "Name" : "Width" }
        isLength(definition.width, LENGTH_BOUNDS);
    }
    {
        const sketchPlane = evOwnerSketchPlane(context, { "entity" : definition.slotPath });
        const created = qCreatedBy(id + "slot", EntityType.FACE);
        opExtrude(context, id + "slotExtrude", {
            "entities" : created,
            "endBound" : BoundingType.THROUGH_ALL
        });
    });

