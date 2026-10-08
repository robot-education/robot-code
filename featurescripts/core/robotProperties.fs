FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export const ALUMINUM = material("Aluminum - 6061", 0.098 * pound / inch ^ 3);
export const ALUMINUM_7075 = material("Aluminum - 7075", 0.102 * pound / inch ^ 3);
export const STEEL = material("Steel", 0.284 * pound / inch ^ 3);
export const STAINLESS_STEEL = material("Stainless Steel", 0.289 * pound / inch ^ 3);
export const PLASTIC = material("Onyx", 1.18 * gram / centimeter ^ 3);

// Appearances of parts, by what they're made of

/** General white (#EAEAEA): electronics housings, buttons. */
export const WHITE = color(234 / 255, 234 / 255, 234 / 255);
/** Light gray (#E6E6E6): purely uncoated aluminum, like REX standoffs and raw tube. */
export const LIGHT_GRAY = color(230 / 255, 230 / 255, 230 / 255);
/** Medium gray (#A6A6A6): aluminum, like clear anodized tube and aluminum shafts. */
export const MEDIUM_GRAY = color(166 / 255, 166 / 255, 166 / 255);
/** Steel (#B3B3B3): bearings, magnets, uncoated steel bolts and shafts, omni wheel rollers. */
export const STEEL_GRAY = color(179 / 255, 179 / 255, 179 / 255);
/** Uncoated steel (#B8C0D4): threaded plates. */
export const UNCOATED_STEEL_GRAY = color(184 / 255, 192 / 255, 212 / 255);
/**
 * General black (#4D4D4D): anything black, like sprockets, steel gears, black anodized aluminum (like WCP's), black
 * oxide bolts, belts and tread, and motors.
 */
export const BLACK = color(77 / 255, 77 / 255, 77 / 255);
/** Machined brass yellow (#F5D578): brass gears and pins. */
export const BRASS_YELLOW = color(245 / 255, 213 / 255, 120 / 255);
/** REV orange (#FE8B00): Wago levers. */
export const REV_ORANGE = color(254 / 255, 139 / 255, 0 / 255);
/** 3D printed green (#4DD188): printed parts. */
export const PRINTED_GREEN = color(77 / 255, 209 / 255, 136 / 255);
/** Copper (#FCBC84): copper, like flywheels. */
export const COPPER = color(252 / 255, 188 / 255, 132 / 255);
