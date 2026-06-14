using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GKMC
{
    /// <summary>
    /// Entry point and conductor for the whole experience. Builds the twelve worlds, the player,
    /// the audio, the helicopters and the easter eggs, then continuously morphs the atmosphere
    /// (fog, ambient, sky, sun) toward the world you're standing in. Auto-boots in any scene.
    /// </summary>
    public class GKMCExperience : MonoBehaviour
    {
        public static GKMCExperience Instance { get; private set; }
        public static bool DisableAutoBoot = false;

        [Tooltip("Set false to keep procedural primitives even when Meshy models are present.")]
        public bool useModels = true;
        public float atmosphereLerp = 1.4f;

        public PlayerController Player { get; private set; }
        public Camera PlayerCamera => Player != null ? Player.Cam : null;
        public TourUI UI { get; private set; }
        public YouTubeAudioManager Audio { get; private set; }

        List<TrackInfo> _tracks;
        Light _sun;
        int _currentWorld = 0;

        // Atmosphere state (lerped toward the active world).
        Color _fog, _ambient, _sky, _sunColor = Color.white;
        float _fogDensity, _sunIntensity;
        Color _tFog, _tAmbient, _tSky, _tSunColor = Color.white;
        float _tFogDensity, _tSunIntensity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (DisableAutoBoot) return;
            if (FindAnyObjectByType<GKMCExperience>() != null) return;
            var go = new GameObject("GKMC_Experience");
            go.AddComponent<GKMCExperience>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Build();
        }

        void Build()
        {
            ModelLibrary.ModelsEnabled = useModels;

            // Make our player the only active camera / listener in the scene.
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsInactive.Include)) cam.enabled = false;
            foreach (var al in FindObjectsByType<AudioListener>(FindObjectsInactive.Include)) al.enabled = false;

            _tracks = AlbumData.BuildTracks();

            var worldsRoot = new GameObject("Worlds").transform;
            worldsRoot.SetParent(transform, false);
            WorldBuilder.BuildAll(_tracks, worldsRoot);

            // Sun / key directional light.
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(transform, false);
            sunGo.transform.localEulerAngles = new Vector3(55f, -35f, 0f);
            _sun = sunGo.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.shadows = LightShadows.Soft;

            // Player at the entrance of world 1, facing down the tour.
            Player = PlayerController.Create(transform, new Vector3(0f, 1.2f, -AlbumData.WorldLength * 0.5f + 4f));
            Player.transform.localEulerAngles = new Vector3(0f, 0f, 0f);

            Audio = YouTubeAudioManager.Create(transform, _tracks);
            UI = TourUI.Create(transform);
            HelicopterManager.Create(transform, _tracks);
            EasterEggManager.Create(transform, _tracks);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.ambientMode = AmbientMode.Flat;

            // Initialise atmosphere from track 1 with no lerp.
            SetTargets(_tracks[0]);
            _fog = _tFog; _ambient = _tAmbient; _sky = _tSky;
            _sunColor = _tSunColor; _fogDensity = _tFogDensity; _sunIntensity = _tSunIntensity;
            ApplyAtmosphere();

            EnterWorld(1);
        }

        public void EnterWorld(int number)
        {
            if (number == _currentWorld) return;
            _currentWorld = number;
            var ti = _tracks.Find(x => x.number == number);
            if (ti == null) return;

            SetTargets(ti);
            Audio.PlayTrack(number);
            UI.ShowWorld(ti, Audio.LastTrackHadAudio);
        }

        void SetTargets(TrackInfo ti)
        {
            _tFog = ti.fog;
            _tAmbient = ti.ambient;
            _tSky = ti.sky;
            _tFogDensity = ti.fogDensity;
            _tSunIntensity = ti.sun;
            _tSunColor = Color.Lerp(ti.accent, Color.white, 0.6f);
        }

        void Update()
        {
            float k = Time.deltaTime * atmosphereLerp;
            _fog = Color.Lerp(_fog, _tFog, k);
            _ambient = Color.Lerp(_ambient, _tAmbient, k);
            _sky = Color.Lerp(_sky, _tSky, k);
            _sunColor = Color.Lerp(_sunColor, _tSunColor, k);
            _fogDensity = Mathf.Lerp(_fogDensity, _tFogDensity, k);
            _sunIntensity = Mathf.Lerp(_sunIntensity, _tSunIntensity, k);
            ApplyAtmosphere();

            // Audio loaded asynchronously — refresh the "now playing" note once it arrives.
            if (Audio != null && UI != null && _currentWorld > 0)
            {
                var ti = _tracks.Find(x => x.number == _currentWorld);
                if (ti != null) UI.SetNowPlaying(ti, Audio.LastTrackHadAudio);
            }
        }

        void ApplyAtmosphere()
        {
            RenderSettings.fogColor = _fog;
            RenderSettings.fogDensity = _fogDensity;
            RenderSettings.ambientLight = _ambient;
            if (_sun != null) { _sun.color = _sunColor; _sun.intensity = _sunIntensity; }
            if (PlayerCamera != null) PlayerCamera.backgroundColor = _sky;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
