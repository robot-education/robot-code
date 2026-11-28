# Rendering parts offline

`fs render` builds FeatureScript-backed parts (the part sets' configurations: fasteners, bearings, and whatever else
`[tool.fs.parts]` declares, see [parts.md](parts.md)) without Onshape, and draws them. It runs their FeatureScript in
the evaluator (`fs_eval`), with the operations modeling real geometry in OpenCascade, so Claude (in a cloud session,
from a phone) can show what a part looks like, and check a change to one, without anyone opening Onshape and without
spending API calls.

```
uv run fs render fasteners/screw --set size=QUARTER_20 --each head    # every head of a 1/4-20 screw
uv run fs render bearings --views iso,under,front,top    # each bearing studio's default, four views each
uv run fs render fasteners --check-all                   # build every configuration, report the ones that fail
uv run fs render fasteners/nut --artifact -o <dir>       # the viewer page as an Artifact's, to publish
uv run fs render fasteners/nut --set size=QUARTER_20 --step   # a STEP file of each, too
uv run fs render --all --step --size 256 -o <dir>        # every configuration of every part set, as a catalog
```

Its OpenCascade (OCP) and numpy are the `render` dependency group, one of the project's default groups, so `uv run`
installs them itself the first time, anywhere (`uv sync --no-default-groups` leaves them out, and `fs render` then says
to run `uv sync --group render`).

## What it writes

To `.fs-render/<targets>/` (or `-o`), for each configuration (`<set>-<studio>-<choices>`):

- `.png`: the part drawn from `--views` (`iso` by default; several make one image of them all), shaded, with its edges.
  Drawn in software (`fs_cli/raster.py`, numpy), so it needs no browser or GPU and draws the same anywhere.
- `.glb`: a glTF model, a mesh per color (the part's appearance, and any face's own, like a nylock's insert), and its
  edges as lines; Y up, in meters, as glTF has them.
- `.json`: what it is: name, vendor, part number, link, material, density, appearance, volume, mass, center of mass,
  size, threads (from the hole attributes threading it, as std's External thread and Hole set them), and mate
  connectors.
- `.step`, with `--step`: its solids as STEP (AP214, written with OpenCascade's XDE, `fs_cli/part_step.py`), in
  millimeters, named as the part is and colored as its appearance (a face with its own, like a nylock's insert, keeps
  it). Mate connectors and cosmetic threads aren't STEP geometry, so they're only in the `.json`.

And `index.html`, a page with a 3D viewer of what it rendered (three.js, from jsDelivr): turn, zoom, and pan by mouse
or touch, view from the top, front, or side, and the part's spec sheet beside it. Its mate connectors are drawn as
Onshape draws them, a circle and its center, and one tapped (in the viewer or the sheet) shows its triad (red X, green
Y, blue Z); edges and mate connectors switch on and off. Several configurations are listed beside it (filtered, where
there are many, and stepped through by arrow keys); one is shown alone. With `--artifact` it's written as a claude.ai Artifact's page, its data in `parts.json` beside it, to publish
with the Artifact tool (`files: {"parts.json": ...}`), so it can be opened on a phone.

Choose configurations by their configuration inputs' ids and option ids (as the part set's definition names them, like
`size=NO_10_32`, `head=BUTTON`, `length=IN_0_5`): `--set` picks options (the rest take their defaults, or the first
option offered with the choices), and `--each INPUT` renders every option of an input offered with them. Choices are
checked against what the configuration offers, so a mistyped or unoffered option says what is. Rendering several
studios, each takes the `--set` and `--each` inputs it has (an input none of them has is an error). At most `--limit`
(60) configurations render at once; `--check-all` builds them all (about 2,100, in under two minutes) and draws
nothing, and `--all` renders them all as a catalog (see [A catalog for apps](#a-catalog-for-apps)).

## In fs audit and VS Code's preview

`fs audit` of a part set's Feature Studio shows the same viewer in a Parts section after its Part Studios'
configuration panels, and choosing in a panel shows that configuration's part. The page can't build parts, so it holds
what `fs audit` built: each Part Studio's default configuration, or with `--each-option`, also each option of each
input with the rest at their defaults (so changing any one dropdown shows a part). Choosing a configuration it doesn't
hold says so, with the `fs render` command that builds it. Each panel posts its choices (every input's value) to the
page, which passes them to the viewer (`{fsShow: {target, values}}`, see `fs_cli/part_page.py`); the viewer shows the
part built for them.

VS Code's feature preview builds whatever's chosen: for a part set's Feature Studio, under the configuration panel, it
shows the part the panel's choices build, built by the language server (its `featurescript/buildPart` request, with
`fs_cli.render.PartBuilder`) when they change, from the files as saved, so saving a FeatureScript or a definition
builds it again. The first build loads std (a few seconds); after that, each takes about half a second, as the builder
keeps its evaluator until a FeatureScript changes on disk. The viewer lists the last few built, to go back to.

## A catalog for apps

`fs render --all` renders every configuration of the part sets named (every part set, when none is), past `--limit`,
as a catalog: each configuration's files in `<set>/<studio>/<choices>` (`.png`, `.glb`, `.json`, and with `--step`,
`.step`), and an `index.json` of them, with no viewer page:

```json
{
    "inputs": {"fasteners/washer": ["vendor", "size", "type", "finish"]},
    "configurations": [
        {
            "partSet": "fasteners",
            "studio": "washer",
            "configuration": {"vendor": "MCMASTER", "size": "NO_10_32", "type": "OD_7_16_X_0_03", "finish": "STAINLESS"},
            "name": "#10 Washer (7/16 in OD x 0.03 in, Stainless steel)",
            "partNumber": "92141A011",
            "files": {"png": "fasteners/washer/MCMASTER-NO_10_32-OD_7_16_X_0_03-STAINLESS.png", "glb": "...", "step": "...", "json": "..."}
        }
    ]
}
```

A configuration is keyed by its input and option ids, as the Part Studio's configuration has them and as FRCDesignApp
inserts it with (see `featurescripts/fasteners/fastenerTables.py`), so an app finds a configuration's thumbnail, model,
or STEP file without asking Onshape. Its inputs are those shown with its choices (a hidden input is left out).

The Parts catalog workflow (`.github/workflows/parts.yml`) builds it on main whenever the FeatureScript or the tooling
changes (or by hand, from the Actions tab), at 256 px, with STEP files, and publishes it with GitHub Pages:
`https://<owner>.github.io/<repo>/parts/index.json`, with the files beside it. It needs Pages turned on with GitHub
Actions as its source (the repo's Settings, Pages). Every part set takes about six minutes and 100 MB; a configuration
that fails to build fails the workflow, so the catalog is never missing one quietly. The images are the evaluator's
builds, not Onshape's: the tests hold them to the tables and vendors' STEP files, and the catalog is rebuilt from main
as the FeatureScript changes.

## How it works

`Evaluator(geometry=True)` adds `fs_eval.geometry`'s built-ins (`GEOMETRY_BUILTINS`) to the evaluator's own: the ones
std calls to model geometry (`@opExtrude`, `@evaluateQuery`, `@startFeature`, ...). Std's own FeatureScript runs on top
of them unchanged: `opExtrude` checks its definition and calls `@opExtrude`, `qCreatedBy` builds a query map, and so on.
Without `geometry`, the evaluator is as it was, and they're errors.

- **The model** (`model.py`): the context's bodies (solid, sheet, wire, point, mate connector), each an OpenCascade
  shape, and every face, edge, and vertex in them, named by a transient id (as `qTransient` names them) and knowing the
  operation that created it, which `qCreatedBy` finds them by. Bodies are replaced, never changed, so a feature's
  rollback (`@abortFeature`) restores them as they were at its `@startFeature`.
- **Sketches** (`sketches.py`): a solved sketch's entities become edges on its plane (a wire body), and the regions
  they bound become faces (a sheet body), as Onshape makes them: found by splitting a face larger than the sketch by its
  edges. `qSketchRegion`'s `filterInnerLoops` leaves out the regions inside another's hole. Sketches aren't solved:
  entities are built as given (see `fs_eval.sketch`), which every part set's are.
- **Operations** (`operations.py`): extrude (blind, through all), revolve, boolean (union, subtraction,
  intersection), mate connectors, deleting bodies, and the default origin and planes. A boolean's result keeps the
  identity of what it changed (a union's first tool, a subtraction's targets; an intersection is new, as std's own
  find it), and faces it splits or trims keep their creator, from OpenCascade's history.
- **Queries** (`queries.py`): transient, nothing, everything, union, intersection, subtraction, created by, sketch
  region, cap entity, body type, entity filter, owned by body, owner body, sketch and construction filters, nth element,
  geometry type, and contains point.
- **Evaluations, properties, and attributes** (`builtins.py`): `evSurfaceDefinition` (planes, cylinders, cones,
  spheres, tori, made with std's own constructors), `setProperty`/`getProperty` (on bodies and faces), and attributes.

What it doesn't model is an error naming the built-in or query (`@opFillet isn't available in the evaluator`,
`Queries of type ADJACENT aren't supported`), so a part set using something new fails loudly: add it in
`fs_eval/geometry/` with a test in `tests/fs_eval_geometry_test.py`. Threads are cosmetic, as in Onshape.

## Checking it

`tests/featurescript/part_sets_geometry_test.py` builds every configuration of every part set, and checks the parts
against their tables (washers' and nuts' sizes, screws' lengths below their seats) and vendors' STEP files (bearings,
in `bearings/vendor/`, within 0.05 mm). `tests/fs_eval_geometry_test.py` tests the operations, queries, and rollback on
FeatureScript of its own, and `tests/fs_render_test.py` the command. They're skipped without the `render` group.

## In a cloud session

A Claude Code cloud session needs no setup: `uv run fs render` installs the `render` group the first time (OCP's
wheels are on PyPI for Linux, macOS, and Windows, Python 3.11 to 3.14, so the environment's network access must reach
PyPI). Then Claude renders, looks at the PNGs itself, and publishes the viewer page as an Artifact (see the
`part-render` skill), which opens in the Claude app.
