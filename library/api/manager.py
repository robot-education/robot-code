import functools
import importlib
import importlib.util
import re
import shutil
from concurrent import futures
from typing import Callable, Iterable
from library.base import ctxt, studio

from library.api import api_base, conf
from library.api.endpoints import documents, feature_studios

OUTDATED_VERSION_MATCH: re.Pattern[str] = re.compile(
    r'version : "(\d{2,7})\.0"|FeatureScript (\d{2,7});'
)

VERSION_SUB_MATCH = re.compile(r"\d{2,7}")

CONFLICT_MESSAGE = """
Some files are in conflict and were skipped.
Use "onshape pull --force" or "onshape push --force" to overwrite.
"""


class CommandLineManager:
    """Uses an api client to handle command line operations."""

    def __init__(self, config: conf.Config, api: api_base.Api):
        self.config = config
        self.api = api
        self.curr_data = self.config.read()
        self.conflict = False

    def _finish(self) -> None:
        self.config.write(self.curr_data)

        if self.conflict:
            print(CONFLICT_MESSAGE)

    def _report_conflict(self, studio: conf.FeatureStudio) -> None:
        self.conflict = True
        print(
            "{} has been modified both locally and in Onshape. Skipping.".format(
                studio.name
            )
        )

    def _get_all_studios(self) -> Iterable[conf.FeatureStudio]:
        """Returns an iterable over all feature studios in Onshape."""
        with futures.ThreadPoolExecutor() as executor:
            threads = [
                executor.submit(documents.get_feature_studios, self.api, path)
                for path in self.config.documents.values()
            ]
        return [
            studio
            for future in futures.as_completed(threads)
            for studio in future.result().values()
        ]

    def pull(self, force: bool = False) -> None:
        """Pull code from Onshape.

        Procedure:
        1. Pull all feature studios from Onshape.
        2. Load feature studios from storage.
        3. If the microversion ids are the same, do nothing (even if the local version is modified).
        4. Else if the microversion ids are different and the local version is modified, report a warning and abort.
        5. Else if the microversion ids are different and the local version is not modified, pull.
        6. For each file which was pulled, update the microversion.
        """
        studios = self._get_all_studios()

        pulled = 0
        with futures.ThreadPoolExecutor() as executor:
            for studio in executor.map(
                functools.partial(self.pull_studio, force), studios
            ):
                if studio:
                    self.curr_data[studio.path.element_id] = studio
                    pulled += 1

        if not self.conflict and pulled == 0:
            print("Already up to date.")
        else:
            print("Pulled {} feature studios.".format(pulled))
            self._finish()

    def pull_studio(
        self, force: bool, studio: conf.FeatureStudio
    ) -> conf.FeatureStudio | None:
        curr_studio = self.curr_data.get(studio.path.element_id, None)
        if curr_studio is not None:
            if (
                curr_studio.microversion_id == studio.microversion_id
                or curr_studio.generated
            ):
                # do nothing if microversions match or the studio is auto-generated
                return None
            elif curr_studio.modified and not force:
                # microversions don't match; potential conflict between cloud and local
                self._report_conflict(studio)
                return None
            # okay to overwrite non-modified studio, set id
            studio.microversion_id = curr_studio.microversion_id

        print("Pulling {}".format(studio.name))

        code = feature_studios.pull_code(self.api, studio.path)
        self.config.write_file(studio.name, code)
        # don't need to worry about reseting generated since generated is never pulled
        return studio

    def push(self, force: bool = False) -> None:
        """Push code to Onshape.

        Procedure:
        1. Load feature studios from Onshape.
        2. For each feature studio in storage which has been modified, compare microversions with Onshape. If they don't match, report a warning and abort.
        3. Else, push modified files to Onshape.
        4. Mark all files as the same and update their microversions.
        """
        studios_to_push = [
            studio
            for studio in self.curr_data.values()
            if studio.modified or studio.microversion_id is None
        ]

        onshape_studios = self._get_all_studios()

        pushed = 0
        with futures.ThreadPoolExecutor() as executor:
            for push_studio in executor.map(
                functools.partial(self.push_studio, onshape_studios, force),
                studios_to_push,
            ):
                if push_studio:
                    # get_microversion_id doesn't work with threading - Onshape bug?
                    self.curr_data[push_studio.path.element_id] = push_studio
                    pushed += 1

        if not self.conflict and pushed == 0:
            print("Everything already up to date.")
        else:
            print("Pushed {} feature studios.".format(pushed))
            self._finish()

    def push_studio(
        self,
        onshape_studios: Iterable[conf.FeatureStudio],
        force: bool,
        push_studio: conf.FeatureStudio,
    ) -> conf.FeatureStudio | None:
        onshape_studio = next(
            filter(
                lambda onshape_studio: onshape_studio.path.element_id
                == push_studio.path.element_id,
                onshape_studios,
            ),
        )

        if (
            not force
            and not push_studio.generated
            and onshape_studio.microversion_id != push_studio.microversion_id
        ):
            self._report_conflict(push_studio)
            return None

        print("Pushing {}".format(push_studio.name))
        code = self.config.read_file(push_studio.name)
        if code is None:
            print("Failed to find studio {}. Skipping.".format(push_studio.name))
            return None

        feature_studios.push_code(self.api, push_studio.path, code)
        push_studio.modified = False
        push_studio.microversion_id = documents.get_microversion_id(
            self.api, push_studio.path
        )
        return push_studio

    def update_versions(self) -> None:
        std_version = feature_studios.std_version(self.api)
        modified = 0
        for id, studio in self.curr_data.items():
            contents = self.config.read_file(studio.name)
            if contents is None:
                print("Failed to find file {}. Skipping.".format(studio.name))
                continue
            new_contents = self._update_version(contents, std_version)
            if new_contents != contents:
                modified += 1
                self.config.write_file(studio.name, new_contents)
                studio.modified = True
                self.curr_data[id] = studio
        if modified == 0:
            print("All files already up to date.")
        else:
            print("Modified {} files.".format(modified))
            self._finish()

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

    def build(self) -> None:
        std_version = feature_studios.std_version(self.api)
        paths = self.config.code_gen_path.rglob("**/*.py")
        count = 0
        for path in paths:
            spec = importlib.util.spec_from_file_location(path.stem, path)
            if spec is None or spec.loader is None:
                raise ValueError("Failed to open {}. Aborting.".format(path.stem))
            module = importlib.util.__loader__.create_module(spec)
            if module is None:
                raise ValueError("Failed to initialize {}. Aborting.".format(path.stem))
            spec.loader.exec_module(module)

            for cls in vars(module).values():
                if isinstance(cls, studio.Studio):
                    if self._send_code(cls, std_version):
                        count += 1

        print("Built {} feature studios.".format(count))
        self._finish()

    def _send_code(self, studio: studio.Studio, std_version: str) -> bool:
        context = ctxt.make_context(std_version, self.config, self.api)
        code = studio.build(context)

        curr = self.config.read_file(studio.studio_name)
        if curr == code:
            print("{}: Build resulted in no changes.".format(studio.studio_name))
            return True

        print("{}: Successfully built.".format(studio.studio_name))
        document = self.config.get_document(studio.document_name)
        if document is None:
            print(
                "{}: Failed to find document in config.json named {}. Valid names are: {}".format(
                    studio.studio_name,
                    studio.document_name,
                    ", ".join(self.config.documents.keys()),
                )
            )
            return False
        studios = documents.get_feature_studios(self.api, document)
        feature_studio = studios.get(studio.studio_name, None)

        if feature_studio is None:
            feature_studio = feature_studios.make_feature_studio(
                self.api, document, studio.studio_name
            )
        feature_studio.generated = True
        feature_studio.modified = True
        self.curr_data[feature_studio.path.element_id] = feature_studio
        self.config.write_file(feature_studio.name, code)
        return True


def clean(config: conf.Config) -> None:
    shutil.rmtree(config.code_path)
    shutil.rmtree(config.storage_path.parent)
