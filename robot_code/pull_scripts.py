import pathlib

from library.api import api_base, conf
from library.api.endpoint import documents, feature_studios
from library.transform import transform


def insert_code(function: str, code: list[str]) -> str:
    lines = function.splitlines()
    for stmt in code:
        lines.insert(2, stmt)
    return "\n".join(lines)


def main():
    onshape = api_base.ApiKey(logging=False)
    config = conf.Config()
    backend_path = config.get_document("backend")
    if not backend_path:
        raise ValueError("Failed to find backend?")
    studio_path_map = documents.get_studios(onshape, backend_path)

    json_code = feature_studios.get_code(onshape, studio_path_map["toJson.fs"].path)
    assembly_script_code = feature_studios.get_code(
        onshape, studio_path_map["assemblyScript.fs"].path
    )

    to_json = transform.extract_lambda(json_code, "toJson")
    parse_id = [
        transform.to_lambda(transform.extract_function(assembly_script_code, name))
        # order matters to ensure lambdas see each other
        for name in ["parseMateConnectorId", "parseId"]
    ]
    functions = [to_json, *parse_id]

    evaluate_functions = {
        name: transform.extract_function(assembly_script_code, name)
        for name in ["parseBase", "parseTarget"]
    }

    evaluate_functions = [
        insert_code(
            "function" + (value.strip().removeprefix("function " + key)), functions
        )
        for key, value in evaluate_functions.items()
    ]

    out = "export const parseBaseScript = `{}`\nexport const parseTargetScript = `{}`\n".format(
        *evaluate_functions
    ).replace(
        "\\", "\\\\"
    )
    pathlib.Path("./assembly-script.ts").write_text(out)


if __name__ == "__main__":
    main()
