using System.Collections.Generic;
using UnityEngine;
using CartCombat.Player;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CartCombat.Store
{
    /// <summary>
    /// Core procedural generator and layout builder for the Cart Combat 3D Grocery Store arena.
    /// Builds a full supermarket environment complete with checkout lanes, gondola shelving aisles,
    /// produce section, bakery counter, freezer cooler wall, lighting, and combat hazards.
    /// </summary>
    [ExecuteInEditMode]
    public class GroceryStoreBuilder : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GroceryStoreConfig config = new GroceryStoreConfig();

        [Header("Material Cache")]
        private Material floorMat;
        private Material wallMat;
        private Material trimMat;
        private Material shelfMat;
        private Material shelfBackMat;
        private Material checkoutMat;
        private Material conveyorMat;
        private Material registerMat;
        private Material woodMat;
        private Material glassMat;
        private Material lightFixtureMat;
        private Material lightEmissiveMat;
        private Material metalChromeMat;
        private Material signMat;
        private Material cautionMat;

        // Item materials
        private Material cerealMat;
        private Material cannedMat;
        private Material drinkMat;
        private Material cleaningMat;
        private Material appleMat;
        private Material orangeMat;
        private Material bananaMat;

        public GroceryStoreConfig Config => config;

        private void Awake()
        {
            if (transform.childCount == 0)
            {
                BuildStore();
            }
            else
            {
                EnsureCustomItemsStocked();
            }
        }

        /// <summary>
        /// Checks if the store already has the new 3D custom grocery items (Bananas, Fish, Apples, Chips, etc.).
        /// If not, populates produce tables and deli counters so the player immediately has all weapons available!
        /// </summary>
        public void EnsureCustomItemsStocked()
        {
            GroceryItem[] items = Object.FindObjectsByType<GroceryItem>(FindObjectsSortMode.None);
            bool hasFish = false;
            bool hasBanana = false;
            foreach (GroceryItem it in items)
            {
                if (it.ItemType == GroceryItemType.Fish) hasFish = true;
                if (it.ItemType == GroceryItemType.Banana) hasBanana = true;
            }

            if (hasFish && hasBanana) return;

            Debug.Log("[Cart Combat] Stocking store with fresh 3D Bananas, Apples, Fish, Baguettes, Chips, and Drinks!");

            // Look for Dept_FreshProduce
            Transform produceDept = transform.Find("Departments/Dept_FreshProduce");
            if (produceDept != null)
            {
                for (int t = 0; t < produceDept.childCount; t++)
                {
                    Transform table = produceDept.GetChild(t);
                    List<GameObject> toRemove = new List<GameObject>();
                    foreach (Transform c in table)
                    {
                        if (c.name.StartsWith("Fruit_") || c.name.StartsWith("Item_"))
                        {
                            toRemove.Add(c.gameObject);
                        }
                    }
                    foreach (GameObject g in toRemove)
                    {
                        if (Application.isPlaying) Destroy(g);
                        else DestroyImmediate(g);
                    }

                    GroceryItemType fType = (t == 0) ? GroceryItemType.Apple : ((t == 1) ? GroceryItemType.Watermelon : GroceryItemType.Banana);
                    for (int fx = -1; fx <= 1; fx++)
                    {
                        for (int fz = -1; fz <= 1; fz++)
                        {
                            Vector3 localPos = new Vector3(fx * 0.75f, 1.22f, fz * 0.95f);
                            Vector3 worldPos = table.TransformPoint(localPos);
                            Quaternion rot = (fType == GroceryItemType.Banana)
                                ? Quaternion.Euler(0f, Random.Range(-45f, 45f), 15f)
                                : Quaternion.identity;
                            GameObject fruitObj = GroceryItemFactory.CreateItem(fType, worldPos, rot, table);
                            Rigidbody rb = fruitObj.GetComponent<Rigidbody>();
                            if (rb != null) rb.isKinematic = true;
                        }
                    }
                }
            }

            // Look for Dept_BakeryDeli
            Transform deliDept = transform.Find("Departments/Dept_BakeryDeli");
            if (deliDept != null)
            {
                // Stock Fresh Cartoon Fish
                for (int i = -3; i <= -1; i++)
                {
                    Vector3 fishPos = deliDept.position + new Vector3(0.35f, 1.15f, i * 2.2f);
                    Quaternion fishRot = Quaternion.Euler(0f, 90f + Random.Range(-20f, 20f), 0f);
                    GameObject fishObj = GroceryItemFactory.CreateItem(GroceryItemType.Fish, fishPos, fishRot, deliDept);
                    Rigidbody rb = fishObj.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;
                }

                // Stock French Baguettes
                for (int i = 1; i <= 3; i++)
                {
                    Vector3 breadPos = deliDept.position + new Vector3(0.35f, 1.15f, i * 2.2f);
                    Quaternion breadRot = Quaternion.Euler(0f, Random.Range(-15f, 15f), 10f);
                    GameObject breadObj = GroceryItemFactory.CreateItem(GroceryItemType.Baguette, breadPos, breadRot, deliDept);
                    Rigidbody rb = breadObj.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;
                }
            }
        }

        private void InitializeMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            if (litShader == null) litShader = Shader.Find("Unlit/Color");

            floorMat = GetOrCreateMat(litShader, "Mat_StoreFloor", "FloorMat", new Color(0.88f, 0.88f, 0.86f), 0.05f, 0.65f);
            wallMat = GetOrCreateMat(litShader, "Mat_StoreWall", "WallMat", new Color(0.92f, 0.93f, 0.94f), 0.0f, 0.2f);
            trimMat = GetOrCreateMat(litShader, "Mat_StoreTrim", "TrimMat", new Color(0.18f, 0.24f, 0.32f), 0.2f, 0.4f);
            shelfMat = GetOrCreateMat(litShader, "Mat_MetalShelf", "ShelfMat", new Color(0.82f, 0.84f, 0.86f), 0.45f, 0.45f);
            shelfBackMat = CreateMat(litShader, "ShelfBackMat", new Color(0.65f, 0.68f, 0.72f), 0.2f, 0.3f);
            checkoutMat = GetOrCreateMat(litShader, "Mat_CheckoutCounter", "CheckoutMat", new Color(0.15f, 0.32f, 0.52f), 0.1f, 0.4f);
            conveyorMat = GetOrCreateMat(litShader, "Mat_ConveyorBelt", "ConveyorMat", new Color(0.12f, 0.12f, 0.12f), 0.0f, 0.15f);
            registerMat = CreateMat(litShader, "RegisterMat", new Color(0.22f, 0.24f, 0.26f), 0.3f, 0.5f);
            woodMat = GetOrCreateMat(litShader, "Mat_WoodProduce", "WoodMat", new Color(0.68f, 0.46f, 0.26f), 0.0f, 0.2f);
            
            // Glass material
            glassMat = GetOrCreateMat(litShader, "Mat_Glass", "GlassMat", new Color(0.7f, 0.9f, 0.95f, 0.35f), 0.1f, 0.95f);
            if (glassMat.HasProperty("_Surface")) glassMat.SetFloat("_Surface", 1); // Transparent in URP
            
            lightFixtureMat = CreateMat(litShader, "LightFixtureMat", new Color(0.2f, 0.2f, 0.2f), 0.5f, 0.3f);
            lightEmissiveMat = GetOrCreateMat(litShader, "Mat_FluorescentLight", "LightEmissiveMat", Color.white, 0f, 0f);
            if (lightEmissiveMat.HasProperty("_EmissionColor"))
            {
                lightEmissiveMat.EnableKeyword("_EMISSION");
                lightEmissiveMat.SetColor("_EmissionColor", Color.white * 2.5f);
            }

            metalChromeMat = CreateMat(litShader, "ChromeMat", new Color(0.85f, 0.85f, 0.88f), 0.85f, 0.85f);
            signMat = CreateMat(litShader, "SignMat", new Color(0.1f, 0.45f, 0.3f), 0.1f, 0.4f);
            cautionMat = GetOrCreateMat(litShader, "Mat_HazardCaution", "CautionMat", new Color(0.95f, 0.8f, 0.05f), 0.0f, 0.5f);

            // Item materials
            cerealMat = CreateMat(litShader, "CerealMat", new Color(0.9f, 0.25f, 0.15f), 0.0f, 0.3f);
            cannedMat = CreateMat(litShader, "CannedMat", new Color(0.75f, 0.75f, 0.78f), 0.7f, 0.5f);
            drinkMat = CreateMat(litShader, "DrinkMat", new Color(0.1f, 0.55f, 0.85f), 0.1f, 0.8f);
            cleaningMat = CreateMat(litShader, "CleaningMat", new Color(0.95f, 0.55f, 0.05f), 0.0f, 0.4f);
            appleMat = CreateMat(litShader, "AppleMat", new Color(0.85f, 0.1f, 0.1f), 0.0f, 0.6f);
            orangeMat = CreateMat(litShader, "OrangeMat", new Color(0.95f, 0.5f, 0.05f), 0.0f, 0.5f);
            bananaMat = CreateMat(litShader, "BananaMat", new Color(0.95f, 0.85f, 0.1f), 0.0f, 0.4f);
        }

        private Material GetOrCreateMat(Shader shader, string matFileName, string fallbackName, Color color, float metallic, float smoothness)
        {
#if UNITY_EDITOR
            Material existing = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/Store/{matFileName}.mat");
            if (existing != null) return existing;
#endif
            return CreateMat(shader, fallbackName, color, metallic, smoothness);
        }

        private Material CreateMat(Shader shader, string name, Color color, float metallic, float smoothness)
        {
            Material mat = new Material(shader)
            {
                name = name,
                color = color
            };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }

        /// <summary>
        /// Rebuilds the entire grocery store arena hierarchy from scratch.
        /// </summary>
        public void BuildStore()
        {
            InitializeMaterials();

            // Clear old children
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in transform)
            {
                children.Add(child.gameObject);
            }
            foreach (GameObject child in children)
            {
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            // Create main groups
            GameObject archGroup = CreateGroup("Architecture");
            GameObject checkoutGroup = CreateGroup("CheckoutArena");
            GameObject cartCorralGroup = CreateGroup("CartCorral");
            GameObject aislesGroup = CreateGroup("Aisles");
            GameObject departmentsGroup = CreateGroup("Departments");
            GameObject hazardsGroup = CreateGroup("CombatHazards");

            // 1. Architecture
            BuildFloor(archGroup.transform);
            BuildPerimeterWalls(archGroup.transform);
            if (config.generateCeilingLights)
            {
                BuildCeilingAndLighting(archGroup.transform);
            }

            // 2. Checkout Lanes
            BuildCheckoutArena(checkoutGroup.transform);

            // 3. Cart Corral & Spawn Points
            BuildCartCorral(cartCorralGroup.transform);

            // 4. Gondola Aisles
            BuildAisles(aislesGroup.transform);

            // 5. Produce, Deli, and Freezer Departments
            BuildDepartments(departmentsGroup.transform);

            // 6. Hazards (Wet floor signs, banana peels, pallets)
            if (config.spawnHazards)
            {
                BuildHazards(hazardsGroup.transform);
            }

            // 7. Playable 3D Character & POV Camera
            if (config.spawnPlayerCharacter)
            {
                BuildPlayerCharacter();
            }

            Debug.Log($"[Cart Combat] Grocery Store Arena generated successfully! ({config.storeWidth}m x {config.storeLength}m)");
        }

        private void BuildPlayerCharacter()
        {
            Vector3 spawnPos = new Vector3(-config.storeWidth * 0.35f, 0.05f, -config.storeLength * 0.42f + 1.5f);
            Quaternion spawnRot = Quaternion.identity;

            CartSpawnPoint pSpawn = GetComponentInChildren<CartSpawnPoint>();
            if (pSpawn != null && pSpawn.IsPlayer)
            {
                spawnPos = pSpawn.transform.position;
                spawnRot = pSpawn.transform.rotation;
            }

            PlayerCharacterBuilder.SpawnOrSetupPlayer(spawnPos, spawnRot);
        }

        private GameObject CreateGroup(string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(transform, false);
            return group;
        }

        #region 1. Architecture

        private void BuildFloor(Transform parent)
        {
            float thickness = 0.6f;
            float totalW = config.storeWidth + thickness * 2f;
            float totalL = config.storeLength + thickness * 2f;

            // Main polished supermarket interior floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Store_Floor";
            floor.transform.SetParent(parent, false);
            // Height is 0.2m, centered at Y = -0.1m, so top surface is precisely at Y = 0.000m
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(totalW, 0.2f, totalL);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            floor.isStatic = true;

            // Floor Foundation Slab (prevents any void or ground clipping underneath)
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Store_FoundationSlab";
            slab.transform.SetParent(parent, false);
            slab.transform.position = new Vector3(0f, -0.6f, 0f);
            slab.transform.localScale = new Vector3(totalW + 2f, 0.8f, totalL + 2f);
            slab.GetComponent<Renderer>().sharedMaterial = trimMat;
            slab.isStatic = true;
        }

        private void BuildPerimeterWalls(Transform parent)
        {
            float w = config.storeWidth;
            float l = config.storeLength;
            float h = config.wallHeight;
            float thickness = 0.6f;

            // Back wall (North: +Z)
            CreateWall(parent, "Wall_Back_North", new Vector3(0f, h * 0.5f, l * 0.5f), new Vector3(w, h, thickness));

            // Left wall (West: -X)
            CreateWall(parent, "Wall_Side_West", new Vector3(-w * 0.5f, h * 0.5f, 0f), new Vector3(thickness, h, l));

            // Right wall (East: +X)
            CreateWall(parent, "Wall_Side_East", new Vector3(w * 0.5f, h * 0.5f, 0f), new Vector3(thickness, h, l));

            // Front wall (South: -Z) - with entrance opening in the center
            float entranceWidth = 10f;
            float sideWallWidth = (w - entranceWidth) * 0.5f;

            // Front left
            CreateWall(parent, "Wall_Front_Left", new Vector3(-w * 0.5f + sideWallWidth * 0.5f, h * 0.5f, -l * 0.5f), new Vector3(sideWallWidth, h, thickness));
            // Front right
            CreateWall(parent, "Wall_Front_Right", new Vector3(w * 0.5f - sideWallWidth * 0.5f, h * 0.5f, -l * 0.5f), new Vector3(sideWallWidth, h, thickness));
            // Front lintel over entrance
            CreateWall(parent, "Entrance_Lintel", new Vector3(0f, h - 0.75f, -l * 0.5f), new Vector3(entranceWidth, 1.5f, thickness));

            // Glass automatic sliding doors in entrance opening
            GameObject entrance = new GameObject("Entrance_GlassDoors");
            entrance.transform.SetParent(parent, false);
            entrance.transform.position = new Vector3(0f, 1.6f, -l * 0.5f);

            GameObject doorL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorL.name = "GlassDoor_Left";
            doorL.transform.SetParent(entrance.transform, false);
            doorL.transform.localPosition = new Vector3(-2.2f, 0f, 0f);
            doorL.transform.localScale = new Vector3(4.2f, 3.2f, 0.1f);
            doorL.GetComponent<Renderer>().sharedMaterial = glassMat;

            GameObject doorR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorR.name = "GlassDoor_Right";
            doorR.transform.SetParent(entrance.transform, false);
            doorR.transform.localPosition = new Vector3(2.2f, 0f, 0f);
            doorR.transform.localScale = new Vector3(4.2f, 3.2f, 0.1f);
            doorR.GetComponent<Renderer>().sharedMaterial = glassMat;

            // Baseboard trims along walls
            CreateTrim(parent, "Trim_Back", new Vector3(0f, 0.15f, l * 0.5f - thickness * 0.5f), new Vector3(w, 0.3f, 0.15f));
            CreateTrim(parent, "Trim_West", new Vector3(-w * 0.5f + thickness * 0.5f, 0.15f, 0f), new Vector3(0.15f, 0.3f, l));
            CreateTrim(parent, "Trim_East", new Vector3(w * 0.5f - thickness * 0.5f, 0.15f, 0f), new Vector3(0.15f, 0.3f, l));
        }

        private void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().sharedMaterial = wallMat;
            wall.isStatic = true;
        }

        private void CreateTrim(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name = name;
            trim.transform.SetParent(parent, false);
            trim.transform.position = pos;
            trim.transform.localScale = scale;
            trim.GetComponent<Renderer>().sharedMaterial = trimMat;
            trim.isStatic = true;
        }

        private void BuildCeilingAndLighting(Transform parent)
        {
            GameObject lightingRoot = new GameObject("Ceiling_Lighting_Grid");
            lightingRoot.transform.SetParent(parent, false);

            float startX = -config.storeWidth * 0.35f;
            float endX = config.storeWidth * 0.35f;
            float stepX = (config.lightRowsX > 1) ? (endX - startX) / (config.lightRowsX - 1) : 0f;

            float startZ = -config.storeLength * 0.38f;
            float endZ = config.storeLength * 0.38f;
            float stepZ = (config.lightRowsZ > 1) ? (endZ - startZ) / (config.lightRowsZ - 1) : 0f;

            float yPos = config.wallHeight - 0.4f;

            for (int x = 0; x < config.lightRowsX; x++)
            {
                for (int z = 0; z < config.lightRowsZ; z++)
                {
                    Vector3 fixturePos = new Vector3(startX + x * stepX, yPos, startZ + z * stepZ);
                    CreateFluorescentFixture(lightingRoot.transform, fixturePos);
                }
            }
        }

        private void CreateFluorescentFixture(Transform parent, Vector3 pos)
        {
            GameObject fixture = new GameObject("Fluorescent_Light");
            fixture.transform.SetParent(parent, false);
            fixture.transform.position = pos;

            // Housing
            GameObject housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housing.name = "FixtureHousing";
            housing.transform.SetParent(fixture.transform, false);
            housing.transform.localScale = new Vector3(1.2f, 0.12f, 3.2f);
            housing.GetComponent<Renderer>().sharedMaterial = lightFixtureMat;
            DestroyImmediate(housing.GetComponent<Collider>());

            // Emissive Tube
            GameObject tube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tube.name = "EmissiveTube";
            tube.transform.SetParent(fixture.transform, false);
            tube.transform.localPosition = new Vector3(0f, -0.06f, 0f);
            tube.transform.localScale = new Vector3(0.9f, 0.04f, 3.0f);
            tube.GetComponent<Renderer>().sharedMaterial = lightEmissiveMat;
            DestroyImmediate(tube.GetComponent<Collider>());

            // Point Light
            GameObject lightObj = new GameObject("PointLight");
            lightObj.transform.SetParent(fixture.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            Light l = lightObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = config.lightColor;
            l.intensity = config.lightIntensity;
            l.range = 14f;
            l.shadows = LightShadows.None;
        }

        #endregion

        #region 2. Checkout Arena

        private void BuildCheckoutArena(Transform parent)
        {
            int laneCount = config.checkoutLaneCount;
            float spacing = config.checkoutSpacing;
            float startX = -((laneCount - 1) * spacing) * 0.5f;

            for (int i = 0; i < laneCount; i++)
            {
                float x = startX + i * spacing;
                Vector3 lanePos = new Vector3(x, 0f, config.checkoutZPosition);
                CreateCheckoutLane(parent, i + 1, lanePos);
            }
        }

        private void CreateCheckoutLane(Transform parent, int laneNum, Vector3 pos)
        {
            GameObject lane = new GameObject($"Checkout_Lane_{laneNum:D2}");
            lane.transform.SetParent(parent, false);
            lane.transform.position = pos;
            CheckoutLane comp = lane.AddComponent<CheckoutLane>();

            // Main counter base
            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "CounterBase";
            counter.transform.SetParent(lane.transform, false);
            counter.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            counter.transform.localScale = new Vector3(1.1f, 0.9f, 4.2f);
            counter.GetComponent<Renderer>().sharedMaterial = checkoutMat;

            // Conveyor Belt
            GameObject belt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            belt.name = "ConveyorBelt";
            belt.transform.SetParent(lane.transform, false);
            belt.transform.localPosition = new Vector3(0f, 0.92f, 0.4f);
            belt.transform.localScale = new Vector3(0.9f, 0.06f, 2.8f);
            belt.GetComponent<Renderer>().sharedMaterial = conveyorMat;

            // Register stand
            GameObject registerStand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            registerStand.name = "CashRegisterStand";
            registerStand.transform.SetParent(lane.transform, false);
            registerStand.transform.localPosition = new Vector3(0.25f, 1.15f, -1.2f);
            registerStand.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            registerStand.GetComponent<Renderer>().sharedMaterial = registerMat;

            // Register Screen Monitor
            GameObject monitor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monitor.name = "POS_Screen";
            monitor.transform.SetParent(registerStand.transform, false);
            monitor.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            monitor.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            monitor.transform.localScale = new Vector3(0.45f, 0.35f, 0.08f);
            monitor.GetComponent<Renderer>().sharedMaterial = trimMat;

            // Scanner glass bed
            GameObject scanner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            scanner.name = "BarcodeScanner";
            scanner.transform.SetParent(lane.transform, false);
            scanner.transform.localPosition = new Vector3(0f, 0.93f, -0.6f);
            scanner.transform.localScale = new Vector3(0.4f, 0.02f, 0.4f);
            scanner.GetComponent<Renderer>().sharedMaterial = glassMat;

            // Overhead Lane Number Pillar
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "LaneNumberPole";
            pillar.transform.SetParent(lane.transform, false);
            pillar.transform.localPosition = new Vector3(0.6f, 1.75f, -1.8f);
            pillar.transform.localScale = new Vector3(0.1f, 1.75f, 0.1f);
            pillar.GetComponent<Renderer>().sharedMaterial = metalChromeMat;

            GameObject signCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signCube.name = $"Lane_{laneNum}_Light";
            signCube.transform.SetParent(lane.transform, false);
            signCube.transform.localPosition = new Vector3(0.6f, 3.2f, -1.8f);
            signCube.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
            signCube.GetComponent<Renderer>().sharedMaterial = lightEmissiveMat;

            // Impulse Buy Candy Rack
            GameObject impulseRack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            impulseRack.name = "ImpulseBuyRack";
            impulseRack.transform.SetParent(lane.transform, false);
            impulseRack.transform.localPosition = new Vector3(-0.75f, 0.6f, 0.3f);
            impulseRack.transform.localScale = new Vector3(0.35f, 1.2f, 2.2f);
            impulseRack.GetComponent<Renderer>().sharedMaterial = shelfMat;

            comp.Initialize(laneNum, true);
        }

        #endregion

        #region 3. Cart Corral & Spawn Points

        private void BuildCartCorral(Transform parent)
        {
            Vector3 corralPos = new Vector3(-config.storeWidth * 0.35f, 0f, -config.storeLength * 0.42f);
            GameObject corral = new GameObject("CartCorral_StagingArea");
            corral.transform.SetParent(parent, false);
            corral.transform.position = corralPos;

            // Chrome Railing Bay
            CreateChromeRail(corral.transform, "Rail_Left", new Vector3(-2f, 0.45f, 0f), new Vector3(0.1f, 0.9f, 6f));
            CreateChromeRail(corral.transform, "Rail_Right", new Vector3(2f, 0.45f, 0f), new Vector3(0.1f, 0.9f, 6f));
            CreateChromeRail(corral.transform, "Rail_Back", new Vector3(0f, 0.45f, -3f), new Vector3(4.1f, 0.9f, 0.1f));

            // Player Spawn Point
            GameObject playerSpawn = new GameObject("SpawnPoint_Player");
            playerSpawn.transform.SetParent(corral.transform, false);
            playerSpawn.transform.localPosition = new Vector3(0f, 0.1f, 1.5f);
            playerSpawn.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            CartSpawnPoint pComp = playerSpawn.AddComponent<CartSpawnPoint>();
            pComp.Initialize(true, 0);

            // Rival 1 (Next to corral)
            GameObject rival1 = new GameObject("SpawnPoint_Rival_01");
            rival1.transform.SetParent(parent, false);
            rival1.transform.position = corralPos + new Vector3(4.5f, 0.1f, 1.5f);
            rival1.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            rival1.AddComponent<CartSpawnPoint>().Initialize(false, 1);

            // Rival 2 (Opposite entrance corner)
            GameObject rival2 = new GameObject("SpawnPoint_Rival_02");
            rival2.transform.SetParent(parent, false);
            rival2.transform.position = new Vector3(config.storeWidth * 0.35f, 0.1f, -config.storeLength * 0.42f);
            rival2.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
            rival2.AddComponent<CartSpawnPoint>().Initialize(false, 2);

            // Rival 3 (Near checkout exit)
            GameObject rival3 = new GameObject("SpawnPoint_Rival_03");
            rival3.transform.SetParent(parent, false);
            rival3.transform.position = new Vector3(0f, 0.1f, config.checkoutZPosition + 3.5f);
            rival3.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            rival3.AddComponent<CartSpawnPoint>().Initialize(false, 3);
        }

        private void CreateChromeRail(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = name;
            rail.transform.SetParent(parent, false);
            rail.transform.localPosition = pos;
            rail.transform.localScale = scale;
            rail.GetComponent<Renderer>().sharedMaterial = metalChromeMat;
        }

        #endregion

        #region 4. Gondola Aisles

        private void BuildAisles(Transform parent)
        {
            int count = config.aisleCount;
            float spacing = config.aisleSpacing;
            float startX = -((count - 1) * spacing) * 0.5f;

            string[] categoryNames = new string[]
            {
                "Snacks & Cereal",
                "Canned Goods & Pasta",
                "Beverages & Soda",
                "Household & Cleaning"
            };

            Color[] themeColors = new Color[]
            {
                new Color(0.95f, 0.35f, 0.1f),
                new Color(0.2f, 0.6f, 0.85f),
                new Color(0.1f, 0.7f, 0.4f),
                new Color(0.6f, 0.3f, 0.8f)
            };

            for (int i = 0; i < count; i++)
            {
                float x = startX + i * spacing;
                string catName = (i < categoryNames.Length) ? categoryNames[i] : $"Grocery Aisle {i + 1}";
                Color theme = (i < themeColors.Length) ? themeColors[i] : Color.cyan;

                Vector3 aislePos = new Vector3(x, 0f, 4f);
                CreateAisle(parent, i + 1, catName, theme, aislePos);
            }
        }

        private void CreateAisle(Transform parent, int aisleNum, string category, Color themeColor, Vector3 pos)
        {
            GameObject aisle = new GameObject($"Aisle_{aisleNum:D2}_{category.Replace(" ", "")}");
            aisle.transform.SetParent(parent, false);
            aisle.transform.position = pos;

            StoreAisle comp = aisle.AddComponent<StoreAisle>();
            comp.Initialize(aisleNum, category, themeColor);

            float len = config.aisleLength;
            float width = config.shelvingWidth;
            float height = config.shelfHeight;

            // Center partition / backboard
            GameObject backboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backboard.name = "CentralPartitionBack";
            backboard.transform.SetParent(aisle.transform, false);
            backboard.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            backboard.transform.localScale = new Vector3(0.12f, height, len);
            backboard.GetComponent<Renderer>().sharedMaterial = shelfBackMat;

            // Left & Right Shelves (Tiers)
            float tierStep = (height - 0.4f) / config.shelfTiers;
            for (int t = 0; t < config.shelfTiers; t++)
            {
                float y = 0.3f + t * tierStep;

                // Left shelf tier
                GameObject shelfL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelfL.name = $"Shelf_Left_Tier_{t + 1}";
                shelfL.transform.SetParent(aisle.transform, false);
                shelfL.transform.localPosition = new Vector3(-width * 0.25f, y, 0f);
                shelfL.transform.localScale = new Vector3(width * 0.5f, 0.06f, len);
                shelfL.GetComponent<Renderer>().sharedMaterial = shelfMat;

                // Right shelf tier
                GameObject shelfR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelfR.name = $"Shelf_Right_Tier_{t + 1}";
                shelfR.transform.SetParent(aisle.transform, false);
                shelfR.transform.localPosition = new Vector3(width * 0.25f, y, 0f);
                shelfR.transform.localScale = new Vector3(width * 0.5f, 0.06f, len);
                shelfR.GetComponent<Renderer>().sharedMaterial = shelfMat;

                // Stock items on shelves
                if (config.populateShelves)
                {
                    PopulateShelfItems(comp, shelfL.transform, aisleNum, true);
                    PopulateShelfItems(comp, shelfR.transform, aisleNum, false);
                }
            }

            // Endcaps (Featured displays at both ends of aisle)
            CreateEndcap(aisle.transform, "Endcap_Front", new Vector3(0f, 0f, -len * 0.5f - 0.7f), width, height);
            CreateEndcap(aisle.transform, "Endcap_Back", new Vector3(0f, 0f, len * 0.5f + 0.7f), width, height);

            // Overhead Hanging Aisle Sign
            CreateAisleSign(aisle.transform, aisleNum, category, themeColor, new Vector3(0f, config.wallHeight - 1.2f, 0f));
        }

        private void CreateEndcap(Transform parent, string name, Vector3 pos, float width, float height)
        {
            GameObject endcap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            endcap.name = name;
            endcap.transform.SetParent(parent, false);
            endcap.transform.localPosition = pos + Vector3.up * (height * 0.5f);
            endcap.transform.localScale = new Vector3(width, height, 1.2f);
            endcap.GetComponent<Renderer>().sharedMaterial = shelfMat;
        }

        private void CreateAisleSign(Transform parent, int num, string title, Color themeColor, Vector3 pos)
        {
            GameObject sign = new GameObject($"Sign_Aisle_{num:D2}");
            sign.transform.SetParent(parent, false);
            sign.transform.position = parent.position + pos;

            // Signboard frame
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board";
            board.transform.SetParent(sign.transform, false);
            board.transform.localScale = new Vector3(2.4f, 0.8f, 0.1f);
            board.GetComponent<Renderer>().sharedMaterial = signMat;
            DestroyImmediate(board.GetComponent<Collider>());

            // Two support hanging cables
            GameObject cableL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cableL.transform.SetParent(sign.transform, false);
            cableL.transform.localPosition = new Vector3(-0.9f, 0.6f, 0f);
            cableL.transform.localScale = new Vector3(0.02f, 0.6f, 0.02f);
            cableL.GetComponent<Renderer>().sharedMaterial = metalChromeMat;
            DestroyImmediate(cableL.GetComponent<Collider>());

            GameObject cableR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cableR.transform.SetParent(sign.transform, false);
            cableR.transform.localPosition = new Vector3(0.9f, 0.6f, 0f);
            cableR.transform.localScale = new Vector3(0.02f, 0.6f, 0.02f);
            cableR.GetComponent<Renderer>().sharedMaterial = metalChromeMat;
            DestroyImmediate(cableR.GetComponent<Collider>());
        }

        private void PopulateShelfItems(StoreAisle aisle, Transform shelf, int aisleNum, bool isLeft)
        {
            int itemCount = config.itemsPerShelfTier;
            float stepZ = config.aisleLength / (itemCount + 1);
            float startZ = -config.aisleLength * 0.5f + stepZ;

            for (int i = 0; i < itemCount; i++)
            {
                float z = startZ + i * stepZ;
                Vector3 itemPos = shelf.TransformPoint(new Vector3(0f, 0.05f, z));
                Quaternion itemRot = Quaternion.identity;
                GroceryItemType itemType = GroceryItemType.CerealBox;

                switch (aisleNum)
                {
                    case 1:
                        // Snacks & Cereal: alternating Chips and Cereal boxes
                        itemType = (i % 2 == 0) ? GroceryItemType.Chips : GroceryItemType.CerealBox;
                        itemRot = Quaternion.Euler(0f, (i % 2 == 0) ? 15f : 0f, 0f);
                        break;
                    case 2:
                        // Canned goods & soups
                        itemType = GroceryItemType.SodaCan;
                        break;
                    case 3:
                        // Beverages: Soda Cans and Soda Bottles
                        itemType = (i % 2 == 0) ? GroceryItemType.SodaCan : GroceryItemType.SodaBottle;
                        break;
                    case 4:
                        // Household & misc
                        itemType = (i % 2 == 0) ? GroceryItemType.SodaBottle : GroceryItemType.CerealBox;
                        break;
                }

                GameObject itemObj = GroceryItemFactory.CreateItem(itemType, itemPos, itemRot, shelf);
                Rigidbody rb = itemObj.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                GroceryItem gItem = itemObj.GetComponent<GroceryItem>();
                if (gItem != null && aisle != null) aisle.RegisterItem(gItem);
            }
        }

        #endregion

        #region 5. Departments (Produce, Deli, Freezer)

        private void BuildDepartments(Transform parent)
        {
            BuildProduceSection(parent);
            BuildBakeryDeliSection(parent);
            BuildFreezerWall(parent);
        }

        private void BuildProduceSection(Transform parent)
        {
            GameObject produceRoot = new GameObject("Dept_FreshProduce");
            produceRoot.transform.SetParent(parent, false);
            produceRoot.transform.position = new Vector3(config.storeWidth * 0.36f, 0f, 4f);

            // 3 Produce Stand Display Bins
            for (int i = -1; i <= 1; i++)
            {
                Vector3 tablePos = new Vector3(0f, 0f, i * 6f);
                CreateProduceTable(produceRoot.transform, $"ProduceTable_{i + 2}", tablePos, i);
            }
        }

        private void CreateProduceTable(Transform parent, string name, Vector3 localPos, int typeIndex)
        {
            GameObject table = new GameObject(name);
            table.transform.SetParent(parent, false);
            table.transform.localPosition = localPos;

            // Wooden base
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "TableBase";
            baseObj.transform.SetParent(table.transform, false);
            baseObj.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            baseObj.transform.localScale = new Vector3(3.2f, 0.9f, 4.0f);
            baseObj.GetComponent<Renderer>().sharedMaterial = woodMat;

            // Slanted display top
            GameObject displayBed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            displayBed.name = "DisplayBed";
            displayBed.transform.SetParent(table.transform, false);
            displayBed.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            displayBed.transform.localScale = new Vector3(3.0f, 0.2f, 3.8f);
            displayBed.GetComponent<Renderer>().sharedMaterial = woodMat;

            // Fruit piles on table: -1 = Apples, 0 = Melons, 1 = Bananas
            GroceryItemType fruitType = (typeIndex == -1)
                ? GroceryItemType.Apple
                : (typeIndex == 0 ? GroceryItemType.Watermelon : GroceryItemType.Banana);

            for (int fx = -1; fx <= 1; fx++)
            {
                for (int fz = -1; fz <= 1; fz++)
                {
                    Vector3 fruitLocalPos = new Vector3(fx * 0.75f, 1.22f, fz * 0.95f);
                    Vector3 fruitWorldPos = table.transform.TransformPoint(fruitLocalPos);
                    Quaternion fruitRot = (fruitType == GroceryItemType.Banana)
                        ? Quaternion.Euler(0f, Random.Range(-45f, 45f), 15f)
                        : Quaternion.identity;

                    GameObject fruitObj = GroceryItemFactory.CreateItem(fruitType, fruitWorldPos, fruitRot, table.transform);
                    Rigidbody rb = fruitObj.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;
                }
            }
        }

        private void BuildBakeryDeliSection(Transform parent)
        {
            GameObject deliRoot = new GameObject("Dept_BakeryDeli");
            deliRoot.transform.SetParent(parent, false);
            deliRoot.transform.position = new Vector3(-config.storeWidth * 0.38f, 0f, 4f);

            // Long Service Counter
            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "ServiceCounter";
            counter.transform.SetParent(deliRoot.transform, false);
            counter.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            counter.transform.localScale = new Vector3(2.4f, 1.1f, 18f);
            counter.GetComponent<Renderer>().sharedMaterial = checkoutMat;

            // Glass display casing along counter
            GameObject glassCase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassCase.name = "GlassShowcase";
            glassCase.transform.SetParent(deliRoot.transform, false);
            glassCase.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            glassCase.transform.localScale = new Vector3(2.2f, 0.7f, 17f);
            glassCase.GetComponent<Renderer>().sharedMaterial = glassMat;

            // Hanging Department Sign
            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "BakerySign";
            sign.transform.SetParent(deliRoot.transform, false);
            sign.transform.localPosition = new Vector3(0f, config.wallHeight - 1.2f, 0f);
            sign.transform.localScale = new Vector3(1.8f, 0.8f, 12f);
            sign.GetComponent<Renderer>().sharedMaterial = signMat;

            // Seafood display: Fresh Cartoon Fish on counter!
            for (int i = -3; i <= -1; i++)
            {
                Vector3 fishPos = deliRoot.transform.position + new Vector3(0.35f, 1.15f, i * 2.2f);
                Quaternion fishRot = Quaternion.Euler(0f, 90f + Random.Range(-20f, 20f), 0f);
                GameObject fishObj = GroceryItemFactory.CreateItem(GroceryItemType.Fish, fishPos, fishRot, deliRoot.transform);
                Rigidbody rb = fishObj.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
            }

            // Bakery display: Crusty French Baguettes on counter!
            for (int i = 1; i <= 3; i++)
            {
                Vector3 breadPos = deliRoot.transform.position + new Vector3(0.35f, 1.15f, i * 2.2f);
                Quaternion breadRot = Quaternion.Euler(0f, Random.Range(-15f, 15f), 10f);
                GameObject breadObj = GroceryItemFactory.CreateItem(GroceryItemType.Baguette, breadPos, breadRot, deliRoot.transform);
                Rigidbody rb = breadObj.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
            }
        }

        private void BuildFreezerWall(Transform parent)
        {
            GameObject freezerRoot = new GameObject("Dept_FrozenDairyWall");
            freezerRoot.transform.SetParent(parent, false);
            float backZ = config.storeLength * 0.44f;
            freezerRoot.transform.position = new Vector3(0f, 0f, backZ);

            int coolerUnits = 6;
            float unitWidth = 4.2f;
            float startX = -((coolerUnits - 1) * unitWidth) * 0.5f;

            for (int i = 0; i < coolerUnits; i++)
            {
                float x = startX + i * unitWidth;
                CreateCoolerUnit(freezerRoot.transform, $"CoolerUnit_{i + 1:D2}", new Vector3(x, 0f, 0f));
            }
        }

        private void CreateCoolerUnit(Transform parent, string name, Vector3 localPos)
        {
            GameObject cooler = new GameObject(name);
            cooler.transform.SetParent(parent, false);
            cooler.transform.localPosition = localPos;

            // Frame cabinet
            GameObject cabinet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabinet.name = "CabinetFrame";
            cabinet.transform.SetParent(cooler.transform, false);
            cabinet.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            cabinet.transform.localScale = new Vector3(4.0f, 3.6f, 1.6f);
            cabinet.GetComponent<Renderer>().sharedMaterial = shelfBackMat;

            // Glass door panels (Left & Right)
            GameObject glassDoorL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassDoorL.name = "Door_Left";
            glassDoorL.transform.SetParent(cooler.transform, false);
            glassDoorL.transform.localPosition = new Vector3(-0.95f, 1.8f, -0.82f);
            glassDoorL.transform.localScale = new Vector3(1.85f, 3.2f, 0.08f);
            glassDoorL.GetComponent<Renderer>().sharedMaterial = glassMat;

            GameObject glassDoorR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glassDoorR.name = "Door_Right";
            glassDoorR.transform.SetParent(cooler.transform, false);
            glassDoorR.transform.localPosition = new Vector3(0.95f, 1.8f, -0.82f);
            glassDoorR.transform.localScale = new Vector3(1.85f, 3.2f, 0.08f);
            glassDoorR.GetComponent<Renderer>().sharedMaterial = glassMat;

            // Internal cooler light
            GameObject cLightObj = new GameObject("CoolerInteriorLight");
            cLightObj.transform.SetParent(cooler.transform, false);
            cLightObj.transform.localPosition = new Vector3(0f, 2.5f, -0.4f);
            Light cLight = cLightObj.AddComponent<Light>();
            cLight.type = LightType.Point;
            cLight.color = new Color(0.7f, 0.9f, 1f);
            cLight.range = 5f;
            cLight.intensity = 8f;
        }

        #endregion

        #region 6. Hazards & Combat Props

        private void BuildHazards(Transform parent)
        {
            // Caution Wet Floor signs
            Vector3[] signPositions = new Vector3[]
            {
                new Vector3(-6f, 0f, -8f),
                new Vector3(6f, 0f, 18f),
                new Vector3(0f, 0f, 5f)
            };

            for (int i = 0; i < signPositions.Length; i++)
            {
                CreateCautionSign(parent, $"CautionSign_{i + 1}", signPositions[i]);
            }

            // Banana slip hazards
            Vector3[] bananaPositions = new Vector3[]
            {
                new Vector3(-2.8f, 0.05f, -2f),
                new Vector3(2.8f, 0.05f, 10f),
                new Vector3(-8.5f, 0.05f, 12f),
                new Vector3(8.5f, 0.05f, -4f)
            };

            for (int i = 0; i < bananaPositions.Length; i++)
            {
                CreateBananaHazard(parent, $"BananaSlip_{i + 1}", bananaPositions[i]);
            }

            // Restocking wooden pallets with boxes
            CreateRestockPallet(parent, "RestockPallet_01", new Vector3(10f, 0f, -10f));
            CreateRestockPallet(parent, "RestockPallet_02", new Vector3(-10f, 0f, 16f));
        }

        private void CreateCautionSign(Transform parent, string name, Vector3 pos)
        {
            GameObject sign = new GameObject(name);
            sign.transform.SetParent(parent, false);
            sign.transform.position = pos;

            // A-frame leaning boards
            GameObject sideA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideA.name = "BoardA";
            sideA.transform.SetParent(sign.transform, false);
            sideA.transform.localPosition = new Vector3(0f, 0.45f, -0.15f);
            sideA.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
            sideA.transform.localScale = new Vector3(0.45f, 0.9f, 0.06f);
            sideA.GetComponent<Renderer>().sharedMaterial = cautionMat;

            GameObject sideB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sideB.name = "BoardB";
            sideB.transform.SetParent(sign.transform, false);
            sideB.transform.localPosition = new Vector3(0f, 0.45f, 0.15f);
            sideB.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            sideB.transform.localScale = new Vector3(0.45f, 0.9f, 0.06f);
            sideB.GetComponent<Renderer>().sharedMaterial = cautionMat;

            sign.AddComponent<BoxCollider>().size = new Vector3(0.6f, 1f, 0.6f);
            sign.AddComponent<Rigidbody>().mass = 5f;
        }

        private void CreateBananaHazard(Transform parent, string name, Vector3 pos)
        {
            GameObject banana = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            banana.name = name;
            banana.transform.SetParent(parent, false);
            banana.transform.position = pos;
            banana.transform.localScale = new Vector3(0.45f, 0.1f, 0.25f);
            banana.GetComponent<Renderer>().sharedMaterial = bananaMat;

            SpillHazard hazard = banana.AddComponent<SpillHazard>();
            hazard.Initialize(HazardType.BananaPeel);
        }

        private void CreateRestockPallet(Transform parent, string name, Vector3 pos)
        {
            GameObject pallet = new GameObject(name);
            pallet.transform.SetParent(parent, false);
            pallet.transform.position = pos;

            // Wooden pallet base
            GameObject woodBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            woodBase.name = "PalletWood";
            woodBase.transform.SetParent(pallet.transform, false);
            woodBase.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            woodBase.transform.localScale = new Vector3(2.2f, 0.2f, 2.2f);
            woodBase.GetComponent<Renderer>().sharedMaterial = woodMat;

            // Stacked Cardboard Shipping Boxes
            for (int bx = 0; bx < 2; bx++)
            {
                for (int bz = 0; bz < 2; bz++)
                {
                    GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    box.name = $"CardboardBox_{bx}_{bz}";
                    box.transform.SetParent(pallet.transform, false);
                    box.transform.localPosition = new Vector3((bx - 0.5f) * 0.95f, 0.7f, (bz - 0.5f) * 0.95f);
                    box.transform.localScale = Vector3.one * 0.9f;
                    box.GetComponent<Renderer>().sharedMaterial = woodMat;
                }
            }
        }

        #endregion
    }
}
