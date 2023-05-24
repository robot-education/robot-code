from typing import Self
from typing_extensions import override
from library.base import ctxt, node, imp

_FS_HEADER = "FeatureScript {};\n"
_GENERATED_HEADER = "\n/* Automatically generated file -- DO NOT EDIT */\n\n"


class Studio(node.ParentNode):
    """Represents a feature studio which is generated from scratch."""

    def __init__(
        self, studio_name: str, document_name: str, import_common: bool = True
    ) -> None:
        self.studio_name = studio_name
        self.document_name = document_name
        self.std_imports = []
        self.imports = []
        self.children: list[node.TopStatement] = []
        if import_common:
            self.std_imports.append(imp.Import("common.fs"))

    def add_import(
        self, studio_name: str, document_name: str | None = None, export: bool = False
    ) -> Self:
        node = imp.Import(studio_name, document_name, export)
        if document_name:
            self.imports.append(node)
        else:
            self.std_imports.append(node)
        return self

    def add(self, *nodes: node.TopStatement) -> Self:
        self.children.extend(nodes)
        return self

    def _build_header(self, context: ctxt.Context) -> str:
        header = _FS_HEADER.format(context.std_version)
        if len(self.std_imports) > 0:
            header += "".join(node.run_build_top(context) for node in self.std_imports)
        if len(self.imports) > 0:
            if len(self.std_imports) > 0:  # separate sections
                header += "\n"
            header += "".join(node.run_build_top(context) for node in self.imports)
        return header

    @override
    def build(self, context: ctxt.Context) -> str:
        """The top-level build function for the studio."""
        header = self._build_header(context) + _GENERATED_HEADER
        body = "\n".join(node.run_build_top(context) for node in self.children)
        return header + body


_BEGIN_GENERATION = "// Begin generated section\n"
_END_GENERATION = "// End generated section\n"


class PartialStudio(Studio):
    """A feature studio which is only partially defined in Python."""

    def inject_studio(self, context: ctxt.Context, code: str) -> str:
        """Injects the studio into the code of an existing feature studio.

        This function works as follows:
        1. The code is parsed into generated code sections seperated by the blocks of non-generated code.
        2.
        """
        sections = parse_code_sections(code)
        top_section = self.build(context)
        for section in sections:
            if section[1]:
                section = top_section

        return code

    @override
    def build(self, context: ctxt.Context) -> str:
        """The top-level build function for the studio."""
        header = _BEGIN_GENERATION + self._build_header(context)
        body = (
            "\n".join(
                node.run_build_top(context) for node in self.children  # type: ignore
            )
            + _END_GENERATION
        )
        return header + body


def parse_code_sections(code: str) -> list[str]:
    lines = code.splitlines(True)
    prev, i = 0, 0
    generated = False
    result = []
    for line in lines:
        search_str = _END_GENERATION if generated else _BEGIN_GENERATION
        if line.find(search_str) != -1:
            result.append(("".join(line[prev:i]), generated))
            prev = i
            generated = not generated
        i += 1
    result.append(("".join(lines[prev:i]), generated))
    return result
