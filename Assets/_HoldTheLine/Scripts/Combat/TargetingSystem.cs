// TargetingSystem.cs - Manages target priority switching (Tap-to-toggle model)
// Location: Assets/_HoldTheLine/Scripts/Combat/
// Attach to: Player prefab or GameManager object

using System.Collections.Generic;
using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Manages weapon targeting priority using Tap-to-Toggle model.
    ///
    /// Interaction Model B: Tap-to-toggle priority
    /// - Tap on an UpgradeTarget to focus fire on it
    /// - Fire returns to zombies when:
    ///   1. Target is destroyed
    ///   2. Player taps elsewhere
    ///   3. Timeout occurs (optional)
    /// </summary>
    public class TargetingSystem : MonoBehaviour
    {
        public static TargetingSystem Instance { get; private set; }

        [Header("Targeting Settings")]
        [SerializeField] private float targetingTimeout = 10f; // Auto-return to zombies after this time
        [SerializeField] private float tapRadius = 1f; // How close tap needs to be to select target
        [SerializeField] private LayerMask upgradeTargetLayer;
        [SerializeField] private LayerMask zombieLayer;

        [Header("Visual Feedback")]
        [SerializeField] private bool showTargetIndicator = true;
        [SerializeField] private Color targetingUpgradeColor = Color.yellow;
        [SerializeField] private Color targetingZombiesColor = Color.red;

        // Current state
        private TargetPriority currentPriority = TargetPriority.Zombies;
        private UpgradeTarget currentUpgradeTarget;
        private float targetingTimer;

        // Registered targets for quick lookup
        private List<UpgradeTarget> activeUpgradeTargets = new List<UpgradeTarget>();
        private List<ZombieUnit> activeZombies = new List<ZombieUnit>();

        // Cached
        private Camera mainCamera;
        private Vector2 lastTapPosition;
        private bool wasTapping;

        // Events
        public System.Action<TargetPriority> OnPriorityChanged;
        public System.Action<UpgradeTarget> OnUpgradeTargetSelected;

        public TargetPriority CurrentPriority => currentPriority;
        public UpgradeTarget CurrentUpgradeTarget => currentUpgradeTarget;
        public bool IsTargetingUpgrade => currentPriority == TargetPriority.UpgradeTarget && currentUpgradeTarget != null;

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
            mainCamera = Camera.main;
            SetPriority(TargetPriority.Zombies);
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsGameActive())
            {
                return;
            }

            HandleTapInput();
            UpdateTargetingState();
        }

        private void HandleTapInput()
        {
            bool isTapping = false;
            Vector2 tapPosition = Vector2.zero;

#if UNITY_EDITOR
            // Mouse input for editor
            if (Input.GetMouseButtonDown(0))
            {
                isTapping = true;
                tapPosition = Input.mousePosition;
            }
#else
            // Touch input for mobile
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    isTapping = true;
                    tapPosition = touch.position;
                }
            }
#endif

            if (isTapping && !wasTapping)
            {
                ProcessTap(tapPosition);
            }

            wasTapping = isTapping;
        }

        private void ProcessTap(Vector2 screenPosition)
        {
            // Don't process taps that are clearly for movement (lower third of screen)
            // This prevents accidental target switching while moving
            if (screenPosition.y < Screen.height * 0.35f)
            {
                return;
            }

            // Convert to world position
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
            worldPos.z = 0f;

            // Check if tapping on an upgrade target
            UpgradeTarget tappedTarget = FindNearestUpgradeTarget(worldPos);

            if (tappedTarget != null)
            {
                // Select this upgrade target
                SelectUpgradeTarget(tappedTarget);
            }
            else if (currentPriority == TargetPriority.UpgradeTarget)
            {
                // Tapped elsewhere while targeting upgrade - return to zombies
                ClearUpgradeTarget();
            }
        }

        private UpgradeTarget FindNearestUpgradeTarget(Vector3 worldPosition)
        {
            UpgradeTarget nearest = null;
            float nearestDistance = tapRadius;

            foreach (UpgradeTarget target in activeUpgradeTargets)
            {
                if (target == null || !target.IsAlive) continue;

                float distance = Vector2.Distance(
                    new Vector2(worldPosition.x, worldPosition.y),
                    new Vector2(target.transform.position.x, target.transform.position.y)
                );

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = target;
                }
            }

            return nearest;
        }

        private void UpdateTargetingState()
        {
            // If targeting upgrade, check validity and timeout
            if (currentPriority == TargetPriority.UpgradeTarget)
            {
                // Check if target is still valid
                if (currentUpgradeTarget == null || !currentUpgradeTarget.IsAlive)
                {
                    ClearUpgradeTarget();
                    return;
                }

                // Check timeout
                targetingTimer -= Time.deltaTime;
                if (targetingTimer <= 0f)
                {
                    ClearUpgradeTarget();
                }
            }
        }

        /// <summary>
        /// Select an upgrade target to focus fire on
        /// </summary>
        public void SelectUpgradeTarget(UpgradeTarget target)
        {
            if (target == null || !target.IsAlive) return;

            currentUpgradeTarget = target;
            targetingTimer = targetingTimeout;
            SetPriority(TargetPriority.UpgradeTarget);

            OnUpgradeTargetSelected?.Invoke(target);
            Debug.Log($"[TargetingSystem] Now targeting: {target.name}");
        }

        /// <summary>
        /// Clear upgrade target and return to zombie targeting
        /// </summary>
        public void ClearUpgradeTarget()
        {
            currentUpgradeTarget = null;
            SetPriority(TargetPriority.Zombies);
        }

        private void SetPriority(TargetPriority priority)
        {
            if (currentPriority == priority) return;

            currentPriority = priority;
            OnPriorityChanged?.Invoke(priority);
            Debug.Log($"[TargetingSystem] Priority changed to: {priority}");
        }

        /// <summary>
        /// Get the direction bullets should travel based on current targeting
        /// </summary>
        public Vector3 GetTargetDirection(Vector3 fromPosition)
        {
            if (currentPriority == TargetPriority.UpgradeTarget && currentUpgradeTarget != null)
            {
                // Aim at upgrade target
                Vector3 targetPos = currentUpgradeTarget.transform.position;
                return (targetPos - fromPosition).normalized;
            }

            // Default: aim at nearest zombie or straight up
            ZombieUnit nearestZombie = FindNearestZombie(fromPosition);
            if (nearestZombie != null)
            {
                Vector3 targetPos = nearestZombie.transform.position;
                return (targetPos - fromPosition).normalized;
            }

            // No targets - shoot straight up
            return Vector3.up;
        }

        private ZombieUnit FindNearestZombie(Vector3 fromPosition)
        {
            ZombieUnit nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (ZombieUnit zombie in activeZombies)
            {
                if (zombie == null || !zombie.IsAlive) continue;

                float distance = Vector3.Distance(fromPosition, zombie.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = zombie;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Register an upgrade target (called by UpgradeTarget on enable)
        /// </summary>
        public void RegisterUpgradeTarget(UpgradeTarget target)
        {
            if (!activeUpgradeTargets.Contains(target))
            {
                activeUpgradeTargets.Add(target);
            }
        }

        /// <summary>
        /// Unregister an upgrade target (called by UpgradeTarget on disable/destroy)
        /// </summary>
        public void UnregisterUpgradeTarget(UpgradeTarget target)
        {
            activeUpgradeTargets.Remove(target);

            // If this was our current target, clear it
            if (currentUpgradeTarget == target)
            {
                ClearUpgradeTarget();
            }
        }

        /// <summary>
        /// Register a zombie (called by ZombieUnit on enable)
        /// </summary>
        public void RegisterZombie(ZombieUnit zombie)
        {
            if (!activeZombies.Contains(zombie))
            {
                activeZombies.Add(zombie);
            }
        }

        /// <summary>
        /// Unregister a zombie (called by ZombieUnit on disable/destroy)
        /// </summary>
        public void UnregisterZombie(ZombieUnit zombie)
        {
            activeZombies.Remove(zombie);
        }

        /// <summary>
        /// Get count of active zombies (for UI/wave tracking)
        /// </summary>
        public int GetActiveZombieCount()
        {
            // Clean up null references
            activeZombies.RemoveAll(z => z == null || !z.IsAlive);
            return activeZombies.Count;
        }

        /// <summary>
        /// Get count of active upgrade targets
        /// </summary>
        public int GetActiveUpgradeTargetCount()
        {
            activeUpgradeTargets.RemoveAll(t => t == null || !t.IsAlive);
            return activeUpgradeTargets.Count;
        }
    }
}
