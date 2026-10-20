# Quick rib

Sketches ribs between points (circles' and arcs' centers, vertices, and mate connectors' origins), and from points to
edges, on Face to use, for Robot lighten's Ribs to use. Chains run through their points in order (closed or not; two
points make one rib); hubs have spokes to their hub, and a circle around it through the first spoke if asked; edge ribs
run from points to an edge, meeting it square, or where a manipulator puts them.

| File | What it is |
| --- | --- |
| `quickRib.fs` | The feature: its dialog, editing logic, and manipulators, and the ribs: finding them (`getQuickRibs`), sketching them (`sketchQuickRibs`), the edge ribs' manipulators, the toggle points for deleting ribs and deleting what makes them (`deleteRibs`), finding Face to use, and clearing invalid selections |
| `quickRibIcon.py`, `quickRibIcon.svg` | Its icon (`fs gen` writes the `.svg`): a hub and spokes, white circles joined by gray ribs (see `docs/icons.md`) |
| `../../../tests/featurescript/quickRib_test.fs` | Tests of the lines and circles it draws (`chainSegments`, `hubSegments`, `hubCircleRadius`), ribs' sources, repeats, and toggle points (`ribsOf`, `chainParts`, `hubParts`), deleting ribs' sources (`chainPieces`, `deleteRibSources`), what Quick add would add (`chainRibs`, `hubRibs`), and the Add ribs and Delete ribs buttons |

## Changelog

Unreleased. A feature of its own again, after a while folded into Robot lighten (whose Ribs to use takes its sketch's
edges); it keeps what changed there: Location's bounds are std's fillet's for points on edges (0.001 to 0.999), and
the skipped warning. One file again: the ribs moved into `quickRib.fs` from `quickRibs.fs`, whose tab is gone.

- Quick add: a chain or a hub and spokes (Type), with the same parameters as Chains' and Hub and spokes' items,
  becomes one with Add ribs; until then, its ribs are highlighted in blue. Clear invalid selections is at the bottom.
- Delete ribs: Start deleting ribs puts a toggle point on every rib, and Finish deleting ribs deletes those clicked
  (shown in red) by deleting what makes them: a chain is split (or opened) at its segment, a hub loses its spoke or
  Add circle, and an edge rib its point; a chain, hub, or edge rib left with no ribs is removed.
- No parameter descriptions (Face to use and Add circle had them).

## Strings

| String | Where |
| --- | --- |
| Quick rib | feature name |
| Quickly sketch ribs for use in part lightening. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

No parameter has a description. Two are hidden, set by editing logic and the toggle points: Deleting ribs
(`deletingRibs`, whether ribs are being chosen to delete; it shows Finish deleting ribs in place of Start deleting
ribs), and Ribs to delete (`ribsToDelete`, those chosen so far, by index into `getQuickRibs`'s ribs). Ribs to delete is
`isAnything`, so it's a number until set, which `ribIndices` reads as none. Quick add, Chains, Hub and spokes, Edge ribs, and
Delete ribs are each in a group of their own, with Clear invalid selections below them. Quick add is Type (Chain or
Hub and spokes), then a chain's Points and Closed, or a hub's Hub, Spokes, and Add circle, as Chains' and Hub and
spokes' items are, and Add ribs. Chains and Hub and spokes are
driven by their items' Points and Hub (selecting one adds an item), and focus moves into a new item's first selection.
Each edge rib is End (Perpendicular to edge or Manipulator), Edge, Points (any number), and with Manipulator,
Location.

### Errors, warnings, and info

| Message | Kind | When | Highlights | Closest std error |
| --- | --- | --- | --- | --- |
| Select a face to use. | error | no Face to use | `face` | Select faces to remove. |
| Add chains, hubs, or edge ribs to sketch ribs. | error | nothing makes a rib, nor would Quick add | `chains`, `hubs`, `edgeRibs` | Select face or sketch region to extrude. |
| Some quick ribs are skipped because they're incomplete. | warning | a chain with fewer than two points, a hub without a hub or spokes, or an edge rib without an edge or points | `chains`, `hubs`, `edgeRibs` | Some selected parts could not be deleted. |
| Click add ribs to add the highlighted ribs. | info | Quick add makes ribs (highlighted in blue), unless deleting ribs (reported after it) or the warning above (more severe) | | Select faces to remove. |
| Select ribs to delete. | info | while deleting ribs (the warning above shows instead, being more severe) | | Select faces to remove. |

## How it works

### Execution order

1. **Editing logic** (`quickRibEditLogic`): when Add ribs (a button: std's `isButton`, which editing logic gets as
   `clickedButton`) is clicked, and Quick add makes ribs (`quickAddRibs`), `addQuickRibs` adds its Points and Closed as
   a chain, or its Hub, Spokes, and Add circle as a hub and spokes, and clears those selections (the other type's are
   left). (A query can't hold the same thing twice, so a chain can't come back to its start except by Closed.) When
   Clear invalid selections is clicked, `clearInvalidSelections` evaluates Quick add's Points, Hub, and Spokes and each chain's, hub's, and edge rib's points (dropping
   what isn't there), and drops chains of fewer than two points, hubs without a hub, and edge ribs without an edge or
   points. Then unless Face to use has been set, and while it's empty, it's the face under what's chosen first
   (`firstFaceUnder`: Quick add's Points, Hub, and Spokes, the chains', the hubs' and spokes', then the edge ribs' points and
   edges). Under one (`faceUnder`): the planar face, of the parts which aren't hidden, in its plane (a sketch entity's
   sketch plane, a circle's or arc's plane, or a mate connector's XY plane) nearest its point; parts' vertices and
   lines give none. It only evaluates, and passes over what isn't there (`try silent`): editing logic mustn't throw. Start deleting ribs
   sets Deleting ribs, and clears Ribs to delete. Finish deleting ribs deletes what makes the ribs at Ribs to delete's indices
   (`deleteRibs`: their sources, from `getQuickRibs`, with each item's selections which have points, found as
   `getQuickRibs` finds them, so sources' indices are into them), and stops. If the ribs have changed so an index is
   past them, it deletes nothing (silently), and stops. `deleteRibSources` splits a chain at each
   deleted segment (`chainPieces`: a closed chain opens at its last deleted segment and runs on around from it; runs of
   one point are dropped), takes deleted spokes out of a hub's Spokes and turns off its Add circle if its circle's
   deleted, and takes deleted points out of an edge rib's Points. A chain, hub, or edge rib left with no ribs is removed
   (a hub with no spokes loses its circle too). What's chosen but isn't there is dropped from those it changes.
2. **Body**: Face to use's plane (an error without one), then the ribs (`getQuickRibs`), each with its sources, what makes it: a chain's segment, a hub's spoke or circle, or an
   edge rib's point, each by index:
   1. Each chain's points (`pointsOf`, `selectedPoints`), in order: a mate connector's origin, a vertex's point, or a circle's or arc's
      center (`pointOf`). What isn't there is left out (`try silent`). Chains with fewer than two points are skipped;
      the rest give lines through them (`chainSegments`, `chainParts`).
   2. Each hub's point and its spokes' (`hubSegments`, `hubParts`), and with Add circle, a circle around the hub through its first
      spoke (in the order chosen). Hubs without a hub or spokes are skipped.
   3. Each edge rib's lines (`edgeRibSegments`): from each of its points to its edge's nearest point (Perpendicular to
      edge), or all to the point at Location along it (Manipulator). Those without an edge or points are skipped.
   4. The same rib twice (lines with the same ends, either way round, or circles with the same center and radius,
      `sameRib`) is one, with each's sources, and lines with no length are dropped (`ribsOf`).
   5. They're sketched under `id + "ribSketch"`
      (`sketchQuickRibs`: points projected onto the plane; a circle's radius is how far its first spoke is from its hub
      in the plane, `hubCircleRadius`). While deleting ribs, each gets a toggle point (`addDeleteRibsManipulator`, as
      std's patterns' Skip instances do): a line's middle, or a circle's point across from its first spoke
      (`ribTogglePoint`), toggled if its index is in Ribs to delete, and those toggled are shown in red (debug entities: shown while the
      feature's edited).
   6. What Quick add would add from its selections as they are (`quickAddRibs`: a chain's, `chainRibs`, or a hub and
      spokes', `hubRibs`, as `addQuickRibs` makes them) is shown in blue, as std's `addDebugLine` shows lines (`showRibs`: sketched in a
      subfeature, shown as debug entities, and rolled back), with info saying to click Add ribs.

   Then a manipulator on the end of each Manipulator edge rib (`addEdgeRibManipulators`), along its edge, as std's
   fillet does its points on edges. With nothing to sketch (nor anything Quick add would add), an error; with Quick add's ribs, and while deleting ribs, info; with any skipped, a warning.
3. **Manipulator change** (`quickRibManipulatorChange`): clicking a rib's toggle point sets Ribs to delete to the indices
   of those toggled (`deleteRibsManipulatorChange`); dragging an edge rib's end sets its Location:
   the edge's nearest point to where it's dragged (`edgeRibManipulatorChange`; `edgeRibManipulatorIndex` reads which
   from the manipulator's key).

### Error handling

Incomplete chains, hubs, and edge ribs, and selections which aren't there, are skipped rather than errors (a warning
says so). The only errors are nothing to sketch and no Face to use.

| `try` | What it guards | When it fails |
| --- | --- | --- |
| `selectedPoints` (`try silent`) | Each selection's point | It's left out: what's chosen isn't there any more. |
| `edgeRibSegments` (`try silent`) | Finding where the ribs meet their edge | The edge rib is skipped. |
| `addEdgeRibManipulators` (`try silent`) | Each edge rib's manipulator | No manipulator for it: its edge isn't there. |
| `firstFaceUnder` (`try silent`) | Finding the face under each selection, in editing logic | It's passed over. |

## Issues found

- Untested in Onshape since it was split back out of Robot lighten.
- Robot lighten doesn't find Quick rib's sketch itself: it couldn't be done robustly (by the feature's id, with
  `qCreatedBy`), so its ribs are selected by hand in Ribs to use.
- Lines run center to center, through circles: fine for Robot lighten, which leaves holes' walls around them.
- A hub's circle goes through its first spoke in the order chosen: reordering spokes means choosing them again.
- Perpendicular to edge takes the edge's nearest point in 3D, then projects it onto the sketch: for edges in the
  sketch's plane (or parallel to it) that's square to the edge.
- With Manipulator, an edge rib's points all meet its edge at one Location.
- Deleting a hub's first spoke, with Add circle on and its circle kept, moves the circle through the next spoke: it
  changes size.
- Quick add's ribs are only shown (in blue) until Add ribs is clicked: they aren't in the sketch, so Robot lighten
  can't use them yet.
