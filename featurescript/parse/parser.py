import lark
import pathlib

curr_path = pathlib.Path(__file__).parent


def make_parser() -> lark.Lark:
    with (curr_path / "grammar.lark").open() as grammar:
        return lark.Lark(grammar.read(), start="studio", parser="lalr", strict=True)
