using UnityEngine;

namespace GKMC
{
    /// <summary>First-person walker. WASD to move, mouse to look, Shift to sprint, Space to jump, Esc to free the cursor.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 5.5f;
        public float sprintSpeed = 9.5f;
        public float lookSensitivity = 2.4f;
        public float jumpHeight = 1.3f;
        public float gravity = -18f;
        public float baseFov = 72f;         // wider lens than Unity's 60° default
        public float sprintFovBoost = 7f;   // lens pushes out while sprinting
        public float bobAmplitude = 0.035f; // subtle head bob while walking
        public float bobFrequency = 8.5f;

        CharacterController _cc;
        Camera _cam;
        float _pitch;
        float _yVel;
        bool _cursorLocked = true;
        Vector3 _camBase;
        float _bobPhase;

        public Camera Cam => _cam;

        public static PlayerController Create(Transform parent, Vector3 pos)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.tag = "Player";

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            // Kinematic rigidbody guarantees the world triggers fire OnTriggerEnter.
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(go.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.Skybox;   // show the sky / clouds, not a flat colour
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 1600f;
            cam.fieldOfView = 72f;
            camGo.AddComponent<AudioListener>();

            var pc = go.AddComponent<PlayerController>();
            pc._cc = cc;
            pc._cam = cam;
            return pc;
        }

        void Start()
        {
            LockCursor(true);
            _camBase = _cam.transform.localPosition;
        }

        void Update()
        {
            HandleCursor();
            if (_cursorLocked) Look();
            Move();
        }

        void LateUpdate()
        {
            // Game-feel: subtle head bob while walking and a lens push while sprinting.
            float move = Mathf.Clamp01(new Vector2(Input.GetAxisRaw("Horizontal"),
                                                   Input.GetAxisRaw("Vertical")).magnitude);
            bool moving = _cc.isGrounded && move > 0.1f;
            bool sprint = Input.GetKey(KeyCode.LeftShift) && move > 0.1f;

            if (moving) _bobPhase += Time.deltaTime * bobFrequency * (sprint ? 1.35f : 1f);
            Vector3 bob = moving
                ? new Vector3(Mathf.Cos(_bobPhase * 0.5f) * 0.5f, Mathf.Abs(Mathf.Sin(_bobPhase)), 0f)
                  * (bobAmplitude * move)
                : Vector3.zero;
            _cam.transform.localPosition =
                Vector3.Lerp(_cam.transform.localPosition, _camBase + bob, Time.deltaTime * 10f);

            float targetFov = baseFov + (sprint ? sprintFovBoost : 0f);
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, Time.deltaTime * 5f);
        }

        void HandleCursor()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) LockCursor(false);
            if (!_cursorLocked && Input.GetMouseButtonDown(0)) LockCursor(true);
        }

        void LockCursor(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Look()
        {
            float mx = Input.GetAxis("Mouse X") * lookSensitivity;
            float my = Input.GetAxis("Mouse Y") * lookSensitivity;
            transform.Rotate(Vector3.up, mx);
            _pitch = Mathf.Clamp(_pitch - my, -85f, 85f);
            _cam.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }

        void Move()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = (transform.right * h + transform.forward * v);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
            Vector3 horizontal = dir * speed;

            if (_cc.isGrounded)
            {
                _yVel = -2f;
                if (Input.GetKeyDown(KeyCode.Space))
                    _yVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            _yVel += gravity * Time.deltaTime;

            Vector3 motion = horizontal + Vector3.up * _yVel;
            _cc.Move(motion * Time.deltaTime);
        }
    }
}
