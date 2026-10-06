# Agent instructions

See `README.md` for how the repo and the `fs` CLI work, and `docs/featurescript-style.md` for how to write
FeatureScript here (including selections, editing logic, and manipulator change functions).

## Backwards compatibility

Don't consider older versions of features or documents made with them: renaming or removing parameters, changing
defaults, or changing what's built is fine, with no migration or fallback for old values. Don't add code to handle
values saved by an older version.

The exception is attributes, which other features and documents read back. When an attribute's format changes, either
handle old attributes or report them as an error; either is fine.
