# Robot chain

Makes #25 or #35 roller chain around sprockets and idlers: its path goes around each sprocket's pitch circle (or an
idler, with the chain's rollers on it), with lines tangent between them, and the feature says whether the chain's
links fit that path, and how many would. Robot tensioner finds where to put a sprocket or idler for it to fit exactly.

| File | What it is |
| --- | --- |
| `robotChain.fs` | The feature: its dialog, the chain's dimensions, the sprockets, the chain's body, and its editing logic |
| `../core/loop.fs` | The path around circles, its length, and the attribute Robot tensioner reads (shared with Robot belt) |
| `../core/boundary.fs` | Lines tangent between circles, and arcs around them |
| `../core/startOffset.fs` | The starting offset |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot chain | feature name |
| Create #25 and #35 roller chain around sprockets and idlers, and check its length. `<br>`See also the Robot tensioner FeatureScript, which finds where to put a sprocket or idler for a chain to fit. `<CREDIT>` | feature description |
| `<links>` Link `<#25 or #35>` Chain | part name |

Its material is steel, and its appearance steel gray.

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Location (`location`) | A sketch point, mate connector, circle, or cylinder at its center: like a sprocket's bore, or its teeth's circle. |
| Pitch circle (`pitchCircle`) | The selected circle is the sprocket's pitch circle (its pins' circle), which gives its teeth. |
| Idler diameter (`idlerDiameter`) | The diameter the chain's rollers run on. |
| Chain side (`chainSide`) | Which side of the chain it's on: inside its loop, or outside it. |
| Select closest links (`selectClosestLinks`) | Sets Links to the even number of links nearest the length of the path around the sprockets. |
| Chain fit adjustment (`fitAdjustment`) | How much longer the path around the sprockets should be than the chain: positive for a looser chain. |

No parameters are hidden.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Add the sprockets and idlers the chain goes around. | error | no sprockets | `sprockets` |
| Add at least two sprockets. | error | one sprocket | `sprockets` |
| Select the sprocket's or idler's center. | error | a sprocket's location is empty | its `location` |
| Select a circle the size of the sprocket's pitch circle. | error | Pitch circle, but the location isn't a circle | its `location`, the selection |
| The selected pitch circle is too small for the chain. | error | the pitch circle's diameter is a pitch or less | its `location`, the selection |
| Select a sketch point, mate connector, circle, or cylinder. | error | a location isn't one (like a face which isn't a cylinder) | its `location`, the selection |
| A selected pitch circle isn't the size of a sprocket with a whole number of teeth: it's used as the nearest one. | warning | a pitch circle's teeth aren't a whole number | those `location`s |
| The `<links>` link chain fits the sprockets.`<odd>` | info | the path is the chain's length (to the display's precision) | |
| The `<links>` link chain is `<distance>` too `<long or short>` for the sprockets: `<closest>` links are closest. Get the distance close, then use Robot tensioner to make it exact.`<odd>` | warning | it isn't | `links` |

`<odd>` is " An odd number of links needs an offset link." for an odd number of links, or nothing.

## How it works

### Execution order

1. **Precondition**: Sprockets (each: Location, Type, then Pitch circle and Teeth, or Idler diameter, and Chain side),
   the starting offset, Chain type, Links, Select closest links, Add mate connectors, Chain fit adjustment.
2. **Editing logic** (`robotChainEditLogic`): Select closest links sets Links to `closestLinks` of the path's length
   (less the fit adjustment): the even number nearest it, as chain is joined with a master link.
3. **Body**:
   1. `getSprockets` finds each sprocket's plane (`getLocationPlane`: a circle's, a point's or mate connector's, or a
      cylinder's, at its middle) and teeth (entered, or from its pitch circle, inverting `getSprocketRadius`), and
      the chain's plane: the first sprocket's, with the starting offset. Each becomes a `BoundaryCircle` at its center
      projected onto the chain's plane: a sprocket's pitch circle (radius `pitch / (2 sin(180° / teeth))`), or an
      idler's radius plus the chain's roller's, flipped when the chain's outside it.
   2. `sketchLoop` (`core/loop.fs`) sketches the path, around the sprockets in the order they're listed.
   3. A flip manipulator on each arc swaps its sprocket's chain side; `validateLength` compares the chain's length
      (links times pitch, plus the fit adjustment) with the path's (`loopLength`).
   4. `createChain` thickens the path by the plates' height (half to each side) and extrudes it as wide as the chain,
      both ways from its plane: the chain's envelope, not its links.
   5. `LOOP_ATTRIBUTE` is set on it (its plane, circles, and length, for Robot tensioner), its properties, and a mate
      connector at each sprocket's center.
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
