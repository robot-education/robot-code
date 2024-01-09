def confirm_version_creation(version_name: str):
    """Prompts the python user to confirm the creation of a version."""
    value = input(
        'You are about to irreversibly create a version named "{}". Versions cannot be deleted (Y/n):'.format(
            version_name
        )
    )
    if value != "" and value.lower() != "y":
        raise ValueError("Aborted version creation.")