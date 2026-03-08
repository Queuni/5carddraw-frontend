using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

// Socket.IO client wrapper for multiplayer events.
public class WebSocketManager : MonoBehaviour
{
    public static WebSocketManager Instance { get; private set; }

    public bool IsConnected => isConnected;

    public event Action<object> OnPlayerJoined;
    public event Action<object> OnPlayerLeft;
    public event Action<object> OnPlayerReadyChanged;
    public event Action<object> OnGameStarted;
    public event Action<object> OnCardsDealt;
    public event Action<object> OnCardsExchanged;
    public event Action<object> OnPlayerExchanged;
    public event Action<object> OnPlayerSkippedExchange;
    public event Action<object> OnPlayerAction;
    public event Action<object> OnPhaseChanged;
    public event Action<object> OnPlayerDisconnected;
    public event Action<object> OnRoomDeleted;
    public event Action<string> OnError;
    public event Action<int> OnGameStarting;
    public event Action<object> OnShowdownReveal;
    public event Action<object> OnShowdownHands;
    public event Action<object> OnShowdownResult;
    public event Action<object> OnRoundContinueState;
    public event Action<object> OnCardsDealtPublic;

    private bool isConnected;
    private SocketIOUnity socket;
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SocketBridge_Connect(string baseUrl, string token, string gameObjectName);

    [DllImport("__Internal")]
    private static extern void SocketBridge_Disconnect();

    [DllImport("__Internal")]
    private static extern void SocketBridge_Emit(string eventName, string jsonPayload);

    private static readonly bool UseWebGLSocket = true;
#else
    private static readonly bool UseWebGLSocket = false;

    private static void SocketBridge_Connect(string baseUrl, string token, string gameObjectName) { }
    private static void SocketBridge_Disconnect() { }
    private static void SocketBridge_Emit(string eventName, string jsonPayload) { }
#endif

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

    public void Connect(string userId, string username, string authToken)
    {
        string baseUrl = GetSocketBaseUrl();
        if (UseWebGLSocket)
        {
            DebugConfig.Log($"[WebSocketManager] Connecting to {baseUrl}...");
            SocketBridge_Connect(baseUrl, authToken ?? string.Empty, gameObject.name);
            return;
        }

        if (socket != null && socket.Connected)
        {
            return;
        }

        var uri = new Uri(baseUrl);
        var query = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(authToken))
        {
            query["token"] = authToken;
        }

        var options = new SocketIOOptions
        {
            EIO = EngineIO.V4,
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
            Query = query
        };

        socket = new SocketIOUnity(uri, options);
        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        RegisterSocketEvents();

        DebugConfig.Log($"[WebSocketManager] Connecting to {baseUrl}...");
        socket.Connect();
    }

    private string GetSocketBaseUrl()
    {
        string baseUrl = APIService.Instance != null ? APIService.Instance.GetBaseURL() : "http://localhost:3000/api";
        if (baseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = baseUrl.Substring(0, baseUrl.Length - 4);
        }

        return baseUrl;
    }

    public void JoinRoom(string roomId, string userId, string username, int avatarIndex)
    {
        if (UseWebGLSocket)
        {
            EmitWebGL("join_room", new JoinRoomPayload
            {
                roomId = roomId,
                userId = userId,
                username = username,
                avatarIndex = avatarIndex
            });
            return;
        }

        if (socket == null)
        {
            OnError?.Invoke("Socket not connected");
            return;
        }

        socket.Emit("join_room", new
        {
            roomId,
            userId,
            username,
            avatarIndex
        });
    }

    public void LeaveRoom(string roomId, string userId)
    {
        if (UseWebGLSocket)
        {
            EmitWebGL("leave_room", new RoomUserPayload
            {
                roomId = roomId,
                userId = userId
            });
            return;
        }

        if (socket == null)
        {
            return;
        }

        socket.Emit("leave_room", new
        {
            roomId,
            userId
        });
    }

    public void SetReady(string roomId, string userId, bool isReady)
    {
        if (UseWebGLSocket)
        {
            EmitWebGL("set_ready", new ReadyPayload
            {
                roomId = roomId,
                userId = userId,
                isReady = isReady
            });
            return;
        }

        if (socket == null)
        {
            return;
        }

        socket.Emit("set_ready", new
        {
            roomId,
            userId,
            isReady
        });
    }

    public void StartGame(string roomId, string userId)
    {
        if (UseWebGLSocket)
        {
            EmitWebGL("start_game", new RoomUserPayload
            {
                roomId = roomId,
                userId = userId
            });
            return;
        }

        if (socket == null)
        {
            return;
        }

        socket.Emit("start_game", new
        {
            roomId,
            userId
        });
    }

    public void DealCards(string roomId, string userId)
    {
        if (UseWebGLSocket)
        {
            EmitWebGL("deal_cards", new RoomUserPayload
            {
                roomId = roomId,
                userId = userId
            });
            return;
        }

        if (socket == null)
        {
            return;
        }

        socket.Emit("deal_cards", new
        {
            roomId,
            userId
        });
    }

    public void ExchangeCards(string roomId, string userId, int[] cardIndices)
    {
        if (UseWebGLSocket)
        {
            EmitWebGL("exchange_cards", new ExchangePayload
            {
                roomId = roomId,
                userId = userId,
                cardIndices = cardIndices
            });
            return;
        }

        if (socket == null)
        {
            return;
        }

        socket.Emit("exchange_cards", new
        {
            roomId,
            userId,
            cardIndices
        });
    }

    public void SkipExchange(string roomId, string userId)
    {
        if (UseWebGLSocket)
        {
            EmitWebGL("skip_exchange", new RoomUserPayload
            {
                roomId = roomId,
                userId = userId
            });
            return;
        }

        if (socket == null)
        {
            return;
        }

        socket.Emit("skip_exchange", new
        {
            roomId,
            userId
        });
    }

    public void SendBettingAction(string roomId, string userId, string actionType, int? amount)
    {
        if (UseWebGLSocket)
        {
            EmitWebGL("betting_action", new BettingPayload
            {
                roomId = roomId,
                userId = userId,
                action = new BettingActionPayload
                {
                    type = actionType,
                    amount = amount ?? 0
                }
            });
            return;
        }

        if (socket == null)
        {
            return;
        }

        socket.Emit("betting_action", new
        {
            roomId,
            userId,
            action = new
            {
                type = actionType,
                amount
            }
        });
    }

    public void SendRoundContinue(string roomId, string userId)
    {
        if (UseWebGLSocket)
        {
            EmitWebGL("round_continue", new RoomUserPayload
            {
                roomId = roomId,
                userId = userId
            });
            return;
        }

        if (socket == null)
        {
            return;
        }

        socket.Emit("round_continue", new
        {
            roomId,
            userId
        });
    }

    private void RegisterSocketEvents()
    {
        if (UseWebGLSocket)
        {
            return;
        }

        socket.OnConnected += (_, __) =>
        {
            isConnected = true;
            DebugConfig.Log("[WebSocketManager] Connected.");
        };
        socket.OnDisconnected += (_, reason) =>
        {
            isConnected = false;
            DebugConfig.LogWarning($"[WebSocketManager] Disconnected: {reason}");
        };
        socket.OnError += (_, error) =>
        {
            isConnected = false;
            DebugConfig.LogError($"[WebSocketManager] Error: {error}");
            OnError?.Invoke(error);
        };
        socket.OnReconnectAttempt += (_, attempt) =>
        {
            DebugConfig.LogWarning($"[WebSocketManager] Reconnect attempt {attempt}");
        };

        socket.OnUnityThread("player_joined", response => OnPlayerJoined?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("player_left", response => OnPlayerLeft?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("player_ready_changed", response => OnPlayerReadyChanged?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("game_started", response => OnGameStarted?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("cards_dealt", response => OnCardsDealt?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("cards_dealt_public", response => OnCardsDealtPublic?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("cards_exchanged", response => OnCardsExchanged?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("player_exchanged", response => OnPlayerExchanged?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("player_skipped_exchange", response => OnPlayerSkippedExchange?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("player_action", response => OnPlayerAction?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("phase_changed", response => OnPhaseChanged?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("player_disconnected", response => OnPlayerDisconnected?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("room_deleted", response => OnRoomDeleted?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("game_starting", response => OnGameStarting?.Invoke(ExtractCountdownSeconds(response)));
        socket.OnUnityThread("showdown_reveal", response => OnShowdownReveal?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("showdown_hands", response => OnShowdownHands?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("showdown_result", response => OnShowdownResult?.Invoke(ExtractJson(response)));
        socket.OnUnityThread("round_continue_state", response => OnRoundContinueState?.Invoke(ExtractJson(response)));

        socket.OnUnityThread("join_room_error", response => OnError?.Invoke(ExtractError(response)));
        socket.OnUnityThread("start_game_error", response => OnError?.Invoke(ExtractError(response)));
        socket.OnUnityThread("deal_cards_error", response => OnError?.Invoke(ExtractError(response)));
        socket.OnUnityThread("exchange_cards_error", response => OnError?.Invoke(ExtractError(response)));
        socket.OnUnityThread("skip_exchange_error", response => OnError?.Invoke(ExtractError(response)));
        socket.OnUnityThread("betting_action_error", response => OnError?.Invoke(ExtractError(response)));
        socket.OnUnityThread("advance_phase_error", response => OnError?.Invoke(ExtractError(response)));
        socket.OnUnityThread("round_continue_error", response => OnError?.Invoke(ExtractError(response)));
    }

    public void OnSocketEvent(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        SocketEventWrapper wrapper = JsonUtility.FromJson<SocketEventWrapper>(json);
        if (wrapper == null || string.IsNullOrEmpty(wrapper.eventName))
        {
            return;
        }

        string payload = wrapper.payload;

        switch (wrapper.eventName)
        {
            case "player_joined":
                OnPlayerJoined?.Invoke(payload);
                break;
            case "player_left":
                OnPlayerLeft?.Invoke(payload);
                break;
            case "player_ready_changed":
                OnPlayerReadyChanged?.Invoke(payload);
                break;
            case "game_started":
                OnGameStarted?.Invoke(payload);
                break;
            case "cards_dealt":
                OnCardsDealt?.Invoke(payload);
                break;
            case "cards_dealt_public":
                OnCardsDealtPublic?.Invoke(payload);
                break;
            case "cards_exchanged":
                OnCardsExchanged?.Invoke(payload);
                break;
            case "player_exchanged":
                OnPlayerExchanged?.Invoke(payload);
                break;
            case "player_skipped_exchange":
                OnPlayerSkippedExchange?.Invoke(payload);
                break;
            case "player_action":
                OnPlayerAction?.Invoke(payload);
                break;
            case "phase_changed":
                OnPhaseChanged?.Invoke(payload);
                break;
            case "player_disconnected":
                OnPlayerDisconnected?.Invoke(payload);
                break;
            case "room_deleted":
                OnRoomDeleted?.Invoke(payload);
                break;
            case "game_starting":
                OnGameStarting?.Invoke(ExtractCountdownSecondsFromJson(payload));
                break;
            case "showdown_reveal":
                OnShowdownReveal?.Invoke(payload);
                break;
            case "showdown_hands":
                OnShowdownHands?.Invoke(payload);
                break;
            case "showdown_result":
                OnShowdownResult?.Invoke(payload);
                break;
            case "round_continue_state":
                OnRoundContinueState?.Invoke(payload);
                break;
            case "join_room_error":
            case "start_game_error":
            case "deal_cards_error":
            case "exchange_cards_error":
            case "skip_exchange_error":
            case "betting_action_error":
            case "advance_phase_error":
            case "round_continue_error":
                OnError?.Invoke(ExtractErrorFromJson(payload));
                break;
        }
    }

    public void OnSocketError(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            OnError?.Invoke(message);
        }
    }

    public void OnSocketState(string state)
    {
        if (string.IsNullOrEmpty(state))
        {
            return;
        }

        if (state == "connected")
        {
            isConnected = true;
        }
        else if (state == "disconnected")
        {
            isConnected = false;
        }
    }

    private string ExtractJson(SocketIOResponse response)
    {
        try
        {
            if (response == null)
            {
                return null;
            }

            var value = response.GetValue();
            return value.GetRawText();
        }
        catch (Exception ex)
        {
            DebugConfig.LogWarning($"[WebSocketManager] Failed to parse payload: {ex.Message}");
            return null;
        }
    }

    private string ExtractError(SocketIOResponse response)
    {
        try
        {
            if (response == null)
            {
                return "Socket error";
            }

            var value = response.GetValue();
            string json = value.GetRawText();
            if (!string.IsNullOrEmpty(json) && json.Contains("error"))
            {
                return json;
            }
        }
        catch (Exception ex)
        {
            DebugConfig.LogWarning($"[WebSocketManager] Failed to parse error payload: {ex.Message}");
        }

        return "Socket error";
    }

    private int ExtractCountdownSeconds(SocketIOResponse response)
    {
        if (response == null)
        {
            return 3;
        }

        try
        {
            string json = response.GetValue().GetRawText();
            if (string.IsNullOrEmpty(json))
            {
                return 3;
            }

            GameStartingPayload payload = JsonUtility.FromJson<GameStartingPayload>(json);
            if (payload != null && payload.seconds > 0)
            {
                return payload.seconds;
            }
        }
        catch (Exception ex)
        {
            DebugConfig.LogWarning($"[WebSocketManager] Failed to parse game_starting payload: {ex.Message}");
        }

        return 3;
    }

    private int ExtractCountdownSecondsFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return 3;
        }

        try
        {
            GameStartingPayload payload = JsonUtility.FromJson<GameStartingPayload>(json);
            if (payload != null && payload.seconds > 0)
            {
                return payload.seconds;
            }
        }
        catch (Exception ex)
        {
            DebugConfig.LogWarning($"[WebSocketManager] Failed to parse game_starting payload: {ex.Message}");
        }

        return 3;
    }

    private string ExtractErrorFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return "Socket error";
        }

        try
        {
            ErrorPayload payload = JsonUtility.FromJson<ErrorPayload>(json);
            if (payload != null && !string.IsNullOrEmpty(payload.error))
            {
                return payload.error;
            }
        }
        catch (Exception ex)
        {
            DebugConfig.LogWarning($"[WebSocketManager] Failed to parse error payload: {ex.Message}");
        }

        return json;
    }

    private async void OnApplicationQuit()
    {
        if (UseWebGLSocket)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SocketBridge_Disconnect();
#endif
            return;
        }

        if (socket != null && socket.Connected)
        {
            await socket.DisconnectAsync();
        }
        socket?.Dispose();
    }

    private void EmitWebGL(string eventName, object payload)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string json = payload != null ? JsonUtility.ToJson(payload) : "{}";
        SocketBridge_Emit(eventName, json);
#endif
    }

    [Serializable]
    private class GameStartingPayload
    {
        public int seconds;
        public string message;
    }

    [Serializable]
    private class SocketEventWrapper
    {
        public string eventName;
        public string payload;
    }

    [Serializable]
    private class ErrorPayload
    {
        public string error;
    }

    [Serializable]
    private class RoomUserPayload
    {
        public string roomId;
        public string userId;
    }

    [Serializable]
    private class JoinRoomPayload
    {
        public string roomId;
        public string userId;
        public string username;
        public int avatarIndex;
    }

    [Serializable]
    private class ReadyPayload
    {
        public string roomId;
        public string userId;
        public bool isReady;
    }

    [Serializable]
    private class ExchangePayload
    {
        public string roomId;
        public string userId;
        public int[] cardIndices;
    }

    [Serializable]
    private class BettingPayload
    {
        public string roomId;
        public string userId;
        public BettingActionPayload action;
    }

    [Serializable]
    private class BettingActionPayload
    {
        public string type;
        public int amount;
    }
}
