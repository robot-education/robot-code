"""Part Studios which expose a feature through their configuration, for definitions in featurescripts/ (see
quickFastener/quickFastenerTables.py).

A configured studio holds one instance of a feature, and a List configuration input for each of its parameters: each
input configures its parameter (a configured parameter, `BTMParameterConfigured`), with one value per option. An
input's options can each be shown only while another input has some values, which is how a nested lookup table is laid
out as a configuration: Length shows only the lengths the chosen Size comes in.

The definition is the source of both sides: the feature's enums and data (`fs gen`), and the configuration
(`fs configure-studio`), so they can't drift apart. Option ids are FeatureScript identifiers, since Onshape makes each
List input an enum (`<input id>_conf`) whose values are its option ids.
"""

from __future__ import annotations

import dataclasses
import re
from typing import Literal, Sequence

_IDENTIFIER = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")


@dataclasses.dataclass
class Option:
    """An option of a List input.

    Attributes:
        id: Its id: a FeatureScript identifier, like `NO_10_32`.
        name: The name shown, like `#10-32`.
        value: The configured parameter's value for it: an enum value's name for an enum parameter, or an expression
            for a quantity, like `0.5 in`.
        shown_with: The options of the input's `depends_on` input which show it; every option shows it when None.
    """

    id: str
    name: str
    value: str
    shown_with: Sequence[str] | None = None


@dataclasses.dataclass
class Input:
    """A List configuration input, configuring one of the feature's parameters.

    Attributes:
        id: The input's id, which configurations are encoded with (`size=NO_10_32;length=IN_0_5`).
        parameter: The feature parameter it configures.
        kind: Whether that parameter is an enum (`enum_name` its type) or a quantity (`units` its units).
        depends_on: The input whose value decides which options show (see `Option.shown_with`).
    """

    id: str
    name: str
    parameter: str
    options: Sequence[Option]
    kind: Literal["enum", "length"] = "enum"
    enum_name: str | None = None
    default: str | None = None
    depends_on: str | None = None

    def __post_init__(self) -> None:
        ids = [option.id for option in self.options]
        if len(set(ids)) != len(ids):
            raise ValueError(f"Input {self.id} has duplicate options: {ids}")
        for id in [self.id, *ids]:
            if not _IDENTIFIER.match(id):
                raise ValueError(f"Input {self.id}: {id!r} isn't a FeatureScript identifier")
        if self.kind == "enum" and self.enum_name is None:
            raise ValueError(f"Input {self.id} configures an enum, so it needs its enum_name")
        if self.default is not None and self.default not in ids:
            raise ValueError(f"Input {self.id} has default {self.default!r}, which isn't one of {ids}")
        if self.depends_on is None and any(option.shown_with is not None for option in self.options):
            raise ValueError(f"Input {self.id} shows options by another input, so it needs depends_on")

    @property
    def default_option(self) -> str:
        return self.default or self.options[0].id


@dataclasses.dataclass
class ConfiguredStudio:
    """A Part Studio exposing `feature` (the constant `defineFeature` is exported as) through `inputs`."""

    name: str
    feature: str
    inputs: Sequence[Input]

    def __post_init__(self) -> None:
        by_id = {input.id: input for input in self.inputs}
        for input in self.inputs:
            if input.depends_on is None:
                continue
            parent = by_id.get(input.depends_on)
            if parent is None or self.inputs.index(parent) > self.inputs.index(input):
                raise ValueError(f"{self.name}: {input.id} depends on {input.depends_on}, which must come before it")
            parent_ids = {option.id for option in parent.options}
            for option in input.options:
                unknown = set(option.shown_with or ()) - parent_ids
                if unknown:
                    raise ValueError(f"{self.name}: {input.id}'s {option.id} is shown with unknown {sorted(unknown)}")
            default = next(option for option in input.options if option.id == input.default_option)
            if default.shown_with is not None and parent.default_option not in default.shown_with:
                raise ValueError(f"{self.name}: {input.id}'s default isn't shown with {parent.id}'s default")

    def manifest(self) -> dict:
        """The configuration as FRCDesign reads it: each input's options, and which show with which."""
        return {
            "feature": self.feature,
            "inputs": [
                {
                    "id": input.id,
                    "name": input.name,
                    "default": input.default_option,
                    **({"dependsOn": input.depends_on} if input.depends_on else {}),
                    "options": [
                        {
                            "id": option.id,
                            "name": option.name,
                            **({"shownWith": list(option.shown_with)} if option.shown_with is not None else {}),
                        }
                        for option in input.options
                    ],
                }
                for input in self.inputs
            ],
        }

    def configuration_body(self) -> dict:
        """The body of `POST /elements/.../configuration` setting the studio's inputs."""
        return {
            "btType": "BTConfigurationUpdateCall-2933",
            "configurationParameters": [_configuration_parameter(input) for input in self.inputs],
        }

    def configured_parameters(self) -> list[dict]:
        """The feature's parameters, each configured by its input."""
        return [_configured_parameter(input) for input in self.inputs]


def _configuration_parameter(input: Input) -> dict:
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
    }
    conditions = _option_visibility(input)
    if conditions:
        parameter["enumOptionVisibilityConditions"] = {
            "btType": "BTEnumOptionVisibilityConditionList-2936",
            "visibilityConditions": conditions,
        }
    return parameter


def _option_visibility(input: Input) -> list[dict]:
    """One condition per set of `depends_on` options, controlling the options shown with exactly that set: Onshape's
    conditions each control a list of options, so no option is controlled twice."""
    groups: dict[tuple[str, ...], list[str]] = {}
    for option in input.options:
        if option.shown_with is not None:
            groups.setdefault(tuple(option.shown_with), []).append(option.id)
    conditions = []
    for shown_with, options in groups.items():
        equals = [
            {"btType": "BTParameterVisibilityOnEqual-180", "parameterId": input.depends_on, "value": value}
            for value in shown_with
        ]
        condition = (
            equals[0]
            if len(equals) == 1
            else {"btType": "BTParameterVisibilityLogical-178", "operation": "OR", "children": equals}
        )
        conditions.append(
            {"btType": "BTEnumOptionVisibilityForList-1613", "controlledOptions": options, "condition": condition}
        )
    return conditions


def _configured_parameter(input: Input) -> dict:
    return {
        "btType": "BTMParameterConfigured-2222",
        "parameterId": input.parameter,
        "configurationParameterId": input.id,
        "values": [
            {
                "btType": "BTMConfiguredValueByEnum-1923",
                "enumName": f"{input.id}_conf",
                "namespace": "",
                "enumValue": option.id,
                "value": _value(input, option),
            }
            for option in input.options
        ],
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
