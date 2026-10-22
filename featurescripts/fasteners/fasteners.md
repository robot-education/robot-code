# Fasteners

## Summary

FRCDesign's library fasteners, generated for FRCDesignApp, which inserts them from the library and into assemblies: `fastenerScrew`,
`fastenerWasher`, and `fastenerNut`, each built in a Part Studio of its own whose configuration sets every
parameter. Files: `fasteners.fs` (the features), `fastenerTables.py` (the catalog, from FRCDesign's Fasteners
group in `vendor/frcdesign-fasteners.json`, with WCP's and AndyMark's screws from their stores in `vendor/stores.json`,
refreshed by `uv run python featurescripts/fasteners/fastenerTables.py fetch`: the enums and data in `fastenerTables.gen.fs`, the Part Studios'
configurations, and FRCDesign's manifest `fasteners.gen.json`), and `fastenerHoleScan.fs` (what FRCDesign
evaluates in the Part Studios of the parts it fastens).

Unreleased. It's released to FRCDesign's Fasteners library document (`[tool.fs.libraries.fasteners]`), whose Part
Studios use it, rather than the frontend document. Those Part Studios (Screw, Washer, and Nut) are built from
`fastenerTables.py`'s `STUDIOS`, never by hand: `fs build-studios featurescripts/fasteners/fastenerTables.py` builds
them in the backend document to try a change, and `fs release fasteners` builds them in the library, versions it, and
records where they are in `fastenerTables.studios.json`, which the manifest's locations come from.

## Strings

| Feature | Name | Description |
| --- | --- | --- |
| `fastenerScrew` | Screw | A standard screw from FRCDesign's library, seated at the origin. |
| `fastenerWasher` | Washer | A washer from FRCDesign's library, from the origin up. |
| `fastenerNut` | Nut | A nut from FRCDesign's library, seated at the origin. |

Each description ends with the credit.

| Message | When | Highlights | Closest std error |
| --- | --- | --- | --- |
| Selected vendor does not make this screw, change the length. (error) | The vendor makes no screw of this size, head, and length | Length | none |
| Selected vendor does not make this screw in this variant, change the variant. (error) | The length is made, but neither in this variant nor the standard one | Variant | none |
| Selected vendor does not make this washer, change the type. (error) | The vendor makes no washer of this size and type | Type | none |
| Selected vendor does not make this washer in this finish, change the finish. (error) | The type is made, but neither in this finish nor black | Finish | none |
| Selected vendor does not make this nut, change the type. (error) | The vendor makes no nut of this size and type | Type | none |

The configurations only offer what's made, so these show only in the features' own dialogs.

| Part | Name |
| --- | --- |
| Screw | `<size> x <length> <head> Screw`, e.g. `#10-32 x 1/2 in. Button Head Screw`, with any variant but the standard after it: `... Socket Head Screw (Fully threaded)` |
| Washer | `<size> Washer (<type>)`, e.g. `#10 Washer (1/2 in. OD x 0.049 in.)`, with any finish but black after the type: `(7/16 in. OD x 0.03 in., Stainless steel)` |
| Nut | `<size> <type> Nut`, e.g. `#10-32 Thin Nylock Nut` |

Each has its material (steel, stainless steel, or aluminum, as its vendor sells it) and its finish's appearance (black,
zinc plated, stainless, or aluminum's gray: WCP doesn't publish its aluminum screws' color), and its vendor as its Vendor,
its part number, and its link as its description, where the vendor has them (Generic has none). `fastenerTables.py` names every configuration's part (its
`Record`s) and writes each into the tables, which the features set as they are, and into the manifest, which
FRCDesignApp indexes in place of asking Onshape: one record per configuration the Part Studio offers, so the two
can't disagree.

## How it works

Every part is built along the Z axis, and has one mate connector at the origin, pointing out of the hole, where it
seats: FRCDesign fastens a screw or nut to the top edge of the washer under it, or to the hole's edge, and a washer to
the hole's edge. So any washer goes under any screw or nut, and none has a washer input.

Each dialog is a flat list of enums, with no conditions: Vendor, Size, Head, one `ScrewLength` of every length any
vendor makes, and Variant, for a screw; Vendor, Size, Type, and Finish for a washer; Vendor, Size, and Type for a nut.
So each Part Studio wires every parameter to a configuration input of the same options one for one, and the
configuration's visibility conditions, not the dialog, show only what's made (Length shows the lengths the chosen vendor
makes in the chosen size and head). Every part is one its vendor makes (`SCREWS`, `WASHERS`, `NUTS`); anything else is
an error.

Variant and Finish are each part beyond its standard: a screw zinc plated, aluminum, or fully threaded (McMaster-Carr's
long socket heads, whose standard is partly threaded), and a washer in plain stainless steel. The configuration shows
each only where the vendor sells more than the standard, so never for Generic. Hidden, an input keeps whatever it was
last set to, so the features look a variant or finish up with `made`, which falls back to the standard one.

`fastenerScrew`:

1. Revolves its profile (`screwProfile`, in `PROFILE_PLANE`) a full turn about Z. A socket or button head is above the
   origin, from its underside; a flat head is below it, its top flush with the origin, and its length includes it. A
   button head's dome is an arc (`buttonDome`) from the axis to the head's edge, a quarter of its height up.
2. Cuts a hex socket, `SOCKET_DEPTH` of the head's height deep, from the top of the head.
3. Adds the mate connector, then deletes the sketches and names the part.

`fastenerWasher` and `fastenerNut` extrude a ring (a hexagon or square for a nut, with a hole the screw's
major diameter) up from the origin, add the mate connector, and name the part.

Nothing is threaded, and nylock nuts have no nylon insert: the parts are for fit and clearance.

`fastenerHoleScan` isn't a feature: FRCDesign evaluates its body as a lambda, so it uses only std's common.fs. It
reports each solid part's cylindrical faces which face their axis (holes), with their extent along it, their circular
edges, whether the part goes on past each end, and a Hole feature's thread; and each selected circular edge (or
circular edge of a selected face), with the normal of the plane beside it.

There are no `try`s.

## Issues found

- Tried in Onshape (FeatureScript 3095, in a scratch Part Studio, not yet in the released studios): each kind of
  screw, washer, and nut regenerates as one part with its mate connector at the origin pointing up, named, and with
  its part number; and `fastenerHoleScan` reports a plate's through and blind holes, and the circles of a
  selected face, whose ids `qTransient` finds again. That found `qOwnerBody` passed to `transientQueriesToStrings`,
  which only names transient queries, so the scan failed whenever anything was selected; it now names the evaluated
  body.
- Nut heights for thin nylock, thin hex, and #4-40 nuts are nominal guesses, not from McMaster-Carr's drawings.
