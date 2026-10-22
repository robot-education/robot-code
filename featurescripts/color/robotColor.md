# Robot color

## Summary

Gives parts an appearance: one of FRCDesign's standard colors, or any color by RGB or hex code, on the parts selected
or every part in the Part Studio. Files: `robotColor.fs`; its standard colors are `core/robotProperties.fs`'s.

Unreleased.

## Strings

| Feature | Name | Description |
| --- | --- | --- |
| `robotColor` | Robot color | Give parts one of FRCDesign's standard appearances, or any color by RGB or hex code. |

The description ends with the credit.

| Message | When | Highlights | Closest std error |
| --- | --- | --- | --- |
| Select parts to color. (error) | Parts is Selected, and none are | Parts to color | none |
| Enter a hex code of 6 digits, like #4D4D4D, or 3, like #FFF. (error) | Color is Hex, and the code isn't one | Hex code | none |
| There are no parts to color. (info) | Parts is All, and the Part Studio has no parts yet | none | none |

It sets each part's appearance, and nothing else.

## How it works

The body:

1. Picks the parts: those selected (`colorsSelectedParts`), or every modifiable solid part
   (`qAllModifiableSolidBodies`), so parts derived or in context are left alone. None selected is an error; none at
   all, with All, is info, and nothing is done.
2. Works out the color (`chosenColor`):
   - Standard: `STANDARD_COLORS`, which maps each `StandardColor` to its `core/robotProperties.fs` constant. Each
     option's name gives its hex code, which a test checks against the constant.
   - RGB: `rgbColor`, three channels from 0 to 255.
   - Hex: `hexColor`, 6 digits or 3 (each doubled, as CSS reads `#FFF`), with or without `#`, either case, spaces
     around it ignored. Anything else is an error.
3. Sets their appearance (`setProperty`, `PropertyType.APPEARANCE`), opaque.

It colors the parts as they are where it sits in the feature list: parts made after it keep their own.

There are no `try`s.

## Issues found

- The standard colors are `core/robotProperties.fs`'s, named after FRCDesign's standards; they aren't yet checked
  against FRCDesignLib's standards document.
