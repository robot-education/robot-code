from library import base

AUTO_FILE = """
/* Automatically generated file -- DO NOT EDIT */\n
"""

__all__ = ["Studio"]


class Studio(base.ParentNode):
    def __init__(self, studio_name: str) -> None:
        super().__init__()
        self.studio_name = studio_name

    def __str__(self) -> str:
        """Process the contents of the feature studio."""
        return AUTO_FILE + self.children_str(sep="\n")

    def send(self) -> None:
        """Sends the output of the studio to onshape."""
        # code = str(self)
        pass

    def print(self) -> None:
        print(str(self))
