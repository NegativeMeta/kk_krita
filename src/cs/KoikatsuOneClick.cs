using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace KK_KritaBridge
{
    [BepInPlugin("johiny.kkritabridge", "KK Krita One Click", "1.6.1")]
    public class KKOneClick : BaseUnityPlugin
    {
        ConfigEntry<KeyCode> CaptureKey;
        ConfigEntry<string> BridgeDir;
        ConfigEntry<string> TargetHost;
        ConfigEntry<int> TargetPort;
        ConfigEntry<bool> ShowButton;
        ConfigEntry<int> BtnX;
        ConfigEntry<int> BtnY;
        ConfigEntry<int> BtnSize;
        ConfigEntry<bool> DockFind;
        RectTransform findRect;
        int scanCooldown;
        bool hideOwnUI;
        bool nativeOk;
        int frame;
        const int windowId = 987121;
        Texture2D kritaIcon;
        Rect windowRect;
        GUIStyle emptyWinStyle;
        GUIStyle iconBtnStyle;

        const string latest = "latest.png";
        const string status = "capture.status";

        void Awake()
        {
            CaptureKey  = Config.Bind("Hotkey", "CaptureKey", KeyCode.F12);
            ShowButton  = Config.Bind("UI", "ShowButton", true, "Muestra el boton flotante para enviar a Krita");
            BtnX  = Config.Bind("UI", "ButtonX", 108, "Posicion X del boton");
            BtnY  = Config.Bind("UI", "ButtonY", 593, "Posicion Y del boton");
            BtnSize  = Config.Bind("UI", "ButtonSize", 42, "Tamano del boton");
            DockFind  = Config.Bind("UI", "DockFind", false, "Pegar el boton al lado de Find");
            windowRect = new Rect(BtnX.Value, BtnY.Value, BtnSize.Value, BtnSize.Value);
            LoadIcon();
            BridgeDir   = Config.Bind("Bridge", "BridgeDir", "UserData/krita_bridge");
            TargetHost  = Config.Bind("Bridge", "Host", "127.0.0.1");
            TargetPort  = Config.Bind("Bridge", "Port", 9184);
            Logger.LogInfo("KK Krita One Click ready");
        }

        void Update()
        {
            if (Input.GetKeyDown(CaptureKey.Value)) CapturarYEnviar();
            if (!nativeOk && ((frame++ % 120) == 0)) nativeOk = TryRegisterNativeButton();
        }

        void OnGUI()
        {
            if (!ShowButton.Value || hideOwnUI || nativeOk) return;
            if (emptyWinStyle == null) emptyWinStyle = new GUIStyle();
            if (DockFind.Value) DockToFind();
            windowRect = GUI.Window(windowId, windowRect, IconWindow, GUIContent.none, emptyWinStyle);
        }

        void IconWindow(int id)
        {
            if (iconBtnStyle == null)
            {
                iconBtnStyle = new GUIStyle();
                iconBtnStyle.normal.background = MakeTex(64, 64, 64);
                iconBtnStyle.hover.background = MakeTex(82, 82, 82);
                iconBtnStyle.active.background = MakeTex(48, 48, 48);
                iconBtnStyle.focused.background = MakeTex(64, 64, 64);
                iconBtnStyle.padding = new RectOffset(5, 5, 5, 5);
            }
            float s = Mathf.Min(windowRect.width, windowRect.height);
            Rect r = new Rect(0, 0, s, s);
            if (kritaIcon != null)
            {
                if (GUI.Button(r, kritaIcon, iconBtnStyle)) CapturarYEnviar();
            }
            else if (GUI.Button(r, "K", iconBtnStyle))
            {
                CapturarYEnviar();
            }
            if (!DockFind.Value) GUI.DragWindow();
        }

        Texture2D MakeTex(byte r, byte g, byte b)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, new Color32(r, g, b, 255));
            t.Apply();
            return t;
        }

        void DockToFind()
        {
            try
            {
                if (findRect == null || ((scanCooldown++ % 60) == 0))
                {
                    bool had = (findRect != null);
                    findRect = null;
                    UnityEngine.Object[] objs = UnityEngine.Object.FindObjectsOfType(typeof(Button));
                    foreach (UnityEngine.Object o in objs)
                    {
                        Button b = o as Button;
                        if (b == null) continue;
                        Text[] texts = b.GetComponentsInChildren<Text>(true);
                        foreach (Text t in texts)
                        {
                            if (t != null && t.text == "Find") { findRect = b.transform as RectTransform; break; }
                        }
                        if (findRect != null) break;
                    }
                    if (findRect == null)
                    {
                        if (had) Logger.LogInfo("KKBridge: Find perdido, uso posicion manual");
                    }
                    else if (!had) Logger.LogInfo("KKBridge: anclado al boton Find");
                }
                if (findRect == null) return;
                Vector3[] c = new Vector3[4];
                findRect.GetWorldCorners(c);
                Canvas cv = findRect.GetComponentInParent<Canvas>();
                float x0 = c[0].x, y0 = c[0].y, x1 = c[2].x, y1 = c[2].y;
                if (cv != null && cv.renderMode == RenderMode.ScreenSpaceCamera && cv.worldCamera != null)
                {
                    Vector3 bl = cv.worldCamera.WorldToScreenPoint(c[0]);
                    Vector3 tr = cv.worldCamera.WorldToScreenPoint(c[2]);
                    x0 = bl.x; y0 = bl.y; x1 = tr.x; y1 = tr.y;
                }
                float w = Mathf.Abs(x1 - x0);
                float h = Mathf.Abs(y1 - y0);
                if (w < 8f || h < 8f) return;
                windowRect = new Rect(x0 - w - 4f, Screen.height - y1, w, h);
            }
            catch (Exception e)
            {
                Logger.LogError("Dock fallo: " + e.Message);
            }
        }

        void LoadIcon()
        {
            try
            {
                string dir = Path.GetDirectoryName(typeof(KKOneClick).Assembly.Location);
                string iconPath = Path.Combine(dir, "krita_icon.png");
                if (File.Exists(iconPath))
                {
                    kritaIcon = new Texture2D(2, 2);
                    kritaIcon.LoadImage(File.ReadAllBytes(iconPath));
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Icon load failed: " + e.Message);
            }
        }

        bool TryRegisterNativeButton()
        {
            try
            {
                Assembly kkapi = null;
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (a.GetName().Name == "KKAPI") { kkapi = a; break; }
                }
                if (kkapi == null) return false;
                Type t = kkapi.GetType("KKAPI.Studio.UI.CustomToolbarButtons");
                if (t == null) return false;
                MethodInfo m = null;
                MethodInfo[] methods = t.GetMethods();
                foreach (MethodInfo cand in methods)
                {
                    if (cand.Name == "AddLeftToolbarButton" && cand.IsStatic && cand.IsPublic)
                    {
                        ParameterInfo[] ps = cand.GetParameters();
                        if (ps.Length == 2 && ps[0].ParameterType == typeof(Texture2D)) { m = cand; break; }
                    }
                }
                if (m == null) return false;
                if (kritaIcon == null) return false;
                Texture2D icon32 = BakeToolbarIcon(kritaIcon);
                if (icon32 == null) return false;
                Type actionType = m.GetParameters()[1].ParameterType;
                Delegate onClick = Delegate.CreateDelegate(actionType, this, "CapturarYEnviar");
                object result = m.Invoke(null, new object[] { icon32, onClick });
                if (result == null) return false;
                Logger.LogInfo("KKBridge: boton nativo registrado en toolbar");
                SetupToolbarPosition(result);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError("Native button failed: " + e.Message);
                return false;
            }
        }

        Texture2D BakeToolbarIcon(Texture2D src)
        {
            try
            {
                int S = 32;
                Color bg = new Color(0.25f, 0.25f, 0.25f, 1f);
                Texture2D dst = new Texture2D(S, S);
                for (int y = 0; y < S; y++)
                {
                    for (int x = 0; x < S; x++)
                    {
                        float u = (float)x / (float)(S - 1);
                        float v = (float)y / (float)(S - 1);
                        Color sc = src.GetPixelBilinear(u, v);
                        Color outc = sc * sc.a + bg * (1f - sc.a);
                        outc.a = 1f;
                        dst.SetPixel(x, y, outc);
                    }
                }
                dst.Apply();
                return dst;
            }
            catch (Exception e)
            {
                Logger.LogError("Icon bake failed: " + e.Message);
                return null;
            }
        }

        void SetupToolbarPosition(object toolbarButton)
        {
            try
            {
                GameObject myGo = ExtractGameObject(toolbarButton);
                if (myGo == null)
                {
                    Logger.LogError("KKBridge: no se pudo obtener GameObject del boton");
                    return;
                }
                Transform parent = myGo.transform.parent;
                if (parent == null) return;
                int findIdx = -1;
                for (int i = 0; i < parent.childCount; i++)
                {
                    string n = parent.GetChild(i).name;
                    Logger.LogInfo("KKB-TOOLBAR child " + i + ": " + n);
                    if (findIdx < 0 && n.IndexOf("Find") >= 0) findIdx = i;
                }
                if (findIdx >= 0)
                {
                    myGo.transform.SetSiblingIndex(findIdx + 1);
                    Logger.LogInfo("KKBridge: boton movido junto a Find");
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Toolbar order failed: " + e.Message);
            }
        }

        GameObject ExtractGameObject(object toolbarButton)
        {
            try
            {
                Type bt = toolbarButton.GetType();
                PropertyInfo[] props = bt.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (PropertyInfo p in props)
                {
                    try
                    {
                        if (p.GetIndexParameters().Length > 0) continue;
                        GameObject go = GameObjectFrom(p.GetValue(toolbarButton, null));
                        if (go != null) return go;
                    }
                    catch (Exception) { }
                }
                FieldInfo[] fields = bt.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (FieldInfo f in fields)
                {
                    try
                    {
                        GameObject go = GameObjectFrom(f.GetValue(toolbarButton));
                        if (go != null) return go;
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Extract GO failed: " + e.Message);
            }
            return null;
        }

        GameObject GameObjectFrom(object v)
        {
            if (v == null) return null;
            if (v is GameObject) return (GameObject)v;
            Component c = v as Component;
            if (c != null) return c.gameObject;
            return null;
        }

        bool CaptureCameraPng(string file)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                return false;
            }
            int w = Screen.width;
            int h = Screen.height;
            RenderTexture rt = new RenderTexture(w, h, 24);
            RenderTexture prevTarget = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;
            foreach (Camera c in Camera.allCameras)
            {
                Logger.LogInfo("CAM name=" + c.name + " enabled=" + c.enabled + " depth=" + c.depth + " rect=" + c.rect);
            }
            List<Canvas> hiddenCanvases = new List<Canvas>();
            try
            {
                UnityEngine.Object[] found = UnityEngine.Object.FindObjectsOfType(typeof(Canvas));
                foreach (UnityEngine.Object o in found)
                {
                    Canvas cv = o as Canvas;
                    if (cv != null && cv.enabled && cv.gameObject.activeInHierarchy)
                    {
                        cv.enabled = false;
                        hiddenCanvases.Add(cv);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError("Canvas hide failed: " + e.Message);
            }
            try
            {
                hideOwnUI = true;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                try
                {
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                    tex.Apply();
                    File.WriteAllBytes(file, tex.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.Destroy(tex);
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError("Camera capture failed: " + e.Message);
                return false;
            }
            finally
            {
                hideOwnUI = false;
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                foreach (Canvas cv in hiddenCanvases)
                {
                    try { if (cv != null) cv.enabled = true; }
                    catch (Exception) { }
                }
                UnityEngine.Object.Destroy(rt);
            }
        }

        void CapturarYEnviar()
        {
            BtnX.Value = (int)windowRect.x;
            BtnY.Value = (int)windowRect.y;
            try { Config.Save(); } catch (Exception) { }
            string baseDir = Path.Combine(Application.dataPath, "..");
            string bridge   = Path.Combine(baseDir, BridgeDir.Value);
            Directory.CreateDirectory(bridge);

            string file  = Path.Combine(bridge, latest);
            string stat  = Path.Combine(bridge, status);

            try
            {
                if (!CaptureCameraPng(file))
                {
                    Application.CaptureScreenshot(file);
                }
                long epoch = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                File.WriteAllText(stat,
                    "{\"ready\":true,\"file\":\"" + latest + "\",\"ts\":" + epoch.ToString() + "}");

                bool sent = false;
                Exception lastError = null;
                for (int i = 0; i < 3 && !sent; i++)
                {
                    try
                    {
                        using (var client = new TcpClient(TargetHost.Value, TargetPort.Value))
                        using (var stream = client.GetStream())
                        using (var writer = new StreamWriter(stream, Encoding.UTF8))
                        {
                            writer.Write("IMPORT " + file);
                            writer.Flush();
                        }
                        sent = true;
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        System.Threading.Thread.Sleep(500);
                    }
                }
                if (sent)
                {
                    Logger.LogInfo("Capture sent to Krita");
                }
                else
                {
                    Logger.LogError("Send failed (Krita abierto con el plugin activo?): " + lastError.Message);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Send failed: {ex.Message}");
            }
        }
    }
}
