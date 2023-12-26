from logging.config import dictConfig
import dotenv


def load_env():
    """Loads the .env file from root.

    Throws if no variables are added."""
    if not dotenv.load_dotenv():
        raise ValueError("Failed to load .env file")

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
