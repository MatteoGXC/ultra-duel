using Cysharp.Threading.Tasks;
using DG.Tweening;
using MDPro3.Duel;
using MDPro3.Duel.YGOSharp;
using MDPro3.Net;
using MDPro3.UI;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using YgomSystem.ElementSystem;

namespace MDPro3.Servant
{
    public class OcgCore : Servant
    {

        public DuelBGManager DuelBGManager => messageDispatcher.duel.duelBGManager;
        public List<GameObject> allGameObjects => messageDispatcher.duel.duelBGManager.allGameObjects;
        public List<PlaceSelector> places => messageDispatcher.duel.duelBGManager.places;
        public static List<GameCard> materialCards = new();

        public static bool inPuzzle;
        public static bool isTag;
        public static int playerType;
        public static bool isFirst;
        public static bool isObserver;
        public static int MasterRule;
        public static int life0;
        public static int life1;
        public static int lpLimit = 8000;
        public static int timeLimit = 180;
        public static int turns;
        public static bool myTurn = true;
        public static string name_0 = string.Empty;
        public static string name_0_c = string.Empty;
        public static string name_0_tag = string.Empty;
        public static string name_1 = string.Empty;
        public static string name_1_c = string.Empty;
        public static string name_1_tag = string.Empty;
        public static int cookie_matchKill;
        public static string winReason = string.Empty;
        public static bool duelEnded;
        public static DuelPhase duelPhase = DuelPhase.Draw;
        public static DuelResult duelResult = DuelResult.DisLink;
        public static Condition condition = Condition.N;
        public static ChainCondition chainCondition = ChainCondition.Smart;

        public static string ES_hint;
        public static int ES_max;
        public static int ES_min;
        public static int ES_level;
        public static bool ES_overFlow;
        public static string ES_selectHint;
        public static int Es_selectMSGHintData;
        public static int Es_selectMSGHintPlayer;
        public static int Es_selectMSGHintType;
        public static List<int> ES_searchCodes = new();
        public static string ES_selectUnselectHint = string.Empty;
        public static bool ES_selectCardFromFieldFirstFlag = false;
        public static int ES_sortSum;
        public static string ES_turnString = string.Empty;

        public static bool surrendered;
        public static bool tagSurrendered;
        public static bool deckReserved;
        public static bool cantCheckGrave;
        public static bool inPendulumSummon;

        public static List<GameCard> cards = new();
        public static List<GameCard> tempCards = new();
        public static int chainSolvingIndex;
        public static GameCard chainSolvingCard;
        public static List<GameCard> cardsInChain = new();
        public static List<int> codesInChain = new();
        public static List<uint> controllerInChain = new();
        public static List<int> negatedInChain = new();
        public static List<GameCard> cardsBeTarget = new();
        public static List<GameCard> cardsInSelection = new();
        public static List<GameCard> cardsMustBeSelected = new();
        public static List<string> confirmedCards = new();
        public static GameCard attackingCard;
        public static GameCard summonCard;
        public static GameCard lastMoveCard;
        public static GameCard lastConfirmedCard;

        public static int mySummonCount;
        public static int mySpSummonCount;
        public static int opSummonCount;
        public static int opSpSummonCount;
        public static List<int> myActivated = new();
        public static List<int> opActivated = new();

        public delegate void ResponseHandler(byte[] buffer);
        public static ResponseHandler handler = null;
        public static GameMessage currentMessage = GameMessage.Waiting;
        public static GameMessage lastMessage = GameMessage.Waiting;
        public static List<Package> packages = new();
        public static List<Package> allPackages = new();
        public static bool pause;
        public static bool NoMoreWait
        {
            get 
            { 
                return noMoreWait; 
            }
            set 
            {
                noMoreWait = value;
                //Debug.Log($"Set noMoreWait: {value}");
            }
        }
        private static bool noMoreWait;

        public static Action endingAction;
        public static Action<int> nextMoveAction;
        public static float nextMoveActionDuration;
        public static ElementObjectManager nextMoveManager;
        public static Action nextEventAction;
        public static Action nextNegateAction;
        public static Action nextNegateAction_Additional;
        public static float nextNegateAction_AdditionalTime;
        public static ElementObjectManager nextNegateAction_AdditionalManager;
        public static Action startCard;
        public Action onSurrenderConfirmed;

        public static Material myProtector;
        public static Material opProtector;

        public static bool needRefreshMyHand = true;
        public static bool needRefreshOpHand = true;
        public static List<GameCard> myHandCards = new();
        public static List<GameCard> opHandCards = new();
        public static List<GameCard> myPreHandCards = new();
        public static List<GameCard> opPreHandCards = new();

        public static int movingToMyGrave = 0;
        public static int movingToMyExclude = 0;
        public static int movingToOpGrave = 0;
        public static int movingToOpExclude = 0;
        public static uint nextMoveMessageController;

        public static bool nextMoveNeedCode;

        #region Config Params

        public static readonly Vector3 myPosition = new(0, 15, -25);
        public static readonly Vector3 opPosition = new(0, 15, 25);

        #endregion

        #region Hand Control

        private static readonly float handCellX = 30f;
        private static float clickInPosition;
        private static int handCount;
        public static float handOffset;
        public static float lastHandOffset;
        public static bool clickingHandCard;
        public static bool handCardDraged;
        private static bool hideMyHandCard;
        public static bool HideMyHandCard
        {
            get { return hideMyHandCard; }
            set 
            { 
                hideMyHandCard = value;
                Program.instance.ocgcore.RefreshMyHandCardPosition();
            }
        }
        private static bool hideOpHandCard;
        public static bool HideOpHandCard
        {
            get { return hideOpHandCard; }
            set
            {
                hideOpHandCard = value;
                Program.instance.ocgcore.RefreshOpHandCardPosition();
            }
        }

        #endregion

        public static DuelButton btnConfirm;
        public static DuelButton btnCancel;

        #region Reference
        [Header("OcgCore")]
        public MeshRenderer greenBackground;
        [HideInInspector] public PopupDuel currentPopup;

        public static bool mycardDuel;
        public static Deck sideReference = new();
        public static bool inputMode;
        public static bool Accing;
        public static bool CurrentReplayUseYRP2;
        public static bool CurrentReplayNativePrompts;
        public static bool CurrentReplayGodView;

        #endregion

        #region Servants

        public override int Depth => -1;
        protected override bool ShowLine => false;

        public override void Initialize()
        {
            base.Initialize();
            SystemEvent.OnResolutionChange += RefreshHandCardPositionInstant;
        }

        public void LoadDuelButton()
        {
            if(btnConfirm == null)
            {
                btnConfirm = ABLoader.LoadMasterDuelGameObject("DuelButton").GetComponent<DuelButton>();
                btnConfirm.response.Add(-4);
                btnConfirm.hint = InterString.Get("确认");
                btnConfirm.type = ButtonType.Decide;
                btnConfirm.Hide();
            }
            if(btnCancel == null)
            {
                btnCancel = ABLoader.LoadMasterDuelGameObject("DuelButton").GetComponent<DuelButton>();
                btnCancel.response.Add(-5);
                btnCancel.hint = InterString.Get("取消");
                btnCancel.type = ButtonType.Cancel;
                btnCancel.Hide();
            }
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            servantUI.gameObject.SetActive(true);
            servantUI.Show(true);
            GetUI<OcgCoreUI>().CG.alpha = 0f;
            GetUI<OcgCoreUI>().CG.blocksRaycasts = false;

            _ = messageDispatcher.duel.duelBGManager.LoadAssetsAsync();
            _ = ProcessMessage();

            returnAction = null;
        }

        protected override void ApplyHideArrangement(int preDepth)
        {
            _ = messageDispatcher.duel.duelBGManager.ExitDuelAsync();
        }

        public override void OnExit()
        {
            if(RoomServant.FromUltraHost)
            {
                MainMenuUI.ReturnToFriendRooms=true;
                returnServant=Program.instance.menu;
            }
            base.OnExit();
            CloseConnection();
            CurrentReplayGodView = false;
            GetUI<OcgCoreUI>().OnNor();
        }

        public void ReturnTo()
        {
            var target=returnServant;
            if(RoomServant.FromUltraHost || target==null || target==Program.instance.online)
            {
                MainMenuUI.ReturnToFriendRooms=true;
                target=Program.instance.menu;
            }
            Program.instance.ShiftToServant(target);
        }

        public override void PerFrameFunction()
        {
            if (!showing)
                return;

            if (DuelBGManager == null)
                return;

            if (!EventSystem.current.IsPointerOverGameObject()
                && UserInput.HoverObject == null
                && UserInput.MouseLeftUp)
            {
                GetUI<OcgCoreUI>().CardDescription.Hide();
                GetUI<OcgCoreUI>().CardList.Hide();
            }

            #region Background

            if (DuelBGManager.HoveringField0() && UserInput.MouseLeftUp)
                DuelBGManager.TapField0();
            else if (DuelBGManager.HoveringField1() && UserInput.MouseLeftUp)
                DuelBGManager.TapField1();
            else if (DuelBGManager.HoveringMate0() && UserInput.MouseLeftUp)
                DuelBGManager.TapMate0();
            else if (DuelBGManager.HoveringMate1() && UserInput.MouseLeftUp)
                DuelBGManager.TapMate1();
            else
            {
                DuelBGManager.PlayMate0Random();
                DuelBGManager.PlayMate1Random();
            }

            #endregion

            #region HandOffset

            if (GetMyHandCount() > 10)
            {
                if (UserInput.MouseLeftDown
                    && UserInput.HoverObject != null
                    && UserInput.HoverObject.name == "CardModel"
                    && UserInput.HoverObject.GetComponent<GameCardMono>().cookieCard.p.controller == 0
                    && (UserInput.HoverObject.GetComponent<GameCardMono>().cookieCard.p.location & (uint)CardLocation.Hand) > 0)
                {
                    clickInPosition = UserInput.MousePos.x;
                    clickingHandCard = true;
                    handCount = GetMyHandCount();
                }
                if (clickingHandCard && UserInput.MouseLeftPressing)
                {
                    var currentOffset = lastHandOffset + UserInput.MousePos.x - clickInPosition;
                    var currentHandCellX = UIManager.ScreenLengthWithScalerX(handCellX);
                    handOffset = currentOffset > (handCount * currentHandCellX) ?
                        handCount * currentHandCellX :
                        Math.Abs(currentOffset) > (handCount * currentHandCellX) ?
                        -(handCount * currentHandCellX) :
                        currentOffset;
                }
                if (UserInput.MouseLeftUp)
                {
                    handCardDraged = false;
                    if (clickingHandCard)
                    {
                        clickingHandCard = false;
                        if (lastHandOffset != handOffset)
                        {
                            handCardDraged = true;
                            lastHandOffset = handOffset;
                        }
                    }
                }
            }
            else
            {
                if (handOffset != 0)
                {
                    handOffset = 0;
                    lastHandOffset = 0;
                    RefreshHandCardPositionInstant();
                }
            }

            #endregion

            #region Hot Key

            if (UserInput.MouseRightDown || UserInput.WasCancelPressed)
            {
                returnAction?.Invoke();
            }
            if (UserInput.MouseLeftDown)
            {
                DuelBGManager.HideEquipLine();
                DuelBGManager.HideTargetLines();
            }

            if ((UserInput.WasCancelPressed
                || UserInput.WasSubmitPressed))
            {
                ToChat();
            }

            if (Program.instance.ui_.chatPanel.showing || inputMode)
                return;

            if (Keyboard.current != null)
            {
                // Timing
                if (Keyboard.current.aKey.wasPressedThisFrame)
                {
                    chainCondition = ChainCondition.All - 1;
                    GetUI<OcgCoreUI>().OnTiming();
                }
                else if (Keyboard.current.sKey.wasPressedThisFrame)
                {
                    chainCondition = ChainCondition.No - 1;
                    GetUI<OcgCoreUI>().OnTiming();
                }
                else if (Keyboard.current.dKey.wasPressedThisFrame)
                {
                    chainCondition = ChainCondition.Smart - 1;
                    GetUI<OcgCoreUI>().OnTiming();
                }

                // Log
                if (Keyboard.current.tabKey.wasPressedThisFrame)
                {
                    GetUI<OcgCoreUI>().OnLog();
                }

                // Green
                if (Keyboard.current.gKey.wasPressedThisFrame)
                {
                    if (greenOn)
                        GreenBackgroundOff();
                    else
                        GreenBackgroundOn();
                }

                if (greenOn)
                {
                    if (Keyboard.current.numpad0Key.wasPressedThisFrame
                        || Keyboard.current.digit0Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = Color.black;
                    }
                    else if (Keyboard.current.numpad1Key.wasPressedThisFrame
                        || Keyboard.current.digit1Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = Color.red;
                    }
                    else if (Keyboard.current.numpad2Key.wasPressedThisFrame
                        || Keyboard.current.digit2Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = new Color(1f, 0.5f, 0f);
                    }
                    else if (Keyboard.current.numpad3Key.wasPressedThisFrame
                        || Keyboard.current.digit3Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = new Color(1f, 1f, 0f);
                    }
                    else if (Keyboard.current.numpad4Key.wasPressedThisFrame
                        || Keyboard.current.digit4Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = Color.green;
                    }
                    else if (Keyboard.current.numpad5Key.wasPressedThisFrame
                        || Keyboard.current.digit5Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = new Color(0f, 1f, 1f);
                    }
                    else if (Keyboard.current.numpad6Key.wasPressedThisFrame
                        || Keyboard.current.digit6Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = Color.blue;
                    }
                    else if (Keyboard.current.numpad7Key.wasPressedThisFrame
                        || Keyboard.current.digit7Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = new Color(0.6f, 0f, 1f);
                    }
                    else if (Keyboard.current.numpad8Key.wasPressedThisFrame
                        || Keyboard.current.digit8Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = Color.gray;
                    }
                    else if (Keyboard.current.numpad9Key.wasPressedThisFrame
                        || Keyboard.current.digit9Key.wasPressedThisFrame)
                    {
                        greenBackground.material.color = Color.white;
                    }
                }
            }

            #endregion

        }

        private bool greenOn;
        public void GreenBackgroundOn()
        {
            greenBackground.gameObject.SetActive(true);
            GetUI<OcgCoreUI>().CG.alpha = 0f;
            GetUI<OcgCoreUI>().CG.blocksRaycasts = false;
            CameraManager.DuelOverlay3DPlus();

            greenOn = true;
        }

        public void GreenBackgroundOff()
        {
            greenBackground.gameObject.SetActive(false);
            GetUI<OcgCoreUI>().CG.alpha = 1f;
            GetUI<OcgCoreUI>().CG.blocksRaycasts = true;
            CameraManager.DuelOverlay3DMinus();

            greenOn = false;
        }

        public void ToChat()
        {
            if (condition == Condition.Replay || inPuzzle)
                return;
            Program.instance.ui_.chatPanel.Switch();
        }

#endregion

        #region Button Function

        public void OnAnnounceCard(string input)
        {
            var datas = CardsManager.AnnounceSearch(input, ES_searchCodes);
            var max = datas.Count;
            if (max > 49)
                max = 40;
            List<GameCard> cards = new List<GameCard>();
            for (var i = 0; i < max; i++)
            {
                var p = new GPS
                {
                    controller = 0,
                    location = (uint)CardLocation.Search,
                    sequence = (uint)i,
                    position = 0
                };
                var card = GCS_Create(p);
                card.SetData(datas[i]);
                cards.Add(card);
            }
            currentPopup.whenQuitDo = () => 
            { 
                GetUI<OcgCoreUI>().ShowPopupSelectCard
                (InterString.Get("请选择需要宣言的卡片。"), cards, 1, 1, true, false); 
            };
        }

        public void ClearAnnounceCards()
        {
            List<GameCard> needClean = new List<GameCard>();
            foreach (var card in cards)
                if (card.p.location == (uint)CardLocation.Search)
                    needClean.Add(card);
            foreach (var card in needClean)
            {
                cards.Remove(card);
                card.Dispose();
            }
        }

        public void CloseConnection()
        {
            if (TcpHelper.tcpClient != null)
            {
                if (TcpHelper.tcpClient.Connected)
                {
                    TcpHelper.tcpClient.Client.Shutdown(0);
                    TcpHelper.tcpClient.Close();
                }
                TcpHelper.tcpClient = null;
            }
        }

        private bool DuelEndNeedExit()
        {
            if (Program.instance.room.duelEnded)
                return true;
            if(TcpHelper.tcpClient == null)
                return true;
            if(!TcpHelper.tcpClient.Connected)
                return true;
            if (surrendered)
            {
                if(RoomServant.Mode == 0)
                    return true;
                if (RoomServant.Mode == 1)
                    return false;
                if (RoomServant.Mode == 2)
                    return tagSurrendered;
            }

            return false;
        }

        public void OnDuelResultConfirmed(bool manual = false)
        {
            RoomServant.JoinWithReconnect = false;

            if(condition != Condition.Watch)
            {
                if (DuelEndNeedExit())
                {
                    surrendered = false;
                    RoomServant.NeedSide = false;
                    RoomServant.SideWaitingObserver = false;
                    if (Program.instance.currentSubServant != null)
                    {
                        Program.instance.currentSubServant.Hide(-1);
                        Program.instance.currentSubServant = null;
                    }
                    OnExit();
                    return;
                }
            }

            if (RoomServant.NeedSide)
            {
                RoomServant.NeedSide = false;
                MessageManager.Cast(InterString.Get("卡片历史中为您准备了对手上一局使用过的卡。"));
                //returnServant = Program.instance.editDeck;
                //Program.instance.editDeck.SwitchCondition(EditDeck.Condition.ChangeSide);
                returnServant = Program.instance.deckEditor;
                Program.instance.deckEditor.SwitchCondition(DeckEditor.Condition.ChangeSide);
                ReturnTo();
                return;
            }

            if (condition == Condition.Watch)
            {
                if (manual)
                {
                    surrendered = false;
                    RoomServant.NeedSide = false;
                    RoomServant.SideWaitingObserver = false;
                    if (Program.instance.currentSubServant != null)
                    {
                        Program.instance.currentSubServant.Hide(-1);
                        Program.instance.currentSubServant = null;
                    }
                    TcpHelper.CtosMessage_LeaveGame();
                    OnExit();
                }
                else
                {
                    if (duelEnded)
                    {
                        if (TcpHelper.tcpClient == null
                            || !TcpHelper.tcpClient.Connected)
                            OnExit();
                        else
                            Hide(0);
                    }
                    else
                    {
                        DuelBGManager.ResetFields();
                    }
                }
                return;
            }

            var selections = new List<string>
            {
                InterString.Get("投降"),
                InterString.Get("您确定要投降吗？"),
                InterString.Get("是"),
                InterString.Get("否")
            };
            UIManager.ShowPopupYesOrNo(selections, ActionSurrender, ActionCancelSurrender);
        }

        private void ActionSurrender()
        {
            onSurrenderConfirmed?.Invoke();
            onSurrenderConfirmed = null;
            surrendered = true;
            if (TcpHelper.tcpClient != null && TcpHelper.tcpClient.Connected)
            {
                TcpHelper.CtosMessage_Surrender();
                Program.instance.ExitCurrentServant();
                if (RoomServant.Mode == 2 && !tagSurrendered)
                    MessageManager.Cast(InterString.Get("您发起了投降。"));
            }
            else
                OnExit();
        }

        private void ActionCancelSurrender()
        {
            onSurrenderConfirmed = null;
        }

        #endregion

        #region Message

        public MessageDispatcher messageDispatcher = new();

        public void AddPackage(Package p)
        {
            TcpHelper.AddRecordLine(p);
            packages.Add(p);
            allPackages.Add(p);

            CheckMessageIsNeedInstanceResponse(p);
        }

        private void CheckMessageIsNeedInstanceResponse(Package p)
        {
            if(p.Function == (int)GameMessage.Win)
            {
                messageDispatcher.playerResponed = true;
            }
            else if(p.Function == (int)GameMessage.SelectIdleCmd
                && currentMessage == GameMessage.SelectIdleCmd && packages.Count > 1
                && packages[0].Function == (int)GameMessage.SelectIdleCmd)
            {
                // An explicit owner command can invalidate an idle menu while
                // the client still awaits input. Release that superseded prompt
                // without sending any of its stale choices back to the server.
                DuelBGManager?.ClearResponse();
                messageDispatcher.playerResponed = true;
            }
        }

        public void FlushPackages(List<Package> packs)
        {
            packages.Clear();
            packages = null;
            packages = packs;
            replayPlaybackEndedNotified = false;
            allPackages.Clear();
            foreach (Package p in packages)
                allPackages.Add(p);
        }

        public void SendReturn(byte[] buffer, float delay = 0f)
        {
            handler?.Invoke(buffer);
            DuelBGManager.ClearResponse();
            DOTween.To(v => { }, 0, 0, delay).OnComplete(() =>
            {
                messageDispatcher.playerResponed = true;
            });
        }

        public void OnResend()
        {
            var binaryMaster = new BinaryMaster();
            binaryMaster.writer.Write(-1);
            SendReturn(binaryMaster.Get());
        }

        public void StocMessage_TeammateSurrender()
        {
            if (surrendered)
                return;
            tagSurrendered = true;
            MessageManager.Cast(InterString.Get("队友发起了投降。"));
        }

        public void StocMessage_TimeLimit(BinaryReader r)
        {
            int player = LocalPlayer(r.ReadByte());
            r.ReadByte();
            int timeLimit = r.ReadInt16();
            TcpHelper.CtosMessage_TimeConfirm();

            if (DuelBGManager == null)
                return;
            DuelBGManager.SetTimeLimit(player, timeLimit);
        }

        public void StocMessage_Error(string error)
        {
            _ = ShowErrorMessageAsync(error);
        }
        
        private async UniTask ShowErrorMessageAsync(string error)
        {
            await UniTask.WaitWhile(() => servantUI == null);
            UniTask.ReturnToMainThread();
            GetUI<OcgCoreUI>().DuelErrorLog.Show(error);
        }

        public static bool ShouldShowPlayerMessages()
        {
            if (condition == Condition.Duel && !Config.GetBool("DuelPlayerMessage", true))
                return false;
            if (condition == Condition.Watch && !Config.GetBool("WatchPlayerMessage", true))
                return false;
            if (condition == Condition.Replay && !Config.GetBool("ReplayPlayerMessage", true))
                return false;
            return true;
        }

        public static bool ShouldShowSystemMessages()
        {
            if (condition == Condition.Duel && !Config.GetBool("DuelSystemMessage", true))
                return false;
            if (condition == Condition.Watch && !Config.GetBool("WatchSystemMessage", true))
                return false;
            if (condition == Condition.Replay && !Config.GetBool("ReplaySystemMessage", true))
                return false;
            return true;
        }

        public bool GetMessageConfig(int player)
        {
            if (player < 4 || player == 7)
            {
                if (!ShouldShowPlayerMessages())
                    return false;
            }
            else
            {
                if (!ShouldShowSystemMessages())
                    return false;
            }
            return true;
        }

        public void ForceMSquit()
        {
            var p = new Package
            {
                Function = (int)GameMessage.sibyl_quit
            };
            packages.Add(p);
        }

        public bool InIgnoranceReplay()
        {
            return condition != Condition.Duel;
        }

        public static Package GetNextPackage()
        {
            int target = 1;
            while (packages.Count > target)
            {
                if (packages[target].Function != (int)GameMessage.UpdateData
                    && packages[target].Function != (int)GameMessage.UpdateCard)
                    return packages[target];
                target++;
            }
            return null;
        }

        public static bool NextMessageIs(GameMessage message)
        {
            var p = GetNextPackage();
            if(p == null) return false;
            return p.Function == (int)message;
        }

        public static CardLocation NextMessageIsMovingToLocation()
        {
            var p = GetNextPackage();
            if (p == null)
                return CardLocation.Unknown;

            if (p.Function == (int)GameMessage.Move)
            {
                var r = p.Data.reader;
                r.BaseStream.Seek(0, 0);
                r.ReadInt32();
                r.ReadGPS();
                var to = r.ReadGPS();
                nextMoveMessageController = to.controller;
                return (CardLocation)to.location;
            }
            return CardLocation.Unknown;
        }

        public static bool NextMessageIsMovingFrom(CardLocation location)
        {
            var p = GetNextPackage();
            if (p == null)
                return false;

            if (p.Function == (int)GameMessage.Move)
            {
                var r = p.Data.reader;
                r.BaseStream.Seek(0, 0);
                r.ReadInt32();
                var from = r.ReadGPS();
                if ((from.location & (uint)location) > 0)
                    return true;
            }
            return false;

        }

        public static bool NextMessageIsMovingTo(CardLocation location, uint player)
        {
            var p = GetNextPackage();
            if (p == null)
                return false;

            if (p.Function == (int)GameMessage.Move)
            {
                var r = p.Data.reader;
                r.BaseStream.Seek(0, 0);
                r.ReadInt32();
                r.ReadGPS();
                var to = r.ReadGPS();
                if (player == to.controller && (to.location & (uint)location) > 0)
                    return true;
            }
            return false;
        }

        public static bool NextMessageIsMovingToGrave(uint player)
        {
            var count = player == 0 ? movingToMyGrave : movingToOpGrave;
            if (count >= 4)
                return false;
            if (NextMessageIsMovingTo(CardLocation.Grave, player))
                return true;
            return false;
        }

        public static bool NextMessageIsMovingToExclude(uint player)
        {
            var count = player == 0 ? movingToMyExclude : movingToOpExclude;
            if (count >= 4)
                return false;
            if (NextMessageIsMovingTo(CardLocation.Removed, player))
                return true;
            return false;
        }

        public static bool CanSyncNextMove(GPS from, GPS to)
        {
            if (!from.InLocation(CardLocation.Grave)
                && to.InLocation(CardLocation.Grave))
            {
                var location = NextMessageIsMovingToLocation();
                if(location == CardLocation.Grave)
                    return NextMessageIsMovingToGrave(nextMoveMessageController);
                else if (location == CardLocation.Removed)
                    return NextMessageIsMovingToExclude(nextMoveMessageController);
                else
                    return false;
            }
            else if (!from.InLocation(CardLocation.Removed)
                && to.InLocation(CardLocation.Removed))
            {
                var location = NextMessageIsMovingToLocation();
                if (location == CardLocation.Removed)
                    return NextMessageIsMovingToExclude(nextMoveMessageController);
                else if (location == CardLocation.Grave)
                    return NextMessageIsMovingToGrave(nextMoveMessageController);
                else
                    return false;
            }
            return false;
        }

        private async UniTask ProcessMessage()
        {
            messageDispatcher.Dispose();
            replayPlaybackEndedNotified = false;

            try
            {
                while (showing)
                {
                    if(!messageDispatcher.duel.duelBGManager.loaded)
                    {
                        await UniTask.Yield();
                        continue;
                    }

                    await UniTask.WaitWhile(() => pause);

                    if (packages.Count == 0)
                    {
                        await UniTask.Yield();
                        continue;
                    }

                    NoMoreWait = false;

                    var currentPackage = packages[0];
                    currentMessage = (GameMessage)currentPackage.Function;

                    //if (currentMessage != GameMessage.UpdateData)
                    //    Debug.Log($"GameMessage: {currentMessage}");

                    try
                    {
                        var messageTask = messageDispatcher.Process(packages[0]);
                        var breakTask = UniTask.WaitUntil(() => NoMoreWait);
                        await UniTask.WhenAny(messageTask, breakTask);
                    }
                    catch (Exception e) { Debug.Log(e); }

                    lastMessage = currentMessage;
                    if (packages.Count == 0)
                    {
                        NotifyReplayPlaybackEnded();
                        break;
                    }
                    packages.RemoveAt(0);

                    if (condition == Condition.Replay && packages.Count == 0)
                    {
                        NotifyReplayPlaybackEnded();
                        break;
                    }
                }
            }
            catch (Exception e) { Debug.Log(e); }
        }

        private bool replayPlaybackEndedNotified;

        public void NotifyReplayPlaybackEnded()
        {
            if (condition != Condition.Replay || replayPlaybackEndedNotified)
                return;

            replayPlaybackEndedNotified = true;
            MessageManager.Cast(InterString.Get("回放播放结束。"));
        }

        #endregion

        #region PracticalizeTools

        public static int LocalPlayer(int p)
        {
            if (p == 0 || p == 1)
            {
                if (isFirst)
                    return p;
                return 1 - p;
            }
            return p;
        }

        public static int[] GetSelectLevelSum(List<GameCard> cards)
        {
            var sum1 = 0;
            foreach (var card in cards)
                sum1 += card.levelForSelect_1;
            var sum2 = 0;
            foreach (var card in cards)
                sum2 += card.levelForSelect_2;
            return new int[] { sum1, sum2 };
        }

        public static bool CheckSelectableInSum(List<GameCard> cards, GameCard card, List<GameCard> selectedCards, int max)
        {
            if (selectedCards.Count >= max)
                return false;
            bool returnValue = false;
            var sum = GetSelectLevelSum(selectedCards);
            if (sum[0] + card.levelForSelect_1 == ES_level || sum[1] + card.levelForSelect_2 == ES_level)
                return true;
            if (sum[0] + card.levelForSelect_1 > ES_level || sum[1] + card.levelForSelect_2 > ES_level)
                return false;

            var newSelectedCards = new List<GameCard>(selectedCards) { card };
            foreach (var c in cards)
                if (!newSelectedCards.Contains(c))
                {
                    returnValue = CheckSelectableInSum(cards, c, newSelectedCards, max);
                    if (returnValue)
                        return true;
                }
            return returnValue;
        }

        public static bool TypeMatchReason(int type, int reason)
        {
            if ((type & (uint)CardType.Ritual) > 0 && (reason & (uint)CardReason.Ritual) > 0)
                return true;
            if ((type & (uint)CardType.Fusion) > 0 && (reason & (uint)CardReason.Fusion) > 0)
                return true;
            if ((type & (uint)CardType.Synchro) > 0 && (reason & (uint)CardReason.Synchro) > 0)
                return true;
            if ((type & (uint)CardType.Xyz) > 0 && (reason & (uint)CardReason.Xyz) > 0)
                return true;
            if ((type & (uint)CardType.Link) > 0 && (reason & (uint)CardReason.Link) > 0)
                return true;
            return false;
        }

        public GameCard GCS_Create(GPS p, bool temp = false)
        {
            var c = Program.instance.container_3D.gameObject.AddComponent<GameCard>();
            c.p = p;
            cards.Add(c);

            if (temp)
                tempCards.Add(c);
            return c;
        }

        public GameCard GCS_Get(GPS p)
        {
            GameCard c = null;
            if ((p.location & (uint)CardLocation.Overlay) > 0)
            {
                for (var i = 0; i < cards.Count; i++)
                    if (cards[i].p.location == p.location)
                        if (cards[i].p.controller == p.controller)
                            if (cards[i].p.sequence == p.sequence)
                                if (cards[i].p.position == p.position)
                                {
                                    c = cards[i];
                                    break;
                                }
            }
            else
            {
                for (var i = 0; i < cards.Count; i++)
                    if (cards[i].p.location == p.location)
                        if (cards[i].p.controller == p.controller)
                            if (cards[i].p.sequence == p.sequence)
                            {
                                c = cards[i];
                                break;
                            }
            }

            if (p.location == 0) c = null;
            return c;
        }

        public List<GameCard> GCS_GetLocationCards(int controller, int location)
        {
            var cardsInLocation = new List<GameCard>();
            for (var i = 0; i < cards.Count; i++)
                if (!tempCards.Contains(cards[i]))
                    if (cards[i].p.location == location)
                        if (cards[i].p.controller == controller)
                            cardsInLocation.Add(cards[i]);
            return cardsInLocation;
        }

        public List<GameCard> GCS_GetOverlays(GameCard c)
        {
            var overlays = new List<GameCard>();
            if (c != null)
                if ((c.p.location & (uint)CardLocation.Overlay) == 0)
                    for (var i = 0; i < cards.Count; i++)
                        if ((cards[i].p.location & (uint)CardLocation.Overlay) > 0)
                            if (cards[i].p.controller == c.p.controller)
                                if ((cards[i].p.location | (uint)CardLocation.Overlay) ==
                                    (c.p.location | (uint)CardLocation.Overlay))
                                    if (cards[i].p.sequence == c.p.sequence)
                                        overlays.Add(cards[i]);
            return overlays;
        }

        public void GCS_CreateBundle(int count, int controller, CardLocation location)
        {
            for (var i = 0; i < count; i++)
            {
                GCS_Create(new GPS
                {
                    controller = (uint)controller,
                    location = (uint)location,
                    position = (int)CardPosition.FaceDownAttack,
                    sequence = (uint)i
                });
            }
        }

        public List<GameCard> GCS_ResizeBundle(int count, int player, CardLocation location)
        {
            var cardBow = new List<GameCard>();
            var waterOutOfBow = new List<GameCard>();
            for (var i = 0; i < cards.Count; i++)
                if ((cards[i].p.location & (uint)location) > 0)
                    if (cards[i].p.controller == player)
                    {
                        if (cardBow.Count < count)
                            cardBow.Add(cards[i]);
                        else
                            waterOutOfBow.Add(cards[i]);
                    }
            foreach (var card in waterOutOfBow)
            {
                cards.Remove(card);
                if ((card.p.location & (uint)CardLocation.Hand) > 0)
                    card.AnimationShuffle(0.15f);
                else
                    card.Dispose();
            }
            while (cardBow.Count < count)
            {
                var card = GCS_Create(new GPS
                {
                    controller = (uint)player,
                    location = (uint)location,
                    position = (int)CardPosition.FaceDownAttack,
                    sequence = (uint)cardBow.Count
                });
                cardBow.Add(card);
            }
            foreach (var card in cardBow)
            {
                card.EraseData();
                card.p.position = (int)CardPosition.FaceDownAttack;
            }
            return cardBow;
        }

        public void ArrangeCards()
        {
            //sort 
            cards.Sort((left, right) =>
            {
                var a = 1;
                if (left.p.controller > right.p.controller)
                {
                    a = 1;
                }
                else if (left.p.controller < right.p.controller)
                {
                    a = -1;
                }
                else
                {
                    if (left.p.location == (uint)CardLocation.Hand && right.p.location != (uint)CardLocation.Hand)
                    {
                        a = -1;
                    }
                    else if (left.p.location != (uint)CardLocation.Hand && right.p.location == (uint)CardLocation.Hand)
                    {
                        a = 1;
                    }
                    else
                    {
                        if ((left.p.location | (uint)CardLocation.Overlay) >
                            (right.p.location | (uint)CardLocation.Overlay))
                        {
                            a = -1;
                        }
                        else if ((left.p.location | (uint)CardLocation.Overlay) <
                                 (right.p.location | (uint)CardLocation.Overlay))
                        {
                            a = 1;
                        }
                        else
                        {
                            if (left.p.sequence > right.p.sequence)
                            {
                                a = 1;
                            }
                            else if (left.p.sequence < right.p.sequence)
                            {
                                a = -1;
                            }
                            else
                            {
                                if ((left.p.location & (uint)CardLocation.Overlay) >
                                    (right.p.location & (uint)CardLocation.Overlay))
                                {
                                    a = -1;
                                }
                                else if ((left.p.location & (uint)CardLocation.Overlay) <
                                         (right.p.location & (uint)CardLocation.Overlay))
                                {
                                    a = 1;
                                }
                                else
                                {
                                    if (left.p.position > right.p.position)
                                        a = 1;
                                    else if (left.p.position < right.p.position) a = -1;
                                }
                            }
                        }
                    }
                }
                return a;
            });

            /////rebuild
            uint preController = 9999;
            uint preLocation = 9999;
            uint preSequence = 9999;

            uint sequenceWriter = 0;
            var positionWriter = 0;

            for (var i = 0; i < cards.Count; i++)
                if (cards[i])
                {
                    if (preController != cards[i].p.controller) sequenceWriter = 0;
                    if ((preLocation | (uint)CardLocation.Overlay) != (cards[i].p.location | (uint)CardLocation.Overlay))
                        sequenceWriter = 0;
                    if (preSequence != cards[i].p.sequence) positionWriter = 0;

                    if ((cards[i].p.location & (uint)CardLocation.MonsterZone) == 0)
                        if ((cards[i].p.location & (uint)CardLocation.SpellZone) == 0)
                            cards[i].p.sequence = sequenceWriter;


                    if ((cards[i].p.location & (uint)CardLocation.Overlay) > 0)
                    {
                        cards[i].p.position = positionWriter;
                        positionWriter++;
                    }
                    else
                    {
                        sequenceWriter++;
                    }

                    preController = cards[i].p.controller;
                    preLocation = cards[i].p.location;
                    preSequence = cards[i].p.sequence;
                }
        }

        public int GetMyHandCount()
        {
            if (needRefreshMyHand)
            {
                myHandCards = new List<GameCard>(myPreHandCards);
                foreach (var card in cards)
                    if (card.p.controller == 0)
                        if ((card.p.location & (uint)CardLocation.Hand) > 0)
                            if (!myHandCards.Contains(card))
                                myHandCards.Add(card);
                needRefreshMyHand = false;
            }
            return myHandCards.Count;
        }

        public int GetOpHandCount()
        {
            if (needRefreshOpHand)
            {
                opHandCards = new List<GameCard>(opPreHandCards);
                foreach (var card in cards)
                    if (card.p.controller != 0)
                        if ((card.p.location & (uint)CardLocation.Hand) > 0)
                            if (!opHandCards.Contains(card))
                                opHandCards.Add(card);
                needRefreshOpHand = false;
            }
            return opHandCards.Count;
        }

        public int GetLocationCardCount(CardLocation location, uint controller)
        {
            int count = 0;
            foreach (var card in cards)
                if ((card.p.location & (uint)location) > 0 && card.p.controller == controller)
                    count++;
            return count;
        }

        public Package GetNamePacket()
        {
            var p__ = new Package
            {
                Function = (int)GameMessage.sibyl_name,
                Data = new BinaryMaster()
            };
            p__.Data.writer.WriteUnicode(name_0, 50);
            p__.Data.writer.WriteUnicode(name_0_tag, 50);
            p__.Data.writer.WriteUnicode(name_0_c != "" ? name_0_c : name_0, 50);
            p__.Data.writer.WriteUnicode(name_1, 50);
            p__.Data.writer.WriteUnicode(name_1_tag, 50);
            p__.Data.writer.WriteUnicode(name_1_c != "" ? name_1_c : name_1, 50);
            p__.Data.writer.Write(MasterRule);
            return p__;
        }

        public bool GetAutoInfo()
        {
            if (condition == Condition.Duel
                && !Config.GetBool("DuelAutoInfo", true))
                return false;
            if (condition == Condition.Watch
                && !Config.GetBool("WatchAutoInfo", true))
                return false;
            if (condition == Condition.Replay
                && !Config.GetBool("ReplayAutoInfo", true))
                return false;

            return true;
        }

        public void RefreshAllCardsLabel()
        {
            if (!showing)
                return;
            foreach (var card in cards)
            {
                card.RefreshLabel();
            }
        }

        public bool CurrentChainDisabled(int currentChain)
        {
            for (int i = 0; i < packages.Count; i++)
            {
                if ((GameMessage)packages[i].Function == GameMessage.ChainDisabled)
                {
                    var r = packages[i].Data.reader;
                    r.BaseStream.Seek(0, 0);
                    if (r.ReadByte() == currentChain)
                        return true;
                }
                if ((GameMessage)packages[i].Function == GameMessage.ChainSolved)
                    return false;
            }
            return false;
        }

        public int GetNextConfirmedCardCode()
        {
            for (int i = 0; i < packages.Count; i++)
            {
                if ((GameMessage)packages[i].Function == GameMessage.ConfirmCards)
                {
                    var reader = packages[i].Data.reader;
                    reader.BaseStream.Seek(0, 0);
                    reader.ReadByte();
                    if (condition != Condition.Replay || CurrentReplayUseYRP2)
                        reader.ReadByte();
                    reader.ReadByte();

                    return reader.ReadInt32();
                }
            }
            return 0;
        }

        public int GetUpdateDataIdByGameCard(GameCard card)
        {
            for (int i = 0; i < packages.Count; i++)
            {
                if ((GameMessage)packages[i].Function == GameMessage.UpdateData)
                {
                    var reader = packages[i].Data.reader;
                    reader.BaseStream.Seek(0, 0);
                    var player = LocalPlayer(reader.ReadChar());
                    var location = reader.ReadChar();
                    if (player != card.p.controller)
                        continue;
                    if((location & card.p.location) == 0)
                        continue;
                    while (true)
                    {
                        var len = reader.ReadInt32();
                        if (len == 4) continue;
                        var pos = reader.BaseStream.Position;

                        var flag = reader.ReadInt32();
                        var code = 0;
                        if((flag & (int)Query.Code) != 0)
                            code = reader.ReadInt32();
                        if ((flag & (int)Query.Position) != 0)
                        {
                            var gps = reader.ReadGPS();
                            var cardToRefresh = Program.instance.ocgcore.GCS_Get(gps);
                            if (cardToRefresh != null && cardToRefresh == card)
                                return code;
                            else
                            {
                                reader.BaseStream.Position = pos + len - 4;
                                continue;
                            }
                        }
                    }
                }
            }

            return 0;
        }

        #endregion

        #region Practicalize

        public void Chat(int player, string content)
        {
            if (!GetMessageConfig(player))
                return;
            if (player == 7 || player < 4)
                MessageManager.Cast(ChatPanel.GetPlayerName(player) + ": " + content);
            else
                MessageManager.Cast(content);
        }


        public static string lastDuelLog;
        public static void PrintDuelLog(string content)
        {
            lastDuelLog = content;
            if (!ShouldShowSystemMessages())
                return;
            MessageManager.Cast(content);
        }

        public void SetFace()
        {
            if (condition == Condition.Duel)
            {
                var selfType = RoomServant.SelfType;
                if (GetUI<OcgCoreUI>().TextPlayer0Name.text == name_0)
                {
                    if (isTag)
                    {
                        if (selfType == 0 || selfType == 2)
                        {
                            GetUI<OcgCoreUI>().AvatarPlayer0.material = Appearance.duelFrameMat0;
                            SetFaceWhenCharaOff(Appearance.duelFace0, 0);
                        }
                        else
                        {
                            GetUI<OcgCoreUI>().AvatarPlayer0.material = Appearance.duelFrameMat0Tag;
                            SetFaceWhenCharaOff(Appearance.duelFace0Tag, 0);
                        }
                    }
                    else
                    {
                        GetUI<OcgCoreUI>().AvatarPlayer0.material = Appearance.duelFrameMat0;
                        SetFaceWhenCharaOff(Appearance.duelFace0, 0);
                    }
                }
                else
                {
                    if (selfType == 0 || selfType == 2)
                    {
                        GetUI<OcgCoreUI>().AvatarPlayer0.material = Appearance.duelFrameMat0Tag;
                        SetFaceWhenCharaOff(Appearance.duelFace0Tag, 0);
                    }
                    else
                    {
                        GetUI<OcgCoreUI>().AvatarPlayer0.material = Appearance.duelFrameMat0;
                        SetFaceWhenCharaOff(Appearance.duelFace0, 0);
                    }
                }
                if (GetUI<OcgCoreUI>().TextPlayer1Name.text == name_1)
                {
                    GetUI<OcgCoreUI>().AvatarPlayer1.material = Appearance.duelFrameMat1;
                    SetFaceWhenCharaOff(Appearance.duelFace1, 1);
                }
                else
                {
                    GetUI<OcgCoreUI>().AvatarPlayer1.material = Appearance.duelFrameMat1Tag;
                    SetFaceWhenCharaOff(Appearance.duelFace1Tag, 1);
                }

                _ = ApplyOnlineOpponentFaceAsync();
            }
            else if (condition == Condition.Watch)
            {
                if (GetUI<OcgCoreUI>().TextPlayer0Name.text == name_0)
                {
                    GetUI<OcgCoreUI>().AvatarPlayer0.material = Appearance.watchFrameMat0;
                    SetFaceWhenCharaOff(Appearance.watchFace0, 0);
                }
                else
                {
                    GetUI<OcgCoreUI>().AvatarPlayer0.material = Appearance.watchFrameMat0Tag;
                    SetFaceWhenCharaOff(Appearance.watchFace0Tag, 0);
                }
                if (GetUI<OcgCoreUI>().TextPlayer1Name.text == name_1)
                {
                    GetUI<OcgCoreUI>().AvatarPlayer1.material = Appearance.watchFrameMat1;
                    SetFaceWhenCharaOff(Appearance.watchFace1, 1);
                }
                else
                {
                    GetUI<OcgCoreUI>().AvatarPlayer1.material = Appearance.watchFrameMat1Tag;
                    SetFaceWhenCharaOff(Appearance.watchFace1Tag, 1);
                }
            }
            else if (condition == Condition.Replay)
            {
                if (GetUI<OcgCoreUI>().TextPlayer0Name.text == name_0)
                {
                    GetUI<OcgCoreUI>().AvatarPlayer0.material = Appearance.replayFrameMat0;
                    SetFaceWhenCharaOff(Appearance.replayFace0, 0);
                }
                else
                {
                    GetUI<OcgCoreUI>().AvatarPlayer0.material = Appearance.replayFrameMat0Tag;
                    SetFaceWhenCharaOff(Appearance.replayFace0Tag, 0);
                }
                if (GetUI<OcgCoreUI>().TextPlayer1Name.text == name_1)
                {
                    GetUI<OcgCoreUI>().AvatarPlayer1.material = Appearance.replayFrameMat1;
                    SetFaceWhenCharaOff(Appearance.replayFace1, 1);
                }
                else
                {
                    GetUI<OcgCoreUI>().AvatarPlayer1.material = Appearance.replayFrameMat1Tag;
                    SetFaceWhenCharaOff(Appearance.replayFace1Tag, 1);
                }
            }

            _ = SetMyCardFace();
        }

        private async UniTask ApplyOnlineOpponentFaceAsync()
        {
            if (condition != Condition.Duel)
                return;

            var useTagOpponent = isTag && GetUI<OcgCoreUI>().TextPlayer1Name.text != name_1;
            if (!RoomServant.TryGetOnlineAppearanceForOpponent(useTagOpponent, out var appearance))
            {
                if (!useTagOpponent || !RoomServant.TryGetOnlineAppearanceForOpponent(false, out appearance))
                    return;
            }

            var iconPlayer = useTagOpponent ? 3 : 1;
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

            var latestUseTagOpponent = isTag && GetUI<OcgCoreUI>().TextPlayer1Name.text != name_1;
            if (latestUseTagOpponent != useTagOpponent)
                return;

            if (!RoomServant.TryGetOnlineAppearanceForOpponent(useTagOpponent, out var latest))
            {
                if (!useTagOpponent || !RoomServant.TryGetOnlineAppearanceForOpponent(false, out latest))
                    return;
            }
            if (latest.Face != appearance.Face || latest.Frame != appearance.Frame)
                return;

            if (frameMaterial != null)
                GetUI<OcgCoreUI>().AvatarPlayer1.material = frameMaterial;
            if (faceSprite != null)
                SetFaceWhenCharaOff(faceSprite, 1);
        }

        private async UniTask SetMyCardFace()
        {
            if (MyCard.account == null || !mycardDuel)
                return;

            var avatar = await MyCard.GetAvatarAsync(GetUI<OcgCoreUI>().TextPlayer0Name.text);
            if (avatar != null)
                SetFaceWhenCharaOff(TextureManager.Texture2Sprite(avatar), 0);

            avatar = await  MyCard.GetAvatarAsync(GetUI<OcgCoreUI>().TextPlayer1Name.text);
            if (avatar != null)
                SetFaceWhenCharaOff(TextureManager.Texture2Sprite(avatar), 1);
        }


        private Sprite mySprite;
        private Sprite opSprite;

        private void SetFaceWhenCharaOff(Sprite sprite, int player)
        {
            if (player == 0)
                mySprite = sprite;
            else
                opSprite = sprite;

            if (!charaFaceSetting)
            {
                GetUI<OcgCoreUI>().AvatarPlayer0.sprite = mySprite;
                GetUI<OcgCoreUI>().AvatarPlayer1.sprite = opSprite;
            }
        }

        public void CloseCharaFace()
        {
            if (!charaFaceSetting)
                return;
            charaFaceSetting = false;
            SetFaceWhenCharaOff(mySprite, 0);
        }

        public void CheckCharaFace()
        {
            if (!showing)
                return;

            if (NeedVoice())
                SetCharacterDefaultFace();
            else
                CloseCharaFace();
        }

        private readonly Sequence[] lpAnimations = new Sequence[2];
        private readonly Tween[] lpCounters = new Tween[2];

        public void SetLP(int player, int val, bool first = false)
        {
            if (first)
            {
                for (int i = 0; i < 2; i++)
                {
                    lpAnimations[i]?.Kill();
                    lpCounters[i]?.Kill();
                }
                GetUI<OcgCoreUI>().TextPlayer0LP.text = life0.ToString();
                GetUI<OcgCoreUI>().TextPlayer1LP.text = life1.ToString();
            }
            else
            {
                AnimationLpChange(player, val);
            }
        }

        private Sequence AnimationLpChange(int player, int val)
        {
            // Messages can arrive before the previous count finishes. An older
            // tween must never overwrite the newer, authoritative LP total.
            lpAnimations[player]?.Kill();
            lpCounters[player]?.Kill();
            TextMeshProUGUI text;
            int targetLP;
            if (player == 0)
            {
                text = GetUI<OcgCoreUI>().TextPlayer0LP;
                targetLP = life0;
            }
            else
            {
                text = GetUI<OcgCoreUI>().TextPlayer1LP;
                targetLP = life1;
            }

            int origin = targetLP - val;
            var sequence = DOTween.Sequence();
            lpAnimations[player] = sequence;
            var obj = ABLoader.LoadMasterDuelGameObject("DuelLpText");
            sequence.OnKill(() => { if (obj != null) Destroy(obj); });
            text.transform.localScale = Vector3.one;
            text.color = Color.white;
            obj.GetComponent<TextMeshProUGUI>().text = Math.Abs(val).ToString();
            obj.transform.SetParent(GetUI<OcgCoreUI>().RectPopup, false);
            var uiWidth = Screen.width * (float)1080 / Screen.height;
            Color color = DuelLog.damageColor;
            string seType = "COUNT";
            float fontSize = 120;
            if (val > 0)
            {
                color = DuelLog.recoverColor;
                seType = "RECOVERY";
            }

            obj.GetComponent<TextMeshProUGUI>().color = color;
            if (player == 0)
            {
                obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -400);
                obj.transform.localScale = Vector3.zero;
                float targetX = -(uiWidth / 2 - 325);
                sequence.Append(obj.transform.DOScale(1, 0.1f));
                sequence.AppendInterval(0.6f);
                sequence.Append(obj.GetComponent<RectTransform>().DOAnchorPosX(targetX, 0.2f));
                sequence.Join(DOTween.To(() => fontSize, x =>
                {
                    fontSize = x;
                    obj.GetComponent<TextMeshProUGUI>().fontSize = (int)fontSize;
                }, 40, 0.2f));
                sequence.Append(obj.GetComponent<RectTransform>().DOAnchorPosY(-490, 0.2f));
            }
            else
            {
                obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 400);
                obj.transform.localScale = Vector3.zero;
                float targetX = uiWidth / 2 - 225;
                sequence.Append(obj.transform.DOScale(1, 0.1f));
                sequence.AppendInterval(0.6f);
                sequence.Append(obj.GetComponent<RectTransform>().DOAnchorPosX(targetX, 0.2f));
                sequence.Join(DOTween.To(() => fontSize, x =>
                {
                    fontSize = x;
                    obj.GetComponent<TextMeshProUGUI>().fontSize = (int)fontSize;
                }, 40, 0.2f));
                sequence.Append(obj.GetComponent<RectTransform>().DOAnchorPosY(450, 0.2f));
            }
            sequence.Join(obj.GetComponent<TextMeshProUGUI>().DOFade(0, 0.2f).OnComplete(() =>
            {
                AudioManager.PlaySE("SE_LP_" + seType + (player == 0 ? "_PLAYER" : "_RIVAL"));
                int displayed;
                float flp = int.TryParse(text.text, out displayed) ? displayed : origin;
                lpCounters[player] = DOTween.To(() => flp, x => { flp = x; text.text = ((int)flp).ToString(); }, Mathf.Max(0, targetLP), 1.2f);
                Destroy(obj);
            }));

            sequence.Append(text.DOColor(color, 0.1f));
            sequence.Join(text.transform.DOScale(1.3f, 0.2f));
            sequence.AppendInterval(0.8f);
            sequence.Append(text.transform.DOScale(1f, 0.2f));
            sequence.Join(text.DOColor(Color.white, 0.2f));

            return sequence;
        }

        private void RefreshHandCardPositionInstant()
        {
            hideMyHandCard = false;
            if (showing)
                foreach (var card in cards)
                    card.SetHandDefault();
        }

        public void RefreshHandCardPosition()
        {
            if (showing)
                foreach (var card in cards)
                    card.SetHandToDefault();
        }

        public void RefreshMyHandCardPosition()
        {
            if (showing)
                foreach (var card in cards)
                    if(card.p.InMyControl())
                        card.SetHandToDefault();
        }

        public void RefreshOpHandCardPosition()
        {
            if (showing)
                foreach (var card in cards)
                    if (!card.p.InMyControl())
                        card.SetHandToDefault();
        }


        string fieldHint;
        int fieldMin;
        int fieldMax;
        bool fieldExitable;
        bool fieldSendable;
        [HideInInspector] public int fieldCounterCount;

        public void FieldSelect(string hint, List<GameCard> cards, int min, int max, bool exitable, bool sendable)
        {
            foreach (var place in places)
                place.InitializeSelectCardInThisZone(cards);
            fieldHint = string.IsNullOrEmpty(hint) ? InterString.Get("请选择卡片") : hint;
            fieldMin = min;
            fieldMax = max;
            fieldExitable = exitable;
            fieldSendable = sendable;
            fieldCounterCount = 0;

            if (currentMessage == GameMessage.SelectCard
                || currentMessage == GameMessage.SelectCounter)
                GetUI<OcgCoreUI>().SetHint(fieldHint + ": " + 0 + Program.STRING_SLASH + fieldMax);
            else if (currentMessage == GameMessage.SelectSum)
            {
                if (!ES_overFlow)
                    foreach (var place in places)
                        if (place.cardSelecting)
                            if (!place.cardSelected)
                                if (CheckSelectableInSum(cardsInSelection, place.cookieCard, cardsMustBeSelected, ES_max + cardsMustBeSelected.Count))
                                    place.CardInThisZoneSelectable();
                                else
                                    place.CardInThisZoneUnselectable();
                GetUI<OcgCoreUI>().SetHint(fieldHint + ": " + GetSelectLevelSum(cardsMustBeSelected)[0] + Program.STRING_SLASH + ES_level);
            }
            else
            {
                if (!string.IsNullOrEmpty(fieldHint))
                    GetUI<OcgCoreUI>().SetHint(fieldHint);
            }

            RefreshButton();
        }

        public void FieldSelectRefresh(GameCard card)
        {
            var selected = new List<GameCard>();
            foreach (var place in places)
                if (place.cardSelecting)
                    if (place.cardSelected)
                        selected.Add(place.cookieCard);
            if (currentMessage == GameMessage.SelectSum)
            {
                var sum = GetSelectLevelSum(selected);
                if ((ES_overFlow && (ES_level <= sum[0] || ES_level <= sum[1]))
                    ||
                    (!ES_overFlow && (ES_level == sum[0] || ES_level == sum[1])))
                    fieldSendable = true;
                else
                    fieldSendable = false;
                if (!ES_overFlow)
                {
                    if (sum[0] == ES_level || sum[1] == ES_level)
                    {
                        FieldSelectedSend();
                        return;
                    }
                    else
                    {
                        foreach (var place in places)
                            if (place.cardSelecting)
                                if (!place.cardSelected)
                                    if (CheckSelectableInSum(cardsInSelection, place.cookieCard, selected, ES_max + cardsMustBeSelected.Count))
                                        place.CardInThisZoneSelectable();
                                    else
                                        place.CardInThisZoneUnselectable();
                    }
                }
                RefreshButton();
                GetUI<OcgCoreUI>().SetHint(fieldHint + ": " + GetSelectLevelSum(selected)[0] + Program.STRING_SLASH + ES_level);
            }
            else if (currentMessage == GameMessage.SelectCounter)
            {
                fieldCounterCount++;
                card.counterSelected++;
                GetUI<OcgCoreUI>().SetHint(fieldHint + ": " + fieldCounterCount + Program.STRING_SLASH + fieldMax);
                if (fieldCounterCount == ES_min)
                {
                    FieldSelectedSend();
                    return;
                }
                foreach (var place in places)
                    if (place.cardSelecting)
                    {
                        if (place.cookieCard.counterCanCount > place.cookieCard.counterSelected)
                            place.CardInThisZoneSelectable();
                        else
                            place.CardInThisZoneUnselectable();
                    }
            }
            else if (currentMessage == GameMessage.SelectTribute)
            {
                var sum = 0;
                foreach (var c in selected)
                    sum += c.levelForSelect_1;
                if (selected.Count >= fieldMax)
                    FieldSelectedSend();
                else if (sum >= fieldMin)
                {
                    fieldSendable = true;
                    RefreshButton();
                }
            }
            else
            {
                if (selected.Count >= fieldMin)
                    fieldSendable = true;
                else
                    fieldSendable = false;
                if (selected.Count >= fieldMax)
                    FieldSelectedSend();
                else
                {
                    foreach (var place in places)
                        if (place.cardSelecting)
                            if (!place.cardSelected)
                                place.CardInThisZoneSelectable();
                    RefreshButton();
                }
                if (currentMessage == GameMessage.SelectCard)
                    GetUI<OcgCoreUI>().SetHint(fieldHint + ": " + selected.Count + Program.STRING_SLASH + fieldMax);
            }
        }

        private void RefreshButton()
        {
            if (fieldSendable)
            {
                btnConfirm.Show();
                if (currentMessage == GameMessage.SelectUnselect)
                    btnCancel.Hide();
            }
            else
                btnConfirm.Hide();
            if (fieldExitable)
            {
                if (currentMessage == GameMessage.SelectUnselect && fieldSendable)
                {
                }
                else
                    btnCancel.Show();
            }
            else
                btnCancel.Hide();
        }

        public void FieldSelectedSend()
        {
            var selected = new List<GameCard>();
            foreach (var place in places)
                if (place.cardSelecting)
                    if (place.cardSelected)
                        selected.Add(place.cookieCard);
            var binaryMaster = new BinaryMaster();
            if (currentMessage == GameMessage.SelectUnselect && selected.Count == 0)
                binaryMaster.writer.Write(-1);
            else if (currentMessage == GameMessage.SelectCounter)
                for (var i = 0; i < cardsInSelection.Count; i++)
                    binaryMaster.writer.Write((short)cardsInSelection[i].counterSelected);
            else if (currentMessage == GameMessage.SelectSum)
            {
                binaryMaster.writer.Write((byte)selected.Count);
                foreach (var card in cardsMustBeSelected)
                    binaryMaster.writer.Write((byte)card.selectPtr);
                foreach (var card in selected)
                    if (!cardsMustBeSelected.Contains(card))
                        binaryMaster.writer.Write((byte)card.selectPtr);
            }
            else
            {
                binaryMaster.writer.Write((byte)selected.Count);
                foreach (var card in selected)
                    binaryMaster.writer.Write((byte)card.selectPtr);
            }
            SendReturn(binaryMaster.Get());
        }

        public void FieldSelectedCancel()
        {
            if (currentMessage == GameMessage.SelectCounter)
            {
                foreach (var card in cardsInSelection)
                    card.counterSelected = 0;
                fieldCounterCount = 0;
                GetUI<OcgCoreUI>().SetHint(fieldHint + ": " + 0 + Program.STRING_SLASH + fieldMax);
                foreach (var place in places)
                    if (place.cardSelecting)
                        place.CardInThisZoneSelectable();
            }
            else
            {
                var binaryMaster = new BinaryMaster();
                binaryMaster.writer.Write(-1);
                SendReturn(binaryMaster.Get());
            }
        }

        public void SetExDeckTop(GameCard card)
        {
            if (DuelBGManager == null)
                return;
            DuelBGManager.SetExDeckTop(card);
        }

        public void UpdateExDeckTop(uint controller)
        {
            if (DuelBGManager == null)
                return;
            DuelBGManager.UpdateExDeckTop(controller);
        }

        public void SetBgTimeScale(float timeScale)
        {
            if (DuelBGManager == null)
                return;
            DuelBGManager.SetBgTimeScale(timeScale);
        }

        public void PlayGraveEffect(GPS p, bool isIn)
        {
            if (DuelBGManager == null)
                return;
            DuelBGManager.PlayGraveEffect(p, isIn);
        }

        public long GetAllAtk(bool mySide)
        {
            long allAttack = 0;
            var monsters = GCS_GetLocationCards(mySide ? 0 : 1, (int)CardLocation.MonsterZone);
            foreach (var card in monsters)
                if ((card.p.position & (uint)CardPosition.FaceUpAttack) > 0)
                    allAttack += Card.NormalizeBattleValue(card.GetData().Attack, false);
            return allAttack;
        }

        private bool PlayerLosing()
        {
            if (myTurn)
            {
                if (GetAllAtk(true) - GetAllAtk(false) > life1)
                {
                    var defenseCount = 0;
                    var monsters = GCS_GetLocationCards(1, (int)CardLocation.MonsterZone);
                    foreach (var card in monsters)
                        if ((card.p.position & (uint)CardPosition.Defence) > 0)
                            defenseCount++;
                    if (defenseCount == 0)
                        return true;
                }
            }
            else if (!myTurn)
            {
                if (GetAllAtk(false) - GetAllAtk(true) > life0)
                {
                    var defenseCount = 0;
                    var monsters = GCS_GetLocationCards(0, (int)CardLocation.MonsterZone);
                    foreach (var card in monsters)
                        if ((card.p.position & (uint)CardPosition.Defence) > 0)
                            defenseCount++;
                    if (defenseCount == 0)
                        return true;
                }
            }
            return false;
        }

        public void ShowEquipLine(Vector3 start, Vector3 end)
        {
            if (DuelBGManager == null)
                return;

            DuelBGManager.ShowEquipLine(start, end);
        }

        public void ShowTargetLines(Vector3 start, List<GameCard> targets)
        {
            if (DuelBGManager == null)
                return;

            DuelBGManager.ShowTargetLines(start, targets);
        }

        public Transform GetFieldTransform(uint player)
        {
            return (player == 0 ? DuelBGManager.field0Manager.transform : DuelBGManager.field1Manager.transform);
        }

        public ElementObjectManager GetDeckModel(uint player, CardLocation location)
        {
            if(location == CardLocation.Deck)
                return (player == 0 ? DuelBGManager.myDeck : DuelBGManager.opDeck);
            else
                return (player == 0 ? DuelBGManager.myExtra : DuelBGManager.opExtra);
        }

        public void SetDeckModelActive(ElementObjectManager deck, bool active)
        {
            deck.GetElement("CardShuffleTop").SetActive(active);
        }

        #endregion

        #region Voice

        public ChatItemHandler duelChat0;
        public ChatItemHandler duelChat1;

        public bool NeedVoice()
        {
            return Config.GetBool(condition + "Voice", false);
        }

        public void SetCharacterFace(string chara, int id, bool isMe, float delay = 0f)
        {
            StartCoroutine(SetCharacterFaceAsync(chara, id, isMe, delay));
        }

        public bool charaFaceSetting;
        public Dictionary<string, Sprite> cachedCharaFaces = new Dictionary<string, Sprite>();
        private IEnumerator SetCharacterFaceAsync(string chara, int id, bool isMe, float delay = 0f)
        {
            charaFaceSetting = true;
            yield return new WaitForSeconds(delay);

            if (id == 0)
                id = 1;

            var address = "sn" + chara + "_3_" + id;
            if (!cachedCharaFaces.TryGetValue(address, out var sprite))
            {
                var handle = Addressables.LoadAssetAsync<Sprite>("sn" + chara + "_3_" + id);
                yield return handle;

                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    if (handle.Result.texture.width != handle.Result.texture.height)
                    {
                        var texture = TextureManager.GetCroppingTex(handle.Result.texture, 80, 0, 320, 240);
                        TextureManager.ReplaceTransparentPixelsWithColor(texture, Color.black);
                        sprite = TextureManager.Texture2Sprite(texture);
                    }
                    else
                    {
                        var texture = TextureManager.CreateCenteredTexture(handle.Result.texture, 280, 0, 10);
                        TextureManager.ReplaceTransparentPixelsWithColor(texture, Color.black);
                        sprite = TextureManager.Texture2Sprite(texture);
                    }
                    cachedCharaFaces[address] = sprite;
                }
                else
                    yield break;
            }

            if (isMe)
                GetUI<OcgCoreUI>().AvatarPlayer0.sprite = sprite;
            else
                GetUI<OcgCoreUI>().AvatarPlayer1.sprite = sprite;
        }

        public void SetCharacterDefaultFace()
        {
            var hero = Config.Get(condition + "Character0", VoicePlayer.defaultCharacter);
            var rival = Config.Get(condition + "Character1", VoicePlayer.defaultCharacter);
            StartCoroutine(SetCharacterFaceAsync(hero, 1, true, 0f));
            StartCoroutine(SetCharacterFaceAsync(rival, 1, false, 0f));
        }

        #endregion

        #region Enum

        public enum DuelResult
        {
            DisLink,
            Win,
            Lose,
            Draw
        }

        public enum Condition
        {
            N,
            Duel,
            Watch,
            Replay
        }

        public enum ChainCondition
        {
            No = 0,
            All = 1,
            Smart = 2,
        }

        #endregion

    }
}
