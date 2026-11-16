# Auto Assembly from FeatureScripts

**Status: proposal.** Nothing here is built yet. It's the contract between the FeatureScripts that declare parts and
FRCDesignApp's Auto Assembly, which places them, so both sides can be built against it.

## The idea

A FeatureScript that cuts something for a part (a tapped shaft end, a retaining ring groove, a gear's bore) also
declares the part that goes there: a library Part Studio, by its document and tab, and its configuration. It always
makes a mate connector for it, on the part it attaches to, and optionally a simple **stand-in** body. In an assembly,
Auto Assembly finds the declarations, inserts each part from the version of its Part Studio the library loaded last,
and fastens it to its connector. The connector is the source of truth; the stand-in only shows where the part goes.

The first declarations:

- **Robot shaft**: a shaft end screw in a tapped end, and a retaining ring or e-clip in a groove.
- **Robot gear**: a COTS gear in place of its modeled one.

## What's there already

- **FRCDesignApp's Auto Assembly** (its `ccr-028f42f9-gzdpdc` branch, not merged; `docs/architecture/auto-assembly.md`
  there). It fastens library screws, washers and nuts into holes picked in an assembly: it scans Part Studios with
  FeatureScript it evaluates (`hole-scan.fs`), plans each hole, inserts the parts in one call, and fastens each to a
  mate connector. Its decisions already say parts a FeatureScript declares should join it rather than becoming a tool
  of their own. This proposal adds a second kind of target beside holes: declared parts.
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
- **2023's `auto-assembly` branch** of this repo prototyped the same shape: mate connectors carrying an attribute
  naming a part, read by evaluating FeatureScript, inserted and fastened by a Python script.

## The declaration

### A mate connector on the host part

Every declaration makes one mate connector, whether or not it makes a stand-in. It's owned by the part the hardware
attaches to, so it comes into an assembly with that part and stays put whatever happens to the stand-in: the stand-in
can be turned off in the feature, suppressed or deleted in the assembly, and the placed part's mate holds. It's named
by the feature's id and a fixed name (`firstEnd`, `secondEnd`), so a mate to it survives the feature being edited.

| Feature | Owner |
| --- | --- |
| Robot shaft | The shaft |
| Robot gear | The part it's mounted to (a new Mount to selection: the shaft, or a hub); without one, its stand-in |

A gear's stand-in can own its connector only while it's turned on, and then the assembly must keep it: Auto Assembly
hides a stand-in that owns its connector rather than suppressing it (see [Stand-ins in the assembly](#stand-ins-in-the-assembly)).
Without Mount to, Stand-in can't be turned off.

Each kind has one convention for where the connector goes, which the library part's own connector follows: a fasten
mate lines up the two, origin on origin, Z on Z, X on X. Each is on a face the part sits against, so a fasten made by
hand picks the same two spots.

| Kind | Host's connector | Library part's connector |
| --- | --- | --- |
| Shaft end screw | Center of the shaft's end face, Z out of the shaft | Center of the head's underside, Z from shank to head (the fasteners' convention) |
| Retaining ring, e-clip | On the shaft's axis, on the groove's wall nearer the shaft's end (the one the ring bears on), Z into the groove, toward the shaft's middle; X toward a flat of a hex | Center of one flat face, Z into the ring, as a washer's; X toward an e-clip's opening |
| Gear | On its axis, on its bottom face, Z along its axis into the gear, X through a tooth (Robot gear's connector) | The same spot on the vendor's gear |

Robot gear's profile centers a tooth on its sketch's X axis, which is its connector's, so a library gear whose
connector's X goes through a tooth meshes as modeled.

### An attribute naming the part

The connector carries an attribute, `frcAutoAssembly`:

```
{
    "version" : 1,
    "kind" : AutoAssemblyKind.SHAFT_END_SCREW,  // SHAFT_END_SCREW, RETAINING_RING, E_CLIP, GEAR
    "documentId" : "...",                       // the library Part Studio, with no version
    "elementId" : "...",
    "configuration" : { "Configuration" : "_1_2__L___10_32__Heavy_Duty_" },   // parameter id to option id
    "spec" : { "size" : "#10-32", "depth" : 0.5 * inch },   // what the feature cut for it, by kind
    "name" : "Shaft End Screw (WCP) 1/2\" L (#10-32, Heavy Duty)"   // for messages, when the app can't find it
}
```

- **The product is a tab, not a version.** The app takes the version of that Part Studio its library loaded last, at
  the time of the run, so a library tweak released as a new version reaches every declaration with no FeatureScript
  release. The configuration is the app's selection (parameter id to option id), as the app stores and sends it, so
  a declaration stays good while the studio's parameter and option ids do: renaming an option is a tweak, deleting
  and re-creating it isn't. Generated part sets never change a released option id (`AGENTS.md`), and the library's
  own studios keep theirs the same way. A FeatureScript is released only to offer a new product, or a new kind.
- **`spec`** is what the feature built for the part, so the app can say a product doesn't fit (a 3/8" ring on a 1/2"
  groove) rather than insert it.
- **`version`** is the attribute's format. A new optional field leaves it as it is; a field changing meaning bumps
  it, and the app reads every version it has read before (the attribute exception in `AGENTS.md`).
- The stand-in body, if there is one, carries `{ "standInFor" : <its connector's name> }`, so the app can find it.

## Stand-ins

A feature with declarations has a **Stand-ins** checkbox, on by default and remembered. A stand-in is a separate,
simple solid part for the hardware's envelope, cheap to regenerate, so the person sees it, measures to it and checks
clearance:

| Kind | Stand-in |
| --- | --- |
| Shaft end screw | A cylinder for the head, on the end face, and one for the shank, its major diameter, down the hole |
| Retaining ring, e-clip | A ring at its outer diameter, as thick as the ring, in the groove |
| Gear | One extrude of one sketch: its pitch circle, with its bore as an inner loop, as thick as the gear. Two meshing gears' stand-ins touch where their teeth would mesh |

The gear's stand-in is the disc, not its teeth: no involutes, fillets or cuts, so a gearbox of them regenerates fast.
A rough outline (a polygon per tooth) costs one sketch with more lines, if a disc reads too little like a gear.

Every stand-in is named as its product, with its vendor and part number, so the Part Studio's parts list reads as the
hardware does. It's excluded from the bill of materials (`PropertyType.EXCLUDE_FROM_BOM`), since the bill counts the
real part once it's placed, and all stand-ins share one translucent appearance, so none is mistaken for a part to make.

### Stand-ins in the assembly

Once its part is placed, a stand-in is suppressed: a suppressed instance is out of the bill of materials and the mass
properties, and its connector isn't the one the part is fastened to. Suppressed rather than deleted, the assembly can
switch between the two: Auto Assembly can suppress every placed part and unsuppress the stand-ins for a light
assembly, and back. A stand-in that owns its connector (a gear without Mount to) is hidden instead, since suppressing
it would take its connector, and the gear's mate, with it.

## Choosing a product in the feature

The feature's dialog offers the products for what it cut, as a lookup table: a tapped #10-32 end offers WCP's and
REV's end screws by length, a 1/2" hex groove offers WCP's and McMaster-Carr's 1/2" retaining rings, and a gear
offers the gears made in its pitch, teeth and bore. Choosing one sets the declaration's product and sizes the
stand-in. A gear's product also sets its teeth, pitch and bore.

The tables are generated, as the fasteners' are (see [fs-cli.md](fs-cli.md#generating-lookup-tables)): a Python
definition reads a snapshot of the library from FRCDesign's public API (`/api/library-data` and
`/api/configuration/insertable/{id}`, as `fastenerTables.py fetch` does), and writes each product's document, element,
configuration, name and stand-in dimensions into a `*.gen.fs` table. It holds no version, so it doesn't go stale when
the library releases one.

Where the products come from:

- **Generated part sets**, for simple parts: e-clips and retaining rings first, then end screws, and eventually
  bearings. They join the fasteners part set (or one beside it), so each studio has its connector where the convention
  says, and one catalog writes the feature's table, the studios' configurations and the manifest. FRCDesignLib has no
  e-clip for goBILDA's 8 mm and 12 mm REX shafts; generated, it would.
- **The library's own studios**, for parts whose model is the point: gears, with their vendors' pockets and hubs.
  Each studio gets a mate connector added in FRCDesignLib, at the convention's spot. A studio whose connector a
  configuration loses is caught by the library's load as it is today (`UNSTABLE_MATE_CONNECTOR`). Until a studio has
  one, its gears are declared and their stand-ins drawn, and Auto Assembly says the part can't be fastened.

## What FRCDesignApp does

Auto Assembly gains declared parts beside holes, run the same way:

1. **Scan.** A second scan beside `hole-scan.fs`, `part-scan.fs`, returns each connector with an `frcAutoAssembly`
   attribute: its owner part's id, its coordinate system, its id as a fasten mate names it
   (`lastModifyingOperationId`, joined with `.`), and its declaration; and each stand-in's part id. It's cached by the
   same immutable keys as the hole scan.
2. **Pick.** Picking a part, or a whole instance, takes every declaration in it; picking nothing takes every one in
   the assembly. A declaration already fastened to (a mate names its connector) is left alone, or replaced when its
   product changed.
3. **Resolve.** Each declaration's document and element are looked up among the loaded insertables, giving the
   version the library loaded last, and its configuration is parsed as any selection is (`toSelection`). A studio the
   library doesn't have, a configuration it doesn't offer, a studio with no connector, or a part that doesn't fit its
   `spec`, gets no part, with why.
4. **Insert** each part at its connector's place, from that version, in the run's one `transformedinstances` call.
5. **Fasten** each to its declaration's connector: the host's instance path, and the connector's id.
6. **Suppress** each stand-in it placed a part for (or hide it, if it owns its connector).
7. **Record** one use per part placed, with a `UseTarget` of its own, so the dashboard can show declared parts beside
   fastened holes.

## Before building: what to try in Onshape

Each of these is unverified, and the design leans on it:

1. A fasten mate made through the API to a connector a custom feature made with a nested id (`id + "firstEnd"` inside
   a feature) names it as `<featureId>.firstEnd`, and keeps resolving after the feature is edited.
2. `lastModifyingOperationId` of that connector, in an evaluated script, gives the same id.
3. An attribute set on a mate connector body can be read back by an evaluated script (`getAttributes`).
4. Suppressing an instance through the API (the assembly `modify` endpoint) works, and leaves mates to connectors on
   other instances alone.
5. Suppressing an instance whose connector a mate uses suppresses that mate too, which is why such a stand-in is
   hidden instead.
6. A stand-in with Exclude from BOM is left out of an assembly's bill of materials, suppressed or not.
7. A library gear studio's mate connector, placed on its bottom face's center, stays put across its configurations.

## Still to decide

- **Mount to for gears.** Proposed: optional, owning the connector when given. The alternative is requiring it for a
  COTS gear, which leaves no stand-in that has to stay in the assembly.
- **Which part sets first.** Proposed: e-clips and retaining rings, then end screws, as part sets; gears from the
  library's studios, with connectors added by hand.
