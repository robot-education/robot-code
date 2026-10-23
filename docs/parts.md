# Part sets

A part set is a family of FeatureScript-backed parts, like FRCDesign's fasteners: Part Studios whose only feature is
one of the set's, each configured by a definition so that every configuration is one part. They're released into
FRCDesignLib's own documents, where people insert them from FRCDesign, so they're kept apart from the user-facing
FeatureScript `fs release` publishes, with their own commands and versions.

Their FeatureScript lives in the backend document like any other, so `fs push`, `fs check`, the language server, the
evaluator, and imports of `core/` all work as usual.

## Declaring one

```toml
[tool.fs.parts.fasteners]
title = "Fasteners"        # its versions' names: "Fasteners - v1.2.0"
folder = "fasteners"       # in featurescripts/: its FeatureScript and definition
main = "https://cad.onshape.com/documents/77461d97ef7c9d816f0c7351"   # the FRCDesignLib document it's released into

[tool.fs.parts.fasteners.targets]   # other documents to release it to, to try it out (optional)
local = "https://cad.onshape.com/documents/..."
```

```
uv run fs parts fasteners --test                  # build its Part Studios in your branch's workspace
uv run fs parts fasteners --diagnose              # find what Onshape refuses in a configuration
uv run fs parts fasteners --release --minor       # release it to its main document (on main)
uv run fs parts fasteners --release --target local   # release it to a target, to try it out (any branch)
uv run fs configuration <url>                     # print a Part Studio's configuration as Onshape stores it
uv run fs features <url>                          # print its features as Onshape stores them, namespaces and all
```

The folder holds:

- One Feature Studio defining every feature the set's Part Studios use (`fasteners/fasteners.fs`), and whatever it
  imports.
- A definition (`fasteners/fastenerTables.py`, see [fs-cli.md](fs-cli.md#generating-lookup-tables)) whose `STUDIOS`
  holds a `ConfiguredStudio` per Part Studio (see [Configured Part Studios](fs-cli.md#configured-part-studios)): its
  tab's name, its feature, and its configuration's inputs. Its `EXPORTS` write what reads the set outside Onshape
  (FRCDesignApp's manifest, `fasteners.gen.json`), including where each Part Studio was last released.

## Trying a change

Push it, then build the set's test Part Studios in the backend document:

```
uv run fs push featurescripts/fasteners
uv run fs parts fasteners --test
```

They're built in the branch's own workspace (see [Branches](fs-cli.md#branches)), so branches don't build over each
other's. Each Part Studio is built as `<tab> (TEST)`, e.g. `Screw (TEST)`, from the Feature Studio as pushed: its tab found by
name or created, its configuration written, and its feature inserted from the Feature Studio as it is now (replacing
the one a build inserted before) and configured. It prints each one's url, to check in Onshape. A feature that fails to regenerate in its default
configuration stops the build. Nothing is versioned or recorded, so try as often as needed.

When Onshape refuses a configuration, `uv run fs parts fasteners --diagnose` finds which part. It writes each studio's
configuration in stages to a scratch `<tab> (DIAGNOSE)` Part Studio: none, the inputs with no conditions, then each
input's conditions in turn, all its option rules together, since one alone could hide its default, which Onshape
refuses. It prints each stage Onshape accepted,
the first it refused with Onshape's error, and the features in the studio's `(TEST)` tab, stopping at the first refusal:
a call per stage, about ten for the screw. Delete the scratch tabs after.

## Releasing

```
uv run fs parts fasteners --release --minor --dry-run    # the plan; a few read calls
uv run fs parts fasteners --release --minor -d "Added zinc screws"
```

A release:

1. Creates a version named e.g. `Fasteners - v1.2.0` in the backend document.
2. Points the main document's Feature Studio of the same name (`fasteners.fs`) at that version, creating it if needed:
   a small generated studio re-exporting the backend's, as the frontend's are.
3. Builds each Part Studio in the main document, by its tab's name (`Screw`), from that studio as just pushed, and
   writes the set's manifest beside them as a file tab, `fasteners.parts.json`: the definition's manifest export with
   each studio's tab there (`elementId`) in place of where it was last released. FRCDesignApp reads it when it loads
   the document's version, so the records it indexes are always those of the version it loads.
4. Creates the same version in the main document.
5. Records where the Part Studios are in `<definition>.studios.json` beside the definition (the main document, that
   version, and each tab), writes the definition's `EXPORTS` again, and tags the release in git (`fasteners/v1.2.0`).

Every studio in the set's folder must be pushed and in sync first. Versions can't be deleted, so it asks for
confirmation (`-y` skips it). A feature that fails to regenerate stops the release before the main document is
versioned; the backend version stays, so the next try takes the next version number.

A release runs on the main branch only. Ask for one in a release file (`part_set = "fasteners"`; see
[Release files](fs-cli.md#release-files)), which the Onshape workflow makes once its branch is merged, committing the
recorded locations and exports; run by hand, commit them yourself. FRCDesignApp's library reads the released manifest
itself; Quick fastener still reads the synced one (`npm run sync:generated-parts -- <commit>`).

### Releasing to a target

`--target` releases to another document, one named in `[tool.fs.parts.<set>.targets]` or any document's url, from any
branch: to load a set into a local FRCDesignApp, say, before releasing it. It does steps 1 to 4 to that document, but
the versions are named for the branch and commit (`Fasteners test - my-branch 1a2b3c4`), not numbered, and nothing
is recorded, marked released, or tagged: the main document and the next release's number are untouched. Its backend
version is made from the branch's workspace. Versions can't be deleted, so each try leaves one in the backend
document and one in the target.

`fs release` refuses a part set's FeatureScript, and `fs sync-versions` leaves its backend versions out: they're never
the frontend's.
