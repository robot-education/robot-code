# The `fs` command

How `fs` syncs `featurescripts/` with the Onshape documents, checks and navigates FeatureScript, generates files,
and releases features. Every command takes `--help`. For tests and the evaluator, see [testing.md](testing.md).

## Documents

`fs` works with a few Onshape documents, named in `[tool.fs.documents]` in `pyproject.toml`. Each has a role:

- **backend**: the source of FeatureScripts and part sets, to try them out in. The backend (`[tool.fs] backend`) is
  synced with `featurescripts/` (`fs push`, `fs pull`), and holds every release's version. Another, like FRCDesign's
  Enterprise document, holds copies of what's pushed to it (`fs push NAME --to DOC`, see
  [Pushing to another backend](#pushing-to-another-backend)) and part sets built there (`fs build SET --to DOC`).
- **library**: for loading into a test FRCDesignApp. `fs release NAME --to DOC` releases a FeatureScript or a part set
  there, from any branch, unnumbered (see [Releasing](#releasing)).
- **frontend**: user facing. Releases go there, numbered, from main: FeatureScripts' to `[tool.fs] frontend`, and each
  part set's to its own (`[tool.fs.parts.<set>] frontend`): FRCDesignLib's and ConfigLib's documents, one per part set.

```toml
[tool.fs]
backend = "backend"     # the backend synced with featurescripts/
frontend = "frontend"   # where FeatureScripts are released

[tool.fs.documents.backend]
role = "backend"
url = "https://cad.onshape.com/documents/<id>/w/<id>"

[tool.fs.documents.enterprise]
role = "backend"
url = "https://frcdesign.onshape.com/documents/<id>/w/<id>"
keys = "FRCDESIGN"      # its own API keys: FRCDESIGN_API_ACCESS_KEY and FRCDESIGN_API_SECRET_KEY

[tool.fs.documents.frcdesignlib-fasteners]
role = "frontend"
url = "https://cad.onshape.com/documents/<id>"

[tool.fs.parts.fasteners]
frontend = "frcdesignlib-fasteners"
```

A document's url says which Onshape it's on: cad.onshape.com, or an Enterprise's own domain. It names a workspace
(`/w/<id>`), or `fs` looks up the document's default workspace (a call) when it needs one; the backend's must name its
workspace. Each is reached:

- With `keys`, with keys of its own, from `<keys>_API_ACCESS_KEY` and `<keys>_API_SECRET_KEY` (in `.env`, like the
  usual keys), on its url's domain: an Enterprise's keys, for its documents. Every request is signed, so it needn't be
  shared, and the calls count against the Enterprise's API limits, not yours.
- Without, with the usual keys (`API_ACCESS_KEY` and `API_SECRET_KEY`). Requests Onshape answers without credentials
  are made anonymously first (see [API usage](#api-usage)).

`fs` writes only to these documents, and each command only to the roles it's for: `--to` names a document of the role
the command takes, and anything else is refused. Two names can't be the same document.

## Pushing and pulling

The backend document's every Feature Studio corresponds to a file in `featurescripts/`. The local folder structure is
yours: once a file is tracked you can move or rename it freely, and the folders in Onshape are ignored. The only time
Onshape's folders matter is when a studio is pulled into the repo for the first time, e.g. a studio named
`robotFrame.fs` in the document's `Robot` folder lands at `featurescripts/Robot/robotFrame.fs`.

The repo is the source of truth, so pushing is the everyday command. It's never implied: `fs` with no command, or a
mistyped one, is an error, so nothing reaches Onshape unless it's asked for by name.

```
uv run fs push               # push every local change
uv run fs push robotFrame    # push one FeatureScript, by name...
uv run fs push featurescripts/Robot   # ...or by file or folder
uv run fs push --dry-run     # show what would be pushed
uv run fs push --no-commit   # push without committing
uv run fs push robotFrame --to enterprise   # push one FeatureScript (and what it imports) to another backend instead
```

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

### What a push does

A file is pushed after the files it imports that are pushed with it, naming the versions they were just given:
Onshape rewrites the import in a tab pushed before the tab it imports, which would leave the local file behind.

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

### Pushing to another backend

`fs push --to DOC` pushes the FeatureScripts named, and every studio and image they import from the repo (directly or
not, by element id or by path), to another backend document than the one synced with the repo: FRCDesign's Enterprise
document (`enterprise`), say. `--to backend` is a plain `fs push`.

```
uv run fs push robotFrame --to enterprise                       # robotFrame and what it imports
uv run fs push robotFrame robotGear --to enterprise             # several, sharing their imports
uv run fs push featurescripts/Robot --to enterprise --dry-run   # a folder's; show what would be pushed
```

This is the way to try out a change in Onshape: push just the FeatureScripts you're testing to another backend,
rather than everything to the backend (with plain `fs push`), unless the test needs every FeatureScript in one document.
It leaves the backend, the local files, `fs-studios.json`, and git as they are (nothing is committed), so it can be
run as often as you like from any branch. `fs build SET --to DOC` builds a part set there the same way (see
[docs/parts.md](parts.md)).

- The target's tabs are matched with files by name (`robotFrame.fs`, or `robotFrame`): a tab of the same name is
  overwritten, without checking whether it changed in Onshape, and a missing one is created at the top level. Nothing
  in the target is deleted, and its other tabs are left alone.
- Imports are pointed at the target's tabs, at the versions just pushed, as each is pushed after what it imports; the
  local files keep importing the backend's tabs (or by path).
- A document with `keys` is reached with them alone: `fs push --to` doesn't touch the backend. One reached with the
  usual keys must be **public or link-shared** (link sharing is enough): fs lists a document's tabs and their
  microversions with anonymous requests (see [API usage](#api-usage)), which only work for those, so it checks first,
  with an anonymous request (free), and refuses any other document before changing anything. The keys' account must be
  able to edit it.

It costs 1 call per studio and image pushed, and 1 per tab created. Listing the target's tabs (once, then again after
each round of studios pushed, for their new microversions) and downloading its images of the same names (to skip
uploading those already the same) cost 1 call each with keys of its own; with the usual keys they're anonymous, as is
checking the document can be opened by anyone with its link.

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
recreated by `fs push` unless you delete the file. A recreated tab has a new id, so files importing its old one
must be pushed with it, and the push points them at the new tab; it refuses to make the tab while one isn't. Onshape's list of tabs can lag behind a tab created seconds
before, so a tracked tab missing from it is listed again, then fetched to confirm it's gone (a 404, which doesn't
count against the API limits); if it's still there, `fs` stops rather than recreating it. Released FeatureScripts are the exception: their tabs are never
deleted or recreated (see [Releasing](#releasing)).

### API usage

Onshape limits API calls per year (2,500 per user on Standard/Free plans; see
[API limits](https://onshape-public.github.io/docs/auth/limits/)), so `fs` keeps them to a minimum. Requests
Onshape answers without credentials for documents anyone with the link can open, public or link-shared (listing tabs,
downloading images, and reading a tab's metadata), are made anonymously, so they don't count (for any other document,
they fall back to signed calls, which do). The backend and frontend documents are public; see
[the Onshape API](../onshape_api/README.md#anonymous-requests). The counts below are of the calls which count:

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
- `fs push --to`: as pushing, but without the downloads (see [Pushing to another backend](#pushing-to-another-backend));
  with a document's own keys, its calls count against their account instead.
- `fs tabs` and `fs link`: just the anonymous listing. `fs tabs --rename`: 1 more per tab renamed (plus an anonymous
  one to read its metadata).
- `fs release`: what it pushes, then at most 6 to 8 calls, and up to 7 more for FRCDesignApp's side (the document's
  tabs, an example, its icon, and `featurescripts.json`); 1 more to look up a document's workspace when its url names
  none. A part set's: 4 to 5 per Part Studio built. `fs deprecate`: about 12.

So `fs status`, and `fs push` or `fs pull` with nothing to do, cost nothing: they only make the anonymous listings.
Failed calls (like a 400) don't count. Commands that only read the repo (`fs check`, `fs gen`, and the others below) make no calls.

## Branches

Every git branch works in the backend's main workspace: `fs push` from any of them writes it, so what's there is
whatever was pushed last, from wherever, and a studio someone's still working on may be broken there for a while. That's
fine, as releases don't take what's there: each pushes what it releases first.

- Releasing to a frontend document (`release`, `publish`, `deprecate`, and `releases --run`) runs only on the main
  branch (`main_branch` in `[tool.fs]`, robot-code's `main`). `release` and `releases --run` also run only once it's
  what GitHub has: they fetch origin, and refuse unless HEAD is origin's main and what's released has no changes but
  what's committed. A release to a library (`release NAME --to DOC`), to try it out, runs from any branch.
- Every release pushes what it releases over Onshape's first: the FeatureScript (a part set's), and every studio and
  image it imports, directly or not, in import order (so each import names the version just pushed), overwriting
  changes made in Onshape (as `--force`), and deleting nothing. Studios it doesn't import are left as they are, and are
  in the version as they are. Their import versions are committed, as `fs push` commits them.

A CI checkout may leave no branch checked out; `FS_BRANCH` names it.

### Release files

A branch asks for a release by adding a file to `releases/`, reviewed with its code; `fs releases --run` makes it once
the branch is merged into main:

```toml
# releases/frame-hubs.toml
name = "robotFrame"         # a FeatureScript or a part set, as `fs release NAME` takes it
bump = "minor"              # major, minor, or patch
description = "Adds hubs"   # internal, as --description
hard = false                # a hard release: version the frontend too, and tag it (not a FeatureScript's first)
```

A release from a file is soft by default (see [Soft and hard releases](#soft-and-hard-releases)): it makes the backend
version and updates the frontend, but doesn't version the frontend, which `fs publish` does once it's checked there
(and a FeatureScript's examples are updated for it). `hard = true` makes it a hard release, for one which needs nothing
checked in the frontend. A FeatureScript's first release is always soft, from a file or with `fs release --hard`: a
new feature needs its examples made first. `fs releases` (a pull request's check) reports a file asking for that.

They run in name order. Each runs as `fs release NAME` would, to its frontend document, then its file is deleted and
what the release recorded (`fs-studios.json`, a part set's locations and exports) is committed. Every version a release
makes ends its description with the file's name and the commit that added it (`[releases/frame-hubs.toml 1a2b3c4d5e6f]`),
so a file whose release Onshape already has, whole or partway, stops `fs releases --run` rather than releasing twice:
check what it made, finish it by hand, and delete the file. A file added again later, under the same name, is a new
release. `fs releases` lists and checks the pending files without calling Onshape.

### The Onshape workflow

`.github/workflows/checks.yml` runs the tests, `fs check`, and `fs releases` on every pull request.
`.github/workflows/onshape.yml` runs on each push to main, one run at a time: the same checks, then `fs releases --run`,
and a push of what it committed to main. It doesn't push anything else to Onshape. A run whose commit isn't origin's main
any more (another push came after it) fails its releases, and the next run makes them. It needs:

- The secrets `ONSHAPE_ACCESS_KEY` and `ONSHAPE_SECRET_KEY`: API keys of an account with edit access to the backend
  and the frontend documents, whose allocation a run spends only on releases (a release's calls, plus one a studio it
  pushes). A frontend document with `keys` needs those too, set in the workflow's environment.
- Its commits pushed to main past branch protection: the workflow's `github-actions[bot]` allowed to bypass it.

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
uv run fs table featurescripts/released/shaft --name frcShaft   # lookup tables as rows (--md, or --html page.html)
uv run fs audit featurescripts/released/lighten/robotLighten.fs   # one page: its working dialog, writeup, and problems (--open: in VS Code)
uv run fs cots 'hex shaft' --details   # how often teams use COTS parts, from FRCDesign (see docs/cots-research.md)
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

A part set's feature (see [docs/parts.md](parts.md)) is used through its Part Studios' configurations, not its own
dialog, so for a Feature Studio in a part set's folder the page and the preview show those in the dialog's place: a
panel for each Part Studio using one of its features (on the page each in turn; in VS Code's preview each chosen as a
file's features are), with Onshape's styles and its inputs as dropdowns (a checkbox's as a checkbox). Choosing options
shows what the configuration shows with them: an option shows while a rule controlling it holds, and an input shows
while its condition holds and it has an option shown; a hidden input keeps its value, which later conditions read.
Where a choice is hidden, the panel shows the first option shown (what Onshape shows there isn't documented).
`fs_cli/configurations.py` reads a configuration from Onshape's JSON (`parse_configuration`: what
`ConfiguredStudio.configuration_body` writes, or what `fs configuration URL` reads back from Onshape) and works out
what it shows (`Configuration.state`), and the page's script (its `RUNTIME`) does the same; a test checks the two draw
alike, and another that every configuration a part set's manifest lists shows its own choices. The panels are read
from the definitions as they are on disk, as `fs build` would build them.

`fs audit --artifact` writes the page as a claude.ai Artifact's, to publish (Claude Code's Artifact tool) so people can
comment on it: without the document around it (publishing adds one), its colors following the viewer's theme (`--theme`
then sets only the dialog's), and its side gutter clearing a phone's system bars.

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
item's depth. `featurescripts/uiTestBench/uiTestBench.local.fs` has one
of every kind of parameter and UI hint (and groups nested in groups), for comparing `fs ui` with Onshape (`uv run fs ui
featurescripts/uiTestBench/uiTestBench.local.fs --set items=2`). To capture how Onshape draws a dialog, open it in Onshape and
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

A Part Studio can expose a feature through its configuration: an input per parameter (a List, or for a boolean a
checkbox or a `switch`, a List of `false` and `true`), each configuring that parameter, with a List's options shown
only for some combinations of the inputs before it (a lookup table, laid out as a configuration). A whole input can be
shown only for some combinations too; hidden, it's left out of `combinations()`, and keeps whatever value it was last
set to. A List of one option is fixed: it's left out of the configuration, and its parameter set to that option when
the feature's inserted.

A `LookupTable` lays a table out by these rules, from what's made, each thing a combination of its inputs' options
(the fasteners' `SCREW_TABLE`, say, of vendor, size, head, finish, and length):

- An input shows where more than one of its options is made with the choices shown before it, offering those, and is
  hidden where only one is. An input never shown is fixed, as the nut's Vendor is (McMaster-Carr makes every nut).
- A condition names an input only where it shows: where it's hidden, Onshape and FRCDesign read its value
  differently, so the options after it show by the choices before it alone. The studio checks this.
- The feature resolves a hidden input to the one option made there, whatever value its parameter holds, and builds
  from what it resolved to (the fasteners' `resolveChoices`).
- A configuration is the choices it shows (`LookupTable.shown`), and what reads the studio outside Onshape lists one
  record per configuration, holding those choices alone.
- Its default is shown wherever its input shows at the other inputs' defaults, and every option is controlled by a
  rule (below).

`ConfiguredStudio.problems()` checks the configuration as it's written against these rules and the shape Onshape takes
conditions in (below); `fs build` and `fs release` refuse a set with any before they send Onshape anything, and
`tests/featurescript/fasteners_manifest_test.py` checks the fasteners' studios as generated. When Onshape refuses a
configuration these allow, `fs build SET --diagnose` finds which input's rules, and what it refused becomes a rule here.
A definition describes one as a `ConfiguredStudio` (see [`fs_cli/configured_studios.py`](../fs_cli/configured_studios.py))
in its `STUDIOS`, and the feature's enums come from the same definition, so the two can't drift apart. The Part
Studios are built, not made by hand: a build finds each one's tab by name (`tab`, the studio's name capitalized by
default), creating it if it's missing, and deletes the feature a build inserted before: its configured parameters name
the configuration's inputs and options, and Onshape refuses a configuration that leaves them naming ones it no longer
has ("The change would result in the feature list becoming invalid"). It then replaces the configuration with the
definition's, which must keep the inputs' ids, inserts the feature from the Feature Studio in the same document as it
is now, with each input's default, and configures it, by the id the insert answered: a feature list read just after
the insert has been seen to fail as though it named the feature deleted before it, so the build reads it again, a second
apart, until it lists the new one, and stops after five reads. The configured feature keeps the namespaces
Onshape stored on insert, for it and each of its enums: a feature from a Feature Studio re-exporting another
document's (as a release's is) is stored under another namespace than it's inserted with, and Onshape refuses an
update naming any other, "Feature does not match". A refusal names the
step Onshape refused, and a feature that fails to regenerate in its default configuration stops the build. `fs build` and
`fs release` build them, for part sets (see [docs/parts.md](parts.md)).
Onshape takes a visibility condition only as its UI makes one, an AND of inputs each one of some values, and refuses
any other with a 409 ("The change would result in the feature list becoming invalid"). So the combinations which show
an option are factored by input, earliest first (or by `factor_first`), into such conditions, each a rule controlling
the options it shows: an option shows while a rule controlling it holds. As Onshape's UI does, a last rule shows the
rest always, since Onshape hides an option no rule controls. Onshape also refuses a List whose default is hidden while
the inputs before it are at theirs, as the studio checks. A List with none of its options shown is
hidden, so a List's own visibility is folded into its options'. A checkbox has no options, and shows by a single
condition, which the studio checks; one needing more is a `switch`. `fs build SET --diagnose` finds a part of a
configuration Onshape refuses (see [docs/parts.md](parts.md)). `ConfiguredStudio.manifest()` gives what FRCDesign reads of
a studio outside Onshape: its feature and the id the feature gives its mate connector. FRCDesign reads the configuration
from the tab.

Feature icons are generated the same way: a definition which sets `ICON` (an `fs_cli.icons.Icon`, drawn from shapely
shapes) is written to the `.svg` of the same name, e.g. `released/lighten/robotLightenIcon.py` to `released/lighten/robotLightenIcon.svg`;
`fs icons` saves a picture of icons side by side to compare them. See [docs/icons.md](icons.md).

After editing one:

```
uv run fs gen                # rewrite every .gen.fs file whose definition changed (no API calls)
uv run fs push               # push them
```

Every `.py` file in `featurescripts/` is a definition. One configuring Part Studios from another definition's tables
sets `STUDIOS` (and `EXPORTS`) without `CONTENTS`, and writes no `.gen.fs` file (`fasteners/ftcFastenerTables.py`). Generated files keep their `FeatureScript` version
(`fs update-std` changes it); new ones use the std version in `std/`. Tests fail if a checked-in `.gen.fs` file is
out of date, or a profile isn't a closed loop.

Sketch entities are named by their position in a profile, and those names end up in the ids of the faces they
create, so reordering a released profile would break references to its faces in documents using it. `Profile`
takes an `order` to keep the order a profile was released with.

## Releasing

`fs release NAME` releases a FeatureScript, or a part set with its Part Studios (see [docs/parts.md](parts.md)), to its
frontend document, as a soft release unless it's `--hard` (see [Soft and hard releases](#soft-and-hard-releases)); with
`--to DOC`, a test release, to a library instead. NAME is a part set's name in `[tool.fs.parts]`, or
else a FeatureScript, by name or file (a part set's own FeatureScript is released with its part set, so naming it is
refused).

```
uv run fs release robotFrame --minor -d "Added a thing"   # 1.2.3 -> 1.3.0
uv run fs release robotFrame --patch --hard               # a hard release: also version the frontend (not a first release)
uv run fs release robotFrame --minor --dry-run            # show the plan without changing anything
uv run fs release robotFrame --to library                 # release it to the library, for a test FRCDesignApp
uv run fs release fasteners --minor                       # a part set, to its own frontend document
```

The frontend document is what users import. It isn't mirrored into the repo: each of its Feature Studios is a
small generated file re-exporting a version of the backend studio, and `fs release` keeps it up to date.

A release pushes what it releases over Onshape's (see [Branches](#branches)), so the version holds it as it is in the
repo, then creates a version named e.g. `Robot frame - v1.3.0` in the backend document, and points the frontend studio
of the same name at it (creating the studio if needed). It asks for confirmation since versions can't be deleted (`-y`
skips the prompt).

### Soft and hard releases

A release to a frontend is soft or hard, the same for FeatureScripts and part sets:

- A **soft release**, the default, stops short of versioning the frontend: it updates the frontend's workspace (its
  studio, a part set's Part Studios and manifest, FRCDesignApp's entry), but people's documents update only to the
  frontend's versions, so they don't get it yet. Check it in the frontend (and make a new feature's example), then
  `fs publish` versions the frontend for it, leaving it as a hard release would.
- A **hard release** (`--hard`) also creates the version in the frontend document (named as the backend's, `Robot
  frame - v1.3.0`), which is what people's documents update to. A FeatureScript's first release can't be hard, and
  nor can one whose feature has no example: a new feature needs its example made first.

A release is tagged in git once it's hard, by `--hard` or `fs publish`: a FeatureScript's by its file's name
(`robotFrame/v1.3.0`), a part set's by the set's (`fasteners/v1.2.0`, since sets share a FeatureScript), on the newest
commit whose file is as released (`fs push` commits what it pushes, so that's usually the last one), and the tag is
pushed to origin (`--no-push-tags` keeps it local). So `git diff robotFrame/v1.3.0 -- featurescripts/Robot/` shows
what's changed since, and `fs changes robotFrame` uses the tag (and `fs-studios.json` at it, for files which have moved)
without any API calls, falling back to the backend version for releases without one. A release with no commit whose
file is as released (older ones, mostly) goes untagged; tags only make finding releases in git easier.

`fs publish` creates the release versions the backend has that their frontends don't: each FeatureScript's and part
set's newest (a version made now holds the frontend as it is, so older ones are left out), and a FeatureScript's
deprecation (see [Deprecating](#deprecating)).

```
uv run fs publish                # version the frontends for every soft release, and tag them
uv run fs publish robotFrame     # just one feature's (or a part set's: fs publish fasteners)
uv run fs changes robotFrame     # what's changed since robotFrame's last release
```

### Test releases

A test release, to a library (`--to DOC`, a library document), does the same there, from any branch, but isn't
numbered: the versions it makes, in the backend and the library, are named for the branch and commit (`Robot frame
test - my-branch 1a2b3c4`), which aren't taken for releases, so the next release's number is untouched. The library is
always versioned, so FRCDesignApp can load it (`--hard` changes nothing), and the entry it writes for the app (below)
has the last release's version with the commit as its build (`1.3.0+test.1a2b3c4`). Nothing is marked released or
tagged, and it takes no `--major`, `--minor`, or `--patch`.

The frontend studio's header links its source: the backend document, and with `github` set in `[tool.fs]`, the
repo on GitHub (the repo, not the file, so the link doesn't break when files move). It's written when the studio's
released, so each FeatureScript gets the GitHub link at its next release.

### FRCDesignApp

FRCDesignApp's FeatureScripts tab lists what `featurescripts.json`, a file tab in the frontend document (or for a test
FRCDesignApp, a library), names, and takes the rest from the document's layout: a feature's category is the folder its frontend studio is in, and its
example is a Part Studio. It reads all of that from the frontend workspace, and adds each feature from the latest
frontend version, so the manifest, examples, and icons can be edited by hand there without a version, and show on the
app's next load. A release keeps that side of the frontend document (`fs_cli/frontend.py`):

- **The example**: a Part Studio named for the feature, its Feature Type Name in any case (`Multi extrude`), preferring
  one after its studio in the same folder; otherwise the first Part Studio after its studio in its folder, so features
  sharing an example sit before it in one folder. Deprecated tabs and folders are skipped, and the app finds examples
  the same way. A feature with none gets an empty one, named for it, at the top level; a hard release is refused until
  it has one, so an empty example is never in a frontend version: release soft, model the example, then `fs publish`.
- **The icon**: the `.svg` its `"Icon"` imports, itself or through a studio it imports (as `core/robotFeature.fs`
  exports `RobotIcon`, the robot icon of features without their own), written to `<Feature Type Name> icon.svg`.
  A feature with none gets `[tool.fs] default_icon` (robot-code's is `core/robotIcon.svg`, the robot icon); only with
  neither is its entry's `iconElementId` null, which the app shows as a generic icon.
- **Its entry in `featurescripts.json`**, replacing the one for the same frontend studio: the exported constant, the
  name, the description (the `"Feature Type Description"` literal, so `~ CREDIT` is cut, with each `<br>` a line break),
  the version, the icon tab, and with `github` set, the file on GitHub's main branch (the latest code, so it doesn't
  depend on tags, which older releases lack). A studio exporting no `defineFeature` gets none.

The API can't make or move folders, so a first release's studio and example land at the top level until they're filed
by hand. `fs deprecate` takes a feature back out: its entry goes, and its icon and its example (unless another
feature's example is it too) are renamed `DEPRECATED ...`.

### Released FeatureScripts

A released FeatureScript's backend tab has to keep its element id and name: Part Studios using its feature only
update to newer versions of it from the same tab, and `fs release` finds its frontend studio by its name. So
`fs-studios.json` lists the released tabs, and `fs` never deletes, recreates, or renames them: `fs push` skips
deleting the tab of a released file you deleted, and won't recreate a released tab deleted in Onshape (restore it
from the document's history). Other tabs can be deleted and recreated freely.

`fs release` marks what it releases (to a frontend; a library's release marks nothing). Otherwise:

```
uv run fs mark-released                  # list released FeatureScripts
uv run fs mark-released robotFrame       # mark one released (--remove unmarks it)
```

`uv run fs changes robotShaft` diffs a released FeatureScript, and every Feature Studio it imports, against the version
of its last release (`--stat` lists only the files which changed). See [docs/feature-writeups.md](feature-writeups.md)
for how it's used.

### Deprecating

Deleting a released FeatureScript would break documents using it as soon as they update to a newer version of
the frontend document, so retire it instead:

```
uv run fs deprecate robotTube -d "Use Robot frame" --hard
```

This renames its feature "DEPRECATED Robot tube" and pushes it, creates a version named `DEPRECATED Robot tube` in
the backend document, points the frontend studio at it and renames that tab `DEPRECATED robotTube.fs`, and (with
`--hard`, as a hard release does) creates the version in the frontend document. The frontend studio keeps its element id, so documents
using the feature keep working and keep updating. Then, since the frontend studio imports a version, the backend tab
and the file are deleted (`--keep-backend` keeps them, renaming the tab too, e.g. if other studios still import
it). The API can't rename folders, so rename any folders holding its tabs by hand. `--dry-run` shows the steps
first. Without `--hard`, `fs publish` creates its frontend version later.

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
