// ObjectPool.cs - Generic object pooling system for performance
// Location: Assets/_HoldTheLine/Scripts/Core/
// Attach to: Empty GameObject named "ObjectPoolManager" in scene

using System.Collections.Generic;
using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Generic object pool to avoid runtime allocations.
    /// Manages bullets, zombies, pickups, and upgrade targets.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private GameObject pickupPrefab;
        [SerializeField] private GameObject upgradeTargetPrefab;

        [Header("Initial Pool Sizes")]
        [SerializeField] private int initialBullets = 100;
        [SerializeField] private int initialZombies = 30;
        [SerializeField] private int initialPickups = 10;
        [SerializeField] private int initialUpgradeTargets = 5;

        // Pool containers - using Stack for O(1) push/pop
        private Stack<GameObject> bulletPool;
        private Stack<GameObject> zombiePool;
        private Stack<GameObject> pickupPool;
        private Stack<GameObject> upgradeTargetPool;

        // Parent transforms for organization
        private Transform bulletParent;
        private Transform zombieParent;
        private Transform pickupParent;
        private Transform upgradeTargetParent;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePools();
        }

        private void InitializePools()
        {
            // Create parent objects for hierarchy organization
            bulletParent = new GameObject("BulletPool").transform;
            bulletParent.SetParent(transform);

            zombieParent = new GameObject("ZombiePool").transform;
            zombieParent.SetParent(transform);

            pickupParent = new GameObject("PickupPool").transform;
            pickupParent.SetParent(transform);

            upgradeTargetParent = new GameObject("UpgradeTargetPool").transform;
            upgradeTargetParent.SetParent(transform);

            // Initialize stacks
            bulletPool = new Stack<GameObject>(initialBullets);
            zombiePool = new Stack<GameObject>(initialZombies);
            pickupPool = new Stack<GameObject>(initialPickups);
            upgradeTargetPool = new Stack<GameObject>(initialUpgradeTargets);

            // Pre-warm pools
            PreWarmPool(bulletPrefab, bulletPool, bulletParent, initialBullets);
            PreWarmPool(zombiePrefab, zombiePool, zombieParent, initialZombies);
            PreWarmPool(pickupPrefab, pickupPool, pickupParent, initialPickups);
            PreWarmPool(upgradeTargetPrefab, upgradeTargetPool, upgradeTargetParent, initialUpgradeTargets);
        }

        private void PreWarmPool(GameObject prefab, Stack<GameObject> pool, Transform parent, int count)
        {
            if (prefab == null) return;

            for (int i = 0; i < count; i++)
            {
                GameObject obj = Instantiate(prefab, parent);
                obj.SetActive(false);
                pool.Push(obj);
            }
        }

        /// <summary>
        /// Get an object from the specified pool
        /// </summary>
        public GameObject Get(PoolType type)
        {
            Stack<GameObject> pool = GetPool(type);
            Transform parent = GetParent(type);
            GameObject prefab = GetPrefab(type);

            GameObject obj;
            if (pool.Count > 0)
            {
                obj = pool.Pop();
            }
            else
            {
                // Pool exhausted, create new instance
                obj = Instantiate(prefab, parent);
            }

            obj.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Return an object to its pool
        /// </summary>
        public void Return(PoolType type, GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(GetParent(type));
            GetPool(type).Push(obj);
        }

        /// <summary>
        /// Return all active objects of a type to pool (useful for wave reset)
        /// </summary>
        public void ReturnAllActive(PoolType type)
        {
            Transform parent = GetParent(type);
            Stack<GameObject> pool = GetPool(type);

            // Iterate through all children and return active ones
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                    pool.Push(child.gameObject);
                }
            }
        }

        private Stack<GameObject> GetPool(PoolType type)
        {
            return type switch
            {
                PoolType.Bullet => bulletPool,
                PoolType.Zombie => zombiePool,
                PoolType.Pickup => pickupPool,
                PoolType.UpgradeTarget => upgradeTargetPool,
                _ => bulletPool
            };
        }

        private Transform GetParent(PoolType type)
        {
            return type switch
            {
                PoolType.Bullet => bulletParent,
                PoolType.Zombie => zombieParent,
                PoolType.Pickup => pickupParent,
                PoolType.UpgradeTarget => upgradeTargetParent,
                _ => bulletParent
            };
        }

        private GameObject GetPrefab(PoolType type)
        {
            return type switch
            {
                PoolType.Bullet => bulletPrefab,
                PoolType.Zombie => zombiePrefab,
                PoolType.Pickup => pickupPrefab,
                PoolType.UpgradeTarget => upgradeTargetPrefab,
                _ => bulletPrefab
            };
        }
    }
}
