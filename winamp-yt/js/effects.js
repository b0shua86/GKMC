/* =========================================================
   effects.js
   ----------------------------------------------------------
   Toggles for CRT / glitch / rave / insane / fake radio /
   easter eggs / boot screen / DJ bubble.
   ========================================================= */
(function (WY) {
  "use strict";

  const DJ_LINES = [
    "you're locked in with WINAMP-YT — 24 hour stream, no commercials",
    "shout out to the listener with the cracked CRT monitor",
    "next track was requested by 'shadowfox42' from undisclosed location",
    "do not adjust your set. this is supposed to look like this.",
    "if you can read this, you survived the boot sequence",
    "tonight on WINAMP-YT — every song, twice as loud",
    "remember: it really whips the YouTube's videos",
    "playlist powered by vibes and questionable taste",
    "low-latency YouTube radio, broadcasting from your localhost",
    "BPM lock acquired — initiating beat sync",
    "skin operator standing by — try Y2K mode (type 'y2k')",
    "fun fact: every preset in this player was hand-tuned. you're welcome."
  ];

  const RADIO_STATIONS = [
    { call: "KWMP",   tag: "Pirate Memestream",    bpm: 128 },
    { call: "98.6FM", tag: "Lo-Fi Heart Rate",     bpm:  88 },
    { call: "VHS-1",  tag: "Late Night Rewinder",  bpm: 110 },
    { call: "Y2K-X",  tag: "Cyber Dispatch",       bpm: 140 },
    { call: "DUSK",   tag: "Dusty Beats Outpost",  bpm: 100 }
  ];

  const state = {
    crt:    false,
    glitch: false,
    rave:   false,
    insane: false,
    radio:  null,   // station object when in radio mode
    djTimer: 0,
    djIdx: 0
  };

  function setBodyClass(name, on) { document.body.classList.toggle(name, !!on); }

  function showDJ(line, ms = 4500) {
    const el = document.getElementById("dj-bubble");
    if (!el) return;
    el.textContent = "📻 " + line;
    el.classList.add("show");
    clearTimeout(el._t);
    el._t = setTimeout(() => el.classList.remove("show"), ms);
  }

  function nextDJ() {
    state.djIdx = (state.djIdx + 1) % DJ_LINES.length;
    showDJ(DJ_LINES[state.djIdx]);
  }

  // ---------- boot screen ----------
  function boot() {
    const el = document.getElementById("boot-screen");
    const tx = document.getElementById("boot-text");
    if (!el || !tx) return;
    const banner = [
      "  ╦ ╦╦╔╗╔╔═╗╔╦╗╔═╗  ╦ ╦╔╦╗",
      "  ║║║║║║║╠═╣║║║╠═╝  ╚╦╝ ║ ",
      "  ╚╩╝╩╝╚╝╩ ╩╩ ╩╩     ╩  ╩ ",
      ""
    ];
    const lines = [
      ...banner,
      "[BIOS]   POST OK",
      "[BIOS]   detecting audio device ......... [ FOUND  ]",
      "[BOOT]   mounting localStorage ........... [ OK     ]",
      "[BOOT]   loading skin: classic ............ [ OK     ]",
      "[BOOT]   probing YouTube IFrame API ....... [ READY  ]",
      "[BOOT]   warming visualizer presets ....... [ 7 OK   ]",
      "[BOOT]   linking simulated AnalyserNode ... [ OK     ]",
      "[BOOT]   linking equalizer (visual-only) .. [ OK     ]",
      "",
      "WINAMP-YT v1.0 — it really whips the YouTube's videos",
      "press any key to continue"
    ];
    let i = 0;
    const tick = () => {
      tx.textContent = lines.slice(0, i+1).join("\n");
      i++;
      if (i < lines.length) setTimeout(tick, 70);
    };
    tick();
    const dismiss = () => {
      el.classList.add("gone");
      window.removeEventListener("keydown", dismiss);
      window.removeEventListener("mousedown", dismiss);
    };
    window.addEventListener("keydown",   dismiss);
    window.addEventListener("mousedown", dismiss);
    setTimeout(dismiss, 3000);
  }

  // ---------- radio mode ----------
  function toggleRadio() {
    if (state.radio) {
      state.radio = null;
      WY.toast("Radio mode OFF");
      const m = document.querySelector("[data-action='toggle-radio']");
      m && m.classList.remove("active");
      return;
    }
    state.radio = WY.pick(RADIO_STATIONS);
    WY.toast("Tuning in to " + state.radio.call);
    showDJ("YOU'RE NOW TUNED IN TO " + state.radio.call + " — " + state.radio.tag);
    const m = document.querySelector("[data-action='toggle-radio']");
    m && m.classList.add("active");
    // bias the simulated BPM
    WY.audio.setBpm(state.radio.bpm);
    // schedule occasional DJ lines
    clearInterval(state._djTimer);
    state._djTimer = setInterval(() => {
      if (state.radio) nextDJ();
    }, 22000);
  }

  // ---------- DJ between songs ----------
  function init() {
    boot();

    // a DJ blurb between tracks
    WY.bus.on("player:ended", () => {
      if (Math.random() < 0.5 || state.radio) setTimeout(nextDJ, 600);
    });

    // wire radio button
    WY.on(document.querySelector("[data-action='toggle-radio']"), "click", toggleRadio);
  }

  WY.effects = {
    init,

    toggleCrt()    { state.crt = !state.crt;     setBodyClass("crt",    state.crt);
                     WY.toast("CRT " + (state.crt    ? "ON" : "OFF")); },
    toggleGlitch() { state.glitch = !state.glitch; setBodyClass("glitch", state.glitch);
                     WY.toast("Glitch " + (state.glitch ? "ON" : "OFF")); },
    toggleRave()   { state.rave = !state.rave;    setBodyClass("rave",   state.rave);
                     WY.toast("Rave " + (state.rave   ? "ON" : "OFF")); },
    toggleInsane() { state.insane = !state.insane;
                     WY.audio.setInsane(state.insane);
                     WY.toast("Insane mode " + (state.insane ? "ON — intensity is rising" : "OFF")); },
    toggleRadio,

    isInsane() { return state.insane; },
    isRadio()  { return !!state.radio; },
    nextDJ,

    easter(code) {
      if (code === "llama") {
        WY.toast("☆ it really whips the llama's ass ☆", 3500);
        document.body.classList.add("glitch");
        setTimeout(() => document.body.classList.remove("glitch"), 1200);
      }
    }
  };

})(window.WY);
