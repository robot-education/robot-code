"""Runs FeatureScript tests with pytest: each exported function named `test...` (taking no arguments) in a
`*_test.fs` file is a test, which fails if it throws (see `tests/featurescript/testing.fs` for what to throw with).

Enabled in `tests/conftest.py`, which imports `pytest_collect_file` from here.
"""

from __future__ import annotations

import pathlib

import pytest

from fs_eval import Evaluator, FSError
from fs_eval import ast

_evaluator: Evaluator | None = None


def shared_evaluator() -> Evaluator:
    """One evaluator for every test, so std's loaded once."""
    global _evaluator
    if _evaluator is None:
        _evaluator = Evaluator()
    return _evaluator


def pytest_collect_file(parent, file_path: pathlib.Path):
    if file_path.name.endswith("_test.fs"):
        return FeatureScriptFile.from_parent(parent, path=file_path)
    return None


class FeatureScriptFile(pytest.File):
    def collect(self):
        module = shared_evaluator().module(self.path)
        for decl in module.program.declarations:
            if (type(decl) is ast.FunctionDecl and decl.kind == "function" and decl.exported
                    and decl.name.startswith("test") and not decl.params):
                yield FeatureScriptTest.from_parent(self, name=decl.name, module=module, line=decl.at[1])


class FeatureScriptTest(pytest.Item):
    def __init__(self, *, module, line: int, **kwargs):
        super().__init__(**kwargs)
        self.module = module
        self.line = line

    def runtest(self):
        interpreter = shared_evaluator().interpreter
        function = interpreter_function(self.module, self.name)
        interpreter.call(function, [])

    def repr_failure(self, excinfo):
        if isinstance(excinfo.value, FSError):
            return f"{self.name} failed: {excinfo.value}"
        return super().repr_failure(excinfo)

    def reportinfo(self):
        return self.path, self.line - 1, self.name


def interpreter_function(module, name: str):
    function = module.resolve(name)
    if function is None:
        raise LookupError(name)
    return function
