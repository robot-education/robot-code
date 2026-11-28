---
name: feature-audit
description: Check, test, and show a FeatureScript feature after changing it, ending with its audit page (its working dialog, writeup, and problems) shown to the user. Use after adding or changing a feature in featurescripts/, or when the user asks to see, audit, or review a feature's options, dialog, or writeup.
---

# Auditing a feature

After changing a feature (its `.fs`, what it imports, or its writeup), run these, fix what they find, then show the
audit page. All are offline: none of them call the Onshape API (never `fs push`, `pull`, `sync`, or `release` without
the user's say-so).

1. `uv run fs format <files>` and `uv run fs check <files>`: no new problems.
2. `uv run pytest -q tests/featurescript`: FeatureScript which computes values (math, tables, editing logic,
   manipulator change functions, sketches) is tested with the evaluator (see `docs/testing.md`). If you changed the
   tooling too, run every test (`uv run pytest -q`).
3. The writeup (the `.md` beside the `.fs`) matches the change: strings, execution order, and `try`s (see
   `docs/feature-writeups.md`).
4. `uv run fs audit <feature>.fs --artifact -o <scratchpad>/<feature>Audit.html --open`. Run from VS Code, that shows
   it in VS Code's feature preview (the same sections, with the language server drawing the dialog), and says so: then
   don't publish the page. Otherwise it says it isn't in VS Code: publish it as an Artifact (the Artifact tool, with
   `icon` "gear" or whatever suits the feature on its first publish), so the user can comment on it, and give them its
   link. Publish each later audit of the feature from the same file path, so it updates the same artifact (and its
   comments) rather than making another; from another session, find its URL (`action: "list"`), read it, and publish
   to it with `url`. Read the user's comments on it with ArtifactComments. Without the Artifact tool, send the page
   with SendUserFile (`display: "render"`) instead.
   It has the dialog, which works: its dropdowns, checkboxes, tabs, lookup table levels, and typed values show what
   each choice shows (a long press shows a parameter's tooltip by touch), in any combination: the page compiles the
   precondition's conditions to JavaScript and draws the dialog itself. Setting a parameter back to its default unsets it. Array items can't be added there;
   `uv run fs ui <file> --set name=value` renders any state as a PNG. For a part set's Feature Studio (fasteners,
   bearings), it shows each of its Part Studios' configuration panels instead, as Onshape will (see docs/parts.md),
   and, with the `render` group installed, a Parts section with each one's default part in 3D (see docs/rendering.md;
   the `part-render` skill shows other configurations).

The page also lists every option of each lookup table the feature's parameters use (a row per path through its
levels, without values), to glance through rather than clicking through the dialog. For a writeup, `uv run fs table <file or folder> --name <table> --md`
prints one as Markdown.

Say in the reply what wasn't checked: nothing here runs the feature in Onshape, so its geometry, and whether its
operations fail, are untested until the user tries it.
