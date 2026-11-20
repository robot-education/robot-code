# Robot gear

Makes involute spur, helical, and herringbone gears, internal gears (teeth inside a ring, sized by its thickness out
from their roots, pitch circle, or tips, or its outside diameter), straight bevel gears (spherical involute teeth
with Gleason's proportions), or cuts rack teeth into a part's flat face,
or a gear's teeth around its round one, all along it or (Partial rack) part of it (Gear type: Gear, Rack, Bevel, or
Internal). All are at a standard pitch (10 DP for 3D printing, 20 DP or 32 DP for FRC, module 0.8 for FTC), or a custom
module, diametral pitch, or circular pitch, with standard proportions (a bevel gear's, Gleason's). A gear is sized by its teeth or a pitch
circle, sits on its location by its bottom (or middle, top, or a bevel gear's apex, by a point manipulator), and with
Gear pair, the gear it meshes with is made too. Its roots are fully rounded (or, as Onshape's Spur gear's Root fillet,
a third or a quarter of the gap's width; or a router bit's radius, with a slot of the bit's width down each gap too
narrow for it; a bevel gear's are full rounds, or a router bit's slots where it can't reach them), or without Root
fillet, sharp, and it can have a bore (an internal gear's roots are rounded the same ways; its teeth are shaped for
its pinion, by Mating gear teeth, and its pinion is its mate, with the bore). A bevel gear's teeth are cut off by its flat top (Flat top), or
without it, end square to its pitch cone, their roots flush with a whole round top.

| File | What it is |
| --- | --- |
| `robotGear.fs` | The feature: its dialog, the gear and its mate (`buildGear`, `gearMesh`, `gearMatePlane`, `bevelPairCones`), a bevel gear's depths, shape, and section (`bevelDepths`, `bevelShape`, `bevelSection`), and cutting a rack (`cutRack`) |
| `gearCommon.fs` | The tooth form (`getGearForm`), its roots (`gearRoot`, `halfGapCurves`, `lowestContactRadius`), a gear's profile (`sketchGearProfile`), a bevel gear's cones, proportions, spherical involute teeth, tooth gap, and router bit slots (`bevelCones`, `gleasonProportions`, `sphericalInvolute`, `sphericalBevelForm`, `sketchToothGap`, `bevelBitRoot`), an internal gear's form, its mesh with its pinion, roots, router bit slots, and profile (`getInternalGearForm`, `internalTipRadius`, `internalMesh`, `internalMeshInterferes`, `internalGapHalfAngle`, `internalFullRound`, `internalFillet`, `internalFilletAt`, `internalSlot`, `internalRoot`, `sketchInternalGearProfile`), and a cut rack's teeth and regions (`rackToothCenters`, `partialTeeth`, `rackCutRegions`, `rackRootFilletRadius`, `relieveRackRoots`, `sketchRackCut`) |
| `../../core/bore.fs` | The bore: its parameters, and cutting it |
| `../../core/boreProfiles.py` | SplineXS bores at each standard fit, sketched with their fit, generating `boreProfiles.gen.fs` |
| `../../../tests/featurescript/bore_test.fs` | Tests of bores' sizes and fits, the SplineXS bores' clearances, and the mate's own bore (its parameters, and those its errors highlight) |
| `../../core/location.fs`, `../../core/startOffset.fs`, `../../core/profileOffset.fs` | The location, starting offset, and profile offset |
| `../../core/pointManipulator.fs` | Where the gear sits on its location, as Robot pulley's (`pointManipulatorPredicate`, `applyPointManipulator`) |
| `../../core/mounting.fs` | The gear pair's Angle reference and Angle, as Robot motor's (`angleReferencePredicate`, `angleOffsetPredicate`, `applyAngleReference`, `applyAngleOffset`), and the rack's angle reference's point (`angleReferencePoint`) |
| `../../../tests/featurescript/fs_eval_sketch_test.py` | Tests of gears' profiles, root fillets, and router bit slots (and racks'), internal gears' profiles (their flanks, tips stopping at their pinion's interference point, too large a pinion, root fillets, router bit fillets and slots, bits too wide for the gaps at the pitch circle, and profile offset), the standard and custom pitches, pitch circles' teeth, bevel gaps, the units values are shown in, where a rack's teeth go and what's cut (never a partial tooth at an end), a partial rack's teeth and stop, and the SplineXS bores' profiles |
| `../../../tests/featurescript/gear_test.fs` | Tests of an internal gear's ring's outside (by thickness from each diameter, or Outside diameter, and switching to it), its mesh with its pinion (its tips, where they touch, which pinions interfere, against a simulation, and its roots meeting its flanks no lower than the pinion's tips touch them), and of where a gear's mate goes (spur, with the center to center adjustment, internal, and bevel), bevel gears' crown gear and thickness errors, their shape, points, section (with and without Flat top), the rack's Angle and stop manipulators, and Gleason proportions, and joining round faces' angles |
| `../../../tests/featurescript/fs_eval_bevel_test.py` | Tests of spherical bevel gears' teeth: taken back onto the sphere (their roots' radial lines too), a pair meshes (touching on both sides, not overlapping) through a tooth; their thickness at the pitch cone; their gap's sketch; where a router bit cuts slots |
| `robotGearIcon.py` | Its icon, a thin spur gear with a hex bore through it, generating `robotGearIcon.svg` |
| `spurGear.local.fs` | Onshape's Spur gear, for reference (see Compared with Spur gear): not synced |

## Changelog

Since v1.3.0 (`fs changes robotGear`).

- Flat top was the wrong way round: on, a bevel gear's teeth are now cut off by its flat top; off (the default), they
  end square to its pitch cone. Existing bevel gears flip when they update.
- A rack's Face at is Measured from, its options Root diameter (was Base), Pitch diameter, and Tip diameter (was
  Outside diameter), as an internal gear's Ring thickness's.
- A rack's Align teeth to is a checkbox, Align teeth: off, its teeth line up with an end (Opposite end); on, with the
  Angle reference, turned by an Angle (Opposite direction flips it). The Angle reference can be a line too.
- An internal gear's pinion's bore warning highlights Mating gear teeth, not Thickness: only a bevel gear's thickness
  reaches its bore.

## Strings

| String | Where |
| --- | --- |
| Robot gear | feature name |
| Create spur and bevel gears and cut gear racks into parts. `<CREDIT>` | feature description |
| `<teeth>`T `<pitch>` `<kind>`: the pitch is like 20DP, Module 0.8, or 0.25 in. CP (or mm, see `pitchUnits`), and the kind Gear, Bevel Gear, or Internal Gear, after Helical or Herringbone if it's helical | part name |

A gear's material is Onyx, and its appearance black. A cut rack leaves its part's properties as they are.

### Descriptions and hidden parameters

Its own parameters have none. From `core/`:

| Parameter | Description |
| --- | --- |
| Width (`hexWidth`, a custom hex bore's, and `mateHexWidth`, the mate's own bore's) | Across the hex's flats. |
| Start offset reference (`startOffsetReference`, the starting offset's) | A reference point, planar edge, or plane to measure the offset from. |

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
| Shaft angle is too large for a bevel gear, decrease the value or increase mating gear teeth. | error | a bevel gear's pitch cone is 90° or more: a crown or internal gear; or its face cone (its tips') is | `shaftAngle`, `bevelMateTeeth` | Selected offset is too large, decrease the value. |
| Shaft angle is too large for a bevel mating gear, decrease the value or decrease mating gear teeth. | error | its mate's pitch cone (or face cone) is 90° or more (with or without Gear pair: the pair is what it's made for) | `shaftAngle`, `bevelMateTeeth` | Selected offset is too large, decrease the value. |
| The angle reference is on the axis, so it doesn't give an angle. / The angle reference must be parallel to the location's plane. / The angle reference must be a point, circle, or line. | error | Gear pair's Angle reference (`core/mounting.fs`, shared with Robot motor, so not reworded here) | the location or `pitchCircle`, the angle reference | Limit face must be parallel to face being moved. |
| Thickness is too small, increase the value. | error | a bevel gear's top is below its teeth's tips at its back, or its mate's teeth, as long as its, don't reach the mate's top | `thickness` | Bend angle is too small. |
| Selected thickness is too large, decrease the value. | error | a bevel gear's top is at its apex or past it; or without Flat top, its tips (coming down toward its apex, as Gleason's do) reach its pitch cone before its front cone (`bevelShape`) | `thickness` | Selected offset is too large, decrease the value. |
| Profile offset is too small, increase the value. / Profile offset is too large, decrease the value. | error | a gear's (or a bevel gear's projected) teeth come to points below the tip circle, or meet at their roots | `profileOffsetDistance`, `teeth` / `profileOffsetDistance` | Selected offset is too large, decrease the value. |
| Failed to create bevel gear teeth. / Failed to cut bevel gear teeth. | error | the blank's revolve, the gap's loft, taking what's in front of the front cone (without Flat top) from the gap, or patterning the gaps and cutting them from the blank, fails | `teeth`, `bevelMateTeeth`, `thickness`, and the blank's section, the gap's sketches, the gap, or the gaps | Failed to extrude selections, check input. |
| Too few teeth for a gear, increase teeth. | error | the root circle's at or below the center | `teeth` | Cannot create hem, increase total length. |
| Too few teeth for an internal gear, increase teeth. / Profile offset is too large, decrease the value. | error | an internal gear's gaps come to points below its root circle (the second with a positive profile offset) | `teeth` / `profileOffsetDistance` | Cannot create hem, increase total length. |
| Mating gear teeth is too large for an internal gear, decrease the value or increase teeth. | error | an internal gear's pinion has as many teeth or more, or the two's tip corners, turning, go into the other's teeth (`internalMeshInterferes`): with few teeth between them | `internalMateTeeth`, `teeth` | Selected offset is too large, decrease the value. |
| Profile offset is too small, increase the value. | error | an internal gear's teeth come to points outside its tip circle | `profileOffsetDistance`, `teeth` | Selected offset is too large, decrease the value. |
| Could not create full root fillet. | error | an internal gear's full round (or a fraction of it) doesn't touch its flanks between its tip and root circles, or is bigger than their curvature where it does (`internalFullRound`, `internalFillet`) | `rootFillet` | Radius of fillet on face too large. |
| Bit diameter is too large, decrease the value. | error | an internal gear's router bit fillets don't fit its flanks, or its slot's round end, a little wider than the full round, doesn't meet them (`internalSlot`) | `bitDiameter` | Selected offset is too large, decrease the value. |
| Bit diameter is too large for the tooth profile. | error | an internal gear's slots meet its flanks inside its pitch circle (as a gear's do, `checkBitSlots`), or the bit's wider than its gaps there; or leave nothing of its tips | `bitDiameter`; its teeth with sharp roots in blue, and the bit's slots in red, built for the error | Selected offset is too large, decrease the value. |
| Ring thickness is too small, increase the value. / Outside diameter is too small, increase the value. | error | an internal gear's ring's outside is at or inside its root circle | `ringThickness`, `ringFrom` / `outsideDiameter` | Selected offset is too large, decrease the value. |
| Profile offset is larger than the bit's radius, decrease profile offset or increase bit diameter. | error | the profile offset leaves no root fillet | `profileOffsetDistance`, `bitDiameter` | Cannot create hem, decrease gap. |
| Bit diameter is too large, decrease the value. | error | a router bit's full round, where it reaches the root but its fillets don't fit, meets the radial lines above where they meet the involutes (a bevel gear's, at its gap's small end); or a rack's corner fillet runs past its flank | `bitDiameter` | Selected offset is too large, decrease the value. |
| Bit diameter is too large for the tooth profile. | error | a router bit's slots (`gearSlot`, `rackBitTooLarge`) meet the flanks above the pitch circle (or a rack's pitch line): the bit's wider than the gap there. Checked before anything's built (`checkBitSlots`; a rack's in `cutRack`) | `bitDiameter`; a gear's teeth with sharp roots (or the rack's part) in blue, and the bit's slots in red, built for the error | Selected offset is too large, decrease the value. |
| Failed to create rack teeth. / Failed to cut rack teeth. | error | a rack's (or round face's teeth's) tools' extrude, the ring around round teeth, or cutting them from the part, fails: nothing known to | `rackFace`, `rackFaceAt`, `rootFillet`, `bitDiameter`, `profileOffsetDistance`, `helixAngle`; the profile, the teeth and ring, or what's cut (and the part in blue) | Failed to extrude selections, check input. |
| Failed to create gear teeth. | error | a spur, helical, or internal gear's teeth's extrude or sweep fails, or joining a helical internal gear's teeth to its ring (`buildGearBody`, `buildInternalGearBody`): nothing known to (the bit's slots are checked first) | `teeth`, `rootFillet`, `bitDiameter`, `profileOffsetDistance`, `helixAngle`; the profile, or the teeth and ring | Failed to extrude selections, check input. |
| Could not create full root fillet. | error | a spur gear's full round would meet the radial lines above where they meet the involutes (a bevel gear's is a smaller fillet instead) | `rootFillet` | Radius of fillet on face too large. |
| Bit diameter is too large for the tooth profile. | error | a bevel gear's router bit slot meets its flanks above the pitch cone at its gap's small end (at its pitch cone), where it's narrowest (`bevelBitRoot`), found before its teeth are cut | `bitDiameter`; the blank in blue, and the bit's slots in red, built for the error | Selected offset is too large, decrease the value. |
| Failed to cut router bit slots. | error | a bevel gear's slot's extrude, or joining it to its gap, fails | `bitDiameter`, the gap and slot | Failed to extrude selections, check input. |
| Select a face to cut teeth into. | error | Rack, no face | `rackFace` | Select a face to terminate the extrude. |
| Failed to offset face. | error | Measured from Pitch diameter or Root diameter (or a round face not at its tips), and moving the faces fails | `rackFace`, `rackFaceAt`, the faces | Could not offset selected faces as requested. |
| Cannot cut teeth on a concave face. | error | a round face whose part is inside it (a hole's) | `rackFace`, the face | Cannot fillet parallel edges. |
| Face to cut is too small for this pitch. | error | a round face under 4 teeth's | `rackFace`, the face | Bend angle is too small. |
| Face to cut is not a whole number of teeth, cutting nearest (`<teeth>` teeth). | warning | a round face's pitch circle (its own; a module inside it at Tip diameter; `1.25 m` outside it at Root diameter) isn't a whole number of teeth's | `rackFace` | none |
| Angle reference could not be projected onto the face. | error | a round face's Angle reference, a point on its axis, or a line along it (neither has an angle around it) | `rackReference`, the reference | Reference must be to a face. |
| Failed to find edge to cut along. | error | none of the faces' edges is a line | `rackFace`, the face | Reference must be to a face. |
| Stop reference must be a point. / Stop reference could not be projected onto the face. | error | the Stop reference isn't a point; or a round face's is on its axis (which has no angle around it). It's projected onto the face: a flat face's along it, a round face's around it | `rackStop`, the reference | Reference must be to a face. |
| Stop distance is too small for a tooth, increase the value. | error | Stop by Stop reference: the gap after the first tooth would run past the stop | `rackStopDistance`, `rackStop`, the reference | Bend angle is too small. |
| Select an angle reference. | error | a rack's Align teeth, with no Angle reference | `rackReference` | Select a pattern axis. |
| Angle reference must cross the face. | error | a flat face's Angle reference, a line along the face's length | `rackReference`, the reference | Reference must be to a face. |
| Face to cut is too short for a tooth. | error | no whole tooth fits along it | `rackFace`, the faces | Bend angle is too small. |
| Profile offset is too small, increase the value. / Profile offset is too large, decrease the value. | error | a rack's teeth come to points below the face, or meet at their roots | `profileOffsetDistance` | Selected offset is too large, decrease the value. |
| Pitch diameter is `<diameter>`. | info | a spur or internal gear (not a bevel gear), without a warning | | none |
| ... Outside diameter is `<diameter>`. | info | added to an internal gear's, sized by Ring thickness | | none |
| ... Gear centers are `<distance>` apart. | info | added to a spur or internal pair's | | none |
| Bore cuts into gear teeth, decrease bore size or increase teeth. / Bore cuts into mating gear teeth, ... (a bevel gear's: decrease bore size or thickness, or increase teeth.) | warning | the bore's corners (and entrance chamfer) reach its root, or a bevel gear's root cone at its top, without an earlier warning | `boreHexSize`, `hexWidth`, `boreDiameter`, `splineType`, `teeth` (the mate's: `mateTeeth`, `bevelMateTeeth`, or `internalMateTeeth`), and a bevel gear's `thickness` | Selected offset is too large, decrease the value. |
| Select a mate pitch circle. | error | Mate selection Pitch circle, with none | `matePitchCircle` | Select a circle or arc. |
| Mate pitch circle must be parallel to gear. / Mate pitch circle is too small for this pitch, select a larger circle. / Mate pitch circle is centered on gear, select a circle beside it. | error | likewise | `matePitchCircle`, the circle | Limit face must be parallel to face being moved. |
| Mate pitch circle is not a whole number of teeth, using nearest (`<teeth>` teeth). / Mate pitch circle is not the center to center distance away, gears will not mesh. | warning | likewise (the gear's own pitch circle's warning comes first); the distance is their pitch radii and the center to center adjustment | `matePitchCircle` | none |
| Failed to cut bore. / The bore is bigger than the part. / Failed to chamfer bore. | error | (`core/bore.fs`) the gear's bore, or its mate's (its own, with its own parameters) | the bore's parameters (or the mate's own), and what failed in red; the gear in blue (built again for the error, `rebuild`, if the bore used it up) | Failed to extrude selections, check input. |

## How it works

### Execution order

1. **Precondition**: Gear type (Gear, Rack, Bevel, or Internal), and a gear's Size by (`sizeBy`: Teeth or Pitch circle), then the
   Gear group: Teeth (a gear's, by Teeth), a bevel gear's Mating gear teeth (`bevelMateTeeth`) and Shaft angle (its cones depend on them, with or
   without Gear pair), an internal gear's Mating gear teeth (`internalMateTeeth`: its teeth are shaped for its pinion,
   likewise), Pitch (with Custom, Pitch type: Module, Diametral pitch, or Circular pitch, and Pressure
   angle), a gear's Thickness, an internal gear's Ring size (`ringSizeBy`: Ring thickness, with Measured from, `ringFrom`: Root, Pitch, or Tip diameter;
   or Outside diameter), a bevel gear's Flat top (`flatTop`), and Root fillet (`addRootFillet`), with its Fillet type (`rootFillet`, or a bevel
   gear's own, `bevelRootFillet`: Full or Router bit), with Router bit, Bit diameter (`isRouterBit`).
   - Gear, Bevel, and Internal: the location (by Teeth) or Pitch circle, and the starting offset under it.
   - Rack: Face to cut, Measured from (`rackFaceAt`: Root, Pitch, or Tip diameter), Align teeth (`alignTeeth`: on, a
     group of Angle reference, `rackReference`, and Angle, `rackAngle`, with Opposite direction; off, Opposite end),
     and Partial rack, with
     Stop by (`rackStopBy`): Teeth, with Rack teeth, or Stop reference, with Stop reference (`rackStop`, optional) and
     Stop distance (`rackStopDistance`).
   - All but Bevel: Helical gear, with Herringbone gear (`doubleHelical`), Helix angle, and Hand (hidden for
     Herringbone gear: a herringbone turned over is the other hand).
   - Gear and Bevel (`isExternalGear`): the bore (`core/bore.fs`): Bore (Hex, Round, or Spline), a hex's Hex size
     (`boreHexSize`, with Custom, Width), a round bore's Diameter, or a Spline type; the fit for a standard hex or a
     spline (`boreHasFit`); and Entrance chamfer.
   - Gear, Bevel, and Internal: Gear pair, with Mate selection (a spur mate's: Teeth or Pitch circle, unlabeled; a
     bevel or internal gear's mate is by its teeth, above) and either Mating gear teeth (a spur mate's), Angle
     reference, and Angle (and its Opposite direction), or Mate pitch circle; then a spur or bevel mate's Use gear's
     bore (`mateUsesGearBore`), and without it, or for an internal gear's pinion (`hasMateBore`), the mate's own bore
     (`mateBorePredicate`: the bore's parameters, each with `mate` before it, `mateAddBore` and so on, and its fit's,
     `mateBoreFitPredicate`).
   - Then the profile offset, a gear's Add mate connector, a spur or internal pair's Center to center adjustment (`isSpurPair`), and
     the hidden `index`.
2. **Body**: the module and pressure angle (a standard pitch's, see `GEAR_PITCHES`, or Custom's: Module, an inch over
   the diametral pitch, or the circular pitch over pi), a bevel or internal gear's mate's teeth (`mateTeeth`, from
   `bevelMateTeeth` or `internalMateTeeth`), and the tooth form (`getGearForm`: pitch radius `m z / 2`,
   base radius its cosine of the pressure angle, tip `m` above it and root `1.25 m` below, all offset by the profile
   offset, and the root fillet: a fraction of the full round's radius, `ROOT_FILLET_FRACTIONS`, or the bit's radius;
   none, without Root fillet, `rootFilletSize`).
   Then:
   - Gear: its teeth and plane (`gearSize`: Teeth at the location, or the nearest whole number of teeth to
     the pitch circle, `nearestGearTeeth`, at its center, with its x axis to an arc's middle), after the starting
     offset; its points along its axis (`gearPointDistances`: its bottom, middle, and top), with the point
     manipulator, which moves the gear so the chosen point is on the location (`applyPointManipulator`); the router
     bit's slots' check (`checkBitSlots`, below), the profile (below), its body (`buildGearBody`, below), its
     properties, a profile offset manipulator, mate connector, its bore
     (`cutBores`, which rebuilds the gear to show, with its `rebuild`, if the bore uses it up), and a warning (the pitch circle's, then the mate's, then the bore's reaching the teeth), or its
     pitch diameter as info.
   - Internal: its teeth, plane, and points as a gear's; its form, for its pinion (`getInternalGearForm`, below), its
     ring's outside (`internalRingRadius`, below), the router bit's slots' check (`checkBitSlots`), its profile
     (`sketchInternalGearProfile`, below) and body (`buildInternalGearBody`), its properties, a profile offset
     manipulator (pointing in: its offset grows its teeth inward), mate connector, and the pitch circle's warning, or
     its pitch and outside diameters as info. It has no bore; with Gear pair, its pinion, its mate (below), has its
     own, and the info its centers' distance.
   - Bevel: its teeth and plane likewise; its shape along its axis (`bevelShape`, below), whose bottom, middle, top,
     and apex are its points; then `buildBevelGear` (below), its properties, a mate connector at its apex, its bore,
     and only a warning, no info.
   - Gear pair: its mate, built the same way (`buildGear`) where `matePlacement` puts it (below), with its own mate
     connector and bore (an internal gear's ring has none: it has no `hubRadius`).
   - Rack: `cutTeeth`: `cutRack` for a flat face, `cutRoundTeeth` for a round one (below).
3. **Editing logic** (`robotGearEditLogic`): a circle newly chosen for the location, or Gear pair's Angle reference,
   whose radius is a pitch radius at the gear's pitch (`isWholeTeeth`: a whole number of teeth's, at least 4) moves to
   Pitch circle (or Mate pitch circle), switching Size by (or Mate selection) to Pitch circle. Switching an internal
   gear's Ring size from Ring thickness to Outside diameter, which hasn't been set since the dialog opened, sets it to
   the ring's as it was (`internalRingRadius`), so the gear doesn't change; by Pitch circle, the circle's teeth are read
   in a `try silent`, as editing logic mustn't throw while the dialog's being filled in (with none chosen yet, it's
   left as it is).
4. **Manipulator change** (`robotGearManipulatorChange`): the rack's end flip; the rack's Angle (a round face's
   turned, a flat face's dragged along it, a pitch a turn, less whole turns either way); a partial rack's stop (its Stop
   distance: a flat face's dragged along it, a round face's turned, its angle times its pitch radius, which
   `roundFacePitchRadius` finds from Face to cut as `cutRoundTeeth` does); or the angle's, profile offset's, point
   manipulator's (`index`), and starting offset's.

A gear's profile (`sketchGearProfile`), per tooth (rotated copies of one centered on the plane's x axis, its other
side mirrored):

- **Flanks**: involutes, as fit splines through 9 points, from the top of a radial line up to the tip. A tooth's half
  angle at radius `r` is `90°/z + inv(α) - inv(acos(r_b / r))` (plus the offset over the base radius: an involute
  offset along its normal is the same involute rotated).
- **Root** (`gearRoot`, `halfGapCurves`): the radial line runs down from the base circle (or higher, up to half the
  tooth's height, to make room for the fillet; there it's a little inside the involute, relieving it; a bevel gear's
  stops at `contactRadius`, where its mate's tips touch it, `lowestContactRadius`), and a fillet joins it to the root
  circle. Full is the full round: tangent to both teeth's lines and the root circle, found by
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
  touch. That's an error, found from the form before anything of the gear is built: a bit much wider (a 3/16 in. one
  on a 60T 20 DP gear, whose gaps are 0.15 in. apart at its root) cuts its gaps' slots into each other, leaving a
  profile whose extrude fails, or makes a body booleans can't use. What's shown is built for the error, without
  booleans: the teeth with sharp roots, in blue (a debug entity), and the bit's slots, in red (the error's entities),
  its path down a gap's middle from its round end on the root circle out to the tip circle, built once down the
  plane's x axis (extruded, or swept as the teeth are), patterned to each gap's middle, and the first deleted.
- **Failed to create gear teeth.**: the extrude, or a helical gear's sweeps and the join of its halves
  (`buildGearBody`), are in a `try`, so no operation's own error shows: it shows the profile it was given. Nothing's
  known to fail it.
- **Tip**: an arc on the tip circle.

### Internal gears

An internal gear (`getInternalGearForm`, `sketchInternalGearProfile`) is a ring with its teeth pointing in, toward
its center. Its gaps are an external gear's teeth, of the same teeth and pitch: their flanks are the same involutes,
from its base circle, and a gap's half width at radius `r` is a tooth's (`toothHalfAngle`), `90°/z + inv(α) -
inv(acos(r_b / r))`, less the offset over the base radius (`internalGapHalfAngle`: its profile offset grows its teeth
into its gaps). Its roots are `1.25 m` outside its pitch circle. Its ring's outside is a circle
(`internalRingRadius`): Ring thickness out from its roots, pitch circle, or tips (Measured from: Root, Pitch, or Tip
diameter, as a rack's, without the profile offset), or Outside diameter. It must be outside the root circle.

Its teeth are shaped for its pinion (Mating gear teeth, `internalMateTeeth`), whether or not Gear pair makes it
(`internalMesh`). Their line of action touches both base circles on the same side of the pitch point, the pinion's
`a sin α` nearer it than the gear's (for `a` the distance between their centers, the pitch radii's difference):

- **Tips** (`internalTipRadius`): a module inside its pitch circle, but no lower than where the line of action touches
  the pinion's base circle (its interference point), `√(r_b² + (a sin α)²)` from its center. Below it, its tips would
  reach the pinion below its base circle, where there's no involute, and clip it: a small pinion's undercut root (cut
  by a standard hob), or the radial flank pinions are often drawn with (as this feature's own are). A module in, a
  10T pinion's roots were clipped by about `0.04 m` at 20° (`0.11 m` at 14.5°), drawn so, or `0.013 m` (`0.033 m`)
  hobbed, as simulated (both turned through the mesh, and checked for overlap). No contact starts before the
  interference point, so stopping there loses none. It's never below the gear's own base circle (where it has no
  involute), which the interference point is outside.
- **Roots**: the pinion's tips touch its flanks out to `contactRadius`, `√(r_b² + (a sin α + √(r_a1² - r_b1²))²)`
  (where they cross the line of action), so its root fillets meet its flanks no lower (`internalRoot`, below). Its
  full round meets them lower only with a large pinion, at 14.5° (a 36T pinion in a 72T gear, by about `0.02 m`).
- **Fit** (`internalMeshInterferes`): an error if the pinion has as many teeth as the gear, or more, or with few teeth
  between them, their tips hit as they turn (the pinion's tips the gear's coming out of mesh: a 10T pinion in a 14T
  gear, or a 16T one at 14.5°). It's checked without the profile offset: each one's tip corners go into the other's
  teeth only on a short arc of their own tip circle, where it crosses the other's, so the corner's sampled along it
  (400 times), each point taken into the other's frame (the gears turned to put it there, the pinion the ratio as far,
  the same way), and checked against the tooth nearest it. The pinion's flanks are involutes down to its base circle,
  and radial below it, as a pinion drawn without undercut is (an undercut one has more room). It agrees with the
  simulation where it was run (10T pinions in 14T to 200T gears, 20T in 26T and 28T, and 30T in 40T, at both pressure
  angles).

- **Root** (`internalRoot`): Root fillet's types, as a gear's. The flank is an involute, whose normal at a point
  runs to its center of curvature on the base circle, on the gap's side (`internalFlankAt`): a circle tangent to it
  there is centered along that normal.
  - **Full** (`internalFullRound`): tangent to the root circle (inside it) at the gap's middle and to both flanks.
    The circle centered on the gap's middle and tangent to the flank at a point has a radius of the point's distance
    from the middle over the normal's, so it's found by halving the range of the point's roll angle (50 times) until
    the circle touches the root circle. It's an error if none does between the tip and root circles, or it's bigger
    than the flank's curvature where it touches.
  - **1/3, 1/4** (`internalFillet`): fillets of two thirds or half the full round's radius, tangent to the root circle
    and the flank, found likewise (the fillet's center, its radius along the normal from the flank, reaches the root
    circle less its radius), and the root circle between them. None is a sharp corner on the root circle.
  - **Where the pinion's tips touch**: a fillet (or the full round) meeting the flank below `contactRadius` would cut
    into where they mesh, so it's the fillet meeting the flank there instead, if that's smaller (`internalFilletAt`:
    for flank point `p` there, normal `n`, and root radius `R`, `r = (R² - |p|²) / (2 (p · n + R))`). A router bit's
    fillets are the bit's, wherever they meet it.
  - **Router bit**: fillets of the bit's radius (less the profile offset), where two fit (it's no bigger than the full
    round); or a slot (`internalSlot`), as a gear's: the bit's path straight out along the gap's middle, its round end
    on the root circle, its sides the bit's radius from the middle, running in to where the gap's as wide as the bit
    (found by halving the range of radii, 50 times: the gap only narrows outward), cutting into the flanks outside
    that. A bit only a little wider than the full round meets the flank on its round end first (found by looking along
    it at 32 points, then halving the range), and has no sides. A slot meeting the flanks inside the pitch circle is
    an error, shown as a gear's (`checkBitSlots`); a bit wider than the gap at the pitch circle is found first, as
    its round end can reach in past the tips, where the rest doesn't apply (its slot's points are then only its
    round end's center's side: nothing's built from them).
  - A helical gear's teeth's join to the rest of its ring (`buildInternalGearBody`) is in a `try`: Failed to create
    gear teeth., showing both.
- **Profile** (`internalHalfToothCurves`): per tooth (rotated copies of one centered on the plane's x axis, its other
  side mirrored), from the gap's middle on the root circle, the root (above) to the flank, the flank (a fit spline
  through 9 points) in to the tip (none, if a slot reaches it), and an arc on the tip circle. The ring is the region between
  them and the outside circle.
- **Body** (`buildInternalGearBody`): extruded, or swept along helices for a helical or herringbone gear, as a spur
  gear's (`buildGearBody`). A helical gear's teeth are swept in a ring of their own, out two thirds of the way from
  their roots to its outside, and joined to the rest of its ring, extruded from a third of the way out: swept, its
  outside would be a swept surface, split where the sweep's halves meet. They overlap, so they join with nothing left
  between them, and its outside is one face. An internal gear meshes with a pinion of the same hand.

### Bevel gears

`buildBevelGear` makes a straight bevel gear with spherical involute teeth and Gleason's proportions:

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
  - Its root's radial lines stop where its mate's tips touch its flanks (`contactRadius`, from their back
    cones' virtual spur gears, `lowestContactRadius`), so they don't relieve flank its mate touches; where a full round
    doesn't fit below that (a Gleason pinion's, whose teeth are long), it's the largest fillet that does, tangent to the
    root circle and the line at its top.
- **Router bit** (Root fillet's, `bevelRootFillet`): a ball end bit square to the root, running along it toward the
  apex, cuts the full round where it fits, and where it doesn't (toward the apex, as the gap narrows), a slot its width
  straight down the gap's middle to the root, as a spur gear's (`gearSlot`). Whether it fits is worked out at the gap's
  small end, at its pitch cone (`bevelBitRoot`: `gearRoot` at that scale, with the bit's radius), where it's narrowest;
  a slot meeting the flanks above the pitch cone there is an error, found once the blank's made, before its teeth are
  cut: the blank in blue, and the slot (below) patterned to each gap in red, built for the error. The slot is a U (its round bottom on the root line,
  its sides the bit's radius from the gap's middle, up past the tips) on the plane square to the root line through the
  gap's middle, extruded along it as far as the gap runs, and joined to the gap before it's patterned. Where the gap's
  wide, the slot's inside it.
- **Thickness** (`bevelShape`): its flat bottom is under its teeth's roots at its back (its dedendum down its back
  cone); its flat top, Thickness above it, square to its axis; its teeth run along its pitch cone from its back to its
  top (its face width). Its mate's teeth are as long (`mateDefinition` passes the face width on), so their ends meet,
  and its mate's top is wherever that reaches. The top must be above the teeth's tips at the back (the gear's and its
  mate's), and below the apex, and its face cone (and its mate's) under 90°.
- **Without Flat top** (`flatTop`, `bevelShape`): its teeth end at its front cone, square to its pitch cone, as its back
  cone is at its back, rather than cut off by its top. Their tips there are Thickness above its bottom (so its face
  width is how far along its pitch cone its tips rise that far up its face cone), and their roots there (its root runs
  straight from its back to its apex) are on its flat top face, a whole round face, which the teeth stand above. Its
  mate (with Gear pair) is without Flat top too, its teeth as long: a pair's front cones, at the same cone distance, meet
  along the line their teeth touch on, as their back cones do. Gleason's tips come down toward the apex, so a gear
  thick enough to nearly reach its apex can have none left at its front cone: an error.
- **Body**: its blank is revolved from its section through its axis (`bevelSection`): across its bottom, up its back
  cone to its face cone, along that toward the apex to its top (without Flat top, to its front cone, and down that to its
  teeth's roots, on its top face), and in to its axis across its top face. The gap runs a tooth's height past the
  blank at both ends (at the front, past where its root cone reaches the top face, or half way from there to the apex, if that's
  nearer), so it cuts through it. Without Flat top, what's in front of the front cone (from where it meets the axis, out
  past the tips and up past them, revolved) is taken from the gap. The gap's patterned around the axis once per tooth,
  and cut from the blank. Inside where the root cone reaches the top face, it's solid all the way through: its bore mustn't
  reach past that.
- **Meshing**: two bevel gears mesh with their apexes together and their axes the shaft angle apart, each made with
  the other's teeth as Mating gear teeth. Its mate connector is at its apex, along its axis, for mating them.

### Gear pairs

With Gear pair, the gear's mate (Mating gear teeth) is made too, meshing with it (an internal gear's, its pinion). A
bevel or internal gear without Gear pair is the same gear, without its mate:

- **Where** (`matePlacement`): by Teeth, toward the Angle reference from the gear's center (or along the gear's x axis
  without one), turned by the Angle (with its manipulator), as Robot motor turns a motor (`core/mounting.fs`). By
  Pitch circle, at the Mate pitch circle's center (on the gear's plane), with the nearest whole number of teeth to its
  size; it warns if the circle isn't a whole number of teeth's, or isn't the center to center distance from the gear's. A spur or helical mate's center is
  the gears' pitch radii away, and the center to center adjustment (`gearMesh`), on the gear's plane; an internal
  gear's pinion's, inside it, its pitch radius less the pinion's away (and the adjustment), toward the angle. A bevel mate shares the gear's apex, its axis the shaft angle from the gear's, its pitch
  cone touching the gear's toward the angle; its back is its cone distance times its pitch angle's cosine from the
  apex.
- **Its teeth** (`gearMatePlane`): where their pitch circles touch, a tooth of one meets a gap of the other. The gear's
  tooth nearest the touching point is the angle to it (less a whole number of teeth) from its first tooth, which is on
  its x axis. Turning
  the gear back by that angle turns the mate forward by it times the ratio, so the mate is turned that far from a gap
  facing the gear. Bevel gears turn the same way, each about its axis toward its apex. An internal gear's pinion is
  between its center and the touching point, so its x axis faces away from the gear's center, and it turns the same
  way as the gear: back by the angle times the ratio.
- **What it is**: a whole gear, of the opposite hand if helical (an internal gear's pinion, a spur gear of the same
  hand), with the gear's pitch, thickness (a bevel mate's face width), root fillet, and profile offset
  (`mateDefinition`), and the gear's bore, or with Use gear's bore off, its own (`definitionMateBore`, whose errors
  highlight its own parameters, `MATE_BORE_PARAMETERS`; an internal gear's pinion's is always its own). An internal gear's pinion's involutes reach down to where the gear's tips touch them
  (`contactRadius`, `internalMesh`'s `mateContactRadius`), as a bevel gear's do: to its base circle, with the gear's
  tips at its interference point (below). It's named for its
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

`cutTeeth` cuts teeth into the part that owns Face to cut: a rack along a flat face (`cutRack`), or a gear's teeth
around a round one (`cutRoundTeeth`, below). `cutRack`:

1. **The faces**: Face to cut, and the faces tangent-connected to it which are in its plane
   (`qTangentConnectedFaces`, `qCoincidesWithPlane`). Its direction is along the faces' longest straight edge
   (`longestEdgeDirection`). The part is found now: moved faces are new faces, which the selection doesn't find.
2. **Moved out**: at Pitch diameter, the faces are moved out a module, to the teeth's tips (`moveFacesOut`, as Move
   face's Offset), so the teeth are whole; at Root diameter, their whole depth, `2.25 m` (`rackFaceDepth`), so they're added on
   the face. The faces beside them stretch to meet them, so they're found again (the
   part's faces in the moved plane), and measured there: their length and width are their exact bounding box along
   the direction and across it.
3. **Where the teeth go** (`rackToothCenters`): a pitch (`π m`) apart, with a gap centered on the start (End), the end
   (Opposite end, also flipped by a manipulator there), or with Align teeth, the Angle reference (`rackReferenceAlong`:
   a point projected onto it, or where a line crosses the face's middle), moved along the face by the Angle as the
   teeth's phase (a turn is a pitch), with a linear manipulator from the reference. As many whole teeth
   as fit, root and all, fill it (so there's never a partial tooth at an end: past the first and last, it's cut to
   the root).
   A partial rack's (`rackStop`, `partialTeeth`) run from an end: End's (the start, or with Opposite end, the end),
   or with Align teeth, the end farther from the Stop reference, or without one, the end nearer the angle
   reference. By Teeth, they stop after Rack teeth; by Stop reference, where the gap after the last would run past
   the stop (the next tooth's root, `halfRoot` from its center, its far side): Stop distance along the face from the
   Stop reference (projected onto the face), or without one, from the end they start at, toward the far end, with a
   linear manipulator there, across the middle of the face. The next tooth after them is the stop: the face is left as
   it is from its near flank on. At Root diameter, there's no stop: past them, it's cut to the root, which is where the face
   was.
4. **What's cut** (`rackCutRegions`): everything between the face and the teeth. The face is `m` above the pitch line,
   at the tips. Flanks are straight at the pressure angle (a rack's involute: a gear's of infinite radius), and the
   root is `1.25 m` below the pitch line, both offset by the profile offset. Teeth reaching the face leave a region per
   gap. Teeth stopping short of it (with clearance) are cut over too, in one region. Past the first and last teeth,
   it's cut to the root; past a partial rack's stop, the region ends at the stop's near flank (up from its tip to the
   face, for teeth stopping short of it), and nothing past it is cut.
   The roots are rounded (`rackRootFilletRadius`, `relieveRackRoots`): each corner between the floor and a flank, by
   a fraction of the full round's radius (the full round's two fillets meet in the floor's middle: `w / 2 · tan(45° +
   α / 2)`, for the floor's width `w`), or a router bit's radius; a bit's, across a gap too narrow for both, a slot
   (`rackSlot`): its width straight down the gap's middle, square to the pitch line, its round bottom on the root, its
   sides cutting into the flanks up to where the gap's as wide as the bit (a round tangent to both flanks is above the
   root once the fillets overlap, so the bit couldn't reach the root). A slot meeting the flanks above the pitch line
   (`rackBitTooLarge`, worked out from the form) is an error, as a gear's is, found before anything's cut: the part
   (blue), and the bit's slots (red), built for the error with `rackTools` (given the slot's region, `rackSlotRegion`,
   rather than `rackCutRegions`), the one in the gap before the first tooth, patterned along the face to the gap after
   each. An end's round, on a floor too short
   for it, is cut off at the end.
5. The regions are sketched on a plane across the face's start (`sketchRackCut`), extruded through its width
   (`rackTools`), and cut from the part, each in a `try` (Failed to create rack teeth. showing the profile; Failed to
   cut rack teeth. showing the part in blue and what's cut). A helical rack's are extruded slanting along it at the helix angle (`rackPieces`), up to a plane at
   the far side, with its teeth where a straight rack's are at the middle of its width; a herringbone rack's in two
   halves slanting opposite ways. Its hand is the hand of the gear it stands in for, so like gears it meshes with the
   opposite hand: a left hand rack's teeth move along it going across its width (z), and a right hand rack's back.
   They're sketched past the face's ends as far as they slant, so the ends are cut to the root all the way across.

### Cutting teeth around a round face

`cutRoundTeeth` cuts a gear's teeth around Face to cut, a cylinder's face (and the tangent-connected faces on the same
cylinder: its axis, and its radius). A face inside a hole (its normal toward its
axis) is an error: that would be an internal gear's teeth.

1. **Its teeth**: the nearest whole number to its pitch circle's (the face, at Pitch diameter; a module inside it, at
   Tip diameter; `1.25 m` outside it, at Root diameter), with a warning if it isn't exactly that size.
2. **Moved out**: the faces are moved out to that gear's tip circle (`moveFacesOut`), unless they're there already,
   and found again (the part's faces on the same axis, at the tip radius): the faces beside them stretch to meet them,
   which can widen their angles.
3. **Its width and angles**: its width is its exact bounding box along the axis; its angles (`roundFacesSpan`), each
   face's from its middle as far as its vertices go either way, joined where they meet (`joinSpans`), or all the way
   around if it has circles for edges or they join all the way around.
4. **Where the teeth go**: a gap is centered on its start (End), its end (Opposite end, also flipped by a manipulator
   there), the Angle reference's angle (`rackReferenceAngle`: a point's, a line's direction, or a line along the axis,
   where it is) turned by the Angle, with an angular manipulator about the axis, or (all the way around, without Align
   teeth) the cylinder's x axis. Whole teeth, each from
   a gap's middle to the next's, fill its angles: a sector of them, or all of them. A partial rack's
   (`roundRackStop`) are chosen as a flat face's (`partialTeeth`, with angles), with its stop's tooth among them; all
   the way around, they start at the gap and go counterclockwise, with a stop before them too. Its Stop distance is
   along the pitch circle (an angle of it over the pitch radius), from the Stop reference's angle (or the end they
   start at, or all the way around, the gap), with an angular manipulator about the axis. At Root diameter, its stops are left
   out (`teethAngles`), and the ring's across all its angles (all the way around, a whole ring), so past its teeth
   it's cut to the root, where the face was.
5. **What's cut**: the teeth (`sketchGearProfile`, `buildGearBody`: helical and herringbone too) on the plane through
   the middle of its width, centered on the sector's middle (or a tooth), its router bit's slots checked first, as a
   gear's (`checkBitSlots`: its sector's teeth with sharp roots, and the slots in each of its gaps, from the one
   before its first tooth to the one after its last), cut from a ring around them: from their root
   circle out a module past their tips, across its angles (a partial rack's, from its start to its stop's middle, or
   between its stops' middles), through its width. That's cut from the part, so past the first and last teeth, it's
   cut to the root, and past a stop's middle, nothing's cut: its near half is a tooth's, its far half the face.

## Compared with Spur gear

Onshape's Spur gear (`spurGear.local.fs`) makes spur and helical gears, one tooth cut from a blank and patterned
around it.

| Spur gear option | Here | Why |
| --- | --- | --- |
| Module, diametral pitch, or circular pitch | Standard pitches (10 DP, 20 DP, 32 DP, module 0.8), or Custom: module, diametral pitch, or circular pitch | Robot gears come in a few standard pitches |
| Pitch circle diameter (tied to Teeth by editing logic) | Size by Teeth or Pitch circle, a top level option (a selected circle, with a warning if it's between sizes) | A circle places the gear too |
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

- A round face's whole teeth are chosen by its angles at its tips (its vertices, `roundFacesSpan`). A side which isn't
  radial (square to the face) crosses the last tooth lower down, cutting it partway; leaning the other way, it leaves
  a wedge past the cut to the root. A helical or herringbone sector's teeth turn toward one end and away from the other
  across the width, so its end teeth can be partial at the faces even with radial sides.
- The SplineXS bores at standard fits (`SPLINE_XS_BORES`), the partial rack's stop manipulators (a round face's
  especially), and the bore's errors showing the gear in blue are untried in Onshape.
- No undercut: on a pinion with few teeth (under about 17 at 20°), its mate's tips can reach below its base circle,
  where a generated gear would be undercut; its radial root relieves some of that, but not all.
- The icon is the generic robot icon.
- Round faces are cut on the outside of a part only: teeth inside a hole (an internal gear's) are an error. Internal
  gears are made whole instead, with their ring.
- Internal gears are tried in Onshape, but not since a helical one's ring was joined to its teeth's
  (`buildInternalGearBody`, as its outside was split), nor since their teeth were shaped for their pinion, nor with
  Gear pair (their pinion, its placement, bore, and mate connector are untried).
- An internal gear's fit with its pinion (`internalMeshInterferes`) is checked as they turn, not as the pinion's put in
  sideways (trimming interference, with few teeth between them): they go together along their axes. A router bit's
  fillets aren't kept below where the pinion's tips touch its flanks, as the other root fillets are.
- With Gear pair, an internal gear's pinion's bore is the bore's default, a 1/2 in. hex, which is too large for a
  small pinion (a 12T 20 DP one's roots are about 0.47 in. across): it warns.
- The router bit's slots too large (a gear's, rack's, and bevel gear's), the teeth failing to build or cut, and the
  bore bigger than the gear are untried in Onshape, as are the mate's own bore and its errors.
- Root fillets' full round is tangent to the radial lines below the involutes (as the router bit's is), not to the
  involutes themselves, as Onshape's Spur gear's is.
- Tried in Onshape (from the browser): a bevel pair, its too thin error, a spur gear on its bottom, a flat rack, and
  round teeth on an arc face at Outside and Pitch diameter. Untried: the bevel's apex point and other shaft angles, a
  flat rack at Pitch diameter, helical round teeth, and the bore warning.
- Helical gears sweep the whole profile along each helix; Onshape's Spur gear sweeps one tooth. Untried in Onshape.
- Helical racks aren't cut.
- Partial racks are untried in Onshape, flat and round. A helical one's stop is where it says at the middle of the
  face's width; across it, its teeth (and the stop) slant past it. A stop is only as tall as the face: a mating gear
  stops when its tooth meets it, so the stop takes the load.
- Spherical bevel gears are untried in Onshape: their blank's face cone, the gap's loft between the projected
  sketches, and cutting it. Their meshing is tested on the sphere (`fs_eval_bevel_test.py`), from the form's flanks.
- Bevel gears' teeth aren't crowned, and they have no helical (spiral) teeth, 1/3, 1/4, or no root fillets, or check of their
  face width against a third of the cone distance (Gleason's most). Gleason's tooth thickness factor `K` (a little
  more thickness for the pinion, from a chart) is left out.
- Gleason's proportions are for a 20° pressure angle; at 20 DP's 14.5°, Gleason's least teeth to avoid undercut are
  far more (24 or more on the pinion), and nothing warns of it (see No undercut, above).
- A bevel gear's router bit slot is untried in Onshape: its round bottom is tangent to the gap's full round along the
  root line where the bit fits, which a boolean may not like. The slot's error shows nothing (only Bit diameter).
- Spur gears' radial lines (as a bevel gear's were) can run above where their mate's tips touch them, relieving flank
  the mate touches: on a 24T pair, from about `r - 0.6 m` up to `r - 0.125 m`, up to `0.1 m` deep (found testing bevel
  gears' meshing). A spur gear knows its mate only in a pair (or for a rack, a rack's), so it's left as it is.
- Router bit slots, and their error bodies, are untried in Onshape. A slot is allowed to cut a flank below the pitch
  circle, which is where a mating gear's tips touch on the lower flank: a bit nearly as wide as the gap there takes
  some contact away. A gear's error shows its own teeth (or a round face's), not the part; a rack's shows the part
  cut.
- REV's MAXSpline 20 DP gears' pressure angle isn't published; FTC gears besides goBILDA's (REV's, say) aren't among
  the standard pitches.
- Its regions are flush with the face, its ends, and its sides: booleans treat faces within their tolerance as the
  same, but it's untried in Onshape.
- Past the last whole tooth, the rest of the face is cut to the root, rather than left as a partial tooth.
