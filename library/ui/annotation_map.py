import dataclasses
from typing import Iterable
from typing_extensions import override
from library.base import ctxt, expr, node, str_utils
from library.core import map
from library.ui import ui_hint


def format_description(description: str) -> str:
    description = description.replace("\n", '<br>" ~\n')
    return description


class AnnotationMap(map.Map):
    @override
    def build(self, context: ctxt.Context) -> str:
        return "annotation " + super().build(context) + "\n"


def parameter_annotation_map(
    parameter_name: str | None = None,
    user_name: str | None = None,
    ui_hints: ui_hint.UiHint | None = None,
    description: str | None = None,
    default: str | bool | None = None,
    additional_args: dict[str, str] = {},
) -> AnnotationMap:
    """Defines a generic annotation map.
    Args:
        parameter_name: The name of the parameter. If specified and user_name is `None`, it is automatically converted to `user_name`.
        user_name: The user facing name of the annotation.
        additional_args: A dict containing additional key-value pairs to add to the annotation map.
    """
    map_args = {}
    excluded_values = ["UIHint"]
    if user_name != None:
        map_args["Name"] = user_name
    elif parameter_name != None:
        map_args["Name"] = str_utils.user_name(parameter_name)

    if default != None:
        if isinstance(default, bool):
            map_args["Default"] = "true" if default else "false"
            excluded_values.append("Default")
        else:
            map_args["Default"] = default

    if ui_hints != None:
        names = [str_utils.quote(ui_hint.name or "") for ui_hint in ui_hints]
        map_args["UIHint"] = "[{}]".format(", ".join(names))

    map_args.update(additional_args)
    if description != None:
        map_args["Description"] = format_description(description)

    return AnnotationMap(map_args, quote_values=True, excluded_values=excluded_values)


# Use a class to define
def feature_annotation_map(
    name: str,
    user_name: str | None = None,
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
    map_args = {"Feature Type Name": user_name or str_utils.user_name(name)}
    if no_preview_provided:
        map_args["UIHint"] = "NO_PREVIEW_PROVIDED"

    if filter_selector:
        map_args["Filter Selector"] = "[" + ", ".join(filter_selector) + "]"

    function_args = locals()
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
