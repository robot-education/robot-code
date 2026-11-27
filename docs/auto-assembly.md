# Auto Assembly from FeatureScripts, and simplification

**Status: Robot motor declares its part and allows simplification, untried in Onshape.** On main: the annotations and
their checks, `core/autoAssembly.fs`, `core/simplify.fs`, the library snapshots (`fs_cli/library.py`) and Robot motor.
Robot bearing and Robot bushing, and FRCDesignApp's **Assemble FeatureScript parts**, are on their branches
(`claude/featurescript-auto-assembly-rb4du7` in each repo). Not yet: Robot gear, Robot lighten, Robot shaft's hardware,
and the app's simplification. This is the contract between robot-code's FeatureScripts and FRCDesignApp for two
things built together:

- **Declared parts**: a FeatureScript names the library part that goes where it cut something, and Auto Assembly puts
  that part in an assembly.
- **Simplification**: one switch, set per Part Studio or assembly and carried down into what they contain, that makes
  the features which allow it build something lighter.

They meet in the Auto Assembly body: the simple body a declaration draws for a big part (a gear, a motor) is what a simplified
feature builds too.

## What's there already

- **FRCDesignApp's Auto Assembly** (`docs/architecture/auto-assembly.md` there). It fastens library screws, washers and nuts into holes picked in an assembly: it scans Part Studios with
  FeatureScript it evaluates (`hole-scan.fs`), plans each hole, inserts the parts in one call, and fastens each to a
  mate connector. Its decisions already say parts a FeatureScript declares should join it rather than becoming a tool
  of their own. It also records that Onshape's API can't replace an instance or change its configuration, so
  replacing is deleting and inserting.
- **The fasteners part set** (`featurescripts/fasteners/`, [parts.md](parts.md)): Part Studios generated from a
  Python catalog, each with a mate connector at a fixed spot, and a manifest the library loads. A connector a feature
  makes is named in a fasten mate as `<featureId>.<id>`, which is how the app fastens to one, since Onshape's API
  lists no connector a feature makes.
- **FRCDesign's library** has the parts to place, as Part Studios modeled by hand, configured by one enum each:
  - Shaft End Screw (WCP): #10-32, 1/4" and 1/2" long, and Heavy Duty from 1/4" to 5" long (WCP-2397 to WCP-2404).
  - Shaft End Screw (REV): #10-32, V1 and V2, 0.5" to 3.75" long.
  - External Retaining Ring (3/8" and 1/2", McMaster-Carr or WCP), Push-On External Retaining Ring, and E-Clip (2 mm,
    3/16", 3/8", 1/2", 1/2" bowed; McMaster-Carr or FRC vendor).
  - Gears: 41 Part Studios by pitch, bore and vendor (WCP, REV, AndyMark, VEX, TTB), about 600 configurations.
  - Motors & Servos (document `…6d66b0`): every FRC motor Robot motor knows, the Kraken X60 and X44, Falcon 500 (V1/2)
    and NEO Vortex as assemblies, the rest as Part Studios. Most have a **Simplified Model** checkbox already: the
    Krakens, Falcons, NEOs, NEO 550, Minion, Pulsar, and the RS-775's Simplified Motor.
  - Block Models (document `5e3874e07384706ec3840340`): **Block Motor** (Kraken X60 and X44, NEO Vortex, NEO V1.0,
    V1.1 and 2.0, Thrifty Pulsar, Falcon 500 V1/2 and V3, NEO 550), **Block MAXPlanetary** and **Block PlanetaryX**,
    among others.

  None of them has a mate connector yet (`supportsFasten` is false for every one).
- **The FeatureScripts tab** reads `featurescripts.json`, the manifest robot-code's `fs release` writes into the
  frontend document (FRCDesignApp's `docs/architecture/featurescripts.md`).
- **Robot motor** (`featurescripts/motor/`, unreleased) cuts a motor's or gearbox's mounting face into plates, and
  with Block motor brings in FRCDesign's Block Motor, derived from its document, or an envelope of its own for motors
  Block Motor doesn't have (the Minion and Yellow Jackets), with a mate connector on its face.
- **2023's `auto-assembly` branch** of this repo prototyped declared parts: mate connectors carrying an attribute
  naming a part, read by evaluating FeatureScript, inserted and fastened by a Python script.

## Declared parts

The first declarations:

- **Robot shaft**: a shaft end screw in a tapped end, and a retaining ring or e-clip in a groove.
- **Robot gear**: a COTS gear in place of its modeled one.
- **Robot motor**: the motor or gearbox whose face it cut (see [Robot motor](#robot-motor)).
- **Robot bearing** (Robot bearing hat, renamed) and **Robot bushing** (new): the bearing or bushing in the bore it
  cuts, and the bearing's retention: its hat, or its screws (see [Robot bearing and Robot bushing](#robot-bearing-and-robot-bushing)).

### The declaration

A declaration is an attribute, `frcAutoAssembly`, naming a library Part Studio and its configuration:

```
{
    "version" : 1,
    "kind" : AutoAssemblyKind.SHAFT_END_SCREW,  // SHAFT_END_SCREW, RETAINING_RING, E_CLIP, GEAR
    "documentId" : "...",                       // the library Part Studio, with no version
    "elementId" : "...",
    "configuration" : { "Configuration" : "_1_2__L___10_32__Heavy_Duty_" },   // parameter id to option id
    "spec" : { "size" : "#10-32", "depth" : 0.5 * inch },   // what the feature cut for it, by kind
    "name" : "Shaft End Screw (WCP) 1/2\" L (#10-32, Heavy Duty)"   // for messages, when the app can't place it
}
```

- **The product is a tab, not a version.** The app inserts from the version of that Part Studio its library loaded
  last, so a library tweak released as a new version reaches every declaration with no FeatureScript release. The
  configuration is the app's selection (parameter id to option id), as the app stores and sends it: a declaration
  stays good while the studio keeps those ids, which [Keeping referenced parts stable](#keeping-referenced-parts-stable)
  guards. A FeatureScript is released only to offer a new product, or a new kind.
- **`spec`** is what the feature built for the part, so the app can say a product doesn't fit (a 3/8" ring on a 1/2"
  groove) rather than insert it.
- **`version`** is the attribute's format. A new optional field leaves it as it is; a field changing meaning bumps
  it, and the app reads every version it has read before (the attribute exception in `AGENTS.md`).

It's set in up to two places, each with its own job: on a host connector, and on an Auto Assembly body.

### The host connector: placing it with no mates

Where the hardware attaches to a part the feature made or cut, the feature puts a mate connector on that part,
carrying the declaration: the shaft's end, for its screw; the groove, for its ring; the plate, for a motor. It comes
into the assembly with that part, so Auto Assembly can place and fasten every screw, ring and motor on it with nobody
mating anything. It's named by the
feature's id and a fixed name (`firstEnd`, `secondEnd`), so a mate to it survives the feature being edited.

A gear has no host in its feature, so it has no host connector: its Auto Assembly body carries it (below).

### Auto Assembly bodies

Only parts big enough to plan around get one: gears, motors, bearings and bushings. Simple hardware (end screws,
e-clips, retaining rings, screws and bolts) gets a host connector alone; Auto Assembly places it, and nothing in the
Part Studio stands for it.

A feature declaring such a part has an **Auto Assembly body** checkbox, on by default and remembered (Robot motor's
builds its block motor). An Auto Assembly body is a separate, simple solid part for the part's envelope, which carries the declaration too:

| Kind | Auto Assembly body |
| --- | --- |
| Gear | One extrude of one sketch: its pitch circle, with its bore as an inner loop, as thick as the gear. Two meshing gears' Auto Assembly bodies touch where their teeth would mesh |
| Motor, gearbox | FRCDesign's Block Motor, Block MAXPlanetary or Block PlanetaryX, configured for it, or Robot motor's own envelope where those have none |

The gear's Auto Assembly body has no involutes, fillets or cuts, so a gearbox of them regenerates fast; it's also what Robot
gear builds when simplified (see [Simplification](#simplification)).

Every Auto Assembly body is named as its product, so the Part Studio's parts list reads as the hardware does. It's excluded from
the bill of materials (`PropertyType.EXCLUDE_FROM_BOM`), since the bill counts the real part once it's placed, and all
Auto Assembly bodies share one translucent appearance, so none is mistaken for a part to make.

### An Auto Assembly body's mate connectors map to the real part's

An Auto Assembly body has its own mate connectors, each named and placed where the library part has one of the same name. So in
an assembly an Auto Assembly body is mated like any part: a person inserts or copies it, fastens it where it goes, and Auto
Assembly later swaps it for the real part on those same mates. A host connector is named and placed as the part's
connector it takes, so it's fastened to the same way.

| Kind | Connectors (on the Auto Assembly body, and on the library part) |
| --- | --- |
| Shaft end screw | `seat`: the center of the head's underside, Z from shank to head (the fasteners' convention) |
| Retaining ring, e-clip | `face`: the center of one flat face, Z into the ring, as a washer's; X toward an e-clip's opening |
| Gear | `bottom` and `top`: on its axis, on each face, Z out of the gear; X through a tooth |
| Motor, gearbox | `face`: the center of its mounting face, Z into the motor, X to the right of its face as its drawing shows it (Robot motor's tables' convention) |
| Bearing, bushing | `mateConnector`: where it seats, as FRCDesign's bearings part set's is (`featurescripts/bearings/`): on its axis, at a flange's underside or a radial bearing's top face, Z out of the hole |

Robot gear's profile centers a tooth on its sketch's X axis, so a library gear whose connectors' X goes through a tooth
meshes as modeled. The library's own studios name their connectors with Mate connector features of those names; a
generated part set's studios make them, and its manifest names them, as the fasteners' do.

A host connector is placed to match: the shaft's end face's center, Z out of the shaft, for an end screw's `seat`; the
groove's wall nearer the shaft's end, Z into the groove, for a ring's `face`; the plate's face under the motor's, Z
out of the plate, for a motor's `face`.

### Placing declared parts

Auto Assembly gains declared parts beside holes, run the same way:

1. **Scan.** A second scan beside `hole-scan.fs`, `part-scan.fs`, returns each body with an `frcAutoAssembly`
   attribute: its part id, whether it's an Auto Assembly body or a host connector, its declaration, and each of its connectors'
   names, coordinate systems and ids as a fasten mate names them (`lastModifyingOperationId`, joined with `.`). It's
   cached by the same immutable keys as the hole scan.
2. **Pick.** Picking an Auto Assembly body, a part with host connectors, or a whole instance takes every declaration in it;
   picking nothing takes every one in the assembly. A host connector already fastened to, or with an Auto Assembly body on it, is
   left to that.
3. **Resolve.** Each declaration's document and element are looked up among the loaded insertables, giving the
   version the library loaded last, and its configuration is parsed as any selection is (`toSelection`). A studio the
   library doesn't have, a configuration it doesn't offer, a connector the library part lacks, or a part that doesn't
   fit its `spec`, gets no part, with why.
4. **Insert** each part where its Auto Assembly body is, or at its host connector, from that version, in the run's one
   `transformedinstances` call.
5. **Mate.**
    - In place of an Auto Assembly body: each mate naming one of the Auto Assembly body's connectors is rewritten to name the real part's
      connector of the same name, so the part lands where the Auto Assembly body was and keeps its mates. Then the Auto Assembly body,
      which nothing references now, is deleted.
    - At a host connector: a fasten mate from it to the part's connector.
6. **Record** one use per part placed, with a `UseTarget` of its own, so the dashboard can show declared parts beside
   fastened holes.

Swapping back, to an Auto Assembly body, is the same in reverse, which simplification uses for assemblies (below).

### Keeping referenced parts stable

robot-code's release writes, in `featurescripts.json`, every library product its features' tables reference: each
studio's document and element, and the parameter and option ids used. The app reads it with the rest of the manifest,
and then:

- An insertable a released FeatureScript references shows a **Used by Auto Assembly** badge on the admin card, saying
  which features reference it, and that its configuration's ids must not change.
- Loading a referenced studio whose parameter or option ids no longer include ones referenced raises a build issue
  naming the features and options affected, on the studio and on the admin card, before anyone hits it in a run.

Renaming an option's name is a tweak and changes nothing; deleting an option and making it again changes its id. A
generated part set never changes a released option id (`AGENTS.md`).

### Where the products come from

- **Generated part sets**, for simple parts: e-clips and retaining rings first, then end screws, and eventually
  bearings. They join the fasteners part set (or one beside it), so each studio has its connectors where the
  convention says, and one catalog writes the feature's table, the studios' configurations and the manifest.
  FRCDesignLib has no e-clip for goBILDA's 8 mm and 12 mm REX shafts; generated, it would.
- **The library's own studios and assemblies**, for parts whose model is the point: gears, with their vendors'
  pockets and hubs, and motors. Each gets its connectors added in FRCDesignLib. A studio whose connector a configuration loses is caught by the
  library's load as it is today (`UNSTABLE_MATE_CONNECTOR`). Until a studio has them, its gears are declared and drawn,
  and Auto Assembly says the part can't be placed.

The features' product lists are generated, as the fasteners' tables are (see
[fs-cli.md](fs-cli.md#generating-lookup-tables)): a Python definition reads a snapshot of the library from FRCDesign's
public API (`/api/library-data` and `/api/configuration/insertable/{id}`, as `fastenerTables.py fetch` does), and
writes each product's document, element, configuration, name and Auto Assembly body dimensions into a `*.gen.fs` table, with no
version. A gear's product also sets its teeth, pitch and bore.

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

Robot bearing hat is renamed **Robot bearing**: its Feature Type Name and its file (`fs mv` to
`bearing/robotBearing.fs`), never its constant, `robotBearingHat`, which documents store (`docs/featurescript-style.md`,
"Released feature constants"). **Robot bushing** is new. They share their placement in `core/bearing.fs`; Robot bushing
is Robot bearing without retention.

### What both do

Both now cut the bore, as Robot motor cuts its face, rather than taking one already cut. The dialog, in Robot motor's
shape and from the same `core/` predicates:

| Group | Parameters |
| --- | --- |
| Bearing (Bushing) | **Bearing**: one of FRCDesign's bearings part set's FRC bearings (`featurescripts/bearings/`), by Vendor, Type, Bore, Size, Dimensions and Seal, skipping a level with one option (`robotBearingTable`); or **Bushing**: one of FRCDesign's bushings part set's (`featurescripts/bushings/`), by Vendor, Bore, Size, Material and Dimensions, the same way (`bushingTable`). Robot bearing's then has Bore fit and its clearance (`boreFitPredicate`, `core/fit.fs`); Robot bushing's has none, as a COTS bushing is pressed in |
| Position | The location (`locationPredicate`: a sketch point, circle, or mate connector), Merge scope (`holeMergeScopePredicate`), Flip primary axis and Reorient secondary axis (`axisOrientationPredicate`), Angle reference and Angle (`angleReferencePredicate`, `angleOffsetPredicate`), Starting offset (`startOffsetPredicate`) |
| Retention (Robot bearing) | Below |
| Other options | Auto Assembly body |

What they do:

1. **Editing logic**: `mountingEditLogic`, as Robot motor's: std's hole heuristics set the merge scope to the parts at
   the location, and Flip primary axis so the bore goes into them, unless they've been set.
2. **The plane**: the location's, offset, turned to the angle reference and Angle, and flipped; its normal points out
   of the face the bearing sits on.
3. **The bore**: a circle at the product's outside diameter plus Bore fit's clearance (a bushing's, at its outside
   diameter), extruded through the merge
   scope behind the plane and subtracted from it, in one boolean, as Robot motor's pilot is. A flanged product's
   flange sits on the face; the bore is its body's diameter, not its flange's. The part the plane is on, of the merge
   scope's, is the host.
4. **Declaration**: a host connector on the host at the bore's center, on the plane, Z out of the bore, X the plane's
   (so the angle reference turns it): the bearing set's `mateConnector`. A bearing's product is its set's studio, with
   its record's configuration; until the set is released, the studio has no document, and the feature declares
   nothing and says so.
5. **Auto Assembly body**: a ring at the part's outside diameter, its hole at its bore's corners (the set's
   `boreRadius`; a bushing's at its nominal size), into the bore, and a flanged bearing's flange on the face, at the
   set's flange sizes: one extrude each.
6. **Retention** (Robot bearing): below.

`"Auto Assembly" : true` on both. Neither allows simplification: their Auto Assembly bodies are already as simple as they get.

### Retention (Robot bearing)

**Retention** is a checkbox driving its own group (`"Driving Parameter" : "retention"`), off by default and remembered.
Off, the bearing is cut and declared with nothing holding it. On, its group starts with a **Retention type**, Hat or
Screws. Both use the Position group's angle reference and Angle, and cut their holes through the same merge scope.

**Hat** is the old feature's: COTS, Standard or Custom hat (a dropdown in the group), its mounting holes and their hole
tables. A standard or COTS hat's size is **Hat centers** (1.25, 1.5 or 2 in.); every hat's bore is the bearing's. Its
Auto Assembly options:

- A COTS hat declares itself: WCP's Bearing Retention Hat for the bearing's outside diameter at Hat centers (0.875 in.
  at 1.25 or 2 in., 1.125 in. at 1.5 or 2 in.), or an error. Its connector goes on the bearing's part at the bore's
  center, X toward a mounting hole.
- **Declare screws**, for a standard or custom hat: a screw at each mounting hole, seated on the hat's top, from the
  fasteners part set: Screw vendor, Screw head, Screw finish, and Screw length or **Automatic length** (no length in
  the declaration, for Auto Assembly to size from what the hole goes through). #10-32 only, as the hat's holes are;
  24 tpi tapped holes declare none, with an info saying why. Each is on a connector named as the fasteners' own
  (`mateConnector`), Z out of the hole. A COTS hat's screws aren't declared, as the library doesn't give its
  thickness, so where they seat; Auto Assembly's hole fastening can fasten them.

**Screws** retains the bearing with screw heads over its outer race or flange:

- **Holes**: Tapped or Clearance, with their hole tables, as Robot bearing hat's.
- **Count**: 2 or 3, evenly around the bore (180 or 120 degrees apart), from the plane's X axis.
- **Hole circle**: the bearing's outside diameter, plus the hole, plus a 0.03 in. wall each side, so each hole clears
  the bore and a head over it overlaps the bearing (`screwPositions`); **Override hole circle** sets it by hand.
- **Declare screws**: as a printed hat's, seated on the bearing's part's face. No washer or nut is declared; Auto
  Assembly's planner adds a nut to a hole it fastens through.

### Robot bushing

The same, with the bushings part set's bushings, and no Retention group: `featurescripts/bushing/`.

The bushings part set (`featurescripts/bushings/`, `libraryBushing`, in a Bushing Part Studio) is built as the bearings'
is, from FRCDesignLib's Flanged Bushing records, with flanges from The Thrifty Bot's STEP files and FRCDesignLib's
models of WCP's. It asks Vendor, Bore, Size, Material, then Dimensions, as the bearings' does; every vendor
makes both bores and several sizes, so the bore and size always show. It offers
WCP's and ThriftyBot's, and Generic's of the same: VEX no longer sells bushings, and REV sells one.

### What changes for documents

Robot bearing hat's released documents select a bore's edge, which Robot bearing no longer takes. Following
`AGENTS.md`'s "Backwards compatibility", the parameters change with no migration: a document updating to it shows the
feature's error until its location is picked again.

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

Each of these is unverified, and the design leans on it:

1. A fasten mate made through the API to a connector a custom feature made with a nested id (`id + "firstEnd"` inside
   a feature) names it as `<featureId>.firstEnd`, and keeps resolving after the feature is edited.
2. `lastModifyingOperationId` of that connector, in an evaluated script, gives the same id.
3. An attribute set on a solid and on a mate connector body can be read back by an evaluated script.
4. A mate's queries can be rewritten through the API (updating the assembly feature), so it names another instance's
   connector, and the instance moves onto it.
5. A FeatureScript reads a configuration variable with `getVariable`, and its value changes with the configuration.
6. A Derived feature's configuration can be set to an expression of the Part Studio's own variable (`#Simplified`).
7. The API can set an assembly's configured instances: an instance's configuration per the assembly's configuration.
8. The API can add a configuration input to an assembly, and configure an instance's suppression by it.
9. An Auto Assembly body with Exclude from BOM is left out of an assembly's bill of materials.
10. A library gear studio's mate connectors stay put across its configurations.
11. A Part Studio configuration variable named `Simplified` and an assembly checkbox input named `Simplified` can be
    passed from one to the other when an assembly configures its instances.

## Still to decide

- **Which part sets first.** Proposed: e-clips and retaining rings, then end screws, as part sets; gears from the
  library's studios, with connectors added by hand.
- **What's left, in order.** The Onshape checks; then Robot gear; Robot lighten; Robot shaft's hardware with the first
  part set; then the app: the part scan and run, the manifest's icons and references, and the Simplify menu. The
  manifest lists no referenced products yet (`autoAssembly` is a boolean): the admin card's guard needs `fs release`
  to collect them from a feature's tables.
- **Robot motor's envelopes.** Proposed: read from Block Motor's sketches through Onshape's features endpoint, a call
  or two.
