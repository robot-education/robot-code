from fsclient import Client
# Regex
import re
import io

OUTDATED_VERSION_MATCH = re.compile('version : "(\d{4,6})\.0"|FeatureScript (\d{4,6});')


def main():
    c = Client(logging=False)
    std_version = c.std_version()

    def replace_number(match_obj):
        return re.sub(pattern='\d{4,6}', repl=str(std_version), string=match_obj.group(0))

    def update_version(contents):
        return re.sub(pattern=OUTDATED_VERSION_MATCH, repl=replace_number, string=contents)

    print("Fetching feature studios.")
    paths = c.get_feature_studio_paths()
    num_studios = len(paths)
    print("Successfully fetched {} feature studios." .format(num_studios))

    for i, path in enumerate(paths):
        code = c.get_code(path)
        new_code = update_version(code)
        # lines = io.StringIO(code).readlines()
        # for j in range(min(LINES_TO_CHECK, len(lines))):
        #     lines[j] = update_version(lines[j])
        # new_code = ''.join(lines)
        c.update_code(path, new_code)
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
