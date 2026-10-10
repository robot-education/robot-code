# Robot lighten

Lightens a part with pockets: everything within the extrude of the face to lighten (into its part, through it or to
a depth) is cut away, but for walls along the face's edges (the part's sides and holes) and ribs along the selected
sketch edges (lines, arcs, circles, or splines), with the pockets' corners rounded as a router bit leaves them.

| File | What it is |
| --- | --- |
| `robotLighten.fs` | The feature: its dialog, the pocket, walls, ribs, and fillets, and its editing logic |
| `partLighten.local.fs` | Part Lighten (by Evan Fish, FRC 2471, from Ilya Baran and Morgan Bartlett's Lighten), for reference: not synced |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot lighten | feature name |
| Lighten a part with pockets, leaving walls around its edges and holes, and ribs along a sketch. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

No parameters have descriptions, and none are hidden. Editing logic sets the shown Face to lighten (see Execution
order).

### Errors, warnings, and info

Errors from a failed operation show what failed in red: what it was given, narrowed down (for some) to what fails when
it's tried alone (see Error handling).

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select the face to lighten. | error | no face to lighten | `face` |
| Select ribs to use. | error | no ribs (or only construction ones, excluded) | `ribEdges` |
| The ribs must be in parallel sketches. | error | ribs from sketches on planes which aren't parallel to the first's | `ribEdges`, those ribs |
| The face to lighten must be parallel to the ribs. | error | it isn't | `face`, `ribEdges`, the face |
| Failed to extrude the pocket. | error | extruding the face into its part fails | `face`, `depth`, the face |
| Failed to extrude regions to ignore. | error | extruding the sketch regions to ignore fails | `ignoredFaces`, the regions |
| Failed to cut regions to ignore from pockets. | error | cutting them from the pocket fails | `ignoredFaces`, the regions |
| Failed to extrude walls. | error | extruding the walls' sketch regions fails | `wallThickness`, the regions |
| Failed to cut walls. | error | cutting the walls from the pocket fails | `wallThickness`, the walls |
| Failed to extrude rib. | error | extruding a rib's edge as a sheet (or its cylinder, for a small arc) fails | `ribEdges`, `ribThickness`, its edge |
| Failed to thicken rib. | error | thickening its sheet fails | `ribEdges`, `ribThickness`, its edge |
| Failed to cut ribs. | error | cutting them from the pockets fails | `ribEdges`, `ribThickness`, the ribs which fail alone |
| Failed to round pocket corners. | error | the hair fillet of the pockets' corners fails | `filletRadius`, the corners |
| Failed to grow pockets back to round their corners. | error | offsetting the pockets' sides fails | `filletRadius`, the pockets which fail alone |
| There's no room for pockets between the walls and ribs. | warning | no pockets are left (they're all narrower than the router bit, or than nothing) | `wallThickness`, `ribThickness`, `filletRadius` |
| Failed to cut pockets. | error | cutting them from the part fails | `face`, the pockets which fail alone |
| Some ribs touch no wall or other rib, so they're left as loose parts. | warning | cutting the pockets cuts pieces free | `ribEdges`, the loose parts |
| Lightened the part by `<percent>`%. | info | the pockets are cut, and nothing's cut free (the warning above would be replaced) | |

## How it works

### Execution order

1. **Editing logic** (`robotLightenEditLogic`): unless Face to lighten has been set, and once there are ribs,
   `facesUnder` finds the faces in the first rib's sketch plane (of parts which aren't hidden) whose bounding boxes, in
   the plane, overlap the ribs'; if there's just one, it's the face to lighten. It only evaluates: building anything in
   editing logic (a trial feature, between `startFeature` and `abortFeature`) can crash the Part Studio.
2. **Body**:
   1. The face to lighten (`getFace`), its plane (whose normal points out of its part), and its part; the ribs
      (`getRibEdges`, less construction ones) and their plane (`ribPlane`: the first's sketch plane, which the rest's
      must be parallel to), which the face must be parallel to (`verifyParallel`). The ribs can be from any number of
      sketches.
   2. The pocket: the face extruded into its part, against its normal (`opExtrude`), by Depth, or for Through all, a
      little past the part's far side (`pocketDepth`, from its bounding box in the face's plane: 5% more, or at least
      0.1 mm). With Blind, a manipulator drags Depth. The pocket is only ever cut from the face's part.
   3. Sketch regions among Faces to ignore are left solid, as Part Lighten's exclude regions are (`excludeRegions`):
      they're extruded along the sketch's normal through the pocket, and 5% past it (`bandExtent`), and cut from it.
      Parts' faces among them mean something else: no wall is left along them (below).
   4. The walls (`cutWalls`), the wall thickness (and, with Fillet corners, the fillet radius) thick: along the face's
      edges, but those it shares with the part's faces to ignore, and around the regions to ignore. One sketch, on the
      face's plane (moved halfway through the pocket), has a capsule around each edge (`wallCurves` reads each as a
      line, arc, circle, or for anything else a polyline through points at most half the wall apart; `sketchWalls`
      draws them): its sides the wall thickness to each side (an arc's or circle's concentric arcs, or for one no
      bigger than the wall, a wedge to its center), and a circle that size at each end (once, where edges meet), but
      at the ends of the sides along faces to ignore, which get straight ends so the walls stop there. Each edge is
      drawn in it too, as construction. The capsules' outlines split the sketch into regions; those closer to the
      edges (their construction copies) than the wall are walls (`isWallRegion`, by `evDistance`), and the rest (like
      the face's middle, and its holes) are outside every capsule, at least the wall's thickness from every edge. The
      walls are extruded through the pocket and cut from it, which leaves as many pockets as there's room for: a hole
      near the edge, or two near each other, just leave no pocket between them.
   5. The ribs (`buildRibs`), half the rib thickness to each side (and, with Fillet corners, the fillet radius), along
      the sketch's normal through the pockets and the ribs' edges, and 5% past them (`bandExtent`). Each is made on its
      own (edges extruded together make one sheet, creased where they meet, which can't be thickened): its edge is
      extruded as a sheet (a circle's is a tube), and thickened to each side; but an arc or circle whose radius is
      hardly more than that (up to 5% more) can't be thickened toward its center, so its rib is a cylinder around its
      center (`fCylinder`), that much bigger than it, instead (a little more than its rib, near its center). Arcs of
      one circle share a cylinder. They're cut from the pockets, which they split further.
   6. With Fillet corners, `roundPockets` rounds them, as Lighten does: their convex edges between sides (their
      corners) are filleted by a hair (std's boolean tolerance, 0.01 mm), and their sides offset out by the radius,
      growing them back from the thicker walls and ribs, which grows those fillets to the radius (and the hair). A
      pocket narrower than the bit is gone before then, and one which narrows between ribs ends in one round, as a
      router bit of that radius would cut it. Filleting the corners by the radius after growing them would fail where
      pockets narrow. Their ends (`pocketEnds`), which stay put, are the extrude's ends, tracked since they were made,
      and any faces parallel to the face, in case tracking misses them.
   7. If no pockets are left, a warning says so; otherwise they're cut from the part, and pieces they cut free (ribs
      touching no wall or other rib) are warned about and shown. Otherwise, info says how much lighter the part is: how
      much less its volume is (to 0.1%).
   8. What's left of the ribs (their sheets) and the sketches are deleted.
3. **Manipulator change** (`robotLightenManipulatorChange`): dragging the depth manipulator sets Depth (its absolute
   value).

### Error handling

Each operation which can fail has its own `try`, and its `catch` throws the feature's error for it, highlighting the
parameters which set what failed, and showing it in red. A failed operation changes nothing, so what it was given is
still there to show. A rib which fails highlights its edge. For the cuts and the growing offset, the `catch` narrows
that down: it tries the operation again on each rib or pocket alone (`failingBodies`; cuts on copies of what they cut,
`copyBodies`, so each try sees what the operation was given), and shows those which fail alone, or everything, if none
does (it fails only on everything together). That runs an operation per body, but only on the way to an error, which
rolls it all back. Selections are checked before anything's built. Warnings are reported when they're found.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| The pocket's extrude, `excludeRegions`, `cutWalls`, `buildRibs`, the cuts, `roundPockets` | Each operation | Throws its error (above). |
| `failingBodies` (`try silent`) | Each try of a failed operation on one body | What it's looking for: the body is one which fails alone. |
| `robotLightenEditLogic` (`try silent`) | Finding the ribs and their plane | A guard: with no ribs yet, editing logic leaves the definition as it is. |

## Issues found

- Untested in Onshape: nothing here has run yet. The walls' sketch has a few entities per edge, all overlapping where
  edges meet; how quickly Onshape finds its regions, for a face with hundreds of edges, isn't known.
- Walls follow the face's outline, so a part whose sides slope or step gets walls of the face's outline, not of its
  sides.
- A spline edge's wall follows a polyline through points at most half the wall apart, so it's off by a little where the
  spline bends.
- A sliver of a capsule right at its outline (where capsules overlap at a shallow angle) can be taken for outside the
  walls, leaving a notch in the wall that thin.
- Rounded corners are 0.01 mm (std's boolean tolerance) bigger than asked: the hair they're rounded by first.
- The icon is the generic robot icon.
