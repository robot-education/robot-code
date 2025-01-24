import enum as std_enum


class UiHint(std_enum.Flag):
    ALWAYS_HIDDEN = std_enum.auto()
    READ_ONLY = std_enum.auto()
    UNCONFIGURABLE = std_enum.auto()
    REMEMBER_PREVIOUS_VALUE = std_enum.auto()
    HORIZONTAL_ENUM = std_enum.auto()
    SHOW_LABEL = std_enum.auto()
    SHOW_EXPRESSION = std_enum.auto()
    OPPOSITE_DIRECTION = std_enum.auto()
    OPPOSITE_DIRECTION_CIRCULAR = std_enum.auto()
    DISPLAY_SHORT = std_enum.auto()
    REMEMBER_EXPRESSION = REMEMBER_PREVIOUS_VALUE | SHOW_EXPRESSION


def add_ui_hint(ui_hint: UiHint | None, new_hint: UiHint) -> UiHint:
    if ui_hint is None:
        return new_hint
    return ui_hint | new_hint
