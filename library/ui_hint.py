import enum as _enum

__all__ = ["UiHint", "remember_hint"]


class UiHint(_enum.StrEnum):
    ALWAYS_HIDDEN = '"ALWAYS_HIDDEN"'
    READ_ONLY = '"READ_ONLY"'
    UNCONFIGURABLE = '"UNCONFIGURABLE"'
    REMEMBER_PREVIOUS_VALUE = '"REMEMBER_PREVIOUS_VALUE"'
    HORIZONTAL_ENUM = '"HORIZONTAL_ENUM"'
    SHOW_LABEL = '"SHOW_LABEL"'
    SHOW_EXPRESSION = '"SHOW_EXPRESSION"'
    OPPOSITE_DIRECTION = '"OPPOSITE_DIRECTION"'
    OPPOSITE_DIRECTION_CIRCULAR = '"OPPOSITE_DIRECTION_CIRCULAR"'


remember_hint = [UiHint.REMEMBER_PREVIOUS_VALUE]

# @dataclasses.dataclass
# class UiHints:
#     # general
#     always_hidden: bool | None = None
#     read_only: bool | None = None
#     unconfigurable: bool | None = None
#     remember_previous_value: bool | None = None
#     # field
#     show_expression: bool | None = None
#     # enum
#     horizontal_enum: bool | None = None
#     show_label: bool | None = None
#     # bool
#     opposite_direction: bool | None = None
#     # arg type is bool | None, defaults to None (or defaults to true if overridden)
#     opposite_direction_circular: bool | None = None
#     # query

#     def __len__(self) -> int:
#         return sum(value for value in dataclasses.astuple(self) if value)

#     def __str__(self) -> str:
#         ui_hints = []
#         for field, value in dataclasses.asdict(self):
#             if value:
#                 ui_hints.append(utils.quote(field.upper()))
#         return utils.to_str(ui_hints, sep=", ")


# @dataclasses.dataclass
# class BaseUiHints(UiHints):
#     remember_previous_value: bool = True


# @dataclasses.dataclass
# class FieldUiHints(BaseUiHints):
#     show_expression: bool = True
