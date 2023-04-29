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
    update_parser = subparsers.add_parser(
        "update-version",
        help="update every feature studio to the latest version of the onshape std",
    )

    # update_parser.add_argument(
    #     "-v",
    #     "--version",
    #     action="store_true",
    # )

    subparsers.add_parser("push", help="push code to Onshape")

    subparsers.add_parser("pull", help="pull code from Onshape")

    return parser.parse_args()


def main():
    args = parse_args()
    config = conf.Config()
    code_manager = manager.make_manager(config, logging=args.log)

    if args.action == "update-version":
        pass
        # code_manager.update_std()
    elif args.action == "pull":
        code_manager.pull()
    # elif args.action == "push":
    #     pass


if __name__ == "__main__":
    main()
