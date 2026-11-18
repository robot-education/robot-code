# Robot bearing

Cuts a bore for a bearing from FRCDesign's library at a sketch point, and declares the bearing for FRCDesignApp's Auto
Assembly (see `docs/auto-assembly.md`), with a stand-in; with Retention, holds it in with a hat (COTS, standard, or
custom) or with two or three screws whose heads overlap it. Formerly Robot bearing hat: its constant is still
`robotBearingHat`, which documents store.

| File | What it is |
| --- | --- |
| `robotBearing.fs` | The feature: its dialog, the retention (hat or screws), the hat, and the screws it declares |
| `../../core/bearing.fs` | The placement Robot bushing shares: Position and Bore, the bore, the declaration, and the stand-in |
| `bearingTables.py` | FRCDesignLib's bearings and bushings, WCP's hats, and the fasteners' Screw studio, from `vendor/frcdesign-bearings.json` (`bearingTables.py fetch`); generates `bearingTables.gen.fs` |
| `bearingHatTables.py` | The hats' hole tables; generates `bearingHatTables.gen.fs` |
| `../../core/autoAssembly.fs` | Declarations and stand-ins |

## Changelog

Since Robot bearing hat's last release:

- Renamed Robot bearing, and rebuilt around the bearing: it cuts the bore at a sketch point (Position: location,
  orientation, angle reference and angle, starting offset; Bore: Bore fit and Merge scope), as Robot motor cuts its
  face, rather than taking a bore's edge. Documents' Robot bearing hats lose their bearing edge, and show the location's
  error until a location is picked.
- The hat is an option: Retention, a checkbox driving its group, with Retention type Hat (the old feature's hats) or
  Screws (two or three screws over the bearing). Hat type is a dropdown in the group, not a horizontal enum at the top.
- A standard or COTS hat's size is its Hat centers; its bore is the bearing's. A custom hat's bore is the bearing's too
  (no Bore table).
- The bearing, a COTS hat, and the screws in a standard or custom hat or a screw retention are declared for Auto
  Assembly; a Stand-in (Other options) shows the bearing.

## Strings

| String | Where |
| --- | --- |
| Robot bearing | feature name |
| Cut a bore for a bearing, and retain it with a hat or screws. `<CREDIT>` | feature description |
| `<bearing's library name>` | the stand-in's part name |
| Bearing Hat `<n>` | a standard or custom hat's part name |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Declare screws | Declare a screw in each hole, for FRCDesign's Auto Assembly to insert. |
| Automatic length | Let Auto Assembly choose each screw's length from what its hole goes through. |
| Stand-in | Model a simple body where the part goes, which FRCDesign's Auto Assembly swaps for the part. (`core/autoAssembly.fs`'s) |

Has clearance is hidden: editing logic sets it (its "Set By" function, `hasTappedHoles`), whether the holes are tapped,
which shows Tap drill diameter. Editing logic also sets Hole diameter and Tap drill diameter from the hole table when
its path changes, and the Merge scope and Flip primary axis (`mountingEditLogic`, as Robot motor's).

### Errors, warnings, and info

| Message | Kind | When | Highlights | Closest std error |
| --- | --- | --- | --- | --- |
| Choose a different bearing. | error | the saved Bearing path isn't a whole table entry | `bearing` | none |
| `core/location.fs`'s, `core/startOffset.fs`'s and `core/mounting.fs`'s errors | error | the location, starting offset, or angle reference | (theirs) | (theirs) |
| std's "select a merge scope" (`HOLE_EMPTY_SCOPE`) | error | an empty Merge scope | `scope` | (std's) |
| The parts to cut aren't behind the sketch point. Flip the primary axis. | error | none of the merge scope is behind the plane | `oppositeDirection`, `scope`, the merge scope | none |
| Failed to cut the bore. | error | subtracting the bore fails | `scope`, the tool | Failed to extrude selections, check input. |
| WCP makes no hat for this bearing at these centers. Use a standard or custom hat, or screws. | error | COTS hat, and WCP's hats have none for the bearing's outside diameter at Hat centers | `hatCenters`, `bearing` | none |
| Select a location for the first mounting hole. / Select a location for the second mounting hole. | error | Custom hat, without its locations | the location | Select a sketch plane. |
| FRCDesign's fasteners have no #10-24 screws, so the screws aren't declared. | info | Declare screws, with 24 tpi tapped holes | | none |
| std's `HOLE_PARAMS_OVERRIDDEN_INFO` | info | Hole diameter isn't the hole table's | | (std's) |
| std's hole errors | error | cutting the mounting holes (std's `hole`) | (std's) | (std's) |

## How it works

### Execution order

1. **Editing logic** (`robotBearingHatEditLogic`): sets Has clearance; when the hole table's path changes, Hole
   diameter and Tap drill diameter from it; then `mountingEditLogic`.
2. **Body**:
   1. The bearing (`bearingTable`: Type, Size, and Vendor where the library has several), as a `BearingPart`.
   2. The plane (`bearingPlane`): the location's, offset, turned to the angle reference, flipped, and turned by Angle,
      with the offset's and angle's manipulators. Its normal points out of the face the bearing sits on.
   3. `placeBearing`: an error for an empty merge scope or one not behind the plane; the part the bearing sits in
      (`partBehind`); the bore, the outside diameter plus Bore fit's clearance, cut through the merge scope behind the
      plane in one boolean; the `face` connector on that part (`bearingConnector`: the bore's center, z out of the
      bore, x the plane's), carrying the bearing's declaration; and with Stand-in, a ring at its outside and inner
      diameters, as wide as it, into the bore (`makeStandIn`).
   4. With Retention, Hole diameter's info if it's overridden, then:
      - **Hat** (`retainWithHat`): its mounting locations (Custom hat's selections, or Hat centers apart on the plane's
        x axis). A COTS hat is declared on the bearing's part (`wcpHat`: WCP's hat for the bearing's outside diameter
        at Hat centers, or the error above), on a `face` connector at the bore's center. A standard or custom hat is
        modeled as Robot bearing hat modeled it (`createBearingHat`), around the bearing's outside diameter, and with
        Declare screws, a screw is declared at each mounting hole, seated on the hat's top. The mounting holes are cut
        (std's `hole`) for a COTS hat, or with Cut mounting holes.
      - **Screws** (`retainWithScrews`): the holes (`screwPositions`: 2 or 3, evenly from the plane's x axis, on a
        circle clearing the bore by 0.03 in., or Hole circle diameter) are cut, and with Declare screws, a screw is
        declared at each, seated on the bearing's part's face.
      Declared screws (`declareScrews`) are FRCDesign's fasteners' #10-32, in Screw vendor, Screw head and Screw
      finish, and Screw length unless Automatic length; each on a connector named for the fasteners' own
      (`mateConnector`), z out of its hole.
   5. Sketches are deleted.
3. **Manipulator change** (`bearingManipulatorChange`): the starting offset's and the angle's.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `placeBearing` | Subtracting the bore | Throws "Failed to cut the bore.", showing the tool. |
| `filletConcaveEdges` | Filleting a custom hat's concave edges | Leaves them sharp: a fallback, to remove. |

## Issues found

- None of the geometry has run in Onshape: the bore, the stand-in, the screws' places, and the hats on the new plane
  (whose normal points away from the parts, as the old bearing edge's did) are reasoned out.
- A COTS hat's screws aren't declared: WCP's hat's thickness isn't in the library's data, so where they seat isn't
  known; Auto Assembly's hole fastening can fasten them.
- The stand-in has no flange: the library's sizes don't give one.
- The screws a clearance hole declares need a nut, which isn't declared; Auto Assembly's planner adds nuts to holes
  it fastens through.
- Only #10 holes: the hole tables are #10's, as the hat's were.
