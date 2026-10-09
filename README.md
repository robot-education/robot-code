# Robot Code

The source of truth for my Onshape FeatureScripts. Every FeatureScript lives in the Robot backend document in
Onshape, and is checked in here as a `.fs` file. They're edited locally in VS Code (with real language support),
pushed to the backend document with the `fs` command, and released to the public frontend document with
`fs release`.

The Robot Manager Onshape app previously lived here; its final state is preserved at the
[`robot-manager-final`](https://github.com/robot-education/robot-code/tree/robot-manager-final) tag.

## Layout

| Path                       | What it is                                                                   |
| -------------------------- | ---------------------------------------------------------------------------- |
| `featurescripts/`          | The backend document's Feature Studios, plus the Python lookup table sources |
| `featurescripts/released/` | Released FeatureScripts (see [Releasing](#releasing)), each with its own files |
| `featurescripts/core/`     | Modules shared between FeatureScripts                                        |
| `fs_cli/`                  | The `fs` command                                                             |
| `onshape_api/`             | A small Onshape REST API client; see [its README](onshape_api/README.md)     |
| `std/`                     | A read-only copy of the Onshape std library, for reference                   |
| `onshape_icons/`           | Onshape's UI icons, for `fs ui`; browse them with its `index.html`           |
| `docs/`                    | Conventions for writing FeatureScripts and their writeups, and COTS research |
| `vscode-extension/`        | The VS Code extension (TypeScript client, grammar, snippets)                 |
| `vscode-extension/server/` | The Python FeatureScript language server the extension runs (`fs_lsp`)       |
| `pyproject.toml`           | Python dependencies, plus the `[tool.fs]` table configuring the documents    |

# Setup

1. Install [uv](https://github.com/astral-sh/uv), then install Python and the dependencies:

    ```
    curl -LsSf https://astral.sh/uv/install.sh | sh
    uv python install 3.12
    uv sync
    ```

2. Create an API key in the [Onshape developer portal](https://dev-portal.onshape.com/keys) and add it to a
   `.env` file in the root of the repo (it's gitignored):

    ```
    API_ACCESS_KEY=<Your API Access Key>
    API_SECRET_KEY=<Your API Secret Key>

    # Optional
    API_LOGGING=false            # Log every request
    API_BASE_URL=https://cad.onshape.com
    ```

    The same variables can be set directly in the environment instead (e.g. in CI).

3. Install the VS Code extension (requires Node.js), then reload VS Code:

    ```
    cd vscode-extension
    npm ci
    npm run install-extension
    ```

# The `fs` command

## Pushing and pulling

`[tool.fs]` in `pyproject.toml` points `fs` at the backend document, and every Feature Studio in it corresponds
to a file in `featurescripts/`. The local folder structure is yours: once a file is tracked you can move or rename
it freely, and the folders in Onshape are ignored. The only time Onshape's folders matter is when a studio is
pulled into the repo for the first time, e.g. a studio named `robotFrame.fs` in the document's `Robot` folder
lands at `featurescripts/Robot/robotFrame.fs`.

### Renaming and moving files

A file stays synced with its Feature Studio through `fs-studios.json`, which maps the studio's element id to the
file. A rename that `fs` doesn't follow looks like a deleted file and a new one, and pushing that would delete the
tab and create another, breaking every document that imports it (imports are by element id). So:

- Renaming or moving files and folders in VS Code updates `fs-studios.json` right away, and updates imports of them
  by path (`import(path : "core/utils.fs", version : "")`), through the language server.
- Elsewhere, use `uv run fs mv OLD NEW`, which does the same (and also records a move you've already made).
- Otherwise `fs` follows a missing file to the one new file of the same name, or with the same contents it was last
  synced with, or most like its last committed contents (as git detects renames). If it still can't tell, `fs push`
  says so before asking to delete the tab.

Renaming a file doesn't rename its tab in Onshape; `fs tabs --rename` does (see below).

### Matching tabs with files by hand

When `fs` pairs a tab with the wrong file, or can't pair them at all (e.g. a file renamed and rewritten outside
`fs`), match them up yourself:

```
uv run fs tabs               # tabs without files, files without tabs, and tabs named differently from their files
uv run fs link featurescripts/core/tube.fs robotTube.fs   # sync a file with a tab (by name or element id)
uv run fs unlink featurescripts/old.fs   # stop syncing a file with its tab, leaving the tab alone
uv run fs tabs --rename      # rename tabs to match their files' names (asks first)
```

`fs link` doesn't change either side: run `fs status` afterwards to see how they differ. Released tabs (see
[Releasing](#releasing)) are never renamed.

The repo is the source of truth, so pushing is the default:

```
uv run fs                    # same as `fs push`: push every local change
uv run fs push robotFrame    # push one FeatureScript, by name...
uv run fs push featurescripts/Robot   # ...or by file or folder
uv run fs push --dry-run     # show what would be pushed
```

Commands which sync or change `fs-studios.json` hold a lock (`.fs.lock`) while they run, so two at once (from two
terminals, or two agents) don't lose each other's changes, like the element ids of tabs a push creates; the second
waits for the first.

Close a Feature Studio's tab in Onshape before pushing it, or reload the tab after: Onshape merges changes made
through the API into an open editor's text, and can garble it (an element id glued to the front of an import has been
seen), and editing in that tab would save the garbled text.

New files become new Feature Studios at the top level of the document (the API can't create folders); move the
tabs in Onshape if you like, `fs` won't care. The API doesn't report compile errors, so run `fs check` first (see
below) and check new code in Onshape.

Studios import each other by element id, which a new file doesn't have until it's pushed. Import it by its path in
`featurescripts/` instead, and `fs push` resolves it (in the local file too) once the studio exists:

```
import(path : "core/myNewUtils.fs", version : "");
```

FeatureScripts named `*.local.fs`, like another lighten feature kept for reference, stay in the repo too: they
aren't synced with tabs, and `fs check` and `fs format` skip them unless they're named (`fs check
featurescripts/lighten/partLighten.local.fs`).

### Images

Images (SVGs and PNGs) in `featurescripts/` are synced with image tabs in the backend document the same way, so
icons live in the repo too: `fs push` uploads new and changed ones, and `fs pull` downloads ones changed in Onshape
(placing new ones beside the files which import them). Images named `*.local.png` (or `*.local.svg`), like screenshots
for reference, stay in the repo and aren't synced. Studios import them like other tabs, by element id, and use
their `BLOB_DATA`:

```
RobotIcon::import(path : "233760ca14ddd2085de9c219", version : "f7c25b250b6b31d37d4e95ff");

annotation { "Feature Type Name" : "Robot grid", "Icon" : RobotIcon::BLOB_DATA }
```

A new image doesn't have an element id until it's pushed, so import it by its path, and `fs push` uploads it and
resolves the import:

```
GridIcon::import(path : "grid/gridIcon.svg", version : "");
```

When an image changes, `fs push` also points the imports of it at its new version, and pushes the studios importing
it. `fs-studios.json` maps image tabs to files under `images`; `fs mv` and `fs status` treat images like studios.
`fs tabs`, `fs link`, and `fs unlink` are for studios only.

For the occasional edit made directly in Onshape:

```
uv run fs status             # what differs, and what to do about it
uv run fs diff               # unified diff of Onshape vs. the repo
uv run fs pull               # bring Onshape changes into the repo
uv run fs sync               # pull Onshape-only changes and push local-only changes
uv run fs update-std --push  # bump `FeatureScript NNNN;` and std imports to the std version in std/
```

Every command takes the same kinds of targets as `fs push`, and `--help` lists each one's options.

### How conflicts are detected

`fs` never silently overwrites work. For each Feature Studio it compares the local file, the Feature Studio in
Onshape, and what the studio looked like the last time it was pushed or pulled. That last-synced state is kept
per machine in `.fs-state.json` (gitignored). On a fresh clone, Onshape's contents are instead compared
against the file's recent git history: if Onshape matches a committed version, local edits are safe to push.

Which file each Feature Studio is synced with is kept in `fs-studios.json`, which is checked in. Commit it when
`fs` changes it (when studios are added, deleted, or moved).

- Only the local file changed: `fs push` updates Onshape.
- Only Onshape changed: `fs push` skips it and suggests `fs pull`.
- Both changed: both commands skip it; inspect with `fs diff`, then pick a side with `fs pull --force` or
  `fs push --force`.

Changes to the `version` of imports of other tabs in the document (`import(path : "<element id>", version :
"...")`) are never treated as edits, since Onshape manages them itself and may update them when the imported tab
changes. When Onshape's are newer, `fs push`, `fs pull`, and `fs sync` copy them into the local file, and
`fs push` never sends older ones.

Files are matched to Feature Studios by element id once synced, so renaming or moving either the file or the tab
doesn't break the link. (On a fresh clone, studios are matched to the local file with the same name.)

Deleting a file and running `fs push` (or `fs sync`) deletes its tab in Onshape, after asking for confirmation
(`--yes` skips it). It's skipped if the tab changed in Onshape since it was last synced (`fs push --force` deletes
it anyway), or if another file still imports it. `fs pull --force` restores a deleted file instead. Only deletions
`fs` has seen are tracked: on a fresh clone, a tab with no file is just pulled. A tab deleted in Onshape is
recreated by `fs push` unless you delete the file. Released FeatureScripts are the exception: their tabs are never
deleted or recreated (see [Releasing](#releasing)).

### API usage

Onshape limits API calls per year (2,500 per user on Standard/Free plans; see
[API limits](https://onshape-public.github.io/docs/auth/limits/)), so `fs` keeps them to a minimum. Requests
Onshape answers without credentials for public documents (listing tabs, downloading images, and reading a tab's
metadata) are made anonymously, so they don't count (if a document isn't public, they fall back to signed calls,
which do); see [the Onshape API](onshape_api/README.md#anonymous-requests). The counts below are of the calls which
count:

- Listing the backend document's tabs: 1 anonymous call. Every command that talks to Onshape does this first, to
  get each tab's microversion.
- A studio is only downloaded when its microversion changed since the last sync, so on a new machine the first
  run downloads each studio once. A changed microversion doesn't always mean changed code (e.g. it may change
  when a tab it imports changes); then the download just confirms nothing needs doing.
- Pushing: 1 call per studio pushed or deleted, plus an anonymous one to list the document's new microversions
  afterwards. Pushing a studio makes Onshape update the versions of its imports in the studios importing it, changing their
  microversions too; `fs` records those from the same listing rather than downloading them later (assuming nobody
  edited them in Onshape during the push).
- Pulling: 1 call per studio pulled, plus 1 to look up folders when a studio is new to the repo.
- Images: 1 anonymous call to list them (which `fs push` skips when the repo has none), 1 anonymous call per image
  downloaded (only when its microversion changed), and 1 per image uploaded, plus 1 anonymous call to list their
  new microversions afterwards.
- `fs tabs` and `fs link`: just the anonymous listing. `fs tabs --rename`: 1 more per tab renamed (plus an anonymous
  one to read its metadata).
- `fs release`: at most 6 to 8 calls (fewer now that listings are anonymous). `fs released --detect`: 2.
  `fs deprecate`: about 12.

So `fs status`, and `fs push` or `fs pull` with nothing to do, cost nothing: they only make the anonymous listings.
Failed calls (like a 400) don't count. Commands that only read the repo (`fs check`, `fs gen`, and the others below) make no calls.

## Checking and navigating

Conventions for writing FeatureScripts (UI state predicates, where horizontal enums go, and so on) are in
[docs/featurescript-style.md](docs/featurescript-style.md).

These read the repo only (no API calls). Imports between studios are resolved through `fs-studios.json`.

```
uv run fs check              # syntax errors, undefined names, unused or unknown imports, and more
uv run fs check featurescripts/released/belt   # ...or just some files or folders
uv run fs format             # indentation and spacing like std's (--check: list what it would change)
uv run fs deps robotShaft    # what a studio imports, and what imports it
uv run fs strings robotShaft # strings it shows users (names, descriptions, errors), with those of what it imports
uv run fs refs cleanup       # where a function, constant, enum, etc. is defined and used (std's too)
uv run fs mv featurescripts/a.fs featurescripts/core/b.fs   # rename or move, keeping its studio and imports
uv run fs unused             # exports nothing uses (--local: also those only their own file uses)
uv run fs ui featurescripts/nutStrip/robotNutStrip.fs --set placement=POINT   # screenshot a feature's dialog
uv run fs eval "getSprocketRadius(0.25 * inch, 16) / inch" -m chain/robotChain.fs   # evaluate an expression (see below)
uv run fs table featurescripts/released/shaft -n frcShaft   # lookup tables as rows (--md, or --html page.html)
uv run fs audit featurescripts/lighten/robotLighten.fs   # one page: its working dialog, writeup, and problems
uv run fs cots 'hex shaft' -d   # how often teams use COTS parts, from FRCDesign (see docs/cots-research.md)
uv run fs step REV-21-2162.STEP featurescripts/frame/vendor/REV-21-2162.STEP   # keep only a vendor STEP file's cross section
```

`fs table` shows lookup tables as the evaluator builds them (generated or written by hand): a row per path through the
options, with each level's default marked, the leaf's values in readable units (lengths in inches if they're round in
them, else millimeters), and the parameters which use the table (their `"Lookup Table"` annotations). `--html` writes
one page of every table, with a filter, to audit them in a browser.

`fs audit` writes one page (`.fs-audit/<feature>.html`, or `-o`) to audit a feature by: its dialog, which works,
the lookup tables its parameters use (every option at once, without their values), its writeup, and its file's
`fs check` problems. The page is static, so the dialog's states are rendered ahead of time:
from its defaults, each choice a click could make (a dropdown option, checkbox, tab, or lookup table level) is
rendered in turn, choices which show or hide parameters first, up to `--max-states` (200). Clicking swaps between them,
and hovering a parameter (or by touch, a long press) shows its description, default, and UI hints, as the VS Code
preview does. Values typed into
fields aren't rendered (`fs ui --set` renders any state).

`fs ui` renders a feature's dialog as Onshape shows it, from its precondition: parameters take their defaults (or the
values given with `--set`), predicates are inlined, and `if`s are decided the way Onshape decides them. Editing logic
doesn't run, so values it would set (like Robot nut strip's end offsets) show their defaults. The dialog is written with
Onshape's own markup and styled with Onshape's own styles and icons (in its dark theme, or `--theme light`), so it looks
the same, down to the layout of rows. It needs Chromium, which it finds in Playwright's browsers folder, on the path,
or through `CHROMIUM`.

The styles and icons are in `fs_cli/onshape_ui/`, extracted from a capture of Onshape with UI test bench's dialog open
(`featurescripts/uiTestBench/uiTestBench.html`): the rules which apply to the dialog's elements, and the theme variables
they use. To update them, capture the dialog again (below), save it over that file, and run `uv run --group onshape-ui
python -m fs_cli.onshape_ui.extract featurescripts/uiTestBench/uiTestBench.html`.

Arrays start empty, as in a new feature; `--set holes=2` shows two items, and `--set holes.1.depth=2in` sets the second
item's depth. `featurescripts/uiTestBench/uiTestBench.fs` has one
of every kind of parameter and UI hint (and groups nested in groups), for comparing `fs ui` with Onshape (`uv run fs ui
featurescripts/uiTestBench/uiTestBench.fs --set items=2`). To capture how Onshape draws a dialog, open it in Onshape and
paste `fs_cli/ui_capture.js` into DevTools' console: it downloads the dialog as a self-contained HTML file, with what's
typed and checked in it, the CSS rules which apply to it, and the icons it uses. It only reads the page.

`fs format` formats like the std library (see `vscode-extension/server/fs_lsp/formatter.py`): it keeps line breaks,
indents blocks (Allman style, 4 spaces), keeps continuation lines where they are relative to their statement's first
line (std indents those several ways), and fixes spacing (`f(a, b)`, `if (x)`, `a == b`, `{ "key" : value }`). Only
whitespace changes; generated `.gen.fs` files are left to `fs gen`. Formatting std's own files changes about 2% of
their lines, mostly where std's indentation is inconsistent. `--check` exits with 1 if anything would change.

`fs check` exits with 1 if it finds anything. A comment on a problem's line, or alone on the line before it, ignores it:
`// fs check: ignore keyword-key (std's hole attributes name it "type")`, with the problem's code (in brackets after
its message) and why. Undefined names are checked against the file, everything it imports
(following `export import`), and the std library. Imports of other documents' Part Studios or Feature Studios (by
`document id/version id/element id`, like Robot motor's of FRCDesign's Block Motor) aren't checked: nothing here can
read them, and `fs push` leaves them as they are. It also reports enums used as a feature's parameter types (directly or through predicates)
which the feature's file doesn't export, as Onshape requires (std enums too: `export import` the std module declaring
one, not `common.fs`, which it reports exporting), top-level constants, enums, and types whose names the file or its
imports already declare, functions and predicates declared with the same name and parameter types as another in the file
or its imports (overloads Onshape can't choose between), groups in array parameters' items, parameters a feature's precondition declares more than once
(directly or through predicates, even in different branches of an if), and predicates in a precondition's `if`
conditions which call other predicates (Onshape doesn't inline those). It warns about parameters which can be toleranced (`CAN_BE_TOLERANT`, ours never are; directly or through std's predicates), comparisons with `true` or `false`, local variables which are set but never used (name one you don't need `_`), map keys which are keywords (like `"type"`, which can't be read as `.type`; reading one that way is an error),
precondition conditions Onshape can't evaluate (only parameters, enum values, literals, and predicates work), top-level
declarations which aren't exported or used anywhere, and map keys written as bare names which are also constants or variables (`{ KEY : 1 }` is the string "KEY"; `{ (KEY) : 1 }` uses KEY's value). The work in
progress in `featurescripts/frame/` doesn't pass yet.

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

## Generated files

Lookup tables and sketch profiles (the `*.gen.fs` files) are generated from Python definitions beside them, e.g.
`featurescripts/released/belt/robotBeltTables.py` generates `featurescripts/released/belt/robotBeltTables.gen.fs`. Each definition
sets `CONTENTS` to a list of items:

- `Enum`s and `Table`s (lookup tables) from [`fs_cli/tables.py`](fs_cli/tables.py)
- `Sketch`es and `SketchMap`s of profiles (`SketchDataArray`s, see `core/sketchData.fs`) built from lines, arcs,
  and fit splines with [`fs_cli/sketches.py`](fs_cli/sketches.py), e.g. `released/splineProfile/splineProfiles.py`. A
  profile can also be read from a vendor's STEP model with [`fs_cli/step.py`](fs_cli/step.py), which turns the
  outer loop of a planar face into lines, arcs, and fit splines (through enough points to stay within 0.0002" of
  B-spline edges); see `released/printAdapter/printAdapterProfiles.py`, which keeps vendor models and drawings in
  `printAdapter/vendor/`.
- `Constant`s and `Code`, e.g. predicates generated from a definition's data so preconditions can show
  parameters based on it (see `released/printAdapter/printAdapterProfiles.py`)
- `Import`s of the studios the generated code uses, by path, e.g. `Import("core/sketchData.fs")`

After editing one:

```
uv run fs gen                # rewrite every .gen.fs file whose definition changed (no API calls)
uv run fs push               # push them
```

Every `.py` file in `featurescripts/` is a definition. Generated files keep their `FeatureScript` version
(`fs update-std` changes it); new ones use the std version in `std/`. Tests fail if a checked-in `.gen.fs` file is
out of date, or a profile isn't a closed loop.

Sketch entities are named by their position in a profile, and those names end up in the ids of the faces they
create, so reordering a released profile would break references to its faces in documents using it. `Profile`
takes an `order` to keep the order a profile was released with.

## Releasing

The frontend document is what users import. It isn't mirrored into the repo: each of its Feature Studios is a
small generated file re-exporting a version of the backend studio, and `fs release` keeps it up to date.

```
uv run fs release robotFrame --minor -d "Added a thing"   # 1.2.3 -> 1.3.0
uv run fs release robotFrame --patch --publish            # also create the version in the frontend
uv run fs release robotFrameBeta --major --beta           # start a beta: 1.3.0 -> 2.0.0-beta.1
uv run fs release robotFrameBeta --beta                   # continue it: 2.0.0-beta.2
uv run fs release robotFrame --minor --dry-run            # show the plan without changing anything
```

A release creates a version named e.g. `Robot frame - v1.3.0` in the backend document, then points the
frontend studio of the same name at it (creating the studio if needed). Beta releases go to the separate
`frontend_beta` document instead, and the feature's name must contain "beta". The studio must be pushed and in
sync before it can be released, and `fs release` asks for confirmation since versions can't be deleted (`-y`
skips the prompt).

### Released FeatureScripts

A released FeatureScript's backend tab has to keep its element id and name: Part Studios using its feature only
update to newer versions of it from the same tab, and `fs release` finds its frontend studio by its name. So
`fs-studios.json` lists the released tabs, and `fs` never deletes, recreates, or renames them: `fs push` skips
deleting the tab of a released file you deleted, and won't recreate a released tab deleted in Onshape (restore it
from the document's history). Other tabs can be deleted and recreated freely.

`fs release` marks what it releases. Otherwise:

```
uv run fs released                  # list released FeatureScripts
uv run fs released robotFrame       # mark one released (--remove unmarks it)
uv run fs released --detect         # mark every tab with a release version in the backend document
```

`uv run fs changes robotShaft` diffs a released FeatureScript, and every Feature Studio it imports, against the version
of its last release (`--stat` lists only the files which changed). See [docs/feature-writeups.md](docs/feature-writeups.md)
for how it's used.

### Deprecating

Deleting a released FeatureScript would break documents using it as soon as they update to a newer version of
the frontend document, so retire it instead:

```
uv run fs deprecate robotTube -d "Use Robot frame" --publish
```

This renames its feature "DEPRECATED Robot tube" and pushes it, creates a version named `DEPRECATED Robot tube` in
the backend document, points the frontend studio at it and renames that tab `DEPRECATED robotTube.fs`, and (with
`--publish`) creates the version in the frontend document. The frontend studio keeps its element id, so documents
using the feature keep working and keep updating. Then, since the frontend studio imports a version, the backend tab
and the file are deleted (`--keep-backend` keeps them, renaming the tab too, e.g. if other studios still import
it). The API can't rename folders, so rename any folders holding its tabs by hand. `--dry-run` shows the steps
first.

`uv run fs sync-versions` creates any release versions that exist in the backend document but not in the
frontend document.

# VS Code

Open the repo root in VS Code; `.vscode/settings.json` associates `*.fs` with FeatureScript.

The extension provides:

- TextMate and semantic highlighting (custom features, predicates, enums and members, annotation and map keys,
  stdlib symbols, ...)
- Outline, breadcrumbs, sticky scroll, and folding
- Go to Definition and Find References across files (imports are resolved through `fs-studios.json`), highlights,
  and workspace symbol search (Ctrl+T). Go to Definition also works on the function names in a feature's
  `"Manipulator Change Function"` and `"Editing Logic Function"`, and on `"UIHint"` strings
- Go to Definition into std (`std/`, the copy `fs pull-std` keeps): on its functions, constants, enums and their
  members, types, and the files imports of it refer to. Find References on a std symbol lists where the repo uses it
  (as `fs refs` does). Std's files navigate the same way, and get no diagnostics or formatting. Completions, hovers,
  and signature help for std come from an index of it built by `fs pull-std`, which is quick to load; its files are
  only parsed when they're navigated to
- A Preview Feature UI button (in the editor's title bar, for files defining a feature) which shows the feature's
  dialog as `fs ui` renders it, beside the file, following edits as they're made (saved or not), in VS Code's light
  or dark theme. It works like Onshape's dialog: clicking a tab, checkbox, button, or dropdown option, or entering a
  value, changes that parameter (showing and hiding the parameters which depend on it); groups and array items open
  and close; and array items can be added and removed. Clicking a number selects it all. Hovering over a parameter shows
  a tooltip like Onshape's (its name, a number's value, and its description), with its default and UI hints under it.
  The language server renders it, keeping the parsed std library between renders, so it updates in tens of milliseconds
- Hovers with doc comments laid out like Onshape's [FsDoc](https://cad.onshape.com/FsDoc/library.html) (for std
  symbols too), signatures, enum variants, feature definition fields, and the file an import refers to
- Signature help in calls, with each parameter's documentation
- Completions for enum members (`BoundingType.`) and feature definition-map keys
  (`extrude(context, id, { ... })`)
- Diagnostics: syntax errors, undefined names, and unused or unknown imports (the same as `fs check`)
- Formatting (Format Document, Format Selection, or `editor.formatOnSave`), the same as `fs format`. Files with syntax
  errors, generated files, and std's files are left as they are
- Snippets (`fs-header`, `defineFeature`, `annotation`, ...)
- Commands: **FeatureScript: Push File to Onshape** (also a button in the editor title bar), **Push All**,
  **Pull**, **Sync**, and **Show Onshape Status**, which save and then run `fs` in a terminal

It starts the language server with `.venv/bin/fs-lsp`, falling back to `uv run fs-lsp`; override this with the
`featurescript.server.command` setting.

## Doc comments

Write doc comments like the std library's, which the hovers and signature help lay out like FsDoc
(see `vscode-extension/server/fs_lsp/fsdoc.py`):

```
/**
 * Extrudes faces. Refer to other functions like [qCreatedBy].
 * @param id : @autocomplete `id + "extrude1"`
 * @param definition {{
 *      @field entities {Query} : Faces to extrude.
 *      @field endDepth {ValueWithUnits} : @requiredif {`endBound` is `BLIND`.}
 *              How far to extrude.
 *              @eg `1 * inch`
 *      @field startBound {BoundingType} : @optional
 *              The type of start bound.
 * }}
 * @returns {Query} : The new bodies.
 * @throws {GBTErrorStringEnum.BAD_GEOMETRY} : If there's nothing to extrude.
 * @seealso [opRevolve]
 */
```

Enums document their values with `@value NAME : description`, and `@internal` marks what isn't part of a module's
interface.

## Developing the extension and language server

- All the language smarts are Python, in `vscode-extension/server/fs_lsp/`; `server.py` wires them up with
  [pygls](https://github.com/openlawlibrary/pygls). After changing them, run
  **FeatureScript: Restart Language Server**.
- `vscode-extension/src/extension.ts` only launches the server and registers the `fs` commands. After changing
  it (or the grammar, snippets, or `package.json`), run `npm run install-extension` again, or press F5 (the
  **Run FeatureScript extension** launch configuration) to try it in a new window.
- The grammar is edited in `vscode-extension/syntaxes/featurescript.tmLanguage.yaml`; `npm run build`
  regenerates the JSON.
- The server's knowledge of the Onshape standard library lives in `vscode-extension/server/fs_lsp/data/`, generated
  from `std/`. See below to update it.

# The std library

`std/` holds a copy of the Onshape std library ([MIT](std/LICENSE.txt)); `std/std.json` records its version. To
move to the latest std:

```
uv run fs pull-std           # update std/ (and the language server's index of it)
uv run fs update-std --push  # move your FeatureScripts to that version and push them
```

`fs pull-std` uses the [std library mirror on GitHub](https://github.com/javawizard/onshape-std-library-mirror),
which costs no Onshape API calls. If the mirror lags behind Onshape, `fs pull-std --from-onshape` pulls from
Onshape instead. That records each std file's microversion and only downloads files whose microversion changed,
but note that every std release bumps the version numbers in every std file, so pulling a new release from
Onshape still downloads all ~265 files (1 API call each). `uv run python -m fs_lsp.tools.update_stdlib` just
regenerates the language server's index from `std/`.

# The Onshape API

`onshape_api/reference/openapi.json` is a trimmed, committed copy of Onshape's API definition (the one behind the
[Glassworks API explorer](https://cad.onshape.com/glassworks/explorer)), covering just the endpoints we call.
`onshape_api/types.py` hand-types the parts of those responses we read. See the
[onshape_api README](onshape_api/README.md) for how to add an endpoint.

The extension and language server are based on
[gatrall/featurescript-language-support](https://github.com/gatrall/featurescript-language-support) (MIT).

# Tests

```
uv run pytest                                      # fs CLI, Onshape client, language server, evaluator, and FeatureScript tests
cd vscode-extension && npm test                    # TextMate grammar
cd vscode-extension && npm run test:integration    # the extension inside a real VS Code
```

On a headless Linux machine, run the integration test under `xvfb-run -a`.
