from library.api import api, api_path, constant, storage
import re


class StudioClient:
    def __init__(self, api: api.Api) -> None:
        self._api = api

    # def get_microversion(self, studio_path: api_path.StudioPath) -> None:
    #     result = self._api.request(
    #         api_path.ApiRequest("get", "documents", studio_path, "currentmicroversion")
    #     ).json()
    #     return result["microversion"]

    def get_studios(
        self, document_path: api_path.DocumentPath
    ) -> list[storage.FeatureStudio]:
        """Returns an array of feature studios in a document."""
        query = {"elementType": "FEATURESTUDIO"}
        elements = self._api.request(
            api_path.ApiRequest("get", "documents", document_path, "elements"),
            query=query,
        ).json()
        return self.extract_studios(elements, document_path)

    # Extracts paths from elements
    def extract_studios(
        self, elements: list[dict], document_path: api_path.DocumentPath
    ) -> list[storage.FeatureStudio]:
        return [
            storage.FeatureStudio(
                element["name"],
                api_path.StudioPath(document_path.copy(), element["id"]),
                element["microversionId"],
            )
            for element in elements
        ]

    # Fetches code from the feature studio specified by path
    def get_code(self, path: api_path.StudioPath) -> str:
        result = self._api.request(
            api_path.ApiRequest("get", "featurestudios", path)
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
