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

## Anonymous requests

Onshape answers some requests about public documents without credentials, and those don't count against the API
limits of the keys' owner. Endpoints which can pass `anonymous=True` to `api.get`: `KeyApi` sends the request
unsigned, and if the document isn't public (401, 403, or 404), sends it again signed, and signs every later request
about that document from the start.

Tested in October 2026 (API v16), on public documents:

| Endpoint | Anonymous | Used by |
| --- | --- | --- |
| `GET /documents/d/{did}/{wvm}/{wvmid}/elements` | Yes | `documents.get_document_elements` |
| `GET /metadata/d/{did}/{wvm}/{wvmid}/e/{eid}` | Yes | `metadata.get_element_metadata` |
| `GET /blobelements/d/{did}/{wvm}/{wvmid}/e/{eid}` | Yes | `blob_elements.download_file` |
| `GET /documents/d/{did}/{wvm}/{wvmid}/contents` | No (401) | `documents.get_document_contents` |
| `GET /documents/d/{did}/versions` | No (401) | `versions.get_versions` (and so the std's versions) |
| `GET /featurestudios/d/{did}/{wvm}/{wvmid}/e/{eid}` | No (401) | `feature_studios.get_contents` |
| `GET /featurestudios/d/{did}/{wvm}/{wvmid}/e/{eid}/featurespecs` | No (401) | `feature_studios.get_feature_specs` |

Anything that changes a document always needs credentials. To check an endpoint, request it with `curl` and no
credentials (e.g. `curl -sS "https://cad.onshape.com/api/v16/documents/d/{did}/w/{wid}/elements"`); when one
works, pass `anonymous=True` in its function, and add it to this table.
