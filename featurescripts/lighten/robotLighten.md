# Robot lighten

Lightens parts with pockets: everything within the extrude of the faces to lighten (into their parts, as the end type
says) is cut away, but for walls along the faces' edges (their parts' sides and holes) and ribs along the rib sketch's
edges (lines, arcs, circles, or splines), with the pockets' corners rounded as a router bit leaves them.

| File | What it is |
| --- | --- |
| `robotLighten.fs` | The feature: its dialog, the pockets, ribs, walls, and fillets, and its editing logic |
| `../core/stdExtrude.fs` | The end type and its options (`extrudePredicate`), as std's extrude has them |
| `../core/steps.fs` | Runs each step as a subfeature, showing its status, and its failures as the feature's errors |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot lighten | feature name |
| Lighten parts with pockets, leaving walls around their edges and holes, and ribs along a sketch. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Faces to lighten (`faces`) | The flat faces pockets are cut into, parallel to the rib sketch. |
| Rib sketch (`ribEdges`) | The sketch whose edges (lines, arcs, circles, or splines) ribs are left along. |
| Wall thickness (`wallThickness`) | How thick the walls left along the parts' sides and around their holes are. |
| Fillet corners (`filletCorners`) | Round the pockets' corners, as a router bit leaves them. |
| Faces to ignore (`ignoredFaces`) | Faces no wall is left along, so pockets run out through them. |

No parameters are hidden. Editing logic sets the shown Faces to lighten and Opposite direction (see Execution order).

### Errors, warnings, and info

Each step's own error display (what it highlights) is shown with these, and its own message after them when it's a
custom one (see `core/steps.fs`). Steps' warnings and info are shown as the feature's.

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select the faces to lighten. | error | no faces to lighten | `faces` |
| Select the edges of a sketch to leave ribs along. | error | no rib sketch edges (or only construction ones, excluded) | `ribEdges` |
| The rib sketch's edges must all be in one plane. | error | edges from sketches on different planes | `ribEdges`, the edges |
| The faces to lighten must be parallel to the rib sketch. | error | a face to lighten isn't | `faces`, `ribEdges`, those faces |
| Couldn't extrude the faces to lighten. | error | std's extrude of the faces fails (like an up to face which isn't reached) | the extrude's parameters, the faces |
| Couldn't make the walls along the faces' edges. | error | extruding the faces' edges as sheets, or thickening them, fails | `faces`, `wallThickness`, `ignoredFaces`, the edges |
| Couldn't make the ribs along the rib sketch's edges. | error | extruding the rib edges as sheets, or thickening them, fails | `ribEdges`, `ribThickness`, the edges |
| Couldn't cut the walls and ribs from the pockets. | error | the boolean fails | `wallThickness`, `ribThickness`, the extrude, walls, and ribs |
| Couldn't grow the pockets back to round their corners. | error | offsetting the pockets' sides fails | `cornerRadius`, the pockets |
| Couldn't fillet the pockets' corners. | error | `opFillet` fails | `cornerRadius`, the corners |
| There's no room for pockets between the walls and ribs. | warning | no pockets are left (they're all narrower than the router bit, or than nothing) | `wallThickness`, `ribThickness`, `cornerRadius` |
| Couldn't cut the pockets from the parts. | error | the boolean fails | `faces`, the pockets and faces |
| Some ribs touch no wall or other rib, so they're left as loose parts. | warning | cutting the pockets cuts pieces free | `ribEdges`, the loose parts |

## How it works

### Execution order

1. **Precondition**: Faces to lighten, Rib sketch, Exclude construction lines, Wall thickness, Rib thickness, the end
   type and its options (`extrudePredicate`: end type and bounds, starting offset, symmetric, second end position),
   Fillet corners and its Radius, Faces to ignore (in Ignored faces).
2. **Editing logic** (`robotLightenEditLogic`):
   1. Unless Opposite direction has been set, sets it, so the pockets go into the faces' parts (an extrude of a part's
      face goes out of it).
   2. Unless Faces to lighten has been set, and once there's a rib sketch, `facesUnder` fills it: the faces in the
      sketch's plane of the parts a through-all extrude of the sketch's bounding rectangle hits (run between
      `startFeature` and `abortFeature`, so it's rolled back).
3. **Body**:
   1. The faces to lighten (`getFaces`), the rib edges (`getRibEdges`, less construction ones) and their plane
      (`ribPlane`) are found, and the faces are checked to be parallel to it (`verifyParallel`).
   2. `buildPockets`: the faces are extruded with std's `extrude`, at the top level id (so its manipulators are the
      feature's), as a new body: the most the pockets can be.
   3. `buildBands` makes the walls, along the faces' edges (but those also of an ignored face), and the ribs, along the
      rib edges: each edge is extruded through everything both ways as a sheet (a circle's is a tube), and thickened
      to each side, by the wall thickness, or half the rib thickness. With Fillet corners, each is the radius thicker
      to each side.
   4. The walls and ribs are cut from the extrude, leaving the pockets between them, in pieces.
   5. With Fillet corners, `roundPockets` rounds them: their sides are offset out by the radius (growing them back
      from the thicker walls and ribs), and their convex edges along the sketch's normal (their corners) are filleted
      by it. A pocket narrower than the radius allows is gone before then, so every corner has room for its fillet,
      and one which narrows between ribs ends in one round, as a router bit of that radius would cut it.
   6. If no pockets are left, a warning says so; otherwise they're cut from the faces' parts, and pieces they cut
      free (ribs touching no wall or other rib) are warned about and shown.
   7. What's left of the walls and ribs (their sheets) is deleted.
4. **Manipulator change** (`robotLightenManipulatorChange`): std's extrude manipulators.

### Error handling

Every operation runs through `runStep` (`core/steps.fs`): its warnings and info are shown as the feature's, and when it
fails, its error display is kept, what it was given is shown in red (the selections, or the pockets and ribs, which a
failed step leaves as they were), and an error of the feature's own is thrown, highlighting the parameters which set
what failed. Selections are checked before anything's built. Warnings are reported when they're found.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `runStep` (`core/steps.fs`) | Each step | Throws the step's error (above). |
| `robotLightenEditLogic` (`try silent`) | Finding the rib sketch's edges and plane | A guard: with no rib sketch yet, editing logic leaves the definition as it is. |
| `facesUnder` (`try silent`) | Extruding the sketch's footprint to find the parts it's over | A guard: no faces are found. |

## Issues found

- Untested in Onshape: nothing here has run yet.
- Walls follow the faces' edges straight down (along the sketch's normal), so a part whose sides slope or step gets
  walls of the faces' outline, not of its sides.
- The icon is the generic robot icon.
