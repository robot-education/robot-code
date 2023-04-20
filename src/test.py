from src.library.enum import Enum
from src.library.predicate import Predicate
from src.library.stmt import If
from src.library.ui import EnumAnnotation, UiPredicate, definition
from src.library.studio import Studio

# All values are predicates or enums anyways...
# Yeah not a lot of raw variables floating around (or literals, for that matter)


def main() -> None:
    comp_enum = Enum("Competition", "FRC", "VEX", ui=False)

    frame_creation_style = Enum("FrameCreationStyle", "CREATE_VALUE", "CONVERT")

    is_frc = Predicate("isFrc", definition)

    pred = UiPredicate("competition")

    comp = EnumAnnotation(comp_enum)
    pred += comp

    is_frc += comp.equal(comp_enum.FRC)

    frame_if = If(is_frc.call())
    pred += frame_if

    enum_ann = EnumAnnotation(frame_creation_style)
    frame_if += enum_ann

    studio = Studio("test.fs")
    studio += comp_enum
    studio += is_frc
    studio += frame_creation_style
    studio += pred
    studio.print()


if __name__ == "__main__":
    main()
