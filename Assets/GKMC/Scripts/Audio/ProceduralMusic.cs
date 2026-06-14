using UnityEngine;

namespace GKMC
{
    /// <summary>
    /// Generates a short, seamless, looping musical bed per track entirely in code, so the tour
    /// always has mood-matched audio even without the (copyrighted) album files. Drop real
    /// StreamingAssets/Audio/NN.ogg files to hear the actual songs instead — those take priority.
    ///
    /// Each clip is a slow chord pad (tuned to the track's key/mood) over an optional soft beat.
    /// Every oscillator is locked to an exact whole number of cycles across the buffer, so the
    /// clip loops with no click. The approach mirrors the runtime-synthesised helicopter rotor.
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
            const int beats = 8;                       // two 4/4 bars
            float loopSec = beats * beatSec;
            int n = Mathf.CeilToInt(loopSec * sr);
            float f0 = sr / (float)n;                  // exact loop fundamental → seamless tones

            // Quantise every partial to a whole number of cycles across the buffer.
            float Q(float f) => Mathf.Max(1f, Mathf.Round(f / f0)) * f0;
            float r = m.rootHz;
            float third = m.minor ? r * 1.1892f : r * 1.2599f; // minor / major third
            float fifth = r * 1.4983f;
            float[] freqs = { Q(r * 0.5f), Q(r), Q(third), Q(fifth), Q(r * 2f) };
            float[] gains = { 0.9f, 1.0f, 0.7f, 0.7f, 0.45f };  // first = sub-octave bass
            float swellHz = f0;                        // one slow swell per loop

            var rng = new System.Random(ti.number * 7919 + 17);
            bool[] kickOn = new bool[beats];
            bool[] hatOn = new bool[beats];
            for (int b = 0; b < beats; b++)
            {
                kickOn[b] = m.rhythmic ? (b % 2 == 0) : (b == 0);
                hatOn[b] = m.rhythmic && (b % 2 == 1);
            }

            const float twoPi = Mathf.PI * 2f;
            var data = new float[n];
            for (int s = 0; s < n; s++)
            {
                float t = s / (float)sr;

                // Chord pad, gently swelling over the loop.
                float swell = 0.55f + 0.45f * Mathf.Sin(twoPi * swellHz * t);
                float pad = 0f;
                for (int k = 0; k < freqs.Length; k++)
                {
                    float w = twoPi * freqs[k] * t;
                    float voice = Mathf.Sin(w);
                    if (k >= 2) voice += m.brightness * 0.18f * Mathf.Sin(3f * w); // shimmer on upper voices
                    pad += voice * gains[k];
                }
                pad *= 0.10f * swell;

                // Percussion — repeats every beat and decays within it, so it loops seamlessly.
                float perc = 0f;
                int beatIdx = (int)(t / beatSec) % beats;
                float localT = t - Mathf.Floor(t / beatSec) * beatSec;
                if (kickOn[beatIdx])
                {
                    float env = Mathf.Exp(-localT * 16f);
                    float pitch = Mathf.Lerp(120f, 48f, Mathf.Clamp01(localT * 12f));
                    perc += Mathf.Sin(twoPi * pitch * localT) * env * 0.55f;
                }
                if (hatOn[beatIdx])
                {
                    float env = Mathf.Exp(-localT * 60f);
                    perc += (float)(rng.NextDouble() * 2.0 - 1.0) * env * 0.12f;
                }

                float val = (float)System.Math.Tanh((pad + perc) * 1.1); // soft clip
                data[s] = val * 0.5f;
            }

            var clip = AudioClip.Create("GKMC_Music_" + ti.number, n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
