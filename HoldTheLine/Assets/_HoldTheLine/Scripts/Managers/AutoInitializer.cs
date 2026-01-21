using UnityEngine;
using HoldTheLine.Player;
using HoldTheLine.Enemy;
using HoldTheLine.Weapons;

namespace HoldTheLine.Managers
{
    /// <summary>
    /// Auto-initializes the game on startup using RuntimeInitializeOnLoadMethod.
    /// No need to add anything to the scene - this runs automatically.
    /// </summary>
    public static class AutoInitializer
    {
        private static Sprite whiteSquare;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            // Check if game is already set up
            if (Object.FindObjectOfType<GameManager>() != null)
            {
                Debug.Log("AutoInitializer: Game already set up, skipping.");
                return;
            }

            Debug.Log("AutoInitializer: Setting up game...");
            SetupGame();
        }

        private static void SetupGame()
        {
            // Create white square sprite for visuals
            whiteSquare = CreateRuntimeSprite();

            // Configure camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 8f;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.transform.position = new Vector3(0, 0, -10);
            }

            // Create prefabs (in-memory templates)
            GameObject bulletPrefab = CreateBulletPrefab();
            GameObject zombiePrefab = CreateZombiePrefab();

            // Setup managers
            SetupManagers(bulletPrefab, zombiePrefab);

            // Create player
            CreatePlayer(bulletPrefab);

            Debug.Log("AutoInitializer: Game setup complete! Use A/D or Left/Right arrows to move.");
        }

        private static Sprite CreateRuntimeSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] colors = new Color[16];
            for (int i = 0; i < 16; i++) colors[i] = Color.white;
            tex.SetPixels(colors);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }

        private static GameObject CreateBulletPrefab()
        {
            GameObject bullet = new GameObject("BulletTemplate");
            bullet.SetActive(false);

            SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSquare;
            sr.color = Color.yellow;
            sr.sortingOrder = 5;
            bullet.transform.localScale = new Vector3(0.2f, 0.4f, 1f);

            BoxCollider2D col = bullet.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.isKinematic = true;

            bullet.AddComponent<Bullet>();

            Object.DontDestroyOnLoad(bullet);
            return bullet;
        }

        private static GameObject CreateZombiePrefab()
        {
            GameObject zombie = new GameObject("ZombieTemplate");
            zombie.tag = "Enemy";
            zombie.SetActive(false);

            SpriteRenderer sr = zombie.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSquare;
            sr.color = new Color(0.2f, 0.8f, 0.2f);
            sr.sortingOrder = 3;
            zombie.transform.localScale = new Vector3(0.6f, 0.8f, 1f);

            BoxCollider2D col = zombie.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            Rigidbody2D rb = zombie.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.isKinematic = true;

            zombie.AddComponent<Zombie>();
            zombie.AddComponent<ZombieHealth>();

            Object.DontDestroyOnLoad(zombie);
            return zombie;
        }

        private static void SetupManagers(GameObject bulletPrefab, GameObject zombiePrefab)
        {
            // GameManager
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();

            // ObjectPool
            GameObject poolObj = new GameObject("ObjectPool");
            ObjectPool pool = poolObj.AddComponent<ObjectPool>();

            // Add pools after a frame delay
            var helper = new GameObject("PoolSetupHelper").AddComponent<PoolSetupHelper>();
            helper.Setup(pool, bulletPrefab, zombiePrefab);

            // SpawnerManager
            GameObject spawnerObj = new GameObject("SpawnerManager");
            SpawnerManager spawner = spawnerObj.AddComponent<SpawnerManager>();
            spawner.SetZombiePrefab(zombiePrefab);
        }

        private static void CreatePlayer(GameObject bulletPrefab)
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";

            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSquare;
            sr.color = Color.cyan;
            sr.sortingOrder = 10;
            player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
            player.transform.position = new Vector3(0, -6f, 0);

            BoxCollider2D col = player.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.isKinematic = true;

            // Fire point
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(player.transform);
            firePoint.transform.localPosition = new Vector3(0, 0.5f, 0);

            // Add scripts
            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerHealth>();
            player.AddComponent<TargetingSystem>();

            WeaponSystem ws = player.AddComponent<WeaponSystem>();
            ws.SetBulletPrefab(bulletPrefab);
            ws.SetFirePoint(firePoint.transform);
        }
    }

    /// <summary>
    /// Helper MonoBehaviour to set up pools after ObjectPool initializes.
    /// </summary>
    public class PoolSetupHelper : MonoBehaviour
    {
        private ObjectPool pool;
        private GameObject bulletPrefab;
        private GameObject zombiePrefab;

        public void Setup(ObjectPool p, GameObject bullet, GameObject zombie)
        {
            pool = p;
            bulletPrefab = bullet;
            zombiePrefab = zombie;
            StartCoroutine(SetupPools());
        }

        private System.Collections.IEnumerator SetupPools()
        {
            yield return null; // Wait for ObjectPool.Awake

            if (pool != null)
            {
                pool.AddPool("Bullet", bulletPrefab, 20);
                pool.AddPool("Zombie", zombiePrefab, 15);
            }

            Destroy(gameObject);
        }
    }
}
