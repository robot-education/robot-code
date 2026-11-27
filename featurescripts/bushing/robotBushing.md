# Robot bushing

Cuts bores for bushings from FRCDesign's library, one at each sketch point, and declares each for FRCDesignApp's Auto
Assembly (see `docs/auto-assembly.md`), with an Auto Assembly body: Robot bearing's placement.

| File | What it is |
| --- | --- |
| `robotBushing.fs` | The feature: its dialog |
| `../core/bearing.fs` | Position, the bores, the declarations, and the Auto Assembly bodies, shared with Robot bearing |
| `../released/bearing/robotBearingTables.py` | The bushings part set's bushings (`bushingTable`), from `../bushings/bushingTables.py` |
| `../bushings/bushings.fs`, `bushingTables.gen.fs` | FRCDesign's bushings part set: the bushings' sizes, flanges, steps and bores (`BUSHINGS`, `BUSHING_BORES`) |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot bushing | feature name |
| Cut bores for bushings. `<CREDIT>` | feature description |
| `<bushing's library name>` | each Auto Assembly body's part name |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Auto Assembly body | Model a simple body where the part goes, which FRCDesign's Auto Assembly swaps for the part. (`core/autoAssembly.fs`'s) |

Editing logic sets the Merge scope and Flip primary axis (`bearingEditLogic`).

### Errors, warnings, and info

Robot bearing's, with "Choose a different bushing." for a saved Bushing path that isn't a whole table entry.

## How it works

Robot bearing's (see `../bearing/robotBearing.md`), in its groups (Bushing; Position; Other options). It has no
Bore fit: a COTS bushing is pressed in, so its bore is its outside diameter.

1. The bushing (`libraryBushing`): its `bushingTable` entry (Vendor, Bore, Size, Material, and Dimensions, skipping a
   level with one option), looked up in the bushings set's `BUSHINGS` for its outside diameter, width, and flange,
   with its hole at its bore's corners (the bearings' `boreRadius`), as a `BearingPart`.
2. The planes, and `placeBearings`, declaring each as a bushing. Until the bushings set is released, its studio has no
   document, so it's declared nothing, with Robot bearing's info.

## Issues found

- None of the geometry has run in Onshape.
- Only the bushings the set offers (see `../bushings/bushings.md`).
