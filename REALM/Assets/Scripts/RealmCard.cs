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
        public static readonly Dictionary<string, (string name, int score, string rule)> RoleInfo = new Dictionary<string, (string, int, string)>
        {
            { "king", ("왕", -1, "전체 무덤의 왕 카드가 9장 이상.") },
            { "noble", ("귀족", 1, "전원이 4라운드에 비공개로 버린 귀족 카드가 정확히 3장.") },
            { "assassin", ("암살자", 0, "서로 다른 두 상대의 직업을 모두 맞힘.") },
            { "beggar", ("거지", -1, "전원 손패 합계가 인원수 × 2보다 적음.") },
            { "slave", ("노예", 0, "미공개 상대의 직업을 맞히고 그 직업이 성공.") },
            { "jester", ("광대", -1, "암살자에게 광대가 아닌 다른 직업으로 지목당함.") },
            { "priest", ("성직자", 1, "전원이 성직자 카드를 1장 이상 보유.") },
            { "knight", ("기사", 1, "기사가 전체 무덤에서 최저 버림 종류 중 하나.") },
            { "bard", ("음유시인", 0, "손패가 음유시인뿐이고 막라에 1장 이상 버림.") },
            { "hunter", ("사냥꾼", 0, "상대 손패의 카드 종류를 맞힘.") },
            { "commoner", ("평민", 1, "평민이 전원 손패에 가장 많이 남은 종류 중 하나.") },
            { "merchant", ("상인", 1, "전원 손패 합계가 인원수 × 2보다 많음.") },
            { "blacksmith", ("대장장이", 0, "막라 더미 3장 단조. 절댓값 2배, 최대 12점.") },
            { "thief", ("도적", 0, "강탈한 두 카드가 같은 종류.") },
            { "mercenary", ("용병", 0, "공개 무덤과 최대 2장 교환 후 지정한 4장이 모두 같은 종류.") },
            { "seer", ("점술가", 0, "손패 2장 이상, 모두 같은 종류.") },
            { "alchemist", ("연금술사", -1, "손에 -1·0·+1점 카드가 각각 1장 이상.") },
            { "librarian", ("사서", 1, "선택된 직업 카드 5종을 각각 1장 이상 보유.") },
            { "mage", ("마법사", 0, "손패 3장 이상, 카드 점수 합이 정확히 0.") },
            { "farmer", ("농민", 1, "손패 5장 이상, 농민 카드 3장 이상.") },
            { "courtesan", ("기생", 0, "지목한 미공개 상대 직업 카드가 내 손에 2장 이상.") },
            { "pope", ("교황", 1, "무덤 교황 6장 이상 + 내 손에 교황 1장 이상.") },
            { "barbarian", ("바바리안", -1, "1~3라 공개 버림 5장 이상이며 전원 최다.") },
            { "chancellor", ("재상", 1, "전원 손패에서 가장 많은 카드 종류를 예측.") },
            { "queen", ("왕비", 1, "왕비 2장 이상, 왕비 보유 수 단독 최다.") }
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
                ruleText.text = info.rule;
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
            if (rect != null) rect.localScale = Vector3.one * Mathf.Clamp(scale, 0.5f, 1f);
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
