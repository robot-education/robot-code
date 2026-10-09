# Robot chain

Makes #25, #35, or 8mm (05B, goBILDA's) roller chain around sprockets and idlers: its path goes around each
sprocket's pitch circle (or an idler, with the chain's rollers on it), with lines tangent between them, and the feature
says whether the chain's links fit that path, and how many would. Robot sprocket makes sprockets where it goes, and
Robot tensioner finds where to put a sprocket or idler for it to fit exactly.

| File | What it is |
| --- | --- |
| `robotChain.fs` | The feature: its dialog, the sprockets, the chain's body, and its editing logic |
| `chainCommon.fs` | Chain sizes, sprockets' geometry, and the attributes Robot chain and Robot sprocket read of each other's |
| `../core/loop.fs` | The path around circles, its length, the attribute Robot tensioner reads, and what a selected location is (shared with Robot belt) |
| `../core/boundary.fs` | Lines tangent between circles, and arcs around them |
| `../core/startOffset.fs` | The starting offset |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot chain | feature name |
| Create #25, #35, and 8mm roller chain around sprockets and idlers, and check its length. `<br>`See also the Robot sprocket and Robot tensioner FeatureScripts, which work with this feature directly. `<CREDIT>` | feature description |
| `<links>` Link `<#25, #35, or 8mm>` Chain | part name |

Its material is steel, and its appearance steel gray.

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Location (`location`) | Its center (a sketch point, mate connector, or a circle or cylinder around it, like a sprocket's bore), its pitch circle, or a Robot sprocket. |
| Location type (`selectionType`) | What the location is. Set from what's selected: a circle the size of a pitch circle is one. |
| Idler diameter (`idlerDiameter`) | The diameter the chain's rollers run on. |
| Chain side (`chainSide`) | Which side of the chain it's on: inside its loop, or outside it. |
| Select closest links (`selectClosestLinks`) | Sets Links to the even number of links nearest the length of the path around the sprockets. |
| Chain fit adjustment (`fitAdjustment`) | How much longer the path around the sprockets should be than the chain: positive for a looser chain. |

No parameters are hidden. Editing logic sets the shown Location type (see Execution order).

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Add the sprockets and idlers the chain goes around. | error | no sprockets | `sprockets` |
| Add at least two sprockets. | error | one sprocket | `sprockets` |
| Select the sprocket's or idler's center, pitch circle, or Robot sprocket. | error | a sprocket's location is empty | its `location` |
| Select a sprocket made by Robot sprocket, or one of its mate connectors. | error | Robot sprocket, but the selection has no `SprocketAttribute` | its `location`, the selection |
| Select a circle the size of the sprocket's pitch circle. | error | Pitch circle, but the location isn't a circle | its `location`, the selection |
| The selected pitch circle is too small for the chain. | error | the pitch circle's diameter is a pitch or less | its `location`, the selection |
| Select a sketch point, mate connector, circle, or cylinder (or set the location type to Robot sprocket). | error | a center that isn't one | its `location`, the selection |
| A selected pitch circle isn't the size of a sprocket with a whole number of teeth: it's used as the nearest one. | warning | a pitch circle's teeth aren't a whole number | those `location`s |
| A selected Robot sprocket is for another type of chain. | warning | its chain type isn't the chain's | those `location`s |
| The `<links>` link chain fits the sprockets.`<odd>` | info | the path is the chain's length (to the display's precision) | |
| The `<links>` link chain is `<distance>` too `<long or short>` for the sprockets: `<closest>` links are closest. Get the distance close, then use Robot tensioner to make it exact.`<odd>` | warning | it isn't | `links` |

`<odd>` is " An odd number of links needs an offset link." for an odd number of links, or nothing.

## How it works

### Execution order

1. **Precondition**: Sprockets (each: Location, Location type, then (but for a Robot sprocket) Type, and Teeth or Idler
   diameter, and Chain side), the starting offset, Chain type, Links, Select closest links, Add mate connectors, Chain
   fit adjustment.
2. **Editing logic** (`robotChainEditLogic`):
   1. For each sprocket whose location is new or changed (`selectionChanged`), `locationKind` sets its Location type:
      Robot sprocket for a part or mate connector with a `SprocketAttribute`, Pitch circle for a circle the size of a
      sprocket with a whole number of teeth (`pitchCircleTeeth`, within 0.01), and Center for anything else.
   2. Select closest links sets Links to `closestLinks` of the path's length (less the fit adjustment): the even
      number nearest it, as chain is joined with a master link.
3. **Body**:
   1. `getSprockets` finds each sprocket's plane and teeth: a Robot sprocket's from its attribute, a center's
      (`getLocationPlane`: a circle's, a point's or mate connector's, or a cylinder's, at its middle) with its Teeth,
      or a pitch circle's, from its size. The chain's plane is the first's, with the starting offset. Each becomes a
      `BoundaryCircle` at its center projected onto the chain's plane: a sprocket's pitch circle (radius
      `pitch / (2 sin(180° / teeth))`), or an idler's radius plus the chain's roller's, flipped when the chain's
      outside it; and a `ChainFaceAttribute` (its chain type, and teeth or idler radius).
   2. `sketchLoop` (`core/loop.fs`) sketches the path, around the sprockets in the order they're listed.
   3. A flip manipulator on each arc swaps its sprocket's chain side; `validateLength` compares the chain's length
      (links times pitch, plus the fit adjustment) with the path's (`loopLength`).
   4. `createChain` thickens the path by the plates' height (half to each side) and extrudes it as wide as the chain,
      both ways from its plane: the chain's envelope, not its links. Its curved faces around each sprocket (tracked
      from the path's arcs) get that sprocket's `ChainFaceAttribute`, for Robot sprocket.
   5. `LOOP_ATTRIBUTE` is set on it (its plane, circles, and length, for Robot tensioner), its properties, and a mate
      connector at each sprocket's center (with its `ChainFaceAttribute` too).
4. **Manipulator change** (`robotChainManipulatorChange`): the flip manipulators, and the starting offset's.

The path goes around pitch circles, as chain length formulas do: a chain on a sprocket is really a polygon of its
links, a little shorter than the arc it wraps (about 1% at 12 teeth, and less on bigger sprockets).

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `robotChainEditLogic` (`try silent`) | Finding the sprockets and the path's length | A guard: with sprockets missing or wrong, Links is left as it is. |

## Issues found

- Untested in Onshape: nothing here has run yet.
- The chain is modeled as its envelope, not its links.
- The icon is the generic robot icon.
