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
uv run fs parts test fasteners
```

Each Part Studio is built as `<tab> (TEST)`, e.g. `Screw (TEST)`, from the Feature Studio as pushed: its tab found by
name or created, its configuration written, and its feature inserted (or pointed at the Feature Studio as it is now)
and configured. It prints each one's url, to check in Onshape. A feature that fails to regenerate in its default
configuration stops the build. Nothing is versioned or recorded, so try as often as needed.

## Releasing

```
uv run fs parts release fasteners --minor --dry-run    # the plan; a few read calls
uv run fs parts release fasteners --minor -d "Added zinc screws"
```

A release:

1. Creates a version named e.g. `Fasteners - v1.2.0` in the backend document.
2. Points the main document's Feature Studio of the same name (`fasteners.fs`) at that version, creating it if needed:
   a small generated studio re-exporting the backend's, as the frontend's are.
3. Builds each Part Studio in the main document, by its tab's name (`Screw`), from that studio as just pushed.
4. Creates the same version in the main document.
5. Records where the Part Studios are in `<definition>.studios.json` beside the definition (the main document, that
   version, and each tab), writes the definition's `EXPORTS` again, and tags the release in git (`fasteners/v1.2.0`).

Every studio in the set's folder must be pushed and in sync first. Versions can't be deleted, so it asks for
confirmation (`-y` skips it). A feature that fails to regenerate stops the release before the main document is
versioned; the backend version stays, so the next try takes the next version number.

Commit the recorded locations and exports, then sync them where they're read (FRCDesignApp:
`npm run sync:generated-parts -- <commit>`).

`fs release` refuses a part set's FeatureScript, and `fs sync-versions` leaves its backend versions out: they're never
the frontend's.
