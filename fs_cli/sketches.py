"""Sketch profiles for generated Feature Studios (see fs_cli.gen).

A profile is a list of lines, arcs, and circles, rendered as a `SketchDataArray` (see
core/sketchData.fs) which `skDataArray` adds to a sketch. Generated files using them should
Import("core/sketchData.fs").

Lengths are in inches (multiply by MM for millimeters), and angles are in degrees,
counterclockwise from the x axis.
"""

from __future__ import annotations

import dataclasses
import math
from typing import Mapping, Sequence

from fs_cli.tables import number

MM = 1 / 25.4

# Coordinates are rounded to this many decimal places (of an inch), well below Onshape's tolerances
PRECISION = 12


@dataclasses.dataclass(frozen=True)
class Point:
    x: float
    y: float

    def __add__(self, other: Point) -> Point:
        return Point(self.x + other.x, self.y + other.y)

    def __sub__(self, other: Point) -> Point:
        return Point(self.x - other.x, self.y - other.y)

    def __mul__(self, scale: float) -> Point:
        return Point(self.x * scale, self.y * scale)

    @property
    def length(self) -> float:
        return math.hypot(self.x, self.y)

    @property
    def angle(self) -> float:
        return math.degrees(math.atan2(self.y, self.x))

    def rotated(self, degrees: float) -> Point:
        c, s = math.cos(math.radians(degrees)), math.sin(math.radians(degrees))
        return Point(self.x * c - self.y * s, self.x * s + self.y * c)

    def render(self) -> str:
        return f"vector({_coordinate(self.x)}, {_coordinate(self.y)}) * inch"


ORIGIN = Point(0, 0)


def polar(radius: float, degrees: float, center: Point = ORIGIN) -> Point:
    return center + Point(radius, 0).rotated(degrees)


@dataclasses.dataclass(frozen=True)
class Line:
    start: Point
    end: Point

    def rotated(self, degrees: float) -> Line:
        return Line(self.start.rotated(degrees), self.end.rotated(degrees))

    def render(self) -> str:
        return (
            f'{{ "operation" : SketchOperation.LINE, "start" : {self.start.render()}, '
            f'"end" : {self.end.render()} }}'
        )


@dataclasses.dataclass(frozen=True)
class Arc:
    start: Point
    mid: Point
    end: Point

    @classmethod
    def around(cls, center: Point, radius: float, start: float, end: float) -> Arc:
        """The arc about center from angle start counterclockwise to angle end."""
        while end <= start:
            end += 360
        return cls(
            polar(radius, start, center),
            polar(radius, (start + end) / 2, center),
            polar(radius, end, center),
        )

    @classmethod
    def between(cls, center: Point, start: Point, end: Point, major: bool = False) -> Arc:
        """The arc about center joining two points on a circle: the shorter way, unless major."""
        a, b = (start - center).angle, (end - center).angle
        sweep = (b - a) % 360
        counterclockwise = (sweep <= 180) != major
        arc = cls.around(center, (start - center).length, *((a, b) if counterclockwise else (b, a)))
        return arc if counterclockwise else arc.reversed()

    def reversed(self) -> Arc:
        return Arc(self.end, self.mid, self.start)

    def rotated(self, degrees: float) -> Arc:
        return Arc(*(point.rotated(degrees) for point in (self.start, self.mid, self.end)))

    def render(self) -> str:
        return (
            f'{{ "operation" : SketchOperation.ARC, "start" : {self.start.render()}, '
            f'"mid" : {self.mid.render()}, "end" : {self.end.render()} }}'
        )


@dataclasses.dataclass(frozen=True)
class Circle:
    center: Point
    radius: float

    def rotated(self, degrees: float) -> Circle:
        return Circle(self.center.rotated(degrees), self.radius)

    def render(self) -> str:
        return (
            f'{{ "operation" : SketchOperation.CIRCLE, "center" : {self.center.render()}, '
            f'"radius" : {_coordinate(self.radius)} * inch }}'
        )


@dataclasses.dataclass(frozen=True)
class FitSpline:
    """A smooth spline through points (see `skFitSpline`), for curves which aren't lines or arcs."""

    points: tuple[Point, ...]

    @property
    def start(self) -> Point:
        return self.points[0]

    @property
    def end(self) -> Point:
        return self.points[-1]

    def rotated(self, degrees: float) -> FitSpline:
        return FitSpline(tuple(point.rotated(degrees) for point in self.points))

    def render(self) -> str:
        points = ", ".join(point.render() for point in self.points)
        return f'{{ "operation" : SketchOperation.SPLINE, "points" : [{points}] }}'


Entity = Line | Arc | Circle | FitSpline


def rotated(entities: Sequence[Entity], degrees: float) -> list[Entity]:
    return [entity.rotated(degrees) for entity in entities]


@dataclasses.dataclass
class Profile:
    """Sketch entities, in the order they're added to a sketch.

    skDataArray names each entity by its position, and those names end up in the ids of the
    faces it creates. Changing the order of a released profile would break references to its
    faces in documents using it, so `order` can list (by index into entities) the order an
    older version of the profile used.
    """

    entities: Sequence[Entity]
    order: Sequence[int] | None = None

    def __post_init__(self) -> None:
        if self.order is not None and sorted(self.order) != list(range(len(self.entities))):
            raise ValueError(
                f"order must list each of the {len(self.entities)} entities once, got {list(self.order)}"
            )

    def ordered(self) -> list[Entity]:
        if self.order is None:
            return list(self.entities)
        return [self.entities[index] for index in self.order]

    def render(self, indent: int) -> str:
        pad = " " * indent
        lines = [f"{pad}    {entity.render()}," for entity in self.ordered()]
        return "[\n" + "\n".join(lines) + f"\n{pad}] as SketchDataArray"


@dataclasses.dataclass
class Sketch:
    """An exported constant holding a profile."""

    name: str
    profile: Profile

    def render(self) -> str:
        return f"export const {self.name} = {self.profile.render(8)};\n"


@dataclasses.dataclass
class SketchMap:
    """An exported constant mapping FeatureScript expressions (e.g. "ProfileSide.INSIDE") to profiles."""

    name: str
    profiles: Mapping[str, Profile]

    def render(self) -> str:
        entries = "".join(
            f"        {key} : {profile.render(12)},\n" for key, profile in self.profiles.items()
        )
        return f"export const {self.name} = {{\n{entries}    }};\n"


def _coordinate(value: float) -> str:
    value = round(value, PRECISION)
    return number(0.0 if value == 0 else value)
