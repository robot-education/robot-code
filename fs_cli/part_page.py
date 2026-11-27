"""The page `fs render` writes: a 3D viewer of the configurations it built (turn, zoom, and pan by mouse or touch),
with each one's metadata beside it: what it is, what it's made of, how big and heavy it is, its threads, and its mate
connectors. three.js loads from jsDelivr.

Written two ways: a standalone page (`index.html`, its data inline, to open from disk), or a claude.ai Artifact's
(`artifact=True`: no document around it, its data in `parts.json` beside it, which it fetches), to publish so people
can look at the parts on a phone.
"""

from __future__ import annotations

import html
import json

THREE = "https://cdn.jsdelivr.net/npm/three@0.147.0/build/three.min.js"
ORBIT = "https://cdn.jsdelivr.net/npm/three@0.147.0/examples/js/controls/OrbitControls.js"

STYLE = """
/* Layout: a viewer and a spec sheet side by side (stacked on phones), configurations as a list above them */
:root {
  --bg: #f3f5f8;
  --panel: #ffffff;
  --ink: #1b2129;
  --muted: #5d6877;
  --line: #d9dee6;
  --accent: #2f6fde;
  --accent-ink: #ffffff;
  --stage: #e4e9ef;
  --font-body: "IBM Plex Sans", system-ui, -apple-system, "Segoe UI", sans-serif;
  --font-mono: "IBM Plex Mono", ui-monospace, "SFMono-Regular", Menlo, Consolas, monospace;
  --font-display: "IBM Plex Sans Condensed", "IBM Plex Sans", system-ui, sans-serif;
}
@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) {
    --bg: #12161b; --panel: #1a2027; --ink: #e6ebf1; --muted: #98a3b1; --line: #2c343e;
    --accent: #6d9cf0; --accent-ink: #0d1117; --stage: #3b4450; color-scheme: dark;
  }
}
:root[data-theme="dark"] {
  --bg: #12161b; --panel: #1a2027; --ink: #e6ebf1; --muted: #98a3b1; --line: #2c343e;
  --accent: #6d9cf0; --accent-ink: #0d1117; --stage: #3b4450; color-scheme: dark;
}
* { box-sizing: border-box; }
[hidden] { display: none !important; }
body {
  background: var(--bg); color: var(--ink); font-family: var(--font-body); font-size: 15px; line-height: 1.45;
  padding-left: max(16px, env(safe-area-inset-left, 0px)); padding-right: max(16px, env(safe-area-inset-right, 0px));
  padding-block: 20px 32px;
}
.wrap { max-width: 1180px; margin: 0 auto; display: grid; gap: 16px; }
header { display: flex; flex-wrap: wrap; align-items: baseline; gap: 4px 14px; }
h1 { font-family: var(--font-display); font-weight: 600; font-size: 1.6rem; margin: 0; letter-spacing: 0.01em; text-wrap: balance; }
.sub { color: var(--muted); font-size: 0.9rem; }
.configs { display: flex; gap: 8px; overflow-x: auto; padding-bottom: 4px; scrollbar-width: thin; }
.config {
  flex: 0 0 auto; border: 1px solid var(--line); background: var(--panel); color: var(--ink); border-radius: 8px;
  padding: 7px 11px; font: inherit; font-size: 0.86rem; cursor: pointer; text-align: left; max-width: 260px;
}
.config small { display: block; color: var(--muted); font-family: var(--font-mono); font-size: 0.72rem; }
.config[aria-pressed="true"] { background: var(--accent); border-color: var(--accent); color: var(--accent-ink); }
.config[aria-pressed="true"] small { color: var(--accent-ink); opacity: 0.85; }
.config:focus-visible, .tool:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
.main { display: grid; grid-template-columns: minmax(0, 1.6fr) minmax(0, 1fr); gap: 16px; align-items: start; }
@media (max-width: 820px) { .main { grid-template-columns: minmax(0, 1fr); } }
.stage { position: relative; background: var(--stage); border-radius: 10px; overflow: hidden; aspect-ratio: 4 / 3; max-width: 100%; touch-action: none; }
.stage canvas { display: block; width: 100%; height: 100%; }
.tools { position: absolute; left: 10px; bottom: 10px; display: flex; gap: 6px; flex-wrap: wrap; }
.tool {
  border: 1px solid var(--line); background: var(--panel); color: var(--ink); border-radius: 6px; padding: 5px 9px;
  font: inherit; font-size: 0.8rem; cursor: pointer;
}
.tool[aria-pressed="false"] { color: var(--muted); }
.hint { position: absolute; right: 10px; top: 8px; color: var(--muted); font-size: 0.75rem; }
.status { position: absolute; inset: 0; display: grid; place-items: center; color: var(--muted); padding: 16px; text-align: center; }
.sheet { background: var(--panel); border: 1px solid var(--line); border-radius: 10px; padding: 16px; display: grid; gap: 14px; min-width: 0; }
.sheet h2 { font-family: var(--font-display); font-weight: 600; font-size: 1.15rem; margin: 0; text-wrap: balance; }
.swatch { display: inline-block; width: 12px; height: 12px; border-radius: 3px; border: 1px solid var(--line); vertical-align: -1px; margin-right: 6px; }
dl { display: grid; grid-template-columns: max-content minmax(0, 1fr); gap: 6px 14px; margin: 0; }
dt { color: var(--muted); font-size: 0.78rem; text-transform: uppercase; letter-spacing: 0.06em; padding-top: 2px; }
dd { margin: 0; overflow-wrap: anywhere; font-variant-numeric: tabular-nums; }
.mono { font-family: var(--font-mono); font-size: 0.85rem; }
.section-label { color: var(--muted); font-size: 0.78rem; text-transform: uppercase; letter-spacing: 0.06em; margin: 0 0 6px; }
.choices { display: flex; flex-wrap: wrap; gap: 6px; }
.chip { border: 1px solid var(--line); border-radius: 999px; padding: 2px 9px; font-size: 0.8rem; }
.chip b { font-weight: 500; color: var(--muted); }
a { color: var(--accent); }
footer { color: var(--muted); font-size: 0.8rem; }
@media (prefers-reduced-motion: reduce) { * { scroll-behavior: auto; } }
"""

SCRIPT = r"""
(function () {
  const state = { index: 0, edges: true, mates: true, data: null };
  const stage = document.getElementById("stage");
  const status = document.getElementById("status");
  const fail = (message) => { status.hidden = false; status.textContent = message; };
  if (!window.THREE || !THREE.OrbitControls) { fail("The 3D viewer couldn't load three.js."); return; }

  const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.outputEncoding = THREE.sRGBEncoding;
  stage.insertBefore(renderer.domElement, stage.firstChild);
  const scene = new THREE.Scene();
  const camera = new THREE.OrthographicCamera(-1, 1, 1, -1, -100, 100);
  camera.up.set(0, 0, 1);
  const controls = new THREE.OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true;
  controls.dampingFactor = 0.12;
  scene.add(new THREE.HemisphereLight(0xffffff, 0x8a93a0, 0.75));
  const key = new THREE.DirectionalLight(0xffffff, 0.75);
  camera.add(key);
  key.position.set(0.5, 1, 1.2);
  const fill = new THREE.DirectionalLight(0xffffff, 0.25);
  camera.add(fill);
  fill.position.set(-1, -0.3, 0.6);
  scene.add(camera);
  let group = null, edgeGroup = null, mateGroup = null, size = 1;

  const floats = (b64) => { const s = atob(b64); const a = new Uint8Array(s.length); for (let i = 0; i < s.length; i++) a[i] = s.charCodeAt(i); return new Float32Array(a.buffer); };
  const ints = (b64) => { const s = atob(b64); const a = new Uint8Array(s.length); for (let i = 0; i < s.length; i++) a[i] = s.charCodeAt(i); return new Uint32Array(a.buffer); };

  function resize() {
    const w = stage.clientWidth, h = stage.clientHeight;
    renderer.setSize(w, h, false);
    const aspect = w / Math.max(h, 1);
    const half = size * 0.62;
    camera.left = -half * aspect; camera.right = half * aspect; camera.top = half; camera.bottom = -half;
    camera.near = -size * 20; camera.far = size * 20;
    camera.updateProjectionMatrix();
  }

  function show(index) {
    state.index = index;
    document.querySelectorAll(".config").forEach((b, i) => b.setAttribute("aria-pressed", String(i === index)));
    const entry = state.data.parts[index];
    if (group) scene.remove(group);
    group = new THREE.Group();
    const mesh = entry.mesh;
    for (const p of mesh.primitives) {
      const geometry = new THREE.BufferGeometry();
      geometry.setAttribute("position", new THREE.BufferAttribute(floats(p.positions), 3));
      geometry.setAttribute("normal", new THREE.BufferAttribute(floats(p.normals), 3));
      geometry.setIndex(new THREE.BufferAttribute(ints(p.indices), 1));
      const color = new THREE.Color().setRGB(p.color[0], p.color[1], p.color[2]).convertSRGBToLinear();
      const material = new THREE.MeshStandardMaterial({ color, metalness: mesh.metallic * 0.8, roughness: 0.42,
        polygonOffset: true, polygonOffsetFactor: 1, polygonOffsetUnits: 1, side: THREE.DoubleSide });
      group.add(new THREE.Mesh(geometry, material));
    }
    edgeGroup = new THREE.Group();
    if (mesh.edges) {
      const geometry = new THREE.BufferGeometry();
      geometry.setAttribute("position", new THREE.BufferAttribute(floats(mesh.edges), 3));
      edgeGroup.add(new THREE.LineSegments(geometry, new THREE.LineBasicMaterial({ color: 0x15181d })));
    }
    group.add(edgeGroup);
    const lo = new THREE.Vector3(...mesh.bounds[0]), hi = new THREE.Vector3(...mesh.bounds[1]);
    const center = lo.clone().add(hi).multiplyScalar(0.5);
    size = hi.clone().sub(lo).length() || 0.01;
    mateGroup = new THREE.Group();
    for (const mc of mesh.mateConnectors) {
      const o = new THREE.Vector3(...mc.origin);
      const x = new THREE.Vector3(...mc.xAxis), z = new THREE.Vector3(...mc.zAxis);
      const y = z.clone().cross(x);
      for (const [dir, color] of [[x, 0xd9443b], [y, 0x2e9e57], [z, 0x2f6fde]]) {
        const arrow = new THREE.ArrowHelper(dir.normalize(), o, size * 0.18, color, size * 0.05, size * 0.03);
        arrow.renderOrder = 2;
        arrow.traverse((m) => { if (m.material) { m.material.depthTest = false; m.material.transparent = true; } });
        mateGroup.add(arrow);
      }
    }
    group.add(mateGroup);
    edgeGroup.visible = state.edges;
    mateGroup.visible = state.mates;
    scene.add(group);
    controls.target.copy(center);
    resetView(center);
    sheet(entry);
    try { localStorage.setItem("fs-render-config", entry.key); } catch (e) {}
  }

  function resetView(center) {
    center = center || controls.target;
    const d = new THREE.Vector3(1, -1, 0.85).normalize().multiplyScalar(size * 3);
    camera.position.copy(center).add(d);
    camera.zoom = 1;
    camera.lookAt(center);
    controls.update();
    resize();
  }

  const mm = (m) => (m * 1000).toFixed(m * 1000 < 10 ? 2 : 1) + " mm";
  const inch = (m) => (m / 0.0254).toFixed(3) + " in";
  const esc = (s) => String(s).replace(/[&<>"]/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]));

  function sheet(entry) {
    const part = entry.metadata.parts[0] || {};
    const b = entry.metadata.bounds.size;
    const rows = [];
    const row = (label, value) => { if (value !== undefined && value !== null && value !== "") rows.push(`<dt>${label}</dt><dd>${value}</dd>`); };
    row("Vendor", part.vendor && esc(part.vendor));
    const link = part.description && /^https?:/.test(part.description) ? part.description : null;
    row("Part number", part.partNumber && (link ? `<a class="mono" href="${esc(link)}" target="_blank" rel="noopener">${esc(part.partNumber)}</a>` : `<span class="mono">${esc(part.partNumber)}</span>`));
    if (!part.partNumber && link) row("Link", `<a href="${esc(link)}" target="_blank" rel="noopener">${esc(link)}</a>`);
    row("Material", part.material && esc(part.material));
    row("Finish", part.appearance && `<span class="swatch" style="background:${esc(part.appearance)}"></span><span class="mono">${esc(part.appearance)}</span>`
      + (part.faceAppearances || []).map((c) => ` <span class="swatch" style="background:${esc(c)}"></span>`).join(""));
    row("Mass", part.mass != null && `${(part.mass * 1000).toFixed(2)} g`);
    row("Volume", part.volume != null && `${(part.volume * 1e9).toFixed(1)} mm³`);
    row("Size", `${mm(b[0])} × ${mm(b[1])} × ${mm(b[2])}<br><span class="sub">${inch(b[0])} × ${inch(b[1])} × ${inch(b[2])}</span>`);
    row("Threads", (part.threads || []).map((t) => `${esc(t.size)} ${t.external ? "external" : "internal"}${t.through ? ", through" : t.depth ? `, ${mm(t.depth)} deep` : ""}`).join("<br>"));
    row("Mate connectors", entry.metadata.mateConnectors.map((m) => `<span class="mono">${esc(m.id)}</span> at z = ${mm(m.origin[2])}`).join("<br>"));
    const choices = entry.choices.map(([k, v]) => `<span class="chip"><b>${esc(k)}</b> ${esc(v)}</span>`).join("");
    document.getElementById("sheet").innerHTML =
      `<div><p class="section-label">${esc(entry.studio)}</p><h2>${esc(part.name || entry.label)}</h2></div>`
      + `<div class="choices">${choices}</div><dl>${rows.join("")}</dl>`;
  }

  function toggle(button, key, target) {
    button.addEventListener("click", () => {
      state[key] = !state[key];
      button.setAttribute("aria-pressed", String(state[key]));
      if (key === "edges" && edgeGroup) edgeGroup.visible = state.edges;
      if (key === "mates" && mateGroup) mateGroup.visible = state.mates;
    });
  }

  function start(data) {
    state.data = data;
    const list = document.getElementById("configs");
    data.parts.forEach((entry, i) => {
      const b = document.createElement("button");
      b.className = "config"; b.type = "button"; b.id = "config-" + i;
      b.innerHTML = `${esc(entry.metadata.parts[0] ? entry.metadata.parts[0].name || entry.label : entry.label)}<small>${esc(entry.label)}</small>`;
      b.addEventListener("click", () => show(i));
      list.appendChild(b);
    });
    let first = 0;
    try { const k = localStorage.getItem("fs-render-config"); const i = data.parts.findIndex((p) => p.key === k); if (i >= 0) first = i; } catch (e) {}
    status.hidden = true;
    show(first);
    (function loop() { controls.update(); renderer.render(scene, camera); requestAnimationFrame(loop); })();
  }

  toggle(document.getElementById("edges"), "edges");
  toggle(document.getElementById("mates"), "mates");
  document.getElementById("reset").addEventListener("click", () => resetView());
  new ResizeObserver(resize).observe(stage);

  const inline = document.getElementById("parts-data");
  if (inline) start(JSON.parse(inline.textContent));
  else fetch("parts.json").then((r) => { if (!r.ok) throw new Error(r.status); return r.json(); })
    .then(start).catch(() => fail("The parts' data (parts.json) didn't load. Publish it beside this page."));
})();
"""


def page(title: str, subtitle: str, entries: list[dict], artifact: bool = False) -> tuple[str, str | None]:
    """The page, and (for an Artifact) its data, to write beside it as parts.json."""
    data = json.dumps({"parts": entries}, separators=(",", ":"))
    fonts = ('<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500'
             '&family=IBM+Plex+Sans+Condensed:wght@600&family=IBM+Plex+Sans:wght@400;500;600&display=swap">')
    body = f"""<div class="wrap">
  <header><h1>{html.escape(title)}</h1><span class="sub">{html.escape(subtitle)}</span></header>
  <div class="configs" id="configs" aria-label="Configurations"></div>
  <div class="main">
    <div class="stage" id="stage">
      <div class="status" id="status">Loading the parts…</div>
      <span class="hint">Drag to turn · pinch or scroll to zoom</span>
      <div class="tools">
        <button class="tool" id="edges" type="button" aria-pressed="true">Edges</button>
        <button class="tool" id="mates" type="button" aria-pressed="true">Mate connectors</button>
        <button class="tool" id="reset" type="button">Reset view</button>
      </div>
    </div>
    <section class="sheet" id="sheet" aria-live="polite"></section>
  </div>
  <footer>Built offline from the FeatureScript by <span class="mono">fs render</span>: OpenCascade geometry, no Onshape. Threads are cosmetic, as in Onshape.</footer>
</div>"""
    inline = "" if artifact else f'<script type="application/json" id="parts-data">{_script_safe(data)}</script>'
    scripts = f'<script src="{THREE}"></script>\n<script src="{ORBIT}"></script>\n{inline}\n<script>{SCRIPT}</script>'
    head = f'<title>{html.escape(title)}</title>\n<meta name="claude:viewport-fit-sides" content="cover">\n{fonts}\n<style>{STYLE}</style>'
    if artifact:
        return f"{head}\n{body}\n{scripts}\n", data
    document = (f'<!doctype html>\n<html lang="en">\n<head>\n<meta charset="utf-8">\n'
                f'<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">\n{head}\n</head>\n'
                f"<body>\n{body}\n{scripts}\n</body>\n</html>\n")
    return document, None


def _script_safe(text: str) -> str:
    return text.replace("</", "<\\/")
