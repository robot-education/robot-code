# Auto Assembly from FeatureScripts, and simplification

**Status: proposal.** Nothing here is built yet. It's the contract between robot-code's FeatureScripts and
FRCDesignApp for two things built together:

- **Declared parts**: a FeatureScript names the library part that goes where it cut something, and Auto Assembly puts
  that part in an assembly.
- **Simplification**: one switch, set per Part Studio or assembly and carried down into what they contain, that makes
  the features which allow it build something lighter.

They meet in the stand-in: the simple body a declaration draws is what a simplified feature builds too.

## What's there already

- **FRCDesignApp's Auto Assembly** (its `ccr-028f42f9-gzdpdc` branch, not merged; `docs/architecture/auto-assembly.md`
  there). It fastens library screws, washers and nuts into holes picked in an assembly: it scans Part Studios with
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

  None of them has a mate connector yet (`supportsFasten` is false for every one).
- **The FeatureScripts tab** reads `featurescripts.json`, the manifest robot-code's `fs release` writes into the
  frontend document (FRCDesignApp's `docs/architecture/featurescripts.md`).
- **2023's `auto-assembly` branch** of this repo prototyped declared parts: mate connectors carrying an attribute
  naming a part, read by evaluating FeatureScript, inserted and fastened by a Python script.

## Declared parts

The first declarations:

- **Robot shaft**: a shaft end screw in a tapped end, and a retaining ring or e-clip in a groove.
- **Robot gear**: a COTS gear in place of its modeled one.

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

It's set in up to two places, each with its own job: on a host connector, and on a stand-in.

### The host connector: placing it with no mates

Where the hardware attaches to a part the feature made, the feature puts a mate connector on that part, carrying the
declaration: the shaft's end, for its screw; the groove, for its ring. It comes into the assembly with the shaft, so
Auto Assembly can place and fasten every screw and ring on a shaft with nobody mating anything. It's named by the
feature's id and a fixed name (`firstEnd`, `secondEnd`), so a mate to it survives the feature being edited.

A gear has no host in its feature, so it has no host connector: its stand-in carries it (below).

### Stand-ins

A feature with declarations has a **Stand-ins** checkbox, on by default and remembered. A stand-in is a separate,
simple solid part for the hardware's envelope, which carries the declaration too:

| Kind | Stand-in |
| --- | --- |
| Shaft end screw | A cylinder for the head, on the end face, and one for the shank, its major diameter, down the hole |
| Retaining ring, e-clip | A ring at its outer diameter, as thick as the ring, in the groove |
| Gear | One extrude of one sketch: its pitch circle, with its bore as an inner loop, as thick as the gear. Two meshing gears' stand-ins touch where their teeth would mesh |

The gear's stand-in has no involutes, fillets or cuts, so a gearbox of them regenerates fast; it's also what Robot
gear builds when simplified (see [Simplification](#simplification)).

Every stand-in is named as its product, so the Part Studio's parts list reads as the hardware does. It's excluded from
the bill of materials (`PropertyType.EXCLUDE_FROM_BOM`), since the bill counts the real part once it's placed, and all
stand-ins share one translucent appearance, so none is mistaken for a part to make.

### A stand-in's mate connectors map to the real part's

A stand-in has its own mate connectors, each named and placed where the library part has one of the same name. So in
an assembly a stand-in is mated like any part: a person inserts or copies it, fastens it where it goes, and Auto
Assembly later swaps it for the real part on those same mates.

| Kind | Connectors (on the stand-in, and on the library part) |
| --- | --- |
| Shaft end screw | `seat`: the center of the head's underside, Z from shank to head (the fasteners' convention) |
| Retaining ring, e-clip | `face`: the center of one flat face, Z into the ring, as a washer's; X toward an e-clip's opening |
| Gear | `bottom` and `top`: on its axis, on each face, Z out of the gear; X through a tooth |

Robot gear's profile centers a tooth on its sketch's X axis, so a library gear whose connectors' X goes through a tooth
meshes as modeled. The library's own studios name their connectors with Mate connector features of those names; a
generated part set's studios make them, and its manifest names them, as the fasteners' do.

A host connector is placed to match: the shaft's end face's center, Z out of the shaft, for `seat`; the groove's wall
nearer the shaft's end, Z into the groove, for `face`. A fasten made by hand between a stand-in and its host picks the
same two spots.

### Placing declared parts

Auto Assembly gains declared parts beside holes, run the same way:

1. **Scan.** A second scan beside `hole-scan.fs`, `part-scan.fs`, returns each body with an `frcAutoAssembly`
   attribute: its part id, whether it's a stand-in or a host connector, its declaration, and each of its connectors'
   names, coordinate systems and ids as a fasten mate names them (`lastModifyingOperationId`, joined with `.`). It's
   cached by the same immutable keys as the hole scan.
2. **Pick.** Picking a stand-in, a part with host connectors, or a whole instance takes every declaration in it;
   picking nothing takes every one in the assembly. A host connector already fastened to, or with a stand-in on it, is
   left to that.
3. **Resolve.** Each declaration's document and element are looked up among the loaded insertables, giving the
   version the library loaded last, and its configuration is parsed as any selection is (`toSelection`). A studio the
   library doesn't have, a configuration it doesn't offer, a connector the library part lacks, or a part that doesn't
   fit its `spec`, gets no part, with why.
4. **Insert** each part where its stand-in is, or at its host connector, from that version, in the run's one
   `transformedinstances` call.
5. **Mate.**
    - In place of a stand-in: each mate naming one of the stand-in's connectors is rewritten to name the real part's
      connector of the same name, so the part lands where the stand-in was and keeps its mates. Then the stand-in,
      which nothing references now, is deleted.
    - At a host connector: a fasten mate from it to the part's connector.
6. **Record** one use per part placed, with a `UseTarget` of its own, so the dashboard can show declared parts beside
   fastened holes.

Swapping back, to a stand-in, is the same in reverse, which simplification uses for assemblies (below).

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
- **The library's own studios**, for parts whose model is the point: gears, with their vendors' pockets and hubs. Each
  studio gets its connectors added in FRCDesignLib. A studio whose connector a configuration loses is caught by the
  library's load as it is today (`UNSTABLE_MATE_CONNECTOR`). Until a studio has them, its gears are declared and drawn,
  and Auto Assembly says the part can't be placed.

The features' product lists are generated, as the fasteners' tables are (see
[fs-cli.md](fs-cli.md#generating-lookup-tables)): a Python definition reads a snapshot of the library from FRCDesign's
public API (`/api/library-data` and `/api/configuration/insertable/{id}`, as `fastenerTables.py fetch` does), and
writes each product's document, element, configuration, name and stand-in dimensions into a `*.gen.fs` table, with no
version. A gear's product also sets its teeth, pitch and bore.

## Simplification

A large robot's assembly regenerates slowly, mostly because of detail nobody needs while laying out the robot: gear
teeth, lightening pockets, belt and chain teeth, threads, fillets. Simplification is a switch that turns that detail
off, everywhere at once.

### The switch: a configuration variable

A Part Studio is simplified by a configuration variable, `simplified`, a number: 0 builds everything, 1 simplifies.
A FeatureScript reads it as a variable (`getVariable(context, "simplified")`, as `core/logging.fs` reads `#logging`),
and a Part Studio without it is never simplified. A number rather than a checkbox, so a later level (2, an envelope)
needs no new switch.

`core/simplify.fs` holds it for features:

- `simplificationPredicate(definition)`: **Allow simplification**, a checkbox in the feature's last group, on by
  default and remembered. Off, the feature always builds in full: for a part whose detail matters (a gear being
  checked for interference, a plate going to the router).
- `isSimplified(context, definition)`: whether to build simplified: allowed, and the Part Studio's `simplified` is 1.

What each feature builds when simplified:

| Feature | Simplified |
| --- | --- |
| Robot gear | Its pitch-circle disc with its bore, as a stand-in's (a bevel gear: its pitch cone's frustum) |
| Robot lighten | Nothing: the part, unpocketed |
| Later: Robot belt, Robot chain, Robot pulley, Robot sprocket | Their pitch path or pitch circle, without teeth |
| Later: Robot shaft | The shaft, with no end holes, grooves or flutes |

Mass changes when a part is simplified, so the app never reports mass, and nothing should be measured, from a
simplified configuration.

### The Simplify menu

A **Simplify** button in the app's top toolbar opens a menu acting on the Part Studio or assembly the app was opened
from:

- **Add simplification**: adds the `simplified` configuration variable, at 0. In a Part Studio, it also offers to
  suppress std features when simplified (fillets, chamfers, cosmetic threads, and any others picked): each gets a
  suppression configured by the variable.
- **Include what's inside**: carries the variable into what the element uses, so simplifying a top assembly
  simplifies everything below it:
  - In a Part Studio, each Derived feature from a Part Studio with the variable gets its configuration set from the
    Part Studio's own (`#simplified`); a Part Studio without it is offered Add simplification first, when it's in a
    document the person can edit.
  - In an assembly, each instance of an element with the variable follows the assembly's own `simplified`, down
    through its subassemblies. The API can't change an instance's configuration, so how depends on what Onshape
    allows (see [What to try in Onshape](#what-to-try-in-onshape)): by the assembly's configured instances, if the API
    can set them; otherwise by replacing each instance with one in the other configuration, keeping its mates, as
    placing a declared part does.
- **Simplified / Full**: switches the element's own configuration, for the person viewing it.
- In an assembly, **Swap placed parts for stand-ins** and back: the declared parts' real models are often the
  heaviest left, and a stand-in is lighter still than a simplified Part Studio.

The menu lists what it would change before it changes anything, and each action records its use, as every feature
does (FRCDesignApp's `AGENTS.md`).

### Library parts

A library Part Studio or assembly can have the variable too. An admin marks it on the insertable, and the app then
offers **Insert simplified** beside Insert, remembered per person, and Include what's inside sets it on library parts
already in an assembly. A library part's simplified configuration is the admin's to model: a gear studio's might
suppress its pockets and teeth.

### Ideas, for later

- **Levels.** 1 drops detail, 2 builds envelopes (a gear as a cylinder at its outside diameter, a plate as its
  outline), for packaging studies at robot scale.
- **Simplify by cost.** The menu shows each Part Studio's regeneration time (Onshape's per-feature timings), so the
  person simplifies where it pays.
- **Simplified by default.** An assembly's own setting, so new instances arrive simplified and the full configuration
  is used for drawings and exports.

## Icons in the FeatureScripts tab

Each feature's card shows two small icons beside its name: **Auto Assembly** (the panel's `assembly.svg`) when it
declares parts, and **Simplified** (a new icon: the gear's own outline reduced to a disc) when it allows
simplification, each with a hover saying what it means.

`fs release` decides both from the feature's precondition, which it already parses (`fs ui`), and writes them into the
feature's manifest entry, beside the products it references:

```
"autoAssembly" : { "products" : [ { "documentId" : "...", "elementId" : "...", "configurations" : [ ... ] } ] },
"simplification" : true
```

Declaring parts is calling `standInPredicate` (`core/autoAssembly.fs`); allowing simplification is calling
`simplificationPredicate`. Library parts with the variable show the Simplified icon on their cards too.

## What to try in Onshape

Each of these is unverified, and the design leans on it:

1. A fasten mate made through the API to a connector a custom feature made with a nested id (`id + "firstEnd"` inside
   a feature) names it as `<featureId>.firstEnd`, and keeps resolving after the feature is edited.
2. `lastModifyingOperationId` of that connector, in an evaluated script, gives the same id.
3. An attribute set on a solid and on a mate connector body can be read back by an evaluated script.
4. A mate's queries can be rewritten through the API (updating the assembly feature), so it names another instance's
   connector, and the instance moves onto it.
5. A FeatureScript reads a configuration variable with `getVariable`, and its value changes with the configuration.
6. A Derived feature's configuration can be set to an expression of the Part Studio's own variable (`#simplified`).
7. The API can set an assembly's configured instances: an instance's configuration per the assembly's configuration.
8. A configured suppression can be added to a Part Studio feature through the API.
9. A stand-in with Exclude from BOM is left out of an assembly's bill of materials.
10. A library gear studio's mate connectors stay put across its configurations.

## Still to decide

- **The switch's name and type.** Proposed: `simplified`, a number, 0 or 1. A shared name is what lets Include what's
  inside find the variable in Part Studios it didn't add it to.
- **Which part sets first.** Proposed: e-clips and retaining rings, then end screws, as part sets; gears from the
  library's studios, with connectors added by hand.
- **Order of work.** Proposed: the Onshape checks; `core/autoAssembly.fs` and `core/simplify.fs`; Robot gear (its
  stand-in is its simplified form, so it exercises both); Robot lighten; Robot shaft's hardware with the first part
  set; then the app: the part scan and run, the manifest's icons and references, and the Simplify menu.
