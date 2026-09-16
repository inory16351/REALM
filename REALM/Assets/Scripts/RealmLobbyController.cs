using System;
using UnityEngine;
using UnityEngine.UI;

namespace Realm
{
    public sealed class RealmLobbyController : MonoBehaviour
    {
        [Header("Lobby Scene References")]
        [SerializeField] private InputField hostNameInput;
        [SerializeField] private InputField roomCodeInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Text statusText;

        [Header("Waiting Room Scene References")]
        [SerializeField] private GameObject lobbyContent;
        [SerializeField] private GameObject roomWaitingPanel;
        [SerializeField] private Text waitingRoomCode;
        [SerializeField] private Text rosterTitle;
        [SerializeField] private Text hostRosterName;

        [SerializeField] private Button addBotButton;
        [SerializeField] private Button startGameButton;

        [Header("Game Play Panel Reference")]
        [SerializeField] private GameObject gamePlayPanel;

        private void Awake()
        {
            createRoomButton.onClick.AddListener(CreateRoom);
            joinRoomButton.onClick.AddListener(JoinRoom);

            if (gamePlayPanel == null)
            {
                var gpTrans = transform.Find("GamePlayPanel") ?? transform.Find("RealmGameUI");
                if (gpTrans != null) gamePlayPanel = gpTrans.gameObject;
            }

            if (gamePlayPanel != null)
            {
                gamePlayPanel.SetActive(false);
            }

            SetupWaitingControlPanel();

            roomWaitingPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            createRoomButton.onClick.RemoveListener(CreateRoom);
            joinRoomButton.onClick.RemoveListener(JoinRoom);
            if (addBotButton != null) addBotButton.onClick.RemoveListener(AddBot);
            if (startGameButton != null) startGameButton.onClick.RemoveListener(StartGame);
            
            if (RealmNetworkManager.Instance != null)
            {
                RealmNetworkManager.Instance.OnStateUpdated -= OnStateUpdated;
            }
        }

        private void SetupWaitingControlPanel()
        {
            var controlPanel = roomWaitingPanel.transform.Find("WaitingControlPanel");
            if (controlPanel == null) return;

            // Remove any unwanted loose objects
            for (int i = controlPanel.childCount - 1; i >= 0; i--)
            {
                var child = controlPanel.GetChild(i);
                if (child.name == "Text" || (child.name.StartsWith("StartGameButton") && child.name != "StartGameButton"))
                {
                    Destroy(child.gameObject);
                }
            }

            // Find original StartGameButton
            var startBtnTrans = controlPanel.Find("StartGameButton") as RectTransform;
            if (startBtnTrans != null)
            {
                startGameButton = startBtnTrans.GetComponent<Button>();
                startBtnTrans.anchorMin = new Vector2(0.52f, 0.12f);
                startBtnTrans.anchorMax = new Vector2(0.90f, 0.27f);
                startBtnTrans.offsetMin = Vector2.zero;
                startBtnTrans.offsetMax = Vector2.zero;

                var label = startBtnTrans.Find("StartGameButtonLabel")?.GetComponent<Text>();
                if (label != null)
                {
                    label.alignment = TextAnchor.MiddleCenter;
                    label.text = "게임 시작";
                }
            }

            // Find or create AddBotButton
            var botBtnTrans = controlPanel.Find("AddBotButton") as RectTransform;
            if (botBtnTrans == null)
            {
                var go = new GameObject("AddBotButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.transform.SetParent(controlPanel, false);
                botBtnTrans = go.GetComponent<RectTransform>();
            }

            addBotButton = botBtnTrans.GetComponent<Button>();
            botBtnTrans.anchorMin = new Vector2(0.10f, 0.12f);
            botBtnTrans.anchorMax = new Vector2(0.48f, 0.27f);
            botBtnTrans.offsetMin = Vector2.zero;
            botBtnTrans.offsetMax = Vector2.zero;

            var botImg = botBtnTrans.GetComponent<Image>();
            if (botImg != null)
            {
                botImg.color = new Color(0.18f, 0.38f, 0.54f, 1f);
            }

            var textTrans = botBtnTrans.Find("AddBotButtonLabel") as RectTransform ?? botBtnTrans.Find("Text") as RectTransform;
            if (textTrans == null)
            {
                var textGo = new GameObject("AddBotButtonLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textGo.transform.SetParent(botBtnTrans, false);
                textTrans = textGo.GetComponent<RectTransform>();
            }
            else
            {
                textTrans.name = "AddBotButtonLabel";
            }

            textTrans.anchorMin = Vector2.zero;
            textTrans.anchorMax = Vector2.one;
            textTrans.offsetMin = Vector2.zero;
            textTrans.offsetMax = Vector2.zero;

            var botText = textTrans.GetComponent<Text>();
            if (botText != null)
            {
                botText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                botText.fontSize = 17;
                botText.alignment = TextAnchor.MiddleCenter;
                botText.color = Color.white;
                botText.text = "봇 추가";
            }

            if (addBotButton != null)
            {
                addBotButton.onClick.RemoveAllListeners();
                addBotButton.onClick.AddListener(AddBot);
            }

            if (startGameButton != null)
            {
                startGameButton.onClick.RemoveAllListeners();
                startGameButton.onClick.AddListener(StartGame);
            }

            var instructions = controlPanel.Find("WaitingInstructions")?.GetComponent<Text>();
            if (instructions != null)
            {
                instructions.text = "5명이 모두 모이면 게임을 시작할 수 있습니다.\n'봇 추가'를 눌러 빈 자리를 봇으로 채울 수 있습니다.";
            }

            var netStatus = controlPanel.Find("WaitingNetworkStatus")?.GetComponent<Text>();
            if (netStatus != null)
            {
                netStatus.text = "● 온라인 서버 대기 중";
            }
        }

        [Serializable]
        private class SetupPayload
        {
            public string[] selected;
        }

        private void EnsureSetup()
        {
            var defaultRoles = new string[] { "king", "noble", "assassin", "beggar", "slave" };
            RealmNetworkManager.Instance.SendMessagePayload("SETUP", new SetupPayload { selected = defaultRoles });
        }

        private void AddBot()
        {
            Debug.Log("[RealmLobbyController] AddBot clicked");
            EnsureSetup();
            RealmNetworkManager.Instance.SendMessagePayload("ADD_BOT");
        }

        private void StartGame()
        {
            Debug.Log("[RealmLobbyController] StartGame clicked");
            EnsureSetup();
            RealmNetworkManager.Instance.SendMessagePayload("START");
        }

        private async void CreateRoom()
        {
            if (string.IsNullOrWhiteSpace(hostNameInput.text))
            {
                SetStatus("플레이어 이름을 입력하세요.");
                hostNameInput.ActivateInputField();
                return;
            }

            SetStatus("방 생성 중...");
            createRoomButton.interactable = false;
            joinRoomButton.interactable = false;
            
            var playerName = hostNameInput.text.Trim();
            var code = await RealmNetworkManager.Instance.CreateRoom(playerName, 5, 60);
            
            createRoomButton.interactable = true;
            joinRoomButton.interactable = true;
            
            if (!string.IsNullOrEmpty(code))
            {
                roomCodeInput.text = code;
                EnterWaitingRoom(code, playerName, true);
                RealmNetworkManager.Instance.ConnectWebSocket(code);
            }
            else
            {
                SetStatus("방 생성 실패. 다시 시도해 주세요.");
            }
        }

        private async void JoinRoom()
        {
            var roomCode = roomCodeInput.text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(roomCode))
            {
                SetStatus("방 코드를 입력하세요.");
                roomCodeInput.ActivateInputField();
                return;
            }

            var playerName = string.IsNullOrWhiteSpace(hostNameInput.text) ? "참가자" : hostNameInput.text.Trim();
            
            SetStatus("방 참가 중...");
            createRoomButton.interactable = false;
            joinRoomButton.interactable = false;

            var success = await RealmNetworkManager.Instance.JoinRoom(playerName, roomCode);
            
            createRoomButton.interactable = true;
            joinRoomButton.interactable = true;

            if (success)
            {
                EnterWaitingRoom(roomCode, playerName, false);
                RealmNetworkManager.Instance.ConnectWebSocket(roomCode);
            }
            else
            {
                SetStatus("방 참가 실패. 코드를 확인하세요.");
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        private void EnterWaitingRoom(string roomCode, string playerName, bool isHost)
        {
            waitingRoomCode.text = $"방 코드 · {roomCode}";
            rosterTitle.text = isHost ? "참가자 · 1 / 5" : "참가자 · 참가 요청";
            hostRosterName.text = isHost ? $"방장 · {playerName}" : $"참가자 · {playerName}";
            lobbyContent.SetActive(false);
            roomWaitingPanel.SetActive(true);
            if (gamePlayPanel != null) gamePlayPanel.SetActive(false);

            SetStatus("서버 연결 중...");
            
            // Setup default roles early
            if (isHost) EnsureSetup();

            RealmNetworkManager.Instance.OnStateUpdated -= OnStateUpdated;
            RealmNetworkManager.Instance.OnStateUpdated += OnStateUpdated;
        }

        private void OnStateUpdated(GameState state)
        {
            if (state == null) return;

            // Transition to Game Play Panel if game started
            if (state.phase == "round" || state.phase == "actions" || state.phase == "results" || state.phase == "discussion")
            {
                lobbyContent.SetActive(false);
                roomWaitingPanel.SetActive(false);

                var lobbyHeader = transform.Find("LobbyHeaderTemplate")?.gameObject;
                if (lobbyHeader != null) lobbyHeader.SetActive(false);
                var lobbyFooter = transform.Find("LobbyFooter")?.gameObject;
                if (lobbyFooter != null) lobbyFooter.SetActive(false);
                var gallery = transform.Find("CardArtGallery")?.gameObject;
                if (gallery != null) gallery.SetActive(false);

                if (gamePlayPanel != null)
                {
                    gamePlayPanel.SetActive(true);
                    var gui = gamePlayPanel.GetComponent<RealmGameUI>();
                    if (gui != null)
                    {
                        gui.OnStateUpdated(state);
                    }
                }
                return;
            }

            // Update waiting room status
            SetStatus($"대기 중... ({state.players.Length} / {state.playerTarget}명 접속)");
            rosterTitle.text = $"참가자 · {state.players.Length} / {state.playerTarget}";

            var controlPanel = roomWaitingPanel.transform.Find("WaitingControlPanel");
            if (controlPanel != null)
            {
                var netStatus = controlPanel.Find("WaitingNetworkStatus")?.GetComponent<Text>();
                if (netStatus != null)
                {
                    netStatus.text = $"● 온라인 접속 완료 ({state.players.Length}/{state.playerTarget})";
                }
            }

            // Update roster slots
            var rosterPanel = roomWaitingPanel.transform.Find("PlayerRosterPanel");
            if (rosterPanel != null)
            {
                string[] slotNames = new[] { "PlayerSlotTemplate", "PlayerSlot02", "PlayerSlot03", "PlayerSlot04", "PlayerSlot05" };
                for (int i = 0; i < slotNames.Length; i++)
                {
                    var slot = rosterPanel.Find(slotNames[i]);
                    if (slot == null) continue;

                    var texts = slot.GetComponentsInChildren<Text>(true);
                    if (i < state.players.Length)
                    {
                        var p = state.players[i];
                        string rolePrefix = (i == 0) ? "방장" : (p.name.Contains("봇") ? "AI 봇" : "플레이어");
                        if (texts.Length > 0) texts[0].text = $"{rolePrefix} · {p.name}";
                        if (texts.Length > 1) texts[1].text = "준비 완료";
                    }
                    else
                    {
                        if (texts.Length > 0) texts[0].text = $"슬롯 {i + 1} · (비어있음)";
                        if (texts.Length > 1) texts[1].text = "대기 중";
                    }
                }
            }

            // Update button interactability
            if (addBotButton != null)
            {
                addBotButton.interactable = state.host && state.players.Length < state.playerTarget;
            }
            if (startGameButton != null)
            {
                startGameButton.interactable = state.host && state.players.Length >= state.playerTarget;
            }
        }

        public void ReturnToLobby()
        {
            if (gamePlayPanel != null) gamePlayPanel.SetActive(false);
            lobbyContent.SetActive(true);
            roomWaitingPanel.SetActive(false);

            var lobbyHeader = transform.Find("LobbyHeaderTemplate")?.gameObject;
            if (lobbyHeader != null) lobbyHeader.SetActive(true);
            var lobbyFooter = transform.Find("LobbyFooter")?.gameObject;
            if (lobbyFooter != null) lobbyFooter.SetActive(true);

            SetStatus("로비로 돌아왔습니다.");
        }
    }
}