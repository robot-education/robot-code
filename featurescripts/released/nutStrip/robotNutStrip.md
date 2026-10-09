# Robot nut strip

Adds a nut strip, a solid bar of tapped holes for bolting through tube, along an edge (such as an inside edge of tube),
or extrudes one from a point. The placing and building are linear stock's; see [linearStock.md](../core/linearStock.md),
which this writeup doesn't repeat.

| File | What it is |
| --- | --- |
| `robotNutStrip.fs` | The feature: its dialog, the chosen nut strip as a `Stock`, and its editing logic |
| `nutStripTables.py` | The nut strip tables' source; `fs gen` writes `nutStripTables.gen.fs` |
| `../core/linearStock.fs` | Placing and building stock, and the Position and Tie holes to end groups |
| `vendor/` | Vendors' drawings and models the tables were checked against |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot nut strip | feature name |
| Add a nut strip along an edge, such as an inside edge of tube, or extrude one from a point. `<CREDIT>` | feature description |

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
   `stockEditLogic` with the nut strip's `tieStart` as the default offset, and whether the program or nut strip
   changed: turning on an end's offset (or changing the nut strip) sets the offset to the distance from the end to the
   closest hole, unless it's been set.
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
