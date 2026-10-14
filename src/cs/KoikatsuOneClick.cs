using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace KK_KritaBridge
{
    [BepInPlugin("johiny.kkritabridge", "KK Krita One Click", "1.0.0")]
    public class KKOneClick : BaseUnityPlugin
    {
        ConfigEntry<KeyCode> CaptureKey;
        ConfigEntry<string> BridgeDir;
        ConfigEntry<string> TargetHost;
        ConfigEntry<int> TargetPort;

        const string latest = "latest.png";
        const string status = "capture.status";

        void Awake()
        {
            CaptureKey  = Config.Bind("Hotkey", "CaptureKey", KeyCode.F12);
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

        void CapturarYEnviar()
        {
            string baseDir = Path.Combine(Application.dataPath, "..");
            string bridge   = Path.Combine(baseDir, BridgeDir.Value);
            Directory.CreateDirectory(bridge);

            string file  = Path.Combine(bridge, latest);
            string stat  = Path.Combine(bridge, status);

            try
            {
                Application.CaptureScreenshot(file);
                long epoch = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                File.WriteAllText(stat,
                    "{\"ready\":true,\"file\":\"" + latest + "\",\"ts\":" + epoch.ToString() + "}");

                using (var client = new TcpClient(TargetHost.Value, TargetPort.Value))
                using (var stream = client.GetStream())
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.Write($"IMPORT {file}");
                    writer.Flush();
                }
                Logger.LogInfo("Capture sent to Krita");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Send failed: {ex.Message}");
            }
        }
    }
}
