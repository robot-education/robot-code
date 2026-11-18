# Robot bushing

Cuts a bore for a bushing from FRCDesign's library at a sketch point, and declares the bushing for FRCDesignApp's Auto
Assembly (see `docs/auto-assembly.md`), with an Auto Assembly body: Robot bearing's placement, without its retention.

| File | What it is |
| --- | --- |
| `robotBushing.fs` | The feature: its dialog |
| `../core/bearing.fs` | Position and Bore, the bore, the declaration, and the Auto Assembly body, shared with Robot bearing |
| `../released/bearing/robotBearingTables.py` | FRCDesignLib's bushings (`bushingTable`), from `vendor/frcdesign-bushings.json` |

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

Robot bearing's steps 2, 3 and 5, in its groups (Bushing: the bushing and Bore fit; Position) (see `../released/bearing/robotBearing.md`): the bushing from `bushingTable` (Type,
Size, Vendor), the plane, and `placeBearing`, declaring it as a bushing.

The library's Flanged Bushing with a hex outside (0.375 in. ID x 1/2 in. hex OD) isn't offered: its bore would be a
hex.

## Issues found

- None of the geometry has run in Onshape.
- The Auto Assembly body has no flange or step: the library's sizes give a step's outside diameter for two, and a flange's for
  two, but not their thicknesses.
