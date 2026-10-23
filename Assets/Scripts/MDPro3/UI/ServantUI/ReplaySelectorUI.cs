using Percy;
using System;
using System.Collections.Generic;
using System.IO;
using SevenZip.Compression.LZMA;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using TMPro;
using MDPro3.Servant;
using MDPro3.UI.PropertyOverride;
using GameMessage = MDPro3.Duel.YGOSharp.GameMessage;

namespace MDPro3.UI.ServantUI
{
    public class ReplaySelectorUI : ServantUI
    {

        #region Elements

        private const string LABEL_SR = "ScrollRect";
        private ScrollRect m_ScrollRect;
        private ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetElement<ScrollRect>(LABEL_SR);

        private const string LABEL_TXT_OVERVIEW = "TextOverview";
        private TextMeshProUGUI m_TextOverview;
        public TextMeshProUGUI TextOverview =>
            m_TextOverview = m_TextOverview != null ? m_TextOverview
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_OVERVIEW);

        private const string LABEL_SBN_PLAYER0 = "ButtonPlayer0";
        private SelectionButton m_ButtonPlayer0;
        public SelectionButton ButtonPlayer0 =>
            m_ButtonPlayer0 = m_ButtonPlayer0 != null ? m_ButtonPlayer0
            : Manager.GetElement<SelectionButton>(LABEL_SBN_PLAYER0);

        private const string LABEL_SBN_PLAYER1 = "ButtonPlayer1";
        private SelectionButton m_ButtonPlayer1;
        public SelectionButton ButtonPlayer1 =>
            m_ButtonPlayer1 = m_ButtonPlayer1 != null ? m_ButtonPlayer1
            : Manager.GetElement<SelectionButton>(LABEL_SBN_PLAYER1);

        private const string LABEL_SBN_PLAYER2 = "ButtonPlayer2";
        private SelectionButton m_ButtonPlayer2;
        public SelectionButton ButtonPlayer2 =>
            m_ButtonPlayer2 = m_ButtonPlayer2 != null ? m_ButtonPlayer2
            : Manager.GetElement<SelectionButton>(LABEL_SBN_PLAYER2);

        private const string LABEL_SBN_PLAYER3 = "ButtonPlayer3";
        private SelectionButton m_ButtonPlayer3;
        public SelectionButton ButtonPlayer3 =>
            m_ButtonPlayer3 = m_ButtonPlayer3 != null ? m_ButtonPlayer3
            : Manager.GetElement<SelectionButton>(LABEL_SBN_PLAYER3);

        private const string LABEL_SBN_SORT = "ButtonSort";
        private SelectionButton m_ButtonSort;
        private SelectionButton ButtonSort =>
            m_ButtonSort = m_ButtonSort != null ? m_ButtonSort
            : Manager.GetElement<SelectionButton>(LABEL_SBN_SORT);

        private const string LABEL_SBN_GODVIEW = "ButtonGodView";
        private SelectionButton m_ButtonGodView;
        public SelectionButton ButtonGodView =>
            m_ButtonGodView = m_ButtonGodView != null ? m_ButtonGodView
            : Manager.GetElement<SelectionButton>(LABEL_SBN_GODVIEW);

        private const string LABEL_IMG_HOVER = "ButtonHover";
        private Image m_ImageHover;
        public Image ImageHover =>
            m_ImageHover = m_ImageHover != null ? m_ImageHover
            : Manager.GetElement<Image>(LABEL_IMG_HOVER);

        private const string LABEL_IMG_OUT = "ButtonOut";
        private Image m_ImageOut;
        public Image ImageOut =>
            m_ImageOut = m_ImageOut != null ? m_ImageOut
            : Manager.GetElement<Image>(LABEL_IMG_OUT);

        #endregion

        public SuperScrollView superScrollView;
        private readonly Dictionary<string, YRP> cachedYRPs = new();
        private bool sortByName = true;
        private PercyOCG percy;

        public void KF_Replay(string name, bool god = false)
        {
            OcgCore.CurrentReplayGodView = god;
            string fileName = Program.PATH_REPLAY + name + (name.EndsWith(Program.EXPANSION_YRP) ? string.Empty : Program.EXPANSION_YRP3D);
            if (!File.Exists(fileName))
            {
                fileName = fileName.Replace(Program.EXPANSION_YRP3D, Program.EXPANSION_YRP);
                if (!File.Exists(fileName))
                    return;
            }
            OcgCore.CurrentReplayNativePrompts=File.Exists(fileName+".protocol") && File.ReadAllText(fileName+".protocol").Trim()=="native";
            bool yrp3d = fileName.Length > 6 && fileName.ToLower().Substring(fileName.Length - 6, 6) == Program.EXPANSION_YRP3D;
            try
            {
                if (yrp3d)
                {
                    if (god)
                    {
                        MessageManager.Cast(InterString.Get("您正在观看旧版的回放（上帝视角），不保证稳定性。"));
                        percy?.Dispose();
                        percy = new PercyOCG();
                        var replays = GetYRPBuffer(fileName);
                        var collections = TcpHelper.GetPackages(percy.ygopro.GetYRP3dBuffer(GetYRP(replays[^1])));
                        PushCollection(collections);
                    }
                    else
                    {
                        var replays = GetYRPBuffer(fileName);
                        if (replays.Count == 0)
                            OcgCore.CurrentReplayUseYRP2 = true;
                        else
                            OcgCore.CurrentReplayUseYRP2 = GetYRP(replays[^1]).IsNew();

                        var collection = TcpHelper.ReadPackagesInRecord(fileName);
                        PushCollection(collection);
                    }
                }
                else
                {
                    MessageManager.Cast(InterString.Get("您正在观看旧版的回放（上帝视角），不保证稳定性。"));
                    percy?.Dispose();
                    percy = new PercyOCG();
                    var collections = TcpHelper.GetPackages(percy.ygopro.GetYRP3dBuffer(GetYRP(File.ReadAllBytes(fileName))));
                    PushCollection(collections);
                }
            }
            catch(Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError(e);
#endif
                MessageManager.Cast(InterString.Get("回放没有录制完整。"));
            }
        }

        private List<byte[]> GetYRPBuffer(string path)
        {
            if (path.EndsWith(Program.EXPANSION_YRP))
                return new List<byte[]>() { File.ReadAllBytes(path) };
            var returnValue = new List<byte[]>();
            try
            {
                var collection = TcpHelper.ReadPackagesInRecord(path);
                foreach (var item in collection)
                    if (item.Function == (int)GameMessage.sibyl_replay)
                    {
                        var replay = item.Data.reader.ReadToEnd();
                        // TODO: don't include other replays
                        returnValue.Add(replay);
                    }
            }
            catch (Exception e) { Debug.LogError(e); }
            return returnValue;
        }

        private YRP GetYRP(byte[] buffer)
        {
            var returnValue = new YRP();
            try
            {
                var reader = new BinaryReader(new MemoryStream(buffer));
                returnValue.ID = reader.ReadInt32();
                returnValue.Version = reader.ReadInt32();
                returnValue.Flag = reader.ReadInt32();
                returnValue.Seed = reader.ReadUInt32();
                returnValue.DataSize = reader.ReadInt32();
                returnValue.Hash = reader.ReadInt32();
                returnValue.Props = reader.ReadBytes(8);
                if (returnValue.ID == 0x32707279) // REPLAY_ID_YRP2
                {
                    for (int i = 0; i < 8; i++)
                    {
                        returnValue.SeedsV2[i] = reader.ReadUInt32();
                    }
                    for (int i = 0; i < 4; i++) // other flags, unused for now
                    {
                        reader.ReadUInt32();
                    }
                }
                var raw = reader.ReadToEnd();
                if ((returnValue.Flag & 0x1) > 0)
                {
                    var lzma = new Decoder();
                    lzma.SetDecoderProperties(returnValue.Props);
                    var decompressed = new MemoryStream();
                    lzma.Code(new MemoryStream(raw), decompressed, raw.LongLength, returnValue.DataSize, null);
                    raw = decompressed.ToArray();
                }

                reader = new BinaryReader(new MemoryStream(raw));
                if ((returnValue.Flag & 0x2) > 0)
                {
                    RoomServant.Mode = 2;
                    returnValue.playerData.Add(new YRP.PlayerData());
                    returnValue.playerData.Add(new YRP.PlayerData());
                    returnValue.playerData.Add(new YRP.PlayerData());
                    returnValue.playerData.Add(new YRP.PlayerData());
                    returnValue.playerData[0].name = reader.ReadUnicode(20);
                    returnValue.playerData[1].name = reader.ReadUnicode(20);
                    returnValue.playerData[2].name = reader.ReadUnicode(20);
                    returnValue.playerData[3].name = reader.ReadUnicode(20);
                    returnValue.StartLp = reader.ReadInt32();
                    returnValue.StartHand = reader.ReadInt32();
                    returnValue.DrawCount = reader.ReadInt32();
                    returnValue.opt = reader.ReadUInt32();
                    OcgCore.MasterRule = (int)(returnValue.opt >> 16);
                    for (var i = 0; i < 4; i++)
                    {
                        var count = reader.ReadInt32();
                        for (var i2 = 0; i2 < count; i2++) returnValue.playerData[i].main.Add(reader.ReadInt32());
                        count = reader.ReadInt32();
                        for (var i2 = 0; i2 < count; i2++) returnValue.playerData[i].extra.Add(reader.ReadInt32());
                    }
                }
                else
                {
                    returnValue.playerData.Add(new YRP.PlayerData());
                    returnValue.playerData.Add(new YRP.PlayerData());
                    returnValue.playerData[0].name = reader.ReadUnicode(20);
                    returnValue.playerData[1].name = reader.ReadUnicode(20);
                    returnValue.StartLp = reader.ReadInt32();
                    returnValue.StartHand = reader.ReadInt32();
                    returnValue.DrawCount = reader.ReadInt32();
                    returnValue.opt = reader.ReadUInt32();
                    OcgCore.MasterRule = (int)(returnValue.opt >> 16);
                    for (var i = 0; i < 2; i++)
                    {
                        var count = reader.ReadInt32();
                        for (var i2 = 0; i2 < count; i2++) returnValue.playerData[i].main.Add(reader.ReadInt32());
                        count = reader.ReadInt32();
                        for (var i2 = 0; i2 < count; i2++) returnValue.playerData[i].extra.Add(reader.ReadInt32());
                    }
                }
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                    returnValue.gameData.Add(reader.ReadBytes(reader.ReadByte()));
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            return returnValue;
        }

        public YRP CacheYRP(string replay)
        {
            if (cachedYRPs.ContainsKey(replay))
                return cachedYRPs[replay];
            YRP yrp;
            if (File.Exists(Program.PATH_REPLAY + replay))
                yrp = GetYRP(File.ReadAllBytes(Program.PATH_REPLAY + replay));
            else
            {
                var buffer = GetYRPBuffer(Program.PATH_REPLAY + replay + Program.EXPANSION_YRP3D);
                if (buffer.Count == 0)
                    yrp = null;
                else
                    yrp = GetYRP(buffer[0]);
            }
            if (yrp != null)
                cachedYRPs.Add(replay, yrp);
            return yrp;
        }

        private void PushCollection(List<Package> collection)
        {
            Program.instance.ocgcore.returnServant = Program.instance.replay;
            OcgCore.handler = a => { };
            OcgCore.name_0 = Config.Get("ReplayPlayerName0", Config.EMPTY_STRING);
            OcgCore.name_0_tag = Config.Get("ReplayPlayerName0Tag", Config.EMPTY_STRING);
            OcgCore.name_0_c = OcgCore.name_0;
            OcgCore.name_1 = Config.Get("ReplayPlayerName1", Config.EMPTY_STRING);
            OcgCore.name_1_tag = Config.Get("ReplayPlayerName1Tag", Config.EMPTY_STRING);
            OcgCore.name_1_c = OcgCore.name_1;
            OcgCore.timeLimit = 240;
            OcgCore.lpLimit = 8000;
            OcgCore.isFirst = true;
            OcgCore.condition = OcgCore.Condition.Replay;
            Program.instance.ShiftToServant(Program.instance.ocgcore);
            Program.instance.ocgcore.FlushPackages(collection);
        }

        private void SelectZero()
        {
            var item0 = superScrollView.items[0].gameObject.GetComponent<SelectionToggle_Replay>();
            item0.SetToggleOn();
            Program.instance.replay.lastSelectedReplayItem = item0;
        }

        public void Print()
        {
            superScrollView?.Clear();

            if (!Directory.Exists(Program.PATH_REPLAY))
                Directory.CreateDirectory(Program.PATH_REPLAY);
            var fileInfos = new DirectoryInfo(Program.PATH_REPLAY).GetFiles();
            if (sortByName)
                Array.Sort(fileInfos, Tools.CompareName);
            else
                Array.Sort(fileInfos, Tools.CompareTime);

            List<string[]> tasks = new List<string[]>();
            int count = 0;
            for (int i = 0; i < fileInfos.Length; i++)
            {
                if (fileInfos[i].Name.EndsWith(Program.EXPANSION_YRP3D))
                {
                    var task = new string[] { count.ToString(), fileInfos[i].Name.Replace(Program.EXPANSION_YRP3D, string.Empty) };
                    tasks.Add(task);
                    count++;
                }
                else if (fileInfos[i].Name.EndsWith(Program.EXPANSION_YRP))
                {
                    var task = new string[] { count.ToString(), fileInfos[i].Name };
                    tasks.Add(task);
                    count++;
                }
            }

            var handle = Addressables.LoadAssetAsync<GameObject>("UI/ItemReplay.prefab");
            handle.Completed += (result) =>
            {
                var itemHeight = PropertyOverrider.NeedMobileLayout() ? 180f : 150f;
                float topPadding = PropertyOverrider.NeedMobileLayout() ? 148f : 134f;
                float space = itemHeight - (PropertyOverrider.NeedMobileLayout() ? 152f : 122f);
                float bottomPadding = (PropertyOverrider.NeedMobileLayout() ? 64f : 54f) - space;
                superScrollView = new SuperScrollView(
                        1,
                        700,
                        itemHeight,
                        topPadding,
                        bottomPadding,
                        result.Result,
                        ItemOnListRefresh,
                        Manager.GetElement<ScrollRect>("ScrollRect"));
                superScrollView.Print(tasks);
                if (tasks.Count > 0)
                    SelectZero();
            };
        }

        private void ItemOnListRefresh(string[] task, GameObject item)
        {
            var handler = item.GetComponent<SelectionToggle_Replay>();
            handler.index = int.Parse(task[0]);
            handler.replayName = task[1];
            handler.Refresh();
        }

        public void OnRename()
        {
            var selections = new List<string>()
            {
                InterString.Get("请输入新的回放名称"),
                superScrollView.items[superScrollView.selected].args[1].Replace(Program.EXPANSION_YRP, string.Empty)
            };
            UIManager.ShowPopupInput(selections, ReplayRename, null, TmpInputValidation.ValidationType.Path);
        }

        private void ReplayRename(string newName)
        {
            string replay = superScrollView.items[superScrollView.selected].args[1];
            if (replay.EndsWith(Program.EXPANSION_YRP))
                File.Move(Program.PATH_REPLAY + replay, Program.PATH_REPLAY + newName + Program.EXPANSION_YRP);
            else
                File.Move(Program.PATH_REPLAY + replay + Program.EXPANSION_YRP3D, Program.PATH_REPLAY + newName + Program.EXPANSION_YRP3D);
            string oldMeta=Program.PATH_REPLAY+replay+(replay.EndsWith(Program.EXPANSION_YRP)?"":Program.EXPANSION_YRP3D)+".protocol";
            string newMeta=Program.PATH_REPLAY+newName+(replay.EndsWith(Program.EXPANSION_YRP)?Program.EXPANSION_YRP:Program.EXPANSION_YRP3D)+".protocol";
            if(File.Exists(oldMeta))File.Move(oldMeta,newMeta);
            foreach(var game in UltraProgress.Data.matches)if(game.replay==replay)game.replay=newName;
            UltraProgress.Save();
            Print();
        }

        public void OnPlay()
        {
            KF_Replay(superScrollView.items[superScrollView.selected].args[1]);
        }

        public void OnGodView()
        {
            KF_Replay(superScrollView.items[superScrollView.selected].args[1], true);
        }

        public void OnDelete()
        {
            var replay = superScrollView.items[superScrollView.selected].args[1];
            if (File.Exists(Program.PATH_REPLAY + replay))
                File.Delete(Program.PATH_REPLAY + replay);
            else
                File.Delete(Program.PATH_REPLAY + replay + Program.EXPANSION_YRP3D);
            MessageManager.Cast(InterString.Get("已删除回放「[?]」。", replay));
            Print();
        }

        public void OnSort()
        {
            sortByName = !sortByName;
            if (sortByName)
                ButtonSort.SetButtonText(InterString.Get("名称排序"));
            else
                ButtonSort.SetButtonText(InterString.Get("时间排序"));

            Print();
        }

        public void OnDeck(int player)
        {
            var replay = superScrollView.items[superScrollView.selected].args[1];
            var yrp = cachedYRPs[replay];
            replay = replay.Replace(Program.EXPANSION_YRP, string.Empty);

            var deckName = replay + "_" + yrp.playerData[player].name;
            var deck = new Duel.YGOSharp.Deck(yrp.playerData[player].main, yrp.playerData[player].extra, new List<int>());

            Program.instance.deckEditor.SwitchCondition(DeckEditor.Condition.ReplayDeck, deckName, deck);
            Program.instance.ShiftToServant(Program.instance.deckEditor);
        }

        public void SelectLastReplayItem()
        {
            UserInput.NextSelectionIsAxis = true;
            if (Program.instance.replay.lastSelectedReplayItem != null)
                Program.instance.replay.lastSelectedReplayItem.GetSelectable().Select();
        }

    }
}
