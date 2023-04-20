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

    # is_frc = Predicate(
    #     "isFrc", definition, comp_enum.equals("competition", comp_enum.CREATE)
    # )

    pred = UiPredicate("competition")

    comp = pred.add(EnumAnnotation(comp_enum))

    is_frc += comp.equal(comp_enum.FRC)

    frame_if = pred.add(If(is_frc.call()))

    enum_ann = frame_if.add(EnumAnnotation(frame_creation_style))

    studio = Studio("test.fs")
    studio += comp_enum
    studio += is_frc
    studio += frame_creation_style
    studio += pred
    studio.print()


if __name__ == "__main__":
    main()
