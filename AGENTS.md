# Agent instructions

See `README.md` for how the repo and the `fs` CLI work, and `docs/featurescript-style.md` for how to write
FeatureScript here.

## Backwards compatibility

Don't consider older versions of features or documents made with them: renaming or removing parameters, changing
defaults, or changing what's built is fine, with no migration or fallback for old values. Don't add code to handle
values saved by an older version.

The exception is attributes, which other features and documents read back. When an attribute's format changes, either
handle old attributes or report them as an error; either is fine.

## Selections

A selection of where to place something takes one pick (`"MaxNumberOfPicks" : 1`), and is named for it, e.g. "Edge to
place nut strip" or "Sketch point to place nut strip"; to place more, add more features. Selections of what to act on,
like a merge scope or the faces to extrude, can take several.

## Manipulator change functions and editing logic

Write these defensively: check that values are what they should be before using them (a manipulator's `index` or
`flipped`, a hidden parameter, a field of `oldDefinition`), and fall back to a default rather than throwing. An error in
one surfaces when the user clicks a manipulator or edits a parameter, far from its cause. This is ordinary defensive
programming, not handling older versions.

The first time editing logic runs (when the feature is created), `oldDefinition` is the empty map `{}`, so every
`oldDefinition.x` is `undefined`. Comparing it is fine (`oldDefinition.edges != definition.edges` is true then), but
don't use it as a value, e.g. `oldDefinition.flag ?? false` instead of `oldDefinition.flag`.
