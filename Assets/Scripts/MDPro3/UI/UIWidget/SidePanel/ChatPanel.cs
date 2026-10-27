using DG.Tweening;
using MDPro3.Utility;
using MDPro3.Duel.YGOSharp;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;

namespace MDPro3.UI
{
    public class ChatPanel : SidePanel
    {
        protected override bool Permanent => true;
        protected override float TransitionTime => 0.4f;
        [Header("Chat Panel")]
        public TMP_InputField input;
        [SerializeField] private ScrollRect scrollRect;

        private GameObject chatItemMe;
        private GameObject chatItemOp;
        private GameObject chatItemSystem;
        private List<GameObject> chatItems = new();
        private struct ChatMessage
        {
            public int player;
            public string content;
        }
        private readonly List<ChatMessage> cachedMessages = new();

        protected override void Awake()
        {
            base.Awake();

            var handle = Addressables.LoadAssetAsync<GameObject>("UI/ChatItemMe.prefab");
            handle.Completed += (result) =>
            {
                chatItemMe = result.Result;
            };
            var handle2 = Addressables.LoadAssetAsync<GameObject>("UI/ChatItemOp.prefab");
            handle2.Completed += (result) =>
            {
                chatItemOp = result.Result;
            };
            var handle3 = Addressables.LoadAssetAsync<GameObject>("UI/ChatItemSystem.prefab");
            handle3.Completed += (result) =>
            {
                chatItemSystem = result.Result;
            };
        }

        public void Switch()
        {
            if (showing)
            {
                if (input != null && input.isFocused)
                    input.DeactivateInputField();
                HideWithSound();
            }
            else
                Show();
        }

        public void Show(bool takeOver = true)
        {
            EnsureClose();
            base.Show();
            RestyleChat();
            if (BG != null) BG.blocksRaycasts = false;
            if (Program.instance.currentServant != Program.instance.room
                && !DeviceInfo.OnMobile())
                input.Select();

            scrollRect.DOVerticalNormalizedPos(0f, 0f);
        }

        protected override void Update()
        {
            if (!showing) return;
            if (!NeedResponse()) return;

            if ((UserInput.WasCancelPressed 
                || UserInput.MouseRightDown)
                && Program.instance.currentServant == Program.instance.ocgcore)
                HideWithSound();
        }

        protected override bool NeedResponse()
        {
            if (input.isFocused)
                return false;
            return base.NeedResponse() || Program.instance.room.showing;
        }

        void EnsureClose()
        {
            if (Window == null || Window.Find("Ultra chat close") != null) return;
            var close = new GameObject("Ultra chat close", typeof(RectTransform));
            close.layer = gameObject.layer;
            var rect = close.GetComponent<RectTransform>();
            rect.SetParent(Window, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(132f, 44f);
            rect.anchoredPosition = new Vector2(-18f, -16f);
            var chrome = close.AddComponent<UltraChrome>();
            chrome.color = UltraTheme.PanelSolid;
            chrome.cut = UltraCut.Small;
            chrome.raycastTarget = true;
            var button = close.AddComponent<Button>();
            button.targetGraphic = chrome;
            button.onClick.AddListener(() =>
            {
                if (input != null) input.DeactivateInputField();
                HideWithSound();
            });
            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.layer = gameObject.layer;
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = "CLOSE";
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = UltraTheme.Fg;
            label.raycastTarget = false;
            if (Program.instance != null && Program.instance.ui_ != null && Program.instance.ui_.tmpFont != null)
                label.font = Program.instance.ui_.tmpFont;
        }

        void RestyleChat()
        {
            if (Window != null)
            {
                var own = Window.GetComponent<Image>();
                if (own != null) own.color = UltraTheme.PanelSolid;
                foreach (Transform child in Window)
                {
                    if (child.name is not ("Base" or "BG" or "Background" or "Frame" or "Image")) continue;
                    var image = child.GetComponent<Image>();
                    if (image != null && image is not UltraChrome)
                        image.color = UltraTheme.PanelSolid;
                }
            }
            if (BG != null)
            {
                var dim = BG.GetComponent<Image>();
                if (dim != null) dim.color = new Color(0.02f, 0.024f, 0.032f, 0.55f);
            }
        }

        public void OnSend()
        {
            if (input.text == string.Empty)
            {
                Hide();
                return;
            }
            OnChat(input.text);
        }

        public void OnChat(string content)
        {
            if (content == string.Empty)
                return;
            TcpHelper.CtosMessage_Chat(content);
            Program.instance.ui_.chatPanel.ClearInputField();
        }

        public string GetInputFieldText()
        {
            return input.text;
        }

        public void ClearInputField()
        {
            input.text = string.Empty;
        }

        public enum PlayerPosition
        {
            Me,
            MyTag,
            Op,
            OpTag,
            WatchMe, // 本机处于观战时的一号位
            WatchMyTag,
            WatchOp,
            WatchOpTag,
            Other
        }

        public void AddChatItem(int player, string content)
        {
            if (RoomServant.CoreShowing == 1)
            {
                cachedMessages.Add(new ChatMessage
                {
                    player = player,
                    content = content
                });
                return;
            }
            if (RoomServant.CoreShowing == 2 && cachedMessages.Count > 0)
            {
                var cacehd = new List<ChatMessage>(cachedMessages);
                cachedMessages.Clear();
                for (int i = 0; i < cacehd.Count; i++)
                    AddChatItem(cacehd[i].player, cacehd[i].content);
            }
            if (player == -2)
                return;
            var nickName = GetPlayerName(player);
            GameObject item = null;
            var position = GetPlayerPosition(player);
            switch (position)
            {
                case PlayerPosition.Me:
                    item = Instantiate(chatItemMe);
                    item.transform.GetChild(2).GetComponent<Image>().material = Appearance.duelFrameMat0;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Appearance.duelFace0;
                    break;
                case PlayerPosition.MyTag:
                    item = Instantiate(chatItemMe);
                    item.transform.GetChild(2).GetComponent<Image>().material = Appearance.duelFrameMat0Tag;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Appearance.duelFace0Tag;
                    break;
                case PlayerPosition.Op:
                    item = Instantiate(chatItemOp);
                    item.transform.GetChild(2).GetComponent<Image>().material = Appearance.duelFrameMat1;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Appearance.duelFace1;
                    break;
                case PlayerPosition.OpTag:
                    item = Instantiate(chatItemOp);
                    item.transform.GetChild(2).GetComponent<Image>().material = Appearance.duelFrameMat1Tag;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Appearance.duelFace1Tag;
                    break;
                case PlayerPosition.WatchMe:
                    item = Instantiate(chatItemMe);
                    item.transform.GetChild(2).GetComponent<Image>().material = Appearance.watchFrameMat0;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Appearance.watchFace0;
                    break;
                case PlayerPosition.WatchMyTag:
                    item = Instantiate(chatItemMe);
                    item.transform.GetChild(2).GetComponent<Image>().material = Appearance.watchFrameMat0Tag;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Appearance.watchFace0Tag;
                    break;
                case PlayerPosition.WatchOp:
                    item = Instantiate(chatItemOp);
                    item.transform.GetChild(2).GetComponent<Image>().material = Appearance.watchFrameMat1;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Appearance.watchFace1;
                    break;
                case PlayerPosition.WatchOpTag:
                    item = Instantiate(chatItemOp);
                    item.transform.GetChild(2).GetComponent<Image>().material = Appearance.watchFrameMat1Tag;
                    item.transform.GetChild(2).GetComponent<Image>().sprite = Appearance.watchFace1Tag;
                    break;
                case PlayerPosition.Other:
                    item = Instantiate(chatItemSystem);
                    break;
            }
            item.transform.GetChild(0).GetComponent<Text>().text = nickName + ":";
            item.transform.GetChild(1).GetComponent<Text>().text = content;
            if (position == PlayerPosition.Other)
            {
                item.transform.GetChild(0).GetComponent<Text>().text = string.Empty;
                item.transform.GetChild(1).GetComponent<Text>().text = string.Empty;
                item.transform.GetChild(2).GetComponent<Text>().text = content;
            }
            item.transform.SetParent(scrollRect.content, false);
            item.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -chatItems.Count * 150);
            chatItems.Add(item);
            scrollRect.content.sizeDelta = new Vector2(0, chatItems.Count * 150);
            scrollRect.DOVerticalNormalizedPos(0, 0.2f);

            var p = new Package
            {
                Function = (int)GameMessage.sibyl_chat,
                Data = new BinaryMaster()
            };
            p.Data.writer.Write(player);
            p.Data.writer.WriteUnicode(content, content.Length + 1);
            TcpHelper.AddRecordLine(p);
            if (Program.instance.ocgcore.showing)
                Program.instance.ocgcore.Chat(player, content);
            if (Program.instance.online.showing)
                MessageManager.Cast(content);
        }

        private static int GetRoomPlayerIndex(int player)
        {
            if (!Program.instance.ocgcore.showing)
                return player;
            if (player > -1 && player < 4)
            {
                var swapMask = RoomServant.Mode == 2 ? 2 : 1;
                if (RoomServant.SelfType == 7)
                {
                    if (!OcgCore.isFirst)
                        return player ^ swapMask;
                    else
                        return player;
                }
                if (InFirst() && !OcgCore.isFirst)
                    return player ^ swapMask;
                if(!InFirst() && OcgCore.isFirst)
                    return player ^ swapMask;
            }
            return player;
        }

        private static bool InFirst()
        {
            if(RoomServant.Mode < 2)
                return RoomServant.SelfType == 0;
            else
                return RoomServant.SelfType == 0 || RoomServant.SelfType == 1;
        }

        private static string GetPlayerConfigName(PlayerPosition position)
        {
            return position switch
            {
                PlayerPosition.Me => Config.Get("DuelPlayerName0", Config.EMPTY_STRING),
                PlayerPosition.MyTag => Config.Get("DuelPlayerName0Tag", Config.EMPTY_STRING),
                PlayerPosition.Op => Config.Get("DuelPlayerName1", Config.EMPTY_STRING),
                PlayerPosition.OpTag => Config.Get("DuelPlayerName1Tag", Config.EMPTY_STRING),
                PlayerPosition.WatchMe => Config.Get("WatchPlayerName0", Config.EMPTY_STRING),
                PlayerPosition.WatchMyTag => Config.Get("WatchPlayerName0Tag", Config.EMPTY_STRING),
                PlayerPosition.WatchOp => Config.Get("WatchPlayerName1", Config.EMPTY_STRING),
                PlayerPosition.WatchOpTag => Config.Get("WatchPlayerName1Tag", Config.EMPTY_STRING),
                _ => string.Empty,
            };
        }

        public static string GetPlayerName(int player)
        {
            if(RoomServant.Mode==2 && player>=0 && player<4 && RoomServant.players[player]==null
                && RoomServant.players[player^1]!=null)player^=1;
            var playerPosition = GetPlayerPosition(player);
            player = GetRoomPlayerIndex(player);
            string nickName = string.Empty;
            switch (player)
            {
                case -1: //local name
                    nickName = Config.Get("DuelPlayerName0", Config.EMPTY_STRING);
                    break;
                case 0: //from host
                case 1: //from client
                case 2: //host tag
                case 3: //client tag
                    nickName = RoomServant.players[player].name;
                    var configName = GetPlayerConfigName(playerPosition);
                    if (configName.Length > 0)
                        nickName = configName;
                    break;
                case 7: //observer
                    nickName += InterString.Get("观战者");
                    break;
                case 8: //system custom message, no prefix.
                    nickName += "[System]";
                    break;
                case 9: //error message
                    nickName += "[Script error]";
                    break;
                default: //from watcher or unknown
                    nickName += "[---]";
                    break;
            }
            return nickName;
        }

        public static PlayerPosition GetPlayerPosition(int player)
        {
            player = GetRoomPlayerIndex(player);
            PlayerPosition position;
            if (player < 4)
            {
                if (RoomServant.Mode < 2)
                {
                    if (RoomServant.SelfType != 7)
                    {
                        if (RoomServant.SelfType == player)
                            position = PlayerPosition.Me;
                        else
                            position = PlayerPosition.Op;
                    }
                    else
                    {
                        if (player == 0)
                            position = PlayerPosition.WatchMe;
                        else
                            position = PlayerPosition.WatchOp;
                    }
                }
                else
                {
                    if (RoomServant.SelfType != 7)
                    {
                        if (RoomServant.SelfType == player)
                            position = PlayerPosition.Me;
                        else if ((RoomServant.SelfType + player) % 4 == 1)
                            position = PlayerPosition.MyTag;
                        else
                        {
                            if (player == 0 || player == 2)
                                position = PlayerPosition.Op;
                            else
                                position = PlayerPosition.OpTag;
                        }
                    }
                    else
                    {
                        if (player == 0)
                            position = PlayerPosition.WatchMe;
                        else if (player == 1)
                            position = PlayerPosition.WatchMyTag;
                        else if (player == 2)
                            position = PlayerPosition.WatchOp;
                        else
                            position = PlayerPosition.WatchOpTag;
                    }
                }
            }
            else
                position = PlayerPosition.Other;
            return position;
        }

    }
}
