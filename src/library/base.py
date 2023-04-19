from abc import ABC, abstractmethod
from typing import Self


class Node(ABC):
    def __init__(self):
        pass

    @abstractmethod
    def enter(self) -> str:
        raise NotImplementedError

    def exit(self) -> str:
        return ""


class DummyNode(Node):
    def enter(self) -> str:
        return ""

dummy_node = DummyNode()
