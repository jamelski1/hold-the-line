// UIHUD.cs - Main HUD display for game information
// Location: Assets/_HoldTheLine/Scripts/UI/
// Attach to: Canvas object with HUD elements

using UnityEngine;
using UnityEngine.UI;

namespace HoldTheLine
{
    /// <summary>
    /// Manages all HUD elements: health, wave, weapon tier, targeting indicator.
    /// Updates UI reactively based on game events.
    /// </summary>
    public class UIHUD : MonoBehaviour
    {
        [Header("Health Display")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Text healthText;
        [SerializeField] private Color healthFullColor = Color.green;
        [SerializeField] private Color healthLowColor = Color.red;

        [Header("Wave Display")]
        [SerializeField] private Text waveText;
        [SerializeField] private Text waveProgressText;
        [SerializeField] private Slider waveProgressSlider;

        [Header("Weapon Display")]
        [SerializeField] private Text weaponTierText;
        [SerializeField] private Text damageMultiplierText;
        [SerializeField] private Image weaponIcon;

        [Header("Targeting Indicator")]
        [SerializeField] private GameObject targetingIndicator;
        [SerializeField] private Text targetingText;
        [SerializeField] private Slider upgradeProgressSlider;

        [Header("Score/Stats")]
        [SerializeField] private Text killCountText;
        [SerializeField] private Text gameTimeText;

        [Header("Menu Panel")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private Text titleText;

        [Header("Fail Panel")]
        [SerializeField] private GameObject failPanel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Text finalWaveText;

        [Header("Wave Transition")]
        [SerializeField] private GameObject waveTransitionPanel;
        [SerializeField] private Text waveTransitionText;

        [Header("Upgrade Obtained")]
        [SerializeField] private GameObject upgradeObtainedPanel;
        [SerializeField] private Text upgradeObtainedText;

        // Cached references
        private UpgradeTarget currentTrackedTarget;

        private void Start()
        {
            SetupButtons();
            SubscribeToEvents();
            RefreshUI();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SetupButtons()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartClicked);
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (menuButton != null)
            {
                menuButton.onClick.AddListener(OnMenuClicked);
            }
        }

        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                GameManager.Instance.OnWaveStarted += HandleWaveStarted;
                GameManager.Instance.OnWaveCompleted += HandleWaveCompleted;
                GameManager.Instance.OnWeaponUpgraded += HandleWeaponUpgraded;
                GameManager.Instance.OnZombieKilled += HandleZombieKilled;
            }

            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.OnHealthChanged += HandleHealthChanged;
            }

            if (TargetingSystem.Instance != null)
            {
                TargetingSystem.Instance.OnPriorityChanged += HandlePriorityChanged;
                TargetingSystem.Instance.OnUpgradeTargetSelected += HandleUpgradeTargetSelected;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                GameManager.Instance.OnWaveStarted -= HandleWaveStarted;
                GameManager.Instance.OnWaveCompleted -= HandleWaveCompleted;
                GameManager.Instance.OnWeaponUpgraded -= HandleWeaponUpgraded;
                GameManager.Instance.OnZombieKilled -= HandleZombieKilled;
            }

            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.OnHealthChanged -= HandleHealthChanged;
            }

            if (TargetingSystem.Instance != null)
            {
                TargetingSystem.Instance.OnPriorityChanged -= HandlePriorityChanged;
                TargetingSystem.Instance.OnUpgradeTargetSelected -= HandleUpgradeTargetSelected;
            }
        }

        private void Update()
        {
            UpdateDynamicElements();
        }

        private void UpdateDynamicElements()
        {
            // Update game time
            if (gameTimeText != null && GameManager.Instance != null)
            {
                float time = GameManager.Instance.GameTime;
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                gameTimeText.text = $"{minutes:00}:{seconds:00}";
            }

            // Update wave progress
            if (waveProgressSlider != null && SpawnerManager.Instance != null)
            {
                waveProgressSlider.value = SpawnerManager.Instance.GetWaveProgress();
            }

            if (waveProgressText != null && SpawnerManager.Instance != null)
            {
                int remaining = SpawnerManager.Instance.GetRemainingZombies();
                waveProgressText.text = $"{remaining} remaining";
            }

            // Update upgrade target progress if targeting one
            UpdateUpgradeTargetProgress();
        }

        private void UpdateUpgradeTargetProgress()
        {
            if (TargetingSystem.Instance == null) return;

            UpgradeTarget target = TargetingSystem.Instance.CurrentUpgradeTarget;

            if (target != null && target.IsAlive)
            {
                if (upgradeProgressSlider != null)
                {
                    upgradeProgressSlider.gameObject.SetActive(true);
                    upgradeProgressSlider.value = 1f - target.GetHealthNormalized();
                }
            }
            else
            {
                if (upgradeProgressSlider != null)
                {
                    upgradeProgressSlider.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshUI()
        {
            // Refresh all UI elements based on current state
            if (PlayerHealth.Instance != null)
            {
                HandleHealthChanged(PlayerHealth.Instance.CurrentHealth, PlayerHealth.Instance.MaxHealth);
            }

            if (WeaponSystem.Instance != null)
            {
                UpdateWeaponDisplay();
            }

            if (GameManager.Instance != null)
            {
                HandleGameStateChanged(GameManager.Instance.CurrentState);
            }
        }

        #region Event Handlers

        private void HandleGameStateChanged(GameState newState)
        {
            // Hide all panels first
            SetPanelActive(menuPanel, false);
            SetPanelActive(failPanel, false);
            SetPanelActive(waveTransitionPanel, false);
            SetPanelActive(upgradeObtainedPanel, false);

            switch (newState)
            {
                case GameState.Menu:
                    SetPanelActive(menuPanel, true);
                    break;

                case GameState.Playing:
                    // HUD visible, panels hidden
                    break;

                case GameState.WaveTransition:
                    SetPanelActive(waveTransitionPanel, true);
                    if (waveTransitionText != null)
                    {
                        waveTransitionText.text = $"WAVE {GameManager.Instance.CurrentWave} COMPLETE!";
                    }
                    break;

                case GameState.UpgradeObtained:
                    SetPanelActive(upgradeObtainedPanel, true);
                    if (upgradeObtainedText != null && WeaponSystem.Instance != null)
                    {
                        upgradeObtainedText.text = $"UPGRADED!\n{WeaponSystem.Instance.GetCurrentTierName()}";
                    }
                    break;

                case GameState.Fail:
                    SetPanelActive(failPanel, true);
                    if (finalScoreText != null)
                    {
                        finalScoreText.text = $"Kills: {GameManager.Instance.TotalZombiesKilled}";
                    }
                    if (finalWaveText != null)
                    {
                        finalWaveText.text = $"Wave: {GameManager.Instance.CurrentWave}";
                    }
                    break;
            }
        }

        private void HandleWaveStarted(int wave)
        {
            if (waveText != null)
            {
                waveText.text = $"WAVE {wave}";
            }
        }

        private void HandleWaveCompleted(int wave)
        {
            // Handled by state change
        }

        private void HandleWeaponUpgraded(WeaponTier newTier)
        {
            UpdateWeaponDisplay();
        }

        private void HandleZombieKilled(int totalKills)
        {
            if (killCountText != null)
            {
                killCountText.text = totalKills.ToString();
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            float percent = current / max;

            if (healthSlider != null)
            {
                healthSlider.value = percent;
            }

            if (healthFillImage != null)
            {
                healthFillImage.color = Color.Lerp(healthLowColor, healthFullColor, percent);
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
            }
        }

        private void HandlePriorityChanged(TargetPriority priority)
        {
            if (targetingIndicator != null)
            {
                targetingIndicator.SetActive(priority == TargetPriority.UpgradeTarget);
            }

            if (targetingText != null)
            {
                targetingText.text = priority == TargetPriority.UpgradeTarget
                    ? "TARGETING UPGRADE"
                    : "TARGETING ZOMBIES";
            }
        }

        private void HandleUpgradeTargetSelected(UpgradeTarget target)
        {
            currentTrackedTarget = target;
        }

        #endregion

        #region Button Handlers

        private void OnStartClicked()
        {
            GameManager.Instance?.StartGame();
        }

        private void OnRetryClicked()
        {
            GameManager.Instance?.RestartGame();
        }

        private void OnMenuClicked()
        {
            GameManager.Instance?.ReturnToMenu();
        }

        #endregion

        private void UpdateWeaponDisplay()
        {
            if (WeaponSystem.Instance == null) return;

            if (weaponTierText != null)
            {
                weaponTierText.text = WeaponSystem.Instance.GetCurrentTierName();
            }

            if (damageMultiplierText != null)
            {
                float multiplier = WeaponSystem.Instance.DamageMultiplier;
                if (multiplier > 1f)
                {
                    damageMultiplierText.text = $"x{multiplier:F0} DMG";
                    damageMultiplierText.gameObject.SetActive(true);
                }
                else
                {
                    damageMultiplierText.gameObject.SetActive(false);
                }
            }
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
    }
}
