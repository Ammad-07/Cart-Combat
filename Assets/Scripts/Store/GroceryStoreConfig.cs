using System;
using UnityEngine;

namespace CartCombat.Store
{
    [System.Serializable]
    public class GroceryStoreConfig
    {
        [Header("Store Boundaries")]
        public float storeWidth = 36f;
        public float storeLength = 48f;
        public float wallHeight = 5.5f;

        [Header("Aisles Configuration")]
        public int aisleCount = 4;
        public float aisleLength = 22f;
        public float aisleSpacing = 5.5f;
        public float shelvingWidth = 2.2f;
        public float shelfHeight = 2.6f;
        public int shelfTiers = 4;

        [Header("Checkout Configuration")]
        public int checkoutLaneCount = 4;
        public float checkoutSpacing = 3.8f;
        public float checkoutZPosition = -16f;

        [Header("Lighting Configuration")]
        public bool generateCeilingLights = true;
        public int lightRowsX = 4;
        public int lightRowsZ = 6;
        public float lightIntensity = 25f;
        public Color lightColor = new Color(0.98f, 0.98f, 1f);

        [Header("Stock & Items")]
        public bool populateShelves = true;
        public int itemsPerShelfTier = 6;

        [Header("Hazards & Decor")]
        public bool spawnHazards = true;
        public int cautionSignCount = 3;
        public int bananaPeelCount = 4;

        [Header("Player & Combatants")]
        public bool spawnPlayerCharacter = true;
    }
}
