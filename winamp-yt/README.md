# WINAMP-YT

> It really whips the YouTube's videos.

A pure HTML / CSS / JavaScript browser app inspired by classic Winamp, but the
audio source is any YouTube playlist instead of local MP3s. Comes with
draggable windows, three skins, ten EQ bands, seven Winamp-style visualizers
(including a MilkDrop-inspired psychedelic mode with saveable presets), an
"insane mode" that slowly cranks visualizer intensity, fake internet-radio DJ
mode, CRT/glitch overlays, keyboard shortcuts, and easter eggs.

No build step, no framework, no npm. Drop the folder on disk, open
`index.html`, and you're in.

---

## Running locally

The YouTube IFrame Player works best when served over `http://` rather than
`file://` (some browsers refuse to deliver the IFrame on `file://`).

**Easiest – Python 3:**

```bash
cd winamp-yt
python3 -m http.server 8080
# then open http://localhost:8080
```

**Node:**

```bash
npx http-server winamp-yt -p 8080
# or:
npx serve winamp-yt
```

**No server (works in most desktop browsers anyway):** double-click
`index.html`. If you get an "embedded video blocked" error, switch to a local
HTTP server using one of the methods above.

---

## How to load a YouTube playlist

1. Click the `PL` button on the main window (or press `P`) to open the
   playlist window.
2. Paste any of the following into the input box and press `LOAD`:
   - a full playlist URL — `https://www.youtube.com/playlist?list=PLxxxxx`
   - a watch URL that contains `&list=PLxxxxx`
   - a bare playlist id — `PLxxxxxxxxxxxxxxxxxxxxxx`
   - any list of video URLs or 11-character video IDs separated by commas,
     spaces or newlines (handy if you don't have a real playlist).
3. Press `▶` (or `X` on the keyboard).

The loaded playlist persists in `localStorage`, so it'll be there next time
you open the app.

> **Why does the title sometimes say "YouTube • abc123" before resolving?**
> We don't ship a YouTube Data API key, so we don't pre-fetch titles. The
> IFrame Player reports the title of the currently playing video once it
> starts — we back-fill the row at that point.

---

## Keyboard shortcuts

| Key | Action                              |
|-----|-------------------------------------|
| `Z` | previous track                       |
| `X` | play                                 |
| `C` | pause                                |
| `V` | stop                                 |
| `B` | next track                           |
| `S` | toggle shuffle                       |
| `R` | cycle repeat (none → all → one)      |
| `M` | mute / unmute                        |
| `L` | focus the playlist input             |
| `P` | toggle playlist window               |
| `E` | toggle equalizer window              |
| `F` | fullscreen visualizer                |
| `[` `]` or `Shift+←` `Shift+→` | previous / next visualizer / MilkDrop preset |
| `T` | toggle CRT scanlines                 |
| `G` | toggle glitch overlay                |
| `+` `-` | volume up / down                  |

### Hidden cheat codes

Just type the letters (no modifier keys needed):

- `rave`    – strobing rave mode overlay
- `insane`  – visualizer intensity rises forever
- `crt`     – CRT scanlines
- `y2k`     – switch to Y2K Hacker skin
- `vhs`     – switch to Skate VHS skin
- `retro` / `classic` – back to Classic Green LCD
- `llama`   – the original Winamp easter egg

---

## Visualizers

Seven built-in visualizers, each ported from the classic Winamp idiom:

1. **Spectrum Analyzer** – gradient bars with peak-hold caps.
2. **Oscilloscope** – mirrored dual-trace with beat ring.
3. **Stereo VU Meters** – 24-cell LED meters, beat-aware peak markers.
4. **Frequency Tunnel** – concentric rings driven by FFT bands.
5. **Particle Field** – beat bursts, bass attractor, treble jitter.
6. **Rotating Waveform Tunnel** – two counter-rotating waveform rings.
7. **MilkDrop (psychedelic)** – formula-driven preset engine with radial
   symmetry, kaleidoscope mirroring, audio-warped feedback trails, and beat
   flashes. Six presets ship; you can save your own.

### Why are visualizers simulated?

The YouTube IFrame Player runs in a cross-origin sandbox. Browsers do not
let you attach a Web Audio `AnalyserNode` to its output (the `<iframe>` and
the parent page can't share an `AudioContext`). So we synthesise plausible
FFT + waveform data from playback state and the track's hashed BPM, with
beat detection, bass/mid/treble pulses, and treble sparkle.

If you ever do get a real `AnalyserNode` (e.g. you swap in an `<audio>`
element with a CORS-friendly source), call:

```js
WY.audio.attachAnalyser(myAnalyserNode);
```

and every visualizer will switch to real data automatically.

### Add a new visualizer

1. Create `js/visualizers/myviz.js`:

   ```js
   WY.viz = WY.viz || {};
   WY.viz.myviz = {
     name: "My Visualizer",
     // ctx: 2D context. w/h: pixel size (DPR-fitted).
     // frame: { fft, wave, bass, mid, treble, energy, beat, beatStrength, bpm }
     // pal: { bg, fg, fg2, fg3 }   – skin's visualizer palette
     draw(ctx, w, h, frame, pal) {
       ctx.fillStyle = pal.bg; ctx.fillRect(0, 0, w, h);
       // ...draw stuff...
     }
   };
   ```

2. Add `<script src="js/visualizers/myviz.js"></script>` to `index.html`
   above `visualizers/index.js`.
3. Push it into the `LIST` array at the top of `js/visualizers/index.js`.

### Add a new MilkDrop preset

You can either:

- save one from the UI (switch to MilkDrop, tweak with `[`/`]`, press `SAVE`),
  or
- add it programmatically in `js/visualizers/milkdrop.js` by pushing onto
  the `BUILTIN` array. Each preset is:

  ```js
  { name, sym, rot, zoom, driftX, driftY, blobs, hueSpd,
    flash, kaleido, waveAmp, trail, bg }
  ```

  See the existing presets for value ranges.

---

## Skins

Three built-in skins:

- **Classic Green LCD** – Winamp 2.x meets Game Boy.
- **Y2K Hacker** – magenta-on-purple chrome.
- **Skate VHS** – faded cassette tape vibes.

Skins are pure CSS files that override a small set of CSS custom properties
on `body.skin-<name>`. To add one:

1. Copy `css/skins/classic.css` to `css/skins/mine.css` and edit the values.
2. Add `<link rel="stylesheet" href="css/skins/mine.css">` to `index.html`.
3. Add `<option value="mine">My Skin</option>` to the `<select id="skin-select">`.
4. Append `"mine"` to the `SKINS` array in `js/skins.js`.

All visualizers automatically use the skin's `--vis-bg / --vis-fg / --vis-fg2 / --vis-fg3`
palette.

---

## Equalizer

The equalizer is **visual-only**. Same cross-origin limitation as the
visualizer: we can't actually filter the IFrame's audio output. The EQ
window still gives you:

- 10 bands + preamp slider
- 8 presets (Flat, Rock, Hip-Hop, Punk, Bass Boost, Treble Boost, Vocal, Dance)
- bass-boost toggle
- live response-curve display that animates as you drag

Slider state persists.

---

## Project layout

```
winamp-yt/
├── index.html
├── README.md
├── css/
│   ├── main.css
│   └── skins/
│       ├── classic.css
│       ├── y2k.css
│       └── vhs.css
└── js/
    ├── util.js              – tiny helpers (clamp/lerp/bus/toast/...)
    ├── storage.js           – localStorage wrapper
    ├── skins.js             – skin switching
    ├── effects.js           – CRT/glitch/rave/radio/easter-eggs/boot
    ├── audio-sim.js         – simulated FFT + waveform analyzer
    ├── player.js            – YouTube IFrame Player wrapper
    ├── playlist.js          – parsing, persistence, render
    ├── equalizer.js         – visual EQ + curve display
    ├── ui.js                – window manager + keyboard shortcuts
    ├── app.js               – wires transport buttons, boots everything
    └── visualizers/
        ├── index.js          – render loop + preset switching
        ├── spectrum.js
        ├── oscilloscope.js
        ├── vumeter.js
        ├── tunnel.js
        ├── particles.js
        ├── waveformTunnel.js
        └── milkdrop.js
```

---

## Known limits / notes

- Audio analysis is **simulated**, not real (cross-origin sandbox — see above).
  Visualizers still react to play/pause/seek/track changes via synthesised BPM,
  bass/mid/treble pulses, and beat events.
- The equalizer is visual-only.
- Some videos are not embeddable (uploader has disabled embedding, or the
  video is region-locked / age-gated). The player emits an error and
  auto-skips to the next track.
- The first interaction (a click on PLAY) is required by browser autoplay
  policies — that's normal.

Enjoy. It really whips the YouTube's videos.
