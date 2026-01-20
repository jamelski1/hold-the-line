// WeaponSystem.cs - Continuous firing weapon with upgrade tiers
// Location: Assets/_HoldTheLine/Scripts/Player/
// Attach to: Player prefab (same object as PlayerController)

using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Manages automatic weapon firing with upgrade tiers.
    /// Coordinates with TargetingSystem for target selection.
    /// </summary>
    public class WeaponSystem : MonoBehaviour
    {
        public static WeaponSystem Instance { get; private set; }

        [Header("Weapon Mount")]
        [SerializeField] private Transform firePoint;

        [Header("Base Stats")]
        [SerializeField] private float baseDamage = 10f;
        [SerializeField] private float baseFireRate = 5f; // shots per second
        [SerializeField] private float baseBulletSpeed = 20f;
        [SerializeField] private int baseProjectileCount = 1;
        [SerializeField] private float baseSpreadAngle = 0f;

        [Header("Tier Multipliers")]
        [SerializeField] private WeaponTierData[] tierData;

        // Current state
        private WeaponTier currentTier = WeaponTier.Tier1_Pistol;
        private float fireTimer;
        private float damageMultiplier = 1f; // From pickups
        private int fireteamBonus = 0; // Additional projectiles from pickups

        // Cached current stats
        private float currentDamage;
        private float currentFireRate;
        private float currentBulletSpeed;
        private int currentProjectileCount;
        private float currentSpreadAngle;

        // Properties
        public WeaponTier CurrentTier => currentTier;
        public float CurrentDamage => currentDamage * damageMultiplier;
        public float DamageMultiplier => damageMultiplier;
        public int FireteamBonus => fireteamBonus;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultTiers();
        }

        private void Start()
        {
            if (firePoint == null)
            {
                firePoint = transform;
            }

            ResetWeapon();
        }

        private void InitializeDefaultTiers()
        {
            // Only initialize if not set in inspector
            if (tierData == null || tierData.Length == 0)
            {
                tierData = new WeaponTierData[]
                {
                    // Tier 1: Pistol - Basic single shot
                    new WeaponTierData
                    {
                        tierName = "Pistol",
                        damageMultiplier = 1f,
                        fireRateMultiplier = 1f,
                        projectileCount = 1,
                        spreadAngle = 0f,
                        bulletSpeedMultiplier = 1f
                    },
                    // Tier 2: SMG - Faster fire rate
                    new WeaponTierData
                    {
                        tierName = "SMG",
                        damageMultiplier = 1f,
                        fireRateMultiplier = 2f,
                        projectileCount = 1,
                        spreadAngle = 5f,
                        bulletSpeedMultiplier = 1.1f
                    },
                    // Tier 3: Rifle - More damage
                    new WeaponTierData
                    {
                        tierName = "Rifle",
                        damageMultiplier = 2f,
                        fireRateMultiplier = 1.5f,
                        projectileCount = 1,
                        spreadAngle = 2f,
                        bulletSpeedMultiplier = 1.3f
                    },
                    // Tier 4: Shotgun - Multiple projectiles
                    new WeaponTierData
                    {
                        tierName = "Shotgun",
                        damageMultiplier = 1.5f,
                        fireRateMultiplier = 1.2f,
                        projectileCount = 3,
                        spreadAngle = 15f,
                        bulletSpeedMultiplier = 1f
                    },
                    // Tier 5: Minigun - Maximum firepower
                    new WeaponTierData
                    {
                        tierName = "Minigun",
                        damageMultiplier = 1.2f,
                        fireRateMultiplier = 4f,
                        projectileCount = 2,
                        spreadAngle = 8f,
                        bulletSpeedMultiplier = 1.2f
                    }
                };
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
            {
                return;
            }

            // Continuous firing
            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                Fire();
                fireTimer = 1f / currentFireRate;
            }
        }

        /// <summary>
        /// Reset weapon to initial state
        /// </summary>
        public void ResetWeapon()
        {
            currentTier = WeaponTier.Tier1_Pistol;
            damageMultiplier = 1f;
            fireteamBonus = 0;
            fireTimer = 0f;
            RecalculateStats();
        }

        /// <summary>
        /// Upgrade weapon to next tier
        /// </summary>
        public bool UpgradeWeapon()
        {
            int nextTier = (int)currentTier + 1;
            if (nextTier >= tierData.Length)
            {
                return false; // Already at max tier
            }

            currentTier = (WeaponTier)nextTier;
            RecalculateStats();
            GameManager.Instance?.WeaponUpgraded(currentTier);

            Debug.Log($"[WeaponSystem] Upgraded to {tierData[nextTier].tierName}");
            return true;
        }

        /// <summary>
        /// Apply damage multiplier from pickup
        /// </summary>
        public void ApplyDamageMultiplier(float multiplier, float duration = 0f)
        {
            damageMultiplier *= multiplier;

            // If duration specified, reset after time
            if (duration > 0f)
            {
                // In a full implementation, you'd track this with a timer
                // For simplicity, permanent multipliers stack
            }

            Debug.Log($"[WeaponSystem] Damage multiplier now: {damageMultiplier}");
        }

        /// <summary>
        /// Add fireteam members (additional projectiles)
        /// </summary>
        public void AddFireteam(int count)
        {
            fireteamBonus += count;
            RecalculateStats();
            Debug.Log($"[WeaponSystem] Fireteam bonus now: {fireteamBonus}");
        }

        private void RecalculateStats()
        {
            int tierIndex = (int)currentTier;
            if (tierIndex >= tierData.Length)
            {
                tierIndex = tierData.Length - 1;
            }

            WeaponTierData data = tierData[tierIndex];

            currentDamage = baseDamage * data.damageMultiplier;
            currentFireRate = baseFireRate * data.fireRateMultiplier;
            currentBulletSpeed = baseBulletSpeed * data.bulletSpeedMultiplier;
            currentProjectileCount = data.projectileCount + fireteamBonus;
            currentSpreadAngle = data.spreadAngle;
        }

        private void Fire()
        {
            if (ObjectPool.Instance == null) return;

            // Get target direction from TargetingSystem
            Vector3 targetDirection = Vector3.up; // Default: shoot upward
            if (TargetingSystem.Instance != null)
            {
                targetDirection = TargetingSystem.Instance.GetTargetDirection(firePoint.position);
            }

            // Calculate final damage with multiplier
            float finalDamage = currentDamage * damageMultiplier;

            // Fire projectiles with spread
            if (currentProjectileCount == 1)
            {
                SpawnBullet(firePoint.position, targetDirection, finalDamage);
            }
            else
            {
                // Multiple projectiles with spread
                float halfSpread = currentSpreadAngle / 2f;
                float angleStep = currentSpreadAngle / (currentProjectileCount - 1);

                for (int i = 0; i < currentProjectileCount; i++)
                {
                    float angle = -halfSpread + (angleStep * i);
                    Vector3 spreadDir = Quaternion.Euler(0f, 0f, angle) * targetDirection;
                    SpawnBullet(firePoint.position, spreadDir, finalDamage);
                }
            }
        }

        private void SpawnBullet(Vector3 position, Vector3 direction, float damage)
        {
            GameObject bulletObj = ObjectPool.Instance.Get(PoolType.Bullet);
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.Initialize(position, direction, damage, currentBulletSpeed);
            }
        }

        /// <summary>
        /// Get current tier name for UI
        /// </summary>
        public string GetCurrentTierName()
        {
            int tierIndex = (int)currentTier;
            if (tierIndex < tierData.Length)
            {
                return tierData[tierIndex].tierName;
            }
            return "Unknown";
        }

        /// <summary>
        /// Check if can upgrade further
        /// </summary>
        public bool CanUpgrade()
        {
            return (int)currentTier < tierData.Length - 1;
        }
    }

    /// <summary>
    /// Data container for weapon tier configuration
    /// </summary>
    [System.Serializable]
    public class WeaponTierData
    {
        public string tierName = "Weapon";
        public float damageMultiplier = 1f;
        public float fireRateMultiplier = 1f;
        public int projectileCount = 1;
        public float spreadAngle = 0f;
        public float bulletSpeedMultiplier = 1f;
    }
}
