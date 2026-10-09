"""Recorded sketches (see `fs_eval.builtins`) as geometry, in meters in the sketch's plane, for tests to check
properties of rather than pictures: that a profile's curves join into closed loops, meet tangent, stay within a
radius, and so on.

```
sketch = recorded_sketch(context, "profile")
for loop in sketch.loops():
    assert loop.closed
```
"""

from __future__ import annotations

import dataclasses
import math

from fs_eval.values import Context, FSArray, FSMap, key_of, untag

Point = tuple[float, float]


def _point(value) -> Point:
    value = untag(value)
    coordinates = []
    for item in value.items:
        item = untag(item)
        if type(item) is FSMap:
            item = untag(item.get_str("value"))
        coordinates.append(float(item))
    return (coordinates[0], coordinates[1])


def _length(value) -> float:
    value = untag(value)
    if type(value) is FSMap:
        return float(untag(value.get_str("value")))
    return float(value)


@dataclasses.dataclass
class Curve:
    """A line, an arc (with `center`, `radius`, and whether it goes counter clockwise), or a fit spline (through
    `points`), from `start` to `end`; or a whole circle (`start` and `end` are the same point)."""

    kind: str
    id: str
    start: Point
    end: Point
    center: Point | None = None
    radius: float | None = None
    counter_clockwise: bool = True
    construction: bool = False
    points: list | None = None

    def tangent(self, at_end: bool) -> Point:
        """The unit direction it's traveling in at its start, or end (a spline's, roughly: along its end segment)."""
        if self.kind == "line":
            dx, dy = self.end[0] - self.start[0], self.end[1] - self.start[1]
        elif self.kind == "spline":
            a, b = (self.points[-2], self.points[-1]) if at_end else (self.points[0], self.points[1])
            dx, dy = b[0] - a[0], b[1] - a[1]
        else:
            point = self.end if at_end else self.start
            rx, ry = point[0] - self.center[0], point[1] - self.center[1]
            dx, dy = (-ry, rx) if self.counter_clockwise else (ry, -rx)
        norm = math.hypot(dx, dy)
        return (dx / norm, dy / norm)

    def reversed(self) -> Curve:
        points = list(reversed(self.points)) if self.points else self.points
        return dataclasses.replace(self, start=self.end, end=self.start, counter_clockwise=not self.counter_clockwise, points=points)

    def farthest(self, origin: Point = (0.0, 0.0)) -> float:
        """How far from `origin` it gets."""
        ends = max(math.dist(self.start, origin), math.dist(self.end, origin))
        if self.kind == "spline":
            return max(math.dist(point, origin) for point in self.points)
        if self.kind == "line":
            return ends
        # An arc's farthest point is an end, or where it crosses the line from origin through its center
        to_center = math.dist(self.center, origin)
        if to_center == 0:
            return self.radius
        direction = ((self.center[0] - origin[0]) / to_center, (self.center[1] - origin[1]) / to_center)
        far = (self.center[0] + direction[0] * self.radius, self.center[1] + direction[1] * self.radius)
        return max(ends, to_center + self.radius) if self.kind == "circle" or self.contains_angle(far) else ends

    def nearest(self, origin: Point = (0.0, 0.0)) -> float:
        """How near `origin` it gets."""
        ends = min(math.dist(self.start, origin), math.dist(self.end, origin))
        if self.kind == "spline":
            return min(math.dist(point, origin) for point in self.points)
        if self.kind == "line":
            sx, sy = self.start
            dx, dy = self.end[0] - sx, self.end[1] - sy
            t = max(0.0, min(1.0, ((origin[0] - sx) * dx + (origin[1] - sy) * dy) / (dx * dx + dy * dy)))
            return math.dist((sx + t * dx, sy + t * dy), origin)
        to_center = math.dist(self.center, origin)
        if to_center == 0:
            return self.radius
        direction = ((origin[0] - self.center[0]) / to_center, (origin[1] - self.center[1]) / to_center)
        near = (self.center[0] + direction[0] * self.radius, self.center[1] + direction[1] * self.radius)
        if self.kind == "circle" or self.contains_angle(near):
            return abs(to_center - self.radius)
        return ends

    def contains_angle(self, point: Point) -> bool:
        """Whether a point on an arc's circle is on the arc."""
        def angle(p):
            return math.atan2(p[1] - self.center[1], p[0] - self.center[0])

        start, end, test = angle(self.start), angle(self.end), angle(point)
        if not self.counter_clockwise:
            start, end = end, start
        sweep = (end - start) % (2 * math.pi)
        return (test - start) % (2 * math.pi) <= sweep


def _circle_through(a: Point, b: Point, c: Point) -> tuple[Point, float]:
    d = 2 * (a[0] * (b[1] - c[1]) + b[0] * (c[1] - a[1]) + c[0] * (a[1] - b[1]))
    if d == 0:
        raise ValueError("an arc's three points are in a line")
    a2, b2, c2 = a[0] ** 2 + a[1] ** 2, b[0] ** 2 + b[1] ** 2, c[0] ** 2 + c[1] ** 2
    x = (a2 * (b[1] - c[1]) + b2 * (c[1] - a[1]) + c2 * (a[1] - b[1])) / d
    y = (a2 * (c[0] - b[0]) + b2 * (a[0] - c[0]) + c2 * (b[0] - a[0])) / d
    return (x, y), math.dist((x, y), a)


@dataclasses.dataclass
class Loop:
    curves: list[Curve]
    closed: bool

    def joints(self) -> list[tuple[Curve, Curve]]:
        """Each pair of curves which meet, in order (the last and first too, if it's closed)."""
        pairs = list(zip(self.curves, self.curves[1:]))
        if self.closed and len(self.curves) > 1:
            pairs.append((self.curves[-1], self.curves[0]))
        return pairs


class RecordedSketch:
    def __init__(self, sketch):
        self.sketch = sketch
        self.curves: list[Curve] = []
        self.points: list[Point] = []
        for entity in sketch.entities:
            value: FSMap = entity["value"]
            kind = entity["type"]
            construction = value.get_str("construction") is True
            if kind == "line":
                self.curves.append(Curve("line", entity["id"], _point(value.get_str("start")), _point(value.get_str("end")),
                                         construction=construction))
            elif kind == "arc":
                start, mid, end = (_point(value.get_str(key)) for key in ("start", "mid", "end"))
                center, radius = _circle_through(start, mid, end)
                cross = (mid[0] - start[0]) * (end[1] - mid[1]) - (mid[1] - start[1]) * (end[0] - mid[0])
                self.curves.append(Curve("arc", entity["id"], start, end, center, radius, cross > 0, construction))
            elif kind == "circle":
                center = _point(value.get_str("center"))
                radius = _length(value.get_str("radius"))
                point = (center[0] + radius, center[1])
                self.curves.append(Curve("circle", entity["id"], point, point, center, radius, True, construction))
            elif kind == "fitSpline":
                points = [_point(point) for point in untag(value.get_str("points")).items]
                self.curves.append(Curve("spline", entity["id"], points[0], points[-1], construction=construction, points=points))
            elif kind == "point":
                self.points.append(_point(value.get_str("position")))

    def loops(self, tolerance: float = 1e-9) -> list[Loop]:
        """The sketch's (non-construction) curves, joined end to end where they meet within `tolerance`."""
        remaining = [curve for curve in self.curves if not curve.construction]
        loops = []
        while remaining:
            first = remaining.pop(0)
            if first.kind == "circle":
                loops.append(Loop([first], True))
                continue
            chain = [first]
            while True:
                end = chain[-1].end
                for i, curve in enumerate(remaining):
                    if curve.kind == "circle":
                        continue
                    if math.dist(curve.start, end) <= tolerance:
                        chain.append(remaining.pop(i))
                        break
                    if math.dist(curve.end, end) <= tolerance:
                        chain.append(remaining.pop(i).reversed())
                        break
                else:
                    break
            loops.append(Loop(chain, math.dist(chain[-1].end, chain[0].start) <= tolerance))
        return loops


def recorded_sketch(context: Context, *id_components: str) -> RecordedSketch:
    """The sketch recorded under the id made of `id_components` (like `newId() + "profile"`'s "profile")."""
    key = key_of(FSArray(tuple(id_components)))
    sketch = context.sketches.get(key)
    if sketch is None:
        names = [" + ".join(part[1] for part in k[1]) for k in context.sketches]
        raise KeyError(f"No sketch was solved under {id_components}: there are {names}")
    return RecordedSketch(sketch)
