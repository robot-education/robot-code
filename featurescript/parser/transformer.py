import lark


class TreeToAst(lark.Transformer):
    """Converts a Lark parse tree into a FeatureScript AST."""

    def list(self, items):
        return list(items)
