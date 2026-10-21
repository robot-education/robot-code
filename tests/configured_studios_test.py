import pytest

from fs_cli.configured_studios import ConfigureError, ConfiguredStudio, Input, Option, configure, find_studio
from onshape_api.paths.paths import ElementPath


def studio() -> ConfiguredStudio:
    return ConfiguredStudio(
        "screw",
        "fastenerScrew",
        [
            Input("size", "Size", "size", [Option("A", "A", "A"), Option("B", "B", "B")], enum_name="Size"),
            Input(
                "length",
                "Length",
                "length",
                [
                    Option("L1", "1 in.", "1 in"),
                    Option("L2", "2 in.", "2 in", [{"size": "B"}]),
                    Option("L3", "3 in.", "3 in", [{"size": "A"}, {"size": "B"}]),
                ],
                kind="length",
            ),
            Input("washer", "Washer", "washer", kind="boolean"),
        ],
    )


def test_inputs_are_checked():
    with pytest.raises(ValueError, match="duplicate"):
        Input("size", "Size", "size", [Option("A", "A", "A"), Option("A", "A", "A")], enum_name="Size")
    with pytest.raises(ValueError, match="identifier"):
        Input("size", "Size", "size", [Option("1/4", "1/4", "A")], enum_name="Size")
    with pytest.raises(ValueError, match="enum_name"):
        Input("size", "Size", "size", [Option("A", "A", "A")])
    with pytest.raises(ValueError, match="checkbox"):
        Input("washer", "Washer", "washer", [Option("A", "A", "A")], kind="boolean")


def test_options_show_only_with_earlier_inputs():
    size = Input("size", "Size", "size", [Option("A", "A", "A")], enum_name="Size")
    with pytest.raises(ValueError, match="isn't before it"):
        ConfiguredStudio("s", "f", [Input("length", "Length", "length", [Option("L", "L", "1 in", [{"size": "A"}])], kind="length"), size])
    with pytest.raises(ValueError, match="unknown size=B"):
        ConfiguredStudio("s", "f", [size, Input("length", "Length", "length", [Option("L", "L", "1 in", [{"size": "B"}])], kind="length")])


def test_a_default_must_show_with_its_parents_default():
    with pytest.raises(ValueError, match="default"):
        ConfiguredStudio(
            "screw",
            "f",
            [
                Input("size", "Size", "size", [Option("A", "A", "A"), Option("B", "B", "B")], enum_name="Size"),
                Input("length", "Length", "length", [Option("L", "L", "1 in", [{"size": "B"}])], kind="length"),
            ],
        )


def test_each_option_is_controlled_by_one_condition():
    length = studio().configuration_body()["configurationParameters"][1]
    assert studio().configuration_body()["configurationParameters"][2] == {
        "btType": "BTMConfigurationParameterBoolean-2550",
        "parameterId": "washer",
        "parameterName": "Washer",
        "defaultValue": False,
    }
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


def test_combinations_of_several_inputs_are_ands_ored():
    from fs_cli.configured_studios import _condition

    condition = _condition([{"head": "X", "size": "A"}, {"head": "Y", "size": "B"}], ["size", "head"])
    assert condition["operation"] == "OR"
    assert [child["operation"] for child in condition["children"]] == ["AND", "AND"]
    assert condition["children"][0]["children"] == [
        {"btType": "BTParameterVisibilityOnEqual-180", "parameterId": "size", "value": "A"},
        {"btType": "BTParameterVisibilityOnEqual-180", "parameterId": "head", "value": "X"},
    ]


def test_conditions_are_factored_by_input_order():
    from fs_cli.configured_studios import _condition

    # Sizes A and B both take heads X and Y, so they share one branch
    combinations = [{"size": size, "head": head} for size in "AB" for head in "XY"] + [{"size": "C", "head": "X"}]
    condition = _condition(combinations, ["size", "head"])

    def equal(id, value):
        return {"btType": "BTParameterVisibilityOnEqual-180", "parameterId": id, "value": value}

    def logical(operation, *children):
        return {"btType": "BTParameterVisibilityLogical-178", "operation": operation, "children": list(children)}

    assert condition == logical(
        "OR",
        logical("AND", logical("OR", equal("size", "A"), equal("size", "B")), logical("OR", equal("head", "X"), equal("head", "Y"))),
        logical("AND", equal("size", "C"), equal("head", "X")),
    )


def test_factored_conditions_show_exactly_the_combinations():
    from fs_cli.configured_studios import _condition

    def holds(condition, selection):
        if condition["btType"] == "BTParameterVisibilityOnEqual-180":
            return selection[condition["parameterId"]] == condition["value"]
        results = [holds(child, selection) for child in condition["children"]]
        return all(results) if condition["operation"] == "AND" else any(results)

    combinations = [{"v": "1", "s": "A", "h": "X"}, {"v": "1", "s": "B", "h": "X"}, {"v": "2", "s": "A", "h": "Y"}]
    condition = _condition(combinations, ["v", "s", "h"])
    for v in "12":
        for s in "AB":
            for h in "XY":
                selection = {"v": v, "s": s, "h": h}
                assert holds(condition, selection) == (selection in combinations)


def test_configured_parameters_set_each_option():
    size, length, washer = studio().configured_parameters()
    assert [value["booleanValue"] for value in washer["values"]] == [False, True]
    assert washer["values"][1]["value"] == {"btType": "BTMParameterBoolean-144", "parameterId": "washer", "value": True}
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


def test_combinations_are_what_the_configuration_shows():
    # L1 shows with any size, L2 only with B, L3 with either
    assert studio().combinations() == [
        {"size": "A", "length": "L1", "washer": "false"},
        {"size": "A", "length": "L1", "washer": "true"},
        {"size": "A", "length": "L3", "washer": "false"},
        {"size": "A", "length": "L3", "washer": "true"},
        {"size": "B", "length": "L1", "washer": "false"},
        {"size": "B", "length": "L1", "washer": "true"},
        {"size": "B", "length": "L2", "washer": "false"},
        {"size": "B", "length": "L2", "washer": "true"},
        {"size": "B", "length": "L3", "washer": "false"},
        {"size": "B", "length": "L3", "washer": "true"},
    ]


def test_manifest_holds_the_configuration_onshape_answers():
    manifest = studio().manifest()
    assert manifest["configuration"] == studio().configuration_body()
    assert "inputs" not in manifest


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
        "featureType": "fastenerScrew",
        "parameters": [
            {"btType": "BTMParameterEnum-145", "parameterId": "size", "value": "A"},
            {"btType": "BTMParameterBoolean-144", "parameterId": "other", "value": True},
        ],
    }
    api = FakeApi([{"featureId": "S", "featureType": "newSketch"}, feature])
    configure(api, part_studio(), studio())

    [_, (method, path, configuration), _, (_, feature_path, update)] = api.requests
    assert (method, path) == ("POST", "/elements/d/d/w/w/e/e/configuration")
    assert [parameter["parameterId"] for parameter in configuration["configurationParameters"]] == ["size", "length", "washer"]
    assert feature_path == "/partstudios/d/d/w/w/e/e/features/featureid/F1"
    # The microversion after the configuration changed
    assert update["sourceMicroversion"] == "mv3"
    parameters = update["feature"]["parameters"]
    assert [parameter["parameterId"] for parameter in parameters] == ["size", "other", "length", "washer"]
    assert parameters[0]["btType"] == "BTMParameterConfigured-2222"
    assert parameters[1] == feature["parameters"][1]


def test_configure_needs_exactly_one_feature():
    api = FakeApi([])
    with pytest.raises(ConfigureError, match="0 fastenerScrew"):
        configure(api, part_studio(), studio())
    assert [method for method, _, _ in api.requests] == ["GET"]
