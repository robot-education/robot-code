"""Defines a command line parser for the `robot` command.
"""
import argparse

from featurescript.api import api_base, conf
from robot_code.script import release

"""
Versioning is handled as follows:
A major release is a major event, typically constituting major changes to documentation/significant changes to functionality.
A minor release introduces minor new features.
A patch introduces bug fixes.

Numbering scheme:
script - [major release number].[minor release number].[patch number]
"""


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Perform robot code specific actions.")

    parser.add_argument(
        "-l", "--log", help="run with logging enabled", action="store_true"
    )

    subparsers = parser.add_subparsers(required=True, dest="action")
    release_parser = subparsers.add_parser(
        "release",
        help="release a new version of a FeatureScript",
        description="Release a FeatureScript by adding it to the frontend document.",
    )
    release_parser.add_argument(
        "--studio",
        "-s",
        help="The name of the studio to release",
        required=True,
    )
    release_parser.add_argument(
        "--version",
        "-v",
        choices=["major", "minor", "patch"],
        help="The version type of the script being released",
        required=True,
    )
    release_parser.add_argument(
        "--description",
        "-d",
        help="A brief description of the changes made",
        required=True,
    )

    return parser.parse_args()


def main():
    args = parse_args()
    api = api_base.ApiKey(logging=args.log)

    if args.action == "release":
        match args.version:
            case "major":
                type = release.VersionType.MAJOR
            case "minor":
                type = release.VersionType.MINOR
            case "patch":
                type = release.VersionType.PATCH
            case _:
                raise RuntimeError("Invalid version was not caught")
        release.release(api, args.studio, type, args.description)


if __name__ == "__main__":
    main()
