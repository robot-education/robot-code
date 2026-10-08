# Robot print adapter

Cuts a pocket for a 3D print adapter (a metal insert pressed into a printed part, like a hex or spline insert for a
printed pulley) into parts, at a point, and optionally a bore through them for the shaft.

| File | What it is |
| --- | --- |
| `robotPrintAdapter.fs` | The feature: its dialog, the pocket, the bore, and its manipulators and editing logic |
| `printAdapterProfiles.py` | The adapters (outlines, thicknesses, bosses, bores) and their dialog; `fs gen` writes `printAdapterProfiles.gen.fs` |
| `../../core/profileOffset.fs`, `../../core/simpleExtrude.fs`, `../../core/stdHole.fs`, `../../core/mounting.fs` | Offsets, the bore's extrude, the merge scope, and editing logic shared with other mounting features |
| `vendor/` | Vendors' drawings and models the outlines come from |

## Changelog

Since v1.0.0 (`fs changes robotPrintAdapter`).

### Changes to existing features when their documents update

- **The adapter choice is new, so existing features lose theirs.** v1.0.0's single Adapter list (WCP 1/2" Hex
  Adapter, WCP SplineXS Adapter, TTB Aluminum Insert) is replaced by Vendor, then Adapter. Existing features get the
  defaults, ThriftyBot's 1/2" Hex Insert (TTB-0034), until they're set again; WCP adapters change shape.
- The Selections group is gone; its parameters are at the top of the dialog.
- **Field tolerancing is gone** from the bore's lengths.

### New

- Adapters from AndyMark (1/2" Hex, 3/8" Hex, 8mm Keyed, and Kraken Spline Inserts), Last Anvil (3/8" and 1/2" Hex
  Inserts), Swyft (1/2" Hex Adapter), and ThriftyBot (SplineXS, 7mm Hex, and 3/8" Hex Inserts), besides v1.0.0's three.
  Adapters are named with their part numbers.
- Use boss, for adapters with a boss on one side (AndyMark's): leaves room for the boss above the print.
- The bore suits each adapter (its hex size, or a clearance circle); SplineXS adapters can cut a SplineXS bore instead
  (Bore type).

### Fixes

- "Failed to offset print adapter. Is the offset to large?" now says "too large".

The three v1.0.0 outlines are generated now (`printAdapterProfiles.py`), with the same geometry.

## Strings

| String | Where |
| --- | --- |
| Robot print adapter | feature name |
| Cuts mounting holes for 3D print adapters. `<CREDIT>` | feature description |

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Use boss (`useBoss`) | Leave room for the adapter's boss above the print, instead of sinking the whole adapter in. |

No parameters are hidden. Editing logic sets the shown Merge scope and Opposite direction (see Execution order).

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select a sketch point, circle, or mate connector to use. | error | no location | `location` |
| Failed to offset print adapter. Is the offset too large? | error | offsetting the pocket's sides fails | `profileOffsetDistance`, the feature's bodies |
| Failed to offset bore. Is the offset too large? | error | offsetting the bore's sides fails | `boreProfileOffsetDistance`, the feature's bodies |
| std's `HOLE_EMPTY_SCOPE` | error | Merge scope is empty | `scope`, and the pocket (and bore) as error bodies |
| std's extrude and boolean errors | error | the bore's extrude or the cut fails | (std's) |

## How it works

### Execution order

1. **Precondition**: Vendor and that vendor's Adapter (`printAdapterSelectionPredicate`, generated); Use boss
   (`printAdapterHasBoss`); location; Opposite direction; Offset profile; Merge scope; Add bore, with Bore type
   (`printAdapterHasSplineXsBore`), Offset bore profile, and the bore's extrude.
2. **Editing logic** (`robotPrintAdapterEditLogic` calls `mountingEditLogic`): std's hole heuristics
   (`holeScopeFlipHeuristicsCall`), with a sketch point at the location: sets the merge scope to the parts at the
   location, and flips Opposite direction to point into them, unless they've been set.
3. **Body**:
   1. The location's plane is flipped so the pocket goes down into the parts (and back up with Opposite direction).
   2. `createPrintAdapter`: sketches the adapter's outline (`getPrintAdapter(...).profile`) and extrudes it the pocket's
      depth (`getPocketDepth`: the adapter's thickness, less its boss with Use boss); with Offset profile, adds the
      offset's flip manipulator and offsets the pocket's sides.
   3. With Add bore, `createPrintBore`: sketches the bore (`sketchBoreProfile`: SplineXS, a hex with a corner on X, or a
      circle), extrudes it with std's `extrude` as a subfeature at the top level id (so its manipulators work), and
      with Offset bore profile, adds that offset's manipulator and offsets its sides.
   4. If Merge scope is empty, the pocket and bore are rebuilt under another id to show as error bodies, and the
      error is thrown.
   5. `processNewBodyIfNeeded` subtracts them from the merge scope (`reconstructOp` rebuilds them if it needs to show
      them for an error).
4. **Manipulator change** (`robotPrintAdapterManipulatorChange`): the profile offset's flip; with Add bore, the bore
   offset's flip and std's extrude manipulators (with the second direction and symmetric, which the bore doesn't
   have, cleared).

### Functions

| Function | What it does |
| --- | --- |
| `createPrintAdapter` | The pocket, above. |
| `createPrintBore`, `sketchBoreProfile` | The bore, above. |
| `getPocketDepth` | The adapter's thickness, less its boss with Use boss. |
| `robotPrintAdapterManipulatorChange`, `robotPrintAdapterEditLogic` | Above. |
| `printAdapterSelectionPredicate`, `printAdapterHasBoss`, `printAdapterHasSplineXsBore`, `getPrintAdapter` (generated) | The dialog's adapter parameters; which adapters have a boss, or fit SplineXS shafts; the chosen adapter's data (`PRINT_ADAPTERS`, by vendor and adapter). |

### Data flow

`getPrintAdapter` looks up the chosen adapter by vendor (`adapterVendor`) and that vendor's parameter
(`PRINT_ADAPTER_PARAMETERS`), giving its outline, thickness (0.25 in. for every adapter), boss, and bore. The pocket
and bore are tools, made as new bodies and then subtracted from the merge scope, so nothing is changed until the end.

### Error handling

Offsets are caught and replaced with errors of their own. An empty merge scope is checked after the tools are built,
so they can be shown as error bodies. Std's extrude and boolean report their own errors.

### `try`s

| Where | What it guards | When it fails |
| --- | --- | --- |
| `createPrintAdapter` | Offsetting the pocket's sides | Throws "Failed to offset print adapter. Is the offset too large?" |
| `createPrintBore` | Offsetting the bore's sides | Throws "Failed to offset bore. Is the offset too large?" |
| The body, empty Merge scope | Rebuilding the pocket and bore as error bodies | Already throwing; they're left out if they can't be built. |
| `addProfileOffsetManipulator` (`core/profileOffset.fs`) | Casting a ray to place an offset's manipulator | **Fallback**: the manipulator is left out. |
| `mountingEditLogic` (`core/mounting.fs`, `try silent`) | Sketching a point at the location for std's hole heuristics | A guard: with no location yet, the heuristics run with no location. |

## Issues found

- Opposite direction flips the plane once in the body and again in `createPrintBore`, so with it on, the pocket goes
  one way and the bore the other. Worth checking in Onshape; it was this way in v1.0.0 too.
- The vendor is "TTB" here, and "ThriftyBot" in Robot frame and Robot shaft.
- The pocket's offset is "Profile offset", and the bore's "Offset distance".
- Every adapter is assumed 0.25 in. thick, though TTB-0438's drawing doesn't show its thickness.
- "Sketch point to place print adapter" also takes circles and mate connectors.
- The feature description says "mounting holes", where the dialog and adapters say pocket and bore.
