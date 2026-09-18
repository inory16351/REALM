using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Realm
{
    // Positions existing card-template instances only. The badge is not a card.
    public class DiscardPileHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private float collapsedStep = 16;
        [SerializeField] private float expandedGap = 8;
        [SerializeField] private float animSpeed = 12;
        private bool hovered;
        private bool pinned;
        private float currentStep;
        private float expandedStep;
        private RealmCard[] cards = new RealmCard[0];
        private RectTransform rect;
        private int restingSiblingIndex = -1;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            currentStep = collapsedStep;
        }

        public void RefreshStack()
        {
            cards = GetComponentsInChildren<RealmCard>(false);
            if (rect == null) rect = GetComponent<RectTransform>();
            restingSiblingIndex = transform.GetSiblingIndex();
            var oldLayout = GetComponent<HorizontalLayoutGroup>();
            if (oldLayout != null) oldLayout.enabled = false;
            if (cards.Length == 0) return;

            expandedStep = CardWidth() + expandedGap;
            currentStep = collapsedStep;
            var badge = transform.Find("Badge");
            if (badge != null) badge.SetAsLastSibling();
            LayoutCards();
        }

        private void Update()
        {
            if (cards.Length == 0) return;
            float desired = hovered || pinned ? expandedStep : collapsedStep;
            currentStep = Mathf.Lerp(currentStep, desired,
                1 - Mathf.Exp(-animSpeed * Time.unscaledDeltaTime));
            LayoutCards();
        }

        private void LayoutCards()
        {
            if (cards.Length == 0) return;
            float width = CardWidth();
            float shift = ExpansionShift(width);
            for (int i = 0; i < cards.Length; i++)
            {
                var child = (RectTransform)cards[i].transform;
                child.anchorMin = new Vector2(0, .5f);
                child.anchorMax = new Vector2(0, .5f);
                child.pivot = new Vector2(.5f, .5f);
                child.anchoredPosition = new Vector2(6 + width / 2 + i * currentStep - shift, 0);
                child.localRotation = Quaternion.Euler(0, 0, hovered || pinned ? 0 : (i % 2 == 0 ? -1.2f : 1.2f));
            }
        }

        // Grave cards are displayed at a reduced scale, so every offset has to
        // be measured after that scale rather than from the authored width.
        private float CardWidth()
        {
            var card = (RectTransform)cards[0].transform;
            return card.rect.width * card.localScale.x;
        }

        // A fanned pile is wider than its slot. Slots near the right edge slide
        // the fan left so the last card never lands outside the row.
        private float ExpansionShift(float cardWidth)
        {
            var row = rect.parent as RectTransform;
            // Piles lay out once on creation, before the row has been sized.
            // Treating that unresolved width as the row would shove the whole
            // fan off-screen, so wait until the row is real.
            if (row == null || row.rect.width <= 0f || cards.Length < 2) return 0f;

            float fanWidth = 6f + cardWidth + (cards.Length - 1) * currentStep;
            float leftEdge = rect.anchoredPosition.x - rect.rect.width * rect.pivot.x;
            return Mathf.Max(0f, leftEdge + fanWidth - row.rect.width);
        }

        public void OnPointerEnter(PointerEventData e) { hovered = true; UpdateSorting(); }
        public void OnPointerExit(PointerEventData e) { hovered = false; UpdateSorting(); }

        // Slots are positioned by hand, so sibling order only affects draw order
        // and a fanned pile can safely rise above the slots it overlaps.
        private void UpdateSorting()
        {
            if (restingSiblingIndex < 0) restingSiblingIndex = transform.GetSiblingIndex();
            if (hovered || pinned) transform.SetAsLastSibling();
            else transform.SetSiblingIndex(restingSiblingIndex);
        }
        public void OnPointerClick(PointerEventData e) { pinned = !pinned; UpdateSorting(); }
    }
}
