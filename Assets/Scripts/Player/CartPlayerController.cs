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
    /// Player character controller pushing the combat shopping cart.
    /// Supports grabbing, ungrabbing/dropping, wielding grocery items as melee weapons
    /// (hitting/swinging bananas, apples, chips, soda cans, fish, baguettes, melons),
    /// throwing grocery physics projectiles, cart basket storage, and high-speed cart ram attacks.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CartPlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 6.5f;
        [SerializeField] private float sprintSpeed = 12.0f;
        [SerializeField] private float reverseSpeed = 3.5f;
        [SerializeField] private float jumpForce = 5.5f;
        [SerializeField] private float gravity = 20.0f;

        [Header("Cart Combat Mechanics")]
        [SerializeField] private float ramSpeed = 18.0f;
        [SerializeField] private float ramDuration = 0.45f;
        [SerializeField] private float ramCooldown = 1.5f;
        [SerializeField] private float ramDamage = 40.0f;
        [SerializeField] private float itemThrowForce = 24.0f;
        [SerializeField] private float pickupRadius = 2.8f;

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
        private float lastRamTime = -10f;
        private float currentSteerTilt = 0f;
        private ShopperHUD hud;

        public bool IsRamming => isRamming;
        public bool IsSwinging => isSwinging;
        public GroceryItem HeldItem => heldItem;
        public int ItemCount => (heldItem != null ? 1 : 0) + carriedItems.Count;
        public IReadOnlyList<GroceryItem> CarriedItems => carriedItems;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;

            // Basket slot on the cart
            if (cartBasketSlot == null)
            {
                GameObject slot = new GameObject("CartBasketSlot");
                slot.transform.SetParent(transform, false);
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
        }

        private void Update()
        {
            HandleMovement();
            HandleActions();
        }

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
            if (controller.isGrounded)
            {
                verticalVelocity = -1.0f;
                if (jumpPressed && !isRamming)
                {
                    verticalVelocity = jumpForce;
                }
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
            }

            if (!isRamming)
            {
                float targetSpeed = (inputZ < 0f) ? reverseSpeed : (isSprinting ? sprintSpeed : walkSpeed);
                Vector3 inputVec = (transform.forward * inputZ + transform.right * inputX).normalized;
                moveDirection = inputVec * targetSpeed;

                // Subtle cart lean when steering
                float targetTilt = -inputX * steerTiltAmount;
                currentSteerTilt = Mathf.Lerp(currentSteerTilt, targetTilt, Time.deltaTime * 10f);
                if (cartModelTransform != null)
                {
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

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                grabPressed = Keyboard.current.eKey.wasPressedThisFrame;
                dropPressed = Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.gKey.wasPressedThisFrame;
                attackPressed = Keyboard.current.fKey.wasPressedThisFrame;
                swapPressed = Keyboard.current.tabKey.wasPressedThisFrame;
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
#endif

            // 1. Grab item from shelf or ground
            if (grabPressed)
            {
                TryPickupNearbyItem();
            }

            // 2. Ungrab / Drop currently held item onto the ground
            if (dropPressed)
            {
                DropHeldItem();
            }

            // 3. Attack (Melee swing with held weapon, or Cart Ram if empty-handed)
            if (attackPressed)
            {
                if (heldItem != null)
                {
                    TryMeleeSwing();
                }
                else
                {
                    TryRamAttack();
                }
            }

            // 4. Throw held item as a projectile
            if (throwPressed && heldItem != null)
            {
                ThrowHeldItem();
            }

            // 5. Swap held item with basket item
            if (swapPressed)
            {
                CycleBasketItem();
            }
        }

        // ── Item Pickup & Equipping ──────────────────────────────────────────

        private void TryPickupNearbyItem()
        {
            // First try precise camera raycast (aiming directly at an item on a shelf)
            Ray ray = (playerCamera != null)
                ? playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
                : new Ray(transform.position + Vector3.up * 1.2f, transform.forward);

            GroceryItem targetItem = null;

            if (Physics.Raycast(ray, out RaycastHit hit, 3.8f))
            {
                GroceryItem item = hit.collider.GetComponentInParent<GroceryItem>();
                if (item != null && item != heldItem && !carriedItems.Contains(item) && item.CanBePickedUp)
                {
                    targetItem = item;
                }
            }

            // Fallback: OverlapSphere around player
            if (targetItem == null)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 0.9f + Vector3.up * 0.5f, pickupRadius);
                float closestDist = float.MaxValue;

                foreach (Collider c in hits)
                {
                    GroceryItem item = c.GetComponentInParent<GroceryItem>();
                    if (item != null && item != heldItem && !carriedItems.Contains(item) && item.CanBePickedUp)
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

            if (targetItem != null)
            {
                PickUpItem(targetItem);
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
            // Otherwise, store in cart basket
            else if (carriedItems.Count < maxBasketCapacity)
            {
                StoreInBasket(item);
                Debug.Log($"[Cart Combat] Stored {item.ItemName} in cart basket ({carriedItems.Count}/{maxBasketCapacity}).");
            }
            else
            {
                Debug.Log("[Cart Combat] Cart basket is full! Press [Q] to drop or [LMB] to swing!");
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
                // Take out of basket and drop
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

            // Swap held item with first item in basket
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

            // Spherecast forward for generous, responsive melee hit detection
            RaycastHit[] hits = Physics.SphereCastAll(origin, 0.45f, forward, range);

            bool hitTarget = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;

                // Check for NPC Shopper
                NPCShopperHealth npc = hit.collider.GetComponentInParent<NPCShopperHealth>();
                if (npc != null && !npc.IsDead)
                {
                    Vector3 hitDir = (npc.transform.position - transform.position).normalized + Vector3.up * 0.2f;
                    bool broke = heldItem.ApplyMeleeHit(npc, hitDir);

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

            // Auto-equip next weapon from basket
            EquipNextFromBasket();
        }

        // ── Cart Ramming Attack ──────────────────────────────────────────────

        private void TryRamAttack()
        {
            if (Time.time < lastRamTime + ramCooldown || isRamming) return;
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

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // If ramming, bash destructible props, rival carts, or NPC shoppers
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

                // RAM hits NPC shoppers
                NPCShopperHealth npc = hit.collider.GetComponentInParent<NPCShopperHealth>();
                if (npc != null)
                {
                    Vector3 hitDir = (hit.transform.position - transform.position).normalized;
                    npc.TakeDamage(ramDamage, hitDir);
                    if (hud != null)
                    {
                        hud.ShowHitMarker();
                        hud.AddScore(50f);
                    }
                    Debug.Log($"[Cart Combat] RAM hit NPC shopper for {ramDamage} dmg!");
                }
            }
        }
    }
}
