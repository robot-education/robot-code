# Robot sprocket

Makes #25, #35, or 8mm (05B) sprockets: ISO 606 teeth, as wide as the chain's (0.93 of its inner width), at a sketch
point, or wherever a Robot chain goes around a sprocket, with a bore.

| File | What it is |
| --- | --- |
| `robotSprocket.fs` | The feature: its dialog, and the sprockets |
| `chainCommon.fs` | Chain sizes, the tooth form (`getSprocketToothForm`, `sketchSprocketProfile`), and the attributes |
| `../core/bore.fs` | The bore: its parameters, and cutting it |
| `../core/location.fs`, `../core/startOffset.fs`, `../core/profileOffset.fs` | The location, starting offset, and profile offset |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot sprocket | feature name |
| Create #25, #35, and 8mm sprockets. `<br>`See also the Robot chain FeatureScript, which works with this feature directly. `<CREDIT>` | feature description |
| `<teeth>`T `<#25, #35, or 8mm>` Sprocket | part name |

Its material is Onyx, and its appearance printed green (as Robot pulley's).

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Curved chain faces or mate connectors (`chainSelections`) | A Robot chain's faces around its sprockets, or its mate connectors: a sprocket is made for each. |
| Width (`hexWidth`) | Across the hex's flats. |
| Diameter (`boreDiameter`) | The diameter of the shaft it goes on. |
| Fit (`fit`), Clearance (`fitClearance`) | (`core/fit.fs`'s) |

No parameters are hidden.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select a sketch point, circle, or mate connector to use. | error | Manual, no location | `location` |
| Select a Robot chain's curved faces or mate connectors. | error | Chain, nothing selected | `chainSelections` |
| Select a curved face or mate connector of a chain made by Robot chain. | error | a selection without a `ChainFaceAttribute` | `chainSelections`, it |
| The selection is around one of the chain's idlers, not a sprocket. | error | an idler's face | `chainSelections`, it |
| The profile offset is too large for this chain. | error | the offset leaves no seating curve | `profileOffsetDistance` |
| A `<teeth>` tooth sprocket's teeth don't fit this chain. | error | the flanks can't reach a tip | `teeth` |
| Couldn't cut the bore. / Couldn't chamfer the bore's entrances. | error | (`core/bore.fs`) | the bore's parameters, and what failed |

## How it works

### Execution order

1. **Precondition**: Creation method; Manual: location, Chain type, Teeth, starting offset; Chain: the chain's faces
   or mate connectors; the bore (`borePredicate`); profile offset; Add mate connectors.
2. **Body**:
   1. The sprockets: Manual's one, at the location's plane with the starting offset; or Chain's, one per place the
      selected faces or mate connectors are around (`getChainSprockets`: a face's plane is its cylinder's, at its
      middle), with the chain's type and the sprocket's teeth from its `ChainFaceAttribute`.
   2. `createSprockets` sketches each one's profile (`sketchSprocketProfile`) and extrudes it the tooth width both ways
      from its plane, sets its `SprocketAttribute` (its chain type, teeth, and plane, which Robot chain reads) and
      properties.
   3. A profile offset manipulator, mate connectors (with the attribute too), and the bore (`cutBores`).
3. **Manipulator change** (`robotSprocketManipulatorChange`): the profile offset's and starting offset's.

The tooth form (ISO 606, with the most room for the chain): each gap's seating curve is an arc around its roller's
center on the pitch circle, of radius `0.505 d1 + 0.069 d1^(1/3)` (mm, `d1` the roller's diameter), `140° - 90°/z`
around; each flank's an arc tangent to it, of radius `0.008 d1 (z² + 180)`, out to a tip circle halfway between the
largest and smallest ISO 606 allows. A tooth whose flanks would cross below the tip circle is cut off where they're a
tenth of the tooth apart (found by bisection). The profile offset moves every curve outward (seating curves shrink,
flanks and tips grow).

## Issues found

- Untested in Onshape: nothing here has run yet.
- No hub: a sprocket is its toothed plate, as wide as its teeth.
- The icon is the generic robot icon.
