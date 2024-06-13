import unittest

from featurescript.parser import ast
from featurescript.parser.ast import make_parser


class TestImports(unittest.TestCase):
    def setUp(self):
        self.parser = make_parser()

    def test_featurescript_version(self):
        fs_ast = self.parser.parse("FeatureScript 111;")
        self.assertEqual(fs_ast, ast.Studio(111, [], []))

    def test_imports(self):
        code = """
        FeatureScript 111;
        export namespace::import(path: "path", version: "version");
        import(path: "path2", version: "version2");
        """
        fs_ast = self.parser.parse(code)
        self.assertEqual(
            fs_ast,
            ast.Studio(
                111,
                [
                    ast.Import("path", "version", True, "namespace"),
                    ast.Import("path2", "version2", False),
                ],
                [],
            ),
        )
