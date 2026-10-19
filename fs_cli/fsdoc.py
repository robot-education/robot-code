"""A copy of Onshape's FeatureScript guide (https://cad.onshape.com/FsDoc/) as Markdown, for reference offline.

`fs fsdoc` crawls the guide from its index, following links to other FsDoc pages (the guide, the language reference,
and the tutorials), and writes each page's body as Markdown to `docs/fsdoc/`, at the page's path (`uispec.html` to
`docs/fsdoc/uispec.md`). The standard library documentation (`library.html`) is left out: it's generated from std's
doc comments, which are in `std/` (and which `fs doc` shows). Images stay on Onshape: they're linked by their URLs.
"""

from __future__ import annotations

import html.parser
import posixpath
import re
from collections.abc import Callable
from urllib.parse import urljoin, urlparse

BASE = "https://cad.onshape.com/FsDoc/"
START = "index.html"
# Pages not to copy: the standard library's documentation, generated from std's doc comments
SKIPPED = frozenset(["library.html"])

_BLOCKS = frozenset(["p", "div", "ul", "ol", "li", "pre", "h1", "h2", "h3", "h4", "h5", "h6", "table", "tr", "video"])


class _Converter(html.parser.HTMLParser):
    """Turns an FsDoc page's body (its `fs-doc-body` div) into Markdown, and collects the FsDoc pages it links to."""

    def __init__(self, page: str) -> None:
        super().__init__(convert_charrefs=True)
        self.page = page
        self.links: set[str] = set()
        self.out: list[str] = []
        self.title = ""
        self._in_title = False
        # Depth of divs inside the body, or None outside it
        self._body_depth: int | None = None
        self._lists: list[list[int | None]] = []  # Each open list's next number (None for bullets)
        self._pre = False
        self._code = False
        self._href: list[str | None] = []
        self._row: list[str] | None = None
        self._rows: list[list[str]] = []
        self._cell: list[str] | None = None

    # Output

    def _write(self, text: str) -> None:
        if self._cell is not None:
            self._cell.append(text)
        else:
            self.out.append(text)

    def _block(self) -> None:
        """Starts a new block: a blank line, unless one's already there."""
        if self._cell is not None:
            self._cell.append(" ")
            return
        text = "".join(self.out)
        if text and not text.endswith("\n\n"):
            self.out.append("\n" if text.endswith("\n") else "\n\n")

    def _indent(self) -> str:
        return "   " * max(len(self._lists) - 1, 0)

    def markdown(self) -> str:
        text = "".join(self.out)
        text = re.sub(r"[ \t]+\n", "\n", text)
        text = re.sub(r"\n{3,}", "\n\n", text)
        return text.strip() + "\n"

    # Links

    def _page_link(self, href: str) -> str:
        """An href as the Markdown copy should link it: FsDoc pages to their copies, anything else absolutely."""
        url = urljoin(BASE + self.page, href)
        parsed = urlparse(url)
        if parsed.netloc == "cad.onshape.com" and parsed.path.startswith("/FsDoc/") and parsed.path.endswith(".html"):
            target = parsed.path[len("/FsDoc/") :]
            if target not in SKIPPED:
                self.links.add(target)
                relative = posixpath.relpath(target, posixpath.dirname(self.page) or ".")
                return relative[: -len(".html")] + ".md" + (f"#{parsed.fragment}" if parsed.fragment else "")
        return url

    # Parsing

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        attributes = dict(attrs)
        if tag == "title":
            self._in_title = True
        if self._body_depth is None:
            if tag == "div" and "fs-doc-body" in (attributes.get("class") or "").split():
                self._body_depth = 0
            elif tag == "a" and attributes.get("href"):
                # The sidebar's links, to the guide's other pages
                self._page_link(attributes["href"] or "")
            return
        if tag == "div":
            self._body_depth += 1
        if tag in ("h1", "h2", "h3", "h4", "h5", "h6"):
            self._block()
            self._write("#" * int(tag[1]) + " ")
        elif tag in ("p", "div"):
            self._block()
        elif tag in ("ul", "ol"):
            self._block()
            self._lists.append([1 if tag == "ol" else None])
        elif tag == "li":
            text = "".join(self.out)
            if text and not text.endswith("\n"):
                self.out.append("\n")
            number = self._lists[-1][0] if self._lists else None
            if self._lists and number is not None:
                self._lists[-1][0] = number + 1
            self._write(self._indent() + (f"{number}. " if number is not None else "- "))
        elif tag == "pre":
            self._block()
            self._write("```\n")
            self._pre = True
        elif tag == "code" and not self._pre:
            self._code = True
            self._write("`")
        elif tag in ("strong", "b"):
            self._write("**")
        elif tag in ("em", "i"):
            self._write("*")
        elif tag == "kbd":
            self._write("<kbd>")
        elif tag == "br":
            self._write("  \n" if not self._pre else "\n")
        elif tag == "a":
            href = attributes.get("href")
            self._href.append(self._page_link(href) if href else None)
            if href:
                self._write("[")
        elif tag == "img":
            src = attributes.get("src")
            if src:
                alt = attributes.get("alt") or "image"
                self._write(f"![{alt}]({urljoin(BASE + self.page, src)})")
        elif tag == "source":
            src = attributes.get("src")
            if src:
                self._write(f"[Video]({urljoin(BASE + self.page, src)})")
        elif tag == "table":
            self._block()
            self._rows = []
        elif tag == "tr":
            self._row = []
        elif tag in ("td", "th"):
            self._cell = []

    def handle_endtag(self, tag: str) -> None:
        if tag == "title":
            self._in_title = False
        if self._body_depth is None:
            return
        if tag == "div":
            if self._body_depth == 0:
                self._body_depth = None
                return
            self._body_depth -= 1
        if tag in ("h1", "h2", "h3", "h4", "h5", "h6", "p"):
            self._block()
        elif tag in ("ul", "ol"):
            if self._lists:
                self._lists.pop()
            self._block()
        elif tag == "pre":
            text = "".join(self.out)
            if not text.endswith("\n"):
                self.out.append("\n")
            self._write("```")
            self._pre = False
            self._block()
        elif tag == "code" and not self._pre:
            self._code = False
            self._write("`")
        elif tag in ("strong", "b"):
            self._write("**")
        elif tag in ("em", "i"):
            self._write("*")
        elif tag == "kbd":
            self._write("</kbd>")
        elif tag == "a":
            href = self._href.pop() if self._href else None
            if href:
                self._write(f"]({href})")
        elif tag in ("td", "th"):
            if self._row is not None and self._cell is not None:
                self._row.append(re.sub(r"\s+", " ", "".join(self._cell)).strip().replace("|", "\\|"))
            self._cell = None
        elif tag == "tr":
            if self._row is not None:
                self._rows.append(self._row)
            self._row = None
        elif tag == "table":
            if self._rows:
                width = max(len(row) for row in self._rows)
                rows = [row + [""] * (width - len(row)) for row in self._rows]
                lines = ["| " + " | ".join(rows[0]) + " |", "|" + " --- |" * width]
                lines += ["| " + " | ".join(row) + " |" for row in rows[1:]]
                self._write("\n".join(lines))
            self._rows = []
            self._block()

    def handle_data(self, data: str) -> None:
        if self._in_title:
            self.title += data
        if self._body_depth is None:
            return
        if self._pre:
            self._write(data)
            return
        text = re.sub(r"\s+", " ", data)
        if not self._code:
            # Characters Markdown would read as formatting
            text = re.sub(r"([\\*_\[\]<>])", r"\\\1", text)
        current = "".join(self.out[-1:]) if self._cell is None else "".join(self._cell[-1:])
        if current.endswith(("\n", "- ", ". ")) or not current:
            text = text.lstrip()
        self._write(text)


def convert(page: str, source: str) -> tuple[str, set[str]]:
    """An FsDoc page (its path under FsDoc, like `uispec.html`, and its HTML) as Markdown, headed with its source, and
    the FsDoc pages it links to."""
    converter = _Converter(page)
    converter.feed(source)
    converter.close()
    header = f"<!-- Copied from {BASE}{page} by `fs fsdoc`; don't edit it here. -->\n\n"
    return header + converter.markdown(), converter.links


def crawl(fetch: Callable[[str], str], start: str = START) -> dict[str, str]:
    """Every FsDoc page reachable from start (other than SKIPPED), as Markdown, by its path under FsDoc. fetch gets a
    page's HTML from its URL."""
    pages: dict[str, str] = {}
    queue = [start]
    while queue:
        page = queue.pop(0)
        if page in pages or page in SKIPPED:
            continue
        markdown, links = convert(page, fetch(BASE + page))
        pages[page] = markdown
        queue.extend(sorted(links - pages.keys()))
    return pages


def markdown_path(page: str) -> str:
    """Where a page's copy goes, under docs/fsdoc: `tutorials/x.html` to `tutorials/x.md`."""
    return page[: -len(".html")] + ".md"
