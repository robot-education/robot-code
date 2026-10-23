# Fasteners

## Summary

FRCDesign's library fasteners, generated for FRCDesignApp, which inserts them from the library and into assemblies: `fastenerScrew`,
`fastenerWasher`, and `fastenerNut`, each built in a Part Studio of its own whose configuration sets every
parameter. Files: `fasteners.fs` (the features), `fastenerTables.py` (the catalog, from FRCDesign's Fasteners
group in `vendor/frcdesign-fasteners.json`, with WCP's and AndyMark's screws from their stores in `vendor/stores.json`,
refreshed by `uv run python featurescripts/fasteners/fastenerTables.py fetch`: the enums and data in `fastenerTables.gen.fs`, the Part Studios'
configurations, and FRCDesign's manifest `fasteners.gen.json`), and `fastenerHoleScan.fs` (what FRCDesign
evaluates in the Part Studios of the parts it fastens).

Unreleased. It's a part set (`[tool.fs.parts.fasteners]`, see docs/parts.md), not released to the frontend document:
its Part Studios (Screw, Washer, and Nut) are built from `fastenerTables.py`'s `STUDIOS`, never by hand. `fs parts
fasteners --test` builds them in the backend document as `Screw (TEST)` and so on, to try a change, and `fs parts
fasteners --release` releases them into FRCDesignLib's Fasteners document, recording where they are in
`fastenerTables.studios.json`, which the manifest's locations come from.

## Strings

| Feature | Name | Description |
| --- | --- | --- |
| `fastenerScrew` | Screw | A standard screw from FRCDesign's library, seated at the origin. |
| `fastenerWasher` | Washer | A washer from FRCDesign's library, from the origin up. |
| `fastenerNut` | Nut | A nut from FRCDesign's library, seated at the origin. |

Each description ends with the credit.

| Message | When | Highlights | Closest std error |
| --- | --- | --- | --- |
| Selected vendor does not make this screw in this finish, change the finish. (error) | The vendor makes no screw of this size and head in this finish | Finish | none |
| Selected vendor does not make this screw, change the length. (error) | The vendor makes no screw of this size, head, finish, and length | Length | none |
| Selected vendor does not make this washer, change the type. (error) | The vendor makes no washer of this size and type | Type | none |
| Selected vendor does not make this washer in this finish, change the finish. (error) | The type is made, but neither in this finish nor black | Finish | none |
| Selected vendor does not make this nut, change the type. (error) | The vendor makes no nut of this size and type | Type | none |

The configurations only offer what's made, so these show only in the features' own dialogs.

| Part | Name |
| --- | --- |
| Screw | `<size> x <length> <head> Screw`, e.g. `#10-32 x 1/2 in Button Head Screw`, with any finish but black, and fully threaded, after it: `... Socket Head Screw (Zinc plated)`, `... (Fully threaded)` |
| Washer | `<size> Washer (<type>)`, e.g. `#10 Washer (1/2 in OD x 0.049 in)`, with any finish but black after the type: `(7/16 in OD x 0.03 in, Stainless steel)` |
| Nut | `<size> <type> Nut`, e.g. `#10-32 Thin Nylock Nut` |

Each has its material (steel, stainless steel, or aluminum, as its vendor sells it) and its finish's appearance, as
FRCDesignLib's color standards give it (general black, medium grey for zinc plated and for aluminum, whose color WCP
doesn't publish, and steel grey for stainless); nuts are the standards' zinc-coated steel (#E1E7EC), whatever their
finish. Each has its vendor as its Vendor,
its part number, and its link as its description, where the vendor has them (Generic has none). `fastenerTables.py` names every configuration's part (its
`Record`s) and writes each into the tables, which the features set as they are, and into the manifest, which
FRCDesignApp indexes in place of asking Onshape: one record per configuration the Part Studio offers, so the two
can't disagree.

## How it works

Every part is built along the Z axis, and has one mate connector at the origin, pointing out of the hole, where it
seats: FRCDesign fastens a screw or nut to the top edge of the washer under it, or to the hole's edge, and a washer to
the hole's edge. So any washer goes under any screw or nut, and none has a washer input.

Each dialog is a flat list, with no conditions: Vendor, Size, Head, Finish, one `ScrewLength` of every length any vendor
makes, and a Fully threaded checkbox, for a screw; Vendor, Size, Type, and Finish for a washer; Vendor, Size, and Type
for a nut.
Screws and washers have their own finish enums (`ScrewFinish`, `WasherFinish`), so neither dialog offers a finish its
kind never comes in.
So each Part Studio wires every parameter to a configuration input of the same options one for one, and the
configuration's visibility conditions, not the dialog, show only what's made (Length shows the lengths the chosen vendor
makes in the chosen size, head, and finish). Every part is one its vendor makes (`SCREWS`, `WASHERS`, `NUTS`); anything else is
an error.

Each part is its vendor's standard unless chosen otherwise. A screw's Finish always shows, offering only what's made in
its size and head (Generic's only black), so Length can show by it: black, zinc plated, or aluminum. Fully threaded
shows only where a length is sold both ways (McMaster-Carr's long socket heads, whose standard is partly threaded); a
screw's table entry has its `standard` part and, there, its `fullThread` one. In the configuration it's a List, No or
Yes, not a checkbox: a checkbox shows by a single condition, and where it's sold both ways needs several. A washer's
Finish (plain stainless steel) shows only where its vendor sells more than black. A hidden input keeps its default, and
in a feature's own dialog whatever it was last set to, so the features fall back to the standard: a washer's finish
with `made`, and a screw's thread when it has no `fullThread`.

`fastenerScrew`:

1. Revolves its profile (`screwProfile`, in `PROFILE_PLANE`) a full turn about Z. A socket or button head is above the
   origin, from its underside; a flat head is below it, its top flush with the origin, and its length includes it. The
   proportions are McMaster-Carr's models': every tip has a 45 degree chamfer (`tipChamfer`), a socket head a 45 degree
   chamfer around its top, a flat head a short band above its cone (`flatHeadDepth`), and a button head a short side
   below its dome, an arc (`buttonDome`) up to a flat top 1.4 times its socket's size across.
2. Cuts a hex socket from the top of the head (`socketDepth`: half a socket head's height, 0.45 of a button head's,
   0.46 of a flat head's cone).
3. Threads the shank as std's External thread does: a hole attribute on the shank and the edge above the tip
   chamfer, and on the chamfer as its chamfer face, and a cosmetic thread up the shank from that edge, at the size's
   pitch, `threadLength` long. That's up to the head, except on a standard socket head, threaded 2D + 1/2 in (ASTM
   A574) where it's longer.
4. Adds the mate connector, then deletes the sketches and names the part.

`fastenerWasher` extrudes a ring up from the origin, adds the mate connector, and names the part. `fastenerNut`:

1. Extrudes a hexagon or square up from the origin, as tall as the nut.
2. Revolves its profile (`nutProfile`) and keeps what's inside both. The profile is its bore, the size's tap drill,
   and its chamfers from the circle across its flats: 30 degrees on both ends of a hex nut, on the top only of a
   square nut, 25 degrees on both ends of a nylock's hex. A nylock rises from its hex in a quarter circle
   (`nylockDome`) to a flat top, where its nylon insert sits 0.009 in down in a bore of its own.
3. Taps the bore through as std's Hole does: a hole attribute on it (which `fastenerHoleScan` reads as threaded), and
   a cosmetic thread up from the origin, to a nylock's insert.
4. Adds the mate connector, deletes the sketches, names the part, and colors a nylock's insert white.

Threads are cosmetic: the parts are for fit and clearance.

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
- Nuts are measured from McMaster-Carr's models in #6-32 and #10-32, and the 1/4-20 square nut. The rest are
  nominal, and the other sizes' nylock domes and inserts take the measured nylocks' proportions: a dome a sixth of the
  nut's height, and an insert 0.61 of its width across flats.
- Not yet tried in Onshape: the nuts' chamfers, domes, and inserts.
- Not yet tried in Onshape: the chamfers, button domes, and threads. Neither is whether Onshape's hole callouts and
  Fasten mates read the attributes as they read std's.
