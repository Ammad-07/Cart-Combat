using UnityEngine;

namespace CartCombat.Store
{
    /// <summary>
    /// Designates spawn locations and orientations for player and opponent combat carts.
    /// </summary>
    public class CartSpawnPoint : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private bool isPlayer = false;
        [SerializeField] private int spawnIndex = 0;
        [SerializeField] private string cartType = "Standard Shopping Cart";

        public bool IsPlayer => isPlayer;
        public int SpawnIndex => spawnIndex;
        public string CartType => cartType;

        public void Initialize(bool player, int index)
        {
            isPlayer = player;
            spawnIndex = index;
            gameObject.name = isPlayer ? "SpawnPoint_Player" : $"SpawnPoint_Rival_{index}";
        }

        // Gizmo visuals disabled for a clean, unobstructed scene view
        // To re-enable when setting up spawn points, change to OnDrawGizmosSelected
        private void OnDrawGizmosSelected()
        {
            // Only visible if explicitly selected in Hierarchy
            Gizmos.color = isPlayer ? new Color(0.1f, 0.9f, 0.2f, 0.9f) : new Color(0.9f, 0.2f, 0.1f, 0.9f);
            
            // Draw base disc/sphere
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.3f, 0.6f);

            // Draw forward direction pointer
            Vector3 start = transform.position + Vector3.up * 0.3f;
            Vector3 forward = transform.forward * 1.5f;
            Gizmos.DrawRay(start, forward);
            Gizmos.DrawRay(start + forward, (transform.right - transform.forward) * 0.3f);
            Gizmos.DrawRay(start + forward, (-transform.right - transform.forward) * 0.3f);
        }
    }
}
