#! /bin/bash
sudo apt-get update && sudo apt-get install -y man vim less
pipx install poetry
# poetry config virtualenvs.create false
poetry install