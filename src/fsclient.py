from apikey.onshape import Onshape, Path, ApiPath

import mimetypes
import random
import string
import os

DOCUMENT_PATHS = [
    # backend
    Path('00dd11dabe44da2db458f898', '6c20cd994b174cc99668701f'),
    # frontend
    Path('9cffa92db8b62219498f89af', '06b332ccabc9d2e0aa0abf88')
]

class Client():
    def __init__(self, logging=True):
        self._api = Onshape(logging=logging)

    def get_feature_studio_paths(self):
        feature_studio_paths = []
        for path in DOCUMENT_PATHS:
            feature_studio_ids = self._get_feature_studio_ids(path)
            for id in feature_studio_ids:
                path.set_eid(id)
                feature_studio_paths.append(path)
        return feature_studio_paths

    def _get_feature_studio_ids(self, path):
        queries = {'elementType': 'FEATURESTUDIO'}
        feature_studios = self._api.request('get', ApiPath('documents', path, 'elements'), query=queries).json()

        result = []
        for feature_studio in feature_studios:
            result.append(feature_studio['id'])
        return result

    def get_code(self, path):
        return self._api.request('get', ApiPath('featurestudios', path)).json()['contents']

    def update_code(self, path, code):
        payload = {'contents': code}
        return self._api.request('post', ApiPath('featurestudios', path), body=payload)
