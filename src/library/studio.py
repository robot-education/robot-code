from typing import Self
from src.library import stmt, base

AUTO_FILE = """
/**
* Auto-generated file. Do not edit!
*/\n
"""


class Studio:
    def __init__(self, studio_name: str) -> None:
        self.studio_name = studio_name
        self.statements = []

    def __add__(self, statement: stmt.Statement) -> Self:
        self.statements.append(statement)
        return self

    def __str__(self) -> str:
        """Process the contents of the feature studio."""
        return AUTO_FILE + "\n".join(base.to_str(self.statements))

    def send(self) -> None:
        """Sends the output of the studio to onshape."""
        pass

    def print(self) -> None:
        print(str(self))
