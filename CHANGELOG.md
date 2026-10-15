# ChangeLog

## v1.7.14 (geometry-driven pairing)
- Pairing is computed from real button positions in the visible grid (finds the
  cell directly below the snapshot button) instead of "next sibling index".
- Logs each toolbar cell with x/y, the container's layout components and whether
  the reorder actually took effect (adjacent true/false).

## v1.7.13 (leftmost match wins)
- When several copies of a button exist (KKAPI logic objects vs visible clones),
  pairing uses the leftmost active one — the one actually drawn in the strip.

## v1.7.12 (label census)
- Pair log adds a global census of visible button labels, to find which UI
  framework the real strip uses (UGUI vs IMGUI vs TextMeshPro).

## v1.7.11 (visual strip pairing)
- Finds the real visual toolbar (the container holding Find) and pairs the
  buttons there; KKAPI's logic container is no longer mistaken for the grid.

## v1.7.10 (toolbar census)
- Pair log now lists every toolbar child with active state and icon tags, so a
  wrong match can be seen instead of guessed.

## v1.7.9 (find buttons by icon)
- Locates both toolbar buttons by their baked icon texture (immune to KKAPI
  internals and toolbar rebuilds), then glues live right below snapshot.

## v1.7.8 (ControlObject pairing)
- Uses KKAPI's documented ControlObject handle (re-resolved every check, immune
  to toolbar rebuilds) to glue live right below the snapshot button.

## v1.7.7 (pairing actually works)
- Fixed GameObject extraction (direct result + private base-class fields), so the
  live toggle is really found and glued right below the snapshot button.
- Snapshot keeps its KKAPI spot; no more forced move next to Find.

## v1.7.6 (stacked: capture on top)
- Snapshot button on top, live toggle glued right below it (same column).
- Pairing diagnostics in the game log (KKBridge: pair ...).

## v1.7.5 (same-row pairing)
- Detects the toolbar fill direction from real button positions and keeps both
  buttons in the same row (works with vertical or horizontal grids, re-checked live).

## v1.7.4 (paired buttons)
- Live toggle docks right after the snapshot button: both stay side by side.

## v1.7.3 (live REC icon)
- Live toggle uses the Krita icon with a red recording dot (white ring) in the corner.
- Sends without BOM at the source (Krita already tolerates both).

## v1.7.2 (verified live)
- Fix LIVE path slice [5:] + BOM-tolerant protocol + zero hardcoded paths.
- Final working state: live toggle updates the "KK live" layer.

## v1.7.1 (judder-free live)
- Krita: anti pile-up (only the latest pending frame is processed).
- Game: 5s Canvas cache + 1000ms default interval.

## v1.7.0 (live mode)
- Native toolbar toggle: captures every IntervalMs (500) only when the scene changes.
- Detection via 160x90 mini-render + diff (ThresholdPct 2); first frame always sent.
- Krita updates the "KK live" layer in place, no popups; 5s backoff when Krita is not listening.

## v1.6.6 (correct margin)
- Fix: margin now actually shrinks the icon (it used to zoom inversely).

## v1.6.5 (icon margin)
- IconMargin % configurable with live preview (12 by default).

## v1.6.4 (gradient)
- Vertical gradient background, light on top / dark at bottom, like the stock buttons.
- Top+bottom auto-sampling of the Find button; manual Top/Bottom override.

## v1.6.3 (auto gray)
- Auto-samples the gray of the real Find button and applies it (manual override: UI > ButtonBg).
- Default 60 + live re-render when the value changes.

## v1.6.1 (background + order)
- 32x32 icon with baked dark-gray background and rounded corners.
- Reorders the button next to Find via hierarchy (KKB-TOOLBAR log with children).

## v1.6.0 (native toolbar button)
- Studio toolbar registration via KKAPI CustomToolbarButtons through reflection (like KKPE/Timeline).
- 32x32 icon + retry every 2s until registered; floating button stays as fallback.
- Flat dark button to match the grid.

## v1.5.2 (next to Find)
- Defaults measured from your 1920x1080 capture: 42px button right of Find (108, 593).
- Live cfg updated with those values; DockFind off.

## v1.5.1 (manual placement)
- The Studio bar is IMGUI (no UGUI buttons): automatic docking impossible.
- Configurable size (UI > ButtonSize, 64 by default) and DockFind off by default.
- Drag by hand next to Find; position is saved on capture.

## v1.5.0 (docked to Find)
- Button sticks to the left cell of Find, same size, at any resolution.
- UI > DockFind toggle (off = manual draggable floating).
- "docked to Find button" / "Find lost" log for diagnostics.

## v1.4.0 (Krita icon button)
- Draggable 48px square button with the official Krita icon (krita_icon.png next to the DLL).
- Persistent position in settings (UI > ButtonX/ButtonY, saved on capture).
- Fallback to "K" button when the icon is missing.

## v1.3.0 (floating button)
- Draggable "KK Krita" IMGUI window with an "A Krita" button (excluded from captures).
- Settings toggle: UI > ShowButton (ConfigurationManager shows it automatically).
- Button hidden during render as a safeguard.

## v1.2.1 (final)
- Silent success: no popup on import, warnings only on errors.
- Verified working state: clean capture without UI + new layer in Krita.

## v1.2.0 (clean capture)
- Camera.main-only render to RenderTexture (IMGUI UI is not drawn there).
- UGUI Canvases disabled during render and restored afterwards (Screencap style).
- Log of every camera (name/depth/rect) to diagnose angles.
- Note: with the game open the DLL stays locked; close the game to update.

## v1.1.5 (full Krita 6 ABC)
- Self-registration with Krita.instance().addExtension (Krita 6 never calls createExtension).
- setup() and createActions() overrides required by the abstract base class.
- import-top marker and guards against silent failures.
- Removed X-Krita-AutoStart from .desktop (same as working plugins).

## v1.1.3 (PyQt6)
- Real Krita is 6.0.4/Qt6: ships PyQt6 6.8.1 (neither PyQt5 nor PySide6). Chain: PyQt6 -> PySide6 -> PyQt5.
- Verified against ai_diffusion, which uses from PyQt6... pyqtSignal in the same Krita.

## v1.1.2 (Krita 6)
- Real Krita is 6.0.4/Qt6: uses PySide6, not PyQt5. Dual import with fallback.
- Cause of "connection refused": the plugin never started because of the import.

## v1.1.1 (connection diagnostics)
- C#: 3 TCP attempts with 500ms pause; clear message when Krita is not listening.
- Krita: log at UserData/krita_bridge/kk_bridge.log (loads, listens, receives, imports).
- Krita: waits up to 3s for CaptureScreenshot to finish writing the PNG.

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
- Single `F12` hotkey imports the current scene as a new layer.
- No watcher; direct TCP socket between plugin and Krita script.
- Keeps `latest.png` output at `UserData/krita_bridge/latest.png`.
- Added deploy/run-bridge.bat for Krita bootstrap.
- Supports `dotnet build -c Release`.
