import threading
import time

from fs_cli.state import state_lock


def test_a_second_holder_waits_for_the_first(tmp_path):
    studios_path = tmp_path / "fs-studios.json"
    events = []
    first_holds = threading.Event()

    def first():
        with state_lock(studios_path):
            events.append("first locked")
            first_holds.set()
            time.sleep(0.2)
            events.append("first unlocking")

    def second():
        first_holds.wait()
        with state_lock(studios_path):
            events.append("second locked")

    threads = [threading.Thread(target=first), threading.Thread(target=second)]
    for thread in threads:
        thread.start()
    for thread in threads:
        thread.join()
    assert events == ["first locked", "first unlocking", "second locked"]
    assert (tmp_path / ".fs.lock").exists()
