# Robot lighten

Lightens parts with pockets: everything within an extrude from the rib sketch's plane (as the end type says) is cut
away, but for walls along the parts' sides and around their holes, and ribs along the sketch's edges, with the pockets'
corners filleted.

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
| Rib sketch (`ribEdges`) | The sketch whose edges ribs are left along. Its plane is where the pockets start. |
| Wall thickness (`wallThickness`) | How thick the walls left along the parts' sides and around their holes are. |
| Fillet corners (`filletCorners`) | Round the pockets' corners, as a router bit leaves them. |
| Faces to ignore (`ignoredFaces`) | Faces no wall is left along, so pockets run out through them. |
| Merge scope (`booleanScope`) | The parts to lighten. |

No parameters are hidden. Editing logic sets the shown Merge scope and Opposite direction (see Execution order).

### Errors, warnings, and info

Each step's own error display (what it highlights) is shown with these, and its own message after them when it's a
custom one (see `core/steps.fs`). Steps' warnings and info are shown as the feature's.

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select the edges of a sketch to leave ribs along. | error | no rib sketch edges (or only construction ones, excluded) | `ribEdges` |
| The rib sketch's edges must all be in one plane. | error | edges from sketches on different planes | `ribEdges`, the edges |
| Select the parts to lighten. | error | no merge scope | `booleanScope` |
| Couldn't extrude the pockets. | error | std's extrude of the pockets fails (like an up to face which isn't reached) | the extrude's parameters, the parts |
| Couldn't extrude the rib sketch's edges. | error | extruding the edges as sheets fails | `ribEdges`, the edges |
| Couldn't thicken the ribs. | error | thickening those sheets fails | `ribEdges`, `ribThickness`, the edges |
| Couldn't cut the ribs from the pockets. | error | the boolean fails | `ribEdges`, `ribThickness`, the pockets and ribs, rebuilt |
| A part has no faces parallel to the rib sketch, which pockets are cut into. | error | a part in the merge scope has no such face | `booleanScope`, the part |
| Couldn't copy a part to lighten. | error | `opPattern` fails | the part |
| Couldn't cut the pockets from a part. | error | the boolean fails | `booleanScope`, the part, the pockets, rebuilt |
| Couldn't shell a part to leave its walls. Are they too thick? | error | `opShell` fails | `wallThickness`, `ignoredFaces`, the part and the faces it removes |
| Couldn't join a part's walls to its ribs and what's outside its pockets. | error | the union fails | `booleanScope`, the part, the pockets, rebuilt |
| Couldn't fillet the pockets' corners. Is the radius too large? | error | `opFillet` fails | `cornerRadius`, the corners |

## How it works

### Execution order

1. **Precondition**: Rib sketch, Exclude construction, Wall thickness, Rib thickness, the end type and its options
   (`extrudePredicate`: end type and bounds, starting offset, symmetric, second end position), Fillet corners and
   its Radius, Faces to ignore (in Ignored faces), Merge scope.
2. **Editing logic** (`robotLightenEditLogic`), once there's a rib sketch:
   1. Unless Merge scope has been set, `partsUnder` fills it: the parts a through-all extrude of the sketch's
      bounding rectangle hits (run between `startFeature` and `abortFeature`, so it's rolled back), keeping those with a
      face in the sketch's plane if there are any.
   2. Unless Opposite direction has been set, points the pockets into the merge scope: flipped if the parts' center is
      behind the sketch's plane.
3. **Body**:
   1. The rib edges (`getRibEdges`, less construction ones) and their plane (`ribPlane`) are found, and the merge
      scope's solids.
   2. `buildPockets`: a rectangle on the plane covering the parts (`sketchCovering`) is extruded with std's `extrude`,
      at the top level id (so its manipulators are the feature's), as a new body.
   3. `buildRibs`: the rib edges are extruded through everything both ways as sheets, and thickened by half the rib
      thickness each side. The ribs are cut from the pockets.
   4. For each part (`lightenPart`, under its own id, disambiguated by the part):
      1. A copy of it has the pockets cut from it (keeping them): what's left is what's outside the pockets, and the
         ribs.
      2. The part is shelled inward by the wall thickness, removing its faces parallel to the sketch and the ignored
         faces, leaving walls along its sides and around its holes.
      3. What's left of the copy is joined to it (if anything is: pockets through all with no ribs across the part
         leave nothing).
      4. With Fillet corners, its concave edges along the sketch's normal which the feature made are filleted
         (`filletCorners`).
   5. The pockets, ribs, and sketches are deleted.
4. **Manipulator change** (`robotLightenManipulatorChange`): std's extrude manipulators.

### Error handling

Every operation runs through `runStep` (`core/steps.fs`): its warnings and info are shown as the feature's, and when it
fails, its error display is kept, what it was given is shown in red (the parts and selections, which exist before the
feature, or the pockets and ribs, rebuilt by `reconstructPockets`, since they're rolled back with the feature), and an
error of the feature's own is thrown, highlighting the parameters which set what failed. Selections are checked before
anything's built.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `runStep` (`core/steps.fs`) | Each step | Throws the step's error (above). |
| `showErrorEntities` (`core/steps.fs`, `try silent`) | Rebuilding what to show for an error | A guard: the error's thrown without it. |
| `robotLightenEditLogic` (`try silent`) | Finding the rib sketch's edges and plane | A guard: with no rib sketch yet, editing logic leaves the definition as it is. |
| `partsUnder` (`try silent`) | Extruding the sketch's footprint to find the parts it's over | A guard: no parts are found. |

## Issues found

- Untested in Onshape: nothing here has run yet.
- Walls come from shelling the part with its faces parallel to the sketch removed, so a part with a step (a face part
  way through it) gets walls along the step's sides too, and none across the step.
- The icon is the generic robot icon.
