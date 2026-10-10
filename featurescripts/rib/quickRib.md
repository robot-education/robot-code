# Quick rib

A prototype: sketches ribs between circles' centers, vertices, mate connectors' origins, and points on edges, for
Robot lighten, which finds the sketch itself (by its feature id, from the `_quickRibs` variable). Chains run through their points in order (closed or
not; two points make one rib); hubs have spokes to their hub, and a circle around it if asked.

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
| Sketch ribs between circles and points, as chains or hubs and spokes, for Robot lighten. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Face to use | Where the ribs are sketched (points are projected onto it). Without one, the first circle's plane. |
| Location | How far along the edge the rib meets it, from its start (0) to its end (1). |
| Clear invalid selections | Drops selections which no longer exist, chains of fewer than two points, hubs without a hub, and points on edges nothing uses. |

Points on edges' Edge is hidden: Points on edges is driven by it (selecting an edge adds an item), and editing logic adds
one for each edge the chains and hubs use.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Add chains or hubs to sketch ribs. | error | no chain or hub makes a rib | `chains`, `hubs` |
| A chain or hub isn't complete, so it's skipped. / `<n>` chains and hubs aren't complete, so they're skipped. | warning | chains of fewer than two points, hubs without a hub (or without spokes or a circle) | |
| Select a face to use: none of the ribs' circles give a plane. | error | no Face to use, and no full circle chosen | `face` |

### Variables

| Name | Set by | Read by |
| --- | --- | --- |
| `_quickRibs` | Quick rib: its id, added to those before it | Robot lighten's editing logic, which selects their sketches by id (`quickRibEdges`: `qCreatedBy(<id> + "ribSketch", EntityType.EDGE)`) |
| `_quickRibsUsed` | Robot lighten: the ids of the Quick ribs its ribs come from, added to those before it | Robot lighten's editing logic, to leave those out |

Their names start with an underscore, so they aren't shown in the Variable table.

## How it works

### Execution order

1. **Editing logic** (`quickRibEditLogic`): when Clear invalid selections (a button: std's `isButton`, which editing
   logic gets as `clickedButton`) is clicked, `clearInvalidSelections` evaluates each selection (dropping what isn't
   there), drops chains of fewer than two points and hubs without a hub, and drops points on edges nothing uses. Then each edge (but full circles) a chain or hub uses without a Points on
   edges item gets one, halfway along it (`usedEdges`, `edgePointIndex`).
2. **Body**:
   1. Each chain's points (`pointsOf`), in order: a mate connector's origin, a vertex's point, a full circle's center,
      or a point along another edge at its Points on edges location (`pointOf`, `edgeLocation`: its item is found by
      the edge, not its place in the chain, so it follows the edge). What isn't there is left out (`try silent`).
      Chains with fewer than two points are skipped; the rest give lines through them (`chainSegments`).
   2. Each hub's point and its spokes' (`hubSegments`), and its circle with Add circle. Hubs without a hub, or with
      neither spokes nor a circle, are skipped.
   3. Repeats and lines with no length are dropped (`uniqueSegments`); with nothing left, an error, and with groups
      skipped, a warning.
   4. The plane (`getSketchPlane`): Face to use's, or the first full circle's. A sketch on it with each line and
      circle (points projected onto it), under `id + "ribSketch"`; its id is added to `_quickRibs`, which Robot lighten finds
      it by.
   5. A manipulator on each point on an edge (`addEdgePointManipulators`), along it, as std's fillet does its points on
      edges.
3. **Manipulator change** (`quickRibManipulatorChange`): dragging a point on an edge sets its Location: the edge's
   nearest point to where it's dragged (`edgePointManipulatorIndex` reads which from the manipulator's key).

### Error handling

Incomplete chains and hubs, and selections which aren't there, are skipped rather than errors (a warning counts
them). The only errors are nothing to sketch, and no plane.

| `try` | What it guards | When it fails |
| --- | --- | --- |
| `pointsOf` (`try silent`) | Each selection's point | It's left out: what's chosen isn't there any more. |
| `addEdgePointManipulators` (`try silent`) | Each point on an edge's manipulator | No manipulator for it: its edge isn't there. |

## Issues found

- A prototype: untested in Onshape.
- Lines run center to center, through circles: fine for Robot lighten, which leaves holes' walls around them.
- A hub's spokes come in the order chosen; the first selection isn't treated specially yet.
- The icon is the generic robot icon.
