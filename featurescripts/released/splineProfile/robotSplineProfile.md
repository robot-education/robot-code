# Robot spline profile

Extrudes a MAXSpline, SplineXL, or SplineXS profile from a point, to cut a spline bore or make a spline boss: the
shaft's outside, or (for tube splines) the tube's inside, optionally offset for fit. It's std's extrude with a spline
profile, so it can create, add, remove, or intersect.

| File | What it is |
| --- | --- |
| `robotSplineProfile.fs` | The feature: its dialog, sketching the profile, the extrude and offset, and editing logic |
| `splineProfileCommon.fs` | `SplineType`, and `skSplineProfile`, which sketches a spline's outside or inside profile (shared with Robot shaft) |
| `splineProfiles.py` | The profiles' source; `fs gen` writes `splineProfiles.gen.fs` |
| `../../core/stdExtrude.fs`, `../../core/profileSide.fs`, `../../core/profileOffset.fs` | The extrude's dialog, Profile side, and Offset profile |

## Changelog

Since v1.1.0 (`fs changes robotSplineProfile`).

### Changes to existing features when their documents update

- **Field tolerancing is gone** from the extrude's lengths (depth, offsets).
- End type, Symmetric, and the second end type remember their previous values.

### New

- SplineXS (a solid 15 tooth spline). The description is now "Create MAXSpline, SplineXL, and SplineXS profiles."

### Fixes

- Offsetting the profile no longer fails when its manipulator can't be placed: the manipulator is left out instead,
  a fallback (see `try`s).

The MAXSpline and SplineXL profiles are generated now (`splineProfiles.py`), with the same geometry: only a stray line
from the center to a tooth of the outside profiles, which didn't change the sketch's region, is gone.

## Strings

| String | Where |
| --- | --- |
| Robot spline profile | feature name |
| Create MAXSpline, SplineXL, and SplineXS profiles. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

None.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select a sketch point, circle, or mate connector to use. | error | no location | `location` |
| Failed to apply profile offset. | error | offsetting the extruded faces fails (too big an offset) | `profileOffsetDistance` |
| std's extrude and boolean errors | error | the extrude or boolean fails (std's `extrude` as a subfeature, `processNewBodyIfNeeded`) | (std's) |

`splineProfileCommon.fs` also has `<spline> shafts are solid, so have no inside profile.`, but this feature never
sketches an inside profile, so never shows it.

## How it works

### Execution order

1. **Precondition**: std's operation type, Spline type, Profile side, location, Offset profile, the extrude
   (`extrudePredicate`: end type and bounds, starting offset, symmetric, second end position), std's merge scope.
2. **Editing logic** (`robotSplineProfileEditLogic`): meant to sketch the profile so std's extrude editing logic
   (`stdExtrudeEditLogic`: up to flips, merge scope) can use it, then run it; but it throws first (see Issues found).
3. **Body**:
   1. `createEntities` sketches the spline's outside profile at the location (always the outside; Inside is made by
      offsetting it).
   2. Std's `extrude` runs as a subfeature at the top level id, as a new body (the operation type is restored after).
   3. The profile offset (`getSplineProfileOffset`: Profile offset, signed by its flip, plus 1/16 in. inward for
      Inside) is applied by offsetting the extrude's side faces (`opOffsetFace`).
   4. The profile offset's flip manipulator is added, if Offset profile is on (`addSplineProfileOffsetManipulator`,
      placed by the extrude's bounding box).
   5. `processNewBodyIfNeeded` applies the operation type (add, remove, intersect) with the merge scope; if that needs
      the tools rebuilt for an error display, `reconstructOp` extrudes and offsets them again.
   6. The profile sketch is deleted.
4. **Manipulator change** (`robotSplineProfileManipulatorChange`): std's extrude manipulators, then the profile
   offset's flip.

### Functions

| Function | What it does |
| --- | --- |
| `createEntities` | Sketches the outside profile at the location's plane; returns its faces. |
| `getSplineProfileOffset` | The offset applied to the extrude's sides: Profile offset (negated by its flip), and for Inside, 1/16 in. more, inward. |
| `addSplineProfileOffsetManipulator` | The profile offset's flip manipulator, on a side face, found by casting a ray from the middle of the extrude. |
| `robotSplineProfileManipulatorChange`, `robotSplineProfileEditLogic` | Above. |
| `skSplineProfile`, `splineName`, `isTubeSpline` (`splineProfileCommon.fs`) | Sketch a profile from the generated data; a spline's name; whether its shafts are tubes. |

### Data flow

The only geometry the feature creates is the extrude of the outside profile; Profile side and Offset profile only
change how far its sides are offset (`getSplineProfileOffset`). Everything else is std's extrude reading the
definition as it is (this feature names its parameters as std's do).

### Error handling

Selections are checked by `getLocationPlane`. The offset is the only operation caught and replaced with an error of
its own; std's extrude and boolean report their own errors.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| The body | Offsetting the extrude's sides (`opOffsetFace`) | Throws "Failed to apply profile offset." |
| `addProfileOffsetManipulator` (`core/profileOffset.fs`) | Casting a ray to place the profile offset's manipulator | **Fallback**: the manipulator is left out. |
| `robotSplineProfileEditLogic` (`try silent`) | Sketching the profile for std's extrude editing logic | A guard (but editing logic throws before it; see Issues found). |

## Issues found

- Editing logic reads `definition.locations`, which doesn't exist (the parameter is `location`), and passes it to
  `isQueryEmpty`, which only takes a `Query`, so editing logic throws every time it runs: std's up to flips and merge
  scope defaults never happen. This was in v1.1.0 too.
- Profile side is offered for SplineXS, whose shafts are solid: Inside shrinks the outside profile by 1/16 in. (the
  tube wall of MAXSpline and SplineXL), which matches nothing.
- "Profile offset" here is "Offset distance" for the bore in Robot print adapter.
- "Sketch point to place spline" also takes circles and mate connectors.
