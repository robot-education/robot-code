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
- Custom sizes from the table choose their Top rows, then their Side rows (2x2's separately too), and each face's rows
  have their own distance: Top row distance and Side row distance replace Distance between rows and 2x1's 2 in. and
  1 in. face row distances. A custom tube's or angle's top or sides can have 0 rows, which hides that face's row
  distance; with neither, it has no holes, which hides the hole parameters too.
- The Custom size adds holes to its top and sides with Add top holes and Add side holes (both on by default), each a
  group with that face's rows and row distance.
- Last Anvil's tube is Aluminum - 7075 (frames have a `material`, Aluminum - 6061 unless their vendor's is another).
- AndyMark's Robits tube has no Wall choice, as it comes in one.
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

`customSize` is hidden: editing logic sets it (its "Set By" function, `isCustomSizeFrame`) for the Custom size, and it
shows Width, Height, and the Add top holes and Add side holes groups (each with its face's rows and row distance), in
place of a table size's Top row distance and Side row distance (`tableTopRowSpacing`, `tableSideRowSpacing`).
`noTopHoles` is too (`isNoTopHolesFrame`), for a custom frame with no holes on its top (a table size with 0 top rows, or
the Custom size without Add top holes), and it hides Top row distance; `noSideHoles` likewise (`isNoSideHolesFrame`).
With both, Distance between holes, Hole diameter, and Hole start distance are hidden too. Hole diameter (FRC) is shown, but editing logic sets it to the frame's when the frame changes, and likewise a COTS frame's Hole start distance
to where its holes start as it's sold (unless it's been set).

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| This frame's holes are `<diameter>`, so holes can't be smaller. | error | FRC COTS: Hole diameter is smaller than the frame's holes | `holeDiameter` |
| The walls are too thick for the frame. | error | Custom: two walls are as thick as the frame is wide or high | `customFrame`, `width`, `height`, `wallThickness` |
| The rows of holes don't fit across the frame's faces. | error | Custom: a face's rows (and a hole's diameter) are wider than the face inside its walls | `customFrame`, `width`, `height`, `topRows`, `sideRows`, `topRowSpacing`, `sideRowSpacing`, `tableTopRowSpacing`, `tableSideRowSpacing`, `holeDiameter` |

And linear stock's, with `<name>` "frame".

### Properties set

Linear stock's, with the entry's `partName`, e.g. `24 in. 2x1 Tube (WCP, 1/16 in. wall)`; a custom frame's is
`<width>x<height> Tube` or `Angle` `(Custom, <wall> wall)`, with the wall from `lengthString` (to 3 places), and it's
Medium Gray (`MEDIUM_GRAY`), with no part number or link. Frames are tagged as frames (profile, swept faces, caps).

## How it works

### Execution order

1. **Precondition**: Program; Source (FRC); the Frame group: FRC's, FTC's, or the custom table, and for custom frames
   the Custom size's size and Add top holes and Add side holes groups (by the hidden `customSize`), or a table size's
   row distances (each unless the hidden `noTopHoles` or `noSideHoles`), wall thickness, and hole spacing (unless
   both); Hole diameter (FRC); Hole start distance; linear
   stock's groups.
2. **Editing logic** (`robotFrameEditLogic`, then `frameChangeEditLogic`): sets `customSize`
   (`isCustomSizeFrame`: whether it's the Custom size), and `noTopHoles` and `noSideHoles` (`isNoTopHolesFrame` and
   `isNoSideHolesFrame`, with `noRows`: whether its top or sides have no rows, from its entry or the Custom size's Add
   top holes and Add side holes). Then if the program, source, or any of the three frame
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
| `customFrameSize` | A custom frame's width, height, angle, rows on each face, and their distances: from its entry, with the distances from the dialog (`tableTopRowSpacing`, `tableSideRowSpacing`), or for the Custom size (`customSize` in its entry), all but the angle from the dialog (`width`, `height`, `topRows` and `topRowSpacing` if `topHoles`, else no rows, and likewise for its sides). |
| `holeRows` | A table entry's rows, as `HoleRow`s. |
| `frameHoleDiameter` | The frame's own hole diameter: the table's, or `CUSTOM_HOLE_DIAMETER` (0.196 in.). |
| `gridRows` | A row of holes along a face: `count` across, centered, `spacing` apart; the first `start` from the start. |
| `verifyFrame` | Above (FRC only). |
| `rowsFit` | Whether `count` rows fit across a face: `(count - 1) * spacing + holeDiameter` is at most its width. |
| `isCustomSizeFrame`, `isNoTopHolesFrame`, `isNoSideHolesFrame` | Set `customSize`, `noTopHoles`, and `noSideHoles` (above). |
| `noRows` | Whether a custom frame's top (or sides) has no rows: a table size's entry has none, or the Custom size doesn't add them. |
| `robotFrameEditLogic` | Above. |

### Data flow

A COTS frame's table entry (profile, walls, holes, stock lengths, appearance, part name) is used as is, but for the hole
diameter. A custom frame's entry only has its size and how many rows each face has (the Custom size's, neither);
everything else comes from the dialog. `sideRows` go through the walls facing X, which are `height` wide; `topRows` through those facing Y, `width`
wide (for 2x1, the 2 in. faces): its Top rows and Side rows, from the table or (for the Custom size) the dialog.

### Error handling

`verifyFrame` runs before anything is built, so its errors leave nothing behind. Then linear stock's.

### `try`s

None of its own, and no fallbacks.

## Issues found

- A custom frame's part name gives its wall to 3 places, e.g. `0.063 in. wall` for 1/16 in., where COTS frames say
  `1/16 in. wall`.
- A face's row distance is shown when it has one row, where it does nothing.
- A custom frame's hole diameter isn't checked against anything but its rows: holes bigger than the distance between
  holes overlap.
