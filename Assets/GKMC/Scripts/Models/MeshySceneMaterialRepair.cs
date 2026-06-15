using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GKMC
{
    /// <summary>
    /// Runtime safety net for Meshy/GLB/FBX imports that arrive in Unity as solid white, over-metallic,
    /// or textureless materials. This scans the live scene for washed-out renderers, applies generated
    /// detail textures and non-white prop tints, and repeats for a few seconds so async GLB loads are fixed too.
    /// </summary>
    public class MeshySceneMaterialRepair : MonoBehaviour
    {
        public float scanInterval = 0.75f;
        public float scanDuration = 12f;
        public bool repairWholeScene = true;

        float _nextScan;
        float _stopAt;
        int _scanCount;

        static readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        static readonly HashSet<int> _repairedMaterials = new HashSet<int>();

        public static MeshySceneMaterialRepair Install(Transform parent)
        {
            var go = new GameObject("Meshy_Scene_Material_Repair");
            go.transform.SetParent(parent, false);
            var repair = go.AddComponent<MeshySceneMaterialRepair>();
            repair._stopAt = Time.time + repair.scanDuration;
            repair.RepairNow();
            return repair;
        }

        void Start()
        {
            _stopAt = Time.time + scanDuration;
            RepairNow();
        }

        void Update()
        {
            if (Time.time > _stopAt) return;
            if (Time.time < _nextScan) return;
            _nextScan = Time.time + scanInterval;
            RepairNow();
        }

        public void RepairNow()
        {
            _scanCount++;
            Renderer[] renderers = repairWholeScene
                ? Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                : GetComponentsInParent<Transform>()[0].GetComponentsInChildren<Renderer>(true);

            int touched = 0;
            foreach (Renderer r in renderers)
            {
                if (r == null || ShouldSkip(r)) continue;
                touched += RepairRenderer(r) ? 1 : 0;
            }

            if (_scanCount <= 3 || touched > 0)
                Debug.Log($"[GKMC] Meshy material repair scan {_scanCount}: touched {touched} renderers.");
        }

        static bool ShouldSkip(Renderer r)
        {
            if (r.GetComponent<TextMesh>() != null) return true;
            if (r.GetComponent<ParticleSystemRenderer>() != null) return true;
            string n = r.gameObject.name.ToLowerInvariant();
            if (n.Contains("projectioncurtain") || n.Contains("lightbeam") || n.Contains("glassscrim")) return true;
            return false;
        }

        static bool RepairRenderer(Renderer r)
        {
            bool changed = false;
            r.shadowCastingMode = ShadowCastingMode.On;
            r.receiveShadows = true;

            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;

                int id = m.GetInstanceID();
                bool already = _repairedMaterials.Contains(id);
                bool hasTexture = HasAnyTexture(m);
                Color current = ReadColor(m);
                bool white = LooksWhite(current);
                bool suspicious = white || IsOverMetallic(m) || IsTooSmooth(m) || !hasTexture;
                if (!suspicious && already) continue;

                string key = GuessKey(r, m);
                Color tint = TintForKey(key);

                if (!hasTexture)
                {
                    Texture2D detail = DetailTexture(key, tint);
                    AssignTexture(m, detail);
                    SetColor(m, Color.Lerp(tint, Color.white, 0.16f));
                    changed = true;
                }
                else if (white)
                {
                    SetColor(m, Color.Lerp(tint, Color.white, 0.45f));
                    changed = true;
                }

                MakeReadable(m);
                _repairedMaterials.Add(id);
            }
            r.materials = mats;
            return changed;
        }

        static string GuessKey(Renderer r, Material m)
        {
            string full = r.transform.name + " " + r.gameObject.name + " " + (m != null ? m.name : "");
            Transform t = r.transform.parent;
            int guard = 0;
            while (t != null && guard++ < 8)
            {
                full += " " + t.name;
                t = t.parent;
            }
            full = full.ToLowerInvariant();

            if (full.Contains("lowrider")) return "lowrider";
            if (full.Contains("minivan")) return "minivan";
            if (full.Contains("car")) return "car";
            if (full.Contains("money") || full.Contains("tree")) return "money_tree";
            if (full.Contains("palm")) return "palm_tree";
            if (full.Contains("heart")) return "heart";
            if (full.Contains("barrel") || full.Contains("fire")) return "fire_barrel";
            if (full.Contains("camera") || full.Contains("surveillance")) return "surveillance_camera";
            if (full.Contains("candle")) return "candle";
            if (full.Contains("reel") || full.Contains("film")) return "film_reel";
            if (full.Contains("house")) return "house";
            if (full.Contains("sign")) return "city_sign";
            if (full.Contains("helicopter")) return "helicopter_body";
            if (full.Contains("butterfly")) return "ee_butterfly";
            if (full.Contains("crown")) return "ee_crown_of_thorns";
            if (full.Contains("pulitzer")) return "ee_pulitzer";
            if (full.Contains("panther")) return "ee_panther";
            if (full.Contains("gnx")) return "ee_gnx";
            if (full.Contains("hiiipower")) return "ee_hiiipower";
            if (full.Contains("sam")) return "ee_uncle_sam";
            if (full.Contains("pglang")) return "ee_pglang";
            return "generic";
        }

        static void MakeReadable(Material m)
        {
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("metallicFactor")) m.SetFloat("metallicFactor", 0f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.32f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.32f);
            if (m.HasProperty("roughnessFactor")) m.SetFloat("roughnessFactor", 0.68f);
            if (m.HasProperty("_SpecColor")) m.SetColor("_SpecColor", new Color(0.08f, 0.08f, 0.08f));
            m.DisableKeyword("_METALLICGLOSSMAP");
            m.DisableKeyword("_SPECGLOSSMAP");
        }

        static bool HasAnyTexture(Material m)
        {
            if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null) return true;
            if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) return true;
            if (m.HasProperty("baseColorTexture") && m.GetTexture("baseColorTexture") != null) return true;
            if (m.HasProperty("_BaseColorMap") && m.GetTexture("_BaseColorMap") != null) return true;
            return false;
        }

        static void AssignTexture(Material m, Texture2D tex)
        {
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("baseColorTexture")) m.SetTexture("baseColorTexture", tex);
            if (m.HasProperty("_BaseColorMap")) m.SetTexture("_BaseColorMap", tex);
        }

        static Color ReadColor(Material m)
        {
            if (m.HasProperty("_Color")) return m.GetColor("_Color");
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            if (m.HasProperty("baseColorFactor")) return m.GetColor("baseColorFactor");
            return Color.white;
        }

        static void SetColor(Material m, Color c)
        {
            c.a = Mathf.Max(c.a, 1f);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("baseColorFactor")) m.SetColor("baseColorFactor", c);
        }

        static bool LooksWhite(Color c)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return max > 0.68f && (max - min) < 0.22f;
        }

        static bool IsOverMetallic(Material m)
        {
            if (m.HasProperty("_Metallic") && m.GetFloat("_Metallic") > 0.5f) return true;
            if (m.HasProperty("metallicFactor") && m.GetFloat("metallicFactor") > 0.5f) return true;
            return false;
        }

        static bool IsTooSmooth(Material m)
        {
            if (m.HasProperty("_Glossiness") && m.GetFloat("_Glossiness") > 0.78f) return true;
            if (m.HasProperty("_Smoothness") && m.GetFloat("_Smoothness") > 0.78f) return true;
            return false;
        }

        static Texture2D DetailTexture(string key, Color tint)
        {
            string cache = key + tint;
            if (_textures.TryGetValue(cache, out Texture2D existing)) return existing;

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            int seed = Mathf.Abs(key.GetHashCode()) % 8192;
            Color dark = Color.Lerp(tint, Color.black, 0.45f);
            Color light = Color.Lerp(tint, Color.white, 0.18f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n1 = Mathf.PerlinNoise((x + seed) * 0.12f, (y - seed) * 0.12f);
                    float n2 = Mathf.PerlinNoise((x - seed) * 0.31f, (y + seed) * 0.31f);
                    float stripe = ((x + y + seed) % 19 == 0) ? 0.28f : 0f;
                    float n = Mathf.Clamp01(n1 * 0.68f + n2 * 0.32f + stripe);
                    tex.SetPixel(x, y, Color.Lerp(dark, light, n));
                }
            }
            tex.Apply(false, true);
            _textures[cache] = tex;
            return tex;
        }

        static Color TintForKey(string key)
        {
            switch (key)
            {
                case "car": return new Color(0.16f, 0.2f, 0.28f);
                case "lowrider": return new Color(0.55f, 0.12f, 0.35f);
                case "minivan": return new Color(0.34f, 0.27f, 0.22f);
                case "money_tree": return new Color(0.18f, 0.52f, 0.16f);
                case "palm_tree": return new Color(0.12f, 0.36f, 0.14f);
                case "heart": return new Color(0.72f, 0.04f, 0.12f);
                case "fire_barrel": return new Color(0.28f, 0.12f, 0.05f);
                case "surveillance_camera": return new Color(0.1f, 0.14f, 0.2f);
                case "candle": return new Color(0.86f, 0.72f, 0.48f);
                case "film_reel": return new Color(0.34f, 0.34f, 0.38f);
                case "house": return new Color(0.34f, 0.22f, 0.16f);
                case "city_sign": return new Color(0.14f, 0.18f, 0.24f);
                case "helicopter_body": return new Color(0.06f, 0.08f, 0.1f);
                case "ee_butterfly": return new Color(0.16f, 0.48f, 0.86f);
                case "ee_crown_of_thorns": return new Color(0.76f, 0.55f, 0.18f);
                case "ee_pulitzer": return new Color(0.9f, 0.68f, 0.2f);
                case "ee_panther": return new Color(0.018f, 0.018f, 0.022f);
                case "ee_gnx": return new Color(0.14f, 0.16f, 0.2f);
                case "ee_hiiipower": return new Color(0.72f, 0.16f, 0.06f);
                case "ee_uncle_sam": return new Color(0.24f, 0.24f, 0.3f);
                case "ee_pglang": return new Color(0.74f, 0.72f, 0.58f);
                default: return new Color(0.38f, 0.32f, 0.24f);
            }
        }
    }
}
