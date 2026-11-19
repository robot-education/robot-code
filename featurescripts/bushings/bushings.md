# Bushings

## Summary

FRCDesign's library bushings, generated for FRCDesignApp: `libraryBushing`, built in one Part Studio whose configuration
sets every parameter, in place of FRCDesign's hand-made Flanged Bushing. Files: `bushings.fs` (the feature),
`bushingTables.py` (the catalog, read from FRCDesign's Flanged Bushing in `vendor/frcdesign-bushings.json` and refreshed
by `uv run python featurescripts/bushings/bushingTables.py fetch`: the enums and data in `bushingTables.gen.fs`, the Part
Studio's configuration, and FRCDesign's manifest `bushings.gen.json`). `vendor/` holds The Thrifty Bot's STEP files, and WCP's
bushings as FRCDesign models them (`wcp-bushings.json`), which the
flanges are measured from.

Unreleased. It's a part set (`[tool.fs.parts.bushings]`, see docs/parts.md), released into the FRCDesignLib document
the fasteners and bearings are, its Part Studio, Bushing, built from its definition's `STUDIOS`, never by hand. Robot
bushing chooses from it and declares its bushings for Auto Assembly (`featurescripts/bushing/`).

## Strings

| Feature | Name | Description |
| --- | --- | --- |
| `libraryBushing` | Bushing | A bushing from FRCDesign's library, seated at the origin. |

The description ends with the credit.

| Message | When | Highlights | Closest std error |
| --- | --- | --- | --- |
| Selected vendor does not make this bushing with this bore, change the bore. (error) | The vendor makes none with this bore | Bore | none |
| Selected vendor does not make this bushing in this size, change the size. (error) | The vendor makes none of this bore in this size | Size | none |
| Selected vendor does not make this bushing in this material, change the material. (error) | The vendor makes none of this bore and size in this material | Material | none |
| Selected vendor does not make this bushing, change the dimensions. (error) | The vendor makes none of these dimensions in this bore, size, and material | Dimensions | none |

The configuration only offers what's made, so these show only in the feature's own dialog.

| Part | Name |
| --- | --- |
| Bushing | `<size> <bore> Flanged <material> Bushing (<OD> OD x <width>)`: `1/2 in Rounded Hex Flanged Bronze Bushing (0.75 in OD x 0.313 in)`, `3/4 in Round Flanged Bronze Bushing (1.125 in OD x 0.333 in)` |

Bronze bushings are SAE 841 oil-impregnated bronze (`BRONZE` in `core/robotProperties.fs`), machined brass yellow;
nylon ones Nylon 6/6 (`NYLON`), general black, as ThriftyBot sells its in black only. Each has its vendor as its Vendor
and its part number where the vendor has one; Generic has neither.

## How it works

The configuration asks Vendor, then Bore (Rounded hex or Round), Size (1/2 in and 3/8 in, then the rest from
smallest), Material (Bronze or Nylon), and Dimensions (outer diameter by width), each laid out by `LookupTable` (see
docs/fs-cli.md's "Configured Part Studios") as the bearings' are: an input shows only where more than one of its
options is made with the choices before it. Every vendor makes both bores and several sizes of each, so Bore and Size
show for every bushing; Material shows for 1/2 in rounded hex, and Dimensions where a size comes in more than one. The
dialog is a flat list of the same enums, with no conditions.

What's made comes from FRCDesign's records, by each record's Dimensions option, which names its bore, outer diameter,
width, and any flange or material (`parse`), and a record's name says nylon where its option doesn't. A bushing
is offered only where a vendor's model or listing gives its flange (`FLANGES`):

| Bushing | Flange | Source |
| --- | --- | --- |
| 3/8 in rounded hex, 0.625 in OD x 0.375 in, bronze | 0.75 in, 0.0825 in thick | `TTB-0266.stp` |
| 1/2 in rounded hex, 0.75 in OD x 0.333 in, bronze | 0.875 in, 0.083 in thick | `TTB-0267.stp` |
| 1/2 in rounded hex, 0.75 in OD x 0.375 in, nylon | 0.875 in, 0.125 in thick | TTB-0004's store listing |
| 3/4, 7/8, and 1 in round, 1.125 in OD x 0.333 in, bronze | 1.225 in, 0.083 in thick | `TTB-0268.stp`, `TTB-0269.stp`, `TTB-0270.stp` |

| 1/2 in and 3/8 in rounded hex, 1/4, 3/8, 1/2, 3/4, and 1 in round, 0.313 in wide, bronze | 0.0625 in thick: 0.875, 0.75, 0.625, 0.75, 0.875, 1, and 1.25 in | `wcp-bushings.json`: FRCDesignLib's models of WCP-0999, 0997, 0995, 0996, 0998, 1000, and 1004, measured in Onshape |

The vendors offered are WCP and ThriftyBot (`VENDORS`), and Generic makes every bushing one of them does, as the
bearings' Generic does. VEX's aren't offered: VEX no longer sells bushings. Nor are REV's: it sells one, a 3/4 in
bushing with a step under its flange. Generic's of what only they sold (VEX's 1/2 in rounded hex nylon bushing 0.313 in
wide, REV's stepped one) aren't either. The 3/8 in ID bushing with a 1/2 in hex outside (WCP-0567) isn't offered: its bore would be a hex.

`libraryBushing`:

1. Resolves each choice with the bearings' `resolveChoices`: a hidden input keeps whatever it was last set to, so it
   stands for the one option made there. Anything else not made is an error.
2. Revolves its half section (`bushingProfile`, in `PROFILE_PLANE`) a full turn about Z: a solid from the axis, its
   flange above the origin, and its body below.
3. Cuts the bore through with the bearings' `cutBore`: a circle, or a hexagon across its flats rounded to 13.75 mm for
   1/2 in, 10.25 mm for 3/8 in (`BUSHING_BORES`), as Robot shaft rounds its shafts.
4. Adds its mate connectors on the axis, each pointing out of the part it's on, as the bearings' do: where it seats, the
   flange's underside at the origin pointing up (`mateConnector`); its flange's top, pointing up (`topMateConnector`);
   and its bottom, pointing down (`bottomMateConnector`). Then it deletes the sketches and sets the part's properties
   (the bearings' `setBearingProperties`).

Bores are their shafts' nominal sizes, with no clearance, as the vendors' models have them.

There are no `try`s.

## Issues found

- Not yet tried in Onshape: none of it has regenerated there, and no connector has been fastened to.
- FRCDesign names ThriftyBot's 3/8 in rounded hex bushing 0.333 in wide in its record, and 0.375 in in its option;
  TTB-0266's model is 0.375 in, as built.
- TTB-0268 to TTB-0270 measure 1.124 in OD in their models; they're built 1.125 in, as FRCDesign and ThriftyBot list them.
