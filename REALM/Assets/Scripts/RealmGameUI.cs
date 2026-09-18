using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Realm
{
    public class RealmGameUI : MonoBehaviour
    {
        [Header("Card Prefab")]
        [SerializeField] public GameObject cardPrefab;
        [SerializeField] private Sprite uiPanelSprite;

        [Header("Runtime Plate Sprites")]
        [SerializeField] private Sprite publicSlotSprite;
        [SerializeField] private Sprite secretSlotSprite;
        [SerializeField] private Sprite roundBadgeSprite;
        [SerializeField] private Sprite secretBadgeSprite;
        [SerializeField] private Sprite pillIdleSprite;
        [SerializeField] private Sprite pillTurnSprite;
        [SerializeField] private Sprite pillSelfSprite;

        [Header("Main Play Area (Left Panel)")]
        [SerializeField] public TextMeshProUGUI roundEyebrowText;
        [SerializeField] public TextMeshProUGUI turnHeadingText;
        [SerializeField] public Transform playersStripContainer;
        [SerializeField] public TextMeshProUGUI secretRoleText;
        [SerializeField] public TextMeshProUGUI lastRollText;
        [SerializeField] public TextMeshProUGUI turnPromptText;

        [Header("Card Sockets & Hand (10 Slots)")]
        [SerializeField] public Transform cardSocketsContainer;
        [SerializeField] public TextMeshProUGUI selectionCountText;
        [SerializeField] public Button discardConfirmButton;

        [Header("Public Grave Modal")]
        [SerializeField] public Transform graveListContainer;
        [SerializeField] public GameObject graveModal;
        [SerializeField] public Button openGraveButton;
        [SerializeField] public Button closeGraveButton;

        [Header("Action Phase Modal")]
        [SerializeField] public GameObject actionPanel;
        [SerializeField] public TextMeshProUGUI actionTitleText;
        [SerializeField] public TMP_Dropdown targetDropdown1;
        [SerializeField] public TMP_Dropdown roleDropdown1;
        [SerializeField] public Button actionConfirmButton;

        [Header("Discussion Phase Modal")]
        [SerializeField] public GameObject discussionPanel;
        [SerializeField] public TextMeshProUGUI discussionTimerText;
        [SerializeField] public TextMeshProUGUI skipVotesText;
        [SerializeField] public Button skipDiscussionButton;
        [SerializeField] public TextMeshProUGUI chatLogText;
        [SerializeField] public InputField chatInputField;
        [SerializeField] public Button chatSendButton;

        [Header("Dice Stage")]
        [SerializeField] public RealmDiceRoll diceRoll;

        [Header("In-Game Warning Toast")]
        [SerializeField] public GameObject warningToast;
        [SerializeField] public TextMeshProUGUI warningToastText;

        [Header("Results Phase Modal")]
        [SerializeField] public GameObject resultsPanel;
        [SerializeField] public TextMeshProUGUI resultsRankingsText;
        [SerializeField] public Button returnLobbyButton;

        private readonly List<RealmCard> _spawnedCards = new List<RealmCard>();
        private readonly List<string> _chosenCardIds = new List<string>();
        private readonly List<Transform> _socketSlots = new List<Transform>();
        private const float CardWidth = 182f;
        private const float CardHeight = 254.2222f;
        private const int GraveSlotCount = 4;
        private const float GraveSlotSpacing = 12f;
        private const float GraveContentPadding = 38f;
        private const float GraveCardScale = 0.78f;
        private const float GraveSlotHeight = 206f;
        private const float GraveRowHeight = 292f;
        private const float GraveBadgeHeight = 26f;
        private GameState _latestState;
        private CanvasGroup _warningCanvasGroup;
        private float _warningHideAt = -1f;
        private bool _diceWasRolling;

        private void Awake()
        {
            AutoWireReferences();

            if (discardConfirmButton != null) discardConfirmButton.onClick.AddListener(OnDiscardConfirmClicked);
            if (actionConfirmButton != null) actionConfirmButton.onClick.AddListener(OnActionConfirmClicked);
            if (skipDiscussionButton != null) skipDiscussionButton.onClick.AddListener(OnSkipDiscussionClicked);
            if (chatSendButton != null) chatSendButton.onClick.AddListener(OnSendChatClicked);
            if (returnLobbyButton != null) returnLobbyButton.onClick.AddListener(OnReturnLobbyClicked);
            if (openGraveButton != null) openGraveButton.onClick.AddListener(OnOpenGraveClicked);
            if (closeGraveButton != null) closeGraveButton.onClick.AddListener(OnCloseGraveClicked);

            CacheSockets();
            if (warningToast != null)
            {
                _warningCanvasGroup = warningToast.GetComponent<CanvasGroup>();
                warningToast.SetActive(false);
            }
        }

        private void Start()
        {
            if (RealmNetworkManager.Instance != null)
            {
                RealmNetworkManager.Instance.OnStateUpdated += OnStateUpdated;
                RealmNetworkManager.Instance.OnServerError += ShowWarning;
            }
        }

        private void OnDestroy()
        {
            if (RealmNetworkManager.Instance != null)
            {
                RealmNetworkManager.Instance.OnStateUpdated -= OnStateUpdated;
                RealmNetworkManager.Instance.OnServerError -= ShowWarning;
            }
        }

        private void Update()
        {
            // The dice finishes on a timer, not on a server message, so the
            // discard controls have to be re-enabled from here.
            if (diceRoll != null)
            {
                if (diceRoll.IsRolling) _diceWasRolling = true;
                else if (_diceWasRolling)
                {
                    _diceWasRolling = false;
                    UpdateDiscardButtonStatus();
                }
            }

            if (_warningCanvasGroup == null || warningToast == null || !warningToast.activeSelf) return;

            float remaining = _warningHideAt - Time.unscaledTime;
            if (remaining <= 0f)
            {
                warningToast.SetActive(false);
                return;
            }

            // 0.18s fade-in and 0.42s fade-out without a runtime-created object.
            _warningCanvasGroup.alpha = remaining > 3.1f
                ? Mathf.Clamp01((3.35f - remaining) / 0.18f)
                : Mathf.Clamp01(remaining / 0.42f);
        }

        public void ShowWarning(string message)
        {
            if (warningToast == null || warningToastText == null) return;
            warningToastText.text = string.IsNullOrWhiteSpace(message) ? "요청을 처리할 수 없습니다." : message;
            warningToast.SetActive(true);
            if (_warningCanvasGroup == null) _warningCanvasGroup = warningToast.GetComponent<CanvasGroup>();
            if (_warningCanvasGroup != null) _warningCanvasGroup.alpha = 0f;
            _warningHideAt = Time.unscaledTime + 3.35f;
        }

        private void CacheSockets()
        {
            _socketSlots.Clear();
            if (cardSocketsContainer != null)
            {
                for (int i = 0; i < cardSocketsContainer.childCount; i++)
                {
                    _socketSlots.Add(cardSocketsContainer.GetChild(i));
                }
            }
        }

        public void AutoWireReferences()
        {
            var font = RealmCard.GetNotoTmpFont();

            if (roundEyebrowText == null) roundEyebrowText = transform.Find("MainPlayArea/TopHeader/RoundEyebrowText")?.GetComponent<TextMeshProUGUI>();
            if (turnHeadingText == null) turnHeadingText = transform.Find("MainPlayArea/TopHeader/TurnHeadingText")?.GetComponent<TextMeshProUGUI>();
            if (playersStripContainer == null) playersStripContainer = transform.Find("MainPlayArea/PlayersStrip");

            if (secretRoleText == null) secretRoleText = transform.Find("MainPlayArea/Notices/SecretRoleText")?.GetComponent<TextMeshProUGUI>();
            if (lastRollText == null) lastRollText = transform.Find("MainPlayArea/Notices/LastRollText")?.GetComponent<TextMeshProUGUI>();
            if (turnPromptText == null) turnPromptText = transform.Find("MainPlayArea/Notices/TurnPromptText")?.GetComponent<TextMeshProUGUI>();

            if (cardSocketsContainer == null) cardSocketsContainer = transform.Find("MainPlayArea/CardSocketsArea");
            if (selectionCountText == null) selectionCountText = transform.Find("MainPlayArea/ControlsLine/SelectionCountText")?.GetComponent<TextMeshProUGUI>();
            if (discardConfirmButton == null) discardConfirmButton = transform.Find("MainPlayArea/ControlsLine/DiscardConfirmButton")?.GetComponent<Button>();

            // Prefer the MCP-authored horizontal content strip. Older scene
            // versions serialized the viewport itself, so resolve this every
            // time rather than retaining that legacy reference.
            var authoredGraveContent = transform.Find("PublicGraveArea/GraveScroll/GraveListContainer");
            graveListContainer = authoredGraveContent ?? graveListContainer ?? transform.Find("PublicGraveArea/GraveScroll");

            if (graveModal == null) graveModal = transform.Find("PublicGraveArea")?.gameObject;
            if (openGraveButton == null) openGraveButton = transform.Find("MainPlayArea/TopHeader/OpenGraveButton")?.GetComponent<Button>();
            if (closeGraveButton == null) closeGraveButton = transform.Find("PublicGraveArea/CloseGraveButton")?.GetComponent<Button>();

            if (actionPanel == null) actionPanel = transform.Find("ActionPanel")?.gameObject;
            if (actionPanel != null)
            {
                if (actionTitleText == null) actionTitleText = actionPanel.transform.Find("ActionTitleText")?.GetComponent<TextMeshProUGUI>();
                if (targetDropdown1 == null) targetDropdown1 = actionPanel.transform.Find("TargetDropdown")?.GetComponent<TMP_Dropdown>();
                if (roleDropdown1 == null) roleDropdown1 = actionPanel.transform.Find("RoleDropdown")?.GetComponent<TMP_Dropdown>();
                if (actionConfirmButton == null) actionConfirmButton = actionPanel.transform.Find("ActionConfirmButton")?.GetComponent<Button>();
            }

            if (discussionPanel == null) discussionPanel = transform.Find("DiscussionPanel")?.gameObject;
            if (discussionPanel != null)
            {
                if (discussionTimerText == null) discussionTimerText = discussionPanel.transform.Find("DiscTimerText")?.GetComponent<TextMeshProUGUI>();
                if (skipVotesText == null) skipVotesText = discussionPanel.transform.Find("SkipVotesText")?.GetComponent<TextMeshProUGUI>();
                if (skipDiscussionButton == null) skipDiscussionButton = discussionPanel.transform.Find("SkipButton")?.GetComponent<Button>();
                if (chatLogText == null) chatLogText = discussionPanel.transform.Find("ChatBox/ChatLogText")?.GetComponent<TextMeshProUGUI>();
                if (chatInputField == null) chatInputField = discussionPanel.transform.Find("ChatInput")?.GetComponent<InputField>();
                if (chatSendButton == null) chatSendButton = discussionPanel.transform.Find("SendChatButton")?.GetComponent<Button>();
            }

            if (diceRoll == null) diceRoll = GetComponentInChildren<RealmDiceRoll>(true);

            if (warningToast == null) warningToast = transform.Find("WarningToast")?.gameObject;
            if (warningToast != null && warningToastText == null)
                warningToastText = warningToast.transform.Find("WarningText")?.GetComponent<TextMeshProUGUI>();

            if (resultsPanel == null) resultsPanel = transform.Find("ResultsPanel")?.gameObject;
            if (resultsPanel != null)
            {
                if (resultsRankingsText == null) resultsRankingsText = resultsPanel.transform.Find("RankingsText")?.GetComponent<TextMeshProUGUI>();
                if (returnLobbyButton == null) returnLobbyButton = resultsPanel.transform.Find("ReturnLobbyButton")?.GetComponent<Button>();
            }

            // Apply Noto font to all texts
            foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                t.font = font;
            }
        }

        public void OnStateUpdated(GameState state)
        {
            if (state == null) return;
            _latestState = state;

            if (state.phase == "lobby")
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            UpdateDiceStage(state);
            UpdateHeaderAndNotices(state);
            UpdatePlayersStrip(state);
            UpdateHandAndSockets(state);
            // The grave is a modal now; rebuilding it while hidden would lay out
            // against a zero-sized viewport, so it refreshes on open instead.
            if (graveModal != null && graveModal.activeSelf) UpdatePublicGrave(state);
            UpdateActionPhase(state);
            UpdateDiscussionPhase(state);
            UpdateResultsPhase(state);
        }

        private static string PlayerName(GameState state, int index)
        {
            if (state.players != null)
            {
                foreach (var p in state.players)
                    if (p.index == index) return p.name;
            }
            return "플레이어";
        }

        private void UpdateDiceStage(GameState state)
        {
            if (diceRoll == null) return;

            var roll = state.lastRoll;
            if (state.phase == "round" && roll != null && roll.round == state.round)
                diceRoll.Play(roll, PlayerName(state, roll.playerIndex));
            else
                diceRoll.Dismiss();
        }

        private void UpdateHeaderAndNotices(GameState state)
        {
            bool isRound = state.phase == "round";
            bool isFinal = state.round == 4;

            if (roundEyebrowText != null)
            {
                string phaseLabel;
                if (isRound) phaseLabel = $"{state.round}라운드 {(isFinal ? "마지막 비공개 버리기" : "공개 버리기")}";
                else if (state.phase == "discussion") phaseLabel = $"{state.round}라운드 토론";
                else if (state.phase == "actions") phaseLabel = "직업 능력 발동";
                else if (state.phase == "results") phaseLabel = "최종 결산";
                else phaseLabel = "진행 중";
                roundEyebrowText.text = $"방 코드: {state.roomCode}  ·  {phaseLabel}";
            }

            var current = (state.players != null && state.turn >= 0 && state.turn < state.players.Length) ? state.players[state.turn] : null;
            bool isMyTurn = state.you != null && state.you.canDiscard;

            if (turnHeadingText != null)
            {
                turnHeadingText.color = Color.white;
                if (state.phase == "results") turnHeadingText.text = "게임이 끝났습니다.";
                else if (state.phase == "discussion") turnHeadingText.text = "토론 중입니다.";
                else if (state.phase == "actions") turnHeadingText.text = "직업 능력을 발동하는 중입니다.";
                else if (isMyTurn)
                {
                    turnHeadingText.text = "★ 당신의 차례입니다.";
                    turnHeadingText.color = new Color(1f, 0.90f, 0.45f);
                }
                else turnHeadingText.text = (current != null) ? $"{current.name} 플레이어의 차례입니다." : "진행 중...";
            }

            if (secretRoleText != null)
            {
                if (state.you != null && !string.IsNullOrEmpty(state.you.role))
                {
                    string roleName = RealmCard.RoleInfo.TryGetValue(state.you.role, out var info) ? info.name : state.you.role;
                    string roleRule = RealmCard.RoleInfo.TryGetValue(state.you.role, out var rinfo) ? rinfo.rule : "";
                    secretRoleText.text = $"당신의 비밀 직업: <color=#FFE08B><b>{roleName}</b></color> — {roleRule}";
                }
                else
                {
                    secretRoleText.text = "서버에서 내 비밀 직업을 수신하는 중...";
                }
            }

            if (lastRollText != null)
            {
                var roll = state.lastRoll;
                lastRollText.text = (roll != null && roll.round == state.round)
                    ? $"최근 D6 · <b>{PlayerName(state, roll.playerIndex)}</b> → <b>{roll.value}</b>"
                    : "";
            }

            if (turnPromptText != null)
            {
                // Discard instructions only make sense while discarding; other
                // phases were showing a meaningless "0~0장을 버리세요".
                if (!isRound)
                {
                    if (state.phase == "discussion") turnPromptText.text = "상대의 버림패를 읽고 직업을 추리하세요.";
                    else if (state.phase == "actions") turnPromptText.text = "직업 능력 처리가 끝나면 결산으로 넘어갑니다.";
                    else turnPromptText.text = "";
                }
                else if (state.required <= 0)
                {
                    int max = Math.Min(3, state.you?.hand != null ? state.you.hand.Length : 3);
                    turnPromptText.text = $"0~{max}장을 {(isFinal ? "비공개로" : "공개로")} 버리세요.";
                }
                else
                {
                    turnPromptText.text = $"주사위 결과 {state.required}: 정확히 <b>{state.required}장</b>을 반드시 버리세요.";
                }
            }
        }

        private void UpdatePlayersStrip(GameState state)
        {
            if (playersStripContainer == null || state.players == null) return;

            for (int i = playersStripContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(playersStripContainer.GetChild(i).gameObject);
            }

            var font = RealmCard.GetNotoTmpFont();

            foreach (var p in state.players)
            {
                var pillGo = new GameObject($"PlayerPill_{p.index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                pillGo.transform.SetParent(playersStripContainer, false);
                var pr = pillGo.GetComponent<RectTransform>();
                pr.anchorMin = new Vector2(0.5f, 0.5f);
                pr.anchorMax = new Vector2(0.5f, 0.5f);
                pr.pivot = new Vector2(0.5f, 0.5f);
                pr.sizeDelta = new Vector2(196f, 50f);
                var pillLayout = pillGo.GetComponent<LayoutElement>();
                pillLayout.preferredWidth = 196f;
                pillLayout.preferredHeight = 50f;

                bool isCurrent = state.phase == "round" && p.index == state.turn;
                bool isMe = state.you != null && p.index == state.you.index;

                var plate = isCurrent ? pillTurnSprite : (isMe ? pillSelfSprite : pillIdleSprite);
                var fallback = isCurrent ? new Color(0.06f, 0.19f, 0.34f, 0.98f)
                    : (isMe ? new Color(0.14f, 0.11f, 0.06f, 0.98f) : new Color(0.025f, 0.075f, 0.14f, 0.95f));
                ApplyPlate(pillGo.GetComponent<Image>(), plate, fallback);

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(pillGo.transform, false);
                var tr = txtGo.GetComponent<RectTransform>();
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.offsetMin = new Vector2(8f, 2f);
                tr.offsetMax = new Vector2(-8f, -2f);

                var txt = txtGo.GetComponent<TextMeshProUGUI>();
                txt.font = font;
                txt.fontSize = 17;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;

                string meTag = isMe ? " <color=#FFE08B>(나)</color>" : "";
                txt.text = $"<b>{p.name}</b>{meTag}  ({p.handCount}장)";
            }
        }

        private void UpdateHandAndSockets(GameState state)
        {
            CacheSockets();
            if (cardSocketsContainer == null) return;

            bool isRound = state.phase == "round";
            bool showHand = isRound || state.phase == "discussion";
            bool canSelect = isRound && state.you != null && state.you.canDiscard;
            cardSocketsContainer.gameObject.SetActive(showHand);
            if (selectionCountText != null) selectionCountText.gameObject.SetActive(isRound);
            if (discardConfirmButton != null) discardConfirmButton.gameObject.SetActive(isRound);

            if (!showHand) return;

            // Scene-authored sockets are only visual frames. Hide every unused
            // frame so the hand reads as cards, not as a row of empty slots.
            int handCount = state.you?.hand?.Length ?? 0;
            // The whole hand stays on one row at whatever width still fits, so a
            // ten-card hand reads at the same proportions as a two-card one.
            float cardScale = 1f;
            var handGrid = cardSocketsContainer.GetComponent<GridLayoutGroup>();
            if (handGrid != null && handCount > 0)
            {
                const float spacing = 10f;
                float available = ((RectTransform)cardSocketsContainer).rect.width - spacing * (handCount - 1);
                float cellWidth = Mathf.Clamp(available / handCount, 104f, CardWidth);
                handGrid.spacing = new Vector2(spacing, spacing);
                handGrid.cellSize = new Vector2(cellWidth, cellWidth * (CardHeight / CardWidth));
                cardScale = cellWidth / CardWidth;
            }
            for (int i = 0; i < _socketSlots.Count; i++)
            {
                bool hasCard = i < handCount;
                if (_socketSlots[i].gameObject.activeSelf != hasCard)
                    _socketSlots[i].gameObject.SetActive(hasCard);

                var socketImage = _socketSlots[i].GetComponent<Image>();
                if (socketImage != null) socketImage.color = new Color(0f, 0f, 0f, 0f);
                var socketOutline = _socketSlots[i].GetComponent<Outline>();
                if (socketOutline != null) socketOutline.enabled = false;
                var indexText = _socketSlots[i].Find("IndexText")?.GetComponent<TextMeshProUGUI>();
                if (indexText != null) indexText.enabled = false;
            }

            if (state.you?.hand != null)
            {
                _chosenCardIds.RemoveAll(id => Array.Find(state.you.hand, c => c.id == id) == null);

                // Destroy old spawned cards
                foreach (var c in _spawnedCards) if (c != null) Destroy(c.gameObject);
                _spawnedCards.Clear();

                // Put cards into card sockets
                for (int i = 0; i < state.you.hand.Length; i++)
                {
                    var cData = state.you.hand[i];
                    Transform parentSocket = (i < _socketSlots.Count) ? _socketSlots[i] : cardSocketsContainer;

                    var cardGo = InstantiateCardPrefab(parentSocket);
                    var rc = cardGo.GetComponent<RealmCard>();
                    rc.Setup(cData.id, cData.type, canSelect, mini: false);
                    rc.SetDisplayScale(cardScale);
                    rc.SetSelected(_chosenCardIds.Contains(cData.id));
                    rc.OnClicked += OnCardClicked;
                    _spawnedCards.Add(rc);
                }
            }

            UpdateDiscardButtonStatus();
        }

        private void OnCardClicked(RealmCard card)
        {
            if (_latestState == null || !_latestState.you.canDiscard) return;

            string id = card.CardId;
            int max = _latestState.required <= 0 ? 3 : _latestState.required;

            if (_chosenCardIds.Contains(id))
            {
                _chosenCardIds.Remove(id);
                card.SetSelected(false);
            }
            else
            {
                if (_chosenCardIds.Count < max)
                {
                    _chosenCardIds.Add(id);
                    card.SetSelected(true);
                }
            }

            UpdateDiscardButtonStatus();
        }

        private void UpdateDiscardButtonStatus()
        {
            if (_latestState == null) return;

            int count = _chosenCardIds.Count;
            int req = _latestState.required;
            bool valid = (req <= 0) ? (count <= 3) : (count == req);
            bool rolling = diceRoll != null && diceRoll.IsRolling;

            if (selectionCountText != null)
                selectionCountText.text = rolling
                    ? "주사위 확인 중…"
                    : (req <= 0 ? $"{count}장 선택" : $"{count}장 선택 / {req}장 필수");

            if (discardConfirmButton != null)
                discardConfirmButton.interactable = _latestState.you.canDiscard && valid && !rolling;
        }

        private void OnDiscardConfirmClicked()
        {
            if (_chosenCardIds.Count == 0 && _latestState.required > 0) return;

            Debug.Log($"[RealmGameUI] Confirming discard: {string.Join(", ", _chosenCardIds)}");
            RealmNetworkManager.Instance.SendMessagePayload("DISCARD", new DiscardPayload { cardIds = _chosenCardIds.ToArray() });
            _chosenCardIds.Clear();
        }

        [Serializable]
        private class DiscardPayload
        {
            public string[] cardIds;
        }

        private void UpdatePublicGrave(GameState state)
        {
            if (graveListContainer == null || state.players == null) return;

            for (int i = graveListContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(graveListContainer.GetChild(i).gameObject);
            }

            var font = RealmCard.GetNotoTmpFont();

            float rowWidth = ((RectTransform)graveListContainer).rect.width - GraveContentPadding;
            float pilesWidth = rowWidth * 0.96f;
            float slotWidth = (pilesWidth - GraveSlotSpacing * (GraveSlotCount - 1)) / GraveSlotCount;

            foreach (var p in state.players)
            {
                bool hasVisibleDiscard = (p.publicDiscard != null && p.publicDiscard.Length > 0) || p.finalDiscardCount > 0;
                if (!hasVisibleDiscard) continue;

                // One full-width row per player, stacked by the container's
                // VerticalLayoutGroup so the modal scrolls vertically.
                var rowGo = new GameObject($"GraveRow_{p.name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                rowGo.transform.SetParent(graveListContainer, false);
                var rowLayout = rowGo.GetComponent<LayoutElement>();
                rowLayout.minHeight = GraveRowHeight;
                rowLayout.preferredHeight = GraveRowHeight;

                var rowImg = rowGo.GetComponent<Image>();
                ApplyPanelSkin(rowImg, new Color(0.015f, 0.055f, 0.10f, 0.96f));
                var rowOutl = rowGo.AddComponent<Outline>();
                rowOutl.effectColor = new Color(0.2f, 0.35f, 0.5f, 0.4f);

                // Player name label
                var nameGo = new GameObject("PlayerName", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                nameGo.transform.SetParent(rowGo.transform, false);
                var nr = nameGo.GetComponent<RectTransform>();
                nr.anchorMin = new Vector2(0.02f, 0.865f);
                nr.anchorMax = new Vector2(0.98f, 0.99f);
                nr.offsetMin = Vector2.zero;
                nr.offsetMax = Vector2.zero;

                var nt = nameGo.GetComponent<TextMeshProUGUI>();
                nt.font = font;
                nt.fontSize = 19;
                nt.fontStyle = FontStyles.Bold;
                nt.color = new Color(1f, 0.90f, 0.65f);
                nt.text = $"<b>{p.name}</b>";

                // Piles container
                var pilesGo = new GameObject("PilesContainer", typeof(RectTransform));
                pilesGo.transform.SetParent(rowGo.transform, false);
                var pr = pilesGo.GetComponent<RectTransform>();
                // Leaves a strip above the slots for the round badges, which used
                // to sit on top of the cards.
                pr.anchorMin = new Vector2(0.02f, 0.02f);
                pr.anchorMax = new Vector2(0.98f, 0.735f);
                pr.offsetMin = Vector2.zero;
                pr.offsetMax = Vector2.zero;

                // All four rounds always get a slot, so the 1R/2R/3R/4R columns
                // line up across every player even when a round is empty.
                for (int r = 1; r <= 3; r++)
                {
                    int roundNum = r;
                    var roundCards = (p.publicDiscard != null) ? p.publicDiscard.Where(c => c.round == roundNum || (c.round == 0 && roundNum == 1)).ToArray() : new CardData[0];
                    CreateDiscardPileUI(pilesGo.transform, $"{roundNum}R", roundCards, font, slotWidth, roundNum - 1);
                }
                CreateSecretPileUI(pilesGo.transform, "4R", p.finalDiscardCount, font, slotWidth, 3);
            }

        }

        private void OnOpenGraveClicked()
        {
            if (graveModal == null) return;
            graveModal.SetActive(true);
            if (_latestState != null) UpdatePublicGrave(_latestState);

            var scroll = graveModal.GetComponentInChildren<ScrollRect>(true);
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        private void OnCloseGraveClicked()
        {
            if (graveModal != null) graveModal.SetActive(false);
        }

        private void CreateDiscardPileUI(Transform parent, string roundLabel, CardData[] cards, TMP_FontAsset font, float slotWidth, int slotIndex)
        {
            var pile = CreateGraveSlot(parent, $"Pile_{roundLabel}", roundLabel, font, slotWidth, slotIndex,
                publicSlotSprite, new Color(0.025f, 0.09f, 0.16f, 0.94f),
                roundBadgeSprite, new Color(0.18f, 0.40f, 0.60f, 0.9f), 68f);

            foreach (var cData in cards)
            {
                var cardGo = InstantiateCardPrefab(pile);
                var rc = cardGo.GetComponent<RealmCard>();
                // Full game card, so the win condition is readable straight
                // from the grave rather than only on the player's own hand.
                rc.Setup(cData.id, cData.type, false, mini: false);
                rc.SetDisplayScale(GraveCardScale);
                DisableCardRaycasts(cardGo);
            }

            FinishGraveSlot(pile, cards.Length, font);
        }

        private void CreateSecretPileUI(Transform parent, string roundLabel, int count, TMP_FontAsset font, float slotWidth, int slotIndex)
        {
            // ART_DIRECTION pins the secret state to violet (#513a71), not the
            // red this used to fall back to.
            var pile = CreateGraveSlot(parent, $"SecretPile_{roundLabel}", $"{roundLabel} 비공개", font, slotWidth, slotIndex,
                secretSlotSprite, new Color(0.19f, 0.13f, 0.28f, 0.95f),
                secretBadgeSprite, new Color(0.32f, 0.23f, 0.44f, 0.92f), 112f);

            for (int i = 0; i < count; i++)
            {
                var cardGo = InstantiateCardPrefab(pile);
                var rc = cardGo.GetComponent<RealmCard>();
                rc.Setup("", "", false, mini: false); // Face-down
                rc.SetDisplayScale(GraveCardScale);
                DisableCardRaycasts(cardGo);
            }

            FinishGraveSlot(pile, count, font);
        }

        private Transform CreateGraveSlot(Transform parent, string name, string badgeLabel, TMP_FontAsset font,
            float slotWidth, int slotIndex, Sprite surfaceSprite, Color surfaceTint,
            Sprite badgeSprite, Color badgeTint, float badgeWidth)
        {
            var pileGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(DiscardPileHover));
            pileGo.transform.SetParent(parent, false);

            // Slots are placed by hand rather than by a layout group: every
            // round then occupies the same x across players, and a hovered pile
            // is free to jump to the front without disturbing the arrangement.
            var slotRect = pileGo.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 0.5f);
            slotRect.anchorMax = new Vector2(0f, 0.5f);
            slotRect.pivot = new Vector2(0f, 0.5f);
            slotRect.sizeDelta = new Vector2(slotWidth, GraveSlotHeight);
            slotRect.anchoredPosition = new Vector2(slotIndex * (slotWidth + GraveSlotSpacing), 0f);

            ApplyPlate(pileGo.GetComponent<Image>(), surfaceSprite, surfaceTint);

            // Sits just above the slot instead of over the first card.
            var badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeGo.transform.SetParent(pileGo.transform, false);
            var br = badgeGo.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0f, 1f);
            br.anchorMax = new Vector2(0f, 1f);
            br.pivot = new Vector2(0f, 0f);
            br.anchoredPosition = new Vector2(2f, 5f);
            br.sizeDelta = new Vector2(badgeWidth, GraveBadgeHeight);
            ApplyPlate(badgeGo.GetComponent<Image>(), badgeSprite, badgeTint);

            var btGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            btGo.transform.SetParent(badgeGo.transform, false);
            var btr = btGo.GetComponent<RectTransform>();
            btr.anchorMin = Vector2.zero;
            btr.anchorMax = Vector2.one;
            btr.offsetMin = Vector2.zero;
            btr.offsetMax = Vector2.zero;
            var bt = btGo.GetComponent<TextMeshProUGUI>();
            bt.font = font;
            bt.fontSize = 13;
            bt.fontStyle = FontStyles.Bold;
            bt.alignment = TextAlignmentOptions.Center;
            bt.color = Color.white;
            bt.text = badgeLabel;

            return pileGo.transform;
        }

        private static void FinishGraveSlot(Transform pile, int cardCount, TMP_FontAsset font)
        {
            if (cardCount == 0)
            {
                var emptyGo = new GameObject("EmptyText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                emptyGo.transform.SetParent(pile, false);
                var er = emptyGo.GetComponent<RectTransform>();
                er.anchorMin = Vector2.zero;
                er.anchorMax = new Vector2(1f, 0.76f);
                er.offsetMin = Vector2.zero;
                er.offsetMax = Vector2.zero;
                var et = emptyGo.GetComponent<TextMeshProUGUI>();
                et.font = font;
                et.fontSize = 16;
                et.alignment = TextAlignmentOptions.Center;
                et.color = new Color(0.40f, 0.50f, 0.60f);
                et.text = "버린 카드 없음";
                return;
            }

            pile.GetComponent<DiscardPileHover>().RefreshStack();
        }

        // The pile root owns hover input. Child card graphics are visual-only,
        // so entering a card cannot emit a false exit on the pile.
        private static void DisableCardRaycasts(GameObject cardGo)
        {
            foreach (var graphic in cardGo.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        private void ApplyPanelSkin(Image image, Color tint)
        {
            if (image == null) return;
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = tint;
        }

        // Authored plate art, falling back to the flat tint when a sprite has
        // not been assigned so the UI still reads if art is missing.
        private static void ApplyPlate(Image image, Sprite sprite, Color fallbackTint)
        {
            if (image == null) return;
            image.preserveAspect = false;
            if (sprite == null)
            {
                image.sprite = null;
                image.type = Image.Type.Simple;
                image.color = fallbackTint;
                return;
            }
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        private void UpdateActionPhase(GameState state)
        {
            bool isAction = state.phase == "actions";
            if (actionPanel != null) actionPanel.SetActive(isAction);
            if (!isAction) return;

            var action = state.action;
            bool canAct = state.you.canAct && action != null && action.playerIndex == state.you.index;

            if (actionTitleText != null)
            {
                if (canAct)
                {
                    string rName = RealmCard.RoleInfo.TryGetValue(action.role, out var inf) ? inf.name : action.role;
                    actionTitleText.text = $"★ 당신의 <b>{rName}</b> 능력을 사용할 차례입니다!";
                }
                else if (action != null)
                {
                    string rName = RealmCard.RoleInfo.TryGetValue(action.role, out var inf) ? inf.name : action.role;
                    actionTitleText.text = $"누군가 <b>{rName}</b> 능력을 발동 중입니다...";
                }
            }

            if (actionConfirmButton != null) actionConfirmButton.interactable = canAct;
            if (canAct && action != null) PopulateActionDropdowns(action.role, state);
        }

        private void PopulateActionDropdowns(string role, GameState state)
        {
            if (targetDropdown1 == null || roleDropdown1 == null) return;

            targetDropdown1.ClearOptions();
            roleDropdown1.ClearOptions();

            var playerNames = new List<string>();
            foreach (var p in state.players)
            {
                if (p.index != state.you.index) playerNames.Add(p.name);
            }
            targetDropdown1.AddOptions(playerNames);

            var roleNames = new List<string>();
            foreach (var r in state.selected)
            {
                string rName = RealmCard.RoleInfo.TryGetValue(r, out var inf) ? inf.name : r;
                roleNames.Add(rName);
            }
            roleDropdown1.AddOptions(roleNames);
        }

        private void OnActionConfirmClicked()
        {
            if (_latestState?.action == null || targetDropdown1 == null || roleDropdown1 == null) return;
            string role = _latestState.action.role;

            int targetIdx = targetDropdown1.value;
            var otherPlayers = new List<PublicPlayer>();
            foreach (var p in _latestState.players) if (p.index != _latestState.you.index) otherPlayers.Add(p);
            int resolvedPlayer = (targetIdx < otherPlayers.Count) ? otherPlayers[targetIdx].index : 0;

            int roleIdx = roleDropdown1.value;
            string resolvedRole = (roleIdx < _latestState.selected.Length) ? _latestState.selected[roleIdx] : "king";

            Debug.Log($"[RealmGameUI] Sending ACTION for {role}: target={resolvedPlayer}, role={resolvedRole}");

            if (role == "assassin")
            {
                RealmNetworkManager.Instance.SendMessagePayload("ACTION", new AssassinActionPayload { p1 = resolvedPlayer, r1 = resolvedRole, p2 = resolvedPlayer, r2 = resolvedRole });
            }
            else
            {
                RealmNetworkManager.Instance.SendMessagePayload("ACTION", new TargetRoleActionPayload { target = resolvedPlayer, role = resolvedRole });
            }
        }

        [Serializable]
        private class AssassinActionPayload { public int p1; public string r1; public int p2; public string r2; }
        [Serializable]
        private class TargetRoleActionPayload { public int target; public string role; }

        private void UpdateDiscussionPhase(GameState state)
        {
            bool isDisc = state.phase == "discussion";
            if (discussionPanel != null) discussionPanel.SetActive(isDisc);
            if (!isDisc) return;

            if (discussionTimerText != null) discussionTimerText.text = $"토론 시간 ({state.discussionSeconds}초)";

            if (skipVotesText != null)
            {
                int votes = state.discussionVotes != null ? state.discussionVotes.Length : 0;
                skipVotesText.text = $"토론 종료 동의: {votes} / {state.playerTarget}";
            }

            if (chatLogText != null && state.chat != null)
            {
                var lines = new List<string>();
                foreach (var m in state.chat)
                {
                    string mine = (m.playerIndex == state.you.index) ? "<color=#FFE08B>[나]</color> " : "";
                    lines.Add($"{mine}<b>{m.name}</b>: {m.text}");
                }
                chatLogText.text = string.Join("\n", lines);
            }
        }

        private void OnSkipDiscussionClicked()
        {
            RealmNetworkManager.Instance.SendMessagePayload("SKIP_DISCUSSION");
        }

        private void OnSendChatClicked()
        {
            if (chatInputField == null || string.IsNullOrWhiteSpace(chatInputField.text)) return;
            string txt = chatInputField.text.Trim();
            chatInputField.text = "";
            RealmNetworkManager.Instance.SendMessagePayload("CHAT", new ChatPayload { text = txt });
        }

        [Serializable]
        private class ChatPayload { public string text; }

        private void UpdateResultsPhase(GameState state)
        {
            bool isRes = state.phase == "results";
            if (resultsPanel != null) resultsPanel.SetActive(isRes);
            if (!isRes) return;

            if (resultsRankingsText != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("<b>★ 게임 종료 · 최종 결산</b>\n");

                if (state.players != null)
                {
                    foreach (var p in state.players)
                    {
                        string rName = RealmCard.RoleInfo.TryGetValue(p.role ?? "", out var inf) ? inf.name : p.role;
                        sb.AppendLine($"• <b>{p.name}</b> (직업: {rName}) - 최종 손패 {p.handCount}장");
                    }
                }

                resultsRankingsText.text = sb.ToString();
            }
        }

        private void OnReturnLobbyClicked()
        {
            gameObject.SetActive(false);
            var lobby = FindAnyObjectByType<RealmLobbyController>(FindObjectsInactive.Include);
            if (lobby != null)
            {
                lobby.ReturnToLobby();
            }
        }

        private GameObject InstantiateCardPrefab(Transform parent)
        {
            if (cardPrefab != null)
            {
                var go = Instantiate(cardPrefab, parent);
                go.SetActive(true);
                return go;
            }

            throw new InvalidOperationException("Assign the MCP-authored RealmCardTemplate to RealmGameUI.cardPrefab.");
        }
    }
}
