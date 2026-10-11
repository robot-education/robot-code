# Robot lighten

Lightens a part with pockets. Everything within the extrude of the face to lighten (into its part, through it or to a
depth) is cut away, except walls along the face's edges (the part's sides and holes) and ribs. Ribs run along selected
sketch edges, and along quick ribs, which it sketches itself between points and to edges. The pockets' corners are
rounded as a router bit leaves them.

| File | What it is |
| --- | --- |
| `robotLighten.fs` | The feature |
| `quickRibs.fs` | Quick ribs: sketching them, the edge ribs' manipulators, and their editing logic |
| `../../tests/featurescript/quickRibs_test.fs` | Tests of the lines and circles quick ribs draw |
| `robotLightenIcon.py` | Its icon (`fs gen` writes `robotLightenIcon.svg`): the plate icon, pocketed |
| `vent.local.fs` | Vent, for reference (see Method comparison): not synced |
| `partLighten.local.fs` | Part Lighten (by Evan Fish, FRC 2471), for reference: not synced |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot lighten | feature name |
| Lighten a part with pockets, leaving walls around its edges and holes, and ribs along a sketch or between holes. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Add circle | Add a circle through the first selected spoke. |

None are hidden.

### Errors, warnings, and info

Errors show what's been built so far in blue and what's at fault in red, while the dialog's open (see Errors).

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select the face to lighten. | error | no face to lighten | `face` |
| Select faces to ignore. | error | Ignore faces is on, with none selected | `ignoredFaces` |
| Select faces to override. | error | a wall override without faces | its Faces |
| Face overrides must be adjacent to the lightened face. | error | a wall override's faces share no edge with the face to lighten | its Faces, the faces |
| Some quick ribs are skipped because they're incomplete. | warning | a chain with fewer than two points, a hub without a hub or spokes, or an edge rib without an edge or points | `chains`, `hubs`, `edgeRibs` |
| Select ribs to use. | error | no ribs at all: none selected (or only construction ones, excluded) and no quick ribs | `ribEdges` |
| Select ribs to override. | error | a rib override without ribs | its Ribs |
| The ribs must be in parallel sketches. | error | ribs from sketches on planes not parallel to the first's | the rib parameters, those ribs |
| The face to lighten must be parallel to the ribs. | error | it isn't | `face`, the rib parameters, the face |
| Failed to create pockets. | error | extruding the face fails | `face`, `depth`, the face |
| Failed to ignore the selected regions. | error | extruding or cutting the regions to ignore fails | `ignoredFaces`, the regions |
| Selected wall overrides are invalid. | error | the walls fail, but work without wall overrides | the overrides which fail alone (or all of them), and their faces |
| Fillet radius is too large for the pockets shown. | error | the walls fail, but work without Fillet radius | `filletRadius`, the pockets which fail alone |
| (the failed operation's own message) | error | the walls fail otherwise: hollowing, enclosing, or one of their fillets or offsets fails | `wallThickness` (and `filletRadius`), and the operation's own error display |
| Failed to create rib. | error | a rib's extrude, thicken, or cylinder fails, or cutting the ribs from the pockets fails | that rib's group's parameters, and its edge (or the ribs which fail alone) |
| (the failed operation's own message) | error | rounding the pockets' corners fails: a full round, fillet, offset, or fillet change | `filletRadius`, and the operation's own error display |
| There's no room for pockets between the walls and ribs. | warning | no pockets are left | the wall, fillet, and rib parameters |
| Failed to cut the pockets shown. | error | cutting the pockets from the part fails | `face`, the pockets which fail alone |
| Some ribs touch no wall or other rib, so they're left as loose parts. | warning | the cut leaves pieces of ribs free | the rib parameters, the loose parts |
| Weight reduced by `<percent>`%. | info | the pockets are cut, with no warning to replace | |

## UI

### Dialog

- **Selections** (at the top): Face to lighten, Ignore faces (with Faces to ignore), Ribs to use, and Exclude
  construction lines.
- **Quick ribs**: three arrays, each in a group of its own, then Clear invalid selections (a button).
  - Chains: each has Points (in order) and Closed. Selecting a point adds a chain.
  - Hub and spokes: each has a Hub, Spokes, and Add circle. Selecting a hub adds one.
  - Edge ribs: each has End (Perpendicular to edge or Manipulator), Edge, Points, and with Manipulator, Location.
- Wall thickness, Rib thickness, Fillet corners, and Fillet radius.
- **Override rib thickness**: an array of ribs at other thicknesses (labeled like "0.25 in ribs"). Later overrides
  win over earlier ones, and all of them over Rib thickness.
- **Override wall thickness**: an array of the part's faces whose walls are other thicknesses (labeled like
  "0.0625 in walls"). Later overrides win, and all of them win over Faces to ignore.
- End type (Through all or Blind), and Depth.

### Editing logic

`robotLightenEditLogic` only evaluates; it never builds.

- **Clear invalid selections** drops quick rib selections which no longer exist, chains with fewer than two points,
  hubs without a hub, and edge ribs without an edge or points (`clearInvalidSelections`).
- **Face to lighten** is filled in until it's set by hand:
  - with Ribs to use, the one part face in the ribs' sketch plane which they're over (`facesUnder`);
  - otherwise, while it's empty, the part face under the first quick rib point (`firstFaceUnder`).

### Manipulators

`robotLightenManipulatorChange` handles both:

- Depth (with Blind): an arrow on the face.
- Each Manipulator edge rib's Location: a point that slides along its edge (`edgeRibManipulatorChange`).

## Feature execution

```
robotLighten
--> getFace, getIgnoredFaces, getWallOverrides     // selections, checked before anything's built
--> sketchQuickRibs                                // quick ribs, sketched on the face's plane
    --> chainSegments, hubSegments, edgeRibSegments, uniqueSegments
--> addEdgeRibManipulators
--> getRibGroups --> ribGroups                     // ribs by thickness; later overrides win
--> ribPlane, verifyParallel
--> pocketDepth, addDepthManipulator
--> opExtrude "pocket"                             // the face, into its part
--> excludeRegions                                 // sketch regions to ignore, cut from the pocket
--> insetPockets                                   // the walls, in a feature of their own
    --> buildWalls
        --> tightCorners --> opDeleteFace          // tight rounded corners made sharp
        --> opOffsetFace "extend", "override"      // ends, ignored and overridden sides
        --> opFillet "roundConcave"                // a hair, std's boolean tolerance
        --> opShell, opEnclose                     // hollow by wall + radius; keep what's inside
        --> opModifyFillet "exactCorners"          // inside corners: exactly wall + radius
    --> wallFailure                                // only when they fail: which input is to blame
--> buildRibs --> buildRibGroup                    // each rib: a sheet thickened, or a cylinder
--> opBoolean "cutRibs"                            // ribs cut from the pockets
--> roundPockets                                   // with Fillet corners
    --> smallPockets --> opDeleteBodies "dropSmall"  // slivers under the radius both ways, left solid
    --> narrowEnds --> opFullRoundFillet
    --> pocketCorners --> opFillet "roundCorners"  // a hair
    --> opOffsetFace "grow"                        // back out by the radius
    --> opModifyFillet "exactCorners"              // corners (tracked through the grow): exactly the radius
--> opBoolean "cut"                                // pockets cut from the part
--> opDeleteBodies "cleanup"
```

### Walls

The walls follow the previous Onshape Lighten feature. The pocket (the face, extruded into its part) is hollowed by
the wall thickness plus the fillet radius, and what the hollow encloses is the inset pocket. Hollowing moves every
face in at once, so holes of any size and their corners get walls.

Before hollowing:

- The pocket's ends, and its sides along faces to ignore, are moved out by the same amount, so hollowing brings them
  back to where they were.
- Each wall override's sides move out by Wall thickness less its own.
- Concave edges get a hair fillet, which hollowing grows into the walls' rounded inside corners. After hollowing,
  they're set to exactly the hollow's depth (`opModifyFillet`, as Vent does), so growing the pockets back leaves them
  exactly the wall thickness.
- The part's rounded corners tighter than the hollow are made sharp. Hollowing can't move a convex face in by more
  than its radius, and the walls inside a sharp corner are the same as inside a rounded one that tight.

### Ribs

Each rib group's edges are extruded as sheets through the pockets, and each sheet thickened to half the group's
thickness (plus the fillet radius) on each side. Each rib is made on its own: edges extruded together make one sheet,
creased where they meet, which can't be thickened. A circle or arc too small to thicken toward its center (within
std's boolean tolerance of it) is a solid cylinder instead.

### Rounding the pockets

The walls and ribs were built thicker by the fillet radius. Growing the pockets back out by the radius rounds their
corners as a router bit of that radius would, and a pocket narrower than the bit is already gone. Corners get a hair
fillet first, which the growth rounds; then they're set to exactly the radius (`opModifyFillet`). The fillets are
tracked through the growth (`startTracking`).

Before that, pockets whose bounding box on the face is less than the fillet radius both ways are deleted, and
left solid: slivers between walls and ribs, not worth cutting, whose corners can be too sharp to round.

A pocket end so narrow that the hair fillets on its two corners would meet gets one full round across it instead.

### Extents

Nothing is padded by a margin. Through all goes exactly to the part's far side, and ribs and regions to ignore go
exactly through the pockets. Booleans treat faces within their tolerance as the same, so flush ends cut through (see
the style guide's "Tolerances, not margins").

### Errors

- Each operation has its own `try`. Onshape's own error display for it is copied onto the feature
  (`showOperationError`, std's `processSubfeatureStatus` with `propagateErrorDisplay`, on the feature's top level id).
- Where the operation's own error says what's wrong best (rounding the pockets, and the walls when no input is to
  blame), that error is thrown, as std's `callSubfeatureAndProcessStatus` rethrows a subfeature's
  (`operationError`). Throwing a message of our own replaces the status the error display belongs to.
- Elsewhere the `catch` throws a short message of ours. What's built so far shows in blue, and what's at fault in red
  (`showBuiltOnFailure`, `showOut`): the pockets' geometry as copies moved out of the part onto its face, selections
  in place.
- Where several bodies were given to a cut, the `catch` tries each alone and shows those which fail
  (`failingBodies`).
- The walls are built in a feature of their own (`startFeature`), so a failure rolls back cleanly. `wallFailure` then
  tries them again a few ways, each rolled back, and lays blame as if optional inputs weren't there:
  1. wall overrides, if the walls work without them;
  2. Fillet radius, if the walls work without it;
  3. otherwise the failed operation's own error.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| The pocket's extrude, `excludeRegions`, `buildRibs`, the cuts, `roundPockets` | Each operation | Throws its error. |
| `insetPockets` (`try silent`) | Building the walls | Rolled back; `wallFailure` works out the error. |
| `buildWalls` (`try silent`) | Each step | Throws which step failed, for `wallFailure`. |
| `wallFailure`, `failingBodies` (`try silent`) | Each trial | What they're looking for: whether that trial works. |
| `showOut` (`try silent`) | Copying what an error shows | Less is shown; the error's still thrown. |
| `pointsOf` (`try silent`) | Each quick rib point | Left out: it isn't there any more. |
| `edgeRibSegments` (`try silent`) | Where an edge rib meets its edge | The edge rib is skipped. |
| `addEdgeRibManipulators` (`try silent`) | Each edge rib's manipulator | No manipulator: its edge isn't there. |
| `robotLightenEditLogic`, `firstFaceUnder` (`try silent`) | Finding the face to fill in | Left as it is. |

## Method comparison

Robot lighten, Vent (`vent.local.fs`), and Part Lighten (`partLighten.local.fs`) all build walls the way the previous
Onshape Lighten feature did: extrude the face, hollow it (`opShell`), and keep what the hollow encloses (`opEnclose`).

| | Robot lighten | Vent | Part Lighten |
| --- | --- | --- | --- |
| What's lightened | One face of one part | Selected faces on parts' faces (any number of parts) | One face of one or more parts |
| Ribs | Sketch edges of any shape, each thickened on its own; quick ribs; overrides by group | None: separate faces leave a wall between them, half from each | Sketched rectangles (and arcs) around selected lines |
| Walls along the part's edges | Hollow of the face's extrude, ends moved out first | Second extrude of the part's coplanar faces, hollowed by the full wall, intersected (`SUBTRACT_COMPLEMENT`) | Hollow of the face's extrude |
| Inside wall corners | Hair fillet, grown, then `opModifyFillet` to exactly the offset | As Robot lighten | As Robot lighten |
| Rounded pocket corners | Hair fillet, grown, then `opModifyFillet` to exactly the radius | As Robot lighten | As Robot lighten, with the tool radius rounded up by 0.01 in |
| End types | Through all, Blind | Blind, Up to face, Up to part, Through all | Blind, Through, Up to face, Up to vertex |
| Extents | Exact (tight bounding box) | Exact (tight bounding box) | Measured distances; tool radius rounded |
| Narrow pocket ends | Full round | Not handled | Not handled; sharp edges where walls barely meet are found after the cut, and their faces deleted (`opDeleteFace`) |
| Tight rounded part corners | Made sharp before hollowing | Not handled | Not handled |
| Errors | Diagnosed: which input, shown in blue and red, with the operation's own display | Std errors; a warning if trimming fails | Generic messages |
| Feature patterns, sheet metal | No | Yes (`getRemainderPatternTransform`, `booleanBodies`) | No |

Taken from Vent: `opModifyFillet`, setting the hair fillets to their exact radii. Still worth taking: Up to face and Up to part end types, and feature pattern support.

## Issues found

- Untested in Onshape.
- Small pockets are measured by their bounding box (loose) along the face's x and y axes, so a sliver turned on the
  face can measure up to √2 bigger, and the box itself can be a little bigger than the pocket. Without Fillet corners, slivers are cut as they are.
- Walls follow the face's outline, so a part whose sides slope or step gets walls of the face's outline, not of its
  sides.
- An overridden wall steps to its thickness at the corners its side meets others at. A much thicker override moves
  its side in before the hollow, which can close a small pocket.
- A rounded part corner spanning half a circle or more (a lobe narrower than the walls) has no sides to extend, so it
  can't be made sharp, and fails.
- Errors' copies are shown out of the part, but Onshape's own error display for an operation is in place, inside it.
- Quick ribs are always at Rib thickness: overrides select sketch edges, and quick ribs' sketch is the feature's own.
- A hub's circle goes through its first spoke in the order selected.
- With Manipulator, an edge rib's points all meet its edge at one Location. Perpendicular to edge projects the edge's
  nearest point onto the face, which is only square for edges in (or parallel to) its plane.
