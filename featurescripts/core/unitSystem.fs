FeatureScript 2960;
import(path : "onshape/std/units.fs", version : "2960.0");

export enum UnitSystem
{
    annotation { "Name" : "Inch" }
    IMPERIAL,
    annotation { "Name" : "Metric" }
    METRIC
}

export predicate unitSystemPredicate(definition is map)
{
    annotation { "Name" : "Unit system", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "HORIZONTAL_ENUM"] }
    definition.unitSystem is UnitSystem;
}

export predicate isImperial(definition is map)
{
    definition.unitSystem == UnitSystem.IMPERIAL;
}

/**
 * Selects a value based on the current unit system.
 */
export function unitSystemValue(unitSystem is UnitSystem, imperialValue, metricValue)
{
    return unitSystem == UnitSystem.IMPERIAL ? imperialValue : metricValue;
}
