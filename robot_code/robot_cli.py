"""Defines a command line parser for the `robot` command.
"""

import argparse
from onshape_api.api import key_api
from robot_code.release import release, sync_versions

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

    # release_parser.add_argument(
    #     "--version",
    #     "-v",
    #     choices=["major", "minor", "patch"],
    #     help="The version type of the script being released",
    # )

    version_group = release_parser.add_mutually_exclusive_group()
    version_group.add_argument(
        "--major",
        dest="version",
        action="store_const",
        const="major",
        help="Mark this release as a new major release",
    )

    version_group.add_argument(
        "--minor",
        dest="version",
        action="store_const",
        const="minor",
        help="Mark this release as a new minor release",
    )

    version_group.add_argument(
        "--patch",
        dest="version",
        action="store_const",
        const="patch",
        help="Mark this release as a new patch",
    )

    release_parser.add_argument(
        "-b",
        "--beta",
        action="store_true",
        help="Whether the release is a prerelease",
    )

    release_parser.add_argument(
        "-d",
        "--description",
        help="A brief internal-only description of the changes made",
        default="",
    )
    release_parser.add_argument(
        "--make-version",
        help="True to also create the version in the Frontend, releasing the script immediately",
        action="store_true",
    )
    return release_parser


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Release Robot FeatureScripts.", prog="scripts/robot.sh"
    )

    parser.add_argument(
        "-l", "--log", help="whether to run with logging enabled", action="store_true"
    )

    action_parsers = parser.add_subparsers(required=True, dest="action")

    release_parser = get_release_parser()
    action_parsers.add_parser(
        "release",
        help="release a new version of a FeatureScript",
        parents=[release_parser],
        description="Release a FeatureScript to the frontend document.",
    )
    action_parsers.add_parser(
        "test-release",
        parents=[release_parser],
        help="release a test FeatureScript",
        description="Release a test FeatureScript in the test-frontend document.",
    )
    action_parsers.add_parser(
        "sync-versions",
        help="sync versions to the frontend document",
        description="Create all missing versions in the frontend document.",
    )
    action_parsers.add_parser(
        "update-fs-versions",
        help="Update all Feature Studios to the latest FeatureScript version",
        description="Update all Feature Studios to the latest FeatureScript version.",
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
            create_frontend_version=args.make_version,
            test=(args.action == "test-release"),
        )
    elif args.action == "sync-versions":
        api = key_api.make_key_api()
        sync_versions(api)


if __name__ == "__main__":
    main()
