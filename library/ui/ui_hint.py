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


# tuple to ensure immutability to prevent mutation bugs
HORIZONTAL_HINT = UiHint.REMEMBER_PREVIOUS_VALUE | UiHint.HORIZONTAL_ENUM
SHOW_LABEL_HINT = UiHint.REMEMBER_PREVIOUS_VALUE | UiHint.SHOW_LABEL
SHOW_EXPRESSION_HINT = UiHint.REMEMBER_PREVIOUS_VALUE | UiHint.SHOW_EXPRESSION
