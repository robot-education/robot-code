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
| `../../tests/featurescript/lighten_test.fs` | Tests of which pockets are small |
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

Errors show what's at fault in red, with the failed operation's own error display. Warnings and info are reported
where they come up, and show only once it's done; only the first reported shows.

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
| The regions to ignore cover the whole face. | error | nothing's left of the pocket after cutting them | `ignoredFaces`, the face and the regions |
| Failed to create the wall along the edges shown. | error | offsetting a chain of the face's edges fails | `wallThickness` and `filletRadius`, or the override's parameters, and the edges |
| Failed to create the wall around the region shown. | error | offsetting a region to ignore's edges fails | `ignoredFaces`, `wallThickness`, and the edges |
| Failed to create walls. | error | extruding the walls' bands fails | `wallThickness`, `filletRadius`, and the bands |
| Failed to cut the walls from the pockets. | error | cutting them fails | the same, and the walls |
| There's no room for pockets inside the walls. | error | the walls leave nothing of the pockets | the same, and the face |
| Failed to create rib. | error | a rib's band, extrude, or cylinder fails | that rib's group's parameters, and its edge |
| Failed to cut the ribs from the pockets. | error | cutting them fails | the rib parameters, and the ribs |
| There's no room for pockets between the walls and ribs. | error | the ribs leave nothing of the pockets | the wall, fillet, and rib parameters, and the face and ribs (what was cut is used up) |
| The pockets are all too small to cut. | error | every pocket's small (see Small pockets) | `wallThickness`, `filletRadius`, and the pockets |
| Failed to round a narrow pocket end. | warning | a narrow end's full round fails: that pocket's left smaller | `filletRadius`, and the end's faces |
| Failed to fillet pocket. | warning | a pocket's corner fillet fails (with no narrow ends, or after their full rounds): that pocket's left smaller | `filletRadius`, its corners, and the fillet's error display (failed fillets in pink, bad edges in red) |
| Failed to grow the pockets back by the fillet radius. | error | growing the filleted pockets back fails | `filletRadius`, and the pockets |
| Failed to cut the pockets from the part. | error | cutting them fails | `face`, and the pockets |
| Some ribs touch no wall or other rib, so they're left as loose parts. | warning | the cut leaves pieces of ribs free | the rib parameters, the loose parts |
| Weight reduced by `<percent>`%. | info | the pockets are cut (shown if nothing was reported before it) | |

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
--> buildWalls                                     // the walls, in steps:
    --> faceLoops, wallChains                      // 1. the face's loops, in chains with the same wall
    --> opOffsetWire (offsetBand)                  // 2. each chain, a band into the face; regions to ignore too
    --> fCylinder "corner"                         //    where chains with walls meet
    --> opExtrude, opBoolean "cut"                 // 4. the bands, through the pockets, cut from them
--> buildRibs --> buildRibGroup                    // each rib: a band both sides, extruded, or a cylinder
--> opBoolean "cutRibs"                            // ribs cut from the pockets
--> smallPockets --> opDeleteBodies "dropSmall"    // too small to be worth cutting: left solid
--> roundPockets                                   // with Fillet corners, in steps:
    --> opFillet "fillet"                          // 1. each pocket's corners, a hair
    --> narrowSides --> opFullRoundFillet          // 2. only if it fails: narrow sides, found by trying their
        --> opFillet "refillet"                    //    corners alone; a full round across each, then fillet again
    --> opOffsetFace "grow"                        // 3. the rounded pockets, out by the radius less the hair
--> opBoolean "cut"                                // pockets cut from the part
--> opDeleteBodies "cleanup"
```

### Walls

The walls are bands along the face's edges, `opOffsetWire`'s flat regions between an edge chain and its offset, each
Wall thickness (and the pockets' growth, see Rounding) wide. Extruded through the pocket, they're cut from it, and
what's left is the inset pocket: the face's region less its walls, in 2D. The bands are true offsets, so the walls'
inside corners (where the face's outline turns in, and around holes) come out as arcs of their width, and a neck
which the walls close splits the pocket into pieces.

- **Which way**: for a closed chain, `opOffsetWire`'s `offset1` is outward of the region it encloses, and `offset2`
  inward. The face's outer loop is offset in (`offset2`), and its holes' loops out of them (`offset1`), into the face.
  The outer loop is the one whose bounding box is the face's. A band offset both ways around a hole fails where it's
  wider than the hole's radius, so each loop goes one way.
- **Faces to ignore and overrides**: a loop's edges are split into chains with the same wall (`wallChains`): none
  along faces to ignore, an override's thickness along its faces, and Wall thickness elsewhere. A loop split into
  chains is offset both ways, since an open chain's sides depend on its first edge: the half outside the face is
  where there's nothing to cut. Where two chains with walls meet, a cylinder of the thicker one's width fills the
  corner between their bands' square ends.
- **Regions to ignore** are cut from the pocket first, and get walls of Wall thickness around them, the other way:
  out of their outer loops, and into their holes.

A failed offset fails the feature, showing the edges it was given. If the walls leave nothing of the pockets, that's
an error too, rather than a feature with no result.

With logging on (a variable `#logging` of `true`, see the style guide's Logging), it prints each loop's edges (each
line's length, and each arc's radius, angle, center, and whether it's convex or concave), each chain's wall, and
how many bands, corners, and pockets each step makes, and shows them as debug entities. A failed offset shows its
edges in red, and how far they do offset, found by trying smaller offsets.

### Ribs

Each rib is a band half its group's thickness (and the pockets' growth) either side of its edge (`opOffsetWire`),
extruded through the pockets. A circle or arc too small to offset toward its center (within std's boolean tolerance
of it) is a solid cylinder instead.

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

1. Its corners are filleted a hair.
2. If that fails, its narrow sides are found (`narrowSides`): each side between two convex corners has those two
   corners filleted alone, in a feature of its own, rolled back, and the sides where that fails are narrow. Narrow
   sides next to each other make one end. Each end gets a full round across it (`opFullRoundFillet`), which growing
   makes a round end as wide as the pocket, and the pocket's corners are filleted again. Nothing's measured: the
   fillet decides.
3. If a full round fails, or the fillet still does, it's a warning, with that operation's error display (failed
   fillets in pink, bad edges in red), and the pocket isn't grown back: it's cut smaller, by the fillet radius. The
   rest are rounded as usual.

Full rounds grow to half the end's width (under a hair) and the growth, which is what the bit's two rounds with that
width between them amount to.

#### Small pockets

Pockets too small to be worth cutting are left solid, with or without Fillet corners. This is a heuristic: a pocket
is small if its area, as cut, is less than a circle twice as wide as the bit or the wall thickness, whichever is wider
(`isSmallPocket`). With a 1/8 in. bit and 1/8 in. walls, that's a circle 1/4 in. across. Its area as cut is
estimated before it's grown back: its area, plus its outline's length times the radius, plus the bit's (Steiner's
formula: exact for convex pockets, and a little more for others).

The pockets' ends are the pocket extrude's caps (`qCapEntity`), tracked from after the walls' bands are made through
the cuts and rounding (`startTracking`). Tracking them from straight after the extrude takes in the extrude too,
whose sides are derived from its caps, and finds every face. Every other face is a side, and the
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
- Failures it can carry on from aren't errors: a pocket's full round or fillet is a warning. Each is reported where it comes up; only the first reported shows. A warning is reported
  before the failed operation's status is copied, so the warning shows, with the operation's error display.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| The pocket's extrude, `excludeRegions`, `buildWalls` and `offsetBand`, `buildRibGroup`, the cuts, the pockets' growth | Each operation | Fails the feature, with a message of ours and the operation's error display. |
| `roundPockets` (`try silent`) | Each pocket's corner fillet, and each full round | A warning: that pocket's left smaller. |
| `hairFillet` (`try silent`) | A fillet, and each of `narrowSides`' trials | Whether it works. |
| `pointsOf` (`try silent`) | Each quick rib point | Left out: it isn't there any more. |
| `edgeRibSegments` (`try silent`) | Where an edge rib meets its edge | The edge rib is skipped. |
| `addEdgeRibManipulators` (`try silent`) | Each edge rib's manipulator | No manipulator: its edge isn't there. |
| `robotLightenEditLogic`, `firstFaceUnder` (`try silent`) | Finding the face to fill in | Left as it is. |

## Method comparison

Vent (`vent.local.fs`) and Part Lighten (`partLighten.local.fs`) build walls the way the previous Onshape Lighten
feature did: extrude the face, hollow it (`opShell`), and keep what the hollow encloses (`opEnclose`). Robot lighten
offsets the face's edges in 2D instead (`opOffsetWire`, as std's thin extrudes do), which needs no hollowing, no
moving the ends out first, and no fillets for the walls' inside corners.

| | Robot lighten | Vent | Part Lighten |
| --- | --- | --- | --- |
| What's lightened | One face of one part | Selected faces on parts' faces (any number of parts) | One face of one or more parts |
| Ribs | Sketch edges of any shape, each offset both sides (`opOffsetWire`); quick ribs; overrides by group | None: separate faces leave a wall between them, half from each | Sketched rectangles (and arcs) around selected lines |
| Walls along the part's edges | Bands along the face's edges (`opOffsetWire`), cut from its extrude | Second extrude of the part's coplanar faces, hollowed by the full wall, intersected (`SUBTRACT_COMPLEMENT`) | Hollow of the face's extrude |
| Inside wall corners | Arcs, from the offset | Hair fillet, grown, then `opModifyFillet` to exactly the offset | As Vent |
| Rounded pocket corners | Hair fillet, grown by the radius less the hair: exactly the radius | Hair fillet, grown by the radius, then `opModifyFillet` to exactly it | As Vent, with the tool radius rounded up by 0.01 in |
| End types | Through all, Blind | Blind, Up to face, Up to part, Through all | Blind, Through, Up to face, Up to vertex |
| Extents | Exact (tight bounding box) | Exact (tight bounding box) | Measured distances; tool radius rounded |
| Narrow pocket ends | Full round, where a side's two corners can't be filleted | Not handled | Not handled; sharp edges where walls barely meet are found after the cut, and their faces deleted (`opDeleteFace`) |
| Small pockets | Left solid: less area than a circle twice the bit or wall | Not handled | Not handled |
| Tight rounded part corners | Not handled | Not handled | Not handled |
| Errors | Short messages, highlighting the parameters, with the operation's own display; warnings for pockets it can't fillet | Std errors; a warning if trimming fails | Generic messages |
| Feature patterns, sheet metal | No | Yes (`getRemainderPatternTransform`, `booleanBodies`) | No |

Taken from Vent: rounding the pockets' corners by a hair fillet, grown. Still worth taking: Up to face and Up to part
end types, and feature pattern support.

## Issues found

- Untested in Onshape.
- Small pockets are found by a heuristic (twice the bit or wall), which is untested on real parts.
- Walls follow the face's outline, so a part whose sides slope or step gets walls of the face's outline, not of its
  sides.
- Where an overridden wall meets another, a cylinder of the thicker one's width fills the corner, so the thinner wall
  thickens in an arc around it.
- A rib band along a short edge of a hole, or a split loop's chain along a small hole, is offset both ways: where it's
  wider than the hole's radius, the offset may fail.
- Untested in Onshape with `opOffsetWire`: the walls' and ribs' bands, and the corner cylinders.
- On one plate, offsetting the outer loop in fails once Wall thickness and Fillet radius together reach 1/4 in.,
  where a similar plate offsets past 1/2 in. It's an Onshape bug in `opOffsetWire`'s `makeRegions` on that face,
  reported to Onshape.
- A narrow side next to a concave corner (one side of it a rib's or wall's corner) has nothing to full round
  between, so its fillet fails, and the pocket's left smaller with a warning.
- A pocket's fillet warning is reported before the fillet's status is copied (for its error display), on the
  understanding that the first status reported is the one shown. Untested in Onshape: if the copied error shows
  instead, std's pattern uses an internal function (`@transferSubfeatureErrorDisplay`) to copy only the display.
- Trying each narrow side's corners alone costs a fillet per side of a pocket, but only for pockets whose fillet
  failed.
- Quick ribs are always at Rib thickness: overrides select sketch edges, and quick ribs' sketch is the feature's own.
- A hub's circle goes through its first spoke in the order selected.
- With Manipulator, an edge rib's points all meet its edge at one Location. Perpendicular to edge projects the edge's
  nearest point onto the face, which is only square for edges in (or parallel to) its plane.
