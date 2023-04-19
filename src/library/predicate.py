from typing import Self
from src.library import base


class Predicate(base.Node):
    def __init__(
        self,
        name: str,
        args: base.Node = base.dummy_node,
    ):
        self._name = name
        self._args = args
        self._statements = base.dummy_node

    def children(self, statements: base.Node) -> Self:
        self._statements = statements
        return self

    def enter(self) -> str:
        return "predicate {}({})\n{\n".format(name=self._name, args=self._args.enter())

    def exit(self) -> str:
        return "};\n"


class UiPredicate(Predicate):
    def __init__(self, name: str):
        super().__init__(name, args=base.dummy_node)


class Parameter(base.Node):
    def __init__(self, name: str, type: str):
        self.name = name
        self.type = type

    def enter(self) -> str:
        return "{} is {}".format(self.name, self.type)


class Parameters(base.Node):
    def __init__(self, parameters: list[Parameter]):
        pass
