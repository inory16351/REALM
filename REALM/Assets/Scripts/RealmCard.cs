using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Realm
{
    public class RealmCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        // Mirrors ROLE_DATA in the Worker (server/src/index.ts). The server is
        // authoritative: if these drift, players are shown win conditions the
        // game does not actually score.
        // `brief` is what fits on a card face; `rule` is the full server wording
        // shown in the role codex and the secret-role banner.
        public static readonly Dictionary<string, (string name, int score, int bonus, string brief, string rule)> RoleInfo = new Dictionary<string, (string, int, int, string, string)>
        {
            { "king", ("국왕", -1, 9, "무덤 왕 9장 이상, 또는 전원 손패에 왕 없음.", "종료 직전 다른 유저를 지목해, 그 사람의 손패 위치 2곳을 지정해 버리게 합니다. (손패는 비공개라 내용을 보지 못합니다.) 전체 무덤에 왕이 9장 이상이거나(투입량 적을 시) 전원의 손패에 왕이 없으면 성공.") },
            { "noble", ("귀족", 1, 7, "전원이 4R에 비공개로 버린 귀족이 정확히 3장.", "모든 플레이어가 4라운드에 비공개로 버린 카드 중 귀족이 정확히 3장이면 성공합니다.") },
            { "assassin", ("암살자", 0, 10, "서로 다른 상대 2명의 직업을 모두 적중.", "서로 다른 상대 두 명과 그 직업을 각각 지목합니다. 두 지목이 모두 맞으면 성공합니다.") },
            { "beggar", ("거지", -1, 10, "전원 남은 손패 합계가 인원수×2보다 적음.", "4라운드 직후 전원의 남은 손패 합계가 인원수×2장보다 적으면 성공합니다.") },
            { "slave", ("노예", 0, 7, "지목한 상대 1명의 직업을 적중.", "상대 한 명과 그 직업을 지목합니다. 지목이 맞으면 성공합니다.") },
            { "jester", ("광대", -1, 8, "암살자가 나를 지목하거나, 나를 지목한 사람이 3명(5인전 2명) 이상.", "최종 지목에서 암살자가 나를 지목하거나, 나를 지목한 사람이 3명(5인전은 2명) 이상이면 성공합니다. 지목된 직업이 맞는지는 상관없습니다. 2·3라운드의 의심은 포함되지 않습니다.") },
            { "priest", ("사제", 1, 8, "전원이 사제를 1장 이상 보유.", "모든 플레이어가(본인 포함) 손패에 사제를 최소 1장 이상 가지고 있으면 성공합니다.") },
            { "knight", ("기사", 1, 7, "무덤의 기사 수가 최소 종류 중 하나.", "전체 무덤(공개+비공개)에 버려진 기사 수가, 버려진 다른 어떤 카드 종류의 수보다 적거나 같으면 성공합니다.") },
            { "bard", ("음유시인", 0, 9, "손패가 모두 음유시인 + 4R에 음유시인 버림.", "마지막 손패가 모두 음유시인이고(최소 1장 이상), 자신이 4라운드에 비공개로 버린 카드 중 음유시인이 최소 1장 있으면 성공합니다.") },
            { "hunter", ("사냥꾼", 0, 8, "지목한 상대 손패에 그 카드가 있음.", "상대 한 명과 특정 카드를 지목합니다. 그 상대의 손패에 지목한 카드가 최소 1장 있으면 성공합니다.") },
            { "commoner", ("평민", 1, 7, "전원 손패에서 평민이 최다 종류.", "전원의 남은 손패에 있는 평민 카드의 수가, 다른 어떤 카드의 수보다 많거나 같으면 성공합니다.") },
            { "merchant", ("상인", 1, 6, "전원 남은 손패 합계가 인원수×2보다 많음.", "4라운드 직후 전원의 남은 손패 합계가 인원수×2장보다 많으면 성공합니다.") },
            { "blacksmith", ("대장장이", 0, 0, "단조·손패 점수 절댓값의 2배 (최대 12점).", "선택할 것이 없습니다. 전원의 비공개 무덤에서 무작위 3장을 단조하며, 단조한 카드의 점수 총합과 손패 점수 총합을 더한 절댓값의 2배가 내 점수가 됩니다 (최대 12점).") },
            { "thief", ("도적", 0, 9, "훔친 2장이 같은 종류.", "대상과 그 사람의 손패 위치를 지정해 2장을 훔쳐 내 손패에 넣습니다. 한 명을 두 번 지정해도 됩니다. 훔친 2장의 종류가 같으면 성공합니다. (손패는 비공개라 내용을 보지 못합니다.)") },
            { "mercenary", ("용병", 0, 9, "남긴 4장이 모두 같은 종류.", "내 손패 4장을 다른 플레이어에게 비공개로 제시합니다. 남길 4장의 카드가 모두 같은 종류면 성공합니다.") },
            { "seer", ("예언자", 0, 7, "손패 2장 이상, 모두 같은 종류.", "최종 손패가 2장 이상이고, 모두 같은 종류의 카드면 성공합니다.") },
            { "alchemist", ("연금술사", -1, 8, "손패에 -1·0·+1점이 각각 1장 이상.", "마지막 손패에 원래 점수가 -1점, 0점, +1점인 카드가 각각 최소 1장씩 모두 존재하면 성공합니다.") },
            { "librarian", ("사서", 1, 10, "손패에 서로 다른 카드가 4종 이상.", "마지막 손패에 서로 다른 종류의 카드가 4종 이상 있으면 성공합니다.") },
            { "mage", ("마법사", 0, 8, "손패 3장 이상, 점수 합이 정확히 0.", "마지막 손패가 3장 이상이고, 손패의 원래 점수 합이 정확히 0점이면 성공합니다.") },
            { "farmer", ("농부", 1, 7, "손패 5장 이상, 농부 3장 이상.", "마지막 손패가 5장 이상이고, 손패에 농부 카드가 3장 이상 있으면 성공합니다.") },
            { "courtesan", ("매춘부", 0, 8, "지목한 상대의 첫 카드가 최다 종류.", "지목한 플레이어의 손패 중 첫 번째 카드가 전체 게임에 남은 동일한 종류의 카드 중 가장 많은 카드 종류라면 성공합니다.") },
            { "pope", ("교황", 1, 8, "무덤 교황 6장 이상 + 내 손패에 교황 1장 이상.", "전체 무덤에 교황이 6장 이상 버려져 있고, 내 마지막 손패에 교황이 최소 1장 있으면 성공합니다.") },
            { "barbarian", ("야만인", -1, 8, "1~3R에 최다로 버림 (최소 5장).", "1~3라운드에 나를 포함해 누구보다 많은 카드를 버렸다면(최소 5장) 성공합니다. (공동 1위도 인정)") },
            { "chancellor", ("재상", 1, 7, "지목한 종류가 전원 손패 중 최다.", "지목한 카드 종류가 전원의 손패 중 가장 많다면 성공합니다. (공동 1위도 인정)") },
            { "queen", ("왕비", 1, 7, "2장 버리고 회수, 왕비 보유 수가 최다.", "손패에서 2장을 버리고 공개 무덤 1장 혹은 비공개 무덤 2장(무작위)을 가져옵니다. 마지막 손패의 왕비가 2장 이상이고, 다른 모든 플레이어보다 많아야 성공합니다. (동점은 실패)") }
        };


        [Header("MCP-authored scene references")]
        [SerializeField] private RectTransform liftVisual;
        [SerializeField] private Image cardBack;
        [SerializeField] private GameObject frontGroup;
        [SerializeField] private Image illustration;
        [SerializeField] private Image scoreBadge;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private GameObject ruleBox;
        [SerializeField] private TextMeshProUGUI ruleText;
        [SerializeField] private Image rim;
        [SerializeField] private Image sideWall;
        [SerializeField] private Button button;
        [Header("Imported card illustrations (12 per role)")]
        [SerializeField] private Sprite[] artLibrary;
        [SerializeField] private Sprite correctedFarmerArt;
        [Header("Presentation")]
        [SerializeField] private Vector2 normalSize = new Vector2(182, 254.2222f);
        [SerializeField] private Vector2 miniSize = new Vector2(122, 170.4127f);
        [SerializeField] private Color rimColor = new Color(0.973f, 0.902f, 0.718f);
        [SerializeField] private Color sideColor = new Color(0.584f, 0.494f, 0.333f);
        [SerializeField] private Color selectedRim = new Color(0.90f, 0.39f, 0.42f);
        [SerializeField] private float hoverLift = 7;
        [SerializeField] private float selectedLift = 14;
        [SerializeField] private float movementSpeed = 18;
        private readonly Dictionary<string, Sprite> artByName = new Dictionary<string, Sprite>();
        private RectTransform rect;
        private bool hovered;
        private bool isMini;
        private static TMP_FontAsset notoFont;
        public string CardId { get; private set; }
        public string RoleType { get; private set; }
        public bool IsSelected { get; private set; }
        public event Action<RealmCard> OnClicked;

        // The baked Noto Sans KR SDF atlas. Dynamic population means a glyph
        // outside the pre-baked set still renders instead of showing a box.
        public static TMP_FontAsset GetNotoTmpFont()
        {
            if (notoFont == null) notoFont = Resources.Load<TMP_FontAsset>("NotoSansKR SDF");
#if UNITY_EDITOR
            if (notoFont == null)
                notoFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/NotoSansKR SDF.asset");
#endif
            if (notoFont == null) notoFont = TMP_Settings.defaultFontAsset;
            return notoFont;
        }

        // All objects and references are authored in the scene via MCP.
        // Runtime only changes data and animates the existing hierarchy.
        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        public void Setup(string cardId, string roleType, bool selectable = true, bool mini = false)
        {
            if (cardBack == null || frontGroup == null || illustration == null)
            {
                Debug.LogError("RealmCard requires the MCP-authored RealmCardTemplate scene references.", this);
                return;
            }
            if (rect == null) rect = GetComponent<RectTransform>();
            CardId = cardId;
            RoleType = roleType;
            isMini = mini;
            hovered = false;
            rect.localScale = Vector3.one;
            rect.sizeDelta = mini ? miniSize : normalSize;
            if (button != null) button.interactable = selectable;
            bool faceUp = !string.IsNullOrEmpty(roleType);
            cardBack.gameObject.SetActive(!faceUp);
            frontGroup.SetActive(faceUp);
            SetSelected(false);
            if (!faceUp) return;

            if (artByName.Count == 0 && artLibrary != null)
                foreach (var art in artLibrary)
                    if (art != null) artByName[art.name] = art;
            int copy = 0;
            var parts = (cardId ?? "").Split('-');
            if (parts.Length > 1) int.TryParse(parts[parts.Length - 1], out copy);
            int variant = copy >= 0 && copy < 12 ? copy + 1 : 1;
            Sprite sprite;
            if (roleType == "farmer" && variant == 12 && correctedFarmerArt != null)
                sprite = correctedFarmerArt;
            else artByName.TryGetValue($"{roleType}-{variant:D2}", out sprite);
            illustration.sprite = sprite;

            if (RoleInfo.TryGetValue(roleType, out var info))
            {
                nameText.text = info.name;
                ruleText.text = info.brief;
                scoreText.text = info.score > 0 ? $"+{info.score}" : info.score.ToString();
                scoreBadge.color = info.score > 0 ? new Color(0.106f, 0.369f, 0.125f) :
                    info.score < 0 ? new Color(0.718f, 0.11f, 0.11f) : new Color(0.004f, 0.341f, 0.608f);
            }
            else
            {
                nameText.text = roleType;
                ruleText.text = "";
                scoreText.text = "0";
                scoreBadge.color = Color.gray;
            }
            ruleBox.SetActive(!mini);
            var ruleShadow = frontGroup.transform.Find("RCT_RuleShadow");
            if (ruleShadow != null) ruleShadow.gameObject.SetActive(!mini);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMax = mini ? 13 : 22;
            nameText.fontSizeMin = mini ? 10 : 16;
            nameText.fontStyle = FontStyles.Bold;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            scoreText.fontSize = mini ? 13 : 20;
            scoreText.fontStyle = FontStyles.Bold;

            // Win conditions vary a lot in length, so the rule box shrinks to
            // fit rather than clipping the tail of the longer ones.
            ruleText.enableAutoSizing = true;
            ruleText.fontSizeMax = mini ? 12 : 15;
            ruleText.fontSizeMin = mini ? 7 : 8;
            ruleText.fontStyle = FontStyles.Bold;
            // Win conditions are sentences: they wrap inside the box rather
            // than running off both edges of the card.
            ruleText.textWrappingMode = TextWrappingModes.Normal;
            ruleText.overflowMode = TextOverflowModes.Overflow;
            ruleText.alignment = TextAlignmentOptions.Center;
        }

        // Public discard cards remain compact, but retain their condition panel.
        // This only toggles MCP-authored objects; it does not create UI at runtime.
        public void SetRuleVisible(bool visible)
        {
            if (ruleBox != null) ruleBox.SetActive(visible);
            if (frontGroup != null)
            {
                var ruleShadow = frontGroup.transform.Find("RCT_RuleShadow");
                if (ruleShadow != null) ruleShadow.gameObject.SetActive(visible);
            }
            if (visible && isMini && ruleText != null)
            {
                ruleText.fontSize = 12;
                ruleText.enableAutoSizing = true;
                ruleText.fontSizeMin = 9;
                ruleText.fontSizeMax = 12;
            }
        }

        // The dense hand view scales the existing MCP-authored card template.
        // No child UI is created or replaced at runtime.
        public void SetDisplayScale(float scale)
        {
            if (rect == null) rect = GetComponent<RectTransform>();
            // The ceiling is above 1 so the grave zoom can blow a card up to a
            // readable size; hand and grave layouts still pass 0.78-1.0.
            if (rect != null) rect.localScale = Vector3.one * Mathf.Clamp(scale, 0.5f, 2.5f);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (rim != null) rim.color = selected ? selectedRim : rimColor;
            if (sideWall != null) sideWall.color = selected ? new Color(0.43f, 0.15f, 0.19f) : sideColor;
            if (!Application.isPlaying && liftVisual != null)
                liftVisual.anchoredPosition = new Vector2(0, selected ? selectedLift : 0);
        }

        private void Update()
        {
            if (liftVisual == null) return;
            float target = IsSelected ? selectedLift : hovered && !isMini ? hoverLift : 0;
            liftVisual.anchoredPosition = Vector2.Lerp(liftVisual.anchoredPosition,
                new Vector2(0, target), 1 - Mathf.Exp(-movementSpeed * Time.unscaledDeltaTime));
        }
        public void OnPointerEnter(PointerEventData e) { hovered = button != null && button.interactable; }
        public void OnPointerExit(PointerEventData e) { hovered = false; }
        private void OnDisable() { hovered = false; }
        private void HandleClick() { OnClicked?.Invoke(this); }
        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(HandleClick); }
    }
}
