# Testing

```
uv run pytest tests/featurescript                  # the FeatureScripts' tests: after changing only FeatureScript
uv run pytest                                      # every test: after changing the tooling (below)
cd vscode-extension && npm test                    # TextMate grammar
cd vscode-extension && npm run test:integration    # the extension inside a real VS Code
```

On a headless Linux machine, run the integration test under `xvfb-run -a`.

## The FeatureScripts' tests, and the tooling's

`tests/featurescript` tests the FeatureScripts in `featurescripts/`: the `*_test.fs` files, the Python tests which
evaluate their code (`fs_eval_*_test.py`), and checks of what `fs gen` generates from their Python definitions
(`generated_test.py`). After changing only FeatureScript, or a lookup table's or profile's definition, run just these.

The rest test the tooling: the fs CLI, the Onshape client, the evaluator, the language server and formatter, and
the dialog preview and `fs audit` page. After changing any of it, run every test, since the FeatureScripts' tests run
on the evaluator.

So that changing a feature never breaks the tooling's tests, they don't depend on what's in the features: they write
the FeatureScript they need to a repo in `tmp_path` (with `std = "..."` in its `[tool.fs]` pointing at the checked-in
std, rather than a copy), or use `featurescripts/uiTestBench/uiTestBench.fs`, a feature with every kind of parameter
which exists for comparing dialogs. The exceptions are corpora: std, which the parser and formatter are run over, and
the shaft and frame features, whose dialogs `live_dialog_test.py` draws (found by name, wherever they are) without
checking what's in them.

pytest runs the tests in parallel (pytest-xdist's `-n auto`, in `pyproject.toml`), a process per core, so tests
mustn't share files they write: use `tmp_path`, and write shared caches (like `.fs-eval-cache`) to a file beside them
and move it into place. `-n0` runs them in one process, for a debugger or `print`s. Parsing std's files is cached in
each process (`fs_lsp.project`'s `_file_modules`, and `fs_cli.ui`'s `_parsed_files`), so a test which makes its own
`Project` costs little more than one which shares one.

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
sketches: see `tests/featurescript/fs_eval_features_test.py` and `tests/featurescript/fs_eval_sketch_test.py`. `fs eval` evaluates an expression
from the command line, in std's scope or a file's (`-m`).

What's parsed is cached in `.fs-eval-cache`, so loading std takes about half a second after the first run. The
evaluator follows Onshape's semantics where std shows them (maps iterate in key order, enums by ordinal; assigning
undefined to a key removes it; overloads are chosen by their parameters' types), but it's not Onshape: number
formatting, error messages, and built-ins' edge cases may differ, so a passing test is good evidence, not proof.

