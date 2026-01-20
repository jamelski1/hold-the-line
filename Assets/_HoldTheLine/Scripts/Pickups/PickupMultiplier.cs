// PickupMultiplier.cs - Moving pickup that grants damage/fireteam bonuses
// Location: Assets/_HoldTheLine/Scripts/Pickups/
// Attach to: PickupMultiplier prefab (requires Collider2D with IsTrigger)

using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Pickup that moves downward from spawn and grants bonuses on player collision.
    /// Types: x2 damage, x3 damage, +1 fireteam, +3 fireteam
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PickupMultiplier : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float horizontalOscillation = 0.5f;
        [SerializeField] private float oscillationSpeed = 2f;

        [Header("Visual")]
        [SerializeField] private Renderer pickupRenderer;
        [SerializeField] private TextMesh labelText; // Optional 3D text
        [SerializeField] private Color damageX2Color = Color.yellow;
        [SerializeField] private Color damageX3Color = new Color(1f, 0.5f, 0f); // Orange
        [SerializeField] private Color fireteamColor = Color.cyan;

        [Header("Pulse Effect")]
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float pulseScale = 0.1f;

        // Runtime state
        private MultiplierType multiplierType;
        private float startX;
        private float oscillationPhase;
        private bool isActive;
        private Vector3 baseScale;

        // Cached
        private Transform cachedTransform;

        private void Awake()
        {
            cachedTransform = transform;
            baseScale = cachedTransform.localScale;
        }

        private void OnEnable()
        {
            isActive = true;
            oscillationPhase = Random.Range(0f, Mathf.PI * 2f);
        }

        /// <summary>
        /// Initialize pickup with spawn parameters
        /// </summary>
        public void Initialize(Vector3 spawnPosition, MultiplierType type)
        {
            cachedTransform.position = spawnPosition;
            startX = spawnPosition.x;
            multiplierType = type;
            isActive = true;

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // Set color and label based on type
            Color color;
            string label;

            switch (multiplierType)
            {
                case MultiplierType.DamageX2:
                    color = damageX2Color;
                    label = "x2";
                    break;
                case MultiplierType.DamageX3:
                    color = damageX3Color;
                    label = "x3";
                    break;
                case MultiplierType.FireteamPlus1:
                    color = fireteamColor;
                    label = "+1";
                    break;
                case MultiplierType.FireteamPlus3:
                    color = fireteamColor;
                    label = "+3";
                    break;
                default:
                    color = Color.white;
                    label = "?";
                    break;
            }

            if (pickupRenderer != null)
            {
                pickupRenderer.material.color = color;
            }

            if (labelText != null)
            {
                labelText.text = label;
            }
        }

        private void Update()
        {
            if (!isActive) return;

            // Move downward with horizontal oscillation
            oscillationPhase += oscillationSpeed * Time.deltaTime;
            float xOffset = Mathf.Sin(oscillationPhase) * horizontalOscillation;

            Vector3 pos = cachedTransform.position;
            pos.x = startX + xOffset;
            pos.y -= moveSpeed * Time.deltaTime;
            cachedTransform.position = pos;

            // Pulse effect
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
            cachedTransform.localScale = baseScale * pulse;

            // Check despawn
            if (GameManager.Instance != null && pos.y < GameManager.Instance.DespawnY)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            // Check if collided with player
            if (other.CompareTag("Player"))
            {
                ApplyEffect();
                ReturnToPool();
            }
        }

        private void ApplyEffect()
        {
            if (WeaponSystem.Instance == null) return;

            switch (multiplierType)
            {
                case MultiplierType.DamageX2:
                    WeaponSystem.Instance.ApplyDamageMultiplier(2f);
                    Debug.Log("[PickupMultiplier] Applied x2 damage multiplier");
                    break;

                case MultiplierType.DamageX3:
                    WeaponSystem.Instance.ApplyDamageMultiplier(3f);
                    Debug.Log("[PickupMultiplier] Applied x3 damage multiplier");
                    break;

                case MultiplierType.FireteamPlus1:
                    WeaponSystem.Instance.AddFireteam(1);
                    Debug.Log("[PickupMultiplier] Added +1 fireteam");
                    break;

                case MultiplierType.FireteamPlus3:
                    WeaponSystem.Instance.AddFireteam(3);
                    Debug.Log("[PickupMultiplier] Added +3 fireteam");
                    break;
            }
        }

        private void ReturnToPool()
        {
            if (!isActive) return;

            isActive = false;
            cachedTransform.localScale = baseScale;
            ObjectPool.Instance?.Return(PoolType.Pickup, gameObject);
        }

        /// <summary>
        /// Get the display label for this pickup type
        /// </summary>
        public static string GetTypeLabel(MultiplierType type)
        {
            return type switch
            {
                MultiplierType.DamageX2 => "x2 DMG",
                MultiplierType.DamageX3 => "x3 DMG",
                MultiplierType.FireteamPlus1 => "+1 TEAM",
                MultiplierType.FireteamPlus3 => "+3 TEAM",
                _ => "?"
            };
        }
    }
}
