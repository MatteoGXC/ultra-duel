using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MDPro3.Servant;
using MDPro3.Net;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using MDPro3.UI.PropertyOverride;

namespace MDPro3.UI.ServantUI
{
    public class SoloSelectorUI : ServantUI
    {

        #region Elements

        private const string LABEL_SR = "ScrollRect";
        private ScrollRect m_ScrollRect;
        public ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetElement<ScrollRect>(LABEL_SR);

        private const string LABEL_TXT_OVERVIEW = "TextOverview";
        private TextMeshProUGUI m_TextOverview;
        protected TextMeshProUGUI TextOverview =>
            m_TextOverview = m_TextOverview != null ? m_TextOverview
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_OVERVIEW);

        private const string LABEL_SBN_DECK = "ButtonDeck";
        private SelectionButton m_ButtonDeck;
        protected SelectionButton ButtonDeck =>
            m_ButtonDeck = m_ButtonDeck != null ? m_ButtonDeck
            : Manager.GetElement<SelectionButton>(LABEL_SBN_DECK);

        private const string LABEL_STG_LOCKHAND = "Settings/ToggleLockHand";
        private SelectionToggle m_ToggleLockHand;
        public SelectionToggle ToggleLockHand =>
            m_ToggleLockHand = m_ToggleLockHand != null ? m_ToggleLockHand
            : Manager.GetNestedElement<SelectionToggle>(LABEL_STG_LOCKHAND);

        private const string LABEL_STG_NOCHECK = "Settings/ToggleNoCheck";
        private SelectionToggle m_ToggleNoCheck;
        protected SelectionToggle ToggleNoCheck =>
            m_ToggleNoCheck = m_ToggleNoCheck != null ? m_ToggleNoCheck
            : Manager.GetNestedElement<SelectionToggle>(LABEL_STG_NOCHECK);

        private const string LABEL_STG_NOSHUFFLE = "Settings/ToggleNoShuffle";
        private SelectionToggle m_ToggleNoShuffle;
        protected SelectionToggle ToggleNoShuffle =>
            m_ToggleNoShuffle = m_ToggleNoShuffle != null ? m_ToggleNoShuffle
            : Manager.GetNestedElement<SelectionToggle>(LABEL_STG_NOSHUFFLE);

        private const string LABEL_IPT_PORT = "Settings/InputFieldPort";
        private TMP_InputField m_InputPort;
        protected TMP_InputField InputPort =>
            m_InputPort = m_InputPort != null ? m_InputPort
            : Manager.GetNestedElement<TMP_InputField>(LABEL_IPT_PORT);

        private const string LABEL_IPT_LP = "Settings/InputFieldLP";
        private TMP_InputField m_InputLP;
        protected TMP_InputField InputLP =>
            m_InputLP = m_InputLP != null ? m_InputLP
            : Manager.GetNestedElement<TMP_InputField>(LABEL_IPT_LP);

        private const string LABEL_IPT_HAND = "Settings/InputFieldHand";
        private TMP_InputField m_InputHand;
        protected TMP_InputField InputHand =>
            m_InputHand = m_InputHand != null ? m_InputHand
            : Manager.GetNestedElement<TMP_InputField>(LABEL_IPT_HAND);

        private const string LABEL_IPT_DRAW = "Settings/InputFieldDraw";
        private TMP_InputField m_InputDraw;
        protected TMP_InputField InputDraw =>
            m_InputDraw = m_InputDraw != null ? m_InputDraw
            : Manager.GetNestedElement<TMP_InputField>(LABEL_IPT_DRAW);

        #endregion

        private const int DEFAULT_PORT = 7911;
        private const int DEFAULT_LP = 8000;
        private const int DEFAULT_HAND = 5;
        private const int DEFAULT_DRAW = 1;

        public SuperScrollView superScrollView;
        private TextMeshProUGUI formatLabel;
        private TextMeshProUGUI playerDeckLabel;
        private GameObject formatControls;
        private GameObject sourceControl;
        private string selectedSource;

        public override void Initialize(Servant.Servant servant)
        {            
            base.Initialize(servant);
            ButtonDeck.gameObject.SetActive(false);
            formatControls = new GameObject("Solo format controls", typeof(RectTransform));
            formatControls.layer = gameObject.layer;
            var controls = formatControls.GetComponent<RectTransform>();
            controls.SetParent(Root, false);
            controls.anchorMin = Vector2.zero;
            controls.anchorMax = Vector2.one;
            controls.offsetMin = controls.offsetMax = Vector2.zero;
            playerDeckLabel = AddSoloControl("Your deck", .34f, .30f, () => {
                Program.instance.deckSelector.SwitchCondition(DeckSelector.Condition.ForSolo);
                Program.instance.ShiftToServant(Program.instance.deckSelector);
            });
            formatLabel = AddSoloControl("Format", .66f, .29f, OnChangeSoloFormat);
            var sourceLabel = AddSoloControl("Source", .55f, .38f, () => {
                if (!string.IsNullOrEmpty(selectedSource)) Application.OpenURL(selectedSource);
            });
            sourceLabel.text = "View published list  >";
            sourceControl = sourceLabel.transform.parent.gameObject;
            var sourceRect = sourceControl.GetComponent<RectTransform>();
            sourceRect.anchorMin = new Vector2(.55f, .18f);
            sourceRect.anchorMax = new Vector2(.93f, .235f);
            sourceControl.SetActive(false);
            TextOverview.enableAutoSizing = false;
            TextOverview.fontSize = 25;
            TextOverview.textWrappingMode = TextWrappingModes.Normal;
            if (GetComponent<UltraSoloRepaint>() == null) gameObject.AddComponent<UltraSoloRepaint>();
        }

        private TextMeshProUGUI AddSoloControl(string name, float x, float width, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UltraChrome), typeof(Button));
            go.layer = gameObject.layer;
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(formatControls.transform, false);
            rect.anchorMin = new Vector2(x, .92f);
            rect.anchorMax = new Vector2(x + width, .985f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var plate = go.GetComponent<UltraChrome>();
            plate.color = UltraTheme.PanelSolid;
            plate.cut = UltraCut.Small;
            plate.rim = 1.5f;
            plate.followThemeRim = true;
            plate.raycastTarget = true;
            var button = go.GetComponent<Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
            var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.layer = gameObject.layer;
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14, 3);
            textRect.offsetMax = new Vector2(-14, -3);
            var label = textObject.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 27;
            label.enableAutoSizing = true;
            label.fontSizeMin = 17;
            label.fontSizeMax = 27;
            label.alignment = TextAlignmentOptions.Center;
            label.color = UltraTheme.Fg;
            label.raycastTarget = false;
            UltraTheme.Style(label, true, 6f);
            label.color = UltraTheme.Fg;
            return label;
        }

        public void OnChangeSoloFormat()
        {
            DuelFormat.Select(() => {
                SoloSelector.HasFormatOverride = true;
                formatLabel.text = "Format: " + DuelFormat.DisplayName + "  >";
                Program.instance.solo.lastSoloItem = null;
                Program.instance.solo.LoadBots();
            });
        }

        public override void ShowEvent()
        {
            base.ShowEvent();
            formatControls.SetActive(SoloSelector.condition == SoloSelector.Condition.ForSolo);
            formatLabel.text = "Format: " + DuelFormat.DisplayName + "  >";
            playerDeckLabel.text = "Your deck: " + Config.GetConfigDeckName() + "  >";

            switch (SoloSelector.condition)
            {
                case SoloSelector.Condition.ForSolo:
                    ToggleNoCheck.gameObject.SetActive(false);
                    ToggleNoShuffle.gameObject.SetActive(true);
                    InputPort.gameObject.SetActive(true);
                    InputLP.gameObject.SetActive(true);
                    InputHand.gameObject.SetActive(true);
                    InputDraw.gameObject.SetActive(true);

                    YgoServer.StopServer();
                    break;
                case SoloSelector.Condition.ForRoom:
                    ToggleNoCheck.gameObject.SetActive(false);
                    ToggleNoShuffle.gameObject.SetActive(false);
                    InputPort.gameObject.SetActive(false);
                    InputLP.gameObject.SetActive(false);
                    InputHand.gameObject.SetActive(false);
                    InputDraw.gameObject.SetActive(false);
                    break;
            }

            ButtonDeck.SetButtonText(Config.GetConfigDeckName());
            UltraSoloChrome.Apply(this);
        }

        private int printRevision;

        public void Print()
        {
            int revision = ++printRevision;
            superScrollView?.Clear();
            List<string[]> tasks = new();

            for (int i = 0; i < SoloSelector.bots.Count; i++)
            {
                string[] task = new string[]
                {
                    i.ToString(),
                };
                tasks.Add(task);
            }
            var handle = Addressables.LoadAssetAsync<GameObject>("UI/ItemSolo.prefab");
            handle.Completed += (result) =>
            {
                if (this == null || revision != printRevision)
                {
                    Addressables.Release(result);
                    return;
                }
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
                    ScrollRect);
                superScrollView.Print(tasks);
                if(superScrollView.items.Count > 0)
                {
                    var item0 = superScrollView.items[0].gameObject.GetComponent<SelectionToggle_Solo>();
                    if(Cursor.lockState == CursorLockMode.Locked)
                    {
                        item0.GetSelectable().Select();
                    }
                    else
                    {
                        Program.instance.solo.lastSoloItem = item0;
                        item0.SetToggleOn();
                    }
                }
            };
        }

        private void ItemOnListRefresh(string[] task, GameObject item)
        {
            var handler = item.GetComponent<SelectionToggle_Solo>();
            handler.index = int.Parse(task[0]);
            handler.botInfo = SoloSelector.bots[handler.index];
            handler.Refresh();
        }

        public bool IsLockHand()
        {
            return ToggleLockHand.isOn;
        }

        public bool IsNoCheck()
        {
            return ToggleNoCheck.isOn;
        }

        public bool IsNoShuffle()
        {
            return ToggleNoShuffle.isOn;
        }

        public int GetPort()
        {
            if (int.TryParse(InputPort.text, out var result) 
                && result > 0 
                && result <= 65535)
            { return result; }
            InputPort.text = DEFAULT_PORT.ToString();
            return DEFAULT_PORT;
        }

        public int GetLP()
        {
            if(int.TryParse(InputLP.text, out var result)
                && result > 0)
                { return result; }
            InputLP.text = DEFAULT_LP.ToString();
            return DEFAULT_LP;
        }

        public int GetHand()
        {
            if(int.TryParse(InputHand.text, out var result)
                && result > 0)
            { return result; }
            InputHand.text = DEFAULT_HAND.ToString();
            return DEFAULT_HAND;
        }

        public int GetDraw()
        {
            if(int.TryParse(InputDraw.text, out var result)
                && result >= 0)
            { return result; }
            InputDraw.text = DEFAULT_DRAW.ToString();
            return DEFAULT_DRAW;
        }

        public void SetOverview(string text, bool activateDeckButton, string source = null)
        {
            TextOverview.text = text;
            ButtonDeck.gameObject.SetActive(activateDeckButton);
            selectedSource = source;
            sourceControl.SetActive(!string.IsNullOrEmpty(source));
        }

        public string GetAIDeck()
        {
            return ButtonDeck.GetButtonText();
        }

        public void OnPlay()
        {
            if (SoloSelector.condition == SoloSelector.Condition.ForSolo)
                Program.instance.solo.StartAIForSolo(superScrollView.selected, ButtonDeck.gameObject.activeSelf);
            else
                Program.instance.solo.StartAIForRoom(superScrollView.selected, ButtonDeck.gameObject.activeSelf);
        }

        public void SelectLastSoloItem()
        {
            UserInput.NextSelectionIsAxis = true;
            Program.instance.solo.Select();
        }

        public void SelectOnRight()
        {
            ToggleLockHand.GetSelectable().Select();
        }

        public void OnSelectAIDeck()
        {
            Program.instance.deckSelector.SwitchCondition(DeckSelector.Condition.ForSolo);
            Program.instance.ShiftToServant(Program.instance.deckSelector);
        }
    }
}
