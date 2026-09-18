using UnityEngine;
using UnityEngine.UI;

namespace Realm
{
    // Binds the MCP-authored review panel. No UI is generated at runtime.
    public sealed class RealmCardReview : MonoBehaviour
    {
        [SerializeField] private GameObject reviewPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private RealmCard[] samples = new RealmCard[0];

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            foreach (var card in samples)
                if (card != null) card.OnClicked += ToggleCard;
        }
        private void Open() { reviewPanel.SetActive(true); }
        private void Close() { reviewPanel.SetActive(false); }
        private void ToggleCard(RealmCard card) { card.SetSelected(!card.IsSelected); }
        private void OnDestroy()
        {
            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            foreach (var card in samples)
                if (card != null) card.OnClicked -= ToggleCard;
        }
    }
}
