# Quick fastener

## Summary

The screws, washers, and nuts FRCDesign's Quick fastener inserts into assemblies: `quickFastenerScrew`,
`quickFastenerWasher`, and `quickFastenerNut`, each built in a Part Studio of its own whose configuration sets every
parameter. Files: `quickFastener.fs` (the features), `quickFastenerTables.py` (the catalog, from FRCDesign's Fasteners
group in `vendor/frcdesign-fasteners.json`, with WCP's and AndyMark's screws from their stores in `vendor/stores.json`,
refreshed by `uv run python featurescripts/quickFastener/quickFastenerTables.py fetch`: the enums and data in `quickFastenerTables.gen.fs`, the Part Studios'
configurations, and FRCDesign's manifest `quickFastener.gen.json`), and `quickFastenerHoleScan.fs` (what FRCDesign
evaluates in the Part Studios of the parts it fastens).

Unreleased. It's released to FRCDesign's Fasteners library document (`[tool.fs.libraries.fasteners]`), whose Part
Studios use it, rather than the frontend document.

## Strings

| Feature | Name | Description |
| --- | --- | --- |
| `quickFastenerScrew` | Quick fastener screw | A standard screw for FRCDesign's Quick fastener, seated at the origin. |
| `quickFastenerWasher` | Quick fastener washer | A washer for FRCDesign's Quick fastener, from the origin up. |
| `quickFastenerNut` | Quick fastener nut | A nut for FRCDesign's Quick fastener, seated at the origin. |

Each description ends with the credit.

| Message | When | Highlights | Closest std error |
| --- | --- | --- | --- |
| Selected vendor does not make this screw, change the length. (error) | The vendor makes no screw of this size, head, and length | Length | none |
| Selected vendor does not make this washer, change the type. (error) | The vendor makes no washer of this size and type | Type | none |
| Selected vendor does not make this nut, change the type. (error) | The vendor makes no nut of this size and type | Type | none |

The configurations only offer what's made, so these show only in the features' own dialogs.

| Part | Name |
| --- | --- |
| Screw | `<size> x <length> <head> Screw`, e.g. `#10-32 x 1/2 in. Button Head Screw` |
| Washer | `<size> Washer (<type>)`, e.g. `#10 Washer (1/2 in. OD x 0.049 in.)` |
| Nut | `<size> <type> Nut`, e.g. `#10-32 Thin Nylock Nut` |

Each is steel, with the black appearance, and has its vendor's part number, and its link as its description, where
the vendor has them (Generic has neither).

## How it works

Every part is built along the Z axis, and has one mate connector at the origin, pointing out of the hole, where it
seats: FRCDesign fastens a screw or nut to the top edge of the washer under it, or to the hole's edge, and a washer to
the hole's edge. So any washer goes under any screw or nut, and none has a washer input. Every part is one its vendor makes (`SCREWS`, `WASHERS`, `NUTS`), and lengths are
the standard ones (`ScrewLength`); anything else is an error.

`quickFastenerScrew`:

1. Revolves its profile (`screwProfile`, in `PROFILE_PLANE`) a full turn about Z. A socket or button head is above the
   origin, from its underside; a flat head is below it, its top flush with the origin, and its length includes it. A
   button head's dome is an arc (`buttonDome`) from the axis to the head's edge, a quarter of its height up.
2. Cuts a hex socket, `SOCKET_DEPTH` of the head's height deep, from the top of the head.
3. Adds the mate connector, then deletes the sketches and names the part.

`quickFastenerWasher` and `quickFastenerNut` extrude a ring (a hexagon or square for a nut, with a hole the screw's
major diameter) up from the origin, add the mate connector, and name the part.

Nothing is threaded, and nylock nuts have no nylon insert: the parts are for fit and clearance.

`quickFastenerHoleScan` isn't a feature: FRCDesign evaluates its body as a lambda, so it uses only std's common.fs. It
reports each solid part's cylindrical faces which face their axis (holes), with their extent along it, their circular
edges, whether the part goes on past each end, and a Hole feature's thread; and each selected circular edge (or
circular edge of a selected face), with the normal of the plane beside it.

There are no `try`s.

## Issues found

- Tried in Onshape (FeatureScript 3095, in a scratch Part Studio, not yet in the released studios): each kind of
  screw, washer, and nut regenerates as one part with its mate connector at the origin pointing up, named, and with
  its part number; and `quickFastenerHoleScan` reports a plate's through and blind holes, and the circles of a
  selected face, whose ids `qTransient` finds again. That found `qOwnerBody` passed to `transientQueriesToStrings`,
  which only names transient queries, so the scan failed whenever anything was selected; it now names the evaluated
  body.
- Nut heights for thin nylock, thin hex, and #4-40 nuts are nominal guesses, not from McMaster-Carr's drawings.
