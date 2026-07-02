using UnityEngine;
#if GKMC_POSTFX
using UnityEngine.Rendering.PostProcessing;
#endif

namespace GKMC
{
    /// <summary>
    /// Runtime graphics setup. The repo ships no hand-authored ProjectSettings, so a fresh clone
    /// opens with Unity's defaults — 4 pixel lights for a scene with dozens, no HDR, no
    /// post-processing. This applies sensible quality settings from code and wires up the
    /// Post Processing v2 stack (bloom / ACES tonemapping / vignette / FXAA) when the
    /// com.unity.postprocessing package and the GKMC_POSTFX define are present.
    /// </summary>
    public static class VisualQuality
    {
        public static void Apply(Camera cam)
        {
            QualitySettings.vSyncCount = 1;
            QualitySettings.pixelLightCount = 12;                // fire barrels, sirens, candles…
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 130f;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.softParticles = true;                // fire/smoke blend into geometry
            QualitySettings.realtimeReflectionProbes = true;

            cam.depthTextureMode |= DepthTextureMode.Depth;      // for soft particles

            bool postfx = TrySetupPostFx(cam);
            // HDR pairs with the tonemapper; without the stack, LDR + MSAA is the better combo
            // (Built-in forward can't do hardware MSAA on an HDR target anyway).
            cam.allowHDR = postfx;
            QualitySettings.antiAliasing = postfx ? 0 : 4;       // FXAA when post is on, else MSAA
            if (!postfx)
                Debug.Log("[GKMC] Running without post-processing (package missing or resources " +
                          "not found) — visuals fall back to plain forward rendering.");
        }

#if GKMC_POSTFX
        static bool TrySetupPostFx(Camera cam)
        {
            var resources = LoadResources();
            if (resources == null) return false;

            // Layer 1 (TransparentFX) exists in every project, so no TagManager asset is needed.
            const int volumeLayer = 1;

            var layer = cam.gameObject.AddComponent<PostProcessLayer>();
            layer.Init(resources);
            layer.volumeTrigger = cam.transform;
            layer.volumeLayer = 1 << volumeLayer;
            layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;

            var bloom = ScriptableObject.CreateInstance<Bloom>();
            bloom.enabled.Override(true);
            bloom.intensity.Override(2.6f);
            bloom.threshold.Override(1.05f);
            bloom.softKnee.Override(0.6f);
            bloom.diffusion.Override(8.5f);

            var grading = ScriptableObject.CreateInstance<ColorGrading>();
            grading.enabled.Override(true);
            grading.tonemapper.Override(Tonemapper.ACES);
            grading.postExposure.Override(0.25f);
            grading.saturation.Override(12f);
            grading.contrast.Override(8f);

            var vignette = ScriptableObject.CreateInstance<Vignette>();
            vignette.enabled.Override(true);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.45f);

            PostProcessManager.instance.QuickVolume(volumeLayer, 100f, bloom, grading, vignette);
            return true;
        }

        static PostProcessResources LoadResources()
        {
#if UNITY_EDITOR
            var r = UnityEditor.AssetDatabase.LoadAssetAtPath<PostProcessResources>(
                "Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset");
            if (r != null) return r;
#endif
            // Player builds need the asset referenced by something that ships; if it made it into
            // the build (or in the editor as a fallback), this finds the loaded instance.
            var all = Resources.FindObjectsOfTypeAll<PostProcessResources>();
            return all.Length > 0 ? all[0] : null;
        }
#else
        static bool TrySetupPostFx(Camera cam) => false;
#endif
    }
}
