# ChangeLog

## v1.1.2 (Krita 6)
- Krita real es 6.0.4/Qt6: usa PySide6, no PyQt5. Import dual con fallback.
- Causa del "connection refused": el plugin nunca arranco por el import.

## v1.1.1 (connection diagnostics)
- C#: 3 intentos de TCP con pausa de 500ms; mensaje claro si Krita no escucha.
- Krita: log en UserData/krita_bridge/kk_bridge.log (carga, escucha, recepciones, imports).
- Krita: espera hasta 3s a que CaptureScreenshot termine de escribir el PNG.

## v1.1.0 (working install)
- Compiled KK_KritaBridge.dll targeting net35 (v2.0.50727), matching game runtime.
- Roslyn csc with /noconfig /nostdlib + net35 refs (no .NET SDK on machine).
- Unity is monolithic without ScreenCapture -> Application.CaptureScreenshot.
- DLL deployed to BepInEx/plugins/KK_KritaBridge/.
- Krita plugin as proper pykrita module (kk_krita_bridge) with .desktop + AutoStart.
- Signal/slot bridge (no polling); layer ops on Krita main thread.
- Pre-enabled in kritarc (enable_kk_krita_bridge=true).
- Python syntax verified with py_compile.

## v1.0.0 (first release)
- Initial CharaStudio -> Krita one-click bridge.
- Single `F12` hotkey import current scene as a new layer.
- No watcher; direct TCP socket between plugin and Krita script.
- Keep `latest.png` output at `UserData/krita_bridge/latest.png`.
- Added deploy/run-bridge.bat for Krita bootstrap.
- Supports `dotnet build -c Release`.
