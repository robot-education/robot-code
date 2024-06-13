import lark
import pathlib
import logging
from featurescript.parser import ast

lark.logger.setLevel(logging.DEBUG)

curr_path = pathlib.Path(__file__).parent


def main():
    parser = ast.make_parser(debug=True)

    with (curr_path / "test.fs").open() as test:
        parsed_ast = parser.parse(test.read())
        print(parsed_ast)


if __name__ == "__main__":
    main()
