from apikey.onshape import Onshape, Path, ApiPath
import json

class Part:
    def __init__(self, document_id: str, middle_id: str, element_id: str, part_id: str) -> None:
        self.document_id = document_id
        self.middle_id = middle_id
        self.element_id = element_id
        self.part_id = part_id

    def part_studio_path(self) -> Path:
        path = Path(self.document_id, self.middle_id, self.element_id)
        path.middle_type = 'm'
        return path

class AssemblyClient:
    def __init__(self, api: Onshape) -> None:
        self._api = api

    def get_assembly_features(self, assembly_path: Path) -> object:
        apiPath = ApiPath('assemblies', assembly_path, 'features')
        return self._api.request('get', apiPath)
    
    def get_assembly(self, assembly_path: Path)-> object:
        apiPath = ApiPath('assemblies', assembly_path) 
        query = { 'includeMateFeatures' : True, 'includeMateConnectors' : True, 'excludeSuppressed' : True }
        return self._api.request('get', apiPath, query=query)

    def add_part_to_assembly(self, assembly_path: Path, part_studio_path: Path, part_id: str) -> object:
        body = {
            'documentId': part_studio_path.document_id,
            'elementId': part_studio_path.element_id,
            'workspaceId': part_studio_path.middle_id,
            'includePartTypes': ['PARTS'],
            'isWholePartStudio': False,
            'partId': part_id
        }
        apiPath = ApiPath('assemblies', assembly_path, 'instances')
        return self._api.request('post', apiPath, body=body)
    
    def add_part_studio_to_assembly(self, assembly_path: Path, part_studio_path: Path) -> str:
        body = {
            'documentId': part_studio_path.document_id,
            'workspaceId': part_studio_path.middle_id,
            'elementId': part_studio_path.element_id,
            'includePartTypes': ['PARTS'],
            'isWholePartStudio': True
        }
        apiPath = ApiPath('assemblies', assembly_path, 'instances')
        self._api.request('post', apiPath, body=body)

        query = {
            'includeNonSolids' : False
        }
        apiPath = ApiPath('assemblies', assembly_path)
        result = self._api.request('get', apiPath, query=query)
        return result['rootAssembly']['instances'][-1]['id']


    def evaluate_feature_script(self, part_studio_path: Path, code: str) -> object:
        apiPath = ApiPath('partstudios', part_studio_path, 'featurescript')
        body = { 'script': code }
        return self._api.request('post', apiPath, body=body)
    
    def add_assembly_feature(self, assembly_path: Path, feature: object):
        apiPath = ApiPath('assemblies', assembly_path, 'features')
        return self._api.request('post', apiPath, body={ 'feature' : feature })