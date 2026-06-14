using System.Collections.Generic;
using UnityEngine;

namespace GKMC
{
    /// <summary>
    /// Police/news choppers patrolling the sky over the whole tour — a recurring Kendrick motif
    /// (the "Alright" video, the surveillance of "good kid" / "m.A.A.d city"). Rotor sound is
    /// synthesised at runtime so they're audible with zero audio files, and a searchlight sweeps
    /// the ground below. Helicopters cluster lower over the good kid / m.A.A.d city worlds.
    /// </summary>
    public class HelicopterManager : MonoBehaviour
    {
        public static HelicopterManager Create(Transform parent, List<TrackInfo> tracks)
        {
            var go = new GameObject("Helicopters");
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<HelicopterManager>();
            m.Spawn(tracks);
            return m;
        }

        void Spawn(List<TrackInfo> tracks)
        {
            float minZ = -AlbumData.WorldLength;
            float maxZ = (tracks.Count) * AlbumData.WorldLength;
            var rotor = SynthRotorClip();

            // High patrol choppers that cross the whole length of the tour.
            for (int i = 0; i < 4; i++)
            {
                var h = Helicopter.Create(transform, rotor);
                var mover = h.gameObject.AddComponent<HelicopterMover>();
                mover.mode = HelicopterMover.Mode.Cross;
                mover.minZ = minZ; mover.maxZ = maxZ;
                mover.altitude = Random.Range(34f, 44f);
                mover.xPos = Random.Range(-22f, 22f);
                mover.speed = Random.Range(10f, 16f) * (Random.value < 0.5f ? 1f : -1f);
                mover.Reset();
            }

            // Low patrol choppers that hover over good kid (7) and m.A.A.d city (8).
            float z7 = tracks[6].ZCenter, z8 = tracks[7].ZCenter;
            for (int i = 0; i < 2; i++)
            {
                var h = Helicopter.Create(transform, rotor, lowPatrol: true);
                var mover = h.gameObject.AddComponent<HelicopterMover>();
                mover.mode = HelicopterMover.Mode.Patrol;
                mover.minZ = z7 - 12f; mover.maxZ = z8 + 12f;
                mover.altitude = Random.Range(20f, 26f);
                mover.xPos = Random.Range(-10f, 10f);
                mover.speed = Random.Range(7f, 11f) * (i == 0 ? 1f : -1f);
                mover.Reset();
            }
        }

        /// <summary>Synthesise a seamless looping "wop-wop" rotor thrum (low rumble × blade-pass modulation).</summary>
        static AudioClip SynthRotorClip()
        {
            const int sr = 44100;
            const float dur = 2f;
            int n = (int)(sr * dur);
            var data = new float[n];
            const float chop = 18f; // blade-pass frequency
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float rumble = 0.5f * Mathf.Sin(2f * Mathf.PI * 30f * t)
                             + 0.35f * Mathf.Sin(2f * Mathf.PI * 60f * t)
                             + 0.2f * Mathf.Sin(2f * Mathf.PI * 90f * t);
                float noise = (Random.value * 2f - 1f) * 0.22f;
                float mod = 0.45f + 0.55f * Mathf.Sin(2f * Mathf.PI * chop * t);
                data[i] = (rumble + noise) * mod * 0.5f;
            }
            var clip = AudioClip.Create("RotorLoop", n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }

    /// <summary>A single chopper: body, boom, spinning rotors, skids, searchlight, blinking nav light, 3D rotor audio.</summary>
    public class Helicopter : MonoBehaviour
    {
        public static Helicopter Create(Transform parent, AudioClip rotorClip, bool lowPatrol = false)
        {
            var go = new GameObject("Helicopter");
            go.transform.SetParent(parent, false);
            var heli = go.AddComponent<Helicopter>();
            var t = go.transform;

            var dark = GKMCUtil.Mat(new Color(0.04f, 0.04f, 0.05f), 0.5f, 0.4f);
            var metal = GKMCUtil.Mat(new Color(0.2f, 0.2f, 0.22f), 0.8f, 0.5f);

            // Body (model-aware) + tail boom + tail fin.
            ModelLibrary.SpawnOrFallback("helicopter_body", t, Vector3.zero, Vector3.zero, a =>
            {
                GKMCUtil.Prim(PrimitiveType.Capsule, a, "Cabin", new Vector3(0f, 0f, 0.3f), new Vector3(1.6f, 1.4f, 2.4f), dark, false, new Vector3(90f, 0f, 0f));
                GKMCUtil.Box(a, "Boom", new Vector3(0f, 0.1f, -2.6f), new Vector3(0.3f, 0.3f, 3.2f), dark, false);
                GKMCUtil.Box(a, "TailFin", new Vector3(0f, 0.5f, -4f), new Vector3(0.1f, 1f, 0.6f), dark, false);
                GKMCUtil.Box(a, "SkidL", new Vector3(-0.8f, -0.9f, 0.2f), new Vector3(0.1f, 0.1f, 2.4f), metal, false);
                GKMCUtil.Box(a, "SkidR", new Vector3(0.8f, -0.9f, 0.2f), new Vector3(0.1f, 0.1f, 2.4f), metal, false);
            });

            // Main rotor (spins fast) — kept as separate primitives so it always animates.
            var mainHub = new GameObject("MainRotor");
            mainHub.transform.SetParent(t, false);
            mainHub.transform.localPosition = new Vector3(0f, 0.9f, 0.3f);
            GKMCUtil.Box(mainHub.transform, "BladeA", Vector3.zero, new Vector3(7f, 0.06f, 0.35f), metal, false);
            GKMCUtil.Box(mainHub.transform, "BladeB", Vector3.zero, new Vector3(0.35f, 0.06f, 7f), metal, false);
            var ms = mainHub.AddComponent<Spinner>(); ms.axis = Vector3.up; ms.degPerSec = 900f;

            // Tail rotor.
            var tailHub = new GameObject("TailRotor");
            tailHub.transform.SetParent(t, false);
            tailHub.transform.localPosition = new Vector3(0.15f, 0.4f, -4f);
            GKMCUtil.Box(tailHub.transform, "TailBlade", Vector3.zero, new Vector3(0.1f, 1.6f, 0.12f), metal, false);
            var ts = tailHub.AddComponent<Spinner>(); ts.axis = Vector3.right; ts.degPerSec = 1400f;

            // Searchlight pointing down, sweeping.
            var spot = GKMCUtil.SpotLight(t, "Searchlight", new Vector3(0f, -0.4f, 0.6f),
                new Vector3(90f, 0f, 0f), new Color(0.9f, 0.95f, 1f), lowPatrol ? 8f : 5f, lowPatrol ? 45f : 60f, 26f);
            var sweep = spot.gameObject.AddComponent<Sweeper>();
            sweep.axis = Vector3.right; sweep.range = lowPatrol ? 24f : 14f; sweep.speed = 0.8f;

            // Blinking red nav light.
            var navGo = GKMCUtil.Sphere(t, "NavLight", new Vector3(0f, -0.7f, -1f), Vector3.one * 0.18f,
                GKMCUtil.MatEmissive(Color.red, Color.red, 3f), false);
            var navLight = GKMCUtil.PointLight(navGo.transform, "NavBlink", Vector3.zero, Color.red, 2f, 6f);
            var fl = navGo.AddComponent<Flasher>(); fl.light = navLight; fl.a = Color.red; fl.b = Color.red; fl.speed = 3f; fl.minI = 0f; fl.maxI = 3f;

            // 3D rotor audio.
            var src = go.AddComponent<AudioSource>();
            src.clip = rotorClip;
            src.loop = true;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 6f;
            src.maxDistance = 80f;
            src.volume = lowPatrol ? 0.7f : 0.45f;
            src.dopplerLevel = 0.4f;
            src.Play();

            return heli;
        }
    }

    /// <summary>Flies a chopper along Z, facing its travel direction, with a gentle altitude bob.</summary>
    public class HelicopterMover : MonoBehaviour
    {
        public enum Mode { Cross, Patrol }
        public Mode mode = Mode.Cross;
        public float minZ, maxZ;
        public float altitude = 38f;
        public float xPos = 0f;
        public float speed = 12f; // sign sets direction
        float _bobSeed;

        public void Reset()
        {
            _bobSeed = Random.value * 100f;
            float z = speed >= 0f ? minZ : maxZ;
            transform.position = new Vector3(xPos, altitude, z);
            FaceTravel();
        }

        void Update()
        {
            var p = transform.position;
            p.z += speed * Time.deltaTime;

            if (mode == Mode.Cross)
            {
                float margin = 8f;
                if (speed > 0f && p.z > maxZ + margin) { p.z = minZ - margin; xPos = Random.Range(-22f, 22f); }
                else if (speed < 0f && p.z < minZ - margin) { p.z = maxZ + margin; xPos = Random.Range(-22f, 22f); }
            }
            else // Patrol: ping-pong between bounds.
            {
                if (p.z > maxZ) { p.z = maxZ; speed = -Mathf.Abs(speed); FaceTravel(); }
                else if (p.z < minZ) { p.z = minZ; speed = Mathf.Abs(speed); FaceTravel(); }
            }

            p.x = xPos;
            p.y = altitude + Mathf.Sin(Time.time * 0.8f + _bobSeed) * 1.2f;
            transform.position = p;
        }

        void FaceTravel()
        {
            transform.rotation = Quaternion.Euler(0f, speed >= 0f ? 0f : 180f, 0f);
        }
    }
}
