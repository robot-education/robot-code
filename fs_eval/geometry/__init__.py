"""Geometry for the evaluator: FeatureScript's operations, queries, and evaluations, modeled with OpenCascade (OCP, in
the `render` dependency group: `uv sync --group render`), so FeatureScript-backed parts can be built, measured, and
drawn without Onshape. See docs/rendering.md.

```
evaluator = Evaluator(geometry=True)
part = build_feature(evaluator, "fasteners/fasteners.fs", "fastenerWasher", {"size": "FastenerSize.NO_10_32", ...})
part.bodies  # its solids, with their properties
```

Only what FeatureScript-backed parts use is modeled (see `GEOMETRY_BUILTINS`); anything else is still an error naming
the built-in.
"""

from __future__ import annotations


def available() -> bool:
    """Whether OpenCascade is installed."""
    try:
        import OCP  # noqa: F401
    except ImportError:
        return False
    return True


def __getattr__(name):
    if name == "GEOMETRY_BUILTINS":
        if not available():
            raise ImportError("Modeling geometry needs OpenCascade: run `uv sync --group render`")
        from fs_eval.geometry.builtins import GEOMETRY_BUILTINS

        return GEOMETRY_BUILTINS
    if name in ("build_feature", "BuiltPart", "BuildError"):
        from fs_eval.geometry import build

        return getattr(build, name)
    raise AttributeError(name)
