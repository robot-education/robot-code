# Robot gear

Makes involute spur, helical, and herringbone gears or sectors of them, straight bevel gears, or cuts rack teeth into a
part's face (Gear type: Gear, Sector, Rack, or Bevel). All are at a standard pitch (10 DP for 3D printing, 20 DP or 32 DP for FRC, module 0.8
for FTC), or a custom diametral pitch (Inch)
or module (Metric), with standard proportions. A gear is sized by its teeth or a pitch circle, and with Gear pair, the
gear it meshes with is made too. It has root fillets, optionally roots a router bit can cut, and can have a bore.

| File | What it is |
| --- | --- |
| `robotGear.fs` | The feature: its dialog, the gear and its mate (`buildGear`, `gearMesh`, `gearMatePlane`, `bevelPairCones`), and cutting a rack (`cutRack`) |
| `gearCommon.fs` | The tooth form (`getGearForm`), a gear's profile (`sketchGearProfile`), a bevel gear's cones and tooth gap (`bevelCones`, `sketchToothGap`), and a cut rack's teeth and regions (`rackToothCenters`, `rackCutRegions`, `sketchRackCut`) |
| `../core/bore.fs` | The bore: its parameters, and cutting it |
| `../core/location.fs`, `../core/startOffset.fs`, `../core/profileOffset.fs` | The location, starting offset, and profile offset |
| `../core/mounting.fs` | The gear pair's Angle reference and Angle, as Robot motor's (`angleReferencePredicate`, `angleOffsetPredicate`, `applyAngleReference`, `applyAngleOffset`), and the rack's angle reference's point (`angleReferencePoint`) |
| `../../tests/fs_eval_sketch_test.py` | Tests of gears' profiles, the standard pitches, pitch circles' teeth, and where a rack's teeth go and what's cut |
| `../../tests/featurescript/gear_test.fs` | Tests of where a gear's mate goes (spur, sector, and bevel), and bevel gears' crown gear errors |
| `spurGear.local.fs` | Onshape's Spur gear, for reference (see Compared with Spur gear): not synced |

## Changelog

Unreleased.

- Gear type's Bevel is last; Size by is a dropdown.
- Gear pair: the gear it meshes with, placed by Angle reference and Angle.
- Bevel gears check their mate's pitch cone too: a shaft angle where the mate would be a crown or internal gear is an
  error.

## Strings

| String | Where |
| --- | --- |
| Robot gear | feature name |
| Create spur, helical, and herringbone gears, or cut racks into parts, at FRC and FTC gears' standard pitches. `<CREDIT>` | feature description |
| `<teeth>`T `<pitch>` `<kind>`: the pitch is like 20DP or Module 0.8, and the kind Gear, Sector Gear, or Bevel Gear, after Helical or Herringbone if it's helical | part name |

A gear's material is Onyx, and its appearance printed green. A cut rack leaves its part's properties as they are.

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Pitch circle (`pitchCircle`) | A circle or arc: its center places the gear, and its size sets its teeth. A sector is centered on an arc. |
| Teeth (`teeth`) | The whole gear's. |
| Sector teeth (`sectorTeeth`) | The sector's: a wedge of the gear's, from its center. |
| Diametral pitch (`diametralPitch`) | Teeth per inch of pitch diameter. |
| Module (`module`) | Pitch diameter per tooth. |
| Helical (`helical`) | Teeth on a helix: quieter and smoother, but they push the gear along its shaft. |
| Hand (`hand`) | Meshing helical gears have opposite hands. |
| Double helical (`doubleHelical`) | Herringbone: two halves of opposite hands, so they don't push along the shaft. |
| Gear pair (`gearPair`) | Also make the gear it meshes with. |
| Mating gear teeth (`mateTeeth`) | The gear it meshes with. |
| Shaft angle (`shaftAngle`) | Between its shaft and its mate's. |
| Face width (`faceWidth`) | Its teeth's length, from its back toward its apex. |
| Face to cut (`rackFace`) | The teeth run along its longest edge, cut through its width. |
| Tangent propagation (`tangentPropagation`) | Also cut along the flat faces in line with it, across tangent edges. |
| Face at (`rackFaceAt`) | The teeth's pitch line (their tips are cut off at the face), or their tips. |
| Angle reference (`rackReference`) | A point: a gap is centered on it, as on an end. |
| Router relief (`routerRelief`) | Round the roots to a router bit's radius, so it can cut them (a full round, deeper than the root, where the gap's too narrow). |
| (the bore's, as Robot sprocket's) | |

No parameters are hidden.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select a sketch point, circle, or mate connector to use. | error | no location | `location` |
| Select a pitch circle. | error | Size by Pitch circle, with none | `pitchCircle` |
| The pitch circle is too small for a gear of this pitch. | error | it's under 4 teeth's | `pitchCircle`, the circle |
| The pitch circle isn't the size of a gear with a whole number of teeth: it's the nearest, with `<teeth>` teeth. | warning | its diameter isn't a whole number of modules (within std's length tolerance) | `pitchCircle` |
| A sector has fewer teeth than its gear. | error | Sector teeth is the gear's teeth or more | `sectorTeeth` |
| At this shaft angle, it would be a crown or internal gear: use a smaller shaft angle, or more mating teeth. | error | a bevel gear's pitch cone is 90° or more | `shaftAngle`, `mateTeeth` |
| At this shaft angle, its mate would be a crown or internal gear: use a smaller shaft angle, or fewer mating teeth. | error | its mate's pitch cone is 90° or more (with or without Gear pair: the pair is what it's made for) | `shaftAngle`, `mateTeeth` |
| The angle reference is on the axis, so it doesn't give an angle. / The angle reference must be parallel to the location's plane. / The angle reference must be a point, circle, or line. | error | Gear pair's Angle reference (`core/mounting.fs`) | the location or `pitchCircle`, `angleReference` |
| The face width reaches the gear's apex: make it narrower. | error | Face width is the cone distance or more | `faceWidth` |
| The gear's teeth come to points: give it more teeth, or less clearance. / The gear's teeth meet at their roots: use more clearance. | error | a bevel gear's virtual teeth do | `teeth` / `profileOffsetDistance` |
| Failed to create the bevel gear's teeth. / Failed to cut the bevel gear's teeth. | error | the gap's loft, or cutting the gaps from the blank, fails | `teeth`, `mateTeeth`, `faceWidth`, and the gap's sketches or the gaps |
| The gear's teeth come to points: give it more teeth, or less clearance. | error | a tooth's flanks meet below the tip circle | `teeth` |
| The gear's too small for its teeth: give it more teeth. | error | the root circle's at or below the center | `teeth` |
| The profile offset is bigger than the roots' round: use less clearance, or a bigger router bit. | error | the profile offset leaves no root fillet | `profileOffsetDistance` |
| The root's round doesn't fit between the teeth: use a smaller router bit, or a bigger module. | error | not even a full round of the fillet's radius fits below the involutes | `bitDiameter` |
| Select a face to cut the rack into. | error | Rack, no face | `rackFace` |
| The face needs a straight edge for the teeth to run along. | error | none of the faces' edges is a line | `rackFace`, the face |
| Select an angle reference. / The angle reference must be a point. | error | a rack's Angle reference, with none, or a selection that isn't a point | `rackReference` |
| The face is too short for a whole tooth. | error | no whole tooth fits along it | `rackFace`, the faces |
| The rack's teeth come to points: use less clearance. / The rack's teeth meet at their roots: use more clearance. | error | the profile offset's too far | `profileOffsetDistance` |
| Its pitch diameter is `<diameter>`. | info | a gear, without the pitch circle's warning | |
| Its pitch diameter is `<diameter>`, and its apex (where its mate's goes) is `<distance>` in front of its back. | info | a bevel gear, likewise | |
| ... The gears' centers are `<distance>` apart. | info | added to a spur pair's | |
| Failed to cut bore. / The bore is bigger than the part. / Failed to chamfer bore. | error | (`core/bore.fs`) | the bore's parameters, and what failed |

## How it works

### Execution order

1. **Precondition**: Gear type (Gear, Sector, Rack, or Bevel).
   - Gear, Sector, and Bevel: Size by (Teeth or Pitch circle, a dropdown), then the location or Pitch circle; Gear
     pair, with Angle reference and Angle (and its Opposite direction); Pitch (with Custom,
     Diametral pitch or Module by unit system, and Pressure angle), Teeth (by Teeth), Mating gear teeth (a bevel gear's, or with Gear
     pair), a sector's Sector teeth, and Thickness (a bevel gear's Shaft angle and Face width instead); a spur gear's or sector's
     Helical, with Helix angle, Hand, and Double helical, and Router relief and Bit diameter; starting offset; bore;
     profile offset; Add mate connector.
   - Rack: Face to cut, Tangent propagation, Face at, Align teeth to (End, with Opposite end, or Angle
     reference); Pitch (and with Custom, its pitch and Pressure angle); profile offset. (A rack's roots are square:
     there's no round for Router relief.)
2. **Body**: the module and pressure angle (a standard pitch's, see `GEAR_PITCHES`, or Custom's: an inch over the
   diametral pitch), and the tooth form (`getGearForm`: pitch radius `m z / 2`,
   base radius its cosine of the pressure angle, tip `m` above it and root `1.25 m` below, fillet `0.38 m` or the bit's
   radius, all offset by the profile offset). Then:
   - Gear and Sector: its teeth and plane (`gearSize`: Teeth at the location, or the nearest whole number of teeth to
     the pitch circle, `nearestGearTeeth`, at its center, with its x axis to an arc's middle), the profile (below), its body (`buildGearBody`, below), its
     properties, a profile offset manipulator, mate connector, its bore (`cutBores`), and the pitch circle's warning
     or its pitch diameter as info.
   - Bevel: its teeth and plane likewise, then `buildBevelGear` (below), its properties, a mate connector at its
     apex, its bore, and its pitch diameter and apex as info.
   - Gear pair: its mate, built the same way (`buildGear`) on the plane `gearMatePlane` gives (below), with its own
     mate connector and bore, and an angle manipulator.
   - Rack: `cutRack` (below).
3. **Manipulator change** (`robotGearManipulatorChange`): the rack's end flip, or the angle's, profile offset's, and
   starting offset's.

A gear's profile (`sketchGearProfile`), per tooth (rotated copies of one centered on the plane's x axis, its other
side mirrored):

- **Flanks**: involutes, as fit splines through 9 points, from the top of a radial line up to the tip. A tooth's half
  angle at radius `r` is `90°/z + inv(α) - inv(acos(r_b / r))` (plus the offset over the base radius: an involute
  offset along its normal is the same involute rotated).
- **Root**: the radial line runs down from the base circle (or higher, up to half the tooth's height, to make room for
  the fillet; there it's a little inside the involute, relieving it), and a fillet joins it to the root circle; if two
  fillets don't fit in the gap, it's a full round of the fillet's radius tangent to both teeth's lines, deeper than the
  root circle.
- **Tip**: an arc on the tip circle.

A sector's teeth are centered on the x axis (so on a pitch arc's middle), and its ends run from the middle of the gaps
at its ends to the center.

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
- **Body**: its blank is revolved from its section through its axis: between its root and tip cones, from its back
  cone to the same cone scaled Face width toward the apex, and in to its axis. The gap runs a tooth's height past the
  blank at both ends (or at the front, half way to the apex, if that's nearer), so it cuts through it; it's
  patterned around the axis once per tooth, and cut from the blank.
- **Meshing**: two bevel gears mesh with their apexes together and their axes the shaft angle apart, each made with
  the other's teeth as Mating gear teeth. Its mate connector is at its apex, along its axis, for mating them.

Its gaps' roots are arcs without a fillet, and its flanks below the base circle radial lines.

### Gear pairs

With Gear pair, the gear's mate (Mating gear teeth) is made too, meshing with it:

- **Where**: toward the Angle reference from the gear's center (or along the gear's x axis without one), turned by the
  Angle, as Robot motor turns a motor (`core/mounting.fs`). A spur or helical mate's center is the gears' pitch radii
  away, on the gear's plane. A bevel mate shares the gear's apex, its axis the shaft angle from the gear's, its pitch
  cone touching the gear's toward the angle; its back is its cone distance times its pitch angle's cosine from the
  apex.
- **Its teeth** (`gearMatePlane`): where their pitch circles touch, a tooth of one meets a gap of the other. The gear's
  tooth nearest the touching point is the angle to it (less a whole number of teeth) from its first tooth, which is on
  its x axis (or for a sector with an even number of teeth, half a tooth from it: `gearMesh`'s tooth phase). Turning
  the gear back by that angle turns the mate forward by it times the ratio, so the mate is turned that far from a gap
  facing the gear. Bevel gears turn the same way, each about its axis toward its apex.
- **What it is**: a whole gear (a sector's mate too), of the opposite hand if helical, with the gear's pitch,
  thickness, router relief, profile offset, and bore (`mateDefinition`). It's named for its own teeth.
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

1. **The faces**: Face to cut, and with Tangent propagation, the faces tangent-connected to it which are in its plane
   (`qTangentConnectedFaces`, `qCoincidesWithPlane`). Curved ones are left out until sector gears can be cut too.
2. **Its direction**: along the faces' longest straight edge (`longestEdgeDirection`). Its length and width are the
   faces' exact bounding box along it and across it.
3. **Where the teeth go** (`rackToothCenters`): a pitch (`π m`) apart, with a gap centered on the start (End), the end
   (Opposite end, also flipped by a manipulator there), or the angle reference projected onto it. As many whole teeth
   as fit, root and all, fill it.
4. **What's cut** (`rackCutRegions`): everything between the face and the teeth. The face is at the pitch line, or
   `m` above it at the tips (Outside diameter). Flanks are straight at the pressure angle, and the root is `1.25 m`
   below the pitch line, both offset by the profile offset. Teeth reaching the face are cut off at it, leaving a
   region per gap. Teeth stopping short of it (with clearance, at Outside diameter) are cut over too, in one region.
   Past the first and last teeth, it's cut to the root.
5. The regions are sketched on a plane across the face's start (`sketchRackCut`), extruded through its width, and cut
   from the part.

## Compared with Spur gear

Onshape's Spur gear (`spurGear.local.fs`) makes spur and helical gears, one tooth cut from a blank and patterned
around it.

| Spur gear option | Here | Why |
| --- | --- | --- |
| Module, diametral pitch, or circular pitch | Standard pitches (10 DP, 20 DP, 32 DP, module 0.8), or Custom: diametral pitch (Inch) or module (Metric) | Robot gears come in a few standard pitches; circular pitch is rarely used |
| Pitch circle diameter (tied to Teeth by editing logic) | Size by Teeth or Pitch circle (a selected circle, with a warning if it's between sizes) | A circle places the gear too |
| Pressure angle | With the standard pitch; Custom only | 20 DP robot gears are 14.5°, which is easy to miss |
| Root fillet (none, 1/4, 1/3, full) | Standard (`0.38 m`), or a router bit's radius (Router relief) | What matters is whether it can be machined |
| Backlash | Profile offset | |
| Dedendum (1.157, 1.2, or 1.25 times the module), root and tip offsets | Left out: always `1.25 m`, and `m` | Standard full depth teeth; the profile offset covers clearance |
| Clocking angle | Left out: a tooth's on the location's x axis | Mate connectors and the location place it |
| Helical: angle, direction, double helix | Helical: Helix angle, Hand, Double helical | Hands are how helical gears are specified |
| Chamfer | Left out | Machined robot gears aren't chamfered |
| Center bore, keyway | The shared bore (`core/bore.fs`): hex, round, spline, and so on | Robot shafts' bores, not keyways |
| Depth, symmetric, opposite direction | Thickness, symmetric about the location, and the starting offset | |
| Gears table, `spurGear` attribute | Left out | |

## Issues found

- Untested in Onshape: nothing here has run yet.
- No undercut: on a pinion with few teeth (under about 17 at 20°), its mate's tips can reach below its base circle,
  where a generated gear would be undercut; its radial root relieves some of that, but not all.
- The icon is the generic robot icon.
- A rack is only cut along flat faces: sector gears along curved ones are still to come.
- Helical gears sweep the whole profile along each helix; Onshape's Spur gear sweeps one tooth. Untried in Onshape.
- Helical racks aren't cut.
- Bevel gears use Tredgold's approximation (the back cone's spur teeth, lofted to the apex), not spherical involutes:
  close for robot use and 3D printing, not for precision gearing. Their teeth aren't crowned, their roots aren't
  filleted, and they have no Router relief, helical (spiral) teeth, or sizing check of Face width against a third of
  the cone distance (the usual most).
- A bevel gear's gap is lofted between two sketches of the same gap at different scales; the loft's untried in
  Onshape.
- REV's MAXSpline 20 DP gears' pressure angle isn't published; FTC gears besides goBILDA's (REV's, say) aren't among
  the standard pitches.
- Its regions are flush with the face, its ends, and its sides: booleans treat faces within their tolerance as the
  same, but it's untried in Onshape.
- Past the last whole tooth, the rest of the face is cut to the root, rather than left as a partial tooth.
