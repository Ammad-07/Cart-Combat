using System.Collections;
using UnityEngine;

namespace CartCombat.NPC
{
    /// <summary>
    /// Health, hit-reaction, knockback, death, and respawn system for NPC shoppers.
    /// Flashes the NPC's body red when hit. When HP drops below the fight-back
    /// threshold the NPC enters aggressive mode and fights back.
    /// </summary>
    public class NPCShopperHealth : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Hit Reaction")]
        [SerializeField] private float hitFlashDuration = 0.22f;
        [SerializeField] private float stunDuration     = 0.75f;
        [SerializeField] private float knockbackForce   = 5.5f;

        [Header("Fight-Back")]
        [SerializeField] private float fightBackThreshold  = 60f;  // enter fight mode below this
        [SerializeField] private float counterAttackDamage = 5.0f; // 5% damage per hit (5 HP out of 100)
        [SerializeField] private float counterAttackRange  = 2.4f;
        [SerializeField] private float counterAttackCooldown = 1.4f;

        [Header("Cart Bump Aggro")]
        [SerializeField] private int bumpCount = 0;
        [SerializeField] private int bumpsToAggro = 3; // Enrage strictly after 3 hits!
        [SerializeField] private bool isEnraged = false;
        private float lastBumpTime = -10f;

        // References
        private NPCShopperAI ai;
        private Renderer[]   bodyRenderers;
        private Color[]      originalColors;
        private Material[]   originalMats;

        private bool  isDead = false;
        private float lastCounterTime = -99f;
        private bool  isFlashing = false;

        public bool  IsDead       => isDead;
        public float HealthPercent => currentHealth / maxHealth;
        public bool  IsAggressive  => (isEnraged || currentHealth < fightBackThreshold) && !isDead;
        public int   BumpCount     => bumpCount;

        // ── Unity ────────────────────────────────────────────────────────────

        private void Awake()
        {
            currentHealth = maxHealth;
            bumpsToAggro = 3; // Trigger fight strictly on the 3rd hit!
            counterAttackDamage = 5.0f; // Exactly 5% damage (5 HP per hit)
            ai = GetComponent<NPCShopperAI>();

            // Cache body renderers and their original material colours
            bodyRenderers  = GetComponentsInChildren<Renderer>();
            originalColors = new Color[bodyRenderers.Length];
            originalMats   = new Material[bodyRenderers.Length];

            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] != null)
                {
                    originalMats[i]   = bodyRenderers[i].sharedMaterial;
                    originalColors[i] = bodyRenderers[i].sharedMaterial != null
                        ? bodyRenderers[i].sharedMaterial.color
                        : Color.white;
                }
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Deal damage from any source (throw, ram, punch).</summary>
        public void TakeDamage(float amount, Vector3 hitDirection)
        {
            if (isDead) return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            Debug.Log($"[NPC] {name} took {amount:F0} dmg → HP {currentHealth:F0}/{maxHealth}");

            if (!isFlashing) StartCoroutine(HitFlash());
            ApplyKnockback(hitDirection);

            if (ai != null) ai.OnHurt(hitDirection, stunDuration);

            if (currentHealth <= 0f) StartCoroutine(DieSequence());
        }

        /// <summary>Attempt a melee counter-attack on the player.</summary>
        public bool TryCounterAttack(Transform target)
        {
            if (isDead) return false;
            if (Time.time < lastCounterTime + counterAttackCooldown) return false;
            if (target == null) return false;
            if (Vector3.Distance(transform.position, target.position) > counterAttackRange) return false;

            lastCounterTime = Time.time;
            StartCoroutine(CounterFlash());

            // Animate swing if customer has a grocery item or basket in hand
            Transform heldItem = transform.Find("Visual/Hand_R/Customer_HeldWeapon")
                              ?? transform.Find("Visual/ShoppingBasket")
                              ?? transform.Find("Visual/Hand_R");
            if (heldItem != null)
            {
                StartCoroutine(AnimateCustomerSwing(heldItem));
            }

            // Deal 5% damage to player controller (5 HP out of 100)
            CartCombat.Player.CartPlayerController pc = target.GetComponentInParent<CartCombat.Player.CartPlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(counterAttackDamage, transform.forward);
            }

            string weaponName = (heldItem != null && heldItem.name.Contains("Weapon")) ? "held grocery weapon" : "fist strike";
            Debug.Log($"<color=#FF3333><b>[Customer Combat]</b></color> {name} strikes player with {weaponName} for {counterAttackDamage:F0} dmg (5% HP)! Player HP: {pc?.Health:F0}");
            return true;
        }

        private IEnumerator AnimateCustomerSwing(Transform itemTrans)
        {
            Quaternion origRot = itemTrans.localRotation;
            Quaternion swingRot = origRot * Quaternion.Euler(60f, -25f, 0f);

            float dur = 0.12f;
            float el = 0f;
            while (el < dur)
            {
                itemTrans.localRotation = Quaternion.Slerp(origRot, swingRot, el / dur);
                el += Time.deltaTime;
                yield return null;
            }

            el = 0f;
            while (el < dur)
            {
                itemTrans.localRotation = Quaternion.Slerp(swingRot, origRot, el / dur);
                el += Time.deltaTime;
                yield return null;
            }

            itemTrans.localRotation = origRot;
        }

        /// <summary>
        /// Called when the player's cart bumps into the customer.
        /// After exactly 3 hits, the customer gets angry, turns, and fights back!
        /// </summary>
        public void RegisterCartBump(Vector3 hitDirection, float speed, bool wasRam)
        {
            if (isDead) return;
            if (Time.time < lastBumpTime + 0.35f) return;
            lastBumpTime = Time.time;

            bumpCount++;
            Debug.Log($"<color=#FF9900><b>[Customer Bump]</b></color> {name} hit by cart ({bumpCount}/{bumpsToAggro}) at speed {speed:F1} m/s");

            float dmg = wasRam ? 25f : 8f;
            currentHealth = Mathf.Max(1f, currentHealth - dmg);

            if (!isFlashing) StartCoroutine(HitFlash());
            ApplyKnockback(hitDirection * (wasRam ? 1.6f : 1.0f));

            if (bumpCount >= 3 || wasRam)
            {
                TriggerEnragedFight(hitDirection);
            }
            else
            {
                if (ai != null) ai.OnHurt(hitDirection, 0.45f);
            }
        }

        public void TriggerEnragedFight(Vector3 hitDirection)
        {
            if (isDead || isEnraged) return;
            isEnraged = true;
            fightBackThreshold = maxHealth + 10f;
            Debug.Log($"<color=#FF2222><b>[CUSTOMER ENRAGED!]</b> {name} has been hit {bumpCount} times and is now FIGHTING back!</color>");

            StartCoroutine(AngerYellFlash());
            if (ai != null)
            {
                ai.SetAggressiveState();
            }
        }

        private IEnumerator AngerYellFlash()
        {
            for (int f = 0; f < 3; f++)
            {
                SetAllBodyColor(new Color(1f, 0.15f, 0.05f));
                yield return new WaitForSeconds(0.12f);
                RestoreBodyColors();
                yield return new WaitForSeconds(0.10f);
            }
        }

        // ── Private ──────────────────────────────────────────────────────────

        private void ApplyKnockback(Vector3 dir)
        {
            Vector3 kb = dir.normalized * knockbackForce;
            kb.y = 1.5f;  // slight vertical pop

            if (ai != null)
            {
                ai.AddKnockback(kb);
            }
        }

        // ── Coroutines ───────────────────────────────────────────────────────

        private IEnumerator HitFlash()
        {
            isFlashing = true;
            SetAllBodyColor(Color.red);
            yield return new WaitForSeconds(hitFlashDuration);
            RestoreBodyColors();
            isFlashing = false;
        }

        private IEnumerator CounterFlash()
        {
            SetAllBodyColor(new Color(1f, 0.45f, 0f));  // orange = attack
            yield return new WaitForSeconds(0.14f);
            RestoreBodyColors();
        }

        private IEnumerator DieSequence()
        {
            isDead = true;
            Debug.Log($"[NPC] {name} knocked out!");

            if (ai != null) ai.enabled = false;

            // Collapse animation: tilt and sink
            float elapsed  = 0f;
            float duration = 1.0f;
            Vector3    startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Quaternion fallRot  = Quaternion.Euler(88f, startRot.eulerAngles.y, 0f);

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.rotation = Quaternion.Slerp(startRot, fallRot, t);
                transform.position = Vector3.Lerp(startPos,
                    startPos - Vector3.up * 0.55f, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Fade out
            yield return new WaitForSeconds(4.0f);
            yield return FadeOut(0.8f);

            // Respawn
            Respawn(startPos);
        }

        private IEnumerator FadeOut(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float a = 1f - (elapsed / duration);
                foreach (Renderer r in bodyRenderers)
                {
                    if (r == null) continue;
                    Material m = r.material; // instance
                    Color c = m.color; c.a = a; m.color = c;
                    if (m.HasProperty("_BaseColor"))
                    { Color bc = m.GetColor("_BaseColor"); bc.a = a; m.SetColor("_BaseColor", bc); }
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private void Respawn(Vector3 originalPos)
        {
            isDead        = false;
            currentHealth = maxHealth;

            transform.position = originalPos + Vector3.up * 0.05f;
            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            RestoreBodyColors();

            if (ai != null)
            {
                ai.enabled = true;
                ai.ResetState();
            }

            Debug.Log($"[NPC] {name} recovered!");
        }

        // ── Color helpers ────────────────────────────────────────────────────

        private void SetAllBodyColor(Color c)
        {
            foreach (Renderer r in bodyRenderers)
            {
                if (r == null) continue;
                Material m = r.material;
                m.color = c;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            }
        }

        private void RestoreBodyColors()
        {
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] == null) continue;
                Material m = bodyRenderers[i].material;
                m.color = originalColors[i];
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", originalColors[i]);
            }
        }
    }
}
