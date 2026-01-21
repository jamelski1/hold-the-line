using UnityEngine;
using System.Collections;

namespace HoldTheLine.Managers
{
    public class SpawnerManager : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private float spawnIntervalDecrease = 0.1f;
        [SerializeField] private float minimumSpawnInterval = 0.5f;

        [Header("Spawn Area")]
        [SerializeField] private int numberOfLanes = 5;
        [SerializeField] private float laneWidth = 1f;

        private Camera mainCamera;
        private float spawnY;
        private float[] lanePositions;
        private ObjectPool objectPool;
        private float currentSpawnInterval;

        private void Start()
        {
            mainCamera = Camera.main;
            objectPool = FindObjectOfType<ObjectPool>();

            SetupSpawnArea();
            currentSpawnInterval = spawnInterval;

            StartCoroutine(SpawnRoutine());
        }

        private void SetupSpawnArea()
        {
            if (mainCamera == null) return;

            float halfHeight = mainCamera.orthographicSize;
            float halfWidth = halfHeight * mainCamera.aspect;

            spawnY = halfHeight + 1f;

            // Calculate lane positions
            lanePositions = new float[numberOfLanes];
            float totalWidth = (numberOfLanes - 1) * laneWidth;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < numberOfLanes; i++)
            {
                lanePositions[i] = startX + (i * laneWidth);
            }
        }

        private IEnumerator SpawnRoutine()
        {
            yield return new WaitForSeconds(1f); // Initial delay

            while (true)
            {
                if (GameManager.Instance == null || !GameManager.Instance.IsGameOver)
                {
                    SpawnZombie();
                }

                yield return new WaitForSeconds(currentSpawnInterval);

                // Gradually increase difficulty
                if (currentSpawnInterval > minimumSpawnInterval)
                {
                    currentSpawnInterval -= spawnIntervalDecrease * Time.deltaTime;
                    currentSpawnInterval = Mathf.Max(currentSpawnInterval, minimumSpawnInterval);
                }
            }
        }

        private void SpawnZombie()
        {
            if (zombiePrefab == null)
            {
                Debug.LogWarning("SpawnerManager: No zombie prefab assigned!");
                return;
            }

            int laneIndex = Random.Range(0, numberOfLanes);
            Vector3 spawnPosition = new Vector3(lanePositions[laneIndex], spawnY, 0);

            GameObject zombie;
            if (objectPool != null)
            {
                zombie = objectPool.GetPooledObject("Zombie");
                if (zombie != null)
                {
                    zombie.transform.position = spawnPosition;
                    zombie.SetActive(true);
                }
                else
                {
                    zombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
                }
            }
            else
            {
                zombie = Instantiate(zombiePrefab, spawnPosition, Quaternion.identity);
            }
        }

        public void SetZombiePrefab(GameObject prefab)
        {
            zombiePrefab = prefab;
        }
    }
}
