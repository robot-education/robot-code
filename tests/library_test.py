"""Tests for fs_cli/library.py: FRCDesignLib's parts as definitions read them."""

from fs_cli.library import records

PART = {
    "name": "Flanged Bushing",
    "documentId": "doc",
    "elementId": "tab",
    "elementType": "PARTSTUDIO",
    "parameters": [
        {
            "id": "List_vendor",
            "name": "Vendor",
            "type": "enum",
            "default": "Default",
            "options": [{"id": "Default", "name": "Generic"}, {"id": "WCP", "name": "West Coast Products"}],
        },
        {"id": "Flag", "name": "Simplified Model", "type": "boolean", "default": True, "options": []},
    ],
    "records": [
        {"values": {}, "name": "Generic bushing"},
        {"values": {"List_vendor": "WCP"}, "name": "WCP bushing", "partNumber": "WCP-0999"},
    ],
}


def test_records_name_every_enum_option_by_its_name():
    assert records(PART) == [
        {"name": "Generic bushing", "partNumber": None, "options": {"Vendor": "Generic"}},
        {"name": "WCP bushing", "partNumber": "WCP-0999", "options": {"Vendor": "West Coast Products"}},
    ]
