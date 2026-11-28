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
released to the frontend document: each Part Studio is built from its definition's `STUDIOS`, never by hand. FRC's set
has two, Bearing and Specialty Bearing (below), FTC's one, Bearing.

Each Part Studio fixes the feature's Catalog (FRC, FRC specialty, or FTC) to its own, and the feature looks its bearing
up in that catalog's alone: a configuration hides an input where its studio makes one option, and the feature resolves
it to that one, which it couldn't from every studio's bearings, where another studio can make more there.
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

Every ball bearing has the same colors, whatever its vendor's model has or it's made of: its races in the standards' steel grey
(#B3B3B3), with shields of #ABA99C or black seals between them, as WCP's models color them (WCP-0785). Each is steel, but some of McMaster-Carr's, which are 440C stainless.
Each has its vendor
as its Vendor, its part number, and its link as its description, where the vendor has them; Generic has no vendor or
link, and its part number is the bearing's designation (`FR8ZZ-HexHD`) where FRCDesign gives one.

## How it works

FRC's bearings are split between two Part Studios, so the long tail of sizes doesn't crowd the lists teams use: Bearing
offers the standard sizes (`STANDARD`), those inserted from FRCDesign's library 150 times or more in a year (98% of its
flanged bearings' uses, and 90% of its radial bearings'), and the flanged bearings clearing a #10 screw (5 x 11 x 4 and
5 x 13 x 4 mm, and REV's #10 clearance bearing); Specialty Bearing offers the rest, every one a round bore. A size is
standard whatever its vendor and seal, and each bearing is in exactly one of them. Both build `libraryBearing`, and the
manifest lists both, each with its records. FTC's are few enough for one, Bearing.

Each configuration asks Vendor, then Type (Flanged or Radial), Bore (Hex, Rounded hex, Round, or REX), Size
(1/2 in and 3/8 in, then the rest from smallest), Dimensions (outer diameter by width), and Seal (Shielded, Sealed,
or Open), each laid out by `LookupTable` (see docs/fs-cli.md's "Configured Part Studios"): an
input shows only where the vendor makes more than one of its options with the choices before it, so Dimensions shows
only where a size comes in more than one, and Seal only where those dimensions come both shielded and sealed. An open
bearing is narrower than its shielded one (FR6 is 0.219 in wide, FR6ZZ 0.281 in), so it's chosen by its dimensions and
its Seal is hidden. The dialog is a flat list of the same enums, with no conditions.

What FRC's vendors make comes from FRCDesign's records, by each record's Dimensions option, which names its bore, outer
diameter, width, and seal (its record names disagree with it in places, such as WCP-2058's width), but for parts their
vendors no longer sell (`DISCONTINUED`): those AndyMark's store tags Discontinued, WCP-0782 and 217-5830, which WCP's
store no longer lists, and every VEX bearing, since VEX sells none. Generic makes every bearing any of them does, and
every standard one FRCDesign's Generic records name by its designation, so a standard bearing only a discontinued part
was (1621ZZ, say) is still Generic's. A Generic record with no designation is a vendor's own bearing (a rounded hex,
say), Generic's only while some vendor sells it: VEX's ThunderSquare bearing and the 3/4 in flanged bearing WCP and VEX
sold are gone. FRCDesign has F126ZZ 8 mm
wide; it's 4 mm (am-3377's model).

McMaster-Carr's bearings come from `vendor/mcmaster-bearings.json`, transcribed from its tables by hand, since its pages
refuse automated reads: its flanged steel bearings with 5, 6, and 8 mm bores (5 mm clears a #10 screw), shielded and
open, each with its flange; its open radial R3, R4, R6, R8, 635, 608, 6805,
and 6806; its 5 and 7 mm radial bearings, shielded, sealed, and open; and its shielded radial bearings in the sizes the
catalog's other vendors sell (R3, R4, R4A, R6, R8, 608, 6000, 6001, 6702, 6703, 6801 to 6803, 6805 to 6807, and
6902). Where it sells a bearing more than one way (steel or 440C stainless, or another ABEC rating), the cheapest is
offered, and is steel or stainless as that one is. One costing more than $20 (`MCMASTER_MAX_PRICE`) is offered only
where another vendor sells the same bearing (its 6703, 6806 flanged, and 6807, as WCP, REV, and ThriftyBot do): a size
no one else sells at that price is a specialty bearing teams don't buy, so its flanged 6805 and its sealed 607 aren't
offered. Nor are its bearings with no ABEC rating, rated to 5,000 rpm at most, or its 316 stainless ones, to 2,300 rpm. Each joins both sets' catalogs, FTC's its metric ones alone. A flange only McMaster-Carr's table gives is
Generic's too, as Generic offers every bearing a vendor does.

FTC's are goBILDA's flanged and radial ball bearings (from its store and STEP files), REV's 8 x 12 x 3.5 mm flanged
bearing, and AndyMark's bearings its store tags for FTC (All FTC Parts, and its Robits line): MF115ZZ, F126ZZ, F6702ZZ, 695ZZ, and
its 3/8 in hex sealed flanged bearing. Both vendors' pulley bearings (REV-41-1368, am-4836) are V-grooved, and REV's Through Bore
Bearings are acetal bushings, so none is offered: bushings are a part of their own.

The defaults are what's inserted most: from FRCDesign's library, Generic's 1/2 in rounded hex flanged bearing, 1.125 in
OD x 0.313 in, shielded, and of its specialty bearings, Generic's 8 x 22 x 7 mm flanged one; from ConfigLib, goBILDA's 8 mm round flanged bearing, 14 mm OD x 5 mm. FTC's vendors are
goBILDA, REV, AndyMark, then McMaster-Carr. goBILDA names only its 12 mm radial bearing sealed, and the rest are taken as shielded.

Flanges are each vendor's where its model or drawing has them (`VENDOR_FLANGES`), and otherwise by size and dimensions
(`FLANGES`): REV's drawings, AndyMark's specifications, goBILDA's, WCP's, Swyft's, and ThriftyBot's STEP files, or the
designation's standard. None is assumed.

`libraryBearing`:

1. Resolves each choice with `resolveChoices`: a hidden input keeps whatever it was last set to, so it stands for the
   one option made there. Anything else not made is an error.
2. Revolves the bearing's half section (`bearingProfile`, in `PROFILE_PLANE`) a full turn about Z: a solid from the
   axis, with the shields between its races recessed a sixteenth of its width from each face. Each race is 0.3 of the
   way from the bore's corners (`boreRadius`) to the outer diameter, as REV's and AndyMark's models have them. A
   flanged bearing's flange is above the origin and the rest below it, so the origin is the flange's underside; a
   radial bearing is below the origin, its top face there. R166ZZEE's inner race (0.156 in) stands out of both faces of
   its outer race (0.125 in).
3. Cuts the bore through (`cutBore`): a circle, or a hexagon across its flats, with a round of `diameter` where its
   corners are rounded (13.75 mm for a 1/2 in rounded hex, 10.25 mm for 3/8 in, as Robot shaft rounds its shafts); the
   bore is the sketch region around the axis. goBILDA's 8 mm REX bore
   is a 7 mm hex with sharp corners, as its STEP file has it.
4. Adds its mate connectors on the axis, each pointing out of the part it's on: where it seats, at the origin pointing
   up (`id + "mateConnector"`); a flanged bearing's top, on its flange's top face, pointing up
   (`id + "topMateConnector"`); and its bottom, on its bottom face, pointing down (`id + "bottomMateConnector"`). What's
   stacked on its shaft (a spacer, a collar, another bearing) sits against these. A radial bearing's top is where it
   seats, so it makes no top connector: its records in the manifest name `mateConnector` as their `topMateConnector`,
   in place of the studio's (see docs/parts.md). Then it deletes the sketches, sets the part's properties
   (`setBearingProperties`), and colors a shielded bearing's shields and a sealed one's seals.

Bores are their shafts' nominal sizes, with no clearance, as the vendors' models have them. Balls aren't modeled: the
parts are for fit and clearance.

There are no `try`s.

## Issues found

- Not yet tried in Onshape: none of it has regenerated there, and no connector has been fastened to.
- AndyMark's FR82RS model (am-4465) is 0.375 in wide; AndyMark lists it, and FRCDesign has it, 0.313 in, as built.
- goBILDA's 12 x 32 x 10 mm flanged bearing (1601-1032-0012) narrows between its body and its 2.3 mm flange in its STEP
  file, and the vendors' 1.125 in OD flanged bearings are undercut under their flanges; both are built as a plain step.
- McMaster-Carr's shielded radial bearings are transcribed for 5 and 7 mm bores and the sizes the other vendors sell;
  its other sizes (3 and 4 mm bores, its 6200 and 6300 series, R10 and up) aren't offered.
- ThriftyBot's, Swyft's, and SDS's availability is unchecked: only AndyMark's, WCP's, REV's, and goBILDA's stores
  were checked for discontinued parts.
- FRCDesignApp reads no part set's manifest yet, so nothing there uses `bearings.gen.json`.
