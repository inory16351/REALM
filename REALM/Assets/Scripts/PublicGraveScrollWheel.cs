using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Realm
{
    // Attached to the MCP-authored public-grave viewport. It converts a mouse
    // wheel into a horizontal move while ScrollRect still supports drag input.
    [RequireComponent(typeof(ScrollRect))]
    public sealed class PublicGraveScrollWheel : MonoBehaviour, IScrollHandler
    {
        [SerializeField] private float wheelStep = 0.14f;
        private ScrollRect scrollRect;

        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (scrollRect == null || !scrollRect.horizontal || scrollRect.content == null) return;

            float delta = Mathf.Abs(eventData.scrollDelta.x) > 0.001f
                ? eventData.scrollDelta.x
                : eventData.scrollDelta.y;
            if (Mathf.Abs(delta) < 0.001f) return;

            scrollRect.StopMovement();
            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(
                scrollRect.horizontalNormalizedPosition - delta * wheelStep);
            eventData.Use();
        }
    }
}
