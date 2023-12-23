import http


class ApiException(Exception):
    def __init__(
        self, message: str, status_code: http.HTTPStatus = http.HTTPStatus.BAD_REQUEST
    ):
        super().__init__()
        self.message = message
        self.status_code = status_code

    def to_dict(self):
        return {"message": self.message}
