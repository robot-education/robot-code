import json
import pathlib
from typing import Any
from library.api import api_path

STORAGE_FILE = "studio_data.pickle"


class Config:
    def __init__(self):
        path = pathlib.Path("config.json")
        if not path.is_file:
            raise IOError("Failed to find conf.json in the root directory")
        config = json.load(path.open())
        self._parse_config(config)

    def _get_config_key(self, config: dict, key: str) -> Any:
        value = config.get(key, None)
        if value is None:
            raise KeyError("config.json must contain a {} field".format(key))
        return value

    def _get_document_key(self, config: dict, key: str) -> Any:
        value = config.get(key, None)
        if value is None:
            raise KeyError(
                "Each document in config.json must contain a {} field".format(key)
            )
        return value

    def _get_dir(self, dir_path: str) -> pathlib.Path:
        path = pathlib.Path(dir_path)
        path.mkdir(exist_ok=True)
        return path

    def _parse_config(self, config: dict):
        self.storage_path = self._get_dir(
            self._get_config_key(config, "storage_path")
        ) / pathlib.Path(STORAGE_FILE)
        self.code_path = self._get_dir(self._get_config_key(config, "code_path"))
        self.code_gen_path = self._get_dir(
            self._get_config_key(config, "code_gen_path")
        )

        self._parse_document_paths(config)

    def _parse_document_paths(self, config: dict):
        documents: list = self._get_config_key(config, "documents")
        self.documents: dict[str, api_path.DocumentPath] = dict(
            (
                self._get_document_key(document, "name"),
                api_path.make_document_path(
                    self._get_document_key(document, "url"),
                ),
            )
            for document in documents
        )
