// GameManager.cs - Central game state and flow controller
// Location: Assets/_HoldTheLine/Scripts/Core/
// Attach to: Empty GameObject named "GameManager" in scene

using System;
using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Controls game state, wave progression, and coordinates all systems.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] private int startingWave = 1;
        [SerializeField] private float waveTransitionDuration = 2f;
        [SerializeField] private float upgradeObtainedPauseDuration = 1f;

        [Header("Playfield Bounds (World Units)")]
        [SerializeField] private float playfieldMinX = -2.5f;
        [SerializeField] private float playfieldMaxX = 2.5f;
        [SerializeField] private float playfieldMinY = -5f;
        [SerializeField] private float playfieldMaxY = 5f;
        [SerializeField] private float playerY = -4f;
        [SerializeField] private float spawnY = 6f;
        [SerializeField] private float despawnY = -6f;

        // Public properties for playfield bounds
        public float PlayfieldMinX => playfieldMinX;
        public float PlayfieldMaxX => playfieldMaxX;
        public float PlayfieldMinY => playfieldMinY;
        public float PlayfieldMaxY => playfieldMaxY;
        public float PlayerY => playerY;
        public float SpawnY => spawnY;
        public float DespawnY => despawnY;

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
        // Visualize playfield bounds in editor
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Vector3 center = new Vector3((playfieldMinX + playfieldMaxX) / 2f, (playfieldMinY + playfieldMaxY) / 2f, 0);
            Vector3 size = new Vector3(playfieldMaxX - playfieldMinX, playfieldMaxY - playfieldMinY, 0.1f);
            Gizmos.DrawWireCube(center, size);

            // Draw spawn line
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(playfieldMinX, spawnY, 0), new Vector3(playfieldMaxX, spawnY, 0));

            // Draw despawn line
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(new Vector3(playfieldMinX, despawnY, 0), new Vector3(playfieldMaxX, despawnY, 0));

            // Draw player line
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(playfieldMinX, playerY, 0), new Vector3(playfieldMaxX, playerY, 0));
        }
#endif
    }
}
