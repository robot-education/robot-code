import pickle
from typing import Iterable
from library.api import api

ID_FILE = "ids.pickle"


class FeatureStudio:
    def __init__(self, name: str, path: api.Path, id: str):
        self.modified = False
        self.name = name
        self.path = path
        self.id = id


class Store:
    def __init__(self):
        data = None

    def store_feature_studios(self, feature_studios: Iterable[FeatureStudio]):
        pass
        # data = dict
        # pickle.dump(_ID_FILE)
