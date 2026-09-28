using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Realm
{
    /// <summary>Cathedral ornament stays outside the protected reading surfaces.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(900)]
    public sealed class RealmUiSkin : MonoBehaviour
    {
        private static readonly Color Paper = new Color32(245, 235, 214, 255);
        private static readonly Color Muted = new Color32(188, 198, 216, 255);
        private static readonly Color Gold = new Color32(202, 166, 94, 255);
        private static readonly Color Navy = new Color32(14, 23, 39, 255);
        private static readonly Color Field = new Color32(23, 37, 58, 255);
        private static readonly Color Blue = new Color32(35, 65, 109, 255);
        private readonly HashSet<Button> _styledButtons = new HashSet<Button>();
        private TMP_FontAsset _font;
        private float _nextScan;

        private void Start() { Rebuild(); }
        private void LateUpdate()
        {
            // Scan for new runtime controls; do not continually overwrite live text/status.
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + .75f;
            StyleNewButtons();
        }

        [ContextMenu("Rebuild Cathedral UI")]
        public void Rebuild()
        {
            _font = RealmCard.GetNotoTmpFont();
            _styledButtons.Clear();
            Hide(transform.Find("RealmAmbientBackdrop"));
            Hide(transform.Find("RealmAmbientCrest"));
            LayoutHeader();
            LayoutLobby();
            LayoutWaitingRoom();
            LayoutSplash();
            StyleNewButtons();
        }

        private void LayoutHeader()
        {
            var h = transform.Find("LobbyHeaderTemplate");
            if (h == null) return;
            Box(h, .12f, .80f, .88f, .98f);
            Clear(h);
            Hide(h.Find("LobbyTitle"));
            Hide(h.Find("Underline"));
            var logo = h.Find("LogoLockup");
            // The mosaic wordmark is cobalt glass inside heavy black leading, so
            // on the near-black header wall the outline merges with the
            // background and it turns to mud. The solid gold lockup is the
            // legible one at this size.
            var image = logo != null ? logo.GetComponent<Image>() : null;
            var lockup = Resources.Load<Sprite>("rock_up");
            if (image != null && lockup != null)
            {
                image.sprite = lockup; image.color = Color.white; image.preserveAspect = true;
                // preserveAspect letterboxes and centres the art when the box
                // aspect differs, which left the logo floating mid-header. Match
                // the box to the art and pin it to the left edge instead.
                var rect = (RectTransform)logo;
                rect.anchorMin = new Vector2(0f, .34f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, .5f);
                rect.sizeDelta = new Vector2(rect.rect.height * (lockup.rect.width / lockup.rect.height), 0f);
                rect.anchoredPosition = Vector2.zero;
                var shadow = logo.GetComponent<Shadow>() ?? logo.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, .75f);
                shadow.effectDistance = new Vector2(0f, -3f);
                shadow.useGraphicAlpha = true;
            }
            else Box(logo, 0, .40f, .38f, 1);
            // Muted blue-grey sat too close to the wall; warm cream reads.
            Label(h.Find("LobbySubtitle"), "가면 뒤의 직업, 마지막에 드러나는 진실.", 27, Paper);
            Box(h.Find("LobbySubtitle"), .01f, .04f, .69f, .30f);
            Box(h.Find("OpenCardReview"), .76f, .38f, 1, .76f);
            TextIn(h.Find("OpenCardReview"), "직업 도감");
        }

        private void LayoutLobby()
        {
            var l = transform.Find("LobbyContent");
            if (l == null) return;
            Box(l, .12f, .16f, .88f, .77f);
            Clear(l);
            var identity = Child(l, "PlayerIdentity");
            Box(identity, 0, .83f, 1, 1);
            Surface(identity, Navy, false);
            LabelChild(identity, "IdentityLabel", "플레이어 이름", 28, Paper, .035f, .26f, .23f, .76f);
            // Preserve the controller's InputField reference when moving the shared identity.
            var input = l.Find("CreateRoomPanel/HostNameInput") ?? identity.Find("HostNameInput");
            if (input != null) input.SetParent(identity, false);
            Box(input, .245f, .20f, .66f, .80f);
            StyleInput(input, "함께 플레이할 이름을 입력하세요");
            LabelChild(identity, "IdentityHint", "만들기 · 참가에 함께 사용", 23, Muted, .69f, .20f, .975f, .80f);

            var c = l.Find("CreateRoomPanel");
            var j = l.Find("JoinRoomPanel");
            Box(c, 0, .10f, .482f, .77f);
            Box(j, .518f, .10f, 1, .77f);
            Panel(c); Panel(j);
            LabelChild(c, "SectionNumber", "01  /  HOST", 21, Gold, .06f, .85f, .94f, .93f);
            LabelChild(j, "SectionNumber", "02  /  GUEST", 21, Gold, .06f, .85f, .94f, .93f);
            Label(c.Find("CreateRoomTitle"), "새로운 방 만들기", 36, Paper);
            Label(j.Find("JoinRoomTitle"), "초대받은 방에 참가", 36, Paper);
            c.Find("CreateRoomTitle").GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
            j.Find("JoinRoomTitle").GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
            Box(c.Find("CreateRoomTitle"), .06f, .68f, .94f, .83f);
            Box(j.Find("JoinRoomTitle"), .06f, .68f, .94f, .83f);
            Label(c.Find("CreateRoomDescription"), "인원을 정하고 친구들을 초대하세요.", 26, Muted);
            Label(j.Find("JoinRoomDescription"), "방장이 알려준 초대 코드를 입력하세요.", 26, Muted);
            Box(c.Find("CreateRoomDescription"), .06f, .55f, .94f, .67f);
            Box(j.Find("JoinRoomDescription"), .06f, .55f, .94f, .67f);
            Label(c.Find("PlayerTargetCaption"), "참가 인원", 27, Paper);
            Box(c.Find("PlayerTargetCaption"), .06f, .32f, .43f, .47f);
            var dropdown = c.Find("PlayerTargetDropdown");
            Box(dropdown, .48f, .32f, .94f, .47f);
            StyleDropdown(dropdown);
            Box(j.Find("RoomCodeInput"), .06f, .32f, .94f, .47f);
            StyleInput(j.Find("RoomCodeInput"), "초대 코드 입력");
            Box(c.Find("CreateRoomButtonTemplate"), .06f, .08f, .94f, .25f);
            Box(j.Find("JoinRoomButton"), .06f, .08f, .94f, .25f);
            TextIn(c.Find("CreateRoomButtonTemplate"), "방 만들기");
            TextIn(j.Find("JoinRoomButton"), "코드로 참가하기");
            var guide = l.Find("GuidePanel");
            Clear(guide);
            Box(guide, 0, -.035f, 1, .05f);
            Label(guide.Find("GuideText"), "5–10명  ·  15–40분  ·  네 번의 버리기, 그리고 마지막 직업 추리", 24, Muted, TextAlignmentOptions.Center);
            Box(guide.Find("GuideText"), 0, 0, 1, 1);
            var footer = transform.Find("LobbyFooter");
            Box(footer, .12f, .045f, .88f, .105f);
            Surface(footer, Navy, false);
            var status = footer.Find("LobbyStatus");
            Label(status, null, 25, Paper, TextAlignmentOptions.Center);
            var statusText = status != null ? status.GetComponent<TMP_Text>() : null;
            if (statusText != null && statusText.text.Contains("LAN 호스트"))
                statusText.text = "이름을 입력한 뒤 방을 만들거나 초대 코드로 참가하세요.";
            Box(status, .02f, .08f, .98f, .92f);
        }

        private void LayoutWaitingRoom()
        {
            var w = transform.Find("RoomWaitingPanel");
            if (w == null) return;
            Clear(w);
            var h = w.Find("WaitingHeader");
            Clear(h); Hide(h.Find("LogoLockup")); Hide(h.Find("Underline"));
            Box(h, .12f, .81f, .88f, .97f);
            Label(h.Find("WaitingTitle"), "참가자를 기다리는 중", 36, Paper);
            h.Find("WaitingTitle").gameObject.SetActive(true);
            Label(h.Find("WaitingSubtitle"), "친구에게 방 코드를 공유해 함께 입장하세요.", 26, Muted);
            Box(h.Find("WaitingTitle"), 0, .46f, .58f, .93f);
            Box(h.Find("WaitingSubtitle"), 0, .08f, .60f, .39f);
            var badge = h.Find("WaitingRoomCodeBadge");
            Box(badge, .64f, .18f, 1, .82f);
            Surface(badge, Field, true);
            Label(badge.Find("WaitingRoomCode"), null, 28, Paper, TextAlignmentOptions.Center);
            var roster = w.Find("PlayerRosterPanel");
            var controls = w.Find("WaitingControlPanel");
            Box(roster, .12f, .18f, .56f, .77f);
            Box(controls, .59f, .18f, .88f, .77f);
            Panel(roster); Panel(controls);
            Label(roster.Find("RosterTitle"), null, 29, Paper);
            Box(roster.Find("RosterTitle"), .06f, .86f, .94f, .95f);
            Box(roster.Find("RosterList"), .06f, .05f, .94f, .82f);
            var list = roster.Find("RosterList");
            var layout = list.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 5;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
            }
            foreach (Transform slot in list)
            {
                var size = slot.GetComponent<LayoutElement>() ?? slot.gameObject.AddComponent<LayoutElement>();
                size.minHeight = size.preferredHeight = 44;
                size.flexibleHeight = 0;
            }
            foreach (var text in roster.GetComponentsInChildren<TMP_Text>(true))
                if (text.name != "RosterTitle") Label(text.transform, null, 26, text.name.Contains("State") ? Muted : Paper);
            Label(controls.Find("WaitingControlTitle"), "게임 준비", 32, Paper);
            Box(controls.Find("WaitingControlTitle"), .08f, .81f, .92f, .94f);
            Label(controls.Find("WaitingNetworkStatus"), null, 25, Muted);
            Box(controls.Find("WaitingNetworkStatus"), .08f, .65f, .92f, .78f);
            Label(controls.Find("WaitingInstructions"), null, 26, Paper);
            Box(controls.Find("WaitingInstructions"), .08f, .36f, .92f, .63f);
            Box(controls.Find("AddBotButton"), .08f, .21f, .92f, .32f);
            Box(controls.Find("StartGameButton"), .08f, .065f, .92f, .18f);
        }

        private void LayoutSplash()
        {
            var s = transform.Find("SplashPanel");
            if (s == null) return;
            var image = s.GetComponent<Image>();
            if (image != null) { image.sprite = Resources.Load<Sprite>("title"); image.type = Image.Type.Simple; image.color = Color.white; }
            var button = s.GetComponent<Button>();
            if (button != null) button.transition = Selectable.Transition.None;
            var shade = Child(s, "TitleReadingSurface");
            Box(shade, 0, 0, 1, .32f);
            Surface(shade, new Color(.025f, .035f, .055f, .94f), false);
            shade.SetSiblingIndex(1);
            var wordmark = s.Find("SplashWordmark");
            Box(wordmark, .22f, .15f, .78f, .32f);
            var wordImage = wordmark.GetComponent<Image>();
            if (wordImage != null) { wordImage.sprite = Resources.Load<Sprite>("title_02"); wordImage.preserveAspect = true; }
            Label(s.Find("Tagline"), "5–10인 · 숨은 직업 추리 카드 게임", 26, Paper, TextAlignmentOptions.Center);
            s.Find("Tagline").gameObject.SetActive(true);
            Box(s.Find("Tagline"), .2f, .105f, .8f, .15f);
            Label(s.Find("StartPrompt"), "클릭하거나 아무 키를 눌러 입장", 28, Paper, TextAlignmentOptions.Center);
            Box(s.Find("StartPrompt"), .2f, .025f, .8f, .078f);
        }

        private void StyleNewButtons()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (_styledButtons.Contains(button) || button.GetComponentInParent<RealmCard>(true) != null ||
                    button.GetComponentInParent<RealmSplash>(true) != null) continue;
                var labels = button.GetComponentsInChildren<TMP_Text>(true);
                // Multi-label role tiles own their score, name, rules and selection visuals.
                if (labels.Length != 1) continue;
                var label = labels[0];
                string key = button.name.ToLowerInvariant();
                bool primary = key.Contains("create") || key.Contains("start") || key.Contains("confirm") || key.Contains("send");
                bool ghost = key.Contains("cancel") || key.Contains("close") || key.Contains("return");
                var image = Surface(button.transform, primary ? Gold : ghost ? Navy : Blue, true);
                image.raycastTarget = true;
                button.targetGraphic = image;
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = new Color(.75f, .75f, .75f, 1);
                colors.disabledColor = new Color(.45f, .45f, .45f, .72f);
                colors.colorMultiplier = 1;
                colors.fadeDuration = .12f;
                button.colors = colors;
                float height = ((RectTransform)button.transform).rect.height;
                Label(label.transform, null, Mathf.Clamp(height * .4f, 18, 29), primary ? Navy : Paper, TextAlignmentOptions.Center);
                label.fontStyle = FontStyles.Bold;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                Box(label.transform, .08f, .08f, .92f, .92f);
                // Small leaded-glass inlays occupy only the ends, outside the label.
                if (height >= 48)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var inlay = Child(button.transform, "GlassInlay" + i) as RectTransform;
                        inlay.anchorMin = inlay.anchorMax = new Vector2(i == 0 ? .035f : .965f, .5f);
                        inlay.anchoredPosition = Vector2.zero;
                        inlay.sizeDelta = new Vector2(9, 9);
                        inlay.localRotation = Quaternion.Euler(0, 0, 45);
                        Surface(inlay, primary ? Blue : Gold, false);
                    }
                }
                _styledButtons.Add(button);
            }
        }

        private void StyleInput(Transform t, string placeholder)
        {
            if (t == null) return;
            Surface(t, Field, true).raycastTarget = true;
            var input = t.GetComponent<InputField>();
            if (input == null) return;
            input.customCaretColor = true; input.caretColor = Paper;
            input.selectionColor = new Color(.32f, .48f, .70f, .65f);
            foreach (var text in t.GetComponentsInChildren<Text>(true))
            {
                text.fontSize = 27;
                text.color = text == input.placeholder ? Muted : Paper;
                text.alignment = TextAnchor.MiddleLeft;
                text.raycastTarget = false;
                Box(text.transform, .035f, .12f, .965f, .88f);
            }
            if (input.placeholder is Text ph) ph.text = placeholder;
        }

        private void StyleDropdown(Transform t)
        {
            if (t == null) return;
            Surface(t, Field, true).raycastTarget = true;
            var dropdown = t.GetComponent<TMP_Dropdown>();
            if (dropdown == null) return;
            dropdown.alphaFadeSpeed = 0;
            Label(dropdown.captionText.transform, null, 27, Paper);
            Box(dropdown.captionText.transform, .07f, .12f, .81f, .88f);
            if (dropdown.itemText != null) Label(dropdown.itemText.transform, null, 27, Paper);
            if (dropdown.template != null)
            {
                Surface(dropdown.template, Field, true).raycastTarget = true;
                dropdown.template.sizeDelta = new Vector2(dropdown.template.sizeDelta.x, 300);
                var item = dropdown.itemText != null ? dropdown.itemText.transform.parent as RectTransform : null;
                if (item != null) item.sizeDelta = new Vector2(item.sizeDelta.x, 48);
                foreach (var toggle in dropdown.template.GetComponentsInChildren<Toggle>(true))
                {
                    var colors = toggle.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(.55f, .70f, .95f, 1);
                    colors.selectedColor = colors.highlightedColor;
                    colors.pressedColor = new Color(.45f, .60f, .85f, 1);
                    toggle.colors = colors;
                }
            }
        }

        private void Panel(Transform panel)
        {
            if (panel == null) return;
            Hide(panel.Find("Frame"));
            Surface(panel, Navy, true);
            var band = Child(panel, "MosaicCornice");
            Box(band, .025f, .976f, .975f, .989f);
            for (int i = 0; i < 21; i++)
            {
                var tile = Child(band, "Tessera" + i);
                Box(tile, i / 21f, 0, (i + .78f) / 21f, 1);
                Surface(tile, i % 4 == 0 ? Gold : i % 2 == 0 ? new Color32(63, 93, 145, 255) : Blue, false);
            }
        }

        private static Image Surface(Transform t, Color color, bool border)
        {
            var image = t.GetComponent<Image>() ?? t.gameObject.AddComponent<Image>();
            image.sprite = null; image.type = Image.Type.Simple;
            image.color = color; image.raycastTarget = false;
            var outline = t.GetComponent<Outline>();
            if (border && outline == null) outline = t.gameObject.AddComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = border;
                outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, .65f);
                outline.effectDistance = new Vector2(1, -1);
            }
            return image;
        }

        private static void Clear(Transform t)
        {
            if (t == null) return;
            var image = t.GetComponent<Image>();
            if (image != null) { image.sprite = null; image.color = Color.clear; image.raycastTarget = false; }
            var outline = t.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
        }

        private void Label(Transform t, string value, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            if (t == null) return;
            var text = t.GetComponent<TMP_Text>();
            if (text == null) return;
            if (value != null) text.text = value;
            if (_font != null) text.font = _font;
            text.enableAutoSizing = false; text.fontSize = size; text.fontStyle = FontStyles.Normal;
            text.color = color; text.alignment = align; text.margin = Vector4.zero;
            text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false;
        }

        private void LabelChild(Transform parent, string name, string value, float size, Color color, float x0, float y0, float x1, float y1)
        {
            var child = Child(parent, name);
            if (child.GetComponent<TMP_Text>() == null) child.gameObject.AddComponent<TextMeshProUGUI>();
            Box(child, x0, y0, x1, y1);
            Label(child, value, size, color);
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return child;
            child = new GameObject(name, typeof(RectTransform)).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static void Box(Transform t, float x0, float y0, float x1, float y1)
        {
            if (!(t is RectTransform rect)) return;
            rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one;
        }

        private static void TextIn(Transform t, string value)
        {
            var label = t != null ? t.GetComponentInChildren<TMP_Text>(true) : null;
            if (label != null) label.text = value;
        }
        private static void Hide(Transform t) { if (t != null) t.gameObject.SetActive(false); }
    }
}
