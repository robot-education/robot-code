"""Tests of `fs render` (fs_cli/render.py and what it writes: fs_cli/part_mesh.py, raster.py, part_page.py), on the
repo's washers, the quickest part set to build. Skipped without OpenCascade (`uv sync --group render`)."""

import json
import math
import pathlib
import struct

import pytest

pytest.importorskip("OCP", reason="building parts needs OpenCascade: uv sync --group render")

from fs_cli.cli import main  # noqa: E402
from fs_cli.config import load_config  # noqa: E402
from fs_cli.render import PartBuilder, RenderError, combinations_for, configuration, part_set_studios  # noqa: E402

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
    # Each configuration panel names its studio, which the viewer shows the configuration it chooses of
    assert 'data-target="fasteners/washer"' in page and "fsShow" in page


def test_a_part_sets_audit_can_build_each_option(in_repo, tmp_path):
    output = tmp_path / "audit.html"
    args = ["audit", "featurescripts/fasteners/fasteners.fs", "--feature", "fastenerWasher", "-o", str(output)]
    assert main(args) == 0
    defaults = output.read_text(encoding="utf-8").count("&quot;key&quot;:")
    assert main(args + ["--each-option"]) == 0
    assert output.read_text(encoding="utf-8").count("&quot;key&quot;:") > defaults


def test_each_varies_the_studios_with_that_input(in_repo, tmp_path, capsys):
    # Washers have no head: each renders its default, and every screw head
    args = ["render", "fasteners/washer", "fasteners/screw", "--each", "head", "-o", str(tmp_path), "--views", "", "--no-glb", "--no-page"]
    assert main(args) == 0
    names = sorted(p.name for p in tmp_path.glob("*.json"))
    assert len([n for n in names if n.startswith("fasteners-washer-")]) == 1
    assert len([n for n in names if n.startswith("fasteners-screw-")]) >= 2
    capsys.readouterr()
    # An input none of them has is a usage error, as is a choice
    assert main(["render", "fasteners/washer", "fasteners/screw", "--each", "colour"]) == 2
    assert "None of fasteners/washer, fasteners/screw has an input colour" in capsys.readouterr().err
    assert main(["render", "fasteners/washer", "--set", "colour=RED"]) == 2
    assert "fasteners/washer has no input colour" in capsys.readouterr().err


def test_step_files_are_named_colored_and_in_millimeters(in_repo, tmp_path):
    from OCP.Bnd import Bnd_Box
    from OCP.BRepBndLib import BRepBndLib
    from OCP.STEPControl import STEPControl_Reader

    assert main(["render", "fasteners/nut", "--set", "size=QUARTER_20", "--set", "type=NYLOCK", "--step", "-o", str(tmp_path),
                 "--views", "", "--no-glb", "--no-page"]) == 0
    [step] = tmp_path.glob("*.step")
    text = step.read_text(encoding="utf-8")
    assert "PRODUCT('1/4-20 Nylock Nut'" in text
    assert "SI_UNIT(.MILLI.,.METRE.)" in text
    # The nut's color, and its insert's (white, which STEP names)
    assert "COLOUR_RGB" in text and "DRAUGHTING_PRE_DEFINED_COLOUR('white')" in text
    reader = STEPControl_Reader()
    reader.ReadFile(str(step))
    reader.TransferRoots()
    box = Bnd_Box()
    BRepBndLib.Add_s(reader.OneShape(), box)
    # 7/16 in across flats, so across its corners, read back in millimeters
    assert box.CornerMax().X() - box.CornerMin().X() == pytest.approx(7 / 16 * 25.4 / math.cos(math.radians(30)), abs=0.05)


def test_all_writes_a_catalog_of_every_configuration(in_repo, tmp_path):
    assert main(["render", "fasteners/washer", "--set", "vendor=MCMASTER", "--all", "-o", str(tmp_path), "--size", "64", "--no-glb"]) == 0
    index = json.loads((tmp_path / "index.json").read_text(encoding="utf-8"))
    assert index["inputs"]["fasteners/washer"][:2] == ["vendor", "size"]
    entries = index["configurations"]
    assert len(entries) > 1 and all(e["configuration"]["vendor"] == "MCMASTER" for e in entries)
    first = entries[0]
    assert first["partSet"] == "fasteners" and first["studio"] == "washer" and first["name"]
    assert first["files"]["png"].startswith("fasteners/washer/") and (tmp_path / first["files"]["png"]).is_file()
    assert not (tmp_path / "index.html").exists()


def test_a_part_builder_builds_what_a_configuration_panel_chooses(in_repo):
    builder = PartBuilder(load_config(ROOT))
    entry = builder.build("fasteners/washer", {"size": "QUARTER_20"})
    assert entry["target"] == "fasteners/washer" and entry["configuration"]["size"] == "QUARTER_20"
    assert entry["metadata"]["parts"][0]["name"].startswith("1/4 in Washer")
    # A choice the panel doesn't offer takes what it shows, as Onshape's does
    assert builder.build("fasteners/washer", {"size": "NOT_A_SIZE"})["configuration"]["size"] != "NOT_A_SIZE"


def test_unknown_targets_are_usage_errors(in_repo, capsys):
    assert main(["render", "gears"]) == 2
    assert "No part set 'gears'" in capsys.readouterr().err
