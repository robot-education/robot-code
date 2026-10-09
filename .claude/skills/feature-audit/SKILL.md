---
name: feature-audit
description: Check, test, and show a FeatureScript feature after changing it, ending with its audit page (its working dialog, writeup, and problems) sent to the user. Use after adding or changing a feature in featurescripts/, or when the user asks to see, audit, or review a feature's options, dialog, or writeup.
---

# Auditing a feature

After changing a feature (its `.fs`, what it imports, or its writeup), run these, fix what they find, then send the
audit page. All are offline: none of them call the Onshape API (never `fs push`, `pull`, `sync`, or `release` without
the user's say-so).

1. `uv run fs format <files>` and `uv run fs check <files>`: no new problems.
2. `uv run pytest -q`: FeatureScript which computes values (math, tables, editing logic, manipulator change functions,
   sketches) is tested with the evaluator (see README's "Testing FeatureScript").
3. The writeup (the `.md` beside the `.fs`) matches the change: strings, execution order, `try`s, and how to trigger
   each error (see `docs/feature-writeups.md`).
4. `uv run fs audit <feature>.fs -o <scratchpad>/<feature>.html`, then send it with SendUserFile (`display: "render"`).
   It has the dialog, which works: its dropdowns, checkboxes, tabs, and lookup table levels show what each choice
   shows (a long press shows a parameter's tooltip by touch), from states rendered ahead of time (`--max-states`, 200
   by default; past that, choices say they aren't rendered). Typed values aren't rendered;
   `uv run fs ui <file> --set name=value` renders any state as a PNG.

The page also lists every lookup table the feature's parameters use, a row per option with its values, to glance
through rather than clicking through the dialog. For a writeup, `uv run fs table <file or folder> -n <table> --md`
prints one as Markdown.

Say in the reply what wasn't checked: nothing here runs the feature in Onshape, so its geometry, and whether its
operations fail, are untested until the user tries it.
