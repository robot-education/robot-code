"""Tests for fs_cli/frontend.py: what a release keeps in the frontend document for FRCDesignApp."""

from fs_cli.frontend import (
    FrontendTab,
    feature_description,
    find_example,
    tabs_in_order,
)

FS, PS = "FEATURESTUDIO", "PARTSTUDIO"


def tab(element_id, element_type, name=None, folders=(), placed=True):
    return FrontendTab(element_id, name or element_id, element_type, folders, placed)


def example(tabs, studio, name):
    found = find_example(tabs, studio, name)
    return found.element_id if found else None


def test_takes_the_part_studio_named_for_the_feature_in_any_case():
    tabs = [tab("extrude", FS), tab("other", PS), tab("ex", PS, "multi extrude")]
    assert example(tabs, "extrude", "Multi Extrude") == "ex"


def test_shares_the_example_after_several_features_in_their_folder():
    folder = ("Basic operations",)
    tabs = [
        tab("extrude", FS, folders=folder),
        tab("revolve", FS, folders=folder),
        tab("ex", PS, "Multi extrude", folder),
    ]
    assert example(tabs, "extrude", "Multi extrude") == "ex"
    assert example(tabs, "revolve", "Multi revolve") == "ex"


def test_prefers_a_named_part_studio_after_the_feature_in_its_folder():
    folder = ("Basic operations",)
    tabs = [tab("early", PS, "Multi extrude"), tab("extrude", FS, folders=folder), tab("late", PS, "Multi extrude", folder)]
    assert example(tabs, "extrude", "Multi extrude") == "late"


def test_looks_only_after_the_feature_and_only_in_its_own_folder():
    folder = ("Basic operations",)
    tabs = [
        tab("before", PS, folders=folder),
        tab("extrude", FS, folders=folder),
        tab("nested", PS, folders=folder + ("Inner",)),
    ]
    assert example(tabs, "extrude", "Multi extrude") is None


def test_skips_deprecated_part_studios():
    tabs = [tab("extrude", FS), tab("old", PS, "DEPRECATED Multi extrude"), tab("gone", PS, folders=("Deprecated",))]
    assert example(tabs, "extrude", "Multi extrude") is None


def test_an_unplaced_studio_finds_its_example_by_name_alone():
    tabs = [tab("next", PS), tab("ex", PS, "Multi extrude"), tab("extrude", FS, placed=False)]
    assert example(tabs, "extrude", "Multi extrude") == "ex"


def test_tabs_in_order_follows_the_folder_tree_and_leaves_out_none():
    elements = [
        {"id": "a", "name": "A", "elementType": FS},
        {"id": "b", "name": "B", "elementType": PS},
        {"id": "c", "name": "C", "elementType": PS},
    ]
    root = {"groups": [{"groupName": "Parts", "groups": [{"elementId": "b"}]}, {"elementId": "a"}]}
    assert [(t.element_id, t.folders, t.placed) for t in tabs_in_order(elements, root)] == [
        ("b", ("Parts",), True),
        ("a", (), True),
        ("c", (), False),
    ]
    assert [t.placed for t in tabs_in_order(elements, None)] == [False, False, False]


def test_the_description_is_its_literal_without_the_credit():
    code = '"Feature Type Description" : "Add \\"tube\\".<br><br>Or channel." ~ CREDIT,'
    assert feature_description(code) == 'Add "tube".\n\nOr channel.'
