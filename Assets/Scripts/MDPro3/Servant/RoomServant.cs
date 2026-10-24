using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using MDPro3.Duel.YGOSharp;
using MDPro3.UI;
using MDPro3.Net;
using TMPro;
using UnityEngine.EventSystems;
using static MDPro3.UI.ChatPanel;
using MDPro3.UI.ServantUI;
using static MDPro3.Duel.YGOSharp.PacksManager;

namespace MDPro3.Servant
{
    public class RoomServant : Servant
    {
        [HideInInspector] public bool duelEnded;
        public static uint LFList;
        public static byte Rule;
        public static byte Mode;
        public static bool NoCheckDeck;
        public static bool NoShuffleDeck;
        public static int StartLp = 8000;
        public static byte StartHand;
        public static byte DrawCount;
        public static short TimeLimit = 180;
        public static int ObserverCount;
        public static int SelfType;
        public static bool IsHost;
        public static bool NeedSide;
        public static bool JoinWithReconnect;
        public static bool SideWaitingObserver;
        public static bool SoloLockHand;
        public static int CoreShowing = 0;

        public static bool FromSolo;
        public static bool FromLocalHost;
        public static bool FromUltraHost;
        public static bool FromHandTest;

        public class Player
        {
            public string name;
            public bool ready;
        }
        public static Player[] players = new Player[32];
        private static readonly Dictionary<int, OnlineAppearanceData> onlineAppearances = new();

        private Deck deck;

        #region Servant

        public override int Depth => 2;
        protected override bool ShowLine => false;

        public override void Initialize()
        {
            returnServant = Program.instance.online;
            base.Initialize();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);
            CoreShowing = 0;
            Program.instance.ui_.chatPanel.Show(false);
            OcgCore.handler = Handler;
            if(preDepth != Program.instance.deckSelector.Depth)
                _ = Program.instance.appearance.LoadSettingAssets();
            GetUI<RoomServantUI>().RefreshDeckSelector();
        }

        protected override void ApplyHideArrangement(int preDepth)
        {
            Program.instance.ui_.chatPanel.Hide();
            base.ApplyHideArrangement(preDepth);
        }

        public override void OnExit()
        {
            if (FromUltraHost)
            {
                MainMenuUI.ReturnToFriendRooms=true;
                returnServant=Program.instance.menu;
            }
            else if (FromSolo)
                returnServant = Program.instance.solo;
            else
            {
                returnServant = Program.instance.online;
                if (FromLocalHost)
                    YgoServer.StopServer();
            }
            onlineAppearances.Clear();
            base.OnExit();
            Program.instance.ocgcore.CloseConnection();
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;

            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null || selected != lastSelectable)
            {
                if (lastSelectable != null && lastSelectable.gameObject.activeInHierarchy)
                    lastSelectable.Select();
                else
                    servantUI.SelectDefaultSelectable();
            }
        }

        #endregion

        public void Handler(byte[] buffer)
        {
            TcpHelper.CtosMessage_Response(buffer);
        }

        public bool RoomIsFull()
        {
            int playerSeats = 2;
            if (Mode == 2)
                playerSeats = 4;

            for (int i = 0; i < playerSeats; i++)
                if (players[i] == null)
                    return false;
            return true;
        }

        public bool CanChangeDeck()
        {
            if (players[SelfType] != null && players[SelfType].ready)
            {
                MessageManager.Toast(InterString.Get("请先取消准备，再选择卡组。"));
                return false;
            }
            return true;
        }

        public static bool TryGetOnlineAppearanceForSide(int side, out OnlineAppearanceData appearance)
        {
            appearance = default;
            if (!TryGetTeamPlayer(side, false, out var player))
                return false;
            return onlineAppearances.TryGetValue(player, out appearance);
        }

        public static bool TryGetOnlineAppearanceForOpponent(bool tag, out OnlineAppearanceData appearance)
        {
            appearance = default;
            if (!TryGetTeamPlayer(1, tag, out var player))
                return false;
            return onlineAppearances.TryGetValue(player, out appearance);
        }

        public static bool TryGetOnlineAppearanceForPlayer(int player, out OnlineAppearanceData appearance)
        {
            appearance = default;
            if (player < 0 || player >= 4)
                return false;
            return onlineAppearances.TryGetValue(player, out appearance);
        }

        private static bool TryGetTeamPlayer(int side, bool tag, out int player)
        {
            player = -1;
            if (side != 0 && side != 1)
                return false;

            if (Mode != 2)
            {
                if (tag)
                    return false;

                if (SelfType == 7)
                    player = side;
                else
                    player = side == 0 ? SelfType : 1 - SelfType;
                return player >= 0 && player < 4;
            }

            var hostTeamAsSide0 = SelfType == 7 || SelfType < 2;
            var useHostTeam = hostTeamAsSide0 ? side == 0 : side == 1;

            if (useHostTeam)
                player = tag ? 1 : 0;
            else
                player = tag ? 3 : 2;

            return true;
        }

        private static void MoveOnlineAppearance(int from, int to)
        {
            if (onlineAppearances.TryGetValue(from, out var appearance))
            {
                onlineAppearances[to] = appearance;
                onlineAppearances.Remove(from);
            }
            else
            {
                onlineAppearances.Remove(to);
            }
        }

        private static void RemoveOnlineAppearance(int player)
        {
            onlineAppearances.Remove(player);
        }

        private void ShowOcgCore()
        {
            if(CoreShowing == 0)
                CoreShowing = 1;
            if (Program.instance.ocgcore.showing)
                return;
            if (Mode != 2)
            {
                if (SelfType == 7)
                {
                    OcgCore.name_0 = GetPlayerName(0);
                    OcgCore.name_1 = GetPlayerName(1);
                }
                else
                {
                    OcgCore.name_0 = GetPlayerName(SelfType);
                    OcgCore.name_1 = GetPlayerName(1 - SelfType);
                }
                OcgCore.name_0_c = OcgCore.name_0;
                OcgCore.name_1_c = OcgCore.name_1;
                OcgCore.name_0_tag = "---";
                OcgCore.name_1_tag = "---";
            }
            else
            {
                if (SelfType == 7)
                {
                    OcgCore.name_0 = GetPlayerName(0);
                    OcgCore.name_0_tag = GetPlayerName(1);
                    OcgCore.name_1 = GetPlayerName(2);
                    OcgCore.name_1_tag = GetPlayerName(3);
                }
                else
                {
                    int op = 0;
                    int opTag = 0;
                    switch (SelfType)
                    {
                        case 0:
                        case 1:
                            op = 2;
                            opTag = 3;
                            break;
                        case 2:
                        case 3:
                            op = 0;
                            opTag = 1;
                            break;
                    }
                    OcgCore.name_0 = GetPlayerName((SelfType == 0 || SelfType == 2) ? SelfType : SelfType - 1);
                    OcgCore.name_0_tag = GetPlayerName((SelfType == 0 || SelfType == 2) ? SelfType + 1 : SelfType);
                    OcgCore.name_1 = GetPlayerName(op);
                    OcgCore.name_1_tag = GetPlayerName(opTag);
                }
            }
            OcgCore.timeLimit = TimeLimit;
            OcgCore.lpLimit = StartLp;
            if(FromSolo)
                Program.instance.ocgcore.returnServant = Program.instance.solo;
            else if(FromHandTest)
                Program.instance.ocgcore.returnServant = Program.instance.deckEditor;
            else
                Program.instance.ocgcore.returnServant = FromUltraHost ? Program.instance.menu : Program.instance.online;
            if (SelfType == 7)
                OcgCore.condition = OcgCore.Condition.Watch;
            else
                OcgCore.condition = OcgCore.Condition.Duel;
            OcgCore.inPuzzle = false;
            Program.instance.ShiftToServant(Program.instance.ocgcore);
        }

        public void Realize()
        {
            if (servantUI == null)
                return;
            if (FromHandTest)
                return;

            GetUI<RoomServantUI>().Realize();
        }

        #region STOC

        private void GoFirst(bool first)
        {
            TcpHelper.CtosMessage_TpResult(first);
        }

        public void StocMessage_GameMsg(BinaryReader r)
        {
            ShowOcgCore();
            var p = new Package
            {
                Function = r.ReadByte(),
                Data = new BinaryMaster(r.ReadToEnd())
            };
            Program.instance.ocgcore.AddPackage(p);
        }

        public void StocMessage_ErrorMsg(BinaryReader r)
        {
            int msg = r.ReadByte();
            r.ReadByte();
            r.ReadByte();
            r.ReadByte();
            var code = r.ReadInt32();
            switch (msg)
            {
                case 1:
                    switch (code)
                    {
                        case 0:
                            MessageManager.Cast(StringHelper.GetUnsafe(1403)); // 无法加入主机。
                            break;
                        case 1:
                            MessageManager.Cast(StringHelper.GetUnsafe(1404)); // 密码错误。
                            break;
                        case 2:
                            MessageManager.Cast(StringHelper.GetUnsafe(1405)); // 主机拒绝了连接。
                            break;
                    }
                    break;
                case 2: // DECKERROR_LFLIST
                    var flag = code >> 28;
                    code &= 0xFFFFFFF;
                    var cardName = CardsManager.Get(code).Name;
                    List<string> tasks = new() { StringHelper.GetUnsafe(1406) }; //无效卡组。
                    string task;
                    switch (flag)
                    {
                        case 1:
                            task = StringHelper.GetUnsafe(1407); //「%ls」的数量不符合当前禁限卡表设定。
                            var replace = new Regex("%ls");
                            task = replace.Replace(task, cardName);
                            break;
                        case 2: // 「%ls」为OCG独有卡，不允许在当前设定下使用。
                        case 3: // 「%ls」为TCG独有卡，不允许在当前设定下使用。
                        case 4: // 卡组中「%ls(%d)」无法被主机识别。
                        case 5: // 卡组中「%ls」的总数量超过3张。
                            task = StringHelper.GetUnsafe(1411 + flag);
                            replace = new Regex("%ls");
                            task = replace.Replace(task, cardName);
                            if(flag == 4)
                            {
                                replace = new Regex("%d");
                                task = replace.Replace(task, code.ToString());
                            }
                            break;
                        case 6: // 主卡组数量应为40-60张，当前卡组数量为%d张。
                        case 7: // 额外卡组数量应不超过15张，当前卡组数量为%d张。
                        case 8: // 副卡组数量应不超过15张，当前卡组数量为%d张。
                        case 9: // 有额外卡组卡片存在于主卡组，可能是额外卡组数量超过15张。
                            task = StringHelper.GetUnsafe(1411 + flag);
                            replace = new Regex("%d");
                            deck = new Deck(Program.PATH_DECK + Config.GetConfigDeckName() + Program.EXPANSION_YDK);
                            if(deck != null)
                            {
                                string target;
                                if (flag == 6)
                                    target = deck.Main.Count.ToString();
                                else if (flag == 7)
                                    target = deck.Extra.Count.ToString();
                                else if (flag == 8)
                                    target = deck.Side.Count.ToString();
                                else
                                    break;
                                task = replace.Replace(task, target);
                            }
                            break;
                        default:
                            task = StringHelper.GetUnsafe(1406); // 无效卡组。
                            break;
                    }
                    tasks.Add(task);
                    UIManager.ShowPopupConfirm(tasks);
                    break;
                case 3:
                    tasks = new List<string>()
                    {
                        StringHelper.GetUnsafe(1408), // 更换副卡组失败。
                        StringHelper.GetUnsafe(1410),
                    };
                    UIManager.ShowPopupConfirm(tasks);
                    break;
                case 4:
                    Debug.Log("ERROR 4: " + code);
                    break;
            }
        }

        public void StocMessage_SelectHand(BinaryReader r)
        {
            if (SoloLockHand || Config.Get("AutoRPS", "0") == "0")
            {
                var handle = Addressables.InstantiateAsync("Popup/PopupRockPaperScissors.prefab");
                handle.Completed += (result) =>
                {
                    result.Result.transform.SetParent(Program.instance.ui_.popup, false);
                    var popupRPS = result.Result.GetComponent<UI.Popup.PopupRockPaperScissors>();
                    popupRPS.args = new List<string> { InterString.Get("猜拳") };
                    popupRPS.Show();
                };
            }
            else
                TcpHelper.CtosMessage_HandResult(Random.Range(1, 4));
        }

        public void StocMessage_SelectTp(BinaryReader r)
        {
            List<string> selections = new List<string>
            {
                Program.instance.currentServant == Program.instance.room ?
                InterString.Get("猜拳获胜") :
                InterString.Get("选择先后手"),
                InterString.Get("选择是否由我方先手？"),
                InterString.Get("先攻"),
                InterString.Get("后攻")
            };
            UIManager.ShowPopupYesOrNo(selections, () => { GoFirst(true); }, () => { GoFirst(false); });
        }

        public void StocMessage_HandResult(BinaryReader r)
        {
            if (SelfType == 7)
                return;

            int meResult = r.ReadByte();
            int opResult = r.ReadByte();
            if (meResult == opResult)
                MessageManager.Cast(InterString.Get("猜拳平局。"));
            else if (meResult == 1 && opResult == 2
                || meResult == 2 && opResult == 3
                || meResult == 3 && opResult == 1)
                MessageManager.Cast(InterString.Get("猜拳落败。"));
        }

        public void StocMessage_TpResult(BinaryReader r)
        {
        }

        public void StocMessage_ChangeSide(BinaryReader r)
        {
            NeedSide = true;
            if (OcgCore.condition != OcgCore.Condition.Duel || JoinWithReconnect)
                Program.instance.ocgcore.OnDuelResultConfirmed();
        }

        public void StocMessage_WaitingSide(BinaryReader r)
        {
            SideWaitingObserver = true;
            MessageManager.Cast(InterString.Get("请耐心等待双方玩家更换副卡组。"));
        }

        public void StocMessage_DeckCount(BinaryReader r)
        {
        }

        public void StocMessage_CreateGame(BinaryReader r)
        {
        }

        public void StocMessage_JoinGame(BinaryReader r)
        {
            LFList = r.ReadUInt32();
            Rule = r.ReadByte();
            Mode = r.ReadByte();
            OcgCore.MasterRule = r.ReadChar();
            NoCheckDeck = r.ReadBoolean();
            NoShuffleDeck = r.ReadBoolean();
            r.ReadByte();
            r.ReadByte();
            r.ReadByte();
            StartLp = r.ReadInt32();
            StartHand = r.ReadByte();
            DrawCount = r.ReadByte();
            TimeLimit = r.ReadInt16();

            for (int i = 0; i < 4; i++)
                players[i] = null;
            onlineAppearances.Clear();

            if(!FromHandTest)
                Program.instance.ShiftToServant(Program.instance.room);
        }

        public void StocMessage_TypeChange(BinaryReader r)
        {
            int type = r.ReadByte();
            SelfType = type & 0xF;
            IsHost = ((type >> 4) & 0xF) != 0;
            if (SelfType < 4 && players[SelfType] != null)
                players[SelfType].ready = false;
            if (SelfType < 4)
                TcpHelper.CtosMessage_UpdateAppearanceFromCurrentDeck();
            Realize();
        }

        public void StocMessage_LeaveGame(BinaryReader r)
        {
        }

        public void StocMessage_DuelStart(BinaryReader r)
        {
            NeedSide = false;
            JoinWithReconnect = true;
            if (Program.instance.deckEditor.showing)
            {
                Program.instance.deckEditor.Hide(0);
                if(!FromHandTest)
                    MessageManager.Cast(InterString.Get("更换副卡组成功，请等待对手更换副卡组。"));
            }

            if (showing)
                Hide(0);

            UIManager.HideExitButton(TransitionTime);
        }

        public void StocMessage_DuelEnd(BinaryReader r)
        {
            duelEnded = true;
            Program.instance.ocgcore.ForceMSquit();
        }

        public void StocMessage_Replay(BinaryReader r)
        {
            var data = r.ReadToEnd();
            var p = new Package();
            p.Function = (int)GameMessage.sibyl_replay;
            p.Data.writer.Write(data);
            TcpHelper.AddRecordLine(p);
        }

        public void StocMessage_Chat(BinaryReader r)
        {
            int player = r.ReadInt16();
            var length = (int)((r.BaseStream.Length - r.BaseStream.Position) / 2);
            var content = r.ReadUnicode(length);
            if (OnlineAppearanceSync.IsSyncMessage(content))
            {
                if (player >= 0 && player < 4 && OnlineAppearanceSync.TryParse(content, out var appearance))
                {
                    onlineAppearances[player] = appearance;
                    if (showing)
                        Realize();
                    Debug.Log($"[OnlineAppearance] Received sync from seat {player}: {appearance.Case},{appearance.Protector},{appearance.Field},{appearance.Grave},{appearance.Stand},{appearance.Mate},{appearance.Face},{appearance.Frame}");
                }
                else
                {
                    Debug.LogWarning($"[OnlineAppearance] Ignored sync chat. seat={player}");
                }
                return;
            }
            //Debug.Log($"{player}: {content} {OcgCore.isFirst} {RoomServant.SelfType}");
            Program.instance.ui_.chatPanel.AddChatItem(player, content);
        }

        public void StocMessage_HsPlayerEnter(BinaryReader r)
        {
            if(!FromHandTest)
                AudioManager.PlaySE("SE_ROOM_SITDOWN");
            var name = r.ReadUnicode(20);
            var pos = r.ReadByte() & 3;
            var player = new Player();
            player.name = name;
            player.ready = false;
            players[pos] = player;
            RemoveOnlineAppearance(pos);
            if (SelfType < 4 && players[SelfType] != null && pos != SelfType)
                TcpHelper.CtosMessage_UpdateAppearanceFromCurrentDeck();
            Realize();
        }

        public void StocMessage_HsPlayerChange(BinaryReader r)
        {
            int status = r.ReadByte();
            var pos = (status >> 4) & 0xF;
            var state = status & 0xF;
            if (pos < 4)
            {
                if (state < 8)
                {
                    players[state] = players[pos];
                    players[pos] = null;
                    MoveOnlineAppearance(pos, state);
                }
                if (state == 0x9)
                    players[pos].ready = true;
                if (state == 0xA)
                    players[pos].ready = false;
                if (state == 0xB)
                {
                    players[pos] = null;
                    RemoveOnlineAppearance(pos);
                }
                if (state == 0x8)
                {
                    players[pos] = null;
                    RemoveOnlineAppearance(pos);
                    ObserverCount++;
                }
                Realize();
            }
        }

        public void StocMessage_HsWatchChange(BinaryReader r)
        {
            ObserverCount = r.ReadUInt16();
            Realize();
        }

        #endregion
    }
}
