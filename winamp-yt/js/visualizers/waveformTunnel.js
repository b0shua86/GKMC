/* =========================================================
   visualizers/waveformTunnel.js
   The current waveform wrapped into a rotating ring, with a
   second copy rotating the other way for a tunnel feel.
   ========================================================= */
window.WY = window.WY || {}; WY.viz = WY.viz || {};

WY.viz.waveformTunnel = (function () {
  "use strict";

  let rot = 0;

  function ring(ctx, cx, cy, baseR, wave, color, lw, scale, phase, rotate) {
    ctx.strokeStyle = color;
    ctx.lineWidth   = lw;
    ctx.beginPath();
    for (let i = 0; i <= wave.length; i++) {
      const a   = (i / wave.length) * Math.PI * 2 + rotate;
      const v   = wave[i % wave.length];
      const r   = baseR + v * scale;
      const x   = cx + Math.cos(a) * r;
      const y   = cy + Math.sin(a) * r;
      if (i === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
    }
    ctx.stroke();
  }

  function draw(ctx, w, h, f, pal) {
    // fade
    ctx.fillStyle = pal.bg;
    ctx.globalAlpha = 0.2;
    ctx.fillRect(0, 0, w, h);
    ctx.globalAlpha = 1;

    rot += 0.005 + f.energy * 0.04 + (f.beat ? 0.15 : 0);
    const cx = w/2, cy = h/2;
    const baseR = Math.min(w, h) * 0.18;
    const scale = 30 + 80 * f.bass;

    // outer (cw)
    ring(ctx, cx, cy, baseR * 1.2, f.wave, pal.fg,  2,  scale,  0,  rot);
    // inner (ccw)
    ring(ctx, cx, cy, baseR * 0.7, f.wave, pal.fg3, 1.5, scale*0.5, 0, -rot * 1.3);

    // glow dot in centre on beat
    if (f.beatStrength > 0.1) {
      ctx.fillStyle = pal.fg2;
      ctx.globalAlpha = f.beatStrength;
      ctx.beginPath();
      ctx.arc(cx, cy, baseR * 0.4 * f.beatStrength, 0, Math.PI*2);
      ctx.fill();
      ctx.globalAlpha = 1;
    }
  }

  return { name: "Rotating Waveform Tunnel", draw };
})();
