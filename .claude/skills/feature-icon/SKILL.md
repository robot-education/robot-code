---
name: feature-icon
description: Draw or change a feature's icon in Onshape's style, from shapes in Python (fs_cli/icons.py), and show variations side by side. Use when the user asks for a feature icon, or to change, compare, or tweak one.
---

# Drawing a feature icon

Read `docs/icons.md` first: Onshape's style, the drawing functions, and lessons from earlier icons. All of this is
offline: never `fs push` (which uploads icons) without the user's say-so.

1. Find what to base it on: our icons (`featurescripts/**/*.svg`) and Onshape's (`onshape_icons/feature/`, more in its
   sprite; see the doc). Save Onshape icons you use there, named.
2. Draw a few variations: a definition per variation (`ICON = Icon()...`), each in the scratchpad while trying them,
   rendered with `fs_cli.icons` (`ICON.render()` to a file), or written beside the feature with `uv run fs gen`.
3. `uv run fs icons <reference.svg ...> <variation.svg ...> -o <scratchpad>/icons.png`, look at it (big, and at 20 px
   on light and dark), fix what doesn't read, and send it with SendUserFile (`display: "render"`), saying which you'd
   pick and why.
4. Once one's chosen: its definition beside the feature (`<feature>Icon.py`), `uv run fs gen`, the feature importing
   it by path (see the doc), and its writeup's files table mentioning it. Then the feature-audit skill.
