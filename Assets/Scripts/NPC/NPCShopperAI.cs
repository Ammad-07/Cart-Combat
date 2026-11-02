using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CartCombat.Store;

namespace CartCombat.NPC
{
    /// <summary>
    /// AI brain for a cartoon NPC shopper in Cart Combat.
    ///
    /// Behaviour states:
    ///   Wandering  → walks patrol route through aisle corridors (never inside shelves)
    ///   Shopping   → pauses at shelf, bobs and "inspects" items
    ///   Fleeing    → runs away when first hurt but not yet angry
    ///   Fighting   → chases and counter-attacks player when HP is low
    ///   Stunned    → briefly cannot act after a big hit
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class NPCShopperAI : MonoBehaviour
    {
        // ── States ──────────────────────────────────────────────────────────

        public enum ShopperState { Wandering, Shopping, Fleeing, Fighting, Stunned }

        [Header("State (Read-only)")]
        [SerializeField] private ShopperState currentState = ShopperState.Wandering;

        // ── Movement ────────────────────────────────────────────────────────

        [Header("Movement")]
        [SerializeField] private float walkSpeed       = 2.2f;
        [SerializeField] private float runSpeed        = 4.8f;
        [SerializeField] private float rotateSpeed     = 6.0f;
        [SerializeField] private float gravity         = 22f;
        [SerializeField] private float waypointReachDist = 0.85f;
        [SerializeField] private float stuckTimeout    = 4.0f;   // pick new waypoint if stuck

        // ── Shopping ─────────────────────────────────────────────────────────

        [Header("Shopping")]
        [SerializeField] private float minShopPause = 2.0f;
        [SerializeField] private float maxShopPause = 5.0f;

        // ── Combat ──────────────────────────────────────────────────────────

        [Header("Combat")]
        [SerializeField] private float detectionRange  = 7.0f;
        [SerializeField] private float attackRange     = 1.9f;
        [SerializeField] private float throwCooldown   = 3.5f;
        [SerializeField] private float thrownItemForce = 13f;

        // ── Internals ────────────────────────────────────────────────────────

        private CharacterController cc;
        private NPCShopperHealth health;

        private Vector3 velocity     = Vector3.zero;   // horizontal + vertical
        private Vector3 knockback    = Vector3.zero;
        private Vector3 currentWP    = Vector3.zero;
        private bool    hasWP        = false;
        private float   stateTimer   = 0f;
        private float   stuckTimer   = 0f;
        private Vector3 lastPosition = Vector3.zero;

        private float   lastThrowTime = -10f;
        private Transform player;

        // Store-aware aisle corridor waypoints (set by NPCShopperSpawner)
        public List<Vector3> AisleWaypoints { get; set; } = new List<Vector3>();

        // Head transform for shopping bob animation
        private Transform headTransform;

        // Cartoon walk cycle
        private float walkCycleTimer = 0f;

        // ── Unity lifecycle ─────────────────────────────────────────────────

        private void Start()
        {
            cc     = GetComponent<CharacterController>();
            health = GetComponent<NPCShopperHealth>();

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;

            headTransform = transform.Find("Visual/Head");

            lastPosition = transform.position;
            PickWaypoint();
        }

        private void Update()
        {
            if (health != null && health.IsDead) return;

            ApplyGravity();
            DecayKnockback();
            RunStateMachine();
            ApplyWalkAnimation();

            // Final move
            Vector3 move = velocity + knockback;
            cc.Move(move * Time.deltaTime);

            // Stuck detection
            CheckStuck();
        }

        // ── State machine ────────────────────────────────────────────────────

        private void RunStateMachine()
        {
            switch (currentState)
            {
                case ShopperState.Wandering: StateWandering(); break;
                case ShopperState.Shopping:  StateShopping();  break;
                case ShopperState.Fleeing:   StateFleeing();   break;
                case ShopperState.Fighting:  StateFighting();  break;
                case ShopperState.Stunned:   StateStunned();   break;
            }
        }

        private void StateWandering()
        {
            // Transition → Fighting if low HP and player close
            if (health != null && health.IsAggressive && player != null
                && DistToPlayer() < detectionRange)
            {
                SetState(ShopperState.Fighting); return;
            }

            if (!hasWP) { PickWaypoint(); return; }

            MoveToward(currentWP, walkSpeed);

            if (Vector3.Distance(HorzPos(), HorzOf(currentWP)) < waypointReachDist)
            {
                bool doShop = (Random.value < 0.45f);
                if (doShop) SetState(ShopperState.Shopping);
                else        PickWaypoint();
            }
        }

        private void StateShopping()
        {
            SlowToStop();
            stateTimer -= Time.deltaTime;

            // Head bob while "inspecting"
            walkCycleTimer += Time.deltaTime * 2.5f;
            if (headTransform != null)
            {
                headTransform.localRotation = Quaternion.Euler(
                    Mathf.Sin(walkCycleTimer) * 10f,
                    Mathf.Sin(walkCycleTimer * 0.5f) * 18f,
                    0f);
            }

            if (stateTimer <= 0f) { PickWaypoint(); SetState(ShopperState.Wandering); }
        }

        private void StateFleeing()
        {
            if (player == null || (health != null && health.IsAggressive))
            {
                SetState(ShopperState.Fighting); return;
            }

            Vector3 fleeDir = (HorzPos() - HorzOf(player.position)).normalized;
            MoveInDir(fleeDir, runSpeed);

            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f) { PickWaypoint(); SetState(ShopperState.Wandering); }
        }

        private void StateFighting()
        {
            if (player == null || (health != null && !health.IsAggressive))
            {
                PickWaypoint(); SetState(ShopperState.Wandering); return;
            }

            float dist = DistToPlayer();

            if (dist > attackRange)
            {
                MoveToward(player.position, runSpeed);
            }
            else
            {
                SlowToStop();
                FaceTarget(player.position);
                if (health != null) health.TryCounterAttack(player);
            }

            if (Time.time > lastThrowTime + throwCooldown)
                TryThrowItem();
        }

        private void StateStunned()
        {
            SlowToStop();
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                SetState(health != null && health.IsAggressive
                    ? ShopperState.Fighting : ShopperState.Wandering);
            }
        }

        // ── Public callbacks ─────────────────────────────────────────────────

        public void OnHurt(Vector3 hitDir, float stunTime)
        {
            stateTimer = stunTime;
            SetState(ShopperState.Stunned);

            if (player != null) FaceTarget(player.position);
        }

        public void SetAggressiveState()
        {
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) player = playerObj.transform;
            }

            if (player != null)
            {
                FaceTarget(player.position);
            }

            SetState(ShopperState.Fighting);
        }

        public void AddKnockback(Vector3 force) => knockback += force;

        public void ResetState()
        {
            velocity   = Vector3.zero;
            knockback  = Vector3.zero;
            hasWP      = false;
            PickWaypoint();
            SetState(ShopperState.Wandering);
        }

        // ── Movement helpers ─────────────────────────────────────────────────

        private void MoveToward(Vector3 target, float speed)
        {
            Vector3 dir = HorzOf(target) - HorzPos();
            if (dir.sqrMagnitude < 0.01f) return;
            MoveInDir(dir.normalized, speed);
        }

        private void MoveInDir(Vector3 dir, float speed)
        {
            velocity.x = dir.x * speed;
            velocity.z = dir.z * speed;
            FaceDir(dir);
        }

        private void SlowToStop()
        {
            velocity.x = Mathf.Lerp(velocity.x, 0f, Time.deltaTime * 8f);
            velocity.z = Mathf.Lerp(velocity.z, 0f, Time.deltaTime * 8f);
        }

        private void FaceDir(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                Time.deltaTime * rotateSpeed);
        }

        private void FaceTarget(Vector3 target)
        {
            Vector3 dir = HorzOf(target) - HorzPos();
            FaceDir(dir.normalized);
        }

        private void ApplyGravity()
        {
            if (cc.isGrounded && velocity.y < 0f)
                velocity.y = -1.5f;
            else
                velocity.y -= gravity * Time.deltaTime;
        }

        private void DecayKnockback()
        {
            knockback = Vector3.Lerp(knockback, Vector3.zero, Time.deltaTime * 7f);
        }

        private void ApplyWalkAnimation()
        {
            float horzSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            bool isMoving = horzSpeed > 0.3f;

            if (isMoving)
            {
                walkCycleTimer += Time.deltaTime * horzSpeed * 2.5f;

                // Whole-body subtle tilt (left-right sway)
                float sway = Mathf.Sin(walkCycleTimer) * 2.5f;
                Transform visual = transform.Find("Visual");
                if (visual != null)
                    visual.localRotation = Quaternion.Euler(0f, 0f, sway);
            }
            else if (currentState != ShopperState.Shopping)
            {
                // Reset sway
                Transform visual = transform.Find("Visual");
                if (visual != null)
                    visual.localRotation = Quaternion.Lerp(visual.localRotation,
                        Quaternion.identity, Time.deltaTime * 5f);
            }
        }

        private void CheckStuck()
        {
            if (!hasWP || currentState != ShopperState.Wandering &&
                currentState != ShopperState.Fleeing &&
                currentState != ShopperState.Fighting) return;

            float moved = Vector3.Distance(transform.position, lastPosition);
            if (moved < 0.02f)
                stuckTimer += Time.deltaTime;
            else
                stuckTimer = 0f;

            lastPosition = transform.position;

            if (stuckTimer > stuckTimeout)
            {
                stuckTimer = 0f;
                PickWaypoint();
            }
        }

        // ── Waypoint selection ───────────────────────────────────────────────

        private void PickWaypoint()
        {
            hasWP = false;

            if (AisleWaypoints.Count > 0)
            {
                // Pick a random aisle corridor waypoint
                Vector3 chosen = AisleWaypoints[Random.Range(0, AisleWaypoints.Count)];
                // Add small random offset within corridor (±1m) so NPCs spread out
                chosen += new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-1.0f, 1.0f));
                currentWP = chosen;
            }
            else
            {
                // Fallback wander around spawn
                Vector2 rand = Random.insideUnitCircle * 8f;
                currentWP = transform.position + new Vector3(rand.x, 0f, rand.y);
            }

            hasWP = true;
        }

        // ── Throwing ─────────────────────────────────────────────────────────

        private void TryThrowItem()
        {
            if (player == null) return;
            lastThrowTime = Time.time;

            GameObject throwable = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            throwable.name = "NPC_ThrownItem";
            throwable.transform.localScale = Vector3.one * 0.20f;
            throwable.transform.position   = transform.position + Vector3.up * 1.4f + transform.forward * 0.5f;

            // Random cartoon colour
            Color[] cols = { Color.yellow, new Color(1f,0.4f,0f), Color.green, Color.cyan, Color.magenta };
            Renderer rend = throwable.GetComponent<Renderer>();
            if (rend != null)
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                m.color = cols[Random.Range(0, cols.Length)];
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", m.color);
                rend.sharedMaterial = m;
            }

            Rigidbody rb = throwable.AddComponent<Rigidbody>();
            Vector3 throwDir = ((player.position + Vector3.up) - throwable.transform.position).normalized;
            rb.linearVelocity = throwDir * thrownItemForce;
            rb.AddTorque(Random.insideUnitSphere * 12f, ForceMode.Impulse);

            throwable.AddComponent<NPCThrowableProjectile>().Initialize(player, 5f);

            Object.Destroy(throwable, 5f);
        }

        // ── Utility ──────────────────────────────────────────────────────────

        private void SetState(ShopperState s)
        {
            currentState = s;
            if (s == ShopperState.Shopping)
                stateTimer = Random.Range(minShopPause, maxShopPause);
            else if (s == ShopperState.Fleeing)
                stateTimer = Random.Range(2.5f, 4.5f);
        }

        private float DistToPlayer() =>
            player == null ? float.MaxValue
                           : Vector3.Distance(transform.position, player.position);

        private static Vector3 HorzOf(Vector3 v) => new Vector3(v.x, 0f, v.z);
        private Vector3 HorzPos() => new Vector3(transform.position.x, 0f, transform.position.z);

        // ── Gizmos ──────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            if (hasWP)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentWP);
                Gizmos.DrawSphere(currentWP, 0.25f);
            }

            Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
            foreach (var wp in AisleWaypoints)
                Gizmos.DrawSphere(wp, 0.18f);
        }
    }
}
