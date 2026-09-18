using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Realm
{
    // Mirrors the web client's dice stage: a short tumble, a settle, then an
    // automatic dismiss. Every object is authored in the scene; this only
    // animates and re-labels what is already there.
    public class RealmDiceRoll : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private GameObject stage;
        [SerializeField] private RectTransform die;
        [SerializeField] private TextMeshProUGUI eyebrowText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TextMeshProUGUI helpText;
        [SerializeField] private RectTransform pipRoot;
        [SerializeField] private Image[] pips;
        [SerializeField] private Image zeroPip;

        [Header("Timing")]
        [SerializeField] private float tumbleSeconds = 0.82f;
        [SerializeField] private float settleSeconds = 0.20f;
        [SerializeField] private float holdSeconds = 1.40f;

        private string _shownRollId;
        private float _elapsed = -1f;
        private int _value;
        private bool _resultShown;

        // Derived from the clock rather than latched. A latched flag stayed true
        // forever if anything hid the stage mid-roll, which left the discard
        // controls permanently stuck on "주사위 확인 중…".
        public bool IsRolling { get { return _elapsed >= 0f && _elapsed < tumbleSeconds; } }

        private void Awake()
        {
            // This component must live on an always-active object; it owns the
            // timer for a stage it switches off.
            if (stage == gameObject)
            {
                Debug.LogError("RealmDiceRoll must not sit on the stage it hides. Move it to a parent.", this);
                return;
            }
            if (stage != null) stage.SetActive(false);
        }

        // Returns true when this roll is new and the stage was started.
        public bool Play(DieRoll roll, string rollerName)
        {
            if (roll == null || stage == null || string.IsNullOrEmpty(roll.id)) return false;
            if (roll.id == _shownRollId) return false;

            _shownRollId = roll.id;
            _value = Mathf.Clamp(roll.value, 0, 3);
            _elapsed = 0f;
            _resultShown = false;

            stage.SetActive(true);
            if (eyebrowText != null) eyebrowText.text = $"D6 주사위 · {roll.round}라운드";
            if (titleText != null) titleText.text = $"{rollerName} 플레이어가 주사위를 굴립니다";
            if (resultText != null) resultText.text = "주사위를 굴리는 중…";
            if (helpText != null) helpText.text = "면 구성: 0 · 1 · 1 · 2 · 2 · 3";
            ShowPips(_value);
            return true;
        }

        public void Dismiss()
        {
            _elapsed = -1f;
            _resultShown = false;
            if (stage != null) stage.SetActive(false);
        }

        private void ShowPips(int value)
        {
            if (zeroPip != null) zeroPip.gameObject.SetActive(value == 0);
            if (pips == null) return;
            for (int i = 0; i < pips.Length; i++)
            {
                if (pips[i] != null) pips[i].gameObject.SetActive(i < value);
            }
        }

        private void Update()
        {
            // The clock keeps running even if the stage is hidden, so the roll
            // always resolves and never wedges the rest of the UI.
            if (_elapsed < 0f) return;

            _elapsed += Time.unscaledDeltaTime;

            if (_elapsed < tumbleSeconds)
            {
                float t = _elapsed / tumbleSeconds;
                ApplyDie(Mathf.Lerp(0f, 720f, EaseOut(t)), TumbleScale(t), 0f);
                return;
            }

            if (!_resultShown)
            {
                _resultShown = true;
                if (resultText != null)
                    resultText.text = $"결과 {_value} · {_value}장 반드시 버리기";
            }

            float settled = _elapsed - tumbleSeconds;
            if (settled < settleSeconds)
            {
                float t = settled / settleSeconds;
                ApplyDie(0f, Mathf.Lerp(1.07f, 1f, t), 1f);
                return;
            }

            ApplyDie(0f, 1f, 1f);
            if (settled - settleSeconds >= holdSeconds) Dismiss();
        }

        private void ApplyDie(float angle, float scale, float pipAlpha)
        {
            if (die == null) return;
            die.localRotation = Quaternion.Euler(0f, 0f, angle);
            die.localScale = Vector3.one * scale;
            // Pips are unreadable mid-tumble and only fade in once it lands.
            if (pipRoot != null)
            {
                var group = pipRoot.GetComponent<CanvasGroup>();
                if (group != null) group.alpha = pipAlpha;
            }
        }

        private static float EaseOut(float t)
        {
            return 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
        }

        // Matches the web keyframes: .72 → 1.12 → .9 → 1.08 → 1.
        private static float TumbleScale(float t)
        {
            if (t < 0.24f) return Mathf.Lerp(0.72f, 1.12f, t / 0.24f);
            if (t < 0.53f) return Mathf.Lerp(1.12f, 0.90f, (t - 0.24f) / 0.29f);
            if (t < 0.78f) return Mathf.Lerp(0.90f, 1.08f, (t - 0.53f) / 0.25f);
            return Mathf.Lerp(1.08f, 1f, (t - 0.78f) / 0.22f);
        }
    }
}
