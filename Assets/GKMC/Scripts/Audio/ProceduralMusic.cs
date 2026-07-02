using UnityEngine;

namespace GKMC
{
    /// <summary>
    /// Generates a short, seamless, looping musical bed per track entirely in code, so the tour
    /// always has mood-matched audio even without the (copyrighted) album files. Drop real
    /// StreamingAssets/Audio/NN.ogg files to hear the actual songs instead — those take priority.
    ///
    /// Each clip is a four-chord progression (tuned to the track's key/mood) over a soft
    /// kick/snare/hat beat. Every oscillator is locked to an exact whole number of cycles across
    /// the buffer and the chord crossfade windows are periodic over it, so the clip loops with
    /// no click. The approach mirrors the runtime-synthesised helicopter rotor.
    /// </summary>
    public static class ProceduralMusic
    {
        struct Mood
        {
            public float bpm;
            public float rootHz;
            public bool minor;
            public bool rhythmic;
            public float brightness; // 0..1 upper-harmonic content
            public Mood(float bpm, float rootHz, bool minor, bool rhythmic, float brightness)
            { this.bpm = bpm; this.rootHz = rootHz; this.minor = minor; this.rhythmic = rhythmic; this.brightness = brightness; }
        }

        static Mood MoodFor(int track)
        {
            switch (track)
            {
                case 1:  return new Mood(84f,  130.81f, true,  false, 0.25f); // Sherane — dusk tension
                case 2:  return new Mood(80f,  146.83f, true,  false, 0.35f); // Vibe — smoke
                case 3:  return new Mood(100f, 110.00f, true,  true,  0.70f); // Backseat — hype
                case 4:  return new Mood(92f,  123.47f, true,  true,  0.30f); // Peer pressure
                case 5:  return new Mood(86f,  196.00f, true,  true,  0.60f); // Money Trees — warm groove
                case 6:  return new Mood(88f,  220.00f, false, true,  0.55f); // Poetic Justice — love
                case 7:  return new Mood(80f,  130.81f, true,  false, 0.20f); // good kid — cold
                case 8:  return new Mood(108f,  98.00f, true,  true,  0.40f); // m.A.A.d city — heavy
                case 9:  return new Mood(75f,  146.83f, true,  true,  0.30f); // Swimming Pools — woozy
                case 10: return new Mood(70f,  110.00f, true,  false, 0.20f); // Sing About Me — solemn
                case 11: return new Mood(84f,  174.61f, false, true,  0.60f); // Real — warm
                case 12: return new Mood(92f,  130.81f, false, true,  0.80f); // Compton — triumphant
                default: return new Mood(84f,  130.81f, true,  false, 0.40f);
            }
        }

        public static AudioClip Generate(TrackInfo ti)
        {
            var m = MoodFor(ti.number);
            const int sr = 44100;
            float beatSec = 60f / m.bpm;
            const int beats = 8;                       // two 4/4 bars, one chord per half-bar
            float loopSec = beats * beatSec;
            int n = Mathf.CeilToInt(loopSec * sr);
            float f0 = sr / (float)n;                  // exact loop fundamental → seamless tones

            // Quantise every partial to a whole number of cycles across the buffer.
            float Q(float f) => Mathf.Max(1f, Mathf.Round(f / f0)) * f0;
            float Semi(int semitones) => m.rootHz * Mathf.Pow(2f, semitones / 12f);

            // A real progression instead of one static chord: i–VI–III–VII in minor keys,
            // I–V–vi–IV in major. Each chord = bass, root, third, fifth, octave.
            int[] roots = m.minor ? new[] { 0, 8, 3, 10 } : new[] { 0, 7, 9, 5 };
            bool[] isMinor = m.minor ? new[] { true, false, false, false }
                                     : new[] { false, false, true, false };
            const int chords = 4, voices = 5;
            var freq = new float[chords, voices];
            float[] gains = { 0.95f, 1.0f, 0.7f, 0.7f, 0.4f };  // first = sub-octave bass
            for (int c = 0; c < chords; c++)
            {
                float root = Semi(roots[c]);
                freq[c, 0] = Q(root * 0.5f);
                freq[c, 1] = Q(root);
                freq[c, 2] = Q(root * (isMinor[c] ? 1.1892f : 1.2599f));
                freq[c, 3] = Q(root * 1.4983f);
                freq[c, 4] = Q(root * 2f);
            }

            var rng = new System.Random(ti.number * 7919 + 17);
            const float twoPi = Mathf.PI * 2f;
            float chordSec = loopSec / chords;
            float fade = Mathf.Min(0.16f, chordSec * 0.25f); // chord crossfade time

            var data = new float[n];
            for (int s = 0; s < n; s++)
            {
                float t = s / (float)sr;

                // Chord pad. Raised-cosine chord windows overlap into an equal-sum crossfade and
                // wrap around the loop point, so chord 4 hands back to chord 1 without a seam.
                float pad = 0f;
                for (int c = 0; c < chords; c++)
                {
                    float local = t - c * chordSec;
                    if (local < 0f) local += loopSec;
                    if (local >= chordSec + fade) continue;    // this chord is silent here
                    float w = local < fade
                        ? 0.5f - 0.5f * Mathf.Cos(Mathf.PI * local / fade)
                        : local < chordSec ? 1f
                        : 0.5f + 0.5f * Mathf.Cos(Mathf.PI * (local - chordSec) / fade);

                    float sum = 0f;
                    for (int k = 0; k < voices; k++)
                    {
                        float ph = twoPi * freq[c, k] * t;
                        float voice = Mathf.Sin(ph);
                        if (k >= 2) voice += m.brightness * 0.18f * Mathf.Sin(3f * ph); // shimmer
                        sum += voice * gains[k];
                    }
                    pad += sum * w;
                }
                float swell = 0.8f + 0.2f * Mathf.Sin(twoPi * f0 * t); // gentle once-per-loop rise
                pad *= 0.10f * swell;

                // Percussion — every hit's envelope decays inside its own beat, so it loops clean.
                float perc = 0f;
                int beatIdx = (int)(t / beatSec) % beats;
                float localT = t - Mathf.Floor(t / beatSec) * beatSec;
                if (m.rhythmic)
                {
                    if (beatIdx % 2 == 0)   // kick on 1 & 3
                    {
                        float env = Mathf.Exp(-localT * 16f);
                        float pitch = Mathf.Lerp(120f, 48f, Mathf.Clamp01(localT * 12f));
                        perc += Mathf.Sin(twoPi * pitch * localT) * env * 0.5f;
                    }
                    else                    // snare-ish backbeat on 2 & 4
                    {
                        float env = Mathf.Exp(-localT * 22f);
                        perc += ((float)(rng.NextDouble() * 2.0 - 1.0) * 0.5f
                                 + Mathf.Sin(twoPi * 190f * localT) * 0.35f) * env * 0.4f;
                    }
                    float hatEnv = Mathf.Exp(-localT * 60f);
                    perc += (float)(rng.NextDouble() * 2.0 - 1.0) * hatEnv * 0.07f; // ticking hat
                }
                else if (beatIdx == 0)      // ambient worlds: one soft heartbeat per bar pair
                {
                    float env = Mathf.Exp(-localT * 10f);
                    perc += Mathf.Sin(twoPi * 55f * localT) * env * 0.3f;
                }

                float val = (float)System.Math.Tanh((pad + perc) * 1.15); // soft clip
                data[s] = val * 0.5f;
            }

            var clip = AudioClip.Create("GKMC_Music_" + ti.number, n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
