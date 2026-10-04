# Onshape API

A small client for the Onshape REST API, used by the `fs` CLI and the stdlib index generator.

- `onshape_api.api`: `make_key_api()` builds an `Api` authenticated with the API keys in `.env` or the environment.
- `onshape_api.paths`: `DocumentPath`, `InstancePath`, and `ElementPath`, plus conversions to and from Onshape urls.
- `onshape_api.endpoints`: one function per REST endpoint (documents, feature studios, versions, std versions).
- `onshape_api.model`: constants such as the path to the Onshape std library.
