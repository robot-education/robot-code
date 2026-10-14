"""Tests for `fs audit`: a feature's dialog (drawn by the page's script, see live_dialog_test.py), its writeup, and its
problems, on one page."""

import html
import json
import re
import subprocess

import pytest

from fs_cli import cli
from fs_cli.audit import render_markdown
from fs_cli.ui import find_chromium


def test_render_markdown():
    assert render_markdown("| a |\n| --- |\n| `x` |\n") == "<table>\n<thead>\n<tr>\n<th>a</th>\n</tr>\n</thead>\n<tbody>\n<tr>\n<td><code>x</code></td>\n</tr>\n</tbody>\n</table>\n"


def dialog_page(page: str) -> str:
    """The dialog's page, from the audit page's frame."""
    return html.unescape(re.search(r'srcdoc="(.*?)"', page, re.S).group(1))


def dialog_data(page: str) -> dict:
    return json.loads(re.search(r"const DIALOG = (\{.*?\});\n", dialog_page(page), re.S).group(1).replace("<\\/", "</"))


def test_command(tmp_path, capsys):
    output = tmp_path / "lighten.html"
    assert cli.main(["audit", "featurescripts/lighten/robotLighten.fs", "-o", str(output)]) == 0
    assert re.search(r"\d+ parameters, \d+ conditions, \d+ problems\.", capsys.readouterr().out)
    page = output.read_text()
    assert "<h1>Robot lighten</h1>" in page
    # The writeup, rendered
    assert "<h2>Strings</h2>" in page
    # The dialog's parameters, each with its HTML for each value, and its conditions, compiled
    data = dialog_data(page)
    assert set(data["parameters"]["excludeConstruction"]["html"]) == {"true", "false"}
    frame = dialog_page(page)
    assert "const CONDITIONS = [" in frame and "function renderList(" in frame


def test_command_shows_lookup_tables(tmp_path):
    output = tmp_path / "shaft.html"
    assert cli.main(["audit", "featurescripts/released/shaft/robotShaft.fs", "-o", str(output)]) == 0
    page = output.read_text()
    # Every table its parameters use, in the order they're named, with their rows
    tables = re.findall(r"<h3>(\w+Table)</h3>", page)
    assert tables == ["tappedHoleTable", "clearanceHoleTable", "frcShaftTable", "ftcShaftTable"]
    assert "32 tpi (UNF)" in page and "id='table-filter'" in page
    # Options only: no values
    assert "0.1590 in" not in page
    # Its lookup tables are drawn by the page's script, from their levels: in order, with their names
    tree = dialog_data(page)["parameters"]["frcShaft"]["tree"]
    assert tree["name"] == "vendor" and tree["entries"][0][0] == "WCP"


def dump(page: str, script: str, tmp_path) -> str:
    """The dialog's page, with `script` run after it loads, as Chromium leaves it."""
    chromium, _ = find_chromium()
    path = tmp_path / "dialog.html"
    path.write_text(dialog_page(page).replace("</body>", f"<script>{script}</script></body>"))
    return subprocess.run(
        [chromium, "--headless", "--no-sandbox", "--disable-gpu", "--dump-dom", path.as_uri()],
        capture_output=True,
        text=True,
        timeout=120,
    ).stdout


def test_clicks_set_and_unset(tmp_path):
    try:
        find_chromium()
    except Exception:
        pytest.skip("needs Chromium")
    output = tmp_path / "lighten.html"
    assert cli.main(["audit", "featurescripts/lighten/robotLighten.fs", "-o", str(output)]) == 0
    page = output.read_text()
    script = """
    const results = [];
    const checkbox = () => document.querySelector('[data-set="excludeConstruction"] input');
    const record = () => results.push([checkbox().checked, settings.excludeConstruction ?? null]);
    record();
    checkbox().closest("[data-set]").click();
    record();
    // Clicking it again sets it back to its default, which unsets it
    checkbox().closest("[data-set]").click();
    record();
    document.body.dataset.results = JSON.stringify(results);
    """
    dom = dump(page, script, tmp_path)
    results = json.loads(html.unescape(re.search(r'data-results="(.*?)"', dom).group(1)))
    assert results == [[True, None], [False, "false"], [True, None]]
