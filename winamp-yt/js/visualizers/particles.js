/* =========================================================
   visualizers/particles.js
   Audio-reactive particle field. Beats spawn bursts; bass
   pulls particles inward; treble scatters them.
   ========================================================= */
window.WY = window.WY || {}; WY.viz = WY.viz || {};

WY.viz.particles = (function () {
  "use strict";

  const N = 220;
  const P = [];
  function spawn(w, h) {
    P.length = 0;
    for (let i = 0; i < N; i++) {
      P.push({
        x: Math.random()*w, y: Math.random()*h,
        vx: (Math.random()-.5)*0.5,
        vy: (Math.random()-.5)*0.5,
        life: Math.random(),
        size: 1 + Math.random()*1.8
      });
    }
  }

  function draw(ctx, w, h, f, pal) {
    if (P.length === 0) spawn(w, h);

    // motion blur
    ctx.fillStyle = pal.bg;
    ctx.globalAlpha = 0.22;
    ctx.fillRect(0,0,w,h);
    ctx.globalAlpha = 1;

    const cx = w/2, cy = h/2;
    const pull = f.bass * 0.06;
    const push = f.treble * 0.05;
    const beatKick = f.beat ? 1.0 : 0;

    // beat -> burst
    if (beatKick) {
      const burst = Math.min(40, Math.floor(20 + f.energy * 30));
      for (let i = 0; i < burst; i++) {
        const a = Math.random() * Math.PI * 2;
        const sp = 2 + Math.random() * 4;
        P.push({
          x: cx, y: cy,
          vx: Math.cos(a)*sp, vy: Math.sin(a)*sp,
          life: 1, size: 1 + Math.random()*2.5
        });
      }
      // cap
      if (P.length > N + 200) P.splice(0, P.length - (N + 200));
    }

    for (const p of P) {
      // gravity-like attractor at centre, scaled by bass
      const dx = cx - p.x, dy = cy - p.y;
      const d  = Math.hypot(dx, dy) + 0.001;
      p.vx += (dx/d) * pull;
      p.vy += (dy/d) * pull;
      // treble jitter
      p.vx += (Math.random()-.5) * push;
      p.vy += (Math.random()-.5) * push;
      // damping
      p.vx *= 0.985;
      p.vy *= 0.985;
      p.x  += p.vx;
      p.y  += p.vy;
      p.life *= 0.998;

      // wrap
      if (p.x < 0)   p.x += w;
      if (p.x > w)   p.x -= w;
      if (p.y < 0)   p.y += h;
      if (p.y > h)   p.y -= h;

      const v = WY.clamp(p.life, 0.05, 1);
      ctx.fillStyle = v > 0.6 ? pal.fg3 : (v > 0.3 ? pal.fg : pal.fg2);
      ctx.globalAlpha = v;
      ctx.fillRect(p.x, p.y, p.size, p.size);
    }
    ctx.globalAlpha = 1;
  }

  return { name: "Particle Field", draw, _reset() { P.length = 0; } };
})();
