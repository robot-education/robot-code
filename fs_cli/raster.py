"""Drawing a part's mesh (`fs_cli.part_mesh.PartMesh`) to a PNG, in software (numpy): an orthographic view, shaded
with two lights and a highlight, its edges drawn over it, antialiased by drawing it larger and scaling down. No
browser or GPU, so it draws the same anywhere, a cloud session's container included."""

from __future__ import annotations

import math
import struct
import zlib

import numpy as np

from fs_cli.part_mesh import EDGE_COLOR, PartMesh

# Where each view looks from (toward the part), Z up: Onshape's named views
VIEWS = {
    "iso": (1.0, -1.0, 1.0),
    "front": (0.0, -1.0, 0.0),
    "back": (0.0, 1.0, 0.0),
    "right": (1.0, 0.0, 0.0),
    "left": (-1.0, 0.0, 0.0),
    "top": (0.0, 0.0, 1.0),
    "bottom": (0.0, 0.0, -1.0),
    "under": (1.0, -1.0, -0.8),
}
BACKGROUND = (0.965, 0.969, 0.976)


def _camera(direction):
    """The view's axes: right, up, and toward the viewer."""
    toward = np.array(direction, dtype=float)
    toward /= np.linalg.norm(toward)
    world_up = np.array((0.0, 0.0, 1.0)) if abs(toward[2]) < 0.99 else np.array((0.0, 1.0, 0.0))
    right = np.cross(world_up, toward)
    right /= np.linalg.norm(right)
    up = np.cross(toward, right)
    return right, up, toward


def render(mesh: PartMesh, view: str | tuple = "iso", size: int = 640, supersample: int = 3, edges: bool = True,
           background=BACKGROUND) -> np.ndarray:
    """The part drawn from `view` (a name in VIEWS, or a direction to look from), as an RGB array `size` square."""
    direction = VIEWS[view] if isinstance(view, str) else view
    right, up, toward = _camera(direction)
    n = size * supersample
    # Fit the part's corners, with a margin
    lo, hi = np.array(mesh.bounds[0]), np.array(mesh.bounds[1])
    corners = np.array([[x, y, z] for x in (lo[0], hi[0]) for y in (lo[1], hi[1]) for z in (lo[2], hi[2])])
    center = (lo + hi) / 2
    projected = (corners - center) @ np.stack([right, up]).T
    extent = max(np.abs(projected).max(), 1e-9) * 1.18
    scale = (n / 2) / extent

    def to_screen(points: np.ndarray) -> np.ndarray:
        relative = points - center
        sx = relative @ right * scale + n / 2
        sy = n / 2 - relative @ up * scale
        depth = relative @ toward  # Nearer is larger
        return np.stack([sx, sy, depth], axis=-1)

    image = np.empty((n, n, 3), dtype=np.float32)
    image[:] = background
    zbuffer = np.full((n, n), -np.inf, dtype=np.float32)
    light_a = _unit(0.4 * right + 0.8 * up + 0.9 * toward)
    light_b = _unit(-0.7 * right - 0.2 * up + 0.5 * toward)
    view_dir = toward
    for primitive in mesh.primitives:
        positions = np.array(primitive.positions, dtype=np.float64).reshape(-1, 3)
        normals = np.array(primitive.normals, dtype=np.float64).reshape(-1, 3)
        lengths = np.linalg.norm(normals, axis=1, keepdims=True)
        normals = normals / np.where(lengths == 0, 1, lengths)
        indices = np.array(primitive.indices, dtype=np.int64).reshape(-1, 3)
        screen = to_screen(positions)
        color = np.array(primitive.color[:3], dtype=np.float32) ** 2.2  # Lit in linear light
        metallic = mesh.metallic
        _draw_triangles(image, zbuffer, screen, normals, indices, color, light_a, light_b, view_dir, metallic)
    if edges and mesh.edges:
        width = max(1.0, supersample * size / 640 * 1.1)
        bias = (np.abs(zbuffer[np.isfinite(zbuffer)]).max() if np.isfinite(zbuffer).any() else 1.0) * 0.004 + 2.0 / scale
        edge_color = np.array(EDGE_COLOR[:3], dtype=np.float32)
        for line in mesh.edges:
            points = to_screen(np.array(line, dtype=np.float64))
            _draw_polyline(image, zbuffer, points, width, bias, edge_color)
    # Back to display light, then down to size
    image = np.clip(image, 0, 1)
    small = image.reshape(size, supersample, size, supersample, 3).mean(axis=(1, 3))
    return small


def _unit(v):
    return v / np.linalg.norm(v)


def _shade(normals, color, light_a, light_b, view_dir, metallic):
    """Linear-light colors of points with `normals` (each row)."""
    facing = normals @ view_dir
    normals = np.where(facing[:, None] < 0, -normals, normals)
    diffuse = 0.62 * np.clip(normals @ light_a, 0, 1) + 0.22 * np.clip(normals @ light_b, 0, 1)
    ambient = 0.2 + 0.08 * np.clip(normals @ np.array([0.0, 0.0, 1.0]), -1, 1)
    half = _unit(light_a + view_dir)
    specular = np.clip(normals @ half, 0, 1) ** (24 if metallic else 40) * (0.45 if metallic else 0.22)
    base = color[None, :] * (ambient + diffuse * (1 - 0.35 * metallic))[:, None]
    tint = color[None, :] * metallic + (1 - metallic)
    linear = base + specular[:, None] * tint
    return linear


def _draw_triangles(image, zbuffer, screen, normals, indices, color, light_a, light_b, view_dir, metallic):
    n = image.shape[0]
    for a, b, c in indices:
        pa, pb, pc = screen[a], screen[b], screen[c]
        xmin = max(int(math.floor(min(pa[0], pb[0], pc[0]))), 0)
        xmax = min(int(math.ceil(max(pa[0], pb[0], pc[0]))), n - 1)
        ymin = max(int(math.floor(min(pa[1], pb[1], pc[1]))), 0)
        ymax = min(int(math.ceil(max(pa[1], pb[1], pc[1]))), n - 1)
        if xmin > xmax or ymin > ymax:
            continue
        area = (pb[0] - pa[0]) * (pc[1] - pa[1]) - (pb[1] - pa[1]) * (pc[0] - pa[0])
        if abs(area) < 1e-12:
            continue
        xs = np.arange(xmin, xmax + 1) + 0.5
        ys = np.arange(ymin, ymax + 1) + 0.5
        gx, gy = np.meshgrid(xs, ys)
        w0 = ((pb[0] - gx) * (pc[1] - gy) - (pb[1] - gy) * (pc[0] - gx)) / area
        w1 = ((pc[0] - gx) * (pa[1] - gy) - (pc[1] - gy) * (pa[0] - gx)) / area
        w2 = 1 - w0 - w1
        inside = (w0 >= -1e-6) & (w1 >= -1e-6) & (w2 >= -1e-6)
        if not inside.any():
            continue
        depth = w0 * pa[2] + w1 * pb[2] + w2 * pc[2]
        region = zbuffer[ymin:ymax + 1, xmin:xmax + 1]
        nearer = inside & (depth > region)
        if not nearer.any():
            continue
        rows, cols = np.nonzero(nearer)
        weights = np.stack([w0[rows, cols], w1[rows, cols], w2[rows, cols]], axis=1)
        point_normals = weights @ np.stack([normals[a], normals[b], normals[c]])
        point_normals /= np.maximum(np.linalg.norm(point_normals, axis=1, keepdims=True), 1e-12)
        lit = _shade(point_normals, color, light_a, light_b, view_dir, metallic)
        region[rows, cols] = depth[rows, cols]
        image[ymin + rows, xmin + cols] = np.clip(lit, 0, 1) ** (1 / 2.2)


def _draw_polyline(image, zbuffer, points, width, bias, color):
    n = image.shape[0]
    radius = width / 2
    offsets = [(dx, dy) for dx in range(-int(math.ceil(radius)), int(math.ceil(radius)) + 1)
               for dy in range(-int(math.ceil(radius)), int(math.ceil(radius)) + 1) if dx * dx + dy * dy <= radius * radius + 0.25]
    for p, q in zip(points, points[1:]):
        length = math.hypot(q[0] - p[0], q[1] - p[1])
        steps = max(int(length * 2), 1)
        t = np.linspace(0, 1, steps + 1)
        xs = p[0] + (q[0] - p[0]) * t
        ys = p[1] + (q[1] - p[1]) * t
        zs = p[2] + (q[2] - p[2]) * t
        for dx, dy in offsets:
            cx = np.floor(xs + dx).astype(np.int64)
            cy = np.floor(ys + dy).astype(np.int64)
            ok = (cx >= 0) & (cx < n) & (cy >= 0) & (cy < n)
            cx, cy, cz = cx[ok], cy[ok], zs[ok]
            visible = cz + bias >= zbuffer[cy, cx]
            image[cy[visible], cx[visible]] = color


def sheet(images: list[np.ndarray], columns: int = 2, gap: int = 6, background=BACKGROUND) -> np.ndarray:
    """Images side by side, in rows of `columns`."""
    size = images[0].shape[0]
    rows = math.ceil(len(images) / columns)
    out = np.empty((rows * size + (rows - 1) * gap, columns * size + (columns - 1) * gap, 3), dtype=np.float32)
    out[:] = background
    for i, image in enumerate(images):
        r, c = divmod(i, columns)
        out[r * (size + gap):r * (size + gap) + size, c * (size + gap):c * (size + gap) + size] = image
    return out


def write_png(image: np.ndarray, path) -> None:
    pixels = (np.clip(image, 0, 1) * 255 + 0.5).astype(np.uint8)
    height, width = pixels.shape[:2]
    raw = b"".join(b"\x00" + pixels[y].tobytes() for y in range(height))

    def chunk(kind: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)

    png = (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0))
           + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))
    with open(path, "wb") as file:
        file.write(png)
