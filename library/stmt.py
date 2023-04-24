from library import base


class Statement(base.Node):
    """An class representing a statement which takes up one or more lines.

    By default, a statement is assumed to be an expression. However, many classes which are statements
    override this behavior.
    """

    pass
