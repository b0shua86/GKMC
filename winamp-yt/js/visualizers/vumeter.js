/* =========================================================
   visualizers/vumeter.js
   Stereo VU meters — left = bass+mid, right = mid+treble.
   Pixel-stepped bars so they look like proper LED meters.
   ========================================================= */
window.WY = window.WY || {}; WY.viz = WY.viz || {};

WY.viz.vumeter = (function () {
  "use strict";

  const env = { L: 0, R: 0, peakL: 0, peakR: 0 };

  function drawMeter(ctx, x, y, w, h, level, peak, pal, label) {
    const cells = 24;
    const cellH = (h - 18) / cells;
    for (let i = 0; i < cells; i++) {
      const lit = (cells - i) / cells <= level;
      const t = (cells - i) / cells;
      let color = pal.fg2;
      if (t > 0.66) color = pal.fg3;
      else if (t > 0.4) color = pal.fg;
      ctx.fillStyle = lit ? color : "rgba(255,255,255,0.07)";
      ctx.fillRect(x + 2, y + i * cellH + 14, w - 4, cellH - 1);
    }
    // peak marker
    const py = y + (1 - peak) * (h - 18) + 14;
    ctx.fillStyle = pal.fg3;
    ctx.fillRect(x + 2, py - 1, w - 4, 2);

    // label
    ctx.fillStyle = pal.fg;
    ctx.font = "bold 11px monospace";
    ctx.textAlign = "center";
    ctx.fillText(label, x + w/2, y + 12);
  }

  function draw(ctx, w, h, f, pal) {
    ctx.fillStyle = pal.bg;
    ctx.fillRect(0, 0, w, h);

    const L = WY.clamp((f.bass * 0.8 + f.mid * 0.45) * 1.1, 0, 1);
    const R = WY.clamp((f.mid  * 0.6 + f.treble * 0.9) * 1.1, 0, 1);
    env.L  = WY.ema(env.L, L, L > env.L ? 0.6 : 0.12);
    env.R  = WY.ema(env.R, R, R > env.R ? 0.6 : 0.12);
    env.peakL = Math.max(env.L, env.peakL * 0.985);
    env.peakR = Math.max(env.R, env.peakR * 0.985);

    const meterW = Math.min(120, w * 0.4);
    const gap    = 20;
    const totalW = meterW * 2 + gap;
    const startX = (w - totalW) / 2;
    drawMeter(ctx, startX,             10, meterW, h - 20, env.L, env.peakL, pal, "L");
    drawMeter(ctx, startX + meterW+gap,10, meterW, h - 20, env.R, env.peakR, pal, "R");

    // dB ticks down the centre
    ctx.fillStyle = pal.fg2;
    ctx.font = "9px monospace";
    ctx.textAlign = "center";
    const labels = ["+3","0","-3","-6","-12","-20"];
    const yTop = 24, yBot = h - 6;
    for (let i = 0; i < labels.length; i++) {
      const yy = yTop + (yBot - yTop) * (i / (labels.length - 1));
      ctx.fillText(labels[i], w/2, yy);
    }
  }

  return { name: "Stereo VU Meters", draw };
})();
