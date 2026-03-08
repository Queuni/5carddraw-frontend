mergeInto(LibraryManager.library, {
  SocketBridge_Connect: function(baseUrlPtr, tokenPtr, gameObjectPtr) {
    var baseUrl = UTF8ToString(baseUrlPtr);
    var token = UTF8ToString(tokenPtr);
    var gameObjectName = UTF8ToString(gameObjectPtr);

    if (!window.SocketBridge) {
      window.SocketBridge = {
        socket: null,
        gameObjectName: null,
        scriptLoading: false,
        pendingConnect: null
      };
    }

    var bridge = window.SocketBridge;
    bridge.gameObjectName = gameObjectName;

    var sendState = function(state) {
      if (bridge.gameObjectName) {
        SendMessage(bridge.gameObjectName, "OnSocketState", state);
      }
    };

    var sendError = function(message) {
      if (bridge.gameObjectName) {
        SendMessage(bridge.gameObjectName, "OnSocketError", message || "Socket error");
      }
    };

    var forwardEvent = function(eventName, payload) {
      if (!bridge.gameObjectName) {
        return;
      }
      var message = JSON.stringify({
        eventName: eventName,
        payload: JSON.stringify(payload || {})
      });
      SendMessage(bridge.gameObjectName, "OnSocketEvent", message);
    };

    var connectNow = function() {
      if (!window.io) {
        sendError("Socket.IO client not loaded");
        return;
      }

      if (bridge.socket) {
        bridge.socket.disconnect();
        bridge.socket = null;
      }

      var opts = { transports: ["websocket"] };
      if (token && token.length > 0) {
        opts.query = { token: token };
      }

      bridge.socket = window.io(baseUrl, opts);

      bridge.socket.on("connect", function() {
        sendState("connected");
      });
      bridge.socket.on("disconnect", function() {
        sendState("disconnected");
      });
      bridge.socket.on("connect_error", function(err) {
        sendError(err && err.message ? err.message : "Socket connect error");
      });

      var forwardEvents = [
        "player_joined",
        "player_left",
        "player_ready_changed",
        "game_started",
        "cards_dealt",
        "cards_dealt_public",
        "cards_exchanged",
        "player_exchanged",
        "player_skipped_exchange",
        "player_action",
        "phase_changed",
        "player_disconnected",
        "room_deleted",
        "game_starting",
        "showdown_reveal",
        "showdown_hands",
        "showdown_result",
        "round_continue_state",
        "join_room_error",
        "start_game_error",
        "deal_cards_error",
        "exchange_cards_error",
        "skip_exchange_error",
        "betting_action_error",
        "advance_phase_error",
        "round_continue_error"
      ];

      forwardEvents.forEach(function(eventName) {
        bridge.socket.on(eventName, function(payload) {
          forwardEvent(eventName, payload);
        });
      });
    };

    if (window.io) {
      connectNow();
      return;
    }

    if (bridge.scriptLoading) {
      bridge.pendingConnect = connectNow;
      return;
    }

    bridge.scriptLoading = true;
    var script = document.createElement("script");
    script.src = "https://cdn.socket.io/4.7.5/socket.io.min.js";
    script.onload = function() {
      bridge.scriptLoading = false;
      connectNow();
      if (bridge.pendingConnect) {
        bridge.pendingConnect();
        bridge.pendingConnect = null;
      }
    };
    script.onerror = function() {
      bridge.scriptLoading = false;
      sendError("Failed to load Socket.IO client");
    };
    document.head.appendChild(script);
  },

  SocketBridge_Disconnect: function() {
    if (window.SocketBridge && window.SocketBridge.socket) {
      window.SocketBridge.socket.disconnect();
      window.SocketBridge.socket = null;
    }
  },

  SocketBridge_Emit: function(eventPtr, jsonPtr) {
    if (!window.SocketBridge || !window.SocketBridge.socket) {
      return;
    }
    var eventName = UTF8ToString(eventPtr);
    var json = UTF8ToString(jsonPtr);
    var payload = {};
    if (json && json.length > 0) {
      try {
        payload = JSON.parse(json);
      } catch (e) {
        payload = {};
      }
    }
    window.SocketBridge.socket.emit(eventName, payload);
  }
});
