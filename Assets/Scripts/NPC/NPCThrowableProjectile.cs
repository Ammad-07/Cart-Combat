using UnityEngine;
using CartCombat.NPC;

namespace CartCombat.NPC
{
    /// <summary>
    /// Short-lived projectile spawned by an NPC when it throws an item.
    /// On collision with the player it deals damage and destroys itself.
    /// </summary>
    public class NPCThrowableProjectile : MonoBehaviour
    {
        private Transform target;
        private float damage;
        private bool  hasHit = false;

        public void Initialize(Transform playerTarget, float dmgAmount)
        {
            target = playerTarget;
            damage = dmgAmount;
        }

        private void OnCollisionEnter(Collision col)
        {
            if (hasHit) return;

            // Hit the player?
            if (target != null && (col.transform == target || col.transform.IsChildOf(target)))
            {
                hasHit = true;
                CartCombat.Player.CartPlayerController pc = col.transform.GetComponentInParent<CartCombat.Player.CartPlayerController>();
                if (pc != null)
                {
                    pc.TakeDamage(damage, transform.forward);
                }
                Debug.Log($"[NPC Projectile] Hit player for {damage} dmg!");

                // Visual pop: shrink and destroy
                Destroy(gameObject);
            }
            else if (!col.gameObject.CompareTag("NPC"))
            {
                // Hit anything that isn't another NPC → destroy
                Destroy(gameObject, 0.1f);
            }
        }
    }
}
