# Robot bushing

Cuts a bore for a bushing from FRCDesign's library at a sketch point, and declares the bushing for FRCDesignApp's Auto
Assembly (see `docs/auto-assembly.md`), with an Auto Assembly body: Robot bearing's placement, without its retention.

| File | What it is |
| --- | --- |
| `robotBushing.fs` | The feature: its dialog |
| `../core/bearing.fs` | Position and Bore, the bore, the declaration, and the Auto Assembly body, shared with Robot bearing |
| `../released/bearing/robotBearingTables.py` | The bushings part set's bushings (`bushingTable`), from `../bushings/bushingTables.py` |
| `../bushings/bushings.fs`, `bushingTables.gen.fs` | FRCDesign's bushings part set: the bushings' sizes, flanges, steps and bores (`BUSHINGS`, `BUSHING_BORES`) |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot bushing | feature name |
| Cut a bore for a bushing. `<CREDIT>` | feature description |
| `<bushing's library name>` | the Auto Assembly body's part name |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Auto Assembly body | Model a simple body where the part goes, which FRCDesign's Auto Assembly swaps for the part. (`core/autoAssembly.fs`'s) |

Editing logic sets the Merge scope and Flip primary axis (`mountingEditLogic`).

### Errors, warnings, and info

Robot bearing's, but for its retention's, with "Choose a different bushing." for a saved Bushing path that isn't a
whole table entry.

## How it works

Robot bearing's steps 2, 3 and 5 (see `../released/bearing/robotBearing.md`), in its groups (Bushing: the bushing and
Bore fit; Position):

1. The bushing (`libraryBushing`): its `bushingTable` entry (Bore, Size, Material, Dimensions, and Vendor, skipping a
   level with one option), looked up in the bushings set's `BUSHINGS` for its outside diameter, width, flange, and any
   step, with its hole at its bore's corners (the bearings' `boreRadius`), as a `BearingPart`.
2. The plane, and `placeBearing`, declaring it as a bushing: a bushing with a step gets a counterbore for it, at the
   same fit, and its Auto Assembly body has the step under its flange. Until the bushings set is released, its studio
   has no document, so it's declared nothing, with Robot bearing's info.

## Issues found

- None of the geometry has run in Onshape.
- Only the bushings the set offers (see `../bushings/bushings.md`).
