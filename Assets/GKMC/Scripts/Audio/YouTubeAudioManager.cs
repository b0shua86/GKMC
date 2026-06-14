using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace GKMC
{
    /// <summary>
    /// Plays the album track-by-track and cross-fades as you walk between worlds.
    ///
    /// Audio resolution order, per track:
    ///   1) A local file in StreamingAssets/Audio (e.g. 01.ogg) — most reliable, works in every build.
    ///   2) A configured stream-resolver URL (e.g. an Invidious instance) that returns a direct audio stream.
    ///   3) Nothing — the visual tour still runs and the UI shows the YouTube link to open manually.
    ///
    /// YouTube does not hand out a plain audio URL for a watch page, so direct in-engine streaming needs a
    /// resolver. See README for the two supported ways to wire real audio. Visuals never depend on audio.
    /// </summary>
    public class YouTubeAudioManager : MonoBehaviour
    {
        // Optional resolver template. {id} is replaced by the track's youtubeId.
        // Example (Invidious, audio-only itag 140): "https://yewtu.be/latest_version?id={id}&itag=140"
        // Leave empty to disable network streaming and rely on local files only.
        public string streamResolverTemplate = "";

        public float crossfadeSeconds = 2.0f;
        public float volume = 0.85f;

        List<TrackInfo> _tracks;
        AudioSource _a, _b;
        AudioSource _active;
        readonly Dictionary<int, AudioClip> _cache = new Dictionary<int, AudioClip>();
        int _currentTrack = -1;
        Coroutine _fade;

        public int CurrentTrack => _currentTrack;
        public bool LastTrackHadAudio { get; private set; }

        public static YouTubeAudioManager Create(Transform parent, List<TrackInfo> tracks)
        {
            var go = new GameObject("AudioManager");
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<YouTubeAudioManager>();
            m._tracks = tracks;
            m._a = go.AddComponent<AudioSource>();
            m._b = go.AddComponent<AudioSource>();
            foreach (var s in new[] { m._a, m._b })
            {
                s.playOnAwake = false;
                s.loop = true;
                s.spatialBlend = 0f;     // album plays as 2D narration over the whole tour
                s.volume = 0f;
            }
            m._active = m._a;
            m.LoadConfigOverrides();
            return m;
        }

        /// <summary>Optional StreamingAssets/gkmc_tracks.json lets the user set links/files without recompiling.</summary>
        void LoadConfigOverrides()
        {
            try
            {
                string path = Path.Combine(Application.streamingAssetsPath, "gkmc_tracks.json");
                if (!File.Exists(path)) return;
                var json = File.ReadAllText(path);
                var cfg = JsonUtility.FromJson<TrackConfigList>(json);
                if (cfg?.tracks == null) return;
                foreach (var c in cfg.tracks)
                {
                    var ti = _tracks.Find(x => x.number == c.number);
                    if (ti == null) continue;
                    if (!string.IsNullOrEmpty(c.youtubeUrl)) ti.youtubeUrl = c.youtubeUrl;
                    if (!string.IsNullOrEmpty(c.youtubeId)) ti.youtubeId = c.youtubeId;
                    if (!string.IsNullOrEmpty(c.audioFile)) ti.audioFile = c.audioFile;
                }
                if (!string.IsNullOrEmpty(cfg.streamResolverTemplate))
                    streamResolverTemplate = cfg.streamResolverTemplate;
                Debug.Log("[GKMC] Loaded audio overrides from gkmc_tracks.json");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[GKMC] Could not parse gkmc_tracks.json: " + e.Message);
            }
        }

        public void PlayTrack(int number)
        {
            if (number == _currentTrack) return;
            _currentTrack = number;
            var ti = _tracks.Find(x => x.number == number);
            if (ti == null) return;
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(LoadAndCrossfade(ti));
        }

        IEnumerator LoadAndCrossfade(TrackInfo ti)
        {
            AudioClip clip = null;
            if (_cache.TryGetValue(ti.number, out var cached))
            {
                clip = cached;
            }
            else
            {
                yield return StartCoroutine(Resolve(ti, c => clip = c));
                if (clip != null) _cache[ti.number] = clip;
            }

            LastTrackHadAudio = clip != null;
            if (clip == null)
            {
                // No audio available — fade out whatever was playing; visuals continue regardless.
                yield return StartCoroutine(Fade(_active, _active.volume, 0f));
                _active.Stop();
                yield break;
            }

            var next = _active == _a ? _b : _a;
            next.clip = clip;
            next.volume = 0f;
            next.Play();

            float t = 0f;
            float dur = Mathf.Max(0.01f, crossfadeSeconds);
            var from = _active;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                next.volume = Mathf.Lerp(0f, volume, k);
                from.volume = Mathf.Lerp(volume, 0f, k);
                yield return null;
            }
            next.volume = volume;
            from.Stop();
            _active = next;
        }

        IEnumerator Resolve(TrackInfo ti, System.Action<AudioClip> done)
        {
            // 1) Local StreamingAssets file (try the configured name, then common extensions).
            foreach (var candidate in LocalCandidates(ti))
            {
                bool ok = false;
                yield return StartCoroutine(TryLoadUrl(candidate, c => { if (c != null) { done(c); ok = true; } }));
                if (ok) yield break;
            }

            // 2) Network stream resolver (optional).
            if (!string.IsNullOrEmpty(streamResolverTemplate) && !string.IsNullOrEmpty(ti.youtubeId))
            {
                string url = streamResolverTemplate.Replace("{id}", ti.youtubeId);
                bool ok = false;
                yield return StartCoroutine(TryLoadUrl(url, c => { if (c != null) { done(c); ok = true; } }, AudioType.UNKNOWN));
                if (ok) yield break;
            }

            Debug.Log($"[GKMC] No playable audio for track {ti.number} \"{ti.title}\". " +
                      $"Drop a file at StreamingAssets/Audio/{ti.audioFile} or set a stream resolver. " +
                      $"YouTube link: {ti.youtubeUrl}");
            done(null);
        }

        IEnumerable<string> LocalCandidates(TrackInfo ti)
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Audio");
            string baseName = Path.GetFileNameWithoutExtension(ti.audioFile);
            foreach (var ext in new[] { ".ogg", ".mp3", ".wav" })
                yield return ToUri(Path.Combine(dir, baseName + ext));
        }

        static string ToUri(string path)
        {
            // On most platforms StreamingAssets is a real path that needs a file:// scheme; on
            // Android/WebGL it is already a URL.
            if (path.Contains("://")) return path;
            return "file://" + path;
        }

        IEnumerator TryLoadUrl(string url, System.Action<AudioClip> done, AudioType type = AudioType.OGGVORBIS)
        {
            AudioType at = type;
            if (type == AudioType.OGGVORBIS)
            {
                if (url.EndsWith(".mp3")) at = AudioType.MPEG;
                else if (url.EndsWith(".wav")) at = AudioType.WAV;
                else at = AudioType.OGGVORBIS;
            }

            using (var req = UnityWebRequestMultimedia.GetAudioClip(url, at))
            {
                ((DownloadHandlerAudioClip)req.downloadHandler).streamAudio = true;
                yield return req.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
                bool failed = req.result != UnityWebRequest.Result.Success;
#else
                bool failed = req.isNetworkError || req.isHttpError;
#endif
                if (failed) { done(null); yield break; }
                var clip = DownloadHandlerAudioClip.GetContent(req);
                done(clip);
            }
        }

        IEnumerator Fade(AudioSource s, float from, float to)
        {
            float t = 0f, dur = Mathf.Max(0.01f, crossfadeSeconds);
            while (t < dur) { t += Time.deltaTime; s.volume = Mathf.Lerp(from, to, t / dur); yield return null; }
            s.volume = to;
        }

        [System.Serializable] public class TrackConfig
        {
            public int number; public string youtubeUrl; public string youtubeId; public string audioFile;
        }
        [System.Serializable] public class TrackConfigList
        {
            public string streamResolverTemplate; public TrackConfig[] tracks;
        }
    }
}
