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

As in std features, a horizontal enum (`"UIHint" : ["HORIZONTAL_ENUM"]`) belongs at the top of its UI block: a
precondition, a parameter group, or an array parameter's items. It may only follow other horizontal enums, or a single
normal enum at the very top. Hidden parameters don't count, and a predicate counts as the parameters it declares.

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
there instead, or start a group with it.

## Enums stored in documents

Enum values used as parameter types are stored in documents, so never rename or remove them once released. Generated
enums (e.g. in `*.gen.fs` files) follow the same rule; their definitions say so where it matters.

## What `fs check` enforces

- Enums used as a feature's parameter types (directly or through predicates) must be exported by the feature's file.
- No comparisons with `true` or `false`; use the value, or `!` it.
- Precondition conditions may only use parameters, enum values, literals, and predicates.
- No unused or unknown imports, and no top-level declarations which aren't exported or used.
- No map keys written as bare names which are also constants or variables (`{ KEY : 1 }` is the string `"KEY"`;
  write `{ (KEY) : 1 }` to use KEY's value).
