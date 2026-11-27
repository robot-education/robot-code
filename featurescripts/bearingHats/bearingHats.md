# Bearing hats

## Summary

FRCDesign's library bearing hats, generated for FRCDesignApp: `libraryBearingHat`, WCP's bearing retention plates, built
in one Part Studio whose configuration sets every parameter, in place of FRCDesign's hand-made Bearing Retention Hat
(WCP). Files: `bearingHats.fs` (the feature), `hatTables.py` (the catalog, read from FRCDesign's records in
`vendor/frcdesign-hats.json` and refreshed by `uv run python featurescripts/bearingHats/hatTables.py fetch`: the enums
and data in `hatTables.gen.fs`, the Part Studio's configuration, and FRCDesign's manifest `bearingHats.gen.json`).

Unreleased. It's a part set (`[tool.fs.parts.bearingHats]`, see docs/parts.md), released into the FRCDesignLib document
the fasteners and bearings are, its Part Studio, Bearing Hat, built from its definition's `STUDIOS`, never by hand.
Robot bearing's COTS hats choose from it and declare it for Auto Assembly, with their screws seated on it
(`featurescripts/released/bearing/`).

## Strings

| Feature | Name | Description |
| --- | --- | --- |
| `libraryBearingHat` | Bearing hat | A bearing hat from FRCDesign's library, seated at the origin. |

The description ends with the credit.

| Message | When | Highlights | Closest std error |
| --- | --- | --- | --- |
| Selected vendor does not make this hat in this size, change the size. (error) | The vendor makes none for this bearing | Size | none |
| Selected vendor does not make this hat with these centers, change the centers. (error) | The vendor makes none of this size at these centers | Centers | none |

The configuration only offers what's made, so these show only in the feature's own dialog.

| Part | Name |
| --- | --- |
| Bearing hat | FRCDesign's record's name: `1.125" OD Bearing Retention Plate (2" C-C)` |

Each is Aluminum - 6061 (`ALUMINUM`), light grey as uncoated aluminum is, with West Coast Products as its Vendor and
its part number (WCP-0487, WCP-0327, WCP-0488, WCP-0328).

## How it works

The configuration asks Vendor (WCP alone, so it's fixed), then Size (the bearing's outer diameter: 1.125 in or 0.875
in) and Centers (2 in, 1.5 in or 1.25 in), each laid out by `LookupTable` as the bearings' are: an input shows only
where more than one of its options is made with the choices before it.

Each plate lies on the XY plane, above it:

1. Its outline is Robot bearing's standard hat's: a circle about the bore, 0.125 in past the bearing's outer
   diameter, and a 0.25 in lobe round each mounting hole, joined by tangents (`core/boundary.fs`).
2. It's extruded 0.09 in up.
3. Its bore, 0.125 in narrower than the bearing, so it reaches over the outer race, and its #10 free clearance
   mounting holes (0.201 in) are cut through.
4. A `mateConnector` at the origin points up, out of the face it sits on, with X toward a mounting hole; a
   `topMateConnector` is on its top.

## Issues found

- Every dimension is an estimate: WCP publishes none. The thickness is the one that gives WCP-0328's listed 0.012 lb
  of 6061 with this outline. Confirm each against FRCDesignLib's model, and fix `SHAPE` in `hatTables.py`.
- None of the geometry has run in Onshape.
