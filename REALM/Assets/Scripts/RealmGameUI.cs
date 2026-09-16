using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Realm
{
    public class RealmGameUI : MonoBehaviour
    {
        [Header("Card Prefab")]
        [SerializeField] public GameObject cardPrefab;

        [Header("Main Play Area (Left Panel)")]
        [SerializeField] public Text roundEyebrowText;
        [SerializeField] public Text turnHeadingText;
        [SerializeField] public Transform playersStripContainer;
        [SerializeField] public Text secretRoleText;
        [SerializeField] public Text lastRollText;
        [SerializeField] public Text turnPromptText;

        [Header("Card Sockets & Hand (10 Slots)")]
        [SerializeField] public Transform cardSocketsContainer;
        [SerializeField] public Text selectionCountText;
        [SerializeField] public Button discardConfirmButton;

        [Header("Public Grave Area (Right Panel)")]
        [SerializeField] public Transform graveListContainer;

        [Header("Action Phase Modal")]
        [SerializeField] public GameObject actionPanel;
        [SerializeField] public Text actionTitleText;
        [SerializeField] public Dropdown targetDropdown1;
        [SerializeField] public Dropdown roleDropdown1;
        [SerializeField] public Button actionConfirmButton;

        [Header("Discussion Phase Modal")]
        [SerializeField] public GameObject discussionPanel;
        [SerializeField] public Text discussionTimerText;
        [SerializeField] public Text skipVotesText;
        [SerializeField] public Button skipDiscussionButton;
        [SerializeField] public Text chatLogText;
        [SerializeField] public InputField chatInputField;
        [SerializeField] public Button chatSendButton;

        [Header("Results Phase Modal")]
        [SerializeField] public GameObject resultsPanel;
        [SerializeField] public Text resultsRankingsText;
        [SerializeField] public Button returnLobbyButton;

        private readonly List<RealmCard> _spawnedCards = new List<RealmCard>();
        private readonly List<string> _chosenCardIds = new List<string>();
        private readonly List<Transform> _socketSlots = new List<Transform>();
        private GameState _latestState;

        private void Awake()
        {
            AutoWireReferences();

            if (discardConfirmButton != null) discardConfirmButton.onClick.AddListener(OnDiscardConfirmClicked);
            if (actionConfirmButton != null) actionConfirmButton.onClick.AddListener(OnActionConfirmClicked);
            if (skipDiscussionButton != null) skipDiscussionButton.onClick.AddListener(OnSkipDiscussionClicked);
            if (chatSendButton != null) chatSendButton.onClick.AddListener(OnSendChatClicked);
            if (returnLobbyButton != null) returnLobbyButton.onClick.AddListener(OnReturnLobbyClicked);

            CacheSockets();
        }

        private void Start()
        {
            if (RealmNetworkManager.Instance != null)
            {
                RealmNetworkManager.Instance.OnStateUpdated += OnStateUpdated;
            }
        }

        private void OnDestroy()
        {
            if (RealmNetworkManager.Instance != null)
            {
                RealmNetworkManager.Instance.OnStateUpdated -= OnStateUpdated;
            }
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
            var font = RealmCard.GetNotoFont();

            if (roundEyebrowText == null) roundEyebrowText = transform.Find("MainPlayArea/TopHeader/RoundEyebrowText")?.GetComponent<Text>();
            if (turnHeadingText == null) turnHeadingText = transform.Find("MainPlayArea/TopHeader/TurnHeadingText")?.GetComponent<Text>();
            if (playersStripContainer == null) playersStripContainer = transform.Find("MainPlayArea/PlayersStrip");

            if (secretRoleText == null) secretRoleText = transform.Find("MainPlayArea/Notices/SecretRoleText")?.GetComponent<Text>();
            if (lastRollText == null) lastRollText = transform.Find("MainPlayArea/Notices/LastRollText")?.GetComponent<Text>();
            if (turnPromptText == null) turnPromptText = transform.Find("MainPlayArea/Notices/TurnPromptText")?.GetComponent<Text>();

            if (cardSocketsContainer == null) cardSocketsContainer = transform.Find("MainPlayArea/CardSocketsArea");
            if (selectionCountText == null) selectionCountText = transform.Find("MainPlayArea/ControlsLine/SelectionCountText")?.GetComponent<Text>();
            if (discardConfirmButton == null) discardConfirmButton = transform.Find("MainPlayArea/ControlsLine/DiscardConfirmButton")?.GetComponent<Button>();

            if (graveListContainer == null) graveListContainer = transform.Find("PublicGraveArea/GraveScroll/GraveListContainer") ?? transform.Find("PublicGraveArea/GraveScroll");

            if (actionPanel == null) actionPanel = transform.Find("ActionPanel")?.gameObject;
            if (actionPanel != null)
            {
                if (actionTitleText == null) actionTitleText = actionPanel.transform.Find("ActionTitleText")?.GetComponent<Text>();
                if (targetDropdown1 == null) targetDropdown1 = actionPanel.transform.Find("TargetDropdown")?.GetComponent<Dropdown>();
                if (roleDropdown1 == null) roleDropdown1 = actionPanel.transform.Find("RoleDropdown")?.GetComponent<Dropdown>();
                if (actionConfirmButton == null) actionConfirmButton = actionPanel.transform.Find("ActionConfirmButton")?.GetComponent<Button>();
            }

            if (discussionPanel == null) discussionPanel = transform.Find("DiscussionPanel")?.gameObject;
            if (discussionPanel != null)
            {
                if (discussionTimerText == null) discussionTimerText = discussionPanel.transform.Find("DiscTimerText")?.GetComponent<Text>();
                if (skipVotesText == null) skipVotesText = discussionPanel.transform.Find("SkipVotesText")?.GetComponent<Text>();
                if (skipDiscussionButton == null) skipDiscussionButton = discussionPanel.transform.Find("SkipButton")?.GetComponent<Button>();
                if (chatLogText == null) chatLogText = discussionPanel.transform.Find("ChatBox/ChatLogText")?.GetComponent<Text>();
                if (chatInputField == null) chatInputField = discussionPanel.transform.Find("ChatInput")?.GetComponent<InputField>();
                if (chatSendButton == null) chatSendButton = discussionPanel.transform.Find("SendChatButton")?.GetComponent<Button>();
            }

            if (resultsPanel == null) resultsPanel = transform.Find("ResultsPanel")?.gameObject;
            if (resultsPanel != null)
            {
                if (resultsRankingsText == null) resultsRankingsText = resultsPanel.transform.Find("RankingsText")?.GetComponent<Text>();
                if (returnLobbyButton == null) returnLobbyButton = resultsPanel.transform.Find("ReturnLobbyButton")?.GetComponent<Button>();
            }

            // Apply Noto font to all texts
            foreach (var t in GetComponentsInChildren<Text>(true))
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

            UpdateHeaderAndNotices(state);
            UpdatePlayersStrip(state);
            UpdateHandAndSockets(state);
            UpdatePublicGrave(state);
            UpdateActionPhase(state);
            UpdateDiscussionPhase(state);
            UpdateResultsPhase(state);
        }

        private void UpdateHeaderAndNotices(GameState state)
        {
            bool isFinal = state.round == 4;
            if (roundEyebrowText != null)
                roundEyebrowText.text = $"방 코드: {state.roomCode}  ·  {state.round}라운드 {(isFinal ? "마지막 비공개 버리기" : "공개 버리기")}";

            var current = (state.players != null && state.turn >= 0 && state.turn < state.players.Length) ? state.players[state.turn] : null;
            bool isMyTurn = state.you != null && state.you.canDiscard;

            if (turnHeadingText != null)
            {
                if (isMyTurn)
                {
                    turnHeadingText.text = "★ 당신의 차례입니다.";
                    turnHeadingText.color = new Color(1f, 0.90f, 0.45f);
                }
                else
                {
                    turnHeadingText.text = (current != null) ? $"{current.name} 플레이어의 차례입니다." : "진행 중...";
                    turnHeadingText.color = Color.white;
                }
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
                if (state.lastRoll > 0)
                {
                    string pName = (current != null) ? current.name : "플레이어";
                    lastRollText.text = $"최근 D6 · <b>{pName}</b> → <b>{state.lastRoll}</b>";
                }
                else
                {
                    lastRollText.text = "";
                }
            }

            if (turnPromptText != null)
            {
                if (state.required <= 0)
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

            var font = RealmCard.GetNotoFont();

            foreach (var p in state.players)
            {
                var pillGo = new GameObject($"PlayerPill_{p.index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                pillGo.transform.SetParent(playersStripContainer, false);
                var pr = pillGo.GetComponent<RectTransform>();
                pr.sizeDelta = new Vector2(160f, 44f);

                bool isCurrent = state.phase == "round" && p.index == state.turn;
                bool isMe = state.you != null && p.index == state.you.index;

                var img = pillGo.GetComponent<Image>();
                img.color = isCurrent ? new Color(0.18f, 0.45f, 0.65f, 0.95f) : (isMe ? new Color(0.28f, 0.25f, 0.15f, 0.9f) : new Color(0.06f, 0.15f, 0.25f, 0.85f));

                var outl = pillGo.AddComponent<Outline>();
                outl.effectColor = isCurrent ? new Color(1f, 0.85f, 0.4f) : new Color(0.25f, 0.38f, 0.5f);
                outl.effectDistance = isCurrent ? new Vector2(2f, 2f) : new Vector2(1f, 1f);

                var txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                txtGo.transform.SetParent(pillGo.transform, false);
                var tr = txtGo.GetComponent<RectTransform>();
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.offsetMin = new Vector2(8f, 2f);
                tr.offsetMax = new Vector2(-8f, -2f);

                var txt = txtGo.GetComponent<Text>();
                txt.font = font;
                txt.fontSize = 12;
                txt.alignment = TextAnchor.MiddleCenter;
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
            cardSocketsContainer.gameObject.SetActive(isRound);
            if (selectionCountText != null) selectionCountText.gameObject.SetActive(isRound);
            if (discardConfirmButton != null) discardConfirmButton.gameObject.SetActive(isRound);

            if (!isRound) return;

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
                    rc.Setup(cData.id, cData.type, state.you.canDiscard);
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

            if (selectionCountText != null)
                selectionCountText.text = req <= 0 ? $"{count}장 선택" : $"{count}장 선택 / {req}장 필수";

            if (discardConfirmButton != null)
                discardConfirmButton.interactable = _latestState.you.canDiscard && valid;
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

            var font = RealmCard.GetNotoFont();

            foreach (var p in state.players)
            {
                // Player row in grave
                var rowGo = new GameObject($"GraveRow_{p.name}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                rowGo.transform.SetParent(graveListContainer, false);
                var rowRect = rowGo.GetComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(580f, 155f);

                var rowImg = rowGo.GetComponent<Image>();
                rowImg.color = new Color(0.04f, 0.10f, 0.18f, 0.75f);
                var rowOutl = rowGo.AddComponent<Outline>();
                rowOutl.effectColor = new Color(0.2f, 0.35f, 0.5f, 0.4f);

                // Player name label
                var nameGo = new GameObject("PlayerName", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                nameGo.transform.SetParent(rowGo.transform, false);
                var nr = nameGo.GetComponent<RectTransform>();
                nr.anchorMin = new Vector2(0.02f, 0.78f);
                nr.anchorMax = new Vector2(0.98f, 0.98f);
                nr.offsetMin = Vector2.zero;
                nr.offsetMax = Vector2.zero;

                var nt = nameGo.GetComponent<Text>();
                nt.font = font;
                nt.fontSize = 14;
                nt.fontStyle = FontStyle.Bold;
                nt.color = new Color(1f, 0.90f, 0.65f);
                nt.text = $"<b>{p.name}</b>";

                // Piles container
                var pilesGo = new GameObject("PilesContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                pilesGo.transform.SetParent(rowGo.transform, false);
                var pr = pilesGo.GetComponent<RectTransform>();
                pr.anchorMin = new Vector2(0.02f, 0.05f);
                pr.anchorMax = new Vector2(0.98f, 0.75f);
                pr.offsetMin = Vector2.zero;
                pr.offsetMax = Vector2.zero;

                var hlg = pilesGo.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 25f;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;

                bool hasAnyDiscard = false;

                // 1R, 2R, 3R
                for (int r = 1; r <= 3; r++)
                {
                    int roundNum = r;
                    var roundCards = (p.publicDiscard != null) ? p.publicDiscard.Where(c => c.round == roundNum || (c.round == 0 && roundNum == 1)).ToArray() : new CardData[0];
                    if (roundCards.Length == 0) continue;

                    hasAnyDiscard = true;
                    CreateDiscardPileUI(pilesGo.transform, $"{roundNum}R", roundCards, font);
                }

                // 4R Final Discards (Secret)
                if (p.finalDiscardCount > 0)
                {
                    hasAnyDiscard = true;
                    CreateSecretPileUI(pilesGo.transform, "4R", p.finalDiscardCount, font);
                }

                if (!hasAnyDiscard)
                {
                    var emptyTxtGo = new GameObject("EmptyText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                    emptyTxtGo.transform.SetParent(pilesGo.transform, false);
                    var et = emptyTxtGo.GetComponent<Text>();
                    et.font = font;
                    et.fontSize = 13;
                    et.color = new Color(0.5f, 0.6f, 0.7f);
                    et.text = "(아직 버린 카드 없음)";
                }
            }
        }

        private void CreateDiscardPileUI(Transform parent, string roundLabel, CardData[] cards, Font font)
        {
            var pileGo = new GameObject($"Pile_{roundLabel}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(DiscardPileHover));
            pileGo.transform.SetParent(parent, false);
            var rect = pileGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 130f);

            var img = pileGo.GetComponent<Image>();
            img.color = new Color(0.08f, 0.16f, 0.25f, 0.4f);

            // Round Badge (Top Left)
            var badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeGo.transform.SetParent(pileGo.transform, false);
            var br = badgeGo.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0f, 0.8f);
            br.anchorMax = new Vector2(0.4f, 1f);
            br.offsetMin = Vector2.zero;
            br.offsetMax = Vector2.zero;
            badgeGo.GetComponent<Image>().color = new Color(0.18f, 0.40f, 0.60f, 0.9f);

            var btGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            btGo.transform.SetParent(badgeGo.transform, false);
            var btr = btGo.GetComponent<RectTransform>();
            btr.anchorMin = Vector2.zero;
            btr.anchorMax = Vector2.one;
            btr.offsetMin = Vector2.zero;
            btr.offsetMax = Vector2.zero;
            var bt = btGo.GetComponent<Text>();
            bt.font = font;
            bt.fontSize = 11;
            bt.fontStyle = FontStyle.Bold;
            bt.alignment = TextAnchor.MiddleCenter;
            bt.color = Color.white;
            bt.text = roundLabel;

            // Spawn mini cards in pile
            foreach (var cData in cards)
            {
                var cardGo = InstantiateCardPrefab(pileGo.transform);
                var rc = cardGo.GetComponent<RealmCard>();
                rc.Setup(cData.id, cData.type, false, mini: true);
            }

            var hover = pileGo.GetComponent<DiscardPileHover>();
            hover.RefreshStack();
        }

        private void CreateSecretPileUI(Transform parent, string roundLabel, int count, Font font)
        {
            var pileGo = new GameObject($"SecretPile_{roundLabel}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(DiscardPileHover));
            pileGo.transform.SetParent(parent, false);
            var rect = pileGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100f, 130f);

            var img = pileGo.GetComponent<Image>();
            img.color = new Color(0.15f, 0.12f, 0.20f, 0.4f);

            // Badge
            var badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeGo.transform.SetParent(pileGo.transform, false);
            var br = badgeGo.GetComponent<RectTransform>();
            br.anchorMin = new Vector2(0f, 0.8f);
            br.anchorMax = new Vector2(0.55f, 1f);
            br.offsetMin = Vector2.zero;
            br.offsetMax = Vector2.zero;
            badgeGo.GetComponent<Image>().color = new Color(0.55f, 0.20f, 0.20f, 0.9f);

            var btGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            btGo.transform.SetParent(badgeGo.transform, false);
            var btr = btGo.GetComponent<RectTransform>();
            btr.anchorMin = Vector2.zero;
            btr.anchorMax = Vector2.one;
            btr.offsetMin = Vector2.zero;
            btr.offsetMax = Vector2.zero;
            var bt = btGo.GetComponent<Text>();
            bt.font = font;
            bt.fontSize = 11;
            bt.fontStyle = FontStyle.Bold;
            bt.alignment = TextAnchor.MiddleCenter;
            bt.color = Color.white;
            bt.text = $"{roundLabel} 비공개";

            for (int i = 0; i < count; i++)
            {
                var cardGo = InstantiateCardPrefab(pileGo.transform);
                var rc = cardGo.GetComponent<RealmCard>();
                rc.Setup("", "", false, mini: true); // Face-down
            }

            var hover = pileGo.GetComponent<DiscardPileHover>();
            hover.RefreshStack();
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
                sb.AppendLine("<b>🏆 게임 종료 · 최종 결산</b>\n");

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

            var fallbackGo = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline), typeof(RealmCard));
            fallbackGo.transform.SetParent(parent, false);
            return fallbackGo;
        }
    }
}
