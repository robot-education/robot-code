# Quick rib

Sketches ribs between points (circles' and arcs' centers, vertices, and mate connectors' origins), and from points to
edges, on Face to use, for Robot lighten's Ribs to use. Chains run through their points in order (closed or not; two
points make one rib); hubs have spokes to their hub, and a circle around it through the first spoke if asked; edge ribs
run from points to an edge, meeting it square, or where a manipulator puts them.

| File | What it is |
| --- | --- |
| `quickRib.fs` | The feature: its dialog, editing logic, and manipulators |
| `quickRibs.fs` | The ribs: sketching them (`sketchQuickRibs`), the edge ribs' manipulators, finding Face to use, and clearing invalid selections |
| `quickRibIcon.py`, `quickRibIcon.svg` | Its icon (`fs gen` writes the `.svg`): a hub and spokes, white circles joined by gray ribs (see `docs/icons.md`) |
| `../../tests/featurescript/quickRibs_test.fs` | Tests of the lines and circles it draws (`chainSegments`, `hubSegments`, `uniqueSegments`, `hubCircleRadius`) |

## Changelog

Unreleased. A feature of its own again, after a while folded into Robot lighten (whose Ribs to use takes its sketch's
edges); it keeps what changed there: Location's bounds are std's fillet's for points on edges (0.001 to 0.999), Add
circle's description, and the skipped warning.

## Strings

| String | Where |
| --- | --- |
| Quick rib | feature name |
| Sketch ribs between circles and points, as chains or hubs and spokes, or to edges, for Robot lighten. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Face to use | Where the ribs are sketched (points are projected onto it). Filled in from what's chosen first. |
| Add circle | Add a circle through the first selected spoke. |

None are hidden. Chains, Hub and spokes, and Edge ribs are each in a group of their own. Chains and Hub and spokes are
driven by their items' Points and Hub (selecting one adds an item), and focus moves into a new item's first selection.
Each edge rib is End (Perpendicular to edge or Manipulator), Edge, Points (any number), and with Manipulator,
Location.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select a face to use. | error | no Face to use | `face` |
| Add chains, hubs, or edge ribs to sketch ribs. | error | nothing makes a rib | `chains`, `hubs`, `edgeRibs` |
| Some quick ribs are skipped because they're incomplete. | warning | a chain with fewer than two points, a hub without a hub or spokes, or an edge rib without an edge or points | `chains`, `hubs`, `edgeRibs` |

## How it works

### Execution order

1. **Editing logic** (`quickRibEditLogic`): when Clear invalid selections (a button: std's `isButton`, which editing
   logic gets as `clickedButton`) is clicked, `clearInvalidSelections` evaluates each chain's, hub's, and edge rib's
   points (dropping what isn't there), and drops chains of fewer than two points, hubs without a hub, and edge ribs
   without an edge or points. Then unless Face to use has been set, and while it's empty, it's the face under what's
   chosen first (`firstFaceUnder`: the chains', the hubs' and spokes', then the edge ribs' points and edges). Under one
   (`faceUnder`): the planar face, of the parts which aren't hidden, in its plane (a sketch entity's sketch plane, a
   circle's or arc's plane, or a mate connector's XY plane) nearest its point; parts' vertices and lines give none. It
   only evaluates, and passes over what isn't there (`try silent`): editing logic mustn't throw.
2. **Body**: Face to use's plane (an error without one), then `sketchQuickRibs` sketches on it, under
   `id + "ribSketch"`:
   1. Each chain's points (`pointsOf`), in order: a mate connector's origin, a vertex's point, or a circle's or arc's
      center (`pointOf`). What isn't there is left out (`try silent`). Chains with fewer than two points are skipped;
      the rest give lines through them (`chainSegments`).
   2. Each hub's point and its spokes' (`hubSegments`), and with Add circle, a circle around the hub through its first
      spoke (in the order chosen). Hubs without a hub or spokes are skipped.
   3. Each edge rib's lines (`edgeRibSegments`): from each of its points to its edge's nearest point (Perpendicular to
      edge), or all to the point at Location along it (Manipulator). Those without an edge or points are skipped.
   4. Repeats and lines with no length are dropped (`uniqueSegments`), and each line and circle is sketched (points
      projected onto the plane; a circle's radius is how far its first spoke is from its hub in the plane,
      `hubCircleRadius`).

   Then a manipulator on the end of each Manipulator edge rib (`addEdgeRibManipulators`), along its edge, as std's
   fillet does its points on edges. With nothing sketched, an error; with any skipped, a warning.
3. **Manipulator change** (`quickRibManipulatorChange`, `edgeRibManipulatorChange`): dragging an edge rib's end sets
   its Location: the edge's nearest point to where it's dragged (`edgeRibManipulatorIndex` reads which from the
   manipulator's key).

### Error handling

Incomplete chains, hubs, and edge ribs, and selections which aren't there, are skipped rather than errors (a warning
says so). The only errors are nothing to sketch, and no Face to use.

| `try` | What it guards | When it fails |
| --- | --- | --- |
| `pointsOf` (`try silent`) | Each selection's point | It's left out: what's chosen isn't there any more. |
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
