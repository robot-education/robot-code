FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");
import(path : "onshape/std/holetables.gen.fs", version : "2960.0");

/**
 * How loosely one part fits in or over another: a bore on a shaft, a pocket around a part, or a hole for a fastener.
 * Close and free fits are std's clearance holes for the size (see `standardFitClearance` and `fastenerHoleDiameter`);
 * a custom fit is a clearance of your own.
 */
export enum Fit
{
    annotation { "Name" : "Close" }
    CLOSE,
    annotation { "Name" : "Free" }
    FREE,
    annotation { "Name" : "None" }
    NONE,
    annotation { "Name" : "Custom" }
    CUSTOM
}

/**
 * A custom fit's clearance, across (twice the gap on each side). Negative for an interference fit.
 */
export const FIT_CLEARANCE_BOUNDS = { (meter) : [-0.01, 0.0001, 0.01], (millimeter) : 0.1, (inch) : 0.005 } as LengthBoundSpec;

/**
 * The fit of a feature's main profile: `fit`, and `fitClearance` for a custom fit.
 */
export predicate fitPredicate(definition is map)
{
    annotation { "Name" : "Fit", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"], "Description" : "How loosely it fits. Close and free fits are the standard clearance holes for a fastener its size, as in the Hole feature's tables." }
    definition.fit is Fit;

    if (definition.fit == Fit.CUSTOM)
    {
        annotation { "Name" : "Clearance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "How much bigger the hole is than what goes in it, across it (not per side). Negative for an interference fit." }
        isLength(definition.fitClearance, FIT_CLEARANCE_BOUNDS);
    }
}

/**
 * The fit of a bore, for features with a fit of their own too: `boreFit`, and `boreFitClearance` for a custom fit.
 */
export predicate boreFitPredicate(definition is map)
{
    annotation { "Name" : "Bore fit", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"], "Description" : "How loosely it fits. Close and free fits are the standard clearance holes for a fastener its size, as in the Hole feature's tables." }
    definition.boreFit is Fit;

    if (definition.boreFit == Fit.CUSTOM)
    {
        annotation { "Name" : "Bore clearance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "How much bigger the hole is than what goes in it, across it (not per side). Negative for an interference fit." }
        isLength(definition.boreFitClearance, FIT_CLEARANCE_BOUNDS);
    }
}

/**
 * The clearance of the fit `fitPredicate` declares, for a profile `across` wide (see `standardFitClearance`).
 */
export function fitClearance(definition is map, across is ValueWithUnits) returns ValueWithUnits
{
    return standardFitClearance(definition.fit, definition.fitClearance, across);
}

/**
 * The clearance of the fit `boreFitPredicate` declares, for a bore `across` wide (see `standardFitClearance`).
 */
export function boreFitClearance(definition is map, across is ValueWithUnits) returns ValueWithUnits
{
    return standardFitClearance(definition.boreFit, definition.boreFitClearance, across);
}

/**
 * How wide a profile (sketched faces on `plane`) is, across its widest: the size its fit is for.
 */
export function profileAcross(context is Context, profile is Query, plane is Plane) returns ValueWithUnits
{
    const bounds = evBox3d(context, { "topology" : profile, "cSys" : coordSystem(plane), "tight" : true });
    const extent = bounds.maxCorner - bounds.minCorner;
    return max(extent[0], extent[1]);
}

/**
 * How much bigger a hole should be than the shaft or part going in it (its size across, `across`), across: for a close
 * or free fit, as much bigger as std's clearance hole for the inch fastener nearest its size (`ANSI_V2ClearanceHoleTable`,
 * which the Hole feature uses). That's 1/64 in. for a close fit and 1/32 in. for a free one from 7/16 in. to 4 in., and
 * less below (0.011 and 0.022 in. at 3/8 in.).
 *
 * @param customClearance : The clearance of a custom fit. Ignored otherwise.
 */
export function standardFitClearance(fit is Fit, customClearance, across is ValueWithUnits) returns ValueWithUnits
{
    if (fit == Fit.NONE)
    {
        return 0 * meter;
    }
    if (fit == Fit.CUSTOM)
    {
        return customClearance;
    }
    var nearest = undefined;
    for (var name, fits in ANSI_V2ClearanceHoleTable.entries)
    {
        const diameter = ansiFastenerDiameter(name);
        if (nearest == undefined || abs(diameter - across) < abs(nearest.diameter - across))
        {
            nearest = { "diameter" : diameter, "fits" : fits.entries };
        }
    }
    return lookupTableEvaluate(nearest.fits[fit == Fit.CLOSE ? "Close" : "Free"].holeDiameter) - nearest.diameter;
}

/**
 * The diameter of a hole for a fastener of `fastenerSize` (as std's hole tables name it, like `"#10"`, `"1/4"`, or
 * `"M3"`) with the fit `fitPredicate` declares: std's close or free clearance hole for it (`ANSI_V2ClearanceHoleTable`'s
 * Close or Free, or `ISO_V2ClearanceHoleTable`'s Close or Normal), its nominal diameter for no fit, or that plus a
 * custom fit's clearance.
 */
export function fastenerHoleDiameter(definition is map, fastenerSize is string) returns ValueWithUnits
{
    const metric = match(fastenerSize, "M([0-9.]+)");
    const fits = (metric.hasMatch ? ISO_V2ClearanceHoleTable : ANSI_V2ClearanceHoleTable).entries[fastenerSize];
    if (fits == undefined)
    {
        throw regenError("There's no clearance hole for a " ~ fastenerSize ~ " fastener.");
    }
    const diameter = metric.hasMatch ? stringToNumber(metric.captures[1]) * millimeter : ansiFastenerDiameter(fastenerSize);
    if (definition.fit == Fit.NONE || definition.fit == Fit.CUSTOM)
    {
        return diameter + (definition.fit == Fit.CUSTOM ? definition.fitClearance : 0 * meter);
    }
    const fitName = definition.fit == Fit.CLOSE ? "Close" : (metric.hasMatch ? "Normal" : "Free");
    return lookupTableEvaluate(fits.entries[fitName].holeDiameter);
}

/**
 * The nominal diameter of an inch fastener, by its size as std's hole tables name it: a number size (`"#10"`, 0.06 in.
 * and 0.013 in. a number), or inches, whole, fractional, or both (`"3"`, `"3/8"`, `"1 3/8"`).
 */
function ansiFastenerDiameter(size is string) returns ValueWithUnits
{
    const numbered = match(size, "#([0-9]+)");
    if (numbered.hasMatch)
    {
        return (0.06 + 0.013 * stringToNumber(numbered.captures[1])) * inch;
    }
    const fraction = match(size, "(([0-9]+) )?([0-9]+)/([0-9]+)");
    if (fraction.hasMatch)
    {
        const whole = (fraction.captures[2] ?? "") == "" ? 0 : stringToNumber(fraction.captures[2]);
        return (whole + stringToNumber(fraction.captures[3]) / stringToNumber(fraction.captures[4])) * inch;
    }
    return stringToNumber(size) * inch;
}
