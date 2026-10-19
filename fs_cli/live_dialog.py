"""A feature's dialog, for a page to draw as it's used (`fs audit`): its precondition, compiled to a tree of its groups,
parameters, and ifs, whose conditions are JavaScript, and each parameter's HTML for each of its values. The page's
script (`RUNTIME`) walks the tree with the current settings and lays the dialog out as `fs_cli.ui.Renderer` does, so
any combination of choices works, with nothing rendered ahead of time but the parameters themselves.

Conditions are compiled as `fs_cli.ui.Evaluator` evaluates them: what doesn't depend on the definition is evaluated
here, and what does becomes JavaScript, with the same rules for what's unknown (like a function other than a
predicate), so the page shows what `fs ui --set` would. A test checks the two agree, state by state.
"""

from __future__ import annotations

import copy
import html
import json
import pathlib
from typing import Any

from fs_cli.ui import (
    BUTTONS,
    UNKNOWN,
    VALUE_CHECKS,
    Declarations,
    Definition,
    DialogBuilder,
    EnumValue,
    Evaluator,
    Node,
    Parameter,
    Renderer,
    _attribute,
    _is_button,
    _tooltip,
    feature_title,
    load_feature,
)

# Where a typed value goes in its parameter's HTML
VALUE_TOKEN = "@@fs-value@@"


class JsVar:
    """A predicate's parameter whose argument depends on the definition: a JavaScript variable."""

    def __init__(self, name: str) -> None:
        self.name = name


class DialogCompiler:
    def __init__(self, declarations: Declarations, builder: DialogBuilder) -> None:
        self.declarations = declarations
        self.builder = builder
        self.evaluator: Evaluator = builder.evaluator
        self.conditions: list[str] = []
        self.variables = 0

    # The dialog's structure

    def walk(self, node: Node, scope: dict[str, Any], out: list, annotation: dict) -> None:
        """As `DialogBuilder.walk` walks a precondition, but down both branches of each if, keeping its condition."""
        kind = node.kind
        if kind == "block":
            for statement in node.args:
                self.walk(statement, scope, out, {})
        elif kind == "annotated":
            value = self.evaluator.value(node.args[0], scope)
            annotation = value if isinstance(value, dict) else {}
            inner = node.args[1]
            if "Group Name" in annotation and inner.kind == "block":
                group = self.group(annotation)
                out.append(group)
                self.walk(inner, scope, group["c"], {})
            else:
                self.walk(inner, scope, out, annotation)
        elif kind == "if":
            index = len(self.conditions)
            self.conditions.append(self.js(node.args[0], scope))
            then: list = []
            otherwise: list = []
            self.walk(node.args[1], scope, then, {})
            if node.args[2] is not None:
                self.walk(node.args[2], scope, otherwise, {})
            out.append({"t": "if", "c": index, "a": then, "b": otherwise, "dg": self.driving_groups(node.args[1], scope)})
        elif kind == "declare":
            name, value = node.args
            scope[name] = self.static(value, scope)
        elif kind == "expression":
            self.expression(node.args[0], scope, out)

    def group(self, annotation: dict) -> dict:
        name = str(annotation["Group Name"])
        driving = annotation.get("Driving Parameter")
        return {
            "t": "g",
            "name": html.escape(name),
            "attr": _attribute(name),
            "drive": None if driving is None else str(driving),
            "collapsed": annotation.get("Collapsed By Default") is True,
            "c": [],
        }

    def driving_groups(self, node: Node, scope: dict[str, Any]) -> list[dict]:
        """The groups with driving parameters directly in a branch, empty: Onshape shows their headers when the branch
        isn't taken, if their driving parameters are shown (see `DialogBuilder.driving_groups`)."""
        statements = node.args if node.kind == "block" else (node,)
        groups = []
        for statement in statements:
            if statement.kind == "annotated" and statement.args[1].kind == "block":
                annotation = self.evaluator.value(statement.args[0], scope)
                if isinstance(annotation, dict) and "Group Name" in annotation and annotation.get("Driving Parameter"):
                    groups.append(self.group(annotation))
        return groups

    def expression(self, node: Node, scope: dict[str, Any], out: list) -> None:
        if node.kind == "is":
            name = self.builder.parameter_name(node.args[0], scope)
            if name is not None:
                out.append({"t": "p", "k": name})
            return
        if node.kind != "call" or node.args[0].kind != "name":
            return
        function, arguments = node.args[0].args[0], node.args[1]
        if function in VALUE_CHECKS and arguments:
            name = self.builder.parameter_name(arguments[0], scope)
            if name is not None:
                out.append({"t": "p", "k": name})
            return
        predicate = self.declarations.predicates.get(function)
        if predicate is not None:
            inner = {parameter: self.static(argument, scope) for parameter, argument in zip(predicate.parameters, arguments)}
            self.walk(predicate.body, inner, out, {})

    def static(self, node: Node, scope: dict[str, Any]) -> Any:
        """An expression's value, if it doesn't depend on the definition's values (the definition itself doesn't), or
        unknown."""
        value = self.evaluator.value(node, scope)
        if isinstance(value, Definition):
            return value
        return UNKNOWN if self.dynamic(node, scope) else value

    # Conditions

    def dynamic(self, node: Any, scope: dict[str, Any]) -> bool:
        """Whether an expression depends on the definition (or a predicate's argument which does)."""
        if isinstance(node, (list, tuple)):
            return any(self.dynamic(item, scope) for item in node)
        if not isinstance(node, Node):
            return False
        if node.kind == "name":
            value = scope.get(node.args[0])
            return isinstance(value, (JsVar, Definition))
        if node.kind in ("member", "index") and isinstance(self.evaluator.value(node.args[0], scope), Definition):
            return True
        return any(self.dynamic(arg, scope) for arg in node.args)

    def js(self, node: Node, scope: dict[str, Any], seen: frozenset[str] = frozenset()) -> str:
        """An expression, as JavaScript (see `RUNTIME`'s helpers), with what doesn't depend on the definition
        evaluated."""
        if not self.dynamic(node, scope):
            return literal(self.evaluator.value(node, scope))
        kind, args = node.kind, node.args
        if kind == "name":
            value = scope.get(args[0])
            return value.name if isinstance(value, JsVar) else "U"
        if kind in ("member", "index"):
            target = self.evaluator.value(args[0], scope)
            if isinstance(target, Definition):
                if kind == "member":
                    key = args[1]
                elif self.dynamic(args[1], scope):
                    return "U"
                else:
                    key = self.evaluator.value(args[1], scope)
                return f"v({json.dumps(target.prefix + key)})" if isinstance(key, str) else "U"
            key = json.dumps(args[1]) if kind == "member" else self.js(args[1], scope, seen)
            return f"get({self.js(args[0], scope, seen)}, {key})"
        if kind == "unary":
            operator, operand = args
            inner = self.js(operand, scope, seen)
            return {"!": f"not({inner})", "-": f"neg({inner})"}.get(operator, inner if operator == "+" else "U")
        if kind == "binary":
            operator, left, right = args
            a, b = self.js(left, scope, seen), self.js(right, scope, seen)
            if operator == "&&":
                return f"and({a}, () => {b})"
            if operator == "||":
                return f"or({a}, () => {b})"
            return f"op({json.dumps(operator)}, {a}, {b})"
        if kind == "ternary":
            condition, then, otherwise = (self.js(arg, scope, seen) for arg in args)
            return f"tern({condition}, () => {then}, () => {otherwise})"
        if kind == "as":
            return self.js(args[0], scope, seen)
        if kind == "call":
            callee, arguments = args
            name = callee.args[0] if callee.kind == "name" else None
            predicate = self.declarations.predicates.get(name) if name is not None else None
            if predicate is None or name in seen:
                return "U"
            inner: dict[str, Any] = {}
            names, values = [], []
            for parameter, argument in zip(predicate.parameters, arguments):
                if isinstance(self.evaluator.value(argument, scope), Definition):
                    inner[parameter] = self.evaluator.value(argument, scope)
                elif self.dynamic(argument, scope):
                    self.variables += 1
                    variable = JsVar(f"a{self.variables}")
                    inner[parameter] = variable
                    names.append(variable.name)
                    values.append(self.js(argument, scope, seen))
                else:
                    inner[parameter] = self.evaluator.value(argument, scope)
            body = self.statements(predicate.body, inner, seen | {name})
            return f"(({', '.join(names)}) => {body})({', '.join(values)})"
        # `is`, maps and arrays of the definition's values, and anything else: unknown, as the evaluator has them
        return "U"

    def statements(self, node: Node, scope: dict[str, Any], seen: frozenset[str]) -> str:
        """Whether a predicate's statements hold, as `Evaluator.statements_hold` decides: true, false, or unknown."""
        if node.kind == "block":
            parts = [self.statements(statement, scope, seen) for statement in node.args]
            return f"all([{', '.join(f'() => {part}' for part in parts)}])"
        if node.kind == "expression":
            return self.js(node.args[0], scope, seen)
        if node.kind == "annotated":
            return self.statements(node.args[1], scope, seen)
        if node.kind == "if":
            condition = self.js(node.args[0], scope, seen)
            then = "true" if node.args[1] is None else self.statements(node.args[1], scope, seen)
            otherwise = "true" if node.args[2] is None else self.statements(node.args[2], scope, seen)
            return f"held({condition}, () => {then}, () => {otherwise})"
        return "true"


def literal(value: Any) -> str:
    """A value the evaluator found, as JavaScript: enums as `Enum.VALUE`, and anything it can't be as unknown."""
    converted = _convert(value)
    return "U" if converted is UNKNOWN else ("undefined" if converted is None else json.dumps(converted))


def _convert(value: Any) -> Any:
    if value is None or isinstance(value, (bool, float, str)):
        return value
    if isinstance(value, int):
        return float(value)
    if isinstance(value, EnumValue):
        return f"{value.type}.{value.value}"
    if isinstance(value, dict):
        converted = {}
        for key, item in value.items():
            if not isinstance(key, str):
                return UNKNOWN
            item = _convert(item)
            if item is UNKNOWN:
                return UNKNOWN
            if item is not None:
                converted[key] = item
        return converted
    if isinstance(value, list):
        items = [_convert(item) for item in value]
        return UNKNOWN if any(item is UNKNOWN or item is None for item in items) else items
    return UNKNOWN


def lookup_tree(table: Any) -> dict | None:
    """A lookup table's levels, without its values, for the page's script: each level's name (which conditions read),
    label, default, and entries, in order (each its name and the next level, or None at the last; a list, as
    JavaScript puts number-like keys of an object first)."""
    entries = table.get("entries") if isinstance(table, dict) else None
    if not isinstance(entries, dict) or not entries:
        return None
    return {
        "name": str(table.get("name", "")),
        "label": str(table.get("displayName", table.get("name", ""))),
        "default": None if table.get("default") is None else str(table.get("default")),
        "entries": [[str(name), lookup_tree(entry)] for name, entry in entries.items()],
    }


def _setting_text(parameter: Parameter) -> str:
    """A parameter's value, as `fs ui --set` takes it."""
    if parameter.kind == "boolean":
        return "true" if parameter.value else "false"
    if parameter.kind == "lookup":
        return parameter.default
    return "" if parameter.value is None else str(parameter.value)


def parameter_data(parameter: Parameter, theme: str) -> dict:
    """What the page needs to draw a parameter: how it's laid out, its tooltip, its default (as a setting), and its
    HTML: for each value of an enum or boolean, with a token where a typed value goes, or for a query, focused (the
    first in the dialog) and not."""
    hints = parameter.hint_names
    data: dict[str, Any] = {
        "name": parameter.name,
        "attr": _attribute(parameter.name),
        "kind": parameter.kind,
        "hints": hints,
        "tip": _tooltip(parameter),
        "button": _is_button(parameter),
        "default": _setting_text(parameter),
    }

    def render(value: Any, focused: bool = True) -> str:
        item = copy.copy(parameter)
        item.value = value
        renderer = Renderer(theme)
        renderer.focused = focused
        return renderer.parameter(item)

    if parameter.kind == "enum" and parameter.enum is not None:
        data["enum"] = parameter.enum.name
        data["html"] = {value: render(value) for value in parameter.enum.values}
    elif parameter.kind == "boolean":
        data["html"] = {"true": render(True), "false": render(False)}
    elif parameter.kind in ("query", "reference"):
        data["html"] = {"focus": render(None, focused=False), "plain": render(None)}
    elif parameter.kind in ("length", "angle", "integer", "real", "string"):
        data["html"] = {"typed": render(VALUE_TOKEN)}
    elif parameter.kind == "lookup":
        tree = lookup_tree(parameter.annotation.get("Lookup Table"))
        data["tree"] = tree
        if tree is None:
            data["html"] = {"plain": render(None)}
    else:
        data["html"] = {"plain": render(parameter.value)}
    return data


def dialog_data(
    project, std_dir: pathlib.Path, path: pathlib.Path, feature_name: str | None, theme: str = "dark"
) -> tuple[dict, str]:
    """A feature's dialog, for `RUNTIME`: its title, structure (the tree), parameters (by key), and the JavaScript of
    its conditions (`const CONDITIONS = [...]`)."""
    declarations, feature, feature_name = load_feature(project, std_dir, path, feature_name)
    builder = DialogBuilder(declarations, {})
    builder.build(feature)
    compiler = DialogCompiler(declarations, builder)
    tree: list = []
    compiler.walk(feature.precondition, {"definition": Definition({})}, tree, {})
    parameters = {
        key: parameter_data(parameter, theme) for key, parameter in builder.parameters.items() if "." not in key
    }
    for key, set_by in set_by_trees(path, builder).items():
        if key in parameters:
            parameters[key]["setBy"] = set_by
    data = {"title": feature_title(builder.evaluator, feature, feature_name), "tree": tree, "parameters": parameters}
    conditions = "const CONDITIONS = [\n" + "".join(f"  (v) => {condition},\n" for condition in compiler.conditions) + "];\n"
    return data, conditions


def set_by_trees(path: pathlib.Path, builder: DialogBuilder) -> dict[str, dict]:
    """The settings of the parameters editing logic sets from a function of the definition, by what it reads (see
    `fs_cli.set_by.SetBy.tree`), for the page's `setting`. Those it can't work out are left out (keeping their
    defaults)."""
    if not any("Set By" in parameter.annotation for parameter in builder.parameters.values()):
        return {}
    from fs_cli.set_by import SetBy

    set_by = SetBy(path, builder)
    trees = {}
    for key in set_by.parameters:
        try:
            tree = set_by.tree(key)
        except Exception:  # noqa: BLE001 - the feature's file doesn't load in the evaluator
            continue
        if tree is not None:
            trees[key] = tree
    return trees


def buttons_json() -> str:
    return json.dumps(sorted(BUTTONS))


# The page's dialog: helpers for conditions (mirroring fs_cli.ui.Evaluator), the walk of the tree, and the layout
# (mirroring fs_cli.ui.Renderer). `DIALOG` (see `dialog_data`) and `CONDITIONS` come before it. `renderList(settings)`
# is the HTML of the dialog's parameters for some settings (as `fs ui --set` takes them).
RUNTIME = r"""
const U = { unknown: true };
const BUTTON_HINTS = new Set(%BUTTONS%);

function truthy(value) {
  if (value === undefined || value === null || value === false || value === 0 || value === "") {
    return false;
  }
  if (Array.isArray(value)) {
    return value.length > 0;
  }
  if (typeof value === "object") {
    return Object.keys(value).length > 0;
  }
  return true;
}

function equal(a, b) {
  if (a === b) {
    return true;
  }
  if (a && b && typeof a === "object" && typeof b === "object") {
    const keys = Object.keys(a);
    return keys.length === Object.keys(b).length && keys.every((key) => key in b && equal(a[key], b[key]));
  }
  return false;
}

function text(value) {
  return typeof value === "number" ? String(Number(value.toPrecision(6))) : String(value);
}

function get(target, key) {
  if (target === U || key === U || target === null || typeof target !== "object") {
    return U;
  }
  if (Array.isArray(target)) {
    return typeof key === "number" && key >= 0 && key < target.length ? target[Math.floor(key)] : U;
  }
  return target[key];
}

function not(value) {
  return value === U ? U : !truthy(value);
}

function neg(value) {
  return typeof value === "number" ? -value : U;
}

function and(first, rest) {
  return first === U ? U : !truthy(first) ? false : rest();
}

function or(first, rest) {
  return first === U ? U : truthy(first) ? true : rest();
}

function tern(condition, then, otherwise) {
  return condition === U ? U : truthy(condition) ? then() : otherwise();
}

function op(operator, a, b) {
  if (a === U || b === U) {
    return U;
  }
  if (operator === "==") {
    return equal(a, b);
  }
  if (operator === "!=") {
    return !equal(a, b);
  }
  if (operator === "~") {
    return text(a) + text(b);
  }
  if (typeof a !== "number" || typeof b !== "number") {
    return U;
  }
  switch (operator) {
    case "+": return a + b;
    case "-": return a - b;
    case "*": return a * b;
    case "/": return b ? a / b : U;
    case "%": return b ? a % b : U;
    case "^": return a ** b;
    case "<": return a < b;
    case ">": return a > b;
    case "<=": return a <= b;
    case ">=": return a >= b;
  }
  return U;
}

function all(statements) {
  let result = true;
  for (const statement of statements) {
    const held = statement();
    if (held === false) {
      return false;
    }
    if (held === U) {
      result = U;
    }
  }
  return result;
}

function held(condition, then, otherwise) {
  return condition === U ? U : truthy(condition) ? then() : otherwise();
}

function escapeHtml(value) {
  return String(value).replace(/[&<>'"]/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#x27;", '"': "&quot;" })[c]);
}

// A lookup table parameter's levels, following its setting's choices, then defaults: each its node and choice
function lookupLevels(tree, path) {
  const choices = path ? path.split(" > ") : [];
  const levels = [];
  let node = tree;
  while (node) {
    const names = node.entries.map(([name]) => name);
    let choice = choices[levels.length] ?? node.default;
    if (!names.includes(choice)) {
      choice = names[0];
    }
    levels.push([node, choice]);
    node = node.entries[names.indexOf(choice)][1];
  }
  return levels;
}

let SETTINGS = {};

function setting(key) {
  if (key in SETTINGS) {
    return SETTINGS[key];
  }
  const parameter = DIALOG.parameters[key];
  const set = parameter.setBy ? setByValue(parameter.setBy) : undefined;
  return set === undefined || set === null ? parameter.default : set;
}

// A parameter's setting editing logic would set from its "Set By" function (see fs_cli.set_by.SetBy.tree): following
// the tree by the settings of the parameters it read
function setByValue(node) {
  while (node && node.k !== undefined) {
    const parameter = DIALOG.parameters[node.k];
    let value = parameter ? setting(node.k) : undefined;
    if (parameter && parameter.kind === "lookup" && parameter.tree) {
      value = lookupLevels(parameter.tree, value).map(([, choice]) => choice).join(" > ");
    }
    node = node.b[value];
  }
  return node ? node.v : undefined;
}

// A parameter's value, as conditions read it (as fs_cli.ui.DialogBuilder.declare sets the definition's)
function v(key) {
  const parameter = DIALOG.parameters[key];
  if (!parameter) {
    return undefined;
  }
  const value = setting(key);
  if (parameter.kind === "enum") {
    return parameter.enum + "." + value;
  }
  if (parameter.kind === "boolean") {
    return value === "true";
  }
  if (parameter.kind === "lookup" && parameter.tree) {
    return Object.fromEntries(lookupLevels(parameter.tree, value).map(([node, choice]) => [node.name, choice]));
  }
  return undefined;
}

// The parameters and groups shown, by walking the tree
function shownItems(nodes, out, shown) {
  for (const node of nodes) {
    if (node.t === "p") {
      if (DIALOG.parameters[node.k] && !shown.has(node.k)) {
        shown.add(node.k);
        out.push(node);
      }
    } else if (node.t === "g") {
      const children = [];
      out.push({ ...node, c: children });
      shownItems(node.c, children, shown);
    } else {
      let condition = CONDITIONS[node.c](v);
      if (condition === U) {
        condition = true;
      }
      shownItems(truthy(condition) ? node.a : node.b, out, shown);
      if (!truthy(condition)) {
        for (const group of node.dg) {
          if (shown.has(group.drive)) {
            out.push({ ...group, c: [] });
          }
        }
      }
    }
  }
}

// Laid out as fs_cli.ui.Renderer lays out a dialog
let focused = false;

function sprite(name, attributes) {
  return `<svg${attributes}><use href='#svg-icon-${name}'></use></svg>`;
}

function meta(item) {
  return DIALOG.parameters[item.k];
}

function hasHint(item, hint) {
  return meta(item).hints.includes(hint);
}

function isButton(item) {
  return meta(item).button;
}

function isCheckbox(item) {
  return meta(item).kind === "boolean" && !isButton(item);
}

function drivingParameters(items) {
  const driving = new Set(items.filter((item) => item.t === "g").map((group) => group.drive));
  const found = new Map();
  for (const item of items) {
    if (item.t === "p" && driving.has(meta(item).name)) {
      found.set(meta(item).name, item);
    }
  }
  return found;
}

function lookupHtml(item) {
  const parameter = meta(item);
  const levels = lookupLevels(parameter.tree, setting(item.k));
  const rows = levels.map(([node, choice], index) => {
    const above = levels.slice(0, index).map(([, chosen]) => chosen);
    const options = node.entries.map(([option]) =>
      `<div class='os-select-choices-row' data-set='${escapeHtml(item.k)}' data-value='${escapeHtml([...above, option].join(" > "))}'>${escapeHtml(option)}</div>`).join("");
    const select = `<div class='os-select-container os-select-bootstrap dropdown' data-parameter-id='${escapeHtml(node.label)}'>` +
      "<div class='os-select-match'><span class='btn btn-secondary form-control os-select-toggle' style='outline: 0;'>" +
      `<span class='os-select-match-text float-start'><span>${escapeHtml(choice)}</span></span><i class='caret float-end'></i>` +
      "</span></div><span class='os-spinner-small os-spinner-spinning ng-hide'></span>" +
      "<input type='search' class='form-control os-select-search ng-hide'>" +
      `<ul class='os-select-choices os-select-choices-content os-select-dropdown dropdown-menu ng-hide'><li class='os-select-choices-group'>${options}</li></ul>` +
      "<div class='os-select-no-choice'></div><os-select-single></os-select-single>" +
      "<input class='os-select-focusser os-select-offscreen' type='text' tabindex='-1'></div>";
    return "<tr class='os-param-lookup-table-selector-container'>" +
      `<td class='os-param-table-label'><span>${escapeHtml(node.label)}</span></td>` +
      `<td class='os-param-table-value'><div class='os-param-lookup-table-selector'>${select}</div></td></tr>`;
  });
  return "<os-lookup-table-parameter data-parameter-type='os-lookup-table-parameter'>" +
    `<table class='os-param-lookup-table' data-parameter-id='${parameter.attr}'><tbody>${rows.join("")}</tbody></table>` +
    "</os-lookup-table-parameter>";
}

function parameterHtml(item) {
  const parameter = meta(item);
  const value = setting(item.k);
  if (parameter.kind === "lookup" && parameter.tree) {
    return lookupHtml(item);
  }
  if (parameter.kind === "query" || parameter.kind === "reference") {
    const html = focused ? parameter.html.plain : parameter.html.focus;
    focused = true;
    return html;
  }
  if (parameter.html.typed !== undefined) {
    return parameter.html.typed.split("%TOKEN%").join(escapeHtml(value));
  }
  return parameter.html[value] ?? parameter.html[parameter.default] ?? parameter.html.plain;
}

function listItem(item, following, inline) {
  const parameter = meta(item);
  const classes = ["os-parameter-list-item"];
  const kind = parameter.kind;
  const fillsBoth = ["query", "reference", "lookup", "array", "button"].includes(kind) ||
    (kind === "enum" && hasHint(item, "HORIZONTAL_ENUM"));
  if (inline) {
    // A button in a row of buttons
  } else if (isButton(item)) {
    classes.push("os-param-fits-in-right-column");
  } else if (hasHint(item, "DISPLAY_SHORT")) {
    classes.push("os-param-display-short");
  } else {
    const beside = following && isButton(following) && !hasHint(following, "FIRST_IN_ROW");
    if (!fillsBoth || beside) {
      classes.push("os-param-fill-first-column");
    }
    if (fillsBoth) {
      classes.push("os-param-fill-both-columns");
    }
  }
  if (["query", "reference", "array"].includes(kind)) {
    classes.push("os-param-requires-margin");
  }
  return `<div class='${classes.join(" ")}' data-parameter-id='${parameter.attr}'${parameter.tip}>${parameterHtml(item)}</div>`;
}

function parametersHtml(items) {
  const pieces = [];
  let index = 0;
  while (index < items.length) {
    const item = items[index];
    if (isButton(item) && hasHint(item, "FIRST_IN_ROW")) {
      let end = index + 1;
      while (end < items.length && isButton(items[end]) && !hasHint(items[end], "FIRST_IN_ROW")) {
        end += 1;
      }
      const buttons = items.slice(index, end).map((button) => listItem(button, null, true)).join("");
      pieces.push("<div class='os-parameter-list-item os-param-fill-both-columns os-param-button-row' " +
        `style='display: flex; align-items: center; gap: 6px'>${buttons}</div>`);
      index = end;
      continue;
    }
    pieces.push(listItem(item, index + 1 < items.length ? items[index + 1] : null, false));
    index += 1;
  }
  return pieces.join("");
}

function visible(item, driving) {
  return item.t === "g" || (!driving.has(meta(item).name) && !hasHint(item, "ALWAYS_HIDDEN"));
}

function groupHtml(group, drivingParameters) {
  const driving = group.drive === null ? undefined : drivingParameters.get(group.drive);
  const enabled = driving === undefined || setting(driving.k) === "true";
  const expanded = enabled && !group.collapsed;
  const expander = "<div class='node-expander-wrapper'>" +
    `<node-expander class='os-param-group-expander${enabled ? "" : " node-expander-disabled"}'${enabled ? " data-toggle" : ""}>` +
    `<div class='os-center-content'>${sprite("collapsed", ` class="os-svg-icon${expanded ? " expanded" : ""}"`)}</div>` +
    "</node-expander></div>";
  const name = driving === undefined
    ? `<span class='os-param-group-name'>${group.name}</span>`
    : `<div class='os-param-group-driving-parameter os-parameter-list-item' data-parameter-id='${meta(driving).attr}'${meta(driving).tip}>` +
      `${meta(driving).html[setting(driving.k)] ?? meta(driving).html[meta(driving).default]}</div>`;
  const nested = drivingParameters_(group.c);
  const rows = subgroupRows(group.c.filter((item) => visible(item, nested)), nested);
  return `<div class='os-param-group-collapsible-container' data-group='${group.attr}'>` +
    `<div class='os-param-group-header' data-driving-parameter-id='${driving === undefined ? "" : meta(driving).attr}'>` +
    `${expander}${name}</div>` +
    `<div class='os-param-group-collapsible-contents${expanded ? "" : " ng-hide"}'>${rows}</div></div>`;
}

const drivingParameters_ = drivingParameters;

function subgroupRows(items, drivingParameters) {
  const rows = [];
  for (const item of items) {
    const previous = rows.length ? rows[rows.length - 1][0] : null;
    if (item.t === "g" || previous === null || previous.t === "g") {
      rows.push([item]);
      continue;
    }
    const joinsCheckbox = isCheckbox(previous) && hasHint(item, "DISPLAY_SHORT") && !hasHint(item, "FIRST_IN_ROW");
    const beside = isButton(item) && !hasHint(item, "FIRST_IN_ROW");
    if (joinsCheckbox || beside || (!isCheckbox(item) && !isCheckbox(previous))) {
      rows[rows.length - 1].push(item);
    } else {
      rows.push([item]);
    }
  }
  return rows.map((row, index) =>
    "<div class='os-param-subgroup-row'>" +
    `<div class='os-param-group-indent ${index === rows.length - 1 ? "node-indent-line-end" : "node-indent-line"}'></div>` +
    "<os-parameter-group>" +
    (row[0].t === "g" ? groupHtml(row[0], drivingParameters) : parametersHtml(row)) +
    "</os-parameter-group></div>").join("");
}

function itemsHtml(items) {
  const parts = [];
  let run = [];
  const driving = drivingParameters(items);
  const flush = () => {
    if (run.length) {
      parts.push(`<os-parameter-group>${parametersHtml(run)}</os-parameter-group>`);
      run = [];
    }
  };
  for (const item of items) {
    if (item.t === "g") {
      flush();
      parts.push(`<os-parameter-group>${groupHtml(item, driving)}</os-parameter-group>`);
    } else if (visible(item, driving)) {
      run.push(item);
    }
  }
  flush();
  return parts.join("");
}

// The dialog's parameters' HTML for `settings`, and the keys of those shown
function renderList(settings) {
  SETTINGS = settings;
  focused = false;
  const items = [];
  const shown = new Set();
  shownItems(DIALOG.tree, items, shown);
  return { html: itemsHtml(items), shown: [...shown] };
}
""".replace("%BUTTONS%", buttons_json()).replace("%TOKEN%", VALUE_TOKEN)


def page_script(data: dict, conditions: str) -> str:
    """The script defining a dialog's `DIALOG`, `CONDITIONS`, and `RUNTIME`, safe in a page's script element."""
    return (f"const DIALOG = {json.dumps(data, separators=(',', ':'))};\n{conditions}{RUNTIME}").replace("</", "<\\/")
