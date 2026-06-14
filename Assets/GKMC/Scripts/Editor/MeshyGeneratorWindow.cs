#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace GKMC.EditorTools
{
    /// <summary>
    /// In-Unity tool to generate (or fetch) the tour's 3D models with Meshy AI and drop them into
    /// Assets/StreamingAssets/Models. The API key is stored in EditorPrefs (per-machine, never in
    /// the repo). Mirrors tools/meshy_generate.py.
    /// </summary>
    public class MeshyGeneratorWindow : EditorWindow
    {
        const string API_BASE = "https://api.meshy.ai/openapi/v2/text-to-3d";
        const string PREF_KEY = "GKMC_MESHY_KEY";

        [Serializable] class Entry { public string key; public string prompt; public bool essential; public float target_size; public string glb_url; public string task_id; }
        [Serializable] class Catalog { public Entry[] models; }
        [Serializable] class PostResult { public string result; }
        [Serializable] class Urls { public string glb; }
        [Serializable] class Task { public string status; public int progress; public Urls model_urls; }

        string _key = "";
        bool _refine = false, _lowpoly = false, _hd = false, _force = false;
        Vector2 _scroll;
        Catalog _catalog;
        readonly HashSet<string> _selected = new HashSet<string>();
        string _outDir;
        string _catalogPath;

        // pump state
        IEnumerator _routine;
        UnityWebRequestAsyncOperation _waitOp;
        double _waitUntil;
        bool _busy;
        string _statusLine = "";

        public static void Open()
        {
            var w = GetWindow<MeshyGeneratorWindow>("Meshy Generator");
            w.minSize = new Vector2(520f, 560f);
        }

        void OnEnable()
        {
            _key = EditorPrefs.GetString(PREF_KEY, Environment.GetEnvironmentVariable("MESHY_API_KEY") ?? "");
            _outDir = Path.Combine(Application.dataPath, "StreamingAssets", "Models");
            _catalogPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tools", "meshy_models.json"));
            LoadCatalog();
        }

        void LoadCatalog()
        {
            try
            {
                if (File.Exists(_catalogPath))
                    _catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(_catalogPath));
            }
            catch (Exception e) { Debug.LogError("[GKMC] Could not read meshy_models.json: " + e.Message); }
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Meshy AI → good kid, m.A.A.d city models", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generates GLB models into Assets/StreamingAssets/Models. Loaded at runtime by ModelLibrary " +
                "(needs com.unity.cloud.gltfast + the GKMC_GLTFAST define — see GKMC menu). " +
                "Tip: add a glb_url or task_id to an entry in tools/meshy_models.json to use a community model.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("API key", EditorStyles.boldLabel);
            _key = EditorGUILayout.PasswordField("MESHY_API_KEY", _key);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save to EditorPrefs")) { EditorPrefs.SetString(PREF_KEY, _key); _statusLine = "Key saved."; }
                if (GUILayout.Button("Load from env")) { _key = Environment.GetEnvironmentVariable("MESHY_API_KEY") ?? _key; }
                if (GUILayout.Button("Clear")) { _key = ""; EditorPrefs.DeleteKey(PREF_KEY); }
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                _refine = EditorGUILayout.ToggleLeft("Refine (textures)", _refine, GUILayout.Width(150));
                _hd = EditorGUILayout.ToggleLeft("HD", _hd, GUILayout.Width(60));
                _lowpoly = EditorGUILayout.ToggleLeft("Low-poly", _lowpoly, GUILayout.Width(90));
                _force = EditorGUILayout.ToggleLeft("Overwrite", _force, GUILayout.Width(100));
            }

            EditorGUILayout.Space();
            if (_catalog == null || _catalog.models == null)
            {
                EditorGUILayout.HelpBox("tools/meshy_models.json not found at:\n" + _catalogPath, MessageType.Error);
                if (GUILayout.Button("Reload catalogue")) LoadCatalog();
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select essentials")) { _selected.Clear(); foreach (var m in _catalog.models) if (m.essential) _selected.Add(m.key); }
                if (GUILayout.Button("Select all")) { _selected.Clear(); foreach (var m in _catalog.models) _selected.Add(m.key); }
                if (GUILayout.Button("Select none")) _selected.Clear();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(220));
            foreach (var m in _catalog.models)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool sel = _selected.Contains(m.key);
                    bool newSel = EditorGUILayout.ToggleLeft(m.key + (m.essential ? "  ★" : ""), sel, GUILayout.Width(200));
                    if (newSel != sel) { if (newSel) _selected.Add(m.key); else _selected.Remove(m.key); }
                    bool exists = File.Exists(Path.Combine(_outDir, m.key + ".glb"));
                    EditorGUILayout.LabelField(exists ? "✓ have glb" : (m.glb_url != null && m.glb_url.Length > 0 ? "url" : (m.task_id != null && m.task_id.Length > 0 ? "task" : "generate")), GUILayout.Width(80));
                    using (new EditorGUI.DisabledScope(_busy))
                        if (GUILayout.Button("Make", GUILayout.Width(60))) RunFor(new List<Entry> { m });
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(_busy))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate selected", GUILayout.Height(30))) RunSelected();
                if (GUILayout.Button("Generate essentials", GUILayout.Height(30)))
                {
                    var list = new List<Entry>();
                    foreach (var m in _catalog.models) if (m.essential) list.Add(m);
                    RunFor(list);
                }
            }

            if (_busy) EditorGUILayout.HelpBox("Working… " + _statusLine, MessageType.None);
            else if (!string.IsNullOrEmpty(_statusLine)) EditorGUILayout.LabelField(_statusLine);
        }

        void RunSelected()
        {
            var list = new List<Entry>();
            foreach (var m in _catalog.models) if (_selected.Contains(m.key)) list.Add(m);
            RunFor(list);
        }

        void RunFor(List<Entry> entries)
        {
            if (string.IsNullOrEmpty(_key)) { EditorUtility.DisplayDialog("Meshy", "Set your API key first.", "OK"); return; }
            if (entries == null || entries.Count == 0) { _statusLine = "Nothing selected."; return; }
            Directory.CreateDirectory(_outDir);
            _routine = GenerateRoutine(entries);
            _busy = true;
            EditorApplication.update -= Pump;
            EditorApplication.update += Pump;
        }

        void Pump()
        {
            if (_routine == null) { EditorApplication.update -= Pump; return; }
            if (_waitOp != null) { if (!_waitOp.isDone) return; _waitOp = null; }
            if (_waitUntil > 0) { if (EditorApplication.timeSinceStartup < _waitUntil) return; _waitUntil = 0; }

            bool more;
            try { more = _routine.MoveNext(); }
            catch (Exception e) { Debug.LogError("[GKMC] Meshy error: " + e); more = false; }

            if (!more)
            {
                _routine = null;
                EditorApplication.update -= Pump;
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
                _busy = false;
                Repaint();
                return;
            }

            var c = _routine.Current;
            if (c is UnityWebRequestAsyncOperation op) _waitOp = op;
            else if (c is float f) _waitUntil = EditorApplication.timeSinceStartup + f;
            Repaint();
        }

        IEnumerator GenerateRoutine(List<Entry> entries)
        {
            int i = 0;
            foreach (var e in entries)
            {
                i++;
                string dest = Path.Combine(_outDir, e.key + ".glb");
                _statusLine = $"({i}/{entries.Count}) {e.key}";
                EditorUtility.DisplayProgressBar("Meshy", _statusLine, (float)i / entries.Count);

                if (File.Exists(dest) && !_force) { Debug.Log("[GKMC] skip (exists): " + e.key); continue; }

                string glbUrl = null;

                if (!string.IsNullOrEmpty(e.glb_url))
                {
                    glbUrl = e.glb_url;
                }
                else if (!string.IsNullOrEmpty(e.task_id))
                {
                    Task t = null;
                    foreach (var step in GetTask(e.task_id, r => t = r)) yield return step;
                    glbUrl = t?.model_urls?.glb;
                }
                else
                {
                    // preview
                    string previewId = null;
                    var prevBody = "{\"mode\":\"preview\",\"prompt\":" + Quote(e.prompt) +
                                   ",\"model_type\":\"" + (_lowpoly ? "lowpoly" : "standard") +
                                   "\",\"ai_model\":\"latest\",\"should_remesh\":true,\"target_formats\":[\"glb\"]}";
                    foreach (var step in Post(prevBody, id => previewId = id)) yield return step;
                    if (previewId == null) { Debug.LogError("[GKMC] preview failed: " + e.key); continue; }

                    Task t = null;
                    foreach (var step in PollTask(previewId, r => t = r)) yield return step;
                    if (t == null) { Debug.LogError("[GKMC] preview poll failed: " + e.key); continue; }
                    glbUrl = t.model_urls?.glb;

                    if (_refine)
                    {
                        string refineId = null;
                        var refBody = "{\"mode\":\"refine\",\"preview_task_id\":\"" + previewId +
                                      "\",\"enable_pbr\":true,\"hd_texture\":" + (_hd ? "true" : "false") +
                                      ",\"ai_model\":\"latest\",\"target_formats\":[\"glb\"]}";
                        foreach (var step in Post(refBody, id => refineId = id)) yield return step;
                        if (refineId != null)
                        {
                            Task rt = null;
                            foreach (var step in PollTask(refineId, r => rt = r)) yield return step;
                            if (rt?.model_urls?.glb != null) glbUrl = rt.model_urls.glb;
                        }
                    }
                }

                if (string.IsNullOrEmpty(glbUrl)) { Debug.LogError("[GKMC] no glb url: " + e.key); continue; }

                foreach (var step in Download(glbUrl, dest)) yield return step;
                Debug.Log("[GKMC] saved " + dest);
            }
        }

        IEnumerable Post(string jsonBody, Action<string> onId)
        {
            var req = new UnityWebRequest(API_BASE, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", "Bearer " + _key);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (Err(req)) { Debug.LogError("[GKMC] POST error: " + req.error + " " + req.downloadHandler.text); req.Dispose(); yield break; }
            var pr = JsonUtility.FromJson<PostResult>(req.downloadHandler.text);
            onId(pr?.result);
            req.Dispose();
        }

        IEnumerable GetTask(string id, Action<Task> onTask)
        {
            var req = UnityWebRequest.Get(API_BASE + "/" + id);
            req.SetRequestHeader("Authorization", "Bearer " + _key);
            yield return req.SendWebRequest();
            if (Err(req)) { Debug.LogError("[GKMC] GET error: " + req.error); req.Dispose(); yield break; }
            onTask(JsonUtility.FromJson<Task>(req.downloadHandler.text));
            req.Dispose();
        }

        IEnumerable PollTask(string id, Action<Task> onDone)
        {
            double start = EditorApplication.timeSinceStartup;
            while (true)
            {
                Task t = null;
                foreach (var step in GetTask(id, r => t = r)) yield return step;
                if (t == null) { onDone(null); yield break; }
                _statusLine = id.Substring(0, 8) + "… " + t.status + " " + t.progress + "%";
                if (t.status == "SUCCEEDED") { onDone(t); yield break; }
                if (t.status == "FAILED" || t.status == "CANCELED") { onDone(null); yield break; }
                if (EditorApplication.timeSinceStartup - start > 25 * 60) { onDone(null); yield break; }
                yield return 6f;
            }
        }

        IEnumerable Download(string url, string dest)
        {
            var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();
            if (Err(req)) { Debug.LogError("[GKMC] download error: " + req.error); req.Dispose(); yield break; }
            File.WriteAllBytes(dest, req.downloadHandler.data);
            req.Dispose();
        }

        static string Quote(string s)
        {
            return "\"" + (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ") + "\"";
        }

        static bool Err(UnityWebRequest r)
        {
#if UNITY_2020_1_OR_NEWER
            return r.result != UnityWebRequest.Result.Success;
#else
            return r.isNetworkError || r.isHttpError;
#endif
        }
    }
}
#endif
