"""Pulls the FeatureScript scripts used to parse assemblies from Onshape."""

import pathlib
from typing import Iterable

from library.api import api_base, conf
from library.api.endpoints import documents, feature_studios
from library.transform import transform


def insert_code(function: str, statements: Iterable[str]) -> str:
    """Inserts one or more statements into the top of the function using a simplistic algorithm."""
    lines = function.splitlines()
    for stmt in statements:
        lines.insert(2, stmt)
    return "\n".join(lines)


def main():
    onshape = api_base.ApiKey(logging=False)
    config = conf.Config()
    backend_path = config.get_document("backend")
    if not backend_path:
        raise ValueError("Failed to find backend?")
    studio_path_map = documents.get_feature_studios(onshape, backend_path)

    json_code = feature_studios.pull_code(
        onshape, studio_path_map["assemblyScriptUtils.fs"].path
    )
    assembly_script_code = feature_studios.pull_code(
        onshape, studio_path_map["assemblyScript.fs"].path
    )

    to_json = transform.extract_lambda(json_code, "toJson")
    parse_id = [
        transform.to_lambda(transform.extract_function(assembly_script_code, name))
        # order matters to ensure lambdas see each other
        for name in ["parseMateConnectorId", "parseId"]
    ]
    functions = [to_json, *parse_id]

    for name in ["parseBase", "parseTarget"]:
        function = transform.extract_function(assembly_script_code, name)
        function = insert_code(
            # Remove function name from result scripts
            "function" + (function.strip().removeprefix("function " + name)),
            functions,
        )  # .replace("\\", "\\\\")
        pathlib.Path("backend/scripts/" + name + ".fs").write_text(function)


if __name__ == "__main__":
    main()
