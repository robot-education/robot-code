import unittest

from featurescript.parse.parser import make_parser


class AssignmentStmtTestCase(unittest.TestCase):
    def setUp(self):
        self.parser = make_parser()
