"""Part Studio configurations, as Onshape stores them, for previews: a part set's feature is used through its Part
Studios' configurations (see `fs_cli.configured_studios`), so its preview shows those, not the feature's own dialog.

`parse_configuration` reads Onshape's JSON, the body `ConfiguredStudio.configuration_body` writes or the configuration
`fs configuration` reads back, into a `Configuration`: its inputs, each List's options and the rules showing them, and
each input's own condition. `Configuration.state` works out what shows for some settings, as Onshape's configuration
panel would, and `configuration_page` renders it in Onshape's dialog styles, as `fs ui` renders a feature, for VS Code's
preview; `configuration_script` gives `fs audit`'s page the same, drawn by its script (`RUNTIME`, which mirrors
`state`).
"""

from __future__ import annotations

import dataclasses
import html
import json
import pathlib
import runpy
from typing import Any, Mapping, Sequence

from fs_cli.ui import Renderer, _setting

# Where a dialog's parameters are, in a page `fs_cli.ui.Renderer` renders
LIST_START = "<os-parameter-list-view>"
LIST_END = "</os-parameter-list-view>"


class ConfigurationError(Exception):
    pass


# A condition: ("and" | "or", [conditions]), ("equal", input id, value), or ("always",)
Condition = tuple


@dataclasses.dataclass(frozen=True)
class ConfigurationInput:
    """A configuration input: a List (`options`, each its id and name, shown by `rules`), or a checkbox.

    Attributes:
        rules: Each a condition and the options it shows: an option shows while a rule controlling it holds. With no
            rules, every option shows.
        visibility: The input's own condition; always shown when None.
    """

    id: str
    name: str
    kind: str
    default: str
    options: tuple[tuple[str, str], ...] = ()
    rules: tuple[tuple[Condition, frozenset[str]], ...] = ()
    visibility: Condition | None = None


@dataclasses.dataclass(frozen=True)
class ShownInput:
    """An input as the panel shows it: the options it offers, and the one chosen."""

    input: ConfigurationInput
    options: tuple[tuple[str, str], ...]
    value: str


@dataclasses.dataclass(frozen=True)
class Configuration:
    inputs: tuple[ConfigurationInput, ...]

    def values(self, settings: Mapping[str, str]) -> dict[str, str]:
        """Every input's value for `settings` (by input id): what's set, else its default, as shown (see `state`)."""
        values: dict[str, str] = {}
        for input in self.inputs:
            value = settings.get(input.id, input.default)
            if input.kind == "list":
                shown = self.shown_options(input, values)
                # Onshape doesn't say what a panel shows when its choice is hidden: the preview takes the first shown
                if shown and value not in [id for id, _ in shown]:
                    value = shown[0][0]
            values[input.id] = value
        return values

    def shown_options(self, input: ConfigurationInput, values: Mapping[str, str]) -> tuple[tuple[str, str], ...]:
        if not input.rules:
            return input.options
        shown = {id for condition, controlled in input.rules if holds(condition, values) for id in controlled}
        return tuple(option for option in input.options if option[0] in shown)

    def state(self, settings: Mapping[str, str]) -> list[ShownInput]:
        """The inputs the panel shows for `settings`, in order: a List with none of its options shown is hidden, as is
        an input whose condition doesn't hold. A hidden input keeps its value, which later conditions read."""
        values = self.values(settings)
        shown = []
        for input in self.inputs:
            if input.visibility is not None and not holds(input.visibility, values):
                continue
            options = self.shown_options(input, values) if input.kind == "list" else ()
            if input.kind == "list" and not options:
                continue
            shown.append(ShownInput(input, options, values[input.id]))
        return shown

    def defaults(self) -> dict[str, str]:
        return {input.id: input.default for input in self.inputs}

    def data(self) -> dict:
        """The configuration as JSON for `RUNTIME`: each input's id, name, kind, default, options, rules, and
        visibility."""
        return {
            "inputs": [
                {
                    "id": input.id,
                    "name": input.name,
                    "kind": input.kind,
                    "default": input.default,
                    "options": [list(option) for option in input.options],
                    "rules": [[_condition_data(condition), sorted(controlled)] for condition, controlled in input.rules],
                    "visibility": None if input.visibility is None else _condition_data(input.visibility),
                }
                for input in self.inputs
            ]
        }


def holds(condition: Condition, values: Mapping[str, str]) -> bool:
    kind = condition[0]
    if kind == "always":
        return True
    if kind == "equal":
        return values.get(condition[1]) == condition[2]
    children = [holds(child, values) for child in condition[1]]
    return all(children) if kind == "and" else any(children)


def _condition_data(condition: Condition) -> list:
    if condition[0] in ("and", "or"):
        return [condition[0], [_condition_data(child) for child in condition[1]]]
    return list(condition)


# Parsing Onshape's JSON


def parse_condition(data: Mapping[str, Any]) -> Condition:
    """A visibility condition, as Onshape stores it: logical ANDs and ORs, equals, and always shown."""
    kind = str(data.get("btType", "")).split("-")[0]
    if kind == "BTParameterVisibilityLogical":
        operation = str(data.get("operation", "")).lower()
        if operation not in ("and", "or"):
            raise ConfigurationError(f"A condition's operation is {data.get('operation')!r}, not AND or OR.")
        return (operation, tuple(parse_condition(child) for child in data.get("children", [])))
    if kind == "BTParameterVisibilityOnEqual":
        return ("equal", str(data["parameterId"]), _text(data["value"]))
    if kind == "BTParameterVisibilityAlwaysShown":
        return ("always",)
    raise ConfigurationError(f"A condition is a {data.get('btType')}, which the preview can't read.")


def parse_configuration(data: Mapping[str, Any]) -> Configuration:
    """A configuration from Onshape's JSON: an object with `configurationParameters`, as Onshape answers for an
    element's configuration and takes to set one."""
    inputs = []
    for parameter in data.get("configurationParameters", []):
        kind = str(parameter.get("btType", "")).split("-")[0]
        visibility = parameter.get("visibilityCondition")
        common = {
            "id": str(parameter["parameterId"]),
            "name": str(parameter.get("parameterName", parameter["parameterId"])),
            "visibility": None if not visibility else parse_condition(visibility),
        }
        if kind == "BTMConfigurationParameterEnum":
            options = tuple((str(option["option"]), str(option.get("optionName", option["option"]))) for option in parameter.get("options", []))
            conditions = parameter.get("enumOptionVisibilityConditions") or {}
            rules = tuple(
                (parse_condition(rule["condition"]), frozenset(str(id) for id in rule.get("controlledOptions", [])))
                for rule in conditions.get("visibilityConditions", [])
            )
            inputs.append(ConfigurationInput(kind="list", default=str(parameter.get("defaultValue", options[0][0] if options else "")),
                                             options=options, rules=rules, **common))
        elif kind == "BTMConfigurationParameterBoolean":
            inputs.append(ConfigurationInput(kind="boolean", default=_text(parameter.get("defaultValue", False)), **common))
        else:
            raise ConfigurationError(f"{common['name']} is a {parameter.get('btType')} input, which the preview can't show.")
    return Configuration(tuple(inputs))


def _text(value: Any) -> str:
    """A value as the panel's settings hold it: booleans as `true` and `false`."""
    return ("true" if value else "false") if isinstance(value, bool) else str(value)


# Rendering, in Onshape's dialog styles


def inputs_html(shown: Sequence[ShownInput], theme: str = "dark") -> str:
    """The panel's inputs, a row each, as the dialog's parameter list: a List's dropdown of its shown options, or a
    checkbox, each setting its input (`data-set`) when clicked."""
    renderer = Renderer(theme)
    rows = []
    for item in shown:
        input = item.input
        if input.kind == "boolean":
            control = _checkbox(input.id, item.value == "true")
        else:
            names = dict(item.options)
            control = renderer.select(input.name, names.get(item.value, item.value), [(name, _setting(input.id, id)) for id, name in item.options])
        rows.append(_row(html.escape(input.name), control))
    return _table("".join(rows))


def _row(name: str, control: str) -> str:
    return (
        "<tr class='os-param-lookup-table-selector-container'>"
        f"<td class='os-param-table-label'><span>{name}</span></td>"
        f"<td class='os-param-table-value'><div class='os-param-lookup-table-selector'>{control}</div></td></tr>"
    )


def _checkbox(id: str, on: bool) -> str:
    return (
        f"<label class='os-param-checkbox'{_setting(id, 'false' if on else 'true')}>"
        f"<input type='checkbox' class='os-param-checkbox-input'{' checked' if on else ''}>"
        "<span class='os-checkbox-indicator'></span></label>"
    )


def _table(rows: str) -> str:
    return (
        "<os-parameter-group><div class='os-parameter-list-item os-param-fill-both-columns' data-parameter-id='configuration'>"
        "<os-lookup-table-parameter data-parameter-type='os-lookup-table-parameter'>"
        f"<table class='os-param-lookup-table'><tbody>{rows}</tbody></table>"
        "</os-lookup-table-parameter></div></os-parameter-group>"
    )


def configuration_page(title: str, configuration: Configuration, settings: Mapping[str, str], theme: str = "dark") -> str:
    """The panel for `settings`, as a page in Onshape's dialog styles, as `fs ui` renders a feature's dialog."""
    page = Renderer(theme).page(title, [])
    start = page.index(LIST_START) + len(LIST_START)
    return page[:start] + inputs_html(configuration.state(settings), theme) + page[page.index(LIST_END, start):]


def configuration_script(configuration: Configuration, theme: str = "dark") -> str:
    """The script drawing the panel on `fs audit`'s page: `DIALOG` (each input's default, by id, for the page's glue),
    `CONFIGURATION`, a dropdown's and a checkbox's HTML to fill in, and `RUNTIME`, safe in a page's script element."""
    renderer = Renderer(theme)
    templates = {
        "table": _table("%ROWS%"),
        "row": _row("%NAME%", "%CONTROL%"),
        "select": renderer.select("%NAME%", "%CHOICE%", [("%OPTION%", _setting("%ID%", "%VALUE%"))]),
        "checkbox": {"true": _checkbox("%ID%", True), "false": _checkbox("%ID%", False)},
    }
    dialog = {"parameters": {input.id: {"default": input.default} for input in configuration.inputs}}
    return (
        f"const DIALOG = {json.dumps(dialog)};\n"
        f"const CONFIGURATION = {json.dumps(configuration.data(), separators=(',', ':'))};\n"
        f"const TEMPLATES = {json.dumps(templates)};\n{RUNTIME}"
    ).replace("</", "<\\/")


# The panel, drawn by `fs audit`'s page: `CONFIGURATION` (see `Configuration.data`) and `TEMPLATES` come before it.
# `renderList(settings)` is the HTML of the panel's inputs for some settings, as `inputs_html` writes it for
# `Configuration.state`.
RUNTIME = r"""
function holds(condition, values) {
  const [kind, ...rest] = condition;
  if (kind === "always") {
    return true;
  }
  if (kind === "equal") {
    return values[rest[0]] === rest[1];
  }
  const children = rest[0].map((child) => holds(child, values));
  return kind === "and" ? children.every(Boolean) : children.some(Boolean);
}

function shownOptions(input, values) {
  if (input.rules.length === 0) {
    return input.options;
  }
  const shown = new Set(input.rules.filter(([condition]) => holds(condition, values)).flatMap(([, controlled]) => controlled));
  return input.options.filter(([id]) => shown.has(id));
}

function escapeHtml(text) {
  // [<] rather than <, which a page's script element mustn't hold before a slash
  return String(text).replace(/&/g, "&amp;").replace(/[<]/g, "&lt;").replace(/>/g, "&gt;").replace(/'/g, "&#x27;").replace(/"/g, "&quot;");
}

// A template with its tokens' values, escaped, put in
function fill(template, values) {
  return Object.entries(values).reduce((html, [token, value]) => html.split(token).join(escapeHtml(value)), template);
}

// Every input's value: what's set, else its default, and a List's first shown option where its choice is hidden
function inputValues(settings) {
  const values = {};
  for (const input of CONFIGURATION.inputs) {
    let value = settings[input.id] ?? input.default;
    if (input.kind === "list") {
      const shown = shownOptions(input, values);
      if (shown.length && !shown.some(([id]) => id === value)) {
        value = shown[0][0];
      }
    }
    values[input.id] = value;
  }
  return values;
}

function renderList(settings) {
  const values = inputValues(settings);
  const option = TEMPLATES.select.match(/<div class='os-select-choices-row'[^>]*>[^<]*<\/div>/)[0];
  const rows = [];
  for (const input of CONFIGURATION.inputs) {
    if (input.visibility && !holds(input.visibility, values)) {
      continue;
    }
    let control;
    if (input.kind === "boolean") {
      control = fill(TEMPLATES.checkbox[values[input.id] === "true" ? "true" : "false"], { "%ID%": input.id });
    } else {
      const options = shownOptions(input, values);
      if (!options.length) {
        continue;
      }
      const names = Object.fromEntries(options);
      const choices = options.map(([id, name]) => fill(option, { "%ID%": input.id, "%VALUE%": id, "%OPTION%": name })).join("");
      control = fill(TEMPLATES.select, { "%NAME%": input.name, "%CHOICE%": names[values[input.id]] ?? values[input.id] })
        .replace(fill(option, {}), choices);
    }
    rows.push(fill(TEMPLATES.row, { "%NAME%": input.name }).replace("%CONTROL%", control));
  }
  return { html: TEMPLATES.table.replace("%ROWS%", rows.join("")) };
}
"""


# A part set's studios, for its feature's preview


@dataclasses.dataclass(frozen=True)
class StudioView:
    """A Part Studio of a part set, as a preview shows it: its label (its part set's name and tab), and its
    configuration."""

    label: str
    tab: str
    feature: str
    configuration: Configuration
    target: str = ""  # As `fs render` takes it: SET/STUDIO


def part_set_views(config, path: pathlib.Path) -> list[StudioView]:
    """The Part Studios of the part sets whose Feature Studio `path` is, which use a feature it defines, each with its
    configuration as its definition writes it: what Onshape has once `fs build` (or a release) builds it. None for any other file."""
    from fs_cli.configured_studios import configured_studios
    from fs_cli.parts import defines

    path = path.resolve()
    code = path.read_text(encoding="utf-8")
    views = []
    for part_set in config.parts:
        folder = (config.code_dir / part_set.folder).resolve()
        if folder not in path.parents:
            continue
        definitions = [folder / part_set.definition] if part_set.definition else sorted(folder.rglob("*.py"))
        for definition in definitions:
            for studio in configured_studios(run_definition(definition)):
                if defines(code, studio.feature):
                    configuration = parse_configuration(studio.configuration_body())
                    views.append(StudioView(f"{part_set.title}: {studio.tab_name}", studio.tab_name, studio.feature, configuration,
                                           f"{part_set.name}/{studio.name}"))
    return views


# Definitions run once a version of their folder: a part set's runs its whole catalog
_RUNS: dict[tuple[pathlib.Path, float], dict] = {}


def run_definition(definition: pathlib.Path) -> dict:
    stamp = max(file.stat().st_mtime for file in definition.parent.rglob("*") if file.is_file())
    key = (definition, stamp)
    if key not in _RUNS:
        _RUNS[key] = runpy.run_path(str(definition))
    return _RUNS[key]
