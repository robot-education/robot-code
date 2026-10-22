"""Part Studios which expose a feature through their configuration, for definitions in featurescripts/ (see
fasteners/fastenerTables.py).

A configured studio holds one instance of a feature, and a configuration input for each of its parameters: each input
configures its parameter (a configured parameter, `BTMParameterConfigured`), with one value per option, or per state
of a checkbox. A List input's options can each be shown only for some combinations of earlier inputs, which is how a
lookup table is laid out as a configuration: Length shows only the lengths the chosen vendor makes in the chosen size
and head. A whole input can be shown only for some, too; hidden, it takes its default.

The definition is the source of both sides: the feature's enums and data (`fs gen`), and the configuration
(`fs configure-studio`), so they can't drift apart. Option ids are FeatureScript identifiers, since Onshape makes each
List input an enum (`<input id>_conf`) whose values are its option ids.
"""

from __future__ import annotations

import dataclasses
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
    """A Part Studio exposing `feature` (the constant `defineFeature` is exported as) through `inputs`."""

    name: str
    feature: str
    inputs: Sequence[Input]

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

    def configuration_body(self) -> dict:
        """The body of `POST /elements/.../configuration` setting the studio's inputs."""
        return {
            "btType": "BTConfigurationUpdateCall-2933",
            "configurationParameters": [
                _configuration_parameter(input, [earlier.id for earlier in self.inputs]) for input in self.inputs
            ],
        }

    def configured_parameters(self) -> list[dict]:
        """The feature's parameters, each configured by its input."""
        return [_configured_parameter(input) for input in self.inputs]


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


def _configured_parameter(input: Input) -> dict:
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
                "value": _value(input, option),
            }
            for option in input.options
        ]
    return {
        "btType": "BTMParameterConfigured-2222",
        "parameterId": input.parameter,
        "configurationParameterId": input.id,
        "values": values,
    }


def _value(input: Input, option: Option) -> dict:
    if input.kind == "enum":
        return {
            "btType": "BTMParameterEnum-145",
            "parameterId": input.parameter,
            "enumName": input.enum_name,
            "value": option.value,
        }
    return {"btType": "BTMParameterQuantity-147", "parameterId": input.parameter, "expression": option.value}


class ConfigureError(Exception):
    pass


def find_studio(definition: dict, name: str) -> ConfiguredStudio:
    """The ConfiguredStudio named name in a definition's STUDIOS (nested in maps as deep as it likes)."""
    found: list[ConfiguredStudio] = []

    def search(value: object) -> None:
        if isinstance(value, ConfiguredStudio) and value.name == name:
            found.append(value)
        elif isinstance(value, dict):
            for item in value.values():
                search(item)

    search(definition.get("STUDIOS", {}))
    if not found:
        raise ConfigureError(f"The definition has no configured studio named {name}.")
    return found[0]


def configure(api, part_studio, studio: ConfiguredStudio) -> None:
    """Writes studio's configuration inputs to a Part Studio, then configures its feature's parameters by them.

    The Part Studio must hold one instance of the feature (inserted by hand, from the released version it should use):
    which version of a custom feature a Part Studio uses is chosen when it's inserted.

    Args:
        api: An onshape_api Api.
        part_studio: The Part Studio's ElementPath, in a workspace.
    """
    from onshape_api.endpoints import elements, part_studios

    feature_list = part_studios.get_features(api, part_studio)
    matches = [feature for feature in feature_list["features"] if feature.get("featureType") == studio.feature]
    if len(matches) != 1:
        raise ConfigureError(
            f"The Part Studio has {len(matches)} {studio.feature} features; insert exactly one, then run this again."
        )
    elements.update_configuration(api, part_studio, studio.configuration_body())
    # Its configuration changed its microversion
    feature_list = part_studios.get_features(api, part_studio)
    feature = next(feature for feature in feature_list["features"] if feature.get("featureType") == studio.feature)
    configured = {parameter["parameterId"]: parameter for parameter in studio.configured_parameters()}
    parameters = [configured.pop(parameter["parameterId"], parameter) for parameter in feature.get("parameters", [])]
    part_studios.update_feature(
        api,
        part_studio,
        {**feature, "parameters": [*parameters, *configured.values()]},
        feature_list["sourceMicroversion"],
    )
