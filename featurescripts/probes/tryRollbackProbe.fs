FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * Checks whether a `try` rolls back what was done in it when something in it throws: it makes a point in a `try`, then
 * throws, and reports in its `catch` whether the point is still there. See docs/featurescript-style.md's "What rolls back
 * when something throws".
 */
annotation { "Feature Type Name" : "Try rollback probe" }
export const tryRollbackProbe = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
    }
    {
        var rolledBack;
        try silent
        {
            opPoint(context, id + "point", { "point" : vector(0, 0, 0) * inch });
            throw "thrown after the point is made";
        }
        catch
        {
            rolledBack = isQueryEmpty(context, qCreatedBy(id + "point"));
        }
        reportFeatureInfo(context, id, rolledBack ? "The try was rolled back: the point it made is gone." :
                "The try wasn't rolled back: the point it made is still there.");
    });
