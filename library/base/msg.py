import warnings
import enum as std_enum

ERROR_SNIPPET = "<INPUT_ERROR>"


class ContextType(std_enum.StrEnum):
    DEFINITION = "top level statement"
    STATEMENT = "statement"
    EXPRESSION = "expression"


def warn_context(*types: str) -> str:
    warnings.warn("Invalid context: expected " + " or ".join(types))
    return ERROR_SNIPPET


def warn_definition() -> str:
    return warn_context(ContextType.DEFINITION)


def warn_expression() -> str:
    return warn_context(ContextType.STATEMENT)


def warn_statement() -> str:
    return warn_context(ContextType.EXPRESSION)
