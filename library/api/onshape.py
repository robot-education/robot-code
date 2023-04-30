import argparse

from library.api import manager, conf


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build FeatureScript code and push and pull from Onshape."
    )

    parser.add_argument(
        "-l", "--log", help="whether to run in logging mode", action="store_true"
    )

    subparsers = parser.add_subparsers(required=True, dest="action")
    subparsers.add_parser(
        "update-versions",
        help="update every feature studio to the latest version of the onshape std",
    )
    subparsers.add_parser(
        "clean",
        help="delete everything under storage_path and code_path",
    )

    push_parser = subparsers.add_parser("push", help="push code to Onshape")
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
    code_manager = manager.make_manager(config, logging=args.log)

    if args.action == "update-versions":
        code_manager.update_versions()
    elif args.action == "pull":
        code_manager.pull(args.force)
    elif args.action == "push":
        code_manager.push(args.force)
    elif args.action == "clean":
        code_manager.clean()


if __name__ == "__main__":
    main()
