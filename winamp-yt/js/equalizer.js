/* =========================================================
   equalizer.js
   ----------------------------------------------------------
   Visual-only equalizer. Cannot actually filter YouTube IFrame
   audio (cross-origin AudioContext is blocked), so the EQ
   sliders just:
     - persist their values to localStorage
     - paint a live "response curve" over the slider area
     - publish a "preamp boost" hint that the visualizer
       multiplies into the FFT for a subtle hands-on feel.

   Bass-boost toggles a low-shelf boost in the curve display
   and pushes the bass-band fft values in the visualizer.
   ========================================================= */
(function (WY) {
  "use strict";

  const PRESETS = {
    "Flat":        [ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
    "Rock":        [ 5, 4, 3, 1,-2,-1, 1, 3, 4, 5],
    "Hip-Hop":     [ 6, 5, 3, 1, 0,-1, 0, 1, 2, 3],
    "Punk":        [ 4, 3, 2, 0,-1, 0, 2, 3, 4, 4],
    "Bass Boost":  [ 8, 7, 5, 3, 0, 0, 0, 0, 0, 0],
    "Treble Boost":[ 0, 0, 0, 0, 0, 2, 4, 6, 7, 8],
    "Vocal":       [-2,-1, 0, 2, 4, 4, 2, 0,-1,-2],
    "Dance":       [ 6, 5, 2, 0, 0,-2,-3, 0, 4, 6]
  };

  const state = {
    on: true,
    auto: false,
    preamp: 0,
    bands: [0,0,0,0,0,0,0,0,0,0],
    bass: false
  };

  // expose to audio sim
  WY.eq = state;

  function applyToInputs() {
    const all = WY.$$(".eq-band input[type=range]");
    all.forEach(el => {
      const b = el.parentElement.dataset.band;
      if (b === "pre") el.value = state.preamp;
      else             el.value = state.bands[parseInt(b, 10)];
    });
  }

  function readInputs() {
    const all = WY.$$(".eq-band input[type=range]");
    all.forEach(el => {
      const b = el.parentElement.dataset.band;
      if (b === "pre") state.preamp = parseFloat(el.value);
      else             state.bands[parseInt(b,10)] = parseFloat(el.value);
    });
  }

  function drawCurve() {
    const c = document.getElementById("eq-curve");
    if (!c) return;
    const ctx = c.getContext("2d");
    const { w, h } = WY.fitCanvas(c);
    ctx.fillStyle = WY.cssVar("--lcd-bg", "#001a00");
    ctx.fillRect(0, 0, w, h);

    // grid
    ctx.strokeStyle = WY.cssVar("--lcd-border", "#003800");
    ctx.lineWidth = 1;
    ctx.beginPath();
    for (let i = 0; i <= 4; i++) {
      const y = (h * i) / 4;
      ctx.moveTo(0, y); ctx.lineTo(w, y);
    }
    ctx.stroke();

    // build interpolated curve from 10 bands
    const N = 10;
    ctx.strokeStyle = state.on ? WY.cssVar("--accent", "#5bff5b") : "rgba(255,255,255,.25)";
    ctx.lineWidth = 2;
    ctx.beginPath();
    for (let x = 0; x < w; x++) {
      const t = (x / w) * (N - 1);
      const i = Math.floor(t), f = t - i;
      let v = state.bands[i] + (state.bands[i+1] === undefined ? 0 : (state.bands[i+1] - state.bands[i]) * f);
      v += state.preamp * 0.5;
      if (state.bass) v += Math.max(0, 6 * (1 - x / (w * 0.35)));
      // map -18..+18 -> 0..h
      const y = h/2 - (v / 18) * (h/2 - 4);
      if (x === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
    }
    ctx.stroke();
  }

  function save() { WY.store.set("eq", state); }
  function load() {
    const s = WY.store.get("eq", null);
    if (!s) return;
    Object.assign(state, s);
  }

  WY.equalizer = {
    state,

    init() {
      load();
      applyToInputs();

      WY.$$(".eq-band input[type=range]").forEach(el => {
        el.addEventListener("input", () => { readInputs(); drawCurve(); save(); });
      });

      const presetSel = document.getElementById("eq-preset");
      WY.on(presetSel, "change", () => {
        const p = PRESETS[presetSel.value];
        if (!p) return;
        state.bands = p.slice();
        applyToInputs();
        drawCurve();
        save();
      });

      const on = document.getElementById("eq-on");
      WY.on(on, "click", () => {
        state.on = !state.on; on.classList.toggle("active", state.on);
        drawCurve(); save();
      });
      on.classList.toggle("active", state.on);

      const auto = document.getElementById("eq-auto");
      WY.on(auto, "click", () => {
        state.auto = !state.auto; auto.classList.toggle("active", state.auto);
        save();
      });
      auto.classList.toggle("active", state.auto);

      const bass = document.getElementById("eq-bass");
      WY.on(bass, "click", () => {
        state.bass = !state.bass; bass.classList.toggle("active", state.bass);
        drawCurve(); save();
      });
      bass.classList.toggle("active", state.bass);

      drawCurve();
      // redraw curve when window resizes
      window.addEventListener("resize", drawCurve);
      WY.bus.on("skin", drawCurve);
    }
  };

})(window.WY);
