from flask import current_app as app
from flask import request, jsonify

import flask_restful as restful
from flask_restful import reqparse

# import pandas as pd

class AutoAssembly(restful.Resource):
    def post(self):
        app.logger.info("Success!")
        return {"message": "Success"}