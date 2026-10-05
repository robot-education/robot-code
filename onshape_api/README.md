# Onshape API

A small client for the Onshape REST API, used by the `fs` CLI and the std updater.

- `onshape_api.api`: `make_key_api()` builds an `Api` authenticated with the API keys in `.env` or the environment.
- `onshape_api.paths`: `DocumentPath`, `InstancePath`, and `ElementPath`, plus conversions to and from Onshape urls.
- `onshape_api.endpoints`: one function per REST endpoint (documents, feature studios, versions, std versions).
- `onshape_api.types`: hand-written types for the parts of each response we read.
- `onshape_api.model`: constants such as the path to the Onshape std library.

## The API reference

`reference/openapi.json` is Onshape's OpenAPI definition (the one behind the
[Glassworks API explorer](https://cad.onshape.com/glassworks/explorer)), trimmed to the operations we call and the
schemas they use. Nothing imports it; it's there to read, and to diff against when Onshape changes the API.
Regenerate it with:

```
uv run python -m onshape_api.reference.generate
```

To call a new endpoint or read a new field:

1. Add the operation to `OPERATIONS` in `reference/generate.py` and regenerate.
2. Read the operation's response schema in `reference/openapi.json` for the real shape.
3. Hand-write the subset you need in `types.py`, and add a function to `endpoints/`.

Onshape limits API calls per year (see [API limits](https://onshape-public.github.io/docs/auth/limits/)), so prefer
endpoints that answer several questions at once (e.g. document contents lists every tab with its microversion and
folder).
