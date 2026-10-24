using MDPro3.Duel;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using YgomSystem.ElementSystem;
using static MDPro3.Servant.OcgCore;

namespace MDPro3.UI.ServantUI
{
    public class OcgCoreUI : ServantUI
    {

        #region Elements

        #region UI

        private const string LABEL_TXT_PLAYER0NAME = "TextPlayer0Name";
        private TextMeshProUGUI m_TextPlayer0Name;
        public TextMeshProUGUI TextPlayer0Name =>
            m_TextPlayer0Name = m_TextPlayer0Name != null ? m_TextPlayer0Name
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_PLAYER0NAME);

        private const string LABEL_TXT_PLAYER1NAME = "TextPlayer1Name";
        private TextMeshProUGUI m_TextPlayer1Name;
        public TextMeshProUGUI TextPlayer1Name =>
            m_TextPlayer1Name = m_TextPlayer1Name != null ? m_TextPlayer1Name
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_PLAYER1NAME);

        private const string LABEL_TXT_PLAYER0LP = "TextPlayer0LP";
        private TextMeshProUGUI m_TextPlayer0LP;
        public TextMeshProUGUI TextPlayer0LP =>
            m_TextPlayer0LP = m_TextPlayer0LP != null ? m_TextPlayer0LP
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_PLAYER0LP);

        private const string LABEL_TXT_PLAYER1LP = "TextPlayer1LP";
        private TextMeshProUGUI m_TextPlayer1LP;
        public TextMeshProUGUI TextPlayer1LP =>
            m_TextPlayer1LP = m_TextPlayer1LP != null ? m_TextPlayer1LP
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_PLAYER1LP);

        private const string LABEL_IMG_AVATARPLAYER0 = "AvatarPlayer0";
        private Image m_AvatarPlayer0;
        public Image AvatarPlayer0 =>
            m_AvatarPlayer0 = m_AvatarPlayer0 != null ? m_AvatarPlayer0
            : Manager.GetElement<Image>(LABEL_IMG_AVATARPLAYER0);

        private const string LABEL_IMG_AVATARPLAYER1 = "AvatarPlayer1";
        private Image m_AvatarPlayer1;
        public Image AvatarPlayer1 =>
            m_AvatarPlayer1 = m_AvatarPlayer1 != null ? m_AvatarPlayer1
            : Manager.GetElement<Image>(LABEL_IMG_AVATARPLAYER1);

        private const string LABEL_GO_HINT = "Hint";
        private GameObject m_Hint;
        public GameObject Hint =>
            m_Hint = m_Hint != null ? m_Hint
            : Manager.GetElement(LABEL_GO_HINT);

        private const string LABEL_TXT_HINT = "TextHint";
        private TextMeshProUGUI m_TextHint;
        public TextMeshProUGUI TextHint =>
            m_TextHint = m_TextHint != null ? m_TextHint
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_HINT);

        private const string LABEL_RT_POPUP = "Popup";
        private RectTransform m_RectPopup;
        public RectTransform RectPopup =>
            m_RectPopup = m_RectPopup != null ? m_RectPopup
            : Manager.GetElement<RectTransform>(LABEL_RT_POPUP);

        private const string LABEL_RT_PLACECOUNT = "PlaceCount";
        private RectTransform m_RectPlaceCount;
        private RectTransform RectPlaceCount =>
            m_RectPlaceCount = m_RectPlaceCount != null ? m_RectPlaceCount
            : Manager.GetElement<RectTransform>(LABEL_RT_PLACECOUNT);

        private const string LABEL_TXT_PLACECOUNT = "TextPlaceCount";
        private TextMeshProUGUI m_TextPlaceCount;
        private TextMeshProUGUI TextPlaceCount =>
            m_TextPlaceCount = m_TextPlaceCount != null ? m_TextPlaceCount
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_PLACECOUNT);

        #endregion

        #region Mono

        private const string LABEL_MONO_CARD_DESCRIPTION = "CardDescription";
        private CardDescription m_CardDescription;
        public CardDescription CardDescription =>
            m_CardDescription = m_CardDescription != null ? m_CardDescription
            : Manager.GetElement<CardDescription>(LABEL_MONO_CARD_DESCRIPTION);

        private const string LABEL_MONO_CARD_LIST = "CardList";
        private CardList m_CardList;
        public CardList CardList =>
            m_CardList = m_CardList != null ? m_CardList
            : Manager.GetElement<CardList>(LABEL_MONO_CARD_LIST);

        private const string LABEL_MONO_DUEL_LOG = "DuelLog";
        private DuelLog m_DuelLog;
        public DuelLog DuelLog =>
            m_DuelLog = m_DuelLog != null ? m_DuelLog
            : Manager.GetElement<DuelLog>(LABEL_MONO_DUEL_LOG);

        private const string LABEL_MONO_DUEL_ERROR_LOG = "DuelErrorLog";
        private DuelErrorLog m_DuelErrorLog;
        public DuelErrorLog DuelErrorLog =>
            m_DuelErrorLog = m_DuelErrorLog != null ? m_DuelErrorLog
            : Manager.GetElement<DuelErrorLog>(LABEL_MONO_DUEL_ERROR_LOG);

        #endregion

        #region Buttons

        private const string LABEL_GO_BUTTONS = "Buttons";
        private GameObject m_Buttons;
        public GameObject Buttons =>
            m_Buttons = m_Buttons != null ? m_Buttons
            : Manager.GetElement(LABEL_GO_BUTTONS);

        private const string LABEL_BTN_STOP = "ButtonStop";
        private Button m_ButtonStop;
        private Button ButtonStop =>
            m_ButtonStop = m_ButtonStop != null ? m_ButtonStop
            : Manager.GetElement<Button>(LABEL_BTN_STOP);

        private const string LABEL_BTN_PLAY = "ButtonPlay";
        private Button m_ButtonPlay;
        private Button ButtonPlay =>
            m_ButtonPlay = m_ButtonPlay != null ? m_ButtonPlay
            : Manager.GetElement<Button>(LABEL_BTN_PLAY);

        private const string LABEL_BTN_ACC = "ButtonAcc";
        private Button m_ButtonAcc;
        private Button ButtonAcc =>
            m_ButtonAcc = m_ButtonAcc != null ? m_ButtonAcc
            : Manager.GetElement<Button>(LABEL_BTN_ACC);

        private const string LABEL_BTN_NOR = "ButtonNor";
        private Button m_ButtonNor;
        private Button ButtonNor =>
            m_ButtonNor = m_ButtonNor != null ? m_ButtonNor
            : Manager.GetElement<Button>(LABEL_BTN_NOR);

        private const string LABEL_BTN_TIMING = "ButtonTiming";
        private Button m_ButtonTiming;
        public Button ButtonTiming =>
            m_ButtonTiming = m_ButtonTiming != null ? m_ButtonTiming
            : Manager.GetElement<Button>(LABEL_BTN_TIMING);

        private const string LABEL_BTN_LOG = "ButtonLog";
        private Button m_ButtonLog;
        public Button ButtonLog =>
            m_ButtonLog = m_ButtonLog != null ? m_ButtonLog
            : Manager.GetElement<Button>(LABEL_BTN_LOG);

        #endregion

        #endregion

        public override void Show(bool cover)
        {
            ShowEvent();
        }

        public override void ShowEvent()
        {
            base.ShowEvent();
            if(GetComponent<UltraAdminDuelUI>()==null)gameObject.AddComponent<UltraAdminDuelUI>();

            CloseHint();

            ButtonStop.gameObject.SetActive(true);
            ButtonPlay.gameObject.SetActive(false);
            ButtonAcc.gameObject.SetActive(true);
            ButtonNor.gameObject.SetActive(false);

            ButtonTiming.gameObject.SetActive(condition == Condition.Duel);
        }

        #region UI Tools

        private Vector2 GetLocationCountPosition(GPS p)
        {
            var position = UIManager.WorldToScreenPoint(Program.instance.camera_.cameraMain, GameCard.GetCardPosition(p));

            if ((p.location & ((uint)CardLocation.Deck + (uint)CardLocation.Extra)) > 0 && p.controller == 0)
                position.y += 80;
            else if ((p.location & ((uint)CardLocation.Deck + (uint)CardLocation.Extra)) > 0 && p.controller == 1)
                position.y -= 50;
            else if ((p.location & ((uint)CardLocation.Grave + (uint)CardLocation.Removed)) > 0 && p.controller == 0)
            {
                position.x -= 10;
                position.y -= 10;
                RectPlaceCount.localScale = new Vector3(-1, 1, 1);
            }
            else if ((p.location & ((uint)CardLocation.Grave + (uint)CardLocation.Removed)) > 0 && p.controller == 1)
            {
                position.x += 10;
                position.y -= 10;
                RectPlaceCount.localScale = new Vector3(1, 1, 1);
            }

            if (p.controller == 0 && (p.location & (uint)CardLocation.Extra) > 0
                || p.controller == 1 && (p.location & (uint)CardLocation.Deck) > 0)
            {
                position.x += 20;
                RectPlaceCount.localScale = new Vector3(1, 1, 1);
            }
            else if (p.controller == 1 && (p.location & (uint)CardLocation.Extra) > 0
                || p.controller == 0 && (p.location & (uint)CardLocation.Deck) > 0)
            {
                position.x -= 20;
                RectPlaceCount.localScale = new Vector3(-1, 1, 1);
            }

            return position;
        }

        public void SetHint(string hint)
        {
            Hint.SetActive(true);
            TextHint.text = hint;
        }

        public void CloseHint()
        {
            Hint.SetActive(false);
        }

        public void ShowLocationCount(GPS p)
        {
            var position = GetLocationCountPosition(p);
            TextPlaceCount.rectTransform.localScale = RectPlaceCount.localScale;
            RectPlaceCount.anchoredPosition = position;
            RectPlaceCount.gameObject.SetActive(true);
            TextPlaceCount.text = Program.instance.ocgcore.GetLocationCardCount((CardLocation)p.location, p.controller).ToString();
        }

        public void HidePlaceCount()
        {
            if (RectPlaceCount.gameObject.activeSelf)
                RectPlaceCount.gameObject.SetActive(false);
        }

        #endregion

        #region Popup

        public void ShowPopupYesOrNo(List<string> selections, Action confirmAction, Action cancelAction)
        {
            var handler = Addressables.InstantiateAsync("UI/PopupDuelYesOrNo.prefab");
            handler.Completed += (result) =>
            {
                result.Result.transform.SetParent(RectPopup, false);
                var popupYesOrNo = result.Result.GetComponent<PopupDuelYesOrNo>();
                popupYesOrNo.exitable = false;
                popupYesOrNo.selections = selections;
                popupYesOrNo.confirmAction = confirmAction;
                popupYesOrNo.cancelAction = cancelAction;
                popupYesOrNo.Show();
            };
        }

        public void ShowPopupPhase(List<string> selections)
        {
            var handler = Addressables.InstantiateAsync("UI/PopupDuelPhase.prefab");
            handler.Completed += (result) =>
            {
                result.Result.transform.SetParent(RectPopup, false);
                var popupPhase = result.Result.GetComponent<PopupDuelPhase>();
                popupPhase.exitable = true;
                popupPhase.selections = selections;
                popupPhase.Show();
            };
        }

        public void ShowPopupSelectCard(string hint, List<GameCard> cards, int min, int max, bool exitable, bool sendable)
        {
            if (string.IsNullOrEmpty(hint))
                hint = InterString.Get("请选择卡片");
            var handler = Addressables.InstantiateAsync("UI/PopupDuelSelectCard.prefab");
            handler.Completed += (result) =>
            {
                result.Result.transform.SetParent(RectPopup, false);
                var popupSelectCard = result.Result.GetComponent<PopupDuelSelectCard>();
                popupSelectCard.exitable = exitable;
                popupSelectCard.hint = hint;
                popupSelectCard.cards = cards;
                popupSelectCard.min = min;
                popupSelectCard.max = max;
                popupSelectCard.sendable = sendable;
                popupSelectCard.Show();
            };
        }

        public void ShowPopupPosition(int code, int count, int option1 = 1, int option2 = 2)
        {
            var handler = Addressables.InstantiateAsync("UI/PopupDuelPosition.prefab");
            handler.Completed += (result) =>
            {
                result.Result.transform.SetParent(RectPopup, false);
                var popupPosition = result.Result.GetComponent<PopupDuelPosition>();
                popupPosition.exitable = false;
                popupPosition.count = count;
                popupPosition.code = code;
                popupPosition.option1 = option1;
                popupPosition.option2 = option2;
                popupPosition.Show();
            };
        }

        public void ShowPopupSelection(List<string> selections, List<int> responses)
        {
            var handler = Addressables.InstantiateAsync("UI/PopupDuelSelection.prefab");
            handler.Completed += (result) =>
            {
                result.Result.transform.SetParent(RectPopup, false);
                var popupSelection = result.Result.GetComponent<PopupDuelSelection>();
                popupSelection.exitable = false;
                popupSelection.selections = selections;
                popupSelection.responses = responses;
                popupSelection.Show();
            };
        }

        public void ShowPopupInput(List<string> selections, Action<string> confirmAction, Action cancelAction, InputValidation.ValidationType type = InputValidation.ValidationType.None)
        {
            var handler = Addressables.InstantiateAsync("UI/PopupDuelInput.prefab");
            handler.Completed += (result) =>
            {
                result.Result.transform.SetParent(RectPopup, false);
                var popupInput = result.Result.GetComponent<PopupDuelInput>();
                popupInput.selections = selections;
                popupInput.confirmAction = confirmAction;
                popupInput.cancelAction = cancelAction;
                popupInput.validationType = type;
                popupInput.Show();
            };
        }

        #endregion

        #region Button Functions

        public void ShowSaveReplay()
        {
            var selections = new List<string>()
            {
                InterString.Get("保存回放"),
                InterString.Get("保存"),
                InterString.Get("放弃"),
                Tools.GetTimeString()
            };
            ShowPopupInput(selections, OnSaveReplay, OnGiveUpReplay, InputValidation.ValidationType.Path);
        }

        public void OnForcedSaveReplay()
        {
            var selections = new List<string>()
                    {
                        InterString.Get("保存回放"),
                        InterString.Get("保存"),
                        InterString.Get("放弃"),
                        Tools.GetTimeString()
                    };
            ShowPopupInput(selections, OnSaveReplay, OnGiveUpReplay, InputValidation.ValidationType.Path);
        }

        private void OnSaveReplay(string replayName)
        {
            TcpHelper.SaveRecord(replayName);
            Program.instance.ocgcore.returnAction = null;
            Program.instance.ocgcore.OnDuelResultConfirmed();
        }

        private void OnGiveUpReplay()
        {
            Program.instance.ocgcore.returnAction = null;
            Program.instance.ocgcore.OnDuelResultConfirmed();
        }

        public void OnStop()
        {
            pause = true;
            ButtonStop.gameObject.SetActive(false);
            ButtonPlay.gameObject.SetActive(true);
        }

        public void OnPlay()
        {
            pause = false;
            ButtonStop.gameObject.SetActive(true);
            ButtonPlay.gameObject.SetActive(false);
        }

        public void OnAcc()
        {
            Accing = true;
            float targetSpeed = 2f;
            targetSpeed = condition switch
            {
                Condition.Duel => Config.GetFloat("DuelAcc", 2f),
                Condition.Watch => Config.GetFloat("WatchAcc", 2f),
                Condition.Replay => Config.GetFloat("ReplayAcc", 2f),
                _ => 2f,
            };

#if UNITY_EDITOR
            Program.instance.timeScaleForEditor = targetSpeed;
#else
            Program.instance.TimeScale = targetSpeed;
#endif

            ButtonAcc.gameObject.SetActive(false);
            ButtonNor.gameObject.SetActive(true);
            Program.instance.ocgcore.SetBgTimeScale(1f / targetSpeed);
        }

        public void OnNor()
        {
            Accing = false;
            float targetSpeed = 1f;
#if UNITY_EDITOR
            Program.instance.timeScaleForEditor = targetSpeed;
#else
            Program.instance.TimeScale = targetSpeed;
#endif
            ButtonAcc.gameObject.SetActive(true);
            ButtonNor.gameObject.SetActive(false);
            Program.instance.ocgcore.SetBgTimeScale(targetSpeed);
        }

        public void OnTiming()
        {
            chainCondition = (ChainCondition)(((int)chainCondition + 1) % 3);
            SetTimingIcon();
        }

        private void SetTimingIcon()
        {
            var state = ButtonTiming.spriteState;
            switch (chainCondition)
            {
                case ChainCondition.No:
                    ButtonTiming.GetComponent<Image>().sprite = TextureManager.container.offTiming[0];
                    state.highlightedSprite = TextureManager.container.offTiming[1];
                    state.pressedSprite = TextureManager.container.offTiming[2];
                    break;
                case ChainCondition.Smart:
                    ButtonTiming.GetComponent<Image>().sprite = TextureManager.container.autoTiming[0];
                    state.highlightedSprite = TextureManager.container.autoTiming[1];
                    state.pressedSprite = TextureManager.container.autoTiming[2];
                    break;
                case ChainCondition.All:
                    ButtonTiming.GetComponent<Image>().sprite = TextureManager.container.onTiming[0];
                    state.highlightedSprite = TextureManager.container.onTiming[1];
                    state.pressedSprite = TextureManager.container.onTiming[2];
                    break;
            }
            ButtonTiming.spriteState = state;
        }

        public void OnLog(bool silent = false)
        {
            if (DuelLog.showing)
            {
                DuelLog.Hide(silent);
                ButtonLog.GetComponent<Image>().sprite = TextureManager.container.onLog[0];
                var state = ButtonLog.spriteState;
                state.highlightedSprite = TextureManager.container.onLog[1];
                state.pressedSprite = TextureManager.container.onLog[2];
                state.disabledSprite = TextureManager.container.onLog[3];
                ButtonLog.spriteState = state;
            }
            else
            {
                DuelLog.Show();
                ButtonLog.GetComponent<Image>().sprite = TextureManager.container.offLog[0];
                var state = ButtonLog.spriteState;
                state.highlightedSprite = TextureManager.container.offLog[1];
                state.pressedSprite = TextureManager.container.offLog[2];
                state.disabledSprite = TextureManager.container.offLog[3];
                ButtonLog.spriteState = state;

                CardList.Hide();
            }
        }

        public void OnSetting()
        {
            Program.instance.ShowSubServant(Program.instance.setting);
        }

        private bool bgDetailShowing;
        public void RefreshBgDetail()
        {
            var core = Program.instance.ocgcore;
            var info = core.messageDispatcher.duel.duelBGManager.fieldSummonRightInfo;
            if (info == null)
                return;

            if (!info.TryGetComponent<ElementObjectManager>(out var summonInfoManager))
                return;

            var nearManager = summonInfoManager.GetElement<ElementObjectManager>("RootNear");
            var farManager = summonInfoManager.GetElement<ElementObjectManager>("RootFar");
            nearManager.GetElement<TextMeshPro>("TextSummon").text = mySummonCount.ToString();
            nearManager.GetElement<TextMeshPro>("TextSpSummon").text = mySpSummonCount.ToString();
            farManager.GetElement<TextMeshPro>("TextSummon").text = opSummonCount.ToString();
            farManager.GetElement<TextMeshPro>("TextSpSummon").text = opSpSummonCount.ToString();

            nearManager.GetElement<TextMeshPro>("TextTotalAtk").text = core.GetAllAtk(true).ToString();
            farManager.GetElement<TextMeshPro>("TextTotalAtk").text = core.GetAllAtk(false).ToString();

            summonInfoManager.GetElement<TextMeshPro>("GraveNear").text = core.GetLocationCardCount(CardLocation.Grave, 0).ToString();
            summonInfoManager.GetElement<TextMeshPro>("GraveFar").text = core.GetLocationCardCount(CardLocation.Grave, 1).ToString();
            summonInfoManager.GetElement<TextMeshPro>("ExcludeNear").text = core.GetLocationCardCount(CardLocation.Removed, 0).ToString();
            summonInfoManager.GetElement<TextMeshPro>("ExcludeFar").text = core.GetLocationCardCount(CardLocation.Removed, 1).ToString();
            summonInfoManager.GetElement<TextMeshPro>("DeckNear").text = core.GetLocationCardCount(CardLocation.Deck, 0).ToString();
            summonInfoManager.GetElement<TextMeshPro>("DeckFar").text = core.GetLocationCardCount(CardLocation.Deck, 1).ToString();
            summonInfoManager.GetElement<TextMeshPro>("ExtraNear").text = core.GetLocationCardCount(CardLocation.Extra, 0).ToString();
            summonInfoManager.GetElement<TextMeshPro>("ExtraFar").text = core.GetLocationCardCount(CardLocation.Extra, 1).ToString();
            summonInfoManager.GetElement<TextMeshPro>("HandNear").text = core.GetLocationCardCount(CardLocation.Hand, 0).ToString();
            summonInfoManager.GetElement<TextMeshPro>("HandFar").text = core.GetLocationCardCount(CardLocation.Hand, 1).ToString();
        }

        public void ResetBgDetailState()
        {
            var wasShowing = bgDetailShowing;
            bgDetailShowing = false;
            foreach (var card in cards)
                card.HideHiddenLabel();

            var info = Program.instance.ocgcore.messageDispatcher.duel.duelBGManager.fieldSummonRightInfo;
            if (info != null)
            {
                if (wasShowing || info.activeSelf)
                    CameraManager.DuelOverlay3DMinus();
                info.SetActive(false);
            }
        }

        public void SwitchBgDetail(bool show)
        {
            if (show)
                ShowBgDetail();
            else
                HideBgDetail();
        }

        private void ShowBgDetail()
        {
            var info = Program.instance.ocgcore.messageDispatcher.duel.duelBGManager.fieldSummonRightInfo;

            if (bgDetailShowing)
                return;
            bgDetailShowing = true;
            foreach (var card in cards)
                card.ShowHiddenLabel();

            if (info != null)
            {
                CameraManager.DuelOverlay3DPlus();
                info.SetActive(true);
                RefreshBgDetail();
            }
        }

        private void HideBgDetail()
        {
            var info = Program.instance.ocgcore.messageDispatcher.duel.duelBGManager.fieldSummonRightInfo;

            if (!bgDetailShowing)
                return;
            bgDetailShowing = false;
            foreach (var card in cards)
                card.HideHiddenLabel();
            if (info != null)
            {
                CameraManager.DuelOverlay3DMinus();
                info.SetActive(false);
            }

        }

        public void ToChat()
        {
            if (condition == Condition.Replay || inPuzzle)
                return;
            Program.instance.ui_.chatPanel.Switch();
        }

        #endregion
    }
}
