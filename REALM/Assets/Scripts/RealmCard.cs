using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Realm
{
    public class RealmCard : MonoBehaviour
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

        private static readonly Dictionary<string, Sprite> ArtCache = new Dictionary<string, Sprite>();
        private static Font s_NotoFont = null;

        public string CardId { get; private set; }
        public string RoleType { get; private set; }
        public bool IsSelected { get; private set; }

        public event Action<RealmCard> OnClicked;

        private RectTransform _rect;
        private Image _cardBack;
        private GameObject _frontGroup;
        private Image _illustrationImage;
        private Image _scoreBadge;
        private Text _scoreText;
        private Text _nameText;
        private Image _ruleBoxImage;
        private Text _ruleHeader;
        private Text _ruleText;
        private Outline _selectionOutline;
        private Button _button;

        private Vector2 _baseAnchoredPosition;

        public static Font GetNotoFont()
        {
            if (s_NotoFont == null)
            {
                s_NotoFont = Resources.Load<Font>("NotoSansKR");
                if (s_NotoFont == null)
                {
#if UNITY_EDITOR
                    s_NotoFont = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansKR.ttf");
#endif
                }
                if (s_NotoFont == null)
                {
                    s_NotoFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }
            return s_NotoFont;
        }

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            EnsureUIHierarchy();
        }

        public void Setup(string cardId, string roleType, bool selectable = true, bool mini = false)
        {
            CardId = cardId;
            RoleType = roleType;
            IsSelected = false;

            if (_button != null)
            {
                _button.interactable = selectable;
            }

            if (_rect != null)
            {
                _rect.sizeDelta = mini ? new Vector2(100f, 140f) : new Vector2(136f, 190f);
            }

            if (string.IsNullOrEmpty(roleType))
            {
                // Face-down
                _cardBack.gameObject.SetActive(true);
                _frontGroup.SetActive(false);
                return;
            }

            // Face-up
            _cardBack.gameObject.SetActive(false);
            _frontGroup.SetActive(true);

            // Load illustration
            var sprite = GetOrLoadArt(cardId, roleType);
            if (sprite != null && _illustrationImage != null)
            {
                _illustrationImage.sprite = sprite;
                _illustrationImage.color = Color.white;
            }

            // Role data
            if (RoleInfo.TryGetValue(roleType, out var info))
            {
                _nameText.text = info.name;
                _ruleText.text = info.rule;

                // Score badge
                int score = info.score;
                _scoreText.text = score > 0 ? $"+{score}" : $"{score}";
                if (score > 0)
                    _scoreBadge.color = new Color(0.11f, 0.37f, 0.13f, 1f); // Green
                else if (score < 0)
                    _scoreBadge.color = new Color(0.72f, 0.11f, 0.11f, 1f); // Red
                else
                    _scoreBadge.color = new Color(0.01f, 0.34f, 0.61f, 1f); // Blue
            }
            else
            {
                _nameText.text = roleType;
                _ruleText.text = "";
                _scoreText.text = "0";
                _scoreBadge.color = Color.gray;
            }

            if (mini)
            {
                if (_ruleBoxImage != null) _ruleBoxImage.gameObject.SetActive(false);
                if (_nameText != null) _nameText.fontSize = 13;
                if (_scoreText != null) _scoreText.fontSize = 11;
            }
            else
            {
                if (_ruleBoxImage != null) _ruleBoxImage.gameObject.SetActive(true);
                if (_nameText != null) _nameText.fontSize = 16;
                if (_scoreText != null) _scoreText.fontSize = 14;
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (_selectionOutline != null)
            {
                _selectionOutline.enabled = selected;
            }

            if (_rect != null)
            {
                _rect.anchoredPosition = selected ? _baseAnchoredPosition + new Vector2(0, 18f) : _baseAnchoredPosition;
            }
        }

        private void OnButtonClick()
        {
            OnClicked?.Invoke(this);
        }

        private Sprite GetOrLoadArt(string cardId, string roleType)
        {
            int variant = 1;
            if (!string.IsNullOrEmpty(cardId))
            {
                var parts = cardId.Split('-');
                if (parts.Length > 1 && int.TryParse(parts[parts.Length - 1], out int copy))
                {
                    variant = (copy >= 0 && copy < 12) ? copy + 1 : 1;
                }
            }

            string key = $"{roleType}-{variant:D2}";
            if (ArtCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            string diskPath = Path.Combine(Application.dataPath, "Textures", "RoleCardArt", $"{key}.jpg");
            if (File.Exists(diskPath))
            {
                byte[] bytes = File.ReadAllBytes(diskPath);
                var tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    ArtCache[key] = spr;
                    return spr;
                }
            }

            return null;
        }

        private void EnsureUIHierarchy()
        {
            Font font = GetNotoFont();

            if (_rect == null) _rect = gameObject.AddComponent<RectTransform>();
            _rect.sizeDelta = new Vector2(136f, 190f);

            _button = GetComponent<Button>();
            if (_button == null) _button = gameObject.AddComponent<Button>();
            _button.transition = Selectable.Transition.ColorTint;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnButtonClick);

            _selectionOutline = GetComponent<Outline>();
            if (_selectionOutline == null) _selectionOutline = gameObject.AddComponent<Outline>();
            _selectionOutline.effectColor = new Color(1f, 0.88f, 0.45f, 1f); // Warm gold glow
            _selectionOutline.effectDistance = new Vector2(3.5f, 3.5f);
            _selectionOutline.enabled = false;

            // 1. Card Back
            var backTrans = transform.Find("CardBack") as RectTransform;
            if (backTrans == null)
            {
                var backGo = new GameObject("CardBack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                backGo.transform.SetParent(transform, false);
                backTrans = backGo.GetComponent<RectTransform>();
            }
            backTrans.anchorMin = Vector2.zero;
            backTrans.anchorMax = Vector2.one;
            backTrans.offsetMin = Vector2.zero;
            backTrans.offsetMax = Vector2.zero;
            _cardBack = backTrans.GetComponent<Image>();
            _cardBack.color = new Color(0.12f, 0.22f, 0.35f, 1f);

            // Load CardBack_Cropped sprite if available
            string backPath = Path.Combine(Application.dataPath, "Textures", "CardBack_Cropped.jpg");
            if (File.Exists(backPath) && !ArtCache.ContainsKey("CARDBACK"))
            {
                byte[] b = File.ReadAllBytes(backPath);
                var t = new Texture2D(2, 2);
                if (t.LoadImage(b))
                {
                    ArtCache["CARDBACK"] = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
                }
            }
            if (ArtCache.TryGetValue("CARDBACK", out var backSpr))
            {
                _cardBack.sprite = backSpr;
                _cardBack.color = Color.white;
            }

            // 2. Front Group
            var frontTrans = transform.Find("FrontGroup") as RectTransform;
            if (frontTrans == null)
            {
                var frontGo = new GameObject("FrontGroup", typeof(RectTransform));
                frontGo.transform.SetParent(transform, false);
                frontTrans = frontGo.GetComponent<RectTransform>();
            }
            frontTrans.anchorMin = Vector2.zero;
            frontTrans.anchorMax = Vector2.one;
            frontTrans.offsetMin = Vector2.zero;
            frontTrans.offsetMax = Vector2.zero;
            _frontGroup = frontTrans.gameObject;

            // 2a. Full illustration
            var illTrans = frontTrans.Find("Illustration") as RectTransform;
            if (illTrans == null)
            {
                var illGo = new GameObject("Illustration", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                illGo.transform.SetParent(frontTrans, false);
                illTrans = illGo.GetComponent<RectTransform>();
            }
            illTrans.anchorMin = Vector2.zero;
            illTrans.anchorMax = Vector2.one;
            illTrans.offsetMin = Vector2.zero;
            illTrans.offsetMax = Vector2.zero;
            _illustrationImage = illTrans.GetComponent<Image>();
            _illustrationImage.color = new Color(0.07f, 0.15f, 0.23f, 1f);

            // 2b. Top Header Banner
            var bannerTrans = frontTrans.Find("TopBanner") as RectTransform;
            if (bannerTrans == null)
            {
                var bannerGo = new GameObject("TopBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bannerGo.transform.SetParent(frontTrans, false);
                bannerTrans = bannerGo.GetComponent<RectTransform>();
            }
            bannerTrans.anchorMin = new Vector2(0f, 0.77f);
            bannerTrans.anchorMax = Vector2.one;
            bannerTrans.offsetMin = Vector2.zero;
            bannerTrans.offsetMax = Vector2.zero;
            var bannerImg = bannerTrans.GetComponent<Image>();
            bannerImg.color = new Color(0.04f, 0.12f, 0.20f, 0.90f);

            // 2c. Score Badge inside banner
            var badgeTrans = bannerTrans.Find("ScoreBadge") as RectTransform;
            if (badgeTrans == null)
            {
                var badgeGo = new GameObject("ScoreBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                badgeGo.transform.SetParent(bannerTrans, false);
                badgeTrans = badgeGo.GetComponent<RectTransform>();
            }
            badgeTrans.anchorMin = new Vector2(0.04f, 0.12f);
            badgeTrans.anchorMax = new Vector2(0.25f, 0.88f);
            badgeTrans.offsetMin = Vector2.zero;
            badgeTrans.offsetMax = Vector2.zero;
            _scoreBadge = badgeTrans.GetComponent<Image>();

            var scoreTextTrans = badgeTrans.Find("Text") as RectTransform;
            if (scoreTextTrans == null)
            {
                var stGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                stGo.transform.SetParent(badgeTrans, false);
                scoreTextTrans = stGo.GetComponent<RectTransform>();
            }
            scoreTextTrans.anchorMin = Vector2.zero;
            scoreTextTrans.anchorMax = Vector2.one;
            scoreTextTrans.offsetMin = Vector2.zero;
            scoreTextTrans.offsetMax = Vector2.zero;
            _scoreText = scoreTextTrans.GetComponent<Text>();
            _scoreText.font = font;
            _scoreText.fontSize = 14;
            _scoreText.fontStyle = FontStyle.Bold;
            _scoreText.alignment = TextAnchor.MiddleCenter;
            _scoreText.color = Color.white;

            // 2d. Role Name Text inside banner
            var nameTrans = bannerTrans.Find("RoleName") as RectTransform;
            if (nameTrans == null)
            {
                var nameGo = new GameObject("RoleName", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                nameGo.transform.SetParent(bannerTrans, false);
                nameTrans = nameGo.GetComponent<RectTransform>();
            }
            nameTrans.anchorMin = new Vector2(0.28f, 0.05f);
            nameTrans.anchorMax = new Vector2(0.96f, 0.95f);
            nameTrans.offsetMin = Vector2.zero;
            nameTrans.offsetMax = Vector2.zero;
            _nameText = nameTrans.GetComponent<Text>();
            _nameText.font = font;
            _nameText.fontSize = 16;
            _nameText.fontStyle = FontStyle.Bold;
            _nameText.alignment = TextAnchor.MiddleLeft;
            _nameText.color = new Color(1f, 0.91f, 0.65f, 1f); // #ffe7a7

            // 2e. Bottom Rule Box (using card-rule-slot-v1)
            var ruleBoxTrans = frontTrans.Find("RuleBox") as RectTransform;
            if (ruleBoxTrans == null)
            {
                var rbGo = new GameObject("RuleBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                rbGo.transform.SetParent(frontTrans, false);
                ruleBoxTrans = rbGo.GetComponent<RectTransform>();
            }
            ruleBoxTrans.anchorMin = new Vector2(0.04f, 0.04f);
            ruleBoxTrans.anchorMax = new Vector2(0.96f, 0.36f);
            ruleBoxTrans.offsetMin = Vector2.zero;
            ruleBoxTrans.offsetMax = Vector2.zero;
            _ruleBoxImage = ruleBoxTrans.GetComponent<Image>();
            _ruleBoxImage.color = new Color(0.98f, 0.97f, 0.94f, 0.96f);

            // Load card-rule-slot-v1.png if available
            string ruleSlotPath = Path.Combine(Application.dataPath, "Textures", "card-rule-slot-v1.png");
            if (File.Exists(ruleSlotPath) && !ArtCache.ContainsKey("RULE_SLOT"))
            {
                byte[] b = File.ReadAllBytes(ruleSlotPath);
                var t = new Texture2D(2, 2);
                if (t.LoadImage(b))
                {
                    ArtCache["RULE_SLOT"] = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
                }
            }
            if (ArtCache.TryGetValue("RULE_SLOT", out var ruleSlotSpr))
            {
                _ruleBoxImage.sprite = ruleSlotSpr;
                _ruleBoxImage.color = Color.white;
            }

            var rbOutline = ruleBoxTrans.GetComponent<Outline>();
            if (rbOutline == null) rbOutline = ruleBoxTrans.gameObject.AddComponent<Outline>();
            rbOutline.effectColor = new Color(0.25f, 0.18f, 0.10f, 0.8f);
            rbOutline.effectDistance = new Vector2(1f, -1f);

            // Rule Header "[승리조건]"
            var rhTrans = ruleBoxTrans.Find("Header") as RectTransform;
            if (rhTrans == null)
            {
                var rhGo = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                rhGo.transform.SetParent(ruleBoxTrans, false);
                rhTrans = rhGo.GetComponent<RectTransform>();
            }
            rhTrans.anchorMin = new Vector2(0.06f, 0.68f);
            rhTrans.anchorMax = new Vector2(0.94f, 0.98f);
            rhTrans.offsetMin = Vector2.zero;
            rhTrans.offsetMax = Vector2.zero;
            _ruleHeader = rhTrans.GetComponent<Text>();
            _ruleHeader.font = font;
            _ruleHeader.fontSize = 10;
            _ruleHeader.fontStyle = FontStyle.Bold;
            _ruleHeader.alignment = TextAnchor.MiddleLeft;
            _ruleHeader.color = new Color(0.83f, 0.18f, 0.18f, 1f); // Red
            _ruleHeader.text = "승리조건";

            // Rule Description Text
            var rdTrans = ruleBoxTrans.Find("Description") as RectTransform;
            if (rdTrans == null)
            {
                var rdGo = new GameObject("Description", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                rdGo.transform.SetParent(ruleBoxTrans, false);
                rdTrans = rdGo.GetComponent<RectTransform>();
            }
            rdTrans.anchorMin = new Vector2(0.06f, 0.04f);
            rdTrans.anchorMax = new Vector2(0.94f, 0.70f);
            rdTrans.offsetMin = Vector2.zero;
            rdTrans.offsetMax = Vector2.zero;
            _ruleText = rdTrans.GetComponent<Text>();
            _ruleText.font = font;
            _ruleText.fontSize = 10;
            _ruleText.alignment = TextAnchor.UpperLeft;
            _ruleText.color = new Color(0.10f, 0.10f, 0.10f, 1f);
            _ruleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _ruleText.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }
}
