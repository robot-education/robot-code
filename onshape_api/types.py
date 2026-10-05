"""Hand-written subsets of the Onshape API's response shapes: only the fields we read.

For a field's real shape, read (or regenerate) onshape_api/reference/openapi.json; see
onshape_api/reference/generate.py. Schema names from the definition are noted on each type.
"""

from __future__ import annotations

import enum
from typing import Literal, NotRequired, TypedDict


class ElementType(enum.StrEnum):
    """Element (tab) types in a document (GBTElementType values we handle)."""

    PART_STUDIO = "PARTSTUDIO"
    ASSEMBLY = "ASSEMBLY"
    DRAWING = "DRAWING"
    FEATURE_STUDIO = "FEATURESTUDIO"
    BLOB = "BLOB"


# === documents (GET .../contents, GET .../elements) ===


class DocumentElement(TypedDict):
    """An element (tab) in a document. BTDocumentElementInfo."""

    id: str
    name: str
    # Kept as a str since documents can hold element types we don't handle
    elementType: str
    # Changes whenever the element changes (including, possibly, indirectly)
    microversionId: str


class ElementReference(TypedDict):
    """A reference to an element in the folder tree. BTDocumentElementReference-2484."""

    btType: Literal["BTDocumentElementReference-2484"]
    elementId: str


class ElementGroup(TypedDict):
    """A folder in the folder tree. BTElementGroup-1458."""

    btType: Literal["BTElementGroup-1458"]
    # Empty for the root folder
    groupName: NotRequired[str]
    # Child folders and element references, in display order
    groups: list[ElementGroup | ElementReference]


class DocumentContents(TypedDict):
    """GET /documents/d/{did}/{wvm}/{wvmid}/contents. BTDocumentContentsInfo."""

    # Every element, regardless of folder
    elements: list[DocumentElement]
    # The root folder
    folders: ElementGroup


# === versions (GET/POST /documents/d/{did}/versions) ===


class VersionInfo(TypedDict):
    """A version of a document. BTVersionInfo."""

    id: str
    name: str
    description: NotRequired[str]


# === feature studios ===


class FeatureStudioContents(TypedDict):
    """GET/POST /featurestudios/d/{did}/{wvm}/{wvmid}/e/{eid}. BTFeatureStudioContents-2239.

    The definition doesn't list any compile errors or notices in the response.
    """

    contents: str
    # The document (not element) microversion the contents were read from or written to
    sourceMicroversion: NotRequired[str]


class FeatureStudioInfo(TypedDict):
    """POST /featurestudios/d/{did}/w/{wid}. BTDocumentElementInfo."""

    id: str
    name: str
    microversionId: str


class FeatureSpec(TypedDict):
    """A custom feature defined in a Feature Studio. BTFeatureSpec-129."""

    featureTypeName: str


class FeatureSpecs(TypedDict):
    """GET /featurestudios/d/{did}/{wvm}/{wvmid}/e/{eid}/featurespecs. BTFeatureSpecsResponse-664."""

    featureSpecs: list[FeatureSpec]
