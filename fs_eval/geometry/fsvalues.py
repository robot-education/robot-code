"""Converting between FeatureScript's values and the plain Python the geometry works in: lengths in meters, angles in
radians, vectors as tuples, and back again (as std's own types, made by std's own constructors, so they're tagged
and checked as FeatureScript's are)."""

from __future__ import annotations

import math

from fs_eval.values import EnumValue, FSArray, FSMap, Tagged, untag

Vec = tuple[float, float, float]


def number(value) -> float:
    """A number, or a `ValueWithUnits` in std's base units (meters, radians, ...)."""
    value = untag(value)
    if type(value) is FSMap:
        inner = untag(value.get_str("value"))
        if inner is None:
            raise ValueError("expected a number or a value with units")
        return float(inner)
    if isinstance(value, (int, float)) and not isinstance(value, bool):
        return float(value)
    raise ValueError(f"expected a number, got {value!r}")


def optional_number(value, default: float | None = None) -> float | None:
    return default if value is None else number(value)


def vector(value) -> tuple[float, ...]:
    value = untag(value)
    if type(value) is not FSArray:
        raise ValueError(f"expected a vector, got {value!r}")
    return tuple(number(item) for item in value.items)


def vec3(value) -> Vec:
    v = vector(value)
    if len(v) != 3:
        raise ValueError(f"expected a 3D vector, got {len(v)} components")
    return v  # type: ignore[return-value]


def field(value, key: str):
    value = untag(value)
    if type(value) is not FSMap:
        raise ValueError(f"expected a map with {key!r}, got {value!r}")
    return value.get_str(key)


def plane(value) -> tuple[Vec, Vec, Vec]:
    """A `Plane` as (origin, normal, x)."""
    origin, normal, x = vec3(field(value, "origin")), vec3(field(value, "normal")), field(value, "x")
    x = vec3(x) if x is not None else perpendicular(normal)
    return origin, unit(normal), unit(x)


def line(value) -> tuple[Vec, Vec]:
    """A `Line` as (origin, direction)."""
    return vec3(field(value, "origin")), unit(vec3(field(value, "direction")))


def coord_system(value) -> tuple[Vec, Vec, Vec]:
    """A `CoordSystem` as (origin, xAxis, zAxis)."""
    return vec3(field(value, "origin")), unit(vec3(field(value, "xAxis"))), unit(vec3(field(value, "zAxis")))


def id_parts(value) -> tuple[str, ...]:
    """An `Id` as its components."""
    value = untag(value)
    if type(value) is not FSArray:
        raise ValueError(f"expected an Id, got {value!r}")
    return tuple(str(untag(item)) for item in value.items)


def enum_name(value) -> str | None:
    value = untag(value)
    if value is None:
        return None
    if type(value) is EnumValue:
        return value.name
    if isinstance(value, str):
        return value
    raise ValueError(f"expected an enum value, got {value!r}")


def enum_names(value) -> list[str]:
    """An enum value, or an array of them, as their names."""
    value = untag(value)
    if type(value) is FSArray:
        return [enum_name(item) for item in value.items]
    name = enum_name(value)
    return [] if name is None else [name]


def boolean(value, default: bool = False) -> bool:
    value = untag(value)
    return default if value is None else bool(value)


# Vector math


def add(a: Vec, b: Vec) -> Vec:
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])


def sub(a: Vec, b: Vec) -> Vec:
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def scale(a: Vec, s: float) -> Vec:
    return (a[0] * s, a[1] * s, a[2] * s)


def dot(a: Vec, b: Vec) -> float:
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def cross(a: Vec, b: Vec) -> Vec:
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def norm(a: Vec) -> float:
    return math.sqrt(dot(a, a))


def unit(a: Vec) -> Vec:
    n = norm(a)
    if n == 0:
        raise ValueError("a zero-length direction")
    return scale(a, 1 / n)


def perpendicular(a: Vec) -> Vec:
    """Some unit vector perpendicular to `a`, as std's `perpendicularVector` picks it."""
    a = unit(a)
    other = (1.0, 0.0, 0.0) if abs(a[0]) < 0.9 else (0.0, 1.0, 0.0)
    return unit(cross(a, other))


# Making FeatureScript values


class Maker:
    """Makes std's typed values (vectors with units, planes, cylinders, ...) with std's own constructors."""

    def __init__(self, interpreter):
        self.interpreter = interpreter
        self._symbols: dict[tuple[str, str], object] = {}

    def symbol(self, file: str, name: str):
        key = (file, name)
        found = self._symbols.get(key)
        if found is None:
            from fs_eval.interpreter import symbol_value

            module = self.interpreter.load(self.interpreter.std_dir / file)
            symbol = module.exported_symbol(name)
            if symbol is None:
                raise ValueError(f"std's {file} doesn't export {name}")
            found = symbol_value(symbol)
            self._symbols[key] = found
        return found

    def call(self, file: str, name: str, *args):
        return self.interpreter.call(self.symbol(file, name), list(args))

    def length(self, meters: float):
        return self.interpreter.binary("*", float(meters), self.symbol("units.fs", "meter"), None)

    def angle(self, radians: float):
        return self.interpreter.binary("*", float(radians), self.symbol("units.fs", "radian"), None)

    def point(self, p: Vec):
        return self.call("vector.fs", "vector", *(self.length(c) for c in p))

    def direction(self, d: Vec):
        return self.call("vector.fs", "vector", *(float(c) for c in d))

    def coord_system(self, origin: Vec, x: Vec, z: Vec):
        return self.call("coordSystem.fs", "coordSystem", self.point(origin), self.direction(x), self.direction(z))

    def plane(self, origin: Vec, normal: Vec, x: Vec):
        return self.call("surfaceGeometry.fs", "plane", self.point(origin), self.direction(normal), self.direction(x))

    def line(self, origin: Vec, direction: Vec):
        return self.call("curveGeometry.fs", "line", self.point(origin), self.direction(direction))

    def transient(self, tid: str):
        return self.call("query.fs", "qTransient", tid)

    def enum(self, file: str, enum: str, member: str):
        return untag(self.symbol(file, enum)).members[member]


def to_plain(value):
    """A FeatureScript value as JSON-able Python, for writing parts' metadata: numbers with units in base units, enums
    by name, colors and materials as their maps."""
    value = untag(value)
    if type(value) is FSMap:
        if value.get_str("unit") is not None and value.get_str("value") is not None:
            return float(untag(value.get_str("value")))
        return {str(to_plain(k)): to_plain(v) for k, v in value.sorted_items()}
    if type(value) is FSArray:
        return [to_plain(item) for item in value.items]
    if type(value) is EnumValue:
        return value.name
    if type(value) is Tagged:
        return to_plain(value.value)
    if isinstance(value, float) and value == int(value) and abs(value) < 1e15:
        return int(value)
    return value
