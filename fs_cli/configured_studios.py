"""Part Studios which expose a feature through their configuration, for definitions in featurescripts/ (see
fasteners/fastenerTables.py).

A configured studio holds one instance of a feature, and a configuration input for each of its parameters: each input
configures its parameter (a configured parameter, `BTMParameterConfigured`), with one value per option, or per state
of a checkbox. A List input's options can each be shown only for some combinations of earlier inputs, which is how a
lookup table is laid out as a configuration (`LookupTable`): Length shows only the lengths the chosen vendor makes in
the chosen size and head. A whole input can be shown only for some, too; hidden, it takes its default. An input of one
option is fixed: it's left out of the configuration, and its parameter set to that option when the feature's inserted.

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
        kind: A List of `options` configuring an enum (`enum_name` its type), a length, or a boolean (`switch`: options
            `false` and `true`, which unlike a checkbox's can show by any number of conditions), or a checkbox
            configuring a boolean.
        shown_when: The combinations of earlier inputs' options which show it, as an option's are; always shown when
            None. A combination names an input only where that input and its option show: Onshape and FRCDesign read
            a hidden input's value differently.
    """

    id: str
    name: str
    parameter: str
    options: Sequence[Option] = ()
    kind: Literal["enum", "length", "switch", "boolean"] = "enum"
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
        if self.kind == "switch" and not set(ids) <= {"false", "true"}:
            raise ValueError(f"Input {self.id} is a switch, so its options are false and true")
        if self.kind == "enum" and self.enum_name is None:
            raise ValueError(f"Input {self.id} configures an enum, so it needs its enum_name")
        if self.default is not None and self.default not in ids:
            raise ValueError(f"Input {self.id} has default {self.default!r}, which isn't one of {ids}")
        if self.fixed and (self.shown_when is not None or self.options[0].shown_when is not None):
            raise ValueError(f"Input {self.id} has one option, so it's fixed, and shown nowhere")

    @property
    def default_option(self) -> str:
        if self.kind == "boolean":
            return self.default or "false"
        return self.default or self.options[0].id

    def option_ids(self) -> set[str]:
        return {option.id for option in self.options}

    @property
    def fixed(self) -> bool:
        """Whether it's a List of one option, which the configuration leaves out."""
        return self.kind != "boolean" and len(self.options) == 1


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
                        if parent.kind == "boolean" or value not in parent.option_ids():
                            raise ValueError(f"{self.name}: {what} shows with unknown {id}={value}")
                        if parent.fixed:
                            raise ValueError(f"{self.name}: {what} shows by {id}, which is fixed")
                        if not input_shows(parent, combination) or not shows(_option(parent, value), combination):
                            raise ValueError(f"{self.name}: {what} shows by {id}={value}, which isn't shown there")
            if input.kind == "boolean" and input.shown_when is not None:
                count = len(_products(input.shown_when, self._factor_order()))
                if count > 1:
                    raise ValueError(
                        f"{self.name}: checkbox {input.id} needs {count} conditions to show where it does, and Onshape "
                        "gives an input one (an AND of inputs); a List's options take any number"
                    )
            # With none of its options shown, a List is hidden
            shown = input.kind != "boolean" and any(shows(option, defaults) for option in input.options)
            if shown and input_shows(input, defaults):
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
        for input in self.configured_inputs():
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

    def configured_inputs(self) -> list[Input]:
        """Its inputs but the fixed ones, which the configuration leaves out."""
        return [input for input in self.inputs if not input.fixed]

    def defaults(self) -> dict[str, str]:
        return {input.id: input.default_option for input in self.configured_inputs()}

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
                _configuration_parameter(input, self._factor_order()) for input in self.configured_inputs()
            ],
        }

    def default_parameters(self, namespace: str = "") -> list[dict]:
        """The feature's parameters, each set to its input's default, a fixed input's its one option; `namespace` as for
        `configured_parameters`."""
        return [_default_parameter(input, namespace) for input in self.inputs]

    def configured_parameters(self, namespace: str = "") -> list[dict]:
        """The feature's parameters, each configured by its input. `namespace` is the feature's (see
        `feature_namespace`), which its enums' values are named in too, since they're defined in its FeatureScript."""
        return [_configured_parameter(input, namespace) for input in self.configured_inputs()]


class LookupTable:
    """What's made, as a configuration offers it: each thing made a combination of its inputs' options (`made`), the
    inputs `ids` in order.

    An input shows where more than one of its options is made with the choices shown before it, offering those, and is
    hidden where only one is: so nothing later shows by it there, and the feature resolves its parameter to the one
    made. An input never shown is fixed. A configuration of the table is the choices it shows (`shown`).
    """

    def __init__(self, ids: Sequence[str], made: Sequence[Sequence[str]]) -> None:
        self.ids = list(ids)
        # For each input, the options made in each context: the choices shown before it
        self._made_in: list[dict[tuple, list[str]]] = []
        contexts: dict[tuple, tuple] = {tuple(key): () for key in made}
        for index, id in enumerate(self.ids):
            made_in: dict[tuple, list[str]] = {}
            for key, context in contexts.items():
                values = made_in.setdefault(context, [])
                if key[index] not in values:
                    values.append(key[index])
            self._made_in.append(made_in)
            contexts = {
                key: context + ((id, key[index]),) if len(made_in[context]) > 1 else context
                for key, context in contexts.items()
            }
        self._shown = contexts

    def shown(self, key: Sequence[str]) -> dict[str, str]:
        """The choices a configuration shows to make `key`."""
        return dict(self._shown[tuple(key)])

    def input(self, spec: Input) -> Input:
        """`spec`, one of the table's inputs with every option it could offer, as the configuration offers what's
        made: each option shown where it's made beside another, the rest dropped. Never shown, it's fixed to its
        default, or the first option made where that isn't."""
        made_in = self._made_in[self.ids.index(spec.id)]
        made = {value for values in made_in.values() for value in values}
        if not made <= spec.option_ids():
            raise ValueError(f"Input {spec.id} has no option for {sorted(made - spec.option_ids())}")
        shown_in: dict[str, list[dict[str, str]]] = {}
        for context, values in made_in.items():
            if len(values) > 1:
                for value in values:
                    shown_in.setdefault(value, []).append(dict(context))
        options = [
            dataclasses.replace(option, shown_when=None if len(shown_in[option.id]) == len(made_in) else shown_in[option.id])
            for option in spec.options
            if option.id in shown_in
        ]
        if options:
            return dataclasses.replace(spec, options=options)
        fixed = spec.default if spec.default in made else next(option.id for option in spec.options if option.id in made)
        return dataclasses.replace(spec, options=[_option(spec, fixed)], default=None)


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
    if input.kind == "boolean":
        visibility = {}
        if input.shown_when is not None:
            [product] = _products(input.shown_when, order)
            visibility = {"visibilityCondition": _product_condition(product)}
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
    }
    conditions = _option_visibility(input, order)
    if conditions:
        parameter["enumOptionVisibilityConditions"] = {
            "btType": "BTEnumOptionVisibilityConditionList-2936",
            "visibilityConditions": conditions,
        }
    return parameter


# Onshape takes a condition only as its UI makes one: an AND of inputs, each one of some values
Product = dict[str, list[str]]


def _option_visibility(input: Input, order: Sequence[str]) -> list[dict]:
    """A rule per product, controlling the options it shows, and one showing the rest always, as Onshape's UI ends its
    rules: an option shows while a rule controlling it holds. A List with none of its options shown is hidden, so a
    List's own visibility is folded into its options'."""
    shown = [{}] if input.shown_when is None else _products(input.shown_when, order)
    rules: dict[str, tuple[Product, list[str]]] = {}
    always: list[str] = []
    for option in input.options:
        own = [{}] if option.shown_when is None else _products(option.shown_when, order)
        products = [product for a in own for b in shown if (product := _intersect(a, b)) is not None]
        if not products:
            raise ValueError(f"Input {input.id}'s {option.id} is never shown")
        if any(not product for product in products):
            always.append(option.id)
            continue
        for product in products:
            rules.setdefault(repr(product), (product, []))[1].append(option.id)
    if not rules:
        return []
    return [
        *(_option_rule(options, _product_condition(product)) for product, options in rules.values()),
        *([_option_rule(always, ALWAYS_SHOWN)] if always else []),
    ]


ALWAYS_SHOWN = {
    "btType": "BTParameterVisibilityLogical-178",
    "operation": "AND",
    "children": [{"btType": "BTParameterVisibilityAlwaysShown-5487"}],
}


def _option_rule(options: list[str], condition: dict) -> dict:
    return {"btType": "BTEnumOptionVisibilityForList-1613", "controlledOptions": options, "condition": condition}


def _products(combinations: Combinations, order: Sequence[str]) -> list[Product]:
    """Products matching exactly the combinations, factored by input in `order`: grouped by their first input's
    value, and values whose rest is the same share their products. `[{}]` when every combination is."""
    ids = [id for id in order if any(id in combination for combination in combinations)]
    return _factor(combinations, ids)


def _factor(combinations: Combinations, ids: Sequence[str]) -> list[Product]:
    if not ids or any(not combination for combination in combinations):
        return [{}]
    first, rest = ids[0], ids[1:]
    by_value: dict[str, list[dict[str, str]]] = {}
    # Those not naming it match any of its values
    unnamed: list[Mapping[str, str]] = []
    for combination in combinations:
        if first not in combination:
            unnamed.append(combination)
            continue
        remainder = {id: value for id, value in combination.items() if id != first}
        by_value.setdefault(combination[first], []).append(remainder)
    branches: dict[str, tuple[list[str], list[Product]]] = {}
    for value, remainders in by_value.items():
        sub = _factor(remainders, rest)
        branches.setdefault(repr(sub), ([], sub))[0].append(value)
    named = [{first: values, **product} for values, sub in branches.values() for product in sub]
    return named + (_factor(unnamed, rest) if unnamed else [])


def _intersect(a: Product, b: Product) -> Product | None:
    """What both products match; None when nothing does."""
    both = dict(a)
    for id, values in b.items():
        if id in both:
            values = [value for value in both[id] if value in values]
            if not values:
                return None
        both[id] = values
    return both


def _product_condition(product: Product) -> dict:
    return {
        "btType": "BTParameterVisibilityLogical-178",
        "operation": "AND",
        "children": [
            {
                "btType": "BTParameterVisibilityLogical-178",
                "operation": "OR",
                "children": [
                    {"btType": "BTParameterVisibilityOnEqual-180", "parameterId": id, "value": value, "inArray": False}
                    for value in values
                ],
            }
            for id, values in product.items()
        ],
    }


def _configured_parameter(input: Input, namespace: str = "") -> dict:
    """Configured by its input. Onshape refuses a configured value naming the parameter, which only the whole names."""
    if input.kind == "boolean":
        values = [
            {
                "btType": "BTMConfiguredValueByBoolean-1501",
                "booleanValue": state,
                "value": {"btType": "BTMParameterBoolean-144", "parameterId": "", "value": state},
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
                "value": {**_value(input, option, namespace), "parameterId": ""},
            }
            for option in input.options
        ]
    return {
        "btType": "BTMParameterConfigured-2222",
        "parameterId": input.parameter,
        "configurationParameterId": input.id,
        "values": values,
    }


def _default_parameter(input: Input, namespace: str) -> dict:
    if input.kind == "boolean":
        return {"btType": "BTMParameterBoolean-144", "parameterId": input.parameter, "value": input.default_option == "true"}
    return _value(input, _option(input, input.default_option), namespace)


def _value(input: Input, option: Option, namespace: str = "") -> dict:
    if input.kind == "enum":
        return {
            "btType": "BTMParameterEnum-145",
            "parameterId": input.parameter,
            "namespace": namespace,
            "enumName": input.enum_name,
            "value": option.value,
        }
    if input.kind == "switch":
        return {"btType": "BTMParameterBoolean-144", "parameterId": input.parameter, "value": option.id == "true"}
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


def _check_configuration(studio: ConfiguredStudio, written: dict) -> None:
    """Fails unless Onshape kept the inputs' ids and enum names, which the feature's configured parameters and
    FRCDesign's configurations name."""
    sent = [
        (parameter["parameterId"], parameter.get("enumName"))
        for parameter in studio.configuration_body()["configurationParameters"]
    ]
    kept = [
        (parameter.get("parameterId"), parameter.get("enumName"))
        for parameter in written.get("configurationParameters", [])
    ]
    if kept != sent:
        raise ConfigureError(f"Onshape stored {studio.tab_name}'s configuration inputs as {kept}, not as sent: {sent}.")


def build(api, workspace, studio: ConfiguredStudio, feature_studio_id: str, feature_studio_microversion: str) -> Built:
    """Makes a Part Studio exactly `studio`: finds its tab by name in a workspace, or creates it; writes its
    configuration; then inserts its feature, from the Feature Studio in the same workspace as it is now, replacing the
    one a build inserted before, and configures it.

    The configured feature keeps the namespaces Onshape stored on insert, for the feature and each of its enums: a
    feature from a Feature Studio which re-exports another document's is stored under another namespace than it was
    inserted with, and Onshape refuses an update naming any other ("Feature does not match").

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
        written = elements.update_configuration(api, part_studio, studio.configuration_body())
    _check_configuration(studio, written)
    namespace = feature_namespace(feature_studio_id, feature_studio_microversion)
    if matches:
        with step(f"delete the {studio.feature} a build inserted before"):
            part_studios.delete_feature(api, part_studio, matches[0]["featureId"])
    # Inserted plain and configured after, so a refusal names which of the two Onshape objects to
    with step(f"insert {studio.feature}"):
        part_studios.add_feature(
            api,
            part_studio,
            {
                "btType": "BTMFeature-134",
                "featureType": studio.feature,
                "name": studio.tab_name,
                "namespace": namespace,
                "parameters": studio.default_parameters(namespace),
            },
        )
    # Its configuration, or its insert, changed its microversion
    feature_list = part_studios.get_features(api, part_studio)
    feature = next(feature for feature in feature_list["features"] if feature.get("featureType") == studio.feature)
    stored = feature.get("parameters", [])
    configured = {
        parameter["parameterId"]: parameter
        for parameter in stored_namespaces(studio.configured_parameters(feature.get("namespace", "")), stored)
    }
    parameters = [configured.pop(parameter["parameterId"], parameter) for parameter in stored]
    with step(f"configure {studio.feature}"):
        response = part_studios.update_feature(
            api,
            part_studio,
            {**feature, "parameters": [*parameters, *configured.values()]},
            feature_list["sourceMicroversion"],
        )
    status = response.get("featureState", {}).get("featureStatus", "OK")
    return Built(element_id, created, status)


def stored_namespaces(configured: list[dict], stored: list[dict]) -> list[dict]:
    """Configured parameters with each enum value in the namespace Onshape stored its parameter's enum under."""
    namespaces = {
        parameter["parameterId"]: parameter.get("namespace", "")
        for parameter in stored
        if parameter.get("btType") == "BTMParameterEnum-145"
    }

    def renamed(parameter: dict) -> dict:
        namespace = namespaces.get(parameter["parameterId"])
        if namespace is None:
            return parameter
        values = [
            {**value, "value": {**value["value"], "namespace": namespace}}
            if value["value"].get("btType") == "BTMParameterEnum-145"
            else value
            for value in parameter["values"]
        ]
        return {**parameter, "values": values}

    return [renamed(parameter) for parameter in configured]


def configuration_stages(studio: ConfiguredStudio) -> list[tuple[str, dict]]:
    """Ever larger configurations, ending with the studio's own: none, its inputs with no conditions, then each input's
    conditions added in turn, each with what it adds. An input's rules go in together: alone, one could hide its
    default, which Onshape refuses."""
    full = studio.configuration_body()["configurationParameters"]
    conditions = ("visibilityCondition", "enumOptionVisibilityConditions")
    bare = [{key: value for key, value in parameter.items() if key not in conditions} for parameter in full]
    stages: list[tuple[str, list[dict]]] = [("an empty configuration", []), ("the inputs, with no conditions", bare)]
    for index, parameter in enumerate(full):
        rules = parameter.get("enumOptionVisibilityConditions", {}).get("visibilityConditions", [])
        if parameter != bare[index]:
            described = f"{len(rules)} option rules" if len(rules) > 1 else "option rule" if rules else "visibility condition"
            stages.append((f"{parameter['parameterId']}'s {described}", [*full[: index + 1], *bare[index + 1 :]]))
    return [
        (what, {"btType": "BTConfigurationUpdateCall-2933", "configurationParameters": parameters})
        for what, parameters in stages
    ]


@dataclasses.dataclass
class Diagnosis:
    """What `diagnose` found.

    Attributes:
        element_id: The scratch tab it wrote to.
        accepted: The stages Onshape accepted, in order.
        refused: The first stage Onshape refused, and its error; None when it accepted them all.
        features: The feature types in the tab the studio is built in, if there is one.
    """

    element_id: str
    accepted: list[str]
    refused: tuple[str, str] | None
    features: list[str] | None


DIAGNOSE_SUFFIX = " (DIAGNOSE)"


def diagnose(api, workspace, studio: ConfiguredStudio, built_tab: str) -> Diagnosis:
    """Writes `configuration_stages` in turn to a scratch Part Studio, `<tab> (DIAGNOSE)`, found or created in a
    workspace, until Onshape refuses one: which part of the configuration it refuses. Also lists the features in
    `built_tab`, the tab the studio is built in."""
    from onshape_api.endpoints import documents, elements, part_studios
    from onshape_api.exceptions import ApiError
    from onshape_api.paths.paths import ElementPath
    from onshape_api.types import ElementType

    tabs = documents.get_document_elements(api, workspace, ElementType.PART_STUDIO)
    built = next((element for element in tabs if element["name"] == built_tab), None)
    features = None
    if built is not None:
        listed = part_studios.get_features(api, ElementPath.from_path(workspace, built["id"]))["features"]
        features = [feature.get("featureType", feature.get("btType", "?")) for feature in listed]
    name = studio.tab_name + DIAGNOSE_SUFFIX
    scratch = next((element for element in tabs if element["name"] == name), None)
    element_id = scratch["id"] if scratch else part_studios.create_part_studio(api, workspace, name)["id"]
    part_studio = ElementPath.from_path(workspace, element_id)
    accepted: list[str] = []
    for what, body in configuration_stages(studio):
        try:
            elements.update_configuration(api, part_studio, body)
        except ApiError as error:
            return Diagnosis(element_id, accepted, (what, str(error)), features)
        accepted.append(what)
    return Diagnosis(element_id, accepted, None, features)
