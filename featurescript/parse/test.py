import lark
import pathlib
import logging

from featurescript.parse.parser import make_parser

lark.logger.setLevel(logging.WARN)

curr_path = pathlib.Path(__file__).parent


def main():
    parser = make_parser()

    with (curr_path / "test.fs").open() as test:
        ast = parser.parse(test.read())
        print(ast.pretty())


if __name__ == "__main__":
    main()
