# Robot frame

Adds tube, channel, angle, or extrusion along an edge, or extrudes it from a point, tagged as a frame so std's frame
features work with it. Frames are COTS (from vendors' tables) or custom (a common size of tube or angle, with holes as
the dialog says). The placing and building are linear stock's; see [linearStock.md](../core/linearStock.md), which this
writeup doesn't repeat.

| File | What it is |
| --- | --- |
| `robotFrame.fs` | The feature: its dialog, the chosen frame as a `Stock`, checking its holes, and its editing logic |
| `frameTables.py` | The frame tables' source (and extrusion and MAXTube cross sections); `fs gen` writes `frameTables.gen.fs` |
| `../core/linearStock.fs` | Placing and building stock, and the Position and Tie holes to end groups |
| `vendor/` | Vendors' drawings and STEP files the tables and cross sections come from |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot frame | feature name |
| Add tube, channel, angle, or extrusion along an edge, or extrude it from a point. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

No descriptions. `rectangularFrame` is hidden: editing logic sets it for custom frames whose faces differ in width
(2x1), and it shows the 2 in. and 1 in. face row distances in place of Distance between rows. Hole diameter (FRC) is
shown, but editing logic sets it to the frame's when the frame changes.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| This frame's holes are `<diameter>`, so holes can't be smaller. | error | FRC COTS: Hole diameter is smaller than the frame's holes | `holeDiameter` |
| The rows of holes don't fit across the frame's faces. | error | Custom: a face's rows (and a hole's diameter) are wider than the face inside its walls | `customFrame`, `rowSpacing`, `wideRowSpacing`, `narrowRowSpacing`, `holeDiameter` |

And linear stock's, with `<name>` "frame".

### Properties set

Linear stock's, with the entry's `partName`, e.g. `24 in. 2x1 Tube (WCP, 1/16 in. wall)`; a custom frame's is
`<width>x<height> Tube` or `Angle` `(Custom, <wall> wall)`, with the wall from `lengthString` (to 3 places), and it's
Medium Gray (`MEDIUM_GRAY`), with no part number or link. Frames are tagged as frames (profile, swept faces, caps).

## How it works

### Execution order

1. **Precondition**: Program; Source (FRC); the Frame group: FRC's, FTC's, or the custom table, and for custom frames
   the row distances (by the hidden `rectangularFrame`), wall thickness, and hole spacing; Hole diameter (FRC); linear
   stock's groups.
2. **Editing logic** (`robotFrameEditLogic`): if the program, source, or any of the three frame paths changed (or the
   feature is being created), sets `rectangularFrame` for a custom frame (whether its width and height differ), and
   sets Hole diameter to the frame's (`frameHoleDiameter`: the table's, or 0.196 in. for custom frames), unless it's
   been set larger. Then `stockEditLogic`, with no default offset.
3. **Body**: `verifyHoles` checks the hole diameter or rows, then `getFrame` builds the `Stock`, and `placeStock`
   places it.
4. **Manipulator change**: `stockManipulatorChange` (linear stock's).

### Functions

| Function | What it does |
| --- | --- |
| `frameTableEntry` | The chosen entry: the custom table's for custom frames, else FRC's or FTC's. |
| `getFrame` | The `Stock`. A COTS frame is its entry, with Hole diameter as its holes' (FTC frames keep the table's) and its rows as `HoleRow`s. A custom frame is built from its entry (width, height, angle, rows on each face) and the dialog: walls `wallThickness` thick, and a grid of holes on each face (`gridRows`), with `holeSpacing` between holes along it and the rows `rowSpacing` apart across it; holes count for tying every `holeSpacing`. |
| `holeRows` | A table entry's rows, as `HoleRow`s. |
| `frameHoleDiameter` | The frame's own hole diameter: the table's, or `CUSTOM_HOLE_DIAMETER` (0.196 in.). |
| `isRectangular` | Whether a custom frame's width and height differ (2x1). |
| `rowSpacing` | The distance between rows across a face of a custom frame: `wideRowSpacing` on a rectangular frame's wider faces, `narrowRowSpacing` on its narrower ones, `rowSpacing` on a square one's. |
| `gridRows` | A row of holes along a face: `count` across, centered, `spacing` apart; starting one pitch from the end. |
| `verifyHoles` | Above (FRC only). |
| `rowsFit` | Whether `count` rows fit across a face: `(count - 1) * spacing + holeDiameter` is at most its width. |
| `robotFrameEditLogic` | Above. |

### Data flow

A COTS frame's table entry (profile, walls, holes, stock lengths, appearance, part name) is used as is, but for the hole
diameter. A custom frame's entry only has its size and how many rows each face has; everything else comes from the
dialog. `sideRows` go through the walls facing X, which are `height` wide; `topRows` through those facing Y, `width`
wide (for 2x1, the 2 in. faces).

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
