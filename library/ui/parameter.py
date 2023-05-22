from abc import ABC
import dataclasses
from typing import Self
from typing_extensions import override
import warnings
from library.core import control, utils, map
from library.base import ctxt, expr, node, stmt, str_utils
from library.ui import bounds, enum, ui_hint, annotation_map


@dataclasses.dataclass
class TypeParameter(stmt.Statement, ABC):
    """Represents a UI element which is a type, such as an enum or boolean."""

    parameter_name: str
    type: str
    annotation_map: annotation_map.AnnotationMap
    parent: node.ParentNode | None = None

    def __post_init__(self) -> None:
        super().__init__(self.parent)

    @override
    def build(self, context: ctxt.Context) -> str:
        return (
            self.annotation_map.run_build(context)
            + utils.definition(self.parameter_name).run_build(context)
            + " is {};\n".format(self.type)
        )


def enum_parameter(
    enum: enum.EnumDict,
    parameter_name: str | None = None,
    user_name: str | None = None,
    ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
    description: str | None = None,
    default: str | None = None,
) -> TypeParameter:
    parameter_name = parameter_name or enum.default_parameter_name
    map = annotation_map.parameter_annotation_map(
        parameter_name,
        user_name,
        ui_hints,
        description,
        default,
    )
    return TypeParameter(parameter_name, enum.name, map)


def labeled_enum_parameter(
    enum: enum.EnumDict,
    ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
    **kwargs,
):
    """Constructs a labeled enum parameter.

    Args:
        ui_hints: Ui hints. `SHOW_LABEL` is automatically appended.
        kwargs: Additional kwargs to pass to `enum_parameter`.
    """
    ui_hints = ui_hint.add_ui_hint(ui_hints, ui_hint.UiHint.SHOW_LABEL)
    return enum_parameter(enum, ui_hints=ui_hints, **kwargs)


def horizontal_enum_parameter(
    enum: enum.EnumDict,
    ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
    **kwargs,
):
    """Constructs a horizontal enum parameter.

    Args:
        ui_hints: Ui hints. `HORIZONTAL_ENUM` is automatically appended.
        kwargs: Additional kwargs to pass to `enum_parameter`.
    """

    ui_hints = ui_hint.add_ui_hint(ui_hints, ui_hint.UiHint.HORIZONTAL_ENUM)
    return enum_parameter(enum, ui_hints=ui_hints, **kwargs)


def boolean_parameter(
    parameter_name: str,
    user_name: str | None = None,
    ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
    description: str | None = None,
    default: bool = False,
) -> TypeParameter:
    """Constructs a boolean parameter."""
    map = annotation_map.parameter_annotation_map(
        parameter_name, user_name, ui_hints, description, default
    )
    return TypeParameter(parameter_name, "boolean", map)


def boolean_flip_parameter(
    parameter_name: str, ui_hints: ui_hint.UiHint | None = None, **kwargs
):
    """Constructs a boolean flip parameter."""
    ui_hints = ui_hint.add_ui_hint(ui_hints, ui_hint.UiHint.OPPOSITE_DIRECTION)
    return boolean_parameter(parameter_name, ui_hints=ui_hints, **kwargs)


def boolean_circular_flip_parameter(
    parameter_name: str, ui_hints: ui_hint.UiHint | None = None, **kwargs
) -> TypeParameter:
    """Constructs a boolean circular flip parameter."""
    ui_hints = ui_hint.add_ui_hint(ui_hints, ui_hint.UiHint.OPPOSITE_DIRECTION_CIRCULAR)
    return boolean_parameter(parameter_name, ui_hints=ui_hints, **kwargs)


class ValueParameter(stmt.Statement, ABC):
    """Represents a UI element which belongs to a predicate, such as a length, angle, or query."""

    def __init__(
        self,
        parameter_name: str,
        bound_spec: str | bounds.BoundSpec,
        predicate: str,
        annotation_map: annotation_map.AnnotationMap,
        parent: node.ParentNode | None = None,
    ):
        super().__init__(parent)
        self.parameter_name = parameter_name
        self.bound_spec = expr.cast_to_expr(bound_spec)
        self.predicate = predicate
        self.annotation_map = annotation_map

    @override
    def build(self, context: ctxt.Context) -> str:
        return self.annotation_map.run_build(context) + "{}({}, {});\n".format(
            self.predicate,
            utils.definition(self.parameter_name).run_build(context),
            self.bound_spec.run_build(context),
        )


def length_parameter(
    parameter_name: str,
    bound_spec: str
    | bounds.LengthBoundSpec = bounds.LengthBound.NONNEGATIVE_LENGTH_BOUNDS,
    user_name: str | None = None,
    ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_EXPRESSION,
    description: str | None = None,
) -> ValueParameter:
    map = annotation_map.parameter_annotation_map(
        parameter_name, user_name, ui_hints, description
    )
    return ValueParameter(parameter_name, bound_spec, "isLength", map)


def integer_parameter(
    parameter_name: str,
    bound_spec: str
    | bounds.IntegerBoundSpec = bounds.IntegerBound.POSITIVE_COUNT_BOUNDS,
    user_name: str | None = None,
    ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_EXPRESSION,
    description: str | None = None,
) -> ValueParameter:
    map = annotation_map.parameter_annotation_map(
        parameter_name, user_name, ui_hints, description
    )
    return ValueParameter(parameter_name, bound_spec, "isInteger", map)


def real_parameter(
    parameter_name: str,
    *,
    bound_spec: str | bounds.RealBoundSpec,
    user_name: str | None = None,
    ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_EXPRESSION,
    description: str | None = None,
) -> ValueParameter:
    map = annotation_map.parameter_annotation_map(
        parameter_name, user_name, ui_hints, description
    )
    return ValueParameter(parameter_name, bound_spec, "isReal", map)


class ParameterGroup(stmt.BlockStatement):
    def __init__(
        self,
        user_name: str,
        parent: node.ParentNode | None = None,
        collapsed_by_default: bool = False,
        driving_parameter: str | None = None,
    ) -> None:
        """A parameter group annotation.

        Args:
            driving_parameter: The name of a parameter driving the group. See also `DrivenParameterGroup`.
        """
        super().__init__(parent)
        dictionary = {
            "Group Name": user_name,
            "Collapsed By Default": collapsed_by_default,
        }
        if driving_parameter:
            dictionary["Driving Parameter"] = driving_parameter
        self.map = map.Map(
            dictionary, quote_values=True, excluded_values=["Collapsed By Default"]
        )

    @override
    def build(self, context: ctxt.Context) -> str:
        if len(self.children) == 0:
            warnings.warn("Empty parameter group not permitted.")
        return "".join(
            [
                "annotation " + self.map.run_build(context),
                "\n{\n",
                self.build_children(context, sep="\n", indent=True),
                "}\n",
            ]
        )


class DrivenParameterGroup(stmt.BlockStatement):
    def __init__(
        self,
        parameter_name: str,
        parent: node.ParentNode | None = None,
        user_name: str | None = None,
        ui_hints: ui_hint.UiHint | None = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
        default: bool = False,
        drive_group_test: expr.Expr | None = None,
    ) -> None:
        """
        Args:
            parameter_name: The parameter name of the boolean parameter.
            user_name: The user facing name of both the boolean parameter and parameter group.
            drive_group_test:
                A test used to determine whether to drive the group or not.
                When true, the group is driven by the boolean.
                When false, the boolean is hidden, and the group is a standard group.
        """
        super().__init__(parent)
        self.drive_group_test = drive_group_test
        self.parameter_name = parameter_name
        self.group = ParameterGroup(
            user_name or str_utils.user_name(parameter_name),
            driving_parameter=parameter_name,
        )
        self.boolean = boolean_parameter(
            parameter_name=self.parameter_name,
            user_name=user_name,
            ui_hints=ui_hints,
            default=default,
        )

    @override
    def add(self, *args) -> Self:
        self.group.add(*args)
        return self

    @override
    def build(self, context: ctxt.Context) -> str:
        if self.drive_group_test is None:
            string = self.boolean.run_build(context) + "\n"
            string += (
                control.IfBlock(utils.definition(self.parameter_name))
                .add(self.group)
                .run_build(context)
            )
        else:
            string = (
                control.IfBlock(self.drive_group_test)
                .add(self.boolean)
                .run_build(context)
            )
            string += (
                control.IfBlock(
                    ~expr.add_parens(self.drive_group_test)
                    | utils.definition(self.parameter_name)
                )
                .add(self.group)
                .run_build(context)
            )
        return string
