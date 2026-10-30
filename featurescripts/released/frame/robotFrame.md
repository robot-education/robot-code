# Robot frame

Adds tube, channel, angle, or extrusion along an edge, or extrudes it from a point, tagged as a frame so std's frame
features work with it. Frames are COTS (from vendors' tables) or custom (a common size of tube or angle, or any size,
with holes as the dialog says). The placing and building are linear stock's; see [linearStock.md](../core/linearStock.md), which this
writeup doesn't repeat.

| File | What it is |
| --- | --- |
| `robotFrame.fs` | The feature: its dialog, the chosen frame as a `Stock`, checking its holes, and its editing logic |
| `frameTables.py` | The frame tables' source (and extrusion and MAXTube cross sections); `fs gen` writes `frameTables.gen.fs` |
| `../core/linearStock.fs` | Placing stock, and the Position and Tie holes to end groups |
| `../core/wallStock.fs` | Building it from its walls (`buildWallStock`), or cutting it (`../core/cutStock.fs`) when it can't be |
| `vendor/` | Vendors' drawings and STEP files the tables and cross sections come from |

## Changelog

Since v1.2.0 (`fs changes robotFrame`).

- Tubes, channels, and angles with holes are built from their walls, each sketched with its holes and extruded, rather
  than extruded whole with their holes cut (see linear stock's Wall stock). Their faces' ids change, so
  references to them in existing documents need fixing: a hole is named by its count from the start, or from the end
  if it's tied to it. Square Beam is still cut.
- MAXTube's grooves inside its walls (0.005 in. deep and 0.01 in. wide, behind the rows of holes: inside 2x1 standard's
  and MAX Pattern's 2 in. sides, and every side of 2x1 light with the grid pattern) are cut along it once its walls are
  joined. The drilling grooves outside it are left out, as before.
- FTC frames include Swyft's Channel (48x12 mm, 424 mm long), with its bearing, 10-32, goBILDA, and REV holes; its
  keyholes are two overlapping holes.
- Custom tubes can have No holes (the first choice of their rows), which hides the hole and row parameters.
- WCP's 1x1 and 2x2 3/32 in. wall tubes' walls are 3/32 in. (0.09375 in.), as their 2x1's are, rather than 0.093 in.
- Ends cut by trims (or miters) are tagged as frame caps, as an uncut end is, and shown (or colored) with the tied
  holes; they weren't.

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
Top and Side row distances in place of either. `noHoles` is too (`isNoHolesFrame`), for a custom size from the table
with No holes, and it hides the row distances, Distance between holes, Hole diameter, and Hole start distance. Hole
diameter (FRC) is
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
   (`isCustomSizeFrame`: whether it's the Custom size), and `noHoles` (`isNoHolesFrame`: whether it's a size from the
   table with no rows of holes). Then if the program, source, or any of the three frame
   paths changed (or the feature is being created), sets a COTS frame's Hole start distance to where its holes start as it's sold (`tieStart`) unless
   it's been set, and Hole diameter to the frame's (`frameHoleDiameter`: the table's, or 0.196 in. for custom frames), unless it's
   been set larger. Then `stockEditLogic`, with no default offset.
3. **Body**: `verifyFrame` checks the hole diameter, or the walls and rows, then `getFrame` builds the `Stock`, and `placeStock`
   places it.
4. **Manipulator change**: `stockManipulatorChange` (linear stock's).

### Functions

| Function | What it does |
| --- | --- |
| `frameTableEntry` | The chosen entry: the custom table's for custom frames, else FRC's or FTC's. |
| `getFrame` | The `Stock`. A COTS frame is its entry, with Hole diameter as its holes' (FTC frames keep the table's) and its rows as `HoleRow`s (`holeRows`). Each row's first hole, and the holes counted for tying (`tieStart`), move along it so the first which counts is Hole start distance from the start. A custom frame is built from its size (`customFrameSize`) and the dialog: walls `wallThickness` thick, and a grid of holes on each face (`gridRows`), the first `holeStart` from the start, with `holeSpacing` between holes along it and the face's row distance between rows across it; holes count for tying every `holeSpacing` from `holeStart`. |
| `customFrameSize` | A custom frame's width, height, angle, rows on each face, and their distances: from its entry, with the distances from `rowSpacing`, or for the Custom size (`customSize` in its entry), all but the angle from the dialog (`width`, `height`, `topRows`, `sideRows`, `topRowSpacing`, `sideRowSpacing`). |
| `holeRows` | A table entry's rows, as `HoleRow`s. |
| `frameHoleDiameter` | The frame's own hole diameter: the table's, or `CUSTOM_HOLE_DIAMETER` (0.196 in.). |
| `isRectangular` | Whether a custom frame's width and height differ (2x1). |
| `rowSpacing` | The distance between rows across a face of a custom frame: `wideRowSpacing` on a rectangular frame's wider faces, `narrowRowSpacing` on its narrower ones, `rowSpacing` on a square one's. |
| `gridRows` | A row of holes along a face: `count` across, centered, `spacing` apart; the first `start` from the start. |
| `verifyFrame` | Above (FRC only). |
| `rowsFit` | Whether `count` rows fit across a face: `(count - 1) * spacing + holeDiameter` is at most its width. |
| `isRectangularFrame`, `isCustomSizeFrame`, `isNoHolesFrame` | Set `rectangularFrame`, `customSize`, and `noHoles` (above). |
| `robotFrameEditLogic` | Above. |

### Data flow

A COTS frame's table entry (profile, walls, holes, stock lengths, appearance, part name) is used as is, but for the hole
diameter. A custom frame's entry only has its size and how many rows each face has (the Custom size's, neither);
everything else comes from the dialog. `sideRows` go through the walls facing X, which are `height` wide; `topRows` through those facing Y, `width`
wide (for 2x1, the 2 in. faces). The Custom size's Top rows are `topRows`, and Side rows `sideRows`.

### Error handling

`verifyHoles` runs before anything is built, so its errors leave nothing behind. Then linear stock's.

### `try`s

None of its own, and no fallbacks.

## Issues found

- A custom frame's part name gives its wall to 3 places, e.g. `0.063 in. wall` for 1/16 in., where COTS frames say
  `1/16 in. wall`.
- Every frame gets Aluminum - 6061 as its material, including Last Anvil's 7075 tube.
- The row distances are shown by a hidden parameter editing logic sets, so `fs ui` and the preview (which don't run
  editing logic) show Distance between rows for 2x1 too.
- A custom frame's hole diameter isn't checked against anything but its rows: holes bigger than the distance between
  holes overlap.
