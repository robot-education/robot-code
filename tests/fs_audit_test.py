"""Tests for `fs audit`: a feature's dialog's states, pre-rendered, its writeup, and its problems, on one page."""

import html
import json
import re

from fs_cli import cli
from fs_cli.audit import audit_page, explore_dialog, render_markdown


def fake_render(settings: dict[str, str]) -> str:
    """A dialog with a Mode dropdown (A or B), whose B shows an Extra checkbox."""
    mode = settings.get("mode", "A")
    parameters = (
        "<os-parameter-group><div data-parameter-id='mode'>"
        f"<div data-set='mode' data-value='A'>A</div><div data-set='mode' data-value='B'>B</div> {mode}</div></os-parameter-group>"
    )
    if mode == "B":
        extra = settings.get("extra", "false")
        other = "false" if extra == "true" else "true"
        parameters += f"<os-parameter-group><input data-parameter-id='extra' data-set='extra' data-value='{other}'> {extra}</os-parameter-group>"
    return f"<html><body><os-parameter-list-view>{parameters}</os-parameter-list-view></body></html>"


def test_explore_dialog():
    shell, states, truncated = explore_dialog(fake_render, 10)
    assert not truncated
    assert shell == "<html><body><os-parameter-list-view>\0</os-parameter-list-view></body></html>"
    assert [state["settings"] for state in states] == [{}, {"mode": "B"}, {"mode": "B", "extra": "true"}]
    # Choosing what's already chosen stays put; each choice leads where it should
    assert states[0]["next"] == {"mode=A": 0, "mode=B": 1}
    assert states[1]["next"] == {"mode=A": 0, "mode=B": 1, "extra=true": 2}
    assert states[2]["next"]["extra=false"] == 1


def test_explore_dialog_stops_at_its_limit():
    _, states, truncated = explore_dialog(fake_render, 2)
    assert truncated and len(states) == 2


def test_audit_page_shares_parameter_groups():
    shell, states, truncated = explore_dialog(fake_render, 10)
    page = audit_page("Fake", "fake.fs", shell, states, truncated, "# Fake\n\n| a | b |\n| --- | --- |\n| 1 | 2 |\n", ["1:1: warning: x [y]"], "dark")
    frame = html.unescape(re.search(r'srcdoc="(.*?)"', page, re.S).group(1))
    data = json.loads(re.search(r"id='fs-states'>(.*?)</script>", frame, re.S).group(1).replace("<\\/", "</"))
    # Mode's group is stored once, though every state shows it
    assert len(data["pieces"]) == 4
    assert [state["settings"] for state in data["states"]] == [[], ["mode=B"], ["extra=true", "mode=B"]]
    assert "<td>1</td>" in page and "1:1: warning: x [y]" in page


def test_render_markdown():
    assert render_markdown("| a |\n| --- |\n| `x` |\n") == "<table>\n<thead>\n<tr>\n<th>a</th>\n</tr>\n</thead>\n<tbody>\n<tr>\n<td><code>x</code></td>\n</tr>\n</tbody>\n</table>\n"


def test_command(tmp_path, capsys):
    output = tmp_path / "lighten.html"
    assert cli.main(["audit", "featurescripts/lighten/robotLighten.fs", "-o", str(output), "--max-states", "15"]) == 0
    assert "15 dialog states (the limit" in capsys.readouterr().out
    page = output.read_text()
    assert "<h1>Robot lighten</h1>" in page
    # The writeup, rendered
    assert "<h2>Strings</h2>" in page


def test_command_shows_lookup_tables(tmp_path, capsys):
    output = tmp_path / "shaft.html"
    assert cli.main(["audit", "featurescripts/released/shaft/robotShaft.fs", "-o", str(output), "--max-states", "3"]) == 0
    page = output.read_text()
    # Every table its parameters use, in the order they're named, with their rows
    tables = re.findall(r"<h3>(\w+Table)</h3>", page)
    assert tables == ["tappedHoleTable", "clearanceHoleTable", "frcShaftTable", "ftcShaftTable"]
    assert "32 tpi (UNF)" in page and "id='table-filter'" in page
