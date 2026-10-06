# FeatureScript style

Conventions for the FeatureScripts in `featurescripts/`. `fs check` enforces some of them (see the end); the rest are
up to review.

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
that data with `fs gen` rather than reading a constant. For example, `printAdapter/printAdapterProfiles.py`
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

## Selections

A selection of where to place something takes one pick (`"MaxNumberOfPicks" : 1`), and is named for it, e.g. "Edge to
place nut strip" or "Sketch point to place nut strip"; to place more, add more features. Selections of what to act on,
like a merge scope or the faces to extrude, can take several.

## No defaults maps

Don't pass `defineFeature` its third argument, a map of defaults. It only applies when a feature is called from code,
which ours never are; in a dialog, parameters take their defaults from their annotations and bounds.

## Editing logic and manipulator change functions

Write these defensively: check that values are what they should be before using them (a manipulator's `index` or
`flipped`, a hidden parameter, a field of `oldDefinition`), and fall back to a default rather than throwing. An error in
one surfaces when the user clicks a manipulator or edits a parameter, far from its cause.

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

Compare measured values with std's tolerant functions (`tolerantEquals`, `tolerantLessThan`,
`tolerantLessThanOrEqual`, `tolerantGreaterThan`, ...) rather than `<`, `>`, and `==`, or adding
`TOLERANCE.zeroLength` by hand. This goes for editing logic too.

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
- Precondition conditions may only use parameters, enum values, literals, and predicates.
- No unused or unknown imports, and no top-level declarations which aren't exported or used.
- No map keys written as bare names which are also constants or variables (`{ KEY : 1 }` is the string `"KEY"`;
  write `{ (KEY) : 1 }` to use KEY's value).
