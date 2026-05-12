/* =========================================================
   storage.js  -  thin localStorage wrapper with namespacing
   Preserves: current playlist, skin, EQ, volume, presets.
   ========================================================= */
(function (WY) {
  "use strict";
  const NS = "winamp-yt:";

  WY.store = {
    get(key, def) {
      try {
        const raw = localStorage.getItem(NS + key);
        if (raw == null) return def;
        return JSON.parse(raw);
      } catch (e) { return def; }
    },
    set(key, val) {
      try { localStorage.setItem(NS + key, JSON.stringify(val)); }
      catch (e) { /* quota or disabled — silently ignore */ }
    },
    del(key) { try { localStorage.removeItem(NS + key); } catch(e){} }
  };

})(window.WY);
