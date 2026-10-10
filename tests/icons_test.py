"""Tests for drawing icons (fs_cli.icons) and generating them from definitions."""

import re

from fs_cli.gen import generate
from fs_cli.icons import (
    FACE,
    OUTLINE,
    SIDE,
    WHITE,
    Icon,
    circle,
    path,
    preview_page,
    round_corners,
    rounded_rectangle,
    sweep,
)


def test_path_is_compact():
    data = path(rounded_rectangle(2, 2, 18, 18, 0))
    assert data.startswith("M") and data.endswith("Z")
    # Whole numbers lose their decimals, and a square has just its corners
    assert re.fullmatch(r"M[\d. L]+Z", data) and data.count("L") == 3 and ".00" not in data


def test_sweep_goes_back_up_and_to_the_left():
    swept = sweep(rounded_rectangle(4, 4, 10, 10, 0), 2)
    assert swept.bounds == (2, 2, 10, 10)


def test_round_corners_rounds_only_convex_corners():
    square = rounded_rectangle(0, 0, 10, 10, 0)
    notched = square.difference(rounded_rectangle(5, 5, 10, 10, 0))
    rounded = round_corners(notched, 1)
    # The outside corners are rounded off, the inside one isn't
    assert rounded.area < notched.area
    assert rounded.covers(rounded_rectangle(4, 4, 5, 5, 0))


def test_icon_draws_in_onshapes_colors():
    icon = Icon().extrusion(rounded_rectangle(3, 3, 19, 19, 2), 2).through(circle((11, 11), 3))
    svg = icon.render()
    assert svg.startswith('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20">')
    for color in (OUTLINE, FACE, SIDE, WHITE):
        assert f'fill="{color}"' in svg
    # A cut's wall and opening are clipped to it
    assert 'clip-path="url(#c1)"' in svg


def test_icon_definitions_generate_svgs(tmp_path):
    (tmp_path / "icon.py").write_text(
        "from fs_cli.icons import Icon, circle, WHITE\nICON = Icon().fill(circle((10, 10), 5), WHITE)\n"
    )
    [result] = generate(tmp_path, None)
    assert result.output == tmp_path / "icon.svg"
    assert result.code.startswith("<svg") and result.changed
    result.output.write_text(result.code)
    [result] = generate(tmp_path, None)
    assert not result.changed


def test_preview_page_shows_each_icon_big_and_small(tmp_path):
    icon = tmp_path / "a.svg"
    icon.write_text(Icon().render())
    page = preview_page([icon])
    assert page.count(icon.as_uri()) == 3 and "width:20px" in page
