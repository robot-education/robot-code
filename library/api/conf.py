import dataclasses
import json5
import pathlib
import pickle
from typing import Any
from library.api import api_path

STORAGE_FILE = "studio_data.pickle"


@dataclasses.dataclass
class FeatureStudio:
    name: str
    path: api_path.ElementPath
    microversion_id: str
    modified: bool = False
    generated: bool = False


FileData = dict[str, FeatureStudio]


class Config:
    """Represents the user-specified config information secified in `config.json`."""

    def __init__(self) -> None:
        path = pathlib.Path("config.json")
        if not path.is_file:
            raise IOError("Failed to find conf.json in the root directory")
        config = json5.load(path.open())
        self._parse_config(config)  # type: ignore

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

    def _parse_config(self, config: dict) -> None:
        self.storage_path = self._get_dir(
            self._get_config_key(config, "storage_path")
        ) / pathlib.Path(STORAGE_FILE)
        self.code_path = self._get_dir(self._get_config_key(config, "code_path"))
        self.code_gen_path = self._get_dir(
            self._get_config_key(config, "code_gen_path")
        )

        self._parse_document_paths(config)

    def _parse_document_paths(self, config: dict) -> None:
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

    def store(self, data: FileData) -> None:
        """Writes FileData to storage_path."""
        with self.storage_path.open("wb") as file:
            pickle.dump(data, file)

    def fetch(self) -> FileData:
        """Fetches pickled FileData from storage_path."""
        if not self.storage_path.is_file():
            return {}

        with self.storage_path.open("rb") as file:
            return pickle.load(file)

    def write_file(self, name: str, code: str) -> None:
        """Writes code to the specified file."""
        with (self.code_path / name).open("w") as file:
            file.write(code)

    def read_file(self, name: str) -> str | None:
        """Reads code from the specified file.

        Returns `None` if the file does not exist."""
        path = self.code_path / name
        if not path.is_file():
            return None
        return path.read_text()

    def get_document(self, name: str) -> api_path.DocumentPath | None:
        return self.documents.get(name, None)
