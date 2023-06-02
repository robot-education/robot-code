from library.base import expr

__all__ = ["definition", "var_definition"]


def export(export: bool) -> str:
    """Returns "export " if export is true. Otherwise, returns ""."""
    return "export " if export else ""


def var_definition(identifier: str, definition: str = "definition") -> expr.Expr:
    return expr.Id("{}[{}]".format(definition, identifier))


def definition(parameter_name: str, definition: str = "definition") -> expr.Expr:
    return expr.Id("{}.{}".format(definition, parameter_name))
