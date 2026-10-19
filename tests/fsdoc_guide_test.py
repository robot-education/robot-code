"""Tests for fs fsdoc's copy of Onshape's FeatureScript guide (fs_cli/fsdoc.py), on pages written here."""

from fs_cli.fsdoc import BASE, convert, crawl

PAGE = """<!doctype html><html><head><title>Feature UI</title></head><body>
<div class="fs-sidebar"><a href="index.html">FeatureScript</a><a href="library.html">Library</a>
<a href="tutorials/slot.html">Tutorial</a></div>
<div class="fs-doc-body"><h1>Feature UI</h1>
<p>Call <code>defineFeature</code> with a <em>precondition</em>, as in <a href="syntax.html#if">if</a>
or <a href="https://example.com/x">elsewhere</a>; a * b.</p>
<div class="fs-block-row"><pre><code>export const f = defineFeature(function(context is Context)
    precondition { }
</code></pre></div>
<ul><li>One<li>Two<ol><li>First</li></ol></ul>
<table><tr><th>Name</th><th>Type</th></tr><tr><td>depth</td><td><code>a | b</code></td></tr></table>
<img src="images/dialog.png" alt="Dialog">
</div></body></html>"""


def test_convert():
    markdown, links = convert("uispec.html", PAGE)
    assert markdown.startswith(f"<!-- Copied from {BASE}uispec.html by `fs fsdoc`; don't edit it here. -->\n\n# Feature UI\n")
    assert (
        "Call `defineFeature` with a *precondition*, as in [if](syntax.md#if) or "
        "[elsewhere](https://example.com/x); a \\* b." in markdown
    )
    assert "```\nexport const f = defineFeature(function(context is Context)\n    precondition { }\n```" in markdown
    assert "- One\n- Two\n\n   1. First" in markdown
    assert "| Name | Type |\n| --- | --- |\n| depth | `a \\| b` |" in markdown
    assert f"![Dialog]({BASE}images/dialog.png)" in markdown
    # The sidebar isn't copied, but its links are followed; the library's documentation isn't
    assert "Tutorial" not in markdown
    assert links == {"index.html", "syntax.html", "tutorials/slot.html"}


def test_crawl_links_pages_relative_to_each_other():
    pages = {
        "index.html": '<div class="fs-doc-body"><a href="tutorials/slot.html">Slot</a></div>',
        "tutorials/slot.html": '<div class="fs-doc-body"><a href="../index.html">Home</a><a href="next.html">Next</a></div>',
        "tutorials/next.html": '<div class="fs-doc-body"><p>Done</p></div>',
    }
    fetched = []

    def fetch(url: str) -> str:
        fetched.append(url)
        return pages[url[len(BASE) :]]

    copies = crawl(fetch)
    assert sorted(copies) == sorted(pages)
    assert len(fetched) == 3
    assert "[Home](../index.md)" in copies["tutorials/slot.html"]
    assert "[Next](next.md)" in copies["tutorials/slot.html"]
    assert "[Slot](tutorials/slot.md)" in copies["index.html"]
