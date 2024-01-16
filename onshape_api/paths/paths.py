"""Contains Object classes for Onshape. Objects can be used to work on Onshape Documents, Workspaces, and Tabs. 

The object model has the following elements:
* Document - Represents an Onshape document. 
    Notably, documents are actually just collections of instances (workspaces and versions), so this isn't inherently useful on it's own.
* Instance - Represents an Onshape workspace, version, or microversion. 
    Workspaces are editable and are thus the most common. Note Onshape allows creating multiple workspaces in the same document, but most users don't utilize this functionality. 
    Versions correspond to explicit versions in the version list.
    Microversions typically correspond to individual edits in the edit history of a document.
* Element - Represents an Onshape tab, such as a Part Studio, Assembly, or Drawing.
* Part - Represents a Part inside a Part Studio.
"""
from __future__ import annotations
import pathlib
from typing import cast
from urllib import parse
from onshape_api.paths.instance_type import InstanceType


class DocumentPath:
    """Represents a path to an Onshape document.

    Note this isn't very useful on its own since Onshape documents are actually collections of different workspaces, versions, and microversions (instances).
    """

    def __init__(self, document_id: str) -> None:
        self.document_id = document_id

    @staticmethod
    def to_api_path(path: DocumentPath) -> str:
        return "/d/{}".format(path.document_id)

    @classmethod
    def to_path(cls, path: DocumentPath) -> DocumentPath:
        return cls(path.document_id)

    def __hash__(self) -> int:
        return hash(self.document_id)

    def __eq__(self, other) -> bool:
        return isinstance(other, DocumentPath) and self.document_id == other.document_id


class InstancePath(DocumentPath):
    """Represents a path to a specific instance (generally a workspace or version) of an Onshape document."""

    def __init__(
        self,
        document_id: str,
        instance_id: str,
        instance_type: str | None = InstanceType.WORKSPACE,
    ) -> None:
        super().__init__(document_id)
        self.instance_id = instance_id
        if instance_type not in InstanceType:
            raise ValueError(
                "Invalid value for instance_type: {}".format(instance_type)
            )
        self._instance_type = cast(InstanceType, instance_type)

    @property
    def instance_type(self) -> InstanceType:
        """Returns the instance type of this object."""
        return self._instance_type

    @property
    def wvm(self) -> str:
        """An alias for the string value of instanceType."""
        return str(self._instance_type)

    @staticmethod
    def to_api_path(instance: InstancePath) -> str:
        """Returns a path to this instance formated for api consumption."""
        return DocumentPath.to_api_path(instance) + "/{}/{}".format(
            instance.wvm, instance.instance_id
        )

    @classmethod
    def from_path(
        cls,
        document_path: DocumentPath,
        instance_id: str,
        instance_type: str | None = InstanceType.WORKSPACE,
    ) -> InstancePath:
        return cls(
            document_path.document_id,
            instance_id,
            instance_type,
        )

    @classmethod
    def to_path(cls, instance: InstancePath) -> InstancePath:
        return cls(instance.document_id, instance.instance_id, instance.instance_type)

    def __hash__(self) -> int:
        return super().__hash__() ^ hash(self.instance_id) ^ hash(self.wvm)

    def __eq__(self, other) -> bool:
        return (
            isinstance(other, InstancePath)
            and super().__eq__(other)
            and self.instance_id == other.instance_id
            and self.wvm == other.wvm
        )


class ElementPath(InstancePath):
    """Represents a document tab in an instance."""

    def __init__(
        self,
        document_id: str,
        instance_id: str,
        element_id: str,
        instance_type: str | None = InstanceType.WORKSPACE,
    ) -> None:
        super().__init__(document_id, instance_id, instance_type)
        self.element_id = element_id

    @staticmethod
    def to_api_path(element: ElementPath) -> str:
        return InstancePath.to_api_path(element) + "/e/" + element.element_id

    @classmethod
    def from_path(cls, instance: InstancePath, element_id: str) -> ElementPath:
        return cls(
            instance.document_id,
            instance.instance_id,
            element_id,
            instance_type=instance.instance_type,
        )

    @classmethod
    def to_path(cls, element: ElementPath) -> ElementPath:
        return cls(
            element.document_id,
            element.instance_id,
            element.element_id,
            instance_type=element.instance_type,
        )

    def to_feature_studio_path(self) -> str:
        """Returns a version of the path suitable for use as the path property of Feature Studio imports."""
        return "{}/{}/{}".format(
            self.document_id,
            self.instance_id,
            self.element_id,
        )

    def __hash__(self) -> int:
        return super().__hash__() ^ hash(self.element_id)

    def __eq__(self, other) -> bool:
        return (
            isinstance(other, ElementPath)
            and self.__eq__(other)
            and self.element_id == other.element_id
        )


class PartPath(ElementPath):
    """Represents a part in a part studio."""

    def __init__(
        self,
        document_id: str,
        instance_id: str,
        element_id: str,
        part_id: str,
        instance_type: str | None = InstanceType.WORKSPACE,
    ) -> None:
        super().__init__(document_id, instance_id, element_id, instance_type)
        self.part_id = part_id

    @classmethod
    def to_path(cls, part: PartPath) -> PartPath:
        return cls(
            part.document_id,
            part.instance_id,
            part.element_id,
            part.part_id,
            part.instance_type,
        )

    @classmethod
    def from_path(cls, element: ElementPath, part_id: str) -> PartPath:
        return cls(
            element.document_id,
            element.instance_id,
            element.element_id,
            part_id,
            element.instance_type,
        )

    def __hash__(self) -> int:
        return super().__hash__() ^ hash(self.part_id)

    def __eq__(self, other) -> bool:
        return (
            isinstance(other, PartPath)
            and self.__eq__(other)
            and self.part_id == other.part_id
        )


def url_to_path(url: str) -> ElementPath:
    """Constructs an ElementPath from an Onshape document url."""
    path = parse.urlparse(url).path
    path = path.removeprefix("/documents")
    parts = pathlib.Path(path).parts
    return ElementPath(parts[1], parts[3], parts[5], instance_type=parts[2])


def path_to_url(object: DocumentPath) -> str:
    """Constructs an Onshape document url from a path."""
    base = "https://cad.onshape.com/documents/"
    if isinstance(object, ElementPath):
        api_path = ElementPath.to_api_path(object)
    elif isinstance(object, InstancePath):
        api_path = InstancePath.to_api_path(object)
    else:
        api_path = DocumentPath.to_api_path(object)
    return base + api_path.removeprefix("/d/")
