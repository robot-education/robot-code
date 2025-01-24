def confirm(message: str, abort_message: str, default: bool = False):
    """Prompts the user to confirm the release.

    Args:
        default: True to make the default (on no input) Y. Defaults to False.
    """
    default_str = "Y/n" if default else "y/N"
    value = input(message + f" Please confirm ({default_str}):").lower()
    if value == "":
        # User hit enter, execute default behavior
        if not default:
            raise ValueError(abort_message)
    elif value != "y":
        raise ValueError(abort_message)
