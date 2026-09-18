using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Realm
{
    // Swaps the OS cursor based on what is under the pointer. Polls the UI
    // raycaster rather than adding enter/exit handlers to every widget, so new
    // buttons and runtime-spawned cards are covered without extra wiring.
    public class RealmCursor : MonoBehaviour
    {
        [Header("Textures")]
        [SerializeField] private Texture2D defaultCursor;
        [SerializeField] private Texture2D hoverCursor;
        [SerializeField] private Texture2D grabCursor;

        private readonly List<RaycastResult> _hits = new List<RaycastResult>();
        private State _state = State.None;

        private enum State { None, Default, Hover, Grab }

        private void OnEnable()
        {
            Apply(State.Default);
        }

        private void OnDisable()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            _state = State.None;
        }

        private void Update()
        {
            Apply(Probe());
        }

        private State Probe()
        {
            var events = EventSystem.current;
            if (events == null) return State.Default;

            // This project runs the Input System backend, so the legacy Input
            // class throws. Pointer covers both mouse and touch.
            var device = Pointer.current;
            if (device == null) return State.Default;

            var pointer = new PointerEventData(events) { position = device.position.ReadValue() };
            _hits.Clear();
            events.RaycastAll(pointer, _hits);

            for (int i = 0; i < _hits.Count; i++)
            {
                var go = _hits[i].gameObject;
                if (go == null) continue;

                // A card is grabbable only while it is actually selectable.
                var card = go.GetComponentInParent<RealmCard>();
                if (card != null)
                {
                    var cardButton = card.GetComponent<Button>();
                    if (cardButton != null && cardButton.interactable) return State.Grab;
                }

                var selectable = go.GetComponentInParent<Selectable>();
                if (selectable != null && selectable.interactable) return State.Hover;

                // An opaque graphic blocks anything behind it, so stop here.
                var graphic = go.GetComponent<Graphic>();
                if (graphic != null && graphic.raycastTarget) break;
            }
            return State.Default;
        }

        private void Apply(State next)
        {
            if (next == _state) return;
            _state = next;

            Texture2D texture;
            Vector2 hotspot;
            switch (next)
            {
                case State.Grab:
                    texture = grabCursor;
                    // The gauntlet is drawn centred, so it grips at its middle.
                    hotspot = texture != null ? new Vector2(texture.width, texture.height) * 0.5f : Vector2.zero;
                    break;
                case State.Hover:
                    texture = hoverCursor;
                    hotspot = Vector2.zero;
                    break;
                default:
                    texture = defaultCursor;
                    hotspot = Vector2.zero;
                    break;
            }
            Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        }
    }
}
