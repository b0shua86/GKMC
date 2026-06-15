using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if GKMC_GLTFAST
using System.Threading.Tasks;
using GLTFast;
#endif

namespace GKMC
{
    /// <summary>
    /// Spawns a prop and, where a Meshy-generated model is available, swaps the procedural
    /// fallback for the real model. Resolution order per key:
    ///   1) Resources/GKMC_Models/&lt;key&gt;  (any natively-imported FBX/OBJ/prefab — no package needed)
    ///   2) StreamingAssets/Models/&lt;key&gt;.glb via glTFast  (textured GLB; needs com.unity.cloud.gltfast
    ///      and the GKMC_GLTFAST scripting-define symbol)
    ///   3) the procedural fallback that is always built first, so the world is never empty.
    /// </summary>
    public static class ModelLibrary
    {
        public static bool ModelsEnabled = true;
        static int _loaded, _fallbacks;

        public static string Stats => $"models loaded: {_loaded}, procedural: {_fallbacks}";

        /// <summary>
        /// Create an anchor at (pos, euler), build the primitive fallback into it immediately, then
        /// attempt to upgrade it to a generated model. Returns the anchor transform.
        /// </summary>
        public static Transform SpawnOrFallback(string key, Transform parent, Vector3 pos,
            Vector3 euler, System.Action<Transform> fallback)
        {
            var anchor = new GameObject("Prop_" + key);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = pos;
            anchor.transform.localEulerAngles = euler;

            fallback?.Invoke(anchor.transform);

            // Capture exactly the fallback geometry now; effects the caller adds afterwards survive a swap.
            var fallbackChildren = Snapshot(anchor.transform);
            if (ModelsEnabled)
                TryUpgrade(key, anchor.transform, fallbackChildren);

            return anchor.transform;
        }

        static List<Transform> Snapshot(Transform anchor)
        {
            var list = new List<Transform>();
            for (int i = 0; i < anchor.childCount; i++) list.Add(anchor.GetChild(i));
            return list;
        }

        static void DestroyAll(List<Transform> ts)
        {
            foreach (var t in ts) if (t != null) Object.Destroy(t.gameObject);
        }

        static void TryUpgrade(string key, Transform anchor, List<Transform> fallbackChildren)
        {
            // 1) Resources (works with zero extra packages).
            var prefab = Resources.Load<GameObject>("GKMC_Models/" + key);
            if (prefab != null)
            {
                var inst = Object.Instantiate(prefab, anchor);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                PrepareImportedModel(inst.transform, key);
                Fit(inst.transform, MeshyModels.Get(key));
                DestroyAll(fallbackChildren);
                _loaded++;
                return;
            }

#if GKMC_GLTFAST
            // 2) GLB via glTFast.
            LoadGlbAsync(key, anchor, fallbackChildren);
            return;
#else
            _fallbacks++;
#endif
        }

#if GKMC_GLTFAST
        static async void LoadGlbAsync(string key, Transform anchor, List<Transform> fallbackChildren)
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Models", key + ".glb");
            string uri = path.Contains("://") ? path : "file://" + path;

            try
            {
                var import = new GltfImport();
                bool ok = await import.Load(uri);
                if (!ok || anchor == null) { _fallbacks++; return; }

                var holder = new GameObject("model");
                holder.transform.SetParent(anchor, false);
                bool inst = await import.InstantiateMainSceneAsync(holder.transform);
                if (!inst || anchor == null) { if (holder != null) Object.Destroy(holder); _fallbacks++; return; }

                PrepareImportedModel(holder.transform, key);
                Fit(holder.transform, MeshyModels.Get(key));
                DestroyAll(fallbackChildren);
                _loaded++;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[GKMC] glTF load failed for '{key}': {e.Message}");
                _fallbacks++;
            }
        }
#endif

        /// <summary>Scale the model so its largest dimension matches the catalogue size, then seat it.</summary>
        static void Fit(Transform model, ModelDef def)
        {
            var rends = model.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return;

            Bounds b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            float max = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (max > 0.0001f)
            {
                float s = def.targetSize / max;
                model.localScale *= s;
            }

            // Re-measure after scaling and align to the anchor.
            b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            Vector3 a = model.parent.position;
            float baseY = def.grounded ? b.min.y : b.center.y;
            model.position += new Vector3(a.x - b.center.x, a.y - baseY, a.z - b.center.z);
        }

        /// <summary>
        /// Meshy imports often arrive as washed-out white/metallic materials in Unity's Built-in
        /// pipeline. This pass fixes both GLB and prefab imports: keep real textures when they exist,
        /// but tint textureless-white materials by prop type, lower metallic, keep moderate roughness,
        /// and ensure the renderers cast/receive shadows.
        /// </summary>
        static void PrepareImportedModel(Transform root, string key)
        {
            Color fallbackTint = ModelTint(key);

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;

                // Use material instances so runtime fixes do not permanently dirty imported assets.
                var mats = r.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) continue;

                    bool hasTexture = HasAnyBaseTexture(m);
                    bool looksWhite = LooksWhite(GetMaterialColor(m));
                    Color target = hasTexture ? Color.white : fallbackTint;

                    if (!hasTexture && looksWhite)
                        SetMaterialColor(m, target);
                    else if (!hasTexture && key.StartsWith("ee_"))
                        SetMaterialColor(m, Color.Lerp(GetMaterialColor(m), fallbackTint, 0.45f));

                    // Make Meshy assets readable in Built-in even when they import too metallic/glossy.
                    if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
                    if (m.HasProperty("metallicFactor")) m.SetFloat("metallicFactor", 0f);
                    if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.38f);
                    if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.38f);
                    if (m.HasProperty("roughnessFactor")) m.SetFloat("roughnessFactor", 0.62f);

                    m.DisableKeyword("_METALLICGLOSSMAP");
                    m.DisableKeyword("_SPECGLOSSMAP");
                    if (m.HasProperty("_EmissionColor"))
                    {
                        Color c = hasTexture ? Color.black : fallbackTint * 0.08f;
                        m.SetColor("_EmissionColor", c);
                    }
                }
                r.materials = mats;
            }
        }

        static bool HasAnyBaseTexture(Material m)
        {
            if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null) return true;
            if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null) return true;
            if (m.HasProperty("baseColorTexture") && m.GetTexture("baseColorTexture") != null) return true;
            if (m.HasProperty("_BaseColorMap") && m.GetTexture("_BaseColorMap") != null) return true;
            return false;
        }

        static Color GetMaterialColor(Material m)
        {
            if (m.HasProperty("_Color")) return m.GetColor("_Color");
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            if (m.HasProperty("baseColorFactor")) return m.GetColor("baseColorFactor");
            return Color.white;
        }

        static void SetMaterialColor(Material m, Color c)
        {
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("baseColorFactor")) m.SetColor("baseColorFactor", c);
        }

        static bool LooksWhite(Color c)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return max > 0.72f && (max - min) < 0.16f;
        }

        static Color ModelTint(string key)
        {
            switch (key)
            {
                case "car": return new Color(0.22f, 0.26f, 0.34f);
                case "lowrider": return new Color(0.55f, 0.18f, 0.36f);
                case "minivan": return new Color(0.34f, 0.3f, 0.26f);
                case "money_tree": return new Color(0.2f, 0.55f, 0.18f);
                case "palm_tree": return new Color(0.16f, 0.38f, 0.17f);
                case "heart": return new Color(0.75f, 0.08f, 0.16f);
                case "fire_barrel": return new Color(0.28f, 0.16f, 0.08f);
                case "surveillance_camera": return new Color(0.12f, 0.15f, 0.2f);
                case "candle": return new Color(0.85f, 0.74f, 0.55f);
                case "film_reel": return new Color(0.42f, 0.42f, 0.46f);
                case "house": return new Color(0.35f, 0.24f, 0.18f);
                case "city_sign": return new Color(0.18f, 0.2f, 0.25f);
                case "helicopter_body": return new Color(0.08f, 0.1f, 0.12f);
                case "ee_butterfly": return new Color(0.22f, 0.52f, 0.85f);
                case "ee_crown_of_thorns": return new Color(0.74f, 0.58f, 0.25f);
                case "ee_pulitzer": return new Color(0.9f, 0.72f, 0.28f);
                case "ee_panther": return new Color(0.025f, 0.025f, 0.03f);
                case "ee_gnx": return new Color(0.18f, 0.2f, 0.24f);
                case "ee_hiiipower": return new Color(0.72f, 0.2f, 0.08f);
                case "ee_uncle_sam": return new Color(0.28f, 0.28f, 0.34f);
                case "ee_pglang": return new Color(0.8f, 0.78f, 0.68f);
                default: return new Color(0.42f, 0.38f, 0.32f);
            }
        }
    }
}
