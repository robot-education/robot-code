from abc import ABC
from typing import Iterable, Self
from typing_extensions import override
import warnings
from library.core import control, utils, map
from library.base import ctxt, expr, node, stmt, str_utils
from library.ui import bounds, enum, ui_hint

__all__ = [
    "EnumParameter",
    "BooleanParameter",
    "LengthParameter",
    "CountParameter",
    "BooleanFlipParameter",
    "BooleanCircularFlipParameter",
    "GroupParameter",
    "DrivenGroupParameter",
]


class Annotation(stmt.Statement, ABC):
    def __init__(
        self,
        parameter_name: str,
        parent: node.ParentNode | None = None,
        user_name: str | None = None,
        ui_hints: ui_hint.UiHint | None = None,
        args: dict[str, str] = {},
        exclude_keys: Iterable[str] = [],
        add_name: bool = True,
    ) -> None:
        """
        A dict containing additional strings to add to the annotation map.
        """
        super().__init__(parent=parent)
        self.parameter_name = parameter_name
        self.user_name = user_name or str_utils.user_name(self.parameter_name)

        # always put name and ui hints first
        map_args = {"Name": self.user_name} if add_name else {}
        if ui_hints is not None:
            names = [str_utils.quote(ui_hint.name or "") for ui_hint in ui_hints]
            map_args["UIHint"] = "[{}]".format(", ".join(names))
        map_args.update(args)

        keys = list(exclude_keys)
        keys.append("UIHint")
        self.map = map.Map(map_args, quote_values=True, exclude_keys=keys)

    @override
    def build(self, context: ctxt.Context) -> str:
        return "annotation " + self.map.build(context) + "\n"


class TypeParameter(Annotation, ABC):
    """A class defining a UI element which is a type, such as an enum or boolean."""

    def __init__(self, parameter_name: str, type: str, **kwargs) -> None:
        super().__init__(parameter_name, **kwargs)
        self.type = type

    @override
    def build(self, context: ctxt.Context) -> str:
        return (
            super().build(context)
            + utils.definition(self.parameter_name).build(context)
            + " is {};\n".format(self.type)
        )


class EnumParameter(TypeParameter):
    def __init__(
        self,
        enum: enum.EnumDict,
        parameter_name: str | None = None,
        default: str | None = None,
        ui_hints: ui_hint.UiHint = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
        **kwargs,
    ) -> None:
        self.enum = enum
        parameter_name = parameter_name or enum.default_parameter_name
        args = {} if not default else {"Default": default}
        super().__init__(
            parameter_name, type=self.enum.name, ui_hints=ui_hints, args=args, **kwargs
        )


class BooleanParameter(TypeParameter):
    def __init__(
        self,
        parameter_name: str,
        default: bool = False,
        ui_hints: ui_hint.UiHint = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
        **kwargs,
    ) -> None:
        args = {} if not default else {"Default": "true"}
        exclude_keys = ["Default"]
        super().__init__(
            parameter_name,
            type="boolean",
            ui_hints=ui_hints,
            args=args,
            exclude_keys=exclude_keys,
            **kwargs,
        )


class ValueParameter(Annotation, ABC):
    """A class defining a UI element which belongs to a predicate, such as a length, angle, or query."""

    def __init__(
        self, parameter_name: str, bound_spec: str, *, predicate: str, **kwargs
    ):
        super().__init__(parameter_name, **kwargs)
        self.bound_spec = bound_spec
        self.predicate = predicate

    @override
    def build(self, context: ctxt.Context) -> str:
        return super().build(context) + "{}({}, {});\n".format(
            self.predicate,
            utils.definition(self.parameter_name).build(context),
            self.bound_spec,
        )


class LengthParameter(ValueParameter):
    def __init__(
        self,
        parameter_name: str,
        *,
        bound_spec: str,
        ui_hints: ui_hint.UiHint = ui_hint.SHOW_EXPRESSION_HINT,
        **kwargs,
    ) -> None:
        super().__init__(
            parameter_name,
            bound_spec,
            ui_hints=ui_hints,
            predicate="isLength",
            **kwargs,
        )


class CountParameter(ValueParameter):
    def __init__(
        self,
        parameter_name: str,
        *,
        bound_spec: str = bounds.CountBound.POSITIVE_COUNT_BOUNDS,
        ui_hints: ui_hint.UiHint = ui_hint.SHOW_EXPRESSION_HINT,
        **kwargs,
    ) -> None:
        super().__init__(
            parameter_name,
            bound_spec,
            ui_hints=ui_hints,
            predicate="isInteger",
            **kwargs,
        )


class BooleanFlipParameter(BooleanParameter):
    def __init__(
        self,
        parameter_name: str,
        ui_hints: ui_hint.UiHint = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
        **kwargs,
    ) -> None:
        ui_hints |= ui_hint.UiHint.OPPOSITE_DIRECTION
        super().__init__(parameter_name, ui_hints=ui_hints, **kwargs)


class BooleanCircularFlipParameter(BooleanParameter):
    def __init__(
        self,
        parameter_name: str,
        ui_hints: ui_hint.UiHint = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
        **kwargs,
    ) -> None:
        ui_hints |= ui_hint.UiHint.OPPOSITE_DIRECTION_CIRCULAR
        super().__init__(parameter_name, ui_hints=ui_hints, **kwargs)


class GroupParameter(Annotation, stmt.BlockStatement):
    def __init__(
        self,
        user_name: str,
        collapsed_by_default: bool = False,
        args: dict[str, str] = {},
        **kwargs,
    ) -> None:
        args["Group Name"] = user_name
        args["Collapsed By Default"] = "true" if collapsed_by_default else "false"
        exclude_keys = ["Collapsed By Default"]

        super().__init__(
            "",
            user_name=user_name,
            args=args,
            exclude_keys=exclude_keys,
            add_name=False,
            **kwargs,
        )

    @override
    def build(self, context: ctxt.Context) -> str:
        if len(self.children) == 0:
            warnings.warn("Empty parameter group not permitted")
        string = super().build(context)
        string += "{\n"
        string += self.build_children(context, sep="\n", indent=True)
        return string + "}\n"


class DrivenGroupParameter(stmt.BlockStatement):
    def __init__(
        self,
        *,
        parameter_name: str,
        user_name: str,
        ui_hints: ui_hint.UiHint = ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
        default: bool = False,
        drive_group_test: expr.Expr | None = None,
    ) -> None:
        """
        Args:
            drive_group_test:
                A test used to determine whether to drive the group or not.
                When true, the group is driven by the boolean.
                When false, the boolean is hidden, and the group is a standard group.
        """
        self.drive_group_test = drive_group_test
        self.group = GroupParameter(
            user_name,
            args={"Driving Parameter": parameter_name},
        )
        self.parameter_name = parameter_name
        self.boolean = BooleanParameter(
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
            string = self.boolean.build(context) + "\n"
            string += (
                control.IfBlock(utils.definition(self.parameter_name))
                .add(self.group)
                .build(context)
            )
        else:
            string = (
                control.IfBlock(self.drive_group_test).add(self.boolean).build(context)
            )
            string += (
                control.IfBlock(
                    ~expr.add_parens(self.drive_group_test)
                    | utils.definition(self.parameter_name)
                )
                .add(self.group)
                .build(context)
            )
        return string
