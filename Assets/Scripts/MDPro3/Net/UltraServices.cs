using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using MDPro3.Servant;
using MDPro3.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace MDPro3.Net
{
    public static class UltraServices
    {
        public const string Version = "0.1.5";
        public const string Repository = "https://github.com/MatteoGXC/ultra-duel";
        const string SessionPath = "Data/friends-session.json";
        public static string Token { get; private set; } = "";
        public static string PlayerName { get; private set; } = "";
        public static JObject State { get; private set; }
        public static string UpdateStatus { get; private set; } = "Check for updates";
        public static event Action Changed;
        static string endpoint, fingerprint;
        static bool initialized, updateBusy, friendsBusy;
        static float nextHeartbeat;

        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            try
            {
                var settings = JObject.Parse(File.ReadAllText("Data/ultra-services.json"));
                endpoint = ((string)settings["url"]).TrimEnd('/');
                fingerprint = ((string)settings["certificateSha256"]).ToLowerInvariant();
                if (!endpoint.StartsWith("https://") || fingerprint.Length != 64)
                    throw new Exception("Invalid friends service settings.");
                if (File.Exists(SessionPath))
                {
                    var session = JObject.Parse(File.ReadAllText(SessionPath));
                    Token = (string)session["token"] ?? "";
                    PlayerName = (string)session["name"] ?? "";
                }
            }
            catch (Exception e) { Debug.LogWarning("Ultra services: " + e.Message); }
        }

        sealed class PinnedCertificate : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData)
            {
                using var sha = SHA256.Create();
                string actual = BitConverter.ToString(sha.ComputeHash(certificateData)).Replace("-", "").ToLowerInvariant();
                return actual == fingerprint;
            }
        }

        public static async UniTask<JObject> Request(string path, string method = "GET", object body = null)
        {
            Initialize();
            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(fingerprint))
                throw new Exception("Friends service settings are missing. Re-run the Ultra Duel updater.");
            using var request = new UnityWebRequest(endpoint + path, method);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new PinnedCertificate();
            request.timeout = 15;
            if (body != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            if (!string.IsNullOrEmpty(Token)) request.SetRequestHeader("Authorization", "Bearer " + Token);
            try { await request.SendWebRequest(); }
            catch (Exception)
            {
                if (request.responseCode == 401) ClearSession();
                string error = null;
                try { error = (string)JObject.Parse(request.downloadHandler.text)["error"]; } catch { }
                throw new Exception(error ?? "Could not reach Ultra Duel. Check your connection and try again.");
            }
            return JObject.Parse(request.downloadHandler.text);
        }

        public static async UniTask SignIn(string name, string password, bool register)
        {
            var session = await Request(register ? "/register" : "/login", "POST", new { name, password });
            Token = (string)session["token"];
            PlayerName = (string)session["name"];
            Directory.CreateDirectory("Data");
            File.WriteAllText(SessionPath, JsonConvert.SerializeObject(new { token = Token, name = PlayerName }));
            Config.Set("DuelPlayerName0", PlayerName);
            Config.Save();
            await Refresh();
        }

        public static void ClearSession()
        {
            Token = PlayerName = "";
            State = null;
            if (File.Exists(SessionPath)) File.Delete(SessionPath);
            Changed?.Invoke();
        }

        public static async UniTask SignOut()
        {
            try { if (!string.IsNullOrEmpty(Token)) await Request("/logout", "POST", new { }); }
            finally { ClearSession(); }
        }

        public static string CurrentRoomId { get; private set; }
        public static void SetProfile(JObject profile)
        {
            if(State!=null)State["profile"]=profile;
            Changed?.Invoke();
        }
        public static async UniTask Refresh()
        {
            if (friendsBusy || string.IsNullOrEmpty(Token)) return;
            friendsBusy = true;
            try { State = await Request("/state"); Changed?.Invoke(); await UltraProgress.SyncPending(); }
            finally { friendsBusy = false; }
        }

        public static async UniTaskVoid Heartbeat()
        {
            Initialize();
            if (Time.realtimeSinceStartup < nextHeartbeat) return;
            nextHeartbeat = Time.realtimeSinceStartup + 15;
            try { if(!string.IsNullOrEmpty(Token)) await Request("/presence","POST",new{activity=UltraAccount.Activity,mate=UltraAccount.ProfileMate}); await Refresh(); } catch { /* Presence retries without interrupting a duel. */ }
        }

        public static async UniTask JoinRoom(JObject room)
        {
            CurrentRoomId=(string)room["id"];
            DuelFormat.Set((string)room["format"] == "Genesys", (int)room["cap"]);
            var deck = new MDPro3.Duel.YGOSharp.Deck(Program.PATH_DECK + Config.GetConfigDeckName() + Program.EXPANSION_YDK);
            if (!DuelFormat.Validate(deck, "Your Deck")) return;
            Program.instance.online.KF_OnlineGame(PlayerName, (string)room["address"],
                ((int)room["port"]).ToString(), (string)room["secret"]);
            RoomServant.FromUltraHost = true;
        }

        static async UniTask<JObject> GitHub(string path)
        {
            using var request = UnityWebRequest.Get("https://api.github.com/repos/MatteoGXC/ultra-duel/" + path);
            request.timeout = 10;
            request.SetRequestHeader("User-Agent", "Ultra-Duel/" + Version);
            request.SetRequestHeader("Accept", "application/vnd.github+json");
            await request.SendWebRequest();
            return JObject.Parse(request.downloadHandler.text);
        }

        public static async UniTaskVoid CheckUpdates(bool manual = false)
        {
            if (updateBusy) return;
            updateBusy = true;
            try
            {
                var release = await GitHub("releases/latest");
                string tag = (string)release["tag_name"];
                bool newer = System.Version.TryParse(tag?.TrimStart('v'), out var available)
                    && available > new System.Version(Version);
                string message, identity;
                if (newer)
                {
                    UpdateStatus = "Update " + tag + " available";
                    identity = tag;
                    message = "Ultra Duel " + tag + " is available. Close the game and run Update-Ultra-Duel.cmd in your installation folder to download only changed files.";
                }
                else
                {
                    var main = await GitHub("commits/main");
                    string current = "";
                    if (File.Exists("build-info.json")) current = (string)JObject.Parse(File.ReadAllText("build-info.json"))["commit"];
                    identity = (string)main["sha"];
                    bool sourceChanged = !string.IsNullOrEmpty(current) && current != identity;
                    UpdateStatus = sourceChanged ? "New development update" : "Ultra Duel is up to date";
                    message = sourceChanged
                        ? "New changes were pushed to GitHub. A new Windows release has not been published yet; your installed release is current."
                        : "You have the latest published Ultra Duel release (v" + Version + ").";
                }
                Changed?.Invoke();
                if (manual) UIManager.ShowPopupConfirm(new List<string> { "Ultra Duel updates", message });
                else if ((newer || UpdateStatus == "New development update") && Config.Get("UltraUpdateSeen", "") != identity)
                {
                    MessageManager.Cast(message);
                    Config.Set("UltraUpdateSeen", identity); Config.Save();
                }
            }
            catch
            {
                UpdateStatus = "Update check unavailable";
                Changed?.Invoke();
                if (manual) UIManager.ShowPopupConfirm(new List<string> { "Ultra Duel updates", "Could not check GitHub. Try again when you are online." });
            }
            finally { updateBusy = false; }
        }
    }
}
