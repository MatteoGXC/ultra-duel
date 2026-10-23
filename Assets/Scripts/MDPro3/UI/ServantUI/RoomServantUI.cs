using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.Utility;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MDPro3.UI.ChatPanel;

namespace MDPro3.UI.ServantUI
{
    public class RoomServantUI : ServantUI
    {

        #region Elements

        private const string LABEL_TXT_ROOMINFO = "TextRoomInfo";
        private TextMeshProUGUI m_TextRoomInfo;
        private TextMeshProUGUI TextRoomInfo =>
            m_TextRoomInfo = m_TextRoomInfo != null ? m_TextRoomInfo
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_ROOMINFO);

        private const string LABEL_SBN_DECKSELECTOR = "DeckSelector";
        private SelectionButton_DeckSelector m_ButtonDeckSelector;
        private SelectionButton_DeckSelector ButtonDeckSelector =>
            m_ButtonDeckSelector = m_ButtonDeckSelector != null ? m_ButtonDeckSelector
            : Manager.GetElement<SelectionButton_DeckSelector>(LABEL_SBN_DECKSELECTOR);

        private const string LABEL_SBN_ADDBOT = "ButtonAddBot";
        private SelectionButton m_ButtonAddBot;
        private SelectionButton ButtonAddBot =>
            m_ButtonAddBot = m_ButtonAddBot != null ? m_ButtonAddBot
            : Manager.GetElement<SelectionButton>(LABEL_SBN_ADDBOT);

        private const string LABEL_SBN_PLAYER0 = "ButtonPlayer0";
        private SelectionButton_RoomPlayer m_ButtonPlayer0;
        private SelectionButton_RoomPlayer ButtonPlayer0 =>
            m_ButtonPlayer0 = m_ButtonPlayer0 != null ? m_ButtonPlayer0
            : Manager.GetElement<SelectionButton_RoomPlayer>(LABEL_SBN_PLAYER0);

        private const string LABEL_SBN_PLAYER1 = "ButtonPlayer1";
        private SelectionButton_RoomPlayer m_ButtonPlayer1;
        private SelectionButton_RoomPlayer ButtonPlayer1 =>
            m_ButtonPlayer1 = m_ButtonPlayer1 != null ? m_ButtonPlayer1
            : Manager.GetElement<SelectionButton_RoomPlayer>(LABEL_SBN_PLAYER1);

        private const string LABEL_SBN_PLAYER2 = "ButtonPlayer2";
        private SelectionButton_RoomPlayer m_ButtonPlayer2;
        private SelectionButton_RoomPlayer ButtonPlayer2 =>
            m_ButtonPlayer2 = m_ButtonPlayer2 != null ? m_ButtonPlayer2
            : Manager.GetElement<SelectionButton_RoomPlayer>(LABEL_SBN_PLAYER2);

        private const string LABEL_SBN_PLAYER3 = "ButtonPlayer3";
        private SelectionButton_RoomPlayer m_ButtonPlayer3;
        private SelectionButton_RoomPlayer ButtonPlayer3 =>
            m_ButtonPlayer3 = m_ButtonPlayer3 != null ? m_ButtonPlayer3
            : Manager.GetElement<SelectionButton_RoomPlayer>(LABEL_SBN_PLAYER3);

        private const string LABEL_SBN_TODUEL = "ButtonToDuel";
        private SelectionButton m_ButtonToDuel;
        private SelectionButton ButtonToDuel =>
            m_ButtonToDuel = m_ButtonToDuel != null ? m_ButtonToDuel
            : Manager.GetElement<SelectionButton>(LABEL_SBN_TODUEL);

        private const string LABEL_SBN_READY = "ButtonReady";
        private SelectionButton m_ButtonReady;
        private SelectionButton ButtonReady =>
            m_ButtonReady = m_ButtonReady != null ? m_ButtonReady
            : Manager.GetElement<SelectionButton>(LABEL_SBN_READY);

        private const string LABEL_SBN_TOWATCH = "ButtonToWatch";
        private SelectionButton m_ButtonToWatch;
        private SelectionButton ButtonToWatch =>
            m_ButtonToWatch = m_ButtonToWatch != null ? m_ButtonToWatch
            : Manager.GetElement<SelectionButton>(LABEL_SBN_TOWATCH);

        private const string LABEL_SBN_START = "ButtonStart";
        private SelectionButton m_ButtonStart;
        private SelectionButton ButtonStart =>
            m_ButtonStart = m_ButtonStart != null ? m_ButtonStart
            : Manager.GetElement<SelectionButton>(LABEL_SBN_START);

        #endregion

        private List<SelectionButton_RoomPlayer> roomPlayers;
        private readonly Dictionary<int, Sprite> syncedFaces = new();
        private readonly Dictionary<int, Material> syncedFrames = new();
        private readonly Dictionary<int, int> syncedFaceIds = new();
        private readonly Dictionary<int, int> syncedFrameIds = new();

        private void Awake()
        {
            roomPlayers = new List<SelectionButton_RoomPlayer>()
            { ButtonPlayer0, ButtonPlayer1, ButtonPlayer2, ButtonPlayer3 };

            Realize();
        }

        public override void AfterShowEvent()
        {
            base.AfterShowEvent();
            if (Program.instance.currentServant != Program.instance.room)
            {
                ShutDown();
                Program.instance.ui_.chatPanel.Hide();
            }
        }

        public void SelectMiddleSelectableFromRight()
        {
            if (!gameObject.activeSelf)
                return;

            UserInput.NextSelectionIsAxis = true;

            if (ButtonStart.gameObject.activeSelf)
                ButtonStart.GetSelectable().Select();
            else
                ButtonToWatch.GetSelectable().Select();
        }

        public void SelectMiddleFromLeft()
        {

        }

        public void Realize()
        {
            var roomInfo = string.Empty;
            var rn = Program.STRING_LINE_BREAK;
            if (RoomServant.FromLocalHost)
            {
                foreach (var ip in Tools.GetLocalIPv4())
                    roomInfo += InterString.Get("本机地址：") + Language.GetBlankIfNeed() + ip + rn;
                roomInfo += InterString.Get("端口：") + Language.GetBlankIfNeed() + "7911" + rn;
            }
            roomInfo += StringHelper.GetUnsafe(1227) + Language.GetBlankIfNeed() + StringHelper.GetUnsafe(1244 + RoomServant.Mode) + rn;//决斗模式：
            roomInfo += StringHelper.GetUnsafe(1236) + Language.GetBlankIfNeed() + StringHelper.GetUnsafe(1259 + OcgCore.MasterRule) + rn;//规则：
            roomInfo += StringHelper.GetUnsafe(1225) + Language.GetBlankIfNeed() + StringHelper.GetUnsafe(1481 + RoomServant.Rule) + rn;//卡片允许：
            roomInfo += StringHelper.GetUnsafe(1226) + Language.GetBlankIfNeed() + BanlistManager.GetName(RoomServant.LFList) + rn;//禁限卡表
            roomInfo += StringHelper.GetUnsafe(1231) + Language.GetBlankIfNeed() + RoomServant.StartLp + rn;//初始基本分：
            roomInfo += StringHelper.GetUnsafe(1232) + Language.GetBlankIfNeed() + RoomServant.StartHand + rn;//初始手卡数：
            roomInfo += StringHelper.GetUnsafe(1233) + Language.GetBlankIfNeed() + RoomServant.DrawCount + rn;//每回合抽卡：
            roomInfo += StringHelper.GetUnsafe(1237) + Language.GetBlankIfNeed() + RoomServant.TimeLimit + rn;//每回合时间：
            roomInfo += StringHelper.GetUnsafe(1253) + Language.GetBlankIfNeed() + RoomServant.ObserverCount + rn;//当前观战人数：
            if (RoomServant.NoCheckDeck) roomInfo += StringHelper.GetUnsafe(1229) + rn;//不检查卡组
            if (RoomServant.NoShuffleDeck) roomInfo += StringHelper.GetUnsafe(1230);//不洗切卡组
            TextRoomInfo.text = roomInfo;

            if (!Appearance.loaded)
            {
                foreach(var rp in roomPlayers)
                    rp.gameObject.SetActive(false);
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                if (RoomServant.players[i] == null)
                    roomPlayers[i].gameObject.SetActive(false);
                else
                {
                    roomPlayers[i].gameObject.SetActive(true);
                    roomPlayers[i].SetButtonText(RoomServant.players[i].name);
                    roomPlayers[i].SetReadyIcon(RoomServant.players[i].ready);
                    roomPlayers[i].SetButtonTextColor(RoomServant.SelfType == i ? Color.cyan : Color.white);

                    var position = GetPlayerPosition(i);
                    switch (position)
                    {
                        case PlayerPosition.Me:
                            roomPlayers[i].GetAvatar().material = Appearance.duelFrameMat0;
                            roomPlayers[i].GetAvatar().sprite = Appearance.duelFace0;
                            break;
                        case PlayerPosition.MyTag:
                            roomPlayers[i].GetAvatar().material = Appearance.duelFrameMat0Tag;
                            roomPlayers[i].GetAvatar().sprite = Appearance.duelFace0Tag;
                            break;
                        case PlayerPosition.Op:
                            roomPlayers[i].GetAvatar().material = Appearance.duelFrameMat1;
                            roomPlayers[i].GetAvatar().sprite = Appearance.duelFace1;
                            break;
                        case PlayerPosition.OpTag:
                            roomPlayers[i].GetAvatar().material = Appearance.duelFrameMat1Tag;
                            roomPlayers[i].GetAvatar().sprite = Appearance.duelFace1Tag;
                            break;
                        case PlayerPosition.WatchMe:
                            roomPlayers[i].GetAvatar().material = Appearance.watchFrameMat0;
                            roomPlayers[i].GetAvatar().sprite = Appearance.watchFace0;
                            break;
                        case PlayerPosition.WatchMyTag:
                            roomPlayers[i].GetAvatar().material = Appearance.watchFrameMat0Tag;
                            roomPlayers[i].GetAvatar().sprite = Appearance.watchFace0Tag;
                            break;
                        case PlayerPosition.WatchOp:
                            roomPlayers[i].GetAvatar().material = Appearance.watchFrameMat1;
                            roomPlayers[i].GetAvatar().sprite = Appearance.watchFace1;
                            break;
                        case PlayerPosition.WatchOpTag:
                            roomPlayers[i].GetAvatar().material = Appearance.watchFrameMat1Tag;
                            roomPlayers[i].GetAvatar().sprite = Appearance.watchFace1Tag;
                            break;
                    }
                    TryApplyOnlineAvatar(i);
                }
            }
            if (RoomServant.IsHost)
            {
                ButtonStart.gameObject.SetActive(true);
                ButtonAddBot.gameObject.SetActive(true);
            }
            else
            {
                ButtonStart.gameObject.SetActive(false);
                ButtonAddBot.gameObject.SetActive(false);
            }

            if (RoomServant.FromSolo)
                ButtonAddBot.gameObject.SetActive(false);

            if (RoomServant.SelfType == 7)
                ButtonReady.gameObject.SetActive(false);
            else
                ButtonReady.gameObject.SetActive(true);

        }

        public void RefreshDeckSelector()
        {
            ButtonDeckSelector.SetConfigDeck(InterString.Get("请点击此处选择卡组"));
        }

        public void OnReady()
        {
            if (RoomServant.players[RoomServant.SelfType] == null)
                return;
            if (RoomServant.players[RoomServant.SelfType].ready)
                TcpHelper.CtosMessage_HsNotReady();
            else
            {
                var deckPath = Program.PATH_DECK + Config.GetConfigDeckName() + Program.EXPANSION_YDK;
                if (File.Exists(deckPath))
                {
                    var selectedDeck = new Deck(deckPath);
                    if (DuelFormat.LocalSession && !DuelFormat.Validate(selectedDeck, "Your Deck")) return;
                    TcpHelper.CtosMessage_UpdateDeck(selectedDeck);
                    TcpHelper.CtosMessage_HsReady();
                }
                else
                    MessageManager.Cast(InterString.Get("请先选择有效的卡组。"));
            }
        }

        public void OnToDuel()
        {
            TcpHelper.CtosMessage_HsToDuelist();
        }

        public void OnToObserver()
        {
            TcpHelper.CtosMessage_HsToObserver();
        }

        public void OnStart()
        {
            TcpHelper.CtosMessage_HsStart();
        }

        public void OnKick(int player)
        {
            TcpHelper.CtosMessage_HsKick(player);
        }

        public void OnAddBot()
        {
            if (Program.instance.room.RoomIsFull())
            {
                MessageManager.Toast(InterString.Get("房间已满，无法继续添加AI。"));
            }
            else
            {
                Program.instance.solo.SwitchCondition(SoloSelector.Condition.ForRoom);
                Program.instance.ShiftToServant(Program.instance.solo);
            }
        }

        public void OnSelectDeck()
        {
            if (!Program.instance.room.CanChangeDeck())
                return;
            Program.instance.deckSelector.SwitchCondition(DeckSelector.Condition.ForDuel);
            Program.instance.ShiftToServant(Program.instance.deckSelector);
        }

        private void TryApplyOnlineAvatar(int player)
        {
            if (!RoomServant.TryGetOnlineAppearanceForPlayer(player, out var appearance))
                return;
            var iconPlayer = GetIconPlayerIndex(GetPlayerPosition(player));

            if (syncedFaceIds.TryGetValue(player, out var cachedFaceId) &&
                syncedFrameIds.TryGetValue(player, out var cachedFrameId) &&
                cachedFaceId == appearance.Face &&
                cachedFrameId == appearance.Frame &&
                syncedFaces.TryGetValue(player, out var cachedFace) &&
                syncedFrames.TryGetValue(player, out var cachedFrame) &&
                cachedFace != null &&
                cachedFrame != null)
            {
                roomPlayers[player].GetAvatar().sprite = cachedFace;
                roomPlayers[player].GetAvatar().material = cachedFrame;
                return;
            }

            _ = ApplyOnlineAvatarAsync(player, appearance, iconPlayer);
        }

        private async UniTask ApplyOnlineAvatarAsync(int player, OnlineAppearanceData appearance, int iconPlayer)
        {
            var frameCode = appearance.Frame.ToString();
            var frameSprite = await Program.items.LoadConcreteItemIconAsync(frameCode, Items.ItemType.Frame, iconPlayer);
            Material frameMaterial;
            if (appearance.Frame == Items.CODE_DIY)
                frameMaterial = Appearance.matForFace == null ? null : new Material(Appearance.matForFace);
            else
                frameMaterial = await ABLoader.LoadFrameMaterial(frameCode);
            if (frameMaterial != null && frameSprite != null)
                frameMaterial.SetTexture("_ProfileFrameTex", frameSprite.texture);

            var faceSprite = await Program.items.LoadConcreteItemIconAsync(appearance.Face.ToString(), Items.ItemType.Face, iconPlayer);
            if (!RoomServant.TryGetOnlineAppearanceForPlayer(player, out var latest))
                return;
            if (latest.Face != appearance.Face || latest.Frame != appearance.Frame)
                return;
            if (player < 0 || player >= roomPlayers.Count)
                return;
            if (!roomPlayers[player].gameObject.activeInHierarchy)
                return;

            if (frameMaterial != null)
                roomPlayers[player].GetAvatar().material = frameMaterial;
            if (faceSprite != null)
                roomPlayers[player].GetAvatar().sprite = faceSprite;

            syncedFaceIds[player] = appearance.Face;
            syncedFrameIds[player] = appearance.Frame;
            syncedFaces[player] = faceSprite;
            syncedFrames[player] = frameMaterial;
        }

        private static int GetIconPlayerIndex(PlayerPosition position)
        {
            switch (position)
            {
                case PlayerPosition.Op:
                case PlayerPosition.WatchOp:
                    return 1;
                case PlayerPosition.MyTag:
                case PlayerPosition.WatchMyTag:
                    return 2;
                case PlayerPosition.OpTag:
                case PlayerPosition.WatchOpTag:
                    return 3;
                default:
                    return 0;
            }
        }

    }
}
