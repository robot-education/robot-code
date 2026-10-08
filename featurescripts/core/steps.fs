FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * Steps of a feature run as subfeatures (std's features like `extrude` and `fillet`, or operations like `opShell`),
 * with their statuses shown as the feature's and, when one fails, an error of the feature's own which shows what failed
 * (see docs/featurescript-style.md's "Errors").
 */

/**
 * Runs `operation(context, subId, definition)` as a step of the feature `id` (its top level id), and returns what it
 * returns. Its warnings and info are shown as the feature's (but not its OK status, which would hide an earlier step's
 * warning). If it fails, its error display (the entities it highlights in red) is kept, and `stepError` throws an error
 * of the feature's own, from `failure`.
 *
 * @param failure {{
 *      @field message {string} : What failed, in the feature's terms, like `"Couldn't shell the parts."`.
 *      @field faultyParameters {array} : @optional The feature's parameters to highlight.
 *      @field featureParameterMap {map} : @optional The step's parameters which are the feature's, by name, to
 *              highlight those the step's error highlights.
 *      @field featureParameterMappingFunction {function} : @optional The same, as a function of the step's parameter's
 *              name (like `function(name) { return name; }`, for a step given the feature's own definition).
 *      @field entities {Query} : @optional What to show in red, which exists before the step (like its inputs).
 *      @field reconstruct {function} : @optional `function(errorId)`, which builds what to show in red under
 *              `errorId`, for what the feature made before the step, which is rolled back with the feature: like std's
 *              `reconstructOp` (see `processNewBodyIfNeeded`).
 * }}
 */
export function runStep(context is Context, id is Id, subId is Id, operation is function, definition is map, failure is map)
{
    var result;
    try
    {
        result = operation(context, subId, definition);
    }
    catch (error)
    {
        throw stepError(context, id, subId, failure, error);
    }
    if (featureHasNonTrivialStatus(context, subId))
    {
        processSubfeatureStatus(context, id, statusOptions(subId, failure));
    }
    return result;
}

/**
 * The error to throw when a step of the feature `id` (run under `subId`) failed, throwing `thrown`:
 * `failure.message` (see `runStep`), with the step's own message after it, when it's one std doesn't translate (a
 * custom message, like a feature's `regenError("...")`; std's own are `ErrorStringEnum`s, which are only translated in
 * Onshape's UI). The step's status and error display are shown as the feature's, then `failure`'s entities, and what
 * `failure.reconstruct` builds.
 */
export function stepError(context is Context, id is Id, subId is Id, failure is map, thrown) returns map
{
    processSubfeatureStatus(context, id, statusOptions(subId, failure));
    showErrorEntities(context, id, failure);

    var message = failure.message;
    // A step which isn't a feature or an operation (a function of the feature's own) reports no status of its own
    var stepMessage = getFeatureError(context, subId);
    if (stepMessage == undefined && thrown is map)
    {
        stepMessage = thrown.customMessage;
    }
    if (stepMessage is string && stepMessage != "")
    {
        message ~= " " ~ stepMessage;
    }
    return regenError(message, failure.faultyParameters ?? []);
}

/**
 * Shows `failure.entities`, and what `failure.reconstruct` builds, in red (see `runStep`), for an error the feature
 * `id` is about to throw. What's reconstructed is deleted again: the display stays after the feature's rolled back.
 */
export function showErrorEntities(context is Context, id is Id, failure is map)
{
    if (failure.entities != undefined)
    {
        setErrorEntities(context, id, { "entities" : failure.entities });
    }
    if (failure.reconstruct != undefined)
    {
        const errorId = id + "errorEntities";
        // A guard: what's reconstructed is only shown, so if it can't be built, the error's still thrown, without it
        try silent
        {
            failure.reconstruct(errorId);
            setErrorEntities(context, id, { "entities" : qCreatedBy(errorId, EntityType.BODY) });
        }
        const built = qCreatedBy(errorId, EntityType.BODY);
        if (!isQueryEmpty(context, built))
        {
            opDeleteBodies(context, errorId + "delete", { "entities" : built });
        }
    }
}

function statusOptions(subId is Id, failure is map) returns map
{
    return {
            "subfeatureId" : subId,
            "propagateErrorDisplay" : true,
            "featureParameterMap" : failure.featureParameterMap ?? {},
            "featureParameterMappingFunction" : failure.featureParameterMappingFunction
        };
}
