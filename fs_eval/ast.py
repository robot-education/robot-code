"""The syntax tree `fs_eval.parser` builds: plain nodes, each with the source position it starts at (`at`, a
(path, line) pair, for error messages)."""

from __future__ import annotations


class Node:
    __slots__ = ("at",)


# Expressions


class Literal(Node):
    __slots__ = ("value",)

    def __init__(self, value):
        self.value = value


class Name(Node):
    __slots__ = ("name",)

    def __init__(self, name: str):
        self.name = name


class NamespaceName(Node):
    """`Namespace::name`."""

    __slots__ = ("namespace", "name")

    def __init__(self, namespace: str, name: str):
        self.namespace = namespace
        self.name = name


class Member(Node):
    __slots__ = ("target", "name", "safe")

    def __init__(self, target: Node, name: str, safe: bool):
        self.target = target
        self.name = name
        self.safe = safe


class Index(Node):
    __slots__ = ("target", "index", "safe")

    def __init__(self, target: Node, index: Node, safe: bool):
        self.target = target
        self.index = index
        self.safe = safe


class BoxGet(Node):
    """`box[]`."""

    __slots__ = ("target", "safe")

    def __init__(self, target: Node, safe: bool):
        self.target = target
        self.safe = safe


class Call(Node):
    __slots__ = ("function", "args")

    def __init__(self, function: Node, args: list[Node]):
        self.function = function
        self.args = args


class BuiltinCall(Node):
    """`@name(args)`."""

    __slots__ = ("name", "args")

    def __init__(self, name: str, args: list[Node]):
        self.name = name
        self.args = args


class Unary(Node):
    __slots__ = ("op", "operand")

    def __init__(self, op: str, operand: Node):
        self.op = op
        self.operand = operand


class Binary(Node):
    __slots__ = ("op", "left", "right")

    def __init__(self, op: str, left: Node, right: Node):
        self.op = op
        self.left = left
        self.right = right


class Logical(Node):
    """`&&`, `||`, and `??`, which don't evaluate their right side unless they need it."""

    __slots__ = ("op", "left", "right")

    def __init__(self, op: str, left: Node, right: Node):
        self.op = op
        self.left = left
        self.right = right


class Ternary(Node):
    __slots__ = ("condition", "then", "otherwise")

    def __init__(self, condition: Node, then: Node, otherwise: Node):
        self.condition = condition
        self.then = then
        self.otherwise = otherwise


class TypeRef(Node):
    """A type, as written after `is` or `as`: a name, maybe in a namespace."""

    __slots__ = ("namespace", "name")

    def __init__(self, namespace: str | None, name: str):
        self.namespace = namespace
        self.name = name


class Is(Node):
    __slots__ = ("value", "type")

    def __init__(self, value: Node, type: TypeRef):
        self.value = value
        self.type = type


class As(Node):
    __slots__ = ("value", "type")

    def __init__(self, value: Node, type: TypeRef):
        self.value = value
        self.type = type


class MapLiteral(Node):
    """Entries are (key, value): a key is a `Literal` (a bare name's string, too) or an expression."""

    __slots__ = ("entries",)

    def __init__(self, entries: list[tuple[Node, Node]]):
        self.entries = entries


class ArrayLiteral(Node):
    __slots__ = ("items",)

    def __init__(self, items: list[Node]):
        self.items = items


class Param:
    __slots__ = ("name", "type")

    def __init__(self, name: str, type: TypeRef | None):
        self.name = name
        self.type = type


class Lambda(Node):
    """A function expression: `function(params) { ... }`, or `(params) => expression` (whose body is an
    expression, not a block)."""

    __slots__ = ("params", "returns", "precondition", "body", "expression")

    def __init__(self, params: list[Param], returns, precondition, body, expression: bool):
        self.params = params
        self.returns = returns
        self.precondition = precondition
        self.body = body
        self.expression = expression


class NewBox(Node):
    __slots__ = ("value",)

    def __init__(self, value: Node):
        self.value = value


class Switch(Node):
    """`switch (subject) { key : value, ... }`: the value of the first key equal to the subject (only it's
    evaluated), or undefined."""

    __slots__ = ("subject", "entries")

    def __init__(self, subject: Node, entries: list[tuple[Node, Node]]):
        self.subject = subject
        self.entries = entries


class TryExpression(Node):
    """`try(expression)`: its value, or undefined if it throws."""

    __slots__ = ("value", "silent")

    def __init__(self, value: Node, silent: bool):
        self.value = value
        self.silent = silent


# Statements


class Block(Node):
    __slots__ = ("statements",)

    def __init__(self, statements: list[Node]):
        self.statements = statements


class VarDecl(Node):
    __slots__ = ("const", "name", "type", "value")

    def __init__(self, const: bool, name: str, type: TypeRef | None, value: Node | None):
        self.const = const
        self.name = name
        self.type = type
        self.value = value


class Assign(Node):
    """`target op value`, where op is `=` or a compound assignment like `+=`."""

    __slots__ = ("target", "op", "value")

    def __init__(self, target: Node, op: str, value: Node):
        self.target = target
        self.op = op
        self.value = value


class ExpressionStatement(Node):
    __slots__ = ("value",)

    def __init__(self, value: Node):
        self.value = value


class If(Node):
    __slots__ = ("condition", "then", "otherwise")

    def __init__(self, condition: Node, then: Node, otherwise: Node | None):
        self.condition = condition
        self.then = then
        self.otherwise = otherwise


class For(Node):
    __slots__ = ("init", "condition", "update", "body")

    def __init__(self, init, condition, update, body):
        self.init = init
        self.condition = condition
        self.update = update
        self.body = body


class ForIn(Node):
    """`for (var value in iterable)`, or `for (var key, value in iterable)` (an array's index and item, or a
    map's key and value; one name over a map gets `{ key, value }` entries)."""

    __slots__ = ("key", "value", "iterable", "body", "declared")

    def __init__(self, key: str | None, value: str, iterable: Node, body: Node, declared: bool):
        self.key = key
        self.value = value
        self.iterable = iterable
        self.body = body
        self.declared = declared


class While(Node):
    __slots__ = ("condition", "body", "do")

    def __init__(self, condition: Node, body: Node, do: bool):
        self.condition = condition
        self.body = body
        self.do = do


class Return(Node):
    __slots__ = ("value",)

    def __init__(self, value: Node | None):
        self.value = value


class Break(Node):
    __slots__ = ()


class Continue(Node):
    __slots__ = ()


class Throw(Node):
    __slots__ = ("value",)

    def __init__(self, value: Node):
        self.value = value


class Try(Node):
    __slots__ = ("body", "silent", "catch_name", "catch_body")

    def __init__(self, body: Node, silent: bool, catch_name: str | None, catch_body: Node | None):
        self.body = body
        self.silent = silent
        self.catch_name = catch_name
        self.catch_body = catch_body


# Top level


class Import(Node):
    __slots__ = ("path", "exported", "namespace")

    def __init__(self, path: str, exported: bool, namespace: str | None):
        self.path = path
        self.exported = exported
        self.namespace = namespace


class FunctionDecl(Node):
    """A function, predicate, or operator (whose name is its operator, like `+`)."""

    __slots__ = ("kind", "name", "params", "returns", "precondition", "body", "exported")

    def __init__(self, kind: str, name: str, params: list[Param], returns, precondition, body: Block, exported: bool):
        self.kind = kind
        self.name = name
        self.params = params
        self.returns = returns
        self.precondition = precondition
        self.body = body
        self.exported = exported


class ConstDecl(Node):
    __slots__ = ("name", "type", "value", "exported")

    def __init__(self, name: str, type: TypeRef | None, value: Node, exported: bool):
        self.name = name
        self.type = type
        self.value = value
        self.exported = exported


class EnumDecl(Node):
    __slots__ = ("name", "members", "exported")

    def __init__(self, name: str, members: list[str], exported: bool):
        self.name = name
        self.members = members
        self.exported = exported


class TypeDecl(Node):
    __slots__ = ("name", "typecheck", "exported")

    def __init__(self, name: str, typecheck: Node, exported: bool):
        self.name = name
        self.typecheck = typecheck
        self.exported = exported


class Program(Node):
    __slots__ = ("path", "declarations")

    def __init__(self, path: str, declarations: list[Node]):
        self.path = path
        self.declarations = declarations
