using UnityEngine;

namespace Tripledot.Shared
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        void Awake()
        {
            Rect safe = Screen.safeArea;
            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(safe.x / Screen.width,
                                         safe.y / Screen.height);
            rect.anchorMax = new Vector2((safe.x + safe.width)  / Screen.width,
                                         (safe.y + safe.height) / Screen.height);
        }
    }
}
