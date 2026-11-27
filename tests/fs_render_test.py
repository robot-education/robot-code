"""Tests of `fs render` (fs_cli/render.py and what it writes: fs_cli/part_mesh.py, raster.py, part_page.py), on the
repo's washers, the quickest part set to build. Skipped without OpenCascade (`uv sync --group render`)."""

import json
import pathlib
import struct

import pytest

pytest.importorskip("OCP", reason="building parts needs OpenCascade: uv sync --group render")

from fs_cli.cli import main  # noqa: E402
from fs_cli.config import load_config  # noqa: E402
from fs_cli.render import RenderError, combinations_for, configuration, part_set_studios  # noqa: E402

ROOT = pathlib.Path(__file__).parents[1]


@pytest.fixture
def in_repo(monkeypatch):
    monkeypatch.chdir(ROOT)


def test_render_writes_images_models_metadata_and_a_page(in_repo, tmp_path, capsys):
    assert main(["render", "fasteners/washer", "--set", "size=QUARTER_20", "-o", str(tmp_path), "--views", "iso,top", "--size", "120"]) == 0
    [png] = tmp_path.glob("*.png")
    assert png.read_bytes()[:8] == b"\x89PNG\r\n\x1a\n"
    # Two views side by side
    width, height = struct.unpack(">II", png.read_bytes()[16:24])
    assert (width, height) == (246, 120)
    [glb] = tmp_path.glob("*.glb")
    magic, version, length = struct.unpack("<III", glb.read_bytes()[:12])
    assert (magic, version, length) == (0x46546C67, 2, glb.stat().st_size)
    [info] = [json.loads(p.read_text(encoding="utf-8")) for p in tmp_path.glob("*.json")]
    [part] = info["parts"]
    assert part["name"].startswith("1/4 in Washer")
    assert part["mass"] > 0 and part["material"]
    assert [m["id"] for m in info["mateConnectors"]] == ["mateConnector", "topMateConnector"]
    page = (tmp_path / "index.html").read_text(encoding="utf-8")
    assert page.startswith("<!doctype html>") and 'id="parts-data"' in page
    assert "1/4 in Washer" in page
    assert "Wrote 1 configuration" in capsys.readouterr().out


def test_render_as_an_artifact_puts_its_data_beside_the_page(in_repo, tmp_path):
    assert main(["render", "fasteners/washer", "-o", str(tmp_path), "--views", "", "--no-glb", "--artifact"]) == 0
    page = (tmp_path / "index.html").read_text(encoding="utf-8")
    assert page.startswith("<title>") and "<html" not in page and 'id="parts-data"' not in page
    data = json.loads((tmp_path / "parts.json").read_text(encoding="utf-8"))
    [entry] = data["parts"]
    assert entry["mesh"]["primitives"] and entry["mesh"]["edges"]
    assert not list(tmp_path.glob("*.png")) and not list(tmp_path.glob("*.glb"))


def test_each_renders_every_option_of_an_input(in_repo):
    [ref] = [r for r in part_set_studios(load_config(ROOT)) if r.label == "fasteners/screw"]
    found = combinations_for(ref, {"size": "QUARTER_20", "length": "IN_0_5"}, ["head"])
    heads = [c["head"] for c in found]
    assert len(heads) == len(set(heads)) >= 2
    assert all(c["size"] == "QUARTER_20" and c["length"] == "IN_0_5" for c in found)


def test_choices_are_checked_against_what_the_configuration_offers(in_repo):
    [ref] = [r for r in part_set_studios(load_config(ROOT)) if r.label == "fasteners/washer"]
    with pytest.raises(RenderError, match="isn't offered"):
        configuration(ref.studio, {"size": "NOT_A_SIZE"})
    with pytest.raises(RenderError, match="has no input"):
        configuration(ref.studio, {"colour": "RED"})


def test_check_all_builds_every_configuration(in_repo, capsys):
    assert main(["render", "fasteners/washer", "--check-all"]) == 0
    assert "fasteners/washer: 45 configurations, 0 failed" in capsys.readouterr().out


def test_a_part_sets_audit_shows_its_parts(in_repo, tmp_path):
    output = tmp_path / "audit.html"
    assert main(["audit", "featurescripts/fasteners/fasteners.fs", "--feature", "fastenerWasher", "-o", str(output)]) == 0
    page = output.read_text(encoding="utf-8")
    assert 'class="parts-frame"' in page and "Washer" in page


def test_unknown_targets_are_usage_errors(in_repo, capsys):
    assert main(["render", "gears"]) == 2
    assert "No part set 'gears'" in capsys.readouterr().err
