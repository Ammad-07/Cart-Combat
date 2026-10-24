using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CartCombat.Store.Editor
{
    /// <summary>
    /// Custom Unity Editor window and inspector tools for Cart Combat's Grocery Store builder.
    /// Provides one-click arena generation, customizable dimensions, and new scene authoring.
    /// </summary>
    public class GroceryStoreEditorWindow : EditorWindow
    {
        private GroceryStoreConfig config = new GroceryStoreConfig();
        private Vector2 scrollPos;

        [MenuItem("Cart Combat/Build Grocery Store Arena", false, 1)]
        public static void ShowWindow()
        {
            GroceryStoreEditorWindow window = GetWindow<GroceryStoreEditorWindow>("Grocery Store Builder");
            window.minSize = new Vector2(380f, 520f);
            window.Show();
        }

        [MenuItem("Cart Combat/Create New Grocery Store Scene", false, 2)]
        public static void CreateNewStoreScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                newScene.name = "GroceryStoreArena";

                // Setup top-down combat camera
                Camera cam = Camera.main;
                if (cam != null)
                {
                    cam.transform.position = new Vector3(0f, 26f, -22f);
                    cam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
                    cam.fieldOfView = 60f;
                }

                // Create builder and generate
                GameObject storeRoot = new GameObject("[GroceryStore]");
                GroceryStoreBuilder builder = storeRoot.AddComponent<GroceryStoreBuilder>();
                builder.BuildStore();

                string scenePath = "Assets/Scenes/GroceryStoreArena.unity";
                EditorSceneManager.SaveScene(newScene, scenePath);
                Debug.Log($"[Cart Combat] New Grocery Store Arena scene created and saved at: {scenePath}");
            }
        }

        [MenuItem("Cart Combat/Spawn 3D Player Character (POV)", false, 3)]
        public static void SpawnPlayerMenu()
        {
            Vector3 spawnPos = new Vector3(-12.6f, 0.05f, -20.16f);
            Quaternion spawnRot = Quaternion.identity;

            CartSpawnPoint pSpawn = Object.FindFirstObjectByType<CartSpawnPoint>();
            if (pSpawn != null && pSpawn.IsPlayer)
            {
                spawnPos = pSpawn.transform.position;
                spawnRot = pSpawn.transform.rotation;
            }

            CartCombat.Player.PlayerCharacterBuilder.SpawnOrSetupPlayer(spawnPos, spawnRot);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(10);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("CART COMBAT", headerStyle);

            GUIStyle subHeaderStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("3D Grocery Store Arena Generator", subHeaderStyle);
            EditorGUILayout.Space(10);

            // Store Dimensions
            EditorGUILayout.LabelField("Store Boundaries", EditorStyles.boldLabel);
            config.storeWidth = EditorGUILayout.Slider("Store Width (m)", config.storeWidth, 20f, 60f);
            config.storeLength = EditorGUILayout.Slider("Store Length (m)", config.storeLength, 25f, 80f);
            config.wallHeight = EditorGUILayout.Slider("Wall Height (m)", config.wallHeight, 4f, 10f);

            EditorGUILayout.Space(8);

            // Aisles
            EditorGUILayout.LabelField("Gondola Shelving Aisles", EditorStyles.boldLabel);
            config.aisleCount = EditorGUILayout.IntSlider("Aisle Count", config.aisleCount, 2, 8);
            config.aisleLength = EditorGUILayout.Slider("Aisle Length (m)", config.aisleLength, 12f, 40f);
            config.aisleSpacing = EditorGUILayout.Slider("Aisle Spacing (m)", config.aisleSpacing, 3.5f, 8f);
            config.shelfTiers = EditorGUILayout.IntSlider("Shelf Tiers", config.shelfTiers, 2, 5);
            config.populateShelves = EditorGUILayout.Toggle("Populate Grocery Items", config.populateShelves);
            if (config.populateShelves)
            {
                config.itemsPerShelfTier = EditorGUILayout.IntSlider("Items Per Tier", config.itemsPerShelfTier, 2, 12);
            }

            EditorGUILayout.Space(8);

            // Checkout
            EditorGUILayout.LabelField("Checkout Arena", EditorStyles.boldLabel);
            config.checkoutLaneCount = EditorGUILayout.IntSlider("Checkout Lanes", config.checkoutLaneCount, 2, 8);
            config.checkoutSpacing = EditorGUILayout.Slider("Lane Spacing (m)", config.checkoutSpacing, 2.5f, 5f);
            config.checkoutZPosition = EditorGUILayout.Slider("Checkout Z Position", config.checkoutZPosition, -35f, 0f);

            EditorGUILayout.Space(8);

            // Lighting
            EditorGUILayout.LabelField("Lighting & Atmosphere", EditorStyles.boldLabel);
            config.generateCeilingLights = EditorGUILayout.Toggle("Generate Ceiling Lights", config.generateCeilingLights);
            if (config.generateCeilingLights)
            {
                config.lightRowsX = EditorGUILayout.IntSlider("Light Columns (X)", config.lightRowsX, 2, 6);
                config.lightRowsZ = EditorGUILayout.IntSlider("Light Rows (Z)", config.lightRowsZ, 2, 8);
                config.lightIntensity = EditorGUILayout.Slider("Light Intensity", config.lightIntensity, 5f, 50f);
            }

            EditorGUILayout.Space(8);

            // Hazards
            EditorGUILayout.LabelField("Combat Hazards & Props", EditorStyles.boldLabel);
            config.spawnHazards = EditorGUILayout.Toggle("Spawn Hazards (Signs, Bananas)", config.spawnHazards);

            EditorGUILayout.Space(15);

            // Action Buttons
            GUI.backgroundColor = new Color(0.2f, 0.75f, 0.35f);
            if (GUILayout.Button("Build Store in Current Scene", GUILayout.Height(38)))
            {
                BuildInCurrentScene();
            }

            GUI.backgroundColor = new Color(0.95f, 0.6f, 0.1f);
            if (GUILayout.Button("Spawn / Reset 3D Player Character (POV)", GUILayout.Height(32)))
            {
                SpawnPlayerMenu();
            }

            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.95f);
            if (GUILayout.Button("Create New Grocery Store Scene", GUILayout.Height(30)))
            {
                CreateNewStoreScene();
            }

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("Clear Store from Current Scene", GUILayout.Height(26)))
            {
                ClearStore();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();
        }

        private void BuildInCurrentScene()
        {
            GroceryStoreBuilder existing = Object.FindFirstObjectByType<GroceryStoreBuilder>();
            GameObject root;
            if (existing != null)
            {
                root = existing.gameObject;
            }
            else
            {
                root = GameObject.Find("[GroceryStore]");
                if (root == null)
                {
                    root = new GameObject("[GroceryStore]");
                }
            }

            GroceryStoreBuilder builder = root.GetComponent<GroceryStoreBuilder>();
            if (builder == null) builder = root.AddComponent<GroceryStoreBuilder>();

            // Copy window config to builder
            CopyConfig(config, builder.Config);
            builder.BuildStore();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
        }

        private void ClearStore()
        {
            GameObject root = GameObject.Find("[GroceryStore]");
            if (root != null)
            {
                Undo.DestroyObjectImmediate(root);
                Debug.Log("[Cart Combat] Grocery store removed from current scene.");
            }
        }

        private void CopyConfig(GroceryStoreConfig source, GroceryStoreConfig target)
        {
            target.storeWidth = source.storeWidth;
            target.storeLength = source.storeLength;
            target.wallHeight = source.wallHeight;
            target.aisleCount = source.aisleCount;
            target.aisleLength = source.aisleLength;
            target.aisleSpacing = source.aisleSpacing;
            target.shelfTiers = source.shelfTiers;
            target.populateShelves = source.populateShelves;
            target.itemsPerShelfTier = source.itemsPerShelfTier;
            target.checkoutLaneCount = source.checkoutLaneCount;
            target.checkoutSpacing = source.checkoutSpacing;
            target.checkoutZPosition = source.checkoutZPosition;
            target.generateCeilingLights = source.generateCeilingLights;
            target.lightRowsX = source.lightRowsX;
            target.lightRowsZ = source.lightRowsZ;
            target.lightIntensity = source.lightIntensity;
            target.spawnHazards = source.spawnHazards;
        }
    }

    /// <summary>
    /// Custom inspector providing convenient in-scene buttons on the GroceryStoreBuilder component.
    /// </summary>
    [CustomEditor(typeof(GroceryStoreBuilder))]
    public class GroceryStoreBuilderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GroceryStoreBuilder builder = (GroceryStoreBuilder)target;

            EditorGUILayout.Space(12);
            GUI.backgroundColor = new Color(0.2f, 0.75f, 0.35f);
            if (GUILayout.Button("Rebuild Store Arena", GUILayout.Height(36)))
            {
                builder.BuildStore();
                EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
            }

            GUI.backgroundColor = Color.white;
        }
    }
}
