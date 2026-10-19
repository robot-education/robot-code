# Robot lighten

Lightens a part with pockets. Everything within the extrude of the face to lighten (into its part, through it or to a
depth) is cut away, except walls along the face's edges (the part's sides and holes) and ribs. Ribs run along selected
sketch edges: a sketch's, or Quick rib's (`../rib/quickRib.fs`), which sketches them between points and to edges. The
pockets' corners are rounded as a router bit leaves them.

| File | What it is |
| --- | --- |
| `robotLighten.fs` | The feature |
| `robotLightenIcon.py` | Its icon (`fs gen` writes `robotLightenIcon.svg`): the plate icon, pocketed |
| `vent.local.fs` | Vent, for reference (see Method comparison): not synced |
| `partLighten.local.fs` | Part Lighten (by Evan Fish, FRC 2471), for reference: not synced |

## Changelog

Unreleased.

- Quick ribs are a feature of their own again (Quick rib): select its sketch's edges in Ribs to use.
- Holes get their walls separately from the face's outline, so a hole near the outline (like one in a rounded corner)
  no longer fails the walls, or silently leaves out pieces of pockets.
- When the walls can't be made, the error says the wall thickness (plus the fillet radius) is too large, and shows the
  face or the holes they fail at, instead of the pockets sticking out of the part.
- Every pocket is cut, however small: none are left solid.
- Exact pockets (under Options, on by default): off, walls and ribs are built 0.1 mm thicker, so walls which exactly
  meet at the sizes given (like round ones) don't fail; with Fillet corners, the pockets are grown back by as much, so
  their corners are rounded 0.1 mm more than Fillet radius. Errors which exact sizes can cause suggest it.
- Growing the pockets back failing is a warning again, rather than failing the feature.
- Ribs are optional: with none, there are just walls. Rib and wall overrides with nothing selected are left out,
  rather than errors.
- Works in feature mirrors and patterns (Mirror, or a pattern, of the feature onto another part).
- Add walls around ignored faces (under Ignore faces, on by default): off, sketch regions to ignore get no walls, and
  pockets go right up to them.
- Pieces the cut leaves loose are info ("Some parts are loose."), shown in blue, rather than a warning.
- Messages are modeled on std's (see the errors table's "Closest std error"). Ribs in sketches not parallel to the face
  are the same error as the face not being parallel to them.
- The walls' inside corners are rounded a hair (0.01 mm) more than Wall thickness, rather than set to it exactly.

## Strings

| String | Where |
| --- | --- |
| Robot lighten | feature name |
| Lighten a part with pockets, leaving walls around its edges and holes, and ribs along a sketch. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Exact pockets | If false, created pockets will be adjusted a slight amount in order to prevent issues with non-manifold geometry. |

None are hidden.

### Errors, warnings, and info

Errors show what's at fault in red, with the failed operation's own error display. Warnings and info are reported
where they come up, and show only once it's done; of several, the last of the most severe shows (see the style guide's
"An id has one status at a time").

| Message | Kind | When | Highlights | Closest std error |
| --- | --- | --- | --- | --- |
| Select a face to lighten. | error | no face to lighten | `face` | Select faces to mirror. |
| Select faces to ignore. | error | Ignore faces is on, with none selected | `ignoredFaces` | Select faces to remove. |
| Override faces must be adjacent to the face to lighten. | error | a wall override's faces share no edge with the face to lighten | its Faces, the faces | Reference face must be adjacent to ruled surface path. |
| Face to lighten must be parallel to the ribs. | error | a rib's sketch (as it'll be relative to the face, in a mirror or pattern) isn't parallel to the face | `face`, the rib parameters, the face and those ribs | Limit face must be parallel to face being moved. |
| Cannot find a part to lighten where the feature is mirrored or patterned. | error | mirrored or patterned onto another part, there's no part with a face where the face goes | `face`, the face | Cannot find a unique, planar wall to split. |
| Failed to extrude face to lighten, check input. | error | extruding the face fails | `face`, `depth`, the face | Failed to extrude selections, check input. |
| Failed to ignore selected regions, check input. | error | extruding or cutting the regions to ignore fails | `ignoredFaces`, the regions | Failed to extrude selections, check input. |
| Regions to ignore cover the whole face to lighten. | error | nothing's left of the pockets after cutting them (before the walls, or after rounding, without walls around them) | `ignoredFaces`, the face and the regions | The parts either do not intersect or are totally contained. |
| Could not offset override faces as requested. | error | moving an override's sides fails | that override's parameters and faces, and the offset's error display | Could not offset selected faces as requested. |
| Failed to create walls along highlighted faces. | error | moving the pockets' ends and sides along faces to ignore out fails | `wallThickness`, `filletRadius` (and `wallOverrides`), and those faces | Could not offset selected faces as requested. |
| Failed to create walls around highlighted holes. | error | splitting the holes off the pockets fails | the same, and the holes' faces and edges on the face (or the face, if they're gone) | Could not pattern selected parts. |
| Failed to fillet inside corners of walls. | error | the hair fillet on the pockets' concave edges and the holes' corners fails | the same, the edges, and the fillet's error display | Failed to fillet selections. |
| Wall thickness is too large, decrease the value. | error | hollowing the pockets (their outlines) fails; "Wall thickness plus fillet radius is too large, decrease them." with Fillet corners | the same, and the face | Selected offset is too large, decrease the value. |
| Wall thickness is too large for highlighted holes, decrease the value. | error | growing the holes fails; with Fillet corners, as above | the same, and the faces and edges on the face of the holes which fail to grow on their own (`holesFailingGrowth`) | Selected offset is too large, decrease the value. |
| Failed to create pocket inside highlighted walls. | error | enclosing a hollowed pocket fails | the same, and that pocket | Failed to create a part from enclosed region. |
| Walls around highlighted holes would make a non-manifold part. Try toggling the exact pockets option. | error | cutting the grown holes fails (`holesFailingCut` finds which) | the same and `exactPockets`, and those holes' faces and edges on the face, and the holes they meet | Boolean operation would result in non-manifold body. |
| Failed to cut walls around holes from pockets. | error | cutting the grown holes fails, and no hole's cut fails on its own or after another's | the same, and the face | Boolean operation failed to return a valid part. |
| Walls do not enclose a region for pockets, decrease the value. | error | hollowing encloses nothing: every pocket's narrower than twice the walls; "decrease them." with Fillet corners | the same, and the hollowed pockets | Selections do not enclose a region. |
| Failed to create a rib from highlighted edge. | error | a rib's extrude, thicken, or cylinder fails | that rib's group's parameters, and its edge | Failed to create a rib from a selected profile. |
| Failed to cut ribs from pockets. Try toggling the exact pockets option. | error | cutting them fails (as where a rib exactly meets a wall) | the rib parameters and `exactPockets`, and the ribs | Failed to merge ribs into parts. |
| Walls and ribs do not leave room for pockets. | error | the ribs leave nothing of the pockets | the wall, fillet, and rib parameters, and the face and ribs (what was cut is used up) | Selections do not enclose a region. |
| Failed to fillet highlighted pocket corner. | warning | a corner's fillet fails, and neither side next to it has another corner to round across to: it's left sharp | `filletRadius`, and the corner | Failed to fillet selections. |
| Failed to fillet highlighted pocket. | warning | a full round across a failed corner's narrow side fails: the corner's left sharp, and the fillet it deleted is put back | `filletRadius`, the corner and the side | Failed to fillet selections. |
| Could not offset pockets to fillet radius. Try toggling the exact pockets option. | warning | growing the rounded pockets back fails (as for a sliver left where walls and ribs exactly meet): they're cut smaller, by the fillet radius (the growth's error is copied for its display, then cleared, so it doesn't fail the feature) | `filletRadius` and `exactPockets`, the pockets, and the growth's error display | Could not offset selected faces as requested. |
| Failed to cut pockets from part. Try toggling the exact pockets option. | error | cutting them fails | `face` and `exactPockets`, and the pockets | Boolean operation failed to return a valid part. |
| Weight reduced by `<percent>`%. | info | the pockets are cut (shown if nothing was reported before it, or after it) | | none |
| Some parts are loose. | info | the cut leaves pieces free (ribs which touch no wall or other rib): reported after the weight's, so it shows instead | the loose parts, in blue (debug entities) | Hole operation split part into multiple parts. |

## UI

### Dialog

- **Selections** (at the top): Face to lighten, Ignore faces (with Faces to ignore, and Add walls around ignored
  faces), Ribs to use (optional), and Exclude construction lines.
- Wall thickness, Rib thickness, Fillet corners, and Fillet radius.
- **Override rib thickness**: an array of ribs at other thicknesses (labeled like "0.25 in ribs"). Later overrides
  win over earlier ones, and all of them over Rib thickness.
- **Override wall thickness**: an array of the part's faces whose walls are other thicknesses (labeled like
  "0.0625 in walls"). Later overrides win, and all of them win over Faces to ignore.
- End type (Through all or Blind), and Depth.
- **Options** (collapsed): Exact pockets.

### Editing logic

`robotLightenEditLogic` only evaluates; it never builds.

- **Face to lighten** is filled in until it's set by hand: the one part face in the ribs' sketch plane which they're
  over (`facesUnder`).

### Manipulators

`robotLightenManipulatorChange`: Depth (with Blind), an arrow on the face.

## Feature execution

```
robotLighten
--> getRemainderPatternTransform                   // in a mirror or pattern: where the face goes
--> getFace, getIgnoredFaces, getWallOverrides     // selections, checked before anything's built
--> getRibGroups --> ribGroups                     // ribs by thickness (maybe none); later overrides win
--> relativeTransform                              // how ribs and regions move relative to the face (in a pattern)
--> verifyParallel                                 // the ribs' sketches, parallel to the face
--> byFace --> opPattern                           // ribs and regions copied relative to the face, if they move
--> pocketDepth, addDepthManipulator
--> opExtrude "pocket"                             // the face, into its part
--> excludeRegions                                 // with walls around ignored faces: regions cut from the pocket
--> buildWalls                                     // the walls, in steps:
    --> opOffsetFace "extend"                      // 1. ends and ignored sides out
    --> opOffsetFace "override"                    // 2. each override's sides
    --> holeRings                                  // 3. the holes split off:
        --> opPattern, opDeleteFace, opBoolean     //    copied, filled in the pockets, the copies cut to the holes
    --> opFillet "roundConcave"                    // 4. concave edges and the holes' corners, a hair
    --> opShell, opEnclose                         // 5. the outlines hollowed by wall + growth; keep what's inside
    --> opOffsetFace "growHoles", opBoolean        // 6. the holes grown by wall + growth, and cut from it
        --> holesFailingGrowth, partFaces          //    if growing fails: the holes which fail on their own
        --> holesFailingCut, grownHolesOnPart      //    if cutting fails: the holes whose walls exactly meet others'
--> buildRibs --> buildRibGroup                    // with ribs: each a sheet thickened, or a cylinder
--> opBoolean "cutRibs"                            // ribs cut from the pockets
--> roundPockets                                   // with Fillet corners (and TIE_OFFSET), in steps:
    --> opFillet "fillet"                          // 1. each corner on its own, a hair, kept if it works
    --> narrowSide                                 // 2. each failed corner: the side to its nearest other corner
        --> opDeleteFace, opFullRoundFillet        //    that corner's fillet deleted, a full round across the side
    --> opOffsetFace "grow"                        // 3. the pockets, out by the radius less the hair
--> excludeRegions                                 // without walls around ignored faces: regions cut from pockets
--> patternedPart, opTransform "patterned"         // in a mirror or pattern onto another part: pockets moved there
--> opBoolean "cut"                                // pockets cut from the part
--> reportFeatureInfo                              // weight reduced, then loose parts (blue)
--> opDeleteBodies "cleanup"                       // pockets, ribs, and the sketches' copies
```

### Walls

The walls follow the previous Onshape Lighten feature, but for holes. The pocket (the face, extruded into its part),
with its holes filled, is hollowed by the wall thickness plus the pockets' growth (see Rounding), and what the hollow
encloses is the pocket inset from its outline. Each hole, grown by as much, is cut from that. What's at least the
inset from the outline, less what's within the inset of a hole, is what's at least the inset from both, so this is the
same as hollowing the pocket with its holes, as the previous feature did, where that works.

It often doesn't. Hollowing moves every face in at once, and where the offsets of a hole and the outline meet, the
shell can fail, or leave out pieces of pockets. On Exercise 3's Inner Plate (Robot Lighten Example 1), a 0.196 in hole
centered in a corner rounded 3/16 in, 0.09 in from the edge, failed walls from 0.4040 in (0.4039 worked) to at least
0.6 in; at 0.8 in hollowing worked, but left out 43% of what the pockets should have been, and on the plate beside it,
1%. Hollowing only the outline and cutting the grown holes matched the exact inset (computed with shapely from the
face's edges) on both plates, from 0.125 in to 1 in, and the examples in Robot Lighten.

The holes are the rings of the pockets' sides between their ends (`qFaceOrEdgeBoundedFaces`), but the outline, which
spans the most (`holeRings`). They're split off after the steps which move faces: the pockets are copied
(`opPattern`), their holes filled (`opDeleteFace`, which heals a hole closed), copied again, and the first copy cut from
the second, leaving the holes. So their ends are moved out with the pockets', and their sides along faces to ignore
and overrides move as the pockets' do: a hole along a face to ignore is grown back to where it was, and an
overridden one by its override's thickness.

Before hollowing:

- The pocket's ends, and its sides along faces to ignore, are moved out by the same amount, so hollowing brings them
  back to where they were.
- Each wall override's sides move out by Wall thickness less its own.
- Concave edges (of the outline, now that the holes are filled), and the holes' corners, get a hair fillet, which
  hollowing and growing grow into the walls' rounded inside corners: the inset and the hair, so once the pockets are
  grown back, a hair (0.01 mm) more than Wall thickness. Setting them to exactly the inset (`opModifyFillet`, as Vent
  does) was a step for that hair, and was dropped. (No offset can make both the walls and these corners exact: a
  corner's radius is the hair, the hollow, less the growth back, and the wall is the hollow less the growth, so the
  corner's always the wall and the hair.)

Hollowing the outline fails where the walls are too thick for it, and growing the holes where they're too thick for a
hole, so those errors say the wall thickness (plus the fillet radius, with Fillet corners: the walls are thicker by
it) is too large, and show where: the face, or the holes which fail to grow on their own (`holesFailingGrowth`, each
grown in a feature of its own which is always rolled back, only once growing them all has failed). Holes are shown on
the part: its faces along them, and their edges on the face (`partFaces`), since the pockets' ends were moved out, so
their own sides stick out of the part, and seen along the face's normal the faces are edge on. The shell's own error
display isn't copied: it shows the pockets, sticking out of the part.

Where the walls around a hole exactly meet another hole's (or the outline's), the pockets either side of where they
meet touch along an edge, and cutting the grown holes fails (`BOOLEAN_NON_MANIFOLD_RESULT`): on Exercise 3's Inner
Plate, its two 1 1/8 in bearing holes are 0.875 in apart, so 0.4375 in walls met exactly, before ties were broken
(see Ties): at the sizes given, they now meet a little past that, so this is for ties at other sizes. Its error says
so, showing
the holes which meet: `holesFailingCut` cuts them in turn, skipping those whose cut fails, and finds the hole each
which fails meets (the one whose cut, followed by its own, fails, or none, if it fails alone: it meets the outline's
walls), each try in a feature of its own which is always rolled back. The grown holes no longer have the part's
edges, so they're shown where they were (`grownHolesOnPart`): the face's edges the inset from their sides.

Splitting the holes off costs about 20 ms a feature on Robot Lighten's examples, and 40 ms on Exercise 3's plates (a
dozen holes): two copies, a fill, a cut, the growth, and the cut of the grown holes, most of them a few milliseconds
each. That's 5 to 10% of the feature; rounding the pockets and cutting the ribs cost more.

Every other step's failure fails the feature, with a message saying which step, and what it was given shown in red
(see the errors table). An override's offset failing is "Could not offset override faces as requested.", with its faces. A
pocket narrower than twice the walls encloses nothing; if none of them encloses anything, that's an error, rather
than a feature with no result.

Offsetting the face's edges in 2D (`opOffsetWire` with `makeRegions`, as std's thin extrudes do) was tried instead,
and dropped: it hit an Onshape bug on one plate's face, and it regenerated slower than hollowing on the plates tried.

With logging on (a variable `#logging` of `true`, see the style guide's Logging), it prints the wall thickness,
radius, and growth, how many holes are cut separately, and how many pockets each step leaves (inside the walls, and
between the walls and ribs), and shows them as debug entities. When growing the holes fails, it prints how many of
them fail on their own.

### Ties

At round sizes, walls often exactly meet: Exercise 3's two bearing holes 0.875 in apart, with 0.4375 in walls, or a
pocket exactly the bit's width (Robot Lighten Example 2, with 0.25 in walls, fillets, and ribs). Exactly met, the
pockets either side touch along an edge, which no cut can leave (`BOOLEAN_NON_MANIFOLD_RESULT`), or are left a sliver,
which growing them back fails on.

Exact pockets (under Options) is on by default, and builds everything exactly as given, ties and all. Off, walls and
ribs are built `TIE_OFFSET` (0.1 mm) thicker than they're to be, and where they'd meet exactly, they overlap a little
instead: nothing's detected or retried, it's the same for every part. With Fillet corners, the pockets are grown back
by as much, so walls and ribs are exactly as thick as asked, and the corners are rounded `TIE_OFFSET` more than Fillet
radius. Without Fillet corners, nothing's grown back, and walls and ribs are left 0.1 mm thicker: rounding the corners
just to grow them back costs as much as the rest of the feature (Exercise 3's Robot lighten 2: 310 ms, against
140 ms), and growing them back without rounding brings the tie back at the cut. It's more than std's boolean tolerance
(0.01 mm), which booleans treat as touching.

So the errors exact sizes can cause (cutting the grown holes, the ribs, or the pockets failing, and growing the
pockets back failing) end with "Try toggling the exact pockets option.": off, it breaks ties at round sizes; on, a
tie the offset happens to land on goes away. It says toggling, so it needn't know which it is.

Either way, one of the walls or the corners is off when ties are broken: a tie is broken only if the hollow differs
from the nominal one, and a pocket's corner is the hair and the growth back while its wall is the hollow less the
growth. Taking it back off after the cut (`opModifyFillet` on the corners, or `opOffsetFace` on the pockets' sides)
was tried as a step of Exact pockets, and dropped for the option deciding whether it's added at all.

### Regions to ignore

Sketch regions to ignore are left solid. With Add walls around ignored faces (the default), they're cut from the
pocket before the walls, so walls go around them as around holes. Without, they're cut from the pockets once the
pockets are rounded and grown back, so the pockets go right up to them, and aren't grown into them. Part faces to
ignore never get walls, either way.

### Mirrors and patterns

In a feature mirror or pattern (Mirror, or Linear or Circular pattern, of Robot lighten itself), Onshape runs the
feature again for each instance, with the instance's transform on a stack (std's `getRemainderPatternTransform`, as
Vent does). Where the face to lighten is patterned too (its part's in the pattern), its query finds the patterned
face, and nothing's moved. Where it isn't (the feature mirrored onto a part made separately, like a part mirror of
the plate), everything's built where the face is, and the finished pockets are moved by the transform
(`opTransform`) onto the part there (`patternedPart`: the one with a face in the face's moved plane, through the
middle of one of its edges), and cut from it; the weight's that part's. Ribs and regions to ignore move relative to
the face (`relativeTransform`: the face's transform, undone, then theirs): where they're patterned with the face, or
neither is, they stay as they are; otherwise their sketches are copied (`byFace`, `opPattern`) to where they'd be
relative to it, and the copies used, and deleted at the end. Their sketches' planes, checked against the face, are
moved likewise first. Std asks for pattern transforms before any operation, so all three are found first.

### Ribs

Ribs are optional: with none (none selected, or only construction ones, excluded), there are just walls. Rib and wall
overrides with nothing selected (or nothing left once later ones take theirs) are left out.

Each rib group's edges are extruded as sheets through the pockets, and each sheet thickened to half the group's
thickness (plus the pockets' growth) on each side. Each rib is made on its own: edges extruded together make one sheet,
creased where they meet, which can't be thickened. A circle or arc too small to thicken toward its center (within
std's boolean tolerance of it) is a solid cylinder instead.

### Rounding the pockets

With Fillet corners, the walls and ribs were built thicker by the fillet radius (and `TIE_OFFSET`, without Exact
pockets, see Ties), less a hair (std's boolean tolerance, 0.01 mm). Growing the pockets back out by that rounds their
corners as a router bit of that radius would, and a pocket narrower than
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

#### Every pocket is cut

Every pocket left between the walls and ribs is cut, however small. Leaving small ones solid was tried, by a heuristic
(less area, as cut, than a circle twice as wide as the bit, then the wall), and dropped: a thin sliver under a big bit
cuts a good-sized pocket, and with thick walls it left every pocket solid ("The pockets are all too small to cut." on
Exercise 3's Inner Plate with 0.6 in walls).

#### The pockets' ends

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

- Messages are modeled on std's (see the style guide's "Model errors on std's"): each in the form and words of the
  closest std error, which the errors table lists.
- Each step is an operation of its own, with a `try` only where its failure needs something said, or where it can
  carry on. Its `catch` copies the operation's status and error display onto the feature (`processSubfeatureStatus`
  with `propagateErrorDisplay`, on the feature's top level id), then throws a short message saying which step failed,
  highlighting the parameters which set it, and showing what it was given: most operations show nothing of their own
  (`opFillet` is the exception).
- Where a step can leave nothing (the regions to ignore, the walls, the ribs' cut), it's
  checked, and nothing left is an error there, showing what's still there to show: when the cut used up the pockets
  and ribs, the face and the rib edges.
- Failures it can carry on from aren't errors: anything in rounding (a corner left sharp, or the pockets left
  ungrown) is a warning, and loose parts are info. Each is reported where it comes up; the last
  of the most severe shows: loose parts' info after the weight's, so it shows instead. Where a failed operation's error display is wanted
  (growing the pockets back), its status is copied, then cleared keeping the display, then the warning's reported:
  a copied error left in place fails the feature.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| The pocket's extrude, `excludeRegions`, `buildWalls`, `buildRibGroup`, the cuts, the pockets' growth | Each operation | Fails the feature, with a message of ours and the operation's error display. |
| `roundPockets` (`try silent`) | Each corner's fillet | The corner's tried again in step 2. |
| `roundPockets` (`try silent`) | Each narrow side's fillet delete and full round | A warning, showing the corner and side; the feature they're in is rolled back. |
| `roundPockets` (`try silent`) | Growing the pockets back | A warning, showing the pockets, cut smaller. |
| `holesFailingGrowth` (`try silent`) | Growing each hole on its own, once growing them all has failed | That hole isn't why; the feature it's in is rolled back either way. |
| `holesFailingCut` (`try silent`) | Cutting each grown hole, in turn and after each other, once cutting them all has failed | That cut isn't why; the feature it's in is rolled back either way. |

## Method comparison

Robot lighten, Vent (`vent.local.fs`), and Part Lighten (`partLighten.local.fs`) all build walls the way the previous
Onshape Lighten feature did: extrude the face, hollow it (`opShell`), and keep what the hollow encloses (`opEnclose`).
Robot lighten hollows it with its holes filled, and cuts the holes, grown, from that (see Walls).

| | Robot lighten | Vent | Part Lighten |
| --- | --- | --- | --- |
| What's lightened | One face of one part | Selected faces on parts' faces (any number of parts) | One face of one or more parts |
| Ribs | Sketch edges of any shape (Quick rib's too), each thickened on its own; overrides by group | None: separate faces leave a wall between them, half from each | Sketched rectangles (and arcs) around selected lines |
| Walls along the part's edges | Hollow of the face's extrude with its holes filled, ends moved out first; the holes grown by the wall and cut from it | Second extrude of the part's coplanar faces, hollowed by the full wall, intersected (`SUBTRACT_COMPLEMENT`) | Hollow of the face's extrude |
| Inside wall corners | Hair fillet, grown: a hair more than the wall | Hair fillet, grown, then `opModifyFillet` to exactly the offset | As Vent |
| Rounded pocket corners | Hair fillet, grown by the radius less the hair: exactly the radius | Hair fillet, grown by the radius, then `opModifyFillet` to exactly it | As Vent, with the tool radius rounded up by 0.01 in |
| End types | Through all, Blind | Blind, Up to face, Up to part, Through all | Blind, Through, Up to face, Up to vertex |
| Extents | Exact (tight bounding box) | Exact (tight bounding box) | Measured distances; tool radius rounded |
| Narrow pocket ends | Full round, where a side's two corners can't be filleted | Not handled | Not handled; sharp edges where walls barely meet are found after the cut, and their faces deleted (`opDeleteFace`) |
| Tight rounded part corners | Not handled | Not handled | Not handled |
| Errors | Short messages modeled on std's, highlighting the parameters, with the operation's own display; warnings for pockets it can't fillet | Std errors; a warning if trimming fails | Generic messages |
| Feature mirrors and patterns | Yes (`getRemainderPatternTransform`, `opTransform`) | Yes (`getRemainderPatternTransform`) | No |
| Sheet metal | No | Yes (`booleanBodies`) | No |

Taken from Vent: rounding the pockets' corners by a hair fillet, grown, and feature pattern support. Still worth
taking: Up to face and Up to part end types.

## Issues found

- Untested in Onshape.
- Slivers between walls and ribs are cut too; their corners can be too sharp to round, which is a warning.
- Walls follow the face's outline, so a part whose sides slope or step gets walls of the face's outline, not of its
  sides.
- An overridden wall steps to its thickness at the corners its side meets others at. A much thicker override moves
  its side in before the hollow, which can close a small pocket.
- Hollowing the outline can still fail where its own offsets meet in a way the shell can't resolve (no example yet),
  and then the error shows the whole face. Rounded corners tighter than the walls hollow fine (Exercise 3's 1/4 in
  corners, with 0.8 in walls).
- Untested in Onshape: the errors when hollowing the outline or growing the holes fails, and holes along faces to
  ignore or overrides.
- With Exact pockets (the default), walls which exactly meet at round sizes fail the holes' cut (or the ribs' or
  pockets'), or leave slivers which fail to grow back; the messages suggest toggling it. Off, ties are broken at the
  sizes given, but walls can still exactly meet at other sizes, 0.1 mm off round ones.
- Without Exact pockets, corners are 0.1 mm rounder than Fillet radius, or without Fillet corners, walls and ribs are
  0.1 mm thicker than asked.
- Untested in Onshape: no ribs, feature mirrors and patterns (onto another part, with ribs or regions patterned or
  not), Add walls around ignored faces off, and the loose parts' info. Checked only by `fs check`, and by Onshape's
  examples still building (none of them use these).
- Without walls around ignored faces, pockets go right up to the regions, with sharp corners there.
- A narrow side next to a concave corner (one side of it a rib's or wall's corner) has no other corner to round
  across to, so its corner's left sharp with a warning.
- Filleting each corner on its own costs a fillet per corner, where one fillet of all of them did before.
- Untested in Onshape: deleting a hair fillet to heal it back to a corner (`opDeleteFace`), and rounding in a feature
  of its own inside the feature (`startFeature`, `endFeature`, `abortFeature`).
