"""The page `fs render` writes: a 3D viewer of the configurations it built (turn, zoom, and pan by mouse or touch),
a list of them, and the one shown's spec sheet: what it is, what it's made of, how big and heavy it is, its threads,
and its mate connectors, drawn as Onshape draws them (a circle and its center, the triad on the one selected). three.js
loads from jsDelivr.

Written two ways: a standalone page (`index.html`, its data inline, to open from disk or show in `fs audit`), or a
claude.ai Artifact's (`artifact=True`: no document around it, its data in `parts.json` beside it, which it fetches), to
publish so people can look at the parts on a phone.
"""

from __future__ import annotations

import html
import json

THREE = "https://cdn.jsdelivr.net/npm/three@0.147.0/build/three.min.js"
ORBIT = "https://cdn.jsdelivr.net/npm/three@0.147.0/examples/js/controls/OrbitControls.js"

FONTS = ("https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500"
         "&family=IBM+Plex+Sans:wght@400;500;600&display=swap")

STYLE = """
:root {
  --bg: #eef1f5; --panel: #ffffff; --raised: #f6f8fa; --ink: #18202a; --muted: #5f6b7a; --faint: #8a95a3;
  --line: #dde2e9; --accent: #2563d9; --accent-soft: #e3ecfc; --accent-ink: #ffffff;
  --stage-a: #f4f6f9; --stage-b: #d9dfe7; --shadow: 0 1px 2px rgb(16 24 40 / 6%), 0 1px 3px rgb(16 24 40 / 8%);
  --x: #d6453d; --y: #2f9e5b; --z: #2563d9;
  --font: "IBM Plex Sans", system-ui, -apple-system, "Segoe UI", sans-serif;
  --mono: "IBM Plex Mono", ui-monospace, "SFMono-Regular", Menlo, Consolas, monospace;
}
@media (prefers-color-scheme: dark) {
  :root:not([data-theme="light"]) {
    --bg: #101419; --panel: #171c23; --raised: #1d232b; --ink: #e5eaf0; --muted: #9aa5b3; --faint: #6c7785;
    --line: #29313b; --accent: #6c9ef5; --accent-soft: #1d2b44; --accent-ink: #0b1220;
    --stage-a: #3a434e; --stage-b: #232a33; --shadow: none; color-scheme: dark;
  }
}
:root[data-theme="dark"] {
  --bg: #101419; --panel: #171c23; --raised: #1d232b; --ink: #e5eaf0; --muted: #9aa5b3; --faint: #6c7785;
  --line: #29313b; --accent: #6c9ef5; --accent-soft: #1d2b44; --accent-ink: #0b1220;
  --stage-a: #3a434e; --stage-b: #232a33; --shadow: none; color-scheme: dark;
}
* { box-sizing: border-box; }
[hidden] { display: none !important; }
html, body { height: 100%; }
body {
  margin: 0; background: var(--bg); color: var(--ink); font: 14px/1.45 var(--font);
  -webkit-font-smoothing: antialiased;
}
button { font: inherit; color: inherit; }
button:focus-visible, input:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }

/* Desktop: list | viewer | spec sheet, filling the window */
.app {
  height: 100svh; display: grid; gap: 12px;
  grid-template-columns: 272px minmax(0, 1fr) 320px; grid-template-rows: minmax(0, 1fr);
  padding-block: 12px;
  padding-left: max(12px, env(safe-area-inset-left, 0px)); padding-right: max(12px, env(safe-area-inset-right, 0px));
}
/* One part: the viewer and its spec sheet alone */
.app.single { grid-template-columns: minmax(0, 1fr) 320px; }
.app.single .list, .app.single .bar.bottom { display: none; }
.panel { background: var(--panel); border: 1px solid var(--line); border-radius: 10px; box-shadow: var(--shadow); min-width: 0; min-height: 0; }

.list { display: flex; flex-direction: column; overflow: hidden; }
.list-head { padding: 12px 12px 8px; display: grid; gap: 8px; border-bottom: 1px solid var(--line); }
.list-head h1 { margin: 0; font-size: 0.95rem; font-weight: 600; display: flex; justify-content: space-between; gap: 8px; }
.list-head h1 span { color: var(--faint); font-weight: 400; font-variant-numeric: tabular-nums; }
.search {
  width: 100%; border: 1px solid var(--line); background: var(--raised); color: var(--ink); border-radius: 7px;
  padding: 6px 9px; font: inherit; font-size: 0.85rem;
}
.items { overflow-y: auto; padding: 6px; display: grid; align-content: start; gap: 1px; }
.group { font-size: 0.7rem; text-transform: uppercase; letter-spacing: 0.07em; color: var(--faint); padding: 10px 8px 4px; }
.item {
  display: grid; gap: 1px; text-align: left; border: 0; background: none; padding: 7px 9px; border-radius: 7px;
  cursor: pointer; position: relative;
}
.item:hover { background: var(--raised); }
.item .name { font-size: 0.86rem; line-height: 1.3; }
.item .key { font-family: var(--mono); font-size: 0.7rem; color: var(--faint); overflow-wrap: anywhere; }
.item[aria-current="true"] { background: var(--accent-soft); }
.item[aria-current="true"] .name { color: var(--accent); font-weight: 500; }
.empty { color: var(--faint); font-size: 0.85rem; padding: 10px; }

.stage {
  position: relative; overflow: hidden; touch-action: none;
  background: radial-gradient(120% 90% at 50% 30%, var(--stage-a), var(--stage-b));
}
.stage canvas { position: absolute; inset: 0; width: 100%; height: 100%; display: block; }
.bar { position: absolute; display: flex; gap: 6px; flex-wrap: wrap; z-index: 1; }
.bar.top { top: 10px; left: 10px; right: 10px; justify-content: space-between; pointer-events: none; }
.bar.top > * { pointer-events: auto; }
.bar.bottom { bottom: 10px; left: 50%; transform: translateX(-50%); align-items: center; flex-wrap: nowrap; }
.group-btns { display: flex; background: var(--panel); border: 1px solid var(--line); border-radius: 8px; padding: 2px; box-shadow: var(--shadow); }
.btn {
  border: 0; background: none; border-radius: 6px; padding: 5px 9px; font-size: 0.8rem; cursor: pointer; color: var(--muted);
  display: inline-flex; align-items: center; gap: 6px; white-space: nowrap;
}
.btn:hover { color: var(--ink); background: var(--raised); }
.btn svg { width: 16px; height: 16px; }
/* A toggle: a switch inside the button, filled and accented while it's on */
.toggle .switch {
  width: 24px; height: 14px; border-radius: 7px; background: var(--line); position: relative; flex: none;
  transition: background 0.15s;
}
.toggle .switch::after {
  content: ""; position: absolute; top: 2px; left: 2px; width: 10px; height: 10px; border-radius: 50%;
  background: var(--panel); box-shadow: 0 1px 2px rgb(0 0 0 / 25%); transition: transform 0.15s;
}
.toggle[aria-pressed="true"] { color: var(--accent); background: var(--accent-soft); }
.toggle[aria-pressed="true"] .switch { background: var(--accent); }
.toggle[aria-pressed="true"] .switch::after { transform: translateX(10px); background: #fff; }
.step { font-variant-numeric: tabular-nums; font-size: 0.8rem; color: var(--muted); padding: 0 6px; min-width: 4.5em; text-align: center; }
.status { position: absolute; inset: 0; display: grid; place-items: center; color: var(--muted); padding: 16px; text-align: center; }

.sheet { overflow-y: auto; padding: 16px; display: grid; align-content: start; gap: 14px; }
.sheet h2 { margin: 0; font-size: 1.05rem; font-weight: 600; line-height: 1.3; text-wrap: balance; }
.studio { font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.07em; color: var(--faint); margin: 0 0 4px; }
.chips { display: flex; flex-wrap: wrap; gap: 5px; }
.chip { background: var(--raised); border: 1px solid var(--line); border-radius: 6px; padding: 1px 7px; font-size: 0.78rem; }
.chip b { font-weight: 400; color: var(--faint); margin-right: 4px; }
dl { display: grid; grid-template-columns: max-content minmax(0, 1fr); gap: 7px 14px; margin: 0; font-size: 0.86rem; }
dt { color: var(--muted); }
dd { margin: 0; overflow-wrap: anywhere; font-variant-numeric: tabular-nums; }
.alt { color: var(--faint); font-size: 0.78rem; }
.mono { font-family: var(--mono); font-size: 0.8rem; }
.swatch { display: inline-block; width: 11px; height: 11px; border-radius: 3px; border: 1px solid var(--line); vertical-align: -1px; margin-right: 5px; }
a { color: var(--accent); text-decoration: none; }
a:hover { text-decoration: underline; }
.section { font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.07em; color: var(--faint); margin: 2px 0 -6px; }
.mates { display: grid; gap: 2px; }
.mate {
  display: flex; align-items: center; gap: 8px; text-align: left; border: 0; background: none; border-radius: 7px;
  padding: 5px 7px; cursor: pointer; font-size: 0.84rem;
}
.mate:hover { background: var(--raised); }
.mate[aria-pressed="true"] { background: var(--accent-soft); color: var(--accent); }
.mate .glyph { width: 14px; height: 14px; border-radius: 50%; border: 2px solid currentColor; display: grid; place-items: center; flex: none; color: var(--muted); }
.mate[aria-pressed="true"] .glyph { color: var(--accent); }
.mate .glyph::after { content: ""; width: 4px; height: 4px; border-radius: 50%; background: currentColor; }
.mate .where { margin-left: auto; color: var(--faint); font-size: 0.76rem; font-variant-numeric: tabular-nums; }
.axes { display: flex; gap: 10px; font-size: 0.74rem; color: var(--faint); padding: 0 7px; }
.axes i { display: inline-block; width: 10px; height: 3px; border-radius: 2px; vertical-align: 2px; margin-right: 4px; }

/* Phones and narrow frames: viewer, spec sheet, then the list */
@media (max-width: 900px) {
  .app, .app.single { height: auto; grid-template-columns: minmax(0, 1fr); grid-template-rows: none; padding-block: 10px 24px; }
  .stage { order: 1; height: 58svh; min-height: 300px; }
  .sheet { order: 2; overflow: visible; }
  .list { order: 3; max-height: 70svh; }
  .btn .label { display: none; }
  .toggle .label { display: inline; }
}
"""

ICONS = {
    "iso": '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.3" stroke-linejoin="round"><path d="M8 1.8 14 5v6.2L8 14.4 2 11.2V5z"/><path d="M2 5l6 3.2L14 5M8 8.2v6.2"/></svg>',
    "top": '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.3"><rect x="2.5" y="2.5" width="11" height="11" rx="1.5"/><path d="M2.5 6.5h11" opacity=".45"/></svg>',
    "front": '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.3"><rect x="2.5" y="4.5" width="11" height="9" rx="1.5"/><path d="M4.5 2.5h7" opacity=".45"/></svg>',
    "right": '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.3"><rect x="4.5" y="2.5" width="9" height="11" rx="1.5"/><path d="M2.5 4.5v7" opacity=".45"/></svg>',
    "prev": '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"><path d="M10 3.5 5.5 8l4.5 4.5"/></svg>',
    "next": '<svg viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"><path d="M6 3.5 10.5 8 6 12.5"/></svg>',
}

SCRIPT = r"""
(function () {
  const $ = (id) => document.getElementById(id);
  const state = { index: -1, edges: true, mates: true, selected: -1, data: null, view: "iso" };
  const stage = $("stage"), status = $("status");
  const fail = (message) => { status.hidden = false; status.textContent = message; };
  if (!window.THREE || !THREE.OrbitControls) { fail("three.js didn't load."); return; }
  const esc = (s) => String(s).replace(/[&<>"]/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]));
  const store = { get(k) { try { return localStorage.getItem(k); } catch (e) { return null; } },
                  set(k, v) { try { localStorage.setItem(k, v); } catch (e) {} } };

  const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.outputEncoding = THREE.sRGBEncoding;
  stage.insertBefore(renderer.domElement, stage.firstChild);
  const scene = new THREE.Scene();
  const camera = new THREE.OrthographicCamera(-1, 1, 1, -1, -100, 100);
  camera.up.set(0, 0, 1);
  const controls = new THREE.OrbitControls(camera, renderer.domElement);
  controls.enableDamping = true;
  controls.dampingFactor = 0.14;
  scene.add(new THREE.HemisphereLight(0xffffff, 0x7d8794, 0.8));
  const key = new THREE.DirectionalLight(0xffffff, 0.8);
  key.position.set(0.5, 1, 1.2);
  const fill = new THREE.DirectionalLight(0xffffff, 0.3);
  fill.position.set(-1, -0.3, 0.6);
  camera.add(key, fill);
  scene.add(camera);
  let part = null, edges = null, mates = null, glyphs = [], size = 1;

  const bytes = (b64) => { const s = atob(b64); const a = new Uint8Array(s.length); for (let i = 0; i < s.length; i++) a[i] = s.charCodeAt(i); return a.buffer; };
  const floats = (b64) => new Float32Array(bytes(b64));
  const ints = (b64) => new Uint32Array(bytes(b64));

  function resize() {
    const w = stage.clientWidth, h = stage.clientHeight;
    renderer.setSize(w, h, false);
    const aspect = w / Math.max(h, 1), half = size * 0.6;
    camera.left = -half * aspect; camera.right = half * aspect; camera.top = half; camera.bottom = -half;
    camera.near = -size * 50; camera.far = size * 50;
    camera.updateProjectionMatrix();
  }

  // A mate connector as Onshape draws one: a circle in its XY plane, filled, and a dot at its origin; selected, its
  // triad (X red, Y green, Z blue). Drawn over the part, the same size on screen at any zoom.
  const MATE_PX = 13;
  function glyph(mc) {
    const g = new THREE.Group();
    const x = new THREE.Vector3(...mc.xAxis).normalize(), z = new THREE.Vector3(...mc.zAxis).normalize();
    const y = z.clone().cross(x);
    g.matrixAutoUpdate = false;
    g.userData = { origin: new THREE.Vector3(...mc.origin), basis: new THREE.Matrix4().makeBasis(x, y, z) };
    const flat = (geometry, color, opacity, order) => {
      const m = new THREE.Mesh(geometry, new THREE.MeshBasicMaterial({ color, transparent: true, opacity, depthTest: false, side: THREE.DoubleSide }));
      m.renderOrder = order;
      g.add(m);
      return m;
    };
    g.userData.disc = flat(new THREE.CircleGeometry(1, 40), 0xffffff, 0.82, 10);
    g.userData.ring = flat(new THREE.RingGeometry(0.8, 1, 40), 0x39414c, 1, 11);
    g.userData.dot = flat(new THREE.CircleGeometry(0.26, 20), 0x39414c, 1, 11);
    const triad = new THREE.Group();
    for (const [dir, color] of [[[1, 0, 0], 0xd6453d], [[0, 1, 0], 0x2f9e5b], [[0, 0, 1], 0x2563d9]]) {
      const arrow = new THREE.ArrowHelper(new THREE.Vector3(...dir), new THREE.Vector3(), 3.4, color, 0.9, 0.5);
      arrow.traverse((m) => { if (m.material) { m.material.depthTest = false; m.material.transparent = true; } m.renderOrder = 12; });
      triad.add(arrow);
    }
    triad.visible = false;
    g.userData.triad = triad;
    g.add(triad);
    return g;
  }

  function placeGlyphs() {
    const world = (camera.top - camera.bottom) / camera.zoom / Math.max(stage.clientHeight, 1);
    const s = MATE_PX * world;
    for (const g of glyphs) {
      g.matrix.copy(g.userData.basis).scale(new THREE.Vector3(s, s, s)).setPosition(g.userData.origin);
      g.matrixWorldNeedsUpdate = true;
    }
  }

  function select(i) {
    state.selected = state.selected === i ? -1 : i;
    glyphs.forEach((g, j) => {
      const on = j === state.selected;
      g.userData.triad.visible = on;
      const color = on ? 0x2563d9 : 0x39414c;
      g.userData.ring.material.color.setHex(color);
      g.userData.dot.material.color.setHex(color);
    });
    document.querySelectorAll(".mate").forEach((b, j) => b.setAttribute("aria-pressed", String(j === state.selected)));
  }

  function show(index) {
    const entries = state.data.parts;
    index = (index + entries.length) % entries.length;
    state.index = index;
    state.selected = -1;
    const entry = entries[index];
    document.querySelectorAll(".item").forEach((b) => b.setAttribute("aria-current", String(Number(b.dataset.index) === index)));
    const current = document.querySelector(`.item[data-index="${index}"]`);
    if (current) current.scrollIntoView({ block: "nearest" });
    $("step").textContent = `${index + 1} / ${entries.length}`;
    if (part) { scene.remove(part); part.traverse((o) => { if (o.geometry) o.geometry.dispose(); if (o.material) o.material.dispose(); }); }
    part = new THREE.Group();
    const mesh = entry.mesh;
    for (const p of mesh.primitives) {
      const geometry = new THREE.BufferGeometry();
      geometry.setAttribute("position", new THREE.BufferAttribute(floats(p.positions), 3));
      geometry.setAttribute("normal", new THREE.BufferAttribute(floats(p.normals), 3));
      geometry.setIndex(new THREE.BufferAttribute(ints(p.indices), 1));
      const color = new THREE.Color().setRGB(p.color[0], p.color[1], p.color[2]).convertSRGBToLinear();
      part.add(new THREE.Mesh(geometry, new THREE.MeshStandardMaterial({ color, metalness: mesh.metallic * 0.8, roughness: 0.42,
        polygonOffset: true, polygonOffsetFactor: 1, polygonOffsetUnits: 1, side: THREE.DoubleSide })));
    }
    edges = new THREE.Group();
    if (mesh.edges) {
      const geometry = new THREE.BufferGeometry();
      geometry.setAttribute("position", new THREE.BufferAttribute(floats(mesh.edges), 3));
      edges.add(new THREE.LineSegments(geometry, new THREE.LineBasicMaterial({ color: 0x14181d })));
    }
    part.add(edges);
    mates = new THREE.Group();
    glyphs = mesh.mateConnectors.map(glyph);
    glyphs.forEach((g) => mates.add(g));
    part.add(mates);
    edges.visible = state.edges;
    mates.visible = state.mates;
    scene.add(part);
    const lo = new THREE.Vector3(...mesh.bounds[0]), hi = new THREE.Vector3(...mesh.bounds[1]);
    size = hi.clone().sub(lo).length() || 0.01;
    controls.target.copy(lo.clone().add(hi).multiplyScalar(0.5));
    view(state.view);
    sheet(entry);
    store.set("fs-render-config", entry.key);
  }

  const VIEWS = { iso: [1, -1, 0.85], top: [0, -0.0001, 1], front: [0, -1, 0], right: [1, 0, 0] };
  function view(name) {
    state.view = name;
    document.querySelectorAll("[data-view]").forEach((b) => b.setAttribute("aria-pressed", String(b.dataset.view === name)));
    const d = new THREE.Vector3(...VIEWS[name]).normalize().multiplyScalar(size * 3);
    camera.position.copy(controls.target).add(d);
    camera.zoom = 1;
    camera.lookAt(controls.target);
    controls.update();
    resize();
  }

  const mm = (m) => { const v = m * 1000; return (Math.abs(v) < 10 ? v.toFixed(2) : v.toFixed(1)).replace(/\.?0+$/, "") + " mm"; };
  const inch = (m) => (m / 0.0254).toFixed(3) + " in";

  function sheet(entry) {
    const p = entry.metadata.parts[0] || {};
    const b = entry.metadata.bounds.size;
    const rows = [];
    const row = (label, value) => { if (value) rows.push(`<dt>${label}</dt><dd>${value}</dd>`); };
    const link = p.description && /^https?:/.test(p.description) ? p.description : null;
    row("Vendor", p.vendor && esc(p.vendor));
    row("Part number", p.partNumber && (link ? `<a class="mono" href="${esc(link)}" target="_blank" rel="noopener">${esc(p.partNumber)} &#8599;</a>` : `<span class="mono">${esc(p.partNumber)}</span>`));
    if (!p.partNumber && link) row("Link", `<a href="${esc(link)}" target="_blank" rel="noopener">${esc(link.replace(/^https?:\/\/(www\.)?/, ""))}</a>`);
    row("Material", p.material && esc(p.material));
    row("Appearance", p.appearance && [p.appearance, ...(p.faceAppearances || [])].map((c) => `<span class="swatch" style="background:${esc(c)}"></span>`).join("") + `<span class="mono">${esc(p.appearance)}</span>`);
    row("Mass", p.mass != null && `${(p.mass * 1000).toFixed(2)} g <span class="alt">${(p.mass / 0.45359237 * 16).toFixed(3)} oz</span>`);
    row("Size", `${b.map(mm).join(" × ")}<br><span class="alt">${b.map(inch).join(" × ")}</span>`);
    row("Volume", p.volume != null && `${(p.volume * 1e9).toFixed(1)} mm³`);
    row("Threads", (p.threads || []).map((t) => `${esc(t.size)} <span class="alt">${t.external ? "external" : "internal"}${t.through ? ", through" : t.depth ? `, ${mm(t.depth)} deep` : ""}</span>`).join("<br>"));
    const chips = entry.choices.map(([k, v]) => `<span class="chip"><b>${esc(k)}</b>${esc(v)}</span>`).join("");
    const mcs = entry.metadata.mateConnectors;
    const mateList = mcs.length ? `<p class="section">Mate connectors</p><div class="mates">${mcs.map((m, i) =>
      `<button class="mate" type="button" data-mate="${i}" aria-pressed="false"><span class="glyph"></span><span class="mono">${esc(m.id)}</span><span class="where">z ${mm(m.origin[2])}</span></button>`).join("")}</div>
      <div class="axes"><span><i style="background:var(--x)"></i>X</span><span><i style="background:var(--y)"></i>Y</span><span><i style="background:var(--z)"></i>Z</span></div>` : "";
    $("sheet").innerHTML = `<div><p class="studio">${esc(entry.studio)}</p><h2>${esc(p.name || entry.label)}</h2></div>`
      + (chips ? `<div class="chips">${chips}</div>` : "") + `<dl>${rows.join("")}</dl>` + mateList;
    document.querySelectorAll(".mate").forEach((button) => button.addEventListener("click", () => {
      if (!state.mates) setToggle("mates", true);
      select(Number(button.dataset.mate));
    }));
  }

  function list(filter) {
    const entries = state.data.parts, words = filter.toLowerCase().split(/\s+/).filter(Boolean);
    const groups = new Map();
    entries.forEach((entry, i) => {
      const name = (entry.metadata.parts[0] && entry.metadata.parts[0].name) || entry.label;
      const text = `${name} ${entry.label} ${entry.studio} ${entry.choices.map((c) => c.join(" ")).join(" ")}`.toLowerCase();
      if (words.every((w) => text.includes(w))) {
        if (!groups.has(entry.studio)) groups.set(entry.studio, []);
        groups.get(entry.studio).push(`<button class="item" type="button" data-index="${i}" aria-current="${i === state.index}"><span class="name">${esc(name)}</span><span class="key">${esc(entry.label)}</span></button>`);
      }
    });
    const many = groups.size > 1;
    $("items").innerHTML = groups.size ? [...groups].map(([studio, items]) => (many ? `<div class="group">${esc(studio)}</div>` : "") + items.join("")).join("")
      : `<div class="empty">Nothing matches.</div>`;
  }

  function setToggle(name, on) {
    state[name] = on;
    $(name).setAttribute("aria-pressed", String(on));
    if (name === "edges" && edges) edges.visible = on;
    if (name === "mates" && mates) mates.visible = on;
    store.set("fs-render-" + name, on ? "1" : "0");
  }

  function start(data) {
    state.data = data;
    const n = data.parts.length;
    $("count").textContent = n;
    $("search").hidden = n < 8;
    $("search").addEventListener("input", (e) => list(e.target.value));
    $("items").addEventListener("click", (e) => { const b = e.target.closest(".item"); if (b) show(Number(b.dataset.index)); });
    $("app").classList.toggle("single", n < 2);
    for (const name of ["edges", "mates"]) if (store.get("fs-render-" + name) === "0") setToggle(name, false);
    list("");
    const i = data.parts.findIndex((p) => p.key === store.get("fs-render-config"));
    status.hidden = true;
    show(i >= 0 ? i : 0);
    (function loop() { controls.update(); placeGlyphs(); renderer.render(scene, camera); requestAnimationFrame(loop); })();
  }

  for (const name of ["edges", "mates"]) $(name).addEventListener("click", () => setToggle(name, !state[name]));
  document.querySelectorAll("[data-view]").forEach((b) => b.addEventListener("click", () => view(b.dataset.view)));
  $("prev").addEventListener("click", () => show(state.index - 1));
  $("next").addEventListener("click", () => show(state.index + 1));
  document.addEventListener("keydown", (e) => {
    if (!state.data || e.target.closest("input")) return;
    if (e.key === "ArrowDown" || e.key === "j") { show(state.index + 1); e.preventDefault(); }
    if (e.key === "ArrowUp" || e.key === "k") { show(state.index - 1); e.preventDefault(); }
  });
  // A tap on a mate connector selects it; a tap elsewhere clears it
  const ray = new THREE.Raycaster(), down = { x: 0, y: 0 };
  renderer.domElement.addEventListener("pointerdown", (e) => { down.x = e.clientX; down.y = e.clientY; });
  renderer.domElement.addEventListener("pointerup", (e) => {
    if (Math.hypot(e.clientX - down.x, e.clientY - down.y) > 5 || !state.mates || !glyphs.length) return;
    const r = renderer.domElement.getBoundingClientRect();
    ray.setFromCamera({ x: ((e.clientX - r.left) / r.width) * 2 - 1, y: -((e.clientY - r.top) / r.height) * 2 + 1 }, camera);
    const hit = ray.intersectObjects(glyphs.map((g) => g.userData.disc))[0];
    const i = hit ? glyphs.findIndex((g) => g.userData.disc === hit.object) : -1;
    if (i >= 0) select(i); else if (state.selected >= 0) select(state.selected);
  });
  new ResizeObserver(resize).observe(stage);

  const inline = $("parts-data");
  if (inline) start(JSON.parse(inline.textContent));
  else fetch("parts.json").then((r) => { if (!r.ok) throw new Error(r.status); return r.json(); })
    .then(start).catch(() => fail("parts.json didn't load: publish it beside this page."));
})();
"""


def page(title: str, entries: list[dict], artifact: bool = False) -> tuple[str, str | None]:
    """The page, and (for an Artifact) its data, to write beside it as parts.json."""
    data = json.dumps({"parts": entries}, separators=(",", ":"))
    i = ICONS
    body = f"""<div class="app" id="app">
  <aside class="panel list">
    <div class="list-head">
      <h1>{html.escape(title)} <span id="count"></span></h1>
      <input class="search" id="search" type="search" placeholder="Filter" aria-label="Filter configurations" hidden>
    </div>
    <nav class="items" id="items" aria-label="Configurations"></nav>
  </aside>
  <main class="panel stage" id="stage">
    <div class="status" id="status">Loading&hellip;</div>
    <div class="bar top">
      <div class="group-btns">
        <button class="btn toggle" id="edges" type="button" aria-pressed="true"><span class="switch"></span><span class="label">Edges</span></button>
        <button class="btn toggle" id="mates" type="button" aria-pressed="true"><span class="switch"></span><span class="label">Mate connectors</span></button>
      </div>
      <div class="group-btns" role="group" aria-label="View">
        <button class="btn" type="button" data-view="iso" title="Isometric">{i["iso"]}<span class="label">Iso</span></button>
        <button class="btn" type="button" data-view="top" title="Top">{i["top"]}<span class="label">Top</span></button>
        <button class="btn" type="button" data-view="front" title="Front">{i["front"]}<span class="label">Front</span></button>
        <button class="btn" type="button" data-view="right" title="Right">{i["right"]}<span class="label">Right</span></button>
      </div>
    </div>
    <div class="bar bottom group-btns">
      <button class="btn" id="prev" type="button" title="Previous (&uarr;)" aria-label="Previous">{i["prev"]}</button>
      <span class="step" id="step"></span>
      <button class="btn" id="next" type="button" title="Next (&darr;)" aria-label="Next">{i["next"]}</button>
    </div>
  </main>
  <section class="panel sheet" id="sheet" aria-live="polite"></section>
</div>"""
    inline = "" if artifact else f'<script type="application/json" id="parts-data">{_script_safe(data)}</script>'
    scripts = f'<script src="{THREE}"></script>\n<script src="{ORBIT}"></script>\n{inline}\n<script>{SCRIPT}</script>'
    head = (f'<title>{html.escape(title)}</title>\n<meta name="claude:viewport-fit-sides" content="cover">\n'
            f'<link rel="stylesheet" href="{FONTS}">\n<style>{STYLE}</style>')
    if artifact:
        return f"{head}\n{body}\n{scripts}\n", data
    document = (f'<!doctype html>\n<html lang="en">\n<head>\n<meta charset="utf-8">\n'
                f'<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">\n{head}\n</head>\n'
                f"<body>\n{body}\n{scripts}\n</body>\n</html>\n")
    return document, None


def _script_safe(text: str) -> str:
    return text.replace("</", "<\\/")
