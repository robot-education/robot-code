# The `fs` command

How `fs` syncs `featurescripts/` with the Onshape documents, checks and navigates FeatureScript, generates files,
and releases features. Every command takes `--help`. For tests and the evaluator, see [testing.md](testing.md).

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

The repo is the source of truth, so pushing is the everyday command. It's never implied: `fs` with no command, or a
mistyped one, is an error, so nothing reaches Onshape unless it's asked for by name.

```
uv run fs push               # push every local change
uv run fs push robotFrame    # push one FeatureScript, by name...
uv run fs push featurescripts/Robot   # ...or by file or folder
uv run fs push --dry-run     # show what would be pushed
uv run fs push --no-commit   # push without committing
```

After pushing, `fs push` commits what it pushed (and the tabs it deleted) to git, as it now is in Onshape: imports by
path resolved to element ids, import versions from Onshape, and `fs-studios.json` with any new tabs' ids. Nothing else
is committed, even what's already staged, so the repo always has what each push sent.

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
featurescripts/released/lighten/partLighten.local.fs`).

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
- Only Onshape changed: `fs push` leaves it alone (without downloading it to check), and `fs status` and `fs pull`
  report it.
- Both changed: `fs push` lists it and asks whether to overwrite Onshape's changes with the local file (yes by
  default; `fs pull --force` keeps Onshape's instead). Without a terminal to ask in, it's skipped, and
  `fs push --force` overwrites it without asking. `fs pull` and `fs sync` skip it; `fs diff` shows both sides.

Changes to the `version` of imports of other tabs in the document (`import(path : "<element id>", version :
"...")`) are never treated as edits, since Onshape manages them itself and may update them when the imported tab
changes. When Onshape's are newer, `fs pull` and `fs sync` copy them into the local file (as `fs push` does for the
files it pushes), and `fs push` never sends older ones.

Files are matched to Feature Studios by element id once synced, so renaming or moving either the file or the tab
doesn't break the link. (On a fresh clone, studios are matched to the local file with the same name.)

Deleting a file and running `fs push` (or `fs sync`) deletes its tab in Onshape, after asking for confirmation
(`--yes` skips it). If the tab changed in Onshape since it was last synced, `fs push` asks first, as for files changed on both sides
(`fs push --force` deletes it anyway), or if another file still imports it. `fs pull --force` restores a deleted file instead. Only deletions
`fs` has seen are tracked: on a fresh clone, a tab with no file is just pulled. A tab deleted in Onshape is
recreated by `fs push` unless you delete the file. Onshape's list of tabs can lag behind a tab created seconds
before, so a tracked tab missing from it is listed again, then fetched to confirm it's gone (a 404, which doesn't
count against the API limits); if it's still there, `fs` stops rather than recreating it. Released FeatureScripts are the exception: their tabs are never
deleted or recreated (see [Releasing](#releasing)).

### API usage

Onshape limits API calls per year (2,500 per user on Standard/Free plans; see
[API limits](https://onshape-public.github.io/docs/auth/limits/)), so `fs` keeps them to a minimum. Requests
Onshape answers without credentials for public documents (listing tabs, downloading images, and reading a tab's
metadata) are made anonymously, so they don't count (if a document isn't public, they fall back to signed calls,
which do); see [the Onshape API](../onshape_api/README.md#anonymous-requests). The counts below are of the calls which
count:

- Listing the backend document's tabs: 1 anonymous call. Every command that talks to Onshape does this first, to
  get each tab's microversion.
- A studio is only downloaded when its microversion changed since the last sync, so on a new machine the first
  run downloads each studio once. A changed microversion doesn't always mean changed code (e.g. it may change
  when a tab it imports changes, which happens to most studios between pushes); then the download just confirms
  nothing needs doing. `fs push` doesn't download studios unchanged locally (except with `--force`), since it
  wouldn't push them either way, so it only downloads the ones it's pushing.
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
[docs/featurescript-style.md](featurescript-style.md).

These read the repo only (no API calls). Imports between studios are resolved through `fs-studios.json`.

```
uv run fs check              # syntax errors, undefined names, unused or unknown imports, and more
uv run fs check --fix        # ...fixing those with one right answer (misspelled annotation keys, unused imports, ...)
uv run fs check featurescripts/released/belt   # ...or just some files or folders
uv run fs format             # indentation and spacing like std's (--check: list what it would change)
uv run fs deps robotShaft    # what a studio imports, and what imports it
uv run fs strings robotShaft # strings it shows users (names, descriptions, errors), with those of what it imports
uv run fs refs cleanup       # where a function, constant, enum, etc. is defined and used (std's too)
uv run fs rename cleanup tidy  # rename it, and every use (-n to see where first; never a released feature's constant)
uv run fs doc opExtrude       # a name's signature and documentation, as the editor's hover shows it (std's too)
uv run fs mv featurescripts/a.fs featurescripts/core/b.fs   # rename or move, keeping its studio and imports
uv run fs unused             # exports nothing uses (--local: also those only their own file uses)
uv run fs ui featurescripts/nutStrip/robotNutStrip.fs --set placement=POINT   # screenshot a feature's dialog
uv run fs eval "getSprocketRadius(0.25 * inch, 16) / inch" -m chain/robotChain.fs   # evaluate an expression (see below)
uv run fs table featurescripts/released/shaft -n frcShaft   # lookup tables as rows (--md, or --html page.html)
uv run fs audit featurescripts/released/lighten/robotLighten.fs   # one page: its working dialog, writeup, and problems (--open: in VS Code)
uv run fs cots 'hex shaft' -d   # how often teams use COTS parts, from FRCDesign (see docs/cots-research.md)
uv run fs step REV-21-2162.STEP featurescripts/frame/vendor/REV-21-2162.STEP   # keep only a vendor STEP file's cross section
```

`fs table` shows lookup tables as the evaluator builds them (generated or written by hand): a row per path through the
options, with each level's default marked, the leaf's values in readable units (lengths in inches if they're round in
them, else millimeters), and the parameters which use the table (their `"Lookup Table"` annotations). `--html` writes
one page of every table, with a filter, to audit them in a browser.

`fs audit` writes one page (`.fs-audit/<feature>.html`, or `-o`) to audit a feature by: its dialog, which works,
the lookup tables its parameters use (every option at once, without their values), its writeup, and its file's
`fs check` problems. The page draws the dialog itself (`fs_cli/live_dialog.py`): the precondition is compiled to a
tree of its groups, parameters, and `if`s, whose conditions become JavaScript (what doesn't depend on the definition
is evaluated first, as `fs ui` evaluates it), and each parameter is rendered once for each of its values. The page
walks the tree with the current settings and lays the dialog out as `fs ui` does, so any combination of choices, and
typed values, works (a test checks it draws what `fs ui --set` renders, state by state, for several features). Setting
a parameter back to its default (like unchecking a checkbox) unsets it. Hovering a parameter (or by touch, a long press)
shows its description, default, and UI hints. Array items can't be added there (`fs ui --set` renders any state).

VS Code's feature preview is the same page, with the language server drawing the dialog instead (so it follows unsaved
edits, and array items can be added): it has tabs for the lookup tables, writeup, and problems (`audit_sections` in
`fs_cli/audit.py`), and `fs_cli/dialog_page.js` makes both dialogs' controls and tooltips work. Run from VS Code (its
terminal, or Claude Code in it), `fs audit --open` shows the feature's preview there: it writes `.fs-audit/open.json`,
which the extension watches for.

`fs ui` renders a feature's dialog as Onshape shows it, from its precondition: parameters take their defaults (or the
values given with `--set`), predicates are inlined, and `if`s are decided the way Onshape decides them. Editing logic
doesn't run, so values it would set (like Robot nut strip's end offsets) show their defaults, except hidden parameters
whose annotations name the function editing logic sets them from (`"Set By"`, see the style guide's "Hidden parameters
set by editing logic"): `fs ui` runs it with the evaluator, for the values chosen. The dialog is written with
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
or its imports (overloads Onshape can't choose between), keys a feature's editing logic or manipulator change function sets on the definition it returns which its precondition doesn't declare (Onshape rejects the whole result; followed through the project functions the definition is passed to and taken back from, a key set to `undefined` along the way counting as removed), groups in array parameters' items, parameters a feature's precondition declares more than once
(directly or through predicates, even in different branches of an if), and predicates in a precondition's `if`
conditions which call other predicates (Onshape doesn't inline those). It warns about parameters which can be toleranced (`CAN_BE_TOLERANT`, ours never are; directly or through std's predicates), comparisons with `true` or `false`, reads of a feature's definition (`definition.x`, where it reaches: its body, editing logic, manipulator change function, and the functions and predicates they pass it to) of keys which aren't parameters or set on it anywhere it reaches (usually misspelled or renamed parameters; in code several features reach, only keys none of them has), local variables which are set but never used (name one you don't need `_`), map keys which are keywords (like `"type"`, which can't be read as `.type`; reading one that way is an error),
precondition conditions Onshape can't evaluate (only parameters, enum values, literals, and predicates work), top-level
declarations which aren't exported or used anywhere, clicks editing logic checks for (`clickedButton == "x"`) which aren't
buttons, buttons it never checks for, and buttons read or set on the definition (they have no value), `"Set By"`
parameters whose functions, or editing logic, don't fit them (see the style guide's "Hidden parameters set by editing logic"; `--fix` adds the statement setting one), and
map keys written as bare names which are also constants or variables (`{ KEY : 1 }` is the string "KEY"; `{ (KEY) : 1 }` uses KEY's value). The work in
progress in `featurescripts/frame/` doesn't pass yet.

## Generated files

Lookup tables and sketch profiles (the `*.gen.fs` files) are generated from Python definitions beside them, e.g.
`featurescripts/released/belt/robotBeltTables.py` generates `featurescripts/released/belt/robotBeltTables.gen.fs`. Each definition
sets `CONTENTS` to a list of items:

- `Enum`s and `Table`s (lookup tables) from [`fs_cli/tables.py`](../fs_cli/tables.py)
- `Sketch`es and `SketchMap`s of profiles (`SketchDataArray`s, see `core/sketchData.fs`) built from lines, arcs,
  and fit splines with [`fs_cli/sketches.py`](../fs_cli/sketches.py), e.g. `released/splineProfile/splineProfiles.py`. A
  profile can also be read from a vendor's STEP model with [`fs_cli/step.py`](../fs_cli/step.py), which turns the
  outer loop of a planar face into lines, arcs, and fit splines (through enough points to stay within 0.0002" of
  B-spline edges); see `released/printAdapter/printAdapterProfiles.py`, which keeps vendor models and drawings in
  `printAdapter/vendor/`.
- `Constant`s and `Code`, e.g. predicates generated from a definition's data so preconditions can show
  parameters based on it (see `released/printAdapter/printAdapterProfiles.py`)
- `Import`s of the studios the generated code uses, by path, e.g. `Import("core/sketchData.fs")`

A definition can also set `EXPORTS`, a map of file names to their text, for what reads its data outside Onshape;
`fs gen` writes each beside it (`fasteners/fastenerTables.py` writes `fasteners.gen.json`, the catalog
FRCDesignApp reads for its library fasteners).

### Configured Part Studios

A Part Studio can expose a feature through its configuration: an input per parameter (a List, or a checkbox for a
boolean), each configuring that parameter, with a List's options shown only for some combinations of the inputs before
it (a lookup table, laid out as a configuration). A whole input can be shown only for some combinations too, as the
fasteners' Variant and Finish are; hidden, it's left out of `combinations()`, and nothing may show by it.
A definition describes one as a `ConfiguredStudio` (see [`fs_cli/configured_studios.py`](../fs_cli/configured_studios.py))
in its `STUDIOS`, and the feature's enums come from the same definition, so the two can't drift apart. Insert the
feature into the Part Studio once, from the version it should use, then:

```
uv run fs configure-studio featurescripts/fasteners/fastenerTables.py screw <Part Studio url>
```

which replaces the Part Studio's configuration with the definition's, and configures the feature's parameters by it.
A List's visibility conditions are factored by input, earliest first, so a lookup table's conditions don't repeat
themselves (the screw studio's Length is 61 KB rather than 192 KB). `ConfiguredStudio.manifest()` gives the
configuration as Onshape answers it, for what reads it outside Onshape (FRCDesign parses its library fasteners' as it parses
any configuration).

Feature icons are generated the same way: a definition which sets `ICON` (an `fs_cli.icons.Icon`, drawn from shapely
shapes) is written to the `.svg` of the same name, e.g. `released/lighten/robotLightenIcon.py` to `released/lighten/robotLightenIcon.svg`;
`fs icons` saves a picture of icons side by side to compare them. See [docs/icons.md](icons.md).

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
uv run fs release robotFrame --patch --publish            # quick release: also create the version in the frontend
uv run fs release robotFrameBeta --major --beta           # start a beta: 1.3.0 -> 2.0.0-beta.1
uv run fs release robotFrameBeta --beta                   # continue it: 2.0.0-beta.2
uv run fs release robotFrame --minor --dry-run            # show the plan without changing anything
```

A release creates a version named e.g. `Robot frame - v1.3.0` in the backend document, then points the
frontend studio of the same name at it (creating the studio if needed). Beta releases go to the separate
`frontend_beta` document instead, and the feature's name must contain "beta". A library's FeatureScripts, those in a
folder `[tool.fs.libraries]` lists, are only for its parts, so they're released to that library's document (its default
workspace) instead, never as betas, and a studio there may define several features, released together:

```toml
[tool.fs.libraries.fasteners]
document = "https://cad.onshape.com/documents/77461d97ef7c9d816f0c7351"
folders = ["fasteners"]
```

`fs sync-versions` leaves their releases out, since they aren't the frontend's. The studio must be pushed and in
sync before it can be released, and `fs release` asks for confirmation since versions can't be deleted (`-y`
skips the prompt).

A release is final once its version is in the frontend document (or its library's): with `--publish` (a quick release: the backend
version, the frontend studio, then the frontend version), or later with `fs sync-versions`, which creates the frontend
versions the backend has that it doesn't. Then it's tagged in git, e.g. `robotFrame/v1.3.0`, on the newest commit
whose file is as released (`fs push` commits what it pushes, so that's usually the last one), and the tag is pushed to
origin (`--no-push-tags` keeps it local). So `git diff robotFrame/v1.3.0 -- featurescripts/Robot/` shows what's
changed since, and `fs changes robotFrame` uses the tag (and `fs-studios.json` at it, for files which have moved) without
any API calls, falling back to the backend version for releases without one. `fs release-tags` tags final releases
which aren't tagged yet (`--dry-run` lists them), from their code in the backend versions: an API call each.

```
uv run fs sync-versions          # finalize releases: create their frontend versions, and tag them
uv run fs release-tags           # tag final releases which aren't yet
uv run fs changes robotFrame     # what's changed since robotFrame's last release
```

The frontend studio's header links its source: the backend document, and with `github` set in `[tool.fs]`, the
repo on GitHub (the repo, not the file, so the link doesn't break when files move). It's written when the studio's
released, so each FeatureScript gets the GitHub link at its next release.

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
of its last release (`--stat` lists only the files which changed). See [docs/feature-writeups.md](feature-writeups.md)
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

## Onshape's FeatureScript guide

`docs/fsdoc/` is a copy of [Onshape's FeatureScript guide](https://cad.onshape.com/FsDoc/) as Markdown: the guide, the
language reference, and the tutorials, each page at its path there (`uispec.html` is `docs/fsdoc/uispec.md`), linking
to each other. `uv run fs fsdoc` copies it again (it makes no Onshape API calls). The standard library's documentation
(`library.html`) is left out, as it's generated from std's doc comments in `std/` (see `fs doc`), and images are linked
on Onshape rather than copied.

## The std library

`std/` holds a copy of the Onshape std library ([MIT](../std/LICENSE.txt)); `std/std.json` records its version. To
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

## The Onshape API

`onshape_api/reference/openapi.json` is a trimmed, committed copy of Onshape's API definition (the one behind the
[Glassworks API explorer](https://cad.onshape.com/glassworks/explorer)), covering just the endpoints we call.
`onshape_api/types.py` hand-types the parts of those responses we read. See the
[onshape_api README](../onshape_api/README.md) for how to add an endpoint.
