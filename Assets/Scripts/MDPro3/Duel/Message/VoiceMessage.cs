using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static MDPro3.Duel.VoicePlayer;
using static MDPro3.VoiceController;

namespace MDPro3.Duel
{
    public class VoiceMessage : MessageProcessor
    {
        public VoiceMessage(MessageDispatcher dispatcher) : base(dispatcher) { }

        #region Parameters

        private bool isFirst;
        private int MasterRule;
        private int life0;
        private int life1;
        private int turns;
        private bool myTurn;
        private DuelPhase duelPhase;

        private readonly List<GameCard> cardsInChain = new();
        private readonly List<GameCard> cardsBeTarget = new();

        private bool ignoreNextChaining;
        private bool inPendulumSummon;
        private bool ignoreNextPendulumSummon;
        private bool lastVoiceIsRelease;

        private readonly List<VoiceData> voiceData = new();

        #endregion

        #region Tools

        private void LogDebug(string text)
        {
            Program.Debug(text);
        }

        private void DebugNoCard()
        {
            LogDebug($"[Duel Voice]: Not found card for {OcgCore.currentMessage}.");
        }

        private void ResetState()
        {
            ignoreNextChaining = false;
            inPendulumSummon = false;
            ignoreNextPendulumSummon = false;
            lastVoiceIsRelease = false;

            cardsInChain.Clear();
            cardsBeTarget.Clear();
        }

        private async UniTask PlayVoiceAsync()
        {
            var paths = GetVoicePaths(voiceData);
            var clips = new List<AudioClip>[paths.Length];
            for (int i = 0; i < clips.Length; i++)
                clips[i] = new List<AudioClip>();

            for (int i = 0; i < paths.Length; i++)
            {
                for (int j = 0; j < paths[i].Count; j++)
                {
                    try
                    {
                        var clip = await AudioManager.LoadAudioFileUniAsync(paths[i][j], AudioType.OGGVORBIS);
                        clips[i].Add(clip);
                    }
                    catch (Exception ex) 
                    {
                        Debug.LogException(ex);
                    }
                }
            }

            for (int i = 0; i < clips.Length; i++)
            {
                for (int j = 0; j < clips[i].Count; j++)
                {
                    if (j == 0)
                        await UniTask.WaitForSeconds(voiceData[i].delay);

                    var line = GetLine(Path.GetFileNameWithoutExtension(paths[i][j]), voiceData[i].isHero);
                    if (line != null)
                    {
                        var item = ABLoader.LoadMasterDuelGameObject(voiceData[i].isHero ? "DuelChatItemMe" : "DuelChatItemOp");

                        item.transform.SetParent(Core.transform.GetChild(0), false);
                        var handler = item.GetComponent<ChatItemHandler>();
                        handler.text = line.text;

                        if (clips[i][j] == null)
                        {
                            Debug.LogError("Voice File " + paths[i][j] + " not Found!");
                            return;
                        }

                        handler.time = clips[i][j].length;
                        handler.frame = line.frame;
                        if (voiceData[i].isHero)
                        {
                            if (Core.duelChat0 != null)
                                Core.duelChat0.BeGray();
                            Core.duelChat0 = handler;
                        }
                        else
                        {
                            if (Core.duelChat1 != null)
                                Core.duelChat1.BeGray();
                            Core.duelChat1 = handler;
                        }

                        Core.SetCharacterFace(voiceData[i].isHero ? heroString : rivalString, line.face, voiceData[i].isHero, 0f);
                        Core.SetCharacterFace(voiceData[i].isHero ? heroString : rivalString, 1, voiceData[i].isHero, clips[i][j].length - 0.1f);
                    }

                    AudioManager.PlayVoice(clips[i][j]);

                    if (voiceData[i].wait)
                        await UniTask.WaitForSeconds(clips[i][j].length);
                }
            }
        }

        private bool NeedVoice()
        {
            return Config.GetBool(OcgCore.condition + "Voice", false);
        }

        private static bool DamageIsBig(int damage)
        {
            if (damage >= 2000)
                return true;
            else
                return false;
        }

        private int LocalPlayer(int player)
        {
            if (player == 0 || player == 1)
            {
                if (isFirst)
                    return player;
                return 1 - player;
            }
            return player;
        }

        private static VoicesData GetVoiceSet(bool isHero)
        {
            return isHero ? heroVoices : rivalVoices;
        }

        private int GetLeadingState(bool isHero)
        {
            return isHero ? LeadingStateOfHero() : LeadingStateOfRival();
        }

        #endregion

        #region Message Process

        public override async UniTask Process(Package p)
        {
            if (NeedVoice())
            {
                if (!Core.charaFaceSetting)
                    Core.SetCharacterDefaultFace();
                LoadData();
            }
            else
            {
                Core.CloseCharaFace();
                return;
            }

            voiceData.Clear();

            await base.Process(p);

            if (voiceData.Count == 0)
                return;

            var voiceTask = PlayVoiceAsync();
            var clickTask = UniTask.WaitUntil(() => UserInput.MouseLeftDown);

            await UniTask.WhenAny(voiceTask, clickTask);
        }

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

            VoiceData data = new();
            data.name = GetVoiceByDuelist(heroVoices.BeforeDuel, heroVoices.BeforeDuelSp, rivalCode);
            data.num = GetVoiceNum(heroVoices, data.name);
            data.isHero = true;
            data.wait = true;
            data.delay = 0f;
            voiceData.Add(data);

            VoiceData data2 = new();
            data2.name = GetVoiceByDuelist(rivalVoices.BeforeDuel, rivalVoices.BeforeDuelSp, heroCode);
            data2.num = GetVoiceNum(rivalVoices, data2.name);
            data2.isHero = false;
            data2.wait = true;
            data2.delay = 0f;
            voiceData.Add(data2);

            VoiceData data3 = new();
            data3.name = Tools.GetRandomDictionaryElement(heroVoices.DuelStart.rawKvp).Value.shortName;
            data3.num = GetVoiceNum(heroVoices, data3.name);
            data3.isHero = true;
            data3.wait = false;
            data3.delay = 1f;
            voiceData.Add(data3);

            VoiceData data4 = new();
            data4.name = Tools.GetRandomDictionaryElement(rivalVoices.DuelStart.rawKvp).Value.shortName;
            data4.num = GetVoiceNum(rivalVoices, data4.name);
            data4.isHero = false;
            data4.wait = true;
            data4.delay = 0f;
            voiceData.Add(data4);

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

            if (turns == 1)
                return UniTask.CompletedTask;

            var targetData = GetVoiceSet(myTurn);
            var leadingState = GetLeadingState(myTurn);

            var data = new VoiceData();
            data.name = GetVoiceBySituation(targetData.TurnStart, leadingState);
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = myTurn;
            data.wait = true;
            data.delay = 0f;
            voiceData.Add(data);
            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_NewPhase(BinaryReader reader)
        {
            duelPhase = (DuelPhase)reader.ReadUInt16();
            if(duelPhase != DuelPhase.BattleStart && duelPhase != DuelPhase.End)
                return UniTask.CompletedTask;

            var targetData = GetVoiceSet(myTurn);
            var data = new VoiceData();
            if(duelPhase == DuelPhase.BattleStart)
                data.name = Tools.GetRandomDictionaryElement(targetData.BattleStart.rawKvp).Value.shortName;
            else if(duelPhase == DuelPhase.End)
            {
                var leadingState = GetLeadingState(myTurn);
                data.name = GetVoiceBySituation(targetData.TurnEnd, leadingState);
            }
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = myTurn;
            data.wait = true;
            data.delay = 0f;
            voiceData.Add(data);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Win(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            if(player == 2)
                return UniTask.CompletedTask;

            VoicesData targetData;
            VoicesData targetData2;

            if (player == 0)
            {
                targetData = heroVoices;
                targetData2 = rivalVoices;
            }
            else
            {
                targetData = rivalVoices;
                targetData2 = heroVoices;
            }

            var data = new VoiceData();
            data.name = GetVoiceByDuelist(targetData.Win, targetData.WinSp, player == 0 ? rivalCode : heroCode);
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = player == 0;
            data.wait = true;
            data.delay = 0f;
            voiceData.Add(data);

            var data2 = new VoiceData();
            data2.name = GetVoiceByDuelist(targetData2.Lose, targetData2.LoseSp, player == 0 ? heroCode : rivalCode);
            data2.num = GetVoiceNum(targetData2, data2.name);
            data2.isHero = !data.isHero;
            data2.wait = true;
            data2.delay = 0f;
            voiceData.Add(data2);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_PosChange(BinaryReader reader)
        {
            var nextPack = OcgCore.GetNextPackage();
            if(nextPack == null)
                return UniTask.CompletedTask;
            //仅检测盖卡打开
            if ((GameMessage)nextPack.Function != GameMessage.Chaining)
                return UniTask.CompletedTask;

            /*var code = */reader.ReadUInt32();
            var gps = reader.ReadGPS();

            var target = gps.InMyControl() ? heroVoices : rivalVoices;
            if (NeedBeforeCardEffect(gps.InMyControl()))
                voiceData.Add(GetBeforeCardEffectData(target, gps.InMyControl()));

            var data = new VoiceData();
            data.name = GetVoiceBySubCategory(target.CardEffect, (int)CardEffectSub.Reverse, (int)CardEffectSub.Reverse, 0);
            data.num = GetVoiceNum(target, data.name);
            data.isHero = gps.InMyControl();
            data.wait = true;
            data.delay = 0f;
            voiceData.Add(data);

            var simple = SimpleVoiceData.GetCardEffectSubCategory(nextPack.Data.reader, false);
            var data2 = new VoiceData();
            data2.name = GetVoiceBySubCategory(target.CardEffect, simple.subCategory, (int)CardEffectSub.Magic, 0);
            data2.num = GetVoiceNum(target, data2.name);
            data2.isHero = data.isHero;
            data2.wait = true;
            data2.delay = 0f;
            voiceData.Add(data2);

            ignoreNextChaining = true;

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Move(BinaryReader reader)
        {
            var code = (int)reader.ReadUInt32();
            var from = reader.ReadGPS();
            var to = reader.ReadGPS();
            var reason = reader.ReadUInt32();
            var card = Core.GCS_Get(from);
            if(card == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }

            var cardData = card.GetData();
            var nextPack = OcgCore.GetNextPackage();
            if(nextPack == null)
                return UniTask.CompletedTask;
            nextPack.Data.reader.BaseStream.Seek(0, 0);

            var category = 0;
            var subCategory = 0;
            var subInCase = 0;
            var patternIndex = 0;
            var fromHand = false;
            var isMe = to.InMyControl();

            if ((reason & (uint)CardReason.RELEASE) > 0
                && card.GetData().HasType(CardType.Monster))
            {
                if (lastVoiceIsRelease)
                    return UniTask.CompletedTask;
                category = (int)Category.Summon;
                subCategory = (int)SummonSub.Release;
                subInCase = subCategory;
                lastVoiceIsRelease = true;
            }
            else
                lastVoiceIsRelease = false;

            if ((GameMessage)nextPack.Function == GameMessage.Summoning)
            {
                code = nextPack.Data.reader.ReadInt32();
                category = (int)Category.Summon;
                subCategory = (int)SummonSub.Normal;

                isMe = from.InMyControl();
                var targetDataT = GetVoiceSet(isMe);
                var data = GetVoiceByCard(targetDataT, targetDataT.MainMonsterSummon, code, 0, isMe);
                if (data.name != string.Empty)
                {
                    voiceData.Add(data);
                    return UniTask.CompletedTask;
                }
                voiceData.Add(GetBeforeSummonData(targetDataT, isMe));
            }

            if ((GameMessage)nextPack.Function == GameMessage.SpSummoning)
            {
                code = nextPack.Data.reader.ReadInt32();
                cardData = CardsManager.Get(code);

                category = (int)Category.Summon;
                subCategory = (int)SummonSub.Special;
                subInCase = subCategory;
                if (OcgCore.materialCards.Count > 0)
                {
                    patternIndex = -1;

                    if (cardData.HasType(CardType.Link))
                        subCategory = (int)SummonSub.Link;
                    else if (cardData.HasType(CardType.Fusion))
                        subCategory = (int)SummonSub.Fusion;
                    else if (cardData.HasType(CardType.Synchro))
                        subCategory = (int)SummonSub.Sync;
                    else if (cardData.HasType(CardType.Xyz))
                        subCategory = (int)SummonSub.Xyz;
                    else if (cardData.HasType(CardType.Ritual))
                        subCategory = (int)SummonSub.Ritual;
                }
                else if (inPendulumSummon)
                {
                    if (ignoreNextPendulumSummon)
                        return UniTask.CompletedTask;
                    subCategory = (int)SummonSub.Pendulum;
                }

                if (from.InLocation(CardLocation.Hand)
                    && subCategory == (int)SummonSub.Special)
                {
                    fromHand = true;
                    isMe = from.InMyControl();
                }

                var targetDataT = GetVoiceSet(isMe);

                if (subCategory != (int)SummonSub.Special)
                {
                    var data = GetVoiceByCard(targetDataT, targetDataT.BeforeMainSummon, code, 0, isMe);
                    if (data.name != string.Empty)
                        voiceData.Add(data);
                }

                var dataT = GetVoiceByCard(targetDataT, targetDataT.MainMonsterSummon, code, 0, isMe);
                if (dataT.name != string.Empty)
                {
                    if (subCategory != (int)SummonSub.Special && voiceData.Count == 0)
                    {
                        var voiceShortName = GetVoiceBySubCategory(targetDataT.Summon, subCategory, (int)SummonSub.Special, 1, true);
                        if (voiceShortName != null)
                        {
                            var data = new VoiceData();
                            data.name = voiceShortName;
                            data.num = VoicePlayer.GetVoiceNum(targetDataT, data.name);
                            data.isHero = isMe;
                            data.wait = true;
                            data.delay = 0f;
                            voiceData.Add(data);
                        }
                    }
                    voiceData.Add(dataT);
                    return UniTask.CompletedTask;
                }
            }

            if ((GameMessage)nextPack.Function == GameMessage.Set)
            {
                category = (int)Category.CardSet;
                if (to.InLocation(CardLocation.MonsterZone))
                    subCategory = (int)CardSetSub.Monster;
                else
                    subCategory = (int)CardSetSub.MagicTrap;
                subInCase = subCategory;
            }

            if ((GameMessage)nextPack.Function == GameMessage.Chaining)
            {
                if (!to.InLocation(CardLocation.Onfield))
                    return UniTask.CompletedTask;
                ignoreNextChaining = true;

                code = nextPack.Data.reader.ReadInt32();
                var gps = nextPack.Data.reader.ReadGPS();

                var targetDataT = GetVoiceSet(gps.InMyControl());
                var data = GetVoiceByCard(targetDataT, targetDataT.MainMonsterEffect, code, 0, gps.InMyControl());
                if (data.name != string.Empty)
                {
                    voiceData.Add(data);
                    return UniTask.CompletedTask;
                }
                data = GetVoiceByCard(targetDataT, targetDataT.MainMagicTrap, code, 0, gps.InMyControl());
                if (data.name != string.Empty)
                {
                    voiceData.Add(data);
                    return UniTask.CompletedTask;
                }

                if (from.InLocation(CardLocation.Hand))
                {
                    fromHand = true;
                    isMe = from.InMyControl();
                }

                if (NeedBeforeCardEffect(isMe))
                    voiceData.Add(GetBeforeCardEffectData(GetVoiceSet(isMe), isMe));

                var simple = SimpleVoiceData.GetCardEffectSubCategory(nextPack.Data.reader, fromHand);
                category = simple.category;
                subCategory = simple.subCategory;
                subInCase = (int)CardEffectSub.Magic;
                if (subCategory == (int)CardEffectSub.PendulumScale)
                    fromHand = false;
            }

            var targetData = GetVoiceSet(isMe);
            if(category == 0)
                return UniTask.CompletedTask;
            if (fromHand)
            {
                var data = new VoiceData();
                data.name = GetVoiceBySubCategory(targetData.CardEffect, (int)CardEffectSub.FromHand, (int)CardEffectSub.FromHand, 0);
                data.num = GetVoiceNum(targetData, data.name);
                data.isHero = isMe;
                data.wait = true;
                data.delay = 0f;
                voiceData.Add(data);
            }
            var data2 = new VoiceData();
            data2.name = GetVoiceBySubCategory(targetData.GetCategoryEntry((Category)category), subCategory, subInCase, patternIndex);
            data2.num = GetVoiceNum(targetData, data2.name);
            data2.isHero = isMe;
            data2.wait = true;
            data2.delay = 0f;
            voiceData.Add(data2);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Chaining(BinaryReader reader)
        {
            if (ignoreNextChaining)
            {
                ignoreNextChaining = false;
                return UniTask.CompletedTask;
            }

            var code = (int)reader.ReadUInt32();
            var gps = reader.ReadGPS();
            var card = Core.GCS_Get(gps);
            if(card == null)
            {
                DebugNoCard();
            }
            cardsInChain.Add(card);
            if (code == 0)
                code = card.GetData().Id;

            var simple = SimpleVoiceData.GetCardEffectSubCategory(reader, false);
            var targetData = simple.isMe ? heroVoices : rivalVoices;
            if (NeedBeforeCardEffect(simple.isMe))
                voiceData.Add(GetBeforeCardEffectData(targetData, simple.isMe));

            var data = GetVoiceByCard(targetData, targetData.MainMonsterEffect, code, 0, simple.isMe);
            if(data.name != string.Empty)
            {
                voiceData.Add(data);
                return UniTask.CompletedTask;
            }

            if (simple.inHand)
            {
                var data2 = new VoiceData();
                data2.name = GetVoiceBySubCategory(targetData.CardEffect, (int)CardEffectSub.FromHand, (int)CardEffectSub.FromHand, 0);
                data2.num = VoicePlayer.GetVoiceNum(targetData, data2.name);
                data2.isHero = simple.isMe;
                data2.wait = true;
                data2.delay = 0f;
                voiceData.Add(data2);
            }

            var data3 = new VoiceData();
            data3.name = GetVoiceBySubCategory(targetData.CardEffect, simple.subCategory, (int)CardEffectSub.Magic, 0);
            data3.num = GetVoiceNum(targetData, data3.name);
            data3.isHero = simple.isMe;
            data3.wait = true;
            data3.delay = 0f;
            voiceData.Add(data3);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Draw(BinaryReader reader)
        {
            if(duelPhase != DuelPhase.Draw)
                return UniTask.CompletedTask;
            if(turns == 0)
                return UniTask.CompletedTask;
            if(turns == 1 && MasterRule > 2)
                return UniTask.CompletedTask;

            var player = LocalPlayer(reader.ReadByte());
            var isHero = player == 0;
            var targetData = GetVoiceSet(isHero);
            var leadingState = GetLeadingState(isHero);

            var data = new VoiceData();
            data.name = GetVoiceBySituation(targetData.Draw, leadingState);
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = isHero;
            data.wait = true;
            data.delay = 0f;
            voiceData.Add(data);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Damage(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var value = reader.ReadInt32();
            var isHero = player == 0;
            var targetData = GetVoiceSet(isHero);
            var cacheLP = isHero ? life0 : life1;

            if (isHero)
                life0 -= value;
            else
                life1 -= value;

            if (value >= cacheLP)
            {
                voiceData.Add(GetFinishDamageVoiceData(targetData, isHero));
                return UniTask.CompletedTask;
            }

            var data = new VoiceData();
            if(DamageIsBig(value))
                data.name = Tools.GetRandomDictionaryElement(targetData.BigDamage.rawKvp).Value.shortName;
            else
                data.name = Tools.GetRandomDictionaryElement(targetData.Damage.rawKvp).Value.shortName;
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = isHero;
            data.wait = false;
            data.delay = 0f;
            voiceData.Add(data);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_PayLpCost(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var value = reader.ReadInt32();
            var isHero = player == 0;
            var targetData = GetVoiceSet(isHero);
            var cacheLP = isHero ? life0 : life1;
            if (isHero)
                life0 -= value;
            else
                life1 -= value;

            if (value >= cacheLP)
            {
                voiceData.Add(GetFinishDamageVoiceData(targetData, isHero));
                return UniTask.CompletedTask;
            }

            var data = new VoiceData();
            data.name = Tools.GetRandomDictionaryElement(targetData.CostDamage.rawKvp).Value.shortName;
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = isHero;
            data.wait = false;
            data.delay = 0f;
            voiceData.Add(data);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Recover(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var value = reader.ReadInt32();
            if (player == 0)
                life0 += value;
            else
                life1 += value;

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_LpUpdate(BinaryReader reader)
        {
            var player = LocalPlayer(reader.ReadByte());
            var value = reader.ReadInt32();
            var isHero = player == 0;
            var targetData = GetVoiceSet(isHero);

            var diff = (isHero ? life0 : life1) - value;
            if (isHero)
                life0 = value;
            else
                life1 = value;

            if (value == 0)
            {
                voiceData.Add(GetFinishDamageVoiceData(targetData, isHero));
                return UniTask.CompletedTask;
            }

            if (diff <= 0)
                return UniTask.CompletedTask;

            var data = new VoiceData();
            if (DamageIsBig(diff))
                data.name = Tools.GetRandomDictionaryElement(targetData.BigDamage.rawKvp).Value.shortName;
            else
                data.name = Tools.GetRandomDictionaryElement(targetData.Damage.rawKvp).Value.shortName;
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = isHero;
            data.wait = false;
            data.delay = 0f;
            voiceData.Add(data);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_Attack(BinaryReader reader)
        {
            var from = reader.ReadGPS();
            var to = reader.ReadGPS();

            var attackCard = Core.GCS_Get(from);
            if(attackCard == null)
            {
                DebugNoCard();
                return UniTask.CompletedTask;
            }

            bool directAttack = true;
            var value = attackCard.GetData().Attack;
            var attackedCard = Core.GCS_Get(to);

            if(attackedCard != null)
            {
                directAttack = false;
                if (attackedCard.p.InPosition(CardPosition.Attack))
                    value = attackCard.GetData().Attack - attackedCard.GetData().Attack;
                else
                    value = 0;
            }

            bool finalBlow = value >= (from.InMyControl() ? life1 : life0);
            var isHero = from.InMyControl();
            var targetData = GetVoiceSet(isHero);

            var data = new VoiceData();
            data.name = Tools.GetRandomDictionaryElement(finalBlow ? targetData.BeforeAttackFinish.rawKvp : targetData.BeforeAttackNormal.rawKvp).Value.shortName;
            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = isHero;
            data.wait = true;
            data.delay = 0f;
            voiceData.Add(data);

            var data2 = new VoiceData();
            data2.name = Tools.GetRandomDictionaryElement(directAttack ? targetData.DirectAttack.rawKvp : targetData.Attack.rawKvp).Value.shortName;
            data2.num = GetVoiceNum(targetData, data2.name);
            data2.isHero = data.isHero;
            data2.wait = true;
            data2.delay = 0f;
            voiceData.Add(data2);

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_BecomeTarget(BinaryReader reader)
        {
            var count = reader.ReadByte();

            for (int i = 0; i < count; i++)
            {
                var gps = reader.ReadGPS();
                var card = Core.GCS_Get(gps);

                if (card == null)
                {
                    DebugNoCard();
                    continue;
                }
                cardsBeTarget.Add(card);

                if (duelPhase == DuelPhase.Main1 || duelPhase == DuelPhase.Main2)
                    if (cardsInChain.Count == 0)
                        if (cardsBeTarget.Count == 2)
                            if (cardsBeTarget[0].InPendulumZone())
                                if (cardsBeTarget[1].InPendulumZone())
                                    if (cardsBeTarget[0].p.controller == cardsBeTarget[1].p.controller)
                                        inPendulumSummon = true;
            }

            if(!inPendulumSummon)
                return UniTask.CompletedTask;

            var targetData = myTurn ? heroVoices : rivalVoices;

            var data = new VoiceData();
            data.name = GetVoiceBySubCategory(targetData.Summon, (int)SummonSub.Pendulum, (int)SummonSub.Pendulum, 1, true);
            if (data.name == null)
                return UniTask.CompletedTask;

            data.num = GetVoiceNum(targetData, data.name);
            data.isHero = myTurn;
            data.wait = false;
            data.delay = 0f;
            voiceData.Add(data);
            ignoreNextPendulumSummon = true;

            return UniTask.CompletedTask;
        }

        protected override UniTask GameMessage_UpdateData(BinaryReader reader)
        {
            if (inPendulumSummon)
            {
                inPendulumSummon = false;
                ignoreNextPendulumSummon = false;
                cardsBeTarget.Clear();
            }

            return UniTask.CompletedTask;
        }


        #endregion

    }
}
