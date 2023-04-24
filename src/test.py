from src.library.enum import Enum
from src.library.control import If
from src.library.ui import EnumAnnotation, UiPredicate, UiTestPredicate, equal
from src.library.studio import Studio

# All values are predicates or enums anyways...
# Yeah not a lot of raw variables floating around (or literals, for that matter)


def main() -> None:
    studio = Studio("test.fs")

    comp_enum = studio.add(Enum("Competition", "FRC", "VEX", ui=False))

    frame_style = studio.add(Enum("FrameCreationStyle", "CREATE_VALUE", "CONVERT"))

    comp_enum_tests = {}
    for value in comp_enum:
        name = "is" + value.camel_case().capitalize()
        pred = studio.add(UiTestPredicate(name))
        pred.add(equal("competition", value))
        comp_enum_tests[name] = pred.call()

    pred = studio.add(UiPredicate("competition"))
    pred.add(
        EnumAnnotation(
            comp_enum,
            default="VEX",
        )
    )

    frame_if = pred.add(If(comp_enum_tests["isFrc"]))
    frame_if.add(EnumAnnotation(frame_style))

    # studio += comp_enum
    # studio += is_frc
    # studio += frame_creation_style
    # studio += pred

    studio.print()


if __name__ == "__main__":
    main()
