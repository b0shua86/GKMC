using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GKMC
{
    /// <summary>
    /// Third-stage visual pass focused on density, motion and album-world identity. It adds
    /// premium-looking set pieces without requiring imported art: animated neon, kinetic murals,
    /// layered signs, crowds, wires, street furniture, hero sculptures and atmospheric blockers.
    /// </summary>
    public static class PremiumGraphicsPass
    {
        static float WL => AlbumData.WorldLength;
        static float WW => AlbumData.WorldWidth;
        static float WH => AlbumData.WallHeight;
        static float HalfW => WW * 0.5f;
        static float HalfL => WL * 0.5f;

        public static void Apply(List<TrackInfo> tracks, Transform worldsRoot)
        {
            if (tracks == null || worldsRoot == null || tracks.Count == 0) return;

            var previousRandom = Random.state;
            AddMuseumRibbon(worldsRoot, tracks);
            AddGlobalLightRig(worldsRoot, tracks);

            foreach (TrackInfo ti in tracks)
            {
                Transform world = FindWorld(worldsRoot, ti.number);
                if (world == null) continue;

                Random.InitState(5150 + ti.number * 777);
                AddPremiumFrame(world, ti);
                AddForegroundClutter(world, ti);
                AddKineticLayer(world, ti);
                AddPremiumTrackIdentity(world, ti);
            }
            Random.state = previousRandom;
        }

        static Transform FindWorld(Transform root, int number)
        {
            string prefix = $"World_{number:00}_";
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name.StartsWith(prefix)) return child;
            }
            return null;
        }

        static void AddMuseumRibbon(Transform root, List<TrackInfo> tracks)
        {
            Material blackGlass = GKMCUtil.Mat(new Color(0.012f, 0.012f, 0.018f), 0.25f, 0.82f);
            Material rail = GKMCUtil.Mat(new Color(0.04f, 0.04f, 0.048f), 0.35f, 0.65f);
            float zStart = tracks[0].ZCenter - HalfL + 3f;
            float zEnd = tracks[tracks.Count - 1].ZCenter + HalfL - 3f;
            float mid = (zStart + zEnd) * 0.5f;
            float len = zEnd - zStart;

            for (int side = -1; side <= 1; side += 2)
            {
                GKMCUtil.Box(root, "Premium_RibbonRail", new Vector3(side * (HalfW + 1.7f), 6.9f, mid), new Vector3(0.24f, 0.24f, len), rail, false);
                for (int i = 0; i < tracks.Count; i++)
                {
                    TrackInfo ti = tracks[i];
                    float z = ti.ZCenter - HalfL + 8f;
                    Material glow = GKMCUtil.MatEmissive(ti.accent, ti.accent, 1.15f, 0.15f, 0.7f);
                    GKMCUtil.Box(root, "Premium_TrackCardBack", new Vector3(side * (HalfW + 1.55f), 5.2f, z), new Vector3(0.16f, 2.4f, 6.6f), blackGlass, false);
                    GKMCUtil.Box(root, "Premium_TrackCardEdge", new Vector3(side * (HalfW + 1.42f), 6.55f, z), new Vector3(0.08f, 0.08f, 6.4f), glow, false);
                    GKMCUtil.Sign(root, "Premium_TrackCardText", new Vector3(side * (HalfW + 1.35f), 5.2f, z), $"{ti.number:00}\n{ti.theme.ToUpperInvariant()}", ti.accent, 0.22f, new Vector3(0f, side > 0 ? -90f : 90f, 0f));
                }
            }
        }

        static void AddGlobalLightRig(Transform root, List<TrackInfo> tracks)
        {
            float first = tracks[0].ZCenter - HalfL;
            float last = tracks[tracks.Count - 1].ZCenter + HalfL;
            Material rigMat = GKMCUtil.Mat(new Color(0.025f, 0.025f, 0.03f), 0.35f, 0.5f);

            for (int i = 0; i < 18; i++)
            {
                float z = Mathf.Lerp(first, last, i / 17f);
                for (int side = -1; side <= 1; side += 2)
                {
                    GKMCUtil.Cyl(root, "Premium_LightTrussPole", new Vector3(side * (HalfW + 2.9f), 4.8f, z), new Vector3(0.12f, 4.8f, 0.12f), rigMat, false);
                    TrackInfo ti = tracks[Mathf.Clamp(Mathf.RoundToInt(i / 17f * (tracks.Count - 1)), 0, tracks.Count - 1)];
                    Light light = GKMCUtil.SpotLight(root, "Premium_RigSpot", new Vector3(side * (HalfW + 2.7f), 8.5f, z), new Vector3(68f, side > 0 ? -28f : 28f, 0f), ti.accent, 1.8f, 30f, 28f);
                    light.renderMode = LightRenderMode.ForcePixel;
                    light.shadows = i % 3 == 0 ? LightShadows.Soft : LightShadows.None;
                }
            }
        }

        static void AddPremiumFrame(Transform w, TrackInfo ti)
        {
            Material black = GKMCUtil.Mat(new Color(0.012f, 0.012f, 0.015f), 0.25f, 0.55f);
            Material neon = GKMCUtil.MatEmissive(ti.accent, ti.accent, 2.1f, 0.1f, 0.75f);
            Material glass = GKMCUtil.MatTransparent(new Color(ti.accent.r, ti.accent.g, ti.accent.b, 0.12f));

            for (int i = 0; i < 4; i++)
            {
                float z = -18f + i * 12f;
                GKMCUtil.Box(w, "Premium_ProsceniumTop", new Vector3(0f, 8.8f, z), new Vector3(WW - 4f, 0.18f, 0.18f), neon, false);
                GKMCUtil.Box(w, "Premium_ProsceniumL", new Vector3(-HalfW + 2.2f, 4.7f, z), new Vector3(0.18f, 8.0f, 0.18f), black, false);
                GKMCUtil.Box(w, "Premium_ProsceniumR", new Vector3(HalfW - 2.2f, 4.7f, z), new Vector3(0.18f, 8.0f, 0.18f), black, false);
                LightBand(w, new Vector3(0f, 4.8f, z + 0.08f), ti.accent, 0.24f, 0.85f, 0.6f);
            }

            for (int i = 0; i < 5; i++)
            {
                float z = -22f + i * 11f;
                GKMCUtil.Box(w, "Premium_GlassScrim", new Vector3(0f, 4.4f, z), new Vector3(WW - 7f, 5.8f, 0.035f), glass, false, new Vector3(0f, Random.Range(-2f, 2f), Random.Range(-3f, 3f)));
            }
        }

        static void AddForegroundClutter(Transform w, TrackInfo ti)
        {
            Material dark = GKMCUtil.Mat(new Color(0.03f, 0.028f, 0.026f), 0.05f, 0.24f);
            Material paper = GKMCUtil.Mat(new Color(0.72f, 0.68f, 0.58f), 0f, 0.16f);
            Material accent = GKMCUtil.MatEmissive(ti.accent, ti.accent, 0.35f);

            for (int i = 0; i < 34; i++)
            {
                float sideBias = Random.value > 0.5f ? -1f : 1f;
                float x = Random.Range(6f, HalfW - 2.4f) * sideBias;
                float z = Random.Range(-HalfL + 4f, HalfL - 4f);
                float choice = Random.value;
                if (choice < 0.28f)
                {
                    TrashBag(w, new Vector3(x, 0f, z), dark);
                }
                else if (choice < 0.58f)
                {
                    GKMCUtil.Box(w, "Premium_PosterScrap", new Vector3(x, 0.09f, z), new Vector3(Random.Range(0.32f, 0.9f), 0.014f, Random.Range(0.2f, 0.5f)), paper, false, new Vector3(0f, Random.Range(0f, 180f), 0f));
                }
                else
                {
                    GKMCUtil.Cyl(w, "Premium_BottleCan", new Vector3(x, 0.18f, z), new Vector3(0.11f, 0.18f, 0.11f), accent, false, new Vector3(90f, Random.Range(0f, 180f), 0f));
                }
            }
        }

        static void AddKineticLayer(Transform w, TrackInfo ti)
        {
            Material neon = GKMCUtil.MatEmissive(ti.accent, ti.accent, 1.15f);
            for (int i = 0; i < 12; i++)
            {
                float z = -22f + i * 4.1f;
                float x = Random.value > 0.5f ? -HalfW + 0.8f : HalfW - 0.8f;
                GameObject slash = GKMCUtil.Box(w, "Premium_KineticSlash", new Vector3(x, 4f + Random.Range(-1.2f, 1.8f), z), new Vector3(0.08f, 0.18f, Random.Range(2.4f, 5.6f)), neon, false, new Vector3(0f, 0f, Random.Range(-32f, 32f)));
                var pulse = slash.AddComponent<PremiumNeonPulse>();
                pulse.color = ti.accent;
                pulse.intensity = Random.Range(0.8f, 2.4f);
                pulse.speed = Random.Range(1.2f, 3.8f);
            }
        }

        static void AddPremiumTrackIdentity(Transform w, TrackInfo ti)
        {
            switch (ti.number)
            {
                case 1: PremiumSherane(w, ti); break;
                case 2: PremiumVibe(w, ti); break;
                case 3: PremiumBackseat(w, ti); break;
                case 4: PremiumPeerPressure(w, ti); break;
                case 5: PremiumMoneyTrees(w, ti); break;
                case 6: PremiumPoeticJustice(w, ti); break;
                case 7: PremiumGoodKid(w, ti); break;
                case 8: PremiumMaadCity(w, ti); break;
                case 9: PremiumSwimmingPools(w, ti); break;
                case 10: PremiumSingAboutMe(w, ti); break;
                case 11: PremiumReal(w, ti); break;
                case 12: PremiumCompton(w, ti); break;
            }
        }

        static void PremiumSherane(Transform w, TrackInfo ti)
        {
            Material warm = GKMCUtil.MatEmissive(new Color(1f, 0.55f, 0.22f), new Color(1f, 0.36f, 0.08f), 1.2f);
            for (int i = 0; i < 7; i++) StreetLamp(w, new Vector3(i % 2 == 0 ? -11f : 11f, 0f, -22f + i * 7.4f), warm);
            ShadowCrowd(w, new Vector3(4.5f, 0f, 19f), 7, ti.accent, 0.52f);
            GKMCUtil.Sign(w, "Premium_SheraneTitle", new Vector3(0f, 7.6f, -15f), "DUSK ON ROSECRANS", new Color(1f, 0.72f, 0.34f), 0.38f, new Vector3(0f, 180f, 0f));
        }

        static void PremiumVibe(Transform w, TrackInfo ti)
        {
            for (int i = 0; i < 5; i++) Halo(w, new Vector3(0f, 3f + i * 0.55f, 6f), 9.5f - i * 1.15f, ti.accent, 28, 0.75f);
            GKMCUtil.Particles(w, "Premium_ThickHaze", new Vector3(0f, 2f, 6f), new Color(0.42f, 1f, 0.86f, 0.18f), 24f, 1.7f, 7f, new Vector3(WW - 5f, 2.2f, WL - 7f), new Vector3(0.05f, 0.18f, 0.02f), 0f, true);
            GKMCUtil.Sign(w, "Premium_VibeTitle", new Vector3(0f, 7.9f, 6f), "INNER ROOM", ti.accent, 0.5f, new Vector3(0f, 180f, 0f));
        }

        static void PremiumBackseat(Transform w, TrackInfo ti)
        {
            Material mirror = GKMCUtil.Mat(new Color(0.78f, 0.78f, 0.86f), 0.92f, 0.98f);
            for (int i = 0; i < 18; i++)
            {
                float z = -22f + i * 2.7f;
                float x = Mathf.Sin(i * 2.1f) * 5.5f;
                GameObject shard = GKMCUtil.Box(w, "Premium_RotatingMirrorShard", new Vector3(x, 4f + Mathf.Sin(i) * 2f, z), new Vector3(0.12f, Random.Range(1.6f, 4.8f), Random.Range(0.8f, 2.1f)), mirror, false, new Vector3(Random.Range(-15f, 15f), Random.Range(0f, 180f), Random.Range(-10f, 10f)));
                var orbit = shard.AddComponent<PremiumSlowSpin>();
                orbit.axis = Vector3.up;
                orbit.degPerSec = Random.Range(7f, 19f);
            }
            GKMCUtil.Sign(w, "Premium_BackseatTitle", new Vector3(0f, 8.2f, 18f), "MONEY / POWER / MIRRORS", ti.accent, 0.38f, new Vector3(0f, 180f, 0f));
        }

        static void PremiumPeerPressure(Transform w, TrackInfo ti)
        {
            Material lane = GKMCUtil.MatEmissive(ti.accent, ti.accent, 0.45f);
            for (int i = 0; i < 16; i++)
            {
                float z = -22f + i * 3f;
                GKMCUtil.Box(w, "Premium_PeerPressurePath", new Vector3(Mathf.Sin(i * 0.8f) * 2.4f, 0.12f, z), new Vector3(3.2f, 0.018f, 0.18f), lane, false, new Vector3(0f, Mathf.Sin(i) * 35f, 0f));
            }
            ShadowCrowd(w, new Vector3(0f, 0f, 13f), 14, ti.accent, 0.68f);
            GKMCUtil.Sign(w, "Premium_PeerTitle", new Vector3(0f, 7.8f, 20f), "RIDE WITH THE HOMIES", ti.accent, 0.38f, new Vector3(0f, 180f, 0f));
        }

        static void PremiumMoneyTrees(Transform w, TrackInfo ti)
        {
            Material gold = GKMCUtil.MatEmissive(new Color(1f, 0.8f, 0.16f), new Color(1f, 0.6f, 0.05f), 1.15f);
            for (int i = 0; i < 7; i++)
            {
                float x = -12f + i * 4f;
                LuxuryLantern(w, new Vector3(x, 5.4f + Mathf.Sin(i) * 0.5f, -2f + Mathf.Cos(i) * 9f), gold);
            }
            MoneyCanopy(w, ti.accent);
            GKMCUtil.Sign(w, "Premium_MoneyTitle", new Vector3(0f, 8.2f, 19f), "HALLELUJAH BACKYARD", new Color(1f, 0.85f, 0.25f), 0.42f, new Vector3(0f, 180f, 0f));
        }

        static void PremiumPoeticJustice(Transform w, TrackInfo ti)
        {
            Material pink = GKMCUtil.MatEmissive(new Color(1f, 0.38f, 0.68f), new Color(1f, 0.2f, 0.48f), 0.9f);
            for (int i = 0; i < 10; i++) Ribbon(w, new Vector3(-11f + i * 2.45f, 4.5f + Mathf.Sin(i) * 1.4f, -18f + i * 4.2f), pink, 5f, Random.Range(-22f, 22f));
            HeartMarquee(w, new Vector3(0f, 6.2f, 14f), ti.accent);
            GKMCUtil.Sign(w, "Premium_PoeticTitle", new Vector3(0f, 7.9f, 19f), "SOFT LIGHT / HARD CITY", ti.accent, 0.36f, new Vector3(0f, 180f, 0f));
        }

        static void PremiumGoodKid(Transform w, TrackInfo ti)
        {
            Material screen = GKMCUtil.MatEmissive(new Color(0.1f, 0.26f, 0.44f), ti.accent, 0.75f);
            for (int i = 0; i < 12; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                CCTVScreen(w, new Vector3(side * 13.2f, 4.2f + (i % 3) * 0.7f, -20f + i * 3.7f), side > 0 ? -90f : 90f, screen, ti.accent);
            }
            GridDome(w, ti.accent);
            GKMCUtil.Sign(w, "Premium_GoodKidTitle", new Vector3(0f, 8.1f, 19f), "SYSTEM MAP", ti.accent, 0.48f, new Vector3(0f, 180f, 0f));
        }

        static void PremiumMaadCity(Transform w, TrackInfo ti)
        {
            Material fire = GKMCUtil.MatEmissive(new Color(1f, 0.22f, 0.04f), new Color(1f, 0.08f, 0f), 1.8f);
            for (int i = 0; i < 9; i++) FlameTotem(w, new Vector3(-12f + i * 3f, 0f, -17f + (i % 4) * 10f), fire, ti.accent);
            PoliceLightTunnel(w);
            GKMCUtil.Sign(w, "Premium_MaadTitle", new Vector3(0f, 8.2f, 18f), "ANGEL ON ANGEL DUST", new Color(1f, 0.2f, 0.06f), 0.4f, new Vector3(0f, 180f, 0f));
        }

        static void PremiumSwimmingPools(Transform w, TrackInfo ti)
        {
            Material aqua = GKMCUtil.MatTransparent(new Color(0.06f, 0.6f, 1f, 0.18f));
            for (int i = 0; i < 8; i++) WaveWall(w, new Vector3(0f, 3.6f + Mathf.Sin(i) * 0.5f, -19f + i * 5.5f), aqua, ti.accent);
            BubbleColumn(w, new Vector3(-8f, 1f, 4f), ti.accent);
            BubbleColumn(w, new Vector3(8f, 1f, 8f), ti.accent);
            GKMCUtil.Sign(w, "Premium_PoolsTitle", new Vector3(0f, 8f, 19f), "UNDERWATER PRESSURE", ti.accent, 0.4f, new Vector3(0f, 180f, 0f));
        }

        static void PremiumSingAboutMe(Transform w, TrackInfo ti)
        {
            Material candle = GKMCUtil.MatEmissive(new Color(1f, 0.72f, 0.28f), new Color(1f, 0.45f, 0.1f), 1.2f);
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 7; col++)
                    MemorialCandle(w, new Vector3(-9f + col * 3f, 0f, -20f + row * 3.2f), candle);
            BaptismRays(w, ti.accent);
            GKMCUtil.Sign(w, "Premium_SingTitle", new Vector3(0f, 8f, 18f), "MEMORY / WATER / PRAYER", new Color(1f, 0.78f, 0.42f), 0.36f, new Vector3(0f, 180f, 0f));
        }

        static void PremiumReal(Transform w, TrackInfo ti)
        {
            Material red = GKMCUtil.MatEmissive(new Color(1f, 0.16f, 0.2f), new Color(1f, 0.05f, 0.08f), 1.4f);
            PulseRings(w, new Vector3(0f, 3.3f, 4f), red, 9);
            MirrorHalo(w, new Vector3(0f, 3.7f, 4f));
            GKMCUtil.Sign(w, "Premium_RealTitle", new Vector3(0f, 8.3f, 18f), "LOVE OF SELF", ti.accent, 0.5f, new Vector3(0f, 180f, 0f));
        }

        static void PremiumCompton(Transform w, TrackInfo ti)
        {
            Material sun = GKMCUtil.MatEmissive(new Color(1f, 0.78f, 0.12f), new Color(1f, 0.54f, 0.04f), 1.4f);
            for (int i = 0; i < 6; i++) ParadeArch(w, new Vector3(0f, 5.2f, -21f + i * 8f), sun, ti.accent);
            LowriderSpotGrid(w, ti.accent);
            GKMCUtil.Sign(w, "Premium_ComptonTitle", new Vector3(0f, 8.6f, 21f), "CITY THAT MADE HIM", new Color(1f, 0.82f, 0.2f), 0.46f, new Vector3(0f, 180f, 0f));
        }

        static void LightBand(Transform parent, Vector3 pos, Color color, float alpha, float width, float height)
        {
            Material mat = GKMCUtil.MatTransparent(new Color(color.r, color.g, color.b, alpha));
            GKMCUtil.Box(parent, "Premium_LightBand", pos, new Vector3(WW * width, WH * height, 0.04f), mat, false);
        }

        static void TrashBag(Transform parent, Vector3 pos, Material mat)
        {
            GKMCUtil.Sphere(parent, "Premium_TrashBag", pos + new Vector3(0f, 0.32f, 0f), new Vector3(Random.Range(0.35f, 0.7f), Random.Range(0.25f, 0.55f), Random.Range(0.35f, 0.7f)), mat, false);
            GKMCUtil.Cyl(parent, "Premium_TrashTie", pos + new Vector3(0f, 0.77f, 0f), new Vector3(0.08f, 0.18f, 0.08f), mat, false);
        }

        static void StreetLamp(Transform parent, Vector3 pos, Material lightMat)
        {
            Material pole = GKMCUtil.Mat(new Color(0.035f, 0.035f, 0.04f), 0.4f, 0.5f);
            GKMCUtil.Cyl(parent, "Premium_StreetLampPole", pos + new Vector3(0f, 3f, 0f), new Vector3(0.12f, 3f, 0.12f), pole, false);
            GKMCUtil.Box(parent, "Premium_StreetLampArm", pos + new Vector3(0.65f, 5.8f, 0f), new Vector3(1.3f, 0.08f, 0.08f), pole, false);
            GKMCUtil.Sphere(parent, "Premium_StreetLampGlow", pos + new Vector3(1.35f, 5.65f, 0f), Vector3.one * 0.24f, lightMat, false);
            Light l = GKMCUtil.PointLight(parent, "Premium_StreetLampLight", pos + new Vector3(1.35f, 5.45f, 0f), new Color(1f, 0.55f, 0.25f), 1.1f, 12f);
            l.shadows = LightShadows.Soft;
        }

        static void ShadowCrowd(Transform parent, Vector3 center, int count, Color accent, float alpha)
        {
            Material mat = GKMCUtil.MatTransparent(new Color(0f, 0f, 0.02f, alpha));
            for (int i = 0; i < count; i++)
            {
                Vector3 p = center + new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-2.5f, 2.5f));
                float h = Random.Range(1.5f, 2.45f);
                GKMCUtil.Cyl(parent, "Premium_CrowdBody", p + new Vector3(0f, h * 0.5f, 0f), new Vector3(0.25f, h * 0.5f, 0.25f), mat, false);
                GKMCUtil.Sphere(parent, "Premium_CrowdHead", p + new Vector3(0f, h + 0.25f, 0f), Vector3.one * 0.28f, mat, false);
                if (Random.value > 0.6f) GKMCUtil.Sphere(parent, "Premium_CrowdEye", p + new Vector3(0f, h + 0.28f, -0.28f), Vector3.one * 0.06f, GKMCUtil.MatEmissive(accent, accent, 1.2f), false);
            }
        }

        static void Halo(Transform parent, Vector3 center, float radius, Color color, int segments, float intensity)
        {
            Material mat = GKMCUtil.MatEmissive(color, color, intensity);
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                Vector3 p = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                GameObject seg = GKMCUtil.Box(parent, "Premium_HaloSegment", p, new Vector3(0.8f, 0.06f, 0.08f), mat, false, new Vector3(0f, -a * Mathf.Rad2Deg, 0f));
                PremiumNeonPulse pulse = seg.AddComponent<PremiumNeonPulse>(); pulse.color = color; pulse.intensity = intensity; pulse.speed = 1.2f + i * 0.03f;
            }
        }

        static void MoneyCanopy(Transform parent, Color color)
        {
            Material mat = GKMCUtil.MatEmissive(new Color(0.42f, 0.85f, 0.32f), color, 0.45f);
            for (int i = 0; i < 100; i++)
            {
                GameObject bill = GKMCUtil.Box(parent, "Premium_MoneyCanopyBill", new Vector3(Random.Range(-14f, 14f), Random.Range(5f, 9f), Random.Range(-22f, 22f)), new Vector3(0.45f, 0.012f, 0.2f), mat, false, new Vector3(Random.Range(-40f, 40f), Random.Range(0f, 180f), Random.Range(-40f, 40f)));
                PremiumFloatTwist twist = bill.AddComponent<PremiumFloatTwist>(); twist.amplitude = Random.Range(0.1f, 0.5f); twist.degPerSec = Random.Range(12f, 36f);
            }
        }

        static void LuxuryLantern(Transform parent, Vector3 pos, Material mat)
        {
            GKMCUtil.Sphere(parent, "Premium_Lantern", pos, Vector3.one * 0.22f, mat, false);
            GKMCUtil.Cyl(parent, "Premium_LanternDrop", pos + new Vector3(0f, -0.32f, 0f), new Vector3(0.025f, 0.32f, 0.025f), mat, false);
            GKMCUtil.PointLight(parent, "Premium_LanternLight", pos, new Color(1f, 0.78f, 0.18f), 0.55f, 7f);
        }

        static void Ribbon(Transform parent, Vector3 pos, Material mat, float len, float roll)
        {
            GameObject r = GKMCUtil.Box(parent, "Premium_Ribbon", pos, new Vector3(0.18f, 0.08f, len), mat, false, new Vector3(0f, Random.Range(0f, 180f), roll));
            PremiumFloatTwist twist = r.AddComponent<PremiumFloatTwist>(); twist.amplitude = 0.16f; twist.degPerSec = 8f;
        }

        static void HeartMarquee(Transform parent, Vector3 center, Color color)
        {
            Material mat = GKMCUtil.MatEmissive(color, color, 1.5f);
            for (int i = 0; i < 24; i++)
            {
                float t = i / 24f * Mathf.PI * 2f;
                float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f) * 0.09f;
                float y = (13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t)) * 0.09f;
                GKMCUtil.Sphere(parent, "Premium_HeartBulb", center + new Vector3(x, y, 0f), Vector3.one * 0.13f, mat, false);
            }
            GKMCUtil.PointLight(parent, "Premium_HeartMarqueeLight", center, color, 1.0f, 12f);
        }

        static void CCTVScreen(Transform parent, Vector3 pos, float yaw, Material screen, Color color)
        {
            Material frame = GKMCUtil.Mat(new Color(0.01f, 0.012f, 0.016f), 0.25f, 0.55f);
            GKMCUtil.Box(parent, "Premium_CCTVFrame", pos, new Vector3(0.16f, 1.35f, 2.1f), frame, false, new Vector3(0f, yaw, 0f));
            GKMCUtil.Box(parent, "Premium_CCTVScreen", pos + new Vector3(yaw > 0 ? -0.09f : 0.09f, 0f, 0f), new Vector3(0.08f, 1.0f, 1.7f), screen, false, new Vector3(0f, yaw, 0f));
            GKMCUtil.Sign(parent, "Premium_CCTVText", pos + new Vector3(yaw > 0 ? -0.15f : 0.15f, 0f, 0f), "LIVE", color, 0.16f, new Vector3(0f, yaw, 0f));
        }

        static void GridDome(Transform parent, Color color)
        {
            Material mat = GKMCUtil.MatEmissive(color, color, 0.45f);
            for (int i = 0; i < 9; i++)
            {
                float y = 2f + i * 0.65f;
                GKMCUtil.Box(parent, "Premium_GridDomeBand", new Vector3(0f, y, 5f), new Vector3(WW - 6f - i * 1.2f, 0.035f, 0.035f), mat, false);
                GKMCUtil.Box(parent, "Premium_GridDomeDepth", new Vector3(0f, y, 5f), new Vector3(0.035f, 0.035f, WL - 18f - i * 0.8f), mat, false);
            }
        }

        static void FlameTotem(Transform parent, Vector3 pos, Material fire, Color color)
        {
            Material barrel = GKMCUtil.Mat(new Color(0.08f, 0.05f, 0.04f), 0.55f, 0.4f);
            GKMCUtil.Cyl(parent, "Premium_FlameTotemBarrel", pos + new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 0.55f, 0.5f), barrel, false);
            for (int i = 0; i < 3; i++) GKMCUtil.Sphere(parent, "Premium_FlameOrb", pos + new Vector3(Random.Range(-0.18f, 0.18f), 1.25f + i * 0.38f, Random.Range(-0.18f, 0.18f)), Vector3.one * (0.32f - i * 0.05f), fire, false);
            Light l = GKMCUtil.PointLight(parent, "Premium_FlameLight", pos + new Vector3(0f, 1.6f, 0f), color, 1.7f, 10f);
            FireFlicker flicker = l.gameObject.AddComponent<FireFlicker>(); flicker.light = l; flicker.baseIntensity = 1.7f; flicker.range = 0.8f;
        }

        static void PoliceLightTunnel(Transform parent)
        {
            Material red = GKMCUtil.MatEmissive(Color.red, Color.red, 1.4f);
            Material blue = GKMCUtil.MatEmissive(new Color(0.08f, 0.22f, 1f), new Color(0.08f, 0.22f, 1f), 1.4f);
            for (int i = 0; i < 10; i++)
            {
                float z = -23f + i * 5.1f;
                GameObject a = GKMCUtil.Box(parent, "Premium_PolicePulseRed", new Vector3(-5.2f, 6.4f, z), new Vector3(4.8f, 0.12f, 0.12f), red, false);
                GameObject b = GKMCUtil.Box(parent, "Premium_PolicePulseBlue", new Vector3(5.2f, 6.4f, z), new Vector3(4.8f, 0.12f, 0.12f), blue, false);
                a.AddComponent<PremiumNeonPulse>().color = Color.red;
                b.AddComponent<PremiumNeonPulse>().color = new Color(0.08f, 0.22f, 1f);
            }
        }

        static void WaveWall(Transform parent, Vector3 pos, Material mat, Color color)
        {
            for (int i = 0; i < 6; i++)
            {
                GameObject wave = GKMCUtil.Box(parent, "Premium_WaveRibbon", pos + new Vector3(0f, Mathf.Sin(i) * 0.45f, i * 0.35f), new Vector3(WW - 6f, 0.04f, 0.25f), mat, false, new Vector3(0f, 0f, Mathf.Sin(i) * 8f));
                PremiumFloatTwist twist = wave.AddComponent<PremiumFloatTwist>(); twist.amplitude = 0.1f; twist.degPerSec = 2f;
            }
            GKMCUtil.PointLight(parent, "Premium_WaveLight", pos, color, 0.6f, 12f);
        }

        static void BubbleColumn(Transform parent, Vector3 pos, Color color)
        {
            Material mat = GKMCUtil.MatTransparent(new Color(0.65f, 0.9f, 1f, 0.32f));
            for (int i = 0; i < 24; i++)
            {
                GameObject b = GKMCUtil.Sphere(parent, "Premium_Bubble", pos + new Vector3(Random.Range(-1.1f, 1.1f), i * 0.28f, Random.Range(-1.1f, 1.1f)), Vector3.one * Random.Range(0.08f, 0.24f), mat, false);
                Bobber bob = b.AddComponent<Bobber>(); bob.amplitude = 0.28f; bob.speed = Random.Range(0.4f, 1.4f); bob.sway = 0.16f;
            }
            GKMCUtil.PointLight(parent, "Premium_BubbleGlow", pos + new Vector3(0f, 3f, 0f), color, 0.5f, 10f);
        }

        static void MemorialCandle(Transform parent, Vector3 pos, Material flame)
        {
            Material wax = GKMCUtil.Mat(new Color(0.86f, 0.82f, 0.72f), 0f, 0.35f);
            GKMCUtil.Cyl(parent, "Premium_MemorialCandleWax", pos + new Vector3(0f, 0.25f, 0f), new Vector3(0.1f, 0.25f, 0.1f), wax, false);
            GKMCUtil.Sphere(parent, "Premium_MemorialCandleFlame", pos + new Vector3(0f, 0.62f, 0f), Vector3.one * 0.08f, flame, false);
        }

        static void BaptismRays(Transform parent, Color color)
        {
            Material mat = GKMCUtil.MatTransparent(new Color(color.r, color.g, color.b, 0.18f));
            for (int i = 0; i < 8; i++) GKMCUtil.Box(parent, "Premium_BaptismRay", new Vector3(-7f + i * 2f, 5.2f, 15.5f), new Vector3(0.3f, 8f, 8f), mat, false, new Vector3(24f, Random.Range(-8f, 8f), Random.Range(-18f, 18f)));
        }

        static void PulseRings(Transform parent, Vector3 center, Material mat, int rings)
        {
            for (int r = 0; r < rings; r++)
            {
                float radius = 2.5f + r * 0.7f;
                for (int i = 0; i < 14; i++)
                {
                    float a = i / 14f * Mathf.PI * 2f;
                    GameObject seg = GKMCUtil.Box(parent, "Premium_PulseRingSegment", center + new Vector3(Mathf.Cos(a) * radius, r * 0.04f, Mathf.Sin(a) * radius), new Vector3(0.5f, 0.055f, 0.08f), mat, false, new Vector3(0f, -a * Mathf.Rad2Deg, 0f));
                    PremiumNeonPulse pulse = seg.AddComponent<PremiumNeonPulse>(); pulse.color = Color.red; pulse.intensity = 1f + r * 0.1f; pulse.speed = 1.1f + r * 0.07f;
                }
            }
        }

        static void MirrorHalo(Transform parent, Vector3 center)
        {
            Material mirror = GKMCUtil.Mat(new Color(0.8f, 0.8f, 0.88f), 0.92f, 0.97f);
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                GKMCUtil.Box(parent, "Premium_MirrorHaloShard", center + new Vector3(Mathf.Cos(a) * 8.5f, Mathf.Sin(i) * 0.8f, Mathf.Sin(a) * 8.5f), new Vector3(0.12f, 2.8f, 0.8f), mirror, false, new Vector3(0f, -a * Mathf.Rad2Deg + 90f, Random.Range(-10f, 10f)));
            }
        }

        static void ParadeArch(Transform parent, Vector3 center, Material sun, Color accent)
        {
            Material dark = GKMCUtil.Mat(new Color(0.025f, 0.025f, 0.028f), 0.35f, 0.55f);
            GKMCUtil.Cyl(parent, "Premium_ParadeArchL", center + new Vector3(-8f, -1f, 0f), new Vector3(0.16f, 5.6f, 0.16f), dark, false);
            GKMCUtil.Cyl(parent, "Premium_ParadeArchR", center + new Vector3(8f, -1f, 0f), new Vector3(0.16f, 5.6f, 0.16f), dark, false);
            for (int i = 0; i < 17; i++)
            {
                float a = Mathf.Lerp(0f, Mathf.PI, i / 16f);
                Vector3 p = center + new Vector3(Mathf.Cos(a) * 8f, Mathf.Sin(a) * 3.2f, 0f);
                GKMCUtil.Sphere(parent, "Premium_ParadeBulb", p, Vector3.one * 0.16f, sun, false);
            }
            GKMCUtil.PointLight(parent, "Premium_ParadeArchLight", center + new Vector3(0f, 2f, 0f), accent, 0.8f, 14f);
        }

        static void LowriderSpotGrid(Transform parent, Color accent)
        {
            for (int i = 0; i < 8; i++)
            {
                float x = -8.5f + i * 2.4f;
                Light l = GKMCUtil.SpotLight(parent, "Premium_LowriderShowSpot", new Vector3(x, 8.2f, -4f + Mathf.Sin(i) * 12f), new Vector3(82f, Random.Range(-18f, 18f), 0f), accent, 1.35f, 22f, 22f);
                l.renderMode = LightRenderMode.ForcePixel;
            }
        }
    }

    public class PremiumNeonPulse : MonoBehaviour
    {
        public Color color = Color.white;
        public float intensity = 1.4f;
        public float speed = 2.2f;
        Renderer _renderer;
        Vector3 _baseScale;
        Material _mat;
        float _phase;

        void Start()
        {
            _renderer = GetComponent<Renderer>();
            _baseScale = transform.localScale;
            _phase = Random.value * 6.28318f;
            if (_renderer != null) _mat = _renderer.material;
        }

        void Update()
        {
            float k = Mathf.Sin(Time.time * speed + _phase) * 0.5f + 0.5f;
            transform.localScale = _baseScale * (1f + k * 0.06f);
            if (_mat != null && _mat.HasProperty("_EmissionColor")) _mat.SetColor("_EmissionColor", color * Mathf.Lerp(0.35f, intensity, k));
        }
    }

    public class PremiumSlowSpin : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        public float degPerSec = 12f;
        void Update() => transform.Rotate(axis * degPerSec * Time.deltaTime, Space.Self);
    }

    public class PremiumFloatTwist : MonoBehaviour
    {
        public float amplitude = 0.2f;
        public float degPerSec = 18f;
        Vector3 _base;
        float _phase;

        void Start()
        {
            _base = transform.localPosition;
            _phase = Random.value * 6.28318f;
        }

        void Update()
        {
            transform.localPosition = _base + Vector3.up * Mathf.Sin(Time.time * 0.8f + _phase) * amplitude;
            transform.Rotate(Vector3.up * degPerSec * Time.deltaTime, Space.World);
        }
    }
}
