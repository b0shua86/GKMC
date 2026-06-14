using System.Collections.Generic;

namespace GKMC
{
    /// <summary>
    /// Canonical catalogue of the 3D models the experience can use. Each prop is built as a
    /// procedural primitive by default; if a matching model exists (generated with Meshy AI and
    /// placed in Resources/GKMC_Models or StreamingAssets/Models) it transparently replaces the
    /// primitive. Keys here are shared with tools/meshy_models.json (the generator) so the two
    /// stay in step.
    /// </summary>
    public class ModelDef
    {
        public string key;
        public float targetSize;   // largest dimension in metres; the loaded model is fit to this
        public bool grounded;      // sit the base on the floor (true) or centre on the anchor (false)

        public ModelDef(string key, float targetSize, bool grounded = true)
        {
            this.key = key; this.targetSize = targetSize; this.grounded = grounded;
        }
    }

    public static class MeshyModels
    {
        static readonly Dictionary<string, ModelDef> _defs = Build();

        static Dictionary<string, ModelDef> Build()
        {
            var d = new Dictionary<string, ModelDef>();
            void A(string k, float s, bool g = true) => d[k] = new ModelDef(k, s, g);

            // Track-world props.
            A("car", 4.6f);
            A("lowrider", 4.8f);
            A("minivan", 4.9f);
            A("money_tree", 7f);
            A("palm_tree", 9f);
            A("heart", 3.6f, false);
            A("fire_barrel", 1.7f);
            A("surveillance_camera", 0.9f, false);
            A("candle", 0.9f);
            A("film_reel", 2.5f, false);
            A("house", 8f);
            A("city_sign", 5f, false);
            A("helicopter_body", 6f, false);

            // Career easter-egg emblems.
            A("ee_butterfly", 1.4f, false);          // To Pimp a Butterfly
            A("ee_crown_of_thorns", 1.3f, false);    // Mr. Morale & the Big Steppers
            A("ee_pulitzer", 1.1f, false);           // DAMN. (Pulitzer Prize)
            A("ee_panther", 1.6f);                   // Black Panther: The Album
            A("ee_gnx", 4.6f);                       // GNX
            A("ee_hiiipower", 1.4f, false);          // Section.80 (HiiiPoWeR)
            A("ee_uncle_sam", 1.6f);                 // Super Bowl LIX halftime
            A("ee_pglang", 1.5f);                    // pgLang
            return d;
        }

        public static ModelDef Get(string key)
            => _defs.TryGetValue(key, out var def) ? def : new ModelDef(key, 2f);
    }
}
