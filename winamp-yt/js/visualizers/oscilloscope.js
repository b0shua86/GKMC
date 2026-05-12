/* =========================================================
   visualizers/oscilloscope.js
   Stereo-ish dual oscilloscope (mirrored top/bottom).
   ========================================================= */
window.WY = window.WY || {}; WY.viz = WY.viz || {};

WY.viz.oscilloscope = (function () {
  "use strict";

  function trace(ctx, wave, w, h, yMid, color, lw, phaseOff) {
    ctx.strokeStyle = color;
    ctx.lineWidth   = lw;
    ctx.beginPath();
    const step = w / wave.length;
    for (let i = 0; i < wave.length; i++) {
      const idx = (i + phaseOff) % wave.length;
      const x = i * step;
      const y = yMid + wave[idx] * (h * 0.30);
      if (i === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
    }
    ctx.stroke();
  }

  function draw(ctx, w, h, f, pal) {
    // fading trail
    ctx.fillStyle = pal.bg;
    ctx.globalAlpha = 0.25;
    ctx.fillRect(0, 0, w, h);
    ctx.globalAlpha = 1;

    // centre grid
    ctx.strokeStyle = pal.fg2;
    ctx.globalAlpha = 0.18;
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(0, h/2); ctx.lineTo(w, h/2);
    ctx.stroke();
    ctx.globalAlpha = 1;

    // left "channel"
    trace(ctx, f.wave, w, h, h*0.30, pal.fg,  2, 0);
    // right "channel" — same data, slight phase shift to fake stereo
    trace(ctx, f.wave, w, h, h*0.70, pal.fg3, 2, 12);

    // beat ring
    if (f.beatStrength > 0.05) {
      ctx.strokeStyle = pal.fg;
      ctx.globalAlpha = f.beatStrength;
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.arc(w/2, h/2, (h*0.35) * (1 - f.beatStrength), 0, Math.PI*2);
      ctx.stroke();
      ctx.globalAlpha = 1;
    }
  }

  return { name: "Oscilloscope", draw };
})();
