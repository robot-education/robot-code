import pytest

from fs_cli.configured_studios import (
    ConfigureError,
    ConfiguredStudio,
    Input,
    Option,
    build,
    configuration_stages,
    diagnose,
    configured_studios,
    read_locations,
    write_locations,
)
from onshape_api.paths.paths import InstancePath


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
                Input(
                    "length", "Length", "length", [Option("L", "L", "1 in", [{"size": "B"}]), Option("M", "M", "2 in")], kind="length"
                ),
            ],
        )


def test_a_list_with_no_option_shown_is_hidden_so_its_default_needs_no_showing():
    hidden = ConfiguredStudio(
        "screw",
        "f",
        [
            Input("size", "Size", "size", [Option("A", "A", "A"), Option("B", "B", "B")], enum_name="Size"),
            Input("thread", "Thread", "thread", [Option("false", "No", "false", [{"size": "B"}]), Option("true", "Yes", "true", [{"size": "B"}])], kind="switch"),
        ],
    )
    assert hidden.combinations() == [{"size": "A"}, {"size": "B", "thread": "false"}, {"size": "B", "thread": "true"}]
    assert hidden.configured_parameters()[1]["values"][1]["value"] == {
        "btType": "BTMParameterBoolean-144",
        "parameterId": "thread",
        "value": True,
    }


def equal(id, value):
    return {"btType": "BTParameterVisibilityOnEqual-180", "parameterId": id, "value": value, "inArray": False}


def logical(operation, *children):
    return {"btType": "BTParameterVisibilityLogical-178", "operation": operation, "children": list(children)}


def test_each_option_rule_is_an_and_of_inputs_each_one_of_some_values():
    length = studio().configuration_body()["configurationParameters"][1]
    assert studio().configuration_body()["configurationParameters"][2] == {
        "btType": "BTMConfigurationParameterBoolean-2550",
        "parameterId": "washer",
        "parameterName": "Washer",
        "defaultValue": False,
    }
    assert length["enumName"] == "length_conf"
    assert [option["option"] for option in length["options"]] == ["L1", "L2", "L3"]
    assert length["enumOptionVisibilityConditions"]["visibilityConditions"] == [
        {
            "btType": "BTEnumOptionVisibilityForList-1613",
            "controlledOptions": ["L2"],
            "condition": logical("AND", logical("OR", equal("size", "B"))),
        },
        {
            "btType": "BTEnumOptionVisibilityForList-1613",
            "controlledOptions": ["L3"],
            "condition": logical("AND", logical("OR", equal("size", "A"), equal("size", "B"))),
        },
        # Onshape hides an option no rule controls, so the rest are shown always, as its UI ends its rules
        {
            "btType": "BTEnumOptionVisibilityForList-1613",
            "controlledOptions": ["L1"],
            "condition": logical("AND", {"btType": "BTParameterVisibilityAlwaysShown-5487"}),
        },
    ]


def test_products_are_factored_by_input_order():
    from fs_cli.configured_studios import _products

    # Sizes A and B both take heads X and Y, so they share a product
    combinations = [{"size": size, "head": head} for size in "AB" for head in "XY"] + [{"size": "C", "head": "X"}]
    assert _products(combinations, ["size", "head"]) == [
        {"size": ["A", "B"], "head": ["X", "Y"]},
        {"size": ["C"], "head": ["X"]},
    ]
    assert _products([{"size": "A"}, {}], ["size"]) == [{}]


def test_products_match_exactly_the_combinations():
    from fs_cli.configured_studios import _products

    combinations = [{"v": "1", "s": "A", "h": "X"}, {"v": "1", "s": "B", "h": "X"}, {"v": "2", "s": "A", "h": "Y"}]
    products = _products(combinations, ["v", "s", "h"])
    for v in "12":
        for s in "AB":
            for h in "XY":
                selection = {"v": v, "s": s, "h": h}
                matched = any(all(selection[id] in values for id, values in product.items()) for product in products)
                assert matched == (selection in combinations)


def test_products_can_be_factored_by_a_later_input_first():
    from fs_cli.configured_studios import _products

    order = ConfiguredStudio("s", "f", finished().inputs, factor_first=["finish"])._factor_order()
    assert order == ["finish", "size"]
    shown = [{"size": size, "finish": "BLACK"} for size in "AB"]
    assert _products(shown, order) == [{"finish": ["BLACK"], "size": ["A", "B"]}]


def test_a_checkbox_shows_by_one_condition():
    size = Input("size", "Size", "size", [Option("A", "A", "A"), Option("B", "B", "B")], enum_name="Size")
    head = Input("head", "Head", "head", [Option("X", "X", "X"), Option("Y", "Y", "Y")], enum_name="Head")
    one = Input("thread", "Thread", "thread", kind="boolean", shown_when=[{"size": "A", "head": head} for head in "XY"])
    parameter = ConfiguredStudio("s", "f", [size, head, one]).configuration_body()["configurationParameters"][2]
    assert parameter["visibilityCondition"] == logical(
        "AND", logical("OR", equal("size", "A")), logical("OR", equal("head", "X"), equal("head", "Y"))
    )
    two = Input("thread", "Thread", "thread", kind="boolean", shown_when=[{"size": "A", "head": "X"}, {"size": "B", "head": "Y"}])
    with pytest.raises(ValueError, match="checkbox thread needs 2 conditions"):
        ConfiguredStudio("s", "f", [size, head, two])


def test_configured_parameters_set_each_option():
    size, length, washer = studio().configured_parameters()
    assert [value["booleanValue"] for value in washer["values"]] == [False, True]
    assert washer["values"][1]["value"] == {"btType": "BTMParameterBoolean-144", "parameterId": "washer", "value": True}
    assert size["configurationParameterId"] == "size"
    assert size["values"][1]["enumValue"] == "B"
    assert size["values"][1]["value"] == {
        "btType": "BTMParameterEnum-145",
        "parameterId": "size",
        "namespace": "",
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


def finished() -> ConfiguredStudio:
    return ConfiguredStudio(
        "screw",
        "f",
        [
            Input("size", "Size", "size", [Option("A", "A", "A"), Option("B", "B", "B")], enum_name="Size"),
            Input(
                "finish",
                "Finish",
                "finish",
                [Option("BLACK", "Black", "BLACK"), Option("ZINC", "Zinc", "ZINC", [{"size": "B"}])],
                enum_name="Finish",
                shown_when=[{"size": "B"}],
            ),
        ],
    )


def test_a_hidden_input_is_left_out_of_combinations():
    assert finished().combinations() == [{"size": "A"}, {"size": "B", "finish": "BLACK"}, {"size": "B", "finish": "ZINC"}]


def test_a_hidden_list_folds_its_visibility_into_its_options():
    finish = finished().configuration_body()["configurationParameters"][1]
    assert "visibilityCondition" not in finish
    assert finish["enumOptionVisibilityConditions"]["visibilityConditions"] == [
        {
            "btType": "BTEnumOptionVisibilityForList-1613",
            "controlledOptions": ["BLACK", "ZINC"],
            "condition": logical("AND", logical("OR", equal("size", "B"))),
        }
    ]


def test_nothing_shows_by_an_input_hidden_somewhere():
    with pytest.raises(ValueError, match="hidden somewhere"):
        ConfiguredStudio(
            "s",
            "f",
            [
                *finished().inputs,
                Input("length", "Length", "length", [Option("L", "L", "1 in", [{"finish": "BLACK"}])], kind="length"),
            ],
        )


def test_manifest_holds_the_configuration_onshape_answers():
    manifest = studio().manifest()
    assert manifest["configuration"] == studio().configuration_body()
    assert "inputs" not in manifest


def test_configured_studios_searches_nested_maps():
    found = studio()
    assert configured_studios({"STUDIOS": {"frc": {"screw": found}}}) == [found]
    assert configured_studios({}) == []


def test_locations_are_recorded_beside_the_definition(tmp_path):
    definition = tmp_path / "partTables.py"
    assert read_locations(definition) == {}
    write_locations(definition, "doc", "ver", {"screw": "e1"})
    assert (tmp_path / "partTables.studios.json").is_file()
    assert read_locations(definition) == {"screw": {"documentId": "doc", "versionId": "ver", "elementId": "e1"}}


class FakeApi:
    """A workspace's Part Studios, by name, each with its features."""

    def __init__(self, tabs: dict[str, list[dict]], status: str = "OK"):
        self.tabs = {f"e{index}": (name, features) for index, (name, features) in enumerate(tabs.items())}
        self.status = status
        self.requests = []

    def get(self, path, **kwargs):
        self.requests.append(("GET", path, None))
        if path.endswith("/elements"):
            return [{"id": id, "name": name} for id, (name, _) in self.tabs.items()]
        element_id = path.split("/e/")[1].split("/")[0]
        return {"features": self.tabs[element_id][1], "sourceMicroversion": f"mv{len(self.requests)}"}

    def post(self, path, body="", **kwargs):
        self.requests.append(("POST", path, body))
        if path == "/partstudios/d/d/w/w":
            self.tabs["new"] = (body["name"], [])
            return {"id": "new"}
        if path.endswith("/configuration"):
            return body
        if path.endswith("/features"):
            element_id = path.split("/e/")[1].split("/")[0]
            self.tabs[element_id][1].append({**body["feature"], "featureId": "F"})
        return {"featureState": {"featureStatus": self.status}}


WORKSPACE = InstancePath("d", "w")


def test_build_creates_the_tab_then_its_configuration_then_inserts_its_feature_and_configures_it():
    api = FakeApi({"Other": []})
    built = build(api, WORKSPACE, studio(), "fs", "fsmv")

    assert (built.element_id, built.created, built.status) == ("new", True, "OK")
    posts = [(path, body) for method, path, body in api.requests if method == "POST"]
    assert [path for path, _ in posts] == [
        "/partstudios/d/d/w/w",
        "/elements/d/d/w/w/e/new/configuration",
        "/partstudios/d/d/w/w/e/new/features",
        "/partstudios/d/d/w/w/e/new/features/featureid/F",
    ]
    assert posts[0][1] == {"name": "Screw"}
    configuration = posts[1][1]
    assert [parameter["parameterId"] for parameter in configuration["configurationParameters"]] == ["size", "length", "washer"]
    inserted = posts[2][1]["feature"]
    assert inserted["featureType"] == "fastenerScrew"
    assert inserted["namespace"] == "efs::mfsmv"
    assert [parameter["btType"] for parameter in inserted["parameters"]] == [
        "BTMParameterEnum-145",
        "BTMParameterQuantity-147",
        "BTMParameterBoolean-144",
    ]
    # Its enums are defined in its FeatureScript, so their values name its namespace too
    assert (inserted["parameters"][0]["enumName"], inserted["parameters"][0]["namespace"]) == ("Size", "efs::mfsmv")
    configured = posts[3][1]["feature"]
    assert [parameter["btType"] for parameter in configured["parameters"]] == ["BTMParameterConfigured-2222"] * 3
    size = configured["parameters"][0]["values"][0]["value"]
    assert (size["enumName"], size["namespace"]) == ("Size", "efs::mfsmv")


def test_build_refuses_a_configuration_onshape_renamed():
    class Renaming(FakeApi):
        def post(self, path, body="", **kwargs):
            if path.endswith("/configuration"):
                return {"configurationParameters": [{"parameterId": "List_abc", "enumName": "List_abc_conf"}]}
            return super().post(path, body, **kwargs)

    with pytest.raises(ConfigureError, match="Onshape stored Screw's configuration inputs as"):
        build(Renaming({}), WORKSPACE, studio(), "fs", "fsmv")


def test_build_points_a_tabs_feature_at_the_feature_studio_and_configures_it():
    feature = {
        "featureId": "F1",
        "featureType": "fastenerScrew",
        "namespace": "efs::mold",
        "parameters": [
            {"btType": "BTMParameterEnum-145", "parameterId": "size", "value": "A"},
            {"btType": "BTMParameterBoolean-144", "parameterId": "other", "value": True},
        ],
    }
    api = FakeApi({"Screw": [{"featureId": "S", "featureType": "newSketch"}, feature]})
    built = build(api, WORKSPACE, studio(), "fs", "fsmv")

    assert (built.element_id, built.created) == ("e0", False)
    [(_, path, update)] = [request for request in api.requests if request[1].endswith("/featureid/F1")]
    # The microversion after the configuration changed
    assert update["sourceMicroversion"] == "mv4"
    assert update["feature"]["namespace"] == "efs::mfsmv"
    parameters = update["feature"]["parameters"]
    assert [parameter["parameterId"] for parameter in parameters] == ["size", "other", "length", "washer"]
    assert parameters[0]["btType"] == "BTMParameterConfigured-2222"
    assert parameters[1] == feature["parameters"][1]


def test_build_reports_a_feature_that_fails_to_regenerate():
    assert build(FakeApi({}, status="ERROR"), WORKSPACE, studio(), "fs", "fsmv").status == "ERROR"


def test_build_refuses_two_tabs_or_features_of_its_kind():
    with pytest.raises(ConfigureError, match="2 Part Studios named Screw"):
        api = FakeApi({})
        api.tabs = {"a": ("Screw", []), "b": ("Screw", [])}
        build(api, WORKSPACE, studio(), "fs", "fsmv")
    twice = [{"featureId": id, "featureType": "fastenerScrew", "parameters": []} for id in ("F1", "F2")]
    with pytest.raises(ConfigureError, match="2 fastenerScrew features"):
        build(FakeApi({"Screw": twice}), WORKSPACE, studio(), "fs", "fsmv")


def test_build_says_which_step_onshape_refused():
    from onshape_api.exceptions import ApiError

    class Refusing(FakeApi):
        def post(self, path, body="", **kwargs):
            if path.endswith("/features"):
                raise ApiError("The change would result in the feature list becoming invalid", 409)
            return super().post(path, body, **kwargs)

    with pytest.raises(ConfigureError, match="Onshape refused to insert fastenerScrew in Screw: .*feature list"):
        build(Refusing({}), WORKSPACE, studio(), "fs", "fsmv")


def test_configuration_stages_add_each_inputs_conditions_in_turn():
    stages = configuration_stages(studio())
    full = studio().configuration_body()
    assert stages[0][1]["configurationParameters"] == []
    assert all("visibilityCondition" not in parameter and "enumOptionVisibilityConditions" not in parameter
               for parameter in stages[1][1]["configurationParameters"])
    assert stages[-1][1] == full
    assert [len(body["configurationParameters"]) for _, body in stages[1:]] == [3] * (len(stages) - 1)


def test_diagnose_names_the_first_stage_onshape_refuses():
    from onshape_api.exceptions import ApiError

    class Refusing(FakeApi):
        def post(self, path, body="", **kwargs):
            if path.endswith("/configuration") and any(
                "enumOptionVisibilityConditions" in parameter or "visibilityCondition" in parameter
                for parameter in body["configurationParameters"]
            ):
                raise ApiError("The change would result in the feature list becoming invalid", 409)
            return super().post(path, body, **kwargs)

    api = Refusing({"Screw (TEST)": [{"featureType": "fastenerScrew"}]})
    diagnosis = diagnose(api, WORKSPACE, studio(), "Screw (TEST)")
    assert diagnosis.features == ["fastenerScrew"]
    assert diagnosis.accepted == ["an empty configuration", "the inputs, with no conditions"]
    assert diagnosis.refused is not None and "feature list becoming invalid" in diagnosis.refused[1]
    assert diagnosis.element_id == "new"
