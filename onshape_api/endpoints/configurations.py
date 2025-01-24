from __future__ import annotations
from dataclasses import dataclass

from onshape_api.api.api_base import Api
from onshape_api.paths.api_path import api_path
from onshape_api.paths.paths import ElementPath


def get_configuration(api: Api, element_path: ElementPath):
    return api.get(api_path("elements", element_path, ElementPath, "configuration"))


def set_configuration(
    api: Api,
    element_path: ElementPath,
    parameters: list[dict] | None,
    current_configuration: list[dict] | None,
):
    body = {
        "btType": "BTConfigurationResponse-2019",
        "configurationParameters": parameters,
        "currentConfiguration": current_configuration,
    }
    return api.post(
        api_path("elements", element_path, ElementPath, "configuration"), body=body
    )


def decode_configuration(
    api: Api, element_path: ElementPath, config_string: str
) -> dict:
    """Converts a configuration string into JSON."""
    return api.get(
        api_path(
            "elements",
            element_path,
            ElementPath,
            "configurationencodings",
            end_id=config_string,
        )
    )


def encode_configuration(
    api: Api, element_path: ElementPath, configuration: list[dict]
) -> dict:
    """Converts a configuration JSON into a string."""
    body = {"parameters": configuration}
    return api.post(
        f"/elements/d/{element_path.document_id}/e/{element_path.element_id}/configurationencodings",
        body=body,
    )


@dataclass
class ConfigurationParameterEnum:
    parameter_name: str
    parameter_id: str
    options: list[ConfigurationEnumOption]
    default_value: str = "Default"

    def to_dict(self) -> dict:
        result = {
            "btType": "BTMConfigurationParameterEnum-105",
            "defaultValue": self.default_value,
            "parameterId": self.parameter_id,
            "parameterName": self.parameter_name,
            "option": [option.to_dict() for option in self.options],
        }
        return result


@dataclass
class ConfigurationEnumOption:
    option_name: str
    option: str

    def to_dict(self) -> dict:
        return {
            "btType": "BTMEnumOption-592",
            "optionName": self.option_name,
            "option": self.option,
        }
