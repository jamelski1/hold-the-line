// PlayerHealth.cs - Manages player health and damage
// Location: Assets/_HoldTheLine/Scripts/Player/
// Attach to: Player prefab (same object as PlayerController)

using System;
using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Manages player health, damage intake, and death.
    /// </summary>
    public class PlayerHealth : MonoBehaviour
    {
        public static PlayerHealth Instance { get; private set; }

        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float invincibilityDuration = 0.5f;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer playerRenderer;
        [SerializeField] private Color damageFlashColor = Color.red;
        [SerializeField] private float flashDuration = 0.1f;

        // State
        private float currentHealth;
        private float invincibilityTimer;
        private bool isInvincible;
        private Color originalColor;

        // Events
        public event Action<float, float> OnHealthChanged; // current, max
        public event Action OnDamageTaken;
        public event Action OnDied;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercent => currentHealth / maxHealth;
        public bool IsAlive => currentHealth > 0;

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
            if (playerRenderer != null)
            {
                originalColor = playerRenderer.material.color;
            }

            ResetHealth();
        }

        private void Update()
        {
            // Update invincibility timer
            if (isInvincible)
            {
                invincibilityTimer -= Time.deltaTime;
                if (invincibilityTimer <= 0f)
                {
                    isInvincible = false;
                    RestoreColor();
                }
            }
        }

        /// <summary>
        /// Reset health to max (called on game start/restart)
        /// </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            isInvincible = false;
            invincibilityTimer = 0f;
            RestoreColor();
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Apply damage to player
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!IsAlive || isInvincible) return;

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamageTaken?.Invoke();

            // Flash red
            FlashDamage();

            // Brief invincibility
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Heal the player
        /// </summary>
        public void Heal(float amount)
        {
            if (!IsAlive) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            OnDied?.Invoke();
            GameManager.Instance?.PlayerDied();
        }

        private void FlashDamage()
        {
            if (playerRenderer == null) return;

            playerRenderer.material.color = damageFlashColor;
            Invoke(nameof(RestoreColor), flashDuration);
        }

        private void RestoreColor()
        {
            if (playerRenderer != null)
            {
                playerRenderer.material.color = originalColor;
            }
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.Playing && GameManager.Instance.CurrentWave == 1)
            {
                ResetHealth();
            }
        }
    }
}
