using MDPro3.Duel.YGOSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;

namespace MDPro3.UI
{
    public class CardCollectionView : UIWidget
    {

        #region Elements

        #region Root

        private const string LABEL_SBN_NOITEM = "NoItemButton";
        private SelectionButton m_ButtonNoItem;
        protected SelectionButton ButtonNoItem =>
            m_ButtonNoItem = m_ButtonNoItem != null ? m_ButtonNoItem
            : Manager.GetElement<SelectionButton>(LABEL_SBN_NOITEM);

        private const string LABEL_GPC_CURSORWINDOWSELECT = "CursorWindowSelect";
        private GamepadCursor m_CursorWindowSelect;
        protected GamepadCursor CursorWindowSelect =>
            m_CursorWindowSelect = m_CursorWindowSelect != null ? m_CursorWindowSelect
            : Manager.GetElement<GamepadCursor>(LABEL_GPC_CURSORWINDOWSELECT);

        #endregion

        #region TabArea

        private const string LABEL_STG_CARDLIST = "TabArea/CardListToggle";
        private SelectionToggle_CardCollectionTab m_ToggleCardList;
        protected SelectionToggle_CardCollectionTab ToggleCardList =>
            m_ToggleCardList = m_ToggleCardList != null ? m_ToggleCardList
            : Manager.GetNestedElement<SelectionToggle_CardCollectionTab>(LABEL_STG_CARDLIST);

        private const string LABEL_STG_BOOKMARK = "TabArea/BookmarkToggle";
        private SelectionToggle_CardCollectionTab m_ToggleBookmark;
        protected SelectionToggle_CardCollectionTab ToggleBookmark =>
            m_ToggleBookmark = m_ToggleBookmark != null ? m_ToggleBookmark
            : Manager.GetNestedElement<SelectionToggle_CardCollectionTab>(LABEL_STG_BOOKMARK);

        private const string LABEL_STG_HISTORY = "TabArea/HistoryToggle";
        private SelectionToggle_CardCollectionTab m_ToggleHistory;
        protected SelectionToggle_CardCollectionTab ToggleHistory =>
            m_ToggleHistory = m_ToggleHistory != null ? m_ToggleHistory
            : Manager.GetNestedElement<SelectionToggle_CardCollectionTab>(LABEL_STG_HISTORY);

        #endregion

        #region FilterAndSortArea

        private const string LABEL_GO_FILTERANDSORTAREA = "FilterAndSortArea";
        private GameObject m_FilterAndSortArea;
        protected GameObject FilterAndSortArea =>
            m_FilterAndSortArea = m_FilterAndSortArea != null ? m_FilterAndSortArea
            : Manager.GetElement(LABEL_GO_FILTERANDSORTAREA);

        private const string LABEL_IPT_SEARCH = "FilterAndSortArea/InputField";
        private SelectionInputField m_InputSearch;
        public SelectionInputField InputSearch =>
            m_InputSearch = m_InputSearch != null ? m_InputSearch
            : Manager.GetNestedElement<SelectionInputField>(LABEL_IPT_SEARCH);

        private const string LABEL_SBN_SEARCH = "FilterAndSortArea/SearchButton";
        private SelectionButton m_ButtonSearch;
        protected SelectionButton ButtonSearch =>
            m_ButtonSearch = m_ButtonSearch != null ? m_ButtonSearch
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_SEARCH);

        private const string LABEL_STG_FILTER = "FilterAndSortArea/FilterToggle";
        private SelectionToggle_CardFilter m_ToggleFilter;
        protected SelectionToggle_CardFilter ToggleFilter =>
            m_ToggleFilter = m_ToggleFilter != null ? m_ToggleFilter
            : Manager.GetNestedElement<SelectionToggle_CardFilter>(LABEL_STG_FILTER);

        private const string LABEL_SBN_SORT = "FilterAndSortArea/SortButton";
        private SelectionButton m_ButtonSort;
        protected SelectionButton ButtonSort =>
            m_ButtonSort = m_ButtonSort != null ? m_ButtonSort
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_SORT);

        private const string LABEL_SBN_CLEAR = "FilterAndSortArea/ClearButton";
        private SelectionButton m_ButtonClear;
        protected SelectionButton ButtonClear =>
            m_ButtonClear = m_ButtonClear != null ? m_ButtonClear
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_CLEAR);

        #endregion

        #region RelatedArea

        private const string LABEL_GO_RELATEDAREA = "RelatedArea";
        private GameObject m_RelatedArea;
        protected GameObject RelatedArea =>
            m_RelatedArea = m_RelatedArea != null ? m_RelatedArea
            : Manager.GetNestedElement(LABEL_GO_RELATEDAREA);

        private const string LABEL_SBN_RELATEDCARD = "RelatedArea/RelatedCard/RelatedCardButton";
        private SelectionButton m_ButtonRelatedCard;
        protected SelectionButton ButtonRelatedCard =>
            m_ButtonRelatedCard = m_ButtonRelatedCard != null ? m_ButtonRelatedCard
            : Manager.GetNestedElement<SelectionButton>(LABEL_SBN_RELATEDCARD);

        private const string LABEL_TXT_RELATEDCARD = "RelatedArea/RelatedCard/RelatedCardText";
        private TextMeshProUGUI m_TextRelatedCard;
        protected TextMeshProUGUI TextRelatedCard =>
            m_TextRelatedCard = m_TextRelatedCard != null ? m_TextRelatedCard
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_RELATEDCARD);

        private const string LABEL_BTN_CLOSE = "RelatedArea/CloseButton";
        private SelectionButton m_ButtonClose;
        protected SelectionButton ButtonClose =>
            m_ButtonClose = m_ButtonClose != null ? m_ButtonClose
            : Manager.GetNestedElement<SelectionButton>(LABEL_BTN_CLOSE);

        #endregion

        #region CardListArea

        private const string LABEL_RT_COLLECTIONAREACENTER = "CardListArea/CollectionAreaCenter";
        private RectTransform m_CollectionAreaCenter;
        protected RectTransform CollectionAreaCenter =>
            m_CollectionAreaCenter = m_CollectionAreaCenter != null ? m_CollectionAreaCenter
            : Manager.GetNestedElement<RectTransform>(LABEL_RT_COLLECTIONAREACENTER);

        private const string LABEL_SR_CARDLIST = "CardListArea/CardList";
        private ScrollRect m_ScrollRect;
        protected ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetNestedElement<ScrollRect>(LABEL_SR_CARDLIST);

        private const string LABEL_GO_TEMPLATE = "CardListArea/CardList/template";
        private GameObject m_Template;
        protected GameObject Template =>
            m_Template = m_Template != null ? m_Template
            : Manager.GetNestedElement(LABEL_GO_TEMPLATE);

        private const string LABEL_TXT_NOITEM = "CardListArea/NoItemText";
        private TextMeshProUGUI m_TextNoItem;
        protected TextMeshProUGUI TextNoItem =>
            m_TextNoItem = m_TextNoItem != null ? m_TextNoItem
            : Manager.GetNestedElement<TextMeshProUGUI>(LABEL_TXT_NOITEM);

        private const string LABEL_DTM_LOADING = "CardListArea/Loading";
        private DoTweenManager m_Loading;
        protected DoTweenManager Loading =>
            m_Loading = m_Loading != null ? m_Loading
            : Manager.GetNestedElement<DoTweenManager>(LABEL_DTM_LOADING);

        #endregion

        #region Others

        private const string LABEL_DA_DROPAREA = "DropArea";
        private DropArea m_DropArea;
        protected DropArea DropArea =>
            m_DropArea = m_DropArea != null ? m_DropArea
            : Manager.GetElement<DropArea>(LABEL_DA_DROPAREA);

        #endregion


        #endregion

        #region Reference

        public enum Area
        {
            Collection = 0,
            Bookmark = 1,
            History = 2,
        }
        [HideInInspector] public Area area = Area.Collection;
        [HideInInspector] public Area defaultArea = Area.Collection;
        [HideInInspector] public bool showingRelatedCards;

        public enum SortOrder
        {
            ByType = 1,
            ByTypeReverse = 2,
            ByLevelUp = 3,
            ByLevelDown = 4,
            ByAttackUp = 5,
            ByAttackDown = 6,
            ByDefenceUp = 7,
            ByDefenceDown = 8,
            ByRarityUp = 9,
            ByRarityDown = 10,
            ByGPUp = 11,
            ByGPDown = 12
        }
        public static SortOrder _SortOrder = SortOrder.ByType;

        public SuperScrollView superScrollView;
        public static List<long> filters = new();
        public static string packName = string.Empty;
        [HideInInspector] public List<int> historyCards = new();
        [HideInInspector] public List<int> printedCards;

        #endregion

        #region Public Functions

        public void SetNoItemButtonNavigationEvent(MoveDirection direction, UnityAction action)
        {
            ButtonNoItem.SetNavigationEvent(direction, action);
        }

        public void SelectDefaultItem()
        {
            if (superScrollView.gameObjects.Count > 0)
                EventSystem.current.SetSelectedGameObject(superScrollView.gameObjects[0]);
            else if(showingRelatedCards)
                ButtonRelatedCard.GetSelectable().Select();
            else
                ButtonNoItem.GetSelectable().Select();
        }

        public void SelectNearestCard(Vector3 position)
        {
            if (superScrollView.gameObjects.Count == 0)
            {
                ButtonNoItem.GetSelectable().Select();
                return;
            }

            var distance = new Dictionary<GameObject, float>();
            foreach (var card in superScrollView.gameObjects)
                distance.Add(card, Tools.CalculateWeightedDistance(position, card.transform.GetChild(0).position, 'x'));
            EventSystem.current.SetSelectedGameObject(distance.Aggregate((left, right) => left.Value < right.Value ? left : right).Key);
        }

        public void PrintSearchCards(string text = "")
        {
            var cards = new List<int>();
            var results = CardsManager.Search(InputSearch.InputField.text, filters, DeckEditor.Banlist, packName, namesOnly: true);
            SortCards(results);
            foreach(var card in results)
                cards.Add(card.Id);

            ButtonSearch.SetButtonText(cards.Count == 0 ? InterString.Get("搜索") : cards.Count.ToString());

            PrintCards(cards);
        }

        protected Card relatedCardData;
        protected void PrintRelatedCards(Card data)
        {
            relatedCardData = data;
            var cards = new List<int>();
            var results = CardsManager.RelatedSearch(data.Id);
            SortCards(results);
            foreach (var card in results)
                cards.Add(card.Id);

            ButtonSearch.SetButtonText(cards.Count == 0 ? InterString.Get("搜索") : cards.Count.ToString());

            PrintCards(cards);
        }

        public void PrintBookmarkCards()
        {
            PrintCards(CardRarity.GetBookCards());
        }

        public void PrintHistoryCards()
        {
            PrintCards(historyCards);
        }

        private void SortCards(List<Card> cards)
        {
            switch (_SortOrder)
            {
                case SortOrder.ByType:
                    cards.Sort(CardsManager.ComparisonOfCard());
                    break;
                case SortOrder.ByTypeReverse:
                    cards.Sort(CardsManager.ComparisonOfCardReverse());
                    break;
                case SortOrder.ByLevelUp:
                    cards.Sort(CardsManager.ComparisonOfCard_LV_Up());
                    break;
                case SortOrder.ByLevelDown:
                    cards.Sort(CardsManager.ComparisonOfCard_LV_Down());
                    break;
                case SortOrder.ByAttackUp:
                    cards.Sort(CardsManager.ComparisonOfCard_ATK_Up());
                    break;
                case SortOrder.ByAttackDown:
                    cards.Sort(CardsManager.ComparisonOfCard_ATK_Down());
                    break;
                case SortOrder.ByDefenceUp:
                    cards.Sort(CardsManager.ComparisonOfCard_DEF_Up());
                    break;
                case SortOrder.ByDefenceDown:
                    cards.Sort(CardsManager.ComparisonOfCard_DEF_Down());
                    break;
                case SortOrder.ByRarityUp:
                    cards.Sort(CardsManager.ComparisonOfCard_Rarity_Up());
                    break;
                case SortOrder.ByRarityDown:
                    cards.Sort(CardsManager.ComparisonOfCard_Rarity_Down());
                    break;
                case SortOrder.ByGPUp:
                    cards.Sort(CardsManager.ComparisonOfCard_GP_Up());
                    break;
                case SortOrder.ByGPDown:
                    cards.Sort(CardsManager.ComparisonOfCard_GP_Down());
                    break;
            }
        }

        public void AddHistoryCard(int code)
        {
            historyCards.Remove(code);
            historyCards.Insert(0, code);
            if (area == Area.History)
                PrintHistoryCards();
        }

        public void AddHistoryCards(List<int> codes)
        {
            foreach (var code in codes)
            {
                historyCards.Remove(code);
                historyCards.Insert(0, code);
            }
            if (area == Area.History)
                PrintHistoryCards();
        }

        public void SetSortIcon(Sprite icon)
        {
            ButtonSort.SetIconSprite(icon);
        }

        public void SetSortText(string text)
        {
            ButtonSort.SetButtonText(text);
        }

        public void SetCursor(bool selected)
        {
            CursorWindowSelect.Show = selected;
            foreach (var shortcut in transform.GetComponentsInChildren<ShortcutIcon>(true))
                shortcut.GroupShow = selected;
        }

        public void OnTabRight()
        {
            ToggleCardList.OnRightSelection();
        }

        public Vector3 GetRubbishBinPositon()
        {
            if (area == Area.Collection)
                return CollectionAreaCenter.position;
            else
                return ToggleCardList.transform.position;
        }

        public void ShowFilters()
        {
            var handle = Addressables.InstantiateAsync("Popup/PopupSearchFilter.prefab");
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(Program.instance.ui_.popup, false);
                var popupSearchFilter = result.Result.GetComponent<UI.Popup.PopupSearchFilter>();
                popupSearchFilter.Show();
            };
        }

        public void ResetFilters()
        {
            AudioManager.PlaySE("SE_MENU_DECIDE");
            filters.Clear();
            packName = string.Empty;
            SelectionToggle_CardFilter.Instance.SetToggleOff();
            InputSearch.InputField.text = string.Empty;
            PrintSearchCards();
        }

        public void ShowSortOrder()
        {
            var handle = Addressables.InstantiateAsync("Popup/PopupSearchOrder.prefab");
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(Program.instance.ui_.popup, false);
                result.Result.GetComponent<UI.Popup.PopupSearchOrder>().Show();
            };
        }

        public void ActivateInputField()
        {
            InputSearch.InputField.ActivateInputField();
        }

        public TMP_InputField GetInputField()
        {
            return InputSearch.InputField;
        }        

        public void RefreshCardCount()
        {
            foreach (var go in superScrollView.gameObjects)
                go.GetComponent<SelectionButton_CardInCollection>()
                    .RefreshCountIcon();
        }

        public void SetCardInfoType()
        {
            foreach (var go in superScrollView.gameObjects)
                go.GetComponent<SelectionButton_CardInCollection>()
                    .RefreshIcons();
        }

        public void ShowArea(Area area)
        {
            if (this.area == area) return;
            FilterAndSortArea.SetActive(area == Area.Collection);
            this.area = area;
            if (area == Area.Collection)
            {
                if (showingRelatedCards)
                    ShowRelatedCard(relatedCardData);
                else
                {
                    HideRelatedCardArea();
                    PrintSearchCards();
                }
            }
            else if (area == Area.Bookmark)
            {
                HideRelatedCardArea();
                PrintBookmarkCards();
            }
            else if (area == Area.History)
            {
                HideRelatedCardArea();
                PrintHistoryCards();
            }

            SetDropArea();
        }

        public void RefreshRarity(int code)
        {
            foreach (var go in superScrollView.gameObjects)
                go.GetComponent<SelectionButton_CardInCollection>()
                    .RefreshRarity(code);
            if (showingRelatedCards)
                ButtonRelatedCard.GetComponent<CardRawImageHandler>().RefreshRarity(code);
        }

        public void ShowRelatedCard(Card data)
        {
            showingRelatedCards = true;

            ToggleCardList.SetToggleOn(false);
            FilterAndSortArea.SetActive(true);
            InputSearch.SetInteractable(false);
            ButtonSearch.SetInteractable(false);
            ToggleFilter.SetInteractable(false);
            ButtonSort.SetInteractable(false);
            ButtonClear.SetInteractable(false);

            RelatedArea.SetActive(true);
            ButtonRelatedCard.GetComponent<CardRawImageHandler>()
                .SetCard(data);
            TextRelatedCard.text = InterString.Get("「[?]」的相关卡片", data.Name);
            PrintRelatedCards(data);
        }

        public void HideRelatedCard()
        {
            showingRelatedCards = false;
            HideRelatedCardArea();
            PrintSearchCards();
        }

        protected void HideRelatedCardArea()
        {
            InputSearch.SetInteractable(true);
            ButtonSearch.SetInteractable(true);
            ToggleFilter.SetInteractable(true);
            ButtonSort.SetInteractable(true);
            ButtonClear.SetInteractable(true);

            RelatedArea.SetActive(false);
        }

        public GameObject GetUpNavigationObject()
        {
            if(!showingRelatedCards)
                return null;

            return ButtonRelatedCard.gameObject;
        }

        public void OnRalatedAreaNavigationDown()
        {
            if(Program.instance.deckEditor.lastSelectedCardInCollection != null)
            {
                UserInput.NextSelectionIsAxis = true;
                EventSystem.current.SetSelectedGameObject
                    (Program.instance.deckEditor.lastSelectedCardInCollection.gameObject);
            }
        }

        public void SetDropAreaActive(bool active)
        {
            DropArea.active = active;
        }

        public void SetBookmarkDropArea(int code)
        {
            if (area != Area.Bookmark)
                return;
            DropArea.ClearLabels();
            if (CardRarity.CardBookmarked(code))
                DropArea.SetShowLabel(UIHover.LABEL_CANNOTADDBOOKMARK);
            else
                DropArea.SetShowLabel(UIHover.LABEL_ADDBOOKMARK);
            if (DeckEditor.UseMobileLayout)
            {
                DropArea.SetShowLabel(UIHover.LABEL_RIGHT);
                DropArea.SetShowLabel(UIHover.LABEL_MAINDECK);
                DropArea.SetShowLabel(UIHover.LABEL_EXTRADECK);
                DropArea.SetShowLabel(UIHover.LABEL_SIDEDECK);
            }
        }

        #endregion

        #region Protected Functions

        protected override void Awake()
        {
            base.Awake();
            _SortOrder = SortOrder.ByType;

            Template.transform.SetParent(transform, false);
            Template.SetActive(false);

            superScrollView = new SuperScrollView
            (
                6, 
                DeckEditor.UseMobileLayout ? 158 : 88,
                DeckEditor.UseMobileLayout ? 239 : 143,
                DeckEditor.UseMobileLayout ? 10 : 5,
                DeckEditor.UseMobileLayout ? 10 : 5,
                Template, ItemOnListRefresh, ScrollRect
            );

            InputSearch.InputField.onEndEdit.AddListener(PrintSearchCards);
            ButtonSearch.SetClickEvent(() => PrintSearchCards());
            ButtonSort.SetClickEvent(ShowSortOrder);
            ButtonClear.SetClickEvent(ResetFilters);

            RelatedArea.SetActive(false);

            ButtonRelatedCard.SetClickEvent(() =>
            {
                Program.instance.deckEditor.GetUI<DeckEditorUI>().ShowDetail(relatedCardData);
            });
            ButtonClose.SetClickEvent(HideRelatedCard);
            if (DeckEditor.condition != DeckEditor.Condition.ChangeSide)
                SetDropArea();
            else
                defaultArea = Area.History;
        }

        protected void OnDestroy()
        {
            superScrollView?.Clear();
            filters.Clear();
            packName = string.Empty;
        }

        protected void ItemOnListRefresh(string[] tasks, GameObject item)
        {
            var handler = item.GetComponent<SelectionButton_CardInCollection>();
            handler.CardCode = int.Parse(tasks[0]);
            handler.cardCollectionView = this;
        }

        protected void PrintCards(List<int> cards)
        {
            cards=cards.Select(id=>CardsManager.Get(id)).Where(c=>c!=null && c.Id!=0)
                .GroupBy(c=>c.Name.Trim(),StringComparer.OrdinalIgnoreCase)
                .Select(g=>g.OrderBy(c=>c.Alias==0?0:1).ThenBy(c=>c.Id).First().Id).ToList();
            if(area==Area.Collection)ButtonSearch.SetButtonText(cards.Count==0?InterString.Get("搜索"):cards.Count.ToString());
            TextNoItem.gameObject.SetActive(cards.Count == 0);
            printedCards = cards;

            var args = new List<string[]>();
            for(int i = 0; i < cards.Count; i++)
            {
                var arg = new string[1] { cards[i].ToString() };
                args.Add(arg);
            }
            superScrollView.Print(args);
            if (Program.instance.deckEditor.ResponseRegion == DeckEditorUI.ResponseRegion.Collection)
                SelectDefaultItem();
        }

        protected void SetDropArea()
        {
            if (area == Area.Bookmark)
            {
                DropArea.ClearLabels();
                DropArea.SetShowLabel(UIHover.LABEL_ADDBOOKMARK);
            }
            else
            {
                DropArea.ClearLabels();
                if(DeckEditor.condition != DeckEditor.Condition.ChangeSide)
                    DropArea.SetShowLabel(UIHover.LABEL_REMOVEDECK);
            }
            if (DeckEditor.UseMobileLayout)
            {
                DropArea.SetShowLabel(UIHover.LABEL_MAINDECK);
                DropArea.SetShowLabel(UIHover.LABEL_EXTRADECK);
                DropArea.SetShowLabel(UIHover.LABEL_SIDEDECK);
                if (DeckEditor.condition != DeckEditor.Condition.ChangeSide)
                    DropArea.SetShowLabel(UIHover.LABEL_RIGHT);
            }
        }

        #endregion

    }
}
