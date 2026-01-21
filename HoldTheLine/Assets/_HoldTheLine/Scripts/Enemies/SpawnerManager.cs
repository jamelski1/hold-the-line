// SpawnerManager.cs - Handles spawning of zombies, pickups, and upgrade targets
// Location: Assets/_HoldTheLine/Scripts/Enemies/
// Attach to: Empty GameObject named "SpawnerManager" in scene

using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Manages spawning of all game entities with difficulty scaling.
    /// Handles wave progression and spawn patterns.
    /// </summary>
    public class SpawnerManager : MonoBehaviour
    {
        public static SpawnerManager Instance { get; private set; }

        [Header("Spawn Area")]
        [SerializeField] private float spawnYOffset = 1f; // Offset above spawn line

        [Header("Zombie Spawning")]
        [SerializeField] private float baseZombieSpawnRate = 1f; // Zombies per second at wave 1
        [SerializeField] private float spawnRateIncreasePerWave = 0.2f;
        [SerializeField] private float maxSpawnRate = 5f;
        [SerializeField] private int zombiesPerWave = 10;
        [SerializeField] private int zombiesIncreasePerWave = 5;

        [Header("Zombie Scaling")]
        [SerializeField] private float zombieHealthScalePerWave = 0.1f; // +10% per wave
        [SerializeField] private float zombieSpeedScalePerWave = 0.05f; // +5% per wave
        [SerializeField] private float maxHealthMultiplier = 3f;
        [SerializeField] private float maxSpeedMultiplier = 2f;

        [Header("Pickup Spawning")]
        [SerializeField] private float pickupSpawnChance = 0.15f; // 15% chance per spawn cycle
        [SerializeField] private float minPickupInterval = 5f;
        [SerializeField] private float maxPickupInterval = 15f;

        [Header("Upgrade Target Spawning")]
        [SerializeField] private float upgradeTargetSpawnChance = 0.1f; // 10% chance
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

        // Cached bounds
        private float minX;
        private float maxX;
        private float spawnY;

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
                spawnY = GameManager.Instance.SpawnY + spawnYOffset;
            }
            else
            {
                minX = -2.5f;
                maxX = 2.5f;
                spawnY = 7f;
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
                // Random X position within bounds
                float x = Random.Range(minX + 0.5f, maxX - 0.5f);
                Vector3 spawnPos = new Vector3(x, spawnY, 0f);

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
                Vector3 spawnPos = new Vector3(x, spawnY, 0f);

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
                // Spawn in upper portion of playfield
                float x = Random.Range(minX + 1f, maxX - 1f);
                float y = Random.Range(GameManager.Instance.PlayfieldMaxY - 2f, GameManager.Instance.SpawnY - 1f);
                Vector3 spawnPos = new Vector3(x, y, 0f);

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

            // Combine spawn progress and kill progress
            float spawnProgress = (float)zombiesSpawnedThisWave / targetZombiesThisWave;
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
