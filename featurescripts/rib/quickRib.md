# Quick rib

A prototype: sketches ribs between points (circles' and arcs' centers, vertices, and mate connectors' origins), and
from points to edges, on Face to use, for Robot lighten's Ribs to use. Chains run through their points in order (closed or not; two points make one rib); hubs have spokes to
their hub, and a circle around it if asked; edge ribs run from a point to an edge, meeting it square, or where a
manipulator puts it.

| File | What it is |
| --- | --- |
| `quickRib.fs` | The feature, its editing logic, and its manipulators |
| `../../tests/featurescript/quickRib_test.fs` | Tests of the lines it draws (`chainSegments`, `hubSegments`, `uniqueSegments`) |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Quick rib | feature name |
| Sketch ribs between circles and points, as chains or hubs and spokes, or to edges, for Robot lighten. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Face to use | Where the ribs are sketched (points are projected onto it). Filled in from what's chosen first. |
| Location | How far along the edge the rib meets it, from its start (0) to its end (1). |
| Clear invalid selections | Drops selections which no longer exist, chains of fewer than two points, hubs without a hub, and edge ribs without a point or edge. |

None are hidden. Chains, Hubs, and Edge ribs are each in a group of their own. Chains and Hubs are driven by their
items' Points and Hub (selecting one adds an item), and focus moves into a new item's first selection.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Add chains, hubs, or edge ribs to sketch ribs. | error | nothing makes a rib | `chains`, `hubs`, `edgeRibs` |
| A chain, hub, or edge rib isn't complete, so it's skipped. / `<n>` chains, hubs, and edge ribs aren't complete, so they're skipped. | warning | chains of fewer than two points, hubs without a hub (or without spokes or a circle), edge ribs without a point or edge | |
| Select a face to use. | error | no Face to use | `face` |

## How it works

### Execution order

1. **Editing logic** (`quickRibEditLogic`): when Clear invalid selections (a button: std's `isButton`, which editing
   logic gets as `clickedButton`) is clicked, `clearInvalidSelections` evaluates each chain's and hub's selections
   (dropping what isn't there), and drops chains of fewer than two points, hubs without a hub, and edge ribs without a
   point or edge. Then unless Face to use has been set, and while it's empty, it's the face under what's chosen first
   (`firstFaceUnder`: the chains', the hubs' and spokes', then the edge ribs' points and edges). Under one
   (`faceUnder`): the planar face, of the parts which aren't hidden, in its plane (a sketch entity's sketch plane, a
   circle's or arc's plane, or a mate connector's XY plane) nearest its point; parts' vertices and lines give none.
   It only evaluates, and passes over what isn't there (`try silent`): editing logic mustn't throw.
2. **Body**:
   1. Each chain's points (`pointsOf`), in order: a mate connector's origin, a vertex's point, or a circle's or arc's
      center (`pointOf`). What isn't there is left out (`try silent`). Chains with fewer than two points are skipped;
      the rest give lines through them (`chainSegments`).
   2. Each hub's point and its spokes' (`hubSegments`), and its circle with Add circle. Hubs without a hub, or with
      neither spokes nor a circle, are skipped.
   3. Each edge rib's line (`edgeRibSegment`): from its point to its edge's nearest point (Perpendicular to edge), or
      to the point at Location along it (Manipulator). Those without a point or edge are skipped.
   4. Repeats and lines with no length are dropped (`uniqueSegments`); with nothing left, an error, and with any
      skipped, a warning.
   5. Face to use's plane (`getSketchPlane`: an error without one), and a sketch on it under `id + "ribSketch"`, with
      each line and circle (points projected onto it).
   6. A manipulator on the end of each Manipulator edge rib (`addEdgeRibManipulators`), along its edge, as std's fillet
      does its points on edges.
3. **Manipulator change** (`quickRibManipulatorChange`): dragging an edge rib's end sets its Location: the edge's
   nearest point to where it's dragged (`edgeRibManipulatorIndex` reads which from the manipulator's key).

### Error handling

Incomplete chains, hubs, and edge ribs, and selections which aren't there, are skipped rather than errors (a warning
counts them). The only errors are nothing to sketch, and no Face to use.

| `try` | What it guards | When it fails |
| --- | --- | --- |
| `pointsOf` (`try silent`) | Each selection's point | It's left out: what's chosen isn't there any more. |
| `edgeRibSegment` (`try silent`) | Finding where the rib meets its edge | The edge rib is skipped. |
| `addEdgeRibManipulators` (`try silent`) | Each edge rib's manipulator | No manipulator for it: its edge isn't there. |
| `firstFaceUnder` (`try silent`) | Finding the face under each selection, in editing logic | It's passed over. |

## Issues found

- A prototype: untested in Onshape.
- Robot lighten doesn't select Quick rib's sketch itself: it couldn't be done robustly (by the feature's id, with
  `qCreatedBy`), so its ribs are selected by hand.
- Lines run center to center, through circles: fine for Robot lighten, which leaves holes' walls around them.
- A hub's spokes come in the order chosen; the first selection isn't treated specially yet.
- Perpendicular to edge takes the edge's nearest point in 3D, then projects it onto the sketch: for edges in the
  sketch's plane (or parallel to it) that's square to the edge.
- The icon is the generic robot icon.
