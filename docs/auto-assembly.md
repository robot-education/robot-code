# Auto Assembly from FeatureScripts, and simplification

**Status: Robot motor, Robot bearing, Robot bearing retention and Robot bushing declare their parts; FRCDesignApp places them.** Built here: the
`"Auto Assembly"` and `"Simplification"` annotations and their checks, `core/autoAssembly.fs`, `core/simplify.fs`,
`core/bearing.fs`, the library snapshots (`fs_cli/library.py`), the bushings and bearing hats part sets, Robot motor (on
main), Robot bearing, Robot bearing retention and Robot bushing. Built in FRCDesignApp: **Assemble FeatureScript parts**, on its Auto Assembly
page, which inserts each declared part and fastens it to its host connector (FRCDesignApp's
`docs/architecture/auto-assembly.md`). Not yet: swapping an Auto Assembly body for its part, and every other feature
(see [Later](#later)).

This is the contract between robot-code's FeatureScripts and FRCDesignApp for two things built together:

- **Declared parts**: a FeatureScript names the library part that goes where it cut something, and Auto Assembly puts
  that part in an assembly.
- **Simplification**: one switch, set per Part Studio or assembly and carried down into what they contain, that makes
  the features which allow it build something lighter.

They meet in the Auto Assembly body: the simple body a declaration draws for a big part (a motor, later a gear) is what
a simplified feature builds too.

## What's there already

- **FRCDesignApp's Auto Assembly** fastens library screws, washers and nuts into holes picked in an assembly. It scans
  Part Studios with FeatureScript it evaluates (`hole-scan.fs`), plans each hole, inserts the parts in one call, and
  fastens each to a mate connector. Declared parts run the same way, with a scan of their own (`part-scan.fs`). Onshape's
  API can't replace an instance or change its configuration, so replacing is deleting and inserting.
- **The fasteners and bearings part sets** (`featurescripts/fasteners/`, `featurescripts/bearings/`,
  [parts.md](parts.md)): Part Studios generated from a Python catalog, each with a mate connector at a fixed spot, and a
  manifest the library loads. A connector a feature makes is named in a fasten mate as `<featureId>.<id>`, which is how
  the app fastens to one, since Onshape's API lists no connector a feature makes.
- **The FeatureScripts tab** reads `featurescripts.json`, the manifest robot-code's `fs release` writes into the
  frontend document (FRCDesignApp's `docs/architecture/featurescripts.md`).
- **2023's `auto-assembly` branch** of this repo prototyped declared parts: mate connectors carrying an attribute
  naming a part, read by evaluating FeatureScript, inserted and fastened by a Python script.

## Declared parts

The first declarations:

- **Robot motor**: the motor or gearbox whose face it cut (see [Robot motor](#robot-motor)).
- **Robot bearing** and **Robot bushing** (both new): the bearing or bushing in each bore they cut. **Robot bearing
  retention** (Robot bearing hat, renamed): a bearing's hat, or its screws (see
  [Robot bearing and Robot bushing](#robot-bearing-and-robot-bushing)).

### The declaration

A declaration is an attribute, `frcAutoAssembly`, naming a library Part Studio and its configuration:

```
{
    "version" : 1,
    "kind" : AutoAssemblyKind.BEARING,      // MOTOR, GEARBOX, BEARING, BUSHING, BEARING_HAT, SCREW
    "documentId" : "...",                   // the library Part Studio, with no version
    "elementId" : "...",
    "configuration" : { "size" : "IN_1_2" },   // parameter id to option id
    "spec" : { "outerDiameter" : 1.125 * inch, "width" : 0.313 * inch },   // what the feature cut for it, by kind
    "name" : "1/2\" Hex Bearing",           // for messages, when the app can't place it
    "connector" : "mateConnector"           // the library part's connector it takes, by name
}
```

- **The product is a tab, not a version.** The app inserts from the version of that Part Studio its library loaded
  last, so a library tweak released as a new version reaches every declaration with no FeatureScript release. The
  configuration is the app's selection (parameter id to option id), as the app stores and sends it: a declaration
  stays good while the studio keeps those ids, which [Keeping referenced parts stable](#keeping-referenced-parts-stable)
  guards. A FeatureScript is released only to offer a new product, or a new kind.
- **`spec`** is what the feature built for the part, so the app can say a product doesn't fit rather than insert it.
  The app doesn't check it yet. A screw's says how far below its seat the Part Studio's holes start, through a part
  that isn't in it, and that part's hole (`seatHeight`, `seatHoleDiameter`): a COTS hat's, which Auto Assembly places
  in the same run.
- **`nut`**, on a screw's alone, is the nut to put where it comes out the far side: the fasteners' Nut studio and its
  type. Without one, a screw through its holes gets no nut; a tapped hole's declares none.
- **`version`** is the attribute's format. A new optional field leaves it as it is; a field changing meaning bumps
  it, and the app reads every version it has read before (the attribute exception in `AGENTS.md`). The app leaves out
  a connector whose version it doesn't know.

It's set in up to two places, each with its own job: on a host connector, and on an Auto Assembly body.

### The host connector: placing it with no mates

Where the part attaches to a part the feature cut, the feature puts a mate connector on that part, carrying the
declaration: the plate, for a bearing or bushing in its bore. It comes into the assembly with that part, so Auto
Assembly can place and fasten every bearing on it with nobody mating anything. It's named by the feature's id and a
fixed name (`place`, then `bearing<n>` for its location, then `mateConnector`), so a mate to it survives the feature
being edited. A fasten mate names a
connector by its whole id, each part joined with `.`: Robot bearing's first is `<featureId>.place.bearing1.mateConnector`.

### Auto Assembly bodies

Robot bushing and Robot motor have an **Auto assembly body** checkbox, on by default and remembered. An Auto
Assembly body is a separate, simple solid part for the part's envelope, which carries the declaration too: a ring at
the part's outside diameter, with its bore, and a flanged part's flange.

Every Auto Assembly body is named as its product, so the Part Studio's parts list reads as the hardware does. It's
excluded from the bill of materials (`PropertyType.EXCLUDE_FROM_BOM`), since the bill counts the real part once it's
placed, and all Auto Assembly bodies share one translucent appearance (`AUTO_ASSEMBLY_BLUE`), so none is mistaken for a
part to make.

### An Auto Assembly body's mate connectors map to the real part's

An Auto Assembly body has its own mate connectors, each named and placed where the library part has one of the same
name. So in an assembly an Auto Assembly body is mated like any part: a person inserts or copies it, fastens it where it
goes, and Auto Assembly can later swap it for the real part on those same mates. A host connector is named and placed
as the part's connector it takes, so it's fastened to the same way.

| Kind | Connectors (on the Auto Assembly body, and on the library part) |
| --- | --- |
| Motor, gearbox | `face`: the center of its mounting face, Z into the motor, X to the right of its face as its drawing shows it (Robot motor's tables' convention) |
| Bearing, bushing | `mateConnector`: where it seats, as FRCDesign's bearings part set's is (`featurescripts/bearings/`): on its axis, at a flange's underside or a radial bearing's top face, Z out of the hole |
| Bearing hat | `mateConnector`: the center of its bore on the face it sits on, Z out of the face, X toward a mounting hole (the bearing hats part set's) |
| Screw | `mateConnector`: the center of the head's underside, Z from shank to head (the fasteners' convention) |

The library's own studios name their connectors with Mate connector features of those names; a generated part set's
studios make them, and its manifest names them, as the fasteners' do.

### Placing declared parts

FRCDesignApp's **Assemble FeatureScript parts**, a button on its Auto Assembly page, opens a dialog listing every
declared part in the assembly. Each can be left out, and one whose hole has a far side can be **flipped**: seated on
the host's other face, facing the other way, as a plate mirrored in the assembly needs. Its run:

1. **Scan.** `part-scan.fs` returns each mate connector carrying an `frcAutoAssembly` attribute on a part: the part's
   id, the connector's declaration, its coordinate system, and its id as a fasten mate names it
   (`lastModifyingOperationId`, joined with `.`). It also returns the connector's far side: the host's circular edge on
   its axis farthest behind it. A connector on an Auto Assembly body is left out. It's cached by the same immutable
   keys as the hole scan.
2. **Resolve.** Each declaration's document and element are looked up among the loaded insertables, giving the
   version the library loaded last, and its configuration is parsed as any selection is (`toSelection`). A studio the
   library doesn't have, a configuration it doesn't offer, or a part with no connector, gets no part, with why. A
   connector with its part already on it is left alone.
3. **Plan screws.** A screw is planned as Auto Assembly plans a picked hole, from its connector into the holes behind
   it, through a hole as deep as its `seatHeight` first. Its length is the one declared, or with none, the one its
   holes need. Where it comes out the far side, it gets its declared nut, in its size, from its vendor where it makes
   that nut. Into threads, it gets none. A screw whose holes don't fit it gets no part, with the planner's reason.
4. **Insert** each part chosen at its host connector, or a flipped one at its far side, and each screw's nut at the
   far side of its holes, from that version, in the run's one `transformedinstances` call.
5. **Mate** each part's connector to its host connector with a fasten mate; a flipped part's to the implicit connector
   at the center of the hole's far rim, which points out of the far face; a nut's to the implicit connector at the
   center of its holes' far edge. A flip needs nothing more from the feature.
6. **Record** one use per part placed, as an insert from Auto Assembly's FeatureScript parts, so the dashboard shows
   them apart from fastened holes.

Later, picking an Auto Assembly body, a part with host connectors, or a whole instance takes only the declarations in
it; and an Auto Assembly body a person mated is swapped for its part, each of its mates rewritten to name the real
part's connector of the same name.

### Keeping referenced parts stable

robot-code's release writes, in `featurescripts.json`, whether a feature declares parts (`autoAssembly`). Later it
lists every library product its features' tables reference: each studio's document and element, and the parameter and
option ids used. The app would read it with the rest of the manifest, and then:

- An insertable a released FeatureScript references shows a **Used by Auto Assembly** badge on the admin card, saying
  which features reference it, and that its configuration's ids must not change.
- Loading a referenced studio whose parameter or option ids no longer include ones referenced raises a build issue
  naming the features and options affected, on the studio and on the admin card, before anyone hits it in a run.

Renaming an option's name is a tweak and changes nothing; deleting an option and making it again changes its id. A
generated part set never changes a released option id (`AGENTS.md`).

### Where the products come from

- **Generated part sets**, for bearings, bushings and WCP's bearing hats (`featurescripts/bearings/`,
  `featurescripts/bushings/`, `featurescripts/bearingHats/`), and screws and nuts (`featurescripts/fasteners/`). Each
  studio has its connector where the convention says, and one catalog writes the feature's table, the studios'
  configurations and the manifest, so a feature knows a part's sizes, as Robot bearing retention knows a hat's thickness. Until
  a set is released, its parts have no document, and the features declare nothing and say so. The hats' sizes are
  estimates until confirmed against FRCDesignLib's model.
- **The library's own studios**, for Robot motor's motors, from a snapshot of FRCDesignLib (`fs_cli/library.py`). A
  studio whose connector a configuration loses is caught by the library's load as it is today
  (`UNSTABLE_MATE_CONNECTOR`).

The library's product lists are generated, as the fasteners' tables are (see
[fs-cli.md](fs-cli.md#generating-lookup-tables)): a Python definition reads a snapshot of the library from FRCDesign's
public API (`/api/library-data` and `/api/configuration/insertable/{id}`, as `fastenerTables.py fetch` does), and
writes each product's document, element, configuration and name into a `*.gen.fs` table, with no version.

## Simplification

A large robot's assembly regenerates slowly, mostly because of detail nobody needs while laying out the robot: gear
teeth, lightening pockets, belt and chain teeth, and hundreds of screws. Simplification is one switch that turns that
detail off, everywhere at once. It acts on two things only: robot-code's FeatureScripts, which build less, and
FRCDesignLib's hardware in assemblies, which is suppressed. It never suppresses or changes anyone's other features.

### The switch: the Simplified configuration parameter

Every element simplifies by one configuration parameter named **Simplified**, off by default:

- In a **Part Studio**, a configuration variable, which a FeatureScript reads as a variable
  (`getVariable(context, "Simplified")`, as `core/logging.fs` reads `#logging`). A Part Studio without it is never
  simplified.
- In an **assembly**, a checkbox configuration input, which suppresses the hardware that follows it and is passed
  down to the instances that have their own (see Include what's inside, below).

`core/simplify.fs` holds it for features:

- `simplificationPredicate(definition)`: **Allow simplification**, a checkbox in the feature's last group, on by
  default and remembered. Off, the feature always builds in full: for a part whose detail matters (a gear being
  checked for interference, a plate going to the router).
- `isSimplified(context, definition)`: whether to build simplified: allowed, and the Part Studio's Simplified is on.

What each feature builds when simplified:

| Feature | Simplified |
| --- | --- |
| Robot gear | Its pitch-circle disc with its bore, as an Auto Assembly body's (a bevel gear: its pitch cone's frustum) |
| Robot lighten | Nothing: the part, unpocketed |
| Robot motor | The cut, and its own envelope in place of FRCDesign's Block Motor, which it then doesn't derive |
| Later: Robot belt, Robot chain, Robot pulley, Robot sprocket | Their pitch path or pitch circle, without teeth |
| Later: Robot shaft | The shaft, with no end holes, grooves or flutes |

Mass changes when a part is simplified, so the app never reports mass, and nothing should be measured, from a
simplified configuration.

### The Simplify menu

A **Simplify** button in the app's top toolbar opens a menu acting on the Part Studio or assembly the app was opened
from:

- **Add simplification**: adds the Simplified parameter, off. Nothing else: the FeatureScripts that allow
  simplification follow it by themselves.
- **Include what's inside**: carries the variable into what the element uses, so simplifying a top assembly
  simplifies everything below it:
  - In a Part Studio, each Derived feature from a Part Studio with the variable gets its configuration set from the
    Part Studio's own (`#Simplified`); a Part Studio without it is offered Add simplification first, when it's in a
    document the person can edit.
  - In an assembly, each instance of an element with the parameter follows the assembly's own Simplified, down
    through its subassemblies. The API can't change an instance's configuration, so how depends on what Onshape
    allows (see [What to try in Onshape](#what-to-try-in-onshape)): by the assembly's configured instances, if the API
    can set them; otherwise by replacing each instance with one in the other configuration, keeping its mates, as
    placing a declared part does.
- **Simplified / Full**: switches the element's own configuration, for the person viewing it.
- In an assembly, **Suppress library hardware**: every FRCDesignLib fastener and other hardware Auto Assembly
  inserted (screws, washers, nuts, end screws, rings) gets its suppression configured by Simplified, as Follow
  simplified does for new ones (below). Hardware inserted by hand isn't touched.
- In an assembly, **Swap placed parts for Auto Assembly bodies** and back: the declared parts' real models are often the
  heaviest left, and an Auto Assembly body is lighter still than a simplified Part Studio.

The menu lists what it would change before it changes anything, and each action records its use, as every feature
does (FRCDesignApp's `AGENTS.md`).

### Follow simplified, in Auto Assembly

Auto Assembly's form has a **Follow simplified** checkbox, on by default and remembered, with an info icon whose
tooltip reads: "Causes inserted hardware to automatically suppress/unsuppress in response to the Simplified
configuration parameter."

On, a run:

1. Adds the assembly's Simplified parameter if it has none, off.
2. Inserts the hardware as it does now.
3. Configures each hardware instance it inserted to be suppressed when Simplified is on. Declared parts with a
   Auto Assembly body (gears, motors) aren't hardware: they follow Simplified by their library part's own simplification
   (below), not by suppression.
4. If Simplified is on in the configuration the assembly is showing, the run's finished toast carries an info line:
   "The inserted hardware is suppressed because Simplified is on."

### Library parts

Much of the library is simplified already, by a checkbox of its own: the motors' **Simplified Model**. So a library
part's switch is whichever of its parameters an admin marks as its simplification, on the insertable, rather than the
variable. The app then offers **Insert simplified** beside Insert, remembered per person; Auto Assembly places a
declared part simplified when its assembly is; and Include what's inside sets it on library parts already in an
assembly. A library part without such a parameter can have one modeled by its admin: a gear studio's might suppress
its pockets and teeth.

### Ideas, for later

- **Envelopes.** A second level beside Simplified, building envelopes (a gear as a cylinder at its outside diameter,
  a plate as its outline), for packaging studies at robot scale.
- **Simplify by cost.** The menu shows each Part Studio's regeneration time (Onshape's per-feature timings), so the
  person simplifies where it pays.
- **Simplified by default.** An assembly's own setting, so new instances arrive simplified and the full configuration
  is used for drawings and exports.

## Robot motor

Unreleased, so it changes freely. It declares the motor or gearbox whose face it cuts, has an Auto Assembly body, and allows
simplification.

**What it declares.** Each FRC entry of `motorTables.py` names its product: the library's motor, by its document and
tab, at its defaults (a NEO by its Version), from the snapshot in `motor/vendor/frcdesign-motors.json`. The generated
table carries it as it carries the face. Where the library has the motor:

| Robot motor's entry | Library part (Motors & Servos, or Gearboxes) |
| --- | --- |
| Kraken X60, Kraken X44 | Kraken X60 / X44 Brushless Motor (assemblies) |
| Falcon 500 V1/2, V3 | Falcon 500 Brushless Motor (V1/2) (assembly), (V3) |
| NEO V1.0, V1.1 | NEO Brushless Motor, by its Version |
| NEO 2.0, NEO Vortex, NEO 550 | NEO 2.0, NEO Vortex (assembly), NEO 550 Brushless Motor |
| CIM, Mini CIM, RS-775, Minion, Thrifty Pulsar | CIM Motor, Mini CIM Motor (assemblies), RS-775 Motor, Minion, Thrifty Pulsar 775 |
| MAXPlanetary, PlanetaryX, UltraPlanetary, VersaPlanetary | MAXPlanetary (REV), PlanetaryX (WCP), UltraPlanetary (REV), VersaPlanetary v2 (VEX) |
| Yellow Jackets, goBILDA planetary | None yet: ConfigLib's (FTC), once its library is loaded the same way |

**Host connector.** On the part the face sits on (`partBehind`): of the merge scope's parts, the one nearest the plane behind it. At
the face's center, Z out of the plate (toward the motor), X the face's drawing's right: the motor's `face`. With no
merge scope (a block motor alone), there's none, and the Auto Assembly body carries the declaration.

**Auto Assembly body.** Its Auto Assembly body is its block motor, and its checkbox replaces Block motor. FRCDesign's Block Motor already lines up with the face
(`blockAngle`); its connector on the plane becomes `face`, carrying the declaration, and the part is excluded from the
bill of materials and colored as Auto Assembly bodies are. Gearboxes have no block motor yet: Block MAXPlanetary and Block
PlanetaryX could be theirs. The CIM, Mini CIM and RS-775 have no block model in FRCDesignLib; envelopes of our own, as
the Minion's, would give them one.

**Simplified.** Deriving another document's Part Studio costs the most of anything Robot motor builds, so simplified
it leaves the block motor out where there's a merge scope to carry the declaration; the cut is the same. Building an
envelope of our own in its place needs each Block Motor motor's diameter, length, pilot height and shaft, read from
Block Motor's sketches (an Onshape call or two). In an assembly, a motor placed by Auto Assembly is placed with its
Simplified Model on when the assembly is simplified.

## Robot bearing and Robot bushing

**Robot bearing** and **Robot bushing** are new: each cuts bores, one at each sketch point picked, and declares its part
in each. They share their placement in `core/bearing.fs`. **Robot bearing retention** is Robot bearing hat, renamed:
its Feature Type Name and its file (`fs mv` to `released/bearing/robotBearingRetention.fs`), never its constant,
`robotBearingHat`, which documents store (`docs/featurescript-style.md`, "Released feature constants"). It holds one
bearing in, at its bore's edge.

### What Robot bearing and Robot bushing do

Both cut the bores, as Robot motor cuts its face. Their dialogs follow std's hole's order, from `core/` predicates:

1. **Style** (Robot bearing, for a flanged bearing): Simple, or Counterbore, as the hole's.
2. **Sketch points to place bearings** (bushings): any number of sketch points, circles, or mate connectors, and Merge
   scope (`bearingLocationPredicate`).
3. **Bearing**: one of FRCDesign's bearings part set's FRC bearings (`featurescripts/bearings/`), by Vendor, Type,
   Bore, Size, Dimensions and Seal (`robotBearingTable`), with Bore fit and its clearance (`boreFitPredicate`,
   `core/fit.fs`). Or **Bushing**: one of the bushings part set's (`featurescripts/bushings/`), by Vendor, Bore, Size,
   Material and Dimensions (`bushingTable`), with no fit, as a COTS bushing is pressed in.
4. Starting offset, Flip primary axis and Reorient secondary axis, Angle reference and Angle
   (`bearingOrientationPredicate`).
5. **Counterbore diameter** and **Counterbore depth** (Robot bearing, with Counterbore): set by editing logic to the
   flange's diameter plus Bore fit's clearance, and its thickness, so the flange sits flush.
6. **Auto assembly body** (Robot bushing), in Other options.

What they do:

1. **Editing logic** (`bearingEditLogic`): std's hole heuristics, with a point at every location, set the merge scope
   to the parts there, and Flip primary axis so the bores go into them, unless they've been set.
2. **The planes**: each location's, offset, turned to the angle reference and Angle, and flipped; each normal points
   out of the face its bearing sits on. The offset's and angle's manipulators are on the first.
3. **The bores**: at each plane, a circle at the product's outside diameter plus Bore fit's clearance (a bushing's, at
   its outside diameter), extruded through the merge scope behind it, and a counterbore its depth. All are subtracted
   in one boolean, as Robot motor's pilot is. The part each plane is on, of the merge scope's, is that bore's host.
4. **Declarations**: at each bore, a host connector on its host at the bore's center, Z out of the bore, X the plane's
   (so the angle reference turns it): the bearing set's `mateConnector`. It's on the face, or on a counterbore's
   floor, where the flange sits. Each bore's operations are under `place.bearing<n>`, `<n>` its location's place in
   the selection. A part's product is its set's studio, with its record's configuration; until the set is released,
   the studio has no document, and the feature declares nothing and says so.
5. **Auto Assembly bodies** (Robot bushing): at each bore, a ring at the part's outside diameter, its hole at its
   bore's corners (its nominal size), into the bore, and its flange on the face, at the set's flange sizes. Robot
   bearing has none.

`"Auto Assembly" : true` on both.

### Robot bearing retention

It takes **Bearing edge to place retention**: a bore's circular edge, as Robot bearing hat did. The bore's diameter is
the edge's, and its plane is the edge's, its normal out of the hole. Flip primary axis, Reorient secondary axis, Angle
reference and Angle turn it, but for a custom hat or custom screws, whose selections place them. Its holes are cut into the
edge's part, which is the host of what it declares.

A top level **Retention type** is Hat or Screws. Under it, a second: **Hat type** (COTS, Standard hat or Custom hat),
or **Screws type** (Standard screws or Custom screws).

**Hat** is the old feature's hats, with their mounting holes and hole tables:

- A standard hat's size is **Hat centers** (1.25, 1.5 or 2 in.). A standard or custom hat's bore is the edge's.
- A COTS hat is **Hat**: one of FRCDesign's bearing hats part set's WCP hats, by the bearing's outside diameter and
  centers (0.875 in. at 1.25 or 2 in., 1.125 in. at 1.5 or 2 in.; `hatTable`). A hat for another bearing than the
  one nearest the edge's diameter is an error. It declares itself: its connector goes on the host at the bore's
  center, X toward a mounting hole.

**Screws** retains the bearing with screw heads over its outer race or flange:

- **Standard screws**: 2 or 3 (**Screw count**), evenly around the bore (180 or 120 degrees apart), from the plane's X
  axis. Their hole circle is the bore's diameter, plus the hole, plus a 0.03 in. wall each side, so each hole clears
  the bore and a head over it overlaps the bearing (`screwPositions`); **Override hole circle** sets it by hand.
- **Custom screws**: one at each of **Screw locations**, any number of vertices, circles or mate connectors, as a
  custom hat's mounting locations are picked.

**Mounting holes**: Tapped or Clearance, with their hole tables, as Robot bearing hat's.

**Auto assembly**, a checkbox driving its own group, on by default and remembered, declares a screw at each mounting
hole. Its screw and nut are lookup tables of the fasteners part set's #10-32s, as the set's records configure them
(`screwTable`, `nutTable`):

- **Screw**: Vendor, Head and Finish, skipping a level with one option, then Length. Length lists only the lengths
  made in those, after **Automatic**: no length in the declaration, for Auto Assembly to size from what the hole goes
  through.
- **Nut**, where the holes aren't tapped: the nut declared on each screw.
- 24 tpi tapped holes declare no screws, with an info saying why: the set has no #10-24s.
- Each screw is on a connector named as the fasteners' own (`mateConnector`), Z out of the hole.
- A standard or custom hat's screws seat on its top. A COTS hat's seat as high as it is thick, with `seatHeight` and
  `seatHoleDiameter` in their spec, so Auto Assembly goes through the hat it places in the same run. A screw
  retention's seat on the host's face.

`"Auto Assembly" : true`.

### Robot bushing

Robot bearing's placement, with the bushings part set's bushings: `featurescripts/bushing/`.

The bushings part set (`featurescripts/bushings/`, `libraryBushing`, in a Bushing Part Studio) is built as the bearings'
is, from FRCDesignLib's Flanged Bushing records, with flanges from The Thrifty Bot's STEP files and FRCDesignLib's
models of WCP's. It asks Vendor, Bore, Size, Material, then Dimensions, as the bearings' does; every vendor
makes both bores and several sizes, so the bore and size always show. It offers
WCP's and ThriftyBot's, and Generic's of the same: VEX no longer sells bushings, and REV sells one.

### What changes for documents

Robot bearing hat's released documents keep their bearing edge, and update to Robot bearing retention. Following
`AGENTS.md`'s "Backwards compatibility", the hat tables' parameters go with no migration: a standard hat takes the
default Hat centers, and a custom hat's bore is its edge's.

## Icons in the FeatureScripts tab

Each feature's card shows two small icons beside its name: **Auto Assembly** (the panel's `assembly.svg`) when it
declares parts, and **Simplified** (a new icon: the gear's own outline reduced to a disc) when it allows
simplification, each with a hover saying what it means.

A feature says what it supports in its own annotation, beside its name:

```
annotation { "Feature Type Name" : "Robot motor",
        "Feature Type Description" : "Cut the mounting holes of a motor or gearbox, and model a block motor." ~ CREDIT,
        "Auto Assembly" : true,
        "Simplification" : true,
        "Icon" : RobotMotorIcon::BLOB_DATA }
export const robotMotor = defineFeature(...);
```

Both keys are ours: Onshape ignores annotation keys it doesn't know, as it ignores `"Set By"`. `fs release` reads them
as it reads `"Feature Type Name"` (`fs_cli/frontend.py`), and writes them into the feature's manifest entry, beside the
products its tables reference:

```
"autoAssembly" : { "products" : [ { "documentId" : "...", "elementId" : "...", "configurations" : [ ... ] } ] },
"simplification" : true
```

A key that's absent is false. `fs check` keeps them honest:

- Both join its known annotation keys (`ANNOTATION_KEYS`, beside `"Set By"`), allowed only in a `defineFeature`'s
  annotation, with a boolean value.
- `"Auto Assembly" : true` needs the feature to declare parts (call `declarePart`, `core/autoAssembly.fs`), and a
  feature that declares parts needs the key; likewise `"Simplification" : true` and `simplificationPredicate`
  (`core/simplify.fs`). Either way round, it's an error naming what's missing, so a feature never shows an icon for
  something it doesn't do, or hides one it does.

Library parts whose admin marked a simplification parameter show the Simplified icon on their cards too.

## What to try in Onshape

Each of these is unverified, and the run leans on it:

1. A fasten mate to a connector a custom feature made keeps resolving after the feature is edited. (It's named by its
   whole id joined with `.`, as `<featureId>.place.bearing1.mateConnector`, by the owner.)
2. `lastModifyingOperationId` of that connector, in an evaluated script, gives that id (the owner expects so).
3. An attribute set on a solid and on a mate connector body can be read back by an evaluated script, and
   `qMateConnectorsOfParts` finds a connector a feature attached to a part with `owner`.
4. An Auto Assembly body with Exclude from BOM is left out of an assembly's bill of materials.
5. A FeatureScript reads a configuration variable with `getVariable`, and its value changes with the configuration.
6. A Derived feature's configuration can be set to an expression of the Part Studio's own variable (`#Simplified`).
7. The API can set an assembly's configured instances: an instance's configuration per the assembly's configuration.
8. The API can add a configuration input to an assembly, and configure an instance's suppression by it.
9. A Part Studio configuration variable named `Simplified` and an assembly checkbox input named `Simplified` can be
   passed from one to the other when an assembly configures its instances.

## Later

Each its own branch, on this contract:

- **Swapping Auto Assembly bodies**: a mate's queries rewritten through the API to name the real part's connector.
- **The app's simplification**: the Simplify menu, and Follow simplified in Auto Assembly.
- **Robot motor's envelopes**: read from Block Motor's sketches through Onshape's features endpoint, a call or two.
- **Robot gear, Robot shaft's hardware and Robot lighten**: gears from the library's studios, end screws, retaining
  rings and e-clips from part sets of their own.
