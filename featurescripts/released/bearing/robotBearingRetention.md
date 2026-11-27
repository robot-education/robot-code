# Robot bearing retention

Holds a bearing in its bore, at the bore's edge: with a hat (COTS, standard, or custom) or with screws whose heads
overlap it (two or three around the bore, or at locations picked). It declares a COTS hat and the screws for FRCDesignApp's Auto Assembly (see
`docs/auto-assembly.md`). Robot bearing cuts the bore. Formerly Robot bearing hat: its constant is still
`robotBearingHat`, which documents store.

| File | What it is |
| --- | --- |
| `robotBearingRetention.fs` | The feature: its dialog, the hat or screws, and the screws it declares |
| `robotBearingTables.py` | The bearing hats part set's hats with their bearing's outer diameter, centers, thickness and holes (from `../../bearingHats/hatTables.py`), and the fasteners part set's #10-32 screws and nuts (`screwTable`, `nutTable`, from `../../fasteners/fastenerTables.py`); generates `robotBearingTables.gen.fs` |
| `bearingHatTables.py` | The mounting holes' tables; generates `bearingHatTables.gen.fs` |
| `../../core/autoAssembly.fs` | Declarations |

## Changelog

Since Robot bearing hat's last release:

- Renamed Robot bearing retention. It still takes the bore's edge.
- A top level Retention type: Hat (the old feature's hats) or Screws. A second top level option is Hat type, or
  Screws type: Standard screws (two or three around the bore) or Custom screws (at Screw locations).
- A standard or custom hat's bore is the edge's diameter; its Bearing hat table and Bore table are gone. A standard
  hat's size is Hat centers. A COTS hat is chosen from FRCDesign's bearing hats part set (Hat: its bore and centers).
  Documents' hats lose their table choices and take the defaults.
- The angle reference and Angle apply to standard screws too.
- The holes' tables are in their own group, Mounting holes.
- A COTS hat is declared for Auto Assembly. With Auto assembly, a checkbox driving its group, so are the screws in any
  hat or a screw retention: Screw, a lookup table whose Length can be Automatic, and Nut where the holes aren't
  tapped.

## Strings

| String | Where |
| --- | --- |
| Robot bearing retention | feature name |
| Retain a bearing with a hat or screws. `<CREDIT>` | feature description |
| Bearing Hat `<n>` | a standard or custom hat's part name |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Auto assembly | Declare a screw in each hole, for FRCDesign's Auto Assembly to insert. |
| Screw | Automatic lets Auto Assembly choose each screw's length from what its hole goes through. |
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
| This hat is for a different bearing. Choose a `<bore>` hat. | error | COTS hat, when another of the set's hats is for a bearing nearer the edge's diameter (`cotsHatBore`) | `cotsHat`, `bearingEdge`, the edge | none |
| FRCDesign's library doesn't have this hat yet, so it isn't declared for Auto Assembly. | info | COTS hat, before the bearing hats part set is released | | none |
| Select a location for the first mounting hole. / Select a location for the second mounting hole. | error | Custom hat, without its locations | the location | Select a sketch plane. |
| Select locations for the screws. | error | Custom screws, without Screw locations | `screwLocations` | Select a sketch plane. |
| FRCDesign's fasteners have no #10-24 screws, so the screws aren't declared. | info | Auto assembly, with 24 tpi tapped holes | | none |
| std's `HOLE_PARAMS_OVERRIDDEN_INFO` | info | Hole diameter isn't the hole table's | | (std's) |
| std's hole errors | error | cutting the mounting holes (std's `hole`) | (std's; its scope's on `bearingEdge`) | (std's) |

## How it works

### Execution order

1. **Editing logic** (`robotBearingHatEditLogic`): sets Has clearance and Holes tapped; when the hole table's path
   changes, Hole diameter and Tap drill diameter from it.
2. **Body**:
   1. The bore (`bearingFromEdge`): the edge's solid (the host), its diameter, and its plane. The plane is the edge's
      circle, its normal out of the hole (`edgeOutOfHole`). It's turned to the angle reference, flipped, and turned by
      Angle, with the angle's manipulator. A custom hat or custom screws skip the angle reference and Angle.
   2. Hole diameter's info, if it's overridden.
   3. **Hat** (`retainWithHat`): its mounting locations: Custom hat's selections, or its centers apart on the plane's x
      axis (a COTS hat's from its Hat entry, a standard hat's Hat centers). Selections are placed on the plane by
      `selectedPositions`: a vertex's or mate connector's point, or a circle's center.
      - A COTS hat must be for the bearing whose outer diameter is nearest the edge's (`cotsHatBore`), or it's an
        error. It's declared on the host (its Hat entry's product), on a `mateConnector` connector at the bore's
        center, x toward a mounting hole. With Auto assembly, a screw is declared at each mounting hole, seated as high
        as the hat is thick, with `seatHeight` and `seatHoleDiameter` in its spec: the hat isn't in the Part Studio, so
        Auto Assembly goes through a hole that size first.
      - A standard or custom hat is modeled around the bore (`createBearingHat`). With Auto assembly, a screw is
        declared at each mounting hole, seated on the hat's top.
      - The mounting holes are cut into the host (std's `hole`) for a COTS hat, or with Cut mounting holes.
   4. **Screws** (`retainWithScrews`): the holes are cut into the host. Standard screws' are 2 or 3, evenly from the
      plane's x axis, on a circle clearing the bore by 0.03 in., or Hole circle diameter (`screwPositions`). Custom
      screws' are at Screw locations (`selectedPositions`). With Auto assembly, a screw is declared at each, seated on
      the host's face.
   5. Declared screws (`declareScrews`) are the Screw entry's product: the fasteners' Screw studio in its record's
      configuration, with no length for Automatic. Each is on a connector named for the fasteners' own
      (`mateConnector`), z out of its hole. Where the holes aren't tapped, the declaration's `nut` is the Nut entry's
      product.
   6. Sketches are deleted.
3. **Manipulator change** (`robotBearingHatManipulatorChange`): the angle's.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `filletConcaveEdges` | Filleting a custom hat's concave edges | Leaves them sharp: a fallback, to remove. |

## Issues found

- None of the new geometry has run in Onshape: the screws' places, and the hat around the edge's diameter.
- Every #10-32 nut the fasteners set has is McMaster-Carr's, so Nut asks only the type. FRCDesignApp's Auto Assembly
  still uses the screw's vendor's nut where that vendor makes it.
- A COTS hat's thickness and holes are the hats set's estimates (see `featurescripts/bearingHats/hatTables.py`): WCP
  publishes none.
- Only #10 holes: the hole tables are #10's, as the hat's were.
