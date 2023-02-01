from apikey.onshape import Onshape, ApiPath
from fs_constants import BACKEND_PATH
from FeatureStudioClient import FeatureStudioClient

def add_feature(api: Onshape):
    user_id = '5eace32713a966103efd2a9c'
    body = {}
    # body = {
    #     'tool': {
    #         'namespace' : {
    #             'documentId': '00dd11dabe44da2db458f898',
    #             'versionId': '2e434de201984a09c6fc3a8d',
    #             'elementId': '14320b03695982a66d6b3903',
    #             'featureId': '0f2b184b6919f39263492455'
    #         }
    #     }
    # }
    # '63d5919090998a45c82ec92f'

    path = ApiPath('toolbar/tools/u/' + user_id + '/create') 
    result = api.request('post', path, body=body)
    print(result)

def main():
    api = Onshape(logging=True)
    # client = FeatureStudioClient(api)
    # path = BACKEND_PATH

if __name__ == '__main__':
    main()