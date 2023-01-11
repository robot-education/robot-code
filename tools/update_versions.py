from fs_client import FeatureStudioClient
from fs_constants import DOCUMENT_PATHS
# Regex
import re


class Main():
    def __init__(self) -> None:
        self._api = Onshape(logging=False)
        self._client = FeatureStudioClient(self._api)
        self._STD_VERSION = self._client.std_version()
        self._DOCUMENT_PATHS = DOCUMENT_PATHS
        self._OUTDATED_VERSION_MATCH =
        re.compile('version : "(\d{4,6})\.0"|FeatureScript (\d{4,6});')

    def main(self) -> None:
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

    def _extract_feature_paths(self) -> [Path]:
        feature_studio_paths = []
        for path in DOCUMENT_PATHS:
            feature_studio_paths.extend(
                self._client.get_document_feature_studio_paths(path))
        return feature_studio_paths

    def update_version(contents: str) -> str:
        return re.sub(pattern=OUTDATED_VERSION_MATCH, repl=self._replace_number, string=contents)

    def _replace_number(match_obj: object) -> str:
        return re.sub(pattern='\d{4,6}', repl=str(std_version), string=match_obj.group(0))


# call main
Main().main()
