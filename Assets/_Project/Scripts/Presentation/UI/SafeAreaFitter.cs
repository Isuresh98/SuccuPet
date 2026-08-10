using UnityEngine;

namespace SuccuPet.Presentation.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform targetRectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            CacheRectTransform();
        }

        private void OnEnable()
        {
            CacheRectTransform();
            ApplySafeArea();
        }

        private void Update()
        {
            Rect currentSafeArea = Screen.safeArea;
            Vector2Int currentScreenSize =
                new Vector2Int(Screen.width, Screen.height);

            if (currentSafeArea != lastSafeArea ||
                currentScreenSize != lastScreenSize)
            {
                ApplySafeArea();
            }
        }

        private void CacheRectTransform()
        {
            if (targetRectTransform == null)
            {
                targetRectTransform = GetComponent<RectTransform>();
            }
        }

        private void ApplySafeArea()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;

            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            targetRectTransform.anchorMin = anchorMin;
            targetRectTransform.anchorMax = anchorMax;
            targetRectTransform.offsetMin = Vector2.zero;
            targetRectTransform.offsetMax = Vector2.zero;

            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(
                Screen.width,
                Screen.height);
        }
    }
}