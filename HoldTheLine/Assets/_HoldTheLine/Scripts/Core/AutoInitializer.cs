// AutoInitializer.cs - Bootstraps the game automatically on Play
// Location: Assets/_HoldTheLine/Scripts/Core/
// No need to attach - runs automatically via RuntimeInitializeOnLoadMethod

using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Auto-initializes the game on startup using RuntimeInitializeOnLoadMethod.
    /// Creates all necessary GameObjects if they don't exist.
    /// </summary>
    public static class AutoInitializer
    {
        private static Sprite whiteSquare;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            // Check if game is already set up
            if (GameManager.Instance != null)
            {
                Debug.Log("[AutoInitializer] Game already set up, skipping.");
                return;
            }

            Debug.Log("[AutoInitializer] Setting up game...");
            SetupGame();
        }

        private static void SetupGame()
        {
            // Create sprite for visuals
            whiteSquare = CreateRuntimeSprite();

            // Configure camera
            ConfigureCamera();

            // Create prefabs (in-memory templates)
            GameObject bulletPrefab = CreateBulletPrefab();
            GameObject zombiePrefab = CreateZombiePrefab();

            // Setup managers
            SetupManagers(bulletPrefab, zombiePrefab);

            // Create player
            CreatePlayer();

            // Auto-start the game after a brief delay
            var starter = new GameObject("GameStarter").AddComponent<GameStarter>();
            starter.StartCoroutine(starter.StartGameDelayed());

            Debug.Log("[AutoInitializer] Game setup complete!");
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

        private static void ConfigureCamera()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 8f;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.transform.position = new Vector3(0, 0, -10);
            }
        }

        private static GameObject CreateBulletPrefab()
        {
            GameObject bullet = new GameObject("BulletPrefab");
            bullet.SetActive(false);

            SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSquare;
            sr.color = Color.yellow;
            sr.sortingOrder = 5;
            bullet.transform.localScale = new Vector3(0.15f, 0.3f, 1f);

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
            GameObject zombie = new GameObject("ZombiePrefab");
            zombie.tag = "Enemy";
            zombie.SetActive(false);

            SpriteRenderer sr = zombie.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSquare;
            sr.color = new Color(0.2f, 0.8f, 0.2f);
            sr.sortingOrder = 3;
            zombie.transform.localScale = new Vector3(0.5f, 0.7f, 1f);

            BoxCollider2D col = zombie.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            Rigidbody2D rb = zombie.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.isKinematic = true;

            zombie.AddComponent<ZombieUnit>();

            Object.DontDestroyOnLoad(zombie);
            return zombie;
        }

        private static void SetupManagers(GameObject bulletPrefab, GameObject zombiePrefab)
        {
            // GameManager
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();

            // ObjectPool - needs prefabs assigned
            GameObject poolObj = new GameObject("ObjectPoolManager");
            ObjectPool pool = poolObj.AddComponent<ObjectPool>();

            // Use reflection or a helper to set the prefabs
            var helper = new GameObject("PoolHelper").AddComponent<PoolPrefabHelper>();
            helper.Setup(pool, bulletPrefab, zombiePrefab);

            // SpawnerManager
            GameObject spawnerObj = new GameObject("SpawnerManager");
            SpawnerManager spawner = spawnerObj.AddComponent<SpawnerManager>();

            // TargetingSystem
            GameObject targetingObj = new GameObject("TargetingSystem");
            targetingObj.AddComponent<TargetingSystem>();
        }

        private static void CreatePlayer()
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";

            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSquare;
            sr.color = Color.cyan;
            sr.sortingOrder = 10;
            player.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            player.transform.position = new Vector3(0, -4f, 0);

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
            player.AddComponent<WeaponSystem>();
        }
    }

    /// <summary>
    /// Helper to set prefab references on ObjectPool via serialized fields
    /// </summary>
    public class PoolPrefabHelper : MonoBehaviour
    {
        public void Setup(ObjectPool pool, GameObject bulletPrefab, GameObject zombiePrefab)
        {
            StartCoroutine(SetupDelayed(pool, bulletPrefab, zombiePrefab));
        }

        private System.Collections.IEnumerator SetupDelayed(ObjectPool pool, GameObject bulletPrefab, GameObject zombiePrefab)
        {
            yield return null;

            // Use reflection to set the serialized fields
            var type = typeof(ObjectPool);
            var bulletField = type.GetField("bulletPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var zombieField = type.GetField("zombiePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (bulletField != null) bulletField.SetValue(pool, bulletPrefab);
            if (zombieField != null) zombieField.SetValue(pool, zombiePrefab);

            // Reinitialize pools
            var initMethod = type.GetMethod("InitializePools", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (initMethod != null) initMethod.Invoke(pool, null);

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Helper to start the game after initialization
    /// </summary>
    public class GameStarter : MonoBehaviour
    {
        public System.Collections.IEnumerator StartGameDelayed()
        {
            yield return null;
            yield return null; // Wait 2 frames for everything to initialize

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
                Debug.Log("[AutoInitializer] Game started! Use touch/mouse drag to move horizontally.");
            }

            Destroy(gameObject);
        }
    }
}
