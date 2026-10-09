# Robot motor

Cuts a motor's or gearbox's mounting face into parts at a sketch point: a hole for each of its screws (any of which
can be skipped, by clicking them, as std's patterns skip instances) and one for its pilot. For motors, it can also
model a block motor: the motor's envelope, pilot, and shaft, as one part with a mate connector on its face.

| File | What it is |
| --- | --- |
| `robotMotor.fs` | The feature: its dialog, the cut, the block motor, and its manipulators and editing logic |
| `motorTables.py` | The motors' and gearboxes' faces and envelopes, with their sources; generates `motorTables.gen.fs` |
| `vendor/` | REV's drawings the data's from |
| `../core/location.fs`, `../core/mounting.fs` | The sketch point, and its orientation (flip, secondary axis, angle reference, angle) |
| `../core/fit.fs` | Fit (the screws' holes) and Bore fit (the pilot's) |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot motor | feature name |
| Cut the mounting holes of a motor or gearbox, and model a block motor. `<CREDIT>` | feature description |
| `<motor's name>`, as FRCDesign names it (Kraken X60, NEO 2.0, ...) | the block motor's part name |

The block motor is General black (`BLACK` in `core/robotProperties.fs`), and has no material: it's an envelope, so its
mass would be wrong.

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Block motor | Model the motor as a block: its body's envelope, its pilot, and its shaft. |
| Fit, Bore fit, and their clearances | `core/fit.fs`'s |

None are hidden. Editing logic sets the shown Merge scope and Flip primary axis (see How it works).

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select a sketch point, circle, or mate connector to use. | error | no sketch point | `location` |
| `core/mounting.fs`'s angle reference errors | error | an angle reference at the sketch point, or not parallel to it | `location`, `angleReference` |
| The `<part name>` has `<n>` holes, so holes to skip past `<n>` are ignored. | info | Skip holes has an index past the face's last hole | |
| There's no block model of the `<part name>` yet. | error | Block motor, for the CIM, Mini CIM, or RS-775 | `blockMotor` |
| std's "select a merge scope" (`HOLE_EMPTY_SCOPE`) | error | an empty Merge scope, without Block motor (a block motor alone cuts nothing) | `scope` |
| The parts to cut aren't behind the sketch point. Flip the primary axis. | error | no part of the merge scope is behind the sketch point (against the motor's axis) | `oppositeDirection`, `scope`, the merge scope |
| Failed to cut the mounting holes. | error | subtracting the holes from the merge scope fails | `scope`, the holes |

### Triggering them

None of these have been tried in Onshape yet.

- **Select a sketch point...**: clear the sketch point.
- **Angle reference errors**: pick the sketch point itself as the angle reference, or a line which isn't in the sketch
  point's plane.
- **...holes to skip past...**: check Skip holes, and set a hole's index to 12 on a Kraken X60 (11 holes).
- **No block model**: choose the CIM, and check Block motor.
- **Empty merge scope**: clear Merge scope, with Block motor unchecked.
- **Not behind the sketch point**: sketch a point on a plate's top face, with the plate as Merge scope, then check Flip
  primary axis.
- **Failed to cut the mounting holes.**: a guard. Holes through a solid don't fail; to see its display, pass the
  holes a target which isn't a solid (in the code).

## How it works

### Execution order

1. **Precondition**: Component type (Motor or Gearbox); for a motor, Motor (`motorTable`) and Block motor, or for a
   gearbox, Gearbox (`gearboxTable`); the sketch point (`locationPredicate`), Flip primary axis and Reorient secondary
   axis (`axisOrientationPredicate`), Angle reference, Angle and Opposite direction (`core/mounting.fs`); Merge scope
   (`holeMergeScopePredicate`); Fit (the screws' holes) and Bore fit (the pilot's hole); and Skip holes, with Holes to
   skip (indices, from 1) as std's patterns' Skip instances have it.
2. **Editing logic** (`robotMotorEditLogic` calls `mountingEditLogic`): std's hole heuristics
   (`holeScopeFlipHeuristicsCall`), with a sketch point at the location: sets the merge scope to the parts at the
   location, and sets Flip primary axis so the holes go into them, unless they've been set.
3. **Body**:
   1. The face (`getMotorFace`): the chosen table's entry, as a `MotorFace`.
   2. The plane: the sketch point's, turned to the angle reference, flipped and turned by Flip primary axis and
      Reorient secondary axis, then turned by Angle. Its normal points out of the motor's face, away from the parts.
      The angle manipulator is added before Angle turns it.
   3. The holes' positions (`holePositions`): on the bolt circle, at the face's angles, counterclockwise from the
      plane's x axis. With Skip holes, a toggle points manipulator on each hole, with the skipped ones selected, and
      info if an index is past the last hole.
   4. With Block motor, an error if the motor has no block model; then with an empty merge scope, an error unless
      there's a block motor.
   5. The cut (`cutMountingFace`): how far the merge scope goes behind the plane (from its bounding box, in the plane's
      coordinates; an error if it doesn't), then a sketch of the pilot's hole (its pilot, plus Bore fit's clearance)
      and the holes which aren't skipped (std's clearance hole for the screw, as Fit chooses), extruded that far
      behind the plane and subtracted from the merge scope in one boolean.
   6. The block motor (`buildBlockMotor`): the body's profile (`sketchBodyProfile`: a circle, cut flat top and bottom
      for motors with flats) extruded in front of the plane by its length; the pilot (where its height is known) and
      shaft (where it has one) as cylinders behind it; unioned, named for the motor, colored, and given a mate
      connector on the plane.
   7. Sketches are deleted.
4. **Manipulator change function** (`robotMotorManipulatorChange`): toggling a hole sets Holes to skip to the
   selected holes (from 1); dragging the angle sets Angle and its Opposite direction (`angleOffsetManipulatorChange`).

### The data

`motorTables.py` lists each face: its screws, bolt circle, holes' angles, and pilot; and for motors with block models,
the body's diameter (and width across flats), length, and shaft. Names and order follow FRCDesign's (most used first).
Its comments give each value's source. These aren't from a vendor's dimensions:

- The NEO Vortex's pilot and the MAXPlanetary's output boss (1.25 in.), and the PlanetaryX's output (1.5 in.), are
  measured from REV's drawings and WCP's picture.
- The VersaPlanetary's and Sport's pilots (0.75 in. and 1.5 in.), and the RS-775's bolt circle and pilot (29 mm,
  17.5 mm), are the previous version of this feature's.
- The NEO 550's pilot height (1.5 mm) is a guess: REV's drawing doesn't dimension it.
- Envelopes leave out bumps and connectors: the Krakens' bumps (63.5 mm and 47.4 mm across them), the Falcon's fins,
  the NEO 2.0's narrower can (49.3 mm), and phase wires.

The Kraken X60 and X44's 11 holes are every 30° but for 270° (their bump); the NEO 2.0, NEO Vortex, and PlanetaryX
skip 90° and 270° (their flats).

### Error handling

The one `try` is around the cut's boolean, which throws "Failed to cut the mounting holes." showing the holes (a
failed boolean changes nothing, so they're still there). The rest of the errors are checks before anything's built.

## Issues found

- None of the geometry has run in Onshape: the cut's direction, the block motor's, and the manipulators' positions
  are only reasoned out.
- Hole attributes: the holes are cut with a boolean rather than std's hole, so Onshape doesn't know them as holes
  (for hole tables and fastener mates). Std's hole was tried by the previous version and Robot bearing hat, which
  notes it crashes without some of its internal parameters.
- The CIM, Mini CIM, and RS-775 have no block models.
- Minion and Thrifty Pulsar aren't listed: their faces weren't found.
