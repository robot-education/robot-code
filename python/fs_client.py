from apikey.onshape import Onshape, Path, ApiPath
from fs_constants import STD_PATH

import mimetypes
import random
import string
import os
import re


class FeatureStudioClient():
    def __init__(self, api: Onshape) -> None:
        self._api = api

    # Returns an array of paths to feature studios in a document
    def get_feature_studio_paths(self, document_path: Path) -> [Path]:
        feature_studios = self.get_feature_studios()
        return self.extract_paths(feature_studios, document_path)

    # Returns all feature studios in a document
    def get_feature_studios(self, document_path: Path) -> [object]:
        queries = {'elementType': 'FEATURESTUDIO'}
        return self._api.request('get', ApiPath(
            'documents', document_path, 'elements'), query=queries).json()

    # Extracts paths from elements
    def extract_paths(self, elements: [object], document_path: Path) -> [Path]:
        result = []
        for element in elements:
            # create a new path to avoid mutation
            path = Path(document_path.did, document_path.wid, element['id'])
            result.append(path)
        return result

    # Fetches code from the feature studio specified by path
    def get_code(self, path: Path) -> str:
        return self._api.request('get', ApiPath('featurestudios', path)).json()['contents']

    # Sends code to the given feature studio specified by path
    def update_code(self, path: Path, code: str):
        payload = {'contents': code}
        return self._api.request('post', ApiPath('featurestudios', path), body=payload)

    # Returns the latest version of the std.
    def std_version(self) -> str:
        code = self.get_code(STD_PATH)
        return re.search('\d{4,6}', code).group(0)
