# Bearings

## Summary

FRCDesign's library bearings, generated for FRCDesignApp: `libraryBearing`, built in one Part Studio whose
configuration sets every parameter, in place of FRCDesign's hand-made Flanged Radial Bearing and Radial Bearing, and of
ConfigLib's goBILDA Radial Bearing for FTC. Files: `bearings.fs` (the feature), `bearingTables.py` (the catalog, read
from FRCDesign's two bearings in `vendor/frcdesign-bearings.json` and refreshed by `uv run python
featurescripts/bearings/bearingTables.py fetch`, and FTC's vendors' bearings: the enums and data in
`bearingTables.gen.fs`, the Part Studio's configuration, and FRCDesign's manifest `bearings.gen.json`). `vendor/` holds
the vendors' STEP files and drawings (AndyMark's, REV's, and goBILDA's) and AndyMark's listed specifications, which the
flanges and bores are measured from.

FTC's bearings are the same feature and tables, offered by a Part Studio of their own, a second part set
(`[tool.fs.parts.ftcBearings]`), which `ftcBearingTables.py` configures from `bearingTables.py`'s `FTC` catalog, writing
its manifest, `ftcBearings.gen.json`, and released into ConfigLib's document. They're goBILDA's (the only bearings in
ConfigLib), REV's from its FTC catalog, and AndyMark's its store tags for FTC. Each set's studio offers its own
vendors' bearings alone.

Unreleased. They're part sets (`[tool.fs.parts.bearings]` and `[tool.fs.parts.ftcBearings]`, see docs/parts.md), not
released to the frontend document: each Part Studio (Bearing) is built from its definition's `STUDIOS`, never by hand.
FRC's is released into the FRCDesignLib document the fasteners are, FTC's into ConfigLib's.

## Strings

| Feature | Name | Description |
| --- | --- | --- |
| `libraryBearing` | Bearing | A bearing from FRCDesign's library, seated at the origin. |

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
| Bearing | `<size> <bore> <type> Bearing (<OD> OD x <width>)`, with any seal but shields after its dimensions: `1/2 in Rounded Hex Flanged Bearing (1.125 in OD x 0.313 in)`, `3/8 in Round Radial Bearing (0.875 in OD x 0.281 in, Sealed)`, `8 mm Round Flanged Bearing (16 mm OD x 5 mm)`, `8 mm REX Flanged Bearing (14 mm OD x 5 mm)`; R166ZZEE's dimensions are `0.375 in OD x 0.156 in (extended inner race)` |
| Plain bearing | `<size> <bore> <type> Plain Bearing (<OD> OD x <width>)`: `5 mm Hex Flanged Plain Bearing (9 mm OD x 5 mm)` |

A ball bearing is steel, in the standards' steel grey, with a sealed bearing's seals black; a plain bearing (REV's
Through Bore Bearings) is acetal, in black, a placeholder for its color, which REV doesn't publish. Each has its vendor
as its Vendor, its part number, and its link as its description, where the vendor has them; Generic has no vendor or
link, and its part number is the bearing's designation (`FR8ZZ-HexHD`) where FRCDesign gives one.

## How it works

The configuration asks Vendor, then Type (Flanged or Radial), Bore (Hex, Rounded hex, Round, ThunderSquare, or REX),
Size (1/2 in and 3/8 in, then the rest from smallest), Dimensions (outer diameter by width), and Seal (Shielded, Sealed,
Open, or None, a plain bearing's), each laid out by `LookupTable` (see docs/fs-cli.md's "Configured Part Studios"): an
input shows only where the vendor makes more than one of its options with the choices before it, so Dimensions shows
only where a size comes in more than one, and Seal only where those dimensions come both shielded and sealed. An open
bearing is narrower than its shielded one (FR6 is 0.219 in wide, FR6ZZ 0.281 in), so it's chosen by its dimensions and
its Seal is hidden. The dialog is a flat list of the same enums, with no conditions.

What FRC's vendors make comes from FRCDesign's records, by each record's Dimensions option, which names its bore, outer
diameter, width, and seal (its record names disagree with it in places, such as WCP-2058's width). Generic makes every
bearing any of them does. FRCDesign has F126ZZ 8 mm wide; it's 4 mm (am-3377's model).

FTC's are goBILDA's flanged and radial ball bearings (from its store and STEP files), REV's 8 x 12 x 3.5 mm flanged
bearing and its two acetal Through Bore Bearings for 5 mm hex (REV-41-1326, 5 mm overall, and REV-41-1329, 11 mm), and
AndyMark's bearings its store tags for FTC (All FTC Parts, and its Robits line): MF115ZZ, F126ZZ, F6702ZZ, 695ZZ, and
its 3/8 in hex sealed flanged bearing. Both vendors' pulley bearings (REV-41-1368, am-4836) are V-grooved, and aren't
offered.

The defaults are what's inserted most: from FRCDesign's library, Generic's 1/2 in rounded hex flanged bearing, 1.125 in
OD x 0.313 in, shielded; from ConfigLib, goBILDA's 8 mm round flanged bearing, 14 mm OD x 5 mm. FTC's vendors are
goBILDA, REV, then AndyMark. goBILDA names only its 12 mm radial bearing sealed, and the rest are taken as shielded.

Flanges are each vendor's where its model or drawing has them (`VENDOR_FLANGES`), and otherwise by size and dimensions
(`FLANGES`): REV's drawings, AndyMark's specifications, goBILDA's STEP files, or the designation's standard.

`libraryBearing`:

1. Resolves each choice with `resolveChoices`: a hidden input keeps whatever it was last set to, so it stands for the
   one option made there. Anything else not made is an error.
2. Revolves the bearing's half section (`bearingProfile`, in `PROFILE_PLANE`) a full turn about Z: a solid from the
   axis, with the shields between its races recessed a sixteenth of its width from each face. Each race is 0.3 of the
   way from the bore's corners (`boreRadius`) to the outer diameter, as REV's and AndyMark's models have them. A
   flanged bearing's flange is above the origin and the rest below it, so the origin is the flange's underside; a
   radial bearing is below the origin, its top face there. R166ZZEE's inner race (0.156 in) stands out of both faces of
   its outer race (0.125 in). A plain bearing is a solid body under its flange, with no races.
3. Cuts the bore through (`cutBore`): a circle, or a hexagon or square across its flats, with a round of `diameter`
   where its corners are rounded (13.75 mm for a 1/2 in rounded hex, 10.25 mm for 3/8 in, as Robot shaft rounds its
   shafts, and 0.302 in for a ThunderSquare); the bore is the sketch region around the axis. goBILDA's 8 mm REX bore
   is a 7 mm hex with sharp corners, as its STEP file has it.
4. Adds its mate connectors on the axis, each pointing out of the part it's on: where it seats, at the origin pointing
   up (`id + "mateConnector"`); a flanged bearing's top, on its flange's top face, pointing up
   (`id + "topMateConnector"`); and its bottom, on its bottom face, pointing down (`id + "bottomMateConnector"`). What's
   stacked on its shaft (a spacer, a collar, another bearing) sits against these. A radial bearing's top is where it
   seats, so it makes no top connector: its records in the manifest name `mateConnector` as their `topMateConnector`,
   in place of the studio's (see docs/parts.md). Then it deletes the sketches, sets the part's properties
   (`setBearingProperties`), and colors a sealed bearing's seals black.

Bores are their shafts' nominal sizes, with no clearance, as the vendors' models have them. Balls aren't modeled: the
parts are for fit and clearance.

There are no `try`s.

## Issues found

- Not yet tried in Onshape: none of it has regenerated there, and no connector has been fastened to.
- Assumed flanges, marked `TODO` in `bearingTables.py`: the 3/4 in bearing's (WCP-0782, 217-2789), ThriftyBot's
  8 x 16 x 6.3 mm, and Generic's and WCP's 30 and 35 mm thin-section bearings'. Swyft's 8 x 14 x 5 mm takes goBILDA's
  measured one. WCP, VEX, ThriftyBot, Swyft, and SDS publish no models these could be checked against.
- AndyMark's FR82RS model (am-4465) is 0.375 in wide; AndyMark lists it, and FRCDesign has it, 0.313 in, as built.
- goBILDA's 12 x 32 x 10 mm flanged bearing (1601-1032-0012) narrows between its body and its 2.3 mm flange in its STEP
  file, and the vendors' 1.125 in OD flanged bearings are undercut under their flanges; both are built as a plain step.
- AndyMark lists several FRC bearings as discontinued (am-0027, am-0029, am-0031, am-0936, am-2095, am-4149, am-4455,
  am-4459, am-4461, am-4464, am-4466 to am-4469); FRCDesign's records still offer them, and so does FRC's studio.
- FRCDesignApp reads no part set's manifest yet, so nothing there uses `bearings.gen.json`.
