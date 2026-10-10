"""Every FeatureScript in the repo loads in the evaluator, and its top-level constants (lookup tables, bounds, ...)
evaluate, so any of it can be tested with fs_eval: a feature which uses something the evaluator doesn't support
fails here, rather than when a test is first written for it."""

import pathlib

import pytest

from fs_eval import ast
from fs_eval.pytest_plugin import shared_evaluator

CODE = pathlib.Path(__file__).parent.parent / "featurescripts"
FILES = sorted(path for path in CODE.rglob("*.fs") if ".local." not in path.name)


@pytest.mark.parametrize("path", FILES, ids=lambda path: str(path.relative_to(CODE)))
def test_loads_and_its_constants_evaluate(path):
    module = shared_evaluator().module(path.resolve())
    for declaration in module.program.declarations:
        if type(declaration) is ast.ConstDecl:
            module.resolve(declaration.name)
