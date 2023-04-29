from library.api import api, api_path, constant
import re


class FeatureStudioClient:
    def __init__(self, api: api.Api) -> None:
        self._api = api

    # Returns an array of paths to feature studios in a document
    def get_studio_paths(
        self, document_path: api_path.DocumentPath
    ) -> list[api_path.StudioPath]:
        feature_studios = self._get_studios(document_path)
        return self.extract_paths(feature_studios, document_path)

    def _get_studios(self, document_path: api_path.DocumentPath) -> list[dict]:
        queries = {"elementType": "FEATURESTUDIO"}
        return self._api.request(
            api_path.ApiRequest("get", "documents", document_path, "elements"),
            query=queries,
        ).json()

    # Extracts paths from elements
    def extract_paths(
        self, elements: list[dict], document_path: api_path.DocumentPath
    ) -> list[api_path.StudioPath]:
        return [
            api_path.StudioPath(document_path.copy(), element["id"])
            for element in elements
        ]

    # Fetches code from the feature studio specified by path
    def get_code(self, path: api_path.StudioPath) -> str:
        result = self._api.request(
            api_path.ApiRequest("get", "feature_studios", path)
        ).json()
        return result["contents"]

    # Sends code to the given feature studio specified by path
    def update_code(self, path: api_path.StudioPath, code: str):
        payload = {"contents": code}
        return self._api.request(
            api_path.ApiRequest("post", "featurestudios", path), body=payload
        )

    # Returns the latest version of the std.
    def std_version(self) -> str:
        code = self.get_code(constant.STD_PATH)
        parsed = re.search("\\d{4,6}", code)
        if parsed is None:
            raise RuntimeError("Failed to find latest version of onshape std.")
        return parsed.group(0)
