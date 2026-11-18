"""Checks the part sets' configurations read back, as their previews read them (`fs_cli.configurations`), offer each
of their records: every configuration a studio's manifest lists shows its own choices, and nothing else."""

import pathlib

import pytest

from fs_cli.config import load_config
from fs_cli.configurations import part_set_views
from fs_cli.configured_studios import configured_studios

ROOT = pathlib.Path(__file__).parents[2]
CONFIG = load_config(ROOT)


def _studios():
    import runpy

    for part_set in CONFIG.parts:
        definition = CONFIG.code_dir / part_set.folder / part_set.definition
        for studio in configured_studios(runpy.run_path(str(definition))):
            yield pytest.param(studio, id=f"{part_set.name}-{studio.name}")


@pytest.mark.parametrize("studio", list(_studios()))
def test_each_configuration_shows_its_own_choices(studio):
    from fs_cli.configurations import parse_configuration

    configuration = parse_configuration(studio.configuration_body())
    for combination in studio.combinations():
        assert {item.input.id: item.value for item in configuration.state(combination)} == combination


def test_a_part_sets_feature_studio_previews_its_studios():
    labels = [view.label for view in part_set_views(CONFIG, CONFIG.code_dir / "bearings" / "bearings.fs")]
    assert labels == ["Bearings: Bearing", "Bearings: Specialty Bearing", "FTC Bearings: Bearing"]
