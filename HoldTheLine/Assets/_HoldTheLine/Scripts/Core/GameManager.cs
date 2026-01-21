// GameManager.cs - Central game state and flow controller (3D Version)
// Location: Assets/_HoldTheLine/Scripts/Core/
// Attach to: Empty GameObject named "GameManager" in scene

using System;
using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Controls game state, wave progression, and coordinates all systems.
    ///
    /// 3D AXIS MAPPING (Top-Down View):
    /// - X axis = horizontal (player left/right movement)
    /// - Z axis = depth (down the screen - zombies move in -Z direction)
    /// - Y axis = height (minimal use)
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private int startingWave = 1;
        [SerializeField] private float waveTransitionDuration = 2f;
        [SerializeField] private float upgradeObtainedPauseDuration = 1f;

        [Header("Playfield Bounds (World Units - 3D)")]
        [SerializeField] private float playfieldMinX = -4f;
        [SerializeField] private float playfieldMaxX = 4f;
        [SerializeField] private float playfieldMinZ = -10f;  // Bottom of screen (player side)
        [SerializeField] private float playfieldMaxZ = 10f;   // Top of screen (spawn side)
        [SerializeField] private float playerZ = -8f;         // Player position on Z axis
        [SerializeField] private float spawnZ = 12f;          // Where zombies spawn
        [SerializeField] private float despawnZ = -10f;       // Where zombies despawn/damage player

        // Public properties for playfield bounds
        public float PlayfieldMinX => playfieldMinX;
        public float PlayfieldMaxX => playfieldMaxX;
        public float PlayfieldMinZ => playfieldMinZ;
        public float PlayfieldMaxZ => playfieldMaxZ;
        public float PlayerZ => playerZ;
        public float SpawnZ => spawnZ;
        public float DespawnZ => despawnZ;

        // Legacy Y properties mapped to Z for compatibility
        public float PlayfieldMinY => playfieldMinZ;
        public float PlayfieldMaxY => playfieldMaxZ;
        public float PlayerY => playerZ;
        public float SpawnY => spawnZ;
        public float DespawnY => despawnZ;

        // Game state
        public GameState CurrentState { get; private set; } = GameState.Menu;
        public int CurrentWave { get; private set; }
        public int ZombiesKilledThisWave { get; private set; }
        public int TotalZombiesKilled { get; private set; }
        public float GameTime { get; private set; }

        // Events for decoupled communication
        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;
        public event Action OnPlayerDied;
        public event Action<WeaponTier> OnWeaponUpgraded;
        public event Action<int> OnZombieKilled;

        private float stateTimer;
        private bool isTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Set target framerate for mobile
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Start()
        {
            SetState(GameState.Menu);
        }

        private void Update()
        {
            if (CurrentState == GameState.Playing)
            {
                GameTime += Time.deltaTime;
            }

            // Handle timed state transitions
            if (isTransitioning)
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    CompleteTransition();
                }
            }
        }

        /// <summary>
        /// Start the game from menu
        /// </summary>
        public void StartGame()
        {
            CurrentWave = startingWave;
            TotalZombiesKilled = 0;
            GameTime = 0f;

            SetState(GameState.Playing);
            OnWaveStarted?.Invoke(CurrentWave);
        }

        /// <summary>
        /// Restart after fail
        /// </summary>
        public void RestartGame()
        {
            // Return all pooled objects
            ObjectPool.Instance?.ReturnAllActive(PoolType.Zombie);
            ObjectPool.Instance?.ReturnAllActive(PoolType.Bullet);
            ObjectPool.Instance?.ReturnAllActive(PoolType.Pickup);
            ObjectPool.Instance?.ReturnAllActive(PoolType.UpgradeTarget);

            StartGame();
        }

        /// <summary>
        /// Return to main menu
        /// </summary>
        public void ReturnToMenu()
        {
            ObjectPool.Instance?.ReturnAllActive(PoolType.Zombie);
            ObjectPool.Instance?.ReturnAllActive(PoolType.Bullet);
            ObjectPool.Instance?.ReturnAllActive(PoolType.Pickup);
            ObjectPool.Instance?.ReturnAllActive(PoolType.UpgradeTarget);

            SetState(GameState.Menu);
        }

        /// <summary>
        /// Called when a wave is completed
        /// </summary>
        public void CompleteWave()
        {
            if (CurrentState != GameState.Playing) return;

            OnWaveCompleted?.Invoke(CurrentWave);
            ZombiesKilledThisWave = 0;

            SetState(GameState.WaveTransition);
            isTransitioning = true;
            stateTimer = waveTransitionDuration;
        }

        /// <summary>
        /// Called when player dies
        /// </summary>
        public void PlayerDied()
        {
            if (CurrentState == GameState.Fail) return;

            OnPlayerDied?.Invoke();
            SetState(GameState.Fail);
        }

        /// <summary>
        /// Called when weapon is upgraded
        /// </summary>
        public void WeaponUpgraded(WeaponTier newTier)
        {
            OnWeaponUpgraded?.Invoke(newTier);

            // Brief pause to celebrate upgrade
            SetState(GameState.UpgradeObtained);
            isTransitioning = true;
            stateTimer = upgradeObtainedPauseDuration;
        }

        /// <summary>
        /// Register a zombie kill
        /// </summary>
        public void RegisterZombieKill()
        {
            ZombiesKilledThisWave++;
            TotalZombiesKilled++;
            OnZombieKilled?.Invoke(TotalZombiesKilled);
        }

        private void SetState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            OnGameStateChanged?.Invoke(newState);

            Debug.Log($"[GameManager] State changed to: {newState}");
        }

        private void CompleteTransition()
        {
            isTransitioning = false;

            switch (CurrentState)
            {
                case GameState.WaveTransition:
                    CurrentWave++;
                    SetState(GameState.Playing);
                    OnWaveStarted?.Invoke(CurrentWave);
                    break;

                case GameState.UpgradeObtained:
                    SetState(GameState.Playing);
                    break;
            }
        }

        /// <summary>
        /// Check if game is in an active gameplay state
        /// </summary>
        public bool IsGameActive()
        {
            return CurrentState == GameState.Playing ||
                   CurrentState == GameState.WaveTransition ||
                   CurrentState == GameState.UpgradeObtained;
        }

#if UNITY_EDITOR
        // Visualize playfield bounds in editor (3D top-down)
        private void OnDrawGizmos()
        {
            // Draw playfield bounds on XZ plane
            Gizmos.color = Color.green;
            Vector3 center = new Vector3((playfieldMinX + playfieldMaxX) / 2f, 0.1f, (playfieldMinZ + playfieldMaxZ) / 2f);
            Vector3 size = new Vector3(playfieldMaxX - playfieldMinX, 0.1f, playfieldMaxZ - playfieldMinZ);
            Gizmos.DrawWireCube(center, size);

            // Draw spawn line (where zombies spawn)
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(playfieldMinX, 0.1f, spawnZ), new Vector3(playfieldMaxX, 0.1f, spawnZ));

            // Draw despawn line (player defense line)
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(new Vector3(playfieldMinX, 0.1f, despawnZ), new Vector3(playfieldMaxX, 0.1f, despawnZ));

            // Draw player line
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(playfieldMinX, 0.1f, playerZ), new Vector3(playfieldMaxX, 0.1f, playerZ));
        }
#endif
    }
}
