import pytest

from fs_cli.configured_studios import (
    ConfigureError,
    ConfiguredStudio,
    Input,
    LookupTable,
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
    size = Input("size", "Size", "size", [Option("A", "A", "A"), Option("C", "C", "C")], enum_name="Size")

    def length(shown_when):
        return Input("length", "Length", "length", [Option("L", "L", "1 in"), Option("M", "M", "2 in", shown_when)], kind="length")

    with pytest.raises(ValueError, match="isn't before it"):
        ConfiguredStudio("s", "f", [length([{"size": "A"}]), size])
    with pytest.raises(ValueError, match="unknown size=B"):
        ConfiguredStudio("s", "f", [size, length([{"size": "B"}])])


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
        "parameterId": "",
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


def test_a_product_leaves_out_an_input_its_combination_doesnt_name():
    from fs_cli.configured_studios import _products

    assert _products([{"size": "A", "head": "X"}, {"size": "B"}], ["size", "head"]) == [
        {"size": ["A"], "head": ["X"]},
        {"size": ["B"]},
    ]
    assert _products([{"size": "A", "head": "X"}, {"head": "Y"}], ["size", "head"]) == [
        {"size": ["A"], "head": ["X"]},
        {"head": ["Y"]},
    ]


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
    assert washer["values"][1]["value"] == {"btType": "BTMParameterBoolean-144", "parameterId": "", "value": True}
    assert size["configurationParameterId"] == "size"
    assert size["values"][1]["enumValue"] == "B"
    assert size["values"][1]["value"] == {
        "btType": "BTMParameterEnum-145",
        "parameterId": "",
        "namespace": "",
        "enumName": "Size",
        "value": "B",
    }
    assert length["values"][0]["value"] == {
        "btType": "BTMParameterQuantity-147",
        "parameterId": "",
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


def test_an_input_is_named_only_where_it_shows():
    def length(shown_when):
        options = [Option("L", "L", "1 in"), Option("M", "M", "2 in", shown_when)]
        return Input("length", "Length", "length", options, kind="length")

    # Finish shows only with size B
    with pytest.raises(ValueError, match="finish=BLACK, which isn't shown there"):
        ConfiguredStudio("s", "f", [*finished().inputs, length([{"finish": "BLACK"}])])
    with pytest.raises(ValueError, match="finish=ZINC, which isn't shown there"):
        ConfiguredStudio("s", "f", [*finished().inputs, length([{"size": "A", "finish": "ZINC"}])])
    studio = ConfiguredStudio("s", "f", [*finished().inputs, length([{"size": "A"}, {"size": "B", "finish": "ZINC"}])])
    assert {"size": "A", "length": "M"} in studio.combinations()
    assert {"size": "B", "finish": "BLACK", "length": "M"} not in studio.combinations()


def fixed() -> ConfiguredStudio:
    return ConfiguredStudio(
        "nut",
        "f",
        [
            Input("vendor", "Vendor", "vendor", [Option("M", "M", "M")], enum_name="Vendor"),
            Input("size", "Size", "size", [Option("A", "A", "A"), Option("B", "B", "B")], enum_name="Size"),
        ],
    )


def test_an_input_of_one_option_is_fixed_and_left_out_of_the_configuration():
    studio = fixed()
    assert [input.id for input in studio.configured_inputs()] == ["size"]
    assert [parameter["parameterId"] for parameter in studio.configuration_body()["configurationParameters"]] == ["size"]
    assert [parameter["parameterId"] for parameter in studio.configured_parameters()] == ["size"]
    # Set where the feature's inserted
    assert studio.default_parameters()[0]["value"] == "M"
    assert studio.combinations() == [{"size": "A"}, {"size": "B"}]
    assert studio.defaults() == {"size": "A"}


def test_nothing_shows_by_a_fixed_input():
    with pytest.raises(ValueError, match="fixed, and shown nowhere"):
        Input("vendor", "Vendor", "vendor", [Option("M", "M", "M", [{"size": "A"}])], enum_name="Vendor")
    length = Input("length", "Length", "length", [Option("L", "L", "1 in"), Option("M", "M", "2 in", [{"vendor": "M"}])], kind="length")
    with pytest.raises(ValueError, match="shows by vendor, which is fixed"):
        ConfiguredStudio("s", "f", [*fixed().inputs, length])


def enum_input(id: str, values: str, default: str | None = None) -> Input:
    return Input(id, id.capitalize(), id, [Option(value, value, value) for value in values.split()], enum_name=id, default=default)


TABLE = LookupTable(
    ("vendor", "size", "finish"),
    [("A", "S1", "BLACK"), ("A", "S1", "ZINC"), ("A", "S2", "BLACK"), ("B", "S1", "BLACK")],
)


def test_a_lookup_table_shows_an_input_only_where_more_than_one_option_is_made():
    vendor = TABLE.input(enum_input("vendor", "A B C"))
    size = TABLE.input(enum_input("size", "S1 S2"))
    finish = TABLE.input(enum_input("finish", "BLACK ZINC"))
    # C isn't made at all; B makes one size, and A one finish in S2
    assert [(option.id, option.shown_when) for option in vendor.options] == [("A", None), ("B", None)]
    assert [(option.id, option.shown_when) for option in size.options] == [("S1", [{"vendor": "A"}]), ("S2", [{"vendor": "A"}])]
    assert [option.shown_when for option in finish.options] == [[{"vendor": "A", "size": "S1"}]] * 2
    # A configuration is the choices shown, and so is each thing made
    assert TABLE.shown(("B", "S1", "BLACK")) == {"vendor": "B"}
    assert TABLE.shown(("A", "S2", "BLACK")) == {"vendor": "A", "size": "S2"}
    assert TABLE.shown(("A", "S1", "ZINC")) == {"vendor": "A", "size": "S1", "finish": "ZINC"}
    studio = ConfiguredStudio("s", "f", [vendor, size, finish])
    assert sorted(map(str, studio.combinations())) == sorted(
        str(TABLE.shown(key)) for key in [("A", "S1", "BLACK"), ("A", "S1", "ZINC"), ("A", "S2", "BLACK"), ("B", "S1", "BLACK")]
    )


def test_a_lookup_table_fixes_an_input_never_shown():
    table = LookupTable(("vendor", "size"), [("M", "S1"), ("M", "S2")])
    vendor = table.input(enum_input("vendor", "G M", default="G"))
    assert vendor.fixed and vendor.options[0].id == "M"
    assert table.shown(("M", "S2")) == {"size": "S2"}


def test_a_lookup_table_needs_an_option_for_everything_made():
    with pytest.raises(ValueError, match=r"no option for \['B'\]"):
        TABLE.input(enum_input("vendor", "A"))
    with pytest.raises(ValueError, match="default 'S3'"):
        TABLE.input(enum_input("size", "S1 S2 S3", default="S3"))


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

    def __init__(self, tabs: dict[str, list[dict]], status: str = "OK", stored_namespace: str | None = None):
        self.tabs = {f"e{index}": (name, features) for index, (name, features) in enumerate(tabs.items())}
        self.status = status
        # What Onshape stores an inserted feature's namespace, and its enums', as; as sent when None
        self.stored_namespace = stored_namespace
        self.requests = []

    def delete(self, path, **kwargs):
        self.requests.append(("DELETE", path, None))
        element_id = path.split("/e/")[1].split("/")[0]
        feature_id = path.rsplit("/", 1)[1]
        features = self.tabs[element_id][1]
        features[:] = [feature for feature in features if feature["featureId"] != feature_id]

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
            feature = {**body["feature"], "featureId": "F"}
            if self.stored_namespace is not None:
                feature["namespace"] = self.stored_namespace
                feature["parameters"] = [
                    {**parameter, "namespace": self.stored_namespace} if "namespace" in parameter else parameter
                    for parameter in feature["parameters"]
                ]
            self.tabs[element_id][1].append(feature)
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


def test_build_replaces_the_feature_a_build_inserted_before():
    feature = {
        "featureId": "F1",
        "featureType": "fastenerScrew",
        "namespace": "efs::mold",
        "parameters": [{"btType": "BTMParameterEnum-145", "parameterId": "size", "value": "A"}],
    }
    api = FakeApi({"Screw": [{"featureId": "S", "featureType": "newSketch"}, feature]})
    built = build(api, WORKSPACE, studio(), "fs", "fsmv")

    assert (built.element_id, built.created) == ("e0", False)
    writes = [(method, path) for method, path, _ in api.requests if method != "GET"]
    # Deleted first: Onshape refuses a configuration which leaves its configured parameters naming options it lacks
    assert writes == [
        ("DELETE", "/partstudios/d/d/w/w/e/e0/features/featureid/F1"),
        ("POST", "/elements/d/d/w/w/e/e0/configuration"),
        ("POST", "/partstudios/d/d/w/w/e/e0/features"),
        ("POST", "/partstudios/d/d/w/w/e/e0/features/featureid/F"),
    ]
    [update] = [body for _, path, body in api.requests if path.endswith("/featureid/F")]
    # The feature list's, read after the insert
    assert update["sourceMicroversion"] == "mv6"
    assert update["feature"]["namespace"] == "efs::mfsmv"
    parameters = update["feature"]["parameters"]
    assert [parameter["parameterId"] for parameter in parameters] == ["size", "length", "washer"]
    assert all(parameter["btType"] == "BTMParameterConfigured-2222" for parameter in parameters)


def test_build_configures_a_feature_in_the_namespaces_onshape_stored():
    # As from a Feature Studio re-exporting another document's: Onshape stores where it's defined
    stored = "dback::vver::eback::mbackmv"
    api = FakeApi({}, stored_namespace=stored)
    build(api, WORKSPACE, studio(), "fs", "fsmv")

    [update] = [body for _, path, body in api.requests if path.endswith("/featureid/F")]
    assert update["feature"]["namespace"] == stored
    size = update["feature"]["parameters"][0]["values"][0]["value"]
    assert (size["enumName"], size["namespace"]) == ("Size", stored)


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


def test_problems_are_none_for_a_studio_kept_to_the_rules():
    assert studio().problems() == []
    assert finished().problems() == []
    assert ConfiguredStudio("s", "f", [TABLE.input(enum_input(id, values)) for id, values in [("vendor", "A B"), ("size", "S1 S2"), ("finish", "BLACK ZINC")]]).problems() == []


def test_problems_name_an_input_shown_with_one_option():
    one = ConfiguredStudio(
        "s",
        "f",
        [
            Input("size", "Size", "size", [Option("A", "A", "A"), Option("B", "B", "B")], enum_name="Size"),
            Input("finish", "Finish", "finish", [Option("X", "X", "X"), Option("Y", "Y", "Y", [{"size": "B"}])], enum_name="Finish"),
        ],
    )
    assert one.problems() == ["s: finish shows one option, X, with {'size': 'A'}"]


def test_problems_name_a_condition_onshape_refuses():
    from fs_cli.configured_studios import _condition_problems

    options = {"size": ["A", "B"]}
    assert _condition_problems(logical("AND", logical("OR", equal("size", "A"))), {"size"}, options) == []
    assert _condition_problems(logical("OR", equal("size", "A")), {"size"}, options) == ["a condition isn't an AND"]
    assert _condition_problems(logical("AND", logical("OR", equal("size", "C"))), {"size"}, options) == [
        "a condition names size's options ['C'], not each one of its options once"
    ]
    assert _condition_problems(logical("AND", logical("OR", equal("size", "A"))), set(), options) == [
        "a condition names size, which isn't a configured input before it"
    ]
    assert _condition_problems(logical("AND", logical("OR", equal("size", "A"), equal("head", "X"))), {"size"}, options) == [
        "an OR isn't of one input's options",
        "a condition names no input",
    ]
