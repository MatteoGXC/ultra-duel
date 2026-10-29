using MDPro3.Servant;
using MDPro3.UI.PropertyOverride;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MDPro3.UI.ServantUI
{
    public class OnlineDeckViewerUI : ServantUI
    {

        #region Elements

        private const string LABEL_SR = "ScrollRect";
        private ScrollRect m_ScrollRect;
        private ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetElement<ScrollRect>(LABEL_SR);

        private const string LABEL_TXT_DECKNUMVALUE = "TextDeckNumValue";
        private TextMeshProUGUI m_TextDeckNumValue;
        private TextMeshProUGUI TextDeckNumValue =>
            m_TextDeckNumValue = m_TextDeckNumValue != null ? m_TextDeckNumValue
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_DECKNUMVALUE);

        private const string LABEL_IPT_DECKNAME = "InputFieldDeckName";
        private TMP_InputField m_InputDeckName;
        public TMP_InputField InputDeckName =>
            m_InputDeckName = m_InputDeckName != null ? m_InputDeckName
            : Manager.GetElement<TMP_InputField>(LABEL_IPT_DECKNAME);

        private const string LABEL_IPT_DECKAUTHOR = "InputFieldDeckAuthor";
        private TMP_InputField m_InputDeckAuthor;
        public TMP_InputField InputDeckAuthor =>
            m_InputDeckAuthor = m_InputDeckAuthor != null ? m_InputDeckAuthor
            : Manager.GetElement<TMP_InputField>(LABEL_IPT_DECKAUTHOR);

        #endregion

        public SuperScrollView superScrollView;
        static Task<GameObject> itemPrefab;
        int printVersion;
        TextMeshProUGUI formatLabel;
        TextMeshProUGUI sortLabel, availabilityLabel;

        public override void Initialize(Servant.Servant servant)
        {
            base.Initialize(servant);
            Title.text = "Premade Decks";
            Transform ControlHost(Transform field)
            {
                while (field.parent != null && field.parent != Root && field.parent != transform
                    && !ScrollRect.transform.IsChildOf(field.parent) && !Title.transform.IsChildOf(field.parent)) field = field.parent;
                return field;
            }
            var nameHost = ControlHost(InputDeckName.transform);
            var authorHost = ControlHost(InputDeckAuthor.transform);
            var input = InputDeckName;
            var bar = new GameObject("Premade browse controls", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            bar.layer = input.gameObject.layer;
            var barRect = bar.GetComponent<RectTransform>();
            barRect.SetParent(Root, false);
            barRect.anchorMin = new Vector2(.02f, 1); barRect.anchorMax = new Vector2(.98f, 1); barRect.pivot = new Vector2(.5f, 1);
            // The native header ends 120 units below the top. Leave a clear gap
            // before the browse row rather than straddling its divider.
            const float browseTop = 140f;
            const float browseHeight = 66f;
            barRect.sizeDelta = new Vector2(0, browseHeight); barRect.anchoredPosition = new Vector2(0, -browseTop);
            bar.GetComponent<Image>().color = new Color(.025f, .045f, .065f, .98f);
            var layout = bar.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8); layout.spacing = 12;
            layout.childAlignment = TextAnchor.MiddleLeft; layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            input.transform.SetParent(barRect, false);
            input.transform.localScale = Vector3.one;
            var inputLayout = input.GetComponent<LayoutElement>() ?? input.gameObject.AddComponent<LayoutElement>();
            inputLayout.minWidth = 230; inputLayout.preferredWidth = 500; inputLayout.flexibleWidth = 1;
            inputLayout.minHeight = inputLayout.preferredHeight = 50;
            if (nameHost != input.transform) nameHost.gameObject.SetActive(false);
            authorHost.gameObject.SetActive(false);
            if (InputDeckName.placeholder is TMP_Text placeholder) placeholder.text = "Search archetype";
            InputDeckName.onValueChanged.AddListener(_ => OnSearchSubmit());
            formatLabel = BrowseButton(barRect, "PremadeFormatFilter", 270, ShowFormatPicker);
            sortLabel = BrowseButton(barRect, "PremadeSort", 240, ShowSortPicker);
            availabilityLabel = BrowseButton(barRect, "PremadeAvailability", 240, () => { OnlineDeckViewer.AvailableOnly = !OnlineDeckViewer.AvailableOnly; UpdateFilterLabel(); OnSearchSubmit(); });
            InputDeckAuthor.gameObject.SetActive(false);
            var scroll = ScrollRect.GetComponent<RectTransform>();
            scroll.SetParent(Root, false); scroll.localScale = Vector3.one;
            scroll.anchorMin = new Vector2(.02f, .025f); scroll.anchorMax = new Vector2(.98f, 1);
            scroll.offsetMin = Vector2.zero; scroll.offsetMax = new Vector2(0, -(browseTop + browseHeight + 12f));
            barRect.SetAsLastSibling();
            UpdateFilterLabel();
        }

        TextMeshProUGUI BrowseButton(RectTransform parent, string name, float width, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false);
            var sizing = go.GetComponent<LayoutElement>();
            sizing.minWidth = width; sizing.preferredWidth = width; sizing.minHeight = sizing.preferredHeight = 50;
            go.GetComponent<Image>().color = new Color(.06f, .10f, .14f, 1);
            go.GetComponent<Button>().onClick.AddListener(action);
            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.layer = go.layer; label.transform.SetParent(go.transform, false);
            var text = label.GetComponent<TextMeshProUGUI>();
            text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(8, 0); text.rectTransform.offsetMax = new Vector2(-8, 0);
            UltraTheme.Style(text, true); text.fontSize = 22; text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true; text.fontSizeMin = 16; text.fontSizeMax = 22; text.raycastTarget = false;
            return text;
        }

        void UpdateFilterLabel()
        {
            if (formatLabel != null) formatLabel.text = "Format: " + (OnlineDeckViewer.FormatFilter == "TCG" ? "TCG Advanced" : OnlineDeckViewer.FormatFilter) + " >";
            if (sortLabel != null) sortLabel.text = "Sort: " + OnlineDeckViewer.SortOrder + " >";
            if (availabilityLabel != null) availabilityLabel.text = "Available only: " + (OnlineDeckViewer.AvailableOnly ? "On" : "Off");
        }

        public void SetFormatFilter(string label)
        {
            OnlineDeckViewer.FormatFilter = label;
            UpdateFilterLabel();
            OnSearchSubmit();
        }

        public void ShowFormatPicker()
        {
            UIManager.ShowPopupSelection(new List<string> { "Premade deck format", "Choose a starter format. Unavailable recipes explain which rules prevent them.",
                "TCG Advanced", "Genesys 100", "Genesys 0" }, () => {
                    var selected = EventSystem.current.currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
                    SetFormatFilter(selected == "TCG Advanced" ? "TCG" : selected);
                });
        }

        public void SetSortOrder(string order)
        {
            OnlineDeckViewer.SortOrder = order;
            UpdateFilterLabel();
            OnSearchSubmit();
        }

        public void ShowSortPicker()
        {
            UIManager.ShowPopupSelection(new List<string> { "Sort premade decks", "Choose how the current results are ordered.",
                "A–Z", "Z–A", "Lowest points", "Highest points" }, () =>
                SetSortOrder(EventSystem.current.currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText()));
        }

        protected override void AfterHideEvent()
        {
            base.AfterHideEvent();
            printVersion++;
            Program.instance.onlineDeckViewer.lastSelectedDeckItem = null;
            superScrollView?.Clear();
        }

        public void OnSearchSubmit()
        {
            Program.instance.onlineDeckViewer.RefreshList();
        }

        public void Print()
        {
            _ = PrintAsync(++printVersion, OnlineDeckViewer.premades);
        }

        async Task PrintAsync(int version, PremadeDeckLibrary.Entry[] entries)
        {
            try
            {
                itemPrefab ??= Addressables.LoadAssetAsync<GameObject>("UI/ItemDeckOnline.prefab").Task;
                var prefab = await itemPrefab;
                if (this == null || !gameObject.activeInHierarchy || version != printVersion) return;
                superScrollView?.Clear();
                TextDeckNumValue.text = entries.Count(e => e.available) + " / " + entries.Length;
                var itemWidth = PropertyOverrider.NeedMobileLayout() ? 336f : 260f;
                var itemHeight = PropertyOverrider.NeedMobileLayout() ? 300f : 232f;
                var space = PropertyOverrider.NeedMobileLayout() ? 30f : 24f;
                var bottomPadding = (PropertyOverrider.NeedMobileLayout() ? 196f : 150f) - space;
                superScrollView ??= new SuperScrollView(
                    -1,
                    itemWidth + space,
                    itemHeight + space,
                    10,
                    bottomPadding,
                    prefab,
                    ItemOnListRefresh,
                    ScrollRect);
                List<string[]> tasks = new();
                foreach (var deck in entries)
                {
                    var task = new string[10]
                    {
                        deck.archetype, deck.label, deck.id, "1080001",
                        (deck.covers?.ElementAtOrDefault(0) ?? 0).ToString(),
                        (deck.covers?.ElementAtOrDefault(1) ?? 0).ToString(),
                        (deck.covers?.ElementAtOrDefault(2) ?? 0).ToString(),
                        "1070001", "0", deck.Summary
                    };
                    tasks.Add(task);
                }
                superScrollView.Print(tasks);
                if (superScrollView.items.Count > 0)
                {
                    Program.instance.onlineDeckViewer.lastSelectedDeckItem
                        = superScrollView.items[0].gameObject.GetComponent<SelectionToggle_DeckOnline>();
                    if (Cursor.lockState == CursorLockMode.Locked)
                        Program.instance.onlineDeckViewer.Select();
                }
            }
            catch (Exception e)
            {
                if (this != null) MessageManager.Cast("Premade deck library: " + e.Message);
            }
        }

        private void ItemOnListRefresh(string[] task, GameObject item)
        {
            var handler = item.GetComponent<SelectionToggle_DeckOnline>();
            handler.deckName = task[0];
            handler.deckAuthor = task[1];
            handler.deckId = task[2];
            handler.deckCase = int.Parse(task[3]);
            handler.card0 = int.Parse(task[4]);
            handler.card1 = int.Parse(task[5]);
            handler.card2 = int.Parse(task[6]);
            handler.protector = task[7];
            handler.like = int.Parse(task[8]);
            handler.lastDate = task[9];
            handler.Refresh();
        }
    }
}
