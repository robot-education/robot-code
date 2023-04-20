from src.library import base, expr


class Statements(base.Node):
    def __init__(self, *statements) -> None:
        self.statements = statements

    def __str__(self) -> str:
        return "\n".join(base.enter(*self.statements))


class Statement(base.Node):
    def __init__(self, expr: expr.Expr) -> None:
        self.expr = expr

    def __str__(self) -> str:
        return str(self.expr) + ";\n"
