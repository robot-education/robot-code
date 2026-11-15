# Robot shaft

Creates a shaft by extruding its profile from a point: a COTS shaft from vendors' tables, or a custom hex or spline
shaft. Hex and SplineXS shafts can have their ends modified (tapped or clearance holes, retaining ring or e-clip grooves,
captive shaft ends), and shafts can be mirrored across an end.

| File | What it is |
| --- | --- |
| `robotShaft.fs` | The feature: its dialog, the profile, the extrude, end modifications, properties, and editing logic |
| `robotShaftCommon.fs` | Shaft types and sizes (`ShaftType`, `HexType`, `HexSize`), and their dimensions |
| `robotShaftTables.py` | The COTS shaft tables and the tapped and clearance hole tables; `fs gen` writes `robotShaftTables.gen.fs` |
| `../splineProfile/splineProfileCommon.fs` | Spline types and profiles (shared with Robot spline profile) |
| `../../core/stdExtrude.fs` | The extrude's dialog and editing logic, based on std's |
| `robotShaftIcon.py` | Its icon (`fs gen` writes `robotShaftIcon.svg`): a long rounded hex shaft with a tapped end |
| `vendor/` | Vendors' drawings the tables were checked against |

## Changelog

Since v3.0.0 (`fs changes robotShaft`).

- The description is now "Create shafts used for both FRC and FTC." (was "Create the shafts FRC and FTC teams buy, or
  custom ones.").

## Strings

| String | Where |
| --- | --- |
| Robot shaft | feature name |
| Create shafts used for both FRC and FTC. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Mirror shaft (`mirrorShaft`) | Mirror the shaft across one of its ends. Useful when the shaft is modeled up to a plane of symmetry. |
| Side mount ring (`firstEndSideMount`, `secondEndSideMount`) | Whether to use a side-mountable retaining ring. |
| Groove diameter (`firstEndGrooveDiameter`, `secondEndGrooveDiameter`) | The diameter of the groove the retaining ring or e-clip sits in. |
| Extend shaft (`extendFirstEnd`, `extendSecondEnd`) | Whether to extend the shaft to accommodate hardware. |

Three parameters are hidden, set by editing logic from a COTS shaft's table entry (their "Set By" functions,
`isPresetShaft` and `isFactoryGrooved`), as a precondition can't read a lookup table's levels: Preset shaft
(`presetShaft`: it's at a length it's sold in, not Custom, so its length and ends are as
sold), Factory grooved (`factoryGrooved`: it has e-clip grooves, so Flip ends is shown), and Factory end
(`factoryEnd`, `isFactoryEnd`: it's at a Custom length, with factory features, so it has a factory end and a cut end,
and Flip ends is shown). Editing logic also sets
some shown ones: each end's tapped hole table (M4 for metric hex, #10-32 for the others, when the shaft changes to
the other system), its Hole diameter (from its hole table), and
its captive shaft Diameter and Length (from the shaft's size), and its Groove diameter (the ring's usual groove, when
Override groove diameter is turned on, or the hex size or Side mount ring changes with it on). A COTS shaft's profile (`shaftType`, `hexType`,
`hexSize`, `splineType`) is copied into the definition by editing logic though the parameters aren't shown, which
decides whether Shaft ends is.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Choose a different shaft. | error | a COTS shaft's saved path isn't a whole table entry (one saved before a level, like Length, was added) | `frcShaft` or `ftcShaft` |
| Select a sketch point, circle, or mate connector to use. | error | no location | `location` |
| The shaft's ends must be flat. | error | an extruded end isn't planar (up to next onto a curved face) | `endBound`, the end |
| The shaft end to mirror across must be planar. | error | Mirror on, and the mirrored end isn't planar | `flipMirrorEnd`, the end |
| Modified shaft ends must be perpendicular to the direction of the shaft. | error | a modified end is slanted (up to a slanted face) | the end |
| Failed to extend the shaft's end. | error | extending an end for a retaining ring, e-clip, or captive shaft fails | a captive end's Length and Extend shaft, or a retaining ring's or e-clip's end operation; the end |
| The captive shaft diameter is greater than the diameter of the shaft. | error | captive shaft Diameter is wider than the hex | the end's Diameter, a body showing the captive end |
| Failed to cut the captive end. | error | cutting a captive end fails | the end's Diameter and Length, the tool |
| Failed to cut the shaft's e-clip groove. | error | cutting a REX shaft's factory groove fails | `ftcShaft`; the shaft in blue, and the groove |
| Failed to cut the retaining ring's groove. | error | cutting a groove fails | the end's Side mount ring (and Groove diameter, if overridden), the tool |
| Failed to cut the e-clip's groove. | error | cutting a metric hex shaft's groove fails | the end's End operation (and Groove diameter, if overridden), the tool |
| The groove diameter is larger than the shaft. | error | an overridden Groove diameter is as wide as the shaft across its corners, or wider | the end's Groove diameter; the shaft in blue, and the groove as entered in red |
| This shaft is only sold up to `<longest>` long. | warning | COTS, longer than its longest stock | |
| std's hole errors | error | a tapped or clearance hole fails (std's `hole`, as a subfeature) | (std's) |
| std's extrude errors | error | the extrude fails (std's `extrude`, as a subfeature) | (std's) |

When an end modification fails, the shaft is shown in blue (`addDebugEntities`) before the error is thrown.

### Properties set

| Property | COTS | Custom |
| --- | --- | --- |
| Name | `<length> <partName>`, e.g. `3 in. Rounded Hex Shaft (WCP 1/2 in.)`; length from `shaftLengthString` (inches to 3 places, or mm to 1) | `<length> <type> Shaft`, e.g. `3 in. Hex Shaft`; length from `shaftLengthString`, as a COTS shaft's |
| Material | the entry's | Aluminum - 6061 |
| Appearance | the entry's | black, or medium gray for UltraHex, MAXSpline, and SplineXS |
| Vendor | the entry's | (none) |
| Part number, description (link) | the stock it's cut from, if it fits one | (none) |
| `robotShaftAttribute` | shaft type, hex type, hex size, spline type, on shafts whose ends can be modified | same |

Holes get std's hole attributes (they're made by std's `hole`).

## How it works

### Execution order

1. **Precondition**: Program, Source, location; the COTS table or the custom Shaft group; the Extrude group (std's
   extrude options, with `StockBoundingType` end types, and Mirror shaft; only Opposite direction and Starting offset
   for a preset length, `isPresetLength`; no Mirror shaft for a factory end, `hasFactoryEnd`); Shaft ends, for hex and
   SplineXS shafts not at a preset length (only the second end for a factory end, `cannotSpecifySecondEnd`), with
   metric hex's end operations (`isMetricHexShaft`) or the others.
   COTS shafts show Shaft ends by their profile, which editing logic copies into the definition (below).
2. **Editing logic** (`robotShaftEditLogic`):
   1. Sets Preset shaft, Factory grooved, and Factory end from a COTS shaft's table entry (`isPresetShaft`,
      `isFactoryGrooved`, `isFactoryEnd`).
      `withShaft`: sets the unit system from the program, and copies a COTS shaft's profile (`shaftType`, `hexType`,
      `hexSize`, `splineType`, `predrilledHoleDiameter`) into the definition, so the precondition shows what suits it.
      A preset length sets the extrude: Blind, its length, one way. A shaft with factory features isn't mirrored.
   2. When the feature is created: sketches the profile and runs std's extrude editing logic on it
      (`stdNewExtrudeEditLogic`), which points the extrude at what it's up to and sets the merge scope.
   3. For each end: if the shaft type or hex size changed (or the feature is new), and its tapped hole table's size is
      the other system's (metric or inch), sets it to the shaft's usual: M4 coarse for metric hex, #10-32 for the
      others (`updateTappedHolePath`). Then if its hole table path changed (or the feature is new), sets its Hole diameter from
      the table (`applyTableDefinition`: a tapped hole's tap drill, or a clearance hole's diameter for its fit); if the shaft type or hex size changed (or the
      feature is new), sets its captive shaft Diameter and Length (`updateCaptiveShaftParameters`).
   4. Removes `withShaft`'s `unitSystem` and `predrilledHoleDiameter`, which the body uses but aren't parameters:
      Onshape rejects an editing logic result with keys the precondition doesn't declare ("Editing logic result:
      Unknown parameter unitSystem"), and keeps none of it. `fs_eval_features_test.py` checks the result's keys.
3. **Body**:
   1. `withShaft` again, then the profile is sketched at the location (`createShaftProfile`: a hexagon, or a spline's
      outside profile, plus its inside profile for tube splines or a predrilled hole for a solid one).
   2. Std's `extrude` runs as a subfeature on it, at the top level id, so its manipulators and merge scope work.
   3. `verifyFlatEnds` checks both caps are planar.
   4. With Mirror shaft, the mirror plane is taken from the chosen end (`getMirrorPlane`), before the ends change.
   5. At a preset length, or with a factory end: its factory features are cut (`cutFactoryFeatures`: its e-clip
      grooves from its first end, or its second with Flip ends, then std's tapped hole in each end, or in the factory
      end alone, if it has them). Then, if it's not at a preset length, for hex and SplineXS shafts: the ends to modify
      are found (`getShaftEndDefinitions`; with a factory end, only its cut end, by the second end's parameters), hex profile features are
      cut (`cutHexShaftFeatures`), each end is checked square (`verifyPerpendicularShaftEnd`) and modified
      (`modifyShaftEnd`), and the shaft attribute is set.
   6. The shaft is mirrored, if mirroring (`mirrorShaftAcrossEnd`), and its length measured along its axis
      (`measureShaftLength`).
   7. Properties: `setCotsShaftProperties` (with its warnings), or `setShaftProperties` and `setShaftName`.
   8. The profile sketch is deleted.
4. **Manipulator change**: std's `extrudeManipulatorChange` (depth and direction).

### Functions

| Function | What it does |
| --- | --- |
| `getCotsShaft` | The chosen COTS entry, or `undefined` for a custom shaft. |
| `withShaft` | Above. |
| `createShaftProfile` | Above. |
| `getMirrorPlane`, `mirrorShaftAcrossEnd` | The end's plane; pattern the shaft across it and union the two. |
| `cutHexShaftFeatures` | Cuts what makes a hex shaft not stock hex, through its whole length: a predrilled hole (`hasPredrilledHole`, `getPredrilledHoleDiameter`, made slightly smaller than a tap drill the same size by `predrilledHoleRadius`), churro or Hex Lite flutes, UltraHex's inner hex, or a rounded hex's round (`getRoundedDiameter`). |
| `splineTapDrills` | The tap drills of a SplineXS shaft's tapped ends, from the definition, for its predrilled hole. |
| `getShaftEndDefinitions`, `getEndDefinition` | Which ends to modify, and how (`EndDefinition`): a factory end shaft's cut end (by the second end's parameters), the first end alone (clearance holes go through, so there's no second), both alike (Symmetric ends), each its own, or only the end not mirrored. A hole's diameter is the end's Hole diameter (from its hole table, or overridden). |
| `modifyShaftEnd` | A hole: std's `hole` as a subfeature, tapped (blind) or clearance (through), at the end's center, with the definition's diameter and depth, in std's ANSI table, or its ISO table for a metric size. A groove or captive end: `extendShaftEnd` (offsets the end face out by the hardware's length, if extending), then cuts a tool (`extrudeShaftGrooveTool`, `extrudeCaptiveShaftTool`) from the shaft (`cutShaft`). |
| `cutFactoryFeatures` | A shaft's features as it's sold, from its table entry (`factoryGrooves`, `factoryHole`): grooves cut from its first end (or second, with Flip ends, `canFlipEnds`), and a tapped hole in each end (or in its factory end alone, `hasFactoryEnd`), from std's ISO or ANSI hole table. |
| `getGrooveDefinition` | A groove's diameter and width, and how far from the end it is: a retaining ring's for 1/2 in. and 3/8 in. hex, side mount or not, or an e-clip's for 7 mm and 11 mm. `extrudeShaftGrooveTool` uses an end's Groove diameter instead, if it's overridden. |
| `grooveOverrideStarted`, `updateGrooveDiameter` | Editing logic: an end's Groove diameter is set to `getGrooveDefinition`'s when Override groove diameter is turned on, or the hex size or Side mount ring changes with it on. |
| `measureShaftLength` | How far the shaft goes along its axis: its tight bounding box in the shaft plane's coordinates. |
| `setCotsShaftProperties`, `setShaftProperties`, `setShaftName`, `shaftLengthString` | Properties and names, above. |
| `robotShaftEditLogic`, `applyTableDefinition`, `updateTappedHolePath`, `updateCaptiveShaftParameters`, `activePathChanged`, `shaftChanged` | Editing logic, above. |
| `getEndOperation`, `getEndOperationParameter`, `getTableAndPath`, `getShaftEndParameter`, `getShaftEndString` | Read an end's parameters (`firstEnd...` or `secondEnd...`); SplineXS ends are always tapped, and a metric hex shaft's are its `...MetricOperation`, an e-clip read as a retaining ring (a groove). |

### Data flow

The definition is the source of truth for everything after editing logic: a COTS shaft's profile is copied into it
(`withShaft`, in both editing logic and the body), and hole diameters and captive sizes are set in it from tables and
the shaft size. End modifications read their end's parameters (`firstEnd...` or `secondEnd...`); with Symmetric ends,
both ends use the first end's.

### Error handling

Checks run as the shaft is built, so an error stops it partway: flat ends after the extrude, then the mirror end, then
each end as it's modified. Failures of the end modifications (a hole, extending an end, cutting a tool) show the shaft
in blue, then rethrow (std's errors) or throw their own. COTS length problems are warnings, after the shaft is built.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `getMirrorPlane` (`try silent`) | The mirrored end's plane | Throws "The shaft end to mirror across must be planar." A type check written as a `try`; checking the face's geometry would say the same without one. |
| `modifyShaftEnd` | Std's `hole` subfeature | Shows the shaft in blue, and rethrows. |
| `extendShaftEnd` | Offsetting the end face out | Shows the shaft in blue, and throws "Failed to extend the shaft's end." |
| `extrudeCaptiveShaftTool` | Drawing a body showing the too-big captive end, for the error | Already throwing; the body is left out if it can't be drawn. |
| `cutFactoryFeatures` | Std's `hole` subfeature for a factory tapped end | Shows the shaft in blue, and rethrows. |
| `cutShaft` | Cutting a groove or captive tool | Shows the shaft in blue, and throws its end's error (above). |
| `robotShaftEditLogic` | Sketching the profile for std's extrude editing logic | A guard: with no location yet, std's editing logic runs without a profile. |

## Issues found

- The end predicates (`isTappedFirstEnd`, `isGrooveFirstEnd`, ...) each repeat `isMetricHexShaft`'s condition, as
  predicates in a precondition's `if`s can't call others; `fs_eval_features_test.py` checks each agrees with
  `getEndOperation`.
- Clearance holes have only inch sizes (#8 to 1/4).
- goBILDA's 8mm REX standoffs (1516 series, aluminum, tapped M4) aren't in the table: they're sold as standoffs.
