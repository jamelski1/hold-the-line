#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace HoldTheLine.Editor
{
    public class GameSetupEditor : EditorWindow
    {
        [MenuItem("HoldTheLine/Setup Game Scene")]
        public static void SetupGameScene()
        {
            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create prefabs folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/_HoldTheLine/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/_HoldTheLine", "Prefabs");
            }

            // Create prefabs
            GameObject bulletPrefab = CreateBulletPrefab();
            GameObject zombiePrefab = CreateZombiePrefab();
            GameObject playerPrefab = CreatePlayerPrefab(bulletPrefab);

            // Setup scene
            SetupCamera();
            SetupManagers(bulletPrefab, zombiePrefab);
            InstantiatePlayer(playerPrefab);

            // Save scene
            string scenePath = "Assets/Scenes/Game.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);

            // Add scene to build settings
            AddSceneToBuildSettings(scenePath);

            Debug.Log("Game scene setup complete! Press Play to test.");
        }

        private static void SetupCamera()
        {
            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

            cam.orthographic = true;
            cam.orthographicSize = 8f; // Portrait-friendly
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cameraObj.transform.position = new Vector3(0, 0, -10);
        }

        private static GameObject CreateBulletPrefab()
        {
            GameObject bullet = new GameObject("Bullet");

            // Visual (simple sprite)
            SpriteRenderer sr = bullet.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = Color.yellow;
            bullet.transform.localScale = new Vector3(0.2f, 0.4f, 1f);

            // Collider
            BoxCollider2D col = bullet.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            // Rigidbody for physics
            Rigidbody2D rb = bullet.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.isKinematic = true;

            // Script
            bullet.AddComponent<Weapons.Bullet>();

            // Save as prefab
            string prefabPath = "Assets/_HoldTheLine/Prefabs/Bullet.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bullet, prefabPath);
            DestroyImmediate(bullet);

            return prefab;
        }

        private static GameObject CreateZombiePrefab()
        {
            GameObject zombie = new GameObject("Zombie");
            zombie.tag = "Enemy";

            // Visual
            SpriteRenderer sr = zombie.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = new Color(0.2f, 0.8f, 0.2f); // Green
            zombie.transform.localScale = new Vector3(0.6f, 0.8f, 1f);

            // Collider
            BoxCollider2D col = zombie.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            // Rigidbody
            Rigidbody2D rb = zombie.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.isKinematic = true;

            // Scripts
            zombie.AddComponent<Enemy.Zombie>();
            zombie.AddComponent<Enemy.ZombieHealth>();

            // Save as prefab
            string prefabPath = "Assets/_HoldTheLine/Prefabs/Zombie.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(zombie, prefabPath);
            DestroyImmediate(zombie);

            return prefab;
        }

        private static GameObject CreatePlayerPrefab(GameObject bulletPrefab)
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Default");

            // Visual
            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite();
            sr.color = Color.cyan;
            player.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            // Collider
            BoxCollider2D col = player.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            // Rigidbody
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.isKinematic = true;

            // Fire point
            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(player.transform);
            firePoint.transform.localPosition = new Vector3(0, 0.5f, 0);

            // Scripts
            player.AddComponent<Player.PlayerController>();
            player.AddComponent<Player.PlayerHealth>();
            player.AddComponent<Player.TargetingSystem>();
            var weaponSystem = player.AddComponent<Player.WeaponSystem>();

            // Save as prefab first
            string prefabPath = "Assets/_HoldTheLine/Prefabs/Player.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, prefabPath);
            DestroyImmediate(player);

            // Update prefab with references
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            var ws = prefabInstance.GetComponent<Player.WeaponSystem>();
            SerializedObject so = new SerializedObject(ws);
            so.FindProperty("bulletPrefab").objectReferenceValue = bulletPrefab;
            so.FindProperty("firePoint").objectReferenceValue = prefabInstance.transform.Find("FirePoint");
            so.ApplyModifiedProperties();
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabInstance);

            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        private static void SetupManagers(GameObject bulletPrefab, GameObject zombiePrefab)
        {
            // GameManager
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<Managers.GameManager>();

            // ObjectPool
            GameObject poolObj = new GameObject("ObjectPool");
            var pool = poolObj.AddComponent<Managers.ObjectPool>();

            SerializedObject poolSo = new SerializedObject(pool);
            SerializedProperty poolItems = poolSo.FindProperty("poolItems");

            // Add Bullet pool
            poolItems.arraySize = 2;

            poolItems.GetArrayElementAtIndex(0).FindPropertyRelative("tag").stringValue = "Bullet";
            poolItems.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue = bulletPrefab;
            poolItems.GetArrayElementAtIndex(0).FindPropertyRelative("poolSize").intValue = 20;

            // Add Zombie pool
            poolItems.GetArrayElementAtIndex(1).FindPropertyRelative("tag").stringValue = "Zombie";
            poolItems.GetArrayElementAtIndex(1).FindPropertyRelative("prefab").objectReferenceValue = zombiePrefab;
            poolItems.GetArrayElementAtIndex(1).FindPropertyRelative("poolSize").intValue = 15;

            poolSo.ApplyModifiedProperties();

            // SpawnerManager
            GameObject spawnerObj = new GameObject("SpawnerManager");
            var spawner = spawnerObj.AddComponent<Managers.SpawnerManager>();

            SerializedObject spawnerSo = new SerializedObject(spawner);
            spawnerSo.FindProperty("zombiePrefab").objectReferenceValue = zombiePrefab;
            spawnerSo.ApplyModifiedProperties();
        }

        private static void InstantiatePlayer(GameObject playerPrefab)
        {
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0, -6f, 0);
        }

        private static Sprite CreateSquareSprite()
        {
            // Check if we already have a white square sprite
            string spritePath = "Assets/_HoldTheLine/Prefabs/WhiteSquare.png";

            if (!System.IO.File.Exists(Application.dataPath + "/_HoldTheLine/Prefabs/WhiteSquare.png"))
            {
                // Create a simple white texture
                Texture2D tex = new Texture2D(4, 4);
                Color[] colors = new Color[16];
                for (int i = 0; i < 16; i++) colors[i] = Color.white;
                tex.SetPixels(colors);
                tex.Apply();

                byte[] bytes = tex.EncodeToPNG();
                System.IO.File.WriteAllBytes(Application.dataPath + "/_HoldTheLine/Prefabs/WhiteSquare.png", bytes);
                AssetDatabase.Refresh();

                // Set import settings
                TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 4;
                    importer.filterMode = FilterMode.Point;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            bool sceneExists = false;
            foreach (var scene in scenes)
            {
                if (scene.path == scenePath)
                {
                    sceneExists = true;
                    break;
                }
            }

            if (!sceneExists)
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        [MenuItem("HoldTheLine/Create Tags and Layers")]
        public static void CreateTagsAndLayers()
        {
            // Add tags
            AddTag("Player");
            AddTag("Enemy");
            AddTag("Bullet");

            Debug.Log("Tags created successfully!");
        }

        private static void AddTag(string tagName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
                tagManager.ApplyModifiedProperties();
            }
        }
    }
}
#endif
