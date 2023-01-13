from selenium import webdriver
from selenium.webdriver.common.desired_capabilities import DesiredCapabilities
from selenium.common.exceptions import WebDriverException
import pickle
from typing import Optional

class PersistentRemote(webdriver.Firefox):
    def __init__(self, session_id: Optional[str] = None) -> None:
        super(PersistentRemote, self).__init__()

        if session_id is not None:
            self.close() # close or quit?
            self.session_id = session_id
            try:
                self._driver.title
            except WebDriverException:
                raise WebDriverException("Session {} no longer valid.".format(self.session_id))
        # else:
            # self.session_id = self.get_session_id()

class WebClient():
    def __init__(self) -> None:
        self._SELENIUM_SESSION_PICKLE = ".storage/selenium-session.pkl"
    
    def launch(self) -> None:
        self._browser = PersistentRemote()
        # pickle.dump(self._browser.session_id, open(self._SELENIUM_SESSION_PICKLE, 'wb'))
    
    def connect(self) -> None:
        try:
            session_id = pickle.load(open(self._SELENIUM_SESSION_PICKLE, 'rb'))
            self._browser = PersistentRemote(session_id)
        except (FileNotFoundError, WebDriverException):
            self.create()
            # create or throw error
        # browser.get("https://www.google.com") 