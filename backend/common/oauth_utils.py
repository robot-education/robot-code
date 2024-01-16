import enum
import os
import flask
from requests_oauthlib import OAuth2Session


client_id = os.getenv("OAUTH_CLIENT_ID")
client_secret = os.getenv("OAUTH_CLIENT_SECRET")

base_url = "https://oauth.onshape.com/oauth"
auth_base_url = base_url + "/authorize"
token_url = base_url + "/token"
