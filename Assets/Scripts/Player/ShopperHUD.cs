using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CartCombat.Store;
using CartCombat.NPC;

namespace CartCombat.Player
{
    /// <summary>
    /// Crisp, high-definition Heads-Up Display for the Cart Combat player.
    /// Built at runtime using TextMeshPro (SDF vector text) to eliminate any blurriness.
    ///
    /// Layout (1920x1080 Reference):
    ///   TOP-LEFT     : Health bar, numeric HP, cart speed
    ///   TOP-RIGHT    : High-contrast glowing Score display
    ///   BOTTOM-LEFT  : Cart basket inventory & armed item status
    ///   BOTTOM-RIGHT : Combat gauge (radial Ram cooldown ring & ammo)
    ///   CENTRE       : Minimalist crosshair + directional hit marker
    ///   BOTTOM-CENTRE: Action prompt pill with colored key badges
    /// </summary>
    [RequireComponent(typeof(CartPlayerController))]
    public class ShopperHUD : MonoBehaviour
    {
        // ── Visual Palette ───────────────────────────────────────────────────

        [Header("HUD Color Theme")]
        [SerializeField] private Color primaryCyan   = new Color(0.00f, 0.88f, 1.00f);
        [SerializeField] private Color vibrantGold   = new Color(1.00f, 0.80f, 0.15f);
        [SerializeField] private Color dangerRed     = new Color(1.00f, 0.25f, 0.25f);
        [SerializeField] private Color healthGreen   = new Color(0.20f, 0.95f, 0.45f);
        [SerializeField] private Color panelDark     = new Color(0.05f, 0.08f, 0.14f, 0.88f);
        [SerializeField] private Color headerMuted   = new Color(0.70f, 0.82f, 0.95f, 0.85f);

        // ── Runtime References ───────────────────────────────────────────────

        private CartPlayerController player;
        private GameObject           canvasGO;
        private Canvas               canvas;

        private Image           healthFill;
        private TextMeshProUGUI healthText;
        private TextMeshProUGUI speedText;
        private TextMeshProUGUI scoreText;
        private TextMeshProUGUI basketTitleText;
        private TextMeshProUGUI basketItemText;
        private TextMeshProUGUI basketHintText;
        private TextMeshProUGUI ammoText;
        private TextMeshProUGUI ramStatusText;
        private TextMeshProUGUI actionPrompt;

        private Image   ramCooldownFill;
        private Image   crosshairH, crosshairV;
        private Image   hitMarker;
        private float   hitMarkerTimer = 0f;

        private float   playerScore    = 0f;
        private float   playerHealth   = 100f;
        private float   maxHealth      = 100f;
        private float   lastRamTime    = -10f;
        private float   ramCooldown    = 1.5f;

        private const float HIT_MARKER_DURATION = 0.30f;

        private static TMP_FontAsset cachedFont;
        private static Sprite        cachedRingSprite;

        // ── Unity Callbacks ──────────────────────────────────────────────────

        private void Awake()
        {
            player = GetComponent<CartPlayerController>();
            BuildCanvas();
        }

        private void Update()
        {
            RefreshHUD();
            UpdateHitMarker();
        }

        private void OnDestroy()
        {
            if (canvasGO != null)
            {
                Destroy(canvasGO);
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void ShowHitMarker()
        {
            if (hitMarker != null)
            {
                hitMarker.color = new Color(1f, 0.25f, 0.25f, 1f);
                hitMarkerTimer  = HIT_MARKER_DURATION;
            }
        }

        public void NotifyRamUsed(float cooldown)
        {
            lastRamTime = Time.time;
            ramCooldown = cooldown;
            ShowHitMarker();
        }

        public void AddScore(float pts) => playerScore += pts;
        public void SetHealth(float hp, float maxHp) { playerHealth = hp; maxHealth = maxHp; }

        // ── Runtime Refresh ──────────────────────────────────────────────────

        private void RefreshHUD()
        {
            if (player == null) return;

            // Automatically sync health directly with player state
            playerHealth = player.Health;
            maxHealth = player.MaxHealth;

            // Health bar fill & numeric label
            float hpFrac = Mathf.Clamp01(playerHealth / Mathf.Max(1f, maxHealth));
            if (healthFill != null)
            {
                healthFill.fillAmount = hpFrac;
                healthFill.color = Color.Lerp(dangerRed, healthGreen, hpFrac);
            }
            if (healthText != null)
            {
                healthText.text = $"<color=#FF5555>❤</color> {playerHealth:F0} / {maxHealth:F0} HP";
            }

            // Speed display
            CharacterController cc = player.GetComponent<CharacterController>();
            float speed = cc != null ? cc.velocity.magnitude : 0f;
            if (speedText != null)
            {
                if (!player.IsPushingCart)
                {
                    speedText.text = $"🏃 {speed:F1} m/s";
                }
                else
                {
                    speedText.text = player.IsRamming
                        ? "<color=#FFD600>⚡ RAM BOOST!</color>"
                        : $"🛒 {speed:F1} m/s";
                }
            }

            // Score
            if (scoreText != null)
            {
                scoreText.text = $"{playerScore:N0}";
            }

            // Basket inventory & Wielded Weapon
            GroceryItem held = player.HeldItem;
            int basketCount = player.CarriedItems.Count;
            if (basketTitleText != null)
            {
                basketTitleText.text = player.IsPushingCart
                    ? $"CART BASKET  <b>({basketCount}/3)</b>"
                    : "ON FOOT  <b>(CART PARKED)</b>";
            }
            if (basketItemText != null)
            {
                if (held != null)
                {
                    string weaponName = held.ItemName.ToUpper();
                    basketItemText.text = $"WIELDING: <color=#00E5FF>{weaponName}</color>";
                }
                else
                {
                    basketItemText.text = "HANDS: <color=#00FF88>FISTS (BODY FIGHT READY)</color>";
                }
            }
            if (basketHintText != null)
            {
                if (held != null)
                {
                    basketHintText.text = $"DURABILITY: <color=#33FF77>{held.CurrentDurability}/{held.MaxDurability}</color>  ·  [F] Swing  ·  [T] Throw  ·  [Q] Drop";
                }
                else
                {
                    basketHintText.text = player.IsPushingCart
                        ? "[G] Body Attack  ·  [E] Grab Item  ·  [X] Leave Cart"
                        : "[G] Body Attack  ·  [E] Grab Item  ·  [X] Push Cart";
                }
            }

            // Ram cooldown radial ring & combat status
            float elapsed = Time.time - lastRamTime;
            float frac = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, ramCooldown));
            bool ramReady = frac >= 1f;

            if (ramCooldownFill != null)
            {
                ramCooldownFill.fillAmount = frac;
                ramCooldownFill.color = ramReady ? healthGreen : vibrantGold;
            }
            if (ramStatusText != null)
            {
                ramStatusText.text = ramReady
                    ? "<color=#33FF77>READY</color>"
                    : $"<color=#FFD600>{(ramCooldown - elapsed):F1}s</color>";
            }
            if (ammoText != null)
            {
                int totalWeapons = (held != null ? 1 : 0) + basketCount;
                ammoText.text = $"WEAPONS: <b>{totalWeapons}</b>";
            }

            // Action prompt pill (centered bottom)
            if (actionPrompt != null)
            {
                if (player.IsPushingCart)
                {
                    if (held != null)
                    {
                        actionPrompt.text = "<b><color=#00E5FF>[F]</color></b> Swing Weapon   <b><color=#00E5FF>[T]</color></b> Throw   <b><color=#FF7777>[Q]</color></b> Drop   <b><color=#00FF88>[X]</color></b> Leave Cart";
                    }
                    else
                    {
                        actionPrompt.text = "<b><color=#00E5FF>[G]</color></b> Body Attack   <b><color=#FFD600>[Shift]</color></b> Ram   <b><color=#FFD600>[E]</color></b> Grab Item   <b><color=#00FF88>[X]</color></b> Leave Cart";
                    }
                }
                else
                {
                    if (held != null)
                    {
                        actionPrompt.text = "<b><color=#00E5FF>[F]</color></b> Swing Weapon   <b><color=#00E5FF>[T]</color></b> Throw   <b><color=#FF7777>[Q]</color></b> Drop   <b><color=#00FF88>[X]</color></b> Push Cart";
                    }
                    else
                    {
                        actionPrompt.text = "<b><color=#00E5FF>[G]</color></b> Body Attack   <b><color=#FFD600>[E]</color></b> Grab Item   <b><color=#00FF88>[X]</color></b> Push Cart";
                    }
                }
            }
        }

        private void UpdateHitMarker()
        {
            if (hitMarkerTimer > 0f)
            {
                hitMarkerTimer -= Time.deltaTime;
                float a = Mathf.Clamp01(hitMarkerTimer / HIT_MARKER_DURATION);
                if (hitMarker != null)
                {
                    hitMarker.color = new Color(1f, 0.25f, 0.25f, a);
                }
            }
        }

        // ── Canvas Construction ──────────────────────────────────────────────

        private void BuildCanvas()
        {
            canvasGO = new GameObject("ShopperHUD_Canvas");

            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            scaler.dynamicPixelsPerUnit = 2f;

            canvasGO.AddComponent<GraphicRaycaster>();

            BuildTopLeftHealth(canvasGO.transform);
            BuildTopRightScore(canvasGO.transform);
            BuildBottomLeftInventory(canvasGO.transform);
            BuildBottomRightCombat(canvasGO.transform);
            BuildCentreCrosshair(canvasGO.transform);
            BuildBottomCentrePrompt(canvasGO.transform);
        }

        // ── Top-Left: Health & Speed ─────────────────────────────────────────

        private void BuildTopLeftHealth(Transform parent)
        {
            GameObject panel = MakePanel(parent, "Panel_Health",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, -24f),
                new Vector2(360f, 96f), panelDark);

            // Top accent stripe (cyan)
            MakeAccentStripe(panel.transform, "Accent_Top", primaryCyan);

            // Subheader label
            MakeText(panel.transform, "Label_Header",
                "SHOPPER  ·  CART COMBAT", 13f, headerMuted,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -12f), new Vector2(328f, 18f),
                TextAlignmentOptions.TopLeft, FontStyles.Bold);

            // Health bar background
            MakeImage(panel.transform, "HealthBar_BG",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -36f), new Vector2(328f, 22f),
                new Color(0.16f, 0.05f, 0.05f, 0.95f));

            // Health bar fill
            GameObject fillGO = MakeImage(panel.transform, "HealthBar_Fill",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -36f), new Vector2(328f, 22f),
                healthGreen);
            Image fill = fillGO.GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            healthFill = fill;

            // Numeric HP text (bottom-left of panel)
            healthText = MakeText(panel.transform, "Text_Health",
                "<color=#FF5555>❤</color> 100 / 100 HP", 16f, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -66f), new Vector2(180f, 22f),
                TextAlignmentOptions.TopLeft, FontStyles.Bold);

            // Speed text (bottom-right of panel)
            speedText = MakeText(panel.transform, "Text_Speed",
                "🛒 0.0 m/s", 16f, primaryCyan,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-16f, -66f), new Vector2(140f, 22f),
                TextAlignmentOptions.TopRight, FontStyles.Bold);
        }

        // ── Top-Right: Score ─────────────────────────────────────────────────

        private void BuildTopRightScore(Transform parent)
        {
            GameObject panel = MakePanel(parent, "Panel_Score",
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-24f, -24f),
                new Vector2(240f, 96f), panelDark);

            // Top accent stripe (gold)
            MakeAccentStripe(panel.transform, "Accent_Top", vibrantGold);

            // Header label
            MakeText(panel.transform, "Label_ScoreHeader",
                "SCORE", 13f, headerMuted,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-16f, -12f), new Vector2(208f, 18f),
                TextAlignmentOptions.TopRight, FontStyles.Bold);

            // Big glowing score number
            scoreText = MakeText(panel.transform, "Text_Score",
                "0", 40f, primaryCyan,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-16f, -36f), new Vector2(208f, 50f),
                TextAlignmentOptions.TopRight, FontStyles.Bold);
        }

        // ── Bottom-Left: Basket & Inventory ──────────────────────────────────

        private void BuildBottomLeftInventory(Transform parent)
        {
            GameObject panel = MakePanel(parent, "Panel_Inventory",
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 24f),
                new Vector2(340f, 96f), panelDark);

            MakeAccentStripe(panel.transform, "Accent_Top", primaryCyan);

            // Title
            basketTitleText = MakeText(panel.transform, "Text_BasketTitle",
                "CART BASKET  <b>(0/3)</b>", 13f, headerMuted,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -12f), new Vector2(308f, 18f),
                TextAlignmentOptions.TopLeft, FontStyles.Bold);

            // Armed item
            basketItemText = MakeText(panel.transform, "Text_BasketItem",
                "ARMED: <color=#AAAAAA>EMPTY (RAM ONLY)</color>", 18f, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -36f), new Vector2(308f, 26f),
                TextAlignmentOptions.TopLeft, FontStyles.Bold);

            // Hint
            basketHintText = MakeText(panel.transform, "Text_BasketHint",
                "[F] Ram Attack   ·   [E] Pick Up Item", 13f, new Color(0.7f, 0.75f, 0.85f, 0.8f),
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -66f), new Vector2(308f, 20f),
                TextAlignmentOptions.TopLeft, FontStyles.Normal);
        }

        // ── Bottom-Right: Combat Readiness ───────────────────────────────────

        private void BuildBottomRightCombat(Transform parent)
        {
            GameObject panel = MakePanel(parent, "Panel_Combat",
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-24f, 24f),
                new Vector2(260f, 96f), panelDark);

            MakeAccentStripe(panel.transform, "Accent_Top", vibrantGold);

            // Left side: Header & Ammo
            MakeText(panel.transform, "Label_CombatHeader",
                "COMBAT MODE", 13f, headerMuted,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -12f), new Vector2(150f, 18f),
                TextAlignmentOptions.TopLeft, FontStyles.Bold);

            ammoText = MakeText(panel.transform, "Text_Ammo",
                "AMMO: <b>0</b>", 20f, vibrantGold,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -36f), new Vector2(150f, 26f),
                TextAlignmentOptions.TopLeft, FontStyles.Bold);

            ramStatusText = MakeText(panel.transform, "Text_RamStatus",
                "RAM: <color=#33FF77>READY</color>", 13f, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -66f), new Vector2(150f, 20f),
                TextAlignmentOptions.TopLeft, FontStyles.Normal);

            // Right side: Ram cooldown circular ring gauge
            Vector2 ringPos = new Vector2(-46f, 48f);
            Vector2 ringSize = new Vector2(60f, 60f);

            // Outer dark ring background
            GameObject ringBgGO = MakeImage(panel.transform, "RamRing_BG",
                new Vector2(0.5f, 0.5f), new Vector2(1f, 0f),
                ringPos, ringSize,
                new Color(0.18f, 0.22f, 0.30f, 0.9f));
            Image ringBg = ringBgGO.GetComponent<Image>();
            ringBg.sprite = GetRingSprite();

            // Radial progress fill ring
            GameObject ringFillGO = MakeImage(panel.transform, "RamRing_Fill",
                new Vector2(0.5f, 0.5f), new Vector2(1f, 0f),
                ringPos, ringSize,
                healthGreen);
            Image ringFill = ringFillGO.GetComponent<Image>();
            ringFill.sprite = GetRingSprite();
            ringFill.type = Image.Type.Filled;
            ringFill.fillMethod = Image.FillMethod.Radial360;
            ringFill.fillOrigin = (int)Image.OriginVertical.Top;
            ringFill.fillClockwise = true;
            ringFill.fillAmount = 1f;
            ramCooldownFill = ringFill;

            // Center text inside ring
            MakeText(panel.transform, "Text_RamCenter",
                "RAM", 13f, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(1f, 0f),
                ringPos, ringSize,
                TextAlignmentOptions.Center, FontStyles.Bold);
        }

        // ── Centre Crosshair & Hit Marker ────────────────────────────────────

        private void BuildCentreCrosshair(Transform parent)
        {
            // Horizontal bar
            GameObject ch = MakeImage(parent, "Crosshair_H",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(18f, 2f),
                new Color(1f, 1f, 1f, 0.85f));
            crosshairH = ch.GetComponent<Image>();

            // Vertical bar
            GameObject cv = MakeImage(parent, "Crosshair_V",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(2f, 18f),
                new Color(1f, 1f, 1f, 0.85f));
            crosshairV = cv.GetComponent<Image>();

            // Center dot
            MakeImage(parent, "Crosshair_Dot",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(3f, 3f),
                primaryCyan);

            // Hit marker (4 diagonal notches, rotated square outline)
            GameObject hm = MakeImage(parent, "HitMarker",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(26f, 26f),
                new Color(1f, 0.25f, 0.25f, 0f));
            hm.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, 45f);
            hitMarker = hm.GetComponent<Image>();
            hitMarker.sprite = GetRingSprite();
        }

        // ── Bottom-Centre Context Prompt ─────────────────────────────────────

        private void BuildBottomCentrePrompt(Transform parent)
        {
            GameObject panel = MakePanel(parent, "Panel_Prompt",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 24f),
                new Vector2(640f, 46f), new Color(0.04f, 0.07f, 0.12f, 0.85f));

            // Bottom subtle accent
            GameObject stripe = MakeImage(panel.transform, "Accent_Bottom",
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                Vector2.zero, new Vector2(640f, 2f),
                new Color(0f, 0.88f, 1f, 0.5f));
            RectTransform srt = stripe.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 0f);
            srt.sizeDelta = new Vector2(0f, 2f);

            actionPrompt = MakeText(panel.transform, "Text_Prompt",
                "<b><color=#00E5FF>[F] / [LMB]</color></b> Ram Attack     <b><color=#FFD600>[E]</color></b> Grab Grocery Item",
                16f, new Color(0.85f, 0.90f, 0.98f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(620f, 36f),
                TextAlignmentOptions.Center, FontStyles.Normal);
        }

        // ── Helpers & Factory Methods ────────────────────────────────────────

        private static TMP_FontAsset GetFont()
        {
            if (cachedFont == null)
            {
                cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (cachedFont == null && TMP_Settings.defaultFontAsset != null)
                {
                    cachedFont = TMP_Settings.defaultFontAsset;
                }
            }
            return cachedFont;
        }

        private static Sprite GetRingSprite()
        {
            if (cachedRingSprite == null)
            {
                int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                float rOuter = size * 0.5f;
                float rInner = rOuter * 0.70f;
                Vector2 center = new Vector2(rOuter, rOuter);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                        float aOuter = Mathf.Clamp01(rOuter - dist);
                        float aInner = Mathf.Clamp01(dist - rInner);
                        float alpha  = Mathf.Min(aOuter, aInner);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                tex.Apply();
                cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            }
            return cachedRingSprite;
        }

        private static GameObject MakePanel(Transform parent, string name,
            Vector2 pivot, Vector2 anchor,
            Vector2 anchoredPos, Vector2 size, Color bgColor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.pivot            = pivot;
            rt.anchorMin        = anchor;
            rt.anchorMax        = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;

            Image img = go.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = false;

            return go;
        }

        private static GameObject MakeImage(Transform parent, string name,
            Vector2 pivot, Vector2 anchor,
            Vector2 anchoredPos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.pivot            = pivot;
            rt.anchorMin        = anchor;
            rt.anchorMax        = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;

            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return go;
        }

        private static void MakeAccentStripe(Transform panel, string name, Color accentColor)
        {
            GameObject stripe = new GameObject(name);
            stripe.transform.SetParent(panel, false);

            RectTransform rt = stripe.AddComponent<RectTransform>();
            rt.pivot            = new Vector2(0f, 1f);
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0f, 3f);

            Image img = stripe.AddComponent<Image>();
            img.color = accentColor;
            img.raycastTarget = false;
        }

        private static TextMeshProUGUI MakeText(Transform parent, string name,
            string content, float fontSize, Color color,
            Vector2 pivot, Vector2 anchor,
            Vector2 anchoredPos, Vector2 size,
            TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft,
            FontStyles fontStyle = FontStyles.Normal)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.pivot            = pivot;
            rt.anchorMin        = anchor;
            rt.anchorMax        = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();

            TMP_FontAsset font = GetFont();
            if (font != null) tmp.font = font;

            tmp.text               = content;
            tmp.fontSize           = fontSize;
            tmp.color              = color;
            tmp.alignment          = alignment;
            tmp.fontStyle          = fontStyle;
            tmp.enableWordWrapping = false;
            tmp.overflowMode       = TextOverflowModes.Overflow;
            tmp.raycastTarget      = false;

            return tmp;
        }
    }
}
