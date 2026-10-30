"""Robot frame's tables, as the feature reads them: custom frames' rows of holes (on their tops, sides, both, or
neither), and frames' materials."""

import pytest

from fs_eval import to_python
from fs_eval.pytest_plugin import shared_evaluator
from fs_eval.values import untag

FRAME = "released/frame/robotFrame.fs"


def evaluate(expression: str, module: str, **variables):
    return shared_evaluator().eval(expression, module, **variables)


def leaf_paths(node, path=None):
    node = untag(node)
    for key, child in untag(node.get_str("entries")).sorted_items():
        child_path = {**(path or {}), node.get_str("name"): key}
        child = untag(child)
        if type(child).__name__ == "FSMap" and child.get_str("entries") is not None and child.get_str("name") is not None:
            yield from leaf_paths(child, child_path)
        else:
            yield child_path


def cots_frames():
    """Every COTS frame with holes, as Robot frame builds it (its holes where they're sold), unflipped."""
    frames = []
    for program, table in (("FRC", "frcFrameTable"), ("FTC", "ftcFrameTable")):
        for path in leaf_paths(evaluate(table, FRAME)):
            definition = evaluate(
                f'{{ "program" : Program.{program}, "source" : FrameSource.COTS, "{program.lower()}Frame" : path as LookupTablePath, "flip" : false }}',
                FRAME,
                path=path,
            )
            entry = evaluate("frameTableEntry(definition)", FRAME, definition=definition)
            definition = evaluate(
                'mergeMaps(definition, { "holeStart" : entry.tieStart, "holeDiameter" : entry.holeDiameter })',
                FRAME,
                definition=definition,
                entry=entry,
            )
            stock = evaluate("getFrame(definition)", FRAME, definition=definition)
            if to_python(untag(stock))["xRows"] or to_python(untag(stock))["yRows"]:
                frames.append(stock)
    return frames


def custom_tube(size: str, top: str, side: str, profile: str = "Tube"):
    return evaluate(
        f'{{ "program" : Program.FRC, "source" : FrameSource.CUSTOM, "customFrame" : {{ "profile" : "{profile}", "size" : "{size}", "topRows" : "{top}", "sideRows" : "{side}" }} as LookupTablePath, '
        '"wallThickness" : 0.0625 * inch, "holeSpacing" : 0.5 * inch, "holeStart" : 0.5 * inch, "holeDiameter" : 0.196 * inch, '
        '"tableTopRowSpacing" : 0.5 * inch, "tableSideRowSpacing" : 0.5 * inch }',
        FRAME,
    )


def no_holes(definition) -> tuple[bool, bool]:
    """Its hidden No top holes and No side holes, as editing logic sets them."""
    return (
        evaluate("isNoTopHolesFrame(definition)", FRAME, definition=definition),
        evaluate("isNoSideHolesFrame(definition)", FRAME, definition=definition),
    )


@pytest.mark.parametrize("size", ["Tube > 1x1", "Tube > 2x1", "Tube > 2x2", "Angle > 1x1", "Angle > 2x2"])
def test_custom_frames_can_have_holes_on_their_top_sides_both_or_neither(size):
    profile, size = size.split(" > ")
    for top, side in (("0", "0"), ("0", "1"), ("1", "0"), ("1", "1")):
        definition = custom_tube(size, top, side, profile)
        assert no_holes(definition) == (top == "0", side == "0")
        stock = to_python(untag(evaluate("getFrame(definition)", FRAME, definition=definition)))
        assert (len(stock["yRows"]), len(stock["xRows"])) == (int(top), int(side))


def test_the_custom_size_adds_holes_on_its_top_and_sides():
    for top, side in ((False, False), (False, True), (True, False), (True, True)):
        definition = evaluate(
            '{ "program" : Program.FRC, "source" : FrameSource.CUSTOM, "customFrame" : { "profile" : "Tube", "size" : "Custom" } as LookupTablePath, '
            '"width" : 2 * inch, "height" : 1 * inch, "topHoles" : top, "sideHoles" : side, "topRows" : 2, "sideRows" : 1, '
            '"topRowSpacing" : 0.5 * inch, "sideRowSpacing" : 0.5 * inch, "wallThickness" : 0.0625 * inch, '
            '"holeSpacing" : 0.5 * inch, "holeStart" : 0.5 * inch, "holeDiameter" : 0.196 * inch }',
            FRAME,
            top=top,
            side=side,
        )
        assert no_holes(definition) == (not top, not side)
        stock = to_python(untag(evaluate("getFrame(definition)", FRAME, definition=definition)))
        assert (len(stock["yRows"]), len(stock["xRows"])) == (1 if top else 0, 1 if side else 0)
        assert [len(row["shapes"]) for row in stock["yRows"] + stock["xRows"]] == [2] * top + [1] * side


def test_frames_are_their_vendors_material():
    """Last Anvil's tube is 7075; the rest are Aluminum - 6061 (`setStockProperties`'s default)."""
    materials = {
        to_python(untag(stock))["partName"]: to_python(untag(stock)).get("material") for stock in cots_frames()
    }
    assert materials.pop("2x1 Tube (Last Anvil, 1/16 in. wall)")["name"] == "Aluminum - 7075"
    assert set(materials.values()) == {None}
