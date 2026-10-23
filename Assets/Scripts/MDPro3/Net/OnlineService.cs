using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using Newtonsoft.Json;
using Percy;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace MDPro3.Net
{
    public static class OnlineService
    {

        public static void Initialize()
        {
            _ = InitializeGenesysLflist();
            _ = InitializeMyCardAppsAsync();
        }

        #region Genesys lflist

        private const string URL_GENESYS_LFLIST = "https://cdntx.moecube.com/ygopro-genesys/lflist.conf";

        private static async UniTask InitializeGenesysLflist()
        {
            // Preserve the verified official offline snapshot. The upstream feed may
            // contain a different region or an older list; refresh via the source helper.
            if (File.Exists(Program.PATH_DATA + "format-data-sources.json")
                && File.Exists(Program.PATH_DATA + BanlistManager.FILE_NAME_GENESYS)) return;
            var eTag = await GetETagAsync(URL_GENESYS_LFLIST);
            if (string.IsNullOrEmpty(eTag))
                return;

            var configTag = Config.Get(GetLocalETagKey(URL_GENESYS_LFLIST), Config.EMPTY_STRING);
            if(!string.Equals(eTag, configTag, StringComparison.Ordinal))
            {
                Program.Debug("Update Genesys Lflist.");
                await DownloadGenesysLflist(eTag);
                BanlistManager.InitializeForGenesys();
            }
            else
                Program.Debug("Genesys Lflist do not need update.");
        }

        private static async UniTask DownloadGenesysLflist(string ETag)
        {
            using var request = UnityWebRequest.Get(URL_GENESYS_LFLIST);
            request.timeout = 15;

            try
            {
                await request.SendWebRequest();
            }
            catch
            {
                MessageManager.Cast(InterString.Get("下载Genesys禁卡表失败。"));
                return;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllText(Program.PATH_DATA + BanlistManager.FILE_NAME_GENESYS, request.downloadHandler.text);
                Config.Set(GetLocalETagKey(URL_GENESYS_LFLIST), ETag);
                Config.Save();
            }
            else
                MessageManager.Cast(InterString.Get("下载Genesys禁卡表失败。"));
        }

        #endregion

        #region MyCard Apps

        private const string URL_MYCARD_APPS = "https://cdntx.moecube.com/apps.json";
        private const string PATH_MYCARD_APPS = "Data/mycard_apps.json";
        public static MyCardNews myCardNews;

        private static async UniTask InitializeMyCardAppsAsync()
        {
            var eTag = await GetETagAsync(URL_MYCARD_APPS);
            if (!string.IsNullOrEmpty(eTag))
            {
                var configTag = Config.Get(GetLocalETagKey(URL_MYCARD_APPS), Config.EMPTY_STRING);
                if (!string.Equals(eTag, configTag, StringComparison.Ordinal))
                {
                    Program.Debug("Update MyCard Apps.");
                    await DownloadMyCardApps(eTag);
                }
                else
                    Program.Debug("MyCard Apps do not need update.");
            }

            ParseMyCardNews();
        }

        private static async UniTask DownloadMyCardApps(string ETag)
        {
            using var request = UnityWebRequest.Get(URL_MYCARD_APPS);
            request.timeout = 15;

            try
            {
                await request.SendWebRequest();
            }
            catch
            {
                Program.Debug("下载MyCard apps.json失败。");
                return;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                File.WriteAllText(PATH_MYCARD_APPS, request.downloadHandler.text);
                Config.Set(GetLocalETagKey(URL_MYCARD_APPS), ETag);
                Config.Save();
            }
            else
                Program.Debug("下载MyCard apps.json失败。");
        }

        private static void ParseMyCardNews()
        {
            if (!File.Exists(PATH_MYCARD_APPS))
                return;

            var json = File.ReadAllText(PATH_MYCARD_APPS);
            json = json.Replace("\"news\":[]", "\"news\":{}");
            var apps = JsonConvert.DeserializeObject<MyCardApp[]>(json);
            foreach (var app in apps)
                if (app.id == "ygopro")
                {
                    myCardNews = app.news;
                    return;
                }
        }

        #endregion


        #region Online Tools

        private static string GetLocalETagKey(string url) => $"ETag_{url.GetHashCode()}";

        public static async UniTask<string> GetETagAsync(string url)
        {
            using var headRequest = UnityWebRequest.Head(url);
            headRequest.timeout = 8;
            try
            {
                await headRequest.SendWebRequest();
            }
            catch
            {
                return null;
            }

            if(headRequest.result != UnityWebRequest.Result.Success)
            {
                Program.Debug($"HEAD({url})请求失败：{headRequest.error}");
                return null;
            }

            var onlineETag = headRequest.GetResponseHeader("ETag")?.Trim();
            if (string.IsNullOrEmpty(onlineETag))
            {
                Program.Debug($"未找到ETag({url})，服务器可能未启用缓存");
                return null;
            }
            return onlineETag;
        }

        #endregion

    }
}
