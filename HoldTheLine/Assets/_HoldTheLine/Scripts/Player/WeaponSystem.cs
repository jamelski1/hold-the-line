using UnityEngine;
using HoldTheLine.Weapons;
using HoldTheLine.Managers;

namespace HoldTheLine.Player
{
    public class WeaponSystem : MonoBehaviour
    {
        [Header("Weapon Settings")]
        [SerializeField] private float fireRate = 0.3f;
        [SerializeField] private Transform firePoint;

        [Header("References")]
        [SerializeField] private GameObject bulletPrefab;

        private float nextFireTime;
        private TargetingSystem targetingSystem;
        private ObjectPool objectPool;

        private void Start()
        {
            targetingSystem = GetComponent<TargetingSystem>();
            objectPool = FindObjectOfType<ObjectPool>();

            if (firePoint == null)
            {
                // Create a default fire point above the player
                GameObject fp = new GameObject("FirePoint");
                fp.transform.SetParent(transform);
                fp.transform.localPosition = new Vector3(0, 0.5f, 0);
                firePoint = fp.transform;
            }
        }

        private void Update()
        {
            // Auto-fire
            if (Time.time >= nextFireTime)
            {
                Fire();
                nextFireTime = Time.time + fireRate;
            }
        }

        private void Fire()
        {
            if (bulletPrefab == null)
            {
                Debug.LogWarning("WeaponSystem: No bullet prefab assigned!");
                return;
            }

            Vector2 direction = Vector2.up;
            if (targetingSystem != null)
            {
                direction = targetingSystem.GetTargetDirection();
            }

            GameObject bullet;
            if (objectPool != null)
            {
                bullet = objectPool.GetPooledObject("Bullet");
                if (bullet != null)
                {
                    bullet.transform.position = firePoint.position;
                    bullet.transform.rotation = Quaternion.identity;
                    bullet.SetActive(true);
                }
                else
                {
                    bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
                }
            }
            else
            {
                bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            }

            if (bullet != null)
            {
                Bullet bulletComponent = bullet.GetComponent<Bullet>();
                if (bulletComponent != null)
                {
                    bulletComponent.Initialize(direction);
                }
            }
        }

        public void SetBulletPrefab(GameObject prefab)
        {
            bulletPrefab = prefab;
        }

        public void SetFirePoint(Transform point)
        {
            firePoint = point;
        }
    }
}
