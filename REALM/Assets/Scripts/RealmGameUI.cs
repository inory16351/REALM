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
        [SerializeField] public TMP_Dropdown targetDropdown2;
        [SerializeField] public TMP_Dropdown roleDropdown2;
        [SerializeField] public GameObject secondGuessRow;
        [SerializeField] public GameObject actionFormRoot;
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
        private GameObject _graveZoom;
        private readonly List<FormRow> _formRows = new List<FormRow>();
        private string _formRole;
        private Transform _graveZoomCards;
        private TextMeshProUGUI _graveZoomTitle;

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
                if (secondGuessRow == null) secondGuessRow = actionPanel.transform.Find("SecondGuessRow")?.gameObject;
                if (secondGuessRow != null)
                {
                    if (targetDropdown2 == null) targetDropdown2 = secondGuessRow.transform.Find("TargetDropdown2")?.GetComponent<TMP_Dropdown>();
                    if (roleDropdown2 == null) roleDropdown2 = secondGuessRow.transform.Find("RoleDropdown2")?.GetComponent<TMP_Dropdown>();
                }
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
            UpdateAccusationPhase(state);
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
                    if (RealmCard.RoleInfo.TryGetValue(state.you.role, out var info))
                    {
                        string bonus = info.bonus > 0 ? $" <color=#84E5B5>(성공 +{info.bonus})</color>" : "";
                        secretRoleText.text = $"당신의 비밀 직업: <color=#FFE08B><b>{info.name}</b></color>{bonus} — {info.rule}";
                    }
                    else secretRoleText.text = $"당신의 비밀 직업: <color=#FFE08B><b>{state.you.role}</b></color>";
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
            CloseGraveZoom();

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
                    CreateDiscardPileUI(pilesGo.transform, $"{roundNum}R", roundCards, font, slotWidth, roundNum - 1, p.name);
                }
                CreateSecretPileUI(pilesGo.transform, "4R", p.finalDiscardCount, font, slotWidth, 3, p.name);
            }

        }

        private void OnOpenGraveClicked()
        {
            if (graveModal == null) return;
            graveModal.SetActive(true);

            // The grave sits before the discussion and action panels in the
            // hierarchy, so during those phases it would open *behind* them and
            // look like nothing happened. Lift it to the front on every open,
            // then put the toast back on top so warnings stay visible.
            graveModal.transform.SetAsLastSibling();
            if (warningToast != null) warningToast.transform.SetAsLastSibling();

            if (_latestState != null) UpdatePublicGrave(_latestState);

            var scroll = graveModal.GetComponentInChildren<ScrollRect>(true);
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }

        private void OnCloseGraveClicked()
        {
            CloseGraveZoom();
            if (graveModal != null) graveModal.SetActive(false);
        }

        // The grave slots are ~200px tall, so a card in them is unreadable.
        // Clicking one lifts that round's cards into a full-size overlay.
        private void EnsureGraveZoom()
        {
            if (_graveZoom != null || graveModal == null) return;
            var font = RealmCard.GetNotoTmpFont();

            _graveZoom = new GameObject("GraveZoomOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            _graveZoom.transform.SetParent(graveModal.transform, false);
            var rt = (RectTransform)_graveZoom.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var backdrop = _graveZoom.GetComponent<Image>();
            backdrop.color = new Color(0.01f, 0.02f, 0.05f, 0.93f);
            // The backdrop is the dismiss target: anywhere outside a card closes.
            var btn = _graveZoom.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(CloseGraveZoom);

            var titleGo = new GameObject("ZoomTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(_graveZoom.transform, false);
            var tr = (RectTransform)titleGo.transform;
            tr.anchorMin = new Vector2(0.05f, 0.90f); tr.anchorMax = new Vector2(0.95f, 0.975f);
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            _graveZoomTitle = titleGo.GetComponent<TextMeshProUGUI>();
            _graveZoomTitle.font = font;
            _graveZoomTitle.fontSize = 30f;
            _graveZoomTitle.fontStyle = FontStyles.Bold;
            _graveZoomTitle.color = new Color(1f, 0.90f, 0.65f);
            _graveZoomTitle.alignment = TextAlignmentOptions.Center;
            _graveZoomTitle.raycastTarget = false;

            var hintGo = new GameObject("ZoomHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            hintGo.transform.SetParent(_graveZoom.transform, false);
            var hr = (RectTransform)hintGo.transform;
            hr.anchorMin = new Vector2(0.05f, 0.025f); hr.anchorMax = new Vector2(0.95f, 0.08f);
            hr.offsetMin = Vector2.zero; hr.offsetMax = Vector2.zero;
            var hint = hintGo.GetComponent<TextMeshProUGUI>();
            hint.font = font;
            hint.fontSize = 20f;
            hint.color = new Color(0.62f, 0.69f, 0.79f);
            hint.alignment = TextAlignmentOptions.Center;
            hint.text = "빈 공간을 클릭하면 닫힙니다";
            hint.raycastTarget = false;

            var cardsGo = new GameObject("ZoomCards", typeof(RectTransform));
            cardsGo.transform.SetParent(_graveZoom.transform, false);
            var cr = (RectTransform)cardsGo.transform;
            cr.anchorMin = new Vector2(0.04f, 0.10f); cr.anchorMax = new Vector2(0.96f, 0.88f);
            cr.offsetMin = Vector2.zero; cr.offsetMax = Vector2.zero;
            _graveZoomCards = cardsGo.transform;

            _graveZoom.SetActive(false);
        }

        private void OpenGraveZoom(DiscardPileHover pile)
        {
            if (pile == null) return;
            EnsureGraveZoom();
            if (_graveZoom == null) return;

            for (int i = _graveZoomCards.childCount - 1; i >= 0; i--)
                DestroyImmediate(_graveZoomCards.GetChild(i).gameObject);

            int count = pile.secret ? pile.secretCount : (pile.cardData != null ? pile.cardData.Length : 0);
            _graveZoomTitle.text = count == 0
                ? $"{pile.ownerName} · {pile.roundLabel} — 버린 카드 없음"
                : $"{pile.ownerName} · {pile.roundLabel} — {count}장" + (pile.secret ? " (비공개)" : "");

            _graveZoom.SetActive(true);
            _graveZoom.transform.SetAsLastSibling();
            if (count == 0) return;

            // Fit the grid to the overlay rather than to a fixed scale, so a
            // one-card pile reads large and a nine-card pile still fits.
            var area = (RectTransform)_graveZoomCards;
            float areaW = area.rect.width, areaH = area.rect.height;
            int cols = Mathf.Min(count, 5);
            int rows = Mathf.CeilToInt(count / (float)cols);
            float scale = Mathf.Min(2.0f,
                areaW / (cols * CardWidth * 1.10f),
                areaH / (rows * CardHeight * 1.12f));
            float stepX = CardWidth * scale * 1.10f;
            float stepY = CardHeight * scale * 1.12f;

            for (int i = 0; i < count; i++)
            {
                var go = InstantiateCardPrefab(_graveZoomCards);
                var rc = go.GetComponent<RealmCard>();
                if (pile.secret) rc.Setup("", "", false, mini: false);
                else rc.Setup(pile.cardData[i].id, pile.cardData[i].type, false, mini: false);
                rc.SetDisplayScale(scale);
                DisableCardRaycasts(go);

                int row = i / cols;
                int inRow = i % cols;
                int thisRowCount = Mathf.Min(cols, count - row * cols);
                var crt = (RectTransform)go.transform;
                crt.anchorMin = new Vector2(0.5f, 0.5f);
                crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = new Vector2(
                    (inRow - (thisRowCount - 1) / 2f) * stepX,
                    -(row - (rows - 1) / 2f) * stepY);
                crt.localRotation = Quaternion.identity;
            }
        }

        private void CloseGraveZoom()
        {
            if (_graveZoom == null) return;
            for (int i = _graveZoomCards.childCount - 1; i >= 0; i--)
                DestroyImmediate(_graveZoomCards.GetChild(i).gameObject);
            _graveZoom.SetActive(false);
        }

        private void CreateDiscardPileUI(Transform parent, string roundLabel, CardData[] cards, TMP_FontAsset font, float slotWidth, int slotIndex, string ownerName)
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

            var hover = pile.GetComponent<DiscardPileHover>();
            if (hover != null)
            {
                hover.ownerName = ownerName;
                hover.roundLabel = roundLabel;
                hover.cardData = cards;
                hover.secret = false;
                hover.onZoomRequested = OpenGraveZoom;
            }

            FinishGraveSlot(pile, cards.Length, font);
        }

        private void CreateSecretPileUI(Transform parent, string roundLabel, int count, TMP_FontAsset font, float slotWidth, int slotIndex, string ownerName)
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

            var hover = pile.GetComponent<DiscardPileHover>();
            if (hover != null)
            {
                hover.ownerName = ownerName;
                hover.roundLabel = roundLabel;
                hover.secret = true;
                hover.secretCount = count;
                hover.onZoomRequested = OpenGraveZoom;
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

        // The assassin's second row lives in the panel's bottom padding, which
        // the confirm button already occupies, so the button slides down to
        // make room and returns to its authored spot otherwise.
        private void SetConfirmButtonLowered(bool lowered)
        {
            if (actionConfirmButton == null) return;
            var rt = actionConfirmButton.transform as RectTransform;
            if (rt == null) return;

            var min = lowered ? new Vector2(0.3f, 0.04f) : new Vector2(0.3f, 0.19f);
            var max = lowered ? new Vector2(0.7f, 0.17f) : new Vector2(0.7f, 0.33f);
            if (rt.anchorMin == min && rt.anchorMax == max) return;

            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // The accusation phase reuses the action panel: it already is a
        // target + role picker, which is exactly what a guess needs.
        private void UpdateAccusationPhase(GameState state)
        {
            bool isAccuse = state.phase == "accusation";
            // Both phases share the action panel and this runs second, so during
            // the action phase it must leave the panel alone.
            if (!isAccuse && state.phase == "actions") return;
            if (actionPanel != null) actionPanel.SetActive(isAccuse);
            bool showSecond = isAccuse && state.you != null && state.you.accuseQuota >= 2;
            if (secondGuessRow != null) secondGuessRow.SetActive(showSecond);
            SetConfirmButtonLowered(showSecond);
            if (!isAccuse) return;

            if (actionFormRoot != null) actionFormRoot.SetActive(false);
            if (targetDropdown1 != null) targetDropdown1.gameObject.SetActive(true);
            if (roleDropdown1 != null) roleDropdown1.gameObject.SetActive(true);
            var tc = actionPanel != null ? actionPanel.transform.Find("TargetCaption") : null;
            var rc = actionPanel != null ? actionPanel.transform.Find("RoleCaption") : null;
            if (tc != null) tc.gameObject.SetActive(true);
            if (rc != null) rc.gameObject.SetActive(true);

            bool canAccuse = state.you != null && state.you.canAccuse;
            int quota = state.you != null ? Mathf.Max(1, state.you.accuseQuota) : 1;

            if (actionTitleText != null)
                actionTitleText.text = canAccuse
                    ? (quota >= 2
                        ? "★ 최종 지목 — 서로 다른 <b>두 명</b>과 그 직업을 지목하세요"
                        : "★ 최종 지목 — 상대 <b>한 명</b>과 그 직업을 지목하세요")
                    : $"지목 완료. 다른 플레이어를 기다리는 중... ({state.accusedCount} / {state.playerTarget})";

            if (actionConfirmButton != null) actionConfirmButton.interactable = canAccuse;
            var confirmLabel = actionConfirmButton != null ? actionConfirmButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (confirmLabel != null) confirmLabel.text = "지목 확정";

            if (canAccuse) PopulateGuessDropdowns(state);
        }

        private void PopulateGuessDropdowns(GameState state)
        {
            var names = new List<string>();
            foreach (var p in state.players) if (p.index != state.you.index) names.Add(p.name);

            var roleNames = new List<string>();
            foreach (var r in state.selected)
                roleNames.Add(RealmCard.RoleInfo.TryGetValue(r, out var inf) ? inf.name : r);

            foreach (var dd in new[] { targetDropdown1, targetDropdown2 })
            {
                if (dd == null) continue;
                dd.ClearOptions(); dd.AddOptions(names); dd.RefreshShownValue();
            }
            foreach (var dd in new[] { roleDropdown1, roleDropdown2 })
            {
                if (dd == null) continue;
                dd.ClearOptions(); dd.AddOptions(roleNames); dd.RefreshShownValue();
            }
            // Default the second pick to a different player so the server's
            // "two distinct players" check is not tripped by the defaults.
            if (targetDropdown2 != null && names.Count > 1) targetDropdown2.value = 1;
        }

        private void UpdateActionPhase(GameState state)
        {
            bool isAction = state.phase == "actions";
            if (actionPanel != null && !isAction && state.phase != "accusation") actionPanel.SetActive(false);
            if (isAction && actionPanel != null) actionPanel.SetActive(true);
            if (!isAction) return;
            if (secondGuessRow != null) secondGuessRow.SetActive(false);
            SetConfirmButtonLowered(false);
            var confirmLabel = actionConfirmButton != null ? actionConfirmButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (confirmLabel != null) confirmLabel.text = "능력 발동하기";

            // JsonUtility materialises nested [Serializable] classes even when the
            // server sends "action": null, so an empty role has to count as no
            // action - otherwise the dictionary lookup below throws on a null key.
            var action = state.action;
            if (action != null && string.IsNullOrEmpty(action.role)) action = null;
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

            // The action phase now drives the per-role form; the old
            // target+role dropdown pair is only used by the accusation phase.
            bool useForm = canAct && action != null;
            if (actionFormRoot != null) actionFormRoot.SetActive(useForm);
            if (targetDropdown1 != null) targetDropdown1.gameObject.SetActive(!useForm);
            if (roleDropdown1 != null) roleDropdown1.gameObject.SetActive(!useForm);
            var cap1 = actionPanel != null ? actionPanel.transform.Find("TargetCaption") : null;
            var cap2 = actionPanel != null ? actionPanel.transform.Find("RoleCaption") : null;
            if (cap1 != null) cap1.gameObject.SetActive(!useForm);
            if (cap2 != null) cap2.gameObject.SetActive(!useForm);
            if (useForm) ConfigureActionForm(action.role, state);
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

        // Resolves a (target dropdown, role dropdown) pair back into the server's
        // own player index and role key. Returns null when the pair is unusable.
        private Guess ResolveGuess(TMP_Dropdown target, TMP_Dropdown role)
        {
            if (_latestState == null || target == null || role == null) return null;

            var others = new List<PublicPlayer>();
            foreach (var p in _latestState.players) if (p.index != _latestState.you.index) others.Add(p);

            // The dropdowns must agree with the state they were filled from;
            // an empty or stale list would otherwise silently resolve to index 0.
            if (target.options.Count != others.Count || role.options.Count != _latestState.selected.Length) return null;
            if (target.value < 0 || target.value >= others.Count) return null;
            if (role.value < 0 || role.value >= _latestState.selected.Length) return null;

            return new Guess { player = others[target.value].index, role = _latestState.selected[role.value] };
        }

        private void OnAccuseConfirmClicked()
        {
            int quota = _latestState.you != null ? Mathf.Max(1, _latestState.you.accuseQuota) : 1;

            var guesses = new List<Guess>();
            var first = ResolveGuess(targetDropdown1, roleDropdown1);
            if (first == null) { ShowWarning("지목할 상대와 직업을 고르세요."); return; }
            guesses.Add(first);

            if (quota >= 2)
            {
                var second = ResolveGuess(targetDropdown2, roleDropdown2);
                if (second == null) { ShowWarning("두 번째 지목도 골라야 합니다."); return; }
                if (second.player == first.player) { ShowWarning("서로 다른 두 명을 지목해야 합니다."); return; }
                guesses.Add(second);
            }

            Debug.Log($"[RealmGameUI] Sending ACCUSE with {guesses.Count} guess(es).");
            RealmNetworkManager.Instance.SendMessagePayload("ACCUSE", new AccusePayload { guesses = guesses.ToArray() });
        }


        // Every action role needs a different set of choices, and only the
        // chancellor's happened to match the old target+role pair. Rather than
        // seven bespoke panels this is one stack of labelled dropdowns that gets
        // reconfigured per role; each row remembers what its options mean.
        private enum FormKind { Player, Player2, Role, Hand, HandOrGrave, Position, Mode }

        private class FormRow
        {
            public GameObject go;
            public TextMeshProUGUI label;
            public TMP_Dropdown dropdown;
            public FormKind kind;
            // Parallel to the dropdown options: the token each option stands for
            // (a card id, a player index, a role key, a hand position...).
            public readonly List<string> values = new List<string>();
        }

        private void CacheFormRows()
        {
            _formRows.Clear();
            if (actionFormRoot == null) return;
            for (int i = 0; i < actionFormRoot.transform.childCount; i++)
            {
                var child = actionFormRoot.transform.GetChild(i);
                var dd = child.GetComponentInChildren<TMP_Dropdown>(true);
                var lb = child.Find("Label")?.GetComponent<TextMeshProUGUI>();
                if (dd == null || lb == null) continue;
                _formRows.Add(new FormRow { go = child.gameObject, label = lb, dropdown = dd });
            }
        }

        private FormRow UseRow(int index, string label, FormKind kind)
        {
            if (index >= _formRows.Count) return null;
            var row = _formRows[index];
            row.go.SetActive(true);
            row.label.text = label;
            row.kind = kind;
            row.values.Clear();
            row.dropdown.ClearOptions();
            return row;
        }

        private static string CardLabel(CardData card)
        {
            string name = RealmCard.RoleInfo.TryGetValue(card.type ?? "", out var inf) ? inf.name : card.type;
            return name;
        }

        private void FillPlayers(FormRow row, GameState state, bool includeSelf)
        {
            var names = new List<string>();
            foreach (var p in state.players)
            {
                if (!includeSelf && state.you != null && p.index == state.you.index) continue;
                names.Add($"{p.name} (손패 {p.handCount}장)");
                row.values.Add(p.index.ToString());
            }
            row.dropdown.AddOptions(names);
            row.dropdown.RefreshShownValue();
        }

        private void FillRoles(FormRow row, GameState state)
        {
            var names = new List<string>();
            foreach (var key in state.selected)
            {
                names.Add(RealmCard.RoleInfo.TryGetValue(key, out var inf) ? inf.name : key);
                row.values.Add(key);
            }
            row.dropdown.AddOptions(names);
            row.dropdown.RefreshShownValue();
        }

        private void FillHand(FormRow row, GameState state)
        {
            var names = new List<string>();
            var hand = state.you?.hand ?? new CardData[0];
            for (int i = 0; i < hand.Length; i++)
            {
                names.Add($"{i + 1}. {CardLabel(hand[i])}");
                row.values.Add(hand[i].id);
            }
            row.dropdown.AddOptions(names);
            row.dropdown.RefreshShownValue();
        }

        // Public grave cards, tagged so the payload builder can tell them apart
        // from the player's own hand.
        private void FillGrave(FormRow row, GameState state, bool withNone)
        {
            var names = new List<string>();
            if (withNone) { names.Add("— 교환 안 함 —"); row.values.Add(""); }
            foreach (var p in state.players)
            {
                if (p.publicDiscard == null) continue;
                foreach (var c in p.publicDiscard)
                {
                    names.Add($"[{p.name}] {CardLabel(c)}");
                    row.values.Add("grave:" + c.id);
                }
            }
            row.dropdown.AddOptions(names);
            row.dropdown.RefreshShownValue();
        }

        private void HideRowsFrom(int index)
        {
            for (int i = index; i < _formRows.Count; i++) _formRows[i].go.SetActive(false);
        }

        private void ConfigureActionForm(string role, GameState state)
        {
            if (_formRows.Count == 0) CacheFormRows();
            if (_formRows.Count == 0) return;
            _formRole = role;
            int n = 0;

            switch (role)
            {
                case "hunter":
                    FillPlayers(UseRow(n++, "지목할 상대", FormKind.Player), state, false);
                    FillRoles(UseRow(n++, "그 손패에 있을 카드", FormKind.Role), state);
                    break;
                case "chancellor":
                    FillRoles(UseRow(n++, "가장 많을 카드 종류", FormKind.Role), state);
                    break;
                case "courtesan":
                    FillPlayers(UseRow(n++, "지목할 상대", FormKind.Player), state, false);
                    break;
                case "thief":
                {
                    var t1 = UseRow(n++, "훔칠 상대 ①", FormKind.Player);
                    FillPlayers(t1, state, false);
                    var p1 = UseRow(n++, "카드 위치 ①", FormKind.Position);
                    var t2 = UseRow(n++, "훔칠 상대 ② (같은 사람 가능)", FormKind.Player2);
                    FillPlayers(t2, state, false);
                    var p2 = UseRow(n++, "카드 위치 ②", FormKind.Position);
                    // The slot list must match whichever player is selected, so
                    // it is rebuilt whenever that target changes.
                    BindPositionsTo(t1, p1, state);
                    BindPositionsTo(t2, p2, state);
                    if (t2.dropdown.options.Count > 1) { t2.dropdown.value = 1; t2.dropdown.RefreshShownValue(); }
                    RefillPositions(t2, p2, state);
                    break;
                }
                case "king":
                {
                    var target = UseRow(n++, "카드를 버리게 할 상대", FormKind.Player);
                    FillPlayers(target, state, false);
                    // Hands are hidden, so the king picks blind positions.
                    for (int k = 0; k < 2; k++)
                    {
                        var row = UseRow(n++, $"버리게 할 카드 위치 {(k == 0 ? "①" : "②")}", FormKind.Position);
                        var opts = new List<string>();
                        for (int i = 1; i <= 10; i++) { opts.Add($"{i}번째 카드"); row.values.Add((i - 1).ToString()); }
                        row.dropdown.AddOptions(opts);
                        row.dropdown.value = k;
                        row.dropdown.RefreshShownValue();
                    }
                    break;
                }
                case "queen":
                {
                    var mode = UseRow(n++, "교환 방식", FormKind.Mode);
                    mode.dropdown.AddOptions(new List<string> { "공개 무덤에서 1장 가져오기", "비공개 무덤에서 2장 가져오기 (무작위)" });
                    mode.values.Add("public"); mode.values.Add("secret");
                    mode.dropdown.RefreshShownValue();
                    FillHand(UseRow(n++, "버릴 손패 ①", FormKind.Hand), state);
                    var second = UseRow(n++, "버릴 손패 ②", FormKind.Hand);
                    FillHand(second, state);
                    // Defaulting both to the first card would always be invalid.
                    if (second.dropdown.options.Count > 1) { second.dropdown.value = 1; second.dropdown.RefreshShownValue(); }
                    FillGrave(UseRow(n++, "가져올 공개 무덤 카드 (공개 방식일 때)", FormKind.HandOrGrave), state, false);
                    break;
                }
                case "mercenary":
                    // Each slot may be filled from the hand or bought out of the
                    // public grave; a grave pick becomes a swap automatically.
                    for (int k = 0; k < 4; k++)
                    {
                        var row = UseRow(n++, $"목표 카드 {k + 1}", FormKind.HandOrGrave);
                        var names = new List<string>();
                        var hand = state.you?.hand ?? new CardData[0];
                        for (int i = 0; i < hand.Length; i++) { names.Add($"{i + 1}. {CardLabel(hand[i])}"); row.values.Add(hand[i].id); }
                        foreach (var p in state.players)
                        {
                            if (p.publicDiscard == null) continue;
                            foreach (var c in p.publicDiscard) { names.Add($"[무덤·{p.name}] {CardLabel(c)}"); row.values.Add("grave:" + c.id); }
                        }
                        row.dropdown.AddOptions(names);
                        if (k < hand.Length) row.dropdown.value = k;
                        row.dropdown.RefreshShownValue();
                    }
                    break;
            }
            HideRowsFrom(n);
        }

        private void RefillPositions(FormRow targetRow, FormRow posRow, GameState state)
        {
            int idx = targetRow.dropdown.value;
            string token = (idx >= 0 && idx < targetRow.values.Count) ? targetRow.values[idx] : null;
            int handCount = 0;
            if (token != null && int.TryParse(token, out int playerIndex))
                foreach (var p in state.players) if (p.index == playerIndex) handCount = p.handCount;

            int keep = posRow.dropdown.value;
            posRow.values.Clear();
            posRow.dropdown.ClearOptions();
            var opts = new List<string>();
            for (int i = 0; i < Mathf.Max(1, handCount); i++) { opts.Add($"{i + 1}번째 카드"); posRow.values.Add(i.ToString()); }
            posRow.dropdown.AddOptions(opts);
            posRow.dropdown.value = Mathf.Clamp(keep, 0, Mathf.Max(0, opts.Count - 1));
            posRow.dropdown.RefreshShownValue();
        }

        private void BindPositionsTo(FormRow targetRow, FormRow posRow, GameState state)
        {
            targetRow.dropdown.onValueChanged.RemoveAllListeners();
            targetRow.dropdown.onValueChanged.AddListener((_) => RefillPositions(targetRow, posRow, state));
            RefillPositions(targetRow, posRow, state);
        }

        private string RowValue(int index)
        {
            if (index < 0 || index >= _formRows.Count) return null;
            var row = _formRows[index];
            if (!row.go.activeSelf) return null;
            int v = row.dropdown.value;
            return (v >= 0 && v < row.values.Count) ? row.values[v] : null;
        }

        // Builds the exact payload the server expects for this role. Returns
        // null and warns when the choices are not usable.
        private string BuildActionPayload(string role)
        {
            switch (role)
            {
                case "hunter":
                    return $"{{\"player\":{RowValue(0)},\"role\":\"{RowValue(1)}\"}}";
                case "chancellor":
                    return $"{{\"role\":\"{RowValue(0)}\"}}";
                case "courtesan":
                    return $"{{\"player\":{RowValue(0)}}}";
                case "thief":
                {
                    string ta = RowValue(0), pa = RowValue(1), tb = RowValue(2), pb = RowValue(3);
                    if (ta == null || pa == null || tb == null || pb == null) { ShowWarning("훔칠 대상과 위치를 모두 고르세요."); return null; }
                    if (ta == tb && pa == pb) { ShowWarning("같은 카드를 두 번 고를 수 없습니다."); return null; }
                    return $"{{\"picks\":[{{\"player\":{ta},\"index\":{pa}}},{{\"player\":{tb},\"index\":{pb}}}]}}";
                }
                case "king":
                {
                    string a = RowValue(1), b = RowValue(2);
                    if (a == b) { ShowWarning("서로 다른 두 위치를 골라야 합니다."); return null; }
                    return $"{{\"player\":{RowValue(0)},\"indices\":[{a},{b}]}}";
                }
                case "queen":
                {
                    string mode = RowValue(0), h1 = RowValue(1), h2 = RowValue(2), g = RowValue(3);
                    if (string.IsNullOrEmpty(h1) || string.IsNullOrEmpty(h2)) { ShowWarning("버릴 손패 2장을 골라야 합니다."); return null; }
                    if (h1 == h2) { ShowWarning("서로 다른 카드를 버려야 합니다."); return null; }
                    if (mode == "public")
                    {
                        if (string.IsNullOrEmpty(g)) { ShowWarning("가져올 공개 무덤 카드를 골라야 합니다."); return null; }
                        string gid = g.Substring("grave:".Length);
                        return $"{{\"type\":\"public\",\"swaps\":[{{\"ownId\":\"{h1}\",\"graveId\":\"{gid}\"}},{{\"ownId\":\"{h2}\"}}]}}";
                    }
                    return $"{{\"type\":\"secret\",\"swaps\":[{{\"ownId\":\"{h1}\"}},{{\"ownId\":\"{h2}\"}}]}}";
                }
                case "mercenary":
                {
                    var picks = new List<string>();
                    for (int i = 0; i < 4; i++) picks.Add(RowValue(i));
                    if (picks.Exists(string.IsNullOrEmpty)) { ShowWarning("목표 카드 4장을 모두 골라야 합니다."); return null; }

                    var handIds = new List<string>();
                    var graveIds = new List<string>();
                    foreach (var v in picks)
                    {
                        if (v.StartsWith("grave:")) graveIds.Add(v.Substring("grave:".Length));
                        else handIds.Add(v);
                    }
                    if (new HashSet<string>(handIds).Count != handIds.Count
                        || new HashSet<string>(graveIds).Count != graveIds.Count)
                    { ShowWarning("같은 카드를 두 번 고를 수 없습니다."); return null; }
                    if (graveIds.Count > 2) { ShowWarning("무덤에서는 최대 2장까지만 가져올 수 있습니다."); return null; }

                    // The server replaces a group card with the grave card it was
                    // swapped for, so each grave pick needs a spare hand card to
                    // trade away - one that is not already a target.
                    // Prefer trading away a card whose type is not one the player
                    // is collecting, so a grave buy does not cost them a match.
                    var wanted = new HashSet<string>();
                    foreach (var c in _latestState.you.hand)
                        if (handIds.Contains(c.id)) wanted.Add(c.type);
                    var spares = new List<string>();
                    foreach (var c in _latestState.you.hand)
                        if (!handIds.Contains(c.id) && !wanted.Contains(c.type)) spares.Add(c.id);
                    foreach (var c in _latestState.you.hand)
                        if (!handIds.Contains(c.id) && wanted.Contains(c.type)) spares.Add(c.id);
                    if (spares.Count < graveIds.Count)
                    { ShowWarning("무덤 카드와 바꿀 여분 손패가 부족합니다."); return null; }

                    var group = new List<string>(handIds);
                    var swaps = new List<string>();
                    for (int i = 0; i < graveIds.Count; i++)
                    {
                        group.Add(spares[i]);
                        swaps.Add($"{{\"ownId\":\"{spares[i]}\",\"graveId\":\"{graveIds[i]}\"}}");
                    }
                    if (group.Count != 4) { ShowWarning("목표 카드는 정확히 4장이어야 합니다."); return null; }

                    var quoted = new List<string>();
                    foreach (var id in group) quoted.Add($"\"{id}\"");
                    return $"{{\"group\":[{string.Join(",", quoted.ToArray())}],\"swaps\":[{string.Join(",", swaps.ToArray())}]}}";
                }
            }
            return "{}";
        }

        private void OnActionConfirmClicked()
        {
            if (_latestState == null) return;
            if (_latestState.phase == "accusation") { OnAccuseConfirmClicked(); return; }
            if (_latestState.action == null || targetDropdown1 == null || roleDropdown1 == null) return;
            string role = _latestState.action.role;

            int targetIdx = targetDropdown1.value;
            var otherPlayers = new List<PublicPlayer>();
            foreach (var p in _latestState.players) if (p.index != _latestState.you.index) otherPlayers.Add(p);
            int resolvedPlayer = (targetIdx < otherPlayers.Count) ? otherPlayers[targetIdx].index : 0;

            int roleIdx = roleDropdown1.value;
            string resolvedRole = (roleIdx < _latestState.selected.Length) ? _latestState.selected[roleIdx] : "king";

            string payload = BuildActionPayload(role);
            if (payload == null) return;   // the builder already warned the player
            Debug.Log($"[RealmGameUI] ACTION {role} -> {payload}");
            RealmNetworkManager.Instance.SendMessageRaw("ACTION", payload);
        }

        [Serializable]
        private class Guess { public int player; public string role; }
        [Serializable]
        private class AccusePayload { public Guess[] guesses; }
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

            if (resultsRankingsText == null) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<b>★ 게임 종료 · 최종 결산</b>");

            if (state.results == null || state.results.Length == 0)
            {
                sb.AppendLine("\n<color=#9FB0C9>결과를 불러오는 중...</color>");
                resultsRankingsText.text = sb.ToString();
                return;
            }

            // The server already sorts by total descending.
            int rank = 0;
            int shownRank = 0;
            int lastTotal = int.MinValue;
            foreach (var r in state.results)
            {
                rank++;
                if (r.total != lastTotal) { shownRank = rank; lastTotal = r.total; }

                string roleName = RealmCard.RoleInfo.TryGetValue(r.role ?? "", out var inf) ? inf.name : r.role;
                bool isMe = state.you != null && r.index == state.you.index;
                string who = isMe ? $"<color=#FFE08B>{r.name} (나)</color>" : r.name;
                string mark = r.success ? "<color=#7FD98A>성공</color>" : "<color=#FF8B8B>실패</color>";

                sb.AppendLine();
                sb.AppendLine($"<b>{shownRank}위  {who}</b>  <b>{r.total}점</b>");
                sb.AppendLine($"<size=85%><color=#9FB0C9>{roleName} · {mark} · 손패 {r.@base}점" +
                              (r.bonus != 0 ? $" + 직업 {r.bonus}점" : "") +
                              (r.accusePoints != 0 ? $" + 지목 {(r.accusePoints > 0 ? "+" : "")}{r.accusePoints}점" : "") +
                              "</color></size>");
                if (!string.IsNullOrEmpty(r.detail))
                    sb.AppendLine($"<size=80%><color=#8193AC>{r.detail}</color></size>");
            }

            resultsRankingsText.text = sb.ToString();
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
