# FeatureScript style

Conventions for the FeatureScripts in `featurescripts/`. `fs check` enforces some of them (see the end); the rest are
up to review.

## Formatting

Format like std, with `fs format` or the editor's Format Document (see the README): line breaks are yours, the
indentation of blocks and the spacing within lines aren't.

## Definitions are maps

A feature's `definition` is a map, and each parameter its precondition declares is a key in it: `definition.depth is
...` declares the key `"depth"`. Most of how features behave follows from that:

- The precondition isn't run like a function. Onshape reads it to learn the keys, their types, and their annotations,
  and to lay out the dialog, deciding its `if`s with the current values.
- Every parameter declared anywhere in the precondition (in any branch of an `if`, or in a predicate it calls) is a key
  with a value, its default until it's set, whether or not it's shown, and it keeps its value while it's hidden. A
  condition can read a parameter declared after it or in another branch.
- While a feature runs (its body, editing logic, and manipulator change functions), every parameter has a value of
  its type: don't guard parameters with `?? false`, `is boolean`, `isLength(...)`, and the like, even ones whose
  conditions say they aren't shown (`definition.tieLength` is a length whatever `definition.tieHolesBy` is). Where a
  value really can be missing (an optional field of a map, a lookup that can fail), check `!= undefined` rather than
  its type.
- Conditions can't read a lookup table's value. To show parameters by what's chosen in one, set a hidden
  (`ALWAYS_HIDDEN`) parameter from it in editing logic, and use that, as std's hole does with `threadStandard` and
  robotFrame does with `rectangularFrame`.
- So a parameter can't be declared twice, even in different branches of an `if`: both would be the same key. Declare
  it once where both branches can share it (as `stockLocationPredicate` does with the secondary axis), and `fs check`
  reports it if you don't.
- Groups (nested or not) and their driving parameters only lay the dialog out: `definition.x` is the same key whatever
  group it's in, and a driving parameter is an ordinary boolean key.
- An array parameter's value is an array of maps, one per item, each with its items' parameters as keys
  (`item.depth`). A lookup table's value (a `LookupTablePath`) maps each level's `"name"` to the option chosen there.
- Editing logic and manipulator change functions take this map and return it changed; setting a key sets that
  parameter. Subfeatures take a map too: calling std's `extrude` with our definition works because our features name
  their parameters as std's do (`endBound`, `depth`, `oppositeDirection`), and convert any of our own types first
  (see `transformDefintionForNewExtrude`).

`fs ui` (see README.md) follows the same model: settings are keys (`--set depth=2in`, `--set items.0.depth=2in`), and
every parameter has its default value before the dialog is laid out, so conditions see the values Onshape's would.

## UI state: predicates, not editing logic

When which parameters are shown depends on other parameters, write a predicate for each UI state and use it in the
precondition:

```
predicate isCustomHat(definition is map)
{
    definition.hatType == HatType.CUSTOM;
}

export const robotBearingHat = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        ...
        if (isCustomHat(definition))
        {
            ...
        }
    }
    {
        if (isCustomHat(definition))
        ...
```

Don't implement the "editing logic boomerang", where editing logic computes a value and stores it in an
`ALWAYS_HIDDEN` parameter for the precondition to read. Predicates are:

- **Less code**: a one-line predicate instead of a hidden parameter, its default for older features, and the
  editing logic that keeps it up to date.
- **Faster**: editing logic runs again on every change in the feature's dialog; a predicate is just evaluated.
- **Correct under configurations**: editing logic only runs when someone edits the feature in its dialog, so a
  derived value goes stale when a parameter changes any other way, such as through a configuration. A predicate is
  always computed from the current parameters.

The feature's body should use the same predicates, so the parameters it reads are exactly the ones shown.

A precondition's conditions can only use parameters, enum values, literals, and predicates, because Onshape works
out which parameters to show without running the feature. Constants and functions don't work (`fs check` warns about
them). When a UI state depends on data, like whether the chosen part has some property, generate the predicate from
that data with `fs gen` rather than reading a constant. For example, `released/printAdapter/printAdapterProfiles.py`
generates `printAdapterHasBoss` and `printAdapterHasSplineXsBore` from its list of adapters:

```
export predicate printAdapterHasBoss(definition is map)
{
    definition.adapterVendor == PrintAdapterVendor.ANDYMARK;
}
```

Editing logic is still the right tool for behavior that needs the part studio, such as picking a default merge scope
from the selections (`mountingEditLogic`) or filling in a plane from selected geometry.

## Sentence case

Parameter names, enum and lookup table options, group names, and messages are sentence case, as Onshape's are: "Vent
holes", "Rounded hex", "Stainless steel". Product and company names keep their own capitalization ("Hex Lite",
"UltraHex", "SplineXS", "The Thrifty Bot"), as do part names, which are titles ("3 in. Rounded Hex Shaft (WCP 1/2
in.)").

## Horizontal enums go at the top

As in std features, a horizontal enum (`"UIHint" : ["HORIZONTAL_ENUM"]`) belongs at the top of the feature: it may
only follow other horizontal enums, and never goes in a group (or an array parameter's items). Hidden parameters don't
count, and a predicate counts as the parameters it declares.

```
precondition
{
    annotation { "Name" : "Operation type", "UIHint" : ["HORIZONTAL_ENUM"] }
    definition.operationType is NewBodyOperationType;   // OK: first in the precondition

    annotation { "Name" : "Entities" }
    definition.entities is Query;

    annotation { "Name" : "End type", "UIHint" : ["HORIZONTAL_ENUM"] }
    definition.endBound is BoundingType;                 // Not OK: after a query parameter
}
```

A horizontal enum lower down would read as a set of tabs in the middle of the dialog. Use a normal enum (a dropdown)
there instead, with `"SHOW_LABEL"` if its options don't say what they choose.

A "top level option" means exactly this: a horizontal enum at the top of the feature, choosing between its modes (like
Robot lighten's Simple and Complex ribs). `fs check` warns about a horizontal enum after anything shown (hidden
parameters don't count), or in a group or an array parameter's items.

## Released feature constants

A feature's constant (`export const frcBeltCalculator = defineFeature(...)`) is its type: documents store it, and a
Part Studio can't find a feature whose constant changed. So never rename a released feature's constant, even when
the feature itself is renamed: change its `"Feature Type Name"` annotation (what users see) and its file (with
`fs mv`), and leave the constant. Robot belt is still `frcBeltCalculator`, and Robot tensioner `robotBeltTuner`. An
unreleased feature's constant can change freely.

## Enums stored in documents

Enum values used as parameter types are stored in documents, so never rename or remove them once released. Generated
enums (e.g. in `*.gen.fs` files) follow the same rule; their definitions say so where it matters.

## Buttons

A button is a parameter declared with std's `isButton` (`annotation { "Name" : "Clear invalid selections" }
isButton(definition.clearInvalid);`). Clicking it runs the feature's editing logic, which takes an eighth argument,
`clickedButton is string`: the button's parameter id (see std's `routingCurve.fs`). Don't fake one with a checkbox
which unchecks itself.

## Remember previous values

Give parameters `"UIHint" : ["REMEMBER_PREVIOUS_VALUE"]` (alongside any other hints), so a new feature starts with
the choices the user made last time: which part, its options, the placement, and so on. Leave it off only for
parameters which should logically start over in each new feature, such as an opposite direction (or another flip),
which only means something for the geometry it's used with.

## No field tolerancing

Don't let parameters be toleranced (`"UIHint" : ["CAN_BE_TOLERANT"]`, which adds a tolerance to a length or angle
field), and don't use std predicates which declare parameters that can be, like std's
`extrudeBoundParametersPredicate`: use the copies without it in `core/stdExtrude.fs` (`extrudeBoundsPredicate`,
`extrudeSecondBoundsPredicate`, and the `extrudePredicate` built from them). `fs check` warns about both
(`tolerant-parameter`), reading std's predicates from its copy of std's source.

## Negative values

Let lengths and angles be negative, as std does, unless a negative value truly makes no sense (a width, a diameter, a
wall). An offset or distance with an Opposite direction (or a flip manipulator) can be negative too: the flip reverses
it, and a negative value goes the other way again, as std's extrude offsets do. Manipulator change functions that set
one from a dragged manipulator set its absolute value and the flip, as std's do.

## Fits

Wherever one part goes in or over another (a bore on a shaft, a pocket for a part, a hole for a screw), use the fit
in `core/fit.fs` rather than a gap of your own or a profile offset: `fitPredicate` (or `boreFitPredicate` for a second
fit in the same feature) declares Fit (Free, Close, None, or Custom, with a Clearance; Free by default, since printed
parts rarely need a close fit), and the feature applies it:

- `fitClearance` (`boreFitClearance`) for shafts and parts: as much as std's close or free clearance hole
  (`ANSI_V2ClearanceHoleTable`, the Hole feature's) is bigger than the inch fastener nearest their size, across (add
  it to a hole's size, or offset a sketched profile's sides by half of it): 1/64 in. close and 1/32 in. free from
  7/16 in. up, less below. The size is the shaft's (a hex's width across flats, a spline's `splineDiameter`), or a
  sketched profile's widest (`profileAcross`).
- `fastenerHoleDiameter` for screws: std's close or free clearance hole for the screw's size (ANSI's Close or Free, or
  ISO's Close or Normal), which hole tables then pick only the size of.

A fit's direction is always plain from what's made (a hole grows, a part going into something shrinks), so fits have
no flip manipulator; a custom clearance is negative for an interference fit. Profile offsets (`core/profileOffset.fs`)
are left for what isn't a fit, like a pulley's teeth.

## Selections

A selection of where to place something takes one pick (`"MaxNumberOfPicks" : 1`), and is named for it, e.g. "Edge to
place nut strip" or "Sketch point to place nut strip"; to place more, add more features. Selections of what to act on,
like a merge scope or the faces to extrude, can take several.

## No defaults maps

Don't pass `defineFeature` its third argument, a map of defaults. It only applies when a feature is called from code,
which ours never are; in a dialog, parameters take their defaults from their annotations and bounds.

## Editing logic and manipulator change functions

Write these defensively where values can be missing: check with `!= undefined` before using them (a manipulator in
`newManipulators`, a field of `oldDefinition`), and fall back to a default rather than throwing. An error in one
surfaces when the user clicks a manipulator or edits a parameter, far from its cause. Parameters themselves are never
missing (see "Definitions are maps").

Editing logic's parameters are positional: `(context, id, oldDefinition, definition, isCreating, specifiedParameters,
hiddenBodies, clickedButton)`. Declare each, in that order, up to the last one used: a `clickedButton` declared sixth
gets `specifiedParameters` (a map), fails its `is string`, and the editing logic never runs (as Robot belt's didn't).

The first time editing logic runs (when the feature is created), `oldDefinition` is the empty map `{}`. Usually there's
nothing to do then, so return early:

```
if (oldDefinition == {})
{
    return definition;
}
```

Otherwise, every `oldDefinition.x` is `undefined`: comparing it is fine (`oldDefinition.edges != definition.edges` is
true then), but don't use it as a value, e.g. `oldDefinition.flag ?? false` instead of `oldDefinition.flag`.

When a shared function does everything a feature's manipulator change function would, name it in the feature's
annotation (`"Manipulator Change Function" : "stockManipulatorChange"`) rather than wrapping it.

## Tolerances, not margins

Don't pick margins by eye ("5% past", "at least 0.1 mm", "1.05 times the radius"). Whether two things are the same, or
touch, is std's call: `TOLERANCE.zeroLength` for geometry, `TOLERANCE.booleanDefaultTolerance` for what booleans treat
as touching (both unitless, in meters; see `docs/std-functions.md`). Make tools exactly as big as they need to be
(`evBox3d` with `"tight" : true`): a cut ending flush with a part's face cuts through, as booleans treat faces within
their tolerance as the same (Robot lighten's Through all relies on this; it's yet to be tried in Onshape). Where something can't be smaller than a tolerance (a hole left by thickening an arc
toward its center), compare against that tolerance.

## Comparing lengths and angles

Compare measured values with std's tolerant functions (`tolerantEquals`, `tolerantEqualsZero`, `tolerantLessThan`,
`tolerantLessThanOrEqual`, `tolerantGreaterThan`, ...) rather than `<`, `>`, and `==`, or adding
`TOLERANCE.zeroLength` by hand. This goes for editing logic too. Use `tolerantEqualsZero(x)` rather than
`tolerantEquals(x, 0 * meter)`.

Std's functions already allow for tolerance (`parallelVectors`, `perpendicularVectors`, the evaluation functions, and
operations), so don't add margins of your own, like treating nearly parallel vectors as parallel.

## No fallbacks or retries

FeatureScript is deterministic: an operation which fails will fail the same way every time it's run with the same
inputs, so retrying it never helps. Don't write fallbacks either (`try` one operation and do another if it fails):
- They're slow, since the failing operation runs (and fails) on every regeneration.
- They're fragile: which branch runs can change with a small edit upstream, and the branches make different
  geometry with different ids, so references to it break.

Pick the one operation which works and use it. Catching an error is fine to replace it with a clearer one (or to show
what failed before rethrowing), and a `try silent` guard is fine in editing logic, which mustn't throw while the dialog
is being filled in; but say so in a comment, and call out every `try` in the feature's writeup (see
`docs/feature-writeups.md`).

## Errors

A feature which fails should say what failed, in its own terms, and show it.

### Errors, warnings, and info

A feature (or subfeature) ends with one status: an error, a warning, or info (or none).

- **An error** rolls back the entire feature, except its debug entities (`addDebugEntities`) and error entities
  (`setErrorEntities`, or a `regenError`'s entities), and shows its message. A thrown `regenError` is an error:
  `defineFeature` catches it, reports it (its message, faulty parameters, and entities), and rolls the feature back
  (`@abortFeature`).
- **A warning or info** shows only if the feature completes (`reportFeatureWarning`, `reportFeatureInfo`), and can
  have debug and error entities too. Only the first status reported shows; later ones are ignored. Report each
  warning or info where it comes up, with its own parameters and entities, and let whichever comes first show. Never
  gather them to report at the end, or join their messages into one.

### Subfeatures and operations

Subfeatures are std's features (`extrude`, `fillet`) or operations (`opExtrude`, `opFillet`), each with an id of its
own under the feature's. They report errors, warnings, and info, on their own ids. Std's features have errors, and
most show what failed. Operations mostly don't: `opFillet` is the exception (it finds bad fillets, like overlapping
ones, and shows the fillets which failed in pink and the edges it was given in red), and most others show nothing. A
feature shows what failed itself (see "Failing, and carrying on").

- **Errors propagate up on their own**: an operation which fails throws, and uncaught, fails the feature with its
  error. So an operation needn't be wrapped in a `try` just to fail the feature: wrap it to say something more, or to
  carry on.
- **Everything else needs `processSubfeatureStatus`** to reach the feature: warnings, info, and error display (with
  `propagateErrorDisplay`). Its options also remap the subfeature's faulty parameters to the feature's
  (`featureParameterMap`, or `featureParameterMappingFunction`), so the right parameter is highlighted, and override
  its message with a more generic or context specific one (`overrideStatus`, an `ErrorStringEnum`). Its `id` must be
  the feature's top level id for the error display to come through. `callSubfeatureAndProcessStatus` runs a
  subfeature and does this.
- **Copy a status only when it isn't OK**: `processSubfeatureStatus` reports the subfeature's status even when it's
  OK, which counts as the feature's first. Copy one only in a `catch`, or after `featureHasNonTrivialStatus`.
- **Std's messages can't be quoted.** Std's errors are `ErrorStringEnum`s, translated only in Onshape's UI, so a
  feature can't put one in its own message. Custom messages (`regenError("...")`) are strings.

### Failing, and carrying on

- **Write a feature's steps plainly**: one operation after another, each with its own id, and a `try` around one
  only where its failure needs something said, or where the feature carries on without it. No helpers to wrap
  operations or their errors: the step, its `catch`, and what it reports should all be in view where it happens. A
  function doing several steps lists them, numbered, in its doc comment, and marks each in its body.
- **To add context to a failed operation**, in its `catch`, copy its status and error display onto the feature
  (`processSubfeatureStatus`, with `propagateErrorDisplay`, on the feature's top level id), then throw the feature's
  error: a short message saying which step failed, highlighting the parameters which set it, and showing what it was
  given (the `regenError`'s entities, or `setErrorEntities`). Most operations show nothing of their own, so without
  entities the error points nowhere. A message per step ("Failed to inset the pockets by the wall thickness.") says
  more than one for the whole feature ("Failed to create walls.").

  ```
  catch
  {
      processSubfeatureStatus(context, id, { "subfeatureId" : id + "shell", "propagateErrorDisplay" : true });
      throw regenError("Failed to inset the pockets by the wall thickness.", ["wallThickness", "filletRadius"], pockets);
  }
  ```

- **Show something that's still there.** A failed operation changes nothing, so what it was given can be shown. But
  an operation which worked may have used up what a later error wants to show: a boolean consumes its tools, and its
  targets become its result. Show the user's selections instead (they're always there), or rebuild what was used up
  under another id, show it, and delete it, as std's `processNewBodyIfNeeded` does with its `reconstructOp`.
- **Check for nothing left after each step which can leave nothing**: an enclose, a boolean, a filter. An empty
  result isn't an error to the operations after it, so without a check the feature carries on, does nothing, and
  ends with no result and no useful message. Throw at the step which emptied it, saying why, and showing what's left
  to show (Robot lighten checks after the regions to ignore, the walls, the ribs, and dropping small pockets).

- **When a step can fail without spoiling the rest, carry on with a warning**: in its `catch`, report the warning
  first, then copy the operation's status for its error display (its error, reported second, isn't shown), leave
  out what it would have done, and keep going. Robot lighten's pocket fillets do this: a pocket whose corners can't
  be filleted is left smaller, with "Failed to fillet pocket.", and `opFillet`'s display shows which fillets failed.
  Something that only makes the result a little off (a fillet's radius a hair big) is info.

  ```
  catch
  {
      reportFeatureWarning(context, id, "Failed to fillet pocket.", ["filletRadius"]);
      processSubfeatureStatus(context, id, { "subfeatureId" : filletId, "propagateErrorDisplay" : true });
  }
  ```

- **Keep messages short, and show where.** A message says what's wrong and what to change ("Selected wall overrides
  are invalid."); why belongs in the writeup. Show the culprit in red: the error's entities, and the operation's own
  display where it has one.
- **What rolls back when something throws**: a feature, all of it, when it fails. A `try` rolls nothing back: FsDoc's
  "Exceptions" (https://cad.onshape.com/FsDoc/exceptions.html) says a `try` block is aborted when something's thrown,
  and operations in it before the one which threw stay done. To try a sequence of operations and undo it, run it in a
  feature of its own: `startFeature`, then `endFeature` to keep it or `abortFeature` to roll it back (std's
  `sketch.fs`, `bridgingCurve.fs`, and `sheetMetalUtils.fs`), each try with an id of its own.
- **Queries are evaluated when they're used**, not when they're made: a query of faces a later step removes finds
  nothing after it. Track what you'll need through the steps which change it (`startTracking`, see Queries).
- **Don't run trial features in editing logic.** `abortFeature` with no feature started crashes the Part Studio, so
  editing logic should only evaluate (queries, `evBox3d`, `evDistance`), never build.
- **Let operations decide, rather than measuring to predict them.** When whether an operation works decides what to
  do, try it (rolled back, in a feature of its own) rather than estimating it from the geometry: Robot lighten finds
  a pocket's narrow sides, which need a full round, by filleting each side's two corners alone. Do it only once the
  operation itself has failed, so it costs nothing when it doesn't.

For example (`core/bore.fs`): a wrapped operation, whose error says what failed and highlights the parameters which
set it.

```
try
{
    opBoolean(context, id + "cut", { "tools" : tools, "targets" : targets, "operationType" : BooleanOperationType.SUBTRACTION });
}
catch
{
    // A failed boolean changes nothing, so the bores are still there to show
    throw regenError("Failed to cut bore.", BORE_PARAMETERS, tools);
}
```

## Logging

To find out why a feature fails on someone's part, log its steps with `core/logging.fs`: `logLine`, `logEntities`
(a count, and the entities shown as debug entities), and `logEdges` (each edge's type, size, and ends). They do
nothing unless the Part Studio has a variable `#logging` of `true` (a Variable feature of type Any, before the
feature), so features log as they go, with no parameter for it. Debug entities stay when the feature fails, so log
what an operation was given before it runs, and when it fails, what's useful for finding out why (like how far
Robot lighten's failed offset does go).

## Keywords as map keys

Don't name map keys after keywords (`type`, `default`, `function`, ...): `x.type` is a syntax error in Onshape, so such
a key can only be read as `x["type"]`. Use them only where a format needs them (std's hole attributes, lookup tables'
`"default"`); `fs check` warns about the rest.

## Types for structured data

When a map with a known shape is passed between functions, like the stock robot frame and robot nut strip place,
give it a type (`export type Stock typecheck canBeStock;`), document its fields there, and use it in signatures
(`function getFrame(definition is map) returns Stock`). Lookup table entries become one with `as`.

## Features finding each other

Don't have a feature select what an earlier one made, as Robot lighten once selected Quick rib's sketches (by feature
ids kept in a variable, with `qCreatedBy(featureId + "ribSketch", EntityType.EDGE)`): it couldn't be done robustly, so
leave selecting to the user. Where one feature's output is only ever another's input, fold it in: Robot lighten now
sketches those ribs itself (`lighten/quickRibs.fs`), and fills in Face to lighten from their first point in editing
logic.

## Ids

Every operation is given an `Id`, made from the feature's `id` (see `Id` in std's `context.fs`): `id + "extrude"`, or
`id + "holes" + "cut"`. Ids are hierarchical, and `qCreatedBy(id + "holes", EntityType.FACE)` returns what every
operation under `id + "holes"` made. So group the operations which make something under one id, and query the group,
rather than passing the ids of individual operations around:

```
const groupId = id + ("holes" ~ i);
sketchSeed(context, groupId + "seed", ...);    // a sketch and a tool extrude under groupId + "seed"
opBoolean(context, groupId + "cut", {
            "tools" : qCreatedBy(groupId, EntityType.BODY)->qBodyType(BodyType.SOLID),
            ...
        });
opPattern(context, groupId + "pattern", {
            "entities" : qCreatedBy(groupId, EntityType.FACE)->qOwnedByBody(body),
            ...
        });
```

This also doesn't depend on which operation is credited with a face, e.g. the tool's extrude or the boolean which cut
it into the body.

Each id, and each of its parents, must cover one contiguous run of operations. In a loop, put what varies first, as in
`id + ("holes" ~ i) + "cut"` (or `id + ("cut" ~ i)`), not `id + "cut" + i`, which fails on the second iteration if
other operations come between. Ids may only use `a-z`, `A-Z`, `0-9`, `_`, `+`, `-`, and `/`.

Booleans are slow. To cut many holes, cut one (or one of each kind) and face pattern it with `opPattern`, rather than
cutting a tool for each.

## Queries

Std's `query.fs` has the queries most code needs. The ones which come up most:

- `qCreatedBy(id, entityType)`: what an operation (or every operation under an id) made.
- `qCapEntity(id, CapType.START, entityType)` and `CapType.END`, and `qNonCapEntity(id, entityType)`: an extrude's
  (or sweep's) end faces and edges, and its other ones.
- `qPatternInstances(patternId, instanceName, entityType)`: what one instance of an `opPattern` made, by the
  `instanceNames` it was given.
- `qOwnedByBody(body, entityType)`: the faces, edges, or vertices of a body.
- `qSketchRegion(sketchId, filterInnerLoops)`: a sketch's regions (with `true`, leaving out regions inside others, like
  a tube's hollow).
- Filters: `qGeometry(GeometryType.CYLINDER)`, `qBodyType(BodyType.SOLID)`, `qSketchFilter(SketchObject.YES)`,
  `qEntityFilter(EntityType.FACE)`, `qCoincidesWithPlane(plane)`, `qContainsPoint(point)`, `qParallelPlanes(plane)`.
- Combining: `qUnion`, `qSubtraction`, `qIntersection`, and `qNthElement`.

To follow entities through later operations which change them (an offset, a shell, a boolean), wrap the query in
`startTracking` before those operations: `startTracking(context, qCreatedBy(id + "fillet", EntityType.FACE))` resolves,
afterwards, to what the fillet's faces became. A plain `qCreatedBy` used after an operation changed what it made isn't
reliable (Robot lighten's `opModifyFillet` on its pockets' corners failed on every corner until its fillets were
tracked through the offset which grows them).

Tracking takes in the last operation before it (`lastOperationId`) too, so start it after the operation which made
what's tracked, not straight after: tracking an extrude's caps from just after the extrude also finds its sides,
which are derived from its profile, the start cap (Robot lighten offset every face of its pocket, until it tracked
the caps from after the next step).

Build a union of many queries by collecting them in an array and calling `qUnion` once, at the end, rather than
`qUnion([soFar, next])` in a loop, which nests a query per step (and is slower to evaluate, and harder to read when
debugging):

```
var edges = [];
for (var featureId in featureIds)
{
    edges = append(edges, qCreatedBy(featureId + "ribSketch", EntityType.EDGE));
}
definition.ribEdges = qUnion(edges);
```

Queries are evaluated when they're used, not when they're made: a query of a group used after a later operation also
returns what that operation made. Make queries say what they mean instead (the operation's own id, a filter, or a
subtraction of what a later step adds), and pass them on as they are.

Use `evaluateQuery` only to go through a query's entities one by one (a `for` loop, `filter`, `mapArray`). Everywhere
else, use the query itself: operations, `ev` functions, and other queries take queries, and queries compose freely
(`qOwnedByBody(qCreatedBy(id + "pocket", EntityType.BODY), EntityType.FACE)`, `qSubtraction(faces, ends)`). To check
one, use `isQueryEmpty`, or `evaluateQueryCount` for how many it finds; not `evaluateQuery(...) == []` or `size(...)`.
Don't wrap one in `qUnion(evaluateQuery(...))` to pin it down: only where a step deletes what a query finds by (like
the faces a shell opens), and nothing else finds those entities, evaluate it before that step.

## Functions as values

A function declared with `function name(...)` (or a predicate) can only be called. To pass one as a value, e.g. to
`mapArray` or `filter`, declare it as a const set to a function instead; it can still be called the same way:

```
const holeInstanceName = function(k is number) returns string
    {
        return "" ~ k;
    };

mapArray(instances, holeInstanceName);
```

## Unused variables

Onshape warns about a variable which is set but never used. Name one you don't need `_`, e.g. the key of a map in
`for (var _, row in rows)`.

## What `fs check` enforces

- Enums used as a feature's parameter types (directly or through predicates) must be exported by the feature's file,
  including std's (`export import` the std module declaring one, not `common.fs`, which mustn't be exported).
- Predicates a feature's precondition uses from other files must be exported to the feature's file.
- A feature's precondition can't declare a parameter more than once, even in different branches of an `if`, and array
  parameters' items' parameters count too: an item's parameter can't share a name with any other parameter.
- Predicates in a precondition's `if` conditions can't call other predicates.
- Annotation keys must be ones Onshape knows (std's, and `Icon`): it ignores others, so a misspelled one (`"Driving
  query"` for `"Driven query"`, `"UIHInt"`) silently does nothing.
- Horizontal enums go at the top of the dialog: before any other shown parameter, and not in groups or array
  parameters' items (see "Horizontal enums go at the top").
- Top-level constants, enums, and types can't share a name with anything the file or its imports declare.
- Functions and predicates declared with `function` or `predicate`, std's too (`mapArray(points, norm)`), can only
  be called, not used as values (see "Functions as values").
- Std functions must be called with as many arguments as one of their overloads takes (`a->f(b)` passes `a` too):
  Onshape reports "Function f with 2 argument(s) not found".
- A function's variables can't be named as its parameters ("Variable x already declared in this scope").
- Annotation strings (names, descriptions) must be printable ASCII: no degree signs or curly quotes.
- No comparisons with `true` or `false`; use the value, or `!` it.
- No parameters which can be toleranced, directly or through std's predicates (see "No field tolerancing").
- Precondition conditions may only use parameters, enum values, literals, and predicates.
- No unused or unknown imports, std's too: a std module (other than `common.fs`) whose names the file doesn't use, or
  only uses ones its other std imports also give it (`chamfer.fs` for `ChamferType`, which `common.fs` exports). No
  top-level declarations which aren't exported or used.
- No local variables which are set but never used, counting only setting it from itself (`ids = append(ids, id)`,
  `count += 1`) as unused; name one you don't need `_` (see "Unused variables").
- No keywords as map keys (see "Keywords as map keys"); `x.type` is an error.
- Functions and predicates with the same name and parameter types as another in the file or its imports, and groups
  in array parameters' items, which Onshape rejects.
- No map keys written as bare names which are also constants or variables (`{ KEY : 1 }` is the string `"KEY"`;
  write `{ (KEY) : 1 }` to use KEY's value).
