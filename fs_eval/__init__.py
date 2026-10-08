"""A FeatureScript evaluator: runs FeatureScript locally, for unit tests of code which doesn't model geometry
(math, lookup tables, editing logic, manipulator change functions) and of sketches (which are recorded, not
solved). See `fs_eval.interpreter`, and README's "Testing FeatureScript".

```
evaluator = Evaluator()
loop = evaluator.module("core/loop.fs")
evaluator.eval("getSprocketRadius(0.25 * inch, 16) / inch", "chain/robotChain.fs")
```
"""

from __future__ import annotations

import pathlib

from fs_eval.interpreter import FSError, Interpreter, Module, PythonFunction, to_display
from fs_eval.values import FSArray, FSMap, Tagged, untag

__all__ = ["Evaluator", "FSError", "to_fs", "to_python", "to_display"]


class Evaluator:
    """Runs a repo's FeatureScripts (its code folder, and its copy of std), parsing each file once (cached across
    runs in `.fs-eval-cache`)."""

    def __init__(self, start: pathlib.Path | None = None):
        from fs_cli.config import load_config

        config = load_config(start)
        self.config = config
        self.interpreter = Interpreter(config.std_dir, config.code_dir, config.studios_path,
                                       config.root / ".fs-eval-cache")

    def module(self, path: str | pathlib.Path) -> Module:
        """Loads a module (and everything it imports), by its path in the code folder, or any path."""
        path = pathlib.Path(path)
        if not path.is_absolute():
            in_code = self.config.code_dir / path
            path = in_code if in_code.exists() else self.config.root / path
        return self.interpreter.load(path)

    def source(self, source: str, label: str = "<source>") -> Module:
        """Loads a module from FeatureScript source, as if it were in the code folder."""
        return self.interpreter.load_source(source, label, self.config.code_dir / label)

    def eval(self, expression: str, module: str | pathlib.Path | Module = "onshape/std/common.fs", **variables):
        """Evaluates an expression where `module`'s names are visible (std's, by default), with `variables` (Python
        values, converted with `to_fs`) defined too."""
        if not isinstance(module, Module):
            if str(module).startswith("onshape/std/"):
                module = self.interpreter.load(self.config.std_dir / str(module).removeprefix("onshape/std/"))
            else:
                module = self.module(module)
        from fs_eval.interpreter import Compiler, Scope
        from fs_eval.parser import parse_expression

        scope = Scope()
        for name, value in variables.items():
            scope.vars[name] = to_fs(value)
        return Compiler(module).expression(parse_expression(expression))(scope)

    def call(self, function, *args):
        """Calls a FeatureScript function value with arguments (converted with `to_fs`)."""
        return self.interpreter.call(function, [to_fs(arg) for arg in args])


def to_fs(value):
    """A Python value as FeatureScript's: numbers, strings, booleans, None, lists, and dicts (and FeatureScript
    values, as they are). A Python function becomes a FeatureScript function."""
    if isinstance(value, bool) or value is None or isinstance(value, str):
        return value
    if isinstance(value, (int, float)):
        return float(value)
    if isinstance(value, (list, tuple)):
        return FSArray(tuple(to_fs(item) for item in value))
    if isinstance(value, dict):
        return FSMap.of([(to_fs(k), to_fs(v)) for k, v in value.items()])
    if callable(value) and not hasattr(value, "tag"):
        return PythonFunction(getattr(value, "__name__", "python"), lambda *args: to_fs(value(*args)))
    return value


def to_python(value, unit: str | None = None):
    """A FeatureScript value as Python's: arrays as lists, maps as dicts, and a `ValueWithUnits` as its value in
    std's base units (meters, radians, ...), or with `unit` given (like "inch"), in it."""
    value = untag(value)
    if isinstance(value, FSMap):
        if value.get_str("unit") is not None and isinstance(value.get_str("value"), float):
            number = value.get_str("value")
            return number / _UNITS[unit] if unit else number
        return {to_python(k): to_python(v, unit) for k, v in value.sorted_items()}
    if isinstance(value, FSArray):
        return [to_python(item, unit) for item in value.items]
    return value


_UNITS = {
    "meter": 1.0,
    "centimeter": 0.01,
    "millimeter": 0.001,
    "inch": 0.0254,
    "foot": 0.3048,
    "radian": 1.0,
    "degree": 0.0174532925199432957692,
}
