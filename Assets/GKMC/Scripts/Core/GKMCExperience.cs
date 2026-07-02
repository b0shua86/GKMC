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

        // Shared environment (sky / clouds) morphed with the atmosphere.
        Material _skyMat;
        Material _cloudMat;

        // Atmosphere state (lerped toward the active world).
        Color _fog, _ambient, _sky, _sunColor = Color.white;
        float _fogDensity, _sunIntensity;
        Color _tFog, _tAmbient, _tSky, _tSunColor = Color.white;
        float _tFogDensity, _tSunIntensity;

        // Sky-dome state (lerped toward the active world).
        Color _skyTint, _groundCol;
        Color _tSkyTint, _tGroundCol;
        float _skyExposure = 1f, _tSkyExposure = 1f;

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
            sunGo.transform.localEulerAngles = new Vector3(48f, -35f, 0f);
            _sun = sunGo.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.shadows = LightShadows.Soft;

            // Outdoor environment: procedural sky (lit by the sun), drifting clouds, horizon ground.
            var envRoot = new GameObject("Environment").transform;
            envRoot.SetParent(transform, false);
            float minZ = -AlbumData.WorldLength;
            float maxZ = _tracks.Count * AlbumData.WorldLength;
            _skyMat = EnvironmentBuilder.BuildSky();
            if (_skyMat != null) RenderSettings.skybox = _skyMat;
            RenderSettings.sun = _sun;
            _cloudMat = EnvironmentBuilder.BuildClouds(envRoot, minZ, maxZ);
            EnvironmentBuilder.BuildGround(envRoot, new Color(0.11f, 0.11f, 0.12f));
            EnvironmentBuilder.BuildSkyline(envRoot, minZ, maxZ);

            // Player at the entrance of world 1, facing down the tour.
            Player = PlayerController.Create(transform, new Vector3(0f, 1.2f, -AlbumData.WorldLength * 0.5f + 4f));
            Player.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            VisualQuality.Apply(Player.Cam);

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
            _skyTint = _tSkyTint; _groundCol = _tGroundCol; _skyExposure = _tSkyExposure;
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
            UI.ShowWorld(ti, Audio.LastTrackHadAudio, Audio.LastTrackWasProcedural);
        }

        void SetTargets(TrackInfo ti)
        {
            _tFog = ti.fog;
            _tAmbient = ti.ambient;
            _tSky = ti.sky;
            // Thin the authored fog so each world is clear enough to walk and read at a glance.
            _tFogDensity = ti.fogDensity * 0.55f;
            _tSunIntensity = ti.sun;
            _tSunColor = Color.Lerp(ti.accent, Color.white, 0.6f);

            // Sky dome: tint toward the world's sky colour, darken the horizon haze, and let the
            // sun strength set the overall exposure (dusk/night worlds read dark, daylight bright).
            _tSkyTint = ti.sky;
            _tGroundCol = GKMCUtil.Dark(ti.fog, 0.8f);
            _tSkyExposure = Mathf.Clamp(0.35f + ti.sun * 0.85f, 0.32f, 1.35f);
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
            _skyTint = Color.Lerp(_skyTint, _tSkyTint, k);
            _groundCol = Color.Lerp(_groundCol, _tGroundCol, k);
            _skyExposure = Mathf.Lerp(_skyExposure, _tSkyExposure, k);
            ApplyAtmosphere();

            // Audio loaded asynchronously — refresh the "now playing" note once it arrives.
            if (Audio != null && UI != null && _currentWorld > 0)
            {
                var ti = _tracks.Find(x => x.number == _currentWorld);
                if (ti != null) UI.RefreshAudioStatus(ti, Audio.LastTrackHadAudio, Audio.LastTrackWasProcedural);
            }
        }

        void ApplyAtmosphere()
        {
            RenderSettings.fogColor = _fog;
            RenderSettings.fogDensity = _fogDensity;
            RenderSettings.ambientLight = _ambient;
            if (_sun != null) { _sun.color = _sunColor; _sun.intensity = _sunIntensity; }
            if (PlayerCamera != null) PlayerCamera.backgroundColor = _sky;

            if (_skyMat != null)
            {
                _skyMat.SetColor("_SkyTint", _skyTint);
                _skyMat.SetColor("_GroundColor", _groundCol);
                _skyMat.SetFloat("_Exposure", _skyExposure);
            }
            if (_cloudMat != null)
                _cloudMat.color = new Color(
                    Mathf.Lerp(1f, _skyTint.r, 0.4f),
                    Mathf.Lerp(1f, _skyTint.g, 0.4f),
                    Mathf.Lerp(1f, _skyTint.b, 0.4f), 1f);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
