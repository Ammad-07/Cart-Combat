using UnityEngine;

namespace CartCombat.Store
{
    /// <summary>
    /// Represents a supermarket checkout station complete with conveyor, cash register, and lane signage.
    /// </summary>
    [SelectionBase]
    public class CheckoutLane : MonoBehaviour
    {
        [Header("Lane Settings")]
        [SerializeField] private int laneNumber = 1;
        [SerializeField] private bool isOpen = true;
        [SerializeField] private Light laneLight;

        [Header("Components")]
        [SerializeField] private Transform conveyorBelt;
        [SerializeField] private Transform cashRegisterStand;
        [SerializeField] private Transform dividerGate;
        [SerializeField] private Transform impulseRack;

        [Header("Conveyor Animation")]
        [SerializeField] private bool animateConveyor = false;
        [SerializeField] private float conveyorSpeed = 0.5f;

        public int LaneNumber => laneNumber;
        public bool IsOpen => isOpen;
        public float ConveyorSpeed => conveyorSpeed;

        public void Initialize(int number, bool open)
        {
            laneNumber = number;
            isOpen = open;
            gameObject.name = $"Checkout_Lane_{number:D2}";
            if (laneLight != null)
            {
                laneLight.color = isOpen ? Color.green : Color.red;
            }
        }

        private void Update()
        {
            if (animateConveyor && conveyorBelt != null)
            {
                // Subtle offset or animation for gameplay immersion
            }
        }
    }
}
