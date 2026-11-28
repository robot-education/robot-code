# Robot bearing

Cuts bores for bearings from FRCDesign's library, one at each sketch point, with a counterbore for a flanged bearing's
flange. It declares each bearing for FRCDesignApp's Auto Assembly (see `docs/auto-assembly.md`). Robot bearing
retention holds a bearing in. Its dialog follows std's hole's order: Style, the sketch points and merge scope, the
bearing and its fit, Opposite direction, then the counterbore (with the hole's names, ids and icons).

| File | What it is |
| --- | --- |
| `robotBearing.fs` | The feature: its dialog, the bearing chosen, and its counterbore |
| `../core/bearing.fs` | The placement Robot bushing shares: the locations and direction, the bores, and the declarations |
| `../released/bearing/robotBearingTables.py` | The bearings part set's FRC bearings (`robotBearingTable`), from `../bearings/bearingTables.py` |
| `../bearings/bearings.fs`, `bearingTables.gen.fs` | FRCDesign's bearings part set: the bearings' sizes, flanges and bores (`BEARINGS`, `BEARING_BORES`, `boreRadius`) |
| `../core/autoAssembly.fs` | Declarations |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot bearing | feature name |
| Cut bores for bearings. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

None of its parameters has a description.

Flanged is hidden: editing logic sets it (its "Set By" function, `isFlangedBearing`), whether the bearing has a flange,
which shows Style. Editing logic also sets Counterbore diameter and Counterbore depth (`cBoreDiameter`, `cBoreDepth`) from the flange
(`flangeCounterbore`) when the bearing, Bore fit or Style changes, and the Merge scope and Opposite direction
(`bearingEditLogic`).

### Errors, warnings, and info

| Message | Kind | When | Highlights | Closest std error |
| --- | --- | --- | --- | --- |
| Choose a different bearing. | error | the saved Bearing path isn't a whole table entry | `bearing` | none |
| Select a sketch point, circle, or mate connector to use. | error | no locations, or one with no plane | `locations`, that location | Select a sketch plane. |
| The counterbore must be wider than the bore. | error | Counterbore, with Counterbore diameter no wider than the bore | `cBoreDiameter` | none |
| std's "select a merge scope" (`HOLE_EMPTY_SCOPE`) | error | an empty Merge scope | `scope` | (std's) |
| The parts to cut aren't behind the sketch point. Check Opposite direction. | error | none of the merge scope is behind a location's plane | `oppositeDirection`, `scope`, the merge scope | none |
| Failed to cut the bore. | error | subtracting the bores fails | `scope`, the tools | Failed to extrude selections, check input. |
| FRCDesign's library doesn't have this part yet, so it isn't declared for Auto Assembly. | info | the bearings part set isn't released, so its studio has no document | | none |

## How it works

### Execution order

1. **Editing logic** (`robotBearingEditLogic`):
   1. Sets Flanged.
   2. For a flanged bearing, when the bearing, Bore fit or Style changes (or the feature is made), sets the counterbore
      to the flange's diameter plus Bore fit's clearance for it, and the flange's thickness (`flangeCounterbore`).
   3. `bearingEditLogic`: std's hole heuristics, with a sketch point at every location, set the Merge scope and
      Opposite direction, unless they've been set.
2. **Body**:
   1. The bearing (`libraryBearing`): its `robotBearingTable` entry's enums (Vendor, Type, Bore, Size, Dimensions, and
      Seal, skipping a level with one option), looked up in the bearings set's `BEARINGS` for its outside diameter,
      width and flange, with its hole at its bore's corners (`boreRadius`), as a `BearingPart`.
   2. The planes (`bearingPlanes`): each location's, flipped by Opposite direction. Each normal points out of the face
      the bearing sits on; its x axis is the location's.
   3. The bore: the outside diameter plus Bore fit's clearance. A flanged bearing with Counterbore adds its counterbore,
      or the error above where it's no wider than the bore. A radial bearing's saved Counterbore is ignored, as Style
      doesn't show.
   4. `placeBearings`:
      - An error for an empty merge scope, or one not behind a plane.
      - The part each bearing sits in (`partBehind`), found before any cut.
      - Each bore extruded through the merge scope behind its plane, and each counterbore its depth. All are cut in one
        boolean.
      - At each, a `mateConnector` connector on its part (`bearingConnector`: the bore's center, z out of the bore, x
        the plane's, where the bearings set seats its bearings), carrying the bearing's declaration. With a
        counterbore, it's on the counterbore's floor, where the flange sits. Before the set is released, the info
        above instead.
      - Each bearing's operations are under `place.bearing<n>`, so its connector is `<featureId>.place.bearing<n>.mateConnector`.
   5. Sketches are deleted.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `placeBearings` | Subtracting the bores | Throws "Failed to cut the bore.", showing the tools. |

## Issues found

- None of the geometry has run in Onshape: the bores and counterbores are reasoned out.
- A connector's id is its location's place in the selection, so removing a location renames the connectors after it,
  and mates to them break.
- Changing the bearing, Bore fit or Style sets the counterbore back to the flange's, overwriting one typed in, as std's
  hole does when its standard changes.
