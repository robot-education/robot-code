"""Parses FeatureScript into the syntax tree in `fs_eval.ast`, strictly: unlike the language server's tolerant
parser, it stops at the first syntax error (`ParseError`), since it's for running code which compiles in Onshape."""

from __future__ import annotations

from fs_lsp.scanner import ASSIGNMENT_OPERATORS, Token, scan

from fs_eval import ast


class ParseError(Exception):
    pass


# Binary operators by precedence, loosest first (the ternary, unary operators, `^`, and postfix ones are
# handled separately)
BINARY_LEVELS: list[tuple[str, ...]] = [
    ("??",),
    ("||",),
    ("&&",),
    ("==", "!="),
    ("<", ">", "<=", ">="),
    ("~",),
    ("+", "-"),
    ("*", "/", "%"),
]
LOGICAL = frozenset(["&&", "||", "??"])


def parse(source: str, path: str = "<source>") -> ast.Program:
    return Parser(source, path).program()


def parse_expression(source: str, path: str = "<expression>") -> ast.Node:
    parser = Parser(source, path)
    expression = parser.expression()
    parser.expect_kind("eof")
    return expression


def _unquote(token: str) -> str:
    body = token[1:-1]
    if "\\" not in body:
        return body
    out = []
    i = 0
    escapes = {"n": "\n", "t": "\t", "r": "\r", "\\": "\\", '"': '"', "'": "'", "0": "\0", "b": "\b", "f": "\f"}
    while i < len(body):
        char = body[i]
        if char == "\\" and i + 1 < len(body):
            following = body[i + 1]
            if following == "u" and i + 5 < len(body):
                out.append(chr(int(body[i + 2 : i + 6], 16)))
                i += 6
                continue
            out.append(escapes.get(following, following))
            i += 2
            continue
        out.append(char)
        i += 1
    return "".join(out)


class Parser:
    def __init__(self, source: str, path: str) -> None:
        scanned = scan(source)
        for start, _end, kind in scanned.unterminated:
            line, _ = scanned.line_map.position(start)
            raise ParseError(f"{path}:{line + 1}: unterminated {kind}")
        self.tokens: list[Token] = scanned.tokens
        self.index = 0
        self.path = path

    # Tokens

    @property
    def token(self) -> Token:
        return self.tokens[self.index]

    def peek(self, distance: int = 1) -> Token:
        return self.tokens[min(self.index + distance, len(self.tokens) - 1)]

    def advance(self) -> Token:
        token = self.tokens[self.index]
        if token.kind != "eof":
            self.index += 1
        return token

    def check(self, value: str) -> bool:
        token = self.token
        return token.value == value and token.kind not in ("string", "eof")

    def accept(self, value: str) -> bool:
        if self.check(value):
            self.index += 1
            return True
        return False

    def expect(self, value: str) -> Token:
        if not self.check(value):
            self.fail(f"expected {value!r}")
        return self.advance()

    def expect_kind(self, kind: str) -> Token:
        if self.token.kind != kind:
            self.fail(f"expected {kind}")
        return self.advance()

    def identifier(self) -> str:
        token = self.token
        # Keywords like `type` and `default` are fine as names of fields and parameters
        if token.kind not in ("identifier", "keyword"):
            self.fail("expected a name")
        self.index += 1
        return token.value

    def fail(self, message: str):
        token = self.token
        found = token.value or "the end"
        raise ParseError(f"{self.path}:{token.line + 1}:{token.character + 1}: {message}, found {found!r}")

    def node(self, node: ast.Node, token: Token) -> ast.Node:
        node.at = (self.path, token.line + 1)
        return node

    # Top level

    def program(self) -> ast.Program:
        start = self.token
        declarations: list[ast.Node] = []
        if self.accept("FeatureScript"):
            self.expect_kind("number")
            self.expect(";")
        while self.token.kind != "eof":
            declaration = self.top_level()
            if declaration is not None:
                declarations.append(declaration)
        return self.node(ast.Program(self.path, declarations), start)

    def top_level(self) -> ast.Node | None:
        start = self.token
        if self.accept(";"):
            return None
        if self.check("annotation"):
            self.annotation()
            return None
        exported = self.accept("export")
        if self.check("import"):
            return self.node(self.import_(exported, None), start)
        if self.token.kind == "identifier" and self.peek().value == "::" and self.peek(2).value == "import":
            namespace = self.advance().value
            self.advance()
            return self.node(self.import_(exported, namespace), start)
        if self.accept("const"):
            name = self.identifier()
            type_ = self.type_ref() if self.accept("is") else None
            self.expect("=")
            value = self.expression()
            self.expect(";")
            return self.node(ast.ConstDecl(name, type_, value, exported), start)
        if self.check("function") or self.check("predicate"):
            kind = self.advance().value
            name = self.identifier()
            return self.node(self.function_rest(kind, name, exported), start)
        if self.accept("operator"):
            operator = self.advance().value
            # `operator[]`-style operators span tokens; FeatureScript only overloads single-token ones
            return self.node(self.function_rest("operator", operator, exported), start)
        if self.accept("enum"):
            return self.node(self.enum(exported), start)
        if self.accept("type"):
            name = self.identifier()
            self.expect("typecheck")
            typecheck = self.postfix()
            self.expect(";")
            return self.node(ast.TypeDecl(name, typecheck, exported), start)
        self.fail("expected a declaration")

    def import_(self, exported: bool, namespace: str | None) -> ast.Import:
        self.expect("import")
        self.expect("(")
        fields = {}
        while not self.check(")"):
            key = self.identifier()
            self.expect(":")
            fields[key] = _unquote(self.expect_kind("string").value)
            if not self.accept(","):
                break
        self.expect(")")
        self.expect(";")
        return ast.Import(fields.get("path", ""), exported, namespace)

    def annotation(self) -> None:
        self.expect("annotation")
        self.map_literal(self.token)

    def enum(self, exported: bool) -> ast.EnumDecl:
        name = self.identifier()
        self.expect("{")
        members = []
        while not self.check("}"):
            if self.check("annotation"):
                self.annotation()
                continue
            members.append(self.identifier())
            if not self.accept(","):
                break
        self.expect("}")
        return ast.EnumDecl(name, members, exported)

    def function_rest(self, kind: str, name: str, exported: bool) -> ast.FunctionDecl:
        params = self.params()
        returns = self.type_ref() if self.accept("returns") else None
        precondition = self.precondition()
        body = self.block()
        return ast.FunctionDecl(kind, name, params, returns, precondition, body, exported)

    def params(self) -> list[ast.Param]:
        self.expect("(")
        params = []
        while not self.check(")"):
            name = self.identifier()
            type_ = self.type_ref() if self.accept("is") else None
            params.append(ast.Param(name, type_))
            if not self.accept(","):
                break
        self.expect(")")
        return params

    def precondition(self) -> ast.Node | None:
        if not self.accept("precondition"):
            return None
        if self.check("{"):
            return self.block()
        return self.statement()

    def type_ref(self) -> ast.TypeRef:
        start = self.token
        name = self.identifier()
        if self.accept("::"):
            return self.node(ast.TypeRef(name, self.identifier()), start)
        return self.node(ast.TypeRef(None, name), start)

    # Statements

    def block(self) -> ast.Block:
        start = self.expect("{")
        statements = []
        while not self.check("}"):
            if self.token.kind == "eof":
                self.fail("expected '}'")
            statement = self.statement()
            if statement is not None:
                statements.append(statement)
        self.expect("}")
        return self.node(ast.Block(statements), start)

    def statement(self) -> ast.Node | None:
        start = self.token
        value = start.value if start.kind not in ("string", "eof") else None
        if value == ";":
            self.advance()
            return None
        if value == "{":
            return self.block()
        if value == "annotation" and self.peek().value == "{":
            self.annotation()
            return None
        if value in ("var", "const"):
            declaration = self.var_decl()
            self.expect(";")
            return declaration
        if value == "if":
            self.advance()
            self.expect("(")
            condition = self.expression()
            self.expect(")")
            then = self.statement_or_empty()
            otherwise = self.statement_or_empty() if self.accept("else") else None
            return self.node(ast.If(condition, then, otherwise), start)
        if value == "for":
            return self.for_()
        if value == "while":
            self.advance()
            self.expect("(")
            condition = self.expression()
            self.expect(")")
            return self.node(ast.While(condition, self.statement_or_empty(), False), start)
        if value == "do":
            self.advance()
            body = self.statement_or_empty()
            self.expect("while")
            self.expect("(")
            condition = self.expression()
            self.expect(")")
            self.accept(";")
            return self.node(ast.While(condition, body, True), start)
        if value == "return":
            self.advance()
            result = None if self.check(";") else self.expression()
            self.expect(";")
            return self.node(ast.Return(result), start)
        if value == "break":
            self.advance()
            self.expect(";")
            return self.node(ast.Break(), start)
        if value == "continue":
            self.advance()
            self.expect(";")
            return self.node(ast.Continue(), start)
        if value == "throw":
            self.advance()
            thrown = self.expression()
            self.expect(";")
            return self.node(ast.Throw(thrown), start)
        if value == "try" and self.peek().value != "(" and not (self.peek().value == "silent" and self.peek(2).value == "("):
            return self.try_()
        statement = self.simple_statement()
        self.expect(";")
        return statement

    def statement_or_empty(self) -> ast.Node:
        start = self.token
        statement = self.statement()
        return statement if statement is not None else self.node(ast.Block([]), start)

    def var_decl(self) -> ast.VarDecl:
        start = self.advance()
        const = start.value == "const"
        name = self.identifier()
        type_ = self.type_ref() if self.accept("is") else None
        value = self.expression() if self.accept("=") else None
        return self.node(ast.VarDecl(const, name, type_, value), start)

    def simple_statement(self) -> ast.Node:
        """An expression, or an assignment (which isn't an expression)."""
        start = self.token
        target = self.expression()
        if self.token.kind == "operator" and self.token.value in ASSIGNMENT_OPERATORS:
            op = self.advance().value
            return self.node(ast.Assign(target, op, self.expression()), start)
        return self.node(ast.ExpressionStatement(target), start)

    def for_(self) -> ast.Node:
        start = self.expect("for")
        self.expect("(")
        # for (var key, value in iterable), or for (var value in iterable)
        mark = self.index
        declared = self.accept("var") or self.accept("const")
        if self.token.kind in ("identifier", "keyword"):
            first = self.identifier()
            second = None
            if self.accept(","):
                second = self.identifier()
            if self.accept("in"):
                iterable = self.expression()
                self.expect(")")
                body = self.statement_or_empty()
                if second is None:
                    return self.node(ast.ForIn(None, first, iterable, body, declared), start)
                return self.node(ast.ForIn(first, second, iterable, body, declared), start)
        self.index = mark
        init = None
        if not self.check(";"):
            init = self.var_decl() if self.check("var") or self.check("const") else self.simple_statement()
        self.expect(";")
        condition = None if self.check(";") else self.expression()
        self.expect(";")
        update = None if self.check(")") else self.simple_statement()
        self.expect(")")
        return self.node(ast.For(init, condition, update, self.statement_or_empty()), start)

    def try_(self) -> ast.Node:
        start = self.expect("try")
        silent = self.accept("silent")
        body = self.statement_or_empty()
        catch_name = None
        catch_body = None
        if self.accept("catch"):
            if self.accept("("):
                catch_name = self.identifier()
                self.expect(")")
            catch_body = self.statement_or_empty()
        return self.node(ast.Try(body, silent, catch_name, catch_body), start)

    # Expressions

    def expression(self) -> ast.Node:
        start = self.token
        condition = self.binary(0)
        if self.accept("?"):
            then = self.expression()
            self.expect(":")
            otherwise = self.expression()
            return self.node(ast.Ternary(condition, then, otherwise), start)
        return condition

    def binary(self, level: int) -> ast.Node:
        if level == len(BINARY_LEVELS):
            return self.unary()
        start = self.token
        left = self.binary(level + 1)
        operators = BINARY_LEVELS[level]
        while self.token.kind == "operator" and self.token.value in operators:
            op = self.advance().value
            right = self.binary(level + 1)
            node = ast.Logical(op, left, right) if op in LOGICAL else ast.Binary(op, left, right)
            left = self.node(node, start)
        return left

    def unary(self) -> ast.Node:
        start = self.token
        if start.kind == "operator" and start.value in ("-", "!", "+"):
            self.advance()
            operand = self.unary()
            if start.value == "+":
                return operand
            return self.node(ast.Unary(start.value, operand), start)
        return self.power()

    def power(self) -> ast.Node:
        start = self.token
        base = self.postfix()
        if self.check("^"):
            self.advance()
            # Right associative, and binds its exponent's unary minus: 2^-1
            return self.node(ast.Binary("^", base, self.unary()), start)
        return base

    def postfix(self) -> ast.Node:
        start = self.token
        node = self.primary()
        while True:
            token = self.token
            value = token.value if token.kind not in ("string", "eof") else None
            if value == ".":
                self.advance()
                node = self.node(ast.Member(node, self.identifier(), False), start)
            elif value == "?.":
                self.advance()
                node = self.node(ast.Member(node, self.identifier(), True), start)
            elif value == "[" or value == "?[":
                self.advance()
                safe = value == "?["
                if self.accept("]"):
                    node = self.node(ast.BoxGet(node, safe), start)
                else:
                    index = self.expression()
                    self.expect("]")
                    node = self.node(ast.Index(node, index, safe), start)
            elif value == "?[]":
                self.advance()
                node = self.node(ast.BoxGet(node, True), start)
            elif value == "(":
                node = self.node(ast.Call(node, self.args()), start)
            elif value == "->":
                self.advance()
                function = self.primary_name()
                args = self.args()
                node = self.node(ast.Call(function, [node] + args), start)
            elif value == "is":
                self.advance()
                node = self.node(ast.Is(node, self.type_ref()), start)
            elif value == "as":
                self.advance()
                node = self.node(ast.As(node, self.type_ref()), start)
            else:
                return node

    def args(self) -> list[ast.Node]:
        self.expect("(")
        args = []
        while not self.check(")"):
            args.append(self.expression())
            if not self.accept(","):
                break
        self.expect(")")
        return args

    def primary_name(self) -> ast.Node:
        start = self.token
        name = self.identifier()
        if self.accept("::"):
            return self.node(ast.NamespaceName(name, self.identifier()), start)
        return self.node(ast.Name(name), start)

    def primary(self) -> ast.Node:
        start = self.token
        kind = start.kind
        value = start.value
        if kind == "number":
            self.advance()
            return self.node(ast.Literal(float(value)), start)
        if kind == "string":
            self.advance()
            return self.node(ast.Literal(_unquote(value)), start)
        if kind == "atIdentifier":
            self.advance()
            return self.node(ast.BuiltinCall(value[1:], self.args()), start)
        if kind == "keyword":
            if value == "true" or value == "false":
                self.advance()
                return self.node(ast.Literal(value == "true"), start)
            if value == "undefined":
                self.advance()
                return self.node(ast.Literal(None), start)
            if value == "inf":
                self.advance()
                return self.node(ast.Literal(float("inf")), start)
            if value == "function":
                self.advance()
                params = self.params()
                returns = self.type_ref() if self.accept("returns") else None
                precondition = self.precondition()
                body = self.block()
                return self.node(ast.Lambda(params, returns, precondition, body, False), start)
            if value == "new":
                self.advance()
                if self.identifier() != "box":
                    self.fail("expected box")
                self.expect("(")
                boxed = self.expression()
                self.expect(")")
                return self.node(ast.NewBox(boxed), start)
            if value == "switch":
                self.advance()
                self.expect("(")
                subject = self.expression()
                self.expect(")")
                self.expect("{")
                entries = []
                while not self.check("}"):
                    key = self.expression()
                    self.expect(":")
                    entries.append((key, self.expression()))
                    if not self.accept(","):
                        break
                self.expect("}")
                return self.node(ast.Switch(subject, entries), start)
            if value == "try":
                self.advance()
                silent = self.accept("silent")
                self.expect("(")
                tried = self.expression()
                self.expect(")")
                return self.node(ast.TryExpression(tried, silent), start)
        if kind in ("identifier", "keyword"):
            # A one-parameter arrow function: x => expression
            if self.peek().value == "=>":
                name = self.identifier()
                self.advance()
                return self.arrow_body([ast.Param(name, None)], None, start)
            return self.primary_name()
        if value == "(":
            if self.is_arrow_params():
                params = self.params()
                returns = self.type_ref() if self.accept("returns") else None
                self.expect("=>")
                return self.arrow_body(params, returns, start)
            self.advance()
            inner = self.expression()
            self.expect(")")
            return inner
        if value == "[":
            self.advance()
            items = []
            while not self.check("]"):
                items.append(self.expression())
                if not self.accept(","):
                    break
            self.expect("]")
            return self.node(ast.ArrayLiteral(items), start)
        if value == "{":
            return self.map_literal(start)
        self.fail("expected an expression")

    def is_arrow_params(self) -> bool:
        """Whether the `(` here starts an arrow function's parameters: `(a, b) => ...`."""
        depth = 0
        i = self.index
        while i < len(self.tokens):
            token = self.tokens[i]
            if token.kind == "eof":
                return False
            if token.value == "(" and token.kind == "punctuation":
                depth += 1
            elif token.value == ")" and token.kind == "punctuation":
                depth -= 1
                if depth == 0:
                    return self.tokens[i + 1].value in ("=>", "returns")
            i += 1
        return False

    def arrow_body(self, params: list[ast.Param], returns, start: Token) -> ast.Lambda:
        """An arrow function's body: a block, or an expression (which may be a map)."""
        if self.check("{") and not self.looks_like_map():
            return self.node(ast.Lambda(params, returns, None, self.block(), False), start)
        return self.node(ast.Lambda(params, returns, None, self.expression(), True), start)

    def looks_like_map(self) -> bool:
        """Whether the `{` here starts a map literal (`{}`, or a first key and a colon) rather than a block."""
        following = self.peek()
        if following.value == "}" and following.kind == "punctuation":
            return True
        if following.kind in ("string", "number", "identifier", "keyword") and self.peek(2).value == ":":
            return True
        return following.value == "(" and following.kind == "punctuation" and self._closing_then_colon(self.index + 1)

    def _closing_then_colon(self, open_index: int) -> bool:
        depth = 0
        for i in range(open_index, len(self.tokens)):
            token = self.tokens[i]
            if token.kind == "punctuation" and token.value in "([{":
                depth += 1
            elif token.kind == "punctuation" and token.value in ")]}":
                depth -= 1
                if depth == 0:
                    return self.tokens[i + 1].value == ":"
        return False

    def map_literal(self, start: Token) -> ast.MapLiteral:
        self.expect("{")
        entries = []
        while not self.check("}"):
            key_token = self.token
            if key_token.kind in ("identifier", "keyword") and self.peek().value == ":":
                # A bare name is a string key
                self.advance()
                key: ast.Node = self.node(ast.Literal(key_token.value), key_token)
            elif key_token.kind in ("string", "number") and self.peek().value == ":":
                self.advance()
                literal = _unquote(key_token.value) if key_token.kind == "string" else float(key_token.value)
                key = self.node(ast.Literal(literal), key_token)
            else:
                key = self.expression()
            self.expect(":")
            entries.append((key, self.expression()))
            if not self.accept(","):
                break
        self.expect("}")
        return self.node(ast.MapLiteral(entries), start)
