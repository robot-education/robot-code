import asyncio

import flask
from backend import auto_assembly, generate_assembly

app = flask.Flask(__name__)


@app.route("/auto-assembly", methods=["POST"])
def auto_assembly_route():
    return auto_assembly.execute()


@app.route("/generate-assembly", methods=["POST"])
def generate_assembly_route():
    return generate_assembly.execute()


if __name__ == "__main__":
    app.run(
        port=8080,
        debug=True,
        # ssl_context="adhoc",
        # ssl_context=("myCA.pem", "myCA.key"),
    )
