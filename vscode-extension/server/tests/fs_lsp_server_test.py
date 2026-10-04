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
    assert "function helper" in hover["contents"]["value"]

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
