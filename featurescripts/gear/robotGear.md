# Robot gear

Makes involute spur gears (or sectors of them) and racks, sized by diametral pitch (Inch) or module (Metric), with
standard proportions, root fillets, and optionally roots a router bit can cut; a gear can have a bore.

| File | What it is |
| --- | --- |
| `robotGear.fs` | The feature: its dialog, and the gear or rack |
| `gearCommon.fs` | The tooth form (`getGearForm`), and gears' and racks' profiles (`sketchGearProfile`, `sketchRackProfile`) |
| `../core/bore.fs` | The bore: its parameters, and cutting it |
| `../core/location.fs`, `../core/startOffset.fs`, `../core/profileOffset.fs` | The location, starting offset, and profile offset |

## Changelog

Unreleased.

## Strings

| String | Where |
| --- | --- |
| Robot gear | feature name |
| Create involute spur gears, sector gears, and racks, by diametral pitch or module, with roots a router bit can cut. `<CREDIT>` | feature description |
| `<teeth>`T `<pitch>`DP Gear, `<teeth>`T Module `<module>` Gear, `<pitch>`DP Rack, or Module `<module>` Rack | part name |

Its material is Onyx, and its appearance printed green.

### Descriptions and hidden parameters

| Parameter | Description |
| --- | --- |
| Diametral pitch (`diametralPitch`) | Teeth per inch of pitch diameter: 20 for most robot gears. |
| Module (`module`) | Pitch diameter per tooth. |
| Pressure angle (`pressureAngle`) | 20 degrees for most gears; some older ones are 14.5. |
| Height (`rackHeight`) | From its pitch line to its back. |
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
| The gear's too small for its teeth: give it more teeth. | error | the root circle's at or below the center, or the fillet's gone | `teeth` |
| The root's round doesn't fit between the teeth: use a smaller router bit, or a bigger module. | error | not even a full round of the fillet's radius fits below the involutes | `bitDiameter` |
| The rack's back must be below its teeth's roots. | error | Height is less than the dedendum | `rackHeight` |
| The rack's teeth come to points: use less clearance. / The rack's teeth meet at their roots: use more clearance. | error | the profile offset's too far | `profileOffsetDistance` |
| Its pitch diameter is `<diameter>`. | info | a gear | |
| Couldn't cut the bore. / Couldn't chamfer the bore's entrances. | error | (`core/bore.fs`) | the bore's parameters, and what failed |

## How it works

### Execution order

1. **Precondition**: Gear type; location; Diametral pitch or Module (by unit system), Teeth or Rack teeth, Pressure
   angle, Thickness, and a rack's Height; a gear's Sector and Sector teeth; Router relief and Bit diameter; starting
   offset; a gear's bore; profile offset; Add mate connector.
2. **Body**: the module (an inch over the diametral pitch), the tooth form (`getGearForm`: pitch radius `m z / 2`,
   base radius its cosine of the pressure angle, tip `m` above it and root `1.25 m` below, fillet `0.38 m` or the bit's
   radius, all offset by the profile offset), the profile (below), extruded the thickness both ways from the location's
   plane, its properties, a profile offset manipulator, mate connector, a gear's bore (`cutBores`), and its pitch
   diameter as info.
3. **Manipulator change** (`robotGearManipulatorChange`): the profile offset's and starting offset's.

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

A sector's teeth are centered on the x axis, and its ends run from the middle of the gaps at its ends to the center. A
rack's teeth run along the x axis from the location, with straight flanks at the pressure angle, the pitch line on the
x axis, and its back the height below it.

## Issues found

- Untested in Onshape: nothing here has run yet.
- No undercut: on a pinion with few teeth (under about 17 at 20°), its mate's tips can reach below its base circle,
  where a generated gear would be undercut; its radial root relieves some of that, but not all.
- The icon is the generic robot icon.
