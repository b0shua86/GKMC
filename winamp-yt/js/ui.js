/* =========================================================
   ui.js
   ----------------------------------------------------------
   Window manager — drag, minimize, close, toggle, focus.
   Each <section class="win" data-window="<name>"> is a window.
   Drag handles are elements with [data-drag-handle].

   Also wires global keyboard shortcuts and the title-button
   delegate.
   ========================================================= */
(function (WY) {
  "use strict";

  const $ = WY.$;
  const KEY = "windows";

  const wins = {};   // name -> { el, openByDefault }

  // ---------- registration ----------
  function register() {
    for (const el of WY.$$(".win")) {
      const name = el.dataset.window;
      wins[name] = { el, openByDefault: true };
    }
  }

  // ---------- persistence ----------
  function saveState() {
    const out = {};
    for (const [k, w] of Object.entries(wins)) {
      const r = w.el.getBoundingClientRect();
      out[k] = {
        x: parseInt(w.el.style.left, 10) || r.left,
        y: parseInt(w.el.style.top, 10)  || r.top,
        hidden:    w.el.classList.contains("hidden"),
        minimized: w.el.classList.contains("minimized"),
        compact:   w.el.classList.contains("compact")
      };
    }
    WY.store.set(KEY, out);
  }
  function restoreState() {
    const s = WY.store.get(KEY, null);
    if (!s) return;
    for (const [k, w] of Object.entries(wins)) {
      const st = s[k]; if (!st) continue;
      if (typeof st.x === "number") w.el.style.left = st.x + "px";
      if (typeof st.y === "number") w.el.style.top  = st.y + "px";
      w.el.classList.toggle("hidden",    !!st.hidden);
      w.el.classList.toggle("minimized", !!st.minimized);
      w.el.classList.toggle("compact",   !!st.compact);
    }
  }

  // ---------- dragging ----------
  function bindDrag() {
    let drag = null;
    document.addEventListener("mousedown", (e) => {
      const handle = e.target.closest("[data-drag-handle]");
      if (!handle) return;
      // ignore clicks on title buttons
      if (e.target.closest(".title-buttons")) return;
      const win = handle.closest(".win");
      if (!win) return;
      const r = win.getBoundingClientRect();
      drag = {
        el: win,
        dx: e.clientX - r.left,
        dy: e.clientY - r.top
      };
      bringToFront(win);
      e.preventDefault();
    });
    document.addEventListener("mousemove", (e) => {
      if (!drag) return;
      const maxX = window.innerWidth  - 40;
      const maxY = window.innerHeight - 20;
      const x = WY.clamp(e.clientX - drag.dx, -40, maxX);
      const y = WY.clamp(e.clientY - drag.dy,   0, maxY);
      drag.el.style.left = x + "px";
      drag.el.style.top  = y + "px";
    });
    document.addEventListener("mouseup", () => {
      if (drag) { drag = null; saveState(); }
    });

    // touch
    document.addEventListener("touchstart", (e) => {
      const t = e.touches[0]; if (!t) return;
      const handle = e.target.closest("[data-drag-handle]"); if (!handle) return;
      if (e.target.closest(".title-buttons")) return;
      const win = handle.closest(".win"); if (!win) return;
      const r = win.getBoundingClientRect();
      drag = { el: win, dx: t.clientX - r.left, dy: t.clientY - r.top };
      bringToFront(win);
    }, { passive: true });
    document.addEventListener("touchmove", (e) => {
      if (!drag) return;
      const t = e.touches[0]; if (!t) return;
      drag.el.style.left = (t.clientX - drag.dx) + "px";
      drag.el.style.top  = (t.clientY - drag.dy) + "px";
    }, { passive: true });
    document.addEventListener("touchend", () => {
      if (drag) { drag = null; saveState(); }
    });
  }

  let zCounter = 100;
  function bringToFront(el) {
    el.style.zIndex = (++zCounter);
  }

  // ---------- title-button delegate ----------
  function bindTitleButtons() {
    document.addEventListener("click", (e) => {
      const btn = e.target.closest("[data-action]");
      if (!btn) return;
      const action = btn.dataset.action;
      const win    = btn.closest(".win");

      if (action === "close" && win) {
        win.classList.add("hidden");
        WY.bus.emit("window:close", win.dataset.window);
        saveState();
      } else if (action === "minimize" && win) {
        win.classList.toggle("minimized");
        saveState();
      } else if (action === "toggle-compact" && win) {
        win.classList.toggle("compact");
        saveState();
      } else if (action === "toggle-window") {
        const tgt = btn.dataset.target;
        WY.ui.toggle(tgt);
      }
    });
  }

  // ---------- focus on click ----------
  function bindFocus() {
    document.addEventListener("mousedown", (e) => {
      const w = e.target.closest(".win");
      if (w) bringToFront(w);
    }, true);
  }

  // ---------- keyboard shortcuts ----------
  // Z/X/C/V/B  =  prev/play/pause/stop/next   (classic Winamp)
  // M mute, L open input, R repeat, S shuffle, +/- volume,
  // P toggle playlist, E toggle EQ, V toggle vis, F fullscreen vis,
  // [ / ] visualizer presets, T = toggle CRT, G = glitch,
  // also a cheat-code buffer for easter eggs.
  function bindKeys() {
    const buf = [];
    document.addEventListener("keydown", (e) => {
      if (e.target.matches("input,textarea,select")) return;
      const k = e.key.toLowerCase();
      // cheat buffer
      buf.push(k); if (buf.length > 16) buf.shift();
      const joined = buf.join("");
      if (joined.endsWith("llama"))  { WY.effects.easter("llama");  buf.length = 0; }
      if (joined.endsWith("rave"))   { WY.effects.toggleRave();     buf.length = 0; }
      if (joined.endsWith("insane")) { WY.effects.toggleInsane();   buf.length = 0; }
      if (joined.endsWith("crt"))    { WY.effects.toggleCrt();      buf.length = 0; }
      if (joined.endsWith("y2k"))    { WY.skins.apply("y2k");       buf.length = 0; }
      if (joined.endsWith("vhs"))    { WY.skins.apply("vhs");       buf.length = 0; }
      if (joined.endsWith("retro")||joined.endsWith("classic"))
                                     { WY.skins.apply("classic");   buf.length = 0; }

      switch (k) {
        case "z": WY.playlist.prev(); break;
        case "x": WY.player.play();   break;
        case "c": WY.player.pause();  break;
        case "v": WY.player.stop();   break;
        case "b": WY.playlist.next(); break;
        case "m": WY.app.toggleMute();break;
        case "l": document.getElementById("pl-input").focus(); e.preventDefault(); break;
        case "r": WY.playlist.toggleRepeat();  break;
        case "s": WY.playlist.toggleShuffle(); break;
        case "p": WY.ui.toggle("playlist"); break;
        case "e": WY.ui.toggle("eq");       break;
        case "f": WY.visualizers.toggleFullscreen(); break;
        case "[": WY.visualizers.prev(); break;
        case "]": WY.visualizers.next(); break;
        case "arrowleft":  if (e.shiftKey) WY.visualizers.prev(); break;
        case "arrowright": if (e.shiftKey) WY.visualizers.next(); break;
        case "t": WY.effects.toggleCrt();    break;
        case "g": WY.effects.toggleGlitch(); break;
        case "+":
        case "=": WY.app.bumpVolume(+5); break;
        case "-": WY.app.bumpVolume(-5); break;
      }
    });
  }

  // ---------- API ----------
  WY.ui = {
    init() {
      register();
      restoreState();
      bindDrag();
      bindTitleButtons();
      bindFocus();
      bindKeys();
      // make sure all initially-visible windows have a z-index
      Object.values(wins).forEach(w => bringToFront(w.el));
    },

    show(name)  { wins[name] && wins[name].el.classList.remove("hidden"); saveState(); },
    hide(name)  { wins[name] && wins[name].el.classList.add("hidden"); saveState(); },
    toggle(name){
      const w = wins[name]; if (!w) return;
      const hidden = w.el.classList.toggle("hidden");
      if (!hidden) { w.el.classList.remove("minimized"); bringToFront(w.el); }
      saveState();
    },
    el(name) { return wins[name] && wins[name].el; },

    saveState
  };

})(window.WY);
