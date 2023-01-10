from fs_client import FeatureStudioClient
from fs_constants import DOCUMENT_PATHS
# Regex
import re


class Main():
    def __init__(self):
        self._api = Onshape(logging=False)
        self._client = FeatureStudioClient(self._api)
        self._STD_VERSION = self._client.std_version()
        self._DOCUMENT_PATHS = DOCUMENT_PATHS
        self._OUTDATED_VERSION_MATCH =
        re.compile('version : "(\d{4,6})\.0"|FeatureScript (\d{4,6});')

    def main(self):
        print("Fetching feature studios.")
        paths = self._client.get_feature_studio_paths()
        num_studios = len(paths)
        print("Successfully fetched {} feature studios." .format(num_studios))
        for i, path in enumerate(paths):
            code = self._client.get_code(path)
            new_code = self.update_version(code)
            self._client.update_code(path, new_code)
            print("Successfully updated feature studio {} of {}.".format(i + 1, num_studios))
        print("Successfully updated {} studios.".format(num_studios))

    def _extract_feature_paths(self):
        feature_studio_paths = []
        for path in DOCUMENT_PATHS:
            feature_studio_paths.extend(
                self._client.get_document_feature_studio_paths(path))
        return feature_studio_paths

    def update_version(contents):
        return re.sub(pattern=OUTDATED_VERSION_MATCH, repl=self._replace_number, string=contents)

    def _replace_number(match_obj):
        return re.sub(pattern='\d{4,6}', repl=str(std_version), string=match_obj.group(0))


# call main
Main().main()
'''
# make a new document and grab the document ID and workspace ID
new_doc = c.new_document(public=True).json()

did = new_doc['id']
wid = new_doc['defaultWorkspace']['id']

# get the document details
details = c.get_document(did)
print('Document name: ' + details.json()['name'])

# create a new assembly
asm = c.create_assembly(did, wid)

if asm.json()['name'] == 'My Assembly':
    print('Assembly created')
else:
    print('Error: Assembly not created')

# upload blob
blob = c.upload_blob(did, wid)

# delete the doc
c.del_document(did)

# try to get the doc to make sure it's gone (should be in the trash)
trashed_doc = c.get_document(did)

if trashed_doc.json()['trash'] is True:
    print('Document now in trash')
else:
    print('Error: Document not trashed')
'''
