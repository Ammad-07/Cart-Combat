using UnityEditor;
using UnityEngine;
using CartCombat.NPC;

namespace CartCombat.Store.Editor
{
    /// <summary>
    /// Editor utility to force unlock and restore normal 3D Scene View controls,
    /// camera focus, perspective projection, and zoom distances.
    /// </summary>
    public static class SceneCameraFixer
    {
        [MenuItem("Cart Combat/Force Unlock & Reset Scene Camera", false, 0)]
        public static void ForceResetSceneCamera()
        {
            // Unlock cursor state if playmode locked it
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SceneView[] views = Resources.FindObjectsOfTypeAll<SceneView>();
            if (views == null || views.Length == 0)
            {
                if (SceneView.lastActiveSceneView != null)
                {
                    views = new SceneView[] { SceneView.lastActiveSceneView };
                }
            }

            if (views != null && views.Length > 0)
            {
                foreach (SceneView view in views)
                {
                    if (view == null) continue;

                    // 1. Unlock rotation and orbit
                    view.isRotationLocked = false;

                    // 2. Switch from flat Orthographic (< Top) to 3D Perspective
                    view.orthographic = false;

                    // 3. Set a healthy pivot distance so zoom and pan are smooth
                    Vector3 storeCenter = new Vector3(0f, 1.5f, 0f);
                    view.pivot = storeCenter;
                    view.size = 22f;

                    // 4. Position camera looking comfortably down at the arena
                    Quaternion viewAngle = Quaternion.Euler(38f, -25f, 0f);
                    view.rotation = viewAngle;
                    view.LookAt(storeCenter, viewAngle, 22f);

                    view.Repaint();
                }

                Debug.Log("<color=#00FF88><b>[Cart Combat]</b> Scene View Camera successfully UNLOCKED and reset to 3D Perspective!</color>");
            }
            else
            {
                // Open a fresh Scene view if none existed
                SceneView view = EditorWindow.GetWindow<SceneView>();
                view.orthographic = false;
                view.isRotationLocked = false;
                view.LookAt(new Vector3(0f, 1.5f, 0f), Quaternion.Euler(38f, -25f, 0f), 22f);
                view.Repaint();
            }

            // Also select the store so 'F' works automatically
            GameObject storeRoot = GameObject.Find("[GroceryStore]");
            if (storeRoot == null) storeRoot = GameObject.Find("Store_Floor");
            if (storeRoot != null)
            {
                Selection.activeGameObject = storeRoot;
            }

            EnsurePlayerVisible();
        }

        [MenuItem("Cart Combat/Ensure 3D Player Character Visible", false, 2)]
        public static void EnsurePlayerVisible()
        {
            Vector3 spawnPos = new Vector3(-12.6f, 0.05f, -20.16f);
            Quaternion spawnRot = Quaternion.identity;

            CartSpawnPoint pSpawn = Object.FindFirstObjectByType<CartSpawnPoint>();
            if (pSpawn != null && pSpawn.IsPlayer)
            {
                spawnPos = pSpawn.transform.position;
                spawnRot = pSpawn.transform.rotation;
            }

            GameObject player = CartCombat.Player.PlayerCharacterBuilder.SpawnOrSetupPlayer(spawnPos, spawnRot);
            if (player != null)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                SceneView.RepaintAll();
                Debug.Log("<color=#00FF88><b>[Cart Combat]</b> 3D Shopper Character & Combat Cart model is now ATTACHED and VISIBLE!</color>");
            }
        }

        [MenuItem("Cart Combat/Spawn Other Shoppers in Scene", false, 3)]
        public static void EnsureShoppersVisible()
        {
            NPCShopperSpawner spawner = Object.FindFirstObjectByType<NPCShopperSpawner>();
            if (spawner == null)
            {
                GameObject spawnerObj = new GameObject("NPC Spawner");
                spawner = spawnerObj.AddComponent<NPCShopperSpawner>();
            }

            NPCShopperAI[] existing = Object.FindObjectsByType<NPCShopperAI>(FindObjectsSortMode.None);
            if (existing == null || existing.Length == 0)
            {
                spawner.SpawnAll();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            SceneView.RepaintAll();
            Debug.Log("<color=#00FF88><b>[Cart Combat]</b> Other NPC Shoppers are now spawned and VISIBLE in the scene hierarchy!</color>");
        }

        [MenuItem("Cart Combat/Apply Professional Clean Naming", false, 4)]
        public static void ApplyCleanNaming()
        {
            // 1. Rename Scene file to StartingScreen if needed
            string currentScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            if (currentScenePath.Contains("SampleScene.unity"))
            {
                AssetDatabase.RenameAsset("Assets/Scenes/SampleScene.unity", "StartingScreen");
            }

            // 2. Rename Player and Cart Model
            CartCombat.Player.CartPlayerController player = Object.FindFirstObjectByType<CartCombat.Player.CartPlayerController>();
            if (player != null)
            {
                player.gameObject.name = "Player";
                Transform mesh = player.transform.Find("ShopperCartFighter_Mesh");
                if (mesh != null) mesh.name = "CartModel";
            }

            // 3. Rename NPC Spawner to Customers, and NPC_Shoppers to Customer_01, Customer_02...
            NPCShopperSpawner spawner = Object.FindFirstObjectByType<NPCShopperSpawner>();
            if (spawner != null)
            {
                spawner.gameObject.name = "Customers";
                Transform container = spawner.transform.Find("Shoppers");
                if (container != null) container.name = "Customers";
            }

            NPCShopperAI[] npcs = Object.FindObjectsByType<NPCShopperAI>(FindObjectsSortMode.None);
            for (int i = 0; i < npcs.Length; i++)
            {
                if (npcs[i] != null)
                {
                    npcs[i].gameObject.name = $"Customer_{i + 1:D2}";
                }
            }

            // 4. Rename [GroceryStore] to Store, and shorten long child names
            GameObject store = GameObject.Find("[GroceryStore]");
            if (store != null)
            {
                store.name = "Store";
            }
            else
            {
                store = GameObject.Find("Store");
            }

            if (store != null)
            {
                Transform checkouts = store.transform.Find("CheckoutArena");
                if (checkouts != null) checkouts.name = "Checkouts";

                Transform hazards = store.transform.Find("CombatHazards");
                if (hazards != null) hazards.name = "Hazards";

                Transform aisles = store.transform.Find("Aisles");
                if (aisles != null)
                {
                    for (int i = 0; i < aisles.childCount; i++)
                    {
                        Transform a = aisles.GetChild(i);
                        if (a.name.StartsWith("Aisle_"))
                        {
                            string[] parts = a.name.Split('_');
                            if (parts.Length >= 2)
                            {
                                a.name = $"Aisle_{parts[1]}";
                            }
                        }
                    }
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            SceneView.RepaintAll();
            Debug.Log("<color=#00FF88><b>[Cart Combat]</b> Professional clean naming applied across Scene and Hierarchy!</color>");
        }

        [MenuItem("Cart Combat/Stock All Shelves & Store Items", false, 5)]
        public static void StockAllShelves()
        {
            GroceryStoreBuilder builder = Object.FindFirstObjectByType<GroceryStoreBuilder>();
            if (builder != null)
            {
                builder.EnsureCustomItemsStocked(true);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                SceneView.RepaintAll();
                Debug.Log("<color=#00FF88><b>[Cart Combat]</b> All 4 Aisles and produce tables have been freshly STOCKED and PERFECTLY ALIGNED with 3D grocery items!</color>");
            }
        }

        [InitializeOnLoadMethod]
        private static void AutoCleanOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                RemoveGlitchLines();
                EnsurePlayerVisible();
                EnsureShoppersVisible();
                ApplyCleanNaming();
                StockAllShelves();
            };
        }

        [MenuItem("Cart Combat/Remove Glitch Lines & Clean Scene", false, 1)]
        public static void RemoveGlitchLines()
        {
            int deletedCount = 0;

            // 1. Remove all items that were accidentally parented inside scaled shelf tiers anywhere in the scene
            Transform[] allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (Transform t in allTransforms)
            {
                if (t == null) continue;
                if (t.name.StartsWith("Shelf_Left_Tier_") || t.name.StartsWith("Shelf_Right_Tier_"))
                {
                    for (int i = t.childCount - 1; i >= 0; i--)
                    {
                        Transform child = t.GetChild(i);
                        if (child != null)
                        {
                            Undo.DestroyObjectImmediate(child.gameObject);
                            deletedCount++;
                        }
                    }
                }
            }

            // 2. Scan scene for any stretched items with abnormal scales or outside the front entrance
            GroceryItem[] items = Object.FindObjectsByType<GroceryItem>(FindObjectsSortMode.None);
            foreach (GroceryItem it in items)
            {
                if (it == null) continue;
                Transform t = it.transform;
                if (t.lossyScale.z > 3f || t.lossyScale.y < 0.05f || t.position.z < -24.5f)
                {
                    Undo.DestroyObjectImmediate(it.gameObject);
                    deletedCount++;
                }
            }

            // Also check for any loose GameObject named Item_* that is stretched
            GameObject[] allGos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject go in allGos)
            {
                if (go == null) continue;
                if (go.name.StartsWith("Item_") && (go.transform.lossyScale.z > 3f || go.transform.lossyScale.y < 0.05f || go.transform.position.z < -24.5f))
                {
                    Undo.DestroyObjectImmediate(go);
                    deletedCount++;
                }
            }

            if (deletedCount > 0)
            {
                Debug.Log($"<color=#00FF88><b>[Cart Combat]</b> Successfully removed {deletedCount} stretched glitch lines outside the entrance! Scene is now clean.</color>");
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                SceneView.RepaintAll();
            }
        }
    }
}

