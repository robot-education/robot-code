from src.library import stmt, utils, base

AUTO_FILE = """
/**
* Auto-generated file. Do not edit!
*/\n
"""


class Studio(base.ParentNode[stmt.Statement]):
    def __init__(self, studio_name: str) -> None:
        self.studio_name = studio_name
        super().__init__()

    def __str__(self) -> str:
        """Process the contents of the feature studio."""
        return AUTO_FILE + utils.to_str(self.child_nodes, sep="\n")

    def send(self) -> None:
        """Sends the output of the studio to onshape."""
        # code = str(self)
        pass

    def print(self) -> None:
        print(str(self))
