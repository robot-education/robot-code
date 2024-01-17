import http


class BackendException(Exception):
    """An unexpected exception thrown by the backend."""

    def __init__(
        self,
        message: str,
        status_code: http.HTTPStatus = http.HTTPStatus.INTERNAL_SERVER_ERROR,
    ):
        super().__init__()
        self.message = message
        self.status_code = status_code

    def to_dict(self):
        return {"type": "BACKEND_EXCEPTION", "message": self.message}


class UserException(Exception):
    """An exception thrown by the backend which is presumably the user's fault."""

    def __init__(
        self,
        message: str,
        status_code: http.HTTPStatus = http.HTTPStatus.BAD_REQUEST,
    ):
        super().__init__()
        self.message = message
        self.status_code = status_code

    def to_dict(self):
        return {"type": "USER_EXCEPTION", "message": self.message}
