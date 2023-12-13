from __future__ import annotations
from typing import Iterable
from typing_extensions import override
from featurescript.base import ctxt, expr, node, str_utils, user_error
from featurescript.core import map, std


class LookupTable(std.Const):
    """Represents a lookup table with terminal values which are all maps."""

    def __init__(self, name: str, root: LookupNode):
        super().__init__(name, root, export=True)

    def build(self, context: ctxt.Context) -> str:
        if context.scope == ctxt.Scope.TOP:
            return super().build(context)
        return user_error.expected_scope(ctxt.Scope.TOP)


class LookupPayload(node.Node):
    """Represents an individual option the user may choose in the tree."""

    def __init__(
        self,
        display_name: str,
        next: LookupNode | None = None,
        value: dict[str, str] | None = None,
    ) -> None:
        """
        Args:
            display_name: The display name of the option.
            next: The next level in the tree.
            value: A dict of options to merged into the final map of each node which passes through.
        """
        self.display_name = display_name
        self.value = value
        self.next = next


class LookupNode(node.ParentNode, expr.Expression):
    """Represents a set of options in the tree the user may choose from."""

    def __init__(
        self,
        name: str,
        entries: Iterable[LookupPayload],
        display_name: str | None = None,
        default: str | None = None,
    ) -> None:
        """
        Args:
            name: The internal name of the node.
            entries: The options to choose from.
        """
        self.name = name
        self.display_name = display_name or str_utils.display_name(self.name)
        self.default = default
        super().__init__(entries)

    def set_next_node(self, node: LookupNode):
        """Sets the next level in the tree to the given node."""
        for child in self.children:
            child.next = node

    @override
    def build(self, context: ctxt.Context):
        pass
