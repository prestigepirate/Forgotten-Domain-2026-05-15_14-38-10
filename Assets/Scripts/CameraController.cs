using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenDomain
{
    /// <summary>
    /// Tactical camera: scroll to zoom, WASD to pan, right-drag to orbit.
    /// Uses new Input System. Does NOT affect UI (UI is ScreenSpaceOverlay).
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Zoom")]
        [SerializeField] private float minDistance = 8f;
        [SerializeField] private float maxDistance = 60f;
        [SerializeField] private float zoomSpeed = 5f;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 0.5f;

        [Header("Orbit")]
        [SerializeField] private float orbitSpeed = 2f;
        [SerializeField] private float minPitch = 25f;
        [SerializeField] private float maxPitch = 80f;

        private float _distance = 30f;
        private float _pitch = 55f;
        private float _yaw;
        private Vector3 _target = Vector3.zero;
        private Vector3 _panOffset;

        private Camera _cam;
        private UnityEngine.EventSystems.PointerEventData _eventData;
        private System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> _raycastResults;

        // -------------------------------------------------------------- //
        //  Lifecycle
        // -------------------------------------------------------------- //

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
            
            _raycastResults = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            
            ApplyCamera();
        }

        private void Update()
        {
            if (Mouse.current == null) return;

            bool changed = false;

            // --- Zoom (scroll wheel) ---
            Vector2 scroll = Mouse.current.scroll.ReadValue();
            if (Mathf.Abs(scroll.y) > 0.001f)
            {
                _distance -= scroll.y * zoomSpeed * Mathf.Max(_distance * 0.3f, 2f);
                _distance = Mathf.Clamp(_distance, minDistance, maxDistance);
                changed = true;
            }

            // --- Orbit (right-click drag) ---
            if (Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                _yaw   += delta.x * orbitSpeed * 0.1f;
                _pitch -= delta.y * orbitSpeed * 0.1f;
                _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);
                changed = true;
            }

            // --- Pan (middle-click or left-click drag) ---
            if (Mouse.current.middleButton.isPressed || Mouse.current.leftButton.isPressed)
            {
                // Prevent panning when clicking on UI elements (Layer 5)
                if (!IsPointerOverUI())
                {
                    Vector2 delta = Mouse.current.delta.ReadValue();
                    Vector3 right = transform.right;
                    Vector3 fwd = Vector3.Cross(right, Vector3.up).normalized;
                    _panOffset -= right * delta.x * panSpeed * 0.1f;
                    _panOffset -= fwd * delta.y * panSpeed * 0.1f;
                    changed = true;
                }
            }

            // --- WASD pan ---
            Vector3 keyPan = Vector3.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed) keyPan.x -= 1;
                if (Keyboard.current.dKey.isPressed) keyPan.x += 1;
                if (Keyboard.current.wKey.isPressed) keyPan.z += 1;
                if (Keyboard.current.sKey.isPressed) keyPan.z -= 1;
            }

            if (keyPan != Vector3.zero)
            {
                keyPan = keyPan.normalized * panSpeed * Time.deltaTime * 15f;
                _panOffset += transform.right * keyPan.x;
                _panOffset += Vector3.Cross(transform.right, Vector3.up).normalized * keyPan.z;
                changed = true;
            }

            if (changed) ApplyCamera();
        }

        private bool IsPointerOverUI()
        {
            if (UnityEngine.EventSystems.EventSystem.current == null) return false;
            
            if (_eventData == null)
                _eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            
            _eventData.position = Mouse.current.position.ReadValue();
            _raycastResults.Clear();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(_eventData, _raycastResults);
            
            foreach (var r in _raycastResults)
            {
                if (r.gameObject.layer == 5) return true; // UI Layer
            }
            return false;
        }

        // -------------------------------------------------------------- //
        //  Apply
        // -------------------------------------------------------------- //

        private void ApplyCamera()
        {
            Vector3 targetPos = _target + _panOffset;
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0);
            transform.position = targetPos + rot * new Vector3(0, 0, -_distance);
            transform.LookAt(targetPos);
        }

        /// <summary>Focus the camera on a world position.</summary>
        public void FocusOn(Vector3 worldPos)
        {
            _target = worldPos;
            _panOffset = Vector3.zero;
            ApplyCamera();
        }
    }
}
