# Rendering parts offline

`fs render` builds FeatureScript-backed parts (the part sets' configurations: fasteners, bearings, and whatever else
`[tool.fs.parts]` declares, see [parts.md](parts.md)) without Onshape, and draws them. It runs their FeatureScript in
the evaluator (`fs_eval`), with the operations modeling real geometry in OpenCascade, so Claude (in a cloud session,
from a phone) can show what a part looks like, and check a change to one, without anyone opening Onshape and without
spending API calls.

```
uv sync --group render                                   # once: OpenCascade (OCP) and numpy
uv run fs render fasteners/screw --set size=QUARTER_20 --each head    # every head of a 1/4-20 screw
uv run fs render bearings --views iso,under,front,top    # each bearing studio's default, four views each
uv run fs render fasteners --check-all                   # build every configuration, report the ones that fail
uv run fs render fasteners/nut --artifact -o <dir>       # the viewer page as an Artifact's, to publish
```

## What it writes

To `.fs-render/<targets>/` (or `-o`), for each configuration (`<set>-<studio>-<choices>`):

- `.png`: the part drawn from `--views` (`iso` by default; several make one image of them all), shaded, with its edges.
  Drawn in software (`fs_cli/raster.py`, numpy), so it needs no browser or GPU and draws the same anywhere.
- `.glb`: a glTF model, a mesh per color (the part's appearance, and any face's own, like a nylock's insert), and its
  edges as lines; Y up, in meters, as glTF has them.
- `.json`: what it is: name, vendor, part number, link, material, density, appearance, volume, mass, center of mass,
  size, threads (from the hole attributes threading it, as std's External thread and Hole set them), and mate
  connectors.

And `index.html`, a page with a 3D viewer of them all (three.js, from jsDelivr): turn, zoom, and pan by mouse or touch,
a configuration per button, each with its spec sheet, its edges and mate connectors (red X, green Y, blue Z) shown or
hidden. With `--artifact` it's written as a claude.ai Artifact's page, its data in `parts.json` beside it, to publish
with the Artifact tool (`files: {"parts.json": ...}`), so it can be opened on a phone.

Choose configurations by their configuration inputs' ids and option ids (as the part set's definition names them, like
`size=NO_10_32`, `head=BUTTON`, `length=IN_0_5`): `--set` picks options (the rest take their defaults, or the first
option offered with the choices), and `--each INPUT` renders every option of an input offered with them. Choices are
checked against what the configuration offers, so a mistyped or unoffered option says what is. At most `--limit` (60)
configurations render at once; `--check-all` builds them all (about 2,100, in under two minutes) and draws nothing.

`fs audit` of a part set's Feature Studio shows the same viewer, of each Part Studio's default configuration, in a
Parts section after their configurations.

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
FeatureScript of its own, and `tests/fs_render_test.py` the command. They're skipped without the `render` group; CI
installs it.

Some bearing configurations can't be built (`KNOWN_UNBUILDABLE` in that test): their configuration hides Dimensions
where the table makes two, so the feature can't tell which, in Onshape too.

## In a cloud session

A Claude Code cloud environment needs the `render` group installed by its setup script:

```
uv sync --group render
```

OCP's wheels are on PyPI for Linux, macOS, and Windows (Python 3.11 to 3.14), so the environment's network access must
reach PyPI. Then Claude renders, looks at the PNGs itself, and publishes the viewer page as an Artifact (see the
`part-render` skill), which opens in the Claude app.
