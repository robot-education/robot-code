import pytest

from fs_cli.configured_studios import ConfigureError, ConfiguredStudio, Input, Option, configure, find_studio
from onshape_api.paths.paths import ElementPath


def studio() -> ConfiguredStudio:
    return ConfiguredStudio(
        "screw",
        "quickFastenerScrew",
        [
            Input("size", "Size", "size", [Option("A", "A", "A"), Option("B", "B", "B")], enum_name="Size"),
            Input(
                "length",
                "Length",
                "length",
                [
                    Option("L1", "1 in.", "1 in"),
                    Option("L2", "2 in.", "2 in", ["B"]),
                    Option("L3", "3 in.", "3 in", ["A", "B"]),
                ],
                kind="length",
                depends_on="size",
            ),
        ],
    )


def test_inputs_are_checked():
    with pytest.raises(ValueError, match="duplicate"):
        Input("size", "Size", "size", [Option("A", "A", "A"), Option("A", "A", "A")], enum_name="Size")
    with pytest.raises(ValueError, match="identifier"):
        Input("size", "Size", "size", [Option("1/4", "1/4", "A")], enum_name="Size")
    with pytest.raises(ValueError, match="enum_name"):
        Input("size", "Size", "size", [Option("A", "A", "A")])
    with pytest.raises(ValueError, match="depends_on"):
        Input("length", "Length", "length", [Option("L", "L", "1 in", ["A"])], kind="length")


def test_a_default_must_show_with_its_parents_default():
    with pytest.raises(ValueError, match="default"):
        ConfiguredStudio(
            "screw",
            "f",
            [
                Input("size", "Size", "size", [Option("A", "A", "A"), Option("B", "B", "B")], enum_name="Size"),
                Input("length", "Length", "length", [Option("L", "L", "1 in", ["B"])], kind="length", depends_on="size"),
            ],
        )


def test_each_option_is_controlled_by_one_condition():
    length = studio().configuration_body()["configurationParameters"][1]
    assert length["enumName"] == "length_conf"
    assert [option["option"] for option in length["options"]] == ["L1", "L2", "L3"]
    conditions = length["enumOptionVisibilityConditions"]["visibilityConditions"]
    assert conditions == [
        {
            "btType": "BTEnumOptionVisibilityForList-1613",
            "controlledOptions": ["L2"],
            "condition": {"btType": "BTParameterVisibilityOnEqual-180", "parameterId": "size", "value": "B"},
        },
        {
            "btType": "BTEnumOptionVisibilityForList-1613",
            "controlledOptions": ["L3"],
            "condition": {
                "btType": "BTParameterVisibilityLogical-178",
                "operation": "OR",
                "children": [
                    {"btType": "BTParameterVisibilityOnEqual-180", "parameterId": "size", "value": "A"},
                    {"btType": "BTParameterVisibilityOnEqual-180", "parameterId": "size", "value": "B"},
                ],
            },
        },
    ]


def test_configured_parameters_set_each_option():
    size, length = studio().configured_parameters()
    assert size["configurationParameterId"] == "size"
    assert size["values"][1]["enumValue"] == "B"
    assert size["values"][1]["value"] == {
        "btType": "BTMParameterEnum-145",
        "parameterId": "size",
        "enumName": "Size",
        "value": "B",
    }
    assert length["values"][0]["value"] == {
        "btType": "BTMParameterQuantity-147",
        "parameterId": "length",
        "expression": "1 in",
    }


def test_manifest_lists_options_and_what_shows_them():
    inputs = studio().manifest()["inputs"]
    assert inputs[1]["dependsOn"] == "size"
    assert inputs[1]["options"][0] == {"id": "L1", "name": "1 in."}
    assert inputs[1]["options"][1] == {"id": "L2", "name": "2 in.", "shownWith": ["B"]}


def test_find_studio_searches_nested_maps():
    found = studio()
    assert find_studio({"STUDIOS": {"frc": {"screw": found}}}, "screw") is found
    with pytest.raises(ConfigureError):
        find_studio({"STUDIOS": {}}, "screw")


class FakeApi:
    def __init__(self, features):
        self.features = features
        self.requests = []

    def get(self, path, **kwargs):
        self.requests.append(("GET", path, None))
        return {"features": self.features, "sourceMicroversion": f"mv{len(self.requests)}"}

    def post(self, path, body="", **kwargs):
        self.requests.append(("POST", path, body))
        return {}


def part_studio() -> ElementPath:
    return ElementPath("d", "w", "e")


def test_configure_writes_the_configuration_then_the_feature():
    feature = {
        "featureId": "F1",
        "featureType": "quickFastenerScrew",
        "parameters": [
            {"btType": "BTMParameterEnum-145", "parameterId": "size", "value": "A"},
            {"btType": "BTMParameterBoolean-144", "parameterId": "other", "value": True},
        ],
    }
    api = FakeApi([{"featureId": "S", "featureType": "newSketch"}, feature])
    configure(api, part_studio(), studio())

    [_, (method, path, configuration), _, (_, feature_path, update)] = api.requests
    assert (method, path) == ("POST", "/elements/d/d/w/w/e/e/configuration")
    assert [parameter["parameterId"] for parameter in configuration["configurationParameters"]] == ["size", "length"]
    assert feature_path == "/partstudios/d/d/w/w/e/e/features/featureid/F1"
    # The microversion after the configuration changed
    assert update["sourceMicroversion"] == "mv3"
    parameters = update["feature"]["parameters"]
    assert [parameter["parameterId"] for parameter in parameters] == ["size", "other", "length"]
    assert parameters[0]["btType"] == "BTMParameterConfigured-2222"
    assert parameters[1] == feature["parameters"][1]


def test_configure_needs_exactly_one_feature():
    api = FakeApi([])
    with pytest.raises(ConfigureError, match="0 quickFastenerScrew"):
        configure(api, part_studio(), studio())
    assert [method for method, _, _ in api.requests] == ["GET"]
