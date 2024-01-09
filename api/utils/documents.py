from api import api_path


def map_documents(
    elements: list[dict], document_path: api_path.DocumentPath
) -> dict[str, api_path.ElementPath]:
    """Constructs a mapping of document names to their paths."""
    return dict(
        (
            element["name"],
            api_path.ElementPath(document_path, element["id"]),
        )
        for element in elements
    )
