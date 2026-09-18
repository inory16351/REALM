using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Realm
{
    public sealed class RealmLobbyController : MonoBehaviour
    {
        [Header("Lobby Scene References")]
        [SerializeField] private InputField hostNameInput;
        [SerializeField] private InputField roomCodeInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TMP_Dropdown playerTargetDropdown;

        [Header("Waiting Room Scene References")]
        [SerializeField] private GameObject lobbyContent;
        [SerializeField] private GameObject roomWaitingPanel;
        [SerializeField] private TextMeshProUGUI waitingRoomCode;
        [SerializeField] private TextMeshProUGUI rosterTitle;

        [SerializeField] private Button addBotButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private RealmRoleSetup roleSetup;

        [Header("Game Play Panel Reference")]
        [SerializeField] private GameObject gamePlayPanel;

        private const int MinPlayerTarget = 5;
        private const int MaxPlayerTarget = 10;
        private bool _isHost;
        private bool _setupSent;

        // The dropdown lists 5..10, so its index maps straight onto the count.
        private int SelectedPlayerTarget
        {
            get
            {
                if (playerTargetDropdown == null) return MinPlayerTarget;
                return Mathf.Clamp(playerTargetDropdown.value + MinPlayerTarget, MinPlayerTarget, MaxPlayerTarget);
            }
        }

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

                var label = startBtnTrans.Find("StartGameButtonLabel")?.GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    label.alignment = TextAlignmentOptions.Center;
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
                var textGo = new GameObject("AddBotButtonLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
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

            var botText = textTrans.GetComponent<TextMeshProUGUI>();
            if (botText != null)
            {
                botText.font = RealmCard.GetNotoTmpFont();
                botText.fontSize = 19;
                botText.fontStyle = FontStyles.Bold;
                botText.alignment = TextAlignmentOptions.Center;
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

            var instructions = controlPanel.Find("WaitingInstructions")?.GetComponent<TextMeshProUGUI>();
            if (instructions != null)
            {
                instructions.text = "5명이 모두 모이면 게임을 시작할 수 있습니다.\n'봇 추가'를 눌러 빈 자리를 봇으로 채울 수 있습니다.";
            }

            var netStatus = controlPanel.Find("WaitingNetworkStatus")?.GetComponent<TextMeshProUGUI>();
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

        private string[] _chosenRoles;

        private void EnsureSetup()
        {
            if (_chosenRoles == null || _chosenRoles.Length == 0) return;
            RealmNetworkManager.Instance.SendMessagePayload("SETUP", new SetupPayload { selected = _chosenRoles });
        }

        private void OpenRoleSetup(int playerTarget)
        {
            if (roleSetup == null)
            {
                SetStatus("직업 선택 창을 찾을 수 없습니다.");
                return;
            }
            roleSetup.OnConfirmed -= HandleRolesChosen;
            roleSetup.OnConfirmed += HandleRolesChosen;
            roleSetup.Open(playerTarget, _chosenRoles);
        }

        private void HandleRolesChosen(string[] roles)
        {
            _chosenRoles = roles;
            _setupSent = false;
            SetStatus($"직업 {roles.Length}종을 정했습니다. 참가자를 기다리세요.");
            if (RealmNetworkManager.Instance != null) EnsureSetup();
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
            if (_chosenRoles == null || _chosenRoles.Length == 0)
            {
                SetStatus("먼저 이번 판에 쓸 직업을 고르세요.");
                OpenRoleSetup(SelectedPlayerTarget);
                return;
            }
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
            int target = SelectedPlayerTarget;
            var code = await RealmNetworkManager.Instance.CreateRoom(playerName, target, 60);
            
            createRoomButton.interactable = true;
            joinRoomButton.interactable = true;
            
            if (!string.IsNullOrEmpty(code))
            {
                roomCodeInput.text = code;
                EnterWaitingRoom(code, playerName, true);
                RealmNetworkManager.Instance.ConnectWebSocket(code);
                // The host picks the roles for this room before anyone can start.
                OpenRoleSetup(target);
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
            // Only the header is filled here; the roster itself is built by
            // UpdateRoster as soon as the first server state arrives.
            waitingRoomCode.text = $"방 코드 · {roomCode}";
            rosterTitle.text = isHost ? $"참가자 · 1 / {SelectedPlayerTarget}" : "참가자 · 참가 요청";
            lobbyContent.SetActive(false);
            roomWaitingPanel.SetActive(true);
            if (gamePlayPanel != null) gamePlayPanel.SetActive(false);

            SetStatus("서버 연결 중...");

            // SETUP has to wait for the socket: EnterWaitingRoom runs before
            // ConnectWebSocket, so sending here only produced a "연결이 끊어짐"
            // toast and the roles never reached the server.
            _isHost = isHost;
            _setupSent = false;

            RealmNetworkManager.Instance.OnStateUpdated -= OnStateUpdated;
            RealmNetworkManager.Instance.OnStateUpdated += OnStateUpdated;
        }

        private void OnStateUpdated(GameState state)
        {
            if (state == null) return;

            // The first state proves the socket is open, so this is the earliest
            // point the host can actually deliver its role selection.
            if (_isHost && !_setupSent && (state.selected == null || state.selected.Length == 0))
            {
                _setupSent = true;
                EnsureSetup();
            }

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
                var netStatus = controlPanel.Find("WaitingNetworkStatus")?.GetComponent<TextMeshProUGUI>();
                if (netStatus != null)
                {
                    netStatus.text = $"● 온라인 접속 완료 ({state.players.Length}/{state.playerTarget})";
                }

                var instructions = controlPanel.Find("WaitingInstructions")?.GetComponent<TextMeshProUGUI>();
                if (instructions != null)
                {
                    instructions.text = $"{state.playerTarget}명이 모두 모이면 게임을 시작할 수 있습니다.\n'봇 추가'를 눌러 빈 자리를 봇으로 채울 수 있습니다.";
                }
            }

            UpdateRoster(state);

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

        // The room holds 5–10 players, so the roster clones its slot template to
        // match playerTarget rather than relying on a fixed set of five.
        private void UpdateRoster(GameState state)
        {
            var list = roomWaitingPanel.transform.Find("PlayerRosterPanel/RosterList");
            if (list == null) return;
            var template = list.Find("PlayerSlotTemplate");
            if (template == null) return;

            int needed = Mathf.Clamp(Mathf.Max(state.playerTarget, state.players.Length), 1, 10);

            var slots = new List<Transform>();
            foreach (Transform child in list)
                if (child != template) slots.Add(child);

            while (slots.Count < needed)
            {
                var clone = Instantiate(template.gameObject, list);
                clone.name = $"PlayerSlot{slots.Count + 1:D2}";
                clone.SetActive(true);
                slots.Add(clone.transform);
            }

            for (int i = 0; i < slots.Count; i++)
            {
                bool used = i < needed;
                if (slots[i].gameObject.activeSelf != used) slots[i].gameObject.SetActive(used);
                if (!used) continue;

                var texts = slots[i].GetComponentsInChildren<TextMeshProUGUI>(true);
                bool filled = i < state.players.Length;
                if (texts.Length > 0)
                {
                    if (filled)
                    {
                        var p = state.players[i];
                        string rolePrefix = (i == 0) ? "방장" : (p.name.Contains("봇") ? "AI 봇" : "플레이어");
                        texts[0].text = $"{rolePrefix} · {p.name}";
                    }
                    else texts[0].text = $"슬롯 {i + 1} · 비어있음";
                }
                if (texts.Length > 1) texts[1].text = filled ? "준비 완료" : "대기 중";
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