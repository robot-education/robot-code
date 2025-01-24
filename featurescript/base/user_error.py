"""Defines utilities for reporting warnings and errors to users."""

import warnings
from featurescript.base import ctxt


def code_message(message: str) -> str:
    return "<BUILD WARNING: {}>".format(message.upper())


def expected_scope(*expected_scopes: ctxt.Scope) -> str:
    if len(expected_scopes) == 1:
        message = "Expected scope to be {}".format(expected_scopes[0])
    else:
        message = "Expected one of the following scopes: {}".format(
            ", ".join(expected_scopes)
        )
    warnings.warn(message)
    return code_message("INVALID SCOPE")


def assert_scope(context: ctxt.Context, scope: ctxt.Scope) -> str | None:
    if context.scope != scope:
        warnings.warn("Expected scope to be {}".format(scope))
        return code_message("INVALID SCOPE")
