// SpawnerManager.cs - Handles spawning of zombies, pickups, and upgrade targets (3D Version)
// Location: Assets/_HoldTheLine/Scripts/Enemies/
// Attach to: Empty GameObject named "SpawnerManager" in scene

using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Manages spawning of all game entities with difficulty scaling.
    /// Handles wave progression and spawn patterns.
    ///
    /// 3D AXIS MAPPING:
    /// - Spawns at high Z (spawnZ), entities move toward low Z (despawnZ)
    /// - X axis is horizontal spread
    /// - Y is height (fixed at ground level)
    /// </summary>
    public class SpawnerManager : MonoBehaviour
    {
        public static SpawnerManager Instance { get; private set; }

        [Header("Spawn Area")]
        [SerializeField] private float spawnZOffset = 1f; // Offset beyond spawn line
        [SerializeField] private float spawnHeight = 0.5f; // Y position for spawned objects

        [Header("Zombie Spawning")]
        [SerializeField] private float baseZombieSpawnRate = 1f;
        [SerializeField] private float spawnRateIncreasePerWave = 0.2f;
        [SerializeField] private float maxSpawnRate = 5f;
        [SerializeField] private int zombiesPerWave = 10;
        [SerializeField] private int zombiesIncreasePerWave = 5;

        [Header("Zombie Scaling")]
        [SerializeField] private float zombieHealthScalePerWave = 0.1f;
        [SerializeField] private float zombieSpeedScalePerWave = 0.05f;
        [SerializeField] private float maxHealthMultiplier = 3f;
        [SerializeField] private float maxSpeedMultiplier = 2f;

        [Header("Pickup Spawning")]
        [SerializeField] private float pickupSpawnChance = 0.15f;
        [SerializeField] private float minPickupInterval = 5f;
        [SerializeField] private float maxPickupInterval = 15f;

        [Header("Upgrade Target Spawning")]
        [SerializeField] private float upgradeTargetSpawnChance = 0.1f;
        [SerializeField] private float minUpgradeInterval = 10f;
        [SerializeField] private float maxUpgradeInterval = 25f;
        [SerializeField] private int maxActiveUpgradeTargets = 2;

        // Spawn state
        private float zombieSpawnTimer;
        private float pickupSpawnTimer;
        private float upgradeTargetSpawnTimer;
        private int zombiesSpawnedThisWave;
        private int targetZombiesThisWave;
        private bool isSpawning;

        // Current difficulty
        private float currentSpawnRate;
        private float currentHealthMultiplier;
        private float currentSpeedMultiplier;

        // Cached bounds (3D - uses Z for depth)
        private float minX;
        private float maxX;
        private float spawnZ;

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
            CacheBounds();
            ResetSpawnTimers();
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                GameManager.Instance.OnWaveStarted += HandleWaveStarted;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnWaveStarted -= HandleWaveStarted;
            }
        }

        private void CacheBounds()
        {
            if (GameManager.Instance != null)
            {
                minX = GameManager.Instance.PlayfieldMinX;
                maxX = GameManager.Instance.PlayfieldMaxX;
                spawnZ = GameManager.Instance.SpawnZ + spawnZOffset;
            }
            else
            {
                // Fallback defaults for 3D
                minX = -4f;
                maxX = 4f;
                spawnZ = 13f;
            }
        }

        private void Update()
        {
            if (!isSpawning) return;

            UpdateZombieSpawning();
            UpdatePickupSpawning();
            UpdateUpgradeTargetSpawning();

            // Check for wave completion
            CheckWaveCompletion();
        }

        private void HandleGameStateChanged(GameState newState)
        {
            isSpawning = newState == GameState.Playing;

            if (newState == GameState.Menu || newState == GameState.Fail)
            {
                ResetSpawnTimers();
            }
        }

        private void HandleWaveStarted(int waveNumber)
        {
            // Calculate difficulty for this wave
            currentSpawnRate = Mathf.Min(
                baseZombieSpawnRate + (spawnRateIncreasePerWave * (waveNumber - 1)),
                maxSpawnRate
            );

            currentHealthMultiplier = Mathf.Min(
                1f + (zombieHealthScalePerWave * (waveNumber - 1)),
                maxHealthMultiplier
            );

            currentSpeedMultiplier = Mathf.Min(
                1f + (zombieSpeedScalePerWave * (waveNumber - 1)),
                maxSpeedMultiplier
            );

            // Calculate zombies for this wave
            targetZombiesThisWave = zombiesPerWave + (zombiesIncreasePerWave * (waveNumber - 1));
            zombiesSpawnedThisWave = 0;

            // Reset spawn timers
            zombieSpawnTimer = 0f;
            pickupSpawnTimer = Random.Range(minPickupInterval, maxPickupInterval);
            upgradeTargetSpawnTimer = Random.Range(minUpgradeInterval, maxUpgradeInterval);

            Debug.Log($"[SpawnerManager] Wave {waveNumber} started - Zombies: {targetZombiesThisWave}, " +
                      $"SpawnRate: {currentSpawnRate:F1}/s, Health: x{currentHealthMultiplier:F1}, " +
                      $"Speed: x{currentSpeedMultiplier:F1}");
        }

        private void ResetSpawnTimers()
        {
            zombieSpawnTimer = 0f;
            pickupSpawnTimer = Random.Range(minPickupInterval, maxPickupInterval);
            upgradeTargetSpawnTimer = Random.Range(minUpgradeInterval, maxUpgradeInterval);
            zombiesSpawnedThisWave = 0;
        }

        #region Zombie Spawning

        private void UpdateZombieSpawning()
        {
            // Don't spawn if we've reached the wave limit
            if (zombiesSpawnedThisWave >= targetZombiesThisWave) return;

            zombieSpawnTimer -= Time.deltaTime;
            if (zombieSpawnTimer <= 0f)
            {
                SpawnZombie();
                zombieSpawnTimer = 1f / currentSpawnRate;
            }
        }

        private void SpawnZombie()
        {
            if (ObjectPool.Instance == null) return;

            GameObject zombieObj = ObjectPool.Instance.Get(PoolType.Zombie);
            if (zombieObj == null) return;

            ZombieUnit zombie = zombieObj.GetComponent<ZombieUnit>();
            if (zombie != null)
            {
                // Random X position within bounds, fixed Y height, spawn at spawnZ
                float x = Random.Range(minX + 0.5f, maxX - 0.5f);
                Vector3 spawnPos = new Vector3(x, spawnHeight, spawnZ);

                zombie.Initialize(spawnPos, currentHealthMultiplier, currentSpeedMultiplier);
                zombiesSpawnedThisWave++;
            }
        }

        #endregion

        #region Pickup Spawning

        private void UpdatePickupSpawning()
        {
            pickupSpawnTimer -= Time.deltaTime;
            if (pickupSpawnTimer <= 0f)
            {
                if (Random.value < pickupSpawnChance)
                {
                    SpawnPickup();
                }
                pickupSpawnTimer = Random.Range(minPickupInterval, maxPickupInterval);
            }
        }

        private void SpawnPickup()
        {
            if (ObjectPool.Instance == null) return;

            GameObject pickupObj = ObjectPool.Instance.Get(PoolType.Pickup);
            if (pickupObj == null) return;

            PickupMultiplier pickup = pickupObj.GetComponent<PickupMultiplier>();
            if (pickup != null)
            {
                float x = Random.Range(minX + 0.5f, maxX - 0.5f);
                Vector3 spawnPos = new Vector3(x, spawnHeight, spawnZ);

                // Random multiplier type
                MultiplierType type = (MultiplierType)Random.Range(0, 4);
                pickup.Initialize(spawnPos, type);
            }
        }

        #endregion

        #region Upgrade Target Spawning

        private void UpdateUpgradeTargetSpawning()
        {
            // Don't spawn if max active targets
            if (TargetingSystem.Instance != null &&
                TargetingSystem.Instance.GetActiveUpgradeTargetCount() >= maxActiveUpgradeTargets)
            {
                return;
            }

            // Don't spawn if weapon is maxed
            if (WeaponSystem.Instance != null && !WeaponSystem.Instance.CanUpgrade())
            {
                return;
            }

            upgradeTargetSpawnTimer -= Time.deltaTime;
            if (upgradeTargetSpawnTimer <= 0f)
            {
                if (Random.value < upgradeTargetSpawnChance)
                {
                    SpawnUpgradeTarget();
                }
                upgradeTargetSpawnTimer = Random.Range(minUpgradeInterval, maxUpgradeInterval);
            }
        }

        private void SpawnUpgradeTarget()
        {
            if (ObjectPool.Instance == null) return;

            GameObject targetObj = ObjectPool.Instance.Get(PoolType.UpgradeTarget);
            if (targetObj == null) return;

            UpgradeTarget upgradeTarget = targetObj.GetComponent<UpgradeTarget>();
            if (upgradeTarget != null)
            {
                // Spawn in upper portion of playfield (high Z values)
                float x = Random.Range(minX + 1f, maxX - 1f);
                float z = Random.Range(GameManager.Instance.PlayfieldMaxZ - 2f, GameManager.Instance.SpawnZ - 1f);
                Vector3 spawnPos = new Vector3(x, spawnHeight, z);

                // Scale health with wave
                float healthMultiplier = 1f + (GameManager.Instance.CurrentWave * 0.5f);
                upgradeTarget.Initialize(spawnPos, healthMultiplier);
            }
        }

        #endregion

        private void CheckWaveCompletion()
        {
            // Wave is complete when all zombies spawned AND all zombies killed
            if (zombiesSpawnedThisWave >= targetZombiesThisWave)
            {
                // Check if all zombies are dead
                if (TargetingSystem.Instance != null &&
                    TargetingSystem.Instance.GetActiveZombieCount() == 0)
                {
                    GameManager.Instance?.CompleteWave();
                }
            }
        }

        /// <summary>
        /// Force spawn an upgrade target (for testing or special events)
        /// </summary>
        public void ForceSpawnUpgradeTarget()
        {
            SpawnUpgradeTarget();
        }

        /// <summary>
        /// Force spawn a pickup (for testing or special events)
        /// </summary>
        public void ForceSpawnPickup()
        {
            SpawnPickup();
        }

        /// <summary>
        /// Get spawn progress for UI
        /// </summary>
        public float GetWaveProgress()
        {
            if (targetZombiesThisWave == 0) return 0f;

            int activeZombies = TargetingSystem.Instance?.GetActiveZombieCount() ?? 0;
            int killedZombies = zombiesSpawnedThisWave - activeZombies;
            float killProgress = (float)killedZombies / targetZombiesThisWave;

            return killProgress;
        }

        /// <summary>
        /// Get remaining zombies in wave
        /// </summary>
        public int GetRemainingZombies()
        {
            int active = TargetingSystem.Instance?.GetActiveZombieCount() ?? 0;
            int notSpawned = targetZombiesThisWave - zombiesSpawnedThisWave;
            return active + notSpawned;
        }
    }
}
