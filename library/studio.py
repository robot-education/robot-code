from library import stmt, utils, base

AUTO_FILE = """
/* Automatically generated file -- DO NOT EDIT */\n
"""

__all__ = ["Studio"]


class Studio(stmt.Parent):
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
