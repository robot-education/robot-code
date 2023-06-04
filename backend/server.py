import flask
from backend.auto_assembly import auto_assembly

app = flask.Flask(__name__)


@app.route("/autoassembly", methods=["POST"])
def auto_assembly_route():
    return auto_assembly.execute()


if __name__ == "__main__":
    app.run(
        port=8080,
        debug=True,
        # ssl_context="adhoc",
        # ssl_context=("myCA.pem", "myCA.key"),
    )
