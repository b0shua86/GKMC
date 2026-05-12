/* =========================================================
   visualizers/spectrum.js
   Classic Winamp spectrum analyzer with falling peaks.
   ========================================================= */
window.WY = window.WY || {}; WY.viz = WY.viz || {};

WY.viz.spectrum = (function () {
  "use strict";

  const peaks = new Float32Array(64);

  function draw(ctx, w, h, f, pal) {
    ctx.fillStyle = pal.bg; ctx.fillRect(0, 0, w, h);

    const N    = 32;                       // bars
    const bw   = w / N;
    const step = f.fft.length / N;

    for (let i = 0; i < N; i++) {
      // average a slice of the fft
      let v = 0;
      const lo = Math.floor(i * step), hi = Math.floor((i+1) * step);
      for (let k = lo; k < hi; k++) v += f.fft[k];
      v = WY.clamp(v / (hi - lo), 0, 1);
      // mild log scaling so the bars look more "Winamp"
      v = Math.pow(v, 0.78);

      const bh = v * (h - 10);
      const x  = i * bw + 1;
      const y  = h - bh;

      // gradient: green→yellow→red, but mixed with skin palette
      const g = ctx.createLinearGradient(0, h, 0, 0);
      g.addColorStop(0,    pal.fg2);
      g.addColorStop(0.6,  pal.fg);
      g.addColorStop(1,    pal.fg3);
      ctx.fillStyle = g;
      ctx.fillRect(x, y, bw - 2, bh);

      // peak hold
      if (bh > peaks[i]) peaks[i] = bh; else peaks[i] = Math.max(0, peaks[i] - 1.6);
      ctx.fillStyle = pal.fg3;
      ctx.fillRect(x, h - peaks[i] - 2, bw - 2, 2);
    }

    // bottom baseline
    ctx.fillStyle = pal.fg2;
    ctx.globalAlpha = 0.4;
    ctx.fillRect(0, h - 1, w, 1);
    ctx.globalAlpha = 1;
  }

  return { name: "Spectrum Analyzer", draw };
})();
