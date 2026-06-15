using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GKMC
{
    /// <summary>
    /// A non-destructive art pass layered on top of the generated worlds.
    /// It keeps the code-only project design, but adds the small visual details that make a
    /// procedural blockout read like an intentional walkable place.
    /// </summary>
    public static class VisualPolish
    {
        static float WL => AlbumData.WorldLength;
        static float WW => AlbumData.WorldWidth;
        static float WH => AlbumData.WallHeight;
        static float HalfW => WW * 0.5f;
        static float HalfL => WL * 0.5f;

        public static void Apply(List<TrackInfo> tracks, Transform worldsRoot)
        {
            if (tracks == null || worldsRoot == null) return;

            ConfigureRenderQuality();
            AddTourContinuity(worldsRoot, tracks);

            var previousRandom = Random.state;
            foreach (var ti in tracks)
            {
                Transform w = FindWorld(worldsRoot, ti.number);
                if (w == null) continue;

                Random.InitState(90210 + ti.number * 997);
                AddSharedWorldPolish(w, ti);
                AddTrackSpecificPolish(w, ti);
            }
            Random.state = previousRandom;
        }

        static void ConfigureRenderQuality()
        {
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 90f);
            QualitySettings.antiAliasing = Mathf.Max(QualitySettings.antiAliasing, 4);

            RenderSettings.reflectionIntensity = 0.28f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = Mathf.Max(RenderSettings.defaultReflectionResolution, 128);
        }

        static Transform FindWorld(Transform root, int number)
        {
            string prefix = $"World_{number:00}_";
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name.StartsWith(prefix)) return child;
            }
            return null;
        }

        static void AddTourContinuity(Transform root, List<TrackInfo> tracks)
        {
            if (tracks.Count == 0) return;

            Material railMat = GKMCUtil.Mat(new Color(0.035f, 0.035f, 0.04f), 0.15f, 0.35f);
            Material glowMat = GKMCUtil.MatEmissive(new Color(0.25f, 0.26f, 0.32f), new Color(0.45f, 0.48f, 0.65f), 0.22f);
            float startZ = -HalfL - 2f;
            float endZ = tracks[tracks.Count - 1].ZCenter + HalfL + 2f;
            float len = endZ - startZ;
            float mid = (startZ + endZ) * 0.5f;

            GKMCUtil.Box(root, "TourRail_L", new Vector3(-HalfW + 0.55f, 0.8f, mid), new Vector3(0.16f, 0.22f, len), railMat, false);
            GKMCUtil.Box(root, "TourRail_R", new Vector3(HalfW - 0.55f, 0.8f, mid), new Vector3(0.16f, 0.22f, len), railMat, false);
            GKMCUtil.Box(root, "OverheadSpine", new Vector3(0f, WH + 1.1f, mid), new Vector3(0.18f, 0.18f, len), glowMat, false);

            foreach (TrackInfo ti in tracks)
            {
                float z = ti.ZCenter - HalfL;
                Material gateGlow = GKMCUtil.MatEmissive(ti.accent, ti.accent, 0.65f);
                GKMCUtil.Box(root, $"WorldWash_{ti.number:00}", new Vector3(0f, 0.075f, z + 1.5f), new Vector3(WW - 5f, 0.035f, 0.16f), gateGlow, false);
            }
        }

        static void AddSharedWorldPolish(Transform w, TrackInfo ti)
        {
            Material accent = GKMCUtil.MatEmissive(ti.accent, ti.accent, 0.7f);
            Material dimAccent = GKMCUtil.MatEmissive(GKMCUtil.Dark(ti.accent, 0.65f), ti.accent, 0.22f);
            Material dark = GKMCUtil.Mat(Color.Lerp(ti.primary, Color.black, 0.62f), 0.05f, 0.28f);
            Material wallPaint = GKMCUtil.Mat(Color.Lerp(ti.secondary, Color.black, 0.36f), 0.05f, 0.18f);
            Material floorScuff = GKMCUtil.MatTransparent(new Color(0f, 0f, 0f, 0.24f));

            for (int side = -1; side <= 1; side += 2)
            {
                GKMCUtil.Box(w, "PathEdgeGlow", new Vector3(side * 4.75f, 0.08f, 0f), new Vector3(0.08f, 0.045f, WL - 5f), dimAccent, false);
                GKMCUtil.Box(w, "InnerCurb", new Vector3(side * 5.05f, 0.22f, 0f), new Vector3(0.12f, 0.22f, WL - 5f), dark, false);
            }

            for (int i = 0; i < 6; i++)
            {
                float z = -HalfL + 5f + i * 8.7f;
                float panelH = Random.Range(3.0f, 5.5f);
                float panelW = Random.Range(4.8f, 8.0f);
                for (int side = -1; side <= 1; side += 2)
                {
                    Color col = Color.Lerp(ti.secondary, ti.accent, Random.Range(0.1f, 0.45f));
                    Material panelMat = GKMCUtil.Mat(GKMCUtil.Dark(col, Random.Range(0.55f, 0.9f)), 0.05f, 0.25f);
                    GKMCUtil.Box(w, "MuralPanel", new Vector3(side * (HalfW - 0.22f), 2.6f + panelH * 0.12f, z + Random.Range(-1.2f, 1.2f)),
                        new Vector3(0.08f, panelH, panelW), panelMat, false);
                }
            }

            WallWord(w, "ThemeLeft", ti.theme.ToUpperInvariant(), -HalfW + 0.34f, 5.35f, -12f, ti.accent, 0.34f, true);
            WallWord(w, "ThemeRight", $"{ti.number:00}", HalfW - 0.34f, 5.25f, 12f, ti.accent, 0.55f, false);

            for (int i = -2; i <= 2; i++)
            {
                float z = i * 10.3f;
                GKMCUtil.Box(w, "OverheadRib", new Vector3(0f, WH + 0.45f, z), new Vector3(WW - 2.2f, 0.12f, 0.16f), accent, false);
                GKMCUtil.Box(w, "RibDropL", new Vector3(-HalfW + 1.25f, WH * 0.55f, z), new Vector3(0.12f, WH * 0.9f, 0.12f), wallPaint, false);
                GKMCUtil.Box(w, "RibDropR", new Vector3(HalfW - 1.25f, WH * 0.55f, z), new Vector3(0.12f, WH * 0.9f, 0.12f), wallPaint, false);
            }

            for (int i = 0; i < 32; i++)
            {
                bool crack = i % 3 != 0;
                Vector3 scale = crack
                    ? new Vector3(Random.Range(0.5f, 2.4f), 0.014f, Random.Range(0.035f, 0.09f))
                    : new Vector3(Random.Range(0.18f, 0.55f), 0.012f, Random.Range(0.18f, 0.55f));
                Material mat = crack ? floorScuff : GKMCUtil.Mat(Color.Lerp(ti.accent, Color.white, 0.55f), 0f, 0.12f);
                GKMCUtil.Box(w, crack ? "FloorCrack" : "PaperScrap",
                    new Vector3(Random.Range(-HalfW + 2f, HalfW - 2f), 0.075f, Random.Range(-HalfL + 3f, HalfL - 3f)),
                    scale, mat, false, new Vector3(0f, Random.Range(0f, 180f), 0f));
            }

            for (int i = 0; i < 3; i++)
            {
                float z = -17f + i * 17f;
                Light l = GKMCUtil.PointLight(w, "PracticalPool", new Vector3(Random.Range(-8f, 8f), 2.2f, z), ti.accent, 0.38f, 11f);
                l.renderMode = LightRenderMode.ForcePixel;
            }
        }

        static void AddTrackSpecificPolish(Transform w, TrackInfo ti)
        {
            switch (ti.number)
            {
                case 1:
                    AddUtilityWires(w, -14f, 13f, -22f, 22f, 5);
                    Headlights(w, new Vector3(-6f, 0.85f, -2.5f), 8f, new Color(1f, 0.78f, 0.45f));
                    WallWord(w, "TrapText", "THE BLOCK WATCHES", HalfW - 0.32f, 4.5f, 18f, ti.accent, 0.27f, false);
                    GKMCUtil.Particles(w, "DuskDust", new Vector3(0f, 1.2f, 0f), new Color(1f, 0.55f, 0.3f, 0.18f), 8f, 0.35f, 6f, new Vector3(WW - 6f, 2f, WL - 6f), new Vector3(0.15f, 0.08f, 0f));
                    break;
                case 2:
                    AddCanopyLights(w, ti, -19f, 19f, 5);
                    for (int i = 0; i < 11; i++)
                    {
                        float a = i / 11f * Mathf.PI * 2f;
                        CandleGlowOnly(w, new Vector3(Mathf.Cos(a) * 7.5f, 1.1f, 6f + Mathf.Sin(a) * 7.5f), ti.accent);
                    }
                    WallWord(w, "Breathe", "BREATHE", -HalfW + 0.32f, 4.7f, 2f, ti.accent, 0.45f, true);
                    break;
                case 3:
                    AddMirrorShards(w, ti);
                    GKMCUtil.SpotLight(w, "GoldStageLeft", new Vector3(-10f, 8f, -8f), new Vector3(63f, 35f, 0f), ti.accent, 5f, 30f, 34f);
                    GKMCUtil.SpotLight(w, "GoldStageRight", new Vector3(10f, 8f, -8f), new Vector3(63f, -35f, 0f), ti.accent, 5f, 30f, 34f);
                    break;
                case 4:
                    AddPressureArrows(w, ti);
                    AddUtilityWires(w, -12.5f, -8f, -22f, 22f, 4);
                    WallWord(w, "NotYou", "NOT YOUR HOUSE", HalfW - 0.34f, 4.3f, 19f, ti.accent, 0.3f, false);
                    break;
                case 5:
                    AddSunbeams(w);
                    AddCanopyLights(w, ti, -17f, 17f, 4);
                    WallWord(w, "BackyardDream", "BACKYARD DREAM", -HalfW + 0.34f, 4.7f, 16f, ti.accent, 0.3f, true);
                    break;
                case 6:
                    AddCanopyLights(w, ti, -16f, 16f, 6);
                    AddRosePetals(w);
                    WallWord(w, "SoftFocus", "SOFT FOCUS", HalfW - 0.34f, 4.6f, -12f, ti.accent, 0.34f, false);
                    break;
                case 7:
                    AddScannerLines(w, ti);
                    WallWord(w, "Watched", "SURVEILLANCE", -HalfW + 0.34f, 5.3f, -6f, ti.accent, 0.32f, true);
                    break;
                case 8:
                    AddPoliceTape(w);
                    GKMCUtil.Particles(w, "SmokeColumns", new Vector3(0f, 1f, 2f), new Color(0.12f, 0.08f, 0.07f, 0.5f), 18f, 2.2f, 5f, new Vector3(WW - 6f, 1f, WL - 8f), new Vector3(0f, 0.7f, 0f));
                    WallWord(w, "Heat", "HEAT / SIRENS / GLASS", HalfW - 0.34f, 5f, 0f, ti.accent, 0.25f, false);
                    break;
                case 9:
                    AddCaustics(w, ti);
                    GKMCUtil.Particles(w, "MistOverPool", new Vector3(0f, 1.7f, 2f), new Color(0.45f, 0.75f, 1f, 0.25f), 16f, 1.3f, 5f, new Vector3(18f, 0.2f, 28f), new Vector3(0.12f, 0.22f, 0f));
                    break;
                case 10:
                    AddMemoryTiles(w);
                    GKMCUtil.Particles(w, "DustToLight", new Vector3(0f, 1f, 4f), new Color(1f, 0.85f, 0.55f, 0.36f), 18f, 0.24f, 5f, new Vector3(WW - 8f, 1f, WL - 12f), new Vector3(0f, 0.85f, 0f));
                    WallWord(w, "RememberUs", "REMEMBER US", -HalfW + 0.34f, 4.9f, -2f, ti.accent, 0.34f, true);
                    break;
                case 11:
                    AddHeartRings(w, ti);
                    WallWord(w, "SelfLove", "SELF / FAMILY / TRUTH", HalfW - 0.34f, 4.8f, 4f, ti.accent, 0.26f, false);
                    break;
                case 12:
                    AddUtilityWires(w, 15.5f, 14f, -23f, 23f, 6);
                    AddCanopyLights(w, ti, -20f, 20f, 5);
                    AddHeatShimmer(w);
                    WallWord(w, "VictoryLap", "VICTORY LAP", -HalfW + 0.34f, 5.1f, 9f, ti.accent, 0.35f, true);
                    break;
            }
        }

        static void AddCanopyLights(Transform w, TrackInfo ti, float z0, float z1, int count)
        {
            Material wire = GKMCUtil.Mat(new Color(0.025f, 0.025f, 0.027f), 0.2f, 0.25f);
            Material bulb = GKMCUtil.MatEmissive(ti.accent, ti.accent, 1.4f);
            for (int i = 0; i < count; i++)
            {
                float z = Mathf.Lerp(z0, z1, count == 1 ? 0.5f : i / (count - 1f));
                GKMCUtil.Box(w, "CanopyWire", new Vector3(0f, WH + 0.85f, z), new Vector3(WW - 3f, 0.035f, 0.035f), wire, false, new Vector3(0f, 0f, i % 2 == 0 ? 2f : -2f));
                for (int b = -2; b <= 2; b++)
                {
                    Vector3 p = new Vector3(b * 3.2f, WH + 0.62f + Mathf.Sin((b + i) * 0.8f) * 0.12f, z);
                    GKMCUtil.Sphere(w, "CanopyBulb", p, Vector3.one * 0.16f, bulb, false);
                    GKMCUtil.PointLight(w, "CanopyBulbLight", p, ti.accent, 0.22f, 4.5f);
                }
            }
        }

        static void AddUtilityWires(Transform w, float x, float poleX, float z0, float z1, int segments)
        {
            Material pole = GKMCUtil.Mat(new Color(0.12f, 0.09f, 0.065f), 0f, 0.18f);
            Material wire = GKMCUtil.Mat(new Color(0.02f, 0.02f, 0.022f), 0.3f, 0.35f);

            for (int i = 0; i < segments; i++)
            {
                float z = Mathf.Lerp(z0, z1, segments == 1 ? 0.5f : i / (segments - 1f));
                GKMCUtil.Cyl(w, "PolishUtilityPole", new Vector3(poleX, 3.8f, z), new Vector3(0.18f, 3.8f, 0.18f), pole, false);
                GKMCUtil.Box(w, "PolishCrossArm", new Vector3(poleX, 7.4f, z), new Vector3(3f, 0.12f, 0.12f), pole, false);
            }

            for (int lane = 0; lane < 3; lane++)
            {
                float y = 7.2f + lane * 0.35f;
                float offset = (lane - 1) * 0.8f;
                GKMCUtil.Box(w, "PolishUtilityWire", new Vector3(x + offset, y, (z0 + z1) * 0.5f), new Vector3(0.04f, 0.04f, Mathf.Abs(z1 - z0)), wire, false);
            }
        }

        static void Headlights(Transform w, Vector3 pos, float yaw, Color color)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Light spot = GKMCUtil.SpotLight(w, "HeadlightBeam", pos + new Vector3(side * 0.65f, 0f, 0f), new Vector3(72f, yaw, 0f), color, 3.2f, 22f, 28f);
                spot.renderMode = LightRenderMode.ForcePixel;
            }
        }

        static void CandleGlowOnly(Transform w, Vector3 pos, Color color)
        {
            Material flame = GKMCUtil.MatEmissive(new Color(1f, 0.65f, 0.25f), new Color(1f, 0.45f, 0.12f), 3f);
            GKMCUtil.Sphere(w, "TinyFlame", pos, Vector3.one * 0.12f, flame, false);
            Light l = GKMCUtil.PointLight(w, "TinyFlameLight", pos, color, 0.45f, 5f);
            var flicker = l.gameObject.AddComponent<FireFlicker>();
            flicker.light = l;
            flicker.baseIntensity = 0.45f;
            flicker.range = 0.22f;
        }

        static void WallWord(Transform w, string name, string text, float x, float y, float z, Color color, float size, bool leftWall)
        {
            Vector3 euler = new Vector3(0f, leftWall ? 90f : -90f, 0f);
            GKMCUtil.Sign(w, name, new Vector3(x, y, z), text, color, size, euler);
        }

        static void AddMirrorShards(Transform w, TrackInfo ti)
        {
            Material chrome = GKMCUtil.Mat(new Color(0.86f, 0.86f, 0.9f), 0.85f, 0.92f);
            for (int i = 0; i < 7; i++)
            {
                float z = -20f + i * 6.6f;
                GKMCUtil.Box(w, "MirrorShard", new Vector3(Random.Range(-2.5f, 2.5f), 2.2f + Random.Range(0f, 4.5f), z),
                    new Vector3(Random.Range(0.25f, 0.7f), Random.Range(0.9f, 2.2f), 0.05f), chrome, false,
                    new Vector3(Random.Range(-8f, 8f), Random.Range(0f, 180f), Random.Range(-8f, 8f)));
            }
        }

        static void AddPressureArrows(Transform w, TrackInfo ti)
        {
            Material paint = GKMCUtil.MatEmissive(ti.accent, ti.accent, 0.5f);
            for (int i = 0; i < 9; i++)
            {
                float z = -19f + i * 4.7f;
                float x = (i % 2 == 0) ? -1.4f : 1.4f;
                GKMCUtil.Box(w, "PressureArrow", new Vector3(x, 0.09f, z), new Vector3(1.4f, 0.02f, 0.18f), paint, false, new Vector3(0f, 20f * (i % 2 == 0 ? 1f : -1f), 0f));
                GKMCUtil.Box(w, "PressureArrowHead", new Vector3(x + (i % 2 == 0 ? 0.7f : -0.7f), 0.1f, z + 0.45f), new Vector3(0.75f, 0.02f, 0.75f), paint, false, new Vector3(0f, 45f, 0f));
            }
        }

        static void AddSunbeams(Transform w)
        {
            Material sunbeam = GKMCUtil.MatTransparent(new Color(1f, 0.88f, 0.35f, 0.16f));
            for (int i = 0; i < 6; i++)
                GKMCUtil.Box(w, "Sunbeam", new Vector3(-10f + i * 4f, 5f, -6f + i * 3f), new Vector3(0.25f, 9f, 11f), sunbeam, false, new Vector3(22f, 0f, -18f));
        }

        static void AddRosePetals(Transform w)
        {
            Material rose = GKMCUtil.MatEmissive(new Color(1f, 0.28f, 0.55f), new Color(1f, 0.22f, 0.45f), 0.45f);
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                GKMCUtil.Box(w, "RosePetalOnFloor", new Vector3(Mathf.Cos(a) * Random.Range(3f, 8f), 0.08f, 4f + Mathf.Sin(a) * Random.Range(3f, 8f)),
                    new Vector3(0.38f, 0.012f, 0.16f), rose, false, new Vector3(0f, Random.Range(0f, 180f), 0f));
            }
        }

        static void AddScannerLines(Transform w, TrackInfo ti)
        {
            Material cold = GKMCUtil.MatEmissive(ti.accent, ti.accent, 0.35f);
            for (int i = -2; i <= 2; i++)
            {
                GKMCUtil.Box(w, "ScannerLine", new Vector3(0f, 1.4f + (i + 2) * 0.7f, i * 5f), new Vector3(WW - 5f, 0.035f, 0.035f), cold, false);
                GKMCUtil.Box(w, "ScannerLineBack", new Vector3(0f, 4.7f - (i + 2) * 0.5f, i * 5f + 2.5f), new Vector3(WW - 7f, 0.03f, 0.03f), cold, false);
            }
        }

        static void AddPoliceTape(Transform w)
        {
            Material tape = GKMCUtil.MatEmissive(new Color(1f, 0.86f, 0.05f), new Color(1f, 0.7f, 0.05f), 0.4f);
            GKMCUtil.Box(w, "PoliceTapeA", new Vector3(0f, 1.8f, -8f), new Vector3(WW - 4f, 0.12f, 0.05f), tape, false, new Vector3(0f, 0f, 6f));
            GKMCUtil.Box(w, "PoliceTapeB", new Vector3(0f, 2.4f, 7f), new Vector3(WW - 7f, 0.12f, 0.05f), tape, false, new Vector3(0f, 0f, -7f));
        }

        static void AddCaustics(Transform w, TrackInfo ti)
        {
            Material caustic = GKMCUtil.MatEmissive(new Color(0.1f, 0.55f, 1f), new Color(0.2f, 0.75f, 1f), 0.28f);
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 6; i++)
                    GKMCUtil.Box(w, "CausticWallStreak", new Vector3(side * (HalfW - 0.3f), 1.5f + i * 0.65f, -17f + i * 6f), new Vector3(0.05f, 0.08f, 4.6f), caustic, false, new Vector3(0f, 0f, Random.Range(-18f, 18f)));
        }

        static void AddMemoryTiles(Transform w)
        {
            Material gold = GKMCUtil.MatEmissive(new Color(1f, 0.75f, 0.38f), new Color(1f, 0.62f, 0.22f), 0.35f);
            for (int i = 0; i < 8; i++)
                GKMCUtil.Box(w, "MemoryTile", new Vector3(-8.5f + i * 2.4f, 0.09f, -6f), new Vector3(1.1f, 0.025f, 1.7f), gold, false);
        }

        static void AddHeartRings(Transform w, TrackInfo ti)
        {
            Material pulse = GKMCUtil.MatEmissive(ti.accent, ti.accent, 0.55f);
            for (int i = 0; i < 4; i++)
            {
                float s = 5f + i * 3f;
                GKMCUtil.Box(w, "HeartRingA", new Vector3(0f, 0.1f, 4f), new Vector3(s, 0.02f, 0.08f), pulse, false, new Vector3(0f, 45f + i * 13f, 0f));
                GKMCUtil.Box(w, "HeartRingB", new Vector3(0f, 0.105f, 4f), new Vector3(s, 0.02f, 0.08f), pulse, false, new Vector3(0f, -45f - i * 13f, 0f));
            }
        }

        static void AddHeatShimmer(Transform w)
        {
            Material heat = GKMCUtil.MatTransparent(new Color(1f, 0.9f, 0.35f, 0.12f));
            for (int i = 0; i < 8; i++)
                GKMCUtil.Box(w, "HeatShimmer", new Vector3(Random.Range(-7f, 7f), 2.5f + Random.Range(0f, 3f), -20f + i * 6f), new Vector3(0.08f, Random.Range(2.5f, 5f), 6f), heat, false, new Vector3(0f, Random.Range(-12f, 12f), Random.Range(-6f, 6f)));
        }
    }
}
