from typing_extensions import override
from library.base import node, ctxt


class Import(node.TopStatement):
    def __init__(self, studio_name: str, document_name: str | None = None) -> None:
        pass

    @override
    def build_top(self, context: ctxt.Context) -> str:
        return "import blarg;"


def std_import(studio_name: str) -> Import:
    return Import(studio_name, document_name="std")
