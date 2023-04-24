from abc import ABC, abstractmethod
from typing import Iterable, Iterator, TypeVar, Generic


class Node(ABC):
    @abstractmethod
    def __str__(self) -> str:
        raise NotImplementedError


T = TypeVar("T", bound=Node)
# S is assumed to be superclass of T
S = TypeVar("S", bound=Node)


class ParentNode(Node, ABC, Generic[T]):
    """A node which supports an array of (possibly nested) children.

    Note __str__ is not defined for this class; parents should implement themselves.
    """

    def __init__(self, child_nodes: Iterable[T] = []) -> None:
        self.child_nodes: list[T] = list(child_nodes)

    def add(self, node: S) -> S:
        """Adds a node as a child.

        Returns the node which was passed in."""
        # We assume S extends T
        self.child_nodes.append(node)  # type: ignore
        return node

    def __iter__(self) -> Iterator[T]:
        return self.child_nodes.__iter__()

    def __len__(self) -> int:
        return len(self.child_nodes)


class Map(Node):
    """Defines a map literal."""

    def __init__(
        self,
        dict: dict[str, str],
        quote_values: bool = False,
        exclude_keys: Iterable[str] = [],
    ):
        """
        quote_values: Whether to add quotation marks around each value.
        exclude_keys: Specifies keys to ignore when quoting. Does nothing if quote_values is False.
        """
        self.dict = dict
        self.quote_values = quote_values
        self.exclude_values = exclude_keys

    def _quote_format_str(self, quote_value: bool) -> str:
        return ' "{}" : "{}"' if quote_value else ' "{}" : {}'

    def __str__(self) -> str:
        pairs = [
            self._quote_format_str(
                self.quote_values and key not in self.exclude_values
            ).format(key, value)
            for key, value in self.dict.items()
            if value is not None
        ]

        if len(pairs) == 0:
            return "{}"

        return "{{{}}}".format(",".join(pairs) + " ")
