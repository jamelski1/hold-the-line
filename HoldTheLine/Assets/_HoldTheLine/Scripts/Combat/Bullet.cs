// Bullet.cs - Projectile that damages enemies (3D Version)
// Location: Assets/_HoldTheLine/Scripts/Combat/
// Attach to: Bullet prefab (requires Collider with IsTrigger)

using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Projectile fired by WeaponSystem. Moves toward target and deals damage on contact.
    /// Uses object pooling for performance.
    ///
    /// 3D AXIS MAPPING:
    /// - Bullets move in +Z direction (toward zombies) by default
    /// - Bounds check uses Z position
    /// </summary>
    [RequireComponent(typeof(Collider))]
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

            // Rotate to face direction (for 3D, rotate around Y axis for XZ plane movement)
            if (direction != Vector3.zero)
            {
                cachedTransform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
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

            // Bounds check (despawn if off playfield) - uses Z axis now
            if (GameManager.Instance != null)
            {
                Vector3 pos = cachedTransform.position;
                if (pos.z > GameManager.Instance.SpawnZ + 2f ||
                    pos.z < GameManager.Instance.DespawnZ - 2f ||
                    pos.x < GameManager.Instance.PlayfieldMinX - 2f ||
                    pos.x > GameManager.Instance.PlayfieldMaxX + 2f)
                {
                    ReturnToPool();
                }
            }
        }

        // 3D collision detection (replaces OnTriggerEnter2D)
        private void OnTriggerEnter(Collider other)
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
