using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CartCombat.Store;
using CartCombat.NPC;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CartCombat.Player
{
    /// <summary>
    /// Player character controller supporting:
    /// 1. Combat shopping cart pushing & physical collision with NPC customers
    /// 2. Leaving / dismounting cart ([X]) to explore on foot, and re-grabbing cart
    /// 3. Unarmed body combat ([LMB] punch / body strike when no weapon held)
    /// 4. Grabbing weapons/produce from shelves and floor ([E])
    /// 5. Melee weapon swings ([LMB]), throws ([RMB]), and high-speed cart rams
    /// 6. Player health and damage reception from counter-attacking customers
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CartPlayerController : MonoBehaviour
    {
        [Header("Cart Movement Settings")]
        [SerializeField] private float walkSpeed = 6.5f;
        [SerializeField] private float sprintSpeed = 12.0f;
        [SerializeField] private float reverseSpeed = 3.5f;
        [SerializeField] private float jumpForce = 5.5f;
        [SerializeField] private float gravity = 20.0f;

        [Header("Foot Mode Settings (Dismounted)")]
        [SerializeField] private bool isPushingCart = true;
        [SerializeField] private float footWalkSpeed = 7.5f;
        [SerializeField] private float footSprintSpeed = 13.5f;
        [SerializeField] private float footReverseSpeed = 5.0f;
        [SerializeField] private float footJumpForce = 7.0f;

        [Header("Cart Combat Mechanics")]
        [SerializeField] private float ramSpeed = 18.0f;
        [SerializeField] private float ramDuration = 0.45f;
        [SerializeField] private float ramCooldown = 1.5f;
        [SerializeField] private float ramDamage = 40.0f;
        [SerializeField] private float itemThrowForce = 24.0f;
        [SerializeField] private float pickupRadius = 3.8f;

        [Header("Unarmed Body Combat")]
        [SerializeField] private float punchDamage = 16.0f;
        [SerializeField] private float punchRange = 2.2f;
        [SerializeField] private float punchCooldown = 0.32f;

        [Header("Player Health & Combat")]
        [SerializeField] private float playerHealth = 100f;
        [SerializeField] private float maxHealth = 100f;

        [Header("Weapon & Inventory Slots")]
        [SerializeField] private Transform cartBasketSlot;
        [SerializeField] private Transform weaponHoldSlot;
        [SerializeField] private GroceryItem heldItem;
        [SerializeField] private List<GroceryItem> carriedItems = new List<GroceryItem>();
        [SerializeField] private int maxBasketCapacity = 3;

        [Header("Visual Feedback & Tilt")]
        [SerializeField] private Transform cartModelTransform;
        [SerializeField] private float steerTiltAmount = 4.0f;

        // Weapon ready stance & swing offsets relative to POV camera
        private readonly Vector3 weaponReadyPos = new Vector3(0.32f, -0.28f, 0.65f);
        private readonly Quaternion weaponReadyRot = Quaternion.Euler(15f, -15f, 5f);
        private readonly Vector3 weaponSwingPos = new Vector3(0.08f, -0.35f, 0.78f);
        private readonly Quaternion weaponSwingRot = Quaternion.Euler(65f, -45f, 25f);

        // Runtime states
        private CharacterController controller;
        private Camera playerCamera;
        private Vector3 moveDirection = Vector3.zero;
        private float verticalVelocity = 0f;
        private bool isRamming = false;
        private bool isSwinging = false;
        private bool isPunching = false;
        private float lastRamTime = -10f;
        private float lastPunchTime = -10f;
        private float lastBumpRegisterTime = -10f;
        private float currentSteerTilt = 0f;
        private ShopperHUD hud;

        public bool IsRamming => isRamming;
        public bool IsSwinging => isSwinging;
        public bool IsPunching => isPunching;
        public bool IsPushingCart => isPushingCart;
        public float Health => playerHealth;
        public float MaxHealth => maxHealth;
        public GroceryItem HeldItem => heldItem;
        public int ItemCount => (heldItem != null ? 1 : 0) + carriedItems.Count;
        public IReadOnlyList<GroceryItem> CarriedItems => carriedItems;
        public Transform CartModelTransform => cartModelTransform;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;

            // Find cart model if not explicitly wired in Inspector
            if (cartModelTransform == null)
            {
                cartModelTransform = transform.Find("CartModel")
                                  ?? transform.Find("ShopperCartFighter_Mesh")
                                  ?? transform.Find("Visual_CharacterAndCart");
            }

            // Ensure cart has a solid BoxCollider so customers cannot walk through it
            EnsureCartPhysicalCollider();

            // Basket slot on the cart
            if (cartBasketSlot == null)
            {
                GameObject slot = new GameObject("CartBasketSlot");
                slot.transform.SetParent(cartModelTransform != null ? cartModelTransform : transform, false);
                slot.transform.localPosition = new Vector3(0f, 0.7f, 0.6f);
                cartBasketSlot = slot.transform;
            }

            // Weapon hold slot (in front of camera for POV first-person view)
            if (weaponHoldSlot == null)
            {
                GameObject wSlot = new GameObject("WeaponHoldSlot");
                Transform parentT = (playerCamera != null) ? playerCamera.transform : transform;
                wSlot.transform.SetParent(parentT, false);
                wSlot.transform.localPosition = weaponReadyPos;
                wSlot.transform.localRotation = weaponReadyRot;
                weaponHoldSlot = wSlot.transform;
            }

            // Attach HUD
            hud = gameObject.GetComponent<ShopperHUD>();
            if (hud == null) hud = gameObject.AddComponent<ShopperHUD>();
            if (hud != null) hud.SetHealth(playerHealth, maxHealth);
        }

        private void EnsureCartPhysicalCollider()
        {
            if (cartModelTransform == null) return;

            Transform existingCol = cartModelTransform.Find("CartCollider");
            if (existingCol == null)
            {
                GameObject colObj = new GameObject("CartCollider");
                colObj.transform.SetParent(cartModelTransform, false);
                colObj.transform.localPosition = new Vector3(0f, 0.45f, 0.65f);
                BoxCollider bc = colObj.AddComponent<BoxCollider>();
                bc.size = new Vector3(0.85f, 0.90f, 1.25f);
                bc.isTrigger = false;
            }
        }

        private void Update()
        {
            HandleMovement();
            HandleActions();
            ResolveCustomerPushback();
        }

        // ── Movement & Controls ──────────────────────────────────────────────

        private void HandleMovement()
        {
            float inputX = 0f;
            float inputZ = 0f;
            bool isSprinting = false;
            bool jumpPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputZ += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputZ -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputX -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputX += 1f;

                isSprinting = Keyboard.current.leftShiftKey.isPressed;
                jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
            }
#else
            inputX = Input.GetAxisRaw("Horizontal");
            inputZ = Input.GetAxisRaw("Vertical");
            isSprinting = Input.GetKey(KeyCode.LeftShift);
            jumpPressed = Input.GetKeyDown(KeyCode.Space);
#endif

            // Ground check & gravity
            float currentJumpForce = isPushingCart ? jumpForce : footJumpForce;
            if (controller.isGrounded)
            {
                verticalVelocity = -1.0f;
                if (jumpPressed && !isRamming)
                {
                    verticalVelocity = currentJumpForce;
                }
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            if (!isRamming)
            {
                float curWalkSpeed = isPushingCart ? walkSpeed : footWalkSpeed;
                float curSprintSpeed = isPushingCart ? sprintSpeed : footSprintSpeed;
                float curRevSpeed = isPushingCart ? reverseSpeed : footReverseSpeed;

                float targetSpeed = (inputZ < 0f) ? curRevSpeed : (isSprinting ? curSprintSpeed : curWalkSpeed);
                Vector3 inputVec = (transform.forward * inputZ + transform.right * inputX).normalized;
                moveDirection = inputVec * targetSpeed;

                // Subtle cart lean when steering (only when pushing cart)
                if (isPushingCart && cartModelTransform != null && cartModelTransform.parent == transform)
                {
                    float targetTilt = -inputX * steerTiltAmount;
                    currentSteerTilt = Mathf.Lerp(currentSteerTilt, targetTilt, Time.deltaTime * 10f);
                    cartModelTransform.localRotation = Quaternion.Euler(0f, 0f, currentSteerTilt);
                }
            }

            Vector3 finalMove = moveDirection;
            finalMove.y = verticalVelocity;
            controller.Move(finalMove * Time.deltaTime);
        }

        private void HandleActions()
        {
            bool grabPressed = false;
            bool dropPressed = false;
            bool attackPressed = false;
            bool throwPressed = false;
            bool swapPressed = false;
            bool toggleCartPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                grabPressed = Keyboard.current.eKey.wasPressedThisFrame;
                dropPressed = Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.gKey.wasPressedThisFrame;
                attackPressed = Keyboard.current.fKey.wasPressedThisFrame;
                swapPressed = Keyboard.current.tabKey.wasPressedThisFrame;
                toggleCartPressed = Keyboard.current.xKey.wasPressedThisFrame;
            }
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) attackPressed = true;
                if (Mouse.current.rightButton.wasPressedThisFrame) throwPressed = true;
            }
#else
            grabPressed = Input.GetKeyDown(KeyCode.E);
            dropPressed = Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.G);
            attackPressed = Input.GetKeyDown(KeyCode.F) || Input.GetMouseButtonDown(0);
            throwPressed = Input.GetMouseButtonDown(1);
            swapPressed = Input.GetKeyDown(KeyCode.Tab);
            toggleCartPressed = Input.GetKeyDown(KeyCode.X);
#endif

            // 1. Leave Cart / Re-grab Cart ([X])
            if (toggleCartPressed)
            {
                ToggleCartPush();
            }

            // 2. Grab item from shelf or ground ([E])
            if (grabPressed)
            {
                // If on foot and looking right at the parked cart, re-grab the cart!
                if (!isPushingCart && cartModelTransform != null && Vector3.Distance(transform.position, cartModelTransform.position) < 2.5f)
                {
                    AttachToCart();
                }
                else
                {
                    TryPickupNearbyItem();
                }
            }

            // 3. Ungrab / Drop currently held item onto the ground ([Q])
            if (dropPressed)
            {
                DropHeldItem();
            }

            // 4. Attack: Melee weapon swing (if holding weapon), Cart Ram (if sprinting with cart), or Unarmed Body Punch!
            if (attackPressed)
            {
                if (heldItem != null)
                {
                    TryMeleeSwing();
                }
                else
                {
                    // Check if player is sprinting with cart to do a high-speed Ram Attack
                    bool isSprinting = false;
#if ENABLE_INPUT_SYSTEM
                    if (Keyboard.current != null) isSprinting = Keyboard.current.leftShiftKey.isPressed;
#else
                    isSprinting = Input.GetKey(KeyCode.LeftShift);
#endif
                    if (isPushingCart && isSprinting && controller.velocity.magnitude > 6.0f)
                    {
                        TryRamAttack();
                    }
                    else
                    {
                        // Unarmed body combat! Punches and strikes using body
                        TryUnarmedPunch();
                    }
                }
            }

            // 5. Throw held item as a projectile ([RMB])
            if (throwPressed && heldItem != null)
            {
                ThrowHeldItem();
            }

            // 6. Swap held item with basket item ([Tab])
            if (swapPressed)
            {
                CycleBasketItem();
            }
        }

        // ── Leave & Re-grab Cart ─────────────────────────────────────────────

        public void ToggleCartPush()
        {
            if (isPushingCart)
            {
                ReleaseCart();
            }
            else
            {
                if (cartModelTransform != null)
                {
                    float dist = Vector3.Distance(transform.position, cartModelTransform.position);
                    if (dist < 3.5f)
                    {
                        AttachToCart();
                    }
                    else
                    {
                        Debug.Log($"[Cart Combat] Too far from cart ({dist:F1}m). Walk closer to press [X] to push it.");
                    }
                }
            }
        }

        public void ReleaseCart()
        {
            if (!isPushingCart || cartModelTransform == null) return;

            isPushingCart = false;

            // Detach cart model so it stays parked in the world
            cartModelTransform.SetParent(null, true);

            // Step player back slightly behind the handle
            Vector3 stepBack = -transform.forward * 0.75f;
            controller.Move(stepBack);

            Debug.Log("<color=#00FF88><b>[Cart Combat]</b> Left the cart! You are now on foot. Walk around and press [X] or [E] near the cart to push again.</color>");
        }

        public void AttachToCart()
        {
            if (isPushingCart || cartModelTransform == null) return;

            // Position player directly behind the cart
            transform.position = cartModelTransform.position;
            transform.rotation = cartModelTransform.rotation;

            // Re-parent cart model to player root
            cartModelTransform.SetParent(transform, true);
            cartModelTransform.localPosition = Vector3.zero;
            cartModelTransform.localRotation = Quaternion.identity;

            isPushingCart = true;
            Debug.Log("<color=#00FF88><b>[Cart Combat]</b> Re-grabbed shopping cart! Ready to roll.</color>");
        }

        // ── Unarmed Body Combat ──────────────────────────────────────────────

        private void TryUnarmedPunch()
        {
            if (Time.time < lastPunchTime + punchCooldown || isPunching || isSwinging) return;
            StartCoroutine(AnimateUnarmedPunch());
        }

        private IEnumerator AnimateUnarmedPunch()
        {
            isPunching = true;
            lastPunchTime = Time.time;

            // Subtle first-person camera punch jolt
            Vector3 camStart = (playerCamera != null) ? playerCamera.transform.localPosition : Vector3.zero;
            Vector3 punchCamFwd = camStart + Vector3.forward * 0.14f;

            float elapsed = 0f;
            float punchIn = 0.08f;
            while (elapsed < punchIn)
            {
                if (playerCamera != null)
                {
                    playerCamera.transform.localPosition = Vector3.Lerp(camStart, punchCamFwd, elapsed / punchIn);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Punch Hit detection at peak
            PerformUnarmedHitDetection();

            // Smooth return
            elapsed = 0f;
            float punchOut = 0.12f;
            while (elapsed < punchOut)
            {
                if (playerCamera != null)
                {
                    playerCamera.transform.localPosition = Vector3.Lerp(punchCamFwd, camStart, elapsed / punchOut);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (playerCamera != null) playerCamera.transform.localPosition = camStart;
            isPunching = false;
        }

        private void PerformUnarmedHitDetection()
        {
            Vector3 origin = (playerCamera != null) ? playerCamera.transform.position : transform.position + Vector3.up * 1.5f;
            Vector3 forward = (playerCamera != null) ? playerCamera.transform.forward : transform.forward;

            RaycastHit[] hits = Physics.SphereCastAll(origin, 0.45f, forward, punchRange);
            bool hitAny = false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (cartModelTransform != null && hit.collider.transform.IsChildOf(cartModelTransform)) continue;

                // Check Customer / NPC Shopper
                NPCShopperHealth npc = hit.collider.GetComponentInParent<NPCShopperHealth>();
                if (npc != null && !npc.IsDead)
                {
                    Vector3 hitDir = (npc.transform.position - transform.position).normalized + Vector3.up * 0.25f;
                    npc.TakeDamage(punchDamage, hitDir);

                    // Knockback on customer
                    NPCShopperAI ai = npc.GetComponent<NPCShopperAI>();
                    if (ai != null)
                    {
                        ai.AddKnockback(hitDir * 4.5f);
                    }

                    // Register bump/hit on customer so multiple punches enrage them
                    npc.RegisterCartBump(hitDir, 4.0f, false);

                    if (hud != null)
                    {
                        hud.ShowHitMarker();
                        hud.AddScore(25f);
                    }

                    hitAny = true;
                    Debug.Log($"<color=#00FF88><b>[Cart Combat]</b> Punch struck {npc.name} for {punchDamage:F0} dmg!</color>");
                    break;
                }

                // Physics props or grocery items on shelves/floor
                Rigidbody hitRb = hit.collider.attachedRigidbody;
                if (hitRb != null && !hitRb.isKinematic)
                {
                    hitRb.AddForce(forward * 240f + Vector3.up * 50f, ForceMode.Impulse);
                    hitAny = true;
                }
            }

            if (!hitAny)
            {
                Debug.Log("[Cart Combat] Threw a body punch into the air!");
            }
        }

        // ── Item Pickup & Equipping (Floor & Shelves) ─────────────────────────

        private void TryPickupNearbyItem()
        {
            Ray ray = (playerCamera != null)
                ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : new Ray(transform.position + Vector3.up * 1.4f, transform.forward);

            GroceryItem targetItem = null;

            // 1. Raycast forward, strictly filtering out player and cart colliders
            RaycastHit[] hits = Physics.RaycastAll(ray, 4.5f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (cartModelTransform != null && hit.collider.transform.IsChildOf(cartModelTransform)) continue;

                GroceryItem item = hit.collider.GetComponentInParent<GroceryItem>();
                if (item != null && item != heldItem && !carriedItems.Contains(item) && item.CanBePickedUp)
                {
                    targetItem = item;
                    break;
                }
            }

            // 2. Generous SphereCast forward (forgiving aiming at small items on shelves)
            if (targetItem == null)
            {
                RaycastHit[] sphereHits = Physics.SphereCastAll(ray.origin, 0.40f, ray.direction, 4.2f);
                System.Array.Sort(sphereHits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in sphereHits)
                {
                    if (hit.collider == null) continue;
                    if (hit.collider.transform.IsChildOf(transform)) continue;
                    if (cartModelTransform != null && hit.collider.transform.IsChildOf(cartModelTransform)) continue;

                    GroceryItem item = hit.collider.GetComponentInParent<GroceryItem>();
                    if (item != null && item != heldItem && !carriedItems.Contains(item) && item.CanBePickedUp)
                    {
                        targetItem = item;
                        break;
                    }
                }
            }

            // 3. Proximity check for items on the floor, tables, or low shelves
            if (targetItem == null)
            {
                Collider[] overlap = Physics.OverlapSphere(transform.position + transform.forward * 0.9f + Vector3.up * 0.5f, pickupRadius);
                float closestDist = float.MaxValue;

                foreach (Collider c in overlap)
                {
                    if (c == null) continue;
                    if (c.transform.IsChildOf(transform)) continue;
                    if (cartModelTransform != null && c.transform.IsChildOf(cartModelTransform)) continue;

                    GroceryItem item = c.GetComponentInParent<GroceryItem>();
                    if (item != null && item != heldItem && !carriedItems.Contains(item) && item.CanBePickedUp)
                    {
                        Vector3 toItem = item.transform.position - transform.position;
                        toItem.y = 0f;
                        float dot = Vector3.Dot(transform.forward, toItem.normalized);
                        if (dot > -0.2f) // In front or slightly to sides
                        {
                            float dist = Vector3.Distance(transform.position, item.transform.position);
                            if (dist < closestDist)
                            {
                                closestDist = dist;
                                targetItem = item;
                            }
                        }
                    }
                }
            }

            if (targetItem != null)
            {
                PickUpItem(targetItem);
            }
            else
            {
                Debug.Log("[Cart Combat] No grocery items within reach to grab. Move closer to a shelf or floor item.");
            }
        }

        private void PickUpItem(GroceryItem item)
        {
            // If hands are empty, equip directly into hand weapon slot!
            if (heldItem == null)
            {
                EquipHeldItem(item);
                Debug.Log($"[Cart Combat] Wielding {item.ItemName} in hand! Ready to swing.");
            }
            // Otherwise, store in cart basket if pushing cart
            else if (isPushingCart && carriedItems.Count < maxBasketCapacity)
            {
                StoreInBasket(item);
                Debug.Log($"[Cart Combat] Stored {item.ItemName} in cart basket ({carriedItems.Count}/{maxBasketCapacity}).");
            }
            else
            {
                // If on foot or basket full, swap currently held item
                DropHeldItem();
                EquipHeldItem(item);
                Debug.Log($"[Cart Combat] Swapped weapon to {item.ItemName}!");
            }
        }

        private void EquipHeldItem(GroceryItem item)
        {
            heldItem = item;
            item.SetWielded(true);

            if (weaponHoldSlot != null)
            {
                item.transform.SetParent(weaponHoldSlot, false);
                item.transform.localPosition = Vector3.zero;
                item.transform.localRotation = Quaternion.identity;
            }
        }

        private void StoreInBasket(GroceryItem item)
        {
            carriedItems.Add(item);
            item.SetWielded(true);

            if (cartBasketSlot != null)
            {
                item.transform.SetParent(cartBasketSlot, false);
                float itemSpacing = 0.24f;
                int idx = carriedItems.Count - 1;
                item.transform.localPosition = new Vector3((idx - 1) * itemSpacing, 0f, 0f);
                item.transform.localRotation = Quaternion.Euler(0f, idx * 30f, 0f);
            }
        }

        // ── Drop / Ungrab ────────────────────────────────────────────────────

        public void DropHeldItem()
        {
            if (heldItem != null)
            {
                GroceryItem itemToDrop = heldItem;
                heldItem = null;

                Vector3 dropPos = transform.position + transform.forward * 1.0f + Vector3.up * 0.6f;
                Vector3 dropVel = transform.forward * 3.0f + Vector3.up * 1.5f;
                itemToDrop.DropToGround(dropPos, dropVel);

                // Auto-equip next weapon from basket if available
                EquipNextFromBasket();
            }
            else if (carriedItems.Count > 0)
            {
                GroceryItem itemToDrop = carriedItems[carriedItems.Count - 1];
                carriedItems.RemoveAt(carriedItems.Count - 1);

                Vector3 dropPos = transform.position + transform.forward * 0.8f + Vector3.up * 0.5f;
                itemToDrop.DropToGround(dropPos, Vector3.up * 1f);
            }
        }

        private void EquipNextFromBasket()
        {
            if (carriedItems.Count > 0)
            {
                GroceryItem next = carriedItems[0];
                carriedItems.RemoveAt(0);
                EquipHeldItem(next);
                RearrangeBasketVisuals();
            }
        }

        private void CycleBasketItem()
        {
            if (carriedItems.Count == 0) return;

            GroceryItem oldHeld = heldItem;
            GroceryItem newHeld = carriedItems[0];
            carriedItems.RemoveAt(0);

            if (oldHeld != null)
            {
                carriedItems.Add(oldHeld);
                oldHeld.SetWielded(true);
            }

            EquipHeldItem(newHeld);
            RearrangeBasketVisuals();
            Debug.Log($"[Cart Combat] Swapped weapon to {newHeld.ItemName}!");
        }

        private void RearrangeBasketVisuals()
        {
            if (cartBasketSlot == null) return;
            float itemSpacing = 0.24f;
            for (int i = 0; i < carriedItems.Count; i++)
            {
                if (carriedItems[i] != null)
                {
                    carriedItems[i].transform.SetParent(cartBasketSlot, false);
                    carriedItems[i].transform.localPosition = new Vector3((i - 1) * itemSpacing, 0f, 0f);
                }
            }
        }

        // ── Melee Swing & Attack ─────────────────────────────────────────────

        private void TryMeleeSwing()
        {
            if (isSwinging || heldItem == null) return;
            StartCoroutine(AnimateMeleeSwing());
        }

        private IEnumerator AnimateMeleeSwing()
        {
            isSwinging = true;

            float swingDuration = Mathf.Max(0.18f, heldItem.SwingSpeed);
            float halfDuration = swingDuration * 0.45f;
            float returnDuration = swingDuration * 0.55f;

            // Phase 1: Forward & downward chop swing
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                if (weaponHoldSlot != null)
                {
                    weaponHoldSlot.localPosition = Vector3.Lerp(weaponReadyPos, weaponSwingPos, t);
                    weaponHoldSlot.localRotation = Quaternion.Slerp(weaponReadyRot, weaponSwingRot, t);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Hit detection at peak of swing!
            PerformMeleeHitDetection();

            // Phase 2: Smooth return to ready stance
            elapsed = 0f;
            while (elapsed < returnDuration)
            {
                float t = elapsed / returnDuration;
                if (weaponHoldSlot != null)
                {
                    weaponHoldSlot.localPosition = Vector3.Lerp(weaponSwingPos, weaponReadyPos, t);
                    weaponHoldSlot.localRotation = Quaternion.Slerp(weaponSwingRot, weaponReadyRot, t);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (weaponHoldSlot != null)
            {
                weaponHoldSlot.localPosition = weaponReadyPos;
                weaponHoldSlot.localRotation = weaponReadyRot;
            }

            isSwinging = false;
        }

        private void PerformMeleeHitDetection()
        {
            if (heldItem == null) return;

            Vector3 origin = (playerCamera != null) ? playerCamera.transform.position : transform.position + Vector3.up * 1.5f;
            Vector3 forward = (playerCamera != null) ? playerCamera.transform.forward : transform.forward;
            float range = heldItem.MeleeRange;

            RaycastHit[] hits = Physics.SphereCastAll(origin, 0.45f, forward, range);

            bool hitTarget = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (cartModelTransform != null && hit.collider.transform.IsChildOf(cartModelTransform)) continue;

                // Check for NPC Shopper / Customer
                NPCShopperHealth npc = hit.collider.GetComponentInParent<NPCShopperHealth>();
                if (npc != null && !npc.IsDead)
                {
                    Vector3 hitDir = (npc.transform.position - transform.position).normalized + Vector3.up * 0.2f;
                    bool broke = heldItem.ApplyMeleeHit(npc, hitDir);

                    // Also register cart bump / hit so 2-3 hits trigger fight
                    npc.RegisterCartBump(hitDir, 5.0f, false);

                    if (hud != null)
                    {
                        hud.ShowHitMarker();
                        hud.AddScore(heldItem.ScoreValue * 0.5f);
                    }

                    hitTarget = true;

                    if (broke)
                    {
                        heldItem = null;
                        EquipNextFromBasket();
                    }
                    break;
                }

                // Check for other grocery items / props to send them flying
                Rigidbody hitRb = hit.collider.attachedRigidbody;
                if (hitRb != null && !hitRb.isKinematic)
                {
                    hitRb.AddForce(forward * 250f + Vector3.up * 50f, ForceMode.Impulse);
                }
            }

            if (!hitTarget)
            {
                Debug.Log($"[Cart Combat] Swung {heldItem.ItemName} in the air!");
            }
        }

        // ── Throwing Attack ──────────────────────────────────────────────────

        private void ThrowHeldItem()
        {
            if (heldItem == null) return;

            GroceryItem itemToThrow = heldItem;
            heldItem = null;

            Vector3 origin = (playerCamera != null)
                ? playerCamera.transform.position + playerCamera.transform.forward * 0.8f
                : transform.position + transform.forward * 1.2f + Vector3.up * 1.2f;

            Vector3 dir = (playerCamera != null) ? playerCamera.transform.forward : transform.forward;
            dir.y += 0.12f; // upward arc
            Vector3 velocity = dir.normalized * itemThrowForce;

            itemToThrow.LaunchProjectile(origin, velocity);

            EquipNextFromBasket();
        }

        // ── Cart Ramming Attack ──────────────────────────────────────────────

        private void TryRamAttack()
        {
            if (Time.time < lastRamTime + ramCooldown || isRamming || !isPushingCart) return;
            StartCoroutine(PerformRamAttack());
        }

        private IEnumerator PerformRamAttack()
        {
            isRamming = true;
            lastRamTime = Time.time;

            if (hud != null) hud.NotifyRamUsed(ramCooldown);

            Vector3 ramDir = transform.forward;
            float elapsed = 0f;

            while (elapsed < ramDuration)
            {
                controller.Move(ramDir * ramSpeed * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            isRamming = false;
        }

        // ── Player Health & Damage Reception ─────────────────────────────────

        public void TakeDamage(float amount, Vector3 hitDirection)
        {
            if (playerHealth <= 0f) return;

            playerHealth = Mathf.Max(0f, playerHealth - amount);

            if (hud != null)
            {
                hud.SetHealth(playerHealth, maxHealth);
                hud.ShowHitMarker();
            }

            StartCoroutine(DamageFlinchRoutine(hitDirection));

            Debug.Log($"<color=#FF2222><b>[Cart Combat]</b> Player took {amount:F0} damage from angry customer! HP: {playerHealth:F0}/{maxHealth:F0}</color>");

            if (playerHealth <= 0f)
            {
                Debug.Log("<color=#FF0000><b>[Cart Combat]</b> Player was knocked out by customer!</color>");
            }
        }

        private IEnumerator DamageFlinchRoutine(Vector3 hitDir)
        {
            if (playerCamera == null) yield break;
            Vector3 origPos = playerCamera.transform.localPosition;
            Vector3 flinchOffset = -playerCamera.transform.forward * 0.12f + Vector3.up * 0.05f;
            playerCamera.transform.localPosition = origPos + flinchOffset;
            yield return new WaitForSeconds(0.08f);
            playerCamera.transform.localPosition = origPos;
        }

        // ── Physical Collision with Customer & Props ──────────────────────────

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            NPCShopperHealth npc = hit.collider.GetComponentInParent<NPCShopperHealth>();
            if (npc != null && !npc.IsDead)
            {
                float speed = controller.velocity.magnitude;
                Vector3 bumpDir = (npc.transform.position - transform.position);
                bumpDir.y = 0f;
                if (bumpDir.sqrMagnitude < 0.001f) bumpDir = transform.forward;
                bumpDir.Normalize();

                // Physical knockback / push so cart never clips through customer
                NPCShopperAI ai = npc.GetComponent<NPCShopperAI>();
                if (ai != null)
                {
                    float force = isRamming ? 12f : (isPushingCart ? Mathf.Max(2.5f, speed * 1.2f) : 2.0f);
                    ai.AddKnockback(bumpDir * force);
                }

                // Register cart bump: after 2 to 3 hits, customer gets angry and strikes back!
                if (isRamming || speed > 1.0f || (Time.time - lastBumpRegisterTime > 0.5f && speed > 0.35f))
                {
                    lastBumpRegisterTime = Time.time;
                    npc.RegisterCartBump(bumpDir, isRamming ? ramSpeed : speed, isRamming);
                    if (hud != null) hud.ShowHitMarker();
                }
            }

            // Ramming obstacles / props
            if (isRamming)
            {
                Rigidbody hitRb = hit.collider.attachedRigidbody;
                if (hitRb != null && !hitRb.isKinematic)
                {
                    Vector3 bashDir = (hit.point - transform.position).normalized + Vector3.up * 0.3f;
                    hitRb.AddForce(bashDir * 400f, ForceMode.Impulse);
                }

                GroceryItem item = hit.collider.GetComponentInParent<GroceryItem>();
                if (item != null)
                {
                    item.TakeDamage(ramDamage);
                }
            }
        }

        private void ResolveCustomerPushback()
        {
            // Proactive overlap check around cart front so player cart never phases through customers
            Vector3 cartFront = isPushingCart ? transform.position + transform.forward * 0.95f + Vector3.up * 0.5f : transform.position + Vector3.up * 0.5f;
            float checkRadius = isPushingCart ? 0.95f : 0.55f;

            Collider[] hits = Physics.OverlapSphere(cartFront, checkRadius);
            foreach (Collider c in hits)
            {
                if (c == null) continue;
                if (c.transform.IsChildOf(transform)) continue;
                if (cartModelTransform != null && c.transform.IsChildOf(cartModelTransform)) continue;

                NPCShopperAI ai = c.GetComponentInParent<NPCShopperAI>();
                if (ai != null)
                {
                    Vector3 push = (ai.transform.position - transform.position);
                    push.y = 0f;
                    float dist = push.magnitude;
                    if (dist < checkRadius && dist > 0.01f)
                    {
                        Vector3 pushDir = push.normalized;
                        ai.AddKnockback(pushDir * (checkRadius - dist + 0.15f) * 6f);
                    }
                }
            }
        }
    }
}
