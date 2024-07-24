from onshape_api.api.api_base import Api
from onshape_api.endpoints.documents import get_document_elements
from onshape_api.paths.paths import (
    ElementPath,
    InstancePath,
    url_to_element_path,
    url_to_instance_path,
)


class Documents:
    def __init__(self, test: bool = False):
        self.test = test

    @property
    def frontend(self) -> InstancePath:
        return TEST_FRONTEND if self.test else FRONTEND

    @property
    def backend(self) -> InstancePath:
        return TEST_BACKEND if self.test else BACKEND

    def frontend_element(self, api: Api, element_name: str) -> ElementPath:
        return get_element_path(api, self.frontend, element_name)

    def backend_element(self, api: Api, element_name: str) -> ElementPath:
        return get_element_path(api, self.backend, element_name)


def get_element_path(api: Api, instance_path: InstancePath, element_name: str):
    elements = get_document_elements(api, instance_path)
    element = next(filter(lambda element: element["name"] == element_name, elements))
    return ElementPath.from_path(instance_path, element["id"])


FRONTEND = url_to_instance_path(
    "https://cad.onshape.com/documents/9cffa92db8b62219498f89af/w/06b332ccabc9d2e0aa0abf88"
)

BACKEND = url_to_instance_path(
    "https://cad.onshape.com/documents/00dd11dabe44da2db458f898/w/6c20cda994b174cc99668701f"
)

TEST_FRONTEND = url_to_instance_path(
    "https://cad.onshape.com/documents/7e1c9ad7c60518598a860df9/w/97d58ea0bdbe639ad11f913b"
)

TEST_BACKEND = url_to_instance_path(
    "https://cad.onshape.com/documents/151181eeae512b9b7881db08/w/280000cbb686168537e34b00"
)

BASE = url_to_element_path(
    "https://cad.onshape.com/documents/769b556baf61d32b18813fd0/w/e6d6c2b3a472b97a7e352949/e/8a0c13d3b2b68a99502dc436"
)

TARGET = url_to_element_path(
    "https://cad.onshape.com/documents/22f3d6ea902783d4d2edb393/w/f73628fa0b8129209cec904f/e/23b3c801977fa988d3fc7405"
)
