import json5
import pathlib
import pickle
from typing import Any, Protocol
from featurescript import feature_studio
from onshape_api.paths import paths

STORAGE_FILE: str = "studio_data.pickle"

FileData = dict[str, feature_studio.FeatureStudio]


class ConfigData(Protocol):
    """
    Attributes:
        storage_path: A path to a file to store data under.
        code_path: A dict of data.
    """

    storage_path: dict | None
    code_path: dict | None
    code_gen_path: dict | None


class Config:
    """Represents config information specified by the user."""

    def __init__(self) -> None:
        path = pathlib.Path("config.json")
        if not path.is_file:
            raise IOError("Failed to find conf.json in the root directory")
        config = json5.load(path.open())
        self._parse_config(config)  # type: ignore

    def _get_config_key(self, config: dict, key: str) -> Any:
        """Fetches the value of key from config. Throws if key does not exist."""
        value = config.get(key, None)
        if value is None:
            raise KeyError("config.json must contain a {} field".format(key))
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
        documents: dict[str, str] = self._get_config_key(config, "documents")
        self.documents: dict[str, paths.InstancePath] = dict(
            (document_name, paths.url_to_element_path(url))
            for document_name, url in documents.items()
        )

    def write(self, data: FileData) -> None:
        """Writes FileData to storage_path."""
        with self.storage_path.open("wb") as file:
            pickle.dump(data, file)

    def read(self) -> FileData:
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

        Returns None if the file does not exist."""
        path = self.code_path / name
        if not path.is_file():
            return None
        return path.read_text()
