using UnityEngine;
using CartCombat.Store;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CartCombat.Player
{
    /// <summary>
    /// Utility to instantiate, setup, and configure the playable 3D Shopper Character & Combat Cart
    /// with the Point Of View (POV) Camera system in the scene.
    /// </summary>
    public static class PlayerCharacterBuilder
    {
        public static GameObject SpawnOrSetupPlayer(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            // Find existing player or create new one
            CartPlayerController existing = Object.FindFirstObjectByType<CartPlayerController>();
            GameObject playerObj;

            if (existing != null)
            {
                playerObj = existing.gameObject;
                playerObj.transform.position = spawnPosition;
                playerObj.transform.rotation = spawnRotation;
            }
            else
            {
                playerObj = new GameObject("Player_ShopperCartFighter");
                playerObj.tag = "Player";
                playerObj.transform.position = spawnPosition;
                playerObj.transform.rotation = spawnRotation;

                // CharacterController
                CharacterController cc = playerObj.AddComponent<CharacterController>();
                cc.height = 1.85f;
                cc.radius = 0.45f;
                cc.center = new Vector3(0f, 0.92f, 0.2f);
                cc.skinWidth = 0.05f;
                cc.stepOffset = 0.4f;

                // CartPlayerController
                CartPlayerController controller = playerObj.AddComponent<CartPlayerController>();

                // Load or attach 3D Model
                AttachCharacterModel(playerObj.transform);
            }

            // Setup POV Camera
            SetupPOVCamera(playerObj.transform);

            Debug.Log("[Cart Combat] 3D Shopper Character & POV Camera spawned and configured successfully!");
            return playerObj;
        }

        private static void AttachCharacterModel(Transform playerRoot)
        {
            GameObject modelInstance = null;

#if UNITY_EDITOR
            // Try loading the authored 3D OBJ model asset
            GameObject objAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/ShopperCartFighter.obj");
            if (objAsset != null)
            {
                modelInstance = Object.Instantiate(objAsset, playerRoot);
                modelInstance.name = "ShopperCartFighter_Mesh";
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;

                // Assign URP materials to the imported submeshes
                ApplyModelMaterials(modelInstance);
            }
#endif

            // Fallback composite visual if asset is missing
            if (modelInstance == null)
            {
                BuildProceduralVisualFallback(playerRoot);
            }
        }

        private static void ApplyModelMaterials(GameObject modelObj)
        {
#if UNITY_EDITOR
            Renderer[] renderers = modelObj.GetComponentsInChildren<Renderer>();
            Material shelfMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Store/Mat_MetalShelf.mat");
            Material bumperMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Store/Mat_CheckoutCounter.mat");
            Material trimMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Store/Mat_StoreTrim.mat");

            foreach (Renderer r in renderers)
            {
                if (r.name.Contains("Cart") && shelfMat != null)
                {
                    r.sharedMaterial = shelfMat;
                }
                else if (bumperMat != null)
                {
                    r.sharedMaterial = bumperMat;
                }
            }
#endif
        }

        private static void BuildProceduralVisualFallback(Transform parent)
        {
            GameObject visual = new GameObject("Visual_CharacterAndCart");
            visual.transform.SetParent(parent, false);

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(visual.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.65f, -0.45f);
            head.transform.localScale = Vector3.one * 0.35f;

            // Torso
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "Torso";
            torso.transform.SetParent(visual.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.15f, -0.45f);
            torso.transform.localScale = new Vector3(0.5f, 0.65f, 0.3f);

            // Cart Handle
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.name = "CartHandle";
            handle.transform.SetParent(visual.transform, false);
            handle.transform.localPosition = new Vector3(0f, 0.98f, -0.02f);
            handle.transform.localScale = new Vector3(0.75f, 0.06f, 0.06f);

            // Cart Basket
            GameObject basket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basket.name = "CartBasket";
            basket.transform.SetParent(visual.transform, false);
            basket.transform.localPosition = new Vector3(0f, 0.65f, 0.55f);
            basket.transform.localScale = new Vector3(0.72f, 0.5f, 1.0f);

            // Front Ramming Bumper
            GameObject bumper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bumper.name = "RamBumper";
            bumper.transform.SetParent(visual.transform, false);
            bumper.transform.localPosition = new Vector3(0f, 0.35f, 1.15f);
            bumper.transform.localScale = new Vector3(0.85f, 0.16f, 0.12f);
        }

        private static void SetupPOVCamera(Transform playerTransform)
        {
            Camera cam = Camera.main;
            GameObject camObj;

            if (cam == null)
            {
                camObj = new GameObject("POV_MainCamera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }
            else
            {
                camObj = cam.gameObject;
            }

            // Parent camera to player for Point Of View
            camObj.transform.SetParent(playerTransform);
            camObj.transform.localPosition = new Vector3(0f, 1.65f, -0.35f);
            camObj.transform.localRotation = Quaternion.identity;
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.05f;

            CartPOVCamera povComp = camObj.GetComponent<CartPOVCamera>();
            if (povComp == null) povComp = camObj.AddComponent<CartPOVCamera>();
            povComp.SetTarget(playerTransform);
        }
    }
}
