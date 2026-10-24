using Cysharp.Threading.Tasks;
using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI;
using MDPro3.UI.ServantUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using YgomSystem.ElementSystem;
using static MDPro3.Servant.OcgCore;

namespace MDPro3.Duel
{
    public class DuelMessage : MessageProcessor
    {
        public DuelMessage(MessageDispatcher dispatcher) : base(dispatcher)
        {
            duelBGManager = new()
            {
                processor = this
            };
        }

        #region Parameters

        public readonly DuelBGManager duelBGManager;

        private bool preload;
        private Vector3 effectDefenceAngle = new(0f, 90f, 0f);

        #endregion

        public override void Dispose()
        {
            base.Dispose();

            duelBGManager.Dispose();
        }

        private void LogDebug(string text)
        {
            Debug.LogError(text);
            MessageManager.Cast(text);
        }

        private void DebugNoCard()
        {
            LogDebug($"[Duel]: Not found card for {currentMessage}.");
        }

        private void ResetState()
        {
            if (cards.Count > 0)
                foreach (var card in cards)
                    card.Dispose();

            cards.Clear();

            tempCards.Clear();
            sideReference = new Deck();
            pause = false;
            duelEnded = false;
            turns = 0;
            handOffset = 0;
            lastHandOffset = 0;
            myPreHandCards.Clear();
            opPreHandCards.Clear();
            needRefreshMyHand = true;
            needRefreshOpHand = true;
            materialCards.Clear();
            cardsInChain.Clear();
            codesInChain.Clear();
            controllerInChain.Clear();
            negatedInChain.Clear();
            cardsBeTarget.Clear();
            cardsInSelection.Clear();
            cardsMustBeSelected.Clear();
            myActivated.Clear();
            opActivated.Clear();
            Program.instance.ocgcore.GetUI<OcgCoreUI>().CardDescription.Hide();
            Program.instance.ocgcore.GetUI<OcgCoreUI>().CardList.Hide();
            Program.instance.ocgcore.GetUI<OcgCoreUI>().ResetBgDetailState();
            surrendered = false;
            tagSurrendered = false;
            deckReserved = false;
            cantCheckGrave = false;
            cookie_matchKill = 0;
            Config.Set("MateViewTips", "0");
            if (condition == Condition.Duel)
            {
                if (Config.GetBool(Config.LABEL_TIMING, false))
                {
                    chainCondition = ChainCondition.All - 1;
                    Program.instance.ocgcore.GetUI<OcgCoreUI>().OnTiming();
                }
                else
                {
                    chainCondition = ChainCondition.Smart - 1;
                    Program.instance.ocgcore.GetUI<OcgCoreUI>().OnTiming();
                }
            }
            Program.instance.ocgcore.GetUI<OcgCoreUI>().HidePlaceCount();
            mySummonCount = 0;
            mySpSummonCount = 0;
            opSummonCount = 0;
            opSpSummonCount = 0;
            RoomServant.JoinWithReconnect = false;
            endingAction = null;
            nextMoveAction = null;
            Program.instance.ocgcore.GetUI<OcgCoreUI>().DuelLog.ClearLog();
            Program.instance.ocgcore.GetUI<OcgCoreUI>().DuelLog.showing = true;
            Program.instance.ocgcore.GetUI<OcgCoreUI>().OnLog(true);

            Program.instance.ocgcore.greenBackground.gameObject.SetActive(false);
            inputMode = false;
            Program.instance.ocgcore.returnAction = null;
            movingToMyGrave = 0;
            movingToMyExclude = 0;
            movingToOpGrave = 0;
            movingToMyExclude = 0;
        }

        public async UniTask PreloadPlayerNames()
        {
            preload = true;

            Core.GetUI<OcgCoreUI>().TextPlayer0LP.text = string.Empty;
            Core.GetUI<OcgCoreUI>().TextPlayer1LP.text = string.Empty;
            for (int i = 0; i < packages.Count; i++)
            {
                if ((GameMessage)packages[i].Function == GameMessage.Start
                    || (GameMessage)packages[i].Function == GameMessage.AiName
                    || (GameMessage)packages[i].Function == GameMessage.sibyl_name
                    )
                {
                    await Process(packages[i]);
                    break;
                }
                else
                    continue;
            }

            preload = false;
        }

        private async UniTask<int> PreloadUpdateCardMessages()
        {
            if (packages.Count < 2)
                return 0;
            for (int i = 1; i < packages.Count; i++)
            {
                if ((GameMessage)packages[i].Function == GameMessage.UpdateData)
                    await Process(packages[i]);
                else
                    return i;
            }
            return 0;
        }

        #region Message Process

        protected override async UniTask GameMessage_Start(BinaryReader reader)
        {
            ResetState();
            duelBGManager.BackgroundFieldInitialize();

            playerType = reader.ReadByte();
            isFirst = (playerType & 0xF) == 0;
            isObserver = (playerType & 0xF0) > 0;
            if (condition == Condition.Duel && !isObserver && !RoomServant.FromHandTest && !inPuzzle) UltraProgress.BeginDuel();
            if (reader.BaseStream.Length > 17)
                MasterRule = reader.ReadByte() + 1;
            var engineLife0 = reader.ReadInt32();
            var engineLife1 = reader.ReadInt32();
            life0 = isFirst ? engineLife0 : engineLife1;
            life1 = isFirst ? engineLife1 : engineLife0;
            lpLimit = Mathf.Max(life0, life1);
            turns = 0;

            RoomServant.CoreShowing = 2;
            Core.GetUI<OcgCoreUI>().TextPlayer0Name.text = name_0;
            Core.GetUI<OcgCoreUI>().TextPlayer1Name.text = name_1;

            if (RoomServant.Mode == 2)
            {
                if (isFirst)
                    Core.GetUI<OcgCoreUI>().TextPlayer1Name.text = name_1_tag;
                else
                    Core.GetUI<OcgCoreUI>().TextPlayer0Name.text = name_0_tag;
                isTag = true;
            }
            else
                isTag = false;

            Core.SetFace();

            if(preload)
                return;

            Core.GCS_CreateBundle(reader.ReadInt16(), LocalPlayer(0), CardLocation.Deck);
            Core.GCS_CreateBundle(reader.ReadInt16(), LocalPlayer(0), CardLocation.Extra);
            Core.GCS_CreateBundle(reader.ReadInt16(), LocalPlayer(1), CardLocation.Deck);
            Core.GCS_CreateBundle(reader.ReadInt16(), LocalPlayer(1), CardLocation.Extra);
            Core.ArrangeCards();
            duelBGManager.RefreshBgState();
            Core.SetLP(0, 0, true);

            await UniTask.WaitForSeconds(Core.TransitionTime);
            await duelBGManager.ShowDecksWithDuelStartTextAsync();
        }

        protected override async UniTask GameMessage_ReloadField(BinaryReader reader)
        {
            ResetState();

            MasterRule = reader.ReadByte() + 1;
            if (MasterRule > 255)
                MasterRule -= 255;

            if (inPuzzle)
            {
                isFirst = true;
                myTurn = true;
            }
            PhaseButtonHandler.TurnChange(myTurn, 1);

            for (int p = 0; p < 2; p++)
            {
                var player = LocalPlayer(p);
                if (player == 0)
                    life0 = reader.ReadInt32();
                else
                    life1 = reader.ReadInt32();

                for (int i = 0; i < 7; i++)
                {
                    var count = reader.ReadByte();
                    if (count > 0)
                    {
                        var gps = new GPS
                        {
                            controller = (uint)player,
                            location = (uint)CardLocation.MonsterZone,
                            position = reader.ReadByte(),
                            sequence = (uint)i
                        };
                        Core.GCS_Create(gps);
                        count = reader.ReadByte();
                        for (var xyz = 0; xyz < count; ++xyz)
                        {
                            var overlay = new GPS
                            {
                                controller = gps.controller,
                                location = (uint)CardLocation.MonsterZone | (uint)CardLocation.Overlay,
                                position = xyz,
                                sequence = gps.sequence
                            };
                            Core.GCS_Create(overlay);
                        }
                    }
                }

                for (var i = 0; i < 8; i++)
                {
                    var count = reader.ReadByte();
                    if (count > 0)
                    {
                        var gps = new GPS
                        {
                            controller = (uint)player,
                            location = (uint)CardLocation.SpellZone,
                            position = reader.ReadByte(),
                            sequence = (uint)i
                        };
                        Core.GCS_Create(gps);
                    }
                }

                var value = reader.ReadByte();
                for(var i = 0; i < value; i++)
                {
                    var gps = new GPS
                    {
                        controller = (uint)player,
                        location = (uint)CardLocation.Deck,
                        position = (int)CardPosition.FaceDownAttack,
                        sequence = (uint)i
                    };
                    Core.GCS_Create(gps);
                }

                value = reader.ReadByte();
                for (var i = 0; i < value; i++)
                {
                    var gps = new GPS
                    {
                        controller = (uint)player,
                        location = (uint)CardLocation.Hand,
                        position = (int)CardPosition.FaceDownAttack,
                        sequence = (uint)i
                    };
                    Core.GCS_Create(gps);
                }

                value = reader.ReadByte();
                for (var i = 0; i < value; i++)
                {
                    var gps = new GPS
                    {
                        controller = (uint)player,
                        location = (uint)CardLocation.Grave,
                        position = (int)CardPosition.FaceUpAttack,
                        sequence = (uint)i
                    };
                    Core.GCS_Create(gps);
                }

                value = reader.ReadByte();
                for (var i = 0; i < value; i++)
                {
                    var gps = new GPS
                    {
                        controller = (uint)player,
                        location = (uint)CardLocation.Removed,
                        position = (int)CardPosition.FaceUpAttack,
                        sequence = (uint)i
                    };
                    Core.GCS_Create(gps);
                }

                value = reader.ReadByte();
                var valueUp = reader.ReadByte();
                for (var i = 0; i < value - valueUp; i++)
                {
                    var gps = new GPS
                    {
                        controller = (uint)player,
                        location = (uint)CardLocation.Extra,
                        position = (int)CardPosition.FaceDownAttack,
                        sequence = (uint)i
                    };
                    Core.GCS_Create(gps);
                }
                for (var i = 0; i < valueUp; i++)
                {
                    var gps = new GPS
                    {
                        controller = (uint)player,
                        location = (uint)CardLocation.Extra,
                        position = (int)CardPosition.FaceUpAttack,
                        sequence = (uint)i
                    };
                    Core.GCS_Create(gps);
                }
            }

            duelBGManager.UpdateBgEffects(0, true);
            duelBGManager.UpdateBgEffects(1, true);
            Core.SetLP(0, 0, true);
            Core.ArrangeCards();
            duelBGManager.RefreshBgState();

            for (int i = 0; i < cards.Count; i++)
                _ =cards[i].MoveAsync(cards[i].p, true);

            await PreloadUpdateCardMessages();
            await duelBGManager.ShowDecksAsync();
        }

        protected override UniTask GameMessage_sibyl_chat(BinaryReader reader)
        {
            var player = (int)reader.ReadUInt32();
            if(!Core.GetMessageConfig(player))
                return UniTask.CompletedTask;
            var name = string.Empty;
            if (isTag)
            {
                switch (player)
                {
                    case 0:
                        name = name_0;
                        if (playerType < 7 &&
                            ((playerType < 2 && !isFirst) || (playerType >= 2 && isFirst)))
                            name = name_1;
                        break;
                    case 1:
                        name = name_0_tag;
                        if (playerType < 7 &&
                            ((playerType < 2 && !isFirst) || (playerType >= 2 && isFirst)))
                            name = name_1_tag;
                        break;
                    case 2:
                        name = name_1;
                        if (playerType < 7 &&
                            ((playerType < 2 && !isFirst) || (playerType >= 2 && isFirst)))
                            name = name_0;
                        break;
                    case 3:
                        name = name_1_tag;
                        if (playerType < 7 &&
                            ((playerType < 2 && !isFirst) || (playerType >= 2 && isFirst)))
                            name = name_0_tag;
                        break;
                }
            }
            else
            {
                switch (player)
                {
                    case 0:
                        name = name_0;
                        if (playerType < 7 &&
                            ((playerType == 0 && !isFirst) || (playerType == 1 && isFirst)))
                            name = name_1;
                        break;
                    case 1:
                        name = name_1;
                        if (playerType < 7 &&
                            ((playerType == 0 && !isFirst) || (playerType == 1 && isFirst)))
                            name = name_0;
                        break;
                }
            }

            if (player == 7)
                name = InterString.Get("观战者");
            if (name != string.Empty)
                name += ": ";

            var content = reader.ReadALLUnicode();
            MessageManager.Cast(name + content);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_sibyl_name(BinaryReader reader)
        {
            name_0 = reader.ReadUnicode(50);
            name_0_tag = reader.ReadUnicode(50);
            name_0_c = reader.ReadUnicode(50);
            name_1 = reader.ReadUnicode(50);
            name_1_tag = reader.ReadUnicode(50);
            name_1_c = reader.ReadUnicode(50);

            isTag = !(name_0_tag == "---" 
                && name_1_tag == "---" 
                && name_0 == name_0_c 
                && name_1 == name_1_c);

            if (Config.Get("ReplayPlayerName0", Config.EMPTY_STRING).Length > 0)
                name_0 = Config.Get("ReplayPlayerName0", Config.EMPTY_STRING);
            if (Config.Get("ReplayPlayerName1", Config.EMPTY_STRING).Length > 0)
                name_1 = Config.Get("ReplayPlayerName1", Config.EMPTY_STRING);
            if (Config.Get("ReplayPlayerName0Tag", Config.EMPTY_STRING).Length > 0)
                name_0_tag = Config.Get("ReplayPlayerName0Tag", Config.EMPTY_STRING);
            if (Config.Get("ReplayPlayerName1Tag", Config.EMPTY_STRING).Length > 0)
                name_1_tag = Config.Get("ReplayPlayerName1Tag", Config.EMPTY_STRING);

            if (isTag)
            {
                if (isFirst)
                {
                    name_0_c = name_0;
                    name_1_c = name_1_tag;
                }
                else
                {
                    name_0_c = name_0_tag;
                    name_1_c = name_1;
                }
            }
            else
            {
                name_0_c = name_0;
                name_1_c = name_1;
            }

            Core.GetUI<OcgCoreUI>().TextPlayer0Name.text = name_0_c;
            Core.GetUI<OcgCoreUI>().TextPlayer1Name.text = name_1_c;
            Core.SetFace();
            
            if (reader.BaseStream.Position < reader.BaseStream.Length)
                MasterRule = reader.ReadInt32();
            else
                MasterRule = 3;

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_sibyl_quit(BinaryReader reader)
        {
            duelEnded = true;
            duelResult = DuelResult.DisLink;

            return UniTask.CompletedTask;
        }

        protected override async UniTask GameMessage_Retry(BinaryReader reader)
        {
            MessageManager.Cast(InterString.Get("游戏出错，请重试。或者保存回放并联系开发者。"));
            //Todo Show SaveReplay.
            await dispatcher.RetryMessage();
        }

        protected override UniTask GameMessage_ShowHint(BinaryReader reader)
        {
            /*var lenth = */reader.ReadInt16();
            var buffer = reader.ReadToEnd();
            var text = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            MessageManager.Cast(text);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_AiName(BinaryReader reader)
        {
            var length = reader.ReadInt16();
            var buffer = reader.ReadBytes(length + 1);
            var text = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

            name_0 = Config.Get("DuelPlayerName0", Config.EMPTY_STRING);
            name_0_c = name_0;
            name_1 = text;
            name_1_c = name_1;
            Core.GetUI<OcgCoreUI>().TextPlayer0Name.text = name_0_c;
            Core.GetUI<OcgCoreUI>().TextPlayer1Name.text = name_1_c;
            isTag = false;
            Core.SetFace();

            return UniTask.CompletedTask;
        }

        protected override async UniTask GameMessage_Win(BinaryReader reader)
        {
            deckReserved = false;
            cantCheckGrave = false;
            duelEnded = true;

            Core.GetUI<OcgCoreUI>().CardDescription.Hide();
            duelBGManager.ClearResponse();
            AudioManager.StopBGM();

            if (Core.currentPopup != null)
            {
                Core.currentPopup.whenQuitDo = null;
                Core.currentPopup.Hide();
                Core.returnAction = null;
            }

            var player = LocalPlayer(reader.ReadByte());
            var winType = reader.ReadByte();

            var duelText = string.Empty;
            string endingReason = string.Empty;
            if (player == 2)
            {
                duelResult = DuelResult.Draw;
                duelText = "DuelTextDraw";
            }
            else if (player == 0 || winType == 4)
            {
                duelResult = DuelResult.Win;
                duelText = "DuelTextWin";
                if (cookie_matchKill > 0)
                {
                    winReason = CardsManager.Get(cookie_matchKill).Name;
                    endingReason = InterString.Get("比赛胜利，卡片：[?]", winReason);
                }
                else
                {
                    winReason = StringHelper.Get("victory", winType);
                    endingReason = InterString.Get("游戏胜利，原因：[?]", winReason);
                }
            }
            else
            {
                duelResult = DuelResult.Lose;
                duelText = "DuelTextLose";
                if (cookie_matchKill > 0)
                {
                    winReason = CardsManager.Get(cookie_matchKill).Name;
                    endingReason = InterString.Get("比赛败北，卡片：[?]", winReason);
                }
                else
                {
                    winReason = StringHelper.Get("victory", winType);
                    endingReason = InterString.Get("游戏败北，原因：[?]", winReason);
                }
            }

            if (condition == Condition.Duel && !isObserver && !RoomServant.FromHandTest && !inPuzzle)
            {
                UltraProgress.Finish(player,turns);
                if(player==0)MessageManager.Cast("Victory! +100 Duel Pass XP");
            }

            duelBGManager.DuelEndEvent();
            //防止对方在更换副卡组时拔螺丝
            UIManager.UIBlackOut(Core.TransitionTime);
            Core.GetUI<OcgCoreUI>().CG.blocksRaycasts = true;
            Core.GetUI<OcgCoreUI>().CG.alpha = 1f;
            Core.GetUI<OcgCoreUI>().Buttons.SetActive(true);

            if(cookie_matchKill > 0)
            {
                await duelBGManager.PlayCommonSpecialWin(new int[] { cookie_matchKill });
            }
            else if (winType >= 0x10)
            {
                if (winType == 0x10)//被封印的艾克佐迪亚
                {
                    static void Action(ElementObjectManager mner)
                    {
                        _ = Program.instance.texture_.LoadDummyCard(mner.GetElement<ElementObjectManager>("DummyCard"), 33396948, 0, true);
                        _ = Program.instance.texture_.LoadDummyCard(mner.GetElement<ElementObjectManager>("DummyCard2"), 7902349, 0, true);
                        _ = Program.instance.texture_.LoadDummyCard(mner.GetElement<ElementObjectManager>("DummyCard3"), 70903634, 0, true);
                        _ = Program.instance.texture_.LoadDummyCard(mner.GetElement<ElementObjectManager>("DummyCard4"), 44519536, 0, true);
                        _ = Program.instance.texture_.LoadDummyCard(mner.GetElement<ElementObjectManager>("DummyCard5"), 8124921, 0, true);
                    }
                    await duelBGManager.PlaySpecialWin("33396948", Action);
                }
                else if (winType == 0x11)//终焉的倒计时
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 95308449 });
                else if (winType == 0x12)//毒蛇神 维诺米纳迦
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 8062132 });
                else if (winType == 0x13)//光之创造神 哈拉克提
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 10000040 });
                else if (winType == 0x14)//究极封印神 艾克佐迪奥斯
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 13893596 });
                else if (winType == 0x15)//通灵盘
                    await duelBGManager.PlaySpecialWin("40771118");
                else if (winType == 0x16)//最终一战！
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 28566710 });
                else if (winType == 0x17)//No.88 机关傀儡-命运狮子
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 48995978 });
                else if (winType == 0x18)//混沌No.88 机关傀儡-灾厄狮子
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 6165656 });
                else if (winType == 0x19)//头奖壶7
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 81171949, 81171949, 81171949 });
                else if (winType == 0x1A)//魂之接力
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 42776960 });
                else if (winType == 0x1B)//鬼计惰天使
                    await duelBGManager.PlaySpecialWin("53334641");
                else if (winType == 0x1C)//幻煌龙的天涡
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 97795930 });
                else if (winType == 0x1D)//方程式运动员胜利团队
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 69553552 });
                else if (winType == 0x1E)//飞行象
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 66765023 });
                else if (winType == 0x1F)//守护神 艾克佐迪亚
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 5008836 });
                else if (winType == 0x20)//真艾克佐迪亚
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 37984331 });
                else if (winType == 0x21)//混沌虚数No.1000 梦幻虚光神 原数天灵·原数天地
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 15862758 });
                else if (winType == 0x22)//席取-六双丸
                    await duelBGManager.PlaySpecialWin("96637156");
                else if (winType == 0x23)//火器的祝台
                    await duelBGManager.PlayCommonSpecialWin(new int[] { 77751766 });
                else
                    MessageManager.Cast(InterString.Get("请联系开发者修复这张特殊胜利的卡。"));
            }

            // Start win/lose field animation first, then show result text while it is ongoing.
            duelBGManager.ShowBGEnd(duelResult);
            await duelBGManager.ShowDuelResultText(duelText);

            MessageManager.Cast(endingReason);
            if(condition==Condition.Duel && !isObserver && !RoomServant.FromHandTest && !inPuzzle && UltraProgress.LastMatch?.result!=null)
            {
                TcpHelper.SaveRecord("Ultra-"+System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")+"-"+UltraProgress.LastMatch.id.Substring(0,6));
                UltraResultsScreen.Show(Core.GetUI<OcgCoreUI>().transform,UltraProgress.LastMatch,()=>Core.OnDuelResultConfirmed());
            }
            else if (condition != Condition.Replay)
                Core.GetUI<OcgCoreUI>().ShowSaveReplay();
            else
                Core.NotifyReplayPlaybackEnded();
        }

        protected override UniTask GameMessage_UpdateData(BinaryReader reader)
        {
            /*var player = */LocalPlayer(reader.ReadChar());
            /*var location = */reader.ReadChar();

            try
            {
                while (true)
                {
                    var len = reader.ReadInt32();
                    if (len == 4) continue;
                    var pos = reader.BaseStream.Position;
                    reader.ReadCardData();
                    reader.BaseStream.Position = pos + len - 4;
                }
            }
            catch { }

            inPendulumSummon = false;
            myPreHandCards.Clear();
            opPreHandCards.Clear();
            Core.RefreshHandCardPosition();
            duelBGManager.RefreshBgState();

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_UpdateCard(BinaryReader reader)
        {
            var gps = reader.ReadShortGPS();
            var card = Core.GCS_Get(gps);
            reader.ReadUInt32();
            reader.ReadCardData(card);

            return UniTask.CompletedTask;
        }

        protected override async UniTask GameMessage_Move(BinaryReader reader)
        {
            var code = reader.ReadInt32();
            var from = reader.ReadGPS();
            var to = reader.ReadGPS();
            var reason = reader.ReadUInt32();
            if(from.controller==1 && from.InLocation(CardLocation.MonsterZone) && (reason&1)!=0)UltraProgress.Destroyed();

            var card = Core.GCS_Get(from);
            if(card == null)
            {
                //DebugNoCard();
                card = Core.GCS_Create(from);
            }

            lastMoveCard = card;
            card.SetCode(code);
            to.reason = reason;
            nextMoveNeedCode = false;

            if(CanSyncNextMove(from, to))
                _ = card.MoveAsync(to);
            else
                await card.MoveAsync(to);
        }

        protected override async UniTask GameMessage_PosChange(BinaryReader reader)
        {
            ES_hint = StringHelper.GetUnsafe(1600);//卡片改变了表示形式
            var code = reader.ReadInt32();
            var from = reader.ReadGPS();
            var card = Core.GCS_Get(from);
            if(card == null)
            {
                DebugNoCard();
                return;
            }

            var to = from;
            to.position = reader.ReadByte();
            card.ShowFaceDownCardOrNot(false);
            card.SetCode(code);
            await card.MoveAsync(to);
            if(to.InPosition(CardPosition.FaceUp)
                && to.InLocation(CardLocation.MonsterZone))
            {
                card.AnimationPositon();
                await UniTask.WaitForSeconds(0.3f);
            }
        }

        protected override UniTask GameMessage_Set(BinaryReader reader)
        {
            ES_hint = StringHelper.GetUnsafe(1601);//盖放了卡片
            var effect = ABLoader.LoadMasterDuelGameObject("fxp_som_mgctrpfld_001");
            effect.transform.position = lastMoveCard.model.transform.position;
            UnityEngine.Object.Destroy(effect, 3f);
            AudioManager.PlaySE("SE_LAND_MT_SET");
            return UniTask.CompletedTask;
        }

        protected override async UniTask GameMessage_Swap(BinaryReader reader)
        {
            /*var code = */reader.ReadInt32();
            var from = reader.ReadGPS();
            /*code = */reader.ReadInt32();
            var to = reader.ReadGPS();

            if(from.controller != to.controller)
                ES_hint = StringHelper.GetUnsafe(1602);//卡的控制权改变了

            var from2 = new GPS
            {
                controller = from.controller,
                location = from.location,
                sequence = from.sequence,
                position = to.position
            };
            var to2 = new GPS
            {
                controller = to.controller,
                location = to.location,
                sequence = to.sequence,
                position = from.position
            };
            var card = Core.GCS_Get(from);
            var card2 = Core.GCS_Get(to);

            if (card == null || card2 == null)
            {
                DebugNoCard();
                return;
            }

            _ = card.MoveAsync(to2);
            await card2.MoveAsync(from2);
        }

        protected override async UniTask GameMessage_Summoning(BinaryReader reader)
        {
            cardsInSelection.Clear();

            var code = reader.ReadInt32();
            var gps = reader.ReadGPS();
            var card = Core.GCS_Get(gps);
            if(card == null)
            {
                DebugNoCard();
                return;
            }

            if(gps.InMyControl())
                mySummonCount++;
            else
                opSummonCount++;
            Core.GetUI<OcgCoreUI>().RefreshBgDetail();


            var se = "SE_LAND_NORMAL";

            card.SetCode(code);
            card.AddStringTail(InterString.Get("通常召唤登场"));
            card.AnimationPositon();
            ES_hint = InterString.Get("「[?]」通常召唤宣言时", card.GetData().Name);

            if (card.GetData().Level > 6)
            {
                var effect = ABLoader.LoadMasterDuelGameObject("fxp_somldg_Advance_S2_001");
                effect.transform.position = GameCard.GetCardPosition(gps);
                UnityEngine.Object.Destroy(effect, 5f);

                var effect2 = ABLoader.LoadMasterDuelGameObject("fxp_somldg_Advance_S2imp_001");
                effect2.transform.position = effect.transform.position;
                if (card.p.InPosition(CardPosition.Defence))
                    effect2.transform.eulerAngles = effectDefenceAngle;

                se = "SE_LAND_ADVANCE_HIGH";
                CameraManager.ShakeCamera(true);
            }
            else if (card.GetData().Level > 4)
            {
                var effect = ABLoader.LoadMasterDuelGameObject("fxp_somldg_Advance_S1_001");
                effect.transform.localPosition = GameCard.GetCardPosition(gps);
                UnityEngine.Object.Destroy(effect, 10);

                var effect2 = ABLoader.LoadMasterDuelGameObject("fxp_somldg_Advance_S1imp_001");
                effect2.transform.position = effect.transform.position;
                if (card.p.InPosition(CardPosition.Defence))
                    effect2.transform.eulerAngles = effectDefenceAngle;

                se = "SE_LAND_ADVANCE_MIDDLE";
                CameraManager.ShakeCamera();
            }
            else
            {
                var effect = ABLoader.LoadMasterDuelGameObject("fxp_somldg_Hand_001");
                effect.transform.localPosition = GameCard.GetCardPosition(gps);
                if (gps.InPosition(CardPosition.Attack))
                    UnityEngine.Object.Destroy(effect.transform.GetChild(1).gameObject);
                else
                    UnityEngine.Object.Destroy(effect.transform.GetChild(0).gameObject);
                UnityEngine.Object.Destroy(effect, 5f);
            }

            if(Core.GetAutoInfo())
                Core.GetUI<OcgCoreUI>().CardDescription.Show(card, card.GetMaterial());

            AudioManager.PlaySE(se);

            int shakeLevel = 0;
            if (CutinViewer.CutinExist(card.GetData().Id))
                shakeLevel = 1;
            if (card.GetData().Level > 4)
                shakeLevel = 1;
            if (card.GetData().Level > 6)
                shakeLevel = 2;

            foreach (var c in cards)
                c.AnimationLandShake(card, shakeLevel);
            materialCards.Clear();

            await UniTask.WaitForSeconds(1f);
        }

        protected override UniTask GameMessage_Summoned(BinaryReader reader)
        {
            ES_hint = StringHelper.GetUnsafe(1604);//怪兽召唤成功

            return UniTask.CompletedTask;
        }

        protected override async UniTask GameMessage_SpSummoning(BinaryReader reader)
        {
            cardsInSelection.Clear();

            var code = reader.ReadInt32();
            var gps = reader.ReadGPS();
            var card = Core.GCS_Get(gps);
            if(card == null)
            {
                DebugNoCard();
                return;
            }

            if(gps.InMyControl())
            {
                mySpSummonCount++;
                UltraProgress.SpecialSummon();
            }
            else
                opSpSummonCount++;
            Core.GetUI<OcgCoreUI>().RefreshBgDetail();

            if (card.GetData().HasType(CardType.Token))
                goto TokenPass;

            var eff = string.Empty;
            var imp = string.Empty;
            var se = "SE_LAND_NORMAL";

            card.SetCode(code);
            card.AnimationPositon();
            ES_hint = InterString.Get("「[?]」特殊召唤宣言时", card.GetData().Name);

            var isExtraDeckMonster = card.GetData().HasType(CardType.Fusion)
                || card.GetData().HasType(CardType.Synchro)
                || card.GetData().HasType(CardType.Xyz)
                || card.GetData().HasType(CardType.Link);
            if (isExtraDeckMonster)
                duelBGManager.OnSpecialSummonFromExtra(gps.InMyControl() ? 0 : 1);

            if(materialCards.Count > 0)
            {
                if (card.GetData().HasType(CardType.Link))
                {
                    eff = "fxp_somldg_Link_S1_001";
                    imp = "fxp_somldg_Link_S1imp_001";
                    se = "SE_LAND_LINK_MIDDLE";
                }
                else if (card.GetData().HasType(CardType.Fusion))
                {
                    eff = "fxp_somldg_Fusion_S1_001";
                    imp = "fxp_somldg_Fusion_S1imp_001";
                    se = "SE_LAND_FUSION_MIDDLE";
                }
                else if (card.GetData().HasType(CardType.Synchro))
                {
                    eff = "fxp_somldg_Synchro_S1_001";
                    imp = "fxp_somldg_Synchro_S1imp_001";
                    se = "SE_LAND_SYNCHRO_MIDDLE";
                }
                else if (card.GetData().HasType(CardType.Xyz))
                {
                    eff = "fxp_somldg_Xyz_S1_001";
                    imp = "fxp_somldg_Xyz_S1imp_001";
                    se = "SE_LAND_XYZ_MIDDLE";
                }
                else if (card.GetData().HasType(CardType.Ritual))
                {
                    eff = "fxp_somldg_Ritual_S1_001";
                    imp = "fxp_somldg_Ritual_S1imp_001";
                    se = "SE_LAND_RITUAL_MIDDLE";
                }
            }
            else if (inPendulumSummon)
            {
                eff = "fxp_somldg_Pendulum_S1_001";
                imp = "fxp_somldg_Pendulum_S1imp_001";
                se = "SE_LAND_PENDULUM_MIDDLE";
            }
            else
            {
                eff = "fxp_somldg_Special_S1_001";
                imp = "fxp_somldg_Special_S1imp_001";
                se = "SE_LAND_NORMAL_MIDDLE";
            }

            if (card.GetData().IsHighLevel())
            {
                eff = eff.Replace("S1", "S2");
                imp = imp.Replace("S1", "S2");
                se = se.Replace("MIDDLE", "HIGH");
            }

            CameraManager.Overlay3DReset();

            var effect = ABLoader.LoadMasterDuelGameObject(eff);
            effect.transform.position = GameCard.GetCardPosition(gps);
            UnityEngine.Object.Destroy(effect, 5f);

            var impact = ABLoader.LoadMasterDuelGameObject(imp);
            impact.transform.position = effect.transform.position;
            UnityEngine.Object.Destroy(impact, 5f);

            if (gps.InPosition(CardPosition.Defence))
            {
                effect.transform.eulerAngles = effectDefenceAngle;
                impact.transform.eulerAngles = effectDefenceAngle;
            }

            AudioManager.PlaySE(se);
            if (se.EndsWith("HIGH"))
                CameraManager.ShakeCamera(true);
            else
                CameraManager.ShakeCamera();
            if (Core.GetAutoInfo())
                Core.GetUI<OcgCoreUI>().CardDescription.Show(card, card.GetMaterial());

            int shakeLevel = 0;
            if(CutinViewer.CutinExist(card.GetData().Id))
                shakeLevel = 1;
            if (card.GetData().IsHighLevel())
                shakeLevel = 2;
            foreach(var c in cards)
                c.AnimationLandShake(card, shakeLevel);

            TokenPass:
            if (card.GetData().HasType(CardType.Token))
                await UniTask.WaitForSeconds(0.2f);
            else
                await UniTask.WaitForSeconds(1f);

            materialCards.Clear();
        }

        protected override UniTask GameMessage_SpSummoned(BinaryReader reader)
        {
            ES_hint = StringHelper.GetUnsafe(1606);//怪兽特殊召唤成功

            return UniTask.CompletedTask;
        }

        protected override async UniTask GameMessage_FlipSummoning(BinaryReader reader)
        {
            cardsInSelection.Clear();

            var code = reader.ReadInt32();
            var gps = reader.ReadShortGPS();
            var card = Core.GCS_Get(gps);
            if(card == null)
            {
                DebugNoCard();
                return;
            }

            card.SetCode(code);
            gps.position = (int)CardPosition.FaceUpAttack;
            card.RefreshData();
            ES_hint = InterString.Get("「[?]」反转召唤宣言时", card.GetData().Name);
            if (Core.GetAutoInfo())
                Core.GetUI<OcgCoreUI>().CardDescription.Show(card, card.GetMaterial());
            materialCards.Clear();

            await card.MoveAsync(gps);
            card.AnimationPositon();
        }

        protected override UniTask GameMessage_FlipSummoned(BinaryReader reader)
        {
            ES_hint = StringHelper.GetUnsafe(1608);//怪兽反转召唤成功

            return UniTask.CompletedTask;
        }

        protected override async UniTask GameMessage_Chaining(BinaryReader reader)
        {
            var code = reader.ReadInt32();
            var gps = reader.ReadGPS();
            var card = Core.GCS_Get(gps);
            if(card == null)
            {
                DebugNoCard(); 
                return;
            }

            card.SetCode(code);
            if(gps.InMyControl() && card.GetData().HasType(CardType.Spell))UltraProgress.Spell();
            cardsInChain.Add(card);
            codesInChain.Add(code);
            controllerInChain.Add(gps.controller);
            ES_hint = InterString.Get("「[?]」被发动时", card.GetData().Name);
            if (gps.InMyControl())
            {
                if (!myActivated.Contains(code))
                    myActivated.Add(code);
            }
            else
            {
                if (!opActivated.Contains(code))
                    opActivated.Add(code);
            }
            if(Core.GetAutoInfo())
                Core.GetUI<OcgCoreUI>().CardDescription.Show(card, null);
            await card.AnimationActivate().WaitAsync();
        }

        protected override async UniTask GameMessage_Chained(BinaryReader reader)
        {
            var currentChainCard = cardsInChain[^1];
            currentChainCard.AddChain(cardsInChain.Count);
            await duelBGManager.ShowChainStack();
        }

        protected override async UniTask GameMessage_ChainSolving(BinaryReader reader)
        {
            chainSolvingIndex = reader.ReadByte();
            if (chainSolvingIndex > cardsInChain.Count)
            {
                LogDebug("[Duel]: Chain index overflow.");
                return;
            }
            chainSolvingCard = cardsInChain[chainSolvingIndex - 1];
            await duelBGManager.ShowChainResolve();
            await duelBGManager.ShowCardEffectAnimation();
        }

        protected override UniTask GameMessage_ChainSolved(BinaryReader reader)
        {
            materialCards.Clear();

            var id = reader.ReadByte();
            if(id > cardsInChain.Count)
            {
                LogDebug("[Duel]: Chain index overflow.");
                return UniTask.CompletedTask;
            }
            var card = cardsInChain[id - 1];
            card.RemoveChain(id);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_ChainEnd(BinaryReader reader)
        {
            foreach (var c in cardsInChain)
            {
                c.negated = false;
                c.disabledInChain = false;
                c.RemoveAllChain();
                c.effectTargets.Clear();
            }
            cardsBeTarget.Clear();
            cardsInChain.Clear();
            codesInChain.Clear();
            controllerInChain.Clear();
            negatedInChain.Clear();
            materialCards.Clear();
            chainSolvingCard = null;
            foreach (var c in tempCards)
                c.Dispose();
            tempCards.Clear();

            return UniTask.CompletedTask;
        }

        protected override async UniTask GameMessage_ChainNegated(BinaryReader reader)
        {
            var id = reader.ReadByte();
            if (id > cardsInChain.Count)
            {
                LogDebug("[Duel]: Chain index overflow.");
                return;
            }
            negatedInChain.Add(id);
            var card = cardsInChain[id - 1];
            card.negated = true;
            await card.AnimationNegate().WaitAsync();
        }

        protected override async UniTask GameMessage_ChainDisabled(BinaryReader reader)
        {
            var id = reader.ReadByte();
            if (id > cardsInChain.Count)
            {
                LogDebug("[Duel]: Chain index overflow.");
                return;
            }

            var card = cardsInChain[id - 1];
            if (!card.disabledInChain)
            {
                card.disabledInChain = true;
                await card.AnimationNegate().WaitAsync();
            }
        }

        protected override async UniTask GameMessage_Attack(BinaryReader reader)
        {
            var from = reader.ReadGPS();
            var to = reader.ReadGPS();
            var attackCard = Core.GCS_Get(from);
            if(attackCard == null)
            {
                DebugNoCard();
                return;
            }

            attackingCard = attackCard;
            ES_hint = InterString.Get("「[?]」攻击时", attackCard.GetData().Name);
            Vector3 endPosition;
            var attackedCard = Core.GCS_Get(to);
            var finalBlow = false;

            if (attackedCard != null)
            {
                endPosition = attackedCard.model.transform.position;
                if (attackedCard.p.InPosition(CardPosition.Attack))
                {
                    var diff = attackCard.GetData().Attack - attackedCard.GetData().Attack;
                    if (attackCard.p.InMyControl())
                    {
                        if(diff >= life1)
                            finalBlow = true;
                    }
                    else
                    {
                        if (diff >= life0)
                            finalBlow = true;
                    }
                }
            }
            else
            {
                if (attackCard.p.InMyControl())
                {
                    endPosition = opPosition;
                    if (attackCard.GetData().Attack >= life1)
                        finalBlow = true;
                }
                else
                {
                    endPosition = myPosition;
                    if (attackCard.GetData().Attack >= life0)
                        finalBlow = true;
                }
                var effect = ABLoader.LoadMasterDuelGameObject("DuelDirectAtk00");
                UnityEngine.Object.Destroy(effect, 1f);
                AudioManager.PlaySE("SE_DA_TEXT");
            }

            duelBGManager.ShowAttackLine(attackCard.model.transform.position, endPosition);
            if (finalBlow)
                duelBGManager.ShowDuelFinalBlowText();
            await UniTask.WaitForSeconds(0.2f);
        }

        protected override UniTask GameMessage_AttackDisabled(BinaryReader reader)
        {
            ES_hint = InterString.Get("攻击被无效时");
            duelBGManager.HideAttackLine();
            duelBGManager.HideDuelFinalBlowText();

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_DamageStepStart(BinaryReader reader)
        {
            PhaseButtonHandler.SetTextBelow("03");
            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_DamageStepEnd(BinaryReader reader)
        {
            PhaseButtonHandler.SetTextBelow("04");
            return UniTask.CompletedTask;
        }

        protected override async UniTask GameMessage_Battle(BinaryReader reader)
        {
            var from = reader.ReadShortGPS();
            reader.ReadByte();
            var attackCard = Core.GCS_Get(from);

            if(attackCard != null)
            {
                var data = attackCard.GetData();
                data.Attack = reader.ReadInt32();
                data.Defense = reader.ReadInt32();
                attackCard.SetData(data);
            }
            else
            {
                reader.ReadInt32();
                reader.ReadInt32();
            }

            reader.ReadByte();
            var to = reader.ReadShortGPS();
            reader.ReadByte();
            var  attackedCard = Core.GCS_Get(to);
            if (attackedCard != null && to.location != 0)
            {
                var data = attackedCard.GetData();
                data.Attack = reader.ReadInt32();
                data.Defense = reader.ReadInt32();
                attackedCard.SetData(data);
            }
            else
            {
                reader.ReadInt32();
                reader.ReadInt32();
            }
            reader.ReadByte();

            var attackTransform = attackCard.manager.GetElement<Transform>("CardPlane");
            var attackPosition = attackTransform.position;
            var attackAngle = attackTransform.eulerAngles;

            Vector3 attackedPosition;
            int directAttack = 0;

            if(attackedCard == null || to.location == 0)
            {
                if (from.InMyControl())
                {
                    attackedPosition = opPosition;
                    directAttack = 1;
                }
                else
                {
                    attackedPosition = myPosition;
                    directAttack = -1;
                }
            }
            else
                attackedPosition = attackedCard.model.transform.position;

            if (directAttack != 0)
                duelBGManager.OnDirectAttack(from.InMyControl() ? 0 : 1);

            var isFinalAttack = duelBGManager.IsFinalBlow();
            duelBGManager.HideAttackLine();
            duelBGManager.HideDuelFinalBlowText();

            if (isFinalAttack && (await duelBGManager.NeedSpecialFinalAttackAsync(attackCard, attackedPosition)))
                return;

            string hit;
            string tail;
            string sound1;
            string sound2;

            if (attackCard.GetData().IsAttribute(CardAttribute.Dark))
            {
                tail = "fxp_atkdak_S2_001";
                hit = "fxp_hitdak_S2_001";
                sound1 = "SE_ATTACK_A_DARK_SPECIAL_01";
                sound2 = "SE_ATTACK_A_DARK_SPECIAL_02";
            }
            else if (attackCard.GetData().IsAttribute(CardAttribute.Earth))
            {
                tail = "fxp_atkeah_S2_001";
                hit = "fxp_hiteah_S2_001";
                sound1 = "SE_ATTACK_A_EARTH_SPECIAL_01";
                sound2 = "SE_ATTACK_A_EARTH_SPECIAL_02";
            }
            else if (attackCard.GetData().IsAttribute(CardAttribute.Fire))
            {
                tail = "fxp_atkfie_S2_001";
                hit = "fxp_hitfie_S2_001";
                sound1 = "SE_ATTACK_A_FIRE_SPECIAL_01";
                sound2 = "SE_ATTACK_A_FIRE_SPECIAL_02";
            }
            else if (attackCard.GetData().IsAttribute(CardAttribute.Light))
            {
                tail = "fxp_atklit_S2_001";
                hit = "fxp_hitlit_S2_001";
                sound1 = "SE_ATTACK_A_LIGHT_SPECIAL_01";
                sound2 = "SE_ATTACK_A_LIGHT_SPECIAL_02";
            }
            else if (attackCard.GetData().IsAttribute(CardAttribute.Water))
            {
                tail = "fxp_atkwtr_S2_001";
                hit = "fxp_hitwtr_S2_001";
                sound1 = "SE_ATTACK_A_WATER_SPECIAL_01";
                sound2 = "SE_ATTACK_A_WATER_SPECIAL_02";
            }
            else if (attackCard.GetData().IsAttribute(CardAttribute.Wind))
            {
                tail = "fxp_atkwid_S2_001";
                hit = "fxp_hitwid_S2_001";
                sound1 = "SE_ATTACK_A_WIND_SPECIAL_01";
                sound2 = "SE_ATTACK_A_WIND_SPECIAL_02";
            }
            else
            {
                tail = "fxp_atkdve_S2_001";
                hit = "fxp_hitdve_S2_001";
                sound1 = "SE_ATTACK_A_DIVINE_SPECIAL_01";
                sound2 = "SE_ATTACK_A_DIVINE_SPECIAL_02";
            }

            if(attackCard.GetData().Attack < 2000)
            {
                tail = tail.Replace("S2", "S1");
                hit = hit.Replace("S2", "S1");
                sound1 = sound1.Replace("SPECIAL", "DEFAULT");
                sound2 = sound2.Replace("SPECIAL", "DEFAULT");
            }

            attackTransform.LookAt(attackedPosition);

            if (directAttack == 0)
            {
                if(attackedCard.p.InPosition(CardPosition.Defence))
                    if(attackedCard.GetData().Defense >= attackCard.GetData().Attack)
                    {
                        hit = "fxp_hit_guard_001";
                        sound2 = "SE_ATTACK_GUARD";
                    }
                if (attackedCard.p.InPosition(CardPosition.Attack))
                    if (attackedCard.GetData().Attack >= attackCard.GetData().Attack)
                    {
                        hit = "fxp_hit_guard_001";
                        sound2 = "SE_ATTACK_GUARD";
                    }
            }
            else
            {
                if(directAttack == 1)
                {
                    hit = "fxp_dithit_far_001";
                    sound2 = "SE_DIRECT_ATTACK_RIVAL";
                }
                else
                {
                    hit = "fxp_dithit_near_001";
                    sound2 = "SE_DIRECT_ATTACK_PLAYER";
                }
            }

            var tailObj = ABLoader.LoadMasterDuelGameObject(tail);
            tailObj.transform.SetParent(attackTransform, false);
            tailObj.SetActive(false);

            Vector3 v = attackedPosition - attackPosition;
            v.y = 0;
            Vector3 faceAngle = attackTransform.eulerAngles;
            faceAngle.x = 0;
            attackTransform.eulerAngles = attackAngle;

            Sequence sequence = DOTween.Sequence();
            if (attackCard.GetData().Attack < 2000)
            {
                faceAngle.z = faceAngle.y >= 0 && faceAngle.y < 180 ? -20f : 20f;
                sequence.Append(attackTransform.DOMove(attackPosition + new Vector3(0f, 10f, 0f) - v * 0.3f, 0.3f).SetEase(Ease.InOutCubic).OnComplete(() =>
                {
                    tailObj.SetActive(true);
                    foreach (Transform t in tailObj.GetComponentsInChildren<Transform>(true))
                        t.gameObject.SetActive(true);
                }));
                sequence.Join(attackTransform.DORotate(faceAngle, 0.3f).SetEase(Ease.InOutCubic));
                sequence.Append(attackTransform.DOMove(attackPosition + (attackedPosition - attackPosition) * 0.8f + new Vector3(0f, 0f, 0f), 0.1f).SetEase(Ease.InSine));
                faceAngle.z = 0;
                sequence.Join(attackTransform.DORotate(faceAngle, 0.1f).SetEase(Ease.InSine));
                sequence.Join(Program.instance.camera_.cameraMain.transform.DOMove(new Vector3(0, 95, -37 + directAttack * 5), 0.1f));
                sequence.AppendCallback(() =>
                {
                    CameraManager.ShakeCamera();
                    if (NextMessageIs(GameMessage.Damage))
                        NoMoreWait = true;
                    var hitObj = ABLoader.LoadMasterDuelGameObject(hit);
                    attackedPosition.y += 5;
                    hitObj.transform.position = attackedPosition;
                    UnityEngine.Object.Destroy(hitObj, 5f);
                    AudioManager.PlaySE(sound2);
                });
                sequence.AppendInterval(0.3f);
                sequence.Append(attackTransform.DOMove(attackPosition, 0.3f).SetEase(Ease.InQuad));
                sequence.Join(Program.instance.camera_.cameraMain.transform.DOMove(new Vector3(0, 95, -37), 0.3f));
                sequence.Join(attackTransform.DORotate(attackAngle, 0.3f).SetEase(Ease.InQuad));
            }
            else
            {
                faceAngle.z = faceAngle.y >= 0 && faceAngle.y < 180 ? -30f : 30f;
                sequence.Append(attackTransform.DOMove(attackPosition + new Vector3(0f, 10f, 0f) - v * 0.4f, 0.5f).SetEase(Ease.InOutCubic));
                sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(45f, 0f, 0f), 0.5f).SetEase(Ease.InOutCubic));
                sequence.InsertCallback(0.4f, () =>
                {
                    tailObj.SetActive(true);
                    foreach (Transform t in tailObj.GetComponentsInChildren<Transform>(true))
                        t.gameObject.SetActive(true);
                });

                sequence.Append(attackTransform.DOMove(attackPosition + (attackedPosition - attackPosition) * 0.8f + new Vector3(0f, 0f, 0f), 0.15f).SetEase(Ease.InSine));
                faceAngle.z = 0;
                sequence.Join(attackTransform.DORotate(faceAngle, 0.15f));
                sequence.Join(Program.instance.camera_.cameraMain.transform.DOMove(new Vector3(0, 95, -37 + directAttack * 5), 0.15f));
                sequence.AppendCallback(() =>
                {
                    CameraManager.ShakeCamera(true);
                    if (NextMessageIs(GameMessage.Damage))
                        NoMoreWait = true;
                    var hitObj = ABLoader.LoadMasterDuelGameObject(hit);
                    attackedPosition.y += 5;
                    hitObj.transform.position = attackedPosition;
                    UnityEngine.Object.Destroy(hitObj, 5f);
                    AudioManager.PlaySE(sound2);
                });
                sequence.AppendInterval(0.3f);
                sequence.Append(attackTransform.DOMove(attackPosition, 0.3f).SetEase(Ease.InQuad));
                sequence.Join(Program.instance.camera_.cameraMain.transform.DOMove(new Vector3(0, 95, -37), 0.3f));
                sequence.Join(attackTransform.DORotate(attackAngle, 0.3f).SetEase(Ease.InQuad));
            }

            AudioManager.PlaySE(sound1);
            UnityEngine.Object.Destroy(tailObj, 3f);
            await sequence.WaitAsync();
        }

        protected override async UniTask GameMessage_Damage(BinaryReader reader)
        {
            var enginePlayer = reader.ReadByte();
            var player = LocalPlayer(enginePlayer);
            var value = reader.ReadInt32();

            if(currentMessage==GameMessage.Damage)UltraProgress.Damage(player,value);
            if(player == 0)
            {
                life0 -= value;
                if(currentMessage == GameMessage.Damage)
                    ES_hint = InterString.Get("我方受到伤害时");
            }
            else
            {
                life1 -= value;
                if (currentMessage == GameMessage.Damage)
                    ES_hint = InterString.Get("对方受到伤害时");
            }

            if(life0 <= 0 || life1 <= 0)
                duelBGManager.FinishDamageEffect();

            if (currentMessage == GameMessage.Damage)
                duelBGManager.OnPlayerDamaged(player, Mathf.Max(value, 0));

            duelBGManager.UpdateBgEffects(player);
            duelBGManager.OnLifePointsChanged(0, life0);
            duelBGManager.OnLifePointsChanged(1, life1);
            AudioManager.PlaySE("SE_COST_DAMAGE");
            Core.SetLP(player, -value);
            string owner = player == 0 ? "You" : "Opponent";
            MessageManager.Cast(currentMessage == GameMessage.PayLpCost
                ? owner + " paid " + value + " LP (card cost)."
                : owner + " took " + value + " damage.");
            Debug.Log($"DUEL_LP {currentMessage} enginePlayer={enginePlayer} localPlayer={player} amount={value} you={life0} opponent={life1}");
            await UniTask.WaitForSeconds(0.5f);
        }

        protected override async UniTask GameMessage_PayLpCost(BinaryReader reader)
        {
            await GameMessage_Damage(reader);
        }

        protected override async UniTask GameMessage_Recover(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var value = reader.ReadInt32();

            if(player == 0)
            {
                life0 += value;
                ES_hint = InterString.Get("我方生命值回复时");
            }
            else
            {
                life1 += value;
                ES_hint = InterString.Get("对方生命值回复时");
            }

            duelBGManager.OnLifePointsChanged(0, life0);
            duelBGManager.OnLifePointsChanged(1, life1);
            Core.SetLP(player, value);
            await UniTask.WaitForSeconds(0.5f);
        }

        protected override async UniTask GameMessage_LpUpdate(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var value = reader.ReadInt32();

            int diff;
            if (player == 0)
            {
                diff = value - life0;
                life0 = value;
            }
            else
            {
                diff = value - life1;
                life1 = value;
            }

            if (life0 <= 0 || life1 <= 0)
                duelBGManager.FinishDamageEffect();

            duelBGManager.UpdateBgEffects(player);
            if (diff < 0)
                duelBGManager.OnPlayerDamaged(player, -diff);
            duelBGManager.OnLifePointsChanged(0, life0);
            duelBGManager.OnLifePointsChanged(1, life1);
            if(diff < 0)
                AudioManager.PlaySE("SE_COST_DAMAGE");
            Core.SetLP(player, diff);
            await UniTask.WaitForSeconds(0.5f);
        }

        protected override async UniTask GameMessage_TossCoin(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var count = reader.ReadByte();
            bool config = true;
            if(condition == Condition.Duel
                && !Config.GetBool("DuelCoin", true))
                config = false;
            if (condition == Condition.Watch
                && !Config.GetBool("DuelWatch", true))
                config = false;
            if (condition == Condition.Replay
                && !Config.GetBool("DuelReplay", true))
                config = false;

            if (config)
            {
                AudioManager.PlaySE("SE_COIN_THROW");
                var random = UnityEngine.Random.Range(1, 3);

                for (int i = 0; i < count; i++)
                {
                    var coin = ABLoader.LoadMasterDuelGameObject("DuelCoinToss118000" + random);
                    var manager = coin.GetComponent<ElementObjectManager>();
                    UnityEngine.Object.Destroy(coin, 3f);
                    var x = -(count - 1) * 8 + i * 16;
                    coin.transform.position = new Vector3(x, 0, 0);
                    GameObject targetCoin;
                    if (player == 0)
                        targetCoin = manager.GetElement("Blue");
                    else
                        targetCoin = manager.GetElement("Red");
                    targetCoin.SetActive(true);

                    var data = reader.ReadByte();
                    if (data == 0)
                    {
                        DOTween.To(v => { }, 0, 0, 2f).OnComplete(() =>
                        {
                            AudioManager.PlaySE("SE_COIN_DECIDE_02");
                        });
                        var quence = DOTween.Sequence();
                        quence.AppendInterval(1.2f);
                        quence.Append(targetCoin.transform.DOLocalRotate(new Vector3(0, 180, 0), 0f));
                    }
                    else
                    {
                        DOTween.To(v => { }, 0, 0, 2f).OnComplete(() =>
                        {
                            AudioManager.PlaySE("SE_COIN_DECIDE");
                        });
                    }
                }
                await UniTask.WaitForSeconds(3f);
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var data = reader.ReadByte();
                    if (data == 1)
                        MessageManager.Cast(InterString.Get("硬币正面"));
                    else
                        MessageManager.Cast(InterString.Get("硬币反面"));
                }
            }
        }

        protected override async UniTask GameMessage_TossDice(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var count = reader.ReadByte();
            bool config = true;
            if (condition == Condition.Duel
                && !Config.GetBool("DuelDice", true))
                config = false;
            if (condition == Condition.Watch
                && !Config.GetBool("DuelDice", true))
                config = false;
            if (condition == Condition.Replay
                && !Config.GetBool("DuelDice", true))
                config = false;

            if (config)
            {
                AudioManager.PlaySE("SE_DICE_ROLL");
                DOTween.To(v => { }, 0, 0, 0.6f).OnComplete(() =>
                {
                    AudioManager.PlaySE("SE_DICE_DECIDE");
                });

                for (var i = 0; i < count; i++)
                {
                    var dice = ABLoader.LoadMasterDuelGameObject(player == 0 ? "DuelDice" : "DuelDiceEn");
                    UnityEngine.Object.Destroy(dice, 2f);
                    var diceNumber = dice.GetComponent<ElementObjectManager>().
                        GetElement<Transform>("DiceNumber");
                    var data = reader.ReadByte();
                    switch (data)
                    {
                        case 1:
                            diceNumber.localEulerAngles = Vector3.zero;
                            break;
                        case 2:
                            diceNumber.localEulerAngles = new Vector3(270, 0, 0);
                            break;
                        case 3:
                            diceNumber.localEulerAngles = new Vector3(0, 0, 270);
                            break;
                        case 4:
                            diceNumber.localEulerAngles = new Vector3(0, 0, 90);
                            break;
                        case 5:
                            diceNumber.localEulerAngles = new Vector3(90, 0, 0);
                            break;
                        case 6:
                            diceNumber.localEulerAngles = new Vector3(180, 90, 0);
                            break;
                    }
                    var x = -(count - 1) * 5 + i * 10;
                    dice.transform.position = new Vector3(x, 0, 0);
                }
                await UniTask.WaitForSeconds(2f);
            }
            else
            {
                for(var i = 0; i < count; i++)
                {
                    var data = reader.ReadByte();
                    MessageManager.Cast(InterString.Get("骰子结果：[?]", data.ToString()));
                }
            }
        }

        protected override async UniTask GameMessage_Draw(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            ES_hint = player == 0 ? InterString.Get("我方抽卡时") : InterString.Get("对方抽卡时");
            var count = reader.ReadByte();
            var deckCount = Core.GetLocationCardCount(CardLocation.Deck, (uint)player);
            var handCount = Core.GetLocationCardCount(CardLocation.Hand, (uint)player);

            var preHands = new List<GameCard>();
            for(var i = 0; i < count; i++)
            {
                var card = Core.GCS_Get(
                    new GPS
                    {
                        controller = (uint)player,
                        location = (uint)CardLocation.Deck,
                        sequence = (uint)(deckCount - 1 - i),
                    });
                card.SetCode(reader.ReadInt32() & 0x7fffffff);
                preHands.Add(card);
            }
            if(player == 0)
            {
                needRefreshMyHand = true;
                myPreHandCards = preHands;
            }
            else
            {
                needRefreshOpHand = true;
                opPreHandCards = preHands;
            }

            for(var i = 0; i < preHands.Count; i++)
            {
                _ = preHands[i].MoveAsync(
                    new GPS
                    {
                        controller = (uint)player,
                        location = (uint)CardLocation.Hand,
                        sequence = (uint)(handCount + i),
                    });
            }
            await UniTask.WaitForSeconds(player == 0 ? 0.5f : 0.3f);
        }

        protected override async UniTask GameMessage_TagSwap(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            if (player == 0)
            {
                if (Core.GetUI<OcgCoreUI>().TextPlayer0Name.text == name_0)
                    Core.GetUI<OcgCoreUI>().TextPlayer0Name.text = name_0_tag;
                else
                    Core.GetUI<OcgCoreUI>().TextPlayer0Name.text = name_0;
            }
            else
            {
                if (Core.GetUI<OcgCoreUI>().TextPlayer1Name.text == name_1)
                    Core.GetUI<OcgCoreUI>().TextPlayer1Name.text = name_1_tag;
                else
                    Core.GetUI<OcgCoreUI>().TextPlayer1Name.text = name_1;
            }
            Core.SetFace();

            int mainCount = reader.ReadByte();
            int extraCount = reader.ReadByte();
            int pendulumCount = reader.ReadByte();
            int handsCount = reader.ReadByte();
            var cardsInDeck = Core.GCS_ResizeBundle(mainCount, player, CardLocation.Deck);
            var cardsInExtra = Core.GCS_ResizeBundle(extraCount, player, CardLocation.Extra);
            var cardsInHand = Core.GCS_ResizeBundle(handsCount, player, CardLocation.Hand);
            if (cardsInDeck.Count > 0)
                cardsInDeck[^1].SetCode(reader.ReadInt32());
            for (int i = 0; i < cardsInHand.Count; i++)
                cardsInHand[i].SetCode(reader.ReadInt32());
            for (int i = 0; i < cardsInExtra.Count; i++)
                cardsInExtra[i].SetCode(reader.ReadInt32() & 0x7FFFFFFF);
            for (int i = 0; i < pendulumCount; i++)
                if (cardsInExtra.Count - 1 - i > 0)
                    cardsInExtra[cardsInExtra.Count - 1 - i].p.position = (int)CardPosition.FaceUpAttack;
            Core.ArrangeCards();
            needRefreshMyHand = true;
            needRefreshOpHand = true;
            duelBGManager.RefreshBgState();
            foreach (var c in cardsInHand)
            {
                c.AnimationShuffle(0.15f);
                c.EraseData();
            }
            await UniTask.WaitForSeconds(0.14f);
            await PreloadUpdateCardMessages();
            await UniTask.WaitForSeconds(0.16f);
        }

        protected override UniTask GameMessage_MatchKill(BinaryReader reader)
        {
            cookie_matchKill = reader.ReadInt32();
            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_PlayerHint(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            int pType = reader.ReadByte();
            var pValue = reader.ReadInt32();
            var valString = StringHelper.Get(pValue);
            if (pValue == 38723936)
            {
                valString = InterString.Get("不能确认墓地中的卡");
                if (player == 0)
                {
                    if (pType == 6)
                    {
                        cantCheckGrave = true;
                        Core.GetUI<OcgCoreUI>().CardList.Hide();
                    }
                    if (pType == 7)
                        cantCheckGrave = false;
                }
            }
            if (pType == 6)
            {
                if (player == 0)
                    PrintDuelLog(InterString.Get("我方状态：[?]", valString));
                else
                    PrintDuelLog(InterString.Get("对方状态：[?]", valString));
            }
            else if (pType == 7)
            {
                if (player == 0)
                    PrintDuelLog(InterString.Get("我方状态结束：[?]", valString));
                else
                    PrintDuelLog(InterString.Get("对方状态结束：[?]", valString));
            }

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_CardHint(BinaryReader reader)
        {
            var card = Core.GCS_Get(reader.ReadGPS());
            int cType = reader.ReadByte();
            var value = reader.ReadInt32();

            if (card == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }

            switch (cType)
            {
                case 1:
                    card.RemoveStringTail(InterString.Get("数字记录："));
                    card.AddStringTail(InterString.Get("数字记录：") + value);
                    break;
                case 2:
                    card.RemoveStringTail(InterString.Get("卡片记录："));
                    card.AddStringTail(InterString.Get("卡片记录：") + CardsManager.Get(value).Name);
                    break;
                case 3:
                    card.RemoveStringTail(InterString.Get("种族记录："));
                    card.AddStringTail(InterString.Get("种族记录：") + StringHelper.Race(value));
                    break;
                case 4:
                    card.RemoveStringTail(InterString.Get("属性记录："));
                    card.AddStringTail(InterString.Get("属性记录：") + StringHelper.Attribute(value));
                    break;
                case 5:
                    card.RemoveStringTail(InterString.Get("数字记录："));
                    card.AddStringTail(InterString.Get("数字记录：") + value);
                    break;
                case 6:
                    card.AddStringTail(StringHelper.Get(value));
                    break;
                case 7:
                    card.RemoveStringTail(StringHelper.Get(value));
                    break;
            }

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Hint(BinaryReader reader)
        {
            Es_selectMSGHintType = reader.ReadChar();
            Es_selectMSGHintPlayer = LocalPlayer(reader.ReadChar());
            Es_selectMSGHintData = reader.ReadInt32();
            var type = Es_selectMSGHintType;
            var player = Es_selectMSGHintPlayer;
            var data = Es_selectMSGHintData;
            if (type == 1)
                ES_hint = StringHelper.Get(data);
            else if (type == 2)
                PrintDuelLog(StringHelper.Get(data));
            else if (type == 3)
                ES_selectHint = StringHelper.Get(data);
            else if (type == 4)
                PrintDuelLog(InterString.Get("效果选择：[?]", StringHelper.Get(data)));
            else if (type == 5)
                PrintDuelLog(StringHelper.Get(data));
            else if (type == 6)
                PrintDuelLog(InterString.Get("种族选择：[?]", StringHelper.Race(data)));
            else if (type == 7)
                PrintDuelLog(InterString.Get("属性选择：[?]", StringHelper.Attribute(data)));
            else if (type == 8)
            {
                Program.instance.message_.CastCard(data);
                lastDuelLog = InterString.Get("宣言卡片：[?]", CardsManager.Get(data).Name);
            }
            else if (type == 9)
                PrintDuelLog(InterString.Get("数字选择：[?]", data.ToString()));
            else if (type == 10)
            {
                Program.instance.message_.CastCard(data);
                lastDuelLog = InterString.Get("效果适用：[?]", CardsManager.Get(data).Name);
            }
            else if (type == 11)
            {
                if (player == 1)
                    data = (data >> 16) | (data << 16);
                PrintDuelLog(InterString.Get("区域选择：[?]", StringHelper.Zone(data)));
            }
            ES_selectCardFromFieldFirstFlag = (type == 3 && data == 575);

            return UniTask.CompletedTask;
        }

        protected override async UniTask GameMessage_NewTurn(BinaryReader reader)
        {
            cardsInSelection.Clear();
            myActivated.Clear();
            opActivated.Clear();
            mySummonCount = 0;
            mySpSummonCount = 0;
            opSummonCount = 0;
            opSpSummonCount = 0;
            Core.GetUI<OcgCoreUI>().RefreshBgDetail();
            turns++;
            myTurn = LocalPlayer(reader.ReadByte()) == 0;
            duelBGManager.OnNewTurn(myTurn, turns);

            PhaseButtonHandler.TurnChange(myTurn, turns);
            PhaseButtonHandler.SetTextMain(string.Empty);
            PhaseButtonHandler.SetTextBelow(string.Empty);
            foreach (var c in cards)
                c.ShowDisquiet();
            duelBGManager.ReleaseTurnObjects();
            duelBGManager.SetPlayableGuide(myTurn);

            await duelBGManager.ShowTurnChangeBanner(myTurn ? 0 : 1);
        }

        protected override async UniTask GameMessage_NewPhase(BinaryReader reader)
        {
            duelBGManager.HideAttackLine();
            duelBGManager.HideDuelFinalBlowText();

            cardsInSelection.Clear();

            duelPhase = (DuelPhase)reader.ReadUInt16();
            var player = myTurn ? 0 : 1;
            PhaseButtonHandler.SetTextBelow(string.Empty);

            if (duelPhase == DuelPhase.Draw)
                PhaseButtonHandler.SetTextMain("Draw");
            else if(duelPhase == DuelPhase.Standby)
                PhaseButtonHandler.SetTextMain("Standby");
            else if (duelPhase == DuelPhase.Main1)
                PhaseButtonHandler.SetTextMain("Main1");
            else if (duelPhase == DuelPhase.BattleStart)
            {
                PhaseButtonHandler.SetTextMain("Battle");
                if(myTurn && Core.GetAllAtk(true) >= life1)
                    AudioManager.PlayBgmClimax();
                if (!myTurn && Core.GetAllAtk(false) >= life0)
                    AudioManager.PlayBgmClimax();
            }
            else if (duelPhase == DuelPhase.BattleStep)
                PhaseButtonHandler.SetTextBelow("01");
            else if (duelPhase == DuelPhase.Damage)
                PhaseButtonHandler.SetTextBelow("02");
            else if (duelPhase == DuelPhase.DamageCal)
                PhaseButtonHandler.SetTextBelow("03");
            else if (duelPhase == DuelPhase.Battle)
                PhaseButtonHandler.SetTextBelow(string.Empty);
            else if (duelPhase == DuelPhase.Main2)
                PhaseButtonHandler.SetTextMain("Main2");
            else if (duelPhase == DuelPhase.End)
                PhaseButtonHandler.SetTextMain("End");

            duelBGManager.OnNewPhase(player, duelPhase);
            await duelBGManager.ShowPhaseBanner(player, duelPhase);
        }

        protected override async UniTask GameMessage_ConfirmDecktop(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var count = reader.ReadByte();
            var countOfDeck = Core.GetLocationCardCount(CardLocation.Deck, (uint)player);

            for (int i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                reader.ReadShortGPS();
                var gps = new GPS
                {
                    controller = (uint)player,
                    location = (uint)CardLocation.Deck,
                    sequence = (uint)(countOfDeck - 1 - i)
                };
                var card = Core.GCS_Get(gps);
                if (card == null)
                {
                    DebugNoCard();
                    continue;
                }
                card.SetCode(code);
                card.AnimationConfirmDeckTop(i);
            }
            var camera = Program.instance.camera_.cameraMain.transform;
            var sequence = DOTween.Sequence();
            if (player == 0)
                sequence.Append(camera.DOLocalMove(new Vector3(0, 95, -40), 0.25f));
            else
                sequence.Append(camera.DOLocalMove(new Vector3(0, 95, -31), 0.25f));
            sequence.Join(camera.DOLocalRotate(new Vector3(70, 0, 0), 0.25f));
            sequence.AppendInterval(count);
            sequence.Append(camera.DOLocalMove(new Vector3(0, 95, -37), 0.25f));
            sequence.Join(camera.DOLocalRotate(new Vector3(70, 0, 0), 0.25f));

            await sequence.WaitAsync();
        }

        protected override async UniTask GameMessage_ConfirmCards(BinaryReader reader)
        {
            /*var player = LocalPlayer(*/reader.ReadByte()/*)*/;
            if((reader.BaseStream.Length-reader.BaseStream.Position)%7==2)reader.ReadByte();

            var count = reader.ReadByte();
            var listShow = false;
            if (count > 3 && condition == Condition.Duel)
                listShow = true;
            var confirmCards = new List<GameCard>();

            Sequence sequence = null;
            for (int i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var card = Core.GCS_Get(gps);
                if(card == null)
                {
                    DebugNoCard();
                    continue;
                }
                card.SetCode(code);
                if (listShow)
                    confirmCards.Add(card);
                else
                    sequence = card.AnimationConfirm(i);
            }

            if (listShow)
            {
                Core.GetUI<OcgCoreUI>().ShowPopupSelectCard
                    (InterString.Get("确认卡片：[?]张。", count.ToString()), confirmCards, 0, 0, true, true);
                await UniTask.WaitUntil(() => dispatcher.playerResponed);
            }
            else
            {
                await UniTask.WaitForSeconds((sequence?.Duration() ?? 0f) + 0.1f);
            }
        }

        protected override async UniTask GameMessage_DeckTop(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var countOfDeck = Core.GetLocationCardCount(CardLocation.Deck, (uint)player);
            var gps = new GPS
            {
                controller = (uint)player,
                location = (uint)CardLocation.Deck,
                sequence = (uint)(countOfDeck - 1 - reader.ReadByte())
            };
            var code = reader.ReadInt32();
            var card = Core.GCS_Get(gps);
            if(card == null)
            {
                DebugNoCard();
                return;
            }
            card.SetCode(code);
            PrintDuelLog(InterString.Get("确认卡片：[?]", CardsManager.Get(code).Name));
            await card.AnimationConfirm(0).WaitAsync();
        }

        protected override async UniTask GameMessage_ShuffleDeck(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            if (Core.GetLocationCardCount(CardLocation.Deck, (uint)player) > 0)
            {
                for (var i = 0; i < cards.Count; i++)
                    if (cards[i].gameObject.activeInHierarchy)
                        if ((cards[i].p.location & (uint)CardLocation.Deck) > 0)
                            if (cards[i].p.controller == player)
                                cards[i].EraseData();
                await duelBGManager.PlayShuffleDeckAsync(player);
            }
        }

        protected override async UniTask GameMessage_RefreshDeck(BinaryReader reader)
        {
            await GameMessage_ShuffleDeck(reader);
        }

        protected override async UniTask GameMessage_ShuffleHand(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            for (var i = 0; i < cards.Count; i++)
                if (cards[i].gameObject.activeInHierarchy)
                    if ((cards[i].p.location & (uint)CardLocation.Hand) > 0)
                        if (cards[i].p.controller == player)
                        {
                            cards[i].AnimationShuffle(0.15f);
                            cards[i].EraseData();
                        }
            Program.instance.audio_.PlayShuffleSE();
            await UniTask.WaitForSeconds(0.14f);
            await PreloadUpdateCardMessages();
            await UniTask.WaitForSeconds(0.16f);
        }

        protected override UniTask GameMessage_SwapGraveDeck(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            foreach (var c in cards)
            {
                if (c.p.controller == player)
                {
                    if ((c.p.location & (uint)CardLocation.Deck) > 0)
                        c.p.location = (uint)CardLocation.Grave;
                    else if ((c.p.location & (uint)CardLocation.Grave) > 0)
                    {
                        if (c.GetData().IsExtraCard())
                            c.p.location = (uint)CardLocation.Extra;
                        else
                            c.p.location = (uint)CardLocation.Deck;
                    }
                }
            }
            //TODO
            return UniTask.CompletedTask;
        }

        protected override async UniTask GameMessage_ShuffleSetCard(BinaryReader reader)
        {
            var location = reader.ReadByte();
            var count = reader.ReadByte();
            var gpss = new List<GPS>();
            var cardsToShuffle = new List<GameCard>();
            for (int i = 0; i < count; i++)
            {
                var gps = reader.ReadGPS();
                gpss.Add(gps);
                var card = Core.GCS_Get(gps);
                if(card == null)
                {
                    DebugNoCard();
                    continue;
                }
                card.EraseData();
                cardsToShuffle.Add(card);
            }

            for (int i = 0; i < cardsToShuffle.Count; i++)
            {
                var card = cardsToShuffle[i];
                var newGPS = reader.ReadGPS();
                var oldGPS = card.p;
                oldGPS.reason = 0;
                card.model.transform.DOLocalMove(new Vector3(0f, 5f, card.p.InMyControl() ? -12 : 12), 0.2f).OnComplete(() =>
                {
                    _ = card.MoveAsync(newGPS.location > 0 ? newGPS : oldGPS);
                });
            }

            await UniTask.WaitForSeconds(0.4f);
        }

        protected override UniTask GameMessage_ReverseDeck(BinaryReader reader)
        {
            deckReserved = !deckReserved;
            //TODO
            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_FieldDisabled(BinaryReader reader)
        {
            var disabledFields = reader.ReadUInt32();
            foreach(var field in duelBGManager.places)
                field.SetDisabled(disabledFields);

            return UniTask.CompletedTask;
        }

        protected override async UniTask GameMessage_RandomSelected(BinaryReader reader)
        {
            /*var player = LocalPlayer(*/reader.ReadByte()/*)*/;
            var count = reader.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var gps = reader.ReadGPS();
                var card = Core.GCS_Get(gps);
                if (card == null)
                {
                    DebugNoCard();
                    continue;
                }
                cardsBeTarget.Add(card);
                card.AnimationTarget();
                await UniTask.WaitForSeconds(0.5f);
            }
        }

        protected override async UniTask GameMessage_BecomeTarget(BinaryReader reader)
        {
            var count = reader.ReadByte();
            for(var i = 0; i < count; i++)
            {
                var gps = reader.ReadGPS();
                var card = Core.GCS_Get(gps);
                if (card == null)
                {
                    DebugNoCard();
                    continue;
                }

                cardsBeTarget.Add(card);
                if (cardsInChain.Count > 0)
                    cardsInChain[^1].AddEffectTarget(card);

                if(cardsInChain.Count == 0
                    && card.InPendulumZone())
                {
                    //Pendulum
                }
                else
                {
                    card.AnimationTarget();
                    await UniTask.WaitForSeconds(0.5f);
                }
            }

            if(duelPhase == DuelPhase.Main1 || duelPhase == DuelPhase.Main2)
                if (cardsInChain.Count == 0)
                    if (cardsBeTarget.Count == 2)
                        if (cardsBeTarget[0].InPendulumZone())
                            if (cardsBeTarget[1].InPendulumZone())
                                if (cardsBeTarget[0].p.controller == cardsBeTarget[1].p.controller)
                                    inPendulumSummon = true;

            if (!inPendulumSummon)
                return;

            if(condition == Condition.Duel && !Config.GetBool("DuelPendulum", true))
                return;
            if (condition == Condition.Watch && !Config.GetBool("WatchPendulum", true))
                return;
            if (condition == Condition.Replay && !Config.GetBool("ReplayPendulum", true))
                return;

            await duelBGManager.PlaySummonPendulum();
        }

        protected override UniTask GameMessage_CardTarget(BinaryReader reader)
        {
            var from = reader.ReadGPS();
            var to = reader.ReadGPS();

            var cardFrom = Core.GCS_Get(from);
            var cardTo = Core.GCS_Get(to);
            if(cardFrom == null || cardTo == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }
            if (currentMessage == GameMessage.CardTarget)
                cardFrom.AddTarget(cardTo);
            else if (currentMessage == GameMessage.Equip)
            {
                cardFrom.equipedCard = cardTo;
                duelBGManager.OnEquipApplied(from.InMyControl() ? 0 : 1);
            }

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Equip(BinaryReader reader)
        {
            return GameMessage_CardTarget(reader);
        }

        protected override UniTask GameMessage_CancelTarget(BinaryReader reader)
        {
            var from = reader.ReadGPS();
            var card = Core.GCS_Get(from);
            if (card == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }
            if(currentMessage == GameMessage.CancelTarget)
                card.targets.Clear();
            else if (currentMessage == GameMessage.Unequip)
                card.equipedCard = null;

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Unequip(BinaryReader reader)
        {
            return GameMessage_CancelTarget(reader);
        }

        protected override UniTask GameMessage_AddCounter(BinaryReader reader)
        {
            var type = reader.ReadUInt16();
            var gps = reader.ReadShortGPS();
            var card = Core.GCS_Get(gps);
            if (card == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }
            var count = reader.ReadUInt16();
            if (currentMessage == GameMessage.AddCounter)
                card.AddCounter(type, count);
            else if (currentMessage == GameMessage.RemoveCounter)
                card.RemoveCounter(type, count);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_RemoveCounter(BinaryReader reader)
        {
            return GameMessage_AddCounter(reader);
        }

        protected override UniTask GameMessage_Waiting(BinaryReader reader)
        {
            if(Core.InIgnoranceReplay())
                return UniTask.CompletedTask;
            duelBGManager.SetPlayableGuide(false);
            return UniTask.CompletedTask;
        }

        protected override async UniTask GameMessage_AnnounceRace(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            //var player = LocalPlayer(reader.ReadByte());
            reader.ReadByte();
            ES_min = reader.ReadByte();

            var available = reader.ReadUInt32();
            var selections = new List<string>() { InterString.Get("宣言种族") };
            var responses = new List<int>();
            for (int i = 0; i < (uint)CardRace.Count; i++)
            {
                if ((available & (1 << i)) > 0)
                {
                    selections.Add(StringHelper.GetUnsafe(1020 + i));
                    responses.Add(1 << i);
                }
            }
            Core.GetUI<OcgCoreUI>().ShowPopupSelection(selections, responses);

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_AnnounceAttrib(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            //var player = LocalPlayer(reader.ReadByte());
            reader.ReadByte();

            ES_min = reader.ReadByte();

            var available = reader.ReadUInt32();
            var selections = new List<string>() { InterString.Get("宣言属性") };
            var responses = new List<int>();
            for (int i = 0; i < (uint)CardAttribute.Count; i++)
            {
                if ((available & (1 << i)) > 0)
                {
                    selections.Add(StringHelper.GetUnsafe(1010 + i));
                    responses.Add(1 << i);
                }
            }
            Core.GetUI<OcgCoreUI>().ShowPopupSelection(selections, responses);

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_AnnounceNumber(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            //var player = LocalPlayer(reader.ReadByte());
            reader.ReadByte();

            var count = reader.ReadByte();
            ES_min = 1;

            var selections = new List<string>() { InterString.Get("宣言数字") };
            var responses = new List<int>();
            for (int i = 0; i < count; i++)
            {
                selections.Add(reader.ReadUInt32().ToString());
                responses.Add(i);
            }
            Core.GetUI<OcgCoreUI>().ShowPopupSelection(selections, responses);

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_AnnounceCard(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            //var player = LocalPlayer(reader.ReadByte());
            reader.ReadByte();

            ES_searchCodes.Clear();
            var count = reader.ReadByte();
            for (int i = 0; i < count; i++)
                ES_searchCodes.Add(reader.ReadInt32());
            var selections = new List<string>()
                    {
                        InterString.Get("请输入关键字："),
                        InterString.Get("搜索"),
                        string.Empty,
                        string.Empty
                    };
            Core.GetUI<OcgCoreUI>().ShowPopupInput(selections, Core.OnAnnounceCard, null);

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SelectIdleCmd(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            /*var player = LocalPlayer(*/reader.ReadChar()/*)*/;

            var count = reader.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var card = Core.GCS_Get(gps);
                if(card == null)
                {
                    DebugNoCard();
                    continue;
                }
                card.SetCode(code);
                card.AddButton((i << 16) + 0, InterString.Get("召唤"), ButtonType.Summon);
            }

            count = reader.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var card = Core.GCS_Get(gps);
                if (card == null)
                {
                    DebugNoCard();
                    continue;
                }
                card.SetCode(code);
                if (card.InPendulumZone())
                    card.AddButton((i << 16) + 1, InterString.Get("灵摆召唤"), ButtonType.PenSummon);
                else
                    card.AddButton((i << 16) + 1, InterString.Get("特殊召唤"), ButtonType.SpSummon);
            }

            count = reader.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var card = Core.GCS_Get(gps);
                if (card == null)
                {
                    DebugNoCard();
                    continue;
                }
                card.SetCode(code);
                if (card.p.InPosition(CardPosition.Defence))
                    card.AddButton((i << 16) + 2, InterString.Get("变为攻击表示"), ButtonType.ToAttackPosition);
                else
                    card.AddButton((i << 16) + 2, InterString.Get("变为守备表示"), ButtonType.ToDefensePosition);
            }

            count = reader.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var card = Core.GCS_Get(gps);
                if (card != null)
                {
                    card.SetCode(code);
                    card.AddButton((i << 16) + 3, InterString.Get("设置"), ButtonType.SetMonster);
                }
            }

            count = reader.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var card = Core.GCS_Get(gps);
                if (card != null)
                {
                    card.SetCode(code);
                    card.AddButton((i << 16) + 4, InterString.Get("设置"), ButtonType.SetSpell);
                }
            }

            count = reader.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var descP = reader.ReadInt32();
                var desc = StringHelper.Get(descP);
                var card = Core.GCS_Get(gps);
                if (card != null)
                {
                    card.SetCode(code);
                    if (descP == 1160)
                        card.AddButton((i << 16) + 5, InterString.Get("灵摆发动"), ButtonType.SetPendulum);
                    else
                    {
                        var eff = new Effect
                        {
                            ptr = (i << 16) + 5,
                            desc = desc
                        };
                        card.effects.Add(eff);
                        card.AddButton((i << 16) + 5, InterString.Get("发动效果"), ButtonType.Activate);
                    }
                }
            }

            foreach (var c in cards)
                c.CreateButtons();
            int buttonsCount = 0;
            foreach (var c in cards)
                buttonsCount += c.buttons.Count;
            if (buttonsCount == 0)
                PhaseButtonHandler.SetHint();

            var bp = reader.ReadByte();
            var ep = reader.ReadByte();
            var shuffle = reader.ReadByte();
            if (bp == 1)
                PhaseButtonHandler.battlePhase = true;
            if (ep == 1)
                PhaseButtonHandler.endPhase = true;

            duelBGManager.ShowBgHint();

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SelectBattleCmd(BinaryReader reader)
        {
            duelBGManager.HideAttackLine();
            duelBGManager.HideDuelFinalBlowText();

            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            /*var player = LocalPlayer(*/reader.ReadChar()/*)*/;

            var count = reader.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var desc = StringHelper.Get(reader.ReadInt32());
                var card = Core.GCS_Get(gps);
                if (card != null)
                {
                    card.SetCode(code);
                    var eff = new Effect
                    {
                        ptr = (i << 16) + 0,
                        desc = desc
                    };
                    card.effects.Add(eff);
                    card.AddButton((i << 16) + 0, InterString.Get("发动效果"), ButtonType.Activate);
                }
            }
            count = reader.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                reader.ReadByte();
                var card = Core.GCS_Get(gps);
                if (card != null)
                {
                    card.SetCode(code);
                    card.AddButton((i << 16) + 1, InterString.Get("攻击"), ButtonType.Battle);
                }
            }
            foreach (var c in cards)
                c.CreateButtons();
            var buttonsCount = 0;
            foreach (var c in cards)
                buttonsCount += c.buttons.Count;
            if (buttonsCount == 0)
                PhaseButtonHandler.SetHint();

            var mp2 = reader.ReadByte();
            var ep = reader.ReadByte();
            if (mp2 == 1)
                PhaseButtonHandler.main2Phase = true;
            if (ep == 1)
                PhaseButtonHandler.endPhase = true;
            duelBGManager.ShowBgHint();

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SelectYesNo(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            /*var player = LocalPlayer(*/reader.ReadByte()/*)*/;

            var desc = StringHelper.Get(reader.ReadInt32());
            var title = InterString.Get("选择");
            var selections = new List<string>
                    {
                        title,
                        desc,
                        InterString.Get("是"),
                        InterString.Get("否")
                    };

            void yes()
            {
                var binaryMaster = new BinaryMaster();
                binaryMaster.writer.Write(1);
                Core.SendReturn(binaryMaster.Get());
            }

            void no()
            {
                var binaryMaster = new BinaryMaster();
                binaryMaster.writer.Write(0);
                Core.SendReturn(binaryMaster.Get());
            }

            Core.GetUI<OcgCoreUI>().ShowPopupYesOrNo(selections, yes, no);

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SelectEffectYn(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            /*var player = LocalPlayer(*/reader.ReadByte()/*)*/;

            /*var code = */reader.ReadInt32();
            var gps = reader.ReadShortGPS();
            reader.ReadByte();
            var cr = reader.ReadInt32();
            var card = Core.GCS_Get(gps);

            if(card == null)
            {
                DebugNoCard();
                return;
            }

            var desc = string.Empty;
            var displayname = "「" + card.GetData().Name + "」";
            var forReplaceFirst = new Regex("\\[%ls\\]");
            if (cr == 0)
            {
                desc = StringHelper.Get(200);//是否在[%ls]发动[%ls]的效果？
                desc = forReplaceFirst.Replace(desc, StringHelper.FormatLocation(gps), 1);
                desc = ES_hint + "，" + forReplaceFirst.Replace(desc, displayname, 1);
            }
            else if (cr == 221)
            {
                desc = StringHelper.Get(221);//是否在[%ls]发动[%ls]的诱发类效果？
                desc = forReplaceFirst.Replace(desc, StringHelper.FormatLocation(gps), 1);
                desc = forReplaceFirst.Replace(desc, displayname, 1);
                desc = ES_hint + "，" + desc + "\n" + StringHelper.Get(223);//稍后将询问其他可以发动的效果。
            }
            else
            {
                desc = StringHelper.Get(cr);
                desc = forReplaceFirst.Replace(desc, displayname, 1);
            }

            var oneCardToSend = new List<GameCard>() { card };
            Core.GetUI<OcgCoreUI>().ShowPopupSelectCard(desc, oneCardToSend, 1, 1, true, false);

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SelectChain(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            var prompt=global::YGOSharp.Network.ChainPrompt.Read(reader,condition==Condition.Replay?CurrentReplayNativePrompts:(DuelFormat.LocalSession || RoomServant.FromHandTest || inPuzzle));
            int spcount=prompt.SpecialCount;
            int hint0=prompt.Hint0,hint1=prompt.Hint1;
            var chainCards=new List<GameCard>();
            int forceCount=0;
            foreach(var entry in prompt.Entries)
            {
                if(entry.Forced)forceCount++;
                var gps=new GPS{controller=(uint)LocalPlayer(entry.Controller),location=entry.Location,sequence=entry.Sequence,position=entry.Subsequence};
                var card=Core.GCS_Get(gps)??Core.GCS_Create(gps,true);
                if(!chainCards.Contains(card))chainCards.Add(card);
                card.SetCode(entry.Code%1000000000);
                card.effects.Add(new Effect{flag=entry.Flag,ptr=prompt.Entries.IndexOf(entry),desc=StringHelper.Get(entry.Description),forced=entry.Forced});
            }

            var handleFlag = 0;

            // 无强制发动的卡
            if (forceCount == 0)
            {
                // 无关键卡
                if (spcount == 0)
                {
                    if (chainCondition == ChainCondition.No)
                    {
                        handleFlag = 0;
                    }
                    else if (chainCondition == ChainCondition.All)
                    {
                        if (chainCards.Count == 0)
                        {
                            handleFlag = -1;
                        }
                        else
                        {
                            if (chainCards.Count == 1 && chainCards[0].effects.Count == 1)
                                handleFlag = 1;
                            else
                                handleFlag = 2;
                        }
                    }
                    else if (chainCondition == ChainCondition.Smart)
                    {
                        handleFlag = 0;
                    }
                }
                // 有关键卡
                else
                {
                    if (chainCards.Count == 0)
                    {
                        handleFlag = 0;
                        if (chainCondition == ChainCondition.All)
                            handleFlag = -1;
                    }
                    else if (chainCondition == ChainCondition.No)
                    {
                        handleFlag = 0;
                    }
                    else
                    {
                        if (chainCards.Count == 1 && chainCards[0].effects.Count == 1)
                            handleFlag = 1;
                        else
                            handleFlag = 2;
                    }
                }
            }
            // 有强制发动的卡
            else
            {
                if (chainCards.Count == 1 && chainCards[0].effects.Count == 1)
                {
                    handleFlag = 3;
                }
                else
                {
                    handleFlag = 4;
                }
            }

            //Debug.Log($"ChainCondition: {chainCondition} spcount: {spcount} handleFlag: {handleFlag}");

            // handleFlag
            // -1   无卡 需要提醒
            // 0    无卡 直接回应
            // 1    一张卡需要处理
            // 2    多张卡需要处理
            // 3    一张卡需要处理（强制发动）
            // 4    多张卡需要处理（强制发动）

            switch (handleFlag)
            {
                case 1:
                case 2:
                    Core.GetUI<OcgCoreUI>().ShowPopupSelectCard(InterString.Get("[?]，是否连锁？", ES_hint), chainCards, 1, 1, true, false);
                    break;
                case 3:
                case 4:
                    Core.GetUI<OcgCoreUI>().ShowPopupSelectCard(InterString.Get("[?]，请选择效果发动。", ES_hint), chainCards, 1, 1, false, false);
                    break;
                default:
                    Core.OnResend();
                    return;
            }

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SelectCard(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            /*var player = LocalPlayer(*/reader.ReadByte()/*)*/;
            var cancelable = reader.ReadByte() != 0;
            ES_min = reader.ReadByte();
            ES_max = reader.ReadByte();
            ES_level = 0;
            var count = reader.ReadByte();
            cardsInSelection.Clear();
            for (var i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadGPS();
                var card = Core.GCS_Get(gps);
                if(card == null)
                {
                    DebugNoCard();
                    continue;
                }
                card.SetCode(code);
                card.selectPtr = i;
                cardsInSelection.Add(card);
            }
            if (ES_selectCardFromFieldFirstFlag && cancelable)
            {
                ES_selectCardFromFieldFirstFlag = false;
                Core.OnResend();
                return;
            }

            if (ES_min == 1 && count == 1)
            {
                var binaryMaster = new BinaryMaster();
                binaryMaster.writer.Write(count);
                foreach (var c in cardsInSelection)
                    binaryMaster.writer.Write(c.selectPtr);
                Core.SendReturn(binaryMaster.Get());
                return;
            }
            bool allOnfield = cardsInSelection.All(c => c.p.InLocation(CardLocation.Onfield) && !c.p.InLocation(CardLocation.Overlay));
            if (allOnfield)
                Core.FieldSelect(ES_selectHint, cardsInSelection, ES_min, ES_max, cancelable, ES_min == 0);
            else
                Core.GetUI<OcgCoreUI>().ShowPopupSelectCard(ES_selectHint, cardsInSelection, ES_min, ES_max, cancelable, ES_min == 0);

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SelectUnselect(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            /*var player = LocalPlayer(*/reader.ReadByte()/*)*/;
            var finishable = reader.ReadByte() != 0;
            var cancelable = reader.ReadByte() != 0 || finishable;
            ES_min = reader.ReadByte();
            ES_max = reader.ReadByte();
            ES_level = 0;
            var count = reader.ReadByte();
            cardsInSelection.Clear();
            for (var i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadGPS();
                var card = Core.GCS_Get(gps);
                if(card == null)
                {
                    DebugNoCard();
                    continue;
                }
                card.SetCode(code);
                card.selectPtr = i;
                cardsInSelection.Add(card);
            }
            var allOnfield = cardsInSelection.All(c => c.p.InLocation(CardLocation.Onfield) && !c.p.InLocation(CardLocation.Overlay));

            if (!string.IsNullOrEmpty(ES_selectHint))
                ES_selectUnselectHint = ES_selectHint;
            if (string.IsNullOrEmpty(ES_selectUnselectHint))
                ES_selectUnselectHint = InterString.Get("请选择卡片");

            if (allOnfield)
                Core.FieldSelect(ES_selectUnselectHint, cardsInSelection, 1, 1, cancelable, finishable);
            else
                Core.GetUI<OcgCoreUI>().ShowPopupSelectCard(ES_selectUnselectHint, cardsInSelection, 1, 1, cancelable, finishable);

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SelectSum(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            ES_overFlow = reader.ReadByte() != 0;
            /*var player = LocalPlayer(*/reader.ReadByte()/*)*/;
            ES_level = reader.ReadInt32();
            ES_min = reader.ReadByte();
            ES_max = reader.ReadByte();

            if (ES_min < 1) ES_min = 1;
            if (ES_max < 1) ES_max = 99;
            cardsInSelection.Clear();
            cardsMustBeSelected.Clear();

            var count = reader.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var para = reader.ReadInt32();
                var card = Core.GCS_Get(gps);
                if(card == null)
                {
                    DebugNoCard();
                    continue;
                }
                card.SetCode(code);
                card.selectPtr = i;
                card.levelForSelect_1 = para & 0xffff;
                card.levelForSelect_2 = para >> 16;
                if ((para & 0x80000000) > 0)
                {
                    card.levelForSelect_1 = para & 0x7fffffff;
                    card.levelForSelect_2 = card.levelForSelect_1;
                }
                if (card.levelForSelect_2 == 0)
                    card.levelForSelect_2 = card.levelForSelect_1;
                cardsInSelection.Add(card);
                cardsMustBeSelected.Add(card);
            }

            bool sendable = false;
            var level = 0;
            foreach (var c in cardsMustBeSelected)
                level += c.levelForSelect_1;
            if (level == ES_level)
                sendable = true;
            if (!sendable)
            {
                level = 0;
                foreach (var c in cardsMustBeSelected)
                    level += c.levelForSelect_2;
                if (level == ES_level)
                    sendable = true;
            }
            if (sendable)
            {
                var binaryMaster = new BinaryMaster();
                binaryMaster.writer.Write(cardsMustBeSelected.Count);
                for (var i = 0; i < cardsMustBeSelected.Count; i++)
                    binaryMaster.writer.Write(i);
                Core.SendReturn(binaryMaster.Get());
                return;
            }

            count = reader.ReadByte();
            for (var i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var para = reader.ReadInt32();
                var card = Core.GCS_Get(gps);
                if(card == null)
                {
                    DebugNoCard();
                    continue;
                }
                card.SetCode(code);
                card.selectPtr = i;
                card.levelForSelect_1 = para & 0xffff;
                card.levelForSelect_2 = para >> 16;
                if ((para & 0x80000000) > 0)
                {
                    card.levelForSelect_1 = para & 0x7fffffff;
                    card.levelForSelect_2 = card.levelForSelect_1;
                }
                if (card.levelForSelect_2 == 0)
                    card.levelForSelect_2 = card.levelForSelect_1;
                cardsInSelection.Add(card);
            }

            level = 0;
            foreach (var c in cardsInSelection)
                level += c.levelForSelect_1;
            if (level == ES_level)
                sendable = true;
            if (!sendable)
            {
                level = 0;
                foreach (var c in cardsInSelection)
                    level += c.levelForSelect_2;
                if (level == ES_level)
                    sendable = true;
            }
            if (sendable)
            {
                var binaryMaster = new BinaryMaster();
                binaryMaster.writer.Write((byte)cardsInSelection.Count);
                foreach (var c in cardsMustBeSelected)
                    binaryMaster.writer.Write((byte)c.selectPtr);
                foreach (var c in cardsInSelection)
                    if (!cardsMustBeSelected.Contains(c))
                        binaryMaster.writer.Write((byte)c.selectPtr);
                Core.SendReturn(binaryMaster.Get());
                return;
            }

            var allOnfield = cardsInSelection.All(c => c.p.InLocation(CardLocation.Onfield) && !c.p.InLocation(CardLocation.Overlay));
            if (allOnfield)
                Core.FieldSelect(ES_selectHint, cardsInSelection, ES_min, ES_max, false, false);
            else
                Core.GetUI<OcgCoreUI>().ShowPopupSelectCard(ES_selectHint, cardsInSelection, ES_min, ES_max, false, false);

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SelectTribute(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            /*var player = LocalPlayer(*/reader.ReadByte()/*)*/;
            var cancelable = reader.ReadByte() != 0;
            ES_min = reader.ReadByte();
            ES_max = reader.ReadByte();
            ES_level = 0;
            var count = reader.ReadByte();
            cardsInSelection.Clear();
            for (var i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var card = Core.GCS_Get(gps);
                if(card == null)
                {
                    DebugNoCard();
                    continue;
                }
                card.SetCode(code);
                card.selectPtr = i;
                int para = reader.ReadByte();
                card.levelForSelect_1 = para;
                card.levelForSelect_2 = para;
                cardsInSelection.Add(card);
            }
            var allOnfield = cardsInSelection.All(c => c.p.InLocation(CardLocation.Onfield) && !c.p.InLocation(CardLocation.Overlay));
            if (allOnfield)
                Core.FieldSelect(ES_selectHint, cardsInSelection, ES_min, ES_max, cancelable, false);
            else
                Core.GetUI<OcgCoreUI>().ShowPopupSelectCard(ES_selectHint, cardsInSelection, ES_min, ES_max, cancelable, false);

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SelectOption(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            /*var player = LocalPlayer(*/reader.ReadByte()/*)*/;
            var count = reader.ReadByte();
            if (count > 1)
            {
                var selections = new List<string>() { InterString.Get("效果选择") };
                var responses = new List<int> { };
                for (var i = 0; i < count; i++)
                {
                    var desc = StringHelper.Get(reader.ReadInt32());
                    selections.Add(desc);
                    responses.Add(i);
                }
                Core.GetUI<OcgCoreUI>().ShowPopupSelection(selections, responses);
            }
            else
            {
                var binaryMaster = new BinaryMaster();
                binaryMaster.writer.Write(0);
                Core.SendReturn(binaryMaster.Get());
                return;
            }

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SelectPlace(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            /*var player = LocalPlayer(*/reader.ReadByte()/*)*/;
            var min = reader.ReadByte();
            if (min == 0)
                min = 1;
            ES_min = min;
            var filter = ~reader.ReadUInt32();
            bool haveMySpellZone = false;
            bool haveOpSpellZone = false;
            foreach (var place in duelBGManager.places)
            {
                var p = place.HighlightThisZone(filter, min);
                if(p != null)
                    if (p.InLocation(CardLocation.SpellZone))
                    {
                        if (p.InMyControl())
                            haveMySpellZone = true;
                        else
                            haveOpSpellZone = true;
                    }
            }
            if (haveMySpellZone)
                HideMyHandCard = true;
            else if (haveOpSpellZone)
                HideOpHandCard = true;

            if (currentMessage == GameMessage.SelectPlace)
            {
                if (Es_selectMSGHintType == 3)
                {
                    if (Es_selectMSGHintPlayer == 0)
                        ES_selectHint = InterString.Get("请为我方的「[?]」选择位置。", CardsManager.Get(Es_selectMSGHintData).Name);
                    else
                        ES_selectHint = InterString.Get("请为对方的「[?]」选择位置。", CardsManager.Get(Es_selectMSGHintData).Name);
                }
            }
            else if (ES_selectHint == string.Empty)
                ES_selectHint = StringHelper.GetUnsafe(570);//请选择要变成不能使用的卡片区域
            Core.GetUI<OcgCoreUI>().SetHint(ES_selectHint);

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SelectDisfield(BinaryReader reader)
        {
            await GameMessage_SelectPlace(reader);
        }

        protected override async UniTask GameMessage_SelectPosition(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            /*var player = LocalPlayer(*/reader.ReadByte()/*)*/;
            var code = reader.ReadInt32();
            int positions = reader.ReadByte();
            var op1 = 0x1;
            var op2 = 0x4;
            if (positions == 0x1 || positions == 0x2 || positions == 0x4 || positions == 0x8)
            {
                var binaryMaster = new BinaryMaster();
                binaryMaster.writer.Write(positions);
                Core.SendReturn(binaryMaster.Get());
                return;
            }
            else if (positions == (0x1 | 0x4 | 0x8))
            {
                Core.GetUI<OcgCoreUI>().ShowPopupPosition(code, 3);
            }
            else
            {
                if ((positions & 0x1) > 0)
                    op1 = 0x1;
                if ((positions & 0x2) > 0)
                    op1 = 0x2;
                if ((positions & 0x4) > 0)
                    op2 = 0x4;
                if ((positions & 0x8) > 0)
                {
                    if ((positions & 0x4) > 0)
                        op1 = 0x4;
                    op2 = 0x8;
                }
                Core.GetUI<OcgCoreUI>().ShowPopupPosition(code, 2, op1, op2);
            }

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SelectCounter(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            var length_of_message = reader.BaseStream.Length;
            var version1033b = (length_of_message - 5) % 8 == 0;
            /*var player = LocalPlayer(*/reader.ReadByte()/*)*/;
            reader.ReadInt16();
            if (version1033b)
                ES_min = reader.ReadByte();
            else
                ES_min = reader.ReadUInt16();
            var count = reader.ReadByte();
            cardsInSelection.Clear();
            for (int i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var card = Core.GCS_Get(gps);
                var pew = 0;
                if (version1033b)
                    pew = reader.ReadByte();
                else
                    pew = reader.ReadUInt16();
                if (card != null)
                {
                    card.SetCode(code);
                    card.counterCanCount = pew;
                    card.counterSelected = 0;
                    card.selectPtr = i;
                    cardsInSelection.Add(card);
                }
            }
            Core.FieldSelect(InterString.Get("请取除指示物"), cardsInSelection, ES_min, ES_min, true, false);

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SortChain(BinaryReader reader)
        {
            if (Core.InIgnoranceReplay())
                return;
            duelBGManager.SetPlayableGuide(true);

            /*var player = LocalPlayer(*/reader.ReadByte()/*)*/;
            var ES_sortSum = 0;
            var count = reader.ReadByte();
            List<GameCard> sortingCards = new List<GameCard>();
            for (int i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var card = Core.GCS_Get(gps);
                if (card != null)
                {
                    card.SetCode(code);
                    if (!sortingCards.Contains(card))
                        sortingCards.Add(card);
                    ES_sortSum++;
                }
            }
            Core.GetUI<OcgCoreUI>().ShowPopupSelectCard(InterString.Get("请为卡片排序。"), sortingCards, sortingCards.Count, sortingCards.Count, false, false);

            await UniTask.WaitUntil(() => dispatcher.playerResponed);
        }

        protected override async UniTask GameMessage_SortCard(BinaryReader reader)
        {
            await GameMessage_SortChain(reader);
        }

        #endregion
    }
}
