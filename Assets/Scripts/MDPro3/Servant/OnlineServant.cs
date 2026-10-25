using MDPro3.Duel.YGOSharp;
using MDPro3.Net;
using MDPro3.UI;
using MDPro3.UI.ServantUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YgomGame.TextIDs;
using static MDPro3.Duel.YGOSharp.PacksManager;

namespace MDPro3.Servant
{
    public class OnlineServant : Servant
    {
        [HideInInspector] public SelectionToggle_Address lastSelectedAddressItem;
        [HideInInspector] public SelectionToggle_Watch lastSelectedWatchItem;

        public const int DEFAULT_TIME = 180;
        public const int DEFAULT_PORT = 7911;
        public const int DEFAULT_LP = 8000;
        public const int DEFAULT_HAND = 5;
        public const int DEFAULT_DRAW = 1;

        #region Servant

        public override int Depth => 1;
        protected override bool ShowLine => true;

        public override void Initialize()
        {
            returnServant = Program.instance.menu;
            base.Initialize();
            TryTokenIn();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            if (Program.exitOnReturn)
            {
                Program.GameQuit();
                return;
            }

            base.ApplyShowArrangement(preDepth);
            if (servantUI != null)
                RefreshDeckSelector();

            StartCoroutine(RefreshMyCardAssets());
        }

        protected override void FirstLoadEvent()
        {
            base.FirstLoadEvent();
            RefreshDeckSelector();
            MyCard.ConnectToAthleticWatchListWebSocket();
        }

        protected override void ApplyHideArrangement(int preDepth)
        {
            base.ApplyHideArrangement(preDepth);
            Config.Save();
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;

            if (servantUI != null)
                GetUI<OnlineServantUI>().SelectLastSelectable(lastSelectable);
        }

        public override void PerFrameFunction()
        {
            if (!NeedResponseInput())
                return;

            if (UserInput.MouseRightDown || UserInput.WasCancelPressed)
                OnReturn();
            if (UserInput.WasLeftShoulderPressed)
                GetUI<OnlineServantUI>().PageLeft();
            if (UserInput.WasRightShoulderPressed)
                GetUI<OnlineServantUI>().PageRight();
        }

        #endregion

        #region Legacy

        public void KF_OnlineGame(string name, string ip, string port, string password)
        {
            if (string.IsNullOrEmpty(name))
            {
                MessageManager.Cast(InterString.Get("用户名不能为空。"));
                return;
            }

            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port))
            {
                MessageManager.Cast(InterString.Get("主机地址和端口不能为空。"));
                return;
            }

            RoomServant.FromUltraHost = false; RoomServant.FromUltraMatch = false;
            RoomServant.FromSolo = false;
            RoomServant.FromLocalHost = false;
            RoomServant.FromHandTest = false;
            TcpHelper.LinkStart(ip, name, port, password, false, null);
        }

        #endregion

        #region Local Host

        public void CreateServer(List<string> serverArgs)
        {
            // Hand-test uses its own permissive setup and is not a tournament duel.
            if (!DuelFormat.RequireNativeRules()) return;
            if (!DuelFormat.Genesys && DuelFormat.TcgBanlist == null)
            {
                MessageManager.Toast("TCG Forbidden & Limited List unavailable. Refresh rules data first.");
                return;
            }
            int port = 7911;
            while (!TcpHelper.IsPortAvailable(port))
            {
                port++;
                if (port == 65536)
                    port = 1;
            }

            string args = string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}",
                port.ToString(),
                DuelFormat.Genesys ? -1 : BanlistManager.GetIndexByName(DuelFormat.TcgBanlist.Name),
                "1", // TCG card pool for both formats.
                GetModeCodeByName(serverArgs[3]),
                DuelFormat.MasterRule,
                "F", // Tournament format checks cannot be bypassed by the practice toggle.
                serverArgs[5],
                serverArgs[7],
                serverArgs[8],
                serverArgs[9],
                serverArgs[6],
                "0");

            RoomServant.FromUltraHost = false; RoomServant.FromUltraMatch = false;
            RoomServant.FromSolo = false;
            RoomServant.FromLocalHost = true;
            RoomServant.FromHandTest = false;

            YgoServer.StartServer(args);
            TcpHelper.LinkStart("127.0.0.1", Config.Get("DuelPlayerName0", Config.EMPTY_STRING), port.ToString(), string.Empty, true, null);
        }

        private string GetPoolCodeByName(string pool)
        {
            for (int i = 1481; i < 1487; i++)
            {
                if (StringHelper.GetUnsafe(i) == pool)
                    return (i - 1481).ToString();
            }
            return "5";
        }

        private string GetModeCodeByName(string mode)
        {
            for (int i = 1244; i < 1247; i++)
            {
                if (StringHelper.GetUnsafe(i) == mode)
                    return (i - 1244).ToString();
            }
            return "0";
        }

        #endregion

        #region MyCard

        public void LoginMyCard(string name, string passwd)
        {
            StartCoroutine(LoginMyCardAsync(name, passwd));
        }

        private IEnumerator LoginMyCardAsync(string account, string passwd)
        {
            GetUI<OnlineServantUI>().PageMyCard.PageLogin.SetActive(false);

            var task = MyCard.Login(account, passwd);
            while(!task.IsCompleted)
                yield return null;
            if (task.Result.user.id == 0)
            {
                MessageManager.Cast(InterString.Get("登录失败：") + task.Result.user.username);
                GetUI<OnlineServantUI>().PageMyCard.PageLogin.SetActive(true);
                if (UserInput.NeedDefaultSelect())
                    Select();
                yield break;
            }
            Config.Set("MyCardToken", task.Result.token);
            Config.Save();
            LoginSuccessEvent();
        }

        private void TryTokenIn()
        {
            StartCoroutine(TryTokenInAsync());
        }

        private IEnumerator TryTokenInAsync()
        {
            var token = Config.Get("MyCardToken", Config.STRING_NO);
            if(token == Config.STRING_NO)
                yield break;
            var task = MyCard.TokenIn(token);
            while (!task.IsCompleted)
                yield return null;

            if (task.Result.user.id == 0)
            {
                MessageManager.Cast(InterString.Get("MyCard登录失败。"));
                yield break;
            }
            LoginSuccessEvent();
        }

        private void LoginSuccessEvent()
        {
            if (MyCard.account == null)
                return;
            StartCoroutine(SyncDecks());
            if (servantUI == null)
                return;
            StartCoroutine(RefreshMyCardAssets());
        }

        private IEnumerator RefreshMyCardAssets()
        {
            var onlineUI = GetUI<OnlineServantUI>();
            if (onlineUI == null || onlineUI.PageMyCard == null || onlineUI.PageMyCard.UserProfile == null)
                yield break;

            var userProfile = onlineUI.PageMyCard.UserProfile;

            var task = MyCard.GetExp();
            while (!task.IsCompleted)
                yield return null;

            MyCardUserExp expData = null;
            if (!task.IsCanceled && task.Exception == null)
                expData = task.Result;
            userProfile.SetProfile(expData);

            while (!Appearance.loaded)
                yield return null;
            if (userProfile.Avatar != null)
                userProfile.Avatar.material = Appearance.duelFrameMat0;

            if (MyCard.avatar == null)
            {
                var avatarAddress = MyCard.account?.user?.avatar;
                if (!string.IsNullOrEmpty(avatarAddress))
                {
                    var avatarTask = Tools.DownloadImageAsync(avatarAddress);
                    while (!avatarTask.IsCompleted)
                        yield return null;
                    MyCard.avatar = avatarTask.Result;
                }
            }

            if (userProfile.Avatar != null && MyCard.avatar != null)
                userProfile.Avatar.texture = MyCard.avatar;

            onlineUI.PageMyCard.ActivePageFunction();
        }

        private void RefreshDeckSelector()
        {
            GetUI<OnlineServantUI>().PageMyCard.ButtonDeckSelector.SetConfigDeck(InterString.Get("未选中有效卡组"));
        }

        private float timeError = 3f;

        private string GetDeckNameWithType(string path)
        {
            var type = Path.GetFileName(Path.GetDirectoryName(path));
            if(type == "Deck" && type == Path.GetDirectoryName(path))
                type = string.Empty;
            return (type == string.Empty ? string.Empty : $"{type}/") + Path.GetFileNameWithoutExtension(path);
        }

        private string GetDeckTypeFromPath(string path)
        {
            var type = Path.GetFileName(Path.GetDirectoryName(path));
            if (type == "Deck" && type == Path.GetDirectoryName(path))
                type = string.Empty;
            return type;
        }

        private string GetDeckTypeFromName(string deckName)
        {
            if (!deckName.Contains("/"))
                return string.Empty;
            return deckName.Split('/')[0];
        }

        private IEnumerator SyncDecks()
        {
            if (OnlineDeck.decks == null)
            {
                MessageManager.Cast(InterString.Get("同步卡组失败。"));
                yield break;
            }

            var deckFiles = Directory.GetFiles(Program.PATH_DECK, "*.ydk", SearchOption.AllDirectories);
            var decks = new List<Deck>();
            foreach (var deckPath in deckFiles)
                decks.Add(new Deck(deckPath));

            var decksNeedUpload = new Dictionary<string, Deck>();//没在服务器找到对应的deckId的本地卡组
            var decksNeedUpdateToServer = new Dictionary<string, Deck>();//找到deckId但本地时间大于服务器时间timeError秒以上的卡组
            var decksNeedUpdateFromServer = new Dictionary<string, Deck>();//找到deckId但本地时间小于服务器时间timeError秒以上的卡组
            var localFoundIds = new List<string>();

            for (int i = 0; i < decks.Count; i++)
            {
                var deckName = GetDeckNameWithType(deckFiles[i]);
                var type = GetDeckTypeFromPath(deckFiles[i]);
                decks[i].type = type;

                bool deckIdFound = false;
                foreach (var od in OnlineDeck.decks)
                {
                    if (od.deckId == decks[i].deckId)
                    {
                        deckIdFound = true;
                        localFoundIds.Add(od.deckId);
                        if (od.isDelete)
                        {
                            File.Delete(deckFiles[i]);
                        }
                        else
                        {
                            var fileInfo = new FileInfo(deckFiles[i]);
                            var serverTime = od.GetUpdateUtcTime();
                            var diff = serverTime - fileInfo.LastWriteTimeUtc;

                            //Debug.Log($"{od.deckName}: serverTimeUtc: {od.GetUpdateUtcTime()}, localTime: {fileInfo.LastWriteTimeUtc} diff: {diff.TotalSeconds}");

                            if (diff.TotalSeconds > timeError || diff.TotalSeconds < -timeError)
                            {
                                if (fileInfo.LastWriteTimeUtc > serverTime)
                                    decksNeedUpdateToServer.Add(deckName, decks[i]);
                                else
                                    decksNeedUpdateFromServer.Add(deckName, decks[i]);
                            }
                            if((Path.GetFileName(deckName) != od.deckName || GetDeckTypeFromName(deckName) != od.GetType())
                                && !decksNeedUpdateFromServer.Keys.Contains(deckName)
                                && !decksNeedUpdateToServer.Keys.Contains(deckName))
                            {
                                //Debug.Log($"[{Path.GetFileName(deckName)}] [{od.deckName}] [{GetDeckTypeFromName(deckName)}] [{od.GetType()}]");
                                decksNeedUpdateFromServer.Add(deckName, decks[i]);
                            }
                        }
                        break;
                    }
                }

                if (!deckIdFound)
                    decksNeedUpload.Add(deckName, decks[i]);
            }

            //上传已经有Id的本地较新卡组
            foreach (var deck in decksNeedUpdateToServer)
            {
                Debug.LogFormat("卡组[{0}]需要更新上传。", deck.Key);
                var time = DateTime.UtcNow;
                var task = OnlineDeck.SyncDeck(deck.Value.deckId, Path.GetFileName(deck.Key), deck.Value, time, false);
                while (!task.IsCompleted)
                    yield return null;
            }
            //更新已经有Id的本地较旧卡组
            foreach (var deck in decksNeedUpdateFromServer)
            {
                Debug.LogFormat("卡组[{0}]需要更新。", deck.Key);

                var od = OnlineDeck.GetByID(deck.Value.deckId);
                var oldPath = Program.PATH_DECK + deck.Key + Program.EXPANSION_YDK;
                if (Path.GetFileName(deck.Key) != od.deckName || deck.Value.type != od.GetType())
                    File.Delete(oldPath);
                var newPath = Program.PATH_DECK + (od.GetType() == string.Empty ? string.Empty : $"{od.GetType()}/") + od.deckName + Program.EXPANSION_YDK;
                if(!Directory.Exists(Path.GetDirectoryName(newPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                File.WriteAllText(newPath, od.deckYdk);
                File.SetLastWriteTimeUtc(newPath, od.GetUpdateUtcTime());
            }

            //下载本地ID不存在的服务器卡组
            //OnlineDeck.UploadDecks() 会刷新OnlineDeck.decks，因此本功能需要在[上传没有Id的本地卡组]之前执行
            var odtd = OnlineDeck.decks
                .Where(od => !od.isDelete && !localFoundIds.Contains(od.deckId));
            foreach (var deck in odtd)
            {
                var path = Program.PATH_DECK + (deck.GetType() == string.Empty ? string.Empty : $"{deck.GetType()}/") + deck.deckName + Program.EXPANSION_YDK;
                if (File.Exists(path))
                {
                    Debug.Log($"删除服务器同名卡组 [{deck.GetType()}/{deck.deckName}]  [{deck.deckId}]。");
                    _ = OnlineDeck.DeleteDecks(new List<string> { deck.deckId });
                    continue;
                }
                Debug.Log($"卡组[{deck.GetType()} / {deck.deckName}] [{deck.deckId}]需要下载。");

                var d = new Deck(deck.deckYdk, deck.deckId, MyCard.account.user.username)
                {
                    type = deck.GetType()
                };
                d.Save(Path.GetFileName(deck.deckName), deck.GetUpdateUtcTime(), false);
            }

            //上传没有Id的本地卡组
            if (decksNeedUpload.Count > 0)
            {
                var decksToUp = new List<Deck>();
                var deckNames = new List<string>();
                foreach (var deck in decksNeedUpload)
                {
                    var info = string.Format("卡组[{0}]需要上传：{1}。", deck.Key, deck.Value.deckId);
                    Debug.Log(info);

                    deckNames.Add(Path.GetFileName(deck.Key));
                    decksToUp.Add(deck.Value);
                }
                var task2 = OnlineDeck.UploadDecks(decksToUp, deckNames);
                while (!task2.IsCompleted)
                    yield return null;
            }


#if UNITY_EDITOR
            MessageManager.Cast("Deck Sync Finished.");
#endif
        }

        #region MyCard Match

        public static IEnumerator entertainMatch;
        public static IEnumerator athleticMatch;

        public void EntertainMatch()
        {
            if(entertainMatch == null)
            {
                StartCoroutine(entertainMatch = EntertainMatchAsync());
                if(athleticMatch != null)
                {
                    StopCoroutine(athleticMatch);
                    athleticMatch = null;
                }
                GetUI<OnlineServantUI>().PageMyCard.ButtonAMatch.SetButtonText(InterString.Get("竞技匹配"));
            }
            else
            {
                GetUI<OnlineServantUI>().PageMyCard.ButtonEMatch.SetButtonText(InterString.Get("娱乐匹配"));
                StopCoroutine(entertainMatch);
                entertainMatch = null;
            }
        }

        public void AthleticMatch()
        {
            if (athleticMatch == null)
            {
                StartCoroutine(athleticMatch = AthleticMatchAsync());
                if (entertainMatch != null)
                {
                    StopCoroutine(entertainMatch);
                    entertainMatch = null;
                }
                GetUI<OnlineServantUI>().PageMyCard.ButtonEMatch.SetButtonText(InterString.Get("娱乐匹配"));
            }
            else
            {
                GetUI<OnlineServantUI>().PageMyCard.ButtonAMatch.SetButtonText(InterString.Get("竞技匹配"));
                StopCoroutine(athleticMatch);
                athleticMatch = null;
            }
        }

        private IEnumerator EntertainMatchAsync()
        {
            var task = MyCard.GetMatchInfo("entertain");
            var startTime = DateTime.Now;

            while(!task.IsCompleted)
            {
                var elapsedTimeInSeconds = (DateTime.Now - startTime).TotalSeconds;
                int minutes = (int)Math.Floor(elapsedTimeInSeconds / 60);
                int seconds = (int)(elapsedTimeInSeconds % 60);
                GetUI<OnlineServantUI>().PageMyCard.ButtonEMatch.SetButtonText($"{minutes:D2}:{seconds:D2}");
                yield return new WaitForSeconds(0.5f);
            }

            if(task.Result != null)
            {
                GetUI<OnlineServantUI>().PageMyCard.ButtonEMatch.SetButtonText(InterString.Get("娱乐匹配"));
                TcpHelper.LinkStart(task.Result.address, MyCard.account.user.username, task.Result.port.ToString(), task.Result.password, false, null);
            }
            else
            {
                GetUI<OnlineServantUI>().PageMyCard.ButtonEMatch.SetButtonText(InterString.Get("匹配失败"));
            }
            entertainMatch = null;
        }

        private IEnumerator AthleticMatchAsync()
        {
            var task = MyCard.GetMatchInfo("athletic");
            var startTime = DateTime.Now;

            while (!task.IsCompleted)
            {
                var elapsedTimeInSeconds = (DateTime.Now - startTime).TotalSeconds;
                int minutes = (int)Math.Floor(elapsedTimeInSeconds / 60);
                int seconds = (int)(elapsedTimeInSeconds % 60);
                GetUI<OnlineServantUI>().PageMyCard.ButtonAMatch.SetButtonText($"{minutes:D2}:{seconds:D2}");
                yield return new WaitForSeconds(0.5f);
            }

            if (task.Result != null)
            {
                GetUI<OnlineServantUI>().PageMyCard.ButtonAMatch.SetButtonText(InterString.Get("竞技匹配"));
                TcpHelper.LinkStart(task.Result.address, MyCard.account.user.username, task.Result.port.ToString(), task.Result.password, false, null);
            }
            else
            {
                GetUI<OnlineServantUI>().PageMyCard.ButtonAMatch.SetButtonText(InterString.Get("匹配失败"));
            }
            athleticMatch = null;
        }

        #endregion

        #region Watch List

        public void SetWatchRooms(List<MyCardRoom> rooms)
        {
            GetUI<OnlineServantUI>().PageMyCard.WatchList.SetRooms(rooms);
        }

        public void CreateWatchRoom(MyCardRoom room)
        {
            GetUI<OnlineServantUI>().PageMyCard.WatchList.CreateRoom(room);
        }

        public void UpdateWatchRoom(MyCardRoom room)
        {
            GetUI<OnlineServantUI>().PageMyCard.WatchList.UpdateRoom(room);
        }

        public void DeleteWatchRoom(string roomId)
        {
            GetUI<OnlineServantUI>().PageMyCard.WatchList.DeleteRoom(roomId);
        }

        public void ClearWatchList()
        {
            GetUI<OnlineServantUI>().PageMyCard.WatchList.Clear();
        }

        #endregion

        #endregion
    }
}
