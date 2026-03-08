using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Manages multiplayer game state and synchronization
public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance { get; private set; }

    // Game configuration values are defined in Constants class
    // startingTokens = Constants.STARTING_CHIPS (100)
    // anteAmount = Constants.ANTE_AMOUNT (5)

    // Current game state
    private string currentRoomId;
    private string localUserId;
    private bool isHost = false;
    private GameState gameState;
    private Dictionary<string, Player> playerMap = new Dictionary<string, Player>();

    // Events
    public event Action<GameState> OnGameStateUpdated;
    public event Action<string> OnError;
    public event Action<string> OnRoomDeleted;

    public RoomSnapshot CurrentRoom { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (WebSocketManager.Instance != null)
        {
            SubscribeToWebSocketEvents();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromWebSocketEvents();
    }

    private void SubscribeToWebSocketEvents()
    {
        WebSocketManager.Instance.OnPlayerJoined += HandlePlayerJoined;
        WebSocketManager.Instance.OnPlayerLeft += HandlePlayerLeft;
        WebSocketManager.Instance.OnPlayerReadyChanged += HandlePlayerReadyChanged;
        WebSocketManager.Instance.OnGameStarted += HandleGameStarted;
        WebSocketManager.Instance.OnCardsDealt += HandleCardsDealt;
        WebSocketManager.Instance.OnCardsExchanged += HandleCardsExchanged;
        WebSocketManager.Instance.OnPlayerExchanged += HandlePlayerExchanged;
        WebSocketManager.Instance.OnPlayerAction += HandlePlayerAction;
        WebSocketManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        WebSocketManager.Instance.OnPlayerDisconnected += HandlePlayerDisconnected;
        WebSocketManager.Instance.OnRoomDeleted += HandleRoomDeleted;
        WebSocketManager.Instance.OnError += HandleWebSocketError;
    }

    private void UnsubscribeFromWebSocketEvents()
    {
        if (WebSocketManager.Instance == null) return;

        WebSocketManager.Instance.OnPlayerJoined -= HandlePlayerJoined;
        WebSocketManager.Instance.OnPlayerLeft -= HandlePlayerLeft;
        WebSocketManager.Instance.OnPlayerReadyChanged -= HandlePlayerReadyChanged;
        WebSocketManager.Instance.OnGameStarted -= HandleGameStarted;
        WebSocketManager.Instance.OnCardsDealt -= HandleCardsDealt;
        WebSocketManager.Instance.OnCardsExchanged -= HandleCardsExchanged;
        WebSocketManager.Instance.OnPlayerExchanged -= HandlePlayerExchanged;
        WebSocketManager.Instance.OnPlayerAction -= HandlePlayerAction;
        WebSocketManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        WebSocketManager.Instance.OnPlayerDisconnected -= HandlePlayerDisconnected;
        WebSocketManager.Instance.OnRoomDeleted -= HandleRoomDeleted;
        WebSocketManager.Instance.OnError -= HandleWebSocketError;
    }

    /// <summary>
    /// Initialize multiplayer session
    /// </summary>
    public void Initialize(string roomId, string userId, bool isHost)
    {
        currentRoomId = roomId;
        localUserId = userId;
        this.isHost = isHost;

        gameState = new GameState(GameMode.MultiMode);
        playerMap.Clear();

        DebugConfig.Log($"[MultiplayerManager] Initialized - Room: {roomId}, User: {userId}, Host: {isHost}");
    }

    /// <summary>
    /// Create a new game room
    /// </summary>
    public void CreateRoom(string userId, string username, Action<string> onSuccess, Action<string> onError)
    {
        CreateRoom(userId, username, 0, $"{username}'s Room", onSuccess, onError);
    }

    /// <summary>
    /// Create a new game room with a custom name
    /// </summary>
    public void CreateRoom(string userId, string username, int avatarIndex, string roomName, Action<string> onSuccess, Action<string> onError)
    {
        if (APIService.Instance == null)
        {
            onError?.Invoke("APIService not available");
            return;
        }

        string safeRoomName = string.IsNullOrWhiteSpace(roomName) ? $"{username}'s Room" : roomName.Trim();

        APIService.Instance.Post<CreateRoomResponse>(
            "/multiplayer/rooms",
            new CreateRoomRequest
            {
                roomName = safeRoomName,
                visibility = "public"
            },
            (response) => {
                currentRoomId = response.roomId;
                localUserId = userId;
                isHost = true;
                
                // Connect WebSocket and join room
                if (WebSocketManager.Instance != null)
                {
                    var session = UserSession.LoadFromPlayerPrefs();
                    WebSocketManager.Instance.Connect(userId, username, session?.authToken);
                    WebSocketManager.Instance.JoinRoom(response.roomId, userId, username, avatarIndex);
                }
                
                onSuccess?.Invoke(response.roomId);
            },
            (error) => {
                onError?.Invoke(error);
            },
            true // requires auth
        );
    }

    /// <summary>
    /// Join an existing game room
    /// </summary>
    public void JoinRoom(string roomId, string userId, string username, int avatarIndex, Action onSuccess, Action<string> onError)
    {
        if (WebSocketManager.Instance == null)
        {
            onError?.Invoke("WebSocketManager not available");
            return;
        }

        if (string.IsNullOrEmpty(roomId) || string.IsNullOrEmpty(userId))
        {
            onError?.Invoke("Invalid room or user");
            return;
        }

        if (!string.IsNullOrEmpty(currentRoomId) && currentRoomId == roomId)
        {
            onSuccess?.Invoke();
            return;
        }

        if (!WebSocketManager.Instance.IsConnected)
        {
            var session = UserSession.LoadFromPlayerPrefs();
            WebSocketManager.Instance.Connect(userId, username, session?.authToken);
        }

        WebSocketManager.Instance.JoinRoom(roomId, userId, username, avatarIndex);
        Initialize(roomId, userId, false);
        onSuccess?.Invoke();
    }

    /// <summary>
    /// Set player ready status
    /// </summary>
    public void SetPlayerReady(bool isReady)
    {
        if (WebSocketManager.Instance != null && !string.IsNullOrEmpty(currentRoomId))
        {
            WebSocketManager.Instance.SetReady(currentRoomId, localUserId, isReady);
        }
    }

    /// <summary>
    /// Start the game (host only)
    /// </summary>
    public void StartGame()
    {
        if (!isHost)
        {
            OnError?.Invoke("Only host can start the game");
            return;
        }

        if (WebSocketManager.Instance != null && !string.IsNullOrEmpty(currentRoomId))
        {
            WebSocketManager.Instance.StartGame(currentRoomId, localUserId);
        }
    }

    /// <summary>
    /// Deal cards (host only)
    /// </summary>
    public void DealCards()
    {
        if (!isHost)
        {
            OnError?.Invoke("Only host can deal cards");
            return;
        }

        if (WebSocketManager.Instance != null && !string.IsNullOrEmpty(currentRoomId))
        {
            WebSocketManager.Instance.DealCards(currentRoomId, localUserId);
        }
    }

    /// <summary>
    /// Exchange cards
    /// </summary>
    public void ExchangeCards(int[] cardIndices)
    {
        if (WebSocketManager.Instance != null && !string.IsNullOrEmpty(currentRoomId))
        {
            WebSocketManager.Instance.ExchangeCards(currentRoomId, localUserId, cardIndices);
        }
    }

    /// <summary>
    /// Skip exchange
    /// </summary>
    public void SkipExchange()
    {
        if (WebSocketManager.Instance != null && !string.IsNullOrEmpty(currentRoomId))
        {
            WebSocketManager.Instance.SkipExchange(currentRoomId, localUserId);
        }
    }

    /// <summary>
    /// Send betting action
    /// </summary>
    public void SendBettingAction(string actionType, int? amount = null)
    {
        if (WebSocketManager.Instance != null && !string.IsNullOrEmpty(currentRoomId))
        {
            WebSocketManager.Instance.SendBettingAction(currentRoomId, localUserId, actionType, amount);
        }
    }

    // Event handlers
    private void HandlePlayerJoined(object data)
    {
        DebugConfig.Log("[MultiplayerManager] Player joined");
        UpdateGameStateFromServer(data);
    }

    private void HandlePlayerLeft(object data)
    {
        DebugConfig.Log("[MultiplayerManager] Player left");
        UpdateGameStateFromServer(data);
    }

    private void HandlePlayerReadyChanged(object data)
    {
        DebugConfig.Log("[MultiplayerManager] Player ready changed");
        UpdateGameStateFromServer(data);
    }

    private void HandleGameStarted(object data)
    {
        DebugConfig.Log("[MultiplayerManager] Game started");
        UpdateGameStateFromServer(data);
    }

    private void HandleCardsDealt(object data)
    {
        DebugConfig.Log("[MultiplayerManager] Cards dealt");
        UpdateGameStateFromServer(data);
    }

    private void HandleCardsExchanged(object data)
    {
        DebugConfig.Log("[MultiplayerManager] Cards exchanged");
        UpdateGameStateFromServer(data);
    }

    private void HandlePlayerExchanged(object data)
    {
        DebugConfig.Log("[MultiplayerManager] Player exchanged");
        UpdateGameStateFromServer(data);
    }

    private void HandlePlayerAction(object data)
    {
        DebugConfig.Log("[MultiplayerManager] Player action");
        UpdateGameStateFromServer(data);
    }

    private void HandlePhaseChanged(object data)
    {
        DebugConfig.Log("[MultiplayerManager] Phase changed");
        UpdateGameStateFromServer(data);
    }

    private void HandlePlayerDisconnected(object data)
    {
        DebugConfig.Log("[MultiplayerManager] Player disconnected");
        UpdateGameStateFromServer(data);
    }

    private void HandleWebSocketError(string error)
    {
        Debug.LogError($"[MultiplayerManager] WebSocket error: {error}");
        OnError?.Invoke(error);
    }

    private void HandleRoomDeleted(object data)
    {
        string roomId = currentRoomId;
        if (data is string json)
        {
            try
            {
                RoomDeletedPayload payload = JsonUtility.FromJson<RoomDeletedPayload>(json);
                if (payload != null && !string.IsNullOrEmpty(payload.roomId))
                {
                    roomId = payload.roomId;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MultiplayerManager] Failed to parse room_deleted payload: {ex.Message}");
            }
        }

        if (!string.IsNullOrEmpty(roomId))
        {
            OnRoomDeleted?.Invoke(roomId);
        }
    }

    /// <summary>
    /// Update local game state from server data
    /// </summary>
    private void UpdateGameStateFromServer(object serverData)
    {
        // Parse server data and update local game state
        // This will be implemented based on the actual server response format
        if (TryParseRoomSnapshot(serverData, out RoomSnapshot room))
        {
            CurrentRoom = room;
            ApplyRoomSnapshot(room);
        }

        if (gameState != null)
        {
            OnGameStateUpdated?.Invoke(gameState);
        }
    }

    private void ApplyRoomSnapshot(RoomSnapshot room)
    {
        if (gameState == null || room == null)
        {
            return;
        }

        gameState.pot = room.pot;
        gameState.currentBet = room.currentBet;
        gameState.anteAmount = room.anteAmount > 0 ? room.anteAmount : gameState.anteAmount;
        gameState.currentPlayerIndex = room.currentPlayerIndex;

        if (TryParseGamePhase(room.currentPhase, out GamePhase parsedPhase))
        {
            gameState.currentPhase = parsedPhase;
        }
    }

    private bool TryParseGamePhase(string phase, out GamePhase parsed)
    {
        parsed = GamePhase.None;
        if (string.IsNullOrEmpty(phase))
        {
            return false;
        }

        if (System.Enum.TryParse(phase, true, out GamePhase result))
        {
            parsed = result;
            return true;
        }

        return false;
    }

    private bool TryParseRoomSnapshot(object serverData, out RoomSnapshot room)
    {
        room = null;
        if (serverData == null)
        {
            return false;
        }

        if (serverData is string json)
        {
            try
            {
                RoomEventWrapper wrapper = JsonUtility.FromJson<RoomEventWrapper>(json);
                if (wrapper != null && wrapper.room != null)
                {
                    room = wrapper.room;
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MultiplayerManager] Failed to parse room JSON: {ex.Message}");
            }
        }

        if (TryParseRoomFromDictionary(serverData, out room))
        {
            return true;
        }

        return false;
    }

    private bool TryParseRoomFromDictionary(object serverData, out RoomSnapshot room)
    {
        room = null;
        IDictionary dataDict = serverData as IDictionary;
        if (dataDict == null)
        {
            return false;
        }

        if (!dataDict.Contains("room"))
        {
            return false;
        }

        IDictionary roomDict = dataDict["room"] as IDictionary;
        if (roomDict == null)
        {
            return false;
        }

        room = new RoomSnapshot
        {
            roomId = GetString(roomDict, "roomId"),
            hostId = GetString(roomDict, "hostId"),
            maxPlayers = GetInt(roomDict, "maxPlayers"),
            currentPhase = GetString(roomDict, "currentPhase"),
            pot = GetInt(roomDict, "pot"),
            currentBet = GetInt(roomDict, "currentBet"),
            anteAmount = GetInt(roomDict, "anteAmount"),
            currentPlayerIndex = GetInt(roomDict, "currentPlayerIndex"),
            isActive = GetBool(roomDict, "isActive"),
            lastWinnerId = GetString(roomDict, "lastWinnerId"),
            lastWinnerHandValue = GetInt(roomDict, "lastWinnerHandValue"),
            players = new List<RoomPlayerSnapshot>()
        };

        IList playersList = roomDict.Contains("players") ? roomDict["players"] as IList : null;
        if (playersList != null)
        {
            foreach (object playerObj in playersList)
            {
                IDictionary playerDict = playerObj as IDictionary;
                if (playerDict == null)
                {
                    continue;
                }

                room.players.Add(new RoomPlayerSnapshot
                {
                    userId = GetString(playerDict, "userId"),
                    username = GetString(playerDict, "username"),
                    avatarIndex = GetInt(playerDict, "avatarIndex", 0),
                    seatIndex = GetInt(playerDict, "seatIndex", -1),
                    tokenAmount = GetInt(playerDict, "tokenAmount", Constants.STARTING_CHIPS),
                    isReady = GetBool(playerDict, "isReady"),
                    isConnected = GetBool(playerDict, "isConnected")
                });
            }
        }

        return true;
    }

    private static string GetString(IDictionary dict, string key)
    {
        return dict != null && dict.Contains(key) && dict[key] != null ? dict[key].ToString() : null;
    }

    private static int GetInt(IDictionary dict, string key, int defaultValue = 0)
    {
        if (dict == null || !dict.Contains(key) || dict[key] == null)
        {
            return defaultValue;
        }

        if (dict[key] is long longValue)
        {
            return (int)longValue;
        }

        if (dict[key] is int intValue)
        {
            return intValue;
        }

        if (int.TryParse(dict[key].ToString(), out int parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static bool GetBool(IDictionary dict, string key, bool defaultValue = false)
    {
        if (dict == null || !dict.Contains(key) || dict[key] == null)
        {
            return defaultValue;
        }

        if (dict[key] is bool boolValue)
        {
            return boolValue;
        }

        if (bool.TryParse(dict[key].ToString(), out bool parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    public GameState GetGameState() => gameState;
    public bool IsHost => isHost;
    public string CurrentRoomId => currentRoomId;
    public string LocalUserId => localUserId;

    public void UpdateRoomSnapshot(RoomSnapshot room)
    {
        if (room == null)
        {
            return;
        }

        CurrentRoom = room;
        ApplyRoomSnapshot(room);
        OnGameStateUpdated?.Invoke(gameState);
    }

    public void LeaveRoomAndCleanup()
    {
        if (WebSocketManager.Instance != null && !string.IsNullOrEmpty(currentRoomId) && !string.IsNullOrEmpty(localUserId))
        {
            WebSocketManager.Instance.LeaveRoom(currentRoomId, localUserId);
        }

        if (isHost && !string.IsNullOrEmpty(currentRoomId) && APIService.Instance != null)
        {
            string roomId = currentRoomId;
            APIService.Instance.Delete<DeleteRoomResponse>(
                $"/multiplayer/rooms/{roomId}",
                (_) => { OnRoomDeleted?.Invoke(roomId); },
                (error) => { Debug.LogWarning($"[MultiplayerManager] Failed to delete room: {error}"); },
                true
            );
        }

        ClearRoomState();
    }

    private void ClearRoomState()
    {
        currentRoomId = null;
        isHost = false;
        CurrentRoom = null;
        playerMap.Clear();
    }
}

[Serializable]
public class RoomSnapshot
{
    public string roomId;
    public string hostId;
    public List<RoomPlayerSnapshot> players = new List<RoomPlayerSnapshot>();
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
public class RoomPlayerSnapshot
{
    public string userId;
    public string username;
    public int avatarIndex;
    public int seatIndex;
    public int tokenAmount;
    public bool isReady;
    public bool isConnected;
}

[Serializable]
internal class RoomEventWrapper
{
    public RoomSnapshot room;
}

[Serializable]
internal class RoomDeletedPayload
{
    public string roomId;
    public string reason;
}

// Response types
[Serializable]
public class CreateRoomResponse
{
    public string roomId;
    public string hostId;
    public int maxPlayers;
}

[Serializable]
public class CreateRoomRequest
{
    public string roomName;
    public string visibility;
}

[Serializable]
public class DeleteRoomResponse
{
    public string roomId;
}
