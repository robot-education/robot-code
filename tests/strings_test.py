from fs_cli.strings import user_strings

SOURCE = """FeatureScript 1;
export enum Placement
{
    annotation { "Name" : "Edge" }
    EDGE
}

annotation { "Feature Type Name" : "Widget", "Feature Type Description" : "Makes " ~ "widgets." }
export const widget = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Depth", "Description" : "How deep it is" }
        isLength(definition.depth, LENGTH_BOUNDS);
    }
    {
        verifyNonemptyQuery(context, definition, "edges", "Select edges.");
        if (definition.depth < 0)
        {
            throw regenError("The " ~ name ~ " is too short.", ["depth"]);
        }
        reportFeatureWarning(context, id, "Careful.");
    });

export const table = { "name" : "size", "displayName" : "Size", "entries" : { "A" : { "displayName" : "Size" } } };
"""


def test_user_strings():
    assert [(found.line, found.kind, found.text) for found in user_strings(SOURCE)] == [
        (4, "enum value", '"Edge"'),
        (8, "feature name", '"Widget"'),
        (8, "feature description", '"Makes " ~ "widgets."'),
        (12, "name", '"Depth"'),
        (12, "description", '"How deep it is"'),
        (16, "empty selection", '"Select edges."'),
        (19, "error", '"The " ~ name ~ " is too short."'),
        (21, "warning", '"Careful."'),
        # Each level name once
        (24, "lookup table level", '"Size"'),
    ]
