---
name: part-render
description: Build FeatureScript-backed parts (part sets: fasteners like screws, nuts, washers, and retainers; bearings; bushings) offline, without Onshape, and show them: PNG renders, their metadata (name, vendor, part number, material, mass, size, threads), and a 3D viewer page published as an Artifact the user can open on a phone. Use when the user asks to see, render, screenshot, preview, or check a part set's part or configurations, or after changing a part set's FeatureScript or tables.
---

# Rendering parts

`fs render` runs a part set's FeatureScript in the evaluator with real geometry (OpenCascade) and draws the result. It
calls no Onshape API, so it needs no asking. See `docs/rendering.md` for how it works. Its OpenCascade is in a default
dependency group, so `uv run` installs it the first time, in a cloud session too.

1. Render what the user asked for, and only that: the page lists every configuration rendered, so a page of one part
   shows that part alone. Pick configurations by their input and option ids, as the part set's definition names them
   (`featurescripts/<folder>/<definition>.py`'s `STUDIOS`; a mistyped or unoffered option makes `fs render` list what's
   offered). `--set` picks options (the rest take their defaults); `--each INPUT` renders every option of one input,
   for when the user asks to compare them. Examples:
   - "show me a 1/4-20 button head screw, 1/2 in long":
     `uv run fs render fasteners/screw --set size=QUARTER_20 --set head=BUTTON --set length=IN_0_5 --artifact -o <scratchpad>/render`
   - "what nuts are there for #10-32?": `uv run fs render fasteners/nut --set size=NO_10_32 --each type --artifact -o <scratchpad>/render`
   - "the default bearing": `uv run fs render bearings/bearing --artifact -o <scratchpad>/render`
   Always add `--artifact` when the user will look at it: the page is then an Artifact's, its data in `parts.json`
   beside it.
2. Look at the PNGs yourself (Read them) before showing anything: check the part is what was asked for (its head,
   socket, chamfers, bore, flange, the nylon insert's color) and nothing's missing or inside out. The JSON beside each
   has its name, part number, material, mass, size, threads, and mate connectors; quote them rather than guessing.
3. Show it:
   - Publish the page with the Artifact tool: `file_path` its `index.html`, `files: {"parts.json": "<dir>/parts.json"}`,
     `icon` "gear" on its first publish, and a one-sentence `description`. Give the user its link: it opens in the
     Claude app, where the part can be turned by touch. Publish later renders of the same question from the same
     `index.html` path, so the link stays the same.
   - Where showing files works (SendUserFile), also send the PNG, with `display: "render"`.
4. After changing a part set's FeatureScript or tables, also run `uv run fs render <set> --check-all`, which builds
   every configuration and reports the ones that fail and why, and `uv run pytest -q tests/featurescript`.

Threads are cosmetic (as in Onshape), so they're listed, not drawn. Mate connectors are drawn as Onshape draws them (a
circle and its center); tapping one shows its triad (red X, green Y, blue Z).

If a build fails with "isn't available in the evaluator" or "aren't supported", the part uses an operation or query
`fs_eval.geometry` doesn't model yet: say so, and offer to add it (`fs_eval/geometry/`, with a test in
`tests/fs_eval_geometry_test.py`). Don't fall back to Onshape without asking (AGENTS.md).
