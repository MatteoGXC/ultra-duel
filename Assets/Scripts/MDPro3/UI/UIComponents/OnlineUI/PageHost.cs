using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using YgomSystem.ElementSystem;

namespace MDPro3.UI
{
    public class PageHost : MonoBehaviour
    {

        #region Elements

        private ElementObjectManager m_Manager;
        private ElementObjectManager Manager =>
            m_Manager = m_Manager != null ? m_Manager
            : GetComponent<ElementObjectManager>();

        private const string LABEL_SBN_REGULATION = "ButtonRegulation";
        private SelectionButton m_ButtonRegulation;
        private SelectionButton ButtonRegulation =>
            m_ButtonRegulation = m_ButtonRegulation != null ? m_ButtonRegulation
            : Manager.GetElement<SelectionButton>(LABEL_SBN_REGULATION);

        private const string LABEL_SBN_POOL = "ButtonPool";
        private SelectionButton m_ButtonPool;
        private SelectionButton ButtonPool =>
            m_ButtonPool = m_ButtonPool != null ? m_ButtonPool
            : Manager.GetElement<SelectionButton>(LABEL_SBN_POOL);

        private const string LABEL_SBN_MODE = "ButtonMode";
        private SelectionButton m_ButtonMode;
        private SelectionButton ButtonMode =>
            m_ButtonMode = m_ButtonMode != null ? m_ButtonMode
            : Manager.GetElement<SelectionButton>(LABEL_SBN_MODE);

        private const string LABEL_STG_CHECK = "ToggleCheck";
        private SelectionToggle m_ToggleCheck;
        private SelectionToggle ToggleCheck =>
            m_ToggleCheck = m_ToggleCheck != null ? m_ToggleCheck
            : Manager.GetElement<SelectionToggle>(LABEL_STG_CHECK);

        private const string LABEL_STG_SHUFFLE = "ToggleShuffle";
        private SelectionToggle m_ToggleShuffle;
        private SelectionToggle ToggleShuffle =>
            m_ToggleShuffle = m_ToggleShuffle != null ? m_ToggleShuffle
            : Manager.GetElement<SelectionToggle>(LABEL_STG_SHUFFLE);

        private const string LABEL_IPT_TIME = "InputFieldTime";
        private TMP_InputField m_InputTime;
        private TMP_InputField InputTime =>
            m_InputTime = m_InputTime != null ? m_InputTime
            : Manager.GetElement<TMP_InputField>(LABEL_IPT_TIME);

        private const string LABEL_IPT_LP = "InputFieldLP";
        private TMP_InputField m_InputLP;
        private TMP_InputField InputLP =>
            m_InputLP = m_InputLP != null ? m_InputLP
            : Manager.GetElement<TMP_InputField>(LABEL_IPT_LP);

        private const string LABEL_IPT_HAND = "InputFieldHand";
        private TMP_InputField m_InputHand;
        private TMP_InputField InputHand =>
            m_InputHand = m_InputHand != null ? m_InputHand
            : Manager.GetElement<TMP_InputField>(LABEL_IPT_HAND);

        private const string LABEL_IPT_DRAW = "InputFieldDraw";
        private TMP_InputField m_InputDraw;
        private TMP_InputField InputDraw =>
            m_InputDraw = m_InputDraw != null ? m_InputDraw
            : Manager.GetElement<TMP_InputField>(LABEL_IPT_DRAW);

        private const string LABEL_SBN_CREATE = "ButtonCreate";
        private SelectionButton m_ButtonCreate;
        private SelectionButton ButtonCreate =>
            m_ButtonCreate = m_ButtonCreate != null ? m_ButtonCreate
            : Manager.GetElement<SelectionButton>(LABEL_SBN_CREATE);

        #endregion

        private void Awake()
        {
            ResetArgs();
            SystemEvent.OnLanguageChange += ResetArgs;
        }

        private void OnDestroy()
        {
            SystemEvent.OnLanguageChange -= ResetArgs;
        }

        private void ResetArgs()
        {
            ButtonRegulation.SetButtonText(BanlistManager.Banlists[0].Name);
            ButtonPool.SetButtonText(StringHelper.GetUnsafe(1481));
            ButtonMode.SetButtonText(StringHelper.GetUnsafe(1244));
            ToggleCheck.SetToggleOff();
            ToggleShuffle.SetToggleOff();

            InputTime.text = OnlineServant.DEFAULT_TIME.ToString();
            InputLP.text = OnlineServant.DEFAULT_LP.ToString();
            InputHand.text = OnlineServant.DEFAULT_HAND.ToString();
            InputDraw.text = OnlineServant.DEFAULT_DRAW.ToString();
        }

        public void OnRegulation()
        {
            List<string> selections = new()
            {
                InterString.Get("禁限卡表"),
                string.Empty
            };
            selections.AddRange(BanlistManager.GetAllNames(true));
            UIManager.ShowPopupSelection(selections, ChangeBanlist, null);
        }

        private void ChangeBanlist()
        {
            string selected = EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            ButtonRegulation.SetButtonText(selected);
        }

        public void OnPool()
        {
            List<string> selections = new()
            {
                InterString.Get("卡片允许"),
                string.Empty
            };
            for (int i = 1481; i < 1487; i++)
                selections.Add(StringHelper.GetUnsafe(i));
            UIManager.ShowPopupSelection(selections, ChangePool, null);
        }

        private void ChangePool()
        {
            string selected = EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            ButtonPool.SetButtonText(selected);
        }

        public void OnMode()
        {
            List<string> selections = new()
            {
                InterString.Get("决斗模式"),
                string.Empty
            };
            for (int i = 1244; i < 1247; i++)
                selections.Add(StringHelper.GetUnsafe(i));
            UIManager.ShowPopupSelection(selections, ChangeMode, null);
        }

        private void ChangeMode()
        {
            string selected = EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            Manager.GetElement<SelectionButton>("ButtonMode").SetButtonText(selected);
        }

        public void OnHostCreate()
        {
            DuelFormat.Select(() => Program.instance.online.CreateServer(GerHostArgs()));
        }

        private List<string> GerHostArgs()
        {
            return new List<string>()
            {
                InterString.Get("创建主机"),
                ButtonRegulation.GetButtonText(),
                ButtonPool.GetButtonText(),
                ButtonMode.GetButtonText(),
                ToggleCheck.isOn ? "T" : "F",
                ToggleShuffle.isOn ? "T" : "F",
                GetTime().ToString(),
                GetLP().ToString(),
                GetHand().ToString(),
                GetDraw().ToString()
            };
        }

        private int GetTime()
        {
            if (int.TryParse(InputTime.text, out var result)
                && result >= 0)
            { return result; }
            InputTime.text = OnlineServant.DEFAULT_TIME.ToString();
            return OnlineServant.DEFAULT_TIME;
        }

        private int GetLP()
        {
            if (int.TryParse(InputLP.text, out var result)
                && result > 0)
            { return result; }
            InputLP.text = OnlineServant.DEFAULT_LP.ToString();
            return OnlineServant.DEFAULT_LP;
        }

        private int GetHand()
        {
            if (int.TryParse(InputHand.text, out var result)
                && result > 0)
            { return result; }
            InputHand.text = OnlineServant.DEFAULT_HAND.ToString();
            return OnlineServant.DEFAULT_HAND;
        }

        private int GetDraw()
        {
            if (int.TryParse(InputDraw.text, out var result)
                && result >= 0)
            { return result; }
            InputDraw.text = OnlineServant.DEFAULT_DRAW.ToString();
            return OnlineServant.DEFAULT_DRAW;
        }

        public void SelectDefault()
        {
            ButtonCreate.GetSelectable().Select();
        }
    }
}
