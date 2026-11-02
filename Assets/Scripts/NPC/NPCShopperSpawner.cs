using System.Collections.Generic;
using UnityEngine;
using CartCombat.Store;

namespace CartCombat.NPC
{
    /// <summary>
    /// Builds cartoon-style NPC shoppers that look like real humans (chibi proportions)
    /// and spawns them in the AISLE CORRIDORS of the grocery store — never inside shelves.
    ///
    /// Each NPC gets a set of aisle-corridor waypoints so they can navigate the store.
    ///
    /// The store layout (from GroceryStoreConfig) is:
    ///   • aisleCount  aisles spaced config.aisleSpacing apart, centred on X=0
    ///   • each aisle is a solid gondola at position x → corridor is between aisles
    ///   • aisles run along Z from -(aisleLength/2)+4 to +(aisleLength/2)+4
    /// 
    /// Shelf collider fix:
    ///   In Awake, this spawner ensures every Cube primitive inside the
    ///   "Aisles" hierarchy that represents a shelf tier or backboard has a
    ///   Box Collider that is correctly sized, so NPCs and the player cannot
    ///   walk through shelving.
    /// </summary>
    public class NPCShopperSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private int   npcCount     = 5;
        [SerializeField] private bool  spawnOnStart = true;

        [Header("Store Layout Reference (auto-read from GroceryStoreConfig)")]
        [SerializeField] private GroceryStoreBuilder storeBuilder;

        // Cached aisle corridor walk points built from config
        private List<Vector3> aisleCorridorWaypoints = new List<Vector3>();

        // NPC colour palettes
        private static readonly Color[] JacketColors = {
            new Color(0.88f, 0.22f, 0.18f),  // Red
            new Color(0.18f, 0.50f, 0.88f),  // Blue
            new Color(0.20f, 0.72f, 0.38f),  // Green
            new Color(0.90f, 0.68f, 0.08f),  // Yellow
            new Color(0.62f, 0.18f, 0.88f),  // Purple
            new Color(0.92f, 0.48f, 0.10f),  // Orange
        };

        private static readonly Color[] PantsColors = {
            new Color(0.14f, 0.16f, 0.22f),
            new Color(0.22f, 0.34f, 0.50f),
            new Color(0.48f, 0.32f, 0.18f),
            new Color(0.18f, 0.18f, 0.18f),
        };

        private static readonly Color[] SkinColors = {
            new Color(0.97f, 0.82f, 0.67f),
            new Color(0.88f, 0.68f, 0.52f),
            new Color(0.72f, 0.52f, 0.38f),
            new Color(0.48f, 0.34f, 0.26f),
            new Color(0.62f, 0.44f, 0.34f),
        };

        private static readonly Color[] HairColors = {
            new Color(0.14f, 0.09f, 0.06f),
            new Color(0.82f, 0.68f, 0.28f),
            new Color(0.08f, 0.06f, 0.06f),
            new Color(0.68f, 0.22f, 0.12f),
            new Color(0.90f, 0.88f, 0.85f),
        };

        private static readonly Color[] ShoeColors = {
            new Color(0.12f, 0.12f, 0.14f),
            new Color(0.85f, 0.22f, 0.18f),
            new Color(0.88f, 0.88f, 0.90f),
        };

        // ── Unity ────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Ensure shelf colliders are solid
            EnsureShelfColliders();
        }

        private void Start()
        {
            BuildAisleWaypoints();
            if (spawnOnStart) SpawnAll();
        }

        // ── Public ───────────────────────────────────────────────────────────

        public void SpawnAll()
        {
            if (aisleCorridorWaypoints.Count == 0) BuildAisleWaypoints();

            // Check if NPCs already exist in the scene so we don't duplicate
            NPCShopperAI[] existing = GetComponentsInChildren<NPCShopperAI>();
            if (existing != null && existing.Length >= npcCount)
            {
                foreach (NPCShopperAI ai in existing)
                {
                    if (ai != null && (ai.AisleWaypoints == null || ai.AisleWaypoints.Count == 0))
                    {
                        ai.AisleWaypoints = new List<Vector3>(aisleCorridorWaypoints);
                    }
                }
                return;
            }

            Transform container = transform.Find("Customers");
            if (container == null) container = transform.Find("Shoppers");
            if (container == null)
            {
                GameObject cObj = new GameObject("Customers");
                cObj.transform.SetParent(transform, false);
                container = cObj.transform;
            }
            else
            {
                container.name = "Customers";
            }

            for (int i = 0; i < npcCount; i++)
            {
                // Distribute across corridors evenly
                Vector3 spawnPos = PickSpawnPosition(i);
                GameObject npc = BuildNPC(i, spawnPos);
                if (npc != null)
                {
                    npc.transform.SetParent(container, true);
                }
            }
        }

        public void ClearSpawnedNPCs()
        {
            Transform container = transform.Find("Shoppers");
            if (container != null)
            {
                if (Application.isPlaying) Destroy(container.gameObject);
                else DestroyImmediate(container.gameObject);
            }

            NPCShopperAI[] all = Object.FindObjectsByType<NPCShopperAI>(FindObjectsSortMode.None);
            foreach (NPCShopperAI ai in all)
            {
                if (ai != null)
                {
                    if (Application.isPlaying) Destroy(ai.gameObject);
                    else DestroyImmediate(ai.gameObject);
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Spawn Shoppers in Scene")]
        public void SpawnShoppersInEditor()
        {
            EnsureShelfColliders();
            BuildAisleWaypoints();
            SpawnAll();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        [ContextMenu("Clear Shoppers from Scene")]
        public void ClearShoppersInEditor()
        {
            ClearSpawnedNPCs();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif

        // ── Shelf Collider Fix ────────────────────────────────────────────────

        /// <summary>
        /// Walk through all shelf/backboard GameObjects and guarantee they have
        /// properly-scaled BoxColliders so NPCs can't walk through them.
        /// Also adds an "Aisle" layer tag so AI knows to avoid them.
        /// </summary>
        private void EnsureShelfColliders()
        {
            // Find the Aisles group
            Transform aislesRoot = null;
            GroceryStoreBuilder builder = storeBuilder != null
                ? storeBuilder : Object.FindFirstObjectByType<GroceryStoreBuilder>();

            if (builder != null)
            {
                foreach (Transform child in builder.transform)
                {
                    if (child.name == "Aisles") { aislesRoot = child; break; }
                }
            }

            if (aislesRoot == null)
            {
                // Try finding by name in scene root
                GameObject aislesGO = GameObject.Find("Aisles");
                if (aislesGO != null) aislesRoot = aislesGO.transform;
            }

            if (aislesRoot == null)
            {
                return;
            }

            int fixed_ = 0;
            // Every Renderer-bearing child that is part of a shelf structure → ensure it has a BoxCollider
            Renderer[] allRenderers = aislesRoot.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in allRenderers)
            {
                // Do not add extra colliders to grocery items or visual props
                if (r.GetComponentInParent<CartCombat.Store.GroceryItem>() != null) continue;

                GameObject go = r.gameObject;
                if (go.GetComponent<Collider>() == null)
                {
                    go.AddComponent<BoxCollider>();
                    fixed_++;
                }
                // Make sure it's not a trigger
                Collider col = go.GetComponent<Collider>();
                if (col != null) col.isTrigger = false;
            }

            // Also add a central partition solid blocker
            // matching only the central spine/backboard (width 0.20f) — prevents NPCs clipping through back panel
            // while leaving the open shelf tiers unblocked for items and player interaction
            foreach (Transform aisle in aislesRoot)
            {
                if (!aisle.name.StartsWith("Aisle_")) continue;

                Rigidbody aisleRB = aisle.GetComponent<Rigidbody>();
                if (aisleRB == null) { /* static — good */ }

                // Central partition solid blocker (thickness 0.20m, height 2.6m, length 22m)
                BoxCollider blocker = aisle.GetComponent<BoxCollider>();
                if (blocker == null) blocker = aisle.gameObject.AddComponent<BoxCollider>();
                blocker.center = new Vector3(0f, 1.3f, 0f);
                blocker.size   = new Vector3(0.20f, 2.6f, 22f);
                blocker.isTrigger = false;
                fixed_++;
            }
        }

        // ── Waypoint builder ─────────────────────────────────────────────────

        /// <summary>
        /// Generates a grid of safe walk positions in the CORRIDORS between aisles,
        /// plus along the perimeter walkways, based on the store config.
        /// </summary>
        private void BuildAisleWaypoints()
        {
            aisleCorridorWaypoints.Clear();

            GroceryStoreBuilder builder = storeBuilder != null
                ? storeBuilder : Object.FindFirstObjectByType<GroceryStoreBuilder>();

            // Defaults that match GroceryStoreConfig
            int   aisleCount   = 4;
            float aisleSpacing = 5.5f;
            float aisleLength  = 22f;
            float storeWidth   = 36f;
            float storeLength  = 48f;
            float aisleStartZ  = 4f;  // aisles centre-Z from BuildAisles()

            if (builder != null)
            {
                GroceryStoreConfig cfg = builder.Config;
                aisleCount   = cfg.aisleCount;
                aisleSpacing = cfg.aisleSpacing;
                aisleLength  = cfg.aisleLength;
                storeWidth   = cfg.storeWidth;
                storeLength  = cfg.storeLength;
            }

            float startX  = -((aisleCount - 1) * aisleSpacing) * 0.5f;
            float halfLen = aisleLength * 0.5f;

            // CORRIDOR WAYPOINTS: between every pair of adjacent aisles + outer corridors
            // Corridor X positions = midpoints between aisle X-centres
            List<float> corridorX = new List<float>();

            // Left outer corridor
            corridorX.Add(startX - aisleSpacing * 0.5f);

            // Between aisles
            for (int i = 0; i < aisleCount - 1; i++)
            {
                float x1 = startX + i * aisleSpacing;
                float x2 = startX + (i + 1) * aisleSpacing;
                corridorX.Add((x1 + x2) * 0.5f);
            }

            // Right outer corridor
            corridorX.Add(startX + (aisleCount - 1) * aisleSpacing + aisleSpacing * 0.5f);

            // For each corridor X, add waypoints along the Z length of the aisle
            int zSteps = 6;
            for (int ci = 0; ci < corridorX.Count; ci++)
            {
                float cx = Mathf.Clamp(corridorX[ci], -storeWidth * 0.45f, storeWidth * 0.45f);
                for (int z = 0; z <= zSteps; z++)
                {
                    float frac = (float)z / zSteps;
                    float cz   = Mathf.Lerp(aisleStartZ - halfLen - 1f,
                                            aisleStartZ + halfLen + 1f, frac);
                    cz = Mathf.Clamp(cz, -storeLength * 0.44f, storeLength * 0.44f);
                    aisleCorridorWaypoints.Add(new Vector3(cx, 0.05f, cz));
                }
            }

            // Front perimeter walk (near entrance / checkout)
            for (int s = 0; s < 5; s++)
            {
                float frac = (float)s / 4f;
                float px = Mathf.Lerp(-storeWidth * 0.4f, storeWidth * 0.4f, frac);
                aisleCorridorWaypoints.Add(new Vector3(px, 0.05f, -storeLength * 0.35f));
            }

            // Back perimeter walk
            for (int s = 0; s < 5; s++)
            {
                float frac = (float)s / 4f;
                float px = Mathf.Lerp(-storeWidth * 0.4f, storeWidth * 0.4f, frac);
                aisleCorridorWaypoints.Add(new Vector3(px, 0.05f, storeLength * 0.35f));
            }
        }

        private Vector3 PickSpawnPosition(int index)
        {
            if (aisleCorridorWaypoints.Count == 0)
                return transform.position + new Vector3(index * 2f, 0.05f, 0f);

            // Distribute across waypoints
            int wpi = (index * (aisleCorridorWaypoints.Count / Mathf.Max(1, npcCount) + 1))
                      % aisleCorridorWaypoints.Count;
            Vector3 wp = aisleCorridorWaypoints[wpi];
            // Small random offset so NPCs don't stack
            wp += new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.4f, 0.4f));
            return wp;
        }

        // ── NPC Builder ──────────────────────────────────────────────────────

        private GameObject BuildNPC(int index, Vector3 position)
        {
            Color jacket = JacketColors[index % JacketColors.Length];
            Color pants  = PantsColors [index % PantsColors.Length];
            Color skin   = SkinColors  [index % SkinColors.Length];
            Color hair   = HairColors  [index % HairColors.Length];
            Color shoes  = ShoeColors  [index % ShoeColors.Length];

            // ── Root ──────────────────────────────────────────────────────
            GameObject root = new GameObject($"Customer_{index + 1:D2}");
            root.tag = "NPC";
            // Place on floor — CharacterController.center.y = height/2 → bottom at 0
            root.transform.position = new Vector3(position.x, 0f, position.z);
            root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height     = 1.75f;
            cc.radius     = 0.32f;
            cc.center     = new Vector3(0f, 0.875f, 0f);  // bottom of CC at Y=0
            cc.skinWidth  = 0.04f;
            cc.stepOffset = 0.38f;
            cc.slopeLimit = 45f;

            // Dedicated physical BodyCollider so player cart physically bumps and blocks against customer
            GameObject colChild = new GameObject("BodyCollider");
            colChild.transform.SetParent(root.transform, false);
            colChild.transform.localPosition = Vector3.zero;
            CapsuleCollider capCol = colChild.AddComponent<CapsuleCollider>();
            capCol.height = 1.75f;
            capCol.radius = 0.42f;
            capCol.center = new Vector3(0f, 0.875f, 0f);

            // ── Visual root ───────────────────────────────────────────────
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;

            // ─────────────────────────────────────────────────────────────
            // HUMAN-PROPORTIONED CARTOON BODY
            // All Y positions are LOCAL from the NPC root (floor = 0)
            // Height breakdown:
            //   0.00 → 0.12  : Shoes
            //   0.12 → 0.95  : Legs / Pants
            //   0.95 → 1.55  : Torso / Jacket
            //   1.55 → 1.95  : Neck + Head
            //   1.95 → 2.10  : Hair
            // ─────────────────────────────────────────────────────────────

            // ── Feet / Shoes ──────────────────────────────────────────────
            MakePrim(visual.transform, PrimitiveType.Cube, "Shoe_L",
                new Vector3(-0.115f, 0.06f, 0.06f), new Vector3(0.18f, 0.12f, 0.28f), shoes);
            MakePrim(visual.transform, PrimitiveType.Cube, "Shoe_R",
                new Vector3( 0.115f, 0.06f, 0.06f), new Vector3(0.18f, 0.12f, 0.28f), shoes);

            // ── Lower Legs / Calves ───────────────────────────────────────
            MakePrim(visual.transform, PrimitiveType.Cube, "Calf_L",
                new Vector3(-0.115f, 0.38f, 0f), new Vector3(0.155f, 0.50f, 0.155f), pants);
            MakePrim(visual.transform, PrimitiveType.Cube, "Calf_R",
                new Vector3( 0.115f, 0.38f, 0f), new Vector3(0.155f, 0.50f, 0.155f), pants);

            // ── Upper Legs / Thighs ───────────────────────────────────────
            MakePrim(visual.transform, PrimitiveType.Cube, "Thigh_L",
                new Vector3(-0.105f, 0.73f, 0f), new Vector3(0.18f, 0.28f, 0.18f), pants);
            MakePrim(visual.transform, PrimitiveType.Cube, "Thigh_R",
                new Vector3( 0.105f, 0.73f, 0f), new Vector3(0.18f, 0.28f, 0.18f), pants);

            // ── Belt ──────────────────────────────────────────────────────
            Color beltCol = new Color(0.22f, 0.16f, 0.10f);
            MakePrim(visual.transform, PrimitiveType.Cube, "Belt",
                new Vector3(0f, 0.92f, 0f), new Vector3(0.42f, 0.055f, 0.22f), beltCol);

            // ── Torso ─────────────────────────────────────────────────────
            MakePrim(visual.transform, PrimitiveType.Cube, "Torso",
                new Vector3(0f, 1.22f, 0f), new Vector3(0.44f, 0.56f, 0.24f), jacket);

            // Shirt / collar peek
            MakePrim(visual.transform, PrimitiveType.Cube, "Collar",
                new Vector3(0f, 1.48f, -0.10f), new Vector3(0.20f, 0.09f, 0.06f),
                new Color(0.95f, 0.95f, 0.95f));

            // ── Upper Arms ────────────────────────────────────────────────
            MakePrim(visual.transform, PrimitiveType.Cube, "UpperArm_L",
                new Vector3(-0.295f, 1.25f, 0f), new Vector3(0.155f, 0.34f, 0.155f), jacket);
            MakePrim(visual.transform, PrimitiveType.Cube, "UpperArm_R",
                new Vector3( 0.295f, 1.25f, 0f), new Vector3(0.155f, 0.34f, 0.155f), jacket);

            // ── Forearms ──────────────────────────────────────────────────
            MakePrim(visual.transform, PrimitiveType.Cube, "Forearm_L",
                new Vector3(-0.305f, 0.98f, 0f), new Vector3(0.13f, 0.28f, 0.13f), skin);
            MakePrim(visual.transform, PrimitiveType.Cube, "Forearm_R",
                new Vector3( 0.305f, 0.98f, 0f), new Vector3(0.13f, 0.28f, 0.13f), skin);

            // ── Hands ────────────────────────────────────────────────────
            MakePrim(visual.transform, PrimitiveType.Sphere, "Hand_L",
                new Vector3(-0.31f, 0.825f, 0f), new Vector3(0.12f, 0.12f, 0.10f), skin);
            MakePrim(visual.transform, PrimitiveType.Sphere, "Hand_R",
                new Vector3( 0.31f, 0.825f, 0f), new Vector3(0.12f, 0.12f, 0.10f), skin);

            // ── Neck ──────────────────────────────────────────────────────
            MakePrim(visual.transform, PrimitiveType.Cylinder, "Neck",
                new Vector3(0f, 1.575f, 0f), new Vector3(0.10f, 0.09f, 0.10f), skin);

            // ── HEAD ──────────────────────────────────────────────────────
            GameObject head = new GameObject("Head");
            head.transform.SetParent(visual.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.75f, 0f);

            // Head sphere (slightly squashed for cartoon look)
            MakePrim(head.transform, PrimitiveType.Sphere, "HeadMesh",
                Vector3.zero, new Vector3(0.33f, 0.36f, 0.32f), skin);

            // Hair / top of head
            MakePrim(head.transform, PrimitiveType.Sphere, "HairTop",
                new Vector3(0f, 0.13f, -0.01f), new Vector3(0.355f, 0.22f, 0.345f), hair);
            // Hair sides / fringe
            MakePrim(head.transform, PrimitiveType.Cube, "HairFringe",
                new Vector3(0f, 0.14f, -0.155f), new Vector3(0.30f, 0.10f, 0.08f), hair);

            // ── Eyes (big cartoon eyes) ───────────────────────────────────
            // Whites
            MakePrim(head.transform, PrimitiveType.Sphere, "Eye_L_White",
                new Vector3(-0.096f, 0.028f, -0.152f), new Vector3(0.105f, 0.10f, 0.06f), Color.white);
            MakePrim(head.transform, PrimitiveType.Sphere, "Eye_R_White",
                new Vector3( 0.096f, 0.028f, -0.152f), new Vector3(0.105f, 0.10f, 0.06f), Color.white);

            // Iris colours
            Color irisCol = new Color(0.20f, 0.45f, 0.80f);
            MakePrim(head.transform, PrimitiveType.Sphere, "Iris_L",
                new Vector3(-0.096f, 0.026f, -0.168f), new Vector3(0.064f, 0.068f, 0.04f), irisCol);
            MakePrim(head.transform, PrimitiveType.Sphere, "Iris_R",
                new Vector3( 0.096f, 0.026f, -0.168f), new Vector3(0.064f, 0.068f, 0.04f), irisCol);

            // Pupils
            MakePrim(head.transform, PrimitiveType.Sphere, "Pupil_L",
                new Vector3(-0.096f, 0.026f, -0.178f), new Vector3(0.034f, 0.040f, 0.026f),
                new Color(0.05f, 0.05f, 0.06f));
            MakePrim(head.transform, PrimitiveType.Sphere, "Pupil_R",
                new Vector3( 0.096f, 0.026f, -0.178f), new Vector3(0.034f, 0.040f, 0.026f),
                new Color(0.05f, 0.05f, 0.06f));

            // Eye-glint (cartoon shine dot)
            MakePrim(head.transform, PrimitiveType.Sphere, "Glint_L",
                new Vector3(-0.082f, 0.040f, -0.184f), Vector3.one * 0.018f, Color.white);
            MakePrim(head.transform, PrimitiveType.Sphere, "Glint_R",
                new Vector3( 0.110f, 0.040f, -0.184f), Vector3.one * 0.018f, Color.white);

            // Eyelids (top edge line)
            MakePrim(head.transform, PrimitiveType.Cube, "Eyelid_L",
                new Vector3(-0.096f, 0.058f, -0.165f), new Vector3(0.108f, 0.022f, 0.022f),
                new Color(0.15f, 0.09f, 0.08f));
            MakePrim(head.transform, PrimitiveType.Cube, "Eyelid_R",
                new Vector3( 0.096f, 0.058f, -0.165f), new Vector3(0.108f, 0.022f, 0.022f),
                new Color(0.15f, 0.09f, 0.08f));

            // Eyebrows
            MakePrim(head.transform, PrimitiveType.Cube, "Brow_L",
                new Vector3(-0.096f, 0.082f, -0.162f), new Vector3(0.10f, 0.018f, 0.018f), hair);
            MakePrim(head.transform, PrimitiveType.Cube, "Brow_R",
                new Vector3( 0.096f, 0.082f, -0.162f), new Vector3(0.10f, 0.018f, 0.018f), hair);

            // Nose
            MakePrim(head.transform, PrimitiveType.Sphere, "Nose",
                new Vector3(0f, -0.022f, -0.168f), new Vector3(0.048f, 0.042f, 0.048f),
                new Color(skin.r * 0.88f, skin.g * 0.78f, skin.b * 0.72f));

            // Mouth
            MakePrim(head.transform, PrimitiveType.Cube, "Mouth",
                new Vector3(0f, -0.082f, -0.160f), new Vector3(0.095f, 0.022f, 0.020f),
                new Color(0.72f, 0.26f, 0.26f));

            // Cheek blush
            MakePrim(head.transform, PrimitiveType.Sphere, "Blush_L",
                new Vector3(-0.138f, -0.035f, -0.148f), new Vector3(0.065f, 0.040f, 0.028f),
                new Color(1f, 0.62f, 0.62f, 0.6f));
            MakePrim(head.transform, PrimitiveType.Sphere, "Blush_R",
                new Vector3( 0.138f, -0.035f, -0.148f), new Vector3(0.065f, 0.040f, 0.028f),
                new Color(1f, 0.62f, 0.62f, 0.6f));

            // Ears
            MakePrim(head.transform, PrimitiveType.Sphere, "Ear_L",
                new Vector3(-0.168f, 0.010f, 0f), new Vector3(0.055f, 0.070f, 0.048f), skin);
            MakePrim(head.transform, PrimitiveType.Sphere, "Ear_R",
                new Vector3( 0.168f, 0.010f, 0f), new Vector3(0.055f, 0.070f, 0.048f), skin);

            // ── Shopping Basket (right hand) ──────────────────────────────
            GameObject basket = new GameObject("ShoppingBasket");
            basket.transform.SetParent(visual.transform, false);
            basket.transform.localPosition = new Vector3(0.40f, 0.90f, 0.04f);
            basket.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);

            Color basketMetal = new Color(0.78f, 0.82f, 0.86f);
            MakePrim(basket.transform, PrimitiveType.Cube, "BasketBody",
                Vector3.zero, new Vector3(0.24f, 0.22f, 0.34f), basketMetal);
            MakePrim(basket.transform, PrimitiveType.Cube, "BasketHandle",
                new Vector3(0f, 0.16f, 0f), new Vector3(0.18f, 0.05f, 0.05f), basketMetal);

            // Grocery items peeking out
            Color[] grocColors = { Color.red, Color.yellow, Color.green };
            MakePrim(basket.transform, PrimitiveType.Cube, "Grocery_1",
                new Vector3(-0.055f, 0.14f, 0f), Vector3.one * 0.095f,
                grocColors[index % grocColors.Length]);
            MakePrim(basket.transform, PrimitiveType.Sphere, "Grocery_2",
                new Vector3(0.055f, 0.13f, 0.04f), Vector3.one * 0.08f,
                grocColors[(index + 1) % grocColors.Length]);

            // ── Held Grocery Weapon in Right Hand ────────────────────────
            Transform handRTrans = visual.transform.Find("Hand_R");
            if (handRTrans != null)
            {
                CartCombat.Store.GroceryItemType[] npcWeaponTypes = new CartCombat.Store.GroceryItemType[]
                {
                    CartCombat.Store.GroceryItemType.Baguette,
                    CartCombat.Store.GroceryItemType.Fish,
                    CartCombat.Store.GroceryItemType.Banana,
                    CartCombat.Store.GroceryItemType.SodaCan,
                    CartCombat.Store.GroceryItemType.CerealBox,
                    CartCombat.Store.GroceryItemType.Chips
                };
                CartCombat.Store.GroceryItemType wType = npcWeaponTypes[index % npcWeaponTypes.Length];
                GameObject wep = CartCombat.Store.GroceryItemFactory.CreateItem(wType, handRTrans.position + handRTrans.forward * 0.12f, handRTrans.rotation * Quaternion.Euler(45f, 0f, 0f), handRTrans);
                wep.name = "Customer_HeldWeapon";
                Rigidbody wrb = wep.GetComponent<Rigidbody>();
                if (wrb != null) wrb.isKinematic = true;
                Collider wcol = wep.GetComponent<Collider>();
                if (wcol != null) wcol.enabled = false;
            }

            // ── AI components ─────────────────────────────────────────────
            NPCShopperHealth healthComp = root.AddComponent<NPCShopperHealth>();
            NPCShopperAI    aiComp     = root.AddComponent<NPCShopperAI>();
            aiComp.AisleWaypoints = new List<Vector3>(aisleCorridorWaypoints);

            return root;
        }

        // ── Primitive helper ─────────────────────────────────────────────────

        private static GameObject MakePrim(Transform parent, PrimitiveType type,
            string objName, Vector3 localPos, Vector3 scale, Color colour)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = objName;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = scale;

            // Material
            Renderer rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(
                    Shader.Find("Universal Render Pipeline/Lit") ??
                    Shader.Find("Standard") ??
                    Shader.Find("Unlit/Color"));
                mat.color = colour;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colour);
                rend.sharedMaterial = mat;
            }

            // Remove collider — the CharacterController on root handles collisions
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }

            return go;
        }

        // ── Gizmos ──────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            // Only visible if explicitly selected in Hierarchy
            Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.25f);
            foreach (var wp in aisleCorridorWaypoints)
                Gizmos.DrawSphere(wp, 0.22f);
        }
    }
}
