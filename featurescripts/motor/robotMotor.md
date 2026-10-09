# Robot motor

Cuts an FRC or FTC motor's or gearbox's mounting face into parts at a sketch point: a hole for each of its screws, in
the hole pattern chosen for faces with several (any of which can be skipped, by clicking them, as std's patterns skip
instances), and one for its pilot. For motors with a block model, it can also bring in a block motor: FRCDesign's Block
Motor, derived from its document, or for motors it doesn't have (the Minion and Yellow Jackets), an envelope of our
own; either way one part with a mate connector on its face.

| File | What it is |
| --- | --- |
| `robotMotor.fs` | The feature: its dialog, the cut, the block motor, and its manipulators and editing logic |
| `motorTables.py` | The motors' and gearboxes' faces and envelopes, with their sources; generates `motorTables.gen.fs` |
| `vendor/` | REV's and The Thrifty Bot's drawings the data's from (CTRE's and goBILDA's models are linked from `motorTables.py`) |
| FRCDesign's Block Motor | A configurable Part Studio in FRCDesign's FRC library (document `5e3874e07384706ec3840340`, version `5657eb187a0b8ed8fb95125a`), imported as `BlockMotor` |
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
| `<motor's name>`: FRC motors' (Kraken X60, Falcon 500, NEO 2.0, ...), Yellow Jackets' as goBILDA names them (5203 Series Yellow Jacket (13.7:1, 435 RPM)) | the block motor's part name |

FRCDesign's Block Motor keeps its own appearance; ours is General black (`BLACK` in `core/robotProperties.fs`). Neither
sets a material: they're envelopes, so their masses would be wrong.

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
| The parts to cut aren't behind the sketch point. Flip the primary axis. | error | no part of the merge scope is behind the sketch point (against the motor's axis) | `flipPrimaryAxis`, `scope`, the merge scope |
| Failed to cut the mounting holes. | error | subtracting the holes from the merge scope fails | `scope`, the holes |
| Failed to bring in FRCDesign's Block Motor. | error | instantiating it fails (like a document that can't be read) | `blockMotor` |

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
- **Failed to bring in FRCDesign's Block Motor.**: may show for someone who can't read FRCDesign's document; to see
  it, change the import's version to one that doesn't exist (in the code).
- **Failed to cut the mounting holes.**: a guard. Holes through a solid don't fail; to see its display, pass the
  holes a target which isn't a solid (in the code).

## How it works

### Execution order

1. **Precondition**, top to bottom:
   - Program (FRC or FTC) and Component type (Motor or Gearbox), and the hidden Has block model.
   - The sketch point (`locationPredicate`).
   - The Motor group: Motor (`frcMotorTable` or `ftcMotorTable`, with a Hole pattern level for motors with several);
     or for a gearbox, the Gearbox group: Gearbox (`frcGearboxTable` or `ftcGearboxTable`).
   - The Position group: Flip primary axis and Reorient secondary axis (`flipPrimaryAxis` and `secondaryMateAxis`, on
     one row, from `mateAxesPredicate` in `core/mounting.fs`), Angle reference, and Angle with its Opposite direction.
   - The Holes group: Merge scope (`holeMergeScopePredicate`), Fit (the screws' holes), Bore fit (the pilot's hole),
     and Skip holes, with Holes to skip (indices, from 1) as std's patterns' Skip instances have it.
   - Block motor, for a motor with Has block model.
2. **Editing logic** (`robotMotorEditLogic`): sets Has block model from the chosen motor's table entry, then calls
   `mountingEditLogic`: std's hole heuristics (`holeScopeFlipHeuristicsCall`), with a sketch point at the location,
   sets the merge scope to the parts at the location, and Flip primary axis so the holes go into them, unless they've
   been set. The heuristics name the flip `oppositeDirection`, so it's passed to them (and taken back) as that.
3. **Body**:
   1. The face (`getMotorFace`): the chosen table's entry, as a `MotorFace`.
   2. The plane: the sketch point's, turned to the angle reference, flipped and turned by Flip primary axis and
      Reorient secondary axis (`applyMateAxes`), then turned by Angle. Its normal points out of the motor's face, away from the parts.
   3. The holes' positions (`holePositions`): on the bolt circle at the face's angles, or at its positions (for
      goBILDA's, on two circles), as its drawing shows them looking at the face; the plane's normal points behind the
      face, so its x axis is the drawing's left. The angle manipulator, half again
      farther out than the farthest hole. With Skip holes, a toggle points manipulator on each hole, with the skipped ones selected, and
      info if an index is past the last hole.
   4. With Block motor (shown and checked), an error if the motor has no block model; then with an empty merge scope, an error unless
      there's a block motor.
   5. The cut (`cutMountingFace`): how far the merge scope goes behind the plane (from its bounding box, in the plane's
      coordinates; an error if it doesn't), then a sketch of the pilot's hole (its pilot, plus Bore fit's clearance)
      and the holes which aren't skipped (std's clearance hole for the screw, as Fit chooses), extruded that far
      behind the plane and subtracted from the merge scope in one boolean.
   6. The block motor (`buildBlockMotor`): FRCDesign's Block Motor (`deriveBlockMotor`), configured as the table says
      (`blockMotorOption` maps its option's id to its Motor list's enum), without its pinion, spacer, or Powerpole
      board. Its face is on its Top plane with its shaft up, so it's placed with its z axis against the plane's normal
      and its x axis the drawing's right, then turned the table's `blockAngle` (-90° for the Krakens, whose bumps it
      has at 0°, where their missing hole is at 270°). Or for motors it doesn't have, our own: cylinders for the body
      (in front of the plane), pilot, and shaft (behind it), unioned and colored. Either is named for the motor and
      given a mate connector on the plane.
   7. Sketches are deleted.
4. **Manipulator change function** (`robotMotorManipulatorChange`): toggling a hole sets Holes to skip to the
   selected holes (from 1); dragging the angle sets Angle and its Opposite direction (`angleOffsetManipulatorChange`).

### The data

`motorTables.py` lists each face: its screws, bolt circle and holes' angles (or holes' positions), and pilot; and for
motors with block models, FRCDesign's Block Motor's option and angle, or our own envelope's sizes. Motors with several
hole patterns have a Hole pattern level: the Krakens' (X60: all 11, a Falcon 500's 6, or a CIM's 2), the Minion's
(#10-32, 550, or 775), the Thrifty Pulsar's (#10-32 or 775), and goBILDA's (their 16 mm square, or all 6). Names and
order follow FRCDesign's (most used first; FRC motors as its Block Motor names them). Its comments give each value's
source:

- REV's drawings (`vendor/`) for the NEOs, MAXPlanetary, and UltraPlanetary; WCP's docs and drawings for the Krakens
  and PlanetaryX; The Thrifty Bot's TTB-0350 drawing for the Pulsar.
- CTRE's STEP of the Minion (from github.com/CrossTheRoadElec/Device-CADs, read for its holes, pilot, and body).
- goBILDA's spec sheets and STEP of the 5203 Yellow Jacket: the gearbox's length by its stages (from 1 for 1:1 to
  5.2:1, to 4 for 188:1). The 5103 gearbox has the same face.

These aren't from a vendor's dimensions:

- The NEO Vortex's pilot and the MAXPlanetary's output boss (1.25 in.), and the PlanetaryX's output (1.5 in.), are
  measured from REV's drawings and WCP's picture.
- The VersaPlanetary's pilot (0.75 in.), and the RS-775's bolt circle and pilot (29 mm, 17.5 mm), are the previous
  version of this feature's.
- goBILDA's pilot is their plates' 14 mm hole for it, which it fits in; its height (2 mm, in our Yellow Jacket's
  envelope) is a guess.
- The Block Motor's turn for each motor is read from its sketches (through Onshape's features endpoint, which answers
  without signing in now that it's public): the Krakens' bumps at +x, the NEO 2.0's and Vortex's flats at ±y.

### Error handling

Two `try`s: around the cut's boolean, which throws "Failed to cut the mounting holes." showing the holes (a failed
boolean changes nothing, so they're still there); and around instantiating FRCDesign's Block Motor, which throws
"Failed to bring in FRCDesign's Block Motor.". The rest of the errors are checks before anything's built.

## Issues found

- None of the geometry has run in Onshape: the cut's direction, the block motor's placement (including the Krakens'
  turn, and FRCDesign's Block Motor's face being at its origin), and the manipulators' positions are only reasoned out.
- Hole attributes: the holes are cut with a boolean rather than std's hole, so Onshape doesn't know them as holes
  (for hole tables and fastener mates). Std's hole was tried by the previous version and Robot bearing hat, which
  notes it crashes without some of its internal parameters.
- FRCDesign's Block Motor is imported at the version its FRC library uses; when FRCDesign updates it, the import's
  version needs updating by hand. Its pinion, spacer, and Powerpole board options aren't offered.
- The CIM, Mini CIM, and RS-775 have no block models (FRCDesign's Block Motor doesn't have them), so Block motor doesn't
  show for them.
- Has block model is set by editing logic, so goes stale when the motor changes another way (a configuration); the
  body then reports it rather than building the wrong thing.
- Swyft's motors, and REV's HD Hex and Core Hex motors, aren't listed yet.
- The Minion's other two M4 holes (on a 32 mm bolt circle) aren't in any pattern: which motor's they are isn't known.
