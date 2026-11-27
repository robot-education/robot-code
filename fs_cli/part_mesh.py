"""A built part (`fs_eval.geometry.BuiltPart`) as what draws it and describes it: its faces meshed into triangles,
grouped by color (a body's appearance, or a face's own, like a nylock's insert), its edges as polylines (but seams
and the edges between tangent faces, which a drawing doesn't show), its mate connectors, and its metadata. Written as
glTF (`write_glb`) and JSON, and drawn by `fs_cli.raster` and the viewer page (`fs_cli.part_page`)."""

from __future__ import annotations

import base64
import dataclasses
import json
import math
import struct

from fs_eval.geometry import fsvalues as fv

DEFAULT_COLOR = (0.62, 0.64, 0.68, 1.0)
EDGE_COLOR = (0.08, 0.09, 0.11, 1.0)


@dataclasses.dataclass
class Primitive:
    color: tuple[float, float, float, float]
    positions: list  # [x, y, z, ...]
    normals: list
    indices: list  # [a, b, c, ...]


@dataclasses.dataclass
class PartMesh:
    primitives: list[Primitive]
    edges: list[list[tuple[float, float, float]]]
    mate_connectors: list[dict]
    bounds: tuple[fv.Vec, fv.Vec]
    metallic: float = 0.0

    @property
    def size(self) -> float:
        lo, hi = self.bounds
        return fv.norm(fv.sub(hi, lo))

    def triangle_count(self) -> int:
        return sum(len(p.indices) // 3 for p in self.primitives)


def _color(value) -> tuple[float, float, float, float] | None:
    plain = fv.to_plain(value) if value is not None else None
    if not isinstance(plain, dict) or "red" not in plain:
        return None
    return (float(plain["red"]), float(plain["green"]), float(plain["blue"]), float(plain.get("alpha", 1.0)))


def mesh_part(built, quality: float = 1.0) -> PartMesh:
    """Meshes a built part's parts (its solids), within a deflection relative to its size (`quality` > 1 finer)."""
    from OCP.Bnd import Bnd_Box
    from OCP.BRep import BRep_Tool
    from OCP.BRepBndLib import BRepBndLib
    from OCP.BRepLib import BRepLib
    from OCP.GeomAbs import GeomAbs_C0
    from OCP.TopAbs import TopAbs_EDGE
    from OCP.TopExp import TopExp_Explorer
    from OCP.TopoDS import TopoDS

    from fs_eval.geometry.tessellate import edge_polyline, face_mesh, mesh

    parts = built.parts
    if not parts:
        raise ValueError("It made no parts")
    box = Bnd_Box()
    for body in parts:
        BRepBndLib.Add_s(body.shape, box)
    lo = (box.GetXMin(), box.GetYMin(), box.GetZMin())
    hi = (box.GetXMax(), box.GetYMax(), box.GetZMax())
    size = fv.norm(fv.sub(hi, lo)) or 1e-3
    deflection = size * 0.0015 / quality
    model = built.model
    groups: dict[tuple, Primitive] = {}
    edges = []
    for body in parts:
        mesh(body.shape, deflection, 0.3 / quality)
        BRepLib.EncodeRegularity_s(body.shape, 1e-3)
        body_color = _color(body.properties.get("APPEARANCE")) or DEFAULT_COLOR
        for tid in body.faces:
            face = model.entities[tid].shape
            color = _color(model.face_properties.get(tid, {}).get("APPEARANCE")) or body_color
            points, triangles, normals = face_mesh(face)
            if not triangles:
                continue
            group = groups.setdefault(color, Primitive(color, [], [], []))
            base = len(group.positions) // 3
            for p in points:
                group.positions.extend(p)
            for n in normals:
                group.normals.extend(n)
            for t in triangles:
                group.indices.extend((t[0] + base, t[1] + base, t[2] + base))
        # Edges, but seams and smooth edges between faces, which a drawing of the part doesn't show
        hidden = []
        faces_of_edges: dict[int, list] = {}
        for tid in body.faces:
            face = TopoDS.Face(model.entities[tid].shape)
            explorer = TopExp_Explorer(face, TopAbs_EDGE)
            while explorer.More():
                edge = TopoDS.Edge(explorer.Current())
                if BRep_Tool.IsClosed_s(edge, face):
                    hidden.append(edge)
                faces_of_edges.setdefault(hash(edge), []).append((edge, face))
                explorer.Next()
        for tid in body.edges:
            edge = TopoDS.Edge(model.entities[tid].shape)
            if any(edge.IsSame(h) for h in hidden):
                continue
            adjacent = [f for e, f in faces_of_edges.get(hash(edge), ()) if e.IsSame(edge)]
            unique = []
            for f in adjacent:
                if not any(f.IsSame(u) for u in unique):
                    unique.append(f)
            if len(unique) == 2 and BRep_Tool.Continuity_s(edge, unique[0], unique[1]) != GeomAbs_C0:
                continue
            line = edge_polyline(edge, deflection)
            if len(line) >= 2:
                edges.append(line)
    connectors = [
        {"id": " + ".join(b.created_by[1:]) or b.created_by[0], "origin": list(b.coord_system[0]),
         "xAxis": list(b.coord_system[1]), "zAxis": list(b.coord_system[2])}
        for b in built.mate_connectors
    ]
    metallic = 0.0
    material = fv.to_plain(parts[0].properties.get("MATERIAL")) if parts[0].properties.get("MATERIAL") is not None else None
    if isinstance(material, dict) and _is_metal(str(material.get("name", ""))):
        metallic = 0.55
    return PartMesh(list(groups.values()), edges, connectors, (lo, hi), metallic)


def _is_metal(name: str) -> bool:
    name = name.lower()
    return any(word in name for word in ("steel", "aluminum", "aluminium", "brass", "bronze", "iron", "titanium", "zinc", "copper", "metal"))


# Metadata


def metadata(built) -> dict:
    """What the part is: its properties, material, mass, size, mate connectors, and threads."""
    from OCP.BRepGProp import BRepGProp
    from OCP.GProp import GProp_GProps

    parts = built.parts
    result = {"parts": []}
    for body in parts:
        properties = {key: fv.to_plain(value) for key, value in body.properties.items()}
        props = GProp_GProps()
        BRepGProp.VolumeProperties_s(body.shape, props)
        volume = props.Mass()
        center = props.CentreOfMass()
        material = properties.get("MATERIAL") if isinstance(properties.get("MATERIAL"), dict) else None
        density = material.get("density") if material else None
        appearance = properties.get("APPEARANCE")
        entry = {
            "name": properties.get("NAME"),
            "vendor": properties.get("VENDOR"),
            "partNumber": properties.get("PART_NUMBER"),
            "description": properties.get("DESCRIPTION"),
            "material": material.get("name") if material else None,
            "density": density,
            "appearance": _hex(appearance) if isinstance(appearance, dict) else None,
            "volume": volume,
            "mass": volume * density if isinstance(density, (int, float)) else None,
            "centerOfMass": [center.X(), center.Y(), center.Z()],
            "faceAppearances": sorted({_hex(fv.to_plain(p["APPEARANCE"])) for tid, p in built.model.face_properties.items()
                                       if tid in body.faces and "APPEARANCE" in p}),
            "threads": _threads(built, body),
        }
        result["parts"].append({k: v for k, v in entry.items() if v not in (None, [], "")})
    lo, hi = mesh_bounds(parts)
    result["bounds"] = {"min": list(lo), "max": list(hi), "size": [hi[i] - lo[i] for i in range(3)]}
    result["mateConnectors"] = [
        {"id": " + ".join(b.created_by[1:]) or b.created_by[0], "origin": list(b.coord_system[0]),
         "xAxis": list(b.coord_system[1]), "zAxis": list(b.coord_system[2])}
        for b in built.mate_connectors
    ]
    return result


def mesh_bounds(parts) -> tuple[fv.Vec, fv.Vec]:
    from OCP.Bnd import Bnd_Box
    from OCP.BRepBndLib import BRepBndLib

    box = Bnd_Box()
    for body in parts:
        BRepBndLib.AddOptimal_s(body.shape, box, False, False)
    return (box.GetXMin(), box.GetYMin(), box.GetZMin()), (box.GetXMax(), box.GetYMax(), box.GetZMax())


def _hex(color: dict) -> str:
    return "#" + "".join(f"{round(float(color[c]) * 255):02x}" for c in ("red", "green", "blue"))


def _threads(built, body) -> list[dict]:
    """Its threads, from the hole attributes threading its faces (as std's External thread and Hole features set
    them)."""
    found = []
    tids = set(body.faces) | set(body.edges)
    for tid, _name, value in built.model.attributes:
        if tid not in tids:
            continue
        attribute = fv.to_plain(value)
        if not isinstance(attribute, dict) or "tapSize" not in attribute:
            continue
        depth = attribute.get("tappedDepth")
        if isinstance(depth, dict):
            depth = depth.get("value")
        thread = {
            "size": attribute.get("tapSize"),
            "external": bool(attribute.get("isExternalThread")),
            "through": bool(attribute.get("isTappedThrough")),
            "depth": depth if depth else None,
        }
        thread = {k: v for k, v in thread.items() if v is not None}
        if thread not in found:
            found.append(thread)
    return found


# glTF


def write_glb(mesh: PartMesh, path) -> None:
    """Writes the mesh as binary glTF: a mesh of a primitive per color, and its edges as lines, in meters, Z up turned
    to glTF's Y up."""
    buffer = bytearray()
    views, accessors, materials, primitives = [], [], [], []

    def add_view(data: bytes, target: int | None) -> int:
        while len(buffer) % 4:
            buffer.append(0)
        views.append({"buffer": 0, "byteOffset": len(buffer), "byteLength": len(data), **({"target": target} if target else {})})
        buffer.extend(data)
        return len(views) - 1

    def add_accessor(values: list, kind: str, component: int, count: int, bounds: bool = False) -> int:
        fmt = "f" if component == 5126 else "I"
        view = add_view(struct.pack(f"<{len(values)}{fmt}", *values), 34963 if component == 5125 else 34962)
        accessor = {"bufferView": view, "componentType": component, "count": count, "type": kind}
        if bounds:
            accessor["min"] = [min(values[i::3]) for i in range(3)]
            accessor["max"] = [max(values[i::3]) for i in range(3)]
        accessors.append(accessor)
        return len(accessors) - 1

    def y_up(values: list) -> list:
        out = []
        for i in range(0, len(values), 3):
            x, y, z = values[i:i + 3]
            out.extend((x, z, -y))
        return out

    for primitive in mesh.primitives:
        materials.append({"pbrMetallicRoughness": {"baseColorFactor": list(primitive.color), "metallicFactor": mesh.metallic,
                                                    "roughnessFactor": 0.45}, "doubleSided": False})
        positions = y_up(primitive.positions)
        primitives.append({
            "attributes": {
                "POSITION": add_accessor(positions, "VEC3", 5126, len(positions) // 3, True),
                "NORMAL": add_accessor(y_up(primitive.normals), "VEC3", 5126, len(positions) // 3),
            },
            "indices": add_accessor(primitive.indices, "SCALAR", 5125, len(primitive.indices)),
            "material": len(materials) - 1,
        })
    if mesh.edges:
        materials.append({"pbrMetallicRoughness": {"baseColorFactor": list(EDGE_COLOR), "metallicFactor": 0.0}})
        positions = []
        for line in mesh.edges:
            for a, b in zip(line, line[1:]):
                positions.extend(a)
                positions.extend(b)
        positions = y_up(positions)
        primitives.append({"attributes": {"POSITION": add_accessor(positions, "VEC3", 5126, len(positions) // 3, True)},
                           "mode": 1, "material": len(materials) - 1})
    document = {
        "asset": {"version": "2.0", "generator": "robot-code fs render"},
        "scene": 0,
        "scenes": [{"nodes": [0]}],
        "nodes": [{"mesh": 0}],
        "meshes": [{"primitives": primitives}],
        "materials": materials,
        "accessors": accessors,
        "bufferViews": views,
        "buffers": [{"byteLength": len(buffer)}],
    }
    text = json.dumps(document, separators=(",", ":")).encode()
    text += b" " * (-len(text) % 4)
    while len(buffer) % 4:
        buffer.append(0)
    body = struct.pack("<II", len(text), 0x4E4F534A) + text + struct.pack("<II", len(buffer), 0x004E4942) + bytes(buffer)
    data = struct.pack("<III", 0x46546C67, 2, 12 + len(body)) + body
    with open(path, "wb") as file:
        file.write(data)


# The viewer page's data


def page_data(mesh: PartMesh, precision: float = 1e-6) -> dict:
    """The mesh as the viewer page loads it: each primitive's positions (quantized to `precision` meters), normals,
    and indices, base64 encoded, and its edges'."""
    def floats(values) -> str:
        return base64.b64encode(struct.pack(f"<{len(values)}f", *values)).decode()

    def ints(values) -> str:
        return base64.b64encode(struct.pack(f"<{len(values)}I", *values)).decode()

    edges = []
    for line in mesh.edges:
        for a, b in zip(line, line[1:]):
            edges.extend(a)
            edges.extend(b)
    return {
        "primitives": [
            {"color": list(p.color), "positions": floats([round(v / precision) * precision for v in p.positions]),
             "normals": floats([round(v, 4) for v in p.normals]), "indices": ints(p.indices)}
            for p in mesh.primitives
        ],
        "edges": floats(edges),
        "mateConnectors": mesh.mate_connectors,
        "bounds": [list(mesh.bounds[0]), list(mesh.bounds[1])],
        "metallic": mesh.metallic,
    }
