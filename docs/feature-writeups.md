# Feature writeups

Each feature with a writeup has a Markdown file beside its `.fs` file, named after it (`robotFrame.md`), for auditing
it: what changed since its last release, every string it shows users, and how it works. Shared modules can have one
too (`core/linearStock.md`), which features' writeups link to rather than repeat.

Writeups describe the code as it is; when they disagree, the code wins, and the writeup should be fixed. Update a
feature's writeup when you change the feature (or a module it uses in a way users would notice), and before releasing
it.

## Sections

1. **Summary**: what the feature does, in a sentence or two, and its files.
2. **Changelog**: user-facing changes since its last release (see below), newest concerns first: anything that changes
   existing documents when they update (parameters renamed or removed, defaults changed, geometry changed), then new
   options, then fixes and wording. Internal changes only belong if they change behavior. An unreleased feature says
   so, and has no changelog.
3. **Strings**: the strings the feature shows which aren't plain to see in its dialog, in tables, so their wording can
   be audited in one place:
   - the feature's name and description;
   - parameters' descriptions (their tooltips), and hidden parameters (which editing logic sets);
   - errors, warnings, and info messages: the message, when it's shown, and what it highlights;
   - part names and other properties it sets.

   Labels and options are left out: they're easy to audit in the dialog itself (`fs ui`, or the VS Code preview).

   Strings built from pieces are written with their pieces in angle brackets, e.g. `The <name> has no length.`
4. **How it works**: enough to audit its data flow and execution order without reading every line:
   - the order things run in when the feature regenerates (editing logic, body, manipulator change function), and
     what each step reads and writes. Leave out the precondition: the dialog (`fs ui`, or `fs audit`'s page) shows it;
   - its functions, briefly, grouped by step;
   - how errors are found and reported, and what's checked where;
   - every `try`: what it guards, and what happens when what it tries fails. Call out fallbacks (doing something else
     when an operation fails) as such; they're to be removed (see "No fallbacks or retries" in
     `docs/featurescript-style.md`).
5. **Issues found**: anything noticed while writing it which looks wrong (bugs, confusing wording, inconsistent
   behavior), for review. Fix them separately, then remove them from the list.

## Gathering what goes in them

```
uv run fs changes robotShaft --stat   # which files changed since its last release
uv run fs changes robotShaft          # how (a diff of the feature and everything it imports)
uv run fs strings robotShaft          # every user-facing string in it and what it imports, by file and line
uv run fs ui featurescripts/released/shaft/robotShaft.fs --set ...   # see a state of its dialog
```

`fs changes` diffs against the backend document's version of the feature's last release (versions are named like
`Robot shaft - v2.2.0`), for the feature and every Feature Studio in the repo it imports, so changes to shared modules
show up too. Most of what it shows is internal; the changelog only needs what users would notice. Check generated
geometry (like profiles moved into a `*.py` generator) by comparing coordinates rather than trusting that it was
unchanged.

`fs strings` lists everything in each imported module, including predicates the feature doesn't use, so check which
apply: descriptions only of parameters the feature can show (walk its precondition, including the predicates it calls),
and messages only from code it runs. `grep -n "try" <files>` finds the `try`s to call out.

Git history (`git log -- <files>`) helps explain why things changed, but the diff against the release is the source of
truth for what did.
