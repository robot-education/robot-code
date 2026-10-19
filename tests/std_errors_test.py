from fs_cli.std_errors import main, search, std_errors

SOURCE = """FeatureScript 2960;
/**
 * @internal
 * The built-in user-facing errors available within Onshape.
 */
export enum ErrorStringEnum
{
    NO_ERROR,
    /* Could not shell part with selections. */
    SHELL_FAILED,
    /* Selected offset is too large,
       decrease the value. */
    EXTRUDE_OFFSET_TOO_DEEP
}
"""


def test_each_error_has_the_comment_above_it_as_its_message():
    errors = std_errors(SOURCE)
    assert errors == {
        "SHELL_FAILED": "Could not shell part with selections.",
        "EXTRUDE_OFFSET_TOO_DEEP": "Selected offset is too large, decrease the value.",
    }


def test_search_needs_every_word_in_the_message_or_name():
    errors = std_errors(SOURCE)
    assert search(["shell"], errors) == [("SHELL_FAILED", "Could not shell part with selections.")]
    assert search(["TOO", "decrease"], errors) == [("EXTRUDE_OFFSET_TOO_DEEP", "Selected offset is too large, decrease the value.")]
    assert search(["shell", "decrease"], errors) == []


def test_std_has_its_errors_messages():
    errors = std_errors()
    assert len(errors) > 1000
    assert errors["BOOLEAN_NON_MANIFOLD_RESULT"] == "Boolean operation would result in non-manifold body."


def test_main_prints_matches(capsys):
    assert main(["non-manifold", "boolean"]) == 0
    assert "BOOLEAN_NON_MANIFOLD_RESULT: Boolean operation would result in non-manifold body." in capsys.readouterr().out
