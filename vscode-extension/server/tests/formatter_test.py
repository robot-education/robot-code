"""Tests for the FeatureScript formatter."""

import pathlib
import textwrap

import pytest

from fs_lsp.formatter import _same_tokens, edits, format_source, is_generated

STD = pathlib.Path(__file__).parents[3] / "std"


def check(source: str, expected: str) -> None:
    source, expected = textwrap.dedent(source).lstrip("\n"), textwrap.dedent(expected).lstrip("\n")
    assert format_source(source) == expected
    assert format_source(expected) == expected


def unchanged(source: str) -> None:
    check(source, source)


def test_blocks_are_indented():
    check(
        """
        function f(x)
        {
        if (x)
          {
                return 1;
           }
          else
        {
        for (var i = 0; i < 3; i += 1)
        {
        x += i;
        }
        }
        }
        """,
        """
        function f(x)
        {
            if (x)
            {
                return 1;
            }
            else
            {
                for (var i = 0; i < 3; i += 1)
                {
                    x += i;
                }
            }
        }
        """,
    )


def test_block_after_a_multi_line_header_lines_up_with_its_first_line():
    unchanged(
        """
        export function f(context is Context, id is Id,
            definition is map) returns map
        {
            return definition;
        }
        """
    )


def test_feature_preconditions_and_bodies():
    check(
        """
        export const f = defineFeature(function(context is Context, id is Id, definition is map)
            precondition
              {
                    definition.a is boolean;
              }
              {
                    return;
              });
        """,
        """
        export const f = defineFeature(function(context is Context, id is Id, definition is map)
            precondition
            {
                definition.a is boolean;
            }
            {
                return;
            });
        """,
    )


def test_continuations_keep_their_indentation():
    # Std indents calls with a map argument both of these ways
    unchanged(
        """
        function f()
        {
            opExtrude(context, id, {
                        "entities" : faces,
                        "direction" : direction
                    });
            opExtrude(context, id, {
                    "entities" : faces
                });
            const long = first +
                second;
        }
        """
    )


def test_continuations_move_with_their_statement():
    check(
        """
        function f()
        {
          opExtrude(context, id, {
                      "entities" : faces
                  });
        }
        """,
        """
        function f()
        {
            opExtrude(context, id, {
                        "entities" : faces
                    });
        }
        """,
    )


def test_function_literal_bodies_are_indented_from_their_line():
    check(
        """
        function f()
        {
            const g = mapArray(values, function(value)
                {
                return value * 2;
                });
            const h = function(x)
                {
                        return x;
                };
        }
        """,
        """
        function f()
        {
            const g = mapArray(values, function(value)
                {
                    return value * 2;
                });
            const h = function(x)
                {
                    return x;
                };
        }
        """,
    )


def test_maps_arent_blocks():
    unchanged(
        """
        const a = { "x" : 1, "y" : { "z" : 2 } };
        function f(x)
        {
            return switch (x) {
                    "a" : 1,
                    "b" : 2
                };
        }
        """
    )


def test_nested_bare_blocks():
    check(
        """
        function f()
        {
            {
            {
            g();
            }
            }
        }
        """,
        """
        function f()
        {
            {
                {
                    g();
                }
            }
        }
        """,
    )


def test_annotations():
    check(
        """
        annotation { "Feature Type Name" : "Thing",
                "Icon" : ICON }
        export const thing = defineFeature(function(context is Context, id is Id, definition is map)
            precondition
            {
            annotation { "Name" : "Width" }
            isLength(definition.width, LENGTH_BOUNDS);
            }
            {
            });
        """,
        """
        annotation { "Feature Type Name" : "Thing",
                "Icon" : ICON }
        export const thing = defineFeature(function(context is Context, id is Id, definition is map)
            precondition
            {
                annotation { "Name" : "Width" }
                isLength(definition.width, LENGTH_BOUNDS);
            }
            {
            });
        """,
    )


def test_comments_are_indented_with_the_code():
    check(
        """
        function f()
        {
        // A comment
          /**
           * A doc comment
           */
        g(); // Trailing,   with its gap kept
        }
        """,
        """
        function f()
        {
            // A comment
            /**
             * A doc comment
             */
            g(); // Trailing,   with its gap kept
        }
        """,
    )


def test_spacing():
    check(
        """
        function f(a,b , c)
        {
            if(a==b&&c){
                g( a , [ 1,2 ] );
            }
            const m = {"a":1, "b"  :  2};
            const e = {};
            return h (a)?b:c;
        }
        """,
        """
        function f(a, b, c)
        {
            if (a == b && c) {
                g(a, [1, 2]);
            }
            const m = { "a" : 1, "b"  : 2 };
            const e = {};
            return h(a) ? b : c;
        }
        """,
    )


def test_arithmetic_and_unary_operators_are_left_as_written():
    unchanged(
        """
        const a = -b + c*d - (e/2);
        const f = x -> y;
        """
    )


def test_aligned_columns_are_kept():
    unchanged(
        """
        const BOUNDS = {
                    (meter)      : [0, 0.005, 500],
                    (centimeter) : 0.5,
                    (inch)       : 0.25
                } as LengthBoundSpec;
        """
    )


def test_strings_are_untouched():
    unchanged(
        """
        const a = "  spaces ,inside  ";
        const b = "a string
              over lines   ";
        """
    )


def test_whitespace():
    assert format_source("a;   \n\n\n\n\nb;\t\n\n") == "a;\n\n\nb;\n"
    assert format_source("a;") == "a;\n"
    assert format_source("a;\r\nb;\r\n") == "a;\nb;\n"
    assert format_source("function f()\n{\n\tg();\n}\n") == "function f()\n{\n    g();\n}\n"


def test_only_whitespace_changes():
    # Unbalanced braces (while typing) give odd indentation, but nothing else changes
    source = "function f()\n{\n    if (a)\n    {\n        g(\n}\n"
    assert _same_tokens(source, format_source(source))


@pytest.mark.parametrize(
    "source",
    [
        "",
        "a;",
        "a;\n",
        "a;\n\n\n\n",
        "\n\n\n\na;",
        "a;\r\nb;\r\n",
        "function f()\n{\ng();\n}",
        "function f()\n{\n  g();\n\n\n\n  h();  \n}\n\n\n",
    ],
)
def test_edits_give_the_formatted_source(source):
    result = source
    for start, end, text in reversed(edits(source)):
        result = result[:start] + text + result[end:]
    assert result == format_source(source)


def test_edits_only_change_changed_lines():
    source = "function f()\n{\n    g();\n  h();\n    i();\n}\n"
    assert edits(source) == [(24, 31, "    h();\n")]


def test_generated_files():
    assert is_generated("featurescripts/frame/frameTables.gen.fs")
    assert not is_generated("featurescripts/frame/robotFrame.fs")


@pytest.mark.skipif(not STD.is_dir(), reason="needs the copy of std")
def test_std_is_nearly_formatted():
    """Formatting std's hand-written files, which this formats like, changes few lines (mostly fixing its own
    inconsistent indentation), only whitespace, and formats them the same again."""
    lines = changed = 0
    for path in sorted(STD.glob("*.fs")):
        if is_generated(path.name):
            continue
        source = path.read_text(encoding="utf-8")
        formatted = format_source(source)
        assert _same_tokens(source, formatted), path.name
        assert format_source(formatted) == formatted, path.name
        lines += source.count("\n")
        changed += sum(source.count("\n", start, end) for start, end, _ in edits(source))
    assert lines > 10000
    assert changed / lines < 0.025, f"{changed} of {lines} lines changed"
