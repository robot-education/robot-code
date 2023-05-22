import collections
import copy
import dataclasses
from typing import Any

_SAVED_FIELDS = ["enum", "ui", "test_predicate", "indent"]


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
    api_manager: None

    enum: bool = False
    ui: bool = False
    test_predicate: bool = False
    indent: int = 0

    stack: collections.deque[dict] = dataclasses.field(
        default_factory=lambda: collections.deque()
    )

    def as_dict(self) -> dict[str, Any]:
        return dict(
            (field.name, copy.copy(getattr(self, field.name)))
            for field in dataclasses.fields(self)
            if field.name in _SAVED_FIELDS
        )

    def save(self) -> None:
        self.stack.append(self.as_dict())

    def restore(self) -> None:
        for key, value in self.stack.pop().items():
            setattr(self, key, value)
