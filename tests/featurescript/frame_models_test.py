"""Robot frame's models (see fs_cli/stock_models.py): every COTS frame has one, which exists, and which it's built from
when it can be."""

import pathlib

import pytest

from fs_cli.stock_models import MODELS_FOLDER, frames, model_file_name
from fs_eval import to_python
from fs_eval.pytest_plugin import shared_evaluator

CODE = pathlib.Path(__file__).parents[2] / "featurescripts"
MODULE = "released/frame/frameModels.gen.fs"
FRAME = "released/frame/robotFrame.fs"
STOCK = "core/linearStock.fs"
WCP_2X1 = (
    '{ "vendor" : "WCP", "profile" : "Tube", "size" : "2x1", "pattern" : "Full", "wall" : "1/16 in." } as LookupTablePath'
)


def wcp_2x1(hole_start: float, hole_diameter: float = 0.196):
    """WCP's 2x1 tube as Robot frame's `getFrame` returns it."""
    definition = (
        f'{{ "program" : Program.FRC, "source" : FrameSource.COTS, "frcFrame" : {WCP_2X1}, '
        f'"holeStart" : {hole_start} * inch, "holeDiameter" : {hole_diameter} * inch }}'
    )
    return shared_evaluator().eval(f"getFrame({definition})", FRAME)


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


def test_cots_frames_have_their_model_moved_with_their_holes():
    stock = wcp_2x1(0.75)
    assert to_python(shared_evaluator().eval("stock.model.shift", FRAME, stock=stock)) == pytest.approx(0.25 * 0.0254)
    assert to_python(shared_evaluator().eval("stock.model.length", FRAME, stock=stock)) == pytest.approx(47 * 0.0254)
    assert to_python(shared_evaluator().eval("stock.xRows[0].start", FRAME, stock=stock)) == pytest.approx(0.75 * 0.0254)


def test_frames_with_bigger_holes_are_built():
    assert to_python(shared_evaluator().eval("stock.model", FRAME, stock=wcp_2x1(0.5, 0.25))) is None


@pytest.mark.parametrize(
    "hole_start,length,fits",
    [(0.5, 24, True), (0.5, 47, True), (0.5, 47.1, False), (2.5, 24, True), (2.6, 24, False), (0, 46.5, True)],
)
def test_frames_are_built_from_their_model_when_they_fit_it(hole_start, length, fits):
    model = shared_evaluator().eval("stock.model", FRAME, stock=wcp_2x1(hole_start))
    assert shared_evaluator().eval(f"fitsModel(model, {length} * inch)", STOCK, model=model) is fits
