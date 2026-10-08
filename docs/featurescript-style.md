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

## Enums stored in documents

Enum values used as parameter types are stored in documents, so never rename or remove them once released. Generated
enums (e.g. in `*.gen.fs` files) follow the same rule; their definitions say so where it matters.

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

## Keywords as map keys

Don't name map keys after keywords (`type`, `default`, `function`, ...): `x.type` is a syntax error in Onshape, so such
a key can only be read as `x["type"]`. Use them only where a format needs them (std's hole attributes, lookup tables'
`"default"`); `fs check` warns about the rest.

## Types for structured data

When a map with a known shape is passed between functions, like the stock robot frame and robot nut strip place,
give it a type (`export type Stock typecheck canBeStock;`), document its fields there, and use it in signatures
(`function getFrame(definition is map) returns Stock`). Lookup table entries become one with `as`.

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

Queries are evaluated when they're used, not when they're made: a query of a group used after a later operation also
returns what that operation made. Use `evaluateQuery` to fix what one returns now, and `isQueryEmpty` to check it.

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
- A feature's precondition can't declare a parameter more than once, even in different branches of an `if`.
- Predicates in a precondition's `if` conditions can't call other predicates.
- Top-level constants, enums, and types can't share a name with anything the file or its imports declare.
- Functions and predicates declared with `function` or `predicate` can only be called, not used as values (see
  "Functions as values").
- No comparisons with `true` or `false`; use the value, or `!` it.
- No parameters which can be toleranced, directly or through std's predicates (see "No field tolerancing").
- Precondition conditions may only use parameters, enum values, literals, and predicates.
- No unused or unknown imports, and no top-level declarations which aren't exported or used.
- No map keys written as bare names which are also constants or variables (`{ KEY : 1 }` is the string `"KEY"`;
  write `{ (KEY) : 1 }` to use KEY's value).
