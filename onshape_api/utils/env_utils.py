import dotenv


def load_env() -> bool:
    """Loads a .env file from the current directory or one of its parents.

    Variables which are already set in the environment take precedence, so credentials
    may also be supplied directly (e.g. in CI). Returns True if a .env file was loaded.
    """
    return dotenv.load_dotenv(dotenv.find_dotenv(usecwd=True))
