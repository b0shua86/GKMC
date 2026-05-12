/* =========================================================
   util.js  -  tiny helpers, no dependencies
   Exposed as window.WY (Winamp-YT) namespace.
   ========================================================= */
window.WY = window.WY || {};

(function (WY) {
  "use strict";

  WY.$  = (sel, ctx) => (ctx || document).querySelector(sel);
  WY.$$ = (sel, ctx) => Array.from((ctx || document).querySelectorAll(sel));

  WY.on = (el, ev, fn, opts) => el && el.addEventListener(ev, fn, opts);

  WY.clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v));
  WY.lerp  = (a, b, t)   => a + (b - a) * t;
  WY.rand  = (a, b)      => a + Math.random() * (b - a);
  WY.pick  = (arr)       => arr[Math.floor(Math.random() * arr.length)];

  // mm:ss
  WY.fmtTime = (sec) => {
    if (!isFinite(sec) || sec < 0) return "--:--";
    sec = Math.floor(sec);
    const m = Math.floor(sec / 60), s = sec % 60;
    return (m < 10 ? "0" : "") + m + ":" + (s < 10 ? "0" : "") + s;
  };

  // Read a CSS variable from <body>
  WY.cssVar = (name, fallback) => {
    const v = getComputedStyle(document.body).getPropertyValue(name).trim();
    return v || fallback;
  };

  // Tiny event bus — used by player/playlist/visualizer to talk.
  WY.bus = (() => {
    const map = new Map();
    return {
      on(name, fn)   { (map.get(name) || map.set(name, new Set()).get(name)).add(fn); },
      off(name, fn)  { map.get(name)?.delete(fn); },
      emit(name, ...a) {
        const s = map.get(name); if (!s) return;
        for (const fn of s) { try { fn(...a); } catch(e){ console.error(e); } }
      }
    };
  })();

  // Toast — small bottom-right notice
  WY.toast = (msg, ms = 1800) => {
    const el = document.getElementById("toast");
    if (!el) return;
    el.textContent = msg;
    el.classList.add("show");
    clearTimeout(el._t);
    el._t = setTimeout(() => el.classList.remove("show"), ms);
  };

  // Linear interpolation between arrays of same length
  WY.lerpArr = (out, a, b, t) => {
    for (let i = 0; i < out.length; i++) out[i] = a[i] + (b[i] - a[i]) * t;
    return out;
  };

  // Smooth EMA: state = state*(1-k) + v*k
  WY.ema = (state, v, k) => state * (1 - k) + v * k;

  // Hi-DPI canvas sizing
  WY.fitCanvas = (cvs) => {
    const dpr  = Math.max(1, Math.min(2, window.devicePixelRatio || 1));
    const rect = cvs.getBoundingClientRect();
    const w = Math.max(1, Math.floor(rect.width  * dpr));
    const h = Math.max(1, Math.floor(rect.height * dpr));
    if (cvs.width !== w || cvs.height !== h) {
      cvs.width  = w;
      cvs.height = h;
    }
    return { w, h, dpr };
  };

  // Convert CSS color (e.g. "#5bff5b") to {r,g,b}
  WY.hexToRgb = (hex) => {
    hex = (hex || "#ffffff").trim();
    if (hex[0] === "#") hex = hex.slice(1);
    if (hex.length === 3) hex = hex.split("").map(c => c+c).join("");
    const n = parseInt(hex, 16);
    return { r: (n>>16)&255, g: (n>>8)&255, b: n&255 };
  };

  WY.rgbStr = (c, a) => "rgba(" + c.r + "," + c.g + "," + c.b + "," + (a==null?1:a) + ")";

})(window.WY);
