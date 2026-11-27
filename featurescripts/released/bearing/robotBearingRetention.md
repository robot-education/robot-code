# Robot bearing retention

Holds a bearing in its bore, at the bore's edge: with a hat (COTS, standard, or custom) or with two or three screws
whose heads overlap it. It declares a COTS hat and the screws for FRCDesignApp's Auto Assembly (see
`docs/auto-assembly.md`). Robot bearing cuts the bore. Formerly Robot bearing hat: its constant is still
`robotBearingHat`, which documents store.

| File | What it is |
| --- | --- |
| `robotBearingRetention.fs` | The feature: its dialog, the hat or screws, and the screws it declares |
| `robotBearingTables.py` | The bearing hats part set's hats with their centers, thickness and holes (from `../../bearingHats/hatTables.py`), and the fasteners' Screw and Nut studios; generates `robotBearingTables.gen.fs` |
| `bearingHatTables.py` | The mounting holes' tables; generates `bearingHatTables.gen.fs` |
| `../../core/autoAssembly.fs` | Declarations |

## Changelog

Since Robot bearing hat's last release:

- Renamed Robot bearing retention. It still takes the bore's edge.
- A top level Retention type: Hat (the old feature's hats) or Screws (two or three screws over the bearing). Hat type
  is a second top level option, under Hat.
- A standard or custom hat's bore is the edge's diameter; its Bearing hat table and Bore table are gone. A standard
  hat's size is Hat centers. A COTS hat is chosen from FRCDesign's bearing hats part set (Hat: its bore and centers).
  Documents' hats lose their table choices and take the defaults.
- The angle reference and Angle apply to screws too.
- The holes' tables are in their own group, Mounting holes.
- A COTS hat, and the screws in any hat or a screw retention, are declared for Auto Assembly. Each screw has the nut
  chosen where its holes aren't tapped.

## Strings

| String | Where |
| --- | --- |
| Robot bearing retention | feature name |
| Retain a bearing with a hat or screws. `<CREDIT>` | feature description |
| Bearing Hat `<n>` | a standard or custom hat's part name |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Declare screws | Declare a screw in each hole, for FRCDesign's Auto Assembly to insert. |
| Automatic length | Let Auto Assembly choose each screw's length from what its hole goes through. |
| Nut | The nut on each screw, where it comes out the far side. |

Has clearance is hidden: editing logic sets it (its "Set By" function, `hasTappedHoles`), whether the holes are tapped,
which shows Tap drill diameter. So is Holes tapped (`isTappedHoles`): whether the mounting holes are tapped, any hole
table's, which hides Nut. Editing logic also sets Hole diameter and Tap drill diameter from the hole table when its
path changes.

### Errors, warnings, and info

| Message | Kind | When | Highlights | Closest std error |
| --- | --- | --- | --- | --- |
| Select a bearing edge to use. | error | no bearing edge | `bearingEdge` | Select a sketch plane. |
| The bearing edge must belong to a solid part. | error | the edge isn't a solid's | `bearingEdge`, the edge | none |
| The bearing edge must be adjacent to a single hole. | error | the edge doesn't border one cylindrical face | `bearingEdge`, the edge | none |
| `core/mounting.fs`'s angle reference errors | error | the angle reference | (theirs) | (theirs) |
| FRCDesign's library doesn't have this hat yet, so it isn't declared for Auto Assembly. | info | COTS hat, before the bearing hats part set is released | | none |
| Select a location for the first mounting hole. / Select a location for the second mounting hole. | error | Custom hat, without its locations | the location | Select a sketch plane. |
| FRCDesign's fasteners have no #10-24 screws, so the screws aren't declared. | info | Declare screws, with 24 tpi tapped holes | | none |
| std's `HOLE_PARAMS_OVERRIDDEN_INFO` | info | Hole diameter isn't the hole table's | | (std's) |
| std's hole errors | error | cutting the mounting holes (std's `hole`) | (std's; its scope's on `bearingEdge`) | (std's) |

## How it works

### Execution order

1. **Editing logic** (`robotBearingHatEditLogic`): sets Has clearance and Holes tapped; when the hole table's path
   changes, Hole diameter and Tap drill diameter from it.
2. **Body**:
   1. The bore (`bearingFromEdge`): the edge's solid (the host), its diameter, and its plane. The plane is the edge's
      circle, its normal out of the hole (`edgeOutOfHole`). It's turned to the angle reference, flipped, and turned by
      Angle, with the angle's manipulator. A custom hat skips the angle reference and Angle.
   2. Hole diameter's info, if it's overridden.
   3. **Hat** (`retainWithHat`): its mounting locations: Custom hat's selections, or its centers apart on the plane's x
      axis (a COTS hat's from its Hat entry, a standard hat's Hat centers).
      - A COTS hat is declared on the host (its Hat entry's product), on a `mateConnector` connector at the bore's
        center, x toward a mounting hole. With Declare screws, a screw is declared at each mounting hole, seated as high
        as the hat is thick, with `seatHeight` and `seatHoleDiameter` in its spec: the hat isn't in the Part Studio, so
        Auto Assembly goes through a hole that size first.
      - A standard or custom hat is modeled around the bore (`createBearingHat`). With Declare screws, a screw is
        declared at each mounting hole, seated on the hat's top.
      - The mounting holes are cut into the host (std's `hole`) for a COTS hat, or with Cut mounting holes.
   4. **Screws** (`retainWithScrews`): the holes (`screwPositions`: 2 or 3, evenly from the plane's x axis, on a circle
      clearing the bore by 0.03 in., or Hole circle diameter) are cut into the host. With Declare screws, a screw is
      declared at each, seated on the host's face.
   5. Declared screws (`declareScrews`) are FRCDesign's fasteners' #10-32, in Screw vendor, Screw head and Screw finish,
      and Screw length unless Automatic length. Each is on a connector named for the fasteners' own (`mateConnector`),
      z out of its hole. Where the holes aren't tapped, the declaration's `nut` is the fasteners' Nut studio in Nut's
      type; Auto Assembly picks its vendor and size from the screw's.
   6. Sketches are deleted.
3. **Manipulator change** (`robotBearingHatManipulatorChange`): the angle's.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `filletConcaveEdges` | Filleting a custom hat's concave edges | Leaves them sharp: a fallback, to remove. |

## Issues found

- None of the new geometry has run in Onshape: the screws' places, and the hat around the edge's diameter.
- A COTS hat's thickness and holes are the hats set's estimates (see `featurescripts/bearingHats/hatTables.py`): WCP
  publishes none.
- Nothing checks a COTS hat's bore against the edge: a 0.875 in. hat can be chosen for a 1.125 in. bore.
- Only #10 holes: the hole tables are #10's, as the hat's were.
