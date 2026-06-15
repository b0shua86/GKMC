using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GKMC
{
    /// <summary>
    /// Heavyweight procedural art direction pass. This deliberately stays inside code so the
    /// project still opens in an empty Unity scene and builds itself, but it pushes the result
    /// closer to a stylized playable installation: layered skyline, textured surfaces, neon portals,
    /// volumetric-looking beams, murals, silhouettes, practical lights and one distinct hero layer
    /// per track-world.
    /// </summary>
    public static class GraphicsOverhaul
    {
        static readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
        static readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();

        static float WL => AlbumData.WorldLength;
        static float WW => AlbumData.WorldWidth;
        static float WH => AlbumData.WallHeight;
        static float HalfW => WW * 0.5f;
        static float HalfL => WL * 0.5f;

        public static void Apply(List<TrackInfo> tracks, Transform worldsRoot)
        {
            if (tracks == null || tracks.Count == 0 || worldsRoot == null) return;

            ConfigureRenderPipeline();
            AddLongRangeSetDressing(worldsRoot, tracks);
            AddAlbumArrival(worldsRoot, tracks[0]);

            var previousRandom = Random.state;
            foreach (TrackInfo ti in tracks)
            {
                Transform w = FindWorld(worldsRoot, ti.number);
                if (w == null) continue;

                Random.InitState(1979 + ti.number * 3889);
                RetextureWorld(w, ti);
                AddCinematicPortal(w, ti);
                AddDepthLayers(w, ti);
                AddHeroLighting(w, ti);
                AddTrackWorld(w, ti);
            }
            Random.state = previousRandom;
        }

        static void ConfigureRenderPipeline()
        {
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 130f);
            QualitySettings.antiAliasing = Mathf.Max(QualitySettings.antiAliasing, 4);
            QualitySettings.softParticles = true;

            RenderSettings.reflectionIntensity = 0.38f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.defaultReflectionResolution = Mathf.Max(RenderSettings.defaultReflectionResolution, 256);
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

        static void AddLongRangeSetDressing(Transform root, List<TrackInfo> tracks)
        {
            float startZ = -HalfL - 36f;
            float endZ = tracks[tracks.Count - 1].ZCenter + HalfL + 46f;
            float midZ = (startZ + endZ) * 0.5f;
            float lenZ = endZ - startZ;

            Material asphalt = NoiseMat("horizon_asphalt", new Color(0.055f, 0.055f, 0.06f), new Color(0.16f, 0.15f, 0.14f), 0f, 0.18f, 0.42f, 16f);
            GKMCUtil.Box(root, "Overhaul_HorizonStreet", new Vector3(0f, -0.34f, midZ), new Vector3(72f, 0.08f, lenZ), asphalt, false);

            Material silhouette = GKMCUtil.Mat(new Color(0.025f, 0.025f, 0.032f), 0f, 0.18f);
            Material window = GKMCUtil.MatEmissive(new Color(0.8f, 0.62f, 0.25f), new Color(1f, 0.62f, 0.16f), 0.25f);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 46; i++)
                {
                    float z = Mathf.Lerp(startZ + 8f, endZ - 8f, i / 45f);
                    float h = Random.Range(3.8f, 13f);
                    float d = Random.Range(3.4f, 8.5f);
                    float x = side * Random.Range(25f, 39f);
                    GKMCUtil.Box(root, "Overhaul_DistantBuilding", new Vector3(x, h * 0.5f - 0.2f, z), new Vector3(Random.Range(3.5f, 9f), h, d), silhouette, false);
                    if (Random.value > 0.35f)
                    {
                        for (int row = 0; row < Mathf.Min(4, Mathf.FloorToInt(h / 2f)); row++)
                            GKMCUtil.Box(root, "Overhaul_WindowBand", new Vector3(x - side * 0.02f, 1.5f + row * 1.7f, z), new Vector3(0.06f, 0.16f, d * 0.62f), window, false);
                    }
                }
            }

            Material wire = GKMCUtil.Mat(new Color(0.018f, 0.018f, 0.02f), 0.25f, 0.36f);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int line = 0; line < 4; line++)
                {
                    GKMCUtil.Box(root, "Overhaul_LongPowerLine", new Vector3(side * (HalfW + 4.8f + line * 0.18f), 8.8f + line * 0.28f, midZ),
                        new Vector3(0.035f, 0.035f, lenZ), wire, false);
                }
            }
        }

        static void AddAlbumArrival(Transform root, TrackInfo first)
        {
            float z = -HalfL - 12f;
            Material black = GKMCUtil.Mat(new Color(0.015f, 0.014f, 0.018f), 0.1f, 0.4f);
            Material neon = GKMCUtil.MatEmissive(first.accent, first.accent, 2.2f);
            Material gold = GKMCUtil.MatEmissive(new Color(1f, 0.75f, 0.18f), new Color(1f, 0.62f, 0.08f), 1.4f, 0.4f, 0.7f);

            GKMCUtil.Box(root, "Overhaul_EntryMonolith", new Vector3(0f, 5.1f, z), new Vector3(18f, 9f, 0.7f), black, false);
            GKMCUtil.Box(root, "Overhaul_EntryNeonTop", new Vector3(0f, 9.7f, z - 0.42f), new Vector3(19f, 0.16f, 0.14f), neon, false);
            GKMCUtil.Box(root, "Overhaul_EntryNeonBottom", new Vector3(0f, 0.55f, z - 0.42f), new Vector3(19f, 0.16f, 0.14f), neon, false);
            GKMCUtil.Sign(root, "Overhaul_EntryTitle", new Vector3(0f, 6.1f, z - 0.48f), "GOOD KID\nM.A.A.D CITY", new Color(1f, 0.86f, 0.42f), 0.86f, new Vector3(0f, 180f, 0f));
            GKMCUtil.Sign(root, "Overhaul_EntrySub", new Vector3(0f, 3.15f, z - 0.48f), "WALK THE ALBUM", first.accent, 0.34f, new Vector3(0f, 180f, 0f));
            Vinyl(root, new Vector3(-8.6f, 5.3f, z - 0.75f), 2.7f, gold);
            Vinyl(root, new Vector3(8.6f, 5.3f, z - 0.75f), 2.7f, gold);
        }

        static void RetextureWorld(Transform w, TrackInfo ti)
        {
            Transform floor = w.Find("Floor");
            if (floor != null)
            {
                Renderer r = floor.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = NoiseMat($"floor_{ti.number}", Color.Lerp(ti.primary, Color.black, 0.46f), Color.Lerp(ti.accent, Color.white, 0.18f), 0.05f, 0.33f, 0.35f, 12f);
            }

            foreach (Renderer r in w.GetComponentsInChildren<Renderer>())
            {
                if (r == null) continue;
                r.shadowCastingMode = r.sharedMaterial != null && r.sharedMaterial.renderQueue >= 3000 ? ShadowCastingMode.Off : ShadowCastingMode.On;
                r.receiveShadows = true;
            }
        }

        static void AddCinematicPortal(Transform w, TrackInfo ti)
        {
            Material outer = GKMCUtil.Mat(new Color(0.012f, 0.012f, 0.016f), 0.25f, 0.42f);
            Material neon = GKMCUtil.MatEmissive(ti.accent, ti.accent, 1.9f);
            float z = -HalfL + 0.7f;
            float width = WW - 4.5f;

            GKMCUtil.Box(w, "Overhaul_PortalL", new Vector3(-width * 0.5f, 3.7f, z), new Vector3(0.45f, 7.4f, 0.45f), outer, false);
            GKMCUtil.Box(w, "Overhaul_PortalR", new Vector3(width * 0.5f, 3.7f, z), new Vector3(0.45f, 7.4f, 0.45f), outer, false);
            GKMCUtil.Box(w, "Overhaul_PortalTop", new Vector3(0f, 7.55f, z), new Vector3(width + 0.7f, 0.45f, 0.45f), outer, false);
            GKMCUtil.Box(w, "Overhaul_PortalNeonL", new Vector3(-width * 0.5f + 0.28f, 3.7f, z - 0.3f), new Vector3(0.08f, 6.8f, 0.08f), neon, false);
            GKMCUtil.Box(w, "Overhaul_PortalNeonR", new Vector3(width * 0.5f - 0.28f, 3.7f, z - 0.3f), new Vector3(0.08f, 6.8f, 0.08f), neon, false);
            GKMCUtil.Box(w, "Overhaul_PortalNeonTop", new Vector3(0f, 7.25f, z - 0.3f), new Vector3(width - 0.4f, 0.08f, 0.08f), neon, false);

            GKMCUtil.Sign(w, "Overhaul_PortalNumber", new Vector3(0f, 6.1f, z - 0.52f), $"{ti.number:00}", ti.accent, 0.78f, new Vector3(0f, 180f, 0f));
            GKMCUtil.Sign(w, "Overhaul_PortalTheme", new Vector3(0f, 4.9f, z - 0.52f), ti.theme.ToUpperInvariant(), Color.white, 0.27f, new Vector3(0f, 180f, 0f));
        }

        static void AddDepthLayers(Transform w, TrackInfo ti)
        {
            Material left = GKMCUtil.Mat(Color.Lerp(ti.secondary, Color.black, 0.32f), 0.05f, 0.22f);
            Material right = GKMCUtil.Mat(Color.Lerp(ti.primary, Color.black, 0.28f), 0.05f, 0.22f);
            Material trim = GKMCUtil.MatEmissive(ti.accent, ti.accent, 0.38f);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 7; i++)
                {
                    float z = -HalfL + 6f + i * 7.4f;
                    float h = Random.Range(1.6f, 4.8f);
                    float d = Random.Range(1.2f, 4.4f);
                    float x = side * (HalfW - 1.1f - Random.Range(0f, 1.2f));
                    GKMCUtil.Box(w, "Overhaul_SideMass", new Vector3(x, h * 0.5f, z), new Vector3(Random.Range(0.35f, 1.2f), h, d), side < 0 ? left : right, false);
                    if (Random.value > 0.25f)
                        GKMCUtil.Box(w, "Overhaul_SideNeonSlash", new Vector3(x - side * 0.34f, h + 0.18f, z), new Vector3(0.08f, 0.08f, d * 0.8f), trim, false, new Vector3(0f, 0f, Random.Range(-9f, 9f)));
                }
            }

            for (int i = 0; i < 9; i++)
            {
                float z = -HalfL + 4f + i * 5.8f;
                Material scuff = GKMCUtil.MatTransparent(new Color(ti.accent.r, ti.accent.g, ti.accent.b, 0.12f));
                GKMCUtil.Box(w, "Overhaul_TrackGlowScuff", new Vector3(Random.Range(-4f, 4f), 0.105f, z), new Vector3(Random.Range(1.4f, 4.5f), 0.012f, Random.Range(0.08f, 0.24f)), scuff, false, new Vector3(0f, Random.Range(-22f, 22f), 0f));
            }
        }

        static void AddHeroLighting(Transform w, TrackInfo ti)
        {
            Color warm = Color.Lerp(ti.accent, Color.white, 0.22f);
            for (int i = 0; i < 4; i++)
            {
                float z = -17f + i * 11.5f;
                Light spot = GKMCUtil.SpotLight(w, "Overhaul_RakingSpot", new Vector3(i % 2 == 0 ? -13.5f : 13.5f, 8.7f, z),
                    new Vector3(62f, i % 2 == 0 ? 38f : -38f, 0f), warm, 2.6f, 28f, 33f);
                spot.renderMode = LightRenderMode.ForcePixel;
                spot.shadows = i == 1 ? LightShadows.Soft : LightShadows.None;

                LightBeam(w, new Vector3(i % 2 == 0 ? -7.2f : 7.2f, 4.2f, z + 2.8f), new Vector3(26f, i % 2 == 0 ? 36f : -36f, 0f), new Color(ti.accent.r, ti.accent.g, ti.accent.b, 0.1f), 3.4f, 8.5f);
            }
        }

        static void AddTrackWorld(Transform w, TrackInfo ti)
        {
            switch (ti.number)
            {
                case 1: WorldSherane(w, ti); break;
                case 2: WorldVibe(w, ti); break;
                case 3: WorldBackseat(w, ti); break;
                case 4: WorldPeerPressure(w, ti); break;
                case 5: WorldMoneyTrees(w, ti); break;
                case 6: WorldPoeticJustice(w, ti); break;
                case 7: WorldGoodKid(w, ti); break;
                case 8: WorldMaadCity(w, ti); break;
                case 9: WorldSwimmingPools(w, ti); break;
                case 10: WorldSingAboutMe(w, ti); break;
                case 11: WorldReal(w, ti); break;
                case 12: WorldCompton(w, ti); break;
            }
        }

        static void WorldSherane(Transform w, TrackInfo ti)
        {
            Color amber = new Color(1f, 0.54f, 0.2f);
            for (int i = 0; i < 5; i++)
            {
                HouseFront(w, new Vector3(-13.2f, 0f, -18f + i * 9.5f), 90f, Color.Lerp(ti.secondary, Color.black, 0.25f), amber);
                if (i < 4) HouseFront(w, new Vector3(13.2f, 0f, -13f + i * 10.5f), -90f, Color.Lerp(ti.primary, Color.black, 0.2f), amber);
            }
            TireMarks(w, -5.7f, -18f, 6f);
            GKMCUtil.Sign(w, "Overhaul_SheraneWhisper", new Vector3(HalfW - 0.36f, 5.4f, 16f), "PORCH LIGHT / TRAP DOOR", ti.accent, 0.24f, new Vector3(0f, -90f, 0f));
            GKMCUtil.Particles(w, "Overhaul_StreetDust", new Vector3(0f, 1.1f, 2f), new Color(1f, 0.48f, 0.18f, 0.18f), 16f, 0.42f, 6f, new Vector3(WW - 4f, 1f, WL - 4f), new Vector3(0.18f, 0.06f, 0.02f));
        }

        static void WorldVibe(Transform w, TrackInfo ti)
        {
            RingShrine(w, new Vector3(0f, 2.2f, 6f), 7.5f, ti.accent, 5);
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * Random.Range(5f, 10.5f), 1.15f, 6f + Mathf.Sin(a) * Random.Range(5f, 10.5f));
                CandleCluster(w, p, ti.accent);
            }
            LightBeam(w, new Vector3(0f, 5.2f, 6f), new Vector3(0f, 0f, 0f), new Color(0.5f, 1f, 0.85f, 0.12f), 8f, 12f);
        }

        static void WorldBackseat(Transform w, TrackInfo ti)
        {
            Material gold = GKMCUtil.MatEmissive(new Color(1f, 0.72f, 0.08f), new Color(1f, 0.55f, 0.02f), 1.35f, 0.6f, 0.86f);
            for (int i = 0; i < 7; i++)
            {
                float z = -21f + i * 7f;
                GoldFrame(w, new Vector3(0f, 3.8f, z), 10f - i * 0.5f, 6.8f, gold);
            }
            GKMCUtil.Sign(w, "Overhaul_Ego", new Vector3(0f, 7.1f, 16f), "EGO", new Color(1f, 0.84f, 0.2f), 1.35f, new Vector3(0f, 180f, 0f));
            Vinyl(w, new Vector3(0f, 4.2f, 17f), 3.8f, gold);
        }

        static void WorldPeerPressure(Transform w, TrackInfo ti)
        {
            Material shadow = GKMCUtil.MatTransparent(new Color(0f, 0f, 0.02f, 0.62f));
            for (int i = 0; i < 13; i++)
            {
                float z = -21f + i * 3.7f;
                float x = Mathf.Sin(i * 1.7f) * 6.4f;
                Silhouette(w, new Vector3(x, 0f, z), Random.Range(-12f, 12f), shadow, Random.Range(1.7f, 2.5f));
            }
            GKMCUtil.Sign(w, "Overhaul_PeerPressure", new Vector3(0f, 6.6f, 20f), "THE HOMIES PULL HARDER", ti.accent, 0.42f, new Vector3(0f, 180f, 0f));
        }

        static void WorldMoneyTrees(Transform w, TrackInfo ti)
        {
            Material bill = GKMCUtil.MatEmissive(new Color(0.4f, 0.85f, 0.35f), new Color(0.22f, 0.9f, 0.28f), 0.42f);
            for (int i = 0; i < 70; i++)
            {
                Vector3 p = new Vector3(Random.Range(-13f, 13f), Random.Range(2.4f, 7.5f), Random.Range(-20f, 22f));
                GameObject b = GKMCUtil.Box(w, "Overhaul_FloatingBill", p, new Vector3(0.55f, 0.018f, 0.24f), bill, false, new Vector3(Random.Range(-25f, 25f), Random.Range(0f, 180f), Random.Range(-25f, 25f)));
                Bobber bob = b.AddComponent<Bobber>(); bob.amplitude = Random.Range(0.12f, 0.45f); bob.sway = Random.Range(0.05f, 0.22f); bob.speed = Random.Range(0.45f, 1.2f);
            }
            SunDisc(w, new Vector3(0f, 8.7f, 20f), 4.2f, new Color(1f, 0.82f, 0.18f));
        }

        static void WorldPoeticJustice(Transform w, TrackInfo ti)
        {
            Material strip = GKMCUtil.Mat(new Color(0.015f, 0.012f, 0.018f), 0.35f, 0.55f);
            Material rose = GKMCUtil.MatEmissive(new Color(1f, 0.24f, 0.55f), new Color(1f, 0.18f, 0.42f), 0.55f);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 8; i++)
                {
                    float z = -20f + i * 6f;
                    GKMCUtil.Box(w, "Overhaul_FilmStrip", new Vector3(side * 9.4f, 3.6f, z), new Vector3(0.14f, 3.5f, 4f), strip, false);
                    for (int f = -1; f <= 1; f++) GKMCUtil.Box(w, "Overhaul_FilmFrame", new Vector3(side * 9.3f, 3.6f, z + f * 1.15f), new Vector3(0.09f, 1.0f, 0.72f), rose, false);
                }
            }
            RingShrine(w, new Vector3(0f, 2.4f, 4f), 6.2f, ti.accent, 4);
        }

        static void WorldGoodKid(Transform w, TrackInfo ti)
        {
            Material cold = GKMCUtil.MatEmissive(ti.accent, ti.accent, 0.72f);
            for (int z = -20; z <= 20; z += 4)
            {
                GKMCUtil.Box(w, "Overhaul_SystemGridX", new Vector3(0f, 0.115f, z), new Vector3(WW - 3f, 0.018f, 0.035f), cold, false);
            }
            for (int x = -14; x <= 14; x += 4)
            {
                GKMCUtil.Box(w, "Overhaul_SystemGridZ", new Vector3(x, 0.12f, 0f), new Vector3(0.035f, 0.018f, WL - 4f), cold, false);
            }
            for (int i = 0; i < 5; i++)
            {
                LightBeam(w, new Vector3(-8f + i * 4f, 4.8f, -12f + i * 7f), new Vector3(0f, Random.Range(-18f, 18f), 0f), new Color(0.5f, 0.72f, 1f, 0.12f), 2.8f, 8f);
            }
        }

        static void WorldMaadCity(Transform w, TrackInfo ti)
        {
            Material red = GKMCUtil.MatEmissive(new Color(1f, 0.08f, 0.03f), new Color(1f, 0.04f, 0f), 1.2f);
            Material blue = GKMCUtil.MatEmissive(new Color(0.08f, 0.25f, 1f), new Color(0.05f, 0.18f, 1f), 1.2f);
            for (int i = 0; i < 8; i++)
            {
                float z = -21f + i * 6f;
                GKMCUtil.Box(w, "Overhaul_SirenSlashRed", new Vector3(-HalfW + 0.4f, 4f, z), new Vector3(0.08f, 0.16f, 5f), red, false, new Vector3(0f, 0f, 22f));
                GKMCUtil.Box(w, "Overhaul_SirenSlashBlue", new Vector3(HalfW - 0.4f, 4f, z + 2f), new Vector3(0.08f, 0.16f, 5f), blue, false, new Vector3(0f, 0f, -22f));
            }
            GKMCUtil.Sign(w, "Overhaul_MaadMural", new Vector3(0f, 6.6f, 21f), "M.A.A.D CITY", new Color(1f, 0.18f, 0.08f), 0.95f, new Vector3(0f, 180f, 0f));
            GKMCUtil.Particles(w, "Overhaul_AshFall", new Vector3(0f, 8f, 0f), new Color(0.18f, 0.15f, 0.13f, 0.5f), 26f, 0.16f, 7f, new Vector3(WW, 1f, WL), new Vector3(0.04f, -0.75f, 0.02f));
        }

        static void WorldSwimmingPools(Transform w, TrackInfo ti)
        {
            Material blue = GKMCUtil.MatTransparent(new Color(0.05f, 0.48f, 1f, 0.14f));
            for (int i = 0; i < 9; i++)
            {
                float z = -19f + i * 5f;
                GKMCUtil.Box(w, "Overhaul_WaterSheet", new Vector3(0f, 2.1f + Mathf.Sin(i) * 0.28f, z), new Vector3(WW - 5f, 0.025f, 2.8f), blue, false, new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-3f, 3f)));
            }
            GKMCUtil.Sign(w, "Overhaul_DrinkMural", new Vector3(0f, 6.5f, 20f), "DROWN / FLOAT / REPEAT", ti.accent, 0.38f, new Vector3(0f, 180f, 0f));
        }

        static void WorldSingAboutMe(Transform w, TrackInfo ti)
        {
            Material warm = GKMCUtil.MatEmissive(new Color(1f, 0.72f, 0.34f), new Color(1f, 0.52f, 0.15f), 0.52f);
            for (int i = 0; i < 20; i++)
            {
                float x = Random.Range(-11f, 11f);
                float z = Random.Range(-20f, 12f);
                GravestoneSilhouette(w, new Vector3(x, 0f, z), warm);
            }
            LightBeam(w, new Vector3(0f, 5.6f, 16f), Vector3.zero, new Color(1f, 0.82f, 0.46f, 0.17f), 7f, 12f);
            GKMCUtil.Sign(w, "Overhaul_Prayer", new Vector3(0f, 7f, 18.5f), "SING ABOUT ME", new Color(1f, 0.78f, 0.36f), 0.56f, new Vector3(0f, 180f, 0f));
        }

        static void WorldReal(Transform w, TrackInfo ti)
        {
            Material mirror = GKMCUtil.Mat(new Color(0.82f, 0.82f, 0.88f), 0.92f, 0.96f);
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 11f, 3.3f, 4f + Mathf.Sin(a) * 11f);
                GKMCUtil.Box(w, "Overhaul_TruthBlade", p, new Vector3(0.18f, 5.7f, 2.2f), mirror, false, new Vector3(0f, -a * Mathf.Rad2Deg + 90f, Random.Range(-3f, 3f)));
            }
            RingShrine(w, new Vector3(0f, 3.4f, 4f), 8f, ti.accent, 6);
            GKMCUtil.Sign(w, "Overhaul_Real", new Vector3(0f, 7.3f, 4f), "REAL", ti.accent, 0.88f, new Vector3(0f, 180f, 0f));
        }

        static void WorldCompton(Transform w, TrackInfo ti)
        {
            Material sun = GKMCUtil.MatEmissive(new Color(1f, 0.77f, 0.16f), new Color(1f, 0.56f, 0.05f), 1.1f);
            SunDisc(w, new Vector3(0f, 9.2f, 22f), 5f, new Color(1f, 0.78f, 0.2f));
            for (int i = 0; i < 9; i++)
            {
                float z = -22f + i * 5.5f;
                PalmSilhouette(w, new Vector3(Random.value > 0.5f ? -14f : 14f, 0f, z), Random.Range(7f, 11f));
            }
            GKMCUtil.Box(w, "Overhaul_VictoryStage", new Vector3(0f, 0.24f, 17f), new Vector3(18f, 0.32f, 8f), GKMCUtil.Mat(new Color(0.04f, 0.04f, 0.045f), 0.2f, 0.48f), false);
            GKMCUtil.Sign(w, "Overhaul_ComptonFinal", new Vector3(0f, 6.9f, 22.2f), "COMPTON", sun.color, 1.15f, new Vector3(0f, 180f, 0f));
        }

        static Material NoiseMat(string key, Color baseColor, Color detailColor, float metallic, float smoothness, float contrast, float tiling)
        {
            string matKey = key + baseColor + detailColor + metallic + smoothness + contrast + tiling;
            if (_materials.TryGetValue(matKey, out Material cached)) return cached;

            Material m = GKMCUtil.Mat(baseColor, metallic, smoothness);
            Texture2D tex = NoiseTexture(key, baseColor, detailColor, contrast);
            m.mainTexture = tex;
            m.SetTextureScale("_MainTex", new Vector2(tiling, tiling));
            _materials[matKey] = m;
            return m;
        }

        static Texture2D NoiseTexture(string key, Color baseColor, Color detailColor, float contrast)
        {
            if (_textures.TryGetValue(key, out Texture2D cached)) return cached;

            const int size = 96;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            int seed = Mathf.Abs(key.GetHashCode()) % 997;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n1 = Mathf.PerlinNoise((x + seed) * 0.075f, (y - seed) * 0.075f);
                    float n2 = Mathf.PerlinNoise((x - seed) * 0.19f, (y + seed) * 0.19f);
                    float n = Mathf.Clamp01((n1 * 0.72f + n2 * 0.28f - 0.5f) * contrast + 0.5f);
                    tex.SetPixel(x, y, Color.Lerp(baseColor, detailColor, n));
                }
            }
            tex.Apply(false, true);
            _textures[key] = tex;
            return tex;
        }

        static void LightBeam(Transform parent, Vector3 pos, Vector3 euler, Color color, float width, float height)
        {
            Material m = GKMCUtil.MatTransparent(color);
            GKMCUtil.Box(parent, "Overhaul_LightBeam", pos, new Vector3(width, height, 0.08f), m, false, euler);
        }

        static void Vinyl(Transform parent, Vector3 pos, float radius, Material accent)
        {
            Material black = GKMCUtil.Mat(new Color(0.006f, 0.006f, 0.008f), 0.35f, 0.72f);
            GKMCUtil.Cyl(parent, "Overhaul_Vinyl", pos, new Vector3(radius, 0.08f, radius), black, false, new Vector3(90f, 0f, 0f));
            GKMCUtil.Cyl(parent, "Overhaul_VinylLabel", pos + new Vector3(0f, 0f, -0.08f), new Vector3(radius * 0.28f, 0.09f, radius * 0.28f), accent, false, new Vector3(90f, 0f, 0f));
        }

        static void HouseFront(Transform w, Vector3 pos, float yaw, Color color, Color light)
        {
            Material wall = GKMCUtil.Mat(color, 0f, 0.2f);
            Material glass = GKMCUtil.MatEmissive(Color.Lerp(light, Color.black, 0.35f), light, 0.6f);
            GKMCUtil.Box(w, "Overhaul_HouseFront", pos + new Vector3(0f, 2.2f, 0f), new Vector3(0.55f, 4.4f, 6f), wall, false, new Vector3(0f, yaw, 0f));
            GKMCUtil.Box(w, "Overhaul_HouseWindow", pos + new Vector3(yaw > 0 ? -0.34f : 0.34f, 2.7f, -1.4f), new Vector3(0.08f, 1.1f, 1.3f), glass, false, new Vector3(0f, yaw, 0f));
            GKMCUtil.Box(w, "Overhaul_HouseDoor", pos + new Vector3(yaw > 0 ? -0.34f : 0.34f, 1.45f, 1.5f), new Vector3(0.08f, 2.4f, 1.1f), GKMCUtil.Mat(new Color(0.06f, 0.035f, 0.025f)), false, new Vector3(0f, yaw, 0f));
        }

        static void TireMarks(Transform w, float x, float z0, float z1)
        {
            Material mark = GKMCUtil.MatTransparent(new Color(0f, 0f, 0f, 0.33f));
            for (int i = 0; i < 2; i++)
                GKMCUtil.Box(w, "Overhaul_TireMark", new Vector3(x + i * 1.2f, 0.11f, (z0 + z1) * 0.5f), new Vector3(0.28f, 0.012f, Mathf.Abs(z1 - z0)), mark, false, new Vector3(0f, 8f, 0f));
        }

        static void RingShrine(Transform w, Vector3 center, float radius, Color color, int rings)
        {
            Material ring = GKMCUtil.MatEmissive(color, color, 0.65f);
            for (int i = 0; i < rings; i++)
            {
                float r = radius - i * 0.78f;
                for (int s = 0; s < 20; s++)
                {
                    float a = s / 20f * Mathf.PI * 2f;
                    Vector3 p = center + new Vector3(Mathf.Cos(a) * r, i * 0.12f, Mathf.Sin(a) * r);
                    GKMCUtil.Box(w, "Overhaul_RingSegment", p, new Vector3(0.58f, 0.055f, 0.08f), ring, false, new Vector3(0f, -a * Mathf.Rad2Deg, 0f));
                }
            }
        }

        static void CandleCluster(Transform w, Vector3 pos, Color color)
        {
            Material wax = GKMCUtil.Mat(new Color(0.88f, 0.83f, 0.72f), 0f, 0.42f);
            Material flame = GKMCUtil.MatEmissive(new Color(1f, 0.58f, 0.16f), new Color(1f, 0.35f, 0.06f), 2.4f);
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = pos + new Vector3((i - 1) * 0.18f, 0f, Random.Range(-0.12f, 0.12f));
                GKMCUtil.Cyl(w, "Overhaul_CandleWax", p + new Vector3(0f, 0.22f, 0f), new Vector3(0.09f, Random.Range(0.16f, 0.38f), 0.09f), wax, false);
                GKMCUtil.Sphere(w, "Overhaul_CandleFlame", p + new Vector3(0f, 0.62f, 0f), Vector3.one * 0.08f, flame, false);
            }
            if (Random.value > 0.55f) GKMCUtil.PointLight(w, "Overhaul_CandlePool", pos + new Vector3(0f, 0.75f, 0f), color, 0.38f, 4.8f);
        }

        static void GoldFrame(Transform w, Vector3 center, float width, float height, Material mat)
        {
            GKMCUtil.Box(w, "Overhaul_GoldFrameTop", center + new Vector3(0f, height * 0.5f, 0f), new Vector3(width, 0.15f, 0.16f), mat, false);
            GKMCUtil.Box(w, "Overhaul_GoldFrameBottom", center + new Vector3(0f, -height * 0.5f, 0f), new Vector3(width, 0.15f, 0.16f), mat, false);
            GKMCUtil.Box(w, "Overhaul_GoldFrameLeft", center + new Vector3(-width * 0.5f, 0f, 0f), new Vector3(0.15f, height, 0.16f), mat, false);
            GKMCUtil.Box(w, "Overhaul_GoldFrameRight", center + new Vector3(width * 0.5f, 0f, 0f), new Vector3(0.15f, height, 0.16f), mat, false);
        }

        static void Silhouette(Transform w, Vector3 pos, float yaw, Material mat, float height)
        {
            GKMCUtil.Cyl(w, "Overhaul_ShadowBody", pos + new Vector3(0f, height * 0.5f, 0f), new Vector3(0.28f, height * 0.5f, 0.28f), mat, false, new Vector3(0f, yaw, 0f));
            GKMCUtil.Sphere(w, "Overhaul_ShadowHead", pos + new Vector3(0f, height + 0.22f, 0f), Vector3.one * 0.34f, mat, false);
            GKMCUtil.Box(w, "Overhaul_ShadowShoulders", pos + new Vector3(0f, height * 0.72f, 0f), new Vector3(1.0f, 0.18f, 0.22f), mat, false, new Vector3(0f, yaw, 0f));
        }

        static void SunDisc(Transform w, Vector3 pos, float radius, Color color)
        {
            Material sun = GKMCUtil.MatEmissive(color, color, 1.8f);
            GKMCUtil.Cyl(w, "Overhaul_SunDisc", pos, new Vector3(radius, 0.08f, radius), sun, false, new Vector3(90f, 0f, 0f));
            GKMCUtil.PointLight(w, "Overhaul_SunDiscGlow", pos + new Vector3(0f, 0f, -2f), color, 1.6f, 32f);
        }

        static void GravestoneSilhouette(Transform w, Vector3 pos, Material mat)
        {
            GKMCUtil.Box(w, "Overhaul_GravestoneBase", pos + new Vector3(0f, 0.55f, 0f), new Vector3(0.75f, 1.1f, 0.22f), mat, false, new Vector3(0f, Random.Range(-8f, 8f), 0f));
            GKMCUtil.Sphere(w, "Overhaul_GravestoneTop", pos + new Vector3(0f, 1.16f, 0f), new Vector3(0.42f, 0.25f, 0.12f), mat, false);
        }

        static void PalmSilhouette(Transform w, Vector3 pos, float height)
        {
            Material mat = GKMCUtil.Mat(new Color(0.025f, 0.035f, 0.026f), 0f, 0.24f);
            GKMCUtil.Cyl(w, "Overhaul_PalmTrunk", pos + new Vector3(0f, height * 0.5f, 0f), new Vector3(0.18f, height * 0.5f, 0.18f), mat, false, new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f)));
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * 360f;
                GKMCUtil.Box(w, "Overhaul_PalmFrond", pos + new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 1.35f, height + Mathf.Sin(i) * 0.18f, Mathf.Sin(a * Mathf.Deg2Rad) * 1.35f), new Vector3(0.28f, 0.12f, 2.8f), mat, false, new Vector3(18f, a, 0f));
            }
        }
    }
}
