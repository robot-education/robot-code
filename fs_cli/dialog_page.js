/**
 * Makes a feature's dialog, as fs_cli.ui.Renderer writes it, work as Onshape's does: dropdowns open, groups and array
 * items open and close, clicking a control (or entering a value) sets it, and hovering over a parameter (or a long
 * press, by touch) shows its tooltip. It's shared by `fs audit`'s page (fs_cli/audit.py) and VS Code's feature panel
 * (vscode-extension/src/uiPreview.ts), which draw the dialog differently, so each gives it a `host`:
 *
 *   set(key, value)     a control was set, as `fs ui --set` takes it
 *   remove(key, index)  an array's item was removed; without it, array items can't be added or removed, and `note`
 *                       says so
 *   note(text)          shows a message (optional)
 *   toggled             which groups and array items were opened or closed, by key, kept across draws
 *   saveToggled()       called when `toggled` changes (optional)
 *   resized()           called when the dialog's height may have changed, like an open menu's (optional)
 *
 * `root` holds the dialog, and stays as its contents are drawn again. Call the `drawn` it returns after they are.
 */
function dialogPage(root, host) {
  // A key for an expander, to remember whether it's open: its group's name, or its array item's position
  function keyOf(expander) {
    const group = expander.closest("[data-group]");
    if (expander.classList.contains("os-param-group-expander") && group) {
      return "group:" + group.dataset.group;
    }
    const item = expander.closest(".os-param-array-item");
    if (item) {
      return "item:" + item.dataset.parentParameterId + ":" + [...item.parentElement.children].indexOf(item);
    }
    return undefined;
  }

  function expanderFor(key) {
    return [...root.querySelectorAll("[data-toggle]")].find((expander) => keyOf(expander) === key);
  }

  function contentsOf(expander) {
    const item = expander.closest(".os-param-array-item");
    if (item && !expander.classList.contains("os-param-group-expander")) {
      return item.querySelector(".os-param-array-item-contents");
    }
    return expander.closest(".os-param-group-collapsible-container").querySelector(".os-param-group-collapsible-contents");
  }

  function setOpen(expander, open) {
    contentsOf(expander).classList.toggle("ng-hide", !open);
    expander.querySelector("svg").classList.toggle("expanded", open);
  }

  function resized() {
    host.resized?.();
  }

  /**
   * Places a dropdown's open menu under it, or over it if there's more room there. It's fixed in place, as Onshape's
   * float above the dialog, since the dialog's parameter list would cut it off; so it's placed again as the page
   * scrolls.
   */
  function placeMenu(toggle, menu) {
    const box = toggle.getBoundingClientRect();
    const below = window.innerHeight - box.bottom;
    const up = menu.scrollHeight > below && box.top > below;
    menu.style.left = box.left + "px";
    menu.style.minWidth = box.width + "px";
    menu.style.maxHeight = Math.min(300, (up ? box.top : below) - 4) + "px";
    menu.style.top = up ? "auto" : box.bottom + "px";
    menu.style.bottom = up ? window.innerHeight - box.top + "px" : "auto";
  }

  function openMenus() {
    return root.querySelectorAll(".os-select-dropdown.open");
  }

  function closeMenus() {
    for (const menu of openMenus()) {
      menu.classList.remove("open");
      menu.classList.add("ng-hide");
    }
  }

  function placeMenus() {
    for (const menu of openMenus()) {
      placeMenu(menu.closest(".os-select-container").querySelector(".os-select-toggle"), menu);
    }
  }

  window.addEventListener("resize", placeMenus);
  window.addEventListener("scroll", (event) => {
    // Not when the menu itself scrolls
    if (!(event.target instanceof Element && event.target.closest(".os-select-dropdown"))) {
      placeMenus();
      hideTooltip();
    }
  }, true);

  /**
   * The expander a click opens or closes: the one clicked, or the one in the group or array item header clicked (as
   * clicking a group's name does in Onshape), but not through the header's checkbox or delete button.
   */
  function expanderAt(target) {
    const expander = target.closest("[data-toggle]");
    if (expander) {
      return expander;
    }
    const header = target.closest(".os-param-group-header, .os-param-selection-list-entry");
    if (!header || target.closest("[data-set], [data-remove]")) {
      return null;
    }
    return header.querySelector("[data-toggle]");
  }

  const TYPED = "input[data-set]:not([type=checkbox])";

  root.addEventListener("click", (event) => {
    const target = event.target;
    const option = target.closest(".os-select-choices-row[data-set]");
    if (option) {
      closeMenus();
      host.set(option.dataset.set, option.dataset.value);
      return;
    }
    const toggle = target.closest(".os-select-toggle");
    if (toggle) {
      const menu = toggle.closest(".os-select-container").querySelector(".os-select-dropdown");
      const open = menu.classList.contains("open");
      closeMenus();
      if (!open) {
        menu.classList.remove("ng-hide");
        menu.classList.add("open");
        placeMenu(toggle, menu);
      }
      resized();
      return;
    }
    closeMenus();
    const expander = expanderAt(target);
    if (expander) {
      const open = !expander.querySelector("svg").classList.contains("expanded");
      setOpen(expander, open);
      const key = keyOf(expander);
      if (key) {
        host.toggled[key] = open;
        host.saveToggled?.();
      }
      resized();
      return;
    }
    const remove = target.closest("[data-remove]");
    if (!host.remove && (remove || target.closest(".os-param-array-add-button, .os-param-array-clear-button"))) {
      host.note?.("Array items can't be added or removed here: use fs ui --set, or VS Code's preview.");
      resized();
      return;
    }
    if (remove) {
      host.remove(remove.dataset.remove, Number(remove.dataset.index));
      return;
    }
    // A typed value's whole row is its target, its label too (Onshape's labels aren't tied to their fields); but not
    // an array's, whose items have their own
    const container = target.closest(".os-param-container");
    const field = container?.querySelector(TYPED);
    if (field && field.closest(".os-param-container") === container && target !== field && !target.closest("[data-set], button")) {
      field.focus();
      field.select();
      return;
    }
    if (target.closest(TYPED)) {
      // Clicking into a value selects all of it, as in Onshape, unless it's already being edited
      if (target !== editing) {
        target.select();
      }
      return;
    }
    const control = target.closest("[data-set]");
    if (control && control.dataset.value !== undefined) {
      // Checkboxes change when the dialog is drawn with the new value
      event.preventDefault();
      host.set(control.dataset.set, control.dataset.value);
    }
  });

  // What was being edited when a click started
  let editing = null;
  root.addEventListener("pointerdown", () => {
    editing = document.activeElement;
  });

  // Values are set when they're entered: Enter commits a value (which sets it, if it's new)
  root.addEventListener("keydown", (event) => {
    if (event.key === "Enter" && event.target.matches(TYPED)) {
      event.target.blur();
    }
  });
  root.addEventListener("change", (event) => {
    if (event.target.matches(TYPED)) {
      host.set(event.target.dataset.set, event.target.value);
    }
  });

  /**
   * Tooltips: a parameter's name (and a typed value) and description, as Onshape's tooltips show them, beside it,
   * then its default and UI hints.
   */
  const TOOLTIP_DELAY_MS = 500;
  const tooltip = document.createElement("div");
  tooltip.className = "fs-tooltip";
  tooltip.hidden = true;
  document.body.appendChild(tooltip);
  let tooltipFor = null;
  let tooltipTimer;

  function hideTooltip() {
    clearTimeout(tooltipTimer);
    tooltip.hidden = true;
    tooltipFor = null;
  }

  function line(className, text) {
    const element = document.createElement("div");
    element.className = className;
    element.textContent = text;
    return element;
  }

  function showTooltip(parameter) {
    const tip = parameter.dataset;
    const number = parameter.querySelector("input.os-param-number");
    const parts = [line("fs-tooltip-name", tip.tipName + (number && number.value ? ": " + number.value : ""))];
    if (tip.tipDescription) {
      parts.push(line("fs-tooltip-description", tip.tipDescription));
    }
    const details = document.createElement("dl");
    details.className = "fs-tooltip-details";
    for (const [term, value] of [["Default", tip.tipDefault], ["UI hints", tip.tipHints]]) {
      if (value) {
        const dt = document.createElement("dt");
        dt.textContent = term;
        const dd = document.createElement("dd");
        dd.textContent = value;
        details.append(dt, dd);
      }
    }
    if (details.childElementCount) {
      parts.push(details);
    }
    tooltip.replaceChildren(...parts);
    tooltip.hidden = false;
    // Beside the parameter's control, centered on it, or under it (or over it) if there's no room beside it
    const control = parameter.querySelector(
      "input, .os-select-container, .os-param-tabs, label.os-param-checkbox, .os-param-query-list, table, osc-svg-icon, .os-param-container"
    ) || parameter;
    const box = control.getBoundingClientRect();
    const size = tooltip.getBoundingClientRect();
    let left = box.right + 4;
    let top = box.top + (box.height - size.height) / 2;
    if (left + size.width > window.innerWidth) {
      left = Math.max(0, Math.min(box.left, window.innerWidth - size.width));
      top = box.bottom + 4;
      if (top + size.height > window.innerHeight) {
        top = box.top - 4 - size.height;
      }
    }
    tooltip.style.left = left + "px";
    tooltip.style.top = Math.max(0, top) + "px";
  }

  // The parameter a pointer's over, but not over an open dropdown, which covers other parameters
  function parameterAt(target) {
    return target.closest(".os-select-dropdown.open") ? null : target.closest("[data-tip-name]");
  }

  // With a mouse, hovering shows a tooltip
  root.addEventListener("pointerover", (event) => {
    if (event.pointerType !== "mouse") {
      return;
    }
    const parameter = parameterAt(event.target);
    if (parameter === tooltipFor) {
      return;
    }
    hideTooltip();
    tooltipFor = parameter;
    if (parameter) {
      tooltipTimer = setTimeout(() => showTooltip(parameter), TOOLTIP_DELAY_MS);
    }
  });
  document.documentElement.addEventListener("mouseleave", hideTooltip);

  // By touch, a tap chooses, as it does in Onshape, and a long press shows the tooltip instead (the tap it ends with
  // is ignored); the next touch hides it
  let pressTimer;
  let pressStart = null;
  let longPressed = false;
  document.addEventListener("pointerdown", (event) => {
    hideTooltip();
    if (event.pointerType === "mouse") {
      return;
    }
    const parameter = parameterAt(event.target);
    if (!parameter) {
      return;
    }
    pressStart = [event.clientX, event.clientY];
    pressTimer = setTimeout(() => {
      longPressed = true;
      tooltipFor = parameter;
      showTooltip(parameter);
    }, TOOLTIP_DELAY_MS);
  });
  document.addEventListener("pointermove", (event) => {
    if (pressStart && Math.hypot(event.clientX - pressStart[0], event.clientY - pressStart[1]) > 10) {
      clearTimeout(pressTimer);
      pressStart = null;
    }
  });
  for (const type of ["pointerup", "pointercancel"]) {
    document.addEventListener(type, () => {
      clearTimeout(pressTimer);
      pressStart = null;
    });
  }
  document.addEventListener("click", (event) => {
    if (longPressed) {
      longPressed = false;
      event.preventDefault();
      event.stopImmediatePropagation();
    }
  }, true);
  document.addEventListener("contextmenu", (event) => {
    if (longPressed || pressStart) {
      event.preventDefault();
    }
  });

  return {
    /** After the dialog's contents are drawn again: opens and closes the groups and items which were toggled. */
    drawn() {
      hideTooltip();
      for (const [key, open] of Object.entries(host.toggled)) {
        const expander = expanderFor(key);
        if (expander) {
          setOpen(expander, open);
        }
      }
      resized();
    }
  };
}
