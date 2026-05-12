/* =========================================================
   visualizers/tunnel.js
   Frequency tunnel — concentric rings whose radii are driven
   by FFT bands. Beat-reactive zoom; treble adds sparkle.
   ========================================================= */
window.WY = window.WY || {}; WY.viz = WY.viz || {};

WY.viz.tunnel = (function () {
  "use strict";

  let zoom = 0, hue = 0;

  function draw(ctx, w, h, f, pal) {
    // Trail (don't fully clear — produces motion blur)
    ctx.fillStyle = pal.bg;
    ctx.globalAlpha = 0.18;
    ctx.fillRect(0, 0, w, h);
    ctx.globalAlpha = 1;

    const cx = w / 2, cy = h / 2;
    const maxR = Math.hypot(cx, cy);

    // beat -> zoom impulse
    if (f.beat) zoom += 0.4;
    zoom = WY.clamp(zoom * 0.94 + 0.02, 0, 1.5);

    // hue cycles with energy / beat
    hue = (hue + (1 + f.energy * 8)) % 360;

    const N = 18;
    for (let i = 0; i < N; i++) {
      const ti = i / N;
      const band = f.fft[Math.floor(ti * f.fft.length)];
      const r    = (ti * maxR) * (1 + zoom * 0.6) + band * 60;
      const lw   = 2 + band * 6;

      // ring color cycles round the wheel
      ctx.beginPath();
      ctx.strokeStyle = "hsl(" + ((hue + i * 18) % 360) + "," + (60 + band*40) + "%," + (50 + band*30) + "%)";
      ctx.globalAlpha = 0.55 + band * 0.45;
      ctx.lineWidth   = lw;
      ctx.arc(cx, cy, r, 0, Math.PI*2);
      ctx.stroke();
    }
    ctx.globalAlpha = 1;

    // treble sparkle
    const sparks = Math.floor(40 * f.treble);
    for (let i = 0; i < sparks; i++) {
      const a = Math.random() * Math.PI * 2;
      const r = Math.random() * maxR;
      ctx.fillStyle = pal.fg3;
      ctx.globalAlpha = Math.random();
      ctx.fillRect(cx + Math.cos(a)*r, cy + Math.sin(a)*r, 2, 2);
    }
    ctx.globalAlpha = 1;
  }

  return { name: "Frequency Tunnel", draw };
})();
