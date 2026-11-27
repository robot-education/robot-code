# Auto Assembly from FeatureScripts

**Status: Robot bearing and Robot bushing declare their parts; FRCDesignApp places them.** Built here: the
`"Auto Assembly"` annotation and its check, `core/autoAssembly.fs`, `core/bearing.fs`, the library snapshots
(`fs_cli/library.py`), the bushings part set, Robot bearing and Robot bushing. Built in FRCDesignApp: **Assemble
FeatureScript parts**, on its Auto Assembly page, which inserts each declared part and fastens it to its host connector
(FRCDesignApp's `docs/architecture/auto-assembly.md`). Not yet: swapping an Auto Assembly body for its part, and every
other feature (see [Later](#later)).

This is the contract between robot-code's FeatureScripts and FRCDesignApp for **declared parts**: a FeatureScript
names the library part that goes where it cut something, and Auto Assembly puts that part in an assembly.

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

The first declarations are **Robot bearing** (Robot bearing hat, renamed) and **Robot bushing** (new): the bearing or
bushing in the bore it cuts, and the bearing's retention: its hat, or its screws (see
[Robot bearing and Robot bushing](#robot-bearing-and-robot-bushing)).

### The declaration

A declaration is an attribute, `frcAutoAssembly`, naming a library Part Studio and its configuration:

```
{
    "version" : 1,
    "kind" : AutoAssemblyKind.BEARING,      // BEARING, BUSHING, BEARING_HAT, SCREW
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
  The app doesn't check it yet.
- **`version`** is the attribute's format. A new optional field leaves it as it is; a field changing meaning bumps
  it, and the app reads every version it has read before (the attribute exception in `AGENTS.md`). The app leaves out
  a connector whose version it doesn't know.

It's set in up to two places, each with its own job: on a host connector, and on an Auto Assembly body.

### The host connector: placing it with no mates

Where the part attaches to a part the feature cut, the feature puts a mate connector on that part, carrying the
declaration: the plate, for a bearing or bushing in its bore. It comes into the assembly with that part, so Auto
Assembly can place and fasten every bearing on it with nobody mating anything. It's named by the feature's id and a
fixed name (`place` then `mateConnector`), so a mate to it survives the feature being edited.

### Auto Assembly bodies

A feature declaring a bearing or bushing has an **Auto Assembly body** checkbox, on by default and remembered. An Auto
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
| Bearing, bushing | `mateConnector`: where it seats, as FRCDesign's bearings part set's is (`featurescripts/bearings/`): on its axis, at a flange's underside or a radial bearing's top face, Z out of the hole |
| Bearing hat | `face`: the center of its bore on the face it sits on, Z out of the face, X toward a mounting hole |
| Screw | `mateConnector`: the center of the head's underside, Z from shank to head (the fasteners' convention) |

The library's own studios name their connectors with Mate connector features of those names; a generated part set's
studios make them, and its manifest names them, as the fasteners' do.

### Placing declared parts

FRCDesignApp's **Assemble FeatureScript parts**, a button on its Auto Assembly page, places every declared part in the
assembly, nothing picked. Its run:

1. **Scan.** `part-scan.fs` returns each mate connector carrying an `frcAutoAssembly` attribute on a part: the part's
   id, the connector's declaration, its coordinate system, and its id as a fasten mate names it
   (`lastModifyingOperationId`, joined with `.`). A connector on an Auto Assembly body is left out. It's cached by the
   same immutable keys as the hole scan.
2. **Resolve.** Each declaration's document and element are looked up among the loaded insertables, giving the
   version the library loaded last, and its configuration is parsed as any selection is (`toSelection`). A studio the
   library doesn't have, a configuration it doesn't offer, a part with no connector, or a screw of automatic length,
   gets no part, with why. A connector with its part already on it is left alone.
3. **Insert** each part at its host connector, from that version, in the run's one `transformedinstances` call.
4. **Mate** each part's connector to its host connector with a fasten mate.
5. **Record** one use per part placed, as an insert from Auto Assembly's FeatureScript parts, so the dashboard shows
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

- **Generated part sets**, for bearings and bushings (`featurescripts/bearings/`, `featurescripts/bushings/`). Each
  studio has its connector where the convention says, and one catalog writes the feature's table, the studios'
  configurations and the manifest. Until a set is released, its parts have no document, and the features declare
  nothing and say so.
- **The library's own studios**, for WCP's bearing hats, from a snapshot of FRCDesignLib (`fs_cli/library.py`). A
  studio whose connector a configuration loses is caught by the library's load as it is today
  (`UNSTABLE_MATE_CONNECTOR`).

The library's product lists are generated, as the fasteners' tables are (see
[fs-cli.md](fs-cli.md#generating-lookup-tables)): a Python definition reads a snapshot of the library from FRCDesign's
public API (`/api/library-data` and `/api/configuration/insertable/{id}`, as `fastenerTables.py fetch` does), and
writes each product's document, element, configuration and name into a `*.gen.fs` table, with no version.

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

`"Auto Assembly" : true` on both.

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

## The Auto Assembly icon in the FeatureScripts tab

A feature's card can show an **Auto Assembly** icon (the panel's `assembly.svg`) beside its name when it declares
parts, with a hover saying what it means. A feature says so in its own annotation, beside its name:

```
annotation { "Feature Type Name" : "Robot bushing",
        "Feature Type Description" : "..." ~ CREDIT,
        "Auto Assembly" : true,
        "Icon" : RobotBushingIcon::BLOB_DATA }
export const robotBushing = defineFeature(...);
```

The key is ours: Onshape ignores annotation keys it doesn't know, as it ignores `"Set By"`. `fs release` reads it as it
reads `"Feature Type Name"` (`fs_cli/frontend.py`), and writes it into the feature's manifest entry as `"autoAssembly"
: true`. A key that's absent is false. `fs check` keeps it honest:

- It joins its known annotation keys (`ANNOTATION_KEYS`, beside `"Set By"`), allowed only in a `defineFeature`'s
  annotation, with a boolean value.
- `"Auto Assembly" : true` needs the feature to declare parts (call `declarePart`, `core/autoAssembly.fs`), and a
  feature that declares parts needs the key. Either way round, it's an error naming what's missing, so a feature never
  shows an icon for something it doesn't do, or hides one it does.

## What to try in Onshape

Each of these is unverified, and the run leans on it:

1. A fasten mate made through the API to a connector a custom feature made with a nested id (`id + "place" +
   "mateConnector"`) names it as `<featureId>.place.mateConnector`, and keeps resolving after the feature is edited.
2. `lastModifyingOperationId` of that connector, in an evaluated script, gives the same id.
3. An attribute set on a solid and on a mate connector body can be read back by an evaluated script, and
   `qMateConnectorsOfParts` finds a connector a feature attached to a part with `owner`.
4. An Auto Assembly body with Exclude from BOM is left out of an assembly's bill of materials.

## Later

Each its own branch, on this contract:

- **Swapping Auto Assembly bodies**: a mate's queries rewritten through the API to name the real part's connector.
- **Robot motor**: declaring the motor or gearbox whose face it cuts, from a snapshot of FRCDesignLib's Motors & Servos,
  with Block Motor as its Auto Assembly body.
- **Simplification**: a Simplified configuration parameter that features follow by building less (`core/simplify.fs`,
  an `"Allow simplification"` checkbox, a `"Simplification"` annotation key), and a Simplify menu in the app.
- **Robot gear, Robot shaft's hardware and Robot lighten**: gears from the library's studios, end screws, retaining
  rings and e-clips from part sets of their own.
