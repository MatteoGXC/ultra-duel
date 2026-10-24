using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.Duel
{
    public class LogMessage : MessageProcessor
    {
        public LogMessage(MessageDispatcher dispatcher) : base(dispatcher) { }

        #region Params

        public static int chainSolvingIndex;
        public static int life0;
        public static int life1;

        private bool isFirst;
        private int MasterRule;
        private int turns;
        private bool myTurn;
        private DuelPhase duelPhase;
        private bool inPendulumSummon;
        private GameCard lastMoveCard;
        public static uint lastMoveReason;

        private readonly List<GameCard> cardsInChain = new();
        private readonly List<GameCard> cardsBeTarget = new();
        private uint lastSpSummonReason;
        private GameCard lastConfirmedCard;
        private GameCard attackingCard;

        private DuelLog DuelLog => Core.GetUI<OcgCoreUI>().DuelLog;

        #endregion

        #region Tools

        private void LogDebug(string text)
        {
            Debug.LogError(text);
            MessageManager.Cast(text);
        }

        private void DebugNoCard()
        {
            LogDebug($"[Duel Log]: Not found card for {OcgCore.currentMessage}.");
        }

        private void ResetState()
        {
            chainSolvingIndex = 0;
            inPendulumSummon = false;

            cardsInChain.Clear();
            cardsBeTarget.Clear();
        }

        private int LocalPlayer(int player)
        {
            if(player == 0 || player == 1)
            {
                if(isFirst)
                    return player;
                return 1 - player;
            }
            return player;
        }

        private void AddMoveHeader(string text)
        {
            DuelLog.AddTextMessageToLog(text, boxed: true);
        }

        private string GetMoveReasonText(GPS from, GPS to, uint reason)
        {
            if ((reason & (uint)CardReason.DESTROY) > 0)
                return InterString.Get("破坏");
            if ((reason & (uint)CardReason.RELEASE) > 0)
                return InterString.Get("解放");
            if ((reason & (uint)CardReason.BATTLE) > 0)
                return InterString.Get("战斗破坏");
            if ((reason & (uint)CardReason.FLIP) > 0)
                return InterString.Get("反转");
            if (to.InLocation(CardLocation.Hand))
            {
                var textReason = InterString.Get("回到");
                if (from.InLocation(CardLocation.Deck, CardLocation.Extra))
                    textReason = InterString.Get("加入");
                if (from.InLocation(CardLocation.Grave, CardLocation.Removed))
                    textReason = InterString.Get("回收");
                return textReason;
            }
            if (to.InLocation(CardLocation.SpellZone)
                && !from.InLocation(CardLocation.SpellZone)
                && !from.InLocation(CardLocation.Hand))
                return InterString.Get("放置");
            if (from.InLocation(CardLocation.SpellZone)
                && to.InLocation(CardLocation.SpellZone))
                return InterString.Get("移动");
            if (from.InLocation(CardLocation.MonsterZone)
                && to.InLocation(CardLocation.MonsterZone))
                return InterString.Get("移动");
            if (to.InLocation(CardLocation.MonsterZone)
                && !from.InLocation(CardLocation.MonsterZone))
                return InterString.Get("回到");
            return InterString.Get("送至");
        }

        #endregion

        #region Message Process

        protected override UniTask GameMessage_Start(BinaryReader reader)
        {
            ResetState();

            isFirst = (reader.ReadByte() & 0xF) == 0;
            if (reader.BaseStream.Length > 17)
                MasterRule = reader.ReadByte() + 1;
            var engineLife0 = reader.ReadInt32();
            var engineLife1 = reader.ReadInt32();
            life0 = isFirst ? engineLife0 : engineLife1;
            life1 = isFirst ? engineLife1 : engineLife0;
            turns = 0;

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_ReloadField(BinaryReader reader)
        {
            ResetState();

            if (OcgCore.inPuzzle)
            {
                isFirst = true;
                myTurn = true;
            }

            MasterRule = reader.ReadByte() + 1;
            if (MasterRule > 255)
                MasterRule -= 255;

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_NewTurn(BinaryReader reader)
        {
            turns++;
            myTurn = isFirst ? (turns % 2 != 0) : (turns % 2 == 0);

            var item = ABLoader.LoadMasterDuelGameObject("DuelLogNewTurn");
            item.transform.GetChild(1).GetComponent<Image>().color = myTurn ? DuelLog.myColor : DuelLog.opColor;
            item.transform.GetChild(2).GetComponent<Text>().text = InterString.Get("第[?]回合", turns.ToString());
            item.transform.GetChild(3).GetComponent<Image>().material = Core.GetUI<OcgCoreUI>().AvatarPlayer0.material;
            item.transform.GetChild(4).GetComponent<Image>().material = Core.GetUI<OcgCoreUI>().AvatarPlayer1.material;
            item.transform.GetChild(3).GetComponent<Image>().sprite = Core.GetUI<OcgCoreUI>().AvatarPlayer0.sprite;
            item.transform.GetChild(4).GetComponent<Image>().sprite = Core.GetUI<OcgCoreUI>().AvatarPlayer1.sprite;
            item.transform.GetChild(7).GetComponent<Text>().text = life0.ToString();
            item.transform.GetChild(8).GetComponent<Text>().text = life1.ToString();
            DuelLog.AddLog(item);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_NewPhase(BinaryReader reader)
        {
            duelPhase = (DuelPhase)reader.ReadInt16();
            string textPhase = duelPhase switch
            {
                DuelPhase.Draw => InterString.Get("抽卡阶段"),
                DuelPhase.Standby => InterString.Get("准备阶段"),
                DuelPhase.Main1 => InterString.Get("主要阶段1"),
                DuelPhase.Battle => InterString.Get("战斗阶段"),
                DuelPhase.Main2 => InterString.Get("主要阶段2"),
                DuelPhase.End => InterString.Get("结束阶段"),
                _ => string.Empty,
            };

            if(textPhase != string.Empty)
            {
                var item = ABLoader.LoadMasterDuelGameObject("DuelLogNewPhase");
                item.transform.GetChild(1).GetComponent<Text>().text = textPhase;
                DuelLog.AddLog(item);
            }

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Move(BinaryReader reader)
        {
            var code = (int)reader.ReadUInt32();
            var from = reader.ReadGPS();
            var to = reader.ReadGPS();
            var reason = reader.ReadUInt32();
            lastMoveReason = reason;

            GameCard card = Core.GCS_Get(from);
            if(card == null)
            {
                if(from.location == 0)
                {
                    //召唤衍生物
                }
                else
                {
                    Debug.Log($"[Duel Log]: Unexpect no GameCard, code: {code}, moveReason: {reason}");
                }
                return UniTask.CompletedTask;
            }

            lastMoveCard = card;
            Card data;
            if (code > 0)
                data = CardsManager.Get(code);
            else
                data = card.GetData();

            //不记录召唤怪兽的移动
            if (to.InPosition(CardPosition.FaceUp)
                && to.InLocation(CardLocation.MonsterZone)
                && from.InLocation(CardLocation.Hand)
                && !to.InLocation(CardLocation.Overlay))
                return UniTask.CompletedTask;

            //不记录特殊召唤怪兽的移动
            if ((reason & (uint)CardReason.SPSUMMON) > 0
                && to.InLocation(CardLocation.MonsterZone)
                && !from.InLocation(CardLocation.MonsterZone)
                && !to.InLocation(CardLocation.Overlay))
            {
                lastSpSummonReason = reason;
                return UniTask.CompletedTask;
            }

            //不记录手卡发动魔陷的移动
            if (from.InLocation(CardLocation.Hand)
                && to.InLocation(CardLocation.SpellZone)
                && to.InPosition(CardPosition.FaceUp)
                && !data.HasType(CardType.Monster))
                return UniTask.CompletedTask;

            //不记录手卡发动的灵摆卡的移动
            if (from.InLocation(CardLocation.Hand)
                && to.InLocation(CardLocation.SpellZone)
                && to.InPosition(CardPosition.FaceUp)
                && data.HasType(CardType.Pendulum)
                && to.InPendulumSequence())
                return UniTask.CompletedTask;

            //不记录发动过的魔陷自动送墓的移动
            if(from.InLocation(CardLocation.SpellZone)
                && !to.InLocation(CardLocation.SpellZone)
                && !data.HasType(CardType.Monster)
                && (reason & (uint)CardReason.RULE) > 0)
                return UniTask.CompletedTask;

            //不记录成为超量素材的卡的移动
            if (from.InLocation(CardLocation.Overlay)
                && to.InLocation(CardLocation.Overlay))
                return UniTask.CompletedTask;

            //不记录卡组洗牌的卡的移动
            if (from.InLocation(CardLocation.Deck)
                && to.InLocation(CardLocation.Deck))
                return UniTask.CompletedTask;

            //不记录场上被盖放的卡的移动
            if (to.InLocation(CardLocation.Onfield)
                && to.InPosition(CardPosition.FaceDown))
                return UniTask.CompletedTask;

            var boxed = (reason & (uint)CardReason.COST) > 0;
            if (boxed)
                AddMoveHeader(InterString.Get("代价"));
            var textReason = GetMoveReasonText(from, to, reason & ~(uint)CardReason.COST);

            if (data.HasType(CardType.Token))
                DuelLog.AddSingleCardMessageToLog(data.Id, null, from, textReason, boxed: boxed);
            else
                DuelLog.AddSingleCardMessageToLog(data.Id, from, to, textReason, boxed: boxed);
            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Summoning(BinaryReader reader)
        {
            var code = (int)reader.ReadUInt32();
            var gps = reader.ReadGPS();
            var card = Core.GCS_Get(gps);
            if (card == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }

            lastConfirmedCard = card;
            var data = card.GetData();
            string textReason;
            if(OcgCore.currentMessage == GameMessage.Summoning)
                textReason = InterString.Get("召唤");
            else if (OcgCore.currentMessage == GameMessage.SpSummoning)
            {
                if ((lastSpSummonReason & (uint)CardReason.Ritual) > 0)
                    textReason = InterString.Get("仪式召唤");
                else if ((lastSpSummonReason & (uint)CardReason.Fusion) > 0)
                    textReason = InterString.Get("融合召唤");
                else if ((lastSpSummonReason & (uint)CardReason.Synchro) > 0)
                    textReason = InterString.Get("同调召唤");
                else if ((lastSpSummonReason & (uint)CardReason.Xyz) > 0)
                    textReason = InterString.Get("超量召唤");
                else if ((lastSpSummonReason & (uint)CardReason.Link) > 0)
                    textReason = InterString.Get("连接召唤");
                else if ((lastSpSummonReason & (uint)CardReason.Pendulum) > 0)
                    textReason = InterString.Get("灵摆召唤");
                else
                    textReason = InterString.Get("特殊召唤");
            }
            else if (OcgCore.currentMessage == GameMessage.FlipSummoning)
                textReason = InterString.Get("反转召唤");
            else
                textReason = InterString.Get("送至");

            if (data.HasType(CardType.Token))
                DuelLog.AddSingleCardMessageToLog(code, null, gps, textReason);
            else
                DuelLog.AddSingleCardMessageToLog(code, card.p, gps, textReason);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_SpSummoning(BinaryReader reader)
        {
            return GameMessage_Summoning(reader);
        }

        protected override UniTask GameMessage_FlipSummoning(BinaryReader reader)
        {
            return GameMessage_Summoning(reader);
        }

        protected override UniTask GameMessage_Set(BinaryReader reader)
        {
            var card = lastMoveCard;
            if(card == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }

            string textReason = InterString.Get("盖放");
            DuelLog.AddSingleCardMessageToLog(card.GetData().Id, card.cacheP, card.p, textReason);
            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Chaining(BinaryReader reader)
        {
            var code = (int)reader.ReadUInt32();
            var gps = reader.ReadGPS();
            var card = Core.GCS_Get(gps);

            if(card == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }

            var data = card.GetData();
            cardsInChain.Add(card);

            if(cardsInChain.Count > 1)
            {
                var item = ABLoader.LoadMasterDuelGameObject("DuelLogChaining");
                item.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() =>
                {
                    Core.GetUI<OcgCoreUI>().CardDescription.Show(card, null);
                });
                item.transform.GetChild(1).GetChild(0).GetComponent<Text>().text = cardsInChain.Count.ToString();
                item.transform.GetChild(1).GetChild(0).GetComponent<Text>().color =
                    card.p.controller == 0 ? DuelLog.myChainColor : DuelLog.opChainColor;
                item.transform.GetChild(2).GetComponent<Text>().text = InterString.Get("连锁");
                _ = Program.instance.texture_.LoadCardToRawImageWithoutMaterialAsync(
                    item.transform.GetChild(3).GetComponent<RawImage>(), code);
                DuelLog.ConfigureChainingItem(item);
                DuelLog.AddLog(item);
            }
            
            var textReason = InterString.Get("发动");
            if(card.cacheP != null 
                && card.cacheP.InLocation(CardLocation.Hand)
                && gps.InLocation(CardLocation.SpellZone)
                && gps.InPosition(CardPosition.FaceUp)
                && data.HasType(CardType.Pendulum)
                && gps.InPendulumSequence()
                && card == lastMoveCard
                && card != lastConfirmedCard)
                textReason = InterString.Get("灵摆发动");

            if(card == lastMoveCard
                && card != lastConfirmedCard
                && gps.InLocation(CardLocation.SpellZone)
                && card.cacheP.InLocation(CardLocation.Hand))
            {
                lastConfirmedCard = card;
                DuelLog.AddSingleCardMessageToLog(code, card.cacheP, gps, textReason);
            }
            else
                DuelLog.AddSingleCardMessageToLog(code, null, gps, textReason);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_ChainNegated(BinaryReader reader)
        {
            var index = reader.ReadByte();
            if(index > cardsInChain.Count)
            {
                LogDebug("[Duel Log]: Chain index overflow.");
                return UniTask.CompletedTask;
            }

            var card = cardsInChain[index - 1];
            if(card == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }

            var textReason = InterString.Get("发动无效");
            if (OcgCore.currentMessage == GameMessage.ChainDisabled)
            {
                textReason = InterString.Get("效果无效");
                if(card.negated)
                    return UniTask.CompletedTask;
            }

            DuelLog.AddSingleCardMessageToLog(card.GetData().Id, null, card.p, textReason);
            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_ChainDisabled(BinaryReader reader)
        {
            return GameMessage_ChainNegated(reader);
        }

        protected override UniTask GameMessage_Draw(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var count = reader.ReadByte();
            var gps = new GPS()
            {
                location = (uint)CardLocation.Hand,
                controller = (uint)player
            };

            var codes = new List<int>();
            for (int i = 0; i < count; i++)
            {
                var code = reader.ReadInt32() & 0x7fffffff;
                codes.Add(code);
            }

            var textReason = InterString.Get("抽卡");
            if (turns == 0 && count > 1)
            {
                DuelLog.AddOpeningDrawMessageToLog(codes, gps, textReason);
                return UniTask.CompletedTask;
            }
            
            var allUnknow = codes.All(x => x == 0);
            if (allUnknow)
            {
                DuelLog.AddSingleCardMessageToLog(0, null, gps, textReason + " x " + count);
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var code = codes[i];
                    DuelLog.AddSingleCardMessageToLog(code, null, gps, textReason);
                }
            }

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_RandomSelected(BinaryReader reader)
        {
            //var player = LocalPlayer(reader.ReadByte());
            reader.ReadByte();

            var count = reader.ReadByte();
            var item = ABLoader.LoadMasterDuelGameObject(cardsInChain.Count > 0 ? "DuelLogText2" : "DuelLogText");

            item.transform.GetChild(1).GetComponent<Text>().text = InterString.Get("对象");
            DuelLog.AddLog(item);
            for(int i = 0; i < count; i++)
            {
                var tempGPS = reader.ReadGPS();
                var card = Core.GCS_Get(tempGPS);
                if(card == null)
                {
                    DebugNoCard();
                    continue;
                }
                cardsBeTarget.Add(card);
                DuelLog.AddSingleCardMessageToLog(card.GetData().Id, null, tempGPS, string.Empty, true);
            }

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_BecomeTarget(BinaryReader reader)
        {
            var count = reader.ReadByte();
            var tempList = new List<GameCard>();

            for(int i = 0; i < count; i++)
            {
                var gps = reader.ReadGPS();
                var card = Core.GCS_Get(gps);

                if(card == null)
                {
                    DebugNoCard();
                    continue;
                }
                tempList.Add(card);
                cardsBeTarget.Add(card);

                if (duelPhase == DuelPhase.Main1 || duelPhase == DuelPhase.Main2)
                    if (cardsInChain.Count == 0)
                        if (cardsBeTarget.Count == 2)
                            if (cardsBeTarget[0].InPendulumZone())
                                if (cardsBeTarget[1].InPendulumZone())
                                    if (cardsBeTarget[0].p.controller == cardsBeTarget[1].p.controller)
                                        inPendulumSummon = true;
            }

            if (cardsInChain.Count == 0
                && cardsBeTarget.Count == 1
                && cardsBeTarget[0].InPendulumZone())
                return UniTask.CompletedTask;
            if(inPendulumSummon)
                return UniTask.CompletedTask;

            var item = ABLoader.LoadMasterDuelGameObject(cardsInChain.Count > 0 ? "DuelLogText2" : "DuelLogText");
            item.transform.GetChild(1).GetComponent<Text>().text = InterString.Get("对象");
            DuelLog.AddLog(item);
            for (int i = 0; i < tempList.Count; i++)
                DuelLog.AddSingleCardMessageToLog(tempList[i].GetData().Id, null, tempList[i].p, string.Empty, true);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_AttackDisabled(BinaryReader reader)
        {
            if(attackingCard == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }
            var textReason = InterString.Get("攻击被无效");
            DuelLog.AddSingleCardMessageToLog(attackingCard.GetData().Id, null, attackingCard.p, textReason);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_ConfirmDecktop(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte()); 
            var count = reader.ReadByte();

            for (int i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var textReason = InterString.Get("公开卡组");
                DuelLog.AddSingleCardMessageToLog(code, null, gps, textReason);
            }

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_ConfirmCards(BinaryReader reader)
        {
            return GameMessage_ConfirmCardsForTry(reader);
        }

        private UniTask GameMessage_ConfirmCardsForTry(BinaryReader reader)
        {
            reader.BaseStream.Position = 0;

            //var player = LocalPlayer(reader.ReadByte());
            reader.ReadByte();

            if ((reader.BaseStream.Length-reader.BaseStream.Position)%7==2)
                reader.ReadByte();

            var count = reader.ReadByte();
            if (reader.BaseStream.Length-reader.BaseStream.Position != count*7)
                throw new InvalidDataException("Invalid card-confirmation packet length.");
            for (int i = 0; i < count; i++)
            {
                var code = reader.ReadInt32();
                var gps = reader.ReadShortGPS();
                var textReason = InterString.Get("公开");
                if (gps.InLocation(CardLocation.Hand))
                    textReason = InterString.Get("公开手卡");
                else if (gps.InLocation(CardLocation.Onfield))
                    textReason = InterString.Get("公开盖卡");
                DuelLog.AddSingleCardMessageToLog(code, null, gps, textReason);
            }
            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_PosChange(BinaryReader reader)
        {
            var code = reader.ReadInt32();
            var from = reader.ReadGPS();

            var textReason = InterString.Get("更改表示形式");
            if(from.InLocation(CardLocation.SpellZone)
                && from.InPosition(CardPosition.FaceUp))
                textReason = InterString.Get("盖放");
            if (from.InLocation(CardLocation.SpellZone)
                && from.InPosition(CardPosition.FaceDown))
                textReason = InterString.Get("盖放");

            if(code == 0)
            {
                var card = Core.GCS_Get(from);
                if(card != null)
                    code = card.GetData().Id;
            }

            var to = from;
            to.position = reader.ReadByte();
            DuelLog.AddSingleCardMessageToLog(code, null, to, textReason);
            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Swap(BinaryReader reader)
        {
            var code = reader.ReadInt32();
            var from = reader.ReadGPS();
            var code2 = reader.ReadInt32();
            var to = reader.ReadGPS();
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

            var textReason = InterString.Get(from.controller == to.controller ? "移动" : "转移控制权");
            DuelLog.AddSingleCardMessageToLog(code, from, to2, textReason);
            DuelLog.AddSingleCardMessageToLog(code2, to, from2, textReason);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_ChainSolving(BinaryReader reader)
        {
            chainSolvingIndex = reader.ReadByte();
            if(chainSolvingIndex > cardsInChain.Count)
            {
                LogDebug("[Duel Log]: Chain index overflow.");
                return UniTask.CompletedTask;
            }
            var card = cardsInChain[chainSolvingIndex - 1];
            if(card == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }

            var item = ABLoader.LoadMasterDuelGameObject("DuelLogChaining");
            item.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() =>
            {
                Core.GetUI<OcgCoreUI>().CardDescription.Show(card, null);
            });
            item.transform.GetChild(1).GetChild(0).GetComponent<Text>().text = chainSolvingIndex.ToString();
            item.transform.GetChild(1).GetChild(0).GetComponent<Text>().color =
                card.p.controller == 0 ? DuelLog.myChainColor : DuelLog.opChainColor;
            item.transform.GetChild(2).GetComponent<Text>().text = InterString.Get("效果处理");
            _ = Program.instance.texture_.LoadCardToRawImageWithoutMaterialAsync(
                item.transform.GetChild(3).GetComponent<RawImage>(), card.GetData().Id);
            DuelLog.ConfigureChainingItem(item);
            DuelLog.AddLog(item);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_ChainEnd(BinaryReader reader)
        {
            cardsInChain.Clear();
            cardsBeTarget.Clear();

            var item = ABLoader.LoadMasterDuelGameObject("DuelLogChaining");
            item.transform.GetChild(1).gameObject.SetActive(false);
            item.transform.GetChild(3).gameObject.SetActive(false);
            var textReason = InterString.Get(chainSolvingIndex > 1 ? "连锁结束" : "处理结束");
            item.transform.GetChild(2).GetComponent<Text>().text = textReason;
            chainSolvingIndex = 0;
            DuelLog.ConfigureChainingItem(item);
            DuelLog.AddLog(item);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Attack(BinaryReader reader)
        {
            var from = reader.ReadGPS();
            var to = reader.ReadGPS();
            attackingCard = Core.GCS_Get(from);
            if(attackingCard == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }
            var code = attackingCard.GetData().Id;

            var item = ABLoader.LoadMasterDuelGameObject("DuelLogAttack");
            var targetColor = from.InMyControl() ? DuelLog.myColor : DuelLog.opColor;
            targetColor.a = 0.75f;
            item.transform.GetChild(1).GetComponent<Image>().color = targetColor;
            item.transform.GetChild(2).GetComponent<Text>().text = InterString.Get("攻击");
            var cardFace1 = item.transform.GetChild(3).GetComponent<RawImage>();
            _ = Program.instance.texture_.LoadCardToRawImageWithoutMaterialAsync(cardFace1, code, true);
            if(from.InPosition(CardPosition.Defence))
                cardFace1.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
            cardFace1.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(() =>
            {
                Core.GetUI<OcgCoreUI>().CardDescription.Show(null, null, code, from);
            });
            var icons = TextureManager.container.GetLocationIcons(from);
            item.transform.GetChild(4).GetComponent<Image>().sprite = icons[0];
            targetColor = from.InMyControl() ? DuelLog.myArrowColor : DuelLog.opArrowColor;
            item.transform.GetChild(5).GetComponent<Image>().color = targetColor;

            var attackedCard = Core.GCS_Get(to);
            if (attackedCard == null)
            {
                item.transform.GetChild(6).gameObject.SetActive(false);
                item.transform.GetChild(7).gameObject.SetActive(false);
                item.transform.GetChild(8).GetComponent<Image>().material =
                    from.InMyControl()
                    ? Core.GetUI<OcgCoreUI>().AvatarPlayer1.material
                    : Core.GetUI<OcgCoreUI>().AvatarPlayer0.material;
                item.transform.GetChild(8).GetComponent<Image>().sprite =
                    from.InMyControl()
                    ? Core.GetUI<OcgCoreUI>().AvatarPlayer1.sprite
                    : Core.GetUI<OcgCoreUI>().AvatarPlayer0.sprite;
            }
            else
            {
                item.transform.GetChild(8).gameObject.SetActive(false);
                icons = TextureManager.container.GetLocationIcons(to);
                item.transform.GetChild(6).GetComponent<Image>().sprite = icons[0];
                var cardFace2 = item.transform.GetChild(7).GetComponent<RawImage>();
                var code2 = attackedCard.GetData().Id;
                if (code2 > 0)
                    _ = Program.instance.texture_.LoadCardToRawImageWithoutMaterialAsync(cardFace2, code2, true);
                else
                {
                    cardFace2.texture = null;
                    cardFace2.material = from.InMyControl() ? OcgCore.opProtector : OcgCore.myProtector;
                    cardFace2.transform.GetChild(0).gameObject.SetActive(false);
                }
                if ((to.position & (uint)CardPosition.Defence) > 0)
                    cardFace2.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
                if ((to.position & (uint)CardPosition.FaceUp) > 0)
                    cardFace2.transform.GetChild(0).gameObject.SetActive(false);
                cardFace2.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(() =>
                {
                    Core.GetUI<OcgCoreUI>().CardDescription.Show(null, null, code2, to);
                });
            }
            DuelLog.AddLog(item);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_UpdateData(BinaryReader reader)
        {
            if (inPendulumSummon)
            {
                inPendulumSummon = false;
                cardsBeTarget.Clear();
                
                var item = ABLoader.LoadMasterDuelGameObject(chainSolvingIndex > 0 ? "DuelLogText2" : "DuelLogText");
                item.transform.GetChild(1).GetComponent<Text>().text = InterString.Get("灵摆召唤结束");
                DuelLog.AddLog(item);
            }

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Hint(BinaryReader reader)
        {
            var type = reader.ReadChar();
            var player = LocalPlayer(reader.ReadChar());
            var data = reader.ReadInt32();

            string hint = string.Empty;
            GameObject item = null;

            if (type == 8
                || type == 10)
            {
                item = ABLoader.LoadMasterDuelGameObject(cardsInChain.Count > 0 ? "DuelLogTextWithCard2" : "DuelLogTextWithCard");
                if (type == 8)
                    hint = InterString.Get("宣言卡片：[?]", CardsManager.Get(data).Name);
                else if (type == 10)
                    hint = InterString.Get("效果适用：[?]", CardsManager.Get(data).Name);
            }
            else if(type >= 2
                && type <= 11
                && type != 3)
            {
                item = ABLoader.LoadMasterDuelGameObject(cardsInChain.Count > 0 ? "DuelLogText2" : "DuelLogText");

                if (type == 2)
                    hint = StringHelper.Get(data);
                else if (type == 4)
                    hint = InterString.Get("效果选择：[?]", StringHelper.Get(data));
                else if (type == 5)
                    hint = StringHelper.Get(data);
                else if (type == 6)
                    hint = InterString.Get("种族选择：[?]", StringHelper.Race(data));
                else if (type == 7)
                    hint = InterString.Get("属性选择：[?]", StringHelper.Attribute(data));
                else if (type == 9)
                    hint = InterString.Get("数字选择：[?]", data.ToString());
                else if (type == 11)
                {
                    if (player == 1)
                        data = (data >> 16) | (data << 16);
                    hint = InterString.Get("区域选择：[?]", StringHelper.Zone(data));
                }
            }

            if (item != null)
            {
                item.transform.GetChild(1).GetComponent<Text>().text = hint;
                if (item.transform.childCount > 2)
                {
                    var cardFace = item.transform.GetChild(2).GetComponent<RawImage>();
                    _ = Program.instance.texture_.LoadCardToRawImageWithoutMaterialAsync(cardFace, data, true);
                    item.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() =>
                    {
                        Core.GetUI<OcgCoreUI>().CardDescription.Show(null, null, data, new GPS());
                    });
                }
                DuelLog.AddLog(item);
            }

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_AddCounter(BinaryReader reader)
        {
            var type = reader.ReadUInt16();
            var gps = reader.ReadShortGPS();
            var card = Core.GCS_Get(gps);
            var count = reader.ReadInt16();

            if(card == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }

            var counterBefore = card.GetCounterCount(type);
            var counterNow = counterBefore + count;
            if (OcgCore.currentMessage == GameMessage.RemoveCounter)
                counterNow = counterBefore - count;

            var item = ABLoader.LoadMasterDuelGameObject("DuelLogCounter");
            var targetColor = gps.InMyControl() ? DuelLog.myColor : DuelLog.opColor;
            item.transform.GetChild(1).GetComponent<Image>().color = targetColor;
            item.transform.GetChild(2).GetComponent<Text>().text = card.GetData().Name;
            var cardFace = item.transform.GetChild(3).GetComponent<RawImage>();
            _ = Program.instance.texture_.LoadCardToRawImageWithoutMaterialAsync(cardFace, card.GetData().Id, true);
            cardFace.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() =>
            {
                Core.GetUI<OcgCoreUI>().CardDescription.Show(null, null, card.GetData().Id, gps);
            });
            var icons = TextureManager.container.GetLocationIcons(gps);
            if (icons.Count == 2)
            {
                item.transform.GetChild(4).GetComponent<Image>().sprite = icons[1];
                item.transform.GetChild(4).GetChild(0).GetComponent<Image>().sprite = icons[0];
            }
            else
            {
                item.transform.GetChild(4).GetComponent<Image>().sprite = icons[0];
                item.transform.GetChild(4).GetChild(0).gameObject.SetActive(false);
            }
            var textCounterTo = item.transform.GetChild(5).GetComponent<Text>();
            textCounterTo.text = counterNow.ToString();
            var textCounterFrom = textCounterTo.transform.GetChild(0).GetChild(0).GetComponent<Text>();
            textCounterFrom.text = counterBefore.ToString();
            textCounterFrom.transform.GetChild(0).GetComponent<Image>().sprite =
                TextureManager.GetCardCounterIcon(type);
            item.transform.GetChild(6).GetComponent<Text>().text = StringHelper.Get("counter", type);
            DuelLog.AddLog(item);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_RemoveCounter(BinaryReader reader)
        {
            return GameMessage_AddCounter(reader);
        }

        protected override UniTask GameMessage_Damage(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var value = reader.ReadInt32();
            var textReason = InterString.Get("伤害");

            if(player == 0)
                life0 -= value;
            else
                life1 -= value;
            DuelLog.AddLpPChangeMessageToLog(player, textReason, value);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_PayLpCost(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var value = reader.ReadInt32();
            var textReason = InterString.Get("代价");
            if (player == 0)
                life0 -= value;
            else
                life1 -= value;
            DuelLog.AddLpPChangeMessageToLog(player, textReason, value, true, true);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Recover(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var value = reader.ReadInt32();
            var textReason = InterString.Get("回复");
            if (player == 0)
                life0 += value;
            else
                life1 += value;
            DuelLog.AddLpPChangeMessageToLog(player, textReason, value, false);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_LpUpdate(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var value = reader.ReadInt32();
            var textReason = InterString.Get("基本分改变");

            var diff = player == 0 ? value - life0 : value - life1;
            if(player == 0)
                life0 = value;
            else
                life1 = value;
            DuelLog.AddLpPChangeMessageToLog(player, textReason, Mathf.Abs(diff), diff < 0);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_TossCoin(BinaryReader reader)
        {
            //TODO
            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_TossDice(BinaryReader reader)
        {
            //TODO
            return UniTask.CompletedTask;
        }

        #endregion

    }
}
