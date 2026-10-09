# Robot motor

Cuts an FRC or FTC motor's or gearbox's mounting face into parts at a sketch point: a hole for each of its screws (any
of which can be skipped, by clicking them, as std's patterns skip instances) and one for its pilot. For motors with a
block model, it can also model a block motor: the motor's envelope, pilot, and shaft, as one part with a mate connector
on its face.

| File | What it is |
| --- | --- |
| `robotMotor.fs` | The feature: its dialog, the cut, the block motor, and its manipulators and editing logic |
| `motorTables.py` | The motors' and gearboxes' faces and envelopes, with their sources; generates `motorTables.gen.fs` |
| `vendor/` | REV's drawings the data's from (CTRE's and goBILDA's models are linked from `motorTables.py`) |
| `../core/location.fs`, `../core/mounting.fs` | The sketch point, and its orientation (flip, secondary axis, angle reference, angle) |
| `../core/fit.fs` | Fit (the screws' holes) and Bore fit (the pilot's) |
| `../core/program.fs` | Program (FRC or FTC) |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot motor | feature name |
| Cut the mounting holes of a motor or gearbox, and model a block motor. `<CREDIT>` | feature description |
| `<motor's name>`: FRC motors' as FRCDesign names them (Kraken X60, NEO 2.0, ...), Yellow Jackets' as goBILDA does (5203 Series Yellow Jacket (13.7:1, 435 RPM)) | the block motor's part name |

The block motor is General black (`BLACK` in `core/robotProperties.fs`), and has no material: it's an envelope, so its
mass would be wrong.

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Block motor | Model the motor as a block: its body's envelope, its pilot, and its shaft. |
| Fit, Bore fit, and their clearances | `core/fit.fs`'s |

Has block model is hidden: editing logic sets it, whether the chosen motor has a block model, and Block motor only
shows when it does (conditions can't read lookup tables). Editing logic also sets the shown Merge scope and Flip primary
axis (see How it works).

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select a sketch point, circle, or mate connector to use. | error | no sketch point | `location` |
| `core/mounting.fs`'s angle reference errors | error | an angle reference at the sketch point, or not parallel to it | `location`, `angleReference` |
| The `<part name>` has `<n>` holes, so holes to skip past `<n>` are ignored. | info | Skip holes has an index past the face's last hole | |
| There's no block model of the `<part name>`. | error | Block motor is checked, but the motor has no block model: Has block model is stale, as the motor was changed some way other than in the dialog (like a configuration) | `blockMotor` |
| std's "select a merge scope" (`HOLE_EMPTY_SCOPE`) | error | an empty Merge scope, without Block motor (a block motor alone cuts nothing) | `scope` |
| The parts to cut aren't behind the sketch point. Flip the primary axis. | error | no part of the merge scope is behind the sketch point (against the motor's axis) | `oppositeDirection`, `scope`, the merge scope |
| Failed to cut the mounting holes. | error | subtracting the holes from the merge scope fails | `scope`, the holes |

### Triggering them

None of these have been tried in Onshape yet.

- **Select a sketch point...**: clear the sketch point.
- **Angle reference errors**: pick the sketch point itself as the angle reference, or a line which isn't in the sketch
  point's plane.
- **...holes to skip past...**: check Skip holes, and set a hole's index to 12 on a Kraken X60 (11 holes).
- **No block model**: check Block motor on a Kraken X60, then set Motor to the CIM through a configuration variable
  (editing logic doesn't run, so Block motor stays shown and checked).
- **Empty merge scope**: clear Merge scope, with Block motor unchecked.
- **Not behind the sketch point**: sketch a point on a plate's top face, with the plate as Merge scope, then check Flip
  primary axis.
- **Failed to cut the mounting holes.**: a guard. Holes through a solid don't fail; to see its display, pass the
  holes a target which isn't a solid (in the code).

## How it works

### Execution order

1. **Precondition**: Program (FRC or FTC) and Component type (Motor or Gearbox); the hidden Has block model; for a
   motor, Motor (`frcMotorTable` or `ftcMotorTable`) and, with Has block model, Block motor, or for a gearbox, Gearbox
   (`frcGearboxTable` or `ftcGearboxTable`); the sketch point (`locationPredicate`), Flip primary axis and Reorient secondary
   axis (`axisOrientationPredicate`), Angle reference, Angle and Opposite direction (`core/mounting.fs`); Merge scope
   (`holeMergeScopePredicate`); Fit (the screws' holes) and Bore fit (the pilot's hole); and Skip holes, with Holes to
   skip (indices, from 1) as std's patterns' Skip instances have it.
2. **Editing logic** (`robotMotorEditLogic`): sets Has block model from the chosen motor's table entry, then calls
   `mountingEditLogic`: std's hole heuristics
   (`holeScopeFlipHeuristicsCall`), with a sketch point at the location: sets the merge scope to the parts at the
   location, and sets Flip primary axis so the holes go into them, unless they've been set.
3. **Body**:
   1. The face (`getMotorFace`): the chosen table's entry, as a `MotorFace`.
   2. The plane: the sketch point's, turned to the angle reference, flipped and turned by Flip primary axis and
      Reorient secondary axis, then turned by Angle. Its normal points out of the motor's face, away from the parts.
   3. The holes' positions (`holePositions`): on the bolt circle, at the face's angles, counterclockwise from the
      plane's x axis (or as the table gives them, for goBILDA's, on two circles). The angle manipulator, half again
      farther out than the farthest hole. With Skip holes, a toggle points manipulator on each hole, with the skipped ones selected, and
      info if an index is past the last hole.
   4. With Block motor (shown and checked), an error if the motor has no block model; then with an empty merge scope, an error unless
      there's a block motor.
   5. The cut (`cutMountingFace`): how far the merge scope goes behind the plane (from its bounding box, in the plane's
      coordinates; an error if it doesn't), then a sketch of the pilot's hole (its pilot, plus Bore fit's clearance)
      and the holes which aren't skipped (std's clearance hole for the screw, as Fit chooses), extruded that far
      behind the plane and subtracted from the merge scope in one boolean.
   6. The block motor (`buildBlockMotor`): the body's profile (`sketchBodyProfile`: a circle, cut flat top and bottom
      for motors with flats, or with the Krakens' bump at 270°, its sides tangent to the circle) extruded in front of the plane by its length; the pilot (where its height is known) and
      shaft (where it has one) as cylinders behind it; unioned, named for the motor, colored, and given a mate
      connector on the plane.
   7. Sketches are deleted.
4. **Manipulator change function** (`robotMotorManipulatorChange`): toggling a hole sets Holes to skip to the
   selected holes (from 1); dragging the angle sets Angle and its Opposite direction (`angleOffsetManipulatorChange`).

### The data

`motorTables.py` lists each face: its screws, bolt circle, holes' angles, and pilot; and for motors with block models,
the body's diameter (and width across flats, or bump), length, and shaft. Names and order follow FRCDesign's (most used
first). Its comments give each value's source:

- REV's drawings (`vendor/`) for the NEOs, MAXPlanetary, and UltraPlanetary; WCP's docs and drawings for the Krakens
  and PlanetaryX.
- CTRE's STEP of the Minion (from github.com/CrossTheRoadElec/Device-CADs, read for its holes, pilot, and body). Its
  holes are tapped for three screws, so Motor has a Hole pattern level for it: #10-32 (5 on a 1 in. bolt circle), 550
  (2x M3 on 25 mm), or 775 (2x M4 on 29 mm).
- goBILDA's spec sheets and STEP of the 5203 Yellow Jacket: 4x M4 on their 16 mm square, and 2 more 24 mm apart; the
  gearbox's length by its stages (from 1 for 1:1 to 5.2:1, to 4 for 188:1). The 5103 gearbox has the same face.

These aren't from a vendor's dimensions:

- The NEO Vortex's pilot and the MAXPlanetary's output boss (1.25 in.), and the PlanetaryX's output (1.5 in.), are
  measured from REV's drawings and WCP's picture; so are the Krakens' bumps' widths (15.5 mm and 11 mm; their reach,
  63.5 mm and 47.4 mm across the motor, is WCP's).
- The VersaPlanetary's pilot (0.75 in.), and the RS-775's bolt circle and pilot (29 mm, 17.5 mm), are the previous
  version of this feature's.
- goBILDA's pilot is their plates' 14 mm hole for it, which it fits in.
- The NEO 550's pilot height (1.5 mm) is a guess: REV's drawing doesn't dimension it.
- Envelopes leave out connectors, the Falcon's fins, the NEO 2.0's narrower can (49.3 mm), the Yellow Jacket's
  narrower motor (36 mm), and phase wires.

The Kraken X60 and X44's 11 holes are every 30° but for 270° (their bump); the NEO 2.0, NEO Vortex, and PlanetaryX
skip 90° and 270° (their flats).

FRCDesign's own Block Motor (a configurable Part Studio, with a pinion, spacer, and Powerpole board) could be
instantiated instead, but its document isn't public (Onshape refuses anonymous reads of it), so documents using the
feature would need access to it.

### Error handling

The one `try` is around the cut's boolean, which throws "Failed to cut the mounting holes." showing the holes (a
failed boolean changes nothing, so they're still there). The rest of the errors are checks before anything's built.

## Issues found

- None of the geometry has run in Onshape: the cut's direction, the block motor's, and the manipulators' positions
  are only reasoned out.
- Hole attributes: the holes are cut with a boolean rather than std's hole, so Onshape doesn't know them as holes
  (for hole tables and fastener mates). Std's hole was tried by the previous version and Robot bearing hat, which
  notes it crashes without some of its internal parameters.
- The CIM, Mini CIM, and RS-775 have no block models, so Block motor doesn't show for them.
- Has block model is set by editing logic, so goes stale when the motor changes another way (a configuration); the
  body then reports it rather than building the wrong thing.
- The Thrifty Pulsar, Swyft's motors, and REV's HD Hex and Core Hex motors aren't listed yet.
- The Minion's other two M4 holes (on a 32 mm bolt circle) aren't in any pattern: which motor's they are isn't known.
