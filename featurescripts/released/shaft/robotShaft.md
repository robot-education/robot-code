# Robot shaft

Creates a shaft by extruding its profile from a point: a COTS shaft from vendors' tables, or a custom hex or spline
shaft. Hex and SplineXS shafts can have their ends modified (tapped or clearance holes, retaining ring grooves, captive
shaft ends), and shafts can be mirrored across an end.

| File | What it is |
| --- | --- |
| `robotShaft.fs` | The feature: its dialog, the profile, the extrude, end modifications, properties, and editing logic |
| `robotShaftCommon.fs` | Shaft types and sizes (`ShaftType`, `HexType`, `HexSize`), and their dimensions |
| `robotShaftTables.py` | The COTS shaft tables and the tapped and clearance hole tables; `fs gen` writes `robotShaftTables.gen.fs` |
| `../splineProfile/splineProfileCommon.fs` | Spline types and profiles (shared with Robot spline profile) |
| `../../core/stdExtrude.fs` | The extrude's dialog and editing logic, based on std's |
| `vendor/` | Vendors' drawings the tables were checked against |

## Changelog

Since v2.2.0 (`fs changes robotShaft`).

### Changes to existing shafts when their documents update

- **Unit system is gone; Program replaces it.** The Unit system tabs (Inch / Metric) are replaced by Program (FRC /
  FTC), which defaults to FRC, so existing shafts use inches. FTC shafts are named in millimeters.
- **Existing shafts become COTS shafts.** The new Source tabs default to COTS, so a shaft from v2.2.0 (all custom)
  becomes WCP's 1/2 in. rounded hex, the first FRC shaft, until it's set back to Custom.
- **End type no longer has Up to part**, and Up to face only takes planar faces (or mate connectors). Shafts extruded
  up to a part lose their end.
- **Field tolerancing is gone** from the extrude's lengths.
- **Ends must be flat**: up to next onto a curved face is now an error (`The shaft's ends must be flat.`) rather than
  a shaft with a curved end.
- **Depth is now Length**, for the end and the second end.
- **Clearance holes' hole table picks Free fits by default** (was Close): #8 0.177 in., #10 0.201 in., 1/4 0.266 in.
  (Close: 0.1695, 0.196, and 0.257 in.). Hole diameter is still set from the table, and can be overridden.
- **Custom shaft colors**: SplineXL and hex shafts other than UltraHex are black; UltraHex, MAXSpline, and SplineXS are
  medium gray (were: hex black but UltraHex white; splines white).
- **Custom shaft names** give their length as COTS shafts' do: inches to 3 places for FRC (`3 in. Hex Shaft`, as
  before), and millimeters to 1 for FTC (`136 mm Hex Shaft`).
- **Captive ends' Diameter and Length don't remember their previous values** (they never did: they're set from the
  shaft's size when it's created), and no longer say they do.

### New

- COTS shafts: FRC shafts from WCP, REV, AndyMark, Swyft, VEX, and ThriftyBot, and FTC shafts from goBILDA and AndyMark
  (Robits), chosen by vendor, type (as the vendor names it: Hex, Rounded hex, Hex Lite, SplineXS, ...), size (for hex),
  and material (only where a vendor sells the type and size in several). They're named `<length> <part name>`, and get their material, appearance, vendor, and the part number and
  link of the stock they're cut from; a warning says when one is longer than it's sold, or isn't one of the set
  lengths it's only sold in.
- SplineXS shafts (custom and COTS), whose ends can only be tapped; COTS SplineXS shafts can have a hole through them.
- Metric hex sizes for custom shafts: 7 mm (8mm REX) and 11 mm (12mm REX), with rounded hex diameters to match.
- Mirror shaft, with Flip mirror end: mirrors the shaft across one of its ends.
- Modify first end, Modify second end, End type, Symmetric, and the second end type remember their previous values.
- goBILDA REX shafts come in their preset lengths: Length, after the shaft, is each length goBILDA sells, or Custom. A
  preset is built as it's sold: that long from its location (with Opposite direction and Starting offset, but no end
  type, Symmetric, second end, or Mirror shaft), with goBILDA's M4 tapped ends (11 mm deep on 8mm REX, 12 mm on
  stainless 12mm, 14 mm on aluminum 12mm) and e-clip grooves (8mm: one 6 mm across, 3.2 to 4 mm from its first end;
  12mm: two 9 mm across, 3 to 4 and 10 to 11 mm), and no Shaft ends. Custom is cut like other shafts, with its ends to
  modify, and a warning if a stainless one isn't a length it's sold in.
- goBILDA's whole stainless steel REX catalog: 12mm REX without an e-clip (100, 140, and 180 mm, untapped) joins the
  8mm and 12mm with one.
- Override groove diameter, for each end's retaining ring: a Groove diameter of its own, for rings other than the
  usual ones. Turned on, it starts at the usual groove (0.39 in. for 1/2 in. hex, 0.396 in. side mount; 0.352 in. for
  3/8 in. hex, 0.303 in. side mount), and is set back to it when the hex size or Side mount ring changes.
- The description is now "Create the shafts FRC and FTC teams buy, or custom ones." (was "Create common robot shafts.").

### Fixes

- Retaining rings on metric hex now give an error saying they aren't defined, instead of failing.
- Captive shaft ends get default sizes for metric hex.

The MAXSpline and SplineXL profiles are generated now (`splineProfiles.py`), with the same geometry: only a stray line
from the center to a tooth of the outside profiles, which didn't affect the shaft, is gone.

## Strings

| String | Where |
| --- | --- |
| Robot shaft | feature name |
| Create the shafts FRC and FTC teams buy, or custom ones. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Mirror shaft (`mirrorShaft`) | Mirror the shaft across one of its ends. Useful when the shaft is modeled up to a plane of symmetry. |
| Side mount ring (`firstEndSideMount`, `secondEndSideMount`) | Whether to use a side-mountable retaining ring. |
| Groove diameter (`firstEndGrooveDiameter`, `secondEndGrooveDiameter`) | The diameter of the groove the retaining ring sits in. |
| Extend shaft (`extendFirstEnd`, `extendSecondEnd`) | Whether to extend the shaft to accommodate hardware. |

No parameters are hidden, but editing logic sets some shown ones: each end's tapped Hole diameter (from its hole table), and
its captive shaft Diameter and Length (from the shaft's size), and its Groove diameter (the ring's usual groove, when
Override groove diameter is turned on, or the hex size or Side mount ring changes with it on). A COTS shaft's profile (`shaftType`, `hexType`,
`hexSize`, `splineType`) is copied into the definition by editing logic though the parameters aren't shown, which
decides whether Shaft ends is.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select a sketch point, circle, or mate connector to use. | error | no location | `location` |
| The shaft's ends must be flat. | error | an extruded end isn't planar (up to next onto a curved face) | `endBound`, the end |
| The shaft end to mirror across must be planar. | error | Mirror on, and the mirrored end isn't planar | `flipMirrorEnd`, the end |
| Modified shaft ends must be perpendicular to the direction of the shaft. | error | a modified end is slanted (up to a slanted face) | the end |
| Failed to extend the shaft's end. | error | extending an end for a retaining ring or captive shaft fails | a captive end's Length and Extend shaft, or a retaining ring's end operation; the end |
| The captive shaft diameter is greater than the diameter of the shaft. | error | captive shaft Diameter is wider than the hex | the end's Diameter, a body showing the captive end |
| Failed to cut the captive end. | error | cutting a captive end fails | the end's Diameter and Length, the tool |
| Failed to cut the shaft's e-clip groove. | error | cutting a preset REX shaft's groove fails | `ftcShaft`; the shaft in blue, and the groove |
| Failed to cut the retaining ring's groove. | error | cutting a groove fails | the end's Side mount ring (and Groove diameter, if overridden), the tool |
| The groove diameter is larger than the shaft. | error | an overridden Groove diameter is as wide as the shaft across its corners, or wider | the end's Groove diameter; the shaft in blue, and the groove as entered in red |
| Retaining ring grooves are only defined for 1/2 in. and 3/8 in. hex. | error | Retaining ring on 7 mm or 11 mm hex | `firstEndOperation`, `secondEndOperation` |
| This shaft is only sold up to `<longest>` long. | warning | COTS, longer than its longest stock | |
| This shaft is only sold in set lengths; the shortest is `<length>`. / ...; the nearest are `<shorter>` and `<longer>`. | warning | COTS sold in set lengths (`fixedLengths`), not one of them | |
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
   for a preset length, `isPresetLength`); Shaft ends, for hex and SplineXS shafts not at a preset length.
   COTS shafts show Shaft ends by their profile, which editing logic copies into the definition (below).
2. **Editing logic** (`robotShaftEditLogic`):
   1. `withShaft`: sets the unit system from the program, and copies a COTS shaft's profile (`shaftType`, `hexType`,
      `hexSize`, `splineType`, `predrilledHoleDiameter`) into the definition, so the precondition shows what suits it.
      A preset length sets the extrude: Blind, its length, one way, not mirrored.
   2. When the feature is created: sketches the profile and runs std's extrude editing logic on it
      (`stdNewExtrudeEditLogic`), which points the extrude at what it's up to and sets the merge scope.
   3. For each end: if its hole table path changed (or the feature is new), sets its Hole diameter from
      the table (`applyTableDefinition`: a tapped hole's tap drill, or a clearance hole's diameter for its fit); if the shaft type or hex size changed (or the
      feature is new), sets its captive shaft Diameter and Length (`updateCaptiveShaftParameters`).
3. **Body**:
   1. `withShaft` again, then the profile is sketched at the location (`createShaftProfile`: a hexagon, or a spline's
      outside profile, plus its inside profile for tube splines or a predrilled hole for a solid one).
   2. Std's `extrude` runs as a subfeature on it, at the top level id, so its manipulators and merge scope work.
   3. `verifyFlatEnds` checks both caps are planar.
   4. With Mirror shaft, the mirror plane is taken from the chosen end (`getMirrorPlane`), before the ends change.
   5. At a preset length: its factory features are cut (`cutFactoryFeatures`: its e-clip grooves from its first end,
      then std's M4 tapped hole in each end, if it has them). Otherwise, for hex and SplineXS shafts: the ends to modify are found (`getShaftEndDefinitions`), hex profile features are
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
| `getShaftEndDefinitions`, `getEndDefinition` | Which ends to modify, and how (`EndDefinition`): the first end alone (clearance holes go through, so there's no second), both alike (Symmetric ends), each its own, or only the end not mirrored. A hole's diameter is the end's Hole diameter (from its hole table, or overridden). |
| `modifyShaftEnd` | A hole: std's `hole` as a subfeature, tapped (blind) or clearance (through), at the end's center, with the definition's diameter and depth. A groove or captive end: `extendShaftEnd` (offsets the end face out by the hardware's length, if extending), then cuts a tool (`extrudeShaftGrooveTool`, `extrudeCaptiveShaftTool`) from the shaft (`cutShaft`). |
| `cutFactoryFeatures` | A preset length's features as it's sold, from its table entry (`factoryGrooves`, `factoryHoleDepth`): grooves cut from its first end, and an M4 tapped hole (`FACTORY_TAPPED_HOLE`, std's ISO table) in each end. |
| `getGrooveDefinition` | A retaining ring groove's diameter and width, and how far from the end it is, for 1/2 in. and 3/8 in. hex, side mount or not. `extrudeShaftGrooveTool` uses an end's Groove diameter instead, if it's overridden. |
| `grooveOverrideStarted`, `updateGrooveDiameter` | Editing logic: an end's Groove diameter is set to `getGrooveDefinition`'s when Override groove diameter is turned on, or the hex size or Side mount ring changes with it on. |
| `measureShaftLength` | How far the shaft goes along its axis: its tight bounding box in the shaft plane's coordinates. |
| `setCotsShaftProperties`, `setShaftProperties`, `setShaftName`, `shaftLengthString` | Properties and names, above. |
| `robotShaftEditLogic`, `applyTableDefinition`, `updateCaptiveShaftParameters`, `activePathChanged`, `shaftChanged` | Editing logic, above. |
| `getEndOperation`, `getTableAndPath`, `getShaftEndParameter`, `getShaftEndString` | Read an end's parameters (`firstEnd...` or `secondEnd...`); SplineXS ends are always tapped. |

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
| `cutFactoryFeatures` | Std's `hole` subfeature for a preset's tapped ends | Shows the shaft in blue, and rethrows. |
| `cutShaft` | Cutting a groove or captive tool | Shows the shaft in blue, and throws its end's error (above). |
| `robotShaftEditLogic` | Sketching the profile for std's extrude editing logic | A guard: with no location yet, std's editing logic runs without a profile. |

## Issues found

- Retaining ring is offered for 7 mm and 11 mm hex, and errors there (clearly: "Retaining ring grooves are only defined
  for 1/2 in. and 3/8 in. hex."). FeatureScript can't hide one value of an enum, so not offering it would take a
  separate end operation enum for metric hex. goBILDA's REX shafts at their preset lengths have their e-clip grooves.
- A preset REX shaft's grooves are at its first end (where it starts); to put them at the other, flip it and offset its
  start by its length.
- goBILDA's 8mm REX standoffs (1516 series, aluminum, tapped M4) aren't in the table: they're sold as standoffs.
