# Robot imported frame

A variant of [Robot frame](../released/frame/robotFrame.md) which imports a model of a COTS frame (a STEP tab) and cuts
it to length, rather than extruding its profile and cutting its holes, to see whether that's faster. Only COTS frames
(Robot frame's FRC and FTC tables) have models, with holes as they're sold: there's no Custom source or Hole diameter.
Placing it is linear stock's, as Robot frame's is; see [linearStock.md](../core/linearStock.md), which this writeup
doesn't repeat.

| File | What it is |
| --- | --- |
| `robotImportedFrame.fs` | The feature: its dialog, the chosen frame as a `Stock` with its model, and its editing logic |
| `frameModels.py` | `fs gen`'s definition of `frameModels.gen.fs`: `FRAME_MODELS`, each frame's model by its part name, and the imports of the models' tabs |
| `models/` | The models, `*.gen.step`, which `fs models` builds (see `fs_cli/stock_models.py`) and `fs push` uploads to tabs |
| `../core/linearStock.fs` | Placing stock, and building it from a model (`buildModeledStock`) |

Unreleased, so it has no changelog.

## Strings

| String | Where |
| --- | --- |
| Robot imported frame | feature name |
| Add COTS tube, channel, angle, or extrusion along an edge, or extrude it from a point, from a model of it. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Hole start distance (`holeStart`) | The distance from the start of the frame to its first hole. |

No hidden parameters. Editing logic sets Hole start distance to where the frame's holes start as it's sold when the
frame changes, unless it's been set.

### Errors, warnings, and info

Linear stock's, with `<name>` "frame", including those of building from a model:

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Holes can't start more than `<lead>` further along than they're sold. | error | Hole start distance is more than the model's lead (2 in.) past where holes start as sold | `holeStart` |
| It can be at most `<length>` long, as its model is. | error | The frame is longer than its model (as long as it's sold, from where its holes start) | `edge` or `depth` |

### Properties set

Linear stock's, as Robot frame's COTS frames: the entry's `partName`, part number, link, and appearance. Frames are
tagged as frames (profile, swept faces and edges, caps).

## How it works

### Execution order

1. **Precondition**: Program; the Frame group: FRC's or FTC's table, and Hole start distance; linear stock's groups.
2. **Editing logic** (`robotImportedFrameEditLogic`): if the program or either frame path changed (or the feature is
   being created), sets Hole start distance to the frame's `tieStart`, unless it's been set. Then `stockEditLogic`,
   with no default offset.
3. **Body**: `getImportedFrame` builds the `Stock`, with its `model`, and `placeStock` places it, building it from the
   model (`buildModeledStock`).
4. **Manipulator change**: `stockManipulatorChange` (linear stock's).

### Functions

| Function | What it does |
| --- | --- |
| `importedFrameEntry` | The chosen entry: FRC's or FTC's. |
| `getImportedFrame` | The `Stock`: the entry, its rows as `HoleRow`s (`holeRows`, Robot frame's) moved along by `shift` (Hole start distance less the entry's `tieStart`), as are the holes counted for tying, and its `model`: its `FRAME_MODELS` entry and `shift`. |
| `robotImportedFrameEditLogic` | Above. |

### Data flow

A model is built from the frame as Robot frame describes it (see `fs_cli/stock_models.py`): its `Stock` from Robot
frame's `getFrame`, with holes where they're sold, its profile from linear stock's `sketchProfile`, and its holes'
shapes from `sketchShape`, run with the evaluator, then modeled with OpenCascade. It runs along +Z from where its holes
start as sold (Z = 0), through the longest length it's sold in, with a 2 in. lead before Z = 0 with no holes, and half an inch more at both
ends, so it's always cut inside it. Frames
with the same geometry (like an extrusion's clear and black anodized) share a model, named after the first of them.
`FRAME_MODELS` maps each frame's part name to its model's tab (`data`, its `BLOB_DATA`), `length`, and `lead`.

### Error handling

Linear stock's; the model's checks run before it's imported.

### `try`s

None of its own, and no fallbacks.

## Issues found

- Untested in Onshape: whether `opImportForeign` takes an uploaded STEP tab's `BLOB_DATA` as `blobData`, whether
  `opTransform` mirrors a body (when flipped), and whether `opNameEntity` (std marks it under development) keeps
  references to tied holes as the length changes.
- A frame longer than it's sold is an error, where Robot frame only warns.
- A length just short of a regular one shows a notch of the next hole at its end (the model has every hole, as the
  stock it's cut from does), where Robot frame leaves it out.
