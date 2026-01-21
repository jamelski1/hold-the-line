// UpgradeTarget.cs - Shootable target that grants weapon upgrades
// Location: Assets/_HoldTheLine/Scripts/Pickups/
// Attach to: UpgradeTarget prefab (requires Collider2D with IsTrigger)

using System;
using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Floating target (crate/drone/panel) that can be shot to unlock weapon upgrades.
    /// Risk-reward: While shooting this, player isn't shooting zombies.
    /// Implements IDamageable for bullet interaction.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class UpgradeTarget : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float baseHealth = 100f;
        [SerializeField] private float despawnTime = 30f; // Disappears after this time

        [Header("Movement")]
        [SerializeField] private float floatAmplitude = 0.3f;
        [SerializeField] private float floatSpeed = 1.5f;
        [SerializeField] private float driftSpeed = 0.2f; // Slow horizontal drift

        [Header("Visual Feedback")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color fullHealthColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.red;
        [SerializeField] private Color selectedColor = Color.yellow;

        [Header("UI Indicator")]
        [SerializeField] private Transform healthBarPivot; // Scale X to show health
        [SerializeField] private Renderer healthBarRenderer;

        // Runtime state
        private float maxHealth;
        private float currentHealth;
        private float spawnTime;
        private float floatPhase;
        private Vector3 basePosition;
        private float driftDirection;
        private bool isActive;
        private bool isSelected;
        private Color originalColor;

        // Events
        public event Action<float> OnHealthChanged; // normalized 0-1
        public event Action OnDestroyed;

        // IDamageable implementation
        public bool IsAlive => isActive && currentHealth > 0f;

        // Cached
        private Transform cachedTransform;

        public Transform GetTransform() => cachedTransform;

        private void Awake()
        {
            cachedTransform = transform;
            if (targetRenderer != null)
            {
                originalColor = targetRenderer.material.color;
            }
        }

        private void OnEnable()
        {
            isActive = true;
            floatPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            driftDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;

            // Register with targeting system
            TargetingSystem.Instance?.RegisterUpgradeTarget(this);
        }

        private void OnDisable()
        {
            isActive = false;
            isSelected = false;
            TargetingSystem.Instance?.UnregisterUpgradeTarget(this);
        }

        /// <summary>
        /// Initialize upgrade target with spawn parameters
        /// </summary>
        public void Initialize(Vector3 spawnPosition, float healthMultiplier = 1f)
        {
            cachedTransform.position = spawnPosition;
            basePosition = spawnPosition;

            maxHealth = baseHealth * healthMultiplier;
            currentHealth = maxHealth;
            spawnTime = Time.time;
            isActive = true;
            isSelected = false;

            UpdateHealthVisual();

            if (targetRenderer != null)
            {
                targetRenderer.material.color = fullHealthColor;
            }
        }

        private void Update()
        {
            if (!isActive) return;

            UpdateMovement();
            UpdateSelection();
            CheckDespawn();
        }

        private void UpdateMovement()
        {
            // Floating motion
            floatPhase += floatSpeed * Time.deltaTime;
            float yOffset = Mathf.Sin(floatPhase) * floatAmplitude;

            // Slow drift
            basePosition.x += driftDirection * driftSpeed * Time.deltaTime;

            // Clamp to playfield
            if (GameManager.Instance != null)
            {
                float minX = GameManager.Instance.PlayfieldMinX + 1f;
                float maxX = GameManager.Instance.PlayfieldMaxX - 1f;

                if (basePosition.x <= minX || basePosition.x >= maxX)
                {
                    driftDirection *= -1f;
                    basePosition.x = Mathf.Clamp(basePosition.x, minX, maxX);
                }
            }

            cachedTransform.position = basePosition + new Vector3(0f, yOffset, 0f);
        }

        private void UpdateSelection()
        {
            // Check if we're the current target
            bool shouldBeSelected = TargetingSystem.Instance != null &&
                                   TargetingSystem.Instance.CurrentUpgradeTarget == this;

            if (shouldBeSelected != isSelected)
            {
                isSelected = shouldBeSelected;
                UpdateSelectionVisual();
            }
        }

        private void UpdateSelectionVisual()
        {
            if (targetRenderer == null) return;

            if (isSelected)
            {
                // Highlight when selected
                targetRenderer.material.color = selectedColor;
            }
            else
            {
                // Return to health-based color
                UpdateHealthVisual();
            }
        }

        private void CheckDespawn()
        {
            // Auto-despawn after timeout
            if (Time.time - spawnTime > despawnTime)
            {
                ReturnToPool();
            }
        }

        /// <summary>
        /// IDamageable: Take damage from bullets
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            currentHealth -= damage;
            OnHealthChanged?.Invoke(currentHealth / maxHealth);

            UpdateHealthVisual();

            if (currentHealth <= 0f)
            {
                GrantUpgrade();
            }
        }

        private void UpdateHealthVisual()
        {
            float healthPercent = currentHealth / maxHealth;

            // Update color based on health (if not selected)
            if (targetRenderer != null && !isSelected)
            {
                targetRenderer.material.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
            }

            // Update health bar
            if (healthBarPivot != null)
            {
                Vector3 scale = healthBarPivot.localScale;
                scale.x = healthPercent;
                healthBarPivot.localScale = scale;
            }

            if (healthBarRenderer != null)
            {
                healthBarRenderer.material.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
            }
        }

        private void GrantUpgrade()
        {
            if (!isActive) return;

            // Grant weapon upgrade
            bool upgraded = WeaponSystem.Instance?.UpgradeWeapon() ?? false;

            if (upgraded)
            {
                Debug.Log("[UpgradeTarget] Weapon upgraded!");
            }
            else
            {
                Debug.Log("[UpgradeTarget] Weapon at max tier, no upgrade granted");
            }

            OnDestroyed?.Invoke();
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            if (!isActive) return;

            isActive = false;
            isSelected = false;
            ObjectPool.Instance?.Return(PoolType.UpgradeTarget, gameObject);
        }

        /// <summary>
        /// Get normalized health (0-1) for UI
        /// </summary>
        public float GetHealthNormalized()
        {
            return currentHealth / maxHealth;
        }

        /// <summary>
        /// Get remaining time before despawn
        /// </summary>
        public float GetRemainingTime()
        {
            return Mathf.Max(0f, despawnTime - (Time.time - spawnTime));
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Visualize tap detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);
        }
#endif
    }
}
