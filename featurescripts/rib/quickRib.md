# Quick rib

Sketches ribs between points (circles' and arcs' centers, vertices, and mate connectors' origins), and from points to
edges, on Face to use, for Robot lighten's Ribs to use. Chains run through their points in order (closed or not; two
points make one rib); hubs have spokes to their hub, and a circle around it through the first spoke if asked; edge ribs
run from points to an edge, meeting it square, or where a manipulator puts them.

| File | What it is |
| --- | --- |
| `quickRib.fs` | The feature: its dialog, editing logic, and manipulators, and the ribs: finding them (`getQuickRibs`) and their keys (`lineKey`, `circleKey`), sketching them (`sketchQuickRibs`), the edge ribs' manipulators, the toggle points for deleting ribs, finding Face to use, and clearing invalid selections |
| `quickRibIcon.py`, `quickRibIcon.svg` | Its icon (`fs gen` writes the `.svg`): a hub and spokes, white circles joined by gray ribs (see `docs/icons.md`) |
| `../../tests/featurescript/quickRib_test.fs` | Tests of the lines and circles it draws (`chainSegments`, `hubSegments`, `uniqueSegments`, `hubCircleRadius`), ribs' keys and toggle points, and the Add ribs and Delete ribs buttons |

## Changelog

Unreleased. A feature of its own again, after a while folded into Robot lighten (whose Ribs to use takes its sketch's
edges); it keeps what changed there: Location's bounds are std's fillet's for points on edges (0.001 to 0.999), Add
circle's description, and the skipped warning. One file again: the ribs moved into `quickRib.fs` from
`quickRibs.fs`, whose tab is gone.

- Quick rib: its Selections, as a chain or a hub and spokes (Type), with Closed or Add circle, become one with Add
  ribs. Clear invalid selections is at the bottom.
- Delete ribs: Start deleting ribs puts a toggle point on every rib, and Finish deleting ribs deletes those clicked.

## Strings

| String | Where |
| --- | --- |
| Quick rib | feature name |
| Sketch ribs between circles and points, as chains or hubs and spokes, or to edges, for Robot lighten. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Face to use | Where the ribs are sketched (points are projected onto it). Filled in from what's chosen first. |
| Selections (Quick rib) | Points to add as a chain, in order, or as a hub (the first) and its spokes. |
| Add circle | Add a circle through the first selected spoke. (Quick rib's too.) |

Three are hidden, set by editing logic and the toggle points: Deleted ribs (`deletedRibs`, their keys), Deleting
ribs (`deletingRibs`, whether ribs are being chosen to delete; it shows Finish deleting ribs in place of Start deleting
ribs), and Ribs to delete (`ribsToDelete`, those chosen so far). Quick rib, Chains, Hub and spokes, Edge ribs, and
Delete ribs are each in a group of their own, with Clear invalid selections below them. Quick rib is Type (Chain or
Hub and spokes), Selections, Closed (Chain) or Add circle (Hub and spokes), and Add ribs. Chains and Hub and spokes are
driven by their items' Points and Hub (selecting one adds an item), and focus moves into a new item's first selection.
Each edge rib is End (Perpendicular to edge or Manipulator), Edge, Points (any number), and with Manipulator,
Location.

### Errors, warnings, and info

| Message | Kind | When | Highlights | Closest std error |
| --- | --- | --- | --- | --- |
| Select a face to use. | error | no Face to use | `face` | Select faces to remove. |
| Add chains, hubs, or edge ribs to sketch ribs. | error | nothing makes a rib | `chains`, `hubs`, `edgeRibs` | Select face or sketch region to extrude. |
| All ribs are deleted. | error | Deleted ribs has them all | `startDeleting` | All instances have been hidden. |
| Some quick ribs are skipped because they're incomplete. | warning | a chain with fewer than two points, a hub without a hub or spokes, or an edge rib without an edge or points | `chains`, `hubs`, `edgeRibs` | Some selected parts could not be deleted. |
| Click ribs to delete. | info | while deleting ribs (the warning above shows instead, being more severe) | | Select faces to remove. |

## How it works

### Execution order

1. **Editing logic** (`quickRibEditLogic`): when Add ribs (a button: std's `isButton`, which editing logic gets as
   `clickedButton`) is clicked, `addQuickRibs` adds Quick rib's Selections (evaluated, in the order chosen) as a chain,
   with its Closed, or a hub and spokes, the first the hub and the rest its spokes, with its Add circle, and clears
   them; fewer than two are left as they are. (A query can't hold the same thing twice, so a chain can't come back to
   its start except by Closed, and each Add ribs makes one chain or one hub.) When Clear invalid selections is clicked,
   `clearInvalidSelections` evaluates Quick rib's Selections and each chain's, hub's, and edge rib's points (dropping
   what isn't there), and drops chains of fewer than two points, hubs without a hub, and edge ribs without an edge or
   points. Then unless Face to use has been set, and while it's empty, it's the face under what's chosen first
   (`firstFaceUnder`: Quick rib's Selections, the chains', the hubs' and spokes', then the edge ribs' points and
   edges). Under one (`faceUnder`): the planar face, of the parts which aren't hidden, in its plane (a sketch entity's
   sketch plane, a circle's or arc's plane, or a mate connector's XY plane) nearest its point; parts' vertices and
   lines give none. It only evaluates, and passes over what isn't there (`try silent`): editing logic mustn't throw. Start deleting ribs
   sets Deleting ribs, and Ribs to delete to Deleted ribs (so ribs deleted before start toggled, and clicking them puts
   them back); Finish deleting ribs sets Deleted ribs to Ribs to delete, and stops.
2. **Body**: Face to use's plane (an error without one), then the ribs (`getQuickRibs`), each with its key: a line's
   ends, either way round, or a circle's center and radius, to a micrometer (`lineKey`, `circleKey`), which stay the
   same while its points don't move:
   1. Each chain's points (`pointsOf`), in order: a mate connector's origin, a vertex's point, or a circle's or arc's
      center (`pointOf`). What isn't there is left out (`try silent`). Chains with fewer than two points are skipped;
      the rest give lines through them (`chainSegments`).
   2. Each hub's point and its spokes' (`hubSegments`), and with Add circle, a circle around the hub through its first
      spoke (in the order chosen). Hubs without a hub or spokes are skipped.
   3. Each edge rib's lines (`edgeRibSegments`): from each of its points to its edge's nearest point (Perpendicular to
      edge), or all to the point at Location along it (Manipulator). Those without an edge or points are skipped.
   4. Repeats and lines with no length are dropped (`uniqueSegments`).
   5. Those not in Deleted ribs (every one, while deleting ribs) are sketched under `id + "ribSketch"`
      (`sketchQuickRibs`: points projected onto the plane; a circle's radius is how far its first spoke is from its hub
      in the plane, `hubCircleRadius`). While deleting ribs, each gets a toggle point (`addDeleteRibsManipulator`, as
      std's patterns' Skip instances do): a line's middle, or a circle's point across from its first spoke
      (`ribTogglePoint`), toggled if it's in Ribs to delete.

   Then a manipulator on the end of each Manipulator edge rib (`addEdgeRibManipulators`), along its edge, as std's
   fillet does its points on edges. With nothing to sketch, or every rib deleted, an error; while deleting ribs,
   info; with any skipped, a warning.
3. **Manipulator change** (`quickRibManipulatorChange`): clicking a rib's toggle point sets Ribs to delete to the keys of
   those toggled (`deleteRibsManipulatorChange`, finding the ribs again); dragging an edge rib's end sets its Location:
   the edge's nearest point to where it's dragged (`edgeRibManipulatorChange`; `edgeRibManipulatorIndex` reads which
   from the manipulator's key).

### Error handling

Incomplete chains, hubs, and edge ribs, and selections which aren't there, are skipped rather than errors (a warning
says so). The only errors are nothing to sketch, every rib deleted, and no Face to use.

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
- A deleted rib is remembered by where its ends are: moving a point it runs to brings it back (and leaves its old key
  in Deleted ribs, unused).
- Quick rib's Selections aren't sketched until Add ribs: they're shown only as selections.
