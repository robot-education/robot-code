"""The built-in functions std calls (`@size`, `@sqrt`, `@skArc`, ...), in Python: each takes the interpreter, its
arguments (FeatureScript values), and where it's called from.

Only those which compute values are here: containers, math, strings, matrices. Built-ins which model geometry
(`@opExtrude`, `@evDistance`, ...) aren't, so calling them is an error. Sketches are recorded, not solved:
`@newSketch` makes a `Sketch` which the `@sk...` built-ins add their entities to, as they're given (a circle's center
and radius, an arc's start, middle, and end, ...), and `@skSolve` files it in its context's `sketches`, by its id.
Constraints are recorded too, but not solved, so only sketches whose entities are fully given come out right.
"""

from __future__ import annotations

import json
import math
import re

from fs_eval.values import (
    Box,
    Builtin,
    Context,
    EnumValue,
    FSArray,
    FSMap,
    FSType,
    Tagged,
    equal,
    key_of,
    untag,
)


def _error(message: str, at):
    from fs_eval.interpreter import error

    return error(message, at)


class Sketch(Builtin):
    """A sketch, as recorded (see the module's description)."""

    def __init__(self, context: Context, id, value: FSMap):
        self.tag: FSType | None = None
        self.context = context
        self.id = id
        self.plane = value.get_str("sketchPlane")
        self.entities: list[dict] = []
        self.constraints: list[dict] = []
        self.solved = False

    def __repr__(self) -> str:
        return f"<Sketch {self.id!r}>"


def _number(value, at, name="argument") -> float:
    value = untag(value)
    if type(value) is not float:
        raise _error(f"The {name} must be a number", at)
    return value


def _array(value, at) -> FSArray:
    value = untag(value)
    if type(value) is not FSArray:
        raise _error("Expected an array", at)
    return value


def _map(value, at) -> FSMap:
    value = untag(value)
    if type(value) is not FSMap:
        raise _error("Expected a map", at)
    return value


def _string(value, at) -> str:
    value = untag(value)
    if type(value) is not str:
        raise _error("Expected a string", at)
    return value


def _index(value, at) -> int:
    number = _number(value, at, "index")
    if number != int(number):
        raise _error("An index must be a whole number", at)
    return int(number)


def _float_list(value, at) -> list:
    return [_number(item, at) for item in _array(value, at).items]


def _matrix(value, at) -> list[list[float]]:
    return [_float_list(row, at) for row in _array(value, at).items]


def _from_matrix(rows) -> FSArray:
    return FSArray(tuple(FSArray(tuple(float(x) for x in row)) for row in rows))


# Containers


def b_size(interp, args, at):
    value = untag(args[0])
    if type(value) in (FSArray, FSMap, str):
        return float(len(value) if type(value) is not FSMap else len(value.entries))
    raise _error("@size of something without a size", at)


def b_resize(interp, args, at):
    array = _array(args[0], at)
    size = _index(args[1], at)
    fill = args[2] if len(args) > 2 else None
    items = array.items[:size] + (fill,) * max(0, size - len(array.items))
    return FSArray(items, array.tag)


def b_subArray(interp, args, at):
    array = _array(args[0], at)
    start = _index(args[1], at)
    end = _index(args[2], at) if len(args) > 2 else len(array.items)
    if start < 0 or end > len(array.items) or start > end:
        raise _error("@subArray's range is out of the array", at)
    return FSArray(array.items[start:end])


def b_concatenateArrays(interp, args, at):
    items = []
    for array in _array(args[0], at).items:
        items.extend(_array(array, at).items)
    return FSArray(tuple(items))


def b_indexOf(interp, args, at):
    array = _array(args[0], at)
    start = _index(args[2], at) if len(args) > 2 else 0
    for i in range(start, len(array.items)):
        if equal(array.items[i], args[1]):
            return float(i)
    return -1.0


def b_keys(interp, args, at):
    return FSArray(tuple(key for key, _ in _map(args[0], at).sorted_items()))


def b_values(interp, args, at):
    return FSArray(tuple(value for _, value in _map(args[0], at).sorted_items()))


def b_mergeMaps(interp, args, at):
    first = _map(args[0], at)
    second = _map(args[1], at)
    entries = dict(first.entries)
    entries.update(second.entries)
    return FSMap(entries, first.tag)


def b_intersectMaps(interp, args, at):
    maps = [_map(m, at) for m in _array(args[0], at).items]
    if not maps:
        return FSMap()
    entries = {k: v for k, v in maps[0].entries.items() if all(k in m.entries for m in maps[1:])}
    return FSMap(entries)


def b_reverse(interp, args, at):
    array = _array(args[0], at)
    return FSArray(tuple(reversed(array.items)), array.tag)


def b_range(interp, args, at):
    start, end, count = args[0], args[1], _index(args[2], at)
    if count < 1:
        raise _error("@range needs a positive count", at)
    if count == 1:
        return FSArray((start,))
    span = interp.binary("-", end, start, at)
    items = []
    for i in range(count):
        step = interp.binary("*", span, float(i) / (count - 1), at)
        items.append(interp.binary("+", start, step, at))
    return FSArray(tuple(items))


def b_tolerantSort(interp, args, at):
    values = _float_list(args[0], at)
    tolerance = _number(args[1], at)
    order = sorted(range(len(values)), key=lambda i: values[i])
    return FSArray(tuple(float(i) for i in order)) if tolerance >= 0 else FSArray()


# Math


def _unary_math(function):
    def builtin(interp, args, at):
        try:
            return float(function(_number(args[0], at)))
        except (ValueError, OverflowError):
            raise _error(f"Can't evaluate {function.__name__} of {args[0]}", at)

    return builtin


def b_hypot(interp, args, at):
    return math.hypot(_number(args[0], at), _number(args[1], at))


def b_atan2(interp, args, at):
    return math.atan2(_number(args[0], at), _number(args[1], at))


def _number_or_value(value, at) -> float:
    """A number, or a `ValueWithUnits`'s value."""
    value = untag(value)
    if type(value) is FSMap:
        value = untag(value.get_str("value"))
    return _number(value, at)


def b_normalize(interp, args, at):
    values = [_number_or_value(item, at) for item in _array(args[0], at).items]
    norm = math.sqrt(sum(x * x for x in values))
    if norm == 0:
        raise _error("Can't normalize a zero vector", at)
    return FSArray(tuple(x / norm for x in values))


def b_isMatrix(interp, args, at):
    value = untag(args[0])
    if type(value) is not FSArray or not value.items:
        return False
    width = None
    for row in value.items:
        row = untag(row)
        if type(row) is not FSArray or not row.items or any(type(untag(x)) is not float for x in row.items):
            return False
        if width is None:
            width = len(row.items)
        elif len(row.items) != width:
            return False
    return True


def b_matrixIdentity(interp, args, at):
    size = _index(args[0], at)
    return _from_matrix([[1.0 if i == j else 0.0 for j in range(size)] for i in range(size)])


def b_matrixMultiply(interp, args, at):
    left = untag(args[0])
    right = untag(args[1])
    if type(right) is float:
        return _from_matrix([[x * right for x in row] for row in _matrix(left, at)])
    a = _matrix(left, at)
    right_items = _array(right, at).items
    if right_items and type(untag(right_items[0])) is float:
        vector = _float_list(right, at)
        return FSArray(tuple(sum(row[j] * vector[j] for j in range(len(vector))) for row in a))
    b = _matrix(right, at)
    return _from_matrix([[sum(a[i][k] * b[k][j] for k in range(len(b))) for j in range(len(b[0]))] for i in range(len(a))])


def _elementwise(function):
    def builtin(interp, args, at):
        a = _matrix(args[0], at)
        b = _matrix(args[1], at)
        return _from_matrix([[function(x, y) for x, y in zip(ra, rb)] for ra, rb in zip(a, b)])

    return builtin


def b_matrixNegate(interp, args, at):
    return _from_matrix([[-x for x in row] for row in _matrix(args[0], at)])


def b_matrixTranspose(interp, args, at):
    return _from_matrix([list(column) for column in zip(*_matrix(args[0], at))])


def b_matrixSquaredNorm(interp, args, at):
    return sum(x * x for row in _matrix(args[0], at) for x in row)


def _determinant(m: list[list[float]]) -> float:
    n = len(m)
    m = [row[:] for row in m]
    det = 1.0
    for i in range(n):
        pivot = max(range(i, n), key=lambda r: abs(m[r][i]))
        if m[pivot][i] == 0:
            return 0.0
        if pivot != i:
            m[i], m[pivot] = m[pivot], m[i]
            det = -det
        det *= m[i][i]
        for r in range(i + 1, n):
            factor = m[r][i] / m[i][i]
            for c in range(i, n):
                m[r][c] -= factor * m[i][c]
    return det


def b_matrixDeterminant(interp, args, at):
    return _determinant(_matrix(args[0], at))


def b_matrixInverse(interp, args, at):
    m = _matrix(args[0], at)
    n = len(m)
    augmented = [row[:] + [1.0 if i == j else 0.0 for j in range(n)] for i, row in enumerate(m)]
    for i in range(n):
        pivot = max(range(i, n), key=lambda r: abs(augmented[r][i]))
        if abs(augmented[pivot][i]) < 1e-300:
            raise _error("The matrix can't be inverted", at)
        augmented[i], augmented[pivot] = augmented[pivot], augmented[i]
        scale = augmented[i][i]
        augmented[i] = [x / scale for x in augmented[i]]
        for r in range(n):
            if r != i:
                factor = augmented[r][i]
                augmented[r] = [x - factor * y for x, y in zip(augmented[r], augmented[i])]
    return _from_matrix([row[n:] for row in augmented])


# Strings


def b_stringToNumber(interp, args, at):
    text = _string(args[0], at).strip()
    try:
        return float(text)
    except ValueError:
        raise _error(f"{text!r} isn't a number", at)


def b_length(interp, args, at):
    return float(len(_string(args[0], at)))


def b_substring(interp, args, at):
    text = _string(args[0], at)
    start = _index(args[1], at)
    end = _index(args[2], at) if len(args) > 2 else len(text)
    return text[start:end]


def b_startsWith(interp, args, at):
    return _string(args[0], at).startswith(_string(args[1], at))


def b_endsWith(interp, args, at):
    return _string(args[0], at).endswith(_string(args[1], at))


def b_splitIntoCharacters(interp, args, at):
    return FSArray(tuple(_string(args[0], at)))


def _regex(pattern: str):
    # Java's syntax, mostly Python's too; named groups differ
    return re.compile(re.sub(r"\(\?<([A-Za-z][A-Za-z0-9]*)>", r"(?P<\1>", pattern))


def b_splitByRegexp(interp, args, at):
    parts = _regex(_string(args[1], at)).split(_string(args[0], at))
    # Like Java's split, without trailing empty strings
    while parts and parts[-1] == "":
        parts.pop()
    return FSArray(tuple(parts))


def b_indexOfString(interp, args, at):
    start = _index(args[2], at) if len(args) > 2 else 0
    return float(_string(args[0], at).find(_string(args[1], at), start))


def b_indexOfRegexp(interp, args, at):
    start = _index(args[2], at) if len(args) > 2 else 0
    found = _regex(_string(args[1], at)).search(_string(args[0], at), start)
    return float(found.start()) if found else -1.0


def b_repeatString(interp, args, at):
    return _string(args[0], at) * _index(args[1], at)


def b_match(interp, args, at):
    text = _string(args[0], at)
    # The whole string must match, as with Java's `matches`
    found = _regex(_string(args[1], at)).fullmatch(text)
    if found is None:
        return FSMap.of([("hasMatch", False), ("captures", FSArray())])
    captures = (found.group(0),) + tuple(group if group is not None else "" for group in found.groups())
    return FSMap.of([("hasMatch", True), ("captures", FSArray(captures))])


def b_replace(interp, args, at):
    replacement = _string(args[2], at)

    def expand(found):
        # $n is a group; anything else is literal (std passes regexes, backslashes and all, as replacements)
        out = []
        i = 0
        while i < len(replacement):
            char = replacement[i]
            if char == "$" and i + 1 < len(replacement) and replacement[i + 1].isdigit():
                out.append(found.group(int(replacement[i + 1])) or "")
                i += 2
            else:
                out.append(char)
                i += 1
        return "".join(out)

    return _regex(_string(args[1], at)).sub(expand, _string(args[0], at))


def b_parseJson(interp, args, at):
    def convert(value):
        if isinstance(value, bool) or value is None or isinstance(value, str):
            return value
        if isinstance(value, (int, float)):
            return float(value)
        if isinstance(value, list):
            return FSArray(tuple(convert(item) for item in value))
        return FSMap.of([(k, convert(v)) for k, v in value.items()])

    return convert(json.loads(_string(args[0], at)))


def b_print(interp, args, at):
    interp.output.append(_string(args[0], at))
    return None


# Contexts and sketches


def b_isContext(interp, args, at):
    return type(untag(args[0])) is Context


def b_newContext(interp, args, at):
    return Context(args[0] if args else None)


def b_isAtVersionOrLater(interp, args, at):
    return True


def b_getCurrentVersion(interp, args, at):
    return untag(args[0]).version


def b_isInSheetMetalFeature(interp, args, at):
    return False


def b_newSketch(interp, args, at):
    context = untag(args[0])
    return Sketch(context, args[1], _map(args[2], at))


def b_isSketch(interp, args, at):
    return type(untag(args[0])) is Sketch


def _sketch_entity(kind):
    def builtin(interp, args, at):
        sketch = untag(args[0])
        if type(sketch) is not Sketch:
            raise _error("Expected a sketch", at)
        sketch.entities.append({"type": kind, "id": _string(args[1], at), "value": _map(args[2], at)})
        return None

    return builtin


def b_skConstraint(interp, args, at):
    sketch = untag(args[0])
    sketch.constraints.append({"id": _string(args[1], at), "value": _map(args[2], at)})
    return None


def b_skSolve(interp, args, at):
    sketch = untag(args[0])
    sketch.solved = True
    sketch.context.sketches[key_of(sketch.id)] = sketch
    return None


def b_noop(interp, args, at):
    return None


def b_setVariable(interp, args, at):
    untag(args[0]).variables[_string(args[1], at)] = args[2]
    return None


def b_getVariable(interp, args, at):
    variables = untag(args[0]).variables
    name = _string(args[1], at)
    if name not in variables:
        if len(args) > 2:
            return args[2]
        raise _error(f"The variable {name} isn't set", at)
    return variables[name]


BUILTINS = {
    # Containers
    "size": b_size,
    "resize": b_resize,
    "subArray": b_subArray,
    "concatenateArrays": b_concatenateArrays,
    "indexOf": b_indexOf,
    "keys": b_keys,
    "values": b_values,
    "mergeMaps": b_mergeMaps,
    "intersectMaps": b_intersectMaps,
    "reverse": b_reverse,
    "range": b_range,
    "tolerantSort": b_tolerantSort,
    # Math
    "floor": _unary_math(math.floor),
    "ceil": _unary_math(math.ceil),
    "sqrt": _unary_math(math.sqrt),
    "log": _unary_math(math.log),
    "log10": _unary_math(math.log10),
    "exp": _unary_math(math.exp),
    "exp2": _unary_math(lambda x: 2.0 ** x),
    "sin": _unary_math(math.sin),
    "cos": _unary_math(math.cos),
    "tan": _unary_math(math.tan),
    "asin": _unary_math(math.asin),
    "acos": _unary_math(math.acos),
    "atan": _unary_math(math.atan),
    "sinh": _unary_math(math.sinh),
    "cosh": _unary_math(math.cosh),
    "tanh": _unary_math(math.tanh),
    "asinh": _unary_math(math.asinh),
    "acosh": _unary_math(math.acosh),
    "atanh": _unary_math(math.atanh),
    "hypot": b_hypot,
    "atan2": b_atan2,
    "normalize": b_normalize,
    # Matrices
    "isMatrix": b_isMatrix,
    "matrixIdentity": b_matrixIdentity,
    "matrixMultiply": b_matrixMultiply,
    "matrixSum": _elementwise(lambda x, y: x + y),
    "matrixDifference": _elementwise(lambda x, y: x - y),
    "matrixCwiseProduct": _elementwise(lambda x, y: x * y),
    "matrixNegate": b_matrixNegate,
    "matrixTranspose": b_matrixTranspose,
    "matrixSquaredNorm": b_matrixSquaredNorm,
    "matrixDeterminant": b_matrixDeterminant,
    "matrixInverse": b_matrixInverse,
    # Strings
    "stringToNumber": b_stringToNumber,
    "length": b_length,
    "substring": b_substring,
    "startsWith": b_startsWith,
    "endsWith": b_endsWith,
    "splitIntoCharacters": b_splitIntoCharacters,
    "splitByRegexp": b_splitByRegexp,
    "indexOfString": b_indexOfString,
    "indexOfRegexp": b_indexOfRegexp,
    "repeatString": b_repeatString,
    "match": b_match,
    "replace": b_replace,
    "parseJson": b_parseJson,
    "print": b_print,
    # Contexts
    "isContext": b_isContext,
    "newContext": b_newContext,
    "isAtVersionOrLater": b_isAtVersionOrLater,
    "getCurrentVersion": b_getCurrentVersion,
    "isInSheetMetalFeature": b_isInSheetMetalFeature,
    "setVariable": b_setVariable,
    "getVariable": b_getVariable,
    "setExternalDisambiguation": b_noop,
    "addDebugEntities": b_noop,
    # Sketches
    "newSketch": b_newSketch,
    "isSketch": b_isSketch,
    "skPoint": _sketch_entity("point"),
    "skLineSegment": _sketch_entity("line"),
    "skCircle": _sketch_entity("circle"),
    "skEllipse": _sketch_entity("ellipse"),
    "skArc": _sketch_entity("arc"),
    "skEllipticalArc": _sketch_entity("ellipticalArc"),
    "skFitSpline": _sketch_entity("fitSpline"),
    "skSpline": _sketch_entity("spline"),
    "skSplineSegment": _sketch_entity("splineSegment"),
    "skInterpolatedSpline": _sketch_entity("interpolatedSpline"),
    "skBezier": _sketch_entity("bezier"),
    "skText": _sketch_entity("text"),
    "skConstraint": b_skConstraint,
    "skSetInitialGuess": b_noop,
    "skSolve": b_skSolve,
}
