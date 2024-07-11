from onshape_api.paths.paths import url_to_element_path, url_to_instance_path

STD_PATH = url_to_instance_path(
    "https://cad.onshape.com/documents/12312312345abcabcabcdeff/w/a855e4161c814f2e9ab3698a"
)
"""The path to the Onshape std."""

STD_STUDIO_PATH = url_to_element_path(
    "https://cad.onshape.com/documents/12312312345abcabcabcdeff/w/a855e4161c814f2e9ab3698a/e/82a05608e31d46b6988ffde7"
)
"""A path to an arbitrary tab in the Onshape std."""

IDENTITY_TRANSFORM = [
    1.0,
    0.0,
    0.0,
    0.0,
    0.0,
    1.0,
    0.0,
    0.0,
    0.0,
    0.0,
    1.0,
    0.0,
    0.0,
    0.0,
    0.0,
    1.0,
]
"""The identity transform."""
