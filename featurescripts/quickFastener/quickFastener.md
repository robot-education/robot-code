# Quick fastener

## Summary

The screws, washers, and nuts FRCDesign's Quick fastener inserts into assemblies: `quickFastenerScrew`,
`quickFastenerWasher`, and `quickFastenerNut`, each built in a Part Studio of its own whose configuration sets every
parameter. Files: `quickFastener.fs` (the features), `quickFastenerTables.py` (the catalog: the enums and dimensions in
`quickFastenerTables.gen.fs`, the Part Studios' configurations, and FRCDesign's manifest `quickFastener.gen.json`), and
`quickFastenerHoleScan.fs` (what FRCDesign evaluates in the Part Studios of the parts it fastens).

Unreleased.

## Strings

| Feature | Name | Description |
| --- | --- | --- |
| `quickFastenerScrew` | Quick fastener screw | A screw for FRCDesign's Quick fastener, seated at the origin. |
| `quickFastenerWasher` | Quick fastener washer | A washer for FRCDesign's Quick fastener, from the origin up. |
| `quickFastenerNut` | Quick fastener nut | A nut for FRCDesign's Quick fastener, seated at the origin. |

Each description ends with the credit.

| Message | When | Highlights | Closest std error |
| --- | --- | --- | --- |
| Length must be longer than the head, increase the value. (error) | A flat head screw whose length doesn't reach past its countersunk head | Length | `SHEET_METAL_HEM_TOO_SHORT`: Cannot create hem, increase total length. |

| Part | Name |
| --- | --- |
| Screw | `<size> x <length> <head> Screw`, e.g. `#10-32 x 0.5 in. Button Head Screw` |
| Washer | `<size> <type> Washer`, e.g. `#10-32 Flat Washer` |
| Nut | `<size> <type> Nut`, e.g. `M3 Nylock Nut` |

Each is steel, with the black appearance.

## How it works

Every part is built along the Z axis, and has one mate connector at the origin, pointing out of the hole: FRCDesign
fastens it to the hole's edge in the assembly. A screw's and a nut's Washer moves the mate connector down by that
washer's thickness (`washerThickness`), so it seats on the washer fastened to the same edge. Parameters are never
missing, and every combination of them builds: Length is free, though the configurations offer only the lengths each
size comes in.

`quickFastenerScrew`:

1. Revolves its profile (`screwProfile`, in `PROFILE_PLANE`) a full turn about Z. A socket or button head is above the
   origin, from its underside; a flat head is below it, its top flush with the origin, and its length includes it. A
   button head's dome is an arc (`buttonDome`) from the axis to the head's edge, a quarter of its height up.
2. Cuts a hex socket, `SOCKET_DEPTH` of the head's height deep, from the top of the head.
3. Adds the mate connector, then deletes the sketches and names the part.

`quickFastenerWasher` and `quickFastenerNut` extrude a ring (a hexagon for a nut, with a hole the screw's major
diameter) up from the origin, add the mate connector, and name the part.

Nothing is threaded, and nylock nuts have no nylon insert: the parts are for fit and clearance.

`quickFastenerHoleScan` isn't a feature: FRCDesign evaluates its body as a lambda, so it uses only std's common.fs.
It reports each solid part's cylindrical faces which face their axis (holes), with their extent along it, their
circular edges, whether
the part goes on past each end, and a Hole feature's thread; and each selected circular edge (or circular edge of a
selected face), with the normal of the plane beside it.

There are no `try`s.

## Issues found

- Untried in Onshape: the geometry, the mate connectors' placement, and `quickFastenerHoleScan`'s evaluation.
