using UnityEngine;

namespace GKMC
{
    /// <summary>
    /// Lightweight camera presentation pass for the built-in pipeline: wider lens, subtle walk bob,
    /// sprint FOV push, vignette and animated grain. It is intentionally dependency-free so the
    /// project does not need a Post Processing package.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CinematicCameraEffects : MonoBehaviour
    {
        public float baseFov = 72f;
        public float sprintFov = 79f;
        public float bobAmplitude = 0.035f;
        public float bobFrequency = 8.5f;
        public float vignetteOpacity = 0.62f;
        public float grainOpacity = 0.045f;

        Camera _camera;
        Vector3 _baseLocalPosition;
        Texture2D _vignette;
        Texture2D _grain;
        float _seed;

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _baseLocalPosition = transform.localPosition;
            _seed = Random.value * 1000f;

            if (_camera != null)
            {
                _camera.fieldOfView = baseFov;
                _camera.allowHDR = true;
                _camera.allowMSAA = true;
                _camera.depthTextureMode |= DepthTextureMode.Depth;
            }
        }

        void LateUpdate()
        {
            if (_camera == null) return;

            float move = Mathf.Clamp01(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).magnitude);
            bool sprinting = Input.GetKey(KeyCode.LeftShift) && move > 0.1f;
            float targetFov = sprinting ? sprintFov : baseFov;
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFov, Time.deltaTime * 5f);

            float t = Time.time * bobFrequency + _seed;
            float sprintBoost = sprinting ? 1.55f : 1f;
            Vector3 bob = new Vector3(
                Mathf.Cos(t * 0.5f) * bobAmplitude * 0.45f,
                Mathf.Abs(Mathf.Sin(t)) * bobAmplitude * sprintBoost,
                0f) * move;
            transform.localPosition = Vector3.Lerp(transform.localPosition, _baseLocalPosition + bob, Time.deltaTime * 8f);
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            EnsureTextures();

            GUI.depth = -2000;
            Color old = GUI.color;

            GUI.color = new Color(1f, 1f, 1f, vignetteOpacity);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _vignette, ScaleMode.StretchToFill, true);

            GUI.color = new Color(1f, 1f, 1f, grainOpacity);
            float offsetX = Mathf.Repeat(Time.frameCount * 17f, 64f) / 64f;
            float offsetY = Mathf.Repeat(Time.frameCount * 29f, 64f) / 64f;
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, Screen.width, Screen.height), _grain, new Rect(offsetX, offsetY, Screen.width / 128f, Screen.height / 128f));

            GUI.color = old;
        }

        void EnsureTextures()
        {
            if (_vignette == null) _vignette = MakeVignette(256);
            if (_grain == null) _grain = MakeGrain(128);
        }

        static Texture2D MakeVignette(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float max = size * 0.58f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / max;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.58f) / 0.42f));
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
                }
            }

            tex.Apply(false, true);
            return tex;
        }

        static Texture2D MakeGrain(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float v = Random.Range(0.35f, 0.65f);
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }

            tex.Apply(false, true);
            return tex;
        }
    }
}
