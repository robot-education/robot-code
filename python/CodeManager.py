from apikey.onshape import Onshape, Path, ApiPath
from fs_client import FeatureStudioClient
import os
import json
import sys
from typing import Optional


class CodeManager():
    def __init__(self, document_path: str) -> None:
        self._document_path = document_path

        self._FOLDER_PATH = './backend'
        self._ID_FILE = 'feature_studio_ids.json'

        self._client = FeatureStudioClient(Onshape(logging=False))

    def pull(self) -> None:
        self.clean()
        feature_studios = self._client.get_feature_studios(self._document_path)
        paths = self._client.extract_paths(feature_studios, self._document_path)

        id_to_name = {}
        for feature_studio in feature_studios:
            name = feature_studio['name']
            id = feature_studio['id']
            id_to_name[id] = name

            path = self._document_path.clone()
            path.eid = id
            code = self._client.get_code(path)
            self._write_to_file(name, code, self._FOLDER_PATH)

        self._write_to_file(self._ID_FILE, json.dumps(id_to_name))

    def push(self) -> None:
        id_to_name = json.loads(self._read_from_file(self._ID_FILE))
        for id, name in id_to_name.items():
            code = self._read_from_file(name, self._FOLDER_PATH)
            path = Path(self._document_path.did, self._document_path.wid, id)
            self._client.update_code(path, code)

    def clean(self) -> None:
        files = os.listdir(self._FOLDER_PATH)
        for file in files:
            os.remove(os.path.join(self._FOLDER_PATH, file))

        if os.path.isfile(self._ID_FILE):
            os.remove(self._ID_FILE)

    def _write_to_file(self, file_name: str, content: str, file_path: Optional[str] = None) -> None:
        path = os.path.join(file_path, file_name) if file_path is not None else file_name
        file = open(path, 'w', encoding='utf-8')
        file.write(content)
        file.close()

    def _read_from_file(self, file_name: str, file_path: Optional[str] = None) -> str:
        path = os.path.join(file_path, file_name) if file_path is not None else file_name
        file = open(path, 'r', encoding='utf-8')
        value = file.read()
        file.close()
        return value
