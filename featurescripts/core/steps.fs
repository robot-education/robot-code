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
 *              `errorId`, for what earlier steps used up (a failed step itself changes nothing, so what it was given
 *              can be shown as `entities`): like std's `reconstructOp` (see `processNewBodyIfNeeded`).
 *      @field diagnose {function} : @optional `function(diagnosisId)`, which pins down what failed, by trying the
 *              step again on parts of what it was given (see `failingItems`), under `diagnosisId`. It runs only after
 *              the step has failed, so it costs nothing when the feature works. Returns `undefined` if it finds
 *              nothing, or a map: `entities` (a query of what fails, shown in red instead of `failure.entities`) and
 *              `message` (said after `failure.message`, like `"The pockets shown fail."`).
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
    var message = failure.message;
    var diagnosis;
    if (failure.diagnose != undefined)
    {
        // A guard: the diagnosis only adds to the error, which is thrown whatever it finds
        try silent
        {
            diagnosis = failure.diagnose(id + "diagnosis");
        }
    }
    if (diagnosis != undefined && !isQueryEmpty(context, diagnosis.entities))
    {
        setErrorEntities(context, id, { "entities" : diagnosis.entities });
        failure.entities = undefined;
        message ~= " " ~ diagnosis.message;
    }
    showErrorEntities(context, id, failure);

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

/**
 * Which of `items` `operation(context, itemId, item)` fails for, run for each in turn, under an id of its own in `id`:
 * to diagnose a failed step (see `runStep`'s `failure.diagnose`), by trying it on its parts one at a time. Each should
 * be tried alone, on what the step was given: an operation which changes what it's given should work on a copy (see
 * `copyBodies`). Only for the cold path: it runs an operation per item.
 */
export function failingItems(context is Context, id is Id, items is array, operation is function) returns array
{
    var failing = [];
    for (var i, item in items)
    {
        try silent
        {
            operation(context, id + unstableIdComponent(i), item);
        }
        catch
        {
            failing = append(failing, item);
        }
    }
    return failing;
}

/**
 * Copies `bodies` (under `id`), and returns the copies: for trying an operation without changing what it's given.
 */
export function copyBodies(context is Context, id is Id, bodies is Query) returns Query
{
    opPattern(context, id, {
                "entities" : bodies,
                "transforms" : [identityTransform()],
                "instanceNames" : ["copy"]
            });
    return qCreatedBy(id, EntityType.BODY);
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
