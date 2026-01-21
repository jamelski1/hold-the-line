using UnityEngine;
using System.Collections.Generic;

namespace HoldTheLine.Managers
{
    [System.Serializable]
    public class PoolItem
    {
        public string tag;
        public GameObject prefab;
        public int poolSize = 10;
    }

    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        [SerializeField] private List<PoolItem> poolItems = new List<PoolItem>();

        private Dictionary<string, Queue<GameObject>> poolDictionary;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            poolDictionary = new Dictionary<string, Queue<GameObject>>();
            InitializePools();
        }

        private void InitializePools()
        {
            foreach (PoolItem item in poolItems)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();

                for (int i = 0; i < item.poolSize; i++)
                {
                    GameObject obj = Instantiate(item.prefab, transform);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                poolDictionary[item.tag] = objectPool;
            }
        }

        public GameObject GetPooledObject(string tag)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
                return null;
            }

            Queue<GameObject> pool = poolDictionary[tag];

            // Find an inactive object
            foreach (GameObject obj in pool)
            {
                if (!obj.activeInHierarchy)
                {
                    return obj;
                }
            }

            // If no inactive object found, expand the pool
            PoolItem poolItem = poolItems.Find(x => x.tag == tag);
            if (poolItem != null)
            {
                GameObject newObj = Instantiate(poolItem.prefab, transform);
                newObj.SetActive(false);
                pool.Enqueue(newObj);
                return newObj;
            }

            return null;
        }

        public void ReturnToPool(string tag, GameObject obj)
        {
            obj.SetActive(false);
        }

        public void AddPool(string tag, GameObject prefab, int size)
        {
            if (poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} already exists.");
                return;
            }

            PoolItem newItem = new PoolItem
            {
                tag = tag,
                prefab = prefab,
                poolSize = size
            };
            poolItems.Add(newItem);

            Queue<GameObject> objectPool = new Queue<GameObject>();
            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }
            poolDictionary[tag] = objectPool;
        }
    }
}
