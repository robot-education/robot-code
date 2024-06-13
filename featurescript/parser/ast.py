from __future__ import annotations
from abc import ABC
from dataclasses import dataclass
import dataclasses
import lark
from lark import Token, ast_utils

import pathlib
import sys

curr_module = sys.modules[__name__]
curr_path = pathlib.Path(__file__).parent


@dataclass
class Studio:
    version: int
    imports: list[Import]
    top_statements: list[TopStatement]


@dataclass
class Import:
    path: str
    version: str
    export: bool = False
    namespace: str | None = None


class Annotation:
    pass


@dataclass
class TopStatement(ABC):
    annotation: Annotation | None = dataclasses.field(init=False)
    export: bool = dataclasses.field(init=False)


@dataclass
class Statement(ABC):
    annotation: Annotation | None = dataclasses.field(init=False)


class Expression(ABC):
    pass


@dataclass
class ConstantDeclaration(ast_utils.Ast, TopStatement, Statement):
    name: str
    type_tag: str | None
    expression: Expression


@dataclass
class VariableDeclaration(ast_utils.Ast, Statement):
    name: str
    type_tag: str | None
    expression: Expression | None


@lark.v_args(inline=True)
class ToAst(lark.Transformer):
    @lark.v_args(inline=False)
    def studio(self, args):
        imports = []
        top_statements = []
        for block in args[1:]:
            if isinstance(block, Import):
                imports.append(block)
            else:
                top_statements.append(block)
        return Studio(args[0], imports, top_statements)

    def export(self, token: Token | None) -> bool:
        return token != None

    def namespace(self, namespace: str | None) -> str | None:
        return namespace

    def import_(
        self, export: bool, namespace: str | None, path: str, version: str
    ) -> Import:
        return Import(path, version, export=export, namespace=namespace)

    def top_statement(
        self, annotation: Annotation | None, export: bool, top_statement: TopStatement
    ) -> TopStatement:
        lark.logger.debug(export)
        top_statement.annotation = annotation
        top_statement.export = export
        return top_statement

    def statement(self, annotation: Annotation | None, statement: Statement):
        statement.annotation = annotation
        return statement

    @lark.v_args(inline=False)
    def map_literal(self, tokens: list[Token]):
        pass

    def numeric_literal(self, s: str):
        return float(s)

    # def number(self, n: str):
    #     if "." in n:
    #         return float(n)
    #     return int(n)

    def INT(self, token: Token) -> int:
        return int(token.value)

    def ESCAPED_STRING(self, token: Token) -> str:
        return token.value[1:-1]

    def ID(self, token: Token) -> str:
        return token.value

    def TYPE_ID(self, token: Token) -> str:
        return token.value


transformer = ast_utils.create_transformer(curr_module, ToAst())


def make_parser(debug: bool = False) -> lark.Lark:
    with (curr_path / "grammar.lark").open() as grammar:
        return lark.Lark(
            grammar.read(),
            start="studio",
            parser="lalr",
            strict=debug,
            debug=debug,
            transformer=transformer,
        )
