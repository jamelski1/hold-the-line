// PlayerController.cs - Handles horizontal movement (3D Version)
// Location: Assets/_HoldTheLine/Scripts/Player/
// Attach to: Player prefab

using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Handles player horizontal movement via keyboard and touch drag.
    /// Player is clamped to playfield bounds and only moves on X axis.
    ///
    /// 3D AXIS MAPPING (Top-Down View):
    /// - X axis = horizontal movement (left/right)
    /// - Z axis = fixed (player stays at PlayerZ)
    /// - Y axis = height (fixed at ground level)
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 15f;
        [SerializeField] private float smoothTime = 0.05f;

        [Header("Touch Settings")]
        [SerializeField] private float dragSensitivity = 1f;
        [SerializeField] private bool useRelativeDrag = true;

        // Current state
        private float targetX;
        private float currentVelocity;
        private bool isDragging;
        private Vector2 lastTouchPosition;
        private Camera mainCamera;

        // Bounds cache
        private float minX;
        private float maxX;
        private float fixedY = 0.5f; // Height above ground
        private float fixedZ;        // Depth position (player line)

        public bool IsDragging => isDragging;
        public Vector3 Position => transform.position;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            mainCamera = Camera.main;
            CacheBounds();
            ResetPosition();
        }

        private void CacheBounds()
        {
            if (GameManager.Instance != null)
            {
                minX = GameManager.Instance.PlayfieldMinX;
                maxX = GameManager.Instance.PlayfieldMaxX;
                fixedZ = GameManager.Instance.PlayerZ;
            }
            else
            {
                // Fallback defaults for 3D
                minX = -4f;
                maxX = 4f;
                fixedZ = -8f;
            }
        }

        /// <summary>
        /// Reset player to center position
        /// </summary>
        public void ResetPosition()
        {
            targetX = 0f;
            transform.position = new Vector3(0f, fixedY, fixedZ);
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameActive())
            {
                return;
            }

            HandleInput();
            UpdateMovement();
        }

        private void HandleInput()
        {
            // Keyboard input (always available)
            HandleKeyboardInput();

#if UNITY_EDITOR
            // Mouse input for editor testing
            HandleMouseInput();
#else
            // Touch input for mobile
            HandleTouchInput();
#endif
        }

        private void HandleKeyboardInput()
        {
            float horizontal = 0f;

            // Arrow keys
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                horizontal = -1f;
            }
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                horizontal = 1f;
            }

            if (horizontal != 0f)
            {
                targetX += horizontal * moveSpeed * Time.deltaTime;
                targetX = Mathf.Clamp(targetX, minX, maxX);
            }
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartDrag(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                UpdateDrag(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                EndDrag();
            }
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0)
            {
                if (isDragging)
                {
                    EndDrag();
                }
                return;
            }

            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    StartDrag(touch.position);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isDragging)
                    {
                        UpdateDrag(touch.position);
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    EndDrag();
                    break;
            }
        }

        private void StartDrag(Vector2 screenPosition)
        {
            isDragging = true;
            lastTouchPosition = screenPosition;
        }

        private void UpdateDrag(Vector2 screenPosition)
        {
            if (useRelativeDrag)
            {
                // Relative drag - move based on delta
                float deltaX = (screenPosition.x - lastTouchPosition.x) / Screen.width;
                float worldDeltaX = deltaX * (maxX - minX) * 2f * dragSensitivity;
                targetX = Mathf.Clamp(targetX + worldDeltaX, minX, maxX);
            }
            else
            {
                // Absolute position - map screen X to world X
                float normalizedX = screenPosition.x / Screen.width;
                targetX = Mathf.Lerp(minX, maxX, normalizedX);
            }

            lastTouchPosition = screenPosition;
        }

        private void EndDrag()
        {
            isDragging = false;
        }

        private void UpdateMovement()
        {
            // Smooth movement to target position
            float newX = Mathf.SmoothDamp(
                transform.position.x,
                targetX,
                ref currentVelocity,
                smoothTime,
                moveSpeed
            );

            // Apply clamped position (X movement only, Y and Z fixed)
            transform.position = new Vector3(
                Mathf.Clamp(newX, minX, maxX),
                fixedY,
                fixedZ
            );
        }

        /// <summary>
        /// Get world position from screen position (for targeting system)
        /// </summary>
        public Vector3 ScreenToWorldPosition(Vector2 screenPos)
        {
            if (mainCamera == null) mainCamera = Camera.main;

            // Create a ray from the camera through the screen position
            Ray ray = mainCamera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));

            // Find where the ray intersects the XZ plane (Y = 0)
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            // Fallback
            return new Vector3(0, 0, fixedZ);
        }

        /// <summary>
        /// Check if a screen position is on the player
        /// </summary>
        public bool IsPositionOnPlayer(Vector2 screenPos)
        {
            Vector3 worldPos = ScreenToWorldPosition(screenPos);
            float distance = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(worldPos.x, 0, worldPos.z)
            );
            return distance < 1f; // 1 unit radius
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Visualize movement bounds (on XZ plane)
            Gizmos.color = Color.cyan;
            float z = Application.isPlaying ? fixedZ : -8f;
            float minBound = Application.isPlaying ? minX : -4f;
            float maxBound = Application.isPlaying ? maxX : 4f;
            Gizmos.DrawLine(new Vector3(minBound, 0.5f, z), new Vector3(maxBound, 0.5f, z));
        }
#endif
    }
}
