/* =========================================================
   skins.js  -  swap skin classes on <body>
   Each skin is a CSS file defining CSS custom properties.
   To add a new skin:
     1. create css/skins/<name>.css that sets the same vars as
        classic.css under  body.skin-<name> { ... }
     2. add  <link rel="stylesheet" href="css/skins/<name>.css">
        to index.html
     3. push a new <option value="<name>">My Skin</option> into
        #skin-select  (and into the SKINS array below)
   ========================================================= */
(function (WY) {
  "use strict";

  const SKINS = ["classic", "y2k", "vhs"];

  WY.skins = {
    SKINS,
    apply(name) {
      if (!SKINS.includes(name)) name = "classic";
      document.body.classList.remove(...SKINS.map(s => "skin-" + s));
      document.body.classList.add("skin-" + name);
      WY.store.set("skin", name);
      WY.bus.emit("skin", name);
    },
    init() {
      const saved = WY.store.get("skin", "classic");
      this.apply(saved);
      const sel = document.getElementById("skin-select");
      if (sel) {
        sel.value = saved;
        sel.addEventListener("change", e => this.apply(e.target.value));
      }
    }
  };

})(window.WY);
