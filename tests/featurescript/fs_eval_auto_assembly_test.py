"""Tests of Robot pulley's and Robot sprocket's library parts (see docs/auto-assembly.md): which of a vendor's products
each pulley or sprocket they build is a stand-in for, and what they say when the vendor has none."""

import json
import pathlib

import pytest

from fs_cli import frcdesign_library
from fs_eval import to_python
from fs_eval.pytest_plugin import shared_evaluator

PULLEY = "released/belt/robotPulley.fs"
SPROCKET = "chain/robotSprocket.fs"
CODE = pathlib.Path(__file__).parents[2] / "featurescripts"


def evaluate(expression: str, module: str, **variables):
    return shared_evaluator().eval(expression, module, **variables)


def path(vendor: str, bore: str) -> str:
    """A product table's path, choosing a vendor and a bore."""
    return f'{{ "vendor" : {json.dumps(vendor)}, "bore" : {json.dumps(bore)} }} as LookupTablePath'


def pulley(vendor: str, bore: str, belt: str, teeth: int, width: str) -> str:
    """`withLibraryProduct` of a pulley in Manual."""
    definition = f'{{ "creationMethod" : CreationMethod.MANUAL, "pulleyProduct" : {path(vendor, bore)} }}'
    pulley_definition = f'{{ "plane" : XY_PLANE, "beltType" : BeltType.{belt}, "beltWidth" : {width}, "teeth" : {teeth} }} as PulleyDefinition'
    return f"withLibraryProduct({definition}, {pulley_definition})"


@pytest.mark.parametrize(
    "vendor,teeth,width,part_number",
    [
        ("WCP", 18, "9 * millimeter", "WCP-0563"),
        # The narrowest at least as wide as the belt: The Thrifty Bot's are 9.5 and 18.5 mm
        ("The Thrifty Bot", 18, "9 * millimeter", "TTB-0346"),
        ("The Thrifty Bot", 18, "15 * millimeter", "TTB-0347"),
    ],
)
def test_a_pulley_is_its_vendors_of_its_teeth_and_width(vendor, teeth, width, part_number):
    product = evaluate(pulley(vendor, '1/2" Hex', "_5_MM_HTD", teeth, width) + ".product", PULLEY)
    assert to_python(product)["partNumber"] == part_number


def caught(call: str) -> str:
    """An expression for the error `call` throws."""
    return f"(function() {{ try {{ {call}; }} catch (error) {{ return error; }} }})()"


def test_a_vendor_without_the_pulley_says_what_it_has():
    expression = pulley("WCP", '1/2" Hex', "_5_MM_HTD", 17, "9 * millimeter")
    assert evaluate(expression + ".missing", PULLEY) is True
    assert evaluate(expression + ".product", PULLEY) is None
    definition = f'{{ "creationMethod" : CreationMethod.MANUAL, "pulleyProduct" : {path("WCP", '1/2" Hex')} }}'
    error = to_python(evaluate(caught(f"reportMissingProducts(newContext(), {definition}, [{expression}], [qNothing()])"), PULLEY))
    assert error["customMessage"] == (
        'WCP has no 17T 5mm HTD pulley 9 mm or wider with a 1/2" Hex bore: '
        "its 5mm HTD ones have 15T, 18T, 20T, 24T, 30T, 36T (9 mm, 15 mm wide)."
    )
    assert error["faultyParameters"] == ["pulleyProduct", "pulleyTeeth"]


def test_a_vendor_without_the_sprocket_says_what_it_has():
    expression = sprocket("WCP", '1/2" Hex', "ANSI_35", 16)
    definition = f'{{ "creationMethod" : SprocketCreationMethod.CHAIN, "sprocketProduct" : {path("WCP", '1/2" Hex')} }}'
    error = to_python(evaluate(caught(f"reportMissingProducts({definition}, [{expression}], [qNothing()])"), SPROCKET))
    assert error["customMessage"] == 'WCP has no 16T #35 sprocket with a 1/2" Hex bore: its #35 ones have 9T, 12T, 15T, 18T.'
    assert error["faultyParameters"] == ["sprocketProduct"]


def test_width_strings():
    assert evaluate("widthString(9.5 * millimeter)", PULLEY) == "9.5 mm"
    assert evaluate("widthString(0.5 * inch)", PULLEY) == "0.5 in"


def sprocket(vendor: str, bore: str, chain: str, teeth: int) -> str:
    definition = f'{{ "sprocketProduct" : {path(vendor, bore)} }}'
    return f'withLibraryProduct({definition}, {{ "plane" : XY_PLANE, "chainType" : ChainType.{chain}, "teeth" : {teeth} }})'


def test_a_sprocket_is_its_vendors_first_of_its_teeth():
    # WCP's 16t #25 sprockets are aluminum and steel (and a double, left out): the first
    product = to_python(evaluate(sprocket("WCP", '1/2" Hex', "ANSI_25", 16) + ".product", SPROCKET))
    assert product["partNumber"] == "WCP-0578"
    # #25H chain goes on #25 sprockets
    assert to_python(evaluate(sprocket("WCP", '1/2" Hex', "ANSI_25H", 16) + ".product", SPROCKET))["partNumber"] == "WCP-0578"
    assert evaluate(sprocket("WCP", '1/2" Hex', "ANSI_35", 16) + ".missing", SPROCKET) is True


@pytest.mark.parametrize("snapshot", ["released/belt/vendor/frcdesign-pulleys.json", "chain/vendor/frcdesign-sprockets.json"])
def test_products_insert_every_parameter_in_full(snapshot):
    data = json.loads((CODE / snapshot).read_text())
    parameters = {part["name"]: part["parameters"] for part in data["parts"]}
    for product in frcdesign_library.products(data):
        studio = parameters[product.studio]
        assert set(product.configuration) == {parameter["id"] for parameter in studio}
        for parameter in studio:
            if parameter["name"] in frcdesign_library.SIMPLIFIED_PARAMETERS:
                assert product.configuration[parameter["id"]] == "false"
            elif parameter["type"] == "enum":
                assert product.configuration[parameter["id"]] in {option["id"] for option in parameter["options"]}
