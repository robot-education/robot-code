"""Drives the language server over stdio, like VS Code does."""

import json
import subprocess
import sys
import threading
import queue

import pytest

SOURCE = "\n".join(
    [
        "FeatureScript 2909;",
        'import(path : "onshape/std/geometry.fs", version : "2909.0");',
        "export enum MyOption { ONE, TWO }",
        "function helper(value is number) returns number",
        "{",
        "    return value;",
        "}",
        "const a = helper(1);",
        "const b = MyOption.",
        "const c = (1;",
    ]
)
URI = "file:///tmp/test.fs"


class Client:
    def __init__(self) -> None:
        self.process = subprocess.Popen(
            [sys.executable, "-m", "fs_lsp"],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
        )
        self.messages: queue.Queue = queue.Queue()
        self.next_id = 0
        threading.Thread(target=self._read, daemon=True).start()

    def _read(self) -> None:
        stream = self.process.stdout
        while True:
            headers = {}
            while (line := stream.readline()) not in (b"\r\n", b""):
                key, value = line.decode().split(":", 1)
                headers[key.strip().lower()] = value.strip()
            if not headers:
                return
            body = stream.read(int(headers["content-length"]))
            self.messages.put(json.loads(body))

    def send(self, message: dict) -> None:
        body = json.dumps({"jsonrpc": "2.0", **message}).encode()
        self.process.stdin.write(b"Content-Length: %d\r\n\r\n" % len(body) + body)
        self.process.stdin.flush()

    def notify(self, method: str, params: dict) -> None:
        self.send({"method": method, "params": params})

    def wait_for(self, predicate) -> dict:
        while True:
            message = self.messages.get(timeout=20)
            if predicate(message):
                return message

    def request(self, method: str, params: dict):
        self.next_id += 1
        request_id = self.next_id
        self.send({"id": request_id, "method": method, "params": params})
        response = self.wait_for(
            lambda m: m.get("id") == request_id and "method" not in m
        )
        assert "error" not in response, response
        return response["result"]

    def close(self) -> None:
        self.request("shutdown", None)
        self.notify("exit", None)
        self.process.wait(timeout=10)


@pytest.fixture(scope="module")
def client():
    client = Client()
    result = client.request(
        "initialize", {"processId": None, "rootUri": None, "capabilities": {}}
    )
    client.capabilities = result["capabilities"]
    client.notify("initialized", {})
    client.notify(
        "textDocument/didOpen",
        {
            "textDocument": {
                "uri": URI,
                "languageId": "featurescript",
                "version": 1,
                "text": SOURCE,
            }
        },
    )
    yield client
    client.close()


def position(line: int, character: int) -> dict:
    return {
        "textDocument": {"uri": URI},
        "position": {"line": line, "character": character},
    }


def test_capabilities(client):
    capabilities = client.capabilities
    for key in [
        "semanticTokensProvider",
        "hoverProvider",
        "definitionProvider",
        "referencesProvider",
        "documentSymbolProvider",
        "foldingRangeProvider",
        "completionProvider",
    ]:
        assert capabilities.get(key), key
    assert "mapKey" in capabilities["semanticTokensProvider"]["legend"]["tokenTypes"]


def test_diagnostics_are_published(client):
    message = client.wait_for(
        lambda m: m.get("method") == "textDocument/publishDiagnostics"
    )
    assert any("never closed" in d["message"] for d in message["params"]["diagnostics"])


def test_semantic_tokens(client):
    result = client.request(
        "textDocument/semanticTokens/full", {"textDocument": {"uri": URI}}
    )
    assert len(result["data"]) > 0 and len(result["data"]) % 5 == 0


def test_hover_definition_and_references(client):
    hover = client.request("textDocument/hover", position(7, 11))
    assert "helper(value is number) returns number" in hover["contents"]["value"]

    definition = client.request("textDocument/definition", position(7, 11))
    assert definition[0]["targetRange"]["start"] == {"line": 3, "character": 9}

    references = client.request(
        "textDocument/references",
        {**position(3, 10), "context": {"includeDeclaration": True}},
    )
    assert len(references) == 2


def test_completion(client):
    items = client.request("textDocument/completion", position(8, 19))
    assert [item["label"] for item in items] == ["ONE", "TWO"]


def test_document_symbols(client):
    symbols = client.request(
        "textDocument/documentSymbol", {"textDocument": {"uri": URI}}
    )
    assert {"MyOption", "helper", "a"} <= {s["name"] for s in symbols}


def test_cross_file_navigation(tmp_path):
    """Definitions, hovers, and diagnostics across files in a repo with an fs config."""
    utils_id = "a" * 24
    (tmp_path / "pyproject.toml").write_text(
        '[tool.fs]\nbackend = "https://cad.onshape.com/documents/d/w/w"\n'
    )
    code_dir = tmp_path / "featurescripts"
    code_dir.mkdir()
    (code_dir / "utils.fs").write_text(
        "FeatureScript 2909;\n/** Doubles. */\nexport function double(x is number) { return x * 2; }\n"
    )
    feature_source = (
        f'FeatureScript 2909;\nimport(path : "{utils_id}", version : "v");\n'
        "export const a = double(1) + missing;\n"
    )
    (code_dir / "feature.fs").write_text(feature_source)
    (tmp_path / "fs-studios.json").write_text(
        json.dumps({"version": 1, "studios": {utils_id: "utils.fs"}})
    )
    uri = (code_dir / "feature.fs").as_uri()
    client = Client()
    try:
        client.request("initialize", {"processId": None, "rootUri": tmp_path.as_uri(), "capabilities": {}})
        client.notify("initialized", {})
        client.notify(
            "textDocument/didOpen",
            {"textDocument": {"uri": uri, "languageId": "featurescript", "version": 1, "text": feature_source}},
        )
        message = client.wait_for(lambda m: m.get("method") == "textDocument/publishDiagnostics")
        assert [d["code"] for d in message["params"]["diagnostics"]] == ["undefined"]

        at = {"textDocument": {"uri": uri}, "position": {"line": 2, "character": 18}}
        [link] = client.request("textDocument/definition", at)
        assert link["targetUri"].endswith("/utils.fs")
        assert link["targetRange"]["start"] == {"line": 2, "character": 16}
        hover = client.request("textDocument/hover", at)["contents"]["value"]
        assert "Doubles." in hover and "utils.fs" in hover

        import_at = {"textDocument": {"uri": uri}, "position": {"line": 1, "character": 16}}
        assert "utils.fs" in client.request("textDocument/hover", import_at)["contents"]["value"]

        symbols = client.request("workspace/symbol", {"query": "doub"})
        assert [symbol["name"] for symbol in symbols] == ["double"]
    finally:
        client.close()


def test_renames_keep_studios_and_imports(tmp_path):
    """Renaming files and folders in the editor updates fs-studios.json and imports by path."""
    utils_id = "a" * 24
    (tmp_path / "pyproject.toml").write_text(
        '[tool.fs]\nbackend = "https://cad.onshape.com/documents/d/w/w"\n'
    )
    code_dir = tmp_path / "featurescripts"
    (code_dir / "core").mkdir(parents=True)
    (code_dir / "core" / "utils.fs").write_text("FeatureScript 2909;\n")
    feature = 'FeatureScript 2909;\nimport(path : "core/utils.fs", version : "");\n'
    (code_dir / "feature.fs").write_text(feature)
    (tmp_path / "fs-studios.json").write_text(
        json.dumps({"version": 1, "studios": {utils_id: "core/utils.fs"}})
    )
    old, new = (code_dir / "core").as_uri(), (code_dir / "shared").as_uri()
    client = Client()
    try:
        # Like VS Code's
        client_capabilities = {"workspace": {"fileOperations": {"willRename": True, "didRename": True}}}
        capabilities = client.request(
            "initialize",
            {"processId": None, "rootUri": tmp_path.as_uri(), "capabilities": client_capabilities},
        )["capabilities"]
        assert capabilities["workspace"]["fileOperations"]["willRename"]
        assert capabilities["workspace"]["fileOperations"]["didRename"]
        client.notify("initialized", {})

        edit = client.request("workspace/willRenameFiles", {"files": [{"oldUri": old, "newUri": new}]})
        [(uri, [change])] = edit["changes"].items()
        assert uri.endswith("/feature.fs")
        assert change == {
            "range": {"start": {"line": 1, "character": 15}, "end": {"line": 1, "character": 28}},
            "newText": "shared/utils.fs",
        }

        (code_dir / "core").rename(code_dir / "shared")
        client.notify("workspace/didRenameFiles", {"files": [{"oldUri": old, "newUri": new}]})
        # Notifications aren't answered, so make a request to know it's been handled
        client.request("workspace/symbol", {"query": ""})
        studios = json.loads((tmp_path / "fs-studios.json").read_text())["studios"]
        assert studios == {utils_id: "shared/utils.fs"}
    finally:
        client.close()


def test_signature_help(client):
    # In helper's parentheses, on its first argument
    result = client.request("textDocument/signatureHelp", position(7, 17))
    [signature] = result["signatures"]
    assert signature["label"] == "helper(value is number) returns number"
    assert signature["parameters"][0]["label"] == [7, 22]
    assert result["activeParameter"] == 0
    # Not in a call
    assert client.request("textDocument/signatureHelp", position(8, 4)) is None


WIDGET = """FeatureScript 1;
import(path : "onshape/std/common.fs", version : "1.0");

export enum Placement
{
    annotation { "Name" : "Edge" }
    EDGE,
    annotation { "Name" : "Point" }
    POINT
}

annotation { "Feature Type Name" : "Widget" }
export const widget = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Placement", "UIHint" : ["HORIZONTAL_ENUM"] }
        definition.placement is Placement;

        if (definition.placement == Placement.POINT)
        {
            annotation { "Name" : "Point" }
            definition.point is Query;
        }
    }
    {
    });
"""


def test_render_ui(tmp_path):
    """The extension's dialog preview renders through the server, with unsaved changes."""
    (tmp_path / "pyproject.toml").write_text('[tool.fs]\nbackend = "https://cad.onshape.com/documents/d/w/w"\n')
    code_dir = tmp_path / "featurescripts"
    code_dir.mkdir()
    (tmp_path / "std").mkdir()
    (tmp_path / "std" / "common.fs").write_text("FeatureScript 1;\n")
    path = code_dir / "widget.fs"
    path.write_text("FeatureScript 1;\n")
    uri = path.as_uri()
    client = Client()
    try:
        client.request("initialize", {"processId": None, "rootUri": tmp_path.as_uri(), "capabilities": {}})
        client.notify("initialized", {})
        client.notify(
            "textDocument/didOpen",
            {"textDocument": {"uri": uri, "languageId": "featurescript", "version": 1, "text": WIDGET}},
        )
        result = client.request(
            "featurescript/renderUi", {"uri": uri, "settings": [["placement", "POINT"], ["nothing", "1"]], "theme": "light"}
        )
        assert result["features"] == ["widget"] and result["feature"] == "widget"
        assert "data-os-theme='light'" in result["html"]
        assert "os-param-query-list-label os-grow'>Point<" in result["html"]
        assert result["warnings"] == ["nothing isn't shown, so --set nothing did nothing."]

        # Unsaved changes are rendered
        client.notify(
            "textDocument/didChange",
            {
                "textDocument": {"uri": uri, "version": 2},
                "contentChanges": [{"text": WIDGET.replace('"Name" : "Point" }\n            definition', '"Name" : "Spot" }\n            definition')}],
            },
        )
        result = client.request("featurescript/renderUi", {"uri": uri, "settings": [["placement", "POINT"]]})
        assert "os-param-query-list-label os-grow'>Spot<" in result["html"]
    finally:
        client.close()
