# Robot lighten

Lightens a part with pockets. Everything within the extrude of the face to lighten (into its part, through it or to a
depth) is cut away, except walls along the face's edges (the part's sides and holes) and ribs. Ribs run along selected
sketch edges: a sketch's, or Quick rib's (`../rib/quickRib.fs`), which sketches them between points and to edges. The
pockets' corners are rounded as a router bit leaves them.

| File | What it is |
| --- | --- |
| `robotLighten.fs` | The feature |
| `../../tests/featurescript/lighten_test.fs` | Tests of which pockets are small |
| `robotLightenIcon.py` | Its icon (`fs gen` writes `robotLightenIcon.svg`): the plate icon, pocketed |
| `vent.local.fs` | Vent, for reference (see Method comparison): not synced |
| `partLighten.local.fs` | Part Lighten (by Evan Fish, FRC 2471), for reference: not synced |

## Changelog

Unreleased.

- Quick ribs are a feature of their own again (Quick rib): select its sketch's edges in Ribs to use.

## Strings

| String | Where |
| --- | --- |
| Robot lighten | feature name |
| Lighten a part with pockets, leaving walls around its edges and holes, and ribs along a sketch. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

No parameters have descriptions, and none are hidden.

### Errors, warnings, and info

Errors show what's at fault in red, with the failed operation's own error display. Warnings and info are reported
where they come up, and show only once it's done; only the first reported shows.

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select the face to lighten. | error | no face to lighten | `face` |
| Select faces to ignore. | error | Ignore faces is on, with none selected | `ignoredFaces` |
| Select faces to override. | error | a wall override without faces | its Faces |
| Face overrides must be adjacent to the lightened face. | error | a wall override's faces share no edge with the face to lighten | its Faces, the faces |
| Select ribs to use. | error | no ribs at all: none selected (or only construction ones, excluded) | `ribEdges` |
| Select ribs to override. | error | a rib override without ribs | its Ribs |
| The ribs must be in parallel sketches. | error | ribs from sketches on planes not parallel to the first's | the rib parameters, those ribs |
| The face to lighten must be parallel to the ribs. | error | it isn't | `face`, the rib parameters, the face |
| Failed to create pockets. | error | extruding the face fails | `face`, `depth`, the face |
| Failed to ignore the selected regions. | error | extruding or cutting the regions to ignore fails | `ignoredFaces`, the regions |
| The regions to ignore cover the whole face. | error | nothing's left of the pocket after cutting them | `ignoredFaces`, the face and the regions |
| Selected wall overrides are invalid. | error | moving an override's sides fails | that override's parameters and faces, and the offset's error display |
| Failed to create the walls along the faces shown. | error | moving the pockets' ends and sides along faces to ignore out fails | `wallThickness`, `filletRadius` (and `wallOverrides`), and those faces |
| Failed to round the walls' inside corners. | error | the hair fillet on the pockets' concave edges fails | the same, the edges, and the fillet's error display |
| Failed to inset the pockets by the wall thickness. | error | hollowing fails | the same, and the pockets |
| Failed to inset the pocket shown. | error | enclosing a hollowed pocket fails | the same, and that pocket |
| There's no room for pockets inside the walls. | error | hollowing encloses nothing: every pocket's narrower than twice the walls | the same, and the hollowed pockets |
| The walls' inside corners are rounded 0.01 mm larger than the wall thickness. | info | setting the walls' inside corners to their exact radius fails | |
| Failed to create rib. | error | a rib's extrude, thicken, or cylinder fails | that rib's group's parameters, and its edge |
| Failed to cut the ribs from the pockets. | error | cutting them fails | the rib parameters, and the ribs |
| There's no room for pockets between the walls and ribs. | error | the ribs leave nothing of the pockets | the wall, fillet, and rib parameters, and the face and ribs (what was cut is used up) |
| The pockets are all too small to cut. | error | every pocket's small (see Small pockets) | `wallThickness`, `filletRadius`, and the pockets |
| Failed to fillet pocket. | warning | a corner's fillet fails, and neither side next to it has another corner to round across to: it's left sharp | `filletRadius`, and the corner |
| Failed to round pocket. | warning | a full round across a failed corner's narrow side fails: the corner's left sharp, and the fillet it deleted is put back | `filletRadius`, the corner and the side |
| Failed to grow back pockets. | warning | growing the rounded pockets back fails: they're cut smaller, by the fillet radius | `filletRadius`, the pockets, and the growth's error display |
| Failed to cut the pockets from the part. | error | cutting them fails | `face`, and the pockets |
| Some ribs touch no wall or other rib, so they're left as loose parts. | warning | the cut leaves pieces of ribs free | the rib parameters, the loose parts |
| Weight reduced by `<percent>`%. | info | the pockets are cut (shown if nothing was reported before it) | |

## UI

### Dialog

- **Selections** (at the top): Face to lighten, Ignore faces (with Faces to ignore), Ribs to use, and Exclude
  construction lines.
- Wall thickness, Rib thickness, Fillet corners, and Fillet radius.
- **Override rib thickness**: an array of ribs at other thicknesses (labeled like "0.25 in ribs"). Later overrides
  win over earlier ones, and all of them over Rib thickness.
- **Override wall thickness**: an array of the part's faces whose walls are other thicknesses (labeled like
  "0.0625 in walls"). Later overrides win, and all of them win over Faces to ignore.
- End type (Through all or Blind), and Depth.

### Editing logic

`robotLightenEditLogic` only evaluates; it never builds.

- **Face to lighten** is filled in until it's set by hand: the one part face in the ribs' sketch plane which they're
  over (`facesUnder`).

### Manipulators

`robotLightenManipulatorChange`: Depth (with Blind), an arrow on the face.

## Feature execution

```
robotLighten
--> getFace, getIgnoredFaces, getWallOverrides     // selections, checked before anything's built
--> getRibGroups --> ribGroups                     // ribs by thickness; later overrides win
--> ribPlane, verifyParallel
--> pocketDepth, addDepthManipulator
--> opExtrude "pocket"                             // the face, into its part
--> excludeRegions                                 // sketch regions to ignore, cut from the pocket
--> buildWalls                                     // the walls, in steps:
    --> opOffsetFace "extend"                      // 1. ends and ignored sides out
    --> opOffsetFace "override"                    // 2. each override's sides
    --> opFillet "roundConcave"                    // 3. concave edges, a hair (std's boolean tolerance)
    --> opShell, opEnclose                         // 4. hollow by wall + growth; keep what's inside
    --> opModifyFillet "exactCorners"              // 5. inside corners: exactly wall + growth (info if not)
--> buildRibs --> buildRibGroup                    // each rib: a sheet thickened, or a cylinder
--> opBoolean "cutRibs"                            // ribs cut from the pockets
--> smallPockets --> opDeleteBodies "dropSmall"    // too small to be worth cutting: left solid
--> roundPockets                                   // with Fillet corners, in steps:
    --> opFillet "fillet"                          // 1. each corner on its own, a hair, kept if it works
    --> narrowSide                                 // 2. each failed corner: the side to its nearest other corner
        --> opDeleteFace, opFullRoundFillet        //    that corner's fillet deleted, a full round across the side
    --> opOffsetFace "grow"                        // 3. the pockets, out by the radius less the hair
--> opBoolean "cut"                                // pockets cut from the part
--> opDeleteBodies "cleanup"
```

### Walls

The walls follow the previous Onshape Lighten feature. The pocket (the face, extruded into its part) is hollowed by
the wall thickness plus the pockets' growth (see Rounding), and what the hollow encloses is the inset pocket. Hollowing moves every
face in at once, so holes of any size and their corners get walls.

Before hollowing:

- The pocket's ends, and its sides along faces to ignore, are moved out by the same amount, so hollowing brings them
  back to where they were.
- Each wall override's sides move out by Wall thickness less its own.
- Concave edges get a hair fillet, which hollowing grows into the walls' rounded inside corners. After hollowing,
  they're set to exactly the hollow's depth (`opModifyFillet`, as Vent does), so growing the pockets back leaves them
  exactly the wall thickness. If that fails, they're left a hair bigger, and the info says so.

Every other step's failure fails the feature, with a message saying which step, and what it was given shown in red
(see the errors table). An override's offset failing is "Selected wall overrides are invalid.", with its faces. A
pocket narrower than twice the walls encloses nothing; if none of them encloses anything, that's an error, rather
than a feature with no result.

Offsetting the face's edges in 2D (`opOffsetWire` with `makeRegions`, as std's thin extrudes do) was tried instead,
and dropped: it hit an Onshape bug on one plate's face, and it regenerated slower than hollowing on the plates tried.

With logging on (a variable `#logging` of `true`, see the style guide's Logging), it prints the wall thickness,
radius, and growth, and how many pockets each step leaves (inside the walls, between the walls and ribs, and too
small), and shows them as debug entities.

### Ribs

Each rib group's edges are extruded as sheets through the pockets, and each sheet thickened to half the group's
thickness (plus the pockets' growth) on each side. Each rib is made on its own: edges extruded together make one sheet,
creased where they meet, which can't be thickened. A circle or arc too small to thicken toward its center (within
std's boolean tolerance of it) is a solid cylinder instead.

### Rounding the pockets

The walls and ribs were built thicker by the fillet radius less a hair (std's boolean tolerance, 0.01 mm). Growing
the pockets back out by that rounds their corners as a router bit of the radius would, and a pocket narrower than
the bit is already gone. This is Vent's `expandWithFillet`, and the previous Onshape Lighten feature's: corners get a
hair fillet first, which the growth rounds. Vent grows by the whole radius, and sets the rounds back to it
(`opModifyFillet`), since a hair and the growth make a round a hair bigger. Growing by the radius less the hair makes
it exactly, with nothing to set after.

Growing first and filleting after, by the radius, would round them the same, but each corner's fillet would then
have to fit on its own, and a sharp corner's offset can fail where a rounded one's doesn't. A hair fillet fails only
where two corners are within a hair of each other.

Each pocket is rounded on its own:

1. Each corner is filleted a hair on its own, in turn, and kept if it works. A corner fails where the side between it
   and its neighbor is too short for both their fillets: whichever is tried second.
2. Each corner which failed gets a full round across its narrow side (`opFullRoundFillet`), which growing makes a
   round end as wide as the side. Its narrow side (`narrowSide`) is the side next to it whose other corner (one which
   failed too, or one filleted) is nearest it: what made it fail. Where both its sides are short, that's a guess. If
   the other corner was filleted, its fillet is deleted first (`opDeleteFace`, which heals it back to a corner), so
   the round goes from side to side. The delete and the round are a feature of their own (`startFeature`), so if the
   round fails, the feature's rolled back (`abortFeature`), putting the fillet back.
3. The pockets are grown back.

Nothing in rounding fails the feature. A corner with no other corner on either side, or a full round which fails, is a
warning, showing the corner (and side) left sharp; growing them back failing is a warning, showing the pockets, which
are cut smaller.

Full rounds grow to half the side's width (under a hair) and the growth, which is what the bit's two rounds with that
width between them amount to.

#### Small pockets

Pockets too small to be worth cutting are left solid, with or without Fillet corners. This is a heuristic: a pocket
is small if its area, as cut, is less than a circle twice as wide as the bit or the wall thickness, whichever is wider
(`isSmallPocket`). With a 1/8 in. bit and 1/8 in. walls, that's a circle 1/4 in. across. Its area as cut is
estimated before it's grown back: its area, plus its outline's length times the radius, plus the bit's (Steiner's
formula: exact for convex pockets, and a little more for others).

The pockets' ends are the pocket extrude's caps (`qCapEntity`). The walls' first step uses them as they are; from
after it, they're tracked through hollowing, the cuts, and rounding (`startTracking`, as Vent tracks its fillets
through hollowing). Tracking them from straight after the extrude takes in the extrude too, whose sides are derived
from its caps, and finds every face. Every other face is a side, and the
corners are a pocket's convex edges not on its ends (`pocketCorners`): the pockets are the face's outline extruded,
so each runs along the face's normal, between two sides. The ends aren't grown.

### Extents

Nothing is padded by a margin. Through all goes exactly to the part's far side, and ribs and regions to ignore go
exactly through the pockets. Booleans treat faces within their tolerance as the same, so flush ends cut through (see
the style guide's "Tolerances, not margins").

### Errors

- Each step is an operation of its own, with a `try` only where its failure needs something said, or where it can
  carry on. Its `catch` copies the operation's status and error display onto the feature (`processSubfeatureStatus`
  with `propagateErrorDisplay`, on the feature's top level id), then throws a short message saying which step failed,
  highlighting the parameters which set it, and showing what it was given: most operations show nothing of their own
  (`opFillet` is the exception).
- Where a step can leave nothing (the regions to ignore, the walls, the ribs' cut, dropping small pockets), it's
  checked, and nothing left is an error there, showing what's still there to show: when the cut used up the pockets
  and ribs, the face and the rib edges.
- Failures it can carry on from aren't errors: anything in rounding (a corner left sharp, or the pockets left
  ungrown) is a warning, and setting the walls'
  inside corners' exact radius is info. Each is reported where it comes up; only the first reported shows. A warning is reported
  before the failed operation's status is copied, so the warning shows, with the operation's error display.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| The pocket's extrude, `excludeRegions`, `buildWalls`, `buildRibGroup`, the cuts, the pockets' growth | Each operation | Fails the feature, with a message of ours and the operation's error display. |
| `roundPockets` (`try silent`) | Each corner's fillet | The corner's tried again in step 2. |
| `roundPockets` (`try silent`) | Each narrow side's fillet delete and full round | A warning, showing the corner and side; the feature they're in is rolled back. |
| `roundPockets` (`try silent`) | Growing the pockets back | A warning, showing the pockets, cut smaller. |
| `buildWalls` (`try silent`) | Setting the walls' inside corners to their exact radius | Info: they're a hair bigger. |
| `robotLightenEditLogic` (`try silent`) | Finding the face to fill in | Left as it is. |

## Method comparison

Robot lighten, Vent (`vent.local.fs`), and Part Lighten (`partLighten.local.fs`) all build walls the way the previous
Onshape Lighten feature did: extrude the face, hollow it (`opShell`), and keep what the hollow encloses (`opEnclose`).

| | Robot lighten | Vent | Part Lighten |
| --- | --- | --- | --- |
| What's lightened | One face of one part | Selected faces on parts' faces (any number of parts) | One face of one or more parts |
| Ribs | Sketch edges of any shape (Quick rib's too), each thickened on its own; overrides by group | None: separate faces leave a wall between them, half from each | Sketched rectangles (and arcs) around selected lines |
| Walls along the part's edges | Hollow of the face's extrude, ends moved out first | Second extrude of the part's coplanar faces, hollowed by the full wall, intersected (`SUBTRACT_COMPLEMENT`) | Hollow of the face's extrude |
| Inside wall corners | Hair fillet, grown, then `opModifyFillet` to exactly the offset | As Robot lighten | As Robot lighten |
| Rounded pocket corners | Hair fillet, grown by the radius less the hair: exactly the radius | Hair fillet, grown by the radius, then `opModifyFillet` to exactly it | As Vent, with the tool radius rounded up by 0.01 in |
| End types | Through all, Blind | Blind, Up to face, Up to part, Through all | Blind, Through, Up to face, Up to vertex |
| Extents | Exact (tight bounding box) | Exact (tight bounding box) | Measured distances; tool radius rounded |
| Narrow pocket ends | Full round, where a side's two corners can't be filleted | Not handled | Not handled; sharp edges where walls barely meet are found after the cut, and their faces deleted (`opDeleteFace`) |
| Small pockets | Left solid: less area than a circle twice the bit or wall | Not handled | Not handled |
| Tight rounded part corners | Not handled | Not handled | Not handled |
| Inside corners not set exactly | Info: a hair bigger | Not handled | Not handled |
| Errors | Short messages, highlighting the parameters, with the operation's own display; warnings for pockets it can't fillet | Std errors; a warning if trimming fails | Generic messages |
| Feature patterns, sheet metal | No | Yes (`getRemainderPatternTransform`, `booleanBodies`) | No |

Taken from Vent: rounding the pockets' corners by a hair fillet, grown, and `opModifyFillet`, setting the walls'
inside corners to their exact radius. Still worth taking: Up to face and Up to part end types, and feature pattern
support.

## Issues found

- Untested in Onshape.
- Small pockets are found by a heuristic (twice the bit or wall), which is untested on real parts.
- Walls follow the face's outline, so a part whose sides slope or step gets walls of the face's outline, not of its
  sides.
- An overridden wall steps to its thickness at the corners its side meets others at. A much thicker override moves
  its side in before the hollow, which can close a small pocket.
- Hollowing can't move a convex face in by more than its radius, so a rounded part corner tighter than the walls
  (or a lobe narrower than them) fails the walls.
- A narrow side next to a concave corner (one side of it a rib's or wall's corner) has no other corner to round
  across to, so its corner's left sharp with a warning.
- Filleting each corner on its own costs a fillet per corner, where one fillet of all of them did before.
- Untested in Onshape: deleting a hair fillet to heal it back to a corner (`opDeleteFace`), and rounding in a feature
  of its own inside the feature (`startFeature`, `endFeature`, `abortFeature`).
