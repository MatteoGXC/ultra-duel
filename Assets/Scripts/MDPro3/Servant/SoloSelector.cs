using MDPro3.Net;
using MDPro3.UI;
using MDPro3.Duel.YGOSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using MDPro3.Utility;
using MDPro3.UI.PropertyOverride;
using MDPro3.UI.ServantUI;

namespace MDPro3.Servant
{
    public class SoloSelector : Servant
    {

        public class BotInfo
        {
            public string name;
            public string command;
            public string desc;
            public string[] flags;
            public int main0;
        }
        public static readonly List<BotInfo> bots = new();

        private const string WINDBOT_DIALOG_PATH = "Data/Windbot/Dialogs/";
        private const string DECK_PREFIX = "Data/WindBot/Decks/Ai_";
        private const int DEFAULT_CARD = 5990062;
        public static string port = "7911";

        public enum Condition
        {
            ForSolo,
            ForRoom
        }
        public static Condition condition = Condition.ForSolo;
        public void SwitchCondition(Condition condition)
        {
            SoloSelector.condition = condition;
            switch (condition)
            {
                case Condition.ForSolo:
                    returnServant = Program.instance.menu;
                    break;
                case Condition.ForRoom:
                    returnServant = Program.instance.room;
                    break;
            }

        }

        [HideInInspector] public SelectionToggle_Solo lastSoloItem;

        private string lastSoloCommand = string.Empty;
        private int lastSoloPort = 7911;
        private int lastSoloLp = 8000;
        private int lastSoloHand = 5;
        private int lastSoloDraw = 1;
        private bool lastSoloLockHand;
        private bool lastSoloNoCheck;
        private bool lastSoloNoShuffle;
        private bool hasLastSoloLaunchConfig;
        private bool retryLastSoloDuelWhenShown;

        #region Servant

        public override int Depth => 3;
        protected override bool ShowLine => false;

        public override void Initialize()
        {
            returnServant = Program.instance.menu;
            base.Initialize();
            LoadBots();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);
            lastSoloItem = null;
            LoadBots();
            if (retryLastSoloDuelWhenShown)
                StartCoroutine(RetryLastSoloDuelWhenReady());
        }

        protected override void FirstLoadEvent()
        {
            base.FirstLoadEvent();
            Print();
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;

            if (servantUI == null)
                return;

            if (lastSoloItem != null)
                lastSoloItem.GetSelectable().Select();
            else
                servantUI.SelectDefaultSelectable();
        }

        #endregion

        #region Solo

        public void LoadBots()
        {
            ReadBots(Program.PATH_LOCALES + Language.GetConfig() + "/bot.conf");
            Print();
        }

        private void ReadBots(string confPath)
        {
            bots.Clear();
            using StreamReader reader = new(new FileStream(confPath, FileMode.Open, FileAccess.Read));
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine().Trim();
                if (line.Length > 0 && line[0] == '!')
                {
                    BotInfo newBot = new()
                    {
                        name = line.TrimStart('!'),
                        command = reader.ReadLine().Trim(),
                        desc = reader.ReadLine().Trim()
                    };
                    line = reader.ReadLine().Trim();
                    newBot.flags = line.Split(' ');

                    if (condition == Condition.ForSolo || DuelFormat.LocalSession)
                    {
                        if (System.Array.IndexOf(newBot.flags, "SELECT_DECKFILE") < 0)
                        {
                            if (!DuelFormat.PrepareSoloBot(newBot.command, out var prepared)) continue;
                            newBot.command = prepared;
                            newBot.desc = DuelFormat.DisplayName + " build\n" + newBot.desc;
                        }
                    }

                    newBot.main0 = DEFAULT_CARD;
                    try
                    {
                        string deckName = string.Empty;
                        deckName = newBot.command.Split(new string[] { "Deck=", " Dialog=" }
                        , StringSplitOptions.RemoveEmptyEntries)[1].Replace("'", string.Empty).Replace(" ", string.Empty);
                        if (File.Exists(DECK_PREFIX + deckName + Program.EXPANSION_YDK))
                        {
                            var aiDeck = new Deck(DECK_PREFIX + deckName + Program.EXPANSION_YDK);
                            if (aiDeck.Main.Count > 0)
                                newBot.main0 = aiDeck.Main[0];
                        }
                    }
                    catch { }

                    bots.Add(newBot);
                }
            }
            // Random picks must have an eligible opponent in the current library.
            if ((condition == Condition.ForSolo || DuelFormat.LocalSession) && File.Exists("Data/solo-executors.conf"))
                foreach (string entry in File.ReadAllLines("Data/solo-executors.conf"))
                {
                    var parts = entry.Split('|');
                    if (parts.Length != 2 || bots.Exists(bot => DuelFormat.SoloBotExecutor(bot.command) == parts[0])) continue;
                    string command = "Deck=\"" + parts[0] + "\" Name=\"" + parts[0] + "\" Dialog=Universal.EN";
                    if (!DuelFormat.PrepareSoloBot(command, out var prepared)) continue;
                    string path = Path.Combine("Data", "Windbot", "Decks", DuelFormat.Genesys ? "Genesys" : "TCG", parts[1] + ".ydk");
                    var deck = new Deck(path);
                    bots.Add(new BotInfo {
                        name = Regex.Replace(parts[0], "(?<=[a-z])(?=[A-Z])", " "), command = prepared,
                        desc = DuelFormat.DisplayName + " build\nPractice against " + parts[0] + ".",
                        flags = new[] { "AI_LV3", "SUPPORT_MASTER_RULE_2020" },
                        main0 = deck.Main.Count > 0 ? deck.Main[0] : DEFAULT_CARD
                    });
                }
            bots.RemoveAll(bot => bot.command.StartsWith("Random=") &&
                !bots.Exists(candidate => System.Array.IndexOf(candidate.flags, bot.command.Substring(7)) >= 0));
        }

        private void Print()
        {
            if (servantUI == null)
                return;

            GetUI<SoloSelectorUI>().Print();
        }

        private string GetWindBotCommand(int aiCode, bool diyDeck)
        {
            BotInfo bot = bots[aiCode];
            string aiCommand = bot.command;
            if (diyDeck)
            {
                string selectedDeck = GetUI<SoloSelectorUI>().GetAIDeck();
                if (!File.Exists(Program.PATH_DECK + selectedDeck + Program.EXPANSION_YDK))
                {
                    MessageManager.Cast(InterString.Get("请先为AI选择有效的卡组。"));
                    return string.Empty;
                }
                aiCommand += " DeckFile=\"" + selectedDeck + "\"";
            }
            Match match = Regex.Match(aiCommand, "Random=(\\w+)");
            if (match.Success)
            {
                string randomFlag = match.Groups[1].Value;
                string command = GetRandomBot(randomFlag);
                if (command != string.Empty)
                    aiCommand = command;
            }
            return aiCommand;
        }

        public void StartAIForSolo(int aiCode, bool diyDeck)
        {
            string aiCommand = GetWindBotCommand(aiCode, diyDeck);
            if (!string.IsNullOrEmpty(aiCommand))
            {
                var ui = GetUI<SoloSelectorUI>();
                var lockHand = ui.IsLockHand();
                var noCheck = ui.IsNoCheck();
                var noShuffle = ui.IsNoShuffle();
                var duelPort = ui.GetPort();
                var duelLp = ui.GetLP();
                var duelHand = ui.GetHand();
                var duelDraw = ui.GetDraw();

                RememberLastSoloLaunch(aiCommand, duelPort, duelLp, duelHand, duelDraw, lockHand, noCheck, noShuffle);
                LaunchWithConfig(aiCommand, duelPort, duelLp, duelHand, duelDraw, lockHand, noCheck, noShuffle);
            }
        }

        public void StartAIForRoom(int aiCode, bool diyDeck)
        {
            string aiCommand = GetWindBotCommand(aiCode, diyDeck);
            if (!string.IsNullOrEmpty(aiCommand))
            {
                if (DuelFormat.LocalSession && !DuelFormat.ValidateBotCommand(aiCommand)) return;
                StartWindBot(aiCommand, TcpHelper.joinedAddress, TcpHelper.joinedPort, TcpHelper.joinedPassword, GetUI<SoloSelectorUI>().IsLockHand(), 600);
                Program.instance.ShiftToServant(Program.instance.room);
            }
        }

        public void StartAIForHandTest(int port)
        {
            string aiCommand = GetWindBotCommand(0, false);
            if (!string.IsNullOrEmpty(aiCommand))
                StartWindBot(aiCommand, "127.0.0.1", port.ToString(), string.Empty, true, 0);
        }

        private string GetRandomBot(string flag)
        {
            IList<BotInfo> foundBots = new List<BotInfo>();
            foreach (var bot in bots)
            {
                if (Array.IndexOf(bot.flags, flag) >= 0) foundBots.Add(bot);
            }
            if (foundBots.Count > 0)
            {
                System.Random rand = new System.Random();
                BotInfo bot = foundBots[rand.Next(foundBots.Count)];
                return bot.command;
            }
            return "";
        }
        public void StartWindBot(string command, string ip, string port, string password, bool lockHand, int delay)
        {
            command = command.Replace("'", "\"");
            if (lockHand)
                command += " Hand=1";
            command += " Host=" + ip;
            command += " Port=" + port;
            command += " HostInfo=" + password;

            var args = Tools.SplitWithPreservedQuotes(command);
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("Dialog="))
                {
                    var path = args[i][7..];
                    if (!File.Exists(WINDBOT_DIALOG_PATH + path + ".json"))
                    {
                        var config = Language.GetConfig();
                        // This client ships named localized dialogue files rather than default.json.
                        string fallback = "Universal." + config.Substring(0, 2).ToUpperInvariant();
                        if (!File.Exists(WINDBOT_DIALOG_PATH + fallback + ".json"))
                            fallback = "Universal.EN";
                        args[i] = "Dialog=" + fallback;
                    }
                    break;
                }
            }

            (new Thread(() => { Thread.Sleep(delay); WindBot.Program.Main(args); })).Start();
        }
        public void Launch(string command, bool lockHand, bool noCheck, bool noShuffle)
        {
            var ui = GetUI<SoloSelectorUI>();
            LaunchWithConfig(command, ui.GetPort(), ui.GetLP(), ui.GetHand(), ui.GetDraw(), lockHand, noCheck, noShuffle);
        }

        public bool CanRetryLastSoloDuel()
        {
            return hasLastSoloLaunchConfig;
        }

        public void QueueRetryLastSoloDuel()
        {
            if (!CanRetryLastSoloDuel())
                return;
            retryLastSoloDuelWhenShown = true;
        }

        private void RememberLastSoloLaunch(string command, int duelPort, int duelLp, int duelHand, int duelDraw
            , bool lockHand, bool noCheck, bool noShuffle)
        {
            lastSoloCommand = command;
            lastSoloPort = duelPort;
            lastSoloLp = duelLp;
            lastSoloHand = duelHand;
            lastSoloDraw = duelDraw;
            lastSoloLockHand = lockHand;
            lastSoloNoCheck = noCheck;
            lastSoloNoShuffle = noShuffle;
            hasLastSoloLaunchConfig = true;
        }

        private IEnumerator RetryLastSoloDuelWhenReady()
        {
            yield return null;
            while (showing && inTransition)
                yield return null;

            if (!showing || !retryLastSoloDuelWhenShown || !hasLastSoloLaunchConfig)
                yield break;

            retryLastSoloDuelWhenShown = false;
            LaunchWithConfig(lastSoloCommand, lastSoloPort, lastSoloLp, lastSoloHand, lastSoloDraw
                , lastSoloLockHand, lastSoloNoCheck, lastSoloNoShuffle);
        }

        private void LaunchWithConfig(string command, int duelPort, int duelLp, int duelHand, int duelDraw
            , bool lockHand, bool noCheck, bool noShuffle)
        {
            if (!DuelFormat.RequireNativeRules() || !DuelFormat.ValidateSelectedDeck()
                || !DuelFormat.ValidateBotCommand(command)) return;
            port = duelPort.ToString();

            string lp = duelLp.ToString();
            string hand = duelHand.ToString();
            string draw = duelDraw.ToString();
            int list = DuelFormat.Genesys ? -1 : BanlistManager.GetIndexByName(DuelFormat.TcgBanlist.Name);
            string args = port + " " + list + " 1 0 " + DuelFormat.MasterRule + " F " + (noShuffle ? "T " : "F ") + lp + " " + hand + " " + draw + " 0 0";
            if (TcpHelper.IsPortAvailable(duelPort))
            {
                YgoServer.StartServer(args);
                RoomServant.FromSolo = true;
                if (lockHand)
                    RoomServant.SoloLockHand = true;
                else
                    RoomServant.SoloLockHand = false;
                RoomServant.FromLocalHost = false;
                RoomServant.FromHandTest = false;

                TcpHelper.LinkStart("127.0.0.1", Config.Get("DuelPlayerName0", Config.EMPTY_STRING), port, string.Empty, true
                    , () => StartWindBot(command, "127.0.0.1", port, string.Empty, lockHand, 0));
            }
            else
            {
                MessageManager.messageFromSubString 
                    = InterString.Get("端口被占用， 请尝试修改端口后再尝试。端口号应大于0，小于65535。");
            }
        }

        #endregion
    }
}

