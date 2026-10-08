# Robot tensioner

Finds how far to move a pulley, sprocket, or idler along a direction, or how big to make an idler, for a belt (made by
Robot belt) or chain (made by Robot chain) to be just the length it should be, and shows its path then. It makes
nothing.

| File | What it is |
| --- | --- |
| `robotTensioner.fs` | The feature (in the backend document's `robotBeltTuner.fs` tab, which it was released as) |
| `../../core/loop.fs` | The attribute belts and chains have (`LoopAttribute`), their paths' length, and `nearestRoot` |

## Changelog

Since its last release, as Robot belt tuner:

- Renamed Robot tensioner, and it works with chains made by Robot chain. Its constant is still `robotBeltTuner`
  (released features' constants are never renamed), and its tab is still `robotBeltTuner.fs`.
- Its parameters are renamed: Robot belt is Belt or chain (`loop`), Curved belt face to adjust is Pulley, sprocket, or
  idler (`adjust`), which can be a mate connector of the belt's too, and Adjustment axis is Direction (`direction`),
  which takes anything with a direction.
- Belts made by an older Robot belt don't have what it reads: update them (edit them) first.
- Simple belts can be tensioned too.
- It finds the nearest fit exactly, either way along the direction, where it used to stop on a fit it couldn't reach
  by stepping, and a resized idler is given as its diameter.

## Strings

| String | Where |
| --- | --- |
| Robot tensioner | feature name |
| Find how far to move a pulley, sprocket, or idler, or how big to make an idler, for a belt or chain to fit exactly. `<br>`Works with belts and chains made by the Robot belt and Robot chain FeatureScripts. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Pulley, sprocket, or idler (`adjust`) | The one to adjust: the belt's or chain's curved face around it, or its mate connector. |
| Direction (`direction`) | The direction to move it in, like along a slot. |

No parameters are hidden.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select a belt or chain to tension. | error | no belt or chain | `loop` |
| Select a belt or chain made by Robot belt or Robot chain (one made by an older Robot belt needs updating first). | error | it has no `LOOP_ATTRIBUTE` | `loop`, it |
| A mirrored belt or chain can't be tensioned: tension the one it mirrors. | error | its attribute's coordinate system was mirrored away | `loop`, it |
| Select the pulley, sprocket, or idler to adjust: the belt's or chain's curved face around it, or its mate connector. | error | nothing to adjust | `adjust` |
| Select a face of the selected belt or chain. | error | the face is another part's | `adjust`, it |
| The selection isn't around one of the belt's or chain's pulleys, sprockets, or idlers. | error | its center isn't one of theirs (like a belt tooth's face) | `adjust`, it |
| Only an idler can be resized: select one, or move this instead. | error | Resize, on a pulley or sprocket | `adjust`, `adjustmentType`, it |
| Select a direction to move it in. | error | Move, with no direction | `direction` |
| Select an edge, axis, or face to move it along (or a face to move it out of). | error | no direction can be found from it | `direction`, it |
| The direction can't be across the belt or chain: it has to move in its plane. | error | the direction is the belt's plane's normal | `direction`, it |
| The `<name>` fits already, so this feature can be deleted. | info | its path is its length already | its path, in green |
| Moving it that way doesn't make the `<name>` fit: try another direction. | error | no fit along the direction | `direction`, it |
| No size of idler makes the `<name>` fit: try moving it instead. | error | no fit by resizing | `adjustmentType` |
| Move it `<distance>` as shown (`<along or against>` the direction) to fit the `<name>`. | info | Move found a fit | its path then, in blue, and an arrow |
| Make the idler `<diameter>` across to fit the `<name>`. | info | Resize found a fit | its path then, in blue |

`<name>` is the belt's or chain's, like "100T 5mm HTD belt" or "100 link #25 chain".

## How it works

### Execution order

1. **Precondition**: Adjustment type, Belt or chain, Pulley, sprocket, or idler, and (to move) Direction.
2. **Body**:
   1. `getLoop` reads the belt's or chain's `LoopAttribute`: its plane (a persistent coordinate system, so it follows
      the part when it's moved), its circles (each pulley's, sprocket's, or idler's, in its plane), and the length its
      path should be.
   2. `getAdjustIndex` finds the circle the selection's center (the face's axis, or the mate connector's origin) is
      at, and `getDirection` projects the direction into the plane.
   3. A move changes the circle's location by an offset along the direction, and a resize its radius. If the path is
      its length already (to the display's strict precision), it's shown in green and an info says so.
   4. `nearestRoot` finds the offset nearest zero at which the path (`tryLoopLength`) is its length: walking out each
      way, by 0.1 mm and then twice as far each time, until it's longer on one side and shorter on the other (or
      there's no path, where it stops looking that way), then bisecting.
   5. The path then is sketched, shown in blue, and deleted, with an arrow for a move, and the offset (or the idler's
      diameter) is reported as info.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `tryLoopLength` (`core/loop.fs`, `try silent`) | The path's length around circles | There's no path (like a circle inside another): `undefined`, which `nearestRoot` stops at. |
| `showLoop` (`try silent`) | Sketching the path to show it | A guard: it's only shown, so the result's still reported without it. |

## Issues found

- Untested in Onshape: nothing here has run yet.
- The icon is the generic robot icon.
