FeatureScript 2960;
/**
 * The FIRST program a feature is designing for, which decides which parts it offers.
 */
import(path : "onshape/std/common.fs", version : "2960.0");

export enum Program
{
    annotation { "Name" : "FRC" }
    FRC,
    annotation { "Name" : "FTC" }
    FTC
}

/**
 * Creates the parameter `program`. Put it at the top of a feature, since it decides the options below it.
 */
export predicate programPredicate(definition is map)
{
    annotation { "Name" : "Program", "UIHint" : ["HORIZONTAL_ENUM", "REMEMBER_PREVIOUS_VALUE"] }
    definition.program is Program;
}

export predicate isFrc(definition is map)
{
    definition.program == Program.FRC;
}
