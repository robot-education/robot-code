# Testing

```
uv run pytest                                      # fs CLI, Onshape client, language server, evaluator, and FeatureScript tests
cd vscode-extension && npm test                    # TextMate grammar
cd vscode-extension && npm run test:integration    # the extension inside a real VS Code
```

On a headless Linux machine, run the integration test under `xvfb-run -a`.

## Testing FeatureScript

`fs_eval` runs FeatureScript locally: a parser and an interpreter for the language, which runs std's own FeatureScript
(units, vectors, transforms, lookup tables, and so on) on top of the built-ins (`@size`, `@sqrt`, ...) it implements in
Python. So code which computes values can be tested without Onshape: math, lookup tables, editing logic and
manipulator change functions which don't query the Part Studio, and sketches. Nothing which models geometry runs:
operations (`opExtrude`), evaluations (`evDistance`), and queries' results aren't available, and calling them is an
error saying so.

Sketches are recorded rather than solved: each `skArc`, `skLineSegment`, and so on is kept as it's given, and
`fs_eval.sketch` reads a solved sketch back as geometry, so a test can check properties of a profile (that its curves
join into closed loops, meet tangent, and stay within a radius) rather than a picture of it. Constraints aren't
solved, so only sketches whose entities are fully given come out right.

Tests are FeatureScript files in `tests/featurescript` (outside the code folder, so they're never pushed), named
`*_test.fs`: each exported function named `test...` is a test, which fails if it throws. They import what they test by
path (`import(path : "core/loop.fs", version : "");`, and only see what it exports) and `testing.fs` beside them for
checks (`expectEqual`, `expectNear`, `expectThrows`, ...). pytest runs them with the rest:

```
FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "chain/robotChain.fs", version : "");
import(path : "testing.fs", version : "");

export function testSprocketPitchDiameter()
{
    expectNear(2 * getSprocketRadius(0.25 * inch, 16), 1.2815 * inch, 0.0001 * inch);
}
```

Python tests can use the evaluator too (`fs_eval.Evaluator`, or `fs_eval.pytest_plugin.shared_evaluator()` to share
one), to evaluate expressions in a module's own scope (where what it doesn't export is visible), or check recorded
sketches: see `tests/fs_eval_features_test.py` and `tests/fs_eval_sketch_test.py`. `fs eval` evaluates an expression
from the command line, in std's scope or a file's (`-m`).

What's parsed is cached in `.fs-eval-cache`, so loading std takes about half a second after the first run. The
evaluator follows Onshape's semantics where std shows them (maps iterate in key order, enums by ordinal; assigning
undefined to a key removes it; overloads are chosen by their parameters' types), but it's not Onshape: number
formatting, error messages, and built-ins' edge cases may differ, so a passing test is good evidence, not proof.

