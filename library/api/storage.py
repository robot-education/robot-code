import pathlib
import pickle
import dataclasses
from library.api import api_path


@dataclasses.dataclass
class FeatureStudio:
    name: str
    document_name: str
    path: api_path.StudioPath
    # None to handle newly generated files
    microversion_id: str | None = None
    modified: bool = False
    generated: bool = False


# @dataclasses.dataclass
# class Document(dict[str, FeatureStudio]):
#     name: str
#     path: api_path.DocumentPath
#     studios: dict[str, FeatureStudio]


@dataclasses.dataclass
class FileData:
    studios: dict[str, FeatureStudio]


def store(storage_path: pathlib.Path, data: FileData) -> None:
    with storage_path.open("wb") as file:
        pickle.dump(data, file)


def fetch(storage_path: pathlib.Path) -> FileData:
    if not storage_path.is_file():
        return FileData({})

    with storage_path.open("rb") as file:
        return pickle.load(file)


class CodeWriter:
    def __init__(self, code_path: pathlib.Path) -> None:
        self.code_path = code_path

    def write(self, name: str, code: str) -> None:
        with (self.code_path / name).open("w") as file:
            file.write(code)

    def read(self, name: str) -> str:
        with (self.code_path / name).open("r") as file:
            return file.read()
