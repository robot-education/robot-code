# Robot color

## Summary

Gives parts an appearance: one of FRCDesign's standard colors, or with Custom color checked, any color by hex code or
RGB, on the parts selected or every part in the Part Studio. Files: `robotColor.fs`; its standard colors are
FRCDesignLib's color standards, which `core/robotProperties.fs`'s appearances share.

Unreleased.

## Strings

| Feature | Name | Description |
| --- | --- | --- |
| `robotColor` | Robot color | Give parts one of FRCDesign's standard appearances, or any color by hex code or RGB. |

The description ends with the credit.

| Parameter | Description |
| --- | --- |
| Color | A hex code, like #373737 or #FFF, or RGB from 0 to 255, like 55, 55, 55. |

| Message | When | Highlights | Closest std error |
| --- | --- | --- | --- |
| Select parts to color. (error) | Parts is Selected, and none are | Parts to color | none |
| Enter a hex code, like #373737, or RGB from 0 to 255, like 55, 55, 55. (error) | Custom color, and Color names no color | Color | none |
| There are no parts to color. (info) | Parts is All, and the Part Studio has no parts yet | none | none |

It sets each part's appearance, and nothing else.

## How it works

The body:

1. Picks the parts: those selected (`colorsSelectedParts`), or every modifiable solid part
   (`qAllModifiableSolidBodies`), so parts derived or in context are left alone. None selected is an error; none at
   all, with All, is info, and nothing is done.
2. Works out the color (`chosenColor`):
   - Standard color (General black by default): `STANDARD_COLORS`, each of FRCDesignLib's color standards, in the
     standards' order, by its hex code; the polycarbonates are 80% opaque, as the standards list them. Each option's
     name gives its hex code. A test checks the colors `core/robotProperties.fs` shares with the standards against
     them.
   - Custom color, overriding it: `parseColor` reads Color as a hex code first (`hexColor`: 6 digits or 3, each
     doubled as CSS reads `#FFF`, with or without `#`, either case), then as red, green, and blue from 0 to 255, apart
     by commas or spaces, in `rgb(...)` or not. So three digits alone are hex. Spaces around it are ignored;
     anything else is an error.
3. Sets their appearance (`setProperty`, `PropertyType.APPEARANCE`).

It colors the parts as they are where it sits in the feature list: parts made after it keep their own.

There are no `try`s.

## Issues found

- The standard colors are FRCDesignLib's color standards as they stood on 2026-10-23; a change to the standards
  needs `STANDARD_COLORS` changing by hand.
