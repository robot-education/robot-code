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
| Failed to extrude regions to ignore. | error | extruding the sketch regions to ignore fails | `ignoredFaces`, the regions |
| Failed to cut regions to ignore from pockets. | error | cutting them from the extrude fails | `ignoredFaces`, the regions |
| Failed to extend pockets past their ends. | error | offsetting the pockets' ends (and sides along ignored faces) out fails | `wallThickness`, those faces |
| Failed to round walls' inside corners. | error | the hair fillet of the pockets' concave edges fails | `wallThickness`, the edges |
| Failed to make walls. | error | hollowing (or enclosing) the pockets fails | `wallThickness`, the pockets which fail alone |
| Failed to extrude rib. | error | extruding a rib's edge as a sheet (or its cylinder, for a small arc) fails | `ribEdges`, `ribThickness`, its edge |
| Failed to thicken rib. | error | thickening its sheet fails | `ribEdges`, `ribThickness`, its edge |
| Failed to cut ribs. | error | cutting them from the inset pockets fails | `ribEdges`, `ribThickness`, the ribs which fail alone |
| Failed to round pocket corners. | error | the hair fillet of the pockets' corners fails | `filletRadius`, the corners |
| Failed to grow pockets back to round their corners. | error | offsetting the pockets' sides fails | `filletRadius`, the pockets which fail alone |
| There's no room for pockets between the walls and ribs. | warning | no pockets are left (they're all narrower than the router bit, or than nothing) | `wallThickness`, `ribThickness`, `filletRadius` |
| Failed to cut pockets. | error | cutting them from the parts fails | `faces`, the pockets which fail alone |
| Some ribs touch no wall or other rib, so they're left as loose parts. | warning | cutting the pockets cuts pieces free | `ribEdges`, the loose parts |
| Lightened the `<part or parts>` by `<percent>`%. | info | the pockets are cut, and nothing's cut free (the warning above would be replaced) | |

### Triggering them

How to make each message show up in Onshape, to check its wording and what it highlights. None of these have been
tried yet. "Should" means the setup is invalid input that the feature checks for, or geometry an operation can't build;
"may" means it depends on how Parasolid handles the geometry. The rest are guards around operations which don't fail
on any input I know of: to see their display, temporarily change the operation's input in the code to something
impossible (like a hollow's thickness to `-1 * meter`), and push it to a scratch studio.

| Message | Setup | |
| --- | --- | --- |
| Select the faces to lighten. | Clear Faces to lighten. | should |
| Select ribs to use. | Clear Ribs to use, or select only construction lines with Exclude construction lines on. | should |
| The ribs must be in parallel sketches. | Select ribs from a sketch on Top and one on Front. | should |
| The faces to lighten must be parallel to the ribs. | Lighten a plate's top face with ribs sketched on Front. | should |
| std's extrude's errors | Up to face, with a face the extrude can't reach (one beside the plate, not under it). | should |
| Failed to extrude regions to ignore. | Ignore a sketch region on a plane perpendicular to the face to lighten (on Front, lightening a top face): it can't be extruded along its own plane. | should |
| Failed to cut regions to ignore from pockets. | A guard. | |
| Failed to extend pockets past their ends. | A guard: moving the extrude's ends outward. | |
| Failed to round walls' inside corners. | A guard: a 0.01 mm fillet of concave edges. | |
| Failed to make walls. | The hollow moves each side in by the wall thickness (plus the fillet radius): a convex curve in the face's outline tighter than that can't be. Lighten a plate whose outer corners are rounded smaller than the wall (a 1/32 in. corner radius with 1/8 in. walls), or whose outline has a spline with a tight bend; or make the walls thicker than half the face is wide. Two holes closer together than twice the wall is the other likely case. | may |
| Failed to extrude rib. | A guard. | |
| Failed to thicken rib. | A spline rib with a bend tighter than half the rib thickness (plus the fillet radius). An arc that tight is made as a cylinder instead, so it won't fail. | should |
| Failed to cut ribs. | A rib which only touches a wall, rather than crossing into a pocket (an arc around a hole, exactly as far out as the wall's inside, plus half the rib). Booleans usually handle this. | may |
| Failed to round pocket corners. | A guard: a 0.01 mm fillet of convex edges. | |
| Failed to grow pockets back to round their corners. | A guard: growing pockets back out to where they were. | |
| There's no room for pockets between the walls and ribs. | Ribs thick enough to cover everything inside the walls (a rib thickness wider than the gaps between ribs). | should |
| Failed to cut pockets. | A guard. | |
| Some ribs touch no wall or other rib, so they're left as loose parts. | A short rib in the middle of a pocket, touching nothing. | should |
| Lightened the `<part or parts>` by `<percent>`%. | Any lighten which works, of one part and of two. | should |

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
   3. Sketch regions among Faces to ignore are left solid, as Part Lighten's exclude regions are (`excludeRegions`):
      they're extruded along the sketch's normal through the extrude, and 5% past it (`bandExtent`), and cut from it,
      so walls go around them as they do the faces' holes. Parts' faces among them mean something else: no wall is
      left along them (below).
   4. The walls (`insetPockets`), as Ilya Baran and Morgan Bartlett's Lighten (and Part Lighten, `partLighten.local.fs`,
      which builds on it) make them: the extrude is inset by the wall thickness (and, with Fillet corners, the fillet
      radius), all at once. Its ends (`qCapEntity`), and its sides along parts' faces to ignore (found by tracking the
      edges the faces to lighten share with them, which the extrude sweeps into those sides), are offset out by that
      much; its concave edges are filleted by a hair (std's boolean
      tolerance, 0.01 mm); and it's hollowed (`opShell`) by that much, which moves every face in by it. Inside each,
      what's enclosed (`opEnclose`) is the inset, and the hollowed extrudes are deleted. So the ends, and the sides
      along ignored faces, are back where they were, and walls are left along every other side: around holes of any
      size, and with inside corners rounded to the wall thickness (and the hair), as a wall of that thickness has.
   5. The ribs (`buildRibs`), half the rib thickness to each side (and, with Fillet corners, the fillet radius), along
      the sketch's normal through the inset pockets and the ribs' edges, and 5% past them (`bandExtent`). Each is made on
      its own (edges extruded together make one sheet, creased where they meet, which can't be thickened): its edge
      is extruded as a sheet (a circle's is a tube), and thickened to each side; but an arc or circle whose radius is
      hardly more than that (up to 5% more) can't be thickened toward its center, so its rib is a cylinder around its
      center (`fCylinder`), that much bigger than it, instead (a little more than its rib, near its center). Arcs of
      one circle share a cylinder. They're cut from the inset pockets, which they split into the pockets.
   6. With Fillet corners, `roundPockets` rounds them, as Lighten does: their convex edges between sides (their
      corners) are filleted by a hair, and their sides offset out by the radius, growing them back from the
      thicker walls and ribs, which grows those fillets to the radius (and the hair). A pocket narrower than the bit
      is gone before then, and one which narrows between ribs ends in one round, as a router bit of that radius would
      cut it. Filleting the corners by the radius after growing them would fail where pockets narrow. Their ends
      (`pocketEnds`), which stay put, are the extrude's ends, tracked since they were made (so an up to face end on a
      curved face is one), and any faces parallel to the sketch, in case tracking misses them.
   7. If no pockets are left, a warning says so; otherwise they're cut from the faces' parts, and pieces they cut
      free (ribs touching no wall or other rib) are warned about and shown. Otherwise, info says how much lighter the
      part (or parts) are: how much less their volume is (to 0.1%), as they're one material.
   8. What's left of the ribs (their sheets) is deleted.
4. **Manipulator change** (`robotLightenManipulatorChange`): std's extrude manipulators.

### Error handling

Each operation which can fail has its own `try`, and its `catch` throws the feature's error for it, highlighting the
parameters which set what failed, and showing it in red. A failed operation changes nothing, so what it was given is
still there to show. A rib which fails highlights its edge. For the hollow, the
cuts, and the growing offset, the `catch` narrows that down: it tries the operation again on each pocket, rib, or
pocket alone (`failingBodies`; cuts on copies of what they cut, `copyBodies`, so each try sees what the
operation was given), and shows those which fail alone, or everything, if none does (it fails only on everything
together). That runs an operation per body, but only on the way to an error, which rolls it all back. Selections are checked before anything's built. Warnings are reported when they're found.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `excludeRegions`, `insetPockets`, `buildRibs`, the cuts, `roundPockets` | Each operation | Throws its error (above). |
| `failingBodies` (`try silent`) | Each try of a failed operation on one body | What it's looking for: the body is one which fails alone. |
| `robotLightenEditLogic` (`try silent`) | Finding the ribs and their plane | A guard: with no ribs yet, editing logic leaves the definition as it is. |

## Issues found

- Untested in Onshape: nothing here has run yet.
- Walls are an inset of the faces' extrude, so a part whose sides slope or step gets walls of the faces' outline, not
  of its sides.
- Rounded corners are 0.01 mm (std's boolean tolerance) bigger than asked: the hair they're rounded by first.
- The icon is the generic robot icon.
