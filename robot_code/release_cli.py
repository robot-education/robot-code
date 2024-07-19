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
script - v[major release number].[minor release number].[patch number][-beta]
"""


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Release Robot FeatureScripts.")

    parser.add_argument(
        "-l", "--log", help="Whether to run with logging enabled", action="store_true"
    )

    parser.add_argument(
        "--test",
        "-t",
        action="store_true",
        help="Whether to use the test documents",
    )

    # subparsers = parser.add_subparsers(required=True, dest="action")
    # release_parser = subparsers.add_parser(
    #     "release",
    #     help="release a new version of a FeatureScript",
    #     description="Release a FeatureScript by adding it to the frontend document.",
    # )
    parser.add_argument(
        "--studio",
        "-s",
        help="The name of the studio to release",
        required=True,
    )
    parser.add_argument(
        "--version",
        "-v",
        choices=["major", "minor", "patch"],
        help="The version type of the script being released",
    )
    parser.add_argument(
        "--beta",
        "-b",
        action="store_true",
        help="Whether the release is a prerelease",
    )
    parser.add_argument(
        "--description",
        "-d",
        help="A brief description of the changes made",
        required=True,
    )

    return parser.parse_args()


def main():
    args = parse_args()
    api = key_api.make_key_api()
    release(
        api,
        args.studio,
        args.description,
        version_type=args.version,
        is_prerelease=args.beta,
        test=args.test,
    )


if __name__ == "__main__":
    main()
