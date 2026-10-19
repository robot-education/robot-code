# Robot gear

Makes involute spur, helical, and herringbone gears or sectors of them, straight bevel gears, or cuts rack teeth into a
part's flat face, or a gear's teeth around its round one (Gear type: Gear, with Sector for a sector of one, Rack, or
Bevel). All are at a standard pitch (10 DP for 3D printing, 20 DP or 32 DP for FRC, module 0.8 for FTC), or a custom
diametral pitch (Inch) or module (Metric), with standard proportions. A gear is sized by its teeth or a pitch circle,
sits on its location by its bottom (or middle, top, or a bevel gear's apex, by a point manipulator), and with Gear
pair, the gear it meshes with is made too. Its roots are fully rounded (or, as Onshape's Spur gear's Root fillet, a
third or a quarter of the gap's width, or sharp; or a router bit's radius), and it can have a bore.

| File | What it is |
| --- | --- |
| `robotGear.fs` | The feature: its dialog, the gear and its mate (`buildGear`, `gearMesh`, `gearMatePlane`, `bevelPairCones`), and cutting a rack (`cutRack`) |
| `gearCommon.fs` | The tooth form (`getGearForm`), its roots (`gearRoot`, `halfGapCurves`), a gear's profile (`sketchGearProfile`), a bevel gear's cones and tooth gap (`bevelCones`, `sketchToothGap`), and a cut rack's teeth and regions (`rackToothCenters`, `rackCutRegions`, `rackRootFilletRadius`, `relieveRackRoots`, `sketchRackCut`) |
| `../core/bore.fs` | The bore: its parameters, and cutting it |
| `../core/stdRevolve.fs` | A sector's angles by Angle, as std's revolve has them (`revolveAnglePredicate`, `revolveAngles`) |
| `../core/location.fs`, `../core/startOffset.fs`, `../core/profileOffset.fs` | The location, starting offset, and profile offset |
| `../core/pointManipulator.fs` | Where the gear sits on its location, as Robot pulley's (`pointManipulatorPredicate`, `applyPointManipulator`) |
| `../core/mounting.fs` | The gear pair's Angle reference and Angle, as Robot motor's (`angleReferencePredicate`, `angleOffsetPredicate`, `applyAngleReference`, `applyAngleOffset`), and the rack's angle reference's point (`angleReferencePoint`) |
| `../../tests/fs_eval_sketch_test.py` | Tests of gears' profiles and root fillets, the standard pitches, pitch circles' teeth, bevel gaps, and where a rack's teeth go and what's cut |
| `../../tests/featurescript/gear_test.fs` | Tests of where a gear's mate goes (spur, sector, and bevel), bevel gears' crown gear and thickness errors, their shape and points, and joining round faces' angles |
| `spurGear.local.fs` | Onshape's Spur gear, for reference (see Compared with Spur gear): not synced |

## Changelog

Unreleased.

- Sector is a checkbox on Gear, rather than a Gear type: sized by Sector teeth, or an angle as std's revolve gives one
  (Angle, Opposite direction, Symmetric, Second end position), from an Angle reference.
- Gear type's Bevel is last. Size by is Selection, an unlabeled dropdown, with the starting offset under it.
- Gear pair: the gear it meshes with, placed by Mating gear teeth, Angle reference, and Angle, or by its pitch circle.
  A bevel gear is always made with its mate.
- Helical: Double helical is above Hand, which it hides.
- Racks can be helical or double helical, and have router relief.
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
| `<teeth>`T `<pitch>` `<kind>`: the pitch is like 20DP or Module 0.8, and the kind Gear, Sector Gear, or Bevel Gear, after Helical or Herringbone if it's helical | part name |

A gear's material is Onyx, and its appearance printed green. A cut rack leaves its part's properties as they are.

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Pitch circle (`pitchCircle`) | A circle or arc: its center places the gear, and its size sets its teeth. A sector is centered on an arc. |
| Teeth (`teeth`) | The whole gear's. |
| Sector (`sector`) | Make a wedge of the gear, from its center. |
| Sector teeth (`sectorTeeth`) | The sector's: a wedge of the gear's, from its center. |
| Angle reference (`sectorReference`) | Where the sector's middle points (by teeth), or its angles are measured from. |
| Diametral pitch (`diametralPitch`) | Teeth per inch of pitch diameter. |
| Module (`module`) | Pitch diameter per tooth. |
| Helical (`helical`) | Teeth on a helix: quieter and smoother, but they push the gear along its shaft. |
| Hand (`hand`) | Meshing helical gears have opposite hands. |
| Double helical (`doubleHelical`) | Herringbone: two halves of opposite hands, so they don't push along the shaft. |
| Gear pair (`gearPair`) | Also make the gear it meshes with. |
| Mating gear teeth (`mateTeeth`) | The gear it meshes with. |
| Mate pitch circle (`matePitchCircle`) | Its center places the mate, and its size sets its teeth. It should touch the gear's. |
| Shaft angle (`shaftAngle`) | The angle the gears turn the drive through: between its shaft and its mate's (90 degrees for a right angle). |
| Thickness (`thickness`) | Along its axis, from its bottom to its top. A bevel gear's teeth run from its back as far toward its apex as it reaches. |
| Root fillet (`rootFillet`) | A full round, a third or a quarter of the gap's width, none, or a router bit's radius, so it can cut them (a full round where the gap's too narrow for both corners). |
| Face to cut (`rackFace`) | A flat face, for a rack: its teeth run along its longest edge, through its width. Or a round one, for a gear's teeth around it. |
| Tangent propagation (`tangentPropagation`) | Also cut the faces in line with it (or on the same cylinder), across tangent edges. |
| Face at (`rackFaceAt`) | The teeth's pitch line (the face is moved out to their tips first), or their tips. |
| Angle reference (`rackReference`) | A point: a gap is centered on it, as on an end. |
| (the bore's, as Robot sprocket's) | |

| Hidden parameter | Set by |
| --- | --- |
| Index (`index`) | The point manipulator: where the gear sits on its location (0, its bottom, by default; 1, its middle; 2, its top; 3, a bevel gear's apex). |

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select a sketch point, circle, or mate connector to use. | error | no location | `location` |
| Select a pitch circle. | error | Size by Pitch circle, with none | `pitchCircle` |
| The pitch circle is too small for a gear of this pitch. | error | it's under 4 teeth's | `pitchCircle`, the circle |
| The pitch circle isn't the size of a gear with a whole number of teeth: it's the nearest, with `<teeth>` teeth. | warning | its diameter isn't a whole number of modules (within std's length tolerance) | `pitchCircle` |
| A sector has fewer teeth than its gear. | error | Sector, with Sector teeth the gear's teeth or more (or by Angle, all the way around) | `sectorTeeth` |
| The sector's angle is too small for a whole tooth. | error | Sector by Angle, spanning less than a tooth | `angle`, `angleBack` |
| At this shaft angle, it would be a crown or internal gear: use a smaller shaft angle, or more mating teeth. | error | a bevel gear's pitch cone is 90° or more | `shaftAngle`, `mateTeeth` |
| At this shaft angle, its mate would be a crown or internal gear: use a smaller shaft angle, or fewer mating teeth. | error | its mate's pitch cone is 90° or more (with or without Gear pair: the pair is what it's made for) | `shaftAngle`, `mateTeeth` |
| The angle reference is on the axis, so it doesn't give an angle. / The angle reference must be parallel to the location's plane. / The angle reference must be a point, circle, or line. | error | Gear pair's Angle reference (`core/mounting.fs`) | the location or `pitchCircle`, `angleReference` |
| The gear's too thin for its teeth: make it thicker than `<thickness>`. | error | a bevel gear's top is below its teeth's tips at its back | `thickness` |
| The gear's too thick: its top would reach its apex. Make it thinner than `<thickness>`. | error | a bevel gear's top is at its apex or past it | `thickness` |
| The gear's too thin for its mate's teeth: make it thicker than `<thickness>`. | error | a bevel gear's mate's teeth, as long as its, don't reach the mate's top | `thickness` |
| A router can't cut a bevel gear's teeth: choose another root fillet. | error | Bevel, with Root fillet Router bit | `rootFillet` |
| The gear's teeth come to points: give it more teeth, or less clearance. / The gear's teeth meet at their roots: use more clearance. | error | a bevel gear's virtual teeth do | `teeth` / `profileOffsetDistance` |
| Failed to create the bevel gear's teeth. / Failed to cut the bevel gear's teeth. | error | the gap's loft, or cutting the gaps from the blank, fails | `teeth`, `mateTeeth`, `thickness`, and the gap's sketches or the gaps |
| The gear's teeth come to points: give it more teeth, or less clearance. | error | a tooth's flanks meet below the tip circle | `teeth` |
| The gear's too small for its teeth: give it more teeth. | error | the root circle's at or below the center | `teeth` |
| The profile offset is bigger than the roots' round: use less clearance, or a bigger router bit. | error | the profile offset leaves no root fillet | `profileOffsetDistance` |
| The root's round doesn't fit between the teeth: use a smaller router bit, or a bigger module. | error | not even a full round of the fillet's radius fits below the involutes | `bitDiameter` |
| The root's full round doesn't fit below the teeth's flanks: use a smaller root fillet. | error | the full round would meet the radial lines above where they meet the involutes | `rootFillet` |
| Select a face to cut the teeth into. | error | Rack, no face | `rackFace` |
| Failed to move the face out to the teeth's tips. | error | Face at Pitch diameter (or a round face not at its tips), and moving the faces fails | `rackFace`, `rackFaceAt`, the faces |
| Teeth can't be cut inside a hole yet: select a round face on the outside of a part. | error | a round face whose part is inside it | `rackFace`, the face |
| The face is too small for a gear of this pitch. | error | a round face under 4 teeth's | `rackFace`, the face |
| The face isn't the size of a gear with a whole number of teeth: it's cut as the nearest, with `<teeth>` teeth. | warning | a round face's pitch circle (its own, or a module inside it at Outside diameter) isn't a whole number of teeth's | `rackFace` |
| The angle reference is on the axis, so it doesn't give an angle. | error | a round face's Angle reference, on its axis | `rackReference`, the reference |
| The face needs a straight edge for the teeth to run along. | error | none of the faces' edges is a line | `rackFace`, the face |
| Select an angle reference. / The angle reference must be a point. | error | a rack's Angle reference, with none, or a selection that isn't a point | `rackReference` |
| The face is too short for a whole tooth. | error | no whole tooth fits along it | `rackFace`, the faces |
| The rack's teeth come to points: use less clearance. / The rack's teeth meet at their roots: use more clearance. | error | the profile offset's too far | `profileOffsetDistance` |
| Its pitch diameter is `<diameter>`. | info | a spur gear (not a bevel gear), without a warning | |
| ... The gears' centers are `<distance>` apart. | info | added to a spur pair's | |
| The bore cuts into the gear's teeth: use a smaller bore, or more teeth. / The bore cuts into the mate's teeth: ... (a bevel gear's: ..., or a thinner gear.) | warning | the bore's corners (and entrance chamfer) reach its root, or a bevel gear's root cone at its top, without an earlier warning | `hexWidth`, `boreDiameter`, `splineType`, `teeth`, `thickness` |
| Select the mate's pitch circle. | error | Mate selection Pitch circle, with none | `matePitchCircle` |
| The mate's pitch circle must be parallel to the gear's. / The mate's pitch circle is too small for a gear of this pitch. / The mate's pitch circle is centered on the gear. | error | likewise | `matePitchCircle`, the circle |
| The mate's pitch circle isn't the size of a gear with a whole number of teeth: it's the nearest, with `<teeth>` teeth. / The pitch circles don't touch, so the gears don't mesh. | warning | likewise (the gear's own pitch circle's warning comes first) | `matePitchCircle` |
| The root's round doesn't fit between the teeth: use a smaller router bit, or a bigger module. | error | a rack's Router bit: its corner rounds run past the flanks, or its full round stops above where a mating gear's tips reach | `bitDiameter` |
| Failed to cut bore. / The bore is bigger than the part. / Failed to chamfer bore. | error | (`core/bore.fs`) | the bore's parameters, and what failed |

## How it works

### Execution order

1. **Precondition**: Gear type (Gear, Rack, or Bevel).
   - Gear and Bevel: Selection (`sizeBy`, unlabeled: Teeth or Pitch circle), then the location or Pitch
     circle, and the starting offset under it; Gear pair (a bevel gear's always on, with no checkbox: its cones depend
     on its mate), with Mate selection (Teeth or Pitch circle, unlabeled; a bevel mate's by teeth) and either Mating
     gear teeth (and a bevel gear's Shaft angle), Angle reference, and Angle (and its Opposite direction), or Mate
     pitch circle; Pitch (with Custom, Diametral pitch or Module by unit system, and Pressure angle), Teeth (by
     Teeth), Thickness, and Root fillet (with Router bit, Bit diameter); a gear's Sector, with Sector size (Teeth or Angle,
     unlabeled), Angle reference, and Sector teeth or std revolve's angles (Angle, Opposite direction, Symmetric, and
     Second end position with Start angle); bore.
   - Rack: Face to cut, Tangent propagation, Face at, Align teeth to (End, with Opposite end, or Angle
     reference); Pitch (and with Custom, its pitch and Pressure angle), and Root fillet.
   - All but Bevel: Helical, with Helix angle, Double helical, and Hand (hidden for Double helical: a herringbone
     turned over is the other hand). Then the profile offset, a gear's Add mate connector, and the hidden `index`.
2. **Body**: the module and pressure angle (a standard pitch's, see `GEAR_PITCHES`, or Custom's: an inch over the
   diametral pitch), and the tooth form (`getGearForm`: pitch radius `m z / 2`,
   base radius its cosine of the pressure angle, tip `m` above it and root `1.25 m` below, all offset by the profile
   offset, and the root fillet: a fraction of the full round's radius, `ROOT_FILLET_FRACTIONS`, or the bit's radius).
   Then:
   - Gear (or a sector): its teeth and plane (`gearSize`: Teeth at the location, or the nearest whole number of teeth to
     the pitch circle, `nearestGearTeeth`, at its center, with its x axis to an arc's middle), after the starting
     offset; its points along its axis (`gearPointDistances`: its bottom, middle, and top), with the point
     manipulator, which moves the gear so the chosen point is on the location (`applyPointManipulator`); the profile
     (below), its body (`buildGearBody`, below), its properties, a profile offset manipulator, mate connector, its bore
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
  lines, above the root circle.
- **Tip**: an arc on the tip circle.

A sector's teeth are centered on the x axis, and its ends run from the middle of the gaps at its ends to the center;
only its teeth are sketched, swept, and cut. `sectorPlacement` turns the plane first: by Sector teeth, so its middle
points toward the Angle reference (or a pitch arc's middle, or the location's x axis, without one); by Angle, to the
middle of the angles from it (`revolveAngles`, as std's revolve: Angle that way, or the other with Opposite direction;
half each way with Symmetric; and back to Start angle with a second end position), with as many whole teeth as fit
(`sectorSpan`). A gear pair's mate, without its own angle reference, goes toward the sector's middle.

### Bevel gears

`buildBevelGear` makes a straight bevel gear by Tredgold's approximation (`bevelCones`), the usual one for
straight bevels:

- **Cones**: its pitch cone's angle from its axis is `atan(sin Σ / (z₂ / z + cos Σ))`, for its teeth `z`, its mate's
  `z₂`, and shaft angle `Σ` (`atan(z / z₂)` at 90°). Its pitch circle (radius `m z / 2`) is at its back, on the
  location's plane, and its apex is the cone distance (the pitch radius over the pitch angle's sine) along its
  pitch cone, in front of it.
- **Teeth**: a bevel gear's teeth, seen on its back cone (the cone square to its pitch cone at its back), are close
  to a spur gear's whose pitch radius is the back cone's: the pitch radius over the pitch angle's cosine, with
  `z / cos(pitch angle)` teeth (its virtual teeth, not a whole number). The gap between two of them
  (`sketchToothGap`) is sketched on the plane square to the pitch cone at its first tooth, and lofted to the same gap
  scaled toward the apex: scaled about the apex, its flanks run straight to it, as a straight bevel's do.
- **Thickness** (`bevelShape`): its flat bottom is under its teeth's roots at its back (`1.25 m` down its back cone);
  its flat top, Thickness above it, square to its axis; its teeth run along its pitch cone from its back to its top
  (its face width). Its mate's teeth are as long (`mateDefinition` passes the face width on), so their ends meet,
  and its mate's top is wherever that reaches. The top must be above the teeth's tips at the back (the gear's and its
  mate's), and below the apex.
- **Body**: its blank is revolved from its section through its axis: across its bottom, up its back cone to its tip
  cone, along that toward the apex to its top, and in to its axis. The gap runs a tooth's height past the blank at
  both ends (at the front, past where its root cone reaches the top, or half way from there to the apex, if that's
  nearer), so it cuts through it; it's patterned around the axis once per tooth, and cut from the blank. Inside where
  the root cone reaches the top, it's solid all the way through: its bore mustn't reach past that.
- **Meshing**: two bevel gears mesh with their apexes together and their axes the shaft angle apart, each made with
  the other's teeth as Mating gear teeth. Its mate connector is at its apex, along its axis, for mating them.

Its gaps' roots are rounded as a spur gear's are (`halfGapCurves`), by Root fillet's fraction: the shapes scale with
the gap, so both ends of the loft match. A router bit can't cut them.

### Gear pairs

With Gear pair, the gear's mate (Mating gear teeth) is made too, meshing with it:

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

1. **The faces**: Face to cut, and with Tangent propagation, the faces tangent-connected to it which are in its plane
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
   α / 2)`, for the floor's width `w`), or a router bit's radius; a bit's, across a gap too narrow for both, a full
   round tangent to both flanks (which a bit can't cut below, so it's above the root, and must be below where a
   mating gear's tips reach). An end's round, on a floor too short for it, is cut off at the end.
5. The regions are sketched on a plane across the face's start (`sketchRackCut`), extruded through its width, and cut
   from the part. A helical rack's are extruded slanting along it at the helix angle (`rackPieces`), up to a plane at
   the far side, with its teeth where a straight rack's are at the middle of its width; a double helical rack's in two
   halves slanting opposite ways. Its hand is the hand of the gear it stands in for, so like gears it meshes with the
   opposite hand: a left hand rack's teeth move along it going across its width (z), and a right hand rack's back.
   They're sketched past the face's ends as far as they slant, so the ends are cut to the root all the way across.

### Cutting teeth around a round face

`cutRoundTeeth` cuts a gear's teeth around Face to cut, a cylinder's face (with Tangent propagation, and the
tangent-connected faces on the same cylinder: its axis, and its radius). A face inside a hole (its normal toward its
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
   the middle of its width, centered on the sector's middle (or a tooth), cut from a ring around them: from their root
   circle out a module past their tips, across its angles, through its width. That's cut from the part, so past the
   first and last teeth, it's cut to the root.

## Compared with Spur gear

Onshape's Spur gear (`spurGear.local.fs`) makes spur and helical gears, one tooth cut from a blank and patterned
around it.

| Spur gear option | Here | Why |
| --- | --- | --- |
| Module, diametral pitch, or circular pitch | Standard pitches (10 DP, 20 DP, 32 DP, module 0.8), or Custom: diametral pitch (Inch) or module (Metric) | Robot gears come in a few standard pitches; circular pitch is rarely used |
| Pitch circle diameter (tied to Teeth by editing logic) | Size by Teeth or Pitch circle (a selected circle, with a warning if it's between sizes) | A circle places the gear too |
| Pressure angle | With the standard pitch; Custom only | 20 DP robot gears are 14.5°, which is easy to miss |
| Root fillet (none, 1/4, 1/3, full; 1/3 by default) | The same, full by default, or a router bit's radius | Full rounds are strongest; a router bit's is what can be machined |
| Backlash | Profile offset | |
| Dedendum (1.157, 1.2, or 1.25 times the module), root and tip offsets | Left out: always `1.25 m`, and `m` | Standard full depth teeth; the profile offset covers clearance |
| Clocking angle | Left out: a tooth's on the location's x axis | Mate connectors and the location place it |
| Helical: angle, direction, double helix | Helical: Helix angle, Hand, Double helical | Hands are how helical gears are specified |
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
- Bevel gears use Tredgold's approximation (the back cone's spur teeth, lofted to the apex), not spherical involutes:
  close for robot use and 3D printing, not for precision gearing. Their teeth aren't crowned, and they have no router
  bit roots, helical (spiral) teeth, or check of their face width against a third of the cone distance (the usual
  most).
- REV's MAXSpline 20 DP gears' pressure angle isn't published; FTC gears besides goBILDA's (REV's, say) aren't among
  the standard pitches.
- Its regions are flush with the face, its ends, and its sides: booleans treat faces within their tolerance as the
  same, but it's untried in Onshape.
- Past the last whole tooth, the rest of the face is cut to the root, rather than left as a partial tooth.
