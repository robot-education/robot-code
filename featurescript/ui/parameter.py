from abc import ABC
import dataclasses
from typing import Self, override
import warnings
from onshape_api.utils import str_utils
from featurescript.core import control, utils
from featurescript.base import ctxt, expr, node
from featurescript.ui import bounds, enum, ui_hint, annotation_map


@dataclasses.dataclass
class TypeParameter(node.Node, ABC):
    """Represents a UI element which is a type, such as an enum or boolean."""

    parameter_name: str
    type: str
    annotation_map: annotation_map.AnnotationMap
    parent: node.ParentNode | None = None

    def __post_init__(self) -> None:
        node.handle_parent(self, self.parent)

    @override
    def build(self, context: ctxt.Context) -> str:
        if not context.ui:
            warnings.warn("UI parameter added to non-UI predicate")
        context.scope = ctxt.Scope.EXPRESSION
        return (
            self.annotation_map.run_build(context)
            + utils.definition(self.parameter_name).run_build(context)
            + " is {};\n".format(self.type)
        )


def enum_parameter(
    enum: enum.Enum,
    parameter_name: str | None = None,
    display_name: str | None = None,
    ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
    description: str | None = None,
    default: str | None = None,
) -> TypeParameter:
    parameter_name = parameter_name or enum.default_parameter_name
    map = annotation_map.parameter_annotation_map(
        parameter_name,
        display_name,
        ui_hints,
        description,
        default,
    )
    return TypeParameter(parameter_name, enum.name, map)


def labeled_enum_parameter(
    enum: enum.Enum,
    parameter_name: str | None = None,
    display_name: str | None = None,
    ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
    default: str | None = None,
    **kwargs,
):
    """Constructs a labeled enum parameter.

    Args:
        ui_hints: Ui hints. `SHOW_LABEL` is automatically appended.
        kwargs: Additional kwargs to pass to `enum_parameter`.
    """
    ui_hints = ui_hint.add_ui_hint(ui_hints, ui_hint.UiHint.SHOW_LABEL)
    # default prevents kwargs error?
    return enum_parameter(
        enum, parameter_name, display_name, ui_hints=ui_hints, default=default, **kwargs
    )


def horizontal_enum_parameter(
    enum: enum.Enum,
    parameter_name: str | None = None,
    display_name: str | None = None,
    ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
    **kwargs,
):
    """Constructs a horizontal enum parameter.

    Args:
        ui_hints: Ui hints. `HORIZONTAL_ENUM` is automatically appended.
        kwargs: Additional kwargs to pass to `enum_parameter`.
    """

    ui_hints = ui_hint.add_ui_hint(ui_hints, ui_hint.UiHint.HORIZONTAL_ENUM)
    return enum_parameter(
        enum, parameter_name, display_name, ui_hints=ui_hints, **kwargs
    )


def boolean_parameter(
    parameter_name: str,
    display_name: str | None = None,
    ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
    description: str | None = None,
    default: bool = False,
) -> TypeParameter:
    """Constructs a boolean parameter."""
    map = annotation_map.parameter_annotation_map(
        parameter_name, display_name, ui_hints, description, default
    )
    return TypeParameter(parameter_name, "boolean", map)


def boolean_flip_parameter(
    parameter_name: str = "oppositeDirection",
    ui_hints: ui_hint.UiHint | None = None,
    **kwargs,
):
    """Constructs a boolean flip parameter."""
    ui_hints = ui_hint.add_ui_hint(ui_hints, ui_hint.UiHint.OPPOSITE_DIRECTION)
    return boolean_parameter(parameter_name, ui_hints=ui_hints, **kwargs)


def boolean_circular_flip_parameter(
    parameter_name: str = "oppositeDirection",
    ui_hints: ui_hint.UiHint | None = None,
    **kwargs,
) -> TypeParameter:
    """Constructs a boolean circular flip parameter."""
    ui_hints = ui_hint.add_ui_hint(ui_hints, ui_hint.UiHint.OPPOSITE_DIRECTION_CIRCULAR)
    return boolean_parameter(parameter_name, ui_hints=ui_hints, **kwargs)


class ValueParameter(node.Node, ABC):
    """Represents a UI element which belongs to a predicate, such as a length, angle, or query."""

    def __init__(
        self,
        parameter_name: str,
        predicate: str,
        annotation_map: annotation_map.AnnotationMap,
        parent: node.ParentNode | None = None,
        bound_spec: str | bounds.BoundSpec | None = None,
    ):
        node.handle_parent(self, parent)
        self.parameter_name = parameter_name
        self.bound_spec = bound_spec
        self.predicate = predicate
        self.annotation_map = annotation_map

    @override
    def build(self, context: ctxt.Context) -> str:
        if not context.ui:
            warnings.warn("UI parameter added to non-UI predicate")
        context.scope = ctxt.Scope.EXPRESSION
        map_str = self.annotation_map.run_build(context)
        if self.bound_spec:
            return map_str + "{}({}, {});\n".format(
                self.predicate,
                utils.definition(self.parameter_name).run_build(context),
                expr.build_expr(self.bound_spec, context),
            )
        return map_str + "{}({});\n".format(
            self.predicate,
            utils.definition(self.parameter_name).run_build(context),
        )


def length_parameter(
    parameter_name: str,
    display_name: str | None = None,
    bound_spec: str
    | bounds.LengthBoundSpec = bounds.LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
    ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_EXPRESSION,
    description: str | None = None,
) -> ValueParameter:
    map = annotation_map.parameter_annotation_map(
        parameter_name, display_name, ui_hints, description
    )
    return ValueParameter(parameter_name, "isLength", map, bound_spec=bound_spec)


def integer_parameter(
    parameter_name: str,
    display_name: str | None = None,
    bound_spec: str
    | bounds.IntegerBoundSpec = bounds.IntegerBound.POSITIVE_COUNT_BOUNDS,
    ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_EXPRESSION,
    description: str | None = None,
) -> ValueParameter:
    map = annotation_map.parameter_annotation_map(
        parameter_name, display_name, ui_hints, description
    )
    return ValueParameter(parameter_name, "isInteger", map, bound_spec=bound_spec)


def real_parameter(
    parameter_name: str,
    display_name: str | None = None,
    *,
    bound_spec: str | bounds.RealBoundSpec,
    ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_EXPRESSION,
    description: str | None = None,
) -> ValueParameter:
    map = annotation_map.parameter_annotation_map(
        parameter_name, display_name, ui_hints, description
    )
    return ValueParameter(parameter_name, "isReal", map, bound_spec=bound_spec)


def query_parameter(
    parameter_name: str,
    display_name: str | None = None,
    *,
    filter: expr.Expression,
    ui_hints: ui_hint.UiHint | None = None,
    max_picks: int | None = None,
    description: str | None = None,
) -> TypeParameter:
    map = annotation_map.parameter_annotation_map(
        parameter_name,
        display_name,
        ui_hints,
        description,
        filter=filter,
        max_picks=max_picks,
    )
    return TypeParameter(parameter_name, "Query", map)


class ParameterGroup(node.ParentNode):
    def __init__(
        self,
        display_name: str,
        uppercase_first_letter: bool = True,
        parent: node.ParentNode | None = None,
        collapsed_by_default: bool = False,
        driving_parameter: str | None = None,
    ) -> None:
        """A parameter group annotation.

        Args:
            driving_parameter: The name of a parameter driving the group. See also `DrivenParameterGroup`.
            uppercase_first_letter: True to automatically uppercase the first letter of `display_name`. Used to promote consistency with naming among parameters.
        """
        super().__init__()
        node.handle_parent(self, parent)
        if uppercase_first_letter:
            display_name = str_utils.upper_first(display_name)

        dictionary = {
            "Group Name": display_name,
            "Collapsed By Default": collapsed_by_default,
        }
        if driving_parameter:
            dictionary["Driving Parameter"] = driving_parameter
        self.map = annotation_map.AnnotationMap(
            dictionary, quote_values=True, excluded_values=["Collapsed By Default"]
        )

    @override
    def build(self, context: ctxt.Context) -> str:
        if not context.ui:
            warnings.warn("UI parameter added to non-UI predicate")
        if len(self.children) == 0:
            warnings.warn("Empty parameter group not permitted")
        return "".join(
            [
                self.map.run_build(context, ctxt.Scope.EXPRESSION),
                "{\n",
                self.build_children(
                    context, sep="\n", indent=True, scope=ctxt.Scope.STATEMENT
                ),
                "}\n",
            ]
        )


class DrivenParameterGroup(node.ParentNode):
    """Represents a parameter group driven by a boolean."""

    def __init__(
        self,
        parameter_name: str,
        display_name: str | None = None,
        parent: node.ParentNode | None = None,
        ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
        description: str | None = None,
        default: bool = False,
        test: expr.Expression | None = None,
    ) -> None:
        """
        Args:
            parameter_name: The parameter name of the boolean parameter.
            display_name: The user facing name of both the boolean parameter and parameter group.
            test:
                An additional test used to determine whether to allow driving the group in the first place.
                When the test evalutes to true, the group is driven by the (internal) boolean parameter.
                When the test evalutes to false, the internal boolean parameter is hidden, and the group is a standard group.
        """
        self.drive_group_test = test
        self.parameter_name = parameter_name
        self.group = ParameterGroup(
            display_name or str_utils.display_name(parameter_name),
            uppercase_first_letter=False,
            parent=parent,
            driving_parameter=parameter_name,
        )
        self.boolean = boolean_parameter(
            parameter_name,
            display_name=display_name,
            ui_hints=ui_hints,
            default=default,
            description=description,
        )

    @override
    def add(self, *nodes: node.Node) -> Self:
        self.group.add(*nodes)
        return self

    @override
    def build(self, context: ctxt.Context) -> str:
        if self.drive_group_test is None:
            string = self.boolean.run_build(context) + "\n"
            test = utils.definition(self.parameter_name)
        else:
            string = (
                control.IfBlock(self.drive_group_test)
                .add(self.boolean)
                .run_build(context)
            )
            test = ~expr.add_parens(self.drive_group_test) | utils.definition(
                self.parameter_name
            )
        string += control.IfBlock(test).add(self.group).run_build(context)
        return string
