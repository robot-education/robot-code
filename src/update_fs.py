from apikey.client import Client
# Regex
import re
import io

# backend_test_eid = '91306cb3a17c3137dde28dd3'

latest_version = 1930
lines_to_check = 40

documents = [
    # backend
    {'did': '00dd11dabe44da2db458f898', 'wid': '6c20cd994b174cc99668701f'},
    # frontend
    {'did': '9cffa92db8b62219498f89af', 'wid': '06b332ccabc9d2e0aa0abf88'}
]

outdated_version_match = re.compile('version : "(\d{4,6})\.0"|FeatureScript (\d{4,6});')


def replace_number(match_obj):
    return re.sub(pattern='\d{4,6}', repl=str(latest_version), string=match_obj.group(0))


def update_version(contents):
    return re.sub(pattern=outdated_version_match, repl=replace_number, string=contents)


def main():
    c = Client(logging=False)

    print("Fetching feature studios.")
    for ids in documents:
        feature_studio_ids = c.get_document_feature_studio_ids(ids['did'], ids['wid'])

        num_studios = len(feature_studio_ids)
        print("Successfully fetched {} feature studios." .format(num_studios))

        for i, eid in enumerate(feature_studio_ids):  # feature_studio_ids):
            contents = c.get_feature_studio_code(ids['did'], ids['wid'], eid)
            lines = io.StringIO(contents).readlines()
            for j in range(min(lines_to_check, len(lines))):
                lines[j] = update_version(lines[j])
            new_contents = ''.join(lines)
            c.update_feature_studio_code(ids['did'], ids['wid'], eid, new_contents)
            print("Successfully updated feature studio {} of {}.".format(i + 1, num_studios))

        print("Successfully updated {} studios.".format(num_studios))


# call main
main()


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
