"""`fs render`: builds FeatureScript-backed parts (part sets' configurations, see docs/parts.md) offline, with the
evaluator's geometry (`fs_eval.geometry`, OpenCascade), and writes what they look like and what they are: a PNG of
each, a glTF model (`.glb`), their metadata (`.json`: name, vendor, part number, material, appearance, mass, size,
mate connectors, threads), and a page with a 3D viewer of them all (`index.html`, or an Artifact's, to publish).

No Onshape involved: see docs/rendering.md.
"""

from __future__ import annotations

import dataclasses
import json
import pathlib
import re

from fs_cli.config import Config
from fs_cli.configured_studios import ConfiguredStudio, Input, configured_studios, input_shows
from fs_cli.configurations import run_definition


class RenderError(Exception):
    pass


@dataclasses.dataclass
class StudioRef:
    part_set: str
    folder: str
    studio: ConfiguredStudio
    module: str  # The Feature Studio exporting its feature, by its path in the code folder

    @property
    def label(self) -> str:
        return f"{self.part_set}/{self.studio.name}"


def part_set_studios(config: Config, part_set: str | None = None) -> list[StudioRef]:
    """Every part set's studios (or one set's), with the Feature Studio each one's feature is in."""
    found = []
    names = [p.name for p in config.parts]
    if part_set is not None and part_set not in names:
        raise RenderError(f"No part set {part_set!r}: there are {', '.join(names)}")
    for each in config.parts:
        if part_set is not None and each.name != part_set:
            continue
        definition = config.code_dir / each.folder / each.definition
        for studio in configured_studios(run_definition(definition)):
            found.append(StudioRef(each.name, each.folder, studio, _feature_module(config, each.folder, studio.feature)))
    return found


def _feature_module(config: Config, folder: str, feature: str) -> str:
    pattern = re.compile(rf"\bexport\s+const\s+{re.escape(feature)}\s*=")
    for path in sorted((config.code_dir / folder).glob("*.fs")):
        if pattern.search(path.read_text(encoding="utf-8")):
            return path.relative_to(config.code_dir).as_posix()
    raise RenderError(f"No Feature Studio in {folder}/ exports {feature}")


# Configurations


def configuration(studio: ConfiguredStudio, choices: dict[str, str] | None = None, strict: bool = True) -> dict[str, str]:
    """A configuration of the studio: `choices` (input id to option id), checked against what it offers, the rest at
    their defaults (or, where a default isn't shown with the choices, the first option that is). Not `strict`, a
    choice which isn't offered is treated as that, as Onshape's configuration panel does."""
    choices = dict(choices or {})
    inputs = {input.id: input for input in studio.configured_inputs()}
    for id in choices:
        if id not in inputs:
            fixed = [input.id for input in studio.inputs if input.fixed]
            raise RenderError(f"{studio.name} has no input {id!r}: it has {', '.join(inputs)}"
                              + (f" ({', '.join(fixed)} fixed)" if fixed else ""))
    combination: dict[str, str] = {}
    from fs_cli.configured_studios import shows

    for input in studio.configured_inputs():
        if not input_shows(input, combination):
            continue
        if input.kind == "boolean":
            options = ["false", "true"]
        else:
            options = [option.id for option in input.options if shows(option, combination)]
        if not options:
            continue
        chosen = choices.get(input.id)
        if chosen is not None and (strict or chosen in options):
            if chosen not in options:
                raise RenderError(f"{input.id}={chosen} isn't offered with {combination}: choose one of {', '.join(options)}")
            combination[input.id] = chosen
        else:
            combination[input.id] = input.default_option if input.default_option in options else options[0]
    return combination


def feature_definition(studio: ConfiguredStudio, combination: dict[str, str]) -> dict[str, str]:
    """The feature's parameters for a configuration, as FeatureScript expressions: each input's chosen option, a hidden
    input's default, a fixed input's one option."""
    definition = {}
    for input in studio.inputs:
        chosen = combination.get(input.id, input.default_option)
        definition[input.parameter] = _expression(input, chosen)
    return definition


def _expression(input: Input, chosen: str) -> str:
    if input.kind in ("boolean", "switch"):
        return "true" if chosen == "true" else "false"
    option = next(option for option in input.options if option.id == chosen)
    if input.kind == "enum":
        return f"{input.enum_name}.{option.value}"
    return _quantity(option.value)


_UNITS = {"in": "inch", "inch": "inch", "mm": "millimeter", "cm": "centimeter", "m": "meter", "ft": "foot",
          "deg": "degree", "rad": "radian"}


def _quantity(expression: str) -> str:
    """An Onshape quantity expression (`0.5 in`, `3 mm`) as FeatureScript (`0.5 * inch`)."""
    match = re.fullmatch(r"\s*([-+]?[\d.]+(?:e[-+]?\d+)?)\s*([a-z]+)\s*", expression)
    if not match or match.group(2) not in _UNITS:
        raise RenderError(f"Can't read the quantity {expression!r}")
    return f"{match.group(1)} * {_UNITS[match.group(2)]}"


def configuration_name(studio: ConfiguredStudio, combination: dict[str, str]) -> str:
    """Its choices as a file name: `MCMASTER-NO_10_32-BUTTON-BLACK-IN_0_5`."""
    return "-".join(combination[input.id] for input in studio.configured_inputs() if input.id in combination) or "default"


# Building


@dataclasses.dataclass
class Rendered:
    """A built configuration: its part's metadata, and its mesh."""

    ref: StudioRef
    combination: dict[str, str]
    metadata: dict
    mesh: object  # fs_cli.part_mesh.PartMesh

    @property
    def name(self) -> str:
        return configuration_name(self.ref.studio, self.combination)


def build(evaluator, ref: StudioRef, combination: dict[str, str]):
    """Builds a configuration, returning the `BuiltPart`."""
    from fs_eval.geometry import BuildError, build_feature

    try:
        return build_feature(evaluator, ref.module, ref.studio.feature, feature_definition(ref.studio, combination))
    except BuildError as e:
        raise RenderError(f"{ref.label} {configuration_name(ref.studio, combination)}: {e}") from e


def geometry_evaluator(config: Config):
    from fs_eval import Evaluator
    from fs_eval.geometry import available

    if not available():
        raise RenderError("Building parts needs OpenCascade: run `uv sync --group render`")
    return Evaluator(config.root, geometry=True)


# The command


DEFAULT_LIMIT = 60


def selected(config: Config, target: str) -> list[StudioRef]:
    """`SET` (each of its studios) or `SET/STUDIO`."""
    part_set, _, studio = target.partition("/")
    refs = part_set_studios(config, part_set)
    if studio:
        refs = [ref for ref in refs if ref.studio.name == studio]
        if not refs:
            names = ", ".join(ref.studio.name for ref in part_set_studios(config, part_set))
            raise RenderError(f"{part_set} has no studio {studio!r}: it has {names}")
    return refs


def parse_choices(values: list[str]) -> dict[str, str]:
    choices = {}
    for value in values:
        key, sep, option = value.partition("=")
        if not sep or not key or not option:
            raise RenderError(f"--set takes INPUT=OPTION, like size=NO_10_32, not {value!r}")
        choices[key.strip()] = option.strip()
    return choices


def combinations_for(ref: StudioRef, choices: dict[str, str], each: list[str]) -> list[dict[str, str]]:
    """The configurations to render: `choices` (where the studio has those inputs) with the rest at defaults, or, for
    `each` input, every option of it shown with them."""
    inputs = {input.id for input in ref.studio.configured_inputs()}
    own = {k: v for k, v in choices.items() if k in inputs}
    if not each:
        return [configuration(ref.studio, own)]
    # Of several studios, each varies the inputs it has (`check_inputs` reports those no studio has)
    each = [e for e in each if e in inputs]
    if not each:
        return [configuration(ref.studio, own)]
    found = []
    base = configuration(ref.studio, own)
    for combination in ref.studio.combinations():
        # Those offered with the choices given and the defaults before the inputs varied
        if any(combination.get(k) != v for k, v in own.items()):
            continue
        key = {k: combination[k] for k in each if k in combination}
        try:
            fixed = configuration(ref.studio, {**own, **key})
        except RenderError:
            continue
        if all(fixed.get(k) == base.get(k) for k in fixed if k not in each and k not in own and _before_each(ref, k, each)):
            if fixed not in found:
                found.append(fixed)
    return found or [base]


def check_inputs(refs: list[StudioRef], choices: dict[str, str], each: list[str]) -> None:
    """Reports an input `--set` or `--each` names which none of the studios has (each studio uses those it has)."""
    have = {input.id for ref in refs for input in ref.studio.configured_inputs()}
    unknown = [id for id in [*choices, *each] if id not in have]
    if unknown:
        names = ", ".join(dict.fromkeys(unknown))
        where = refs[0].label if len(refs) == 1 else "None of " + ", ".join(ref.label for ref in refs)
        verb = "has no input" if len(refs) == 1 else "has an input"
        raise RenderError(f"{where} {verb} {names}: {'it has' if len(refs) == 1 else 'they have'} {', '.join(sorted(have))}")


def _before_each(ref: StudioRef, id: str, each: list[str]) -> bool:
    order = [input.id for input in ref.studio.configured_inputs()]
    return order.index(id) < max(order.index(e) for e in each)


def choice_names(studio: ConfiguredStudio, combination: dict[str, str]) -> list[tuple[str, str]]:
    names = []
    for input in studio.configured_inputs():
        if input.id not in combination:
            continue
        option = combination[input.id]
        if input.kind == "boolean":
            names.append((input.name, "Yes" if option == "true" else "No"))
        else:
            names.append((input.name, next((o.name for o in input.options if o.id == option), option)))
    return names


def _display(config: Config, path: pathlib.Path) -> str:
    try:
        return path.resolve().relative_to(config.root.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def render_command(config: Config, args) -> int:
    """`fs render`. Returns the exit code."""
    import time

    from fs_cli import raster
    from fs_cli.part_page import page

    targets = args.targets
    if not targets:
        if not (args.all or args.check_all):
            raise RenderError("Name a part set (or a studio, like fasteners/screw) to render: there are "
                              + ", ".join(p.name for p in config.parts))
        targets = [p.name for p in config.parts]
    evaluator = geometry_evaluator(config)
    refs = [ref for target in targets for ref in selected(config, target)]
    choices = parse_choices(args.set)
    check_inputs(refs, choices, args.each)
    if args.check_all:
        return _check_all(evaluator, refs)
    work = []
    for ref in refs:
        found = every_combination(ref, choices) if args.all else combinations_for(ref, choices, args.each)
        work.extend((ref, combination) for combination in found)
    if len(work) > args.limit and not args.all:
        raise RenderError(f"That's {len(work)} configurations; render at most {args.limit} at a time (--limit), or "
                          "narrow it with --set (or render them all as a catalog, with --all)")
    if args.output:
        output = pathlib.Path(args.output)
    else:
        output = config.root / ".fs-render" / ("-".join(t.replace("/", "-") for t in args.targets) or "all")
    output.mkdir(parents=True, exist_ok=True)
    views = [v.strip() for v in args.views.split(",") if v.strip()]
    for view in views:
        if view not in raster.VIEWS:
            raise RenderError(f"No view {view!r}: there are {', '.join(raster.VIEWS)}")
    entries = []
    catalog: list[dict] = []
    failed = 0
    started_all = time.time()
    for ref, combination in work:
        label = configuration_name(ref.studio, combination)
        # A catalog's files are in SET/STUDIO/ (its URLs read as what they are); a page's beside it
        stem = f"{ref.part_set}/{ref.studio.name}/{label}" if args.all else f"{ref.part_set}-{ref.studio.name}-{label}"
        started = time.time()
        try:
            built = build(evaluator, ref, combination)
        except RenderError as error:
            print(f"FAILED {error}")
            failed += 1
            continue
        entry, files = write_configuration(built, ref, combination, output, stem, views=views, size=args.size,
                                           quality=args.quality, glb=not args.no_glb, step=args.step)
        part = entry["metadata"]["parts"][0] if entry["metadata"]["parts"] else {}
        if args.all:
            catalog.append({"partSet": ref.part_set, "studio": ref.studio.name, "configuration": combination,
                            "name": part.get("name"), "partNumber": part.get("partNumber"), "files": files})
            if len(catalog) % 100 == 0:
                print(f"{len(catalog)} of {len(work)} ({time.time() - started_all:.0f}s)")
            continue
        mass = f", {part['mass'] * 1000:.2f} g" if part.get("mass") else ""
        print(f"{ref.label} {label}: {part.get('name', '')}{mass} ({time.time() - started:.1f}s)")
        entries.append(entry)
    if args.all:
        index = {"inputs": {ref.label: [input.id for input in ref.studio.configured_inputs()] for ref in refs},
                 "configurations": catalog}
        with open(output / "index.json", "w", newline="", encoding="utf-8") as file:
            file.write(json.dumps(index, indent=4) + "\n")
        print(f"Wrote {len(catalog)} configuration{'s' if len(catalog) != 1 else ''} and their index.json to "
              f"{_display(config, output)}/ ({time.time() - started_all:.0f}s)")
        return 1 if failed else 0
    print(f"Wrote {len(entries)} configuration{'s' if len(entries) != 1 else ''} to {_display(config, output)}/")
    if entries and not args.no_page:
        title = args.title or _title(config, refs)
        document, data = page(title, entries, artifact=args.artifact)
        index = output / "index.html"
        with open(index, "w", newline="", encoding="utf-8") as file:
            file.write(document)
        if data is not None:
            with open(output / "parts.json", "w", newline="", encoding="utf-8") as file:
                file.write(data)
            print(f"Wrote {_display(config, index)}, an Artifact's page: publish it with parts.json beside it")
        else:
            print(f"Wrote {_display(config, index)}")
    return 1 if failed else 0


def every_combination(ref: StudioRef, choices: dict[str, str]) -> list[dict[str, str]]:
    """Every configuration of the studio with `choices` (those it has)."""
    inputs = {input.id for input in ref.studio.configured_inputs()}
    own = {k: v for k, v in choices.items() if k in inputs}
    if own:
        configuration(ref.studio, own)  # Checks they're offered
    return [c for c in ref.studio.combinations() if all(c.get(k) == v for k, v in own.items())]


def write_configuration(built, ref: StudioRef, combination: dict[str, str], output: pathlib.Path, stem: str, *,
                        views: list[str], size: int = 640, quality: float = 1.0, glb: bool = True,
                        step: bool = False) -> tuple[dict, dict[str, str]]:
    """Writes a built configuration's files (`stem` and `.png`, `.glb`, `.step`, and `.json`, its metadata) into
    `output`, returning its page entry (see `page_entry`) and its files by kind."""
    from fs_cli import raster
    from fs_cli.part_mesh import mesh_part, write_glb

    mesh = mesh_part(built, quality)
    entry = page_entry(built, ref, combination, mesh)
    info = entry["metadata"]
    files = {}
    (output / stem).parent.mkdir(parents=True, exist_ok=True)
    if views:
        images = [raster.render(mesh, view, size=size) for view in views]
        image = images[0] if len(images) == 1 else raster.sheet(images, columns=min(len(images), 2))
        raster.write_png(image, output / f"{stem}.png")
        files["png"] = f"{stem}.png"
    if glb:
        write_glb(mesh, output / f"{stem}.glb")
        files["glb"] = f"{stem}.glb"
    if step:
        from fs_cli.part_step import write_step

        write_step(built, output / f"{stem}.step")
        files["step"] = f"{stem}.step"
    with open(output / f"{stem}.json", "w", newline="", encoding="utf-8") as file:
        file.write(json.dumps(info, indent=4) + "\n")
    files["json"] = f"{stem}.json"
    return entry, files


def page_entry(built, ref: StudioRef, combination: dict[str, str], mesh=None) -> dict:
    """A configuration as the viewer page (`fs_cli.part_page`) shows it: its key and label, its studio (`target`, as
    `fs render` takes it, which a configuration panel names it by), its choices, by name and by id (`configuration`),
    its metadata, and its mesh."""
    from fs_cli.part_mesh import mesh_part, metadata, page_data

    if mesh is None:
        mesh = mesh_part(built)
    info = metadata(built)
    info["partSet"] = ref.part_set
    info["studio"] = ref.studio.name
    info["configuration"] = combination
    label = configuration_name(ref.studio, combination)
    return {
        "key": f"{ref.label}:{label}",
        "label": label,
        "studio": f"{ref.part_set} · {ref.studio.tab_name}",
        "target": ref.label,
        "configuration": combination,
        "choices": choice_names(ref.studio, combination),
        "metadata": info,
        "mesh": page_data(mesh),
    }


def feature_parts_page(config: Config, path: pathlib.Path, feature: str | None = None, each_option: bool = False) -> str | None:
    """For `fs audit` of a part set's Feature Studio: a viewer page (standalone, its data inline) of each of its
    studios' default configurations (with `each_option`, also each option of each input, with the rest at their
    defaults), which shows the one its configuration panels choose (see `fs_cli.audit`); None where it's no part
    set's, or OpenCascade isn't installed."""
    from fs_cli.part_page import page
    from fs_eval.geometry import available

    if not available():
        print("Note: no Parts section: building parts needs OpenCascade (uv sync --group render).")
        return None
    refs = feature_studios(config, path, feature)
    if not refs:
        return None
    evaluator = geometry_evaluator(config)
    entries = []
    for ref in refs:
        combinations = [configuration(ref.studio)]
        if each_option:
            for input in ref.studio.configured_inputs():
                combinations.extend(c for c in combinations_for(ref, {}, [input.id]) if c not in combinations)
        for combination in combinations:
            try:
                built = build(evaluator, ref, combination)
            except RenderError as error:
                print(f"Note: {error}")
                continue
            entries.append(page_entry(built, ref, combination))
    if not entries:
        return None
    return page(_title(config, refs), entries)[0]


class PartBuilder:
    """Builds the configurations a preview chooses (VS Code's, through the language server), keeping its evaluator
    while the FeatureScripts it reads are unchanged on disk, so each build after the first takes a moment."""

    def __init__(self, config: Config):
        self.config = config
        self._evaluator = None
        self._stamp: float | None = None

    def _fresh_evaluator(self):
        roots = [self.config.code_dir, self.config.std_dir]
        stamp = max((f.stat().st_mtime for root in roots if root.is_dir() for f in root.rglob("*.fs")), default=0.0)
        if self._evaluator is None or stamp != self._stamp:
            self._evaluator = geometry_evaluator(self.config)
            self._stamp = stamp
        return self._evaluator

    def build(self, target: str, settings: dict[str, str]) -> dict:
        """The page entry (see `page_entry`) of `target`'s (SET/STUDIO) configuration with `settings`, as its
        configuration panel sets them (input id to option id; what isn't offered takes what the panel shows)."""
        [ref] = selected(self.config, target)
        known = {input.id for input in ref.studio.configured_inputs()}
        combination = configuration(ref.studio, {k: v for k, v in settings.items() if k in known}, strict=False)
        return page_entry(build(self._fresh_evaluator(), ref, combination), ref, combination)


def feature_studios(config: Config, path: pathlib.Path, feature: str | None = None) -> list[StudioRef]:
    """The studios of the part sets using a feature the Feature Studio at `path` defines (or `feature`)."""
    try:
        module = path.resolve().relative_to(config.code_dir.resolve()).as_posix()
    except ValueError:
        return []
    return [ref for ref in part_set_studios(config) if ref.module == module and (feature is None or ref.studio.feature == feature)]


def _title(config: Config, refs: list[StudioRef]) -> str:
    sets = []
    for ref in refs:
        title = next((p.title for p in config.parts if p.name == ref.part_set), ref.part_set)
        if title not in sets:
            sets.append(title)
    studios = []
    for ref in refs:
        if ref.studio.tab_name not in studios:
            studios.append(ref.studio.tab_name)
    name = " & ".join(sets)
    return f"{name} {studios[0]}" if len(studios) == 1 and studios[0] not in name else name


def _check_all(evaluator, refs: list[StudioRef]) -> int:
    """Builds every configuration, reporting those that fail and why."""
    import collections
    import time

    failed_total = 0
    for ref in refs:
        started = time.time()
        failures = []
        combinations = ref.studio.combinations()
        for combination in combinations:
            try:
                built = build(evaluator, ref, combination)
                if len(built.parts) != 1:
                    raise RenderError(f"made {len(built.parts)} parts, not 1")
            except RenderError as error:
                failures.append((configuration_name(ref.studio, combination), str(error).split(": ", 1)[-1]))
        failed_total += len(failures)
        print(f"{ref.label}: {len(combinations)} configurations, {len(failures)} failed ({time.time() - started:.0f}s)")
        for message, count in collections.Counter(m for _, m in failures).most_common():
            examples = [name for name, m in failures if m == message][:3]
            print(f"  {count}x {message.splitlines()[0]}  e.g. {', '.join(examples)}")
    return 1 if failed_total else 0
