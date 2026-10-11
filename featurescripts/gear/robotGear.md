# Robot gear

Makes involute spur gears (or sectors of them), or cuts rack teeth into a part's face. Both are sized by diametral
pitch (Inch) or module (Metric), with standard proportions. A gear has root fillets, optionally roots a router bit can
cut, and can have a bore.

| File | What it is |
| --- | --- |
| `robotGear.fs` | The feature: its dialog, the gear, and cutting a rack (`cutRack`) |
| `gearCommon.fs` | The tooth form (`getGearForm`), a gear's profile (`sketchGearProfile`), and a cut rack's teeth and regions (`rackToothCenters`, `rackCutRegions`, `sketchRackCut`) |
| `../core/bore.fs` | The bore: its parameters, and cutting it |
| `../core/location.fs`, `../core/startOffset.fs`, `../core/profileOffset.fs` | The location, starting offset, and profile offset |
| `../core/mounting.fs` | The angle reference's point (`angleReferencePoint`) |
| `../../tests/fs_eval_sketch_test.py` | Tests of gears' profiles, and of where a rack's teeth go and what's cut |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot gear | feature name |
| Create involute spur gears and sector gears, or cut racks into parts, by diametral pitch or module. `<CREDIT>` | feature description |
| `<teeth>`T `<pitch>`DP Gear, or `<teeth>`T Module `<module>` Gear | part name |

A gear's material is Onyx, and its appearance printed green. A cut rack leaves its part's properties as they are.

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Diametral pitch (`diametralPitch`) | Teeth per inch of pitch diameter: 20 for most robot gears. |
| Module (`module`) | Pitch diameter per tooth. |
| Pressure angle (`pressureAngle`) | 20 degrees for most gears; some older ones are 14.5. |
| Face to cut (`rackFace`) | The teeth run along its longest edge, cut through its width. |
| Tangent propagation (`tangentPropagation`) | Also cut along the flat faces in line with it, across tangent edges. |
| Face at (`rackFaceAt`) | The teeth's pitch line (their tips are cut off at the face), or their tips. |
| Angle reference (`angleReference`) | A point: a gap is centered on it, as on an end. |
| Sector (`sector`) | Only some of its teeth, in a wedge from its center. |
| Router relief (`routerRelief`) | Round the roots to a router bit's radius, so it can cut them (a full round, deeper than the root, where the gap's too narrow). |
| (the bore's, as Robot sprocket's) | |

No parameters are hidden.

### Errors, warnings, and info

| Message | Kind | When | Highlights |
| --- | --- | --- | --- |
| Select a sketch point, circle, or mate connector to use. | error | no location | `location` |
| A sector has fewer teeth than its gear. | error | Sector teeth is the gear's teeth or more | `sectorTeeth` |
| The gear's teeth come to points: give it more teeth, or less clearance. | error | a tooth's flanks meet below the tip circle | `teeth` |
| The gear's too small for its teeth: give it more teeth. | error | the root circle's at or below the center | `teeth` |
| The profile offset is bigger than the roots' round: use less clearance, or a bigger router bit. | error | the profile offset leaves no root fillet | `profileOffsetDistance` |
| The root's round doesn't fit between the teeth: use a smaller router bit, or a bigger module. | error | not even a full round of the fillet's radius fits below the involutes | `bitDiameter` |
| Select a face to cut the rack into. | error | Cut rack, no face | `rackFace` |
| The face needs a straight edge for the teeth to run along. | error | none of the faces' edges is a line | `rackFace`, the face |
| Select an angle reference. / The angle reference must be a point. | error | Angle reference, with none, or a selection that isn't a point | `angleReference` |
| The face is too short for a whole tooth. | error | no whole tooth fits along it | `rackFace`, the faces |
| The rack's teeth come to points: use less clearance. / The rack's teeth meet at their roots: use more clearance. | error | the profile offset's too far | `profileOffsetDistance` |
| Its pitch diameter is `<diameter>`. | info | a gear | |
| Failed to cut bore. / The bore is bigger than the part. / Failed to chamfer bore. | error | (`core/bore.fs`) | the bore's parameters, and what failed |

## How it works

### Execution order

1. **Precondition**: Gear type (Create gear or Cut rack). Create gear: location; Diametral pitch or Module (by unit
   system), Teeth, Pressure angle, and Thickness; Sector and Sector teeth; Router relief and Bit diameter; starting
   offset; bore; profile offset; Add mate connector. Cut rack: Face to cut, Tangent propagation, Face at, Align teeth
   to (End, with Opposite end, or Angle reference); Diametral pitch or Module, and Pressure angle; profile offset.
   (A rack's roots are square: there's no round for Router relief.)
2. **Body**: the module (an inch over the diametral pitch), and the tooth form (`getGearForm`: pitch radius `m z / 2`,
   base radius its cosine of the pressure angle, tip `m` above it and root `1.25 m` below, fillet `0.38 m` or the bit's
   radius, all offset by the profile offset). Then:
   - Create gear: the profile (below), extruded the thickness both ways from the location's plane, its properties, a
     profile offset manipulator, mate connector, its bore (`cutBores`), and its pitch diameter as info.
   - Cut rack: `cutRack` (below).
3. **Manipulator change** (`robotGearManipulatorChange`): the rack's end flip, or the profile offset's and starting
   offset's.

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

A sector's teeth are centered on the x axis, and its ends run from the middle of the gaps at its ends to the center.

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

## Issues found

- Untested in Onshape: nothing here has run yet.
- No undercut: on a pinion with few teeth (under about 17 at 20°), its mate's tips can reach below its base circle,
  where a generated gear would be undercut; its radial root relieves some of that, but not all.
- The icon is the generic robot icon.
- Cut rack only cuts along flat faces: sector gears along curved ones are still to come.
- Its regions are flush with the face, its ends, and its sides: booleans treat faces within their tolerance as the
  same, but it's untried in Onshape.
- Past the last whole tooth, the rest of the face is cut to the root, rather than left as a partial tooth.
