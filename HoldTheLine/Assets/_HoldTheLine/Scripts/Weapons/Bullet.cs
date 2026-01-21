using UnityEngine;
using HoldTheLine.Enemy;

namespace HoldTheLine.Weapons
{
    public class Bullet : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float lifetime = 3f;

        private Vector2 direction;
        private float spawnTime;

        private void OnEnable()
        {
            spawnTime = Time.time;
        }

        public void Initialize(Vector2 dir)
        {
            direction = dir.normalized;
        }

        private void Update()
        {
            transform.Translate(direction * speed * Time.deltaTime);

            if (Time.time - spawnTime > lifetime)
            {
                Deactivate();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Enemy"))
            {
                ZombieHealth health = other.GetComponent<ZombieHealth>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                }
                Deactivate();
            }
        }

        private void Deactivate()
        {
            gameObject.SetActive(false);
        }
    }
}
