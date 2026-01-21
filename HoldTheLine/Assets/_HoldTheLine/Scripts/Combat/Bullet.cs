// Bullet.cs - Projectile that damages enemies
// Location: Assets/_HoldTheLine/Scripts/Combat/
// Attach to: Bullet prefab (requires Collider2D with IsTrigger)

using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Projectile fired by WeaponSystem. Moves toward target and deals damage on contact.
    /// Uses object pooling for performance.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Bullet : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private float defaultSpeed = 20f;
        [SerializeField] private float maxLifetime = 3f;

        // Runtime state (set by WeaponSystem)
        private float speed;
        private float damage;
        private Vector3 direction;
        private float lifetime;
        private bool isActive;

        // Cached components
        private Transform cachedTransform;

        private void Awake()
        {
            cachedTransform = transform;
        }

        private void OnEnable()
        {
            lifetime = maxLifetime;
            isActive = true;
        }

        /// <summary>
        /// Initialize bullet with fire parameters
        /// </summary>
        public void Initialize(Vector3 startPosition, Vector3 targetDirection, float bulletDamage, float bulletSpeed = 0f)
        {
            cachedTransform.position = startPosition;
            direction = targetDirection.normalized;
            damage = bulletDamage;
            speed = bulletSpeed > 0f ? bulletSpeed : defaultSpeed;
            lifetime = maxLifetime;
            isActive = true;

            // Rotate to face direction
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            cachedTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            if (!isActive) return;

            // Move bullet
            cachedTransform.position += direction * (speed * Time.deltaTime);

            // Lifetime check
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
            {
                ReturnToPool();
                return;
            }

            // Bounds check (despawn if off screen)
            if (GameManager.Instance != null)
            {
                Vector3 pos = cachedTransform.position;
                if (pos.y > GameManager.Instance.SpawnY + 2f ||
                    pos.y < GameManager.Instance.DespawnY ||
                    pos.x < GameManager.Instance.PlayfieldMinX - 2f ||
                    pos.x > GameManager.Instance.PlayfieldMaxX + 2f)
                {
                    ReturnToPool();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            // Check for damageable target
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                damageable.TakeDamage(damage);
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            if (!isActive) return;

            isActive = false;
            ObjectPool.Instance?.Return(PoolType.Bullet, gameObject);
        }
    }
}
