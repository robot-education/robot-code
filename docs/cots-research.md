# Researching COTS parts

Features like Robot frame, Robot nut strip, and Robot shaft offer the parts teams actually buy, in lookup tables
generated from Python definitions beside them (e.g. `featurescripts/frame/frameTables.py`). This is how to decide
what goes in them, and how to get each part's details right.

## 1. Find what teams use: `fs cots`

[FRCDesign](https://app.frcdesign.org) is an Onshape app with libraries of the COTS parts most FRC and FTC teams use,
and it records how often each is inserted, and which configuration options are chosen. `fs cots` reads that through
FRCDesign's public API (no sign-in, and no Onshape API calls):

```
uv run fs cots 'hex shaft'                 # FRC parts matching a regular expression, most used first
uv run fs cots 'channel|beam' -l ftc       # the FTC library (-l mkcad for MKCad's)
uv run fs cots 'hex shaft' -d              # also each part's configuration usage and part numbers
uv run fs cots --days 90 -n 100            # the top 100 parts over the last 90 days
```

Read the numbers relative to each other: a part used 400 times a year (like WCP box tube) is core, 40 is worth
adding if it's cheap to, and a handful usually isn't. Configuration usage says which sizes to put first (and make
the default), and which to leave out. The part numbers (with names and vendor links) FRCDesign's configurations
carry are a good starting point, but check them against the vendor (below): some are out of date.

FRCDesign's API (see `fs_cli/cots.py`), in case `fs cots` doesn't show what you need:

| Endpoint | Returns |
| --- | --- |
| `/api/library-version/library/{library}` | The library's version, which the data below is cached by |
| `/api/library-data/library/{library}?v={version}` | Its groups and parts ("insertables"), with their vendors |
| `/api/analytics/parts/library/{library}?from={date}&to={date}` | How often each part was inserted |
| `/api/analytics/insertable/library/{library}/element/{element id}?from=...&to=...` | How often each configuration option was chosen |
| `/api/configuration/insertable/{insertable id}?v={version}` | Its configuration parameters, and its configurations' part numbers, names, and links |

Libraries are `frc-design-lib`, `ftc-design-lib`, and `mkcad`. It rejects Python's `urllib` (by its user agent),
so use `requests` or `curl`.

## 2. Get each part's details from its vendor

Prefer the vendor's own drawings and CAD to anyone's model of them:

- **Product data**: Shopify stores (WCP, ThriftyBot, AndyMark, Swyft) list every product and variant, with SKUs, at
  `https://{store}/products.json?limit=250&page={n}`, and one product at `/products/{handle}.json`. Variants are
  often the lengths a part is sold in.
- **Drawings**: WCP, REV (`https://revrobotics.com/content/docs/{part number}-DR.pdf`), and AndyMark (linked from
  the product page) publish PDF drawings with hole patterns and lengths; `pdftotext -layout` and `pdftoppm -png`
  read them.
- **STEP files**: goBILDA (`https://www.gobilda.com/content/step_files/{SKU}.zip`) and REV
  (`https://www.revrobotics.com/content/cad/{part number}.STEP`) publish one per part. Measure them by listing their
  planes and cylinders, or read a cross section with `fs_cli.step.StepFile(path).profile()` (with `holes=True` for
  its inner loops); keep the files in a `vendor/` folder beside the definition, as `frame/vendor/` and
  `printAdapter/vendor/` do.
- **Links**: check every URL a table uses with `curl -sS -o /dev/null -L -w '%{http_code} %{url_effective}'`; vendors
  move pages, and some (like REV's) only have pages for part families, so link a search for the part number
  instead.

Record where each number came from in a comment beside it (e.g. "REV-21-3207-DR.pdf", "1143-0003-0096's STEP file"),
and leave a `TODO` for anything assumed.

## 3. Put it in a table

- Order vendors and parts by use, so the most used one is the default.
- Give each part its `stock`: the lengths it's sold in, shortest first, each with its part number and the page
  for that part number, if it has one (`setStockProperties` picks the shortest one long enough). Parts sold in each
  length (like goBILDA's) list every length.
- Use placeholder appearances (`WHITE`, `BLACK`, `DARK_GRAY`) until colors are chosen, and the right material
  (`ALUMINUM`, `ALUMINUM_7075`, `STEEL`, `STAINLESS_STEEL` in `core/robotProperties.fs`).
- Run `uv run fs gen`, then `uv run fs check` and `uv run fs ui` on the feature, and simulate anything with rules
  (like which holes a length gets) in Python against the vendor's sold lengths.
