# 5 Card Draw

A **Unity** poker game featuring single-player vs CPU and online multiplayer. Built with Unity 2022.3 and playable on **WebGL** and **Android**.

---

## Overview

**5 Card Draw** is a classic poker game where each player is dealt five cards and may exchange cards before the final showdown. This project is the frontend client: a Unity game that connects to a backend API and WebSocket server for authentication, matchmaking, and real-time multiplayer.

| Mode            | Description                                      |
|-----------------|--------------------------------------------------|
| **Single Player** | Play against CPU opponents with hand evaluation. |
| **Multiplayer**   | Join rooms and play with others in real time.   |

---

## Requirements

- **Unity 2022.3** (LTS) or compatible (e.g. 2022.3.62f2)
- Backend API and WebSocket server (see [Backend](#backend) below)

---

## Project Structure

```
Assets/
├── Scenes/           # Game scenes
│   ├── SplashScene
│   ├── RegisterScene / LoginScene
│   ├── MainMenuScene
│   ├── SinglePlayScene    # Single player vs CPU
│   ├── RoomScene          # Lobby / room list
│   ├── MultiPlayScene     # Online multiplayer
│   ├── LeaderboardScene
│   ├── ProfileScene / SettingScene
│   └── RulesScene
├── Scripts/
│   ├── Core/          # HandEvaluator, Rules, UserSession, Utils, etc.
│   ├── Manager/       # Auth, API, WebSocket, GameFlow, Betting, CPU AI
│   ├── UI/            # Scene controllers (MainMenu, Login, Room, etc.)
│   └── Prefab/        # Card, Player, RoomItem, UI components
├── Prefabs/
├── Resources/         # Fonts, deploy (WebGL index_template.html)
├── Demigiant/DOTween # Animation
├── TextMesh Pro/     # Text rendering
└── Plugins/          # Android (e.g. mainTemplate.gradle)
Packages/              # manifest.json (NativeWebSocket, SocketIO, etc.)
ProjectSettings/       # Unity version, build settings, quality
```

---

## Getting Started

### 1. Clone and open in Unity

```bash
git clone <repository-url>
cd 5carddraw-frontend
```

Open the project folder in **Unity Hub** and open with Unity **2022.3.x**.

### 2. Backend

The game expects:

- **REST API** at `http://localhost:3000/api` in the Editor / development builds.
- **Production API** at `https://5carddraw.app/api` in release builds.
- **WebSocket** server for multiplayer (connection is managed by `WebSocketManager` using the current user session).

Configure or override URLs in the **APIService** component if your backend runs elsewhere.

### 3. Run in Editor

1. Open **SplashScene** (or the first scene in **File → Build Settings**).
2. Press **Play**.

Flow: Splash → Login/Register (or guest) → Main Menu → Single Play or Online (Room → Multi Play).

---

## Build

### WebGL

1. **File → Build Settings** → Platform **WebGL** → Switch Platform (if needed).
2. Build (and optionally copy to `StreamingAssets` as required by your deploy setup).
3. Deploy the build output together with your HTML/loader. A custom loader template is in `Assets/Resources/deploy/index_template.html` (update `BUILD_VERSION` and `BUILD_FOLDER` per build).

### Android

1. **File → Build Settings** → Platform **Android** → Switch Platform.
2. Configure **Player Settings** (package name, etc.).
3. Build APK/AAB. Android Gradle template: `Assets/Plugins/Android/mainTemplate.gradle`.

---

## Main Features

- **Single player**: 5-card draw vs CPU; full hand ranking (e.g. Royal Flush, Straight Flush, Four of a Kind, Full House, Flush, Straight, Three of a Kind, Two Pair, One Pair, High Card).
- **Multiplayer**: Rooms, matchmaking, real-time play over WebSocket.
- **Auth**: Guest mode, login, register; session used for API and WebSocket.
- **Profile & leaderboard**: User profile and global leaderboard (requires backend).
- **Settings**: In-game settings scene.
- **Rules**: In-game rules screen.

---

## Key Dependencies (Packages)

- **com.unity.inputsystem** – New Input System
- **com.unity.textmeshpro** – UI text
- **com.unity.ugui** – UI
- **NativeWebSocket** (GitHub) – WebSocket client
- **SocketIOUnity** (GitHub) – Socket.IO client
- **Unity UI Rounded Corners** (GitHub) – Rounded UI
- **DOTween** (in Assets) – Animations

See `Packages/manifest.json` for the full list.

---

## Configuration

- **API URLs**: In the scene/object that holds **APIService**, set:
  - `localBackendURL` – used in Editor and debug builds.
  - `productionBackendURL` – used in release builds.
- **Debug**: Use **DebugConfig** (and any project-specific debug flags) to control logging.

---

## License & Credits

- **Company / product**: Belle View Best – 5 Card Draw (see WebGL template in `Assets/Resources/deploy/`).
- Third-party assets and packages are subject to their own licenses (e.g. DOTween, TextMesh Pro, Socket.IO, NativeWebSocket).

---

## Version

- **Unity**: 2022.3.62f2  
- **Game**: version referenced in WebGL deploy template (e.g. 1.0.2); update there when releasing new builds.
