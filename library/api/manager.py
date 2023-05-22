import importlib
import importlib.util
import re
import shutil
from typing import Callable
from library.base import studio

from library.api import api, client, conf, storage

OUTDATED_VERSION_MATCH: re.Pattern[str] = re.compile(
    r'version : "(\d{2,7})\.0"|FeatureScript (\d{2,7});'
)

VERSION_SUB_MATCH = re.compile(r"\\d{2,7}")

CONFLICT_MESSAGE = """
Some files are in conflict and were skipped.
Use "onshape pull --force" or "onshape push --force" to overwrite.
"""


class CommandLineManager:
    """Uses an api client to handle command line operations."""

    def __init__(self, config: conf.Config, client: client.StudioClient):
        self.config = config
        self.client = client
        self.curr_data = storage.fetch(self.config.storage_path)
        self.conflict = False

    def handle_conflict(self, studio: storage.FeatureStudio) -> None:
        self.conflict = True
        print(
            "{} has been modified both locally and in Onshape. Skipping.".format(
                studio.name
            )
        )

    def get_config_documents(self) -> list[storage.FeatureStudio]:
        return [
            studio
            for path in self.config.documents.values()
            for studio in self.client.get_studios(path)
        ]

    def pull(self, force: bool) -> None:
        """
        Procedure:
        1. Pull all feature studios from Onshape.
        2. Load feature studios from storage.
        3. If the microversion ids are the same, do nothing (even if the local version is modified).
        4. Else if the microversion ids are different and the local version is modified, report a warning and abort.
        5. Else if the microversion ids are different and the local version is not modified, pull.
        6. For each file which was pulled, update the microversion.
        """
        writer = storage.CodeWriter(self.config.code_path)
        studios = self.get_config_documents()
        pulled = 0
        for studio in studios:
            curr_studio = self.curr_data.studios.get(studio.path.id, None)
            if curr_studio is not None:
                # do nothing if microversions match or the studio is auto-generated
                if (
                    curr_studio.microversion_id == studio.microversion_id
                    or curr_studio.generated
                ):
                    continue
                # microversions don't match; potential conflict between cloud and local
                elif curr_studio.modified and not force:
                    self.handle_conflict(studio)
                    continue
                # okay to overwrite non-modified studio, set id
                studio.microversion_id = curr_studio.microversion_id

            print("Pulling {}".format(studio.name))
            code = self.client.get_code(studio.path)
            writer.write(studio.name, code)
            # don't need to worry about reseting generated since generated is never pulled
            self.curr_data.studios[studio.path.id] = studio
            pulled += 1

        if not self.conflict and pulled == 0:
            print("Already up to date.")
        else:
            print("Pulled {} feature studios.".format(pulled))
            self.finish()

    def push(self, force: bool) -> None:
        """
        Procedure:
        1. Load feature studios from Onshape.
        2. For each feature studio in storage which has been modified, compare microversions with Onshape. If they don't match, report a warning and abort.
        3. Else, push modified files to Onshape.
        4. Mark all files as the same and update their microversions.
        """
        studios_to_push = [
            studio
            for studio in self.curr_data.studios.values()
            if studio.modified or studio.microversion_id is None
        ]

        writer = storage.CodeWriter(self.config.code_path)
        onshape_studios = self.get_config_documents()

        pushed = 0
        for studio in studios_to_push:
            onshape_studio = next(
                filter(
                    lambda onshape_studio: onshape_studio.path.id == studio.path.id,
                    onshape_studios,
                )
            )
            if not force and onshape_studio.microversion_id != studio.microversion_id:
                self.handle_conflict(studio)
                continue

            print("Pushing {}".format(studio.name))
            self.client.update_code(studio.path, writer.read(studio.name))
            studio.modified = False
            studio.microversion_id = self.client.get_microversion_id(studio.path)
            self.curr_data.studios[studio.path.id] = studio
            pushed += 1

        if not self.conflict and pushed == 0:
            print("Everything already up to date.")
        else:
            print("Pushed {} feature studios.".format(pushed))
            self.finish()

    def update_versions(self) -> None:
        writer = storage.CodeWriter(self.config.code_path)
        std_version = self.client.std_version()
        modified = 0
        for id, studio in self.curr_data.studios.items():
            contents = writer.read(studio.name)
            new_contents = self._update_version(contents, std_version)
            if new_contents != contents:
                modified += 1
                writer.write(studio.name, new_contents)
                studio.modified = True
                self.curr_data.studios[id] = studio
        if modified == 0:
            print("All files already up to date.")
        else:
            print("Modified {} files.".format(modified))
            self.finish()

    def _update_version(self, contents: str, std_version: str) -> str:
        return re.sub(
            pattern=OUTDATED_VERSION_MATCH,
            repl=self._generate_update_replace(std_version),
            string=contents,
        )

    def _generate_update_replace(self, std_version: str) -> Callable[[re.Match], str]:
        def replace_number(match: re.Match) -> str:
            return re.sub(
                pattern=VERSION_SUB_MATCH, repl=std_version, string=match.group(0)
            )

        return replace_number

    def finish(self) -> None:
        storage.store(self.config.storage_path, self.curr_data)

        if self.conflict:
            print(CONFLICT_MESSAGE)

    def clean(self) -> None:
        shutil.rmtree(self.config.code_path)
        shutil.rmtree(self.config.storage_path.parent)

    def build(self) -> None:
        std_version = self.client.std_version()
        paths = self.config.code_gen_path.rglob("**/*.py")
        count = 0

        for path in paths:
            # output = runpy.run_path(path.as_posix())
            spec = importlib.util.spec_from_file_location(path.stem, path.as_posix())
            output = spec.loader.load_module()  # type: ignore
            for cls in vars(output).values():
                if isinstance(cls, studio.Studio):
                    if self.send_code(cls, std_version):
                        count += 1

        print("Built {} feature studios.".format(count))
        self.finish()

    def send_code(self, studio: studio.Studio, std_version: str) -> bool:
        writer = storage.CodeWriter(self.config.code_path)
        code = studio.build_studio(std_version)
        curr = writer.read(studio.studio_name)
        if curr == code:
            print("{}: Build resulted in no changes.".format(studio.studio_name))
            return True  # Still count it as built

        document = self.config.documents.get(studio.document_name, None)
        if document is None:
            print(
                "{}: Failed to find document in config.json named {}. Skipping.".format(
                    studio.studio_name, studio.document_name
                )
            )
            return False
        feature_studios = self.client.get_studios(document)
        feature_studio = next(
            filter(
                lambda feature_studio: feature_studio.name == studio.studio_name,
                feature_studios,
            ),
            None,
        )

        if feature_studio is None:
            feature_studio = self.client.make_feature_studio(
                document, studio.studio_name
            )
        feature_studio.generated = True
        feature_studio.modified = True
        self.curr_data.studios[feature_studio.path.id] = feature_studio
        writer.write(feature_studio.name, code)
        return True


def make_manager(config: conf.Config, logging: bool = False) -> CommandLineManager:
    feature_client = client.StudioClient(api.Api(logging=logging))
    return CommandLineManager(config, feature_client)
