from abc import ABC
from typing import Iterable, Sequence
from library import base, stmt, ui_hint, utils, enum

__all__ = [
    "EnumAnnotation",
    "BooleanAnnotation",
    "LengthAnnotation",
]


class Map(base.Node):
    """Defines a map literal."""

    def __init__(
        self,
        dict: dict[str, str],
        quote_values: bool = False,
        exclude_keys: Iterable[str] = [],
    ):
        """
        quote_values: Whether to add quotation marks around each value.
        exclude_keys: Specifies keys to ignore when quoting. Does nothing if quote_values is False.
        """
        self.dict = dict
        self.quote_values = quote_values
        self.exclude_values = exclude_keys

    def _quote_format_str(self, quote_value: bool) -> str:
        return ' "{}" : "{}"' if quote_value else ' "{}" : {}'

    def __str__(self) -> str:
        pairs = [
            self._quote_format_str(
                self.quote_values and key not in self.exclude_values
            ).format(key, value)
            for key, value in self.dict.items()
            if value is not None
        ]

        if len(pairs) == 0:
            return "{}"

        return "{{{}}}".format(",".join(pairs) + " ")


class Annotation(stmt.Statement, ABC):
    def __init__(
        self,
        parameter_name: str,
        user_name: str | None = None,
        ui_hints: Sequence[ui_hint.UiHint] = [],
        args: dict[str, str] = {},
    ) -> None:
        """
        A dict containing additional strings to add to the annotation map.
        """
        self.parameter_name = parameter_name
        self.user_name = user_name or utils.user_name(self.parameter_name)

        # always put name and ui hints first
        map_args = {"Name": self.user_name}
        if len(ui_hints) > 0:
            map_args["UIHint"] = "[{}]".format(", ".join(ui_hints))
        map_args.update(args)

        self.map = Map(map_args, quote_values=True, exclude_keys="UIHint")

    def __str__(self) -> str:
        return "annotation " + str(self.map) + "\n"


class TypeAnnotation(Annotation, ABC):
    """A class defining a UI element which is a type, such as an enum or boolean."""

    def __init__(self, parameter_name: str, type: str, **kwargs) -> None:
        super().__init__(parameter_name, **kwargs)
        self.type = type

    def __str__(self) -> str:
        return (
            super().__str__()
            + utils.definition(self.parameter_name)
            + " is {};\n".format(self.type)
        )


class EnumAnnotation(TypeAnnotation):
    def __init__(
        self,
        enum: enum.EnumDict,
        parameter_name: str | None = None,
        user_name: str | None = None,
        default: str | None = None,
        ui_hints: Iterable[ui_hint.UiHint] | None = ui_hint.remember_hint,
    ) -> None:
        self.enum = enum
        parameter_name = parameter_name or enum.default_parameter_name
        args = {} if not default else {"Default": default}
        super().__init__(
            parameter_name,
            type=self.enum.name,
            user_name=user_name,
            ui_hints=ui_hints,
            args=args,
        )


class BooleanAnnotation(TypeAnnotation):
    def __init__(
        self,
        parameter_name: str,
        user_name: str | None = None,
        default: bool = False,
        ui_hints: Iterable[ui_hint.UiHint] = ui_hint.remember_hint,
    ) -> None:
        args = {} if not default else {"Default": "true"}
        super().__init__(
            parameter_name,
            user_name=user_name,
            type="boolean",
            args=args,
            ui_hints=ui_hints,
        )


class ValueAnnotation(Annotation, ABC):
    """A class defining a UI element which belongs to a predicate, such as a length, angle, or query."""

    def __init__(
        self, parameter_name: str, bound_spec: str, *, predicate: str, **kwargs
    ):
        super().__init__(parameter_name, **kwargs)
        self.bound_spec = bound_spec
        self.predicate = predicate

    def __str__(self) -> str:
        return super().__str__() + "{}({}, {});\n".format(
            self.predicate, utils.definition(self.parameter_name), self.bound_spec
        )


class LengthAnnotation(ValueAnnotation):
    def __init__(
        self,
        parameter_name: str,
        bound_spec: str,
        user_name: str | None = None,
        ui_hints: Iterable[ui_hint.UiHint] = [
            ui_hint.UiHint.SHOW_EXPRESSION,
            ui_hint.UiHint.REMEMBER_PREVIOUS_VALUE,
        ],
    ) -> None:
        super().__init__(
            parameter_name,
            bound_spec,
            user_name=user_name,
            ui_hints=ui_hints,
            predicate="isLength",
        )


class BooleanFlipAnnotation(BooleanAnnotation):
    def __init__(
        self,
        parameter_name: str,
        ui_hints: Iterable[ui_hint.UiHint] = ui_hint.remember_hint,
        **kwargs,
    ) -> None:
        ui_hints = list(ui_hints)
        ui_hints.append(ui_hint.UiHint.OPPOSITE_DIRECTION)
        super().__init__(parameter_name, ui_hints=ui_hints, **kwargs)


class BooleanCircularFlipAnnotation(BooleanAnnotation):
    def __init__(
        self,
        parameter_name: str,
        ui_hints: Iterable[ui_hint.UiHint] = ui_hint.remember_hint,
        **kwargs,
    ) -> None:
        ui_hints = list(ui_hints)
        ui_hints.append(ui_hint.UiHint.OPPOSITE_DIRECTION_CIRCULAR)
        super().__init__(parameter_name, ui_hints=ui_hints, **kwargs)
