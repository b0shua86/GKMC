#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GKMC.EditorTools
{
    /// <summary>
    /// The repo ships no ProjectSettings assets (everything is generated), so a fresh clone opens
    /// with Unity's serialized defaults — including Gamma color space, which washes out the PBR
    /// models and post-processed bloom. This nudges the project to Linear once, on load.
    /// </summary>
    [InitializeOnLoad]
    static class GKMCProjectBootstrap
    {
        static GKMCProjectBootstrap()
        {
            // Deferred: PlayerSettings can't always be written during domain reload itself.
            EditorApplication.delayCall += () =>
            {
                if (PlayerSettings.colorSpace != ColorSpace.Linear)
                {
                    PlayerSettings.colorSpace = ColorSpace.Linear;
                    Debug.Log("[GKMC] Color space set to Linear (correct PBR lighting + bloom).");
                }
            };
        }
    }
}
#endif
