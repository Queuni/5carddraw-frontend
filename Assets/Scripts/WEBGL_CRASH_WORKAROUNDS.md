# WebGL "INVALID_ENUM / abort()" Crash – Workarounds

If the game crashes with **"WebGL: INVALID_ENUM: getInternalformatParameter"** and then **"abort() at Error"** or **"RuntimeError: unreachable"**, this is a **known Unity engine bug** (e.g. UUM-93245), not a bug in the game scripts. The engine calls a WebGL API with an internal format some browsers don’t support.

## What we changed in this project

- **WebGL quality level** is set to **0 (Very Low)** in `ProjectSettings/QualitySettings.asset` to use a simpler render path and reduce the chance of hitting the bad code path.
- Multiplayer exchange hand-update and animation timing were fixed so the game state stays consistent (this does not fix the engine crash but avoids other issues).

## What you can try (in order)

### 1. Use a different browser

- **Firefox** or **Safari** (especially on macOS) often do not hit this bug.
- Test in **Chrome Incognito** or **Edge InPrivate** with extensions disabled to rule out extensions.

### 2. In Unity: Force WebGL 1.0 (if your Unity version allows it)

- **Edit → Project Settings → Player**
- Select the **WebGL** tab.
- Open **Other Settings → Rendering**.
- **Uncheck "Auto Graphics API"**.
- In the list, **remove WebGL 2.0** and add/keep **WebGL 1.0** only (order: WebGL 1.0 first).
- Rebuild the WebGL build and test.

*(Note: In Unity 2022+, WebGL 1.0 may be deprecated or hidden; if you don’t see it, skip this step.)*

### 3. Upgrade Unity

- Check the [Unity Issue Tracker](https://issuetracker.unity3d.com/) for **"INVALID_ENUM getInternalformatParameter"** or **UUM-93245**.
- Newer 2022.3 LTS or 6.x patches may contain a fix; upgrade and rebuild.

### 4. Keep WebGL on “Very Low” quality

- The project is already set so **WebGL** uses quality level **0** in `QualitySettings.asset`.
- Do not raise the WebGL quality level until the engine bug is fixed or you’ve confirmed a workaround in your target browsers.

## If it still crashes

The fix has to come from Unity (engine/WebGL backend) or from using a browser/device where the bug doesn’t occur. There is no C# or project-setting change that can fully fix this inside the engine’s WebGL code path.
