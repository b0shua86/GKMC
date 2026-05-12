/* =========================================================
   player.js
   ----------------------------------------------------------
   Thin wrapper around the YouTube IFrame Player API.

   WHY AN IFRAME?
   --------------
   YouTube does not publish CORS-friendly audio streams that a
   regular <audio> element could load, and direct scraping of
   media URLs is against their TOS.  The IFrame Player API is
   the only sanctioned, embeddable way to play any public YT
   video from a third-party page.

   The IFrame runs in a separate origin, so its <audio> output
   is NOT reachable by our AnalyserNode.  That's why we drive
   the visualizers from audio-sim.js instead. (Equalizer is
   visual-only for the same reason.)

   API:
     WY.player.init(hostId)
     WY.player.loadVideo(id)           // single video
     WY.player.loadPlaylist(ids)       // array of videoIds
     WY.player.play() / pause() / stop()
     WY.player.next() / prev()
     WY.player.seekTo(seconds)
     WY.player.setVolume(0..100)
     WY.player.mute() / unmute()
     WY.player.getCurrentTime() / getDuration()
     WY.player.getCurrentId()

   Events emitted on WY.bus:
     "player:ready"
     "player:state"   (YT player state code)
     "player:play"
     "player:pause"
     "player:ended"
     "player:meta"    ({id, title, author, duration})
   ========================================================= */
(function (WY) {
  "use strict";

  let yt = null;            // YT.Player instance
  let ready = false;
  let pendingPlay = null;   // queued action while API loads
  let lastId = null;
  let lastMeta = null;

  // Called by the iframe_api script (defined globally on window)
  window.onYouTubeIframeAPIReady = function () {
    yt = new YT.Player("yt-host", {
      width: 1, height: 1,
      playerVars: {
        autoplay:       0,
        controls:       0,
        disablekb:      1,
        modestbranding: 1,
        rel:            0,
        iv_load_policy: 3,
        playsinline:    1,
        fs:             0,
        origin:         location.origin
      },
      events: {
        onReady: () => {
          ready = true;
          // restore last volume
          const v = WY.store.get("volume", 80);
          try { yt.setVolume(v); } catch(e){}
          WY.bus.emit("player:ready");
          if (pendingPlay) { pendingPlay(); pendingPlay = null; }
        },
        onStateChange: (e) => {
          // 1 playing, 2 paused, 0 ended, 3 buffering, 5 cued
          WY.bus.emit("player:state", e.data);
          if (e.data === YT.PlayerState.PLAYING) {
            WY.audio.setPlaying(true);
            WY.bus.emit("player:play");
            tryMeta();
          } else if (e.data === YT.PlayerState.PAUSED) {
            WY.audio.setPlaying(false);
            WY.bus.emit("player:pause");
          } else if (e.data === YT.PlayerState.ENDED) {
            WY.audio.setPlaying(false);
            WY.bus.emit("player:ended");
          }
        },
        onError: (e) => {
          // 100=removed/private, 101/150=blocked embedding, 2=bad id
          WY.toast("YouTube error " + e.data + " — skipping");
          WY.bus.emit("player:ended"); // treat as ended → next track
        }
      }
    });
  };

  function tryMeta() {
    if (!yt || !ready) return;
    try {
      const data = yt.getVideoData ? yt.getVideoData() : null;
      const dur  = yt.getDuration ? yt.getDuration() : 0;
      if (data && data.video_id) {
        lastId  = data.video_id;
        lastMeta = {
          id:       data.video_id,
          title:    data.title  || "(unknown title)",
          author:   data.author || "(unknown)",
          duration: dur || 0
        };
        WY.audio.setVideoId(data.video_id);
        WY.bus.emit("player:meta", lastMeta);
      }
    } catch (e) {}
  }

  // poll the iframe a few times after a load — metadata isn't
  // always available on the first state-change.
  function metaPoll() {
    let n = 0;
    const id = setInterval(() => {
      tryMeta();
      if (++n > 10 || (lastMeta && lastMeta.title && lastMeta.title !== "(unknown title)")) {
        clearInterval(id);
      }
    }, 300);
  }

  // Build a working "loadPlaylist" call that handles either an
  // array of videoIds or a real YT playlist id.
  function _loadPlaylist(ids) {
    if (!yt) return (pendingPlay = () => _loadPlaylist(ids));
    if (Array.isArray(ids)) {
      yt.loadPlaylist({ playlist: ids, index: 0, startSeconds: 0 });
    } else if (typeof ids === "string" && ids.startsWith("PL")) {
      yt.loadPlaylist({ list: ids, listType: "playlist", index: 0 });
    } else {
      WY.toast("Bad playlist payload");
    }
    metaPoll();
  }

  WY.player = {
    init() { /* iframe API initialises itself; nothing to do here */ },

    loadVideo(id) {
      if (!yt) return (pendingPlay = () => WY.player.loadVideo(id));
      yt.loadVideoById(id);
      metaPoll();
    },

    cueVideo(id) {
      if (!yt) return (pendingPlay = () => WY.player.cueVideo(id));
      yt.cueVideoById(id);
      metaPoll();
    },

    loadPlaylist: _loadPlaylist,

    play()  { try { yt && yt.playVideo();  } catch(e){} },
    pause() { try { yt && yt.pauseVideo(); } catch(e){} },
    stop()  { try { yt && yt.stopVideo();  } catch(e){} WY.audio.setPlaying(false); },
    next()  { try { yt && yt.nextVideo();  } catch(e){} },
    prev()  { try { yt && yt.previousVideo(); } catch(e){} },

    seekTo(sec) { try { yt && yt.seekTo(sec, true); } catch(e){} },

    setVolume(v) {
      v = WY.clamp(v|0, 0, 100);
      WY.store.set("volume", v);
      try { yt && yt.setVolume(v); } catch(e){}
    },
    getVolume() { try { return yt ? yt.getVolume() : 80; } catch(e){ return 80; } },
    mute()      { try { yt && yt.mute();   } catch(e){} },
    unmute()    { try { yt && yt.unMute(); } catch(e){} },
    isMuted()   { try { return yt ? yt.isMuted() : false; } catch(e){ return false; } },

    getCurrentTime() { try { return yt ? yt.getCurrentTime() : 0; } catch(e){ return 0; } },
    getDuration()    { try { return yt ? yt.getDuration()    : 0; } catch(e){ return 0; } },
    getCurrentId()   { return lastId; },
    getCurrentMeta() { return lastMeta; },
    isReady()        { return ready; },

    // Used by playlist.js to discover videoIds inside a real "PL..." playlist
    _getPlaylist() {
      try { return yt && yt.getPlaylist ? yt.getPlaylist() : null; } catch(e){ return null; }
    },
    _playAt(idx) {
      try { yt && yt.playVideoAt && yt.playVideoAt(idx); } catch(e){}
    }
  };

})(window.WY);
