# Koikatsu → Krita One-Click Bridge (v1.0.0)
Converts the current CharaStudio frame to a new layer in the active Krita document via a direct TCP bridge.
Includes C# plugin, Krita Python script, deployment notes and minimal build commands.

## Why this exists
Users wanted a single CharaStudio hotkey (`F12`) to import the current game view into Krita as a layer,
without a watcher, second hotkey, or manual file conversion.

## Current architecture
- **CharaStudio plugin**: captures screenshot, writes `latest.png`, posts `IMPORT <path>` to `127.0.0.1:9184`.
- **Krita Python script**: binds a TCP socket, triggers layer creation natively when data arrives.
- **No watcher**: only runs when data arrives; no idle polling.
- **One-click**: F12 inside CharaStudio → new layer appears in Krita.

## Build CharaStudio plugin
```powershell
cd src/cs
dotnet build -c Release -o deploy/KK_KritaBridge
```

## Install Krita script
- Copy `%AppData%\krita\scripts\KoikatsuBridge.py` from `src/py`.
- Enable it in Krita → Settings → Manage Resources.

## Run
1. Open Krita normally.
2. Open CharaStudio.
3. Press `F12` to take capture and import.
4. Run the provided `deploy/run-bridge.bat` if you need to bootstrap Krita with the script.

## Repo
This bundle is version 1.0.0 with git tags & release notes.
