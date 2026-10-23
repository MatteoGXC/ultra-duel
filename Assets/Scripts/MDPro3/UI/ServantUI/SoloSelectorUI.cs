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

        public override void Initialize(Servant.Servant servant)
        {            
            base.Initialize(servant);
            ButtonDeck.gameObject.SetActive(false);
        }

        public override void ShowEvent()
        {
            base.ShowEvent();

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

        public void SetOverview(string text, bool activateDeckButton)
        {
            TextOverview.text = text;
            ButtonDeck.gameObject.SetActive(activateDeckButton);
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
