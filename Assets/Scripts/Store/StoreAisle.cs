using System.Collections.Generic;
using UnityEngine;

namespace CartCombat.Store
{
    /// <summary>
    /// Represents a grocery aisle in the store, containing shelving units, endcaps, and stocked items.
    /// </summary>
    [SelectionBase]
    public class StoreAisle : MonoBehaviour
    {
        [Header("Aisle Information")]
        [SerializeField] private int aisleNumber = 1;
        [SerializeField] private string categoryName = "Snacks & Cereal";
        [SerializeField] private Color aisleThemeColor = new Color(0.2f, 0.6f, 0.9f);

        [Header("Hierarchy References")]
        [SerializeField] private Transform leftShelving;
        [SerializeField] private Transform rightShelving;
        [SerializeField] private Transform frontEndcap;
        [SerializeField] private Transform backEndcap;
        [SerializeField] private Transform aisleSignboard;

        [Header("Items")]
        [SerializeField] private List<GroceryItem> stockedItems = new List<GroceryItem>();

        public int AisleNumber => aisleNumber;
        public string CategoryName => categoryName;
        public IReadOnlyList<GroceryItem> StockedItems => stockedItems;

        public void Initialize(int number, string category, Color themeColor)
        {
            aisleNumber = number;
            categoryName = category;
            aisleThemeColor = themeColor;
            gameObject.name = $"Aisle_{number:D2}";
        }

        public void RegisterItem(GroceryItem item)
        {
            if (item != null && !stockedItems.Contains(item))
            {
                stockedItems.Add(item);
            }
        }

        // Bounding gizmos removed for a clean, distraction-free store view
    }
}
