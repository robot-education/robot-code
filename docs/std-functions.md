# Useful std functions

Std functions worth knowing when writing features here, with an emphasis on those std marks `@internal`. "Internal"
means std leaves them out of its published interface, not that they don't work: they're exported, features can call
them, and std's own features rely on them. `fs doc NAME` shows any of them; completion offers them, marked internal.
An internal function can change between std versions more freely than a public one. That's a reason to check its
source when bumping std (`fs refs NAME` finds where it's used), not a reason to avoid it.

Each is in `std/<module>.fs`. Those marked † are internal. Not every module is exported by `common.fs` (which most
files import): a name from one which isn't (like `ModifyFilletType`, in `modifyFillet.fs`) needs that module imported
too. `fs check` says so ("isn't defined in this file or anything it imports"); Onshape says "Variable ... not found"
only when it runs.

## Tolerances: deciding "the same" and "touching"

Decide whether two things are the same, or touch, by std's tolerances, never by a margin picked by eye (5% more,
0.1 mm past).

| Name | Module | What it's for |
| --- | --- | --- |
| `TOLERANCE` † | math | std's tolerances: `zeroLength` (1e-8, in meters), `zeroAngle` (1e-11, radians), `booleanDefaultTolerance` (1e-5 m: booleans treat faces this close as touching), `computational` (1e-13). Unitless: multiply by `meter` |
| `tolerantEquals` | math, vector, coordSystem, curveGeometry, surfaceGeometry | Numbers, vectors, lengths, lines, planes, circles: equal within `zeroLength` (or `computational`, for plain numbers) |
| `roundWithinTolerance` † | math | Rounds a number to the nearest integer only if it's within roundoff of it |
| `qToleranceFilter` † | query | Edges and vertices whose tolerance exceeds a threshold (`zeroLength` by default): geometry that's only approximately where it says |
| `evMaxTolerance` †, `evTolerances` † | evaluate | Entities' tolerances (undocumented: read the source before relying on what they return) |

## Feature machinery

| Name | Module | What it's for |
| --- | --- | --- |
| `startFeature` †, `endFeature` †, `abortFeature` † | feature | A feature of its own within a feature: `endFeature` keeps what's built since `startFeature`, `abortFeature` rolls it back. For trying something and undoing it, or retrying a sequence (see the style guide's "What rolls back when something throws") |
| `processSubfeatureStatus` | error | Copies an operation's or subfeature's status onto the feature; with `propagateErrorDisplay`, its error display too (what Onshape highlights) |
| `callSubfeatureAndProcessStatus` | feature | Calls a subfeature and processes its status, rethrowing its error |
| `setHighlightedEntities` † | feature | Highlights entities (undocumented) |
| `verifyNonemptyQuery` † | feature | Throws std's error for an empty selection parameter |
| `getUnstableIncrementingId` † | feature | A function making ids under a prefix, numbered as it's called, each disambiguated by the query it's for |
| `getFeatureName` †, `getLastActiveId` † | feature | The feature's name and the last active feature's id (undocumented) |
| `arrayParameterId` † | error | An array item's parameter id, for errors (as `faultyArrayParameterId`) |
| `processError` †, `reportFeatureError` † | error | What `defineFeature` does with a thrown error, and reporting a std error enum |
| `getAllVariables` † | context | Every variable (configuration variables too) as a map |
| `mapLookup` † | containers | `try (a.b.c)` without logging a warning |

## Queries

| Name | Module | What it's for |
| --- | --- | --- |
| `qClosestTo` | query | The entity nearest a point (ties within `zeroLength` give all of them) |
| `qCoincidesWithPlane` | query | Faces and edges lying in a plane |
| `qContainsPoint` | query | Entities containing a point |
| `qParallelPlanes` | query | Planar faces parallel to a plane (or a normal), optionally only those facing the same way |
| `qCapEntity` | query | An extrude's (or other operation's) start and end caps |
| `qCoincidentFilter` † | query | Entities coincident with a target (undocumented) |
| `qParallelEdges` † | query | Linear edges parallel to reference edges, on the references' bodies ("unconventional semantics") |
| `qTransient` † | query | A query of one transient id: what `evTolerances` and other raw results give |

## Operations

| Name | Module | What it's for |
| --- | --- | --- |
| `opExtractSurface` † | geomOperations | Surfaces copied from faces, optionally offset; the faces are unchanged. For showing faces, or working on copies |
| `opExtractWires` | geomOperations | Wire bodies copied from edges |
| `opOffsetWire` † | geomOperations | A wire offset from planar edges, in their plane |
| `opExtendSheetBody` † | geomOperations | Extends or trims a sheet body's edges by a distance, or up to another sheet |
| `opCreateOutline` † | geomOperations | Surfaces of parts' outlines projected onto a surface |
| `opPlane` † | geomOperations | A construction plane |
| `opEdgeChange` † | geomOperations | Changes edges' geometry (undocumented) |
| `opNameEntity` † | geomOperations | Names an entity, for `qNamed` ("under development") |
| `opFullRoundFillet` | geomOperations | A full round replacing center faces between two side faces |
| `opModifyFillet` | geomOperations | Changes an existing fillet's radius, or removes it (Vent sets its hair fillets to the wall thickness this way). Its `ModifyFilletType` is in `modifyFillet.fs`, which `common.fs` doesn't export: import it (`import(path : "onshape/std/modifyFillet.fs", version : ...)`) |
| `opDeleteFace` | geomOperations | Deletes faces and heals the gap by extending their neighbors (a rounded corner made sharp) |
| `opOffsetFace`, `opShell`, `opEnclose` | geomOperations | Moving faces; hollowing a solid; a solid from the space surfaces enclose (how Lighten and Vent make walls) |
| `opOffsetWire` † | geomOperations | Offsets a chain of planar edges in its plane (`normal`), by `offset1` and `offset2`, as wires, or with `makeRegions`, the flat sheet between them (a band): what thin extrudes use, since about 2023. For a closed chain, `offset1` is outward of the region it encloses, and `offset2` inward (so a hole's loop offset `offset1` grows into the face around it); an open chain's sides follow its first edge. A band offset both ways around a hole fails where it's wider than the hole's radius. Its input edges mustn't cross each other. Robot lighten's walls and ribs are its bands |
| `fSphere` † | primitives | A sphere, as `fCylinder` makes a cylinder |
| `cylinderCast` † | cylinderCast | Casts a cylinder (undocumented) |

## Evaluation and geometry

| Name | Module | What it's for |
| --- | --- | --- |
| `evBox3d` | evaluate | Bounding box in a coordinate system; `"tight" : true` for the exact extent rather than a loose one |
| `evFaceNormalAtEdge` | evaluate | A face's normal at a point along one of its edges: the angle between faces at an edge |
| `evEdgeConvexity` | evaluate | Whether an edge is convex, concave, or smooth |
| `evCollision` | evaluate | Which bodies touch or overlap |
| `evOffsetDetection` † | evaluate | Pairs of faces offset from each other in bodies (a part's thickness) |
| `evFaults` † | evaluate | Entities' faults (undocumented) |
| `evMateConnectorCoordSystem` † | evaluate | The coordinate system a mate connector's picks and inferences give |
| `versionedCoplanarPlanes` † | surfaceGeometry | `coplanarPlanes`, as the context's version computes it |
| `getEdgesCommmonPlane` † | topologyUtils | The plane edges share, or undefined (std spells it with three m's) |
| `getThinWallRegions` † | topologyUtils | Flat regions from offsetting edges, as extrude's Thin option makes them |
| `sweptAlong` | topologyUtils | Whether a face is swept along a direction |
| `getRemainderPatternTransform` | feature | The transform of the feature pattern instance being built: Vent copies its faces by it |

## Bounds and sketches

| Name | Module | What it's for |
| --- | --- | --- |
| `EDGE_PARAMETER_BOUNDS` † | valueBounds | 0 to 1 along an edge, 0.5 by default. Std's fillet bounds its points on edges to 0.001 to 0.999 instead (`EDGE_INTERIOR_PARAMETER_BOUNDS`, private to `fillet.fs`) |
| `skSpline` †, `skInterpolatedSpline` † | sketch | Closed splines; std suggests `skFitSpline` for custom features |
