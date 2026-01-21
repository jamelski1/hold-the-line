// AutoInitializer.cs - Bootstraps the game automatically on Play (3D Version - URP Compatible)
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
    ///
    /// NOTE: Uses URP shaders for Unity 6 / Universal Render Pipeline compatibility.
    /// </summary>
    public static class AutoInitializer
    {
        private static Shader urpShader;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            // Check if game is already set up
            if (GameManager.Instance != null)
            {
                Debug.Log("[AutoInitializer] Game already set up, skipping.");
                return;
            }

            Debug.Log("[AutoInitializer] Setting up 3D game (URP)...");
            SetupGame();
        }

        private static void SetupGame()
        {
            // Find URP shader - try multiple options for compatibility
            urpShader = FindURPShader();

            if (urpShader == null)
            {
                Debug.LogError("[AutoInitializer] Could not find URP shader! Objects will be pink.");
            }

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

            Debug.Log("[AutoInitializer] 3D Game setup complete (URP)!");
        }

        /// <summary>
        /// Find a working URP shader with multiple fallbacks
        /// </summary>
        private static Shader FindURPShader()
        {
            // Try URP Unlit first (simplest, works well for flat colors)
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null) return shader;

            // Try URP Lit
            shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null) return shader;

            // Try URP Simple Lit
            shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader != null) return shader;

            // Try legacy unlit as last resort
            shader = Shader.Find("Unlit/Color");
            if (shader != null) return shader;

            return null;
        }

        /// <summary>
        /// Create a URP-compatible material with the given color
        /// </summary>
        private static Material CreateURPMaterial(Color color)
        {
            if (urpShader == null)
            {
                urpShader = FindURPShader();
            }

            Material mat;
            if (urpShader != null)
            {
                mat = new Material(urpShader);
            }
            else
            {
                // Fallback - will be pink but at least won't crash
                mat = new Material(Shader.Find("Hidden/InternalErrorShader"));
                Debug.LogWarning("[AutoInitializer] Using fallback shader - material will be pink!");
            }

            // Set color - URP uses _BaseColor, legacy uses _Color
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }

            return mat;
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

            // Set color using URP material
            Renderer rend = bullet.GetComponent<Renderer>();
            rend.material = CreateURPMaterial(Color.yellow);

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

            // Set color (green zombie) using URP material
            Renderer rend = zombie.GetComponent<Renderer>();
            rend.material = CreateURPMaterial(new Color(0.2f, 0.8f, 0.2f));

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

            // Set color (cyan player) using URP material
            Renderer rend = player.GetComponent<Renderer>();
            rend.material = CreateURPMaterial(Color.cyan);

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

            // Dark ground material using URP
            Renderer rend = ground.GetComponent<Renderer>();
            rend.material = CreateURPMaterial(new Color(0.1f, 0.12f, 0.15f));

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
