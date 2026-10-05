FeatureScript 2960;
import(path : "onshape/std/common.fs", version : "2960.0");

export const ALUMINUM = material("Aluminum - 6061", 0.098 * pound / inch ^ 3);
export const ALUMINUM_7075 = material("Aluminum - 7075", 0.102 * pound / inch ^ 3);
export const STEEL = material("Steel", 0.284 * pound / inch ^ 3);
export const STAINLESS_STEEL = material("Stainless Steel", 0.289 * pound / inch ^ 3);
export const PLASTIC = material("Onyx", 1.18 * gram / centimeter ^ 3);

export const WHITE = color(230 / 255, 230 / 255, 230 / 255);
export const BLACK = color(0.3, 0.3, 0.3);
export const DARK_GRAY = color(135 / 255, 135 / 255, 135 / 255);
