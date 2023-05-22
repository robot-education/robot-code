# kudos to https://stackoverflow.com/a/50193944/10717280

import setuptools

setuptools.setup(
    name="library",
    version="0.1",
    packages=setuptools.find_namespace_packages(),
)

# setuptools.setup(name="robot_library", version="0.1", packages=["robot_library"])
