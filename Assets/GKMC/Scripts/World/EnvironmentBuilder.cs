using UnityEngine;

namespace GKMC
{
    /// <summary>
    /// Builds the shared outdoor environment that wraps the whole tour: a procedural sky driven by
    /// the scene sun, a soft drifting cloud layer, and a ground plane stretching to the horizon.
    /// The sky / cloud tint and exposure are morphed per world by <see cref="GKMCExperience"/> so
    /// the heavens shift with each track's mood.
    /// </summary>
    public static class EnvironmentBuilder
    {
        /// <summary>A procedural skybox material (uses the scene's sun for the sun disk).</summary>
        public static Material BuildSky()
        {
            var sh = Shader.Find("Skybox/Procedural");
            if (sh == null) return null;
            var m = new Material(sh);
            m.SetFloat("_SunDisk", 2f);             // high-quality sun
            m.SetFloat("_SunSize", 0.04f);
            m.SetFloat("_SunSizeConvergence", 4f);
            m.SetFloat("_AtmosphereThickness", 1.0f);
            m.SetColor("_SkyTint", new Color(0.5f, 0.5f, 0.62f));
            m.SetColor("_GroundColor", new Color(0.18f, 0.18f, 0.2f));
            m.SetFloat("_Exposure", 1.0f);
            return m;
        }

        /// <summary>A slow, soft cloud layer high above the whole tour. Returns its material for tinting.</summary>
        public static Material BuildClouds(Transform parent, float minZ, float maxZ)
        {
            float midZ = (minZ + maxZ) * 0.5f;
            float lenZ = (maxZ - minZ) + 240f;

            var go = new GameObject("Clouds");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 62f, midZ);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();

            var main = ps.main;
            main.loop = true;
            main.duration = 80f;                    // prewarm fills one full duration...
            main.prewarm = true;                    // ...so the sky starts already full of clouds
            main.startLifetime = 80f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(30f, 78f);
            main.startColor = new Color(1f, 1f, 1f, 0.5f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 180;

            var emission = ps.emission;
            emission.rateOverTime = 180f / 80f;     // steady-state ~180 puffs

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(300f, 18f, lenZ);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(1.3f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            var sh = Shader.Find("Sprites/Default");
            var pm = new Material(sh) { mainTexture = SoftTexture() };
            renderer.material = pm;

            ps.Play();
            return pm;
        }

        /// <summary>
        /// Silhouette city blocks beyond the tour walls, two rows deep on each side, with sparse
        /// lit windows. Looking over the low walls now reads as rooftops and a skyline instead of
        /// an empty plain — the m.A.A.d city surrounds the whole walk.
        /// </summary>
        public static void BuildSkyline(Transform parent, float minZ, float maxZ)
        {
            var prev = Random.state;
            Random.InitState(20121022);   // album release date — the same city every run

            var root = new GameObject("Skyline").transform;
            root.SetParent(parent, false);

            var dark = GKMCUtil.Mat(new Color(0.035f, 0.035f, 0.045f), 0f, 0.05f);
            var winWarm = GKMCUtil.MatEmissive(new Color(1f, 0.8f, 0.45f), new Color(1f, 0.72f, 0.38f), 1.7f);
            var winCool = GKMCUtil.MatEmissive(new Color(0.62f, 0.78f, 1f), new Color(0.55f, 0.72f, 1f), 1.4f);

            foreach (float side in new[] { -1f, 1f })
            {
                // Near row: low-rise blocks just past the walls.
                for (float z = minZ - 60f; z < maxZ + 60f; z += Random.Range(11f, 19f))
                    Building(root, side * Random.Range(27f, 40f), z,
                        Random.Range(6f, 12f), Random.Range(4f, 10f), dark, winWarm, winCool);

                // Far row: taller towers for a layered horizon.
                for (float z = minZ - 80f; z < maxZ + 80f; z += Random.Range(16f, 27f))
                    Building(root, side * Random.Range(50f, 85f), z,
                        Random.Range(8f, 16f), Random.Range(9f, 22f), dark, winWarm, winCool);
            }

            Random.state = prev;
        }

        static void Building(Transform root, float x, float z, float w, float h,
            Material dark, Material winWarm, Material winCool)
        {
            float d = Random.Range(6f, 12f);
            GKMCUtil.Box(root, "Block", new Vector3(x, h * 0.5f - 0.3f, z), new Vector3(w, h, d), dark, false);

            // Sparse lit windows on the face toward the tour.
            int windows = Random.value < 0.55f ? Random.Range(1, 4) : 0;
            float face = x > 0f ? x - w * 0.5f - 0.06f : x + w * 0.5f + 0.06f;
            for (int i = 0; i < windows; i++)
                GKMCUtil.Box(root, "Window",
                    new Vector3(face, Random.Range(1.5f, h - 1f), z + Random.Range(-d * 0.35f, d * 0.35f)),
                    new Vector3(0.1f, Random.Range(0.5f, 1f), Random.Range(0.6f, 1.4f)),
                    Random.value < 0.7f ? winWarm : winCool, false);
        }

        /// <summary>A vast ground plane so the world meets a horizon instead of empty void.</summary>
        public static void BuildGround(Transform parent, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "GroundHorizon";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, -0.3f, 0f);
            go.transform.localScale = new Vector3(400f, 1f, 400f);   // 4 km across
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.GetComponent<Renderer>().sharedMaterial = GKMCUtil.Mat(color, 0f, 0.08f);
        }

        // A soft round alpha sprite used for fluffy cloud puffs.
        static Texture2D _soft;
        static Texture2D SoftTexture()
        {
            if (_soft != null) return _soft;
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[N * N];
            Vector2 c = new Vector2(N * 0.5f, N * 0.5f);
            float rad = N * 0.5f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / rad;
                    float a = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(d));
                    px[y * N + x] = new Color(1f, 1f, 1f, a * a);
                }
            tex.SetPixels(px);
            tex.Apply();
            _soft = tex;
            return tex;
        }
    }
}
