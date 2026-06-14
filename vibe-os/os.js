/* =========================================================
   VIBE OS — Desktop shell / window manager
   ========================================================= */
(function (global) {
  "use strict";
  const VIBE = global.VIBE;
  const $ = s => document.querySelector(s);
  const el = (t, p, k) => { const n = document.createElement(t); if (p) for (const x in p) { if (x === "class") n.className = p[x]; else if (x === "html") n.innerHTML = p[x]; else if (x.startsWith("on")) n.addEventListener(x.slice(2), p[x]); else n.setAttribute(x, p[x]); } if (k != null) (Array.isArray(k) ? k : [k]).forEach(c => n.appendChild(typeof c === "string" ? document.createTextNode(c) : c)); return n; };

  let z = 10, openWins = [], focused = null;
  const winLayer = () => $("#windows");
  const taskBtns = () => $("#task-buttons");

  /* ---------------- Window creation ---------------- */
  function openApp(spec, opts) {
    opts = opts || {};
    const id = "win-" + (++z);
    const win = el("div", { class: "win" });
    win.style.zIndex = ++z;
    const cascade = (openWins.length % 8) * 26;
    const w = Math.min(spec.w || 420, global.innerWidth - 40);
    const h = Math.min(spec.h || 360, global.innerHeight - 80);
    win.style.width = w + "px"; win.style.height = h + "px";
    win.style.left = Math.max(8, Math.min((global.innerWidth - w) / 2 - 60 + cascade, global.innerWidth - w - 8)) + "px";
    win.style.top = Math.max(8, 40 + cascade) + "px";

    const titleBar = el("div", { class: "win-title" }, [
      el("span", { class: "win-icon" }, spec.icon || "📦"),
      el("span", { class: "win-name" }, spec.title),
      el("div", { class: "win-btns" }, [
        el("button", { class: "win-btn min", title: "Minimize", onclick: e => { e.stopPropagation(); minimize(rec); } }, "_"),
        el("button", { class: "win-btn max", title: "Maximize", onclick: e => { e.stopPropagation(); toggleMax(rec); } }, "□"),
        el("button", { class: "win-btn close", title: "Close", onclick: e => { e.stopPropagation(); closeWin(rec); } }, "✕")
      ])
    ]);
    const bodyWrap = el("div", { class: "win-body" });
    const resize = el("div", { class: "resize-h" });
    win.append(titleBar, bodyWrap, resize);
    winLayer().appendChild(win);

    const rec = { id, win, bodyWrap, titleBar, spec, taskBtn: null, max: false, prev: null, minimized: false };
    openWins.push(rec);

    // build app
    if (spec.generated) {
      // little "building…" flourish for generated apps
      bodyWrap.appendChild(el("div", { class: "app-pad", style: "text-align:center;color:#666" }, [
        el("div", { style: "font-size:34px;margin-top:30px" }, "🛠️"),
        el("div", { style: "margin-top:10px" }, "Building " + spec.title + "…"),
        el("div", { class: "boot-bar", style: "margin:14px auto" }, el("div", { class: "boot-bar-fill" }))
      ]));
      setTimeout(() => { bodyWrap.innerHTML = ""; safeBuild(spec, bodyWrap); }, 650);
    } else {
      safeBuild(spec, bodyWrap);
    }

    addTaskButton(rec);
    makeDraggable(rec);
    makeResizable(rec, resize);
    win.addEventListener("mousedown", () => focus(rec));
    titleBar.addEventListener("dblclick", () => toggleMax(rec));
    focus(rec);
    hideStart();
    pushRecent(spec);
    return rec;
  }

  function safeBuild(spec, bodyWrap) {
    try {
      bodyWrap._cleanup = null;
      spec.build(bodyWrap, { query: spec._query, title: spec.title });
    } catch (err) {
      bodyWrap.innerHTML = "";
      bodyWrap.appendChild(el("div", { class: "app-pad" }, [
        el("h3", {}, "⚠️ This app hit a snag"),
        el("p", { class: "muted" }, String(err && err.message || err)),
        el("p", {}, "VIBE OS apps are experimental builds — try another search.")
      ]));
    }
  }

  /* ---------------- Window ops ---------------- */
  function focus(rec) {
    if (focused === rec && !rec.minimized) return;
    openWins.forEach(r => { r.win.classList.add("inactive"); if (r.taskBtn) r.taskBtn.classList.remove("active"); });
    rec.win.classList.remove("inactive");
    rec.win.style.zIndex = ++z;
    if (rec.taskBtn) rec.taskBtn.classList.add("active");
    if (rec.minimized) { rec.win.classList.remove("min"); rec.minimized = false; }
    focused = rec;
  }
  function minimize(rec) {
    rec.win.classList.add("min"); rec.minimized = true;
    if (rec.taskBtn) rec.taskBtn.classList.remove("active");
    focused = null;
  }
  function toggleMax(rec) {
    if (rec.max) {
      Object.assign(rec.win.style, rec.prev); rec.win.classList.remove("max"); rec.max = false;
    } else {
      rec.prev = { left: rec.win.style.left, top: rec.win.style.top, width: rec.win.style.width, height: rec.win.style.height };
      Object.assign(rec.win.style, { left: "0px", top: "0px", width: "100vw", height: "calc(100vh - 30px)" });
      rec.win.classList.add("max"); rec.max = true;
    }
    focus(rec);
  }
  function closeWin(rec) {
    try { if (rec.bodyWrap._cleanup) rec.bodyWrap._cleanup(); } catch (e) {}
    rec.win.remove(); if (rec.taskBtn) rec.taskBtn.remove();
    openWins = openWins.filter(r => r !== rec);
    if (focused === rec) { focused = null; const last = openWins[openWins.length - 1]; if (last) focus(last); }
  }

  /* ---------------- Taskbar buttons ---------------- */
  function addTaskButton(rec) {
    const btn = el("div", { class: "task-btn", title: rec.spec.title }, [
      el("span", {}, rec.spec.icon || "📦"),
      el("span", { class: "tb-label" }, rec.spec.title)
    ]);
    btn.addEventListener("click", () => {
      if (rec.minimized) { focus(rec); }
      else if (focused === rec) { minimize(rec); }
      else { focus(rec); }
    });
    rec.taskBtn = btn;
    taskBtns().appendChild(btn);
  }

  /* ---------------- Drag & resize ---------------- */
  function makeDraggable(rec) {
    let sx, sy, ox, oy, drag = false;
    rec.titleBar.addEventListener("mousedown", e => {
      if (e.target.closest(".win-btn") || rec.max) return;
      drag = true; sx = e.clientX; sy = e.clientY;
      ox = parseInt(rec.win.style.left); oy = parseInt(rec.win.style.top);
      document.body.style.userSelect = "none";
    });
    global.addEventListener("mousemove", e => {
      if (!drag) return;
      let nx = ox + e.clientX - sx, ny = oy + e.clientY - sy;
      ny = Math.max(0, Math.min(ny, global.innerHeight - 60));
      nx = Math.max(-rec.win.offsetWidth + 80, Math.min(nx, global.innerWidth - 40));
      rec.win.style.left = nx + "px"; rec.win.style.top = ny + "px";
    });
    global.addEventListener("mouseup", () => { drag = false; document.body.style.userSelect = ""; });
  }
  function makeResizable(rec, handle) {
    let sx, sy, ow, oh, rz = false;
    handle.addEventListener("mousedown", e => { e.stopPropagation(); rz = true; sx = e.clientX; sy = e.clientY; ow = rec.win.offsetWidth; oh = rec.win.offsetHeight; document.body.style.userSelect = "none"; });
    global.addEventListener("mousemove", e => { if (!rz) return; rec.win.style.width = Math.max(200, ow + e.clientX - sx) + "px"; rec.win.style.height = Math.max(140, oh + e.clientY - sy) + "px"; });
    global.addEventListener("mouseup", () => { rz = false; document.body.style.userSelect = ""; });
  }

  /* ---------------- Launch from search ---------------- */
  function launch(query) {
    const spec = VIBE.resolve(query);
    if (!spec) return;
    // clone so per-window query/context doesn't mutate the registry entry
    const inst = Object.assign({}, spec, { _query: query });
    return openApp(inst);
  }

  /* ---------------- Recent apps ---------------- */
  function pushRecent(spec) {
    let r = (VIBE._recent || []);
    r = [{ id: spec.id, title: spec.title, icon: spec.icon, generated: !!spec.generated, query: spec._query || spec.title }, ...r.filter(x => x.id !== spec.id)].slice(0, 6);
    VIBE._recent = r;
    try { localStorage.setItem("vibe:recent", JSON.stringify(r)); } catch (e) {}
    renderRecent();
  }
  function renderRecent() {
    const box = $("#sm-recent"); if (!box) return;
    box.innerHTML = "";
    box.appendChild(el("div", { class: "sm-section-label" }, "Recently built"));
    const r = VIBE._recent || [];
    if (!r.length) { box.appendChild(el("div", { class: "sm-item muted" }, [el("span", {}, "Nothing yet — build something!")])); return; }
    r.forEach(x => box.appendChild(smItem(x.icon, x.title, x.generated ? "generated app" : "app", () => launch(x.query))));
  }

  /* ---------------- Start menu ---------------- */
  function smItem(icon, title, sub, fn) {
    return el("div", { class: "sm-item", onclick: fn }, [
      el("span", { class: "sm-ico" }, icon),
      el("div", { class: "sm-txt" }, [el("b", {}, title), sub ? el("small", {}, sub) : el("small", {}, "")])
    ]);
  }
  function buildStartMenu() {
    const pinned = $("#sm-pinned"); pinned.innerHTML = "";
    pinned.appendChild(el("div", { class: "sm-section-label" }, "Apps"));
    VIBE.pinned().forEach(a => pinned.appendChild(smItem(a.icon, a.title, a.id, () => launch(a.id))));
    pinned.appendChild(el("div", { class: "sm-sep" }));
    pinned.appendChild(smItem("💠", "About VIBE OS", "welcome", () => launch("about")));
    renderRecent();
  }
  function toggleStart() {
    const sm = $("#start-menu");
    if (sm.classList.contains("hidden")) { sm.classList.remove("hidden"); $("#start-btn").classList.add("open"); setTimeout(() => $("#sm-input").focus(), 30); }
    else hideStart();
  }
  function hideStart() { $("#start-menu").classList.add("hidden"); $("#start-btn").classList.remove("open"); }

  /* ---------------- Desktop icons ---------------- */
  function buildDesktopIcons() {
    const box = $("#desktop-icons"); box.innerHTML = "";
    const icons = [
      VIBE.byId("about"), VIBE.byId("calc"), VIBE.byId("notepad"), VIBE.byId("paint"),
      VIBE.byId("browser"), VIBE.byId("snake"), VIBE.byId("mines"), VIBE.byId("piano"),
      VIBE.byId("terminal"), VIBE.byId("todo")
    ].filter(Boolean);
    icons.forEach(a => {
      const ic = el("div", { class: "dicon", tabindex: "0" }, [
        el("div", { class: "glyph", style: "background:linear-gradient(135deg,#eaf1ff,#cfe0ff)" }, a.icon),
        el("div", { class: "label" }, a.title)
      ]);
      ic.addEventListener("dblclick", () => launch(a.id));
      ic.addEventListener("click", () => { box.querySelectorAll(".dicon").forEach(d => d.classList.remove("sel")); ic.classList.add("sel"); });
      box.appendChild(ic);
    });
    // also a "Build New App…" desktop icon
    const newIc = el("div", { class: "dicon", tabindex: "0" }, [el("div", { class: "glyph", style: "background:linear-gradient(135deg,#fff0d0,#ffd98a)" }, "✨"), el("div", { class: "label" }, "Build New App")]);
    newIc.addEventListener("dblclick", () => { $("#hero-input").focus(); });
    box.appendChild(newIc);
  }

  /* ---------------- Hero search chips ---------------- */
  function buildChips() {
    const box = $("#hero-chips"); box.innerHTML = "";
    ["calculator", "paint", "snake", "minesweeper", "piano", "budget tracker", "space shooter", "habit streaks", "chat bot", "weather"].forEach(c =>
      box.appendChild(el("span", { class: "chip", onclick: () => { $("#hero-input").value = c; launch(c); } }, c)));
  }

  /* ---------------- Clock ---------------- */
  function tickClock() {
    const d = new Date();
    $("#clock").textContent = d.toLocaleTimeString([], { hour: "numeric", minute: "2-digit" });
  }

  /* ---------------- Context menu ---------------- */
  function showCtx(x, y, items) {
    const ctx = $("#ctx"); ctx.innerHTML = "";
    items.forEach(it => {
      if (it === "-") ctx.appendChild(el("div", { class: "ctx-sep" }));
      else ctx.appendChild(el("div", { class: "ctx-item", onclick: () => { hideCtx(); it.fn(); } }, it.label));
    });
    ctx.classList.remove("hidden");
    ctx.style.left = Math.min(x, global.innerWidth - 180) + "px";
    ctx.style.top = Math.min(y, global.innerHeight - 60 - ctx.offsetHeight) + "px";
  }
  function hideCtx() { $("#ctx").classList.add("hidden"); }

  /* =========================================================
     BOOT
     ========================================================= */
  function boot() {
    // restore recent
    try { VIBE._recent = JSON.parse(localStorage.getItem("vibe:recent")) || []; } catch (e) { VIBE._recent = []; }

    buildDesktopIcons();
    buildChips();
    buildStartMenu();
    tickClock(); setInterval(tickClock, 1000);

    // tray icons
    const tray = $("#tray-icons");
    ["🔊", "🛡️", "📶"].forEach(i => tray.appendChild(el("span", { class: "tray-ico", title: "VIBE OS" }, i)));

    // hero search
    $("#hero-form").addEventListener("submit", e => { e.preventDefault(); const v = $("#hero-input").value.trim(); if (v) { launch(v); $("#hero-input").value = ""; } });
    // start menu search
    $("#sm-form").addEventListener("submit", e => { e.preventDefault(); const v = $("#sm-input").value.trim(); if (v) { launch(v); $("#sm-input").value = ""; hideStart(); } });

    // start button
    $("#start-btn").addEventListener("click", e => { e.stopPropagation(); toggleStart(); });
    $("#start-menu").addEventListener("click", e => e.stopPropagation());
    $("#sm-logoff").addEventListener("click", () => location.reload());
    $("#sm-shutdown").addEventListener("click", shutdown);

    // global click closes menus
    document.addEventListener("click", () => { hideStart(); hideCtx(); });
    document.addEventListener("mousedown", e => { if (!e.target.closest("#ctx")) hideCtx(); });

    // desktop right-click menu
    $("#desktop").addEventListener("contextmenu", e => {
      if (e.target.closest(".win")) return; // let apps handle their own
      e.preventDefault();
      showCtx(e.clientX, e.clientY, [
        { label: "✨ Build new app…", fn: () => $("#hero-input").focus() },
        { label: "🧮 Calculator", fn: () => launch("calc") },
        { label: "🎨 Paint", fn: () => launch("paint") },
        "-",
        { label: "🔄 Refresh", fn: () => {} },
        { label: "💠 About VIBE OS", fn: () => launch("about") }
      ]);
    });

    // keyboard: Esc closes start; Ctrl+Space focuses search
    document.addEventListener("keydown", e => {
      if (e.key === "Escape") { hideStart(); hideCtx(); }
      if (e.ctrlKey && e.code === "Space") { e.preventDefault(); $("#hero-input").focus(); }
    });

    // window resize keeps maximized windows correct (CSS handles it)
    global.addEventListener("resize", () => {});
  }

  function shutdown() {
    hideStart();
    const ov = el("div", { class: "boot" });
    ov.style.background = "#0a246a";
    ov.appendChild(el("div", { class: "boot-logo" }, [el("span", { class: "boot-vibe" }, "VIBE"), el("span", { class: "boot-os" }, "OS")]));
    ov.appendChild(el("div", { class: "boot-tag" }, "It is now safe to turn off your vibe."));
    ov.appendChild(el("button", { class: "btn", style: "margin-top:10px", onclick: () => location.reload() }, "Boot again"));
    document.body.appendChild(ov);
  }

  /* ---------------- Boot sequence ---------------- */
  function start() {
    const bootEl = $("#boot"), desk = $("#desktop");
    const finish = () => {
      if (desk._started) return; desk._started = true;
      bootEl.style.transition = "opacity .4s"; bootEl.style.opacity = "0";
      setTimeout(() => { bootEl.classList.add("hidden"); desk.classList.remove("hidden"); boot(); }, 400);
    };
    const timer = setTimeout(finish, 2600);
    const skip = () => { clearTimeout(timer); finish(); document.removeEventListener("keydown", skip); bootEl.removeEventListener("click", skip); };
    document.addEventListener("keydown", skip);
    bootEl.addEventListener("click", skip);
  }

  // expose
  VIBE.launch = launch;
  VIBE.openApp = openApp;

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", start);
  else start();

})(window);
