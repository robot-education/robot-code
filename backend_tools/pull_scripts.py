"""Pulls the FeatureScript scripts used to parse assemblies from Onshape."""

import pathlib
from onshape_api.api import key_api
from onshape_api.endpoints import feature_studios
from featurescript import endpoints, feature_studio
from featurescript.transform import transform
from featurescript import conf


def insert_code(function: str, code: list[str]) -> str:
    lines = function.splitlines()
    for stmt in code:
        lines.insert(2, stmt)
    return "\n".join(lines)


def main():
    onshape = key_api.make_key_api()
    config = conf.Config()
    backend_path = config.get_document("backend")
    if not backend_path:
        raise ValueError("Unexpectedly failed to find backend document")
    studio_path_map = feature_studio.get_feature_studios(onshape, backend_path)

    json_code = feature_studios.pull_code(onshape, studio_path_map["toJson.fs"].path)
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
            "function" + (function.strip().removeprefix("function " + name)), functions
        )  # .replace("\\", "\\\\")
        pathlib.Path("backend/scripts/" + name + ".fs").write_text(function)


if __name__ == "__main__":
    main()
