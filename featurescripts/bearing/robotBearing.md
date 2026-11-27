# Robot bearing

Cuts bores for bearings from FRCDesign's library, one at each sketch point. It declares each bearing for FRCDesignApp's
Auto Assembly (see `docs/auto-assembly.md`), with an Auto Assembly body. Robot bearing retention holds a bearing in.

| File | What it is |
| --- | --- |
| `robotBearing.fs` | The feature: its dialog, and the bearing chosen |
| `../core/bearing.fs` | The placement Robot bushing shares: Position, the bores, the declarations, and the Auto Assembly bodies |
| `../released/bearing/robotBearingTables.py` | The bearings part set's FRC bearings (`robotBearingTable`), from `../bearings/bearingTables.py` |
| `../bearings/bearings.fs`, `bearingTables.gen.fs` | FRCDesign's bearings part set: the bearings' sizes, flanges and bores (`BEARINGS`, `BEARING_BORES`, `boreRadius`) |
| `../core/autoAssembly.fs` | Declarations and Auto Assembly bodies |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot bearing | feature name |
| Cut bores for bearings. `<CREDIT>` | feature description |
| `<bearing's library name>` | each Auto Assembly body's part name |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Auto Assembly body | Model a simple body where the part goes, which FRCDesign's Auto Assembly swaps for the part. (`core/autoAssembly.fs`'s) |

Editing logic sets the Merge scope and Flip primary axis (`bearingEditLogic`).

### Errors, warnings, and info

| Message | Kind | When | Highlights | Closest std error |
| --- | --- | --- | --- | --- |
| Choose a different bearing. | error | the saved Bearing path isn't a whole table entry | `bearing` | none |
| Select a sketch point, circle, or mate connector to use. | error | no locations, or one with no plane | `locations`, that location | Select a sketch plane. |
| `core/startOffset.fs`'s and `core/mounting.fs`'s errors | error | the starting offset, or angle reference | (theirs) | (theirs) |
| std's "select a merge scope" (`HOLE_EMPTY_SCOPE`) | error | an empty Merge scope | `scope` | (std's) |
| The parts to cut aren't behind the sketch point. Flip the primary axis. | error | none of the merge scope is behind a location's plane | `oppositeDirection`, `scope`, the merge scope | none |
| Failed to cut the bore. | error | subtracting the bores fails | `scope`, the tools | Failed to extrude selections, check input. |
| FRCDesign's library doesn't have this part yet, so it isn't declared for Auto Assembly. | info | the bearings part set isn't released, so its studio has no document | | none |

## How it works

### Execution order

1. **Editing logic** (`bearingEditLogic`): std's hole heuristics, with a sketch point at every location, set the Merge
   scope and Flip primary axis, unless they've been set.
2. **Body**:
   1. The bearing (`libraryBearing`): its `robotBearingTable` entry's enums (Vendor, Type, Bore, Size, Dimensions, and
      Seal, skipping a level with one option), looked up in the bearings set's `BEARINGS` for its outside diameter,
      width and flange, with its hole at its bore's corners (`boreRadius`), as a `BearingPart`.
   2. The planes (`bearingPlanes`): each location's, offset, turned to the angle reference, flipped, and turned by
      Angle. The offset's and angle's manipulators are on the first. Each normal points out of the face the bearing
      sits on.
   3. `placeBearings`:
      - An error for an empty merge scope, or one not behind a plane.
      - The part each bearing sits in (`partBehind`), found before any cut.
      - Each bore is the outside diameter plus Bore fit's clearance, extruded through the merge scope behind its plane.
        All are cut in one boolean.
      - At each, a `mateConnector` connector on its part (`bearingConnector`: the bore's center, z out of the bore, x
        the plane's, where the bearings set seats its bearings), carrying the bearing's declaration. Before the set is
        released, the info above instead.
      - With Auto Assembly body, a ring at its outside and inner diameters into each bore, and its flange on the face
        (`buildBearingBody`, `makeAutoAssemblyBody`).
      - Each bearing's operations are under `place.bearing<n>`, so its connector is `<featureId>.place.bearing<n>.mateConnector`.
   4. Sketches are deleted.
3. **Manipulator change** (`bearingManipulatorChange`): the starting offset's and the angle's.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `placeBearings` | Subtracting the bores | Throws "Failed to cut the bore.", showing the tools. |

## Issues found

- None of the geometry has run in Onshape: the bores and the Auto Assembly bodies are reasoned out.
- A connector's id is its location's place in the selection, so removing a location renames the connectors after it,
  and mates to them break.
