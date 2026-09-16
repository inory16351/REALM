using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

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

            Font notoFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansKR.ttf");
            if (notoFont == null)
            {
                notoFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

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
            // A. MAIN PLAY AREA (Left Panel: X 0.015 to 0.65)
            // ==========================================
            var mainArea = CreateBox("MainPlayArea", panelGo.transform, new Vector2(0.015f, 0.02f), new Vector2(0.65f, 0.98f), new Color(0.05f, 0.10f, 0.17f, 0.85f));
            var maOutl = mainArea.gameObject.AddComponent<Outline>();
            maOutl.effectColor = new Color(0.2f, 0.35f, 0.5f, 0.4f);

            // A1. Top Header (Y 0.88 to 0.98)
            var topHeader = CreateBox("TopHeader", mainArea, new Vector2(0.02f, 0.88f), new Vector2(0.98f, 0.98f), new Color(0.03f, 0.08f, 0.15f, 0.9f));
            gameUI.roundEyebrowText = CreateText("RoundEyebrowText", topHeader, new Vector2(0.02f, 0.55f), new Vector2(0.98f, 0.95f), 13, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleLeft, notoFont);
            gameUI.roundEyebrowText.text = "1라운드 · 공개 버리기";

            gameUI.turnHeadingText = CreateText("TurnHeadingText", topHeader, new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.55f), 20, new Color(1f, 0.90f, 0.45f), TextAnchor.MiddleLeft, notoFont, FontStyle.Bold);
            gameUI.turnHeadingText.text = "당신의 차례입니다.";

            // A2. Players Strip (Y 0.77 to 0.86)
            var pStrip = CreateBox("PlayersStrip", mainArea, new Vector2(0.02f, 0.77f), new Vector2(0.98f, 0.86f), Color.clear);
            var phlg = pStrip.gameObject.AddComponent<HorizontalLayoutGroup>();
            phlg.spacing = 10;
            phlg.childAlignment = TextAnchor.MiddleLeft;
            phlg.childControlWidth = false;
            phlg.childControlHeight = false;
            gameUI.playersStripContainer = pStrip;

            // Preview player pills
            for (int i = 0; i < 5; i++)
            {
                var pill = CreateBox($"PlayerPill_{i}", pStrip, Vector2.zero, Vector2.one, (i == 0) ? new Color(0.18f, 0.45f, 0.65f, 0.95f) : new Color(0.06f, 0.15f, 0.25f, 0.85f));
                pill.sizeDelta = new Vector2(145f, 42f);
                var pol = pill.gameObject.AddComponent<Outline>();
                pol.effectColor = (i == 0) ? new Color(1f, 0.85f, 0.4f) : new Color(0.25f, 0.38f, 0.5f);
                var ptxt = CreateText("Text", pill, Vector2.zero, Vector2.one, 12, Color.white, TextAnchor.MiddleCenter, notoFont);
                ptxt.rectTransform.offsetMin = new Vector2(6f, 2f);
                ptxt.rectTransform.offsetMax = new Vector2(-6f, -2f);
                ptxt.text = (i == 0) ? "<b>방장</b> (나) (10장)" : $"<b>봇 {i + 1}</b> (10장)";
            }

            // A3. Notices Group (Y 0.59 to 0.75)
            var notices = CreateBox("Notices", mainArea, new Vector2(0.02f, 0.59f), new Vector2(0.98f, 0.75f), new Color(0.03f, 0.07f, 0.13f, 0.95f));
            var nOutl = notices.gameObject.AddComponent<Outline>();
            nOutl.effectColor = new Color(0.25f, 0.4f, 0.55f, 0.5f);

            gameUI.secretRoleText = CreateText("SecretRoleText", notices, new Vector2(0.03f, 0.68f), new Vector2(0.97f, 0.96f), 14, Color.white, TextAnchor.MiddleLeft, notoFont);
            gameUI.secretRoleText.text = "당신의 비밀 직업: <color=#FFE08B><b>국왕</b></color> — 전체 무덤의 왕 카드가 9장 이상.";

            gameUI.lastRollText = CreateText("LastRollText", notices, new Vector2(0.03f, 0.36f), new Vector2(0.97f, 0.65f), 13, new Color(0.5f, 0.9f, 0.8f), TextAnchor.MiddleLeft, notoFont);
            gameUI.lastRollText.text = "최근 D6 · <b>방장</b> → <b>2</b>";

            gameUI.turnPromptText = CreateText("TurnPromptText", notices, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.33f), 13, new Color(0.9f, 0.93f, 0.97f), TextAnchor.MiddleLeft, notoFont);
            gameUI.turnPromptText.text = "주사위 결과 2: 정확히 <b>2장</b>을 반드시 버리세요.";

            // A4. Card Sockets & Hand (Y 0.16 to 0.57)
            var socketArea = CreateBox("CardSocketsArea", mainArea, new Vector2(0.02f, 0.16f), new Vector2(0.98f, 0.57f), Color.clear);
            var shlg = socketArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            shlg.spacing = 10;
            shlg.childAlignment = TextAnchor.MiddleCenter;
            shlg.childControlWidth = false;
            shlg.childControlHeight = false;
            gameUI.cardSocketsContainer = socketArea;

            // Create 10 physical Card Sockets
            for (int i = 0; i < 10; i++)
            {
                var socket = CreateBox($"CardSocket_{i}", socketArea, Vector2.zero, Vector2.one, new Color(0.04f, 0.09f, 0.15f, 0.75f));
                socket.sizeDelta = new Vector2(136f, 190f);
                var sokOutl = socket.gameObject.AddComponent<Outline>();
                sokOutl.effectColor = new Color(0.16f, 0.28f, 0.40f, 0.8f);
                sokOutl.effectDistance = new Vector2(1.5f, -1.5f);

                var idxTxt = CreateText("IndexText", socket, Vector2.zero, Vector2.one, 22, new Color(0.12f, 0.22f, 0.32f, 0.8f), TextAnchor.MiddleCenter, notoFont, FontStyle.Bold);
                idxTxt.text = $"{i + 1}";
            }

            // A5. Controls Line (Y 0.02f to 0.14f)
            var controlsLine = CreateBox("ControlsLine", mainArea, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.14f), new Color(0.03f, 0.07f, 0.13f, 0.95f));
            var clOutl = controlsLine.gameObject.AddComponent<Outline>();
            clOutl.effectColor = new Color(0.2f, 0.35f, 0.5f, 0.5f);

            gameUI.selectionCountText = CreateText("SelectionCountText", controlsLine, new Vector2(0.05f, 0.1f), new Vector2(0.50f, 0.9f), 17, Color.white, TextAnchor.MiddleLeft, notoFont, FontStyle.Bold);
            gameUI.selectionCountText.text = "0장 선택 / 2장 필수";

            gameUI.discardConfirmButton = CreateButton("DiscardConfirmButton", controlsLine, new Vector2(0.65f, 0.15f), new Vector2(0.96f, 0.85f), "선택한 카드 버리기", new Color(0.72f, 0.52f, 0.15f), notoFont);

            // ==========================================
            // B. PUBLIC GRAVE AREA (Right Panel: X 0.665 to 0.985)
            // ==========================================
            var graveArea = CreateBox("PublicGraveArea", panelGo.transform, new Vector2(0.665f, 0.02f), new Vector2(0.985f, 0.98f), new Color(0.04f, 0.08f, 0.14f, 0.95f));
            var gOutl = graveArea.gameObject.AddComponent<Outline>();
            gOutl.effectColor = new Color(0.70f, 0.55f, 0.25f, 0.6f);

            // Header
            var gHeader = CreateBox("GraveHeader", graveArea, new Vector2(0.03f, 0.92f), new Vector2(0.97f, 0.98f), Color.clear);
            var gTitle = CreateText("Title", gHeader, new Vector2(0f, 0.4f), new Vector2(1f, 1f), 17, new Color(1f, 0.90f, 0.55f), TextAnchor.MiddleLeft, notoFont, FontStyle.Bold);
            gTitle.text = "공개 무덤 (Public Grave)";
            var gSub = CreateText("Subtitle", gHeader, new Vector2(0f, 0f), new Vector2(1f, 0.4f), 11, new Color(0.65f, 0.78f, 0.90f), TextAnchor.MiddleLeft, notoFont);
            gSub.text = "카드 더미에 마우스를 올리면 카드가 펼쳐집니다.";

            // Scroll container
            var scrollBox = CreateBox("GraveScroll", graveArea, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.91f), new Color(0.02f, 0.05f, 0.09f, 0.6f));
            var vlg = scrollBox.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            gameUI.graveListContainer = scrollBox;

            // Preview Grave Rows for all 5 players
            string[] pNames = new string[] { "방장 (나)", "봇 2", "봇 3", "봇 4", "봇 5" };
            for (int pIdx = 0; pIdx < 5; pIdx++)
            {
                var row = CreateBox($"GraveRow_{pNames[pIdx]}", scrollBox, Vector2.zero, Vector2.one, new Color(0.05f, 0.11f, 0.19f, 0.75f));
                var le = row.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 150f;
                le.preferredHeight = 150f;

                var rName = CreateText("PlayerName", row, new Vector2(0.03f, 0.78f), new Vector2(0.97f, 0.98f), 14, new Color(1f, 0.88f, 0.60f), TextAnchor.MiddleLeft, notoFont, FontStyle.Bold);
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
                    var bt = CreateText("Text", b, Vector2.zero, Vector2.one, 11, Color.white, TextAnchor.MiddleCenter, notoFont, FontStyle.Bold);
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
            gameUI.actionTitleText = CreateText("ActionTitleText", actBox, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f), 17, new Color(1f, 0.9f, 0.5f), TextAnchor.MiddleCenter, notoFont, FontStyle.Bold);
            gameUI.actionTitleText.text = "★ 직업 능력 발동 단계";
            gameUI.targetDropdown1 = CreateDropdown("TargetDropdown", actBox, new Vector2(0.10f, 0.48f), new Vector2(0.48f, 0.68f), notoFont);
            gameUI.roleDropdown1 = CreateDropdown("RoleDropdown", actBox, new Vector2(0.52f, 0.48f), new Vector2(0.90f, 0.68f), notoFont);
            gameUI.actionConfirmButton = CreateButton("ActionConfirmButton", actBox, new Vector2(0.30f, 0.15f), new Vector2(0.70f, 0.38f), "능력 발동하기", new Color(0.65f, 0.20f, 0.20f), notoFont);
            gameUI.actionPanel = actBox.gameObject;
            actBox.gameObject.SetActive(false);

            // C2. DiscussionPanel
            var discBox = CreateBox("DiscussionPanel", panelGo.transform, new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.75f), new Color(0.03f, 0.08f, 0.15f, 0.98f));
            discBox.gameObject.AddComponent<Outline>().effectColor = new Color(0.3f, 0.7f, 0.9f);
            gameUI.discussionTimerText = CreateText("DiscTimerText", discBox, new Vector2(0.05f, 0.88f), new Vector2(0.55f, 0.98f), 16, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleLeft, notoFont, FontStyle.Bold);
            gameUI.discussionTimerText.text = "토론 시간 (60초)";
            gameUI.skipVotesText = CreateText("SkipVotesText", discBox, new Vector2(0.55f, 0.88f), new Vector2(0.75f, 0.98f), 13, Color.white, TextAnchor.MiddleLeft, notoFont);
            gameUI.skipVotesText.text = "토론 종료 동의: 0 / 5";
            gameUI.skipDiscussionButton = CreateButton("SkipButton", discBox, new Vector2(0.76f, 0.87f), new Vector2(0.96f, 0.98f), "토론 종료 동의", new Color(0.2f, 0.4f, 0.6f), notoFont);

            var chatBox = CreateBox("ChatBox", discBox, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.84f), new Color(0.01f, 0.04f, 0.08f, 0.9f));
            gameUI.chatLogText = CreateText("ChatLogText", chatBox, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f), 13, Color.white, TextAnchor.LowerLeft, notoFont);
            gameUI.chatLogText.text = "토론 채팅 내용이 여기에 표시됩니다.";

            gameUI.chatInputField = CreateInputField("ChatInput", discBox, new Vector2(0.05f, 0.05f), new Vector2(0.78f, 0.15f), "의견을 입력하세요...", notoFont);
            gameUI.chatSendButton = CreateButton("SendChatButton", discBox, new Vector2(0.80f, 0.05f), new Vector2(0.95f, 0.15f), "전송", new Color(0.18f, 0.45f, 0.65f), notoFont);
            gameUI.discussionPanel = discBox.gameObject;
            discBox.gameObject.SetActive(false);

            // C3. ResultsPanel
            var resBox = CreateBox("ResultsPanel", panelGo.transform, new Vector2(0.20f, 0.15f), new Vector2(0.80f, 0.85f), new Color(0.04f, 0.08f, 0.16f, 0.99f));
            resBox.gameObject.AddComponent<Outline>().effectColor = new Color(0.9f, 0.75f, 0.3f);
            gameUI.resultsRankingsText = CreateText("RankingsText", resBox, new Vector2(0.08f, 0.20f), new Vector2(0.92f, 0.92f), 15, Color.white, TextAnchor.UpperLeft, notoFont);
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
            foreach (var t in canvas.GetComponentsInChildren<Text>(true))
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

        private static Text CreateText(string name, Transform parent, Vector2 min, Vector2 max, int fontSize, Color color, TextAnchor align, Font font, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            return t;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 min, Vector2 max, string label, Color color, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;

            var t = CreateText("Label", rt, Vector2.zero, Vector2.one, 15, Color.white, TextAnchor.MiddleCenter, font, FontStyle.Bold);
            t.text = label;

            return go.GetComponent<Button>();
        }

        private static Dropdown CreateDropdown(string name, Transform parent, Vector2 min, Vector2 max, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Dropdown));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.1f, 0.2f, 0.3f);

            var label = CreateText("Label", rt, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f), 14, Color.white, TextAnchor.MiddleLeft, font);
            var dd = go.GetComponent<Dropdown>();
            dd.captionText = label;
            return dd;
        }

        private static InputField CreateInputField(string name, Transform parent, Vector2 min, Vector2 max, string placeholder, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.06f, 0.12f, 0.20f);

            var text = CreateText("Text", rt, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), 13, Color.white, TextAnchor.MiddleLeft, font);
            var ph = CreateText("Placeholder", rt, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.95f), 13, new Color(0.5f, 0.6f, 0.7f), TextAnchor.MiddleLeft, font);
            ph.text = placeholder;

            var input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = ph;
            return input;
        }
    }
}
