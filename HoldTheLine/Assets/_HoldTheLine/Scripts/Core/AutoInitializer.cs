// AutoInitializer.cs - Bootstraps the game automatically on Play (3D Version)
// Location: Assets/_HoldTheLine/Scripts/Core/
// No need to attach - runs automatically via RuntimeInitializeOnLoadMethod

using UnityEngine;

namespace HoldTheLine
{
    /// <summary>
    /// Auto-initializes the game on startup using RuntimeInitializeOnLoadMethod.
    /// Creates all necessary 3D GameObjects if they don't exist.
    ///
    /// 3D AXIS MAPPING (Top-Down View):
    /// - X axis = horizontal (player left/right movement)
    /// - Z axis = depth (down the screen - zombies move in -Z direction)
    /// - Y axis = height (minimal use, camera looks down -Y)
    /// </summary>
    public static class AutoInitializer
    {
        private static Material defaultMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            // Check if game is already set up
            if (GameManager.Instance != null)
            {
                Debug.Log("[AutoInitializer] Game already set up, skipping.");
                return;
            }

            Debug.Log("[AutoInitializer] Setting up 3D game...");
            SetupGame();
        }

        private static void SetupGame()
        {
            // Create default material for 3D objects
            defaultMaterial = new Material(Shader.Find("Standard"));

            // Configure camera for top-down 3D
            ConfigureCamera();

            // Create 3D prefabs (in-memory templates)
            GameObject bulletPrefab = CreateBulletPrefab();
            GameObject zombiePrefab = CreateZombiePrefab();

            // Setup managers
            SetupManagers(bulletPrefab, zombiePrefab);

            // Create player
            CreatePlayer();

            // Create ground plane for visual reference
            CreateGroundPlane();

            // Auto-start the game after a brief delay
            var starter = new GameObject("GameStarter").AddComponent<GameStarter>();
            starter.StartCoroutine(starter.StartGameDelayed());

            Debug.Log("[AutoInitializer] 3D Game setup complete!");
        }

        private static void ConfigureCamera()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                // Top-down orthographic camera
                cam.orthographic = true;
                cam.orthographicSize = 8f;
                cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
                cam.clearFlags = CameraClearFlags.SolidColor;

                // Position camera above playfield, looking down
                cam.transform.position = new Vector3(0, 20f, 0);
                cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Look straight down

                // Adjust near/far clip planes for top-down view
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 50f;
            }
        }

        private static GameObject CreateBulletPrefab()
        {
            // Create bullet as a small sphere
            GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.name = "BulletPrefab";
            bullet.SetActive(false);

            // Scale for bullet size
            bullet.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            // Set color
            Renderer rend = bullet.GetComponent<Renderer>();
            Material bulletMat = new Material(Shader.Find("Standard"));
            bulletMat.color = Color.yellow;
            bulletMat.EnableKeyword("_EMISSION");
            bulletMat.SetColor("_EmissionColor", Color.yellow * 0.5f);
            rend.material = bulletMat;

            // Replace MeshCollider with SphereCollider for triggers
            Object.Destroy(bullet.GetComponent<MeshCollider>());
            SphereCollider col = bullet.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;

            // Add Rigidbody for 3D physics
            Rigidbody rb = bullet.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            // Add bullet script
            bullet.AddComponent<Bullet>();

            Object.DontDestroyOnLoad(bullet);
            return bullet;
        }

        private static GameObject CreateZombiePrefab()
        {
            // Create zombie as a capsule
            GameObject zombie = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            zombie.name = "ZombiePrefab";
            zombie.tag = "Enemy";
            zombie.SetActive(false);

            // Scale for zombie size
            zombie.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            // Set color (green zombie)
            Renderer rend = zombie.GetComponent<Renderer>();
            Material zombieMat = new Material(Shader.Find("Standard"));
            zombieMat.color = new Color(0.2f, 0.8f, 0.2f);
            rend.material = zombieMat;

            // Replace MeshCollider with CapsuleCollider for triggers
            Object.Destroy(zombie.GetComponent<CapsuleCollider>());
            CapsuleCollider col = zombie.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.height = 2f;
            col.radius = 0.5f;

            // Add Rigidbody for 3D physics
            Rigidbody rb = zombie.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            // Add zombie script
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

            // Use helper to set the prefabs
            var helper = new GameObject("PoolHelper").AddComponent<PoolPrefabHelper>();
            helper.Setup(pool, bulletPrefab, zombiePrefab);

            // SpawnerManager
            GameObject spawnerObj = new GameObject("SpawnerManager");
            spawnerObj.AddComponent<SpawnerManager>();

            // TargetingSystem
            GameObject targetingObj = new GameObject("TargetingSystem");
            targetingObj.AddComponent<TargetingSystem>();
        }

        private static void CreatePlayer()
        {
            // Create player as a capsule (cyan color)
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";

            // Scale for player size
            player.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            // Position player near bottom of playfield (negative Z)
            player.transform.position = new Vector3(0, 0.5f, -8f);

            // Set color (cyan player)
            Renderer rend = player.GetComponent<Renderer>();
            Material playerMat = new Material(Shader.Find("Standard"));
            playerMat.color = Color.cyan;
            playerMat.EnableKeyword("_EMISSION");
            playerMat.SetColor("_EmissionColor", Color.cyan * 0.3f);
            rend.material = playerMat;

            // Replace MeshCollider with CapsuleCollider for triggers
            Object.Destroy(player.GetComponent<CapsuleCollider>());
            CapsuleCollider col = player.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.height = 2f;
            col.radius = 0.5f;

            // Add Rigidbody for 3D physics
            Rigidbody rb = player.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            // Fire point (in front of player, toward +Z where zombies come from)
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(player.transform);
            firePoint.transform.localPosition = new Vector3(0, 0, 0.5f);

            // Add scripts
            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerHealth>();
            player.AddComponent<WeaponSystem>();
        }

        private static void CreateGroundPlane()
        {
            // Create a ground plane for visual reference
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, -0.1f, 0);
            ground.transform.localScale = new Vector3(1f, 1f, 3f); // 10 x 30 units

            // Dark ground material
            Renderer rend = ground.GetComponent<Renderer>();
            Material groundMat = new Material(Shader.Find("Standard"));
            groundMat.color = new Color(0.1f, 0.12f, 0.15f);
            rend.material = groundMat;

            // Disable collider on ground (we don't need it)
            Object.Destroy(ground.GetComponent<MeshCollider>());
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
                Debug.Log("[AutoInitializer] Game started! Use A/D or arrow keys to move horizontally.");
            }

            Destroy(gameObject);
        }
    }
}
