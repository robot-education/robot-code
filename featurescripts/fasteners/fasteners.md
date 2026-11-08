# Fasteners

## Summary

FRCDesign's library fasteners, generated for FRCDesignApp, which inserts them from the library and into assemblies: `fastenerScrew`,
`fastenerWasher`, and `fastenerNut`, each built in a Part Studio of its own whose configuration sets every
parameter. Files: `fasteners.fs` (the features), `fastenerTables.py` (the catalog, from FRCDesign's Fasteners
group in `vendor/frcdesign-fasteners.json`, read from FRCDesign's public API, with WCP's and AndyMark's screws from
their stores in `vendor/stores.json`, both refreshed by `uv run python featurescripts/fasteners/fastenerTables.py
fetch`: the enums and data in `fastenerTables.gen.fs`, the Part Studios' configurations, and FRCDesign's manifest
`fasteners.gen.json`). The hole scan FRCDesignApp evaluates in the Part Studios of the parts it fastens lives in
FRCDesignApp, beside the code that reads its answer.

FTC's metric fasteners (M3 and M4, from McMaster-Carr, goBILDA, REV, and AndyMark; see docs/ftc-fasteners.md) are the
same features and tables, offered by Part Studios of their own: a second part set (`[tool.fs.parts.ftcFasteners]`),
which `ftcFastenerTables.py` configures from `fastenerTables.py`'s `FTC` catalog, writing its manifest,
`ftcFasteners.gen.json`, and released into ConfigLib's document. Each set's studios offer its own sizes alone, so FRC's
offer what they did before FTC's.

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
| Selected vendor does not make this washer in this finish, change the finish. (error) | The type is made in other finishes, but not this one | Finish | none |
| Selected vendor does not make this nut, change the type. (error) | The vendor makes no nut of this size and type | Type | none |
| Selected vendor does not make this nut in this finish, change the finish. (error) | The type is made in other finishes, but not this one | Finish | none |

The configurations only offer what's made, so these show only in the features' own dialogs.

| Part | Name |
| --- | --- |
| Screw | `<size> x <length> <head> Screw`, e.g. `#10-32 x 1/2 in Button Head Screw`, with any finish but black, and fully threaded, after it: `... Socket Head Screw (Zinc plated)`, `... (Fully threaded)` |
| Washer | `<size> Washer (<type>)`, e.g. `#10 Washer (1/2 in OD x 0.049 in)`, with any finish but black after the type: `(7/16 in OD x 0.03 in, Stainless steel)`, `(3/4 in OD x 0.062 in, Zinc plated)` |
| Nut | `<size> <type> Nut`, e.g. `#10-32 Thin Nylock Nut` |
| FTC screw | `<size> x <length> <head> Screw (<finish>)`, by its size alone, with its finish but for Generic's: `M4 x 10 mm Socket Head Screw (Stainless steel)`, `M4 x 30 mm Socket Head Screw (Black, fully threaded)`, `M3 x 8 mm T-Slot Screw (Zinc plated)` |
| FTC washer | As FRC's, with any finish but zinc plated after the type: `M3 Washer (7 mm OD x 0.5 mm, Stainless steel)` |
| FTC nut | `<size> <type> Nut`, with any finish but zinc plated after it: `M4 Nylock Nut (Stainless steel)` |

Each has its material (steel, stainless steel, or aluminum, as its vendor sells it) and its finish's appearance, as
FRCDesignLib's color standards give it (general black, medium grey for zinc plated and for aluminum, whose color WCP
doesn't publish, and steel grey for stainless); zinc-plated nuts are the standards' zinc-coated steel (#E1E7EC), and black and
stainless ones take their finish's. AndyMark's washer am-1027 is named for its outer diameter in FRCDesign's record; its configuration gives its
bore, 0.281 in, which it's read with. Each has its vendor as its Vendor,
its part number, and its link as its description, where the vendor has them (Generic has none). `fastenerTables.py` names every configuration's part (its
`Record`s) and writes each into the tables, which the features set as they are, and into the manifest, which
FRCDesignApp indexes in place of asking Onshape: one record per configuration the Part Studio offers, so the two
can't disagree.

## How it works

Every part is built along the Z axis, and has one mate connector at the origin (`id + "mateConnector"`, which each
studio names as its `mate_connector` in the manifest, since a fasten mate names it under the feature's id), pointing out of the hole, where it
seats: FRCDesign fastens a screw or nut to the top edge of the washer under it, or to the hole's edge, and a washer to
the hole's edge. So any washer goes under any screw or nut, and none has a washer input.

Each dialog is a flat list, with no conditions: Vendor, Size, Head, Finish, one `ScrewLength` of every length any vendor
makes, and a Fully threaded checkbox, for a screw; Vendor, Size, Type, and Finish for a washer; Vendor, Size, Type, and
Finish for a nut (FRC's nuts come in zinc plating alone, so their configuration doesn't ask).
Screws, washers, and nuts have their own finish enums (`ScrewFinish`, `WasherFinish`, `NutFinish`), so no dialog offers
a finish its kind never comes in.
So each Part Studio wires every parameter to a configuration input of the same options, and the configuration's
visibility conditions, not the dialog, show only what's made, laid out by `LookupTable` (see docs/fs-cli.md's
"Configured Part Studios"): an input shows only where more than one of its options is made with the choices before it,
so Length shows the lengths the chosen vendor makes in the chosen size, head, and finish, and Finish shows only where
there's a choice of finish. An input with one option anywhere is fixed and left out: McMaster-Carr makes every nut, so
the nut's configuration has no Vendor. A hidden input keeps whatever it was last set to, so the features look each
choice up with `made`, which takes the one thing made where only one is. Anything else not made is an error.

The defaults are what's inserted most from FRCDesign's library, by its analytics: #10-32 x 3/8 in black button heads
(from Generic, which names no vendor), #10-32 nylocks, and McMaster-Carr's #10 x 7/16 in OD stainless washers. FTC's
are M4 x 10 mm socket heads from Generic, McMaster-Carr's M4 nylocks, and Generic M4 washers, ConfigLib's most
inserted; its studios list their finishes cheapest first (stainless for screws, zinc plating for nuts and washers).

Fully threaded shows only where a length is sold both ways (McMaster-Carr's long socket heads, whose standard is partly
threaded); a screw's table entry has its `standard` part and, there, its `fullThread` one, which the screw takes only
when it's chosen. A part its vendor sells threaded its whole length (`fullyThreaded`: goBILDA's and REV's, and
McMaster-Carr's metric ones it says so of) is threaded so, whatever its head's standard. In the configuration it's a List, No or Yes, not a checkbox: a checkbox shows by a single condition,
and where it's sold both ways needs several.

`fastenerScrew`:

1. Revolves its profile (`screwProfile`, in `PROFILE_PLANE`) a full turn about Z. A socket or button head is above the
   origin, from its underside; a flat head is below it, its top flush with the origin, and its length includes it, in
   its size's countersink (82 degrees imperial, 90 metric). A hex head's profile is its shank alone, its head extruded
   onto it (`addHexHead`): a hexagon up from the origin, chamfered at 30 degrees around its top as a nut is
   (`hexHeadProfile`), and a T-slot's cut down to its width across two flats, to drop into an extrusion's slot. The
   proportions are McMaster-Carr's models': every tip has a 45 degree chamfer (`tipChamfer`), a socket head a 45 degree
   chamfer around its top, a flat head a short band above its cone (`flatHeadDepth`), and a button head a short side
   below its dome, an arc (`buttonDome`) up to a flat top 1.4 times its socket's size across.
2. Cuts a hex socket from the top of the head (`addSocket`, `socketDepth`: half a socket head's height, 0.45 of a
   button head's, 0.46 of a flat head's cone). A hex head has none.
3. Threads the shank as std's External thread does: a hole attribute on the shank and the edge above the tip
   chamfer, and on the chamfer as its chamfer face, and a cosmetic thread up the shank from that edge, at the size's
   pitch, `threadLength` long. That's up to the head, except on a standard socket head, threaded 2D plus its size's
   allowance where it's longer (1/2 in by ASTM A574, 12 mm by ISO 4762).
4. Adds the mate connector, then deletes the sketches and names the part.

`fastenerWasher` extrudes a ring up from the origin, adds the mate connector, and names the part. `fastenerNut`:

1. Extrudes a hexagon or square up from the origin, as tall as the nut.
2. Revolves its profile (`nutProfile`) and keeps what's inside both. The profile is its bore, the size's tap drill,
   and its chamfers from the circle across its flats: 30 degrees on both ends of a hex nut, on the top only of a
   square nut, 25 degrees on both ends of a nylock's hex. A nylock rises from its hex in a quarter circle
   (`nylockDome`) to a flat top, where its nylon insert sits 0.009 in down in a bore of its own.
3. Taps the bore through as std's Hole does: a hole attribute on it (which FRCDesignApp's hole scan reads as threaded), and
   a cosmetic thread up from the origin, to a nylock's insert.
4. Adds the mate connector, deletes the sketches, names the part, and colors a nylock's insert white.

Threads are cosmetic: the parts are for fit and clearance.

There are no `try`s.

## Issues found

- Tried in Onshape (FeatureScript 3095, in a scratch Part Studio, not yet in the released studios): each kind of
  screw, washer, and nut regenerates as one part with its mate connector at the origin pointing up, named, and with
  its part number.
- Nuts are measured from McMaster-Carr's models in #6-32 and #10-32, and the 1/4-20 square nut. The rest are
  nominal, and the other sizes' nylock domes and inserts take the measured nylocks' proportions: a dome a sixth of the
  nut's height, and an insert 0.61 of its width across flats.
- Not yet tried in Onshape: the nuts' chamfers, domes, and inserts, and the configurations' hidden inputs.
- Not yet tried in Onshape: FTC's metric parts, hex and T-slot heads, and the nut's finish.
- Metric nuts are measured from McMaster-Carr's, goBILDA's, and REV's models; AndyMark's, which it publishes none of,
  take McMaster-Carr's and REV's of the same type.
- Not yet tried in Onshape: the chamfers, button domes, and threads. Neither is whether Onshape's hole callouts and
  Fasten mates read the attributes as they read std's.
