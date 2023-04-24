import argparse

from library.api import manager


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build FeatureScript code and push and pull it to Onshape."
    )

    parser.add_argument(
        "-l", "--log", help="whether to run in logging mode", action="store_true"
    )

    subparsers = parser.add_subparsers(required=True, dest="mode")
    update_parser = subparsers.add_parser(
        "update",
        help="perform direct updates on std",
    )

    update_parser.add_argument(
        "-v",
        "--version",
        help="update every feature studio to the latest version of the onshape std",
        action="store_true",
    )

    return parser.parse_args()


def main():
    args = parse_args()

    code_manager = manager.make_manager(logging=args.log)

    if args.mode == "update":
        if args.version:
            code_manager.update_std()


# def invalid_args() -> NoReturn:
#     print('Usage: fssync <pull|push|clean>')
#     sys.exit(1)

# def main() -> NoReturn:
#     manager = CodeManager(BACKEND_PATH)
#     if len(sys.argv) != 2:
#         invalid_args()
#     if sys.argv[1] == 'pull':
#         manager.pull()
#     elif sys.argv[1] == 'push':
#         manager.push()
#     elif sys.argv[1] == 'update-std':
#         manager.update_std()
#     else:
#         invalid_args()
#     sys.exit(0)

if __name__ == "__main__":
    main()
