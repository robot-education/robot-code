# Robot spline profile

Extrudes a MAXSpline, SplineXL, or SplineXS profile from a point, to cut a spline bore or make a spline boss: the
shaft's outside, or (for tube splines) the tube's inside, with a fit. It's std's extrude with a spline
profile, so it can create, add, remove, or intersect.

| File | What it is |
| --- | --- |
| `robotSplineProfile.fs` | The feature: its dialog, sketching the profile, the extrude and its fit, and editing logic |
| `splineProfileCommon.fs` | `SplineType`, `skSplineProfile`, which sketches a spline's outside or inside profile (shared with Robot shaft), and `splineDiameter`, the size fits are for |
| `splineProfiles.py` | The profiles' source; `fs gen` writes `splineProfiles.gen.fs` |
| `../../core/stdExtrude.fs`, `../../core/profileSide.fs`, `../../core/fit.fs` | The extrude's dialog, Profile side, and Fit |

## Changelog

Since v1.1.0 (`fs changes robotSplineProfile`).

### Changes to existing features when their documents update

- **Fit replaces Offset profile, and its manipulator is gone.** Fit is Close, Free, None, or Custom (a clearance);
  Close and Free are std's clearance holes for a fastener the spline's size (1/64 and 1/32 in. across). What it fits
  follows Profile side: an outside profile grows (it's what goes on the shaft, like a bore), and an inside profile
  shrinks (it's what goes in the tube). Existing features get Close, so their profiles grow (or shrink) by 1/64 in.;
  set None to keep the shaft's own size.
- **Field tolerancing is gone** from the extrude's lengths (depth, offsets).
- End type, Symmetric, and the second end type remember their previous values.

### New

- SplineXS (a solid 15 tooth spline). The description is now "Create MAXSpline, SplineXL, and SplineXS profiles."

The MAXSpline and SplineXL profiles are generated now (`splineProfiles.py`), with the same geometry: only a stray line
from the center to a tooth of the outside profiles, which didn't change the sketch's region, is gone.

## Strings

| String | Where |
| --- | --- |
| Robot spline profile | feature name |
| Create MAXSpline, SplineXL, and SplineXS profiles. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Fit (`fit`) | How loosely it fits. Close and free fits are the standard clearance holes for a fastener its size, as in the Hole feature's tables. |
| Clearance (`fitClearance`) | How much bigger the hole is than what goes in it, across it (not per side). Negative for an interference fit. |

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select a sketch point, circle, or mate connector to use. | error | no location | `location` |
| Failed to fit the spline profile. | error | offsetting the extruded faces fails (too big a clearance) | `fit`, `fitClearance` |
| std's extrude and boolean errors | error | the extrude or boolean fails (std's `extrude` as a subfeature, `processNewBodyIfNeeded`) | (std's) |

`splineProfileCommon.fs` also has `<spline> shafts are solid, so have no inside profile.`, but this feature never
sketches an inside profile, so never shows it.

## How it works

### Execution order

1. **Precondition**: std's operation type, Spline type, Profile side, location, Fit, the extrude
   (`extrudePredicate`: end type and bounds, starting offset, symmetric, second end position), std's merge scope.
2. **Editing logic** (`robotSplineProfileEditLogic`): meant to sketch the profile so std's extrude editing logic
   (`stdExtrudeEditLogic`: up to flips, merge scope) can use it, then run it; but it throws first (see Issues found).
3. **Body**:
   1. `createEntities` sketches the spline's outside profile at the location (always the outside; Inside is made by
      offsetting it).
   2. Std's `extrude` runs as a subfeature at the top level id, as a new body (the operation type is restored after).
   3. The offset (`getSplineProfileOffset`) is applied by offsetting the extrude's side faces (`opOffsetFace`).
   4. `processNewBodyIfNeeded` applies the operation type (add, remove, intersect) with the merge scope; if that needs
      the tools rebuilt for an error display, `reconstructOp` extrudes and offsets them again.
   5. The profile sketch is deleted.
4. **Manipulator change** (`robotSplineProfileManipulatorChange`): std's extrude manipulators.

### Functions

| Function | What it does |
| --- | --- |
| `createEntities` | Sketches the outside profile at the location's plane; returns its faces. |
| `getSplineProfileOffset` | The offset applied to the extrude's sides: for Outside, out by half the fit's clearance (`fitClearance`, for `splineDiameter`); for Inside, in by the tube's wall (1/16 in.) and half the clearance more. |
| `robotSplineProfileManipulatorChange`, `robotSplineProfileEditLogic` | Above. |
| `skSplineProfile`, `splineName`, `isTubeSpline` (`splineProfileCommon.fs`) | Sketch a profile from the generated data; a spline's name; whether its shafts are tubes. |

### Data flow

The only geometry the feature creates is the extrude of the outside profile; Profile side and Fit only
change how far its sides are offset (`getSplineProfileOffset`). Everything else is std's extrude reading the
definition as it is (this feature names its parameters as std's do).

### Error handling

Selections are checked by `getLocationPlane`. The offset is the only operation caught and replaced with an error of
its own; std's extrude and boolean report their own errors.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| The body | Offsetting the extrude's sides (`opOffsetFace`) | Throws "Failed to fit the spline profile." |
| `robotSplineProfileEditLogic` (`try silent`) | Sketching the profile for std's extrude editing logic | A guard (but editing logic throws before it; see Issues found). |

## Issues found

- Editing logic reads `definition.locations`, which doesn't exist (the parameter is `location`), and passes it to
  `isQueryEmpty`, which only takes a `Query`, so editing logic throws every time it runs: std's up to flips and merge
  scope defaults never happen. This was in v1.1.0 too.
- Profile side is offered for SplineXS, whose shafts are solid: Inside shrinks the outside profile by 1/16 in. (the
  tube wall of MAXSpline and SplineXL), which matches nothing.
- "Sketch point to place spline" also takes circles and mate connectors.
