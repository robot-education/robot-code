from src.library.predicate import UiPredicate
from src.library.expr import Id, Parens

# All values are predicates or enums anyways...
# Yeah not a lot of raw variables floating around (or literals, for that matter)


def main() -> None:
    pred = UiPredicate("isEnumValid")
    pred += Parens(Id("hello") & Id("ahh")) | Parens(Id("hmm") & Id("woah"))
    pred += Id("hello") & Id("ahh")
    pred += pred.call()
    print(pred, end="")


if __name__ == "__main__":
    main()
