"""Tests for `fs audit`: a feature's dialog (drawn by the page's script, see live_dialog_test.py), its writeup, and its
problems, on one page. They audit the UI test bench, a feature which has every kind of parameter, so that changing
the robot features doesn't change what these see."""

import contextlib
import html
import io
import json
import re
import subprocess

import pytest

from fs_cli import cli
from fs_cli.ui import find_chromium

BENCH = "featurescripts/uiTestBench/uiTestBench.fs"


def dialog_page(page: str) -> str:
    """The dialog's page, from the audit page's frame."""
    return html.unescape(re.search(r'srcdoc="(.*?)"', page, re.S).group(1))


def dialog_data(page: str) -> dict:
    return json.loads(re.search(r"const DIALOG = (\{.*?\});\n", dialog_page(page), re.S).group(1).replace("<\\/", "</"))


@pytest.fixture(scope="module")
def audit(tmp_path_factory) -> tuple[str, str]:
    """What `fs audit` prints for the test bench, and the page it writes."""
    output = tmp_path_factory.mktemp("audit") / "bench.html"
    out = io.StringIO()
    with contextlib.redirect_stdout(out):
        assert cli.main(["audit", BENCH, "-o", str(output)]) == 0
    return out.getvalue(), output.read_text()


def test_command(audit):
    out, page = audit
    assert re.search(r"\d+ parameters, \d+ conditions, \d+ problems\.", out)
    assert "<h1>UI test bench</h1>" in page
    assert "It has no writeup" in page
    # The dialog's parameters, each with its HTML for each value, and its conditions, compiled
    data = dialog_data(page)
    assert set(data["parameters"]["checkbox"]["html"]) == {"true", "false"}
    frame = dialog_page(page)
    assert "const CONDITIONS = [" in frame and "function renderList(" in frame


def test_command_shows_lookup_tables(audit):
    _, page = audit
    # Every table its parameters use, with a row per option
    assert re.findall(r"<h3>(\w+)</h3>", page) == ["TEST_TABLE"]
    assert '<tr data-text="wcp 3/8 in. rounded">' in page and "class='table-filter'" in page
    # Its lookup tables are drawn by the page's script, from their levels: in order, with their names
    tree = dialog_data(page)["parameters"]["lookup"]["tree"]
    assert tree["name"] == "vendor" and tree["entries"][0][0] == "WCP"


def test_open_asks_vs_code_for_the_preview(tmp_path, monkeypatch, capsys):
    request = tmp_path / "open.json"
    monkeypatch.setattr("fs_cli.audit.OPEN_REQUEST", str(request))
    monkeypatch.setenv("TERM_PROGRAM", "vscode")
    assert cli.main(["audit", BENCH, "-o", str(tmp_path / "bench.html"), "--open"]) == 0
    assert "Showed it in VS Code's feature preview" in capsys.readouterr().out
    asked = json.loads(request.read_text())
    assert asked["file"].endswith(BENCH) and asked["feature"] is None

    # Outside VS Code, it says so
    request.unlink()
    monkeypatch.delenv("TERM_PROGRAM")
    monkeypatch.delenv("VSCODE_PID", raising=False)
    assert cli.main(["audit", BENCH, "-o", str(tmp_path / "bench.html"), "--open"]) == 0
    assert "Not running in VS Code" in capsys.readouterr().out
    assert not request.exists()


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


def test_clicks_set_and_unset(audit, tmp_path):
    try:
        find_chromium()
    except Exception:
        pytest.skip("needs Chromium")
    script = """
    const results = [];
    const checkbox = () => document.querySelector('[data-set="checkbox"] input');
    const record = () => results.push([checkbox().checked, settings.checkbox ?? null]);
    record();
    checkbox().closest("[data-set]").click();
    record();
    // Clicking it again sets it back to its default, which unsets it
    checkbox().closest("[data-set]").click();
    record();
    document.body.dataset.results = JSON.stringify(results);
    """
    dom = dump(audit[1], script, tmp_path)
    results = json.loads(html.unescape(re.search(r'data-results="(.*?)"', dom).group(1)))
    assert results == [[False, None], [True, "true"], [False, None]]


def test_tapping_a_typed_values_label_focuses_its_field(audit, tmp_path):
    try:
        find_chromium()
    except Exception:
        pytest.skip("needs Chromium")
    script = """
    const item = document.querySelector('.os-param-container[data-parameter-id="count"]');
    item.querySelector("label").click();
    document.body.dataset.results = JSON.stringify(document.activeElement.dataset.set || null);
    """
    dom = dump(audit[1], script, tmp_path)
    assert json.loads(html.unescape(re.search(r'data-results="(.*?)"', dom).group(1))) == "count"
