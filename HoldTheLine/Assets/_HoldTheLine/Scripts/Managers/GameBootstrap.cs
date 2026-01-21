using UnityEngine;
using HoldTheLine.Player;
using HoldTheLine.Enemy;
using HoldTheLine.Weapons;

namespace HoldTheLine.Managers
{
    /// <summary>
    /// Bootstrap script that sets up the game at runtime if prefabs don't exist.
    /// Add this to your scene and it will create all necessary GameObjects.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Auto-Setup")]
        [SerializeField] private bool autoSetup = true;

        private static Sprite whiteSquare;

        private void Awake()
        {
            if (!autoSetup) return;

            // Check if the game is already set up
            if (FindObjectOfType<GameManager>() != null &&
                FindObjectOfType<PlayerController>() != null)
            {
                Destroy(gameObject);
                return;
            }

            SetupGame();
            Destroy(gameObject); // Remove bootstrap after setup
        }

        private void SetupGame()
        {
            Debug.Log("GameBootstrap: Setting up game...");

            // Create white square sprite for all visuals
            whiteSquare = CreateRuntimeSprite();

            // Setup camera if needed
            if (Camera.main == null)
            {
                SetupCamera();
            }
            else
            {
                ConfigureCamera(Camera.main);
            }

            // Create prefabs (in-memory)
            GameObject bulletPrefab = CreateBulletPrefab();
            GameObject zombiePrefab = CreateZombiePrefab();

            // Create managers
            SetupManagers(bulletPrefab, zombiePrefab);

            // Create player
            CreatePlayer(bulletPrefab);

            Debug.Log("GameBootstrap: Setup complete!");
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

        private void SetupCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";

            ConfigureCamera(cam);

            camObj.transform.position = new Vector3(0, 0, -10);
        }

        private void ConfigureCamera(Camera cam)
        {
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        private GameObject CreateBulletPrefab()
        {
            GameObject bullet = new GameObject("BulletPrefab");
            bullet.SetActive(false);

            SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSquare;
            sr.color = Color.yellow;
            bullet.transform.localScale = new Vector3(0.2f, 0.4f, 1f);

            BoxCollider2D col = bullet.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.isKinematic = true;

            bullet.AddComponent<Bullet>();

            DontDestroyOnLoad(bullet);
            return bullet;
        }

        private GameObject CreateZombiePrefab()
        {
            GameObject zombie = new GameObject("ZombiePrefab");
            zombie.tag = "Enemy";
            zombie.SetActive(false);

            SpriteRenderer sr = zombie.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSquare;
            sr.color = new Color(0.2f, 0.8f, 0.2f);
            zombie.transform.localScale = new Vector3(0.6f, 0.8f, 1f);

            BoxCollider2D col = zombie.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            Rigidbody2D rb = zombie.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.isKinematic = true;

            zombie.AddComponent<Zombie>();
            zombie.AddComponent<ZombieHealth>();

            DontDestroyOnLoad(zombie);
            return zombie;
        }

        private void SetupManagers(GameObject bulletPrefab, GameObject zombiePrefab)
        {
            // GameManager
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();

            // ObjectPool
            GameObject poolObj = new GameObject("ObjectPool");
            ObjectPool pool = poolObj.AddComponent<ObjectPool>();

            // We need to add pools at runtime since we can't serialize them
            StartCoroutine(SetupPoolsDelayed(pool, bulletPrefab, zombiePrefab));

            // SpawnerManager
            GameObject spawnerObj = new GameObject("SpawnerManager");
            SpawnerManager spawner = spawnerObj.AddComponent<SpawnerManager>();
            spawner.SetZombiePrefab(zombiePrefab);
        }

        private System.Collections.IEnumerator SetupPoolsDelayed(ObjectPool pool, GameObject bulletPrefab, GameObject zombiePrefab)
        {
            yield return null; // Wait one frame for ObjectPool.Awake to complete

            pool.AddPool("Bullet", bulletPrefab, 20);
            pool.AddPool("Zombie", zombiePrefab, 15);
        }

        private void CreatePlayer(GameObject bulletPrefab)
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";

            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSquare;
            sr.color = Color.cyan;
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
}
