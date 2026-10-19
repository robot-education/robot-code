# Using Onshape

How to drive Onshape from Chrome (the Claude in Chrome extension's `mcp__claude-in-chrome__*` tools), to see how a
feature behaves, read its errors, and try changes. The user is logged in to cad.onshape.com in that Chrome.

When a feature misbehaves, read its errors and notices (below) before theorizing about what Onshape does.

## Opening tabs

The fastest way to a tab is its URL: `navigate` straight to it rather than clicking through the tab bar, which is long
and scrolls. A tab's URL is the document's workspace URL plus `/e/{element id}`. The backend document (`backend` in
`pyproject.toml`) is
`https://cad.onshape.com/documents/00dd11dabe44da2db458f898/w/6c20cd994b174cc99668701f`.

- Feature Studios: the element id is the key of the file in `fs-studios.json`.
- Anything else (Part Studios with examples, Assemblies, the Variable Studio): list the tabs from an Onshape page with
  `fetch('/api/documents/d/{did}/w/{wid}/elements')`. This uses the web session, not the API keys. Each element has
  `name`, `elementType` (`PARTSTUDIO`, `FEATURESTUDIO`, `VARIABLESTUDIO`, ...), and `id`.

The extension hides the results of `javascript_tool` that look like encoded data, which includes bare element ids
(`[BLOCKED: Base64 encoded data]`). Return them inside a longer string, like the tab's URL:

```js
const r = await fetch('/api/documents/d/00dd11dabe44da2db458f898/w/6c20cd994b174cc99668701f/elements').then(r => r.json());
r.filter(e => /Robot grid/.test(e.name)).map(e => e.name + ': ' + location.pathname.replace(/\/e\/.*/, '/e/' + e.id))
```

## FeatureScript notices

The FeatureScript notices pane shows FeatureScript errors, warnings, and `println` output (including
`core/logging.fs`'s). Open it with the `{x}` button in the top toolbar (tooltip "FeatureScript notices"), or from
JavaScript, which toggles it:

```js
document.querySelector('[data-bs-original-title="FeatureScript notices"] .btn').click()
```

It opens along the bottom of the window and lists every Part Studio and Feature Studio in the document, each with a
Monitor button that shows that tab's notices too, along with the current tab's. It has a "Filter notices" box. To read
it, take `document.querySelector('.notices-container').innerText` (it's only in the DOM while the pane is open), or
use `get_page_text`, or take a screenshot.

A failed feature's own message is also on the feature: hover it in the feature list (it's red), or open it and read
the message at the top of its dialog.

## Monitoring and profiling

A Feature Studio's toolbar has a split button, next to the notices button (it reads "Monitor <tab>" after the last
choice), whose dropdown has a Monitor and a Profile entry for every Part Studio, Variable Studio, and Feature Studio in
the document:

- **Monitor** runs that tab's features with extra checks and reports runtime problems in the code, like a value of the
  wrong type, in the notices pane. Use it on an example Part Studio after changing a feature, to catch problems that
  don't make the feature fail.
- **Profile** runs that tab and reports how long the code takes, for finding what makes a feature slow.

Monitor a Part Studio which uses the feature you changed (its example, like Robot grid example for `robotGrid.fs`).

For a quick comparison of how long features take, use the Part Studio's own "Show regeneration times" button (the
stopwatch at the top of the feature list): it lists each feature's time, and the total. Profile when you need to know
which lines the time goes to.

Profiling marks each line that ran in the editor's gutter with how long it took in all, and hovering a mark shows each
call on the line (like "opBoolean 72ms in 2 calls"). To read them without hovering, scroll the editor
(`ace.edit(document.querySelector('.ace_editor'))`, `scrollToLine`, then `renderer.updateFull(true)`, as Ace doesn't
render while the tab's hidden) and take the text of the `.fs-profiled-line-hover-area` beside the line's
`.ace_gutter-cell`. The badge at the top of the gutter (`.regen-time`) is the Part Studio's whole regeneration time.
Timings vary by 10 to 20% between runs.

## Logging

`core/logging.fs`'s `logLine`, `logEntities`, and `logEdges` do something only when the Part Studio has a variable
`#logging` of `true` (see `docs/featurescript-style.md`'s "Logging"). In the backend document, the Variables tab sets
it for every Part Studio and Assembly:
https://cad.onshape.com/documents/00dd11dabe44da2db458f898/w/6c20cd994b174cc99668701f/e/e2b8ba54a0346828f8db39b1

It's a Variable Studio with one variable, `logging` (type Any, `true`), and "Insert into all Part Studios and
Assemblies" turned on, so logging is on for every feature in the document's examples. To turn it off for one Part
Studio, add a Variable feature there setting `#logging` to `false`, before the feature you're looking at; to turn it
off everywhere, set the Variable Studio's value to `false`. Documents that use released features don't have it, so
they don't log.

## Trying changes without `fs push`

`fs push` counts against the yearly API limit. Requests made with `fetch` from a cad.onshape.com page use the web
session instead, so Feature Studios can be edited and FeatureScript run from Chrome freely. Send the `X-XSRF-TOKEN`
header, set to the `XSRF-TOKEN` cookie, with requests which change anything.

- Feature Studio contents: GET and POST `/api/featurestudios/d/{did}/w/{wid}/e/{eid}` (`contents`,
  `serializationVersion`, `sourceMicroversion`). Before posting, check that Onshape matches `git show HEAD:file` (by
  SHA-256), send only your edits, and check that what you post hashes the same as the local file. `fs status` then
  reports the file in sync: commit it as `fs push` would.
- Running FeatureScript in a Part Studio: POST
  `/api/partstudios/d/{did}/w/{wid}/e/{eid}/featurescript?rollbackBarIndex=N` with `{script, queries : [{key,
  value : [deterministic ids]}]}` runs a `function(context is Context, queries is map)`. Operations and
  `startFeature`/`abortFeature` work in it, and its `println` output comes back in `console`. It's good for probing
  geometry. It's rate limited, separately from the other endpoints: about 150 requests in a few minutes got a 429
  with a `retry-after` of 23 hours. Do a sweep's work in one script (loop over the values in FeatureScript) rather than
  a request per value, and check the `x-rate-limit-remaining` header.
- A Part Studio's exact geometry at a point in its history: GET
  `/api/partstudios/d/{did}/w/{wid}/e/{eid}/bodydetails?rollbackBarIndex=N` gives each body's faces (with their loops of
  edges), edges (with their curves: lines, and circles' centers and radii), and vertices. It's a way to check a
  feature's result against geometry computed independently (with shapely, say).
- A feature's parameters: GET `/features`, change a parameter's `expression`, and POST the feature to
  `/features/featureid/{fid}` (with the listing's `serializationVersion` and `sourceMicroversion`); the response's
  `featureState` says whether it worked (`OK`, `INFO`, `WARNING`, `ERROR`), and the monitored tab's notices say why.
  It's rate limited like the FeatureScript endpoint (a few dozen calls got a 429 with a `retry-after` of 16 hours),
  while Feature Studio contents weren't.

The backend document's examples are for trying things: changing a feature's parameters to test it is fine, and so is
leaving them changed afterwards. Don't spend calls putting every value back; just don't leave a feature failing that
was working.

Close a Feature Studio's tab in Onshape before changing it this way (or reload it after), as with `fs push` (see
`docs/fs-cli.md`).
