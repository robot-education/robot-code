// Captures the feature dialog open in Onshape as a self-contained HTML file, for comparing `fs ui` with Onshape and
// for updating fs_cli/onshape_ui/ (see README.md). Open a feature's dialog in Onshape (usually UI test bench, from
// featurescripts/uiTestBench/uiTestBench.fs), then paste this into DevTools' console. It downloads
// "<feature type>.html": the dialog's markup (with what's typed and checked in it), the style rules which apply to it
// (and the theme's variables), and the icons it uses. Save it as featurescripts/uiTestBench/uiTestBench.html and run
//
//     uv run --group onshape-ui python -m fs_cli.onshape_ui.extract featurescripts/uiTestBench/uiTestBench.html
//
// It only reads the page.
(() => {
  // Every icon on the page, not just the dialog's (much bigger, but has icons `fs ui` uses which the dialog doesn't)
  const ALL_ICONS = false;

  const dialog = document.querySelector("#feature-dialog");
  if (!dialog) {
    throw new Error("Open a feature's dialog first.");
  }

  // Pseudo-classes and elements which depend on state or rendering, removed before matching a selector (as extract.py
  // does, and also validity, which the browser judges by what's in the inputs now)
  const DYNAMIC =
    /::?(?:hover|focus(?:-within|-visible)?|active|visited|link|checked|before|after|placeholder|selection|-webkit-[\w-]+|-moz-[\w-]+|-ms-[\w-]+|first-line|first-letter|marker|backdrop|target|indeterminate|placeholder-shown|autofill|focus-ring|scrollbar[\w-]*|file-selector-button|(?:user-)?(?:in)?valid|in-range|out-of-range)(?:\([^)]*\))?/g;
  // Rules for the whole page or its theme, kept whatever they match
  const GLOBAL = /(?:^|[\s,>])(?::root|html|body|\[data-os-theme[^\]]*\])/;
  const elements = [dialog, ...dialog.querySelectorAll("*")];

  function applies(selector) {
    if (GLOBAL.test(selector)) {
      return true;
    }
    let stripped = selector.replace(DYNAMIC, "").trim();
    if (!stripped || /[>+~]$/.test(stripped)) {
      stripped = (stripped + " *").trim();
    }
    try {
      return elements.some((element) => element.matches(stripped));
    } catch {
      // A selector the browser can't evaluate here; it can't be judged, so it's kept
      return true;
    }
  }

  /** A selector list's selectors: split at its commas, but not those inside :not(...) or [...]. */
  function splitSelectors(text) {
    const selectors = [];
    let depth = 0;
    let start = 0;
    for (let index = 0; index < text.length; index++) {
      const character = text[index];
      if (character === "(" || character === "[") {
        depth++;
      } else if (character === ")" || character === "]") {
        depth--;
      } else if (character === "," && depth === 0) {
        selectors.push(text.slice(start, index).trim());
        start = index + 1;
      }
    }
    selectors.push(text.slice(start).trim());
    return selectors.filter(Boolean);
  }

  function keep(rules) {
    const kept = [];
    for (const rule of rules) {
      if (rule instanceof CSSStyleRule) {
        const selectors = splitSelectors(rule.selectorText).filter(applies);
        if (selectors.length) {
          kept.push(`${selectors.join(",")} { ${rule.style.cssText} }`);
        }
      } else if (rule instanceof CSSMediaRule || rule instanceof CSSSupportsRule) {
        const inner = keep(rule.cssRules);
        if (inner.length) {
          const prelude = rule instanceof CSSMediaRule ? `@media ${rule.conditionText}` : `@supports ${rule.conditionText}`;
          kept.push(`${prelude} {\n${inner.join("\n")}\n}`);
        }
      }
      // @font-face, @keyframes, and @import are left out, as extract.py leaves them out
    }
    return kept;
  }

  const css = [];
  let unreadable = 0;
  for (const sheet of [...document.styleSheets, ...(document.adoptedStyleSheets || [])]) {
    let rules;
    try {
      rules = sheet.cssRules;
    } catch {
      // Another origin's stylesheet, which the page can't read
      unreadable++;
      continue;
    }
    css.push(...keep(rules));
  }

  // The markup, with what's typed and checked, which only the elements' properties hold
  const copy = dialog.cloneNode(true);
  const copies = [copy, ...copy.querySelectorAll("*")];
  elements.forEach((element, index) => {
    const clone = copies[index];
    if (element instanceof HTMLInputElement) {
      if (element.type === "checkbox" || element.type === "radio") {
        clone.toggleAttribute("checked", element.checked);
      } else {
        clone.setAttribute("value", element.value);
      }
    } else if (element instanceof HTMLTextAreaElement) {
      clone.textContent = element.value;
    }
  });

  // The icons it uses (`<use href="#...">`), which Onshape defines once for the page
  const used = new Set(
    [...dialog.querySelectorAll("use")]
      .map((use) => use.getAttribute("href") || use.getAttribute("xlink:href") || "")
      .filter((href) => href.startsWith("#"))
      .map((href) => href.slice(1))
  );
  const symbols = [...document.querySelectorAll("symbol[id]")].filter((symbol) => ALL_ICONS || used.has(symbol.id));
  const missing = [...used].filter((id) => !document.getElementById(id));

  const name = dialog.getAttribute("feature-type") || "dialog";
  const attributes = [...document.documentElement.attributes]
    .filter((attribute) => attribute.name.startsWith("data-"))
    .map((attribute) => ` ${attribute.name}="${attribute.value.replace(/"/g, "&quot;")}"`)
    .join("");
  const page =
    `<!doctype html>\n<html${attributes}><head><meta charset='utf-8'><title>${name}</title>\n` +
    `<style>\n${css.join("\n")}\n</style></head>\n<body>\n` +
    `<svg xmlns='http://www.w3.org/2000/svg' style='display: none'>\n${symbols.map((symbol) => symbol.outerHTML).join("\n")}\n</svg>\n` +
    `${copy.outerHTML}\n</body></html>\n`;

  const link = document.createElement("a");
  link.href = URL.createObjectURL(new Blob([page], { type: "text/html" }));
  link.download = `${name}.html`;
  link.click();
  setTimeout(() => URL.revokeObjectURL(link.href), 10000);
  console.log(
    `Saved ${name}.html: ${css.length} style rules, ${symbols.length} icons, ${Math.round(page.length / 1024)} KB.` +
      (unreadable ? ` ${unreadable} stylesheets couldn't be read.` : "") +
      (missing.length ? ` Icons not found: ${missing.join(", ")}.` : "")
  );
})();
