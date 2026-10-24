using UnityEngine;

namespace CartCombat.Store
{
    /// <summary>
    /// Procedural 3D Model Factory for cartoon-style grocery items in Cart Combat.
    /// Builds recognizable, stylized 3D props (Bananas, Apples, Chips, Soda Cans, Fish, Baguettes, Melons)
    /// equipped with Rigidbody physics, colliders, and full weapon combat stats.
    /// </summary>
    public static class GroceryItemFactory
    {
        private static Shader cachedLitShader;

        private static Shader GetShader()
        {
            if (cachedLitShader == null)
            {
                cachedLitShader = Shader.Find("Universal Render Pipeline/Lit")
                               ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                               ?? Shader.Find("Standard")
                               ?? Shader.Find("Unlit/Color");
            }
            return cachedLitShader;
        }

        private static Material CreateMaterial(string name, Color color, float metallic = 0.0f, float smoothness = 0.5f)
        {
            Material mat = new Material(GetShader())
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
        /// Main factory method to create any grocery item with complete 3D model, collider, physics, and weapon stats.
        /// </summary>
        public static GameObject CreateItem(GroceryItemType type, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject root = new GameObject($"Item_{type}");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = rotation;

            GroceryItem gItem = root.AddComponent<GroceryItem>();
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            switch (type)
            {
                case GroceryItemType.Banana:
                    BuildBananaVisual(root.transform);
                    AddBoxCollider(root, new Vector3(0.18f, 0.16f, 0.36f), new Vector3(0f, 0.08f, 0f));
                    rb.mass = 0.35f;
                    gItem.Initialize(type, "Ripe Banana", GroceryCategory.Produce,
                        40, mDmg: 18f, tDmg: 22f, durability: 4, desc: "Slippery melee slap weapon");
                    break;

                case GroceryItemType.Apple:
                    BuildAppleVisual(root.transform);
                    AddSphereCollider(root, 0.16f, new Vector3(0f, 0.14f, 0f));
                    rb.mass = 0.40f;
                    gItem.Initialize(type, "Red Apple", GroceryCategory.Produce,
                        50, mDmg: 20f, tDmg: 32f, durability: 3, desc: "Dense throwing fruit");
                    break;

                case GroceryItemType.Chips:
                    BuildChipsVisual(root.transform);
                    AddBoxCollider(root, new Vector3(0.28f, 0.38f, 0.14f), new Vector3(0f, 0.19f, 0f));
                    rb.mass = 0.25f;
                    gItem.Initialize(type, "Flamin' Chips", GroceryCategory.Cereal,
                        60, mDmg: 14f, tDmg: 18f, durability: 5, desc: "Crinkly foil snack bag");
                    break;

                case GroceryItemType.SodaCan:
                    BuildSodaCanVisual(root.transform);
                    AddCylinderCollider(root, 0.10f, 0.28f, new Vector3(0f, 0.14f, 0f));
                    rb.mass = 0.45f;
                    gItem.Initialize(type, "Fizzie Cola Can", GroceryCategory.Beverage,
                        65, mDmg: 22f, tDmg: 35f, durability: 4, desc: "Refreshing aluminum blunt weapon");
                    break;

                case GroceryItemType.SodaBottle:
                    BuildSodaBottleVisual(root.transform);
                    AddCylinderCollider(root, 0.11f, 0.36f, new Vector3(0f, 0.18f, 0f));
                    rb.mass = 0.60f;
                    gItem.Initialize(type, "Mega Gulp Bottle", GroceryCategory.Beverage,
                        70, mDmg: 24f, tDmg: 38f, durability: 4, desc: "Heavy 2-liter soda bludgeon");
                    break;

                case GroceryItemType.Fish:
                    BuildFishVisual(root.transform);
                    AddBoxCollider(root, new Vector3(0.18f, 0.22f, 0.62f), new Vector3(0f, 0.11f, 0f));
                    rb.mass = 0.75f;
                    gItem.Initialize(type, "Fresh Tuna Fish", GroceryCategory.Bakery,
                        85, mDmg: 25f, tDmg: 28f, durability: 6, desc: "The legendary wet slap weapon!");
                    break;

                case GroceryItemType.Baguette:
                    BuildBaguetteVisual(root.transform);
                    AddBoxCollider(root, new Vector3(0.14f, 0.14f, 0.75f), new Vector3(0f, 0.07f, 0f));
                    rb.mass = 0.55f;
                    gItem.Initialize(type, "Crusty Baguette", GroceryCategory.Bakery,
                        75, mDmg: 28f, tDmg: 20f, durability: 6, desc: "Long French baker's sword");
                    break;

                case GroceryItemType.Watermelon:
                    BuildWatermelonVisual(root.transform);
                    AddSphereCollider(root, 0.22f, new Vector3(0f, 0.22f, 0f));
                    rb.mass = 3.50f;
                    gItem.Initialize(type, "Giant Watermelon", GroceryCategory.Produce,
                        100, mDmg: 45f, tDmg: 55f, durability: 2, desc: "Massive devastating fruit slammer");
                    break;

                case GroceryItemType.CerealBox:
                    BuildCerealBoxVisual(root.transform);
                    AddBoxCollider(root, new Vector3(0.18f, 0.38f, 0.28f), new Vector3(0f, 0.19f, 0f));
                    rb.mass = 0.50f;
                    gItem.Initialize(type, "Mega Crunch Cereal", GroceryCategory.Cereal,
                        55, mDmg: 16f, tDmg: 22f, durability: 4, desc: "Box of champion flakes");
                    break;
            }

            return root;
        }

        // ── 1. Banana ────────────────────────────────────────────────────────

        private static void BuildBananaVisual(Transform parent)
        {
            Material yellowMat = CreateMaterial("Mat_BananaYellow", new Color(1.00f, 0.85f, 0.12f), 0.0f, 0.45f);
            Material stemMat   = CreateMaterial("Mat_BananaStem",   new Color(0.24f, 0.15f, 0.06f), 0.0f, 0.20f);

            GameObject model = new GameObject("Visual_Banana");
            model.transform.SetParent(parent, false);

            // Segment 1 (Base, angled down)
            GameObject seg1 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            StripCollider(seg1);
            seg1.transform.SetParent(model.transform, false);
            seg1.transform.localPosition = new Vector3(0f, 0.06f, -0.10f);
            seg1.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
            seg1.transform.localScale    = new Vector3(0.09f, 0.09f, 0.09f);
            seg1.GetComponent<Renderer>().sharedMaterial = yellowMat;

            // Segment 2 (Center body)
            GameObject seg2 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            StripCollider(seg2);
            seg2.transform.SetParent(model.transform, false);
            seg2.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            seg2.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            seg2.transform.localScale    = new Vector3(0.10f, 0.12f, 0.10f);
            seg2.GetComponent<Renderer>().sharedMaterial = yellowMat;

            // Segment 3 (Top, angled up)
            GameObject seg3 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            StripCollider(seg3);
            seg3.transform.SetParent(model.transform, false);
            seg3.transform.localPosition = new Vector3(0f, 0.08f, 0.10f);
            seg3.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            seg3.transform.localScale    = new Vector3(0.085f, 0.09f, 0.085f);
            seg3.GetComponent<Renderer>().sharedMaterial = yellowMat;

            // Dark Stem at top
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(stem);
            stem.transform.SetParent(model.transform, false);
            stem.transform.localPosition = new Vector3(0f, 0.15f, 0.16f);
            stem.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
            stem.transform.localScale    = new Vector3(0.03f, 0.04f, 0.03f);
            stem.GetComponent<Renderer>().sharedMaterial = stemMat;

            // Dark tip at bottom
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            StripCollider(tip);
            tip.transform.SetParent(model.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0.08f, -0.16f);
            tip.transform.localScale    = new Vector3(0.035f, 0.035f, 0.035f);
            tip.GetComponent<Renderer>().sharedMaterial = stemMat;
        }

        // ── 2. Apple ─────────────────────────────────────────────────────────

        private static void BuildAppleVisual(Transform parent)
        {
            Material redMat   = CreateMaterial("Mat_AppleRed",   new Color(0.92f, 0.12f, 0.12f), 0.05f, 0.75f);
            Material stemMat  = CreateMaterial("Mat_AppleStem",  new Color(0.28f, 0.16f, 0.06f), 0.0f, 0.20f);
            Material leafMat  = CreateMaterial("Mat_AppleLeaf",  new Color(0.22f, 0.85f, 0.25f), 0.0f, 0.50f);

            GameObject model = new GameObject("Visual_Apple");
            model.transform.SetParent(parent, false);

            // Apple body (slightly squashed sphere)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            StripCollider(body);
            body.transform.SetParent(model.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            body.transform.localScale    = new Vector3(0.28f, 0.25f, 0.28f);
            body.GetComponent<Renderer>().sharedMaterial = redMat;

            // Stem
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(stem);
            stem.transform.SetParent(model.transform, false);
            stem.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            stem.transform.localRotation = Quaternion.Euler(12f, 0f, 8f);
            stem.transform.localScale    = new Vector3(0.02f, 0.05f, 0.02f);
            stem.GetComponent<Renderer>().sharedMaterial = stemMat;

            // Leaf
            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(leaf);
            leaf.transform.SetParent(model.transform, false);
            leaf.transform.localPosition = new Vector3(0.04f, 0.28f, 0.01f);
            leaf.transform.localRotation = Quaternion.Euler(20f, 35f, -25f);
            leaf.transform.localScale    = new Vector3(0.07f, 0.015f, 0.045f);
            leaf.GetComponent<Renderer>().sharedMaterial = leafMat;
        }

        // ── 3. Chips (Snack Bag) ─────────────────────────────────────────────

        private static void BuildChipsVisual(Transform parent)
        {
            Material bagMat   = CreateMaterial("Mat_ChipsBag",   new Color(0.95f, 0.20f, 0.15f), 0.10f, 0.60f); // Bright red
            Material crimpMat = CreateMaterial("Mat_ChipsCrimp", new Color(0.85f, 0.85f, 0.88f), 0.85f, 0.80f); // Silver foil seal
            Material labelMat = CreateMaterial("Mat_ChipsLabel", new Color(1.00f, 0.82f, 0.10f), 0.05f, 0.40f); // Gold stripe

            GameObject model = new GameObject("Visual_Chips");
            model.transform.SetParent(parent, false);

            // Main puffed bag
            GameObject bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(bag);
            bag.transform.SetParent(model.transform, false);
            bag.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            bag.transform.localScale    = new Vector3(0.24f, 0.32f, 0.13f);
            bag.GetComponent<Renderer>().sharedMaterial = bagMat;

            // Top crimped foil seal
            GameObject topCrimp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(topCrimp);
            topCrimp.transform.SetParent(model.transform, false);
            topCrimp.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            topCrimp.transform.localScale    = new Vector3(0.26f, 0.025f, 0.02f);
            topCrimp.GetComponent<Renderer>().sharedMaterial = crimpMat;

            // Bottom crimped foil seal
            GameObject botCrimp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(botCrimp);
            botCrimp.transform.SetParent(model.transform, false);
            botCrimp.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            botCrimp.transform.localScale    = new Vector3(0.26f, 0.025f, 0.02f);
            botCrimp.GetComponent<Renderer>().sharedMaterial = crimpMat;

            // Front decorative brand banner
            GameObject label = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(label);
            label.transform.SetParent(model.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.18f, 0.07f);
            label.transform.localScale    = new Vector3(0.20f, 0.08f, 0.01f);
            label.GetComponent<Renderer>().sharedMaterial = labelMat;
        }

        // ── 4. Soda Can ──────────────────────────────────────────────────────

        private static void BuildSodaCanVisual(Transform parent)
        {
            Material canMat    = CreateMaterial("Mat_SodaCan",   new Color(0.00f, 0.55f, 0.95f), 0.35f, 0.70f); // Cyan blue
            Material silverMat = CreateMaterial("Mat_SodaSilver",new Color(0.88f, 0.90f, 0.92f), 0.90f, 0.85f); // Aluminum metal

            GameObject model = new GameObject("Visual_SodaCan");
            model.transform.SetParent(parent, false);

            // Can body
            GameObject can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(can);
            can.transform.SetParent(model.transform, false);
            can.transform.localPosition = new Vector3(0f, 0.13f, 0f);
            can.transform.localScale    = new Vector3(0.17f, 0.12f, 0.17f);
            can.GetComponent<Renderer>().sharedMaterial = canMat;

            // Top aluminum lid
            GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(lid);
            lid.transform.SetParent(model.transform, false);
            lid.transform.localPosition = new Vector3(0f, 0.255f, 0f);
            lid.transform.localScale    = new Vector3(0.165f, 0.01f, 0.165f);
            lid.GetComponent<Renderer>().sharedMaterial = silverMat;

            // Pop-tab on lid
            GameObject tab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(tab);
            tab.transform.SetParent(model.transform, false);
            tab.transform.localPosition = new Vector3(0.02f, 0.268f, 0f);
            tab.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
            tab.transform.localScale    = new Vector3(0.05f, 0.008f, 0.03f);
            tab.GetComponent<Renderer>().sharedMaterial = silverMat;

            // Bottom aluminum rim
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(rim);
            rim.transform.SetParent(model.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0.008f, 0f);
            rim.transform.localScale    = new Vector3(0.165f, 0.01f, 0.165f);
            rim.GetComponent<Renderer>().sharedMaterial = silverMat;
        }

        // ── 5. Soda Bottle ───────────────────────────────────────────────────

        private static void BuildSodaBottleVisual(Transform parent)
        {
            Material bottleMat = CreateMaterial("Mat_SodaBottle", new Color(0.2f, 0.85f, 0.4f, 0.85f), 0.1f, 0.85f);
            Material capMat    = CreateMaterial("Mat_BottleCap",  new Color(0.95f, 0.15f, 0.15f), 0.0f, 0.5f);
            Material labelMat  = CreateMaterial("Mat_BottleLabel",new Color(1.0f, 1.0f, 1.0f), 0.0f, 0.3f);

            GameObject model = new GameObject("Visual_SodaBottle");
            model.transform.SetParent(parent, false);

            // Bottle base
            GameObject baseCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(baseCyl);
            baseCyl.transform.SetParent(model.transform, false);
            baseCyl.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            baseCyl.transform.localScale    = new Vector3(0.18f, 0.11f, 0.18f);
            baseCyl.GetComponent<Renderer>().sharedMaterial = bottleMat;

            // Label around center
            GameObject label = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(label);
            label.transform.SetParent(model.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            label.transform.localScale    = new Vector3(0.182f, 0.06f, 0.182f);
            label.GetComponent<Renderer>().sharedMaterial = labelMat;

            // Tapered neck
            GameObject neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(neck);
            neck.transform.SetParent(model.transform, false);
            neck.transform.localPosition = new Vector3(0f, 0.27f, 0f);
            neck.transform.localScale    = new Vector3(0.08f, 0.05f, 0.08f);
            neck.GetComponent<Renderer>().sharedMaterial = bottleMat;

            // Cap
            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            StripCollider(cap);
            cap.transform.SetParent(model.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.33f, 0f);
            cap.transform.localScale    = new Vector3(0.085f, 0.02f, 0.085f);
            cap.GetComponent<Renderer>().sharedMaterial = capMat;
        }

        // ── 6. Cartoon Fish (Legendary Melee Weapon!) ─────────────────────────

        private static void BuildFishVisual(Transform parent)
        {
            Material fishBodyMat = CreateMaterial("Mat_FishBody", new Color(0.25f, 0.65f, 0.88f), 0.15f, 0.65f); // Oceanic blue
            Material bellyMat    = CreateMaterial("Mat_FishBelly",new Color(0.85f, 0.92f, 0.98f), 0.05f, 0.50f); // Pale belly
            Material finMat      = CreateMaterial("Mat_FishFin",  new Color(0.18f, 0.50f, 0.72f), 0.10f, 0.60f); // Darker blue fin
            Material eyeWhiteMat = CreateMaterial("Mat_FishEyeW", Color.white, 0f, 0.8f);
            Material eyePupilMat = CreateMaterial("Mat_FishEyeP", Color.black, 0f, 0.9f);

            GameObject model = new GameObject("Visual_Fish");
            model.transform.SetParent(parent, false);

            // Fish body (tapered ellipsoid capsule)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            StripCollider(body);
            body.transform.SetParent(model.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.10f, 0f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            body.transform.localScale    = new Vector3(0.13f, 0.22f, 0.17f);
            body.GetComponent<Renderer>().sharedMaterial = fishBodyMat;

            // Belly underside
            GameObject belly = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            StripCollider(belly);
            belly.transform.SetParent(model.transform, false);
            belly.transform.localPosition = new Vector3(0f, 0.05f, 0.02f);
            belly.transform.localScale    = new Vector3(0.12f, 0.08f, 0.30f);
            belly.GetComponent<Renderer>().sharedMaterial = bellyMat;

            // Tail fin (rear vertical wedge)
            GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(tail);
            tail.transform.SetParent(model.transform, false);
            tail.transform.localPosition = new Vector3(0f, 0.10f, -0.26f);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.transform.localScale    = new Vector3(0.15f, 0.15f, 0.02f);
            tail.GetComponent<Renderer>().sharedMaterial = finMat;

            // Dorsal fin (top)
            GameObject dorsal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(dorsal);
            dorsal.transform.SetParent(model.transform, false);
            dorsal.transform.localPosition = new Vector3(0f, 0.20f, -0.04f);
            dorsal.transform.localRotation = Quaternion.Euler(30f, 0f, 0f);
            dorsal.transform.localScale    = new Vector3(0.015f, 0.08f, 0.12f);
            dorsal.GetComponent<Renderer>().sharedMaterial = finMat;

            // Pectoral fin left
            GameObject finL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(finL);
            finL.transform.SetParent(model.transform, false);
            finL.transform.localPosition = new Vector3(-0.08f, 0.08f, 0.05f);
            finL.transform.localRotation = Quaternion.Euler(0f, -35f, -30f);
            finL.transform.localScale    = new Vector3(0.015f, 0.05f, 0.09f);
            finL.GetComponent<Renderer>().sharedMaterial = finMat;

            // Pectoral fin right
            GameObject finR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(finR);
            finR.transform.SetParent(model.transform, false);
            finR.transform.localPosition = new Vector3(0.08f, 0.08f, 0.05f);
            finR.transform.localRotation = Quaternion.Euler(0f, 35f, 30f);
            finR.transform.localScale    = new Vector3(0.015f, 0.05f, 0.09f);
            finR.GetComponent<Renderer>().sharedMaterial = finMat;

            // Big cartoon googly eyes (Left & Right)
            CreateFishEye(model.transform, new Vector3(-0.065f, 0.12f, 0.15f), eyeWhiteMat, eyePupilMat);
            CreateFishEye(model.transform, new Vector3(0.065f, 0.12f, 0.15f), eyeWhiteMat, eyePupilMat);
        }

        private static void CreateFishEye(Transform parent, Vector3 localPos, Material whiteMat, Material pupilMat)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            StripCollider(eye);
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPos;
            eye.transform.localScale    = Vector3.one * 0.045f;
            eye.GetComponent<Renderer>().sharedMaterial = whiteMat;

            GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            StripCollider(pupil);
            pupil.transform.SetParent(eye.transform, false);
            pupil.transform.localPosition = new Vector3(Mathf.Sign(localPos.x) * 0.25f, 0.1f, 0.4f);
            pupil.transform.localScale    = Vector3.one * 0.55f;
            pupil.GetComponent<Renderer>().sharedMaterial = pupilMat;
        }

        // ── 7. Baguette ──────────────────────────────────────────────────────

        private static void BuildBaguetteVisual(Transform parent)
        {
            Material crustMat = CreateMaterial("Mat_BaguetteCrust", new Color(0.82f, 0.56f, 0.25f), 0.0f, 0.35f);
            Material scoreMat = CreateMaterial("Mat_BaguetteScore", new Color(0.95f, 0.85f, 0.65f), 0.0f, 0.25f);

            GameObject model = new GameObject("Visual_Baguette");
            model.transform.SetParent(parent, false);

            // Loaf body
            GameObject loaf = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            StripCollider(loaf);
            loaf.transform.SetParent(model.transform, false);
            loaf.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            loaf.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            loaf.transform.localScale    = new Vector3(0.12f, 0.35f, 0.09f);
            loaf.GetComponent<Renderer>().sharedMaterial = crustMat;

            // Diagonal slash scoring marks along the top
            for (int i = -2; i <= 2; i++)
            {
                GameObject slash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                StripCollider(slash);
                slash.transform.SetParent(model.transform, false);
                slash.transform.localPosition = new Vector3(0f, 0.11f, i * 0.11f);
                slash.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
                slash.transform.localScale    = new Vector3(0.06f, 0.015f, 0.025f);
                slash.GetComponent<Renderer>().sharedMaterial = scoreMat;
            }
        }

        // ── 8. Watermelon ────────────────────────────────────────────────────

        private static void BuildWatermelonVisual(Transform parent)
        {
            Material rindMat   = CreateMaterial("Mat_MelonRind",   new Color(0.15f, 0.55f, 0.22f), 0.05f, 0.65f);
            Material stripeMat = CreateMaterial("Mat_MelonStripe", new Color(0.08f, 0.32f, 0.12f), 0.05f, 0.55f);

            GameObject model = new GameObject("Visual_Watermelon");
            model.transform.SetParent(parent, false);

            // Main melon sphere
            GameObject melon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            StripCollider(melon);
            melon.transform.SetParent(model.transform, false);
            melon.transform.localPosition = new Vector3(0f, 0.20f, 0f);
            melon.transform.localScale    = new Vector3(0.38f, 0.34f, 0.44f);
            melon.GetComponent<Renderer>().sharedMaterial = rindMat;

            // Dark green longitudinal stripes
            for (int i = 0; i < 4; i++)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                StripCollider(stripe);
                stripe.transform.SetParent(model.transform, false);
                stripe.transform.localPosition = new Vector3(0f, 0.20f, 0f);
                stripe.transform.localRotation = Quaternion.Euler(0f, i * 45f, 0f);
                stripe.transform.localScale    = new Vector3(0.035f, 0.35f, 0.45f);
                stripe.GetComponent<Renderer>().sharedMaterial = stripeMat;
            }
        }

        // ── 9. Cereal Box ────────────────────────────────────────────────────

        private static void BuildCerealBoxVisual(Transform parent)
        {
            Material boxMat    = CreateMaterial("Mat_CerealBox",   new Color(0.95f, 0.40f, 0.10f), 0.05f, 0.45f); // Vibrant orange
            Material bannerMat = CreateMaterial("Mat_CerealBanner",new Color(1.00f, 0.88f, 0.15f), 0.05f, 0.45f); // Gold sunburst

            GameObject model = new GameObject("Visual_CerealBox");
            model.transform.SetParent(parent, false);

            // Main box
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(box);
            box.transform.SetParent(model.transform, false);
            box.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            box.transform.localScale    = new Vector3(0.16f, 0.34f, 0.26f);
            box.GetComponent<Renderer>().sharedMaterial = boxMat;

            // Front cover emblem
            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            StripCollider(banner);
            banner.transform.SetParent(model.transform, false);
            banner.transform.localPosition = new Vector3(0f, 0.20f, 0.135f);
            banner.transform.localScale    = new Vector3(0.12f, 0.14f, 0.01f);
            banner.GetComponent<Renderer>().sharedMaterial = bannerMat;
        }

        // ── Physics Helpers ──────────────────────────────────────────────────

        private static void StripCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Object.Destroy(col);
                else Object.DestroyImmediate(col);
            }
        }

        private static void AddBoxCollider(GameObject root, Vector3 size, Vector3 center)
        {
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.size = size;
            box.center = center;
        }

        private static void AddSphereCollider(GameObject root, float radius, Vector3 center)
        {
            SphereCollider sc = root.AddComponent<SphereCollider>();
            sc.radius = radius;
            sc.center = center;
        }

        private static void AddCylinderCollider(GameObject root, float radius, float height, Vector3 center)
        {
            CapsuleCollider cc = root.AddComponent<CapsuleCollider>();
            cc.radius = radius;
            cc.height = height;
            cc.center = center;
            cc.direction = 1; // Y-axis
        }
    }
}
