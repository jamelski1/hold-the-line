// ZombieUnit.cs - Enemy that moves toward player
// Location: Assets/_HoldTheLine/Scripts/Enemies/
// Attach to: Zombie prefab (requires Collider2D with IsTrigger)

using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Zombie enemy that moves vertically downward.
    /// Damages player on contact or when reaching bottom threshold.
    /// Implements IDamageable for bullet interaction.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ZombieUnit : MonoBehaviour, IDamageable
    {
        [Header("Movement")]
        [SerializeField] private float baseSpeed = 2f;
        [SerializeField] private float speedVariance = 0.5f;

        [Header("Combat")]
        [SerializeField] private float baseHealth = 30f;
        [SerializeField] private float contactDamage = 20f;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer zombieRenderer;
        [SerializeField] private Color damageFlashColor = Color.white;
        [SerializeField] private float flashDuration = 0.05f;

        // Runtime state
        private float currentHealth;
        private float currentSpeed;
        private bool isActive;
        private Color originalColor;

        // Cached
        private Transform cachedTransform;

        // IDamageable implementation
        public bool IsAlive => isActive && currentHealth > 0f;

        public Transform GetTransform() => cachedTransform;

        private void Awake()
        {
            cachedTransform = transform;
            if (zombieRenderer != null)
            {
                originalColor = zombieRenderer.material.color;
            }
        }

        private void OnEnable()
        {
            isActive = true;
            currentHealth = baseHealth;
            currentSpeed = baseSpeed + Random.Range(-speedVariance, speedVariance);

            // Register with targeting system
            TargetingSystem.Instance?.RegisterZombie(this);

            if (zombieRenderer != null)
            {
                zombieRenderer.material.color = originalColor;
            }
        }

        private void OnDisable()
        {
            isActive = false;
            TargetingSystem.Instance?.UnregisterZombie(this);
        }

        /// <summary>
        /// Initialize zombie with spawn parameters
        /// </summary>
        public void Initialize(Vector3 spawnPosition, float healthMultiplier = 1f, float speedMultiplier = 1f)
        {
            cachedTransform.position = spawnPosition;
            currentHealth = baseHealth * healthMultiplier;
            currentSpeed = (baseSpeed + Random.Range(-speedVariance, speedVariance)) * speedMultiplier;
            isActive = true;
        }

        private void Update()
        {
            if (!isActive) return;

            // Move downward
            cachedTransform.position += Vector3.down * (currentSpeed * Time.deltaTime);

            // Check if reached despawn threshold
            if (GameManager.Instance != null)
            {
                if (cachedTransform.position.y <= GameManager.Instance.DespawnY)
                {
                    // Zombie reached the line - damage player and despawn
                    ReachedPlayer();
                }
            }
        }

        /// <summary>
        /// IDamageable: Take damage from bullets
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            currentHealth -= damage;

            // Visual feedback
            FlashDamage();

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (!isActive) return;

            isActive = false;
            GameManager.Instance?.RegisterZombieKill();

            // Return to pool
            ObjectPool.Instance?.Return(PoolType.Zombie, gameObject);
        }

        private void ReachedPlayer()
        {
            if (!isActive) return;

            // Damage the player
            PlayerHealth.Instance?.TakeDamage(contactDamage);

            isActive = false;

            // Return to pool
            ObjectPool.Instance?.Return(PoolType.Zombie, gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            // Check if collided with player
            if (other.CompareTag("Player"))
            {
                PlayerHealth.Instance?.TakeDamage(contactDamage);
                Die(); // Zombie dies on contact
            }
        }

        private void FlashDamage()
        {
            if (zombieRenderer == null) return;

            zombieRenderer.material.color = damageFlashColor;
            Invoke(nameof(RestoreColor), flashDuration);
        }

        private void RestoreColor()
        {
            if (zombieRenderer != null && isActive)
            {
                zombieRenderer.material.color = originalColor;
            }
        }

        /// <summary>
        /// Set health multiplier (for wave scaling)
        /// </summary>
        public void SetHealthMultiplier(float multiplier)
        {
            currentHealth = baseHealth * multiplier;
        }

        /// <summary>
        /// Set speed multiplier (for wave scaling)
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            currentSpeed = (baseSpeed + Random.Range(-speedVariance, speedVariance)) * multiplier;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Visualize contact damage radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
#endif
    }
}
