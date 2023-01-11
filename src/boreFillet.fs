FeatureScript 1930;
import(path : "onshape/std/common.fs", version : "1930.0");

annotation { "Feature Type Name" : "Pulley bore fillet" }
export const pulleyBoreFillet = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        // Define the parameters of the feature type
    }
    {
        opFillet(context, id + "fillet", {
                "entities" : qEverything(EntityType.EDGE)->qConstructionFilter(ConstructionObject.NO)->qParallelEdges(Z_DIRECTION),
                "radius" : 0.1 * millimeter
        });
    });
