# Bearings

## Summary

FRCDesign's library ball bearings, generated for FRCDesignApp: `libraryBearing`, built in one Part Studio whose
configuration sets every parameter, in place of FRCDesign's hand-made Flanged Radial Bearing and Radial Bearing.
Files: `bearings.fs` (the feature), `bearingTables.py` (the catalog, read from FRCDesign's two bearings in
`vendor/frcdesign-bearings.json` and refreshed by `uv run python featurescripts/bearings/bearingTables.py fetch`: the
enums and data in `bearingTables.gen.fs`, the Part Studio's configuration, and FRCDesign's manifest
`bearings.gen.json`). `vendor/` also holds REV's drawings and AndyMark's listed specifications, which the flanges come
from.

Unreleased. It's a part set, not released to the frontend document: its Part Studio (Bearing) is built from
`bearingTables.py`'s `STUDIOS`, never by hand. It isn't declared in `pyproject.toml` yet: that needs the FRCDesignLib
document it's released into (`main`).

## Strings

| Feature | Name | Description |
| --- | --- | --- |
| `libraryBearing` | Bearing | A ball bearing from FRCDesign's library, seated at the origin. |

The description ends with the credit.

| Message | When | Highlights | Closest std error |
| --- | --- | --- | --- |
| Selected vendor does not make this type of bearing, change the type. (error) | The vendor makes no bearing of this type | Type | none |
| Selected vendor does not make this bearing with this bore, change the bore. (error) | The vendor makes none of this type with this bore | Bore | none |
| Selected vendor does not make this bearing in this size, change the size. (error) | The vendor makes none of this type and bore in this size | Size | none |
| Selected vendor does not make this bearing, change the dimensions. (error) | The vendor makes none of these dimensions in this type, bore, and size | Dimensions | none |
| Selected vendor does not make this bearing with this seal, change the seal. (error) | The vendor makes these dimensions with another seal alone | Seal | none |

The configuration only offers what's made, so these show only in the feature's own dialog.

| Part | Name |
| --- | --- |
| Bearing | `<size> <bore> <type> Bearing (<OD> OD x <width>)`, with any seal but shields after its dimensions: `1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)`, `3/8 in Round Radial Bearing (0.875 in OD x 0.281 in, Sealed)`, `8 mm Round Flanged Bearing (16 mm OD x 5 mm)`; R166ZZEE's dimensions are `0.375 in OD x 0.156 in (extended inner race)` |

Each is steel, in the standards' steel grey, with a sealed bearing's seals black. Each has its vendor as its Vendor,
its part number, and its link as its description, where the vendor has them; Generic has no vendor or link, and its
part number is the bearing's designation (`FR8ZZ-HexHD`) where FRCDesign gives one.

## How it works

The configuration asks Vendor, then Type (Flanged or Radial), Bore (Hex, Rounded hex, Round, or ThunderSquare), Size
(1/2 in and 3/8 in, then the rest from smallest), Dimensions (outer diameter by width), and Seal (Shielded, Sealed,
or Open), each laid out by `LookupTable` (see docs/fs-cli.md's "Configured Part Studios"): an input shows only where
the vendor makes more than one of its options with the choices before it, so Dimensions shows only where a size comes
in more than one, and Seal only where those dimensions come both shielded and sealed. An open bearing is narrower than
its shielded one (FR6 is 0.219 in wide, FR6ZZ 0.281 in), so it's chosen by its dimensions and its Seal is hidden. The
dialog is a flat list of the same enums, with no conditions.

What each vendor makes comes from FRCDesign's records, by each record's Dimensions option, which names its bore, outer
diameter, width, and seal (its record names disagree with it in places, such as WCP-2058's width). Generic makes every
bearing any vendor does. FRCDesign has F126ZZ 8 mm wide; it's 4 mm (am-3377).

The default is what's inserted most from FRCDesign's library: Generic's 1/2 in rounded hex flanged bearing, 1.125 in
OD x 0.313 in, shielded.

`libraryBearing`:

1. Resolves each choice with `resolveChoices`: a hidden input keeps whatever it was last set to, so it stands for the
   one option made there. Anything else not made is an error.
2. Revolves the bearing's half section (`bearingProfile`, in `PROFILE_PLANE`) a full turn about Z: a solid from the
   axis, with the shields between its races recessed 0.2 mm from each face. Each race is a quarter of the way from the
   bore's corners (`boreRadius`) to the outer diameter. A flanged bearing's flange is above the origin and the rest
   below it, so the origin is the flange's underside; a radial bearing is below the origin, its top face there. R166ZZEE's
   inner race (0.156 in) stands out of both faces of its outer race (0.125 in).
3. Cuts the bore through (`cutBore`): a circle, or a hexagon or square across its flats, with a round of `diameter`
   where its corners are rounded (13.75 mm for a 1/2 in rounded hex, 10.25 mm for 3/8 in, as Robot shaft rounds its
   shafts, and 0.302 in for a ThunderSquare); the bore is the sketch region around the axis.
4. Adds the mate connector at the origin, pointing up (`id + "mateConnector"`), deletes the sketches, and sets the
   part's properties (`setBearingProperties`), then colors a sealed bearing's seals black.

Bores are their shafts' nominal sizes, with no clearance. Balls aren't modeled: the parts are for fit and clearance.

There are no `try`s.

## Issues found

- Not yet tried in Onshape: none of it has regenerated there.
- Assumed flanges, marked `TODO` in `bearingTables.py`: the 3/4 in bearing's (WCP-0782, 217-2789), Swyft's 8 x 14 mm,
  ThriftyBot's 8 x 16 x 6.3 mm, and the 30 and 35 mm thin-section bearings'. The rest are from REV's drawings,
  AndyMark's specifications, or the designation's standard.
- Flanges are by size and dimensions, not vendor: AndyMark's FR6ZZL-Hex flange is 0.055 in thick, its others 0.06 to
  0.065 in, and the table takes REV's 0.063 in for all 1.125 in OD bearings.
- FRCDesignApp reads no part set's manifest yet, so nothing there uses `bearings.gen.json`.
