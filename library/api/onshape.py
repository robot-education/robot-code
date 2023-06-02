"""Defines a command line parser for the `onshape` command.
"""
import argparse

from library.api import api_base, manager, conf


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build FeatureScript code and push and pull from Onshape."
    )

    parser.add_argument(
        "-l", "--log", help="run with logging enabled", action="store_true"
    )

    subparsers = parser.add_subparsers(required=True, dest="action")
    update_parser = subparsers.add_parser(
        "update-versions",
        help="update every local feature studio to the latest version of the onshape std",
        description="update every local feature studio to the latest version of the onshape std.",
    )
    update_parser.add_argument(
        "-p", "--push", action="store_true", help="push code to Onshape after updating"
    )

    subparsers.add_parser(
        "clean",
        help="delete everything under storage_path and code_path",
        description="Delete everything under storage_path and code_path.",
    )

    build_parser = subparsers.add_parser(
        "build",
        help="build everything under code_gen_path",
        description="Build everything under code_gen_path.",
    )
    build_parser.add_argument(
        "-p", "--push", action="store_true", help="push code to Onshape after building"
    )

    push_parser = subparsers.add_parser(
        "push",
        help="push local code to Onshape",
        description="Push local code to Onshape.",
    )
    push_parser.add_argument(
        "--force", action="store_true", help="force push, ignoring conflicts"
    )

    pull_parser = subparsers.add_parser("pull", help="pull code from Onshape")
    pull_parser.add_argument(
        "--force", action="store_true", help="force pull, ignoring conflicts"
    )

    return parser.parse_args()


def main():
    args = parse_args()
    config = conf.Config()
    if args.action == "clean":
        manager.clean(config)
        return

    api_obj = api_base.Api(logging=args.log)
    command_line_manager = manager.CommandLineManager(config, api_obj)

    if args.action == "update-versions":
        command_line_manager.update_versions()
        if args.push:
            command_line_manager.push()
    elif args.action == "build":
        command_line_manager.build()
        if args.push:
            command_line_manager.push()
    elif args.action == "pull":
        command_line_manager.pull(args.force)
    elif args.action == "push":
        command_line_manager.push(args.force)


if __name__ == "__main__":
    main()
