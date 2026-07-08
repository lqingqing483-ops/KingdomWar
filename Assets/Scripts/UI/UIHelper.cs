using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomWar.UI
{
    public static class UIHelper
    {
        /// <summary>
        /// Fade in a CanvasGroup with optional delay
        /// </summary>
        public static Tween FadeIn(this CanvasGroup cg, float duration = 0.3f, float delay = 0f)
        {
            cg.alpha = 0;
            cg.blocksRaycasts = true;
            return cg.DOFade(1, duration).SetDelay(delay).SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// Fade out a CanvasGroup
        /// </summary>
        public static Tween FadeOut(this CanvasGroup cg, float duration = 0.2f)
        {
            cg.blocksRaycasts = false;
            return cg.DOFade(0, duration).SetEase(Ease.InQuad);
        }

        /// <summary>
        /// Scale punch effect for buttons / UI elements
        /// </summary>
        public static Tween PunchScale(this Transform t, float strength = 0.2f)
        {
            return t.DOPunchScale(Vector3.one * strength, 0.3f, 5, 0.5f);
        }

        /// <summary>
        /// Shake effect for error feedback
        /// </summary>
        public static Tween ShakeError(this Transform t)
        {
            return t.DOShakePosition(0.4f, new Vector3(10f, 0, 0), 10, 90);
        }

        /// <summary>
        /// Soft bounce in (scale from 0 to 1)
        /// </summary>
        public static Tween BounceIn(this Transform t, float delay = 0f)
        {
            t.localScale = Vector3.zero;
            return t.DOScale(1f, 0.35f).SetDelay(delay).SetEase(Ease.OutBack, 1.5f);
        }

        /// <summary>
        /// Stagger animate all children of a container
        /// </summary>
        public static void StaggerChildren(Transform container, float staggerDelay = 0.05f, bool fromZero = true)
        {
            int count = container.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform child = container.GetChild(i);
                if (fromZero) child.localScale = Vector3.zero;
                child.DOScale(1f, 0.3f).SetDelay(i * staggerDelay).SetEase(Ease.OutBack, 1.5f);
            }
        }

        /// <summary>
        /// Create floating text that rises and fades (for damage, gold, etc.)
        /// </summary>
        public static GameObject CreateFloatingText(string text, Vector3 worldPos, Color color, float duration = 1.2f)
        {
            GameObject go = new GameObject("FloatingText");
            go.transform.position = worldPos + Vector3.up * 0.5f;
            
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 24;
            tm.color = color;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            go.transform.DOMoveY(worldPos.y + 2f, duration).SetEase(Ease.OutCubic);
            go.GetComponent<Renderer>().material.DOFade(0, duration * 0.7f).SetDelay(duration * 0.3f);
            Object.Destroy(go, duration + 0.1f);
            
            return go;
        }

        /// <summary>
        /// Create floating text in screen space (for UI overlays)
        /// </summary>
        public static void CreateFloatingUI(string text, Vector2 screenPos, Color color, Transform parent = null)
        {
            GameObject go = new GameObject("FloatingUI");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = screenPos;
            
            Text uiText = go.AddComponent<Text>();
            uiText.text = text;
            uiText.fontSize = 20;
            uiText.color = color;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            
            CanvasRenderer cr = go.AddComponent<CanvasRenderer>();
            ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            go.transform.DOMoveY(screenPos.y + 80f, 1f).SetEase(Ease.OutCubic);
            go.GetComponent<CanvasRenderer>().SetAlpha(1);
            DOTween.To(() => 1f, (v) => cr.SetAlpha(v), 0f, 0.6f).SetDelay(0.5f);
            Object.Destroy(go, 1.2f);
        }

        /// <summary>
        /// Count-up effect for numbers (gold, gems, etc.)
        /// </summary>
        public static Tween CountTo(this Text text, int from, int to, float duration = 0.5f)
        {
            return DOTween.To(() => from, (v) => { from = v; text.text = v.ToString(); }, to, duration)
                .SetEase(Ease.OutQuad);
        }
    }
}
