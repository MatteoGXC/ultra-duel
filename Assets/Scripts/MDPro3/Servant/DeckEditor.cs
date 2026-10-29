using DG.Tweening;
using MDPro3.UI;
using MDPro3.Duel.YGOSharp;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using MDPro3.UI.PropertyOverride;
using MDPro3.UI.ServantUI;
using static MDPro3.UI.ServantUI.DeckEditorUI;

namespace MDPro3.Servant
{
    public class DeckEditor : Servant
    {

        #region Reference

        public static Deck Deck { get; set; }
        public static string DeckName { get; set; }
        public static bool DeckIsFromLocal;
        public static Banlist Banlist => condition == Condition.PremadeDeck ? PremadeDeckLibrary.PreviewBanlist : (condition == Condition.EditDeck ? DuelFormat.Genesys : cardInfoType == CardInfoType.Genesys) ? BanlistManager.currentGenesysBanList : BanlistManager.currentBanList;
        public static List<int> historyCards;
        public static bool UseMobileLayout => PropertyOverrider.NeedMobileLayout();
        public static string onlineDeckID;

        public enum Condition
        {
            EditDeck,
            OnlineDeck,
            ReplayDeck,
            ChangeSide,
            PremadeDeck
        }
        public static Condition condition = Condition.EditDeck;
        public void SwitchCondition(Condition condition, string deckName = "", Deck deck = null)
        {
            DeckEditor.condition = condition;
            cardInfoType = DuelFormat.Genesys ? CardInfoType.Genesys : CardInfoType.Pool;
            switch (condition)
            {
                case Condition.EditDeck:
                    returnServant = Program.instance.deckSelector;
                    DeckName = Config.GetConfigDeckName();
                    DuelFormat.ApplyDeckFormat(DeckName);
                    cardInfoType = DuelFormat.Genesys ? CardInfoType.Genesys : CardInfoType.Pool;

                    var deckPath = Program.PATH_DECK + DeckName + Program.EXPANSION_YDK;

                    // Auto-fix the file on disk (pre-release ids -> official ids).
                    YdkIdHelper.NormalizeYdkFile(deckPath);

                    Deck = new Deck(deckPath);
                    DeckIsFromLocal = true;
                    historyCards = new();
                    break;

                case Condition.OnlineDeck:
                    returnServant = Program.instance.onlineDeckViewer;
                    DeckName = deckName;
                    Deck = null;
                    DeckIsFromLocal = false;
                    historyCards = new();
                    break;

                case Condition.PremadeDeck:
                    returnServant = Program.instance.onlineDeckViewer;
                    DeckName = deckName;
                    Deck = deck;
                    DeckIsFromLocal = false;
                    cardInfoType = deck.Format == "Genesys" ? CardInfoType.Genesys : CardInfoType.Pool;
                    historyCards = new();
                    break;

                case Condition.ReplayDeck:
                    returnServant = Program.instance.replay;
                    DeckName = deckName;
                    Deck = deck;
                    DeckIsFromLocal = false;
                    historyCards = new();
                    break;

                case Condition.ChangeSide:
                    DeckName = Config.GetConfigDeckName();
                    Deck = TcpHelper.deck;
                    DeckIsFromLocal = false;
                    historyCards = OcgCore.sideReference.Main;
                    break;
            }
        }

        public ResponseRegion ResponseRegion 
        {
            get { return GetUI<DeckEditorUI>()._ResponseRegion; }
            set { GetUI<DeckEditorUI>()._ResponseRegion = value; }
        }

        public enum CardInfoType
        {
            None = 0,
            Detail = 1,
            Pool = 2,
            Genesys = 3
        }
        public static CardInfoType cardInfoType { get; private set; }

        #endregion

        #region Servant

        [HideInInspector] public SelectionButton_CardInDeck lastSelectedCardInDeck;
        [HideInInspector] public SelectionButton_CardInCollection lastSelectedCardInCollection;
        public static bool ToHandTest;

        public override int Depth => 6;
        protected override bool ShowLine => false;
        protected override bool NeedExitButton => false;
        public override float TransitionTime => 0.6f;
        protected override string Label_UI =>
            PropertyOverrider.NeedMobileLayout()
            ? "ServantUI/DeckEditorUIMobile.prefab" : "ServantUI/DeckEditorUI.prefab";

        public override void Initialize()
        {
            SystemEvent.OnResolutionChange += ChangeCanvasMatch;
            returnServant = Program.instance.deckSelector;
            base.Initialize();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);
            UserInput.SetMoveRepeatSpeed(0.8f);
        }

        protected override void ApplyHideArrangement(int nextDepth)
        {
            base.ApplyHideArrangement(nextDepth);
            UserInput.SetMoveRepeatSpeed(1f);
        }

        public override void PerFrameFunction()
        {
            if (!NeedResponseInput())
                return;

            if (UserInput.WasRightShoulderPressing)
            {
                if (UserInput.WasGamepadButtonNorthPressed)
                    GetUI<DeckEditorUI>().OnRegulation();
                else if (UserInput.WasGamepadButtonWestPressed)
                    GetUI<DeckEditorUI>().SetCardInfoType();
                else if (UserInput.WasGamepadStartPressed)
                    GetUI<DeckEditorUI>().ShiftToAppearance();
                return;
            }

            if (UserInput.WasCancelPressed && condition != Condition.ChangeSide)
                OnReturn();

            if (UserInput.WasGamepadSelectPressed)
            {
                if(condition == Condition.ChangeSide)
                    GetUI<DeckEditorUI>().OnChangeSideComplete();
                else
                    GetUI<DeckEditorUI>().OnSave();
            }

            if (UserInput.WasGamepadStartPressed)
                GetUI<DeckEditorUI>().OnSubMenu();

            if (UserInput.WasLeftTriggerPressed)
                GetUI<DeckEditorUI>().ShowCardActionMenu();

            if (UserInput.WasRightTriggerPressed)
            {
                if (ResponseRegion == ResponseRegion.Deck)
                    SelectLastCollectionViewItem();
                else if (ResponseRegion == ResponseRegion.Collection)
                    SelectLastDeckViewItem();
            }


            if (ResponseRegion == ResponseRegion.Deck)
            {
                if (UserInput.WasGamepadButtonNorthPressed)
                    GetUI<DeckEditorUI>().DeckView.ActivateInputField();
                else if (UserInput.WasGamepadButtonWestPressed)
                    GetUI<DeckEditorUI>().OnDeckButtonClicked();
            }
            else if (ResponseRegion == ResponseRegion.Collection)
            {
                if (GetUI<DeckEditorUI>().CardCollectionView.area == CardCollectionView.Area.Collection)
                {
                    if (UserInput.WasLeftStickPressed)
                        GetUI<DeckEditorUI>().CardCollectionView.PrintSearchCards();

                    if (GetUI<DeckEditorUI>().CardCollectionView.showingRelatedCards)
                        return;

                    if (UserInput.WasGamepadButtonNorthPressed)
                    {
                        if (UserInput.WasLeftShoulderPressing)
                            GetUI<DeckEditorUI>().CardCollectionView.ShowSortOrder();
                        else
                            GetUI<DeckEditorUI>().CardCollectionView.InputSearch.InputField.ActivateInputField();
                    }
                    else if (UserInput.WasGamepadButtonWestPressed)
                    {
                        if (UserInput.WasLeftShoulderPressing)
                            GetUI<DeckEditorUI>().CardCollectionView.ResetFilters();
                        else
                            GetUI<DeckEditorUI>().CardCollectionView.ShowFilters();
                    }
                }

                if (UserInput.WasRightStickPressed)
                    GetUI<DeckEditorUI>().CardCollectionView.OnTabRight();
            }
        }

        public override bool NeedResponseInput()
        {
            if(servantUI == null)
                return false;
            if(GetUI<DeckEditorUI>().CardActionMenu.showing)
                return false;
            return base.NeedResponseInput();
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;

            if (ResponseRegion == ResponseRegion.Collection)
                SelectLastCollectionViewItem();
            else if (ResponseRegion == ResponseRegion.Deck)
                SelectLastDeckViewItem();
            else if (ResponseRegion == ResponseRegion.Action)
            {
                if (lastSelectable != null)
                    lastSelectable.Select();
                else
                    GetUI<DeckEditorUI>().CardActionMenu.SelectDefaultButton();
            }
        }

        public void SelectLastDeckViewItem()
        {
            ResponseRegion = ResponseRegion.Deck;
            if (lastSelectedCardInDeck != null)
                lastSelectedCardInDeck.GetSelectable().Select();
            else
                GetUI<DeckEditorUI>().DeckView.SelectDefaultItem();
        }

        public void SelectNearestDeckViewItem(Vector3 position)
        {
            ResponseRegion = ResponseRegion.Deck;
            UserInput.NextSelectionIsAxis = true;
            GetUI<DeckEditorUI>().DeckView.SelectNearestCard(position);
        }

        public void SelectLastCollectionViewItem()
        {
            ResponseRegion = ResponseRegion.Collection;
            if (lastSelectedCardInCollection != null)
                EventSystem.current
                    .SetSelectedGameObject(lastSelectedCardInCollection.gameObject);
            else
                GetUI<DeckEditorUI>().CardCollectionView.SelectDefaultItem();
        }

        public void SelectNearestCollectionViewItem(Vector3 position)
        {
            ResponseRegion = ResponseRegion.Collection;
            UserInput.NextSelectionIsAxis = true;
            GetUI<DeckEditorUI>().CardCollectionView.SelectNearestCard(position);
        }

        public override void OnReturn()
        {
            if (!GetUI<DeckEditorUI>().DeckView.GetDirty() || !DeckIsFromLocal)
                base.OnReturn();
            else
            {
                GetUI<DeckEditorUI>().callExit = true;

                var selections = new List<string>
                {
                    InterString.Get("卡组未保存"),
                    InterString.Get("卡组已修改，是否保存？"),
                    InterString.Get("保存"),
                    InterString.Get("不保存")
                };
                UIManager.ShowPopupYesOrNo(selections, GetUI<DeckEditorUI>().OnSave, OnExit);
            }
        }

        public override void JudgeInputBlockerExitMark(object o)
        {
            ResponseRegion = (ResponseRegion)o;
        }

        public void CallExitIn(float time)
        {
            inTransition = true;//block input
            DOTween.To(v => { }, 0, 0, time).OnComplete(() =>
            {
                OnExit();
            });
        }

        #endregion

        #region Other

        private void ChangeCanvasMatch()
        {
            if (!showing)
                return;

            UIManager.SetCanvasMatch(GetCanvasMatch(), 0f);
        }

        public float GetCanvasMatch()
        {
            if ((float)Screen.width / Screen.height > 16f / 9f)
                return 1f;
            else return 0f;
        }

        public void SetCardInfoType(CardInfoType type)
        {
            cardInfoType = type;
            GetUI<DeckEditorUI>().SetCardInfoType(type);
            SelectionButton_CardInfoType.instance.SetCardInfoTypeIcon(type);
        }

        #endregion
    }
}
