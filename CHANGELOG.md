# ChangeLog

## v1.5.2 (junto a Find)
- Defaults medidos de tu captura 1920x1080: boton 42px a la derecha de Find (108, 593).
- cfg vivo actualizado con esos valores; DockFind off.

## v1.5.1 (posicion manual)
- La barra del Studio es IMGUI (sin botones UGUI): anclaje automatico imposible.
- Tamano configurable (UI > ButtonSize, 64 por defecto) y DockFind off por defecto.
- Arrastrar a mano junto a Find; la posicion se guarda al capturar.

## v1.5.0 (anclado a Find)
- El boton se pega a la celda izquierda de Find, mismo tamano, a cualquier resolucion.
- Toggle UI > DockFind (off = flotante arrastrable manual).
- Log "anclado al boton Find" o "Find perdido" para diagnosticar.

## v1.4.0 (boton icono Krita)
- Boton cuadrado 48px arrastrable con el icono oficial de Krita (krita_icon.png junto al DLL).
- Posicion persistente en settings (UI > ButtonX/ButtonY, se guarda al capturar).
- Fallback a boton "K" si falta el icono.

## v1.3.0 (boton flotante)
- Ventana IMGUI arrastrable "KK Krita" con boton "A Krita" (no sale en la captura).
- Toggle en settings: UI > ShowButton (ConfigurationManager lo muestra solo).
- El boton se oculta durante el render por seguridad.

## v1.2.1 (final)
- Exito silencioso: sin popup al importar, solo avisos en caso de error.
- Estado verificado funcionando: captura limpia sin UI + capa nueva en Krita.

## v1.2.0 (captura limpia)
- Render solo de Camera.main a RenderTexture (la UI IMGUI no se dibuja ahi).
- Canvas UGUI apagados durante el render y restaurados despues (estilo Screencap).
- Log de todas las camaras (nombre/depth/rect) para diagnosticar angulo.
- Nota: con el juego abierto el DLL queda bloqueado; cerrar el juego para actualizar.

## v1.1.5 (Krita 6 ABC completo)
- Autorregistro con Krita.instance().addExtension (Krita 6 no llama a createExtension).
- Overrides setup() y createActions() exigidos por la clase base abstracta.
- Marcador import-top y guardias para fallos silenciosos.
- Quitado X-Krita-AutoStart del .desktop (igual que plugins que funcionan).

## v1.1.3 (PyQt6)
- Krita 6.0.4 trae PyQt6 6.8.1 (ni PyQt5 ni PySide6). Cadena: PyQt6 -> PySide6 -> PyQt5.
- Verificado contra ai_diffusion, que usa from PyQt6... pyqtSignal en el mismo Krita.

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
