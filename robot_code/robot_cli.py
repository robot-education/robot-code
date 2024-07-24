"""Defines a command line parser for the `robot` command.
"""

import argparse
from onshape_api.api import key_api
from robot_code.release import release

"""
Versioning is handled as follows:
A major release is a major event, typically constituting major changes to documentation/significant changes to functionality.
A minor release introduces minor new features.
A patch introduces bug fixes.

Numbering scheme:
script - v[major release number].[minor release number].[patch number](-beta.[beta number])
"""


def get_release_parser() -> argparse.ArgumentParser:
    release_parser = argparse.ArgumentParser(add_help=False)

    release_parser.add_argument(
        "script",
        help="The camel case name of the script to update",
    )

    release_parser.add_argument(
        "--version",
        "-v",
        choices=["major", "minor", "patch"],
        help="The version type of the script being released",
    )

    release_parser.add_argument(
        "--beta",
        "-b",
        action="store_true",
        help="Whether the release is a prerelease",
    )

    release_parser.add_argument(
        "--description",
        "-d",
        help="A brief internal-only description of the changes made",
        default="",
    )
    return release_parser


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Release Robot FeatureScripts.")

    parser.add_argument(
        "-l", "--log", help="Whether to run with logging enabled", action="store_true"
    )

    subparsers = parser.add_subparsers(required=True, dest="action")

    release_parser = get_release_parser()
    subparsers.add_parser(
        "release",
        help="release a new version of a FeatureScript",
        parents=[release_parser],
        description="Release a FeatureScript to the frontend document.",
    )
    subparsers.add_parser(
        "test-release",
        parents=[release_parser],
        help="release a test FeatureScript",
        description="Release a test FeatureScript in the test-frontend document.",
    )

    return parser.parse_args()


def main():
    args = parse_args()
    if args.action in ["release", "test-release"]:
        api = key_api.make_key_api()
        release(
            api,
            args.script,
            args.description,
            version_type=args.version,
            is_prerelease=args.beta,
            test=(args.action == "test-release"),
        )


if __name__ == "__main__":
    main()
