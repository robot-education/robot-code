from library.api import api, client, constant
import os
import json
import re
from typing import Optional, Callable

OUTDATED_VERSION_MATCH: re.Pattern[str] = re.compile(
    'version : "(\\d{4,6})\\.0"|FeatureScript (\\d{4,6});'
)


class CodeManager:
    def __init__(
        self, document_path: api.Path, client: client.FeatureStudioClient
    ) -> None:
        self._document_path = document_path
        self._client = client

    def pull(self) -> None:
        feature_studios = self._client.get_feature_studios(self._document_path)
        print("Found {} feature studios to pull.".format(len(feature_studios)))

        id_to_name = {}
        for feature_studio in feature_studios:
            name = feature_studio["name"]
            id = feature_studio["id"]
            id_to_name[id] = name

            path = self._document_path.copy()
            path.eid = id
            code = self._client.get_code(path)
            self._write_to_file(name, code, _FOLDER_PATH)

        self._write_to_file(_ID_FILE, json.dumps(id_to_name))

        num_studios = len(feature_studios)
        print("Pulled {} feature studios.".format(num_studios))

    def push(self) -> None:
        id_to_name = json.loads(self._read_from_file(_ID_FILE))
        items = id_to_name.items()
        for id, name in items:
            code = self._read_from_file(name, _FOLDER_PATH)
            path = api.Path(self._document_path.did, self._document_path.wid, id)
            self._client.update_code(path, code)

        print("Pushed {} files to Onshape.".format(len(items)))

    def update_std(self) -> None:
        files = os.listdir(path=_FOLDER_PATH)
        std_version = self._client.std_version()
        updated = 0
        for file in files:
            contents = self._read_from_file(file, _FOLDER_PATH)
            new_contents = self._update_version(contents, std_version)
            if new_contents != contents:
                updated += 1
                self._write_to_file(file, new_contents, _FOLDER_PATH)
        print("Modified {} files.".format(updated))

        self.push()

    def _update_version(self, contents: str, std_version: str) -> str:
        return re.sub(
            pattern=OUTDATED_VERSION_MATCH,
            repl=self._generate_update_replace(std_version),
            string=contents,
        )

    def _generate_update_replace(self, std_version: str) -> Callable[[re.Match], str]:
        def replace_number(match: re.Match) -> str:
            return re.sub(
                pattern="\\d{4,6}", repl=str(std_version), string=match.group(0)
            )

        return replace_number

    def clean(self) -> None:
        files = os.listdir(_FOLDER_PATH)
        for file in files:
            os.remove(os.path.join(_FOLDER_PATH, file))

        if os.path.isfile(_ID_FILE):
            os.remove(_ID_FILE)

    def _write_to_file(
        self, file_name: str, content: str, file_path: Optional[str] = None
    ) -> None:
        path = (
            os.path.join(file_path, file_name) if file_path is not None else file_name
        )
        file = open(path, "w", encoding="utf-8")
        file.write(content)
        file.close()

    def _read_from_file(self, file_name: str, file_path: Optional[str] = None) -> str:
        path = (
            os.path.join(file_path, file_name) if file_path is not None else file_name
        )
        file = open(path, "r", encoding="utf-8")
        value = file.read()
        file.close()
        return value


def make_manager(logging: bool = False) -> CodeManager:
    feature_client = client.FeatureStudioClient(api.Onshape(logging=logging))
    return CodeManager(constant.BACKEND_PATH, feature_client)
