FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export const ALUMINUM = material("Aluminum - 6061", 0.098 * pound / inch ^ 3);
export const ALUMINUM_7075 = material("Aluminum - 7075", 0.102 * pound / inch ^ 3);
export const STEEL = material("Steel", 0.284 * pound / inch ^ 3);
export const STAINLESS_STEEL = material("Stainless Steel", 0.289 * pound / inch ^ 3);
export const PLASTIC = material("Onyx", 1.18 * gram / centimeter ^ 3);

// Appearances of parts, by what they're made of: FRCDesignLib's color standards, but for uncoated steel and printed green

/** General white (#FFFFFF): electronics housings, buttons. */
export const WHITE = color(255 / 255, 255 / 255, 255 / 255);
/** Light grey (#E6E6E6): uncoated aluminum, like REX standoffs and raw tube, and Colson wheels. */
export const LIGHT_GRAY = color(230 / 255, 230 / 255, 230 / 255);
/** Medium grey (#C6C6C6): motor shafts, grey wires, metal contacts and terminals, zinc-coated steel. */
export const MEDIUM_GRAY = color(198 / 255, 198 / 255, 198 / 255);
/** Steel and neodymium grey (#B3B3B3): bearings, magnets, uncoated steel bolts and shafts, omni wheel rollers. */
export const STEEL_GRAY = color(179 / 255, 179 / 255, 179 / 255);
/** Uncoated steel (#B8C0D4): threaded plates. */
export const UNCOATED_STEEL_GRAY = color(184 / 255, 192 / 255, 212 / 255);
/**
 * General black (#373737): anything black, like sprockets, steel gears, black anodized aluminum (like WCP's), black
 * oxide bolts, belts and tread, and motors.
 */
export const BLACK = color(55 / 255, 55 / 255, 55 / 255);
/** Machined brass yellow (#FDE086): brass gears and pins. */
export const BRASS_YELLOW = color(253 / 255, 224 / 255, 134 / 255);
/** Zinc-coated steel (#E1E7EC): nuts. */
export const ZINC_COATED_STEEL = color(225 / 255, 231 / 255, 236 / 255);
/** REV orange (#FE8B00): Wago levers. */
export const REV_ORANGE = color(254 / 255, 139 / 255, 0 / 255);
/** 3D printed green (#4DD188): printed parts. */
export const PRINTED_GREEN = color(77 / 255, 209 / 255, 136 / 255);
/** Copper (#FCBC84): copper, like flywheels. */
export const COPPER = color(252 / 255, 188 / 255, 132 / 255);
