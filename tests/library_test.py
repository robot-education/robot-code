"""Tests for fs_cli/library.py: FRCDesignLib's parts as features' tables declare them."""

import pytest

from fs_cli.library import LibraryError, product

PARTS = {
    "NEO Brushless Motor": {
        "name": "NEO Brushless Motor",
        "documentId": "doc",
        "elementId": "tab",
        "elementType": "PARTSTUDIO",
        "parameters": [
            {
                "id": "List_version",
                "name": "Version",
                "type": "enum",
                "default": "V1_1",
                "options": [{"id": "V1_0", "name": "V1.0"}, {"id": "V1_1", "name": "V1.1"}],
            }
        ],
    }
}


def test_a_product_names_its_options_by_id_and_has_no_version():
    assert product(PARTS, "NEO Brushless Motor", {"Version": "V1.0"}) == (
        '{ "documentId" : "doc", "elementId" : "tab", "configuration" : { "List_version" : "V1_0" }, '
        '"name" : "NEO Brushless Motor" }'
    )
    assert product(PARTS, "NEO Brushless Motor", label="NEO") == (
        '{ "documentId" : "doc", "elementId" : "tab", "configuration" : {}, "name" : "NEO" }'
    )


def test_a_product_the_snapshot_cant_give_fails():
    with pytest.raises(LibraryError, match="has no Kraken"):
        product(PARTS, "Kraken")
    with pytest.raises(LibraryError, match=r"Version has no option V3 \(it offers V1.0, V1.1\)"):
        product(PARTS, "NEO Brushless Motor", {"Version": "V3"})
