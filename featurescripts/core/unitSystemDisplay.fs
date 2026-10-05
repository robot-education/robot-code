FeatureScript 2960;
/**
 * Utilities for displaying values based on a selected `unitSystem`.
 */
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "948c83c1b1ac83de4ccf921b", version : "4aff58a1ab26d9f7aa7abfbb");

function getPrecision(strict is boolean)
{
    return strict ? 4 : 3;
}

/**
 * Returns the unit used to measure errors.
 */
function getDisplayUnit(unitSystem is UnitSystem) returns ValueWithUnits
{
    return unitSystemValue(unitSystem, inch, centimeter);
}

function roundForDisplay(unitSystem is UnitSystem, value is ValueWithUnits, strict is boolean) returns number
{
    return roundToPrecision(value / getDisplayUnit(unitSystem), getPrecision(strict));
}

/**
 * Returns a string displaying the specified `value` with the appropriate precision and unit.
 *
 * @param strict : `true` to use a stricter tolerance. Defaults to `false`.
 */
export function makeValueString(unitSystem is UnitSystem, value is ValueWithUnits, strict is boolean) returns string
{
    const roundedValue = roundForDisplay(unitSystem, value, strict);
    return roundedValue ~ unitSystemValue(unitSystem, " in", " cm");
}

export function makeValueString(unitSystem is UnitSystem, value is ValueWithUnits) returns string
{
    return makeValueString(unitSystem, value, false);
}

/**
 * Returns `true` if two values are up to tolerance based on the display precision.
 *
 * @param strict : `true` to use a stricter tolerance. Defaults to `false`.
 */
export function withinDisplayPrecision(definition is map, measuredValue is ValueWithUnits, idealValue is ValueWithUnits, strict is boolean) returns boolean
{
    // When checking for errors, we have to keep in mind that our error display only goes to 3 decimals
    // So the center to center reported to the user may be +/- 0.001 / 2 from the ideal value
    // Which is half of the smallest possible display increment, plus zero length tolerance for edge cases
    const minDisplayValue = (10 ^ (-getPrecision(strict)) * getDisplayUnit(definition.unitSystem) / 2) + TOLERANCE.zeroLength * meter;
    // This overload only works for numbers
    return tolerantEquals(measuredValue / meter, idealValue / meter, minDisplayValue / meter);
}

export function withinDisplayPrecision(definition is map, measuredValue is ValueWithUnits, idealValue is ValueWithUnits) returns boolean
{
    return withinDisplayPrecision(definition, measuredValue, idealValue, false);
}
