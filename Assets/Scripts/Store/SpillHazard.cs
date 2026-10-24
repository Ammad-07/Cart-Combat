using UnityEngine;

namespace CartCombat.Store
{
    public enum HazardType
    {
        BananaPeel,
        WetFloorPuddle,
        OilSlick,
        BrokenGlass
    }

    /// <summary>
    /// Supermarket floor hazard (banana peel, spilled liquid, wet floor).
    /// Causes shopping carts to slide, lose traction, or spin out when driven over.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SpillHazard : MonoBehaviour
    {
        [Header("Hazard Configuration")]
        [SerializeField] private HazardType hazardType = HazardType.BananaPeel;
        [SerializeField] private float slipDuration = 1.5f;
        [SerializeField] private float spinForce = 50f;
        [SerializeField] private bool destroyOnTrigger = false;

        public HazardType Type => hazardType;
        public float SlipDuration => slipDuration;

        public void Initialize(HazardType type)
        {
            hazardType = type;
            gameObject.name = $"Hazard_{hazardType}";
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if cart or rigidbody drove over hazard
            Rigidbody cartRb = other.attachedRigidbody;
            if (cartRb != null)
            {
                // Apply chaotic spin impulse to the combat cart
                Vector3 randomTorque = new Vector3(0f, Random.Range(-spinForce, spinForce), 0f);
                cartRb.AddTorque(randomTorque, ForceMode.Impulse);

                if (destroyOnTrigger)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
