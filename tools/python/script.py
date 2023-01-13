from CodeManager import CodeManager
from fs_constants import BACKEND_PATH
from web_driver import WebClient

import sys
from typing import NoReturn

def invalid_args() -> NoReturn:
    print("Usage: fssync <pull|push|clean>")
    sys.exit(1)

def main() -> NoReturn:
    manager = CodeManager(BACKEND_PATH)
    if len(sys.argv) != 2:
        invalid_args()
    if sys.argv[1] == 'pull':
        manager.pull()
    elif sys.argv[1] == 'push':
        manager.push()
    elif sys.argv[1] == 'update-std':
        manager.update_std()
    elif sys.argv[1] == 'launch':
        WebClient().launch()
    elif sys.argv[1] == 'connect':
        WebClient().connect()
    else:
        invalid_args()
    sys.exit(0)

if __name__ == '__main__':
    main()