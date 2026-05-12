/* =========================================================
   audio-sim.js
   ----------------------------------------------------------
   Provides a per-frame "fake" audio analysis object that all
   visualizers consume:

     WY.audio.frame() => {
       fft:   Float32Array(128)   // 0..1 frequency bins, bass→treble
       wave:  Float32Array(256)   // -1..1 oscilloscope samples
       bass:  number 0..1
       mid:   number 0..1
       treble:number 0..1
       energy:number 0..1         // overall loudness proxy
       beat:  boolean             // true on detected beat
       beatStrength: number 0..1
       bpm:   number              // current synthetic tempo
       tSec:  number              // current track timestamp (seconds)
       playing: boolean
     }

   STRATEGY
   --------
   Because the YouTube IFrame Player API does not expose its
   audio output via Web Audio (sandboxed cross-origin <iframe>),
   we cannot run an AnalyserNode on it in 99% of browsers.

   So we synthesise a *plausible* frequency/waveform spectrum
   driven by:
     - playback state (playing/paused)
     - the current track position
     - a pseudo-random per-track tempo (derived from videoId hash)
     - small stochastic variation each frame
     - "insane mode" energy multiplier that climbs over time

   The visualizers don't know or care whether the data is real
   or simulated — they just consume the frame object. If you
   later wire up a real AnalyserNode (e.g. for an <audio> element
   playing a cached file), call WY.audio.attachAnalyser(node) and
   frame() will switch to real data automatically.
   ========================================================= */
(function (WY) {
  "use strict";

  const FFT_N   = 128;
  const WAVE_N  = 256;

  const state = {
    fft:    new Float32Array(FFT_N),
    wave:   new Float32Array(WAVE_N),
    bass: 0, mid: 0, treble: 0, energy: 0,
    beat: false, beatStrength: 0,
    bpm: 120,
    tSec: 0,
    playing: false,
    // smoothing buffers
    _smoothFFT:  new Float32Array(FFT_N),
    _lastBeat:   0,
    _bpmPhase:   0,
    _seed:       1,
    _insane:     1.0,
    _insaneOn:   false,
    _t0:         performance.now() / 1000,
    _analyser:   null,
    _analyserBuf:null,
    _waveBuf:    null,
  };

  // Deterministic hash -> pseudo BPM for a given videoId
  function hashBpm(id) {
    if (!id) return 120;
    let h = 0;
    for (let i = 0; i < id.length; i++) h = (h * 31 + id.charCodeAt(i)) | 0;
    // 78..172 bpm, biased towards 90..140
    const v = Math.abs(h % 1000) / 1000;
    return Math.round(78 + Math.pow(v, 0.7) * 94);
  }

  // Cheap noise
  function noise(seed) {
    state._seed = (state._seed * 1664525 + 1013904223) | 0;
    return ((state._seed >>> 0) % 1000) / 1000;
  }

  WY.audio = {
    FFT_N, WAVE_N,

    setPlaying(b)     { state.playing = !!b; },
    setVideoId(id)    { state.bpm = hashBpm(id); },
    setBpm(bpm)       { state.bpm = bpm; },
    setTime(sec)      { state.tSec = sec || 0; },
    setInsane(on)     { state._insaneOn = !!on; if (!on) state._insane = 1.0; },
    isInsane()        { return state._insaneOn; },
    insaneLevel()     { return state._insane; },

    // Wire up a real AnalyserNode if you ever obtain one
    attachAnalyser(node) {
      state._analyser   = node;
      state._analyserBuf = new Uint8Array(node.frequencyBinCount);
      state._waveBuf     = new Uint8Array(node.fftSize);
    },
    detachAnalyser() {
      state._analyser = null;
    },

    // Called every animation frame by the visualizer host
    frame() {
      const now  = performance.now() / 1000;
      const t    = state.tSec;
      const live = state.playing;

      // ---- 1. drive the bpm phase + beat detection ----
      const bps = state.bpm / 60;
      // increment phase each frame by (dt * bps)
      const dt = Math.min(0.1, now - (state._lastNow || now));
      state._lastNow = now;
      if (live) state._bpmPhase += dt * bps;

      // a beat happens each time _bpmPhase crosses an integer
      const phaseFrac = state._bpmPhase - Math.floor(state._bpmPhase);
      const beatNow   = live && phaseFrac < 0.04;
      if (beatNow && now - state._lastBeat > 0.25) {
        state.beat = true;
        state.beatStrength = 0.7 + Math.random() * 0.3;
        state._lastBeat = now;
      } else {
        state.beat = false;
        state.beatStrength *= 0.86;
      }

      // insane mode slowly cranks intensity
      if (state._insaneOn && live) {
        state._insane = Math.min(2.6, state._insane + dt * 0.04);
      }

      // ---- 2. fill FFT bins ----
      // If a real analyser is attached, use it
      if (state._analyser) {
        state._analyser.getByteFrequencyData(state._analyserBuf);
        state._analyser.getByteTimeDomainData(state._waveBuf);
        const src   = state._analyserBuf;
        const wsrc  = state._waveBuf;
        const step  = src.length / FFT_N;
        for (let i = 0; i < FFT_N; i++) state.fft[i] = src[Math.floor(i*step)] / 255;
        const wstep = wsrc.length / WAVE_N;
        for (let i = 0; i < WAVE_N; i++) state.wave[i] = (wsrc[Math.floor(i*wstep)] - 128) / 128;
      } else {
        // SYNTHESISE: stack of sine-like envelopes driven by phase & noise.
        // Bass region (bins 0..16) — pulse on beat.
        const beatEnv = Math.max(0, 1 - (now - state._lastBeat) * 5);
        const bassPulse = (0.55 + 0.45 * beatEnv) * (live ? 1 : 0.15);

        // Mid region (16..56) — sustained, slow LFO
        const midLFO  = (0.45 + 0.35 * Math.sin(t * 1.1 + state._bpmPhase * 0.7))
                       * (live ? 1 : 0.1);

        // Treble (56..128) — sparkle, fast noisy LFO
        const trbLFO  = (0.30 + 0.25 * Math.sin(t * 3.4))
                       * (live ? 1 : 0.05);

        for (let i = 0; i < FFT_N; i++) {
          let v;
          if (i < 16) {
            // bass: falls off slightly with i
            v = bassPulse * (1 - i / 32) + noise(i) * 0.10;
          } else if (i < 56) {
            // mid: gentle hump
            const m = (i - 16) / 40;
            v = midLFO * (0.7 - Math.abs(m - 0.5)) + noise(i) * 0.12;
          } else {
            // treble: high-freq sparkle, scaled
            const trbNoise = noise(i);
            v = trbLFO * (0.6 + 0.4 * trbNoise) * (1 - (i - 56) / 90);
            // occasional sparkle spike
            if (trbNoise > 0.94) v += 0.4;
          }
          v *= state._insane;
          // smooth (attack fast, release slow)
          const prev = state._smoothFFT[i];
          const k = v > prev ? 0.55 : 0.18;
          state._smoothFFT[i] = WY.clamp(prev + (v - prev) * k, 0, 1);
          state.fft[i] = state._smoothFFT[i];
        }

        // SYNTHESISE waveform: sum of a couple of sines + noise
        const a1 = 0.45 * (live ? 1 : 0.15);
        const a2 = 0.25 * (live ? 1 : 0.10);
        const f1 = state.bpm / 60;            // bassy fundamental
        const f2 = f1 * 4.1;                  // mid harmonic
        const f3 = f1 * 11.3;                 // treble
        for (let i = 0; i < WAVE_N; i++) {
          const u = i / WAVE_N;
          const phase = state._bpmPhase * Math.PI * 2;
          state.wave[i] =
              a1 * Math.sin(u * Math.PI * 2 * 2 + phase) * (1 + beatEnv*0.6)
            + a2 * Math.sin(u * Math.PI * 2 * 6.7 + phase * 1.7)
            + 0.12 * Math.sin(u * Math.PI * 2 * 24 + phase * 3.1)
            + (Math.random() - 0.5) * 0.06;
        }
      }

      // ---- 3. summary bands ----
      let bSum = 0, mSum = 0, tSum = 0;
      for (let i = 0; i < 12;  i++) bSum += state.fft[i];
      for (let i = 12; i < 48; i++) mSum += state.fft[i];
      for (let i = 48; i < FFT_N; i++) tSum += state.fft[i];
      state.bass   = WY.clamp(bSum / 8,  0, 1);
      state.mid    = WY.clamp(mSum / 18, 0, 1);
      state.treble = WY.clamp(tSum / 36, 0, 1);
      state.energy = (state.bass + state.mid + state.treble) / 3;

      return state;
    }
  };

})(window.WY);
