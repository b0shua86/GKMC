using UnityEngine;

namespace GKMC
{
    /// <summary>Gentle vertical float (candles, easter eggs, floating cups).</summary>
    public class Bobber : MonoBehaviour
    {
        public float amplitude = 0.2f, speed = 1.5f, phase = 0f, sway = 0f;
        Vector3 _base;
        void Start() { _base = transform.localPosition; if (phase == 0f) phase = Random.value * 6.28f; }
        void Update()
        {
            float t = Time.time * speed + phase;
            transform.localPosition = _base + new Vector3(Mathf.Cos(t) * sway, Mathf.Sin(t) * amplitude, 0f);
        }
    }

    /// <summary>Constant rotation (surveillance cameras, film reels, rotors, signs).</summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        public float degPerSec = 30f;
        void Update() => transform.Rotate(axis * degPerSec * Time.deltaTime, Space.Self);
    }

    /// <summary>Breathing scale pulse (the heart in "Real").</summary>
    public class Pulser : MonoBehaviour
    {
        public float amplitude = 0.08f, speed = 2f;
        Vector3 _base;
        void Start() { _base = transform.localScale; }
        void Update() => transform.localScale = _base * (1f + Mathf.Sin(Time.time * speed) * amplitude);
    }

    /// <summary>Oscillate rotation around an axis (sweeping searchlights / camera pans).</summary>
    public class Sweeper : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        public float range = 35f, speed = 1f, phase = 0f;
        Vector3 _base;
        void Start() { _base = transform.localEulerAngles; if (phase == 0f) phase = Random.value * 6.28f; }
        void Update() => transform.localEulerAngles = _base + axis * Mathf.Sin(Time.time * speed + phase) * range;
    }

    /// <summary>Alternating two-colour flash with intensity wobble (police sirens).</summary>
    public class Flasher : MonoBehaviour
    {
        public Light light;
        public Color a = Color.red, b = Color.blue;
        public float speed = 6f, minI = 1f, maxI = 4f;
        void Update()
        {
            if (light == null) return;
            float k = Mathf.Sin(Time.time * speed) * 0.5f + 0.5f;
            light.color = Color.Lerp(a, b, Mathf.Round(k));
            light.intensity = Mathf.Lerp(minI, maxI, Mathf.PingPong(Time.time * speed * 2f, 1f));
        }
    }

    /// <summary>Warm random flicker for fire-barrel point lights.</summary>
    public class FireFlicker : MonoBehaviour
    {
        public Light light;
        public float baseIntensity = 2.5f, range = 1.2f, speed = 14f;
        float _seed;
        void Start() { _seed = Random.value * 100f; }
        void Update()
        {
            if (light == null) return;
            float n = Mathf.PerlinNoise(_seed, Time.time * speed);
            light.intensity = baseIntensity + (n - 0.5f) * 2f * range;
        }
    }

    /// <summary>Hydraulic lowrider hop, in the classic styles you'd see on Crenshaw.</summary>
    public class LowriderHop : MonoBehaviour
    {
        public enum Style { FrontBack, SideToSide, ThreeWheel, FullBounce, Pancake }
        public Style style = Style.FullBounce;
        public float speed = 2.6f;   // hop tempo
        public float height = 0.45f; // vertical travel
        public float tilt = 15f;     // body lean in degrees
        public float phase = 0f;

        Vector3 _basePos, _baseEuler;

        void Start()
        {
            _basePos = transform.localPosition;
            _baseEuler = transform.localEulerAngles;
            if (phase == 0f) phase = Random.value * 6.28f;
        }

        void Update()
        {
            float t = Time.time * speed + phase;
            float bounce = Mathf.Abs(Mathf.Sin(t));   // springy 0..1
            float s = Mathf.Sin(t);
            Vector3 p = _basePos;
            Vector3 e = _baseEuler;

            switch (style)
            {
                case Style.FrontBack:   // nose dives and lifts
                    e.x = _baseEuler.x - s * tilt;
                    p.y = _basePos.y + bounce * height * 0.35f;
                    break;
                case Style.SideToSide:  // rocks left and right
                    e.z = _baseEuler.z + s * tilt;
                    p.y = _basePos.y + bounce * height * 0.2f;
                    break;
                case Style.ThreeWheel:  // one corner held high, slow sway
                    e.z = _baseEuler.z + tilt * 0.9f;
                    e.x = _baseEuler.x - Mathf.Sin(t * 0.4f) * tilt * 0.4f;
                    p.y = _basePos.y + 0.22f;
                    break;
                case Style.FullBounce:  // whole car hops
                    p.y = _basePos.y + bounce * height;
                    break;
                case Style.Pancake:     // front, then back, alternating slam
                    e.x = _baseEuler.x - Mathf.Max(0f, s) * tilt + Mathf.Max(0f, -s) * tilt;
                    p.y = _basePos.y + Mathf.Abs(s) * height * 0.25f;
                    break;
            }

            transform.localPosition = p;
            transform.localEulerAngles = e;
        }
    }

    /// <summary>Sine alpha fade for the dissolving silhouettes in "Sing About Me".</summary>
    public class Fader : MonoBehaviour
    {
        public float min = 0.05f, max = 0.6f, speed = 0.8f, phase = 0f;
        Material _mat;
        void Start()
        {
            var r = GetComponent<Renderer>();
            if (r != null) _mat = r.material; // instance
            if (phase == 0f) phase = Random.value * 6.28f;
        }
        void Update()
        {
            if (_mat == null) return;
            var c = _mat.color;
            c.a = Mathf.Lerp(min, max, Mathf.Sin(Time.time * speed + phase) * 0.5f + 0.5f);
            _mat.color = c;
        }
    }
}
