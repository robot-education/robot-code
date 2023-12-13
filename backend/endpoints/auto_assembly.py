import flask

from common import setup


def execute():
    api = setup.get_api()


    return {"message": "Success"}
