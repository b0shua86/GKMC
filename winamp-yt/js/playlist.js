/* =========================================================
   playlist.js
   ----------------------------------------------------------
   Owns the playlist data + UI list rendering.

   YouTube playlist input:
     - full URL  (https://www.youtube.com/playlist?list=PLxxxxx)
     - watch URL with &list=PLxxxxx
     - raw playlist id beginning with PL, LL, UU, OL, FL, RD
     - comma- or newline-separated list of video IDs / URLs
       (handy when you don't have a real playlist)

   Real YouTube playlists are loaded via the IFrame Player by
   calling .loadPlaylist({ list: "PL..." }).  Because we have no
   YouTube Data API key, we don't pre-fetch every track's title;
   instead we use the player's onStateChange + getVideoData()
   to discover the currently-playing title and back-fill the
   table row.  We learn track count via getPlaylist().

   For raw videoId lists we know the count up front.
   ========================================================= */
(function (WY) {
  "use strict";

  const state = {
    /** "real" | "ids" */
    mode:     "ids",
    /** PLxxxx if mode=="real", else null */
    plId:     null,
    /** array of {id, title, dur} */
    tracks:   [],
    /** integer index into tracks */
    index:    0,
    shuffle:  false,
    repeat:   "none", // "none" | "one" | "all"
    history:  [],
  };

  // ---------- parsing ----------
  const ID_RE  = /^[A-Za-z0-9_-]{11}$/;
  const PLID_RE = /^(PL|LL|UU|OL|FL|RD|UL)[A-Za-z0-9_-]{10,}$/;

  function parseInput(raw) {
    raw = (raw || "").trim();
    if (!raw) return null;

    // url with list=
    const m = raw.match(/[?&]list=([^&]+)/);
    if (m && PLID_RE.test(m[1])) return { kind: "playlist", id: m[1] };

    // bare playlist id
    if (PLID_RE.test(raw)) return { kind: "playlist", id: raw };

    // single watch url -> single id
    const wm = raw.match(/[?&]v=([A-Za-z0-9_-]{11})/);
    if (wm) return { kind: "ids", ids: [wm[1]] };

    // youtu.be/<id>
    const sm = raw.match(/youtu\.be\/([A-Za-z0-9_-]{11})/);
    if (sm) return { kind: "ids", ids: [sm[1]] };

    // comma / newline list of urls or ids
    const parts = raw.split(/[\s,;\n]+/).map(s => s.trim()).filter(Boolean);
    const ids = [];
    for (const p of parts) {
      if (ID_RE.test(p)) { ids.push(p); continue; }
      const m1 = p.match(/[?&]v=([A-Za-z0-9_-]{11})/);
      if (m1) { ids.push(m1[1]); continue; }
      const m2 = p.match(/youtu\.be\/([A-Za-z0-9_-]{11})/);
      if (m2) { ids.push(m2[1]); continue; }
    }
    if (ids.length) return { kind: "ids", ids };

    return null;
  }

  // ---------- rendering ----------
  const $list  = () => document.getElementById("pl-list");
  const $count = () => document.getElementById("pl-count");
  const $total = () => document.getElementById("pl-total-time");
  const $mtxt  = () => document.getElementById("marquee-text");

  function render() {
    const ul = $list(); if (!ul) return;
    ul.innerHTML = "";
    state.tracks.forEach((t, i) => {
      const li = document.createElement("li");
      li.dataset.idx = i;
      if (i === state.index) li.classList.add("active");
      li.innerHTML =
        "<span class='idx'>" + (i + 1) + ".</span>" +
        "<span class='ttl'></span>" +
        "<span class='dur'>" + (t.dur ? WY.fmtTime(t.dur) : "--:--") + "</span>";
      li.querySelector(".ttl").textContent = t.title || ("YouTube • " + t.id);
      ul.appendChild(li);
    });
    $count().textContent = state.tracks.length + " track" + (state.tracks.length === 1 ? "" : "s");
    let total = 0;
    for (const t of state.tracks) total += (t.dur || 0);
    $total().textContent = WY.fmtTime(total);
  }

  function setMarquee(text) {
    const el = $mtxt(); if (el) el.textContent = text;
  }

  // ---------- public ----------
  WY.playlist = {
    state,

    init() {
      // restore saved playlist
      const saved = WY.store.get("playlist", null);
      if (saved && saved.tracks && saved.tracks.length) {
        state.mode    = saved.mode    || "ids";
        state.plId    = saved.plId    || null;
        state.tracks  = saved.tracks;
        state.index   = WY.clamp(saved.index || 0, 0, state.tracks.length - 1);
        state.shuffle = !!saved.shuffle;
        state.repeat  = saved.repeat || "none";
      }

      // bind input
      WY.on(document.getElementById("pl-load"),  "click", () => {
        const v = document.getElementById("pl-input").value;
        WY.playlist.load(v);
      });
      WY.on(document.getElementById("pl-clear"), "click", () => {
        WY.playlist.clear();
      });
      WY.on(document.getElementById("pl-input"), "keydown", (e) => {
        if (e.key === "Enter") WY.playlist.load(e.target.value);
      });
      WY.on($list(), "dblclick", (e) => {
        const li = e.target.closest("li[data-idx]");
        if (!li) return;
        WY.playlist.playIndex(parseInt(li.dataset.idx, 10));
      });
      WY.on($list(), "click", (e) => {
        const li = e.target.closest("li[data-idx]");
        if (!li) return;
        // single click selects highlight only
        state.index = parseInt(li.dataset.idx, 10);
        render(); save();
      });

      // bind transport
      WY.on(document.querySelector("[data-action='shuffle']"), "click", () => WY.playlist.toggleShuffle());

      // when a player track ends, advance
      WY.bus.on("player:ended", () => WY.playlist.autoAdvance());

      // when the player tells us real metadata, store it on current track
      WY.bus.on("player:meta", (m) => {
        const t = state.tracks[state.index];
        if (t) {
          // if we're in a "real playlist" mode and the iframe just told us
          // the id of the now-playing video, make sure our local index matches
          if (state.mode === "real" && t.id !== m.id) {
            // find by id, else append
            const idx = state.tracks.findIndex(x => x.id === m.id);
            if (idx >= 0) { state.index = idx; }
            else {
              state.tracks.push({ id: m.id, title: m.title, dur: m.duration });
              state.index = state.tracks.length - 1;
            }
          } else {
            t.title = m.title || t.title;
            t.dur   = m.duration || t.dur;
          }
          render();
          setMarquee(m.title + "  —  " + m.author + "    ★    WINAMP-YT    ★    ");
          save();
        }
      });

      render();
      if (state.tracks.length) {
        setMarquee("Loaded " + state.tracks.length + " tracks from cache. Press PLAY.");
      }
    },

    load(input) {
      const parsed = parseInput(input);
      if (!parsed) {
        WY.toast("Couldn't parse that — try a YouTube playlist URL or PL... id");
        return;
      }
      if (parsed.kind === "playlist") {
        state.mode   = "real";
        state.plId   = parsed.id;
        state.tracks = [];                  // filled in as the player reports videos
        state.index  = 0;
        WY.player.loadPlaylist(parsed.id);
        WY.toast("Loading playlist " + parsed.id + " …");
        // After the iframe starts playing, we will poll yt.getPlaylist() to
        // discover all video IDs.
        setTimeout(scanRealPlaylist, 1200);
      } else {
        state.mode   = "ids";
        state.plId   = null;
        state.tracks = parsed.ids.map(id => ({ id, title: "YouTube • " + id, dur: 0 }));
        state.index  = 0;
        WY.player.loadPlaylist(parsed.ids);
        WY.toast("Loaded " + parsed.ids.length + " video" + (parsed.ids.length === 1 ? "" : "s"));
      }
      render();
      save();
    },

    // After loading a real PL... playlist, the IFrame API exposes
    // the resolved id list via getPlaylist().  Backfill our table.
    _scanReal: scanRealPlaylist,

    clear() {
      state.mode    = "ids";
      state.plId    = null;
      state.tracks  = [];
      state.index   = 0;
      state.history = [];
      WY.player.stop();
      render();
      setMarquee("★ playlist cleared ★");
      save();
    },

    playIndex(i) {
      if (i < 0 || i >= state.tracks.length) return;
      state.index = i;
      const t = state.tracks[i];
      if (state.mode === "real") {
        // ask iframe to jump to that index
        try {
          const yt = window.YT && document.getElementById("yt-host");
          // The IFrame Player keeps the playlist; we use playVideoAt via
          // the underlying YT.Player. WY.player exposes a generic helper:
        } catch(e){}
        // simplest reliable approach: load by id (keeps playlist context loose,
        // but works without API key)
        WY.player.loadVideo(t.id);
      } else {
        WY.player.loadVideo(t.id);
      }
      render(); save();
    },

    next() {
      if (!state.tracks.length) return;
      if (state.repeat === "one") return WY.playlist.playIndex(state.index);
      if (state.shuffle) {
        let n = state.index;
        if (state.tracks.length > 1) {
          while (n === state.index) n = Math.floor(Math.random() * state.tracks.length);
        }
        state.history.push(state.index);
        return WY.playlist.playIndex(n);
      }
      const n = state.index + 1;
      if (n >= state.tracks.length) {
        if (state.repeat === "all") return WY.playlist.playIndex(0);
        WY.player.stop();
        WY.bus.emit("player:pause");
        return;
      }
      WY.playlist.playIndex(n);
    },

    prev() {
      if (!state.tracks.length) return;
      if (state.shuffle && state.history.length) {
        return WY.playlist.playIndex(state.history.pop());
      }
      const n = state.index - 1;
      if (n < 0) return WY.playlist.playIndex(state.tracks.length - 1);
      WY.playlist.playIndex(n);
    },

    autoAdvance() { WY.playlist.next(); },

    toggleShuffle() {
      state.shuffle = !state.shuffle;
      WY.toast("Shuffle " + (state.shuffle ? "ON" : "OFF"));
      save();
      WY.bus.emit("playlist:mode", { shuffle: state.shuffle, repeat: state.repeat });
    },

    toggleRepeat() {
      state.repeat = state.repeat === "none" ? "all" : (state.repeat === "all" ? "one" : "none");
      WY.toast("Repeat " + state.repeat.toUpperCase());
      save();
      WY.bus.emit("playlist:mode", { shuffle: state.shuffle, repeat: state.repeat });
    }
  };

  // poll YT.Player's getPlaylist() to learn the IDs of a "real" playlist
  function scanRealPlaylist(attempt = 0) {
    if (state.mode !== "real") return;
    // WY.player exposes _getPlaylist() which proxies to the IFrame
    // player's getPlaylist(). It only returns a non-empty array once the
    // playlist has been resolved server-side, hence the polling loop.
    const ids = (WY.player && WY.player._getPlaylist) ? WY.player._getPlaylist() : null;
    if (ids && ids.length) {
      state.tracks = ids.map(id => ({ id, title: "YouTube • " + id, dur: 0 }));
      state.index  = 0;
      render(); save();
      WY.toast("Found " + ids.length + " videos in playlist");
      return;
    }
    if (attempt < 8) setTimeout(() => scanRealPlaylist(attempt + 1), 800);
  }

  function save() {
    WY.store.set("playlist", {
      mode: state.mode,
      plId: state.plId,
      tracks: state.tracks,
      index: state.index,
      shuffle: state.shuffle,
      repeat: state.repeat
    });
  }

})(window.WY);
