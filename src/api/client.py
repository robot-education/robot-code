from src.api.apikey import onshape
from src.api import constants
import re


class FeatureStudioClient:
    def __init__(self, api: onshape.Onshape) -> None:
        self._api = api

    # Returns an array of paths to feature studios in a document
    def get_feature_studio_paths(
        self, document_path: onshape.Path
    ) -> list[onshape.Path]:
        feature_studios = self.get_feature_studios(document_path)
        return self.extract_paths(feature_studios, document_path)

    # Returns all feature studios in a document
    def get_feature_studios(self, document_path: onshape.Path) -> list[dict]:
        queries = {"elementType": "FEATURESTUDIO"}
        return self._api.request(
            "get",
            onshape.ApiPath("documents", document_path, "elements"),
            query=queries,
        ).json()

    # Extracts paths from elements
    def extract_paths(
        self, elements: list[dict], document_path: onshape.Path
    ) -> list[onshape.Path]:
        result = []
        for element in elements:
            # create a new path to avoid mutation
            path = onshape.Path(document_path.did, document_path.wid, element["id"])
            result.append(path)
        return result

    # Fetches code from the feature studio specified by path
    def get_code(self, path: onshape.Path) -> str:
        result =  self._api.request("get", onshape.ApiPath("featurestudios", path)).json()
        return result [ "contents"
        ]

    # Sends code to the given feature studio specified by path
    def update_code(self, path: onshape.Path, code: str):
        payload = {"contents": code}
        return self._api.request(
            "post", onshape.ApiPath("featurestudios", path), body=payload
        )

    # Returns the latest version of the std.
    def std_version(self) -> str:
        code = self.get_code(constants.STD_PATH)
        parsed = re.search("\\d{4,6}", code)
        if parsed is None:
            raise RuntimeError("Failed to find latest version of onshape std.")
        return parsed.group(0)
