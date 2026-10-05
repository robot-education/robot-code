"""Reads planar profiles out of STEP files, so vendor parts can be turned into sketch profiles.

Only what a profile needs is supported: planar faces bounded by lines, circles, and (non-rational)
B-spline curves. B-splines become FitSplines through points sampled along them; see
`StepFile.profile`.
"""

from __future__ import annotations

import bisect
import dataclasses
import math
import pathlib
import re
from typing import Any, Sequence

from fs_cli.sketches import Arc, Entity, FitSpline, Line, Point


class StepError(Exception):
    pass


@dataclasses.dataclass(frozen=True)
class Ref:
    id: int


@dataclasses.dataclass(frozen=True)
class Enum:
    value: str


@dataclasses.dataclass
class Instance:
    type: str
    args: list[Any]


_TOKEN = re.compile(
    r"\s*(?:(?P<ref>#\d+)|(?P<string>'(?:[^']|'')*')|(?P<enum>\.[A-Z0-9_]+\.)"
    r"|(?P<number>[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?)|(?P<name>[A-Z_][A-Z0-9_]*)|(?P<punct>[(),$*]))"
)


def _parse_value(text: str, position: int) -> tuple[Any, int]:
    """Parses one parameter (possibly a list or a typed value) starting at position."""
    match = _TOKEN.match(text, position)
    if not match:
        raise StepError(f"Can't parse STEP parameter at {text[position:position + 40]!r}")
    end = match.end()
    if match["ref"]:
        return Ref(int(match["ref"][1:])), end
    if match["string"]:
        return match["string"][1:-1].replace("''", "'"), end
    if match["enum"]:
        return Enum(match["enum"][1:-1]), end
    if match["number"]:
        return float(match["number"]), end
    if match["name"]:
        # A typed value, e.g. LENGTH_MEASURE(1.0)
        args, end = _parse_list(text, end)
        return Instance(match["name"], args), end
    if match["punct"] == "(":
        return _parse_list(text, match.start("punct"))
    return None, end  # $ or *


def _parse_list(text: str, position: int) -> tuple[list[Any], int]:
    match = _TOKEN.match(text, position)
    if not match or match["punct"] != "(":
        raise StepError(f"Expected a list at {text[position:position + 40]!r}")
    values: list[Any] = []
    position = match.end()
    while True:
        match = _TOKEN.match(text, position)
        if match and match["punct"] == ")":
            return values, match.end()
        value, position = _parse_value(text, position)
        values.append(value)
        match = _TOKEN.match(text, position)
        if match and match["punct"] == ",":
            position = match.end()


def _parse_instances(body: str) -> list[Instance]:
    """Parses `NAME(...)`, or a complex instance `(NAME(...) NAME(...))`, into its parts."""
    body = body.strip()
    if body.startswith("("):
        parts, position = [], 1
        while True:
            match = _TOKEN.match(body, position)
            if match is None or match["punct"] == ")":
                return parts
            args, position = _parse_list(body, match.end())
            parts.append(Instance(match["name"], args))
    match = _TOKEN.match(body)
    if match is None or not match["name"]:
        raise StepError(f"Can't parse STEP entity {body[:40]!r}")
    args, _ = _parse_list(body, match.end())
    return [Instance(match["name"], args)]


class StepFile:
    def __init__(self, path: pathlib.Path) -> None:
        text = path.read_text(errors="replace")
        text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
        data = text[text.index("DATA;") + len("DATA;") :]
        self.entities: dict[int, list[Instance]] = {}
        for match in re.finditer(r"#(\d+)\s*=\s*(.*?);(?=\s*(?:#\d+\s*=|ENDSEC))", data, re.S):
            self.entities[int(match[1])] = _parse_instances(match[2])
        self.inches_per_unit = self._length_unit()

    def get(self, ref: Ref, type: str | None = None) -> Instance:
        instances = self.entities[ref.id]
        if type is None:
            return instances[0]
        for instance in instances:
            if instance.type == type:
                return instance
        raise StepError(f"#{ref.id} isn't a {type}")

    def _length_unit(self) -> float:
        """Inches per unit of the solid's representation (files may define several units)."""
        contexts = [
            instances
            for instances in self.entities.values()
            if instances[0].type in ("ADVANCED_BREP_SHAPE_REPRESENTATION", "MANIFOLD_SURFACE_SHAPE_REPRESENTATION")
        ]
        candidates = []
        for representation in contexts:
            context = self.entities[representation[0].args[2].id]
            for instance in context:
                if instance.type == "GLOBAL_UNIT_ASSIGNED_CONTEXT":
                    candidates += [self.entities[ref.id] for ref in instance.args[0]]
        if not candidates:
            candidates = list(self.entities.values())
        for instances in candidates:
            types = {instance.type: instance for instance in instances}
            if "LENGTH_UNIT" not in types:
                continue
            if "CONVERSION_BASED_UNIT" in types:
                name = types["CONVERSION_BASED_UNIT"].args[0].lower()
                if name in ("inch", "in"):
                    return 1.0
                if name in ("foot", "ft"):
                    return 12.0
            if "SI_UNIT" in types:
                prefix = types["SI_UNIT"].args[0]
                scale = {"MILLI": 1e-3, "CENTI": 1e-2, None: 1.0}.get(
                    prefix.value if isinstance(prefix, Enum) else None
                )
                if scale is not None:
                    return scale / 0.0254
        raise StepError("Couldn't find the file's length unit.")

    # Geometry

    def point(self, ref: Ref) -> tuple[float, float, float]:
        instance = self.get(ref)
        if instance.type == "VERTEX_POINT":
            instance = self.get(instance.args[1])
        x, y, z = instance.args[1]
        scale = self.inches_per_unit
        return x * scale, y * scale, z * scale

    def direction(self, ref: Ref) -> tuple[float, float, float]:
        return tuple(self.get(ref, "DIRECTION").args[1])  # type: ignore[return-value]

    # Profiles

    def profile(self, z: float | None = None, tolerance: float = 2e-4) -> list[Entity]:
        """The outer loop of a planar face perpendicular to Z, as sketch entities in inches.

        Args:
            z: The face's height (in inches); defaults to the highest such face.
            tolerance: How far (in inches) a FitSpline may stray from the B-spline it replaces,
                as estimated by fitting a cubic spline through the points sampled.
        """
        faces = []
        for id, instances in self.entities.items():
            face = instances[0]
            if face.type != "ADVANCED_FACE":
                continue
            plane = self.get(face.args[2])
            if plane.type != "PLANE":
                continue
            placement = self.get(plane.args[1])
            normal = self.direction(placement.args[2])
            if abs(abs(normal[2]) - 1) > 1e-9:
                continue
            faces.append((self.point(placement.args[1])[2], id, face))
        if z is not None:
            faces = [face for face in faces if abs(face[0] - z) < 1e-6]
        if not faces:
            raise StepError(f"No planar face perpendicular to Z{'' if z is None else f' at z = {z}'}.")
        _, id, face = max(faces, key=lambda face: face[0])
        return [self._edge(ref, tolerance) for ref in self._outer_loop(id, face).args[1]]

    def _outer_loop(self, id: int, face: Instance) -> Instance:
        """A face's outer loop: its FACE_OUTER_BOUND, or (as some exporters only write FACE_BOUNDs)
        the bound reaching furthest from the origin."""
        bounds = [self.get(ref) for ref in face.args[1]]
        outer = [bound for bound in bounds if bound.type == "FACE_OUTER_BOUND"]
        if len(outer) > 1:
            raise StepError(f"Face #{id} has more than one outer bound.")
        if outer:
            return self.get(outer[0].args[1], "EDGE_LOOP")
        loops = [self.get(bound.args[1], "EDGE_LOOP") for bound in bounds]

        def reach(loop: Instance) -> float:
            return max(
                math.hypot(*self.point(vertex)[:2])
                for ref in loop.args[1]
                for vertex in self.get(self.get(ref, "ORIENTED_EDGE").args[3], "EDGE_CURVE").args[1:3]
            )

        return max(loops, key=reach)

    def _edge(self, ref: Ref, tolerance: float) -> Entity:
        oriented = self.get(ref, "ORIENTED_EDGE")
        edge = self.get(oriented.args[3], "EDGE_CURVE")
        start, end = (Point(*self.point(vertex)[:2]) for vertex in edge.args[1:3])
        curve = self.get(edge.args[3])
        forward = edge.args[4] == Enum("T")
        if curve.type == "LINE":
            return Line(start, end)
        if curve.type == "CIRCLE":
            placement = self.get(curve.args[1])
            center = Point(*self.point(placement.args[1])[:2])
            counterclockwise = (self.direction(placement.args[2])[2] > 0) == forward
            a, b = ((start, end) if counterclockwise else (end, start))
            arc = Arc.around(center, curve.args[2] * self.inches_per_unit, (a - center).angle, (b - center).angle)
            return Arc(start, arc.mid, end)
        if curve.type == "B_SPLINE_CURVE_WITH_KNOTS":
            spline = _BSpline.from_step(self, curve)
            points = spline.fit_points(tolerance)
            if not forward:
                points.reverse()
            # The ends must be exactly the vertices, so the profile closes
            return FitSpline((start, *points[1:-1], end))
        raise StepError(f"Unsupported curve {curve.type} in edge #{oriented.args[3].id}.")


@dataclasses.dataclass
class _BSpline:
    degree: int
    control_points: list[tuple[float, float]]
    knots: list[float]  # With multiplicity

    @classmethod
    def from_step(cls, step: StepFile, curve: Instance) -> _BSpline:
        degree, refs = int(curve.args[1]), curve.args[2]
        multiplicities, knots = curve.args[6], curve.args[7]
        return cls(
            degree,
            [step.point(ref)[:2] for ref in refs],
            [k for k, m in zip(knots, multiplicities) for _ in range(int(m))],
        )

    @property
    def domain(self) -> tuple[float, float]:
        return self.knots[self.degree], self.knots[-self.degree - 1]

    def __call__(self, t: float) -> tuple[float, float]:
        """De Boor's algorithm."""
        p, U, n = self.degree, self.knots, len(self.control_points) - 1
        t = min(max(t, self.domain[0]), self.domain[1])
        k = min(max(bisect.bisect_right(U, t) - 1, p), n)
        d = [list(self.control_points[j + k - p]) for j in range(p + 1)]
        for r in range(1, p + 1):
            for j in range(p, r - 1, -1):
                denominator = U[j + 1 + k - r] - U[j + k - p]
                alpha = 0.0 if denominator == 0 else (t - U[j + k - p]) / denominator
                d[j] = [(1 - alpha) * d[j - 1][c] + alpha * d[j][c] for c in range(2)]
        return d[p][0], d[p][1]

    def fit_points(self, tolerance: float) -> list[Point]:
        """Points along the curve which a cubic spline through them follows to within tolerance.

        Candidates are the curve's knots (where exporters concentrate them around detail), each
        span split in four; points are added where the fit strays furthest until it's close enough.
        """
        low, high = self.domain
        knots = sorted({k for k in self.knots if low <= k <= high})
        candidates = sorted(
            {a + (b - a) * i / 4 for a, b in zip(knots, knots[1:]) for i in range(4)} | {high}
        )
        points = [self(t) for t in candidates]
        step = max(1, len(candidates) // 8)
        selected = sorted(set(range(0, len(candidates), step)) | {len(candidates) - 1})
        while True:
            worst, worst_index = _worst_fit(points, selected)
            if worst <= tolerance or worst_index is None:
                return [Point(*points[index]) for index in selected]
            bisect.insort(selected, worst_index)


def _worst_fit(points: Sequence[tuple[float, float]], selected: list[int]) -> tuple[float, int | None]:
    """How far the unselected points are from a cubic spline through the selected ones."""
    chosen = [points[index] for index in selected]
    lengths = [0.0]
    for a, b in zip(chosen, chosen[1:]):
        lengths.append(lengths[-1] + math.dist(a, b))
    x = _natural_cubic(lengths, [p[0] for p in chosen])
    y = _natural_cubic(lengths, [p[1] for p in chosen])
    worst, worst_index = 0.0, None
    for gap in range(len(selected) - 1):
        start, end = lengths[gap], lengths[gap + 1]
        samples = [(x(start + (end - start) * f / 32), y(start + (end - start) * f / 32)) for f in range(33)]
        for index in range(selected[gap] + 1, selected[gap + 1]):
            distance = min(math.dist(points[index], sample) for sample in samples)
            if distance > worst:
                worst, worst_index = distance, index
    return worst, worst_index


def _natural_cubic(ts: Sequence[float], values: Sequence[float]):
    """A natural cubic spline through (ts[i], values[i]), as a function."""
    n = len(ts) - 1
    if n < 2:
        return lambda t: values[0] + (values[-1] - values[0]) * (t - ts[0]) / ((ts[-1] - ts[0]) or 1)
    h = [ts[i + 1] - ts[i] for i in range(n)]
    alpha = [0.0] + [
        3 / h[i] * (values[i + 1] - values[i]) - 3 / h[i - 1] * (values[i] - values[i - 1])
        for i in range(1, n)
    ]
    l, mu, z = [1.0] + [0.0] * n, [0.0] * (n + 1), [0.0] * (n + 1)
    for i in range(1, n):
        l[i] = 2 * (ts[i + 1] - ts[i - 1]) - h[i - 1] * mu[i - 1]
        mu[i] = h[i] / l[i]
        z[i] = (alpha[i] - h[i - 1] * z[i - 1]) / l[i]
    c, b, d = [0.0] * (n + 1), [0.0] * n, [0.0] * n
    for j in range(n - 1, -1, -1):
        c[j] = z[j] - mu[j] * c[j + 1]
        b[j] = (values[j + 1] - values[j]) / h[j] - h[j] * (c[j + 1] + 2 * c[j]) / 3
        d[j] = (c[j + 1] - c[j]) / (3 * h[j])

    def evaluate(t: float) -> float:
        j = min(max(bisect.bisect_right(ts, t) - 1, 0), n - 1)
        x = t - ts[j]
        return values[j] + b[j] * x + c[j] * x * x + d[j] * x**3

    return evaluate
