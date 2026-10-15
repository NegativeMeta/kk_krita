using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace KK_KritaBridge
{
    [BepInPlugin("johiny.kkritabridge", "KK Krita One Click", "1.4.0")]
    public class KKOneClick : BaseUnityPlugin
    {
        ConfigEntry<KeyCode> CaptureKey;
        ConfigEntry<string> BridgeDir;
        ConfigEntry<string> TargetHost;
        ConfigEntry<int> TargetPort;
        ConfigEntry<bool> ShowButton;
        ConfigEntry<int> BtnX;
        ConfigEntry<int> BtnY;
        bool hideOwnUI;
        const int windowId = 987121;
        Texture2D kritaIcon;
        Rect windowRect;
        GUIStyle emptyWinStyle;

        const string latest = "latest.png";
        const string status = "capture.status";

        void Awake()
        {
            CaptureKey  = Config.Bind("Hotkey", "CaptureKey", KeyCode.F12);
            ShowButton  = Config.Bind("UI", "ShowButton", true, "Muestra el boton flotante para enviar a Krita");
            BtnX  = Config.Bind("UI", "ButtonX", 8, "Posicion X del boton");
            BtnY  = Config.Bind("UI", "ButtonY", 320, "Posicion Y del boton");
            windowRect = new Rect(BtnX.Value, BtnY.Value, 52, 52);
            LoadIcon();
            BridgeDir   = Config.Bind("Bridge", "BridgeDir", "UserData/krita_bridge");
            TargetHost  = Config.Bind("Bridge", "Host", "127.0.0.1");
            TargetPort  = Config.Bind("Bridge", "Port", 9184);
            Logger.LogInfo("KK Krita One Click ready");
        }

        void Update()
        {
            if (!Input.GetKeyDown(CaptureKey.Value)) return;
            CapturarYEnviar();
        }

        void OnGUI()
        {
            if (!ShowButton.Value || hideOwnUI) return;
            if (emptyWinStyle == null) emptyWinStyle = new GUIStyle();
            windowRect = GUI.Window(windowId, windowRect, IconWindow, GUIContent.none, emptyWinStyle);
        }

        void IconWindow(int id)
        {
            if (kritaIcon != null)
            {
                if (GUI.Button(new Rect(2, 2, 48, 48), kritaIcon)) CapturarYEnviar();
            }
            else if (GUI.Button(new Rect(2, 2, 48, 48), "K"))
            {
                CapturarYEnviar();
            }
            GUI.DragWindow();
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
