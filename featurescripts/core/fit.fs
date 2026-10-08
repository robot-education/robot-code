FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

/**
 * How loosely one part fits in or over another: a bore on a shaft, a pocket around a part, or a hole for a fastener.
 * Free and close fits follow the standards for their size (see `runningFitClearance` and `fastenerHoleDiameter`); a
 * custom fit is a clearance of your own.
 */
export enum Fit
{
    annotation { "Name" : "Free" }
    FREE,
    annotation { "Name" : "Close" }
    CLOSE,
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
    annotation { "Name" : "Fit", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"], "Description" : "How loosely it fits. Free and close fits follow the standards for its size: ISO 286's free running (H9/d9) and close running (H8/f7) fits, or for a fastener, the standard free and close clearance holes." }
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
    annotation { "Name" : "Bore fit", "UIHint" : ["REMEMBER_PREVIOUS_VALUE", "SHOW_LABEL"], "Description" : "How loosely it fits. Free and close fits follow ISO 286's free running (H9/d9) and close running (H8/f7) fits for its size." }
    definition.boreFit is Fit;

    if (definition.boreFit == Fit.CUSTOM)
    {
        annotation { "Name" : "Bore clearance", "UIHint" : ["REMEMBER_PREVIOUS_VALUE"], "Description" : "How much bigger the hole is than what goes in it, across it (not per side). Negative for an interference fit." }
        isLength(definition.boreFitClearance, FIT_CLEARANCE_BOUNDS);
    }
}

/**
 * The clearance of the fit `fitPredicate` declares, for a profile `across` wide (see `runningFitClearance`).
 */
export function fitClearance(definition is map, across is ValueWithUnits) returns ValueWithUnits
{
    return runningFitClearance(definition.fit, definition.fitClearance, across);
}

/**
 * The clearance of the fit `boreFitPredicate` declares, for a bore `across` wide (see `runningFitClearance`).
 */
export function boreFitClearance(definition is map, across is ValueWithUnits) returns ValueWithUnits
{
    return runningFitClearance(definition.boreFit, definition.boreFitClearance, across);
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
 * How much bigger a hole should be than the shaft or part going in it (its size across, `across`), across: for a free
 * fit, the mean clearance of ISO 286's free running fit (H9/d9), and for a close fit, of its close running fit
 * (H8/f7), for the size's range. A part sold at its nominal size then fits as one made to those tolerances would.
 *
 * These are machining fits; a printed part may need more (a custom fit).
 *
 * @param customClearance : The clearance of a custom fit. Ignored otherwise.
 */
export function runningFitClearance(fit is Fit, customClearance, across is ValueWithUnits) returns ValueWithUnits
{
    if (fit == Fit.NONE)
    {
        return 0 * meter;
    }
    if (fit == Fit.CUSTOM)
    {
        return customClearance;
    }
    const range = iso286Range(across);
    // The clearance between a hole at the middle of its tolerance zone (above the size) and a shaft at the middle of
    // its zone (below the size, by its fundamental deviation)
    const microns = fit == Fit.CLOSE ? range.f + (range.it8 + range.it7) / 2 : range.d + range.it9;
    return microns * 1e-6 * meter;
}

/**
 * ISO 286-1's tolerance grades IT7, IT8, and IT9, and the fundamental deviations of d and f shafts (as distances
 * below the size), in micrometers, for sizes up to `upTo` millimeters (and over the previous range's).
 */
const ISO_286_RANGES = [
        { "upTo" : 3, "it7" : 10, "it8" : 14, "it9" : 25, "d" : 20, "f" : 6 },
        { "upTo" : 6, "it7" : 12, "it8" : 18, "it9" : 30, "d" : 30, "f" : 10 },
        { "upTo" : 10, "it7" : 15, "it8" : 22, "it9" : 36, "d" : 40, "f" : 13 },
        { "upTo" : 18, "it7" : 18, "it8" : 27, "it9" : 43, "d" : 50, "f" : 16 },
        { "upTo" : 30, "it7" : 21, "it8" : 33, "it9" : 52, "d" : 65, "f" : 20 },
        { "upTo" : 50, "it7" : 25, "it8" : 39, "it9" : 62, "d" : 80, "f" : 25 },
        { "upTo" : 80, "it7" : 30, "it8" : 46, "it9" : 74, "d" : 100, "f" : 30 },
        { "upTo" : 120, "it7" : 35, "it8" : 54, "it9" : 87, "d" : 120, "f" : 36 },
        { "upTo" : 180, "it7" : 40, "it8" : 63, "it9" : 100, "d" : 145, "f" : 43 },
        { "upTo" : 250, "it7" : 46, "it8" : 72, "it9" : 115, "d" : 170, "f" : 50 },
        { "upTo" : 315, "it7" : 52, "it8" : 81, "it9" : 130, "d" : 190, "f" : 56 },
        { "upTo" : 400, "it7" : 57, "it8" : 89, "it9" : 140, "d" : 210, "f" : 62 },
        { "upTo" : 500, "it7" : 63, "it8" : 97, "it9" : 155, "d" : 230, "f" : 68 }
    ];

function iso286Range(across is ValueWithUnits) returns map
{
    for (var range in ISO_286_RANGES)
    {
        if (tolerantLessThanOrEqual(across, range.upTo * millimeter))
        {
            return range;
        }
    }
    // Bigger than ISO 286's preferred fits go; the last range's are close enough
    return ISO_286_RANGES[size(ISO_286_RANGES) - 1];
}

/**
 * The fasteners `fastenerHoleDiameter` knows: their sizes (as hole tables name them), nominal diameters, and close
 * and free clearance hole diameters. Inch fasteners' are the usual close and free fit drills; metric fasteners' are
 * ISO 273's fine and medium series.
 */
export const FASTENER_CLEARANCE_HOLES = {
        "#8" : { "diameter" : 0.164 * inch, "close" : 0.1695 * inch, "free" : 0.177 * inch },
        "#10" : { "diameter" : 0.19 * inch, "close" : 0.196 * inch, "free" : 0.201 * inch },
        "1/4" : { "diameter" : 0.25 * inch, "close" : 0.257 * inch, "free" : 0.266 * inch },
        "M3" : { "diameter" : 3 * millimeter, "close" : 3.2 * millimeter, "free" : 3.4 * millimeter },
        "M4" : { "diameter" : 4 * millimeter, "close" : 4.3 * millimeter, "free" : 4.5 * millimeter },
        "M5" : { "diameter" : 5 * millimeter, "close" : 5.3 * millimeter, "free" : 5.5 * millimeter },
        "M6" : { "diameter" : 6 * millimeter, "close" : 6.4 * millimeter, "free" : 6.6 * millimeter },
        "M8" : { "diameter" : 8 * millimeter, "close" : 8.4 * millimeter, "free" : 9 * millimeter },
        "M10" : { "diameter" : 10 * millimeter, "close" : 10.5 * millimeter, "free" : 11 * millimeter }
    };

/**
 * The diameter of a hole for a fastener of `fastenerSize` (a key of `FASTENER_CLEARANCE_HOLES`, like `"#10"`) with the
 * fit `fitPredicate` declares: its standard close or free clearance hole, its nominal diameter for no fit, or
 * that plus a custom fit's clearance.
 */
export function fastenerHoleDiameter(definition is map, fastenerSize is string) returns ValueWithUnits
{
    const hole = FASTENER_CLEARANCE_HOLES[fastenerSize];
    if (hole == undefined)
    {
        throw regenError("There's no clearance hole for a " ~ fastenerSize ~ " fastener.");
    }
    return switch (definition.fit) {
            Fit.FREE : hole.free,
            Fit.CLOSE : hole.close,
            Fit.NONE : hole.diameter,
            Fit.CUSTOM : hole.diameter + definition.fitClearance
        };
}
