"""Feature icons drawn from shapes (with shapely), in the style of Onshape's own.

An icon definition is a Python file in the code folder, beside the feature, which sets ICON to an `Icon`; `fs gen`
writes it to the .svg file of the same name (lighten/robotLightenIcon.py writes lighten/robotLightenIcon.svg), and
`fs icons` shows icons side by side, big and at the size Onshape shows them. See docs/icons.md.

Icons are 20 units square, drawn in Onshape's oblique view: the face toward you, with what's behind it going back up
and to the left. Shapes are shapely geometry in those units (y down, as in SVG), so they can be combined, offset
(`buffer`), and moved (`shapely.affinity`) before they're drawn.
"""

from __future__ import annotations

import dataclasses
import html
import pathlib
import subprocess
import tempfile

from shapely import affinity
from shapely.geometry import Point, box
from shapely.geometry.base import BaseGeometry
from shapely.ops import unary_union

SIZE = 20

# Onshape's icon colors (as its sprite names them)
OUTLINE = "#333333"  # outline-primary: outlines, and openings you see through
FACE = "#999999"  # fill-secondary: the face toward you, and walls inside cuts
SIDE = "#D7D7D7"  # fill-quaternary: faces going back
WHITE = "#FFFFFF"  # fill-primary: sketch faces, and parts' faces in Onshape's Rib and Shell

# Onshape's outlines are about 1.3 units; a cut's rim, inside a face, is thinner
OUTLINE_WIDTH = 1.3
RIM = 0.45

# Enough points that curves look smooth at 120 px
_SEGMENTS = 16


def rounded_rectangle(x0: float, y0: float, x1: float, y1: float, radius: float) -> BaseGeometry:
    return box(x0, y0, x1, y1).buffer(-radius, quad_segs=_SEGMENTS).buffer(radius, quad_segs=_SEGMENTS)


def circle(center: tuple[float, float], radius: float) -> BaseGeometry:
    return Point(center).buffer(radius, quad_segs=2 * _SEGMENTS)


def offset(shape: BaseGeometry, distance: float) -> BaseGeometry:
    """Grows shape by distance (or shrinks it, negative), rounding the corners it grows."""
    return shape.buffer(distance, quad_segs=_SEGMENTS)


def round_corners(shape: BaseGeometry, radius: float) -> BaseGeometry:
    """Rounds shape's convex corners (as a router bit cuts a pocket's)."""
    return offset(offset(shape, -radius), radius)


def back(shape: BaseGeometry, depth: float) -> BaseGeometry:
    """Shape moved back by depth: up and to the left."""
    return affinity.translate(shape, -depth, -depth)


def sweep(shape: BaseGeometry, depth: float) -> BaseGeometry:
    """Everything shape covers as it goes back by depth: what an extrusion of it shows."""
    steps = max(1, int(depth / 0.05))
    return unary_union([back(shape, depth * i / steps) for i in range(steps + 1)])


def path(shape: BaseGeometry, tolerance: float = 0.02) -> str:
    """Shape as an SVG path's data, simplified to within tolerance."""
    shape = shape.simplify(tolerance)
    polygons = getattr(shape, "geoms", [shape])
    data = ""
    for polygon in polygons:
        if polygon.is_empty:
            continue
        for ring in [polygon.exterior, *polygon.interiors]:
            points = list(ring.coords)[:-1]
            data += "M" + "L".join(f"{_number(x)} {_number(y)}" for x, y in points) + "Z"
    return data


def _number(value: float) -> str:
    return f"{value:.2f}".rstrip("0").rstrip(".")


@dataclasses.dataclass
class Icon:
    """An icon, drawn in order: later shapes over earlier ones."""

    elements: list[str] = dataclasses.field(default_factory=list)
    _clips: int = 0

    def fill(self, shape: BaseGeometry, color: str) -> Icon:
        self.elements.append(f'<path d="{path(shape)}" fill="{color}"/>')
        return self

    def outlined(self, shape: BaseGeometry, color: str, width: float = OUTLINE_WIDTH) -> Icon:
        """Shape filled with color, with an outline (centered on its edge)."""
        return self.fill(offset(shape, width / 2), OUTLINE).fill(offset(shape, -width / 2), color)

    def extrusion(self, front: BaseGeometry, depth: float, color: str = WHITE, side: str = SIDE) -> Icon:
        """A solid: its front face (color), and its sides going back by depth (side), outlined."""
        sides = sweep(front, depth)
        return self.outlined(sides, side).outlined(front, color)

    def through(self, outline: BaseGeometry, depth: float = 1.0, rim: float = RIM) -> Icon:
        """A cut through a face (a hole or pocket), its outside edge `outline`: a dark rim, the wall you see inside
        it (on its lower right, as the cut goes back up and to the left), and the dark opening at its back."""
        inside = offset(outline, -rim)
        self._clips += 1
        clip = f"c{self._clips}"
        self.fill(outline, OUTLINE)
        self.elements.append(
            f'<clipPath id="{clip}"><path d="{path(inside)}"/></clipPath><g clip-path="url(#{clip})">'
            f'<path d="{path(inside)}" fill="{FACE}"/><path d="{path(back(inside, depth))}" fill="{OUTLINE}"/></g>'
        )
        return self

    def render(self) -> str:
        return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {SIZE} {SIZE}">{"".join(self.elements)}</svg>\n'


# Previews


def preview_page(icons: list[pathlib.Path], big: int = 120) -> str:
    """A page showing each icon big, and at the size Onshape shows them (20 px) on light and dark."""
    cells = []
    for icon in icons:
        uri = icon.resolve().as_uri()
        cells.append(
            "<div style='text-align:center;font:13px sans-serif;width:150px'>"
            f"<img src='{uri}' style='width:{big}px;height:{big}px'>"
            f"<div style='margin:6px 0'>{html.escape(icon.stem)}</div>"
            f"<img src='{uri}' style='width:20px;height:20px'> "
            "<span style='background:#333;padding:4px;display:inline-block'>"
            f"<img src='{uri}' style='width:20px;height:20px;vertical-align:middle'></span></div>"
        )
    return f"<body style='margin:0;padding:12px;display:flex;flex-wrap:wrap;gap:16px;background:#fff'>{''.join(cells)}</body>"


def screenshot_preview(icons: list[pathlib.Path], output: pathlib.Path) -> None:
    from fs_cli.ui import UiError, find_chromium

    chromium, exact = find_chromium()
    columns = min(len(icons), 6)
    rows = (len(icons) + columns - 1) // columns
    width, height = 24 + 166 * columns, 24 + 190 * rows + (0 if exact else 90)
    with tempfile.TemporaryDirectory() as directory:
        page = pathlib.Path(directory) / "icons.html"
        page.write_text(preview_page(icons))
        subprocess.run(
            [
                chromium,
                "--headless",
                "--no-sandbox",
                "--disable-gpu",
                "--hide-scrollbars",
                "--allow-file-access-from-files",
                f"--window-size={width},{height}",
                f"--screenshot={output.resolve()}",
                page.as_uri(),
            ],
            capture_output=True,
            timeout=120,
        )
    if not output.is_file():
        raise UiError("Chromium didn't save the screenshot.")
