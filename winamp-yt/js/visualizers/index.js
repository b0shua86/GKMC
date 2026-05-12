/* =========================================================
   visualizers/index.js
   ----------------------------------------------------------
   Visualizer host:
     - keeps the list of registered visualizers
     - runs the main render loop (requestAnimationFrame)
     - feeds each visualizer the per-frame audio data
     - draws the small inline "LCD" spectrum on the main window
     - handles preset switching, fullscreen, save/load presets,
       rave/insane mode hooks, beat-driven flash overlay.

   To add a new visualizer:
     1. create  js/visualizers/mything.js
        - it must register as  WY.viz.mything = { name, draw(ctx,w,h,frame,pal) }
        - draw() receives:
            ctx  : 2D context of the visualizer canvas
            w,h  : current size (already DPR-fitted)
            frame: object from WY.audio.frame()
            pal  : current skin's visualizer palette
     2. add  <script src="js/visualizers/mything.js"></script>  to index.html
        (before this file).
     3. add it to the LIST array below.

   To add a new MilkDrop preset specifically, see milkdrop.js
   — call WY.viz.milkdrop.addPreset({...}) at startup, or save
   one from the UI ("SAVE" button) which stashes it in
   localStorage and prepends it to the user preset dropdown.
   ========================================================= */
(function (WY) {
  "use strict";

  // Order = preset cycle order in the visualizer window.
  const LIST = [
    WY.viz.spectrum,
    WY.viz.oscilloscope,
    WY.viz.vumeter,
    WY.viz.tunnel,
    WY.viz.particles,
    WY.viz.waveformTunnel,
    WY.viz.milkdrop
  ];

  let cur = 0;
  let canvas, ctx;
  let lcdCanvas, lcdCtx;
  let raf = 0;

  function palette() {
    return {
      bg:  WY.cssVar("--vis-bg",  "#000"),
      fg:  WY.cssVar("--vis-fg",  "#5bff5b"),
      fg2: WY.cssVar("--vis-fg2", "#00ff66"),
      fg3: WY.cssVar("--vis-fg3", "#ffffff")
    };
  }

  // ---------- main loop ----------
  function loop() {
    raf = requestAnimationFrame(loop);

    const f   = WY.audio.frame();
    const pal = palette();

    // time + seek display
    syncTransportUI(f);

    // main visualizer canvas
    const { w, h } = WY.fitCanvas(canvas);
    LIST[cur].draw(ctx, w, h, f, pal);

    // tiny LCD spectrum in the main window
    drawLcdMini(f, pal);

    // beat flash overlay
    if (f.beat) {
      const el = document.getElementById("fx-flash");
      if (el) {
        el.classList.add("on");
        clearTimeout(el._t);
        el._t = setTimeout(() => el.classList.remove("on"), 70);
      }
    }
  }

  // The tiny inline spectrum in the main window's LCD area.
  function drawLcdMini(f, pal) {
    if (!lcdCtx) return;
    const w = lcdCanvas.width, h = lcdCanvas.height;
    lcdCtx.fillStyle = "rgba(0,0,0,0.55)";
    lcdCtx.fillRect(0, 0, w, h);
    const N = 19;
    const bw = w / N;
    for (let i = 0; i < N; i++) {
      const v = f.fft[Math.floor(i / N * f.fft.length)];
      const bh = v * (h - 2);
      lcdCtx.fillStyle = pal.fg;
      lcdCtx.fillRect(i * bw + 1, h - bh, bw - 1, bh);
    }
  }

  // Pull current time from the player and update the LCD readout +
  // seek bar (but not while user is dragging the seek).
  let userSeeking = false;
  function syncTransportUI(f) {
    const t = WY.player.getCurrentTime();
    const d = WY.player.getDuration();
    WY.audio.setTime(t);

    const lcd = document.getElementById("lcd-time");
    if (lcd) lcd.textContent = WY.fmtTime(t);

    const ts = document.getElementById("seek-time");
    if (ts) ts.textContent = WY.fmtTime(t) + " / " + WY.fmtTime(d);

    const sk = document.getElementById("seek");
    if (sk && d > 0 && !userSeeking) sk.value = (t / d) * 1000;

    // scroll marquee
    const tr = document.getElementById("marquee-track");
    const mt = document.getElementById("marquee-text");
    if (tr && mt) {
      tr._x = (tr._x || 0) - 0.7;
      const tw = mt.offsetWidth;
      const cw = tr.parentElement.offsetWidth;
      if (tr._x < -tw) tr._x = cw;
      tr.style.transform = "translateX(" + tr._x + "px)";
    }
  }

  // ---------- preset dropdown ----------
  function buildSelect() {
    const sel = document.getElementById("vis-select"); if (!sel) return;
    sel.innerHTML = "";
    LIST.forEach((v, i) => {
      const o = document.createElement("option");
      o.value = i; o.textContent = v.name;
      sel.appendChild(o);
    });
    sel.value = cur;
    sel.addEventListener("change", () => setPreset(parseInt(sel.value, 10)));

    // MilkDrop sub-preset selector lives inside the same dropdown
    // (we just label "MilkDrop: <preset name>"). For richer UX it's
    // also exposed via [ / ] when the current visualizer is milkdrop.
  }

  function buildUserPresetSelect() {
    const sel = document.getElementById("vis-userpresets");
    if (!sel) return;
    refreshUserPresetSelect();
    sel.addEventListener("change", () => {
      const idx = parseInt(sel.value, 10);
      if (isNaN(idx)) return;
      const arr = WY.store.get("milkdrop-presets", []);
      const p = arr[idx];
      if (p) {
        WY.viz.milkdrop.importPreset(p);
        setPreset(LIST.indexOf(WY.viz.milkdrop));
        WY.toast("Loaded preset: " + p.name);
      }
    });
  }
  function refreshUserPresetSelect() {
    const sel = document.getElementById("vis-userpresets");
    if (!sel) return;
    const arr = WY.store.get("milkdrop-presets", []);
    sel.innerHTML = "<option value=''>— user presets —</option>";
    arr.forEach((p, i) => {
      const o = document.createElement("option");
      o.value = i; o.textContent = p.name;
      sel.appendChild(o);
    });
  }

  function setPreset(i) {
    cur = (i + LIST.length) % LIST.length;
    const sel = document.getElementById("vis-select");
    if (sel) sel.value = cur;
    WY.toast("Visualizer: " + LIST[cur].name);
    WY.store.set("vizIdx", cur);
  }

  function next() {
    // If we're already on milkdrop, cycle its sub-presets first
    if (LIST[cur] === WY.viz.milkdrop) {
      const md = WY.viz.milkdrop;
      const ps = md.presets();
      if (ps.length > 1) {
        const before = md.currentName();
        md.nextPreset();
        const after = md.currentName();
        if (before !== after) { WY.toast("Preset: " + after); return; }
      }
    }
    setPreset(cur + 1);
    if (LIST[cur] === WY.viz.milkdrop) WY.toast("Preset: " + WY.viz.milkdrop.currentName());
  }
  function prev() {
    if (LIST[cur] === WY.viz.milkdrop) {
      const md = WY.viz.milkdrop;
      const ps = md.presets();
      if (ps.length > 1) {
        const before = md.currentName();
        md.prevPreset();
        const after = md.currentName();
        if (before !== after) { WY.toast("Preset: " + after); return; }
      }
    }
    setPreset(cur - 1);
    if (LIST[cur] === WY.viz.milkdrop) WY.toast("Preset: " + WY.viz.milkdrop.currentName());
  }

  function toggleFullscreen() {
    const win = document.getElementById("win-vis");
    if (!win) return;
    win.classList.toggle("fullscreen-vis");
    win.classList.remove("hidden", "minimized");
  }

  function saveCurrentPreset() {
    if (LIST[cur] !== WY.viz.milkdrop) {
      WY.toast("Switch to MilkDrop to save a preset");
      setPreset(LIST.indexOf(WY.viz.milkdrop));
      return;
    }
    const p = WY.viz.milkdrop.exportCurrent();
    const name = prompt("Save preset as:", p.name + " (mine)");
    if (!name) return;
    p.name = name;
    const arr = WY.store.get("milkdrop-presets", []);
    arr.push(p);
    WY.store.set("milkdrop-presets", arr);
    refreshUserPresetSelect();
    WY.toast("Saved preset: " + name);
  }

  // ---------- public ----------
  WY.visualizers = {
    LIST,
    init() {
      canvas    = document.getElementById("vis-canvas");
      ctx       = canvas.getContext("2d");
      lcdCanvas = document.getElementById("lcd-vis");
      lcdCtx    = lcdCanvas.getContext("2d");

      buildSelect();
      buildUserPresetSelect();

      // restore saved choice
      const saved = WY.store.get("vizIdx", 6); // start on MilkDrop
      setPreset(saved);

      WY.on(document.getElementById("vis-prev"), "click", prev);
      WY.on(document.getElementById("vis-next"), "click", next);
      WY.on(document.getElementById("vis-full"), "click", toggleFullscreen);
      WY.on(document.getElementById("vis-save"), "click", saveCurrentPreset);
      WY.on(document.getElementById("vis-rave"),   "click", () => WY.effects.toggleRave());
      WY.on(document.getElementById("vis-insane"), "click", () => WY.effects.toggleInsane());

      // seek bar interactions
      const seek = document.getElementById("seek");
      WY.on(seek, "mousedown",  () => { userSeeking = true; });
      WY.on(seek, "touchstart", () => { userSeeking = true; }, { passive: true });
      const release = () => {
        if (!userSeeking) return;
        userSeeking = false;
        const d = WY.player.getDuration();
        const v = parseFloat(seek.value) / 1000;
        WY.player.seekTo(v * d);
      };
      WY.on(seek, "mouseup",   release);
      WY.on(seek, "touchend",  release);
      WY.on(seek, "change",    release);

      // start RAF
      raf = requestAnimationFrame(loop);
    },

    next, prev, setPreset, toggleFullscreen, saveCurrentPreset,
    current() { return LIST[cur]; }
  };

})(window.WY);
