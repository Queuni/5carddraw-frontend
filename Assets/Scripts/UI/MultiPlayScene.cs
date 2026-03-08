using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class MultiPlayScene : MonoBehaviour
{
    private const float RoomPollIntervalSeconds = 2f;
    [Header("Player Setup")]
    public CardAnimator cardAnimator;
    public TableAnimator tableAnimator;
    
    public Player[] players = new Player[4]; // 4 players

    // Exchange phase: If WebGL shows "INVALID_ENUM: getInternalformatParameter" and crash, it is a known Unity engine bug
    // (UUM-93245). Workarounds: try another browser (e.g. Firefox), or in Player Settings > WebGL disable Automatic Graphics API and use WebGL 1.0.
    [Header("UI Buttons - Exchange Phase")]
    public Button exchangeButton;
    public Button skipExchangeButton;

    [Header("UI Buttons - Betting Phase")]
    public Button bet5Button;
    public Button bet10Button;
    public Button bet25Button;
    public Button callButton;
    public Button raiseButton;
    public Button foldButton;
    public Button checkButton;

    [Header("UI Display")]
    public TextMeshProUGUI potText;
    public GamePhaseBar gamePhaseBar;
    public RoundFinished roundFinishedDialog;
    public Button backButton;

    private GameState gameState;
    private GameFlowManager gameFlowManager;
    private Player localPlayer;
    private int localPlayerIndex = -1;
    private string currentRoomId;
    private bool localReadySent;
    private Coroutine roomPollCoroutine;
    private string lastWaitingMessage;
    private GamePhase lastPhase = GamePhase.None;
    private bool isWaitingPhase;
    private UserSession session;
    private int localSeatIndexServer;
    private string lastShownWinnerId;
    private int lastShownWinnerHandValue;
    private bool isStartCountdownActive;
    private bool hasDealtLocalHand;
    private int lastTurnPlayerIndex = -1;
    private GamePhase lastTurnPhase = GamePhase.None;
    private readonly Dictionary<string, Player> playerByUserId = new Dictionary<string, Player>();
    private bool hasSentContinue;
    private bool pendingFoldConfirm;
    private int[] _pendingExchangeIndices;
    private List<Card> _pendingExchangeCards;
    private string _lastBetInfoMessage;

    private void Awake()
    {
        if (roundFinishedDialog != null)
        {
            roundFinishedDialog.hideDialog();
            if (roundFinishedDialog.continueButton != null)
            {
                roundFinishedDialog.continueButton.onClick.AddListener(OnContinueClicked);
            }
            if (roundFinishedDialog.quitButton != null)
            {
                roundFinishedDialog.quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnQuitClicked);
        }
    }

    private void Start()
    {
        AppBootstrap.EnsureManagers();
        Spinner.Instance.Hide();
        GameSettings.Load();

        session = AuthManager.Instance != null ? AuthManager.Instance.GetCurrentSession() : null;
        if (session == null)
        {
            AlertBar.Instance.ShowMessage("Please sign in");
            Utils.LoadScene("LoginScene");
            return;
        }

        if (MultiplayerManager.Instance == null)
        {
            AlertBar.Instance.ShowMessage("Multiplayer not ready");
            Utils.LoadScene("RoomScene");
            return;
        }

        currentRoomId = MultiplayerManager.Instance.CurrentRoomId;
        if (string.IsNullOrEmpty(currentRoomId))
        {
            AlertBar.Instance.ShowMessage("Room not found");
            Utils.LoadScene("RoomScene");
            return;
        }

        EnsureGameState();
        SetupUIButtons();
        TrySyncFromCurrentRoom();
    }

    private void OnEnable()
    {
        SubscribeEvents();
        StartRoomPolling();
    }

    private void SubscribeEvents()
    {
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.OnGameStateUpdated += HandleGameStateUpdated;
            MultiplayerManager.Instance.OnRoomDeleted += HandleRoomDeleted;
            MultiplayerManager.Instance.OnError += HandleMultiplayerError;
        }

        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnCardsDealt += HandleCardsDealt;
            WebSocketManager.Instance.OnCardsDealtPublic += HandleCardsDealtPublic;
            WebSocketManager.Instance.OnCardsExchanged += HandleCardsExchanged;
            WebSocketManager.Instance.OnGameStarting += HandleGameStarting;
            WebSocketManager.Instance.OnGameStarted += HandleGameStarted;
            WebSocketManager.Instance.OnPlayerAction += HandlePlayerActionEvent;
            WebSocketManager.Instance.OnPlayerReadyChanged += HandlePlayerReadyChanged;
            WebSocketManager.Instance.OnPlayerExchanged += HandlePlayerExchanged;
            WebSocketManager.Instance.OnPlayerSkippedExchange += HandlePlayerSkippedExchange;
            WebSocketManager.Instance.OnShowdownReveal += HandleShowdownReveal;
            WebSocketManager.Instance.OnShowdownHands += HandleShowdownHands;
            WebSocketManager.Instance.OnShowdownResult += HandleShowdownResult;
            WebSocketManager.Instance.OnRoundContinueState += HandleRoundContinueState;
        }
    }

    private void UnsubscribeEvents()
    {
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.OnGameStateUpdated -= HandleGameStateUpdated;
            MultiplayerManager.Instance.OnRoomDeleted -= HandleRoomDeleted;
            MultiplayerManager.Instance.OnError -= HandleMultiplayerError;
        }

        if (WebSocketManager.Instance != null)
        {
            WebSocketManager.Instance.OnCardsDealt -= HandleCardsDealt;
            WebSocketManager.Instance.OnCardsDealtPublic -= HandleCardsDealtPublic;
            WebSocketManager.Instance.OnCardsExchanged -= HandleCardsExchanged;
            WebSocketManager.Instance.OnGameStarting -= HandleGameStarting;
            WebSocketManager.Instance.OnGameStarted -= HandleGameStarted;
            WebSocketManager.Instance.OnPlayerAction -= HandlePlayerActionEvent;
            WebSocketManager.Instance.OnPlayerReadyChanged -= HandlePlayerReadyChanged;
            WebSocketManager.Instance.OnPlayerExchanged -= HandlePlayerExchanged;
            WebSocketManager.Instance.OnPlayerSkippedExchange -= HandlePlayerSkippedExchange;
            WebSocketManager.Instance.OnShowdownReveal -= HandleShowdownReveal;
            WebSocketManager.Instance.OnShowdownHands -= HandleShowdownHands;
            WebSocketManager.Instance.OnShowdownResult -= HandleShowdownResult;
            WebSocketManager.Instance.OnRoundContinueState -= HandleRoundContinueState;
        }
    }

    private void EnsureGameState()
    {
        if (gameState == null)
        {
            gameState = new GameState(GameMode.MultiMode);
        }
    }

    private void SetupUIButtons()
    {
        if (exchangeButton != null)
        {
            exchangeButton.onClick.AddListener(OnExchangeClicked);
        }
        if (skipExchangeButton != null)
        {
            skipExchangeButton.onClick.AddListener(OnSkipExchangeClicked);
        }
        if (bet5Button != null)
        {
            bet5Button.onClick.AddListener(() => OnBetClicked(Constants.BET_OPTION_1));
        }
        if (bet10Button != null)
        {
            bet10Button.onClick.AddListener(() => OnBetClicked(Constants.BET_OPTION_2));
        }
        if (bet25Button != null)
        {
            bet25Button.onClick.AddListener(() => OnBetClicked(Constants.BET_OPTION_3));
        }
        if (callButton != null)
        {
            callButton.onClick.AddListener(OnCallClicked);
        }
        if (raiseButton != null)
        {
            raiseButton.onClick.AddListener(OnRaiseClicked);
        }
        if (foldButton != null)
        {
            foldButton.onClick.AddListener(OnFoldClicked);
        }
        if (checkButton != null)
        {
            checkButton.onClick.AddListener(OnCheckClicked);
        }

        UpdateButtonStates();
    }

    private void StartRoomPolling()
    {
        if (roomPollCoroutine != null)
        {
            StopCoroutine(roomPollCoroutine);
        }
        roomPollCoroutine = StartCoroutine(PollRoomStateLoop());
    }

    private void StopRoomPolling()
    {
        if (roomPollCoroutine != null)
        {
            StopCoroutine(roomPollCoroutine);
            roomPollCoroutine = null;
        }
    }

    private IEnumerator PollRoomStateLoop()
    {
        while (true)
        {
            yield return RefreshRoomSnapshot();
            yield return new WaitForSeconds(RoomPollIntervalSeconds);
        }
    }

    private IEnumerator RefreshRoomSnapshot()
    {
        if (APIService.Instance == null || string.IsNullOrEmpty(currentRoomId))
        {
            yield break;
        }

        bool completed = false;
        RoomInfoResponse response = null;
        string errorMessage = null;

        APIService.Instance.Get<RoomInfoResponse>(
            $"/multiplayer/rooms/{currentRoomId}",
            (data) =>
            {
                response = data;
                completed = true;
            },
            (error) =>
            {
                errorMessage = error;
                completed = true;
            },
            false
        );

        while (!completed)
        {
            yield return null;
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            Debug.LogWarning($"[MultiPlayScene] Failed to refresh room: {errorMessage}");
            yield break;
        }

        if (response != null && MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.UpdateRoomSnapshot(MapRoomSnapshot(response));
        }
    }

    private RoomSnapshot MapRoomSnapshot(RoomInfoResponse response)
    {
        var room = new RoomSnapshot
        {
            roomId = response.roomId,
            hostId = response.hostId,
            maxPlayers = response.maxPlayers,
            currentPhase = response.currentPhase,
            pot = response.pot,
            currentBet = response.currentBet,
            anteAmount = response.anteAmount,
            currentPlayerIndex = response.currentPlayerIndex,
            isActive = response.isActive,
            lastWinnerId = response.lastWinnerId,
            lastWinnerHandValue = response.lastWinnerHandValue,
            players = new List<RoomPlayerSnapshot>()
        };

        if (response.players != null)
        {
            foreach (RoomInfoPlayer player in response.players)
            {
                room.players.Add(new RoomPlayerSnapshot
                {
                    userId = player.userId,
                    username = player.username,
                    avatarIndex = player.avatarIndex,
                    seatIndex = player.seatIndex,
                    tokenAmount = player.tokenAmount,
                    isReady = player.isReady,
                    isConnected = true
                });
            }
        }

        return room;
    }

    private void TrySyncFromCurrentRoom()
    {
        if (MultiplayerManager.Instance != null && MultiplayerManager.Instance.CurrentRoom != null)
        {
            SyncRoomState(MultiplayerManager.Instance.CurrentRoom);
        }
    }

    private void HandleGameStateUpdated(GameState state)
    {
        if (state != null)
        {
            gameState = state;
        }

        if (MultiplayerManager.Instance != null)
        {
            SyncRoomState(MultiplayerManager.Instance.CurrentRoom);
        }
    }

    private void SyncRoomState(RoomSnapshot room)
    {
        if (room == null)
        {
            return;
        }

        EnsureGameState();
        currentRoomId = room.roomId;

        isWaitingPhase = string.Equals(room.currentPhase, "Waiting", StringComparison.OrdinalIgnoreCase);
        if (!isWaitingPhase)
        {
            isStartCountdownActive = false;
        }
        if (!isWaitingPhase && TryParseRoomPhase(room.currentPhase, out GamePhase parsedPhase))
        {
            gameState.currentPhase = parsedPhase;
        }
        else if (isWaitingPhase)
        {
            gameState.currentPhase = GamePhase.None;
        }

        gameState.pot = room.pot;
        gameState.currentBet = room.currentBet;
        if (room.anteAmount > 0)
        {
            gameState.anteAmount = room.anteAmount;
        }
        BuildPlayerSlots(room);
        gameState.currentPlayerIndex = MapServerSeatToLocalIndex(room.currentPlayerIndex);
        UpdatePotDisplay(gameState.pot);
        UpdatePhaseDisplay();
        UpdateWaitingMessage(room);
        SyncLocalBettingState();
        UpdateButtonStates();
        UpdateTurnMessage();

        if (gameState.currentPhase != lastPhase)
        {
            HandlePhaseTransition(lastPhase, gameState.currentPhase, room);
            lastPhase = gameState.currentPhase;
        }

        if (isWaitingPhase && !localReadySent && MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.SetPlayerReady(true);
            localReadySent = true;
        }
    }

    private void HandlePhaseTransition(GamePhase previous, GamePhase current, RoomSnapshot room = null)
    {
        pendingFoldConfirm = false;
        if (current == GamePhase.Ante || current == GamePhase.None || current == GamePhase.DealCards ||
            (previous == GamePhase.GameOver && current == GamePhase.Exchange))
        {
            ResetPlayersForNewRound();
            hasDealtLocalHand = false;
            hasSentContinue = false;
        }

        // Only show "Game Over" when the whole game has ended (room inactive). When only a round
        // ended, phase is still GameOver but room.isActive is true; the round-ended dialog is
        // shown by ShowWinnerAfterPotAnimation from the showdown_result event.
        if (current == GamePhase.GameOver && roundFinishedDialog != null && room != null && !room.isActive)
        {
            string winnerName = "Winner";
            if (!string.IsNullOrEmpty(room.lastWinnerId) && playerByUserId.TryGetValue(room.lastWinnerId, out Player winner) && winner != null)
            {
                winnerName = winner.playerName ?? "Winner";
            }
            roundFinishedDialog.showDialog("Game Over", $"{winnerName} wins the game!");
            if (roundFinishedDialog.continueButton != null)
            {
                roundFinishedDialog.continueButton.gameObject.SetActive(false);
            }
            if (roundFinishedDialog.quitButton != null)
            {
                roundFinishedDialog.quitButton.gameObject.SetActive(true);
            }
        }

        if (tableAnimator != null)
        {
            if (current == GamePhase.Ante || current == GamePhase.DealCards)
            {
                tableAnimator.ShowPot();
            }
            else if (current == GamePhase.None)
            {
                tableAnimator.HidePot();
            }
        }
    }

    private void UpdatePotVisibility()
    {
        if (tableAnimator == null)
        {
            return;
        }

        if (gameState != null && gameState.pot > 0)
        {
            tableAnimator.ShowPot();
        }
    }

    private void BuildPlayerSlots(RoomSnapshot room)
    {
        if (players == null || players.Length == 0)
        {
            return;
        }

        gameState.playerList.Clear();
        playerByUserId.Clear();
        localPlayer = null;
        localPlayerIndex = -1;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
            {
                continue;
            }

            players[i].gameObject.SetActive(true);
            players[i].playerIndex = i;
            if (players[i].handCenter == null)
            {
                Transform handCenter = players[i].transform.Find("HandCenter");
                if (handCenter != null)
                {
                    players[i].handCenter = handCenter;
                }
            }
            ApplyPlaceholder(players[i], i);
            gameState.playerList.Add(players[i]);
        }

        if (players.Length > 0 && players[0] != null)
        {
            localPlayer = players[0];
            localPlayerIndex = 0;
            ApplyLocalAvatar();
        }

        if (room.players == null || room.players.Count == 0)
        {
            return;
        }

        List<RoomPlayerSnapshot> orderedPlayers = new List<RoomPlayerSnapshot>();
        List<RoomPlayerSnapshot> sortedBySeat = room.players
            .Where(p => p != null)
            .OrderBy(p => p.seatIndex)
            .ToList();

        localSeatIndexServer = 0;
        if (session != null)
        {
            RoomPlayerSnapshot localSnapshot = sortedBySeat.FirstOrDefault(p => p.userId == session.uid);
            if (localSnapshot != null)
            {
                localSeatIndexServer = Mathf.Max(0, localSnapshot.seatIndex);
            }
        }

        int totalSeats = players.Length;
        for (int i = 0; i < totalSeats; i++)
        {
            int targetSeat = (localSeatIndexServer + i) % totalSeats;
            RoomPlayerSnapshot snapshot = sortedBySeat.FirstOrDefault(p => p.seatIndex == targetSeat);
            if (snapshot != null)
            {
                orderedPlayers.Add(snapshot);
            }
        }

        for (int i = 0; i < orderedPlayers.Count && i < players.Length; i++)
        {
            RoomPlayerSnapshot snapshot = orderedPlayers[i];
            Player player = players[i];
            if (player == null)
            {
                continue;
            }

            player.isCPU = session == null || snapshot.userId != session.uid;
            player.SetPlayerName(string.IsNullOrEmpty(snapshot.username) ? $"Player {i + 1}" : snapshot.username);
            player.SetTokenAmount(snapshot.tokenAmount);
            ApplyAvatarForPlayer(player, snapshot.avatarIndex);
            if (!string.IsNullOrEmpty(snapshot.userId))
            {
                playerByUserId[snapshot.userId] = player;
            }

            if (session != null && snapshot.userId == session.uid)
            {
                localPlayer = player;
                localPlayerIndex = i;
                ApplyLocalAvatar();
            }
        }
    }

    private void ApplyPlaceholder(Player player, int index)
    {
        if (player == null)
        {
            return;
        }

        player.isCPU = index != 0;
        player.SetPlayerName($"Player {index + 1}");
        player.SetTokenAmount(0);
        if (index == 0)
        {
            ApplyLocalAvatar();
        }
        else if (ResourceManager.cpuAvatarSprite != null)
        {
            player.SetAvatar(ResourceManager.cpuAvatarSprite);
        }
    }

    private int MapServerSeatToLocalIndex(int serverSeatIndex)
    {
        if (players == null || players.Length == 0)
        {
            return serverSeatIndex;
        }

        int totalSeats = players.Length;
        int normalizedServer = ((serverSeatIndex % totalSeats) + totalSeats) % totalSeats;
        int normalizedLocal = ((normalizedServer - localSeatIndexServer) + totalSeats) % totalSeats;
        return normalizedLocal;
    }

    private void ApplyLocalAvatar()
    {
        if (localPlayer == null || session == null)
        {
            return;
        }

        if (ResourceManager.avatarSpriteList == null || ResourceManager.avatarSpriteList.Count == 0)
        {
            return;
        }

        int index = Mathf.Clamp(session.avatarIndex, 0, ResourceManager.avatarSpriteList.Count - 1);
        localPlayer.SetAvatar(ResourceManager.avatarSpriteList[index]);
    }

    private void ApplyAvatarForPlayer(Player player, int avatarIndex)
    {
        if (player == null)
        {
            return;
        }

        if (ResourceManager.avatarSpriteList == null || ResourceManager.avatarSpriteList.Count == 0)
        {
            return;
        }

        int index = Mathf.Clamp(avatarIndex, 0, ResourceManager.avatarSpriteList.Count - 1);
        player.SetAvatar(ResourceManager.avatarSpriteList[index]);
    }

    private bool TryParseRoomPhase(string phase, out GamePhase parsed)
    {
        parsed = GamePhase.None;
        if (string.IsNullOrEmpty(phase))
        {
            return false;
        }

        if (Enum.TryParse(phase, true, out GamePhase result))
        {
            parsed = result;
            return true;
        }

        return false;
    }

    private void UpdatePhaseDisplay()
    {
        if (gamePhaseBar != null)
        {
            gamePhaseBar.SetPhase(gameState.currentPhase);
        }
    }

    private void UpdateWaitingMessage(RoomSnapshot room)
    {
        if (!isWaitingPhase || isStartCountdownActive)
        {
            lastWaitingMessage = null;
            return;
        }

        int playerCount = room.players != null ? room.players.Count : 0;
        string message = room.maxPlayers > 0
            ? $"Waiting {playerCount}/{room.maxPlayers}"
            : "Waiting for players";

        if (message != lastWaitingMessage && AlertBar.Instance != null)
        {
            AlertBar.Instance.ShowMessage(message);
            lastWaitingMessage = message;
        }
    }


    private void UpdateTurnMessage()
    {
        if (gameState == null || players == null || players.Length == 0)
        {
            return;
        }

        if (isWaitingPhase || gameState.currentPhase == GamePhase.GameOver || gameState.currentPhase == GamePhase.None)
        {
            lastTurnPlayerIndex = -1;
            lastTurnPhase = GamePhase.None;
            return;
        }

        if (!hasDealtLocalHand && (gameState.currentPhase == GamePhase.Exchange || gameState.currentPhase == GamePhase.Betting))
        {
            return;
        }

        int currentIndex = gameState.currentPlayerIndex;
        if (currentIndex < 0 || currentIndex >= players.Length)
        {
            return;
        }

        if (currentIndex == lastTurnPlayerIndex && gameState.currentPhase == lastTurnPhase)
        {
            return;
        }

        Player currentPlayer = players[currentIndex];
        if (currentPlayer == null)
        {
            return;
        }

        string message = gameState.currentPhase switch
        {
            GamePhase.Exchange => "Exchanging...",
            GamePhase.Betting => "Betting...",
            GamePhase.Ante => "Paying ante...",
            GamePhase.DealCards => "Dealing...",
            _ => "Playing..."
        };

        currentPlayer.showMessage(message);
        lastTurnPlayerIndex = currentIndex;
        lastTurnPhase = gameState.currentPhase;
    }

    private void SyncLocalBettingState()
    {
        if (localPlayer == null)
        {
            return;
        }

        if (gameState.currentPhase != GamePhase.Betting)
        {
            return;
        }

        if (localPlayer.hasFolded)
        {
            return;
        }

        // If the current bet increased beyond our bet, allow us to act again.
        if (gameState.currentBet > localPlayer.currentBet)
        {
            localPlayer.hasActedThisRound = false;
        }
    }

    private void HandleGameStarting(int seconds)
    {
        isStartCountdownActive = true;
        string message = $"Players are all ready.";
        if (AlertBar.Instance != null)
        {
            AlertBar.Instance.ShowMessage(message);
        }
        lastWaitingMessage = message;
    }

    private void HandleGameStarted(object payload)
    {
        hasSentContinue = false;
        hasDealtLocalHand = false;

        if (roundFinishedDialog != null)
        {
            roundFinishedDialog.hideDialog();
        }

        // Clear stale cards from all players so eliminated players don't show
        // last round's hands (they won't receive cards_dealt this round).
        EnsureGameState();
        ClearAllHands();

        // Eliminated local player — hide all action buttons immediately.
        if (localPlayer != null && localPlayer.tokenAmount <= 0)
        {
            SetAllActionButtons(false);
        }
    }

    private void HandlePlayerActionEvent(object payload)
    {
        if (payload is not string json)
        {
            return;
        }

        PlayerActionPayload data = JsonUtility.FromJson<PlayerActionPayload>(json);
        if (data == null || data.action == null || string.IsNullOrEmpty(data.playerId))
        {
            return;
        }

        // Immediately apply room snapshot so pot/bet display updates without waiting for the
        // next room_update/phase_changed event — this removes the visible delay after a bet.
        if (data.room != null && MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.UpdateRoomSnapshot(data.room);
        }

        if (session != null && data.playerId == session.uid)
        {
            return;
        }

        string message = data.action.type switch
        {
            "bet" => $"Bet {data.action.amount}",
            "call" => data.action.amount > 0 ? $"Call {data.action.amount}" : "Check",
            "raise" => $"Raise to {data.action.amount}",
            "fold" => "Fold",
            _ => string.Empty
        };

        TryShowPlayerMessage(data.playerId, message);
    }

    private void HandlePlayerReadyChanged(object payload)
    {
        if (payload is not string json)
        {
            return;
        }

        PlayerReadyPayload data = JsonUtility.FromJson<PlayerReadyPayload>(json);
        if (data == null || string.IsNullOrEmpty(data.playerId))
        {
            return;
        }

        if (data.isReady)
        {
            TryShowPlayerMessage(data.playerId, "Ready");
        }
        else
        {
            TryShowPlayerMessage(data.playerId, "Not Ready");
        }
    }

    private void HandlePlayerExchanged(object payload)
    {
        if (payload is not string json)
        {
            return;
        }

        PlayerSimplePayload data = JsonUtility.FromJson<PlayerSimplePayload>(json);
        if (data == null || string.IsNullOrEmpty(data.playerId))
        {
            return;
        }

        TryShowPlayerMessage(data.playerId, "Exchanged");
    }

    private void HandlePlayerSkippedExchange(object payload)
    {
        if (payload is not string json)
        {
            return;
        }

        PlayerSimplePayload data = JsonUtility.FromJson<PlayerSimplePayload>(json);
        if (data == null || string.IsNullOrEmpty(data.playerId))
        {
            return;
        }

        TryShowPlayerMessage(data.playerId, "Skipped");
    }

    private void HandleShowdownReveal(object payload)
    {
        if (payload is not string json)
        {
            return;
        }

        ShowdownRevealPayload data = JsonUtility.FromJson<ShowdownRevealPayload>(json);
        if (data == null || data.players == null)
        {
            return;
        }

        foreach (ShowdownRevealPlayer playerPayload in data.players)
        {
            if (string.IsNullOrEmpty(playerPayload.userId) || playerPayload.cards == null)
            {
                continue;
            }

            if (!playerByUserId.TryGetValue(playerPayload.userId, out Player player) || player == null)
            {
                continue;
            }

            for (int i = 0; i < playerPayload.cards.Length && i < player.playerHand.Count; i++)
            {
                CardPayload cardPayload = playerPayload.cards[i];
                Card card = player.playerHand[i];
                if (card == null || card.cardObject == null)
                {
                    continue;
                }

                Sprite frontSprite = ResolveCardSprite(cardPayload);
                Sprite backSprite = ResourceManager.backSprite;
                card.rank = cardPayload.rank;
                card.suit = ResolveSuit(cardPayload.suit);
                card.frontSprite = frontSprite;
                card.backSprite = backSprite;

                Image frontImage = card.cardObject.transform.Find("Front").GetComponent<Image>();
                Image backImage = card.cardObject.transform.Find("Back").GetComponent<Image>();
                frontImage.sprite = frontSprite;
                frontImage.enabled = true;
                backImage.sprite = backSprite;
                backImage.enabled = false;
            }
        }

        if (cardAnimator != null)
        {
            StartCoroutine(cardAnimator.SpreadCardsForAll(gameState));
        }
    }

    private void HandleShowdownHands(object payload)
    {
        if (payload is not string json)
        {
            return;
        }

        ShowdownHandsPayload data = JsonUtility.FromJson<ShowdownHandsPayload>(json);
        if (data == null || data.players == null)
        {
            return;
        }

        foreach (ShowdownHandEntry entry in data.players)
        {
            if (entry == null || string.IsNullOrEmpty(entry.userId))
            {
                continue;
            }

            string handText = FormatHandRankText(entry.handRank);
            TryShowPlayerMessage(entry.userId, handText);
        }
    }

    private void HandleShowdownResult(object payload)
    {
        if (payload is not string json)
        {
            return;
        }

        ShowdownResultPayload data = JsonUtility.FromJson<ShowdownResultPayload>(json);
        if (data == null)
        {
            return;
        }

        // winnerId may be empty when all players fold simultaneously — show a no-winner dialog.
        if (string.IsNullOrEmpty(data.winnerId) || !playerByUserId.TryGetValue(data.winnerId, out Player winner) || winner == null)
        {
            StartCoroutine(ShowNoWinnerDialog());
            return;
        }

        StartCoroutine(ShowWinnerAfterPotAnimation(winner, data.handRank, data.potAmount));
    }

    private IEnumerator ShowWinnerAfterPotAnimation(Player winner, string handRank, int potAward)
    {
        if (tableAnimator != null)
        {
            yield return StartCoroutine(tableAnimator.AnimatePotToWinner(winner));
        }

        if (roundFinishedDialog != null)
        {
            string winnerName = winner.playerName ?? "Player";
            string handText = FormatHandRankText(handRank);
            bool gameOver = MultiplayerManager.Instance != null && MultiplayerManager.Instance.CurrentRoom != null && !MultiplayerManager.Instance.CurrentRoom.isActive;

            // Determine whether the local player has been eliminated (out of chips).
            bool localEliminated = localPlayer != null && localPlayer.tokenAmount <= 0;

            if (gameOver)
            {
                roundFinishedDialog.showDialog("Game Over", $"{winnerName} wins the game!");
                if (roundFinishedDialog.continueButton != null)
                    roundFinishedDialog.continueButton.gameObject.SetActive(false);
                if (roundFinishedDialog.quitButton != null)
                    roundFinishedDialog.quitButton.gameObject.SetActive(true);
            }
            else if (localEliminated)
            {
                // Eliminated players are auto-counted as ready on the backend so they
                // don't need to (and shouldn't) press Continue.
                roundFinishedDialog.showDialog("You're Out!", $"You have no chips left.\nWinner this round: {winnerName}\n{handText}  +{potAward} chips");
                if (roundFinishedDialog.continueButton != null)
                    roundFinishedDialog.continueButton.gameObject.SetActive(false);
                if (roundFinishedDialog.quitButton != null)
                    roundFinishedDialog.quitButton.gameObject.SetActive(true);
            }
            else
            {
                roundFinishedDialog.showDialog("Round Ended", $"Winner: {winnerName}\n{handText}  +{potAward} chips");
                if (roundFinishedDialog.continueButton != null)
                    roundFinishedDialog.continueButton.gameObject.SetActive(true);
                if (roundFinishedDialog.quitButton != null)
                    roundFinishedDialog.quitButton.gameObject.SetActive(true);
            }
        }
    }

    private IEnumerator ShowNoWinnerDialog()
    {
        if (tableAnimator != null)
        {
            yield return StartCoroutine(tableAnimator.AnimatePotToWinner(null));
        }

        if (roundFinishedDialog != null)
        {
            bool gameOver = MultiplayerManager.Instance != null && MultiplayerManager.Instance.CurrentRoom != null && !MultiplayerManager.Instance.CurrentRoom.isActive;
            if (gameOver)
            {
                roundFinishedDialog.showDialog("Game Over", "All players folded.\nNo winner.");
                if (roundFinishedDialog.continueButton != null)
                    roundFinishedDialog.continueButton.gameObject.SetActive(false);
                if (roundFinishedDialog.quitButton != null)
                    roundFinishedDialog.quitButton.gameObject.SetActive(true);
            }
            else
            {
                roundFinishedDialog.showDialog("Round Ended", "All players folded.\nNo winner — pot returned.");
                if (roundFinishedDialog.continueButton != null)
                    roundFinishedDialog.continueButton.gameObject.SetActive(true);
                if (roundFinishedDialog.quitButton != null)
                    roundFinishedDialog.quitButton.gameObject.SetActive(true);
            }
        }
    }

    private void HandleRoundContinueState(object payload)
    {
        if (payload is not string json)
        {
            return;
        }

        RoundContinueStatePayload data = JsonUtility.FromJson<RoundContinueStatePayload>(json);
        if (data == null || data.readyUserIds == null)
        {
            return;
        }

        int totalPlayers = MultiplayerManager.Instance?.CurrentRoom?.players?.Count ?? players.Length;
        string message = $"Waiting for others ({data.readyUserIds.Count}/{totalPlayers})";
        if (AlertBar.Instance != null)
        {
            AlertBar.Instance.ShowMessage(message);
        }
    }

    private string FormatHandRankText(string handRank)
    {
        return handRank switch
        {
            "RoyalFlush" => "Royal Flush",
            "StraightFlush" => "Straight Flush",
            "FourOfAKind" => "Four of a Kind",
            "FullHouse" => "Full House",
            "ThreeOfAKind" => "Three of a Kind",
            "TwoPair" => "Two Pair",
            "OnePair" => "One Pair",
            "HighCard" => "High Card",
            _ => handRank
        };
    }

    private void TryShowPlayerMessage(string userId, string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (session != null && userId == session.uid)
        {
            return;
        }

        if (!playerByUserId.TryGetValue(userId, out Player player) || player == null)
        {
            return;
        }

        player.showMessage(message);
    }

    private void ResetPlayersForNewRound()
    {
        foreach (Player player in gameState.playerList)
        {
            if (player == null)
            {
                continue;
            }

            player.selectedCards.Clear();
            player.currentBet = 0;
            player.hasActedThisRound = false;
            player.hasFolded = false;
            player.hasExchanged = false;
        }
    }

    private void UpdateButtonStates()
    {
        bool hasLocalPlayer = localPlayer != null;
        if (!hasLocalPlayer)
        {
            SetAllActionButtons(false);
            return;
        }

        bool exchangePhase = gameState.currentPhase == GamePhase.Exchange;
        bool bettingPhase = gameState.currentPhase == GamePhase.Betting;
        bool isLocalTurn = gameState.currentPlayerIndex == localPlayerIndex;

        if (exchangeButton != null)
        {
            exchangeButton.gameObject.SetActive(exchangePhase && !localPlayer.hasExchanged && isLocalTurn);
            exchangeButton.interactable = exchangeButton.gameObject.activeSelf;
        }
        if (skipExchangeButton != null)
        {
            skipExchangeButton.gameObject.SetActive(exchangePhase && !localPlayer.hasExchanged && isLocalTurn);
            skipExchangeButton.interactable = skipExchangeButton.gameObject.activeSelf;
        }

        int callAmount = gameState.currentBet - localPlayer.currentBet;
        bool canBet = bettingPhase && isLocalTurn && !localPlayer.hasActedThisRound && !localPlayer.hasFolded && localPlayer.tokenAmount > 0;
        bool isInitialBet = gameState.currentBet == 0 || gameState.currentBet == localPlayer.currentBet;
        bool isResponseBet = gameState.currentBet > localPlayer.currentBet;
        bool canCheck = bettingPhase && isLocalTurn && !localPlayer.hasActedThisRound && !localPlayer.hasFolded && callAmount == 0;

        if (bettingPhase)
        {
            string toCallStr = callAmount <= 0 ? "Check" : $"To call: {callAmount}";
            string betInfoMessage = $"Current bet: {gameState.currentBet} | {toCallStr}";
            if (isLocalTurn && localPlayer.tokenAmount <= 0 && !localPlayer.hasFolded)
            {
                betInfoMessage += " | You're out of chips.";
            }
            if (AlertBar.Instance != null && betInfoMessage != _lastBetInfoMessage)
            {
                _lastBetInfoMessage = betInfoMessage;
                AlertBar.Instance.ShowMessage(betInfoMessage);
            }
        }
        else
        {
            _lastBetInfoMessage = null;
        }

        if (bet5Button != null)
        {
            bool showBet = canBet && isInitialBet && localPlayer.tokenAmount >= Constants.BET_OPTION_1;
            bet5Button.gameObject.SetActive(showBet);
            bet5Button.interactable = showBet;
        }
        if (bet10Button != null)
        {
            bool showBet = canBet && isInitialBet && localPlayer.tokenAmount >= Constants.BET_OPTION_2;
            bet10Button.gameObject.SetActive(showBet);
            bet10Button.interactable = showBet;
        }
        if (bet25Button != null)
        {
            bool showBet = canBet && isInitialBet && localPlayer.tokenAmount >= Constants.BET_OPTION_3;
            bet25Button.gameObject.SetActive(showBet);
            bet25Button.interactable = showBet;
        }
        if (callButton != null)
        {
            bool canCall = canBet && isResponseBet && callAmount > 0 && callAmount <= localPlayer.tokenAmount;
            callButton.gameObject.SetActive(canCall);
            callButton.interactable = canCall;
        }
        if (checkButton != null)
        {
            checkButton.gameObject.SetActive(canCheck);
            checkButton.interactable = canCheck;
        }
        if (raiseButton != null)
        {
            int raiseAmount = gameState.currentBet * 2 - localPlayer.currentBet;
            bool canRaise = canBet && isResponseBet && raiseAmount > 0 && raiseAmount <= localPlayer.tokenAmount;
            raiseButton.gameObject.SetActive(canRaise);
            raiseButton.interactable = canRaise;
        }
        if (foldButton != null)
        {
            bool canAct = bettingPhase && isLocalTurn && !localPlayer.hasActedThisRound && !localPlayer.hasFolded;
            bool showFold = canBet || (canAct && localPlayer.tokenAmount <= 0);
            foldButton.gameObject.SetActive(showFold);
            foldButton.interactable = showFold;
        }
    }

    private void SetAllActionButtons(bool isActive)
    {
        if (exchangeButton != null) exchangeButton.gameObject.SetActive(isActive);
        if (skipExchangeButton != null) skipExchangeButton.gameObject.SetActive(isActive);
        if (bet5Button != null) bet5Button.gameObject.SetActive(isActive);
        if (bet10Button != null) bet10Button.gameObject.SetActive(isActive);
        if (bet25Button != null) bet25Button.gameObject.SetActive(isActive);
        if (callButton != null) callButton.gameObject.SetActive(isActive);
        if (raiseButton != null) raiseButton.gameObject.SetActive(isActive);
        if (foldButton != null) foldButton.gameObject.SetActive(isActive);
        if (checkButton != null) checkButton.gameObject.SetActive(isActive);
    }

    private void HandleCardsDealt(object payload)
    {
        if (payload is string json)
        {
            CardsDealtPayload data = JsonUtility.FromJson<CardsDealtPayload>(json);
            if (data != null)
            {
                if (data.room != null && MultiplayerManager.Instance != null)
                {
                    MultiplayerManager.Instance.UpdateRoomSnapshot(data.room);
                }

                if (data.cards != null)
                {
                    ApplyLocalHand(data.cards);
                }
            }
        }
    }

    private void HandleCardsDealtPublic(object payload)
    {
        // cards_dealt_public carries the updated room snapshot but no private cards.
        // Only update the room state without triggering a full SyncRoomState that would
        // interrupt the deal animation already in progress.
        if (payload is not string json) return;
        RoomOnlyPayload data = JsonUtility.FromJson<RoomOnlyPayload>(json);
        if (data?.room != null && MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.UpdateRoomSnapshot(data.room);
        }
    }

    private void HandleCardsExchanged(object payload)
    {
        if (payload is string json)
        {
            CardsDealtPayload data = JsonUtility.FromJson<CardsDealtPayload>(json);
            if (data != null && data.cards != null)
            {
                ApplyLocalHand(data.cards);
            }
        }
    }

    private void ApplyLocalHand(CardPayload[] cards)
    {
        if (localPlayer == null || cardAnimator == null || cards == null)
        {
            return;
        }

        if (!hasDealtLocalHand)
        {
            ClearAllHands();

            localPlayer.isCPU = false;
            foreach (CardPayload payload in cards)
            {
                Sprite frontSprite = ResolveCardSprite(payload);
                Sprite backSprite = ResourceManager.backSprite;
                localPlayer.AddCardToHand(new Card(payload.rank, ResolveSuit(payload.suit), frontSprite, backSprite));
            }

            if (GameSettings.CardAutoSort)
            {
                Rules.SortCards(localPlayer.playerHand);
            }

            foreach (Player player in gameState.playerList)
            {
                if (player == null || player == localPlayer)
                {
                    continue;
                }

                player.isCPU = true;
                player.playerHand.Clear();
                for (int i = 0; i < Constants.CARDS_PER_PLAYER; i++)
                {
                    player.AddCardToHand(new Card(0, Suit.Hearts, ResourceManager.backSprite, ResourceManager.backSprite));
                }
            }

            StartCoroutine(DealAndFlagReady());
            return;
        }

        if (_pendingExchangeIndices != null && _pendingExchangeIndices.Length > 0 && _pendingExchangeCards != null)
        {
            int[] indices = _pendingExchangeIndices;
            List<Card> cardsToExchange = _pendingExchangeCards;
            _pendingExchangeIndices = null;
            _pendingExchangeCards = null;
            UpdateLocalHandCardsForPendingExchange(cards, indices, runSpreadAfterUpdate: false);
            if (cardsToExchange.Count > 0 && cardAnimator != null && localPlayer != null && localPlayer.handCenter != null)
            {
                // WebGL: use instant exchange (no DOTween) to avoid engine abort/INVALID_ENUM crash
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    StartCoroutine(cardAnimator.ExchangeCardsInstant(localPlayer, cardsToExchange, localPlayer.handCenter));
                }
                else
                {
                    StartCoroutine(RunExchangeAnimationWithPending(localPlayer, cardsToExchange));
                }
            }
        }
        else
        {
            UpdateLocalHandCards(cards);
        }
    }

    private void UpdateLocalHandCardsForPendingExchange(CardPayload[] cards, int[] skipImageUpdateIndices, bool runSpreadAfterUpdate = true)
    {
        if (localPlayer.playerHand.Count != cards.Length)
        {
            Debug.LogWarning("[MultiPlayScene] Local hand size mismatch, skipping in-place update.");
            return;
        }

        var skipSet = new HashSet<int>(skipImageUpdateIndices ?? System.Array.Empty<int>());

        for (int i = 0; i < cards.Length; i++)
        {
            Card existing = localPlayer.playerHand[i];
            if (existing == null)
            {
                continue;
            }

            CardPayload payload = cards[i];
            Sprite frontSprite = ResolveCardSprite(payload);
            Sprite backSprite = ResourceManager.backSprite;

            existing.rank = payload.rank;
            existing.suit = ResolveSuit(payload.suit);
            existing.frontSprite = frontSprite;
            existing.backSprite = backSprite;

            if (existing.cardObject != null && !skipSet.Contains(i))
            {
                Transform frontT = existing.cardObject.transform.Find("Front");
                Transform backT = existing.cardObject.transform.Find("Back");
                if (frontT != null)
                {
                    Image frontImage = frontT.GetComponent<Image>();
                    if (frontImage != null)
                    {
                        frontImage.sprite = frontSprite;
                        frontImage.enabled = true;
                    }
                }
                if (backT != null)
                {
                    Image backImage = backT.GetComponent<Image>();
                    if (backImage != null)
                    {
                        backImage.sprite = backSprite;
                        backImage.enabled = false;
                    }
                }

                CardClickHandler clickHandler = existing.cardObject.GetComponent<CardClickHandler>();
                if (clickHandler == null)
                {
                    clickHandler = existing.cardObject.AddComponent<CardClickHandler>();
                }
                clickHandler.card = existing;
                clickHandler.player = localPlayer;
            }
        }

        if (runSpreadAfterUpdate && GameSettings.CardAutoSort)
        {
            Rules.SortCards(localPlayer.playerHand);
            if (cardAnimator != null)
            {
                StartCoroutine(cardAnimator.SpreadCards(gameState));
            }
        }
    }

    private void UpdateLocalHandCards(CardPayload[] cards)
    {
        if (localPlayer.playerHand.Count != cards.Length)
        {
            Debug.LogWarning("[MultiPlayScene] Local hand size mismatch, skipping in-place update.");
            return;
        }

        for (int i = 0; i < cards.Length; i++)
        {
            Card existing = localPlayer.playerHand[i];
            if (existing == null)
            {
                continue;
            }

            CardPayload payload = cards[i];
            Sprite frontSprite = ResolveCardSprite(payload);
            Sprite backSprite = ResourceManager.backSprite;

            // Always update Card data so exchange animation (which creates objects for cardObject==null) uses correct sprites
            existing.rank = payload.rank;
            existing.suit = ResolveSuit(payload.suit);
            existing.frontSprite = frontSprite;
            existing.backSprite = backSprite;

            if (existing.cardObject != null)
            {
                Transform frontT = existing.cardObject.transform.Find("Front");
                Transform backT = existing.cardObject.transform.Find("Back");
                if (frontT != null)
                {
                    Image frontImage = frontT.GetComponent<Image>();
                    if (frontImage != null)
                    {
                        frontImage.sprite = frontSprite;
                        frontImage.enabled = true;
                    }
                }
                if (backT != null)
                {
                    Image backImage = backT.GetComponent<Image>();
                    if (backImage != null)
                    {
                        backImage.sprite = backSprite;
                        backImage.enabled = false;
                    }
                }

                CardClickHandler clickHandler = existing.cardObject.GetComponent<CardClickHandler>();
                if (clickHandler == null)
                {
                    clickHandler = existing.cardObject.AddComponent<CardClickHandler>();
                }
                clickHandler.card = existing;
                clickHandler.player = localPlayer;
            }
        }

        if (GameSettings.CardAutoSort)
        {
            Rules.SortCards(localPlayer.playerHand);
            if (cardAnimator != null)
            {
                StartCoroutine(cardAnimator.SpreadCards(gameState));
            }
        }
    }

    private IEnumerator DealAndFlagReady()
    {
        if (cardAnimator != null)
        {
            yield return StartCoroutine(cardAnimator.DealAnimator(gameState));
        }
        hasDealtLocalHand = true;
    }

    private void ClearAllHands()
    {
        foreach (Player player in gameState.playerList)
        {
            if (player == null)
            {
                continue;
            }

            foreach (Card card in player.playerHand)
            {
                if (card != null && card.cardObject != null)
                {
                    Destroy(card.cardObject);
                }
            }

            player.playerHand.Clear();
            player.selectedCards.Clear();
        }
    }

    private Sprite ResolveCardSprite(CardPayload payload)
    {
        if (payload == null)
        {
            return ResourceManager.backSprite;
        }

        string suitKey = payload.suit switch
        {
            0 => "spades",
            1 => "clubs",
            2 => "diamonds",
            _ => "hearts"
        };

        string rankKey = payload.rank switch
        {
            14 => "ace",
            13 => "king",
            12 => "queen",
            11 => "jack",
            15 => "2",
            _ => payload.rank.ToString()
        };

        string cardKey = $"{rankKey}_of_{suitKey}";
        if (ResourceManager.frontSpriteMap != null && ResourceManager.frontSpriteMap.TryGetValue(cardKey, out Sprite sprite))
        {
            return sprite;
        }

        return ResourceManager.backSprite;
    }

    private Suit ResolveSuit(int suitValue)
    {
        return suitValue switch
        {
            0 => Suit.Spades,
            1 => Suit.Clubs,
            2 => Suit.Diamonds,
            _ => Suit.Hearts
        };
    }

    public void OnExchangeClicked()
    {
        if (localPlayer == null || gameState.currentPhase != GamePhase.Exchange || localPlayer.hasExchanged)
        {
            return;
        }

        if (localPlayer.selectedCards.Count == 0)
        {
            AlertBar.Instance.ShowMessage("No cards selected");
            return;
        }

        int[] indices = localPlayer.selectedCards
            .Select(card => localPlayer.playerHand.IndexOf(card))
            .Where(index => index >= 0)
            .Take(Constants.MAX_EXCHANGE_CARDS)
            .ToArray();

        if (indices.Length == 0)
        {
            AlertBar.Instance.ShowMessage("Invalid selection");
            return;
        }

        localPlayer.selectedCards.Clear();
        localPlayer.hasExchanged = true;
        UpdateButtonStates();

        List<Card> cardsToExchange = indices
            .Select(index => index >= 0 && index < localPlayer.playerHand.Count ? localPlayer.playerHand[index] : null)
            .Where(card => card != null)
            .ToList();

        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.ExchangeCards(indices);
        }

        // Wait for server response (HandleCardsExchanged) before animating so hand data is correct first
        if (cardsToExchange.Count > 0)
        {
            _pendingExchangeIndices = indices;
            _pendingExchangeCards = cardsToExchange;
        }
    }

    private IEnumerator RunExchangeAnimationWithPending(Player player, List<Card> cardsToExchange)
    {
        if (player != null && player.handCenter != null && cardAnimator != null)
        {
            yield return cardAnimator.ExchangeCardsAnimation(player, cardsToExchange, player.handCenter);
        }
    }

    public void OnSkipExchangeClicked()
    {
        if (localPlayer == null || gameState.currentPhase != GamePhase.Exchange || localPlayer.hasExchanged)
        {
            return;
        }

        ClearSelectedCards(localPlayer);
        localPlayer.hasExchanged = true;
        UpdateButtonStates();

        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.SkipExchange();
        }
    }

    private void ClearSelectedCards(Player player)
    {
        if (player == null || player.selectedCards == null)
        {
            return;
        }

        foreach (Card card in player.selectedCards)
        {
            if (card == null || card.cardObject == null)
            {
                continue;
            }

            card.isSelected = false;
            Vector3 position = card.cardObject.transform.position;
            position.y = card.originalYPosition;
            card.cardObject.transform.DOMove(position, GameSettings.GetAnimationDuration(0.2f)).SetEase(Ease.OutQuad);
        }

        player.selectedCards.Clear();
    }

    public void OnBetClicked(int amount)
    {
        pendingFoldConfirm = false;
        if (!CanLocalAct())
        {
            return;
        }

        if (!localPlayer.CanBet(amount))
        {
            return;
        }

        localPlayer.hasActedThisRound = true;
        localPlayer.currentBet = amount;
        localPlayer.showMessage($"Bet {amount}");
        UpdateButtonStates();

        MultiplayerManager.Instance?.SendBettingAction("bet", amount);
    }

    public void OnCallClicked()
    {
        pendingFoldConfirm = false;
        if (!CanLocalAct())
        {
            return;
        }

        int callAmount = gameState.currentBet - localPlayer.currentBet;
        if (!localPlayer.CanBet(callAmount))
        {
            return;
        }

        localPlayer.hasActedThisRound = true;
        localPlayer.currentBet = gameState.currentBet;
        localPlayer.showMessage(callAmount > 0 ? $"Call {callAmount}" : "Check");
        UpdateButtonStates();

        MultiplayerManager.Instance?.SendBettingAction("call", callAmount);
    }

    public void OnCheckClicked()
    {
        pendingFoldConfirm = false;
        if (!CanLocalAct())
        {
            return;
        }
        int callAmount = gameState.currentBet - localPlayer.currentBet;
        if (callAmount != 0)
        {
            return;
        }
        localPlayer.hasActedThisRound = true;
        localPlayer.showMessage("Check");
        UpdateButtonStates();
        MultiplayerManager.Instance?.SendBettingAction("call", 0);
    }

    public void OnRaiseClicked()
    {
        pendingFoldConfirm = false;
        if (!CanLocalAct())
        {
            return;
        }

        int raiseTo = gameState.currentBet * 2;
        int raiseAmount = raiseTo - localPlayer.currentBet;
        if (!localPlayer.CanBet(raiseAmount))
        {
            return;
        }

        localPlayer.hasActedThisRound = true;
        localPlayer.currentBet = raiseTo;
        localPlayer.showMessage($"Raise to {raiseTo}");
        UpdateButtonStates();

        MultiplayerManager.Instance?.SendBettingAction("raise", raiseTo);
    }

    public void OnFoldClicked()
    {
        if (!CanLocalAct())
        {
            return;
        }

        if (GameSettings.ConfirmBeforeFold && !pendingFoldConfirm)
        {
            pendingFoldConfirm = true;
            if (AlertBar.Instance != null)
            {
                AlertBar.Instance.ShowMessage("Tap Fold again to confirm");
            }
            return;
        }

        pendingFoldConfirm = false;
        localPlayer.hasFolded = true;
        localPlayer.hasActedThisRound = true;
        if (cardAnimator != null)
        {
            cardAnimator.HidePlayerHand(localPlayer);
        }
        localPlayer.showMessage("Fold");
        UpdateButtonStates();

        MultiplayerManager.Instance?.SendBettingAction("fold");
    }

    private bool CanLocalAct()
    {
        if (localPlayer == null)
        {
            return false;
        }

        if (gameState.currentPhase != GamePhase.Betting)
        {
            return false;
        }

        if (gameState.currentPlayerIndex != localPlayerIndex)
        {
            return false;
        }

        if (localPlayer.hasActedThisRound || localPlayer.hasFolded)
        {
            return false;
        }

        return true;
    }

    private void UpdatePotDisplay(int potAmount)
    {
        if (potText != null)
        {
            potText.text = potAmount.ToString();
        }

        UpdatePotVisibility();
    }

    private void HandleRoomDeleted(string roomId)
    {
        if (!string.IsNullOrEmpty(currentRoomId) && roomId == currentRoomId)
        {
            AlertBar.Instance.ShowMessage("Room closed");
            Utils.LoadScene("RoomScene");
        }
    }

    private void HandleMultiplayerError(string error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            AlertBar.Instance.ShowMessage(error);
        }
    }

    private void OnContinueClicked()
    {
        if (roundFinishedDialog != null)
        {
            roundFinishedDialog.hideDialog();
        }
        if (hasSentContinue)
        {
            return;
        }

        if (WebSocketManager.Instance != null && !string.IsNullOrEmpty(currentRoomId) && session != null)
        {
            WebSocketManager.Instance.SendRoundContinue(currentRoomId, session.uid);
            hasSentContinue = true;
        }
    }

    private void OnQuitClicked()
    {
        if (roundFinishedDialog != null)
        {
            roundFinishedDialog.hideDialog();
        }

        Utils.LoadScene("RoomScene");
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        StopRoomPolling();
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.LeaveRoomAndCleanup();
        }
    }

    [Serializable]
    private class RoomInfoResponse
    {
        public string roomId;
        public string hostId;
        public List<RoomInfoPlayer> players;
        public int maxPlayers;
        public string currentPhase;
        public int pot;
        public int currentBet;
        public int anteAmount;
        public int currentPlayerIndex;
        public bool isActive;
        public string lastWinnerId;
        public int lastWinnerHandValue;
    }

    [Serializable]
    private class RoomInfoPlayer
    {
        public string userId;
        public string username;
        public int avatarIndex;
        public int seatIndex;
        public int tokenAmount;
        public bool isReady;
    }

    [Serializable]
    private class CardsDealtPayload
    {
        public CardPayload[] cards;
        public RoomSnapshot room;
    }

    [Serializable]
    private class RoomOnlyPayload
    {
        public RoomSnapshot room;
    }

    [Serializable]
    private class CardPayload
    {
        public int suit;
        public int rank;
    }

    [Serializable]
    private class PlayerActionPayload
    {
        public string playerId;
        public PlayerActionData action;
        public RoomSnapshot room;
    }

    [Serializable]
    private class PlayerActionData
    {
        public string type;
        public int amount;
    }

    [Serializable]
    private class PlayerReadyPayload
    {
        public string playerId;
        public bool isReady;
    }

    [Serializable]
    private class PlayerSimplePayload
    {
        public string playerId;
    }

    [Serializable]
    private class ShowdownRevealPayload
    {
        public ShowdownRevealPlayer[] players;
    }

    [Serializable]
    private class ShowdownRevealPlayer
    {
        public string userId;
        public CardPayload[] cards;
    }

    [Serializable]
    private class ShowdownHandsPayload
    {
        public ShowdownHandEntry[] players;
    }

    [Serializable]
    private class ShowdownHandEntry
    {
        public string userId;
        public string handRank;
        public int handValue;
    }

    [Serializable]
    private class ShowdownResultPayload
    {
        public string winnerId;
        public string handRank;
        public int potAmount;
    }

    [Serializable]
    private class RoundContinueStatePayload
    {
        public List<string> readyUserIds;
    }
}
