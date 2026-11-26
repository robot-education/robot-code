# Part sets

A part set is a family of FeatureScript-backed parts, like FRCDesign's fasteners: Part Studios whose only feature is
one of the set's, each configured by a definition so that every configuration is one part. Each is released, with
`fs release SET`, into a frontend document of its own (FRCDesignLib's fasteners and bearings documents, ConfigLib's
for FTC's), where people insert them from FRCDesign, so they're kept apart from the user-facing FeatureScripts in the
frontend document, with their own versions.

Their FeatureScript lives in the backend document like any other, so `fs push`, `fs check`, the language server, the
evaluator, and imports of `core/` all work as usual.

## Declaring one

```toml
[tool.fs.documents.frcdesignlib-fasteners]   # the frontend document it's released into (see docs/fs-cli.md#documents)
role = "frontend"
url = "https://cad.onshape.com/documents/77461d97ef7c9d816f0c7351"

[tool.fs.parts.fasteners]
title = "Fasteners"        # its versions' names: "Fasteners - v1.2.0"
folder = "fasteners"       # in featurescripts/: its FeatureScript and definition
frontend = "frcdesignlib-fasteners"
```

Each set names its own frontend document, as each of FRCDesignLib's and ConfigLib's part families is a document of its
own; a set without one yet (bearings, so far) can be built and released to a library, but not released. Two sets can
share a folder, its Feature Studio and its tables, each configuring its own Part Studios for its own frontend: FRC's
fasteners and FTC's (`ftcFasteners`, released into ConfigLib's fasteners document) build the same features, each
offering its own sizes. Each then names its definition, since a set otherwise takes every definition in its folder:

```toml
[tool.fs.parts.ftcFasteners]
title = "FTC Fasteners"
folder = "fasteners"
definition = "ftcFastenerTables.py"   # the file in the folder configuring its Part Studios
frontend = "configlib-fasteners"
```

```
uv run fs build fasteners                         # build its Part Studios in the backend workspace
uv run fs build fasteners --to enterprise         # ...or in another backend, pushing its FeatureScript there first
uv run fs build fasteners --diagnose              # find what Onshape refuses in a configuration
uv run fs release fasteners --minor               # a soft release to its frontend document (on main)
uv run fs release fasteners --minor --hard        # a hard release: also version the frontend
uv run fs publish fasteners                       # version the frontend for its soft release, once checked
uv run fs release fasteners --to library          # a test release, to a library (any branch)
uv run fs configuration <url>                     # print a Part Studio's configuration as Onshape stores it
uv run fs features <url>                          # print its features as Onshape stores them, namespaces and all
```

The folder holds:

- One Feature Studio defining every feature the set's Part Studios use (`fasteners/fasteners.fs`), and whatever it
  imports.
- A definition (`fasteners/fastenerTables.py`, see [fs-cli.md](fs-cli.md#generated-files)), or one per set
  sharing the folder (`fasteners/ftcFastenerTables.py`, which configures FTC's studios from `fastenerTables.py`'s
  tables and writes no Feature Studio of its own), whose `STUDIOS`
  holds a `ConfiguredStudio` per Part Studio (see [Configured Part Studios](fs-cli.md#configured-part-studios)): its
  tab's name, its feature, and its configuration's inputs. Its `EXPORTS` write what reads the set outside Onshape
  (FRCDesignApp's manifest, `fasteners.gen.json`), including where each Part Studio was last released.

`fs audit` on the set's Feature Studio, and VS Code's feature preview of it, show its Part Studios' configurations as
Onshape will, choices and all, rather than the feature's own dialog (see [fs-cli.md](fs-cli.md)'s `fs audit`): no API
calls, so it's the first place to look at a change.

## Trying a change

Push it, then build the set's test Part Studios in the backend document:

```
uv run fs push featurescripts/fasteners
uv run fs build fasteners
```

They're built in the backend's main workspace, which every branch pushes to (see [Branches](fs-cli.md#branches)), so
the last build, from whichever branch, is what's there. Each Part Studio is built as `<tab> (TEST)`, e.g.
`Screw (TEST)`, from the Feature Studio as pushed: its tab found by
name or created, the feature a build inserted before deleted, its configuration written, and its feature inserted from
the Feature Studio as it is now and configured. Before any of it, the set's configurations are checked offline against
their rules (see [Configured Part Studios](fs-cli.md#configured-part-studios)). It refuses while the set's FeatureScripts
aren't in sync with the backend document, and first writes any import versions Onshape changed since into the local
files, as `fs pull` does: Onshape has refused to configure a feature from a Feature Studio whose local file lagged. It prints each one's url, to check in Onshape. A feature that fails to regenerate in its default
configuration stops the build. Nothing is versioned or recorded, so try as often as needed.

`fs build fasteners --to enterprise` builds them in another backend instead: it pushes the set's FeatureScript, with
what it imports, there first, as `fs push --to` does (see [Pushing to another backend](fs-cli.md#pushing-to-another-backend)),
and builds from that copy. The backend's copy needn't be in sync.

When Onshape refuses a configuration, `uv run fs build fasteners --diagnose` finds which part (`--to DOC` there too). It writes each studio's
configuration in stages to a scratch `<tab> (DIAGNOSE)` Part Studio: none, the inputs with no conditions, then each
input's conditions in turn, all its option rules together, since one alone could hide its default, which Onshape
refuses. It prints each stage Onshape accepted,
the first it refused with Onshape's error, and the features in the studio's `(TEST)` tab, stopping at the first refusal:
a call per stage, about ten for the screw. Delete the scratch tabs after.

## Releasing

```
uv run fs release fasteners --minor --dry-run    # the plan; a few read calls
uv run fs release fasteners --minor -d "Added zinc screws"
```

A release, as `fs release` makes a FeatureScript's (see [Releasing](fs-cli.md#releasing)):

1. Pushes the set's Feature Studio, and what it imports, as it is in the repo over Onshape's, deleting nothing.
2. Creates a version named e.g. `Fasteners - v1.2.0` in the backend document.
3. Points its frontend document's Feature Studio of the same name (`fasteners.fs`) at that version, creating it if
   needed: a small generated studio re-exporting the backend's, as the FeatureScripts' frontend's are.
4. Builds each Part Studio in the frontend document, by its tab's name (`Screw`), from that studio as just pushed, and
   writes the set's manifest beside them as a file tab, `fasteners.parts.json`: the definition's manifest export with
   each studio's tab there (`elementId`) in place of where it was last released. A studio whose feature makes a mate
   connector names the id it gives it (`mateConnector`, from `ConfiguredStudio.mate_connector`), and others on its
   top and bottom where it has them (`topMateConnector` and `bottomMateConnector`, from `top_mate_connector` and
   `bottom_mate_connector`). A record naming one of these stands for its own configuration where it differs (a radial
   bearing's `topMateConnector` is `mateConnector`, its top being where it seats). A fasten mate names a connector
   `<featureId>.<id>`, and Onshape's API shows no mate connector a feature makes, so this is how FRCDesignApp fastens
   to one, in that studio or any Part Studio using the feature. FRCDesignApp reads it when it loads
   the document's version, so the records it indexes are always those of the version it loads.
5. Records where the Part Studios are in `<definition>.studios.json` beside the definition (the frontend document and
   each tab), and writes the definition's `EXPORTS` again.
6. A hard release (`--hard`) also creates the version in the frontend document (`Fasteners - v1.2.0`), and tags the
   release in git, by the set's name (`fasteners/v1.2.0`, `ftcFasteners/v1.0.0`).

A soft release, the default, stops at 5 (see [Soft and hard releases](fs-cli.md#soft-and-hard-releases)): check the
Part Studios in the frontend, then `fs publish fasteners` versions it and tags the release, as a hard release would.
FRCDesignApp loads the frontend's version (or holds it for approval). Releases are numbered by the backend document's
versions.

Every other studio in the set's folder must be pushed and in sync first. Versions can't be deleted, so it asks for
confirmation (`-y` skips it). A feature that fails to regenerate stops the release before anything is recorded; the
backend version stays, so the next try takes the next version number.

A release runs on the main branch only. Ask for one in a release file (`name = "fasteners"`; see
[Release files](fs-cli.md#release-files)), which the Onshape workflow makes once its branch is merged, committing the
recorded locations and exports; run by hand, commit them yourself. FRCDesignApp's library reads the released manifest
itself, and Auto Assembly reads the catalog the library stored from it, so a release needs no change there.

### Test releases

`--to DOC` makes a test release, to a library document instead (see [Documents](fs-cli.md#documents)), from any
branch: to load a set into a local FRCDesignApp, say, before releasing it. It does steps 1 to 4 there, then always
versions it, so FRCDesignApp
can load it; the versions are named for the branch and commit (`Fasteners test - my-branch 1a2b3c4`), not numbered, and
nothing is recorded, marked released, or tagged: the frontend document and the next release's number are untouched.
Versions can't be deleted, so each try leaves one in the backend document and one in the library.

`fs release` refuses a part set's FeatureScript by name (`fs release fasteners.fs`): name the part set, which releases
it with its Part Studios.
