# Onshape icons

SVG icons from Onshape's UI, for `fs ui` (see `fs_cli/ui.py`) to draw dialogs the way Onshape does. Open
`index.html` to browse them.

Icons are named as they're used, in folders by where Onshape uses them (`dialog/` for the buttons in feature
dialogs, `hole/` for the hole feature's parameters); code refers to them by name, like `hole/diameter`. The rest are
still numbered in the order they were captured (`svg-N.svg`). To name one, move it into a folder (`git mv svg-846.svg
dialog/flip.svg`), and run `uv run python onshape_icons/make_index.py` to update `index.html`. Numbers are never
reused, so gaps are left by named icons and by exact copies, which were removed. Some icons are the same shape in a
different color (e.g. a feature's normal, hovered, and disabled states, or an error and a warning), and some are white,
for dark backgrounds; those are kept.
