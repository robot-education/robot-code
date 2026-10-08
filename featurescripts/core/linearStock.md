# Linear stock (`linearStock.fs`)

The shared module behind Robot frame and Robot nut strip: what a length of stock is (`Stock`), how it's built
(`buildStock`), how it's placed along an edge or extruded from a point (`placeStock`), and the dialog for placing it
(`stockLocationPredicate`, `tieHolesPredicate`). See `docs/feature-writeups.md` for what writeups cover.

The features using it ([Robot frame](../frame/robotFrame.md), [Robot nut strip](../nutStrip/robotNutStrip.md)) only
choose a part, describe it as a `Stock`, and call `placeStock`; everything below is the same for both. `<name>` in
strings is what's placed: `frame` or `nut strip`.

## Strings

### Descriptions and hidden parameters

None of its parameters have descriptions. `index` (the nine point manipulator's point, `core/pointManipulator.fs`) is
hidden; the manipulator sets it.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select an edge to use. | error | Edge, nothing selected | `edge` |
| Select a sketch point, circle, or mate connector to use. | error | Point, nothing selected | `location` |
| Specified offsets are too long. | error | Edge: the offsets add up to the edge's length or more | `edgeStartOffset`, `edgeEndOffset`, the edge |
| The selected face does not intersect the `<name>`. | error | Edge, Trim: a face's plane misses the stock, or would cut all of it away | `trimFaces`, the face |
| The `<name>` has no length. | error | Point: the ends are at the same place, or the end is behind the start | `depth`, `endBound`, `secondDirectionBound` |
| The `<name>`'s ends must be flat. | error | Point, Up to next: the next face is curved | the bound's parameters, the face |
| std's `EXTRUDE_FAILED` (Failed to extrude selections, check input.) | error | Point: up to next finds nothing in front of the point, or an up to face runs along the extrude | the bound's parameters (and face) |
| std's `EXTRUDE_SELECT_TERMINATING_SURFACE`, `EXTRUDE_SELECT_TERMINATING_VERTEX` | error | Point, Up to face / vertex with nothing selected | the bound's parameters |
| std's `EXTRUDE_SELECT_DIRECTION`, `EXTRUDE_DIRECTION_INVALID_ENTITY` | error | Point, Direction on: no direction, or not a direction | `extrudeDirection` |
| The `<name>` exceeds the max length sold by the vendor (`<longest>`). | warning | the stock is longer than its longest `stock` length | `edge` or `depth` |
| The `<name>`'s length should be a multiple of `<unit>`. / ... should be `<extra>` more than a multiple of `<unit>`. | info | stock with holes isn't a regular length (see Tying holes) | |

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
Frames are also tagged for std's frame features (`core/frameTags.fs`): their profile and swept faces, and the faces
their ends are cut to.

## How it works

### Execution order

1. **Precondition**: the feature's parameters, then `stockLocationPredicate` (the Position group: Edge or Point, and
   their options, Point's extrude options being std's, from `core/stdExtrude.fs`; then, on an edge, the Trim ends
   group) and `tieHolesPredicate`.
2. **Editing logic** (the feature's, which calls `stockEditLogic`): nothing when the feature is created. On edge
   placement, turning an offset on (or changing the part, if the feature says so) sets it to the feature's default
   offset, unless it's been set. On point placement, `newExtrudeEditLogicAlong` (`core/stdExtrude.fs`) points the
   extrude at what it's up to and its second direction away from its first, as std's extrude does, along the axis
   from the selected point.
3. **Body**: the feature builds its `Stock` and calls `placeStock`:
   1. Where it goes: `edgePlacement` or `extrudePlacement` (below) give the `location` of its start (a coordinate
      system with Z along it, oriented by `orientStock`), its `length`, and, from point placement, the `startPlane` and
      `endPlane` its ends are on if they're slanted.
   2. The chosen nine point is moved onto the edge or point (`stockPointOffsets`), and `stockSpan` stretches the length
      to reach the farthest corner of each slanted end, as an extrude up to a slanted face reaches it.
   3. With Trim ends, `trimCuts` turns the selected faces into cuts (below).
   4. `buildStock` builds it (below). If it was trimmed, its length is measured from the body (`stockExtent`).
   5. `setStockProperties` names it and sets its properties.
   6. The nine point and flip manipulators are added (and, by `edgePlacement`, the offset manipulators; by
      `extrudePlacement`, std's depth and flip manipulators).
   7. Tied holes are shown, then a warning if it's longer than it's sold, or else info if it isn't a regular length.
4. **Manipulator change** (`stockManipulatorChange`): the nine point, flip, and offset manipulators set their
   parameters; std's extrude manipulators set depth and direction (`extrudeManipulatorChange`).

### Placing

Stock always runs the same way along its edge or extrude, so editing never turns it around. Flip `<name>` ends (and
its manipulator) draws it from the other end, so its holes start from that end; nothing else moves. Its frame is turned
180 degrees about its Y axis, which turns X around too, so everything across X is mirrored back (`acrossSign`): the
profile (`sketchProfile`), the offsets of holes through the walls facing Y (`stockFaces`), and the nine points
(`stockPointOffsets`).

`edgePlacement`: along the edge, from its start to its end as `evEdgeTangentLine` runs (`edgeCoordSystem`: Z along the
edge; X along its sketch's normal, or any perpendicular for edges not in a sketch), between its offsets.

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

`trimCuts`: each selected face (or mate connector) cuts the stock with its plane, keeping the side the face faces (out
of the part it's a face of, or along a mate connector's Z) and removing the rest. Trimming only ever shortens stock:
it's built along the edge between its offsets, then cut. A face whose plane misses the stock, or would cut all of it
away, is an error. Holes are laid out on the untrimmed length, so tied holes count from the untrimmed end.

### Building (`buildStock`)

1. The profile is sketched (`sketchProfile`: a rectangle, tube, angle, channel, bore, or a `profile` sketch) and
   extruded. Stock with holes is built as the shortest regular length at least as long (`regularLength`), so every
   hole is whole while it's cut.
2. Frames are tagged as frames.
3. Holes are cut. Each row's holes go at `holePositions`; booleans are slow, so each row has one seed (a tool for its
   first hole), seeds patterned the same way are grouped (`seedGroups`), and each group is cut with one boolean and
   face patterned along the stock (`cutHoles`). Tied holes get their own seed at the last of them, patterned back
   toward the start, so their identities count from the end.
4. It's cut to its ends and trims (`trimBeyond`: subtracting a block beyond each plane): its slanted ends, its end (if
   it was built longer for its holes), and each trim. Frames' cut faces are tagged as their caps.
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
behind. Warnings and info don't stop the feature.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `stockEditLogic` (`try silent`) | Finding the extrude's axis in editing logic | No point or direction selected yet: no axis, so editing logic sets no flips. A guard, not a fallback. |

There are no fallbacks.

## Issues found

- Every stock gets Aluminum - 6061 as its material, including Last Anvil's 7075 tube.
- In Point placement, the extrude's opposite direction is labeled "Flip primary axis".
- "Sketch point to place `<name>`" also takes circles and mate connectors.
- Cutting the end to length (stock with holes is built longer) replaces the extrude's end cap with the cut's face, so
  references to the end face follow the cut, not the extrude.
