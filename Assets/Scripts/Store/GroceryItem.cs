using System.Collections;
using UnityEngine;
using CartCombat.NPC;

namespace CartCombat.Store
{
    public enum GroceryCategory
    {
        Cereal,
        CannedGoods,
        Beverage,
        Produce,
        Bakery,
        DairyFrozen,
        CleaningSupplies,
        SpecialWeapon
    }

    public enum GroceryItemType
    {
        Banana,
        Apple,
        Chips,
        SodaCan,
        SodaBottle,
        Fish,
        Baguette,
        Watermelon,
        CerealBox,
        CannedSoup,
        CleaningJug
    }

    /// <summary>
    /// Interactive 3D grocery item that can sit on shelves/tables, be knocked around by cart ramming,
    /// grabbed by the player, wielded as a melee weapon (hit/swing), thrown as a physics projectile,
    /// or dropped/ungrabbed back into the store.
    /// </summary>
    public class GroceryItem : MonoBehaviour
    {
        [Header("Item Identity")]
        [SerializeField] private GroceryItemType itemType = GroceryItemType.Banana;
        [SerializeField] private string itemName = "Banana";
        [SerializeField] private GroceryCategory category = GroceryCategory.Produce;
        [SerializeField] private int scoreValue = 40;

        [Header("Weapon Combat Stats")]
        [SerializeField] private float meleeDamage = 18f;
        [SerializeField] private float throwDamage = 25f;
        [SerializeField] private float meleeRange = 2.2f;
        [SerializeField] private float swingSpeed = 0.22f;
        [SerializeField] private int maxDurability = 4;
        [SerializeField] private int currentDurability = 4;
        [SerializeField] private string weaponDescription = "Slippery slap weapon";

        [Header("State")]
        [SerializeField] private bool canBePickedUp = true;
        [SerializeField] private bool canBeThrown = true;
        [SerializeField] private bool isBroken = false;
        [SerializeField] private bool isWielded = false;

        [Header("Physics")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Collider col;

        public GroceryItemType ItemType => itemType;
        public string ItemName => itemName;
        public GroceryCategory Category => category;
        public int ScoreValue => scoreValue;
        public float MeleeDamage => meleeDamage;
        public float ThrowDamage => throwDamage;
        public float MeleeRange => meleeRange;
        public float SwingSpeed => swingSpeed;
        public int MaxDurability => maxDurability;
        public int CurrentDurability => currentDurability;
        public string WeaponDescription => weaponDescription;
        public bool CanBePickedUp => canBePickedUp && !isBroken;
        public bool CanBeThrown => canBeThrown && !isBroken;
        public bool IsBroken => isBroken;
        public bool IsWielded => isWielded;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (col == null) col = GetComponent<Collider>();
            if (currentDurability <= 0) currentDurability = maxDurability;
        }

        public void Initialize(GroceryItemType type, string name, GroceryCategory cat,
            int score, float mDmg, float tDmg, int durability, string desc)
        {
            itemType = type;
            itemName = name;
            category = cat;
            scoreValue = score;
            meleeDamage = mDmg;
            throwDamage = tDmg;
            maxDurability = durability;
            currentDurability = durability;
            weaponDescription = desc;
            gameObject.name = $"Item_{itemType}_{itemName.Replace(" ", "")}";
        }

        /// <summary>
        /// Called when the player executes a melee swing that impacts an NPC.
        /// </summary>
        public bool ApplyMeleeHit(NPCShopperHealth target, Vector3 hitDirection)
        {
            if (isBroken || target == null) return false;

            target.TakeDamage(meleeDamage, hitDirection);
            currentDurability--;

            Debug.Log($"[Cart Combat] MELEE HIT with {itemName}! Dealt {meleeDamage} dmg (Durability: {currentDurability}/{maxDurability})");

            if (currentDurability <= 0)
            {
                BreakItem(hitDirection);
                return true; // Item broke on hit
            }
            return false;
        }

        /// <summary>
        /// Configure item when wielded in hand or carried in basket.
        /// </summary>
        public void SetWielded(bool wielded)
        {
            isWielded = wielded;
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (col == null) col = GetComponent<Collider>();

            if (wielded)
            {
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                if (col != null) col.enabled = false;
            }
            else
            {
                if (col != null) col.enabled = true;
                if (rb != null) rb.isKinematic = false;
            }
        }

        /// <summary>
        /// Drops / ungrabs the item into the world with physics.
        /// </summary>
        public void DropToGround(Vector3 worldPos, Vector3 dropVelocity)
        {
            SetWielded(false);
            transform.SetParent(null);
            transform.position = worldPos;

            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.linearVelocity = dropVelocity;
            rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.Impulse);

            Debug.Log($"[Cart Combat] Dropped {itemName} onto the ground.");
        }

        /// <summary>
        /// Throws item forward as a physics weapon projectile.
        /// </summary>
        public void LaunchProjectile(Vector3 origin, Vector3 velocity)
        {
            SetWielded(false);
            transform.SetParent(null);
            transform.position = origin;

            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.linearVelocity = velocity;
            rb.AddTorque(Random.insideUnitSphere * 20f, ForceMode.Impulse);

            Debug.Log($"[Cart Combat] Threw {itemName} with velocity {velocity.magnitude:F1} m/s!");
        }

        public void TakeDamage(float amount)
        {
            currentDurability -= Mathf.CeilToInt(amount);
            if (currentDurability <= 0 && !isBroken)
            {
                BreakItem(Vector3.down);
            }
        }

        private void OnCollisionEnter(Collision col)
        {
            // When thrown with sufficient velocity and hits an NPC shopper
            if (isWielded || isBroken) return;
            if (rb == null || rb.isKinematic) return;
            if (rb.linearVelocity.magnitude < 3f) return;

            NPCShopperHealth npc = col.collider.GetComponentInParent<NPCShopperHealth>();
            if (npc != null)
            {
                Vector3 hitDir = rb.linearVelocity.normalized;
                npc.TakeDamage(throwDamage, hitDir);
                currentDurability--;

                Debug.Log($"[Cart Combat] THROW HIT: {itemName} hit NPC for {throwDamage} dmg! (Durability: {currentDurability}/{maxDurability})");

                if (currentDurability <= 0)
                {
                    BreakItem(hitDir);
                }
            }
        }

        public void BreakItem(Vector3 impactDirection)
        {
            if (isBroken) return;
            isBroken = true;

            Debug.Log($"[Cart Combat] {itemName} shattered / broke!");

            // Spawn procedural shatter debris particles
            SpawnBreakDebris(impactDirection);

            // Destroy item GameObject
            Destroy(gameObject, 0.05f);
        }

        private void SpawnBreakDebris(Vector3 dir)
        {
            Color debrisColor = GetDebrisColor();
            GameObject debrisRoot = new GameObject($"Debris_{itemName}");
            debrisRoot.transform.position = transform.position;

            for (int i = 0; i < 6; i++)
            {
                GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.transform.SetParent(debrisRoot.transform);
                chunk.transform.position = transform.position + Random.insideUnitSphere * 0.2f;
                chunk.transform.localScale = Vector3.one * Random.Range(0.06f, 0.12f);

                Renderer r = chunk.GetComponent<Renderer>();
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                m.color = debrisColor;
                r.sharedMaterial = m;

                Rigidbody chunkRb = chunk.AddComponent<Rigidbody>();
                chunkRb.mass = 0.1f;
                chunkRb.linearVelocity = (dir + Random.insideUnitSphere * 1.5f).normalized * Random.Range(3f, 7f);

                Destroy(chunk, 2.5f);
            }
            Destroy(debrisRoot, 3.0f);
        }

        private Color GetDebrisColor()
        {
            switch (itemType)
            {
                case GroceryItemType.Banana: return new Color(1.0f, 0.88f, 0.2f);
                case GroceryItemType.Apple: return new Color(0.9f, 0.15f, 0.15f);
                case GroceryItemType.Chips: return new Color(1.0f, 0.75f, 0.2f);
                case GroceryItemType.Fish: return new Color(0.35f, 0.65f, 0.85f);
                case GroceryItemType.Baguette: return new Color(0.85f, 0.60f, 0.3f);
                case GroceryItemType.Watermelon: return new Color(0.85f, 0.15f, 0.25f);
                case GroceryItemType.SodaCan: return new Color(0.2f, 0.7f, 1.0f);
                default: return Color.white;
            }
        }
    }
}
