using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace Realm
{
    // Full-screen title card shown over the lobby until the player dismisses it.
    // It sits last in the canvas and simply hides itself, so nothing else in the
    // lobby has to know it exists.
    public class RealmSplash : MonoBehaviour
    {
        [SerializeField] public CanvasGroup group;
        [SerializeField] public TextMeshProUGUI promptText;
        [SerializeField] public RectTransform hero;
        [SerializeField] public float fadeSeconds = 0.45f;

        private bool _dismissing;
        private float _fade;
        private float _shown;

        private void OnEnable()
        {
            _dismissing = false;
            _fade = 0f;
            _shown = 0f;
            if (group != null) { group.alpha = 1f; group.blocksRaycasts = true; }
        }

        private void Update()
        {
            _shown += Time.unscaledDeltaTime;

            if (!_dismissing)
            {
                // Breathing prompt, plus a slow rise on the wordmark as it settles.
                if (promptText != null)
                {
                    var c = promptText.color;
                    c.a = 0.35f + 0.5f * (0.5f + 0.5f * Mathf.Sin(_shown * 2.2f));
                    promptText.color = c;
                }
                if (hero != null)
                {
                    float k = Mathf.Clamp01(_shown / 0.9f);
                    float ease = 1f - Mathf.Pow(1f - k, 3f);
                    hero.localScale = Vector3.one * Mathf.Lerp(0.965f, 1f, ease);
                }

                // A quarter second of grace stops a stray click from the previous
                // screen blowing straight through the splash.
                if (_shown > 0.25f && DismissRequested()) Dismiss();
                return;
            }

            _fade += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_fade / Mathf.Max(0.01f, fadeSeconds));
            if (group != null) group.alpha = 1f - t;
            if (t >= 1f) gameObject.SetActive(false);
        }

        // Legacy UnityEngine.Input throws under the Input System package, so every
        // read goes through the new devices and tolerates a missing one.
        private static bool DismissRequested()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) return true;
            return false;
        }

        public void Dismiss()
        {
            if (_dismissing) return;
            _dismissing = true;
            _fade = 0f;
            if (group != null) group.blocksRaycasts = false;
        }
    }
}
