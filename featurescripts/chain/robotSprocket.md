# Robot sprocket

Makes #25 (and #25H, the same), #35, or 8mm (05B) sprockets: ISO 606 teeth, as wide as the chain's (0.93 of its inner width), at a sketch
point, or wherever a Robot chain goes around a sprocket, with a bore; or with Library sprocket, stand-ins for
FRCDesignLib's sprockets, which FRCDesign's Auto Assembly swaps in.

| File | What it is |
| --- | --- |
| `robotSprocket.fs` | The feature: its dialog, and the sprockets |
| `chainCommon.fs` | Chain sizes, the tooth form (`getSprocketToothForm`, `sketchSprocketProfile`), and the attributes |
| `sprocketProducts.py` | FRCDesignLib's sprockets (generating `sprocketProducts.gen.fs`), from a snapshot in `vendor/` |
| `../core/autoAssembly.fs` | Stand-ins, and their declarations |
| `../core/bore.fs` | The bore: its parameters, and cutting it |
| `../core/location.fs`, `../core/startOffset.fs`, `../core/profileOffset.fs` | The location, starting offset, and profile offset |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot sprocket | feature name |
| Create #25, #25H, #35, and 8mm sprockets. `<br>`See also the Robot chain FeatureScript, which works with this feature directly. `<CREDIT>` | feature description |
| `<teeth>`T `<#25, #25H, #35, or 8mm>` Sprocket | part name |
| Library sprocket, Sprocket (Vendor, Bore, Chain type, Teeth), Vendor and bore | Library sprocket's checkbox, group, and tables (Manual's, and Chain's) |

A stand-in is named and numbered as its product (like `16t Aluminum Sprocket (#25, 1/2" Hex Bore, Double Hub)`).

Its material is Onyx, and its appearance printed green (as Robot pulley's).

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Width (`hexWidth`) | Across the hex's flats. |
| Diameter (`boreDiameter`) | The diameter of the shaft it goes on. |
| Fit (`fit`), Clearance (`fitClearance`) | (`core/fit.fs`'s) |
| Allow simplification (`allowSimplification`) | Builds it simplified while the Part Studio's Simplified configuration variable is on. |
| Library sprocket (`librarySprocket`) | Builds a stand-in for one of FRCDesignLib's sprockets, which FRCDesign's Auto Assembly swaps in. |

No parameters are hidden.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select a sketch point, circle, or mate connector to use. | error | Manual, no location | `location` |
| Select a Robot chain's curved faces or mate connectors. | error | Chain, nothing selected | `chainSelections` |
| Select a curved face or mate connector of a chain made by Robot chain. | error | a selection without a `ChainFaceAttribute` | `chainSelections`, it |
| The selection is around one of the chain's idlers, not a sprocket. | error | an idler's face | `chainSelections`, it |
| The profile offset is too large for this chain. | error | the offset leaves no seating curve | `profileOffsetDistance` |
| A `<teeth>` tooth sprocket's teeth don't fit this chain. | error | the flanks can't reach a tip | `teeth` |
| Failed to cut bore. / The bore is bigger than the part. / Failed to chamfer bore. | error | (`core/bore.fs`) | the bore's parameters, and what failed |
| `<vendor>` has no `<teeth>`T `<chain>` sprocket with a `<bore>` bore: its `<chain>` ones have `<teeth>`T, ... (or: it has no `<chain>` sprockets with that bore.) | error | Chain with Library sprocket, the vendor has none for a sprocket | `sprocketProduct`, and each such sprocket's pitch circle disc |

## How it works

### Execution order

1. **Precondition**: Creation method; Manual's location, or Chain's faces or mate connectors; Library sprocket, with
   Manual's table of sprockets or Chain's of vendors and bores; Manual's Chain type and Teeth (without Library
   sprocket) and starting offset; without Library sprocket, the bore (`borePredicate`), profile offset, Add mate
   connectors, and Allow simplification.
2. **Body**:
   1. The sprockets: Manual's one, at the location's plane with the starting offset; or Chain's, one per place the
      selected faces or mate connectors are around (`getChainSprockets`: a face's plane is its cylinder's, at its
      middle, `selectionPlane` in `core/location.fs`; selections whose centers are the same, within std's length
      tolerance, make one), with the chain's type and the sprocket's teeth from its `ChainFaceAttribute`.
   2. `createSprockets` sketches each one's profile (`sketchSprocketProfile`) and extrudes it the tooth width both ways
      from its plane, sets its `SprocketAttribute` (its chain type, teeth, and plane, which Robot chain reads) and
      properties.
   3. A profile offset manipulator, mate connectors (with the attribute too), and the bore (`cutBores`).
   With Library sprocket, Manual's sprocket is the library sprocket chosen, and Chain's each its vendor's product
   (`withLibraryProduct`), built as a stand-in (`addSprocketStandIn`); the body ends erroring if any has none
   (`reportMissingProducts`).
3. **Manipulator change** (`robotSprocketManipulatorChange`): the profile offset's and starting offset's.

The tooth form (ISO 606, with the most room for the chain): each gap's seating curve is an arc around its roller's
center on the pitch circle, of radius `0.505 d1 + 0.069 d1^(1/3)` (mm, `d1` the roller's diameter), `140° - 90°/z`
around; each flank's an arc tangent to it, of radius `0.008 d1 (z² + 180)`, out to a tip circle halfway between the
largest and smallest ISO 606 allows. A tooth whose flanks would cross below the tip circle is cut off where they're a
tenth of the tooth apart (found by bisection). The profile offset moves every curve outward (seating curves shrink,
flanks and tips grow).

### Library sprocket

With Library sprocket, the library sprocket is the source of truth. In Manual, a table chooses it
(`librarySprocketTable`: vendor, bore, chain, and teeth, each level left out where it has one option), replacing Chain
type and Teeth. In Chain mode, the chain has set each sprocket's chain and teeth, so a table chooses a vendor and a
bore (`sprocketProductTable`), and each sprocket is that vendor's first product for its chain (#25's for #25H) with
its teeth (`SPROCKET_PRODUCTS`, from `sprocketProducts.py`). Its stand-in
(`addStandIn`, `core/autoAssembly.fs`) is its pitch circle's disc, as wide as its teeth (library sprockets' widths
aren't in their records), with a hole the shaft fits in (a hex's across its corners), named and numbered as its
product, out of the bill of materials and translucent, with `bottom` and `top` mate connectors on its faces; it and
they carry the declaration (`frcAutoAssembly`) and its `SprocketAttribute`, so a Robot chain goes around it. The
bore, profile offset, mate connectors and simplification are hidden, and off. See `docs/auto-assembly.md`.

### Simplification

Allow simplification (`core/simplify.fs`), in Other options: while the Part Studio's Simplified configuration
variable is on, each sprocket is its pitch circle (with the profile offset), without teeth, and the rest (bore, mate
connectors, attributes) as ever.

## Issues found

- Untested in Onshape: nothing here has run yet.
- No hub: a sprocket is its toothed plate, as wide as its teeth.
- Stand-ins' connectors' X isn't checked against a library sprocket's tooth (FRCDesignLib's sprockets have no
  connectors yet), and FRCDesignApp doesn't read declarations yet.
- The icon is the generic robot icon.
