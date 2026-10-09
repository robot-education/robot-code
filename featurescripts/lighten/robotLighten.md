# Robot lighten

Lightens parts with pockets: everything within the extrude of the faces to lighten (into their parts, as the end type
says) is cut away, but for walls along the faces' edges (their parts' sides and holes) and ribs along the selected
sketch edges (lines, arcs, circles, or splines), with the pockets' corners rounded as a router bit leaves them.

| File | What it is |
| --- | --- |
| `robotLighten.fs` | The feature: its dialog, the pockets, ribs, walls, and fillets, and its editing logic |
| `../core/stdExtrude.fs` | The end type and its options (`extrudePredicate`), as std's extrude has them |
| `partLighten.local.fs` | Part Lighten (by Evan Fish, FRC 2471, from Ilya Baran and Morgan Bartlett's Lighten), for reference: not synced |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot lighten | feature name |
| Lighten parts with pockets, leaving walls around their edges and holes, and ribs along a sketch. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

No parameters have descriptions, and none are hidden. Editing logic sets the shown Faces to lighten and Opposite
direction (see Execution order).

### Errors, warnings, and info

Errors from a failed operation show what failed in red: what it was given, narrowed down (for some) to what fails when
it's tried alone (see Error handling).

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select the faces to lighten. | error | no faces to lighten | `faces` |
| Select ribs to use. | error | no ribs (or only construction ones, excluded) | `ribEdges` |
| The ribs must be in parallel sketches. | error | ribs from sketches on planes which aren't parallel to the first's | `ribEdges`, those ribs |
| The faces to lighten must be parallel to the ribs. | error | a face to lighten isn't | `faces`, `ribEdges`, those faces |
| std's extrude's own errors | error | std's extrude of the faces fails (like an up to face which isn't reached) | its own: the end type's parameters |
| Failed to extend pockets past their ends. | error | offsetting the pockets' ends (and sides along ignored faces) out fails | `wallThickness`, those faces |
| Failed to round walls' inside corners. | error | the hair fillet of the pockets' concave edges fails | `wallThickness`, the edges |
| Failed to make walls. | error | hollowing (or enclosing) the pockets fails | `wallThickness`, the pockets which fail alone |
| Failed to extrude ribs. | error | extruding the ribs as sheets (or the cylinders around small arcs) fails | `ribEdges`, `ribThickness`, the ribs |
| Failed to thicken ribs. | error | thickening those sheets fails | `ribEdges`, `ribThickness`, the ribs which fail alone |
| Failed to cut ribs. | error | cutting them from the inset pockets fails | `ribEdges`, `ribThickness`, the ribs which fail alone |
| Failed to round pocket corners. | error | the hair fillet of the pockets' corners fails | `filletRadius`, the corners |
| Failed to grow pockets back to round their corners. | error | offsetting the pockets' sides fails | `filletRadius`, the pockets which fail alone |
| There's no room for pockets between the walls and ribs. | warning | no pockets are left (they're all narrower than the router bit, or than nothing) | `wallThickness`, `ribThickness`, `filletRadius` |
| Failed to cut pockets. | error | cutting them from the parts fails | `faces`, the pockets which fail alone |
| Some ribs touch no wall or other rib, so they're left as loose parts. | warning | cutting the pockets cuts pieces free | `ribEdges`, the loose parts |

## How it works

### Execution order

1. **Precondition**: Faces to lighten, Ribs to use, Exclude construction lines, Wall thickness, Rib thickness, the end
   type and its options (`extrudePredicate`: end type and bounds, starting offset, symmetric, second end position),
   Fillet corners, Fillet radius, Faces to ignore (in Ignored faces).
2. **Editing logic** (`robotLightenEditLogic`):
   1. Unless Opposite direction has been set, sets it, so the pockets go into the faces' parts (an extrude of a part's
      face goes out of it).
   2. Unless Faces to lighten has been set, and once there are ribs, `facesUnder` fills it: the faces in the first
      rib's sketch plane (of parts which aren't hidden) whose bounding boxes, in the plane, overlap the ribs'. It only
      evaluates: building anything in editing logic (a trial feature, between `startFeature` and `abortFeature`) can
      crash the Part Studio.
3. **Body**:
   1. The faces to lighten (`getFaces`), the ribs (`getRibEdges`, less construction ones) and their plane
      (`ribPlane`: the first's sketch plane, which the rest's must be parallel to) are found, and the faces are checked
      to be parallel to it (`verifyParallel`). The ribs can be from any number of sketches.
   2. `buildPockets`: the faces are extruded with std's `extrude`, at the top level id (so its manipulators are the
      feature's), as a new body: the most the pockets can be.
   3. The walls (`insetPockets`), as Ilya Baran and Morgan Bartlett's Lighten (and Part Lighten, `partLighten.local.fs`,
      which builds on it) make them: the extrude is inset by the wall thickness (and, with Fillet corners, the fillet
      radius), all at once. Its ends (`qCapEntity`), and its sides along ignored faces (`sidesAlong`: those whose
      middles are in one), are offset out by that much; its concave edges are filleted by a hair (std's boolean
      tolerance, 0.01 mm); and it's hollowed (`opShell`) by that much, which moves every face in by it. Inside each,
      what's enclosed (`opEnclose`) is the inset, and the hollowed extrudes are deleted. So the ends, and the sides
      along ignored faces, are back where they were, and walls are left along every other side: around holes of any
      size, and with inside corners rounded to the wall thickness (and the hair), as a wall of that thickness has.
   4. The ribs (`buildRibs`), half the rib thickness to each side (and, with Fillet corners, the fillet radius), along
      the sketch's normal through the inset pockets and the ribs' edges, and 5% past them (`bandExtent`). Each edge is
      extruded as a sheet (a circle's is a tube), and thickened to each side (`thickenEdges`); but an arc or circle
      whose radius is hardly more than that (up to 5% more) can't be thickened toward its center, so its rib is a
      cylinder around its center, that much bigger than it, instead (a little more than its rib, near its center).
      Arcs of one circle share a cylinder. They're cut from the inset pockets, which they split into the pockets.
   5. With Fillet corners, `roundPockets` rounds them, as Lighten does: their convex edges along the sketch's normal
      (their corners) are filleted by a hair, and their sides offset out by the radius, growing them back from the
      thicker walls and ribs, which grows those fillets to the radius (and the hair). A pocket narrower than the bit
      is gone before then, and one which narrows between ribs ends in one round, as a router bit of that radius would
      cut it. Filleting the corners by the radius after growing them would fail where pockets narrow.
   6. If no pockets are left, a warning says so; otherwise they're cut from the faces' parts, and pieces they cut
      free (ribs touching no wall or other rib) are warned about and shown.
   7. What's left of the ribs (their sheets) is deleted.
4. **Manipulator change** (`robotLightenManipulatorChange`): std's extrude manipulators.

### Error handling

Each operation which can fail has its own `try`, and its `catch` throws the feature's error for it, highlighting the
parameters which set what failed, and showing it in red. A failed operation changes nothing, so what it was given is
still there to show. For the hollow, the ribs' thicken, the cuts, and the
growing offset, the `catch` narrows that down: it tries the operation again on each pocket, rib (its edge, for the
thicken), or pocket alone (`failingBodies`; cuts on copies of what they cut, `copyBodies`, so each try sees what the
operation was given), and shows those which fail alone, or everything, if none does (it fails only on everything
together). That runs an operation per body, but only on the way to an error, which rolls it all back. Selections are checked before anything's built. Warnings are reported when they're found.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `insetPockets`, `buildRibs`, `thickenEdges`, the cuts, `roundPockets` | Each operation | Throws its error (above). |
| `thickenEdges`' catch, `failingBodies` (`try silent`) | Each try of a failed operation on one edge or body | What it's looking for: the edge or body is one which fails alone. |
| `robotLightenEditLogic` (`try silent`) | Finding the ribs and their plane | A guard: with no ribs yet, editing logic leaves the definition as it is. |

## Issues found

- Untested in Onshape: nothing here has run yet.
- Walls are an inset of the faces' extrude, so a part whose sides slope or step gets walls of the faces' outline, not
  of its sides.
- An extrude side is along an ignored face if its middle is in one, so a side only partly along one is treated as
  wholly along it, or not at all.
- Rounded corners are 0.01 mm (std's boolean tolerance) bigger than asked: the hair they're rounded by first.
- The icon is the generic robot icon.
