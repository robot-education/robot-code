from typing import Self
from src.library import base


class Statements(base.InlineNode):
    def __init__(self, *statements) -> None:
        self.statements = statements

    def enter(self) -> str:
        return "\n".join(base.enter(*self.statements))


class Statement(Statements):
    def __init__(self) -> None:
        pass

    def enter(self) -> str:
        return ""


class Condition(base.InlineNode):
    def __init__(self, token: str) -> None:
        self.out = token

    def __and__(self, node: Self):
        self.out += " && "
        return node

    def __or__(self, node: Self):
        self.out += " || "
        return node

    def enter(self) -> str:
        return ""

    def exit(self) -> str:
        return ""


class ConditionalStatement(Statement):
    def enter(self, condition: Condition) -> str:
        return ""