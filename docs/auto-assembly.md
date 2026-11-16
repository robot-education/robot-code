# Auto Assembly from FeatureScripts

**Status: proposal.** Nothing here is built yet. It's the contract between the FeatureScripts that declare parts and
FRCDesignApp's Auto Assembly, which places them, so both sides can be built against it.

## The idea

A FeatureScript that cuts something for a part (a tapped shaft end, a retaining ring groove, a gear's bore) also
declares the part that goes there, picked from FRCDesign's library. In the Part Studio, that part shows as a simple
**stand-in** body. In an assembly, Auto Assembly swaps each stand-in for the real library part, fastened to a mate
connector the feature put on the host part. The person picks parts once, in the feature where they're designing, and
the assembly fills itself in.

The first declarations:

- **Robot shaft**: a shaft end screw in a tapped end, and a retaining ring or e-clip in a groove.
- **Robot gear**: the COTS gear its gear stands in for.

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
  - Gears: 41 Part Studios by pitch, bore and vendor (WCP, REV, AndyMark, VEX, TTB), about 600 configurations, named
    like `40t Pocketed Steel`.

  None of them has a mate connector (`supportsFasten` is false for every one), so nothing can be fastened to them
  yet.
- **2023's `auto-assembly` branch** of this repo prototyped the same shape: mate connectors carrying an attribute
  naming a part, read by evaluating FeatureScript, inserted and fastened by a Python script.

## The stand-in

What a declaration makes in the Part Studio, in three pieces with separate jobs.

### The reference: a mate connector on the host part

The assembly's mate goes to a mate connector the feature makes, owned by the part the hardware attaches to: the
shaft for its end screw and its ring. Owned by the host, it stays put whatever happens to the stand-in, and it's
named by the feature's id and a fixed name (`firstEnd`, `secondEnd`), so a mate to it survives the feature being
edited. A gear has no host part in its feature, so its own gear owns its connector (Robot gear's existing one).

Each kind of part has one convention for where its connector goes, which the library part's own connector must
follow: a fasten mate lines up the two connectors, origin on origin, Z on Z, X on X.

| Kind | Host's connector | Library part's connector |
| --- | --- | --- |
| Shaft end screw | Center of the shaft's end face, Z out of the shaft | Center of the head's underside, Z from shank to head (the fasteners' convention) |
| Retaining ring, e-clip | On the shaft's axis, in the groove's middle plane, Z toward the shaft's nearer end, X toward a flat of a hex | Center of the ring, in its middle plane, X toward an e-clip's opening |
| Gear | Its gear's center, on its bottom face, Z along its axis into the gear (Robot gear's connector) | The same spot on the vendor's gear |

A gear's X axis decides how its teeth line up with the gear it meshes with. Robot gear's profile centers a tooth on
its sketch's X axis, which is its connector's; a library gear whose connector's X goes through a tooth meshes as
modeled.

### The declaration: an attribute

The connector carries an attribute, `frcAutoAssembly`, saying what goes there:

```
{
    "version" : 1,
    "kind" : AutoAssemblyKind.SHAFT_END_SCREW,     // SHAFT_END_SCREW, RETAINING_RING, E_CLIP, GEAR
    "product" : {                                  // the library part: its Part Studio and configuration
        "documentId" : "...",
        "elementId" : "...",
        "configuration" : { "Configuration" : "_1_2__L___10_32__Heavy_Duty_" },
        "name" : "Shaft End Screw (WCP) 1/2\" L (#10-32, Heavy Duty)"
    },
    "spec" : { "size" : "#10-32", "length" : 0.0127 * meter },   // what the feature cut for it, by kind
    "standIn" : "firstEnd"                         // the key its stand-in body's attribute carries
}
```

- `product` names a Part Studio by its document and element, not a version: the library loads versions of the same
  tabs, so the app finds the insertable by element and inserts its loaded version. The configuration is the app's
  selection: parameter id to option id, as the app stores and sends it.
- `spec` is what the feature built for the part, so the app can say a product doesn't fit (a 3/8" ring on a 1/2"
  groove) rather than insert it. Lengths are lengths, which the scan writes out as meters.
- The stand-in body gets an attribute of its own, `{ "standInFor" : "firstEnd" }`, so the app can find and hide it.

### The look: a stand-in body

A separate, simple solid part for the hardware's envelope, so the person sees it, measures to it and checks clearance
in the Part Studio, without the cost of the real model:

| Kind | Stand-in |
| --- | --- |
| Shaft end screw | A cylinder for the head, on the end face, and one for the shank, its major diameter, down the hole |
| Retaining ring, e-clip | A ring at its outer diameter, as thick as the ring, in the groove |
| Gear | Robot gear's gear itself: its teeth are what's checked against the gear it meshes with |

Every stand-in is named as its product (`Shaft End Screw (WCP) 1/2" L (#10-32, Heavy Duty)`), with its vendor and
part number, so the Part Studio's parts list reads as the hardware does. It's excluded from the bill of materials
(`PropertyType.EXCLUDE_FROM_BOM`): the bill counts the real part once Auto Assembly places it. All stand-ins share one
appearance, translucent, so a stand-in is never mistaken for a part to make.

## Choosing a product in the feature

The feature's dialog offers the library's products for what it cut, as a lookup table: a tapped #10-32 end offers
WCP's and REV's end screws by length, a 1/2" hex groove offers WCP's and McMaster-Carr's 1/2" retaining rings.
Choosing one sets the declaration's `product` and sizes the stand-in.

The tables are generated, as the fasteners' are (see [fs-cli.md](fs-cli.md#generating-lookup-tables)): a definition
in Python reads a snapshot of the library from FRCDesign's public API (`/api/library-data` and
`/api/configuration/insertable/{id}`, as `fastenerTables.py fetch` does), and writes each product's element,
configuration, name and stand-in dimensions into a `*.gen.fs` table. A product the library drops shows in the app as a
declaration it can't place, and in the next snapshot as a table entry gone.

Two sources, by kind:

- **Generated part sets**, for hardware simple enough to model from a catalog: end screws and rings. They'd join the
  fasteners part set (or one beside it), so each studio has its connector where the convention says, and the same
  catalog writes the feature's table, the studios' configurations and the manifest: nothing to keep in step by hand.
  FRCDesignLib has no e-clip for goBILDA's 8 mm and 12 mm REX shafts; generated, it would.
- **The library's own studios**, for parts whose model is the point: gears, with their vendors' pockets and hubs.
  Each gear studio needs a mate connector added in FRCDesignLib (41 studios), at its center on its bottom face, X
  through a tooth. A studio whose connector a configuration loses is caught by the library's load as it is today
  (`UNSTABLE_MATE_CONNECTOR`).

## What FRCDesignApp does

Auto Assembly gains declared parts beside holes, run the same way:

1. **Scan.** A second scan beside `hole-scan.fs`, `part-scan.fs`, returns each connector with an `frcAutoAssembly`
   attribute: its owner part's id, its coordinate system, its id as a fasten mate names it
   (`lastModifyingOperationId`, joined with `.`), and its declaration; and each stand-in's part id by its key. It's
   cached by the same immutable keys as the hole scan.
2. **Pick.** Picking a part, or a whole instance, takes every declaration in it; picking nothing takes every one in
   the assembly. A declaration already fastened to (a mate names its connector) is left alone, or replaced when its
   product changed.
3. **Plan.** Each declaration's product is looked up among the loaded insertables by document and element. One the
   library doesn't have, or whose studio has no connector, or that doesn't fit its `spec`, gets no part, with why.
4. **Insert** each product at its connector's place, in the run's one `transformedinstances` call, with its
   configuration.
5. **Fasten** each to its declaration's connector: the host's instance path, and the connector's id.
6. **Hide** each stand-in it placed a part for. A hidden stand-in keeps its mates, and is what a later run compares
   against.
7. **Record** one use per part placed, with a `UseTarget` of its own, so the dashboard can show declared parts beside
   fastened holes.

## Before building: what to try in Onshape

Each of these is unverified, and the design leans on it:

1. A fasten mate made through the API to a connector a custom feature made with a nested id (`id + "firstEnd"` inside
   a feature) names it as `<featureId>.firstEnd`, and keeps resolving after the feature is edited.
2. `lastModifyingOperationId` of that connector, in an evaluated script, gives the same id.
3. An attribute set on a mate connector body can be read back by an evaluated script (`getAttributes`).
4. A mate to a hidden instance holds, and the hidden instance is left out of the assembly's mass properties.
5. A stand-in with Exclude from BOM is left out of an assembly's bill of materials.
6. A library gear studio's mate connector, placed on its bottom face's center, stays put across its configurations.

## Decisions to make

- **Stand-ins are hidden, not deleted, when their part is placed.** Hidden, they keep the gear's mates and give a
  later run something to compare; deleted, the assembly is tidier, but a gear's mates go with it. Proposed: hidden.
- **Hardware comes from generated part sets; gears from the library's studios.** Proposed as above; the alternative is
  adding connectors to the library's hand-modeled end screws and rings too, and generating only their tables.
- **A product is chosen in the feature, always.** The alternative is letting a declaration name only its kind and
  spec, and the app choosing a product by a vendor preference set in Auto Assembly. Proposed: chosen in the feature,
  defaulting to the most inserted, with the spec checked by the app.
