# Robot nut strip

Adds a nut strip, a solid bar of tapped holes for bolting through tube, along an edge or a face (such as inside a tube),
or between holes, or extrudes one from a point. The placing and building are linear stock's; see [linearStock.md](../core/linearStock.md),
which this writeup doesn't repeat.

| File | What it is |
| --- | --- |
| `robotNutStrip.fs` | The feature: its dialog, the chosen nut strip as a `Stock`, and its editing logic |
| `nutStripTables.py` | The nut strip tables' source; `fs gen` writes `nutStripTables.gen.fs` |
| `../core/linearStock.fs` | Placing stock, and the Position and Tie holes to end groups |
| `../core/cutStock.fs` | Building it: its profile extruded and its holes cut (`buildCutStock`) |
| `vendor/` | Vendors' drawings and models the tables were checked against |

## Changelog

Since v1.0.0 (`fs changes robotNutStrip`).

- Start offset and End offset (along an edge) extend the nut strip past the edge's ends, and move them in with
  Opposite direction: the other way around from before, so an existing nut strip with an offset gets longer where it
  got shorter. Turned on, each is still set to the distance from its end to the closest hole, now with Opposite
  direction, so it moves the end in to there as before. They're in an Ends group in Position.
- Tied holes are cut by the same seeds and patterns as the rest, their pattern instances named by their count from the
  end, rather than by seeds and patterns of their own: fewer operations, but tied holes' faces have new identities, so
  references to them (and to the holes near the start) in existing documents need fixing once.
- Face selection type: the nut strip on a face as one of its sides (such as inside a tube), along its longest straight
  edge, centered across it, and square at the ends of the face's bounding box along it. Face at (with a three point
  manipulator in the middle) puts its top, middle, or bottom on the face, and Starting offset (std's extrude's, with its
  manipulator) moves it off the face.
- Holes selection type: the nut strip between a start hole and an end hole (holes' edges, sketch points, or mate
  connectors), on the holes' plane, with its holes facing them lined up on the start hole, as far before it as that
  row's first hole is from its start, and past the end hole as a nut strip ends as it's sold (1/4 in. on WCP's,
  whichever row is lined up); a warning when the holes aren't a multiple of its hole spacing apart. Holes at (with a
  three point manipulator) puts its top, middle, or bottom on the holes, and Starting offset moves it off them (for
  spacers). Start offset and End offset, in the Ends group with their manipulators, extend its ends (or move them in,
  with Opposite direction; turned on, each extends its end by the strip's pattern of holes, 1/2 in. on WCP's), its
  holes staying on the selected holes and both rows going on into an extended end.
- Trim nut strip ends has Parts to trim to, as std's Frame does: each part cuts off the end it's nearer, and Trim nut
  strip ends is offered on faces and between holes too.
- Its description mentions faces and holes.

## Strings

| String | Where |
| --- | --- |
| Robot nut strip | feature name |
| Add a nut strip along an edge or a face, such as inside a tube, or between holes, or extrude one from a point. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

None of its own; see [linearStock.md](../core/linearStock.md#descriptions-and-hidden-parameters).

### Errors, warnings, and info

None of its own; all are linear stock's, with `<name>` "nut strip" (see
[linearStock.md](../core/linearStock.md#errors-warnings-and-info)).

### Properties set

Linear stock's, with `partName` `Nut Strip (<vendor> <size>, <thread>)`, e.g. `6 in. Nut Strip (WCP 1/2 in., #10-32)`.
Its holes are tapped holes (hole attributes and cosmetic threads), with the thread's size and pitch.

## How it works

### Execution order

1. **Precondition**: Program; the Nut strip group with FRC's or FTC's table; linear stock's groups.
2. **Editing logic** (`robotNutStripEditLogic`): nothing when the feature is created. Otherwise, calls
   `stockEditLogic` with the nut strip, its `tieStart` as the default offset, and whether the program or nut strip
   changed: turning on an end's offset (or changing the nut strip) sets the offset, unless it's been set, along an edge
   to the distance from the end to the closest hole (with Opposite direction, moving the end in), and between holes to extend that end by the strip's pattern of
   holes (1/2 in. for WCP's, an X hole and a Y hole).
3. **Body**: `getNutStrip` builds the `Stock`, and `placeStock` places it.
4. **Manipulator change**: `stockManipulatorChange` (linear stock's).

### Functions

| Function | What it does |
| --- | --- |
| `getNutStrip` | The chosen table entry (FRC's or FTC's), as a `Stock`: a solid bar `width` by `height`, with an X row and a Y row of tapped holes (`holeRow`, `spacing` apart, starting `xHoleStart` and `yHoleStart` from the start), a center hole if it has one, and its thread. Every hole counts for tying, in either row: `tieStart` is the nearer row's start, and `tieUnit` the distance between the rows' starts (or the spacing, if they start together). |
| `robotNutStripEditLogic` | Above. |

### Data flow

The table entry (vendor, size, thread, dimensions, stock lengths, appearance) becomes the `Stock` unchanged except for
its holes: `tapDrillDiameter` is their diameter, and the thread entry itself is the `thread` they're tapped with.
Nothing else is read from the definition besides the table path and linear stock's parameters.

### Error handling

Only linear stock's.

### `try`s

None of its own, and no fallbacks.

## Issues found

- Nut strips are given Aluminum - 6061 as their material whatever they're made of.
