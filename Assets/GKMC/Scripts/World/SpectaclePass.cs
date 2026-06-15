using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GKMC
{
    /// <summary>
    /// Fourth visual pass. This pass adds large readable landmarks and animated spectacle so every
    /// world has a strong screenshot silhouette before imported GLB models are added.
    /// </summary>
    public static class SpectaclePass
    {
        static float WL => AlbumData.WorldLength;
        static float WW => AlbumData.WorldWidth;
        static float WH => AlbumData.WallHeight;
        static float HalfW => WW * 0.5f;
        static float HalfL => WL * 0.5f;

        public static void Apply(List<TrackInfo> tracks, Transform worldsRoot)
        {
            if (tracks == null || tracks.Count == 0 || worldsRoot == null) return;

            Random.State old = Random.state;
            AddAlbumConstellation(worldsRoot, tracks);

            foreach (TrackInfo ti in tracks)
            {
                Transform world = FindWorld(worldsRoot, ti.number);
                if (world == null) continue;

                Random.InitState(8800 + ti.number * 441);
                AddWorldBillboard(world, ti);
                AddProjectionCurtains(world, ti);
                AddCeilingKinetics(world, ti);
                AddWorldSpecificLandmark(world, ti);
            }
            Random.state = old;
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

        static void AddAlbumConstellation(Transform root, List<TrackInfo> tracks)
        {
            Material lineMat = GKMCUtil.MatEmissive(new Color(0.12f, 0.12f, 0.16f), new Color(0.32f, 0.34f, 0.52f), 0.65f);
            Material nodeMat = GKMCUtil.MatEmissive(new Color(0.65f, 0.65f, 0.9f), new Color(0.52f, 0.58f, 1f), 1.2f);

            for (int i = 0; i < tracks.Count; i++)
            {
                TrackInfo ti = tracks[i];
                Vector3 node = new Vector3(Mathf.Sin(i * 1.27f) * 7.5f, WH + 2.4f + Mathf.Sin(i * 0.6f) * 1.2f, ti.ZCenter);
                GameObject n = GKMCUtil.Sphere(root, "Spectacle_AlbumConstellationNode", node, Vector3.one * 0.32f, nodeMat, false);
                SpectaclePulse pulse = n.AddComponent<SpectaclePulse>();
                pulse.color = ti.accent;
                pulse.intensity = 1.6f;
                pulse.speed = 0.65f + i * 0.04f;

                if (i > 0)
                {
                    TrackInfo prev = tracks[i - 1];
                    Vector3 p = new Vector3(Mathf.Sin((i - 1) * 1.27f) * 7.5f, WH + 2.4f + Mathf.Sin((i - 1) * 0.6f) * 1.2f, prev.ZCenter);
                    Vector3 mid = (node + p) * 0.5f;
                    float length = Vector3.Distance(node, p);
                    GameObject line = GKMCUtil.Box(root, "Spectacle_AlbumConstellationLine", mid, new Vector3(0.08f, 0.08f, length), lineMat, false);
                    line.transform.LookAt(node);
                }
            }
        }

        static void AddWorldBillboard(Transform world, TrackInfo ti)
        {
            Material back = GKMCUtil.Mat(new Color(0.008f, 0.008f, 0.011f), 0.32f, 0.68f);
            Material edge = GKMCUtil.MatEmissive(ti.accent, ti.accent, 1.8f, 0.1f, 0.8f);
            float z = HalfL - 2.2f;

            GKMCUtil.Box(world, "Spectacle_TitleWall", new Vector3(0f, 5.8f, z), new Vector3(WW - 5f, 5.2f, 0.35f), back, false);
            GKMCUtil.Box(world, "Spectacle_TitleWallTop", new Vector3(0f, 8.55f, z - 0.25f), new Vector3(WW - 4f, 0.14f, 0.14f), edge, false);
            GKMCUtil.Box(world, "Spectacle_TitleWallBottom", new Vector3(0f, 3.05f, z - 0.25f), new Vector3(WW - 4f, 0.14f, 0.14f), edge, false);
            GKMCUtil.Sign(world, "Spectacle_BigWorldNumber", new Vector3(-8.2f, 6.1f, z - 0.32f), $"{ti.number:00}", ti.accent, 1.15f, new Vector3(0f, 180f, 0f));
            GKMCUtil.Sign(world, "Spectacle_BigWorldTheme", new Vector3(2.6f, 6.1f, z - 0.32f), ti.theme.ToUpperInvariant(), Color.white, 0.54f, new Vector3(0f, 180f, 0f));
        }

        static void AddProjectionCurtains(Transform world, TrackInfo ti)
        {
            Material curtain = GKMCUtil.MatTransparent(new Color(ti.accent.r, ti.accent.g, ti.accent.b, 0.085f));
            for (int i = 0; i < 7; i++)
            {
                float x = -9f + i * 3f;
                GameObject c = GKMCUtil.Box(world, "Spectacle_ProjectionCurtain", new Vector3(x, 4.9f, -19f + i * 6.7f), new Vector3(1.2f, 8.4f, 0.035f), curtain, false, new Vector3(0f, Random.Range(-8f, 8f), Random.Range(-4f, 4f)));
                SpectacleFloat floaty = c.AddComponent<SpectacleFloat>();
                floaty.amplitude = 0.18f;
                floaty.speed = 0.35f + i * 0.08f;
            }
        }

        static void AddCeilingKinetics(Transform world, TrackInfo ti)
        {
            Material neon = GKMCUtil.MatEmissive(ti.accent, ti.accent, 1.3f);
            Material dark = GKMCUtil.Mat(new Color(0.02f, 0.02f, 0.024f), 0.35f, 0.55f);

            for (int i = 0; i < 6; i++)
            {
                float z = -21f + i * 8.5f;
                GKMCUtil.Box(world, "Spectacle_CeilingTrack", new Vector3(0f, WH + 1.55f, z), new Vector3(WW - 5f, 0.08f, 0.08f), dark, false);
                for (int j = -2; j <= 2; j++)
                {
                    Vector3 p = new Vector3(j * 3.5f, WH + 0.85f, z + Mathf.Sin(j + i) * 0.6f);
                    GameObject orb = GKMCUtil.Sphere(world, "Spectacle_HangingOrb", p, Vector3.one * 0.18f, neon, false);
                    SpectaclePulse pulse = orb.AddComponent<SpectaclePulse>();
                    pulse.color = ti.accent;
                    pulse.intensity = 1.4f;
                    pulse.speed = 0.9f + Random.value;
                    GKMCUtil.Cyl(world, "Spectacle_HangingCable", p + new Vector3(0f, 0.45f, 0f), new Vector3(0.025f, 0.45f, 0.025f), dark, false);
                }
            }
        }

        static void AddWorldSpecificLandmark(Transform world, TrackInfo ti)
        {
            switch (ti.number)
            {
                case 1: AddPhoneBoothBlock(world, ti); break;
                case 2: AddSanctuary(world, ti); break;
                case 3: AddGoldEgoSculpture(world, ti); break;
                case 4: AddPressureMaze(world, ti); break;
                case 5: AddMoneyTreeCanopy(world, ti); break;
                case 6: AddRoseCinema(world, ti); break;
                case 7: AddSurveillanceTower(world, ti); break;
                case 8: AddEmergencyAvenue(world, ti); break;
                case 9: AddAquariumRoom(world, ti); break;
                case 10: AddMemoryChoir(world, ti); break;
                case 11: AddTruthAltar(world, ti); break;
                case 12: AddVictoryBoulevard(world, ti); break;
            }
        }

        static void AddPhoneBoothBlock(Transform world, TrackInfo ti)
        {
            Material glass = GKMCUtil.MatTransparent(new Color(0.28f, 0.56f, 1f, 0.18f));
            Material red = GKMCUtil.MatEmissive(new Color(0.65f, 0.05f, 0.03f), new Color(1f, 0.08f, 0.04f), 0.95f);
            Vector3 p = new Vector3(-9.5f, 0f, 4f);
            GKMCUtil.Box(world, "Spectacle_PhoneBoothBody", p + new Vector3(0f, 2.4f, 0f), new Vector3(2.0f, 4.8f, 1.6f), red, false);
            GKMCUtil.Box(world, "Spectacle_PhoneBoothGlass", p + new Vector3(0f, 2.5f, -0.84f), new Vector3(1.5f, 3.4f, 0.06f), glass, false);
            GKMCUtil.Sign(world, "Spectacle_PhoneBoothSign", p + new Vector3(0f, 5.1f, -0.92f), "CALL HOME", Color.white, 0.21f, new Vector3(0f, 180f, 0f));
            Light l = GKMCUtil.PointLight(world, "Spectacle_PhoneBoothLight", p + new Vector3(0f, 4.6f, -0.8f), ti.accent, 1.2f, 12f);
            l.shadows = LightShadows.Soft;
            AddGiantMural(world, "SHERANE", ti.accent, -HalfW + 0.28f, 3.9f, -7f, true);
        }

        static void AddSanctuary(Transform world, TrackInfo ti)
        {
            Material stone = GKMCUtil.Mat(new Color(0.035f, 0.04f, 0.045f), 0.05f, 0.4f);
            Material glow = GKMCUtil.MatEmissive(ti.accent, ti.accent, 1.05f);
            for (int i = 0; i < 5; i++)
            {
                float z = -13f + i * 6f;
                Arch(world, new Vector3(0f, 0f, z), 12f - i * 0.7f, 7.4f, stone, glow);
            }
            GKMCUtil.Sign(world, "Spectacle_SanctuaryText", new Vector3(0f, 8f, 19f), "DON'T KILL MY VIBE", ti.accent, 0.42f, new Vector3(0f, 180f, 0f));
        }

        static void AddGoldEgoSculpture(Transform world, TrackInfo ti)
        {
            Material gold = GKMCUtil.MatEmissive(new Color(1f, 0.72f, 0.05f), new Color(1f, 0.48f, 0.0f), 1.45f, 0.75f, 0.95f);
            Vector3 c = new Vector3(0f, 4.7f, 4f);
            GKMCUtil.Cyl(world, "Spectacle_GoldCrownBase", c, new Vector3(3.8f, 0.16f, 3.8f), gold, false);
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f;
                GameObject spike = GKMCUtil.Box(world, "Spectacle_GoldCrownSpike", c + new Vector3(Mathf.Cos(a) * 3f, 1.4f, Mathf.Sin(a) * 3f), new Vector3(0.28f, 3f, 0.28f), gold, false, new Vector3(Random.Range(-10f, 10f), -a * Mathf.Rad2Deg, Random.Range(-10f, 10f)));
                SpectaclePulse pulse = spike.AddComponent<SpectaclePulse>(); pulse.color = ti.accent; pulse.intensity = 1.7f; pulse.speed = 1.1f;
            }
            GKMCUtil.Sign(world, "Spectacle_EgoText", new Vector3(0f, 8.8f, 4f), "BACKSEAT FREESTYLE", gold.color, 0.34f, new Vector3(0f, 180f, 0f));
        }

        static void AddPressureMaze(Transform world, TrackInfo ti)
        {
            Material wall = GKMCUtil.Mat(new Color(0.022f, 0.02f, 0.024f), 0.08f, 0.36f);
            Material glow = GKMCUtil.MatEmissive(ti.accent, ti.accent, 0.65f);
            for (int i = 0; i < 12; i++)
            {
                float z = -22f + i * 4f;
                float x = (i % 2 == 0) ? -4.2f : 4.2f;
                GKMCUtil.Box(world, "Spectacle_PressureMazeWall", new Vector3(x, 1.6f, z), new Vector3(7f, 3.2f, 0.22f), wall, false, new Vector3(0f, Mathf.Sin(i) * 8f, 0f));
                GKMCUtil.Box(world, "Spectacle_PressureMazeEdge", new Vector3(x, 3.35f, z - 0.14f), new Vector3(7f, 0.08f, 0.08f), glow, false);
            }
            AddGiantMural(world, "PEER", ti.accent, HalfW - 0.28f, 4.4f, 5f, false);
        }

        static void AddMoneyTreeCanopy(Transform world, TrackInfo ti)
        {
            Material trunk = GKMCUtil.Mat(new Color(0.1f, 0.07f, 0.035f), 0.05f, 0.32f);
            Material leaf = GKMCUtil.MatEmissive(new Color(0.22f, 0.72f, 0.24f), ti.accent, 0.55f);
            for (int i = 0; i < 8; i++)
            {
                float x = -12f + i * 3.4f;
                float z = -14f + Mathf.Sin(i) * 9f;
                GKMCUtil.Cyl(world, "Spectacle_MoneyTreeTrunk", new Vector3(x, 2.2f, z), new Vector3(0.28f, 2.2f, 0.28f), trunk, false);
                for (int j = 0; j < 7; j++)
                {
                    GameObject leafObj = GKMCUtil.Box(world, "Spectacle_MoneyLeaf", new Vector3(x + Random.Range(-1.7f, 1.7f), 5f + Random.Range(-0.8f, 1.2f), z + Random.Range(-1.7f, 1.7f)), new Vector3(0.7f, 0.025f, 0.34f), leaf, false, new Vector3(Random.Range(-25f, 25f), Random.Range(0f, 180f), Random.Range(-25f, 25f)));
                    leafObj.AddComponent<SpectacleSpin>().speed = Random.Range(8f, 26f);
                }
            }
            GKMCUtil.Sign(world, "Spectacle_MoneyText", new Vector3(0f, 8.4f, 18f), "MONEY TREES IS THE PERFECT PLACE", ti.accent, 0.27f, new Vector3(0f, 180f, 0f));
        }

        static void AddRoseCinema(Transform world, TrackInfo ti)
        {
            Material pink = GKMCUtil.MatEmissive(new Color(1f, 0.28f, 0.58f), new Color(1f, 0.12f, 0.34f), 1.0f);
            Material screen = GKMCUtil.MatTransparent(new Color(1f, 0.72f, 0.9f, 0.12f));
            GKMCUtil.Box(world, "Spectacle_RoseCinemaScreen", new Vector3(0f, 5.2f, 12f), new Vector3(17f, 7f, 0.05f), screen, false);
            for (int i = 0; i < 32; i++)
            {
                float a = i / 32f * Mathf.PI * 2f;
                GKMCUtil.Sphere(world, "Spectacle_RoseBulb", new Vector3(Mathf.Cos(a) * 7.9f, 5.2f + Mathf.Sin(a) * 3.4f, 11.8f), Vector3.one * 0.14f, pink, false);
            }
            GKMCUtil.Sign(world, "Spectacle_RoseText", new Vector3(0f, 5.3f, 11.7f), "POETIC JUSTICE", ti.accent, 0.54f, new Vector3(0f, 180f, 0f));
        }

        static void AddSurveillanceTower(Transform world, TrackInfo ti)
        {
            Material metal = GKMCUtil.Mat(new Color(0.035f, 0.04f, 0.05f), 0.55f, 0.7f);
            Material screen = GKMCUtil.MatEmissive(new Color(0.1f, 0.28f, 0.45f), ti.accent, 0.85f);
            Vector3 c = new Vector3(0f, 0f, 5f);
            GKMCUtil.Cyl(world, "Spectacle_SurveillanceTower", c + new Vector3(0f, 4.2f, 0f), new Vector3(0.35f, 4.2f, 0.35f), metal, false);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                Vector3 p = c + new Vector3(Mathf.Cos(a) * 6.5f, 7.5f + Mathf.Sin(i) * 0.4f, Mathf.Sin(a) * 6.5f);
                GKMCUtil.Box(world, "Spectacle_SurveillanceScreen", p, new Vector3(1.8f, 1.0f, 0.12f), screen, false, new Vector3(0f, -a * Mathf.Rad2Deg + 180f, 0f));
                Light sweep = GKMCUtil.SpotLight(world, "Spectacle_CameraSweep", p, new Vector3(70f, -a * Mathf.Rad2Deg, 0f), ti.accent, 0.85f, 28f, 20f);
                SpectacleLightSweep motion = sweep.gameObject.AddComponent<SpectacleLightSweep>(); motion.yawAmount = 26f; motion.speed = 0.6f + i * 0.05f;
            }
        }

        static void AddEmergencyAvenue(Transform world, TrackInfo ti)
        {
            Material red = GKMCUtil.MatEmissive(new Color(1f, 0.04f, 0.02f), new Color(1f, 0f, 0f), 1.8f);
            Material blue = GKMCUtil.MatEmissive(new Color(0.04f, 0.18f, 1f), new Color(0.02f, 0.1f, 1f), 1.8f);
            for (int i = 0; i < 9; i++)
            {
                float z = -22f + i * 5.5f;
                GameObject a = GKMCUtil.Box(world, "Spectacle_EmergencyGateRed", new Vector3(-5.4f, 4.5f, z), new Vector3(6f, 0.12f, 0.12f), red, false);
                GameObject b = GKMCUtil.Box(world, "Spectacle_EmergencyGateBlue", new Vector3(5.4f, 4.5f, z), new Vector3(6f, 0.12f, 0.12f), blue, false);
                a.AddComponent<SpectaclePulse>().color = Color.red;
                b.AddComponent<SpectaclePulse>().color = new Color(0.04f, 0.18f, 1f);
            }
            AddGiantMural(world, "M.A.A.D", Color.red, -HalfW + 0.28f, 5f, 7f, true);
        }

        static void AddAquariumRoom(Transform world, TrackInfo ti)
        {
            Material water = GKMCUtil.MatTransparent(new Color(0.05f, 0.5f, 1f, 0.16f));
            for (int i = 0; i < 5; i++)
            {
                float radius = 3.5f + i * 1.4f;
                Ring(world, new Vector3(0f, 3.2f + i * 0.45f, 5f), radius, water, 26);
            }
            GKMCUtil.Particles(world, "Spectacle_AquariumSparkle", new Vector3(0f, 4f, 5f), new Color(0.5f, 0.88f, 1f, 0.4f), 32f, 0.12f, 5f, new Vector3(18f, 4f, 28f), new Vector3(0.05f, 0.35f, 0.03f), 0f, true);
            GKMCUtil.Sign(world, "Spectacle_AquariumText", new Vector3(0f, 7.9f, 17f), "SWIMMING POOLS", ti.accent, 0.46f, new Vector3(0f, 180f, 0f));
        }

        static void AddMemoryChoir(Transform world, TrackInfo ti)
        {
            Material warm = GKMCUtil.MatEmissive(new Color(1f, 0.68f, 0.28f), new Color(1f, 0.42f, 0.12f), 0.8f);
            for (int i = 0; i < 18; i++)
            {
                float x = -11f + (i % 6) * 4.4f;
                float z = -16f + (i / 6) * 6f;
                MemoryColumn(world, new Vector3(x, 0f, z), warm, ti.accent);
            }
            GKMCUtil.Sign(world, "Spectacle_MemoryText", new Vector3(0f, 8.1f, 19f), "SING ABOUT ME", new Color(1f, 0.76f, 0.42f), 0.52f, new Vector3(0f, 180f, 0f));
        }

        static void AddTruthAltar(Transform world, TrackInfo ti)
        {
            Material mirror = GKMCUtil.Mat(new Color(0.82f, 0.82f, 0.9f), 0.92f, 0.98f);
            Material red = GKMCUtil.MatEmissive(new Color(1f, 0.1f, 0.16f), new Color(1f, 0f, 0.04f), 1.1f);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                GKMCUtil.Box(world, "Spectacle_TruthMirrorPetal", new Vector3(Mathf.Cos(a) * 6.8f, 3.9f, 5f + Mathf.Sin(a) * 6.8f), new Vector3(0.15f, 4.2f, 1.2f), mirror, false, new Vector3(0f, -a * Mathf.Rad2Deg + 90f, 0f));
            }
            Ring(world, new Vector3(0f, 0.18f, 5f), 7.8f, red, 36);
            GKMCUtil.Sign(world, "Spectacle_TruthText", new Vector3(0f, 8.2f, 5f), "REAL", ti.accent, 0.82f, new Vector3(0f, 180f, 0f));
        }

        static void AddVictoryBoulevard(Transform world, TrackInfo ti)
        {
            Material gold = GKMCUtil.MatEmissive(new Color(1f, 0.72f, 0.1f), new Color(1f, 0.48f, 0.02f), 1.3f);
            for (int i = 0; i < 7; i++)
            {
                float z = -22f + i * 7.3f;
                Arch(world, new Vector3(0f, 0f, z), 16f, 7.2f, GKMCUtil.Mat(new Color(0.035f, 0.032f, 0.026f), 0.25f, 0.52f), gold);
            }
            GKMCUtil.Sign(world, "Spectacle_ComptonFinal", new Vector3(0f, 8.7f, 19f), "COMPTON", gold.color, 1.0f, new Vector3(0f, 180f, 0f));
        }

        static void AddGiantMural(Transform parent, string text, Color color, float x, float y, float z, bool left)
        {
            GKMCUtil.Sign(parent, "Spectacle_GiantMural", new Vector3(x, y, z), text, color, 0.72f, new Vector3(0f, left ? 90f : -90f, 0f));
        }

        static void Arch(Transform parent, Vector3 pos, float width, float height, Material stone, Material glow)
        {
            GKMCUtil.Box(parent, "Spectacle_ArchLeft", pos + new Vector3(-width * 0.5f, height * 0.43f, 0f), new Vector3(0.38f, height * 0.86f, 0.34f), stone, false);
            GKMCUtil.Box(parent, "Spectacle_ArchRight", pos + new Vector3(width * 0.5f, height * 0.43f, 0f), new Vector3(0.38f, height * 0.86f, 0.34f), stone, false);
            for (int i = 0; i <= 12; i++)
            {
                float a = Mathf.Lerp(0f, Mathf.PI, i / 12f);
                Vector3 p = pos + new Vector3(Mathf.Cos(a) * width * 0.5f, height * 0.72f + Mathf.Sin(a) * height * 0.34f, 0f);
                GKMCUtil.Sphere(parent, "Spectacle_ArchBulb", p, Vector3.one * 0.13f, glow, false);
            }
        }

        static void Ring(Transform parent, Vector3 center, float radius, Material mat, int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                GameObject seg = GKMCUtil.Box(parent, "Spectacle_RingSegment", center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius), new Vector3(0.55f, 0.06f, 0.08f), mat, false, new Vector3(0f, -a * Mathf.Rad2Deg, 0f));
                SpectaclePulse pulse = seg.AddComponent<SpectaclePulse>();
                pulse.speed = 0.8f + i * 0.02f;
            }
        }

        static void MemoryColumn(Transform parent, Vector3 pos, Material warm, Color accent)
        {
            Material dark = GKMCUtil.Mat(new Color(0.03f, 0.026f, 0.022f), 0.05f, 0.28f);
            GKMCUtil.Cyl(parent, "Spectacle_MemoryColumn", pos + new Vector3(0f, 1.8f, 0f), new Vector3(0.32f, 1.8f, 0.32f), dark, false);
            GKMCUtil.Sphere(parent, "Spectacle_MemoryFlame", pos + new Vector3(0f, 3.85f, 0f), Vector3.one * 0.22f, warm, false);
            GKMCUtil.PointLight(parent, "Spectacle_MemoryLight", pos + new Vector3(0f, 3.8f, 0f), accent, 0.55f, 7f);
        }
    }

    public class SpectaclePulse : MonoBehaviour
    {
        public Color color = Color.white;
        public float intensity = 1.2f;
        public float speed = 1.0f;
        Renderer _renderer;
        Material _material;
        Vector3 _scale;
        float _phase;

        void Start()
        {
            _renderer = GetComponent<Renderer>();
            _material = _renderer != null ? _renderer.material : null;
            _scale = transform.localScale;
            _phase = Random.value * 6.28318f;
        }

        void Update()
        {
            float t = Mathf.Sin(Time.time * speed + _phase) * 0.5f + 0.5f;
            transform.localScale = _scale * Mathf.Lerp(0.96f, 1.08f, t);
            if (_material != null && _material.HasProperty("_EmissionColor"))
                _material.SetColor("_EmissionColor", color * Mathf.Lerp(0.35f, intensity, t));
        }
    }

    public class SpectacleFloat : MonoBehaviour
    {
        public float amplitude = 0.2f;
        public float speed = 0.7f;
        Vector3 _base;
        float _phase;

        void Start()
        {
            _base = transform.localPosition;
            _phase = Random.value * 6.28318f;
        }

        void Update()
        {
            transform.localPosition = _base + new Vector3(Mathf.Sin(Time.time * speed + _phase) * amplitude * 0.4f, Mathf.Sin(Time.time * speed * 0.7f + _phase) * amplitude, 0f);
        }
    }

    public class SpectacleSpin : MonoBehaviour
    {
        public float speed = 16f;
        void Update() => transform.Rotate(Vector3.up * speed * Time.deltaTime, Space.World);
    }

    public class SpectacleLightSweep : MonoBehaviour
    {
        public float yawAmount = 25f;
        public float speed = 0.7f;
        Quaternion _base;
        float _phase;

        void Start()
        {
            _base = transform.localRotation;
            _phase = Random.value * 6.28318f;
        }

        void Update()
        {
            transform.localRotation = _base * Quaternion.Euler(0f, Mathf.Sin(Time.time * speed + _phase) * yawAmount, 0f);
        }
    }
}
