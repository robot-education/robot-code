import re

from library.api import api, client, conf, storage

OUTDATED_VERSION_MATCH: re.Pattern[str] = re.compile(
    'version : "(\\d{4,6})\\.0"|FeatureScript (\\d{4,6});'
)


class StudioManager:
    def __init__(self, config: conf.Config, client: client.StudioClient):
        self.config = config
        self.client = client

    def pull(self) -> None:
        """
        Procedure:
        1. Pull all feature studios from Onshape.
        2. Load feature studios from storage.
        3. If the microversion ids are the same, do nothing (even if the local version is modified).
        4. Else if the microversion ids are different and the local version is modified, report a warning and abort.
        5. Else if the microversion ids are different and the local version is not modified, pull.
        6. For each file which was pulled, update the microversion.
        """
        feature_studios = [
            studio
            for path in self.config.documents.values()
            for studio in self.client.get_studios(path)
        ]
        num_studios = len(feature_studios)
        print("Found {} feature studios to pull.".format(num_studios))

        for feature_studio in feature_studios:
            code = self.client.get_code(feature_studio.path)
            storage.write_studio(self.config.code_path, feature_studio.name, code)

        print("Pulled {} feature studios.".format(num_studios))

    def push(self) -> None:
        """
        Procedure:
        1. Load feature studios from Onshape.
        2. For each feature studio in storage which has been modified, compare microversions with Onshape. If they don't match, report a warning and abort.
        3. Else, push modified files to Onshape.
        4. Mark all files as the same and update their microversions.
        """
        pass

    #     id_to_name = json.loads(self._read_from_file(_ID_FILE))
    #     items = id_to_name.items()
    #     for id, name in items:
    #         code = self._read_from_file(name, _FOLDER_PATH)
    #         path = api.Path(self._document_path.did, self._document_path.wid, id)
    #         self._client.update_code(path, code)

    #     print("Pushed {} files to Onshape.".format(len(items)))

    # def update_std(self) -> None:
    #     files = os.listdir(path=_FOLDER_PATH)
    #     std_version = self._client.std_version()
    #     updated = 0
    #     for file in files:
    #         contents = self._read_from_file(file, _FOLDER_PATH)
    #         new_contents = self._update_version(contents, std_version)
    #         if new_contents != contents:
    #             updated += 1
    #             self._write_to_file(file, new_contents, _FOLDER_PATH)
    #     print("Modified {} files.".format(updated))

    #     self.push()

    # def _update_version(self, contents: str, std_version: str) -> str:
    #     return re.sub(
    #         pattern=OUTDATED_VERSION_MATCH,
    #         repl=self._generate_update_replace(std_version),
    #         string=contents,
    #     )

    # def _generate_update_replace(self, std_version: str) -> Callable[[re.Match], str]:
    #     def replace_number(match: re.Match) -> str:
    #         return re.sub(
    #             pattern="\\d{4,6}", repl=str(std_version), string=match.group(0)
    #         )

    #     return replace_number

    def clean(self) -> None:
        for path in [self.config.code_path, self.config.storage_path]:
            path.rmdir()

        # files = os.listdir(_FOLDER_PATH)
        # for file in files:
        #     os.remove(os.path.join(_FOLDER_PATH, file))

        # if os.path.isfile(_ID_FILE):
        #     os.remove(_ID_FILE)


def make_manager(config: conf.Config, logging: bool = False) -> StudioManager:
    feature_client = client.StudioClient(api.Api(logging=logging))
    return StudioManager(config, feature_client)
