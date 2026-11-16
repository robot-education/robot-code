# Linear stock (`linearStock.fs`, `cutStock.fs`)

The shared modules behind Robot frame and Robot nut strip: what a length of stock is (`Stock`), how it's placed along
an edge, on a face, between holes, or extruded from a point (`placeStock`), the dialog for placing it (`stockLocationPredicate`,
`tieHolesPredicate`), and how it's built. See `docs/feature-writeups.md` for what writeups cover.

| File | What it is |
| --- | --- |
| `linearStock.fs` | What stock is, tying its holes, placing it, its properties, editing logic, and what building it uses (sketching its profile and holes, `stockFaces`, trimming) |
| `cutStock.fs` | Cut stock (`buildCutStock`): its profile extruded and its holes cut, as frames and nut strips are built |

The features using it ([Robot frame](../frame/robotFrame.md), [Robot nut strip](../nutStrip/robotNutStrip.md)) only
choose a part, describe it as a `Stock`, and call `placeStock` with it and its builder (`buildCutStock`); everything
else below is the same for both. `<name>` in strings is what's placed:
`frame` or `nut strip`.

## Strings

### Descriptions and hidden parameters

None of its parameters have descriptions. `index` (the nine point manipulator's point, `core/pointManipulator.fs`) is
hidden; the manipulator sets it. On a face, the three point manipulator sets Face at (`faceAt`), which is shown.

### Errors, warnings, and info

| Message | Kind | When | Highlights | Closest std error |
| --- | --- | --- | --- | --- |
| Select an edge to use. | error | Edge, nothing selected | `edge` | std's form ("Select ... to use.") |
| Select a face to use. | error | Face, nothing selected | `face` | std's form |
| Select a face with straight edges. | error | Face: it has no straight edges | `face`, the face | none |
| Select a start hole. / Select an end hole. | error | Holes, nothing selected | `startHole` / `endHole` | std's form |
| Select a hole's edge, a sketch point, or a mate connector to use. | error | Holes: a selection has no plane (the filter shouldn't allow it) | the hole's parameter, the selection | std's form |
| End hole must be parallel to start hole. | error | Holes: their axes aren't parallel | `startHole`, `endHole`, both | `UP_TO_FACE_NOT_PARALLEL` |
| Select an end hole apart from the start hole. | error | Holes: they're on one axis | `endHole`, the end hole | none |
| The `<name>` has no holes facing the selected holes, reorient secondary axis. | error | Holes: the walls facing the holes' axis have no holes (or only MAXSpline cutouts) | `secondaryAxisType` | none |
| Failed to trim to the selected part. | error | Trim: a part's boolean fails, or leaves no new face on the piece with the other end (it misses the stock, or cuts that end away) | `trimBodies`, the part | `FRAME_TRIM_FAILED` |
| Selected holes are not a multiple of `<pitch>` apart. | warning | Holes: their distance isn't a whole number of the aligned row's pitch, so the far hole misses | `startHole`, `endHole` | none |
| Select a sketch point, circle, or mate connector to use. | error | Point, nothing selected | `location` | std's form |
| Specified offsets are too long. | error | Edge: the offsets add up to the edge's length or more | `edgeStartOffset`, `edgeEndOffset` and their Opposite directions, the edge | none (std's wording) |
| The selected face does not intersect the `<name>`. | error | Trim: a face's plane misses the stock, or would cut all of it away | `trimFaces`, the face | `MOVE_CURVE_BOUNDARY_TRIM_NO_INTERSECTION` |
| The `<name>` has no length. | error | Point: the ends are at the same place, or the end is behind the start; Holes: the offsets add up to its length or more | Point: `depth`, `endBound`, `secondDirectionBound`; Holes: `startHole`, `endHole`, and the offsets | none |
| The `<name>`'s ends must be flat. | error | Point, Up to next: the next face is curved | the bound's parameters, the face | none |
| std's `EXTRUDE_FAILED` (Failed to extrude selections, check input.) | error | Point: up to next finds nothing in front of the point, or an up to face runs along the extrude | the bound's parameters (and face) | itself |
| std's `EXTRUDE_SELECT_TERMINATING_SURFACE`, `EXTRUDE_SELECT_TERMINATING_VERTEX` | error | Point, Up to face / vertex with nothing selected | the bound's parameters | themselves |
| std's `EXTRUDE_SELECT_DIRECTION`, `EXTRUDE_DIRECTION_INVALID_ENTITY` | error | Point, Direction on: no direction, or not a direction | `extrudeDirection` | themselves |
| The `<name>` exceeds the max length sold by the vendor (`<longest>`). | warning | the stock is longer than its longest `stock` length | `edge`, `face`, `startHole` and `endHole`, or `depth` (`selectionParameters`) | none |
| The `<name>`'s length should be a multiple of `<unit>`. / ... should be `<extra>` more than a multiple of `<unit>`. | info | stock with holes isn't a regular length (see Tying holes) | | none |

Lengths in messages and part names are `lengthString`: inches to 3 places for FRC (`6 in.`), millimeters to 1 place
for FTC (`136 mm`).

### Properties set

`setStockProperties` sets, on the stock's body:

| Property | Value |
| --- | --- |
| Name | `<length> <partName>`, e.g. `24 in. 2x1 Tube (WCP, 1/16 in. wall)` |
| Part number | the `partNumber` of the shortest `stock` length it fits in, if any |
| Description | that stock length's `url`, or the part's `url` if it's longer than all of them (if not empty) |
| Material | always Aluminum - 6061 (`ALUMINUM`) |
| Appearance | the part's `appearance` |

With Show tied holes, tied holes and the end face are colored `TIED_HOLE_COLOR` (Color tied holes) or shown as blue
debug entities; tying by Length also shows a blue rectangle across the stock where tied holes start (`showTieMark`).
Frames are also tagged for std's frame features (`core/frameTags.fs`): their swept faces, the faces their ends are cut
and trimmed to, and (once it's trimmed, on the body it ended up as) their profile.

## How it works

### Execution order

1. **Precondition**: the feature's parameters, then `stockLocationPredicate` (the Position group: Edge, Face, Holes, or
   Point, and their options, Point's extrude options being std's, from `core/stdExtrude.fs`; then, but for Point, the
   Trim ends group) and `tieHolesPredicate`.
2. **Editing logic** (the feature's, which calls `stockEditLogic`): nothing when the feature is created. On edge
   placement, turning an offset on (or changing the part, if the feature says so) sets it to the feature's default
   offset, unless it's been set. On face and hole placement, nothing. On point placement, `newExtrudeEditLogicAlong` (`core/stdExtrude.fs`) points the
   extrude at what it's up to and its second direction away from its first, as std's extrude does, along the axis
   from the selected point.
3. **Body**: the feature builds its `Stock` and calls `placeStock`:
   1. Where it goes: `edgePlacement`, `facePlacement`, `holePlacement`, or `extrudePlacement` (below) give the
      `location` of its start (a coordinate system with Z along it, oriented by `orientStock`), its `length`, and, from
      point placement, the `startPlane` and `endPlane` its ends are on if they're slanted. Face and hole placement also
      give the `normal` its top is toward, and have already put its top, middle, or bottom (between holes, its bottom)
      on the face or holes' plane (`selectionShift`); hole placement warns if the holes are off its hole pattern, and
      gives the `holeShift` its holes are moved by (`shiftedHoles`), so they stay on the holes whatever its offsets.
   2. Along an edge or from a point, the chosen nine point is moved onto the edge or point (`stockPointOffsets`). Then
      `stockSpan` stretches the length to reach the farthest corner of each slanted end, as an extrude up to a slanted
      face reaches it.
   3. With Trim ends, `trimCuts` turns the selected faces into cuts, and `partTrims` says which end each selected part
      trims (below).
   4. The feature's builder builds it (below). If it was trimmed, its length is measured from the body (`stockExtent`).
   5. `setStockProperties` names it and sets its properties.
   6. Along an edge or from a point, the nine point manipulator is added; on a face, the three point manipulator
      (`addSelectionAtManipulator`: its top, middle, and bottom along the normal, halfway along it). Then the flip
      manipulator (and, by `edgePlacement` and `holePlacement`, the offset manipulators; by `extrudePlacement`, std's
      depth and flip manipulators, from the point moved by the starting offset).
   7. Tied holes are shown, then a warning if it's longer than it's sold, or else info if it isn't a regular length.
4. **Manipulator change** (`stockManipulatorChange`): the nine point and flip manipulators set their parameters; on a
   face, the three point manipulator sets Face at (its points in `SELECTION_AT_POINTS`' order); along an edge or between
   holes, the offset manipulators set the offsets; from a point, std's extrude manipulators set depth
   and direction (`extrudeManipulatorChange`).

### Placing

Stock always runs the same way along its edge or extrude, so editing never turns it around. Flip `<name>` ends (and
its manipulator) draws it from the other end, so its holes start from that end; nothing else moves. Its frame is turned
180 degrees about its Y axis, which turns X around too, so everything across X is mirrored back (`acrossSign`): the
profile (`sketchProfile`), the offsets of holes through the walls facing Y (`stockFaces`), and the nine points
(`stockPointOffsets`).

`edgePlacement`: along the edge, from its start to its end as `evEdgeTangentLine` runs (`edgeCoordSystem`: Z along the
edge; X along its sketch's normal, or any perpendicular for edges not in a sketch), between its offsets. Each offset
moves its end in along the edge, or out past it with its Opposite direction (`edgeOffset`); dragging an offset's
manipulator past the edge's end sets that. Each offset
moves its end in from the end of the edge, or out past it when it's negative; its Opposite direction reverses it
(`edgeOffset`), and dragging an offset's manipulator past the end of the edge flips it.

`facePlacement`: the face is one of the stock's sides, as a rectangle drawn in a sketch where a tube goes is (a
sketch region is a face):

1. Its direction (`faceDirection`): its longest straight edge's, preferring edges across which the face is as wide as
   one of the stock's sides (measured with `evBox3d` square to each), so a short mitered face runs along its sides, not
   its miters.
2. Centered across the face, along its middle. Turned so its side as wide as the face is on it: X (up, as along a
   sketch's edge) out of the face, unless the face is only as wide as the stock's `width` (its X side), when X is across
   it. The rotate button turns it from there. A face as wide as neither side is allowed (a nut strip on the inside of
   a tube), and isn't reported.
3. Its ends are square, at the ends of the face's bounding box along it (its farthest points), and its holes are
   spaced from its start, as along an edge.
4. Face at puts its top (the side the face's normal points to), middle, or bottom on the face (`selectionShift`, by its
   thickness along the normal, `profileThickness`).

`holePlacement`: between two holes (circular edges, sketch points, or mate connectors), lined up with them:

1. Each hole's plane (`holePlane`, from `selectionPlane`): at its center, facing out of the part it's in, as the flat
   face around the hole's edge does (or a sketch's region, for a sketch circle); a sketch point's sketch's, and a mate
   connector's XY. Their axes must be parallel, and the stock runs from the start hole toward the end hole, in the start
   hole's plane.
2. Its X is along the holes' axis (the rotate button turns it), so the holes through its walls facing the axis (X's, or
   Y's) line up with them; it's an error if those walls have none.
3. The hole it lines up by (`alignedHole`): of those rows' holes, the one nearest the middle of its face, then the
   nearest its start (MAXSpline cutouts aren't holes). That hole is on the start hole (flipped, on the end hole): the
   stock's start is its `position` back from it, and it's moved across by its `offset`.
4. It's `2 * position` longer than the distance between the holes, as far past each as its first hole is from its
   start, so a whole number of that row's pitch apart, the far hole lines up too, and it's a regular length when the
   hole is at `tieStart`. Otherwise a warning says the holes aren't a multiple of the pitch apart.
5. Its bottom is on the start hole's plane, on the side the holes face.
6. Start offset and End offset (the edge's parameters and manipulators) move its ends in from there, or out past them,
   as along an edge. Its holes stay on the selected holes (`shiftedHoles`): with its start moved out, each row's holes go
   on into it, the first as near the start as it was; moved in, those still wholly on it stay. The holes counted for
   tying move with them.

`extrudePlacement` works out where an extrude from the selected point would go, without extruding, as std's extrude
decides it:

1. The axis: from the point, along its normal or the selected direction, which may lie in the point's plane (like a
   sketch line beside it), since the profile is sketched square to it (`extrudeDirectionPlane`). The stock runs along
   the extrude's direction, which Opposite direction flips.
2. The starting offset moves the point along the axis (blind, or to where the entity is, measured with `evDistance`).
3. The end (`boundAlong`): a blind length; the distance to a vertex or mate connector (square to the extrude); where an
   up to face's (or mate connector's) plane crosses the axis; or, for up to next, the nearest face a ray from the point
   hits (`evRaycast` against every modifiable solid), which must be planar. Offsets move it back toward the point,
   unless flipped.
4. The start: the point; half a symmetric length back; or the second end, found the same way, back from the point
   unless its direction is flipped to match the first's.
5. Ends on planes square to the stock are square; others are slanted (`slantedEnd`).

### Trimming

Trim ends is along an edge, on a face, or between holes (an extrude has its own bounds). Trimming only ever shortens
stock: it's placed as it would be, then cut. Holes are laid out on the untrimmed length, so tied holes count from the
untrimmed end.

`trimCuts`: each selected face (or mate connector) cuts the stock with its plane, keeping the side the face faces (out
of the part it's a face of, or along a mate connector's Z) and removing the rest. A face whose plane misses the stock,
or would cut all of it away, is an error.

`partTrims`: each selected part trims the end it's nearer (by the middle of where its box is alongside the stock), as
std's Frame's Parts to trim to does: the builder subtracts it (keeping the part), keeps the piece with the stock's other
end, and deletes the rest, so a part crossing near an end cuts it off there, and a tube's end against a round tube is
coped to it. Unlike std's, the stock isn't first extended to reach the part.

### Cut stock (`cutStock.fs`, `buildCutStock`)

Frames and nut strips are built this way:

1. The profile is sketched (`sketchProfile`: a rectangle, tube, angle, channel, bore, or a `profile` sketch) and
   extruded. Stock with holes is built as the shortest regular length at least as long (`regularLength`), so every
   hole is whole while it's cut.
2. Frames are tagged as frames.
3. Holes are cut. Each row's holes go at `holePositions`; booleans are slow, so each row has one seed (a tool for its
   first hole), seeds patterned the same way are grouped (`seedGroups`), and each group is cut with one boolean and
   face patterned along the stock (`cutHoles`). A pattern's instances are named (`holeInstances`): by each hole's count
   from the start, or for tied holes, from the end (`e0` is the last), so tied holes' identities count from the end,
   with no seed or pattern of their own. When every hole is tied, the seed is the last hole, patterned back toward the
   start.
4. It's cut to its ends and trims (`trimBeyond`: subtracting a block beyond each plane): its slanted ends, its end (if
   it was built longer for its holes), and each trim. Then it's trimmed to each part (`partTrims`, above). Frames' cut
   and trimmed faces are tagged as their caps, and then the body it ended up as with its profile (a boolean splitting a
   body keeps its attributes on only one of the pieces).
5. Tapped stock (nut strips) gets hole attributes and cosmetic threads on its remaining holes
   (`setTappedThroughHoles`, `core/tappedHole.fs`), after cutting so holes the cuts removed aren't threaded.

### Tying holes

Holes are counted from the stock's start, so things attached to the holes near the end would move to other holes as
the length changes. Tying them to the end (`getTie`, `firstTiedHole`) counts those holes from the end instead: the
half of the holes nearest the end (Half), those within Length from end, or Tied holes many. A part's holes that count
are `tieUnit` apart from `tieStart`; a length is regular when it's `2 * tieStart` more than a multiple of `tieUnit`,
so both ends have the same margin (`isRegularLength`).

### Error handling

Errors are thrown with `regenError`, naming the parameters to highlight. Selections are checked before they're used
(`verifyNonemptyQuery`, `isQueryEmpty`), and placements are checked before anything is built, so errors leave nothing
behind; only trimming to parts is checked as it's done (after the stock is built, which the error rolls back), since
whether a part trims it is the boolean's to say. Warnings and info don't stop the feature.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `stockEditLogic` (`try silent`) | Finding the extrude's axis in editing logic | No point or direction selected yet: no axis, so editing logic sets no flips. A guard, not a fallback. |
| `buildCutStock` (`try`) | Subtracting a part to trim to | Replaces the boolean's error with "Failed to trim to the selected part.", showing the part. Not a fallback. |

There are no fallbacks.

## Issues found

- Every stock gets Aluminum - 6061 as its material, including Last Anvil's 7075 tube.
- In Point placement, the extrude's opposite direction is labeled "Flip primary axis".
- "Sketch point to place `<name>`" also takes circles and mate connectors.
- Cutting the end to length (stock with holes is built longer) replaces the extrude's end cap with the cut's face, so
  references to the end face follow the cut, not the extrude.
- On a face as wide as neither of the stock's sides (a wrong size of frame, say), the stock is centered on it and
  nothing says so; a warning would also show for every nut strip placed on a tube's inside face.
- Holes are picked by their edges (or sketch points), not their cylindrical faces, which are easier to click: a
  cylinder doesn't say which of its ends is on the part's face the stock goes on.
- Between holes, a start offset that isn't a multiple of the hole spacing leaves the first hole nearer (or farther
  from) the start than the stock's usual margin, as cutting it from longer stock would, and its length then isn't
  regular (the info says so).
- Trimming to parts doesn't extend the stock to reach a part first, as std's Frame does.
