using UnityEngine;
using UnityEngine.UI;

namespace KingdomWar.UI
{
    public class UnitHealthBar : MonoBehaviour
    {
        private Transform barTransform;
        private Transform backgroundTransform;
        private Transform canvasTransform;
        private Material barMaterial;
        private float currentFill = 1f;
        private Transform targetTransform;
        private Vector3 offset = new Vector3(0, 2f, 0);

        public void Initialize(Transform unitTransform, Vector3 barOffset)
        {
            targetTransform = unitTransform;
            offset = barOffset;

            GameObject canvasGO = new GameObject("HealthBarCanvas");
            canvasTransform = canvasGO.transform;
            canvasTransform.SetParent(targetTransform, false);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasTransform = canvasGO.transform;
            RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1.5f, 0.25f);
            canvasRect.localPosition = offset;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one;

            GameObject backgroundGO = new GameObject("Background");
            backgroundTransform = backgroundGO.transform;
            backgroundTransform.SetParent(canvasTransform, false);
            Image bgImage = backgroundGO.AddComponent<Image>();
            backgroundTransform = backgroundGO.transform;
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            RectTransform bgRect = backgroundGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject barGO = new GameObject("Bar");
            barTransform = barGO.transform;
            barTransform.SetParent(canvasTransform, false);
            Image barImage = barGO.AddComponent<Image>();
            barTransform = barGO.transform;
            barImage.color = Color.green;
            RectTransform barRect = barGO.GetComponent<RectTransform>();
            barRect.anchorMin = Vector2.zero;
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.offsetMin = new Vector2(0.02f, 0.02f);
            barRect.offsetMax = new Vector2(-0.02f, -0.02f);
            barRect.pivot = new Vector2(0f, 0.5f);
        }

        public void UpdateHealth(float health01)
        {
            currentFill = Mathf.Clamp01(health01);
            if (barTransform != null)
            {
                RectTransform barRect = barTransform.GetComponent<RectTransform>();
                barRect.anchorMax = new Vector2(currentFill, 1f);

                Image barImage = barTransform.GetComponent<Image>();
                if (barImage != null)
                {
                    if (currentFill > 0.5f)
                        barImage.color = Color.green;
                    else if (currentFill > 0.25f)
                        barImage.color = new Color(1f, 0.6f, 0f);
                    else
                        barImage.color = Color.red;
                }
            }

            if (canvasTransform != null && Camera.main != null)
            {
                canvasTransform.LookAt(Camera.main.transform);
            }
        }

        public void SetActive(bool active)
        {
            if (canvasTransform != null)
                canvasTransform.gameObject.SetActive(active);
        }

        public void Cleanup()
        {
            if (canvasTransform != null)
            {
                DestroyImmediate(canvasTransform.gameObject);
                canvasTransform = null;
            }
            barTransform = null;
            backgroundTransform = null;
        }

        private void LateUpdate()
        {
            if (canvasTransform != null && Camera.main != null)
            {
                canvasTransform.LookAt(cameraTransform);
                canvasTransform.Rotate(0, 180, 0);
            }
        }

        private Transform cameraTransform;

        private void Start()
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
