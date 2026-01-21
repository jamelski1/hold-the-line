using UnityEngine;
using HoldTheLine.Player;

namespace HoldTheLine.Enemy
{
    public class Zombie : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;

        [Header("Combat")]
        [SerializeField] private int damage = 1;

        private Camera mainCamera;
        private float destroyY;

        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                destroyY = -mainCamera.orthographicSize - 1f;
            }
            else
            {
                destroyY = -10f;
            }
        }

        private void Update()
        {
            Move();
            CheckBounds();
        }

        private void Move()
        {
            transform.Translate(Vector3.down * moveSpeed * Time.deltaTime);
        }

        private void CheckBounds()
        {
            if (transform.position.y < destroyY)
            {
                Deactivate();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
                Deactivate();
            }
        }

        private void Deactivate()
        {
            gameObject.SetActive(false);
        }

        public void SetSpeed(float speed)
        {
            moveSpeed = speed;
        }
    }
}
