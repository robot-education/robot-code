# Robot chain

Makes #25, #25H, #35, or 8mm (05B, goBILDA's) roller chain around sprockets and idlers: its path goes around each
sprocket's pitch circle (or an idler, with the chain's rollers on it), with lines tangent between them. Simple chains go
around two sprockets, the second as far from the first as the chain's links make it; complex ones go around any
sprockets and idlers where they're selected, and the feature says whether the chain's links fit that path, and how
many would. Robot sprocket makes sprockets where it goes, and
Robot tensioner finds where to put a sprocket or idler for it to fit exactly.

| File | What it is |
| --- | --- |
| `robotChain.fs` | The feature: its dialog, the sprockets, the chain's body, and its editing logic |
| `chainCommon.fs` | Chain sizes, sprockets' geometry, and the attributes Robot chain and Robot sprocket read of each other's |
| `../core/loop.fs` | The path around circles, its length, how far apart two circles are for a path's length, the attribute Robot tensioner reads, and what a selected location is (shared with Robot belt) |
| `../core/boundary.fs` | Lines tangent between circles, and arcs around them |
| `../core/startOffset.fs` | The starting offset |
| `../core/location.fs` | A selection's plane: a sketch point's, mate connector's, circle's, or cylinder's (shared with Robot sprocket, belt, and pulley) |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot chain | feature name |
| Create #25, #25H, #35, and 8mm roller chain around sprockets and idlers, and check its length. `<br>`See also the Robot sprocket and Robot tensioner FeatureScripts, which work with this feature directly. `<CREDIT>` | feature description |
| `<links>` Link `<#25, #25H, #35, or 8mm>` Chain | part name |

Its material is steel, and its appearance steel gray.

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Location (`location`), Sprocket one location (`sprocketOneLocation`), Sprocket two location (`sprocketTwoLocation`) | Its center (a sketch point, mate connector, or a circle or cylinder around it, like a sprocket's bore), its pitch circle, or a Robot sprocket. |
| Idler diameter (`idlerDiameter`) | The diameter the chain's rollers run on. |
| Chain fit adjustment (`fitAdjustment`) | How much longer the path around the sprockets should be than the chain: positive for a looser chain. |

No parameters are hidden. Editing logic sets the shown Location types (see Execution order).

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Add the sprockets and idlers the chain goes around. | error | Complex, no sprockets | `sprockets` |
| Add at least two sprockets. | error | Complex, one sprocket | `sprockets` |
| Select the sprocket's or idler's center, pitch circle, or Robot sprocket. | error | Complex, a sprocket's location is empty | its `location` |
| Select sprocket one's center, pitch circle, or Robot sprocket. (or two's) | error | Simple, its location is empty | `sprocketOneLocation` (or two's) |
| The selected sprockets are on the same axis. | error | Simple, sprocket two's center projects onto sprocket one's | both locations, the selections |
| The `<links>` link chain is too short for its sprockets. | error | Simple, no distance apart makes the path that long | `links`, the sprockets' pitch circles where they're selected |
| Center to center adjustment is too large, decrease the value. | error | Simple, no path around the sprockets at their distance with the adjustment | `centerToCenterAdjustment`, the sprockets' pitch circles where they're selected |
| Select a sprocket made by Robot sprocket, or one of its mate connectors. | error | Robot sprocket, but the selection has no `SprocketAttribute` | its `location`, the selection |
| Select a circle the size of the sprocket's pitch circle. | error | Pitch circle, but the location isn't a circle | its `location`, the selection |
| The selected pitch circle is too small for the chain. | error | the pitch circle's diameter is a pitch or less | its `location`, the selection |
| Select a sketch point, mate connector, circle, or cylinder (or set the location type to Robot sprocket). | error | a center that isn't one | its `location`, the selection |
| Two of the pulleys, sprockets, or circles the path goes around are in the same place. | error | two sprockets' centers coincide (`core/boundary.fs`) | every sprocket's circle (`sketchCircles`) |
| The path can't go around a pulley, sprocket, or circle inside another. | error | one sprocket's circle is inside another's (`core/boundary.fs`) | every sprocket's circle (`sketchCircles`) |
| A selected pitch circle isn't the size of a sprocket with a whole number of teeth: it's used as the nearest one. | warning | a pitch circle's radius isn't the nearest whole number of teeth's, within std's length tolerance | those `location`s |
| A selected Robot sprocket is for another type of chain. | warning | its chain's sprockets aren't the chain's (`sameSprockets`: #25's and #25H's are the same) | those `location`s |
| A selected sprocket or idler isn't lined up with the first: the chain goes around where it is in the first one's plane. | warning | a sprocket's plane isn't the first's (or coplanar with it) | those `location`s |
| The `<links>` link chain fits the sprockets.`<odd>` | info | Complex: the path is the chain's length (to the display's precision); Simple: the sprockets are selected as far apart as the chain makes them (to the display's precision) | |
| The `<links>` link chain is `<distance>` too `<long or short>` for the sprockets: `<closest>` links are closest. Get the distance close, then use Robot tensioner to make it exact.`<odd>` | warning | Complex, it isn't | `links` |
| To use this chain, update the part studio so the distance between the sprockets is `<distance>``<odd>` | warning | Simple, the sprockets aren't selected that far apart: sprocket two is put that far from sprocket one, toward its selection | `sprocketTwoLocation` |

`<odd>` is " An odd number of links needs an offset link." for an odd number of links, or nothing.

`<odd>` on the Simple warning has no period before it, as Robot belt's same warning ends without one (to copy the
distance).

Each warning and the info are reported where they come up. Of several, the last of the most severe shows (see the
style guide's "An id has one status at a time"): the length's (or the distance's) warning, if it's off, over the
sprockets' warnings, and any warning over the info.

## How it works

### Execution order

1. **Precondition**: Chain mode; Simple: Sprocket one location, its Location type, and (for a center) Sprocket one
   teeth, and the same for sprocket two; Complex: Sprockets (each: Location, Location type, then (but for a Robot
   sprocket) Type, and Teeth or Idler diameter, and Chain side); the starting offset, Chain type, Links, Select closest
   links, Add mate connectors, and Center to center adjustment (Simple) or Chain fit adjustment (Complex).
2. **Editing logic** (`robotChainEditLogic`):
   1. For each sprocket (Simple's two, or Complex's items) whose location is new or changed (`selectionChanged`, or
      `areQueriesEquivalent` for Simple's), `locationKind` sets its Location type: Robot sprocket for a part or mate
      connector with a `SprocketAttribute`, Pitch circle for a circle the size of a sprocket with a whole number of
      teeth (`pitchCircleTeeth`, within 0.01), and Center for anything else.
   2. Select closest links sets Links to `closestLinks` of `selectedPathLength`: the path's length around the
      sprockets where they're selected, less the fit adjustment (Complex), or around Simple's two at their selected
      distance less the center to center adjustment. The even number nearest it, as chain is joined with a master
      link.
3. **Body**:
   1. `sprocketChoices` gathers what's chosen for each sprocket: Complex's items, or Simple's two (sprockets, inside
      the chain, with no identity, so changing what's selected doesn't change what the chain's faces are). Then
      `readSprockets` finds each one's plane and teeth: a Robot sprocket's from its attribute, a center's
      (`selectionPlane`: a circle's, a point's or mate connector's, or a cylinder's, at its middle) with its Teeth,
      or a pitch circle's, from its size, and its `ChainFaceAttribute` (its chain type, and teeth or idler radius).
      The chain's plane is the first's, with the starting offset. Each becomes a `BoundaryCircle` (`sprocketCircle`):
      a sprocket's pitch circle (radius `pitch / (2 sin(180° / teeth))`), or an idler's radius plus the chain's
      roller's, flipped when the chain's outside it.
      - Complex (`getSprockets`): each at its center projected onto the chain's plane. The path should be the chain's
        length (links times pitch) plus the fit adjustment.
      - Simple (`getSimpleSprockets`): sprocket two's center, projected onto the chain's plane, gives the direction
        from sprocket one. `twoCircleDistance` (`core/loop.fs`) finds the distance apart, nearest the selected one,
        at which the path around them is the chain's length (bisecting, by `nearestRoot`), plus the center to center
        adjustment. If the selected distance is that, to the display's precision, sprocket two's where it's selected;
        otherwise it's that far from sprocket one, toward its selection. The path should be its length at that
        distance (`tryTwoCircleLength`).

      Pitch circles of a fraction of teeth, Robot sprockets for another chain's sprockets, and sprockets not in the
      first's plane are reported as warnings (`reportSprocketWarnings`).
   2. `sketchLoop` (`core/loop.fs`) sketches the path, around the sprockets in the order they're listed.
   3. Complex: a flip manipulator on each arc swaps its sprocket's chain side; `reportLength` compares the chain's
      length (links times pitch, plus the fit adjustment) with the path's (`loopLength`), as info or a warning.
      Simple: `reportSimpleFit` reports whether the sprockets were selected as far apart as the chain makes them, as
      info, or a warning of the distance.
   4. `createChain` thickens the path by the plates' height (half to each side) and extrudes it as wide as the chain,
      both ways from its plane: the chain's envelope, not its links. Its curved faces around each sprocket (tracked
      from the path's arcs) get that sprocket's `ChainFaceAttribute`, for Robot sprocket.
   5. `LOOP_ATTRIBUTE` is set on it (its plane, circles, and the length its path should be, for Robot tensioner),
      its properties, and a mate connector at each sprocket's center (with its `ChainFaceAttribute` too).
4. **Manipulator change** (`robotChainManipulatorChange`): the flip manipulators (Complex's), and the starting
   offset's.

The path goes around pitch circles, as chain length formulas do: a chain on a sprocket is really a polygon of its
links, a little shorter than the arc it wraps (about 1% at 12 teeth, and less on bigger sprockets).

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `robotChainEditLogic` (`try silent`) | Finding the sprockets and the path's length (`selectedPathLength`) | A guard: with sprockets missing or wrong, Links is left as it is. |
| The body's `sketchLoop` (`try`) | Sketching the path around the sprockets | Its error again, showing every sprocket's circle, as there's no path to show. |
| `tryLoopLength` (`core/loop.fs`, `try silent`), through `twoCircleDistance` and `tryTwoCircleLength` | The path's length around two circles | There's no path (like a circle inside another): `undefined`, which `nearestRoot` stops at, and which Simple reports as an error. |

## Issues found

- Untested in Onshape: nothing here has run yet.
- The chain is modeled as its envelope, not its links.
- The icon is the generic robot icon.
