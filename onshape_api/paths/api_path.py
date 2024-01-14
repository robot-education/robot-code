from typing import Type
from urllib import parse
from onshape_api.paths.paths import DocumentPath


# class ApiRequestBuilder:
#     """A builder class which can be used to construct a request to the Onshape API."""

#     def __init__(
#         self,
#         route: str,
#         path: PathBase | None = None,
#         path_type: Type[PathBase] | None = None,
#         end_route: str | None = None,
#     ) -> None:
#         self._route = route
#         if path:
#             if path_type == None:
#                 raise ValueError("The path_type must be supplied alongside the path.")
#             self._path = path_type.to_api_path(path)
#         self._end_route = end_route

#     def add_path(self, path: PathBase, path_type: type[PathBase]) -> Self:
#         self._path = path_type.to_api_path(path)
#         return self

#     @property
#     def route(self) -> str:
#         return self._route

#     @route.setter
#     def route(self, route: str) -> None:
#         self._route = route

#     @property
#     def end_route(self) -> str | None:
#         return self._end_route

#     @end_route.setter
#     def end_route(self, end_route: str) -> None:
#         self._end_route = end_route


def api_path(
    route: str,
    object: DocumentPath | None = None,
    object_type: Type[DocumentPath] | None = None,
    end_route: str | None = None,
    end_id: str | None = None,
) -> str:
    """Constructs a path suitable for consumption by an API.

    Args:
        service: The name of the base Onshape service. It may optionally start with a slash.
        path: A path to use.
        path_type: The actual type of the path. Required when path is provided.
            This is necessary to prevent issues with passing higher order paths.
        end_route: The portion of the route following the path. Note the entire end_route is appended.
            The end_route may optionally start with a slash.
        end_id: An id which is appended with a slash after end_route.
            The end_id is automatically escaped to prevent issues with slashes in the id.
    """
    api_path = "" if route.startswith("/") else "/" + route

    if object is not None:
        if object_type is None:
            raise ValueError("path_type must be provided alongside path")
        api_path += object_type.to_api_path(object)
    if end_route is not None:
        api_path += "" if end_route.startswith("/") else "/" + end_route
    if end_id is not None:
        api_path += "/" + parse.quote(end_id, safe="")
    return api_path
