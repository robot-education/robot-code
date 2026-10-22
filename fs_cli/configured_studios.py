"""Part Studios which expose a feature through their configuration, for definitions in featurescripts/ (see
fasteners/fastenerTables.py).

A configured studio holds one instance of a feature, and a configuration input for each of its parameters: each input
configures its parameter (a configured parameter, `BTMParameterConfigured`), with one value per option, or per state
of a checkbox. A List input's options can each be shown only for some combinations of earlier inputs, which is how a
lookup table is laid out as a configuration: Length shows only the lengths the chosen vendor makes in the chosen size
and head. A whole input can be shown only for some, too; hidden, it takes its default.

The definition is the source of both sides: the feature's enums and data (`fs gen`), and the configuration
(`fs parts`, see docs/parts.md), so they can't drift apart. Option ids are FeatureScript identifiers, since Onshape makes each
List input an enum (`<input id>_conf`) whose values are its option ids.
"""

from __future__ import annotations

import contextlib
import dataclasses
import json
import pathlib
import re
from typing import Literal, Mapping, Sequence

_IDENTIFIER = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")

# Each, the options of earlier inputs it shows with: shown when any one matches
Combinations = Sequence[Mapping[str, str]]


@dataclasses.dataclass
class Option:
    """An option of a List input.

    Attributes:
        id: Its id: a FeatureScript identifier, like `NO_10_32`.
        name: The name shown, like `#10-32`.
        value: The configured parameter's value for it: an enum value's name for an enum parameter, or an expression
            for a quantity, like `0.5 in`.
        shown_when: The combinations of earlier inputs' options which show it, like `[{"size": "NO_10_32"}]`; every
            combination shows it when None.
    """

    id: str
    name: str
    value: str
    shown_when: Combinations | None = None


@dataclasses.dataclass
class Input:
    """A configuration input, configuring one of the feature's parameters.

    Attributes:
        id: The input's id, which configurations are encoded with (`size=NO_10_32;washer=true`).
        parameter: The feature parameter it configures.
        kind: A List of `options` configuring an enum (`enum_name` its type) or a length, or a checkbox configuring a
            boolean.
        shown_when: The combinations of earlier inputs' options which show it, as an option's are; always shown when
            None. Never name an input which is itself hidden somewhere: Onshape and FRCDesign read a hidden input's
            value differently.
    """

    id: str
    name: str
    parameter: str
    options: Sequence[Option] = ()
    kind: Literal["enum", "length", "boolean"] = "enum"
    enum_name: str | None = None
    default: str | None = None
    shown_when: Combinations | None = None

    def __post_init__(self) -> None:
        if not _IDENTIFIER.match(self.id):
            raise ValueError(f"Input {self.id!r} isn't a FeatureScript identifier")
        if self.kind == "boolean":
            if self.options or self.default not in (None, "true", "false"):
                raise ValueError(f"Input {self.id} is a checkbox: it has no options, and defaults to true or false")
            return
        ids = [option.id for option in self.options]
        if not ids:
            raise ValueError(f"Input {self.id} has no options")
        if len(set(ids)) != len(ids):
            raise ValueError(f"Input {self.id} has duplicate options: {ids}")
        for id in ids:
            if not _IDENTIFIER.match(id):
                raise ValueError(f"Input {self.id}: {id!r} isn't a FeatureScript identifier")
        if self.kind == "enum" and self.enum_name is None:
            raise ValueError(f"Input {self.id} configures an enum, so it needs its enum_name")
        if self.default is not None and self.default not in ids:
            raise ValueError(f"Input {self.id} has default {self.default!r}, which isn't one of {ids}")

    @property
    def default_option(self) -> str:
        if self.kind == "boolean":
            return self.default or "false"
        return self.default or self.options[0].id

    def option_ids(self) -> set[str]:
        return {option.id for option in self.options}


@dataclasses.dataclass
class ConfiguredStudio:
    """A Part Studio exposing `feature` (the constant `defineFeature` is exported as) through `inputs`.

    Attributes:
        name: Its key in the definition, like `screw`.
        tab: Its tab's name, which `build` finds it by; `name` capitalized when None.
        factor_first: Inputs to factor visibility conditions by before the rest, which are in order: a lookup
            table's conditions can be far smaller factored by one with few values, like a finish.
    """

    name: str
    feature: str
    inputs: Sequence[Input]
    tab: str | None = None
    factor_first: Sequence[str] = ()

    @property
    def tab_name(self) -> str:
        return self.tab or self.name.capitalize()

    def __post_init__(self) -> None:
        defaults: dict[str, str] = {}
        earlier: dict[str, Input] = {}
        for input in self.inputs:
            shown_by = [(input.id, input.shown_when)] + [(f"{input.id}'s {option.id}", option.shown_when) for option in input.options]
            for what, shown_when in shown_by:
                for combination in shown_when or ():
                    for id, value in combination.items():
                        parent = earlier.get(id)
                        if parent is None:
                            raise ValueError(f"{self.name}: {what} shows by {id}, which isn't before it")
                        if parent.shown_when is not None:
                            raise ValueError(f"{self.name}: {what} shows by {id}, which is hidden somewhere")
                        if parent.kind == "boolean" or value not in parent.option_ids():
                            raise ValueError(f"{self.name}: {what} shows with unknown {id}={value}")
            if input.kind != "boolean" and input_shows(input, defaults):
                default = next(option for option in input.options if option.id == input.default_option)
                if not shows(default, defaults):
                    raise ValueError(f"{self.name}: {input.id}'s default isn't shown with the other inputs' defaults")
            defaults[input.id] = input.default_option
            earlier[input.id] = input

    def combinations(self) -> list[dict[str, str]]:
        """Every combination of its inputs' options the configuration shows, earliest input first: each a choice
        of every input it shows, with each List's option one shown with the choices before it. A hidden input is
        left out, as FRCDesign enumerates it."""
        combinations: list[dict[str, str]] = [{}]
        for input in self.inputs:
            choices = ["false", "true"] if input.kind == "boolean" else [option.id for option in input.options]
            next: list[dict[str, str]] = []
            for combination in combinations:
                shown = [
                    choice
                    for choice in choices
                    if input.kind == "boolean" or shows(_option(input, choice), combination)
                ]
                # Hidden, or with no option shown, it's left at its default
                if not input_shows(input, combination) or not shown:
                    next.append(combination)
                    continue
                next.extend({**combination, input.id: choice} for choice in shown)
            combinations = next
        return combinations

    def defaults(self) -> dict[str, str]:
        return {input.id: input.default_option for input in self.inputs}

    def manifest(self) -> dict:
        """The studio as FRCDesign reads it: its feature, and its configuration as Onshape answers it, which FRCDesign
        parses as it parses any configuration."""
        return {"feature": self.feature, "configuration": self.configuration_body()}

    def _factor_order(self) -> list[str]:
        return [*self.factor_first, *(input.id for input in self.inputs if input.id not in self.factor_first)]

    def configuration_body(self) -> dict:
        """The body of `POST /elements/.../configuration` setting the studio's inputs."""
        return {
            "btType": "BTConfigurationUpdateCall-2933",
            "configurationParameters": [
                _configuration_parameter(input, self._factor_order()) for input in self.inputs
            ],
        }

    def configured_parameters(self, namespace: str = "") -> list[dict]:
        """The feature's parameters, each configured by its input. `namespace` is the feature's (see
        `feature_namespace`), which its enums' values are named in too, since they're defined in its FeatureScript."""
        return [_configured_parameter(input, namespace) for input in self.inputs]


def _option(input: Input, id: str) -> Option:
    return next(option for option in input.options if option.id == id)


def shows(option: Option, selection: Mapping[str, str]) -> bool:
    """Whether an option is shown while earlier inputs are set to `selection`."""
    return _matches(option.shown_when, selection)


def input_shows(input: Input, selection: Mapping[str, str]) -> bool:
    """Whether an input is shown while earlier inputs are set to `selection`."""
    return _matches(input.shown_when, selection)


def _matches(shown_when: Combinations | None, selection: Mapping[str, str]) -> bool:
    if shown_when is None:
        return True
    return any(all(selection.get(id) == value for id, value in combination.items()) for combination in shown_when)


def _configuration_parameter(input: Input, order: Sequence[str]) -> dict:
    visibility = {} if input.shown_when is None else {"visibilityCondition": _condition(input.shown_when, order)}
    if input.kind == "boolean":
        return {
            "btType": "BTMConfigurationParameterBoolean-2550",
            "parameterId": input.id,
            "parameterName": input.name,
            "defaultValue": input.default_option == "true",
            **visibility,
        }
    parameter = {
        "btType": "BTMConfigurationParameterEnum-105",
        "parameterId": input.id,
        "parameterName": input.name,
        "enumName": f"{input.id}_conf",
        "namespace": "",
        "defaultValue": input.default_option,
        "options": [
            {"btType": "BTMEnumOption-592", "option": option.id, "optionName": option.name}
            for option in input.options
        ],
        **visibility,
    }
    conditions = _option_visibility(input, order)
    if conditions:
        parameter["enumOptionVisibilityConditions"] = {
            "btType": "BTEnumOptionVisibilityConditionList-2936",
            "visibilityConditions": conditions,
        }
    return parameter


def _option_visibility(input: Input, order: Sequence[str]) -> list[dict]:
    """One condition per distinct set of combinations, controlling the options shown with exactly that set: Onshape's
    conditions each control a list of options, so no option is controlled twice."""
    groups: dict[tuple, list[str]] = {}
    for option in input.options:
        if option.shown_when is not None:
            key = tuple(sorted(tuple(sorted(combination.items())) for combination in option.shown_when))
            groups.setdefault(key, []).append(option.id)
    return [
        {
            "btType": "BTEnumOptionVisibilityForList-1613",
            "controlledOptions": options,
            "condition": _condition([dict(combination) for combination in key], order),
        }
        for key, options in groups.items()
    ]


def _equal(id: str, value: str) -> dict:
    return {"btType": "BTParameterVisibilityOnEqual-180", "parameterId": id, "value": value}


def _logical(operation: str, children: list[dict]) -> dict:
    if len(children) == 1:
        return children[0]
    return {"btType": "BTParameterVisibilityLogical-178", "operation": operation, "children": children}


def _condition(combinations: Combinations, order: Sequence[str]) -> dict:
    """Shown when any combination matches, factored by input in `order`: the combinations are grouped by their first
    input's value, and values whose rest is the same share one branch. A lookup table's conditions are mostly
    repetition, which this keeps out of the configuration."""
    ids = [id for id in order if any(id in combination for combination in combinations)]
    if not ids:
        raise ValueError("A condition needs at least one input")
    return _factor(combinations, ids) or _logical("OR", [])


def _factor(combinations: Combinations, ids: Sequence[str]) -> dict | None:
    """The condition on `ids` matching any of `combinations`; None when every combination is empty (always shown)."""
    if not ids or all(not combination for combination in combinations):
        return None
    first, rest = ids[0], ids[1:]
    by_value: dict[str, list[dict[str, str]]] = {}
    for combination in combinations:
        remainder = {id: value for id, value in combination.items() if id != first}
        by_value.setdefault(combination[first], []).append(remainder)
    # Values with the same rest share it
    branches: dict[str, tuple[list[str], dict | None]] = {}
    for value, remainders in by_value.items():
        sub = _factor(remainders, rest)
        key = repr(sub)
        branches.setdefault(key, ([], sub))[0].append(value)
    children = []
    for values, sub in branches.values():
        equal = _logical("OR", [_equal(first, value) for value in values])
        children.append(equal if sub is None else _logical("AND", [equal, sub]))
    return _logical("OR", children)


def _configured_parameter(input: Input, namespace: str = "") -> dict:
    if input.kind == "boolean":
        values = [
            {
                "btType": "BTMConfiguredValueByBoolean-1501",
                "booleanValue": state,
                "value": {"btType": "BTMParameterBoolean-144", "parameterId": input.parameter, "value": state},
            }
            for state in (False, True)
        ]
    else:
        values = [
            {
                "btType": "BTMConfiguredValueByEnum-1923",
                "enumName": f"{input.id}_conf",
                "namespace": "",
                "enumValue": option.id,
                "value": _value(input, option, namespace),
            }
            for option in input.options
        ]
    return {
        "btType": "BTMParameterConfigured-2222",
        "parameterId": input.parameter,
        "configurationParameterId": input.id,
        "values": values,
    }


def _value(input: Input, option: Option, namespace: str = "") -> dict:
    if input.kind == "enum":
        return {
            "btType": "BTMParameterEnum-145",
            "parameterId": input.parameter,
            "namespace": namespace,
            "enumName": input.enum_name,
            "value": option.value,
        }
    return {"btType": "BTMParameterQuantity-147", "parameterId": input.parameter, "expression": option.value}


class ConfigureError(Exception):
    pass


def configured_studios(definition: dict) -> list[ConfiguredStudio]:
    """A definition's ConfiguredStudios, from its STUDIOS (nested in maps as deep as it likes)."""
    found: list[ConfiguredStudio] = []

    def search(value: object) -> None:
        if isinstance(value, ConfiguredStudio):
            found.append(value)
        elif isinstance(value, dict):
            for item in value.values():
                search(item)

    search(definition.get("STUDIOS", {}))
    return found


# Where a definition's studios were last released, beside it: written by `fs release`, read by the definition
LOCATIONS_SUFFIX = ".studios.json"


def locations_path(definition: pathlib.Path) -> pathlib.Path:
    return definition.with_name(definition.stem + LOCATIONS_SUFFIX)


def read_locations(definition: pathlib.Path) -> dict[str, dict[str, str]]:
    """Each of a definition's released studios, by name, as `{documentId, versionId, elementId}`; none before its
    first release."""
    path = locations_path(definition)
    if not path.is_file():
        return {}
    data = json.loads(path.read_text())
    return {
        name: {"documentId": data["documentId"], "versionId": data["versionId"], "elementId": element_id}
        for name, element_id in data["elements"].items()
    }


def write_locations(definition: pathlib.Path, document_id: str, version_id: str, elements: Mapping[str, str]) -> None:
    """Records where a definition's studios were released: the library's document and version, and each one's tab."""
    data = {"documentId": document_id, "versionId": version_id, "elements": dict(elements)}
    locations_path(definition).write_text(json.dumps(data, indent=2) + "\n", newline="")


def feature_namespace(element_id: str, microversion_id: str) -> str:
    """How a feature names the Feature Studio defining it, when the Part Studio is in the same workspace."""
    return f"e{element_id}::m{microversion_id}"


@dataclasses.dataclass
class Built:
    """A Part Studio `build` made or updated.

    Attributes:
        element_id: Its tab, which keeps its id from build to build.
        created: Whether `build` made the tab.
        status: Its feature's state in the default configuration, as Onshape reports it: OK, INFO, WARNING or ERROR.
    """

    element_id: str
    created: bool
    status: str


def build(api, workspace, studio: ConfiguredStudio, feature_studio_id: str, feature_studio_microversion: str) -> Built:
    """Makes a Part Studio exactly `studio`: finds its tab by name in a workspace, or creates it; writes its
    configuration; then inserts its feature, from the Feature Studio in the same workspace, configured by it, or
    points the one it has at that Feature Studio as it is now and configures it.

    Args:
        api: An onshape_api Api.
        workspace: The document's InstancePath, a workspace.
        feature_studio_microversion: The Feature Studio's microversion, which the feature is inserted at.
    """
    from onshape_api.endpoints import documents, elements, part_studios
    from onshape_api.exceptions import ApiError
    from onshape_api.paths.paths import ElementPath
    from onshape_api.types import ElementType

    @contextlib.contextmanager
    def step(doing: str):
        """Says which step Onshape refused, since its own messages don't."""
        try:
            yield
        except ApiError as error:
            raise ConfigureError(f"Onshape refused to {doing} in {studio.tab_name}: {error}") from error

    tabs = [
        element
        for element in documents.get_document_elements(api, workspace, ElementType.PART_STUDIO)
        if element["name"] == studio.tab_name
    ]
    if len(tabs) > 1:
        raise ConfigureError(f"The document has {len(tabs)} Part Studios named {studio.tab_name}; keep one.")
    created = not tabs
    if created:
        with step("create the tab"):
            element_id = part_studios.create_part_studio(api, workspace, studio.tab_name)["id"]
    else:
        element_id = tabs[0]["id"]
    part_studio = ElementPath.from_path(workspace, element_id)

    feature_list = part_studios.get_features(api, part_studio)
    matches = [feature for feature in feature_list["features"] if feature.get("featureType") == studio.feature]
    if len(matches) > 1:
        raise ConfigureError(f"{studio.tab_name} has {len(matches)} {studio.feature} features; keep one.")
    # Configured parameters name the configuration's inputs, so it comes first
    with step("write the configuration"):
        elements.update_configuration(api, part_studio, studio.configuration_body())
    namespace = feature_namespace(feature_studio_id, feature_studio_microversion)
    if not matches:
        with step(f"insert {studio.feature}"):
            response = part_studios.add_feature(
                api,
                part_studio,
                {
                    "btType": "BTMFeature-134",
                    "featureType": studio.feature,
                    "name": studio.tab_name,
                    "namespace": namespace,
                    "parameters": studio.configured_parameters(namespace),
                },
            )
    else:
        # Its configuration changed its microversion
        feature_list = part_studios.get_features(api, part_studio)
        feature = next(feature for feature in feature_list["features"] if feature.get("featureType") == studio.feature)
        configured = {parameter["parameterId"]: parameter for parameter in studio.configured_parameters(namespace)}
        parameters = [configured.pop(parameter["parameterId"], parameter) for parameter in feature.get("parameters", [])]
        with step(f"update {studio.feature}"):
            response = part_studios.update_feature(
                api,
                part_studio,
                {**feature, "namespace": namespace, "parameters": [*parameters, *configured.values()]},
                feature_list["sourceMicroversion"],
            )
    status = response.get("featureState", {}).get("featureStatus", "OK")
    return Built(element_id, created, status)
