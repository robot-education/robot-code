# Collects FeatureScript tests (`*_test.fs`), run with the evaluator in fs_eval
import pathlib

import pytest

from fs_eval.pytest_plugin import pytest_collect_file  # noqa: F401


@pytest.fixture(autouse=True)
def _no_branch_override(monkeypatch):
    """The Onshape workflow sets FS_BRANCH for fs, which would put every test on its branch."""
    monkeypatch.delenv("FS_BRANCH", raising=False)

# Tests write files as they're written on Linux and macOS, with "\n" line endings. On Windows, text written without
# newline="" gets "\r\n", which fs keeps (it syncs files byte for byte), so every test's fixtures would differ.
_write_text = pathlib.Path.write_text


def _write_text_lf(self, data, encoding=None, errors=None, newline=None):
    return _write_text(self, data, encoding, errors, "" if newline is None else newline)


pathlib.Path.write_text = _write_text_lf
