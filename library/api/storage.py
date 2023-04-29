import pathlib
import pickle
import dataclasses
from typing import Iterable
from library.api import api_path


@dataclasses.dataclass
class FeatureStudio:
    name: str
    path: api_path.StudioPath
    microversion_id: str | None = None
    modified: bool = dataclasses.field(default=False)
    generated: bool = dataclasses.field(default=False)


# @dataclasses.dataclass
# class Document(dict[str, FeatureStudio]):
#     name: str
#     path: api_path.DocumentPath
#     studios: dict[str, FeatureStudio]


@dataclasses.dataclass
class FileData:
    documents: dict[str, FeatureStudio]
    feature_studios: Iterable[FeatureStudio]


def store(storage_path: pathlib.Path, data: FileData) -> None:
    with storage_path.open("wb") as file:
        pickle.dump(data, file)


def fetch(storage_path: pathlib.Path) -> FileData:
    with storage_path.open("rb") as file:
        return pickle.load(file)


def write_studio(code_path: pathlib.Path, name: str, code: str) -> None:
    with (code_path / name).open("w") as file:
        file.write(code)


def read_studio(code_path: pathlib.Path, name) -> str:
    with (code_path / name).open("r") as file:
        return file.read()
