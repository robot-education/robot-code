import flask
import flask_restful as restful

from backend.auto_assembly import auto_assembly
import logging

app = flask.Flask(__name__)
api = restful.Api(app)

app.logger.setLevel("DEBUG")

api.add_resource(auto_assembly.AutoAssembly, "/autoassembly")

if __name__ == "__main__":
    app.run(port=8080, debug=True)
