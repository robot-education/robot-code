from typing import Any, Iterable, override
from common import str_utils
from featurescript.base import ctxt, expr
from featurescript.core import map
from featurescript.ui import ui_hint


def format_description(description: str) -> str:
    description = description.replace("\n", '<br>" ~\n\t"')
    return description


class AnnotationMap(map.Map):
    @override
    def build(self, context: ctxt.Context) -> str:
        return "annotation " + super().build(context) + "\n"


def parameter_annotation_map(
    parameter_name: str | None = None,
    display_name: str | None = None,
    ui_hints: ui_hint.UiHint | None = None,
    description: str | None = None,
    default: str | bool | None = None,
    filter: expr.Expression | str | None = None,
    max_picks: int | None = None,
    additional_args: dict[str, Any] = {},
) -> AnnotationMap:
    """Defines a generic annotation map.
    Args:
        parameter_name: The name of the parameter. If specified and display_name is `None`, it is automatically converted to `display_name`.
        display_name: The user facing name of the annotation.
        additional_args: A dict containing additional key-value pairs to add to the annotation map.
    """
    map_args = {}
    excluded_values = ["UIHint"]
    if display_name != None:
        map_args["Name"] = display_name
    elif parameter_name != None:
        map_args["Name"] = str_utils.display_name(parameter_name)

    if default != None:
        if isinstance(default, bool):
            map_args["Default"] = "true" if default else "false"
            excluded_values.append("Default")
        else:
            map_args["Default"] = default

    if ui_hints != None:
        names = [str_utils.quote(ui_hint.name or "") for ui_hint in ui_hints]
        map_args["UIHint"] = "[{}]".format(", ".join(names))

    if filter != None:
        map_args["Filter"] = filter
        excluded_values.append("Filter")

    if max_picks != None:
        map_args["MaxNumberOfPicks"] = max_picks
        excluded_values.append("MaxNumberOfPicks")

    map_args.update(additional_args)
    # map_args.update((key, expr.cast_to_expr(value)) for key, value in additional_args)

    # description last
    if description != None:
        map_args["Description"] = format_description(description)

    return AnnotationMap(map_args, quote_values=True, excluded_values=excluded_values)


def feature_annotation_map(
    name: str,
    display_name: str | None = None,
    no_preview_provided: bool = False,
    description: str | None = None,
    manipulator_change_function: str | None = None,
    editing_logic_function: str | None = None,
    icon: str | None = None,
    description_image: str | None = None,
    tooltip_template: str | None = None,
    name_template: str | None = None,
    filter_selector: Iterable[str] = (),
) -> AnnotationMap:
    """The annotation map for a feature."""
    map_args = {"Feature Type Name": display_name or str_utils.display_name(name)}
    if no_preview_provided:
        map_args["UIHint"] = "NO_PREVIEW_PROVIDED"

    if filter_selector:
        map_args["Filter Selector"] = "[" + ", ".join(filter_selector) + "]"

    function_args = locals()  # use locals to access function args programatically
    for name in [
        "manipulator_change_function",
        "editing_logic_function",
        "icon",
        "description_image",
        "tooltip_template",
        "name_template",
    ]:
        if function_args[name] is None:
            continue
        key = " ".join(string.capitalize() for string in name.split("_"))
        map_args[key] = function_args[name]

    if description:
        map_args["Feature Type Description"] = format_description(description)

    return AnnotationMap(map_args, quote_values=True, inline=False)
