from abc import ABC, abstractmethod


class Node(ABC):
    def __init__(self):
        pass

    @abstractmethod
    def enter(self) -> str:
        raise NotImplementedError

    @abstractmethod
    def exit(self) -> str:
        raise NotImplementedError


class InlineNode(Node, ABC):
    def __init__(self):
        pass

    def exit(self) -> str:
        return ""


class DummyNode(InlineNode):
    """An empty node."""

    def enter(self) -> str:
        return ""


dummy_node = DummyNode()


def enter(*nodes: Node) -> tuple[str, ...]:
    return tuple(node.enter() for node in nodes)


def exit(*nodes: Node) -> tuple[str, ...]:
    return tuple(node.exit() for node in nodes)
