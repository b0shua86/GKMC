#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GKMC.EditorTools
{
    /// <summary>Convenience menu items. The experience auto-boots in any scene, so these are optional.</summary>
    public static class GKMCSceneSetup
    {
        [MenuItem("GKMC/Create Tour Scene", false, 0)]
        public static void CreateTourScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("GKMC_Experience");
            go.AddComponent<GKMCExperience>();
            Selection.activeObject = go;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorUtility.DisplayDialog("GKMC",
                "Tour scene created. Press Play to walk the album.\n\n" +
                "For Meshy models: GKMC ▸ Open Meshy Generator, then GKMC ▸ Enable glTFast (GKMC_GLTFAST).",
                "Got it");
        }

        [MenuItem("GKMC/Add Tour To Open Scene", false, 1)]
        public static void AddToScene()
        {
            if (Object.FindFirstObjectByType<GKMCExperience>() != null)
            {
                EditorUtility.DisplayDialog("GKMC", "This scene already has a GKMC_Experience.", "OK");
                return;
            }
            var go = new GameObject("GKMC_Experience");
            go.AddComponent<GKMCExperience>();
            Selection.activeObject = go;
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        [MenuItem("GKMC/Enable glTFast (GKMC_GLTFAST)", false, 20)]
        public static void EnableGltfast()
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            if (defines.Contains("GKMC_GLTFAST"))
            {
                EditorUtility.DisplayDialog("GKMC", "GKMC_GLTFAST is already enabled.", "OK");
                return;
            }
            defines = string.IsNullOrEmpty(defines) ? "GKMC_GLTFAST" : defines + ";GKMC_GLTFAST";
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
            EditorUtility.DisplayDialog("GKMC",
                "GKMC_GLTFAST enabled for " + group + ".\n\n" +
                "Make sure the package 'com.unity.cloud.gltfast' is installed (Window ▸ Package Manager) " +
                "so GLB models in StreamingAssets/Models load at runtime.", "OK");
        }

        [MenuItem("GKMC/Open Meshy Generator", false, 21)]
        public static void OpenGenerator() => MeshyGeneratorWindow.Open();
    }
}
#endif
