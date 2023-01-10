from apikey.onshape import Onshape, Path, ApiPath
from fsclient import FeatureStudioClient
from fsconstants import BACKEND_PATH
import os
import json
import sys


class CodeManager():
    def __init__(self, document_path):
        self._document_path = document_path

        self._FOLDER_PATH = './backend'
        self._ID_FILE = 'featurestudioids'

        self._client = FeatureStudioClient(Onshape(logging=False))

    def pull(self):
        self.clean()
        feature_studios = self._client.get_feature_studios(self._document_path)
        paths = self._client.extract_paths(feature_studios, self._document_path)

        id_to_name = {}
        for i, feature_studio_path in enumerate(paths):
            name = str(feature_studios[i]['name'])
            id_to_name[feature_studio_path.eid] = name
            code = self._client.get_code(feature_studio_path)
            self._write_to_file(name, code, self._FOLDER_PATH)

        self._write_to_file(self._ID_FILE, json.dumps(id_to_name))

    def push(self):
        id_to_name = json.loads(self._read_from_file(self._ID_FILE))
        for id, name in id_to_name.items():
            code = self._read_from_file(name, self._FOLDER_PATH)
            path = Path(self._document_path.did, self._document_path.wid, id)
            self._client.update_code(path, code)

    def clean(self):
        files = os.listdir(self._FOLDER_PATH)
        for file in files:
            os.remove(os.path.join(self._FOLDER_PATH, file))

        if os.path.isfile(self._ID_FILE):
            os.remove(self._ID_FILE)

    def _write_to_file(self, file_name, content, file_path=""):
        file = open(os.path.join(file_path, file_name), 'w', encoding='utf-8')
        file.write(content)
        file.close()

    def _read_from_file(self, file_name, file_path=""):
        file = open(os.path.join(file_path, file_name), 'r', encoding='utf-8')
        value = file.read()
        file.close()
        return value


manager = CodeManager(BACKEND_PATH)
if sys.argv[1] == 'pull':
    manager.pull()
elif sys.argv[1] == 'push':
    manager.push()
elif sys.argv[1] == 'clean':
    manager.clean()
else:
    print("Usage: python3 {} <pull|push>".format(sys.argv[0]))
