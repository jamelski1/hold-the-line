using UnityEngine;

namespace HoldTheLine.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float screenPadding = 0.5f;

        private Camera mainCamera;
        private float minX, maxX;

        private void Start()
        {
            mainCamera = Camera.main;
            CalculateBounds();
        }

        private void CalculateBounds()
        {
            if (mainCamera == null) return;

            float halfHeight = mainCamera.orthographicSize;
            float halfWidth = halfHeight * mainCamera.aspect;

            minX = -halfWidth + screenPadding;
            maxX = halfWidth - screenPadding;
        }

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            float horizontal = 0f;

            // Keyboard input
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                horizontal = -1f;
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                horizontal = 1f;

            // Touch input for mobile
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                Vector3 touchPos = mainCamera.ScreenToWorldPoint(touch.position);

                if (touchPos.x < transform.position.x - 0.1f)
                    horizontal = -1f;
                else if (touchPos.x > transform.position.x + 0.1f)
                    horizontal = 1f;
            }

            Move(horizontal);
        }

        private void Move(float direction)
        {
            Vector3 newPosition = transform.position + Vector3.right * direction * moveSpeed * Time.deltaTime;
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            transform.position = newPosition;
        }
    }
}
