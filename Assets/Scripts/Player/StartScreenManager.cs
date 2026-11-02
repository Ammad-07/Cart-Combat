using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CartCombat.Player
{
    /// <summary>
    /// Displays the high-energy comic splash screen on game start.
    /// Pauses player controls and camera rotation until the user presses any key or clicks,
    /// then smoothly fades out and launches first-person gameplay!
    /// </summary>
    public class StartScreenManager : MonoBehaviour
    {
        public static bool IsGameStarted { get; private set; } = false;

        private GameObject canvasGO;
        private CanvasGroup canvasGroup;
        private RawImage splashImage;
        private bool isStarting = false;

        private void Awake()
        {
            IsGameStarted = false;
            BuildSplashCanvas();
        }

        private void Start()
        {
            // Keep cursor free while on the splash screen
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (IsGameStarted || isStarting) return;

            bool anyInput = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) anyInput = true;
            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)) anyInput = true;
            if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame)) anyInput = true;
#else
            if (Input.anyKeyDown) anyInput = true;
#endif

            if (anyInput)
            {
                StartCoroutine(DismissSplashScreen());
            }
        }

        public void OnClickStart()
        {
            if (IsGameStarted || isStarting) return;
            StartCoroutine(DismissSplashScreen());
        }

        private IEnumerator DismissSplashScreen()
        {
            isStarting = true;

            // Smooth fade out
            float elapsed = 0f;
            float fadeDuration = 0.35f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                }
                yield return null;
            }

            IsGameStarted = true;

            // Lock cursor for first-person gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (canvasGO != null)
            {
                Destroy(canvasGO);
            }
        }

        private void BuildSplashCanvas()
        {
            canvasGO = new GameObject("SplashScreen_Canvas");
            canvasGO.transform.SetParent(transform, false);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGroup = canvasGO.AddComponent<CanvasGroup>();

            // Fullscreen RawImage for the splash artwork
            GameObject imgObj = new GameObject("SplashArtwork");
            imgObj.transform.SetParent(canvasGO.transform, false);

            RectTransform rt = imgObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            splashImage = imgObj.AddComponent<RawImage>();
            splashImage.texture = LoadSplashTexture();
            splashImage.color = Color.white;

            // Invisible clickable button covering full screen
            Button btn = imgObj.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(OnClickStart);
        }

        private Texture2D LoadSplashTexture()
        {
            // 1. Try Resources.Load
            Texture2D tex = Resources.Load<Texture2D>("SplashScreen");
            if (tex != null) return tex;

            // 2. Fallback: Load directly from file bytes
            string[] possiblePaths = new string[]
            {
                Path.Combine(Application.dataPath, "Resources", "SplashScreen.jpg"),
                Path.Combine(Application.dataPath, "Textures", "SplashScreen.jpg")
            };

            foreach (string p in possiblePaths)
            {
                if (File.Exists(p))
                {
                    byte[] bytes = File.ReadAllBytes(p);
                    Texture2D diskTex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                    if (diskTex.LoadImage(bytes))
                    {
                        return diskTex;
                    }
                }
            }

            return null;
        }

        private void OnDestroy()
        {
            if (canvasGO != null)
            {
                Destroy(canvasGO);
            }
        }
    }
}
