# Robot gear

Makes involute spur, helical, and herringbone gears or sectors of them, straight bevel gears (with spherical involute
teeth and Gleason's proportions, or with Simplified bevels, Tredgold's approximation), or cuts rack teeth into a part's
flat face, or a gear's teeth around its round one (Gear type: Gear, with Sector gear for a sector of one, Rack, or
Bevel). All are at a standard pitch (10 DP for 3D printing, 20 DP or 32 DP for FRC, module 0.8 for FTC), or a custom
module, diametral pitch, or circular pitch, with standard proportions (a bevel gear's, Gleason's). A gear is sized by its teeth or a pitch
circle, sits on its location by its bottom (or middle, top, or a bevel gear's apex, by a point manipulator), and with
Gear pair, the gear it meshes with is made too. Its roots are fully rounded (or, as Onshape's Spur gear's Root fillet,
a third or a quarter of the gap's width, or sharp; or a router bit's radius, with a slot of the bit's width down each
gap too narrow for it; a spherical bevel gear's are always full rounds), and it can have a bore.

| File | What it is |
| --- | --- |
| `robotGear.fs` | The feature: its dialog, the gear and its mate (`buildGear`, `gearMesh`, `gearMatePlane`, `bevelPairCones`), a bevel gear's depths and shape (`bevelDepths`, `bevelShape`), and cutting a rack (`cutRack`) |
| `gearCommon.fs` | The tooth form (`getGearForm`), its roots (`gearRoot`, `halfGapCurves`), a gear's profile (`sketchGearProfile`), a bevel gear's cones, proportions, spherical involute teeth, and tooth gap (`bevelCones`, `gleasonProportions`, `sphericalInvolute`, `sphericalBevelForm`, `sketchToothGap`), and a cut rack's teeth and regions (`rackToothCenters`, `rackCutRegions`, `rackRootFilletRadius`, `relieveRackRoots`, `sketchRackCut`) |
| `../core/bore.fs` | The bore: its parameters, and cutting it |
| `../core/stdRevolve.fs` | A sector's angles by Angle, as std's revolve has them (`revolveAnglePredicate`, `revolveAngles`) |
| `../core/location.fs`, `../core/startOffset.fs`, `../core/profileOffset.fs` | The location, starting offset, and profile offset |
| `../core/pointManipulator.fs` | Where the gear sits on its location, as Robot pulley's (`pointManipulatorPredicate`, `applyPointManipulator`) |
| `../core/mounting.fs` | The gear pair's Angle reference and Angle, as Robot motor's (`angleReferencePredicate`, `angleOffsetPredicate`, `applyAngleReference`, `applyAngleOffset`), and the rack's angle reference's point (`angleReferencePoint`) |
| `../../tests/featurescript/fs_eval_sketch_test.py` | Tests of gears' profiles, root fillets, and router bit slots (and racks'), the standard and custom pitches, pitch circles' teeth, bevel gaps, and where a rack's teeth go and what's cut |
| `../../tests/featurescript/gear_test.fs` | Tests of where a gear's mate goes (spur, sector, and bevel), bevel gears' crown gear and thickness errors, their shape, points, and Gleason proportions, and joining round faces' angles |
| `../../tests/featurescript/fs_eval_bevel_test.py` | Tests of spherical bevel gears' teeth: taken back onto the sphere, a pair meshes (touching on both sides, not overlapping) through a tooth; their thickness at the pitch cone; their gap's sketch |
| `spurGear.local.fs` | Onshape's Spur gear, for reference (see Compared with Spur gear): not synced |

## Changelog

Unreleased.

- Bevel gears have spherical involute teeth (exactly conjugate) and Gleason's proportions, as bought straight bevel
  gears do: a small pinion's teeth are longer and thicker (and its mate's shorter and thinner), their roots `0.188 m`
  (and 0.002 in.) below their mate's tips, rather than `0.25 m`, and their tips' cones parallel to their mate's root
  cones, so they come down a little toward the apex. Their roots are always full rounds (Root fillet is hidden).
- Simplified bevels (in Other options) makes them as before, by Tredgold's approximation with a spur gear's
  proportions, except that each gap is now sketched centered on its middle (it was sketched at the tooth before it).
- The Gear group (was Teeth) is at the top, under Gear type, with Teeth first in it, and a bevel gear's Mating gear
  teeth and Shaft angle under that. Gear pair is at the bottom, under the bore.
- Sector gear (was Sector) is a checkbox on Gear, rather than a Gear type: sized by Sector teeth, or an angle as std's
  revolve gives one (Angle, Opposite direction, Symmetric, Second end position), from an Angle reference.
- Gear type's Bevel is last. Size by is Selection, an unlabeled dropdown, with the starting offset under it.
- Gear pair: the gear it meshes with, placed by Mating gear teeth, Angle reference, and Angle, or by its pitch circle.
  It's optional for a bevel gear too, whose cones take Mating gear teeth and Shaft angle either way.
- Custom pitch takes a Pitch type (Module, Diametral pitch, or Circular pitch), as Onshape's Spur gear does, rather
  than diametral pitch for Inch and module for Metric.
- Helical gear (was Helical): Herringbone gear (was Double helical) is above Hand, which it hides.
- Racks can be helical or herringbone, and have router relief.
- Rack: Tangent propagation is gone: the faces in line with Face to cut (or on its cylinder) are always cut too.
- Router bit roots: a bit too wide to reach the root between the flanks cuts a slot of its width straight down the
  gap's middle, square to the pitch circle (or a rack's pitch line), to the root, rather than a full round above it. A
  bit too wide for the gap at the pitch circle is an error, showing the gear in blue and what its slots cut from its
  teeth in red.
- Most parameters' descriptions are gone (see the style guide's "Descriptions"), and the errors are reworded in std's
  form.
- Choosing a circle that's a pitch circle at the gear's pitch (a whole number of teeth's) for the location switches
  Selection to Pitch circle; for Gear pair's Angle reference, Mate selection.
- Bevel gears check their mate's pitch cone too: a shaft angle where the mate would be a crown or internal gear is an
  error.
- Bevel gears take a Thickness (shared with Gear's) rather than Face width: flat back and top, their teeth as long as
  the thickness reaches, and their mate's as long. Too thin for its (or its mate's) teeth, or so thick its top reaches
  its apex, is an error. It reports no info.
- A gear sits on its location by its bottom, rather than its middle; a point manipulator (hidden `index`) picks its
  bottom, middle, or top, or a bevel gear's apex.
- Root fillet (Full, 1/3, 1/4, None, or Router bit, with Bit diameter) replaces Router relief and the `0.38 m` fillet:
  full rounds by default, for gears, racks, and bevel gears.
- Rack: Face to cut can be round (a cylinder's), for a gear's teeth around it; a face inside a hole is an error. At
  Pitch diameter, the face is moved out to the teeth's tips first, so they're whole, rather than cut off at it.
- A bore that cuts into the teeth is a warning.

## Strings

| String | Where |
| --- | --- |
| Robot gear | feature name |
| Create spur, helical, herringbone, and bevel gears, or cut racks and gear teeth into parts, at FRC and FTC gears' standard pitches. `<CREDIT>` | feature description |
| `<teeth>`T `<pitch>` `<kind>`: the pitch is like 20DP, Module 0.8, or 0.25 in. CP (mm by Metric), and the kind Gear, Sector Gear, or Bevel Gear, after Helical or Herringbone if it's helical | part name |

A gear's material is Onyx, and its appearance printed green. A cut rack leaves its part's properties as they are.

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Shaft angle (`shaftAngle`) | Between its shaft and its mate's (90 degrees for a right angle): it sets the angle of its teeth's cone. |
| Simplified bevels (`simplifiedBevel`) | Tredgold's approximation, with a spur gear's proportions: a spur gear's teeth on its back cone, rather than spherical involute teeth with Gleason's proportions. |
| Face at (`rackFaceAt`) | Where the face is on the teeth: their pitch line (the face is moved out to their tips first), or their tips. |
| Angle reference (`rackReference`) | A point a gap is centered on, as on an end. |
| (the bore's, location's, and Gear pair's angle reference, from `core/`) | |

| Hidden parameter | Set by |
| --- | --- |
| Index (`index`) | The point manipulator: where the gear sits on its location (0, its bottom, by default; 1, its middle; 2, its top; 3, a bevel gear's apex). |

### Errors, warnings, and info

| Message | Kind | When | Highlights | Closest std error |
| --- | --- | --- | --- | --- |
| Select a sketch point, circle, or mate connector to use. | error | no location (`core/location.fs`) | `location` | Select a sketch plane. |
| Select a pitch circle. | error | Size by Pitch circle, with none | `pitchCircle` | Select a circle or arc. |
| Pitch circle is too small for this pitch, select a larger circle. | error | it's under 4 teeth's | `pitchCircle`, the circle | Selected offset is too large, decrease the value. |
| Pitch circle is not a whole number of teeth, using nearest (`<teeth>` teeth). | warning | its diameter isn't a whole number of modules (within std's length tolerance) | `pitchCircle` | none |
| Sector teeth must be fewer than the gear's `<teeth>` teeth. | error | Sector gear by Sector teeth, with the gear's teeth or more | `sectorTeeth`, `teeth` | The counterbore diameter must be larger than the hole diameter. |
| Sector angle is too small for a tooth, increase the value. | error | Sector gear by Angle, spanning less than a tooth | `angle`, `angleBack` | Bend angle is too small. |
| Sector angle must be less than a full turn, decrease the value. | error | Sector gear by Angle, spanning every tooth | `angle`, `angleBack` | Animation start value must be less than or equal to the end value. |
| Shaft angle is too large for a bevel gear, decrease the value or increase mating gear teeth. | error | a bevel gear's pitch cone is 90° or more: a crown or internal gear; or its face cone (its tips') is | `shaftAngle`, `bevelMateTeeth` | Selected offset is too large, decrease the value. |
| Shaft angle is too large for a bevel mating gear, decrease the value or decrease mating gear teeth. | error | its mate's pitch cone (or face cone) is 90° or more (with or without Gear pair: the pair is what it's made for) | `shaftAngle`, `bevelMateTeeth` | Selected offset is too large, decrease the value. |
| The angle reference is on the axis, so it doesn't give an angle. / The angle reference must be parallel to the location's plane. / The angle reference must be a point, circle, or line. | error | Gear pair's or a sector's Angle reference (`core/mounting.fs`, shared with Robot motor, so not reworded here) | the location or `pitchCircle`, the angle reference | Limit face must be parallel to face being moved. |
| Thickness is too small for the teeth, increase it past `<thickness>`. | error | a bevel gear's top is below its teeth's tips at its back | `thickness` | Bend angle is too small. |
| Thickness is too large, reaching the apex. Decrease it below `<thickness>`. | error | a bevel gear's top is at its apex or past it | `thickness` | Selected offset is too large, decrease the value. |
| Thickness is too small for the mating gear's teeth, increase it past `<thickness>`. | error | a bevel gear's mate's teeth, as long as its, don't reach the mate's top | `thickness` | Bend angle is too small. |
| Router bit root fillet is not available for bevel gears, select another root fillet. | error | Bevel with Simplified bevels, with Root fillet Router bit (hidden for spherical bevels) | `rootFillet` | Select an entity to align to. |
| Teeth come to points, increase teeth or change profile offset. / Teeth meet at their roots, change profile offset. | error | a gear's (or a bevel gear's virtual or projected) tooth's flanks meet below the tip circle, or above the root | `teeth`, `profileOffsetDistance` / `profileOffsetDistance` | none |
| Failed to create bevel gear teeth. / Failed to cut bevel gear teeth. | error | the gap's loft, or cutting the gaps from the blank, fails | `teeth`, `bevelMateTeeth`, `thickness`, and the gap's sketches or the gaps | Failed to extrude selections, check input. |
| Too few teeth for a gear, increase teeth. | error | the root circle's at or below the center | `teeth` | Cannot create hem, increase total length. |
| Profile offset is larger than the bit's radius, decrease profile offset or increase bit diameter. | error | the profile offset leaves no root fillet | `profileOffsetDistance`, `bitDiameter` | Cannot create hem, decrease gap. |
| Bit diameter is too large, decrease the value. | error | a router bit's full round, where it reaches the root but its fillets don't fit, meets the radial lines above where they meet the involutes; or a rack's corner fillet runs past its flank | `bitDiameter` | Selected offset is too large, decrease the value. |
| Bit diameter is too large, decrease the value. Highlighted slots cut into the teeth's flanks. | error | a router bit's slots (`gearSlot`, `rackBitTooLarge`) meet the flanks above the pitch circle (or a rack's pitch line): the bit's wider than the gap there | `bitDiameter`; the gear (or the cut rack) in blue, and what the slots cut from its teeth in red | Selected offset is too large, decrease the value. |
| Full root fillet does not fit below the flanks, select a smaller root fillet. | error | the full round would meet the radial lines above where they meet the involutes (a spherical bevel gear's too, whose Root fillet is hidden) | `rootFillet` | Radius of fillet on face too large. |
| Select a face to cut teeth into. | error | Rack, no face | `rackFace` | Select a face to terminate the extrude. |
| Could not offset face to tooth tips. | error | Face at Pitch diameter (or a round face not at its tips), and moving the faces fails | `rackFace`, `rackFaceAt`, the faces | Could not offset selected faces as requested. |
| Cannot cut teeth inside a hole, select a round face on the outside of a part. | error | a round face whose part is inside it | `rackFace`, the face | Cannot fillet parallel edges. |
| Face to cut is too small for this pitch. | error | a round face under 4 teeth's | `rackFace`, the face | Bend angle is too small. |
| Face to cut is not a whole number of teeth, cutting nearest (`<teeth>` teeth). | warning | a round face's pitch circle (its own, or a module inside it at Outside diameter) isn't a whole number of teeth's | `rackFace` | none |
| Angle reference must not be on the face's axis. | error | a round face's Angle reference, on its axis | `rackReference`, the reference | Reference must be to a face. |
| Face to cut must have a straight edge. | error | none of the faces' edges is a line | `rackFace`, the face | Reference must be to a face. |
| Select an angle reference. / Angle reference must be a point. | error | a rack's Angle reference, with none, or a selection that isn't a point | `rackReference` | Select a pattern axis. / Reference must be to a face. |
| Face to cut is too short for a tooth. | error | no whole tooth fits along it | `rackFace`, the faces | Bend angle is too small. |
| Teeth come to points, change profile offset. / Teeth meet at their roots, change profile offset. | error | a rack's profile offset's too far | `profileOffsetDistance` | none |
| Pitch diameter is `<diameter>`. | info | a spur gear (not a bevel gear), without a warning | | none |
| ... Gear centers are `<distance>` apart. | info | added to a spur pair's | | none |
| Bore cuts into gear teeth, decrease bore size or increase teeth. / Bore cuts into mating gear teeth, ... (a bevel gear's: decrease bore size or thickness, or increase teeth.) | warning | the bore's corners (and entrance chamfer) reach its root, or a bevel gear's root cone at its top, without an earlier warning | `hexWidth`, `boreDiameter`, `splineType`, `teeth`, `thickness` | Selected offset is too large, decrease the value. |
| Select a mate pitch circle. | error | Mate selection Pitch circle, with none | `matePitchCircle` | Select a circle or arc. |
| Mate pitch circle must be parallel to gear. / Mate pitch circle is too small for this pitch, select a larger circle. / Mate pitch circle is centered on gear, select a circle beside it. | error | likewise | `matePitchCircle`, the circle | Limit face must be parallel to face being moved. |
| Mate pitch circle is not a whole number of teeth, using nearest (`<teeth>` teeth). / Pitch circles do not touch, gears will not mesh. | warning | likewise (the gear's own pitch circle's warning comes first) | `matePitchCircle` | none |
| Failed to cut bore. / The bore is bigger than the part. / Failed to chamfer bore. | error | (`core/bore.fs`) | the bore's parameters, and what failed | Failed to extrude selections, check input. |

## How it works

### Execution order

1. **Precondition**: Gear type (Gear, Rack, or Bevel), then the Gear group: Teeth (a gear's, by Teeth; Selection is
   below it), a bevel gear's Mating gear teeth (`bevelMateTeeth`) and Shaft angle (its cones depend on them, with or
   without Gear pair), Pitch (with Custom, Pitch type: Module, Diametral pitch, or Circular pitch, and Pressure
   angle), a gear's Thickness, and Root fillet (with Router bit, Bit diameter; hidden for a spherical bevel gear,
   `isSphericalBevel`, whose roots are full rounds).
   - Gear and Bevel: Selection (`sizeBy`, unlabeled: Teeth or Pitch circle), then the location or Pitch
     circle, and the starting offset under it; a gear's Sector gear, with Sector size (Teeth or Angle, unlabeled),
     Angle reference, and Sector teeth or std revolve's angles (Angle, Opposite direction, Symmetric, and Second end
     position with Start angle).
   - Rack: Face to cut, Face at, Align teeth to (End, with Opposite end, or Angle reference).
   - All but Bevel: Helical gear, with Helix angle, Herringbone gear (`doubleHelical`), and Hand (hidden for
     Herringbone gear: a herringbone turned over is the other hand).
   - Gear and Bevel: bore, then Gear pair, with Mate selection (Teeth or Pitch circle, unlabeled; a bevel mate's by
     its teeth, above) and either Mating gear teeth (a spur mate's), Angle reference, and Angle (and its Opposite
     direction), or Mate pitch circle.
   - Then the profile offset, a gear's Add mate connector, a bevel gear's Simplified bevels (`simplifiedBevel`), and
     the hidden `index`.
2. **Body**: the module and pressure angle (a standard pitch's, see `GEAR_PITCHES`, or Custom's: Module, an inch over
   the diametral pitch, or the circular pitch over pi), a bevel gear's mate's teeth (`mateTeeth`, from
   `bevelMateTeeth`), and the tooth form (`getGearForm`: pitch radius `m z / 2`,
   base radius its cosine of the pressure angle, tip `m` above it and root `1.25 m` below, all offset by the profile
   offset, and the root fillet: a fraction of the full round's radius, `ROOT_FILLET_FRACTIONS`, or the bit's radius).
   Then:
   - Gear (or a sector): its teeth and plane (`gearSize`: Teeth at the location, or the nearest whole number of teeth to
     the pitch circle, `nearestGearTeeth`, at its center, with its x axis to an arc's middle), after the starting
     offset; its points along its axis (`gearPointDistances`: its bottom, middle, and top), with the point
     manipulator, which moves the gear so the chosen point is on the location (`applyPointManipulator`); the profile
     (below), its body (`buildGearBody`, below), the router bit's slots' check (`checkBitSlots`, below), its
     properties, a profile offset manipulator, mate connector, its bore
     (`cutBores`), and a warning (the pitch circle's, then the mate's, then the bore's reaching the teeth), or its
     pitch diameter as info.
   - Bevel: its teeth and plane likewise; its shape along its axis (`bevelShape`, below), whose bottom, middle, top,
     and apex are its points; then `buildBevelGear` (below), its properties, a mate connector at its apex, its bore,
     and only a warning, no info.
   - Gear pair: its mate, built the same way (`buildGear`) where `matePlacement` puts it (below), with its own mate
     connector and bore.
   - Rack: `cutTeeth`: `cutRack` for a flat face, `cutRoundTeeth` for a round one (below).
3. **Editing logic** (`robotGearEditLogic`): a circle newly chosen for the location, or Gear pair's Angle reference,
   whose radius is a pitch radius at the gear's pitch (`isWholeTeeth`: a whole number of teeth's, at least 4) moves to
   Pitch circle (or Mate pitch circle), switching Selection (or Mate selection) to Pitch circle.
4. **Manipulator change** (`robotGearManipulatorChange`): the rack's end flip, or the angle's, profile offset's, point
   manipulator's (`index`), and starting offset's.

A gear's profile (`sketchGearProfile`), per tooth (rotated copies of one centered on the plane's x axis, its other
side mirrored):

- **Flanks**: involutes, as fit splines through 9 points, from the top of a radial line up to the tip. A tooth's half
  angle at radius `r` is `90°/z + inv(α) - inv(acos(r_b / r))` (plus the offset over the base radius: an involute
  offset along its normal is the same involute rotated).
- **Root** (`gearRoot`, `halfGapCurves`): the radial line runs down from the base circle (or higher, up to half the
  tooth's height, to make room for the fillet; there it's a little inside the involute, relieving it), and a fillet
  joins it to the root circle. Full is the full round: tangent to both teeth's lines and the root circle, found by
  refining its radius (the line rises with it, which turns the line), 12 times. 1/3 and 1/4 are as Onshape's Spur
  gear's: fillets of a third or a quarter of the gap's width (two thirds or half the full round's radius). None is a
  sharp corner. A router bit's fillet that doesn't fit twice in the gap is a full round of its radius tangent to both
  lines, if that reaches the root circle; if it doesn't, the bit cuts a slot (`gearSlot`), as robot gears cut with a
  router are: the bit's path straight down the gap's middle, square to the pitch circle, its round bottom on the root
  circle. Its sides run up from there, parallel to the gap's middle and the bit's radius from it, to where they meet
  the tooth's flank (found by halving the range, from the root circle to the tip, 50 times: the gap only widens
  outward), cutting into the flanks below that.
- **Slots too large** (`checkBitSlots`): a slot meeting the flank above the pitch circle (the bit's wider than the gap
  there, half the circular pitch, as wide as a tooth) cuts into the middle of the flank, where meshing teeth always
  touch. That's an error: the gear's built as it would be, and the same teeth with sharp roots, less it, are what its
  slots cut. The gear is shown in blue (a debug entity), and what's cut in red (the error's entities).
- **Tip**: an arc on the tip circle.

A sector's teeth are centered on the x axis, and its ends run from the middle of the gaps at its ends to the center;
only its teeth are sketched, swept, and cut. `sectorPlacement` turns the plane first: by Sector teeth, so its middle
points toward the Angle reference (or a pitch arc's middle, or the location's x axis, without one); by Angle, to the
middle of the angles from it (`revolveAngles`, as std's revolve: Angle that way, or the other with Opposite direction;
half each way with Symmetric; and back to Start angle with a second end position), with as many whole teeth as fit
(`sectorSpan`). A gear pair's mate, without its own angle reference, goes toward the sector's middle.

### Bevel gears

`buildBevelGear` makes a straight bevel gear with spherical involute teeth and Gleason's proportions, or with Simplified
bevels, by Tredgold's approximation with a spur gear's proportions:

- **Cones** (`bevelCones`): its pitch cone's angle from its axis is `atan(sin Σ / (z₂ / z + cos Σ))`, for its teeth
  `z`, its mate's `z₂`, and shaft angle `Σ` (`atan(z / z₂)` at 90°). Its pitch circle (radius `m z / 2`) is at its
  back, on the location's plane, and its apex is the cone distance `R` (the pitch radius over the pitch angle's sine)
  along its pitch cone, in front of it.
- **Depths** (`bevelDepths`), at its back, along its back cone (the cone square to its pitch cone at its back), from
  its pitch circle, each offset by the profile offset:
  - Gleason's (`gleasonProportions`, as KHK's and KG's tables for Gleason straight bevels have them): a working depth of
    `2 m` shared by the pair, the gear with more teeth's addendum `0.54 m + 0.46 m / m90²` (`m90²`, the equivalent
    ratio at 90° squared, is `z_gear cos δ_pinion / (z_pinion cos δ_gear)`) and the pinion's the rest; a whole depth of
    `2.188 m` and 0.002 in. (Gleason's allowance, `GLEASON_DEPTH_ALLOWANCE`); a circular thickness at the back's pitch
    circle of `π m / 2 + (a - a₂) tan α` (so the pair's add up to the circular pitch, with no backlash; Gleason's `K m`,
    from a chart, is left out); and its face cone parallel to its mate's root cone (`atan(b₂ / R)` from its pitch cone,
    for the mate's dedendum `b₂`), so the clearance between them is the same all along, and its tips come down a
    little toward the apex.
  - Simplified: a spur gear's (`m` and `1.25 m`, half the circular pitch thick), its face cone through its apex.
- **Teeth**: a gap between two teeth (`sketchToothGap`) is sketched on the plane square to the pitch cone through the
  middle of the gap after the first tooth, centered where the axis meets it (the back cone's apex), and lofted to the
  same gap scaled toward the apex: scaled about the apex, its flanks run straight to it, as a straight bevel's do.
  - Spherical involute (`sphericalBevelForm`): a straight bevel's flanks are cones through its apex; these are through
    spherical involutes on the sphere of radius `R`, traced by a plane rolling around its base cone (`asin(sin δ cos
    α)` from its axis, so they cross the pitch cone at the pressure angle α), which is exactly conjugate: a pair
    touches along a line through their apex, on a plane tangent to both base cones, at any angle. A tooth's half width
    around the axis at `θ` from it is its half width at the pitch cone (half the thickness over the pitch radius, and
    the offset over the base cone's radius at the back) `+ invₛ(δ) - invₛ(θ)`, where `invₛ(θ) = β / sin δ_b - atan(tan β /
    sin δ_b)` and `cos β = cos θ / cos δ_b` (`sphericalInvolute`). The sketch is the flank as seen from the apex,
    projected onto the plane: there, planes through the axis are lines through its center, so the form is a gear's
    (`getGearForm`'s, with a `flankAngle` function `toothHalfAngle` uses) whose teeth's middles are lines through its
    center (`atan(tan(180° / z) cos δ)` from the gap's middle, so its `teeth` aren't a whole number), whose flank's
    angle at a radius is found by halving the range of angles from the axis the flank's point there is in (50 times),
    and whose roots are full rounds (`gearRoot`, as a gear's; the root, `b` from the pitch circle on the gap's middle,
    is on the root cone there). The flank runs up to its tips at the back, the highest they reach.
  - Simplified (Tredgold's approximation): its teeth, seen on its back cone, are close to a spur gear's whose pitch
    radius is the back cone's: the pitch radius over the pitch angle's cosine, with `z / cos(pitch angle)` teeth (its
    virtual teeth, not a whole number). Its roots are rounded as a spur gear's are (`halfGapCurves`), by Root fillet's
    fraction: the shapes scale with the gap, so both ends of the loft match. A router bit can't cut them: its teeth
    aren't square to its faces, and a bit's radius doesn't scale toward the apex, so Router bit is an error.
- **Thickness** (`bevelShape`): its flat bottom is under its teeth's roots at its back (its dedendum down its back
  cone); its flat top, Thickness above it, square to its axis; its teeth run along its pitch cone from its back to its
  top (its face width). Its mate's teeth are as long (`mateDefinition` passes the face width on), so their ends meet,
  and its mate's top is wherever that reaches. The top must be above the teeth's tips at the back (the gear's and its
  mate's), and below the apex, and its face cone (and its mate's) under 90°.
- **Body**: its blank is revolved from its section through its axis: across its bottom, up its back cone to its face
  cone, along that toward the apex to its top, and in to its axis. The gap runs a tooth's height past the blank at
  both ends (at the front, past where its root cone reaches the top, or half way from there to the apex, if that's
  nearer), so it cuts through it; it's patterned around the axis once per tooth, and cut from the blank. Inside where
  the root cone reaches the top, it's solid all the way through: its bore mustn't reach past that.
- **Meshing**: two bevel gears mesh with their apexes together and their axes the shaft angle apart, each made with
  the other's teeth as Mating gear teeth. Its mate connector is at its apex, along its axis, for mating them.

### Gear pairs

With Gear pair, the gear's mate (Mating gear teeth) is made too, meshing with it. A bevel gear without Gear pair is
the same gear, without its mate:

- **Where** (`matePlacement`): by Teeth, toward the Angle reference from the gear's center (or along the gear's x axis
  without one), turned by the Angle (with its manipulator), as Robot motor turns a motor (`core/mounting.fs`). By
  Pitch circle, at the Mate pitch circle's center (on the gear's plane), with the nearest whole number of teeth to its
  size; it warns if the circle isn't a whole number of teeth's, or doesn't touch the gear's. A spur or helical mate's center is the gears' pitch radii
  away, on the gear's plane. A bevel mate shares the gear's apex, its axis the shaft angle from the gear's, its pitch
  cone touching the gear's toward the angle; its back is its cone distance times its pitch angle's cosine from the
  apex.
- **Its teeth** (`gearMatePlane`): where their pitch circles touch, a tooth of one meets a gap of the other. The gear's
  tooth nearest the touching point is the angle to it (less a whole number of teeth) from its first tooth, which is on
  its x axis (or for a sector with an even number of teeth, half a tooth from it: `gearMesh`'s tooth phase). Turning
  the gear back by that angle turns the mate forward by it times the ratio, so the mate is turned that far from a gap
  facing the gear. Bevel gears turn the same way, each about its axis toward its apex.
- **What it is**: a whole gear (a sector's mate too), of the opposite hand if helical, with the gear's pitch,
  thickness (a bevel mate's face width), root fillet, profile offset, and bore (`mateDefinition`). It's named for its
  own teeth. It sits on the gear's plane (a spur mate), so it's placed by the same point as the gear.
- **Bevel gears**: both pitch cones (`bevelPairCones`) are checked. Over 90°, one would be a crown or internal gear,
  which a pair of bevel gears can't be: the two pitch angles add up to the shaft angle, so a shaft angle over 90°
  with a big enough ratio makes the gear with more teeth's cone too wide.

### Helical and herringbone gears

`buildGearBody` extrudes a spur gear's profile the thickness both ways from its plane. A helical gear's profile is
swept instead, both ways from its plane along helices on its pitch circle (`opHelix`, `opSweep`), which turn it about
its axis as it goes, as Onshape's Spur gear does for each tooth. The two halves are joined.

- **Lead**: the teeth turn once over `π d / tan ψ` along the axis, for pitch diameter `d` and helix angle `ψ`.
- **Hand**: a right hand helix turns clockwise looking along it, from either end, so both halves of a helical gear
  are the same hand. A herringbone's lower half is the opposite hand, the mirror image of its upper half.
- **Pitch**: the profile is in the gear's plane, so its pitch and pressure angle are transverse. A helical gear has
  the same pitch diameter, and spacing, as a spur gear of its pitch and teeth. Its normal pitch is finer, by the helix
  angle's cosine.
- At its plane (its middle), its teeth are where a spur gear's would be.

### Cutting a rack

`cutRack` cuts teeth into the part that owns Face to cut:

`cutTeeth` cuts teeth into the part that owns Face to cut: a rack along a flat face (`cutRack`), or a gear's teeth
around a round one (`cutRoundTeeth`, below). `cutRack`:

1. **The faces**: Face to cut, and the faces tangent-connected to it which are in its plane
   (`qTangentConnectedFaces`, `qCoincidesWithPlane`). Its direction is along the faces' longest straight edge
   (`longestEdgeDirection`). The part is found now: moved faces are new faces, which the selection doesn't find.
2. **Moved out**: at Pitch diameter, the faces are moved out a module, to the teeth's tips (`moveFacesOut`, as Move
   face's Offset), so the teeth are whole. The faces beside them stretch to meet them, so they're found again (the
   part's faces in the moved plane), and measured there: their length and width are their exact bounding box along
   the direction and across it.
3. **Where the teeth go** (`rackToothCenters`): a pitch (`π m`) apart, with a gap centered on the start (End), the end
   (Opposite end, also flipped by a manipulator there), or the angle reference projected onto it. As many whole teeth
   as fit, root and all, fill it.
4. **What's cut** (`rackCutRegions`): everything between the face and the teeth. The face is `m` above the pitch line,
   at the tips. Flanks are straight at the pressure angle (a rack's involute: a gear's of infinite radius), and the
   root is `1.25 m` below the pitch line, both offset by the profile offset. Teeth reaching the face leave a region per
   gap. Teeth stopping short of it (with clearance) are cut over too, in one region. Past the first and last teeth,
   it's cut to the root.
   The roots are rounded (`rackRootFilletRadius`, `relieveRackRoots`): each corner between the floor and a flank, by
   a fraction of the full round's radius (the full round's two fillets meet in the floor's middle: `w / 2 · tan(45° +
   α / 2)`, for the floor's width `w`), or a router bit's radius; a bit's, across a gap too narrow for both, a slot
   (`rackSlot`): its width straight down the gap's middle, square to the pitch line, its round bottom on the root, its
   sides cutting into the flanks up to where the gap's as wide as the bit (a round tangent to both flanks is above the
   root once the fillets overlap, so the bit couldn't reach the root). A slot meeting the flanks above the pitch line
   (`rackBitTooLarge`, worked out from the form) is an error, as a gear's is: the rack's cut (blue), and what's cut less
   the same cut with sharp roots (`rackTools` again) is what the slots cut (red). An end's round, on a floor too short
   for it, is cut off at the end.
5. The regions are sketched on a plane across the face's start (`sketchRackCut`), extruded through its width
   (`rackTools`), and cut from the part. A helical rack's are extruded slanting along it at the helix angle (`rackPieces`), up to a plane at
   the far side, with its teeth where a straight rack's are at the middle of its width; a herringbone rack's in two
   halves slanting opposite ways. Its hand is the hand of the gear it stands in for, so like gears it meshes with the
   opposite hand: a left hand rack's teeth move along it going across its width (z), and a right hand rack's back.
   They're sketched past the face's ends as far as they slant, so the ends are cut to the root all the way across.

### Cutting teeth around a round face

`cutRoundTeeth` cuts a gear's teeth around Face to cut, a cylinder's face (and the tangent-connected faces on the same
cylinder: its axis, and its radius). A face inside a hole (its normal toward its
axis) is an error: that would be an internal gear's teeth.

1. **Its teeth**: the nearest whole number to its pitch circle's (the face, at Pitch diameter, or a module inside it,
   at Outside diameter), with a warning if it isn't exactly that size.
2. **Moved out**: the faces are moved out to that gear's tip circle (`moveFacesOut`), unless they're there already,
   and found again (the part's faces on the same axis, at the tip radius): the faces beside them stretch to meet them,
   which can widen their angles.
3. **Its width and angles**: its width is its exact bounding box along the axis; its angles (`roundFacesSpan`), each
   face's from its middle as far as its vertices go either way, joined where they meet (`joinSpans`), or all the way
   around if it has circles for edges or they join all the way around.
4. **Where the teeth go**: a gap is centered on its start (End), its end (Opposite end, also flipped by a manipulator
   there), the angle reference's angle, or (all the way around, by End) the cylinder's x axis. Whole teeth, each from
   a gap's middle to the next's, fill its angles: a sector of them, or all of them.
5. **What's cut**: the teeth (`sketchGearProfile`, `buildGearBody`: helical and herringbone too) on the plane through
   the middle of its width, centered on the sector's middle (or a tooth), its router bit's slots checked as a gear's
   (`checkBitSlots`), cut from a ring around them: from their root
   circle out a module past their tips, across its angles, through its width. That's cut from the part, so past the
   first and last teeth, it's cut to the root.

## Compared with Spur gear

Onshape's Spur gear (`spurGear.local.fs`) makes spur and helical gears, one tooth cut from a blank and patterned
around it.

| Spur gear option | Here | Why |
| --- | --- | --- |
| Module, diametral pitch, or circular pitch | Standard pitches (10 DP, 20 DP, 32 DP, module 0.8), or Custom: module, diametral pitch, or circular pitch | Robot gears come in a few standard pitches |
| Pitch circle diameter (tied to Teeth by editing logic) | Size by Teeth or Pitch circle (a selected circle, with a warning if it's between sizes) | A circle places the gear too |
| Pressure angle | With the standard pitch; Custom only | 20 DP robot gears are 14.5°, which is easy to miss |
| Root fillet (none, 1/4, 1/3, full; 1/3 by default) | The same, full by default, or a router bit's radius (with slots where it can't reach the root) | Full rounds are strongest; a router bit's is what can be machined |
| Backlash | Profile offset | |
| Dedendum (1.157, 1.2, or 1.25 times the module), root and tip offsets | Left out: always `1.25 m`, and `m` | Standard full depth teeth; the profile offset covers clearance |
| Clocking angle | Left out: a tooth's on the location's x axis | Mate connectors and the location place it |
| Helical: angle, direction, double helix | Helical gear: Helix angle, Hand, Herringbone gear | Hands are how helical gears are specified |
| Chamfer | Left out | Machined robot gears aren't chamfered |
| Center bore, keyway | The shared bore (`core/bore.fs`): hex, round, spline, and so on | Robot shafts' bores, not keyways |
| Depth, symmetric, opposite direction | Thickness, on the location by its bottom, middle, or top (the point manipulator), and the starting offset | |
| Gears table, `spurGear` attribute | Left out | |

## Issues found

- No undercut: on a pinion with few teeth (under about 17 at 20°), its mate's tips can reach below its base circle,
  where a generated gear would be undercut; its radial root relieves some of that, but not all.
- The icon is the generic robot icon.
- Round faces are cut on the outside of a part only: teeth inside a hole (an internal gear's) are an error.
- Root fillets' full round is tangent to the radial lines below the involutes (as the router bit's is), not to the
  involutes themselves, as Onshape's Spur gear's is.
- Tried in Onshape (from the browser): a bevel pair, its too thin error, a spur gear on its bottom, a flat rack, and
  round teeth on an arc face at Outside and Pitch diameter. Untried: the bevel's apex point and other shaft angles, a
  flat rack at Pitch diameter, helical round teeth, and the bore warning.
- Helical gears sweep the whole profile along each helix; Onshape's Spur gear sweeps one tooth. Untried in Onshape.
- Helical racks aren't cut.
- Spherical bevel gears are untried in Onshape: their blank's face cone, the gap's loft between the projected
  sketches, and cutting it. Their meshing is tested on the sphere (`fs_eval_bevel_test.py`), from the form's flanks.
- Bevel gears' teeth aren't crowned, and they have no router bit roots, helical (spiral) teeth, or check of their
  face width against a third of the cone distance (Gleason's most). Gleason's tooth thickness factor `K` (a little
  more thickness for the pinion, from a chart) is left out.
- Gleason's proportions are for a 20° pressure angle; at 20 DP's 14.5°, Gleason's least teeth to avoid undercut are
  far more (24 or more on the pinion), and nothing warns of it (see No undercut, above).
- A spherical bevel gear's full round that doesn't fit below its flanks says to select a smaller root fillet, which it
  doesn't have (Root fillet is hidden).
- Router bit slots, and their error bodies, are untried in Onshape. A slot is allowed to cut a flank below the pitch
  circle, which is where a mating gear's tips touch on the lower flank: a bit nearly as wide as the gap there takes
  some contact away. A gear's error shows its own teeth (or a round face's), not the part; a rack's shows the part
  cut.
- REV's MAXSpline 20 DP gears' pressure angle isn't published; FTC gears besides goBILDA's (REV's, say) aren't among
  the standard pitches.
- Its regions are flush with the face, its ends, and its sides: booleans treat faces within their tolerance as the
  same, but it's untried in Onshape.
- Past the last whole tooth, the rest of the face is cut to the root, rather than left as a partial tooth.
