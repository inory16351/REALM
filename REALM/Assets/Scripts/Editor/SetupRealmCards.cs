using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Realm.Editor
{
    public static class SetupRealmCards
    {
        [MenuItem("Realm/Build Complete Game Screen In Scene")]
        public static void BuildCompleteGameScreen()
        {
            var canvas = GameObject.Find("RealmLobbyCanvas");
            if (canvas == null)
            {
                Debug.LogError("Could not find RealmLobbyCanvas!");
                return;
            }

            TMP_FontAsset notoFont = Realm.RealmCard.GetNotoTmpFont();

            // Remove old RealmGameUI or GamePlayPanel if exists
            var oldPanel = canvas.transform.Find("RealmGameUI");
            if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);
            var oldGpp = canvas.transform.Find("GamePlayPanel");
            if (oldGpp != null) Object.DestroyImmediate(oldGpp.gameObject);

            // 1. Root GamePlayPanel (Stretch 0,0 to 1,1)
            var panelGo = new GameObject("GamePlayPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RealmGameUI));
            panelGo.transform.SetParent(canvas.transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var bgImg = panelGo.GetComponent<Image>();
            bgImg.color = new Color(0.03f, 0.06f, 0.11f, 0.99f); // Medieval dark background

            var gameUI = panelGo.GetComponent<RealmGameUI>();

            // Assign CardPrefab
            var cardPrefab = canvas.transform.Find("CardPrefab")?.gameObject;
            gameUI.cardPrefab = cardPrefab;

            // ==========================================
            // A. MAIN PLAY AREA (upper-left, leaving a clear bottom lane)
            // ==========================================
            var mainArea = CreateBox("MainPlayArea", panelGo.transform, new Vector2(0.01f, 0.40f), new Vector2(0.73f, 0.98f), new Color(0.05f, 0.10f, 0.17f, 0.85f));
            var maOutl = mainArea.gameObject.AddComponent<Outline>();
            maOutl.effectColor = new Color(0.2f, 0.35f, 0.5f, 0.4f);

            // A1. Top Header (Y 0.88 to 0.98)
            var topHeader = CreateBox("TopHeader", mainArea, new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.98f), new Color(0.03f, 0.08f, 0.15f, 0.9f));
            gameUI.roundEyebrowText = CreateText("RoundEyebrowText", topHeader, new Vector2(0.02f, 0.55f), new Vector2(0.98f, 0.95f), 13, new Color(0.7f, 0.85f, 1f), TextAlignmentOptions.Left, notoFont);
            gameUI.roundEyebrowText.text = "1라운드 · 공개 버리기";

            gameUI.turnHeadingText = CreateText("TurnHeadingText", topHeader, new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.55f), 20, new Color(1f, 0.90f, 0.45f), TextAlignmentOptions.Left, notoFont, FontStyles.Bold);
            gameUI.turnHeadingText.text = "당신의 차례입니다.";

            // A2. Players Strip (Y 0.77 to 0.86)
            var pStrip = CreateBox("PlayersStrip", mainArea, new Vector2(0.02f, 0.77f), new Vector2(0.98f, 0.86f), Color.clear);
            var phlg = pStrip.gameObject.AddComponent<HorizontalLayoutGroup>();
            phlg.spacing = 10;
            phlg.childAlignment = TextAnchor.MiddleLeft;
            phlg.childControlWidth = false;
            phlg.childControlHeight = false;
            phlg.childForceExpandWidth = false;
            phlg.childForceExpandHeight = false;
            gameUI.playersStripContainer = pStrip;

            // Preview player pills
            for (int i = 0; i < 5; i++)
            {
                var pill = CreateBox($"PlayerPill_{i}", pStrip, Vector2.zero, Vector2.one, (i == 0) ? new Color(0.18f, 0.45f, 0.65f, 0.95f) : new Color(0.06f, 0.15f, 0.25f, 0.85f));
                pill.sizeDelta = new Vector2(145f, 42f);
                var pol = pill.gameObject.AddComponent<Outline>();
                pol.effectColor = (i == 0) ? new Color(1f, 0.85f, 0.4f) : new Color(0.25f, 0.38f, 0.5f);
                var ptxt = CreateText("Text", pill, Vector2.zero, Vector2.one, 12, Color.white, TextAlignmentOptions.Center, notoFont);
                ptxt.rectTransform.offsetMin = new Vector2(6f, 2f);
                ptxt.rectTransform.offsetMax = new Vector2(-6f, -2f);
                ptxt.text = (i == 0) ? "<b>방장</b> (나) (10장)" : $"<b>봇 {i + 1}</b> (10장)";
            }

            // A3. Notices Group (Y 0.59 to 0.75)
            var notices = CreateBox("Notices", mainArea, new Vector2(0.02f, 0.59f), new Vector2(0.98f, 0.75f), new Color(0.03f, 0.07f, 0.13f, 0.95f));
            var nOutl = notices.gameObject.AddComponent<Outline>();
            nOutl.effectColor = new Color(0.25f, 0.4f, 0.55f, 0.5f);

            gameUI.secretRoleText = CreateText("SecretRoleText", notices, new Vector2(0.03f, 0.68f), new Vector2(0.97f, 0.96f), 14, Color.white, TextAlignmentOptions.Left, notoFont);
            gameUI.secretRoleText.text = "당신의 비밀 직업: <color=#FFE08B><b>국왕</b></color> — 전체 무덤의 왕 카드가 9장 이상.";

            gameUI.lastRollText = CreateText("LastRollText", notices, new Vector2(0.03f, 0.36f), new Vector2(0.97f, 0.65f), 13, new Color(0.5f, 0.9f, 0.8f), TextAlignmentOptions.Left, notoFont);
            gameUI.lastRollText.text = "최근 D6 · <b>방장</b> → <b>2</b>";

            gameUI.turnPromptText = CreateText("TurnPromptText", notices, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.33f), 13, new Color(0.9f, 0.93f, 0.97f), TextAlignmentOptions.Left, notoFont);
            gameUI.turnPromptText.text = "주사위 결과 2: 정확히 <b>2장</b>을 반드시 버리세요.";

            // A4. Card Sockets & Hand: five columns, two rows when hand is dense.
            var socketArea = CreateBox("CardSocketsArea", mainArea, new Vector2(0.02f, 0.15f), new Vector2(0.98f, 0.69f), Color.clear);
            var shlg = socketArea.gameObject.AddComponent<GridLayoutGroup>();
            shlg.cellSize = new Vector2(158f, 152f);
            shlg.spacing = new Vector2(8f, 8f);
            shlg.startAxis = GridLayoutGroup.Axis.Horizontal;
            shlg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            shlg.childAlignment = TextAnchor.UpperCenter;
            shlg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            shlg.constraintCount = 5;
            gameUI.cardSocketsContainer = socketArea;

            // Create 10 physical Card Sockets
            for (int i = 0; i < 10; i++)
            {
                var socket = CreateBox($"CardSocket_{i}", socketArea, Vector2.zero, Vector2.one, new Color(0.04f, 0.09f, 0.15f, 0.75f));
                socket.sizeDelta = new Vector2(140f, 136f);
                var sokOutl = socket.gameObject.AddComponent<Outline>();
                sokOutl.effectColor = new Color(0.16f, 0.28f, 0.40f, 0.8f);
                sokOutl.effectDistance = new Vector2(1.5f, -1.5f);

                var idxTxt = CreateText("IndexText", socket, Vector2.zero, Vector2.one, 22, new Color(0.12f, 0.22f, 0.32f, 0.8f), TextAlignmentOptions.Center, notoFont, FontStyles.Bold);
                idxTxt.text = $"{i + 1}";
            }

            // A5. Controls Line (Y 0.03f to 0.12f)
            var controlsLine = CreateBox("ControlsLine", mainArea, new Vector2(0.02f, 0.03f), new Vector2(0.98f, 0.12f), new Color(0.03f, 0.07f, 0.13f, 0.95f));
            var clOutl = controlsLine.gameObject.AddComponent<Outline>();
            clOutl.effectColor = new Color(0.2f, 0.35f, 0.5f, 0.5f);

            gameUI.selectionCountText = CreateText("SelectionCountText", controlsLine, new Vector2(0.05f, 0.1f), new Vector2(0.50f, 0.9f), 17, Color.white, TextAlignmentOptions.Left, notoFont, FontStyles.Bold);
            gameUI.selectionCountText.text = "0장 선택 / 2장 필수";

            gameUI.discardConfirmButton = CreateButton("DiscardConfirmButton", controlsLine, new Vector2(0.65f, 0.15f), new Vector2(0.96f, 0.85f), "선택한 카드 버리기", new Color(0.72f, 0.52f, 0.15f), notoFont);

            // ==========================================
            // B. PUBLIC GRAVE AREA (bottom strip, horizontally scrollable)
            // ==========================================
            var graveArea = CreateBox("PublicGraveArea", panelGo.transform, new Vector2(0.01f, 0.09f), new Vector2(0.99f, 0.38f), new Color(0.04f, 0.08f, 0.14f, 0.95f));
            var gOutl = graveArea.gameObject.AddComponent<Outline>();
            gOutl.effectColor = new Color(0.70f, 0.55f, 0.25f, 0.6f);

            // Header
            var gHeader = CreateBox("GraveHeader", graveArea, new Vector2(0.03f, 0.92f), new Vector2(0.97f, 0.98f), Color.clear);
            var gTitle = CreateText("Title", gHeader, new Vector2(0f, 0.4f), new Vector2(1f, 1f), 17, new Color(1f, 0.90f, 0.55f), TextAlignmentOptions.Left, notoFont, FontStyles.Bold);
            gTitle.text = "공개 무덤";
            var gSub = CreateText("Subtitle", gHeader, new Vector2(0f, 0f), new Vector2(1f, 0.4f), 11, new Color(0.65f, 0.78f, 0.90f), TextAlignmentOptions.Left, notoFont);
            gSub.text = "카드 더미에 마우스를 올리면 펼쳐집니다 · 승리조건은 카드에서 확인";

            // Scroll container
            var scrollBox = CreateBox("GraveScroll", graveArea, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.91f), new Color(0.02f, 0.05f, 0.09f, 0.6f));
            scrollBox.gameObject.AddComponent<RectMask2D>();
            var scrollRect = scrollBox.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.viewport = scrollBox;
            scrollBox.gameObject.AddComponent<PublicGraveScrollWheel>();
            var graveContent = CreateBox("GraveListContainer", scrollBox, new Vector2(0f, 0f), new Vector2(0f, 1f), Color.clear);
            graveContent.pivot = new Vector2(0f, 0.5f);
            var vlg = graveContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            vlg.spacing = 14;
            vlg.padding = new RectOffset(12, 12, 8, 8);
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            var graveFitter = graveContent.gameObject.AddComponent<ContentSizeFitter>();
            graveFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            graveFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            scrollRect.content = graveContent;
            gameUI.graveListContainer = graveContent;

            // Preview Grave Rows for all 5 players
            string[] pNames = new string[] { "방장 (나)", "봇 2", "봇 3", "봇 4", "봇 5" };
            for (int pIdx = 0; pIdx < 5; pIdx++)
            {
                var row = CreateBox($"GraveRow_{pNames[pIdx]}", graveContent, Vector2.zero, Vector2.one, new Color(0.05f, 0.11f, 0.19f, 0.75f));
                var le = row.gameObject.AddComponent<LayoutElement>();
                le.minWidth = 610f;
                le.preferredWidth = 610f;
                le.minHeight = 215f;
                le.preferredHeight = 215f;

                var rName = CreateText("PlayerName", row, new Vector2(0.03f, 0.78f), new Vector2(0.97f, 0.98f), 14, new Color(1f, 0.88f, 0.60f), TextAlignmentOptions.Left, notoFont, FontStyles.Bold);
                rName.text = $"<b>{pNames[pIdx]}</b>";

                var piles = CreateBox("PilesContainer", row, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.75f), Color.clear);
                var ph = piles.gameObject.AddComponent<HorizontalLayoutGroup>();
                ph.spacing = 25f;
                ph.childAlignment = TextAnchor.MiddleLeft;
                ph.childControlWidth = false;
                ph.childControlHeight = false;

                // Preview 1R, 2R hoverable piles
                for (int r = 1; r <= 2; r++)
                {
                    var pile = CreateBox($"Pile_{r}R", piles, Vector2.zero, Vector2.one, new Color(0.08f, 0.16f, 0.25f, 0.4f));
                    pile.sizeDelta = new Vector2(100f, 125f);
                    pile.gameObject.AddComponent<DiscardPileHover>();

                    // Badge
                    var b = CreateBox("Badge", pile, new Vector2(0f, 0.8f), new Vector2(0.45f, 1f), new Color(0.18f, 0.40f, 0.60f, 0.9f));
                    var bt = CreateText("Text", b, Vector2.zero, Vector2.one, 11, Color.white, TextAlignmentOptions.Center, notoFont, FontStyles.Bold);
                    bt.text = $"{r}R";

                    // 2 stacked preview cards
                    for (int c = 0; c < 2; c++)
                    {
                        var mc = CreateBox($"MiniCard_{c}", pile, Vector2.zero, Vector2.one, new Color(0.12f, 0.22f, 0.35f, 1f));
                        mc.sizeDelta = new Vector2(100f, 125f);
                        mc.gameObject.AddComponent<Outline>().effectColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);
                    }
                }
            }

            // ==========================================
            // C. MODAL PANELS (Action, Discussion, Results)
            // ==========================================
            // C1. ActionPanel
            var actBox = CreateBox("ActionPanel", panelGo.transform, new Vector2(0.20f, 0.22f), new Vector2(0.80f, 0.68f), new Color(0.05f, 0.12f, 0.22f, 0.98f));
            actBox.gameObject.AddComponent<Outline>().effectColor = new Color(0.85f, 0.65f, 0.2f);
            gameUI.actionTitleText = CreateText("ActionTitleText", actBox, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f), 17, new Color(1f, 0.9f, 0.5f), TextAlignmentOptions.Center, notoFont, FontStyles.Bold);
            gameUI.actionTitleText.text = "★ 직업 능력 발동 단계";
            gameUI.targetDropdown1 = CreateDropdown("TargetDropdown", actBox, new Vector2(0.10f, 0.48f), new Vector2(0.48f, 0.68f), notoFont);
            gameUI.roleDropdown1 = CreateDropdown("RoleDropdown", actBox, new Vector2(0.52f, 0.48f), new Vector2(0.90f, 0.68f), notoFont);
            gameUI.actionConfirmButton = CreateButton("ActionConfirmButton", actBox, new Vector2(0.30f, 0.15f), new Vector2(0.70f, 0.38f), "능력 발동하기", new Color(0.65f, 0.20f, 0.20f), notoFont);
            gameUI.actionPanel = actBox.gameObject;
            actBox.gameObject.SetActive(false);

            // C2. DiscussionPanel
            // Chat remains on the right while hand and bottom grave stay visible.
            var discBox = CreateBox("DiscussionPanel", panelGo.transform, new Vector2(0.75f, 0.40f), new Vector2(0.99f, 0.98f), new Color(0.03f, 0.08f, 0.15f, 0.98f));
            discBox.gameObject.AddComponent<Outline>().effectColor = new Color(0.3f, 0.7f, 0.9f);
            gameUI.discussionTimerText = CreateText("DiscTimerText", discBox, new Vector2(0.06f, 0.88f), new Vector2(0.58f, 0.97f), 16, new Color(1f, 0.9f, 0.4f), TextAlignmentOptions.Left, notoFont, FontStyles.Bold);
            gameUI.discussionTimerText.text = "토론 시간 (60초)";
            gameUI.skipVotesText = CreateText("SkipVotesText", discBox, new Vector2(0.06f, 0.81f), new Vector2(0.94f, 0.87f), 13, Color.white, TextAlignmentOptions.Left, notoFont);
            gameUI.skipVotesText.text = "토론 종료 동의: 0 / 5";
            gameUI.skipDiscussionButton = CreateButton("SkipButton", discBox, new Vector2(0.06f, 0.73f), new Vector2(0.94f, 0.80f), "토론 종료 동의", new Color(0.2f, 0.4f, 0.6f), notoFont);

            var chatBox = CreateBox("ChatBox", discBox, new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.70f), new Color(0.01f, 0.04f, 0.08f, 0.9f));
            gameUI.chatLogText = CreateText("ChatLogText", chatBox, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f), 13, Color.white, TextAlignmentOptions.BottomLeft, notoFont);
            gameUI.chatLogText.text = "토론 채팅 내용이 여기에 표시됩니다.";

            gameUI.chatInputField = CreateInputField("ChatInput", discBox, new Vector2(0.06f, 0.07f), new Vector2(0.67f, 0.15f), "의견을 입력하세요...", notoFont);
            gameUI.chatSendButton = CreateButton("SendChatButton", discBox, new Vector2(0.71f, 0.07f), new Vector2(0.94f, 0.15f), "전송", new Color(0.18f, 0.45f, 0.65f), notoFont);
            gameUI.discussionPanel = discBox.gameObject;
            discBox.gameObject.SetActive(false);

            // C3. ResultsPanel
            var resBox = CreateBox("ResultsPanel", panelGo.transform, new Vector2(0.20f, 0.15f), new Vector2(0.80f, 0.85f), new Color(0.04f, 0.08f, 0.16f, 0.99f));
            resBox.gameObject.AddComponent<Outline>().effectColor = new Color(0.9f, 0.75f, 0.3f);
            gameUI.resultsRankingsText = CreateText("RankingsText", resBox, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.92f), 15, Color.white, TextAlignmentOptions.TopLeft, notoFont);
            gameUI.resultsRankingsText.text = "<b>🏆 게임 종료 · 최종 결산</b>\n\n결과 집계 중...";
            gameUI.returnLobbyButton = CreateButton("ReturnLobbyButton", resBox, new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.16f), "대기실로 복귀", new Color(0.7f, 0.5f, 0.15f), notoFont);
            gameUI.resultsPanel = resBox.gameObject;
            resBox.gameObject.SetActive(false);

            // Connect GamePlayPanel and Waiting Buttons to RealmLobbyController
            var lobby = canvas.GetComponent<RealmLobbyController>();
            if (lobby != null)
            {
                var lso = new SerializedObject(lobby);
                var gprop = lso.FindProperty("gamePlayPanel");
                if (gprop != null) gprop.objectReferenceValue = panelGo;

                var ctrlPanel = canvas.transform.Find("RoomWaitingPanel/WaitingControlPanel");
                if (ctrlPanel != null)
                {
                    var startBtn = ctrlPanel.Find("StartGameButton")?.GetComponent<Button>();
                    var sProp = lso.FindProperty("startGameButton");
                    if (startBtn != null && sProp != null) sProp.objectReferenceValue = startBtn;

                    var botBtn = ctrlPanel.Find("AddBotButton")?.GetComponent<Button>();
                    var bProp = lso.FindProperty("addBotButton");
                    if (botBtn != null && bProp != null) bProp.objectReferenceValue = botBtn;
                }
                lso.ApplyModifiedProperties();
            }

            // Apply Noto Sans to all texts in the canvas
            foreach (var t in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                t.font = notoFont;
            }

            panelGo.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Complete Web-Style Game Screen (Left: Main/Sockets, Right: Grave with Hover-Deck) built and Noto Sans font applied!");
        }

        private static RectTransform CreateBox(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return rt;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 min, Vector2 max, int fontSize, Color color, TextAlignmentOptions align, TMP_FontAsset font, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<TextMeshProUGUI>();
            t.font = font;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            return t;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 min, Vector2 max, string label, Color color, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;

            var t = CreateText("Label", rt, Vector2.zero, Vector2.one, 15, Color.white, TextAlignmentOptions.Center, font, FontStyles.Bold);
            t.text = label;

            return go.GetComponent<Button>();
        }

        private static TMP_Dropdown CreateDropdown(string name, Transform parent, Vector2 min, Vector2 max, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return BuildDropdown(go, font);
        }

        // A TMP_Dropdown is inert without its Template/Item hierarchy: with only a
        // caption it never opens, so every selection silently stays at index 0.
        // Public so scene tooling can rebuild an existing dropdown in place.
        public static TMP_Dropdown BuildDropdown(GameObject go, TMP_FontAsset font)
        {
            var rt = go.GetComponent<RectTransform>();
            var bg = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            bg.sprite = null;
            bg.color = new Color(0.020f, 0.059f, 0.122f, 0.98f);

            var outline = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
            outline.effectColor = new Color(0.525f, 0.651f, 0.753f, 0.70f);
            outline.effectDistance = new Vector2(1f, -1f);

            // Rebuild in place: drop any previous hierarchy and component so the
            // helper is safe to re-run on a dropdown that already exists.
            for (int i = rt.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(rt.GetChild(i).gameObject);
            var legacy = go.GetComponent<Dropdown>();
            if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy);
            var existing = go.GetComponent<TMP_Dropdown>();
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

            var label = MakeLabel("Label", rt, font, 19, new Color(1f, 1f, 1f));
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 1f);
            label.rectTransform.offsetMin = new Vector2(14f, 4f);
            label.rectTransform.offsetMax = new Vector2(-34f, -4f);

            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            arrow.transform.SetParent(rt, false);
            var art = arrow.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(1f, 0.5f);
            art.anchorMax = new Vector2(1f, 0.5f);
            art.pivot = new Vector2(1f, 0.5f);
            art.sizeDelta = new Vector2(16f, 10f);
            art.anchoredPosition = new Vector2(-12f, 0f);
            var ai = arrow.GetComponent<Image>();
            ai.color = new Color(1f, 0.878f, 0.545f, 0.9f);
            ai.raycastTarget = false;

            var template = new GameObject("Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(rt, false);
            var trt = template.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 0f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0f, 2f);
            trt.sizeDelta = new Vector2(0f, 190f);
            var ti = template.GetComponent<Image>();
            ti.sprite = null;
            ti.color = new Color(0.020f, 0.059f, 0.122f, 0.99f);
            var tOutline = template.AddComponent<Outline>();
            tOutline.effectColor = new Color(0.525f, 0.651f, 0.753f, 0.70f);
            tOutline.effectDistance = new Vector2(1f, -1f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(trt, false);
            var vrt = viewport.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.pivot = new Vector2(0f, 1f);
            vrt.sizeDelta = Vector2.zero;
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            viewport.GetComponent<Image>().color = Color.white;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(vrt, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(0f, 38f);

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(crt, false);
            var irt = item.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(1f, 0.5f);
            irt.sizeDelta = new Vector2(0f, 38f);

            var itemBg = new GameObject("Item Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            itemBg.transform.SetParent(irt, false);
            var ibrt = itemBg.GetComponent<RectTransform>();
            ibrt.anchorMin = Vector2.zero;
            ibrt.anchorMax = Vector2.one;
            ibrt.sizeDelta = Vector2.zero;
            itemBg.GetComponent<Image>().color = new Color(0.063f, 0.188f, 0.310f, 1f);

            var itemLabel = MakeLabel("Item Label", irt, font, 18, new Color(0.929f, 0.965f, 1f));
            itemLabel.rectTransform.anchorMin = Vector2.zero;
            itemLabel.rectTransform.anchorMax = Vector2.one;
            itemLabel.rectTransform.offsetMin = new Vector2(14f, 2f);
            itemLabel.rectTransform.offsetMax = new Vector2(-10f, -2f);

            // Without a marker every row looks identical and the current
            // selection is invisible once the list is open.
            var check = new GameObject("Item Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            check.transform.SetParent(irt, false);
            var chrt = check.GetComponent<RectTransform>();
            chrt.anchorMin = new Vector2(1f, 0.5f);
            chrt.anchorMax = new Vector2(1f, 0.5f);
            chrt.pivot = new Vector2(1f, 0.5f);
            chrt.sizeDelta = new Vector2(10f, 10f);
            chrt.anchoredPosition = new Vector2(-14f, 0f);
            var chi = check.GetComponent<Image>();
            chi.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/realm-pip-circle.png");
            chi.preserveAspect = true;
            chi.color = new Color(1f, 0.878f, 0.545f, 1f);
            chi.raycastTarget = false;

            var toggle = item.GetComponent<Toggle>();
            toggle.graphic = chi;
            toggle.targetGraphic = itemBg.GetComponent<Image>();
            toggle.isOn = true;
            var colors = toggle.colors;
            colors.normalColor = new Color(0f, 0f, 0f, 0f);
            colors.highlightedColor = new Color(0.125f, 0.353f, 0.553f, 1f);
            colors.selectedColor = new Color(0.125f, 0.353f, 0.553f, 1f);
            colors.pressedColor = new Color(0.165f, 0.427f, 0.639f, 1f);
            toggle.colors = colors;

            var scroll = template.GetComponent<ScrollRect>();
            scroll.content = crt;
            scroll.viewport = vrt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            var dd = go.AddComponent<TMP_Dropdown>();
            dd.targetGraphic = bg;
            dd.template = trt;
            dd.captionText = label;
            dd.itemText = itemLabel;
            template.SetActive(false);
            return dd;
        }

        private static TextMeshProUGUI MakeLabel(string name, Transform parent, TMP_FontAsset font, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.font = font;
            if (font != null) t.fontSharedMaterial = font.material;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Left;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.raycastTarget = false;
            return t;
        }

        // Legacy Dropdown and InputField can only drive a legacy Text, so their
        // inner labels stay on the TTF while everything else uses the SDF asset.
        private static Text CreateLegacyText(string name, Transform parent, Vector2 min, Vector2 max, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.font = Resources.Load<Font>("NotoSansKR") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAnchor.MiddleLeft;
            t.supportRichText = true;
            return t;
        }

        private static InputField CreateInputField(string name, Transform parent, Vector2 min, Vector2 max, string placeholder, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.06f, 0.12f, 0.20f);

            var text = CreateLegacyText("Text", rt, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), 13, Color.white);
            var ph = CreateLegacyText("Placeholder", rt, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), 13, new Color(0.5f, 0.6f, 0.7f));
            ph.text = placeholder;

            var input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = ph;
            return input;
        }
    }
}
