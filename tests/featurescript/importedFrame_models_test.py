"""Robot imported frame's models (see fs_cli/stock_models.py): every COTS frame has one, which exists."""

import pathlib

from fs_cli.stock_models import MODELS_FOLDER, frames, model_file_name
from fs_eval import to_python
from fs_eval.pytest_plugin import shared_evaluator

CODE = pathlib.Path(__file__).parents[2] / "featurescripts"
MODULE = "importedFrame/frameModels.gen.fs"


def test_every_cots_frame_has_a_model():
    evaluator = shared_evaluator()
    names = set(to_python(evaluator.eval("keys(FRAME_MODELS)", MODULE)))
    for frame in frames(evaluator):
        assert frame.part_name in names, f"{frame.part_name} has no model; run `fs gen`"


def test_every_model_exists():
    source = (CODE / MODULE).read_text()
    for line in source.splitlines():
        if "::import(" in line:
            path = line.rsplit("// ", 1)[1]
            assert (CODE / path).is_file(), f"{path} doesn't exist; run `fs models`"
            assert path.startswith(MODELS_FOLDER + "/")


def test_model_file_names_are_part_names_words():
    assert model_file_name("2x1 Tube (WCP, 1/16 in. wall)") == "2x1-tube-wcp-1-16-in-wall.gen.step"
