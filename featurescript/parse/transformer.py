import lark


class TreeToAst(lark.Transformer):
    """Transforms a Lark AST to """
    def list(self, items):
        return list(items)
