/* =========================================================
   visualizers/milkdrop.js
   ----------------------------------------------------------
   A MilkDrop-inspired psychedelic preset engine, built with
   pure Canvas 2D (no WebGL required — works on old Macs).

   Each preset is a JSON-ish "formula" describing:
     - radial symmetry count           (sym)
     - rotation per frame              (rot)
     - zoom per frame                  (zoom)
     - per-frame center drift          (driftX, driftY)
     - particle/blob count             (blobs)
     - color phase speed               (hueSpd)
     - beat flash strength             (flash)
     - kaleidoscope/mirror toggle      (kaleido)
     - waveform amplitude              (waveAmp)
     - trail strength (0=clear,1=keep) (trail)
     - bg color                        (bg)

   Two offscreen canvases implement the "recursive trail" /
   feedback warp effect: we draw the previous frame back into
   the current one, slightly rotated + zoomed + faded.
   ========================================================= */
window.WY = window.WY || {}; WY.viz = WY.viz || {};

WY.viz.milkdrop = (function () {
  "use strict";

  const BUILTIN = [
    { name: "Geo Bloom",      sym: 6, rot: 0.004, zoom: 1.012, driftX: 0.002, driftY: -0.001,
      blobs: 6, hueSpd: 0.6, flash: 0.55, kaleido: true,  waveAmp: 36, trail: 0.86, bg: "#06010a" },
    { name: "Acid Lava",      sym: 4, rot:-0.006, zoom: 1.018, driftX: 0,     driftY:  0.003,
      blobs: 5, hueSpd: 1.2, flash: 0.7,  kaleido: false, waveAmp: 60, trail: 0.92, bg: "#0a0200" },
    { name: "Crystal Cave",   sym: 8, rot: 0.010, zoom: 0.992, driftX: 0,     driftY:  0,
      blobs: 7, hueSpd: 0.3, flash: 0.4,  kaleido: true,  waveAmp: 22, trail: 0.83, bg: "#02060c" },
    { name: "Plasma Storm",   sym: 3, rot: 0.000, zoom: 1.020, driftX: 0.006, driftY:  0,
      blobs: 9, hueSpd: 1.8, flash: 1.0,  kaleido: true,  waveAmp: 80, trail: 0.94, bg: "#000000" },
    { name: "Saturn Drip",    sym: 5, rot: 0.003, zoom: 1.008, driftX: 0,     driftY: -0.004,
      blobs: 4, hueSpd: 0.4, flash: 0.3,  kaleido: false, waveAmp: 28, trail: 0.88, bg: "#02000a" },
    { name: "Lichen Dreams",  sym: 12,rot:-0.002, zoom: 1.004, driftX: 0,     driftY:  0,
      blobs: 8, hueSpd: 0.2, flash: 0.25, kaleido: true,  waveAmp: 18, trail: 0.78, bg: "#000300" }
  ];

  let off1, off2, octx1, octx2;
  let lastW = 0, lastH = 0;
  let hue = 0, t = 0;
  let presetIdx = 0;
  let _presets = BUILTIN.slice();
  let cx = 0.5, cy = 0.5; // normalised centre drift

  function ensureBuffers(w, h) {
    if (off1 && lastW === w && lastH === h) return;
    off1 = document.createElement("canvas");
    off2 = document.createElement("canvas");
    off1.width = off2.width = w;
    off1.height = off2.height = h;
    octx1 = off1.getContext("2d");
    octx2 = off2.getContext("2d");
    lastW = w; lastH = h;
  }

  function presets()       { return _presets; }
  function currentName()   { return _presets[presetIdx].name; }
  function setPreset(i)    { presetIdx = ((i % _presets.length) + _presets.length) % _presets.length; }
  function nextPreset()    { presetIdx = (presetIdx + 1) % _presets.length; }
  function prevPreset()    { presetIdx = (presetIdx - 1 + _presets.length) % _presets.length; }
  function addPreset(p)    { _presets.push(p); }
  function replacePresets(arr) { _presets = arr.slice(); presetIdx = 0; }

  // The actual frame
  function draw(ctx, w, h, f, pal) {
    ensureBuffers(w, h);
    const p = _presets[presetIdx];

    t += 0.016;
    hue = (hue + p.hueSpd * (1 + f.energy * 4)) % 360;

    // 1) draw last frame into off2, transformed (zoom + rotate + drift)
    octx2.save();
    octx2.fillStyle = p.bg;
    octx2.globalAlpha = 1 - p.trail;
    octx2.fillRect(0, 0, w, h);
    octx2.globalAlpha = 1;
    // drift centre by audio-modulated amount
    cx += p.driftX * (0.5 + f.bass);
    cy += p.driftY * (0.5 + f.mid);
    cx = (cx + 1) % 1;
    cy = (cy + 1) % 1;
    const ccx = cx * w, ccy = cy * h;
    octx2.translate(ccx, ccy);
    const zoomNow = p.zoom + (f.bass * 0.04) + (f.beat ? 0.06 : 0);
    octx2.scale(zoomNow, zoomNow);
    octx2.rotate(p.rot + f.mid * 0.02);
    octx2.translate(-ccx, -ccy);
    octx2.drawImage(off1, 0, 0);
    octx2.restore();

    // 2) draw new "stuff" on top of off2: waveform + blobs
    octx2.globalCompositeOperation = "lighter";

    // waveform — symmetric polygon
    const sym = p.sym | 0;
    octx2.lineWidth = 1.6;
    for (let s = 0; s < sym; s++) {
      const angOff = (s / sym) * Math.PI * 2;
      octx2.beginPath();
      const wave = f.wave;
      for (let i = 0; i <= wave.length; i++) {
        const u   = i / wave.length;
        const a   = angOff + u * (Math.PI * 2 / sym);
        const r   = Math.min(w,h) * 0.22
                  + wave[i % wave.length] * (p.waveAmp + f.bass * 60);
        const x   = ccx + Math.cos(a) * r;
        const y   = ccy + Math.sin(a) * r;
        if (i === 0) octx2.moveTo(x,y); else octx2.lineTo(x,y);
      }
      octx2.strokeStyle = "hsla(" + ((hue + s*30) % 360) + ",90%,65%,0.9)";
      octx2.stroke();
    }

    // beat-driven blob field
    const blobs = p.blobs;
    for (let i = 0; i < blobs; i++) {
      const a   = (i / blobs) * Math.PI * 2 + t * 0.2;
      const r   = Math.min(w,h) * (0.18 + 0.18 * Math.sin(t + i));
      const x   = ccx + Math.cos(a) * r;
      const y   = ccy + Math.sin(a) * r;
      const rad = 18 + f.bass * 60 + (f.beat ? 20 : 0);
      const grd = octx2.createRadialGradient(x, y, 0, x, y, rad);
      grd.addColorStop(0, "hsla(" + ((hue + i*47) % 360) + ",100%,70%,0.9)");
      grd.addColorStop(1, "rgba(0,0,0,0)");
      octx2.fillStyle = grd;
      octx2.beginPath(); octx2.arc(x, y, rad, 0, Math.PI*2); octx2.fill();
    }

    octx2.globalCompositeOperation = "source-over";

    // 3) kaleidoscope: mirror off2 horizontally and add back
    if (p.kaleido) {
      octx2.save();
      octx2.globalAlpha = 0.5;
      octx2.translate(w, 0); octx2.scale(-1, 1);
      octx2.drawImage(off2, 0, 0);
      octx2.restore();
    }

    // 4) beat flash overlay
    if (f.beatStrength > 0.1) {
      octx2.fillStyle = "hsla(" + (hue % 360) + ",100%,80%," + (p.flash * f.beatStrength * 0.4) + ")";
      octx2.fillRect(0, 0, w, h);
    }

    // 5) copy off2 -> off1 (history) and -> ctx (output)
    octx1.drawImage(off2, 0, 0);
    ctx.drawImage(off2, 0, 0);
  }

  // user preset save/load
  function exportCurrent() { return JSON.parse(JSON.stringify(_presets[presetIdx])); }
  function importPreset(p) {
    if (!p || !p.name) return false;
    _presets.push(p);
    presetIdx = _presets.length - 1;
    return true;
  }

  return {
    name: "MilkDrop (psychedelic)",
    draw,
    presets, currentName, setPreset, nextPreset, prevPreset,
    addPreset, replacePresets, exportCurrent, importPreset
  };
})();
