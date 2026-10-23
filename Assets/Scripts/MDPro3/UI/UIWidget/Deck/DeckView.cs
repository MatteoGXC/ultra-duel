using MDPro3.Net;
using MDPro3.Duel.YGOSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using Cysharp.Threading.Tasks;

namespace MDPro3.UI
{
    public class DeckView : UIWidget
    {
        #region Elements

        #region Root

        private const string LABEL_DTM_LOADING = "Loading";
        private DoTweenManager m_TweenLoading;
        protected DoTweenManager TweenLoading =>
            m_TweenLoading = m_TweenLoading != null ? m_TweenLoading
            : Manager.GetElement<DoTweenManager>(LABEL_DTM_LOADING);

        private const string LABEL_CG_VIEWPORT = "Viewport";
        private CanvasGroup m_Viewport;
        protected CanvasGroup Viewport =>
            m_Viewport = m_Viewport != null ? m_Viewport
            : Manager.GetElement<CanvasGroup>(LABEL_CG_VIEWPORT);

        private const string LABEL_SBN_NOITEM = "NoItemButton";
        private SelectionButton m_ButtonNoItem;
        protected SelectionButton ButtonNoItem =>
            m_ButtonNoItem = m_ButtonNoItem != null ? m_ButtonNoItem
            : Manager.GetElement<SelectionButton>(LABEL_SBN_NOITEM);

        private const string LABEL_TXT_NOITEM = "NoItemText";
        private TextMeshProUGUI m_TextNoItem;
        private TextMeshProUGUI TextNoItem =>
            m_TextNoItem = m_TextNoItem != null ? m_TextNoItem
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_NOITEM);

        private const string LABEL_GPC_CURSORWINDOWSELECT = "CursorWindowSelect";
        private GamepadCursor m_CursorWindowSelect;
        protected GamepadCursor CursorWindowSelect =>
            m_CursorWindowSelect = m_CursorWindowSelect != null ? m_CursorWindowSelect
            : Manager.GetElement<GamepadCursor>(LABEL_GPC_CURSORWINDOWSELECT);

        private const string LABEL_SR_DECKVIEW = "ScrollRect";
        private ScrollRect m_ScrollRect;
        public ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetElement<ScrollRect>(LABEL_SR_DECKVIEW);
        protected UIScrollToSelection m_AutoScroll;
        protected UIScrollToSelection AutoScroll =>
            m_AutoScroll = m_AutoScroll != null ? m_AutoScroll
            : ScrollRect.GetComponent<UIScrollToSelection>();

        private const string LABEL_RT_TEMPVIEW = "TempView";
        private RectTransform m_TempView;
        public RectTransform TempView =>
            m_TempView = m_TempView != null ? m_TempView
            : Manager.GetElement<RectTransform>(LABEL_RT_TEMPVIEW);

        #endregion

        #region HeaderArea

        private const string LABEL_RT_HEADERAREA = "HeaderArea";
        private RectTransform m_HeaderArea;
        protected RectTransform HeaderArea =>
            m_HeaderArea = m_HeaderArea != null ? m_HeaderArea
            : Manager.GetElement<RectTransform>(LABEL_RT_HEADERAREA);

        private const string LABEL_TXT_DECKNAME = "HeaderArea/DeckNameText";
        private TextMeshProUGUI m_TextDeckName;
        protected TextMeshProUGUI TextDeckName =>
            m_TextDeckName = m_TextDeckName != null ? m_TextDeckName
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_DECKNAME);

        private const string LABEL_GO_NAMEAREAGROUP = "HeaderArea/NameAreaGroup";
        private GameObject m_NameAreaGroup;
        protected GameObject NameAreaGroup =>
            m_NameAreaGroup = m_NameAreaGroup != null ? m_NameAreaGroup
            : Manager.GetNestedElement(LABEL_GO_NAMEAREAGROUP);

        private const string LABEL_IPT_DECKNAME = "HeaderArea/InputField";
        private TMP_InputField m_InputDeckName;
        protected TMP_InputField InputDeckName =>
            m_InputDeckName = m_InputDeckName != null ? m_InputDeckName
            : Manager.GetNestedElement<TMP_InputField>(LABEL_IPT_DECKNAME);

        private const string LABEL_SBN_BUTTON_DECK = "HeaderArea/ButtonDeck";
        private SelectionButton m_ButtonDeck;
        public SelectionButton ButtonDeck =>
            m_ButtonDeck = m_ButtonDeck != null ? m_ButtonDeck :
            Manager.GetNestedElement<SelectionButton>(LABEL_SBN_BUTTON_DECK);


        private const string LABEL_IMG_DECK = "HeaderArea/IconDeck";
        private Image m_IconDeck;
        protected Image IconDeck =>
            m_IconDeck = m_IconDeck != null ? m_IconDeck
            : Manager.GetNestedElement<Image>(LABEL_IMG_DECK);

        #endregion

        #region MainDeckView

        private const string LABEL_UH_MAINDECKVIEW = "MainDeckView";
        private UIHover m_MainDeckView;
        protected UIHover MainDeckView =>
            m_MainDeckView = m_MainDeckView != null ? m_MainDeckView
            : Manager.GetElement<UIHover>(LABEL_UH_MAINDECKVIEW);

        private const string LABEL_TXT_MAINDECKCARDNUM = "MainDeckView/TextMainDeckCardNum";
        private TextMeshProUGUI m_TextMainDeckCardNum;
        protected TextMeshProUGUI TextMainDeckCardNum =>
            m_TextMainDeckCardNum = m_TextMainDeckCardNum != null ? m_TextMainDeckCardNum
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_MAINDECKCARDNUM);

        private const string LABEL_TXT_MAINDECKMONSTERNUM = "MainDeckView/TextMainDeckMonsterNum";
        private TextMeshProUGUI m_TextMainDeckMonsterNum;
        protected TextMeshProUGUI TextMainDeckMonsterNum =>
            m_TextMainDeckMonsterNum = m_TextMainDeckMonsterNum != null ? m_TextMainDeckMonsterNum
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_MAINDECKMONSTERNUM);

        private const string LABEL_TXT_MAINDECKSPELLNUM = "MainDeckView/TextMainDeckSpellNum";
        private TextMeshProUGUI m_TextMainDeckSpellNum;
        protected TextMeshProUGUI TextMainDeckSpellNum =>
            m_TextMainDeckSpellNum = m_TextMainDeckSpellNum != null ? m_TextMainDeckSpellNum
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_MAINDECKSPELLNUM);

        private const string LABEL_TXT_MAINDECKTRAPNUM = "MainDeckView/TextMainDeckTrapNum";
        private TextMeshProUGUI m_TextMainDeckTrapNum;
        protected TextMeshProUGUI TextMainDeckTrapNum =>
            m_TextMainDeckTrapNum = m_TextMainDeckTrapNum != null ? m_TextMainDeckTrapNum
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_MAINDECKTRAPNUM);

        private const string LABEL_GLG_MAIN_DECK_CONTENT = "MainDeckView/MainDeckContent";
        private GridLayoutGroup m_MainDeckContent;
        protected GridLayoutGroup MainDeckContent =>
            m_MainDeckContent = m_MainDeckContent != null ? m_MainDeckContent
            : Manager.GetNestedElement<GridLayoutGroup>(LABEL_GLG_MAIN_DECK_CONTENT);

        private const string LABEL_GO_TEMPLATE = "MainDeckView/template";
        private GameObject m_Template;
        protected GameObject Template =>
            m_Template = m_Template != null ? m_Template
            : Manager.GetNestedElement(LABEL_GO_TEMPLATE);

        private const string LABEL_GO_MAIN_GENESYS = "MainDeckView/TextMainDeckGenesys";
        private GameObject m_MainDeckGenesys;
        protected GameObject MainDeckGenesys =>
            m_MainDeckGenesys = m_MainDeckGenesys != null ? m_MainDeckGenesys
            : Manager.GetNestedElement(LABEL_GO_MAIN_GENESYS);

        private const string LABEL_TXT_MAIN_DECK_GP = "MainDeckView/TextMainDeckGP";
        private TextMeshProUGUI m_TextMainDeckGP;
        protected TextMeshProUGUI TextMainDeckGP =>
            m_TextMainDeckGP = m_TextMainDeckGP != null ? m_TextMainDeckGP
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_MAIN_DECK_GP);

        #endregion

        #region ExtraDeckView

        private const string LABEL_UH_EXTRADECKVIEW = "ExtraDeckView";
        private UIHover m_ExtraDeckView;
        protected UIHover ExtraDeckView =>
            m_ExtraDeckView = m_ExtraDeckView != null ? m_ExtraDeckView
            : Manager.GetElement<UIHover>(LABEL_UH_EXTRADECKVIEW);

        private const string LABEL_TXT_EXTRADECKCARDNUM = "ExtraDeckView/TextExtraDeckCardNum";
        private TextMeshProUGUI m_TextExtraDeckCardNum;
        protected TextMeshProUGUI TextExtraDeckCardNum =>
            m_TextExtraDeckCardNum = m_TextExtraDeckCardNum != null ? m_TextExtraDeckCardNum
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_EXTRADECKCARDNUM);

        private const string LABEL_TXT_EXTRADECKFUSIONNUM = "ExtraDeckView/TextExtraDeckFusionNum";
        private TextMeshProUGUI m_TextExtraDeckFusionNum;
        protected TextMeshProUGUI TextExtraDeckFusionNum =>
            m_TextExtraDeckFusionNum = m_TextExtraDeckFusionNum != null ? m_TextExtraDeckFusionNum
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_EXTRADECKFUSIONNUM);

        private const string LABEL_TXT_EXTRADECKSYNCHRONUM = "ExtraDeckView/TextExtraDeckSynchroNum";
        private TextMeshProUGUI m_TextExtraDeckSynchroNum;
        protected TextMeshProUGUI TextExtraDeckSynchroNum =>
            m_TextExtraDeckSynchroNum = m_TextExtraDeckSynchroNum != null ? m_TextExtraDeckSynchroNum
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_EXTRADECKSYNCHRONUM);

        private const string LABEL_TXT_EXTRADECKXYZNUM = "ExtraDeckView/TextExtraDeckXyzNum";
        private TextMeshProUGUI m_TextExtraDeckXyzNum;
        protected TextMeshProUGUI TextExtraDeckXyzNum =>
            m_TextExtraDeckXyzNum = m_TextExtraDeckXyzNum != null ? m_TextExtraDeckXyzNum
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_EXTRADECKXYZNUM);

        private const string LABEL_TXT_EXTRADECKLINKNUM = "ExtraDeckView/TextExtraDeckLinkNum";
        private TextMeshProUGUI m_TextExtraDeckLinkNum;
        protected TextMeshProUGUI TextExtraDeckLinkNum =>
            m_TextExtraDeckLinkNum = m_TextExtraDeckLinkNum != null ? m_TextExtraDeckLinkNum
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_EXTRADECKLINKNUM);

        private const string LABEL_GLG_EXTRADeckContent = "ExtraDeckView/ExtraDeckContent";
        private GridLayoutGroup m_ExtraDeckContent;
        protected GridLayoutGroup ExtraDeckContent =>
            m_ExtraDeckContent = m_ExtraDeckContent != null ? m_ExtraDeckContent
            : Manager.GetNestedElement<GridLayoutGroup>(LABEL_GLG_EXTRADeckContent);

        private const string LABEL_GO_EXTRA_GENESYS = "ExtraDeckView/TextExtraDeckGenesys";
        private GameObject m_ExtraDeckGenesys;
        protected GameObject ExtraDeckGenesys =>
            m_ExtraDeckGenesys = m_ExtraDeckGenesys != null ? m_ExtraDeckGenesys
            : Manager.GetNestedElement(LABEL_GO_EXTRA_GENESYS);

        private const string LABEL_TXT_EXTRA_DECK_GP = "ExtraDeckView/TextExtraDeckGP";
        private TextMeshProUGUI m_TextExtraDeckGP;
        protected TextMeshProUGUI TextExtraDeckGP =>
            m_TextExtraDeckGP = m_TextExtraDeckGP != null ? m_TextExtraDeckGP
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_EXTRA_DECK_GP);

        #endregion

        #region SideDeckView

        private const string LABEL_UH_SIDEDECKVIEW = "SideDeckView";
        private UIHover m_SideDeckView;
        protected UIHover SideDeckView =>
            m_SideDeckView = m_SideDeckView != null ? m_SideDeckView
            : Manager.GetElement<UIHover>(LABEL_UH_SIDEDECKVIEW);

        private const string LABEL_TXT_SIDEDECKCARDNUM = "SideDeckView/TextSideDeckCardNum";
        private TextMeshProUGUI m_TextSideDeckCardNum;
        protected TextMeshProUGUI TextSideDeckCardNum =>
            m_TextSideDeckCardNum = m_TextSideDeckCardNum != null ? m_TextSideDeckCardNum
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_SIDEDECKCARDNUM);

        private const string LABEL_TXT_SIDEDECKMONSTERNUM = "SideDeckView/TextSideDeckMonsterNum";
        private TextMeshProUGUI m_TextSideDeckMonsterNum;
        protected TextMeshProUGUI TextSideDeckMonsterNum =>
            m_TextSideDeckMonsterNum = m_TextSideDeckMonsterNum != null ? m_TextSideDeckMonsterNum
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_SIDEDECKMONSTERNUM);

        private const string LABEL_TXT_SIDEDECKSPELLNUM = "SideDeckView/TextSideDeckSpellNum";
        private TextMeshProUGUI m_TextSideDeckSpellNum;
        protected TextMeshProUGUI TextSideDeckSpellNum =>
            m_TextSideDeckSpellNum = m_TextSideDeckSpellNum != null ? m_TextSideDeckSpellNum
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_SIDEDECKSPELLNUM);

        private const string LABEL_TXT_SIDEDECKTRAPNUM = "SideDeckView/TextSideDeckTrapNum";
        private TextMeshProUGUI m_TextSideDeckTrapNum;
        protected TextMeshProUGUI TextSideDeckTrapNum =>
            m_TextSideDeckTrapNum = m_TextSideDeckTrapNum != null ? m_TextSideDeckTrapNum
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_SIDEDECKTRAPNUM);

        private const string LABEL_GLG_SideDeckContent = "SideDeckView/SideDeckContent";
        private GridLayoutGroup m_SideDeckContent;
        protected GridLayoutGroup SideDeckContent =>
            m_SideDeckContent = m_SideDeckContent != null ? m_SideDeckContent
            : Manager.GetNestedElement<GridLayoutGroup>(LABEL_GLG_SideDeckContent);

        private const string LABEL_GO_SIDE_GENESYS = "SideDeckView/TextSideDeckGenesys";
        private GameObject m_SideDeckGenesys;
        protected GameObject SideDeckGenesys =>
            m_SideDeckGenesys = m_SideDeckGenesys != null ? m_SideDeckGenesys
            : Manager.GetNestedElement(LABEL_GO_SIDE_GENESYS);

        private const string LABEL_TXT_SIDE_DECK_GP = "SideDeckView/TextSideDeckGP";
        private TextMeshProUGUI m_TextSideDeckGP;
        protected TextMeshProUGUI TextSideDeckGP =>
            m_TextSideDeckGP = m_TextSideDeckGP != null ? m_TextSideDeckGP
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_SIDE_DECK_GP);

        #endregion

        #endregion

        #region Reference
        public enum DeckLocation
        {
            MainDeck = 1,
            ExtraDeck = 2,
            SideDeck = 4,
            All = 7
        }

        public enum Condition
        {
            Editable,
            ChangeSide,
            Pickup,
            NonEditable
        }
        public Condition condition;

        protected float contentWidth = 772f - 16f;
        protected float templateWidth = 72f;
        protected Vector2 defaultSpacing = new(4f, 4f);
        protected float defaultVerticalSpacing = 4f;
        protected int defaultColumns = 10;
        protected int defaultMainDeckRows = 4;
        protected int defaultExtraDeckRows = 1;
        protected int defaultSideDeckRows = 1;
        protected Vector3 dragCardScale = new(1.7f, 1.7f, 1f);

        public bool deckLoaded;
        public int mainCount;
        public int extraCount;
        public int sideCount;
        public List<SelectionButton_CardInDeck> cards;
        public Deck Deck { get; set; }

        protected string deckNameWithType;
        protected string deckFileName;
        protected string deckType;
        protected bool needSave;

        #endregion

        #region Public Functions

        public void PrintDeck(Deck deck, string deckName, Condition condition)
        {
            Deck = deck;
            deckNameWithType = deckName;
            if (Path.GetFileName(deckName) == deckName)
                deckType = string.Empty;
            else
                deckType = Path.GetFileName(Path.GetDirectoryName(deckName));
            deckFileName = Path.GetFileName(deckName);

            this.condition = condition;

            SetCondition(condition);
            InputDeckName.text = deckFileName;
            TextDeckName.text = deckFileName;

            _ = LoadDeckCaseAsync(deck.Case);
            StartCoroutine(PrintDeckAsync());

            if(Program.instance.currentServant == Program.instance.deckEditor)
                SetCardInfoTypeInternal(DeckEditor.cardInfoType);
        }

        private async UniTask LoadDeckCaseAsync(int deckCase)
        {
            var icon = await Program.items.LoadDeckCaseIconAsync(deckCase, string.Empty);
            if (gameObject != null)
                IconDeck.sprite = icon;
        }

        public void SetDirty(bool dirty)
        {
            needSave = dirty;
        }

        public bool GetDirty()
        {
            if (condition == Condition.Editable && InputDeckName.text != deckFileName)
                return true;
            return needSave;
        }

        public string GetDeckName()
        {
            if (condition == Condition.Editable)
                return InputDeckName.text;
            else
                return deckFileName;
        }

        public void SetCondition(Condition condition)
        {
            this.condition = condition;
            //NameAreaGroup.SetActive(condition == Condition.Editable);
            InputDeckName.gameObject.SetActive(condition == Condition.Editable);
            TextDeckName.gameObject.SetActive(condition != Condition.Editable);
            if (condition == Condition.Pickup)
                ButtonDeck.gameObject.SetActive(false);
        }

        public RectTransform GetDeckLocationParent(DeckLocation location)
        {
            return location switch
            {
                DeckLocation.MainDeck => MainDeckContent.GetComponent<RectTransform>(),
                DeckLocation.ExtraDeck => ExtraDeckContent.GetComponent<RectTransform>(),
                DeckLocation.SideDeck => SideDeckContent.GetComponent<RectTransform>(),
                _ => null
            };
        }

        public int GetCardCount(int code)
        {
            var count = 0;
            var data = CardsManager.Get(code);
            if (data == null) return count;

            foreach (var card in cards)
            {
                if (card.Card == null || card.Card.Id == 0)
                    continue;

                if (data.Alias == 0)
                {
                    if (card.Card.Id == code || card.Card.Alias == code)
                        count++;
                }
                else
                {
                    if (card.Card.Id == data.Alias || card.Card.Alias == data.Alias)
                        count++;
                }
            }
            return count;
        }

        public bool AddCard(Card data, bool playMoveAnimation, bool playBirthAnimation)
        {
            if (!deckLoaded) return false;
            if (!CanEditCard()) return false;
            if (!CanAddCard(data.Id)) return false;

            AudioManager.PlaySE("SE_DECK_PLUS");
            var targetLocaltion = DeckLocation.SideDeck;
            if (data.IsExtraCard())
            {
                if (GetDeckLocationCount(DeckLocation.ExtraDeck) < 15)
                    targetLocaltion = DeckLocation.ExtraDeck;
            }
            else if (GetDeckLocationCount(DeckLocation.MainDeck) < 60)
                targetLocaltion = DeckLocation.MainDeck;

            AddCard(data, targetLocaltion, playMoveAnimation, playBirthAnimation);
            return true;
        }

        public bool AddCardFromPosition(Card data, Vector3 position)
        {
            if (!deckLoaded) return false;
            if (!CanEditCard()) return false;

            SortCards();
            SelectionButton_CardInDeck hoverCard = null;
            foreach (var card in cards)
                if (card.IsHovering())
                {
                    hoverCard = card;
                    break;
                }

            var location = DeckLocation.All;
            if (hoverCard == null)
                location = GetHoveredLocation();
            else
                location = hoverCard.location;

            if (location == DeckLocation.All || !CanSwitchPosition(data, location))
                return false;
            if (!CanAddCard(data.Id)) return false;

            if (hoverCard == null)
            {
                var added = AddCard(data, location, true, false);
                added.MoveToParent(position);
            }
            else
            {
                var siblingIndex = hoverCard.transform.GetSiblingIndex();
                var added = AddCard(data, location, false, false);
                foreach (var card in cards)
                    if (card.location == location)
                        if (card != added)
                            card.LockPosition();

                added.LockPosition(position, dragCardScale);
                added.transform.SetSiblingIndex(siblingIndex);
            }
            return true;
        }

        public bool AddCardFromPositionWithSequence(Card data, Vector3 position)
        {
            if (!deckLoaded) return false;
            if (!CanEditCard()) return false;
            if (!CanAddCard(data.Id)) return false;

            SortCards();
            AudioManager.PlaySE("SE_DECK_PLUS");

            var targetLocaltion = DeckLocation.SideDeck;
            if (data.IsExtraCard())
            {
                if (GetDeckLocationCount(DeckLocation.ExtraDeck) < 15)
                    targetLocaltion = DeckLocation.ExtraDeck;
            }
            else if (GetDeckLocationCount(DeckLocation.MainDeck) < 60)
                targetLocaltion = DeckLocation.MainDeck;

            var added = AddCard(data, targetLocaltion, !DeckEditor.UseMobileLayout, false);
            added.MoveToParentSequence(position);
            return true;
        }

        public SelectionButton_CardInDeck GetHoveringCard()
        {
            foreach (var card in cards)
                if (card.IsHovering())
                    return card;
            return null;
        }

        public void MoveCardToLocation(SelectionButton_CardInDeck card, DeckLocation location, Vector3 position)
        {
            if (!deckLoaded) return;
            SetDirty(true);

            foreach (var c in cards)
                if (c != card)
                    if (c.location == location || c.location == card.location)
                        c.LockPosition();
            card.LockPosition(position, dragCardScale);

            card.transform.SetParent(GetDeckLocationParent(location), false);
            card.location = location;
            RefreshCardsCount(DeckLocation.All);
            ChangeGridSpacing(DeckLocation.All);
        }

        public void MoveCardToLocationWithSiblingIndex(SelectionButton_CardInDeck card
            , DeckLocation location, int siblingIndex, Vector3 position)
        {
            if (!deckLoaded) return;
            SetDirty(true);

            foreach (var c in cards)
                if (c != card)
                    if (c.location == card.location || c.location == location)
                        c.LockPosition();
            card.LockPosition(position, dragCardScale);
            card.transform.SetParent(GetDeckLocationParent(location), false);
            card.transform.SetSiblingIndex(siblingIndex);
            card.location = location;
            RefreshCardsCount(DeckLocation.All);
            ChangeGridSpacing(DeckLocation.All);
        }

        public bool RemoveCard(SelectionButton_CardInDeck card
            , bool needSelect, bool playMoveAnimation, bool destroy)
        {
            if (!deckLoaded) return false;
            if (!CanEditCard()) return false;
            SetDirty(true);

            SortCards();
            int index = -1;
            for (int i = 0; i < cards.Count; i++)
                if (cards[i] == card)
                {
                    index = i;
                    break;
                }
            if (index < 0)
            {
                Debug.LogError("Card to be deleted not found in the list.");
                return false;
            }

            int siblingIndex = card.transform.GetSiblingIndex();
            for (int i = 0; i < cards.Count; i++)
                if (cards[i].location == card.location)
                    if (!DeckEditor.UseMobileLayout || cards[i].transform.GetSiblingIndex() > siblingIndex)
                        if (cards[i] != card)
                            cards[i].LockPosition();

            cards.RemoveAt(index);
            if (destroy)
                Destroy(card.gameObject);
            else
                card.transform.SetParent(TempView, true);

            if (needSelect)
            {
                if (index - 1 >= 0
                    && index < cards.Count
                    && cards[index].location != card.location)
                    index--;
                if (cards.Count <= index)
                    index = cards.Count - 1;

                if (cards.Count == 0)
                {
                    TextNoItem.gameObject.SetActive(true);
                    if (UserInput.gamepadType != UserInput.GamepadType.None)
                        ButtonNoItem.GetSelectable().Select();
                }
                else if (UserInput.gamepadType != UserInput.GamepadType.None)
                    EventSystem.current.SetSelectedGameObject(cards[index].gameObject);
            }

            RefreshCardsCount(card.location);
            ChangeGridSpacing(card.location);
            return true;
        }

        public bool ClearDeck()
        {
            if (!deckLoaded) return false;
            if (!CanEditCard()) return false;
            SetDirty(true);

            foreach (var card in cards)
            {
                card.transform.SetParent(Program.instance.ui_.transform);
                card.gameObject.SetActive(false);
                Destroy(card.gameObject);
            }
            cards.Clear();

            RefreshCardsCount(DeckLocation.All);
            ChangeGridSpacing(DeckLocation.All);
            return true;
        }

        public SelectionButton_CardInDeck GetCardByData(Card data)
        {
            if (!deckLoaded) return null;

            List<SelectionButton_CardInDeck> aliasCards = new();
            foreach (var card in cards)
            {
                if (card.Card.Id == data.Id)
                    return card;

                if (data.Alias == 0)
                {
                    if (card.Card.Alias == data.Id)
                        aliasCards.Add(card);
                }
                else
                {
                    if (card.Card.Id == data.Alias || card.Card.Alias == data.Alias)
                        aliasCards.Add(card);
                }
            }
            if (aliasCards.Count > 0)
                return aliasCards[0];
            else
                return null;
        }

        public SelectionButton_CardInDeck GetNavigationTarget(
            DeckLocation location, MoveDirection direction
            , Vector3 position)
        {
            var targetList = new List<SelectionButton_CardInDeck>();
            foreach (var card in cards)
                if (card.location == location)
                    targetList.Add(card);

            if (targetList.Count == 0)
            {
                if (location == DeckLocation.ExtraDeck)
                {
                    if (direction == MoveDirection.Up)
                        location = DeckLocation.MainDeck;
                    else if (direction == MoveDirection.Down)
                        location = DeckLocation.SideDeck;
                    foreach (var card in cards)
                        if (card.location == location)
                            targetList.Add(card);
                    if (targetList.Count == 0)
                        return null;
                }
                else
                    return null;
            }

            var distances = new Dictionary<SelectionButton_CardInDeck, float>();
            foreach (var card in targetList)
                //distances.Add(card, Vector3.Distance(position, card.transform.position));
                distances.Add(card, Tools.CalculateWeightedDistance(position, card.transform.position, 'y'));
            return distances.Aggregate((left, right) => left.Value < right.Value ? left : right).Key;
        }

        public bool CanEditCard()
        {
            if (condition == Condition.NonEditable)
                MessageManager.Toast(InterString.Get("请先保存卡组"));
            return condition == Condition.Editable;
        }

        public bool CanAddCard(int code)
        {
            var count = GetCardCount(code);
            if (count >= DeckEditor.Banlist.GetQuantity(code))
            {
                if (count == 3)
                    MessageManager.Toast(InterString.Get("卡组中同名卡片不得超过3张"));
                else if (count == 2)
                    MessageManager.Toast(InterString.Get("卡组中准限制卡片不得超过2张，@n如需无视限制，请将禁限卡表设置为无（N/A）。"));
                else if (count == 1)
                    MessageManager.Toast(InterString.Get("卡组中限制卡片不得超过1张，@n如需无视限制，请将禁限卡表设置为无（N/A）。"));
                else
                    MessageManager.Toast(InterString.Get("无法将禁止卡片放入卡组，@n如需无视限制，请将禁限卡表设置为无（N/A）。"));
                return false;
            }
            return true;
        }

        public bool CanSwitchPosition(Card card, DeckLocation location)
        {
            if (card.IsExtraCard() && location == DeckLocation.MainDeck)
            {
                MessageManager.Toast(InterString.Get("无法将该卡片加入主卡组"));
                return false;
            }
            if (!card.IsExtraCard() && location == DeckLocation.ExtraDeck)
            {
                MessageManager.Toast(InterString.Get("无法将该卡片加入额外卡组"));
                return false;
            }
            return true;
        }

        public void HideDeckLocationTable()
        {
            MainDeckView.Hide();
            ExtraDeckView.Hide();
            SideDeckView.Hide();
        }

        private ShortcutIcon[] shortcutIcons;
        public void SetCursor(bool selected)
        {
            CursorWindowSelect.Show = selected;
            shortcutIcons ??= GetComponentsInChildren<ShortcutIcon>(true);
            foreach (var shortcut in shortcutIcons)
                shortcut.GroupShow = selected;
        }

        public void SelectDefaultItem()
        {
            if (cards.Count > 0)
            {
                SortCards();
                EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
            }
            else
                ButtonNoItem.GetSelectable().Select();
        }

        public void SelectNearestCard(Vector3 fromPosition)
        {
            UserInput.NextSelectionIsAxis = true;
            if (cards.Count == 0)
            {
                SelectDefaultItem();
                return;
            }

            var distance = new Dictionary<SelectionButton_CardInDeck, float>();
            foreach (var card in cards)
                //distance.Add(card, Vector3.Distance(fromPosition, card.transform.position));
                distance.Add(card, Tools.CalculateWeightedDistance(fromPosition, card.transform.position, 'x'));
            var minKey = distance.Aggregate((left, right) => left.Value < right.Value ? left : right).Key;
            EventSystem.current.SetSelectedGameObject(minKey.gameObject);
        }

        public void SetNoItemButtonNavigationEvent(MoveDirection direction, UnityAction navigation)
        {
            ButtonNoItem.SetNavigationEvent(direction, navigation);
        }

        public bool Save()
        {
            if (condition == Condition.Editable && !deckLoaded) return false;

            Deck = FromObjectDeckToCodedDeck();
            if (!DeckFileSave())
                return false;
            SetCondition(Condition.Editable);
            return true;
        }

        public int GetDeckLocationCount(DeckLocation location)
        {
            int count = 0;
            if ((location & DeckLocation.MainDeck) > 0)
                count += GetDeckLocationParent(DeckLocation.MainDeck).childCount;
            if ((location & DeckLocation.ExtraDeck) > 0)
                count += GetDeckLocationParent(DeckLocation.ExtraDeck).childCount;
            if ((location & DeckLocation.SideDeck) > 0)
                count += GetDeckLocationParent(DeckLocation.SideDeck).childCount;
            return count;
        }

        public void SetCardInfoType(DeckEditor.CardInfoType type)
        {
            foreach (var card in cards)
                card.RefreshIcons();
            SetCardInfoTypeInternal(type);
        }

        private void SetCardInfoTypeInternal(DeckEditor.CardInfoType type)
        {
            if (Program.instance.currentServant == Program.instance.deckEditor)
            {
                MainDeckGenesys.SetActive(type == DeckEditor.CardInfoType.Genesys);
                TextMainDeckGP.gameObject.SetActive(type == DeckEditor.CardInfoType.Genesys);
                ExtraDeckGenesys.SetActive(type == DeckEditor.CardInfoType.Genesys);
                TextExtraDeckGP.gameObject.SetActive(type == DeckEditor.CardInfoType.Genesys);
                SideDeckGenesys.SetActive(type == DeckEditor.CardInfoType.Genesys);
                TextSideDeckGP.gameObject.SetActive(type == DeckEditor.CardInfoType.Genesys);
            }
            else
            {
                MainDeckGenesys.SetActive(false);
                TextMainDeckGP.gameObject.SetActive(false);
                ExtraDeckGenesys.SetActive(false);
                TextExtraDeckGP.gameObject.SetActive(false);
                SideDeckGenesys.SetActive(false);
                TextSideDeckGP.gameObject.SetActive(false);
            }
        }

        public TMP_InputField GetInputField()
        {
            return InputDeckName;
        }

        public void RefreshRarity(int code)
        {
            foreach (var card in cards)
                card.RefreshRarity(code);
        }

        public void ScrollTo(SelectionButton_CardInDeck card)
        {
            if (AutoScroll == null)
                return;
            AutoScroll.VerticalScrollTo(card.GetComponent<RectTransform>());
        }

        public void ResetDeck()
        {
            if (!deckLoaded) return;
            if(condition != Condition.ChangeSide)
                if (!CanEditCard()) return;

            PrintDeck(Deck, deckNameWithType, condition);
        }

        public void Sort()
        {
            if (!deckLoaded) return;
            if(condition != Condition.ChangeSide)
                if (!CanEditCard()) return;

            SetDirty(true);

            List<SelectionButton_CardInDeck> main = new();
            List<SelectionButton_CardInDeck> extra = new();
            List<SelectionButton_CardInDeck> side = new();
            foreach (var card in cards)
            {
                if (card.location == DeckLocation.MainDeck)
                    main.Add(card);
                else if (card.location == DeckLocation.ExtraDeck)
                    extra.Add(card);
                else if (card.location == DeckLocation.SideDeck)
                    side.Add(card);
                card.LockPosition();
            }

            main.Sort((left, right) =>
            {
                return CardsManager.ComparisonOfCard()
                (left.Card, right.Card);
            });
            extra.Sort((left, right) =>
            {
                return CardsManager.ComparisonOfCard()
                (left.Card, right.Card);
            });
            side.Sort((left, right) =>
            {
                return CardsManager.ComparisonOfCard()
                (left.Card, right.Card);
            });

            for (int i = 0; i < main.Count; i++)
                main[i].transform.SetSiblingIndex(i);
            for (int i = 0; i < extra.Count; i++)
                extra[i].transform.SetSiblingIndex(i);
            for (int i = 0; i < side.Count; i++)
                side[i].transform.SetSiblingIndex(i);

            cards.Clear();
            cards = new(main);
            cards.AddRange(extra);
            cards.AddRange(side);

            if (Cursor.lockState == CursorLockMode.Locked)
                SelectDefaultItem();
        }

        public void Randomize()
        {
            if (!deckLoaded) return;
            if (condition != Condition.ChangeSide)
                if (!CanEditCard()) return;

            SetDirty(true);

            List<SelectionButton_CardInDeck> main = new();
            foreach (var card in cards)
            {
                if (card.location == DeckLocation.MainDeck)
                {
                    main.Add(card);
                    card.LockPosition();
                }
            }

            System.Random rand = new();
            for (int i = 0; i < main.Count; i++)
            {
                int random_index = rand.Next() % main.Count;
                (main[random_index], main[i]) = (main[i], main[random_index]);
            }
            for (int i = 0; i < main.Count; i++)
                main[i].transform.SetSiblingIndex(i);

            if (Cursor.lockState == CursorLockMode.Locked)
                SelectDefaultItem();
        }

        public void Copy()
        {
            if (!deckLoaded) return;
            if (!CanEditCard()) return;

            SetDirty(true);

            deckNameWithType += " - " + InterString.Get("复制");
            deckFileName += " - " + InterString.Get("复制");
            InputDeckName.text = deckFileName;
            Deck.deckId = string.Empty;
        }

        public void Share()
        {
            if (!deckLoaded) return;
            if (!CanEditCard()) return;
            if (GetDirty() || !File.Exists(Program.PATH_DECK + deckNameWithType + Program.EXPANSION_YDK))
            {
                if(condition != Condition.ChangeSide)
                    MessageManager.Toast(InterString.Get("请先保存卡组"));
                return;
            }

            var url = DeckShareURL.DeckToUri(Deck.Main, Deck.Extra, Deck.Side).ToString();
            GUIUtility.systemCopyBuffer = url;
            Application.OpenURL(url);
        }

        public List<int> GetAllCardCodes()
        {
            var returnValue = new List<int>();
            foreach (var card in cards)
                returnValue.Add(card.Card.Id);
            return returnValue;
        }

        public void ActivateInputField()
        {
            InputDeckName.ActivateInputField();
        }

        public Deck FromObjectDeckToCodedDeck()
        {
            SortCards();
            Deck deck = new();
            foreach (var card in cards)
            {
                if (card.location == DeckLocation.MainDeck)
                    deck.Main.Add(card.Card.Id);
                else if (card.location == DeckLocation.ExtraDeck)
                    deck.Extra.Add(card.Card.Id);
                else if (card.location == DeckLocation.SideDeck)
                    deck.Side.Add(card.Card.Id);
            }

            deck.Pickup = Deck.Pickup;
            deck.Format = Deck.Format;
            deck.FormatPointCap = Deck.FormatPointCap;
            deck.Protector = Deck.Protector;
            deck.Case = Deck.Case;
            deck.Field = Deck.Field;
            deck.Grave = Deck.Grave;
            deck.Stand = Deck.Stand;
            deck.Mate = Deck.Mate;
            deck.deckId = Deck.deckId;
            deck.userId = Deck.userId;

            if(Path.GetFileName(deckNameWithType) == deckNameWithType)
                deck.type = string.Empty;
            else
                deck.type = Path.GetDirectoryName(deckNameWithType);

            return deck;
        }

        public int GetGenesysCredits()
        {
            var value = 0;
            foreach (var card in cards)
                value += card.Card.GetCredit();
            return value;
        }

        #endregion

        #region Protected Functions

        protected override void Awake()
        {
            base.Awake();

            Template.transform.SetParent(transform, false);
            Template.SetActive(false);
            ButtonNoItem.SetSelectEvent(() =>
            {
                if (Program.instance.currentServant == Program.instance.deckEditor)
                    Program.instance.deckEditor.ResponseRegion = DeckEditorUI.ResponseRegion.Deck;
            });
        }

        protected IEnumerator PrintDeckAsync()
        {
            deckLoaded = false;

            if (cards != null)
                foreach (var card in cards)
                    Destroy(card.gameObject);
            cards = new();

            Viewport.alpha = 0f;
            Viewport.blocksRaycasts = false;

            while (Program.instance.deckEditor.inTransition)
                yield return null;
            TweenLoading.Show();

            if (Deck == null)//Online Deck
            {
                while(DeckEditor.Deck == null)
                    yield return null;
                Deck = DeckEditor.Deck;
            }

            foreach (var card in Deck.Main)
            {
                AddCard(CardsManager.Get(card), DeckLocation.MainDeck, false, false);
                yield return null;
            }
            foreach (var card in Deck.Extra)
            {
                AddCard(CardsManager.Get(card), DeckLocation.ExtraDeck, false, false);
                yield return null;
            }
            foreach (var card in Deck.Side)
            {
                AddCard(CardsManager.Get(card), DeckLocation.SideDeck, false, false);
                yield return null;
            }

            RefreshCardsCount(DeckLocation.All);

            TweenLoading.Hide();
            Viewport.alpha = 1f;
            Viewport.blocksRaycasts = true;

            deckLoaded = true;
            SetDirty(false);
            if (cards.Count > 0 && UserInput.NeedDefaultSelect())
                cards[0].GetSelectable().Select();
            if (Program.instance.currentServant == Program.instance.deckBrowser)
                PrePick();
        }

        protected virtual void ChangeGridSpacing(DeckLocation location)
        {
            if ((location & DeckLocation.MainDeck) > 0)
            {
                int count = GetDeckLocationCount(DeckLocation.MainDeck);
                if (count <= defaultMainDeckRows * defaultColumns)
                    MainDeckContent.spacing = defaultSpacing;
                else
                {
                    int columns = Mathf.CeilToInt((float)count / defaultMainDeckRows);
                    var targetSpace = (contentWidth - columns * templateWidth) / (columns - 1);
                    MainDeckContent.spacing = new Vector2(targetSpace, defaultVerticalSpacing);
                }
            }
            if ((location & DeckLocation.ExtraDeck) > 0)
            {
                int count = GetDeckLocationCount(DeckLocation.ExtraDeck);
                if (count <= defaultExtraDeckRows * defaultColumns)
                    ExtraDeckContent.spacing = defaultSpacing;
                else
                {
                    int columns = Mathf.CeilToInt((float)count / defaultExtraDeckRows);
                    var targetSpace = (contentWidth - columns * templateWidth) / (columns - 1);
                    ExtraDeckContent.spacing = new Vector2(targetSpace, defaultVerticalSpacing);
                }
            }
            if ((location & DeckLocation.SideDeck) > 0)
            {
                int count = GetDeckLocationCount(DeckLocation.SideDeck);
                if (count <= defaultSideDeckRows * defaultColumns)
                    SideDeckContent.spacing = defaultSpacing;
                else
                {
                    int columns = Mathf.CeilToInt((float)count / defaultSideDeckRows);
                    var targetSpace = (contentWidth - columns * templateWidth) / (columns - 1);
                    SideDeckContent.spacing = new Vector2(targetSpace, defaultVerticalSpacing);
                }
            }
        }

        protected void RefreshCardsCount(DeckLocation location)
        {
            if ((location & DeckLocation.MainDeck) > 0)
            {
                mainCount = 0;
                int monsterCount = 0;
                int spellCount = 0;
                int trapCount = 0;
                int gc = 0;

                foreach (var card in cards)
                    if (card.location == DeckLocation.MainDeck)
                    {
                        mainCount++;
                        if (card.Card.HasType(CardType.Spell))
                            spellCount++;
                        else if (card.Card.HasType(CardType.Trap))
                            trapCount++;
                        else
                            monsterCount++;
                        gc += card.Card.GetCredit();
                    }
                TextMainDeckCardNum.text = mainCount.ToString();
                TextMainDeckMonsterNum.text = monsterCount.ToString();
                TextMainDeckSpellNum.text = spellCount.ToString();
                TextMainDeckTrapNum.text = trapCount.ToString();
                TextMainDeckGP.text = gc.ToString();
            }
            if ((location & DeckLocation.ExtraDeck) > 0)
            {
                extraCount = 0;
                int fusionCount = 0;
                int synchroCount = 0;
                int xyzCount = 0;
                int linkCount = 0;
                int gc = 0;

                foreach (var card in cards)
                    if (card.location == DeckLocation.ExtraDeck)
                    {
                        extraCount++;
                        if (card.Card.HasType(CardType.Fusion))
                            fusionCount++;
                        else if (card.Card.HasType(CardType.Synchro))
                            synchroCount++;
                        else if (card.Card.HasType(CardType.Xyz))
                            xyzCount++;
                        else if (card.Card.HasType(CardType.Link))
                            linkCount++;
                        gc += card.Card.GetCredit();
                    }
                TextExtraDeckCardNum.text = extraCount.ToString();
                TextExtraDeckFusionNum.text = fusionCount.ToString();
                TextExtraDeckSynchroNum.text = synchroCount.ToString();
                TextExtraDeckXyzNum.text = xyzCount.ToString();
                TextExtraDeckLinkNum.text = linkCount.ToString();
                TextExtraDeckGP.text = gc.ToString();
            }
            if ((location & DeckLocation.SideDeck) > 0)
            {
                sideCount = 0;
                int monsterCount = 0;
                int spellCount = 0;
                int trapCount = 0;
                int gc = 0;

                foreach (var card in cards)
                    if (card.location == DeckLocation.SideDeck)
                    {
                        sideCount++;
                        if (card.Card.HasType(CardType.Spell))
                            spellCount++;
                        else if (card.Card.HasType(CardType.Trap))
                            trapCount++;
                        else
                            monsterCount++;
                        gc += card.Card.GetCredit();
                    }
                TextSideDeckCardNum.text = sideCount.ToString();
                TextSideDeckMonsterNum.text = monsterCount.ToString();
                TextSideDeckSpellNum.text = spellCount.ToString();
                TextSideDeckTrapNum.text = trapCount.ToString();
                TextSideDeckGP.text = gc.ToString();
            }

            if(Program.instance.currentServant == Program.instance.deckEditor)
                SelectionButton_CardInfoType.SetGenesysCredits(GetGenesysCredits());
        }

        protected void SortCards()
        {
            if (cards == null)
                return;
            cards.Sort(ComparisonOfCard());
        }

        internal static Comparison<SelectionButton_CardInDeck> ComparisonOfCard()
        {
            return (left, right) =>
            {
                int a = -1;
                if (left.location < right.location)
                    a = -1;
                else if (right.location < left.location)
                    a = 1;
                else
                {
                    if (left.transform.GetSiblingIndex() <= right.transform.GetSiblingIndex())
                        a = -1;
                    else
                        a = 1;
                }
                return a;
            };
        }

        protected SelectionButton_CardInDeck AddCard(Card data, DeckLocation location, bool playMoveAnimation, bool playBirthAnimation)
        {
            SetDirty(true);

            SortCards();
            TextNoItem.gameObject.SetActive(false);

            if (playMoveAnimation)
                foreach (var card in cards)
                    if (card.location == location)
                        card.LockPosition();

            var template = Instantiate(Template);
            template.SetActive(true);
            template.transform.SetParent(GetDeckLocationParent(location), false);
            var handler = template.GetComponent<SelectionButton_CardInDeck>();
            handler.deckView = this;
            handler.Card = data;
            handler.location = location;
            cards.Add(handler);
            RefreshCardsCount(location);
            ChangeGridSpacing(location);

            if (playBirthAnimation)
                handler.PlayBirthAnimation();

            return handler;
        }

        protected DeckLocation GetHoveredLocation()
        {
            if (MainDeckView.Hover)
                return DeckLocation.MainDeck;
            else if (ExtraDeckView.Hover)
                return DeckLocation.ExtraDeck;
            else if (SideDeckView.Hover)
                return DeckLocation.SideDeck;
            return DeckLocation.All;
        }

        protected bool DeckFileSave()
        {
            try
            {
                var deckName = MDPro3.Duel.YGOSharp.Deck.NormalizeDeckFileName(GetDeckName());
                if (!MDPro3.Duel.YGOSharp.Deck.IsValidDeckFileName(deckName))
                    throw new InvalidOperationException($"Invalid deck name: \"{GetDeckName()}\"");

                Deck.type = deckType;

                var oldDeckPath = GetDeckFilePath(deckFileName);
                if (!Deck.Save(deckName, DateTime.UtcNow))
                    throw new IOException($"Failed to save deck \"{deckName}\".");

                var newDeckPath = GetDeckFilePath(deckName);
                if (IsSameDeckPath(oldDeckPath, newDeckPath))
                    ApplyCaseOnlyRename(oldDeckPath, newDeckPath);
                else if (File.Exists(oldDeckPath))
                    File.Delete(oldDeckPath);

                deckFileName = deckName;
                deckNameWithType = deckType == string.Empty ? deckName : $"{deckType}/{deckName}";
                InputDeckName.text = deckFileName;
                TextDeckName.text = deckFileName;
                MessageManager.Toast(InterString.Get("本地卡组「[?]」已保存。", deckName));
                Config.SetConfigDeck(deckName, true);
                SetDirty(false);
                return true;
            }
            catch (Exception e)
            {
                MessageManager.Toast(InterString.Get("保存失败！"));
                MessageManager.Cast(e.Message);
                return false;
            }
        }

        private string GetDeckFilePath(string deckName)
        {
            return Program.PATH_DECK + (deckType == string.Empty ? string.Empty : $"{deckType}/")
                + deckName + Program.EXPANSION_YDK;
        }

        private static bool IsSameDeckPath(string leftPath, string rightPath)
        {
            var fullLeftPath = Path.GetFullPath(leftPath);
            var fullRightPath = Path.GetFullPath(rightPath);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return string.Equals(fullLeftPath, fullRightPath, StringComparison.OrdinalIgnoreCase);
#else
            return string.Equals(fullLeftPath, fullRightPath, StringComparison.Ordinal);
#endif
        }

        private static void ApplyCaseOnlyRename(string oldPath, string newPath)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            var fullOldPath = Path.GetFullPath(oldPath);
            var fullNewPath = Path.GetFullPath(newPath);

            if (string.Equals(fullOldPath, fullNewPath, StringComparison.Ordinal))
                return;
            if (!string.Equals(fullOldPath, fullNewPath, StringComparison.OrdinalIgnoreCase))
                return;
            if (!File.Exists(fullOldPath))
                return;

            var directory = Path.GetDirectoryName(fullOldPath);
            var tempPath = Path.Combine(directory ?? Program.PATH_DECK, $"__mdpro3_casefix_{Guid.NewGuid():N}.tmp");
            File.Move(fullOldPath, tempPath);
            File.Move(tempPath, fullNewPath);
#endif
        }

        #region Pickup

        private void PrePick()
        {
            for (int i = 0; i < 3; i++) 
            {
                foreach (var card in cards)
                {
                    if (card.Card.Id == DeckEditor.Deck.Pickup[i]
                        && !card.picked)
                    {
                        card.PrePickThis(i);
                        break;
                    }
                }
            }
        }

        public void Pickup(SelectionButton_CardInDeck card)
        {
            foreach(var c in cards)
                if (c != card && c.pickupIndex == card.pickupIndex)
                    c.DepickupThis();
        }

        public void Depickup(int index)
        {
            foreach (var c in cards)
                if (c.pickupIndex == index)
                    c.DepickupThis();
        }

        #endregion

        #endregion

    }
}
