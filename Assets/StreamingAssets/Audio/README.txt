AUDIO — good kid, m.A.A.d city tour
===================================

The visual tour runs with or without audio. To hear the album as you walk, the
manager resolves each track in this order:

  1) A local file in THIS folder, one per track. The file just has to START with the
     track number (so your own filenames work without renaming) — these all match:
        01.ogg            "01 Sherane.mp3"        1 - Money Trees.wav
     Track order (match this, e.g. for the deluxe edition's different numbering):
        01  Sherane a.k.a Master Splinter's Daughter
        02  Bitch, Don't Kill My Vibe
        03  Backseat Freestyle
        04  The Art of Peer Pressure
        05  Money Trees
        06  Poetic Justice
        07  good kid
        08  m.A.A.d city
        09  Swimming Pools (Drank)
        10  Sing About Me, I'm Dying of Thirst
        11  Real
        12  Compton
     Use .ogg (most reliable in Unity), .mp3, or .wav. Convert other formats
     (.m4a/.aac/.flac) first — Unity can't decode those at runtime. Audio you drop
     here is git-ignored, so a personal copy won't be committed to the repo.
     If no real file is found, a procedural mood score plays instead (never silent).

  2) A stream resolver URL (optional), if you set one — see below.

  3) Nothing: the on-screen card shows the YouTube link for that track so you
     can open it manually, and the tour continues silently.

USING YOUTUBE
-------------
YouTube does not expose a plain audio URL for a watch page, so Unity cannot
stream a youtube.com/watch link directly. Two supported options:

  A) Easiest (offline, reliable): download each track's audio to .ogg with a
     tool like yt-dlp and drop the files here, named 01.ogg … 12.ogg, e.g.
        yt-dlp -x --audio-format vorbis -o "01.%(ext)s" "<youtube url>"

  B) Stream resolver: run/point to a service that returns a direct audio
     stream for a video id (e.g. an Invidious instance) and set it in
     ../gkmc_tracks.json, for example:
        "streamResolverTemplate": "https://yewtu.be/latest_version?id={id}&itag=140"
     then add each track's "youtubeId". {id} is replaced per track.

Set per-track links/ids/filenames without recompiling by copying
gkmc_tracks.sample.json (one folder up) to gkmc_tracks.json and editing it.

Please respect copyright — use audio you are entitled to use.
