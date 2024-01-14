from onshape_api.paths.paths import ElementPath, InstancePath


def confirm_version_creation(version_name: str):
    """Prompts the python user to confirm the creation of a version."""
    value = input(
        'You are about to irreversibly create a version named "{}". Versions cannot be deleted (Y/n):'.format(
            version_name
        )
    )
    if value != "" and value.lower() != "y":
        raise ValueError("Aborted version creation.")


def map_documents(
    elements: list[dict], document_path: InstancePath
) -> dict[str, ElementPath]:
    """Constructs a mapping of document names to their paths.

    Can be used on the responses from certain endpoints.
    """
    return dict(
        (
            element["name"],
            ElementPath.from_path(document_path, element["id"]),
        )
        for element in elements
    )


# red_color = "\033[91m"
# end_color = "\033[0m"

# configure the logging module
# config = {
#     "version": 1,
#     "disable_existing_loggers": False,
#     "formatters": {
#         "stdout": {
#             "format": "[%(levelname)s]: %(asctime)s - %(message)s",
#             "datefmt": "%x %X",
#         },
#         "stderr": {
#             "format": red_color
#             + "[%(levelname)s]: %(asctime)s - %(message)s"
#             + end_color,
#             "datefmt": "%x %X",
#         },
#     },
#     "handlers": {
#         "stdout": {
#             "class": "logging.StreamHandler",
#             "level": "DEBUG",
#             "formatter": "stdout",
#         },
#         "stderr": {
#             "class": "logging.StreamHandler",
#             "level": "ERROR",
#             "formatter": "stderr",
#         },
#     },
#     "loggers": {
#         "info": {"handlers": ["stdout"], "level": "INFO", "propagate": True},
#         "error": {"handlers": ["stderr"], "level": "ERROR", "propagate": False},
#     },
# }

# # Configure logger
# dictConfig(config)
