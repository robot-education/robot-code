from __future__ import annotations
import collections
import copy
import dataclasses
import enum as std_enum
from typing import TYPE_CHECKING, Any

from library.api import api_base, conf

if TYPE_CHECKING:  # prevent circular import
    from library.base import imp

_STACK_FIELDS = ["enum", "ui", "test_predicate", "indent", "scope"]
"""A list of fields which use the stack (meaning they automatically revert once the setting node exits)."""


class Scope(std_enum.StrEnum):
    """Defines high level scopes nodes may be evaluated in."""

    TOP = std_enum.auto()
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

    document_name: str
    std_version: str
    config: conf.Config
    api: api_base.Api
    imports: list[imp.Import] = dataclasses.field(default_factory=list)

    enum: bool = False
    ui: bool = False
    test_predicate: bool = False
    scope: Scope = Scope.TOP
    indent: int = 0

    stack: collections.deque[dict] = dataclasses.field(
        default_factory=collections.deque
    )

    def as_dict(self) -> dict[str, Any]:
        return dict(
            (field.name, copy.copy(getattr(self, field.name)))
            for field in dataclasses.fields(self)
            if field.name in _STACK_FIELDS
        )

    def save(self) -> None:
        self.stack.append(self.as_dict())

    def restore(self) -> None:
        for key, value in self.stack.pop().items():
            setattr(self, key, value)
