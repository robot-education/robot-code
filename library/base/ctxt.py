import collections
import copy
import dataclasses
import enum as std_enum
from typing import Any, Self


class Level(std_enum.Enum):
    DEFINITION = std_enum.auto()
    STATEMENT = std_enum.auto()
    EXPRESSION = std_enum.auto()


@dataclasses.dataclass()
class Context:
    """
    Fields:
        std_version: Stores the current version of the std.
        level: Stores the current statement level.
        enum: Used to indicate whether we are currently defining an enum.
        ui: Used to indicate whether we're in a UI predicate.
        test_predicate: Used to indicate a test predicate.
        indent: Indicates the current indentation level. Used for inlining.

    ui and test_predicate are used together to trigger automatic inlining of predicates,
      which circumvents nested predicate restrictions.
    """

    std_version: str
    level: Level = Level.DEFINITION
    enum: bool = False
    ui: bool = False
    test_predicate: bool = False
    indent: int = 0

    stack: collections.deque[dict] = dataclasses.field(
        default_factory=lambda: collections.deque()
    )

    def set_definition(self) -> Self:
        self.level = Level.DEFINITION
        return self

    def set_statement(self) -> Self:
        self.level = Level.STATEMENT
        return self

    def set_expression(self) -> Self:
        self.level = Level.EXPRESSION
        return self

    def is_definition(self) -> bool:
        return self.level == Level.DEFINITION

    def is_statement(self) -> bool:
        return self.level == Level.STATEMENT

    def is_expression(self) -> bool:
        return self.level == Level.EXPRESSION

    def as_dict(self) -> dict[str, Any]:
        return dict(
            (field.name, copy.copy(getattr(self, field.name)))
            for field in dataclasses.fields(self)
            if field.name != "stack"
        )

    def save(self) -> None:
        self.stack.append(self.as_dict())

    def restore(self) -> None:
        for key, value in self.stack.pop().items():
            setattr(self, key, value)
