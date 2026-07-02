using System.Collections.Generic;
using UnityEngine;
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
        // One import per GLB, shared by every instance. The tour places some props dozens of
        // times (29 candles alone); importing per instance would duplicate every mesh and
        // texture in memory and multiply load time, so all instances share one GltfImport.
        static readonly Dictionary<string, Task<GltfImport>> _imports = new Dictionary<string, Task<GltfImport>>();

        static Task<GltfImport> GetImportAsync(string key)
        {
            if (!_imports.TryGetValue(key, out var task))
            {
                task = ImportAsync(key);
                _imports[key] = task;
            }
            return task;
        }

        static async Task<GltfImport> ImportAsync(string key)
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Models", key + ".glb");
            string uri = path.Contains("://") ? path : "file://" + path;
            var import = new GltfImport();
            bool ok = await import.Load(uri);
            if (!ok) { import.Dispose(); return null; }
            return import;
        }

        static async void LoadGlbAsync(string key, Transform anchor, List<Transform> fallbackChildren)
        {
            try
            {
                var import = await GetImportAsync(key);
                if (import == null || anchor == null) { _fallbacks++; return; }

                var holder = new GameObject("model");
                holder.transform.SetParent(anchor, false);
                bool inst = await import.InstantiateMainSceneAsync(holder.transform);
                if (!inst || anchor == null) { if (holder != null) Object.Destroy(holder); _fallbacks++; return; }

                TameMaterials(holder.transform);
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
        /// Meshy GLBs are authored fully metallic (metallicFactor/roughnessFactor = 1). In the
        /// Built-in render pipeline a fully-metallic surface shows no diffuse colour — it just
        /// mirrors the bright sky and reads as flat white, hiding the texture. glTFast's shaders
        /// are forks of Unity Standard, so turning metallic down to 0 (and ignoring the
        /// metallic-roughness map) makes the base-colour texture show as albedo again.
        /// </summary>
        static void TameMaterials(Transform root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || m.shader == null || !m.shader.name.StartsWith("glTF/")) continue;
                    if (m.HasProperty("metallicFactor")) m.SetFloat("metallicFactor", 0f);
                    if (m.HasProperty("roughnessFactor")) m.SetFloat("roughnessFactor", 0.55f);
                    m.DisableKeyword("_METALLICGLOSSMAP"); // use the scalar factors, not the metal map
                }
            }
        }
    }
}
