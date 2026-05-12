/* =========================================================
   app.js
   ----------------------------------------------------------
   Boots the whole UI once DOM is ready and wires the main
   window's transport buttons to the player + playlist.
   ========================================================= */
(function (WY) {
  "use strict";

  let muted = false;
  let lastVolume = 80;

  function setMuteIcon(b) {
    const el = document.getElementById("mute");
    if (el) el.textContent = b ? "🔇" : "🔊";
  }

  function bindTransport() {
    const map = {
      play:  () => WY.player.play(),
      pause: () => WY.player.pause(),
      stop:  () => WY.player.stop(),
      next:  () => WY.playlist.next(),
      prev:  () => WY.playlist.prev(),
      open:  () => { WY.ui.show("playlist"); document.getElementById("pl-input").focus(); },
      mute:  () => WY.app.toggleMute(),
      "toggle-shuffle": () => {
        WY.playlist.toggleShuffle();
        WY.$('button[data-action="toggle-shuffle"]')
          .classList.toggle("active", WY.playlist.state.shuffle);
      },
      "toggle-repeat": () => {
        WY.playlist.toggleRepeat();
        const b = WY.$('button[data-action="toggle-repeat"]');
        b.classList.toggle("active", WY.playlist.state.repeat !== "none");
        b.textContent = WY.playlist.state.repeat === "one" ? "REP1" : "REP";
      }
    };

    document.addEventListener("click", (e) => {
      const t = e.target.closest("[data-action]");
      if (!t) return;
      const fn = map[t.dataset.action];
      if (fn) fn();
    });

    // sliders
    const vol = document.getElementById("volume");
    WY.on(vol, "input", () => {
      const v = parseInt(vol.value, 10);
      if (v === 0) muted = true; else muted = false;
      setMuteIcon(muted);
      WY.player.setVolume(v);
      lastVolume = v || lastVolume;
    });
    vol.value = WY.store.get("volume", 80);

    // balance is fake (YouTube doesn't expose pan) — just store it
    const bal = document.getElementById("balance");
    WY.on(bal, "input", () => WY.store.set("balance", parseInt(bal.value, 10)));
    bal.value = WY.store.get("balance", 0);

    // restore initial state of mode buttons
    WY.bus.on("player:ready", () => {
      WY.player.setVolume(parseInt(vol.value, 10));
    });
    if (WY.playlist.state.shuffle) WY.$('button[data-action="toggle-shuffle"]').classList.add("active");
    if (WY.playlist.state.repeat !== "none") {
      const b = WY.$('button[data-action="toggle-repeat"]');
      b.classList.add("active");
      if (WY.playlist.state.repeat === "one") b.textContent = "REP1";
    }
  }

  WY.app = {
    toggleMute() {
      muted = !muted;
      setMuteIcon(muted);
      if (muted) { WY.player.mute(); }
      else       { WY.player.unmute(); }
    },
    bumpVolume(d) {
      const vol = document.getElementById("volume");
      const v = WY.clamp(parseInt(vol.value, 10) + d, 0, 100);
      vol.value = v;
      WY.player.setVolume(v);
      WY.toast("VOL " + v);
    }
  };

  function go() {
    WY.skins.init();
    WY.ui.init();
    WY.effects.init();
    WY.equalizer.init();
    WY.playlist.init();
    WY.visualizers.init();
    bindTransport();

    // First-run hint
    if (!WY.store.get("seenHint", false)) {
      setTimeout(() => {
        WY.toast("Paste a YouTube playlist URL → press LOAD → PLAY ✦ keys: X play, B next, [ ] presets", 6000);
        WY.store.set("seenHint", true);
      }, 1200);
    }

    // Resume a previously-loaded playlist (just cue, don't auto-play)
    const st = WY.playlist.state;
    if (st && st.tracks && st.tracks.length) {
      WY.bus.on("player:ready", () => {
        if (st.mode === "real" && st.plId) {
          WY.player.loadPlaylist(st.plId);
        } else {
          WY.player.cueVideo(st.tracks[st.index].id);
        }
      });
    }
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", go);
  } else {
    go();
  }

})(window.WY);
