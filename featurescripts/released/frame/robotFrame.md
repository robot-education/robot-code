# Robot frame

Adds tube, channel, angle, or extrusion along an edge, or extrudes it from a point, tagged as a frame so std's frame
features work with it. Frames are COTS (from vendors' tables) or custom (a common size of tube or angle, or any size,
with holes as the dialog says). COTS frames are cut from a model of them (a STEP tab) where they can be, rather than
built hole by hole. The placing and building are linear stock's; see [linearStock.md](../core/linearStock.md), which this
writeup doesn't repeat.

| File | What it is |
| --- | --- |
| `robotFrame.fs` | The feature: its dialog, the chosen frame as a `Stock`, checking its holes, and its editing logic |
| `frameTables.py` | The frame tables' source (and extrusion and MAXTube cross sections); `fs gen` writes `frameTables.gen.fs` |
| `frameModels.py` | `fs gen`'s definition of `frameModels.gen.fs`: `FRAME_MODELS`, each COTS frame's model by its part name, and the imports of the models' tabs |
| `models/` | The models, `*.gen.step`, which `fs models` builds (see `fs_cli/stock_models.py`) and `fs push` uploads to tabs |
| `../core/linearStock.fs` | Placing and building stock (from its profile, or its model), and the Position and Tie holes to end groups |
| `vendor/` | Vendors' drawings and STEP files the tables and cross sections come from |

## Changelog

Since v1.1.0 (`fs changes robotFrame`).

- COTS frames with holes as they're sold are cut from a model of them (imported, then cut to length and trimmed),
  rather than extruded with each row of holes cut and patterned, to regenerate faster. Their faces' and edges' ids
  change, so references to them in existing documents need fixing: a tied hole's faces and edges are named by their
  count from the end, and the rest come from the import. Frames are still built hole by hole with bigger holes, holes
  moved more than 2 in. further along than they're sold, or longer than their longest sold length (where they
  already warn), and switching between the two changes their ids.
- A COTS frame cut from its model just short of a regular length is cut through the next hole, as it would be cut from
  longer stock, where it was left out.
- Custom frames' Custom size (tube or angle): Width, Height, Top rows, Side rows, and Top and Side row distances.
- Custom frames whose walls are too thick for them are an error.
- Which row distances a custom frame shows (2x1's faces', or Distance between rows) is worked out on every change,
  not only when the frame changes.

## Strings

| String | Where |
| --- | --- |
| Robot frame | feature name |
| Add tube, channel, angle, or extrusion along an edge, or extrude it from a point. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Hole start distance (`holeStart`) | The distance from the start of the frame to its first hole. |

`rectangularFrame` is hidden: editing logic sets it (its "Set By" function, `isRectangularFrame`) for custom frames
whose faces differ in width (2x1), and it shows the 2 in. and 1 in. face row distances in place of Distance between rows.
`customSize` is too (`isCustomSizeFrame`), for the Custom size, and it shows Width, Height, Top rows, Side rows, and
Top and Side row distances in place of either. Hole diameter (FRC) is
shown, but editing logic sets it to the frame's when the frame changes, and likewise a COTS frame's Hole start distance
to where its holes start as it's sold (unless it's been set).

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| This frame's holes are `<diameter>`, so holes can't be smaller. | error | FRC COTS: Hole diameter is smaller than the frame's holes | `holeDiameter` |
| The walls are too thick for the frame. | error | Custom: two walls are as thick as the frame is wide or high | `customFrame`, `width`, `height`, `wallThickness` |
| The rows of holes don't fit across the frame's faces. | error | Custom: a face's rows (and a hole's diameter) are wider than the face inside its walls | `customFrame`, `width`, `height`, `topRows`, `sideRows`, `rowSpacing`, `wideRowSpacing`, `narrowRowSpacing`, `topRowSpacing`, `sideRowSpacing`, `holeDiameter` |

And linear stock's, with `<name>` "frame".

### Properties set

Linear stock's, with the entry's `partName`, e.g. `24 in. 2x1 Tube (WCP, 1/16 in. wall)`; a custom frame's is
`<width>x<height> Tube` or `Angle` `(Custom, <wall> wall)`, with the wall from `lengthString` (to 3 places), and it's
Medium Gray (`MEDIUM_GRAY`), with no part number or link. Frames are tagged as frames (profile, swept faces, caps).

## How it works

### Execution order

1. **Precondition**: Program; Source (FRC); the Frame group: FRC's, FTC's, or the custom table, and for custom frames
   the Custom size's size, rows, and row distances (by the hidden `customSize`) or the row distances (by the hidden
   `rectangularFrame`), wall thickness, and hole spacing; Hole diameter (FRC); Hole start distance; linear
   stock's groups.
2. **Editing logic** (`robotFrameEditLogic`, then `frameChangeEditLogic`): sets `rectangularFrame` for a custom frame
   (`isRectangularFrame`: whether its width and height differ, but not for the Custom size), and `customSize`
   (`isCustomSizeFrame`: whether it's the Custom size). Then if the program, source, or any of the three frame
   paths changed (or the feature is being created), sets a COTS frame's Hole start distance to where its holes start as it's sold (`tieStart`) unless
   it's been set, and Hole diameter to the frame's (`frameHoleDiameter`: the table's, or 0.196 in. for custom frames), unless it's
   been set larger. Then `stockEditLogic`, with no default offset.
3. **Body**: `verifyFrame` checks the hole diameter, or the walls and rows, then `getFrame` builds the `Stock`, and `placeStock`
   places it: linear stock cuts it from its `model` if it has one and it fits (`fitsModel`), and otherwise builds it
   from its profile.
4. **Manipulator change**: `stockManipulatorChange` (linear stock's).

### Functions

| Function | What it does |
| --- | --- |
| `frameTableEntry` | The chosen entry: the custom table's for custom frames, else FRC's or FTC's. |
| `getFrame` | The `Stock`. A COTS frame is its entry, with Hole diameter as its holes' (FTC frames keep the table's) and its rows as `HoleRow`s (`holeRows`). Each row's first hole, and the holes counted for tying (`tieStart`), move along it so the first which counts is Hole start distance from the start (`shift` further along than sold). If its holes are as sold, it has its `model`: its `FRAME_MODELS` entry and `shift`. A custom frame is built from its size (`customFrameSize`) and the dialog: walls `wallThickness` thick, and a grid of holes on each face (`gridRows`), the first `holeStart` from the start, with `holeSpacing` between holes along it and the face's row distance between rows across it; holes count for tying every `holeSpacing` from `holeStart`. |
| `customFrameSize` | A custom frame's width, height, angle, rows on each face, and their distances: from its entry, with the distances from `rowSpacing`, or for the Custom size (`customSize` in its entry), all but the angle from the dialog (`width`, `height`, `topRows`, `sideRows`, `topRowSpacing`, `sideRowSpacing`). |
| `holeRows` | A table entry's rows, as `HoleRow`s. |
| `frameHoleDiameter` | The frame's own hole diameter: the table's, or `CUSTOM_HOLE_DIAMETER` (0.196 in.). |
| `isRectangular` | Whether a custom frame's width and height differ (2x1). |
| `rowSpacing` | The distance between rows across a face of a custom frame: `wideRowSpacing` on a rectangular frame's wider faces, `narrowRowSpacing` on its narrower ones, `rowSpacing` on a square one's. |
| `gridRows` | A row of holes along a face: `count` across, centered, `spacing` apart; the first `start` from the start. |
| `verifyFrame` | Above (FRC only). |
| `rowsFit` | Whether `count` rows fit across a face: `(count - 1) * spacing + holeDiameter` is at most its width. |
| `isRectangularFrame`, `isCustomSizeFrame` | Set `rectangularFrame` and `customSize` (above). |
| `robotFrameEditLogic` | Above. |

### Data flow

A COTS frame's table entry (profile, walls, holes, stock lengths, appearance, part name) is used as is, but for the hole
diameter. A custom frame's entry only has its size and how many rows each face has (the Custom size's, neither);
everything else comes from the dialog. `sideRows` go through the walls facing X, which are `height` wide; `topRows` through those facing Y, `width`
wide (for 2x1, the 2 in. faces). The Custom size's Top rows are `topRows`, and Side rows `sideRows`.

A COTS frame's model is built from the frame as `getFrame` describes it (see `fs_cli/stock_models.py`), with holes where
they're sold: its profile from linear stock's `sketchProfile` and its holes' shapes from `sketchShape`, run with the
evaluator, then modeled with OpenCascade. It runs along +Z from where its holes start as sold (Z = 0), through the
longest length it's sold in, with a 2 in. lead before Z = 0 with no holes, and half an inch more at both ends, so it's
always cut inside it. Frames with the same geometry, to within 0.05 mm, share a model, built from and named after the
first of them: an extrusion's clear and black anodized, and tubes whose vendors give their holes as 0.196 in. or 5 mm
(0.1969 in.), like WCP's, AndyMark's, and REV's 2x1 1/16 in. wall tube (61 frames, 44 models). A frame cut from a
model built from another's holes has holes up to 0.05 mm off its own. `FRAME_MODELS` maps each frame's part name to its model's tab (`data`, its `BLOB_DATA`),
`length`, and `lead`.

### Error handling

`verifyHoles` runs before anything is built, so its errors leave nothing behind. Then linear stock's.

### `try`s

None of its own, and no fallbacks.

## Issues found

- Cutting frames from their models is untested in Onshape (see [linearStock.md](../core/linearStock.md)'s issues).
- A custom frame's part name gives its wall to 3 places, e.g. `0.063 in. wall` for 1/16 in., where COTS frames say
  `1/16 in. wall`.
- Every frame gets Aluminum - 6061 as its material, including Last Anvil's 7075 tube.
- The row distances are shown by a hidden parameter editing logic sets, so `fs ui` and the preview (which don't run
  editing logic) show Distance between rows for 2x1 too.
- A custom frame's hole diameter isn't checked against anything but its rows: holes bigger than the distance between
  holes overlap.
