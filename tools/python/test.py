from apikey.onshape import Onshape, Path, ApiPath
from fs_constants import BACKEND_PATH
import os
from typing import Optional

from fs_client import FeatureStudioClient

def write_to_file(file_name: str, content: str, file_path: Optional[str] = None) -> None:
    path = os.path.join(file_path, file_name) if file_path is not None else file_name
    file = open(path, 'w', encoding='utf-8')
    file.write(content)
    file.close()

def main(): 
    api = Onshape(logging=False)
    client = FeatureStudioClient(api)
    path = BACKEND_PATH
    partStudioPath = path.copy()

    path.eid = 'e7bc0975feec4017398207a4'
    code = client.get_code(path)

    body = { "script" : code }
    partStudioPath.eid = 'c481a23972b3b7259bd9758b'
    result = api.request('post', ApiPath('partstudios', partStudioPath, 'featurescript'), body=body).json()
    print(result)
    write_to_file('out.log', str(result))

if __name__ == '__main__':
    main()