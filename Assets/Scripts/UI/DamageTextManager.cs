using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace KingdomWar.UI
{
    public class DamageTextManager : MonoBehaviour
    {
        private static DamageTextManager instance;
        public static DamageTextManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("DamageTextManager");
                    instance = obj.AddComponent<DamageTextManager>();
                    DontDestroyOnLoad(obj);
                }
                return instance;
            }
        }

        [Header("Settings")]
        public GameObject damageTextPrefab;
        public float floatUpSpeed = 1f;
        public float fadeDuration = 0.8f;
        public float damageTextLifetime = 1.2f;
        public Color damageColor = Color.red;
        public Color healColor = Color.green;
        public Color criticalColor = Color.yellow;
        public int fontSize = 24;
        public int criticalFontSize = 32;

        private Camera mainCamera;
        private Canvas overlayCanvas;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                CreateOverlayCanvas();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void CreateOverlayCanvas()
        {
            GameObject canvasObj = new GameObject("DamageTextCanvas");
            canvasObj.transform.SetParent(transform);
            overlayCanvas = canvasObj.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 100; // Always on top
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>().enabled = false; // don't block clicks
        }

        void Update()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
        }

        /// <summary>Show damage number above a world position.</summary>
        public void ShowDamage(Vector3 worldPos, int amount, bool isCritical = false)
        {
            ShowText(worldPos, amount.ToString(), isCritical ? criticalColor : damageColor,
                     isCritical ? criticalFontSize : fontSize);
        }

        /// <summary>Show heal number above a world position.</summary>
        public void ShowHeal(Vector3 worldPos, int amount)
        {
            ShowText(worldPos, "+" + amount.ToString(), healColor, fontSize);
        }

        private void ShowText(Vector3 worldPos, string text, Color color, int size)
        {
            if (overlayCanvas == null) return;
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            // Create a text object
            GameObject textObj = new GameObject("DamageText");
            textObj.transform.SetParent(overlayCanvas.transform, false);

            Text txt = textObj.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = size;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            // Position: world to screen with random horizontal offset
            Vector2 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            screenPos += new Vector2(Random.Range(-20f, 20f), 0f);

            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(120, 40);
            rt.anchoredPosition = screenPos;

            // Animate: float up + fade out
            StartCoroutine(AnimateText(textObj, rt, txt));
        }

        private IEnumerator AnimateText(GameObject obj, RectTransform rt, Text txt)
        {
            float elapsed = 0f;
            Vector2 startPos = rt.anchoredPosition;
            Color startColor = txt.color;

            while (elapsed < damageTextLifetime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / damageTextLifetime;

                // Float upward
                rt.anchoredPosition = startPos + new Vector2(0, t * floatUpSpeed * 100f);

                // Fade out in the last 30%
                if (t > 0.7f)
                {
                    float fadeT = (t - 0.7f) / 0.3f;
                    txt.color = new Color(startColor.r, startColor.g, startColor.b, 1f - fadeT);
                }

                yield return null;
            }

            Destroy(obj);
        }
    }
}
