"""Tests for the live dialog (`fs_cli.live_dialog`): the page's script draws each state of a feature's dialog as
`fs ui --set` renders it, byte for byte."""

import html
import json
import pathlib
import re
import shutil
import subprocess

import pytest

from fs_cli.audit import split_page
from fs_cli.cli import _project
from fs_cli.config import load_config
from fs_cli.live_dialog import page_script
from fs_cli.live_dialog import dialog_data
from fs_cli.ui import render_feature

pytestmark = pytest.mark.skipif(shutil.which("node") is None, reason="needs node")

# What clicking a control sets
SETTING = re.compile(r"data-set='([^']*)' data-value='([^']*)'")

CODE = pathlib.Path(__file__).parents[1] / "featurescripts"


def feature(name: str) -> pathlib.Path:
    """A feature's file, wherever in the code folder it is."""
    [path] = CODE.rglob(name)
    return path


FEATURES = [
    # Every kind of parameter and layout
    "uiTestBench.fs",
    # The deepest conditions (predicates calling predicates, lookup tables), and a "Set By" parameter
    "robotShaft.fs",
    "robotFrame.fs",
]

# Inline icons' ids are made unique by a counter, so they differ with the order they're drawn in
ICON_IDS = re.compile(r"\bi\d+-")


def same(html_text: str) -> str:
    return ICON_IDS.sub("i-", html_text)


def states(render, limit: int) -> list[dict[str, str]]:
    """Settings reached by clicking, breadth first, from the defaults: the dialog's choices, and their combinations."""
    found = [{}]
    seen = {""}
    index = 0
    while index < len(found) and len(found) < limit:
        page = render(found[index])
        for match in SETTING.finditer(page):
            name, value = html.unescape(match.group(1)), html.unescape(match.group(2))
            # Array items are added by count, which the page doesn't do
            if value.isdigit():
                continue
            settings = {**found[index], name: value}
            key = json.dumps(settings, sort_keys=True)
            if key not in seen:
                seen.add(key)
                found.append(settings)
        index += 1
    return found[:limit]


def run_script(script: str, cases: list[dict[str, str]], tmp_path: pathlib.Path) -> list[dict]:
    path = tmp_path / "dialog.js"
    path.write_text(script + f"\nconsole.log(JSON.stringify({json.dumps(cases)}.map((settings) => renderList(settings))));\n")
    result = subprocess.run(["node", str(path)], capture_output=True, text=True, check=True)
    return json.loads(result.stdout)


@pytest.mark.parametrize("name", FEATURES)
def test_script_draws_what_fs_ui_renders(name, tmp_path):
    config = load_config()
    project = _project(config)
    path = feature(name)
    rendered: dict[str, str] = {}

    def render(settings: dict[str, str]) -> str:
        key = json.dumps(settings, sort_keys=True)
        if key not in rendered:
            rendered[key] = render_feature(project, config.std_dir, path, None, settings)[0]
        return rendered[key]

    cases = states(render, 40)
    data, conditions = dialog_data(project, config.std_dir, path, None)
    drawn = run_script(page_script(data, conditions), cases, tmp_path)
    for settings, result in zip(cases, drawn):
        expected = split_page(render(settings))[1]
        assert same(result["html"]) == same(expected), f"{name} with {settings}"


def test_typed_values_are_drawn(tmp_path):
    config = load_config()
    project = _project(config)
    path = feature("uiTestBench.fs")
    data, conditions = dialog_data(project, config.std_dir, path, None)
    [result] = run_script(page_script(data, conditions), [{"length": "1 <in>"}], tmp_path)
    expected = split_page(render_feature(project, config.std_dir, path, None, {"length": "1 <in>"})[0])[1]
    assert same(result["html"]) == same(expected)
    assert "1 &lt;in&gt;" in result["html"]


def test_set_by_parameters_follow_what_their_functions_read(tmp_path):
    # Robot frame's hidden No top holes is set by editing logic from its "Set By" function, isNoTopHolesFrame (and No side
    # holes likewise): a tube with rows of holes on a face shows their distance, and hides it without, and with neither,
    # the hole parameters too, in fs ui and on the page alike
    config = load_config()
    project = _project(config)
    path = feature("robotFrame.fs")
    custom = {"program": "FRC", "source": "CUSTOM"}
    cases = [
        {**custom, "customFrame": "Tube > 2x1"},
        {**custom, "customFrame": "Tube > 1x1 > 0 > 0"},
        {**custom, "customFrame": "Tube > 2x1 > 0 > 1"},
        {**custom, "customFrame": "Tube > Custom", "topHoles": "false"},
        {"program": "FTC"},
    ]
    data, conditions = dialog_data(project, config.std_dir, path, None)
    assert data["parameters"]["noTopHoles"]["setBy"]["k"] == "program"
    drawn = run_script(page_script(data, conditions), cases, tmp_path)
    rendered = [split_page(render_feature(project, config.std_dir, path, None, settings)[0])[1] for settings in cases]
    assert [same(result["html"]) for result in drawn] == [same(page) for page in rendered]
    assert "Top row distance" in rendered[0] and "Distance between holes" in rendered[0]
    assert "Top row distance" not in rendered[1] and "Distance between holes" not in rendered[1]
    for page in rendered[2:4]:
        assert "Top row distance" not in page and "Side row distance" in page and "Distance between holes" in page
