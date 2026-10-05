"""Writes index.html, which shows every icon: the named ones by folder, then the numbered ones.

Run it after naming or adding icons: `uv run python onshape_icons/make_index.py`.
"""

import html
import pathlib
import re

FOLDER = pathlib.Path(__file__).parent

HEAD = """<!doctype html>
<meta charset="utf-8">
<title>Onshape icons</title>
<style>
  body { margin: 16px; font: 11px sans-serif; background: #888; color: #fff; }
  body.light { background: #fff; color: #333; }
  h2 { font-size: 13px; margin: 16px 0 6px; }
  main { display: flex; flex-wrap: wrap; gap: 2px; }
  figure { margin: 0; width: 64px; height: 64px; display: flex; flex-direction: column; align-items: center;
    justify-content: center; border: 1px solid rgba(0, 0, 0, 0.15); }
  figure.named { width: 112px; }
  figcaption { max-width: 100%; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  img { max-width: 48px; max-height: 40px; min-width: 16px; }
</style>
<label><input type="checkbox" onchange="document.body.classList.toggle('light', this.checked)"> White background</label>
"""


def figure(path: pathlib.Path, caption: str, named: bool) -> str:
    source = html.escape(path.relative_to(FOLDER).as_posix())
    return (
        f"<figure{' class=\"named\"' if named else ''}><img src=\"{source}\" alt=\"\">"
        f"<figcaption title=\"{html.escape(caption)}\">{html.escape(caption)}</figcaption></figure>"
    )


def main() -> None:
    parts = [HEAD]
    for folder in sorted(path for path in FOLDER.iterdir() if path.is_dir()):
        parts.append(f"<h2>{html.escape(folder.name)}/</h2>\n<main>\n")
        parts.extend(figure(path, path.stem, True) + "\n" for path in sorted(folder.glob("*.svg")))
        parts.append("</main>\n")
    numbered = sorted(FOLDER.glob("svg-*.svg"), key=lambda path: int(re.sub(r"\D", "", path.stem)))
    parts.append("<h2>Unnamed</h2>\n<main>\n")
    parts.extend(figure(path, path.stem.removeprefix("svg-"), False) + "\n" for path in numbered)
    parts.append("</main>\n")
    (FOLDER / "index.html").write_text("".join(parts))


if __name__ == "__main__":
    main()
