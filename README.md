# KK Krita Bridge — CharaStudio → Krita one-click + live

Sends the current CharaStudio view into the active Krita document as a new layer.
Two ways to trigger it: `F12` (single capture) and a native toolbar toggle (live mode,
which re-sends the frame only when the scene actually changes).

Requires: Koikatsu with BepInEx (CharaStudio), Krita 6, both on the same PC.

---

## Install from ZIPs (recommended)

Download the two ZIPs from
[Releases](https://github.com/NegativeMeta/kk_krita/releases)
(`kk_krita-koikatsu-<version>.zip` and `kk_krita-krita-<version>.zip`).

### A. Koikatsu side (game)

1. **Close the game completely** (the DLL stays locked while it runs).
2. Unzip `kk_krita-koikatsu-<version>.zip`.
3. Copy its contents (`KK_KritaBridge.dll`, `krita_icon.png`, `install.txt`) into:
   `<game>/BepInEx/plugins/KK_KritaBridge/`
4. Open CharaStudio. The Krita button appears in the Studio toolbar.

### B. Krita side

1. Unzip `kk_krita-krita-<version>.zip`.
2. Copy its contents (`kk_krita_bridge/` folder + `kk_krita_bridge.desktop`) into:
   `%APPDATA%/krita/pykrita/`
3. In Krita: Settings → Configure Krita → Python Plugin Manager →
   enable **KK Krita One Click** → OK.
4. **Restart Krita.** It listens on `127.0.0.1:9184` from now on.

---

## Install from source

### A. Build the Koikatsu plugin

Prerequisite: .NET SDK **or** the Roslyn compiler from Visual Studio Build Tools.

```powershell
cd src/cs
dotnet build -c Release
```

No .NET SDK? Compile directly with Roslyn (targets net35, the game's runtime):

```powershell
& "C:/Program Files (x86)/Microsoft Visual Studio/2022/BuildTools/MSBuild/Current/Bin/Roslyn/csc.exe" `
  /nologo /noconfig /t:library /out:KK_KritaBridge.dll /nostdlib `
  /r:C:/Windows/Microsoft.NET/Framework/v2.0.50727/mscorlib.dll `
  /r:C:/Windows/Microsoft.NET/Framework/v2.0.50727/System.dll `
  /r:"<game>/BepInEx/core/BepInEx.dll" `
  /r:"<game>/CharaStudio_Data/Managed/UnityEngine.dll" `
  /r:"<game>/CharaStudio_Data/Managed/UnityEngine.UI.dll" `
  KoikatsuOneClick.cs
```

Then (game closed) copy to `<game>/BepInEx/plugins/KK_KritaBridge/`:

- the built `KK_KritaBridge.dll`
- `deploy/krita_icon.png`

### B. Install the Krita script from source

1. Copy `src/py/kk_krita_bridge/` and `src/py/kk_krita_bridge.desktop` into
   `%APPDATA%/krita/pykrita/`.
2. Enable **KK Krita One Click** in Settings → Configure Krita → Python Plugin Manager.
3. Restart Krita.

Verify syntax beforehand with `python -m py_compile` on the script.

---

## Usage

- `F12` in CharaStudio → clean capture (no UI) as a new `KK capture` layer.
- Toolbar snapshot button → same, clickable.
- Toolbar toggle → live mode: the `KK live` layer updates only when the scene
  changes (idle animation counts as change; nothing is sent for a static scene).
- Settings (BepInEx ConfigurationManager, `johiny.kkritabridge.cfg`):
  `Live > IntervalMs` (default 1000), `Live > ThresholdPct` (default 2),
  `UI > IconMargin`, `Bridge > Port` (must match Krita, default 9184).

## How it works

- Game renders `Camera.main` to a texture, writes `UserData/krita_bridge/latest.png`,
  and sends `IMPORT <path>` / `LIVE <path>` over TCP to `127.0.0.1:9184`.
- The Krita script receives it on its main thread and paints a layer in place
  (no popups, no polling, no files to move by hand).
- Game log: `UserData/krita_bridge/kk_bridge.log` next to the PNG
  (falls back to `%TEMP%/kk_bridge.log`).
