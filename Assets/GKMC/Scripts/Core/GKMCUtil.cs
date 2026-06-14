using UnityEngine;

namespace GKMC
{
    /// <summary>
    /// Low-level helpers for spawning primitives, materials, lights, signs and
    /// particle systems entirely from code. The whole experience is generated
    /// procedurally so the project runs with zero hand-authored scenes/prefabs.
    /// </summary>
    public static class GKMCUtil
    {
        static Shader _standard;
        static Shader Standard => _standard != null ? _standard : (_standard = Shader.Find("Standard"));

        static Font _font;
        public static Font UIFont
        {
            get
            {
                if (_font != null) return _font;
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        // ---- Materials -----------------------------------------------------

        public static Material Mat(Color color, float metallic = 0f, float smoothness = 0.25f)
        {
            var m = new Material(Standard);
            m.color = color;
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", smoothness);
            return m;
        }

        public static Material MatEmissive(Color color, Color emission, float intensity = 2f, float metallic = 0f, float smoothness = 0.4f)
        {
            var m = Mat(color, metallic, smoothness);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            return m;
        }

        public static Material MatTransparent(Color color)
        {
            var m = new Material(Standard);
            m.SetFloat("_Mode", 3f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
            m.color = color;
            m.SetFloat("_Glossiness", 0.6f);
            return m;
        }

        // ---- Primitives ----------------------------------------------------

        public static GameObject Prim(PrimitiveType type, Transform parent, string name,
            Vector3 pos, Vector3 scale, Material mat, bool collider = true, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) Object.Destroy(c);
            }
            return go;
        }

        public static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale,
            Material mat, bool collider = true, Vector3? euler = null)
            => Prim(PrimitiveType.Cube, parent, name, pos, scale, mat, collider, euler);

        public static GameObject Cyl(Transform parent, string name, Vector3 pos, Vector3 scale,
            Material mat, bool collider = false, Vector3? euler = null)
            => Prim(PrimitiveType.Cylinder, parent, name, pos, scale, mat, collider, euler);

        public static GameObject Sphere(Transform parent, string name, Vector3 pos, Vector3 scale,
            Material mat, bool collider = false, Vector3? euler = null)
            => Prim(PrimitiveType.Sphere, parent, name, pos, scale, mat, collider, euler);

        // ---- Lights --------------------------------------------------------

        public static Light PointLight(Transform parent, string name, Vector3 pos, Color color,
            float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            return l;
        }

        public static Light SpotLight(Transform parent, string name, Vector3 pos, Vector3 euler,
            Color color, float intensity, float range, float angle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.spotAngle = angle;
            return l;
        }

        // ---- 3D world-space text -------------------------------------------

        public static TextMesh Sign(Transform parent, string name, Vector3 pos, string text,
            Color color, float size = 0.6f, Vector3? euler = null,
            TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = UIFont;
            tm.fontSize = 90;
            tm.characterSize = size;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            if (UIFont != null) mr.sharedMaterial = UIFont.material;
            return tm;
        }

        // ---- Particle systems ---------------------------------------------

        static Shader _particle;
        static Shader ParticleShader
        {
            get
            {
                if (_particle != null) return _particle;
                _particle = Shader.Find("Particles/Standard Unlit");
                if (_particle == null) _particle = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
                if (_particle == null) _particle = Shader.Find("Sprites/Default");
                return _particle;
            }
        }

        public static ParticleSystem Particles(Transform parent, string name, Vector3 pos,
            Color color, float rate, float size, float lifetime, Vector3 emitBox,
            Vector3 velocity, float gravity = 0f, bool additive = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop();

            var main = ps.main;
            main.loop = true;
            main.startColor = color;
            main.startSize = size;
            main.startLifetime = lifetime;
            main.startSpeed = 0f;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2000;

            var emission = ps.emission;
            emission.rateOverTime = rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = emitBox;

            if (velocity != Vector3.zero)
            {
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = new ParticleSystem.MinMaxCurve(velocity.x);
                vel.y = new ParticleSystem.MinMaxCurve(velocity.y);
                vel.z = new ParticleSystem.MinMaxCurve(velocity.z);
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var pm = new Material(ParticleShader);
            pm.color = color;
            renderer.material = pm;

            ps.Play();
            return ps;
        }

        // ---- Misc ----------------------------------------------------------

        public static Color Dark(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, c.a);
        public static Color Bright(Color c, float f) => new Color(
            Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), c.a);
    }
}
