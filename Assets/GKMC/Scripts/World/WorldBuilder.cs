using System.Collections.Generic;
using UnityEngine;

namespace GKMC
{
    /// <summary>
    /// Procedurally builds all twelve track-worlds in a single linear tour along +Z.
    /// Each world is themed to its song; you walk the album from track 1 to 12.
    /// Hero props (cars, trees, heart, barrels…) are built as primitives first, then upgraded to
    /// Meshy-generated/community models through <see cref="ModelLibrary"/> when one is present.
    /// </summary>
    public static class WorldBuilder
    {
        static float WL => AlbumData.WorldLength;
        static float WW => AlbumData.WorldWidth;
        static float WH => AlbumData.WallHeight;
        static float HalfW => WW * 0.5f;
        static float HalfL => WL * 0.5f;

        public static void BuildAll(List<TrackInfo> tracks, Transform root)
        {
            BuildShell(tracks, root);
            foreach (var ti in tracks)
                BuildWorld(ti, root);
        }

        static void BuildShell(List<TrackInfo> tracks, Transform root)
        {
            float startZ = -HalfL - 4f;
            float endZ = (tracks.Count - 1) * WL + HalfL + 4f;
            float midZ = (startZ + endZ) * 0.5f;
            float totalLen = endZ - startZ;

            var wallMat = GKMCUtil.Mat(new Color(0.06f, 0.06f, 0.08f), 0f, 0.1f);
            GKMCUtil.Box(root, "Wall_L", new Vector3(-HalfW - 0.5f, WH * 0.5f, midZ), new Vector3(1f, WH, totalLen), wallMat);
            GKMCUtil.Box(root, "Wall_R", new Vector3(HalfW + 0.5f, WH * 0.5f, midZ), new Vector3(1f, WH, totalLen), wallMat);
            GKMCUtil.Box(root, "Cap_Start", new Vector3(0f, WH * 0.5f, startZ), new Vector3(WW + 2f, WH, 1f), wallMat);
            GKMCUtil.Box(root, "Cap_End", new Vector3(0f, WH * 0.5f, endZ), new Vector3(WW + 2f, WH, 1f), wallMat);

            GKMCUtil.Sign(root, "AlbumTitle", new Vector3(0f, 6.5f, startZ + 0.6f),
                AlbumData.Album + "\n" + AlbumData.Artist + "\n— a walkable tour —",
                new Color(1f, 0.85f, 0.4f), 0.9f, Vector3.zero);
        }

        static void BuildWorld(TrackInfo ti, Transform root)
        {
            var world = new GameObject($"World_{ti.number:00}_{Safe(ti.title)}");
            world.transform.SetParent(root, false);
            world.transform.localPosition = new Vector3(0f, 0f, ti.ZCenter);
            var w = world.transform;

            var floorMat = GKMCUtil.Mat(GKMCUtil.Dark(ti.primary, 0.7f), 0.1f, 0.2f);
            var wallMat = GKMCUtil.Mat(GKMCUtil.Dark(ti.secondary, 0.8f), 0.1f, 0.15f);
            var accentMat = GKMCUtil.MatEmissive(ti.accent, ti.accent, 2.6f);

            GKMCUtil.Box(w, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(WW, 0.5f, WL), floorMat);

            if (ti.number > 1)
                Gateway(w, new Vector3(0f, 0f, -HalfL), accentMat);

            float zEntry = -HalfL + 5f;
            GKMCUtil.Box(w, "Pedestal", new Vector3(0f, 0.6f, zEntry), new Vector3(5f, 1.2f, 0.6f), wallMat);
            GKMCUtil.Sign(w, "TrackNum", new Vector3(0f, 2.6f, zEntry + 0.4f), $"{ti.number:00}", ti.accent, 1.4f);
            string title = ti.title + (string.IsNullOrEmpty(ti.feature) ? "" : "\n" + ti.feature);
            GKMCUtil.Sign(w, "TrackTitle", new Vector3(0f, 1.4f, zEntry + 0.4f), title, Color.white, 0.42f);
            GKMCUtil.Sign(w, "ThemeChip", new Vector3(0f, 0.55f, zEntry + 0.4f), "[ " + ti.theme + " ]", ti.accent, 0.34f);

            WorldTrigger.Create(w, ti.number, new Vector3(0f, WH * 0.5f, 0f), new Vector3(WW, WH, WL));

            Decorate(ti, w, accentMat);
        }

        static void Gateway(Transform w, Vector3 pos, Material accent)
        {
            var frame = GKMCUtil.Mat(new Color(0.04f, 0.04f, 0.05f), 0.4f, 0.4f);
            GKMCUtil.Box(w, "Gate_L", pos + new Vector3(-HalfW + 1.5f, WH * 0.4f, 0f), new Vector3(1f, WH * 0.8f, 1f), frame);
            GKMCUtil.Box(w, "Gate_R", pos + new Vector3(HalfW - 1.5f, WH * 0.4f, 0f), new Vector3(1f, WH * 0.8f, 1f), frame);
            GKMCUtil.Box(w, "Gate_Top", pos + new Vector3(0f, WH * 0.8f, 0f), new Vector3(WW - 1f, 0.6f, 0.6f), accent, false);
        }

        static void Decorate(TrackInfo ti, Transform w, Material accent)
        {
            switch (ti.number)
            {
                case 1: World1_Sherane(ti, w); break;
                case 2: World2_Vibe(ti, w); break;
                case 3: World3_Backseat(ti, w); break;
                case 4: World4_PeerPressure(ti, w, accent); break;
                case 5: World5_MoneyTrees(ti, w); break;
                case 6: World6_PoeticJustice(ti, w); break;
                case 7: World7_GoodKid(ti, w); break;
                case 8: World8_MaadCity(ti, w, accent); break;
                case 9: World9_SwimmingPools(ti, w); break;
                case 10: World10_SingAboutMe(ti, w); break;
                case 11: World11_Real(ti, w); break;
                case 12: World12_Compton(ti, w); break;
            }
        }

        // ============================== WORLDS ==============================

        // 1 — Sherane: dusk street, parked car, a house with a porch light, hooded figures (the trap).
        static void World1_Sherane(TrackInfo ti, Transform w)
        {
            var asphalt = GKMCUtil.Mat(new Color(0.05f, 0.05f, 0.06f), 0.1f, 0.3f);
            GKMCUtil.Box(w, "Street", new Vector3(0f, 0.03f, 0f), new Vector3(8f, 0.05f, WL - 6f), asphalt, false);
            for (int i = -2; i <= 2; i++)
                GKMCUtil.Box(w, "Lane", new Vector3(0f, 0.06f, i * 8f), new Vector3(0.4f, 0.02f, 3f),
                    GKMCUtil.MatEmissive(Color.yellow, Color.yellow, 1.2f), false);

            Car(w, new Vector3(-6f, 0f, -6f), new Color(0.25f, 0.03f, 0.04f), 0f);
            House(w, new Vector3(9f, 0f, 18f), new Vector3(11f, 7f, 7f), new Color(1f, 0.7f, 0.35f));
            var porch = GKMCUtil.PointLight(w, "PorchLight", new Vector3(6.5f, 2.6f, 14f), new Color(1f, 0.6f, 0.3f), 3.5f, 14f);
            GKMCUtil.Particles(w, "Moths", new Vector3(6.5f, 2.6f, 14f), new Color(1f, 0.8f, 0.5f),
                12f, 0.12f, 3f, new Vector3(2f, 2f, 2f), new Vector3(0f, 0.3f, 0f));

            for (int i = 0; i < 3; i++)
                GKMCUtil.Prim(PrimitiveType.Capsule, w, "Figure", new Vector3(2f + i * 2f, 1f, 19f + i),
                    new Vector3(0.8f, 1f, 0.8f), GKMCUtil.Mat(new Color(0.02f, 0.02f, 0.03f)), false);
            GKMCUtil.PointLight(w, "Dusk", new Vector3(0f, 9f, 0f), ti.accent, 0.8f, 40f);
        }

        // 2 — Bitch Don't Kill My Vibe: smoke, drifting candles, a meditative haze.
        static void World2_Vibe(TrackInfo ti, Transform w)
        {
            GKMCUtil.Particles(w, "Smoke", new Vector3(0f, 1.5f, 0f), new Color(0.4f, 0.8f, 0.7f, 0.25f),
                18f, 3.5f, 8f, new Vector3(WW - 4f, 3f, WL - 6f), new Vector3(0.2f, 0.25f, 0f));
            for (int i = 0; i < 9; i++)
                Candle(w, new Vector3(Random.Range(-12f, 12f), 1.6f, Random.Range(-16f, 18f)), ti.accent, bob: true);
            GKMCUtil.Cyl(w, "Mat", new Vector3(0f, 0.05f, 6f), new Vector3(4f, 0.05f, 4f),
                GKMCUtil.Mat(GKMCUtil.Dark(ti.secondary, 1.1f)), false);
            GKMCUtil.PointLight(w, "Glow", new Vector3(0f, 4f, 6f), ti.accent, 1.6f, 22f);
        }

        // 3 — Backseat Freestyle: gold hall of mirrors, ego pillars, a little Eiffel Tower.
        static void World3_Backseat(TrackInfo ti, Transform w)
        {
            var mirror = GKMCUtil.Mat(new Color(0.7f, 0.72f, 0.78f), 0.95f, 0.95f);
            var gold = GKMCUtil.MatEmissive(new Color(1f, 0.78f, 0.1f), new Color(1f, 0.7f, 0.05f), 1.6f, 0.9f, 0.9f);
            for (int i = 0; i < 6; i++)
            {
                float z = -18f + i * 7f;
                GKMCUtil.Box(w, "MirrorL", new Vector3(-9f, 4f, z), new Vector3(0.4f, 8f, 4f), mirror);
                GKMCUtil.Box(w, "MirrorR", new Vector3(9f, 4f, z), new Vector3(0.4f, 8f, 4f), mirror);
            }
            for (int i = 0; i < 5; i++)
                GKMCUtil.Cyl(w, "EgoPillar", new Vector3(-4f + i * 2f, (1f + i), 12f), new Vector3(0.7f, 1f + i, 0.7f), gold);
            for (int i = 0; i < 8; i++)
                GKMCUtil.Sphere(w, "ChainLink", new Vector3(-7f, 7f - i * 0.6f, -2f), Vector3.one * 0.5f, gold);
            EiffelTower(w, new Vector3(7f, 0f, 14f), gold);
            GKMCUtil.Particles(w, "Sparkle", new Vector3(0f, 6f, 0f), new Color(1f, 0.85f, 0.2f),
                25f, 0.15f, 4f, new Vector3(WW - 6f, 6f, WL - 6f), new Vector3(0f, -0.2f, 0f));
            GKMCUtil.PointLight(w, "GoldGlow", new Vector3(0f, 7f, 0f), new Color(1f, 0.8f, 0.2f), 2.2f, 35f);
        }

        // 4 — The Art of Peer Pressure: a path of silhouettes pulling you toward a house that isn't yours.
        static void World4_PeerPressure(TrackInfo ti, Transform w, Material accent)
        {
            var fig = GKMCUtil.Mat(new Color(0.05f, 0.06f, 0.1f));
            for (int i = 0; i < 8; i++)
            {
                float z = -16f + i * 4.5f;
                float x = (i % 2 == 0) ? -3.5f : 3.5f;
                var g = GKMCUtil.Prim(PrimitiveType.Capsule, w, "Homie", new Vector3(x, 1f, z),
                    new Vector3(0.8f, 1f, 0.8f), fig, false);
                var b = g.AddComponent<Bobber>(); b.amplitude = 0.12f; b.speed = 2.2f;
            }
            House(w, new Vector3(0f, 0f, 19f), new Vector3(12f, 6f, 6f), ti.accent);
            var lamp = GKMCUtil.PointLight(w, "Streetlight", new Vector3(-8f, 6f, 0f), new Color(0.7f, 0.78f, 1f), 2f, 18f);
            lamp.gameObject.AddComponent<FireFlicker>().light = lamp;
            GKMCUtil.Cyl(w, "LampPost", new Vector3(-8f, 3f, 0f), new Vector3(0.2f, 3f, 0.2f),
                GKMCUtil.Mat(new Color(0.1f, 0.1f, 0.12f)));
        }

        // 5 — Money Trees: sunlit backyard, trees that grow money, a kiddie pool, a backyard lowrider.
        static void World5_MoneyTrees(TrackInfo ti, Transform w)
        {
            var grass = GKMCUtil.Mat(new Color(0.15f, 0.3f, 0.12f));
            GKMCUtil.Box(w, "Lawn", new Vector3(0f, 0.02f, 0f), new Vector3(WW - 2f, 0.04f, WL - 4f), grass, false);
            Vector3[] spots = {
                new Vector3(-8f,0f,-10f), new Vector3(9f,0f,-4f),
                new Vector3(-7f,0f,8f), new Vector3(8f,0f,14f), new Vector3(0f,0f,18f)
            };
            foreach (var s in spots) MoneyTree(w, s);
            GKMCUtil.Cyl(w, "PoolRim", new Vector3(2f, 0.2f, 2f), new Vector3(3.4f, 0.2f, 3.4f), GKMCUtil.Mat(new Color(0.8f, 0.3f, 0.2f)));
            GKMCUtil.Cyl(w, "PoolWater", new Vector3(2f, 0.35f, 2f), new Vector3(3.1f, 0.05f, 3.1f),
                GKMCUtil.MatTransparent(new Color(0.3f, 0.7f, 1f, 0.6f)), false);
            GKMCUtil.Box(w, "Chair", new Vector3(-3f, 0.4f, 3f), new Vector3(1.6f, 0.1f, 0.8f), GKMCUtil.Mat(new Color(0.9f, 0.9f, 0.95f)));
            // A gold lowrider easing in the driveway.
            Lowrider(w, new Vector3(-10f, 0f, 2f), 25f, new Color(0.85f, 0.65f, 0.1f), LowriderHop.Style.SideToSide);
            GKMCUtil.PointLight(w, "Sun", new Vector3(0f, 12f, 0f), new Color(1f, 0.95f, 0.7f), 1.4f, 50f);
        }

        // 6 — Poetic Justice: rose petals, a slow-dance disc, film reels (Janet's film).
        static void World6_PoeticJustice(TrackInfo ti, Transform w)
        {
            GKMCUtil.Particles(w, "Petals", new Vector3(0f, 8f, 0f), new Color(1f, 0.55f, 0.75f),
                14f, 0.25f, 7f, new Vector3(WW - 4f, 1f, WL - 6f), new Vector3(0.3f, -0.6f, 0f));
            GKMCUtil.Cyl(w, "DanceFloor", new Vector3(0f, 0.06f, 4f), new Vector3(7f, 0.06f, 7f),
                GKMCUtil.MatEmissive(new Color(0.6f, 0.2f, 0.4f), new Color(0.9f, 0.4f, 0.6f), 0.6f, 0.6f, 0.8f), false);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                GKMCUtil.PointLight(w, "RoseLight", new Vector3(Mathf.Cos(a) * 6f, 2.5f, 4f + Mathf.Sin(a) * 6f),
                    new Color(1f, 0.5f, 0.7f), 1.2f, 10f);
            }
            for (int s = -1; s <= 1; s += 2)
            {
                GKMCUtil.Cyl(w, "ReelPost", new Vector3(10f * s, 2.5f, 14f), new Vector3(0.3f, 2.5f, 0.3f), GKMCUtil.Mat(new Color(0.1f, 0.1f, 0.1f)));
                var reel = GKMCUtil.Cyl(w, "FilmReel", new Vector3(10f * s, 5f, 14f), new Vector3(2.5f, 0.2f, 2.5f),
                    GKMCUtil.Mat(new Color(0.15f, 0.15f, 0.15f), 0.6f, 0.5f), false, new Vector3(90f, 0f, 0f));
                reel.AddComponent<Spinner>().axis = Vector3.up;
            }
            GKMCUtil.PointLight(w, "Sunset", new Vector3(0f, 8f, -10f), new Color(1f, 0.5f, 0.7f), 1.5f, 40f);
        }

        // 7 — good kid: chain-link, surveillance cameras, cages, cold institutional light (choppers above).
        static void World7_GoodKid(TrackInfo ti, Transform w)
        {
            for (int i = 0; i < 5; i++)
            {
                float z = -18f + i * 8f;
                FencePanel(w, new Vector3(-11f, 0f, z));
                FencePanel(w, new Vector3(11f, 0f, z));
            }
            FencePanel(w, new Vector3(-2.5f, 0f, 6f), 90f);
            FencePanel(w, new Vector3(2.5f, 0f, 6f), 90f);
            FencePanel(w, new Vector3(0f, 0f, 3.5f));
            FencePanel(w, new Vector3(0f, 0f, 8.5f));
            Vector3[] camSpots = { new Vector3(-9f, 6f, -10f), new Vector3(9f, 6f, 2f), new Vector3(-9f, 6f, 14f) };
            foreach (var s in camSpots) SurveillanceCamera(w, s);
            for (int s = -1; s <= 1; s += 2)
            {
                var spot = GKMCUtil.SpotLight(w, "Searchlight", new Vector3(8f * s, 12f, -6f),
                    new Vector3(70f, 0f, 0f), new Color(0.8f, 0.9f, 1f), 6f, 30f, 30f);
                var sweep = spot.gameObject.AddComponent<Sweeper>();
                sweep.axis = Vector3.right; sweep.range = 22f; sweep.speed = 0.7f;
            }
            GKMCUtil.PointLight(w, "Cold", new Vector3(0f, 9f, 0f), ti.accent, 0.7f, 40f);
        }

        // 8 — m.A.A.d city: fire barrels, graffiti, broken glass, sirens, an overturned car, a hopping lowrider.
        static void World8_MaadCity(TrackInfo ti, Transform w, Material accent)
        {
            Vector3[] barrels = { new Vector3(-8f,0f,-12f), new Vector3(7f,0f,-2f), new Vector3(-6f,0f,10f), new Vector3(9f,0f,16f) };
            foreach (var b in barrels) FireBarrel(w, b);
            for (int s = -1; s <= 1; s += 2)
            {
                GKMCUtil.Box(w, "GraffitiWall", new Vector3(12f * s, 3f, -4f), new Vector3(0.4f, 6f, 14f), GKMCUtil.Mat(new Color(0.1f, 0.05f, 0.05f)));
                GKMCUtil.Box(w, "Tag", new Vector3(11.7f * s, 3f, -4f), new Vector3(0.1f, 2.5f, 8f), accent, false);
            }
            // Personal spray-paint tag on the left wall.
            GKMCUtil.Sign(w, "GraffitiBosh", new Vector3(-11.55f, 3.5f, -4f), "BOSH\nWAS HERE",
                new Color(0.3f, 1f, 0.45f), 0.9f, new Vector3(0f, 90f, 6f));
            GKMCUtil.PointLight(w, "TagGlow", new Vector3(-9f, 3.5f, -4f), new Color(0.3f, 1f, 0.45f), 1.4f, 9f);
            for (int i = 0; i < 40; i++)
                GKMCUtil.Box(w, "Glass", new Vector3(Random.Range(-13f, 13f), 0.05f, Random.Range(-20f, 20f)),
                    Vector3.one * Random.Range(0.05f, 0.18f), GKMCUtil.MatEmissive(new Color(1f, 0.6f, 0.4f), new Color(1f, 0.4f, 0.2f), 0.6f),
                    false, new Vector3(Random.value * 90f, Random.value * 90f, Random.value * 90f));
            Car(w, new Vector3(-4f, 1.2f, 4f), new Color(0.1f, 0.1f, 0.12f), 0f, roll: 150f);
            // A defiant lowrider hopping in the chaos.
            Lowrider(w, new Vector3(5f, 0f, 10f), -20f, new Color(0.6f, 0.05f, 0.05f), LowriderHop.Style.ThreeWheel);
            for (int s = -1; s <= 1; s += 2)
            {
                var sir = GKMCUtil.PointLight(w, "Siren", new Vector3(6f * s, 5f, 0f), Color.red, 3f, 22f);
                var fl = sir.gameObject.AddComponent<Flasher>(); fl.light = sir;
                fl.a = Color.red; fl.b = new Color(0.2f, 0.3f, 1f); fl.speed = 5f + s;
            }
            GKMCUtil.Particles(w, "Embers", new Vector3(0f, 1f, 0f), new Color(1f, 0.4f, 0.1f),
                30f, 0.12f, 4f, new Vector3(WW - 4f, 1f, WL - 6f), new Vector3(0f, 1.5f, 0f));
        }

        // 9 — Swimming Pools: a sunken pool of liquor, floating red cups, caustic blue light.
        static void World9_SwimmingPools(TrackInfo ti, Transform w)
        {
            var tile = GKMCUtil.Mat(new Color(0.2f, 0.45f, 0.7f), 0.2f, 0.7f);
            GKMCUtil.Box(w, "PoolFloorTile", new Vector3(0f, 0.01f, 2f), new Vector3(20f, 0.02f, 30f), tile, false);
            GKMCUtil.Box(w, "Water", new Vector3(0f, 1.2f, 2f), new Vector3(20f, 0.1f, 30f),
                GKMCUtil.MatTransparent(new Color(0.1f, 0.4f, 0.8f, 0.5f)), false);
            for (int i = 0; i < 22; i++)
            {
                float y = (i < 14) ? 1.35f : Random.Range(4f, 8f);
                var cup = GKMCUtil.Cyl(w, "Cup", new Vector3(Random.Range(-9f, 9f), y, Random.Range(-12f, 16f)),
                    new Vector3(0.5f, 0.5f, 0.5f), GKMCUtil.Mat(new Color(0.8f, 0.1f, 0.1f)), false);
                var b = cup.AddComponent<Bobber>(); b.amplitude = 0.25f; b.speed = 1.5f;
            }
            for (int i = 0; i < 4; i++)
                GKMCUtil.PointLight(w, "Caustic", new Vector3(Random.Range(-8f, 8f), 3f, Random.Range(-10f, 14f)),
                    new Color(0.3f, 0.7f, 1f), 1.6f, 16f);
            GKMCUtil.Particles(w, "Bubbles", new Vector3(0f, 1f, 2f), new Color(0.6f, 0.85f, 1f, 0.6f),
                20f, 0.15f, 4f, new Vector3(18f, 0.5f, 28f), new Vector3(0f, 0.8f, 0f));
        }

        // 10 — Sing About Me / Dying of Thirst: a candlelit memorial fading into a baptismal pool.
        static void World10_SingAboutMe(TrackInfo ti, Transform w)
        {
            for (int x = -2; x <= 2; x++)
                for (int z = 0; z < 4; z++)
                    Candle(w, new Vector3(x * 3f, 0.9f, -18f + z * 3f), new Color(1f, 0.7f, 0.3f), bob: false);
            for (int i = 0; i < 6; i++)
            {
                var fig = GKMCUtil.Prim(PrimitiveType.Capsule, w, "Soul",
                    new Vector3(Random.Range(-9f, 9f), 1.2f, Random.Range(-14f, 4f)), new Vector3(0.8f, 1.1f, 0.8f),
                    GKMCUtil.MatTransparent(new Color(1f, 0.85f, 0.6f, 0.3f)), false);
                fig.AddComponent<Fader>();
            }
            GKMCUtil.Cyl(w, "Font", new Vector3(0f, 0.3f, 16f), new Vector3(7f, 0.3f, 7f), GKMCUtil.Mat(new Color(0.7f, 0.7f, 0.72f), 0.3f, 0.6f));
            GKMCUtil.Cyl(w, "FontWater", new Vector3(0f, 0.55f, 16f), new Vector3(6.4f, 0.06f, 6.4f),
                GKMCUtil.MatTransparent(new Color(0.7f, 0.85f, 1f, 0.5f)), false);
            GKMCUtil.SpotLight(w, "LightFromAbove", new Vector3(0f, 13f, 16f), new Vector3(90f, 0f, 0f), Color.white, 7f, 16f, 35f);
            GKMCUtil.Particles(w, "Ascend", new Vector3(0f, 1f, 16f), new Color(1f, 1f, 0.95f, 0.7f),
                14f, 0.18f, 5f, new Vector3(5f, 1f, 5f), new Vector3(0f, 1.2f, 0f));
        }

        // 11 — Real: a glowing, beating heart ringed by mirrors of truth.
        static void World11_Real(TrackInfo ti, Transform w)
        {
            Heart(w, new Vector3(0f, 3.5f, 4f), new Color(1f, 0.2f, 0.25f));
            var mirror = GKMCUtil.Mat(new Color(0.75f, 0.75f, 0.8f), 0.95f, 0.95f);
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                GKMCUtil.Box(w, "TruthMirror", new Vector3(Mathf.Cos(a) * 9f, 3.5f, 4f + Mathf.Sin(a) * 9f),
                    new Vector3(2.5f, 7f, 0.3f), mirror, true, new Vector3(0f, -a * Mathf.Rad2Deg + 90f, 0f));
            }
            GKMCUtil.PointLight(w, "Warm", new Vector3(0f, 8f, 4f), new Color(1f, 0.8f, 0.6f), 1.2f, 35f);
        }

        // 12 — Compton: bright daylight, palm-lined boulevard, the city sign, confetti, and lowriders
        //      hopping in every classic style — the celebration of the hometown.
        static void World12_Compton(TrackInfo ti, Transform w)
        {
            var road = GKMCUtil.Mat(new Color(0.12f, 0.12f, 0.14f), 0.1f, 0.4f);
            GKMCUtil.Box(w, "Boulevard", new Vector3(0f, 0.03f, 0f), new Vector3(10f, 0.06f, WL - 4f), road, false);
            for (int i = 0; i < 6; i++)
            {
                float z = -18f + i * 7f;
                PalmTree(w, new Vector3(-9f, 0f, z));
                PalmTree(w, new Vector3(9f, 0f, z));
            }
            CitySign(w, new Vector3(0f, 0f, 18f), "COMPTON");

            // The lowrider showcase — a few cars, each bouncing in a different hydraulic style.
            Lowrider(w, new Vector3(-3f, 0f, 0f), 0f, new Color(0.6f, 0.1f, 0.65f), LowriderHop.Style.FullBounce);
            Lowrider(w, new Vector3(4f, 0f, -10f), 12f, new Color(0.7f, 0.08f, 0.1f), LowriderHop.Style.ThreeWheel);
            Lowrider(w, new Vector3(-4f, 0f, 10f), -14f, new Color(0.1f, 0.45f, 0.7f), LowriderHop.Style.SideToSide);
            Lowrider(w, new Vector3(3f, 0f, 6f), 180f, new Color(0.1f, 0.55f, 0.2f), LowriderHop.Style.FrontBack);
            Lowrider(w, new Vector3(0f, 0f, -16f), 0f, new Color(0.85f, 0.7f, 0.1f), LowriderHop.Style.Pancake);

            GKMCUtil.Particles(w, "Confetti", new Vector3(0f, 12f, 0f), new Color(1f, 0.85f, 0.3f),
                40f, 0.2f, 8f, new Vector3(WW - 4f, 1f, WL - 6f), new Vector3(0.5f, -1.2f, 0f));
            GKMCUtil.PointLight(w, "Daylight", new Vector3(0f, 14f, 0f), new Color(1f, 0.97f, 0.85f), 1.8f, 60f);
        }

        // ============================== MODEL-AWARE PROPS ==============================

        public static Transform Car(Transform w, Vector3 pos, Color color, float yaw, float roll = 0f, bool low = false)
        {
            string key = low ? "lowrider" : "car";
            return ModelLibrary.SpawnOrFallback(key, w, pos, new Vector3(0f, yaw, roll), t => CarInto(t, color, low));
        }

        static void Lowrider(Transform w, Vector3 pos, float yaw, Color color, LowriderHop.Style style)
        {
            var anchor = Car(w, pos, color, yaw, 0f, low: true);
            var hop = anchor.gameObject.AddComponent<LowriderHop>();
            hop.style = style;
            hop.speed = 2.2f + Random.value * 1.2f;
            hop.height = 0.4f + Random.value * 0.25f;
            hop.tilt = 12f + Random.value * 8f;
        }

        static void CarInto(Transform t, Color color, bool low)
        {
            var body = GKMCUtil.Mat(color, 0.6f, 0.7f);
            var glass = GKMCUtil.Mat(new Color(0.05f, 0.08f, 0.1f), 0.3f, 0.9f);
            var tire = GKMCUtil.Mat(new Color(0.03f, 0.03f, 0.03f));
            var chrome = GKMCUtil.Mat(new Color(0.8f, 0.8f, 0.85f), 0.9f, 0.9f);
            float h = low ? 0.45f : 0.7f;
            GKMCUtil.Box(t, "Body", new Vector3(0f, h, 0f), new Vector3(2.1f, h * 1.2f, 4.6f), body, false);
            GKMCUtil.Box(t, "Cabin", new Vector3(0f, h + 0.6f, -0.2f), new Vector3(1.85f, 0.7f, 2.2f), glass, false);
            float wx = 1.0f, wz = 1.5f, wy = 0.45f;
            foreach (var p in new[] {
                new Vector3(-wx, wy, wz), new Vector3(wx, wy, wz),
                new Vector3(-wx, wy, -wz), new Vector3(wx, wy, -wz) })
                GKMCUtil.Cyl(t, "Wheel", p, new Vector3(0.8f, 0.15f, 0.8f), low ? chrome : tire, false, new Vector3(0f, 0f, 90f));
        }

        static void Candle(Transform w, Vector3 pos, Color flame, bool bob)
        {
            var anchor = ModelLibrary.SpawnOrFallback("candle", w, pos, Vector3.zero,
                t => GKMCUtil.Cyl(t, "Wax", Vector3.zero, new Vector3(0.2f, 0.4f, 0.2f), GKMCUtil.Mat(new Color(0.9f, 0.88f, 0.8f)), false));
            GKMCUtil.Sphere(anchor, "Flame", new Vector3(0f, 0.5f, 0f), Vector3.one * 0.18f,
                GKMCUtil.MatEmissive(new Color(1f, 0.7f, 0.3f), new Color(1f, 0.6f, 0.2f), 4f), false);
            var l = GKMCUtil.PointLight(anchor, "CandleLight", new Vector3(0f, 0.6f, 0f), flame, 1.2f, 6f);
            l.gameObject.AddComponent<FireFlicker>().light = l;
            if (bob) { var b = anchor.gameObject.AddComponent<Bobber>(); b.amplitude = 0.3f; b.speed = 1f; }
        }

        static void MoneyTree(Transform w, Vector3 pos)
        {
            var anchor = ModelLibrary.SpawnOrFallback("money_tree", w, pos, Vector3.zero, t =>
            {
                GKMCUtil.Cyl(t, "Trunk", new Vector3(0f, 2f, 0f), new Vector3(0.6f, 2f, 0.6f), GKMCUtil.Mat(new Color(0.25f, 0.16f, 0.08f)));
                GKMCUtil.Sphere(t, "Canopy", new Vector3(0f, 4.5f, 0f), Vector3.one * 3.5f, GKMCUtil.Mat(new Color(0.2f, 0.45f, 0.18f)), false);
            });
            GKMCUtil.Particles(anchor, "MoneyLeaves", new Vector3(0f, 4.5f, 0f), new Color(0.45f, 0.75f, 0.35f),
                6f, 0.22f, 6f, new Vector3(3f, 0.5f, 3f), new Vector3(0.2f, -0.5f, 0f));
        }

        static void PalmTree(Transform w, Vector3 pos)
        {
            ModelLibrary.SpawnOrFallback("palm_tree", w, pos, Vector3.zero, t =>
            {
                GKMCUtil.Cyl(t, "PalmTrunk", new Vector3(0f, 4f, 0f), new Vector3(0.5f, 4f, 0.5f), GKMCUtil.Mat(new Color(0.4f, 0.3f, 0.15f)), false);
                var frond = GKMCUtil.Mat(new Color(0.15f, 0.45f, 0.15f));
                for (int i = 0; i < 6; i++)
                {
                    float a = i / 6f * 360f;
                    GKMCUtil.Box(t, "Frond", new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 1.5f, 8f, Mathf.Sin(a * Mathf.Deg2Rad) * 1.5f),
                        new Vector3(0.5f, 0.15f, 3f), frond, false, new Vector3(25f, a, 0f));
                }
            });
        }

        static void FireBarrel(Transform w, Vector3 pos)
        {
            var anchor = ModelLibrary.SpawnOrFallback("fire_barrel", w, pos, Vector3.zero,
                t => GKMCUtil.Cyl(t, "Barrel", new Vector3(0f, 0.7f, 0f), new Vector3(1f, 0.7f, 1f), GKMCUtil.Mat(new Color(0.15f, 0.1f, 0.08f), 0.5f, 0.3f)));
            GKMCUtil.Particles(anchor, "Fire", new Vector3(0f, 1.4f, 0f), new Color(1f, 0.5f, 0.1f),
                30f, 0.5f, 1.2f, new Vector3(0.6f, 0.2f, 0.6f), new Vector3(0f, 2.5f, 0f));
            var l = GKMCUtil.PointLight(anchor, "FireLight", new Vector3(0f, 2f, 0f), new Color(1f, 0.45f, 0.15f), 3f, 12f);
            l.gameObject.AddComponent<FireFlicker>().light = l;
        }

        static void SurveillanceCamera(Transform w, Vector3 pos)
        {
            GKMCUtil.Cyl(w, "CamPole", new Vector3(pos.x, pos.y * 0.5f, pos.z), new Vector3(0.2f, pos.y * 0.5f, 0.2f), GKMCUtil.Mat(new Color(0.15f, 0.15f, 0.18f)));
            var head = ModelLibrary.SpawnOrFallback("surveillance_camera", w, pos, Vector3.zero, t =>
            {
                GKMCUtil.Box(t, "CamBody", Vector3.zero, new Vector3(0.5f, 0.4f, 0.9f), GKMCUtil.Mat(new Color(0.1f, 0.1f, 0.12f)), false);
                GKMCUtil.Sphere(t, "Lens", new Vector3(0f, 0f, 0.5f), Vector3.one * 0.25f, GKMCUtil.MatEmissive(Color.red, Color.red, 2f), false);
            });
            var sweep = head.gameObject.AddComponent<Sweeper>(); sweep.axis = Vector3.up; sweep.range = 45f; sweep.speed = 0.6f;
        }

        static void Heart(Transform w, Vector3 pos, Color color)
        {
            var anchor = ModelLibrary.SpawnOrFallback("heart", w, pos, Vector3.zero, t =>
            {
                var mat = GKMCUtil.MatEmissive(color, color, 2.5f);
                GKMCUtil.Sphere(t, "LobeL", new Vector3(-0.7f, 0.6f, 0f), Vector3.one * 1.6f, mat, false);
                GKMCUtil.Sphere(t, "LobeR", new Vector3(0.7f, 0.6f, 0f), Vector3.one * 1.6f, mat, false);
                GKMCUtil.Box(t, "Point", new Vector3(0f, -0.6f, 0f), Vector3.one * 1.8f, mat, false, new Vector3(0f, 45f, 45f));
            });
            anchor.gameObject.AddComponent<Pulser>();
            GKMCUtil.PointLight(anchor, "HeartGlow", Vector3.zero, new Color(1f, 0.3f, 0.35f), 3f, 30f);
        }

        static void CitySign(Transform w, Vector3 pos, string text)
        {
            ModelLibrary.SpawnOrFallback("city_sign", w, pos, Vector3.zero, t =>
            {
                for (int s = -1; s <= 1; s += 2)
                    GKMCUtil.Cyl(t, "SignPost", new Vector3(7f * s, 4f, 0f), new Vector3(0.4f, 4f, 0.4f), GKMCUtil.Mat(new Color(0.2f, 0.2f, 0.2f)));
                GKMCUtil.Box(t, "Billboard", new Vector3(0f, 8.5f, 0f), new Vector3(16f, 3.5f, 0.4f), GKMCUtil.Mat(new Color(0.05f, 0.05f, 0.05f)));
                GKMCUtil.Sign(t, "SignText", new Vector3(0f, 8.5f, -0.3f), text, new Color(1f, 0.85f, 0.2f), 1.6f);
            });
        }

        // ============================== PRIMITIVE-ONLY PROPS ==============================

        static void House(Transform w, Vector3 pos, Vector3 size, Color windowColor)
        {
            var dark = GKMCUtil.Mat(new Color(0.045f, 0.04f, 0.05f), 0f, 0.1f);
            GKMCUtil.Box(w, "House", pos + new Vector3(0f, size.y * 0.5f, 0f), size, dark);
            GKMCUtil.Box(w, "Window", pos + new Vector3(0f, size.y * 0.45f, -size.z * 0.5f - 0.05f),
                new Vector3(1.8f, 1.8f, 0.1f), GKMCUtil.MatEmissive(windowColor, windowColor, 3f), false);
        }

        static void EiffelTower(Transform w, Vector3 pos, Material gold)
        {
            GKMCUtil.Box(w, "Eiffel1", pos + new Vector3(0f, 1.5f, 0f), new Vector3(3f, 3f, 3f), gold, false);
            GKMCUtil.Box(w, "Eiffel2", pos + new Vector3(0f, 4.5f, 0f), new Vector3(1.8f, 3f, 1.8f), gold, false);
            GKMCUtil.Box(w, "Eiffel3", pos + new Vector3(0f, 7f, 0f), new Vector3(0.9f, 2.5f, 0.9f), gold, false);
            GKMCUtil.Cyl(w, "EiffelSpire", pos + new Vector3(0f, 9.5f, 0f), new Vector3(0.2f, 1.5f, 0.2f), gold, false);
        }

        static void FencePanel(Transform w, Vector3 pos, float yaw = 0f)
        {
            var go = new GameObject("Fence");
            go.transform.SetParent(w, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = new Vector3(0f, yaw, 0f);
            var post = GKMCUtil.Mat(new Color(0.3f, 0.32f, 0.35f), 0.7f, 0.4f);
            GKMCUtil.Box(go.transform, "PostL", new Vector3(-2.5f, 2.5f, 0f), new Vector3(0.25f, 5f, 0.25f), post);
            GKMCUtil.Box(go.transform, "PostR", new Vector3(2.5f, 2.5f, 0f), new Vector3(0.25f, 5f, 0.25f), post);
            GKMCUtil.Box(go.transform, "Mesh", new Vector3(0f, 2.5f, 0f), new Vector3(5f, 4.6f, 0.05f),
                GKMCUtil.MatTransparent(new Color(0.5f, 0.55f, 0.6f, 0.25f)));
        }

        static string Safe(string s)
        {
            var arr = s.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
                if (!char.IsLetterOrDigit(arr[i])) arr[i] = '_';
            return new string(arr).Substring(0, Mathf.Min(24, arr.Length));
        }
    }
}
