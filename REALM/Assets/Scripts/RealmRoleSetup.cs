using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Realm
{
    // Host-only role picker. Mirrors the server's validateSelection so the host
    // never sends a selection the room will reject.
    public class RealmRoleSetup : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform gridContainer;
        [SerializeField] private GameObject optionTemplate;
        [SerializeField] private TextMeshProUGUI headlineText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private TextMeshProUGUI problemText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button rerollButton;
        [Header("Reference mode")]
        [SerializeField] private Button codexButton;

        private readonly List<string> _chosen = new List<string>();
        private readonly Dictionary<string, RoleOption> _options = new Dictionary<string, RoleOption>();
        private int _playerTarget = 5;
        private bool _readOnly;

        public bool IsOpen { get { return panel != null && panel.activeSelf; } }
        public event System.Action<string[]> OnConfirmed;

        private sealed class RoleOption
        {
            public GameObject go;
            public Image background;
            public Outline outline;
        }

        private void Awake()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
            if (cancelButton != null) cancelButton.onClick.AddListener(Close);
            if (rerollButton != null) rerollButton.onClick.AddListener(Reroll);
            if (codexButton != null) codexButton.onClick.AddListener(OpenCodex);
            if (panel != null) panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Close);
            if (rerollButton != null) rerollButton.onClick.RemoveListener(Reroll);
            if (codexButton != null) codexButton.onClick.RemoveListener(OpenCodex);
        }

        // Same grid, no selection: a reference sheet of every role's effect.
        public void OpenCodex()
        {
            _readOnly = true;
            _chosen.Clear();
            BuildOptions();
            if (panel != null) panel.SetActive(true);
            Refresh();
        }

        public void Open(int playerTarget, IEnumerable<string> preselected)
        {
            _readOnly = false;
            _playerTarget = Mathf.Clamp(playerTarget, 5, 10);
            _chosen.Clear();
            if (preselected != null)
                foreach (var r in preselected)
                    if (RealmCard.RoleInfo.ContainsKey(r) && _chosen.Count < _playerTarget) _chosen.Add(r);

            BuildOptions();
            if (panel != null) panel.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void BuildOptions()
        {
            if (gridContainer == null || optionTemplate == null) return;

            // The role list never changes, so build it once. Rebuilding per open
            // also leaked badly in the editor, where Destroy is deferred.
            if (_options.Count == RealmCard.RoleInfo.Count)
            {
                bool allAlive = true;
                foreach (var existing in _options.Values)
                    if (existing.go == null) { allAlive = false; break; }
                if (allAlive) return;
            }

            for (int i = gridContainer.childCount - 1; i >= 0; i--)
            {
                var child = gridContainer.GetChild(i).gameObject;
                if (child == optionTemplate) continue;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
            _options.Clear();

            var font = RealmCard.GetNotoTmpFont();
            foreach (var pair in RealmCard.RoleInfo.OrderBy(p => p.Value.score).ThenBy(p => p.Value.name))
            {
                string key = pair.Key;
                var info = pair.Value;

                var go = Instantiate(optionTemplate, gridContainer);
                go.name = "Role_" + key;
                go.SetActive(true);

                var nameText = go.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
                var scoreText = go.transform.Find("Score")?.GetComponent<TextMeshProUGUI>();
                var ruleText = go.transform.Find("Rule")?.GetComponent<TextMeshProUGUI>();
                if (nameText != null) { nameText.font = font; nameText.text = info.name; }
                if (scoreText != null)
                {
                    scoreText.font = font;
                    scoreText.text = info.score > 0 ? "+" + info.score : info.score.ToString();
                    scoreText.color = ScoreColor(info.score);
                }
                if (ruleText != null) { ruleText.font = font; ruleText.text = info.rule; }

                var button = go.GetComponent<Button>() ?? go.AddComponent<Button>();
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Toggle(key));

                var option = new RoleOption
                {
                    go = go,
                    background = go.GetComponent<Image>(),
                    outline = go.GetComponent<Outline>() ?? go.AddComponent<Outline>()
                };
                _options[key] = option;
            }
            optionTemplate.SetActive(false);
        }

        private static Color ScoreColor(int score)
        {
            if (score > 0) return new Color(0.518f, 0.898f, 0.710f);
            if (score < 0) return new Color(1f, 0.710f, 0.667f);
            return new Color(0.725f, 0.851f, 1f);
        }

        private void Toggle(string key)
        {
            if (_readOnly) return;
            if (_chosen.Contains(key)) _chosen.Remove(key);
            else if (_chosen.Count < _playerTarget) _chosen.Add(key);
            Refresh();
        }

        private void Refresh()
        {
            foreach (var pair in _options)
            {
                bool on = _chosen.Contains(pair.Key);
                if (pair.Value.background != null)
                    pair.Value.background.color = on
                        ? new Color(0.149f, 0.341f, 0.475f, 0.98f)
                        : new Color(0.039f, 0.106f, 0.176f, 0.96f);
                if (pair.Value.outline != null)
                {
                    pair.Value.outline.effectColor = on
                        ? new Color(1f, 0.878f, 0.545f, 1f)
                        : new Color(0.435f, 0.545f, 0.639f, 0.45f);
                    pair.Value.outline.effectDistance = on ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
                }
            }

            // Reference mode hides everything about picking and just lists effects.
            if (rerollButton != null) rerollButton.gameObject.SetActive(!_readOnly);
            if (confirmButton != null) confirmButton.gameObject.SetActive(!_readOnly);
            if (balanceText != null) balanceText.gameObject.SetActive(!_readOnly);
            if (cancelButton != null)
            {
                var cancelLabel = cancelButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (cancelLabel != null) cancelLabel.text = _readOnly ? "닫기" : "나중에";
            }

            if (_readOnly)
            {
                if (headlineText != null) headlineText.text = "직업 효과 — 25종";
                if (countText != null) countText.text = $"총 {RealmCard.RoleInfo.Count}종";
                if (problemText != null)
                {
                    problemText.text = "카드 점수는 좌측 숫자, 승리 조건은 아래 설명입니다.";
                    problemText.color = new Color(0.725f, 0.851f, 1f);
                }
                return;
            }

            if (headlineText != null)
                headlineText.text = $"이번 판에 쓸 직업 {_playerTarget}종을 고르세요";
            if (countText != null)
                countText.text = $"{_chosen.Count} / {_playerTarget} 선택";

            var scores = _chosen.Select(k => RealmCard.RoleInfo[k].score).ToList();
            int negatives = scores.Count(s => s < 0);
            int positives = scores.Count(s => s > 0);
            int zeros = scores.Count(s => s == 0);
            int total = scores.Sum();
            int limit = Mathf.CeilToInt(_playerTarget * 0.4f);

            if (balanceText != null)
                balanceText.text = $"-1점 {negatives}개 · 0점 {zeros}개 · +1점 {positives}개  ·  합계 {(total > 0 ? "+" : "")}{total}";

            string problem = Validate(negatives, positives, zeros, total, limit);
            if (problemText != null)
            {
                problemText.text = problem;
                problemText.color = string.IsNullOrEmpty(problem)
                    ? new Color(0.518f, 0.898f, 0.710f)
                    : new Color(1f, 0.710f, 0.667f);
                if (string.IsNullOrEmpty(problem)) problemText.text = "조건을 모두 만족합니다.";
            }
            if (confirmButton != null) confirmButton.interactable = string.IsNullOrEmpty(problem);
        }

        private string Validate(int negatives, int positives, int zeros, int total, int limit)
        {
            return ValidateSet(_chosen, negatives, positives, zeros, total, limit);
        }

        // Kept in step with validateSelection() in the Worker.
        private string ValidateSet(List<string> set, int negatives, int positives, int zeros, int total, int limit)
        {
            if (set.Count != _playerTarget) return $"직업을 정확히 {_playerTarget}개 골라야 합니다.";
            if (negatives < 1 || negatives > limit || positives < 1 || positives > limit || zeros < 1)
                return $"-1점·+1점 직업은 각각 1~{limit}개, 0점 직업은 1개 이상이어야 합니다.";
            if (Mathf.Abs(total) > 1)
                return $"직업 점수 합은 0 또는 ±1이어야 합니다. 현재 {(total > 0 ? "+" : "")}{total}점입니다.";
            if (set.Contains("jester") && !set.Contains("assassin"))
                return "광대를 고르면 암살자도 함께 골라야 합니다.";
            return "";
        }

        // Builds a combination that already satisfies every rule, so the host can
        // start immediately instead of hand-balancing 25 roles.
        private void Reroll()
        {
            var byScore = new Dictionary<int, List<string>>
            {
                { -1, new List<string>() }, { 0, new List<string>() }, { 1, new List<string>() }
            };
            foreach (var pair in RealmCard.RoleInfo)
            {
                int score = Mathf.Clamp(pair.Value.score, -1, 1);
                byScore[score].Add(pair.Key);
            }

            int limit = Mathf.CeilToInt(_playerTarget * 0.4f);
            for (int attempt = 0; attempt < 200; attempt++)
            {
                // Pick counts first: this is what the balance rule constrains.
                int negatives = Random.Range(1, limit + 1);
                int positives = Random.Range(1, limit + 1);
                int zeros = _playerTarget - negatives - positives;
                if (zeros < 1 || zeros > byScore[0].Count) continue;
                if (negatives > byScore[-1].Count || positives > byScore[1].Count) continue;
                if (Mathf.Abs(positives - negatives) > 1) continue;

                var picked = new List<string>();
                picked.AddRange(Sample(byScore[-1], negatives));
                picked.AddRange(Sample(byScore[0], zeros));
                picked.AddRange(Sample(byScore[1], positives));

                // The jester needs an assassin; swap one in rather than retrying.
                if (picked.Contains("jester") && !picked.Contains("assassin"))
                {
                    int swap = picked.FindIndex(r => r != "jester" && RealmCard.RoleInfo[r].score == 0);
                    if (swap < 0) continue;
                    picked[swap] = "assassin";
                }

                var scores = picked.Select(r => RealmCard.RoleInfo[r].score).ToList();
                string problem = ValidateSet(picked, scores.Count(s => s < 0), scores.Count(s => s > 0),
                    scores.Count(s => s == 0), scores.Sum(), limit);
                if (!string.IsNullOrEmpty(problem)) continue;

                _chosen.Clear();
                _chosen.AddRange(picked);
                Refresh();
                return;
            }

            if (problemText != null)
            {
                problemText.text = "추천 조합을 찾지 못했습니다. 직접 골라 주세요.";
                problemText.color = new Color(1f, 0.710f, 0.667f);
            }
        }

        private static IEnumerable<string> Sample(List<string> pool, int count)
        {
            var copy = new List<string>(pool);
            for (int i = 0; i < count && copy.Count > 0; i++)
            {
                int index = Random.Range(0, copy.Count);
                yield return copy[index];
                copy.RemoveAt(index);
            }
        }

        private void Confirm()
        {
            var scores = _chosen.Select(k => RealmCard.RoleInfo[k].score).ToList();
            int limit = Mathf.CeilToInt(_playerTarget * 0.4f);
            if (!string.IsNullOrEmpty(Validate(scores.Count(s => s < 0), scores.Count(s => s > 0),
                scores.Count(s => s == 0), scores.Sum(), limit))) return;

            Close();
            if (OnConfirmed != null) OnConfirmed(_chosen.ToArray());
        }
    }
}
