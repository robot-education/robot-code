"""Tests of the FeatureScript evaluator's semantics (fs_eval)."""

import pytest

from fs_eval import FSError, to_display, to_python
from fs_eval.pytest_plugin import shared_evaluator

HEADER = 'FeatureScript 2960;\nimport(path : "onshape/std/common.fs", version : "2960.0");\n'


def run(source: str, expression: str):
    evaluator = shared_evaluator()
    module = evaluator.source(HEADER + source)
    return evaluator.eval(expression, module)


def test_values_are_copied():
    source = """
    export function f()
    {
        var a = [1, 2];
        var b = a;
        b[0] = 5;
        var m = { "x" : { "y" : 1 } };
        var n = m;
        n.x.y = 2;
        return [a[0], b[0], m.x.y, n.x.y];
    }
    """
    assert to_python(run(source, "f()")) == [1, 5, 1, 2]


def test_maps_drop_undefined_and_iterate_in_key_order():
    source = """
    export function f()
    {
        var m = { "b" : 1, "a" : 2, "c" : undefined };
        m.b = undefined;
        m["d"] = 3;
        var keys = [];
        for (var key, value in m)
        {
            keys = append(keys, key);
        }
        return [size(m), keys];
    }
    """
    assert to_python(run(source, "f()")) == [2, ["a", "d"]]


def test_overloads_choose_the_most_specific():
    source = """
    export function describe(value) { return "anything"; }
    export function describe(value is number) { return "number"; }
    export function describe(value is ValueWithUnits) { return "value with units"; }
    export function describe(value is map) { return "map"; }
    """
    assert run(source, 'describe("x")') == "anything"
    assert run(source, "describe(1)") == "number"
    assert run(source, "describe(1 * inch)") == "value with units"
    assert run(source, "describe({})") == "map"


def test_units():
    assert to_python(run("", "3 * inch + 2 * millimeter"), "millimeter") == pytest.approx(78.2)
    assert run("", "(2 * inch) / inch") == pytest.approx(2)
    assert to_display(run("", "sqrt(4 * meter ^ 2)")) == "2 meter"
    with pytest.raises(FSError, match="precondition"):
        run("", "1 * inch + 1 * degree")


def test_predicates_and_preconditions():
    source = """
    export predicate isSmall(value)
    {
        value is number;
        value < 10;
    }
    export function half(value is number) returns number
    precondition value % 2 == 0;
    {
        return value / 2;
    }
    """
    assert run(source, "isSmall(3)") is True
    assert run(source, "isSmall(30)") is False
    assert run(source, 'isSmall("x")') is False
    assert run(source, "half(4)") == 2
    with pytest.raises(FSError, match="precondition of half"):
        run(source, "half(3)")


def test_types_and_casts():
    source = """
    export type Even typecheck isEven;
    export predicate isEven(value)
    {
        value is number;
        value % 2 == 0;
    }
    """
    assert run(source, "4 as Even is Even") is True
    assert run(source, "4 is Even") is False
    with pytest.raises(FSError, match="typecheck"):
        run(source, "3 as Even")


def test_errors_and_try():
    source = """
    export function f(fail is boolean)
    {
        if (fail)
            throw regenError("It failed.", ["x"]);
        return "fine";
    }
    export function caught()
    {
        try
        {
            f(true);
        }
        catch (error)
        {
            return error.customMessage;
        }
    }
    """
    assert run(source, "caught()") == "It failed."
    assert run(source, "try(f(true))") is None
    with pytest.raises(FSError, match="It failed."):
        run(source, "f(true)")


def test_closures_boxes_and_switch():
    source = """
    export function counter()
    {
        var count = new box(0);
        const add = function(n) { count[] += n; };
        add(2);
        add(3);
        return count[];
    }
    export function name(value is number)
    {
        return switch (value) { 1 : "one", 2 : "two" };
    }
    """
    assert run(source, "counter()") == 5
    assert run(source, "name(2)") == "two"
    assert run(source, "name(3)") is None
    assert to_python(run("", "mapArray([1, 2], x => x * 10)")) == [10, 20]


def test_enums_sort_by_ordinal():
    source = """
    export enum Size { SMALL, LARGE }
    export function first()
    {
        for (var key, value in { Size.LARGE : 1, Size.SMALL : 2 })
            return key;
    }
    """
    assert to_display(run(source, "first()")) == "SMALL"


def test_unavailable_builtins_say_so():
    with pytest.raises(FSError, match="@evDistance isn't available"):
        run("", "evDistance(newContext(), { side0 : vector(0, 0, 0) * meter, side1 : vector(1, 0, 0) * meter })")
