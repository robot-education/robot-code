FeatureScript 2014;
import(path : "onshape/std/common.fs", version : "2014.0");

import(path : "00b10ef1fb1a7418097fc0af", version : "1c5780f138c5c63e16f34d6b");

export const PULLEY_BORE_BOUNDS = { (inch) : [0, 0.5, 1e50] } as LengthBoundSpec;
export const PULLEY_FLANGE_WIDTH_BOUNDS = { (inch) : [0, 0.0625, 1e50] } as LengthBoundSpec;

export const PULLEY_WIDTH_BOUNDS = { (inch) : [0, 0.4375, 1e50] } as LengthBoundSpec;

export const PULLEY_GEAR_TEETH_BOUNDS = { (unitless) : [3, 12, 1e50] } as IntegerBoundSpec;

export enum BoreType
{
    annotation { "Name" : "Hex" }
    HEX,
    annotation { "Name" : "Circular" }
    CIRCULAR,
    annotation { "Name" : "Falcon spline" }
    FALCON_SPLINE,
    annotation { "Name" : "3D print adapter" }
    ADAPTER,
    annotation { "Name" : "20DP gear" }
    GEAR
}

/**
 * @param id : @autocomplete `id + "pulley"`
 * @param definition {{
 *      @field beltDefinition {BeltDefinition} :
 *              A `BeltDefinition` defining the belt the pulley is being added to.
 *      @field pulleyIndex {number} :
 *              The index of the pulley in `beltDefinition` being created.
 *      @field boreType {BoreType} :
 *              The bore type of the pulley.
 *      @field width {ValueWithUnits} :
 *      @field hasFlanges {boolean} :
 *      @field flangeWidth {ValueWithUnits} : @requiredif `hasFlanges == true`
 * }}
 */
export const opRobotPulley = function(context is Context, id is Id, definition is map)
    precondition
    {
        // definition.beltDefinition is BeltDefinition;
        definition.pulleyIndex is number;
        definition.boreType is BoreType;
        definition.width is ValueWithUnits;
        definition.hasFlanges is boolean;
        if (definition.hasFlanges)
        {
            definition.flangeWidth is ValueWithUnits;
        }
    }
    {
        
    };
