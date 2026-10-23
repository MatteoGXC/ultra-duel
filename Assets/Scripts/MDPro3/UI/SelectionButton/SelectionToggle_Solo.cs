using Mono.WebBrowser;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;

namespace MDPro3.UI
{
    public class SelectionToggle_Solo : SelectionToggle_ScrollRectItem
    {

        #region Elements

        private const string LABEL_TXT_TITLE = "Title";
        private TextMeshProUGUI m_Title;
        private TextMeshProUGUI Title =>
            m_Title = m_Title != null ? m_Title
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_TITLE);

        private const string LABEL_ART = "Image";
        private ArtRawImageHandler m_Art;
        private ArtRawImageHandler Art =>
            m_Art = m_Art != null ? m_Art
            : Manager.GetElement<ArtRawImageHandler>(LABEL_ART);

        private const string LABEL_GO_NUMBADGE = "NumBadge";
        private GameObject m_NumBadge;
        private GameObject NumBadge =>
            m_NumBadge = m_NumBadge != null ? m_NumBadge
            : Manager.GetElement(LABEL_GO_NUMBADGE);

        private const string LABEL_GO_TEXTCLEAR = "TextClear";
        private GameObject m_TextClear;
        private GameObject TextClear =>
            m_TextClear = m_TextClear != null ? m_TextClear
            : Manager.GetElement(LABEL_GO_TEXTCLEAR);

        #endregion

        public SoloSelector.BotInfo botInfo;
        private bool isDiyDeck;

        public override void Refresh()
        {
            base.Refresh();
            Title.text = botInfo.name;
            isDiyDeck = botInfo.command.Contains("Lucky");
            Art.SetArt(botInfo.main0);

            NumBadge.SetActive(false);
            TextClear.SetActive(false);
        }

        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();
            Program.instance.solo.GetUI<SoloSelectorUI>().superScrollView.selected = index;
            Program.instance.solo.GetUI<SoloSelectorUI>().SetOverview(botInfo.desc, isDiyDeck, botInfo.source);
            Program.instance.solo.lastSoloItem = this;
        }

        protected override void CallSubmitEvent()
        {
            base.CallSubmitEvent();
            if (SoloSelector.condition == SoloSelector.Condition.ForSolo)
                Program.instance.solo.StartAIForSolo(index, isDiyDeck);
            else
                Program.instance.solo.StartAIForRoom(index, isDiyDeck);
        }

        public void PublicSubmit()
        {
            CallSubmitEvent();
        }

        protected override void OnNavigation(AxisEventData eventData)
        {
            base.OnNavigation(eventData);

            if (eventData.moveDir == MoveDirection.Right)
            {
                UserInput.NextSelectionIsAxis = true;
                Program.instance.solo.GetUI<SoloSelectorUI>().SelectOnRight();
            }
        }
    }
}
