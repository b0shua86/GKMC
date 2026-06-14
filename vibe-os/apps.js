/* =========================================================
   VIBE OS — App engine
   Resolves a search query into a runnable app and mounts it.
   Ships real, working apps + a generative builder for anything else.
   ========================================================= */
(function (global) {
  "use strict";

  /* ---------- tiny DOM helper ---------- */
  function el(tag, props, kids) {
    const n = document.createElement(tag);
    if (props) for (const k in props) {
      if (k === "style") Object.assign(n.style, props[k]);
      else if (k === "class") n.className = props[k];
      else if (k === "html") n.innerHTML = props[k];
      else if (k.startsWith("on") && typeof props[k] === "function") n.addEventListener(k.slice(2), props[k]);
      else if (props[k] != null) n.setAttribute(k, props[k]);
    }
    if (kids != null) (Array.isArray(kids) ? kids : [kids]).forEach(c =>
      n.appendChild(typeof c === "string" ? document.createTextNode(c) : c));
    return n;
  }
  const store = {
    get: (k, d) => { try { const v = localStorage.getItem("vibe:" + k); return v == null ? d : JSON.parse(v); } catch (e) { return d; } },
    set: (k, v) => { try { localStorage.setItem("vibe:" + k, JSON.stringify(v)); } catch (e) {} }
  };

  /* shared audio context for sound-making apps */
  let _actx = null;
  function audio() { if (!_actx) { try { _actx = new (global.AudioContext || global.webkitAudioContext)(); } catch (e) {} } if (_actx && _actx.state === "suspended") _actx.resume(); return _actx; }
  function beep(freq, dur, type, gain) {
    const a = audio(); if (!a) return;
    const o = a.createOscillator(), g = a.createGain();
    o.type = type || "sine"; o.frequency.value = freq;
    g.gain.value = gain == null ? 0.18 : gain;
    o.connect(g); g.connect(a.destination);
    const t = a.currentTime; o.start(t);
    g.gain.setValueAtTime(g.gain.value, t);
    g.gain.exponentialRampToValueAtTime(0.0001, t + (dur || 0.2));
    o.stop(t + (dur || 0.2));
  }

  /* =========================================================
     BUILT-IN APPS
     each: { id, title, icon, keywords:[], w, h, pinned?, build(body) }
     ========================================================= */
  const APPS = [];
  const reg = a => { APPS.push(a); return a; };

  /* ---------- Calculator ---------- */
  reg({
    id: "calc", title: "Calculator", icon: "🧮", w: 240, h: 320, pinned: true,
    keywords: ["calculator", "calc", "math", "arithmetic", "adding machine", "compute"],
    build(body) {
      let expr = "";
      const disp = el("div", { style: { background: "#cdeacd", border: "2px inset #9bbf9b", margin: "8px", padding: "10px 12px", textAlign: "right", fontFamily: "Consolas,monospace", fontSize: "22px", minHeight: "30px", overflow: "hidden" } }, "0");
      const grid = el("div", { style: { display: "grid", gridTemplateColumns: "repeat(4,1fr)", gap: "6px", padding: "0 8px 8px" } });
      const keys = ["C", "←", "%", "/", "7", "8", "9", "*", "4", "5", "6", "-", "1", "2", "3", "+", "0", ".", "="];
      const press = k => {
        if (k === "C") expr = "";
        else if (k === "←") expr = expr.slice(0, -1);
        else if (k === "=") {
          try { expr = String(roundish(Function('"use strict";return (' + sanitize(expr) + ')')())); }
          catch (e) { expr = "Error"; }
        } else expr += k;
        disp.textContent = expr || "0";
        beep(k === "=" ? 660 : 440, 0.05, "square", 0.06);
      };
      function sanitize(s){ return s.replace(/[^-+*/%.\d() ]/g, "").replace(/%/g, "/100"); }
      function roundish(n){ return Math.round(n * 1e10) / 1e10; }
      keys.forEach(k => {
        const span = (k === "0") ? 2 : 1;
        const b = el("button", { class: "btn", style: { gridColumn: "span " + span, padding: "12px 0", fontSize: "16px" }, onclick: () => press(k) }, k);
        if (k === "=") { b.classList.add("primary"); }
        if ("+-*/".includes(k)) b.classList.add("blue");
        grid.appendChild(b);
      });
      body.appendChild(disp); body.appendChild(grid);
      body.tabIndex = 0;
      body.addEventListener("keydown", e => {
        const m = { Enter: "=", "=": "=", Backspace: "←", Escape: "C", c: "C" };
        if (/[0-9.+\-*/%]/.test(e.key)) press(e.key);
        else if (m[e.key]) { e.preventDefault(); press(m[e.key]); }
      });
    }
  });

  /* ---------- Notepad ---------- */
  reg({
    id: "notepad", title: "Notepad", icon: "📝", w: 480, h: 360, pinned: true,
    keywords: ["notepad", "notes", "text", "editor", "write", "writing", "document", "memo", "diary", "journal"],
    build(body, ctx) {
      const key = "notepad:" + (ctx.query || "default");
      const ta = el("textarea", { class: "input", style: { flex: "1", border: "none", borderRadius: "0", fontFamily: "Consolas,Lucida Console,monospace", fontSize: "13px", lineHeight: "1.5", padding: "8px" }, spellcheck: "false" });
      ta.value = store.get(key, "");
      const status = el("div", { class: "statusbar" }, [el("span", {}, "Ln 1, Col 1"), el("span", { class: "muted" }, "saved")]);
      const save = () => { store.set(key, ta.value); status.lastChild.textContent = "saved ✓"; };
      ta.addEventListener("input", () => { status.lastChild.textContent = "editing…"; clearTimeout(ta._t); ta._t = setTimeout(save, 500); count(); });
      ta.addEventListener("keyup", count); ta.addEventListener("click", count);
      function count() {
        const upto = ta.value.slice(0, ta.selectionStart);
        const ln = upto.split("\n").length, col = upto.length - upto.lastIndexOf("\n");
        status.firstChild.textContent = "Ln " + ln + ", Col " + col + "  ·  " + ta.value.length + " chars";
      }
      const tb = el("div", { class: "toolbar" }, [
        el("button", { class: "btn", onclick: () => { ta.value = ""; save(); ta.focus(); } }, "New"),
        el("button", { class: "btn", onclick: save }, "Save"),
        el("button", { class: "btn", onclick: () => {
          const blob = new Blob([ta.value], { type: "text/plain" });
          const a = el("a", { href: URL.createObjectURL(blob), download: (ctx.query || "note") + ".txt" }); a.click();
        } }, "Download"),
        el("span", { style: { flex: "1" } }),
        el("label", { class: "muted", style: { display: "flex", gap: "4px", alignItems: "center" } }, [
          (() => { const c = el("input", { type: "checkbox" }); c.checked = true; c.onchange = () => ta.style.whiteSpace = c.checked ? "pre-wrap" : "pre"; return c; })(), "Wrap"])
      ]);
      body.style.display = "flex"; body.style.flexDirection = "column";
      body.appendChild(tb); body.appendChild(ta); body.appendChild(status);
      count();
    }
  });

  /* ---------- Paint ---------- */
  reg({
    id: "paint", title: "Paint", icon: "🎨", w: 560, h: 440, pinned: true,
    keywords: ["paint", "draw", "drawing", "sketch", "canvas", "art", "doodle", "image editor"],
    build(body) {
      let color = "#000000", size = 4, drawing = false, last = null, erase = false;
      const cvs = el("canvas", { width: 800, height: 600, style: { background: "#fff", cursor: "crosshair", flex: "1", width: "100%", height: "100%", imageRendering: "pixelated" } });
      const cx = cvs.getContext("2d");
      cx.lineCap = cx.lineJoin = "round";
      function pos(e) { const r = cvs.getBoundingClientRect(); return { x: (e.clientX - r.left) * cvs.width / r.width, y: (e.clientY - r.top) * cvs.height / r.height }; }
      function down(e) { drawing = true; last = pos(e); dot(last); }
      function move(e) { if (!drawing) return; const p = pos(e); cx.strokeStyle = erase ? "#fff" : color; cx.lineWidth = size * (erase ? 3 : 1); cx.beginPath(); cx.moveTo(last.x, last.y); cx.lineTo(p.x, p.y); cx.stroke(); last = p; }
      function dot(p) { cx.fillStyle = erase ? "#fff" : color; cx.beginPath(); cx.arc(p.x, p.y, (size * (erase ? 3 : 1)) / 2, 0, 7); cx.fill(); }
      const up = () => drawing = false;
      cvs.addEventListener("mousedown", down); cvs.addEventListener("mousemove", move);
      global.addEventListener("mouseup", up);
      const palette = ["#000000", "#7f7f7f", "#880015", "#ed1c24", "#ff7f27", "#fff200", "#22b14c", "#00a2e8", "#3f48cc", "#a349a4", "#ffffff", "#c3c3c3", "#b97a57", "#ffaec9", "#ffc90e", "#efe4b0", "#b5e61d", "#99d9ea", "#7092be", "#c8bfe7"];
      const swatches = el("div", { style: { display: "grid", gridTemplateColumns: "repeat(10,16px)", gap: "2px" } },
        palette.map(c => el("div", { style: { width: "16px", height: "16px", background: c, border: "1px solid #888", cursor: "pointer" }, onclick: () => { color = c; erase = false; current.style.background = c; } })));
      const current = el("div", { style: { width: "26px", height: "26px", background: color, border: "2px inset #999" } });
      const tb = el("div", { class: "toolbar" }, [
        current,
        el("input", { type: "color", value: "#000000", onchange: e => { color = e.target.value; erase = false; current.style.background = color; } }),
        el("label", { class: "muted" }, "Size"),
        (() => { const r = el("input", { type: "range", min: "1", max: "30", value: "4", oninput: e => size = +e.target.value }); return r; })(),
        el("button", { class: "btn", onclick: () => erase = !erase }, "Eraser"),
        el("button", { class: "btn", onclick: () => { cx.fillStyle = "#fff"; cx.fillRect(0, 0, cvs.width, cvs.height); } }, "Clear"),
        el("button", { class: "btn", onclick: () => { const a = el("a", { href: cvs.toDataURL(), download: "vibe-art.png" }); a.click(); } }, "Save PNG"),
        el("span", { style: { flex: "1" } }), swatches
      ]);
      cx.fillStyle = "#fff"; cx.fillRect(0, 0, cvs.width, cvs.height);
      body.style.display = "flex"; body.style.flexDirection = "column";
      body.appendChild(tb); body.appendChild(el("div", { style: { flex: "1", display: "flex", padding: "6px", background: "#808080" } }, cvs));
      body._cleanup = () => global.removeEventListener("mouseup", up);
    }
  });

  /* ---------- Snake ---------- */
  reg({
    id: "snake", title: "Snake", icon: "🐍", w: 360, h: 420, pinned: true,
    keywords: ["snake", "snake game"],
    build(body) {
      const N = 18, S = 18;
      const cvs = el("canvas", { width: N * S, height: N * S, style: { background: "#9ec96e", display: "block", margin: "0 auto" } });
      const cx = cvs.getContext("2d");
      let snake, dir, food, timer, score, best = store.get("snake:best", 0), over = false;
      const scoreEl = el("span", {}, "Score 0"), bestEl = el("span", {}, "Best " + best);
      function rndCell() { return { x: (Math.random() * N) | 0, y: (Math.random() * N) | 0 }; }
      function reset() { snake = [{ x: 9, y: 9 }]; dir = { x: 1, y: 0 }; food = rndCell(); score = 0; over = false; scoreEl.textContent = "Score 0"; loop(); }
      function loop() { clearInterval(timer); timer = setInterval(step, 130); }
      function step() {
        const h = { x: (snake[0].x + dir.x + N) % N, y: (snake[0].y + dir.y + N) % N };
        if (snake.some(s => s.x === h.x && s.y === h.y)) return gameOver();
        snake.unshift(h);
        if (h.x === food.x && h.y === food.y) { score++; scoreEl.textContent = "Score " + score; food = rndCell(); beep(720, 0.06, "square", 0.08); if (score > best) { best = score; bestEl.textContent = "Best " + best; store.set("snake:best", best); } }
        else snake.pop();
        draw();
      }
      function draw() {
        cx.fillStyle = "#9ec96e"; cx.fillRect(0, 0, cvs.width, cvs.height);
        cx.fillStyle = "#c0392b"; cx.fillRect(food.x * S + 2, food.y * S + 2, S - 4, S - 4);
        snake.forEach((s, i) => { cx.fillStyle = i === 0 ? "#1e5e1e" : "#2d8a2d"; cx.fillRect(s.x * S + 1, s.y * S + 1, S - 2, S - 2); });
      }
      function gameOver() { over = true; clearInterval(timer); beep(160, 0.3, "sawtooth", 0.1); cx.fillStyle = "rgba(0,0,0,.55)"; cx.fillRect(0, 0, cvs.width, cvs.height); cx.fillStyle = "#fff"; cx.font = "bold 22px Tahoma"; cx.textAlign = "center"; cx.fillText("Game Over", cvs.width / 2, cvs.height / 2 - 6); cx.font = "12px Tahoma"; cx.fillText("press New Game", cvs.width / 2, cvs.height / 2 + 16); }
      const onkey = e => {
        const k = e.key, d = dir;
        if ((k === "ArrowUp" || k === "w") && d.y === 0) dir = { x: 0, y: -1 };
        else if ((k === "ArrowDown" || k === "s") && d.y === 0) dir = { x: 0, y: 1 };
        else if ((k === "ArrowLeft" || k === "a") && d.x === 0) dir = { x: -1, y: 0 };
        else if ((k === "ArrowRight" || k === "d") && d.x === 0) dir = { x: 1, y: 0 };
        if (k.startsWith("Arrow")) e.preventDefault();
      };
      body.tabIndex = 0; body.addEventListener("keydown", onkey);
      body.appendChild(el("div", { class: "toolbar" }, [el("button", { class: "btn primary", onclick: () => { reset(); body.focus(); } }, "New Game"), el("span", { style: { flex: "1" } }), scoreEl, el("span", { style: { width: "12px" } }), bestEl]));
      body.appendChild(el("div", { style: { padding: "8px", display: "flex", justifyContent: "center", background: "#dfe9c8" } }, cvs));
      body.appendChild(el("div", { class: "statusbar" }, "Arrow keys / WASD to steer · walls wrap around"));
      reset(); setTimeout(() => body.focus(), 50);
      body._cleanup = () => clearInterval(timer);
    }
  });

  /* ---------- Minesweeper ---------- */
  reg({
    id: "mines", title: "Minesweeper", icon: "💣", w: 330, h: 400, pinned: true,
    keywords: ["minesweeper", "mines", "mine sweeper", "bomb game"],
    build(body) {
      const N = 9, M = 10; let grid, revealed, flagged, dead, won, left, timer, t = 0;
      const face = el("button", { class: "btn", style: { fontSize: "18px", padding: "2px 10px" }, onclick: init }, "🙂");
      const mineCt = el("span", { class: "pill" }, "💣 10"), timeEl = el("span", { class: "pill" }, "⏱ 0");
      const boardEl = el("div", { style: { display: "grid", gridTemplateColumns: "repeat(" + N + ",26px)", gap: "0", justifyContent: "center", padding: "10px", background: "#bdbdbd" } });
      function init() {
        clearInterval(timer); t = 0; timeEl.textContent = "⏱ 0"; dead = won = false; left = N * N - M; face.textContent = "🙂"; mineCt.textContent = "💣 " + M;
        grid = []; revealed = []; flagged = [];
        for (let y = 0; y < N; y++) { grid[y] = []; revealed[y] = []; flagged[y] = []; for (let x = 0; x < N; x++) { grid[y][x] = 0; revealed[y][x] = false; flagged[y][x] = false; } }
        let placed = 0; while (placed < M) { const x = (Math.random() * N) | 0, y = (Math.random() * N) | 0; if (grid[y][x] !== "M") { grid[y][x] = "M"; placed++; } }
        for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) { if (grid[y][x] === "M") continue; let c = 0; neigh(x, y, (nx, ny) => grid[ny][nx] === "M" && c++); grid[y][x] = c; }
        draw();
      }
      function neigh(x, y, fn) { for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) { const nx = x + dx, ny = y + dy; if ((dx || dy) && nx >= 0 && ny >= 0 && nx < N && ny < N) fn(nx, ny); } }
      function reveal(x, y) {
        if (dead || won || revealed[y][x] || flagged[y][x]) return;
        if (!timer) timer = setInterval(() => { timeEl.textContent = "⏱ " + (++t); }, 1000);
        revealed[y][x] = true;
        if (grid[y][x] === "M") { dead = true; face.textContent = "😵"; clearInterval(timer); beep(120, 0.4, "sawtooth", 0.12); }
        else { left--; beep(520, 0.03, "square", 0.04); if (grid[y][x] === 0) neigh(x, y, reveal); }
        if (left === 0 && !dead) { won = true; face.textContent = "😎"; clearInterval(timer); }
        draw();
      }
      function flag(x, y, e) { e.preventDefault(); if (dead || won || revealed[y][x]) return; flagged[y][x] = !flagged[y][x]; let f = 0; for (let yy = 0; yy < N; yy++) for (let xx = 0; xx < N; xx++) if (flagged[yy][xx]) f++; mineCt.textContent = "💣 " + (M - f); draw(); }
      const COL = ["", "#1976d2", "#388e3c", "#d32f2f", "#7b1fa2", "#c62828", "#0097a7", "#000", "#555"];
      function draw() {
        boardEl.innerHTML = "";
        for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) {
          const v = grid[y][x];
          const r = revealed[y][x] || ((dead || won) && v === "M");
          const cell = el("div", {
            style: {
              width: "26px", height: "26px", display: "grid", placeItems: "center",
              fontWeight: "bold", fontSize: "14px", cursor: "pointer", userSelect: "none",
              border: r ? "1px solid #999" : "3px outset #e0e0e0",
              background: r ? (v === "M" ? "#e57373" : "#d6d6d6") : "#bdbdbd",
              color: COL[v] || "#000"
            },
            oncontextmenu: e => flag(x, y, e),
            onclick: () => reveal(x, y)
          }, r ? (v === "M" ? "💣" : (v ? String(v) : "")) : (flagged[y][x] ? "🚩" : ""));
          boardEl.appendChild(cell);
        }
      }
      body.appendChild(el("div", { class: "toolbar", style: { justifyContent: "center", gap: "16px" } }, [mineCt, face, timeEl]));
      body.appendChild(boardEl);
      body.appendChild(el("div", { class: "statusbar" }, "Left-click reveal · Right-click flag"));
      init();
      body._cleanup = () => clearInterval(timer);
    }
  });

  /* ---------- Piano / Synth ---------- */
  reg({
    id: "piano", title: "VIBE Synth", icon: "🎹", w: 560, h: 260, pinned: true,
    keywords: ["piano", "synth", "synthesizer", "music", "keyboard", "instrument", "sound", "audio", "melody"],
    build(body) {
      let wave = "sine";
      const notes = [["C", 261.63], ["C#", 277.18], ["D", 293.66], ["D#", 311.13], ["E", 329.63], ["F", 349.23], ["F#", 369.99], ["G", 392.0], ["G#", 415.3], ["A", 440.0], ["A#", 466.16], ["B", 493.88], ["C2", 523.25], ["C#2", 554.37], ["D2", 587.33], ["D#2", 622.25], ["E2", 659.25], ["F2", 698.46]];
      const keyMap = "awsedftgyhujkolp;".split("");
      const wrap = el("div", { style: { position: "relative", height: "150px", margin: "10px", background: "#222", padding: "6px", borderRadius: "6px" } });
      const whiteRow = el("div", { style: { display: "flex", height: "100%", gap: "2px" } });
      const sharps = el("div", { style: { position: "absolute", top: "6px", left: "6px", right: "6px", height: "60%", pointerEvents: "none", display: "flex" } });
      let whiteIdx = 0;
      function play(freq) { const a = audio(); if (!a) return; const o = a.createOscillator(), g = a.createGain(); o.type = wave; o.frequency.value = freq; g.gain.value = 0.0001; o.connect(g); g.connect(a.destination); const t = a.currentTime; o.start(t); g.gain.exponentialRampToValueAtTime(0.22, t + 0.01); g.gain.exponentialRampToValueAtTime(0.0001, t + 0.9); o.stop(t + 0.95); }
      const whiteW = 100 / notes.filter(n => !n[0].includes("#")).length;
      notes.forEach((n, i) => {
        const isSharp = n[0].includes("#");
        if (!isSharp) {
          const k = el("div", { style: { flex: "1", background: "linear-gradient(#fff,#eee)", border: "1px solid #999", borderRadius: "0 0 4px 4px", display: "flex", alignItems: "flex-end", justifyContent: "center", paddingBottom: "6px", fontSize: "10px", color: "#777", cursor: "pointer" }, onmousedown: () => { k.style.background = "#cfe0ff"; play(n[1]); } });
          k.addEventListener("mouseup", () => k.style.background = "linear-gradient(#fff,#eee)");
          k.addEventListener("mouseleave", () => k.style.background = "linear-gradient(#fff,#eee)");
          k.textContent = keyMap[i] || "";
          k._freq = n[1]; n._el = k;
          whiteRow.appendChild(k); whiteIdx++;
        }
      });
      // sharps overlay positioned over the white keys
      let wpos = 0;
      const whiteCount = notes.filter(n => !n[0].includes("#")).length;
      notes.forEach((n) => {
        if (n[0].includes("#")) {
          const left = (wpos) * (100 / whiteCount) - (100 / whiteCount) * 0.3;
          const k = el("div", { style: { position: "absolute", left: left + "%", width: (100 / whiteCount * 0.6) + "%", height: "100%", background: "linear-gradient(#333,#000)", border: "1px solid #000", borderRadius: "0 0 3px 3px", cursor: "pointer", pointerEvents: "auto", zIndex: "2" }, onmousedown: () => { k.style.background = "#3a6fd6"; play(n[1]); } });
          k.addEventListener("mouseup", () => k.style.background = "linear-gradient(#333,#000)");
          k.addEventListener("mouseleave", () => k.style.background = "linear-gradient(#333,#000)");
          n._el = k; sharps.appendChild(k);
        } else wpos++;
      });
      wrap.appendChild(whiteRow); wrap.appendChild(sharps);
      const onkey = e => { const i = keyMap.indexOf(e.key.toLowerCase()); if (i >= 0 && notes[i]) { play(notes[i][1]); if (notes[i]._el) { const o = notes[i]._el; o.style.filter = "brightness(.8)"; setTimeout(() => o.style.filter = "", 120); } e.preventDefault(); } };
      body.tabIndex = 0; body.addEventListener("keydown", onkey);
      body.appendChild(el("div", { class: "toolbar" }, [
        el("span", { class: "muted" }, "Wave:"),
        ...["sine", "square", "sawtooth", "triangle"].map(w => el("button", { class: "btn", onclick: e => { wave = w; [...e.target.parentNode.querySelectorAll(".btn")].forEach(b => b.classList.remove("primary")); e.target.classList.add("primary"); } }, w)),
        el("span", { style: { flex: "1" } }), el("span", { class: "muted" }, "play with mouse or A-W-S-E-D… keys")
      ]));
      body.querySelector(".btn").classList.add("primary");
      body.appendChild(wrap);
      setTimeout(() => body.focus(), 50);
    }
  });

  /* ---------- Clock ---------- */
  reg({
    id: "clock", title: "Clock", icon: "🕐", w: 280, h: 320,
    keywords: ["clock", "time", "analog clock", "watch"],
    build(body) {
      const R = 110;
      const cvs = el("canvas", { width: 240, height: 240, style: { display: "block", margin: "12px auto" } });
      const cx = cvs.getContext("2d");
      const digital = el("div", { style: { textAlign: "center", fontFamily: "Consolas,monospace", fontSize: "26px", color: "#1941a5" } });
      function tick() {
        const d = new Date();
        cx.clearRect(0, 0, 240, 240); cx.save(); cx.translate(120, 120);
        cx.beginPath(); cx.arc(0, 0, R, 0, 7); cx.fillStyle = "#fff"; cx.fill(); cx.lineWidth = 4; cx.strokeStyle = "#1941a5"; cx.stroke();
        for (let i = 0; i < 12; i++) { cx.save(); cx.rotate(i * Math.PI / 6); cx.beginPath(); cx.moveTo(0, -R + 6); cx.lineTo(0, -R + 16); cx.lineWidth = 2; cx.strokeStyle = "#333"; cx.stroke(); cx.restore(); }
        const h = d.getHours() % 12, m = d.getMinutes(), s = d.getSeconds();
        hand((h + m / 60) * Math.PI / 6, R * 0.5, 5, "#222");
        hand((m + s / 60) * Math.PI / 30, R * 0.75, 3, "#222");
        hand(s * Math.PI / 30, R * 0.85, 1.5, "#c0392b");
        cx.beginPath(); cx.arc(0, 0, 5, 0, 7); cx.fillStyle = "#c0392b"; cx.fill();
        cx.restore();
        digital.textContent = d.toLocaleTimeString();
      }
      function hand(ang, len, w, col) { cx.save(); cx.rotate(ang); cx.beginPath(); cx.moveTo(0, 10); cx.lineTo(0, -len); cx.lineWidth = w; cx.lineCap = "round"; cx.strokeStyle = col; cx.stroke(); cx.restore(); }
      body.appendChild(cvs); body.appendChild(digital);
      tick(); const t = setInterval(tick, 1000);
      body._cleanup = () => clearInterval(t);
    }
  });

  /* ---------- Calendar ---------- */
  reg({
    id: "calendar", title: "Calendar", icon: "📅", w: 320, h: 340,
    keywords: ["calendar", "month", "dates", "schedule", "day planner", "appointments"],
    build(body) {
      let view = new Date();
      const head = el("div", { class: "toolbar", style: { justifyContent: "space-between" } });
      const grid = el("div", { style: { display: "grid", gridTemplateColumns: "repeat(7,1fr)", gap: "2px", padding: "8px" } });
      function render() {
        grid.innerHTML = ""; head.innerHTML = "";
        head.append(
          el("button", { class: "btn", onclick: () => { view.setMonth(view.getMonth() - 1); render(); } }, "‹"),
          el("b", {}, view.toLocaleString(undefined, { month: "long", year: "numeric" })),
          el("button", { class: "btn", onclick: () => { view.setMonth(view.getMonth() + 1); render(); } }, "›")
        );
        ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"].forEach(d => grid.appendChild(el("div", { style: { textAlign: "center", fontWeight: "bold", color: "#1941a5", fontSize: "11px" } }, d)));
        const first = new Date(view.getFullYear(), view.getMonth(), 1).getDay();
        const days = new Date(view.getFullYear(), view.getMonth() + 1, 0).getDate();
        const now = new Date();
        for (let i = 0; i < first; i++) grid.appendChild(el("div"));
        for (let d = 1; d <= days; d++) {
          const isToday = d === now.getDate() && view.getMonth() === now.getMonth() && view.getFullYear() === now.getFullYear();
          grid.appendChild(el("div", { style: { textAlign: "center", padding: "6px 0", borderRadius: "4px", cursor: "default", background: isToday ? "#245edb" : "transparent", color: isToday ? "#fff" : "#000", fontWeight: isToday ? "bold" : "normal" } }, String(d)));
        }
      }
      body.appendChild(head); body.appendChild(grid); render();
    }
  });

  /* ---------- To-Do / Tasks ---------- */
  reg({
    id: "todo", title: "Tasks", icon: "✅", w: 360, h: 400, pinned: true,
    keywords: ["todo", "to-do", "to do", "tasks", "task", "checklist", "list", "reminders", "things to do"],
    build(body, ctx) {
      const key = "todo:" + (ctx.query || "default");
      let items = store.get(key, []);
      const list = el("ul", { class: "gen-list", style: { flex: "1", overflow: "auto", padding: "4px 10px" } });
      const input = el("input", { class: "input", placeholder: "Add a task and press Enter…", style: { flex: "1" } });
      const save = () => store.set(key, items);
      function render() {
        list.innerHTML = "";
        if (!items.length) list.appendChild(el("li", { class: "muted" }, "No tasks yet — add one above."));
        items.forEach((it, i) => {
          const cb = el("input", { type: "checkbox" }); cb.checked = it.done; cb.onchange = () => { it.done = cb.checked; save(); render(); };
          list.appendChild(el("li", {}, [
            cb,
            el("span", { style: { textDecoration: it.done ? "line-through" : "none", color: it.done ? "#999" : "#000", flex: "1" } }, it.text),
            el("span", { class: "x", onclick: () => { items.splice(i, 1); save(); render(); } }, "✕")
          ]));
        });
        count.textContent = items.filter(i => !i.done).length + " left · " + items.length + " total";
      }
      const add = () => { const v = input.value.trim(); if (!v) return; items.push({ text: v, done: false }); input.value = ""; save(); render(); };
      const form = el("form", { class: "toolbar", onsubmit: e => { e.preventDefault(); add(); } }, [input, el("button", { class: "btn primary", type: "submit" }, "Add")]);
      const count = el("span", {});
      body.style.display = "flex"; body.style.flexDirection = "column";
      body.appendChild(form); body.appendChild(list);
      body.appendChild(el("div", { class: "statusbar" }, [count, el("span", { class: "x muted", style: { cursor: "pointer" }, onclick: () => { items = items.filter(i => !i.done); save(); render(); } }, "clear done")]));
      render();
    }
  });

  /* ---------- Weather (generated) ---------- */
  reg({
    id: "weather", title: "Weather", icon: "🌤️", w: 360, h: 380,
    keywords: ["weather", "forecast", "temperature", "rain", "climate"],
    build(body, ctx) {
      const cityInput = el("input", { class: "input", placeholder: "Enter a city…", style: { flex: "1" }, value: cityFromQuery(ctx.query) });
      const out = el("div", { style: { flex: "1", overflow: "auto" } });
      function cityFromQuery(q) { if (!q) return "Vibe City"; const m = q.replace(/weather|forecast|in|for/gi, "").trim(); return m || "Vibe City"; }
      function hashSeed(s) { let h = 2166136261; for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 16777619); } return (h >>> 0); }
      function gen(city) {
        const seed = hashSeed(city.toLowerCase() + new Date().toDateString());
        let s = seed; const rnd = () => { s = (s * 1103515245 + 12345) & 0x7fffffff; return s / 0x7fffffff; };
        const conds = [["☀️", "Sunny"], ["🌤️", "Mostly Sunny"], ["⛅", "Partly Cloudy"], ["☁️", "Cloudy"], ["🌧️", "Rain"], ["⛈️", "Thunderstorms"], ["🌫️", "Foggy"], ["❄️", "Snow"]];
        const base = 8 + Math.floor(rnd() * 22);
        const c0 = conds[Math.floor(rnd() * conds.length)];
        out.innerHTML = "";
        out.appendChild(el("div", { class: "gen-hero", style: { background: "linear-gradient(135deg,#4b8ff0,#6fb0f6)", textAlign: "center" } }, [
          el("div", { style: { fontSize: "64px" } }, c0[0]),
          el("h2", {}, base + "°C — " + c0[1]),
          el("p", {}, city), el("p", { style: { fontSize: "11px" } }, "Feels like " + (base + Math.floor(rnd() * 4 - 2)) + "°C · Humidity " + (40 + Math.floor(rnd() * 50)) + "%")
        ]));
        const week = el("div", { style: { padding: "10px" } });
        const days = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
        const today = new Date().getDay();
        for (let i = 0; i < 5; i++) {
          const c = conds[Math.floor(rnd() * conds.length)];
          const hi = base + Math.floor(rnd() * 6), lo = base - Math.floor(rnd() * 8);
          week.appendChild(el("div", { style: { display: "flex", alignItems: "center", padding: "7px 6px", borderBottom: "1px solid #eee" } }, [
            el("b", { style: { width: "44px" } }, i === 0 ? "Today" : days[(today + i) % 7]),
            el("span", { style: { fontSize: "20px", width: "36px" } }, c[0]),
            el("span", { class: "muted", style: { flex: "1" } }, c[1]),
            el("b", {}, hi + "°"), el("span", { class: "muted" }, " / " + lo + "°")
          ]));
        }
        out.appendChild(week);
        out.appendChild(el("div", { class: "statusbar" }, "Simulated forecast · VIBE Weather Service"));
      }
      const form = el("form", { class: "toolbar", onsubmit: e => { e.preventDefault(); gen(cityInput.value.trim() || "Vibe City"); } }, [cityInput, el("button", { class: "btn primary" }, "Go")]);
      body.style.display = "flex"; body.style.flexDirection = "column";
      body.appendChild(form); body.appendChild(out);
      gen(cityInput.value || "Vibe City");
    }
  });

  /* ---------- Terminal ---------- */
  reg({
    id: "terminal", title: "VIBE Terminal", icon: "💻", w: 520, h: 320,
    keywords: ["terminal", "command", "console", "shell", "cmd", "prompt", "bash"],
    build(body, ctx) {
      const out = el("div", { style: { flex: "1", overflow: "auto", padding: "8px", fontFamily: "Consolas,monospace", fontSize: "13px", whiteSpace: "pre-wrap", color: "#dfe" } });
      const inp = el("input", { style: { flex: "1", background: "transparent", border: "none", color: "#9f9", fontFamily: "Consolas,monospace", fontSize: "13px", outline: "none" }, spellcheck: "false" });
      function line(t, c) { out.appendChild(el("div", { style: c ? { color: c } : {} }, t)); out.scrollTop = out.scrollHeight; }
      line("VIBE OS [Version 1.0.2026]");
      line("(c) VIBE Corporation. Type 'help'.", "#7af");
      const cmds = {
        help: () => "Commands: help, about, apps, build <app>, echo, date, whoami, color, clear, joke, ver",
        about: () => "VIBE OS — the operating system that builds whatever you search for.",
        apps: () => "Installed: " + APPS.map(a => a.id).join(", "),
        date: () => new Date().toString(),
        whoami: () => "vibe-user",
        ver: () => "VIBE OS 1.0.2026",
        joke: () => ["Why did the OS go to therapy? Too many unresolved processes.", "There are 10 kinds of people: those who get binary and those who don't.", "I'd tell you a UDP joke but you might not get it."][Math.floor(Math.random() * 3)],
        echo: a => a.join(" "),
        build: a => { if (!a.length) return "usage: build <app name>"; setTimeout(() => global.VIBE.launch(a.join(" ")), 200); return "Building '" + a.join(" ") + "'…"; },
        color: () => { out.style.color = "#" + Math.floor(Math.random() * 16777215).toString(16).padStart(6, "0"); return "color changed."; },
        clear: () => { out.innerHTML = ""; return null; }
      };
      const run = raw => {
        const t = raw.trim(); if (!t) return;
        line("C:\\VIBE> " + t, "#6cf");
        const [c, ...a] = t.split(/\s+/);
        const fn = cmds[c.toLowerCase()];
        const res = fn ? fn(a) : "'" + c + "' is not recognized. Try 'help'.";
        if (res != null) line(res);
      };
      const form = el("form", { style: { display: "flex", padding: "4px 8px", borderTop: "1px solid #1a3a1a" }, onsubmit: e => { e.preventDefault(); run(inp.value); inp.value = ""; } }, [el("span", { style: { color: "#6cf", marginRight: "6px" } }, "C:\\VIBE>"), inp]);
      body.style.display = "flex"; body.style.flexDirection = "column"; body.style.background = "#0b1a0b";
      body.appendChild(out); body.appendChild(form);
      body.addEventListener("mousedown", () => inp.focus());
      setTimeout(() => inp.focus(), 60);
    }
  });

  /* ---------- Browser ---------- */
  reg({
    id: "browser", title: "Vibe Explorer", icon: "🌐", w: 640, h: 460,
    keywords: ["browser", "internet", "web", "explorer", "google", "search engine", "website"],
    build(body) {
      const home = "about:vibe";
      const addr = el("input", { class: "input", style: { flex: "1" }, value: home });
      const frame = el("div", { style: { flex: "1", overflow: "auto", background: "#fff" } });
      function homePage() {
        frame.innerHTML = "";
        frame.appendChild(el("div", { style: { textAlign: "center", padding: "60px 20px" } }, [
          el("div", { style: { fontSize: "48px", fontWeight: "bold" } }, [el("span", { html: "<span style='color:#245edb'>Vibe</span><span style='color:#ff9f1a'>Search</span>" })]),
          el("p", { class: "muted" }, "the web, the way you imagine it"),
          (() => {
            const f = el("form", { style: { marginTop: "14px" }, onsubmit: e => { e.preventDefault(); go("vibe://results?q=" + encodeURIComponent(f.querySelector("input").value)); } }, [
              el("input", { class: "input", placeholder: "Search the vibe…", style: { width: "60%", padding: "8px" } })
            ]);
            return f;
          })()
        ]));
      }
      function go(url) {
        url = url.trim(); if (!url) return;
        addr.value = url;
        if (url === "about:vibe" || url === "vibe://home") return homePage();
        if (url.startsWith("vibe://results")) {
          const q = decodeURIComponent((url.split("q=")[1] || "")).replace(/\+/g, " ");
          frame.innerHTML = "";
          frame.appendChild(el("div", { style: { padding: "16px" } }, [
            el("div", { class: "muted", style: { marginBottom: "10px" } }, "About 1,000,000 vibes for “" + q + "”"),
            ...Array.from({ length: 5 }, (_, i) => el("div", { style: { marginBottom: "14px" } }, [
              el("div", { html: "<a href='#' style='color:#1a0dab;font-size:15px;text-decoration:none'>" + q + " — result " + (i + 1) + "</a>" }),
              el("div", { style: { color: "#006621", fontSize: "11px" } }, "https://vibe." + q.replace(/\s+/g, "") + ".net/" + (i + 1)),
              el("div", { class: "muted" }, "The definitive resource about " + q + ". Everything you ever wanted to know, generated fresh by the VIBE engine.")
            ])),
            el("div", { style: { marginTop: "10px" }, class: "muted" }, "💡 Tip: type an app name in the VIBE OS search bar to actually build it.")
          ]));
          return;
        }
        // real URL — try iframe (may be blocked by remote site)
        frame.innerHTML = "";
        const f = el("iframe", { src: /^https?:/.test(url) ? url : "https://" + url, style: { width: "100%", height: "100%", border: "none" } });
        f.addEventListener("load", () => {}, { once: true });
        frame.appendChild(f);
        addr.value = /^https?:/.test(url) ? url : "https://" + url;
      }
      const bar = el("form", { class: "toolbar", onsubmit: e => { e.preventDefault(); go(addr.value); } }, [
        el("button", { class: "btn", type: "button", onclick: () => go(home) }, "🏠"),
        el("button", { class: "btn", type: "button", onclick: homePage }, "↻"),
        addr, el("button", { class: "btn blue" }, "Go")
      ]);
      body.style.display = "flex"; body.style.flexDirection = "column";
      body.appendChild(bar); body.appendChild(frame);
      homePage();
    }
  });

  /* ---------- About / Welcome ---------- */
  reg({
    id: "about", title: "About VIBE OS", icon: "💠", w: 460, h: 360,
    keywords: ["about", "welcome", "help", "vibe os", "info", "credits", "system"],
    build(body) {
      body.appendChild(el("div", { class: "gen-hero", style: { background: "linear-gradient(135deg,#245edb,#1941a5)" } }, [
        el("h2", { html: "VIBE&nbsp;<span style='color:#ff9f1a;font-style:italic'>OS</span>" }),
        el("p", {}, "The operating system that builds whatever app you search for.")
      ]));
      body.appendChild(el("div", { class: "app-pad" }, [
        el("p", {}, "Type the name of any app into the search bar — the big one on the desktop, or the one in the Start menu — and press Enter. VIBE OS will build it and open it in a window."),
        el("p", {}, [el("b", {}, "Try the real ones: "), "calculator, notepad, paint, snake, minesweeper, piano, clock, calendar, tasks, weather, terminal, browser."]),
        el("p", {}, [el("b", {}, "Or invent something: "), "“pizza order tracker”, “gym workout logger”, “space shooter”, “budget planner”, “habit streaks” — VIBE OS scaffolds a working app on the spot."]),
        el("div", { class: "tag-row", style: { marginTop: "8px" } }, ["Windows XP vibes", "no install", "100% in your browser", "your data stays local"].map(t => el("span", { class: "pill" }, t)))
      ]));
      body.appendChild(el("div", { class: "statusbar" }, [el("span", {}, "VIBE OS 1.0.2026"), el("span", { class: "muted" }, "© VIBE Corporation")]));
    }
  });

  /* =========================================================
     GENERATIVE BUILDER — for anything not matched above.
     Categorizes the query and assembles a working app.
     ========================================================= */
  function titleCase(s) { return s.replace(/\b\w/g, c => c.toUpperCase()); }
  function pick(arr, seed) { return arr[Math.abs(seed) % arr.length]; }
  function seedOf(s) { let h = 0; for (let i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) | 0; return h; }
  const ICONS = ["🚀", "⭐", "📦", "🎯", "🔥", "💡", "🎮", "📊", "🛠️", "🌈", "🍕", "🏆", "🎵", "📈", "🧩", "🐱", "🌮", "⚡", "🎲", "🪐"];
  const HUE = ["#245edb,#1941a5", "#8e44ad,#5b2c6f", "#16a085,#0e6655", "#d35400,#a04000", "#c0392b,#922b21", "#2980b9,#1b4f72", "#27ae60,#196f3d", "#e67e22,#b9770e"];

  function genericApp(query) {
    const q = titleCase(query.trim());
    const seed = seedOf(query.toLowerCase());
    const lower = query.toLowerCase();
    const icon = pick(ICONS, seed);
    const grad = pick(HUE, seed);

    // sub-category builders that produce a real interactive widget
    function listTracker(body, label, columns) {
      const key = "gen:list:" + lower;
      let rows = store.get(key, []);
      const save = () => store.set(key, rows);
      const list = el("ul", { class: "gen-list" });
      function render() {
        list.innerHTML = "";
        if (!rows.length) list.appendChild(el("li", { class: "muted" }, "Nothing yet — add your first " + label + " below."));
        rows.forEach((r, i) => list.appendChild(el("li", {}, [
          el("span", { style: { flex: "1" } }, r.name),
          r.meta ? el("span", { class: "pill" }, r.meta) : el("span"),
          el("span", { class: "x", onclick: () => { rows.splice(i, 1); save(); render(); } }, "✕")
        ])));
        counter.textContent = rows.length + " " + label + (rows.length === 1 ? "" : "s");
      }
      const n1 = el("input", { class: "input", placeholder: label + " name…", style: { flex: "1" } });
      const n2 = el("input", { class: "input", placeholder: columns[1] || "detail (optional)", style: { width: "120px" } });
      const counter = el("span", {});
      const form = el("form", { class: "toolbar", onsubmit: e => { e.preventDefault(); const v = n1.value.trim(); if (!v) return; rows.push({ name: v, meta: n2.value.trim() }); n1.value = n2.value = ""; save(); render(); n1.focus(); } }, [n1, n2, el("button", { class: "btn primary" }, "Add")]);
      body.appendChild(form);
      body.appendChild(el("div", { style: { flex: "1", overflow: "auto", padding: "4px 10px" } }, list));
      body.appendChild(el("div", { class: "statusbar" }, [counter, el("span", { class: "muted x", style: { cursor: "pointer" }, onclick: () => { rows = []; save(); render(); } }, "clear all")]));
      render();
    }

    function counterApp(body) {
      const key = "gen:count:" + lower;
      let val = store.get(key, 0);
      const big = el("div", { style: { fontSize: "72px", fontWeight: "bold", textAlign: "center", color: "#1941a5", padding: "20px" } }, String(val));
      const set = v => { val = v; big.textContent = String(val); store.set(key, val); beep(420, 0.05, "square", 0.06); };
      body.appendChild(big);
      body.appendChild(el("div", { class: "toolbar", style: { justifyContent: "center", gap: "10px" } }, [
        el("button", { class: "btn", style: { fontSize: "20px", padding: "8px 20px" }, onclick: () => set(val - 1) }, "−"),
        el("button", { class: "btn", onclick: () => set(0) }, "Reset"),
        el("button", { class: "btn primary", style: { fontSize: "20px", padding: "8px 20px" }, onclick: () => set(val + 1) }, "+")
      ]));
      body.appendChild(el("div", { class: "statusbar" }, "Tap + to count your " + q.toLowerCase() + " · saved automatically"));
    }

    function clickerGame(body) {
      let score = 0, time = 15, playing = false, t;
      const best = "gen:game:" + lower;
      const scoreEl = el("div", { style: { fontSize: "40px", fontWeight: "bold", color: "#1941a5" } }, "0");
      const timeEl = el("span", { class: "pill" }, "⏱ 15");
      const bestEl = el("span", { class: "pill" }, "🏆 " + store.get(best, 0));
      const target = el("button", { style: { width: "90px", height: "90px", borderRadius: "50%", fontSize: "40px", border: "none", cursor: "pointer", background: "radial-gradient(circle at 35% 30%, #ffe9a8, #ff9f1a)", boxShadow: "0 6px 14px rgba(0,0,0,.3)", transition: "transform .05s" } }, icon);
      const arena = el("div", { style: { flex: "1", position: "relative", display: "grid", placeItems: "center", background: "linear-gradient(135deg,#eef4ff,#dce9ff)" } }, target);
      function move() { const pad = 60; arena.style.position = "relative"; target.style.position = "absolute"; target.style.left = (10 + Math.random() * 70) + "%"; target.style.top = (10 + Math.random() * 70) + "%"; }
      target.onclick = () => {
        if (!playing) { playing = true; score = 0; time = 15; scoreEl.textContent = "0"; t = setInterval(() => { time--; timeEl.textContent = "⏱ " + time; if (time <= 0) end(); }, 1000); }
        score++; scoreEl.textContent = String(score); beep(600 + score * 8, 0.05, "square", 0.07); target.style.transform = "scale(.85)"; setTimeout(() => target.style.transform = "scale(1)", 60); move();
      };
      function end() { clearInterval(t); playing = false; target.style.position = "static"; const b = store.get(best, 0); if (score > b) { store.set(best, score); bestEl.textContent = "🏆 " + score; } alert("Time! You scored " + score + " " + q.toLowerCase() + " points."); }
      body.appendChild(el("div", { class: "toolbar", style: { justifyContent: "space-between" } }, [el("b", {}, q), el("div", { style: { display: "flex", gap: "8px" } }, [timeEl, bestEl])]));
      body.appendChild(el("div", { style: { textAlign: "center", padding: "6px" } }, scoreEl));
      body.appendChild(arena);
      body.appendChild(el("div", { class: "statusbar" }, "Click the " + icon + " as fast as you can for 15 seconds!"));
      body._cleanup = () => clearInterval(t);
    }

    function budgetApp(body) {
      const key = "gen:budget:" + lower;
      let rows = store.get(key, []);
      const save = () => store.set(key, rows);
      const list = el("ul", { class: "gen-list" });
      const total = el("b", {});
      function render() {
        list.innerHTML = ""; let sum = 0;
        rows.forEach((r, i) => { sum += r.amt; list.appendChild(el("li", {}, [el("span", { style: { flex: "1" } }, r.name), el("b", { style: { color: r.amt < 0 ? "#c0392b" : "#27ae60" } }, (r.amt < 0 ? "-$" : "+$") + Math.abs(r.amt).toFixed(2)), el("span", { class: "x", onclick: () => { rows.splice(i, 1); save(); render(); } }, "✕")])); });
        if (!rows.length) list.appendChild(el("li", { class: "muted" }, "Add income (+) and expenses (−) below."));
        total.textContent = (sum < 0 ? "-$" : "$") + Math.abs(sum).toFixed(2); total.style.color = sum < 0 ? "#c0392b" : "#27ae60";
      }
      const nm = el("input", { class: "input", placeholder: "Description…", style: { flex: "1" } });
      const am = el("input", { class: "input", type: "number", placeholder: "Amount", style: { width: "90px" }, step: "0.01" });
      const addF = sign => { const v = nm.value.trim(), a = parseFloat(am.value); if (!v || isNaN(a)) return; rows.push({ name: v, amt: sign * Math.abs(a) }); nm.value = am.value = ""; save(); render(); nm.focus(); };
      body.appendChild(el("form", { class: "toolbar", onsubmit: e => { e.preventDefault(); addF(1); } }, [nm, am, el("button", { class: "btn primary", type: "button", onclick: () => addF(1) }, "+ Income"), el("button", { class: "btn", type: "button", onclick: () => addF(-1) }, "− Expense")]));
      body.appendChild(el("div", { style: { flex: "1", overflow: "auto", padding: "4px 10px" } }, list));
      body.appendChild(el("div", { class: "statusbar" }, [el("span", {}, "Balance"), total]));
      render();
    }

    function chatApp(body) {
      const log = el("div", { style: { flex: "1", overflow: "auto", padding: "10px", display: "flex", flexDirection: "column", gap: "8px" } });
      function bubble(text, me) { log.appendChild(el("div", { style: { alignSelf: me ? "flex-end" : "flex-start", maxWidth: "75%", padding: "7px 11px", borderRadius: "12px", background: me ? "#245edb" : "#e6e6e6", color: me ? "#fff" : "#000" } }, text)); log.scrollTop = log.scrollHeight; }
      const replies = ["Interesting! Tell me more about that.", "Got it — I'm your " + q + " assistant.", "Sure thing. Anything else I can help with?", "That's a great point about " + q.toLowerCase() + ".", "I'm just a vibe, but I hear you!", "Noted ✨", "Let me think… yes, absolutely."];
      bubble("Hi! I'm your " + q + " bot. What's up?", false);
      const inp = el("input", { class: "input", placeholder: "Message…", style: { flex: "1" } });
      body.appendChild(log);
      body.appendChild(el("form", { class: "toolbar", onsubmit: e => { e.preventDefault(); const v = inp.value.trim(); if (!v) return; bubble(v, true); inp.value = ""; setTimeout(() => bubble(pick(replies, seedOf(v) + log.children.length), false), 500); } }, [inp, el("button", { class: "btn primary" }, "Send")]));
    }

    function dashboard(body) {
      // default: a believable "app" scaffold with generated features + a notes scratchpad
      const features = shuffle([
        "Quick add", "Smart sorting", "Cloud-free local save", "Dark mode (soon™)", "Search & filter",
        "Tag your items", "Daily streaks", "Share a link", "Export to file", "Keyboard shortcuts",
        "Reminders", "Insights & stats", "Drag to reorder", "Templates"
      ], seed).slice(0, 6);
      body.appendChild(el("div", { class: "gen-hero", style: { background: "linear-gradient(135deg," + grad + ")" } }, [
        el("div", { style: { fontSize: "34px" } }, icon),
        el("h2", {}, q), el("p", {}, "Freshly built by VIBE OS · v1.0")
      ]));
      const grid = el("div", { class: "gen-grid" });
      grid.appendChild(el("div", { class: "gen-card" }, [el("h4", {}, "✨ Features"), el("ul", { class: "gen-list" }, features.map(f => el("li", {}, [el("input", { type: "checkbox" }), el("span", {}, f)])))]));
      const notesKey = "gen:notes:" + lower;
      const ta = el("textarea", { class: "input", style: { width: "100%", height: "110px", resize: "none" }, placeholder: "Your " + q.toLowerCase() + " notes…" });
      ta.value = store.get(notesKey, ""); ta.oninput = () => store.set(notesKey, ta.value);
      grid.appendChild(el("div", { class: "gen-card" }, [el("h4", {}, "🗒️ Scratchpad"), ta]));
      // mini interactive items list
      const key = "gen:items:" + lower; let items = store.get(key, []);
      const ul = el("ul", { class: "gen-list" });
      const renderItems = () => { ul.innerHTML = ""; if (!items.length) ul.appendChild(el("li", { class: "muted" }, "Add an item →")); items.forEach((it, i) => ul.appendChild(el("li", {}, [el("span", { style: { flex: "1" } }, it), el("span", { class: "x", onclick: () => { items.splice(i, 1); store.set(key, items); renderItems(); } }, "✕")]))); };
      const ii = el("input", { class: "input", placeholder: "Add item…", style: { flex: "1" } });
      grid.appendChild(el("div", { class: "gen-card", style: { gridColumn: "span 2" } }, [
        el("h4", {}, "📋 Your items"),
        el("form", { style: { display: "flex", gap: "6px", marginBottom: "6px" }, onsubmit: e => { e.preventDefault(); const v = ii.value.trim(); if (!v) return; items.push(v); store.set(key, items); ii.value = ""; renderItems(); } }, [ii, el("button", { class: "btn primary" }, "Add")]),
        ul
      ]));
      renderItems();
      body.appendChild(grid);
      body.appendChild(el("div", { class: "statusbar" }, [el("span", {}, "Generated app · everything saves locally"), el("span", { class: "muted" }, "VIBE Builder")]));
    }
    function shuffle(a, s) { a = a.slice(); for (let i = a.length - 1; i > 0; i--) { s = (s * 9301 + 49297) % 233280; const j = Math.abs(s) % (i + 1); [a[i], a[j]] = [a[j], a[i]]; } return a; }

    // route by keywords
    let builder = dashboard, hint = "dashboard";
    if (/\b(game|shooter|clicker|tap|catch|dodge|jump|run|arcade|play)\b/.test(lower)) { builder = clickerGame; hint = "game"; }
    else if (/\b(chat|bot|assistant|messenger|talk|ai|gpt|friend)\b/.test(lower)) { builder = chatApp; hint = "chat"; }
    else if (/\b(budget|expense|money|finance|spend|cost|bank|wallet|invoice|salary|saving)\b/.test(lower)) { builder = budgetApp; hint = "budget"; }
    else if (/\b(count|counter|tally|score|rep|reps|pomodoro|streak|habit)\b/.test(lower)) { builder = counterApp; hint = "counter"; }
    else if (/\b(track|tracker|log|logger|list|manager|inventory|collection|library|catalog|roster|gym|workout|grocery|shopping|recipe|book|movie|order|fleet)\b/.test(lower)) { builder = b => listTracker(b, singular(query), ["name", "detail"]); hint = "tracker"; }

    return {
      id: "gen:" + lower,
      title: q,
      icon,
      w: hint === "game" ? 380 : 440,
      h: hint === "counter" ? 300 : 420,
      generated: true,
      build(body) {
        body.style.display = "flex"; body.style.flexDirection = "column";
        builder(body);
      }
    };
  }
  function singular(s) { s = s.replace(/\b(tracker|app|manager|list|logger|log)\b/gi, "").trim() || s.trim(); const w = s.split(/\s+/).pop(); return w.replace(/s$/, "") || "item"; }

  /* =========================================================
     PUBLIC API
     ========================================================= */
  function resolve(query) {
    const q = (query || "").trim().toLowerCase();
    if (!q) return null;
    // exact id / title / keyword match
    for (const a of APPS) {
      if (a.id === q || a.title.toLowerCase() === q) return a;
    }
    // keyword contains
    let best = null, bestScore = 0;
    for (const a of APPS) {
      for (const kw of a.keywords) {
        let score = 0;
        if (q === kw) score = 100;
        else if (q.includes(kw)) score = 50 + kw.length;
        else if (kw.includes(q) && q.length >= 3) score = 30 + q.length;
        if (score > bestScore) { bestScore = score; best = a; }
      }
    }
    if (best && bestScore >= 30) return best;
    // nothing matched → generate one
    return genericApp(query);
  }

  global.VIBE = global.VIBE || {};
  global.VIBE.APPS = APPS;
  global.VIBE.resolve = resolve;
  global.VIBE.pinned = () => APPS.filter(a => a.pinned);
  global.VIBE.byId = id => APPS.find(a => a.id === id);

})(window);
