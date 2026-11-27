"""`fs render`: builds FeatureScript-backed parts (part sets' configurations, see docs/parts.md) offline, with the
evaluator's geometry (`fs_eval.geometry`, OpenCascade), and writes what they look like and what they are: a PNG of
each, a glTF model (`.glb`), their metadata (`.json`: name, vendor, part number, material, appearance, mass, size,
mate connectors, threads), and a page with a 3D viewer of them all (`index.html`, or an Artifact's, to publish).

No Onshape involved: see docs/rendering.md.
"""

from __future__ import annotations

import dataclasses
import json
import math
import pathlib
import re
import runpy

from fs_cli.config import Config
from fs_cli.configured_studios import ConfiguredStudio, Input, configured_studios, input_shows


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
        for studio in configured_studios(runpy.run_path(str(definition))):
            found.append(StudioRef(each.name, each.folder, studio, _feature_module(config, each.folder, studio.feature)))
    return found


def _feature_module(config: Config, folder: str, feature: str) -> str:
    pattern = re.compile(rf"\bexport\s+const\s+{re.escape(feature)}\s*=")
    for path in sorted((config.code_dir / folder).glob("*.fs")):
        if pattern.search(path.read_text(encoding="utf-8")):
            return path.relative_to(config.code_dir).as_posix()
    raise RenderError(f"No Feature Studio in {folder}/ exports {feature}")


# Configurations


def configuration(studio: ConfiguredStudio, choices: dict[str, str] | None = None) -> dict[str, str]:
    """A configuration of the studio: `choices` (input id to option id), checked against what it offers, the rest at
    their defaults (or, where a default isn't shown with the choices, the first option that is)."""
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
        if chosen is not None:
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
    unknown = [e for e in each if e not in inputs]
    if unknown:
        raise RenderError(f"{ref.label} has no input {', '.join(unknown)}: it has {', '.join(sorted(inputs))}")
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
    from fs_cli.part_mesh import mesh_part, metadata, page_data, write_glb
    from fs_cli.part_page import page

    evaluator = geometry_evaluator(config)
    refs = [ref for target in args.targets for ref in selected(config, target)]
    choices = parse_choices(args.set)
    if args.check_all:
        return _check_all(evaluator, refs)
    work = []
    for ref in refs:
        for combination in combinations_for(ref, choices, args.each):
            work.append((ref, combination))
    if len(work) > args.limit:
        raise RenderError(f"That's {len(work)} configurations; render at most {args.limit} at a time (--limit), or "
                          "narrow it with --set")
    output = pathlib.Path(args.output) if args.output else config.root / ".fs-render" / "-".join(
        t.replace("/", "-") for t in args.targets)
    output.mkdir(parents=True, exist_ok=True)
    views = [v.strip() for v in args.views.split(",") if v.strip()]
    for view in views:
        if view not in raster.VIEWS:
            raise RenderError(f"No view {view!r}: there are {', '.join(raster.VIEWS)}")
    entries = []
    failed = 0
    for ref, combination in work:
        label = configuration_name(ref.studio, combination)
        name = f"{ref.part_set}-{ref.studio.name}-{label}"
        started = time.time()
        try:
            built = build(evaluator, ref, combination)
        except RenderError as error:
            print(f"FAILED {error}")
            failed += 1
            continue
        mesh = mesh_part(built, args.quality)
        info = metadata(built)
        info["partSet"] = ref.part_set
        info["studio"] = ref.studio.name
        info["configuration"] = combination
        files = []
        if views:
            images = [raster.render(mesh, view, size=args.size) for view in views]
            image = images[0] if len(images) == 1 else raster.sheet(images, columns=min(len(images), 2))
            raster.write_png(image, output / f"{name}.png")
            files.append(f"{name}.png")
        if not args.no_glb:
            write_glb(mesh, output / f"{name}.glb")
            files.append(f"{name}.glb")
        with open(output / f"{name}.json", "w", newline="", encoding="utf-8") as file:
            file.write(json.dumps(info, indent=4) + "\n")
        files.append(f"{name}.json")
        part = info["parts"][0] if info["parts"] else {}
        mass = f", {part['mass'] * 1000:.2f} g" if part.get("mass") else ""
        print(f"{ref.label} {label}: {part.get('name', '')}{mass} ({time.time() - started:.1f}s)")
        entries.append({
            "key": f"{ref.label}:{label}",
            "label": label,
            "studio": f"{ref.part_set} · {ref.studio.tab_name}",
            "choices": choice_names(ref.studio, combination),
            "metadata": info,
            "mesh": page_data(mesh),
        })
    print(f"Wrote {len(entries)} configuration{'s' if len(entries) != 1 else ''} to {_display(config, output)}/")
    if entries and not args.no_page:
        title = args.title or _title(config, refs)
        subtitle = f"{len(entries)} configuration{'s' if len(entries) != 1 else ''}, built offline"
        document, data = page(title, subtitle, entries, artifact=args.artifact)
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


def feature_parts_page(config: Config, path: pathlib.Path, feature: str | None = None) -> str | None:
    """For `fs audit` of a part set's Feature Studio: a viewer page (standalone, its data inline) of each of its
    studios' default configurations; None where it's no part set's, or OpenCascade isn't installed."""
    from fs_cli.part_mesh import mesh_part, metadata, page_data
    from fs_cli.part_page import page
    from fs_eval.geometry import available

    if not available():
        print("Note: no Parts section: building parts needs OpenCascade (uv sync --group render).")
        return None
    try:
        module = path.resolve().relative_to(config.code_dir.resolve()).as_posix()
    except ValueError:
        return None
    refs = [ref for ref in part_set_studios(config) if ref.module == module and (feature is None or ref.studio.feature == feature)]
    if not refs:
        return None
    evaluator = geometry_evaluator(config)
    entries = []
    for ref in refs:
        combination = configuration(ref.studio)
        try:
            built = build(evaluator, ref, combination)
        except RenderError as error:
            print(f"Note: {error}")
            continue
        info = metadata(built)
        label = configuration_name(ref.studio, combination)
        entries.append({"key": f"{ref.label}:{label}", "label": label, "studio": f"{ref.part_set} · {ref.studio.tab_name}",
                        "choices": choice_names(ref.studio, combination), "metadata": info, "mesh": page_data(mesh_part(built))})
    if not entries:
        return None
    return page(_title(config, refs), f"{len(entries)} Part Studio{'s' if len(entries) != 1 else ''}, default configurations", entries)[0]


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
